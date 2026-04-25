// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Microsoft.CodeAnalysis.Razor.Protocol.Debugging;

internal class RazorProximityExpressionsResponse
{
    [JsonPropertyName("expressions")]
    public required IReadOnlyList<string> Expressions { get; init; }
}
