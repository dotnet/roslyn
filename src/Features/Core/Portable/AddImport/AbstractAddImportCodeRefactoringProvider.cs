// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeRefactorings;
using Microsoft.CodeAnalysis.Editing;
using Microsoft.CodeAnalysis.LanguageService;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Simplification;

namespace Microsoft.CodeAnalysis.AddImport;

internal abstract class AbstractAddImportCodeRefactoringProvider : CodeRefactoringProvider
{
    public override async Task ComputeRefactoringsAsync(CodeRefactoringContext context)
    {
        var (document, textSpan, cancellationToken) = context;

        // Only offer when the cursor is at a single point (not a selection)
        if (!textSpan.IsEmpty)
            return;

        var syntaxFacts = document.GetRequiredLanguageService<ISyntaxFactsService>();
        var semanticModelService = document.Project.Services.GetRequiredService<ISemanticFactsService>();
        var root = await document.GetRequiredSyntaxRootAsync(cancellationToken).ConfigureAwait(false);

        // Find the token at the position
        var token = root.FindToken(textSpan.Start);
        if (token.Span.Length == 0)
            return;

        // Find a qualified name node (e.g., System.Console or System.Threading.Tasks.Task)
        // starting from the token's parent, walking up to find the outermost qualified name
        var node = token.Parent;
        if (node == null)
            return;

        // Get the outermost qualified name or member access expression
        var qualifiedName = GetOutermostQualifiedName(syntaxFacts, node);
        if (qualifiedName == null)
            return;

        // Check if this qualified name represents a namespace + type (not just a namespace)
        var semanticModel = await document.GetRequiredSemanticModelAsync(cancellationToken).ConfigureAwait(false);
        var symbolInfo = semanticModel.GetSymbolInfo(qualifiedName, cancellationToken);

        // We need the symbol to be a type (not a namespace)
        var symbol = symbolInfo.Symbol ?? symbolInfo.CandidateSymbols.FirstOrDefault();
        if (symbol is not INamedTypeSymbol namedTypeSymbol)
            return;

        // Get the namespace part of the qualified name
        var namespaceSymbol = namedTypeSymbol.ContainingNamespace;
        if (namespaceSymbol == null || namespaceSymbol.IsGlobalNamespace)
            return;

        // Check if there's already a using directive for this namespace
        var addImportsService = document.GetRequiredLanguageService<IAddImportsService>();
        var generator = SyntaxGenerator.GetGenerator(document);
        var namespaceImport = generator.NamespaceImportDeclaration(namespaceSymbol.ToDisplayString());

        if (addImportsService.HasExistingImport(semanticModel.Compilation, root, qualifiedName, namespaceImport, generator))
            return;

        // Get the type name part (the rightmost part of the qualified name)
        var title = string.Format(FeaturesResources.Add_import_for_0, namespaceSymbol.ToDisplayString());

        context.RegisterRefactoring(
            CodeAction.Create(
                title,
                ct => AddImportAndSimplifyAsync(document, qualifiedName, namespaceSymbol, ct),
                title),
            qualifiedName.Span);
    }

    private static SyntaxNode? GetOutermostQualifiedName(ISyntaxFactsService syntaxFacts, SyntaxNode? node)
    {
        // Walk up from the current node to find the outermost qualified name or member access
        SyntaxNode? current = node;
        SyntaxNode? qualifiedName = null;

        while (current != null)
        {
            if (syntaxFacts.IsQualifiedName(current))
            {
                qualifiedName = current;
                current = current.Parent;
                continue;
            }

            if (syntaxFacts.IsAliasQualifiedName(current))
            {
                qualifiedName = current;
                current = current.Parent;
                continue;
            }

            // Handle member access expressions (for expression contexts like System.Console.WriteLine)
            if (syntaxFacts.IsMemberAccessExpression(current))
            {
                qualifiedName = current;
                current = current.Parent;
                continue;
            }

            // If we hit a simple name that's not part of a qualified name, and we haven't found a qualified name yet
            if (syntaxFacts.IsSimpleName(current) && qualifiedName == null)
            {
                // Check if parent is a qualified name
                if (current.Parent != null && syntaxFacts.IsQualifiedName(current.Parent))
                {
                    current = current.Parent;
                    continue;
                }

                // Not a qualified name
                return null;
            }

            break;
        }

        return qualifiedName;
    }

    private static async Task<Document> AddImportAndSimplifyAsync(
        Document document,
        SyntaxNode qualifiedName,
        INamespaceSymbol namespaceSymbol,
        CancellationToken cancellationToken)
    {
        var root = await document.GetRequiredSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
        var semanticModel = await document.GetRequiredSemanticModelAsync(cancellationToken).ConfigureAwait(false);

        var generator = SyntaxGenerator.GetGenerator(document);
        var addImportsService = document.GetRequiredLanguageService<IAddImportsService>();
        var options = await document.GetAddImportPlacementOptionsAsync(cancellationToken).ConfigureAwait(false);

        // Create the using directive
        var namespaceImport = generator.NamespaceImportDeclaration(namespaceSymbol.ToDisplayString());

        // Add the using directive
        var newRoot = addImportsService.AddImport(
            semanticModel.Compilation,
            root,
            qualifiedName,
            namespaceImport,
            generator,
            options,
            cancellationToken);

        // Find the qualified name in the new tree and annotate it for simplification
        var newQualifiedName = newRoot.FindNode(qualifiedName.Span);
        if (newQualifiedName != null)
        {
            var annotatedNode = newQualifiedName.WithAdditionalAnnotations(Simplifier.Annotation);
            newRoot = newRoot.ReplaceNode(newQualifiedName, annotatedNode);
        }

        var newDocument = document.WithSyntaxRoot(newRoot);

        // Simplify the document
        newDocument = await Simplifier.ReduceAsync(newDocument, Simplifier.Annotation, cancellationToken: cancellationToken).ConfigureAwait(false);

        return newDocument;
    }
}
