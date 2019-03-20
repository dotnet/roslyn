// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using Microsoft.CodeAnalysis;
using Microsoft.VisualStudio.OLE.Interop;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.ProjectSystem
{
    internal partial class VisualStudioWorkspaceImpl
    {
        private class RenameDocumentUndoUnit : IOleUndoUnit
        {
            private readonly VisualStudioWorkspaceImpl _workspace;
            private readonly DocumentId _documentId;
            private readonly string _fromName;
            private readonly string _toName;

            public RenameDocumentUndoUnit(VisualStudioWorkspaceImpl workspace, DocumentId documentId, string fromName, string toName)
            {
                _workspace = workspace;
                _documentId = documentId;
                _fromName = fromName;
                _toName = toName;
            }

            public void Do(IOleUndoManager pUndoManager)
            {
                var updatedSolution = _workspace.CurrentSolution.WithDocumentName(_documentId, _toName);
                _workspace.TryApplyChanges(updatedSolution);
            }

            public void GetDescription(out string pBstr)
            {
                pBstr = $"Rename '{_fromName}' to '{_toName}'";
            }

            public void GetUnitType(out Guid pClsid, out int plID)
            {
                throw new NotImplementedException();
            }

            public void OnNextAdd()
            {
            }
        }
    }
}
