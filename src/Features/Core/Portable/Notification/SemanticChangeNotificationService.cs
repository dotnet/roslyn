﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Concurrent;
using System.Composition;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.SolutionCrawler;

namespace Microsoft.CodeAnalysis.Notification
{
    [Export(typeof(ISemanticChangeNotificationService)), Shared]
    [ExportIncrementalAnalyzerProvider(nameof(SemanticChangeNotificationService), workspaceKinds: null)]
    internal class SemanticChangeNotificationService : ISemanticChangeNotificationService, IIncrementalAnalyzerProvider
    {
        [ImportingConstructor]
        public SemanticChangeNotificationService()
        {
        }

        public event EventHandler<Document> OpenedDocumentSemanticChanged;

        public IIncrementalAnalyzer CreateIncrementalAnalyzer(Workspace workspace)
        {
            return new NotificationService(this);
        }

        private void RaiseOpenDocumentSemanticChangedEvent(Document document)
        {
            OpenedDocumentSemanticChanged?.Invoke(this, document);
        }

        private class NotificationService : IIncrementalAnalyzer
        {
            private readonly SemanticChangeNotificationService _owner;
            private readonly ConcurrentDictionary<DocumentId, VersionStamp> _map = new ConcurrentDictionary<DocumentId, VersionStamp>(concurrencyLevel: 2, capacity: 10);

            public NotificationService(SemanticChangeNotificationService owner)
            {
                _owner = owner;
            }

            public Task RemoveDocumentAsync(DocumentId documentId, CancellationToken cancellationToken)
            {
                // now it runs for all workspace, make sure we get rid of entry from the map
                // as soon as it is not needed.
                // this whole thing will go away when workspace disable itself from solution crawler.
                _map.TryRemove(documentId, out var unused);
                return Task.CompletedTask;
            }

            public async Task RemoveProjectAsync(ProjectId projectId, CancellationToken cancellationToken)
            {
                foreach (var documentId in _map.Keys.Where(id => id.ProjectId == projectId).ToArray())
                {
                    await RemoveDocumentAsync(documentId, cancellationToken).ConfigureAwait(false);
                }
            }

            public Task DocumentCloseAsync(Document document, CancellationToken cancellationToken)
            {
                return DocumentResetAsync(document, cancellationToken);
            }

            public Task DocumentResetAsync(Document document, CancellationToken cancellationToken)
            {
                return RemoveDocumentAsync(document.Id, cancellationToken);
            }

            public bool NeedsReanalysisOnOptionChanged(object sender, OptionChangedEventArgs e)
            {
                return false;
            }

            public async Task AnalyzeDocumentAsync(Document document, SyntaxNode bodyOpt, InvocationReasons reasons, CancellationToken cancellationToken)
            {
                // method body change
                if (bodyOpt != null || !document.IsOpen())
                {
                    return;
                }

                // get semantic version for the project this document belongs to
                var newVersion = await document.Project.GetDependentSemanticVersionAsync(cancellationToken).ConfigureAwait(false);
                // check whether we already saw semantic version change
                if (_map.TryGetValue(document.Id, out var oldVersion) && oldVersion == newVersion)
                {
                    return;
                }

                // update to new version
                _map[document.Id] = newVersion;
                _owner.RaiseOpenDocumentSemanticChangedEvent(document);
            }

            #region unused 
            public Task DocumentOpenAsync(Document document, CancellationToken cancellationToken)
            {
                return Task.CompletedTask;
            }

            public Task NewSolutionSnapshotAsync(Solution solution, CancellationToken cancellationToken)
            {
                return Task.CompletedTask;
            }

            public Task AnalyzeSyntaxAsync(Document document, InvocationReasons reasons, CancellationToken cancellationToken)
            {
                return Task.CompletedTask;
            }

            public Task AnalyzeProjectAsync(Project project, bool semanticsChanged, InvocationReasons reasons, CancellationToken cancellationToken)
            {
                return Task.CompletedTask;
            }

            #endregion
        }
    }
}
