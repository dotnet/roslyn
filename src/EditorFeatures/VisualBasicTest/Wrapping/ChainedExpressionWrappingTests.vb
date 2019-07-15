' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports Microsoft.CodeAnalysis.CodeRefactorings
Imports Microsoft.CodeAnalysis.VisualBasic.Wrapping

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.UnitTests.Wrapping
    Public Class ChainedExpressionWrappingTests
        Inherits AbstractWrappingTests

        Protected Overrides Function CreateCodeRefactoringProvider(workspace As Workspace, parameters As TestParameters) As CodeRefactoringProvider
            Return New VisualBasicWrappingCodeRefactoringProvider()
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsWrapping)>
        Public Async Function TestMissingWithSyntaxError() As Task
            Await TestMissingAsync(
"class C
    sub Bar()
        [||]the.quick().brown.fox(
    end sub
end class")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsWrapping)>
        Public Async Function TestMissingWithoutEnoughChunks() As Task
            Await TestMissingAsync(
"class C
    sub Bar()
        [||]the.quick()
    end sub
end class")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsWrapping)>
        Public Async Function TestWithEnoughChunks() As Task
            Await TestAllWrappingCasesAsync(
"class C
    sub Bar()
        [||]the.quick.brown().fox.jumped()
    end sub
end class",
"class C
    sub Bar()
        the.quick.brown().fox _
            .jumped()
    end sub
end class",
"class C
    sub Bar()
        the.quick.brown().fox _
                 .jumped()
    end sub
end class")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsWrapping)>
        Public Async Function TestOkWithOmittedargs() As Task
            Await TestAllWrappingCasesAsync(
"class C
    sub Bar()
        [||]the.quick.brown().fox.jumped(,)
    end sub
end class",
"class C
    sub Bar()
        the.quick.brown().fox _
            .jumped(,)
    end sub
end class",
"class C
    sub Bar()
        the.quick.brown().fox _
                 .jumped(,)
    end sub
end class")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsWrapping)>
        Public Async Function TestUnwrap() As Task
            Await TestAllWrappingCasesAsync(
"class C
    sub Bar()
        [||]the.quick.brown(1, 2, 3).fox _
                 .jumped(1)(2)(3)
    end sub
end class",
"class C
    sub Bar()
        the.quick.brown(1, 2, 3).fox _
            .jumped(1)(2)(3)
    end sub
end class",
"class C
    sub Bar()
        the.quick.brown(1, 2, 3).fox.jumped(1)(2)(3)
    end sub
end class")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsWrapping)>
        Public Async Function TestWrapAndUnwrap() As Task
            Await TestAllWrappingCasesAsync(
"class C
    sub Bar()
        [||]the.quick.
                brown(1, 2, 3) _
           .fox.jumped(1)(2)(3)
    end sub
end class",
"class C
    sub Bar()
        the.quick.brown(1, 2, 3).fox _
            .jumped(1)(2)(3)
    end sub
end class",
"class C
    sub Bar()
        the.quick.brown(1, 2, 3).fox _
                 .jumped(1)(2)(3)
    end sub
end class",
"class C
    sub Bar()
        the.quick.brown(1, 2, 3).fox.jumped(1)(2)(3)
    end sub
end class")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsWrapping)>
        Public Async Function TestChunkMustHaveDottedSection() As Task
            Await TestAllWrappingCasesAsync(
"class C
    sub Bar()
        [||]the().quick.brown(1, 2, 3).fox.jumped(1)(2)(3)
    end sub
end class",
"class C
    sub Bar()
        the().quick.brown(1, 2, 3).fox _
            .jumped(1)(2)(3)
    end sub
end class",
"class C
    sub Bar()
        the().quick.brown(1, 2, 3).fox _
                   .jumped(1)(2)(3)
    end sub
end class")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsWrapping)>
        Public Async Function TrailingNonCallIsNotWrapped() As Task
            Await TestAllWrappingCasesAsync(
"class C
    sub Bar()
        dim y = [||]the.quick.brown().fox.jumped().over
    end sub
end class",
"class C
    sub Bar()
        dim y = the.quick.brown().fox _
            .jumped().over
    end sub
end class",
"class C
    sub Bar()
        dim y = the.quick.brown().fox _
                         .jumped().over
    end sub
end class")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsWrapping)>
        Public Async Function TrailingLongWrapping1() As Task
            Await TestAllWrappingCasesAsync(
"class C
    sub Bar()
        [||]the.quick.brown().fox.jumped().over.the().lazy().dog()
    end sub
end class",
GetIndentionColumn(35),
"class C
    sub Bar()
        the.quick.brown().fox _
            .jumped().over _
            .the() _
            .lazy() _
            .dog()
    end sub
end class",
"class C
    sub Bar()
        the.quick.brown().fox _
                 .jumped().over _
                 .the() _
                 .lazy() _
                 .dog()
    end sub
end class",
"class C
    sub Bar()
        the.quick.brown().fox _
            .jumped().over.the() _
            .lazy().dog()
    end sub
end class",
"class C
    sub Bar()
        the.quick.brown().fox _
                 .jumped().over _
                 .the().lazy() _
                 .dog()
    end sub
end class")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsWrapping)>
        Public Async Function TrailingLongWrapping2() As Task
            Await TestAllWrappingCasesAsync(
"class C
    sub Bar()
        [||]the.quick.brown().fox.jumped().over.the().lazy().dog()
    end sub
end class",
GetIndentionColumn(40),
"class C
    sub Bar()
        the.quick.brown().fox _
            .jumped().over _
            .the() _
            .lazy() _
            .dog()
    end sub
end class",
"class C
    sub Bar()
        the.quick.brown().fox _
                 .jumped().over _
                 .the() _
                 .lazy() _
                 .dog()
    end sub
end class",
"class C
    sub Bar()
        the.quick.brown().fox _
            .jumped().over.the().lazy() _
            .dog()
    end sub
end class",
"class C
    sub Bar()
        the.quick.brown().fox _
                 .jumped().over.the() _
                 .lazy().dog()
    end sub
end class")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsWrapping)>
        Public Async Function TrailingLongWrapping3() As Task
            Await TestAllWrappingCasesAsync(
"class C
    sub Bar()
        [||]the.quick.brown().fox.jumped().over.the().lazy().dog()
    end sub
end class",
GetIndentionColumn(60),
"class C
    sub Bar()
        the.quick.brown().fox _
            .jumped().over _
            .the() _
            .lazy() _
            .dog()
    end sub
end class",
"class C
    sub Bar()
        the.quick.brown().fox _
                 .jumped().over _
                 .the() _
                 .lazy() _
                 .dog()
    end sub
end class",
"class C
    sub Bar()
        the.quick.brown().fox.jumped().over.the().lazy() _
            .dog()
    end sub
end class",
"class C
    sub Bar()
        the.quick.brown().fox.jumped().over.the().lazy() _
                 .dog()
    end sub
end class")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsWrapping)>
        Public Async Function TestAlignToSecondDotInWith() As Task
            Await TestAllWrappingCasesAsync(
"class C
    sub Bar()
        with goo
            [||].the().quick.brown().fox.jumped(,)
        end with
    end sub
end class",
"class C
    sub Bar()
        with goo
            .the().quick.brown().fox _
                .jumped(,)
        end with
    end sub
end class",
"class C
    sub Bar()
        with goo
            .the().quick.brown().fox _
                        .jumped(,)
        end with
    end sub
end class")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsWrapping)>
        Public Async Function TestAlignToThirdDotInWith() As Task
            Await TestAllWrappingCasesAsync(
"class C
    sub Bar()
        with goo
            [||].the.quick.brown().fox.jumped(,)
        end with
    end sub
end class",
"class C
    sub Bar()
        with goo
            .the.quick.brown().fox _
                .jumped(,)
        end with
    end sub
end class",
"class C
    sub Bar()
        with goo
            .the.quick.brown().fox _
                      .jumped(,)
        end with
    end sub
end class")
        End Function
    End Class
End Namespace
