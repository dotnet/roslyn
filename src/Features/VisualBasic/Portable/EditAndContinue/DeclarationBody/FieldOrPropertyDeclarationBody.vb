' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Collections.Immutable
Imports System.Diagnostics.CodeAnalysis
Imports System.Linq.Expressions
Imports Microsoft.CodeAnalysis.Differencing
Imports Microsoft.CodeAnalysis.EditAndContinue
Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.VisualBasic.EditAndContinue
    ''' <summary>
    ''' Property initializers:
    '''   Property [|p As Integer = expr|]
    '''   Property [|p As New C(expr)|]
    ''' 
    ''' Simple field initializers:
    '''   Dim [|a = expr|]
    '''   Dim [|a As Integer = expr|]
    '''   Dim [|a = expr|], [|b = expr|], [|c As Integer = expr|]
    '''   Dim [|a As New C(expr)|] 
    ''' 
    ''' Array initialized fields
    '''   Dim [|a(expr, ...)|] As Integer
    '''   
    ''' Shared initializers
    '''   Dim [|a|], [|b|] As New C(expr)
    ''' </summary>
    Friend MustInherit Class FieldOrPropertyDeclarationBody
        Inherits MemberBody

        ''' <summary>
        ''' Node that represents the active statement for the initializer of the member.
        ''' </summary>
        Public MustOverride ReadOnly Property InitializerActiveStatement As SyntaxNode

        ''' <summary>
        ''' Node that may include other active statements than <see cref="InitializerActiveStatement"/> (e.g. in a lambda).
        ''' <see cref="Expression"/> or <see cref="ArgumentListSyntax"/>.
        ''' </summary>
        Public MustOverride ReadOnly Property OtherActiveStatementContainer As SyntaxNode

        Public NotOverridable Overrides ReadOnly Property SyntaxTree As SyntaxTree
            Get
                Return InitializerActiveStatement.SyntaxTree
            End Get
        End Property

        Public NotOverridable Overrides ReadOnly Property RootNodes As OneOrMany(Of SyntaxNode)
            Get
                Return OneOrMany.Create(OtherActiveStatementContainer.Parent)
            End Get
        End Property

        Public NotOverridable Overrides Function ComputeSingleRootMatch(newBody As DeclarationBody, knownMatches As IEnumerable(Of KeyValuePair(Of SyntaxNode, SyntaxNode))) As Match(Of SyntaxNode)
            Dim newFieldBody = DirectCast(newBody, FieldOrPropertyDeclarationBody)

            If TypeOf OtherActiveStatementContainer Is ExpressionSyntax Then
                ' Dim a = <Expression>
                ' Dim a As <NewExpression>
                ' Dim a, b, c As <NewExpression>
                Dim comparer = New SyntaxComparer(
                    OtherActiveStatementContainer.Parent,
                    newFieldBody.OtherActiveStatementContainer.Parent,
                    {OtherActiveStatementContainer},
                    {newFieldBody.OtherActiveStatementContainer},
                    matchingLambdas:=False,
                    compareStatementSyntax:=True)

                Return comparer.ComputeMatch(OtherActiveStatementContainer.Parent, newFieldBody.OtherActiveStatementContainer.Parent, knownMatches)
            End If

            ' Method, accessor, operator, etc. bodies are represented by the declaring block, which is also the root.
            ' The body of an array initialized fields is an ArgumentListSyntax, which is the match root.
            Return SyntaxComparer.Statement.ComputeMatch(OtherActiveStatementContainer, newFieldBody.OtherActiveStatementContainer, knownMatches)
        End Function

        Public NotOverridable Overrides Function FindStatementAndPartner(
                span As TextSpan, partnerDeclarationBody As MemberBody, ByRef partnerStatement As SyntaxNode, ByRef statementPart As Integer) As SyntaxNode

            ' If active statement span starts at InitializerActiveStatement it must be an active statement covering the whole modified identifier
            ' (not e.g. active statement of a lambda within the array bounds.

            Dim partnerFieldOrProperty = DirectCast(partnerDeclarationBody, FieldOrPropertyDeclarationBody)

            If span.Start = InitializerActiveStatement.SpanStart Then
                If partnerDeclarationBody IsNot Nothing Then
                    partnerStatement = partnerFieldOrProperty.InitializerActiveStatement
                End If

                Return InitializerActiveStatement
            End If

            Return VisualBasicEditAndContinueAnalyzer.FindStatementAndPartner(
                    span,
                    body:=OtherActiveStatementContainer,
                    partnerBody:=partnerFieldOrProperty?.OtherActiveStatementContainer,
                    partnerStatement,
                    statementPart)
        End Function

        Public Overrides Function TryMatchActiveStatement(newBody As DeclarationBody, oldStatement As SyntaxNode, ByRef statementPart As Integer, <NotNullWhen(True)> ByRef newStatement As SyntaxNode) As Boolean
            If oldStatement Is InitializerActiveStatement Then
                newStatement = DirectCast(newBody, FieldOrPropertyDeclarationBody).InitializerActiveStatement
                Return True
            End If

            newStatement = Nothing
            Return False
        End Function

        Public Overrides ReadOnly Property EncompassingAncestor As SyntaxNode
            Get
                Return InitializerActiveStatement
            End Get
        End Property

        Public Overrides ReadOnly Property Envelope As TextSpan
            Get
                Return InitializerActiveStatement.Span
            End Get
        End Property

        Public Overrides Function GetActiveTokens() As IEnumerable(Of SyntaxToken)
            Return InitializerActiveStatement.DescendantTokens()
        End Function

        Public Overrides Function GetCapturedVariables(model As SemanticModel) As ImmutableArray(Of ISymbol)
            Return model.AnalyzeDataFlow(OtherActiveStatementContainer).Captured
        End Function

        Public NotOverridable Overrides Function GetStateMachineInfo() As StateMachineInfo
            Return StateMachineInfo.None
        End Function
    End Class
End Namespace
