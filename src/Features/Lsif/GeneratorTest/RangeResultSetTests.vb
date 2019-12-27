' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports Microsoft.CodeAnalysis.Editor.UnitTests.Workspaces
Imports Microsoft.CodeAnalysis.Test.Utilities
Imports Microsoft.VisualStudio.LanguageServer.Protocol

Namespace Microsoft.CodeAnalysis.Lsif.Generator.UnitTests
    <UseExportProvider>
    Public NotInheritable Class RangeResultSetTests
        Private Const TestProjectAssemblyName As String = "TestProject"

        <Theory>
        <InlineData("class C { [|string|] s; }", "mscorlib#T:System.String")>
        <InlineData("class C { void M() { [|M|](); }", TestProjectAssemblyName + "#M:C.M")>
        <InlineData("class C { void M(string s) { M([|s|]) }", TestProjectAssemblyName + "#M:C.M(System.String)#s")>
        Public Async Sub ReferenceMonikerAsync(code As String, expectedMoniker As String)
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
            Dim resultSetVertex = lsif.GetLinkedVertices(Of LsifGraph.ResultSet)(rangeVertex, "next").Single()
            Dim monikerVertex = lsif.GetLinkedVertices(Of LsifGraph.Moniker)(resultSetVertex, "moniker").Single()

            Assert.Equal(expectedMoniker, monikerVertex.Identifier)
        End Sub

        <Fact>
        Public Async Sub ReferenceIncludedInReferenceResultAsync()
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
            Dim resultSetVertex = lsif.GetLinkedVertices(Of LsifGraph.ResultSet)(rangeVertex, "next").Single()
            Dim referencesVertex = lsif.GetLinkedVertices(Of LsifGraph.ReferenceResult)(resultSetVertex, Methods.TextDocumentReferencesName).Single()

            ' The references vertex should point back to our range
            Dim referencedRange = Assert.Single(lsif.GetLinkedVertices(Of LsifGraph.Range)(referencesVertex, "item"))
            Assert.Same(rangeVertex, referencedRange)
        End Sub
    End Class
End Namespace
