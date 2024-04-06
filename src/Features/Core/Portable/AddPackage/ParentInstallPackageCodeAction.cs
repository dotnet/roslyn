// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis.AddImport;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.Packaging;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.AddPackage;

/// <summary>
/// Data used to create the 'Install Nuget Package' top level code-action. It will have children to 'Install Latest',
/// 'Install Version 'X' ..., and 'Install with package manager'.
/// </summary>
/// <param name="packageSource">The nuget source to use.  Currently this is only <see
/// cref="PackageSourceHelper.NugetOrg"/> ("nuget.org").  Can be <see langword="null"/> to use the users configured
/// sources.</param>
/// <param name="packageName">The name of the package to install.</param>
/// <param name="packageVersionOpt">A optional preferred version if known. If not present, the user will be given the
/// option to either install the latest version, or install any version installed locally in another project.</param>
/// <param name="textChanges">Additional text changes to make to the <see cref="Document"/>.  Generally, this would be
/// the import to add if not present.</param>
internal readonly struct InstallPackageData(string? packageSource, string packageName, string? packageVersionOpt, ImmutableArray<TextChange> textChanges)
{
    public readonly string? PackageSource = packageSource;
    public readonly string PackageName = packageName;
    public readonly string? PackageVersionOpt = packageVersionOpt;

    public readonly ImmutableArray<TextChange> TextChanges = textChanges;
}

/// <summary>
/// This is the top level 'Install Nuget Package' code action we show in 
/// the lightbulb.  It will have children to 'Install Latest', 
/// 'Install Version 'X' ..., and 'Install with package manager'.
/// </summary>
internal sealed class ParentInstallPackageCodeAction : CodeAction.CodeActionWithNestedActions
{
    /// <summary>
    /// This code action only works by installing a package.  As such, it requires a non document change (and is
    /// thus restricted in which hosts it can run).
    /// </summary>
    public override ImmutableArray<string> Tags => RequiresNonDocumentChangeTags;

    /// <summary>
    /// Even though we have child actions, we mark ourselves as explicitly non-inlinable.
    /// We want to the experience of having the top level item the user has to see and
    /// navigate through, and we don't want our child items confusingly being added to the
    /// top level light-bulb where it's not clear what effect they would have if invoked.
    /// </summary>
    public ParentInstallPackageCodeAction(
        Document document,
        InstallPackageData fixData,
        IPackageInstallerService installerService)
        : base(string.Format(FeaturesResources.Install_package_0, fixData.PackageName),
               CreateNestedActions(document, fixData, installerService),
               isInlinable: false,
               priority: CodeActionPriority.Low) // Adding a nuget reference is lower priority than other fixes..
    {
    }

    public static CodeAction? TryCreateCodeAction(
        Document document,
        InstallPackageData fixData,
        IPackageInstallerService? installerService)
    {
        installerService ??= document.Project.Solution.Services.GetService<IPackageInstallerService>();

        return installerService?.IsInstalled(document.Project.Id, fixData.PackageName) == false
            ? new ParentInstallPackageCodeAction(document, fixData, installerService)
            : null;
    }

    private static ImmutableArray<CodeAction> CreateNestedActions(
        Document document,
        InstallPackageData fixData,
        IPackageInstallerService installerService)
    {
        // Determine what versions of this package are already installed in some project
        // in this solution.  We'll offer to add those specific versions to this project,
        // followed by an option to "Find and install latest version."
        var installedVersions = installerService.GetInstalledVersions(fixData.PackageName).NullToEmpty();
        using var _ = ArrayBuilder<CodeAction>.GetInstance(out var codeActions);

        // First add the actions to install a specific version.
        codeActions.AddRange(installedVersions.Select(
            v => CreateCodeAction(
                document, fixData, installerService, version: v, isLocal: true)));

        // Now add the action to install the specific version.
        var preferredVersion = fixData.PackageVersionOpt;
        if (preferredVersion == null || !installedVersions.Contains(preferredVersion))
        {
            codeActions.Add(CreateCodeAction(
                document, fixData, installerService, preferredVersion, isLocal: false));
        }

        // And finally the action to show the package manager dialog.
        codeActions.Add(new InstallWithPackageManagerCodeAction(installerService, fixData.PackageName));
        return codeActions.ToImmutable();
    }

    private static CodeAction CreateCodeAction(
        Document document,
        InstallPackageData installData,
        IPackageInstallerService installerService,
        string? version,
        bool isLocal)
    {
        var title = version == null
            ? FeaturesResources.Find_and_install_latest_version
            : isLocal
                ? string.Format(FeaturesResources.Use_local_version_0, version)
                : string.Format(FeaturesResources.Install_version_0, version);

        var installOperation = new InstallPackageDirectlyCodeActionOperation(
            installerService, document, installData.PackageSource, installData.PackageName, version,
            includePrerelease: false, isLocal: isLocal);

        // Nuget hits should always come after other results.
        var fixData = new AddImportFixData(
            AddImportFixKind.PackageSymbol, installData.TextChanges, title, priority: CodeActionPriority.Lowest,
            packageSource: installData.PackageSource, packageName: installData.PackageName, packageVersionOpt: installData.PackageVersionOpt);
        return new InstallPackageAndAddImportCodeAction(
            document, fixData, title, installOperation);
    }
}
