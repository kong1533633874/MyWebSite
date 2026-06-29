using Listening.Domain.Service;
using Listening.Infrastructure;
using Microsoft.AspNetCore.Mvc;
using Listening.Domain.Entities;
using Listening.Admin.WebApi.CategoryController.Request;
using FluentValidation;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Logging;

namespace Listening.Admin.WebApi.CategoryController
{
	[Route("api/admin/[controller]/[action]")]
	[ApiController]
	[Authorize(Roles = "admin")]
	public class CategoryController : ControllerBase
	{
		private readonly IListeningRepository listeningRepository;
		private readonly ListeningService listeningService;
		private readonly ListengingDbContext listengingDbContext;
		private readonly IValidator<CategoryAddRequest> addValidator;
		private readonly IValidator<CategoryUpdateRequest> updateValidator;
		private readonly IValidator<CategoriesSortRequest> sortValidator;
		private readonly ILogger<CategoryController> logger;

		public CategoryController(IListeningRepository listeningRepository,
			ListeningService listeningService,
			ListengingDbContext listengingDbContext,
			IValidator<CategoryAddRequest> addValidator,
			IValidator<CategoryUpdateRequest> updateValidator,
			IValidator<CategoriesSortRequest> sortValidator,
			ILogger<CategoryController> logger)
		{
			this.listeningRepository = listeningRepository;
			this.listeningService = listeningService;
			this.listengingDbContext = listengingDbContext;
			this.addValidator = addValidator;
			this.updateValidator = updateValidator;
			this.sortValidator = sortValidator;
			this.logger = logger;
		}

		[HttpPost]
		public async Task<ActionResult<Guid>> Add(CategoryAddRequest categoryAddRequest)
		{
			var result = await addValidator.ValidateAsync(categoryAddRequest);
			if (!result.IsValid)
			{
				return BadRequest(result.Errors);
			}

			Category category = await listeningService.AddCategoryAsync(categoryAddRequest.Title, categoryAddRequest.CoverUrl);
			listengingDbContext.Add(category);
			await listengingDbContext.SaveChangesAsync();
			logger.LogInformation("分类添加成功：{CategoryId}，标题：{Title}", category.Id, categoryAddRequest.Title);
			return category.Id;
		}

		[HttpPut]
		public async Task<ActionResult> Update([FromBody]CategoryUpdateRequest categoryUpdateRequest)
		{
			var result = await updateValidator.ValidateAsync(categoryUpdateRequest);
			if (!result.IsValid)
			{
				return BadRequest(result.Errors);
			}
			var category = await listeningRepository.GetCategoryByIdAsync(categoryUpdateRequest.Id);
			if (category == null)
			{
				return NotFound("id不存在");
			}
			category.ChangeTitle(categoryUpdateRequest.Title).ChangeCoverUrl(categoryUpdateRequest.CoverUrl);
			category.NotifyModified();
			await listengingDbContext.SaveChangesAsync();
			logger.LogInformation("分类更新成功：{CategoryId}", categoryUpdateRequest.Id);
			return Ok();
		}

		[HttpDelete]
		public async Task<ActionResult> Delete(Guid id)
		{
			Category? category = await listeningRepository.GetCategoryByIdAsync(id);
			if (category == null)
			{
				return NotFound("id不存在");
			}
			category.SoftDelete();
			await listengingDbContext.SaveChangesAsync();
			logger.LogWarning("分类已软删除：{CategoryId}", id);
			return Ok();
		}

		[HttpGet]
		public async Task<Category[]> GetAllCategories()
		{
			return await listeningRepository.GetAllCategoriesAsync();
		}

		/// <summary>
		/// 按请求体中的顺序重排未删除分类的 SequenceNumber（从 1 递增）。
		/// </summary>
		[HttpPut]
		public async Task<ActionResult> Reorder(CategoriesSortRequest sortRequest)
		{
			var result = await sortValidator.ValidateAsync(sortRequest);
			if (!result.IsValid)
			{
				return BadRequest(result.Errors);
			}

			await listeningService.SortCategoriesAsync(sortRequest.SortedCategoryIds);

			await listengingDbContext.SaveChangesAsync();
			logger.LogInformation("分类排序成功");
			return Ok();
		}
	}
}
