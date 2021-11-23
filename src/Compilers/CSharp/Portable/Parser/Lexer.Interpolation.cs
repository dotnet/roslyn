// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Microsoft.CodeAnalysis.CSharp.Syntax.InternalSyntax
{
    internal partial class Lexer
    {
        internal readonly struct Interpolation
        {
            public readonly int OpenBracePosition;
            public readonly int ColonPosition;
            public readonly int CloseBracePosition;
            public readonly bool CloseBraceMissing;

            public bool ColonMissing => ColonPosition <= 0;
            public bool HasColon => ColonPosition > 0;
            public int LastPosition => CloseBraceMissing ? CloseBracePosition - 1 : CloseBracePosition;
            public int FormatEndPosition => CloseBracePosition - 1;

            public Interpolation(int openBracePosition, int colonPosition, int closeBracePosition, bool closeBraceMissing)
            {
                this.OpenBracePosition = openBracePosition;
                this.ColonPosition = colonPosition;
                this.CloseBracePosition = closeBracePosition;
                this.CloseBraceMissing = closeBraceMissing;
            }
        }
    }
}
