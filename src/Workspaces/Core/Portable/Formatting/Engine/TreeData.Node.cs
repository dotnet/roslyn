// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Shared.Utilities;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Formatting
{
    internal abstract partial class TreeData
    {
        private class Node : TreeData
        {
            public Node(SyntaxNode root)
                : base(root)
            {
                Contract.ThrowIfFalse(root.GetFirstToken(includeZeroWidth: true).RawKind != 0);
            }

            public override int GetOriginalColumn(int tabSize, SyntaxToken token)
            {
                Contract.ThrowIfTrue(token.RawKind == 0);

                // first find one that has new line text
                var startToken = GetTokenWithLineBreaks(token);

                // get last line text from text between them
                var lineText = GetTextBetween(startToken, token).GetLastLineText();

                return lineText.GetColumnFromLineOffset(lineText.Length, tabSize);
            }

            private SyntaxToken GetTokenWithLineBreaks(SyntaxToken token)
            {
                var currentToken = token.GetPreviousToken(includeZeroWidth: true);

                while (currentToken.RawKind != 0)
                {
                    if (currentToken.ToFullString().IndexOf('\n') >= 0)
                    {
                        return currentToken;
                    }

                    currentToken = currentToken.GetPreviousToken(includeZeroWidth: true);
                }

                return default;
            }

            public override string GetTextBetween(SyntaxToken token1, SyntaxToken token2)
            {
                var builder = StringBuilderPool.Allocate();

                CommonFormattingHelpers.AppendTextBetween(token1, token2, builder);

                return StringBuilderPool.ReturnAndFree(builder);
            }
        }
    }
}
