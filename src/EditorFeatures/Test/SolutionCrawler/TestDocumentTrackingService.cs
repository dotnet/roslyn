// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Immutable;
using System.Composition;
using Microsoft.CodeAnalysis.Host.Mef;

namespace Microsoft.CodeAnalysis.Editor.Test
{
    [ExportWorkspaceService(typeof(IDocumentTrackingService)), Shared]
    internal sealed class TestDocumentTrackingService : IDocumentTrackingService
    {
        private readonly object _gate = new object();
        private event EventHandler<DocumentId> _activeDocumentChangedEventHandler;
        private DocumentId _activeDocumentId;

        [ImportingConstructor]
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
        {
            return _activeDocumentId;
        }

        public ImmutableArray<DocumentId> GetVisibleDocuments()
        {
            return _activeDocumentId != null ? ImmutableArray.Create(_activeDocumentId) : ImmutableArray<DocumentId>.Empty;
        }
    }
}
