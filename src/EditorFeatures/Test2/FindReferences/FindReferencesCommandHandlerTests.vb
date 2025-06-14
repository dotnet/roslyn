' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Threading
Imports Microsoft.CodeAnalysis.Editor.Host
Imports Microsoft.CodeAnalysis.Editor.Shared.Utilities
Imports Microsoft.CodeAnalysis.FindReferences
Imports Microsoft.CodeAnalysis.FindUsages
Imports Microsoft.CodeAnalysis.Shared.TestHooks
Imports Microsoft.CodeAnalysis.Test.Utilities.FindUsages
Imports Microsoft.CodeAnalysis.Text.Shared.Extensions
Imports Microsoft.VisualStudio.Text.Editor.Commanding.Commands

Namespace Microsoft.CodeAnalysis.Editor.UnitTests.FindReferences
    Public Class FindReferencesCommandHandlerTests
        <WorkItem("https://developercommunity.visualstudio.com/content/problem/47594/c-postfix-operators-inhibit-find-all-references-sh.html")>
        <WpfFact(Skip:="https://github.com/dotnet/roslyn/issues/24794"), Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function TestSelection() As Task
            Dim source = "
class C
{
    void M()
    {
        int {|Definition:yyy|} = 0;
        {|Reference:{|Selection:yyy|}|}++;
        {|Reference:yyy|}++;
    }
}"
            Using workspace = EditorTestWorkspace.CreateCSharp(source)
                Dim testDocument = workspace.Documents.Single()

                Dim view = testDocument.GetTextView()
                Dim textBuffer = view.TextBuffer
                Dim snapshot = textBuffer.CurrentSnapshot

                view.Selection.Select(
                    testDocument.AnnotatedSpans("Selection").Single().ToSnapshotSpan(snapshot), isReversed:=False)

                Dim listenerProvider = workspace.ExportProvider.GetExportedValue(Of IAsynchronousOperationListenerProvider)

                Dim context = New FindUsagesTestContext()
                Dim commandHandler = New FindReferencesCommandHandler(
                    New FindReferencesNavigationService(
                        workspace.ExportProvider.GetExportedValue(Of IThreadingContext)(),
                        New MockStreamingFindReferencesPresenter(context),
                        listenerProvider,
                        workspace.GlobalOptions))

                Dim document = workspace.CurrentSolution.GetDocument(testDocument.Id)
                commandHandler.ExecuteCommand(
                    New FindReferencesCommandArgs(view, textBuffer), Utilities.TestCommandExecutionContext.Create())

                ' Wait for the find refs to be done.
                Await listenerProvider.GetWaiter(FeatureAttribute.FindReferences).ExpeditedWaitAsync()

                Assert.Equal(1, context.Definitions.Count)
                Assert.Equal(testDocument.AnnotatedSpans("Definition").Single(),
                             context.Definitions(0).SourceSpans.Single().SourceSpan)
                Assert.Equal(testDocument.AnnotatedSpans("Reference").Count,
                             context.References.Count)

                AssertEx.SetEqual(testDocument.AnnotatedSpans("Reference"),
                                  context.References.Select(Function(r) r.SourceSpan.SourceSpan))
            End Using
        End Function

        Private Class MockStreamingFindReferencesPresenter
            Implements IStreamingFindUsagesPresenter

            Private ReadOnly _context As FindUsagesTestContext

            Public Sub New(context As FindUsagesTestContext)
                _context = context
            End Sub

            Public Sub ClearAll() Implements IStreamingFindUsagesPresenter.ClearAll
            End Sub

            Public Function StartSearch(title As String, options As StreamingFindUsagesPresenterOptions) As (FindUsagesContext, CancellationToken) Implements IStreamingFindUsagesPresenter.StartSearch
                Return (_context, CancellationToken.None)
            End Function
        End Class
    End Class
End Namespace
