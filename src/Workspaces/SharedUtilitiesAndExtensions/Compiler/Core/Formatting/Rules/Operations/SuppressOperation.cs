// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Formatting.Rules
{
    /// <summary>
    /// suppress formatting operations within the given text span
    /// </summary>
    internal readonly struct SuppressOperation
    {
        public readonly TextSpan TextSpan;
        public readonly SuppressOption Option;
        public readonly SyntaxToken StartToken;
        public readonly SyntaxToken EndToken;

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

#if DEBUG
        public override string ToString()
            => $"Suppress {TextSpan} from '{StartToken}' to '{EndToken}' with '{Option}'";
#endif
    }
}
