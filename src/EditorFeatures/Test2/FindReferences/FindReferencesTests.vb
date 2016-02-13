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

            Using workspace = Await TestWorkspace.CreateAsync(definition)
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
                                    Function(g) GetFilePathAndProjectLabel(document.Project.Solution, g.Key),
                                    Function(g) g.Select(Function(loc) loc.SourceSpan).Distinct().ToList())

                    Dim documentsWithAnnotatedSpans = workspace.Documents.Where(Function(d) d.AnnotatedSpans.Any())
                    Assert.Equal(Of String)(documentsWithAnnotatedSpans.Select(Function(d) GetFilePathAndProjectLabel(workspace, d)).Order(), actualDefinitions.Keys.Order())
                    For Each doc In documentsWithAnnotatedSpans
                        Assert.Equal(Of Text.TextSpan)(doc.AnnotatedSpans("Definition").Order(), actualDefinitions(GetFilePathAndProjectLabel(workspace, doc)).Order())
                    Next

                    Dim actualReferences =
                        result.FilterUnreferencedSyntheticDefinitions().
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
