// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeRefactorings;
using Microsoft.CodeAnalysis.Editing;
using Microsoft.CodeAnalysis.LanguageService;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Shared.Utilities;
using Microsoft.CodeAnalysis.Simplification;

namespace Microsoft.CodeAnalysis.AddImport;

internal abstract class AbstractAddImportCodeRefactoringProvider<
    TExpressionSyntax,
    TMemberAccessExpressionSyntax,
    TNameSyntax,
    TSimpleNameSyntax,
    TQualifiedNameSyntax,
    TAliasQualifiedNameSyntax,
    TImportDirectiveSyntax>(ISyntaxFacts syntaxFacts)
    : CodeRefactoringProvider
    where TExpressionSyntax : SyntaxNode
    where TMemberAccessExpressionSyntax : TExpressionSyntax
    where TNameSyntax : TExpressionSyntax
    where TSimpleNameSyntax : TNameSyntax
    where TQualifiedNameSyntax : TNameSyntax
    where TAliasQualifiedNameSyntax : TNameSyntax
    where TImportDirectiveSyntax : SyntaxNode
{
    private static readonly SyntaxAnnotation s_annotation = new();
    private readonly ObjectPool<PooledHashSet<string>> _hashSetPool = PooledHashSet<string>.CreatePool(syntaxFacts.StringComparer);

    protected abstract string AddImportTitle { get; }
    protected abstract string AddImportAndSimplifyAllOccurrencesTitle { get; }

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
        var (qualifiedTypeReferenceNode, namedType) = GetQualifiedTypeReference();
        if (namedType == null)
            return;

        // To simplify things, we don't offer this on a naked alias.  In other words, we don't offer
        // `global::TopLevelType`.  This simplifies later processing.
        var qualifiedTypeReference = qualifiedTypeReferenceNode switch
        {
            TQualifiedNameSyntax qualifiedName => qualifiedName,
            TMemberAccessExpressionSyntax memberAccessExpression => memberAccessExpression,
            _ => (TExpressionSyntax?)null,
        };

        if (qualifiedTypeReference == null)
            return;

        // Don't want to offer to add a import/using for a namespace if we're already inside an import directive.
        if (qualifiedTypeReference.AncestorsAndSelf().OfType<TImportDirectiveSyntax>().Any())
            return;

        // Only offer to add imports for top-most types.  We can't add a (normal) using/import to a type to pull in
        // nested types.  And while we can make a static-using, that's niche enough to not support for now.
        if (namedType.ContainingType != null)
            return;

        var namespaceSymbol = namedType.ContainingNamespace;
        if (namespaceSymbol is null || namespaceSymbol.IsGlobalNamespace)
            return;

        // If this is actually a type reference off of an alias, don't offer to add a using/import.  The user
        // has already qualified in the way they want.
        var namespaceReference = syntaxFacts.GetLeftSideOfDot(qualifiedTypeReference);
        Contract.ThrowIfNull(namespaceReference);
        if (namespaceReference.DescendantNodesAndSelf().Any(n => semanticModel.GetAliasInfo(n, cancellationToken) != null))
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
                string.Format(AddImportTitle, namespaceDisplayString),
                cancellationToken => AddImportAndSimplifyAsync(simplifyAllOccurrences: false, cancellationToken)),
            CodeAction.Create(
                string.Format(AddImportAndSimplifyAllOccurrencesTitle, namespaceDisplayString),
                cancellationToken => AddImportAndSimplifyAsync(simplifyAllOccurrences: true, cancellationToken))],
            qualifiedTypeReference.Span);

        static bool IsQualified([NotNullWhen(true)] SyntaxNode? node)
            => node is TQualifiedNameSyntax or TAliasQualifiedNameSyntax or TMemberAccessExpressionSyntax;

        (SyntaxNode? qualifiedTypeReference, INamedTypeSymbol? namedType) GetQualifiedTypeReference()
        {
            // Offer on any of the namespace or type names in `global::System.Console.WriteLine()`.
            var symbol = semanticModel.GetSymbolInfo(name, cancellationToken).Symbol;
            if (symbol is INamespaceOrTypeSymbol namespaceOrType)
            {
                // Walk up if we keep seeing a named-type/namespace above us.
                SyntaxNode current = name;
                while (IsQualified(current.Parent))
                {
                    var parentSymbol = semanticModel.GetSymbolInfo(current.Parent, cancellationToken).Symbol;
                    if (parentSymbol is INamespaceOrTypeSymbol)
                    {
                        current = current.Parent;

                        // we want to stop on the first named type we see. In other words, if we have NS1.NS2.T1.T2, we want 
                        // to stop on T1.
                        if (parentSymbol is INamespaceSymbol)
                            continue;

                        return (current, (INamedTypeSymbol)parentSymbol);
                    }

                    // `[System.Obsolete]` will bind to the attributes constructor.
                    if (parentSymbol is IMethodSymbol { MethodKind: MethodKind.Constructor } constructor &&
                        constructor.ContainingType.IsAttribute())
                    {
                        current = current.Parent;
                        return (current, constructor.ContainingType);
                    }

                    break;
                }
            }

            return default;
        }

        async Task<Document> AddImportAndSimplifyAsync(
           bool simplifyAllOccurrences,
           CancellationToken cancellationToken)
        {
            var options = await document.GetAddImportPlacementOptionsAsync(cancellationToken).ConfigureAwait(false);

            var rewrittenRoot = RewriteRoot(simplifyAllOccurrences, cancellationToken);
            var rewrittenQualifiedTypeReference = rewrittenRoot.GetAnnotatedNodes(s_annotation).Single();

            var finalRoot = addImportsService.AddImport(
                semanticModel.Compilation,
                rewrittenRoot,
                rewrittenQualifiedTypeReference,
                namespaceImport,
                generator,
                options,
                cancellationToken);

            return document.WithSyntaxRoot(finalRoot);
        }

        SyntaxNode RewriteRoot(
           bool simplifyAllOccurrences,
           CancellationToken cancellationToken)
        {
            var editor = new SyntaxEditor(root, document.Project.Solution.Services);

            // Add all the new type names we know the using/import will be bringing into scope. If we see such a name in
            // the tree, we'll qualify it to ensure it doesn't change meaning.

            using var _1 = _hashSetPool.GetPooledObject();
            using var _2 = PooledHashSet<SyntaxNode>.GetInstance(out var qualifiedTypeReferenceNodes);

            var newTypeNamesInScope = _1.Object;
            newTypeNamesInScope.AddRange(namespaceSymbol.GetTypeMembers().Select(t => t.Name));

            qualifiedTypeReferenceNodes.AddRange(qualifiedTypeReference.DescendantNodes());

            Debug.Assert(qualifiedTypeReference is TQualifiedNameSyntax or TMemberAccessExpressionSyntax);
            var namespacePortion = syntaxFacts.GetLeftSideOfDot(qualifiedTypeReference);

            // Process simple names from inside out.
            foreach (var child in root.DescendantNodes().OrderByDescending(n => n.SpanStart))
            {
                // Don't touch any nodes under the `global::System.Console` node.  We handle that specially.
                // This ensures we can always find it and always annotate it properly, without other edits
                // interfering.
                if (qualifiedTypeReferenceNodes.Contains(child))
                    continue;

                if (child == qualifiedTypeReference)
                {
                    // Mark the node to be simplified, and add the appropriate annotation on it so that our caller can
                    // find this node again to use as the context node when adding the using/import. Note: we can use
                    // the simple ReplaceNode that does not take a callback as the above check ensures that no edits
                    // will have happened underneath us.
                    editor.ReplaceNode(
                        qualifiedTypeReference,
                        qualifiedTypeReference.WithAdditionalAnnotations(Simplifier.Annotation, s_annotation));
                    continue;
                }

                // If we run into a name like `Console` and we know we're adding `System`, then qualify this name so
                // that it doesn't change after this point.
                if (child is TSimpleNameSyntax simpleName &&
                    newTypeNamesInScope.Contains(syntaxFacts.GetIdentifierOfSimpleName(simpleName).ValueText))
                {
                    if (syntaxFacts.IsLeftSideOfDot(simpleName) ||
                        syntaxFacts.GetStandaloneExpression(simpleName) == simpleName)
                    {
                        var symbol = semanticModel.GetSymbolInfo(simpleName, cancellationToken).Symbol;
                        if (symbol is INamedTypeSymbol namedType)
                        {
                            var typeContext = syntaxFacts.IsInNamespaceOrTypeContext(simpleName);
                            editor.ReplaceNode(
                                simpleName,
                                (current, _) => generator.SyntaxGeneratorInternal.Type(namedType, typeContext));
                        }
                    }

                    continue;
                }

                // If we're adding `using System.Collections.Generic;` and we're simplifying everything, and we run
                // into `System.Collection.Generic.IList<C>`, attempt to simplify that as well.
                if (simplifyAllOccurrences &&
                    child is TMemberAccessExpressionSyntax or TQualifiedNameSyntax)
                {
                    // Left side could be something like `System` or `System.Collections.Generic`
                    var leftSide = syntaxFacts.GetLeftSideOfDot(child);
                    if (leftSide is TMemberAccessExpressionSyntax or TQualifiedNameSyntax or TSimpleNameSyntax)
                    {
                        // Right side is now something like `System` (in the `System.Console` case or `Generic` in the
                        // `System.Collections.Generic.List<T>` case).  Check if that's the name of the namespace we're
                        // adding. if so, mark it to be simplified if possible.
                        var rightSideName = leftSide is TSimpleNameSyntax ? leftSide : syntaxFacts.GetRightSideOfDot(leftSide);
                        Debug.Assert(rightSideName != null);
                        if (syntaxFacts.StringComparer.Equals(
                                namespaceSymbol.Name,
                                syntaxFacts.GetIdentifierOfSimpleName(rightSideName).ValueText) &&
                            SymbolEquivalenceComparer.IgnoreAssembliesInstance.Equals(namespaceSymbol, semanticModel.GetSymbolInfo(leftSide, cancellationToken).Symbol))
                        {
                            editor.ReplaceNode(
                                child,
                                (child, _) => child.WithAdditionalAnnotations(Simplifier.Annotation));
                        }
                    }
                }
            }

            return editor.GetChangedRoot();
        }
    }
}
