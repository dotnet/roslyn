// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Roslyn.LanguageServer.Protocol;

/// <summary>
/// TODO: document.
/// </summary>
internal class DocumentUriConverter : JsonConverter<Uri>
{
    public override Uri Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    => new(reader.GetString());

    public override void Write(Utf8JsonWriter writer, Uri value, JsonSerializerOptions options)
        => writer.WriteStringValue(value.AbsoluteUri);
}
