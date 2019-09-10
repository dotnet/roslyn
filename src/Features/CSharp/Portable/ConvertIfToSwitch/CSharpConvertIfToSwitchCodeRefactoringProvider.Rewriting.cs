// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

#nullable enable

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using Microsoft.CodeAnalysis.CSharp.Extensions;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Operations;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp.ConvertIfToSwitch
{
    using static SyntaxFactory;

    internal sealed partial class CSharpConvertIfToSwitchCodeRefactoringProvider
    {
        public override SyntaxNode CreateSwitchExpressionStatement(SyntaxNode target, ImmutableArray<AnalyzedSwitchSection> sections)
        {
            return ReturnStatement(
                SwitchExpression((ExpressionSyntax)target, SeparatedList(sections.Select(AsSwitchExpressionArmSyntax))));
        }

        private static SwitchExpressionArmSyntax AsSwitchExpressionArmSyntax(AnalyzedSwitchSection section)
        {
            // In a switch expression, we expect only a single label
            Debug.Assert(section.Labels.IsDefault || section.Labels.Length == 1);
            var (pattern, whenClause) = section.Labels.IsDefault
                ? (DiscardPattern(), null)
                : (AsPatternSyntax(section.Labels[0].Pattern), AsWhenClause(section.Labels[0]));
            return SwitchExpressionArm(pattern, whenClause, AsExpressionSyntax(section.Body));
        }

        private static ExpressionSyntax AsExpressionSyntax(IOperation operation)
            => operation switch
            {
                IReturnOperation op => (ExpressionSyntax)op.ReturnedValue.Syntax,
                IThrowOperation op => ThrowExpression((ExpressionSyntax)op.Exception.Syntax),
                IBlockOperation op => AsExpressionSyntax(op.Operations.Single()),
                var v => throw ExceptionUtilities.UnexpectedValue(v.Kind)
            };

        public override SyntaxNode CreateSwitchStatement(IfStatementSyntax ifStatement, SyntaxNode expression, IEnumerable<SyntaxNode> sectionList)
        {
            var block = ifStatement.Statement as BlockSyntax;
            return SwitchStatement(
                switchKeyword: Token(SyntaxKind.SwitchKeyword).WithTriviaFrom(ifStatement.IfKeyword),
                openParenToken: ifStatement.OpenParenToken,
                expression: (ExpressionSyntax)expression,
                closeParenToken: ifStatement.CloseParenToken.WithPrependedLeadingTrivia(ElasticMarker),
                openBraceToken: block?.OpenBraceToken ?? Token(SyntaxKind.OpenBraceToken),
                sections: List(sectionList.Cast<SwitchSectionSyntax>()),
                closeBraceToken: block?.CloseBraceToken ?? Token(SyntaxKind.CloseBraceToken));
        }

        private static WhenClauseSyntax? AsWhenClause(AnalyzedSwitchLabel label)
            => AsWhenClause(label.Guards
                .Select(e => e.WalkUpParentheses())
                .AggregateOrDefault((prev, current) => BinaryExpression(SyntaxKind.LogicalAndExpression, current, prev)));

        private static WhenClauseSyntax? AsWhenClause(ExpressionSyntax? expression)
            => expression is null ? null : WhenClause(expression);

        public override SyntaxNode AsSwitchLabelSyntax(AnalyzedSwitchLabel label)
            => CasePatternSwitchLabel(
                AsPatternSyntax(label.Pattern),
                AsWhenClause(label),
                Token(SyntaxKind.ColonToken));

        private static PatternSyntax AsPatternSyntax(AnalyzedPattern pattern)
            => pattern switch
            {
                AnalyzedPattern.Constant p => ConstantPattern(p.ExpressionSyntax),
                AnalyzedPattern.Source p => p.PatternSyntax,
                AnalyzedPattern.Type p => DeclarationPattern((TypeSyntax)p.IsExpressionSyntax.Right, DiscardDesignation()),
                var p => throw ExceptionUtilities.UnexpectedValue(p)
            };

        public override IEnumerable<SyntaxNode> AsSwitchSectionStatements(IOperation operation)
        {
            var node = operation.Syntax;
            var requiresBreak = operation.SemanticModel.AnalyzeControlFlow(node).EndPointIsReachable;
            var requiresBlock = !operation.SemanticModel.AnalyzeDataFlow(node).VariablesDeclared.IsDefaultOrEmpty;

            var statements = ArrayBuilder<SyntaxNode>.GetInstance();
            if (node is BlockSyntax block)
            {
                if (block.Statements.Count == 0)
                {
                    statements.Add(BreakStatement());
                }
                else if (requiresBlock)
                {
                    statements.Add(requiresBreak ? block.AddStatements(BreakStatement()) : block);
                }
                else
                {
                    statements.AddRange(block.Statements);
                    if (requiresBreak)
                    {
                        statements.Add(BreakStatement());
                    }
                }
            }
            else
            {
                statements.Add(node);
                if (requiresBreak)
                {
                    statements.Add(BreakStatement());
                }
            }

            return statements.ToArrayAndFree();
        }
    }
}
