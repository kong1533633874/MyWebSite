using FluentValidation;
using Listening.Admin.WebApi.AlbumController.Request;
using Listening.Domain.Entities;
using Listening.Domain.Service;
using Listening.Infrastructure;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace Listening.Admin.WebApi.AlbumController
{
	[Route("api/admin/[controller]/[action]")]
	[ApiController]
	[Authorize(Roles = "admin")]
	public class AlbumController : ControllerBase
	{
		private readonly ListeningService listeningService;
		private readonly IListeningRepository listeningRepository;
		private readonly ListengingDbContext listengingDbContext;
		private readonly IValidator<AlbumAddRequest> addValidator;
		private readonly IValidator<AlbumUpdateRequest> updateValidator;
		private readonly IValidator<AlbumSortRequest> sortValidator;
		private readonly ILogger<AlbumController> logger;

		public AlbumController(ListeningService listeningService,
			IListeningRepository listeningRepository,
			ListengingDbContext listengingDbContext,
			IValidator<AlbumAddRequest> addValidator,
			IValidator<AlbumUpdateRequest> updateValidator,
			IValidator<AlbumSortRequest>  sortValidator,
			ILogger<AlbumController> logger)
		{
			this.listeningService = listeningService;
			this.listeningRepository = listeningRepository;
			this.listengingDbContext = listengingDbContext;
			this.addValidator = addValidator;
			this.updateValidator = updateValidator;
			this.sortValidator = sortValidator;
			this.logger = logger;
		}

		[HttpGet]
		public async Task<Album[]?> GetAllAlbums(Guid categoryId)
		{
			return await listeningRepository.GetAllAlbumsByCategoryIdAsync(categoryId);
		}

		[HttpPost]
		public async Task<ActionResult> Add(AlbumAddRequest albumAddRequest)
		{
			var result = await addValidator.ValidateAsync(albumAddRequest);
			if (!result.IsValid)
			{
				return BadRequest(result.Errors);
			}
			var album = await listeningService.AddAlbumAsync(albumAddRequest.Title, albumAddRequest.CategoryId);
			await listengingDbContext.albums.AddAsync(album);
			await listengingDbContext.SaveChangesAsync();
			logger.LogInformation("专辑添加成功：{AlbumId}，标题：{Title}，分类：{CategoryId}", album.Id, albumAddRequest.Title, albumAddRequest.CategoryId);
			return Ok();
		}

		[HttpPut]
		public async Task<ActionResult> Update(AlbumUpdateRequest albumUpdateRequest)
		{
			var result = await updateValidator.ValidateAsync(albumUpdateRequest);
			if (!result.IsValid)
			{
				return BadRequest(result.Errors);
			}

			var album = await listeningRepository.GetAlbumByIdAsync(albumUpdateRequest.Id);
			if (album == null)
			{
				return NotFound($"id:{albumUpdateRequest.Id}Ablum不存在");
			}
			album.ChangeTitle(albumUpdateRequest.Title);
			album.NotifyModified();
			await listengingDbContext.SaveChangesAsync();
			logger.LogInformation("专辑更新成功：{AlbumId}", albumUpdateRequest.Id);
			return Ok();
		}
		[HttpDelete]
		public async Task<ActionResult> Delete(Guid id)
		{
			var album = await listeningRepository.GetAlbumByIdAsync(id);
			if (album == null)
			{
				return NotFound($"id:{id}Ablum不存在");
			}
			album.SoftDelete();
			await listengingDbContext.SaveChangesAsync();
			logger.LogWarning("专辑已软删除：{AlbumId}", id);
			return Ok();
		}
		[HttpPut]
		public async Task<ActionResult> Reorder(AlbumSortRequest albumSortRequest)
		{
			var result = sortValidator.Validate(albumSortRequest);
			if (!result.IsValid)
			{
				return BadRequest(result.Errors);
			}
			await listeningService.SortAlbumsAsync(albumSortRequest.SortedAlbumIds, albumSortRequest.CategoryId);

			await listengingDbContext.SaveChangesAsync();
			logger.LogInformation("专辑排序成功，分类：{CategoryId}", albumSortRequest.CategoryId);
			return Ok();
		}

		[HttpPut]
		public async Task<ActionResult> Hide(Guid id)
		{
			var album = await listeningRepository.GetAlbumByIdAsync(id);
			if(album == null)
			{
				return NotFound($"id为{id}的album不存在");
			}
			album.Hide();
			await listengingDbContext.SaveChangesAsync();
			logger.LogInformation("专辑已隐藏：{AlbumId}", id);
			return Ok();
		}

		[HttpPut]
		public async Task<ActionResult> Show(Guid id)
		{
			var album = await listeningRepository.GetAlbumByIdAsync(id);
			if (album == null)
			{
				return NotFound($"id为{id}的album不存在");
			}
			album.Show();
			await listengingDbContext.SaveChangesAsync();
			logger.LogInformation("专辑已显示：{AlbumId}", id);
			return Ok();
		}
	}
}
