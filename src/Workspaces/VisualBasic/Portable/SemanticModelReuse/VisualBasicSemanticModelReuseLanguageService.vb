﻿' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Composition
Imports System.Threading
Imports Microsoft.CodeAnalysis.Host.Mef
Imports Microsoft.CodeAnalysis.LanguageServices
Imports Microsoft.CodeAnalysis.SemanticModelReuse
Imports Microsoft.CodeAnalysis.VisualBasic.LanguageServices
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.VisualBasic.SemanticModelReuse
    <ExportLanguageService(GetType(ISemanticModelReuseLanguageService), LanguageNames.VisualBasic), [Shared]>
    Friend Class VisualBasicSemanticModelReuseLanguageService
        Inherits AbstractSemanticModelReuseLanguageService(Of
            MethodBlockBaseSyntax, AccessorBlockSyntax, PropertyBlockSyntax, EventBlockSyntax)

        <ImportingConstructor>
        <Obsolete(MefConstruction.ImportingConstructorMessage, True)>
        Public Sub New()
        End Sub

        Protected Overrides ReadOnly Property SyntaxFacts As ISyntaxFacts = VisualBasicSyntaxFacts.Instance

        Protected Overrides Function GetAccessors([event] As EventBlockSyntax) As SyntaxList(Of AccessorBlockSyntax)
            Return [event].Accessors
        End Function

        Protected Overrides Function GetAccessors([property] As PropertyBlockSyntax) As SyntaxList(Of AccessorBlockSyntax)
            Return [property].Accessors
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

        Protected Overrides Async Function TryGetSpeculativeSemanticModelWorkerAsync(
            previousSemanticModel As SemanticModel, currentBodyNode As SyntaxNode, cancellationToken As CancellationToken) As Task(Of SemanticModel)

            Dim previousRoot = Await previousSemanticModel.SyntaxTree.GetRootAsync(cancellationToken).ConfigureAwait(False)
            Dim currentRoot = Await currentBodyNode.SyntaxTree.GetRootAsync(cancellationToken).ConfigureAwait(False)

            Dim previousBodyNode = TryCast(GetPreviousBodyNode(previousRoot, currentRoot, currentBodyNode), MethodBlockBaseSyntax)
            If previousBodyNode Is Nothing Then
                Debug.Fail("Could not map current body to previous body, despite no top level changes")
                Return Nothing
            End If

            Dim speculativeModel As SemanticModel = Nothing
            If previousSemanticModel.TryGetSpeculativeSemanticModelForMethodBody(previousBodyNode.BlockStatement.FullSpan.End, DirectCast(currentBodyNode, MethodBlockBaseSyntax), speculativeModel) Then
                Return speculativeModel
            End If

            Return Nothing
        End Function
    End Class
End Namespace
