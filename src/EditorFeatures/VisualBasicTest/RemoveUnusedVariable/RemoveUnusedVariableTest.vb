' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports Microsoft.CodeAnalysis.CodeFixes
Imports Microsoft.CodeAnalysis.Diagnostics
Imports Microsoft.CodeAnalysis.Editor.VisualBasic.UnitTests.Diagnostics
Imports Microsoft.CodeAnalysis.VisualBasic.RemoveUnusedVariable

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.UnitTests.RemoveUnusedVariable
    Partial Public Class RemoveUnusedVariableTest
        Inherits AbstractVisualBasicDiagnosticProviderBasedUserDiagnosticTest

        Friend Overrides Function CreateDiagnosticProviderAndFixer(workspace As Workspace) As (DiagnosticAnalyzer, CodeFixProvider)
            Return (Nothing, New VisualBasicRemoveUnusedVariableCodeFixProvider())
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnusedVariable)>
        Public Async Function RemoveUnusedVariable() As Task
            Dim markup =
<File>
Module M
    Sub Main()
        Dim [|x as String|]
    End Sub
End Module
</File>
            Dim expected =
<File>
Module M
    Sub Main()
    End Sub
End Module
</File>

            Await TestAsync(markup, expected)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnusedVariable)>
        Public Async Function RemoveUnusedVariable1() As Task
            Dim markup =
<File>
Module M
    Sub Main()
        Dim [|x|], c as String
    End Sub
End Module
</File>
            Dim expected =
<File>
Module M
    Sub Main()
        Dim c as String
    End Sub
End Module
</File>

            Await TestAsync(markup, expected)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsRemoveUnusedVariable)>
        Public Async Function RemoveUnusedVariableFixAll() As Task
            Dim markup =
<File>
Module M
    Sub Main()
        Dim x, c as String
        Dim {|FixAllInDocument:a as String|}
    End Sub
End Module
</File>
            Dim expected =
<File>
Module M
    Sub Main()
    End Sub
End Module
</File>

            Await TestAsync(markup, expected)
        End Function
    End Class
End Namespace
