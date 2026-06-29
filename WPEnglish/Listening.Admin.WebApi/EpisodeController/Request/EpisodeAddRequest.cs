using FluentValidation;
using Listening.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace Listening.Admin.WebApi.EpisodeController.Request
{
	public class EpisodeAddRequest
	{
		public string Title { get; set; }
		public Guid AlbumId { get; set; }
		public Uri AudioUrl { get; set; }
		public double DurationInSecond { get; set; }
		public string SubtitleType { get; set; }
		public string Subtitle { get; set; }

	}
	public class EpisodeAddRequestValidator : AbstractValidator<EpisodeAddRequest>
	{
		public EpisodeAddRequestValidator(ListengingDbContext db)
		{
			RuleFor(s => s.Title).NotEmpty().Length(1, 200);
			RuleFor(s => s.AlbumId).NotEmpty().MustAsync(async (albumId,token)=> await db.albums.AnyAsync(s=> s.Id == albumId)).WithMessage(c => $"AlbumId={c.AlbumId}不存在");
			RuleFor(s => s.AudioUrl).NotEmptyUri().Length(1, 1000);	
			RuleFor(s => s.DurationInSecond).GreaterThan(0);
			RuleFor(s => s.SubtitleType).NotEmpty().Length(1, 10);
			RuleFor(s => s.Subtitle).NotEmpty();
		}
	}
}
