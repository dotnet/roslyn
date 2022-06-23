' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

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
            If fieldDeclaration Is Nothing Then
                ' Gets the identifier + type of the field itself (ex. TestField As Integer), since it is nested in the variable declaration
                ' And so the token's parent is not a variable declaration
                Return Await context.TryGetRelevantNodeAsync(Of ModifiedIdentifierSyntax).ConfigureAwait(False)
            Else
                ' Since field declarations can contain multiple variables (each of which are a "member"), we need to find one to choose
                ' First we break the field declaration into variable declaration(s), then each of those into modified identifier(s)
                ' Then we selecting the modified identifier (e.g. Foo As Integer) closest to the start of the span
                Dim span = context.Span
                Dim modifiedIdentifiers = fieldDeclaration.Declarators.SelectMany(Function(vds) vds.Names)
                Return modifiedIdentifiers.OrderBy(
                    Function(mi) Math.Min(Math.Abs(mi.SpanStart - span.Start), Math.Abs(mi.Span.End - span.Start))).First()
            End If
        End Function
    End Module
End Namespace

