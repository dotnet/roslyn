// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Concurrent;
using System.Composition;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.SolutionCrawler;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Notification
{
    [Export(typeof(ISemanticChangeNotificationService)), Shared]
    [ExportIncrementalAnalyzerProvider(WorkspaceKind.Host, WorkspaceKind.Interactive, WorkspaceKind.MiscellaneousFiles)]
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

            public Task DocumentResetAsync(Document document, CancellationToken cancellationToken)
            {
                VersionStamp unused;
                _map.TryRemove(document.Id, out unused);

                return SpecializedTasks.EmptyTask;
            }

            public bool NeedsReanalysisOnOptionChanged(object sender, OptionChangedEventArgs e)
            {
                // TODO: Is this correct?
                return false;
            }

            public async Task AnalyzeDocumentAsync(Document document, SyntaxNode bodyOpt, CancellationToken cancellationToken)
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

            public Task DocumentCloseAsync(Document document, CancellationToken cancellationToken)
            {
                return SpecializedTasks.EmptyTask;
            }

            public Task NewSolutionSnapshotAsync(Solution solution, CancellationToken cancellationToken)
            {
                return SpecializedTasks.EmptyTask;
            }

            public Task AnalyzeSyntaxAsync(Document document, CancellationToken cancellationToken)
            {
                return SpecializedTasks.EmptyTask;
            }

            public Task AnalyzeProjectAsync(Project project, bool semanticsChanged, CancellationToken cancellationToken)
            {
                return SpecializedTasks.EmptyTask;
            }

            public void RemoveDocument(DocumentId documentId)
            {
            }

            public void RemoveProject(ProjectId projectId)
            {
            }
            #endregion
        }
    }
}
