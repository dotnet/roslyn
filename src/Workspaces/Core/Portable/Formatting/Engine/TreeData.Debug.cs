// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Diagnostics.CodeAnalysis;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Formatting
{
    internal abstract partial class TreeData
    {
        private class Debug : NodeAndText
        {
            private readonly TreeData debugNodeData;

            public Debug(SyntaxNode root, SourceText text) :
                base(root, text)
            {
                this.debugNodeData = new Node(root);
            }

            public override string GetTextBetween(SyntaxToken token1, SyntaxToken token2)
            {
                var text = base.GetTextBetween(token1, token2);
                Contract.ThrowIfFalse(text == this.debugNodeData.GetTextBetween(token1, token2));

                return text;
            }
        }
    }
}
