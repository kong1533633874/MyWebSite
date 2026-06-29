using FluentValidation;
using Listening.Infrastructure;

namespace Listening.Admin.WebApi.EpisodeController.Request
{
	public class EpisodeUpdateRequest
	{
		public Guid Id { get; set; }
		public string Title { get; set; }
		public string SubtitleType { get; set; }
		public string Subtitle { get; set; }

	}

	public class EpisodeUpdateRequestValidator : AbstractValidator<EpisodeUpdateRequest>
	{
		public EpisodeUpdateRequestValidator(ListengingDbContext db)
		{
			RuleFor(s=>s.Id).NotEmpty();
			RuleFor(s => s.Title).NotEmpty().Length(1, 200);
			RuleFor(s => s.SubtitleType).NotEmpty().Length(1, 10);
			RuleFor(s => s.Subtitle).NotEmpty();
		}
	}
}
