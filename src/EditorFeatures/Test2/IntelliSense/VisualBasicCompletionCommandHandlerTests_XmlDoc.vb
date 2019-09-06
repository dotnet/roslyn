' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Namespace Microsoft.CodeAnalysis.Editor.UnitTests.IntelliSense
    <[UseExportProvider]>
    Public Class VisualBasicCompletionCommandHandlerTests_XmlDoc
        Public Shared ReadOnly Property AllCompletionImplementations() As IEnumerable(Of Object())
            Get
                Return TestStateFactory.GetAllCompletionImplementations()
            End Get
        End Property

        <MemberData(NameOf(AllCompletionImplementations))> <WpfTheory, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function CommitSummary(completionImplementation As CompletionImplementation) As Task

            Using state = TestStateFactory.CreateVisualBasicTestState(completionImplementation,
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

        <MemberData(NameOf(AllCompletionImplementations))> <WpfTheory, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function CommitSummaryOnTab(completionImplementation As CompletionImplementation) As Task

            Using state = TestStateFactory.CreateVisualBasicTestState(completionImplementation,
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

        <MemberData(NameOf(AllCompletionImplementations))> <WpfTheory, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function CommitSummaryOnCloseAngle(completionImplementation As CompletionImplementation) As Task

            Using state = TestStateFactory.CreateVisualBasicTestState(completionImplementation,
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

        <MemberData(NameOf(AllCompletionImplementations))> <WpfTheory, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function InvokeWithOpenAngleCommitSummary(completionImplementation As CompletionImplementation) As Task

            Using state = TestStateFactory.CreateVisualBasicTestState(completionImplementation,
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

        <MemberData(NameOf(AllCompletionImplementations))> <WpfTheory, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function InvokeWithOpenAngleCommitSummaryOnTab(completionImplementation As CompletionImplementation) As Task

            Using state = TestStateFactory.CreateVisualBasicTestState(completionImplementation,
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

        <MemberData(NameOf(AllCompletionImplementations))> <WpfTheory, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function InvokeWithOpenAngleCommitSummaryOnCloseAngle(completionImplementation As CompletionImplementation) As Task

            Using state = TestStateFactory.CreateVisualBasicTestState(completionImplementation,
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

        <MemberData(NameOf(AllCompletionImplementations))> <WpfTheory, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function CommitRemarksOnCloseAngle(completionImplementation As CompletionImplementation) As Task

            Using state = TestStateFactory.CreateVisualBasicTestState(completionImplementation,
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

        <MemberData(NameOf(AllCompletionImplementations))> <WpfTheory, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function InvokeWithOpenAngleCommitRemarksOnCloseAngle(completionImplementation As CompletionImplementation) As Task

            Using state = TestStateFactory.CreateVisualBasicTestState(completionImplementation,
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

        <MemberData(NameOf(AllCompletionImplementations))> <WpfTheory, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function CommitReturnsOnCloseAngle(completionImplementation As CompletionImplementation) As Task

            Using state = TestStateFactory.CreateVisualBasicTestState(completionImplementation,
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

        <MemberData(NameOf(AllCompletionImplementations))> <WpfTheory, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function InvokeWithOpenAngleCommitReturnsOnCloseAngle(completionImplementation As CompletionImplementation) As Task

            Using state = TestStateFactory.CreateVisualBasicTestState(completionImplementation,
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

        <MemberData(NameOf(AllCompletionImplementations))> <WpfTheory, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function CommitExampleOnCloseAngle(completionImplementation As CompletionImplementation) As Task

            Using state = TestStateFactory.CreateVisualBasicTestState(completionImplementation,
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

        <MemberData(NameOf(AllCompletionImplementations))> <WpfTheory, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function InvokeWithOpenAngleCommitExampleOnCloseAngle(completionImplementation As CompletionImplementation) As Task

            Using state = TestStateFactory.CreateVisualBasicTestState(completionImplementation,
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

        <MemberData(NameOf(AllCompletionImplementations))> <WpfTheory, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function CommitExceptionNoOpenAngle(completionImplementation As CompletionImplementation) As Task

            Using state = TestStateFactory.CreateVisualBasicTestState(completionImplementation,
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

        <MemberData(NameOf(AllCompletionImplementations))> <WpfTheory, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function InvokeWithOpenAngleCommitExceptionOnCloseAngle(completionImplementation As CompletionImplementation) As Task

            Using state = TestStateFactory.CreateVisualBasicTestState(completionImplementation,
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

        <MemberData(NameOf(AllCompletionImplementations))>
        <WpfTheory, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function CommitCommentNoOpenAngle(completionImplementation As CompletionImplementation) As Task
            Using state = TestStateFactory.CreateVisualBasicTestState(completionImplementation,
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

        <MemberData(NameOf(AllCompletionImplementations))>
        <WpfTheory, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function InvokeWithOpenAngleCommitCommentOnCloseAngle(completionImplementation As CompletionImplementation) As Task
            Using state = TestStateFactory.CreateVisualBasicTestState(completionImplementation,
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

        <MemberData(NameOf(AllCompletionImplementations))>
        <WpfTheory, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function CommitCdataNoOpenAngle(completionImplementation As CompletionImplementation) As Task
            Using state = TestStateFactory.CreateVisualBasicTestState(completionImplementation,
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

        <MemberData(NameOf(AllCompletionImplementations))>
        <WpfTheory, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function InvokeWithOpenAngleCommitCdataOnCloseAngle(completionImplementation As CompletionImplementation) As Task
            Using state = TestStateFactory.CreateVisualBasicTestState(completionImplementation,
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

        <MemberData(NameOf(AllCompletionImplementations))> <WpfTheory, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function CommitIncludeNoOpenAngle(completionImplementation As CompletionImplementation) As Task

            Using state = TestStateFactory.CreateVisualBasicTestState(completionImplementation,
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

        <MemberData(NameOf(AllCompletionImplementations))> <WpfTheory, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function InvokeWithOpenAngleCommitIncludeOnCloseAngle(completionImplementation As CompletionImplementation) As Task

            Using state = TestStateFactory.CreateVisualBasicTestState(completionImplementation,
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

        <MemberData(NameOf(AllCompletionImplementations))> <WpfTheory, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function CommitPermissionNoOpenAngle(completionImplementation As CompletionImplementation) As Task

            Using state = TestStateFactory.CreateVisualBasicTestState(completionImplementation,
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

        <MemberData(NameOf(AllCompletionImplementations))> <WpfTheory, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function InvokeWithOpenAngleCommitPermissionOnCloseAngle(completionImplementation As CompletionImplementation) As Task

            Using state = TestStateFactory.CreateVisualBasicTestState(completionImplementation,
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

        <MemberData(NameOf(AllCompletionImplementations))> <WpfTheory, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function CommitSeeNoOpenAngle(completionImplementation As CompletionImplementation) As Task

            Using state = TestStateFactory.CreateVisualBasicTestState(completionImplementation,
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

        <MemberData(NameOf(AllCompletionImplementations))> <WpfTheory, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function InvokeWithOpenAngleCommitSeeOnCloseAngle(completionImplementation As CompletionImplementation) As Task

            Using state = TestStateFactory.CreateVisualBasicTestState(completionImplementation,
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

        <MemberData(NameOf(AllCompletionImplementations))> <WpfTheory, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function InvokeWithOpenAngleCommitSeeOnSpace(completionImplementation As CompletionImplementation) As Task
            Using state = TestStateFactory.CreateVisualBasicTestState(completionImplementation,
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

        <MemberData(NameOf(AllCompletionImplementations))>
        <WpfTheory, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Function InvokeWithNothingKeywordCommitSeeLangword(completionImplementation As CompletionImplementation) As Task
            Return InvokeWithKeywordCommitSeeLangword(completionImplementation, "Nothing")
        End Function

        <MemberData(NameOf(AllCompletionImplementations))>
        <WpfTheory, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Function InvokeWithSharedKeywordCommitSeeLangword(completionImplementation As CompletionImplementation) As Task
            Return InvokeWithKeywordCommitSeeLangword(completionImplementation, "Shared")
        End Function

        <MemberData(NameOf(AllCompletionImplementations))>
        <WpfTheory, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Function InvokeWithOverridableKeywordCommitSeeLangword(completionImplementation As CompletionImplementation) As Task
            Return InvokeWithKeywordCommitSeeLangword(completionImplementation, "Overridable", unique:=False)
        End Function

        <MemberData(NameOf(AllCompletionImplementations))>
        <WpfTheory, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Function InvokeWithTrueKeywordCommitSeeLangword(completionImplementation As CompletionImplementation) As Task
            Return InvokeWithKeywordCommitSeeLangword(completionImplementation, "True")
        End Function

        <MemberData(NameOf(AllCompletionImplementations))>
        <WpfTheory, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Function InvokeWithFalseKeywordCommitSeeLangword(completionImplementation As CompletionImplementation) As Task
            Return InvokeWithKeywordCommitSeeLangword(completionImplementation, "False")
        End Function

        <MemberData(NameOf(AllCompletionImplementations))>
        <WpfTheory, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Function InvokeWithMustInheritKeywordCommitSeeLangword(completionImplementation As CompletionImplementation) As Task
            Return InvokeWithKeywordCommitSeeLangword(completionImplementation, "MustInherit")
        End Function

        <MemberData(NameOf(AllCompletionImplementations))>
        <WpfTheory, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Function InvokeWithNotOverridableKeywordCommitSeeLangword(completionImplementation As CompletionImplementation) As Task
            Return InvokeWithKeywordCommitSeeLangword(completionImplementation, "NotOverridable")
        End Function

        <MemberData(NameOf(AllCompletionImplementations))>
        <WpfTheory, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Function InvokeWithAsyncKeywordCommitSeeLangword(completionImplementation As CompletionImplementation) As Task
            Return InvokeWithKeywordCommitSeeLangword(completionImplementation, "Async")
        End Function

        <MemberData(NameOf(AllCompletionImplementations))>
        <WpfTheory, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Function InvokeWithAwaitKeywordCommitSeeLangword(completionImplementation As CompletionImplementation) As Task
            Return InvokeWithKeywordCommitSeeLangword(completionImplementation, "Await")
        End Function

        Private Async Function InvokeWithKeywordCommitSeeLangword(completionImplementation As CompletionImplementation, keyword As String, Optional unique As Boolean = True) As Task
            Using state = TestStateFactory.CreateVisualBasicTestState(completionImplementation,
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
                    state.SendCommitUniqueCompletionListItem()
                Else
                    Await state.AssertSelectedCompletionItem(displayText:=keyword)
                    state.SendTab()
                End If
                Await state.AssertNoCompletionSession()

                ' ''' <see langword="keyword"/>$$
                Await state.AssertLineTextAroundCaret("    ''' <see langword=""" + keyword + """/>", "")
            End Using
        End Function

        <MemberData(NameOf(AllCompletionImplementations))> <WpfTheory, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function CommitSeealsoNoOpenAngle(completionImplementation As CompletionImplementation) As Task

            Using state = TestStateFactory.CreateVisualBasicTestState(completionImplementation,
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

        <MemberData(NameOf(AllCompletionImplementations))> <WpfTheory, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function InvokeWithOpenAngleCommitSeealsoOnCloseAngle(completionImplementation As CompletionImplementation) As Task

            Using state = TestStateFactory.CreateVisualBasicTestState(completionImplementation,
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
        <MemberData(NameOf(AllCompletionImplementations))> <WpfTheory, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function CommitParam(completionImplementation As CompletionImplementation) As Task
            Using state = TestStateFactory.CreateVisualBasicTestState(completionImplementation,
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

        <MemberData(NameOf(AllCompletionImplementations))> <WpfTheory, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function CommitParamNoOpenAngle(completionImplementation As CompletionImplementation) As Task

            Using state = TestStateFactory.CreateVisualBasicTestState(completionImplementation,
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

        <MemberData(NameOf(AllCompletionImplementations))> <WpfTheory, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function CommitParamNoOpenAngleOnTab(completionImplementation As CompletionImplementation) As Task

            Using state = TestStateFactory.CreateVisualBasicTestState(completionImplementation,
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

        <MemberData(NameOf(AllCompletionImplementations))> <WpfTheory, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function CommitParamNoOpenAngleOnCloseAngle(completionImplementation As CompletionImplementation) As Task

            Using state = TestStateFactory.CreateVisualBasicTestState(completionImplementation,
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

        <MemberData(NameOf(AllCompletionImplementations))> <WpfTheory, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function InvokeWithOpenAngleCommitParam(completionImplementation As CompletionImplementation) As Task

            Using state = TestStateFactory.CreateVisualBasicTestState(completionImplementation,
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

        <MemberData(NameOf(AllCompletionImplementations))> <WpfTheory, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function InvokeWithOpenAngleCommitParamOnTab(completionImplementation As CompletionImplementation) As Task

            Using state = TestStateFactory.CreateVisualBasicTestState(completionImplementation,
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

        <MemberData(NameOf(AllCompletionImplementations))> <WpfTheory, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function InvokeWithOpenAngleCommitParamOnCloseAngle(completionImplementation As CompletionImplementation) As Task

            Using state = TestStateFactory.CreateVisualBasicTestState(completionImplementation,
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

        <MemberData(NameOf(AllCompletionImplementations))> <WpfTheory, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function CommitTypeparamNoOpenAngle(completionImplementation As CompletionImplementation) As Task

            Using state = TestStateFactory.CreateVisualBasicTestState(completionImplementation,
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

        <MemberData(NameOf(AllCompletionImplementations))> <WpfTheory, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function CommitTypeparamNoOpenAngleOnTab(completionImplementation As CompletionImplementation) As Task

            Using state = TestStateFactory.CreateVisualBasicTestState(completionImplementation,
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

        <MemberData(NameOf(AllCompletionImplementations))> <WpfTheory, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function CommitTypeparamNoOpenAngleOnCloseAngle(completionImplementation As CompletionImplementation) As Task

            Using state = TestStateFactory.CreateVisualBasicTestState(completionImplementation,
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

        <MemberData(NameOf(AllCompletionImplementations))> <WpfTheory, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function InvokeWithOpenAngleCommitTypeparam(completionImplementation As CompletionImplementation) As Task

            Using state = TestStateFactory.CreateVisualBasicTestState(completionImplementation,
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

        <MemberData(NameOf(AllCompletionImplementations))> <WpfTheory, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function InvokeWithOpenAngleCommitTypeparamOnTab(completionImplementation As CompletionImplementation) As Task

            Using state = TestStateFactory.CreateVisualBasicTestState(completionImplementation,
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

        <MemberData(NameOf(AllCompletionImplementations))> <WpfTheory, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function InvokeWithOpenAngleCommitTypeparamOnCloseAngle(completionImplementation As CompletionImplementation) As Task

            Using state = TestStateFactory.CreateVisualBasicTestState(completionImplementation,
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

        <MemberData(NameOf(AllCompletionImplementations))> <WpfTheory, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function CommitList(completionImplementation As CompletionImplementation) As Task

            Using state = TestStateFactory.CreateVisualBasicTestState(completionImplementation,
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

        <MemberData(NameOf(AllCompletionImplementations))> <WpfTheory, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function CommitListOnCloseAngle(completionImplementation As CompletionImplementation) As Task

            Using state = TestStateFactory.CreateVisualBasicTestState(completionImplementation,
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

        <MemberData(NameOf(AllCompletionImplementations))> <WpfTheory, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function TestTagCompletion1(completionImplementation As CompletionImplementation) As Task

            Using state = TestStateFactory.CreateVisualBasicTestState(completionImplementation,
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

        <MemberData(NameOf(AllCompletionImplementations))> <WpfTheory, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function TestTagCompletion2(completionImplementation As CompletionImplementation) As Task

            Using state = TestStateFactory.CreateVisualBasicTestState(completionImplementation,
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

        <MemberData(NameOf(AllCompletionImplementations))> <WpfTheory, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function TestTagCompletion3(completionImplementation As CompletionImplementation) As Task

            Using state = TestStateFactory.CreateVisualBasicTestState(completionImplementation,
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
        <MemberData(NameOf(AllCompletionImplementations))>
        <WpfTheory(Skip:="https://github.com/dotnet/roslyn/issues/21481"), Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function AllowTypingDoubleQuote(completionImplementation As CompletionImplementation) As Task
            Using state = TestStateFactory.CreateVisualBasicTestState(completionImplementation,
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
        <MemberData(NameOf(AllCompletionImplementations))>
        <WpfTheory, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function AllowTypingSpace(completionImplementation As CompletionImplementation) As Task
            Using state = TestStateFactory.CreateVisualBasicTestState(completionImplementation,
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
