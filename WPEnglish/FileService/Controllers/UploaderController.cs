using FileService.Domain.Service;
using FileService.Entities;
using FileService.Infrastructure;
using FileService.WebApi.Controllers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore.Metadata.Internal;
using Microsoft.Extensions.Logging;

namespace FileService.Controllers
{
	[Route("api/file/[controller]/[action]")]
	[ApiController]
	[Authorize(Roles = "admin")]
	public class UploaderController : ControllerBase
	{
		private readonly IFSRepository fSRepository;
		private readonly FSDomainService fSDomainService;
		private readonly FSDbContext fSDbContext;
		private readonly ILogger<UploaderController> logger;

		public UploaderController(IFSRepository fSRepository,FSDomainService fSDomainService,FSDbContext fSDbContext,
			ILogger<UploaderController> logger)
		{
			this.fSRepository = fSRepository;
			this.fSDomainService = fSDomainService;
			this.fSDbContext = fSDbContext;
			this.logger = logger;
		}
		[HttpGet]
		public async Task<ActionResult<FileExistsResponse>> FileExist(long fileSize,string sha256Hash)
		{
			AudioFile? audioFile = await fSRepository.FindFileAsync(fileSize, sha256Hash);
			if (audioFile == null)
			{
				return new FileExistsResponse(false,null);
			}
			else
			{
				return new FileExistsResponse(true,audioFile.Url);
			}
		}

		[HttpPost]
		[RequestSizeLimit(60_000_000)]
		public async Task<ActionResult<Uri>> Upload(IFormFile file)
		{
			string fileName = file.FileName;
			Stream stream = file.OpenReadStream();
			var (audiofile,isExisted) = await fSDomainService.UploadAsync(fileName,stream);

			if (!isExisted)
			{
				fSDbContext.AudioFiles.Add(audiofile);
				await fSDbContext.SaveChangesAsync();
				string message = "";
				if (fileName.EndsWith("jpg")) message = "封面图片文件入库成功";
				else message = "音频文件入库成功";
				logger.LogInformation("{Message}：{FileName}，大小：{FileSize}", message, fileName, audiofile.FileSize);
			}
			return audiofile.Url;
		}
	}
}
