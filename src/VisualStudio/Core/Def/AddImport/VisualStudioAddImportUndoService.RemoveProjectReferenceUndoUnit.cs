// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeFixes.AddImport;
using Microsoft.VisualStudio.OLE.Interop;

namespace Microsoft.VisualStudio.LanguageServices.AddImport
{
    internal partial class VisualStudioAddImportUndoService : IAddImportUndoService
    {
        private class RemoveProjectReferenceUndoUnit : AbstractAddRemoveUndoUnit
        {
            private readonly string _projectName;
            private readonly ProjectId _toProjectId;

            public RemoveProjectReferenceUndoUnit(
                VisualStudioAddImportUndoService service, 
                DocumentId contextDocumentId, 
                ProjectId fromProjectId, 
                ProjectId toProjectId,
                string projectName)
                : base(service, contextDocumentId, fromProjectId)
            {
                _toProjectId = toProjectId;
                _projectName = projectName;
            }

            public override void Do(IOleUndoManager pUndoManager)
            {
                Service.TryRemoveProjectReferenceAndAddUndoUnit(
                    ContextDocumentId, FromProjectId, _toProjectId, pUndoManager);
            }

            public override void GetDescription(out string pBstr)
            {
                pBstr = string.Format(FeaturesResources.Remove_reference_to_0, _projectName);
            }
        }
    }
}
