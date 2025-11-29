// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
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

internal abstract class AbstractAddImportCodeRefactoringProvider<
    TExpressionSyntax,
    TMemberAccessExpressionSyntax,
    TNameSyntax,
    TQualifiedNameSyntax,
    TAliasQualifiedNameSyntax>
    : CodeRefactoringProvider
    where TExpressionSyntax : SyntaxNode
    where TMemberAccessExpressionSyntax : TExpressionSyntax
    where TNameSyntax : TExpressionSyntax
    where TQualifiedNameSyntax : TNameSyntax
    where TAliasQualifiedNameSyntax : TNameSyntax
{
    public override async Task ComputeRefactoringsAsync(CodeRefactoringContext context)
    {
        var (document, textSpan, cancellationToken) = context;

        // Only offer when the cursor is at a single point (not a selection)
        if (!textSpan.IsEmpty)
            return;

        var syntaxFacts = document.GetRequiredLanguageService<ISyntaxFactsService>();
        var root = await document.GetRequiredSyntaxRootAsync(cancellationToken).ConfigureAwait(false);

        var token = root.FindToken(textSpan.Start);
        var node = token.GetRequiredParent();
        if (node is not TNameSyntax name)
            return;

        // Get the qualified type reference - this might be a QualifiedName, AliasQualifiedName, or a member access
        // expression that refers to a type.
        var semanticModel = await document.GetRequiredSemanticModelAsync(cancellationToken).ConfigureAwait(false);
        var qualifiedTypeReference = GetQualifiedTypeReference();
        if (qualifiedTypeReference == null)
            return;

        var symbolInfo = semanticModel.GetSymbolInfo(qualifiedTypeReference, cancellationToken);
        if (symbolInfo.Symbol is not INamespaceOrTypeSymbol namespaceOrType)
            return;

        var namespaceSymbol = (namespaceOrType as INamespaceSymbol) ?? namespaceOrType.ContainingNamespace;
        if (namespaceSymbol.IsGlobalNamespace)
            return;

        // Check if there's already a using directive for this namespace
        var namespaceDisplayString = namespaceSymbol.ToDisplayString();
        var addImportsService = document.GetRequiredLanguageService<IAddImportsService>();
        var generator = SyntaxGenerator.GetGenerator(document);
        var namespaceImport = generator.NamespaceImportDeclaration(namespaceDisplayString);

        if (addImportsService.HasExistingImport(semanticModel.Compilation, root, qualifiedTypeReference, namespaceImport, generator))
            return;

        context.RegisterRefactorings([
            CodeAction.Create(
                string.Format(FeaturesResources.Add_import_for_0, namespaceDisplayString),
                cancellationToken => AddImportAndSimplifyAsync(simplifyAllOccurrences: false, cancellationToken)),
            CodeAction.Create(
                string.Format(FeaturesResources.Add_import_for_0_and_simplify_all_occurrences, namespaceDisplayString),
                cancellationToken => AddImportAndSimplifyAsync(simplifyAllOccurrences: true, cancellationToken))],
            qualifiedTypeReference.Span);

        static bool IsQualified([NotNullWhen(true)] SyntaxNode? node)
            => node is TQualifiedNameSyntax or TAliasQualifiedNameSyntax or TMemberAccessExpressionSyntax;

        TExpressionSyntax? GetQualifiedTypeReference()
        {
            // Offer on any of the namespace or type names in `global::System.Console.WriteLine()`.
            var symbol = semanticModel.GetSymbolInfo(name, cancellationToken).Symbol;
            if (symbol is not INamespaceOrTypeSymbol namespaceOrType)
                return null;

            // Walk up to the highest type/namespace we find.
            SyntaxNode current = name;
            while (IsQualified(current.Parent) && semanticModel.GetSymbolInfo(current.Parent, cancellationToken).Symbol is INamespaceOrTypeSymbol)
                current = current.Parent;

            return current switch
            {
                TQualifiedNameSyntax qualifiedName => qualifiedName,
                TAliasQualifiedNameSyntax aliasQualifiedName => aliasQualifiedName,
                TMemberAccessExpressionSyntax memberAccessExpression => memberAccessExpression,
                _ => null,
            };
        }
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
