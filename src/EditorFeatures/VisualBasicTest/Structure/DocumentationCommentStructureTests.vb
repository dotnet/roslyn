' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports Microsoft.CodeAnalysis.Structure
Imports Microsoft.CodeAnalysis.VisualBasic.Structure
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.UnitTests.Outlining
    <Trait(Traits.Feature, Traits.Features.Outlining)>
    Public Class DocumentationCommentStructureProviderTests
        Inherits AbstractVisualBasicSyntaxNodeStructureProviderTests(Of DocumentationCommentTriviaSyntax)

        Friend Overrides Function CreateProvider() As AbstractSyntaxStructureProvider
            Return New DocumentationCommentStructureProvider()
        End Function

        <Fact>
        Public Async Function TestDocumentationCommentWithoutSummaryTag1() As Task
            Const code = "
{|span:''' $$XML doc comment
''' some description
''' of
''' the comment|}
Class C1
End Class
"

            Await VerifyBlockSpansAsync(code,
                Region("span", "''' XML doc comment ...", autoCollapse:=True))
        End Function

        <Fact>
        Public Async Function TestDocumentationCommentWithoutSummaryTag2() As Task
            Const code = "
{|span:''' $$<param name=""syntaxTree""></param>|}
Class C1
End Class
"

            Await VerifyBlockSpansAsync(code,
                Region("span", "''' <param name=""syntaxTree""></param> ...", autoCollapse:=True))
        End Function

        <Fact>
        Public Async Function TestDocumentationComment() As Task
            Const code = "
{|span:''' $$<summary>
''' Hello VB!
''' </summary>|}
Class C1
End Class
"

            Await VerifyBlockSpansAsync(code,
                Region("span", "''' <summary> Hello VB!", autoCollapse:=True))
        End Function

        <Fact>
        Public Async Function TestDocumentationCommentWithLongBannerText() As Task
            Dim code = "
{|span:''' $$<summary>
''' " & New String("x"c, 240) & "
''' </summary>|}
Class C1
End Class
"

            Await VerifyBlockSpansAsync(code,
                Region("span", "''' <summary> " & New String("x"c, 106) & " ...", autoCollapse:=True))
        End Function

        <Fact>
        Public Async Function TestIndentedDocumentationComment() As Task
            Const code = "
    {|span:''' $$<summary>
    ''' Hello VB!
    ''' </summary>|}
    Class C1
    End Class
"

            Await VerifyBlockSpansAsync(code,
                Region("span", "''' <summary> Hello VB!", autoCollapse:=True))
        End Function

        <Fact>
        Public Async Function TestDocumentationCommentOnASingleLine() As Task
            Const code = "
{|span:''' $$<summary>Hello VB!</summary>|}
Class C1
End Class
"

            Await VerifyBlockSpansAsync(code,
                Region("span", "''' <summary>Hello VB!", autoCollapse:=True))
        End Function

        <Fact>
        Public Async Function TestIndentedDocumentationCommentOnASingleLine() As Task
            Const code = "
    {|span:''' $$<summary>Hello VB!</summary>|}
    Class C1
    End Class
"

            Await VerifyBlockSpansAsync(code,
                Region("span", "''' <summary>Hello VB!", autoCollapse:=True))
        End Function

        <Fact>
        Public Async Function TestMultilineSummaryInDocumentationComment1() As Task
            Const code = "
{|span:''' $$<summary>
''' Hello
''' VB!
''' </summary>|}
Class C1
End Class
"

            Await VerifyBlockSpansAsync(code,
                Region("span", "''' <summary> Hello VB!", autoCollapse:=True))
        End Function

        <Fact>
        Public Async Function TestMultilineSummaryInDocumentationComment2() As Task
            Const code = "
{|span:''' $$<summary>
''' Hello
''' 
''' VB!
''' </summary>|}
Class C1
End Class
"

            Await VerifyBlockSpansAsync(code,
                Region("span", "''' <summary> Hello VB!", autoCollapse:=True))
        End Function

        <Fact, WorkItem("https://github.com/dotnet/roslyn/issues/2129")>
        Public Async Function CrefInSummary() As Task
            Const code = "
Class C
    {|span:''' $$<summary>
    ''' Summary with <see cref=""SeeClass"" />, <seealso cref=""SeeAlsoClass"" />,
    ''' <see langword=""Nothing"" />, <typeparamref name=""T"" />, <paramref name=""t"" />, and <see unsupported-attribute=""not-supported"" />.
    ''' </summary>|}
    Sub M(Of T)(t as T)
    End Sub
End Class
"

            Await VerifyBlockSpansAsync(code,
                Region("span", "''' <summary> Summary with SeeClass, SeeAlsoClass, Nothing, T, t, and not-supported.", autoCollapse:=True))
        End Function

        <Fact, WorkItem("https://devdiv.visualstudio.com/DevDiv/_workitems?id=402822")>
        Public Async Function TestSummaryWithPunctuation() As Task
            Const code = "
class C
    {|span:''' $$<summary>
    ''' The main entrypoint for <see cref=""Program""/>.
    ''' </summary>
    ''' <param name=""args""></param>|}
    Sub Main()
    End Sub
end class"

            Await VerifyBlockSpansAsync(code,
                Region("span", "''' <summary> The main entrypoint for Program.", autoCollapse:=True))
        End Function
    End Class
End Namespace
