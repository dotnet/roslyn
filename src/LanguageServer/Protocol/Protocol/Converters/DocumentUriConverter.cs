// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Roslyn.LanguageServer.Protocol;

/// <summary>
/// Converts the LSP spec URI string into our custom wrapper for URI strings.
/// We do not convert directly to <see cref="System.Uri"/> as it is unable to handle
/// certain valid RFC spec URIs.  We do not want serialization / deserialization to fail if we cannot parse the URI.
/// See https://github.com/dotnet/runtime/issues/64707
/// </summary>
internal sealed class DocumentUriConverter : JsonConverter<DocumentUri>
{
    public override DocumentUri Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    => new(reader.GetString());

    public override void Write(Utf8JsonWriter writer, DocumentUri value, JsonSerializerOptions options)
        => writer.WriteStringValue(value.UriString);
}
