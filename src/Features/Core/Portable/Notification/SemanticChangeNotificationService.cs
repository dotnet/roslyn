// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Concurrent;
using System.Composition;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.SolutionCrawler;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Notification
{
    [Export(typeof(ISemanticChangeNotificationService)), Shared]
    [ExportIncrementalAnalyzerProvider(nameof(SemanticChangeNotificationService), workspaceKinds: null)]
    internal class SemanticChangeNotificationService : ISemanticChangeNotificationService, IIncrementalAnalyzerProvider
    {
        public event EventHandler<Document> OpenedDocumentSemanticChanged;

        public IIncrementalAnalyzer CreateIncrementalAnalyzer(Workspace workspace)
        {
            return new NotificationService(this);
        }

        private void RaiseOpenDocumentSemanticChangedEvent(Document document)
        {
            this.OpenedDocumentSemanticChanged?.Invoke(this, document);
        }

        private class NotificationService : IIncrementalAnalyzer
        {
            private readonly SemanticChangeNotificationService _owner;
            private readonly ConcurrentDictionary<DocumentId, VersionStamp> _map = new ConcurrentDictionary<DocumentId, VersionStamp>(concurrencyLevel: 2, capacity: 10);

            public NotificationService(SemanticChangeNotificationService owner)
            {
                _owner = owner;
            }

            public void RemoveDocument(DocumentId documentId)
            {
                // now it runs for all workspace, make sure we get rid of entry from the map
                // as soon as it is not needed.
                // this whole thing will go away when workspace disable itself from solution crawler.
                VersionStamp unused;
                _map.TryRemove(documentId, out unused);
            }

            public void RemoveProject(ProjectId projectId)
            {
                foreach (var documentId in _map.Keys.Where(id => id.ProjectId == projectId).ToArray())
                {
                    RemoveDocument(documentId);
                }
            }

            public Task DocumentCloseAsync(Document document, CancellationToken cancellationToken)
            {
                return DocumentResetAsync(document, cancellationToken);
            }

            public Task DocumentResetAsync(Document document, CancellationToken cancellationToken)
            {
                RemoveDocument(document.Id);
                return SpecializedTasks.EmptyTask;
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
                VersionStamp oldVersion;
                if (_map.TryGetValue(document.Id, out oldVersion) && oldVersion == newVersion)
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
                return SpecializedTasks.EmptyTask;
            }

            public Task NewSolutionSnapshotAsync(Solution solution, CancellationToken cancellationToken)
            {
                return SpecializedTasks.EmptyTask;
            }

            public Task AnalyzeSyntaxAsync(Document document, InvocationReasons reasons, CancellationToken cancellationToken)
            {
                return SpecializedTasks.EmptyTask;
            }

            public Task AnalyzeProjectAsync(Project project, bool semanticsChanged, InvocationReasons reasons, CancellationToken cancellationToken)
            {
                return SpecializedTasks.EmptyTask;
            }

            #endregion
        }
    }
}
