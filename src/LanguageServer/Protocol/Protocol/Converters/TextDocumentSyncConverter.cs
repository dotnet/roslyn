// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.CodeAnalysis.LanguageServer;

namespace Roslyn.LanguageServer.Protocol;

internal sealed class TextDocumentSyncConverter : JsonConverter<TextDocumentSyncOptions>
{
    public override TextDocumentSyncOptions Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType == JsonTokenType.Number)
        {
            // This conversion is modeled after what VS Code does, see https://github.com/microsoft/vscode-languageserver-node/blob/21f4f0af6bf1623483c3e65f36e550bfdb6245a6/client/src/common/client.ts#L1248
            var textDocSync = new TextDocumentSyncOptions
            {
                OpenClose = true,
                Change = (TextDocumentSyncKind)reader.GetInt32(),
                Save = new SaveOptions
                {
                    IncludeText = false,
                },
            };

            return textDocSync;
        }
        else if (reader.TokenType == JsonTokenType.String)
        {
            var value = reader.GetString();
            return JsonSerializer.Deserialize<TextDocumentSyncOptions>(value);
        }
        else if (reader.TokenType == JsonTokenType.StartObject)
        {
            return JsonSerializer.Deserialize<TextDocumentSyncOptions>(ref reader, options);
        }

        throw new JsonException(string.Format(CultureInfo.InvariantCulture, LanguageServerProtocolResources.TextDocumentSyncSerializationError, reader.GetString()));
    }

    /// <inheritdoc/>
    public override void Write(Utf8JsonWriter writer, TextDocumentSyncOptions value, JsonSerializerOptions options)
    {
        JsonSerializer.Serialize(writer, value, options);
    }
}
