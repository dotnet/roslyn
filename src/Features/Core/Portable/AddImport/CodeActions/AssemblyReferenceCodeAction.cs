// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable

using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.Host;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.AddImport
{
    internal abstract partial class AbstractAddImportFeatureService<TSimpleNameSyntax>
    {
        private class AssemblyReferenceCodeAction : AddImportCodeAction
        {
            public AssemblyReferenceCodeAction(
                Document originalDocument,
                AddImportFixData fixData)
                : base(originalDocument, fixData)
            {
                Contract.ThrowIfFalse(fixData.Kind == AddImportFixKind.ReferenceAssemblySymbol);
            }

            private Task<string?> ResolvePathAsync(CancellationToken cancellationToken)
            {
                var assemblyResolverService = OriginalDocument.Project.Solution.Workspace.Services.GetRequiredService<IFrameworkAssemblyPathResolver>();

                return assemblyResolverService.ResolveAssemblyPathAsync(
                    OriginalDocument.Project.Id,
                    FixData.AssemblyReferenceAssemblyName,
                    FixData.AssemblyReferenceFullyQualifiedTypeName,
                    cancellationToken);
            }

            protected override Task<IEnumerable<CodeActionOperation>> ComputePreviewOperationsAsync(CancellationToken cancellationToken)
                => ComputeOperationsAsync(isPreview: true, cancellationToken);

            protected override Task<IEnumerable<CodeActionOperation>> ComputeOperationsAsync(CancellationToken cancellationToken)
                => ComputeOperationsAsync(isPreview: false, cancellationToken);

            private async Task<IEnumerable<CodeActionOperation>> ComputeOperationsAsync(bool isPreview, CancellationToken cancellationToken)
            {
                var newDocument = await GetUpdatedDocumentAsync(cancellationToken).ConfigureAwait(false);
                var newProject = newDocument.Project;

                // Now add the actual assembly reference.
                if (!isPreview)
                {
                    var resolvedPath = await ResolvePathAsync(cancellationToken).ConfigureAwait(false);
                    if (!string.IsNullOrWhiteSpace(resolvedPath))
                    {
                        var service = OriginalDocument.Project.Solution.Workspace.Services.GetRequiredService<IMetadataService>();
                        var reference = service.GetReference(resolvedPath, MetadataReferenceProperties.Assembly);
                        newProject = newProject.WithMetadataReferences(
                            newProject.MetadataReferences.Concat(reference));
                    }
                }

                var operation = new ApplyChangesOperation(newProject.Solution);
                return SpecializedCollections.SingletonEnumerable<CodeActionOperation>(operation);
            }
        }
    }
}
