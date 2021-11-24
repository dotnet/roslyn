// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.CSharp.Syntax.InternalSyntax
{
    internal partial class Lexer
    {
        internal readonly struct Interpolation
        {
            public readonly TextSpan OpenBraceSpan;

            /// <summary>
            /// Span of the format colon in the interpolation.  Empty if there is no colon.
            /// </summary>
            public readonly TextSpan ColonSpan;

            /// <summary>
            /// Span of the close brace.  Empty if there was no close brace (an error condition).
            /// </summary>
            public readonly TextSpan CloseBraceSpan;

            public bool HasColon => !ColonSpan.IsEmpty;

            public Interpolation(TextSpan openBraceSpan, TextSpan colonSpan, TextSpan closeBraceSpan)
            {
                OpenBraceSpan = openBraceSpan;
                ColonSpan = colonSpan;
                CloseBraceSpan = closeBraceSpan;
            }
        }
    }
}
