using Microsoft.Extensions.Logging;
using Opportunity.LrcParser;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Listening.Domain.Subtitles
{
	public class LrcParser : ISubtitleParser
	{
		private readonly ILogger<LrcParser>? logger;

		public LrcParser(ILogger<LrcParser>? logger = null)
		{
			this.logger = logger;
		}
		public LrcParser():this(null) { }

		public bool Accept(string typeName)
		{
			return typeName.Equals("lrc", StringComparison.OrdinalIgnoreCase);
		}

		public IEnumerable<Sentence> Parse(string subtitle)
		{
			var lyrics = Lyrics.Parse(subtitle);
			if(lyrics.Exceptions.Count > 0)
			{
				logger?.LogError("lrc解析失败，异常数量：{ExceptionCount}", lyrics.Exceptions.Count);
				throw new ApplicationException("lrc解析失败");
			}
			lyrics.Lyrics.PreApplyOffset();
			if(lyrics.Lyrics.Lines.Count ==  0) return new List<Sentence>();
			return FromLrc(lyrics.Lyrics);
		}

		private static Sentence[] FromLrc(Lyrics<Line> lyrics)
		{
			var lines = lyrics.Lines;
			Sentence[] sentences = new Sentence[lines.Count];
			for (int i = 0; i < lines.Count - 1; i++)
			{
				var line = lines[i];
				var nextLine = lines[i + 1];
				Sentence sentence = new Sentence(line.Timestamp.TimeOfDay, nextLine.Timestamp.TimeOfDay, line.Content);
				sentences[i] = sentence;
			}
			//last line
			var lastLine = lines.Last();
			TimeSpan lastLineStartTime = lastLine.Timestamp.TimeOfDay;
			//lrc没有结束时间，就极端假定最后一句耗时1分钟
			TimeSpan lastLineEndTime = lastLineStartTime.Add(TimeSpan.FromMinutes(1));
			var lastSentence = new Sentence(lastLineStartTime, lastLineEndTime, lastLine.Content);
			sentences[sentences.Count() - 1] = lastSentence;

			return sentences;
		}
	}
}
