// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Text.Json.Serialization;

namespace Roslyn.VisualStudio.Next.UnitTests.UnifiedSettings.TestModel;

internal sealed record Map
{
    [JsonPropertyName("result")]
    public required string Result { get; init; }

    [JsonPropertyName("match")]
    public required int Match { get; init; }
}
