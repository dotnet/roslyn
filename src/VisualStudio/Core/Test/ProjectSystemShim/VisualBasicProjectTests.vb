' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports Microsoft.CodeAnalysis.Test.Utilities
Imports Microsoft.VisualStudio.LanguageServices.UnitTests.ProjectSystemShim.Framework
Imports Microsoft.VisualStudio.LanguageServices.UnitTests.ProjectSystemShim.VisualBasicHelpers
Imports Roslyn.Test.Utilities

Namespace Microsoft.VisualStudio.LanguageServices.UnitTests.ProjectSystemShim
    <[UseExportProvider]>
    <Trait(Traits.Feature, Traits.Features.ProjectSystemShims)>
    Public Class VisualBasicProjectTests
        <WpfFact()>
        Public Sub RenameProjectUpdatesWorkspace()
            Using environment = New TestEnvironment()
                Dim project = CreateVisualBasicProject(environment, "Test")
                Dim hierarchy = DirectCast(project.Hierarchy, MockHierarchy)

                Assert.Equal(environment.Workspace.CurrentSolution.Projects.Single().Name, "Test")

                hierarchy.RenameProject("Test2")

                Assert.Equal(environment.Workspace.CurrentSolution.Projects.Single().Name, "Test2")

                project.Disconnect()
            End Using
        End Sub

        <WpfFact()>
        Public Sub DisconnectingAProjectDoesNotLeak()
            Using environment = New TestEnvironment()
                Dim project = ObjectReference.CreateFromFactory(Function() CreateVisualBasicProject(environment, "Test"))

                Assert.Single(environment.Workspace.CurrentSolution.Projects)

                project.UseReference(Sub(p) p.Disconnect())

                project.AssertReleased()
            End Using
        End Sub
    End Class
End Namespace
