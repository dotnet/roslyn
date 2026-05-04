// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.Editing;
using Microsoft.CodeAnalysis.FindSymbols;
using Microsoft.CodeAnalysis.Formatting;
using Microsoft.CodeAnalysis.LanguageService;
using Microsoft.CodeAnalysis.Rename;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.MakeMethodSynchronous;

internal abstract class AbstractMakeMethodSynchronousCodeFixProvider : CodeFixProvider
{
    protected abstract bool IsAsyncSupportingFunctionSyntax(SyntaxNode node);
    protected abstract SyntaxNode RemoveAsyncTokenAndFixReturnType(IMethodSymbol methodSymbol, SyntaxNode node, KnownTaskTypes knownTypes);

    public override FixAllProvider GetFixAllProvider() => WellKnownFixAllProviders.BatchFixer;

    public override async Task RegisterCodeFixesAsync(CodeFixContext context)
    {
        var cancellationToken = context.CancellationToken;
        var diagnostic = context.Diagnostics.First();

        var token = diagnostic.Location.FindToken(cancellationToken);
        var node = token.GetAncestor(IsAsyncSupportingFunctionSyntax);
        if (node != null)
        {
            context.RegisterCodeFix(
                CodeAction.Create(
                    AnalyzersResources.Make_method_synchronous,
                    cancellationToken => FixNodeAsync(context.Document, node, cancellationToken),
                    nameof(AnalyzersResources.Make_method_synchronous)),
                context.Diagnostics);
        }
    }

    private const string AsyncSuffix = "Async";

    private async Task<Solution> FixNodeAsync(
        Document document, SyntaxNode node, CancellationToken cancellationToken)
    {
        // See if we're on an actual method declaration (otherwise we're on a lambda declaration).
        // If we're on a method declaration, we'll get an IMethodSymbol back.  In that case, check
        // if it has the 'Async' suffix, and remove that suffix if so.
        var semanticModel = await document.GetRequiredSemanticModelAsync(cancellationToken).ConfigureAwait(false);
        var methodSymbol = (IMethodSymbol?)(semanticModel.GetDeclaredSymbol(node, cancellationToken) ?? semanticModel.GetSymbolInfo(node, cancellationToken).GetAnySymbol());
        Contract.ThrowIfNull(methodSymbol);

        if (methodSymbol.IsOrdinaryMethodOrLocalFunction() &&
            methodSymbol.Name.Length > AsyncSuffix.Length &&
            methodSymbol.Name.EndsWith(AsyncSuffix))
        {
            return await RenameThenRemoveAsyncTokenAsync(document, node, methodSymbol, cancellationToken).ConfigureAwait(false);
        }
        else
        {
            return await RemoveAsyncTokenAsync(document, methodSymbol, node, cancellationToken).ConfigureAwait(false);
        }
    }

    private async Task<Solution> RenameThenRemoveAsyncTokenAsync(Document document, SyntaxNode node, IMethodSymbol methodSymbol, CancellationToken cancellationToken)
    {
        var name = methodSymbol.Name;
        var newName = name[..^AsyncSuffix.Length];
        var solution = document.Project.Solution;

        // Store the path to this node.  That way we can find it post rename.
        var syntaxPath = new SyntaxPath(node);

        // Rename the method to remove the 'Async' suffix, then remove the 'async' keyword.
        var newSolution = await Renamer.RenameSymbolAsync(solution, methodSymbol, new SymbolRenameOptions(), newName, cancellationToken).ConfigureAwait(false);
        var newDocument = newSolution.GetRequiredDocument(document.Id);
        var newRoot = await newDocument.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
        if (syntaxPath.TryResolve(newRoot, out SyntaxNode? newNode))
        {
            var semanticModel = await newDocument.GetRequiredSemanticModelAsync(cancellationToken).ConfigureAwait(false);
            var newMethod = (IMethodSymbol)semanticModel.GetRequiredDeclaredSymbol(newNode, cancellationToken);
            return await RemoveAsyncTokenAsync(newDocument, newMethod, newNode, cancellationToken).ConfigureAwait(false);
        }

        return newSolution;
    }

    private async Task<Solution> RemoveAsyncTokenAsync(
        Document document, IMethodSymbol methodSymbol, SyntaxNode node, CancellationToken cancellationToken)
    {
        var compilation = await document.Project.GetRequiredCompilationAsync(cancellationToken).ConfigureAwait(false);
        var knownTypes = new KnownTaskTypes(compilation);

        var annotation = new SyntaxAnnotation();
        var newNode = RemoveAsyncTokenAndFixReturnType(methodSymbol, node, knownTypes)
            .WithAdditionalAnnotations(Formatter.Annotation, annotation);

        var root = await document.GetRequiredSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
        var newRoot = root.ReplaceNode(node, newNode);

        var newDocument = document.WithSyntaxRoot(newRoot);
        var newSolution = newDocument.Project.Solution;

        if (!methodSymbol.IsOrdinaryMethodOrLocalFunction())
            return newSolution;

        return await RemoveAwaitFromCallersAsync(
            newDocument, annotation, cancellationToken).ConfigureAwait(false);
    }

    private static async Task<Solution> RemoveAwaitFromCallersAsync(
        Document document, SyntaxAnnotation annotation, CancellationToken cancellationToken)
    {
        var syntaxRoot = await document.GetRequiredSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
        var methodDeclaration = syntaxRoot.GetAnnotatedNodes(annotation).FirstOrDefault();
        if (methodDeclaration != null)
        {
            var semanticModel = await document.GetRequiredSemanticModelAsync(cancellationToken).ConfigureAwait(false);

            if (semanticModel.GetDeclaredSymbol(methodDeclaration, cancellationToken) is IMethodSymbol methodSymbol)
            {
#if CODE_STYLE

                var references = await SymbolFinder.FindReferencesAsync(
                    methodSymbol, document.Project.Solution, cancellationToken).ConfigureAwait(false);

#else

                var references = await SymbolFinder.FindRenamableReferencesAsync(
                    [methodSymbol], document.Project.Solution, cancellationToken).ConfigureAwait(false);

#endif

                var referencedSymbol = references.FirstOrDefault(r => Equals(r.Definition, methodSymbol));
                if (referencedSymbol != null)
                {
                    return await RemoveAwaitFromCallersAsync(
                        document.Project.Solution, [.. referencedSymbol.Locations], cancellationToken).ConfigureAwait(false);
                }
            }
        }

        return document.Project.Solution;
    }

    private static async Task<Solution> RemoveAwaitFromCallersAsync(
        Solution solution, ImmutableArray<ReferenceLocation> locations, CancellationToken cancellationToken)
    {
        var currentSolution = solution;

        var groupedLocations = locations.GroupBy(loc => loc.Document);

        foreach (var group in groupedLocations)
        {
            currentSolution = await RemoveAwaitFromCallersAsync(
                currentSolution, group, cancellationToken).ConfigureAwait(false);
        }

        return currentSolution;
    }

    private static async Task<Solution> RemoveAwaitFromCallersAsync(
        Solution currentSolution, IGrouping<Document, ReferenceLocation> group, CancellationToken cancellationToken)
    {
        var document = group.Key;
        var syntaxFactsService = document.GetRequiredLanguageService<ISyntaxFactsService>();
        var root = await document.GetRequiredSyntaxRootAsync(cancellationToken).ConfigureAwait(false);

        var editor = new SyntaxEditor(root, currentSolution.Services);

        foreach (var location in group)
            RemoveAwaitFromCallerIfPresent(editor, syntaxFactsService, location, cancellationToken);

        var newRoot = editor.GetChangedRoot();
        return currentSolution.WithDocumentSyntaxRoot(document.Id, newRoot);
    }

    private static void RemoveAwaitFromCallerIfPresent(
        SyntaxEditor editor, ISyntaxFactsService syntaxFacts, ReferenceLocation referenceLocation, CancellationToken cancellationToken)
    {
        if (referenceLocation.IsImplicit)
        {
            return;
        }

        var location = referenceLocation.Location;
        var token = location.FindToken(cancellationToken);

        var nameNode = token.Parent;
        if (nameNode == null)
        {
            return;
        }

        // Look for the following forms:
        //  await M(...)
        //  await <expr>.M(...)
        //  await M(...).ConfigureAwait(...)
        //  await <expr>.M(...).ConfigureAwait(...)

        var expressionNode = nameNode;
        if (syntaxFacts.IsNameOfSimpleMemberAccessExpression(nameNode) ||
            syntaxFacts.IsNameOfMemberBindingExpression(nameNode))
        {
            expressionNode = nameNode.Parent;
        }

        if (!syntaxFacts.IsExpressionOfInvocationExpression(expressionNode))
        {
            return;
        }

        // We now either have M(...) or <expr>.M(...)

        var invocationExpression = expressionNode.Parent;
        Debug.Assert(syntaxFacts.IsInvocationExpression(invocationExpression));

        if (syntaxFacts.IsExpressionOfAwaitExpression(invocationExpression))
        {
            // Handle the case where we're directly awaited.  
            var awaitExpression = invocationExpression.GetRequiredParent();
            editor.ReplaceNode(awaitExpression, (currentAwaitExpression, generator) =>
                syntaxFacts.GetExpressionOfAwaitExpression(currentAwaitExpression)
                           .WithTriviaFrom(currentAwaitExpression));
        }
        else if (syntaxFacts.IsExpressionOfMemberAccessExpression(invocationExpression))
        {
            // Check for the .ConfigureAwait case.
            var parentMemberAccessExpression = invocationExpression.GetRequiredParent();
            var parentMemberAccessExpressionNameNode = syntaxFacts.GetNameOfMemberAccessExpression(parentMemberAccessExpression);

            var parentMemberAccessExpressionName = syntaxFacts.GetIdentifierOfSimpleName(parentMemberAccessExpressionNameNode).ValueText;
            if (parentMemberAccessExpressionName == nameof(Task.ConfigureAwait))
            {
                var parentExpression = parentMemberAccessExpression.Parent;
                if (syntaxFacts.IsExpressionOfAwaitExpression(parentExpression))
                {
                    var awaitExpression = parentExpression.GetRequiredParent();
                    editor.ReplaceNode(awaitExpression, (currentAwaitExpression, generator) =>
                    {
                        var currentConfigureAwaitInvocation = syntaxFacts.GetExpressionOfAwaitExpression(currentAwaitExpression);
                        var currentMemberAccess = syntaxFacts.GetExpressionOfInvocationExpression(currentConfigureAwaitInvocation);
                        var currentInvocationExpression = syntaxFacts.GetExpressionOfMemberAccessExpression(currentMemberAccess);
                        Contract.ThrowIfNull(currentInvocationExpression);

                        return currentInvocationExpression.WithTriviaFrom(currentAwaitExpression);
                    });
                }
            }
        }
    }
}
