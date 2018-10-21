// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeRefactorings;
using Microsoft.CodeAnalysis.LanguageServices;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.SplitOrMergeIfStatements
{
    internal abstract class AbstractMergeIfStatementsCodeRefactoringProvider : CodeRefactoringProvider
    {
        protected abstract string IfKeywordText { get; }

        protected abstract bool IsApplicableSpan(SyntaxNode node, TextSpan span, out SyntaxNode ifStatement);

        protected abstract bool IsIfStatement(SyntaxNode statement);

        protected abstract Task ComputeRefactoringsAsync(
            CodeRefactoringContext context, SyntaxNode ifStatement, ISyntaxFactsService syntaxFacts);

        public sealed override async Task ComputeRefactoringsAsync(CodeRefactoringContext context)
        {
            var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);
            var node = root.FindNode(context.Span, getInnermostNodeForTie: true);

            if (IsApplicableSpan(node, context.Span, out var ifStatement))
            {
                await ComputeRefactoringsAsync(context, ifStatement, context.Document.GetLanguageService<ISyntaxFactsService>());
            }
        }

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
