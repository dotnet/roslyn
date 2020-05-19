' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports Microsoft.CodeAnalysis
Imports Microsoft.CodeAnalysis.Test.Utilities
Imports Microsoft.VisualStudio.LanguageServices.UnitTests.ProjectSystemShim.Framework
Imports Roslyn.Test.Utilities

Namespace Microsoft.VisualStudio.LanguageServices.UnitTests.ProjectSystemShim
    <[UseExportProvider]>
    Public Class PrimaryProjectTests
        <WpfFact>
        Public Sub ProjectIsPrimaryByDefault()
            Using environment = New TestEnvironment(GetType(TestDynamicFileInfoProviderThatProducesNoFiles))
                Dim project = environment.ProjectFactory.CreateAndAddToWorkspace(
                    "project",
                    LanguageNames.CSharp)

                Assert.True(project.IsPrimary)
            End Using
        End Sub

        <WpfFact>
        Public Sub ChangeProjectIsPrimary()
            Using environment = New TestEnvironment(GetType(TestDynamicFileInfoProviderThatProducesNoFiles))
                Dim project = environment.ProjectFactory.CreateAndAddToWorkspace(
                    "project",
                    LanguageNames.CSharp)

                project.IsPrimary = False
                Assert.False(project.IsPrimary)
            End Using
        End Sub

        <WpfFact>
        Public Sub ChangeProjectIsPrimaryBack()
            Using environment = New TestEnvironment(GetType(TestDynamicFileInfoProviderThatProducesNoFiles))
                Dim project = environment.ProjectFactory.CreateAndAddToWorkspace(
                    "project",
                    LanguageNames.CSharp)

                project.IsPrimary = False
                project.IsPrimary = True
                Assert.True(project.IsPrimary)
            End Using
        End Sub
    End Class
End Namespace

