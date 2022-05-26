// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.Composition;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.LanguageServices;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Shared.Extensions;

namespace Microsoft.CodeAnalysis.NewLines.MultipleBlankLines
{
    [ExportCodeFixProvider(LanguageNames.CSharp, LanguageNames.VisualBasic, Name = PredefinedCodeFixProviderNames.RemoveBlankLines), Shared]
    internal class MultipleBlankLinesCodeFixProvider : CodeFixProvider
    {
        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public MultipleBlankLinesCodeFixProvider()
        {
        }

        public override ImmutableArray<string> FixableDiagnosticIds
            => ImmutableArray.Create(IDEDiagnosticIds.MultipleBlankLinesDiagnosticId);

        public override Task RegisterCodeFixesAsync(CodeFixContext context)
        {
            var document = context.Document;
            var diagnostic = context.Diagnostics.First();
            context.RegisterCodeFix(CodeAction.Create(
                CodeFixesResources.Remove_extra_blank_lines,
                c => UpdateDocumentAsync(document, diagnostic, c),
                nameof(CodeFixesResources.Remove_extra_blank_lines)),
                context.Diagnostics);
            return Task.CompletedTask;
        }

        private static Task<Document> UpdateDocumentAsync(Document document, Diagnostic diagnostic, CancellationToken cancellationToken)
            => FixAllAsync(document, ImmutableArray.Create(diagnostic), cancellationToken);

        private static async Task<Document> FixAllAsync(
            Document document, ImmutableArray<Diagnostic> diagnostics, CancellationToken cancellationToken)
        {
            var syntaxKinds = document.GetRequiredLanguageService<ISyntaxKindsService>();
            var root = await document.GetRequiredSyntaxRootAsync(cancellationToken).ConfigureAwait(false);

            using var _ = PooledDictionary<SyntaxToken, SyntaxToken>.GetInstance(out var replacements);
            foreach (var diagnostic in diagnostics)
            {
                var token = root.FindToken(diagnostic.AdditionalLocations[0].SourceSpan.Start);
                var leadingTrivia = UpdateLeadingTrivia(syntaxKinds, token.LeadingTrivia);
                replacements.Add(token, token.WithLeadingTrivia(leadingTrivia));
            }

            var newRoot = root.ReplaceTokens(replacements.Keys, (token, _) => replacements[token]);

            return document.WithSyntaxRoot(newRoot);
        }

        private static SyntaxTriviaList UpdateLeadingTrivia(ISyntaxKindsService syntaxKinds, SyntaxTriviaList triviaList)
        {
            using var _ = ArrayBuilder<SyntaxTrivia>.GetInstance(out var builder);

            var currentStart = 0;

            while (currentStart < triviaList.Count)
            {
                var trivia = triviaList[currentStart];
                builder.Add(trivia);

                // If it's not an end of line, just keep going.
                if (trivia.RawKind != syntaxKinds.EndOfLineTrivia)
                {
                    currentStart++;
                    continue;
                }

                // We have a newlines.  Walk forward to get to the last newline in this sequence.
                var currentEnd = currentStart + 1;
                while (currentEnd < triviaList.Count &&
                       IsEndOfLine(syntaxKinds, triviaList, currentEnd))
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

        private static bool IsEndOfLine(ISyntaxKindsService syntaxKinds, SyntaxTriviaList triviaList, int index)
        {
            if (index >= triviaList.Count)
                return false;

            var trivia = triviaList[index];
            return trivia.RawKind == syntaxKinds.EndOfLineTrivia;
        }

        public override FixAllProvider? GetFixAllProvider()
            => FixAllProvider.Create(async (context, document, diagnostics) => await FixAllAsync(document, diagnostics, context.CancellationToken).ConfigureAwait(false));
    }
}
