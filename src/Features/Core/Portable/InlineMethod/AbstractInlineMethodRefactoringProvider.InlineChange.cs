// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Microsoft.CodeAnalysis.InlineMethod
{
    internal partial class AbstractInlineMethodRefactoringProvider
    {
        internal interface IInlineChange
        {
        }

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

        protected readonly struct IdentifierRenameVariableChange : IInlineChange
        {
            public ISymbol Symbol { get; }
            public SyntaxNode IdentifierSyntaxNode { get; }

            public IdentifierRenameVariableChange(ISymbol symbol, SyntaxNode identifierSyntaxNode)
            {
                Symbol = symbol;
                IdentifierSyntaxNode = identifierSyntaxNode;
            }
        }

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
