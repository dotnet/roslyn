// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

namespace Microsoft.CodeAnalysis.CSharp.Syntax.InternalSyntax
{
    internal partial class SyntaxParser
    {
        protected struct ResetPoint
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