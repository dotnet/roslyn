// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Threading;
using Microsoft.CodeAnalysis.CodeActions;

namespace Microsoft.CodeAnalysis.CodeFixes.AddImport
{
    internal abstract partial class AbstractAddImportCodeFixProvider<TSimpleNameSyntax>
    {
        private partial class ProjectSymbolReference : SymbolReference
        {
            private class AddProjectReferenceCodeActionOperation : CodeActionOperation
            {
                private readonly DocumentId _contextDocumentId;
                private readonly ProjectId _toProjectId;

                public AddProjectReferenceCodeActionOperation(
                    DocumentId contextDocumentId, ProjectId toProjectId)
                {
                    _contextDocumentId = contextDocumentId;
                    _toProjectId = toProjectId;
                }

                public override void Apply(Workspace workspace, CancellationToken cancellationToken)
                {
                    var service = workspace.Services.GetService<IAddImportUndoService>();
                    service.TryAddProjectReference(workspace, _contextDocumentId, 
                        _contextDocumentId.ProjectId, _toProjectId, cancellationToken);
                }
            }
        }
    }
}
