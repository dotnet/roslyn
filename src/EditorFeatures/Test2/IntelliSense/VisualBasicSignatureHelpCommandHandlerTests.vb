' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports Microsoft.CodeAnalysis.Editor.Options
Imports Microsoft.CodeAnalysis.VisualBasic

Namespace Microsoft.CodeAnalysis.Editor.UnitTests.IntelliSense
    <[UseExportProvider]>
    Public Class VisualBasicSignatureHelpCommandHandlerTests
        Public Shared ReadOnly Property AllCompletionImplementations() As IEnumerable(Of Object())
            Get
                Return TestStateFactory.GetAllCompletionImplementations()
            End Get
        End Property

        <WorkItem(544551, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/544551")>
        <MemberData(NameOf(AllCompletionImplementations))> <WpfTheory, Trait(Traits.Feature, Traits.Features.SignatureHelp)>
        Public Async Function TestFilterOnNamedParameters1(completionImplementation As CompletionImplementation) As Task
            Using state = TestStateFactory.CreateVisualBasicTestState(completionImplementation,
                              <Document>
Class C
    Public Sub M(first As Integer, second As Integer)
    End Sub

    Public Sub M(third As Integer)
    End Sub
End Class
 
Class Program
    Sub Main()
        Call New C().M(first$$
    End Sub
End Class

                              </Document>)

                state.SendInvokeSignatureHelp()
                Await state.AssertSignatureHelpSession()
                Await state.AssertSelectedSignatureHelpItem("C.M(third As Integer)")
                Assert.Equal(2, state.GetSignatureHelpItems().Count)

                state.SendTypeChars(":=")
                Await state.AssertSignatureHelpSession()
                Await state.AssertSelectedSignatureHelpItem("C.M(first As Integer, second As Integer)")
                Assert.Equal(1, state.GetSignatureHelpItems().Count)

                ' Now both items are available again, we're sticking with last selection
                state.SendBackspace()
                Await state.AssertSignatureHelpSession()
                Await state.AssertSelectedSignatureHelpItem("C.M(first As Integer, second As Integer)")
                Assert.Equal(2, state.GetSignatureHelpItems().Count)
            End Using
        End Function

        <WorkItem(544551, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/544551")>
        <MemberData(NameOf(AllCompletionImplementations))> <WpfTheory, Trait(Traits.Feature, Traits.Features.SignatureHelp)>
        Public Async Function TestFilterOnNamedParameters2(completionImplementation As CompletionImplementation) As Task
            Using state = TestStateFactory.CreateVisualBasicTestState(completionImplementation,
                              <Document>
Class C
    Public Sub M(first As Integer, second As Integer)
    End Sub

    Public Sub M(third As Integer)
    End Sub
End Class
 
Class Program
    Sub Main()
        Call New C().M(first$$
    End Sub
End Class

                              </Document>)

                state.SendInvokeSignatureHelp()
                Await state.AssertSignatureHelpSession()
                Await state.AssertSelectedSignatureHelpItem("C.M(third As Integer)")
                Assert.Equal(2, state.GetSignatureHelpItems().Count)

                state.SendTypeChars(":=")
                Await state.AssertSignatureHelpSession()
                Await state.AssertSelectedSignatureHelpItem("C.M(first As Integer, second As Integer)")
                Assert.Equal(1, state.GetSignatureHelpItems().Count)

                state.SendTypeChars("0,")
                Await state.AssertSignatureHelpSession()
                Await state.AssertSelectedSignatureHelpItem("C.M(first As Integer, second As Integer)")
                Assert.Equal(1, state.GetSignatureHelpItems().Count)
            End Using
        End Function

        <WorkItem(539100, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/539100"), WorkItem(530081, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/530081")>
        <MemberData(NameOf(AllCompletionImplementations))> <WpfTheory, Trait(Traits.Feature, Traits.Features.SignatureHelp)>
        Public Async Function TestSigHelpShowsOnBackspace(completionImplementation As CompletionImplementation) As Task
            Using state = TestStateFactory.CreateVisualBasicTestState(completionImplementation,
                              <Document>
Module M
    Sub Method(args As String())
        Method(Nothing)$$
    End Sub
End Module

                              </Document>)

                state.SendInvokeSignatureHelp()
                Await state.AssertNoSignatureHelpSession()

                state.SendBackspace()
                Await state.AssertNoSignatureHelpSession()
            End Using
        End Function

        <MemberData(NameOf(AllCompletionImplementations))> <WpfTheory, Trait(Traits.Feature, Traits.Features.SignatureHelp)>
        Public Async Function TestSigHelpInLinkedFiles(completionImplementation As CompletionImplementation) As Task
            Using state = TestStateFactory.CreateTestStateFromWorkspace(completionImplementation,
                <Workspace>
                    <Project Language="Visual Basic" CommonReferences="true" AssemblyName="VBProj" PreprocessorSymbols="Proj1=True">
                        <Document FilePath="C.vb">
Class C
    Sub M()
        M2($$)
    End Sub

#If Proj1 Then
    Sub M2(x as Integer)
    End Sub
#End If
#If Proj2 Then
        Sub M2(x As String)
    End Sub
#End If
End Class
                              </Document>
                    </Project>
                    <Project Language="Visual Basic" CommonReferences="true" PreprocessorSymbols="Proj2=True">
                        <Document IsLinkFile="true" LinkAssemblyName="VBProj" LinkFilePath="C.vb"/>
                    </Project>
                </Workspace>)

                Dim documents = state.Workspace.Documents
                Dim linkDocument = documents.Single(Function(d) d.IsLinkFile)

                state.SendInvokeSignatureHelp()
                Await state.AssertSelectedSignatureHelpItem("C.M2(x As Integer)")
                state.SendEscape()
                state.Workspace.SetDocumentContext(linkDocument.Id)
                state.SendInvokeSignatureHelp()
                Await state.AssertSelectedSignatureHelpItem("C.M2(x As String)")
            End Using
        End Function

        <WorkItem(1060850, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1060850")>
        <MemberData(NameOf(AllCompletionImplementations))> <WpfTheory, Trait(Traits.Feature, Traits.Features.SignatureHelp)>
        Public Async Function TestSigHelpNotDismissedAfterQuote(completionImplementation As CompletionImplementation) As Task
            Using state = TestStateFactory.CreateVisualBasicTestState(completionImplementation,
                              <Document><![CDATA[
Class C
    Sub M()
    End Sub

    Sub M(s As String)
        M($$)
    End Sub
End Class
]]></Document>)

                state.SendInvokeSignatureHelp()
                Await state.AssertSelectedSignatureHelpItem("C.M()")
                state.SendTypeChars("""")
                Await state.AssertSignatureHelpSession()
                Await state.AssertSelectedSignatureHelpItem("C.M(s As String)")
            End Using
        End Function

        <WorkItem(1060850, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1060850")>
        <MemberData(NameOf(AllCompletionImplementations))> <WpfTheory, Trait(Traits.Feature, Traits.Features.SignatureHelp)>
        Public Async Function TestSigHelpDismissedAfterComment(completionImplementation As CompletionImplementation) As Task
            Using state = TestStateFactory.CreateVisualBasicTestState(completionImplementation,
                              <Document><![CDATA[
Class C
    Sub M()
    End Sub

    Sub M(s As String)
        M($$)
    End Sub
End Class
]]></Document>)

                state.SendInvokeSignatureHelp()
                Await state.AssertSelectedSignatureHelpItem("C.M()")
                state.SendTypeChars("'")
                Await state.AssertNoSignatureHelpSession()
            End Using
        End Function

        <WorkItem(1082128, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1082128")>
        <MemberData(NameOf(AllCompletionImplementations))> <WpfTheory, Trait(Traits.Feature, Traits.Features.SignatureHelp)>
        Public Async Function TestSigHelpNotDismissedAfterSpace(completionImplementation As CompletionImplementation) As Task
            Using state = TestStateFactory.CreateVisualBasicTestState(completionImplementation,
                              <Document><![CDATA[
Class C
    Sub M(a As String, b As String)
        M("",$$)
    End Sub
End Class
]]></Document>)

                state.SendInvokeSignatureHelp()
                state.SendTypeChars(" ")
                Await state.AssertSelectedSignatureHelpItem("C.M(a As String, b As String)")
            End Using
        End Function

        <MemberData(NameOf(AllCompletionImplementations))> <WpfTheory, Trait(Traits.Feature, Traits.Features.SignatureHelp)>
        Public Async Function TestGenericNameSigHelpInTypeParameterListAfterConditionalAccess(completionImplementation As CompletionImplementation) As Task
            Using state = TestStateFactory.CreateVisualBasicTestState(completionImplementation,
                              <Document><![CDATA[
Imports System.Collections
Imports System.Collections.Generic
Imports System.Linq

Class C
    Sub M(args As Object())
        Dim x = args?.OfType$$
    End Sub
End Class
]]></Document>)

                state.SendTypeChars("(")
                Await state.AssertSelectedSignatureHelpItem($"<{VBFeaturesResources.Extension}> Enumerable.OfType(Of TResult)() As IEnumerable(Of TResult)")
            End Using
        End Function

        <MemberData(NameOf(AllCompletionImplementations))> <WpfTheory, Trait(Traits.Feature, Traits.Features.SignatureHelp)>
        Public Async Function TestGenericNameSigHelpInTypeParameterListAfterMultipleConditionalAccess(completionImplementation As CompletionImplementation) As Task
            Using state = TestStateFactory.CreateVisualBasicTestState(completionImplementation,
                              <Document><![CDATA[
Imports System.Collections
Imports System.Collections.Generic
Imports System.Linq

Class C
    Sub M(args As Object())
        Dim x = args?.Select(Function(a) a)?.OfType$$
    End Sub
End Class
]]></Document>)

                state.SendTypeChars("(")
                Await state.AssertSelectedSignatureHelpItem($"<{VBFeaturesResources.Extension}> Enumerable.OfType(Of TResult)() As IEnumerable(Of TResult)")
            End Using
        End Function

        <MemberData(NameOf(AllCompletionImplementations))> <WpfTheory, Trait(Traits.Feature, Traits.Features.SignatureHelp)>
        Public Async Function TestGenericNameSigHelpInTypeParameterListMuchAfterConditionalAccess(completionImplementation As CompletionImplementation) As Task
            Using state = TestStateFactory.CreateVisualBasicTestState(completionImplementation,
                              <Document><![CDATA[
Imports System.Collections
Imports System.Collections.Generic
Imports System.Linq

Class C
    Sub M(args As Object())
        Dim x = args?.Select(Function(a) a.GetHashCode()).Where(Function(temp) True).OfType$$
    End Sub
End Class
]]></Document>)

                state.SendTypeChars("(")
                Await state.AssertSelectedSignatureHelpItem($"<{VBFeaturesResources.Extension}> Enumerable.OfType(Of TResult)() As IEnumerable(Of TResult)")
            End Using
        End Function

        <WorkItem(5174, "https://github.com/dotnet/roslyn/issues/5174")>
        <MemberData(NameOf(AllCompletionImplementations))> <WpfTheory, Trait(Traits.Feature, Traits.Features.SignatureHelp)>
        Public Async Function DontShowSignatureHelpIfOptionIsTurnedOffUnlessExplicitlyInvoked(completionImplementation As CompletionImplementation) As Task
            Using state = TestStateFactory.CreateVisualBasicTestState(completionImplementation,
                              <Document>
Class C
    Sub M(i As Integer)
        M$$
    End Sub
End Class
                              </Document>)

                ' disable implicit sig help then type a trigger character -> no session should be available
                state.Workspace.Options = state.Workspace.Options.WithChangedOption(SignatureHelpOptions.ShowSignatureHelp, LanguageNames.VisualBasic, False)
                state.SendTypeChars("(")
                Await state.AssertNoSignatureHelpSession()

                ' force-invoke -> session should be available
                state.SendInvokeSignatureHelp()
                Await state.AssertSignatureHelpSession()
            End Using
        End Function
    End Class
End Namespace
