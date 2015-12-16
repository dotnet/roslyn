' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Threading
Imports System.Threading.Tasks
Imports Microsoft.CodeAnalysis.Editor.Implementation.Outlining
Imports Microsoft.CodeAnalysis.Editor.UnitTests.Outlining
Imports Microsoft.CodeAnalysis.Editor.VisualBasic.Outlining

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.UnitTests.Outlining
    Public Class CommentTests
        Inherits AbstractSyntaxOutlinerTests

        Protected Overrides ReadOnly Property LanguageName As String
            Get
                Return LanguageNames.VisualBasic
            End Get
        End Property

        Friend Overrides Function GetRegionsAsync(document As Document, position As Integer) As Task(Of OutliningSpan())
            Dim root = document.GetSyntaxRootAsync(CancellationToken.None).Result
            Dim trivia = root.FindTrivia(position, findInsideTrivia:=True)

            Dim token = trivia.Token

            If token.LeadingTrivia.Contains(trivia) Then
                Return Task.FromResult(VisualBasicOutliningHelpers.CreateCommentsRegions(token.LeadingTrivia).ToArray())
            ElseIf token.TrailingTrivia.Contains(trivia) Then
                Return Task.FromResult(VisualBasicOutliningHelpers.CreateCommentsRegions(token.TrailingTrivia).ToArray())
            Else
                Return Task.FromResult(Contract.FailWithReturn(Of OutliningSpan())())
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

            Await VerifyRegionsAsync(code,
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

            Await VerifyRegionsAsync(code,
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

            Await VerifyRegionsAsync(code,
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

            Await VerifyRegionsAsync(code,
                Region("span", "' Hello ...", autoCollapse:=True))
        End Function

    End Class
End Namespace
