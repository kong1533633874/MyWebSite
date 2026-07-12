using FluentValidation;
using Listening.Admin.WebApi.EpisodeController.Request;
using Listening.Domain.Entities;
using Listening.Domain.Service;
using Listening.Infrastructure;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using TencentCloud.Common;
using TencentCloud.Asr.V20190614;
using TencentCloud.Asr.V20190614.Models;
using System.Text;

namespace Listening.Admin.WebApi.EpisodeController
{
	[Route("api/admin/[controller]/[action]")]
	[ApiController]
	[Authorize(Roles = "admin")]
	public class EpisodeController : ControllerBase
	{
		private readonly IListeningRepository repository;
		private readonly ListeningService listeningService;
		private readonly ListengingDbContext db;
		private readonly IValidator<EpisodeAddRequest> addValidator;
		private readonly IValidator<EpisodeUpdateRequest> updateValidator;
		private readonly IValidator<EpisodeSortRequest> sortedValidator;
		private readonly ILogger<EpisodeController> logger;
		private readonly IConfiguration configuration;

		public EpisodeController(IListeningRepository repository,ListeningService listeningService,ListengingDbContext db,
			IValidator<EpisodeAddRequest> addValidator,
			IValidator<EpisodeUpdateRequest> updateValidator,
			IValidator<EpisodeSortRequest> sortedValidator,
			ILogger<EpisodeController> logger,
			IConfiguration configuration)
		{
			this.repository = repository;
			this.listeningService = listeningService;
			this.db = db;
			this.addValidator = addValidator;
			this.updateValidator = updateValidator;
			this.sortedValidator = sortedValidator;
			this.logger = logger;
			this.configuration = configuration;
		}

		[HttpGet]
		public async Task<ActionResult<Episode[]>> GetAllEpisodes(Guid AlbumId)
		{
			return await repository.GetAllEpisodesByAlbumIdAsync(AlbumId);
		}

		[HttpGet]
		public async Task<ActionResult<Episode?>> GetEpisodeById(Guid AlbumId)
		{
			var episode = await repository.GetEpisodeByIdAsync(AlbumId);
			if (episode == null)
			{
				return NotFound($"没有Id={AlbumId}的Episode");
			}
			return episode;
		}
		[HttpPost]
		public async Task<ActionResult> Add(EpisodeAddRequest episodeAddRequest)
		{
			var result = await addValidator.ValidateAsync(episodeAddRequest);
			if(!result.IsValid)
			{
				var errors = string.Join("; ", result.Errors.Select(s => s.ErrorMessage));
				return BadRequest(errors);
			}
			var episode = await listeningService.AddEpisodeAsync(episodeAddRequest.Title,
				episodeAddRequest.SubtitleType,
				episodeAddRequest.Subtitle,
				episodeAddRequest.AlbumId,
				episodeAddRequest.AudioUrl,
				episodeAddRequest.DurationInSecond);
			await db.episodes.AddAsync(episode);
			await db.SaveChangesAsync();
			logger.LogInformation("剧集添加成功：{EpisodeId}，标题：{Title}，专辑：{AlbumId}", episode.Id, episodeAddRequest.Title, episodeAddRequest.AlbumId);
			return Ok();
		}

		[HttpPut]
		public async Task<ActionResult> Update(EpisodeUpdateRequest episodeUpdateRequest)
		{
			var result = await updateValidator.ValidateAsync(episodeUpdateRequest);
			if (!result.IsValid)
			{
				var errors = string.Join("; ", result.Errors.Select(s => s.ErrorMessage));
				return BadRequest(errors);
			}

			var domainResult = await listeningService.UpdateEpisodeAsync(episodeUpdateRequest.Id, episodeUpdateRequest.Title, episodeUpdateRequest.SubtitleType, episodeUpdateRequest.Subtitle);
			if(!domainResult.Success)
			{
				return StatusCode(domainResult.StatusCode,domainResult.Message);
			}
			await db.SaveChangesAsync();
			logger.LogInformation("剧集更新成功：{EpisodeId}", episodeUpdateRequest.Id);
			return Ok();
		}

		[HttpDelete]
		public async Task<ActionResult> Delete(Guid id)
		{
			var domainResult = await listeningService.DeleteEpisodeAsync(id);
			if (!domainResult.Success)
			{
				return StatusCode(domainResult.StatusCode,domainResult.Message);
			}
			await db.SaveChangesAsync();
			logger.LogWarning("剧集已软删除：{EpisodeId}", id);
			return Ok();
		}
		[HttpPut]
		public async Task<ActionResult> Reorder(EpisodeSortRequest episodeSortRequest)
		{
			var result = await sortedValidator.ValidateAsync(episodeSortRequest);
			if (!result.IsValid)
			{
				var errors = string.Join("; ", result.Errors.Select(s => s.ErrorMessage));
				return BadRequest(errors);
			}
			var domainResult = await listeningService.SortEpisodesAsync(episodeSortRequest.SortedAlbumsIds, episodeSortRequest.AlbumId);
			if (!domainResult.Success)
			{
				return StatusCode(domainResult.StatusCode,domainResult.Message);
			}
			await db.SaveChangesAsync();
			logger.LogInformation("剧集排序成功，专辑：{AlbumId}", episodeSortRequest.AlbumId);
			return Ok();
		}

		[HttpPut]
		public async Task<ActionResult> Hide(Guid id)
		{
			var episode = await repository.GetEpisodeByIdAsync(id);
			if (episode == null)
			{
				return NotFound($"没有Id={id}的Episode");
			}
			episode.Hide();
			await db.SaveChangesAsync();
			logger.LogInformation("剧集已隐藏：{EpisodeId}", id);
			return Ok();
		}

		[HttpPut]
		public async Task<ActionResult> Show(Guid id)
		{
			var episode = await repository.GetEpisodeByIdAsync(id);
			if (episode == null)
			{
				return NotFound($"没有Id={id}的Episode");
			}
			episode.Show();
			await db.SaveChangesAsync();
			logger.LogInformation("剧集已显示：{EpisodeId}", id);
			return Ok();
		}

		/// <summary>
		/// AI 生成字幕：调用腾讯云语音识别将音频转写为字幕文本
		/// </summary>
		[HttpPost]
		[RequestSizeLimit(100_000_000)] // 最大 100MB
		[RequestFormLimits(MultipartBodyLengthLimit = 100_000_000)]
		public async Task<ActionResult> GenerateSubtitle(IFormFile audioFile, [FromForm] string subtitleType)
		{
			if (audioFile == null || audioFile.Length == 0)
				return BadRequest("请上传音频文件");

			if (subtitleType != "srt" && subtitleType != "lrc")
				return BadRequest("字幕格式仅支持 srt 或 lrc");

			try
			{
				// 1. 读取音频文件为 base64
				using var ms = new MemoryStream();
				await audioFile.CopyToAsync(ms);
				byte[] audioBytes = ms.ToArray();
				string audioBase64 = Convert.ToBase64String(audioBytes);
				logger.LogInformation("音频接收完成，大小: {Size} bytes，类型: {Type}", audioBytes.Length, audioFile.ContentType);

				// 2. 初始化腾讯云 ASR 客户端
				var cred = new Credential
				{
					SecretId = configuration["TencentCloud:SecretId"],
					SecretKey = configuration["TencentCloud:SecretKey"]
				};
				var client = new AsrClient(cred, configuration["TencentCloud:AsrRegion"]);

				// 3. 提交录音文件识别（上传模式）
				var createReq = new CreateRecTaskRequest
				{
					EngineModelType = configuration["TencentCloud:EngineType"] ?? "16k_en",
					ChannelNum = 1,
					ResTextFormat = 1,
					SourceType = 1,
					Data = audioBase64,
					DataLen = (ulong)audioBytes.LongLength,
					ConvertNumMode = 1
				};

				logger.LogInformation("提交腾讯云 ASR 任务（上传模式），大小: {Size} bytes", audioBytes.Length);
				var createResp = await client.CreateRecTask(createReq);
				ulong? taskId = createResp.Data.TaskId;
				logger.LogInformation("ASR 任务提交成功：TaskId={TaskId}", taskId);

				// 4. 轮询等待识别完成（最多 10 分钟）
				var sentenceDetails = await PollAsrResultAsync(client, taskId);
				if (sentenceDetails == null || sentenceDetails.Count == 0)
				{
					return BadRequest("语音识别未返回有效结果");
				}

				// 5. 格式化为字幕文本
				string subtitle = FormatSubtitle(sentenceDetails, subtitleType);

				logger.LogInformation("字幕生成成功：共 {Count} 句，格式 {Format}", sentenceDetails.Count, subtitleType);
				return Ok(new { subtitle, segmentsCount = sentenceDetails.Count });
			}
			catch (Exception ex)
			{
				logger.LogError(ex, "AI 字幕生成失败");
				return StatusCode(500, $"识别失败：{ex.Message}");
			}
		}

		/// <summary>
		/// 轮询腾讯云 ASR 任务状态直至完成
		/// </summary>
		private async Task<List<SentenceDetail>> PollAsrResultAsync(AsrClient client, ulong? taskId)
		{
			var describeReq = new DescribeTaskStatusRequest
			{
				TaskId = taskId
			};

			int maxRetries = 120; // 最多等 120 次 × 5 秒 = 10 分钟
			for (int i = 0; i < maxRetries; i++)
			{
				var resp = await client.DescribeTaskStatus(describeReq);
				var data = resp.Data;

				if (data.StatusStr == "success")
				{
					return data.ResultDetail?.ToList() ?? new List<SentenceDetail>();
				}
				else if (data.StatusStr == "failed")
				{
					throw new Exception($"识别失败：{data.ErrorMsg ?? "未知错误"}");
				}

				await System.Threading.Tasks.Task.Delay(5000); // 每 5 秒轮询一次
			}

			throw new TimeoutException("语音识别超时（超过 10 分钟）");
		}

		/// <summary>
		/// 将句子列表格式化为 SRT 或 LRC 字幕
		/// </summary>
		private string FormatSubtitle(List<SentenceDetail> sentences, string format)
		{
			var sb = new StringBuilder();

			if (format == "srt")
			{
				int index = 1;
				foreach (var s in sentences)
				{
					string start = FormatSrtTime((long)(s.StartMs!));
					string end = FormatSrtTime((long)(s.EndMs!));
					sb.AppendLine(index.ToString());
					sb.AppendLine($"{start} --> {end}");
					sb.AppendLine(s.FinalSentence?.Trim() ?? "");
					sb.AppendLine();
					index++;
				}
			}
			else // lrc
			{
				foreach (var s in sentences)
				{
					string ts = FormatLrcTime((long)(s.StartMs!));
					sb.AppendLine($"{ts}{s.FinalSentence?.Trim() ?? ""}");
				}
			}

			return sb.ToString().TrimEnd();
		}

		private static string FormatSrtTime(long milliseconds)
		{
			var ts = TimeSpan.FromMilliseconds(milliseconds);
			return $"{ts.Hours:D2}:{ts.Minutes:D2}:{ts.Seconds:D2},{ts.Milliseconds:D3}";
		}

		private static string FormatLrcTime(long milliseconds)
		{
			var ts = TimeSpan.FromMilliseconds(milliseconds);
			return $"[{ts.Minutes:D2}:{ts.Seconds:D2}.{ts.Milliseconds / 10:D2}]";
		}

	}
}
