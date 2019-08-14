// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Options;

namespace Microsoft.CodeAnalysis.SolutionCrawler
{
    internal class IncrementalAnalyzerBase : IIncrementalAnalyzer
    {
        protected IncrementalAnalyzerBase()
        {
        }

        public virtual Task NewSolutionSnapshotAsync(Solution solution, CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }

        public virtual Task DocumentOpenAsync(Document document, CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }

        public virtual Task DocumentCloseAsync(Document document, CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }

        public virtual Task DocumentResetAsync(Document document, CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }

        public bool NeedsReanalysisOnOptionChanged(object sender, OptionChangedEventArgs e)
        {
            return false;
        }

        public virtual Task AnalyzeSyntaxAsync(Document document, InvocationReasons reasons, CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }

        public virtual Task AnalyzeDocumentAsync(Document document, SyntaxNode bodyOpt, InvocationReasons reasons, CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }

        public virtual Task AnalyzeProjectAsync(Project project, bool semanticsChanged, InvocationReasons reasons, CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }

        public virtual void RemoveDocument(DocumentId documentId)
        {
        }

        public virtual void RemoveProject(ProjectId projectId)
        {
        }
    }
}
