' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports Microsoft.CodeAnalysis.Editor.Implementation.Interactive
Imports Microsoft.CodeAnalysis.Editor.Shared.Utilities
Imports Microsoft.CodeAnalysis.Editor.UnitTests
Imports Microsoft.CodeAnalysis.Editor.UnitTests.Workspaces
Imports Microsoft.CodeAnalysis.Editor.VisualBasic.EncapsulateField
Imports Microsoft.CodeAnalysis.Editor.VisualBasic.UnitTests
Imports Microsoft.CodeAnalysis.Shared.TestHooks
Imports Microsoft.VisualStudio.Text.Editor.Commanding.Commands
Imports Microsoft.VisualStudio.Text.Operations

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.UnitTests.EncapsulateField
    <[UseExportProvider]>
    Public Class EncapsulateFieldCommandHandlerTests
        <WpfFact, Trait(Traits.Feature, Traits.Features.EncapsulateField)>
        Public Sub PrivateField()
            Dim text = <File>
Class C
    Private goo$$ As Integer

    Sub bar()
        goo = 3
    End Sub
End Class</File>.ConvertTestSourceTag()

            Dim expected = <File>
Class C
    Private goo As Integer

    Public Property Goo1 As Integer
        Get
            Return goo
        End Get
        Set(value As Integer)
            goo = value
        End Set
    End Property

    Sub bar()
        Goo1 = 3
    End Sub
End Class</File>.ConvertTestSourceTag()

            Using state = EncapsulateFieldTestState.Create(text)
                state.AssertEncapsulateAs(expected)
            End Using
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.EncapsulateField)>
        Public Sub NonPrivateField()
            Dim text = <File>
Class C
    Protected goo$$ As Integer

    Sub bar()
        goo = 3
    End Sub
End Class</File>.ConvertTestSourceTag()

            Dim expected = <File>
Class C
    Private _goo As Integer

    Protected Property Goo As Integer
        Get
            Return _goo
        End Get
        Set(value As Integer)
            _goo = value
        End Set
    End Property

    Sub bar()
        Goo = 3
    End Sub
End Class</File>.ConvertTestSourceTag()

            Using state = EncapsulateFieldTestState.Create(text)
                state.AssertEncapsulateAs(expected)
            End Using
        End Sub

        <WorkItem(1086632, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1086632")>
        <WpfFact, Trait(Traits.Feature, Traits.Features.EncapsulateField)>
        Public Sub EncapsulateTwoFields()
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

            Using state = EncapsulateFieldTestState.Create(text)
                state.AssertEncapsulateAs(expected)
            End Using
        End Sub

        <WpfFact>
        <Trait(Traits.Feature, Traits.Features.EncapsulateField)>
        <Trait(Traits.Feature, Traits.Features.Interactive)>
        Public Sub EncapsulateFieldCommandDisabledInSubmission()
            Dim exportProvider = ExportProviderCache _
                .GetOrCreateExportProviderFactory(TestExportProvider.EntireAssemblyCatalogWithCSharpAndVisualBasic.WithParts(GetType(InteractiveSupportsFeatureService.InteractiveTextBufferSupportsFeatureService))) _
                .CreateExportProvider()

            Using workspace = TestWorkspace.Create(
                <Workspace>
                    <Submission Language="Visual Basic" CommonReferences="true">  
                        Class C
                            Private $goo As Object
                        End Class
                    </Submission>
                </Workspace>,
                workspaceKind:=WorkspaceKind.Interactive,
                exportProvider:=exportProvider)

                ' Force initialization.
                workspace.GetOpenDocumentIds().Select(Function(id) workspace.GetTestDocument(id).GetTextView()).ToList()

                Dim textView = workspace.Documents.Single().GetTextView()

                Dim handler = New EncapsulateFieldCommandHandler(
                    workspace.GetService(Of IThreadingContext),
                    workspace.GetService(Of ITextBufferUndoManagerProvider),
                    workspace.GetService(Of IAsynchronousOperationListenerProvider)())

                Dim state = handler.GetCommandState(New EncapsulateFieldCommandArgs(textView, textView.TextBuffer))
                Assert.True(state.IsUnspecified)
            End Using
        End Sub
    End Class
End Namespace
