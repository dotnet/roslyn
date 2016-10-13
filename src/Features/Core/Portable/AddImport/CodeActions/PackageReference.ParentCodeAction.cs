// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeActions;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CodeFixes.AddImport
{
    internal abstract partial class AbstractAddImportCodeFixProvider<TSimpleNameSyntax>
    {
        private partial class PackageReference : Reference
        {
            private class ParentCodeAction : CodeAction
            {
                private readonly PackageReference _reference;
                private readonly string _title;
                private readonly ImmutableArray<CodeAction> _childCodeActions;

                public override string Title => _title;

                internal override bool HasCodeActions => true;

                internal override ImmutableArray<CodeAction> GetCodeActions() => _childCodeActions;

                internal override int? Glyph => (int)CodeAnalysis.Glyph.NuGet;

                // Adding a nuget reference is lower priority than other fixes..
                internal override CodeActionPriority Priority => CodeActionPriority.Low;

                public ParentCodeAction(
                    PackageReference reference,
                    Document document,
                    SyntaxNode node,
                    bool placeSystemNamespaceFirst)
                {
                    _reference = reference;

                    _title = string.Format(FeaturesResources.Install_package_0, reference._packageName);

                    // Determine what versions of this package are already installed in some project
                    // in this solution.  We'll offer to add those specific versions to this project,
                    // followed by an option to "Find and install latest version."
                    var installedVersions = reference._installerService.GetInstalledVersions(reference._packageName);
                    var codeActions = new List<CodeAction>();

                    // First add the actions to install a specific version.
                    codeActions.AddRange(installedVersions.Select(
                        v => CreateCodeAction(document, node, placeSystemNamespaceFirst, versionOpt: v, isLocal: true)));

                    // Now add the action to install the specific version.
                    var preferredVersion = _reference._versionOpt;
                    if (preferredVersion == null || !installedVersions.Contains(preferredVersion))
                    {
                        codeActions.Add(CreateCodeAction(document, node, placeSystemNamespaceFirst,
                            versionOpt: _reference._versionOpt, isLocal: false));
                    }

                    // And finally the action to show the package manager dialog.
                    codeActions.Add(new InstallWithPackageManagerCodeAction(reference));

                    _childCodeActions = codeActions.ToImmutableArray();
                }

                private CodeAction CreateCodeAction(
                    Document document,
                    SyntaxNode node,
                    bool placeSystemNamespaceFirst,
                    string versionOpt,
                    bool isLocal)
                {
                    var title = versionOpt == null
                        ? FeaturesResources.Find_and_install_latest_version
                        : isLocal
                            ? string.Format(FeaturesResources.Use_local_version_0, versionOpt)
                            : string.Format(FeaturesResources.Install_version_0, versionOpt);

                    var getOperations = new AsyncLazy<ValueTuple<ApplyChangesOperation, InstallNugetPackageOperation>>(
                        c => GetOperationsAsync(versionOpt, isLocal, document, node, placeSystemNamespaceFirst, c),
                        cacheResult: true);

                    // Nuget hits should always come after other results.
                    return new InstallPackageAndAddImportCodeAction(
                        title, CodeActionPriority.Low, getOperations);
                }

                private async Task<ValueTuple<ApplyChangesOperation, InstallNugetPackageOperation>> GetOperationsAsync(
                    string versionOpt, 
                    bool isLocal,
                    Document document, 
                    SyntaxNode node, 
                    bool placeSystemNamespaceFirst, 
                    CancellationToken cancellationToken)
                {
                    _reference.ReplaceNameNode(ref node, ref document, cancellationToken);

                    var newDocument = await _reference.provider.AddImportAsync(
                        node, _reference.SearchResult.NameParts, document, placeSystemNamespaceFirst, cancellationToken).ConfigureAwait(false);
                    var newSolution = newDocument.Project.Solution;

                    // Create a dummy code action here so that we go through the codepath
                    // where the solution is 'preprocessed' (i.e. formatting/simplification/etc.
                    // is run). 
                    var codeAction = new SolutionChangeAction("", c => Task.FromResult(newSolution));
                    var codeActionOperations =  await codeAction.GetOperationsAsync(cancellationToken).ConfigureAwait(false);

                    var operation1 = (ApplyChangesOperation)codeActionOperations.Single();
                    var operation2 = new InstallNugetPackageOperation(
                        _reference._installerService, document, _reference._source, _reference._packageName, versionOpt, isLocal);

                    return ValueTuple.Create(operation1, operation2);
                }
            }
        }
    }
}