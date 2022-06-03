' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports Microsoft.CodeAnalysis.CodeRefactorings
Imports Microsoft.CodeAnalysis.CodeStyle
Imports Microsoft.CodeAnalysis.Editor.UnitTests.CodeActions
Imports Microsoft.CodeAnalysis.VisualBasic.Wrapping

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.UnitTests.Wrapping
    Public Class BinaryExpressionWrappingTests
        Inherits AbstractWrappingTests

        Protected Overrides Function CreateCodeRefactoringProvider(workspace As Workspace, parameters As TestParameters) As CodeRefactoringProvider
            Return New VisualBasicWrappingCodeRefactoringProvider()
        End Function

        Private ReadOnly Property EndOfLine As TestParameters =
            New TestParameters(options:=[Option](CodeStyleOptions2.OperatorPlacementWhenWrapping, OperatorPlacementWhenWrappingPreference.EndOfLine))

        Private ReadOnly Property BeginningOfLine As TestParameters =
            New TestParameters(options:=[Option](CodeStyleOptions2.OperatorPlacementWhenWrapping, OperatorPlacementWhenWrappingPreference.BeginningOfLine))

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

        <WorkItem(34127, "https://github.com/dotnet/roslyn/issues/34127")>
        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsWrapping)>
        Public Async Function TestWrapLowerPrecedenceInLargeBinary() As Task
            Await TestAllWrappingCasesAsync(
"class C
    sub Bar()
        dim goo = [||]a + b + c + d = x * y * z
    end sub
end class",
"class C
    sub Bar()
        dim goo = a + b + c + d _
            = x * y * z
    end sub
end class",
"class C
    sub Bar()
        dim goo = a + b + c + d _
                  = x * y * z
    end sub
end class")
        End Function
    End Class
End Namespace
