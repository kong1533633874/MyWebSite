using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Listening.Domain.Subtitles
{
	public interface ISubtitleParser
	{
		public bool Accept(string typeName);
		public IEnumerable<Sentence> Parse(string subtitle);
	}
}
