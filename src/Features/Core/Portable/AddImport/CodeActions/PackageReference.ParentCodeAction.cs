// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.AddPackage;
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
            private class ParentCodeAction : CodeAction.CodeActionWithNestedActions
            {
                private readonly PackageReference _reference;

                internal override int? Glyph => (int)CodeAnalysis.Glyph.NuGet;

                // Adding a nuget reference is lower priority than other fixes..
                internal override CodeActionPriority Priority => CodeActionPriority.Low;

                /// <summary>
                /// Even though we have child actions, we mark ourselves as explicitly non-inlinable.
                /// We want to the experience of having the top level item the user has to see and
                /// navigate through, and we don't want our child items confusingly being added to the
                /// top level light-bulb where it's not clear what effect they would have if invoked.
                /// </summary>
                public ParentCodeAction(
                    PackageReference reference,
                    Document document,
                    SyntaxNode node,
                    bool placeSystemNamespaceFirst)
                    : base(string.Format(FeaturesResources.Install_package_0, reference._packageName), 
                           CreateNestedActions(reference, document, node, placeSystemNamespaceFirst),
                           isInlinable: false)
                {
                    _reference = reference;
                }

                private static ImmutableArray<CodeAction> CreateNestedActions(
                    PackageReference reference, Document document, 
                    SyntaxNode node, bool placeSystemNamespaceFirst)
                {
                    // Determine what versions of this package are already installed in some project
                    // in this solution.  We'll offer to add those specific versions to this project,
                    // followed by an option to "Find and install latest version."
                    var installedVersions = reference._installerService.GetInstalledVersions(reference._packageName).NullToEmpty();
                    var codeActions = ArrayBuilder<CodeAction>.GetInstance();

                    // First add the actions to install a specific version.
                    codeActions.AddRange(installedVersions.Select(
                        v => CreateCodeAction(reference, document, node, placeSystemNamespaceFirst, versionOpt: v, isLocal: true)));

                    // Now add the action to install the specific version.
                    var preferredVersion = reference._versionOpt;
                    if (preferredVersion == null || !installedVersions.Contains(preferredVersion))
                    {
                        codeActions.Add(CreateCodeAction(reference, document, node, placeSystemNamespaceFirst,
                            versionOpt: reference._versionOpt, isLocal: false));
                    }

                    // And finally the action to show the package manager dialog.
                    codeActions.Add(new InstallWithPackageManagerCodeAction(reference));
                    return codeActions.ToImmutableAndFree();
                }

                private static CodeAction CreateCodeAction(
                    PackageReference reference,
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
                        c => GetInstallDataAsync(reference, versionOpt, isLocal, document, node, placeSystemNamespaceFirst, c),
                        cacheResult: true);

                    // Nuget hits should always come after other results.
                    return new InstallPackageAndAddImportCodeAction(
                        title, CodeActionPriority.Low, installData);
                }

                private static async Task<InstallPackageAndAddImportData> GetInstallDataAsync(
                    PackageReference reference,
                    string versionOpt, 
                    bool isLocal,
                    Document document, 
                    SyntaxNode node, 
                    bool placeSystemNamespaceFirst, 
                    CancellationToken cancellationToken)
                {
                    var oldDocument = document;
                    reference.ReplaceNameNode(ref node, ref document, cancellationToken);

                    var newDocument = await reference.provider.AddImportAsync(
                        node, reference.SearchResult.NameParts, document, placeSystemNamespaceFirst, cancellationToken).ConfigureAwait(false);

                    // We're going to be manually applying this new document to the workspace
                    // (so we can roll it back ourselves if installing the nuget package fails).
                    // As such, we need to do the postprocessing ourselves of tihs document to 
                    // ensure things like formatting/simplification happen to it.
                    newDocument = await CleanupDocumentAsync(
                        newDocument, cancellationToken).ConfigureAwait(false);

                    var installOperation = new InstallPackageDirectlyCodeActionOperation(
                        reference._installerService, document, reference._source, 
                        reference._packageName, versionOpt, isLocal);

                    return new InstallPackageAndAddImportData(
                        oldDocument, newDocument, installOperation);
                }
            }
        }
    }
}