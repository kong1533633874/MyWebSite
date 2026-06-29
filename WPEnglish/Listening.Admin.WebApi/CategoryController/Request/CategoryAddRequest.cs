using FluentValidation;

namespace Listening.Admin.WebApi.CategoryController.Request
{
	public class CategoryAddRequest
	{
		public string Title { get; set; }
		public Uri CoverUrl { get; set; }
	}

	public class CategoryAddRequestValidator: AbstractValidator<CategoryAddRequest>
	{
		public CategoryAddRequestValidator()
		{
			RuleFor(s => s.Title).NotEmpty().Length(1, 200).Must(s=> !string.IsNullOrWhiteSpace(s)).WithMessage("不能为空字符");
			RuleFor(s => s.CoverUrl).NotNull();
		}
	}
}
