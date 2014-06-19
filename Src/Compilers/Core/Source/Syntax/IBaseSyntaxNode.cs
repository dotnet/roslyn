using System;
using System.Collections;
using System.Collections.Generic;

namespace Microsoft.CodeAnalysis
{
#if false
    internal interface IBaseSyntaxNode
    {
        /// <summary>
        /// A integer representing the language specific kind of node
        /// </summary>
        int Kind { get; }
        string KindText { get; }
        string Language { get; }

        bool IsMissing { get; }

        bool ContainsDirectives { get; }
        bool ContainsDiagnostics { get; }

        /// <summary>
        /// Determines if this node has annotations of the specified type. 
        /// The type must be a strict sub type of SyntaxAnnotation.
        /// </summary>
        bool HasAnnotations(Type annotationType);

        /// <summary>
        /// Determines if this node has the specific annotation.
        /// </summary>
        bool HasAnnotation(SyntaxAnnotation annotation);

        /// <summary>
        /// Gets all annotations of the specified type attached to this node.
        /// The type must be a strict sub type of SyntaxAnnotation.
        /// </summary>
        IEnumerable<SyntaxAnnotation> GetAnnotations(Type annotationType);

        /// <summary>
        /// The children nodes.
        /// </summary>
        ChildSyntaxList ChildNodesAndTokens();

        /// <summary>
        /// Returns the string representation of this node, not including its leading and trailing trivia.
        /// </summary>
        /// <returns>The string representation of this node, not including its leading and trailing trivia.</returns>
        string ToString();

        /// <summary>
        /// Returns full string representation of this node including its leading and trailing trivia.
        /// </summary>
        /// <returns>The full string representation of this node including its leading and trailing trivia.</returns>
        string ToFullString();

        /// <summary>
        /// Writes the full text of this node to the specified TextWriter
        /// </summary>
        void WriteTo(System.IO.TextWriter writer);

        /// <summary>
        /// Determines if the two nodes are equivalent to each other.
        /// </summary>
        bool EquivalentTo(IBaseSyntaxNode node);
    }
#endif
}
