using FluentValidation;

namespace Listening.Admin.WebApi.EpisodeController.Request
{
	public class EpisodeSortRequest
	{
		public Guid[] SortedAlbumsIds {  get; set; }
		public Guid AlbumId { get; set; }
	}

	public class EpisodeSortRequestValidator : AbstractValidator<EpisodeSortRequest>
	{
		public EpisodeSortRequestValidator()
		{
			RuleFor(x => x.SortedAlbumsIds).NotEmpty().NotDuplicated().NotContains(Guid.Empty);
			RuleFor(s => s.AlbumId).NotEmpty();
		}
	}
}
