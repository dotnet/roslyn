' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Collections.Immutable
Imports System.Diagnostics.CodeAnalysis
Imports System.Runtime.InteropServices
Imports Microsoft.CodeAnalysis.Differencing
Imports Microsoft.CodeAnalysis.EditAndContinue
Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.VisualBasic.EditAndContinue
    Friend NotInheritable Class MethodBody
        Inherits AbstractSimpleMemberBody

        Public Sub New(node As MethodBlockBaseSyntax)
            MyBase.New(node)
        End Sub

        Public Overrides Function GetCapturedVariables(model As SemanticModel) As ImmutableArray(Of ISymbol)
            Dim methodBlock = DirectCast(Node, MethodBlockBaseSyntax)
            If methodBlock.Statements.IsEmpty Then
                Return ImmutableArray(Of ISymbol).Empty
            End If

            Return model.AnalyzeDataFlow(methodBlock.Statements.First, methodBlock.Statements.Last).Captured
        End Function

        Public Overrides Function GetStateMachineInfo() As StateMachineInfo
            Return VisualBasicEditAndContinueAnalyzer.GetStateMachineInfo(Node)
        End Function

        Public Overrides Function ComputeMatch(newBody As DeclarationBody, knownMatches As IEnumerable(Of KeyValuePair(Of SyntaxNode, SyntaxNode))) As Match(Of SyntaxNode)
            Return SyntaxComparer.Statement.ComputeMatch(Node, DirectCast(newBody, MethodBody).Node, knownMatches)
        End Function

        Public Overrides Function FindStatementAndPartner(span As TextSpan, partnerDeclarationBody As MemberBody, <Out> ByRef partnerStatement As SyntaxNode, <Out> ByRef statementPart As Integer) As SyntaxNode
            Return VisualBasicEditAndContinueAnalyzer.FindStatementAndPartner(
                span,
                body:=Node,
                partnerBody:=DirectCast(partnerDeclarationBody, MethodBody)?.Node,
                partnerStatement,
                statementPart)
        End Function

        Public Overrides Function TryMatchActiveStatement(newBody As DeclarationBody, oldStatement As SyntaxNode, statementPart As Integer, <NotNullWhen(True)> ByRef newStatement As SyntaxNode) As Boolean
            newStatement = Nothing
            Return False
        End Function
    End Class
End Namespace
