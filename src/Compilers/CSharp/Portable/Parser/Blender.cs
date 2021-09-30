// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp.Syntax.InternalSyntax
{
    internal readonly partial struct Blender
    {
        private readonly Lexer _lexer;
        private readonly Cursor _oldTreeCursor;
        private readonly ImmutableStack<TextChangeRange> _changes;

        /// <summary>
        /// newPosition represents the position we are in the final SourceText.  As we consume and reuse
        /// nodes from the old tree we will update our position in the new text accordingly.
        /// Likewise, when we must lex tokens out of the new tree we will update as well.
        /// 
        /// NOTE(cyrusn): We do not need an oldPosition because it is redundant given the
        /// oldTreeCursor.  The oldPosition is implicitly defined by the position of the cursor.
        /// </summary>
        private readonly int _newPosition;
        private readonly int _changeDelta;
        private readonly DirectiveStack _newDirectives;
        private readonly DirectiveStack _oldDirectives;
        private readonly LexerMode _newLexerDrivenMode;

        public Blender(Lexer lexer, CSharp.CSharpSyntaxNode oldTree, IEnumerable<TextChangeRange> changes)
        {
            Debug.Assert(lexer != null);
            _lexer = lexer;
            _changes = ImmutableStack.Create<TextChangeRange>();

            if (changes != null)
            {
                // TODO: Consider implementing NormalizedChangeCollection for TextSpan. the real
                // reason why we are collapsing is because we want to extend change ranges and
                // cannot allow them to overlap. This does not seem to be a big deal since multiple
                // changes are infrequent and typically close to each other. However if we have
                // NormalizedChangeCollection for TextSpan we can have both - we can extend ranges
                // and not require collapsing them. NormalizedChangeCollection would also ensure
                // that changes are always normalized.

                // TODO: this is a temporary measure to prevent individual change spans from
                // overlapping after they are widened to effective width (+1 token at the start).
                // once we have normalized collection for TextSpan we will not need to collapse all
                // the change spans.

                var collapsed = TextChangeRange.Collapse(changes);

                // extend the change to its affected range. This will make it easier 
                // to filter out affected nodes since we will be able simply check 
                // if node intersects with a change.
                var affectedRange = ExtendToAffectedRange(oldTree, collapsed);
                _changes = _changes.Push(affectedRange);
            }

            if (oldTree == null)
            {
                // start at lexer current position if no nodes specified
                _oldTreeCursor = new Cursor();
                _newPosition = lexer.TextWindow.Position;
            }
            else
            {
                _oldTreeCursor = Cursor.FromRoot(oldTree).MoveToFirstChild();
                _newPosition = 0;
            }

            _changeDelta = 0;
            _newDirectives = default(DirectiveStack);
            _oldDirectives = default(DirectiveStack);
            _newLexerDrivenMode = 0;
        }

        private Blender(
            Lexer lexer,
            Cursor oldTreeCursor,
            ImmutableStack<TextChangeRange> changes,
            int newPosition,
            int changeDelta,
            DirectiveStack newDirectives,
            DirectiveStack oldDirectives,
            LexerMode newLexerDrivenMode)
        {
            Debug.Assert(lexer != null);
            Debug.Assert(changes != null);
            Debug.Assert(newPosition >= 0);
            _lexer = lexer;
            _oldTreeCursor = oldTreeCursor;
            _changes = changes;
            _newPosition = newPosition;
            _changeDelta = changeDelta;
            _newDirectives = newDirectives;
            _oldDirectives = oldDirectives;
            _newLexerDrivenMode = newLexerDrivenMode & (LexerMode.MaskXmlDocCommentLocation | LexerMode.MaskXmlDocCommentStyle);
        }

        /// <summary>
        /// Affected range of a change is the range within which nodes can be affected by a change
        /// and cannot be reused. Because of lookahead effective range of a change is larger than
        /// the change itself.
        /// </summary>
        private static TextChangeRange ExtendToAffectedRange(
            CSharp.CSharpSyntaxNode oldTree,
            TextChangeRange changeRange)
        {
            // we will increase affected range of the change by the number of lookahead tokens
            // original code in Blender seem to imply the lookahead at the end of a node is 1 token
            // max. TODO: 1 token lookahead seems a bit too optimistic. Increase if needed. 
            const int maxLookahead = 1;

            // check if change is not after the end. TODO: there should be an assert somewhere about
            // changes starting at least at the End of old tree
            var lastCharIndex = oldTree.FullWidth - 1;

            // Move the start of the change range so that it is contained within oldTree.
            var start = Math.Max(Math.Min(changeRange.Span.Start, lastCharIndex), 0);

            // the first iteration aligns us with the change start. subsequent iteration move us to
            // the left by maxLookahead tokens.  We only need to do this as long as we're not at the
            // start of the tree.  Also, the tokens we get back may be zero width.  In that case we
            // need to keep on looking backward.
            for (var i = 0; start > 0 && i <= maxLookahead;)
            {
                var token = oldTree.FindToken(start, findInsideTrivia: false);
                Debug.Assert(token.Kind() != SyntaxKind.None, "how could we not get a real token back?");

                start = Math.Max(0, token.Position - 1);

                // Only increment i if we got a non-zero width token.  Otherwise, we want to just do
                // this again having moved back one space.
                if (token.FullWidth > 0)
                {
                    i++;
                }
            }

            if (IsInsideInterpolation(oldTree, start))
            {
                // If the changed range starts inside an interpolated string, we
                // move the start of the change range to the beginning of the line so that any
                // interpolated string literal in the changed range will be scanned in its entirety.
                var column = oldTree.SyntaxTree.GetLineSpan(new TextSpan(start, 0)).Span.Start.Character;
                start = Math.Max(start - column, 0);
            }

            var finalSpan = TextSpan.FromBounds(start, changeRange.Span.End);
            var finalLength = changeRange.NewLength + (changeRange.Span.Start - start);
            return new TextChangeRange(finalSpan, finalLength);
        }

        private static bool IsInsideInterpolation(CSharp.CSharpSyntaxNode oldTree, int start)
        {
            var token = oldTree.FindToken(start, findInsideTrivia: false);
            for (var parent = token.Parent; // for each parent
                parent != null;
                parent = parent.Parent)
            {
                if (parent.Kind() == SyntaxKind.InterpolatedStringExpression)
                {
                    return true;
                }
            }

            return false;
        }

        public BlendedNode ReadNode(LexerMode mode)
        {
            return ReadNodeOrToken(mode, asToken: false);
        }

        public BlendedNode ReadToken(LexerMode mode)
        {
            return ReadNodeOrToken(mode, asToken: true);
        }

        private BlendedNode ReadNodeOrToken(LexerMode mode, bool asToken)
        {
            var reader = new Reader(this);
            return reader.ReadNodeOrToken(mode, asToken);
        }
    }
}
