' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports Microsoft.CodeAnalysis.Editor.Implementation.Interactive
Imports Microsoft.CodeAnalysis.Editor.UnitTests
Imports Microsoft.CodeAnalysis.Editor.UnitTests.Workspaces
Imports Microsoft.CodeAnalysis.Editor.VisualBasic.EncapsulateField
Imports Microsoft.CodeAnalysis.Editor.VisualBasic.UnitTests
Imports Microsoft.VisualStudio.Text.Operations

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.UnitTests.EncapsulateField
    Public Class EncapsulateFieldCommandHandlerTests
        <WpfFact, Trait(Traits.Feature, Traits.Features.EncapsulateField)>
        Public Async Function PrivateField() As System.Threading.Tasks.Task
            Dim text = <File>
Class C
    Private foo$$ As Integer

    Sub bar()
        foo = 3
    End Sub
End Class</File>.ConvertTestSourceTag()

            Dim expected = <File>
Class C
    Private foo As Integer

    Public Property Foo1 As Integer
        Get
            Return foo
        End Get
        Set(value As Integer)
            foo = value
        End Set
    End Property

    Sub bar()
        Foo1 = 3
    End Sub
End Class</File>.ConvertTestSourceTag()

            Using state = Await EncapsulateFieldTestState.CreateAsync(text)
                state.AssertEncapsulateAs(expected)
            End Using
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.EncapsulateField)>
        Public Async Function NonPrivateField() As System.Threading.Tasks.Task
            Dim text = <File>
Class C
    Protected foo$$ As Integer

    Sub bar()
        foo = 3
    End Sub
End Class</File>.ConvertTestSourceTag()

            Dim expected = <File>
Class C
    Private _foo As Integer

    Protected Property Foo As Integer
        Get
            Return _foo
        End Get
        Set(value As Integer)
            _foo = value
        End Set
    End Property

    Sub bar()
        Foo = 3
    End Sub
End Class</File>.ConvertTestSourceTag()

            Using state = Await EncapsulateFieldTestState.CreateAsync(text)
                state.AssertEncapsulateAs(expected)
            End Using
        End Function

        <WorkItem(1086632, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1086632")>
        <WpfFact, Trait(Traits.Feature, Traits.Features.EncapsulateField)>
        Public Async Function EncapsulateTwoFields() As System.Threading.Tasks.Task
            Dim text = "
Class Program
    [|Shared A As Integer = 1
    Shared B As Integer = A|]

    Sub Main(args As String())
        System.Console.WriteLine(A)
        System.Console.WriteLine(B)
    End Sub
End Class
"
            Dim expected = "
Class Program
    Shared A As Integer = 1
    Shared B As Integer = A1

    Public Shared Property A1 As Integer
        Get
            Return A
        End Get
        Set(value As Integer)
            A = value
        End Set
    End Property

    Public Shared Property B1 As Integer
        Get
            Return B
        End Get
        Set(value As Integer)
            B = value
        End Set
    End Property

    Sub Main(args As String())
        System.Console.WriteLine(A1)
        System.Console.WriteLine(B1)
    End Sub
End Class
"

            Using state = Await EncapsulateFieldTestState.CreateAsync(text)
                state.AssertEncapsulateAs(expected)
            End Using
        End Function

        <WpfFact>
        <Trait(Traits.Feature, Traits.Features.EncapsulateField)>
        <Trait(Traits.Feature, Traits.Features.Interactive)>
        Public Async Function EncapsulateFieldCommandDisabledInSubmission() As System.Threading.Tasks.Task
            Dim exportProvider = MinimalTestExportProvider.CreateExportProvider(
                TestExportProvider.EntireAssemblyCatalogWithCSharpAndVisualBasic.WithParts(GetType(InteractiveDocumentSupportsFeatureService)))

            Using workspace = Await TestWorkspace.CreateAsync(
                <Workspace>
                    <Submission Language="Visual Basic" CommonReferences="true">  
                        Class C
                            Private $foo As Object
                        End Class
                    </Submission>
                </Workspace>,
                workspaceKind:=WorkspaceKind.Interactive,
                exportProvider:=exportProvider)

                ' Force initialization.
                workspace.GetOpenDocumentIds().Select(Function(id) workspace.GetTestDocument(id).GetTextView()).ToList()

                Dim textView = workspace.Documents.Single().GetTextView()

                Dim handler = New EncapsulateFieldCommandHandler(workspace.GetService(Of Host.IWaitIndicator), workspace.GetService(Of ITextBufferUndoManagerProvider))
                Dim delegatedToNext = False
                Dim nextHandler =
                    Function()
                        delegatedToNext = True
                        Return CommandState.Unavailable
                    End Function

                Dim state = handler.GetCommandState(New Commands.EncapsulateFieldCommandArgs(textView, textView.TextBuffer), nextHandler)
                Assert.True(delegatedToNext)
                Assert.False(state.IsAvailable)
            End Using
        End Function
    End Class
End Namespace
