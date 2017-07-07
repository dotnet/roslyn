﻿// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.PooledObjects;

namespace Microsoft.CodeAnalysis.CSharp.RemoveUnreachableCode
{
    internal static class RemoveUnreachableCodeHelpers
    {
        public static ImmutableArray<ImmutableArray<StatementSyntax>> GetSubsequentUnreachableSections(StatementSyntax firstUnreachableStatement)
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
                    // We're an embedded statement.  So the unreachable section is just us.
                    return ImmutableArray<ImmutableArray<StatementSyntax>>.Empty;
            }

            var sections = ArrayBuilder<ImmutableArray<StatementSyntax>>.GetInstance();

            var currentSection = ArrayBuilder<StatementSyntax>.GetInstance();
            var firstUnreachableStatementIndex = siblingStatements.IndexOf(firstUnreachableStatement);

            for (int i = firstUnreachableStatementIndex + 1, n = siblingStatements.Count; i < n; i++)
            {
                var currentStatement = siblingStatements[i];
                if (currentStatement.IsKind(SyntaxKind.LabeledStatement))
                {
                    // In the case of a subsequent labeled statement, we don't want to consider it 
                    // unreachable as there may be a 'goto' somewhere else to that label.  If the 
                    // compiler actually thinks that label is unreachable, it will give an diagnostic 
                    // on that label itself  and we can use that diagnostic to handle it and any 
                    // subsequent sections.
                    break;
                }

                if (currentStatement.IsKind(SyntaxKind.LocalFunctionStatement))
                {
                    // In the case of local functions, it is legal for a local function to be declared
                    // in code that is otherwise unreachable.  It can still be called elsewhere.  If
                    // the local function itself is not called, there will be a particular diagnostic
                    // for that ("The variable XXX is declared but never used") and the user can choose
                    // if they want to remove it or not. 
                    var section = currentSection.ToImmutableAndFree();
                    AddIfNonEmpty(sections, section);

                    currentSection = ArrayBuilder<StatementSyntax>.GetInstance();
                    continue;
                }

                currentSection.Add(currentStatement);
            }

            var lastSection = currentSection.ToImmutableAndFree();
            AddIfNonEmpty(sections, lastSection);

            return sections.ToImmutableAndFree();
        }

        private static void AddIfNonEmpty(ArrayBuilder<ImmutableArray<StatementSyntax>> sections, ImmutableArray<StatementSyntax> lastSection)
        {
            if (!lastSection.IsEmpty)
            {
                sections.Add(lastSection);
            }
        }
    }
}
