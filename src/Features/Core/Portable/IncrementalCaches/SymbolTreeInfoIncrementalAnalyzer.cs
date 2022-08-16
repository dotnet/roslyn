// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.FindSymbols;
using Microsoft.CodeAnalysis.FindSymbols.SymbolTree;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Shared.Collections;
using Microsoft.CodeAnalysis.SolutionCrawler;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.IncrementalCaches
{
    internal partial class SymbolTreeInfoIncrementalAnalyzerProvider
    {
        private class SymbolTreeInfoIncrementalAnalyzer : IncrementalAnalyzerBase
        {
            private readonly SymbolTreeInfoCacheService _cacheService;

            public SymbolTreeInfoIncrementalAnalyzer(SymbolTreeInfoCacheService cacheService)
            {
                _cacheService = cacheService;
            }

            private static bool SupportAnalysis(Project project)
                => project.SupportsCompilation;

            public override Task AnalyzeDocumentAsync(Document document, SyntaxNode bodyOpt, InvocationReasons reasons, CancellationToken cancellationToken)
            {
                if (!SupportAnalysis(document.Project))
                    return Task.CompletedTask;

                return _cacheService.AnalyzeDocumentAsync(document, bodyOpt, cancellationToken);
            }

            public override Task AnalyzeProjectAsync(Project project, bool semanticsChanged, InvocationReasons reasons, CancellationToken cancellationToken)
            {
                if (!SupportAnalysis(project))
                    return Task.CompletedTask;

                return _cacheService.UpdateSymbolTreeInfoAsync(project, cancellationToken);
            }

            public override Task RemoveProjectAsync(ProjectId projectId, CancellationToken cancellationToken)
            {
                _cacheService.RemoveProject(projectId);

                return Task.CompletedTask;
            }
        }
    }
}
