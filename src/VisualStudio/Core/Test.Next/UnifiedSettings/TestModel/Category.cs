﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Text.Json.Serialization;

namespace Roslyn.VisualStudio.Next.UnitTests.UnifiedSettings.TestModel;

internal record Category
{
    [JsonPropertyName("title")]
    [JsonConverter(typeof(ResourceStringConverter))]
    public required string Title { get; init; }

    [JsonPropertyName("legacyOptionPageId")]
    public string? LegacyOptionPageId { get; init; }
}
