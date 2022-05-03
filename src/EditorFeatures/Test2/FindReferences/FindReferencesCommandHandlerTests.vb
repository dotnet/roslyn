' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Threading
Imports Microsoft.CodeAnalysis.Editor.Host
Imports Microsoft.CodeAnalysis.Editor.UnitTests.Workspaces
Imports Microsoft.CodeAnalysis.FindReferences
Imports Microsoft.CodeAnalysis.FindUsages
Imports Microsoft.CodeAnalysis.Shared.TestHooks
Imports Microsoft.CodeAnalysis.Text.Shared.Extensions
Imports Microsoft.VisualStudio.Text.Editor.Commanding.Commands

Namespace Microsoft.CodeAnalysis.Editor.UnitTests.FindReferences
    Public Class FindReferencesCommandHandlerTests
        <WorkItem(47594, "https://developercommunity.visualstudio.com/content/problem/47594/c-postfix-operators-inhibit-find-all-references-sh.html")>
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
            Using workspace = TestWorkspace.CreateCSharp(source)
                Dim testDocument = workspace.Documents.Single()

                Dim view = testDocument.GetTextView()
                Dim textBuffer = view.TextBuffer
                Dim snapshot = textBuffer.CurrentSnapshot

                view.Selection.Select(
                    testDocument.AnnotatedSpans("Selection").Single().ToSnapshotSpan(snapshot), isReversed:=False)

                Dim listenerProvider = workspace.ExportProvider.GetExportedValue(Of IAsynchronousOperationListenerProvider)

                Dim context = New FindReferencesTests.TestContext()
                Dim commandHandler = New FindReferencesCommandHandler(
                    New MockStreamingFindReferencesPresenter(context),
                    workspace.GlobalOptions,
                    listenerProvider)

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

            Private ReadOnly _context As FindReferencesTests.TestContext

            Public Sub New(context As FindReferencesTests.TestContext)
                _context = context
            End Sub

            Public Sub ClearAll() Implements IStreamingFindUsagesPresenter.ClearAll
            End Sub

            Public Function StartSearch(title As String, supportsReferences As Boolean) As (FindUsagesContext, CancellationToken) Implements IStreamingFindUsagesPresenter.StartSearch
                Return (_context, CancellationToken.None)
            End Function

            Public Function StartSearchWithCustomColumns(title As String, supportsReferences As Boolean, includeContainingTypeAndMemberColumns As Boolean, includeKindColumn As Boolean) As (FindUsagesContext, CancellationToken) Implements IStreamingFindUsagesPresenter.StartSearchWithCustomColumns
                Return (_context, CancellationToken.None)
            End Function
        End Class
    End Class
End Namespace
