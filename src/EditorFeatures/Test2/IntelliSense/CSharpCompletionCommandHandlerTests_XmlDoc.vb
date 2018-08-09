' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Threading.Tasks

Namespace Microsoft.CodeAnalysis.Editor.UnitTests.IntelliSense
    <[UseExportProvider]>
    Public Class CSharpCompletionCommandHandlerTests_XmlDoc

        Private Shared Function GetAllCompletions() As IEnumerable(Of Object())
            Return TestStateFactory.GetAllCompletions()
        End Function

        <MemberData(NameOf(GetAllCompletions))>
        <WpfTheory, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function CommitSummary(completion As Completions) As Task
            Using state = TestStateFactory.CreateCSharpTestState(completion,
                <Document><![CDATA[
class c
{
    /// $$
    void goo() { }
}
            ]]></Document>)

                state.SendInvokeCompletionList()
                Await state.AssertCompletionSession()
                state.SendTypeChars("summ")
                Await state.AssertSelectedCompletionItem(displayText:="summary")
                state.SendReturn()
                Await state.AssertNoCompletionSession()
                Await state.AssertLineTextAroundCaret("    /// summary", "")
            End Using
        End Function

        <MemberData(NameOf(GetAllCompletions))>
        <WpfTheory, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function CommitSummaryOnTab(completion As Completions) As Task

            Using state = TestStateFactory.CreateCSharpTestState(completion,
                <Document><![CDATA[
class c
{
    /// $$
    void goo() { }
}
            ]]></Document>)

                state.SendInvokeCompletionList()
                Await state.AssertCompletionSession()
                state.SendTypeChars("summ")
                Await state.AssertSelectedCompletionItem(displayText:="summary")
                state.SendTab()
                Await state.AssertNoCompletionSession()

                ' /// summary$$
                Await state.AssertLineTextAroundCaret("    /// summary", "")
            End Using
        End Function

        <MemberData(NameOf(GetAllCompletions))>
        <WpfTheory, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function CommitSummaryOnCloseAngle(completion As Completions) As Task

            Using state = TestStateFactory.CreateCSharpTestState(completion,
                <Document><![CDATA[
class c
{
    /// $$
    void goo() { }
}
            ]]></Document>)

                state.SendInvokeCompletionList()
                Await state.AssertCompletionSession()
                state.SendTypeChars("summ")
                Await state.AssertSelectedCompletionItem(displayText:="summary")
                state.SendTypeChars(">")
                Await state.AssertNoCompletionSession()

                ' /// summary>$$
                Await state.AssertLineTextAroundCaret("    /// summary>", "")
            End Using
        End Function

        <MemberData(NameOf(GetAllCompletions))>
        <WpfTheory, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function InvokeWithOpenAngleCommitSummary(completion As Completions) As Task

            Using state = TestStateFactory.CreateCSharpTestState(completion,
                <Document><![CDATA[
class c
{
    /// $$
    void goo() { }
}
            ]]></Document>)

                state.SendTypeChars("<")
                Await state.AssertCompletionSession()
                state.SendTypeChars("summ")
                Await state.AssertSelectedCompletionItem(displayText:="summary")
                state.SendReturn()
                Await state.AssertNoCompletionSession()

                ' /// <summary$$
                Await state.AssertLineTextAroundCaret("    /// <summary", "")
            End Using
        End Function

        <MemberData(NameOf(GetAllCompletions))>
        <WpfTheory, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function InvokeWithOpenAngleCommitSummaryOnTab(completion As Completions) As Task

            Using state = TestStateFactory.CreateCSharpTestState(completion,
                <Document><![CDATA[
class c
{
    /// $$
    void goo() { }
}
            ]]></Document>)

                state.SendTypeChars("<")
                Await state.AssertCompletionSession()
                state.SendTypeChars("summ")
                Await state.AssertSelectedCompletionItem(displayText:="summary")
                state.SendTab()
                Await state.AssertNoCompletionSession()

                ' /// <summary$$
                Await state.AssertLineTextAroundCaret("    /// <summary", "")
            End Using
        End Function

        <MemberData(NameOf(GetAllCompletions))>
        <WpfTheory, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function InvokeWithOpenAngleCommitSummaryOnCloseAngle(completion As Completions) As Task

            Using state = TestStateFactory.CreateCSharpTestState(completion,
                <Document><![CDATA[
class c
{
    /// $$
    void goo() { }
}
            ]]></Document>)

                state.SendTypeChars("<")
                Await state.AssertCompletionSession()
                state.SendTypeChars("summ")
                Await state.AssertSelectedCompletionItem(displayText:="summary")
                state.SendTypeChars(">")
                Await state.AssertNoCompletionSession()

                ' /// <summary>$$
                Await state.AssertLineTextAroundCaret("    /// <summary>", "")
            End Using
        End Function

        <MemberData(NameOf(GetAllCompletions))>
        <WpfTheory, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function CommitRemarksOnCloseAngle(completion As Completions) As Task

            Using state = TestStateFactory.CreateCSharpTestState(completion,
                <Document><![CDATA[
class c
{
    /// $$
    void goo() { }
}
            ]]></Document>)

                state.SendInvokeCompletionList()
                Await state.AssertCompletionSession()
                state.SendTypeChars("rema")
                Await state.AssertSelectedCompletionItem(displayText:="remarks")
                state.SendTypeChars(">")
                Await state.AssertNoCompletionSession()

                ' /// remarks>$$
                Await state.AssertLineTextAroundCaret("    /// remarks>", "")
            End Using
        End Function

        <MemberData(NameOf(GetAllCompletions))>
        <WpfTheory, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function InvokeWithOpenAngleCommitRemarksOnCloseAngle(completion As Completions) As Task

            Using state = TestStateFactory.CreateCSharpTestState(completion,
                <Document><![CDATA[
class c
{
    /// $$
    void goo() { }
}
            ]]></Document>)

                state.SendTypeChars("<")
                Await state.AssertCompletionSession()
                state.SendTypeChars("rema")
                Await state.AssertSelectedCompletionItem(displayText:="remarks")
                state.SendTypeChars(">")
                Await state.AssertNoCompletionSession()

                ' /// <remarks>$$
                Await state.AssertLineTextAroundCaret("    /// <remarks>", "")
            End Using
        End Function

        <MemberData(NameOf(GetAllCompletions))>
        <WpfTheory, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function CommitReturnsOnCloseAngle(completion As Completions) As Task

            Using state = TestStateFactory.CreateCSharpTestState(completion,
                <Document><![CDATA[
class c
{
    /// $$
    int goo() { }
}
            ]]></Document>)

                state.SendInvokeCompletionList()
                Await state.AssertCompletionSession()
                state.SendTypeChars("retur")
                Await state.AssertSelectedCompletionItem(displayText:="returns")
                state.SendTypeChars(">")
                Await state.AssertNoCompletionSession()

                ' /// returns>$$
                Await state.AssertLineTextAroundCaret("    /// returns>", "")
            End Using
        End Function

        <MemberData(NameOf(GetAllCompletions))>
        <WpfTheory, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function InvokeWithOpenAngleCommitReturnsOnCloseAngle(completion As Completions) As Task

            Using state = TestStateFactory.CreateCSharpTestState(completion,
                <Document><![CDATA[
class c
{
    /// $$
    int goo() { }
}
            ]]></Document>)

                state.SendTypeChars("<")
                Await state.AssertCompletionSession()
                state.SendTypeChars("retur")
                Await state.AssertSelectedCompletionItem(displayText:="returns")
                state.SendTypeChars(">")
                Await state.AssertNoCompletionSession()

                ' /// <returns>$$
                Await state.AssertLineTextAroundCaret("    /// <returns>", "")
            End Using
        End Function

        <MemberData(NameOf(GetAllCompletions))>
        <WpfTheory, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function CommitExampleOnCloseAngle(completion As Completions) As Task

            Using state = TestStateFactory.CreateCSharpTestState(completion,
                <Document><![CDATA[
class c
{
    /// $$
    void goo() { }
}
            ]]></Document>)

                state.SendInvokeCompletionList()
                Await state.AssertCompletionSession()
                state.SendTypeChars("examp")
                Await state.AssertSelectedCompletionItem(displayText:="example")
                state.SendTypeChars(">")
                Await state.AssertNoCompletionSession()

                ' /// example>$$
                Await state.AssertLineTextAroundCaret("    /// example>", "")
            End Using
        End Function

        <MemberData(NameOf(GetAllCompletions))>
        <WpfTheory, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function InvokeWithOpenAngleCommitExampleOnCloseAngle(completion As Completions) As Task

            Using state = TestStateFactory.CreateCSharpTestState(completion,
                <Document><![CDATA[
class c
{
    /// $$
    void goo() { }
}
            ]]></Document>)

                state.SendTypeChars("<")
                Await state.AssertCompletionSession()
                state.SendTypeChars("examp")
                Await state.AssertSelectedCompletionItem(displayText:="example")
                state.SendTypeChars(">")
                Await state.AssertNoCompletionSession()

                ' /// <example>$$
                Await state.AssertLineTextAroundCaret("    /// <example>", "")
            End Using
        End Function

        <MemberData(NameOf(GetAllCompletions))>
        <WpfTheory, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function CommitExceptionNoOpenAngle(completion As Completions) As Task

            Using state = TestStateFactory.CreateCSharpTestState(completion,
                <Document><![CDATA[
class c
{
    /// $$
    void goo() { }
}
            ]]></Document>)

                state.SendInvokeCompletionList()
                Await state.AssertCompletionSession()
                state.SendTypeChars("except")
                Await state.AssertSelectedCompletionItem(displayText:="exception")
                state.SendReturn()
                Await state.AssertNoCompletionSession()

                ' /// <exception cref="$$"
                Await state.AssertLineTextAroundCaret("    /// <exception cref=""", """")
            End Using
        End Function

        <MemberData(NameOf(GetAllCompletions))>
        <WpfTheory, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function InvokeWithOpenAngleCommitExceptionOnCloseAngle(completion As Completions) As Task

            Using state = TestStateFactory.CreateCSharpTestState(completion,
                <Document><![CDATA[
class c
{
    /// $$
    void goo() { }
}
            ]]></Document>)

                state.SendTypeChars("<")
                Await state.AssertCompletionSession()
                state.SendTypeChars("except")
                Await state.AssertSelectedCompletionItem(displayText:="exception")
                state.SendTypeChars(">")
                Await state.AssertNoCompletionSession()

                ' /// <exception cref=">$$"
                Await state.AssertLineTextAroundCaret("    /// <exception cref="">", """")
            End Using
        End Function

        <MemberData(NameOf(GetAllCompletions))>
        <WpfTheory, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function CommitCommentNoOpenAngle(completion As Completions) As Task

            Using state = TestStateFactory.CreateCSharpTestState(completion,
                <Document><![CDATA[
class c
{
    /// $$
    void goo() { }
}
            ]]></Document>)

                state.SendInvokeCompletionList()
                Await state.AssertCompletionSession()
                state.SendSelectCompletionItem("!--")
                state.SendReturn()
                Await state.AssertNoCompletionSession()

                ' /// <!--$$-->
                Await state.AssertLineTextAroundCaret("    /// <!--", "-->")
            End Using
        End Function

        <MemberData(NameOf(GetAllCompletions))>
        <WpfTheory, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function InvokeWithOpenAngleCommitCommentOnCloseAngle(completion As Completions) As Task

            Using state = TestStateFactory.CreateCSharpTestState(completion,
                <Document><![CDATA[
class c
{
    /// $$
    void goo() { }
}
            ]]></Document>)

                state.SendTypeChars("<")
                Await state.AssertCompletionSession()
                state.SendSelectCompletionItem("!--")
                state.SendTypeChars(">")
                Await state.AssertNoCompletionSession()

                ' /// <!-->$$-->
                Await state.AssertLineTextAroundCaret("    /// <!-->", "-->")
            End Using
        End Function

        <MemberData(NameOf(GetAllCompletions))>
        <WpfTheory, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function CommitCdataNoOpenAngle(completion As Completions) As Task

            Using state = TestStateFactory.CreateCSharpTestState(completion,
                <Document><![CDATA[
class c
{
    /// $$
    void goo() { }
}
            ]]></Document>)

                state.SendInvokeCompletionList()
                Await state.AssertCompletionSession()
                state.SendTypeChars("![CDAT")
                Await state.AssertSelectedCompletionItem(displayText:="![CDATA[")
                state.SendReturn()
                Await state.AssertNoCompletionSession()

                ' /// <![CDATA[$$]]>
                Await state.AssertLineTextAroundCaret("    /// <![CDATA[", "]]>")
            End Using
        End Function

        <MemberData(NameOf(GetAllCompletions))>
        <WpfTheory, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function InvokeWithOpenAngleCommitCdataOnCloseAngle(completion As Completions) As Task

            Using state = TestStateFactory.CreateCSharpTestState(completion,
                <Document><![CDATA[
class c
{
    /// $$
    void goo() { }
}
            ]]></Document>)

                state.SendTypeChars("<")
                Await state.AssertCompletionSession()
                state.SendTypeChars("![CDAT")
                Await state.AssertSelectedCompletionItem(displayText:="![CDATA[")
                state.SendTypeChars(">")
                Await state.AssertNoCompletionSession()

                ' /// <![CDATA[>$$]]>
                Await state.AssertLineTextAroundCaret("    /// <![CDATA[>", "]]>")
            End Using
        End Function

        <MemberData(NameOf(GetAllCompletions))>
        <WpfTheory, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function CommitIncludeNoOpenAngle(completion As Completions) As Task

            Using state = TestStateFactory.CreateCSharpTestState(completion,
                <Document><![CDATA[
class c
{
    /// $$
    void goo() { }
}
            ]]></Document>)

                state.SendInvokeCompletionList()
                Await state.AssertCompletionSession()
                state.SendTypeChars("inclu")
                Await state.AssertSelectedCompletionItem(displayText:="include")
                state.SendReturn()
                Await state.AssertNoCompletionSession()

                ' /// <include file='$$' path='[@name=""]'/>
                Await state.AssertLineTextAroundCaret("    /// <include file='", "' path='[@name=""""]'/>")
            End Using
        End Function

        <MemberData(NameOf(GetAllCompletions))>
        <WpfTheory, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function InvokeWithOpenAngleCommitIncludeOnCloseAngle(completion As Completions) As Task

            Using state = TestStateFactory.CreateCSharpTestState(completion,
                <Document><![CDATA[
class c
{
    /// $$
    void goo() { }
}
            ]]></Document>)

                state.SendTypeChars("<")
                Await state.AssertCompletionSession()
                state.SendTypeChars("inclu")
                Await state.AssertSelectedCompletionItem(displayText:="include")
                state.SendTypeChars(">")
                Await state.AssertNoCompletionSession()

                ' /// <include file='>$$' path='[@name=""]'/>
                Await state.AssertLineTextAroundCaret("    /// <include file='>", "' path='[@name=""""]'/>")
            End Using
        End Function

        <MemberData(NameOf(GetAllCompletions))>
        <WpfTheory, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function CommitPermissionNoOpenAngle(completion As Completions) As Task

            Using state = TestStateFactory.CreateCSharpTestState(completion,
                <Document><![CDATA[
class c
{
    /// $$
    void goo() { }
}
            ]]></Document>)

                state.SendInvokeCompletionList()
                Await state.AssertCompletionSession()
                state.SendTypeChars("permiss")
                Await state.AssertSelectedCompletionItem(displayText:="permission")
                state.SendReturn()
                Await state.AssertNoCompletionSession()

                ' /// <permission cref="$$"
                Await state.AssertLineTextAroundCaret("    /// <permission cref=""", """")
            End Using
        End Function

        <MemberData(NameOf(GetAllCompletions))>
        <WpfTheory, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function InvokeWithOpenAngleCommitPermissionOnCloseAngle(completion As Completions) As Task

            Using state = TestStateFactory.CreateCSharpTestState(completion,
                <Document><![CDATA[
class c
{
    /// $$
    void goo() { }
}
            ]]></Document>)

                state.SendTypeChars("<")
                Await state.AssertCompletionSession()
                state.SendTypeChars("permiss")
                Await state.AssertSelectedCompletionItem(displayText:="permission")
                state.SendTypeChars(">")
                Await state.AssertNoCompletionSession()

                ' /// <permission cref=">$$"
                Await state.AssertLineTextAroundCaret("    /// <permission cref="">", """")
            End Using
        End Function

        <MemberData(NameOf(GetAllCompletions))>
        <WpfTheory, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function CommitSeeNoOpenAngle(completion As Completions) As Task

            Using state = TestStateFactory.CreateCSharpTestState(completion,
                <Document><![CDATA[
class c
{
    /// $$
    void goo() { }
}
            ]]></Document>)

                state.SendInvokeCompletionList()
                Await state.AssertCompletionSession()
                state.SendTypeChars("see")
                Await state.AssertSelectedCompletionItem(displayText:="see")
                state.SendReturn()
                Await state.AssertNoCompletionSession()

                ' /// <see cref="$$"/>
                Await state.AssertLineTextAroundCaret("    /// <see cref=""", """/>")
            End Using
        End Function

        <MemberData(NameOf(GetAllCompletions))>
        <WpfTheory, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function InvokeWithOpenAngleCommitSeeOnCloseAngle(completion As Completions) As Task

            Using state = TestStateFactory.CreateCSharpTestState(completion,
                <Document><![CDATA[
class c
{
    /// $$
    void goo() { }
}
            ]]></Document>)

                state.SendTypeChars("<")
                Await state.AssertCompletionSession()
                state.SendTypeChars("see")
                Await state.AssertSelectedCompletionItem(displayText:="see")
                state.SendTypeChars(">")
                Await state.AssertNoCompletionSession()

                ' /// <see cref=">$$"/>
                Await state.AssertLineTextAroundCaret("    /// <see cref="">", """/>")
            End Using
        End Function

        <MemberData(NameOf(GetAllCompletions))>
        <WpfTheory, Trait(Traits.Feature, Traits.Features.Completion)>
        <WorkItem(22789, "https://github.com/dotnet/roslyn/issues/22789")>
        Public Async Function InvokeWithOpenAngleCommitSeeOnTab(completion As Completions) As Task

            Using state = TestStateFactory.CreateCSharpTestState(completion,
                <Document><![CDATA[
class c
{
    /// $$
    void goo() { }
}
            ]]></Document>)

                state.SendTypeChars("<")
                Await state.AssertCompletionSession()
                state.SendTypeChars("se")
                Await state.AssertSelectedCompletionItem(displayText:="see")
                state.SendTab()
                Await state.AssertNoCompletionSession()

                ' /// <see cref="$$"/>
                Await state.AssertLineTextAroundCaret("    /// <see cref=""", """/>")
            End Using
        End Function

        <MemberData(NameOf(GetAllCompletions))>
        <WpfTheory, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function InvokeWithOpenAngleCommitSeeOnSpace(completion As Completions) As Task

            Using state = TestStateFactory.CreateCSharpTestState(completion,
                <Document><![CDATA[
class c
{
    /// <summary>
    /// $$
    /// </summary>
    void goo() { }
}
            ]]></Document>)

                state.SendTypeChars("<")
                Await state.AssertCompletionSession()
                state.SendTypeChars("see")
                Await state.AssertSelectedCompletionItem(displayText:="see")
                state.SendTypeChars(" ")

                ' /// <see cref="$$"/>
                Await state.AssertLineTextAroundCaret("    /// <see cref=""", """/>")
            End Using
        End Function

        <MemberData(NameOf(GetAllCompletions))>
        <WpfTheory, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Function InvokeWithNullKeywordCommitSeeLangword(completion As Completions) As Task
            Return InvokeWithKeywordCommitSeeLangword(completion, "null")
        End Function

        <MemberData(NameOf(GetAllCompletions))>
        <WpfTheory, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Function InvokeWithStaticKeywordCommitSeeLangword(completion As Completions) As Task
            Return InvokeWithKeywordCommitSeeLangword(completion, "static")
        End Function

        <MemberData(NameOf(GetAllCompletions))>
        <WpfTheory, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Function InvokeWithVirtualKeywordCommitSeeLangword(completion As Completions) As Task
            Return InvokeWithKeywordCommitSeeLangword(completion, "virtual")
        End Function

        <MemberData(NameOf(GetAllCompletions))>
        <WpfTheory, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Function InvokeWithTrueKeywordCommitSeeLangword(completion As Completions) As Task
            Return InvokeWithKeywordCommitSeeLangword(completion, "true")
        End Function

        <MemberData(NameOf(GetAllCompletions))>
        <WpfTheory, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Function InvokeWithFalseKeywordCommitSeeLangword(completion As Completions) As Task
            Return InvokeWithKeywordCommitSeeLangword(completion, "false")
        End Function

        <MemberData(NameOf(GetAllCompletions))>
        <WpfTheory, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Function InvokeWithAbstractKeywordCommitSeeLangword(completion As Completions) As Task
            Return InvokeWithKeywordCommitSeeLangword(completion, "abstract")
        End Function

        <MemberData(NameOf(GetAllCompletions))>
        <WpfTheory, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Function InvokeWithSealedKeywordCommitSeeLangword(completion As Completions) As Task
            Return InvokeWithKeywordCommitSeeLangword(completion, "sealed")
        End Function

        <MemberData(NameOf(GetAllCompletions))>
        <WpfTheory, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Function InvokeWithAsyncKeywordCommitSeeLangword(completion As Completions) As Task
            Return InvokeWithKeywordCommitSeeLangword(completion, "async")
        End Function

        <MemberData(NameOf(GetAllCompletions))>
        <WpfTheory, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Function InvokeWithAwaitKeywordCommitSeeLangword(completion As Completions) As Task
            Return InvokeWithKeywordCommitSeeLangword(completion, "await")
        End Function

        Private Async Function InvokeWithKeywordCommitSeeLangword(completion As Completions, keyword As String) As Task
            Using state = TestStateFactory.CreateCSharpTestState(completion,
                <Document><![CDATA[
class c
{
    /// <summary>
    /// $$
    /// </summary>
    void goo() { }
}
            ]]></Document>)

                ' Omit the last letter of the keyword to make it easier to diagnose failures (inserted the wrong text,
                ' or did not insert text at all).
                state.SendTypeChars(keyword.Substring(0, keyword.Length - 1))
                state.SendInvokeCompletionList()
                state.SendCommitUniqueCompletionListItem()
                Await state.AssertNoCompletionSession()

                ' /// <see langword="keyword"/>$$
                Await state.AssertLineTextAroundCaret("    /// <see langword=""" + keyword + """/>", "")
            End Using
        End Function

        <MemberData(NameOf(GetAllCompletions))>
        <WpfTheory, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function CommitSeealsoNoOpenAngle(completion As Completions) As Task

            Using state = TestStateFactory.CreateCSharpTestState(completion,
                <Document><![CDATA[
class c
{
    /// $$
    void goo() { }
}
            ]]></Document>)

                state.SendInvokeCompletionList()
                Await state.AssertCompletionSession()
                state.SendTypeChars("seeal")
                Await state.AssertSelectedCompletionItem(displayText:="seealso")
                state.SendReturn()
                Await state.AssertNoCompletionSession()

                ' /// <seealso cref="$$"/>
                Await state.AssertLineTextAroundCaret("    /// <seealso cref=""", """/>")
            End Using
        End Function

        <MemberData(NameOf(GetAllCompletions))>
        <WpfTheory, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function InvokeWithOpenAngleCommitSeealsoOnCloseAngle(completion As Completions) As Task

            Using state = TestStateFactory.CreateCSharpTestState(completion,
                <Document><![CDATA[
class c
{
    /// $$
    void goo() { }
}
            ]]></Document>)

                state.SendTypeChars("<")
                Await state.AssertCompletionSession()
                state.SendTypeChars("seeal")
                Await state.AssertSelectedCompletionItem(displayText:="seealso")
                state.SendTypeChars(">")
                Await state.AssertNoCompletionSession()

                ' /// <seealso cref=">$$"/>
                Await state.AssertLineTextAroundCaret("    /// <seealso cref="">", """/>")
            End Using
        End Function

        <WorkItem(623219, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/623219")>
        <WorkItem(746919, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/746919")>
        <MemberData(NameOf(GetAllCompletions))>
        <WpfTheory, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function CommitParam(completion As Completions) As Task
            Using state = TestStateFactory.CreateCSharpTestState(completion,
                <Document><![CDATA[
class c<T>
{
    /// <param$$
    void goo<T>(T bar) { }
}
            ]]></Document>)

                state.SendInvokeCompletionList()
                Await state.AssertCompletionSession()
                Await state.AssertSelectedCompletionItem(displayText:="param name=""bar""")
                state.SendReturn()
                Await state.AssertNoCompletionSession()

                ' /// <param name="bar"$$
                Await state.AssertLineTextAroundCaret("    /// <param name=""bar""", "")
            End Using
        End Function

        <MemberData(NameOf(GetAllCompletions))>
        <WpfTheory, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function CommitParamNoOpenAngle(completion As Completions) As Task

            Using state = TestStateFactory.CreateCSharpTestState(completion,
                <Document><![CDATA[
class c<T>
{
    /// $$
    void goo<T>(T bar) { }
}
            ]]></Document>)

                state.SendInvokeCompletionList()
                Await state.AssertCompletionSession()
                state.SendTypeChars("param")
                Await state.AssertSelectedCompletionItem(displayText:="param name=""bar""")
                state.SendReturn()
                Await state.AssertNoCompletionSession()

                ' /// param name="bar"$$
                Await state.AssertLineTextAroundCaret("    /// param name=""bar""", "")
            End Using
        End Function

        <MemberData(NameOf(GetAllCompletions))>
        <WpfTheory, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function CommitParamNoOpenAngleOnTab(completion As Completions) As Task

            Using state = TestStateFactory.CreateCSharpTestState(completion,
                <Document><![CDATA[
class c<T>
{
    /// $$
    void goo<T>(T bar) { }
}
            ]]></Document>)

                state.SendInvokeCompletionList()
                Await state.AssertCompletionSession()
                state.SendTypeChars("param")
                Await state.AssertSelectedCompletionItem(displayText:="param name=""bar""")
                state.SendTab()
                Await state.AssertNoCompletionSession()

                ' /// param name="bar"$$
                Await state.AssertLineTextAroundCaret("    /// param name=""bar""", "")
            End Using
        End Function

        <MemberData(NameOf(GetAllCompletions))>
        <WpfTheory, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function CommitParamNoOpenAngleOnCloseAngle(completion As Completions) As Task

            Using state = TestStateFactory.CreateCSharpTestState(completion,
                <Document><![CDATA[
class c<T>
{
    /// $$
    void goo<T>(T bar) { }
}
            ]]></Document>)

                state.SendInvokeCompletionList()
                Await state.AssertCompletionSession()
                state.SendTypeChars("param")
                Await state.AssertSelectedCompletionItem(displayText:="param name=""bar""")
                state.SendTypeChars(">")
                Await state.AssertNoCompletionSession()

                ' /// param name="bar">$$
                Await state.AssertLineTextAroundCaret("    /// param name=""bar"">", "")
            End Using
        End Function

        <MemberData(NameOf(GetAllCompletions))>
        <WpfTheory, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function InvokeWithOpenAngleCommitParam(completion As Completions) As Task

            Using state = TestStateFactory.CreateCSharpTestState(completion,
                <Document><![CDATA[
class c<T>
{
    /// $$
    void goo<T>(T bar) { }
}
            ]]></Document>)

                state.SendTypeChars("<")
                Await state.AssertCompletionSession()
                state.SendTypeChars("param")
                Await state.AssertSelectedCompletionItem(displayText:="param name=""bar""")
                state.SendReturn()
                Await state.AssertNoCompletionSession()

                ' /// <param name="bar"$$
                Await state.AssertLineTextAroundCaret("    /// <param name=""bar""", "")
            End Using
        End Function

        <MemberData(NameOf(GetAllCompletions))>
        <WpfTheory, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function InvokeWithOpenAngleCommitParamOnTab(completion As Completions) As Task

            Using state = TestStateFactory.CreateCSharpTestState(completion,
                <Document><![CDATA[
class c<T>
{
    /// $$
    void goo<T>(T bar) { }
}
            ]]></Document>)

                state.SendTypeChars("<")
                Await state.AssertCompletionSession()
                state.SendTypeChars("param")
                Await state.AssertSelectedCompletionItem(displayText:="param name=""bar""")
                state.SendTab()
                Await state.AssertNoCompletionSession()

                ' /// <param name="bar"$$
                Await state.AssertLineTextAroundCaret("    /// <param name=""bar""", "")
            End Using
        End Function

        <MemberData(NameOf(GetAllCompletions))>
        <WpfTheory, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function InvokeWithOpenAngleCommitParamOnCloseAngle(completion As Completions) As Task

            Using state = TestStateFactory.CreateCSharpTestState(completion,
                <Document><![CDATA[
class c<T>
{
    /// $$
    void goo<T>(T bar) { }
}
            ]]></Document>)

                state.SendTypeChars("<")
                Await state.AssertCompletionSession()
                state.SendTypeChars("param")
                Await state.AssertSelectedCompletionItem(displayText:="param name=""bar""")
                state.SendTypeChars(">")
                Await state.AssertNoCompletionSession()

                ' /// <param name="bar">$$
                Await state.AssertLineTextAroundCaret("    /// <param name=""bar"">", "")
            End Using
        End Function

        <MemberData(NameOf(GetAllCompletions))>
        <WpfTheory, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function CommitTypeparamNoOpenAngle(completion As Completions) As Task

            Using state = TestStateFactory.CreateCSharpTestState(completion,
                <Document><![CDATA[
class c<T>
{
    /// $$
    void goo<T>(T bar) { }
}
            ]]></Document>)

                state.SendInvokeCompletionList()
                Await state.AssertCompletionSession()
                state.SendTypeChars("typepara")
                Await state.AssertSelectedCompletionItem(displayText:="typeparam name=""T""")
                state.SendReturn()
                Await state.AssertNoCompletionSession()

                ' /// typeparam name="T"$$
                Await state.AssertLineTextAroundCaret("    /// typeparam name=""T""", "")
            End Using
        End Function

        <MemberData(NameOf(GetAllCompletions))>
        <WpfTheory, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function CommitTypeparamNoOpenAngleOnTab(completion As Completions) As Task

            Using state = TestStateFactory.CreateCSharpTestState(completion,
                <Document><![CDATA[
class c<T>
{
    /// $$
    void goo<T>(T bar) { }
}
            ]]></Document>)

                state.SendInvokeCompletionList()
                Await state.AssertCompletionSession()
                state.SendTypeChars("typepara")
                Await state.AssertSelectedCompletionItem(displayText:="typeparam name=""T""")
                state.SendTab()
                Await state.AssertNoCompletionSession()

                ' /// typeparam name="T"$$
                Await state.AssertLineTextAroundCaret("    /// typeparam name=""T""", "")
            End Using
        End Function

        <MemberData(NameOf(GetAllCompletions))>
        <WpfTheory, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function CommitTypeparamNoOpenAngleOnCloseAngle(completion As Completions) As Task

            Using state = TestStateFactory.CreateCSharpTestState(completion,
                <Document><![CDATA[
class c<T>
{
    /// $$
    void goo<T>(T bar) { }
}
            ]]></Document>)

                state.SendInvokeCompletionList()
                Await state.AssertCompletionSession()
                state.SendTypeChars("typepara")
                Await state.AssertSelectedCompletionItem(displayText:="typeparam name=""T""")
                state.SendTypeChars(">")
                Await state.AssertNoCompletionSession()

                ' /// typeparam name="T">$$
                Await state.AssertLineTextAroundCaret("    /// typeparam name=""T"">", "")
            End Using
        End Function

        <MemberData(NameOf(GetAllCompletions))>
        <WpfTheory, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function InvokeWithOpenAngleCommitTypeparam(completion As Completions) As Task

            Using state = TestStateFactory.CreateCSharpTestState(completion,
                <Document><![CDATA[
class c<T>
{
    /// $$
    void goo<T>(T bar) { }
}
            ]]></Document>)

                state.SendTypeChars("<")
                Await state.AssertCompletionSession()
                state.SendTypeChars("typepara")
                Await state.AssertSelectedCompletionItem(displayText:="typeparam name=""T""")
                state.SendReturn()
                Await state.AssertNoCompletionSession()

                ' /// <typeparam name="T"$$
                Await state.AssertLineTextAroundCaret("    /// <typeparam name=""T""", "")
            End Using
        End Function

        <MemberData(NameOf(GetAllCompletions))>
        <WpfTheory, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function InvokeWithOpenAngleCommitTypeparamOnTab(completion As Completions) As Task

            Using state = TestStateFactory.CreateCSharpTestState(completion,
                <Document><![CDATA[
class c<T>
{
    /// $$
    void goo<T>(T bar) { }
}
            ]]></Document>)

                state.SendTypeChars("<")
                Await state.AssertCompletionSession()
                state.SendTypeChars("typepara")
                Await state.AssertSelectedCompletionItem(displayText:="typeparam name=""T""")
                state.SendTab()
                Await state.AssertNoCompletionSession()

                ' /// <typeparam name="T"$$
                Await state.AssertLineTextAroundCaret("    /// <typeparam name=""T""", "")
            End Using
        End Function

        <MemberData(NameOf(GetAllCompletions))>
        <WpfTheory, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function InvokeWithOpenAngleCommitTypeparamOnCloseAngle(completion As Completions) As Task

            Using state = TestStateFactory.CreateCSharpTestState(completion,
                <Document><![CDATA[
class c<T>
{
    /// $$
    void goo<T>(T bar) { }
}
            ]]></Document>)

                state.SendTypeChars("<")
                Await state.AssertCompletionSession()
                state.SendTypeChars("typepara")
                Await state.AssertSelectedCompletionItem(displayText:="typeparam name=""T""")
                state.SendTypeChars(">")
                Await state.AssertNoCompletionSession()

                ' /// <typeparam name="T">$$
                Await state.AssertLineTextAroundCaret("    /// <typeparam name=""T"">", "")
            End Using
        End Function

        <MemberData(NameOf(GetAllCompletions))>
        <WpfTheory, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function CommitList(completion As Completions) As Task

            Using state = TestStateFactory.CreateCSharpTestState(completion,
                <Document><![CDATA[
class c<T>
{
    /// <summary>
    /// $$
    /// </summary>
    void goo<T>(T bar) { }
}
            ]]></Document>)

                state.SendInvokeCompletionList()
                Await state.AssertCompletionSession()
                state.SendTypeChars("lis")
                Await state.AssertSelectedCompletionItem(displayText:="list")
                state.SendReturn()
                Await state.AssertNoCompletionSession()

                ' /// <list type="$$"
                Await state.AssertLineTextAroundCaret("    /// <list type=""", """")
            End Using
        End Function

        <MemberData(NameOf(GetAllCompletions))>
        <WpfTheory, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function CommitListOnCloseAngle(completion As Completions) As Task

            Using state = TestStateFactory.CreateCSharpTestState(completion,
                <Document><![CDATA[
class c<T>
{
    /// <summary>
    /// $$
    /// </summary>
    void goo<T>(T bar) { }
}
            ]]></Document>)

                state.SendInvokeCompletionList()
                Await state.AssertCompletionSession()
                state.SendTypeChars("lis")
                Await state.AssertSelectedCompletionItem(displayText:="list")
                state.SendTypeChars(">")
                Await state.AssertNoCompletionSession()

                ' /// <list type=">$$"
                Await state.AssertLineTextAroundCaret("    /// <list type="">", """")
            End Using
        End Function

        <MemberData(NameOf(GetAllCompletions))>
        <WpfTheory, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function TestTagCompletion1(completion As Completions) As Task

            Using state = TestStateFactory.CreateCSharpTestState(completion,
                <Document><![CDATA[
class c<T>
{
    /// <$$
    /// </summary>
    void goo<T>(T bar) { }
}
            ]]></Document>)

                state.SendInvokeCompletionList()
                Await state.AssertCompletionSession()
                state.SendTypeChars("summa")
                Await state.AssertSelectedCompletionItem(displayText:="summary")
                state.SendTypeChars(">")
                Await state.AssertNoCompletionSession()

                ' /// <summary>$$
                Await state.AssertLineTextAroundCaret("    /// <summary>", "")
            End Using
        End Function

        <MemberData(NameOf(GetAllCompletions))>
        <WpfTheory, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function TestTagCompletion2(completion As Completions) As Task

            Using state = TestStateFactory.CreateCSharpTestState(completion,
                <Document><![CDATA[
class c<T>
{
    /// <$$
    /// <remarks></remarks>
    /// </summary>
    void goo<T>(T bar) { }
}
            ]]></Document>)

                state.SendInvokeCompletionList()
                Await state.AssertCompletionSession()
                state.SendTypeChars("summa")
                Await state.AssertSelectedCompletionItem(displayText:="summary")
                state.SendTypeChars(">")
                Await state.AssertNoCompletionSession()

                ' /// <summary>$$
                Await state.AssertLineTextAroundCaret("    /// <summary>", "")
            End Using
        End Function

        <MemberData(NameOf(GetAllCompletions))>
        <WpfTheory, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function TestTagCompletion3(completion As Completions) As Task

            Using state = TestStateFactory.CreateCSharpTestState(completion,
                <Document><![CDATA[
class c<T>
{
    /// <$$
    /// <remarks>
    /// </summary>
    void goo<T>(T bar) { }
}
            ]]></Document>)

                state.SendInvokeCompletionList()
                Await state.AssertCompletionSession()
                state.SendTypeChars("summa")
                Await state.AssertSelectedCompletionItem(displayText:="summary")
                state.SendTypeChars(">")
                Await state.AssertNoCompletionSession()

                ' /// <summary>$$
                Await state.AssertLineTextAroundCaret("    /// <summary>", "")
            End Using
        End Function

        <WorkItem(638653, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/638653")>
        <MemberData(NameOf(GetAllCompletions))>
        <WpfTheory(Skip:="https://github.com/dotnet/roslyn/issues/21481"), Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function AllowTypingDoubleQuote(completion As Completions) As Task
            Using state = TestStateFactory.CreateCSharpTestState(completion,
                <Document><![CDATA[
class c
{
    /// <param$$
    void goo<T>(T bar) { }
}
            ]]></Document>)

                state.SendInvokeCompletionList()
                Await state.AssertCompletionSession()
                Await state.AssertSelectedCompletionItem(displayText:="param name=""bar""")
                state.SendTypeChars(" name=""")

                ' /// <param name="$$
                Await state.AssertLineTextAroundCaret("    /// <param name=""", "")

                ' Because the item contains a double quote, the completion list should still be present with the same selection
                Await state.AssertCompletionSession()
                Await state.AssertSelectedCompletionItem(displayText:="param name=""bar""")
            End Using
        End Function

        <WorkItem(638653, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/638653")>
        <MemberData(NameOf(GetAllCompletions))>
        <WpfTheory, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function AllowTypingSpace(completion As Completions) As Task
            Using state = TestStateFactory.CreateCSharpTestState(completion,
                <Document><![CDATA[
class c
{
    /// <param$$
    void goo<T>(T bar) { }
}
            ]]></Document>)

                state.SendInvokeCompletionList()
                Await state.AssertCompletionSession()
                Await state.AssertSelectedCompletionItem(displayText:="param name=""bar""")
                state.SendTypeChars(" ")

                ' /// <param $$
                Await state.AssertLineTextAroundCaret("    /// <param ", "")

                ' Because the item contains a space, the completion list should still be present with the same selection
                Await state.AssertCompletionSession()
                Await state.AssertSelectedCompletionItem(displayText:="param name=""bar""")
            End Using
        End Function
    End Class
End Namespace
