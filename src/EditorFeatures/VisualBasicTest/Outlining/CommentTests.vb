' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Threading
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

        Friend Overrides Function GetRegions(document As Document, position As Integer) As OutliningSpan()
            Dim root = document.GetSyntaxRootAsync(CancellationToken.None).Result
            Dim trivia = root.FindTrivia(position, findInsideTrivia:=True)

            Dim token = trivia.Token

            If token.LeadingTrivia.Contains(trivia) Then
                Return VisualBasicOutliningHelpers.CreateCommentsRegions(token.LeadingTrivia).ToArray()
            ElseIf token.TrailingTrivia.Contains(trivia) Then
                Return VisualBasicOutliningHelpers.CreateCommentsRegions(token.TrailingTrivia).ToArray()
            Else
                Return Contract.FailWithReturn(Of OutliningSpan())()
            End If
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.Outlining)>
        Public Sub TestSimpleComment1()
            Const code = "
{|span:' $$Hello
' VB!|}
Class C1
End Class
"

            Regions(code,
                Region("span", "' Hello ...", autoCollapse:=True))
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.Outlining)>
        Public Sub TestSimpleComment2()
            Const code = "
{|span:' $$Hello
'
' VB!|}
Class C1
End Class
"

            Regions(code,
                Region("span", "' Hello ...", autoCollapse:=True))
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.Outlining)>
        Public Sub TestSimpleComment3()
            Const code = "
{|span:' $$Hello

' VB!|}
Class C1
End Class
"

            Regions(code,
                Region("span", "' Hello ...", autoCollapse:=True))
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.Outlining)>
        Public Sub TestSingleLineCommentGroupFollowedByDocumentationComment()
            Const code = "
{|span:' $$Hello

' VB!|}
''' <summary></summary>
Class C1
End Class
"

            Regions(code,
                Region("span", "' Hello ...", autoCollapse:=True))
        End Sub

    End Class
End Namespace
