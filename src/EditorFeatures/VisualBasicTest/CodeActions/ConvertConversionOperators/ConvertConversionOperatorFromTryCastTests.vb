' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports Microsoft.CodeAnalysis.CodeRefactorings
Imports Microsoft.CodeAnalysis.VisualBasic.CodeRefactorings.ConvertConversionOperators

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.UnitTests.CodeRefactorings.ConvertConversionOperators
    <Trait(Traits.Feature, Traits.Features.ConvertConversionOperators)>
    Public Class ConvertConversionOperatorFromTryCastTests
        Inherits AbstractVisualBasicCodeActionTest

        Protected Overrides Function CreateCodeRefactoringProvider(workspace As Workspace, parameters As TestParameters) As CodeRefactoringProvider
            Return New VisualBasicConvertConversionOperatorFromTryCastCodeRefactoringProvider()
        End Function

        <Fact>
        Public Async Function ConvertFromTryCastToCType() As Task
            Dim markup =
<File>
Module Program
    Sub M()
        Dim x = TryCast(1[||], Object)
    End Sub
End Module
</File>

            Dim expected =
<File>
Module Program
    Sub M()
        Dim x = CType(1, Object)
    End Sub
End Module
</File>

            Await TestAsync(markup, expected)
        End Function

        <Theory>
        <InlineData("TryCast(TryCast(1, [||]object), C)",
                    "TryCast(CType(1, object), C)")>
        <InlineData("TryCast(TryCast(1, object), [||]C)",
                    "CType(TryCast(1, object), C)")>
        Public Async Function ConvertFromTryCastNested(cTypeExpression As String, converted As String) As Task
            Dim markup =
<File>
Public Class C
End Class

Module Program
    Sub M()
        Dim x = <%= cTypeExpression %>
    End Sub
End Module
</File>

            Dim fixed =
<File>
Public Class C
End Class

Module Program
    Sub M()
        Dim x = <%= converted %>
    End Sub
End Module
</File>

            Await TestAsync(markup, fixed)
        End Function
    End Class
End Namespace
