Imports Microsoft.CodeAnalysis
Imports Microsoft.CodeAnalysis.CodeRefactorings
Imports Microsoft.CodeAnalysis.Editor.UnitTests.Workspaces
Imports Microsoft.CodeAnalysis.Editor.VisualBasic.UnitTests.CodeRefactorings
Imports Microsoft.CodeAnalysis.VisualBasic.InlineMethod

<Trait(Traits.Feature, Traits.Features.CodeActionsInlineMethod)>
Public Class VisualBasicInlineMethodTests
    Inherits AbstractVisualBasicCodeActionTest

    Protected Overrides Function CreateCodeRefactoringProvider(workspace As Workspace, parameters As TestParameters) As CodeRefactoringProvider
        Dim testWorkspace = DirectCast(workspace, TestWorkspace)
        Return testWorkspace.ExportProvider.GetExportedValue(Of VisualBasicInlineMethodRefactoringProvider)
    End Function

    <Fact>
    Public Function TestInlineSingleStatement() As Task
        Return TestInRegularAndScript1Async("
Public Class TestClass
    Public Sub Caller(i As Int32)
        Me.Ca[||]llee(i)
    End Sub

    Private Sub Callee(i As Int32)
        System.Console.WriteLine(i)
    End Sub
End Class
", "
Public Class TestClass
    Public Sub Caller(i As Int32)
        System.Console.WriteLine(i)
    End Sub

    Private Sub Callee(i As Int32)
        System.Console.WriteLine(i)
    End Sub
End Class
")
    End Function
End Class
