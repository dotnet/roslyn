' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

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
                Await state.AssertCompletionSession()
                state.SendTypeChars("summ")
                Await state.AssertSelectedCompletionItem(displayText:="summary")
                state.SendReturn()
                Await state.AssertNoCompletionSession()
                Await state.AssertLineTextAroundCaret("    ''' summary", "")
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
                Await state.AssertCompletionSession()
                state.SendTypeChars("summ")
                Await state.AssertSelectedCompletionItem(displayText:="summary")
                state.SendTab()
                Await state.AssertNoCompletionSession()

                ' ''' summary$$
                Await state.AssertLineTextAroundCaret("    ''' summary", "")
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
                Await state.AssertCompletionSession()
                state.SendTypeChars("summ")
                Await state.AssertSelectedCompletionItem(displayText:="summary")
                state.SendTypeChars(">")
                Await state.AssertNoCompletionSession()

                ' ''' summary>$$
                Await state.AssertLineTextAroundCaret("    ''' summary>", "")
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
                Await state.AssertCompletionSession()
                state.SendTypeChars("summ")
                Await state.AssertSelectedCompletionItem(displayText:="summary")
                state.SendReturn()
                Await state.AssertNoCompletionSession()

                ' ''' <summary$$
                Await state.AssertLineTextAroundCaret("    ''' <summary", "")
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
                Await state.AssertCompletionSession()
                state.SendTypeChars("summ")
                Await state.AssertSelectedCompletionItem(displayText:="summary")
                state.SendTab()
                Await state.AssertNoCompletionSession()

                ' ''' <summary$$
                Await state.AssertLineTextAroundCaret("    ''' <summary", "")
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
                Await state.AssertCompletionSession()
                state.SendTypeChars("summ")
                Await state.AssertSelectedCompletionItem(displayText:="summary")
                state.SendTypeChars(">")
                Await state.AssertNoCompletionSession()

                ' ''' <summary>$$
                Await state.AssertLineTextAroundCaret("    ''' <summary>", "")
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
                Await state.AssertCompletionSession()
                state.SendTypeChars("rema")
                Await state.AssertSelectedCompletionItem(displayText:="remarks")
                state.SendTypeChars(">")
                Await state.AssertNoCompletionSession()

                ' ''' remarks>$$
                Await state.AssertLineTextAroundCaret("    ''' remarks>", "")
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
                Await state.AssertCompletionSession()
                state.SendTypeChars("rema")
                Await state.AssertSelectedCompletionItem(displayText:="remarks")
                state.SendTypeChars(">")
                Await state.AssertNoCompletionSession()

                ' ''' <remarks>$$
                Await state.AssertLineTextAroundCaret("    ''' <remarks>", "")
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
                Await state.AssertCompletionSession()
                state.SendTypeChars("retur")
                Await state.AssertSelectedCompletionItem(displayText:="returns")
                state.SendTypeChars(">")
                Await state.AssertNoCompletionSession()

                ' ''' returns>$$
                Await state.AssertLineTextAroundCaret("    ''' returns>", "")
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
                Await state.AssertCompletionSession()
                state.SendTypeChars("retur")
                Await state.AssertSelectedCompletionItem(displayText:="returns")
                state.SendTypeChars(">")
                Await state.AssertNoCompletionSession()

                ' ''' <returns>$$
                Await state.AssertLineTextAroundCaret("    ''' <returns>", "")
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
                Await state.AssertCompletionSession()
                state.SendTypeChars("examp")
                Await state.AssertSelectedCompletionItem(displayText:="example")
                state.SendTypeChars(">")
                Await state.AssertNoCompletionSession()

                ' ''' example>$$
                Await state.AssertLineTextAroundCaret("    ''' example>", "")
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
                Await state.AssertCompletionSession()
                state.SendTypeChars("examp")
                Await state.AssertSelectedCompletionItem(displayText:="example")
                state.SendTypeChars(">")
                Await state.AssertNoCompletionSession()

                ' ''' <example>$$
                Await state.AssertLineTextAroundCaret("    ''' <example>", "")
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
                Await state.AssertCompletionSession()
                state.SendTypeChars("except")
                Await state.AssertSelectedCompletionItem(displayText:="exception")
                state.SendReturn()
                Await state.AssertNoCompletionSession()

                ' ''' <exception cref="$$"
                Await state.AssertLineTextAroundCaret("    ''' <exception cref=""", """")
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
                Await state.AssertCompletionSession()
                state.SendTypeChars("except")
                Await state.AssertSelectedCompletionItem(displayText:="exception")
                state.SendTypeChars(">")
                Await state.AssertNoCompletionSession()

                ' ''' <exception cref=">$$"
                Await state.AssertLineTextAroundCaret("    ''' <exception cref="">", """")
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
                Await state.AssertCompletionSession()
                state.SendSelectCompletionItem("!--")
                state.SendReturn()
                Await state.AssertNoCompletionSession()

                ' ''' <!--$$-->
                Await state.AssertLineTextAroundCaret("    ''' <!--", "-->")
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
                Await state.AssertCompletionSession()
                state.SendSelectCompletionItem("!--")
                state.SendTypeChars(">")
                Await state.AssertNoCompletionSession()

                ' ''' <!-->$$-->
                Await state.AssertLineTextAroundCaret("    ''' <!-->", "-->")
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
                Await state.AssertCompletionSession()
                state.SendSelectCompletionItem("![CDATA[")
                state.SendReturn()
                Await state.AssertNoCompletionSession()

                ' ''' <![CDATA[$$]]>
                Await state.AssertLineTextAroundCaret("    ''' <![CDATA[", "]]>")
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
                Await state.AssertCompletionSession()
                state.SendSelectCompletionItem("![CDATA[")
                state.SendTypeChars(">")
                Await state.AssertNoCompletionSession()

                ' ''' <![CDATA[>$$]]>
                Await state.AssertLineTextAroundCaret("    ''' <![CDATA[>", "]]>")
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
                Await state.AssertCompletionSession()
                state.SendTypeChars("inclu")
                Await state.AssertSelectedCompletionItem(displayText:="include")
                state.SendReturn()
                Await state.AssertNoCompletionSession()

                ' ''' <include file='$$' path='[@name=""]'/>
                Await state.AssertLineTextAroundCaret("    ''' <include file='", "' path='[@name=""""]'/>")
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
                Await state.AssertCompletionSession()
                state.SendTypeChars("inclu")
                Await state.AssertSelectedCompletionItem(displayText:="include")
                state.SendTypeChars(">")
                Await state.AssertNoCompletionSession()

                ' ''' <include file='>$$' path='[@name=""]'/>
                Await state.AssertLineTextAroundCaret("    ''' <include file='>", "' path='[@name=""""]'/>")
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
                Await state.AssertCompletionSession()
                state.SendTypeChars("permiss")
                Await state.AssertSelectedCompletionItem(displayText:="permission")
                state.SendReturn()
                Await state.AssertNoCompletionSession()

                ' ''' <permission cref="$$"
                Await state.AssertLineTextAroundCaret("    ''' <permission cref=""", """")
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
                Await state.AssertCompletionSession()
                state.SendTypeChars("permiss")
                Await state.AssertSelectedCompletionItem(displayText:="permission")
                state.SendTypeChars(">")
                Await state.AssertNoCompletionSession()

                ' ''' <permission cref=">$$"
                Await state.AssertLineTextAroundCaret("    ''' <permission cref="">", """")
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
                Await state.AssertCompletionSession()
                state.SendTypeChars("see")
                Await state.AssertSelectedCompletionItem(displayText:="see")
                state.SendReturn()
                Await state.AssertNoCompletionSession()

                ' ''' <see cref="$$"/>
                Await state.AssertLineTextAroundCaret("    ''' <see cref=""", """/>")
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
                Await state.AssertCompletionSession()
                state.SendTypeChars("see")
                Await state.AssertSelectedCompletionItem(displayText:="see")
                state.SendTypeChars(">")
                Await state.AssertNoCompletionSession()

                ' ''' <see cref=">$$"/>
                Await state.AssertLineTextAroundCaret("    ''' <see cref="">", """/>")
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
                Await state.AssertCompletionSession()
                state.SendTypeChars("see")
                Await state.AssertSelectedCompletionItem(displayText:="see")
                state.SendTypeChars(" ")

                ' ''' <see cref="$$"/>
                Await state.AssertLineTextAroundCaret("    ''' <see cref=""", """/>")
            End Using
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Function InvokeWithNothingKeywordCommitSeeLangword() As Task
            Return InvokeWithKeywordCommitSeeLangword("Nothing")
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Function InvokeWithSharedKeywordCommitSeeLangword() As Task
            Return InvokeWithKeywordCommitSeeLangword("Shared")
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Function InvokeWithOverridableKeywordCommitSeeLangword() As Task
            Return InvokeWithKeywordCommitSeeLangword("Overridable", unique:=False)
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Function InvokeWithTrueKeywordCommitSeeLangword() As Task
            Return InvokeWithKeywordCommitSeeLangword("True")
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Function InvokeWithFalseKeywordCommitSeeLangword() As Task
            Return InvokeWithKeywordCommitSeeLangword("False")
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Function InvokeWithMustInheritKeywordCommitSeeLangword() As Task
            Return InvokeWithKeywordCommitSeeLangword("MustInherit")
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Function InvokeWithNotOverridableKeywordCommitSeeLangword() As Task
            Return InvokeWithKeywordCommitSeeLangword("NotOverridable")
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Function InvokeWithAsyncKeywordCommitSeeLangword() As Task
            Return InvokeWithKeywordCommitSeeLangword("Async")
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Function InvokeWithAwaitKeywordCommitSeeLangword() As Task
            Return InvokeWithKeywordCommitSeeLangword("Await")
        End Function

        Private Async Function InvokeWithKeywordCommitSeeLangword(keyword As String, Optional unique As Boolean = True) As Task
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
                    Await state.AssertSelectedCompletionItem(displayText:=keyword)
                    state.SendTab()
                End If
                Await state.AssertNoCompletionSession()

                ' ''' <see langword="keyword"/>$$
                Await state.AssertLineTextAroundCaret("    ''' <see langword=""" + keyword + """/>", "")
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
                Await state.AssertCompletionSession()
                state.SendTypeChars("seeal")
                Await state.AssertSelectedCompletionItem(displayText:="seealso")
                state.SendReturn()
                Await state.AssertNoCompletionSession()

                ' ''' <seealso cref="$$"/>
                Await state.AssertLineTextAroundCaret("    ''' <seealso cref=""", """/>")
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
                Await state.AssertCompletionSession()
                state.SendTypeChars("seeal")
                Await state.AssertSelectedCompletionItem(displayText:="seealso")
                state.SendTypeChars(">")
                Await state.AssertNoCompletionSession()

                ' ''' <seealso cref=">$$"/>
                Await state.AssertLineTextAroundCaret("    ''' <seealso cref="">", """/>")
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
                Await state.AssertCompletionSession()
                Await state.AssertSelectedCompletionItem(displayText:="param name=""bar""")
                state.SendReturn()
                Await state.AssertNoCompletionSession()

                ' ''' <param name="bar"$$
                Await state.AssertLineTextAroundCaret("    ''' <param name=""bar""", "")
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
                Await state.AssertCompletionSession()
                state.SendTypeChars("param")
                Await state.AssertSelectedCompletionItem(displayText:="param name=""bar""")
                state.SendReturn()
                Await state.AssertNoCompletionSession()

                ' ''' param name="bar"$$
                Await state.AssertLineTextAroundCaret("    ''' param name=""bar""", "")
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
                Await state.AssertCompletionSession()
                state.SendTypeChars("param")
                Await state.AssertSelectedCompletionItem(displayText:="param name=""bar""")
                state.SendTab()
                Await state.AssertNoCompletionSession()

                ' ''' param name="bar"$$
                Await state.AssertLineTextAroundCaret("    ''' param name=""bar""", "")
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
                Await state.AssertCompletionSession()
                state.SendTypeChars("param")
                Await state.AssertSelectedCompletionItem(displayText:="param name=""bar""")
                state.SendTypeChars(">")
                Await state.AssertNoCompletionSession()

                ' ''' param name="bar">$$
                Await state.AssertLineTextAroundCaret("    ''' param name=""bar"">", "")
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
                Await state.AssertCompletionSession()
                state.SendTypeChars("param")
                Await state.AssertSelectedCompletionItem(displayText:="param name=""bar""")
                state.SendReturn()
                Await state.AssertNoCompletionSession()

                ' ''' <param name="bar"$$
                Await state.AssertLineTextAroundCaret("    ''' <param name=""bar""", "")
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
                Await state.AssertCompletionSession()
                state.SendTypeChars("param")
                Await state.AssertSelectedCompletionItem(displayText:="param name=""bar""")
                state.SendTab()
                Await state.AssertNoCompletionSession()

                ' ''' <param name="bar"$$
                Await state.AssertLineTextAroundCaret("    ''' <param name=""bar""", "")
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
                Await state.AssertCompletionSession()
                state.SendTypeChars("param")
                Await state.AssertSelectedCompletionItem(displayText:="param name=""bar""")
                state.SendTypeChars(">")
                Await state.AssertNoCompletionSession()

                ' ''' <param name="bar">$$
                Await state.AssertLineTextAroundCaret("    ''' <param name=""bar"">", "")
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
                Await state.AssertCompletionSession()
                state.SendTypeChars("typepara")
                Await state.AssertSelectedCompletionItem(displayText:="typeparam name=""T""")
                state.SendReturn()
                Await state.AssertNoCompletionSession()

                ' ''' typeparam name="T"$$
                Await state.AssertLineTextAroundCaret("    ''' typeparam name=""T""", "")
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
                Await state.AssertCompletionSession()
                state.SendTypeChars("typepara")
                Await state.AssertSelectedCompletionItem(displayText:="typeparam name=""T""")
                state.SendTab()
                Await state.AssertNoCompletionSession()

                ' ''' typeparam name="T"$$
                Await state.AssertLineTextAroundCaret("    ''' typeparam name=""T""", "")
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
                Await state.AssertCompletionSession()
                state.SendTypeChars("typepara")
                Await state.AssertSelectedCompletionItem(displayText:="typeparam name=""T""")
                state.SendTypeChars(">")
                Await state.AssertNoCompletionSession()

                ' ''' typeparam name="T">$$
                Await state.AssertLineTextAroundCaret("    ''' typeparam name=""T"">", "")
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
                Await state.AssertCompletionSession()
                state.SendTypeChars("typepara")
                Await state.AssertSelectedCompletionItem(displayText:="typeparam name=""T""")
                state.SendReturn()
                Await state.AssertNoCompletionSession()

                ' ''' <typeparam name="T"$$
                Await state.AssertLineTextAroundCaret("    ''' <typeparam name=""T""", "")
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
                Await state.AssertCompletionSession()
                state.SendTypeChars("typepara")
                Await state.AssertSelectedCompletionItem(displayText:="typeparam name=""T""")
                state.SendTab()
                Await state.AssertNoCompletionSession()

                ' ''' <typeparam name="T"$$
                Await state.AssertLineTextAroundCaret("    ''' <typeparam name=""T""", "")
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
                Await state.AssertCompletionSession()
                state.SendTypeChars("typepara")
                Await state.AssertSelectedCompletionItem(displayText:="typeparam name=""T""")
                state.SendTypeChars(">")
                Await state.AssertNoCompletionSession()

                ' ''' <typeparam name="T">$$
                Await state.AssertLineTextAroundCaret("    ''' <typeparam name=""T"">", "")
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
                Await state.AssertCompletionSession()
                state.SendTypeChars("lis")
                Await state.AssertSelectedCompletionItem(displayText:="list")
                state.SendReturn()
                Await state.AssertNoCompletionSession()

                ' ''' <list type="$$"
                Await state.AssertLineTextAroundCaret("    ''' <list type=""", """")
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
                Await state.AssertCompletionSession()
                state.SendTypeChars("lis")
                Await state.AssertSelectedCompletionItem(displayText:="list")
                state.SendTypeChars(">")
                Await state.AssertNoCompletionSession()

                ' ''' <list type=">$$"
                Await state.AssertLineTextAroundCaret("    ''' <list type="">", """")
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
                Await state.AssertCompletionSession()
                state.SendTypeChars("summa")
                Await state.AssertSelectedCompletionItem(displayText:="summary")
                state.SendTypeChars(">")
                Await state.AssertNoCompletionSession()

                ' ''' <summary>$$
                Await state.AssertLineTextAroundCaret("    ''' <summary>", "")
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
                Await state.AssertCompletionSession()
                state.SendTypeChars("summa")
                Await state.AssertSelectedCompletionItem(displayText:="summary")
                state.SendTypeChars(">")
                Await state.AssertNoCompletionSession()

                ' ''' <summary>$$
                Await state.AssertLineTextAroundCaret("    ''' <summary>", "")
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
                Await state.AssertCompletionSession()
                state.SendTypeChars("summa")
                Await state.AssertSelectedCompletionItem(displayText:="summary")
                state.SendTypeChars(">")
                Await state.AssertNoCompletionSession()

                ' ''' <summary>$$
                Await state.AssertLineTextAroundCaret("    ''' <summary>", "")
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
                Await state.AssertCompletionSession()
                Await state.AssertSelectedCompletionItem(displayText:="param name=""bar""")
                state.SendTypeChars(" name=""")

                ' ''' <param name="$$
                Await state.AssertLineTextAroundCaret("    ''' <param name=""", "")

                ' Because the item contains a double quote, the completionImplementation list should still be present with the same selection
                Await state.AssertCompletionSession()
                Await state.AssertSelectedCompletionItem(displayText:="param name=""bar""")
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
                Await state.AssertCompletionSession()
                Await state.AssertSelectedCompletionItem(displayText:="param name=""bar""")
                state.SendTypeChars(" ")

                ' ''' <param $$
                Await state.AssertLineTextAroundCaret("    ''' <param ", "")

                ' Because the item contains a space, the completionImplementation list should still be present with the same selection
                Await state.AssertCompletionSession()
                Await state.AssertSelectedCompletionItem(displayText:="param name=""bar""")
            End Using
        End Function
    End Class
End Namespace
