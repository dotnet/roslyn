' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Threading
Imports Microsoft.CodeAnalysis
Imports Microsoft.CodeAnalysis.Shared.Extensions
Imports Microsoft.CodeAnalysis.Test.Utilities
Imports Microsoft.VisualStudio.LanguageServices.Implementation.SolutionExplorer
Imports Roslyn.Test.Utilities

Namespace Microsoft.VisualStudio.LanguageServices.UnitTests.SolutionExplorer
    Public MustInherit Class AbstractSolutionExplorerSymbolTreeItemProviderTests

        Protected MustOverride Function CreateWorkspace(code As String) As TestWorkspace

        Protected Async Function TestNode(Of TNode As SyntaxNode)(
                code As String,
                expected As String) As Task

            Using workspace = CreateWorkspace(code)
                Dim testDocument = workspace.Documents.Single()
                Dim document = workspace.CurrentSolution.Projects.Single().Documents.Single()
                Dim root = Await document.GetRequiredSyntaxRootAsync(CancellationToken.None)

                Dim service = document.GetRequiredLanguageService(Of ISolutionExplorerSymbolTreeItemProvider)()

                Dim node = root.DescendantNodesAndSelf().OfType(Of TNode)().First()
                Dim items = service.GetItems(node, CancellationToken.None)

                Dim actual = String.Join(vbCrLf, items)
                AssertEx.Equal(expected, actual)

                AssertEx.SequenceEqual(
                    testDocument.SelectedSpans,
                    items.Select(Function(i) i.ItemSyntax.NavigationToken.Span))
            End Using
        End Function
    End Class
End Namespace
