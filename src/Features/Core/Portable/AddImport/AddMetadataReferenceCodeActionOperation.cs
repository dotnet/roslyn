// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Threading;
using Microsoft.CodeAnalysis.CodeActions;

namespace Microsoft.CodeAnalysis.CodeFixes.AddImport
{
    internal abstract partial class AbstractAddImportCodeFixProvider<TSimpleNameSyntax>
    {
        private partial class MetadataSymbolReference : SymbolReference
        {
            private class AddMetadataReferenceCodeActionOperation: CodeActionOperation
            {
                private readonly DocumentId _documentId;
                private readonly PortableExecutableReference _reference;

                public AddMetadataReferenceCodeActionOperation(DocumentId documentId, PortableExecutableReference reference)
                {
                    _documentId = documentId;
                    _reference = reference;
                }

                public override void Apply(Workspace workspace, CancellationToken cancellationToken)
                {
                    var service = workspace.Services.GetService<IAddImportUndoService>();
                    service.TryAddMetadataReference(workspace, _documentId,
                        _documentId.ProjectId, _reference, cancellationToken);
                }
            }
        }
    }
}
