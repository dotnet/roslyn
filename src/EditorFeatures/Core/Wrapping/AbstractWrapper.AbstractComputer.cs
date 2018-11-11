// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

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
    internal readonly struct Edit
    {
        public readonly SyntaxToken Left;
        public readonly SyntaxToken Right;
        public readonly string NewTrivia;

        public Edit(SyntaxToken left, SyntaxToken right, string newTrivia)
        {
            Left = left;
            Right = right;
            NewTrivia = newTrivia;
        }
    }

    internal abstract partial class AbstractWrapper
    {
        /// <summary>
        /// Class responsible for actually computing the entire set of code actions to offer the user.
        /// Contains lots of helper functionality used by all the different Wrapper implementations.
        /// </summary>
        protected abstract class AbstractComputer<TService> where TService : AbstractWrapper
        {
            private static readonly SyntaxAnnotation s_toFormatAnnotation = new SyntaxAnnotation();

            protected readonly TService Service;
            protected readonly Document OriginalDocument;
            protected readonly SourceText OriginalSourceText;
            private readonly DocumentOptionSet _options;

            protected readonly bool UseTabs;
            protected readonly int TabSize;
            protected readonly string NewLine;
            protected readonly int WrappingColumn;

            public AbstractComputer(
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
            }

            protected abstract Task AddTopLevelCodeActionsAsync(ArrayBuilder<CodeAction> codeActions, HashSet<string> seenDocuments, CancellationToken cancellationToken);

            protected static Edit DeleteBetween(SyntaxNodeOrToken left, SyntaxNodeOrToken right)
                => UpdateBetween(left, right, "");

            protected static Edit UpdateBetween(SyntaxNodeOrToken left, SyntaxNodeOrToken right, string text)
            {
                var leftLastToken = left.IsToken ? left.AsToken() : left.AsNode().GetLastToken();
                var rightFirstToken = right.IsToken ? right.AsToken() : right.AsNode().GetFirstToken();
                return new Edit(leftLastToken, rightFirstToken, text);
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

                var leftTokenToNewTrailingTrivia = PooledDictionary<SyntaxToken, string>.GetInstance();
                var rightTokens = PooledHashSet<SyntaxToken>.GetInstance();

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
                        if (text != edit.NewTrivia)
                        {
                            leftTokenToNewTrailingTrivia.Add(edit.Left, edit.NewTrivia);
                            rightTokens.Add(edit.Right);
                        }
                    }

                    if (leftTokenToNewTrailingTrivia.Count == 0)
                    {
                        return null;
                    }

                    var generator = SyntaxGenerator.GetGenerator(OriginalDocument);
                    var root = await OriginalDocument.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);

                    var rewrittenRoot = root.ReplaceSyntax(
                        new [] { nodeToFormat },
                        (oldNode, newNode) => newNode.WithAdditionalAnnotations(s_toFormatAnnotation),
                        leftTokenToNewTrailingTrivia.Keys.Concat(rightTokens).Distinct(),
                        (oldToken, newToken) =>
                        {
                            if (leftTokenToNewTrailingTrivia.TryGetValue(oldToken, out var trivia))
                            {
                                return newToken.WithTrailingTrivia(generator.Whitespace(trivia));
                            }

                            if (rightTokens.Contains(oldToken))
                            {
                                return newToken.WithLeadingTrivia(default(SyntaxTriviaList));
                            }

                            return newToken;
                        }, null, null);

                    // root = root.TrackNodes(nodeToFormat);
                    // var rewrittenRoot = Service.Rewrite(root, edits);
                    var trackedNode = rewrittenRoot.GetAnnotatedNodes(s_toFormatAnnotation).Single();

                    //var root = await OriginalDocument.GetSyntaxRootAsync(cancellationToken);
                    //var editor = new SyntaxEditor(root, OriginalDocument.Project.Solution.Workspace);
                    //editor.ReplaceNode(nodeToFormat, (n, _) => n.WithAdditionalAnnotations(s_toFormatAnnotation));

                    //foreach (var edit in edits)
                    //{
                    //    editor.Generator.token
                    //}

                    //var newSourceText = OriginalSourceText.WithChanges(finalEdits);
                    //var newDocument = OriginalDocument.WithText(newSourceText);

                    //var newRoot = 
                    //var currentNode = new
                    //var spanToFormat = await GetSpanToFormatAsync(newDocument, cancellationToken).ConfigureAwait(false);

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
                    leftTokenToNewTrailingTrivia.Free();
                    rightTokens.Free();
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
