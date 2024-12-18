// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Linq;
using System.Text.Json.Serialization;
using Microsoft.CodeAnalysis.Options;
using Roslyn.Utilities;
using Xunit;

namespace Roslyn.VisualStudio.Next.UnitTests.UnifiedSettings.TestModel
{
    internal abstract partial record UnifiedSettingBase()
    {
        [JsonPropertyName("title")]
        [JsonConverter(typeof(ResourceConverter))]
        public required string Title { get; init; }

        [JsonPropertyName("type")]
        public required string Type { get; init; }

        [JsonPropertyName("order")]
        public required int Order { get; init; }

        [JsonPropertyName("enableWhen")]
        public string? EnableWhen { get; init; }

        [JsonPropertyName("migration")]
        public required Migration Migration { get; init; }
    }
}
