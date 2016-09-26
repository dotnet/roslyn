' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Collections.Immutable
Imports Microsoft.CodeAnalysis.Editor.UnitTests.Structure
Imports Microsoft.CodeAnalysis.Structure
Imports Microsoft.CodeAnalysis.VisualBasic.Structure

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.UnitTests.Outlining
    Public Class CommentTests
        Inherits AbstractSyntaxStructureProviderTests

        Protected Overrides ReadOnly Property LanguageName As String
            Get
                Return LanguageNames.VisualBasic
            End Get
        End Property

        Friend Overrides Async Function GetBlockSpansWorkerAsync(document As Document, position As Integer) As Task(Of ImmutableArray(Of BlockSpan))
            Dim root = Await document.GetSyntaxRootAsync()
            Dim trivia = root.FindTrivia(position, findInsideTrivia:=True)

            Dim token = trivia.Token

            If token.LeadingTrivia.Contains(trivia) Then
                Return CreateCommentsRegions(token.LeadingTrivia)
            ElseIf token.TrailingTrivia.Contains(trivia) Then
                Return CreateCommentsRegions(token.TrailingTrivia)
            Else
                Return Contract.FailWithReturn(Of ImmutableArray(Of BlockSpan))()
            End If
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.Outlining)>
        Public Async Function TestSimpleComment1() As Task
            Const code = "
{|span:' $$Hello
' VB!|}
Class C1
End Class
"

            Await VerifyBlockSpansAsync(code,
                Region("span", "' Hello ...", autoCollapse:=True))
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.Outlining)>
        Public Async Function TestSimpleComment2() As Task
            Const code = "
{|span:' $$Hello
'
' VB!|}
Class C1
End Class
"

            Await VerifyBlockSpansAsync(code,
                Region("span", "' Hello ...", autoCollapse:=True))
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.Outlining)>
        Public Async Function TestSimpleComment3() As Task
            Const code = "
{|span:' $$Hello

' VB!|}
Class C1
End Class
"

            Await VerifyBlockSpansAsync(code,
                Region("span", "' Hello ...", autoCollapse:=True))
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.Outlining)>
        Public Async Function TestSingleLineCommentGroupFollowedByDocumentationComment() As Task
            Const code = "
{|span:' $$Hello

' VB!|}
''' <summary></summary>
Class C1
End Class
"

            Await VerifyBlockSpansAsync(code,
                Region("span", "' Hello ...", autoCollapse:=True))
        End Function
    End Class
End Namespace