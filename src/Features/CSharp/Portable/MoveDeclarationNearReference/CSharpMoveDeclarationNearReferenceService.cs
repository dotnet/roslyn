// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Composition;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CSharp.Extensions;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.MoveDeclarationNearReference;

namespace Microsoft.CodeAnalysis.CSharp.MoveDeclarationNearReference
{
    [ExportLanguageService(typeof(IMoveDeclarationNearReferenceService), LanguageNames.CSharp), Shared]
    internal partial class CSharpMoveDeclarationNearReferenceService :
        AbstractMoveDeclarationNearReferenceService<
            CSharpMoveDeclarationNearReferenceService,
            StatementSyntax,
            LocalDeclarationStatementSyntax,
            VariableDeclaratorSyntax>
    {
        [ImportingConstructor]
        public CSharpMoveDeclarationNearReferenceService()
        {
        }

        protected override bool IsMeaningfulBlock(SyntaxNode node)
        {
            return node is AnonymousFunctionExpressionSyntax ||
                   node is LocalFunctionStatementSyntax ||
                   node is CommonForEachStatementSyntax ||
                   node is ForStatementSyntax ||
                   node is WhileStatementSyntax ||
                   node is DoStatementSyntax ||
                   node is CheckedStatementSyntax;
        }

        protected override SyntaxNode GetVariableDeclaratorSymbolNode(VariableDeclaratorSyntax variableDeclarator)
            => variableDeclarator;

        protected override bool IsValidVariableDeclarator(VariableDeclaratorSyntax variableDeclarator)
            => true;

        protected override SyntaxToken GetIdentifierOfVariableDeclarator(VariableDeclaratorSyntax variableDeclarator)
            => variableDeclarator.Identifier;

        protected override async Task<bool> TypesAreCompatibleAsync(
            Document document, ILocalSymbol localSymbol,
            LocalDeclarationStatementSyntax declarationStatement,
            SyntaxNode right, CancellationToken cancellationToken)
        {
            var type = declarationStatement.Declaration.Type;
            if (type.IsVar)
            {
                // Type inference.  Only merge if types match.
                var semanticModel = await document.GetSemanticModelAsync(cancellationToken).ConfigureAwait(false);
                var rightType = semanticModel.GetTypeInfo(right, cancellationToken);
                return Equals(localSymbol.Type, rightType.Type);
            }

            return true;
        }

        protected override bool CanMoveToBlock(ILocalSymbol localSymbol, SyntaxNode currentBlock, SyntaxNode destinationBlock)
            => localSymbol.CanSafelyMoveLocalToBlock(currentBlock, destinationBlock);
    }
}
