' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Collections.Immutable
Imports System.Threading
Imports Microsoft.CodeAnalysis.Editor.NavigableSymbols
Imports Microsoft.CodeAnalysis.Editor.Shared.Utilities
Imports Microsoft.CodeAnalysis.Editor.UnitTests.Utilities
Imports Microsoft.CodeAnalysis.Navigation
Imports Microsoft.CodeAnalysis.Shared.TestHooks
Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.Text.Shared.Extensions
Imports Microsoft.VisualStudio.Language.Intellisense
Imports Microsoft.VisualStudio.Text
Imports Microsoft.VisualStudio.Utilities

Namespace Microsoft.CodeAnalysis.Editor.UnitTests.NavigableSymbols
    <[UseExportProvider]>
    <Trait(Traits.Feature, Traits.Features.NavigableSymbols)>
    Public Class NavigableSymbolsTest
        Private Shared ReadOnly s_composition As TestComposition = EditorTestCompositions.EditorFeatures.AddParts(
            GetType(MockDocumentNavigationServiceProvider),
            GetType(MockSymbolNavigationServiceProvider))

        <WpfFact>
        Public Async Function TestCharp() As Task
            Dim markup = "
class {|target:|}C
{
    {|highlighted:C|}$$ c
}"
            Dim text As String = Nothing
            Dim position As Integer? = Nothing
            Dim spans As IDictionary(Of String, ImmutableArray(Of TextSpan)) = Nothing
            MarkupTestFile.GetPositionAndSpans(markup, text, position, spans)

            Using workspace = EditorTestWorkspace.CreateCSharp(text, composition:=s_composition)
                Await TestNavigated(workspace, position.Value, spans)
            End Using
        End Function

        <WpfFact, WorkItem("https://github.com/dotnet/roslyn/issues/23030")>
        Public Async Function TestCharpLiteral() As Task
            Dim markup = "int x = {|highlighted:1$$23|};"
            Dim text As String = Nothing
            Dim position As Integer? = Nothing
            Dim spans As IDictionary(Of String, ImmutableArray(Of TextSpan)) = Nothing
            MarkupTestFile.GetPositionAndSpans(markup, text, position, spans)

            Using workspace = EditorTestWorkspace.CreateCSharp(text, composition:=s_composition)
                Await TestNavigated(workspace, position.Value, spans)
            End Using
        End Function

        <WpfFact, WorkItem("https://github.com/dotnet/roslyn/issues/23030")>
        Public Async Function TestCharpStringLiteral() As Task
            Dim markup = "string x = {|highlighted:""w$$ow""|};"
            Dim text As String = Nothing
            Dim position As Integer? = Nothing
            Dim spans As IDictionary(Of String, ImmutableArray(Of TextSpan)) = Nothing
            MarkupTestFile.GetPositionAndSpans(markup, text, position, spans)

            Using workspace = EditorTestWorkspace.CreateCSharp(text, composition:=s_composition)
                Await TestNavigated(workspace, position.Value, spans)
            End Using
        End Function

        <WpfFact>
        Public Async Function TestVB() As Task
            Dim markup = "
Class {|target:|}C
    Dim c as {|highlighted:C|}$$
End Class"
            Dim text As String = Nothing
            Dim position As Integer? = Nothing
            Dim spans As IDictionary(Of String, ImmutableArray(Of TextSpan)) = Nothing
            MarkupTestFile.GetPositionAndSpans(markup, text, position, spans)

            Using workspace = EditorTestWorkspace.CreateVisualBasic(text, composition:=s_composition)
                Await TestNavigated(workspace, position.Value, spans)
            End Using
        End Function

        <WpfFact, WorkItem("https://github.com/dotnet/roslyn/issues/23030")>
        Public Async Function TestVBLiteral() As Task
            Dim markup = "Dim x as Integer = {|highlighted:1$$23|}"
            Dim text As String = Nothing
            Dim position As Integer? = Nothing
            Dim spans As IDictionary(Of String, ImmutableArray(Of TextSpan)) = Nothing
            MarkupTestFile.GetPositionAndSpans(markup, text, position, spans)

            Using workspace = EditorTestWorkspace.CreateVisualBasic(text, composition:=s_composition)
                Await TestNavigated(workspace, position.Value, spans)
            End Using
        End Function

        <WpfFact, WorkItem("https://github.com/dotnet/roslyn/issues/23030")>
        Public Async Function TestVBStringLiteral() As Task
            Dim markup = "Dim x as String = {|highlighted:""w$$ow""|};"
            Dim text As String = Nothing
            Dim position As Integer? = Nothing
            Dim spans As IDictionary(Of String, ImmutableArray(Of TextSpan)) = Nothing
            MarkupTestFile.GetPositionAndSpans(markup, text, position, spans)

            Using workspace = EditorTestWorkspace.CreateVisualBasic(text, composition:=s_composition)
                Await TestNavigated(workspace, position.Value, spans)
            End Using
        End Function

        Private Shared Function ExtractSymbol(workspace As EditorTestWorkspace, position As Integer) As Task(Of INavigableSymbol)
            Dim threadingContext = workspace.ExportProvider.GetExportedValue(Of IThreadingContext)()
            Dim listenerProvider = workspace.ExportProvider.GetExportedValue(Of IAsynchronousOperationListenerProvider)
            Dim service = New NavigableSymbolService(threadingContext, listenerProvider)
            Dim view = workspace.Documents.First().GetTextView()
            Dim buffer = workspace.Documents.First().GetTextBuffer()
            Dim triggerSpan = New SnapshotSpan(buffer.CurrentSnapshot, New Span(position, 0))
            Dim source = service.TryCreateNavigableSymbolSource(view, buffer)
            Return source.GetNavigableSymbolAsync(triggerSpan, CancellationToken.None)
        End Function

        Private Shared Async Function TestNavigated(workspace As EditorTestWorkspace, position As Integer, spans As IDictionary(Of String, ImmutableArray(Of TextSpan))) As Task
            Dim symbol = Await ExtractSymbol(workspace, position)

            Dim highlightedSpan = spans("highlighted").First()

            Assert.NotNull(symbol)
            Assert.Equal(highlightedSpan.ToSpan(), symbol.SymbolSpan.Span)

            Dim listenerProvider = workspace.ExportProvider.GetExportedValue(Of IAsynchronousOperationListenerProvider)
            symbol.Navigate(symbol.Relationships.First())
            Await listenerProvider.GetWaiter(FeatureAttribute.NavigableSymbols).ExpeditedWaitAsync()

            Dim navigationService = DirectCast(workspace.Services.GetService(Of IDocumentNavigationService)(), MockDocumentNavigationServiceProvider.MockDocumentNavigationService)
            Assert.Equal(True, navigationService.TryNavigateToPositionReturnValue)
            Assert.Equal(True, navigationService.TryNavigateToSpanReturnValue)

            Dim value As ImmutableArray(Of TextSpan) = Nothing
            If spans.TryGetValue("target", value) Then
                Assert.Equal(value.First().Start, navigationService.ProvidedPosition)
            End If
        End Function
    End Class
End Namespace
