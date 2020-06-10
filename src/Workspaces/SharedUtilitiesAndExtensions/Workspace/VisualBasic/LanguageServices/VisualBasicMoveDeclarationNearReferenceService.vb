' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Composition
Imports System.Threading
Imports Microsoft.CodeAnalysis.Host.Mef
Imports Microsoft.CodeAnalysis.MoveDeclarationNearReference
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.VisualBasic.MoveDeclarationNearReference
    <ExportLanguageService(GetType(IMoveDeclarationNearReferenceService), LanguageNames.VisualBasic), [Shared]>
    Friend Class VisualBasicMoveDeclarationNearReferenceService
        Inherits AbstractMoveDeclarationNearReferenceService(Of
            VisualBasicMoveDeclarationNearReferenceService,
            StatementSyntax,
            LocalDeclarationStatementSyntax,
            VariableDeclaratorSyntax)

        <ImportingConstructor>
        <Obsolete(MefConstruction.ImportingConstructorMessage, True)>
        Public Sub New()
        End Sub

        Protected Overrides Function IsMeaningfulBlock(node As SyntaxNode) As Boolean
            Return TypeOf node Is LambdaExpressionSyntax OrElse
                   TypeOf node Is ForOrForEachBlockSyntax OrElse
                   TypeOf node Is WhileStatementSyntax OrElse
                   TypeOf node Is DoStatementSyntax OrElse
                   TypeOf node Is LoopStatementSyntax
        End Function

        Protected Overrides Function GetVariableDeclaratorSymbolNode(variableDeclarator As VariableDeclaratorSyntax) As SyntaxNode
            Return variableDeclarator.Names(0)
        End Function

        Protected Overrides Function IsValidVariableDeclarator(variableDeclarator As VariableDeclaratorSyntax) As Boolean
            Return variableDeclarator.Names.Count = 1
        End Function

        Protected Overrides Function GetIdentifierOfVariableDeclarator(variableDeclarator As VariableDeclaratorSyntax) As SyntaxToken
            Return variableDeclarator.Names(0).Identifier
        End Function

        Protected Overrides Function TypesAreCompatibleAsync(document As Document, localSymbol As ILocalSymbol, declarationStatement As LocalDeclarationStatementSyntax, right As SyntaxNode, cancellationToken As CancellationToken) As Task(Of Boolean)
            Return SpecializedTasks.True
        End Function

        Protected Overrides Function CanMoveToBlock(localSymbol As ILocalSymbol, currentBlock As SyntaxNode, destinationBlock As SyntaxNode) As Boolean
            Return True
        End Function
    End Class
End Namespace
