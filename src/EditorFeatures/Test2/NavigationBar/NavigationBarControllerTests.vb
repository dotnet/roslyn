' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports Microsoft.CodeAnalysis.Collections
Imports Microsoft.CodeAnalysis.Editor.Implementation.NavigationBar
Imports Microsoft.CodeAnalysis.Editor.UnitTests.Workspaces
Imports Microsoft.CodeAnalysis.Shared.TestHooks
Imports Microsoft.VisualStudio.Text

Namespace Microsoft.CodeAnalysis.Editor.UnitTests.NavigationBar
    <[UseExportProvider]>
    <Trait(Traits.Feature, Traits.Features.NavigationBar)>
    Public Class NavigationBarControllerTests
        <WpfFact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/544957")>
        Public Async Function TestDoNotRecomputeAfterFullRecompute() As Task
            Using workspace = EditorTestWorkspace.Create(
                <Workspace>
                    <Project Language="C#" CommonReferences="true">
                        <Document>class C { }</Document>
                    </Project>
                </Workspace>)

                Dim document = workspace.Documents.Single()

                Dim presentItemsCalled As Boolean = False
                Dim mockPresenter As New MockNavigationBarPresenter(document.GetTextView(), Sub() presentItemsCalled = True)

                Dim controllerFactory = workspace.GetService(Of INavigationBarControllerFactoryService)()
                Dim controller = controllerFactory.CreateController(mockPresenter, document.GetTextBuffer())

                Dim listenerProvider = workspace.ExportProvider.GetExport(Of IAsynchronousOperationListenerProvider).Value
                Dim workspaceWaiter = listenerProvider.GetWaiter(FeatureAttribute.Workspace)
                Dim navbarWaiter = listenerProvider.GetWaiter(FeatureAttribute.NavigationBar)

                Await navbarWaiter.ExpeditedWaitAsync()

                ' The first time this is called, we should get various calls as a part of the
                ' present
                Assert.True(presentItemsCalled)

                ' After it, we should not get any calls
                presentItemsCalled = False

                Await navbarWaiter.ExpeditedWaitAsync()
                Assert.False(presentItemsCalled, "The presenter should not have been called a second time.")
            End Using
        End Function

        <WpfFact(Skip:="https://github.com/dotnet/roslyn/issues/24754"), WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/544957")>
        Public Async Function ProjectionBuffersWork() As Task
            Using workspace = EditorTestWorkspace.Create(
                <Workspace>
                    <Project Language="C#" CommonReferences="true">
                        <Document>{|Document:class C { $$ }|}</Document>
                    </Project>
                </Workspace>, composition:=EditorTestCompositions.EditorFeatures)

                Dim subjectDocument = workspace.Documents.Single()
                Dim projectedDocument = workspace.CreateProjectionBufferDocument("LEADING TEXT {|Document:|} TRAILING TEXT", {subjectDocument})
                Dim view = projectedDocument.GetTextView()
                view.Caret.MoveTo(New SnapshotPoint(view.TextSnapshot, projectedDocument.CursorPosition.Value))

                Dim presentItemsCalled As Boolean = False
                Dim mockPresenter As New MockNavigationBarPresenter(view, Sub() presentItemsCalled = True)

                ' The first time this is called, we should get various calls as a part of the
                ' present

                Dim controllerFactory = workspace.GetService(Of INavigationBarControllerFactoryService)()
                Dim controller = controllerFactory.CreateController(mockPresenter, subjectDocument.GetTextBuffer())

                Dim provider = workspace.ExportProvider.GetExportedValue(Of IAsynchronousOperationListenerProvider)
                Await provider.WaitAllDispatcherOperationAndTasksAsync(workspace, FeatureAttribute.Workspace, FeatureAttribute.NavigationBar)

                Assert.True(presentItemsCalled)
            End Using
        End Function

        <WpfFact>
        Public Async Function TestNavigationBarInCSharpLinkedFiles() As Task
            Using workspace = EditorTestWorkspace.Create(
                <Workspace>
                    <Project Language="C#" CommonReferences="true" AssemblyName="CSProj" PreprocessorSymbols="Proj1">
                        <Document FilePath="C.cs">
class C
{
#if Proj1
    void M1(int x) { }
#endif
#if Proj2
    void M2(int x) { }
#endif
}
                              </Document>
                    </Project>
                    <Project Language="C#" CommonReferences="true" PreprocessorSymbols="Proj2">
                        <Document IsLinkFile="true" LinkAssemblyName="CSProj" LinkFilePath="C.cs"/>
                    </Project>
                </Workspace>, composition:=EditorTestCompositions.EditorFeatures)

                Dim baseDocument = workspace.Documents.Single(Function(d) Not d.IsLinkFile)
                Dim linkDocument = workspace.Documents.Single(Function(d) d.IsLinkFile)

                Dim presentItemsCalled As Boolean = False
                Dim memberName As String = Nothing
                Dim projectGlyph As Glyph = Nothing

                Dim mockPresenter As New MockNavigationBarPresenter(
                    baseDocument.GetTextView(),
                    Sub(projects, selectedProject, typesWithMembers, selectedType, selectedMember)
                        memberName = typesWithMembers.Single().ChildItems.Single().Text
                        projectGlyph = selectedProject.Glyph
                    End Sub)

                Dim controllerFactory = workspace.GetService(Of INavigationBarControllerFactoryService)()
                Dim controller = controllerFactory.CreateController(mockPresenter, baseDocument.GetTextBuffer())

                Dim listenerProvider = workspace.ExportProvider.GetExport(Of IAsynchronousOperationListenerProvider).Value
                Dim workspaceWaiter = listenerProvider.GetWaiter(FeatureAttribute.Workspace)
                Dim navbarWaiter = listenerProvider.GetWaiter(FeatureAttribute.NavigationBar)

                Await navbarWaiter.ExpeditedWaitAsync()

                Assert.Equal("M1(int x)", memberName)
                Assert.Equal(projectGlyph, Glyph.CSharpProject)

                workspace.SetDocumentContext(linkDocument.Id)

                Await workspaceWaiter.ExpeditedWaitAsync()
                Await navbarWaiter.ExpeditedWaitAsync()

                Assert.Equal("M2(int x)", memberName)
                Assert.Equal(projectGlyph, Glyph.CSharpProject)
            End Using
        End Function

        <WpfFact>
        Public Async Function TestNavigationBarInVisualBasicLinkedFiles() As Task
            Using workspace = EditorTestWorkspace.Create(
                <Workspace>
                    <Project Language="Visual Basic" CommonReferences="true" AssemblyName="VBProj" PreprocessorSymbols="Proj1=True">
                        <Document FilePath="C.vb">
Class C
#If Proj1 Then
    Sub M1()
    End Sub
#End If
#If Proj2 Then
    Sub M2()
    End Sub
#End If
End Class
                              </Document>
                    </Project>
                    <Project Language="Visual Basic" CommonReferences="true" PreprocessorSymbols="Proj2=True">
                        <Document IsLinkFile="true" LinkAssemblyName="VBProj" LinkFilePath="C.vb"/>
                    </Project>
                </Workspace>, composition:=EditorTestCompositions.EditorFeatures)

                Dim baseDocument = workspace.Documents.Single(Function(d) Not d.IsLinkFile)
                Dim linkDocument = workspace.Documents.Single(Function(d) d.IsLinkFile)

                Dim presentItemsCalled As Boolean = False
                Dim memberNames As IEnumerable(Of String) = Nothing
                Dim projectGlyph As Glyph = Nothing

                Dim mockPresenter As New MockNavigationBarPresenter(
                    baseDocument.GetTextView(),
                    Sub(projects, selectedProject, typesWithMembers, selectedType, selectedMember)
                        memberNames = typesWithMembers.Single().ChildItems.Select(Function(c) c.Text)
                        projectGlyph = selectedProject.Glyph
                    End Sub)

                Dim controllerFactory = workspace.GetService(Of INavigationBarControllerFactoryService)()
                Dim controller = controllerFactory.CreateController(mockPresenter, baseDocument.GetTextBuffer())

                Dim listenerProvider = workspace.ExportProvider.GetExport(Of IAsynchronousOperationListenerProvider).Value
                Dim workspaceWaiter = listenerProvider.GetWaiter(FeatureAttribute.Workspace)
                Dim navbarWaiter = listenerProvider.GetWaiter(FeatureAttribute.NavigationBar)

                Await navbarWaiter.ExpeditedWaitAsync()

                Assert.Contains("M1", memberNames)
                Assert.DoesNotContain("M2", memberNames)
                Assert.Equal(projectGlyph, Glyph.BasicProject)

                workspace.SetDocumentContext(linkDocument.Id)

                Await workspaceWaiter.ExpeditedWaitAsync()
                Await navbarWaiter.ExpeditedWaitAsync()

                Assert.Contains("M2", memberNames)
                Assert.DoesNotContain("M1", memberNames)
                Assert.Equal(projectGlyph, Glyph.BasicProject)
            End Using
        End Function

        <WpfFact>
        Public Async Function TestProjectItemsAreSortedCSharp() As Task
            Using workspace = EditorTestWorkspace.Create(
                <Workspace>
                    <Project Language="C#" CommonReferences="true" AssemblyName="BProj">
                        <Document FilePath="C.cs">
class C
{
}
                              </Document>
                    </Project>
                    <Project Language="C#" CommonReferences="true" AssemblyName="AProj">
                        <Document IsLinkFile="true" LinkAssemblyName="BProj" LinkFilePath="C.cs"/>
                    </Project>
                    <Project Language="C#" CommonReferences="true" AssemblyName="CProj">
                        <Document IsLinkFile="true" LinkAssemblyName="BProj" LinkFilePath="C.cs"/>
                    </Project>
                </Workspace>, composition:=EditorTestCompositions.EditorFeatures)

                Dim baseDocument = workspace.Documents.Single(Function(d) Not d.IsLinkFile)
                Dim expectedProjectNames As New List(Of String) From {"AProj", "BProj", "CProj"}
                Dim actualProjectNames As List(Of String) = Nothing

                Dim mockPresenter As New MockNavigationBarPresenter(
                    baseDocument.GetTextView(),
                    Sub(projects, selectedProject, typesWithMembers, selectedType, selectedMember)
                        actualProjectNames = projects.Select(Function(item) item.Text).ToList()
                    End Sub)

                Dim controllerFactory = workspace.GetService(Of INavigationBarControllerFactoryService)()
                Dim controller = controllerFactory.CreateController(mockPresenter, baseDocument.GetTextBuffer())
                Await workspace.ExportProvider.GetExportedValue(Of IAsynchronousOperationListenerProvider).GetWaiter(FeatureAttribute.NavigationBar).ExpeditedWaitAsync()

                Assert.True(actualProjectNames.SequenceEqual(expectedProjectNames))
            End Using
        End Function

        <WpfFact>
        Public Async Function TestProjectItemsAreSortedVisualBasic() As Task
            Using workspace = EditorTestWorkspace.Create(
                <Workspace>
                    <Project Language="Visual Basic" CommonReferences="true" AssemblyName="VBProj">
                        <Document FilePath="C.vb">
Class C
End Class
                              </Document>
                    </Project>
                    <Project Language="Visual Basic" CommonReferences="true" AssemblyName="VB-Proj1">
                        <Document IsLinkFile="true" LinkAssemblyName="VBProj" LinkFilePath="C.vb"/>
                    </Project>
                </Workspace>, composition:=EditorTestCompositions.EditorFeatures)

                Dim baseDocument = workspace.Documents.Single(Function(d) Not d.IsLinkFile)
                Dim expectedProjectNames As New List(Of String) From {"VBProj", "VB-Proj1"}
                Dim actualProjectNames As List(Of String) = Nothing

                Dim mockPresenter As New MockNavigationBarPresenter(
                    baseDocument.GetTextView(),
                    Sub(projects, selectedProject, typesWithMembers, selectedType, selectedMember)
                        actualProjectNames = projects.Select(Function(item) item.Text).ToList()
                    End Sub)

                Dim controllerFactory = workspace.GetService(Of INavigationBarControllerFactoryService)()
                Dim controller = controllerFactory.CreateController(mockPresenter, baseDocument.GetTextBuffer())
                Await workspace.ExportProvider.GetExportedValue(Of IAsynchronousOperationListenerProvider).GetWaiter(FeatureAttribute.NavigationBar).ExpeditedWaitAsync()

                Assert.True(actualProjectNames.SequenceEqual(expectedProjectNames))
            End Using
        End Function

        <WpfFact>
        Public Async Function TestNavigationBarRefreshesAfterProjectRename() As Task
            Using workspace = EditorTestWorkspace.Create(
                <Workspace>
                    <Project Language="Visual Basic" CommonReferences="true" AssemblyName="VBProj">
                        <Document FilePath="C.vb">
Class C
    $$
End Class
                        </Document>
                    </Project>
                </Workspace>, composition:=EditorTestCompositions.EditorFeatures)

                Dim document = workspace.Documents.Single()

                Dim projectName As String = Nothing

                Dim mockPresenter As New MockNavigationBarPresenter(
                    document.GetTextView(),
                    Sub(projects, selectedProject, typesWithMembers, selectedType, selectedMember)
                        projectName = selectedProject?.Text
                    End Sub)

                Dim controllerFactory = workspace.GetService(Of INavigationBarControllerFactoryService)()
                Dim controller = controllerFactory.CreateController(mockPresenter, document.GetTextBuffer())
                Await workspace.ExportProvider.GetExportedValue(Of IAsynchronousOperationListenerProvider).GetWaiter(FeatureAttribute.NavigationBar).ExpeditedWaitAsync()

                Assert.Equal("VBProj", projectName)

                workspace.OnProjectNameChanged(workspace.Projects.Single().Id, "VBProj2", "VBProj2.vbproj")

                Dim listenerProvider = workspace.ExportProvider.GetExportedValue(Of AsynchronousOperationListenerProvider)()
                Dim workspaceWaiter = listenerProvider.GetWaiter(FeatureAttribute.Workspace)
                Dim navigationBarWaiter = listenerProvider.GetWaiter(FeatureAttribute.NavigationBar)

                Await workspaceWaiter.ExpeditedWaitAsync()
                Await navigationBarWaiter.ExpeditedWaitAsync()

                Await listenerProvider.WaitAllDispatcherOperationAndTasksAsync(workspace, FeatureAttribute.Workspace, FeatureAttribute.NavigationBar)

                Assert.Equal("VBProj2", projectName)
            End Using
        End Function

        <WpfFact, WorkItem("https://devdiv.visualstudio.com/DevDiv/_workitems/edit/1116665")>
        Public Async Function TestNoCompilationLanguage() As Task

            Dim composition = EditorTestCompositions.EditorFeatures.AddParts(
                GetType(NoCompilationContentTypeDefinitions),
                GetType(NoCompilationContentTypeLanguageService))

            Using workspace = EditorTestWorkspace.Create(
                <Workspace>
                    <Project Language="NoCompilation" CommonReferences="true" AssemblyName="Test">
                        <Document FilePath="C.js">
                        </Document>
                    </Project>
                </Workspace>, composition:=composition)

                Dim document = workspace.Documents.Single()

                Dim mockPresenter As New MockNavigationBarPresenter(
                    document.GetTextView(),
                    Sub(projects, selectedProject, typesWithMembers, selectedType, selectedMember)
                    End Sub)

                Dim controllerFactory = workspace.GetService(Of INavigationBarControllerFactoryService)()
                Dim controller = DirectCast(controllerFactory.CreateController(mockPresenter, document.GetTextBuffer()), NavigationBarController)
                Dim accessor = controller.GetTestAccessor()

                ' Ensure we don't crash computing the model for a language that isn't available.
                Await accessor.GetModelAsync()
            End Using
        End Function
    End Class
End Namespace
