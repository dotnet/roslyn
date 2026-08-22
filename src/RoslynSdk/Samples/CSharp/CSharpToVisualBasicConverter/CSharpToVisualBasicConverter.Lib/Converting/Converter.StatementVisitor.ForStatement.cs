// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using CS = Microsoft.CodeAnalysis.CSharp;
using VB = Microsoft.CodeAnalysis.VisualBasic;

namespace CSharpToVisualBasicConverter
{
    public partial class Converter
    {
        private partial class StatementVisitor
        {
            public override SyntaxList<VB.Syntax.StatementSyntax> VisitForStatement(CS.Syntax.ForStatementSyntax node)
            {
                // VB doesn't have a For statement that directly maps to C#'s.  However, some C# for
                // statements will map to a VB for statement.  Check for those common cases and
                // translate those.
                return IsSimpleForStatement(node)
                    ? VisitSimpleForStatement(node)
                    : VisitComplexForStatement(node);
            }

            private SyntaxList<VB.Syntax.StatementSyntax> VisitSimpleForStatement(CS.Syntax.ForStatementSyntax node)
            {
                VB.Syntax.ForStatementSyntax forStatement = CreateForStatement(node);
                IEnumerable<VB.Syntax.StatementSyntax> statements = VisitStatementEnumerable(node.Statement);

                VB.Syntax.ForBlockSyntax forBlock = VB.SyntaxFactory.ForBlock(
                    forStatement,
                    List(statements),
                    VB.SyntaxFactory.NextStatement());

                return List<VB.Syntax.StatementSyntax>(forBlock);
            }

            private VB.Syntax.ForStatementSyntax CreateForStatement(CS.Syntax.ForStatementSyntax node)
            {
                string variableName = node.Declaration.Variables[0].Identifier.ValueText;
                VB.Syntax.ForStepClauseSyntax stepClause = CreateForStepClause(node);
                VB.Syntax.ExpressionSyntax toValue = CreateForToValue(node);
                return VB.SyntaxFactory.ForStatement(
                    controlVariable: VB.SyntaxFactory.IdentifierName(variableName),
                    fromValue: nodeVisitor.VisitExpression(node.Declaration.Variables[0].Initializer.Value),
                    toValue: toValue,
                    stepClause: stepClause);
            }

            private VB.Syntax.ExpressionSyntax CreateForToValue(CS.Syntax.ForStatementSyntax node)
            {
                VB.Syntax.ExpressionSyntax expression = nodeVisitor.VisitExpression(((CS.Syntax.BinaryExpressionSyntax)node.Condition).Right);

                if (!node.Condition.IsKind(CS.SyntaxKind.LessThanOrEqualExpression) &&
                    !node.Condition.IsKind(CS.SyntaxKind.GreaterThanOrEqualExpression))
                {
                    if (node.Condition.IsKind(CS.SyntaxKind.LessThanExpression))
                    {
                        return VB.SyntaxFactory.SubtractExpression(
                            expression, CreateOneExpression());
                    }

                    if (node.Condition.IsKind(CS.SyntaxKind.GreaterThanExpression))
                    {
                        return VB.SyntaxFactory.AddExpression(
                            expression, CreateOneExpression());
                    }
                }

                return expression;
            }

            private VB.Syntax.ForStepClauseSyntax CreateForStepClause(CS.Syntax.ForStatementSyntax node)
            {
                ExpressionSyntax incrementor = node.Incrementors[0];
                if (!incrementor.IsKind(CS.SyntaxKind.PreIncrementExpression) &&
                    !incrementor.IsKind(CS.SyntaxKind.PostIncrementExpression))
                {
                    if (incrementor.IsKind(CS.SyntaxKind.PreDecrementExpression) ||
                        incrementor.IsKind(CS.SyntaxKind.PostDecrementExpression))
                    {
                        return VB.SyntaxFactory.ForStepClause(
                            VB.SyntaxFactory.UnaryMinusExpression(CreateOneExpression()));
                    }

                    if (incrementor.IsKind(CS.SyntaxKind.AddAssignmentExpression))
                    {
                        return VB.SyntaxFactory.ForStepClause(nodeVisitor.VisitExpression(((CS.Syntax.AssignmentExpressionSyntax)incrementor).Right));
                    }

                    if (incrementor.IsKind(CS.SyntaxKind.SubtractAssignmentExpression))
                    {
                        return VB.SyntaxFactory.ForStepClause(VB.SyntaxFactory.UnaryMinusExpression(
                            nodeVisitor.VisitExpression(((CS.Syntax.AssignmentExpressionSyntax)incrementor).Right)));
                    }
                }

                return null;
            }

            private static VB.Syntax.LiteralExpressionSyntax CreateOneExpression()
            {
                return VB.SyntaxFactory.NumericLiteralExpression(VB.SyntaxFactory.IntegerLiteralToken("1", VB.Syntax.LiteralBase.Decimal, VB.Syntax.TypeCharacter.None, 1));
            }

            private bool IsSimpleForStatement(CS.Syntax.ForStatementSyntax node)
            {
                // Has to look like one of the following:
#if false
                for (Declaration; Condition; Incrementor)

                Declaration must be one of:
                var name = v1
                primitive_type name = v1

                Condition must be one of:
                name < v2
                name <= v2
                name > v2
                name >= v2

                Incrementor must be one of:
                name++;
                name--;
                name += v3;
                name -= v3;
#endif
                if (node.Declaration == null ||
                    node.Declaration.Variables.Count != 1)
                {
                    return false;
                }

                string variableName = node.Declaration.Variables[0].Identifier.ValueText;

                return
                    IsSimpleForDeclaration(node) &&
                    IsSimpleForCondition(node, variableName) &&
                    IsSimpleForIncrementor(node, variableName);
            }

            private bool IsSimpleForDeclaration(CS.Syntax.ForStatementSyntax node)
            {
#if false
                Declaration must be one of:
                var name = v1
                primitive_type name = v1
#endif

                if (node.Declaration != null &&
                    node.Declaration.Variables.Count == 1 &&
                    node.Declaration.Variables[0].Initializer != null)
                {
                    if (node.Declaration.Type.IsVar || node.Declaration.Type.IsKind(CS.SyntaxKind.PredefinedType))
                    {
                        return true;
                    }
                }

                return false;
            }

            private bool IsSimpleForCondition(CS.Syntax.ForStatementSyntax node, string variableName)
            {
#if false
                Condition must be one of:
                name < v2
                name <= v2
                name > v2
                name >= v2
#endif
                if (node.Condition != null)
                {
                    if (node.Condition.IsKind(CS.SyntaxKind.LessThanExpression) ||
                        node.Condition.IsKind(CS.SyntaxKind.LessThanOrEqualExpression) ||
                        node.Condition.IsKind(CS.SyntaxKind.GreaterThanExpression) ||
                        node.Condition.IsKind(CS.SyntaxKind.GreaterThanOrEqualExpression))
                    {
                        BinaryExpressionSyntax binaryExpression = (CS.Syntax.BinaryExpressionSyntax)node.Condition;
                        return binaryExpression.Left is CS.Syntax.IdentifierNameSyntax identifierName &&
                               identifierName.Identifier.ValueText == variableName;
                    }
                }

                return false;
            }

            private bool IsSimpleForIncrementor(CS.Syntax.ForStatementSyntax node, string variableName)
            {
#if false
                name++;
                name--;
                ++name;
                --name;
                name += v3;
                name -= v3;
#endif
                if (node.Incrementors.Count == 1)
                {
                    ExpressionSyntax incrementor = node.Incrementors[0];
                    if (incrementor.IsKind(CS.SyntaxKind.PostIncrementExpression) ||
                        incrementor.IsKind(CS.SyntaxKind.PostDecrementExpression))
                    {
                        return ((CS.Syntax.PostfixUnaryExpressionSyntax)incrementor).Operand is CS.Syntax.IdentifierNameSyntax identifierName &&
                               identifierName.Identifier.ValueText == variableName;
                    }

                    if (incrementor.IsKind(CS.SyntaxKind.PreIncrementExpression) ||
                        incrementor.IsKind(CS.SyntaxKind.PreDecrementExpression))
                    {
                        return ((CS.Syntax.PrefixUnaryExpressionSyntax)incrementor).Operand is CS.Syntax.IdentifierNameSyntax identifierName &&
                               identifierName.Identifier.ValueText == variableName;
                    }

                    if (incrementor.IsKind(CS.SyntaxKind.AddAssignmentExpression) ||
                        incrementor.IsKind(CS.SyntaxKind.SubtractAssignmentExpression))
                    {
                        AssignmentExpressionSyntax binaryExpression = (CS.Syntax.AssignmentExpressionSyntax)incrementor;
                        return binaryExpression.Left is CS.Syntax.IdentifierNameSyntax identifierName &&
                               identifierName.Identifier.ValueText == variableName;
                    }
                }

                return false;
            }

            private SyntaxList<VB.Syntax.StatementSyntax> VisitComplexForStatement(CS.Syntax.ForStatementSyntax node)
            {
                // VB doesn't have a for loop.  So convert:
                //   for (declarations; condition; incrementors) body into:
                //
                // declarations
                // while (condition) {
                //   body;
                //   incrementors;
                // }

                VB.Syntax.WhileStatementSyntax begin;
                if (node.Condition == null)
                {
                    begin = VB.SyntaxFactory.WhileStatement(
                        condition: VB.SyntaxFactory.TrueLiteralExpression(VB.SyntaxFactory.Token(VB.SyntaxKind.TrueKeyword)));
                }
                else
                {
                    begin = VB.SyntaxFactory.WhileStatement(
                        condition: nodeVisitor.VisitExpression(node.Condition));
                }

                SyntaxList<VB.Syntax.StatementSyntax> initialBlock = Visit(node.Statement);

                List<VB.Syntax.StatementSyntax> whileStatements = initialBlock.Concat(
                    node.Incrementors.Select(nodeVisitor.VisitStatement)).ToList();
                SyntaxList<VB.Syntax.StatementSyntax> whileBody = List<VB.Syntax.StatementSyntax>(whileStatements);

                VB.Syntax.WhileBlockSyntax whileBlock = VB.SyntaxFactory.WhileBlock(
                    begin,
                    whileBody);

                List<VB.Syntax.StatementSyntax> statements = new List<VB.Syntax.StatementSyntax>();
                if (node.Declaration != null)
                {
                    statements.Add(nodeVisitor.Visit<VB.Syntax.StatementSyntax>(node.Declaration));
                }

                statements.Add(whileBlock);

                return List<VB.Syntax.StatementSyntax>(statements);
            }
        }
    }
}
