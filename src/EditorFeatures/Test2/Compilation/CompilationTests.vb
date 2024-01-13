' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Threading
Imports Microsoft.CodeAnalysis.Editor.UnitTests
Imports Microsoft.CodeAnalysis.Editor.UnitTests.Workspaces

Namespace Microsoft.CodeAnalysis.Editor.Implementation.Compilation.UnitTests
    <[UseExportProvider]>
    Public Class CompilationTests
        Private Shared Function GetProject(snapshot As Solution, assemblyName As String) As Project
            Return snapshot.Projects.Single(Function(p) p.AssemblyName = assemblyName)
        End Function

        <Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1107492")>
        Public Async Function TestProjectThatDoesntSupportCompilations() As Tasks.Task
            Dim workspaceDefinition =
<Workspace>
    <Project Language="NoCompilation" AssemblyName="TestAssembly" CommonReferencesPortable="true">
        <Document>
            var x = {}; // e.g., TypeScript code or anything else that doesn't support compilations
        </Document>
    </Project>
</Workspace>

            Dim composition = EditorTestCompositions.EditorFeatures.AddParts(
                GetType(NoCompilationContentTypeLanguageService),
                GetType(NoCompilationContentTypeDefinitions))

            Using workspace = EditorTestWorkspace.Create(workspaceDefinition, composition:=composition)
                Dim project = GetProject(workspace.CurrentSolution, "TestAssembly")
                Assert.Null(Await project.GetCompilationAsync())

                Assert.Null(Await project.GetCompilationAsync())
            End Using
        End Function
    End Class
End Namespace
