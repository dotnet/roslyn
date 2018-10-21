// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis.CodeRefactorings;
using Microsoft.CodeAnalysis.LanguageServices;

namespace Microsoft.CodeAnalysis.SplitOrMergeIfStatements
{
    internal abstract class AbstractMergeIfStatementsCodeRefactoringProvider : CodeRefactoringProvider
    {
        protected abstract string IfKeywordText { get; }

        protected static IReadOnlyList<SyntaxNode> WalkDownBlocks(ISyntaxFactsService syntaxFacts, IReadOnlyList<SyntaxNode> statements)
        {
            while (statements.Count == 1 && syntaxFacts.IsPureBlock(statements[0]))
            {
                statements = syntaxFacts.GetExecutableBlockStatements(statements[0]);
            }

            return statements;
        }

        protected static IReadOnlyList<SyntaxNode> WalkUpBlocks(ISyntaxFactsService syntaxFacts, IReadOnlyList<SyntaxNode> statements)
        {
            while (statements.Count > 0 && syntaxFacts.IsPureBlock(statements[0].Parent) &&
                   syntaxFacts.GetExecutableBlockStatements(statements[0].Parent).Count == statements.Count)
            {
                statements = ImmutableArray.Create(statements[0].Parent);
            }

            return statements;
        }
    }
}
