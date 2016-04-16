// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Host.Mef;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.SolutionCrawler
{
    internal class PerLanguageIncrementalAnalyzerProviderMetadata : LanguageMetadata
    {
        public string Name { get; }

        public PerLanguageIncrementalAnalyzerProviderMetadata(IDictionary<string, object> data)
            : base(data)
        {
            this.Name = (string)data.GetValueOrDefault("Name");
        }
    }
}
