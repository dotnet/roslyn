' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Collections.Immutable
Imports System.Threading
Imports Microsoft.CodeAnalysis.CodeRefactorings
Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.VisualBasic.LanguageService
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.VisualBasic.CodeRefactorings
    Friend Module NodeSelectionHelpers
        Friend Async Function GetSelectedMemberDeclarationAsync(context As CodeRefactoringContext) As Task(Of ImmutableArray(Of SyntaxNode))
            Dim document As Document = context.Document
            Dim span As TextSpan = context.Span
            Dim cancellationToken As CancellationToken = context.CancellationToken
            If span.IsEmpty Then
                ' MethodBaseSyntax also includes properties
                Dim methodMember = Await context.TryGetRelevantNodeAsync(Of MethodBaseSyntax)().ConfigureAwait(False)
                If methodMember IsNot Nothing Then
                    Return ImmutableArray.Create(Of SyntaxNode)(methodMember)
                End If
                ' Gets field variable declarations (not including the keywords like Public/Shared, etc), which are not methods
                Dim fieldDeclaration = Await context.TryGetRelevantNodeAsync(Of FieldDeclarationSyntax).ConfigureAwait(False)
                If fieldDeclaration Is Nothing Then
                    ' Gets the identifier + type of the field itself (ex. TestField As Integer), since it is nested in the variable declaration
                    ' And so the token's parent is not a variable declaration
                    Dim modifiedIdentifier = Await context.TryGetRelevantNodeAsync(Of ModifiedIdentifierSyntax).ConfigureAwait(False)
                    If modifiedIdentifier Is Nothing Then
                        Return ImmutableArray(Of SyntaxNode).Empty
                    Else
                        Return ImmutableArray.Create(Of SyntaxNode)(modifiedIdentifier)
                    End If
                Else
                    ' Field declarations can contain multiple variables (each of which are a "member")
                    Return fieldDeclaration.Declarators.SelectMany(Function(vds) vds.Names).Cast(Of SyntaxNode).AsImmutable()
                End If
            Else
                ' if the span is non-empty, then we get potentially multiple members
                ' Note: even though this method handles the empty span case, we don't use it because it doesn't correctly
                ' pick up on keywords before the declaration, such as "public static int".
                ' We could potentially use it for every case if that behavior changes
                Dim tree = Await document.GetSyntaxTreeAsync(cancellationToken).ConfigureAwait(False)
                Dim selectedMembers = Await VisualBasicSelectedMembers.Instance.
                    GetSelectedMembersAsync(tree, span, allowPartialSelection:=True, cancellationToken).ConfigureAwait(False)
                If selectedMembers.OfType(Of IncompleteMemberSyntax)().Any() Then
                    Return ImmutableArray(Of SyntaxNode).Empty
                Else
                    Return selectedMembers
                End If
            End If
        End Function
    End Module
End Namespace

