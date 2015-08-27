// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Composition;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Options;

namespace Microsoft.CodeAnalysis.SolutionCrawler
{
    internal class AggregateIncrementalAnalyzer : IIncrementalAnalyzer
    {
        public readonly ImmutableDictionary<string, Lazy<IIncrementalAnalyzer>> Analyzers;

        public AggregateIncrementalAnalyzer(Workspace workspace, IncrementalAnalyzerProviderBase owner, List<Lazy<IPerLanguageIncrementalAnalyzerProvider, PerLanguageIncrementalAnalyzerProviderMetadata>> providers)
        {
            this.Analyzers = providers.ToImmutableDictionary(
                p => p.Metadata.Language, p => new Lazy<IIncrementalAnalyzer>(() => p.Value.CreatePerLanguageIncrementalAnalyzer(workspace, owner), isThreadSafe: true));
        }

        public async Task NewSolutionSnapshotAsync(Solution solution, CancellationToken cancellationToken)
        {
            foreach (var analyzer in this.Analyzers.Values)
            {
                if (analyzer.IsValueCreated)
                {
                    await analyzer.Value.NewSolutionSnapshotAsync(solution, cancellationToken).ConfigureAwait(false);
                }
            }
        }

        public async Task DocumentOpenAsync(Document document, CancellationToken cancellationToken)
        {
            IIncrementalAnalyzer analyzer;
            if (TryGetAnalyzer(document.Project, out analyzer))
            {
                await analyzer.DocumentOpenAsync(document, cancellationToken).ConfigureAwait(false);
            }
        }

        public async Task DocumentResetAsync(Document document, CancellationToken cancellationToken)
        {
            IIncrementalAnalyzer analyzer;
            if (TryGetAnalyzer(document.Project, out analyzer))
            {
                await analyzer.DocumentResetAsync(document, cancellationToken).ConfigureAwait(false);
            }
        }

        public async Task DocumentCloseAsync(Document document, CancellationToken cancellationToken)
        {
            IIncrementalAnalyzer analyzer;
            if (TryGetAnalyzer(document.Project, out analyzer))
            {
                await analyzer.DocumentCloseAsync(document, cancellationToken).ConfigureAwait(false);
            }
        }

        public bool NeedsReanalysisOnOptionChanged(object sender, OptionChangedEventArgs e)
        {
            // TODO: Is this correct?
            return false;
        }

        public async Task AnalyzeSyntaxAsync(Document document, CancellationToken cancellationToken)
        {
            IIncrementalAnalyzer analyzer;
            if (TryGetAnalyzer(document.Project, out analyzer))
            {
                await analyzer.AnalyzeSyntaxAsync(document, cancellationToken).ConfigureAwait(false);
            }
        }

        public async Task AnalyzeDocumentAsync(Document document, SyntaxNode bodyOpt, CancellationToken cancellationToken)
        {
            IIncrementalAnalyzer analyzer;
            if (TryGetAnalyzer(document.Project, out analyzer))
            {
                await analyzer.AnalyzeDocumentAsync(document, bodyOpt, cancellationToken).ConfigureAwait(false);
            }
        }

        public async Task AnalyzeProjectAsync(Project project, bool semanticsChanged, CancellationToken cancellationToken)
        {
            IIncrementalAnalyzer analyzer;
            if (TryGetAnalyzer(project, out analyzer))
            {
                await analyzer.AnalyzeProjectAsync(project, semanticsChanged, cancellationToken).ConfigureAwait(false);
            }
        }

        private bool TryGetAnalyzer(Project project, out IIncrementalAnalyzer analyzer)
        {
            Lazy<IIncrementalAnalyzer> lazyAnalyzer;
            if (!this.Analyzers.TryGetValue(project.Language, out lazyAnalyzer))
            {
                analyzer = null;
                return false;
            }

            analyzer = lazyAnalyzer.Value;
            return true;
        }

        public void RemoveDocument(DocumentId documentId)
        {
            foreach (var analyzer in this.Analyzers.Values)
            {
                if (analyzer.IsValueCreated)
                {
                    analyzer.Value.RemoveDocument(documentId);
                }
            }
        }

        public void RemoveProject(ProjectId projectId)
        {
            foreach (var analyzer in this.Analyzers.Values)
            {
                if (analyzer.IsValueCreated)
                {
                    analyzer.Value.RemoveProject(projectId);
                }
            }
        }
    }
}
