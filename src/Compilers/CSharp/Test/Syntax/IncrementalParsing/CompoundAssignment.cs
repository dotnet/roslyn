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
    // These tests test changes between different compound assignment expressions
    public class CompoundAssignment
    {
        [Fact]
        public void AssignToPlus()
        {
            MakeAssignmentChange(SyntaxKind.SimpleAssignmentExpression, SyntaxKind.AddAssignmentExpression);
        }

        [Fact]
        public void AssignToSubtract()
        {
            MakeAssignmentChange(SyntaxKind.SimpleAssignmentExpression, SyntaxKind.SubtractAssignmentExpression);
        }

        [Fact]
        public void AssignToMultiply()
        {
            MakeAssignmentChange(SyntaxKind.SimpleAssignmentExpression, SyntaxKind.MultiplyAssignmentExpression);
        }

        [Fact]
        public void AssignToDivide()
        {
            MakeAssignmentChange(SyntaxKind.SimpleAssignmentExpression, SyntaxKind.DivideAssignmentExpression);
        }

        [Fact]
        public void AssignToModule()
        {
            MakeAssignmentChange(SyntaxKind.SimpleAssignmentExpression, SyntaxKind.ModuloAssignmentExpression);
        }

        [Fact]
        public void AssignToExclusiveOr()
        {
            MakeAssignmentChange(SyntaxKind.SimpleAssignmentExpression, SyntaxKind.ExclusiveOrAssignmentExpression);
        }

        [Fact]
        public void AssignToLeftShift()
        {
            MakeAssignmentChange(SyntaxKind.SimpleAssignmentExpression, SyntaxKind.LeftShiftAssignmentExpression);
        }

        [Fact]
        public void AssignToRightShift()
        {
            MakeAssignmentChange(SyntaxKind.SimpleAssignmentExpression, SyntaxKind.RightShiftAssignmentExpression);
        }

        [Fact]
        public void AssignToUnsignedRightShift()
        {
            MakeAssignmentChange(SyntaxKind.SimpleAssignmentExpression, SyntaxKind.UnsignedRightShiftAssignmentExpression);
        }

        [Fact]
        public void AssignToAnd()
        {
            MakeAssignmentChange(SyntaxKind.SimpleAssignmentExpression, SyntaxKind.AndAssignmentExpression);
        }

        [Fact]
        public void AssignToOr()
        {
            MakeAssignmentChange(SyntaxKind.SimpleAssignmentExpression, SyntaxKind.OrAssignmentExpression);
        }

        #region Helper Methods
        private static void MakeAssignmentChange(SyntaxKind oldStyle, SyntaxKind newStyle)
        {
            MakeAssignmentChanges(oldStyle, newStyle);
            MakeAssignmentChanges(oldStyle, newStyle, options: TestOptions.Script);
            MakeAssignmentChanges(oldStyle, newStyle, topLevel: true, options: TestOptions.Script);
        }

        private static void MakeAssignmentChanges(SyntaxKind oldSyntaxKind, SyntaxKind newSyntaxKind, bool topLevel = false, CSharpParseOptions options = null)
        {
            string oldName = GetExpressionString(oldSyntaxKind);
            string newName = GetExpressionString(newSyntaxKind);

            string topLevelStatement = "x " + oldName + " y";
            var code = @"class C { void m() {
                 " + topLevelStatement + @";
                }}";

            var oldTree = SyntaxFactory.ParseSyntaxTree(topLevel ? topLevelStatement : code, options: options);

            // Make the change to the node
            var newTree = oldTree.WithReplaceFirst(oldName, newName);
            var binNode = topLevel ? GetGlobalStatementSyntaxChange(newTree) : GetExpressionSyntaxChange(newTree);
            Assert.Equal(binNode.Kind(), newSyntaxKind);
        }

        private static string GetExpressionString(SyntaxKind oldStyle)
        {
            switch (oldStyle)
            {
                case SyntaxKind.SimpleAssignmentExpression:
                    return "=";
                case SyntaxKind.AddAssignmentExpression:
                    return "+=";
                case SyntaxKind.SubtractAssignmentExpression:
                    return "-=";
                case SyntaxKind.MultiplyAssignmentExpression:
                    return "*=";
                case SyntaxKind.DivideAssignmentExpression:
                    return "/=";
                case SyntaxKind.ModuloAssignmentExpression:
                    return "%=";
                case SyntaxKind.AndAssignmentExpression:
                    return "&=";
                case SyntaxKind.ExclusiveOrAssignmentExpression:
                    return "^=";
                case SyntaxKind.OrAssignmentExpression:
                    return "|=";
                case SyntaxKind.LeftShiftAssignmentExpression:
                    return "<<=";
                case SyntaxKind.RightShiftAssignmentExpression:
                    return ">>=";
                case SyntaxKind.UnsignedRightShiftAssignmentExpression:
                    return ">>>=";
                default:
                    throw new Exception("No operator found");
            }
        }

        private static AssignmentExpressionSyntax GetExpressionSyntaxChange(SyntaxTree newTree)
        {
            var classType = newTree.GetCompilationUnitRoot().Members[0] as TypeDeclarationSyntax;
            var method = classType.Members[0] as MethodDeclarationSyntax;
            var block = method.Body;
            var statement = block.Statements[0] as ExpressionStatementSyntax;
            var expression = statement.Expression as AssignmentExpressionSyntax;
            return expression;
        }

        private static AssignmentExpressionSyntax GetGlobalStatementSyntaxChange(SyntaxTree newTree)
        {
            var statementType = newTree.GetCompilationUnitRoot().Members[0] as GlobalStatementSyntax;
            Assert.True(statementType.AttributeLists.Count == 0);
            Assert.True(statementType.Modifiers.Count == 0);
            var statement = statementType.Statement as ExpressionStatementSyntax;
            var expression = statement.Expression as AssignmentExpressionSyntax;
            return expression;
        }
        #endregion
    }
}
