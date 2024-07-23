' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Collections.Immutable
Imports System.Threading
Imports Microsoft.CodeAnalysis.CodeActions
Imports Microsoft.CodeAnalysis.Editor.UnitTests.Workspaces
Imports Microsoft.CodeAnalysis.Host
Imports Microsoft.CodeAnalysis.SyncNamespaces

Namespace Microsoft.CodeAnalysis.Editor.Implementation.CodeFixes.UnitTests

    <[UseExportProvider]>
    <Trait(Traits.Feature, Traits.Features.SyncNamespaces)>
    Public Class SyncNamespacesServiceTests
        <Fact>
        Public Async Function SingleProject_MatchingNamespace_NoChanges() As Task
            Dim test =
<Workspace>
    <Project Language="C#" CommonReferences="true" FilePath="/Test/Test.csproj">
        <AnalyzerConfigDocument FilePath="/Test/.editorconfig">
is_global=true
build_property.ProjectDir = /Test/
build_property.RootNamespace = Test.Namespace
        </AnalyzerConfigDocument>
        <Document FilePath="/Test/App/Test.cs">
namespace Test.Namespace.App
{
    class Goo
    {
    }
}
</Document>
    </Project>
</Workspace>

            Using workspace = EditorTestWorkspace.Create(test)

                Dim project = workspace.CurrentSolution.Projects(0)
                Dim document = project.Documents.Single()

                Dim syncService = project.GetLanguageService(Of ISyncNamespacesService)()
                Dim newSolution = Await syncService.SyncNamespacesAsync(ImmutableArray.Create(project), CodeAnalysisProgress.None, CancellationToken.None)

                Dim solutionChanges = workspace.CurrentSolution.GetChanges(newSolution)

                Assert.Empty(solutionChanges.GetProjectChanges())
            End Using
        End Function

        <Fact>
        Public Async Function SingleProject_MismatchedNamespace_HasChanges() As Task
            Dim test =
<Workspace>
    <Project Language="C#" CommonReferences="true" FilePath="/Test/Test.csproj">
        <AnalyzerConfigDocument FilePath="/Test/.editorconfig">
is_global=true
build_property.ProjectDir = /Test/
build_property.RootNamespace = Test.Namespace
        </AnalyzerConfigDocument>
        <Document FilePath="/Test/App/Test.cs">
namespace Test
{
    class Goo
    {
    }
}
</Document>
    </Project>
</Workspace>

            Using workspace = EditorTestWorkspace.Create(test)

                Dim projects = workspace.CurrentSolution.Projects.ToImmutableArray()
                Dim project = projects(0)
                Dim document = project.Documents.Single()

                Dim syncService = project.GetLanguageService(Of ISyncNamespacesService)()
                Dim newSolution = Await syncService.SyncNamespacesAsync(projects, CodeAnalysisProgress.None, CancellationToken.None)

                Dim solutionChanges = workspace.CurrentSolution.GetChanges(newSolution)
                Dim projectChanges = solutionChanges.GetProjectChanges().Single()

                Dim changedDocumentId = projectChanges.GetChangedDocuments().Single()
                Dim changedDocument = newSolution.GetDocument(changedDocumentId)

                Dim textChanges = Await changedDocument.GetTextChangesAsync(document)
                Dim textChange = textChanges.Single()

                Assert.Equal("namespace Test.Namespace.App", textChange.NewText)
            End Using
        End Function

        <Fact>
        Public Async Function MultipleProjects_MatchingNamespaces_NoChanges() As Task
            Dim test =
<Workspace>
    <Project Language="C#" CommonReferences="true" FilePath="/Test/Test.csproj">
        <AnalyzerConfigDocument FilePath="/Test/.editorconfig">
is_global=true
build_property.ProjectDir = /Test/
build_property.RootNamespace = Test.Namespace
        </AnalyzerConfigDocument>
        <Document FilePath="/Test/App/Test.cs">
namespace Test.Namespace.App
{
    class Goo
    {
    }
}
</Document>
    </Project>
    <Project Language="C#" CommonReferences="true" FilePath="/Test2/Test2.csproj">
        <AnalyzerConfigDocument FilePath="/Test2/.editorconfig">
is_global=true
build_property.ProjectDir = /Test2/
build_property.RootNamespace = Test2.Namespace
        </AnalyzerConfigDocument>
        <Document FilePath="/Test2/App/Test2.cs">
namespace Test2.Namespace.App
{
    class Goo
    {
    }
}
</Document>
    </Project>
</Workspace>

            Using workspace = EditorTestWorkspace.Create(test)

                Dim projects = workspace.CurrentSolution.Projects.ToImmutableArray()
                Dim project = projects(0)

                Dim syncService = project.GetLanguageService(Of ISyncNamespacesService)()
                Dim newSolution = Await syncService.SyncNamespacesAsync(projects, CodeAnalysisProgress.None, CancellationToken.None)

                Dim solutionChanges = workspace.CurrentSolution.GetChanges(newSolution)

                Assert.Empty(solutionChanges.GetProjectChanges())
            End Using
        End Function

        <Fact>
        Public Async Function MultipleProjects_OneMismatchedNamespace_HasChanges() As Task
            Dim test =
<Workspace>
    <Project Language="C#" CommonReferences="true" FilePath="/Test/Test.csproj">
        <AnalyzerConfigDocument FilePath="/Test/.editorconfig">
is_global=true
build_property.ProjectDir = /Test/
build_property.RootNamespace = Test.Namespace
        </AnalyzerConfigDocument>
        <Document FilePath="/Test/App/Test.cs">
namespace Test.Namespace
{
    class Goo
    {
    }
}
</Document>
    </Project>
    <Project Language="C#" CommonReferences="true" FilePath="/Test2/Test2.csproj">
        <AnalyzerConfigDocument FilePath="/Test2/.editorconfig">
is_global=true
build_property.ProjectDir = /Test2/
build_property.RootNamespace = Test2.Namespace
        </AnalyzerConfigDocument>
        <Document FilePath="/Test2/App/Test2.cs">
namespace Test2.Namespace.App
{
    class Goo
    {
    }
}
</Document>
    </Project>
</Workspace>

            Using workspace = EditorTestWorkspace.Create(test)

                Dim projects = workspace.CurrentSolution.Projects.ToImmutableArray()
                Dim project = projects.Single(Function(proj As Project)
                                                  Return proj.FilePath = "/Test/Test.csproj"
                                              End Function)

                Dim document = project.Documents.Single()

                Dim syncService = project.GetLanguageService(Of ISyncNamespacesService)()
                Dim newSolution = Await syncService.SyncNamespacesAsync(projects, CodeAnalysisProgress.None, CancellationToken.None)

                Dim solutionChanges = workspace.CurrentSolution.GetChanges(newSolution)
                Dim projectChanges = solutionChanges.GetProjectChanges().Single()

                Dim changedDocumentId = projectChanges.GetChangedDocuments().Single()
                Dim changedDocument = newSolution.GetDocument(changedDocumentId)

                Dim textChanges = Await changedDocument.GetTextChangesAsync(document)
                Dim textChange = textChanges.Single()

                Assert.Equal("namespace Test.Namespace.App", textChange.NewText)
            End Using
        End Function

        <Fact>
        Public Async Function MultipleProjects_MultipleMismatchedNamespaces_HasChanges() As Task
            Dim test =
<Workspace>
    <Project Language="C#" CommonReferences="true" FilePath="/Test/Test.csproj">
        <AnalyzerConfigDocument FilePath="/Test/.editorconfig">
is_global=true
build_property.ProjectDir = /Test/
build_property.RootNamespace = Test.Namespace
        </AnalyzerConfigDocument>
        <Document FilePath="/Test/App/Test.cs">
namespace Test.Namespace
{
    class Goo
    {
    }
}
</Document>
    </Project>
    <Project Language="C#" CommonReferences="true" FilePath="/Test2/Test2.csproj">
        <AnalyzerConfigDocument FilePath="/Test2/.editorconfig">
is_global=true
build_property.ProjectDir = /Test2/
build_property.RootNamespace = Test2.Namespace
        </AnalyzerConfigDocument>
        <Document FilePath="/Test2/App/Test2.cs">
namespace Test2.Namespace
{
    class Goo
    {
    }
}
</Document>
    </Project>
</Workspace>

            Using workspace = EditorTestWorkspace.Create(test)

                Dim projects = workspace.CurrentSolution.Projects.ToImmutableArray()
                Dim project = projects.Single(Function(proj As Project)
                                                  Return proj.FilePath = "/Test/Test.csproj"
                                              End Function)
                Dim document = project.Documents.Single()

                Dim project2 = projects.Single(Function(proj As Project)
                                                   Return proj.FilePath = "/Test2/Test2.csproj"
                                               End Function)
                Dim document2 = project2.Documents.Single()

                Dim syncService = project.GetLanguageService(Of ISyncNamespacesService)()
                Dim newSolution = Await syncService.SyncNamespacesAsync(projects, CodeAnalysisProgress.None, CancellationToken.None)

                Dim solutionChanges = workspace.CurrentSolution.GetChanges(newSolution)
                Dim projectChanges = solutionChanges.GetProjectChanges().ToImmutableArray()

                Assert.Equal(2, projectChanges.Length)

                For Each projectChange In projectChanges
                    Dim changedDocumentId = projectChange.GetChangedDocuments().Single()
                    Dim changedDocument = newSolution.GetDocument(changedDocumentId)

                    If projectChange.ProjectId = project.Id Then
                        Dim textChanges = Await changedDocument.GetTextChangesAsync(document)
                        Dim textChange = textChanges.Single()

                        Assert.Equal("namespace Test.Namespace.App", textChange.NewText)
                    Else
                        Dim textChanges = Await changedDocument.GetTextChangesAsync(document2)
                        Dim textChange = textChanges.Single()

                        Assert.Equal("namespace Test2.Namespace.App", textChange.NewText)
                    End If
                Next
            End Using
        End Function
    End Class
End Namespace
