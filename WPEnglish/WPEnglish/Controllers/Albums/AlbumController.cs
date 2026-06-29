using Listening.Domain.Entities;
using Listening.Domain.Service;
using Listening.Infrastructure;
using Listening.Main.WebApi.Controllers.Albums.ViewModels;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.VisualBasic;

namespace Listening.Main.WebApi.Controllers.Albums
{
	[Route("api/main/[controller]/[action]")]
	[ApiController]
	public class AlbumController : ControllerBase
	{
		private readonly IListeningRepository _repository;
		private readonly IMemoryCache _memoryCache;

		public AlbumController(IListeningRepository repository, IMemoryCache memoryCache)
		{
			this._repository = repository;
			this._memoryCache = memoryCache;
		}

		[HttpGet]
		public async Task<ActionResult<AlbumDto?>> FindById(Guid id)
		{
			var album = await _memoryCache.GetOrCreateAsync($"AlbumController.FindById.{id}", async (e) =>
			{
				e.AbsoluteExpirationRelativeToNow = TimeSpan.FromSeconds(Random.Shared.NextDouble(60, 60 * 2));
				return  await _repository.GetAlbumByIdAsync(id);
			});
			if (album == null)
			{
				return NotFound($"不存在id:{id}的album");
			}
			return Ok(AlbumDto.Create(album));
		}

		[HttpGet]
		public async Task<ActionResult<AlbumDto[]?>> FindAllByCategoryId(Guid id)
		{
			var albums = await _memoryCache.GetOrCreateAsync($"AlbumController.FindAllByCategoryId.{id}", async (e) =>
			{
				e.AbsoluteExpirationRelativeToNow = TimeSpan.FromSeconds(Random.Shared.NextDouble(60, 60 * 2));
				return await _repository.GetAllAlbumsByCategoryIdAsync(id);
			});
			return Ok(AlbumDto.Create(albums));

		}
	}
}
