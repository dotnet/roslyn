// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.AddImport;
using Microsoft.CodeAnalysis.Editing;
using Microsoft.CodeAnalysis.Formatting;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.Completion.Providers;

internal static class ImportCompletionProviderHelpers
{
    /// <summary>
    /// Gets the changes needed to add a particular import for <paramref name="namespaceName"/> to the supplied document.
    /// </summary>
    public static async Task<ImmutableArray<TextChange>> GetAddImportTextChangesAsync(
        Document document, int completionItemPosition, string namespaceName, CancellationToken cancellationToken)
    {
        // Find context node so we can use it to decide where to insert using/imports.
        var tree = await document.GetRequiredSyntaxTreeAsync(cancellationToken).ConfigureAwait(false);
        var root = await tree.GetRootAsync(cancellationToken).ConfigureAwait(false);
        var addImportContextNode = root.FindToken(completionItemPosition, findInsideTrivia: true).Parent;

        // Add required using/imports directive.
        var addImportService = document.GetRequiredLanguageService<IAddImportsService>();
        var generator = document.GetRequiredLanguageService<SyntaxGenerator>();

        var addImportsOptions = await document.GetAddImportPlacementOptionsAsync(cancellationToken).ConfigureAwait(false);
        var formattingOptions = await document.GetSyntaxFormattingOptionsAsync(cancellationToken).ConfigureAwait(false);

        var syntaxGenerator = SyntaxGenerator.GetGenerator(document);
        var importNode = syntaxGenerator.NamespaceImportDeclaration(namespaceName).WithAdditionalAnnotations(Formatter.Annotation);

        var semanticModel = await document.GetRequiredSemanticModelAsync(cancellationToken).ConfigureAwait(false);
        if (addImportService.HasExistingImport(semanticModel, root, addImportContextNode, importNode, syntaxGenerator, cancellationToken))
            return [];

        var rootWithImport = addImportService.AddImport(semanticModel, root, addImportContextNode!, importNode, generator, addImportsOptions, cancellationToken);
        var documentWithImport = document.WithSyntaxRoot(rootWithImport);

        // This only formats the annotated import we just added, not the entire document.
        var formattedDocumentWithImport = await Formatter.FormatAsync(documentWithImport, Formatter.Annotation, formattingOptions, cancellationToken).ConfigureAwait(false);
        var importChanges = await formattedDocumentWithImport.GetTextChangesAsync(document, cancellationToken).ConfigureAwait(false);
        return [.. importChanges];
    }
}
