// *********************************************************
//
// Copyright © Microsoft Corporation
//
// Licensed under the Apache License, Version 2.0 (the
// "License"); you may not use this file except in
// compliance with the License. You may obtain a copy of
// the License at
//
// http://www.apache.org/licenses/LICENSE-2.0 
//
// THIS CODE IS PROVIDED ON AN *AS IS* BASIS, WITHOUT WARRANTIES
// OR CONDITIONS OF ANY KIND, EITHER EXPRESS OR IMPLIED,
// INCLUDING WITHOUT LIMITATION ANY IMPLIED WARRANTIES
// OR CONDITIONS OF TITLE, FITNESS FOR A PARTICULAR
// PURPOSE, MERCHANTABILITY OR NON-INFRINGEMENT.
//
// See the Apache 2 License for the specific language
// governing permissions and limitations under the License.
//
// *********************************************************

using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace TreeTransformsCS
{
    public class TransformVisitor : CSharpSyntaxRewriter
    {
        private readonly SyntaxTree tree;
        private TransformKind transformKind;

        public TransformVisitor(SyntaxTree tree, TransformKind transKind)
        {
            this.tree = tree;
            this.transformKind = transKind;
        }

        public override SyntaxNode VisitAnonymousMethodExpression(AnonymousMethodExpressionSyntax node)
        {
            node = (AnonymousMethodExpressionSyntax)base.VisitAnonymousMethodExpression(node);

            if (transformKind == TransformKind.AnonMethodToLambda)
            {
                SyntaxToken arrowToken = SyntaxFactory.Token(SyntaxKind.EqualsGreaterThanToken);

                return SyntaxFactory.ParenthesizedLambdaExpression(default(SyntaxToken), node.ParameterList, arrowToken, node.Block);
            }

            return node;
        }

        public override SyntaxNode VisitParenthesizedLambdaExpression(ParenthesizedLambdaExpressionSyntax node)
        {
            node = (ParenthesizedLambdaExpressionSyntax)base.VisitParenthesizedLambdaExpression(node);

            if (transformKind == TransformKind.LambdaToAnonMethod)
            {
                // If any of the lambda parameters do not have type explicitly specified then we don't do any transforms.
                foreach (var parameter in node.ParameterList.Parameters)
                {
                    if (parameter.Type == null)
                    {
                        return node;
                    }
                }

                // If the body of the lambda is not a block syntax we don't do any transforms.
                if (node.Body.Kind() != SyntaxKind.Block)
                {
                    return node;
                }

                return SyntaxFactory.AnonymousMethodExpression(
                    default(SyntaxToken),
                    SyntaxFactory.Token(SyntaxKind.DelegateKeyword),
                    node.ParameterList,
                    (BlockSyntax)node.Body);
            }

            return node;
        }

        public override SyntaxNode VisitDoStatement(DoStatementSyntax node)
        {
            node = (DoStatementSyntax)base.VisitDoStatement(node);

            if (transformKind == TransformKind.DoToWhile)
            {
                // Get the different syntax nodes components of the Do Statement
                var doKeyword = node.DoKeyword;
                var doStatement = node.Statement;
                var whileKeyword = node.WhileKeyword;
                var condition = node.Condition;
                var openParen = node.OpenParenToken;
                var closeParen = node.CloseParenToken;
                var semicolon = node.SemicolonToken;

                // Preserve some level of trivia that was in the original Do keyword node.
                var newWhileKeyword = SyntaxFactory.Token(doKeyword.LeadingTrivia, SyntaxKind.WhileKeyword, whileKeyword.TrailingTrivia);

                // Preserve some level of trivia that was in the original Do keyword node and the original CloseParen token.
                var newCloseParenTrivias = closeParen.TrailingTrivia.ToList();
                newCloseParenTrivias.AddRange(doKeyword.TrailingTrivia.ToList());
                var newCloseParenTriviaList = SyntaxFactory.TriviaList(newCloseParenTrivias);
                var newCloseParen = SyntaxFactory.Token(closeParen.LeadingTrivia, SyntaxKind.CloseParenToken, newCloseParenTriviaList);

                var newTrailingTrivias = doStatement.GetTrailingTrivia().ToList();
                newTrailingTrivias.AddRange(semicolon.TrailingTrivia.ToList());
                var newWhileStatement = doStatement.WithTrailingTrivia(newTrailingTrivias);

                return SyntaxFactory.WhileStatement(newWhileKeyword, openParen, condition, newCloseParen, newWhileStatement);
            }

            return node;
        }

        public override SyntaxNode VisitWhileStatement(WhileStatementSyntax node)
        {
            node = (WhileStatementSyntax)base.VisitWhileStatement(node);

            if (transformKind == TransformKind.WhileToDo)
            {
                // Get the different syntax nodes components of the While Statement
                var whileKeyword = node.WhileKeyword;
                var openParen = node.OpenParenToken;
                var condition = node.Condition;
                var closeParen = node.CloseParenToken;
                var whileStatement = node.Statement;

                // Preserve as much trivia and formatting info as possible while constructing the new nodes.
                var newDoKeyword = SyntaxFactory.Token(whileKeyword.LeadingTrivia, SyntaxKind.DoKeyword, closeParen.TrailingTrivia);
                var newWhileKeyword = SyntaxFactory.Token(SyntaxFactory.TriviaList(SyntaxFactory.ElasticMarker), SyntaxKind.WhileKeyword, whileKeyword.TrailingTrivia);
                var semiColonToken = SyntaxFactory.Token(SyntaxFactory.TriviaList(SyntaxFactory.ElasticMarker), SyntaxKind.SemicolonToken, whileStatement.GetTrailingTrivia());
                var newCloseParen = SyntaxFactory.Token(closeParen.LeadingTrivia, SyntaxKind.CloseParenToken, SyntaxFactory.TriviaList(SyntaxFactory.ElasticMarker));
                var newDoStatement = whileStatement.ReplaceTrivia(whileStatement.GetTrailingTrivia().Last(), SyntaxFactory.TriviaList());

                return SyntaxFactory.DoStatement(newDoKeyword, newDoStatement, newWhileKeyword, openParen, condition, newCloseParen, semiColonToken);
            }

            return node;
        }

        public override SyntaxNode VisitCheckedStatement(CheckedStatementSyntax node)
        {
            node = (CheckedStatementSyntax)base.VisitCheckedStatement(node);

            // Get the components of the checked statement
            var keyword = node.Keyword;
            var block = node.Block;

            if ((transformKind == TransformKind.CheckedStmtToUncheckedStmt) && (keyword.Kind() == SyntaxKind.CheckedKeyword))
            {
                var uncheckedToken = SyntaxFactory.Token(keyword.LeadingTrivia, SyntaxKind.UncheckedKeyword, keyword.TrailingTrivia);

                return SyntaxFactory.CheckedStatement(SyntaxKind.UncheckedStatement, uncheckedToken, block);
            }

            if ((transformKind == TransformKind.UncheckedStmtToCheckedStmt) && (keyword.Kind() == SyntaxKind.UncheckedKeyword))
            {
                var checkedToken = SyntaxFactory.Token(keyword.LeadingTrivia, SyntaxKind.CheckedKeyword, keyword.TrailingTrivia);
                return SyntaxFactory.CheckedStatement(SyntaxKind.CheckedStatement, checkedToken, block);
            }

            return node;
        }

        public override SyntaxNode VisitCheckedExpression(CheckedExpressionSyntax node)
        {
            node = (CheckedExpressionSyntax)base.VisitCheckedExpression(node);

            // Get the components of the checked expression
            var keyword = node.Keyword;
            var openParenToken = SyntaxFactory.Token(node.OpenParenToken.LeadingTrivia, SyntaxKind.OpenParenToken, node.OpenParenToken.TrailingTrivia);
            var expression = node.Expression;
            var closeParenToken = SyntaxFactory.Token(node.CloseParenToken.LeadingTrivia, SyntaxKind.CloseParenToken, node.CloseParenToken.TrailingTrivia);

            if ((transformKind == TransformKind.CheckedExprToUncheckedExpr) && (keyword.Kind() == SyntaxKind.CheckedKeyword))
            {
                var uncheckedToken = SyntaxFactory.Token(keyword.LeadingTrivia, SyntaxKind.UncheckedKeyword, keyword.TrailingTrivia);

                return SyntaxFactory.CheckedExpression(SyntaxKind.UncheckedExpression, uncheckedToken, openParenToken, expression, closeParenToken);
            }

            if ((transformKind == TransformKind.UncheckedExprToCheckedExpr) && (keyword.Kind() == SyntaxKind.UncheckedKeyword))
            {
                var checkedToken = SyntaxFactory.Token(keyword.LeadingTrivia, SyntaxKind.CheckedKeyword, keyword.TrailingTrivia);

                return SyntaxFactory.CheckedExpression(SyntaxKind.CheckedExpression, checkedToken, openParenToken, expression, closeParenToken);
            }

            return node;
        }

        public override SyntaxNode VisitLiteralExpression(LiteralExpressionSyntax node)
        {
            node = (LiteralExpressionSyntax)base.VisitLiteralExpression(node);

            var token = node.Token;

            if ((transformKind == TransformKind.TrueToFalse) && (node.Kind() == SyntaxKind.TrueLiteralExpression))
            {
                var newToken = SyntaxFactory.Token(token.LeadingTrivia, SyntaxKind.FalseKeyword, token.TrailingTrivia);

                return SyntaxFactory.LiteralExpression(SyntaxKind.FalseLiteralExpression, newToken);
            }

            if ((transformKind == TransformKind.FalseToTrue) && (node.Kind() == SyntaxKind.FalseLiteralExpression))
            {
                var newToken = SyntaxFactory.Token(token.LeadingTrivia, SyntaxKind.TrueKeyword, token.TrailingTrivia);

                return SyntaxFactory.LiteralExpression(SyntaxKind.TrueLiteralExpression, newToken);
            }

            return node;
        }

        public override SyntaxNode VisitAssignmentExpression(AssignmentExpressionSyntax node)
        {
            node = (AssignmentExpressionSyntax)base.VisitAssignmentExpression(node);
            var left = node.Left;
            var right = node.Right;
            var operatorToken = node.OperatorToken;

            if ((transformKind == TransformKind.AddAssignToAssign) && (node.Kind() == SyntaxKind.AddAssignmentExpression))
            {
                var equalsToken = SyntaxFactory.Token(operatorToken.LeadingTrivia, SyntaxKind.EqualsToken, operatorToken.TrailingTrivia);
                var newLeft = left.WithLeadingTrivia(SyntaxFactory.TriviaList());
                var addExpression = SyntaxFactory.BinaryExpression(SyntaxKind.AddExpression, newLeft, SyntaxFactory.Token(operatorToken.LeadingTrivia, SyntaxKind.PlusToken, operatorToken.TrailingTrivia), right);

                return SyntaxFactory.AssignmentExpression(SyntaxKind.SimpleAssignmentExpression, left, equalsToken, addExpression);
            }

            return node;
        }

        public override SyntaxNode VisitParameter(ParameterSyntax node)
        {
            node = (ParameterSyntax)base.VisitParameter(node);

            if ((transformKind == TransformKind.RefParamToOutParam) || (transformKind == TransformKind.OutParamToRefParam))
            {
                List<SyntaxToken> listOfModifiers = new List<SyntaxToken>();

                foreach (var modifier in node.Modifiers)
                {
                    var modifierToken = modifier;

                    if ((modifier.Kind() == SyntaxKind.RefKeyword) && (transformKind == TransformKind.RefParamToOutParam))
                    {
                        modifierToken = SyntaxFactory.Token(modifierToken.LeadingTrivia, SyntaxKind.OutKeyword, modifierToken.TrailingTrivia);
                    }
                    else if ((modifier.Kind() == SyntaxKind.OutKeyword) && (transformKind == TransformKind.OutParamToRefParam))
                    {
                        modifierToken = SyntaxFactory.Token(modifierToken.LeadingTrivia, SyntaxKind.RefKeyword, modifierToken.TrailingTrivia);
                    }

                    listOfModifiers.Add(modifierToken);
                }

                var newModifiers = SyntaxFactory.TokenList(listOfModifiers);

                return SyntaxFactory.Parameter(node.AttributeLists, newModifiers, node.Type, node.Identifier, node.Default);
            }

            return node;
        }

        public override SyntaxNode VisitArgument(ArgumentSyntax node)
        {
            node = (ArgumentSyntax)base.VisitArgument(node);

            var refOrOut = node.RefOrOutKeyword;

            if ((transformKind == TransformKind.RefArgToOutArg) && (refOrOut.Kind() == SyntaxKind.RefKeyword))
            {
                var outKeyword = SyntaxFactory.Token(refOrOut.LeadingTrivia, SyntaxKind.OutKeyword, refOrOut.TrailingTrivia);

                return SyntaxFactory.Argument(node.NameColon, outKeyword, node.Expression);
            }

            if ((transformKind == TransformKind.OutArgToRefArg) && (refOrOut.Kind() == SyntaxKind.OutKeyword))
            {
                var refKeyword = SyntaxFactory.Token(refOrOut.LeadingTrivia, SyntaxKind.RefKeyword, refOrOut.TrailingTrivia);

                return SyntaxFactory.Argument(node.NameColon, refKeyword, node.Expression);
            }

            return node;
        }

        public override SyntaxNode VisitOrdering(OrderingSyntax node)
        {
            node = (OrderingSyntax)base.VisitOrdering(node);

            var orderingKind = node.AscendingOrDescendingKeyword;

            if ((transformKind == TransformKind.OrderByAscToOrderByDesc) && (orderingKind.Kind() == SyntaxKind.AscendingKeyword))
            {
                var descToken = SyntaxFactory.Token(orderingKind.LeadingTrivia, SyntaxKind.DescendingKeyword, orderingKind.TrailingTrivia);

                return SyntaxFactory.Ordering(SyntaxKind.DescendingOrdering, node.Expression, descToken);
            }

            if ((transformKind == TransformKind.OrderByDescToOrderByAsc) && (orderingKind.Kind() == SyntaxKind.DescendingKeyword))
            {
                var ascToken = SyntaxFactory.Token(orderingKind.LeadingTrivia, SyntaxKind.AscendingKeyword, orderingKind.TrailingTrivia);

                return SyntaxFactory.Ordering(SyntaxKind.AscendingOrdering, node.Expression, ascToken);
            }

            return node;
        }

        public override SyntaxNode VisitVariableDeclaration(VariableDeclarationSyntax node)
        {
            node = (VariableDeclarationSyntax)base.VisitVariableDeclaration(node);

            var type = node.Type;
            var declarations = node.Variables;

            List<VariableDeclaratorSyntax> listOfVariables = new List<VariableDeclaratorSyntax>();

            List<SyntaxToken> listOfSeperators = new List<SyntaxToken>();

            if (transformKind == TransformKind.DefaultInitAllVars)
            {
                foreach (var decl in declarations)
                {
                    if (decl.Initializer == null)
                    {
                        var newType = type;

                        if (newType.HasLeadingTrivia)
                        {
                            newType = newType.WithLeadingTrivia(new SyntaxTriviaList());
                        }

                        if (newType.HasTrailingTrivia)
                        {
                            newType = newType.WithLeadingTrivia(new SyntaxTriviaList());
                        }

                        var whiteSpaceTrivia = SyntaxFactory.Whitespace(" ");
                        var defaultExpr = SyntaxFactory.DefaultExpression(newType);
                        var equalsClause = SyntaxFactory.EqualsValueClause(SyntaxFactory.Token(SyntaxFactory.TriviaList(whiteSpaceTrivia), SyntaxKind.EqualsToken, SyntaxFactory.TriviaList(whiteSpaceTrivia)), defaultExpr);

                        var newDecl = SyntaxFactory.VariableDeclarator(decl.Identifier, decl.ArgumentList, equalsClause);
                        listOfVariables.Add(newDecl);
                    }
                    else
                    {
                        listOfVariables.Add(decl);
                    }
                }

                for (int i = 0; i < declarations.SeparatorCount; i++)
                {
                    var seperator = declarations.GetSeparator(i);
                    listOfSeperators.Add(SyntaxFactory.Token(seperator.LeadingTrivia, seperator.Kind(), seperator.TrailingTrivia));
                }

                var seperatedSyntaxList = SyntaxFactory.SeparatedList(listOfVariables, listOfSeperators);

                return SyntaxFactory.VariableDeclaration(type, seperatedSyntaxList);
            }

            return node;
        }

        public override SyntaxNode VisitClassDeclaration(ClassDeclarationSyntax node)
        {
            node = (ClassDeclarationSyntax)base.VisitClassDeclaration(node);
            var typeDeclKindKeyword = node.Keyword;

            if (transformKind == TransformKind.ClassDeclToStructDecl)
            {
                var structToken = SyntaxFactory.Token(typeDeclKindKeyword.LeadingTrivia, SyntaxKind.StructKeyword, typeDeclKindKeyword.TrailingTrivia);

                return SyntaxFactory.StructDeclaration(node.AttributeLists, node.Modifiers, structToken, node.Identifier,
                    node.TypeParameterList, node.BaseList, node.ConstraintClauses, node.OpenBraceToken, node.Members, node.CloseBraceToken,
                    node.SemicolonToken);
            }

            return node;
        }

        public override SyntaxNode VisitStructDeclaration(StructDeclarationSyntax node)
        {
            node = (StructDeclarationSyntax)base.VisitStructDeclaration(node);
            var typeDeclKindKeyword = node.Keyword;

            if (transformKind == TransformKind.StructDeclToClassDecl)
            {
                var classToken = SyntaxFactory.Token(typeDeclKindKeyword.LeadingTrivia, SyntaxKind.ClassKeyword, typeDeclKindKeyword.TrailingTrivia);

                return SyntaxFactory.ClassDeclaration(node.AttributeLists, node.Modifiers, classToken, node.Identifier,
                    node.TypeParameterList, node.BaseList, node.ConstraintClauses, node.OpenBraceToken, node.Members, node.CloseBraceToken,
                    node.SemicolonToken);
            }

            return node;
        }

        public override SyntaxNode VisitInterfaceDeclaration(InterfaceDeclarationSyntax node)
        {
            return base.VisitInterfaceDeclaration(node);
        }

        public override SyntaxNode VisitPredefinedType(PredefinedTypeSyntax node)
        {
            node = (PredefinedTypeSyntax)base.VisitPredefinedType(node);
            var token = node.Keyword;

            if ((transformKind == TransformKind.IntTypeToLongType) && (token.Kind() == SyntaxKind.IntKeyword))
            {
                var longToken = SyntaxFactory.Token(token.LeadingTrivia, SyntaxKind.LongKeyword, token.TrailingTrivia);

                return SyntaxFactory.PredefinedType(longToken);
            }

            return node;
        }

        public override SyntaxNode VisitPostfixUnaryExpression(PostfixUnaryExpressionSyntax node)
        {
            node = (PostfixUnaryExpressionSyntax)base.VisitPostfixUnaryExpression(node);

            if (transformKind == TransformKind.PostfixToPrefix)
            {
                var operatorToken = node.OperatorToken;
                var operand = node.Operand;

                var newOperatorToken = SyntaxFactory.Token(operand.GetLeadingTrivia(), operatorToken.Kind(), SyntaxFactory.TriviaList(SyntaxFactory.ElasticMarker));
                var newOperand = operand.WithLeadingTrivia(operatorToken.LeadingTrivia);
                newOperand = newOperand.WithTrailingTrivia(operatorToken.TrailingTrivia);

                if (node.Kind() == SyntaxKind.PostIncrementExpression)
                {
                    return SyntaxFactory.PrefixUnaryExpression(SyntaxKind.PreIncrementExpression, newOperatorToken, newOperand);
                }

                if (node.Kind() == SyntaxKind.PostDecrementExpression)
                {
                    return SyntaxFactory.PrefixUnaryExpression(SyntaxKind.PreDecrementExpression, newOperatorToken, newOperand);
                }
            }

            return node;
        }

        public override SyntaxNode VisitPrefixUnaryExpression(PrefixUnaryExpressionSyntax node)
        {
            node = (PrefixUnaryExpressionSyntax)base.VisitPrefixUnaryExpression(node);

            if (transformKind == TransformKind.PrefixToPostfix)
            {
                var operatorToken = node.OperatorToken;
                var operand = node.Operand;

                var newOperatorToken = SyntaxFactory.Token(SyntaxFactory.TriviaList(SyntaxFactory.ElasticMarker), operatorToken.Kind(), operand.GetTrailingTrivia());
                var newOperand = operand.WithTrailingTrivia(operatorToken.TrailingTrivia);
                newOperand = newOperand.WithLeadingTrivia(operatorToken.LeadingTrivia);

                if (node.Kind() == SyntaxKind.PreIncrementExpression)
                {
                    return SyntaxFactory.PostfixUnaryExpression(SyntaxKind.PostIncrementExpression, newOperand, newOperatorToken);
                }

                if (node.Kind() == SyntaxKind.PreDecrementExpression)
                {
                    return SyntaxFactory.PostfixUnaryExpression(SyntaxKind.PostDecrementExpression, newOperand, newOperatorToken);
                }
            }

            return node;
        }
    }
}
