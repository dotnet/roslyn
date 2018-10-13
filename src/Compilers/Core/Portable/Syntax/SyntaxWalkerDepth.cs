// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

namespace Microsoft.CodeAnalysis
{
    /// <summary>
    /// Syntax the <see cref="SyntaxWalker"/> should descend into.
    /// </summary>
    public enum SyntaxWalkerDepth : int
    {
        /// <summary>
        /// descend into only nodes
        /// </summary>
        Node = 0,

        /// <summary>
        /// descend into nodes and tokens
        /// </summary>
        Token = 1,

        /// <summary>
        /// descend into nodes, tokens and trivia
        /// </summary>
        Trivia = 2,

        /// <summary>
        /// descend into everything
        /// </summary>
        StructuredTrivia = 3,
    }
}
