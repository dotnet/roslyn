// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Composition;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.SolutionCrawler;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.DiagnosticCache
{
    [ExportIncrementalAnalyzerProvider(nameof(CachedDiagnosticIncrementalAnalyzerProvider), new[] { WorkspaceKind.Host }), Shared]
    internal sealed class CachedDiagnosticIncrementalAnalyzerProvider : IIncrementalAnalyzerProvider
    {
        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public CachedDiagnosticIncrementalAnalyzerProvider()
        {
        }

        public IIncrementalAnalyzer? CreateIncrementalAnalyzer(Workspace workspace)
        {
            if (workspace is not VisualStudioWorkspace)
                return null;

            if (workspace.Services.GetService<IDiagnosticCacheService>() is not VisualStudioDiagnosticCacheService service)
                return null;

            return new CachedDiagnosticIncrementalAnalyzer(service);
        }

        private class CachedDiagnosticIncrementalAnalyzer : IncrementalAnalyzerBase
        {
            private VisualStudioDiagnosticCacheService Service { get; }

            public CachedDiagnosticIncrementalAnalyzer(VisualStudioDiagnosticCacheService service)
            {
                Service = service;
            }

            public override Task AnalyzeDocumentAsync(Document document, SyntaxNode bodyOpt, InvocationReasons reasons, CancellationToken cancellationToken)
            {
                // Live analysis started
                Service.OnAnalyzeDocument(document);
                return Task.CompletedTask;
            }
        }
    }
}
