// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.VisualStudio.OLE.Interop;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.ProjectSystem
{
    internal partial class VisualStudioWorkspaceImpl
    {
        private class RenameDocumentUndoUnit : IOleUndoUnit
        {
            private readonly VisualStudioWorkspaceImpl _workspace;
            private readonly string _fromName;
            private readonly string _toName;
            private readonly string _filePath;

            public RenameDocumentUndoUnit(VisualStudioWorkspaceImpl workspace, string fromName, string toName, string filePath)
            {
                _workspace = workspace;
                _fromName = fromName;
                _toName = toName;
                _filePath = filePath;
            }

            public void Do(IOleUndoManager pUndoManager)
            {
                // FirstOrDefault is okay because that's how the rename would have been
                // done in the forward direction as well.
                var documentId = _workspace.CurrentSolution.GetDocumentIdsWithFilePath(_filePath).FirstOrDefault();
                if (documentId != null)
                {
                    var updatedSolution = _workspace.CurrentSolution.WithDocumentName(documentId, _toName);
                    _workspace.TryApplyChanges(updatedSolution);
                }
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
