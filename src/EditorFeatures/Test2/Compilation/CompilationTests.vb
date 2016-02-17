' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Threading
Imports Microsoft.CodeAnalysis.Editor.UnitTests.Workspaces

Namespace Microsoft.CodeAnalysis.Editor.Implementation.Compilation.UnitTests

    Public Class CompilationTests
        Private Function GetProject(snapshot As Solution, assemblyName As String) As Project
            Return snapshot.Projects.Single(Function(p) p.AssemblyName = assemblyName)
        End Function

        <Fact>
        <WorkItem(1107492, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1107492")>
        Public Async Function TestProjectThatDoesntSupportCompilations() As Tasks.Task
            Dim workspaceDefinition =
<Workspace>
    <Project Language="NoCompilation" AssemblyName="TestAssembly" CommonReferencesPortable="true">
        <Document>
            var x = {}; // e.g., TypeScript code or anything else that doesn't support compilations
        </Document>
    </Project>
</Workspace>

            Using workspace = Await TestWorkspace.CreateAsync(workspaceDefinition)
                Dim project = GetProject(workspace.CurrentSolution, "TestAssembly")
                Assert.Null(Await project.GetCompilationAsync())

                Dim solution = project.Solution
                Assert.Null(Await project.GetCompilationAsync())
                Assert.False(Await solution.ContainsSymbolsWithNameAsync(project.Id, Function(dummy) True, SymbolFilter.TypeAndMember, CancellationToken.None))
                Assert.Empty(Await solution.GetDocumentsWithNameAsync(project.Id, Function(dummy) True, SymbolFilter.TypeAndMember, CancellationToken.None))
            End Using
        End Function
    End Class

End Namespace
