namespace Microsoft.CodeAnalysis.InlineMethod
{
    internal partial class AbstractInlineMethodRefactoringProvider
    {
        internal interface IInlineChange
        {
        }

        /// <summary>
        ///
        /// </summary>
        protected readonly struct ReplaceVariableChange : IInlineChange
        {
            public SyntaxNode ReplacementLiteralExpression { get; }
            public ISymbol Symbol { get; }

            public ReplaceVariableChange(SyntaxNode replacementLiteralExpression, ISymbol symbol)
            {
                ReplacementLiteralExpression = replacementLiteralExpression;
                Symbol = symbol;
            }
        }

        protected readonly struct RenameVariableChange : IInlineChange
        {
            public string NewName { get; }
            public ISymbol Symbol { get; }

            public RenameVariableChange(string newName, ISymbol symbol)
            {
                NewName = newName;
                Symbol = symbol;
            }
        }

        /// <summary>
        ///
        /// </summary>
        protected readonly struct ExtractDeclarationChange : IInlineChange
        {
            public SyntaxNode DeclarationStatement { get; }

            public ExtractDeclarationChange(SyntaxNode declarationStatement)
            {
                DeclarationStatement = declarationStatement;
            }
        }
    }
}
