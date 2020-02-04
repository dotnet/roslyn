' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Collections.Concurrent
Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.VisualBasic

    ''' <summary>
    ''' The visitor which searches for a bound node inside a bound subtree
    ''' </summary>
    Friend NotInheritable Class BoundNodeFinder
        Inherits BoundTreeWalkerWithStackGuardWithoutRecursionOnTheLeftOfBinaryOperator

        Private ReadOnly _convertInsufficientExecutionStackExceptionToCancelledByStackGuardException As Boolean

        Public Shared Function ContainsNode(findWhere As BoundNode, findWhat As BoundNode, recursionDepth As Integer, convertInsufficientExecutionStackExceptionToCancelledByStackGuardException As Boolean) As Boolean
            Debug.Assert(findWhere IsNot Nothing)
            Debug.Assert(findWhat IsNot Nothing)

            If findWhere Is findWhat Then
                Return True
            End If

            Dim walker As New BoundNodeFinder(findWhat, recursionDepth, convertInsufficientExecutionStackExceptionToCancelledByStackGuardException)
            walker.Visit(findWhere)
            Return walker._nodeToFind Is Nothing
        End Function

        Private Sub New(_nodeToFind As BoundNode, recursionDepth As Integer, convertInsufficientExecutionStackExceptionToCancelledByStackGuardException As Boolean)
            MyBase.New(recursionDepth)
            Me._nodeToFind = _nodeToFind
            Me._convertInsufficientExecutionStackExceptionToCancelledByStackGuardException = convertInsufficientExecutionStackExceptionToCancelledByStackGuardException
        End Sub

        ''' <summary> Note: Nothing if node is found </summary>
        Private _nodeToFind As BoundNode

        Public Overrides Function Visit(node As BoundNode) As BoundNode
            If Me._nodeToFind IsNot Nothing Then
                If Me._nodeToFind Is node Then
                    Me._nodeToFind = Nothing
                Else
                    MyBase.Visit(node)
                End If
            End If
            Return Nothing
        End Function

        Protected Overrides Function ConvertInsufficientExecutionStackExceptionToCancelledByStackGuardException() As Boolean
            Return _convertInsufficientExecutionStackExceptionToCancelledByStackGuardException
        End Function

        Public Overrides Function VisitUnboundLambda(node As UnboundLambda) As BoundNode
            Visit(node.BindForErrorRecovery())
            Return Nothing
        End Function
    End Class

End Namespace
