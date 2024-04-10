// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

namespace Microsoft.CodeAnalysis.CSharp.Syntax.InternalSyntax
{
    internal partial class SyntaxParser
    {
        protected readonly struct ResetPoint
        {
            internal readonly int ResetCount;
            internal readonly LexerMode Mode;
            internal readonly int Position;
            internal readonly GreenNode PrevTokenTrailingTrivia;

            internal ResetPoint(int resetCount, LexerMode mode, int position, GreenNode prevTokenTrailingTrivia)
            {
                this.ResetCount = resetCount;
                this.Mode = mode;
                this.Position = position;
                this.PrevTokenTrailingTrivia = prevTokenTrailingTrivia;
            }
        }
    }
}
