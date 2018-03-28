// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.Rename;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.MakeMethodAsynchronous
{
    internal abstract class AbstractMakeMethodAsynchronousCodeFixProvider : CodeFixProvider
    {
        protected abstract bool IsAsyncSupportingFunctionSyntax(SyntaxNode node);
        protected abstract SyntaxNode AddAsyncTokenAndFixReturnType(
            bool keepVoid, IMethodSymbol methodSymbolOpt, SyntaxNode node,
            INamedTypeSymbol taskType, INamedTypeSymbol taskOfTType, INamedTypeSymbol valueTaskOfTType);

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
            var (taskType, taskOfTType, valueTaskOfTTypeOpt) = GetTaskTypes(compilation);
            if (taskType == null || taskOfTType == null)
            {
                return;
            }

            var symbol = semanticModel.GetDeclaredSymbol(node, cancellationToken) as IMethodSymbol;

            // If it's a void returning method, offer to keep the void return type, or convert to 
            // a Task return type.
            bool isOrdinaryOrLocalFunction = symbol.IsOrdinaryMethodOrLocalFunction();
            if (isOrdinaryOrLocalFunction && symbol.ReturnsVoid)
            {
                context.RegisterCodeFix(
                    new MyCodeAction(GetMakeAsyncTaskFunctionResource(), c => FixNodeAsync(
                        context.Document, diagnostic, keepVoid: false, cancellationToken: c)),
                    context.Diagnostics);

                context.RegisterCodeFix(
                    new MyCodeAction(GetMakeAsyncVoidFunctionResource(), c => FixNodeAsync(
                        context.Document, diagnostic, keepVoid: true, cancellationToken: c)),
                    context.Diagnostics);
            }
            else
            {
                context.RegisterCodeFix(
                    new MyCodeAction(GetMakeAsyncTaskFunctionResource(), c => FixNodeAsync(
                        context.Document, diagnostic, keepVoid: false, cancellationToken: c)),
                    context.Diagnostics);
            }
        }

        private (INamedTypeSymbol taskType, INamedTypeSymbol taskOfTType, INamedTypeSymbol valueTaskOfTTypeOpt) GetTaskTypes(Compilation compilation)
        {
            var taskType = compilation.TaskType();
            var taskOfTType = compilation.TaskOfTType();
            var valueTaskOfTType = compilation.ValueTaskOfTType();

            return (taskType, taskOfTType, valueTaskOfTType);
        }

        protected abstract string GetMakeAsyncTaskFunctionResource();

        protected abstract string GetMakeAsyncVoidFunctionResource();

        private const string AsyncSuffix = "Async";

        private async Task<Solution> FixNodeAsync(
            Document document, Diagnostic diagnostic,
            bool keepVoid, CancellationToken cancellationToken)
        {
            var node = GetContainingFunction(diagnostic, cancellationToken);

            // See if we're on an actual method declaration (otherwise we're on a lambda declaration).
            // If we're on a method declaration, we'll get an IMethodSymbol back.  In that case, check
            // if it has the 'Async' suffix, and remove that suffix if so.
            var semanticModel = await document.GetSemanticModelAsync(cancellationToken).ConfigureAwait(false);
            var methodSymbolOpt = semanticModel.GetDeclaredSymbol(node) as IMethodSymbol;

            bool isOrdinaryOrLocalFunction = methodSymbolOpt.IsOrdinaryMethodOrLocalFunction();
            if (isOrdinaryOrLocalFunction && !methodSymbolOpt.Name.EndsWith(AsyncSuffix))
            {
                return await RenameThenAddAsyncTokenAsync(
                    keepVoid, document, node, methodSymbolOpt, cancellationToken).ConfigureAwait(false);
            }
            else
            {
                return await AddAsyncTokenAsync(
                    keepVoid, document, methodSymbolOpt, node, cancellationToken).ConfigureAwait(false);
            }
        }

        private SyntaxNode GetContainingFunction(Diagnostic diagnostic, CancellationToken cancellationToken)
        {
            var token = diagnostic.Location.FindToken(cancellationToken);
            var node = token.GetAncestor(IsAsyncSupportingFunctionSyntax);
            return node;
        }

        private async Task<Solution> RenameThenAddAsyncTokenAsync(
            bool keepVoid, Document document, SyntaxNode node,
            IMethodSymbol methodSymbol, CancellationToken cancellationToken)
        {
            var name = methodSymbol.Name;
            var newName = name + AsyncSuffix;
            var solution = document.Project.Solution;

            // Store the path to this node.  That way we can find it post rename.
            var syntaxPath = new SyntaxPath(node);

            // Rename the method to add the 'Async' suffix, then add the 'async' keyword.
            var newSolution = await Renamer.RenameSymbolAsync(solution, methodSymbol, newName, solution.Options, cancellationToken).ConfigureAwait(false);
            var newDocument = newSolution.GetDocument(document.Id);
            var newRoot = await newDocument.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
            if (syntaxPath.TryResolve(newRoot, out SyntaxNode newNode))
            {
                var semanticModel = await newDocument.GetSemanticModelAsync(cancellationToken).ConfigureAwait(false);
                var newMethod = (IMethodSymbol)semanticModel.GetDeclaredSymbol(newNode, cancellationToken);
                return await AddAsyncTokenAsync(keepVoid, newDocument, newMethod, newNode, cancellationToken).ConfigureAwait(false);
            }

            return newSolution;
        }

        private async Task<Solution> AddAsyncTokenAsync(
            bool keepVoid, Document document, IMethodSymbol methodSymbolOpt,
            SyntaxNode node, CancellationToken cancellationToken)
        {
            var compilation = await document.Project.GetCompilationAsync(cancellationToken).ConfigureAwait(false);
            var (taskType, taskOfTType, valueTaskOfTType) = GetTaskTypes(compilation);

            var newNode = AddAsyncTokenAndFixReturnType(keepVoid, methodSymbolOpt, node, taskType, taskOfTType, valueTaskOfTType);

            var root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
            var newRoot = root.ReplaceNode(node, newNode);

            var newDocument = document.WithSyntaxRoot(newRoot);
            return newDocument.Project.Solution;
        }

        protected static bool IsTaskLike(
            ITypeSymbol returnType, INamedTypeSymbol taskType,
            INamedTypeSymbol taskOfTType, INamedTypeSymbol valueTaskOfTType)
        {
            if (returnType.Equals(taskType))
            {
                return true;
            }

            if (returnType.OriginalDefinition.Equals(taskOfTType))
            {
                return true;
            }

            if (returnType.OriginalDefinition.Equals(valueTaskOfTType))
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

        private class MyCodeAction : CodeAction.SolutionChangeAction
        {
            public MyCodeAction(string title, Func<CancellationToken, Task<Solution>> createChangedSolution)
                : base(title, createChangedSolution, equivalenceKey: title)
            {
            }
        }
    }
}
