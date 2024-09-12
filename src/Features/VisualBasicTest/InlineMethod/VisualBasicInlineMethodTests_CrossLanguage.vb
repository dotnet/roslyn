' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports Microsoft.CodeAnalysis.CodeRefactorings
Imports Microsoft.CodeAnalysis.Editor.UnitTests.Workspaces
Imports Microsoft.CodeAnalysis.Editor.VisualBasic.UnitTests.CodeRefactorings
Imports Microsoft.CodeAnalysis.VisualBasic.CodeRefactorings.InlineTemporary

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.UnitTests.InlineMethod
    Public Class VisualBasicInlineMethodTests_CrossLanguage
        Inherits AbstractVisualBasicCodeActionTest

        Protected Overrides Function CreateCodeRefactoringProvider(workspace As Workspace, parameters As TestParameters) As CodeRefactoringProvider
            Dim testWorkspace = DirectCast(workspace, TestWorkspace)
            Return testWorkspace.ExportProvider.GetExportedValue(Of VisualBasicInlineMethodRefactoringProvider)
        End Function

        Private Async Function TestNoActionIsProvided(initialMarkup As String) As Task
            Dim workspace = CreateWorkspaceFromOptions(initialMarkup)
            Dim actions = Await GetCodeActionsAsync(workspace, Nothing).ConfigureAwait(False)
            Assert.True(actions.Item1.IsEmpty())
        End Function

        ' Because this issue: https://github.com/dotnet/roslyn-sdk/issues/464
        ' it is hard to test cross language scenario.
        ' After it is resolved then this test should be merged to the other test class
        <Fact>
        Public Async Function TestCrossLanguageInline() As Task
            Dim input = "
    <Workspace>
        <Project Language=""C#"" AssemblyName=""CSAssembly"" CommonReferences=""true"">
        <Document>
            public class TestClass
            {
                private void Callee()
                {
                }
            }
        </Document>
        </Project>
        <Project Language=""Visual Basic"" AssemblyName=""VBAssembly"" CommonReferences=""true"">
        <ProjectReference>CSAssembly</ProjectReference>
        <Document>
            Public Class VBClass
                Private Sub Caller()
                    Dim x = new TestClass()
                    x.Cal[||]lee()
                End Sub
            End Class
        </Document>
        </Project>
    </Workspace>"
            Await TestNoActionIsProvided(input).ConfigureAwait(False)
        End Function
    End Class
End Namespace
