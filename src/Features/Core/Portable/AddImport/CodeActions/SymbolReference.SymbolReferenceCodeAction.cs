// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeActions;

namespace Microsoft.CodeAnalysis.AddImport
{
    internal abstract partial class AbstractAddImportFeatureService<TSimpleNameSyntax>
    {
        /// <summary>
        /// Code action we use when just adding a using, possibly with a project or
        /// metadata reference.  We don't use the standard code action types because
        /// we want to do things like show a glyph if this will do more than just add
        /// an import.
        /// </summary>
        private abstract class SymbolReferenceCodeAction : AddImportCodeAction
        {
            protected SymbolReferenceCodeAction(
                Document originalDocument,
                AddImportFixData fixData)
                : base(originalDocument, fixData)
            {
            }

            protected override async Task<IEnumerable<CodeActionOperation>> ComputePreviewOperationsAsync(CancellationToken cancellationToken)
            {
                var changedSolution = await GetChangedSolutionAsync(isPreview: true, cancellationToken).ConfigureAwait(false);
                if (changedSolution == null)
                {
                    return Array.Empty<CodeActionOperation>();
                }

                return new CodeActionOperation[] { new ApplyChangesOperation(changedSolution) };
            }

            protected override Task<Solution> GetChangedSolutionAsync(CancellationToken cancellationToken)
            {
                return GetChangedSolutionAsync(isPreview: false, cancellationToken);
            }

            private async Task<Solution> GetChangedSolutionAsync(bool isPreview, CancellationToken cancellationToken)
            {
                var updatedDocument = await GetUpdatedDocumentAsync(cancellationToken).ConfigureAwait(false);

                // Defer to subtype to add any p2p or metadata refs as appropriate.
                var updatedProject = await UpdateProjectAsync(updatedDocument.Project, isPreview, cancellationToken).ConfigureAwait(false);

                var updatedSolution = updatedProject.Solution;
                return updatedSolution;
            }

            protected abstract Task<Project> UpdateProjectAsync(Project project, bool isPreview, CancellationToken cancellationToken);
        }
    }
}
