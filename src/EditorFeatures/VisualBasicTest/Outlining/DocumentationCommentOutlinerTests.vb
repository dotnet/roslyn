' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Threading.Tasks
Imports Microsoft.CodeAnalysis.Editor.Implementation.Outlining
Imports Microsoft.CodeAnalysis.Editor.VisualBasic.Outlining
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.UnitTests.Outlining
    Public Class DocumentationCommentOutlinerTests
        Inherits AbstractVisualBasicSyntaxNodeOutlinerTests(Of DocumentationCommentTriviaSyntax)

        Friend Overrides Function CreateOutliner() As AbstractSyntaxOutliner
            Return New DocumentationCommentOutliner()
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.Outlining)>
        Public Async Function TestDocumentationCommentWithoutSummaryTag1() As Task
            Const code = "
{|span:''' $$XML doc comment
''' some description
''' of
''' the comment|}
Class C1
End Class
"

            Await VerifyRegionsAsync(code,
                Region("span", "''' XML doc comment ...", autoCollapse:=True))
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.Outlining)>
        Public Async Function TestDocumentationCommentWithoutSummaryTag2() As Task
            Const code = "
{|span:''' $$<param name=""syntaxTree""></param>|}
Class C1
End Class
"

            Await VerifyRegionsAsync(code,
                Region("span", "''' <param name=""syntaxTree""></param> ...", autoCollapse:=True))
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.Outlining)>
        Public Async Function TestDocumentationComment() As Task
            Const code = "
{|span:''' $$<summary>
''' Hello VB!
''' </summary>|}
Class C1
End Class
"

            Await VerifyRegionsAsync(code,
                Region("span", "''' <summary> Hello VB!", autoCollapse:=True))
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.Outlining)>
        Public Async Function TestDocumentationCommentWithLongBannerText() As Task
            Dim code = "
{|span:''' $$<summary>
''' " & New String("x"c, 240) & "
''' </summary>|}
Class C1
End Class
"

            Await VerifyRegionsAsync(code,
                Region("span", "''' <summary> " & New String("x"c, 106) & " ...", autoCollapse:=True))
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.Outlining)>
        Public Async Function TestIndentedDocumentationComment() As Task
            Const code = "
    {|span:''' $$<summary>
    ''' Hello VB!
    ''' </summary>|}
    Class C1
    End Class
"

            Await VerifyRegionsAsync(code,
                Region("span", "''' <summary> Hello VB!", autoCollapse:=True))
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.Outlining)>
        Public Async Function TestDocumentationCommentOnASingleLine() As Task
            Const code = "
{|span:''' $$<summary>Hello VB!</summary>|}
Class C1
End Class
"

            Await VerifyRegionsAsync(code,
                Region("span", "''' <summary> Hello VB!", autoCollapse:=True))
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.Outlining)>
        Public Async Function TestIndentedDocumentationCommentOnASingleLine() As Task
            Const code = "
    {|span:''' $$<summary>Hello VB!</summary>|}
    Class C1
    End Class
"

            Await VerifyRegionsAsync(code,
                Region("span", "''' <summary> Hello VB!", autoCollapse:=True))
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.Outlining)>
        Public Async Function TestMultilineSummaryInDocumentationComment1() As Task
            Const code = "
{|span:''' $$<summary>
''' Hello
''' VB!
''' </summary>|}
Class C1
End Class
"

            Await VerifyRegionsAsync(code,
                Region("span", "''' <summary> Hello VB!", autoCollapse:=True))
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.Outlining)>
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

            Await VerifyRegionsAsync(code,
                Region("span", "''' <summary> Hello VB!", autoCollapse:=True))
        End Function

        <WorkItem(2129, "https://github.com/dotnet/roslyn/issues/2129")>
        <WpfFact, Trait(Traits.Feature, Traits.Features.Outlining)>
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

            Await VerifyRegionsAsync(code,
                Region("span", "''' <summary> Summary with SeeClass , SeeAlsoClass , Nothing , T , t , and not-supported .", autoCollapse:=True))
        End Function
    End Class
End Namespace
