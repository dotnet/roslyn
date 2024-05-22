' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports Microsoft.CodeAnalysis.LanguageServerIndexFormat.Generator.Graph
Imports Microsoft.CodeAnalysis.Test.Utilities
Imports Roslyn.Test.Utilities
Imports LSP = Roslyn.LanguageServer.Protocol

Namespace Microsoft.CodeAnalysis.LanguageServerIndexFormat.Generator.UnitTests
    <UseExportProvider>
    Public Class DocumentSymbolTests
        <Theory>
        <InlineData("{|fullRange:class [|C|] { }|}", CType(LSP.SymbolKind.Class, Integer), "C")>
        <InlineData(" {|fullRange:/* leading comment 1 */ /* leading comment 2 */ class [|C|] { } /* trailing comment 1*/ /* trailing comment 2 */|} ", CType(LSP.SymbolKind.Class, Integer), "C")>
        <InlineData("{|fullRange:class [|@class|] { }|}", CType(LSP.SymbolKind.Class, Integer), "@class")>
        <InlineData("{|fullRange:struct [|S|] { }|}", CType(LSP.SymbolKind.Struct, Integer), "S")>
        <InlineData("class C { {|fullRange:void [|M|]() { }|} }", CType(LSP.SymbolKind.Method, Integer), "M")>
        <InlineData("class C { int {|fullRange:[|field|]|}; }", CType(LSP.SymbolKind.Field, Integer), "field")>
        <InlineData("{|fullRange:partial class [|C|] { int a; }|} partial class C { int b; }", CType(LSP.SymbolKind.Class, Integer), "C")>
        Public Async Function TestDefinition(code As String, expectedSymbolKindInt As Integer, expectedText As String) As Task
            Dim expectedSymbolKind = CType(expectedSymbolKindInt, LSP.SymbolKind)
            Dim lsif = Await TestLsifOutput.GenerateForWorkspaceAsync(
                <Workspace>
                    <Project Language="C#" FilePath="Z:\TestProject.csproj" CommonReferences="true">
                        <Document Name="A.cs" FilePath="Z:\A.cs">
                            <%= code %>
                        </Document>
                    </Project>
                </Workspace>)

            ' Assert the specific range is what we expected
            Dim selectedRange = Await lsif.GetSelectedRangeAsync()
            Assert.NotNull(selectedRange)
            Dim definitionTag = Assert.IsType(Of DefinitionRangeTag)(selectedRange.Tag)
            Assert.Equal(expectedSymbolKind, definitionTag.Kind)

            Dim expectedFullRange = Await lsif.GetAnnotatedLspRangeAsync("fullRange")
            Assert.Equal(expectedFullRange, definitionTag.FullRange)

            Assert.Equal(expectedText, definitionTag.Text)

            ' We also output the overall hierarchy of document symbols as a document request; ensure that contains all the ranges
            Dim document = lsif.Vertices.OfType(Of LsifDocument).Single()
            Dim documentSymbolResult = Assert.Single(lsif.GetLinkedVertices(Of DocumentSymbolResult)(document, LSP.Methods.TextDocumentDocumentSymbolName))
            Dim allDocumentSymbolRangeVertices = GetDocumentSymbolRangeIds(documentSymbolResult.Result)
            Dim allRangeVertices = lsif.Vertices.OfType(Of Range).Where(Function(r) TypeOf r.Tag Is DefinitionRangeTag)

            AssertEx.SetEqual(allRangeVertices.Select(Function(r) r.GetId()), allDocumentSymbolRangeVertices)

            For Each documentSymbol In documentSymbolResult.Result
                AssertDocumentSymbolContainsChildren(documentSymbol)
            Next
        End Function

        ''' <summary>
        ''' This tests symbols that might be considered "definitions" in the C#/Roslyn sense, but don't make sense to emit as a document symbol.
        ''' </summary>
        <Theory>
        <InlineData("class C { (int [|A|], int B) tuple; }")>
        <InlineData("class C { ((int A, int B) [|A|], int B) nestedTuple; }")>
        <InlineData("class C { object o = new { [|AnonymousTypeField|] = 42 }; }")>
        Public Async Function TestIsNotDefinition(code As String) As Task
            Dim lsif = Await TestLsifOutput.GenerateForWorkspaceAsync(
                <Workspace>
                    <Project Language="C#" FilePath="Z:\TestProject.csproj" CommonReferences="true">
                        <Document Name="A.cs" FilePath="Z:\A.cs">
                            <%= code %>
                        </Document>
                    </Project>
                </Workspace>)

            ' Assert the specific range is what we expected
            Dim selectedRange = Await lsif.GetSelectedRangeAsync()
            Assert.NotNull(selectedRange)
            Assert.Null(selectedRange.Tag)
        End Function

        Private Shared Function GetDocumentSymbolRangeIds(result As List(Of RangeBasedDocumentSymbol)) As IEnumerable(Of Id(Of Range))
            If result Is Nothing Then
                Return Array.Empty(Of Id(Of Range))
            End If

            Return result.SelectMany(Iterator Function(documentSymbol)
                                         Yield documentSymbol.Id

                                         For Each childId In GetDocumentSymbolRangeIds(documentSymbol.Children)
                                             Yield childId
                                         Next
                                     End Function)
        End Function

        Private Shared Sub AssertDocumentSymbolContainsChildren(documentSymbol As RangeBasedDocumentSymbol)
            If documentSymbol.Children IsNot Nothing Then
                For Each child In documentSymbol.Children
                    Assert.True(documentSymbol.Span.Contains(child.Span))

                    AssertDocumentSymbolContainsChildren(child)
                Next
            End If
        End Sub
    End Class
End Namespace
