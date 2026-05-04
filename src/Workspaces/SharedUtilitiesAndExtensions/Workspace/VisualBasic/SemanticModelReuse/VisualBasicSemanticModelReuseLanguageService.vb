' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Composition
Imports Microsoft.CodeAnalysis.Host.Mef
Imports Microsoft.CodeAnalysis.LanguageService
Imports Microsoft.CodeAnalysis.SemanticModelReuse
Imports Microsoft.CodeAnalysis.VisualBasic.LanguageService
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.VisualBasic.SemanticModelReuse
    <ExportLanguageService(GetType(ISemanticModelReuseLanguageService), LanguageNames.VisualBasic), [Shared]>
    Friend Class VisualBasicSemanticModelReuseLanguageService
        Inherits AbstractSemanticModelReuseLanguageService(Of
            DeclarationStatementSyntax,
            DeclarationStatementSyntax,
            AccessorBlockSyntax)

        <ImportingConstructor>
        <Obsolete(MefConstruction.ImportingConstructorMessage, True)>
        Public Sub New()
        End Sub

        Protected Overrides ReadOnly Property SyntaxFacts As ISyntaxFacts = VisualBasicSyntaxFacts.Instance

        Protected Overrides Function GetBasePropertyDeclaration(accessor As AccessorBlockSyntax) As DeclarationStatementSyntax
            Dim container = accessor.Parent
            Contract.ThrowIfFalse(TypeOf container Is PropertyBlockSyntax OrElse
                                  TypeOf container Is EventBlockSyntax)
            Return DirectCast(container, DeclarationStatementSyntax)
        End Function

        Protected Overrides Function GetAccessors(member As DeclarationStatementSyntax) As SyntaxList(Of AccessorBlockSyntax)
            Contract.ThrowIfFalse(TypeOf member Is PropertyBlockSyntax OrElse
                                  TypeOf member Is EventBlockSyntax)
            Return GetAccessorList(member)
        End Function

        Public Overrides Function TryGetContainingMethodBodyForSpeculation(node As SyntaxNode) As SyntaxNode
            Dim previous = node
            While node IsNot Nothing
                Dim methodBlock = TryCast(node, MethodBlockBaseSyntax)
                If methodBlock IsNot Nothing Then
                    Return If(methodBlock.Statements.Contains(TryCast(previous, StatementSyntax)), methodBlock, Nothing)
                End If

                previous = node
                node = node.Parent
            End While

            Return Nothing
        End Function

        Protected Overrides Function TryGetSpeculativeSemanticModelWorker(previousSemanticModel As SemanticModel, previousBodyNode As SyntaxNode, currentBodyNode As SyntaxNode) As SemanticModel

            Dim previousMethodBlockBaseNode = TryCast(previousBodyNode, MethodBlockBaseSyntax)
            If previousMethodBlockBaseNode Is Nothing Then
                Debug.Fail("Could not map current body to previous body, despite no top level changes")
                Return Nothing
            End If

            Dim speculativeModel As SemanticModel = Nothing
            If previousSemanticModel.TryGetSpeculativeSemanticModelForMethodBody(previousMethodBlockBaseNode.BlockStatement.FullSpan.End, DirectCast(currentBodyNode, MethodBlockBaseSyntax), speculativeModel) Then
                Return speculativeModel
            End If

            Return Nothing
        End Function
    End Class
End Namespace
