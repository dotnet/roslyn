// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.CodeDom.Compiler;
using System.Threading;

namespace Microsoft.CodeAnalysis.Diagnostics
{
    internal abstract partial class AnalyzerDriver
    {
        private sealed class GeneratedCodeTokenWalker
        {
            private readonly CancellationToken _cancellationToken;

            public GeneratedCodeTokenWalker(CancellationToken cancellationToken)
            {
                _cancellationToken = cancellationToken;
            }

            public bool HasGeneratedCodeIdentifier { get; private set; }

            public void Visit(SyntaxNode node)
            {
                foreach (var token in node.DescendantTokens())
                {
                    _cancellationToken.ThrowIfCancellationRequested();

                    HasGeneratedCodeIdentifier = string.Equals(token.ValueText, "GeneratedCode", StringComparison.Ordinal) ||
                                                 string.Equals(token.ValueText, nameof(GeneratedCodeAttribute), StringComparison.Ordinal);

                    if (HasGeneratedCodeIdentifier)
                        return;
                }
            }
        }
    }
}
