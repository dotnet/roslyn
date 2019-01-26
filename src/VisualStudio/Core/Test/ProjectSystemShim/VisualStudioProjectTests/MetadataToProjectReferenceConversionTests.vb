' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports Microsoft.CodeAnalysis
Imports Microsoft.CodeAnalysis.Test.Utilities
Imports Microsoft.VisualStudio.LanguageServices.UnitTests.ProjectSystemShim.Framework
Imports Roslyn.Test.Utilities

Namespace Microsoft.VisualStudio.LanguageServices.UnitTests.ProjectSystemShim
    <[UseExportProvider]>
    Public Class MetadataToProjectReferenceConversionTests
        <WpfFact>
        <WorkItem(32554, "https://github.com/dotnet/roslyn/issues/32554")>
        Public Sub ProjectReferenceConvertedToMetadataReferenceCanBeRemoved()
            Using environment = New TestEnvironment()
                Dim project1 = environment.ProjectFactory.CreateAndAddToWorkspace(
                    "project1",
                    LanguageNames.CSharp)

                Dim project2 = environment.ProjectFactory.CreateAndAddToWorkspace(
                    "project2",
                    LanguageNames.CSharp)

                Const ReferencePath = "C:\project1.dll"
                project1.OutputFilePath = ReferencePath
                project2.AddMetadataReference(ReferencePath, MetadataReferenceProperties.Assembly)

                Dim getProject2 = Function() environment.Workspace.CurrentSolution.GetProject(project2.Id)

                Assert.Single(getProject2().ProjectReferences)
                Assert.Empty(getProject2().MetadataReferences)

                project1.OutputFilePath = Nothing

                Assert.Single(getProject2().MetadataReferences)
                Assert.Empty(getProject2().ProjectReferences)

                project2.RemoveMetadataReference(ReferencePath, MetadataReferenceProperties.Assembly)
            End Using
        End Sub
    End Class
End Namespace
