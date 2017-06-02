' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports Microsoft.CodeAnalysis.Completion
Imports Microsoft.CodeAnalysis.Editor.UnitTests.Workspaces
Imports Microsoft.CodeAnalysis.VisualBasic.Completion.Providers

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.UnitTests.Completion.CompletionProviders
    Public Class InternalsVisibleToCompletionProviderTests
        Inherits AbstractVisualBasicCompletionProviderTests

        Public Sub New(workspaceFixture As VisualBasicTestWorkspaceFixture)
            MyBase.New(workspaceFixture)
            Dim ws = workspaceFixture.GetWorkspace()
            Dim solution = ws.CurrentSolution
            Dim projectInfo1 = ProjectInfo.Create(ProjectId.CreateNewId(), VersionStamp.Create(), "ClassLibrary1", "ClassLibrary1", LanguageNames.CSharp)
            solution = solution.AddProject(projectInfo1)
            ws.ChangeSolution(solution)
        End Sub

        Friend Overrides Function CreateCompletionProvider() As CompletionProvider
            Return New InternalsVisibleToCompletionProvider()
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function CodeCompletionContainsOtherAssembliesOfSolutionAsync() As Task
            Dim text = "<Assembly:System.Runtime.CompilerServices.InternalsVisibleTo(""$$"")>"
            Await VerifyItemExistsAsync(text, "ClassLibrary1")
        End Function

        <Theory, Trait(Traits.Feature, Traits.Features.Completion)>
        <InlineData("<Assembly: InternalsVisibleToAttribute(""$$"")>", True)>
        <InlineData("<Assembly: InternalsVisibleTo(""$$"")>", True)>
        <InlineData("<Assembly: InternalsVisibleTo(""$$)>", True)>
        <InlineData("<Assembly: InternalsVisibleTo(""$$", True)>
        <InlineData("<Assembly: InternalsVisibleTo(""Test$$)>", True)>
        <InlineData("<Assembly: InternalsVisibleTo(""Test"", ""$$", True)>
        <InlineData("<Assembly: InternalsVisibleTo(""Test""$$", False)>
        <InlineData("<Assembly: InternalsVisibleTo(""""$$", False)>
        <InlineData("<Assembly: InternalsVisibleTo($$)>", False)>
        <InlineData("<Assembly: InternalsVisibleTo($$", False)>
        <InlineData("<Assembly: InternalsVisibleTo$$", False)>
        <InlineData("<Assembly: InternalsVisibleTo(""$$, AllInternalsVisible := True)>", True)>
        <InlineData("<Assembly: InternalsVisibleTo(""$$"", AllInternalsVisible := True)>", True)>
        <InlineData("<Assembly: AssemblyVersion(""$$"")>", False)>
        <InlineData("<Assembly: AssemblyVersion(""$$", False)>
        <InlineData("
            <Assembly: AssemblyVersion(""1.0.0.0"")> 
            <Assembly: InternalsVisibleTo(""$$", True)>
        <InlineData("
            <Assembly: InternalsVisibleTo(""$$
            <Assembly: AssemblyVersion(""1.0.0.0"")>
            <Assembly: AssemblyCompany(""Test"")>", True)>
        <InlineData("
            <Assembly: InternalsVisibleTo(""$$
            Namespace A
                Public Class A
                End Class
            End Namespace", True)>
        Public Async Function CodeCompletionListHasItems(code As String, hasItems As Boolean) As Task
            code = "Imports System.Runtime.CompilerServices
                    Imports System.Reflection
                   " + code
            If hasItems Then
                Await VerifyAnyItemExistsAsync(code)
            Else
                Await VerifyNoItemsExistAsync(code)
            End If
        End Function
    End Class
End Namespace