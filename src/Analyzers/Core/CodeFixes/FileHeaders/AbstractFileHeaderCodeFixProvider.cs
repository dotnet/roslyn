// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CodeStyle;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.LanguageServices;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Roslyn.Utilities;

#if !CODE_STYLE
using Microsoft.CodeAnalysis.Formatting;
#endif

namespace Microsoft.CodeAnalysis.FileHeaders
{
    internal abstract class AbstractFileHeaderCodeFixProvider : CodeFixProvider
    {
        protected abstract AbstractFileHeaderHelper FileHeaderHelper { get; }
        protected abstract ISyntaxFacts SyntaxFacts { get; }
        protected abstract ISyntaxKinds SyntaxKinds { get; }

        protected abstract SyntaxTrivia EndOfLine(string text);

        public override ImmutableArray<string> FixableDiagnosticIds { get; }
            = ImmutableArray.Create(IDEDiagnosticIds.FileHeaderMismatch);

        public override FixAllProvider GetFixAllProvider()
            => new FixAll(this);

        public override Task RegisterCodeFixesAsync(CodeFixContext context)
        {
            foreach (var diagnostic in context.Diagnostics)
            {
                context.RegisterCodeFix(
                    new MyCodeAction(cancellationToken => GetTransformedDocumentAsync(context.Document, cancellationToken)),
                    diagnostic);
            }

            return Task.CompletedTask;
        }

        private async Task<Document> GetTransformedDocumentAsync(Document document, CancellationToken cancellationToken)
            => document.WithSyntaxRoot(await GetTransformedSyntaxRootAsync(document, cancellationToken).ConfigureAwait(false));

        private Task<SyntaxNode> GetTransformedSyntaxRootAsync(Document document, CancellationToken cancellationToken)
        {
#if CODE_STYLE
            var newLineText = Environment.NewLine;
#else
            var newLineText = document.Project.Solution.Options.GetOption(FormattingOptions.NewLine, document.Project.Language);
#endif
            var newLineTrivia = EndOfLine(newLineText);

            return GetTransformedSyntaxRootAsync(SyntaxFacts, FileHeaderHelper, newLineTrivia, document, cancellationToken);
        }

        internal static async Task<SyntaxNode> GetTransformedSyntaxRootAsync(ISyntaxFacts syntaxFacts, AbstractFileHeaderHelper fileHeaderHelper, SyntaxTrivia newLineTrivia, Document document, CancellationToken cancellationToken)
        {
            var tree = await document.GetRequiredSyntaxTreeAsync(cancellationToken).ConfigureAwait(false);
            var root = await tree.GetRootAsync(cancellationToken).ConfigureAwait(false);

            if (!document.Project.AnalyzerOptions.TryGetEditorConfigOption(CodeStyleOptions2.FileHeaderTemplate, tree, out string fileHeaderTemplate)
                || string.IsNullOrEmpty(fileHeaderTemplate))
            {
                // This exception would show up as a gold bar, but as indicated we do not believe this is reachable.
                throw ExceptionUtilities.Unreachable;
            }

            var expectedFileHeader = fileHeaderTemplate.Replace("{fileName}", Path.GetFileName(document.FilePath));

            var fileHeader = fileHeaderHelper.ParseFileHeader(root);
            SyntaxNode newSyntaxRoot;
            if (fileHeader.IsMissing)
            {
                newSyntaxRoot = AddHeader(syntaxFacts, fileHeaderHelper, newLineTrivia, root, expectedFileHeader);
            }
            else
            {
                newSyntaxRoot = ReplaceHeader(syntaxFacts, fileHeaderHelper, newLineTrivia, root, expectedFileHeader);
            }

            return newSyntaxRoot;
        }

        private static SyntaxNode ReplaceHeader(ISyntaxFacts syntaxFacts, AbstractFileHeaderHelper fileHeaderHelper, SyntaxTrivia newLineTrivia, SyntaxNode root, string expectedFileHeader)
        {
            // Skip single line comments, whitespace, and end of line trivia until a blank line is encountered.
            var triviaList = root.GetLeadingTrivia();

            // True if the current line is blank so far (empty or whitespace); otherwise, false. The first line is
            // assumed to not be blank, which allows the analysis to detect a file header which follows a blank line at
            // the top of the file.
            var onBlankLine = false;

            // The set of indexes to remove from 'triviaList'. After removing these indexes, the remaining trivia (if
            // any) will be preserved in the document along with the replacement header.
            var removalList = new List<int>();

            // The number of spaces to indent the new header. This is expected to match the indentation of the header
            // which is being replaced.
            var leadingSpaces = string.Empty;

            // The number of spaces found so far on the current line. This will become 'leadingSpaces' if the spaces are
            // followed by a comment which is considered a header comment.
            var possibleLeadingSpaces = string.Empty;

            // Need to do this with index so we get the line endings correct.
            for (var i = 0; i < triviaList.Count; i++)
            {
                var triviaLine = triviaList[i];
                if (triviaLine.RawKind == syntaxFacts.SyntaxKinds.SingleLineCommentTrivia)
                {
                    if (possibleLeadingSpaces != string.Empty)
                    {
                        // One or more spaces precedes the comment. Keep track of these spaces so we can indent the new
                        // header by the same amount.
                        leadingSpaces = possibleLeadingSpaces;
                    }

                    removalList.Add(i);
                    onBlankLine = false;
                }
                else if (triviaLine.RawKind == syntaxFacts.SyntaxKinds.WhitespaceTrivia)
                {
                    if (leadingSpaces == string.Empty)
                    {
                        possibleLeadingSpaces = triviaLine.ToFullString();
                    }

                    removalList.Add(i);
                }
                else if (triviaLine.RawKind == syntaxFacts.SyntaxKinds.EndOfLineTrivia)
                {
                    possibleLeadingSpaces = string.Empty;
                    removalList.Add(i);

                    if (onBlankLine)
                    {
                        break;
                    }
                    else
                    {
                        onBlankLine = true;
                    }
                }
                else
                {
                    break;
                }
            }

            // Remove copyright lines in reverse order.
            for (var i = removalList.Count - 1; i >= 0; i--)
            {
                triviaList = triviaList.RemoveAt(removalList[i]);
            }

            var newHeaderTrivia = CreateNewHeader(syntaxFacts, leadingSpaces + fileHeaderHelper.CommentPrefix, expectedFileHeader, newLineTrivia.ToFullString());

            // Add a blank line and any remaining preserved trivia after the header.
            newHeaderTrivia = newHeaderTrivia.Add(newLineTrivia).Add(newLineTrivia).AddRange(triviaList);

            // Insert header at top of the file.
            return root.WithLeadingTrivia(newHeaderTrivia);
        }

        private static SyntaxNode AddHeader(ISyntaxFacts syntaxFacts, AbstractFileHeaderHelper fileHeaderHelper, SyntaxTrivia newLineTrivia, SyntaxNode root, string expectedFileHeader)
        {
            var newTrivia = CreateNewHeader(syntaxFacts, fileHeaderHelper.CommentPrefix, expectedFileHeader, newLineTrivia.ToFullString()).Add(newLineTrivia).Add(newLineTrivia);

            // Skip blank lines already at the beginning of the document, since we add our own
            var leadingTrivia = root.GetLeadingTrivia();
            var skipCount = 0;
            for (var i = 0; i < leadingTrivia.Count; i++)
            {
                if (leadingTrivia[i].RawKind == syntaxFacts.SyntaxKinds.EndOfLineTrivia)
                {
                    skipCount = i + 1;
                }
                else if (leadingTrivia[i].RawKind != syntaxFacts.SyntaxKinds.WhitespaceTrivia)
                {
                    break;
                }
            }

            newTrivia = newTrivia.AddRange(leadingTrivia.Skip(skipCount));

            return root.WithLeadingTrivia(newTrivia);
        }

        private static SyntaxTriviaList CreateNewHeader(ISyntaxFacts syntaxFacts, string prefixWithLeadingSpaces, string expectedFileHeader, string newLineText)
        {
            var copyrightText = GetCopyrightText(prefixWithLeadingSpaces, expectedFileHeader, newLineText);
            var newHeader = copyrightText;
            return syntaxFacts.ParseLeadingTrivia(newHeader);
        }

        private static string GetCopyrightText(string prefixWithLeadingSpaces, string copyrightText, string newLineText)
        {
            copyrightText = copyrightText.Replace("\r\n", "\n");
            var lines = copyrightText.Split('\n');
            return string.Join(newLineText, lines.Select(line =>
            {
                // Rewrite the lines of the header as comments without trailing whitespace.
                if (string.IsNullOrEmpty(line))
                {
                    // This is a blank line of the header. We want the prefix indicating the line is a comment, but no
                    // additional trailing whitespace.
                    return prefixWithLeadingSpaces;
                }
                else
                {
                    // This is a normal line of the header. We want the prefix, followed by a single space, and then the
                    // text of the header line.
                    return prefixWithLeadingSpaces + " " + line;
                }
            }));
        }

        private class MyCodeAction : CustomCodeActions.DocumentChangeAction
        {
            public MyCodeAction(Func<CancellationToken, Task<Document>> createChangedDocument)
                : base(CodeFixesResources.Add_file_header, createChangedDocument, nameof(AbstractFileHeaderCodeFixProvider))
            {
            }
        }

        private class FixAll : DocumentBasedFixAllProvider
        {
            private readonly AbstractFileHeaderCodeFixProvider _codeFixProvider;

            public FixAll(AbstractFileHeaderCodeFixProvider codeFixProvider)
                => _codeFixProvider = codeFixProvider;

            protected override string CodeActionTitle => CodeFixesResources.Add_file_header;

            protected override Task<SyntaxNode?> FixAllInDocumentAsync(FixAllContext fixAllContext, Document document, ImmutableArray<Diagnostic> diagnostics)
            {
                if (diagnostics.IsEmpty)
                {
                    return SpecializedTasks.Null<SyntaxNode>();
                }

                return _codeFixProvider.GetTransformedSyntaxRootAsync(document, fixAllContext.CancellationToken).AsNullable();
            }
        }
    }
}
