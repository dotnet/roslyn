// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Threading;
using Microsoft.CodeAnalysis.CodeActions;

namespace Microsoft.CodeAnalysis.CodeFixes.AddImport
{
    internal abstract partial class AbstractAddImportCodeFixProvider<TSimpleNameSyntax>
    {
        /// <summary>
        /// Handles references to source symbols both from the current project the user is invoking
        /// 'add-import' from, as well as symbols from other viable projects.
        /// 
        /// In the case where the reference is from another project we put a glyph in the add using
        /// light bulb and we say "(from ProjectXXX)" to make it clear that this will do more than
        /// just add a using/import.
        /// </summary>
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
