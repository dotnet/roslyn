' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Namespace Microsoft.CodeAnalysis.Editor.UnitTests.IntelliSense
    <[UseExportProvider]>
    <Trait(Traits.Feature, Traits.Features.Completion)>
    Public Class CSharpCompletionCommandHandlerTests_XmlDoc

        <WpfTheory, CombinatorialData>
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
                Await state.AssertCompletionSession()
                state.SendTypeChars("summ")
                Await state.AssertSelectedCompletionItem(displayText:="summary")
                state.SendReturn()
                Await state.AssertNoCompletionSession()
                Await state.AssertLineTextAroundCaret("    /// summary", "")
            End Using
        End Function

        <WpfTheory, CombinatorialData>
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
                Await state.AssertCompletionSession()
                state.SendTypeChars("summ")
                Await state.AssertSelectedCompletionItem(displayText:="summary")
                state.SendTab()
                Await state.AssertNoCompletionSession()

                ' /// summary$$
                Await state.AssertLineTextAroundCaret("    /// summary", "")
            End Using
        End Function

        <WpfTheory, CombinatorialData>
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
                Await state.AssertCompletionSession()
                state.SendTypeChars("summ")
                Await state.AssertSelectedCompletionItem(displayText:="summary")
                state.SendTypeChars(">")
                Await state.AssertNoCompletionSession()

                ' /// summary>$$
                Await state.AssertLineTextAroundCaret("    /// summary>", "")
            End Using
        End Function

        <WpfTheory, CombinatorialData>
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
                Await state.AssertCompletionSession()
                state.SendTypeChars("summ")
                Await state.AssertSelectedCompletionItem(displayText:="summary")
                state.SendReturn()
                Await state.AssertNoCompletionSession()

                ' /// <summary$$
                Await state.AssertLineTextAroundCaret("    /// <summary", "")
            End Using
        End Function

        <WpfTheory, CombinatorialData>
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
                Await state.AssertCompletionSession()
                state.SendTypeChars("summ")
                Await state.AssertSelectedCompletionItem(displayText:="summary")
                state.SendTab()
                Await state.AssertNoCompletionSession()

                ' /// <summary$$
                Await state.AssertLineTextAroundCaret("    /// <summary", "")
            End Using
        End Function

        <WpfTheory, CombinatorialData>
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
                Await state.AssertCompletionSession()
                state.SendTypeChars("summ")
                Await state.AssertSelectedCompletionItem(displayText:="summary")
                state.SendTypeChars(">")
                Await state.AssertNoCompletionSession()

                ' /// <summary>$$
                Await state.AssertLineTextAroundCaret("    /// <summary>", "")
            End Using
        End Function

        <WpfTheory, CombinatorialData>
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
                Await state.AssertCompletionSession()
                state.SendTypeChars("rema")
                Await state.AssertSelectedCompletionItem(displayText:="remarks")
                state.SendTypeChars(">")
                Await state.AssertNoCompletionSession()

                ' /// remarks>$$
                Await state.AssertLineTextAroundCaret("    /// remarks>", "")
            End Using
        End Function

        <WpfTheory, CombinatorialData>
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
                Await state.AssertCompletionSession()
                state.SendTypeChars("rema")
                Await state.AssertSelectedCompletionItem(displayText:="remarks")
                state.SendTypeChars(">")
                Await state.AssertNoCompletionSession()

                ' /// <remarks>$$
                Await state.AssertLineTextAroundCaret("    /// <remarks>", "")
            End Using
        End Function

        <WpfTheory, CombinatorialData>
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
                Await state.AssertCompletionSession()
                state.SendTypeChars("retur")
                Await state.AssertSelectedCompletionItem(displayText:="returns")
                state.SendTypeChars(">")
                Await state.AssertNoCompletionSession()

                ' /// returns>$$
                Await state.AssertLineTextAroundCaret("    /// returns>", "")
            End Using
        End Function

        <WpfTheory, CombinatorialData>
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
                Await state.AssertCompletionSession()
                state.SendTypeChars("retur")
                Await state.AssertSelectedCompletionItem(displayText:="returns")
                state.SendTypeChars(">")
                Await state.AssertNoCompletionSession()

                ' /// <returns>$$
                Await state.AssertLineTextAroundCaret("    /// <returns>", "")
            End Using
        End Function

        <WpfTheory, CombinatorialData>
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
                Await state.AssertCompletionSession()
                state.SendTypeChars("examp")
                Await state.AssertSelectedCompletionItem(displayText:="example")
                state.SendTypeChars(">")
                Await state.AssertNoCompletionSession()

                ' /// example>$$
                Await state.AssertLineTextAroundCaret("    /// example>", "")
            End Using
        End Function

        <WpfTheory, CombinatorialData>
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
                Await state.AssertCompletionSession()
                state.SendTypeChars("examp")
                Await state.AssertSelectedCompletionItem(displayText:="example")
                state.SendTypeChars(">")
                Await state.AssertNoCompletionSession()

                ' /// <example>$$
                Await state.AssertLineTextAroundCaret("    /// <example>", "")
            End Using
        End Function

        <WpfTheory, CombinatorialData>
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
                Await state.AssertCompletionSession()
                state.SendTypeChars("except")
                Await state.AssertSelectedCompletionItem(displayText:="exception")
                state.SendReturn()
                Await state.AssertNoCompletionSession()

                ' /// <exception cref="$$"
                Await state.AssertLineTextAroundCaret("    /// <exception cref=""", """")
            End Using
        End Function

        <WpfTheory, CombinatorialData>
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
                Await state.AssertCompletionSession()
                state.SendTypeChars("except")
                Await state.AssertSelectedCompletionItem(displayText:="exception")
                state.SendTypeChars(">")
                Await state.AssertNoCompletionSession()

                ' /// <exception cref=">$$"
                Await state.AssertLineTextAroundCaret("    /// <exception cref="">", """")
            End Using
        End Function

        <WpfTheory, CombinatorialData>
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
                Await state.AssertCompletionSession()
                state.SendSelectCompletionItem("!--")
                state.SendReturn()
                Await state.AssertNoCompletionSession()

                ' /// <!--$$-->
                Await state.AssertLineTextAroundCaret("    /// <!--", "-->")
            End Using
        End Function

        <WpfTheory, CombinatorialData>
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
                Await state.AssertCompletionSession()
                state.SendSelectCompletionItem("!--")
                state.SendTypeChars(">")
                Await state.AssertNoCompletionSession()

                ' /// <!-->$$-->
                Await state.AssertLineTextAroundCaret("    /// <!-->", "-->")
            End Using
        End Function

        <WpfTheory, CombinatorialData>
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
                Await state.AssertCompletionSession()
                state.SendTypeChars("![CDAT")
                Await state.AssertSelectedCompletionItem(displayText:="![CDATA[")
                state.SendReturn()
                Await state.AssertNoCompletionSession()

                ' /// <![CDATA[$$]]>
                Await state.AssertLineTextAroundCaret("    /// <![CDATA[", "]]>")
            End Using
        End Function

        <WpfTheory, CombinatorialData>
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
                Await state.AssertCompletionSession()
                state.SendTypeChars("![CDAT")
                Await state.AssertSelectedCompletionItem(displayText:="![CDATA[")
                state.SendTypeChars(">")
                Await state.AssertNoCompletionSession()

                ' /// <![CDATA[>$$]]>
                Await state.AssertLineTextAroundCaret("    /// <![CDATA[>", "]]>")
            End Using
        End Function

        <WpfTheory, CombinatorialData>
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
                Await state.AssertCompletionSession()
                state.SendTypeChars("inclu")
                Await state.AssertSelectedCompletionItem(displayText:="include")
                state.SendReturn()
                Await state.AssertNoCompletionSession()

                ' /// <include file='$$' path='[@name=""]'/>
                Await state.AssertLineTextAroundCaret("    /// <include file='", "' path='[@name=""""]'/>")
            End Using
        End Function

        <WpfTheory, CombinatorialData>
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
                Await state.AssertCompletionSession()
                state.SendTypeChars("inclu")
                Await state.AssertSelectedCompletionItem(displayText:="include")
                state.SendTypeChars(">")
                Await state.AssertNoCompletionSession()

                ' /// <include file='>$$' path='[@name=""]'/>
                Await state.AssertLineTextAroundCaret("    /// <include file='>", "' path='[@name=""""]'/>")
            End Using
        End Function

        <WpfTheory, CombinatorialData>
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
                Await state.AssertCompletionSession()
                state.SendTypeChars("permiss")
                Await state.AssertSelectedCompletionItem(displayText:="permission")
                state.SendReturn()
                Await state.AssertNoCompletionSession()

                ' /// <permission cref="$$"
                Await state.AssertLineTextAroundCaret("    /// <permission cref=""", """")
            End Using
        End Function

        <WpfTheory, CombinatorialData>
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
                Await state.AssertCompletionSession()
                state.SendTypeChars("permiss")
                Await state.AssertSelectedCompletionItem(displayText:="permission")
                state.SendTypeChars(">")
                Await state.AssertNoCompletionSession()

                ' /// <permission cref=">$$"
                Await state.AssertLineTextAroundCaret("    /// <permission cref="">", """")
            End Using
        End Function

        <WpfTheory, CombinatorialData>
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
                Await state.AssertCompletionSession()
                state.SendTypeChars("see")
                Await state.AssertSelectedCompletionItem(displayText:="see")
                state.SendReturn()
                Await state.AssertNoCompletionSession()

                ' /// <see cref="$$"/>
                Await state.AssertLineTextAroundCaret("    /// <see cref=""", """/>")
            End Using
        End Function

        <WpfTheory, CombinatorialData>
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
                Await state.AssertCompletionSession()
                state.SendTypeChars("see")
                Await state.AssertSelectedCompletionItem(displayText:="see")
                state.SendTypeChars(">")
                Await state.AssertNoCompletionSession()

                ' /// <see cref=">$$"/>
                Await state.AssertLineTextAroundCaret("    /// <see cref="">", """/>")
            End Using
        End Function

        <WpfTheory, CombinatorialData>
        <WorkItem("https://github.com/dotnet/roslyn/issues/22789")>
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
                Await state.AssertCompletionSession()
                state.SendTypeChars("se")
                Await state.AssertSelectedCompletionItem(displayText:="see")
                state.SendTab()
                Await state.AssertNoCompletionSession()

                ' /// <see cref="$$"/>
                Await state.AssertLineTextAroundCaret("    /// <see cref=""", """/>")
            End Using
        End Function

        <WpfTheory, CombinatorialData>
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
                Await state.AssertCompletionSession()
                state.SendTypeChars("see")
                Await state.AssertSelectedCompletionItem(displayText:="see")
                state.SendTypeChars(" ")

                ' /// <see cref="$$"/>
                Await state.AssertLineTextAroundCaret("    /// <see cref=""", """/>")
            End Using
        End Function

        <WpfTheory, CombinatorialData>
        Public Async Function InvokeWithOpenAngleSeeCommitSeeWithEqualsQuotes(showCompletionInArgumentLists As Boolean) As Task
            Using state = TestStateFactory.CreateCSharpTestState(
                <Document><![CDATA[
class c
{
    /// <summary>
    /// <see $$=""
    /// </summary>
    void goo() { }
}
            ]]></Document>, showCompletionInArgumentLists:=showCompletionInArgumentLists)

                state.SendInvokeCompletionList()
                state.AssertItemsInOrder({"!--", "![CDATA[", "inheritdoc", "see", "seealso"})
                state.SendTypeChars("see")
                Await state.AssertSelectedCompletionItem(displayText:="see")
                state.SendReturn()

                ' /// <see <see cref=""/$$>=""
                Await state.AssertLineTextAroundCaret("    /// <see <see cref=""""/", ">=""""")
            End Using
        End Function

        <WpfTheory, CombinatorialData>
        Public Async Function InvokeWithOpenAngleSeeCommitLangwordWithEqualsQuotes(showCompletionInArgumentLists As Boolean) As Task
            Using state = TestStateFactory.CreateCSharpTestState(
                <Document><![CDATA[
class c
{
    /// <summary>
    /// <see $$=""
    /// </summary>
    void goo() { }
}
            ]]></Document>, showCompletionInArgumentLists:=showCompletionInArgumentLists)

                state.SendTypeChars("l")
                state.SendInvokeCompletionList()
                Await state.AssertSelectedCompletionItem(displayText:="langword")
                state.SendReturn()

                ' /// <see langword="$$"
                Await state.AssertLineTextAroundCaret("    /// <see langword=""", """")
            End Using
        End Function

        <WpfTheory, CombinatorialData>
        Public Async Function InvokeWithOpenAngleSeeCommitLangwordWithSpaceEqualsQuotes(showCompletionInArgumentLists As Boolean) As Task
            Using state = TestStateFactory.CreateCSharpTestState(
                <Document><![CDATA[
class c
{
    /// <summary>
    /// <see $$ =""
    /// </summary>
    void goo() { }
}
            ]]></Document>, showCompletionInArgumentLists:=showCompletionInArgumentLists)

                state.SendTypeChars("l")
                state.SendInvokeCompletionList()
                Await state.AssertSelectedCompletionItem(displayText:="langword")
                state.SendReturn()

                ' /// <see langword="$$" =""
                Await state.AssertLineTextAroundCaret("    /// <see langword=""", """ =""""")
            End Using
        End Function

        <WpfTheory, CombinatorialData>
        Public Function InvokeWithNullKeywordCommitSeeLangword(showCompletionInArgumentLists As Boolean) As Task
            Return InvokeWithKeywordCommitSeeLangword("null", showCompletionInArgumentLists)
        End Function

        <WpfTheory, CombinatorialData>
        Public Function InvokeWithStaticKeywordCommitSeeLangword(showCompletionInArgumentLists As Boolean) As Task
            Return InvokeWithKeywordCommitSeeLangword("static", showCompletionInArgumentLists)
        End Function

        <WpfTheory, CombinatorialData>
        Public Function InvokeWithVirtualKeywordCommitSeeLangword(showCompletionInArgumentLists As Boolean) As Task
            Return InvokeWithKeywordCommitSeeLangword("virtual", showCompletionInArgumentLists)
        End Function

        <WpfTheory, CombinatorialData>
        Public Function InvokeWithTrueKeywordCommitSeeLangword(showCompletionInArgumentLists As Boolean) As Task
            Return InvokeWithKeywordCommitSeeLangword("true", showCompletionInArgumentLists, unique:=False)
        End Function

        <WpfTheory, CombinatorialData>
        Public Function InvokeWithFalseKeywordCommitSeeLangword(showCompletionInArgumentLists As Boolean) As Task
            Return InvokeWithKeywordCommitSeeLangword("false", showCompletionInArgumentLists)
        End Function

        <WpfTheory, CombinatorialData>
        Public Function InvokeWithAbstractKeywordCommitSeeLangword(showCompletionInArgumentLists As Boolean) As Task
            Return InvokeWithKeywordCommitSeeLangword("abstract", showCompletionInArgumentLists)
        End Function

        <WpfTheory, CombinatorialData>
        Public Function InvokeWithSealedKeywordCommitSeeLangword(showCompletionInArgumentLists As Boolean) As Task
            Return InvokeWithKeywordCommitSeeLangword("sealed", showCompletionInArgumentLists)
        End Function

        <WpfTheory, CombinatorialData>
        Public Function InvokeWithAsyncKeywordCommitSeeLangword(showCompletionInArgumentLists As Boolean) As Task
            Return InvokeWithKeywordCommitSeeLangword("async", showCompletionInArgumentLists)
        End Function

        <WpfTheory, CombinatorialData>
        Public Function InvokeWithAwaitKeywordCommitSeeLangword(showCompletionInArgumentLists As Boolean) As Task
            Return InvokeWithKeywordCommitSeeLangword("await", showCompletionInArgumentLists)
        End Function

        Private Shared Async Function InvokeWithKeywordCommitSeeLangword(keyword As String, showCompletionInArgumentLists As Boolean, Optional unique As Boolean = True) As Task
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
                If unique Then
                    Await state.SendCommitUniqueCompletionListItemAsync()
                Else
                    Await state.AssertSelectedCompletionItem(displayText:=keyword)
                    state.SendTab()
                End If

                Await state.AssertNoCompletionSession()

                ' /// <see langword="keyword"/>$$
                Await state.AssertLineTextAroundCaret("    /// <see langword=""" + keyword + """/>", "")
            End Using
        End Function

        <WpfTheory, CombinatorialData>
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
                Await state.AssertCompletionSession()
                state.SendTypeChars("seeal")
                Await state.AssertSelectedCompletionItem(displayText:="seealso")
                state.SendReturn()
                Await state.AssertNoCompletionSession()

                ' /// <seealso cref="$$"/>
                Await state.AssertLineTextAroundCaret("    /// <seealso cref=""", """/>")
            End Using
        End Function

        <WpfTheory, CombinatorialData>
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
                Await state.AssertCompletionSession()
                state.SendTypeChars("seeal")
                Await state.AssertSelectedCompletionItem(displayText:="seealso")
                state.SendTypeChars(">")
                Await state.AssertNoCompletionSession()

                ' /// <seealso cref=">$$"/>
                Await state.AssertLineTextAroundCaret("    /// <seealso cref="">", """/>")
            End Using
        End Function

        <WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/623219")>
        <WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/746919")>
        <WpfTheory, CombinatorialData>
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
                Await state.AssertCompletionSession()
                Await state.AssertSelectedCompletionItem(displayText:="param name=""bar""")
                state.SendReturn()
                Await state.AssertNoCompletionSession()

                ' /// <param name="bar"$$
                Await state.AssertLineTextAroundCaret("    /// <param name=""bar""", "")
            End Using
        End Function

        <WpfTheory, CombinatorialData>
        Public Async Function CommitParam_Record(showCompletionInArgumentLists As Boolean) As Task
            Using state = TestStateFactory.CreateCSharpTestState(
                <Document><![CDATA[
/// <param$$
record R(int I);
            ]]></Document>, showCompletionInArgumentLists:=showCompletionInArgumentLists)

                state.SendInvokeCompletionList()
                Await state.AssertCompletionSession()
                Await state.AssertSelectedCompletionItem(displayText:="param name=""I""")
                state.SendReturn()
                Await state.AssertNoCompletionSession()

                ' /// <param name="I"$$
                Await state.AssertLineTextAroundCaret("/// <param name=""I""", "")
            End Using
        End Function

        <WpfTheory, CombinatorialData>
        Public Async Function CommitParam_Class(showCompletionInArgumentLists As Boolean) As Task
            Using state = TestStateFactory.CreateCSharpTestState(
                <Document><![CDATA[
/// <param$$
class R(int I);
            ]]></Document>, showCompletionInArgumentLists:=showCompletionInArgumentLists)

                state.SendInvokeCompletionList()
                Await state.AssertCompletionSession()
                Await state.AssertSelectedCompletionItem(displayText:="param name=""I""")
                state.SendReturn()
                Await state.AssertNoCompletionSession()

                ' /// <param name="I"$$
                Await state.AssertLineTextAroundCaret("/// <param name=""I""", "")
            End Using
        End Function

        <WpfTheory, CombinatorialData>
        Public Async Function CommitParam_Struct(showCompletionInArgumentLists As Boolean) As Task
            Using state = TestStateFactory.CreateCSharpTestState(
                <Document><![CDATA[
/// <param$$
struct R(int I);
            ]]></Document>, showCompletionInArgumentLists:=showCompletionInArgumentLists)

                state.SendInvokeCompletionList()
                Await state.AssertCompletionSession()
                Await state.AssertSelectedCompletionItem(displayText:="param name=""I""")
                state.SendReturn()
                Await state.AssertNoCompletionSession()

                ' /// <param name="I"$$
                Await state.AssertLineTextAroundCaret("/// <param name=""I""", "")
            End Using
        End Function

        <WpfTheory, CombinatorialData>
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
                Await state.AssertCompletionSession()
                state.SendTypeChars("param")
                Await state.AssertSelectedCompletionItem(displayText:="param name=""bar""")
                state.SendReturn()
                Await state.AssertNoCompletionSession()

                ' /// param name="bar"$$
                Await state.AssertLineTextAroundCaret("    /// param name=""bar""", "")
            End Using
        End Function

        <WpfTheory, CombinatorialData>
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
                Await state.AssertCompletionSession()
                state.SendTypeChars("param")
                Await state.AssertSelectedCompletionItem(displayText:="param name=""bar""")
                state.SendTab()
                Await state.AssertNoCompletionSession()

                ' /// param name="bar"$$
                Await state.AssertLineTextAroundCaret("    /// param name=""bar""", "")
            End Using
        End Function

        <WpfTheory, CombinatorialData>
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
                Await state.AssertCompletionSession()
                state.SendTypeChars("param")
                Await state.AssertSelectedCompletionItem(displayText:="param name=""bar""")
                state.SendTypeChars(">")
                Await state.AssertNoCompletionSession()

                ' /// param name="bar">$$
                Await state.AssertLineTextAroundCaret("    /// param name=""bar"">", "")
            End Using
        End Function

        <WpfTheory, CombinatorialData>
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
                Await state.AssertCompletionSession()
                state.SendTypeChars("param")
                Await state.AssertSelectedCompletionItem(displayText:="param name=""bar""")
                state.SendReturn()
                Await state.AssertNoCompletionSession()

                ' /// <param name="bar"$$
                Await state.AssertLineTextAroundCaret("    /// <param name=""bar""", "")
            End Using
        End Function

        <WpfTheory, CombinatorialData>
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
                Await state.AssertCompletionSession()
                state.SendTypeChars("param")
                Await state.AssertSelectedCompletionItem(displayText:="param name=""bar""")
                state.SendTab()
                Await state.AssertNoCompletionSession()

                ' /// <param name="bar"$$
                Await state.AssertLineTextAroundCaret("    /// <param name=""bar""", "")
            End Using
        End Function

        <WpfTheory, CombinatorialData>
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
                Await state.AssertCompletionSession()
                state.SendTypeChars("param")
                Await state.AssertSelectedCompletionItem(displayText:="param name=""bar""")
                state.SendTypeChars(">")
                Await state.AssertNoCompletionSession()

                ' /// <param name="bar">$$
                Await state.AssertLineTextAroundCaret("    /// <param name=""bar"">", "")
            End Using
        End Function

        <WpfTheory, CombinatorialData>
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
                Await state.AssertCompletionSession()
                state.SendTypeChars("typepara")
                Await state.AssertSelectedCompletionItem(displayText:="typeparam name=""T""")
                state.SendReturn()
                Await state.AssertNoCompletionSession()

                ' /// typeparam name="T"$$
                Await state.AssertLineTextAroundCaret("    /// typeparam name=""T""", "")
            End Using
        End Function

        <WpfTheory, CombinatorialData>
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
                Await state.AssertCompletionSession()
                state.SendTypeChars("typepara")
                Await state.AssertSelectedCompletionItem(displayText:="typeparam name=""T""")
                state.SendTab()
                Await state.AssertNoCompletionSession()

                ' /// typeparam name="T"$$
                Await state.AssertLineTextAroundCaret("    /// typeparam name=""T""", "")
            End Using
        End Function

        <WpfTheory, CombinatorialData>
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
                Await state.AssertCompletionSession()
                state.SendTypeChars("typepara")
                Await state.AssertSelectedCompletionItem(displayText:="typeparam name=""T""")
                state.SendTypeChars(">")
                Await state.AssertNoCompletionSession()

                ' /// typeparam name="T">$$
                Await state.AssertLineTextAroundCaret("    /// typeparam name=""T"">", "")
            End Using
        End Function

        <WpfTheory, CombinatorialData>
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
                Await state.AssertCompletionSession()
                state.SendTypeChars("typepara")
                Await state.AssertSelectedCompletionItem(displayText:="typeparam name=""T""")
                state.SendReturn()
                Await state.AssertNoCompletionSession()

                ' /// <typeparam name="T"$$
                Await state.AssertLineTextAroundCaret("    /// <typeparam name=""T""", "")
            End Using
        End Function

        <WpfTheory, CombinatorialData>
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
                Await state.AssertCompletionSession()
                state.SendTypeChars("typepara")
                Await state.AssertSelectedCompletionItem(displayText:="typeparam name=""T""")
                state.SendTab()
                Await state.AssertNoCompletionSession()

                ' /// <typeparam name="T"$$
                Await state.AssertLineTextAroundCaret("    /// <typeparam name=""T""", "")
            End Using
        End Function

        <WpfTheory, CombinatorialData>
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
                Await state.AssertCompletionSession()
                state.SendTypeChars("typepara")
                Await state.AssertSelectedCompletionItem(displayText:="typeparam name=""T""")
                state.SendTypeChars(">")
                Await state.AssertNoCompletionSession()

                ' /// <typeparam name="T">$$
                Await state.AssertLineTextAroundCaret("    /// <typeparam name=""T"">", "")
            End Using
        End Function

        <WpfTheory, CombinatorialData>
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
                Await state.AssertCompletionSession()
                state.SendTypeChars("lis")
                Await state.AssertSelectedCompletionItem(displayText:="list")
                state.SendReturn()
                Await state.AssertNoCompletionSession()

                ' /// <list type="$$"
                Await state.AssertLineTextAroundCaret("    /// <list type=""", """")
            End Using
        End Function

        <WpfTheory, CombinatorialData>
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
                Await state.AssertCompletionSession()
                state.SendTypeChars("lis")
                Await state.AssertSelectedCompletionItem(displayText:="list")
                state.SendTypeChars(">")
                Await state.AssertNoCompletionSession()

                ' /// <list type=">$$"
                Await state.AssertLineTextAroundCaret("    /// <list type="">", """")
            End Using
        End Function

        <WpfTheory, CombinatorialData>
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
                Await state.AssertCompletionSession()
                state.SendTypeChars("summa")
                Await state.AssertSelectedCompletionItem(displayText:="summary")
                state.SendTypeChars(">")
                Await state.AssertNoCompletionSession()

                ' /// <summary>$$
                Await state.AssertLineTextAroundCaret("    /// <summary>", "")
            End Using
        End Function

        <WpfTheory, CombinatorialData>
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
                Await state.AssertCompletionSession()
                state.SendTypeChars("summa")
                Await state.AssertSelectedCompletionItem(displayText:="summary")
                state.SendTypeChars(">")
                Await state.AssertNoCompletionSession()

                ' /// <summary>$$
                Await state.AssertLineTextAroundCaret("    /// <summary>", "")
            End Using
        End Function

        <WpfTheory, CombinatorialData>
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
                Await state.AssertCompletionSession()
                state.SendTypeChars("summa")
                Await state.AssertSelectedCompletionItem(displayText:="summary")
                state.SendTypeChars(">")
                Await state.AssertNoCompletionSession()

                ' /// <summary>$$
                Await state.AssertLineTextAroundCaret("    /// <summary>", "")
            End Using
        End Function

        <WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/638653")>
        <WpfTheory(Skip:="https://github.com/dotnet/roslyn/issues/21481"), CombinatorialData>
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
                Await state.AssertCompletionSession()
                Await state.AssertSelectedCompletionItem(displayText:="param name=""bar""")
                state.SendTypeChars(" name=""")

                ' /// <param name="$$
                Await state.AssertLineTextAroundCaret("    /// <param name=""", "")

                ' Because the item contains a double quote, the completionImplementation list should still be present with the same selection
                Await state.AssertCompletionSession()
                Await state.AssertSelectedCompletionItem(displayText:="param name=""bar""")
            End Using
        End Function

        <WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/638653")>
        <WpfTheory, CombinatorialData>
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
                Await state.AssertCompletionSession()
                Await state.AssertSelectedCompletionItem(displayText:="param name=""bar""")
                state.SendTypeChars(" ")

                ' /// <param $$
                Await state.AssertLineTextAroundCaret("    /// <param ", "")

                ' Because the item contains a space, the completionImplementation list should still be present with the same selection
                Await state.AssertCompletionSession()
                Await state.AssertSelectedCompletionItem(displayText:="param name=""bar""")
            End Using
        End Function

        <WorkItem("https://github.com/dotnet/roslyn/issues/44472")>
        <WpfTheory, CombinatorialData>
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
                Await state.AssertCompletionSession()
                Await state.AssertSelectedCompletionItem(displayText:="MyClassLongDescription")
                state.SendTab()
                Await state.AssertNoCompletionSession()

                Await state.AssertLineTextAroundCaret("        /// This is from <see cref=""MyClassLongDescription", """/>")
            End Using
        End Function

        <WpfTheory, CombinatorialData>
        <WorkItem("https://github.com/dotnet/roslyn/issues/7424")>
        Public Async Function TestKeywordsInCref(showCompletionInArgumentLists As Boolean) As Task
            Using state = TestStateFactory.CreateTestStateFromWorkspace(
                <Workspace>
                    <Project Language="C#" AssemblyName="Assembly1" CommonReferences="true">
                        <Document>
class OtherClass
{
    /// &lt;summary&gt;
    /// This is from &lt;see cref="Goo($$)"/&gt;
    /// &lt;/summary&gt;
    public void Method()
    {
    }
}
                        </Document>
                    </Project>
                </Workspace>, showCompletionInArgumentLists:=showCompletionInArgumentLists)

                state.SendInvokeCompletionList()
                Await state.AssertCompletionSession()
                Await state.AssertCompletionItemsContainAll("out", "ref")
            End Using
        End Function
    End Class
End Namespace
