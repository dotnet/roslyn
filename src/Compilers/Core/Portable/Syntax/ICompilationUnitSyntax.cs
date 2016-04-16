// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis
{
    /// <summary>
    /// Interface implemented by any node that is the root 'CompilationUnit' of a <see cref="SyntaxTree"/>.  i.e. 
    /// any node returned by <see cref="SyntaxTree.GetRoot"/> where <see cref="SyntaxTree.HasCompilationUnitRoot"/>
    /// is <code>true</code> will implement this interface.
    ///
    /// This interface provides a common way to both easily find the root of a <see cref="SyntaxTree"/>
    /// given any <see cref="SyntaxNode"/>, as well as a common way for handling the special 
    /// <see cref="EndOfFileToken"/> that is needed to store all final trivia in a <see cref="SourceText"/>
    /// that is not owned by any other <see cref="SyntaxToken"/>.
    /// </summary>
    public interface ICompilationUnitSyntax
    {
        /// <summary>
        /// Represents the end of the source file. This <see cref="SyntaxToken"/> may have
        /// <see cref="SyntaxTrivia"/> (whitespace, comments, directives) attached to it.
        /// </summary>
        SyntaxToken EndOfFileToken { get; }
    }
}