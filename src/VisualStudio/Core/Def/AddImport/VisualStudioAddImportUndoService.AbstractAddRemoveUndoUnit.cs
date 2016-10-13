// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeFixes.AddImport;
using Microsoft.VisualStudio.OLE.Interop;

namespace Microsoft.VisualStudio.LanguageServices.AddImport
{
    internal partial class VisualStudioAddImportUndoService : IAddImportUndoService
    {
        private abstract class AbstractAddRemoveUndoUnit : IOleUndoUnit
        {
            protected readonly DocumentId ContextDocumentId;
            protected readonly ProjectId FromProjectId;
            protected readonly VisualStudioAddImportUndoService Service;

            protected AbstractAddRemoveUndoUnit(
                VisualStudioAddImportUndoService service,
                DocumentId contextDocumentId,
                ProjectId fromProjectId)
            {
                Service = service;
                ContextDocumentId = contextDocumentId;
                FromProjectId = fromProjectId;
            }

            public abstract void Do(IOleUndoManager pUndoManager);
            public abstract void GetDescription(out string pBstr);

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