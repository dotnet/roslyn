// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.AddPackage;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.Editing;
using Microsoft.CodeAnalysis.Shared.Extensions;

namespace Microsoft.CodeAnalysis.ExternalAccess.AspNetCore.AddPackage;

/// <inheritdoc cref="InstallPackageData"/>
/// <param name="packageNamespaceName">The fully qualified name of the namespace that should be added as a
/// <c>using/Import</c> in the file if not already present. Should be of the form <c>A.B.C.D</c> only.</param>
internal readonly struct AspNetCoreInstallPackageData(string packageSource, string packageName, string packageVersionOpt, string packageNamespaceName)
{
    public readonly string PackageSource = packageSource;
    public readonly string PackageName = packageName;
    public readonly string PackageVersionOpt = packageVersionOpt;
    public readonly string PackageNamespaceName = packageNamespaceName;
}

internal static class AspNetCoreAddPackageCodeAction
{

    public static async Task<CodeAction?> TryCreateCodeActionAsync(
        Document document,
        int position,
        AspNetCoreInstallPackageData installPackageData,
        CancellationToken cancellationToken)
    {
        return ParentInstallPackageCodeAction.TryCreateCodeAction(
            document,
            await ConvertDataAsync(document, position, installPackageData, cancellationToken).ConfigureAwait(false),
            installerService: null);

        installerService ??= document.Project.Solution.Services.GetService<IPackageInstallerService>();

        return installerService?.IsInstalled(document.Project.Id, fixData.PackageName) == false
            ? new ParentInstallPackageCodeAction(document, fixData, installerService)
            : null;
    }

    private static Task<InstallPackageData> ConvertDataAsync(
        Document document, int position, AspNetCoreInstallPackageData installPackageData, CancellationToken cancellationToken)
    {
        var generator = document.GetRequiredLanguageService<SyntaxGenerator>();
        var importDirective = generator.NamespaceImportDeclaration(installPackageData.PackageNamespaceName);

        var compilation = await document.Project.GetCompilationAsync(cancellationToken).ConfigureAwait(false);
        var service = document.GetLanguageService<IAddImportsService>();
        var generator = SyntaxGenerator.GetGenerator(document);
        var newRoot = service.AddImport(
            compilation, root, contextNode, usingDirective, generator, options, cancellationToken);

    }
}
