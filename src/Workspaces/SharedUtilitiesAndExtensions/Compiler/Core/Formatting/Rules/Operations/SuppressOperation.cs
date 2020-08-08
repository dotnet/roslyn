// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable

using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Formatting.Rules
{
    /// <summary>
    /// suppress formatting operations within the given text span
    /// </summary>
    internal sealed class SuppressOperation
    {
        internal SuppressOperation(SyntaxToken startToken, SyntaxToken endToken, TextSpan textSpan, SuppressOption option)
        {
            Contract.ThrowIfTrue(textSpan.Start < 0 || textSpan.Length < 0);
            Contract.ThrowIfTrue(startToken.RawKind == 0);
            Contract.ThrowIfTrue(endToken.RawKind == 0);

            this.TextSpan = textSpan;
            this.Option = option;

            this.StartToken = startToken;
            this.EndToken = endToken;
        }

        public TextSpan TextSpan { get; }
        public SuppressOption Option { get; }

        public SyntaxToken StartToken { get; }
        public SyntaxToken EndToken { get; }

#if DEBUG
        public override string ToString()
            => $"Suppress {TextSpan} from '{StartToken}' to '{EndToken}' with '{Option}'";
#endif
    }
}
