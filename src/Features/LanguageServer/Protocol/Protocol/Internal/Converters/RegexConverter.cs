// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Roslyn.LanguageServer.Protocol
{
    using System;
    using System.Text.RegularExpressions;

    using Newtonsoft.Json;

    /// <summary>
    /// Similar to https://devdiv.visualstudio.com/DevDiv/_git/VS-Platform?path=/src/Productivity/TextMate/Core/LanguageConfiguration/Impl/FastRegexConverter.cs
    /// to allow us to only compile the regex option once.
    /// </summary>
    internal class RegexConverter : JsonConverter
    {
        public override bool CanConvert(Type objectType)
        {
            // nameof is faster than typeof, so below is a fast path.
            return objectType.Name == nameof(Regex) && objectType == typeof(Regex);
        }

        public override object? ReadJson(JsonReader reader, Type objectType, object? existingValue, JsonSerializer serializer)
        {
            // Create a custom deserializer for regex as the default provided by newtonsoft doesn't
            // specify the Compiled option.
            var regexText = reader.Value as string;
            if (string.IsNullOrEmpty(regexText))
            {
                return null;
            }

            return new Regex(regexText, RegexOptions.Compiled | RegexOptions.ECMAScript, matchTimeout: TimeSpan.FromMilliseconds(1000));
        }

        public override void WriteJson(JsonWriter writer, object? value, JsonSerializer serializer)
        {
            if (value is Regex valueAsRegex)
            {
                writer.WriteValue(valueAsRegex.ToString());
            }
            else
            {
                throw new ArgumentException($"{nameof(value)} must be of type {nameof(Regex)}");
            }
        }
    }
}
