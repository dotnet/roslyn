' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports Microsoft.CodeAnalysis.Editor.UnitTests.Workspaces
Imports Microsoft.CodeAnalysis.Test.Utilities
Imports Microsoft.VisualStudio.LanguageServer.Protocol

Namespace Microsoft.CodeAnalysis.LanguageServerIndexFormat.Generator.UnitTests
    <UseExportProvider>
    Public NotInheritable Class RangeResultSetTests
        Private Const TestProjectAssemblyName As String = "TestProject"

        <Theory>
        <InlineData("class C { [|string|] s; }", "mscorlib#T:System.String", WellKnownSymbolMonikerSchemes.DotnetXmlDoc)>
        <InlineData("class C { void M() { [|M|](); }", TestProjectAssemblyName + "#M:C.M", WellKnownSymbolMonikerSchemes.DotnetXmlDoc)>
        <InlineData("class C { void M(string s) { M([|s|]) }", TestProjectAssemblyName + "#M:C.M(System.String)#s", WellKnownSymbolMonikerSchemes.DotnetXmlDoc)>
        <InlineData("class C { void M(string s) { string local; M([|local|]) }", Nothing, Nothing)>
        <InlineData("class C { void M(string s) { M(s [|+|] s) }", Nothing, Nothing)>
        <InlineData("using [|S|] = System.String;", Nothing, Nothing)>
        <InlineData("class C { [|global|]::System.String s; }", "<global namespace>", WellKnownSymbolMonikerSchemes.DotnetNamespace)>
        Public Async Sub ReferenceMoniker(code As String, expectedMoniker As String, expectedMonikerScheme As String)
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
            Dim monikerVertex = lsif.GetLinkedVertices(Of Graph.Moniker)(resultSetVertex, "moniker").SingleOrDefault()

            Assert.Equal(expectedMoniker, monikerVertex?.Identifier)
            Assert.Equal(expectedMonikerScheme, monikerVertex?.Scheme)
        End Sub

        <Theory>
        <InlineData("// Comment")>
        <InlineData("extern alias A;")>
        Public Async Sub NoRangesAtAll(code As String)
            Dim lsif = Await TestLsifOutput.GenerateForWorkspaceAsync(
                TestWorkspace.CreateWorkspace(
                    <Workspace>
                        <Project Language="C#" AssemblyName=<%= TestProjectAssemblyName %> FilePath="Z:\TestProject.csproj" CommonReferences="true">
                            <Document Name="A.cs" FilePath="Z:\A.cs">
                                <%= code %>
                            </Document>
                        </Project>
                    </Workspace>))

            Assert.Empty(lsif.Vertices.OfType(Of Range))
        End Sub

        <Fact>
        Public Async Sub DefinitionIncludedInDefinitionResult()
            Dim lsif = Await TestLsifOutput.GenerateForWorkspaceAsync(
                TestWorkspace.CreateWorkspace(
                    <Workspace>
                        <Project Language="C#" AssemblyName=<%= TestProjectAssemblyName %> FilePath="Z:\TestProject.csproj" CommonReferences="true">
                            <Document Name="A.cs" FilePath="Z:\A.cs">
                                class [|C|] { }
                            </Document>
                        </Project>
                    </Workspace>))

            Dim rangeVertex = Await lsif.GetSelectedRangeAsync()
            Dim resultSetVertex = lsif.GetLinkedVertices(Of Graph.ResultSet)(rangeVertex, "next").Single()
            Dim definitionsVertex = lsif.GetLinkedVertices(Of Graph.DefinitionResult)(resultSetVertex, Methods.TextDocumentDefinitionName).Single()

            ' The definition vertex should point back to our range
            Dim referencedRange = Assert.Single(lsif.GetLinkedVertices(Of Graph.Range)(definitionsVertex, "item"))
            Assert.Same(rangeVertex, referencedRange)
        End Sub

        <Fact>
        Public Async Sub ReferenceIncludedInReferenceResult()
            Dim lsif = Await TestLsifOutput.GenerateForWorkspaceAsync(
                TestWorkspace.CreateWorkspace(
                    <Workspace>
                        <Project Language="C#" AssemblyName=<%= TestProjectAssemblyName %> FilePath="Z:\TestProject.csproj" CommonReferences="true">
                            <Document Name="A.cs" FilePath="Z:\A.cs">
                                class C { [|string|] s; }
                            </Document>
                        </Project>
                    </Workspace>))

            Dim rangeVertex = Await lsif.GetSelectedRangeAsync()
            Dim resultSetVertex = lsif.GetLinkedVertices(Of Graph.ResultSet)(rangeVertex, "next").Single()
            Dim referencesVertex = lsif.GetLinkedVertices(Of Graph.ReferenceResult)(resultSetVertex, Methods.TextDocumentReferencesName).Single()

            ' The references vertex should point back to our range
            Dim referencedRange = Assert.Single(lsif.GetLinkedVertices(Of Graph.Range)(referencesVertex, "item"))
            Assert.Same(rangeVertex, referencedRange)
        End Sub

        <Fact>
        Public Async Sub ReferenceIncludedInSameReferenceResultForMultipleFiles()
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

            For Each rangeVertex In Await lsif.GetSelectedRangesAsync()
                Dim resultSetVertex = lsif.GetLinkedVertices(Of Graph.ResultSet)(rangeVertex, "next").Single()
                Dim referencesVertex = lsif.GetLinkedVertices(Of Graph.ReferenceResult)(resultSetVertex, Methods.TextDocumentReferencesName).Single()

                ' The references vertex should point back to our range
                Dim referencedRanges = lsif.GetLinkedVertices(Of Graph.Range)(referencesVertex, "item")
                Assert.Contains(rangeVertex, referencedRanges)
            Next
        End Sub
    End Class
End Namespace
