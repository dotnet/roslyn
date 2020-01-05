// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Formatting
{
    internal abstract partial class TreeData
    {
        private class NodeAndText : TreeData
        {
            private readonly SourceText _text;

            public NodeAndText(SyntaxNode root, SourceText text)
                : base(root)
            {
                Contract.ThrowIfNull(text);
                _text = text;
            }

            public override int GetOriginalColumn(int tabSize, SyntaxToken token)
            {
                Contract.ThrowIfTrue(token.RawKind == 0);

                var line = _text.Lines.GetLineFromPosition(token.SpanStart);

                return line.GetColumnFromLineOffset(token.SpanStart - line.Start, tabSize);
            }

            public override string GetTextBetween(SyntaxToken token1, SyntaxToken token2)
            {
                if (token1.RawKind == 0)
                {
                    // get leading trivia text
                    return _text.ToString(TextSpan.FromBounds(token2.FullSpan.Start, token2.SpanStart));
                }

                if (token2.RawKind == 0)
                {
                    // get trailing trivia text
                    return _text.ToString(TextSpan.FromBounds(token1.Span.End, token1.FullSpan.End));
                }

                return _text.ToString(TextSpan.FromBounds(token1.Span.End, token2.SpanStart));
            }
        }
    }
}
