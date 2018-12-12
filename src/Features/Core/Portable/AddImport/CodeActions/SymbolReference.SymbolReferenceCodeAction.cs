// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Threading;
using System.Threading.Tasks;

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

            protected override async Task<Solution> GetChangedSolutionAsync(CancellationToken cancellationToken)
            {
                var updatedDocument = await GetUpdatedDocumentAsync(cancellationToken).ConfigureAwait(false);

                // Defer to subtype to add any p2p or metadata refs as appropriate.
                var updatedProject = UpdateProject(updatedDocument.Project);

                var updatedSolution = updatedProject.Solution;
                return updatedSolution;
            }

            protected abstract Project UpdateProject(Project project);
        }
    }
}
