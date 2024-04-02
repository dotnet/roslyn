// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.Editing;
using Microsoft.CodeAnalysis.ErrorReporting;
using Microsoft.CodeAnalysis.Formatting;
using Microsoft.CodeAnalysis.Indentation;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Shared.Collections;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Wrapping;

internal abstract partial class AbstractSyntaxWrapper
{
    /// <summary>
    /// Class responsible for actually computing the entire set of code actions to offer the
    /// user.  Contains lots of helper functionality used by all the different Wrapper
    /// implementations.
    /// 
    /// Specifically subclasses of this type can simply provide a list of code-actions to
    /// perform.  This type will then take those code actions and will ensure there aren't
    /// multiple code actions that end up having the same effect on the document.  For example,
    /// a "wrap all" action may produce the same results as a "wrap long" action.  In that case
    /// this type will only keep around the first of those actions to prevent showing the user
    /// something that will be unclear.
    /// </summary>
    protected abstract class AbstractCodeActionComputer<TWrapper> : ICodeActionComputer
        where TWrapper : AbstractSyntaxWrapper
    {
        /// <summary>
        /// Annotation used so that we can track the top-most node we want to format after
        /// performing all our edits.
        /// </summary>
        private static readonly SyntaxAnnotation s_toFormatAnnotation = new();

        protected readonly TWrapper Wrapper;

        protected readonly Document OriginalDocument;
        protected readonly SourceText OriginalSourceText;
        protected readonly CancellationToken CancellationToken;
        protected readonly SyntaxWrappingOptions Options;

        protected readonly SyntaxTriviaList NewLineTrivia;
        protected readonly SyntaxTriviaList SingleWhitespaceTrivia;
        protected readonly SyntaxTriviaList NoTrivia;

        /// <summary>
        /// The contents of the documents we've created code-actions for.  This is used so that
        /// we can prevent creating multiple code actions that produce the same results.
        /// </summary>
        private readonly List<SyntaxNode> _seenDocumentRoots = [];

        public AbstractCodeActionComputer(
            TWrapper service,
            Document document,
            SourceText originalSourceText,
            SyntaxWrappingOptions options,
            CancellationToken cancellationToken)
        {
            Wrapper = service;
            OriginalDocument = document;
            OriginalSourceText = originalSourceText;
            CancellationToken = cancellationToken;
            Options = options;

            var generator = SyntaxGenerator.GetGenerator(document);
            var generatorInternal = document.GetRequiredLanguageService<SyntaxGeneratorInternal>();
            NewLineTrivia = new SyntaxTriviaList(generatorInternal.EndOfLine(options.FormattingOptions.NewLine));
            SingleWhitespaceTrivia = new SyntaxTriviaList(generator.Whitespace(" "));
        }

        protected abstract Task<ImmutableArray<WrappingGroup>> ComputeWrappingGroupsAsync();

        protected string GetSmartIndentationAfter(SyntaxNodeOrToken nodeOrToken)
            => GetIndentationAfter(nodeOrToken, FormattingOptions2.IndentStyle.Smart);

        protected string GetIndentationAfter(SyntaxNodeOrToken nodeOrToken, FormattingOptions2.IndentStyle indentStyle)
        {
            var newLine = Options.FormattingOptions.NewLine;
            var newSourceText = OriginalSourceText.WithChanges(
                new TextChange(TextSpan.FromBounds(nodeOrToken.Span.End, OriginalSourceText.Length), newLine));

            var newDocument = OriginalDocument.WithText(newSourceText);

            // The only auto-formatting option that's relevant is indent style. Others only control behavior on typing.
            var indentationOptions = new IndentationOptions(Options.FormattingOptions) { IndentStyle = indentStyle };

            var indentationService = Wrapper.IndentationService;
            var originalLineNumber = newSourceText.Lines.GetLineFromPosition(nodeOrToken.Span.End).LineNumber;

            // TODO: should be async https://github.com/dotnet/roslyn/issues/61998
            var newParsedDocument = ParsedDocument.CreateSynchronously(newDocument, CancellationToken);

            var desiredIndentation = indentationService.GetIndentation(
                newParsedDocument, originalLineNumber + 1,
                indentationOptions,
                CancellationToken);

            return desiredIndentation.GetIndentationString(newSourceText, Options.FormattingOptions.UseTabs, Options.FormattingOptions.TabSize);
        }

        /// <summary>
        /// Try to create a CodeAction representing these edits.  Can return <see langword="null"/> in several 
        /// cases, including:
        /// 
        ///     1. No edits.
        ///     2. Edits would change more than whitespace.
        ///     3. A previous code action was created that already had the same effect.
        /// </summary>
        protected async Task<WrapItemsAction?> TryCreateCodeActionAsync(
            ImmutableArray<Edit> edits, string parentTitle, string title)
        {
            // First, rewrite the tree with the edits provided.
            var (root, rewrittenRoot, spanToFormat) = await RewriteTreeAsync(edits).ConfigureAwait(false);
            if (rewrittenRoot == null)
            {
                // Couldn't rewrite for some reason.  No code action to create.
                return null;
            }

            // Now, format the part of the tree that we edited.  This will ensure we properly 
            // respect the user preferences around things like comma/operator spacing.
            var formattedDocument = await FormatDocumentAsync(rewrittenRoot, spanToFormat).ConfigureAwait(false);
            var formattedRoot = await formattedDocument.GetRequiredSyntaxRootAsync(CancellationToken).ConfigureAwait(false);

            // Now, check if this new formatted tree matches our starting tree, or any of the
            // trees we've already created for our other code actions.  If so, we don't want to
            // add this duplicative code action.  Note: this check will actually run quickly.
            // 'IsEquivalentTo' can return quickly when comparing equivalent green nodes.  So
            // all that we need to check is the spine of the change which will happen very
            // quickly.

            if (root.IsEquivalentTo(formattedRoot))
            {
                return null;
            }

            foreach (var seenRoot in _seenDocumentRoots)
            {
                if (seenRoot.IsEquivalentTo(formattedRoot))
                {
                    return null;
                }
            }

            // This is a genuinely different code action from all previous ones we've created.
            // Store the root so we don't just end up creating this code action again.
            _seenDocumentRoots.Add(formattedRoot);

            return new WrapItemsAction(title, parentTitle, (_, _) => Task.FromResult(formattedDocument));
        }

        private async Task<Document> FormatDocumentAsync(SyntaxNode rewrittenRoot, TextSpan spanToFormat)
        {
            var newDocument = OriginalDocument.WithSyntaxRoot(rewrittenRoot);
            var formattedDocument = await Formatter.FormatAsync(
                newDocument, spanToFormat, Options.FormattingOptions, CancellationToken).ConfigureAwait(false);
            return formattedDocument;
        }

        private async Task<(SyntaxNode root, SyntaxNode rewrittenRoot, TextSpan spanToFormat)> RewriteTreeAsync(ImmutableArray<Edit> edits)
        {
            using var _1 = PooledDictionary<SyntaxToken, SyntaxTriviaList>.GetInstance(out var leftTokenToTrailingTrivia);
            using var _2 = PooledDictionary<SyntaxToken, SyntaxTriviaList>.GetInstance(out var rightTokenToLeadingTrivia);

            foreach (var edit in edits)
            {
                var span = TextSpan.FromBounds(edit.Left.Span.End, edit.Right.Span.Start);
                var text = OriginalSourceText.ToString(span);
                if (!IsSafeToRemove(text))
                {
                    // editing some piece of non-whitespace trivia.  We don't support this.
                    return default;
                }

                // Make sure we're not about to make an edit that just changes the code to what
                // is already there.
                if (text != edit.GetNewTrivia())
                {
                    leftTokenToTrailingTrivia.Add(edit.Left, edit.NewLeftTrailingTrivia);
                    rightTokenToLeadingTrivia.Add(edit.Right, edit.NewRightLeadingTrivia);
                }
            }

            if (leftTokenToTrailingTrivia.Count == 0)
            {
                // No actual edits that would change anything.  Nothing to do.
                return default;
            }

            return await RewriteTreeAsync(
                leftTokenToTrailingTrivia, rightTokenToLeadingTrivia).ConfigureAwait(false);
        }

        private static bool IsSafeToRemove(string text)
        {
            foreach (var ch in text)
            {
                // It's safe to remove whitespace between tokens, or the VB line-continuation character.
                if (!char.IsWhiteSpace(ch) && ch != '_')
                {
                    return false;
                }
            }

            return true;
        }

        private async Task<(SyntaxNode root, SyntaxNode rewrittenRoot, TextSpan spanToFormat)> RewriteTreeAsync(
            Dictionary<SyntaxToken, SyntaxTriviaList> leftTokenToTrailingTrivia,
            Dictionary<SyntaxToken, SyntaxTriviaList> rightTokenToLeadingTrivia)
        {
            var root = await OriginalDocument.GetRequiredSyntaxRootAsync(CancellationToken).ConfigureAwait(false);
            var tokens = leftTokenToTrailingTrivia.Keys.Concat(rightTokenToLeadingTrivia.Keys).Distinct().ToImmutableArray();

            // Find the closest node that contains all the tokens we're editing.  That's the
            // node we'll format at the end.  This will ensure that all formattin respects
            // user settings for things like spacing around commas/operators/etc.
            var nodeToFormat = tokens.SelectAsArray(t => t.Parent).FindInnermostCommonNode<SyntaxNode>();
            Contract.ThrowIfNull(nodeToFormat);

            // Rewrite the tree performing the following actions:
            //
            //  1. Add an annotation to nodeToFormat so that we can find that node again after
            //     updating all tokens.
            //
            //  2. Hit all tokens in the two passed in maps, and update their leading/trailing
            //     trivia accordingly.

            var rewrittenRoot = root.ReplaceSyntax(
                nodes: [nodeToFormat],
                computeReplacementNode: (oldNode, newNode) => newNode.WithAdditionalAnnotations(s_toFormatAnnotation),

                tokens: leftTokenToTrailingTrivia.Keys.Concat(rightTokenToLeadingTrivia.Keys).Distinct(),
                computeReplacementToken: (oldToken, newToken) =>
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
                },
                trivia: null,
                computeReplacementTrivia: null);

            Contract.ThrowIfNull(rewrittenRoot);
            var trackedNode = rewrittenRoot.GetAnnotatedNodes(s_toFormatAnnotation).Single();

            return (root, rewrittenRoot, trackedNode.Span);
        }

        public async Task<ImmutableArray<CodeAction>> GetTopLevelCodeActionsAsync()
        {
            try
            {
                // Ask subclass to produce whole nested list of wrapping code actions
                var wrappingGroups = await ComputeWrappingGroupsAsync().ConfigureAwait(false);

                using var result = TemporaryArray<CodeAction>.Empty;
                foreach (var group in wrappingGroups)
                {
                    // if a group is empty just ignore it.
                    var wrappingActions = group.WrappingActions.WhereNotNull().ToImmutableArray();
                    if (wrappingActions.Length == 0)
                    {
                        continue;
                    }

                    // If a group only has one item, and subclass says the item is inlinable,
                    // then just directly return that nested item as a top level item.
                    if (wrappingActions.Length == 1 && group.IsInlinable)
                    {
                        result.Add(wrappingActions[0]);
                        continue;
                    }

                    // Otherwise, sort items and add to the resultant list
                    var sorted = WrapItemsAction.SortActionsByMostRecentlyUsed(ImmutableArray<CodeAction>.CastUp(wrappingActions));

                    // Make our code action low priority.  This option will be offered *a lot*, and 
                    // much of  the time will not be something the user particularly wants to do.  
                    // It should be offered after all other normal refactorings.
                    result.Add(CodeAction.Create(
                        wrappingActions[0].ParentTitle, sorted,
                        group.IsInlinable, CodeActionPriority.Low));
                }

                // Finally, sort the topmost list we're building and return that.  This ensures that
                // both the top level items and the nested items are ordered appropriate.
                return WrapItemsAction.SortActionsByMostRecentlyUsed(result.ToImmutableAndClear());
            }
            catch (Exception ex) when (FatalError.ReportAndCatchUnlessCanceled(ex, CancellationToken, ErrorSeverity.Diagnostic))
            {
                throw;
            }
        }
    }
}
