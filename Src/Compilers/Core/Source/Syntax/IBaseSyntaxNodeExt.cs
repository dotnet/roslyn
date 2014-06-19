using System.Collections.Generic;

namespace Microsoft.CodeAnalysis
{
#if false
    internal interface IBaseSyntaxNodeExt : IBaseSyntaxNode
    {
        bool IsList { get; }
        bool IsStructuredTrivia { get; }
        bool IsDirective { get; }

        bool ContainsAnnotations { get; }
        bool HasStructuredTrivia { get; }

        SyntaxNode GetStructure(SyntaxTrivia trivia);
        int SlotCount { get; }
        IBaseSyntaxNodeExt GetSlot(int index);
        int GetSlotOffset(int index);
        int LeadingWidth { get; }
        int TrailingWidth { get; }

        SyntaxAnnotation[] GetAnnotations();
        IBaseSyntaxNodeExt WithAdditionalAnnotations(params SyntaxAnnotation[] syntaxAnnotations);
        IBaseSyntaxNodeExt WithoutAnnotations(params SyntaxAnnotation[] annotation);

        /// <summary>
        /// If this is a red node, this returns it's underlying green node.  If this is a green node
        /// already, then this is returned.
        /// </summary>
        IBaseSyntaxNodeExt GetUnderlyingGreenNode();

        /// <summary>
        /// The width of the node in characters, not including leading and trailing trivia
        /// </summary>
        int Width { get; }

        /// <summary>
        /// The complete width of the node in characters including leading and trailing trivia
        /// </summary>
        int FullWidth { get; }

        /// <summary>
        /// Writes the text of this node to the specified TextWriter, optionally including leading and trailing trivia.
        /// </summary>
        void WriteTo(System.IO.TextWriter writer, bool leading, bool trailing);

        IBaseSyntaxNodeExt CreateList(IEnumerable<IBaseSyntaxNodeExt> nodes);
    }
#endif
}