// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.IO;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeFixes.AddImport;
using Microsoft.VisualStudio.OLE.Interop;

namespace Microsoft.VisualStudio.LanguageServices.AddImport
{
    internal partial class VisualStudioAddImportUndoService : IAddImportUndoService
    {
        private class AddMetadataReferenceUndoUnit : AbstractAddRemoveUndoUnit
        {
            private readonly string _filePath;

            public AddMetadataReferenceUndoUnit(
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
                Service.TryAddMetadataReferenceAndAddUndoAction(
                    Service._workspace, ContextDocumentId, FromProjectId, _filePath, pUndoManager, 
                    CancellationToken.None);
            }

            public override void GetDescription(out string pBstr)
            {
                pBstr = string.Format(FeaturesResources.Add_reference_to_0,
                    Path.GetFileName(_filePath));
            }
        }
    }
}
