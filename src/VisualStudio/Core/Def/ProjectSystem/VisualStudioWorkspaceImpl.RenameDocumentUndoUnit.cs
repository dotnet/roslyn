// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Linq;
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
                // Using FirstOrDefault because we only need to rename one document, as that will get
                // applied to linked files.
                var documentId = _workspace.CurrentSolution.GetDocumentIdsWithFilePath(_filePath).FirstOrDefault();
                if (documentId != null)
                {
                    var updatedSolution = _workspace.CurrentSolution.WithDocumentName(documentId, _toName);
                    _workspace.TryApplyChanges(updatedSolution);
                }
            }

            public void GetDescription(out string pBstr)
                => pBstr = string.Format(ServicesVSResources.Rename_0_to_1, _fromName, _toName);

            public void GetUnitType(out Guid pClsid, out int plID)
                => throw new NotImplementedException();

            public void OnNextAdd()
            {
            }
        }
    }
}
