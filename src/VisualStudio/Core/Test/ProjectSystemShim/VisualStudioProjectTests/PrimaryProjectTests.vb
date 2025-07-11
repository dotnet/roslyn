' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Threading
Imports Microsoft.CodeAnalysis
Imports Microsoft.CodeAnalysis.Test.Utilities
Imports Microsoft.VisualStudio.LanguageServices.Implementation.ProjectSystem
Imports Microsoft.VisualStudio.LanguageServices.UnitTests.ProjectSystemShim.Framework
Imports Roslyn.Test.Utilities

Namespace Microsoft.VisualStudio.LanguageServices.UnitTests.ProjectSystemShim
    <[UseExportProvider]>
    Public Class PrimaryProjectTests
        <WpfFact>
        Public Async Function ProjectIsPrimaryByDefault() As Task
            Using environment = New TestEnvironment(GetType(TestDynamicFileInfoProviderThatProducesNoFiles))
                Dim project = Await environment.ProjectFactory.CreateAndAddToWorkspaceAsync(
                    "project", LanguageNames.CSharp, New VisualStudioProjectCreationInfo(), CancellationToken.None)

                Assert.True(project.IsPrimary)
            End Using
        End Function

        <WpfFact>
        Public Async Function ChangeProjectIsPrimary() As Task
            Using environment = New TestEnvironment(GetType(TestDynamicFileInfoProviderThatProducesNoFiles))
                Dim project = Await environment.ProjectFactory.CreateAndAddToWorkspaceAsync(
                    "project", LanguageNames.CSharp, New VisualStudioProjectCreationInfo(), CancellationToken.None)

                project.IsPrimary = False
                Assert.False(project.IsPrimary)
            End Using
        End Function

        <WpfFact>
        Public Async Function ChangeProjectIsPrimaryBack() As Task
            Using environment = New TestEnvironment(GetType(TestDynamicFileInfoProviderThatProducesNoFiles))
                Dim project = Await environment.ProjectFactory.CreateAndAddToWorkspaceAsync(
                    "project", LanguageNames.CSharp, New VisualStudioProjectCreationInfo(), CancellationToken.None)

                project.IsPrimary = False
                project.IsPrimary = True
                Assert.True(project.IsPrimary)
            End Using
        End Function
    End Class
End Namespace

