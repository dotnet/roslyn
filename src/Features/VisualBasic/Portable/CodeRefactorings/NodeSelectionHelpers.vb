' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Collections.Immutable
Imports System.Threading
Imports Microsoft.CodeAnalysis.CodeRefactorings
Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.VisualBasic.CodeRefactorings
    Friend Module NodeSelectionHelpers
        Friend Async Function GetSelectedMemberDeclarationAsync(context As CodeRefactoringContext) As Task(Of SyntaxNode)
            Dim methodMember = Await context.TryGetRelevantNodeAsync(Of MethodBaseSyntax)().ConfigureAwait(False)
            If methodMember IsNot Nothing Then
                Return methodMember
            End If
            ' Gets field variable declarations (not including the keywords like Public/Shared, etc), which are not methods
            Dim fieldDeclaration = Await context.TryGetRelevantNodeAsync(Of FieldDeclarationSyntax).ConfigureAwait(False)
            If fieldDeclaration IsNot Nothing Then
                ' Since field declarations can contain multiple variables (each of which are a "member"), we need to find one to choose
                ' We'll do this by selecting the one closest to the start of the span
                Dim span As TextSpan, cancellationToken As CancellationToken
                context.Deconstruct(Nothing, span, cancellationToken)
                Dim closestDeclarator As ModifiedIdentifierSyntax = Nothing
                Dim leastDistance = Integer.MaxValue
                For Each candidate In fieldDeclaration.Declarators.SelectMany(Function(vds) vds.Names)
                    Dim dist = Math.Min(Math.Abs(candidate.SpanStart - span.Start), Math.Abs(candidate.Span.End - span.Start))
                    If (dist < leastDistance) Then
                        closestDeclarator = candidate
                        leastDistance = dist
                    End If
                Next
                Return closestDeclarator
            End If
            ' Gets the identifier + type of the field itself (ex. TestField As Integer), since it is nested in the variable declaration
            ' And so the token's parent is not a variable declaration
            Return Await context.TryGetRelevantNodeAsync(Of ModifiedIdentifierSyntax).ConfigureAwait(False)
        End Function
    End Module
End Namespace

