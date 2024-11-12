// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.Packaging;

namespace Microsoft.CodeAnalysis.AddPackage;

/// <summary>
/// This is the top level 'Install Nuget Package' code action we show in 
/// the lightbulb.  It will have children to 'Install Latest', 
/// 'Install Version 'X' ..., and 'Install with package manager'.
/// </summary>
/// <remarks>
/// Even though we have child actions, we mark ourselves as explicitly non-inlinable.
/// We want to the experience of having the top level item the user has to see and
/// navigate through, and we don't want our child items confusingly being added to the
/// top level light-bulb where it's not clear what effect they would have if invoked.
/// </remarks>
internal sealed class InstallPackageParentCodeAction(
    IPackageInstallerService installerService,
    string source,
    string packageName,
    bool includePrerelease,
    Document document) : CodeAction.CodeActionWithNestedActions(string.Format(FeaturesResources.Install_package_0, packageName),
           CreateNestedActions(installerService, source, packageName, includePrerelease, document),
           isInlinable: false)
{
    /// <summary>
    /// This code action only works by installing a package.  As such, it requires a non document change (and is
    /// thus restricted in which hosts it can run).
    /// </summary>
    public override ImmutableArray<string> Tags => RequiresNonDocumentChangeTags;

    private static ImmutableArray<CodeAction> CreateNestedActions(
        IPackageInstallerService installerService,
        string source, string packageName, bool includePrerelease,
        Document document)
    {
        // Determine what versions of this package are already installed in some project
        // in this solution.  We'll offer to add those specific versions to this project,
        // followed by an option to "Find and install latest version."
        var installedVersions = installerService.GetInstalledVersions(packageName);
        return
        [
            // First add the actions to install a specific version.
            .. installedVersions.Select(v => CreateCodeAction(
                installerService, source, packageName, document,
                versionOpt: v, includePrerelease: includePrerelease, isLocal: true)),
            // Now add the action to install the specific version.
            CreateCodeAction(
                installerService, source, packageName, document,
                versionOpt: null, includePrerelease: includePrerelease, isLocal: false),
            // And finally the action to show the package manager dialog.
            new InstallWithPackageManagerCodeAction(installerService, packageName),
        ];
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
