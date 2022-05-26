' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.VisualBasic
    ''' <summary>
    ''' Represents a <see cref="SyntaxNode"/> visitor that visits only the single SyntaxNode
    ''' passed into its <see cref="Visit(SyntaxNode)"/> method.
    ''' </summary>
    Partial Public MustInherit Class VisualBasicSyntaxVisitor

        Public Overridable Sub Visit(ByVal node As SyntaxNode)
            If node IsNot Nothing Then
                DirectCast(node, VisualBasicSyntaxNode).Accept(Me)
            End If
        End Sub

        Public Overridable Sub DefaultVisit(ByVal node As SyntaxNode)
        End Sub
    End Class

    ''' <summary>
    ''' Represents a <see cref="SyntaxNode"/> visitor that visits only the single SyntaxNode
    ''' passed into its <see cref="Visit(SyntaxNode)"/> method and produces 
    ''' a value of the type specified by the <typeparamref name="TResult"/> parameter.
    ''' </summary>
    ''' <typeparam name="TResult">
    ''' The type of the return value of this visitor's Visit method.
    ''' </typeparam>
    Partial Public MustInherit Class VisualBasicSyntaxVisitor(Of TResult)

        Public Overridable Function Visit(ByVal node As SyntaxNode) As TResult
            If node IsNot Nothing Then
                Return DirectCast(node, VisualBasicSyntaxNode).Accept(Me)
            End If

            Return Nothing
        End Function

        Public Overridable Function DefaultVisit(ByVal node As SyntaxNode) As TResult
            Return Nothing
        End Function
    End Class
End Namespace
