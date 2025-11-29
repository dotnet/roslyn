// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
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

        // Get the qualified type reference - this might be a QualifiedName, AliasQualifiedName,
        // or a member access expression that refers to a type
        var qualifiedTypeReference = GetQualifiedTypeReference(syntaxFacts, node);
        if (qualifiedTypeReference == null)
            return;

        // Check if this qualified name represents a namespace + type (not just a namespace)
        var semanticModel = await document.GetRequiredSemanticModelAsync(cancellationToken).ConfigureAwait(false);
        var symbolInfo = semanticModel.GetSymbolInfo(qualifiedTypeReference, cancellationToken);

        // We need the symbol to be a type (not a namespace or method)
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

        if (addImportsService.HasExistingImport(semanticModel.Compilation, root, qualifiedTypeReference, namespaceImport, generator))
            return;

        var namespaceDisplayString = namespaceSymbol.ToDisplayString();

        // First action: Add import and simplify just this occurrence
        var title1 = string.Format(FeaturesResources.Add_import_for_0, namespaceDisplayString);
        var action1 = CodeAction.Create(
            title1,
            ct => AddImportAndSimplifyAsync(document, qualifiedTypeReference, namespaceSymbol, simplifyAllOccurrences: false, ct),
            title1);

        // Second action: Add import and simplify all occurrences
        var title2 = string.Format(FeaturesResources.Add_import_for_0_and_simplify_all_occurrences, namespaceDisplayString);
        var action2 = CodeAction.Create(
            title2,
            ct => AddImportAndSimplifyAsync(document, qualifiedTypeReference, namespaceSymbol, simplifyAllOccurrences: true, ct),
            title2);

        context.RegisterRefactoring(action1, qualifiedTypeReference.Span);
        context.RegisterRefactoring(action2, qualifiedTypeReference.Span);
    }

    private static SyntaxNode? GetQualifiedTypeReference(ISyntaxFactsService syntaxFacts, SyntaxNode? node)
    {
        // Walk up from the current node to find the outermost qualified name or member access
        // that represents a type reference
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
            // We need to be careful here - we want to stop at the type, not continue to the method
            if (syntaxFacts.IsMemberAccessExpression(current))
            {
                // If we already have a qualified name, and the parent is a member access,
                // we should check if this is still part of a type reference
                // For now, we'll mark this as a candidate and continue
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

                // Check if parent is a member access expression
                if (current.Parent != null && syntaxFacts.IsMemberAccessExpression(current.Parent))
                {
                    current = current.Parent;
                    continue;
                }

                // Not a qualified name
                return null;
            }

            break;
        }

        // If we found a member access expression, we need to find the part that refers to a type
        // For example, in System.Console.WriteLine(), we want System.Console, not System.Console.WriteLine
        if (qualifiedName != null && syntaxFacts.IsMemberAccessExpression(qualifiedName))
        {
            // Try to find the leftmost expression that's a type
            return FindTypePartOfMemberAccess(syntaxFacts, qualifiedName);
        }

        return qualifiedName;
    }

    private static SyntaxNode? FindTypePartOfMemberAccess(ISyntaxFactsService syntaxFacts, SyntaxNode memberAccess)
    {
        // Walk down the member access chain to find the part that refers to a type
        // For System.Console.WriteLine, we start at the top and work down:
        // - System.Console.WriteLine (method) - not a type
        // - System.Console (type) - this is what we want
        // - System (namespace) - not a type

        var current = memberAccess;
        while (syntaxFacts.IsMemberAccessExpression(current))
        {
            syntaxFacts.GetPartsOfMemberAccessExpression(current, out var expression, out _, out _);
            if (expression != null)
            {
                // Check if the expression part is itself a member access that could be a type
                if (syntaxFacts.IsMemberAccessExpression(expression))
                {
                    current = expression;
                    continue;
                }

                // If the expression is a simple name or qualified name, return the whole current expression
                // The caller will check if it resolves to a type
                break;
            }

            break;
        }

        return current;
    }

    private static async Task<Document> AddImportAndSimplifyAsync(
        Document document,
        SyntaxNode qualifiedName,
        INamespaceSymbol namespaceSymbol,
        bool simplifyAllOccurrences,
        CancellationToken cancellationToken)
    {
        var root = await document.GetRequiredSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
        var semanticModel = await document.GetRequiredSemanticModelAsync(cancellationToken).ConfigureAwait(false);
        var syntaxFacts = document.GetRequiredLanguageService<ISyntaxFactsService>();

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

        if (simplifyAllOccurrences)
        {
            // Find all qualified names that start with this namespace and annotate them for simplification
            var namespaceDisplayString = namespaceSymbol.ToDisplayString();
            var nodesToAnnotate = FindAllQualifiedNamesWithNamespace(newRoot, syntaxFacts, semanticModel, namespaceSymbol, cancellationToken);

            foreach (var nodeToAnnotate in nodesToAnnotate)
            {
                var currentNode = newRoot.FindNode(nodeToAnnotate.Span);
                if (currentNode != null)
                {
                    var annotatedNode = currentNode.WithAdditionalAnnotations(Simplifier.Annotation);
                    newRoot = newRoot.ReplaceNode(currentNode, annotatedNode);
                }
            }
        }
        else
        {
            // Find the qualified name in the new tree and annotate it for simplification
            var newQualifiedName = newRoot.FindNode(qualifiedName.Span);
            if (newQualifiedName != null)
            {
                var annotatedNode = newQualifiedName.WithAdditionalAnnotations(Simplifier.Annotation);
                newRoot = newRoot.ReplaceNode(newQualifiedName, annotatedNode);
            }
        }

        var newDocument = document.WithSyntaxRoot(newRoot);

        // Simplify the document
        newDocument = await Simplifier.ReduceAsync(newDocument, Simplifier.Annotation, cancellationToken: cancellationToken).ConfigureAwait(false);

        return newDocument;
    }

    private static ImmutableArray<SyntaxNode> FindAllQualifiedNamesWithNamespace(
        SyntaxNode root,
        ISyntaxFactsService syntaxFacts,
        SemanticModel semanticModel,
        INamespaceSymbol namespaceSymbol,
        CancellationToken cancellationToken)
    {
        var builder = ImmutableArray.CreateBuilder<SyntaxNode>();
        var namespaceDisplayString = namespaceSymbol.ToDisplayString();

        foreach (var node in root.DescendantNodes())
        {
            cancellationToken.ThrowIfCancellationRequested();

            // Check if this is a qualified name or member access expression
            if (!syntaxFacts.IsQualifiedName(node) && !syntaxFacts.IsMemberAccessExpression(node) && !syntaxFacts.IsAliasQualifiedName(node))
                continue;

            // Get the symbol for this node
            var symbolInfo = semanticModel.GetSymbolInfo(node, cancellationToken);
            var symbol = symbolInfo.Symbol ?? symbolInfo.CandidateSymbols.FirstOrDefault();

            // Check if this is a type from the namespace we're adding
            if (symbol is INamedTypeSymbol typeSymbol)
            {
                var containingNamespace = typeSymbol.ContainingNamespace;
                if (containingNamespace != null &&
                    !containingNamespace.IsGlobalNamespace &&
                    containingNamespace.ToDisplayString() == namespaceDisplayString)
                {
                    builder.Add(node);
                }
            }
        }

        return builder.ToImmutable();
    }
}
