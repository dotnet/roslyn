// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Microsoft.CodeAnalysis.CSharp.RemoveUnreachableCode
{
    internal static class RemoveUnreachableCodeHelpers
    {
        public static ImmutableArray<StatementSyntax> GetSubsequentUnreachableStatements(StatementSyntax firstUnreachableStatement)
        {
            SyntaxList<StatementSyntax> siblingStatements;
            switch (firstUnreachableStatement.Parent)
            {
                case BlockSyntax block:
                    siblingStatements = block.Statements;
                    break;

                case SwitchSectionSyntax switchSection:
                    siblingStatements = switchSection.Statements;
                    break;

                default:
                    return ImmutableArray<StatementSyntax>.Empty;
            }

            var result = ArrayBuilder<StatementSyntax>.GetInstance();

            // Keep consuming statements after the statement that was reported unreachable and
            // fade them out as appropriate.
            var firstUnreachableStatementIndex = siblingStatements.IndexOf(firstUnreachableStatement);
            for (int i = firstUnreachableStatementIndex + 1, n = siblingStatements.Count; i < n; i++)
            {
                var nextStatement = siblingStatements[i];
                if (nextStatement.IsKind(SyntaxKind.LabeledStatement))
                {
                    // In the case of a labeled statement, we don't want to consider it unreachable as
                    // there may be a 'goto' somewhere else to that label.  If the compiler thinks that
                    // label is actually unreachable, it will give an diagnostic on that label itself 
                    // and we can use that diagnostic to fade the label and any subsequent statements.
                    break;
                }

                if (nextStatement.IsKind(SyntaxKind.LocalFunctionStatement))
                {
                    // In the case of local functions, it is legal for a local function to be declared
                    // in code that is otherwise unreachable.  It can still be called elsewhere.  If
                    // the local function itself is not called, there will be a particular diagnostic
                    // for that ("The variable XXX is declared but never used") and the user can choose
                    // if they want to remove it or not. 
                    continue;
                }

                result.Add(nextStatement);
            }

            return result.ToImmutableAndFree();
        }
    }
}
