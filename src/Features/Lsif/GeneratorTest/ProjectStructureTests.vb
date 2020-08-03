' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports Microsoft.CodeAnalysis.Editor.UnitTests.Workspaces
Imports Microsoft.CodeAnalysis.Test.Utilities

Namespace Microsoft.CodeAnalysis.LanguageServerIndexFormat.Generator.UnitTests
    <UseExportProvider>
    Public NotInheritable Class ProjectStructureTests
        <Fact>
        Public Async Function ProjectContainsDocuments() As Task
            Dim lsif = Await TestLsifOutput.GenerateForWorkspaceAsync(
                TestWorkspace.CreateWorkspace(
                    <Workspace>
                        <Project Language="C#" Name="TestProject" FilePath="Z:\TestProject.csproj">
                            <Document Name="A.cs" FilePath="Z:\A.cs"/>
                            <Document Name="B.cs" FilePath="Z:\B.cs"/>
                        </Project>
                    </Workspace>))

            Dim projectVertex = Assert.Single(lsif.Vertices.OfType(Of Graph.LsifProject))
            Dim documentVertices = lsif.GetLinkedVertices(Of Graph.LsifDocument)(projectVertex, "contains")

            Assert.Single(documentVertices, Function(d) d.Uri.LocalPath = "Z:\A.cs")
            Assert.Single(documentVertices, Function(d) d.Uri.LocalPath = "Z:\B.cs")
        End Function
    End Class
End Namespace
