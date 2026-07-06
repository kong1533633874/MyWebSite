using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace Commons.JsonConverters
{
	public class NullableDateTimeJsonConverter : JsonConverter<DateTime?>
	{
		private readonly DateTimeJsonConverter _inner;
		public NullableDateTimeJsonConverter() : this("yyyy-MM-ddTHH:mm:ssZ") { }
		public NullableDateTimeJsonConverter(string format) => _inner = new DateTimeJsonConverter(format);

		public override DateTime? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
		{
			if (reader.TokenType == JsonTokenType.Null) return null;
			return _inner.Read(ref reader, typeof(DateTime), options);
		}

		public override void Write(Utf8JsonWriter writer, DateTime? value, JsonSerializerOptions options)
		{
			if (value == null) { writer.WriteNullValue(); return; }
			_inner.Write(writer, value.Value, options);
		}
	}
}
