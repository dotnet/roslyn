' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Collections.Immutable
Imports System.ComponentModel.Composition
Imports System.Threading
Imports System.Windows
Imports System.Windows.Controls
Imports System.Windows.Media
Imports Microsoft.CodeAnalysis
Imports Microsoft.CodeAnalysis.Classification
Imports Microsoft.CodeAnalysis.CSharp.Syntax
Imports Microsoft.CodeAnalysis.Diagnostics
Imports Microsoft.CodeAnalysis.FindUsages
Imports Microsoft.CodeAnalysis.Editor.UnitTests
Imports Microsoft.CodeAnalysis.Editor.UnitTests.Diagnostics
Imports Microsoft.CodeAnalysis.Editor.UnitTests.Workspaces
Imports Microsoft.CodeAnalysis.Host
Imports Microsoft.CodeAnalysis.Host.Mef
Imports Microsoft.CodeAnalysis.Shared.Extensions
Imports Microsoft.CodeAnalysis.Test.Utilities
Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.VisualStudio.Composition
Imports Microsoft.VisualStudio.LanguageServices.CodeLens
Imports Microsoft.VisualStudio.LanguageServices.FindUsages
Imports Microsoft.VisualStudio.OLE.Interop
Imports Microsoft.VisualStudio.Shell
Imports Microsoft.VisualStudio.Shell.FindAllReferences
Imports Microsoft.VisualStudio.Shell.TableControl
Imports Microsoft.VisualStudio.Shell.TableManager
Imports Microsoft.VisualStudio.Text
Imports Roslyn.Test.Utilities
Imports Microsoft.CodeAnalysis.Editor.Host

Namespace Microsoft.VisualStudio.LanguageServices.UnitTests.Venus

    <UseExportProvider>
    <Trait(Traits.Feature, Traits.Features.FindReferences)>
    Public Class DocumentService_IntegrationTests
        Private Shared ReadOnly s_compositionWithMockDiagnosticUpdateSourceRegistrationService As TestComposition = EditorTestCompositions.EditorFeatures

        <WpfFact>
        Public Async Function TestFindUsageIntegration() As System.Threading.Tasks.Task
            Dim input =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document FilePath="Original.cs">
class {|Original:C|}
{
        [|C|]$$ c;
}
        </Document>
        <Document FilePath="Mapped.cs">
class {|Definition:C1|}
{
        [|C1|] c;
}reference 
        </Document>
    </Project>
</Workspace>

            ' TODO: Use VisualStudioTestComposition or move the feature down to EditorFeatures and avoid dependency on IServiceProvider.
            ' https://github.com/dotnet/roslyn/issues/46279
            Dim composition = EditorTestCompositions.EditorFeatures.AddParts(GetType(MockServiceProvider))

            Using workspace = EditorTestWorkspace.Create(input, composition:=composition, documentServiceProvider:=TestDocumentServiceProvider.Instance)

                Dim presenter = New StreamingFindUsagesPresenter(workspace, workspace.ExportProvider.AsExportProvider())
                Dim tuple = presenter.StartSearch("test", New StreamingFindUsagesPresenterOptions() With {.SupportsReferences = True})
                Dim context = tuple.context

                Dim cursorDocument = workspace.Documents.First(Function(d) d.CursorPosition.HasValue)
                Dim cursorPosition = cursorDocument.CursorPosition.Value

                Dim startDocument = workspace.CurrentSolution.GetDocument(cursorDocument.Id)
                Assert.NotNull(startDocument)

                Dim classificationOptions = workspace.GlobalOptions.GetClassificationOptionsProvider()

                Dim findRefsService = startDocument.GetLanguageService(Of IFindUsagesService)
                Await findRefsService.FindReferencesAsync(context, startDocument, cursorPosition, classificationOptions, CancellationToken.None)

                Dim definitionDocument = workspace.Documents.First(Function(d) d.AnnotatedSpans.ContainsKey("Definition"))
                Dim definitionText = Await workspace.CurrentSolution.GetDocument(definitionDocument.Id).GetTextAsync()

                Dim definitionSpan = definitionDocument.AnnotatedSpans("Definition").Single()
                Dim referenceSpan = definitionDocument.SelectedSpans.First()
                Dim expected = {
                    (definitionDocument.Name, definitionText.Lines.GetLinePositionSpan(definitionSpan).Start, definitionText.Lines.GetLineFromPosition(definitionSpan.Start).ToString().Trim()),
                    (definitionDocument.Name, definitionText.Lines.GetLinePositionSpan(referenceSpan).Start, definitionText.Lines.GetLineFromPosition(referenceSpan.Start).ToString().Trim())}

                Dim factory = TestFindAllReferencesService.Instance.LastWindow.MyTableManager.LastSink.LastFactory
                Dim snapshot = factory.GetCurrentSnapshot()

                Dim actual = New List(Of (String, LinePosition, String))

                For i = 0 To snapshot.Count - 1
                    Dim name As Object = Nothing
                    Dim line As Object = Nothing
                    Dim position As Object = Nothing
                    Dim content As Object = Nothing

                    Assert.True(snapshot.TryGetValue(i, StandardTableKeyNames.DocumentName, name))
                    Assert.True(snapshot.TryGetValue(i, StandardTableKeyNames.Line, line))
                    Assert.True(snapshot.TryGetValue(i, StandardTableKeyNames.Column, position))
                    Assert.True(snapshot.TryGetValue(i, StandardTableKeyNames.Text, content))

                    actual.Add((DirectCast(name, String), New LinePosition(CType(line, Integer), CType(position, Integer)), DirectCast(content, String)))
                Next

                ' confirm that all FAR results are mapped to ones in mapped.cs file rather than ones in original.cs
                AssertEx.SetEqual(expected, actual)
            End Using
        End Function

        <WpfFact>
        Public Async Function TestCodeLensIntegration() As System.Threading.Tasks.Task
            Dim input =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document FilePath="Original.cs">
class {|Original:C|}
{
        [|C|] c { get; };
}
        </Document>
        <Document FilePath="Mapped.cs">
class {|Definition:C1|}
{
        [|C1|] c { get; };
}reference 
        </Document>
    </Project>
</Workspace>

            Using workspace = EditorTestWorkspace.Create(input, documentServiceProvider:=TestDocumentServiceProvider.Instance)

                Dim codelensService = New RemoteCodeLensReferencesService(workspace.GlobalOptions)

                Dim originalDocument = workspace.Documents.First(Function(d) d.AnnotatedSpans.ContainsKey("Original"))

                Dim startDocument = workspace.CurrentSolution.GetDocument(originalDocument.Id)
                Assert.NotNull(startDocument)

                Dim root = Await startDocument.GetSyntaxRootAsync()
                Dim node = root.FindNode(originalDocument.AnnotatedSpans("Original").First()).AncestorsAndSelf().OfType(Of ClassDeclarationSyntax).First()
                Dim results = Await codelensService.FindReferenceLocationsAsync(workspace.CurrentSolution, startDocument.Id, node, CancellationToken.None)
                Assert.True(results.HasValue)

                Dim definitionDocument = workspace.Documents.First(Function(d) d.AnnotatedSpans.ContainsKey("Definition"))
                Dim definitionText = Await workspace.CurrentSolution.GetDocument(definitionDocument.Id).GetTextAsync()

                Dim referenceSpan = definitionDocument.SelectedSpans.First()
                Dim expected = {(definitionDocument.Name, definitionText.Lines.GetLinePositionSpan(referenceSpan).Start, definitionText.Lines.GetLineFromPosition(referenceSpan.Start).ToString())}

                Dim actual = New List(Of (String, LinePosition, String))

                For Each result In results.Value
                    actual.Add((result.FilePath, New LinePosition(result.LineNumber, result.ColumnNumber), result.ReferenceLineText))
                Next

                ' confirm that all FAR results are mapped to ones in mapped.cs file rather than ones in original.cs
                AssertEx.SetEqual(expected, actual)
            End Using
        End Function

        <InlineData(True)>
        <InlineData(False)>
        <WpfTheory>
        Public Async Function TestDocumentOperationCanApplyChange(ignoreUnchangeableDocuments As Boolean) As System.Threading.Tasks.Task
            Dim input =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document FilePath="Original.cs">
class C { }
        </Document>
    </Project>
</Workspace>

            Using workspace = EditorTestWorkspace.Create(input, documentServiceProvider:=TestDocumentServiceProvider.Instance, ignoreUnchangeableDocumentsWhenApplyingChanges:=ignoreUnchangeableDocuments)

                Dim document = workspace.CurrentSolution.GetDocument(workspace.Documents.First().Id)

                ' made a change
                Dim newDocument = document.WithText(SourceText.From(""))

                ' confirm the change
                Assert.Equal(String.Empty, (Await newDocument.GetTextAsync()).ToString())

                ' confirm apply changes are not supported
                Assert.False(document.CanApplyChange())

                Assert.Equal(ignoreUnchangeableDocuments, workspace.IgnoreUnchangeableDocumentsWhenApplyingChanges)

                ' see whether changes can be applied to the solution
                If ignoreUnchangeableDocuments Then
                    Assert.True(workspace.TryApplyChanges(newDocument.Project.Solution))

                    ' Changes should not be made if Workspace.IgnoreUnchangeableDocumentsWhenApplyingChanges is true
                    Dim currentDocument = workspace.CurrentSolution.GetDocument(document.Id)
                    Assert.True(currentDocument.GetTextSynchronously(CancellationToken.None).ContentEquals(document.GetTextSynchronously(CancellationToken.None)))
                Else
                    ' should throw if Workspace.IgnoreUnchangeableDocumentsWhenApplyingChanges is false
                    Assert.Throws(Of NotSupportedException)(Sub() workspace.TryApplyChanges(newDocument.Project.Solution))
                End If
            End Using
        End Function

        <WpfFact>
        Public Async Function TestDocumentOperationCanApplySupportDiagnostics() As System.Threading.Tasks.Task
            Dim input =
<Workspace>
    <Project Language="C#" CommonReferences="true">
        <Document FilePath="Original.cs">
class { }
        </Document>
    </Project>
</Workspace>

            Using workspace = EditorTestWorkspace.Create(input, composition:=s_compositionWithMockDiagnosticUpdateSourceRegistrationService, documentServiceProvider:=TestDocumentServiceProvider.Instance)
                Dim analyzerReference = New TestAnalyzerReferenceByLanguage(DiagnosticExtensions.GetCompilerDiagnosticAnalyzersMap())
                workspace.TryApplyChanges(workspace.CurrentSolution.WithAnalyzerReferences({analyzerReference}))

                Dim document = workspace.CurrentSolution.GetDocument(workspace.Documents.First().Id)

                Dim model = Await document.GetSemanticModelAsync()

                ' confirm there are errors
                Assert.True(model.GetDiagnostics().Any())

                Dim diagnosticService = Assert.IsType(Of DiagnosticAnalyzerService)(workspace.GetService(Of IDiagnosticAnalyzerService)())

                ' confirm diagnostic support is off for the document
                Assert.False(document.SupportsDiagnostics())

                ' register the workspace to the service
                diagnosticService.CreateIncrementalAnalyzer(workspace)

                ' confirm that IDE doesn't report the diagnostics
                Dim diagnostics = Await diagnosticService.GetDiagnosticsAsync(workspace.CurrentSolution, projectId:=Nothing, documentId:=document.Id,
                                                                              includeSuppressedDiagnostics:=False, includeNonLocalDocumentDiagnostics:=True,
                                                                              CancellationToken.None)
                Assert.False(diagnostics.Any())
            End Using
        End Function

        Private Class TestDocumentServiceProvider
            Implements IDocumentServiceProvider

            Public Shared ReadOnly Instance As TestDocumentServiceProvider = New TestDocumentServiceProvider()

            Public Function GetService(Of TService As {Class, IDocumentService})() As TService Implements IDocumentServiceProvider.GetService
                If TypeOf SpanMapper.Instance Is TService Then
                    Return TryCast(SpanMapper.Instance, TService)
                ElseIf TypeOf Excerpter.Instance Is TService Then
                    Return TryCast(Excerpter.Instance, TService)
                ElseIf TypeOf DocumentOperations.Instance Is TService Then
                    Return TryCast(DocumentOperations.Instance, TService)
                End If

                Return Nothing
            End Function

            Private Class SpanMapper
                Implements ISpanMappingService

                Public Shared ReadOnly Instance As SpanMapper = New SpanMapper()

                Public ReadOnly Property SupportsMappingImportDirectives As Boolean = False Implements ISpanMappingService.SupportsMappingImportDirectives

                Public Async Function MapSpansAsync(document As Document, spans As IEnumerable(Of TextSpan), cancellationToken As CancellationToken) As Task(Of ImmutableArray(Of MappedSpanResult)) Implements ISpanMappingService.MapSpansAsync
                    Dim testWorkspace = DirectCast(document.Project.Solution.Workspace, EditorTestWorkspace)
                    Dim testDocument = testWorkspace.GetTestDocument(document.Id)

                    Dim mappedTestDocument = testWorkspace.Documents.First(Function(d) d.Id <> testDocument.Id)
                    Dim mappedDocument = testWorkspace.CurrentSolution.GetDocument(mappedTestDocument.Id)
                    Dim mappedSource = Await mappedDocument.GetTextAsync(cancellationToken).ConfigureAwait(False)

                    Dim results = New List(Of MappedSpanResult)
                    For Each span In spans
                        If testDocument.AnnotatedSpans("Original").First() = span Then
                            Dim mappedSpan = mappedTestDocument.AnnotatedSpans("Definition").First()
                            Dim lineSpan = mappedSource.Lines.GetLinePositionSpan(mappedSpan)
                            results.Add(New MappedSpanResult(mappedDocument.FilePath, lineSpan, mappedSpan))
                        ElseIf testDocument.SelectedSpans.First() = span Then
                            Dim mappedSpan = mappedTestDocument.SelectedSpans.First()
                            Dim lineSpan = mappedSource.Lines.GetLinePositionSpan(mappedSpan)
                            results.Add(New MappedSpanResult(mappedDocument.FilePath, lineSpan, mappedSpan))
                        Else
                            Throw New Exception("shouldn't reach here")
                        End If
                    Next

                    Return results.ToImmutableArray()
                End Function

                Public Function GetMappedTextChangesAsync(oldDocument As Document, newDocument As Document, cancellationToken As CancellationToken) _
                    As Task(Of ImmutableArray(Of (mappedFilePath As String, mappedTextChange As Microsoft.CodeAnalysis.Text.TextChange))) _
                    Implements ISpanMappingService.GetMappedTextChangesAsync
                    Throw New NotImplementedException()
                End Function
            End Class

            Private Class Excerpter
                Implements IDocumentExcerptService

                Public Shared ReadOnly Instance As Excerpter = New Excerpter()

                Public Async Function TryExcerptAsync(document As Document, span As TextSpan, mode As ExcerptMode, classificationOptions As ClassificationOptions, cancellationToken As CancellationToken) As Task(Of ExcerptResult?) Implements IDocumentExcerptService.TryExcerptAsync
                    Dim testWorkspace = DirectCast(document.Project.Solution.Workspace, EditorTestWorkspace)
                    Dim testDocument = testWorkspace.GetTestDocument(document.Id)

                    Dim mappedTestDocument = testWorkspace.Documents.First(Function(d) d.Id <> testDocument.Id)
                    Dim mappedDocument = testWorkspace.CurrentSolution.GetDocument(mappedTestDocument.Id)
                    Dim mappedSource = Await mappedDocument.GetTextAsync(cancellationToken).ConfigureAwait(False)

                    Dim mappedSpan As TextSpan
                    If testDocument.AnnotatedSpans("Original").First() = span Then
                        mappedSpan = mappedTestDocument.AnnotatedSpans("Definition").First()
                    ElseIf testDocument.SelectedSpans.First() = span Then
                        mappedSpan = mappedTestDocument.SelectedSpans.First()
                    Else
                        Throw New Exception("shouldn't reach here")
                    End If

                    Dim line = mappedSource.Lines.GetLineFromPosition(mappedSpan.Start)
                    Return New ExcerptResult(mappedSource.GetSubText(line.Span), New TextSpan(mappedSpan.Start - line.Start, mappedSpan.Length), ImmutableArray.Create(New ClassifiedSpan(New TextSpan(0, line.Span.Length), ClassificationTypeNames.Text)), document, span)
                End Function
            End Class

            Private Class DocumentOperations
                Implements IDocumentOperationService

                Public Shared ReadOnly Instance As DocumentOperations = New DocumentOperations()

                Public ReadOnly Property CanApplyChange As Boolean Implements IDocumentOperationService.CanApplyChange
                    Get
                        Return False
                    End Get
                End Property

                Public ReadOnly Property SupportDiagnostics As Boolean Implements IDocumentOperationService.SupportDiagnostics
                    Get
                        Return False
                    End Get
                End Property
            End Class
        End Class

        <PartNotDiscoverable>
        <Export(GetType(SVsServiceProvider))>
        Private Class MockServiceProvider
            Implements SVsServiceProvider

            <ImportingConstructor>
            <Obsolete(MefConstruction.ImportingConstructorMessage, True)>
            Public Sub New()
            End Sub

            Public Function GetService(serviceType As Type) As Object Implements SVsServiceProvider.GetService
                If GetType(SVsFindAllReferences) = serviceType Then
                    Return TestFindAllReferencesService.Instance
                End If

                Return Nothing
            End Function
        End Class

        Private Class TestFindAllReferencesService
            Implements IFindAllReferencesService

            Public Shared ReadOnly Instance As TestFindAllReferencesService = New TestFindAllReferencesService()

            ' this mock is not thread-safe. don't use it concurrently
            Public LastWindow As FindAllReferencesWindow

            Public Function StartSearch(label As String) As IFindAllReferencesWindow Implements IFindAllReferencesService.StartSearch
                LastWindow = New FindAllReferencesWindow(label)

                Return LastWindow
            End Function
        End Class

        Private Class FindAllReferencesWindow
            Implements IFindAllReferencesWindow

            Public ReadOnly Label As String

            Public ReadOnly MyTableControl As WpfTableControl = New WpfTableControl()
            Public ReadOnly MyTableManager As TableManager = New TableManager()

            Public Sub New(label As String)
                Me.Label = label
            End Sub

            Public ReadOnly Property TableControl As IWpfTableControl Implements IFindAllReferencesWindow.TableControl
                Get
                    Return MyTableControl
                End Get
            End Property

            Public ReadOnly Property Manager As ITableManager Implements IFindAllReferencesWindow.Manager
                Get
                    Return MyTableManager
                End Get
            End Property

            Public Property Title As String Implements IFindAllReferencesWindow.Title
            Public Event Closed As EventHandler Implements IFindAllReferencesWindow.Closed

#Region "Not Implemented"
            Public Sub AddCommandTarget(target As IOleCommandTarget, ByRef [next] As IOleCommandTarget) Implements IFindAllReferencesWindow.AddCommandTarget
                Throw New NotImplementedException()
            End Sub
#End Region

            Public Sub SetProgress(progress As Double) Implements IFindAllReferencesWindow.SetProgress
            End Sub

            Public Sub SetProgress(completed As Integer, maximum As Integer) Implements IFindAllReferencesWindow.SetProgress
            End Sub
        End Class

        Private Class TableDataSink
            Implements ITableDataSink

            ' not thread safe. it should never be used concurrently
            Public LastFactory As ITableEntriesSnapshotFactory

            Public Property IsStable As Boolean Implements ITableDataSink.IsStable

            Public Sub AddFactory(newFactory As ITableEntriesSnapshotFactory, Optional removeAllFactories As Boolean = False) Implements ITableDataSink.AddFactory
                LastFactory = newFactory
            End Sub

            Public Sub FactorySnapshotChanged(factory As ITableEntriesSnapshotFactory) Implements ITableDataSink.FactorySnapshotChanged
                LastFactory = factory
            End Sub

#Region "Not Implemented"
            Public Sub AddEntries(newEntries As IReadOnlyList(Of ITableEntry), Optional removeAllEntries As Boolean = False) Implements ITableDataSink.AddEntries
                Throw New NotImplementedException()
            End Sub

            Public Sub RemoveEntries(oldEntries As IReadOnlyList(Of ITableEntry)) Implements ITableDataSink.RemoveEntries
                Throw New NotImplementedException()
            End Sub

            Public Sub ReplaceEntries(oldEntries As IReadOnlyList(Of ITableEntry), newEntries As IReadOnlyList(Of ITableEntry)) Implements ITableDataSink.ReplaceEntries
                Throw New NotImplementedException()
            End Sub

            Public Sub RemoveAllEntries() Implements ITableDataSink.RemoveAllEntries
                Throw New NotImplementedException()
            End Sub

            Public Sub AddSnapshot(newSnapshot As ITableEntriesSnapshot, Optional removeAllSnapshots As Boolean = False) Implements ITableDataSink.AddSnapshot
                Throw New NotImplementedException()
            End Sub

            Public Sub RemoveSnapshot(oldSnapshot As ITableEntriesSnapshot) Implements ITableDataSink.RemoveSnapshot
                Throw New NotImplementedException()
            End Sub

            Public Sub RemoveAllSnapshots() Implements ITableDataSink.RemoveAllSnapshots
                Throw New NotImplementedException()
            End Sub

            Public Sub ReplaceSnapshot(oldSnapshot As ITableEntriesSnapshot, newSnapshot As ITableEntriesSnapshot) Implements ITableDataSink.ReplaceSnapshot
                Throw New NotImplementedException()
            End Sub

            Public Sub RemoveFactory(oldFactory As ITableEntriesSnapshotFactory) Implements ITableDataSink.RemoveFactory
                Throw New NotImplementedException()
            End Sub

            Public Sub ReplaceFactory(oldFactory As ITableEntriesSnapshotFactory, newFactory As ITableEntriesSnapshotFactory) Implements ITableDataSink.ReplaceFactory
                Throw New NotImplementedException()
            End Sub

            Public Sub RemoveAllFactories() Implements ITableDataSink.RemoveAllFactories
                Throw New NotImplementedException()
            End Sub
#End Region
        End Class

        Private Class TableManager
            Implements ITableManager

            Private ReadOnly _sources As List(Of ITableDataSource) = New List(Of ITableDataSource)()

            Public LastSink As TableDataSink

            Public ReadOnly Property Identifier As String Implements ITableManager.Identifier
                Get
                    Return "Test"
                End Get
            End Property

            Public ReadOnly Property Sources As IReadOnlyList(Of ITableDataSource) Implements ITableManager.Sources
                Get
                    Return _sources
                End Get
            End Property

            Public Event SourcesChanged As EventHandler Implements ITableManager.SourcesChanged

            Public Function AddSource(source As ITableDataSource, ParamArray columns() As String) As Boolean Implements ITableManager.AddSource
                Return AddSource(source, columns.ToImmutableArray())
            End Function

            Public Function AddSource(source As ITableDataSource, columns As IReadOnlyCollection(Of String)) As Boolean Implements ITableManager.AddSource
                LastSink = New TableDataSink()

                source.Subscribe(LastSink)
                _sources.Add(source)

                Return True
            End Function

            Public Function RemoveSource(source As ITableDataSource) As Boolean Implements ITableManager.RemoveSource
                Return _sources.Remove(source)
            End Function

#Region "Not Implemented"
            Public Function GetColumnsForSources(sources As IEnumerable(Of ITableDataSource)) As IReadOnlyList(Of String) Implements ITableManager.GetColumnsForSources
                Throw New NotImplementedException()
            End Function
#End Region
        End Class

        Private Class WpfTableControl
            Implements IWpfTableControl2

            Public Event GroupingsChanged As EventHandler Implements IWpfTableControl2.GroupingsChanged

            Private _states As List(Of ColumnState) = New List(Of ColumnState)({New ColumnState2(StandardTableColumnDefinitions2.Definition, isVisible:=True, width:=10)})

            Public ReadOnly Property ColumnStates As IReadOnlyList(Of ColumnState) Implements IWpfTableControl.ColumnStates
                Get
                    Return _states
                End Get
            End Property

            Public Sub SetColumnStates(states As IEnumerable(Of ColumnState)) Implements IWpfTableControl2.SetColumnStates
                _states = states.ToList()
            End Sub

            Public ReadOnly Property ColumnDefinitionManager As ITableColumnDefinitionManager Implements IWpfTableControl.ColumnDefinitionManager
                Get
                    Return Nothing
                End Get
            End Property

#Region "Not Implemented"
            Public ReadOnly Property IsDataStable As Boolean Implements IWpfTableControl2.IsDataStable
                Get
                    Throw New NotImplementedException()
                End Get
            End Property

            Public Property NavigationBehavior As TableEntryNavigationBehavior Implements IWpfTableControl2.NavigationBehavior
                Get
                    Throw New NotImplementedException()
                End Get
                Set(value As TableEntryNavigationBehavior)
                    Throw New NotImplementedException()
                End Set
            End Property

            Public Property KeepSelectionInView As Boolean Implements IWpfTableControl2.KeepSelectionInView
                Get
                    Throw New NotImplementedException()
                End Get
                Set(value As Boolean)
                    Throw New NotImplementedException()
                End Set
            End Property

            Public Property ShowGroupingLine As Boolean Implements IWpfTableControl2.ShowGroupingLine
                Get
                    Throw New NotImplementedException()
                End Get
                Set(value As Boolean)
                    Throw New NotImplementedException()
                End Set
            End Property

            Public Property RaiseDataUnstableChangeDelay As TimeSpan Implements IWpfTableControl2.RaiseDataUnstableChangeDelay
                Get
                    Throw New NotImplementedException()
                End Get
                Set(value As TimeSpan)
                    Throw New NotImplementedException()
                End Set
            End Property

            Public Property SelectedItemActiveBackground As Brush Implements IWpfTableControl2.SelectedItemActiveBackground
                Get
                    Throw New NotImplementedException()
                End Get
                Set(value As Brush)
                    Throw New NotImplementedException()
                End Set
            End Property

            Public Property SelectedItemActiveForeground As Brush Implements IWpfTableControl2.SelectedItemActiveForeground
                Get
                    Throw New NotImplementedException()
                End Get
                Set(value As Brush)
                    Throw New NotImplementedException()
                End Set
            End Property

            Public Property SelectedItemInactiveBackground As Brush Implements IWpfTableControl2.SelectedItemInactiveBackground
                Get
                    Throw New NotImplementedException()
                End Get
                Set(value As Brush)
                    Throw New NotImplementedException()
                End Set
            End Property

            Public Property SelectedItemInactiveForeground As Brush Implements IWpfTableControl2.SelectedItemInactiveForeground
                Get
                    Throw New NotImplementedException()
                End Get
                Set(value As Brush)
                    Throw New NotImplementedException()
                End Set
            End Property

            Public ReadOnly Property Manager As ITableManager Implements IWpfTableControl.Manager
                Get
                    Throw New NotImplementedException()
                End Get
            End Property

            Public ReadOnly Property Control As FrameworkElement Implements IWpfTableControl.Control
                Get
                    Throw New NotImplementedException()
                End Get
            End Property

            Public ReadOnly Property AutoSubscribe As Boolean Implements IWpfTableControl.AutoSubscribe
                Get
                    Throw New NotImplementedException()
                End Get
            End Property

            Public Property SortFunction As Comparison(Of ITableEntryHandle) Implements IWpfTableControl.SortFunction
                Get
                    Throw New NotImplementedException()
                End Get
                Set(value As Comparison(Of ITableEntryHandle))
                    Throw New NotImplementedException()
                End Set
            End Property

            Public Property SelectionMode As SelectionMode Implements IWpfTableControl.SelectionMode
                Get
                    Throw New NotImplementedException()
                End Get
                Set(value As SelectionMode)
                    Throw New NotImplementedException()
                End Set
            End Property

            Public ReadOnly Property Entries As IEnumerable(Of ITableEntryHandle) Implements IWpfTableControl.Entries
                Get
                    Throw New NotImplementedException()
                End Get
            End Property

            Public Property SelectedEntries As IEnumerable(Of ITableEntryHandle) Implements IWpfTableControl.SelectedEntries
                Get
                    Throw New NotImplementedException()
                End Get
                Set(value As IEnumerable(Of ITableEntryHandle))
                    Throw New NotImplementedException()
                End Set
            End Property

            Public ReadOnly Property SelectedEntry As ITableEntryHandle Implements IWpfTableControl.SelectedEntry
                Get
                    Throw New NotImplementedException()
                End Get
            End Property

            Public ReadOnly Property SelectedOrFirstEntry As ITableEntryHandle Implements IWpfTableControl.SelectedOrFirstEntry
                Get
                    Throw New NotImplementedException()
                End Get
            End Property

            Public Event DataStabilityChanged As EventHandler Implements IWpfTableControl2.DataStabilityChanged
            Public Event FiltersChanged As EventHandler(Of FiltersChangedEventArgs) Implements IWpfTableControl.FiltersChanged
            Public Event PreEntriesChanged As EventHandler Implements IWpfTableControl.PreEntriesChanged
            Public Event EntriesChanged As EventHandler(Of EntriesChangedEventArgs) Implements IWpfTableControl.EntriesChanged

            Public Sub SubscribeToDataSource(source As ITableDataSource) Implements IWpfTableControl.SubscribeToDataSource
                Throw New NotImplementedException()
            End Sub

            Public Sub SelectAll() Implements IWpfTableControl.SelectAll
                Throw New NotImplementedException()
            End Sub

            Public Sub UnselectAll() Implements IWpfTableControl.UnselectAll
                Throw New NotImplementedException()
            End Sub

            Public Sub RefreshUI() Implements IWpfTableControl.RefreshUI
                Throw New NotImplementedException()
            End Sub

            Public Function GetAllFilters() As IEnumerable(Of Tuple(Of String, IEntryFilter)) Implements IWpfTableControl2.GetAllFilters
                Throw New NotImplementedException()
            End Function

            Public Function UnsubscribeFromDataSource(source As ITableDataSource) As Boolean Implements IWpfTableControl.UnsubscribeFromDataSource
                Throw New NotImplementedException()
            End Function

            Public Function SetFilter(key As String, newFilter As IEntryFilter) As IEntryFilter Implements IWpfTableControl.SetFilter
                Throw New NotImplementedException()
            End Function

            Public Function GetFilter(key As String) As IEntryFilter Implements IWpfTableControl.GetFilter
                Throw New NotImplementedException()
            End Function

            Public Function ForceUpdateAsync() As Task(Of EntriesChangedEventArgs) Implements IWpfTableControl.ForceUpdateAsync
                Throw New NotImplementedException()
            End Function

            Public Sub Dispose() Implements IDisposable.Dispose
            End Sub
#End Region
        End Class
    End Class
End Namespace
