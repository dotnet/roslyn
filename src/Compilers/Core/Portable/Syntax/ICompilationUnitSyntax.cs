using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.Syntax
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
        SyntaxList<SyntaxNode> Members { get; }

        SyntaxToken EndOfFileToken { get; }
    }
}
