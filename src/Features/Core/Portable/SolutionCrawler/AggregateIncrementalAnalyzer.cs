// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Options;

#if NETSTANDARD2_0
using Roslyn.Utilities;
#endif

namespace Microsoft.CodeAnalysis.SolutionCrawler
{
    internal class AggregateIncrementalAnalyzer : IIncrementalAnalyzer
    {
        public readonly ImmutableDictionary<string, Lazy<IIncrementalAnalyzer>> Analyzers;

        public AggregateIncrementalAnalyzer(Workspace workspace, IncrementalAnalyzerProviderBase owner, List<Lazy<IPerLanguageIncrementalAnalyzerProvider, PerLanguageIncrementalAnalyzerProviderMetadata>> providers)
        {
            Analyzers = providers.ToImmutableDictionary(
                p => p.Metadata.Language, p => new Lazy<IIncrementalAnalyzer>(() => p.Value.CreatePerLanguageIncrementalAnalyzer(workspace, owner), isThreadSafe: true));
        }

        public async Task NewSolutionSnapshotAsync(Solution solution, CancellationToken cancellationToken)
        {
            foreach (var (_, analyzer) in Analyzers)
            {
                if (analyzer.IsValueCreated)
                {
                    await analyzer.Value.NewSolutionSnapshotAsync(solution, cancellationToken).ConfigureAwait(false);
                }
            }
        }

        public async Task DocumentOpenAsync(Document document, CancellationToken cancellationToken)
        {
            if (TryGetAnalyzer(document.Project, out var analyzer))
            {
                await analyzer.DocumentOpenAsync(document, cancellationToken).ConfigureAwait(false);
            }
        }

        public async Task DocumentResetAsync(Document document, CancellationToken cancellationToken)
        {
            if (TryGetAnalyzer(document.Project, out var analyzer))
            {
                await analyzer.DocumentResetAsync(document, cancellationToken).ConfigureAwait(false);
            }
        }

        public async Task DocumentCloseAsync(Document document, CancellationToken cancellationToken)
        {
            if (TryGetAnalyzer(document.Project, out var analyzer))
            {
                await analyzer.DocumentCloseAsync(document, cancellationToken).ConfigureAwait(false);
            }
        }

        public async Task ActiveDocumentSwitchedAsync(TextDocument document, CancellationToken cancellationToken)
        {
            if (TryGetAnalyzer(document.Project, out var analyzer))
            {
                await analyzer.ActiveDocumentSwitchedAsync(document, cancellationToken).ConfigureAwait(false);
            }
        }

        public bool NeedsReanalysisOnOptionChanged(object sender, OptionChangedEventArgs e)
        {
            // TODO: Is this correct?
            return false;
        }

        public async Task AnalyzeSyntaxAsync(Document document, InvocationReasons reasons, CancellationToken cancellationToken)
        {
            if (TryGetAnalyzer(document.Project, out var analyzer))
            {
                await analyzer.AnalyzeSyntaxAsync(document, reasons, cancellationToken).ConfigureAwait(false);
            }
        }

        public async Task AnalyzeDocumentAsync(Document document, SyntaxNode bodyOpt, InvocationReasons reasons, CancellationToken cancellationToken)
        {
            if (TryGetAnalyzer(document.Project, out var analyzer))
            {
                await analyzer.AnalyzeDocumentAsync(document, bodyOpt, reasons, cancellationToken).ConfigureAwait(false);
            }
        }

        public async Task AnalyzeProjectAsync(Project project, bool semanticsChanged, InvocationReasons reasons, CancellationToken cancellationToken)
        {
            if (TryGetAnalyzer(project, out var analyzer))
            {
                await analyzer.AnalyzeProjectAsync(project, semanticsChanged, reasons, cancellationToken).ConfigureAwait(false);
            }
        }

        private bool TryGetAnalyzer(Project project, [NotNullWhen(true)] out IIncrementalAnalyzer? analyzer)
        {
            if (!Analyzers.TryGetValue(project.Language, out var lazyAnalyzer))
            {
                analyzer = null;
                return false;
            }

            analyzer = lazyAnalyzer.Value;
            return true;
        }

        public async Task RemoveDocumentAsync(DocumentId documentId, CancellationToken cancellationToken)
        {
            foreach (var (_, analyzer) in Analyzers)
            {
                if (analyzer.IsValueCreated)
                {
                    await analyzer.Value.RemoveDocumentAsync(documentId, cancellationToken).ConfigureAwait(false);
                }
            }
        }

        public async Task RemoveProjectAsync(ProjectId projectId, CancellationToken cancellationToken)
        {
            foreach (var (_, analyzer) in Analyzers)
            {
                if (analyzer.IsValueCreated)
                {
                    await analyzer.Value.RemoveProjectAsync(projectId, cancellationToken).ConfigureAwait(false);
                }
            }
        }

        public async Task NonSourceDocumentOpenAsync(TextDocument textDocument, CancellationToken cancellationToken)
        {
            if (TryGetAnalyzer(textDocument.Project, out var analyzer))
            {
                await analyzer.NonSourceDocumentOpenAsync(textDocument, cancellationToken).ConfigureAwait(false);
            }
        }

        public async Task NonSourceDocumentCloseAsync(TextDocument textDocument, CancellationToken cancellationToken)
        {
            if (TryGetAnalyzer(textDocument.Project, out var analyzer))
            {
                await analyzer.NonSourceDocumentCloseAsync(textDocument, cancellationToken).ConfigureAwait(false);
            }
        }

        public async Task NonSourceDocumentResetAsync(TextDocument textDocument, CancellationToken cancellationToken)
        {
            if (TryGetAnalyzer(textDocument.Project, out var analyzer))
            {
                await analyzer.NonSourceDocumentResetAsync(textDocument, cancellationToken).ConfigureAwait(false);
            }
        }

        public async Task AnalyzeNonSourceDocumentAsync(TextDocument textDocument, InvocationReasons reasons, CancellationToken cancellationToken)
        {
            if (TryGetAnalyzer(textDocument.Project, out var analyzer))
            {
                await analyzer.AnalyzeNonSourceDocumentAsync(textDocument, reasons, cancellationToken).ConfigureAwait(false);
            }
        }

        public void LogAnalyzerCountSummary()
        {
        }

        public int Priority => 1;
    }
}
