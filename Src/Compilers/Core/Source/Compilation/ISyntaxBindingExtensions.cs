
namespace Roslyn.Compilers
{
    public static class ISemanticModelExtensions
    {
        /// <summary>
        /// Gets semantic information, such as type, symbols, and diagnostics, about the parent of a token.
        /// </summary>
        /// <param name="semanticModel">The SemanticModel object to get semantic information
        /// from.</param>
        /// <param name="token">The token to get semantic information from. This must be part of the syntax tree
        /// associated with the binding.</param>
        public static ISemanticInfo GetSemanticInfo(this ISemanticModel semanticModel, CommonSyntaxToken token)
        {
            return semanticModel.GetSemanticInfo(token.Parent);
        }

        /// <summary>
        /// Gets the semantic information for a syntax node as seen from the perspective of the
        /// node's parent.
        /// </summary>
        /// <param name="semanticModel">The SemanticModel object to get semantic information
        /// from.</param>
        /// <param name="token">The token to get semantic information from. This must be part of the syntax tree
        /// associated with the binding.</param>
        /// <remarks>
        /// This method allows observing the result of implicit conversions that do not have syntax node's directly
        /// associated with them. For example, consider the following code:
        /// <code>
        /// void f(long x); 
        /// int i = 17; 
        /// f(i);  
        /// </code>
        /// A call to GetSemanticInfo on the syntax node for "i" in "f(i)" would have a Type of "int". A call to
        /// GetSemanticInfoInParent on the same syntax node would have a Type of "long", since there is an implicit
        /// conversion from int to long as part of the function call to f.
        /// </remarks>
        public static ISemanticInfo GetSemanticInfoInParent(this ISemanticModel semanticModel, CommonSyntaxToken token)
        {
            return semanticModel.GetSemanticInfoInParent(token.Parent);
        }

        /// <summary>
        /// Gets semantic information, such as type, symbols, and diagnostics, about a node or
        /// token.
        /// </summary>
        /// <param name="semanticModel">The SemanticModel object to get semantic information
        /// from.</param>
        /// <param name="nodeOrToken">The node or token to inquire about. If this a token, the parent of the token is
        /// queried.</param>
        public static ISemanticInfo GetSemanticInfo(this ISemanticModel semanticModel, CommonSyntaxNodeOrToken nodeOrToken)
        {
            return nodeOrToken.IsToken
                ? semanticModel.GetSemanticInfo(nodeOrToken.AsToken())
                : semanticModel.GetSemanticInfo(nodeOrToken.AsNode());
        }

        /// <summary>
        /// Gets semantic information, such as type, symbols, and diagnostics, about a node or
        /// token.
        /// </summary>
        /// <param name="semanticModel">The SemanticModel object to get semantic information
        /// from.</param>
        /// <param name="nodeOrToken">The node or token to inquire about. If this a token, the parent of the token is
        /// queried.</param>
        public static ISemanticInfo GetSemanticInfoInParent(this ISemanticModel semanticModel, CommonSyntaxNodeOrToken nodeOrToken)
        {
            return nodeOrToken.IsToken
                ? semanticModel.GetSemanticInfoInParent(nodeOrToken.AsToken())
                : semanticModel.GetSemanticInfoInParent(nodeOrToken.AsNode());
        }
    }
}