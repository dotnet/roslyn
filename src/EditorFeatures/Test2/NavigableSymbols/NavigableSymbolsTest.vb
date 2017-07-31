' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System
Imports System.Linq
Imports System.Threading
Imports System.Threading.Tasks
Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.Editor.Host
Imports Microsoft.CodeAnalysis.Editor.NavigableSymbols
Imports Microsoft.CodeAnalysis.Editor.UnitTests.BraceMatching
Imports Microsoft.CodeAnalysis.Editor.UnitTests.Utilities
Imports Microsoft.CodeAnalysis.Editor.UnitTests.Utilities.GoToHelpers
Imports Microsoft.CodeAnalysis.Editor.UnitTests.Workspaces
Imports Microsoft.CodeAnalysis.Navigation
Imports Microsoft.VisualStudio.Composition
Imports Microsoft.VisualStudio.Text
Imports Roslyn.Test.Utilities
Imports Xunit

Namespace Microsoft.CodeAnalysis.Editor.UnitTests.NavigableSymbols

    Public Class NavigableSymbolsTest

        Private Shared ReadOnly s_exportProvider As ExportProvider = MinimalTestExportProvider.CreateExportProvider(
            TestExportProvider.EntireAssemblyCatalogWithCSharpAndVisualBasic.WithParts(
                GetType(MockDocumentNavigationServiceProvider),
                GetType(MockSymbolNavigationServiceProvider)))

        <WpfFact>
        Public Async Function TestCharp() As Task
            Dim markup = "
class [|C|]
{
    C$$ c
}"
            Dim text As String = Nothing
            Dim position As Integer
            Dim span As TextSpan
            MarkupTestFile.GetPositionAndSpan(markup, text, position, span)

            Using workspace = TestWorkspace.CreateCSharp(text, exportProvider:=s_exportProvider)
                Await TestNavigated(workspace, position, span)
            End Using
        End Function

        <WpfFact>
        Public Async Function TestVB() As Task
            Dim markup = "
Class [|C|]
    Dim c as C$$
End Class"
            Dim text As String = Nothing
            Dim position As Integer
            Dim span As TextSpan
            MarkupTestFile.GetPositionAndSpan(markup, text, position, span)

            Using workspace = TestWorkspace.CreateVisualBasic(text, exportProvider:=s_exportProvider)
                Await TestNavigated(workspace, position, span)
            End Using
        End Function

        Private Async Function TestNavigated(workspace As TestWorkspace, position As Integer, span As TextSpan) As Task

            Dim presenter = {New Lazy(Of IStreamingFindUsagesPresenter)(Function() New MockStreamingFindUsagesPresenter(Sub() Return))}
            Dim service = New NavigableSymbolService(TestWaitIndicator.Default, presenter)
            Dim view = workspace.Documents.First().GetTextView()
            Dim buffer = workspace.Documents.First().GetTextBuffer()
            Dim triggerSpan = New SnapshotSpan(buffer.CurrentSnapshot, New Span(position, 0))
            Dim source = service.TryCreateNavigableSymbolSource(view, buffer)
            Dim symbol = Await source.GetNavigableSymbolAsync(triggerSpan, CancellationToken.None)

            Assert.NotNull(symbol)
            symbol.Navigate(symbol.Relationships.First())

            Dim navigationService = DirectCast(workspace.Services.GetService(Of IDocumentNavigationService)(), MockDocumentNavigationServiceProvider.MockDocumentNavigationService)
            Assert.Equal(True, navigationService.TryNavigateToLineAndOffsetReturnValue)
            Assert.Equal(True, navigationService.TryNavigateToPositionReturnValue)
            Assert.Equal(True, navigationService.TryNavigateToSpanReturnValue)
            Assert.Equal(span, navigationService.ProvidedTextSpan)
        End Function
    End Class
End Namespace
