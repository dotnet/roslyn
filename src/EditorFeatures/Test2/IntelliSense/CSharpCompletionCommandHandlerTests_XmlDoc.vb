' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Namespace Microsoft.CodeAnalysis.Editor.UnitTests.IntelliSense
    <[UseExportProvider]>
    Public Class CSharpCompletionCommandHandlerTests_XmlDoc

        <WpfTheory, CombinatorialData, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function CommitSummary(showCompletionInArgumentLists As Boolean) As Task
            Using state = TestStateFactory.CreateCSharpTestState(
                <Document><![CDATA[
class c
{
    /// $$
    void goo() { }
}
            ]]></Document>, showCompletionInArgumentLists:=showCompletionInArgumentLists)

                state.SendInvokeCompletionList()
                Await state.AssertCompletionSessionAsync()
                state.SendTypeChars("summ")
                Await state.AssertSelectedCompletionItemAsync(displayText:="summary")
                state.SendReturn()
                Await state.AssertNoCompletionSessionAsync()
                Await state.AssertLineTextAroundCaretAsync("    /// summary", "")
            End Using
        End Function

        <WpfTheory, CombinatorialData, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function CommitSummaryOnTab(showCompletionInArgumentLists As Boolean) As Task

            Using state = TestStateFactory.CreateCSharpTestState(
                <Document><![CDATA[
class c
{
    /// $$
    void goo() { }
}
            ]]></Document>, showCompletionInArgumentLists:=showCompletionInArgumentLists)

                state.SendInvokeCompletionList()
                Await state.AssertCompletionSessionAsync()
                state.SendTypeChars("summ")
                Await state.AssertSelectedCompletionItemAsync(displayText:="summary")
                state.SendTab()
                Await state.AssertNoCompletionSessionAsync()

                ' /// summary$$
                Await state.AssertLineTextAroundCaretAsync("    /// summary", "")
            End Using
        End Function

        <WpfTheory, CombinatorialData, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function CommitSummaryOnCloseAngle(showCompletionInArgumentLists As Boolean) As Task

            Using state = TestStateFactory.CreateCSharpTestState(
                <Document><![CDATA[
class c
{
    /// $$
    void goo() { }
}
            ]]></Document>, showCompletionInArgumentLists:=showCompletionInArgumentLists)

                state.SendInvokeCompletionList()
                Await state.AssertCompletionSessionAsync()
                state.SendTypeChars("summ")
                Await state.AssertSelectedCompletionItemAsync(displayText:="summary")
                state.SendTypeChars(">")
                Await state.AssertNoCompletionSessionAsync()

                ' /// summary>$$
                Await state.AssertLineTextAroundCaretAsync("    /// summary>", "")
            End Using
        End Function

        <WpfTheory, CombinatorialData, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function InvokeWithOpenAngleCommitSummary(showCompletionInArgumentLists As Boolean) As Task

            Using state = TestStateFactory.CreateCSharpTestState(
                <Document><![CDATA[
class c
{
    /// $$
    void goo() { }
}
            ]]></Document>, showCompletionInArgumentLists:=showCompletionInArgumentLists)

                state.SendTypeChars("<")
                Await state.AssertCompletionSessionAsync()
                state.SendTypeChars("summ")
                Await state.AssertSelectedCompletionItemAsync(displayText:="summary")
                state.SendReturn()
                Await state.AssertNoCompletionSessionAsync()

                ' /// <summary$$
                Await state.AssertLineTextAroundCaretAsync("    /// <summary", "")
            End Using
        End Function

        <WpfTheory, CombinatorialData, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function InvokeWithOpenAngleCommitSummaryOnTab(showCompletionInArgumentLists As Boolean) As Task

            Using state = TestStateFactory.CreateCSharpTestState(
                <Document><![CDATA[
class c
{
    /// $$
    void goo() { }
}
            ]]></Document>, showCompletionInArgumentLists:=showCompletionInArgumentLists)

                state.SendTypeChars("<")
                Await state.AssertCompletionSessionAsync()
                state.SendTypeChars("summ")
                Await state.AssertSelectedCompletionItemAsync(displayText:="summary")
                state.SendTab()
                Await state.AssertNoCompletionSessionAsync()

                ' /// <summary$$
                Await state.AssertLineTextAroundCaretAsync("    /// <summary", "")
            End Using
        End Function

        <WpfTheory, CombinatorialData, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function InvokeWithOpenAngleCommitSummaryOnCloseAngle(showCompletionInArgumentLists As Boolean) As Task

            Using state = TestStateFactory.CreateCSharpTestState(
                <Document><![CDATA[
class c
{
    /// $$
    void goo() { }
}
            ]]></Document>, showCompletionInArgumentLists:=showCompletionInArgumentLists)

                state.SendTypeChars("<")
                Await state.AssertCompletionSessionAsync()
                state.SendTypeChars("summ")
                Await state.AssertSelectedCompletionItemAsync(displayText:="summary")
                state.SendTypeChars(">")
                Await state.AssertNoCompletionSessionAsync()

                ' /// <summary>$$
                Await state.AssertLineTextAroundCaretAsync("    /// <summary>", "")
            End Using
        End Function

        <WpfTheory, CombinatorialData, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function CommitRemarksOnCloseAngle(showCompletionInArgumentLists As Boolean) As Task

            Using state = TestStateFactory.CreateCSharpTestState(
                <Document><![CDATA[
class c
{
    /// $$
    void goo() { }
}
            ]]></Document>, showCompletionInArgumentLists:=showCompletionInArgumentLists)

                state.SendInvokeCompletionList()
                Await state.AssertCompletionSessionAsync()
                state.SendTypeChars("rema")
                Await state.AssertSelectedCompletionItemAsync(displayText:="remarks")
                state.SendTypeChars(">")
                Await state.AssertNoCompletionSessionAsync()

                ' /// remarks>$$
                Await state.AssertLineTextAroundCaretAsync("    /// remarks>", "")
            End Using
        End Function

        <WpfTheory, CombinatorialData, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function InvokeWithOpenAngleCommitRemarksOnCloseAngle(showCompletionInArgumentLists As Boolean) As Task

            Using state = TestStateFactory.CreateCSharpTestState(
                <Document><![CDATA[
class c
{
    /// $$
    void goo() { }
}
            ]]></Document>, showCompletionInArgumentLists:=showCompletionInArgumentLists)

                state.SendTypeChars("<")
                Await state.AssertCompletionSessionAsync()
                state.SendTypeChars("rema")
                Await state.AssertSelectedCompletionItemAsync(displayText:="remarks")
                state.SendTypeChars(">")
                Await state.AssertNoCompletionSessionAsync()

                ' /// <remarks>$$
                Await state.AssertLineTextAroundCaretAsync("    /// <remarks>", "")
            End Using
        End Function

        <WpfTheory, CombinatorialData, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function CommitReturnsOnCloseAngle(showCompletionInArgumentLists As Boolean) As Task

            Using state = TestStateFactory.CreateCSharpTestState(
                <Document><![CDATA[
class c
{
    /// $$
    int goo() { }
}
            ]]></Document>, showCompletionInArgumentLists:=showCompletionInArgumentLists)

                state.SendInvokeCompletionList()
                Await state.AssertCompletionSessionAsync()
                state.SendTypeChars("retur")
                Await state.AssertSelectedCompletionItemAsync(displayText:="returns")
                state.SendTypeChars(">")
                Await state.AssertNoCompletionSessionAsync()

                ' /// returns>$$
                Await state.AssertLineTextAroundCaretAsync("    /// returns>", "")
            End Using
        End Function

        <WpfTheory, CombinatorialData, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function InvokeWithOpenAngleCommitReturnsOnCloseAngle(showCompletionInArgumentLists As Boolean) As Task

            Using state = TestStateFactory.CreateCSharpTestState(
                <Document><![CDATA[
class c
{
    /// $$
    int goo() { }
}
            ]]></Document>, showCompletionInArgumentLists:=showCompletionInArgumentLists)

                state.SendTypeChars("<")
                Await state.AssertCompletionSessionAsync()
                state.SendTypeChars("retur")
                Await state.AssertSelectedCompletionItemAsync(displayText:="returns")
                state.SendTypeChars(">")
                Await state.AssertNoCompletionSessionAsync()

                ' /// <returns>$$
                Await state.AssertLineTextAroundCaretAsync("    /// <returns>", "")
            End Using
        End Function

        <WpfTheory, CombinatorialData, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function CommitExampleOnCloseAngle(showCompletionInArgumentLists As Boolean) As Task

            Using state = TestStateFactory.CreateCSharpTestState(
                <Document><![CDATA[
class c
{
    /// $$
    void goo() { }
}
            ]]></Document>, showCompletionInArgumentLists:=showCompletionInArgumentLists)

                state.SendInvokeCompletionList()
                Await state.AssertCompletionSessionAsync()
                state.SendTypeChars("examp")
                Await state.AssertSelectedCompletionItemAsync(displayText:="example")
                state.SendTypeChars(">")
                Await state.AssertNoCompletionSessionAsync()

                ' /// example>$$
                Await state.AssertLineTextAroundCaretAsync("    /// example>", "")
            End Using
        End Function

        <WpfTheory, CombinatorialData, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function InvokeWithOpenAngleCommitExampleOnCloseAngle(showCompletionInArgumentLists As Boolean) As Task

            Using state = TestStateFactory.CreateCSharpTestState(
                <Document><![CDATA[
class c
{
    /// $$
    void goo() { }
}
            ]]></Document>, showCompletionInArgumentLists:=showCompletionInArgumentLists)

                state.SendTypeChars("<")
                Await state.AssertCompletionSessionAsync()
                state.SendTypeChars("examp")
                Await state.AssertSelectedCompletionItemAsync(displayText:="example")
                state.SendTypeChars(">")
                Await state.AssertNoCompletionSessionAsync()

                ' /// <example>$$
                Await state.AssertLineTextAroundCaretAsync("    /// <example>", "")
            End Using
        End Function

        <WpfTheory, CombinatorialData, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function CommitExceptionNoOpenAngle(showCompletionInArgumentLists As Boolean) As Task

            Using state = TestStateFactory.CreateCSharpTestState(
                <Document><![CDATA[
class c
{
    /// $$
    void goo() { }
}
            ]]></Document>, showCompletionInArgumentLists:=showCompletionInArgumentLists)

                state.SendInvokeCompletionList()
                Await state.AssertCompletionSessionAsync()
                state.SendTypeChars("except")
                Await state.AssertSelectedCompletionItemAsync(displayText:="exception")
                state.SendReturn()
                Await state.AssertNoCompletionSessionAsync()

                ' /// <exception cref="$$"
                Await state.AssertLineTextAroundCaretAsync("    /// <exception cref=""", """")
            End Using
        End Function

        <WpfTheory, CombinatorialData, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function InvokeWithOpenAngleCommitExceptionOnCloseAngle(showCompletionInArgumentLists As Boolean) As Task

            Using state = TestStateFactory.CreateCSharpTestState(
                <Document><![CDATA[
class c
{
    /// $$
    void goo() { }
}
            ]]></Document>, showCompletionInArgumentLists:=showCompletionInArgumentLists)

                state.SendTypeChars("<")
                Await state.AssertCompletionSessionAsync()
                state.SendTypeChars("except")
                Await state.AssertSelectedCompletionItemAsync(displayText:="exception")
                state.SendTypeChars(">")
                Await state.AssertNoCompletionSessionAsync()

                ' /// <exception cref=">$$"
                Await state.AssertLineTextAroundCaretAsync("    /// <exception cref="">", """")
            End Using
        End Function

        <WpfTheory, CombinatorialData, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function CommitCommentNoOpenAngle(showCompletionInArgumentLists As Boolean) As Task

            Using state = TestStateFactory.CreateCSharpTestState(
                <Document><![CDATA[
class c
{
    /// $$
    void goo() { }
}
            ]]></Document>, showCompletionInArgumentLists:=showCompletionInArgumentLists)

                state.SendInvokeCompletionList()
                Await state.AssertCompletionSessionAsync()
                state.SendSelectCompletionItem("!--")
                state.SendReturn()
                Await state.AssertNoCompletionSessionAsync()

                ' /// <!--$$-->
                Await state.AssertLineTextAroundCaretAsync("    /// <!--", "-->")
            End Using
        End Function

        <WpfTheory, CombinatorialData, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function InvokeWithOpenAngleCommitCommentOnCloseAngle(showCompletionInArgumentLists As Boolean) As Task
            Using state = TestStateFactory.CreateCSharpTestState(
                <Document><![CDATA[
class c
{
    /// $$
    void goo() { }
}
            ]]></Document>, showCompletionInArgumentLists:=showCompletionInArgumentLists)

                state.SendTypeChars("<")
                Await state.AssertCompletionSessionAsync()
                state.SendSelectCompletionItem("!--")
                state.SendTypeChars(">")
                Await state.AssertNoCompletionSessionAsync()

                ' /// <!-->$$-->
                Await state.AssertLineTextAroundCaretAsync("    /// <!-->", "-->")
            End Using
        End Function

        <WpfTheory, CombinatorialData, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function CommitCdataNoOpenAngle(showCompletionInArgumentLists As Boolean) As Task
            Using state = TestStateFactory.CreateCSharpTestState(
                <Document><![CDATA[
class c
{
    /// $$
    void goo() { }
}
            ]]></Document>, showCompletionInArgumentLists:=showCompletionInArgumentLists)

                state.SendInvokeCompletionList()
                Await state.AssertCompletionSessionAsync()
                state.SendTypeChars("![CDAT")
                Await state.AssertSelectedCompletionItemAsync(displayText:="![CDATA[")
                state.SendReturn()
                Await state.AssertNoCompletionSessionAsync()

                ' /// <![CDATA[$$]]>
                Await state.AssertLineTextAroundCaretAsync("    /// <![CDATA[", "]]>")
            End Using
        End Function

        <WpfTheory, CombinatorialData, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function InvokeWithOpenAngleCommitCdataOnCloseAngle(showCompletionInArgumentLists As Boolean) As Task
            Using state = TestStateFactory.CreateCSharpTestState(
                <Document><![CDATA[
class c
{
    /// $$
    void goo() { }
}
            ]]></Document>, showCompletionInArgumentLists:=showCompletionInArgumentLists)

                state.SendTypeChars("<")
                Await state.AssertCompletionSessionAsync()
                state.SendTypeChars("![CDAT")
                Await state.AssertSelectedCompletionItemAsync(displayText:="![CDATA[")
                state.SendTypeChars(">")
                Await state.AssertNoCompletionSessionAsync()

                ' /// <![CDATA[>$$]]>
                Await state.AssertLineTextAroundCaretAsync("    /// <![CDATA[>", "]]>")
            End Using
        End Function

        <WpfTheory, CombinatorialData, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function CommitIncludeNoOpenAngle(showCompletionInArgumentLists As Boolean) As Task

            Using state = TestStateFactory.CreateCSharpTestState(
                <Document><![CDATA[
class c
{
    /// $$
    void goo() { }
}
            ]]></Document>, showCompletionInArgumentLists:=showCompletionInArgumentLists)

                state.SendInvokeCompletionList()
                Await state.AssertCompletionSessionAsync()
                state.SendTypeChars("inclu")
                Await state.AssertSelectedCompletionItemAsync(displayText:="include")
                state.SendReturn()
                Await state.AssertNoCompletionSessionAsync()

                ' /// <include file='$$' path='[@name=""]'/>
                Await state.AssertLineTextAroundCaretAsync("    /// <include file='", "' path='[@name=""""]'/>")
            End Using
        End Function

        <WpfTheory, CombinatorialData, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function InvokeWithOpenAngleCommitIncludeOnCloseAngle(showCompletionInArgumentLists As Boolean) As Task

            Using state = TestStateFactory.CreateCSharpTestState(
                <Document><![CDATA[
class c
{
    /// $$
    void goo() { }
}
            ]]></Document>, showCompletionInArgumentLists:=showCompletionInArgumentLists)

                state.SendTypeChars("<")
                Await state.AssertCompletionSessionAsync()
                state.SendTypeChars("inclu")
                Await state.AssertSelectedCompletionItemAsync(displayText:="include")
                state.SendTypeChars(">")
                Await state.AssertNoCompletionSessionAsync()

                ' /// <include file='>$$' path='[@name=""]'/>
                Await state.AssertLineTextAroundCaretAsync("    /// <include file='>", "' path='[@name=""""]'/>")
            End Using
        End Function

        <WpfTheory, CombinatorialData, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function CommitPermissionNoOpenAngle(showCompletionInArgumentLists As Boolean) As Task

            Using state = TestStateFactory.CreateCSharpTestState(
                <Document><![CDATA[
class c
{
    /// $$
    void goo() { }
}
            ]]></Document>, showCompletionInArgumentLists:=showCompletionInArgumentLists)

                state.SendInvokeCompletionList()
                Await state.AssertCompletionSessionAsync()
                state.SendTypeChars("permiss")
                Await state.AssertSelectedCompletionItemAsync(displayText:="permission")
                state.SendReturn()
                Await state.AssertNoCompletionSessionAsync()

                ' /// <permission cref="$$"
                Await state.AssertLineTextAroundCaretAsync("    /// <permission cref=""", """")
            End Using
        End Function

        <WpfTheory, CombinatorialData, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function InvokeWithOpenAngleCommitPermissionOnCloseAngle(showCompletionInArgumentLists As Boolean) As Task

            Using state = TestStateFactory.CreateCSharpTestState(
                <Document><![CDATA[
class c
{
    /// $$
    void goo() { }
}
            ]]></Document>, showCompletionInArgumentLists:=showCompletionInArgumentLists)

                state.SendTypeChars("<")
                Await state.AssertCompletionSessionAsync()
                state.SendTypeChars("permiss")
                Await state.AssertSelectedCompletionItemAsync(displayText:="permission")
                state.SendTypeChars(">")
                Await state.AssertNoCompletionSessionAsync()

                ' /// <permission cref=">$$"
                Await state.AssertLineTextAroundCaretAsync("    /// <permission cref="">", """")
            End Using
        End Function

        <WpfTheory, CombinatorialData, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function CommitSeeNoOpenAngle(showCompletionInArgumentLists As Boolean) As Task

            Using state = TestStateFactory.CreateCSharpTestState(
                <Document><![CDATA[
class c
{
    /// $$
    void goo() { }
}
            ]]></Document>, showCompletionInArgumentLists:=showCompletionInArgumentLists)

                state.SendInvokeCompletionList()
                Await state.AssertCompletionSessionAsync()
                state.SendTypeChars("see")
                Await state.AssertSelectedCompletionItemAsync(displayText:="see")
                state.SendReturn()
                Await state.AssertNoCompletionSessionAsync()

                ' /// <see cref="$$"/>
                Await state.AssertLineTextAroundCaretAsync("    /// <see cref=""", """/>")
            End Using
        End Function

        <WpfTheory, CombinatorialData, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function InvokeWithOpenAngleCommitSeeOnCloseAngle(showCompletionInArgumentLists As Boolean) As Task

            Using state = TestStateFactory.CreateCSharpTestState(
                <Document><![CDATA[
class c
{
    /// $$
    void goo() { }
}
            ]]></Document>, showCompletionInArgumentLists:=showCompletionInArgumentLists)

                state.SendTypeChars("<")
                Await state.AssertCompletionSessionAsync()
                state.SendTypeChars("see")
                Await state.AssertSelectedCompletionItemAsync(displayText:="see")
                state.SendTypeChars(">")
                Await state.AssertNoCompletionSessionAsync()

                ' /// <see cref=">$$"/>
                Await state.AssertLineTextAroundCaretAsync("    /// <see cref="">", """/>")
            End Using
        End Function

        <WpfTheory, CombinatorialData, Trait(Traits.Feature, Traits.Features.Completion)>
        <WorkItem(22789, "https://github.com/dotnet/roslyn/issues/22789")>
        Public Async Function InvokeWithOpenAngleCommitSeeOnTab(showCompletionInArgumentLists As Boolean) As Task

            Using state = TestStateFactory.CreateCSharpTestState(
                <Document><![CDATA[
class c
{
    /// $$
    void goo() { }
}
            ]]></Document>, showCompletionInArgumentLists:=showCompletionInArgumentLists)

                state.SendTypeChars("<")
                Await state.AssertCompletionSessionAsync()
                state.SendTypeChars("se")
                Await state.AssertSelectedCompletionItemAsync(displayText:="see")
                state.SendTab()
                Await state.AssertNoCompletionSessionAsync()

                ' /// <see cref="$$"/>
                Await state.AssertLineTextAroundCaretAsync("    /// <see cref=""", """/>")
            End Using
        End Function

        <WpfTheory, CombinatorialData, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function InvokeWithOpenAngleCommitSeeOnSpace(showCompletionInArgumentLists As Boolean) As Task

            Using state = TestStateFactory.CreateCSharpTestState(
                <Document><![CDATA[
class c
{
    /// <summary>
    /// $$
    /// </summary>
    void goo() { }
}
            ]]></Document>, showCompletionInArgumentLists:=showCompletionInArgumentLists)

                state.SendTypeChars("<")
                Await state.AssertCompletionSessionAsync()
                state.SendTypeChars("see")
                Await state.AssertSelectedCompletionItemAsync(displayText:="see")
                state.SendTypeChars(" ")

                ' /// <see cref="$$"/>
                Await state.AssertLineTextAroundCaretAsync("    /// <see cref=""", """/>")
            End Using
        End Function

        <WpfTheory, CombinatorialData, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Function InvokeWithNullKeywordCommitSeeLangword(showCompletionInArgumentLists As Boolean) As Task
            Return InvokeWithKeywordCommitSeeLangwordAsync("null", showCompletionInArgumentLists)
        End Function

        <WpfTheory, CombinatorialData, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Function InvokeWithStaticKeywordCommitSeeLangword(showCompletionInArgumentLists As Boolean) As Task
            Return InvokeWithKeywordCommitSeeLangwordAsync("static", showCompletionInArgumentLists)
        End Function

        <WpfTheory, CombinatorialData, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Function InvokeWithVirtualKeywordCommitSeeLangword(showCompletionInArgumentLists As Boolean) As Task
            Return InvokeWithKeywordCommitSeeLangwordAsync("virtual", showCompletionInArgumentLists)
        End Function

        <WpfTheory, CombinatorialData, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Function InvokeWithTrueKeywordCommitSeeLangword(showCompletionInArgumentLists As Boolean) As Task
            Return InvokeWithKeywordCommitSeeLangwordAsync("true", showCompletionInArgumentLists)
        End Function

        <WpfTheory, CombinatorialData, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Function InvokeWithFalseKeywordCommitSeeLangword(showCompletionInArgumentLists As Boolean) As Task
            Return InvokeWithKeywordCommitSeeLangwordAsync("false", showCompletionInArgumentLists)
        End Function

        <WpfTheory, CombinatorialData, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Function InvokeWithAbstractKeywordCommitSeeLangword(showCompletionInArgumentLists As Boolean) As Task
            Return InvokeWithKeywordCommitSeeLangwordAsync("abstract", showCompletionInArgumentLists)
        End Function

        <WpfTheory, CombinatorialData, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Function InvokeWithSealedKeywordCommitSeeLangword(showCompletionInArgumentLists As Boolean) As Task
            Return InvokeWithKeywordCommitSeeLangwordAsync("sealed", showCompletionInArgumentLists)
        End Function

        <WpfTheory, CombinatorialData, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Function InvokeWithAsyncKeywordCommitSeeLangword(showCompletionInArgumentLists As Boolean) As Task
            Return InvokeWithKeywordCommitSeeLangwordAsync("async", showCompletionInArgumentLists)
        End Function

        <WpfTheory, CombinatorialData, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Function InvokeWithAwaitKeywordCommitSeeLangword(showCompletionInArgumentLists As Boolean) As Task
            Return InvokeWithKeywordCommitSeeLangwordAsync("await", showCompletionInArgumentLists)
        End Function

        Private Shared Async Function InvokeWithKeywordCommitSeeLangwordAsync(keyword As String, showCompletionInArgumentLists As Boolean) As Task
            Using state = TestStateFactory.CreateCSharpTestState(
                <Document><![CDATA[
class c
{
    /// <summary>
    /// $$
    /// </summary>
    void goo() { }
}
            ]]></Document>, showCompletionInArgumentLists:=showCompletionInArgumentLists)

                ' Omit the last letter of the keyword to make it easier to diagnose failures (inserted the wrong text,
                ' or did not insert text at all).
                state.SendTypeChars(keyword.Substring(0, keyword.Length - 1))
                state.SendInvokeCompletionList()
                Await state.SendCommitUniqueCompletionListItemAsync()
                Await state.AssertNoCompletionSessionAsync()

                ' /// <see langword="keyword"/>$$
                Await state.AssertLineTextAroundCaretAsync("    /// <see langword=""" + keyword + """/>", "")
            End Using
        End Function

        <WpfTheory, CombinatorialData, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function CommitSeealsoNoOpenAngle(showCompletionInArgumentLists As Boolean) As Task

            Using state = TestStateFactory.CreateCSharpTestState(
                <Document><![CDATA[
class c
{
    /// $$
    void goo() { }
}
            ]]></Document>, showCompletionInArgumentLists:=showCompletionInArgumentLists)

                state.SendInvokeCompletionList()
                Await state.AssertCompletionSessionAsync()
                state.SendTypeChars("seeal")
                Await state.AssertSelectedCompletionItemAsync(displayText:="seealso")
                state.SendReturn()
                Await state.AssertNoCompletionSessionAsync()

                ' /// <seealso cref="$$"/>
                Await state.AssertLineTextAroundCaretAsync("    /// <seealso cref=""", """/>")
            End Using
        End Function

        <WpfTheory, CombinatorialData, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function InvokeWithOpenAngleCommitSeealsoOnCloseAngle(showCompletionInArgumentLists As Boolean) As Task

            Using state = TestStateFactory.CreateCSharpTestState(
                <Document><![CDATA[
class c
{
    /// $$
    void goo() { }
}
            ]]></Document>, showCompletionInArgumentLists:=showCompletionInArgumentLists)

                state.SendTypeChars("<")
                Await state.AssertCompletionSessionAsync()
                state.SendTypeChars("seeal")
                Await state.AssertSelectedCompletionItemAsync(displayText:="seealso")
                state.SendTypeChars(">")
                Await state.AssertNoCompletionSessionAsync()

                ' /// <seealso cref=">$$"/>
                Await state.AssertLineTextAroundCaretAsync("    /// <seealso cref="">", """/>")
            End Using
        End Function

        <WorkItem(623219, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/623219")>
        <WorkItem(746919, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/746919")>
        <WpfTheory, CombinatorialData, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function CommitParam(showCompletionInArgumentLists As Boolean) As Task
            Using state = TestStateFactory.CreateCSharpTestState(
                <Document><![CDATA[
class c<T>
{
    /// <param$$
    void goo<T>(T bar) { }
}
            ]]></Document>, showCompletionInArgumentLists:=showCompletionInArgumentLists)

                state.SendInvokeCompletionList()
                Await state.AssertCompletionSessionAsync()
                Await state.AssertSelectedCompletionItemAsync(displayText:="param name=""bar""")
                state.SendReturn()
                Await state.AssertNoCompletionSessionAsync()

                ' /// <param name="bar"$$
                Await state.AssertLineTextAroundCaretAsync("    /// <param name=""bar""", "")
            End Using
        End Function

        <WpfTheory, CombinatorialData, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function CommitParam_Record(showCompletionInArgumentLists As Boolean) As Task
            Using state = TestStateFactory.CreateCSharpTestState(
                <Document><![CDATA[
/// <param$$
record R(int I);
            ]]></Document>, showCompletionInArgumentLists:=showCompletionInArgumentLists)

                state.SendInvokeCompletionList()
                Await state.AssertCompletionSessionAsync()
                Await state.AssertSelectedCompletionItemAsync(displayText:="param name=""I""")
                state.SendReturn()
                Await state.AssertNoCompletionSessionAsync()

                ' /// <param name="I"$$
                Await state.AssertLineTextAroundCaretAsync("/// <param name=""I""", "")
            End Using
        End Function

        <WpfTheory, CombinatorialData, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function CommitParamNoOpenAngle(showCompletionInArgumentLists As Boolean) As Task

            Using state = TestStateFactory.CreateCSharpTestState(
                <Document><![CDATA[
class c<T>
{
    /// $$
    void goo<T>(T bar) { }
}
            ]]></Document>, showCompletionInArgumentLists:=showCompletionInArgumentLists)

                state.SendInvokeCompletionList()
                Await state.AssertCompletionSessionAsync()
                state.SendTypeChars("param")
                Await state.AssertSelectedCompletionItemAsync(displayText:="param name=""bar""")
                state.SendReturn()
                Await state.AssertNoCompletionSessionAsync()

                ' /// param name="bar"$$
                Await state.AssertLineTextAroundCaretAsync("    /// param name=""bar""", "")
            End Using
        End Function

        <WpfTheory, CombinatorialData, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function CommitParamNoOpenAngleOnTab(showCompletionInArgumentLists As Boolean) As Task

            Using state = TestStateFactory.CreateCSharpTestState(
                <Document><![CDATA[
class c<T>
{
    /// $$
    void goo<T>(T bar) { }
}
            ]]></Document>, showCompletionInArgumentLists:=showCompletionInArgumentLists)

                state.SendInvokeCompletionList()
                Await state.AssertCompletionSessionAsync()
                state.SendTypeChars("param")
                Await state.AssertSelectedCompletionItemAsync(displayText:="param name=""bar""")
                state.SendTab()
                Await state.AssertNoCompletionSessionAsync()

                ' /// param name="bar"$$
                Await state.AssertLineTextAroundCaretAsync("    /// param name=""bar""", "")
            End Using
        End Function

        <WpfTheory, CombinatorialData, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function CommitParamNoOpenAngleOnCloseAngle(showCompletionInArgumentLists As Boolean) As Task

            Using state = TestStateFactory.CreateCSharpTestState(
                <Document><![CDATA[
class c<T>
{
    /// $$
    void goo<T>(T bar) { }
}
            ]]></Document>, showCompletionInArgumentLists:=showCompletionInArgumentLists)

                state.SendInvokeCompletionList()
                Await state.AssertCompletionSessionAsync()
                state.SendTypeChars("param")
                Await state.AssertSelectedCompletionItemAsync(displayText:="param name=""bar""")
                state.SendTypeChars(">")
                Await state.AssertNoCompletionSessionAsync()

                ' /// param name="bar">$$
                Await state.AssertLineTextAroundCaretAsync("    /// param name=""bar"">", "")
            End Using
        End Function

        <WpfTheory, CombinatorialData, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function InvokeWithOpenAngleCommitParam(showCompletionInArgumentLists As Boolean) As Task

            Using state = TestStateFactory.CreateCSharpTestState(
                <Document><![CDATA[
class c<T>
{
    /// $$
    void goo<T>(T bar) { }
}
            ]]></Document>, showCompletionInArgumentLists:=showCompletionInArgumentLists)

                state.SendTypeChars("<")
                Await state.AssertCompletionSessionAsync()
                state.SendTypeChars("param")
                Await state.AssertSelectedCompletionItemAsync(displayText:="param name=""bar""")
                state.SendReturn()
                Await state.AssertNoCompletionSessionAsync()

                ' /// <param name="bar"$$
                Await state.AssertLineTextAroundCaretAsync("    /// <param name=""bar""", "")
            End Using
        End Function

        <WpfTheory, CombinatorialData, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function InvokeWithOpenAngleCommitParamOnTab(showCompletionInArgumentLists As Boolean) As Task

            Using state = TestStateFactory.CreateCSharpTestState(
                <Document><![CDATA[
class c<T>
{
    /// $$
    void goo<T>(T bar) { }
}
            ]]></Document>, showCompletionInArgumentLists:=showCompletionInArgumentLists)

                state.SendTypeChars("<")
                Await state.AssertCompletionSessionAsync()
                state.SendTypeChars("param")
                Await state.AssertSelectedCompletionItemAsync(displayText:="param name=""bar""")
                state.SendTab()
                Await state.AssertNoCompletionSessionAsync()

                ' /// <param name="bar"$$
                Await state.AssertLineTextAroundCaretAsync("    /// <param name=""bar""", "")
            End Using
        End Function

        <WpfTheory, CombinatorialData, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function InvokeWithOpenAngleCommitParamOnCloseAngle(showCompletionInArgumentLists As Boolean) As Task

            Using state = TestStateFactory.CreateCSharpTestState(
                <Document><![CDATA[
class c<T>
{
    /// $$
    void goo<T>(T bar) { }
}
            ]]></Document>, showCompletionInArgumentLists:=showCompletionInArgumentLists)

                state.SendTypeChars("<")
                Await state.AssertCompletionSessionAsync()
                state.SendTypeChars("param")
                Await state.AssertSelectedCompletionItemAsync(displayText:="param name=""bar""")
                state.SendTypeChars(">")
                Await state.AssertNoCompletionSessionAsync()

                ' /// <param name="bar">$$
                Await state.AssertLineTextAroundCaretAsync("    /// <param name=""bar"">", "")
            End Using
        End Function

        <WpfTheory, CombinatorialData, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function CommitTypeparamNoOpenAngle(showCompletionInArgumentLists As Boolean) As Task

            Using state = TestStateFactory.CreateCSharpTestState(
                <Document><![CDATA[
class c<T>
{
    /// $$
    void goo<T>(T bar) { }
}
            ]]></Document>, showCompletionInArgumentLists:=showCompletionInArgumentLists)

                state.SendInvokeCompletionList()
                Await state.AssertCompletionSessionAsync()
                state.SendTypeChars("typepara")
                Await state.AssertSelectedCompletionItemAsync(displayText:="typeparam name=""T""")
                state.SendReturn()
                Await state.AssertNoCompletionSessionAsync()

                ' /// typeparam name="T"$$
                Await state.AssertLineTextAroundCaretAsync("    /// typeparam name=""T""", "")
            End Using
        End Function

        <WpfTheory, CombinatorialData, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function CommitTypeparamNoOpenAngleOnTab(showCompletionInArgumentLists As Boolean) As Task

            Using state = TestStateFactory.CreateCSharpTestState(
                <Document><![CDATA[
class c<T>
{
    /// $$
    void goo<T>(T bar) { }
}
            ]]></Document>, showCompletionInArgumentLists:=showCompletionInArgumentLists)

                state.SendInvokeCompletionList()
                Await state.AssertCompletionSessionAsync()
                state.SendTypeChars("typepara")
                Await state.AssertSelectedCompletionItemAsync(displayText:="typeparam name=""T""")
                state.SendTab()
                Await state.AssertNoCompletionSessionAsync()

                ' /// typeparam name="T"$$
                Await state.AssertLineTextAroundCaretAsync("    /// typeparam name=""T""", "")
            End Using
        End Function

        <WpfTheory, CombinatorialData, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function CommitTypeparamNoOpenAngleOnCloseAngle(showCompletionInArgumentLists As Boolean) As Task

            Using state = TestStateFactory.CreateCSharpTestState(
                <Document><![CDATA[
class c<T>
{
    /// $$
    void goo<T>(T bar) { }
}
            ]]></Document>, showCompletionInArgumentLists:=showCompletionInArgumentLists)

                state.SendInvokeCompletionList()
                Await state.AssertCompletionSessionAsync()
                state.SendTypeChars("typepara")
                Await state.AssertSelectedCompletionItemAsync(displayText:="typeparam name=""T""")
                state.SendTypeChars(">")
                Await state.AssertNoCompletionSessionAsync()

                ' /// typeparam name="T">$$
                Await state.AssertLineTextAroundCaretAsync("    /// typeparam name=""T"">", "")
            End Using
        End Function

        <WpfTheory, CombinatorialData, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function InvokeWithOpenAngleCommitTypeparam(showCompletionInArgumentLists As Boolean) As Task

            Using state = TestStateFactory.CreateCSharpTestState(
                <Document><![CDATA[
class c<T>
{
    /// $$
    void goo<T>(T bar) { }
}
            ]]></Document>, showCompletionInArgumentLists:=showCompletionInArgumentLists)

                state.SendTypeChars("<")
                Await state.AssertCompletionSessionAsync()
                state.SendTypeChars("typepara")
                Await state.AssertSelectedCompletionItemAsync(displayText:="typeparam name=""T""")
                state.SendReturn()
                Await state.AssertNoCompletionSessionAsync()

                ' /// <typeparam name="T"$$
                Await state.AssertLineTextAroundCaretAsync("    /// <typeparam name=""T""", "")
            End Using
        End Function

        <WpfTheory, CombinatorialData, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function InvokeWithOpenAngleCommitTypeparamOnTab(showCompletionInArgumentLists As Boolean) As Task

            Using state = TestStateFactory.CreateCSharpTestState(
                <Document><![CDATA[
class c<T>
{
    /// $$
    void goo<T>(T bar) { }
}
            ]]></Document>, showCompletionInArgumentLists:=showCompletionInArgumentLists)

                state.SendTypeChars("<")
                Await state.AssertCompletionSessionAsync()
                state.SendTypeChars("typepara")
                Await state.AssertSelectedCompletionItemAsync(displayText:="typeparam name=""T""")
                state.SendTab()
                Await state.AssertNoCompletionSessionAsync()

                ' /// <typeparam name="T"$$
                Await state.AssertLineTextAroundCaretAsync("    /// <typeparam name=""T""", "")
            End Using
        End Function

        <WpfTheory, CombinatorialData, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function InvokeWithOpenAngleCommitTypeparamOnCloseAngle(showCompletionInArgumentLists As Boolean) As Task

            Using state = TestStateFactory.CreateCSharpTestState(
                <Document><![CDATA[
class c<T>
{
    /// $$
    void goo<T>(T bar) { }
}
            ]]></Document>, showCompletionInArgumentLists:=showCompletionInArgumentLists)

                state.SendTypeChars("<")
                Await state.AssertCompletionSessionAsync()
                state.SendTypeChars("typepara")
                Await state.AssertSelectedCompletionItemAsync(displayText:="typeparam name=""T""")
                state.SendTypeChars(">")
                Await state.AssertNoCompletionSessionAsync()

                ' /// <typeparam name="T">$$
                Await state.AssertLineTextAroundCaretAsync("    /// <typeparam name=""T"">", "")
            End Using
        End Function

        <WpfTheory, CombinatorialData, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function CommitList(showCompletionInArgumentLists As Boolean) As Task

            Using state = TestStateFactory.CreateCSharpTestState(
                <Document><![CDATA[
class c<T>
{
    /// <summary>
    /// $$
    /// </summary>
    void goo<T>(T bar) { }
}
            ]]></Document>, showCompletionInArgumentLists:=showCompletionInArgumentLists)

                state.SendInvokeCompletionList()
                Await state.AssertCompletionSessionAsync()
                state.SendTypeChars("lis")
                Await state.AssertSelectedCompletionItemAsync(displayText:="list")
                state.SendReturn()
                Await state.AssertNoCompletionSessionAsync()

                ' /// <list type="$$"
                Await state.AssertLineTextAroundCaretAsync("    /// <list type=""", """")
            End Using
        End Function

        <WpfTheory, CombinatorialData, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function CommitListOnCloseAngle(showCompletionInArgumentLists As Boolean) As Task

            Using state = TestStateFactory.CreateCSharpTestState(
                <Document><![CDATA[
class c<T>
{
    /// <summary>
    /// $$
    /// </summary>
    void goo<T>(T bar) { }
}
            ]]></Document>, showCompletionInArgumentLists:=showCompletionInArgumentLists)

                state.SendInvokeCompletionList()
                Await state.AssertCompletionSessionAsync()
                state.SendTypeChars("lis")
                Await state.AssertSelectedCompletionItemAsync(displayText:="list")
                state.SendTypeChars(">")
                Await state.AssertNoCompletionSessionAsync()

                ' /// <list type=">$$"
                Await state.AssertLineTextAroundCaretAsync("    /// <list type="">", """")
            End Using
        End Function

        <WpfTheory, CombinatorialData, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function TestTagCompletion1(showCompletionInArgumentLists As Boolean) As Task

            Using state = TestStateFactory.CreateCSharpTestState(
                <Document><![CDATA[
class c<T>
{
    /// <$$
    /// </summary>
    void goo<T>(T bar) { }
}
            ]]></Document>, showCompletionInArgumentLists:=showCompletionInArgumentLists)

                state.SendInvokeCompletionList()
                Await state.AssertCompletionSessionAsync()
                state.SendTypeChars("summa")
                Await state.AssertSelectedCompletionItemAsync(displayText:="summary")
                state.SendTypeChars(">")
                Await state.AssertNoCompletionSessionAsync()

                ' /// <summary>$$
                Await state.AssertLineTextAroundCaretAsync("    /// <summary>", "")
            End Using
        End Function

        <WpfTheory, CombinatorialData, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function TestTagCompletion2(showCompletionInArgumentLists As Boolean) As Task

            Using state = TestStateFactory.CreateCSharpTestState(
                <Document><![CDATA[
class c<T>
{
    /// <$$
    /// <remarks></remarks>
    /// </summary>
    void goo<T>(T bar) { }
}
            ]]></Document>, showCompletionInArgumentLists:=showCompletionInArgumentLists)

                state.SendInvokeCompletionList()
                Await state.AssertCompletionSessionAsync()
                state.SendTypeChars("summa")
                Await state.AssertSelectedCompletionItemAsync(displayText:="summary")
                state.SendTypeChars(">")
                Await state.AssertNoCompletionSessionAsync()

                ' /// <summary>$$
                Await state.AssertLineTextAroundCaretAsync("    /// <summary>", "")
            End Using
        End Function

        <WpfTheory, CombinatorialData, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function TestTagCompletion3(showCompletionInArgumentLists As Boolean) As Task

            Using state = TestStateFactory.CreateCSharpTestState(
                <Document><![CDATA[
class c<T>
{
    /// <$$
    /// <remarks>
    /// </summary>
    void goo<T>(T bar) { }
}
            ]]></Document>, showCompletionInArgumentLists:=showCompletionInArgumentLists)

                state.SendInvokeCompletionList()
                Await state.AssertCompletionSessionAsync()
                state.SendTypeChars("summa")
                Await state.AssertSelectedCompletionItemAsync(displayText:="summary")
                state.SendTypeChars(">")
                Await state.AssertNoCompletionSessionAsync()

                ' /// <summary>$$
                Await state.AssertLineTextAroundCaretAsync("    /// <summary>", "")
            End Using
        End Function

        <WorkItem(638653, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/638653")>
        <WpfTheory(Skip:="https://github.com/dotnet/roslyn/issues/21481"), CombinatorialData, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function AllowTypingDoubleQuote(showCompletionInArgumentLists As Boolean) As Task
            Using state = TestStateFactory.CreateCSharpTestState(
                <Document><![CDATA[
class c
{
    /// <param$$
    void goo<T>(T bar) { }
}
            ]]></Document>, showCompletionInArgumentLists:=showCompletionInArgumentLists)

                state.SendInvokeCompletionList()
                Await state.AssertCompletionSessionAsync()
                Await state.AssertSelectedCompletionItemAsync(displayText:="param name=""bar""")
                state.SendTypeChars(" name=""")

                ' /// <param name="$$
                Await state.AssertLineTextAroundCaretAsync("    /// <param name=""", "")

                ' Because the item contains a double quote, the completionImplementation list should still be present with the same selection
                Await state.AssertCompletionSessionAsync()
                Await state.AssertSelectedCompletionItemAsync(displayText:="param name=""bar""")
            End Using
        End Function

        <WorkItem(638653, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/638653")>
        <WpfTheory, CombinatorialData, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function AllowTypingSpace(showCompletionInArgumentLists As Boolean) As Task
            Using state = TestStateFactory.CreateCSharpTestState(
                <Document><![CDATA[
class c
{
    /// <param$$
    void goo<T>(T bar) { }
}
            ]]></Document>, showCompletionInArgumentLists:=showCompletionInArgumentLists)

                state.SendInvokeCompletionList()
                Await state.AssertCompletionSessionAsync()
                Await state.AssertSelectedCompletionItemAsync(displayText:="param name=""bar""")
                state.SendTypeChars(" ")

                ' /// <param $$
                Await state.AssertLineTextAroundCaretAsync("    /// <param ", "")

                ' Because the item contains a space, the completionImplementation list should still be present with the same selection
                Await state.AssertCompletionSessionAsync()
                Await state.AssertSelectedCompletionItemAsync(displayText:="param name=""bar""")
            End Using
        End Function

        <WorkItem(44472, "https://github.com/dotnet/roslyn/issues/44472")>
        <WpfTheory, CombinatorialData, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function InvokeWithAliasAndImportedNamespace(showCompletionInArgumentLists As Boolean) As Task

            Using state = TestStateFactory.CreateTestStateFromWorkspace(
                <Workspace>
                    <Project Language="C#" AssemblyName="Assembly1" CommonReferences="true">
                        <Document>
using System;
using System.Collections.Generic;
using System.Text;

namespace First.NestedA
{
    public class MyClass
    {
    }
}
                        </Document>
                        <Document>
using First.NestedA;
using MyClassLongDescription = First.NestedA.MyClass;

namespace Second.NestedB
{
    class OtherClass
    {
        /// &lt;summary&gt;
        /// This is from &lt;see cref="MyClassL$$"/&gt;
        /// &lt;/summary&gt;
        public void Method()
        {
        }
    }
}
                        </Document>
                    </Project>
                </Workspace>, showCompletionInArgumentLists:=showCompletionInArgumentLists)

                state.SendInvokeCompletionList()
                Await state.AssertCompletionSessionAsync()
                Await state.AssertSelectedCompletionItemAsync(displayText:="MyClassLongDescription")
                state.SendTab()
                Await state.AssertNoCompletionSessionAsync()

                Await state.AssertLineTextAroundCaretAsync("        /// This is from <see cref=""MyClassLongDescription", """/>")
            End Using
        End Function
    End Class
End Namespace
