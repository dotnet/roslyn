// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.Unicode;

namespace Microsoft.CodeAnalysis.MSBuild;

internal static class JsonSettings
{
    public static readonly Encoding StreamEncoding = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false);

    public static readonly JsonSerializerOptions SingleLineSerializerOptions = new()
    {
        // We use nulls for optional things, so doesn't matter
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,

        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,

        // We escape all non-ASCII characters. Because we're writing to stdin/stdout, on Windows codepages could be set that might interfere with Unicode, especially on build machines.
        // By escaping all non-ASCII it means the JSON stream itself is ASCII and thus can't be impacted by codepage issues.
        // JavaScriptEncoder.Create(UnicodeRanges.BasicLatin) only allows BasicLatin (ASCII 0x00-0x7F) to be unescaped.
        Encoder = JavaScriptEncoder.Create(UnicodeRanges.BasicLatin),

        // Ensure WriteIndented is false (which is the default) so each message is serialized to its own line
        WriteIndented = false
    };
}
