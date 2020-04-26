// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Analyzer.Utilities.PooledObjects;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Roslyn.Diagnostics.Analyzers;

namespace Roslyn.Diagnostics.CSharp.Analyzers.BlankLines
{
    public abstract class AbstractBlankLinesCodeFixProvider : CodeFixProvider
    {
        protected abstract bool IsEndOfLine(SyntaxTrivia trivia);

        public override ImmutableArray<string> FixableDiagnosticIds
            => ImmutableArray.Create(RoslynDiagnosticIds.BlankLinesRuleId);

        public override FixAllProvider GetFixAllProvider()
            => WellKnownFixAllProviders.BatchFixer;

        public override Task RegisterCodeFixesAsync(CodeFixContext context)
        {
            var document = context.Document;
            var diagnostic = context.Diagnostics.First();
            context.RegisterCodeFix(
                CodeAction.Create(
                    RoslynDiagnosticsAnalyzersResources.Remove_extra_blank_lines,
                    c => UpdateDocumentAsync(document, diagnostic, c),
                    RoslynDiagnosticsAnalyzersResources.Remove_extra_blank_lines),
                context.Diagnostics);
            return Task.CompletedTask;
        }

        private async Task<Document> UpdateDocumentAsync(
            Document document,
            Diagnostic diagnostic,
            CancellationToken cancellationToken)
        {
            var root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
            var token = root.FindToken(diagnostic.AdditionalLocations[0].SourceSpan.Start);
            var leadingTrivia = UpdateLeadingTrivia(token.LeadingTrivia);

            var newRoot = root.ReplaceToken(token, token.WithLeadingTrivia(leadingTrivia));
            return document.WithSyntaxRoot(newRoot);
        }

        private SyntaxTriviaList UpdateLeadingTrivia(SyntaxTriviaList triviaList)
        {
            var builder = ArrayBuilder<SyntaxTrivia>.GetInstance();

            var currentStart = 0;

            while (currentStart < triviaList.Count)
            {
                var trivia = triviaList[currentStart];
                builder.Add(trivia);

                // If it's not an end of line, just keep going.
                if (!IsEndOfLine(trivia))
                {
                    currentStart++;
                    continue;
                }

                // We have a newlines.  Walk forward to get to the last newline in this sequence.
                var currentEnd = currentStart + 1;
                while (currentEnd < triviaList.Count &&
                       IsEndOfLine(triviaList, currentEnd))
                {
                    currentEnd++;
                }

                var newLineCount = currentEnd - currentStart;
                if (newLineCount == 1)
                {
                    // only a single newline.  keep as is.
                    currentStart = currentEnd;
                    continue;
                }

                // we have two or more newlines.  We have three cases to handle:
                //
                // 1. We're at the start of the token's trivia.  Collapse this down to 1 blank line.
                // 2. We follow structured trivia (i.e. pp-directive or doc comment).  These already end with a newline,
                //    so we only need to add one newline to get a blank line.
                // 3. We follow something else.  We only want to collapse if we have 3 or more newlines.

                if (currentStart == 0)
                {
                    // case 1.
                    // skip the second newline onwards.
                    currentStart = currentEnd;
                    continue;
                }

                if (triviaList[currentStart - 1].HasStructure)
                {
                    // case 2.
                    // skip the second newline onwards
                    currentStart = currentEnd;
                    continue;
                }

                if (newLineCount >= 3)
                {
                    // case 3.  We want to keep the first two newlines to end up with one blank line,
                    // and then skip the rest.
                    builder.Add(triviaList[currentStart + 1]);
                    currentStart = currentEnd;
                    continue;
                }

                // for anything else just add the trivia and move forward like normal.
                currentStart++;
            }

            return new SyntaxTriviaList(builder.ToImmutable());
        }

        private bool IsEndOfLine(SyntaxTriviaList triviaList, int index)
        {
            if (index >= triviaList.Count)
                return false;

            var trivia = triviaList[index];
            return IsEndOfLine(trivia);
        }
    }
}
