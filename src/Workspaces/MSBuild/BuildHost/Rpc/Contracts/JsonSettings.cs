// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Text;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace Microsoft.CodeAnalysis.MSBuild;

internal static class JsonSettings
{
    public static readonly Encoding StreamEncoding = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false);

    public static readonly JsonSerializerSettings SingleLineSerializerSettings = new JsonSerializerSettings
    {
        // Setting Formatting.None ensures each is serialized to it's own line, which we implicitly rely on
        Formatting = Newtonsoft.Json.Formatting.None,

        // We use nulls for optional things, so doesn't matter
        NullValueHandling = NullValueHandling.Ignore,

        ContractResolver = new CamelCasePropertyNamesContractResolver(),

        // We escape all non-ASCII characters. Because we're writing to stdin/stdout, on Windows codepages could be set that might interfere with Unicode, especially on build machines.
        // By escaping all non-ASCII it means the JOSN stream itself is ASCII and thus can't be impacted by codepage issues.
        StringEscapeHandling = StringEscapeHandling.EscapeNonAscii
    };
}
