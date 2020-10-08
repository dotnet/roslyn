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
        <InlineData("class C { void M() { [|M|](); } }", TestProjectAssemblyName + "#M:C.M", WellKnownSymbolMonikerSchemes.DotnetXmlDoc)>
        <InlineData("class C { void M(string s) { M([|s|]); } }", TestProjectAssemblyName + "#M:C.M(System.String)#s", WellKnownSymbolMonikerSchemes.DotnetXmlDoc)>
        <InlineData("class C { void M(string s) { string local = """"; M([|local|]); } }", Nothing, Nothing)>
        <InlineData("using [|S|] = System.String;", Nothing, Nothing)>
        <InlineData("class C { [|global|]::System.String s; }", "<global namespace>", WellKnownSymbolMonikerSchemes.DotnetNamespace)>
        Public Async Function ReferenceMoniker(code As String, expectedMoniker As String, expectedMonikerScheme As String) As Task
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
        End Function

        <Theory>
        <InlineData("// Comment")>
        <InlineData("extern alias A;")>
        Public Async Function NoRangesAtAll(code As String) As Task
            Dim lsif = Await TestLsifOutput.GenerateForWorkspaceAsync(
                TestWorkspace.CreateWorkspace(
                    <Workspace>
                        <Project Language="C#" AssemblyName=<%= TestProjectAssemblyName %> FilePath="Z:\TestProject.csproj" CommonReferences="true">
                            <Document Name="A.cs" FilePath="Z:\A.cs">
                                <%= code %>
                            </Document>
                            <ProjectReference Alias="A">ReferencedWithAlias</ProjectReference>
                        </Project>
                        <Project AssemblyName="ReferencedWithAlias" Language="C#" FilePath="Z:\ReferencedWithAlias.csproj"></Project>
                    </Workspace>))

            Assert.Empty(lsif.Vertices.OfType(Of Range))
        End Function

        <Fact>
        Public Async Function DefinitionIncludedInDefinitionResult() As Task
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
        End Function

        <Fact>
        Public Async Function ReferenceIncludedInReferenceResult() As Task
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
        End Function

        <Fact>
        Public Async Function ReferenceIncludedInSameReferenceResultForMultipleFiles() As Task
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
        End Function

        <Theory>
        <InlineData("class A { public const int C = 42 + 42; }", "class B { public const int C = 42 + 42; }")> ' case for built-in operators
        <InlineData("class A { public void M() { } }",
                    "class B
                    {
                        /// <see cref=""A.M()"" />
                        public void M2() { }
                    }")> ' case for crefs
        Public Async Function NoCrossDocumentReferencesWithoutAMoniker(file1 As String, file2 As String) As Task
            Dim lsif = Await TestLsifOutput.GenerateForWorkspaceAsync(
                TestWorkspace.CreateWorkspace(
                    <Workspace>
                        <Project Language="C#" AssemblyName=<%= TestProjectAssemblyName %> FilePath="Z:\TestProject.csproj" CommonReferences="true">
                            <Document Name="File1.cs" FilePath="Z:\File1.cs"><%= file1 %></Document>
                            <Document Name="File2.cs" FilePath="Z:\File2.cs"><%= file2 %></Document>
                        </Project>
                    </Workspace>))

            ' If we ever emit a result set that doesn't have a moniker, some LSIF importers will make up a moniker
            ' for us when they're importing, which can be based on the first range that they see. This is problematic if
            ' the result set crosses multiple files, because there's no very stable moniker they can easily pick that won't
            ' change if unrelated documents change.
            For Each resultSetVertex In lsif.Vertices.OfType(Of Graph.ResultSet)
                Dim monikerVertex = lsif.GetLinkedVertices(Of Graph.Moniker)(resultSetVertex, "moniker").SingleOrDefault()

                ' If it's got a moniker, then no concerns
                If monikerVertex IsNot Nothing Then
                    Continue For
                End If

                Dim documents As New HashSet(Of Graph.LsifDocument)

                ' Let's now enumerate all the documents and ranges to see which documents contain a range that links to
                ' this resultSet
                For Each document In lsif.Vertices.OfType(Of Graph.LsifDocument)
                    For Each range In lsif.GetLinkedVertices(Of Graph.Range)(document, "contains")
                        If lsif.GetLinkedVertices(Of Graph.ResultSet)(range, "next").Contains(resultSetVertex) Then
                            documents.Add(document)
                        End If
                    Next
                Next

                If documents.Count > 0 Then
                    Assert.Single(documents)
                End If
            Next
        End Function
    End Class
End Namespace
