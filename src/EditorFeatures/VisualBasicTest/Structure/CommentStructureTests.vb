' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Collections.Immutable
Imports Microsoft.CodeAnalysis.Editor.UnitTests.Structure
Imports Microsoft.CodeAnalysis.Structure
Imports Microsoft.CodeAnalysis.VisualBasic.Structure

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.UnitTests.Outlining
    <Trait(Traits.Feature, Traits.Features.Outlining)>
    Public Class CommentTests
        Inherits AbstractSyntaxStructureProviderTests

        Protected Overrides ReadOnly Property LanguageName As String
            Get
                Return LanguageNames.VisualBasic
            End Get
        End Property

        Friend Overrides Async Function GetBlockSpansWorkerAsync(document As Document, options As BlockStructureOptions, position As Integer) As Task(Of ImmutableArray(Of BlockSpan))
            Dim root = Await document.GetSyntaxRootAsync()
            Dim trivia = root.FindTrivia(position, findInsideTrivia:=True)

            Dim token = trivia.Token

            If token.LeadingTrivia.Contains(trivia) Then
                Return CreateCommentsRegions(token.LeadingTrivia)
            ElseIf token.TrailingTrivia.Contains(trivia) Then
                Return CreateCommentsRegions(token.TrailingTrivia)
            Else
                Throw Roslyn.Utilities.ExceptionUtilities.Unreachable
            End If
        End Function

        <Fact>
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

        <Fact>
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

        <Fact>
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

        <Fact>
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
