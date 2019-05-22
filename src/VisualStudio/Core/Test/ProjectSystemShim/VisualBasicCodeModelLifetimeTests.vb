Imports Microsoft.CodeAnalysis.Test.Utilities
Imports Microsoft.VisualStudio.LanguageServices.UnitTests.ProjectSystemShim.Framework
Imports Microsoft.VisualStudio.LanguageServices.UnitTests.ProjectSystemShim.VisualBasicHelpers
Imports Microsoft.VisualStudio.Shell.Interop
Imports Roslyn.Test.Utilities

Namespace Microsoft.VisualStudio.LanguageServices.UnitTests.ProjectSystemShim
    <[UseExportProvider]>
    Public Class VisualBasicCodeModelLifetimeTests
        <ConditionalWpfFact(GetType(x86)), Trait(Traits.Feature, Traits.Features.CodeModel)>
        <Trait(Traits.Feature, Traits.Features.ProjectSystemShims)>
        <WorkItem(33080, "https://github.com/dotnet/roslyn/issues/33080")>
        Public Sub RemovingAndReAddingSourceFileWorksCorrectly()
            Using environment = New TestEnvironment()
                Dim project = CreateVisualBasicProject(environment, "Test")

                ' Add the file we're adding and removing to our MockHierarchy, so CodeModel operations correctly work
                Dim project3 = DirectCast(project.Hierarchy, IVsProject3)
                project3.AddItem(42, VSADDITEMOPERATION.VSADDITEMOP_CLONEFILE, "Z:\SourceFile.vb", Nothing, Nothing, Nothing, Nothing)

                project.AddFile("Z:\SourceFile.vb", Nothing, False)

                ' Confirm that we are able to get a file code model
                Dim originalDocumentId = environment.Workspace.CurrentSolution.Projects.Single().DocumentIds.Single()
                Assert.NotNull(originalDocumentId)
                Dim originalFileCodeModel = environment.Workspace.GetFileCodeModel(originalDocumentId)
                Assert.NotNull(originalDocumentId)

                project.RemoveFile("Z:\SourceFile.vb", Nothing)
                Assert.Throws(Of ArgumentException)(Sub() environment.Workspace.GetFileCodeModel(originalDocumentId))

                ' Add it back in
                project.AddFile("Z:\SourceFile.vb", Nothing, False)
                Dim newDocumentId = environment.Workspace.CurrentSolution.Projects.Single().DocumentIds.Single()
                Dim newFileCodeModel = environment.Workspace.GetFileCodeModel(newDocumentId)

                Assert.NotSame(originalDocumentId, newDocumentId)
                Assert.NotSame(originalFileCodeModel, newFileCodeModel)

                project.Disconnect()
            End Using
        End Sub
    End Class
End Namespace
