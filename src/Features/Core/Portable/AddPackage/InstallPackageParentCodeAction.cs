// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.Packaging;

namespace Microsoft.CodeAnalysis.AddPackage
{
    /// <summary>
    /// This is the top level 'Install Nuget Package' code action we show in 
    /// the lightbulb.  It will have children to 'Install Latest', 
    /// 'Install Version 'X' ..., and 'Install with package manager'.
    /// </summary>
    internal class InstallPackageParentCodeAction : CodeAction.CodeActionWithNestedActions
    {
        private readonly IPackageInstallerService _installerService;
        private readonly PackageInfo _packageInfo;

        internal override int? Glyph => (int)CodeAnalysis.Glyph.NuGet;

        /// <summary>
        /// Even though we have child actions, we mark ourselves as explicitly non-inlinable.
        /// We want to the experience of having the top level item the user has to see and
        /// navigate through, and we don't want our child items confusingly being added to the
        /// top level light-bulb where it's not clear what effect they would have if invoked.
        /// </summary>
        public InstallPackageParentCodeAction(
            IPackageInstallerService installerService,
            PackageInfo packageInfo,
            Document document)
            : base(string.Format(FeaturesResources.Install_package_0, packageInfo.PackageName),
                   CreateNestedActions(installerService, packageInfo, document),
                   isInlinable: false)
        {
            _installerService = installerService;
            _packageInfo = packageInfo;
        }

        private static ImmutableArray<CodeAction> CreateNestedActions(
            IPackageInstallerService installerService,
            PackageInfo packageInfo, Document document)
        {
            // Determine what versions of this package are already installed in some project
            // in this solution.  We'll offer to add those specific versions to this project,
            // followed by an option to "Find and install latest version."
            var installedVersions = installerService.GetInstalledVersions(packageInfo.PackageName);
            var codeActions = ArrayBuilder<CodeAction>.GetInstance();

            // First add the actions to install a specific version.
            codeActions.AddRange(installedVersions.Select(v => CreateCodeAction(
                installerService, packageInfo, 
                document, versionOpt: v, isLocal: true)));

            // Now add the action to install the specific version.
            codeActions.Add(CreateCodeAction(
                installerService, packageInfo, document,
                versionOpt: null, isLocal: false));

            // And finally the action to show the package manager dialog.
            codeActions.Add(new InstallWithPackageManagerCodeAction(installerService, packageInfo.PackageName));
            return codeActions.ToImmutableAndFree();
        }

        private static CodeAction CreateCodeAction(
            IPackageInstallerService installerService,
            PackageInfo packageInfo,
            Document document,
            string versionOpt,
            bool isLocal)
        {
            return new InstallPackageDirectlyCodeAction(
                installerService, document, packageInfo, versionOpt, isLocal);
        }
    }
}