' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports Microsoft.CodeAnalysis.VisualBasic
Imports Roslyn.Test.Utilities
Imports Microsoft.VisualStudio.LanguageServices.UnitTests.ProjectSystemShim.Framework
Imports Microsoft.VisualStudio.LanguageServices.UnitTests.ProjectSystemShim.VisualBasicHelpers
Imports Microsoft.CodeAnalysis

Namespace Microsoft.VisualStudio.LanguageServices.UnitTests.ProjectSystemShim
    Public Class VisualBasicProjectTests
        <WpfFact()>
        <Trait(Traits.Feature, Traits.Features.ProjectSystemShims)>
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
    End Class
End Namespace
