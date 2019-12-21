' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Namespace Microsoft.CodeAnalysis.Lsif.Generator.UnitTests.Utilities
    Friend Module TestGenerator
        Public Async Function GenerateForWorkspaceAsync(workspace As Workspace) As Task(Of TestLsifJsonWriter)
            Dim testLsifJsonWriter = New TestLsifJsonWriter()
            Dim generator = New Generator(testLsifJsonWriter)

            For Each Project In workspace.CurrentSolution.Projects
                Dim compilation = Await Project.GetCompilationAsync()
                Await generator.GenerateForCompilation(compilation, Project.FilePath, Project.LanguageServices)
            Next

            Return testLsifJsonWriter
        End Function
    End Module
End Namespace
