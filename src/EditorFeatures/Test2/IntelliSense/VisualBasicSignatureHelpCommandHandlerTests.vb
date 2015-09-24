' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports Microsoft.CodeAnalysis.Editor.Options
Imports Microsoft.CodeAnalysis.VisualBasic.VBFeaturesResources

Namespace Microsoft.CodeAnalysis.Editor.UnitTests.IntelliSense
    Public Class VisualBasicSignatureHelpCommandHandlerTests

        <WorkItem(544551)>
        <Fact, Trait(Traits.Feature, Traits.Features.SignatureHelp)>
        Public Sub TestFilterOnNamedParameters1()
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
                state.AssertSignatureHelpSession()
                state.AssertSelectedSignatureHelpItem("C.M(third As Integer)")
                Assert.Equal(2, state.CurrentSignatureHelpPresenterSession.SignatureHelpItems.Count)

                state.SendTypeChars(":=")
                state.AssertSignatureHelpSession()
                state.AssertSelectedSignatureHelpItem("C.M(first As Integer, second As Integer)")
                Assert.Equal(1, state.CurrentSignatureHelpPresenterSession.SignatureHelpItems.Count)

                ' Keep the same item selected when the colon is deleted, but now both items are
                ' available again.
                state.SendBackspace()
                state.AssertSignatureHelpSession()
                state.AssertSelectedSignatureHelpItem("C.M(first As Integer, second As Integer)")
                Assert.Equal(2, state.CurrentSignatureHelpPresenterSession.SignatureHelpItems.Count)
            End Using
        End Sub

        <WorkItem(544551)>
        <Fact, Trait(Traits.Feature, Traits.Features.SignatureHelp)>
        Public Sub TestFilterOnNamedParameters2()
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
                state.AssertSignatureHelpSession()
                state.AssertSelectedSignatureHelpItem("C.M(third As Integer)")
                Assert.Equal(2, state.CurrentSignatureHelpPresenterSession.SignatureHelpItems.Count)

                state.SendTypeChars(":=")
                state.AssertSignatureHelpSession()
                state.AssertSelectedSignatureHelpItem("C.M(first As Integer, second As Integer)")
                Assert.Equal(1, state.CurrentSignatureHelpPresenterSession.SignatureHelpItems.Count)

                state.SendTypeChars("0,")
                state.AssertSignatureHelpSession()
                state.AssertSelectedSignatureHelpItem("C.M(first As Integer, second As Integer)")
                Assert.Equal(1, state.CurrentSignatureHelpPresenterSession.SignatureHelpItems.Count)
            End Using
        End Sub

        <WorkItem(539100), WorkItem(530081)>
        <Fact, Trait(Traits.Feature, Traits.Features.SignatureHelp)>
        Public Sub TestSigHelpShowsOnBackspace()
            Using state = TestState.CreateVisualBasicTestState(
                              <Document>
Module M
    Sub Method(args As String())
        Method(Nothing)$$
    End Sub
End Module

                              </Document>)

                state.SendInvokeSignatureHelp()
                state.AssertNoSignatureHelpSession()

                state.SendBackspace()
                state.AssertNoSignatureHelpSession()
            End Using
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.SignatureHelp)>
        Public Sub TestSigHelpInLinkedFiles()
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
                state.AssertSelectedSignatureHelpItem("C.M2(x As Integer)")
                state.SendEscape()
                state.Workspace.SetDocumentContext(linkDocument.Id)
                state.SendInvokeSignatureHelp()
                state.AssertSelectedSignatureHelpItem("C.M2(x As String)")
            End Using
        End Sub

        <WorkItem(1060850)>
        <Fact, Trait(Traits.Feature, Traits.Features.SignatureHelp)>
        Public Sub TestSigHelpNotDismissedAfterQuote()
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
                state.AssertSelectedSignatureHelpItem("C.M()")
                state.SendTypeChars("""")
                state.AssertSignatureHelpSession()
                state.AssertSelectedSignatureHelpItem("C.M(s As String)")
            End Using
        End Sub

        <WorkItem(1060850)>
        <Fact, Trait(Traits.Feature, Traits.Features.SignatureHelp)>
        Public Sub TestSigHelpDismissedAfterComment()
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
                state.AssertSelectedSignatureHelpItem("C.M()")
                state.SendTypeChars("'")
                state.AssertNoSignatureHelpSession()
            End Using
        End Sub

        <WorkItem(1082128)>
        <Fact, Trait(Traits.Feature, Traits.Features.SignatureHelp)>
        Public Sub TestSigHelpNotDismissedAfterSpace()
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
                state.AssertSelectedSignatureHelpItem("C.M(a As String, b As String)")
            End Using
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.SignatureHelp)>
        Public Sub TestGenericNameSigHelpInTypeParameterListAfterConditionalAccess()
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
                state.AssertSelectedSignatureHelpItem($"<{Extension}> Enumerable.OfType(Of TResult)() As IEnumerable(Of TResult)")
            End Using
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.SignatureHelp)>
        Public Sub TestGenericNameSigHelpInTypeParameterListAfterMultipleConditionalAccess()
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
                state.AssertSelectedSignatureHelpItem($"<{Extension}> Enumerable.OfType(Of TResult)() As IEnumerable(Of TResult)")
            End Using
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.SignatureHelp)>
        Public Sub TestGenericNameSigHelpInTypeParameterListMuchAfterConditionalAccess()
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
                state.AssertSelectedSignatureHelpItem($"<{Extension}> Enumerable.OfType(Of TResult)() As IEnumerable(Of TResult)")
            End Using
        End Sub

        <WorkItem(5174, "https://github.com/dotnet/roslyn/issues/5174")>
        <Fact, Trait(Traits.Feature, Traits.Features.SignatureHelp)>
        Public Sub DontShowSignatureHelpIfOptionIsTurnedOffUnlessExplicitlyInvoked()
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
                state.AssertNoSignatureHelpSession()

                ' force-invoke -> session should be available
                state.SendInvokeSignatureHelp()
                state.AssertSignatureHelpSession()
            End Using
        End Sub
    End Class
End Namespace
