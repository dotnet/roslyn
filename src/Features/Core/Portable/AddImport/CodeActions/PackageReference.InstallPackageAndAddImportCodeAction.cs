// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.Shared.Utilities;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CodeFixes.AddImport
{
    internal abstract partial class AbstractAddImportCodeFixProvider<TSimpleNameSyntax> : CodeFixProvider, IEqualityComparer<PortableExecutableReference>
    {
        private partial class PackageReference
        {
            private class InstallPackageAndAddImportCodeAction : CodeAction
            {
                private readonly string _title;
                private readonly CodeActionPriority _priority;
                private readonly AsyncLazy<ValueTuple<ApplyChangesOperation, InstallNugetPackageOperation>> _getOperations;

                public override string Title => _title;
                public override string EquivalenceKey => _title;
                internal override CodeActionPriority Priority => _priority;

                public InstallPackageAndAddImportCodeAction(
                    string title, CodeActionPriority priority,
                    AsyncLazy<ValueTuple<ApplyChangesOperation, InstallNugetPackageOperation>> getOperations)
                {
                    _title = title;
                    _priority = priority;
                    _getOperations = getOperations;
                }

                /// <summary>
                /// For preview purposes we return all the operations in a list.  This way the 
                /// preview system stiches things together in the UI to make a suitable display.
                /// i.e. if we have a SolutionChangedOperation and some other operation with a 
                /// Title, then the UI will show that nicely to the user.
                /// </summary>
                protected override async Task<IEnumerable<CodeActionOperation>> ComputePreviewOperationsAsync(CancellationToken cancellationToken)
                {
                    var operations = await _getOperations.GetValueAsync(cancellationToken).ConfigureAwait(false);
                    return ImmutableArray.Create<CodeActionOperation>(operations.Item1, operations.Item2);
                }

                /// <summary>
                /// However, for application purposes, we end up returning a single operation
                /// that will then apply all our sub actions in order, stopping the moment
                /// one of them fails.
                /// </summary>
                protected override async Task<IEnumerable<CodeActionOperation>> ComputeOperationsAsync(
                    CancellationToken cancellationToken)
                {
                    var operations = await _getOperations.GetValueAsync(cancellationToken).ConfigureAwait(false);
                    return ImmutableArray.Create<CodeActionOperation>(new CompoundOperation(operations.Item1, operations.Item2));
                }
            }

            private class CompoundOperation : CodeActionOperation
            {
                private readonly ApplyChangesOperation _applyChanges;
                private readonly InstallNugetPackageOperation _installNugetPackage;

                public CompoundOperation(ApplyChangesOperation item1, InstallNugetPackageOperation item2)
                {
                    _applyChanges = item1;
                    _installNugetPackage = item2;
                }

                internal override bool ApplyDuringTests => _installNugetPackage.ApplyDuringTests;
                public override string Title => _installNugetPackage.Title;

                internal override bool TryApply(Workspace workspace, IProgressTracker progressTracker, CancellationToken cancellationToken)
                {
                    var oldSolution = workspace.CurrentSolution;

                    // First make the changes to add the import to the document.
                    if (_applyChanges.TryApply(workspace, progressTracker, cancellationToken))
                    {
                        if (_installNugetPackage.TryApply(workspace, progressTracker, cancellationToken))
                        {
                            return true;
                        }

                        // Installing the nuget package failed.  Roll back the workspace.
                        workspace.TryApplyChanges(oldSolution, progressTracker);
                    }

                    return false;
                }
            }
        }
    }
}