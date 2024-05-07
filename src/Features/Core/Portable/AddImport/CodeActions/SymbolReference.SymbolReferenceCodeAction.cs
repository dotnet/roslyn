// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeActions;
using Roslyn.Utilities;

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
                AddImportFixData fixData,
                ImmutableArray<string> additionalTags)
                : base(originalDocument, fixData, additionalTags)
            {
            }

            protected override async Task<IEnumerable<CodeActionOperation>> ComputePreviewOperationsAsync(CancellationToken cancellationToken)
            {
                var operation = await GetChangeSolutionOperationAsync(isPreview: true, cancellationToken).ConfigureAwait(false);
                if (operation is null)
                {
                    return Array.Empty<CodeActionOperation>();
                }

                return SpecializedCollections.SingletonEnumerable(operation);
            }

            protected override async Task<ImmutableArray<CodeActionOperation>> ComputeOperationsAsync(
                IProgress<CodeAnalysisProgress> progress, CancellationToken cancellationToken)
            {
                var operation = await GetChangeSolutionOperationAsync(isPreview: false, cancellationToken).ConfigureAwait(false);
                if (operation is null)
                {
                    return ImmutableArray<CodeActionOperation>.Empty;
                }

                return ImmutableArray.Create(operation);
            }

            private async Task<CodeActionOperation?> GetChangeSolutionOperationAsync(bool isPreview, CancellationToken cancellationToken)
            {
                var updatedDocument = await GetUpdatedDocumentAsync(cancellationToken).ConfigureAwait(false);

                // Defer to subtype to add any p2p or metadata refs as appropriate. If no changes to project references
                // are necessary, the call to 'UpdateProjectAsync' will return null, in which case we fall back to just
                // returning the updated document with its text changes.
                var updatedProject = await UpdateProjectAsync(updatedDocument.Project, isPreview, cancellationToken).ConfigureAwait(false);
                return updatedProject ?? new ApplyChangesOperation(updatedDocument.Project.Solution);
            }

            protected abstract Task<CodeActionOperation?> UpdateProjectAsync(Project project, bool isPreview, CancellationToken cancellationToken);
        }
    }
}
