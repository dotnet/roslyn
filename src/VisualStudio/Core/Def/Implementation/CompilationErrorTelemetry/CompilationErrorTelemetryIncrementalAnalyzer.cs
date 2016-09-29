// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Composition;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.SolutionCrawler;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.CompilationErrorTelemetry
{
    // Disabled compilation error telemetry per discussion with .net team.
    // tracking bug - https://github.com/dotnet/roslyn/issues/11133
    // [ExportIncrementalAnalyzerProvider(WorkspaceKind.Host), Shared]
    internal class CompilationErrorTelemetryIncrementalAnalyzer : IncrementalAnalyzerProviderBase
    {
        public const string Name = "CompilationErrorTelemetryIncrementalAnalyzer";

        [ImportingConstructor]
        public CompilationErrorTelemetryIncrementalAnalyzer(
            [ImportMany] IEnumerable<Lazy<IPerLanguageIncrementalAnalyzerProvider, PerLanguageIncrementalAnalyzerProviderMetadata>> perLanguageProviders) :
            base(Name, perLanguageProviders)
        {
        }
    }
}
