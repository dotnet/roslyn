' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports Microsoft.CodeAnalysis.CodeRefactorings
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.VisualBasic.CodeRefactorings
    Friend Module NodeSelectionHelpers
        Friend Async Function GetSelectedMemberDeclarationAsync(context As CodeRefactoringContext) As Task(Of SyntaxNode)
            Dim methodMember = Await context.TryGetRelevantNodeAsync(Of MethodBaseSyntax)().ConfigureAwait(False)
            If methodMember IsNot Nothing Then
                Return methodMember
            Else
                ' Gets field members, which are not methods
                Return Await context.TryGetRelevantNodeAsync(Of VariableDeclaratorSyntax).ConfigureAwait(False)
            End If
        End Function
    End Module
End Namespace

