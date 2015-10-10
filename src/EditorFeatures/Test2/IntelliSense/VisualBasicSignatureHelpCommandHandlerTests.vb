' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Threading.Tasks
Imports Microsoft.CodeAnalysis.Editor.Options
Imports Microsoft.CodeAnalysis.VisualBasic.VBFeaturesResources

Namespace Microsoft.CodeAnalysis.Editor.UnitTests.IntelliSense
    Public Class VisualBasicSignatureHelpCommandHandlerTests

        <WorkItem(544551)>
        <WpfFact, Trait(Traits.Feature, Traits.Features.SignatureHelp)>
        Public Async Function TestFilterOnNamedParameters1() As Task
            Using state = TestState.CreateVisualBasicTestState(
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
                Await state.AssertSignatureHelpSession().ConfigureAwait(True)
                Await state.AssertSelectedSignatureHelpItem("C.M(third As Integer)").ConfigureAwait(True)
                Assert.Equal(2, state.CurrentSignatureHelpPresenterSession.SignatureHelpItems.Count)

                state.SendTypeChars(":=")
                Await state.AssertSignatureHelpSession().ConfigureAwait(True)
                Await state.AssertSelectedSignatureHelpItem("C.M(first As Integer, second As Integer)").ConfigureAwait(True)
                Assert.Equal(1, state.CurrentSignatureHelpPresenterSession.SignatureHelpItems.Count)

                ' Keep the same item selected when the colon is deleted, but now both items are
                ' available again.
                state.SendBackspace()
                Await state.AssertSignatureHelpSession().ConfigureAwait(True)
                Await state.AssertSelectedSignatureHelpItem("C.M(first As Integer, second As Integer)").ConfigureAwait(True)
                Assert.Equal(2, state.CurrentSignatureHelpPresenterSession.SignatureHelpItems.Count)
            End Using
        End Function

        <WorkItem(544551)>
        <WpfFact, Trait(Traits.Feature, Traits.Features.SignatureHelp)>
        Public Async Function TestFilterOnNamedParameters2() As Task
            Using state = TestState.CreateVisualBasicTestState(
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
                Await state.AssertSignatureHelpSession().ConfigureAwait(True)
                Await state.AssertSelectedSignatureHelpItem("C.M(third As Integer)").ConfigureAwait(True)
                Assert.Equal(2, state.CurrentSignatureHelpPresenterSession.SignatureHelpItems.Count)

                state.SendTypeChars(":=")
                Await state.AssertSignatureHelpSession().ConfigureAwait(True)
                Await state.AssertSelectedSignatureHelpItem("C.M(first As Integer, second As Integer)").ConfigureAwait(True)
                Assert.Equal(1, state.CurrentSignatureHelpPresenterSession.SignatureHelpItems.Count)

                state.SendTypeChars("0,")
                Await state.AssertSignatureHelpSession().ConfigureAwait(True)
                Await state.AssertSelectedSignatureHelpItem("C.M(first As Integer, second As Integer)").ConfigureAwait(True)
                Assert.Equal(1, state.CurrentSignatureHelpPresenterSession.SignatureHelpItems.Count)
            End Using
        End Function

        <WorkItem(539100), WorkItem(530081)>
        <WpfFact, Trait(Traits.Feature, Traits.Features.SignatureHelp)>
        Public Async Function TestSigHelpShowsOnBackspace() As Task
            Using state = TestState.CreateVisualBasicTestState(
                              <Document>
Module M
    Sub Method(args As String())
        Method(Nothing)$$
    End Sub
End Module

                              </Document>)

                state.SendInvokeSignatureHelp()
                Await state.AssertNoSignatureHelpSession().ConfigureAwait(True)

                state.SendBackspace()
                Await state.AssertNoSignatureHelpSession().ConfigureAwait(True)
            End Using
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.SignatureHelp)>
        Public Async Function TestSigHelpInLinkedFiles() As Task
            Using state = TestState.CreateTestStateFromWorkspace(
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
                Await state.AssertSelectedSignatureHelpItem("C.M2(x As Integer)").ConfigureAwait(True)
                state.SendEscape()
                state.Workspace.SetDocumentContext(linkDocument.Id)
                state.SendInvokeSignatureHelp()
                Await state.AssertSelectedSignatureHelpItem("C.M2(x As String)").ConfigureAwait(True)
            End Using
        End Function

        <WorkItem(1060850)>
        <WpfFact, Trait(Traits.Feature, Traits.Features.SignatureHelp)>
        Public Async Function TestSigHelpNotDismissedAfterQuote() As Task
            Using state = TestState.CreateVisualBasicTestState(
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
                Await state.AssertSelectedSignatureHelpItem("C.M()").ConfigureAwait(True)
                state.SendTypeChars("""")
                Await state.AssertSignatureHelpSession().ConfigureAwait(True)
                Await state.AssertSelectedSignatureHelpItem("C.M(s As String)").ConfigureAwait(True)
            End Using
        End Function

        <WorkItem(1060850)>
        <WpfFact, Trait(Traits.Feature, Traits.Features.SignatureHelp)>
        Public Async Function TestSigHelpDismissedAfterComment() As Task
            Using state = TestState.CreateVisualBasicTestState(
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
                Await state.AssertSelectedSignatureHelpItem("C.M()").ConfigureAwait(True)
                state.SendTypeChars("'")
                Await state.AssertNoSignatureHelpSession().ConfigureAwait(True)
            End Using
        End Function

        <WorkItem(1082128)>
        <WpfFact, Trait(Traits.Feature, Traits.Features.SignatureHelp)>
        Public Async Function TestSigHelpNotDismissedAfterSpace() As Task
            Using state = TestState.CreateVisualBasicTestState(
                              <Document><![CDATA[
Class C
    Sub M(a As String, b As String)
        M("",$$)
    End Sub
End Class
]]></Document>)

                state.SendInvokeSignatureHelp()
                state.SendTypeChars(" ")
                Await state.AssertSelectedSignatureHelpItem("C.M(a As String, b As String)").ConfigureAwait(True)
            End Using
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.SignatureHelp)>
        Public Async Function TestGenericNameSigHelpInTypeParameterListAfterConditionalAccess() As System.Threading.Tasks.Task
            Using state = TestState.CreateVisualBasicTestState(
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
                Await state.AssertSelectedSignatureHelpItem($"<{Extension}> Enumerable.OfType(Of TResult)() As IEnumerable(Of TResult)").ConfigureAwait(True)
            End Using
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.SignatureHelp)>
        Public Async Function TestGenericNameSigHelpInTypeParameterListAfterMultipleConditionalAccess() As Task
            Using state = TestState.CreateVisualBasicTestState(
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
                Await state.AssertSelectedSignatureHelpItem($"<{Extension}> Enumerable.OfType(Of TResult)() As IEnumerable(Of TResult)").ConfigureAwait(True)
            End Using
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.SignatureHelp)>
        Public Async Function TestGenericNameSigHelpInTypeParameterListMuchAfterConditionalAccess() As Task
            Using state = TestState.CreateVisualBasicTestState(
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
                Await state.AssertSelectedSignatureHelpItem($"<{Extension}> Enumerable.OfType(Of TResult)() As IEnumerable(Of TResult)").ConfigureAwait(True)
            End Using
        End Function

        <WorkItem(5174, "https://github.com/dotnet/roslyn/issues/5174")>
        <WpfFact, Trait(Traits.Feature, Traits.Features.SignatureHelp)>
        Public Async Function DontShowSignatureHelpIfOptionIsTurnedOffUnlessExplicitlyInvoked() As Task
            Using state = TestState.CreateVisualBasicTestState(
                              <Document>
Class C
    Sub M(i As Integer)
        M$$
    End Sub
End Class
                              </Document>)

                ' disable implicit sig help then type a trigger character -> no session should be available
                state.Workspace.Options = state.Workspace.Options.WithChangedOption(SignatureHelpOptions.ShowSignatureHelp, "Visual Basic", False)
                state.SendTypeChars("(")
                Await state.AssertNoSignatureHelpSession().ConfigureAwait(True)

                ' force-invoke -> session should be available
                state.SendInvokeSignatureHelp()
                Await state.AssertSignatureHelpSession().ConfigureAwait(True)
            End Using
        End Function
    End Class
End Namespace
