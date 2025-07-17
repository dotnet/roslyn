// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using System.Composition;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CSharp.Extensions;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.MoveDeclarationNearReference;

namespace Microsoft.CodeAnalysis.CSharp.MoveDeclarationNearReference;

[ExportLanguageService(typeof(IMoveDeclarationNearReferenceService), LanguageNames.CSharp), Shared]
internal sealed partial class CSharpMoveDeclarationNearReferenceService :
    AbstractMoveDeclarationNearReferenceService<
        CSharpMoveDeclarationNearReferenceService,
        StatementSyntax,
        LocalDeclarationStatementSyntax,
        VariableDeclaratorSyntax>
{
    [ImportingConstructor]
    [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
    public CSharpMoveDeclarationNearReferenceService()
    {
    }

    protected override bool IsMeaningfulBlock(SyntaxNode node)
    {
        return node is AnonymousFunctionExpressionSyntax or
               LocalFunctionStatementSyntax or
               CommonForEachStatementSyntax or
               ForStatementSyntax or
               WhileStatementSyntax or
               DoStatementSyntax or
               CheckedStatementSyntax;
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
