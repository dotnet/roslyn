// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Roslyn.LanguageServer.Protocol
{
    using System;
    using System.Globalization;
    using Microsoft.CodeAnalysis.LanguageServer;
    using Microsoft.CommonLanguageServerProtocol.Framework;
    using Newtonsoft.Json;
    using Newtonsoft.Json.Linq;

    /// <summary>
    /// TODO: document.
    /// </summary>
    internal class DocumentUriConverter : JsonConverter
    {
        /// <inheritdoc/>
        public override bool CanConvert(Type objectType)
        {
            return true;
        }

        /// <inheritdoc/>
        public override object? ReadJson(JsonReader reader, Type objectType, object? existingValue, JsonSerializer serializer)
        {
            reader = reader ?? throw new ArgumentNullException(nameof(reader));
            if (reader.TokenType == JsonToken.String)
            {
                var token = JToken.ReadFrom(reader);
                var uri = new Uri(token.ToObject<string>());

                return uri;
            }
            else if (reader.TokenType == JsonToken.Null)
            {
                return null;
            }

            throw new JsonSerializationException(string.Format(CultureInfo.InvariantCulture, LanguageServerProtocolResources.DocumentUriSerializationError, reader.Value));
        }

        /// <inheritdoc/>
        public override void WriteJson(JsonWriter writer, object? value, JsonSerializer serializer)
        {
            writer = writer ?? throw new ArgumentNullException(nameof(writer));

            if (value is Uri uri)
            {
                var token = JToken.FromObject(uri.AbsoluteUri);
                token.WriteTo(writer);
            }
            else
            {
                throw new ArgumentException($"{nameof(value)} must be of type {nameof(Uri)}");
            }
        }
    }
}
