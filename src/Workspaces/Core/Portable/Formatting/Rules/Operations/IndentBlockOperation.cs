﻿// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Formatting.Rules
{
    /// <summary>
    /// set indentation level for the given text span. it can be relative, absolute or dependent to other tokens
    /// </summary>
    internal sealed class IndentBlockOperation
    {
        internal IndentBlockOperation(SyntaxToken startToken, SyntaxToken endToken, TextSpan textSpan, int indentationDelta, IndentBlockOption option)
        {
            Contract.ThrowIfFalse(option.IsMaskOn(IndentBlockOption.PositionMask));

            Contract.ThrowIfTrue(textSpan.Start < 0 || textSpan.Length < 0);
            Contract.ThrowIfTrue(startToken.RawKind == 0);
            Contract.ThrowIfTrue(endToken.RawKind == 0);

            this.BaseToken = default;
            this.TextSpan = textSpan;

            this.Option = option;
            this.StartToken = startToken;
            this.EndToken = endToken;

            this.IsRelativeIndentation = false;
            this.IndentationDeltaOrPosition = indentationDelta;
        }

        internal IndentBlockOperation(SyntaxToken baseToken, SyntaxToken startToken, SyntaxToken endToken, TextSpan textSpan, int indentationDelta, IndentBlockOption option)
        {
            Contract.ThrowIfFalse(option.IsMaskOn(IndentBlockOption.PositionMask));

            Contract.ThrowIfFalse(option.IsMaskOn(IndentBlockOption.RelativePositionMask));
            Contract.ThrowIfFalse(baseToken.Span.End <= textSpan.Start);

            Contract.ThrowIfTrue(textSpan.Start < 0 || textSpan.Length < 0);
            Contract.ThrowIfTrue(startToken.RawKind == 0);
            Contract.ThrowIfTrue(endToken.RawKind == 0);

            this.BaseToken = baseToken;
            this.TextSpan = textSpan;

            this.Option = option;
            this.StartToken = startToken;
            this.EndToken = endToken;

            this.IsRelativeIndentation = true;
            this.IndentationDeltaOrPosition = indentationDelta;
        }

        public SyntaxToken BaseToken { get; }
        public TextSpan TextSpan { get; }

        public IndentBlockOption Option { get; }

        public SyntaxToken StartToken { get; }
        public SyntaxToken EndToken { get; }

        public bool IsRelativeIndentation { get; }
        public int IndentationDeltaOrPosition { get; }
    }
}
