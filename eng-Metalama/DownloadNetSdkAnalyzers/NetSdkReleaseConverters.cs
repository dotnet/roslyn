using NuGet.Versioning;
using System.Text.Json.Serialization;
using System.Text.Json;

namespace DownloadNetSdkAnalyzers;

class SemanticVersionConverter : JsonConverter<SemanticVersion>
{
    public override SemanticVersion? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        => SemanticVersion.Parse(reader.GetString()!);

    public override void Write(Utf8JsonWriter writer, SemanticVersion value, JsonSerializerOptions options)
        => writer.WriteStringValue(value.ToString());
}

class SemanticVersionDictionaryKeyConverterFactory : JsonConverterFactory
{
    public override bool CanConvert(Type typeToConvert)
    {
        return typeToConvert.IsGenericType
            && typeToConvert.GetGenericTypeDefinition() == typeof(SortedDictionary<,>)
            && typeToConvert.GenericTypeArguments[0] == typeof(SemanticVersion);
    }

    public override JsonConverter? CreateConverter(Type typeToConvert, JsonSerializerOptions options)
        => (JsonConverter)Activator.CreateInstance(typeof(Converter<>).MakeGenericType(typeToConvert.GenericTypeArguments[1]))!;

    class Converter<TValue> : JsonConverter<SortedDictionary<SemanticVersion, TValue>>
    {
        public override SortedDictionary<SemanticVersion, TValue>? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            var stringDictionaryType = typeof(SortedDictionary<string, TValue>);
            var dictionaryConverter = (JsonConverter<SortedDictionary<string, TValue>>)options.GetConverter(stringDictionaryType);

            var stringDictionary = dictionaryConverter.Read(ref reader, stringDictionaryType, options);

            var result = new SortedDictionary<SemanticVersion, TValue>();

            foreach (var kvp in stringDictionary!)
            {
                result.Add(SemanticVersion.Parse(kvp.Key), kvp.Value);
            }

            return result;
        }

        public override void Write(Utf8JsonWriter writer, SortedDictionary<SemanticVersion, TValue> value, JsonSerializerOptions options)
        {
            var stringDictionaryType = typeof(SortedDictionary<string, TValue>);
            var dictionaryConverter = (JsonConverter<SortedDictionary<string, TValue>>)options.GetConverter(stringDictionaryType);

            var stringDictionary = new SortedDictionary<string, TValue>();

            foreach (var kvp in value)
            {
                stringDictionary.Add(kvp.Key.ToString(), kvp.Value);
            }

            dictionaryConverter.Write(writer, stringDictionary, options);
        }
    }
}
