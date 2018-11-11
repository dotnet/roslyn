// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.Editing;
using Microsoft.CodeAnalysis.Formatting;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.Editor.Wrapping
{
    internal abstract partial class AbstractWrapper
    {
        /// <summary>
        /// Class responsible for actually computing the entire set of code actions to offer the user.
        /// Contains lots of helper functionality used by all the different Wrapper implementations.
        /// </summary>
        protected abstract class AbstractCodeActionComputer<TService> where TService : AbstractWrapper
        {
            /// <summary>
            /// Annotation used so that we can track the top-most node we want to format after
            /// performing all our edits.
            /// </summary>
            private static readonly SyntaxAnnotation s_toFormatAnnotation = new SyntaxAnnotation();

            protected readonly TService Service;
            protected readonly Document OriginalDocument;
            protected readonly SourceText OriginalSourceText;
            private readonly DocumentOptionSet _options;

            protected readonly bool UseTabs;
            protected readonly int TabSize;
            protected readonly string NewLine;
            protected readonly int WrappingColumn;

            protected readonly SyntaxTriviaList NewLineTrivia;
            protected readonly SyntaxTriviaList SingleWhitespaceTrivia;
            protected readonly SyntaxTriviaList NoTrivia = default;

            public AbstractCodeActionComputer(
                TService service,
                Document document,
                SourceText originalSourceText,
                DocumentOptionSet options)
            {
                Service = service;
                OriginalDocument = document;
                OriginalSourceText = originalSourceText;
                _options = options;

                UseTabs = options.GetOption(FormattingOptions.UseTabs);
                TabSize = options.GetOption(FormattingOptions.TabSize);
                NewLine = options.GetOption(FormattingOptions.NewLine);
                WrappingColumn = options.GetOption(FormattingOptions.PreferredWrappingColumn);

                var generator = SyntaxGenerator.GetGenerator(document);
                NewLineTrivia = new SyntaxTriviaList(generator.EndOfLine(NewLine));
                SingleWhitespaceTrivia = new SyntaxTriviaList(generator.Whitespace(" "));
            }

            protected abstract Task AddTopLevelCodeActionsAsync(ArrayBuilder<CodeAction> codeActions, HashSet<string> seenDocuments, CancellationToken cancellationToken);

            protected static Edit DeleteBetween(SyntaxNodeOrToken left, SyntaxNodeOrToken right)
                => UpdateBetween(left, default, right, default(SyntaxTriviaList));

            protected static Edit UpdateBetween(
                SyntaxNodeOrToken left, SyntaxTriviaList leftTrailingTrivia,
                SyntaxNodeOrToken right, SyntaxTrivia rightLeadingTrivia)
            {
                return UpdateBetween(left, leftTrailingTrivia, right, new SyntaxTriviaList(rightLeadingTrivia));
            }

            protected static Edit UpdateBetween(
                SyntaxNodeOrToken left, SyntaxTriviaList leftTrailingTrivia,
                SyntaxNodeOrToken right, SyntaxTriviaList rightLeadingTrivia)
            {
                var leftLastToken = left.IsToken ? left.AsToken() : left.AsNode().GetLastToken();
                var rightFirstToken = right.IsToken ? right.AsToken() : right.AsNode().GetFirstToken();
                return new Edit(leftLastToken, leftTrailingTrivia, rightFirstToken, rightLeadingTrivia);
            }

            protected async Task<CodeAction> CreateCodeActionAsync(
                HashSet<string> seenDocuments,
                SyntaxNode nodeToFormat,
                ImmutableArray<Edit> edits,
                string parentTitle,
                string title,
                CancellationToken cancellationToken)
            {
                if (edits.Length == 0)
                {
                    return null;
                }

                var leftTokenToTrailingTrivia = PooledDictionary<SyntaxToken, SyntaxTriviaList>.GetInstance();
                var rightTokenToLeadingTrivia = PooledDictionary<SyntaxToken, SyntaxTriviaList>.GetInstance();

                try
                {

                    foreach (var edit in edits)
                    {
                        var span = TextSpan.FromBounds(edit.Left.Span.End, edit.Right.Span.Start);
                        var text = OriginalSourceText.ToString(span);
                        if (!string.IsNullOrWhiteSpace(text))
                        {
                            // editing some piece of non-whitespace trivia.  We don't support this.
                            return null;
                        }

                        // Make sure we're not about to make an edit that just changes the code to what
                        // is already there.
                        if (text != edit.GetNewTrivia())
                        {
                            leftTokenToTrailingTrivia.Add(edit.Left, edit.LeftTrailingTrivia);
                            rightTokenToLeadingTrivia.Add(edit.Right, edit.RightLeadingTrivia);
                        }
                    }

                    if (leftTokenToTrailingTrivia.Count == 0)
                    {
                        return null;
                    }

                    var generator = SyntaxGenerator.GetGenerator(OriginalDocument);
                    var root = await OriginalDocument.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);

                    var rewrittenRoot = root.ReplaceSyntax(
                        new [] { nodeToFormat },
                        (oldNode, newNode) => newNode.WithAdditionalAnnotations(s_toFormatAnnotation),
                        leftTokenToTrailingTrivia.Keys.Concat(rightTokenToLeadingTrivia.Keys).Distinct(),
                        (oldToken, newToken) =>
                        {
                            if (leftTokenToTrailingTrivia.TryGetValue(oldToken, out var trailingTrivia))
                            {
                                newToken = newToken.WithTrailingTrivia(trailingTrivia);
                            }

                            if (rightTokenToLeadingTrivia.TryGetValue(oldToken, out var leadingTrivia))
                            {
                                newToken = newToken.WithLeadingTrivia(leadingTrivia);
                            }

                            return newToken;
                        }, null, null);

                    var trackedNode = rewrittenRoot.GetAnnotatedNodes(s_toFormatAnnotation).Single();
                    var newDocument = OriginalDocument.WithSyntaxRoot(rewrittenRoot);

                    var formattedDocument = await Formatter.FormatAsync(
                        newDocument, trackedNode.Span, cancellationToken: cancellationToken).ConfigureAwait(false);

                    // make sure we've actually made a textual change.
                    var finalSourceText = await formattedDocument.GetTextAsync(cancellationToken).ConfigureAwait(false);

                    var originalText = OriginalSourceText.ToString();
                    var finalText = finalSourceText.ToString();

                    if (!seenDocuments.Add(finalText) ||
                        originalText == finalText)
                    {
                        return null;
                    }

                    return new WrapItemsAction(title, parentTitle, _ => Task.FromResult(formattedDocument));
                }
                finally
                {
                    leftTokenToTrailingTrivia.Free();
                    rightTokenToLeadingTrivia.Free();
                }
            }

            public async Task<ImmutableArray<CodeAction>> GetTopLevelCodeActionsAsync(CancellationToken cancellationToken)
            {
                var codeActions = ArrayBuilder<CodeAction>.GetInstance();
                var seenDocuments = new HashSet<string>();

                await AddTopLevelCodeActionsAsync(codeActions, seenDocuments, cancellationToken);

                return SortActionsByMostRecentlyUsed(codeActions.ToImmutableAndFree());
            }
        }
    }
}
