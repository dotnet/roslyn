// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Composition;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.SolutionCrawler;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.DesignerAttribute
{
    [ExportIncrementalAnalyzerProvider(Name, new[] { WorkspaceKind.Host }), Shared]
    internal class DesignerAttributeIncrementalAnalyzerProvider : IncrementalAnalyzerProviderBase
    {
        public const string Name = "DesignerAttributeIncrementalAnalyzerProvider";

        [ImportingConstructor]
        public DesignerAttributeIncrementalAnalyzerProvider(
            [ImportMany] IEnumerable<Lazy<IPerLanguageIncrementalAnalyzerProvider, PerLanguageIncrementalAnalyzerProviderMetadata>> perLanguageProviders) :
            base(Name, perLanguageProviders)
        {
        }
    }
}
