using Listening.Domain.Entities;
using Listening.Domain.Subtitles;

namespace Listening.Main.WebApi.Controllers.Episodes.ViewModels
{
	public class EpisodeDto
	{
		public Guid Id { get; set; }
		public string Title { get; set; }
		public Uri AudioUrl { get; set; }
		public Guid AlbumId { get; set; }
		public double DurationInSecond { get; set; }
		public IEnumerable<SentenceDto>? Sentences { get; set; }

		public static EpisodeDto? Create(Episode? e, bool loadSubtitle)
		{
			if (e == null) return null;

			List<SentenceDto> sentenceDtos = new();
			if (loadSubtitle)
			{
				var sentences = e.ParseSubtitle();
				foreach (Sentence s in sentences)
				{
					SentenceDto vm = new SentenceDto() { StartTime = s.StartTime.TotalSeconds,EndTime = s.EndTime.TotalSeconds, Value = s.Value};
					sentenceDtos.Add(vm);
				}
			}
			return new EpisodeDto() { Id = e.Id, AlbumId = e.AlbumId , AudioUrl = e.AudioUrl, Title = e.Title, DurationInSecond = e.DurationInSecond, Sentences = sentenceDtos };
		}
		public static EpisodeDto[] Create(Episode[] episodes,bool loadSubtitle)
		{
			return episodes.Select(e => Create(e, loadSubtitle)!).ToArray();
		}
	}
}
