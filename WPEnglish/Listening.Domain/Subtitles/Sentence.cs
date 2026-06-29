using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Listening.Domain.Subtitles
{
	public class Sentence
	{
		public TimeSpan StartTime { get; set; }
		public TimeSpan EndTime { get; set; }
		public string Value { get; set; }

		public Sentence(TimeSpan startTime,TimeSpan endTime,string value)
		{
			this.StartTime = startTime;
			this.EndTime = endTime;
			this.Value = value;
		}
	}
}
