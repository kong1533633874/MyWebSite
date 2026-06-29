using FluentValidation;

namespace Listening.Admin.WebApi.CategoryController.Request
{
	public class CategoriesSortRequest
	{
		public Guid[] SortedCategoryIds { set; get; }
	}

	public class CategoriesSortRequestValidator : AbstractValidator<CategoriesSortRequest>
	{
		public CategoriesSortRequestValidator()
		{
			RuleFor(s => s.SortedCategoryIds).NotEmpty().NotNull().NotDuplicated().NotContains(Guid.Empty);
		}
	}


}
