using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Listening.Domain.Subtitles
{
	public class SrtParser : ISubtitleParser
	{
		public bool Accept(string typeName)
		{
			return typeName.Equals("srt", StringComparison.OrdinalIgnoreCase) || typeName.Equals("vtt", StringComparison.OrdinalIgnoreCase);
		}

		public IEnumerable<Sentence> Parse(string subtitle)
		{
			var srtParser = new SubtitlesParser.Classes.Parsers.SubParser();
			using (MemoryStream ms = new MemoryStream(Encoding.UTF8.GetBytes(subtitle)))
			{
				var items = srtParser.ParseStream(ms);
				return items.Select(s => new Sentence(TimeSpan.FromMilliseconds(s.StartTime),
					TimeSpan.FromMilliseconds(s.EndTime), String.Join(" ", s.Lines)));
			}
		}
	}
}
