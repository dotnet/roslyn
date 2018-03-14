' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.


Imports Microsoft.CodeAnalysis.CodeRefactorings
Imports Microsoft.CodeAnalysis.Editor.UnitTests.CodeActions
Imports Microsoft.CodeAnalysis.Editor.VisualBasic.UnitTests.CodeRefactorings
Imports Microsoft.CodeAnalysis.VisualBasic.ForeachToFor

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.UnitTests.ForeachToFor
    Partial Public Class ForeachToForTests
        Inherits AbstractVisualBasicCodeActionTest

        Protected Overrides Function CreateCodeRefactoringProvider(workspace As Workspace, parameters As TestParameters) As CodeRefactoringProvider
            Return New VisualBasicForEachToForCodeRefactoringProvider()
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.ForeachToFor)>
        Public Async Function EmptyBlockBody() As Task
            Dim initial = "
Class Test
    Sub Method()
        Dim array = New Integer() {1, 2, 3}
        For Each [||] a In array
        Next
    End Sub
End Class
"

            Dim expected = "
Class Test
    Sub Method()
        Dim array = New Integer() {1, 2, 3}
        For {|Rename:i|} = 0 To array.Length - 1
        Next
    End Sub
End Class
"
            Await TestInRegularAndScriptAsync(initial, expected)
        End Function
    End Class
End Namespace
