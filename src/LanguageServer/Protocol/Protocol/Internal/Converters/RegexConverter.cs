// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;

namespace Roslyn.LanguageServer.Protocol;

/// <summary>
/// Similar to https://devdiv.visualstudio.com/DevDiv/_git/VS-Platform?path=/src/Productivity/TextMate/Core/LanguageConfiguration/Impl/FastRegexConverter.cs
/// to allow us to only compile the regex option once.
/// </summary>
internal class RegexConverter : JsonConverter<Regex>
{
    public override Regex? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        return new Regex(reader.GetString(), RegexOptions.Compiled | RegexOptions.ECMAScript, matchTimeout: TimeSpan.FromMilliseconds(1000));
    }

    public override void Write(Utf8JsonWriter writer, Regex value, JsonSerializerOptions options)
    {
        writer.WriteStringValue(value.ToString());
    }
}
