// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.Host;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.AddImport;

internal abstract partial class AbstractAddImportFeatureService<TSimpleNameSyntax>
{
    private sealed class AssemblyReferenceCodeAction : AddImportCodeAction
    {
        /// <summary>
        /// This code action only works by adding a reference.  As such, it requires a non document change (and is
        /// thus restricted in which hosts it can run).
        /// </summary>
        public AssemblyReferenceCodeAction(
            Document originalDocument,
            AddImportFixData fixData)
            : base(originalDocument, fixData, RequiresNonDocumentChangeTags)
        {
            Contract.ThrowIfFalse(fixData.Kind == AddImportFixKind.ReferenceAssemblySymbol);
        }

        protected override async Task<IEnumerable<CodeActionOperation>> ComputePreviewOperationsAsync(CancellationToken cancellationToken)
            => await ComputeOperationsAsync(isPreview: true, cancellationToken).ConfigureAwait(false);

        protected override Task<ImmutableArray<CodeActionOperation>> ComputeOperationsAsync(IProgress<CodeAnalysisProgress> progress, CancellationToken cancellationToken)
            => ComputeOperationsAsync(isPreview: false, cancellationToken);

        private async Task<ImmutableArray<CodeActionOperation>> ComputeOperationsAsync(bool isPreview, CancellationToken cancellationToken)
        {
            var newDocument = await GetUpdatedDocumentAsync(cancellationToken).ConfigureAwait(false);
            var newProject = newDocument.Project;

            if (isPreview)
            {
                // If this is a preview, just return an ApplyChangesOperation for the updated document
                var operation = new ApplyChangesOperation(newProject.Solution);
                return [operation];
            }
            else
            {
                // Otherwise return an operation that can apply the text changes and add the reference
                var operation = new AddAssemblyReferenceCodeActionOperation(
                    FixData.AssemblyReferenceAssemblyName,
                    FixData.AssemblyReferenceFullyQualifiedTypeName,
                    newProject);
                return [operation];
            }
        }

        private sealed class AddAssemblyReferenceCodeActionOperation(
            string assemblyReferenceAssemblyName,
            string assemblyReferenceFullyQualifiedTypeName,
            Project newProject) : CodeActionOperation
        {
            private readonly string _assemblyReferenceAssemblyName = assemblyReferenceAssemblyName;
            private readonly string _assemblyReferenceFullyQualifiedTypeName = assemblyReferenceFullyQualifiedTypeName;
            private readonly Project _newProject = newProject;

            internal override bool ApplyDuringTests => true;

            public override void Apply(Workspace workspace, CancellationToken cancellationToken)
            {
                var operation = GetApplyChangesOperation(workspace);
                if (operation is null)
                    return;

                operation.Apply(workspace, cancellationToken);
            }

            internal override async Task<bool> TryApplyAsync(
                Workspace workspace, Solution originalSolution, IProgress<CodeAnalysisProgress> progressTracker, CancellationToken cancellationToken)
            {
                var operation = GetApplyChangesOperation(workspace);
                if (operation is null)
                    return false;

                return await operation.TryApplyAsync(workspace, originalSolution, progressTracker, cancellationToken).ConfigureAwait(false);
            }

            private ApplyChangesOperation? GetApplyChangesOperation(Workspace workspace)
            {
                var resolvedPath = ResolvePath(workspace);
                if (string.IsNullOrWhiteSpace(resolvedPath))
                    return null;

                var service = workspace.Services.GetRequiredService<IMetadataService>();
                var reference = service.GetReference(resolvedPath, MetadataReferenceProperties.Assembly);
                var newProject = _newProject.WithMetadataReferences(
                    _newProject.MetadataReferences.Concat(reference));

                return new ApplyChangesOperation(newProject.Solution);
            }

            private string? ResolvePath(Workspace workspace)
            {
                var assemblyResolverService = workspace.Services.GetRequiredService<IFrameworkAssemblyPathResolver>();

                return assemblyResolverService.ResolveAssemblyPath(
                    _newProject.Id,
                    _assemblyReferenceAssemblyName,
                    _assemblyReferenceFullyQualifiedTypeName);
            }
        }
    }
}
