// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.CodeDom.Compiler;

namespace Microsoft.CodeAnalysis.Diagnostics
{
    internal abstract partial class AnalyzerDriver
    {
        private sealed class GeneratedCodeTokenWalker : SyntaxWalker
        {
            public GeneratedCodeTokenWalker()
                : base(SyntaxWalkerDepth.Token)
            {
            }

            public bool HasGeneratedCodeIdentifier { get; private set; }

            public override void Visit(SyntaxNode node)
            {
                if (HasGeneratedCodeIdentifier)
                    return;

                base.Visit(node);
            }

            protected override void VisitToken(SyntaxToken token)
            {
                HasGeneratedCodeIdentifier |= string.Equals(token.ValueText, "GeneratedCode", StringComparison.Ordinal)
                    || string.Equals(token.ValueText, nameof(GeneratedCodeAttribute), StringComparison.Ordinal);
            }
        }
    }
}
