using Listening.Domain.Entities;
using Listening.Domain.Service;
using Listening.Infrastructure;
using Listening.Main.WebApi.Controllers.Catogories.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.VisualBasic;


namespace Listening.Main.WebApi.Controllers.Catogories
{
	[Route("api/main/[controller]/[action]")]
	[ApiController]
	public class CategoryController : ControllerBase
	{
		private readonly IListeningRepository _repository;
		private readonly IMemoryCache _memoryCache;

		public CategoryController(IListeningRepository repository,IMemoryCache memoryCache)
		{
			this._repository = repository;
			this._memoryCache = memoryCache;
		}

		[HttpGet]
		public async Task<ActionResult<CategoryDto?>> FindById(Guid id)
		{
		 	var category = await _memoryCache.GetOrCreateAsync($"CategoryController.FindById.{id}", async (e) =>
			{
				e.AbsoluteExpirationRelativeToNow = TimeSpan.FromSeconds(Random.Shared.NextDouble(60, 60 * 2));
				return await _repository.GetCategoryByIdAsync(id);
			});
			if (category == null)
			{
				return NotFound($"没有Id={id}的Category");
			}
			else
				return CategoryDto.Create(category);
		}

		[HttpGet]
		public async Task<ActionResult<CategoryDto[]?>> FindAll()
		{
			var categories = await _memoryCache.GetOrCreateAsync($"CategoryController.FindAll", async (e) =>
			{
				e.AbsoluteExpirationRelativeToNow = TimeSpan.FromSeconds(Random.Shared.NextDouble(60, 60 * 2));
				return await _repository.GetAllCategoriesAsync();
			});
		 	
			return CategoryDto.Create(categories);
		}
	}
}
