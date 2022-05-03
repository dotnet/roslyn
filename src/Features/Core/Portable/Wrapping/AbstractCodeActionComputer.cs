// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.Editing;
using Microsoft.CodeAnalysis.Formatting;
using Microsoft.CodeAnalysis.Indentation;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;
using static Microsoft.CodeAnalysis.CodeActions.CodeAction;

namespace Microsoft.CodeAnalysis.Wrapping
{
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
            private readonly List<SyntaxNode> _seenDocumentRoots = new();

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
                NewLineTrivia = new SyntaxTriviaList(generatorInternal.EndOfLine(options.NewLine));
                SingleWhitespaceTrivia = new SyntaxTriviaList(generator.Whitespace(" "));
            }

            protected abstract Task<ImmutableArray<WrappingGroup>> ComputeWrappingGroupsAsync();

            protected string GetSmartIndentationAfter(SyntaxNodeOrToken nodeOrToken)
                => GetIndentationAfter(nodeOrToken, FormattingOptions.IndentStyle.Smart);

            protected string GetIndentationAfter(SyntaxNodeOrToken nodeOrToken, FormattingOptions.IndentStyle indentStyle)
            {
                var newSourceText = OriginalSourceText.WithChanges(new TextChange(new TextSpan(nodeOrToken.Span.End, 0), Options.NewLine));
                newSourceText = newSourceText.WithChanges(
                    new TextChange(TextSpan.FromBounds(nodeOrToken.Span.End + Options.NewLine.Length, newSourceText.Length), ""));
                var newDocument = OriginalDocument.WithText(newSourceText);

                var indentationService = Wrapper.IndentationService;
                var originalLineNumber = newSourceText.Lines.GetLineFromPosition(nodeOrToken.Span.End).LineNumber;
                var desiredIndentation = indentationService.GetIndentation(
                    newDocument, originalLineNumber + 1,
                    indentStyle,
                    CancellationToken);

                return desiredIndentation.GetIndentationString(newSourceText, Options.UseTabs, Options.TabSize);
            }

            /// <summary>
            /// Try to create a CodeAction representing these edits.  Can return <see langword="null"/> in several 
            /// cases, including:
            /// 
            ///     1. No edits.
            ///     2. Edits would change more than whitespace.
            ///     3. A previous code action was created that already had the same effect.
            /// </summary>
            protected async Task<WrapItemsAction> TryCreateCodeActionAsync(
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
                var formattedRoot = await formattedDocument.GetSyntaxRootAsync(CancellationToken).ConfigureAwait(false);

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

                return new WrapItemsAction(title, parentTitle, _ => Task.FromResult(formattedDocument));
            }

            private async Task<Document> FormatDocumentAsync(SyntaxNode rewrittenRoot, TextSpan spanToFormat)
            {
                var newDocument = OriginalDocument.WithSyntaxRoot(rewrittenRoot);
                var formattedDocument = await Formatter.FormatAsync(
                    newDocument, spanToFormat, cancellationToken: CancellationToken).ConfigureAwait(false);
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
                var root = await OriginalDocument.GetSyntaxRootAsync(CancellationToken).ConfigureAwait(false);
                var tokens = leftTokenToTrailingTrivia.Keys.Concat(rightTokenToLeadingTrivia.Keys).Distinct().ToImmutableArray();

                // Find the closest node that contains all the tokens we're editing.  That's the
                // node we'll format at the end.  This will ensure that all formattin respects
                // user settings for things like spacing around commas/operators/etc.
                var nodeToFormat = tokens.SelectAsArray(t => t.Parent).FindInnermostCommonNode<SyntaxNode>();

                // Rewrite the tree performing the following actions:
                //
                //  1. Add an annotation to nodeToFormat so that we can find that node again after
                //     updating all tokens.
                //
                //  2. Hit all tokens in the two passed in maps, and update their leading/trailing
                //     trivia accordingly.

                var rewrittenRoot = root.ReplaceSyntax(
                    nodes: new[] { nodeToFormat },
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

                var trackedNode = rewrittenRoot.GetAnnotatedNodes(s_toFormatAnnotation).Single();

                return (root, rewrittenRoot, trackedNode.Span);
            }

            public async Task<ImmutableArray<CodeAction>> GetTopLevelCodeActionsAsync()
            {
                // Ask subclass to produce whole nested list of wrapping code actions
                var wrappingGroups = await ComputeWrappingGroupsAsync().ConfigureAwait(false);

                var result = ArrayBuilder<CodeAction>.GetInstance();
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
                    result.Add(new CodeActionWithNestedActions(
                        wrappingActions[0].ParentTitle, sorted,
                        group.IsInlinable, CodeActionPriority.Low));
                }

                // Finally, sort the topmost list we're building and return that.  This ensures that
                // both the top level items and the nested items are ordered appropriate.
                return WrapItemsAction.SortActionsByMostRecentlyUsed(result.ToImmutableAndFree());
            }
        }
    }
}
