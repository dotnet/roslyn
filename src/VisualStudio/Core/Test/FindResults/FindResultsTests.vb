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
        <WpfFact, Trait(Traits.Feature, Traits.Features.FindReferences)>
        <WorkItem(1138943)>
        Public Async Function ConstructorReferencesShouldNotAppearUnderClassNodeInCSharp() As System.Threading.Tasks.Task
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
                    TestFindResult.CreateDefinition($"[CSharpAssembly1] C.C() ({ServicesVSResources.ReferenceCountSingular})",
                        TestFindResult.CreateReference("CSharpAssembly1\Test1.cs - (11, 21) : var a = new C();")),
                    TestFindResult.CreateDefinition($"[CSharpAssembly1] C.C(int) ({ServicesVSResources.ReferenceCountSingular})",
                        TestFindResult.CreateReference("CSharpAssembly1\Test1.cs - (12, 21) : var b = new C(5);")),
                    TestFindResult.CreateDefinition($"[CSharpAssembly1] class C ({ServicesVSResources.ReferenceCountSingular})",
                        TestFindResult.CreateReference("CSharpAssembly1\Test1.cs - (13, 17) : var c = C.z;"))
                }

            Await VerifyAsync(markup, LanguageNames.CSharp, expectedResults)
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.FindReferences)>
        <WorkItem(1138943)>
        Public Async Function ConstructorReferencesShouldNotAppearUnderClassNodeInVisualBasic() As System.Threading.Tasks.Task
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
                    TestFindResult.CreateDefinition($"[VisualBasicAssembly1] Class C ({ServicesVSResources.ReferenceCountSingular})",
                        TestFindResult.CreateReference("VisualBasicAssembly1\Test1.vb - (14, 17) : Dim d = C.z")),
                    TestFindResult.CreateDefinition($"[VisualBasicAssembly1] Sub C.New() ({ServicesVSResources.ReferenceCountSingular})",
                        TestFindResult.CreateReference("VisualBasicAssembly1\Test1.vb - (12, 21) : Dim a = New C()")),
                    TestFindResult.CreateDefinition($"[VisualBasicAssembly1] Sub C.New(Integer) ({ServicesVSResources.ReferenceCountSingular})",
                        TestFindResult.CreateReference("VisualBasicAssembly1\Test1.vb - (13, 21) : Dim b = New C(5)"))
                }

            Await VerifyAsync(markup, LanguageNames.VisualBasic, expectedResults)
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.FindReferences)>
        Public Async Function TestSourceNamespace() As System.Threading.Tasks.Task
            Dim markup = <Text><![CDATA[
namespace NS$$
{
}

namespace NS
{
}
]]></Text>

            Dim expectedResults = New List(Of AbstractTreeItem) From
                {
                    TestFindResult.CreateUnnavigable($"namespace NS ({String.Format(ServicesVSResources.ReferenceCountPlural, 2)})",
                        TestFindResult.CreateReference("CSharpAssembly1\Test1.cs - (2, 11) : namespace NS"),
                        TestFindResult.CreateReference("CSharpAssembly1\Test1.cs - (6, 11) : namespace NS"))
                }

            Await VerifyAsync(markup, LanguageNames.CSharp, expectedResults)
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.FindReferences)>
        Public Async Function TestMetadataNamespace() As System.Threading.Tasks.Task
            Dim markup = <Text><![CDATA[
using System$$;
using System.Threading;
]]></Text>

            Dim expectedResults = New List(Of AbstractTreeItem) From
                {
                    TestFindResult.CreateUnnavigable($"namespace System ({String.Format(ServicesVSResources.ReferenceCountPlural, 2)})",
                        TestFindResult.CreateReference("CSharpAssembly1\Test1.cs - (2, 7) : using System;"),
                        TestFindResult.CreateReference("CSharpAssembly1\Test1.cs - (3, 7) : using System.Threading;"))
                }

            Await VerifyAsync(markup, LanguageNames.CSharp, expectedResults)
        End Function


        Private Shared ReadOnly s_exportProvider As ExportProvider = MinimalTestExportProvider.CreateExportProvider(
            TestExportProvider.MinimumCatalogWithCSharpAndVisualBasic.WithParts(
                GetType(MockDocumentNavigationServiceProvider),
                GetType(MockSymbolNavigationServiceProvider)))

        Private Async Function VerifyAsync(markup As XElement, languageName As String, expectedResults As IList(Of AbstractTreeItem)) As System.Threading.Tasks.Task
            Dim workspaceXml =
                <Workspace>
                    <Project Language=<%= languageName %> CommonReferences="true">
                        <Document><%= markup %></Document>
                    </Project>
                </Workspace>

            Using workspace = Await TestWorkspace.CreateWorkspaceAsync(workspaceXml, exportProvider:=s_exportProvider)
                Dim doc = workspace.Documents.Single()
                Dim workspaceDoc = workspace.CurrentSolution.GetDocument(doc.Id)
                If Not doc.CursorPosition.HasValue Then
                    Assert.True(False, "Missing caret location in document.")
                End If

                Dim symbol = Await SymbolFinder.FindSymbolAtPositionAsync(workspaceDoc, doc.CursorPosition.Value, CancellationToken.None)
                Assert.NotNull(symbol)

                Dim result = Await SymbolFinder.FindReferencesAsync(symbol, workspace.CurrentSolution, CancellationToken.None)

                WpfTestCase.RequireWpfFact($"The {NameOf(Implementation.Library.FindResults.LibraryManager)} assumes it's on the VS UI thread and thus uses WaitAndGetResult")
                Dim libraryManager = New LibraryManager(New MockServiceProvider(New MockComponentModel(workspace.ExportProvider)))
                Dim findReferencesTree = libraryManager.CreateFindReferencesItems(workspace.CurrentSolution, result)

                ' We cannot control the ordering of top-level nodes in the Find Symbol References window, so do not consider ordering of these items here.
                expectedResults = expectedResults.OrderBy(Function(n) n.DisplayText).ToList()
                findReferencesTree = findReferencesTree.OrderBy(Function(n) n.DisplayText).ToList()

                VerifyResultsTree(expectedResults, findReferencesTree)
            End Using
        End Function

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
                Assert.Equal(expectedItem.CanGoToDefinition, actualItem.CanGoToDefinition)
                Assert.Equal(expectedItem.CanGoToReference, actualItem.CanGoToReference)

                Dim expectedHasChildren = expectedItem.Children IsNot Nothing AndAlso expectedItem.Children.Count > 0
                Dim actualHasChildren = actualItem.Children IsNot Nothing AndAlso actualItem.Children.Count > 0

                Assert.Equal(expectedHasChildren, actualHasChildren)

                If expectedHasChildren Then
                    VerifyResultsTree(expectedItem.Children, actualItem.Children)
                End If
            Next
        End Sub

        Private Function GetResultText(items As IList(Of AbstractTreeItem)) As String
            Dim indentString = String.Empty
            Dim stringBuilder = New StringBuilder()

            GetResultTextWorker(items, stringBuilder, indentString)

            Return stringBuilder.ToString()
        End Function

        Private Sub GetResultTextWorker(items As IList(Of AbstractTreeItem), stringBuilder As StringBuilder, indentString As String)
            For Each item In items
                stringBuilder.Append(indentString)
                stringBuilder.Append(item.DisplayText)
                stringBuilder.Append(" [Kind: " &
                                     If(item.CanGoToDefinition, "Def", String.Empty) &
                                     If(item.CanGoToReference, "Ref", String.Empty) &
                                     If(item.CanGoToDefinition OrElse item.CanGoToDefinition, "None", String.Empty) &
                                     "]")

                If item.Children IsNot Nothing AndAlso item.Children.Any() Then
                    GetResultTextWorker(item.Children, stringBuilder, indentString + "  ")
                End If
            Next
        End Sub

        Private Class TestFindResult
            Inherits AbstractTreeItem

            Public ReadOnly expectedCanGoToDefinition As Boolean?
            Public ReadOnly expectedCanGoToReference As Boolean?

            Public Overrides Function CanGoToDefinition() As Boolean
                Return If(expectedCanGoToDefinition, True)
            End Function

            Public Overrides Function CanGoToReference() As Boolean
                Return If(expectedCanGoToReference, True)
            End Function

            Public Shared Function CreateDefinition(displayText As String, ParamArray children As TestFindResult()) As TestFindResult
                Return New TestFindResult(displayText, children, expectedCanGoToDefinition:=True, expectedCanGoToReference:=False)
            End Function

            Public Shared Function CreateReference(displayText As String, ParamArray children As TestFindResult()) As TestFindResult
                Return New TestFindResult(displayText, children, expectedCanGoToDefinition:=False, expectedCanGoToReference:=True)
            End Function

            Public Shared Function CreateUnnavigable(displayText As String, ParamArray children As TestFindResult()) As TestFindResult
                Return New TestFindResult(displayText, children, expectedCanGoToDefinition:=False, expectedCanGoToReference:=False)
            End Function

            Private Sub New(displayText As String,
                            children As TestFindResult(),
                            expectedCanGoToDefinition As Boolean,
                            expectedCanGoToReference As Boolean)

                MyBase.New(0)
                Me.DisplayText = displayText
                Me.expectedCanGoToDefinition = expectedCanGoToDefinition
                Me.expectedCanGoToReference = expectedCanGoToReference
                Me.Children = If(children IsNot Nothing AndAlso children.Length > 0, children, Nothing)
            End Sub

            Public Overrides Function GoToSource() As Integer
                Throw New NotImplementedException()
            End Function
        End Class
    End Class
End Namespace
