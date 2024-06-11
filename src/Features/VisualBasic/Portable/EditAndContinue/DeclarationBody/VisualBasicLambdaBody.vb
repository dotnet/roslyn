' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Collections.Immutable
Imports System.Diagnostics.CodeAnalysis
Imports Microsoft.CodeAnalysis.Differencing
Imports Microsoft.CodeAnalysis.EditAndContinue
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.VisualBasic.EditAndContinue
    Friend NotInheritable Class VisualBasicLambdaBody
        Inherits LambdaBody

        Private ReadOnly _node As SyntaxNode

        Public Sub New(node As SyntaxNode)
            Debug.Assert(TypeOf node.Parent Is LambdaExpressionSyntax OrElse TypeOf node Is ExpressionSyntax)
            _node = node
        End Sub

        Public Overrides ReadOnly Property SyntaxTree As SyntaxTree
            Get
                Return _node.SyntaxTree
            End Get
        End Property

        Public Overrides ReadOnly Property RootNodes As OneOrMany(Of SyntaxNode)
            Get
                Return OneOrMany.Create(EncompassingAncestor)
            End Get
        End Property

        Public Overrides ReadOnly Property EncompassingAncestor As SyntaxNode
            Get
                Return If(TypeOf _node.Parent Is LambdaExpressionSyntax, _node.Parent, _node)
            End Get
        End Property

        Public Overrides Function GetStateMachineInfo() As StateMachineInfo
            Return VisualBasicEditAndContinueAnalyzer.GetStateMachineInfo(_node)
        End Function

        Public Overrides Function GetCapturedVariables(model As SemanticModel) As ImmutableArray(Of ISymbol)
            Return model.AnalyzeDataFlow(If(TryCast(_node.Parent, LambdaExpressionSyntax), _node)).Captured
        End Function

        Public Overrides Function TryGetPartnerLambdaBody(newLambda As SyntaxNode) As LambdaBody
            Return SyntaxUtilities.CreateLambdaBody(LambdaUtilities.GetCorrespondingLambdaBody(_node, newLambda))
        End Function

        Public Overrides Function GetExpressionsAndStatements() As IEnumerable(Of SyntaxNode)
            Return LambdaUtilities.GetLambdaBodyExpressionsAndStatements(_node)
        End Function

        Public Overrides Function GetLambda() As SyntaxNode
            Return LambdaUtilities.GetLambda(_node)
        End Function

        Public Overrides Function IsSyntaxEquivalentTo(other As LambdaBody) As Boolean
            Return GetExpressionsAndStatements().SequenceEqual(other.GetExpressionsAndStatements(), AddressOf SyntaxFactory.AreEquivalent)
        End Function

        Public Overrides Function ComputeSingleRootMatch(newBody As DeclarationBody, knownMatches As IEnumerable(Of KeyValuePair(Of SyntaxNode, SyntaxNode))) As Match(Of SyntaxNode)
            Dim newLambdaBody = DirectCast(newBody, VisualBasicLambdaBody)

            If TypeOf _node.Parent Is LambdaExpressionSyntax Then
                ' The root is a single/multi line sub/function lambda.
                Dim comparer = New SyntaxComparer(_node.Parent, newLambdaBody._node.Parent, _node.Parent.ChildNodes(), newLambdaBody._node.Parent.ChildNodes(), matchingLambdas:=True, compareStatementSyntax:=True)
                Return comparer.ComputeMatch(_node.Parent, newLambdaBody._node.Parent, knownMatches)
            Else
                ' Queries: The root is a query clause, the body is the expression.
                Dim comparer = New SyntaxComparer(_node.Parent, newLambdaBody._node.Parent, {_node}, {newLambdaBody._node}, matchingLambdas:=False, compareStatementSyntax:=True)
                Return comparer.ComputeMatch(_node.Parent, newLambdaBody._node.Parent, knownMatches)
            End If
        End Function

        Public Overrides Function TryMatchActiveStatement(newBody As DeclarationBody, oldStatement As SyntaxNode, ByRef statementPart As Integer, <NotNullWhen(True)> ByRef newStatement As SyntaxNode) As Boolean
            Dim newLambdaBody = DirectCast(newBody, VisualBasicLambdaBody)

            If TypeOf _node.Parent Is LambdaExpressionSyntax Then
                Dim oldSingleLineLambda = TryCast(_node.Parent, SingleLineLambdaExpressionSyntax)
                Dim newSingleLineLambda = TryCast(newLambdaBody._node.Parent, SingleLineLambdaExpressionSyntax)

                If oldSingleLineLambda IsNot Nothing AndAlso
                   newSingleLineLambda IsNot Nothing AndAlso
                   oldStatement Is oldSingleLineLambda.Body Then

                    newStatement = newSingleLineLambda.Body
                    Return True
                End If
            ElseIf oldStatement Is _node Then ' Queries
                newStatement = newLambdaBody._node
                Return True
            End If

            newStatement = Nothing
            Return False
        End Function
    End Class
End Namespace
