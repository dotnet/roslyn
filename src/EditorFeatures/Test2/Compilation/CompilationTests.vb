' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Threading
Imports Microsoft.CodeAnalysis.Editor.UnitTests.Workspaces

Namespace Microsoft.CodeAnalysis.Editor.Implementation.Compilation.UnitTests

    Public Class CompilationTests
        Private Function GetProject(snapshot As Solution, assemblyName As String) As Project
            Return snapshot.Projects.Single(Function(p) p.AssemblyName = assemblyName)
        End Function

        <WpfFact>
        <WorkItem(1107492)>
        Public Async Function TestProjectThatDoesntSupportCompilations() As Tasks.Task
            Dim workspaceDefinition =
<Workspace>
    <Project Language="NoCompilation" AssemblyName="TestAssembly" CommonReferencesPortable="true">
        <Document>
            var x = {}; // e.g., TypeScript code or anything else that doesn't support compilations
        </Document>
    </Project>
</Workspace>

            Using workspace = Await TestWorkspaceFactory.CreateWorkspaceAsync(workspaceDefinition)
                Dim project = GetProject(workspace.CurrentSolution, "TestAssembly")
                Assert.Null(project.GetCompilationAsync(CancellationToken.None).Result)

                Dim solution = project.Solution
                Assert.Null(project.GetCompilationAsync(CancellationToken.None).Result)
                Assert.False(solution.ContainsSymbolsWithNameAsync(project.Id, Function(dummy) True, SymbolFilter.TypeAndMember, CancellationToken.None).Result)
                Assert.Empty(solution.GetDocumentsWithName(project.Id, Function(dummy) True, SymbolFilter.TypeAndMember, CancellationToken.None).Result)
            End Using
        End Function
    End Class

End Namespace
