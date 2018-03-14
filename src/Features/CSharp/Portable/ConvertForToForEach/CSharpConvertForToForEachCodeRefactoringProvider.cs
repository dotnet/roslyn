// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Composition;
using System.Text;
using System.Threading;
using Microsoft.CodeAnalysis.CodeRefactorings;
using Microsoft.CodeAnalysis.ConvertForToForEach;
using Microsoft.CodeAnalysis.CSharp.CodeGeneration;
using Microsoft.CodeAnalysis.CSharp.CodeStyle.TypeStyle;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Options;

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
            => CSharpFeaturesResources.Convert_for_to_foreach;

        protected override SyntaxList<StatementSyntax> GetBodyStatements(ForStatementSyntax forStatement)
        {
            if (forStatement.Statement is BlockSyntax block)
            {
                return block.Statements;
            }

            return SyntaxFactory.SingletonList(forStatement.Statement);
        }
        protected override bool TryGetForStatementComponents(
            ForStatementSyntax forStatement,
            out SyntaxToken iterationVariable, out ExpressionSyntax initializer,
            out MemberAccessExpressionSyntax memberAccess, out ExpressionSyntax stepValue,
            CancellationToken cancellationToken)
        {
            if (forStatement.Declaration != null &&
                forStatement.Condition is BinaryExpressionSyntax binaryExpression &&
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

                        // Look for:  i < expr.Length
                        if (binaryExpression.Kind() == SyntaxKind.LessThanExpression &&
                            binaryExpression.Left is IdentifierNameSyntax identifierName &&
                            identifierName.Identifier.ValueText == iterationVariable.ValueText &&
                            binaryExpression.Right is MemberAccessExpressionSyntax)
                        {
                            memberAccess = (MemberAccessExpressionSyntax)binaryExpression.Right;

                            var incrementor = forStatement.Incrementors[0];

                            if (TryGetStepValue(iterationVariable, incrementor, out stepValue, cancellationToken))
                            {
                                return true;
                            }
                        }
                    }
                }
            }

            iterationVariable = default;
            memberAccess = default;
            initializer = default;
            stepValue = default;
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
            if (incrementor.Kind() == SyntaxKind.PostIncrementExpression)
            {
                operand = ((PostfixUnaryExpressionSyntax)incrementor).Operand;
                stepValue = default;
            }
            else if (incrementor.Kind() == SyntaxKind.PreIncrementExpression)
            {
                operand = ((PrefixUnaryExpressionSyntax)incrementor).Operand;
                stepValue = default;
            }
            else if (incrementor.Kind() == SyntaxKind.AddAssignmentExpression)
            {
                var assignment = (AssignmentExpressionSyntax)incrementor;
                operand = assignment.Left;
                stepValue = assignment.Right;
            }
            else
            {
                stepValue = default;
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
            if (typeNode == null)
            {
                // types are not apperant in foreach statements.
                var isBuiltInTypeContext = TypeStyleHelper.IsBuiltInType(iterationVariableType);
                if (TypeStyleHelper.IsImplicitStylePreferred(
                        optionSet, isBuiltInTypeContext, isTypeApparentContext: false))
                {
                    typeNode = SyntaxFactory.IdentifierName("var");
                }
                else
                {
                    typeNode = (TypeSyntax)CSharpSyntaxGenerator.Instance.TypeExpression(iterationVariableType);
                }
            }

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

        protected override bool IsValidVariableDeclarator(VariableDeclaratorSyntax firstVariable)
            => true;
    }
}
