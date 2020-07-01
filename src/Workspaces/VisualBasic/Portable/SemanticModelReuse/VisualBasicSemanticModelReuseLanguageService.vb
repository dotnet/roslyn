' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Composition
Imports System.Threading
Imports Microsoft.CodeAnalysis.Host.Mef
Imports Microsoft.CodeAnalysis.SemanticModelReuse
Imports Microsoft.CodeAnalysis.VisualBasic.LanguageServices
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.VisualBasic.SemanticModelReuse
    <ExportLanguageService(GetType(ISemanticModelReuseLanguageService), LanguageNames.VisualBasic), [Shared]>
    Friend Class VisualBasicSemanticModelReuseLanguageService
        Implements ISemanticModelReuseLanguageService

        <ImportingConstructor>
        <Obsolete(MefConstruction.ImportingConstructorMessage, True)>
        Public Sub New()
        End Sub

        Public Function TryGetContainingMethodBodyForSpeculation(node As SyntaxNode) As SyntaxNode Implements ISemanticModelReuseLanguageService.TryGetContainingMethodBodyForSpeculation
            While node IsNot Nothing
                Dim methodBlock = TryCast(node, MethodBlockBaseSyntax)
                If methodBlock IsNot Nothing Then
                    Return If(methodBlock.Statements.IsEmpty AndAlso methodBlock.EndBlockStatement.IsMissing, Nothing, methodBlock)
                End If

                node = node.Parent
            End While

            Return Nothing
        End Function

        Public Async Function TryGetSpeculativeSemanticModelAsync(
            previousSemanticModel As SemanticModel, currentBodyNode As SyntaxNode, cancellationToken As CancellationToken) As Task(Of SemanticModel) Implements ISemanticModelReuseLanguageService.TryGetSpeculativeSemanticModelAsync

            Dim previousRoot = Await previousSemanticModel.SyntaxTree.GetRootAsync(cancellationToken).ConfigureAwait(False)
            Dim currentRoot = Await currentBodyNode.SyntaxTree.GetRootAsync(cancellationToken).ConfigureAwait(False)

            Dim previousBodyNode = GetPreviousBodyNode(previousRoot, currentRoot, currentBodyNode)
            If previousBodyNode Is Nothing Then
                Debug.Fail("Could not map current body to previous body, despite no top level changes")
                Return Nothing
            End If

            Dim speculativeModel As SemanticModel = Nothing
            If previousSemanticModel.TryGetSpeculativeSemanticModelForMethodBody(previousBodyNode.BlockStatement.FullSpan.End, previousBodyNode, speculativeModel) Then
                Return speculativeModel
            End If

            Return Nothing
        End Function

        Private Shared Function GetPreviousBodyNode(previousRoot As SyntaxNode, currentRoot As SyntaxNode, currentBodyNode As SyntaxNode) As MethodBlockBaseSyntax
            Dim currentMembers = VisualBasicSyntaxFacts.Instance.GetMethodLevelMembers(currentRoot)
            Dim index = currentMembers.IndexOf(currentBodyNode)
            If index < 0 Then
                Debug.Fail("Unhandled member type in GetPreviousBodyNode")
                Return Nothing
            End If

            Dim previousMembers = VisualBasicSyntaxFacts.Instance.GetMethodLevelMembers(previousRoot)
            If currentMembers.Count <> previousMembers.Count Then
                Debug.Fail("Member count shouldn't have changed as there were no top level edits.")
                Return Nothing
            End If

            Return TryCast(previousMembers(index), MethodBlockBaseSyntax)
        End Function
    End Class
End Namespace
