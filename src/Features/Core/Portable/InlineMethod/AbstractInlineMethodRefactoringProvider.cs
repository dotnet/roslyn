// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeRefactorings;
using Microsoft.CodeAnalysis.Editing;
using Microsoft.CodeAnalysis.FindSymbols;
using Microsoft.CodeAnalysis.Operations;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.InlineMethod
{
    internal abstract partial class AbstractInlineMethodRefactoringProvider : CodeRefactoringProvider
    {
        protected abstract Task<SyntaxNode?> GetInvocationExpressionSyntaxNodeAsync(CodeRefactoringContext context);

        /// <summary>
        /// Check if the <param name="calleeMethodDeclarationSyntaxNode"/> has only one expression or it is using arrow expression.
        /// </summary>
        protected abstract bool IsMethodContainsOneStatement(SyntaxNode calleeMethodDeclarationSyntaxNode);

        /// <summary>
        /// Extract the expression from the single one statement or Arrow Expression in <param name="calleeMethodDeclarationSyntaxNode"/>.
        /// </summary>
        protected abstract SyntaxNode ExtractExpressionFromMethodDeclaration(SyntaxNode calleeMethodDeclarationSyntaxNode);

        protected abstract ImmutableArray<IInlineChange> ComputeInlineChanges(
            SyntaxNode calleeInvocationExpressionSyntaxNode,
            SemanticModel semanticModel,
            IMethodSymbol calleeMethodSymbol,
            SyntaxNode calleeMethodDeclarationSyntaxNode,
            CancellationToken cancellation);

        public override async Task ComputeRefactoringsAsync(CodeRefactoringContext context)
        {
            var (document, _, cancellationToken) = context;
            var calleeMethodInvocationSyntaxNode = await GetInvocationExpressionSyntaxNodeAsync(context).ConfigureAwait(false);
            if (calleeMethodInvocationSyntaxNode == null)
            {
                return;
            }

            var semanticModel = await document.GetSemanticModelAsync(cancellationToken).ConfigureAwait(false);
            if (semanticModel == null)
            {
                return;
            }

            var symbolInfo = semanticModel.GetSymbolInfo(calleeMethodInvocationSyntaxNode);
            var methodSymbol = symbolInfo.Symbol;
            if (methodSymbol == null && symbolInfo.CandidateSymbols.Any())
            {
                methodSymbol = symbolInfo.CandidateSymbols[0];
            }

            if (methodSymbol == null
                || methodSymbol.DeclaredAccessibility != Accessibility.Private
                || !methodSymbol.IsOrdinaryMethod())
            {
                return;
            }

            if (methodSymbol is IMethodSymbol calleeMethodInvocationSymbol)
            {
                var calleeMethodDeclarationSyntaxNodes = await Task.WhenAll(calleeMethodInvocationSymbol.DeclaringSyntaxReferences
                    .Select(reference => reference.GetSyntaxAsync())).ConfigureAwait(false);

                if (calleeMethodDeclarationSyntaxNodes == null || calleeMethodDeclarationSyntaxNodes.Length != 1)
                {
                    return;
                }

                var calleeMethodDeclarationSyntaxNode = calleeMethodDeclarationSyntaxNodes[0];

                if (!IsMethodContainsOneStatement(calleeMethodDeclarationSyntaxNode))
                {
                    return;
                }

                var codeAction = new CodeAction.DocumentChangeAction(
                    string.Format(FeaturesResources.Inline_0, calleeMethodInvocationSymbol.ToNameDisplayString()),
                    cancellationToken => InlineMethodAsync(document, semanticModel!, calleeMethodInvocationSyntaxNode, calleeMethodInvocationSymbol, calleeMethodDeclarationSyntaxNode, cancellationToken));

                context.RegisterRefactoring(codeAction);
            }
        }

        protected static Dictionary<ISymbol, string> ComputeRenameTable(
            SyntaxNode calleeInvocationExpressionSyntaxNode,
            SemanticModel semanticModel,
            SyntaxNode calleeDeclarationSyntaxNode,
            ImmutableArray<(IParameterSymbol parameter, string argumentName)> parameterNeedRename,
            ImmutableArray<IParameterSymbol> parametersNeedMoveToCaller,
            CancellationToken cancellationToken)
        {
            var operationVisitor = new VariableDeclaratorOperationVisitor(cancellationToken);
            var calleeOperation = semanticModel.GetOperation(calleeDeclarationSyntaxNode, cancellationToken);
            var invocationSpanEnd = calleeInvocationExpressionSyntaxNode.Span.End;
            var localSymbolNamesOfCaller = semanticModel.LookupSymbols(invocationSpanEnd)
                .Where(symbol => !symbol.IsInaccessibleLocal(invocationSpanEnd))
                .Select(symbol => symbol.Name)
                .ToImmutableHashSet();

            var renameTable = new Dictionary<ISymbol, string>();
            foreach (var (parameter, argumentName) in parameterNeedRename)
            {
                renameTable[parameter] = argumentName;
            }

            // 1. Make sure no local variable conflict
            var parameterNames = parameterNeedRename
                .Select(paramAndArg => paramAndArg.argumentName).ToSet();
            var localSymbolsOfCallee = operationVisitor.FindAllLocalSymbols(calleeOperation);
            foreach (var localSymbol in localSymbolsOfCallee)
            {
                var localSymbolName = localSymbol.Name;
                while (parameterNames.Contains(localSymbolName))
                {
                    localSymbolName = GenerateNewName(localSymbolName);
                }

                renameTable[localSymbol] = localSymbolName;
                parameterNames.Add(localSymbolName);
            }

            // 2. Make sure no variable conflict after the parameter is moved to caller
            foreach (var parameterSymbol in parametersNeedMoveToCaller)
            {
                var parameterName = parameterSymbol.Name;
                while (localSymbolNamesOfCaller.Contains(parameterName) || parameterNames.Contains(parameterName))
                {
                    parameterName = GenerateNewName(parameterName);
                }

                renameTable[parameterSymbol] = parameterName;
                parameterNames.Add(parameterName);
            }

            return renameTable;
        }

        /// <summary>
        /// Generate a new identifier name. If <param name="identifierName"/> has a number suffix,
        /// increase it by 1. Otherwise, append 1 to it.
        /// </summary>
        private static string GenerateNewName(string identifierName)
        {
            var stack = new Stack<char>();
            for (int i = identifierName.Length - 1; i >= 0; i--)
            {
                var currentCharacter = identifierName[i];
                if (char.IsNumber(currentCharacter))
                {
                    stack.Push(currentCharacter);
                }
                else
                {
                    break;
                }
            }

            var suffixNumber = stack.IsEmpty() ? 1 : int.Parse(new string(stack.ToArray())) + 1;
            return identifierName.Substring(0, identifierName.Length - stack.Count) + suffixNumber;
        }

        private async Task<Document> InlineMethodAsync(
            Document document,
            SemanticModel semanticModel,
            SyntaxNode calleeMethodInvocationSyntaxNode,
            IMethodSymbol calleeMethodSymbol,
            SyntaxNode calleeMethodDeclarationSyntaxNode,
            CancellationToken cancellationToken)
        {
            var replacementChanges = ComputeInlineChanges(
                calleeMethodInvocationSyntaxNode, semanticModel, calleeMethodSymbol, calleeMethodDeclarationSyntaxNode, cancellationToken);

            var calleeMethodDeclarationNodeEditor = new SyntaxEditor(
                calleeMethodDeclarationSyntaxNode,
                document.Project.Solution.Workspace);

            foreach (var change in replacementChanges.OfType<ReplaceVariableChange>())
            {
                var allReferences =
                    await SymbolFinder.FindReferencesAsync(change.Symbol, document.Project.Solution, cancellationToken).ConfigureAwait(false);

                foreach (var nodeToReplace in change.SyntaxNodesToReplace)
                {
                    calleeMethodDeclarationNodeEditor.ReplaceNode(nodeToReplace, change.ReplacementLiteralExpression);
                }
            }

            var inlineMethodBody = ExtractExpressionFromMethodDeclaration(calleeMethodDeclarationNodeEditor.GetChangedRoot());

            var documentEditor = await DocumentEditor.CreateAsync(document, cancellationToken).ConfigureAwait(false);
            foreach (var change in replacementChanges.OfType<ExtractDeclarationChange>())
            {
                documentEditor.InsertBefore(calleeMethodInvocationSyntaxNode, change.DeclarationStatement);
            }

            documentEditor.ReplaceNode(calleeMethodInvocationSyntaxNode, inlineMethodBody);
            return documentEditor.GetChangedDocument();
        }

        private class VariableDeclaratorOperationVisitor : OperationWalker
        {
            private readonly CancellationToken _cancellationToken;
            private readonly HashSet<ILocalSymbol> _localSymbols;

            public VariableDeclaratorOperationVisitor(CancellationToken cancellationToken)
            {
                _cancellationToken = cancellationToken;
                _localSymbols = new HashSet<ILocalSymbol>();
            }

            public ImmutableArray<ILocalSymbol> FindAllLocalSymbols(IOperation? operation)
            {
                if (operation != null)
                {
                    Visit(operation);
                }

                return _localSymbols.ToImmutableArray();
            }

            public override void Visit(IOperation operation)
            {
                _cancellationToken.ThrowIfCancellationRequested();
                if (operation is IVariableDeclaratorOperation variableDeclaratorOperation)
                {
                    _localSymbols.Add(variableDeclaratorOperation.Symbol);
                }

                base.Visit(operation);
            }
        }
    }
}
