using Listening.Domain.Subtitles;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace Listening.Domain.Entities
{
	public class Episode
	{
		public Guid Id { get;private set; }
		public string Title { get;private set; }
		public Guid AlbumId { get; private set; }
		public int SequenceNumber { get; private set; }
		public bool IsVisible { get; private set; }

		//
		public Uri AudioUrl { get; private set; }
		public double DurationInSecond { get; private set; }
		public string Subtitle { get; private set; }
		public string SubtitleType { get; private set; }
		//
		public DateTime CreateTime { get; private set; }
		public DateTime? LastModificationTime { get; private set; }
		public bool IsDeleted { get; private set; }
		public DateTime? DeletionTime { get; private set; }
		public static Episode Create(string title, Uri url, Guid albumId, int seq)
		{
			Episode episode = new Episode()
			{
				Id = Guid.NewGuid(),
				Title = title,
				AudioUrl = url,
				AlbumId = albumId,
				CreateTime = DateTime.UtcNow,
				SequenceNumber = seq,
				IsVisible = true
			};
			return episode;
		}
		public Episode ChangeSequenceNumber(int sequenceNumber)
		{
			this.SequenceNumber = sequenceNumber;
			return this;
		}

		public Episode ChangeTitle(string value)
		{
			this.Title = value;
			return this;
		}

		public Episode ChangeSubtitle(string subtitleType,string subtitle)
		{
			var parser = SubtitleParserFactory.GetParser(subtitleType);
			if (parser == null)
			{
				throw new ArgumentOutOfRangeException(nameof(subtitleType), $"subtitleType={subtitleType} is not supported.");
			}
			this.SubtitleType = subtitleType;
			this.Subtitle = subtitle;
			return this;
		}
		public IEnumerable<Sentence> ParseSubtitle()
		{
			if (string.IsNullOrEmpty(this.SubtitleType) || string.IsNullOrEmpty(this.Subtitle))
				return Enumerable.Empty<Sentence>();

			var parser = SubtitleParserFactory.GetParser(this.SubtitleType);
			if (parser == null) return Enumerable.Empty<Sentence>();

			return parser.Parse(this.Subtitle);
		}

		public Episode Hide()
		{
			this.IsVisible = false;
			return this;
		}

		public Episode Show()
		{
			this.IsVisible = true;
			return this;
		}

		public void SoftDelete()
		{
			this.IsDeleted = true;
			this.DeletionTime = DateTime.UtcNow;
		}

		public void NotifyModified()
		{
			this.LastModificationTime = DateTime.UtcNow;
		}

		public class Builder
		{
			private Guid id;
			private int sequenceNumber;
			private string title;
			private Guid albumId;
			private Uri audioUrl;
			private double durationInSecond;
			private string subtitle;
			private string subtitleType;

			public Builder Id(Guid id)
			{
				this.id = id;
				return this;
			}

			public Builder SequenceNumber(int sequenceNumber)
			{
				this.sequenceNumber = sequenceNumber;
				return this;
			}
			public Builder Title(string title)
			{
				this.title = title;
				return this;
			}

			public Builder AlbumId(Guid albumId)
			{
				this.albumId = albumId;
				return this;
			}
			public Builder AudioUrl(Uri audioUrl)
			{
				this.audioUrl = audioUrl;
				return this;
			}
			public Builder DurationInSecond(double durationInSecond)
			{
				this.durationInSecond = durationInSecond;
				return this;
			}
			public Builder Subtitle(string subtitle)
			{
				this.subtitle = subtitle;
				return this;
			}
			public Builder SubtitleType(string subtitleType)
			{
				this.subtitleType = subtitleType;
				return this;
			}

			public Episode Build()
			{
				if (id == Guid.Empty)
				{
					throw new ArgumentOutOfRangeException(nameof(id));
				}
				if (title == null)
				{
					throw new ArgumentNullException(nameof(title));
				}
				if (albumId == Guid.Empty)
				{
					throw new ArgumentOutOfRangeException(nameof(albumId));
				}
				if (audioUrl == null)
				{
					throw new ArgumentNullException(nameof(audioUrl));
				}
				if (durationInSecond <= 0)
				{
					throw new ArgumentOutOfRangeException(nameof(durationInSecond));
				}
				if (subtitle == null)
				{
					throw new ArgumentNullException(nameof(subtitle));
				}
				if (subtitleType == null)
				{
					throw new ArgumentNullException(nameof(subtitleType));
				}

				Episode episode = new Episode()
				{
					Id = id,
					Title = title,
					AlbumId = albumId,
					SequenceNumber = sequenceNumber,
					AudioUrl = audioUrl,
					DurationInSecond = durationInSecond,
					SubtitleType = subtitleType,
					Subtitle = subtitle,
					IsVisible = true,
					CreateTime = DateTime.UtcNow
				};

				return episode;
			}
		}
	}
}
