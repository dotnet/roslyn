' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports Microsoft.CodeAnalysis.AddRequiredParentheses
Imports Microsoft.CodeAnalysis.CodeFixes
Imports Microsoft.CodeAnalysis.Diagnostics
Imports Microsoft.CodeAnalysis.Editor.VisualBasic.UnitTests.Diagnostics
Imports Microsoft.CodeAnalysis.VisualBasic.AddRequiredParentheses

Imports VerifyVB = Microsoft.CodeAnalysis.Editor.UnitTests.CodeActions.VisualBasicCodeFixVerifier(Of Microsoft.CodeAnalysis.VisualBasic.AddRequiredParentheses.VisualBasicAddRequiredParenthesesForBinaryLikeExpressionDiagnosticAnalyzer, Microsoft.CodeAnalysis.AddRequiredParentheses.AddRequiredParenthesesCodeFixProvider)

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.UnitTests.AddRequiredParentheses
    <Trait(Traits.Feature, Traits.Features.CodeActionsAddRequiredParentheses)>
    Partial Public Class AddRequiredParenthesesTests
        Private Const RequireAllParenthesesForClarity As String = "[*]
dotnet_style_parentheses_in_arithmetic_binary_operators = always_for_clarity
dotnet_style_parentheses_in_relational_binary_operators = always_for_clarity
dotnet_style_parentheses_in_other_binary_operators = always_for_clarity
dotnet_style_parentheses_in_other_operators = never_if_unnecessary
"
        Private Const RequireOtherBinaryParenthesesForClarity As String = "[*]
dotnet_style_parentheses_in_arithmetic_binary_operators = never_if_unnecessary
dotnet_style_parentheses_in_relational_binary_operators = never_if_unnecessary
dotnet_style_parentheses_in_other_binary_operators = always_for_clarity
dotnet_style_parentheses_in_other_operators = never_if_unnecessary
"
        Private Const RequireArithmeticBinaryParenthesesForClarity As String = "[*]
dotnet_style_parentheses_in_arithmetic_binary_operators = always_for_clarity
dotnet_style_parentheses_in_relational_binary_operators = never_if_unnecessary
dotnet_style_parentheses_in_other_binary_operators = never_if_unnecessary
dotnet_style_parentheses_in_other_operators = never_if_unnecessary
"
        Private Shared Async Function VerifyCodeFixAsync(code As String, fixedCode As String, editorConfig As String) As Task
            Await New VerifyVB.Test With
            {
                .TestCode = code,
                .FixedCode = fixedCode,
                .EditorConfig = editorConfig
            }.RunAsync()
        End Function

        Private Shared Async Function VerifyNoCodeFixAsync(code As String, editorConfig As String) As Task
            Await New VerifyVB.Test With
            {
                .TestCode = code,
                .FixedCode = code,
                .EditorConfig = editorConfig
            }.RunAsync()
        End Function

        <Fact>
        Public Async Function TestArithmeticPrecedence() As Task
            Await VerifyCodeFixAsync(
"class C
    sub M()
        dim x = 1 + 2 [|*|] 3
    end sub
end class",
"class C
    sub M()
        dim x = 1 + (2 * 3)
    end sub
end class", RequireAllParenthesesForClarity)
        End Function

        <Fact>
        Public Async Function TestNoArithmeticOnLowerPrecedence() As Task
            Await VerifyCodeFixAsync(
"class C
    sub M()
        dim x = 1 + 2 [|*|] 3
    end sub
end class",
"class C
    sub M()
        dim x = 1 + (2 * 3)
    end sub
end class", RequireAllParenthesesForClarity)
        End Function

        <Fact>
        Public Async Function TestNotIfArithmeticPrecedenceStaysTheSame() As Task
            Await VerifyNoCodeFixAsync(
"class C
    sub M()
        dim x = 1 + 2 + 3
    end sub
end class", RequireAllParenthesesForClarity)
        End Function

        <Fact>
        Public Async Function TestNotIfArithmeticPrecedenceIsNotEnforced1() As Task
            Await VerifyNoCodeFixAsync(
"class C
    sub M()
        dim x = 1 + 2 + 3
    end sub
end class", RequireOtherBinaryParenthesesForClarity)
        End Function

        <Fact>
        Public Async Function TestNotIfArithmeticPrecedenceIsNotEnforced2() As Task
            Await VerifyNoCodeFixAsync(
"class C
    sub M()
        dim x = 1 + 2 * 3
    end sub
end class", RequireOtherBinaryParenthesesForClarity)
        End Function

        <Fact>
        Public Async Function TestLogicalPrecedence() As Task
            Await VerifyCodeFixAsync(
"class C
    sub M()
        dim x = {|BC30451:a|} orelse {|BC30451:b|} [|andalso|] {|BC30109:c|}
    end sub
end class",
"class C
    sub M()
        dim x = {|BC30451:a|} orelse ({|BC30451:b|} andalso {|BC30109:c|})
    end sub
end class", RequireAllParenthesesForClarity)
        End Function

        <Fact>
        Public Async Function TestNoLogicalOnLowerPrecedence() As Task
            Await VerifyCodeFixAsync(
"class C
    sub M()
        dim x = {|BC30451:a|} orelse {|BC30451:b|} [|andalso|] {|BC30109:c|}
    end sub
end class",
"class C
    sub M()
        dim x = {|BC30451:a|} orelse ({|BC30451:b|} andalso {|BC30109:c|})
    end sub
end class", RequireAllParenthesesForClarity)
        End Function

        <Fact>
        Public Async Function TestNotIfLogicalPrecedenceStaysTheSame() As Task
            Await VerifyNoCodeFixAsync(
"class C
    sub M()
        {|BC30451:int|} {|BC30800:{|BC30451:x|} = {|BC30451:a|} orelse {|BC30451:b|} orelse {|BC30109:c|}|}
    end sub
end class", RequireAllParenthesesForClarity)
        End Function

        <Fact>
        Public Async Function TestNotIfLogicalPrecedenceIsNotEnforced() As Task
            Await VerifyNoCodeFixAsync(
"class C
    sub M()
        dim x = {|BC30451:a|} orelse {|BC30451:b|} orelse {|BC30109:c|}
    end sub
end class", RequireArithmeticBinaryParenthesesForClarity)
        End Function

        <Fact>
        Public Async Function TestMixedArithmeticAndLogical() As Task
            Await VerifyNoCodeFixAsync(
"class C
    sub M()
        dim x = {|BC30451:a|} = {|BC30451:b|} andalso {|BC30109:c|} = {|BC30451:d|}
    end sub
end class", RequireAllParenthesesForClarity)
        End Function

        <Fact>
        Public Async Function TestShiftPrecedence1() As Task
            Await VerifyCodeFixAsync(
"class C
    sub M()
        dim x = 1 [|+|] 2 << 3
    end sub
end class",
"class C
    sub M()
        dim x = (1 + 2) << 3
    end sub
end class", RequireAllParenthesesForClarity)
        End Function

        <Fact>
        Public Async Function TestShiftPrecedence2() As Task
            Await VerifyCodeFixAsync(
"class C
    sub M()
        dim x = 1 [|+|] 2 << 3
    end sub
end class",
"class C
    sub M()
        dim x = (1 + 2) << 3
    end sub
end class", RequireArithmeticBinaryParenthesesForClarity)
        End Function

        <Fact>
        Public Async Function TestShiftPrecedence3() As Task
            Await VerifyNoCodeFixAsync(
"class C
    sub M()
        dim x = 1 << 2 << 3
    end sub
end class", RequireArithmeticBinaryParenthesesForClarity)
        End Function
    End Class
End Namespace
