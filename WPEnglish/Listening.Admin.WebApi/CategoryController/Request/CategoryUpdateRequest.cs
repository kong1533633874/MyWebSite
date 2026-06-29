using FluentValidation;

namespace Listening.Admin.WebApi.CategoryController.Request
{
	public class CategoryUpdateRequest
	{
		public Guid Id { get; set; }
		public string Title { get; set; }
		public Uri CoverUrl { get; set; }
	}

	public class CategoryUpdateRequestValidator : AbstractValidator<CategoryUpdateRequest>
	{
		public CategoryUpdateRequestValidator()
		{
			RuleFor(s => s.Id).NotEmpty();
			RuleFor(s => s.Title).NotEmpty().WithMessage("标题不能为空").NotNull().Length(1, 200);
			RuleFor(s => s.CoverUrl).NotNull();
		}
	}
}
