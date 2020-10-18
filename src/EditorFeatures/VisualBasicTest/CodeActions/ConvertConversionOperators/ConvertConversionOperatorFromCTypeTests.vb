' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports Microsoft.CodeAnalysis.CodeRefactorings
Imports Microsoft.CodeAnalysis.VisualBasic.CodeRefactorings.ConvertConversionOperators

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.UnitTests.CodeRefactorings.ConvertConversionOperators
    <Trait(Traits.Feature, Traits.Features.ConvertConversionOperators)>
    Public Class ConvertConversionOperatorFromCTypeTests
        Inherits AbstractVisualBasicCodeActionTest

        Protected Overrides Function CreateCodeRefactoringProvider(workspace As Workspace, parameters As TestParameters) As CodeRefactoringProvider
            Return New VisualBasicConvertConversionOperatorFromCTypeCodeRefactoringProvider()
        End Function

        <Fact>
        Public Async Function ConvertFromCTypeToTryCast() As Task
            Dim markup =
<File>
Module Program
    Sub M()
        Dim x = CType(1[||], Object)
    End Sub
End Module
</File>

            Dim expected =
<File>
Module Program
    Sub M()
        Dim x = TryCast(1, Object)
    End Sub
End Module
</File>

            Await TestAsync(markup, expected)
        End Function

        <Fact>
        Public Async Function ConvertFromCTypeNoConverionIfTypeIsValueType() As Task
            Dim markup =
<File>
Module Program
    Sub M()
        Dim x = CType(1[||], Byte)
    End Sub
End Module
</File>

            Await TestMissingAsync(markup)
        End Function

        <Fact>
        Public Async Function ConvertFromCTypeNoConverionIfTypeIsValueType_GenericTypeConstraint() As Task
            Dim markup =
<File>
Module Program
    Sub M(Of T As Structure)()
        Dim x = CType([||]1, T)
    End Sub
End Module
</File>

            Await TestMissingAsync(markup)
        End Function

        <Fact>
        Public Async Function ConvertFromCBoolIsNotOffered() As Task
            Dim markup =
<File>
Module Program
    Sub M()
        Dim x = CBool([||]1)
    End Sub
End Module
</File>

            Await TestMissingAsync(markup)
        End Function

        <Theory>
        <InlineData("CType(CType(1, [||]object), C)",
                    "CType(TryCast(1, object), C)")>
        <InlineData("CType(CType(1, object), [||]C)",
                    "TryCast(CType(1, object), C)")>
        Public Async Function ConvertFromCTypeNested(cTypeExpression As String, converted As String) As Task
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
