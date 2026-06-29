using FluentValidation;
using Listening.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.Identity.Client;

namespace Listening.Admin.WebApi.AlbumController.Request
{
	public class AlbumAddRequest
	{
		public string Title {  get; set; }
		public Guid CategoryId { get; set; }
	}

	public class AlbumAddRequrestValidator : AbstractValidator<AlbumAddRequest>
	{
		public AlbumAddRequrestValidator(ListengingDbContext dbContext)
		{
			RuleFor(s=>s.Title).NotEmpty();
			RuleFor(s=>s.CategoryId).MustAsync((cid,token) => dbContext.categories.AnyAsync(c => c.Id == cid)).WithMessage(c => $"CategoryId={c.CategoryId}不存在");
		}
	}
}
