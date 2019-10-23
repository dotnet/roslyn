' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Threading.Tasks
Imports Microsoft.CodeAnalysis.Editor.UnitTests.Workspaces
Imports Microsoft.CodeAnalysis.Shared.TestHooks
Imports Microsoft.VisualStudio.Composition
Imports Microsoft.VisualStudio.Text

Namespace Microsoft.CodeAnalysis.Editor.UnitTests.NavigationBar
    <[UseExportProvider]>
    Public Class NavigationBarControllerTests
        <WpfFact, Trait(Traits.Feature, Traits.Features.NavigationBar), WorkItem(544957, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/544957")>
        Public Sub TestDoNotRecomputeAfterFullRecompute()
            Using workspace = TestWorkspace.Create(
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

                ' The first time this is called, we should get various calls as a part of the
                ' present

                presentItemsCalled = False
                mockPresenter.RaiseDropDownFocused()

                Assert.True(presentItemsCalled)

                ' After it, we should not get any calls
                presentItemsCalled = False
                mockPresenter.RaiseDropDownFocused()

                Assert.False(presentItemsCalled, "The presenter should not have been called a second time.")
            End Using
        End Sub

        <WpfFact(Skip:="https://github.com/dotnet/roslyn/issues/24754"), Trait(Traits.Feature, Traits.Features.NavigationBar), WorkItem(544957, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/544957")>
        Public Async Function ProjectionBuffersWork() As Task
            Using workspace = TestWorkspace.Create(
                <Workspace>
                    <Project Language="C#" CommonReferences="true">
                        <Document>{|Document:class C { $$ }|}</Document>
                    </Project>
                </Workspace>, exportProvider:=TestExportProvider.ExportProviderWithCSharpAndVisualBasic)

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
                Await provider.WaitAllDispatcherOperationAndTasksAsync(FeatureAttribute.Workspace, FeatureAttribute.NavigationBar)

                Assert.True(presentItemsCalled)
            End Using
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.NavigationBar)>
        Public Sub TestNavigationBarInCSharpLinkedFiles()
            Using workspace = TestWorkspace.Create(
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
                </Workspace>, exportProvider:=TestExportProvider.ExportProviderWithCSharpAndVisualBasic)

                Dim baseDocument = workspace.Documents.Single(Function(d) Not d.IsLinkFile)
                Dim linkDocument = workspace.Documents.Single(Function(d) d.IsLinkFile)

                Dim presentItemsCalled As Boolean = False
                Dim memberName As String = Nothing
                Dim projectGlyph As Glyph = Nothing

                Dim mockPresenter As New MockNavigationBarPresenter(
                    baseDocument.GetTextView(),
                    Sub(projects As IList(Of NavigationBarProjectItem),
                            selectedProject As NavigationBarProjectItem,
                            typesWithMembers As IList(Of NavigationBarItem),
                            selectedType As NavigationBarItem,
                            selectedMember As NavigationBarItem)
                        memberName = typesWithMembers.Single().ChildItems.Single().Text
                        projectGlyph = selectedProject.Glyph
                    End Sub)

                Dim controllerFactory = workspace.GetService(Of INavigationBarControllerFactoryService)()
                Dim controller = controllerFactory.CreateController(mockPresenter, baseDocument.GetTextBuffer())

                memberName = Nothing
                mockPresenter.RaiseDropDownFocused()
                Assert.Equal("M1(int x)", memberName)
                Assert.Equal(projectGlyph, Glyph.CSharpProject)

                workspace.SetDocumentContext(linkDocument.Id)

                memberName = Nothing
                mockPresenter.RaiseDropDownFocused()
                Assert.Equal("M2(int x)", memberName)
                Assert.Equal(projectGlyph, Glyph.CSharpProject)
            End Using
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.NavigationBar)>
        Public Sub TestNavigationBarInVisualBasicLinkedFiles()
            Using workspace = TestWorkspace.Create(
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
                </Workspace>, exportProvider:=TestExportProvider.ExportProviderWithCSharpAndVisualBasic)

                Dim baseDocument = workspace.Documents.Single(Function(d) Not d.IsLinkFile)
                Dim linkDocument = workspace.Documents.Single(Function(d) d.IsLinkFile)

                Dim presentItemsCalled As Boolean = False
                Dim memberNames As IEnumerable(Of String) = Nothing
                Dim projectGlyph As Glyph = Nothing

                Dim mockPresenter As New MockNavigationBarPresenter(
                    baseDocument.GetTextView(),
                    Sub(projects As IList(Of NavigationBarProjectItem),
                            selectedProject As NavigationBarProjectItem,
                            typesWithMembers As IList(Of NavigationBarItem),
                            selectedType As NavigationBarItem,
                            selectedMember As NavigationBarItem)
                        memberNames = typesWithMembers.Single().ChildItems.Select(Function(c) c.Text)
                        projectGlyph = selectedProject.Glyph
                    End Sub)

                Dim controllerFactory = workspace.GetService(Of INavigationBarControllerFactoryService)()
                Dim controller = controllerFactory.CreateController(mockPresenter, baseDocument.GetTextBuffer())

                memberNames = Nothing
                mockPresenter.RaiseDropDownFocused()
                Assert.Contains("M1", memberNames)
                Assert.DoesNotContain("M2", memberNames)
                Assert.Equal(projectGlyph, Glyph.BasicProject)

                workspace.SetDocumentContext(linkDocument.Id)

                memberNames = Nothing
                mockPresenter.RaiseDropDownFocused()
                Assert.Contains("M2", memberNames)
                Assert.DoesNotContain("M1", memberNames)
                Assert.Equal(projectGlyph, Glyph.BasicProject)
            End Using
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.NavigationBar)>
        Public Sub TestProjectItemsAreSortedCSharp()
            Using workspace = TestWorkspace.Create(
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
                </Workspace>, exportProvider:=TestExportProvider.ExportProviderWithCSharpAndVisualBasic)

                Dim baseDocument = workspace.Documents.Single(Function(d) Not d.IsLinkFile)
                Dim expectedProjectNames As New List(Of String) From {"AProj", "BProj", "CProj"}
                Dim actualProjectNames As List(Of String) = Nothing

                Dim mockPresenter As New MockNavigationBarPresenter(
                    baseDocument.GetTextView(),
                    Sub(projects As IList(Of NavigationBarProjectItem),
                            selectedProject As NavigationBarProjectItem,
                            typesWithMembers As IList(Of NavigationBarItem),
                            selectedType As NavigationBarItem,
                            selectedMember As NavigationBarItem)
                        actualProjectNames = projects.Select(Function(item)
                                                                 Return item.Text
                                                             End Function).ToList()
                    End Sub)

                Dim controllerFactory = workspace.GetService(Of INavigationBarControllerFactoryService)()
                Dim controller = controllerFactory.CreateController(mockPresenter, baseDocument.GetTextBuffer())

                mockPresenter.RaiseDropDownFocused()
                Assert.True(actualProjectNames.SequenceEqual(expectedProjectNames))
            End Using
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.NavigationBar)>
        Public Sub TestProjectItemsAreSortedVisualBasic()
            Using workspace = TestWorkspace.Create(
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
                </Workspace>, exportProvider:=TestExportProvider.ExportProviderWithCSharpAndVisualBasic)

                Dim baseDocument = workspace.Documents.Single(Function(d) Not d.IsLinkFile)
                Dim expectedProjectNames As New List(Of String) From {"VBProj", "VB-Proj1"}
                Dim actualProjectNames As List(Of String) = Nothing

                Dim mockPresenter As New MockNavigationBarPresenter(
                    baseDocument.GetTextView(),
                    Sub(projects As IList(Of NavigationBarProjectItem),
                            selectedProject As NavigationBarProjectItem,
                            typesWithMembers As IList(Of NavigationBarItem),
                            selectedType As NavigationBarItem,
                            selectedMember As NavigationBarItem)
                        actualProjectNames = projects.Select(Function(item)
                                                                 Return item.Text
                                                             End Function).ToList()
                    End Sub)

                Dim controllerFactory = workspace.GetService(Of INavigationBarControllerFactoryService)()
                Dim controller = controllerFactory.CreateController(mockPresenter, baseDocument.GetTextBuffer())

                mockPresenter.RaiseDropDownFocused()
                Assert.True(actualProjectNames.SequenceEqual(expectedProjectNames))
            End Using
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.NavigationBar)>
        Public Async Function TestNavigationBarRefreshesAfterProjectRename() As Task
            Using workspace = TestWorkspace.Create(
                <Workspace>
                    <Project Language="Visual Basic" CommonReferences="true" AssemblyName="VBProj">
                        <Document FilePath="C.vb">
Class C
    $$
End Class
                        </Document>
                    </Project>
                </Workspace>, exportProvider:=TestExportProvider.ExportProviderWithCSharpAndVisualBasic)

                Dim document = workspace.Documents.Single()

                Dim projectName As String = Nothing

                Dim mockPresenter As New MockNavigationBarPresenter(
                    document.GetTextView(),
                    Sub(projects As IList(Of NavigationBarProjectItem),
                            selectedProject As NavigationBarProjectItem,
                            typesWithMembers As IList(Of NavigationBarItem),
                            selectedType As NavigationBarItem,
                            selectedMember As NavigationBarItem)
                        projectName = If(selectedProject IsNot Nothing, selectedProject.Text, Nothing)
                    End Sub)

                Dim controllerFactory = workspace.GetService(Of INavigationBarControllerFactoryService)()
                Dim controller = controllerFactory.CreateController(mockPresenter, document.GetTextBuffer())

                mockPresenter.RaiseDropDownFocused()
                Assert.Equal("VBProj", projectName)

                workspace.OnProjectNameChanged(workspace.Projects.Single().Id, "VBProj2", "VBProj2.vbproj")

                Dim listenerProvider = workspace.ExportProvider.GetExportedValue(Of AsynchronousOperationListenerProvider)()
                Dim workspaceWaiter = listenerProvider.GetWaiter(FeatureAttribute.Workspace)
                Dim navigationBarWaiter = listenerProvider.GetWaiter(FeatureAttribute.NavigationBar)

                Await workspaceWaiter.CreateExpeditedWaitTask()
                Await navigationBarWaiter.CreateExpeditedWaitTask()

                Await listenerProvider.WaitAllDispatcherOperationAndTasksAsync(FeatureAttribute.Workspace, FeatureAttribute.NavigationBar)

                Assert.Equal("VBProj2", projectName)
            End Using
        End Function
    End Class
End Namespace
