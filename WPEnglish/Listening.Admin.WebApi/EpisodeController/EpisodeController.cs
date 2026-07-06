using FluentValidation;
using Listening.Admin.WebApi.EpisodeController.Request;
using Listening.Domain.Entities;
using Listening.Domain.Service;
using Listening.Infrastructure;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

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

		public EpisodeController(IListeningRepository repository,ListeningService listeningService,ListengingDbContext db,
			IValidator<EpisodeAddRequest> addValidator,
			IValidator<EpisodeUpdateRequest> updateValidator,
			IValidator<EpisodeSortRequest> sortedValidator,
			ILogger<EpisodeController> logger)
		{
			this.repository = repository;
			this.listeningService = listeningService;
			this.db = db;
			this.addValidator = addValidator;
			this.updateValidator = updateValidator;
			this.sortedValidator = sortedValidator;
			this.logger = logger;
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
	}
}
