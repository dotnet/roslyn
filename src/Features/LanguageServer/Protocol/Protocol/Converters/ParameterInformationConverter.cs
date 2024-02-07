// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Roslyn.LanguageServer.Protocol
{
    using System;
    using Newtonsoft.Json;
    using Newtonsoft.Json.Linq;

    /// <summary>
    /// JsonConverter to correctly deserialize int arrays in the Label param of ParameterInformation.
    /// </summary>
    internal class ParameterInformationConverter : JsonConverter
    {
        /// <inheritdoc/>
        public override bool CanWrite => false;

        /// <inheritdoc/>
        public override bool CanConvert(Type objectType)
        {
            return true;
        }

        /// <inheritdoc/>
        public override object? ReadJson(JsonReader reader, Type objectType, object? existingValue, JsonSerializer serializer)
        {
            var token = JToken.Load(reader);

            var label = ((JObject)token).Property("label", StringComparison.Ordinal);
            var documentation = ((JObject)token).Property("documentation", StringComparison.Ordinal);

            var parameter = new ParameterInformation();

            if (label != null)
            {
                var value = label.Value;
                if (value is JArray arr)
                {
                    var tuple = new Tuple<int, int>(arr[0].Value<int>(), arr[1].Value<int>());
                    parameter.Label = tuple;
                }
                else
                {
                    // If label is not an array we can serialize it normally
                    parameter.Label = value.ToObject<SumType<string, Tuple<int, int>>>();
                }
            }

            if (documentation != null)
            {
                var value = documentation.Value;
                parameter.Documentation = value.ToObject<SumType<string, MarkupContent>?>();
            }

            return parameter;
        }

        /// <inheritdoc/>
        public override void WriteJson(JsonWriter writer, object? value, JsonSerializer serializer)
        {
            throw new NotImplementedException();
        }
    }
}
