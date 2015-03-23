' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Text
Imports System.Threading
Imports Microsoft.CodeAnalysis
Imports Microsoft.CodeAnalysis.Editor.UnitTests
Imports Microsoft.CodeAnalysis.Editor.UnitTests.Extensions
Imports Microsoft.CodeAnalysis.Editor.UnitTests.Utilities
Imports Microsoft.CodeAnalysis.Editor.UnitTests.Workspaces
Imports Microsoft.CodeAnalysis.FindSymbols
Imports Microsoft.VisualStudio.Composition
Imports Microsoft.VisualStudio.LanguageServices.Implementation.Library.FindResults
Imports Microsoft.VisualStudio.LanguageServices.UnitTests.ObjectBrowser.Mocks
Imports Roslyn.Test.Utilities
Imports Roslyn.Utilities

Namespace Microsoft.VisualStudio.LanguageServices.UnitTests.FindResults
    Public Class FindResultsTests
        <Fact, Trait(Traits.Feature, Traits.Features.FindReferences)>
        <WorkItem(1138943)>
        Public Sub ConstructorReferencesShouldNotAppearUnderClassNodeInCSharp()
            Dim markup = <Text><![CDATA[
class $$C
{
    const int z = 1;

    public C() { }
    public C(int x) { }

    void T()
    {
        var a = new C();
        var b = new C(5);
        var c = C.z;
    }
}"]]></Text>

            Dim expectedResults = New List(Of AbstractTreeItem) From
                {
                    New TestFindResult($"[CSharpAssembly1] C.C() ({ServicesVSResources.ReferenceCountSingular})",
                        New TestFindResult("CSharpAssembly1\Test1.cs - (11, 21) : var a = new C();")),
                    New TestFindResult($"[CSharpAssembly1] C.C(int) ({ServicesVSResources.ReferenceCountSingular})",
                        New TestFindResult("CSharpAssembly1\Test1.cs - (12, 21) : var b = new C(5);")),
                    New TestFindResult($"[CSharpAssembly1] class C ({ServicesVSResources.ReferenceCountSingular})",
                        New TestFindResult("CSharpAssembly1\Test1.cs - (13, 17) : var c = C.z;"))
                }

            Verify(markup, LanguageNames.CSharp, expectedResults)
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.FindReferences)>
        <WorkItem(1138943)>
        Public Sub ConstructorReferencesShouldNotAppearUnderClassNodeInVisualBasic()
            Dim markup = <Text><![CDATA[
Class C$$
    Const z = 1

    Public Sub New()
    End Sub

    Public Sub New(x As Integer)
    End Sub

    Sub T()
        Dim a = New C()
        Dim b = New C(5)
        Dim d = C.z
    End Sub
End Class"]]></Text>

            Dim expectedResults = New List(Of AbstractTreeItem) From
                {
                    New TestFindResult($"[VisualBasicAssembly1] Class C ({ServicesVSResources.ReferenceCountSingular})",
                        New TestFindResult("VisualBasicAssembly1\Test1.vb - (14, 17) : Dim d = C.z")),
                    New TestFindResult($"[VisualBasicAssembly1] Sub C.New() ({ServicesVSResources.ReferenceCountSingular})",
                        New TestFindResult("VisualBasicAssembly1\Test1.vb - (12, 21) : Dim a = New C()")),
                    New TestFindResult($"[VisualBasicAssembly1] Sub C.New(Integer) ({ServicesVSResources.ReferenceCountSingular})",
                        New TestFindResult("VisualBasicAssembly1\Test1.vb - (13, 21) : Dim b = New C(5)"))
                }

            Verify(markup, LanguageNames.VisualBasic, expectedResults)
        End Sub

        Private Shared ReadOnly s_exportProvider As ExportProvider = MinimalTestExportProvider.CreateExportProvider(
            TestExportProvider.MinimumCatalogWithCSharpAndVisualBasic.WithParts(
                GetType(MockDocumentNavigationServiceProvider),
                GetType(MockSymbolNavigationServiceProvider)))

        Private Sub Verify(markup As XElement, languageName As String, expectedResults As IList(Of AbstractTreeItem))
            Dim workspaceXml =
                <Workspace>
                    <Project Language=<%= languageName %> CommonReferences="true">
                        <Document><%= markup %></Document>
                    </Project>
                </Workspace>

            Using workspace = TestWorkspaceFactory.CreateWorkspace(workspaceXml, exportProvider:=s_exportProvider)
                Dim doc = workspace.Documents.Single()
                Dim workspaceDoc = workspace.CurrentSolution.GetDocument(doc.Id)
                If Not doc.CursorPosition.HasValue Then
                    Assert.True(False, "Missing caret location in document.")
                End If

                Dim symbol = SymbolFinder.FindSymbolAtPositionAsync(workspaceDoc, doc.CursorPosition.Value, CancellationToken.None).Result
                Assert.NotNull(symbol)

                Dim result = SymbolFinder.FindReferencesAsync(symbol, workspace.CurrentSolution, CancellationToken.None).Result

                Dim libraryManager = New LibraryManager(New MockServiceProvider(New MockComponentModel(workspace.ExportProvider)))
                Dim findReferencesTree = libraryManager.CreateFindReferencesItems(workspace.CurrentSolution, result)

                ' We cannot control the ordering of top-level nodes in the Find Symbol References window, so do not consider ordering of these items here.
                expectedResults = expectedResults.OrderBy(Function(n) n.DisplayText).ToList()
                findReferencesTree = findReferencesTree.OrderBy(Function(n) n.DisplayText).ToList()

                VerifyResultsTree(expectedResults, findReferencesTree)
            End Using
        End Sub

        Private Sub VerifyResultsTree(expectedResults As IList(Of AbstractTreeItem), findReferencesTree As IList(Of AbstractTreeItem))
            Assert.True(expectedResults.Count = findReferencesTree.Count, $"Unexpected number of results. Expected: {expectedResults.Count} Actual: {findReferencesTree.Count}
Expected Items:
{GetResultText(expectedResults)}
Actual Items:
{GetResultText(findReferencesTree)}
")

            For index = 0 To expectedResults.Count - 1
                Dim expectedItem = expectedResults(index)
                Dim actualItem = findReferencesTree(index)

                Assert.Equal(expectedItem.DisplayText, actualItem.DisplayText)

                Dim expectedHasChildren = expectedItem.Children IsNot Nothing AndAlso expectedItem.Children.Count > 0
                Dim actualHasChildren = actualItem.Children IsNot Nothing AndAlso actualItem.Children.Count > 0

                Assert.Equal(expectedHasChildren, actualHasChildren)

                If expectedHasChildren Then
                    VerifyResultsTree(expectedItem.Children, actualItem.Children)
                End If
            Next
        End Sub

        Private Function GetResultText(references As IList(Of AbstractTreeItem)) As String
            Dim indentString = String.Empty
            Dim stringBuilder = New StringBuilder()

            GetResultTextWorker(references, stringBuilder, indentString)

            Return stringBuilder.ToString()
        End Function

        Private Sub GetResultTextWorker(references As IList(Of AbstractTreeItem), stringBuilder As StringBuilder, indentString As String)
            For Each reference In references
                stringBuilder.Append(indentString)
                stringBuilder.AppendLine(reference.DisplayText)

                If reference.Children IsNot Nothing AndAlso reference.Children.Any() Then
                    GetResultTextWorker(reference.Children, stringBuilder, indentString + "  ")
                End If
            Next
        End Sub

        Private Class TestFindResult
            Inherits AbstractTreeItem

            Public Sub New(displayText As String, ParamArray children As TestFindResult())
                MyBase.New(0)
                Me.DisplayText = displayText
                Me.Children = If(children.Length > 0, children, Nothing)
            End Sub

            Public Overrides Function GoToSource() As Integer
                Throw New NotImplementedException()
            End Function
        End Class
    End Class
End Namespace
