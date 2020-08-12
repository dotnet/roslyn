// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable

using System;
using System.CodeDom.Compiler;
using System.Threading;

namespace Microsoft.CodeAnalysis.Diagnostics
{
    internal abstract partial class AnalyzerDriver
    {
        private sealed class GeneratedCodeTokenWalker : SyntaxWalker
        {
            private readonly CancellationToken _cancellationToken;

            public GeneratedCodeTokenWalker(CancellationToken cancellationToken)
                : base(SyntaxWalkerDepth.Token)
            {
                _cancellationToken = cancellationToken;
            }

            public bool HasGeneratedCodeIdentifier { get; private set; }

            public override void Visit(SyntaxNode node)
            {
                if (HasGeneratedCodeIdentifier)
                    return;

                _cancellationToken.ThrowIfCancellationRequested();
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
