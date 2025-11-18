// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Text.Json.Serialization;

namespace Roslyn.VisualStudio.Next.UnitTests.UnifiedSettings.TestModel;

internal sealed record Migration
{
    [JsonPropertyName("pass")]
    public Pass? Pass { get; init; }

    [JsonPropertyName("enumIntegerToString")]
    public EnumIntegerToString? EnumIntegerToString { get; init; }
}

