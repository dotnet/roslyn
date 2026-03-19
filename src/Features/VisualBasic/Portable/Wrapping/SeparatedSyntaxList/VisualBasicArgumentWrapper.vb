' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.VisualBasic.LanguageService
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.VisualBasic.Wrapping.SeparatedSyntaxList
    Partial Friend Class VisualBasicArgumentWrapper
        Inherits AbstractVisualBasicSeparatedSyntaxListWrapper(Of ArgumentListSyntax, ArgumentSyntax)

        Protected Overrides ReadOnly Property Align_wrapped_items As String = FeaturesResources.Align_wrapped_arguments
        Protected Overrides ReadOnly Property Indent_all_items As String = FeaturesResources.Indent_all_arguments
        Protected Overrides ReadOnly Property Indent_wrapped_items As String = FeaturesResources.Indent_wrapped_arguments
        Protected Overrides ReadOnly Property Unwrap_all_items As String = FeaturesResources.Unwrap_all_arguments
        Protected Overrides ReadOnly Property Unwrap_and_indent_all_items As String = FeaturesResources.Unwrap_and_indent_all_arguments
        Protected Overrides ReadOnly Property Unwrap_list As String = FeaturesResources.Unwrap_argument_list
        Protected Overrides ReadOnly Property Wrap_every_item As String = FeaturesResources.Wrap_every_argument
        Protected Overrides ReadOnly Property Wrap_long_list As String = FeaturesResources.Wrap_long_argument_list

        Public Overrides ReadOnly Property Supports_UnwrapGroup_WrapFirst_IndentRest As Boolean = True
        Public Overrides ReadOnly Property Supports_WrapEveryGroup_UnwrapFirst As Boolean = True
        Public Overrides ReadOnly Property Supports_WrapLongGroup_UnwrapFirst As Boolean = True

        Protected Overrides ReadOnly Property ShouldMoveCloseBraceToNewLine As Boolean = False

        Protected Overrides Function FirstToken(listSyntax As ArgumentListSyntax) As SyntaxToken
            Return listSyntax.OpenParenToken
        End Function

        Protected Overrides Function LastToken(listSyntax As ArgumentListSyntax) As SyntaxToken
            Return listSyntax.CloseParenToken
        End Function

        Protected Overrides Function GetListItems(listSyntax As ArgumentListSyntax) As SeparatedSyntaxList(Of ArgumentSyntax)
            Return listSyntax.Arguments
        End Function

        Protected Overrides Function TryGetApplicableList(node As SyntaxNode) As ArgumentListSyntax
            Return If(TryCast(node, InvocationExpressionSyntax)?.ArgumentList,
                      TryCast(node, ObjectCreationExpressionSyntax)?.ArgumentList)
        End Function

        Protected Overrides Function PositionIsApplicable(
                root As SyntaxNode, position As Integer, declaration As SyntaxNode, containsSyntaxError As Boolean, listSyntax As ArgumentListSyntax) As Boolean

            If containsSyntaxError Then
                Return False
            End If

            Dim startToken = listSyntax.GetFirstToken()

            ' If we have something Like  Foo(...)  Or  this.Foo(...)  allow anywhere in the Foo(...)
            If TypeOf declaration Is InvocationExpressionSyntax Then
                Dim expr = DirectCast(declaration, InvocationExpressionSyntax).Expression
                Dim name =
                    If(TryCast(expr, NameSyntax),
                       TryCast(expr, MemberAccessExpressionSyntax)?.Name)

                startToken = If(name Is Nothing, listSyntax.GetFirstToken(), name.GetFirstToken())
            ElseIf TypeOf declaration Is ObjectCreationExpressionSyntax Then
                ' allow anywhere in `New Foo(...)`
                startToken = declaration.GetFirstToken()
            End If

            Dim endToken = listSyntax.GetLastToken()
            Dim span = TextSpan.FromBounds(startToken.SpanStart, endToken.Span.End)
            If Not span.IntersectsWith(position) Then
                Return False
            End If

            ' allow anywhere in the arg list, as long we don't end up walking through something
            ' complex Like a lambda/anonymous function.
            Dim token = root.FindToken(position)
            If token.Parent.Ancestors().Contains(listSyntax) Then
                Dim current = token.Parent
                While current IsNot listSyntax
                    If VisualBasicSyntaxFacts.Instance.IsAnonymousFunctionExpression(current) Then
                        Return False
                    End If

                    current = current.Parent
                End While
            End If

            Return True
        End Function
    End Class
End Namespace
