' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Collections.Immutable
Imports System.Threading.Tasks
Imports Microsoft.CodeAnalysis
Imports Microsoft.CodeAnalysis.Editor.UnitTests.Workspaces
Imports Microsoft.CodeAnalysis.FindReferences
Imports Microsoft.CodeAnalysis.FindSymbols
Imports Microsoft.CodeAnalysis.Text
Imports Roslyn.Utilities
Imports Xunit.Abstractions

Namespace Microsoft.CodeAnalysis.Editor.UnitTests.FindReferences
    Partial Public Class FindReferencesTests
        Private Const DefinitionKey As String = "Definition"

        Private ReadOnly _outputHelper As ITestOutputHelper

        Public Sub New(outputHelper As ITestOutputHelper)
            _outputHelper = outputHelper
        End Sub

        Private Async Function TestAPIAndFeature(definition As XElement, Optional searchSingleFileOnly As Boolean = False, Optional uiVisibleOnly As Boolean = False) As Task
            Await TestAPI(definition, searchSingleFileOnly, uiVisibleOnly)
            Await TestStreamingFeature(definition, searchSingleFileOnly, uiVisibleOnly)
        End Function

        Private Async Function TestStreamingFeature(element As XElement, searchSingleFileOnly As Boolean, uiVisibleOnly As Boolean) As Task
            ' We don't support testing features htat only expect partial results.
            If searchSingleFileOnly OrElse uiVisibleOnly Then
                Return
            End If

            Using workspace = Await TestWorkspace.CreateAsync(element)
                For Each cursorDocument In workspace.Documents.Where(Function(d) d.CursorPosition.HasValue)
                    Dim cursorPosition = cursorDocument.CursorPosition.Value

                    Dim startDocument = workspace.CurrentSolution.GetDocument(cursorDocument.Id)
                    Assert.NotNull(startDocument)

                    Dim findRefsService = startDocument.GetLanguageService(Of IStreamingFindReferencesService)
                    Dim context = New TestContext()
                    Await findRefsService.FindReferencesAsync(startDocument, cursorPosition, context)

                    Dim expectedDefinitions =
                        workspace.Documents.Where(Function(d) d.AnnotatedSpans.ContainsKey(DefinitionKey) AndAlso d.AnnotatedSpans(DefinitionKey).Any()).
                                            OrderBy(Function(d) d.Name).
                                            Select(Function(d) New FileNameAndSpans(
                                                   d.Name, d.AnnotatedSpans(DefinitionKey).ToList())).ToList()

                    Dim actualDefinitions = GetFileNamesAndSpans(
                        context.Definitions.Where(AddressOf context.ShouldShow).
                                            SelectMany(Function(d) d.SourceSpans))

                    Assert.Equal(expectedDefinitions, actualDefinitions)

                    Dim expectedReferences =
                        workspace.Documents.Where(Function(d) d.SelectedSpans.Any()).
                                            OrderBy(Function(d) d.Name).
                                            Select(Function(d) New FileNameAndSpans(
                                                   d.Name, d.SelectedSpans.ToList())).ToList()

                    Dim actualReferences = GetFileNamesAndSpans(
                        context.References.Select(Function(r) r.SourceSpan))

                    Assert.Equal(expectedReferences, actualReferences)
                Next
            End Using
        End Function

        Private Function GetFileNamesAndSpans(items As IEnumerable(Of DocumentSpan)) As List(Of FileNameAndSpans)
            Return items.Where(Function(i) i.Document IsNot Nothing).
                         GroupBy(Function(i) i.Document).
                         OrderBy(Function(g) g.Key.Name).
                         Select(Function(g) GetFileNameAndSpans(g)).ToList()
        End Function

        Private Function GetFileNameAndSpans(g As IGrouping(Of Document, DocumentSpan)) As FileNameAndSpans
            Return New FileNameAndSpans(
                g.Key.Name,
                g.Select(Function(i) i.SourceSpan).OrderBy(Function(s) s.Start).
                                                   Distinct().ToList())
        End Function

        Private Structure FileNameAndSpans
            Public ReadOnly FileName As String
            Public ReadOnly Spans As List(Of TextSpan)

            Public Sub New(fileName As String, spans As List(Of TextSpan))
                Me.FileName = fileName
                Me.Spans = spans
            End Sub

            Public Overrides Function Equals(obj As Object) As Boolean
                Return Equals(DirectCast(obj, FileNameAndSpans))
            End Function

            Public Overloads Function Equals(f As FileNameAndSpans) As Boolean
                Assert.Equal(Me.FileName, f.FileName)
                Assert.Equal(Me.Spans.Count, f.Spans.Count)

                For i = 0 To Me.Spans.Count - 1
                    Assert.Equal(Me.Spans(i), f.Spans(i))
                Next

                Return True
            End Function

        End Structure

        Private Class TestContext
            Inherits FindReferencesContext

            Private ReadOnly gate As Object = New Object()

            Public ReadOnly Definitions As List(Of DefinitionItem) = New List(Of DefinitionItem)()
            Public ReadOnly References As List(Of SourceReferenceItem) = New List(Of SourceReferenceItem)()

            Public Function ShouldShow(definition As DefinitionItem) As Boolean
                If References.Any(Function(r) r.Definition Is definition) Then
                    Return True
                End If

                Return definition.DisplayIfNoReferences
            End Function

            Public Overrides Function OnDefinitionFoundAsync(definition As DefinitionItem) As Task
                SyncLock gate
                    Me.Definitions.Add(definition)
                End SyncLock

                Return SpecializedTasks.EmptyTask
            End Function

            Public Overrides Function OnReferenceFoundAsync(reference As SourceReferenceItem) As Task
                SyncLock gate
                    References.Add(reference)
                End SyncLock

                Return SpecializedTasks.EmptyTask
            End Function
        End Class

        Private Async Function TestAPI(definition As XElement, Optional searchSingleFileOnly As Boolean = False, Optional uiVisibleOnly As Boolean = False) As Task
            Using workspace = Await TestWorkspace.CreateAsync(definition)
                workspace.SetTestLogger(AddressOf _outputHelper.WriteLine)

                For Each cursorDocument In workspace.Documents.Where(Function(d) d.CursorPosition.HasValue)
                    Dim cursorPosition = cursorDocument.CursorPosition.Value

                    Dim document = workspace.CurrentSolution.GetDocument(cursorDocument.Id)
                    Assert.NotNull(document)

                    Dim symbol = Await SymbolFinder.FindSymbolAtPositionAsync(document, cursorPosition)
                    Dim result = SpecializedCollections.EmptyEnumerable(Of ReferencedSymbol)()
                    If symbol IsNot Nothing Then

                        Dim scope = If(searchSingleFileOnly, ImmutableHashSet.Create(Of Document)(document), Nothing)

                        result = result.Concat(Await SymbolFinder.FindReferencesAsync(symbol, document.Project.Solution, progress:=Nothing, documents:=scope))
                    End If

                    Dim actualDefinitions =
                        result.FilterToItemsToShow().
                               Where(Function(s) Not IsImplicitNamespace(s)).
                               SelectMany(Function(r) r.Definition.GetDefinitionLocationsToShow()).
                               Where(Function(loc) IsInSource(workspace, loc, uiVisibleOnly)).
                               GroupBy(Function(loc) loc.SourceTree).
                               ToDictionary(
                                    Function(g) GetFilePathAndProjectLabel(document.Project.Solution, g.Key),
                                    Function(g) g.Select(Function(loc) loc.SourceSpan).Distinct().ToList())

                    Dim documentsWithAnnotatedSpans = workspace.Documents.Where(Function(d) d.AnnotatedSpans.Any())
                    Assert.Equal(Of String)(documentsWithAnnotatedSpans.Select(Function(d) GetFilePathAndProjectLabel(workspace, d)).Order(), actualDefinitions.Keys.Order())
                    For Each doc In documentsWithAnnotatedSpans
                        Assert.Equal(Of Text.TextSpan)(doc.AnnotatedSpans(DefinitionKey).Order(), actualDefinitions(GetFilePathAndProjectLabel(workspace, doc)).Order())
                    Next

                    Dim actualReferences =
                        result.FilterToItemsToShow().
                               SelectMany(Function(r) r.Locations.Select(Function(loc) loc.Location)).
                               Where(Function(loc) IsInSource(workspace, loc, uiVisibleOnly)).
                               Distinct().
                               GroupBy(Function(loc) loc.SourceTree).
                               ToDictionary(
                                   Function(g) GetFilePathAndProjectLabel(document.Project.Solution, g.Key),
                                   Function(g) g.Select(Function(loc) loc.SourceSpan).Distinct().ToList())

                    Dim expectedDocuments = workspace.Documents.Where(Function(d) d.SelectedSpans.Any())
                    Assert.Equal(expectedDocuments.Select(Function(d) GetFilePathAndProjectLabel(workspace, d)).Order(), actualReferences.Keys.Order())

                    For Each doc In expectedDocuments
                        Dim expectedSpans = doc.SelectedSpans.Order()
                        Dim actualSpans = actualReferences(GetFilePathAndProjectLabel(workspace, doc)).Order()

                        AssertEx.Equal(expectedSpans, actualSpans)
                    Next
                Next
            End Using
        End Function

        Private Function IsImplicitNamespace(referencedSymbol As ReferencedSymbol) As Boolean
            Return referencedSymbol.Definition.IsImplicitlyDeclared AndAlso
                   referencedSymbol.Definition.Kind = SymbolKind.Namespace
        End Function

        Private Shared Function IsInSource(workspace As Workspace, loc As Location, uiVisibleOnly As Boolean) As Boolean
            If uiVisibleOnly Then
                Return loc.IsInSource AndAlso Not loc.SourceTree.IsHiddenPosition(loc.SourceSpan.Start)
            Else
                Return loc.IsInSource
            End If
        End Function

        Private Shared Function GetFilePathAndProjectLabel(solution As Solution, syntaxTree As SyntaxTree) As String
            Dim document = solution.GetDocument(syntaxTree)
            Return GetFilePathAndProjectLabel(document)
        End Function

        Private Shared Function GetFilePathAndProjectLabel(document As Document) As String
            Return $"{document.Project.Name}: {document.FilePath}"
        End Function

        Private Shared Function GetFilePathAndProjectLabel(workspace As TestWorkspace, hostDocument As TestHostDocument) As String
            Dim document = workspace.CurrentSolution.GetDocument(hostDocument.Id)
            Return GetFilePathAndProjectLabel(document)
        End Function
    End Class
End Namespace
