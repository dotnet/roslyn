﻿// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Composition;
using System.Threading;
using Microsoft.CodeAnalysis.CodeRefactorings;
using Microsoft.CodeAnalysis.ConvertForToForEach;
using Microsoft.CodeAnalysis.CSharp.CodeGeneration;
using Microsoft.CodeAnalysis.CSharp.CodeStyle.TypeStyle;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.CSharp.ConvertForToForEach
{
    [ExportCodeRefactoringProvider(LanguageNames.CSharp, Name = nameof(CSharpConvertForToForEachCodeRefactoringProvider)), Shared]
    internal class CSharpConvertForToForEachCodeRefactoringProvider :
        AbstractConvertForToForEachCodeRefactoringProvider<
            StatementSyntax,
            ForStatementSyntax,
            ExpressionSyntax,
            MemberAccessExpressionSyntax,
            TypeSyntax,
            VariableDeclaratorSyntax>
    {
        protected override string GetTitle()
            => CSharpFeaturesResources.Convert_to_foreach;

        protected override bool IsValidCursorPosition(ForStatementSyntax forStatement, int cursorPos)
        {
            // If there isn't a selection, then we allow the refactoring from the start of
            // 'for' to the start of the open paren, or in the trailing trivia of the c
            // close paren.
            var startSpan = TextSpan.FromBounds(forStatement.ForKeyword.SpanStart, forStatement.OpenParenToken.SpanStart);
            var endSpan = TextSpan.FromBounds(forStatement.CloseParenToken.Span.End, forStatement.CloseParenToken.FullSpan.End);

            return startSpan.IntersectsWith(cursorPos) || endSpan.IntersectsWith(cursorPos);
        }

        protected override SyntaxList<StatementSyntax> GetBodyStatements(ForStatementSyntax forStatement)
            => forStatement.Statement is BlockSyntax block
                ? block.Statements
                : SyntaxFactory.SingletonList(forStatement.Statement);

        protected override bool TryGetForStatementComponents(
            ForStatementSyntax forStatement,
            out SyntaxToken iterationVariable, out ExpressionSyntax initializer,
            out MemberAccessExpressionSyntax memberAccess,
            out ExpressionSyntax stepValueExpressionOpt,
            CancellationToken cancellationToken)
        {
            // Look for very specific forms.  Basically, only minor variations around:
            // for (var i = 0; i < expr.Lenth; i++)

            if (forStatement.Declaration != null &&
                forStatement.Condition.IsKind(SyntaxKind.LessThanExpression) &&
                forStatement.Incrementors.Count == 1)
            {
                var declaration = forStatement.Declaration;
                if (declaration.Variables.Count == 1)
                {
                    var declarator = declaration.Variables[0];
                    if (declarator.Initializer != null)
                    {
                        iterationVariable = declarator.Identifier;
                        initializer = declarator.Initializer.Value;

                        var binaryExpression = (BinaryExpressionSyntax)forStatement.Condition;

                        // Look for:  i < expr.Length
                        if (binaryExpression.Left is IdentifierNameSyntax identifierName &&
                            identifierName.Identifier.ValueText == iterationVariable.ValueText &&
                            binaryExpression.Right is MemberAccessExpressionSyntax)
                        {
                            memberAccess = (MemberAccessExpressionSyntax)binaryExpression.Right;

                            var incrementor = forStatement.Incrementors[0];
                            return TryGetStepValue(
                                iterationVariable, incrementor, out stepValueExpressionOpt, cancellationToken);
                        }
                    }
                }
            }

            iterationVariable = default;
            memberAccess = default;
            initializer = default;
            stepValueExpressionOpt = default;
            return false;
        }

        private static bool TryGetStepValue(
            SyntaxToken iterationVariable, ExpressionSyntax incrementor,
            out ExpressionSyntax stepValue, CancellationToken cancellationToken)
        {
            // support
            //  x++
            //  ++x
            //  x += constant_1

            ExpressionSyntax operand;
            switch (incrementor.Kind())
            {
                case SyntaxKind.PostIncrementExpression:
                    operand = ((PostfixUnaryExpressionSyntax)incrementor).Operand;
                    stepValue = default;
                    break;

                case SyntaxKind.PreIncrementExpression:
                    operand = ((PrefixUnaryExpressionSyntax)incrementor).Operand;
                    stepValue = default;
                    break;

                case SyntaxKind.AddAssignmentExpression:
                    var assignment = (AssignmentExpressionSyntax)incrementor;
                    operand = assignment.Left;
                    stepValue = assignment.Right;
                    break;

                default:
                    stepValue = null;
                    return false;
            }

            return operand is IdentifierNameSyntax identifierName &&
                identifierName.Identifier.ValueText == iterationVariable.ValueText;
        }

        protected override SyntaxNode ConvertForNode(
            ForStatementSyntax forStatement, TypeSyntax typeNode,
            SyntaxToken foreachIdentifier, ExpressionSyntax collectionExpression,
            ITypeSymbol iterationVariableType, OptionSet optionSet)
        {
            typeNode = typeNode ?? CSharpSyntaxGenerator.Instance.GetTypeExpression(optionSet, iterationVariableType);

            return SyntaxFactory.ForEachStatement(
                SyntaxFactory.Token(SyntaxKind.ForEachKeyword).WithTriviaFrom(forStatement.ForKeyword),
                forStatement.OpenParenToken,
                typeNode,
                foreachIdentifier,
                SyntaxFactory.Token(SyntaxKind.InKeyword),
                collectionExpression,
                forStatement.CloseParenToken,
                forStatement.Statement);
        }

        // C# has no special variable declarator forms that would cause us to not be able to convert.
        protected override bool IsValidVariableDeclarator(VariableDeclaratorSyntax firstVariable)
            => true;
    }
}
