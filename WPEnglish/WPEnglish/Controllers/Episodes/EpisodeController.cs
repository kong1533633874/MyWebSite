using Listening.Domain.Entities;
using Listening.Domain.Service;
using Listening.Main.WebApi.Controllers.Episodes.ViewModels;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.OpenApi.Writers;
using Microsoft.VisualBasic;

namespace Listening.Main.WebApi.Controllers.Episodes
{
	[Route("api/main/[controller]/[action]")]
	[ApiController]
	public class EpisodeController : ControllerBase
	{
		private readonly IListeningRepository _repository;
		private readonly IMemoryCache _memoryCache;

		public EpisodeController(IListeningRepository repository, IMemoryCache memoryCache)
		{
			this._repository = repository;
			this._memoryCache = memoryCache;
		}

		[HttpGet]
		public async Task<ActionResult<EpisodeDto?>> FindById(Guid id)
		{
			var episode = await _memoryCache.GetOrCreateAsync($"EpisodeController.FindById.{id}", async (e) =>
			{
				e.AbsoluteExpirationRelativeToNow = TimeSpan.FromSeconds(Random.Shared.NextDouble(60, 60 * 2));
				return await _repository.GetEpisodeByIdAsync(id);
			});
			if (episode == null)
			{
				return NotFound($"不存在id:{id}的episode");
			}
			else
				return EpisodeDto.Create(episode, true);
		}

		[HttpGet]
		public async Task<ActionResult<EpisodeDto[]?>> FindAllByAlbumId(Guid id)
		{
			var episdoes = await _memoryCache.GetOrCreateAsync($"EpisodeController.FindAllByAlbumId.{id}", async (e) =>
			{
				e.AbsoluteExpirationRelativeToNow = TimeSpan.FromSeconds(Random.Shared.NextDouble(60, 60 * 2));
				return await _repository.GetAllEpisodesByAlbumIdAsync(id);
			});
			return EpisodeDto.Create(episdoes,false);
		}
	}
}
