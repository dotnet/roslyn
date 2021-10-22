﻿' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Text
Imports Microsoft.CodeAnalysis.Editor.UnitTests.Workspaces
Imports Microsoft.CodeAnalysis.Test.Utilities
Imports Roslyn.Test.Utilities

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

            Dim documentA = Assert.Single(documentVertices, Function(d) d.Uri.LocalPath = "Z:\A.cs")
            Dim documentB = Assert.Single(documentVertices, Function(d) d.Uri.LocalPath = "Z:\B.cs")

            ' We don't include contents for normal files, just generated ones
            Assert.Null(documentA.Contents)
            Assert.Null(documentB.Contents)
        End Function

        <Fact>
        Public Async Function SourceGeneratedDocumentsIncludeContent() As Task
            Dim workspace = TestWorkspace.CreateWorkspace(
                    <Workspace>
                        <Project Language="C#" Name="TestProject" FilePath="Z:\TestProject.csproj" CommonReferences="true">
                        </Project>
                    </Workspace>)

            workspace.OnAnalyzerReferenceAdded(workspace.CurrentSolution.ProjectIds.Single(),
                                               New TestGeneratorReference(New TestSourceGenerator.HelloWorldGenerator()))

            Dim lsif = Await TestLsifOutput.GenerateForWorkspaceAsync(workspace)

            Dim projectVertex = Assert.Single(lsif.Vertices.OfType(Of Graph.LsifProject))
            Dim generatedDocumentVertices = lsif.GetLinkedVertices(Of Graph.LsifDocument)(projectVertex, "contains")

            For Each generatedDocumentVertex In generatedDocumentVertices
                ' Assert the contents were included and does match the tree
                Dim contentBase64Encoded = generatedDocumentVertex.Contents
                Assert.NotNull(contentBase64Encoded)

                Dim contents = Encoding.UTF8.GetString(Convert.FromBase64String(contentBase64Encoded))

                Dim compilation = Await workspace.CurrentSolution.Projects.Single().GetCompilationAsync()
                Dim tree = Assert.Single(compilation.SyntaxTrees, Function(t) t.FilePath = generatedDocumentVertex.Uri.OriginalString)

                Assert.Equal(tree.GetText().ToString(), contents)
            Next
        End Function
    End Class
End Namespace
