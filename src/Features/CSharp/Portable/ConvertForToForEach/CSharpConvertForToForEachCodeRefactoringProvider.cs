﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System.Composition;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using Microsoft.CodeAnalysis.CodeRefactorings;
using Microsoft.CodeAnalysis.ConvertForToForEach;
using Microsoft.CodeAnalysis.CSharp.Extensions;
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
        [ImportingConstructor]
        [SuppressMessage("RoslynDiagnosticsReliability", "RS0033:Importing constructor should be [Obsolete]", Justification = "Used in test code: https://github.com/dotnet/roslyn/issues/42814")]
        public CSharpConvertForToForEachCodeRefactoringProvider()
        {
        }

        protected override string GetTitle()
            => CSharpFeaturesResources.Convert_to_foreach;

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
                            return TryGetStepValue(iterationVariable, incrementor, out stepValueExpressionOpt);
                        }
                    }
                }
            }

            iterationVariable = default;
            memberAccess = null;
            initializer = null;
            stepValueExpressionOpt = null;
            return false;
        }

        private static bool TryGetStepValue(
            SyntaxToken iterationVariable, ExpressionSyntax incrementor, out ExpressionSyntax stepValue)
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
                    stepValue = null;
                    break;

                case SyntaxKind.PreIncrementExpression:
                    operand = ((PrefixUnaryExpressionSyntax)incrementor).Operand;
                    stepValue = null;
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
            typeNode ??= iterationVariableType.GenerateTypeSyntax();

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
