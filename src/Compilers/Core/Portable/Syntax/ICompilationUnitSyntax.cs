// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis
{
    /// <summary>
    /// Interface implemented by any node that is the root 'CompilationUnit' of a <see cref="SyntaxTree"/>.  i.e. 
    /// any node returned by <see cref="SyntaxTree.GetRoot"/> where <see cref="SyntaxTree.HasCompilationUnitRoot"/>
    /// is <see langword="true"/> will implement this interface.
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
