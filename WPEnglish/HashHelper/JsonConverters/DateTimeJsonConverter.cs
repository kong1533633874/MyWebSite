using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace Commons.JsonConverters
{
	public class DateTimeJsonConverter : JsonConverter<DateTime>
	{
		private readonly string _format;
		public DateTimeJsonConverter() : this("yyyy-MM-ddTHH:mm:ssZ")
		{

		}

		public DateTimeJsonConverter(string format)
		{
			_format = format;
		}
		public override DateTime Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
		{
			string? str = reader.GetString();
			if (string.IsNullOrEmpty(str))
			{
				return default(DateTime);
			}
			if(DateTime.TryParse(str,CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out var dt))
			{
				return dt;
			}

			return reader.GetDateTime();
		}

		public override void Write(Utf8JsonWriter writer, DateTime value, JsonSerializerOptions options)
		{
			writer.WriteStringValue(value.ToString(_format));
		}
	}
}
