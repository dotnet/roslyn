' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Collections.Immutable
Imports System.Threading
Imports Microsoft.CodeAnalysis
Imports Microsoft.CodeAnalysis.Test.Utilities
Imports Microsoft.VisualStudio.LanguageServices.UnitTests.ProjectSystemShim.Framework
Imports Roslyn.Test.Utilities

Namespace Microsoft.VisualStudio.LanguageServices.UnitTests.ProjectSystemShim
    <[UseExportProvider]>
    Public Class AnalyzerReferenceTests
        <WpfFact>
        Public Async Function RemoveAndReAddInSameBatchWorksCorrectly() As Task
            Using environment = New TestEnvironment(GetType(TestDynamicFileInfoProviderThatProducesNoFiles))
                Dim project = Await environment.ProjectFactory.CreateAndAddToWorkspaceAsync(
                    "Project", LanguageNames.CSharp, CancellationToken.None)
                Const analyzerPath = "Z:\TestAnalyzer.dll"

                project.AddAnalyzerReference(analyzerPath)

                Using project.CreateBatchScope()
                    project.RemoveAnalyzerReference(analyzerPath)
                    project.AddAnalyzerReference(analyzerPath)
                    project.RemoveAnalyzerReference(analyzerPath)
                    project.AddAnalyzerReference(analyzerPath)
                End Using

                ' In the end, we should have exactly one reference
                Assert.Single(environment.Workspace.CurrentSolution.Projects.Single().AnalyzerReferences)
            End Using
        End Function
    End Class
End Namespace
