' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System
Imports System.Linq
Imports System.Collections.Immutable
Imports System.Threading
Imports System.Threading.Tasks
Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.Text.Shared.Extensions
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

        <WpfFact, Trait(Traits.Feature, Traits.Features.NavigableSymbols)>
        Public Async Function TestCharp() As Task
            Dim markup = "
class {|target:C|}
{
    {|highlighted:C|}$$ c
}"
            Dim text As String = Nothing
            Dim position As Integer? = Nothing
            Dim spans As IDictionary(Of String, ImmutableArray(Of TextSpan)) = Nothing
            MarkupTestFile.GetPositionAndSpans(markup, text, position, spans)

            Using workspace = TestWorkspace.CreateCSharp(text, exportProvider:=s_exportProvider)
                Await TestNavigated(workspace, position.Value, spans)
            End Using
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.NavigableSymbols)>
        Public Async Function TestVB() As Task
            Dim markup = "
Class {|target:C|}
    Dim c as {|highlighted:C|}$$
End Class"
            Dim text As String = Nothing
            Dim position As Integer? = Nothing
            Dim spans As IDictionary(Of String, ImmutableArray(Of TextSpan)) = Nothing
            MarkupTestFile.GetPositionAndSpans(markup, text, position, spans)

            Using workspace = TestWorkspace.CreateVisualBasic(text, exportProvider:=s_exportProvider)
                Await TestNavigated(workspace, position.Value, spans)
            End Using
        End Function

        Private Async Function TestNavigated(workspace As TestWorkspace, position As Integer, spans As IDictionary(Of String, ImmutableArray(Of TextSpan))) As Task
            Dim presenter = {New Lazy(Of IStreamingFindUsagesPresenter)(Function() New MockStreamingFindUsagesPresenter(Sub() Return))}
            Dim service = New NavigableSymbolService(TestWaitIndicator.Default, presenter)
            Dim view = workspace.Documents.First().GetTextView()
            Dim buffer = workspace.Documents.First().GetTextBuffer()
            Dim triggerSpan = New SnapshotSpan(buffer.CurrentSnapshot, New Span(position, 0))
            Dim source = service.TryCreateNavigableSymbolSource(view, buffer)
            Dim symbol = Await source.GetNavigableSymbolAsync(triggerSpan, CancellationToken.None)

            Dim highlightedSpan = spans("highlighted").First()
            Dim navigationTarget = spans("target").First()

            Assert.NotNull(symbol)
            Assert.Equal(highlightedSpan.ToSpan(), symbol.SymbolSpan.Span)

            symbol.Navigate(symbol.Relationships.First())

            Dim navigationService = DirectCast(workspace.Services.GetService(Of IDocumentNavigationService)(), MockDocumentNavigationServiceProvider.MockDocumentNavigationService)
            Assert.Equal(True, navigationService.TryNavigateToLineAndOffsetReturnValue)
            Assert.Equal(True, navigationService.TryNavigateToPositionReturnValue)
            Assert.Equal(True, navigationService.TryNavigateToSpanReturnValue)
            Assert.Equal(navigationTarget, navigationService.ProvidedTextSpan)
        End Function
    End Class
End Namespace
