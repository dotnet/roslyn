// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;
using Microsoft.CodeAnalysis.CodeActions;

namespace Microsoft.CodeAnalysis.CodeRefactorings.ConvertIfToSwitch
{
    internal abstract class AbstractConvertIfToSwitchCodeRefactoringProvider<
        TStatementSyntax, TIfStatementSyntax, TExpressionSyntax, TPattern> : CodeRefactoringProvider
        where TPattern : class
        where TExpressionSyntax : SyntaxNode
        where TIfStatementSyntax : SyntaxNode
    {
        public sealed override async Task ComputeRefactoringsAsync(CodeRefactoringContext context)
        {
            var document = context.Document;
            if (document.Project.Solution.Workspace.Kind == WorkspaceKind.MiscellaneousFiles)
            {
                return;
            }

            var cancellationToken = context.CancellationToken;
            var root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);

            var ifStatement = root.FindNode(context.Span).FirstAncestorOrSelf<TIfStatementSyntax>();
            if (ifStatement == null)
            {
                return;
            }

            if (ifStatement.ContainsDiagnostics)
            {
                return;
            }

            if (!ifStatement.GetFirstToken().GetLocation().SourceSpan.IntersectsWith(context.Span))
            {
                return;
            }

            var semanticModel = await document.GetSemanticModelAsync(cancellationToken).ConfigureAwait(false);
            if (!CanConvertIfToSwitch(ifStatement, semanticModel))
            {
                return;
            }

            var switchDefaultBody = default(TStatementSyntax);
            var switchExpression = default(TExpressionSyntax);
            var switchSections = new List<(List<TPattern>, TStatementSyntax)>();

            // Iterate over if-else statement chain.
            foreach (var (body, condition) in GetIfElseStatementChain(ifStatement))
            {
                if (condition == null)
                {
                    switchDefaultBody = body;
                    break;
                }

                var operands = GetLogicalOrExpressionOperands(condition);
                var patterns = new List<TPattern>();

                // Iterate over "||" operands to make a case label per each condition.
                foreach (var operand in operands.Reverse())
                {
                    var pattern = CreatePatternFromExpression(operand, semanticModel, ref switchExpression);
                    if (pattern == null)
                    {
                        return;
                    }

                    patterns.Add(pattern);
                }

                switchSections.Add((patterns, body));
            }

            context.RegisterRefactoring(new MyCodeAction(Title, c =>
                UpdateDocumentAsync(root, document, ifStatement, switchDefaultBody, switchExpression, semanticModel, switchSections)));
        }

        protected bool AreEquivalent(TExpressionSyntax expression, ref TExpressionSyntax switchExpression)
        {
            // If we have not figured the switch expression yet,
            // we will assume that the first expression is the one.
            if (switchExpression == null)
            {
                switchExpression = expression;
                return true;
            }

            return AreEquivalentCore(expression, switchExpression);
        }

        private static bool IsConstant(TExpressionSyntax node, SemanticModel semanticModel)
            => semanticModel.GetConstantValue(node).HasValue;

        protected static bool TryDetermineConstant(
            TExpressionSyntax expression1,
            TExpressionSyntax expression2,
            SemanticModel semanticModel,
            out TExpressionSyntax constant,
            out TExpressionSyntax expression)
        {
            (constant, expression) =
                    IsConstant(expression1, semanticModel) ? (expression1, expression2) :
                    IsConstant(expression2, semanticModel) ? (expression2, expression1) :
                    default((TExpressionSyntax, TExpressionSyntax));

            return constant != null;
        }

        protected abstract bool AreEquivalentCore(TExpressionSyntax expression, TExpressionSyntax switchExpression);
        protected abstract bool CanConvertIfToSwitch(TIfStatementSyntax ifStatement, SemanticModel semanticModel);
        protected abstract IEnumerable<TExpressionSyntax> GetLogicalOrExpressionOperands(
            TExpressionSyntax syntaxNode);

        protected abstract IEnumerable<(TStatementSyntax, TExpressionSyntax)> GetIfElseStatementChain(
            TIfStatementSyntax ifStatement);

        protected abstract TPattern CreatePatternFromExpression(
            TExpressionSyntax operand, SemanticModel semanticModel, ref TExpressionSyntax switchExpression);

        protected abstract SyntaxNode CreateSwitchStatement(
            TStatementSyntax switchDefaultBody,
            TExpressionSyntax switchExpression,
            SemanticModel semanticModel,
            List<(List<TPattern> patterns, TStatementSyntax body)> sections);

        private Task<Document> UpdateDocumentAsync(
            SyntaxNode root,
            Document document,
            TIfStatementSyntax ifStatement,
            TStatementSyntax switchDefaultBody,
            TExpressionSyntax switchExpression,
            SemanticModel semanticModel,
            List<(List<TPattern>, TStatementSyntax)> sections)
        {
            var @switch = CreateSwitchStatement(switchDefaultBody, switchExpression, semanticModel, sections);
            var newRoot = root.ReplaceNode(ifStatement, @switch);
            return Task.FromResult(document.WithSyntaxRoot(newRoot));
        }

        protected abstract string Title { get; }

        protected sealed class MyCodeAction : CodeAction.DocumentChangeAction
        {
            public MyCodeAction(string title, Func<CancellationToken, Task<Document>> createChangedDocument)
                : base(title, createChangedDocument)
            {
            }
        }
    }
}