// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.AddImport;
using Microsoft.CodeAnalysis.AddPackage;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeCleanup;
using Microsoft.CodeAnalysis.Editing;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.ExternalAccess.AspNetCore.AddPackage;

/// <inheritdoc cref="InstallPackageData"/>
/// <param name="packageNamespaceName">The fully qualified name of the namespace that should be added as a
/// <c>using/Import</c> in the file if not already present. Should be of the form <c>A.B.C.D</c> only.</param>
internal readonly struct AspNetCoreInstallPackageData(string? packageSource, string packageName, string? packageVersionOpt, string packageNamespaceName)
{
    public readonly string? PackageSource = packageSource;
    public readonly string PackageName = packageName;
    public readonly string? PackageVersionOpt = packageVersionOpt;
    public readonly string PackageNamespaceName = packageNamespaceName;
}

internal static class AspNetCoreAddPackageCodeAction
{
    /// <summary>
    /// Try to create the top level 'Add Nuget Package' code action to add to a lightbulb.
    /// </summary>
    /// <param name="document">The document the fix is being offered in.</param>
    /// <param name="position">The location where the fix is offered.  This will also influence where the using/Import
    /// directive is added.</param>
    /// <param name="installPackageData">Information about the package to be added.</param>
    public static async Task<CodeAction?> TryCreateCodeActionAsync(
        Document document,
        int position,
        AspNetCoreInstallPackageData installPackageData,
        CancellationToken cancellationToken)
    {
        var textChanges = await GetTextChangesAsync(
            document, position, installPackageData.PackageNamespaceName, cancellationToken).ConfigureAwait(false);

        var convertedData = new InstallPackageData(
            installPackageData.PackageSource,
            installPackageData.PackageName,
            installPackageData.PackageVersionOpt,
            textChanges);
        return ParentInstallPackageCodeAction.TryCreateCodeAction(document, convertedData, installerService: null);
    }

    private static async Task<ImmutableArray<TextChange>> GetTextChangesAsync(
        Document document, int position, string namespaceName, CancellationToken cancellationToken)
    {
        // Take the package namespace and make an actual using/import for it.
        var generator = document.GetRequiredLanguageService<SyntaxGenerator>();
        var importDirective = generator.NamespaceImportDeclaration(namespaceName);

        // Now add the import to the document.
        var updatedDocument = await AddImportAsync(document, position, generator, importDirective, cancellationToken).ConfigureAwait(false);

        // Clean things up after adding (this is what normal add-package-import does).
        var codeCleanupOptions = await document.GetCodeCleanupOptionsAsync(CodeCleanupOptions.GetDefault(document.Project.Services), cancellationToken).ConfigureAwait(false);
        var cleanedDocument = await CodeAction.CleanupDocumentAsync(
            updatedDocument, codeCleanupOptions, cancellationToken).ConfigureAwait(false);

        // Determine what text actually changed. Note: this may be empty if the file already had that import in it.
        var textChanges = await cleanedDocument.GetTextChangesAsync(document, cancellationToken).ConfigureAwait(false);

        return textChanges.ToImmutableArray();
    }

    private static async Task<Document> AddImportAsync(Document document, int position, SyntaxGenerator generator, SyntaxNode importDirective, CancellationToken cancellationToken)
    {
        var root = await document.GetRequiredSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
        var compilation = await document.Project.GetRequiredCompilationAsync(cancellationToken).ConfigureAwait(false);

        var addImportOptions = await document.GetAddImportPlacementOptionsAsync(AddImportPlacementOptions.Default, cancellationToken).ConfigureAwait(false);

        var service = document.GetRequiredLanguageService<IAddImportsService>();

        var contextNode = root.FindToken(position).GetRequiredParent();
        var newRoot = service.AddImport(
            compilation, root, contextNode, importDirective, generator, addImportOptions, cancellationToken);

        var updatedDocument = document.WithSyntaxRoot(newRoot);
        return updatedDocument;
    }
}
