using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Listening.Domain.Subtitles
{
	public static class SubtitleParserFactory
	{
		private static List<ISubtitleParser> SubtitleParsers = new List<ISubtitleParser>();
		static SubtitleParserFactory()
		{
			var parserTypes = typeof(SubtitleParserFactory).Assembly.GetTypes().Where(s => typeof(ISubtitleParser).IsAssignableFrom(s) && !s.IsAbstract);
			foreach (var type in parserTypes)
			{
				SubtitleParsers.Add((ISubtitleParser)Activator.CreateInstance(type));
			}
		}

		public static ISubtitleParser? GetParser(string SubtitleType, ILogger? logger = null)
		{ 
			foreach (var parser in SubtitleParsers)
			{
				if (parser.Accept(SubtitleType))
				{
					return parser;
				}
			}
			logger?.LogWarning("未找到支持 {SubtitleType} 类型的字幕解析器", SubtitleType);
			return null;
		}
	}
}
