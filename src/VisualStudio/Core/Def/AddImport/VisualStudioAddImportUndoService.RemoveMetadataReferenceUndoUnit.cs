// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.IO;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeFixes.AddImport;
using Microsoft.VisualStudio.OLE.Interop;

namespace Microsoft.VisualStudio.LanguageServices.AddImport
{
    internal partial class VisualStudioAddImportUndoService : IAddImportUndoService
    {
        private class RemoveMetadataReferenceUndoUnit : AbstractAddRemoveUndoUnit
        {
            private readonly string _filePath;

            public RemoveMetadataReferenceUndoUnit(
                VisualStudioAddImportUndoService service, 
                DocumentId contextDocumentId, 
                ProjectId fromProjectId, 
                string filePath)
                : base(service, contextDocumentId, fromProjectId)
            {
                _filePath = filePath;
            }

            public override void Do(IOleUndoManager pUndoManager)
            {
                Service.TryRemoveMetadataReferenceAndAddUndoUnit(
                    ContextDocumentId, FromProjectId, _filePath, pUndoManager);
            }

            public override void GetDescription(out string pBstr)
            {
                pBstr = string.Format(FeaturesResources.Remove_reference_to_0,
                    Path.GetFileName(_filePath));
            }
        }
    }
}
