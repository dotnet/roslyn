// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis.AddPackage;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.Packaging;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Tags;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.AddImport
{
    internal abstract partial class AbstractAddImportFeatureService<TSimpleNameSyntax>
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
                Document document,
                AddImportFixData fixData,
                IPackageInstallerService installerService)
                : base(string.Format(FeaturesResources.Install_package_0, fixData.PackageName),
                       CreateNestedActions(document, fixData, installerService),
                       isInlinable: false)
            {
                Contract.ThrowIfFalse(fixData.Kind == AddImportFixKind.PackageSymbol);
            }

            private static ImmutableArray<CodeAction> CreateNestedActions(
                Document document,
                AddImportFixData fixData,
                IPackageInstallerService installerService)
            {
                // Determine what versions of this package are already installed in some project
                // in this solution.  We'll offer to add those specific versions to this project,
                // followed by an option to "Find and install latest version."
                var installedVersions = installerService.GetInstalledVersions(fixData.PackageName).NullToEmpty();
                var codeActions = ArrayBuilder<CodeAction>.GetInstance();

                // First add the actions to install a specific version.
                codeActions.AddRange(installedVersions.Select(
                    v => CreateCodeAction(
                        document, fixData, installerService, versionOpt: v, isLocal: true)));

                // Now add the action to install the specific version.
                var preferredVersion = fixData.PackageVersionOpt;
                if (preferredVersion == null || !installedVersions.Contains(preferredVersion))
                {
                    codeActions.Add(CreateCodeAction(
                        document, fixData, installerService, preferredVersion, isLocal: false));
                }

                // And finally the action to show the package manager dialog.
                codeActions.Add(new InstallWithPackageManagerCodeAction(installerService, fixData.PackageName));
                return codeActions.ToImmutableAndFree();
            }

            private static CodeAction CreateCodeAction(
                Document document,
                AddImportFixData fixData,
                IPackageInstallerService installerService,
                string versionOpt,
                bool isLocal)
            {
                var title = versionOpt == null
                    ? FeaturesResources.Find_and_install_latest_version
                    : isLocal
                        ? string.Format(FeaturesResources.Use_local_version_0, versionOpt)
                        : string.Format(FeaturesResources.Install_version_0, versionOpt);

                var installOperation = new InstallPackageDirectlyCodeActionOperation(
                    installerService, document, fixData.PackageSource, fixData.PackageName, versionOpt,
                    includePrerelease: false, isLocal: isLocal);

                // Nuget hits should always come after other results.
                return new InstallPackageAndAddImportCodeAction(
                    document, fixData, title, installOperation);
            }
        }
    }
}
