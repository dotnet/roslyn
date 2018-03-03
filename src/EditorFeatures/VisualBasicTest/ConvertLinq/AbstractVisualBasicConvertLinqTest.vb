' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Xml.Linq
Imports Microsoft.CodeAnalysis
Imports Microsoft.CodeAnalysis.CodeRefactorings
Imports Microsoft.CodeAnalysis.Editor.VisualBasic.UnitTests.CodeRefactorings

Public MustInherit Class AbstractVisualBasicConvertLinqTest
    Inherits BasicTestBase

    Private _codeRefactoringTestProvider As CodeRefactoringTestProvider

    Private Class CodeRefactoringTestProvider
        Inherits AbstractVisualBasicCodeActionTest

        Private _codeRefactoringProvider As CodeRefactoringProvider
        Public Sub New(codeRefactoringProvider As CodeRefactoringProvider)
            _codeRefactoringProvider = codeRefactoringProvider
        End Sub

        Protected Overrides Function CreateCodeRefactoringProvider(workspace As Workspace, parameters As TestParameters) As CodeRefactoringProvider
            ' Return New VisualBasicConvertLinqQueryToLinqMethodProvider()
            ' TODO should create a new one each time
            Return _codeRefactoringProvider
        End Function
    End Class

    Public Sub New(codeRefactoringProvider As CodeRefactoringProvider)
        _codeRefactoringTestProvider = New CodeRefactoringTestProvider(codeRefactoringProvider)
    End Sub

    Protected Async Function Test(initialSource As XElement, expectedOutput As XCData) As Task
        Dim modifiedSource As Tuple(Of Solution, Solution) = Await _codeRefactoringTestProvider.ApplyRefactoring(initialSource.Value)
        Dim newSolution = modifiedSource.Item2
        ' TODO make async
        Dim options As VisualBasicCompilationOptions = TestOptions.ReleaseExe
        Dim compilation As Compilation = CreateCompilation(
            newSolution.Projects.SelectMany(Function(project) project.Documents).Select(Function(doc) doc.GetSyntaxTreeAsync().Result),
                LatestVbReferences,
        options:=options)

        CompileAndVerify(compilation, expectedOutput:=expectedOutput)
    End Function
End Class
