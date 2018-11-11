// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.Formatting;
using Microsoft.CodeAnalysis.LanguageServices;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.Editor.Wrapping
{
    internal abstract partial class AbstractWrapper
    {
        /// <summary>
        /// Class responsible for actually computing the entire set of code actions to offer the user.
        /// Contains lots of helper functionality used by all the different Wrapper implementations.
        /// </summary>
        protected abstract class AbstractComputer<TService> where TService : AbstractWrapper
        {
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
            protected abstract Task<TextSpan> GetSpanToFormatAsync(Document newDocument, CancellationToken cancellationToken);

            protected static TextChange DeleteBetween(SyntaxNodeOrToken left, SyntaxNodeOrToken right)
                => UpdateBetween(left, right, "");

            protected static TextChange UpdateBetween(SyntaxNodeOrToken left, SyntaxNodeOrToken right, string text)
                => new TextChange(TextSpan.FromBounds(left.Span.End, right.Span.Start), text);

            protected async Task<CodeAction> CreateCodeActionAsync(
                HashSet<string> seenDocuments, ImmutableArray<TextChange> edits,
                string parentTitle, string title, CancellationToken cancellationToken)
            {
                if (edits.Length == 0)
                {
                    return null;
                }

                var finalEdits = ArrayBuilder<TextChange>.GetInstance();

                try
                {
                    foreach (var edit in edits)
                    {
                        var text = OriginalSourceText.ToString(edit.Span);
                        if (!string.IsNullOrWhiteSpace(text))
                        {
                            // editing some piece of non-whitespace trivia.  We don't support this.
                            return null;
                        }

                        // Make sure we're not about to make an edit that just changes the code to what
                        // is already there.
                        if (text != edit.NewText)
                        {
                            finalEdits.Add(edit);
                        }
                    }

                    if (finalEdits.Count == 0)
                    {
                        return null;
                    }

                    var newSourceText = OriginalSourceText.WithChanges(finalEdits);
                    var newDocument = OriginalDocument.WithText(newSourceText);

                    var spanToFormat = await GetSpanToFormatAsync(newDocument, cancellationToken).ConfigureAwait(false);

                    var formattedDocument = await Formatter.FormatAsync(
                        newDocument, spanToFormat, cancellationToken: cancellationToken).ConfigureAwait(false);

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
                    finalEdits.Free();
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
