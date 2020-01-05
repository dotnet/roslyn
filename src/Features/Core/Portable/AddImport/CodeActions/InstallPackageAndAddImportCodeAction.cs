// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.AddPackage;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Shared.Utilities;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.AddImport
{
    internal abstract partial class AbstractAddImportFeatureService<TSimpleNameSyntax>
    {
        private class InstallPackageAndAddImportCodeAction : AddImportCodeAction
        {
            public override string Title { get; }

            /// <summary>
            /// The operation that will actually install the nuget package.
            /// </summary>
            private readonly InstallPackageDirectlyCodeActionOperation _installOperation;

            public InstallPackageAndAddImportCodeAction(
                Document originalDocument,
                AddImportFixData fixData,
                string title,
                InstallPackageDirectlyCodeActionOperation installOperation)
                : base(originalDocument, fixData)
            {
                Contract.ThrowIfFalse(fixData.Kind == AddImportFixKind.PackageSymbol);
                Title = title;
                _installOperation = installOperation;
            }

            /// <summary>
            /// For preview purposes we return all the operations in a list.  This way the 
            /// preview system stiches things together in the UI to make a suitable display.
            /// i.e. if we have a SolutionChangedOperation and some other operation with a 
            /// Title, then the UI will show that nicely to the user.
            /// </summary>
            protected override async Task<IEnumerable<CodeActionOperation>> ComputePreviewOperationsAsync(CancellationToken cancellationToken)
            {
                // Make a SolutionChangeAction.  This way we can let it generate the diff
                // preview appropriately.
                var solutionChangeAction = new SolutionChangeAction(
                    "", c => GetUpdatedSolutionAsync(c));

                var result = ArrayBuilder<CodeActionOperation>.GetInstance();
                result.AddRange(await solutionChangeAction.GetPreviewOperationsAsync(cancellationToken).ConfigureAwait(false));
                result.Add(_installOperation);
                return result.ToImmutableAndFree();
            }

            private async Task<Solution> GetUpdatedSolutionAsync(CancellationToken cancellationToken)
            {
                var newDocument = await GetUpdatedDocumentAsync(cancellationToken).ConfigureAwait(false);
                var newRoot = await newDocument.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);

                // Suppress diagnostics on the import we create.  Because we only get here when we are 
                // adding a nuget package, it is certainly the case that in the preview this will not
                // bind properly.  It will look silly to show such an error, so we just suppress things.
                var updatedRoot = newRoot.WithAdditionalAnnotations(SuppressDiagnosticsAnnotation.Create());
                var updatedDocument = newDocument.WithSyntaxRoot(updatedRoot);

                return updatedDocument.Project.Solution;
            }

            /// <summary>
            /// However, for application purposes, we end up returning a single operation
            /// that will then apply all our sub actions in order, stopping the moment
            /// one of them fails.
            /// </summary>
            protected override async Task<IEnumerable<CodeActionOperation>> ComputeOperationsAsync(
                CancellationToken cancellationToken)
            {
                var updatedDocument = await GetUpdatedDocumentAsync(cancellationToken).ConfigureAwait(false);

                var oldText = await OriginalDocument.GetTextAsync(cancellationToken).ConfigureAwait(false);
                var newText = await updatedDocument.GetTextAsync(cancellationToken).ConfigureAwait(false);

                return ImmutableArray.Create<CodeActionOperation>(
                    new InstallPackageAndAddImportOperation(
                        OriginalDocument.Id, oldText, newText, _installOperation));
            }
        }

        private class InstallPackageAndAddImportOperation : CodeActionOperation
        {
            private readonly DocumentId _changedDocumentId;
            private readonly SourceText _oldText;
            private readonly SourceText _newText;
            private readonly InstallPackageDirectlyCodeActionOperation _installPackageOperation;

            public InstallPackageAndAddImportOperation(
                DocumentId changedDocumentId,
                SourceText oldText,
                SourceText newText,
                InstallPackageDirectlyCodeActionOperation item2)
            {
                _changedDocumentId = changedDocumentId;
                _oldText = oldText;
                _newText = newText;
                _installPackageOperation = item2;
            }

            internal override bool ApplyDuringTests => _installPackageOperation.ApplyDuringTests;
            public override string Title => _installPackageOperation.Title;

            internal override bool TryApply(Workspace workspace, IProgressTracker progressTracker, CancellationToken cancellationToken)
            {
                var newSolution = workspace.CurrentSolution.WithDocumentText(
                    _changedDocumentId, _newText);

                // First make the changes to add the import to the document.
                if (workspace.TryApplyChanges(newSolution, progressTracker))
                {
                    if (_installPackageOperation.TryApply(workspace, progressTracker, cancellationToken))
                    {
                        return true;
                    }

                    // Installing the nuget package failed.  Roll back the workspace.
                    var rolledBackSolution = workspace.CurrentSolution.WithDocumentText(
                        _changedDocumentId, _oldText);
                    workspace.TryApplyChanges(rolledBackSolution, progressTracker);
                }

                return false;
            }
        }
    }
}
