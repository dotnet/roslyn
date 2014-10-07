using Roslyn.Compilers.Common;

namespace Roslyn.Services.Shared.Utilities
{
    internal static class ISemanticModelExtensions
    {
        /// <summary>
        /// Gets semantic information, such as type, symbols, and diagnostics, about the parent of a token.
        /// </summary>
        /// <param name="semanticModel">The SemanticModel object to get semantic information
        /// from.</param>
        /// <param name="token">The token to get semantic information from. This must be part of the syntax tree
        /// associated with the binding.</param>
        public static CommonSymbolInfo GetSymbolInfo(this ISemanticModel semanticModel, CommonSyntaxToken token)
        {
            return semanticModel.GetSymbolInfo(token.Parent);
        }

        /// <summary>
        /// Gets semantic information, such as type, symbols, and diagnostics, about a node or
        /// token.
        /// </summary>
        /// <param name="semanticModel">The SemanticModel object to get semantic information
        /// from.</param>
        /// <param name="nodeOrToken">The node or token to inquire about. If this a token, the parent of the token is
        /// queried.</param>
        public static CommonSymbolInfo GetSymbolInfo(this ISemanticModel semanticModel, CommonSyntaxNodeOrToken nodeOrToken)
        {
            return nodeOrToken.IsToken
                ? semanticModel.GetSymbolInfo(nodeOrToken.AsToken())
                : semanticModel.GetSymbolInfo(nodeOrToken.AsNode());
        }
    }
}