// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.LanguageServices;
using Microsoft.CodeAnalysis.Rename;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.MakeMethodAsynchronous
{
    internal abstract partial class AbstractMakeMethodAsynchronousCodeFixProvider : CodeFixProvider
    {
        protected abstract bool IsAsyncSupportingFunctionSyntax(SyntaxNode node);

        protected abstract bool IsAsyncReturnType(ITypeSymbol type, KnownTypes knownTypes);

        protected abstract SyntaxNode AddAsyncTokenAndFixReturnType(
            bool keepVoid, IMethodSymbol methodSymbolOpt, SyntaxNode node,
            KnownTypes knownTypes);

        public override FixAllProvider GetFixAllProvider() => WellKnownFixAllProviders.BatchFixer;

        public override async Task RegisterCodeFixesAsync(CodeFixContext context)
        {
            var diagnostic = context.Diagnostics.First();
            var cancellationToken = context.CancellationToken;

            var node = GetContainingFunction(diagnostic, cancellationToken);
            if (node == null)
            {
                return;
            }

            var semanticModel = await context.Document.GetSemanticModelAsync(cancellationToken).ConfigureAwait(false);
            var compilation = semanticModel.Compilation;

            // Find the symbols for Task, Task<T> and ValueTask<T>.  Note that the first
            // two are mandatory (since we need them to generate the return types for our
            // method if we convert it.  The last is optional.  It is only needed to know
            // if our member is already Task-Like, and that functionality recognizes
            // ValueTask if it is available, but does not care if it is not.
            var knownTypes = new KnownTypes(compilation);
            if (knownTypes._taskType == null || knownTypes._taskOfTType == null)
            {
                return;
            }

            var symbol = semanticModel.GetDeclaredSymbol(node, cancellationToken) as IMethodSymbol;

            // Heuristic to recognize the common case for entry point method
            var isEntryPoint = symbol != null && symbol.IsStatic && IsLikelyEntryPointName(symbol.Name, context.Document);

            // Offer to convert to a Task return type.
            var taskTitle = GetMakeAsyncTaskFunctionResource();
            context.RegisterCodeFix(
                CodeAction.Create(
                    taskTitle,
                    c => FixNodeAsync(context.Document, diagnostic, keepVoid: false, isEntryPoint, cancellationToken: c),
                    taskTitle),
                context.Diagnostics);

            // If it's a void returning method (and not an entry point), also offer to keep the void return type
            var isOrdinaryOrLocalFunction = symbol.IsOrdinaryMethodOrLocalFunction();
            if (isOrdinaryOrLocalFunction && symbol.ReturnsVoid && !isEntryPoint)
            {
                var asyncVoidTitle = GetMakeAsyncVoidFunctionResource();
                context.RegisterCodeFix(
                    CodeAction.Create(
                        asyncVoidTitle,
                        c => FixNodeAsync(context.Document, diagnostic, keepVoid: true, isEntryPoint: false, cancellationToken: c),
                        asyncVoidTitle),
                    context.Diagnostics);
            }
        }

        private static bool IsLikelyEntryPointName(string name, Document document)
        {
            var syntaxFacts = document.GetLanguageService<ISyntaxFactsService>();
            return syntaxFacts.StringComparer.Equals(name, "Main");
        }

        protected abstract string GetMakeAsyncTaskFunctionResource();

        protected abstract string GetMakeAsyncVoidFunctionResource();

        private const string AsyncSuffix = "Async";

        private async Task<Solution> FixNodeAsync(
            Document document, Diagnostic diagnostic,
            bool keepVoid, bool isEntryPoint, CancellationToken cancellationToken)
        {
            var node = GetContainingFunction(diagnostic, cancellationToken);

            // See if we're on an actual method declaration (otherwise we're on a lambda declaration).
            // If we're on a method declaration, we'll get an IMethodSymbol back.  In that case, check
            // if it has the 'Async' suffix, and remove that suffix if so.
            var semanticModel = await document.GetSemanticModelAsync(cancellationToken).ConfigureAwait(false);
            var methodSymbolOpt = semanticModel.GetDeclaredSymbol(node, cancellationToken) as IMethodSymbol;
            var compilation = await document.Project.GetCompilationAsync(cancellationToken).ConfigureAwait(false);
            var knownTypes = new KnownTypes(compilation);

            if (NeedsRename(this, methodSymbolOpt, keepVoid, isEntryPoint, in knownTypes))
            {
                return await RenameThenAddAsyncTokenAsync(
                    keepVoid, document, node, methodSymbolOpt, knownTypes, cancellationToken).ConfigureAwait(false);
            }
            else
            {
                return await AddAsyncTokenAsync(
                    keepVoid, document, methodSymbolOpt, knownTypes, node, cancellationToken).ConfigureAwait(false);
            }

            static bool NeedsRename(AbstractMakeMethodAsynchronousCodeFixProvider @this, IMethodSymbol methodSymbol, bool keepVoid, bool isEntryPoint, in KnownTypes knownTypes)
            {
                if (!methodSymbol.IsOrdinaryMethodOrLocalFunction())
                {
                    // We don't need to rename methods that don't have a name
                    return false;
                }

                if (methodSymbol.Name.EndsWith(AsyncSuffix))
                {
                    // We don't need to rename methods that already have an Async suffix
                    return false;
                }

                if (isEntryPoint)
                {
                    // We don't need to rename entry point methods
                    return false;
                }

                // Only rename if the return type will change
                if (methodSymbol.ReturnsVoid)
                {
                    return !keepVoid;
                }
                else
                {
                    return !@this.IsAsyncReturnType(methodSymbol.ReturnType, knownTypes);
                }
            }
        }

        private SyntaxNode GetContainingFunction(Diagnostic diagnostic, CancellationToken cancellationToken)
        {
            var token = diagnostic.Location.FindToken(cancellationToken);
            var node = token.GetAncestor(IsAsyncSupportingFunctionSyntax);
            return node;
        }

        private async Task<Solution> RenameThenAddAsyncTokenAsync(
            bool keepVoid,
            Document document,
            SyntaxNode node,
            IMethodSymbol methodSymbol,
            KnownTypes knownTypes,
            CancellationToken cancellationToken)
        {
            var name = methodSymbol.Name;
            var newName = name + AsyncSuffix;
            var solution = document.Project.Solution;

            // Store the path to this node.  That way we can find it post rename.
            var syntaxPath = new SyntaxPath(node);

            // Rename the method to add the 'Async' suffix, then add the 'async' keyword.
            var newSolution = await Renamer.RenameSymbolAsync(solution, methodSymbol, new SymbolRenameOptions(), newName, cancellationToken).ConfigureAwait(false);

            var newDocument = newSolution.GetDocument(document.Id);
            var newRoot = await newDocument.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
            if (syntaxPath.TryResolve(newRoot, out SyntaxNode newNode))
            {
                var semanticModel = await newDocument.GetSemanticModelAsync(cancellationToken).ConfigureAwait(false);
                var newMethod = (IMethodSymbol)semanticModel.GetDeclaredSymbol(newNode, cancellationToken);
                return await AddAsyncTokenAsync(keepVoid, newDocument, newMethod, knownTypes, newNode, cancellationToken).ConfigureAwait(false);
            }

            return newSolution;
        }

        private async Task<Solution> AddAsyncTokenAsync(
            bool keepVoid,
            Document document,
            IMethodSymbol methodSymbolOpt,
            KnownTypes knownTypes,
            SyntaxNode node,
            CancellationToken cancellationToken)
        {
            var newNode = AddAsyncTokenAndFixReturnType(keepVoid, methodSymbolOpt, node, knownTypes);

            var root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
            var newRoot = root.ReplaceNode(node, newNode);

            var newDocument = document.WithSyntaxRoot(newRoot);
            return newDocument.Project.Solution;
        }

        protected static bool IsTaskLike(ITypeSymbol returnType, KnownTypes knownTypes)
        {
            if (returnType.Equals(knownTypes._taskType))
            {
                return true;
            }

            if (returnType.Equals(knownTypes._valueTaskType))
            {
                return true;
            }

            if (returnType.OriginalDefinition.Equals(knownTypes._taskOfTType))
            {
                return true;
            }

            if (returnType.OriginalDefinition.Equals(knownTypes._valueTaskOfTTypeOpt))
            {
                return true;
            }

            if (returnType.IsErrorType())
            {
                return returnType.Name.Equals("Task") ||
                       returnType.Name.Equals("ValueTask");
            }

            return false;
        }
    }
}
