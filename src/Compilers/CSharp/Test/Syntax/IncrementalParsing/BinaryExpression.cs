// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.CSharp.Test.Utilities;
using Microsoft.CodeAnalysis.Text;
using Xunit;

namespace Microsoft.CodeAnalysis.CSharp.UnitTests.IncrementalParsing
{
    public class BinaryExpressionChanges
    {
        // This test is an exception to the others as * will be assumed to be a pointer type in an
        // expression syntax.  To make this parse correctly, the multiplication must be in a location
        // that is definitely an expression (i.e. an assignment expression)
        [Fact]
        public void PlusToMultiply()
        {
            string text = @"class C{
                       void M() {
                            int x = y + 2;
                            } 
                    }";
            var oldTree = SyntaxFactory.ParseSyntaxTree(text);
            var newTree = oldTree.WithReplaceFirst("+", "*");
            var type = newTree.GetCompilationUnitRoot().Members[0] as TypeDeclarationSyntax;
            var method = type.Members[0] as MethodDeclarationSyntax;
            var block = method.Body;
            var statement = block.Statements[0] as LocalDeclarationStatementSyntax;
            var expression = statement.Declaration.Variables[0].Initializer.Value as BinaryExpressionSyntax;
            Assert.Equal(SyntaxKind.MultiplyExpression, expression.Kind());
        }

        [Fact]
        public void PlusToMinus()
        {
            MakeBinOpChange(SyntaxKind.AddExpression, SyntaxKind.SubtractExpression);
        }

        [Fact]
        public void PlusToDivide()
        {
            MakeBinOpChange(SyntaxKind.AddExpression, SyntaxKind.DivideExpression);
        }

        [Fact]
        public void PlusToModulo()
        {
            MakeBinOpChange(SyntaxKind.AddExpression, SyntaxKind.ModuloExpression);
        }

        [Fact]
        public void PlusToLeftShift()
        {
            MakeBinOpChange(SyntaxKind.AddExpression, SyntaxKind.LeftShiftExpression);
        }

        [Fact]
        public void PlusToRightShift()
        {
            MakeBinOpChange(SyntaxKind.AddExpression, SyntaxKind.RightShiftExpression);
        }

        [Fact]
        public void PlusToUnsignedRightShift()
        {
            MakeBinOpChange(SyntaxKind.AddExpression, SyntaxKind.UnsignedRightShiftExpression);
        }

        [Fact]
        public void PlusToLogicalOr()
        {
            MakeBinOpChange(SyntaxKind.AddExpression, SyntaxKind.LogicalOrExpression);
        }

        [Fact]
        public void PlusToLogicalAnd()
        {
            MakeBinOpChange(SyntaxKind.AddExpression, SyntaxKind.LogicalAndExpression);
        }

        [Fact]
        public void PlusToBitwiseAnd()
        {
            MakeBinOpChange(SyntaxKind.AddExpression, SyntaxKind.BitwiseAndExpression);
        }

        [Fact]
        public void PlusToBitwiseOr()
        {
            MakeBinOpChange(SyntaxKind.AddExpression, SyntaxKind.BitwiseOrExpression);
        }

        [Fact]
        public void PlusToExclusiveOr()
        {
            MakeBinOpChange(SyntaxKind.AddExpression, SyntaxKind.ExclusiveOrExpression);
        }

        [Fact]
        public void PlusToEquals()
        {
            MakeBinOpChange(SyntaxKind.AddExpression, SyntaxKind.EqualsExpression);
        }

        [Fact]
        public void PlusToNotEquals()
        {
            MakeBinOpChange(SyntaxKind.AddExpression, SyntaxKind.NotEqualsExpression);
        }

        [Fact]
        public void PlusToLessThan()
        {
            MakeBinOpChange(SyntaxKind.AddExpression, SyntaxKind.LessThanExpression);
        }

        [Fact]
        public void PlusToLessThanEqualTo()
        {
            MakeBinOpChange(SyntaxKind.AddExpression, SyntaxKind.LessThanOrEqualExpression);
        }

        [Fact]
        public void PlusToGreaterThan()
        {
            MakeBinOpChange(SyntaxKind.AddExpression, SyntaxKind.GreaterThanExpression);
        }

        [Fact]
        public void PlusToGreaterThanEqualTo()
        {
            MakeBinOpChange(SyntaxKind.AddExpression, SyntaxKind.GreaterThanOrEqualExpression);
        }

        [Fact]
        public void PlusToAs()
        {
            MakeBinOpChange(SyntaxKind.AddExpression, SyntaxKind.AsExpression);
        }

        [Fact]
        public void PlusToIs()
        {
            MakeBinOpChange(SyntaxKind.AddExpression, SyntaxKind.IsExpression);
        }

        [Fact]
        public void PlusToCoalesce()
        {
            MakeBinOpChange(SyntaxKind.AddExpression, SyntaxKind.CoalesceExpression);
        }

        [Fact]
        public void DotToArrow()
        {
            MakeMemberAccessChange(SyntaxKind.SimpleMemberAccessExpression, SyntaxKind.PointerMemberAccessExpression);
        }

        [Fact]
        public void ArrowToDot()
        {
            MakeMemberAccessChange(SyntaxKind.PointerMemberAccessExpression, SyntaxKind.SimpleMemberAccessExpression);
        }

        #region Helpers

        private static void MakeMemberAccessChange(SyntaxKind oldStyle, SyntaxKind newStyle)
        {
            MakeChange(oldStyle, newStyle);
            MakeChange(oldStyle, newStyle, options: TestOptions.Script);
            MakeChange(oldStyle, newStyle, topLevel: true, options: TestOptions.Script);
        }

        private static void MakeBinOpChange(SyntaxKind oldStyle, SyntaxKind newStyle)
        {
            MakeChange(oldStyle, newStyle);
            MakeChange(oldStyle, newStyle, options: TestOptions.Script);
            MakeChange(oldStyle, newStyle, topLevel: true, options: TestOptions.Script);
        }

        private static void MakeChange(SyntaxKind oldSyntaxKind, SyntaxKind newSyntaxKind, bool topLevel = false, CSharpParseOptions options = null)
        {
            string oldName = GetExpressionString(oldSyntaxKind);
            string newName = GetExpressionString(newSyntaxKind);

            string topLevelStatement = "x " + oldName + " y";
            // Be warned when changing the fields here
            var code = @"class C { void m() {
                 " + topLevelStatement + @";
                }}";
            var oldTree = SyntaxFactory.ParseSyntaxTree(topLevel ? topLevelStatement : code, options: options);

            // Make the change to the node
            var newTree = oldTree.WithReplaceFirst(oldName, newName);
            var treeNode = topLevel ? GetGlobalExpressionNode(newTree) : GetExpressionNode(newTree);
            Assert.Equal(treeNode.Kind(), newSyntaxKind);
        }

        private static ExpressionSyntax GetExpressionNode(SyntaxTree newTree)
        {
            TypeDeclarationSyntax classType = newTree.GetCompilationUnitRoot().Members[0] as TypeDeclarationSyntax;
            MethodDeclarationSyntax method = classType.Members[0] as MethodDeclarationSyntax;
            var block = method.Body;
            var statement = block.Statements[0] as ExpressionStatementSyntax;
            return statement.Expression;
        }

        private static ExpressionSyntax GetGlobalExpressionNode(SyntaxTree newTree)
        {
            var statementType = newTree.GetCompilationUnitRoot().Members[0] as GlobalStatementSyntax;
            Assert.True(statementType.AttributeLists.Count == 0);
            Assert.True(statementType.Modifiers.Count == 0);
            var statement = statementType.Statement as ExpressionStatementSyntax;
            return statement.Expression;
        }

        private static string GetExpressionString(SyntaxKind oldStyle)
        {
            switch (oldStyle)
            {
                case SyntaxKind.AddExpression:
                    return "+";
                case SyntaxKind.SubtractExpression:
                    return "-";
                case SyntaxKind.MultiplyExpression:
                    return " * ";
                case SyntaxKind.DivideExpression:
                    return "/";
                case SyntaxKind.ModuloExpression:
                    return "%";
                case SyntaxKind.LeftShiftExpression:
                    return "<<";
                case SyntaxKind.RightShiftExpression:
                    return ">>";
                case SyntaxKind.UnsignedRightShiftExpression:
                    return ">>>";
                case SyntaxKind.LogicalOrExpression:
                    return "||";
                case SyntaxKind.LogicalAndExpression:
                    return "&&";
                case SyntaxKind.BitwiseOrExpression:
                    return "|";
                case SyntaxKind.BitwiseAndExpression:
                    return "&";
                case SyntaxKind.ExclusiveOrExpression:
                    return "^";
                case SyntaxKind.EqualsExpression:
                    return "==";
                case SyntaxKind.NotEqualsExpression:
                    return "!=";
                case SyntaxKind.LessThanExpression:
                    return "<";
                case SyntaxKind.LessThanOrEqualExpression:
                    return "<=";
                case SyntaxKind.GreaterThanExpression:
                    return ">";
                case SyntaxKind.GreaterThanOrEqualExpression:
                    return ">=";
                case SyntaxKind.AsExpression:
                    return " as ";
                case SyntaxKind.IsExpression:
                    return " is ";
                case SyntaxKind.CoalesceExpression:
                    return "??";
                case SyntaxKind.SimpleMemberAccessExpression:
                    return ".";
                case SyntaxKind.PointerMemberAccessExpression:
                    return "->";
                default:
                    throw new Exception("unexpected Type given");
            }
        }
        #endregion
    }
}
