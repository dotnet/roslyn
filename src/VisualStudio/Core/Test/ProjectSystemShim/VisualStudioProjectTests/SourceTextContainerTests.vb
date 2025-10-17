' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Threading
Imports Microsoft.CodeAnalysis
Imports Microsoft.CodeAnalysis.Test.Utilities
Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.VisualStudio.LanguageServices.Implementation.ProjectSystem
Imports Microsoft.VisualStudio.LanguageServices.UnitTests.ProjectSystemShim.Framework
Imports Microsoft.VisualStudio.Text
Imports Roslyn.Test.Utilities

Namespace Microsoft.VisualStudio.LanguageServices.UnitTests.ProjectSystemShim
    <[UseExportProvider]>
    Public Class SourceTextContainerTests
        <WpfFact>
        Public Async Function AddAndRemoveWorks() As Task
            Using environment = New TestEnvironment(GetType(TestDynamicFileInfoProviderThatProducesNoFiles))
                Dim project = Await environment.ProjectFactory.CreateAndAddToWorkspaceAsync(
                    "project", LanguageNames.CSharp, New VisualStudioProjectCreationInfo(), CancellationToken.None)

                Dim textBufferFactory = environment.ExportProvider.GetExportedValue(Of ITextBufferFactoryService)()
                Dim sourceTextContainer = textBufferFactory.CreateTextBuffer().AsTextContainer()

                project.AddSourceTextContainer(sourceTextContainer, "Z:\Test.cs")

                Assert.Single(environment.Workspace.GetOpenDocumentIds())

                project.RemoveSourceTextContainer(sourceTextContainer)

                Assert.Empty(environment.Workspace.GetOpenDocumentIds())
            End Using
        End Function
    End Class
End Namespace

