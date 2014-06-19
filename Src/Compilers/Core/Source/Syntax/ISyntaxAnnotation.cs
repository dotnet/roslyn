

#if false
namespace Roslyn.Compilers
{
    /// <summary>
    /// Annotates syntax with additional information. As syntax elements are immutable they can not
    /// actually be annotated with information.  However, new syntax elements can be created from an
    /// existing syntax element and an annotation to return an entirely new syntax element with that
    /// associated annotation.  
    /// </summary>
    public interface ISyntaxAnnotation
    {
        /// <summary>
        /// Add this annotation to the given syntax node, creating a new 
        /// syntax node of the same type with the annotation on it.
        /// </summary>
        T AddAnnotationTo<T>(T node) where T : CommonSyntaxNode;

        /// <summary>
        /// Add this annotation to the given syntax token, creating a new 
        /// syntax token of the same type with the annotation on it.
        /// </summary>
        CommonSyntaxToken AddAnnotationTo(CommonSyntaxToken token);

        /// <summary>
        /// Add this annotation to the given syntax trivia, creating a new 
        /// syntax trivia of the same type with the annotation on it.
        /// </summary>
        CommonSyntaxTrivia AddAnnotationTo(CommonSyntaxTrivia trivia);

        /// <summary>
        /// Finds all nodes with this annotation attached, 
        /// that are on or under node.
        /// </summary>
        IEnumerable<CommonSyntaxNodeOrToken> FindAnnotatedNodesOrTokens(CommonSyntaxNode root);

        /// <summary>
        /// Finds all trivia with this annotation attached, 
        /// that are on or under node.
        /// </summary>
        IEnumerable<CommonSyntaxTrivia> FindAnnotatedTrivia(CommonSyntaxNode root);
    }
}
#endif