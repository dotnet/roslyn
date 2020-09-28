// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.Composition;
using Microsoft.CodeAnalysis.Host.Mef;

namespace Microsoft.CodeAnalysis.Editor.Test
{
    [ExportWorkspaceService(typeof(IDocumentTrackingService), ServiceLayer.Test), Shared, PartNotDiscoverable]
    internal sealed class TestDocumentTrackingService : IDocumentTrackingService
    {
        private readonly object _gate = new object();
        private event EventHandler<DocumentId> _activeDocumentChangedEventHandler;
        private DocumentId _activeDocumentId;

        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public TestDocumentTrackingService()
        {
        }

        public event EventHandler<DocumentId> ActiveDocumentChanged
        {
            add
            {
                lock (_gate)
                {
                    _activeDocumentChangedEventHandler += value;
                }
            }

            remove
            {
                lock (_gate)
                {
                    _activeDocumentChangedEventHandler -= value;
                }
            }
        }

        public event EventHandler<EventArgs> NonRoslynBufferTextChanged
        {
            add { }
            remove { }
        }

        public void SetActiveDocument(DocumentId newActiveDocumentId)
        {
            _activeDocumentId = newActiveDocumentId;
            _activeDocumentChangedEventHandler?.Invoke(this, newActiveDocumentId);
        }

        public DocumentId TryGetActiveDocument()
            => _activeDocumentId;

        public ImmutableArray<DocumentId> GetVisibleDocuments()
            => _activeDocumentId != null ? ImmutableArray.Create(_activeDocumentId) : ImmutableArray<DocumentId>.Empty;
    }
}
