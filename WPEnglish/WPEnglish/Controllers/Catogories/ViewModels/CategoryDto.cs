using Listening.Domain.Entities;

namespace Listening.Main.WebApi.Controllers.Catogories.ViewModels
{
	public class CategoryDto
	{
		public Guid Id { get; set; }
		public string Title { get; set; }
		public Uri CoverUrl { get; set; }
		public static CategoryDto? Create(Category? category)
		{
			if (category == null) return null;
			return new CategoryDto { Id = category.Id, CoverUrl = category.CoverUrl, Title = category.Title };
		}
		public static CategoryDto[]? Create(Category[] categories)
		{
			return categories.Select(s => Create(s)!).ToArray();
		}
	}
}
