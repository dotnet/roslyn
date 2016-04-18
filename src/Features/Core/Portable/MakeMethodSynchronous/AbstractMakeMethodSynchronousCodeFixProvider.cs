// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.Formatting;
using Microsoft.CodeAnalysis.LanguageServices;
using Microsoft.CodeAnalysis.Rename;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.MakeMethodSynchronous
{
    internal abstract class AbstractMakeMethodSynchronousCodeFixProvider : CodeFixProvider
    {
        public static readonly string MakeMethodSynchronousKey = FeaturesResources.Make_method_synchronous;
        public static readonly string MakeMethodSynchronousChangingReturnTypeKey = FeaturesResources.Make_method_synchronous_changing_return_type;

        protected abstract bool IsMethodOrAnonymousFunction(SyntaxNode node);
        protected abstract SyntaxNode RemoveAsyncTokenAndFixReturnType(
            IMethodSymbol methodSymbolOpt, SyntaxNode node, bool changeReturnType);

        public override FixAllProvider GetFixAllProvider() => WellKnownFixAllProviders.BatchFixer;

        public override async Task RegisterCodeFixesAsync(CodeFixContext context)
        {
            var cancellationToken = context.CancellationToken;
            var document = context.Document;
            var diagnostic = context.Diagnostics.First();

            var token = diagnostic.Location.FindToken(cancellationToken);

            var semanticModel = await document.GetSemanticModelAsync(cancellationToken).ConfigureAwait(false);
            var node = token.GetAncestor(IsMethodOrAnonymousFunction);
            if (node == null)
            {
                return;
            }

            var methodSymbolOpt = semanticModel.GetDeclaredSymbol(node) as IMethodSymbol;
            if (methodSymbolOpt?.MethodKind == MethodKind.Ordinary)
            {
                var compilation = semanticModel.Compilation;
                var taskType = compilation.GetTypeByMetadataName("System.Threading.Tasks.Task");
                var taskOfTType = compilation.GetTypeByMetadataName("System.Threading.Tasks.Task`1");

                var returnType = methodSymbolOpt.ReturnType.OriginalDefinition;
                if (returnType.Equals(taskType) || returnType.Equals(taskOfTType))
                {
                    context.RegisterCodeFix(new MyCodeAction(
                        FeaturesResources.Make_method_synchronous_changing_return_type,
                        c => FixNodeAsync(context.Document, node, changeReturnType: true, cancellationToken: c),
                        MakeMethodSynchronousChangingReturnTypeKey), diagnostic);
                }
            }

            context.RegisterCodeFix(new MyCodeAction(
                FeaturesResources.Make_method_synchronous,
                c => FixNodeAsync(context.Document, node, changeReturnType: false, cancellationToken: c),
                MakeMethodSynchronousKey), diagnostic);
        }

        private const string AsyncSuffix = "Async";

        private async Task<Solution> FixNodeAsync(
            Document document, SyntaxNode node, bool changeReturnType, CancellationToken cancellationToken)
        {
            // See if we're on an actual method declaration (otherwise we're on a lambda declaration).
            // If we're on a method declaration, we'll get an IMethodSymbol back.  In that case, check
            // if it has the 'Async' suffix, and remove that suffix if so.
            var semanticModel = await document.GetSemanticModelAsync(cancellationToken).ConfigureAwait(false);
            var methodSymbolOpt = semanticModel.GetDeclaredSymbol(node) as IMethodSymbol;

            if (methodSymbolOpt != null)
            {
                var shouldRenameIfEndsWithAsync = changeReturnType || methodSymbolOpt.ReturnsVoid;

                if (shouldRenameIfEndsWithAsync &&
                    methodSymbolOpt.MethodKind == MethodKind.Ordinary &&
                    methodSymbolOpt.Name.Length > AsyncSuffix.Length &&
                    methodSymbolOpt.Name.EndsWith(AsyncSuffix))
                {
                    return await RenameThenRemoveAsyncTokenAsync(
                        document, node, methodSymbolOpt, changeReturnType, cancellationToken).ConfigureAwait(false);
                }
            }

            return await RemoveAsyncTokenAsync(
                document, methodSymbolOpt, node, changeReturnType, cancellationToken).ConfigureAwait(false);
        }

        private async Task<Solution> RenameThenRemoveAsyncTokenAsync(
            Document document, SyntaxNode node, IMethodSymbol methodSymbol, bool changeReturnType, 
            CancellationToken cancellationToken)
        {
            var name = methodSymbol.Name;
            var newName = name.Substring(0, name.Length - AsyncSuffix.Length);
            var solution = document.Project.Solution;
            var options = solution.Workspace.Options;

            // Store the path to this node.  That way we can find it post rename.
            var syntaxPath = new SyntaxPath(node);

            // Rename the method to remove the 'Async' suffix, then remove the 'async' keyword.
            var newSolution = await Renamer.RenameSymbolAsync(solution, methodSymbol, newName, options, cancellationToken).ConfigureAwait(false);
            var newDocument = newSolution.GetDocument(document.Id);
            var newRoot = await newDocument.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);

            SyntaxNode newNode;
            if (syntaxPath.TryResolve<SyntaxNode>(newRoot, out newNode))
            {
                var semanticModel = await newDocument.GetSemanticModelAsync(cancellationToken).ConfigureAwait(false);
                var newMethod = (IMethodSymbol)semanticModel.GetDeclaredSymbol(newNode, cancellationToken);
                return await RemoveAsyncTokenAsync(newDocument, newMethod, newNode, changeReturnType, cancellationToken).ConfigureAwait(false);
            }

            return newSolution;
        }

        private async Task<Solution> RemoveAsyncTokenAsync(
            Document document, IMethodSymbol methodSymbolOpt, SyntaxNode node, bool changeReturnType,
            CancellationToken cancellationToken)
        {
            var compilation = await document.Project.GetCompilationAsync(cancellationToken).ConfigureAwait(false);
            var taskType = compilation.GetTypeByMetadataName("System.Threading.Tasks.Task");
            var taskOfTType = compilation.GetTypeByMetadataName("System.Threading.Tasks.Task`1");

            var newNode = RemoveAsyncTokenAndFixReturnType(methodSymbolOpt, node, changeReturnType)
                .WithAdditionalAnnotations(Formatter.Annotation);

            var root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
            var newRoot = root.ReplaceNode(node, newNode);

            var newDocument = document.WithSyntaxRoot(newRoot);
            return newDocument.Project.Solution;
        }

        private class MyCodeAction : CodeAction.SolutionChangeAction
        {
            public MyCodeAction(
                string title,
                Func<CancellationToken, Task<Solution>> createChangedSolution,
                string equivalenceKey)
                : base(title, createChangedSolution, equivalenceKey)
            {
            }
        }
    }
}
