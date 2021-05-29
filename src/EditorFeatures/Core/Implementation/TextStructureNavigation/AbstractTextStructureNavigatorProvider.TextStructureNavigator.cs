// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis.Internal.Log;
using Microsoft.CodeAnalysis.Text;
using Microsoft.CodeAnalysis.Text.Shared.Extensions;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Operations;
using Microsoft.VisualStudio.Utilities;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Editor.Implementation.TextStructureNavigation
{
    internal partial class AbstractTextStructureNavigatorProvider
    {
        private class TextStructureNavigator : ITextStructureNavigator
        {
            private readonly ITextBuffer _subjectBuffer;
            private readonly ITextStructureNavigator _naturalLanguageNavigator;
            private readonly AbstractTextStructureNavigatorProvider _provider;
            private readonly IUIThreadOperationExecutor _uiThreadOperationExecutor;

            internal TextStructureNavigator(
                ITextBuffer subjectBuffer,
                ITextStructureNavigator naturalLanguageNavigator,
                AbstractTextStructureNavigatorProvider provider,
                IUIThreadOperationExecutor uIThreadOperationExecutor)
            {
                Contract.ThrowIfNull(subjectBuffer);
                Contract.ThrowIfNull(naturalLanguageNavigator);
                Contract.ThrowIfNull(provider);

                _subjectBuffer = subjectBuffer;
                _naturalLanguageNavigator = naturalLanguageNavigator;
                _provider = provider;
                _uiThreadOperationExecutor = uIThreadOperationExecutor;
            }

            public IContentType ContentType => _subjectBuffer.ContentType;

            public TextExtent GetExtentOfWord(SnapshotPoint currentPosition)
            {
                using (Logger.LogBlock(FunctionId.TextStructureNavigator_GetExtentOfWord, CancellationToken.None))
                {
                    var result = default(TextExtent);
                    _uiThreadOperationExecutor.Execute(
                        title: EditorFeaturesResources.Text_Navigation,
                        defaultDescription: EditorFeaturesResources.Finding_word_extent,
                        allowCancellation: true,
                        showProgress: false,
                        action: context =>
                    {
                        result = GetExtentOfWordWorker(currentPosition, context.UserCancellationToken);
                    });

                    return result;
                }
            }

            private TextExtent GetExtentOfWordWorker(SnapshotPoint position, CancellationToken cancellationToken)
            {
                var textLength = position.Snapshot.Length;
                if (textLength == 0)
                {
                    return _naturalLanguageNavigator.GetExtentOfWord(position);
                }

                // If at the end of the file, go back one character so stuff works
                if (position == textLength && position > 0)
                {
                    position -= 1;
                }

                // If we're at the EOL position, return the line break's extent
                var line = position.Snapshot.GetLineFromPosition(position);
                if (position >= line.End && position < line.EndIncludingLineBreak)
                {
                    return new TextExtent(new SnapshotSpan(line.End, line.EndIncludingLineBreak - line.End), isSignificant: false);
                }

                var document = GetDocument(position);
                if (document != null)
                {
                    var root = document.GetSyntaxRootSynchronously(cancellationToken);
                    var trivia = root.FindTrivia(position, findInsideTrivia: true);

                    if (trivia != default)
                    {
                        if (trivia.Span.Start == position && _provider.ShouldSelectEntireTriviaFromStart(trivia))
                        {
                            // We want to select the entire comment
                            return new TextExtent(trivia.Span.ToSnapshotSpan(position.Snapshot), isSignificant: true);
                        }
                    }

                    var token = root.FindToken(position, findInsideTrivia: true);

                    // If end of file, go back a token
                    if (token.Span.Length == 0 && token.Span.Start == textLength)
                    {
                        token = token.GetPreviousToken();
                    }

                    if (token.Span.Length > 0 && token.Span.Contains(position) && !_provider.IsWithinNaturalLanguage(token, position))
                    {
                        // Cursor position is in our domain - handle it.
                        return _provider.GetExtentOfWordFromToken(token, position);
                    }
                }

                // Fall back to natural language navigator do its thing.
                return _naturalLanguageNavigator.GetExtentOfWord(position);
            }

            public SnapshotSpan GetSpanOfEnclosing(SnapshotSpan activeSpan)
            {
                using (Logger.LogBlock(FunctionId.TextStructureNavigator_GetSpanOfEnclosing, CancellationToken.None))
                {
                    var span = default(SnapshotSpan);
                    var result = _uiThreadOperationExecutor.Execute(
                        title: EditorFeaturesResources.Text_Navigation,
                        defaultDescription: EditorFeaturesResources.Finding_enclosing_span,
                        allowCancellation: true,
                        showProgress: false,
                        action: context =>
                    {
                        span = GetSpanOfEnclosingWorker(activeSpan, context.UserCancellationToken);
                    });

                    return result == UIThreadOperationStatus.Completed ? span : activeSpan;
                }
            }

            private static SnapshotSpan GetSpanOfEnclosingWorker(SnapshotSpan activeSpan, CancellationToken cancellationToken)
            {
                // Find node that covers the entire span.
                var node = FindLeafNode(activeSpan, cancellationToken);
                if (node != null && activeSpan.Length == node.Value.Span.Length)
                {
                    // Go one level up so the span widens.
                    node = GetEnclosingNode(node.Value);
                }

                return node == null ? activeSpan : node.Value.Span.ToSnapshotSpan(activeSpan.Snapshot);
            }

            public SnapshotSpan GetSpanOfFirstChild(SnapshotSpan activeSpan)
            {
                using (Logger.LogBlock(FunctionId.TextStructureNavigator_GetSpanOfFirstChild, CancellationToken.None))
                {
                    var span = default(SnapshotSpan);
                    var result = _uiThreadOperationExecutor.Execute(
                        title: EditorFeaturesResources.Text_Navigation,
                        defaultDescription: EditorFeaturesResources.Finding_enclosing_span,
                        allowCancellation: true,
                        showProgress: false,
                        action: context =>
                    {
                        span = GetSpanOfFirstChildWorker(activeSpan, context.UserCancellationToken);
                    });

                    return result == UIThreadOperationStatus.Completed ? span : activeSpan;
                }
            }

            private static SnapshotSpan GetSpanOfFirstChildWorker(SnapshotSpan activeSpan, CancellationToken cancellationToken)
            {
                // Find node that covers the entire span.
                var node = FindLeafNode(activeSpan, cancellationToken);
                if (node != null)
                {
                    // Take first child if possible, otherwise default to node itself.
                    var firstChild = node.Value.ChildNodesAndTokens().FirstOrNull();
                    if (firstChild.HasValue)
                    {
                        node = firstChild.Value;
                    }
                }

                return node == null ? activeSpan : node.Value.Span.ToSnapshotSpan(activeSpan.Snapshot);
            }

            public SnapshotSpan GetSpanOfNextSibling(SnapshotSpan activeSpan)
            {
                using (Logger.LogBlock(FunctionId.TextStructureNavigator_GetSpanOfNextSibling, CancellationToken.None))
                {
                    var span = default(SnapshotSpan);
                    var result = _uiThreadOperationExecutor.Execute(
                        title: EditorFeaturesResources.Text_Navigation,
                        defaultDescription: EditorFeaturesResources.Finding_span_of_next_sibling,
                        allowCancellation: true,
                        showProgress: false,
                        action: context =>
                    {
                        span = GetSpanOfNextSiblingWorker(activeSpan, context.UserCancellationToken);
                    });

                    return result == UIThreadOperationStatus.Completed ? span : activeSpan;
                }
            }

            private static SnapshotSpan GetSpanOfNextSiblingWorker(SnapshotSpan activeSpan, CancellationToken cancellationToken)
            {
                // Find node that covers the entire span.
                var node = FindLeafNode(activeSpan, cancellationToken);
                if (node != null)
                {
                    // Get ancestor with a wider span.
                    var parent = GetEnclosingNode(node.Value);
                    if (parent != null)
                    {
                        // Find node immediately after the current in the children collection.
                        var nodeOrToken = parent.Value
                            .ChildNodesAndTokens()
                            .SkipWhile(child => child != node)
                            .Skip(1)
                            .FirstOrNull();

                        if (nodeOrToken.HasValue)
                        {
                            node = nodeOrToken.Value;
                        }
                        else
                        {
                            // If this is the last node, move to the parent so that the user can continue 
                            // navigation at the higher level.
                            node = parent.Value;
                        }
                    }
                }

                return node == null ? activeSpan : node.Value.Span.ToSnapshotSpan(activeSpan.Snapshot);
            }

            public SnapshotSpan GetSpanOfPreviousSibling(SnapshotSpan activeSpan)
            {
                using (Logger.LogBlock(FunctionId.TextStructureNavigator_GetSpanOfPreviousSibling, CancellationToken.None))
                {
                    var span = default(SnapshotSpan);
                    var result = _uiThreadOperationExecutor.Execute(
                        title: EditorFeaturesResources.Text_Navigation,
                        defaultDescription: EditorFeaturesResources.Finding_span_of_previous_sibling,
                        allowCancellation: true,
                        showProgress: false,
                        action: context =>
                    {
                        span = GetSpanOfPreviousSiblingWorker(activeSpan, context.UserCancellationToken);
                    });

                    return result == UIThreadOperationStatus.Completed ? span : activeSpan;
                }
            }

            private static SnapshotSpan GetSpanOfPreviousSiblingWorker(SnapshotSpan activeSpan, CancellationToken cancellationToken)
            {
                // Find node that covers the entire span.
                var node = FindLeafNode(activeSpan, cancellationToken);
                if (node != null)
                {
                    // Get ancestor with a wider span.
                    var parent = GetEnclosingNode(node.Value);
                    if (parent != null)
                    {
                        // Find node immediately before the current in the children collection.
                        var nodeOrToken = parent.Value
                            .ChildNodesAndTokens()
                            .Reverse()
                            .SkipWhile(child => child != node)
                            .Skip(1)
                            .FirstOrNull();

                        if (nodeOrToken.HasValue)
                        {
                            node = nodeOrToken.Value;
                        }
                        else
                        {
                            // If this is the first node, move to the parent so that the user can continue 
                            // navigation at the higher level.
                            node = parent.Value;
                        }
                    }
                }

                return node == null ? activeSpan : node.Value.Span.ToSnapshotSpan(activeSpan.Snapshot);
            }

            private static Document GetDocument(SnapshotPoint point)
            {
                var textLength = point.Snapshot.Length;
                if (textLength == 0)
                {
                    return null;
                }

                return point.Snapshot.GetOpenDocumentInCurrentContextWithChanges();
            }

            /// <summary>
            /// Finds deepest node that covers given <see cref="SnapshotSpan"/>.
            /// </summary>
            private static SyntaxNodeOrToken? FindLeafNode(SnapshotSpan span, CancellationToken cancellationToken)
            {
                if (!TryFindLeafToken(span.Start, out var token, cancellationToken))
                {
                    return null;
                }

                SyntaxNodeOrToken? node = token;
                while (node != null && (span.End.Position > node.Value.Span.End))
                {
                    node = GetEnclosingNode(node.Value);
                }

                return node;
            }

            /// <summary>
            /// Given position in a text buffer returns the leaf syntax node it belongs to.
            /// </summary>
            private static bool TryFindLeafToken(SnapshotPoint point, out SyntaxToken token, CancellationToken cancellationToken)
            {
                var syntaxTree = GetDocument(point).GetSyntaxTreeSynchronously(cancellationToken);
                if (syntaxTree != null)
                {
                    token = syntaxTree.GetRoot(cancellationToken).FindToken(point, true);
                    return true;
                }

                token = default;
                return false;
            }

            /// <summary>
            /// Returns first ancestor of the node which has a span wider than node's span.
            /// If none exist, returns the last available ancestor.
            /// </summary>
            private static SyntaxNodeOrToken SkipSameSpanParents(SyntaxNodeOrToken node)
            {
                while (node.Parent != null && node.Parent.Span == node.Span)
                {
                    node = node.Parent;
                }

                return node;
            }

            /// <summary>
            /// Finds node enclosing current from navigation point of view (that is, some immediate ancestors
            /// may be skipped during this process).
            /// </summary>
            private static SyntaxNodeOrToken? GetEnclosingNode(SyntaxNodeOrToken node)
            {
                var parent = SkipSameSpanParents(node).Parent;
                if (parent != null)
                {
                    return parent;
                }
                else
                {
                    return null;
                }
            }
        }
    }
}
