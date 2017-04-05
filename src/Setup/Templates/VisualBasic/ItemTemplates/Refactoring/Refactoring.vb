Imports System.Composition
Imports System.Threading
Imports Microsoft.CodeAnalysis
Imports Microsoft.CodeAnalysis.CodeActions
Imports Microsoft.CodeAnalysis.CodeRefactorings
Imports Microsoft.CodeAnalysis.Rename
Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.VisualBasic
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

<ExportCodeRefactoringProvider(LanguageNames.VisualBasic, Name:=NameOf($safeitemname$)), [Shared]>
Friend Class $safeitemname$
    Inherits CodeRefactoringProvider

    Public NotOverridable Overrides Function ComputeRefactoringsAsync(context As CodeRefactoringContext) As Task
        Throw New NotImplementedException()
    End Function
End Class