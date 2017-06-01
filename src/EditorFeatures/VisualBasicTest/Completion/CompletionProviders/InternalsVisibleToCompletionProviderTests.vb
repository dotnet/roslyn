' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System
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
    End Class
End Namespace