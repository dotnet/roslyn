' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

'-----------------------------------------------------------------------------------------------------------
' Contains hand-written Partial class extensions to certain of the syntax nodes (other that the 
' base node SyntaxNode, which is in a different file.)
'-----------------------------------------------------------------------------------------------------------

Imports System.Threading
Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.VisualBasic.Syntax

    Partial Public Class DocumentationCommentTriviaSyntax
        Friend Function GetInteriorXml() As String
            ' NOTE: is only used in parse tests
            Return DirectCast(Me.Green, InternalSyntax.DocumentationCommentTriviaSyntax).GetInteriorXml
        End Function
    End Class

    Partial Public Class DirectiveTriviaSyntax
        Private Shared ReadOnly s_hasDirectivesFunction As Func(Of SyntaxToken, Boolean) = Function(n) n.ContainsDirectives

        Public Function GetNextDirective(Optional predicate As Func(Of DirectiveTriviaSyntax, Boolean) = Nothing) As DirectiveTriviaSyntax
            Dim token = CType(MyBase.ParentTrivia.Token, SyntaxToken)

            Dim [next] As Boolean = False
            Do While (token.Kind <> SyntaxKind.None)
                Dim tr As SyntaxTrivia
                For Each tr In token.LeadingTrivia
                    If [next] Then
                        If tr.IsDirective Then
                            Dim d As DirectiveTriviaSyntax = DirectCast(tr.GetStructure, DirectiveTriviaSyntax)
                            If ((predicate Is Nothing) OrElse predicate.Invoke(d)) Then
                                Return d
                            End If
                        End If
                        Continue For
                    End If
                    If (tr.UnderlyingNode Is MyBase.Green) Then
                        [next] = True
                    End If
                Next
                token = token.GetNextToken(s_hasDirectivesFunction)
            Loop
            Return Nothing
        End Function

        Public Function GetPreviousDirective(Optional predicate As Func(Of DirectiveTriviaSyntax, Boolean) = Nothing) As DirectiveTriviaSyntax
            Dim token As SyntaxToken = CType(MyBase.ParentTrivia.Token, SyntaxToken)

            Dim [next] As Boolean = False
            Do While (token.Kind <> SyntaxKind.None)
                For Each tr In token.LeadingTrivia.Reverse()
                    If [next] Then
                        If tr.IsDirective Then
                            Dim d As DirectiveTriviaSyntax = DirectCast(tr.GetStructure, DirectiveTriviaSyntax)
                            If ((predicate Is Nothing) OrElse predicate.Invoke(d)) Then
                                Return d
                            End If
                        End If
                    ElseIf (tr.UnderlyingNode Is MyBase.Green) Then
                        [next] = True
                    End If
                Next
                token = token.GetPreviousToken(s_hasDirectivesFunction)
            Loop
            Return Nothing
        End Function
    End Class

    Partial Public Class SingleLineLambdaExpressionSyntax
        ''' <summary>
        ''' Single line subs only have a single statement.  However, when binding it is convenient to have a statement list.  For example,
        ''' dim statements are not valid in a single line lambda.  However, it is nice to be able to provide semantic info about the local.
        ''' The only way to create locals is to have a statement list. This method is friend because the statement list should not be part
        ''' of the public api.
        ''' </summary>
        Friend ReadOnly Property Statements As SyntaxList(Of StatementSyntax)
            Get
                Debug.Assert(Kind = SyntaxKind.SingleLineSubLambdaExpression, "Only SingleLineSubLambdas have statements.")
                Debug.Assert(GetNodeSlot(1) Is Body, "SingleLineLambdaExpressionSyntax structure has changed.  Update index passed to GetChildIndex.")

                Return New SyntaxList(Of StatementSyntax)(Body)
            End Get
        End Property
    End Class

    Partial Public Class MethodBaseSyntax

        Friend ReadOnly Property AsClauseInternal As AsClauseSyntax
            Get
                Select Case Me.Kind
                    Case SyntaxKind.SubStatement, SyntaxKind.FunctionStatement
                        Return DirectCast(Me, MethodStatementSyntax).AsClause

                    Case SyntaxKind.SubLambdaHeader, SyntaxKind.FunctionLambdaHeader
                        Return DirectCast(Me, LambdaHeaderSyntax).AsClause

                    Case SyntaxKind.DeclareSubStatement, SyntaxKind.DeclareFunctionStatement
                        Return DirectCast(Me, DeclareStatementSyntax).AsClause

                    Case SyntaxKind.DelegateSubStatement, SyntaxKind.DelegateFunctionStatement
                        Return DirectCast(Me, DelegateStatementSyntax).AsClause

                    Case SyntaxKind.EventStatement
                        Return DirectCast(Me, EventStatementSyntax).AsClause

                    Case SyntaxKind.OperatorStatement
                        Return DirectCast(Me, OperatorStatementSyntax).AsClause

                    Case SyntaxKind.PropertyStatement
                        Return DirectCast(Me, PropertyStatementSyntax).AsClause

                    Case SyntaxKind.SubNewStatement,
                        SyntaxKind.GetAccessorStatement,
                        SyntaxKind.SetAccessorStatement,
                        SyntaxKind.AddHandlerAccessorStatement,
                        SyntaxKind.RemoveHandlerAccessorStatement,
                        SyntaxKind.RaiseEventAccessorStatement
                        Return Nothing

                    Case Else
                        Throw ExceptionUtilities.UnexpectedValue(Me.Kind)
                End Select
            End Get
        End Property

    End Class

End Namespace

