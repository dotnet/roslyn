// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Microsoft.CodeAnalysis.CSharp.SplitOrMergeIfStatements
{
    internal static class Helpers
    {
        public static bool IsConditionOfIfStatement(SyntaxNode expression, out SyntaxNode ifStatementNode)
        {
            if (expression.Parent is IfStatementSyntax ifStatement && ifStatement.Condition == expression)
            {
                ifStatementNode = ifStatement;
                return true;
            }

            ifStatementNode = null;
            return false;
        }
    }
}
