// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.AddPackage;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.Packaging;
using Microsoft.CodeAnalysis.Tags;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CodeFixes.AddImport
{
    internal abstract partial class AbstractAddImportCodeFixProvider<TSimpleNameSyntax>
    {
            /// <summary>
            /// This is the top level 'Install Nuget Package' code action we show in 
            /// the lightbulb.  It will have children to 'Install Latest', 
            /// 'Install Version 'X' ..., and 'Install with package manager'.
            /// </summary>
            private class ParentInstallPackageCodeAction : CodeAction.CodeActionWithNestedActions
            {
                public override ImmutableArray<string> Tags => WellKnownTagArrays.NuGet;

                // Adding a nuget reference is lower priority than other fixes..
                internal override CodeActionPriority Priority => CodeActionPriority.Low;

                /// <summary>
                /// Even though we have child actions, we mark ourselves as explicitly non-inlinable.
                /// We want to the experience of having the top level item the user has to see and
                /// navigate through, and we don't want our child items confusingly being added to the
                /// top level light-bulb where it's not clear what effect they would have if invoked.
                /// </summary>
                public ParentInstallPackageCodeAction(
                    IPackageInstallerService installerService,
                    string source,
                    string packageName,
                    string versionOpt,
                    Document document,
                    ImmutableArray<TextChange> textChanges)
                    : base(string.Format(FeaturesResources.Install_package_0, packageName),
                           CreateNestedActions(installerService, source, packageName, versionOpt, document, textChanges),
                           isInlinable: false)
                {
                }

                private static ImmutableArray<CodeAction> CreateNestedActions(
                    IPackageInstallerService installerService,
                    string source,
                    string packageName,
                    string versionOpt,
                    Document document,
                    ImmutableArray<TextChange> textChanges)
                {
                    // Determine what versions of this package are already installed in some project
                    // in this solution.  We'll offer to add those specific versions to this project,
                    // followed by an option to "Find and install latest version."
                    var installedVersions = installerService.GetInstalledVersions(packageName).NullToEmpty();
                    var codeActions = ArrayBuilder<CodeAction>.GetInstance();

                    // First add the actions to install a specific version.
                    codeActions.AddRange(installedVersions.Select(
                        v => CreateCodeAction(
                            installerService, source, packageName, v,
                            document, textChanges, isLocal: true)));

                    // Now add the action to install the specific version.
                    var preferredVersion = versionOpt;
                    if (preferredVersion == null || !installedVersions.Contains(preferredVersion))
                    {
                        codeActions.Add(CreateCodeAction(
                            installerService, source, packageName, versionOpt,
                            document, textChanges, isLocal: false));
                    }

                    // And finally the action to show the package manager dialog.
                    codeActions.Add(new InstallWithPackageManagerCodeAction(installerService, packageName));
                    return codeActions.ToImmutableAndFree();
                }

                private static CodeAction CreateCodeAction(
                    IPackageInstallerService installerService,
                    string source,
                    string packageName,
                    string versionOpt,
                    Document document,
                    ImmutableArray<TextChange> textChanges,
                    bool isLocal)
                {
                    var title = versionOpt == null
                        ? FeaturesResources.Find_and_install_latest_version
                        : isLocal
                            ? string.Format(FeaturesResources.Use_local_version_0, versionOpt)
                            : string.Format(FeaturesResources.Install_version_0, versionOpt);

                    var installOperation = new InstallPackageDirectlyCodeActionOperation(
                        installerService, document, source, packageName, versionOpt,
                        includePrerelease: false, isLocal: isLocal);

                    // Nuget hits should always come after other results.
                    return new InstallPackageAndAddImportCodeAction(
                        title, CodeActionPriority.Low,
                        document, textChanges, installOperation);
                }
            }
    }
}