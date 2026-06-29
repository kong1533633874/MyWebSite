using FluentValidation;
using Listening.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace Listening.Admin.WebApi.AlbumController.Request
{
	public class AlbumSortRequest
	{
		public Guid[] SortedAlbumIds { get; set; }
		public Guid CategoryId { get; set; }
	}

	public class AlbumSortRequestValidator : AbstractValidator<AlbumSortRequest>
	{
		public AlbumSortRequestValidator(ListengingDbContext dbContext)
		{
			RuleFor(s => s.SortedAlbumIds).NotEmpty().NotNull().NotDuplicated().NotContains(Guid.Empty);
			//RuleFor(s => s.CategoryId).NotEmpty().MustAsync((albumId,token)=> dbContext.albums.AnyAsync(a => a.Id == albumId));
			RuleFor(s => s.CategoryId).NotEmpty();
		}
	}
}
