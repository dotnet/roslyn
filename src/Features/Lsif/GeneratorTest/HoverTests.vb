' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports Microsoft.CodeAnalysis.Editor.UnitTests.Workspaces
Imports Microsoft.CodeAnalysis.Test.Utilities
Imports Microsoft.VisualStudio.LanguageServer.Protocol

Namespace Microsoft.CodeAnalysis.LanguageServerIndexFormat.Generator.UnitTests
    <UseExportProvider>
    Public NotInheritable Class HoverTests
        Private Const TestProjectAssemblyName As String = "TestProject"

        <Theory>
        <InlineData("class [|C|] { string s; }", "class C")>
        <InlineData("class C { void [|M|]() { } }", "void C.M()")>
        <InlineData("class C { string [|s|]; }", "(field) string C.s")>
        <InlineData("class C { void M(string [|s|]) { M(s); } }", "(parameter) string s")>
        <InlineData("class C { void M(string s) { string [|local|] = """"; } }", "(local variable) string local")>
        <InlineData("
class C
{
    /// <summary>Doc Comment</summary>
    void [|M|]() { }
}",
"void C.M()
Doc Comment")>
        Public Async Function TestDefinition(code As String, expectedHoverContents As String) As Task
            Dim lsif = Await TestLsifOutput.GenerateForWorkspaceAsync(
                TestWorkspace.CreateWorkspace(
                    <Workspace>
                        <Project Language="C#" AssemblyName=<%= TestProjectAssemblyName %> FilePath="Z:\TestProject.csproj" CommonReferences="true">
                            <Document Name="A.cs" FilePath="Z:\A.cs">
                                <%= code %>
                            </Document>
                        </Project>
                    </Workspace>))

            Dim rangeVertex = Await lsif.GetSelectedRangeAsync()
            Dim resultSetVertex = lsif.GetLinkedVertices(Of Graph.ResultSet)(rangeVertex, "next").Single()
            Dim hoverVertex = lsif.GetLinkedVertices(Of Graph.HoverResult)(resultSetVertex, Methods.TextDocumentHoverName).SingleOrDefault()
            Dim hoverMarkupContent = DirectCast(hoverVertex.Result.Contents.Value, MarkupContent)

            Assert.Equal(MarkupKind.PlainText, hoverMarkupContent.Kind)
            Assert.Equal(expectedHoverContents + Environment.NewLine, hoverMarkupContent.Value)
        End Function

        <Theory>
        <InlineData("class C { [|string|] s; }", "class System.String")>
        <InlineData("class C { void M() { [|M|](); } }", "void C.M()")>
        <InlineData("class C { void M(string s) { M([|s|]); } }", "(parameter) string s")>
        <InlineData("class C { void M(string s) { string local = """"; M([|local|]); } }", "(local variable) string local")>
        <InlineData("using [|S|] = System.String;", "class System.String")>
        <InlineData("class C { [|global|]::System.String s; }", "<global namespace>")>
        <InlineData("
class C
{
    /// <see cref=""C.[|M|]()"" />
    void M() { }
}",
"void C.M()")>
        Public Async Function TestReference(code As String, expectedHoverContents As String) As Task
            Dim lsif = Await TestLsifOutput.GenerateForWorkspaceAsync(
                TestWorkspace.CreateWorkspace(
                    <Workspace>
                        <Project Language="C#" AssemblyName=<%= TestProjectAssemblyName %> FilePath="Z:\TestProject.csproj" CommonReferences="true">
                            <Document Name="A.cs" FilePath="Z:\A.cs">
                                <%= code %>
                            </Document>
                        </Project>
                    </Workspace>))

            Dim rangeVertex = Await lsif.GetSelectedRangeAsync()
            Dim resultSetVertex = lsif.GetLinkedVertices(Of Graph.ResultSet)(rangeVertex, "next").Single()
            Dim hoverVertex = lsif.GetLinkedVertices(Of Graph.HoverResult)(resultSetVertex, Methods.TextDocumentHoverName).SingleOrDefault()
            Dim hoverMarkupContent = DirectCast(hoverVertex.Result.Contents.Value, MarkupContent)

            Assert.Equal(MarkupKind.PlainText, hoverMarkupContent.Kind)
            Assert.Equal(expectedHoverContents + Environment.NewLine, hoverMarkupContent.Value)
        End Function

        <Fact>
        Public Async Function ToplevelResultsInMultipleFiles() As Task
            Dim lsif = Await TestLsifOutput.GenerateForWorkspaceAsync(
                TestWorkspace.CreateWorkspace(
                    <Workspace>
                        <Project Language="C#" AssemblyName=<%= TestProjectAssemblyName %> FilePath="Z:\TestProject.csproj" CommonReferences="true">
                            <Document Name="A.cs" FilePath="Z:\A.cs">
                                class A { [|string|] s; }
                            </Document>
                            <Document Name="B.cs" FilePath="Z:\B.cs">
                                class B { [|string|] s; }
                            </Document>
                            <Document Name="C.cs" FilePath="Z:\C.cs">
                                class C { [|string|] s; }
                            </Document>
                            <Document Name="D.cs" FilePath="Z:\D.cs">
                                class D { [|string|] s; }
                            </Document>
                            <Document Name="E.cs" FilePath="Z:\E.cs">
                                class E { [|string|] s; }
                            </Document>
                        </Project>
                    </Workspace>))

            Dim hoverVertex As Graph.HoverResult = Nothing
            For Each rangeVertex In Await lsif.GetSelectedRangesAsync()
                Dim resultSetVertex = lsif.GetLinkedVertices(Of Graph.ResultSet)(rangeVertex, "next").Single()
                Dim vertex = lsif.GetLinkedVertices(Of Graph.HoverResult)(resultSetVertex, Methods.TextDocumentHoverName).Single()
                If hoverVertex Is Nothing Then
                    hoverVertex = vertex
                Else
                    Assert.Same(hoverVertex, vertex)
                End If
            Next

            Dim hoverMarkupContent = DirectCast(hoverVertex.Result.Contents.Value, MarkupContent)
            Assert.Equal(MarkupKind.PlainText, hoverMarkupContent.Kind)
            Assert.Equal("class System.String" + Environment.NewLine, hoverMarkupContent.Value)
        End Function
    End Class
End Namespace
