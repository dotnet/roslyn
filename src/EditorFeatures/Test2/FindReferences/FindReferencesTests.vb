' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Collections.Immutable
Imports System.Threading
Imports System.Threading.Tasks
Imports Microsoft.CodeAnalysis
Imports Microsoft.CodeAnalysis.Editor.UnitTests.Workspaces
Imports Microsoft.CodeAnalysis.FindSymbols
Imports Roslyn.Utilities
Imports Xunit.Abstractions

Namespace Microsoft.CodeAnalysis.Editor.UnitTests.FindReferences
    Partial Public Class FindReferencesTests
        Private Const DefinitionKey As String = "Definition"

        Private ReadOnly _outputHelper As ITestOutputHelper

        Public Sub New(outputHelper As ITestOutputHelper)
            _outputHelper = outputHelper
        End Sub

        Private Async Function TestAsync(definition As XElement, Optional searchSingleFileOnly As Boolean = False, Optional uiVisibleOnly As Boolean = False) As Task

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
                               SelectMany(Function(r) r.Definition.Locations).
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
