// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.Shared.Utilities;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CodeFixes.AddImport
{
    internal abstract partial class AbstractAddImportCodeFixProvider<TSimpleNameSyntax> : CodeFixProvider, IEqualityComparer<PortableExecutableReference>
    {
        private partial class PackageReference
        {
            private struct InstallPackageAndAddImportData
            {
                /// <summary>
                /// The document before we added the import. Used so we can roll back if installing
                /// the package failed.
                /// </summary>
                public readonly Document OldDocument;

                /// <summary>
                /// The document after the import has been added.
                /// </summary>
                public readonly Document NewDocument;

                /// <summary>
                /// The operation that will actually install the nuget package.
                /// </summary>
                public readonly InstallNugetPackageOperation InstallOperation;

                public InstallPackageAndAddImportData(Document oldDocument, Document newDocument, InstallNugetPackageOperation installOperation)
                {
                    OldDocument = oldDocument;
                    NewDocument = newDocument;
                    InstallOperation = installOperation;
                }
            }

            private class InstallPackageAndAddImportCodeAction : CodeAction
            {
                private readonly string _title;
                private readonly CodeActionPriority _priority;
                private readonly AsyncLazy<InstallPackageAndAddImportData> _installData;

                public override string Title => _title;
                public override string EquivalenceKey => _title;
                internal override CodeActionPriority Priority => _priority;

                public InstallPackageAndAddImportCodeAction(
                    string title, CodeActionPriority priority,
                    AsyncLazy<InstallPackageAndAddImportData> installData)
                {
                    _title = title;
                    _priority = priority;
                    _installData = installData;
                }

                /// <summary>
                /// For preview purposes we return all the operations in a list.  This way the 
                /// preview system stiches things together in the UI to make a suitable display.
                /// i.e. if we have a SolutionChangedOperation and some other operation with a 
                /// Title, then the UI will show that nicely to the user.
                /// </summary>
                protected override async Task<IEnumerable<CodeActionOperation>> ComputePreviewOperationsAsync(CancellationToken cancellationToken)
                {
                    var installData = await _installData.GetValueAsync(cancellationToken).ConfigureAwait(false);

                    // Make a SolutionChangeAction.  This way we can let it generate the diff
                    // preview appropriately.
                    var solutionChangeAction = new SolutionChangeAction(
                        "", c => Task.FromResult(installData.NewDocument.Project.Solution));

                    var result = ArrayBuilder<CodeActionOperation>.GetInstance();
                    result.AddRange(await solutionChangeAction.GetPreviewOperationsAsync(cancellationToken).ConfigureAwait(false));
                    result.Add(installData.InstallOperation);
                    return result.ToImmutableAndFree();
                }

                /// <summary>
                /// However, for application purposes, we end up returning a single operation
                /// that will then apply all our sub actions in order, stopping the moment
                /// one of them fails.
                /// </summary>
                protected override async Task<IEnumerable<CodeActionOperation>> ComputeOperationsAsync(
                    CancellationToken cancellationToken)
                {
                    var installData = await _installData.GetValueAsync(cancellationToken).ConfigureAwait(false);

                    var oldText = await installData.OldDocument.GetTextAsync(cancellationToken).ConfigureAwait(false);
                    var newText = await installData.NewDocument.GetTextAsync(cancellationToken).ConfigureAwait(false);

                    return ImmutableArray.Create<CodeActionOperation>(
                        new InstallPackageAndAddImportOperation(
                            installData.OldDocument.Id, oldText, newText, installData.InstallOperation));
                }
            }

            private class InstallPackageAndAddImportOperation : CodeActionOperation
            {
                private readonly DocumentId _changedDocumentId;
                private readonly SourceText _oldText;
                private readonly SourceText _newText;
                private readonly InstallNugetPackageOperation _installNugetPackage;

                public InstallPackageAndAddImportOperation(
                    DocumentId changedDocumentId, 
                    SourceText oldText,
                    SourceText newText, 
                    InstallNugetPackageOperation item2)
                {
                    _changedDocumentId = changedDocumentId;
                    _oldText = oldText;
                    _newText = newText;
                    _installNugetPackage = item2;
                }

                internal override bool ApplyDuringTests => _installNugetPackage.ApplyDuringTests;
                public override string Title => _installNugetPackage.Title;

                internal override bool TryApply(Workspace workspace, IProgressTracker progressTracker, CancellationToken cancellationToken)
                {
                    var newSolution = workspace.CurrentSolution.WithDocumentText(
                        _changedDocumentId, _newText);

                    // First make the changes to add the import to the document.
                    if (workspace.TryApplyChanges(newSolution, progressTracker))
                    {
                        if (_installNugetPackage.TryApply(workspace, progressTracker, cancellationToken))
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
}