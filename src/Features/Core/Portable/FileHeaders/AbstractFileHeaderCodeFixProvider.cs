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
using Microsoft.CodeAnalysis.Formatting;
using Microsoft.CodeAnalysis.LanguageServices;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.FileHeaders
{
    internal abstract class AbstractFileHeaderCodeFixProvider : CodeFixProvider
    {
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

        protected abstract AbstractFileHeaderHelper FileHeaderHelper { get; }
        protected abstract ISyntaxKinds SyntaxKinds { get; }

        protected abstract SyntaxTrivia EndOfLine(string text);
        protected abstract SyntaxTriviaList ParseLeadingTrivia(string text);

        private async Task<Document> GetTransformedDocumentAsync(Document document, CancellationToken cancellationToken)
        {
            return document.WithSyntaxRoot(await GetTransformedSyntaxRootAsync(document, cancellationToken).ConfigureAwait(false));
        }

        private async Task<SyntaxNode> GetTransformedSyntaxRootAsync(Document document, CancellationToken cancellationToken)
        {
            var tree = await document.GetRequiredSyntaxTreeAsync(cancellationToken).ConfigureAwait(false);
            var root = await tree.GetRootAsync(cancellationToken).ConfigureAwait(false);

            var options = document.Project.AnalyzerOptions.AnalyzerConfigOptionsProvider.GetOptions(tree);
            if (!options.TryGetEditorConfigOption(CodeStyleOptions.FileHeaderTemplate, out var fileHeaderTemplate)
                || string.IsNullOrEmpty(fileHeaderTemplate))
            {
                // Avoid making changes if we fail to read the expected header
                return root;
            }

            var expectedFileHeader = fileHeaderTemplate.Replace("{fileName}", Path.GetFileName(document.FilePath));

            var fileHeader = FileHeaderHelper.ParseFileHeader(root);
            SyntaxNode newSyntaxRoot;
            if (fileHeader.IsMissing)
            {
                newSyntaxRoot = AddHeader(document, root, expectedFileHeader);
            }
            else
            {
                newSyntaxRoot = ReplaceHeader(document, root, expectedFileHeader);
            }

            return newSyntaxRoot;
        }

        private SyntaxNode ReplaceHeader(Document document, SyntaxNode root, string expectedFileHeader)
        {
            // Skip single line comments, whitespace, and end of line trivia until a blank line is encountered.
            var trivia = root.GetLeadingTrivia();
            var onBlankLine = false;
            var inCopyright = true;
            var removalList = new List<int>();
            var leadingSpaces = string.Empty;
            var possibleLeadingSpaces = string.Empty;

            // Need to do this with index so we get the line endings correct.
            for (var i = 0; i < trivia.Count; i++)
            {
                var triviaLine = trivia[i];
                if (triviaLine.RawKind == SyntaxKinds.SingleLineCommentTrivia)
                {
                    if (possibleLeadingSpaces != string.Empty)
                    {
                        leadingSpaces = possibleLeadingSpaces;
                    }

                    removalList.Add(i);
                    onBlankLine = false;
                }
                else if (triviaLine.RawKind == SyntaxKinds.WhitespaceTrivia)
                {
                    if (leadingSpaces == string.Empty)
                    {
                        possibleLeadingSpaces = triviaLine.ToFullString();
                    }

                    if (inCopyright)
                    {
                        removalList.Add(i);
                    }
                }
                else if (triviaLine.RawKind == SyntaxKinds.EndOfLineTrivia)
                {
                    possibleLeadingSpaces = string.Empty;

                    if (inCopyright)
                    {
                        removalList.Add(i);
                    }

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
                trivia = trivia.RemoveAt(removalList[i]);
            }

            var newLineText = document.Project.Solution.Options.GetOption(FormattingOptions.NewLine, root.Language)!;
            var newLineTrivia = EndOfLine(newLineText);

            var newHeaderTrivia = CreateNewHeader(leadingSpaces + FileHeaderHelper.CommentPrefix, expectedFileHeader, newLineText);

            // Add a blank line after the header.
            newHeaderTrivia = newHeaderTrivia.Add(newLineTrivia);

            // Insert header at top of the file.
            return root.WithLeadingTrivia(newHeaderTrivia.Add(newLineTrivia).AddRange(trivia));
        }

        private SyntaxNode AddHeader(Document document, SyntaxNode root, string expectedFileHeader)
        {
            var newLineText = document.Project.Solution.Options.GetOption(FormattingOptions.NewLine, root.Language)!;
            var newLineTrivia = EndOfLine(newLineText);
            var newTrivia = CreateNewHeader(FileHeaderHelper.CommentPrefix, expectedFileHeader, newLineText).Add(newLineTrivia).Add(newLineTrivia);

            // Skip blank lines already at the beginning of the document, since we add our own
            var leadingTrivia = root.GetLeadingTrivia();
            var skipCount = 0;
            for (var i = 0; i < leadingTrivia.Count; i++)
            {
                if (leadingTrivia[i].RawKind == SyntaxKinds.EndOfLineTrivia)
                {
                    skipCount = i + 1;
                }
                else if (leadingTrivia[i].RawKind != SyntaxKinds.WhitespaceTrivia)
                {
                    break;
                }
            }

            newTrivia = newTrivia.AddRange(leadingTrivia.Skip(skipCount));

            return root.WithLeadingTrivia(newTrivia);
        }

        private SyntaxTriviaList CreateNewHeader(string prefixWithLeadingSpaces, string expectedFileHeader, string newLineText)
        {
            var copyrightText = GetCopyrightText(prefixWithLeadingSpaces, expectedFileHeader, newLineText);
            var newHeader = copyrightText;
            return ParseLeadingTrivia(newHeader);
        }

        private static string GetCopyrightText(string prefixWithLeadingSpaces, string copyrightText, string newLineText)
        {
            copyrightText = copyrightText.Replace("\r\n", "\n");
            var lines = copyrightText.Split('\n');
            return string.Join(newLineText, lines.Select(line =>
            {
                if (string.IsNullOrEmpty(line))
                {
                    return prefixWithLeadingSpaces;
                }
                else
                {
                    return prefixWithLeadingSpaces + " " + line;
                }
            }));
        }

        private class MyCodeAction : CodeAction.DocumentChangeAction
        {
            public MyCodeAction(Func<CancellationToken, Task<Document>> createChangedDocument)
                : base(FeaturesResources.Add_file_banner, createChangedDocument, nameof(AbstractFileHeaderCodeFixProvider))
            {
            }
        }

        private class FixAll : DocumentBasedFixAllProvider
        {
            private readonly AbstractFileHeaderCodeFixProvider _codeFixProvider;

            public FixAll(AbstractFileHeaderCodeFixProvider codeFixProvider)
            {
                _codeFixProvider = codeFixProvider;
            }

            protected override string CodeActionTitle => FeaturesResources.Add_file_banner;

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
