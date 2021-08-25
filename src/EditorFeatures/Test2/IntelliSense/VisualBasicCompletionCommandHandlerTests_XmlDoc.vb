' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Namespace Microsoft.CodeAnalysis.Editor.UnitTests.IntelliSense
    <[UseExportProvider]>
    Public Class VisualBasicCompletionCommandHandlerTests_XmlDoc

        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function CommitSummary() As Task

            Using state = TestStateFactory.CreateVisualBasicTestState(
                <Document><![CDATA[
Class C
    ''' $$
    Sub Goo()
    End Sub
End Class
            ]]></Document>)

                state.SendInvokeCompletionList()
                Await state.AssertCompletionSessionAsync()
                state.SendTypeChars("summ")
                Await state.AssertSelectedCompletionItemAsync(displayText:="summary")
                state.SendReturn()
                Await state.AssertNoCompletionSessionAsync()
                Await state.AssertLineTextAroundCaretAsync("    ''' summary", "")
            End Using
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function CommitSummaryOnTab() As Task

            Using state = TestStateFactory.CreateVisualBasicTestState(
                <Document><![CDATA[
Class C
    ''' $$
    Sub Goo()
    End Sub
End Class
            ]]></Document>)

                state.SendInvokeCompletionList()
                Await state.AssertCompletionSessionAsync()
                state.SendTypeChars("summ")
                Await state.AssertSelectedCompletionItemAsync(displayText:="summary")
                state.SendTab()
                Await state.AssertNoCompletionSessionAsync()

                ' ''' summary$$
                Await state.AssertLineTextAroundCaretAsync("    ''' summary", "")
            End Using
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function CommitSummaryOnCloseAngle() As Task

            Using state = TestStateFactory.CreateVisualBasicTestState(
                <Document><![CDATA[
Class C
    ''' $$
    Sub Goo()
    End Sub
End Class
            ]]></Document>)

                state.SendInvokeCompletionList()
                Await state.AssertCompletionSessionAsync()
                state.SendTypeChars("summ")
                Await state.AssertSelectedCompletionItemAsync(displayText:="summary")
                state.SendTypeChars(">")
                Await state.AssertNoCompletionSessionAsync()

                ' ''' summary>$$
                Await state.AssertLineTextAroundCaretAsync("    ''' summary>", "")
            End Using
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function InvokeWithOpenAngleCommitSummary() As Task

            Using state = TestStateFactory.CreateVisualBasicTestState(
                <Document><![CDATA[
Class C
    ''' $$
    Sub Goo()
    End Sub
End Class
            ]]></Document>)

                state.SendTypeChars("<")
                Await state.AssertCompletionSessionAsync()
                state.SendTypeChars("summ")
                Await state.AssertSelectedCompletionItemAsync(displayText:="summary")
                state.SendReturn()
                Await state.AssertNoCompletionSessionAsync()

                ' ''' <summary$$
                Await state.AssertLineTextAroundCaretAsync("    ''' <summary", "")
            End Using
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function InvokeWithOpenAngleCommitSummaryOnTab() As Task

            Using state = TestStateFactory.CreateVisualBasicTestState(
                <Document><![CDATA[
Class C
    ''' $$
    Sub Goo()
    End Sub
End Class
            ]]></Document>)

                state.SendTypeChars("<")
                Await state.AssertCompletionSessionAsync()
                state.SendTypeChars("summ")
                Await state.AssertSelectedCompletionItemAsync(displayText:="summary")
                state.SendTab()
                Await state.AssertNoCompletionSessionAsync()

                ' ''' <summary$$
                Await state.AssertLineTextAroundCaretAsync("    ''' <summary", "")
            End Using
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function InvokeWithOpenAngleCommitSummaryOnCloseAngle() As Task

            Using state = TestStateFactory.CreateVisualBasicTestState(
                <Document><![CDATA[
Class C
    ''' $$
    Sub Goo()
    End Sub
End Class
            ]]></Document>)

                state.SendTypeChars("<")
                Await state.AssertCompletionSessionAsync()
                state.SendTypeChars("summ")
                Await state.AssertSelectedCompletionItemAsync(displayText:="summary")
                state.SendTypeChars(">")
                Await state.AssertNoCompletionSessionAsync()

                ' ''' <summary>$$
                Await state.AssertLineTextAroundCaretAsync("    ''' <summary>", "")
            End Using
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function CommitRemarksOnCloseAngle() As Task

            Using state = TestStateFactory.CreateVisualBasicTestState(
                <Document><![CDATA[
Class C
    ''' $$
    Sub Goo()
    End Sub
End Class
            ]]></Document>)

                state.SendInvokeCompletionList()
                Await state.AssertCompletionSessionAsync()
                state.SendTypeChars("rema")
                Await state.AssertSelectedCompletionItemAsync(displayText:="remarks")
                state.SendTypeChars(">")
                Await state.AssertNoCompletionSessionAsync()

                ' ''' remarks>$$
                Await state.AssertLineTextAroundCaretAsync("    ''' remarks>", "")
            End Using
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function InvokeWithOpenAngleCommitRemarksOnCloseAngle() As Task

            Using state = TestStateFactory.CreateVisualBasicTestState(
                <Document><![CDATA[
Class C
    ''' $$
    Sub Goo()
    End Sub
End Class
            ]]></Document>)

                state.SendTypeChars("<")
                Await state.AssertCompletionSessionAsync()
                state.SendTypeChars("rema")
                Await state.AssertSelectedCompletionItemAsync(displayText:="remarks")
                state.SendTypeChars(">")
                Await state.AssertNoCompletionSessionAsync()

                ' ''' <remarks>$$
                Await state.AssertLineTextAroundCaretAsync("    ''' <remarks>", "")
            End Using
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function CommitReturnsOnCloseAngle() As Task

            Using state = TestStateFactory.CreateVisualBasicTestState(
                <Document><![CDATA[
Class C
    ''' $$
    Function Goo() As Integer
    End Function
End Class
            ]]></Document>)

                state.SendInvokeCompletionList()
                Await state.AssertCompletionSessionAsync()
                state.SendTypeChars("retur")
                Await state.AssertSelectedCompletionItemAsync(displayText:="returns")
                state.SendTypeChars(">")
                Await state.AssertNoCompletionSessionAsync()

                ' ''' returns>$$
                Await state.AssertLineTextAroundCaretAsync("    ''' returns>", "")
            End Using
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function InvokeWithOpenAngleCommitReturnsOnCloseAngle() As Task

            Using state = TestStateFactory.CreateVisualBasicTestState(
                <Document><![CDATA[
Class C
    ''' $$
    Function Goo() As Integer
    End Function
End Class
            ]]></Document>)

                state.SendTypeChars("<")
                Await state.AssertCompletionSessionAsync()
                state.SendTypeChars("retur")
                Await state.AssertSelectedCompletionItemAsync(displayText:="returns")
                state.SendTypeChars(">")
                Await state.AssertNoCompletionSessionAsync()

                ' ''' <returns>$$
                Await state.AssertLineTextAroundCaretAsync("    ''' <returns>", "")
            End Using
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function CommitExampleOnCloseAngle() As Task

            Using state = TestStateFactory.CreateVisualBasicTestState(
                <Document><![CDATA[
Class C
    ''' $$
    Sub Goo()
    End Sub
End Class
            ]]></Document>)

                state.SendInvokeCompletionList()
                Await state.AssertCompletionSessionAsync()
                state.SendTypeChars("examp")
                Await state.AssertSelectedCompletionItemAsync(displayText:="example")
                state.SendTypeChars(">")
                Await state.AssertNoCompletionSessionAsync()

                ' ''' example>$$
                Await state.AssertLineTextAroundCaretAsync("    ''' example>", "")
            End Using
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function InvokeWithOpenAngleCommitExampleOnCloseAngle() As Task

            Using state = TestStateFactory.CreateVisualBasicTestState(
                <Document><![CDATA[
Class C
    ''' $$
    Sub Goo()
    End Sub
End Class
            ]]></Document>)

                state.SendTypeChars("<")
                Await state.AssertCompletionSessionAsync()
                state.SendTypeChars("examp")
                Await state.AssertSelectedCompletionItemAsync(displayText:="example")
                state.SendTypeChars(">")
                Await state.AssertNoCompletionSessionAsync()

                ' ''' <example>$$
                Await state.AssertLineTextAroundCaretAsync("    ''' <example>", "")
            End Using
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function CommitExceptionNoOpenAngle() As Task

            Using state = TestStateFactory.CreateVisualBasicTestState(
                <Document><![CDATA[
Class C
    ''' $$
    Sub Goo()
    End Sub
End Class
            ]]></Document>)

                state.SendInvokeCompletionList()
                Await state.AssertCompletionSessionAsync()
                state.SendTypeChars("except")
                Await state.AssertSelectedCompletionItemAsync(displayText:="exception")
                state.SendReturn()
                Await state.AssertNoCompletionSessionAsync()

                ' ''' <exception cref="$$"
                Await state.AssertLineTextAroundCaretAsync("    ''' <exception cref=""", """")
            End Using
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function InvokeWithOpenAngleCommitExceptionOnCloseAngle() As Task

            Using state = TestStateFactory.CreateVisualBasicTestState(
                <Document><![CDATA[
Class C
    ''' $$
    Sub Goo()
    End Sub
End Class
            ]]></Document>)

                state.SendTypeChars("<")
                Await state.AssertCompletionSessionAsync()
                state.SendTypeChars("except")
                Await state.AssertSelectedCompletionItemAsync(displayText:="exception")
                state.SendTypeChars(">")
                Await state.AssertNoCompletionSessionAsync()

                ' ''' <exception cref=">$$"
                Await state.AssertLineTextAroundCaretAsync("    ''' <exception cref="">", """")
            End Using
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function CommitCommentNoOpenAngle() As Task
            Using state = TestStateFactory.CreateVisualBasicTestState(
                <Document><![CDATA[
Class C
    ''' $$
    Sub Goo()
    End Sub
End Class
            ]]></Document>)

                state.SendInvokeCompletionList()
                Await state.AssertCompletionSessionAsync()
                state.SendSelectCompletionItem("!--")
                state.SendReturn()
                Await state.AssertNoCompletionSessionAsync()

                ' ''' <!--$$-->
                Await state.AssertLineTextAroundCaretAsync("    ''' <!--", "-->")
            End Using
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function InvokeWithOpenAngleCommitCommentOnCloseAngle() As Task
            Using state = TestStateFactory.CreateVisualBasicTestState(
                <Document><![CDATA[
Class C
    ''' $$
    Sub Goo()
    End Sub
End Class
            ]]></Document>)

                state.SendTypeChars("<")
                Await state.AssertCompletionSessionAsync()
                state.SendSelectCompletionItem("!--")
                state.SendTypeChars(">")
                Await state.AssertNoCompletionSessionAsync()

                ' ''' <!-->$$-->
                Await state.AssertLineTextAroundCaretAsync("    ''' <!-->", "-->")
            End Using
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function CommitCdataNoOpenAngle() As Task
            Using state = TestStateFactory.CreateVisualBasicTestState(
                <Document><![CDATA[
Class C
    ''' $$
    Sub Goo()
    End Sub
End Class
            ]]></Document>)

                state.SendInvokeCompletionList()
                Await state.AssertCompletionSessionAsync()
                state.SendSelectCompletionItem("![CDATA[")
                state.SendReturn()
                Await state.AssertNoCompletionSessionAsync()

                ' ''' <![CDATA[$$]]>
                Await state.AssertLineTextAroundCaretAsync("    ''' <![CDATA[", "]]>")
            End Using
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function InvokeWithOpenAngleCommitCdataOnCloseAngle() As Task
            Using state = TestStateFactory.CreateVisualBasicTestState(
                <Document><![CDATA[
Class C
    ''' $$
    Sub Goo()
    End Sub
End Class
            ]]></Document>)

                state.SendTypeChars("<")
                Await state.AssertCompletionSessionAsync()
                state.SendSelectCompletionItem("![CDATA[")
                state.SendTypeChars(">")
                Await state.AssertNoCompletionSessionAsync()

                ' ''' <![CDATA[>$$]]>
                Await state.AssertLineTextAroundCaretAsync("    ''' <![CDATA[>", "]]>")
            End Using
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function CommitIncludeNoOpenAngle() As Task

            Using state = TestStateFactory.CreateVisualBasicTestState(
                <Document><![CDATA[
Class C
    ''' $$
    Sub Goo()
    End Sub
End Class
            ]]></Document>)

                state.SendInvokeCompletionList()
                Await state.AssertCompletionSessionAsync()
                state.SendTypeChars("inclu")
                Await state.AssertSelectedCompletionItemAsync(displayText:="include")
                state.SendReturn()
                Await state.AssertNoCompletionSessionAsync()

                ' ''' <include file='$$' path='[@name=""]'/>
                Await state.AssertLineTextAroundCaretAsync("    ''' <include file='", "' path='[@name=""""]'/>")
            End Using
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function InvokeWithOpenAngleCommitIncludeOnCloseAngle() As Task

            Using state = TestStateFactory.CreateVisualBasicTestState(
                <Document><![CDATA[
Class C
    ''' $$
    Sub Goo()
    End Sub
End Class
            ]]></Document>)

                state.SendTypeChars("<")
                Await state.AssertCompletionSessionAsync()
                state.SendTypeChars("inclu")
                Await state.AssertSelectedCompletionItemAsync(displayText:="include")
                state.SendTypeChars(">")
                Await state.AssertNoCompletionSessionAsync()

                ' ''' <include file='>$$' path='[@name=""]'/>
                Await state.AssertLineTextAroundCaretAsync("    ''' <include file='>", "' path='[@name=""""]'/>")
            End Using
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function CommitPermissionNoOpenAngle() As Task

            Using state = TestStateFactory.CreateVisualBasicTestState(
                <Document><![CDATA[
Class C
    ''' $$
    Sub Goo()
    End Sub
End Class
            ]]></Document>)

                state.SendInvokeCompletionList()
                Await state.AssertCompletionSessionAsync()
                state.SendTypeChars("permiss")
                Await state.AssertSelectedCompletionItemAsync(displayText:="permission")
                state.SendReturn()
                Await state.AssertNoCompletionSessionAsync()

                ' ''' <permission cref="$$"
                Await state.AssertLineTextAroundCaretAsync("    ''' <permission cref=""", """")
            End Using
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function InvokeWithOpenAngleCommitPermissionOnCloseAngle() As Task

            Using state = TestStateFactory.CreateVisualBasicTestState(
                <Document><![CDATA[
Class C
    ''' $$
    Sub Goo()
    End Sub
End Class
            ]]></Document>)

                state.SendTypeChars("<")
                Await state.AssertCompletionSessionAsync()
                state.SendTypeChars("permiss")
                Await state.AssertSelectedCompletionItemAsync(displayText:="permission")
                state.SendTypeChars(">")
                Await state.AssertNoCompletionSessionAsync()

                ' ''' <permission cref=">$$"
                Await state.AssertLineTextAroundCaretAsync("    ''' <permission cref="">", """")
            End Using
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function CommitSeeNoOpenAngle() As Task

            Using state = TestStateFactory.CreateVisualBasicTestState(
                <Document><![CDATA[
Class C
    ''' $$
    Sub Goo()
    End Sub
End Class
            ]]></Document>)

                state.SendInvokeCompletionList()
                Await state.AssertCompletionSessionAsync()
                state.SendTypeChars("see")
                Await state.AssertSelectedCompletionItemAsync(displayText:="see")
                state.SendReturn()
                Await state.AssertNoCompletionSessionAsync()

                ' ''' <see cref="$$"/>
                Await state.AssertLineTextAroundCaretAsync("    ''' <see cref=""", """/>")
            End Using
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function InvokeWithOpenAngleCommitSeeOnCloseAngle() As Task

            Using state = TestStateFactory.CreateVisualBasicTestState(
                <Document><![CDATA[
Class C
    ''' $$
    Sub Goo()
    End Sub
End Class
            ]]></Document>)

                state.SendTypeChars("<")
                Await state.AssertCompletionSessionAsync()
                state.SendTypeChars("see")
                Await state.AssertSelectedCompletionItemAsync(displayText:="see")
                state.SendTypeChars(">")
                Await state.AssertNoCompletionSessionAsync()

                ' ''' <see cref=">$$"/>
                Await state.AssertLineTextAroundCaretAsync("    ''' <see cref="">", """/>")
            End Using
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function InvokeWithOpenAngleCommitSeeOnSpace() As Task
            Using state = TestStateFactory.CreateVisualBasicTestState(
                <Document><![CDATA[
Class C
    ''' <summary>
    ''' $$
    ''' </summary>
    Sub Goo()
    End Sub
End Class
            ]]></Document>)

                state.SendTypeChars("<")
                Await state.AssertCompletionSessionAsync()
                state.SendTypeChars("see")
                Await state.AssertSelectedCompletionItemAsync(displayText:="see")
                state.SendTypeChars(" ")

                ' ''' <see cref="$$"/>
                Await state.AssertLineTextAroundCaretAsync("    ''' <see cref=""", """/>")
            End Using
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Function InvokeWithNothingKeywordCommitSeeLangword() As Task
            Return InvokeWithKeywordCommitSeeLangwordAsync("Nothing")
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Function InvokeWithSharedKeywordCommitSeeLangword() As Task
            Return InvokeWithKeywordCommitSeeLangwordAsync("Shared")
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Function InvokeWithOverridableKeywordCommitSeeLangword() As Task
            Return InvokeWithKeywordCommitSeeLangwordAsync("Overridable", unique:=False)
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Function InvokeWithTrueKeywordCommitSeeLangword() As Task
            Return InvokeWithKeywordCommitSeeLangwordAsync("True")
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Function InvokeWithFalseKeywordCommitSeeLangword() As Task
            Return InvokeWithKeywordCommitSeeLangwordAsync("False")
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Function InvokeWithMustInheritKeywordCommitSeeLangword() As Task
            Return InvokeWithKeywordCommitSeeLangwordAsync("MustInherit")
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Function InvokeWithNotOverridableKeywordCommitSeeLangword() As Task
            Return InvokeWithKeywordCommitSeeLangwordAsync("NotOverridable")
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Function InvokeWithAsyncKeywordCommitSeeLangword() As Task
            Return InvokeWithKeywordCommitSeeLangwordAsync("Async")
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Function InvokeWithAwaitKeywordCommitSeeLangword() As Task
            Return InvokeWithKeywordCommitSeeLangwordAsync("Await")
        End Function

        Private Shared Async Function InvokeWithKeywordCommitSeeLangwordAsync(keyword As String, Optional unique As Boolean = True) As Task
            Using state = TestStateFactory.CreateVisualBasicTestState(
                <Document><![CDATA[
Class C
    ''' <summary>
    ''' $$
    ''' </summary>
    Sub Goo()
    End Sub
End Class
            ]]></Document>)

                ' Omit the last letter of the keyword to make it easier to diagnose failures (inserted the wrong text,
                ' or did not insert text at all).
                state.SendTypeChars(keyword.Substring(0, keyword.Length - 1))
                state.SendInvokeCompletionList()
                If unique Then
                    Await state.SendCommitUniqueCompletionListItemAsync()
                Else
                    Await state.AssertSelectedCompletionItemAsync(displayText:=keyword)
                    state.SendTab()
                End If

                Await state.AssertNoCompletionSessionAsync()

                ' ''' <see langword="keyword"/>$$
                Await state.AssertLineTextAroundCaretAsync("    ''' <see langword=""" + keyword + """/>", "")
            End Using
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function CommitSeealsoNoOpenAngle() As Task

            Using state = TestStateFactory.CreateVisualBasicTestState(
                <Document><![CDATA[
Class C
    ''' $$
    Sub Goo()
    End Sub
End Class
            ]]></Document>)

                state.SendInvokeCompletionList()
                Await state.AssertCompletionSessionAsync()
                state.SendTypeChars("seeal")
                Await state.AssertSelectedCompletionItemAsync(displayText:="seealso")
                state.SendReturn()
                Await state.AssertNoCompletionSessionAsync()

                ' ''' <seealso cref="$$"/>
                Await state.AssertLineTextAroundCaretAsync("    ''' <seealso cref=""", """/>")
            End Using
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function InvokeWithOpenAngleCommitSeealsoOnCloseAngle() As Task

            Using state = TestStateFactory.CreateVisualBasicTestState(
                <Document><![CDATA[
Class C
    ''' $$
    Sub Goo()
    End Sub
End Class
            ]]></Document>)

                state.SendTypeChars("<")
                Await state.AssertCompletionSessionAsync()
                state.SendTypeChars("seeal")
                Await state.AssertSelectedCompletionItemAsync(displayText:="seealso")
                state.SendTypeChars(">")
                Await state.AssertNoCompletionSessionAsync()

                ' ''' <seealso cref=">$$"/>
                Await state.AssertLineTextAroundCaretAsync("    ''' <seealso cref="">", """/>")
            End Using
        End Function

        <WorkItem(623219, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/623219")>
        <WorkItem(746919, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/746919")>
        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function CommitParam() As Task
            Using state = TestStateFactory.CreateVisualBasicTestState(
                <Document><![CDATA[
Class C(Of T)
    ''' <param$$
    Sub Goo(Of T)(bar As T)
    End Sub
End Class
            ]]></Document>)

                state.SendInvokeCompletionList()
                Await state.AssertCompletionSessionAsync()
                Await state.AssertSelectedCompletionItemAsync(displayText:="param name=""bar""")
                state.SendReturn()
                Await state.AssertNoCompletionSessionAsync()

                ' ''' <param name="bar"$$
                Await state.AssertLineTextAroundCaretAsync("    ''' <param name=""bar""", "")
            End Using
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function CommitParamNoOpenAngle() As Task

            Using state = TestStateFactory.CreateVisualBasicTestState(
                <Document><![CDATA[
Class C(Of T)
    ''' $$
    Sub Goo(Of T)(bar As T)
    End Sub
End Class
            ]]></Document>)

                state.SendInvokeCompletionList()
                Await state.AssertCompletionSessionAsync()
                state.SendTypeChars("param")
                Await state.AssertSelectedCompletionItemAsync(displayText:="param name=""bar""")
                state.SendReturn()
                Await state.AssertNoCompletionSessionAsync()

                ' ''' param name="bar"$$
                Await state.AssertLineTextAroundCaretAsync("    ''' param name=""bar""", "")
            End Using
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function CommitParamNoOpenAngleOnTab() As Task

            Using state = TestStateFactory.CreateVisualBasicTestState(
                <Document><![CDATA[
Class C(Of T)
    ''' $$
    Sub Goo(Of T)(bar As T)
    End Sub
End Class
            ]]></Document>)

                state.SendInvokeCompletionList()
                Await state.AssertCompletionSessionAsync()
                state.SendTypeChars("param")
                Await state.AssertSelectedCompletionItemAsync(displayText:="param name=""bar""")
                state.SendTab()
                Await state.AssertNoCompletionSessionAsync()

                ' ''' param name="bar"$$
                Await state.AssertLineTextAroundCaretAsync("    ''' param name=""bar""", "")
            End Using
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function CommitParamNoOpenAngleOnCloseAngle() As Task

            Using state = TestStateFactory.CreateVisualBasicTestState(
                <Document><![CDATA[
Class C(Of T)
    ''' $$
    Sub Goo(Of T)(bar As T)
    End Sub
End Class
            ]]></Document>)

                state.SendInvokeCompletionList()
                Await state.AssertCompletionSessionAsync()
                state.SendTypeChars("param")
                Await state.AssertSelectedCompletionItemAsync(displayText:="param name=""bar""")
                state.SendTypeChars(">")
                Await state.AssertNoCompletionSessionAsync()

                ' ''' param name="bar">$$
                Await state.AssertLineTextAroundCaretAsync("    ''' param name=""bar"">", "")
            End Using
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function InvokeWithOpenAngleCommitParam() As Task

            Using state = TestStateFactory.CreateVisualBasicTestState(
                <Document><![CDATA[
Class C(Of T)
    ''' $$
    Sub Goo(Of T)(bar As T)
    End Sub
End Class
            ]]></Document>)

                state.SendTypeChars("<")
                Await state.AssertCompletionSessionAsync()
                state.SendTypeChars("param")
                Await state.AssertSelectedCompletionItemAsync(displayText:="param name=""bar""")
                state.SendReturn()
                Await state.AssertNoCompletionSessionAsync()

                ' ''' <param name="bar"$$
                Await state.AssertLineTextAroundCaretAsync("    ''' <param name=""bar""", "")
            End Using
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function InvokeWithOpenAngleCommitParamOnTab() As Task

            Using state = TestStateFactory.CreateVisualBasicTestState(
                <Document><![CDATA[
Class C(Of T)
    ''' $$
    Sub Goo(Of T)(bar As T)
    End Sub
End Class
            ]]></Document>)

                state.SendTypeChars("<")
                Await state.AssertCompletionSessionAsync()
                state.SendTypeChars("param")
                Await state.AssertSelectedCompletionItemAsync(displayText:="param name=""bar""")
                state.SendTab()
                Await state.AssertNoCompletionSessionAsync()

                ' ''' <param name="bar"$$
                Await state.AssertLineTextAroundCaretAsync("    ''' <param name=""bar""", "")
            End Using
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function InvokeWithOpenAngleCommitParamOnCloseAngle() As Task

            Using state = TestStateFactory.CreateVisualBasicTestState(
                <Document><![CDATA[
Class C(Of T)
    ''' $$
    Sub Goo(Of T)(bar As T)
    End Sub
End Class
            ]]></Document>)

                state.SendTypeChars("<")
                Await state.AssertCompletionSessionAsync()
                state.SendTypeChars("param")
                Await state.AssertSelectedCompletionItemAsync(displayText:="param name=""bar""")
                state.SendTypeChars(">")
                Await state.AssertNoCompletionSessionAsync()

                ' ''' <param name="bar">$$
                Await state.AssertLineTextAroundCaretAsync("    ''' <param name=""bar"">", "")
            End Using
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function CommitTypeparamNoOpenAngle() As Task

            Using state = TestStateFactory.CreateVisualBasicTestState(
                <Document><![CDATA[
Class C(Of T)
    ''' $$
    Sub Goo(Of T)(bar As T)
    End Sub
End Class
            ]]></Document>)

                state.SendInvokeCompletionList()
                Await state.AssertCompletionSessionAsync()
                state.SendTypeChars("typepara")
                Await state.AssertSelectedCompletionItemAsync(displayText:="typeparam name=""T""")
                state.SendReturn()
                Await state.AssertNoCompletionSessionAsync()

                ' ''' typeparam name="T"$$
                Await state.AssertLineTextAroundCaretAsync("    ''' typeparam name=""T""", "")
            End Using
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function CommitTypeparamNoOpenAngleOnTab() As Task

            Using state = TestStateFactory.CreateVisualBasicTestState(
                <Document><![CDATA[
Class C(Of T)
    ''' $$
    Sub Goo(Of T)(bar As T)
    End Sub
End Class
            ]]></Document>)

                state.SendInvokeCompletionList()
                Await state.AssertCompletionSessionAsync()
                state.SendTypeChars("typepara")
                Await state.AssertSelectedCompletionItemAsync(displayText:="typeparam name=""T""")
                state.SendTab()
                Await state.AssertNoCompletionSessionAsync()

                ' ''' typeparam name="T"$$
                Await state.AssertLineTextAroundCaretAsync("    ''' typeparam name=""T""", "")
            End Using
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function CommitTypeparamNoOpenAngleOnCloseAngle() As Task

            Using state = TestStateFactory.CreateVisualBasicTestState(
                <Document><![CDATA[
Class C(Of T)
    ''' $$
    Sub Goo(Of T)(bar As T)
    End Sub
End Class
            ]]></Document>)

                state.SendInvokeCompletionList()
                Await state.AssertCompletionSessionAsync()
                state.SendTypeChars("typepara")
                Await state.AssertSelectedCompletionItemAsync(displayText:="typeparam name=""T""")
                state.SendTypeChars(">")
                Await state.AssertNoCompletionSessionAsync()

                ' ''' typeparam name="T">$$
                Await state.AssertLineTextAroundCaretAsync("    ''' typeparam name=""T"">", "")
            End Using
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function InvokeWithOpenAngleCommitTypeparam() As Task

            Using state = TestStateFactory.CreateVisualBasicTestState(
                <Document><![CDATA[
Class C(Of T)
    ''' $$
    Sub Goo(Of T)(bar As T)
    End Sub
End Class
            ]]></Document>)

                state.SendTypeChars("<")
                Await state.AssertCompletionSessionAsync()
                state.SendTypeChars("typepara")
                Await state.AssertSelectedCompletionItemAsync(displayText:="typeparam name=""T""")
                state.SendReturn()
                Await state.AssertNoCompletionSessionAsync()

                ' ''' <typeparam name="T"$$
                Await state.AssertLineTextAroundCaretAsync("    ''' <typeparam name=""T""", "")
            End Using
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function InvokeWithOpenAngleCommitTypeparamOnTab() As Task

            Using state = TestStateFactory.CreateVisualBasicTestState(
                <Document><![CDATA[
Class C(Of T)
    ''' $$
    Sub Goo(Of T)(bar As T)
    End Sub
End Class
            ]]></Document>)

                state.SendTypeChars("<")
                Await state.AssertCompletionSessionAsync()
                state.SendTypeChars("typepara")
                Await state.AssertSelectedCompletionItemAsync(displayText:="typeparam name=""T""")
                state.SendTab()
                Await state.AssertNoCompletionSessionAsync()

                ' ''' <typeparam name="T"$$
                Await state.AssertLineTextAroundCaretAsync("    ''' <typeparam name=""T""", "")
            End Using
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function InvokeWithOpenAngleCommitTypeparamOnCloseAngle() As Task

            Using state = TestStateFactory.CreateVisualBasicTestState(
                <Document><![CDATA[
Class C(Of T)
    ''' $$
    Sub Goo(Of T)(bar As T)
    End Sub
End Class
            ]]></Document>)

                state.SendTypeChars("<")
                Await state.AssertCompletionSessionAsync()
                state.SendTypeChars("typepara")
                Await state.AssertSelectedCompletionItemAsync(displayText:="typeparam name=""T""")
                state.SendTypeChars(">")
                Await state.AssertNoCompletionSessionAsync()

                ' ''' <typeparam name="T">$$
                Await state.AssertLineTextAroundCaretAsync("    ''' <typeparam name=""T"">", "")
            End Using
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function CommitList() As Task

            Using state = TestStateFactory.CreateVisualBasicTestState(
                <Document><![CDATA[
Class C(Of T)
    ''' <summary>
    ''' $$
    ''' </summary>
    Sub Goo(Of T)(bar As T)
    End Sub
End Class
            ]]></Document>)

                state.SendInvokeCompletionList()
                Await state.AssertCompletionSessionAsync()
                state.SendTypeChars("lis")
                Await state.AssertSelectedCompletionItemAsync(displayText:="list")
                state.SendReturn()
                Await state.AssertNoCompletionSessionAsync()

                ' ''' <list type="$$"
                Await state.AssertLineTextAroundCaretAsync("    ''' <list type=""", """")
            End Using
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function CommitListOnCloseAngle() As Task

            Using state = TestStateFactory.CreateVisualBasicTestState(
                <Document><![CDATA[
Class C(Of T)
    ''' <summary>
    ''' $$
    ''' </summary>
    Sub Goo(Of T)(bar As T)
    End Sub
End Class
            ]]></Document>)

                state.SendInvokeCompletionList()
                Await state.AssertCompletionSessionAsync()
                state.SendTypeChars("lis")
                Await state.AssertSelectedCompletionItemAsync(displayText:="list")
                state.SendTypeChars(">")
                Await state.AssertNoCompletionSessionAsync()

                ' ''' <list type=">$$"
                Await state.AssertLineTextAroundCaretAsync("    ''' <list type="">", """")
            End Using
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function TestTagCompletion1() As Task

            Using state = TestStateFactory.CreateVisualBasicTestState(
                <Document><![CDATA[
Class C(Of T)
    ''' <$$
    ''' </summary>
    Sub Goo(Of T)(bar As T)
    End Sub
End Class
            ]]></Document>)

                state.SendInvokeCompletionList()
                Await state.AssertCompletionSessionAsync()
                state.SendTypeChars("summa")
                Await state.AssertSelectedCompletionItemAsync(displayText:="summary")
                state.SendTypeChars(">")
                Await state.AssertNoCompletionSessionAsync()

                ' ''' <summary>$$
                Await state.AssertLineTextAroundCaretAsync("    ''' <summary>", "")
            End Using
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function TestTagCompletion2() As Task

            Using state = TestStateFactory.CreateVisualBasicTestState(
                <Document><![CDATA[
Class C(Of T)
    ''' <$$
    ''' <remarks></remarks>
    ''' </summary>
    Sub Goo(Of T)(bar As T)
    End Sub
End Class
            ]]></Document>)

                state.SendInvokeCompletionList()
                Await state.AssertCompletionSessionAsync()
                state.SendTypeChars("summa")
                Await state.AssertSelectedCompletionItemAsync(displayText:="summary")
                state.SendTypeChars(">")
                Await state.AssertNoCompletionSessionAsync()

                ' ''' <summary>$$
                Await state.AssertLineTextAroundCaretAsync("    ''' <summary>", "")
            End Using
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function TestTagCompletion3() As Task

            Using state = TestStateFactory.CreateVisualBasicTestState(
                <Document><![CDATA[
Class C(Of T)
    ''' <$$
    ''' <remarks>
    ''' </summary>
    Sub Goo(Of T)(bar As T)
    End Sub
End Class
            ]]></Document>)

                state.SendInvokeCompletionList()
                Await state.AssertCompletionSessionAsync()
                state.SendTypeChars("summa")
                Await state.AssertSelectedCompletionItemAsync(displayText:="summary")
                state.SendTypeChars(">")
                Await state.AssertNoCompletionSessionAsync()

                ' ''' <summary>$$
                Await state.AssertLineTextAroundCaretAsync("    ''' <summary>", "")
            End Using
        End Function

        <WorkItem(638653, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/638653")>
        <WpfFact(Skip:="https://github.com/dotnet/roslyn/issues/21481"), Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function AllowTypingDoubleQuote() As Task
            Using state = TestStateFactory.CreateVisualBasicTestState(
                <Document><![CDATA[
Class C(Of T)
    ''' <param$$
    Sub Goo(Of T)(bar as T)
    End Sub
End Class
            ]]></Document>)

                state.SendInvokeCompletionList()
                Await state.AssertCompletionSessionAsync()
                Await state.AssertSelectedCompletionItemAsync(displayText:="param name=""bar""")
                state.SendTypeChars(" name=""")

                ' ''' <param name="$$
                Await state.AssertLineTextAroundCaretAsync("    ''' <param name=""", "")

                ' Because the item contains a double quote, the completionImplementation list should still be present with the same selection
                Await state.AssertCompletionSessionAsync()
                Await state.AssertSelectedCompletionItemAsync(displayText:="param name=""bar""")
            End Using
        End Function

        <WorkItem(638653, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/638653")>
        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function AllowTypingSpace() As Task
            Using state = TestStateFactory.CreateVisualBasicTestState(
                <Document><![CDATA[
Class C(Of T)
    ''' <param$$
    Sub Goo(Of T)(bar as T)
    End Sub
End Class
            ]]></Document>)

                state.SendInvokeCompletionList()
                Await state.AssertCompletionSessionAsync()
                Await state.AssertSelectedCompletionItemAsync(displayText:="param name=""bar""")
                state.SendTypeChars(" ")

                ' ''' <param $$
                Await state.AssertLineTextAroundCaretAsync("    ''' <param ", "")

                ' Because the item contains a space, the completionImplementation list should still be present with the same selection
                Await state.AssertCompletionSessionAsync()
                Await state.AssertSelectedCompletionItemAsync(displayText:="param name=""bar""")
            End Using
        End Function
    End Class
End Namespace
