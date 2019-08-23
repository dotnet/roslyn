// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

#nullable enable

using System.Collections.Generic;
using System.Composition;
using System.Linq;
using Microsoft.CodeAnalysis.CodeRefactorings;
using Microsoft.CodeAnalysis.ConvertIfToSwitch;
using Microsoft.CodeAnalysis.CSharp.Extensions;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.LanguageServices;
using Microsoft.CodeAnalysis.Operations;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp.ConvertIfToSwitch
{
    using static SyntaxFactory;

    [ExportCodeRefactoringProvider(LanguageNames.CSharp, Name = nameof(CSharpConvertIfToSwitchCodeRefactoringProvider)), Shared]
    internal sealed class CSharpConvertIfToSwitchCodeRefactoringProvider : AbstractConvertIfToSwitchCodeRefactoringProvider<IfStatementSyntax>
    {
        [ImportingConstructor]
        public CSharpConvertIfToSwitchCodeRefactoringProvider()
        {
        }

        protected override SyntaxNode CreateSwitchStatement(SyntaxNode node, SyntaxNode expression, IEnumerable<SyntaxNode> sectionList)
        {
            var ifStatement = (IfStatementSyntax)node;
            var block = ifStatement.Statement as BlockSyntax;
            return SwitchStatement(
                switchKeyword: Token(SyntaxKind.SwitchKeyword).WithTriviaFrom(ifStatement.IfKeyword),
                openParenToken: ifStatement.OpenParenToken,
                expression: (ExpressionSyntax)expression,
                closeParenToken: ifStatement.CloseParenToken.WithPrependedLeadingTrivia(ElasticMarker),
                openBraceToken: block?.OpenBraceToken ?? Token(SyntaxKind.OpenBraceToken),
                sections: List(sectionList.OfType<SwitchSectionSyntax>()),
                closeBraceToken: block?.CloseBraceToken ?? Token(SyntaxKind.CloseBraceToken));
        }

        protected override IEnumerable<SyntaxNode> AsSwitchSectionStatements(IOperation operation)
        {
            var node = operation.Syntax;
            var requiresBreak = operation.SemanticModel.AnalyzeControlFlow(node).EndPointIsReachable;
            var requiresBlock = !operation.SemanticModel.AnalyzeDataFlow(node).VariablesDeclared.IsDefaultOrEmpty;

            if (node is BlockSyntax block)
            {
                if (block.Statements.Count == 0)
                {
                    yield return BreakStatement();
                }
                else if (requiresBlock)
                {
                    if (requiresBreak)
                    {
                        yield return block.AddStatements(BreakStatement());
                    }
                    else
                    {
                        yield return block;
                    }
                }
                else
                {
                    foreach (var statement in block.Statements)
                    {
                        yield return statement;
                    }

                    if (requiresBreak)
                    {
                        yield return BreakStatement();
                    }
                }
            }
            else
            {
                yield return node;

                if (requiresBreak)
                {
                    yield return BreakStatement();
                }
            }
        }

        private static WhenClauseSyntax? AsWhenClause(ExpressionSyntax? expression)
        {
            return expression is null ? null : WhenClause(expression);
        }

        protected override SyntaxNode AsSwitchLabelSyntax(SwitchLabel label)
        {
            return CasePatternSwitchLabel(
                AsPatternSyntax(label.Pattern),
                AsWhenClause(label.Guards
                    .Cast<ExpressionSyntax>()
                    .AggregateOrDefault((prev, current) => BinaryExpression(SyntaxKind.LogicalAndExpression, current.WalkDownParentheses(), prev))),
                Token(SyntaxKind.ColonToken));
        }

        private static PatternSyntax AsPatternSyntax(Pattern pattern)
        {
            return pattern switch
            {
                ConstantPattern p => ConstantPattern((ExpressionSyntax)p.ExpressionSyntax),
                SourcePattern p => (PatternSyntax)p.PatternSyntax,
                TypePattern p => DeclarationPattern((TypeSyntax)((BinaryExpressionSyntax)p.IsExpressionSyntax).Right, DiscardDesignation()),
                var v => throw ExceptionUtilities.UnexpectedValue(v.GetType())
            };
        }

        protected override Analyzer CreateAnalyzer(ISyntaxFactsService syntaxFacts)
            => new CSharpAnalyzer(syntaxFacts);

        protected override string Title => CSharpFeaturesResources.Convert_to_switch;

        private sealed class CSharpAnalyzer : Analyzer
        {
            public CSharpAnalyzer(ISyntaxFactsService syntaxFacts)
                : base(syntaxFacts)
            {
            }

            // We do not offer a fix if the if-statement contains a break-statement, e.g.
            //
            //      while (...)
            //      {
            //          if (...) {
            //              break;
            //          }
            //      }
            //
            // When the 'break' moves into the switch, it will have different flow control impact.
            public override bool CanConvert(IConditionalOperation operation)
                => !operation.SemanticModel.AnalyzeControlFlow(operation.Syntax).ExitPoints.Any(n => n.IsKind(SyntaxKind.BreakStatement));

            public override bool SupportsCaseGuard => true;
            public override bool SupportsRangePattern => false;
            public override bool SupportsTypePattern => true;
            public override bool SupportsSourcePattern => true;
            public override bool SupportsRelationalPattern => false;
        }
    }
}
