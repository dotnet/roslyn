' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports Microsoft.CodeAnalysis.Editor.[Shared].Utilities
Imports Microsoft.CodeAnalysis.Editor.UnitTests
Imports Microsoft.CodeAnalysis.Editor.UnitTests.ChangeSignature
Imports Microsoft.CodeAnalysis.Editor.UnitTests.Extensions
Imports Microsoft.CodeAnalysis.Editor.UnitTests.Workspaces
Imports Microsoft.CodeAnalysis.Editor.VisualBasic.ChangeSignature
Imports Microsoft.VisualStudio.Text.Editor.Commanding.Commands

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.UnitTests.ChangeSignature
    Partial Public Class ChangeSignatureTests
        Inherits AbstractChangeSignatureTests

        <Fact, Trait(Traits.Feature, Traits.Features.ChangeSignature)>
        Public Async Function TestRemoveParameters1() As Task

            Dim markup = <Text><![CDATA[
Module Program
    ''' <summary>
    ''' See <see cref="M(String, Integer, String, Boolean, Integer, String)"/>
    ''' </summary>
    ''' <param name="o">o!</param>
    ''' <param name="a">a!</param>
    ''' <param name="b">b!</param>
    ''' <param name="c">c!</param>
    ''' <param name="x">x!</param>
    ''' <param name="y">y!</param>
    <System.Runtime.CompilerServices.Extension>
    Sub $$M(ByVal o As String, a As Integer, b As String, c As Boolean, Optional x As Integer = 0, Optional y As String = "Zero")
        Dim t = "Test"

        M(t, 1, "Two", True, 3, "Four")
        t.M(1, "Two", True, 3, "Four")

        M(t, 1, "Two", True, 3)
        M(t, 1, "Two", True)

        M(t, 1, "Two", True, 3, y:="Four")
        M(t, 1, "Two", c:=True)

        M(t, 1, "Two", True, y:="Four")
        M(t, 1, "Two", True, x:=3)

        M(t, 1, "Two", True, y:="Four", x:=3)
        M(t, 1, y:="Four", x:=3, b:="Two", c:=True)
        M(t, y:="Four", x:=3, c:=True, b:="Two", a:=1)
        M(y:="Four", x:=3, c:=True, b:="Two", a:=1, o:=t)
    End Sub
End Module

]]></Text>.NormalizedValue()
            Dim permutation = {0, 3, 1, 5}
            Dim updatedCode = <Text><![CDATA[
Module Program
    ''' <summary>
    ''' See <see cref="M(String, Boolean, Integer, String)"/>
    ''' </summary>
    ''' <param name="o">o!</param>
    ''' <param name="c">c!</param>
    ''' <param name="a">a!</param>
    ''' <param name="y">y!</param>
    ''' 
    ''' 
    <System.Runtime.CompilerServices.Extension>
    Sub M(ByVal o As String, c As Boolean, a As Integer, Optional y As String = "Zero")
        Dim t = "Test"

        M(t, True, 1, "Four")
        t.M(True, 1, "Four")

        M(t, True, 1)
        M(t, True, 1)

        M(t, True, 1, y:="Four")
        M(t, c:=True, a:=1)

        M(t, True, 1, y:="Four")
        M(t, True, 1)

        M(t, True, 1, y:="Four")
        M(t, a:=1, y:="Four", c:=True)
        M(t, y:="Four", c:=True, a:=1)
        M(y:="Four", c:=True, a:=1, o:=t)
    End Sub
End Module

]]></Text>.NormalizedValue()

            Await TestChangeSignatureViaCommandAsync(LanguageNames.VisualBasic, markup, updatedSignature:=permutation, expectedUpdatedInvocationDocumentCode:=updatedCode)

        End Function

        <WpfFact>
        <Trait(Traits.Feature, Traits.Features.ChangeSignature)>
        <Trait(Traits.Feature, Traits.Features.Interactive)>
        Public Sub TestChangeSignatureCommandDisabledInSubmission()
            Using workspace = EditorTestWorkspace.Create(
                <Workspace>
                    <Submission Language="Visual Basic" CommonReferences="true">  
                        Class C
                            Sub M$$(x As Integer)
                            End Sub
                        End Class
                    </Submission>
                </Workspace>,
                workspaceKind:=WorkspaceKind.Interactive,
                composition:=EditorTestCompositions.EditorFeaturesWpf)

                ' Force initialization.
                workspace.GetOpenDocumentIds().Select(Function(id) workspace.GetTestDocument(id).GetTextView()).ToList()

                Dim textView = workspace.Documents.Single().GetTextView()

                Dim handler = New VisualBasicChangeSignatureCommandHandler(
                    workspace.GetService(Of IThreadingContext))

                Dim state = handler.GetCommandState(New ReorderParametersCommandArgs(textView, textView.TextBuffer))
                Assert.True(state.IsUnspecified)

                state = handler.GetCommandState(New RemoveParametersCommandArgs(textView, textView.TextBuffer))
                Assert.True(state.IsUnspecified)
            End Using
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.ChangeSignature)>
        <WorkItem("https://github.com/dotnet/roslyn/issues/49941")>
        Public Async Function TestRemoveParameters_DoNotAddUnnecessaryParensToInvocation() As Task

            Dim markup = <Text><![CDATA[
Class C
    Sub M(Optional s As String = "str")
        $$M
        M()
        M("test")
    End Sub
End Class]]></Text>.NormalizedValue()
            Dim permutation = Array.Empty(Of Integer)()
            Dim updatedCode = <Text><![CDATA[
Class C
    Sub M()
        M
        M()
        M()
    End Sub
End Class]]></Text>.NormalizedValue()

            Await TestChangeSignatureViaCommandAsync(LanguageNames.VisualBasic, markup, updatedSignature:=permutation, expectedUpdatedInvocationDocumentCode:=updatedCode)

        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.ChangeSignature)>
        <WorkItem("https://github.com/dotnet/roslyn/issues/66547")>
        Public Async Function RemoveParameters_SpecialSymbolNamedParameter() As Task

            Dim markup = <Text><![CDATA[
Class C
    Sub $$M(param As Object, Optional [new] As Boolean = False)
    End Sub

    Sub M2()
        M(Nothing, [new]:=True)
    End Sub
End Class]]></Text>.NormalizedValue()
            Dim permutation = {1}
            Dim updatedCode = <Text><![CDATA[
Class C
    Sub M(Optional [new] As Boolean = False)
    End Sub

    Sub M2()
        M([new]:=True)
    End Sub
End Class]]></Text>.NormalizedValue()

            Await TestChangeSignatureViaCommandAsync(LanguageNames.VisualBasic, markup, updatedSignature:=permutation, expectedUpdatedInvocationDocumentCode:=updatedCode)

        End Function
    End Class
End Namespace
