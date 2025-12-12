' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.UnitTests.Wrapping
    <Trait(Traits.Feature, Traits.Features.CodeActionsWrapping)>
    Public Class ChainedExpressionWrappingTests
        Inherits AbstractWrappingTests

        <Fact>
        Public Async Function TestMissingWithSyntaxError() As Task
            Await TestMissingAsync(
"class C
    sub Bar()
        [||]the.quick().brown.fox(
    end sub
end class")
        End Function

        <Fact>
        Public Async Function TestMissingWithoutEnoughChunks() As Task
            Await TestMissingAsync(
"class C
    sub Bar()
        [||]the.quick()
    end sub
end class")
        End Function

        <Fact>
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

        <Fact>
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

        <Fact>
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

        <Fact>
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

        <Fact>
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

        <Fact>
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

        <Fact>
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

        <Fact>
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

        <Fact>
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

        <Fact>
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

        <Fact>
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
