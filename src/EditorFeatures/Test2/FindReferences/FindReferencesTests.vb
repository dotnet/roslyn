' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Collections.Immutable
Imports System.Threading
Imports System.Threading.Tasks
Imports Microsoft.CodeAnalysis
Imports Microsoft.CodeAnalysis.Editor.UnitTests.Workspaces
Imports Microsoft.CodeAnalysis.FindSymbols
Imports Roslyn.Utilities

Namespace Microsoft.CodeAnalysis.Editor.UnitTests.FindReferences
    Partial Public Class FindReferencesTests
        Private Async Function TestAsync(definition As XElement, Optional searchSingleFileOnly As Boolean = False, Optional uiVisibleOnly As Boolean = False) As Task

            Using workspace = Await TestWorkspaceFactory.CreateWorkspaceAsync(definition)
                Dim cursorDocument = workspace.Documents.First(Function(d) d.CursorPosition.HasValue)
                Dim cursorPosition = cursorDocument.CursorPosition.Value

                Dim document = workspace.CurrentSolution.GetDocument(cursorDocument.Id)
                Assert.NotNull(document)

                Dim symbol = SymbolFinder.FindSymbolAtPositionAsync(document, cursorPosition).Result
                Dim result = SpecializedCollections.EmptyEnumerable(Of ReferencedSymbol)()
                If symbol IsNot Nothing Then

                    Dim scope = If(searchSingleFileOnly, ImmutableHashSet.Create(Of Document)(document), Nothing)

                    result = result.Concat(SymbolFinder.FindReferencesAsync(symbol, document.Project.Solution, progress:=Nothing, documents:=scope).Result())
                End If

                Dim actualDefinitions =
                    result.FilterUnreferencedSyntheticDefinitions().
                           Where(Function(r)
                                     Return Not r.Definition.IsImplicitlyDeclared OrElse
                                        (r.Definition.Kind = SymbolKind.Property AndAlso
                                        r.Definition.ContainingSymbol IsNot Nothing AndAlso
                                        r.Definition.ContainingSymbol.Kind = SymbolKind.NamedType AndAlso
                                        DirectCast(r.Definition.ContainingSymbol, INamedTypeSymbol).IsAnonymousType)
                                 End Function).
                           SelectMany(Function(r) r.Definition.Locations).
                           Where(Function(loc) IsInSource(workspace, loc, uiVisibleOnly)).
                           GroupBy(Function(loc) loc.SourceTree).
                           ToDictionary(
                                Function(g) GetFilePath(workspace, document.Project.Solution, g.Key),
                                Function(g) g.Select(Function(loc) loc.SourceSpan).Distinct().ToList())

                Dim documentsWithAnnotatedSpans = workspace.Documents.Where(Function(d) d.AnnotatedSpans.Any())
                AssertEx.Equal(documentsWithAnnotatedSpans.Select(Function(d) d.FilePath).Order(), actualDefinitions.Keys.Order())
                For Each doc In documentsWithAnnotatedSpans
                    AssertEx.Equal(doc.AnnotatedSpans("Definition").Order(), actualDefinitions(doc.FilePath).Order())
                Next

                Dim actualReferences =
                    result.FilterUnreferencedSyntheticDefinitions().
                           SelectMany(Function(r) r.Locations.Select(Function(loc) loc.Location)).
                           Where(Function(loc) IsInSource(workspace, loc, uiVisibleOnly)).
                           Distinct().
                           GroupBy(Function(loc) loc.SourceTree).
                           ToDictionary(
                               Function(g) GetFilePath(workspace, document.Project.Solution, g.Key),
                               Function(g) g.Select(Function(loc) loc.SourceSpan).Distinct().ToList())

                Dim expectedDocuments = workspace.Documents.Where(Function(d) d.SelectedSpans.Any())
                AssertEx.Equal(expectedDocuments.Select(Function(d) d.FilePath).Order(), actualReferences.Keys.Order())

                For Each doc In expectedDocuments
                    Dim expectedSpans = doc.SelectedSpans.Order()
                    Dim actualSpans = actualReferences(doc.FilePath).Order()

                    AssertEx.Equal(expectedSpans, actualSpans)
                Next
            End Using
        End Function

        Private Shared Function IsInSource(workspace As Workspace, loc As Location, uiVisibleOnly As Boolean) As Boolean
            If uiVisibleOnly Then
                Return loc.IsInSource AndAlso Not loc.SourceTree.IsHiddenPosition(loc.SourceSpan.Start)
            Else
                Return loc.IsInSource
            End If
        End Function

        Private Function GetFilePath(workspace As TestWorkspace, solution As Solution, syntaxTree As SyntaxTree) As String
            Dim document = solution.GetDocument(syntaxTree)
            Dim id = document.Id
            Return workspace.GetTestDocument(id).FilePath
        End Function

        Private Shared Function Timeout(Of T)(_task As Task(Of T), milliseconds As Integer) As Task(Of T)
            Dim source = New TaskCompletionSource(Of Task(Of T))()
            Dim tasks = {_task, Task.Delay(milliseconds)}
            Dim taskFunc As Action(Of Task) = Sub(completedTask)
                                                  If completedTask Is _task Then
                                                      source.SetResult(_task)
                                                  Else
                                                      source.SetCanceled()
                                                  End If
                                              End Sub

            Task.Factory.ContinueWhenAny(
                tasks, taskFunc, CancellationToken.None, TaskContinuationOptions.ExecuteSynchronously, TaskScheduler.Default)

            Return source.Task.Unwrap()
        End Function
    End Class
End Namespace
