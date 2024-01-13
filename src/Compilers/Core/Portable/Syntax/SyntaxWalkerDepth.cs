// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Microsoft.CodeAnalysis
{
#pragma warning disable RS0030 // Do not use banned APIs: SyntaxWalker
    /// <summary>
    /// Syntax the <see cref="SyntaxWalker"/> should descend into.
    /// </summary>
#pragma warning restore
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
