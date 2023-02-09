﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.LanguageService;
using Microsoft.CodeAnalysis.Rename;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.MakeMethodAsynchronous
{
    internal abstract partial class AbstractMakeMethodAsynchronousCodeFixProvider : CodeFixProvider
    {
        protected abstract bool IsSupportedDiagnostic(Diagnostic diagnostic, CancellationToken cancellationToken);
        protected abstract bool IsAsyncSupportingFunctionSyntax(SyntaxNode node);

        protected abstract string GetMakeAsyncTaskFunctionResource();
        protected abstract string GetMakeAsyncVoidFunctionResource();

        protected abstract bool IsAsyncReturnType(ITypeSymbol type, KnownTypes knownTypes);

        protected abstract SyntaxNode AddAsyncTokenAndFixReturnType(
            bool keepVoid, IMethodSymbol methodSymbol, SyntaxNode node, KnownTypes knownTypes, CancellationToken cancellationToken);

        public override FixAllProvider GetFixAllProvider() => WellKnownFixAllProviders.BatchFixer;

        public override async Task RegisterCodeFixesAsync(CodeFixContext context)
        {
            var document = context.Document;
            var diagnostic = context.Diagnostics.First();
            var cancellationToken = context.CancellationToken;

            if (!IsSupportedDiagnostic(diagnostic, cancellationToken))
                return;

            var node = GetContainingFunction(diagnostic, cancellationToken);
            if (node == null)
                return;

            var semanticModel = await document.GetRequiredSemanticModelAsync(cancellationToken).ConfigureAwait(false);
            var compilation = semanticModel.Compilation;

            // Find the symbols for Task, Task<T> and ValueTask<T>.  Note that the first
            // two are mandatory (since we need them to generate the return types for our
            // method if we convert it.  The last is optional.  It is only needed to know
            // if our member is already Task-Like, and that functionality recognizes
            // ValueTask if it is available, but does not care if it is not.
            var knownTypes = new KnownTypes(compilation);
            if (knownTypes.TaskType == null || knownTypes.TaskOfTType == null)
                return;

            var methodSymbol = GetMethodSymbol(semanticModel, node, cancellationToken);
            if (methodSymbol is null)
                return;

            // Heuristic to recognize the common case for entry point method
            var isEntryPoint = methodSymbol.IsStatic && IsLikelyEntryPointName(methodSymbol.Name, document);

            // Offer to convert to a Task return type.
            var taskTitle = GetMakeAsyncTaskFunctionResource();
            context.RegisterCodeFix(
                CodeAction.Create(
                    taskTitle,
                    cancellationToken => FixNodeAsync(document, diagnostic, keepVoid: false, isEntryPoint, cancellationToken),
                    taskTitle),
                context.Diagnostics);

            // If it's a void returning method (and not an entry point), also offer to keep the void return type
            if (methodSymbol.IsOrdinaryMethodOrLocalFunction() && methodSymbol.ReturnsVoid && !isEntryPoint)
            {
                var asyncVoidTitle = GetMakeAsyncVoidFunctionResource();
                context.RegisterCodeFix(
                    CodeAction.Create(
                        asyncVoidTitle,
                        cancellationToken => FixNodeAsync(document, diagnostic, keepVoid: true, isEntryPoint: false, cancellationToken),
                        asyncVoidTitle),
                    context.Diagnostics);
            }
        }

        private static IMethodSymbol? GetMethodSymbol(SemanticModel semanticModel, SyntaxNode node, CancellationToken cancellationToken)
        {
            // GetDeclaredSymbol for methods/local-functions.  GetSymbolInfo for lambdas.
            var symbol = semanticModel.GetDeclaredSymbol(node, cancellationToken) ?? semanticModel.GetSymbolInfo(node, cancellationToken).GetAnySymbol();
            return symbol as IMethodSymbol;
        }

        private static bool IsLikelyEntryPointName(string name, Document document)
        {
            var syntaxFacts = document.GetRequiredLanguageService<ISyntaxFactsService>();
            return syntaxFacts.StringComparer.Equals(name, "Main");
        }

        private const string AsyncSuffix = "Async";

        private async Task<Solution> FixNodeAsync(
            Document document,
            Diagnostic diagnostic,
            bool keepVoid,
            bool isEntryPoint,
            CancellationToken cancellationToken)
        {
            var node = GetContainingFunction(diagnostic, cancellationToken);
            Contract.ThrowIfNull(node);

            // See if we're on an actual method declaration (otherwise we're on a lambda declaration).
            // If we're on a method declaration, we'll get an IMethodSymbol back.  In that case, check
            // if it has the 'Async' suffix, and remove that suffix if so.
            var semanticModel = await document.GetRequiredSemanticModelAsync(cancellationToken).ConfigureAwait(false);
            var methodSymbol = GetMethodSymbol(semanticModel, node, cancellationToken);
            Contract.ThrowIfNull(methodSymbol);

            var knownTypes = new KnownTypes(semanticModel.Compilation);

            return NeedsRename()
                ? await RenameThenAddAsyncTokenAsync(keepVoid, document, node, methodSymbol, knownTypes, cancellationToken).ConfigureAwait(false)
                : await AddAsyncTokenAsync(keepVoid, document, methodSymbol, knownTypes, node, cancellationToken).ConfigureAwait(false);

            bool NeedsRename()
            {
                // We don't need to rename methods that don't have a name
                if (!methodSymbol.IsOrdinaryMethodOrLocalFunction())
                    return false;

                // We don't need to rename methods that already have an Async suffix
                if (methodSymbol.Name.EndsWith(AsyncSuffix))
                    return false;

                // We don't need to rename entry point methods
                if (isEntryPoint)
                    return false;

                // Only rename if the return type will change
                if (methodSymbol.ReturnsVoid)
                    return !keepVoid;

                return !IsAsyncReturnType(methodSymbol.ReturnType, knownTypes);
            }
        }

        private SyntaxNode? GetContainingFunction(Diagnostic diagnostic, CancellationToken cancellationToken)
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

            var newDocument = newSolution.GetRequiredDocument(document.Id);
            var newRoot = await newDocument.GetRequiredSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
            if (syntaxPath.TryResolve(newRoot, out SyntaxNode? newNode))
            {
                var semanticModel = await newDocument.GetRequiredSemanticModelAsync(cancellationToken).ConfigureAwait(false);
                var newMethod = (IMethodSymbol)semanticModel.GetRequiredDeclaredSymbol(newNode, cancellationToken);
                return await AddAsyncTokenAsync(keepVoid, newDocument, newMethod, knownTypes, newNode, cancellationToken).ConfigureAwait(false);
            }

            return newSolution;
        }

        private async Task<Solution> AddAsyncTokenAsync(
            bool keepVoid,
            Document document,
            IMethodSymbol methodSymbol,
            KnownTypes knownTypes,
            SyntaxNode node,
            CancellationToken cancellationToken)
        {
            var newNode = AddAsyncTokenAndFixReturnType(keepVoid, methodSymbol, node, knownTypes, cancellationToken);

            var root = await document.GetRequiredSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
            var newRoot = root.ReplaceNode(node, newNode);

            var newDocument = document.WithSyntaxRoot(newRoot);
            return newDocument.Project.Solution;
        }

        protected static bool IsTaskLike(ITypeSymbol returnType, KnownTypes knownTypes)
        {
            if (returnType.Equals(knownTypes.TaskType))
            {
                return true;
            }

            if (returnType.Equals(knownTypes.ValueTaskType))
            {
                return true;
            }

            if (returnType.OriginalDefinition.Equals(knownTypes.TaskOfTType))
            {
                return true;
            }

            if (returnType.OriginalDefinition.Equals(knownTypes.ValueTaskOfTTypeOpt))
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
