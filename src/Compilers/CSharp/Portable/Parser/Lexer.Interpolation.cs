// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.CSharp.Syntax.InternalSyntax
{
    internal partial class Lexer
    {
        internal readonly struct Interpolation
        {
            public readonly Range OpenBraceRange;

            /// <summary>
            /// Range of the format colon in the interpolation.  Empty if there is no colon.
            /// </summary>
            public readonly Range ColonRange;

            /// <summary>
            /// Range of the close brace.  Empty if there was no close brace (an error condition).
            /// </summary>
            public readonly Range CloseBraceRange;

            public bool HasColon => ColonRange.Start.Value != ColonRange.End.Value;

            public Interpolation(Range openBraceRange, Range colonRange, Range closeBraceRange)
            {
                OpenBraceRange = openBraceRange;
                ColonRange = colonRange;
                CloseBraceRange = closeBraceRange;
            }
        }
    }
}
