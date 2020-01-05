' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Collections.Immutable
Imports Microsoft.CodeAnalysis.Editor.UnitTests.Utilities
Imports Microsoft.CodeAnalysis.Editor.UnitTests.Workspaces
Imports Microsoft.CodeAnalysis.Editor.VisualBasic.SplitComment
Imports Microsoft.CodeAnalysis.Formatting.FormattingOptions
Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.Text.Shared.Extensions
Imports Microsoft.VisualStudio.Text
Imports Microsoft.VisualStudio.Text.Editor.Commanding.Commands
Imports Microsoft.VisualStudio.Text.Operations

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.UnitTests.SplitComment

    <UseExportProvider>
    Public Class SplitCommentCommandHandlerTests

        ''' <summary>
        ''' verifyUndo Is needed because of https://github.com/dotnet/roslyn/issues/28033
        ''' Most tests will continue to verifyUndo, but select tests will skip it due to
        ''' this known test infrastructure issure. This bug does Not represent a product
        ''' failure.
        ''' </summary>
        Private Sub TestWorker(inputMarkup As String,
                               expectedOutputMarkup As String,
                               callback As Action,
                               Optional verifyUndo As Boolean = True,
                               Optional indentStyle As IndentStyle = IndentStyle.Smart)

            Using workspace = TestWorkspace.CreateVisualBasic(inputMarkup)
                workspace.Options = workspace.Options.WithChangedOption(SmartIndent, LanguageNames.VisualBasic, indentStyle)

                Dim document = workspace.Documents.Single()
                Dim view = document.GetTextView()

                Dim originalSnapshot = view.TextBuffer.CurrentSnapshot
                Dim originalSelections = document.SelectedSpans

                Dim snapshotSpans = New List(Of SnapshotSpan)()
                For Each selection In originalSelections
                    snapshotSpans.Add(selection.ToSnapshotSpan(originalSnapshot))
                Next
                view.SetMultiSelection(snapshotSpans)

                Dim undoHistoryRegistry = workspace.GetService(Of ITextUndoHistoryRegistry)()
                Dim commandHandler = New SplitCommentCommandHandler(
                    undoHistoryRegistry, workspace.GetService(Of IEditorOperationsFactoryService)())

                If Not commandHandler.ExecuteCommand(New ReturnKeyCommandArgs(view, view.TextBuffer), TestCommandExecutionContext.Create()) Then
                    callback()
                End If

                If expectedOutputMarkup IsNot Nothing Then
                    Dim expectedOutput = String.Empty
                    Dim expectedSpans = New ImmutableArray(Of TextSpan)()

                    MarkupTestFile.GetSpans(expectedOutputMarkup, expectedOutput, expectedSpans)
                    Assert.Equal(expectedOutput, view.TextBuffer.CurrentSnapshot.AsText().ToString())

                    If verifyUndo Then
                        ' Ensure that after undo we go back to where we were to begin with.
                        Dim history = undoHistoryRegistry.GetHistory(document.GetTextBuffer())
                        history.Undo(originalSelections.Count)

                        Dim currentSnapshot = document.GetTextBuffer().CurrentSnapshot
                        Assert.Equal(originalSnapshot.GetText(), currentSnapshot.GetText())
                    End If
                End If
            End Using
        End Sub

        ''' <summary>
        ''' verifyUndo Is needed because of https://github.com/dotnet/roslyn/issues/28033
        ''' Most tests will continue to verifyUndo, but select tests will skip it due to
        ''' this known test infrastructure issure. This bug does Not represent a product
        ''' failure.
        ''' </summary>
        Private Sub TestHandled(inputMarkup As String, expectedOutputMarkup As String,
                                Optional verifyUndo As Boolean = True, Optional indentStyle As IndentStyle = IndentStyle.Smart)
            TestWorker(
                inputMarkup, expectedOutputMarkup,
                callback:=Sub()
                              Assert.True(False, "Should not reach here.")
                          End Sub,
                verifyUndo, indentStyle)
        End Sub

        Private Sub TestNotHandled(inputMarkup As String)
            Dim notHandled As Boolean = False
            TestWorker(
                inputMarkup, Nothing,
                callback:=Sub()
                              notHandled = True
                          End Sub
                )

            Assert.True(notHandled)
        End Sub

        <WorkItem(38516, "https://github.com/dotnet/roslyn/issues/38516")>
        <WpfFact, Trait(Traits.Feature, Traits.Features.SplitComment)>
        Public Sub TestSplitStartOfComment()
            TestHandled(
"Module Program
    Sub Main(args As String())
        '[||] Test Comment
    End Sub
End Module
",
"Module Program
    Sub Main(args As String())
        '
        ' Test Comment
    End Sub
End Module
")
        End Sub

        <WorkItem(38516, "https://github.com/dotnet/roslyn/issues/38516")>
        <WpfFact, Trait(Traits.Feature, Traits.Features.SplitComment)>
        Public Sub TestSplitMiddleOfComment()
            TestHandled(
"Module Program
    Sub Main(args As String())
        ' Test [||]Comment
    End Sub
End Module
",
"Module Program
    Sub Main(args As String())
        ' Test 
        'Comment
    End Sub
End Module
")
        End Sub

        <WorkItem(38516, "https://github.com/dotnet/roslyn/issues/38516")>
        <WpfFact, Trait(Traits.Feature, Traits.Features.SplitComment)>
        Public Sub TestSplitEndOfComment()
            TestHandled(
"Module Program
    Sub Main(args As String())
        ' Test Comment[||] 
    End Sub
End Module
",
"Module Program
    Sub Main(args As String())
        ' Test Comment
        ' 
    End Sub
End Module
")
        End Sub

        <WorkItem(38516, "https://github.com/dotnet/roslyn/issues/38516")>
        <WpfFact, Trait(Traits.Feature, Traits.Features.SplitComment)>
        Public Sub TestSplitCommentOutOfMethod()
            TestHandled(
"Module Program
    Sub Main(args As String())
        
    End Sub
    ' Test [||]Comment
End Module
",
"Module Program
    Sub Main(args As String())
        
    End Sub
    ' Test 
    'Comment
End Module
")
        End Sub

        <WorkItem(38516, "https://github.com/dotnet/roslyn/issues/38516")>
        <WpfFact, Trait(Traits.Feature, Traits.Features.SplitComment)>
        Public Sub TestSplitCommentOutOfModule()
            TestHandled(
"Module Program
    Sub Main(args As String())
        
    End Sub
End Module
' Test [||]Comment
",
"Module Program
    Sub Main(args As String())
        
    End Sub
End Module
' Test 
'Comment
")
        End Sub

        <WorkItem(38516, "https://github.com/dotnet/roslyn/issues/38516")>
        <WpfFact, Trait(Traits.Feature, Traits.Features.SplitComment)>
        Public Sub TestSplitCommentOutOfClass()
            TestHandled(
"Class Program
    Public Shared Sub Main(args As String())
        
    End Sub
End Class
' Test [||]Comment
",
"Class Program
    Public Shared Sub Main(args As String())
        
    End Sub
End Class
' Test 
'Comment
")
        End Sub

        <WorkItem(38516, "https://github.com/dotnet/roslyn/issues/38516")>
        <WpfFact, Trait(Traits.Feature, Traits.Features.SplitComment)>
        Public Sub TestSplitCommentOutOfNamespace()
            TestHandled(
"Namespace TestNamespace
    Module Program
        Sub Main(args As String())

        End Sub
    End Module
End Namespace
' Test [||]Comment
",
"Namespace TestNamespace
    Module Program
        Sub Main(args As String())

        End Sub
    End Module
End Namespace
' Test 
'Comment
")
        End Sub

        <WorkItem(38516, "https://github.com/dotnet/roslyn/issues/38516")>
        <WpfFact, Trait(Traits.Feature, Traits.Features.SplitComment)>
        Public Sub TestSplitCommentWithLineContinuation()
            TestHandled(
"Module Program
    Sub Main(args As String())
        Dim X As Integer _ ' Comment [||] is here
                       = 4
    End Sub
End Module
",
"Module Program
    Sub Main(args As String())
        Dim X As Integer _ ' Comment 
 _ ' is here
                       = 4
    End Sub
End Module
")
        End Sub
    End Class
End Namespace
