// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Formatting
{
    internal abstract partial class TreeData
    {
        private class Debug : NodeAndText
        {
            private readonly TreeData _debugNodeData;

            public Debug(SyntaxNode root, SourceText text)
                : base(root, text)
            {
                _debugNodeData = new Node(root);
            }

            public override string GetTextBetween(SyntaxToken token1, SyntaxToken token2)
            {
                var text = base.GetTextBetween(token1, token2);
                Contract.ThrowIfFalse(text == _debugNodeData.GetTextBetween(token1, token2));

                return text;
            }
        }
    }
}
