using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Roslyn.Compilers.CSharp
{
    // TODO: SyntaxReferenceLocation should be internal, but it's currently
    // public just for Neal's prototype to work.

    /// <summary>
    /// Represents a location in source code obtained via a SyntaxReference.
    /// </summary>
    public class SyntaxReferenceLocation : SourceLocation
    {
        private SyntaxReference reference;

        public SyntaxReferenceLocation(SyntaxReference reference)
        {
            this.reference = reference;
        }

        public SyntaxReferenceLocation(SyntaxNode node, SyntaxTree tree)
            : this(tree.GetReference(node))
        {
        }

        public override SyntaxTree SyntaxTree
        {
            get { return reference.Tree; }
        }

        public override TextSpan Span
        {
            get { return reference.Span; }
        }

        public override string ToString()
        {
            // TODO: this is awful. We don't have a filename anywhere, and Span.ToString() is not line/column based.
            return String.Format("SyntaxTree Span {0}", Span);
        }
    }
}