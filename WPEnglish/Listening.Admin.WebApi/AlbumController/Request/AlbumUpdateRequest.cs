using FluentValidation;
using Microsoft.EntityFrameworkCore.ChangeTracking;

namespace Listening.Admin.WebApi.AlbumController.Request
{
	public class AlbumUpdateRequest
	{ 
		public Guid Id { get; set; }
		public string Title { get; set; }

	}

	public class AlbumUpdateRequestValidator : AbstractValidator<AlbumUpdateRequest>
	{
		public AlbumUpdateRequestValidator()
		{
			RuleFor(x => x.Id).NotEmpty();
			RuleFor(s => s.Title).NotEmpty();
		}
	}
}
