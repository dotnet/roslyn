// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Text.Json.Serialization;

namespace Roslyn.VisualStudio.Next.UnitTests.UnifiedSettings.TestModel;

internal record UnifiedSettingsOption<T> : UnifiedSettingBase
{
    [JsonPropertyName("Default")]
    public required T Default { get; init; }

    [JsonPropertyName("AlternativeDefault")]
    public AlternativeDefault<T>? AlternativeDefault { get; init; }
}
