// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable

using System.Collections.Generic;
using Microsoft.CodeAnalysis.Host.Mef;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.SolutionCrawler
{
    internal class PerLanguageIncrementalAnalyzerProviderMetadata : LanguageMetadata
    {
        public string? Name { get; }

        public PerLanguageIncrementalAnalyzerProviderMetadata(IDictionary<string, object> data)
            : base(data)
        {
            Name = (string?)data.GetValueOrDefault("Name");
        }
    }
}
