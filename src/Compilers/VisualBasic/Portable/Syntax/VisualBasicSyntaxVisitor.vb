' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.VisualBasic
    ''' <summary>
    ''' Represents a <see cref="SyntaxNode"/> visitor that visits only the single SyntaxNode
    ''' passed into its <see cref="Visit(SyntaxNode)"/> method.
    ''' </summary>
    Partial Public MustInherit Class VisualBasicSyntaxVisitor
        Private Const MaxUncheckedRecursionDepth As Integer = Syntax.InternalSyntax.Parser.MaxUncheckedRecursionDepth
        Private _recursionDepth As Integer

        Public Overridable Sub Visit(ByVal node As SyntaxNode)
            If node IsNot Nothing Then
                _recursionDepth += 1

                If _recursionDepth > MaxUncheckedRecursionDepth Then
                    PortableShim.RuntimeHelpers.EnsureSufficientExecutionStack()
                End If

                DirectCast(node, VisualBasicSyntaxNode).Accept(Me)

                _recursionDepth -= 1
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
        Private Const MaxUncheckedRecursionDepth As Integer = Syntax.InternalSyntax.Parser.MaxUncheckedRecursionDepth
        Private _recursionDepth As Integer

        Public Overridable Function Visit(ByVal node As SyntaxNode) As TResult
            If node IsNot Nothing Then
                _recursionDepth += 1

                If _recursionDepth > MaxUncheckedRecursionDepth Then
                    PortableShim.RuntimeHelpers.EnsureSufficientExecutionStack()
                End If

                Dim result = DirectCast(node, VisualBasicSyntaxNode).Accept(Me)

                _recursionDepth -= 1

                Return result
            End If

            Return Nothing
        End Function

        Public Overridable Function DefaultVisit(ByVal node As SyntaxNode) As TResult
            Return Nothing
        End Function
    End Class
End Namespace
