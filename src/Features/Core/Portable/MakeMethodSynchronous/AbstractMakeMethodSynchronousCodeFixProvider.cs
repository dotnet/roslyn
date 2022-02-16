// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
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
using Microsoft.CodeAnalysis.LanguageServices;
using Microsoft.CodeAnalysis.Rename;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Roslyn.Utilities;
using static Microsoft.CodeAnalysis.MakeMethodAsynchronous.AbstractMakeMethodAsynchronousCodeFixProvider;

namespace Microsoft.CodeAnalysis.MakeMethodSynchronous
{
    internal abstract class AbstractMakeMethodSynchronousCodeFixProvider : CodeFixProvider
    {
        public static readonly string EquivalenceKey = FeaturesResources.Make_method_synchronous;

        protected abstract bool IsAsyncSupportingFunctionSyntax(SyntaxNode node);
        protected abstract SyntaxNode RemoveAsyncTokenAndFixReturnType(IMethodSymbol methodSymbolOpt, SyntaxNode node, KnownTypes knownTypes);

        public override FixAllProvider GetFixAllProvider() => WellKnownFixAllProviders.BatchFixer;

        public override Task RegisterCodeFixesAsync(CodeFixContext context)
        {
            context.RegisterCodeFix(
                new MyCodeAction(c => FixNodeAsync(context.Document, context.Diagnostics.First(), c)),
                context.Diagnostics);
            return Task.CompletedTask;
        }

        private const string AsyncSuffix = "Async";

        private async Task<Solution> FixNodeAsync(
            Document document, Diagnostic diagnostic, CancellationToken cancellationToken)
        {
            var token = diagnostic.Location.FindToken(cancellationToken);
            var node = token.GetAncestor(IsAsyncSupportingFunctionSyntax);

            // See if we're on an actual method declaration (otherwise we're on a lambda declaration).
            // If we're on a method declaration, we'll get an IMethodSymbol back.  In that case, check
            // if it has the 'Async' suffix, and remove that suffix if so.
            var semanticModel = await document.GetSemanticModelAsync(cancellationToken).ConfigureAwait(false);
            var methodSymbolOpt = semanticModel.GetDeclaredSymbol(node, cancellationToken) as IMethodSymbol;

            var isOrdinaryOrLocalFunction = methodSymbolOpt.IsOrdinaryMethodOrLocalFunction();
            if (isOrdinaryOrLocalFunction &&
                methodSymbolOpt.Name.Length > AsyncSuffix.Length &&
                methodSymbolOpt.Name.EndsWith(AsyncSuffix))
            {
                return await RenameThenRemoveAsyncTokenAsync(document, node, methodSymbolOpt, cancellationToken).ConfigureAwait(false);
            }
            else
            {
                return await RemoveAsyncTokenAsync(document, methodSymbolOpt, node, cancellationToken).ConfigureAwait(false);
            }
        }

        private async Task<Solution> RenameThenRemoveAsyncTokenAsync(Document document, SyntaxNode node, IMethodSymbol methodSymbol, CancellationToken cancellationToken)
        {
            var name = methodSymbol.Name;
            var newName = name.Substring(0, name.Length - AsyncSuffix.Length);
            var solution = document.Project.Solution;

            // Store the path to this node.  That way we can find it post rename.
            var syntaxPath = new SyntaxPath(node);

            // Rename the method to remove the 'Async' suffix, then remove the 'async' keyword.
            var newSolution = await Renamer.RenameSymbolAsync(solution, methodSymbol, new SymbolRenameOptions(), newName, cancellationToken).ConfigureAwait(false);
            var newDocument = newSolution.GetDocument(document.Id);
            var newRoot = await newDocument.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
            if (syntaxPath.TryResolve(newRoot, out SyntaxNode newNode))
            {
                var semanticModel = await newDocument.GetSemanticModelAsync(cancellationToken).ConfigureAwait(false);
                var newMethod = (IMethodSymbol)semanticModel.GetDeclaredSymbol(newNode, cancellationToken);
                return await RemoveAsyncTokenAsync(newDocument, newMethod, newNode, cancellationToken).ConfigureAwait(false);
            }

            return newSolution;
        }

        private async Task<Solution> RemoveAsyncTokenAsync(
            Document document, IMethodSymbol methodSymbolOpt, SyntaxNode node, CancellationToken cancellationToken)
        {
            var compilation = await document.Project.GetCompilationAsync(cancellationToken).ConfigureAwait(false);
            var knownTypes = new KnownTypes(compilation);

            var annotation = new SyntaxAnnotation();
            var newNode = RemoveAsyncTokenAndFixReturnType(methodSymbolOpt, node, knownTypes)
                .WithAdditionalAnnotations(Formatter.Annotation, annotation);

            var root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
            var newRoot = root.ReplaceNode(node, newNode);

            var newDocument = document.WithSyntaxRoot(newRoot);
            var newSolution = newDocument.Project.Solution;

            if (methodSymbolOpt == null)
            {
                return newSolution;
            }

            return await RemoveAwaitFromCallersAsync(
                newDocument, annotation, cancellationToken).ConfigureAwait(false);
        }

        private static async Task<Solution> RemoveAwaitFromCallersAsync(
            Document document, SyntaxAnnotation annotation, CancellationToken cancellationToken)
        {
            var syntaxRoot = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
            var methodDeclaration = syntaxRoot.GetAnnotatedNodes(annotation).FirstOrDefault();
            if (methodDeclaration != null)
            {
                var semanticModel = await document.GetSemanticModelAsync(cancellationToken).ConfigureAwait(false);

                if (semanticModel.GetDeclaredSymbol(methodDeclaration, cancellationToken) is IMethodSymbol methodSymbol)
                {
                    var references = await SymbolFinder.FindRenamableReferencesAsync(
                        methodSymbol, document.Project.Solution, cancellationToken).ConfigureAwait(false);

                    var referencedSymbol = references.FirstOrDefault(r => Equals(r.Definition, methodSymbol));
                    if (referencedSymbol != null)
                    {
                        return await RemoveAwaitFromCallersAsync(
                            document.Project.Solution, referencedSymbol.Locations.ToImmutableArray(), cancellationToken).ConfigureAwait(false);
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
            var syntaxFactsService = document.GetLanguageService<ISyntaxFactsService>();
            var root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);

            var editor = new SyntaxEditor(root, currentSolution.Workspace.Services);

            foreach (var location in group)
            {
                RemoveAwaitFromCallerIfPresent(editor, syntaxFactsService, root, location, cancellationToken);
            }

            var newRoot = editor.GetChangedRoot();
            return currentSolution.WithDocumentSyntaxRoot(document.Id, newRoot);
        }

        private static void RemoveAwaitFromCallerIfPresent(
            SyntaxEditor editor, ISyntaxFactsService syntaxFacts,
            SyntaxNode root, ReferenceLocation referenceLocation,
            CancellationToken cancellationToken)
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
                var awaitExpression = invocationExpression.Parent;
                editor.ReplaceNode(awaitExpression, (currentAwaitExpression, generator) =>
                    syntaxFacts.GetExpressionOfAwaitExpression(currentAwaitExpression)
                               .WithTriviaFrom(currentAwaitExpression));
            }
            else if (syntaxFacts.IsExpressionOfMemberAccessExpression(invocationExpression))
            {
                // Check for the .ConfigureAwait case.
                var parentMemberAccessExpression = invocationExpression.Parent;
                var parentMemberAccessExpressionNameNode = syntaxFacts.GetNameOfMemberAccessExpression(
                    parentMemberAccessExpression);

                var parentMemberAccessExpressionName = syntaxFacts.GetIdentifierOfSimpleName(parentMemberAccessExpressionNameNode).ValueText;
                if (parentMemberAccessExpressionName == nameof(Task.ConfigureAwait))
                {
                    var parentExpression = parentMemberAccessExpression.Parent;
                    if (syntaxFacts.IsExpressionOfAwaitExpression(parentExpression))
                    {
                        var awaitExpression = parentExpression.Parent;
                        editor.ReplaceNode(awaitExpression, (currentAwaitExpression, generator) =>
                        {
                            var currentConfigureAwaitInvocation = syntaxFacts.GetExpressionOfAwaitExpression(currentAwaitExpression);
                            var currentMemberAccess = syntaxFacts.GetExpressionOfInvocationExpression(currentConfigureAwaitInvocation);
                            var currentInvocationExpression = syntaxFacts.GetExpressionOfMemberAccessExpression(currentMemberAccess);
                            return currentInvocationExpression.WithTriviaFrom(currentAwaitExpression);
                        });
                    }
                }
            }
        }

        private class MyCodeAction : CodeAction.SolutionChangeAction
        {
            public MyCodeAction(Func<CancellationToken, Task<Solution>> createChangedSolution)
                : base(FeaturesResources.Make_method_synchronous, createChangedSolution, AbstractMakeMethodSynchronousCodeFixProvider.EquivalenceKey)
            {
            }
        }
    }
}
