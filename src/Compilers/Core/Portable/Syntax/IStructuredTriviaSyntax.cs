// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

namespace Microsoft.CodeAnalysis
{
    /// <summary>
    /// Represents the root node of a structured trivia tree (for example, a preprocessor directive
    /// or a documentation comment). From this root node you can traverse back up to the containing
    /// trivia in the outer tree that contains it.
    /// </summary>
    public interface IStructuredTriviaSyntax
    {
        /// <summary>
        /// Returns the parent trivia syntax for this structured trivia syntax.
        /// </summary>
        /// <returns>The parent trivia syntax for this structured trivia syntax.</returns>
        SyntaxTrivia ParentTrivia { get; }
    }
}
