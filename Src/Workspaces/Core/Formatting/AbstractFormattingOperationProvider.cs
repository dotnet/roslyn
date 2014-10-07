using System;
using System.Collections.Generic;
using Roslyn.Compilers;
using Roslyn.Compilers.Common;

namespace Roslyn.Services.Formatting
{
    /// <summary>
    /// base IFormattingOperationProvider implementation that user can subclass and override to
    /// provide its own functionality
    /// </summary>
    internal abstract class AbstractFormattingOperationProvider : IFormattingOperationProvider
    {
        private readonly IFormattingOperationProvider formattingRule;

        protected AbstractFormattingOperationProvider(IFormattingOperationProvider formattingRule)
        {
            if (formattingRule == null)
            {
                throw new ArgumentNullException("formattingRule");
            }

            this.formattingRule = formattingRule;
        }

        public virtual void AddSuppressOperations(List<ISuppressOperation> list, CommonSyntaxNode node)
        {
            this.formattingRule.AddSuppressOperations(list, node);
        }

        public virtual void AddAnchorIndentationOperations(List<IAnchorIndentationOperation> list, CommonSyntaxNode node)
        {
            this.formattingRule.AddAnchorIndentationOperations(list, node);
        }

        public virtual void AddIndentBlockOperations(List<IIndentBlockOperation> list, CommonSyntaxNode node)
        {
            this.formattingRule.AddIndentBlockOperations(list, node);
        }

        public virtual void AddAlignTokensOperations(List<IAlignTokensOperation> list, CommonSyntaxNode node)
        {
            this.formattingRule.AddAlignTokensOperations(list, node);
        }

        public virtual IAdjustNewLinesOperation GetAdjustNewLinesOperation(CommonSyntaxToken previousToken, CommonSyntaxToken currentToken)
        {
            return this.formattingRule.GetAdjustNewLinesOperation(previousToken, currentToken);
        }

        public virtual IAdjustSpacesOperation GetAdjustSpacesOperation(CommonSyntaxToken previousToken, CommonSyntaxToken currentToken)
        {
            return this.formattingRule.GetAdjustSpacesOperation(previousToken, currentToken);
        }
    }
}