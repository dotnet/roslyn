// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.Composition;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Host.Mef;

namespace Microsoft.VisualStudio.LanguageServices.Implementation
{
    [ExportWorkspaceServiceFactory(typeof(IDocumentTrackingService), ServiceLayer.Host), Shared]
    internal sealed class VisualStudioDocumentTrackingServiceFactory : IWorkspaceServiceFactory
    {
        private readonly VisualStudioActiveDocumentTracker _activeDocumentTracker;

        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public VisualStudioDocumentTrackingServiceFactory(VisualStudioActiveDocumentTracker activeDocumentTracker)
            => _activeDocumentTracker = activeDocumentTracker;

        public IWorkspaceService CreateService(HostWorkspaceServices workspaceServices)
            => new VisualStudioDocumentTrackingService(_activeDocumentTracker, workspaceServices.Workspace);

        private class VisualStudioDocumentTrackingService : IDocumentTrackingService
        {
            private readonly VisualStudioActiveDocumentTracker _activeDocumentTracker;
            private readonly Workspace _workspace;

            public VisualStudioDocumentTrackingService(VisualStudioActiveDocumentTracker activeDocumentTracker, Workspace workspace)
            {
                _activeDocumentTracker = activeDocumentTracker;
                _workspace = workspace;
            }

            private readonly object _gate = new();
            private int _subscriptions = 0;
            private EventHandler<DocumentId?>? _activeDocumentChangedEventHandler;

            public bool SupportsDocumentTracking => true;

            public event EventHandler<DocumentId?> ActiveDocumentChanged
            {
                add
                {
                    lock (_gate)
                    {
                        _subscriptions++;

                        if (_subscriptions == 1)
                        {
                            _activeDocumentTracker.DocumentsChanged += ActiveDocumentTracker_DocumentsChanged;
                        }

                        _activeDocumentChangedEventHandler += value;
                    }
                }

                remove
                {
                    lock (_gate)
                    {
                        _activeDocumentChangedEventHandler -= value;

                        _subscriptions--;

                        if (_subscriptions == 0)
                        {
                            _activeDocumentTracker.DocumentsChanged -= ActiveDocumentTracker_DocumentsChanged;
                        }
                    }
                }
            }

            private void ActiveDocumentTracker_DocumentsChanged(object? sender, EventArgs e)
                => _activeDocumentChangedEventHandler?.Invoke(this, TryGetActiveDocument());

            public event EventHandler<EventArgs> NonRoslynBufferTextChanged
            {
                add
                {
                    _activeDocumentTracker.NonRoslynBufferTextChanged += value;
                }

                remove
                {
                    _activeDocumentTracker.NonRoslynBufferTextChanged -= value;
                }
            }

            public DocumentId? TryGetActiveDocument()
                => _activeDocumentTracker.TryGetActiveDocument(_workspace);

            public ImmutableArray<DocumentId> GetVisibleDocuments()
                => _activeDocumentTracker.GetVisibleDocuments(_workspace);
        }
    }
}
