' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Runtime.CompilerServices

Namespace Microsoft.CodeAnalysis.VisualBasic.Syntax.InternalSyntax

    Friend Module BlockContextExtensions

        <Extension()>
        Friend Function EndLambda(context As BlockContext) As BlockContext
            Dim complete = False
            Do
                complete = context.IsLambda
                context = context.EndBlock(Nothing)
            Loop Until complete
            Return context
        End Function

        <Extension()>
        Friend Sub RecoverFromMissingEnd(context As BlockContext, lastContext As BlockContext)
            Debug.Assert(lastContext IsNot Nothing)

            While context.Level > lastContext.Level
                context = context.EndBlock(Nothing)
            End While
        End Sub

        <Extension()>
        Friend Function IsWithin(context As BlockContext, ParamArray kinds() As SyntaxKind) As Boolean
            Return context.FindNearest(kinds) IsNot Nothing
        End Function

        <Extension()>
        Friend Function FindNearest(context As BlockContext, conditionIsTrue As Func(Of BlockContext, Boolean)) As BlockContext
            While context IsNot Nothing
                If conditionIsTrue(context) Then
                    Return context
                End If
                context = context.PrevBlock
            End While
            Return Nothing
        End Function

        <Extension()>
        Friend Function FindNearest(context As BlockContext, conditionIsTrue As Func(Of SyntaxKind, Boolean)) As BlockContext
            While context IsNot Nothing
                If conditionIsTrue(context.BlockKind) Then
                    Return context
                End If
                context = context.PrevBlock
            End While
            Return Nothing
        End Function

        <Extension()>
        Friend Function FindNearest(context As BlockContext, ParamArray kinds() As SyntaxKind) As BlockContext
            While context IsNot Nothing
                If kinds.Contains(context.BlockKind) Then
                    Return context
                End If
                context = context.PrevBlock
            End While
            Return Nothing
        End Function

        <Extension()>
        Friend Function FindNearestInSameMethodScope(context As BlockContext, ParamArray kinds() As SyntaxKind) As BlockContext
            While context IsNot Nothing
                If kinds.Contains(context.BlockKind) Then
                    Return context
                End If
                If context.IsLambda Then
                    Return Nothing
                End If
                context = context.PrevBlock
            End While
            Return Nothing
        End Function

        <Extension()>
        Friend Function FindNearestLambdaOrSingleLineIf(context As BlockContext, lastContext As BlockContext) As BlockContext
            While context IsNot lastContext
                If context.IsLambda OrElse context.IsLineIf Then
                    Return context
                End If
                context = context.PrevBlock
            End While
            Return Nothing
        End Function

    End Module

End Namespace
