Imports System.Threading
Imports System.Threading.Tasks
Imports Microsoft.CodeAnalysis.Editor.Host
Imports Microsoft.CodeAnalysis.Editor.NavigableSymbols
Imports Microsoft.CodeAnalysis.Editor.UnitTests.Utilities
Imports Microsoft.CodeAnalysis.Editor.UnitTests.Utilities.GoToHelpers
Imports Microsoft.CodeAnalysis.Editor.UnitTests.Workspaces
Imports Microsoft.CodeAnalysis.Navigation
Imports Microsoft.VisualStudio.Composition
Imports Microsoft.VisualStudio.Text

Namespace Microsoft.CodeAnalysis.Editor.UnitTests.NavigableSymbols
    Public Class NavigableSymbolsTests
        Private Shared ReadOnly s_ExportProvider As ExportProvider =
            MinimalTestExportProvider.CreateExportProvider(
                TestExportProvider.EntireAssemblyCatalogWithCSharpAndVisualBasic.WithParts(
                    GetType(MockDocumentNavigationService), GetType(MockSymbolNavigationServiceProvider)))

        <WpfFact>
        Public Async Function TestCSharp() As Task
            Using workspace = TestWorkspace.CreateCSharp(<text>
class C
{
    C$$ c;
}</text>.Value, exportProvider:=s_ExportProvider)
                Await TestNavigated(workspace)
            End Using
        End Function

        <WpfFact>
        Public Async Function TestVB() As Task
            Using workspace = TestWorkspace.CreateCSharp(<text>
Class C
    Dim c as C$$;
End Class</text>.Value, exportProvider:=s_ExportProvider)
                Await TestNavigated(workspace)
            End Using
        End Function

        Private Async Function TestNavigated(workspace As TestWorkspace) As Task
            Dim presenter = {New Lazy(Of IStreamingFindUsagesPresenter)((Function() New MockStreamingFindUsagesPresenter(Sub() Return)))}
            Dim service = New NavigableSymbolService(TestWaitIndicator.Default, presenter)

            Dim view = workspace.Documents.First().GetTextView()
            Dim Buffer = workspace.Documents.First().GetTextBuffer()
            Dim caretPosition = view.Caret.Position.BufferPosition.Position
            Dim span = New SnapshotSpan(Buffer.CurrentSnapshot, New Span(caretPosition, 0))
            Dim source = service.TryCreateNavigableSymbolSource(view, Buffer)
            Dim symbol = Await source.GetNavigableSymbolAsync(span, CancellationToken.None)

            Assert.NotNull(symbol)
            symbol.Navigate(symbol.Relationships.First())

            Dim navigationService = DirectCast(workspace.Services.GetService(Of IDocumentNavigationService)(), MockDocumentNavigationServiceProvider.MockDocumentNavigationService)
            Assert.Equal(True, navigationService.TryNavigateToLineAndOffsetReturnValue)
            Assert.Equal(True, navigationService.TryNavigateToPositionReturnValue)
            Assert.Equal(True, navigationService.TryNavigateToSpanReturnValue)
        End Function
    End Class
End Namespace

