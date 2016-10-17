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
            /// <summary>
            /// This is the top level 'Install Nuget Package' code action we show in 
            /// the lightbulb.  It will have children to 'Install Latest', 
            /// 'Install Version 'X' ..., and 'Install with package manager'.
            /// </summary>
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

                    var installData = new AsyncLazy<InstallPackageAndAddImportData>(
                        c => GetInstallDataAsync(versionOpt, isLocal, document, node, placeSystemNamespaceFirst, c),
                        cacheResult: true);

                    // Nuget hits should always come after other results.
                    return new InstallPackageAndAddImportCodeAction(
                        title, CodeActionPriority.Low, installData);
                }

                private async Task<InstallPackageAndAddImportData> GetInstallDataAsync(
                    string versionOpt, 
                    bool isLocal,
                    Document document, 
                    SyntaxNode node, 
                    bool placeSystemNamespaceFirst, 
                    CancellationToken cancellationToken)
                {
                    var oldDocument = document;
                    _reference.ReplaceNameNode(ref node, ref document, cancellationToken);

                    var newDocument = await _reference.provider.AddImportAsync(
                        node, _reference.SearchResult.NameParts, document, placeSystemNamespaceFirst, cancellationToken).ConfigureAwait(false);

                    // We're going to be manually applying this new document to the workspace
                    // (so we can roll it back ourselves if installing the nuget package fails).
                    // As such, we need to do the postprocessing ourselves of tihs document to 
                    // ensure things like formatting/simplification happen to it.
                    newDocument = await this.PostProcessChangesAsync(
                        newDocument, cancellationToken).ConfigureAwait(false);

                    var installOperation = new InstallNugetPackageOperation(
                        _reference._installerService, document, _reference._source, _reference._packageName, versionOpt, isLocal);

                    return new InstallPackageAndAddImportData(
                        oldDocument, newDocument, installOperation);
                }
            }
        }
    }
}