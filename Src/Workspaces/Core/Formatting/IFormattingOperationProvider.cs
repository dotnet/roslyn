using System.Collections.Generic;
using Roslyn.Compilers;
using Roslyn.Compilers.Common;

namespace Roslyn.Services.Formatting
{
    /// <summary>
    /// Provides operations that the formatting engine will use to format a tree.
    /// </summary>
    /// <remarks>All methods defined in this interface can be called concurrently. Must be
    /// thread-safe.</remarks>
    public interface IFormattingOperationProvider
    {
        /// <summary>
        /// Returns SuppressWrappingIfOnSingleLineOperations under a node.
        /// </summary>
        void AddSuppressOperations(List<ISuppressOperation> list, CommonSyntaxNode node);

        /// <summary>
        /// returns AnchorIndentationOperations under a node
        /// </summary>
        void AddAnchorIndentationOperations(List<IAnchorIndentationOperation> list, CommonSyntaxNode node);

        /// <summary>
        /// returns IndentBlockOperations under a node
        /// </summary>
        void AddIndentBlockOperations(List<IIndentBlockOperation> list, CommonSyntaxNode node);

        /// <summary>
        /// returns AlignTokensOperations under a node
        /// </summary>
        void AddAlignTokensOperations(List<IAlignTokensOperation> list, CommonSyntaxNode node);

        /// <summary>
        /// returns AdjustNewLinesOperation between two tokens
        /// </summary>
        IAdjustNewLinesOperation GetAdjustNewLinesOperation(CommonSyntaxToken previousToken, CommonSyntaxToken currentToken);

        /// <summary>
        /// returns AdjustSpacesOperation between two tokens
        /// </summary>
        IAdjustSpacesOperation GetAdjustSpacesOperation(CommonSyntaxToken previousToken, CommonSyntaxToken currentToken);
    }
}