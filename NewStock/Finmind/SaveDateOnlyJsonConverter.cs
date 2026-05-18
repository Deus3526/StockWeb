using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace NewStock.Finmind;

/// <summary>
/// 將 JSON 字串以 DateOnly.TryParse 解析；JSON null、空白或解析失敗時為 DateOnly.MinValue。
/// </summary>
public sealed class SaveDateOnlyJsonConverter : JsonConverter<DateOnly>
{
    public override DateOnly Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType == JsonTokenType.Null)
            return DateOnly.MinValue;

        if (reader.TokenType != JsonTokenType.String)
            throw new JsonException($"Unexpected JSON token ({reader.TokenType}) when parsing DateOnly.");

        var s = reader.GetString()?.Trim();
        if (string.IsNullOrEmpty(s))
            return DateOnly.MinValue;

        return DateOnly.TryParse(s, CultureInfo.InvariantCulture, DateTimeStyles.None, out var d)
            ? d
            : DateOnly.MinValue;
    }

    public override void Write(Utf8JsonWriter writer, DateOnly value, JsonSerializerOptions options)
    {
        if (value == DateOnly.MinValue)
        {
            writer.WriteNullValue();
            return;
        }

        writer.WriteStringValue(value.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture));
    }
}
