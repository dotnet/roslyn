// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Formatting;

internal abstract partial class TreeData
{
    private class Debug(SyntaxNode root, SourceText text) : NodeAndText(root, text)
    {
        private readonly TreeData _debugNodeData = new Node(root);

        public override string GetTextBetween(SyntaxToken token1, SyntaxToken token2)
        {
            var text = base.GetTextBetween(token1, token2);
            Contract.ThrowIfFalse(text == _debugNodeData.GetTextBetween(token1, token2));

            return text;
        }
    }
}
