﻿' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Collections.Immutable
Imports System.Runtime.CompilerServices

Namespace Microsoft.CodeAnalysis.VisualBasic

    Friend Module BoundNodeExtensions

        'Return if any node in an array of nodes has errors.
        <Extension()>
        Public Function NonNullAndHasErrors(Of T As BoundNode)(nodeArray As ImmutableArray(Of T)) As Boolean
            If nodeArray.IsDefault Then
                Return False
            End If

            For i As Integer = 0 To nodeArray.Length - 1
                If nodeArray(i).HasErrors Then
                    Return True
                End If
            Next

            Return False
        End Function

        ' Like HasErrors property, but also returns false for a null node. 
        <Extension()>
        Public Function NonNullAndHasErrors(node As BoundNode) As Boolean
            Return node IsNot Nothing AndAlso node.HasErrors
        End Function

        <Extension()>
        Public Function MakeCompilerGenerated(Of T As BoundNode)(this As T) As T
            this.SetWasCompilerGenerated()
            Return this
        End Function

        ''' <summary>
        ''' Get the Binder from a lambda node, or return Nothing if this isn't 
        ''' a lambda node.
        ''' </summary>
        <Extension()>
        Public Function GetBinderFromLambda(boundNode As BoundNode) As Binder
            Select Case boundNode.Kind
                Case BoundKind.UnboundLambda
                    Return DirectCast(boundNode, UnboundLambda).Binder
                Case BoundKind.QueryLambda
                    Return DirectCast(boundNode, BoundQueryLambda).LambdaSymbol.ContainingBinder
                Case BoundKind.GroupTypeInferenceLambda
                    Return DirectCast(boundNode, GroupTypeInferenceLambda).Binder
                Case Else
                    Return Nothing
            End Select
        End Function

        ' Is this bound node any kind of lambda?
        <Extension()>
        Public Function IsAnyLambda(boundNode As BoundNode) As Boolean
            Dim kind = boundNode.Kind
            Return kind = BoundKind.UnboundLambda OrElse kind = BoundKind.Lambda OrElse kind = BoundKind.QueryLambda OrElse kind = BoundKind.GroupTypeInferenceLambda
        End Function
    End Module
End Namespace

