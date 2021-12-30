' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Runtime.CompilerServices
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.VisualBasic.Extensions

    Friend Module SimpleNameSyntaxExtensions
        <Extension()>
        Public Function GetLeftSideOfDot(name As SimpleNameSyntax) As ExpressionSyntax
            Debug.Assert(IsSimpleMemberAccessExpressionName(name) OrElse IsRightSideOfQualifiedName(name))
            If IsSimpleMemberAccessExpressionName(name) Then
                Return DirectCast(name.Parent, MemberAccessExpressionSyntax).Expression
            Else
                Return DirectCast(name.Parent, QualifiedNameSyntax).Left
            End If
        End Function

        ' Returns true if this looks like a possible type name that is on it's own (i.e. not after a
        ' dot).  This function is not exhaustive and additional checks may be added if they are
        ' believed to be valuable.
        <Extension()>
        Public Function LooksLikeStandaloneTypeName(simpleName As SimpleNameSyntax) As Boolean
            If simpleName Is Nothing Then
                Return False
            End If

            ' Isn't stand-alone if it's on the right of a dot/arrow
            If simpleName.IsRightSideOfDot() Then
                Return False
            End If

            ' type names can't be invoked.
            If simpleName.IsParentKind(SyntaxKind.InvocationExpression) Then
                Dim invocationExpression = DirectCast(simpleName.Parent, InvocationExpressionSyntax)
                If invocationExpression.Expression Is simpleName AndAlso invocationExpression.ArgumentList IsNot Nothing Then
                    Return False
                End If
            End If

            ' Looks good.  However, feel free to add additional checks if this function is too
            ' lenient in some circumstances.
            Return True
        End Function
    End Module
End Namespace
