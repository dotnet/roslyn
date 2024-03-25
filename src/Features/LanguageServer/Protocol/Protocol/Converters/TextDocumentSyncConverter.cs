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
    /// Converter which offers custom serialization for <see cref="TextDocumentSyncKind"/> enum to a <see cref="TextDocumentSyncOptions"/> object.
    /// </summary>
    /// <remarks>
    /// This is to support backwards compatibility for the protocol.
    /// </remarks>
    internal class TextDocumentSyncConverter : JsonConverter
    {
        /// <inheritdoc/>
        public override bool CanConvert(Type objectType)
        {
            return true;
        }

        /// <summary>
        /// Deserializes a json value to a <see cref="TextDocumentSyncOptions"/> object.
        /// </summary>
        /// <param name="reader">Reader from which to read json value.</param>
        /// <param name="objectType">Type of the json value.</param>
        /// <param name="existingValue">Existing value.</param>
        /// <param name="serializer">Default serializer.</param>
        /// <returns>A <see cref="TextDocumentSyncOptions"/> which matches the json value.</returns>
        public override object? ReadJson(JsonReader reader, Type objectType, object? existingValue, JsonSerializer serializer)
        {
            reader = reader ?? throw new ArgumentNullException(nameof(reader));
            if (reader.TokenType is JsonToken.Float or JsonToken.Integer)
            {
                // This conversion is modeled after what VS Code does, see https://github.com/Microsoft/vscode-languageserver-node/blob/master/client/src/client.ts#L1234
                var textDocSync = new TextDocumentSyncOptions
                {
                    OpenClose = true,
                    Change = (TextDocumentSyncKind)int.Parse(reader.Value!.ToString(), NumberStyles.Integer, CultureInfo.CurrentCulture),
                    Save = new SaveOptions
                    {
                        IncludeText = false,
                    },
                };

                return textDocSync;
            }
            else if (reader.TokenType == JsonToken.String)
            {
                return JsonConvert.DeserializeObject<TextDocumentSyncOptions>(reader.Value!.ToString());
            }
            else if (reader.TokenType == JsonToken.StartObject)
            {
                var token = JToken.ReadFrom(reader);
                return token.ToObject<TextDocumentSyncOptions>();
            }
            else if (reader.TokenType == JsonToken.Null)
            {
                // This conversion is modeled after what VS Code does, see https://github.com/Microsoft/vscode-languageserver-node/blob/master/client/src/client.ts#L1234
                var textDocSync = new TextDocumentSyncOptions
                {
                    OpenClose = true,
                    Change = TextDocumentSyncKind.None,
                    Save = new SaveOptions
                    {
                        IncludeText = false,
                    },
                };

                return textDocSync;
            }

            throw new JsonSerializationException(string.Format(CultureInfo.InvariantCulture, LanguageServerProtocolResources.TextDocumentSyncSerializationError, reader.Value));
        }

        /// <inheritdoc/>
        public override void WriteJson(JsonWriter writer, object? value, JsonSerializer serializer)
        {
            writer = writer ?? throw new ArgumentNullException(nameof(writer));

            if (value is null)
            {
                writer.WriteNull();
            }
            else
            {
                writer = writer ?? throw new ArgumentNullException(nameof(writer));

                var token = JToken.FromObject(value);
                token.WriteTo(writer);
            }
        }
    }
}
