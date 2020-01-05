// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.Packaging;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Tags;

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
        private readonly string _source;
        private readonly string _packageName;

        public override ImmutableArray<string> Tags => WellKnownTagArrays.NuGet;

        /// <summary>
        /// Even though we have child actions, we mark ourselves as explicitly non-inlinable.
        /// We want to the experience of having the top level item the user has to see and
        /// navigate through, and we don't want our child items confusingly being added to the
        /// top level light-bulb where it's not clear what effect they would have if invoked.
        /// </summary>
        public InstallPackageParentCodeAction(
            IPackageInstallerService installerService,
            string source,
            string packageName,
            bool includePrerelease,
            Document document)
            : base(string.Format(FeaturesResources.Install_package_0, packageName),
                   CreateNestedActions(installerService, source, packageName, includePrerelease, document),
                   isInlinable: false)
        {
            _installerService = installerService;
            _source = source;
            _packageName = packageName;
        }

        private static ImmutableArray<CodeAction> CreateNestedActions(
            IPackageInstallerService installerService,
            string source, string packageName, bool includePrerelease,
            Document document)
        {
            // Determine what versions of this package are already installed in some project
            // in this solution.  We'll offer to add those specific versions to this project,
            // followed by an option to "Find and install latest version."
            var installedVersions = installerService.GetInstalledVersions(packageName);
            var codeActions = ArrayBuilder<CodeAction>.GetInstance();

            // First add the actions to install a specific version.
            codeActions.AddRange(installedVersions.Select(v => CreateCodeAction(
                installerService, source, packageName, document,
                versionOpt: v, includePrerelease: includePrerelease, isLocal: true)));

            // Now add the action to install the specific version.
            codeActions.Add(CreateCodeAction(
                installerService, source, packageName, document,
                versionOpt: null, includePrerelease: includePrerelease, isLocal: false));

            // And finally the action to show the package manager dialog.
            codeActions.Add(new InstallWithPackageManagerCodeAction(installerService, packageName));
            return codeActions.ToImmutableAndFree();
        }

        private static CodeAction CreateCodeAction(
            IPackageInstallerService installerService,
            string source,
            string packageName,
            Document document,
            string versionOpt,
            bool includePrerelease,
            bool isLocal)
        {
            return new InstallPackageDirectlyCodeAction(
                installerService, document, source, packageName,
                versionOpt, includePrerelease, isLocal);
        }
    }
}
