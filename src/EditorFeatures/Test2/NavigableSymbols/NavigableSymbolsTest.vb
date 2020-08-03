' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Collections.Immutable
Imports System.Threading
Imports Microsoft.CodeAnalysis.Editor.NavigableSymbols
Imports Microsoft.CodeAnalysis.Editor.Shared.Utilities
Imports Microsoft.CodeAnalysis.Editor.UnitTests.Utilities
Imports Microsoft.CodeAnalysis.Editor.UnitTests.Utilities.GoToHelpers
Imports Microsoft.CodeAnalysis.Editor.UnitTests.Workspaces
Imports Microsoft.CodeAnalysis.Navigation
Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.Text.Shared.Extensions
Imports Microsoft.VisualStudio.Composition
Imports Microsoft.VisualStudio.Language.Intellisense
Imports Microsoft.VisualStudio.Text

Namespace Microsoft.CodeAnalysis.Editor.UnitTests.NavigableSymbols

    <[UseExportProvider]>
    Public Class NavigableSymbolsTest

        Private Shared ReadOnly s_composition As TestComposition = EditorTestCompositions.EditorFeatures.AddParts(
            GetType(MockDocumentNavigationServiceProvider),
            GetType(MockSymbolNavigationServiceProvider))

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

            Using workspace = TestWorkspace.CreateCSharp(text, composition:=s_composition)
                Await TestNavigated(workspace, position.Value, spans)
            End Using
        End Function

        <WorkItem(23030, "https://github.com/dotnet/roslyn/issues/23030")>
        <WpfFact, Trait(Traits.Feature, Traits.Features.NavigableSymbols)>
        Public Async Function TestCharpLiteral() As Task
            Dim markup = "int x = 1$$23;"
            Dim text As String = Nothing
            Dim position As Integer? = Nothing
            Dim spans As IDictionary(Of String, ImmutableArray(Of TextSpan)) = Nothing
            MarkupTestFile.GetPositionAndSpans(markup, text, position, spans)

            Using workspace = TestWorkspace.CreateCSharp(text, composition:=s_composition)
                Await TestNotNavigated(workspace, position.Value)
            End Using
        End Function

        <WorkItem(23030, "https://github.com/dotnet/roslyn/issues/23030")>
        <WpfFact, Trait(Traits.Feature, Traits.Features.NavigableSymbols)>
        Public Async Function TestCharpStringLiteral() As Task
            Dim markup = "string x = ""w$$ow"";"
            Dim text As String = Nothing
            Dim position As Integer? = Nothing
            Dim spans As IDictionary(Of String, ImmutableArray(Of TextSpan)) = Nothing
            MarkupTestFile.GetPositionAndSpans(markup, text, position, spans)

            Using workspace = TestWorkspace.CreateCSharp(text, composition:=s_composition)
                Await TestNotNavigated(workspace, position.Value)
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

            Using workspace = TestWorkspace.CreateVisualBasic(text, composition:=s_composition)
                Await TestNavigated(workspace, position.Value, spans)
            End Using
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.NavigableSymbols)>
        <WorkItem(23030, "https://github.com/dotnet/roslyn/issues/23030")>
        Public Async Function TestVBLiteral() As Task
            Dim markup = "Dim x as Integer = 1$$23"
            Dim text As String = Nothing
            Dim position As Integer? = Nothing
            Dim spans As IDictionary(Of String, ImmutableArray(Of TextSpan)) = Nothing
            MarkupTestFile.GetPositionAndSpans(markup, text, position, spans)

            Using workspace = TestWorkspace.CreateVisualBasic(text, composition:=s_composition)
                Await TestNotNavigated(workspace, position.Value)
            End Using
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.NavigableSymbols)>
        <WorkItem(23030, "https://github.com/dotnet/roslyn/issues/23030")>
        Public Async Function TestVBStringLiteral() As Task
            Dim markup = "Dim x as String = ""w$$ow"";"
            Dim text As String = Nothing
            Dim position As Integer? = Nothing
            Dim spans As IDictionary(Of String, ImmutableArray(Of TextSpan)) = Nothing
            MarkupTestFile.GetPositionAndSpans(markup, text, position, spans)

            Using workspace = TestWorkspace.CreateVisualBasic(text, composition:=s_composition)
                Await TestNotNavigated(workspace, position.Value)
            End Using
        End Function

        Private Shared Function ExtractSymbol(workspace As TestWorkspace, position As Integer) As Task(Of INavigableSymbol)
            Dim threadingContext = workspace.ExportProvider.GetExportedValue(Of IThreadingContext)()
            Dim presenter = New MockStreamingFindUsagesPresenter(Sub() Return)
            Dim service = New NavigableSymbolService(TestWaitIndicator.Default, threadingContext, presenter)
            Dim view = workspace.Documents.First().GetTextView()
            Dim buffer = workspace.Documents.First().GetTextBuffer()
            Dim triggerSpan = New SnapshotSpan(buffer.CurrentSnapshot, New Span(position, 0))
            Dim source = service.TryCreateNavigableSymbolSource(view, buffer)
            Return source.GetNavigableSymbolAsync(triggerSpan, CancellationToken.None)
        End Function

        Private Shared Async Function TestNavigated(workspace As TestWorkspace, position As Integer, spans As IDictionary(Of String, ImmutableArray(Of TextSpan))) As Task
            Dim symbol = Await ExtractSymbol(workspace, position)

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

        Private Shared Async Function TestNotNavigated(workspace As TestWorkspace, position As Integer) As Task
            Dim symbol = Await ExtractSymbol(workspace, position)
            Assert.Null(symbol)
        End Function
    End Class
End Namespace
