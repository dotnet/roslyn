// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

namespace Microsoft.CodeAnalysis.CSharp
{
    internal sealed class SyntaxTreeAndPosition
    {
        internal readonly SyntaxTree SyntaxTree;
        internal readonly int Position;

        internal SyntaxTreeAndPosition(SyntaxTree syntaxTree, int position)
        {
            SyntaxTree = syntaxTree;
            Position = position;
        }
    }
}
