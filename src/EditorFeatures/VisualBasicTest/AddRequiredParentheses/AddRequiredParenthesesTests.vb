' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports Microsoft.CodeAnalysis.AddRequiredParentheses
Imports Microsoft.CodeAnalysis.CodeFixes
Imports Microsoft.CodeAnalysis.Diagnostics
Imports Microsoft.CodeAnalysis.Editor.VisualBasic.UnitTests.Diagnostics
Imports Microsoft.CodeAnalysis.VisualBasic.AddRequiredParentheses

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.UnitTests.AddRequiredParentheses
    Partial Public Class AddRequiredParenthesesTests
        Inherits AbstractVisualBasicDiagnosticProviderBasedUserDiagnosticTest

        Friend Overrides Function CreateDiagnosticProviderAndFixer(Workspace As Workspace) As (DiagnosticAnalyzer, CodeFixProvider)
            Return (New VisualBasicAddRequiredParenthesesForBinaryLikeExpressionDiagnosticAnalyzer(), New AddRequiredParenthesesCodeFixProvider())
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsAddRequiredParentheses)>
        Public Async Function TestArithmeticPrecedence() As Task
            Await TestInRegularAndScript1Async(
"class C
    sub M()
        dim x = 1 + 2 $$* 3
    end sub
end class",
"class C
    sub M()
        dim x = 1 + (2 * 3)
    end sub
end class", parameters:=New TestParameters(options:=RequireAllParenthesesForClarity))
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsAddRequiredParentheses)>
        Public Async Function TestNoArithmeticOnLowerPrecedence() As Task
            Await TestMissingAsync(
"class C
    sub M()
        dim x = 1 $$+ 2 * 3
    end sub
end class", parameters:=New TestParameters(options:=RequireAllParenthesesForClarity))
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsAddRequiredParentheses)>
        Public Async Function TestNotIfArithmeticPrecedenceStaysTheSame() As Task
            Await TestMissingAsync(
"class C
    sub M()
        dim x = 1 + 2 $$+ 3
    end sub
end class", parameters:=New TestParameters(options:=RequireAllParenthesesForClarity))
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsAddRequiredParentheses)>
        Public Async Function TestNotIfArithmeticPrecedenceIsNotEnforced1() As Task
            Await TestMissingAsync(
"class C
    sub M()
        dim x = 1 + 2 $$+ 3
    end sub
end class", parameters:=New TestParameters(options:=RequireOtherBinaryParenthesesForClarity))
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsAddRequiredParentheses)>
        Public Async Function TestNotIfArithmeticPrecedenceIsNotEnforced2() As Task
            Await TestMissingAsync(
"class C
    sub M()
        dim x = 1 + 2 $$* 3
    end sub
end class", parameters:=New TestParameters(options:=RequireOtherBinaryParenthesesForClarity))
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsAddRequiredParentheses)>
        Public Async Function TestLogicalPrecedence() As Task
            Await TestInRegularAndScript1Async(
"class C
    sub M()
        dim x = a orelse b $$andalso c
    end sub
end class",
"class C
    sub M()
        dim x = a orelse (b andalso c)
    end sub
end class", parameters:=New TestParameters(options:=RequireAllParenthesesForClarity))
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsAddRequiredParentheses)>
        Public Async Function TestNoLogicalOnLowerPrecedence() As Task
            Await TestMissingAsync(
"class C
    sub M()
        dim x = a $$orelse b andalso c
    end sub
end class", parameters:=New TestParameters(options:=RequireAllParenthesesForClarity))
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsAddRequiredParentheses)>
        Public Async Function TestNotIfLogicalPrecedenceStaysTheSame() As Task
            Await TestMissingAsync(
"class C
    sub M()
        int x = a orelse b $$orelse c
    end sub
end class", parameters:=New TestParameters(options:=RequireAllParenthesesForClarity))
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsAddRequiredParentheses)>
        Public Async Function TestNotIfLogicalPrecedenceIsNotEnforced() As Task
            Await TestMissingAsync(
"class C
    sub M()
        dim x = a orelse b $$orelse c
    end sub
end class", parameters:=New TestParameters(options:=RequireArithmeticBinaryParenthesesForClarity))
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsAddRequiredParentheses)>
        Public Async Function TestMixedArithmeticAndLogical() As Task
            Await TestMissingAsync(
"class C
    sub M()
        dim x = a = b $$andalso c = d
    end sub
end class", New TestParameters(options:=RequireAllParenthesesForClarity))
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsAddRequiredParentheses)>
        Public Async Function TestShiftPrecedence1() As Task
            Await TestInRegularAndScript1Async(
"class C
    sub M()
        dim x = 1 $$+ 2 << 3
    end sub
end class",
"class C
    sub M()
        dim x = (1 + 2) << 3
    end sub
end class", parameters:=New TestParameters(options:=RequireAllParenthesesForClarity))
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsAddRequiredParentheses)>
        Public Async Function TestShiftPrecedence2() As Task
            Await TestInRegularAndScript1Async(
"class C
    sub M()
        dim x = 1 $$+ 2 << 3
    end sub
end class",
"class C
    sub M()
        dim x = (1 + 2) << 3
    end sub
end class", parameters:=New TestParameters(options:=RequireArithmeticBinaryParenthesesForClarity))
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsAddRequiredParentheses)>
        Public Async Function TestShiftPrecedence3() As Task
            Await TestMissingAsync(
"class C
    sub M()
        dim x = 1 $$<< 2 << 3
    end sub
end class", parameters:=New TestParameters(options:=RequireArithmeticBinaryParenthesesForClarity))
        End Function
    End Class
End Namespace
