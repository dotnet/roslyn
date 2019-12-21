' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports Microsoft.CodeAnalysis.Editor.UnitTests.Workspaces
Imports Microsoft.CodeAnalysis.Test.Utilities

Namespace Microsoft.CodeAnalysis.Lsif.Generator.UnitTests
    <UseExportProvider>
    Public NotInheritable Class ProjectStructureTests
        <Fact>
        Public Async Sub ProjectContainsDocuments()
            Dim lsif = Await GenerateForWorkspaceAsync(
                TestWorkspace.CreateWorkspace(
                    <Workspace>
                        <Project Language="C#" Name="TestProject" FilePath="Z:\TestProject.csproj">
                            <Document Name="A.cs" FilePath="Z:\A.cs"/>
                            <Document Name="B.cs" FilePath="Z:\B.cs"/>
                        </Project>
                    </Workspace>))

            Dim projectVertex = Assert.Single(lsif.Vertices.OfType(Of LsifGraph.Project))
            Dim documentVertices = lsif.GetLinkedVertices(Of LsifGraph.Document)(projectVertex, "contains")

            Assert.Single(documentVertices, Function(d) d.Uri.LocalPath = "Z:\A.cs")
            Assert.Single(documentVertices, Function(d) d.Uri.LocalPath = "Z:\B.cs")
        End Sub
    End Class
End Namespace
