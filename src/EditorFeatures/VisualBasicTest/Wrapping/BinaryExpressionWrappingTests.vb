' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports Microsoft.CodeAnalysis.CodeRefactorings
Imports Microsoft.CodeAnalysis.CodeStyle
Imports Microsoft.CodeAnalysis.Options
Imports Microsoft.CodeAnalysis.VisualBasic.Wrapping

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.UnitTests.Wrapping
    Public Class BinaryExpressionWrappingTests
        Inherits AbstractWrappingTests

        Protected Overrides Function CreateCodeRefactoringProvider(workspace As Workspace, parameters As TestParameters) As CodeRefactoringProvider
            Return New VisualBasicWrappingCodeRefactoringProvider()
        End Function

        Private ReadOnly Property EndOfLine As IDictionary(Of OptionKey, Object) = [Option](
            CodeStyleOptions.OperatorPlacementWhenWrapping,
            OperatorPlacementWhenWrappingPreference.EndOfLine)

        Private ReadOnly Property BeginningOfLine As IDictionary(Of OptionKey, Object) = [Option](
            CodeStyleOptions.OperatorPlacementWhenWrapping,
            OperatorPlacementWhenWrappingPreference.BeginningOfLine)

        Private Function TestEndOfLine(markup As String, expected As String) As Task
            Return TestInRegularAndScript1Async(markup, expected, parameters:=New TestParameters(
                options:=EndOfLine))
        End Function

        Private Function TestBeginningOfLine(markup As String, expected As String) As Task
            Return TestInRegularAndScript1Async(markup, expected, parameters:=New TestParameters(
                options:=BeginningOfLine))
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsWrapping)>
        Public Async Function TestMissingWithSyntaxError() As Task
            Await TestMissingAsync(
"class C
    sub Bar()
        if ([||]i andalso (j andalso )
    end sub
end class")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsWrapping)>
        Public Async Function TestMissingWithSelection() As Task
            Await TestMissingAsync(
"class C
    sub Bar()
        if ([|i|] andalso j)
        end if
    end sub
end class")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsWrapping)>
        Public Async Function TestMissingBeforeExpr() As Task
            Await TestMissingAsync(
"class C
    sub Bar()
        [||]if (i andalso j)
        end if
    end sub
end class")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsWrapping)>
        Public Async Function TestMissingWithSingleExpr() As Task
            Await TestMissingAsync(
"class C
    sub Bar()
        if ([||]i)
        end if
    end sub
end class")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsWrapping)>
        Public Async Function TestMissingWithMultiLineExpression() As Task
            Await TestMissingAsync(
"class C
    sub Bar()
        if ([||]i andalso (j +
            k))
        end if
    end sub
end class")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsWrapping)>
        Public Async Function TestMissingWithMultiLineExpr2() As Task
            Await TestMissingAsync(
"class C
    sub Bar()
        if ([||]i andalso ""
        "")
        end if
    end sub
end class")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsWrapping)>
        Public Async Function TestInIf() As Task
            Await TestAllWrappingCasesAsync(
"class C
    sub Bar()
        if ([||]i andalso j)
        end if
    end sub
end class",
EndOfLine,
"class C
    sub Bar()
        if (i andalso
                j)
        end if
    end sub
end class",
"class C
    sub Bar()
        if (i andalso
            j)
        end if
    end sub
end class")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsWrapping)>
        Public Async Function TestInIf_IncludingOp() As Task
            Await TestAllWrappingCasesAsync(
"class C
    sub Bar()
        if ([||]i andalso j)
        end if
    end sub
end class",
BeginningOfLine,
"class C
    sub Bar()
        if (i _
                andalso j)
        end if
    end sub
end class",
"class C
    sub Bar()
        if (i _
            andalso j)
        end if
    end sub
end class")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsWrapping)>
        Public Async Function TestInIf2() As Task
            Await TestAllWrappingCasesAsync(
"class C
    sub Bar()
        if (i[||] andalso j)
        end if
    end sub
end class",
EndOfLine,
"class C
    sub Bar()
        if (i andalso
                j)
        end if
    end sub
end class",
"class C
    sub Bar()
        if (i andalso
            j)
        end if
    end sub
end class")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsWrapping)>
        Public Async Function TestInIf3() As Task
            Await TestAllWrappingCasesAsync(
"class C
    sub Bar()
        if (i [||]andalso j)
        end if
    end sub
end class",
EndOfLine,
"class C
    sub Bar()
        if (i andalso
                j)
        end if
    end sub
end class",
"class C
    sub Bar()
        if (i andalso
            j)
        end if
    end sub
end class")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsWrapping)>
        Public Async Function TestInIf4() As Task
            Await TestAllWrappingCasesAsync(
"class C
    sub Bar()
        if (i andalso[||] j)
        end if
    end sub
end class",
EndOfLine,
"class C
    sub Bar()
        if (i andalso
                j)
        end if
    end sub
end class",
"class C
    sub Bar()
        if (i andalso
            j)
        end if
    end sub
end class")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsWrapping)>
        Public Async Function TestInIf5() As Task
            Await TestAllWrappingCasesAsync(
"class C
    sub Bar()
        if (i andalso [||]j)
        end if
    end sub
end class",
EndOfLine,
"class C
    sub Bar()
        if (i andalso
                j)
        end if
    end sub
end class",
"class C
    sub Bar()
        if (i andalso
            j)
        end if
    end sub
end class")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsWrapping)>
        Public Async Function TestTwoExprWrappingCases_End() As Task
            Await TestAllWrappingCasesAsync(
"class C
    sub Bar()
        if ([||]i andalso j)
        end if
    end sub
end class",
EndOfLine,
"class C
    sub Bar()
        if (i andalso
                j)
        end if
    end sub
end class",
"class C
    sub Bar()
        if (i andalso
            j)
        end if
    end sub
end class")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsWrapping)>
        Public Async Function TestTwoExprWrappingCases_Beginning() As Task
            Await TestAllWrappingCasesAsync(
"class C
    sub Bar()
        if ([||]i andalso j)
        end if
    end sub
end class",
BeginningOfLine,
"class C
    sub Bar()
        if (i _
                andalso j)
        end if
    end sub
end class",
"class C
    sub Bar()
        if (i _
            andalso j)
        end if
    end sub
end class")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsWrapping)>
        Public Async Function TestThreeExprWrappingCases_End() As Task
            Await TestAllWrappingCasesAsync(
"class C
    sub Bar()
        if ([||]i andalso j orelse k)
        end if
    end sub
end class",
EndOfLine,
"class C
    sub Bar()
        if (i andalso
                j orelse
                k)
        end if
    end sub
end class",
"class C
    sub Bar()
        if (i andalso
            j orelse
            k)
        end if
    end sub
end class")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsWrapping)>
        Public Async Function TestThreeExprWrappingCases_Beginning() As Task
            Await TestAllWrappingCasesAsync(
"class C
    sub Bar()
        if ([||]i andalso j orelse k)
        end if
    end sub
end class",
BeginningOfLine,
"class C
    sub Bar()
        if (i _
                andalso j _
                orelse k)
        end if
    end sub
end class",
"class C
    sub Bar()
        if (i _
            andalso j _
            orelse k)
        end if
    end sub
end class")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsWrapping)>
        Public Async Function Test_AllOptions_NoInitialMatches_End() As Task
            Await TestAllWrappingCasesAsync(
"class C
    sub Bar()
        if (
            [||]i   andalso
                j _
                 orelse   k)
        end if
    end sub
end class",
EndOfLine,
"class C
    sub Bar()
        if (
            i andalso
            j orelse
            k)
        end if
    end sub
end class",
"class C
    sub Bar()
        if (
            i andalso j orelse k)
        end if
    end sub
end class")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsWrapping)>
        Public Async Function Test_AllOptions_NoInitialMatches_Beginning() As Task
            Await TestAllWrappingCasesAsync(
"class C
    sub Bar()
        if (
            [||]i   andalso
                j _
                 orelse   k)
        end if
    end sub
end class",
BeginningOfLine,
"class C
    sub Bar()
        if (
            i _
            andalso j _
            orelse k)
        end if
    end sub
end class",
"class C
    sub Bar()
        if (
            i andalso j orelse k)
        end if
    end sub
end class")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsWrapping)>
        Public Async Function Test_DoNotOfferExistingOption1() As Task
            Await TestAllWrappingCasesAsync(
"class C
    sub Bar()
        if ([||]a andalso
            b)
        end if
    end sub
end class",
"class C
    sub Bar()
        if (a _
                andalso b)
        end if
    end sub
end class",
"class C
    sub Bar()
        if (a _
            andalso b)
        end if
    end sub
end class",
"class C
    sub Bar()
        if (a andalso b)
        end if
    end sub
end class")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsWrapping)>
        Public Async Function Test_DoNotOfferExistingOption2_End() As Task
            Await TestAllWrappingCasesAsync(
"class C
    sub Bar()
        if ([||]a _
            andalso b)
        end if
    end sub
end class",
EndOfLine,
"class C
    sub Bar()
        if (a andalso
            b)
        end if
    end sub
end class",
"class C
    sub Bar()
        if (a andalso b)
        end if
    end sub
end class")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsWrapping)>
        Public Async Function Test_DoNotOfferExistingOption2_Beginning() As Task
            Await TestAllWrappingCasesAsync(
"class C
    sub Bar()
        if ([||]a _
            andalso b)
        end if
    end sub
end class",
BeginningOfLine,
"class C
    sub Bar()
        if (a andalso b)
        end if
    end sub
end class")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsWrapping)>
        Public Async Function TestInLocalInitializer() As Task
            Await TestAllWrappingCasesAsync(
"class C
    sub Goo()
        dim v = [||]a andalso b andalso c
    end sub
end class",
EndOfLine,
"class C
    sub Goo()
        dim v = a andalso
            b andalso
            c
    end sub
end class",
"class C
    sub Goo()
        dim v = a andalso
                b andalso
                c
    end sub
end class")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsWrapping)>
        Public Async Function TestInField_Beginning() As Task
            Await TestAllWrappingCasesAsync(
"class C
    dim v = [||]a andalso b andalso c
end class",
BeginningOfLine,
"class C
    dim v = a _
        andalso b _
        andalso c
end class",
"class C
    dim v = a _
            andalso b _
            andalso c
end class")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsWrapping)>
        Public Async Function TestInField_End() As Task
            Await TestAllWrappingCasesAsync(
"class C
    dim v = [||]a andalso b andalso c
end class",
EndOfLine,
"class C
    dim v = a andalso
        b andalso
        c
end class",
"class C
    dim v = a andalso
            b andalso
            c
end class")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsWrapping)>
        Public Async Function TestAddition_End() As Task
            Await TestAllWrappingCasesAsync(
"class C
    sub Bar()
        dim goo = [||]""now"" & ""is"" & ""the"" & ""time""
    end sub
end class",
EndOfLine,
"class C
    sub Bar()
        dim goo = ""now"" &
            ""is"" &
            ""the"" &
            ""time""
    end sub
end class",
"class C
    sub Bar()
        dim goo = ""now"" &
                  ""is"" &
                  ""the"" &
                  ""time""
    end sub
end class")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsWrapping)>
        Public Async Function TestAddition_Beginning() As Task
            Await TestAllWrappingCasesAsync(
"class C
    sub Bar()
        dim goo = [||]""now"" & ""is"" & ""the"" & ""time""
    end sub
end class",
"class C
    sub Bar()
        dim goo = ""now"" _
            & ""is"" _
            & ""the"" _
            & ""time""
    end sub
end class",
"class C
    sub Bar()
        dim goo = ""now"" _
                  & ""is"" _
                  & ""the"" _
                  & ""time""
    end sub
end class")
        End Function
    End Class
End Namespace
