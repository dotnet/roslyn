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
    // These tests handle changing between different unary expressions 
    public class UnaryExpression
    {
        [Fact]
        public void PlusToNegate()
        {
            MakeUnaryChange(SyntaxKind.UnaryPlusExpression, SyntaxKind.UnaryMinusExpression);
        }

        [Fact]
        public void PlusToAddressOf()
        {
            MakeUnaryChange(SyntaxKind.UnaryPlusExpression, SyntaxKind.AddressOfExpression);
        }

        [Fact]
        public void PlusToBitwiseNot()
        {
            MakeUnaryChange(SyntaxKind.UnaryPlusExpression, SyntaxKind.BitwiseNotExpression);
        }

        [Fact]
        public void PlusToLogicalNot()
        {
            MakeUnaryChange(SyntaxKind.UnaryPlusExpression, SyntaxKind.LogicalNotExpression);
        }

        [Fact]
        public void PlusToPointerIndirect()
        {
            MakeUnaryChange(SyntaxKind.UnaryPlusExpression, SyntaxKind.PointerIndirectionExpression);
        }

        [Fact]
        public void PlusToPreDecrement()
        {
            MakeUnaryChange(SyntaxKind.UnaryPlusExpression, SyntaxKind.PreDecrementExpression);
        }

        [Fact]
        public void PlusToPreIncrement()
        {
            MakeUnaryChange(SyntaxKind.UnaryPlusExpression, SyntaxKind.PreIncrementExpression);
        }

        #region Helpers
        private static void MakeUnaryChange(SyntaxKind oldStyle, SyntaxKind newStyle)
        {
            MakeUnaryChanges(oldStyle, newStyle);
            MakeUnaryChanges(oldStyle, newStyle, options: TestOptions.Script);
            MakeUnaryChanges(oldStyle, newStyle, topLevel: true, options: TestOptions.Script);
        }

        private static void MakeUnaryChanges(SyntaxKind oldSyntaxKind, SyntaxKind newSyntaxKind, bool topLevel = false, CSharpParseOptions options = null)
        {
            string oldName = GetExpressionString(oldSyntaxKind);
            string newName = GetExpressionString(newSyntaxKind);

            string topLevelStatement = oldName + " y";
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

        private static PrefixUnaryExpressionSyntax GetExpressionNode(SyntaxTree newTree)
        {
            var classType = newTree.GetCompilationUnitRoot().Members[0] as TypeDeclarationSyntax;
            var method = classType.Members[0] as MethodDeclarationSyntax;
            var block = method.Body;
            var statement = block.Statements[0] as ExpressionStatementSyntax;
            var expression = statement.Expression as PrefixUnaryExpressionSyntax;
            return expression;
        }

        private static PrefixUnaryExpressionSyntax GetGlobalExpressionNode(SyntaxTree newTree)
        {
            var statementType = newTree.GetCompilationUnitRoot().Members[0] as GlobalStatementSyntax;
            Assert.True(statementType.AttributeLists.Count == 0);
            Assert.True(statementType.Modifiers.Count == 0);
            var statement = statementType.Statement as ExpressionStatementSyntax;
            var expression = statement.Expression as PrefixUnaryExpressionSyntax;
            return expression;
        }

        private static string GetExpressionString(SyntaxKind oldStyle)
        {
            switch (oldStyle)
            {
                case SyntaxKind.UnaryPlusExpression:
                    return "+";
                case SyntaxKind.UnaryMinusExpression:
                    return "-";
                case SyntaxKind.BitwiseNotExpression:
                    return "~";
                case SyntaxKind.LogicalNotExpression:
                    return "!";
                case SyntaxKind.PreIncrementExpression:
                    return "++";
                case SyntaxKind.PreDecrementExpression:
                    return "--";
                case SyntaxKind.PointerIndirectionExpression:
                    return "*";
                case SyntaxKind.AddressOfExpression:
                    return "&";
                default:
                    throw new Exception("Unexpected case");
            }
        }
        #endregion
    }
}
