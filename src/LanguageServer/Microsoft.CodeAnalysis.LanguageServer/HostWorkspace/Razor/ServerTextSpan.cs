// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Text.Json.Serialization;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.LanguageServer.HostWorkspace.Razor;

internal class ServerTextSpan
{
    [JsonPropertyName("start")]
    public int Start { get; set; }

    [JsonPropertyName("length")]
    public int Length { get; set; }

    public TextSpan ToTextSpan()
        => new(Start, Length);
}
