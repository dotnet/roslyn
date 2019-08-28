' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports Microsoft.CodeAnalysis.CodeRefactorings
Imports Microsoft.CodeAnalysis.VisualBasic.Wrapping

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.UnitTests.Wrapping
    Public Class ArgumentWrappingTests
        Inherits AbstractWrappingTests

        Protected Overrides Function CreateCodeRefactoringProvider(workspace As Workspace, parameters As TestParameters) As CodeRefactoringProvider
            Return New VisualBasicWrappingCodeRefactoringProvider()
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsWrapping)>
        Public Async Function TestMissingWithSyntaxError() As Task
            Await TestMissingAsync(
"class C
    sub Bar()
        Goobar([||]i, j
    end sub
end class")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsWrapping)>
        Public Async Function TestMissingWithSelection() As Task
            Await TestMissingAsync(
"class C
    sub Bar()
        Goobar([|i|], j)
    end sub
end class")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsWrapping)>
        Public Async Function TestMissingBeforeName() As Task
            Await TestMissingAsync(
"class C
    sub Bar()
        a.[||]b.Goobar(i, j)
    end sub
end class")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsWrapping)>
        Public Async Function TestMissingWithSingleParameter() As Task
            Await TestMissingAsync(
"class C
    sub Bar()
        Goobar([||]i)
    end sub
end class")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsWrapping)>
        Public Async Function TestMissingWithMultiLineParameter() As Task
            Await TestMissingAsync(
"class C
    sub Bar()
        Goobar([||]i, j +
            k)
    end sub
end class")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsWrapping)>
        Public Async Function TestInHeader1() As Task
            Await TestInRegularAndScript1Async(
"class C
    sub Bar()
        [||]Goobar(i, j)
    end sub
end class",
"class C
    sub Bar()
        Goobar(i,
               j)
    end sub
end class")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsWrapping)>
        Public Async Function TestInHeader2() As Task
            Await TestInRegularAndScript1Async(
"class C
    sub Bar()
        a.[||]Goobar(i, j)
    end sub
end class",
"class C
    sub Bar()
        a.Goobar(i,
                 j)
    end sub
end class")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsWrapping)>
        Public Async Function TestInHeader4() As Task
            Await TestInRegularAndScript1Async(
"class C
    sub Bar()
        a.Goobar(i, j[||])
    end sub
end class",
"class C
    sub Bar()
        a.Goobar(i,
                 j)
    end sub
end class")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsWrapping)>
        Public Async Function TestTwoParamWrappingCases() As Task
            Await TestAllWrappingCasesAsync(
"class C
    sub Bar()
        a.Goobar([||]i, j)
    end sub
end class",
"class C
    sub Bar()
        a.Goobar(i,
                 j)
    end sub
end class",
"class C
    sub Bar()
        a.Goobar(
            i,
            j)
    end sub
end class",
"class C
    sub Bar()
        a.Goobar(i,
            j)
    end sub
end class",
"class C
    sub Bar()
        a.Goobar(
            i, j)
    end sub
end class")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsWrapping)>
        Public Async Function TestThreeParamWrappingCases() As Task
            Await TestAllWrappingCasesAsync(
"class C
    sub Bar()
        a.Goobar([||]i, j, k)
    end sub
end class",
"class C
    sub Bar()
        a.Goobar(i,
                 j,
                 k)
    end sub
end class",
"class C
    sub Bar()
        a.Goobar(
            i,
            j,
            k)
    end sub
end class",
"class C
    sub Bar()
        a.Goobar(i,
            j,
            k)
    end sub
end class",
"class C
    sub Bar()
        a.Goobar(
            i, j, k)
    end sub
end class")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsWrapping)>
        Public Async Function Test_AllOptions_NoInitialMatches() As Task
            Await TestAllWrappingCasesAsync(
"class C
    sub Bar()
        a.Goobar(
            [||]i,
                j,
                    k)
    end sub
end class",
"class C
    sub Bar()
        a.Goobar(i,
                 j,
                 k)
    end sub
end class",
"class C
    sub Bar()
        a.Goobar(
            i,
            j,
            k)
    end sub
end class",
"class C
    sub Bar()
        a.Goobar(i,
            j,
            k)
    end sub
end class",
"class C
    sub Bar()
        a.Goobar(i, j, k)
    end sub
end class",
"class C
    sub Bar()
        a.Goobar(
            i, j, k)
    end sub
end class")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsWrapping)>
        Public Async Function Test_LongWrapping_ShortIds() As Task
            Await TestAllWrappingCasesAsync(
"class C
    sub Goo()
        me.Goobar([||]
            i, j, k, l, m, n, o, p,
            n)
    end sub
end class",
GetIndentionColumn(30),
"class C
    sub Goo()
        me.Goobar(i,
                  j,
                  k,
                  l,
                  m,
                  n,
                  o,
                  p,
                  n)
    end sub
end class",
"class C
    sub Goo()
        me.Goobar(
            i,
            j,
            k,
            l,
            m,
            n,
            o,
            p,
            n)
    end sub
end class",
"class C
    sub Goo()
        me.Goobar(i,
            j,
            k,
            l,
            m,
            n,
            o,
            p,
            n)
    end sub
end class",
"class C
    sub Goo()
        me.Goobar(i, j, k, l, m, n, o, p, n)
    end sub
end class",
"class C
    sub Goo()
        me.Goobar(
            i, j, k, l, m, n, o, p, n)
    end sub
end class",
"class C
    sub Goo()
        me.Goobar(i, j, k, l,
                  m, n, o, p,
                  n)
    end sub
end class",
"class C
    sub Goo()
        me.Goobar(
            i, j, k, l, m, n,
            o, p, n)
    end sub
end class",
"class C
    sub Goo()
        me.Goobar(i, j, k, l,
            m, n, o, p, n)
    end sub
end class")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsWrapping)>
        Public Async Function Test_LongWrapping_VariadicLengthIds() As Task
            Await TestAllWrappingCasesAsync(
"class C
    sub Goo()
        me.Goobar([||]
            i, jj, kkkkk, llllllll, mmmmmmmmmmmmmmmmmm,
            nnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnn)
    end sub
end class",
GetIndentionColumn(30),
"class C
    sub Goo()
        me.Goobar(i,
                  jj,
                  kkkkk,
                  llllllll,
                  mmmmmmmmmmmmmmmmmm,
                  nnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnn)
    end sub
end class",
"class C
    sub Goo()
        me.Goobar(
            i,
            jj,
            kkkkk,
            llllllll,
            mmmmmmmmmmmmmmmmmm,
            nnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnn)
    end sub
end class",
"class C
    sub Goo()
        me.Goobar(i,
            jj,
            kkkkk,
            llllllll,
            mmmmmmmmmmmmmmmmmm,
            nnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnn)
    end sub
end class",
"class C
    sub Goo()
        me.Goobar(i, jj, kkkkk, llllllll, mmmmmmmmmmmmmmmmmm, nnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnn)
    end sub
end class",
"class C
    sub Goo()
        me.Goobar(
            i, jj, kkkkk, llllllll, mmmmmmmmmmmmmmmmmm, nnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnn)
    end sub
end class",
"class C
    sub Goo()
        me.Goobar(i, jj, kkkkk,
                  llllllll,
                  mmmmmmmmmmmmmmmmmm,
                  nnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnn)
    end sub
end class",
"class C
    sub Goo()
        me.Goobar(
            i, jj, kkkkk,
            llllllll,
            mmmmmmmmmmmmmmmmmm,
            nnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnn)
    end sub
end class",
"class C
    sub Goo()
        me.Goobar(i, jj, kkkkk,
            llllllll,
            mmmmmmmmmmmmmmmmmm,
            nnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnn)
    end sub
end class")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsWrapping)>
        Public Async Function Test_DoNotOfferLongWrappingOptionThatAlreadyAppeared() As Task
            Await TestAllWrappingCasesAsync(
"class C
    sub Goo()
        me.Goobar([||]
            iiiii, jjjjj, kkkkk, lllll, mmmmm,
            nnnnn)
    end sub
end class",
GetIndentionColumn(25),
"class C
    sub Goo()
        me.Goobar(iiiii,
                  jjjjj,
                  kkkkk,
                  lllll,
                  mmmmm,
                  nnnnn)
    end sub
end class",
"class C
    sub Goo()
        me.Goobar(
            iiiii,
            jjjjj,
            kkkkk,
            lllll,
            mmmmm,
            nnnnn)
    end sub
end class",
"class C
    sub Goo()
        me.Goobar(iiiii,
            jjjjj,
            kkkkk,
            lllll,
            mmmmm,
            nnnnn)
    end sub
end class",
"class C
    sub Goo()
        me.Goobar(iiiii, jjjjj, kkkkk, lllll, mmmmm, nnnnn)
    end sub
end class",
"class C
    sub Goo()
        me.Goobar(
            iiiii, jjjjj, kkkkk, lllll, mmmmm, nnnnn)
    end sub
end class",
"class C
    sub Goo()
        me.Goobar(
            iiiii, jjjjj,
            kkkkk, lllll,
            mmmmm, nnnnn)
    end sub
end class",
"class C
    sub Goo()
        me.Goobar(iiiii,
            jjjjj, kkkkk,
            lllll, mmmmm,
            nnnnn)
    end sub
end class")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsWrapping)>
        Public Async Function Test_DoNotOfferAllLongWrappingOptionThatAlreadyAppeared() As Task
            Await TestAllWrappingCasesAsync(
"class C
    sub Bar()
        a.[||]Goobar(
            iiiii, jjjjj, kkkkk, lllll, mmmmm,
            nnnnn)
    end sub
end class",
GetIndentionColumn(20),
"class C
    sub Bar()
        a.Goobar(iiiii,
                 jjjjj,
                 kkkkk,
                 lllll,
                 mmmmm,
                 nnnnn)
    end sub
end class",
"class C
    sub Bar()
        a.Goobar(
            iiiii,
            jjjjj,
            kkkkk,
            lllll,
            mmmmm,
            nnnnn)
    end sub
end class",
"class C
    sub Bar()
        a.Goobar(iiiii,
            jjjjj,
            kkkkk,
            lllll,
            mmmmm,
            nnnnn)
    end sub
end class",
"class C
    sub Bar()
        a.Goobar(iiiii, jjjjj, kkkkk, lllll, mmmmm, nnnnn)
    end sub
end class",
"class C
    sub Bar()
        a.Goobar(
            iiiii, jjjjj, kkkkk, lllll, mmmmm, nnnnn)
    end sub
end class")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsWrapping)>
        Public Async Function Test_LongWrapping_VariadicLengthIds2() As Task
            Await TestAllWrappingCasesAsync(
"class C
    sub Bar()
        a.[||]Goobar(
            i, jj, kkkk, lll, mm,
            n)
    end sub
end class",
GetIndentionColumn(30),
"class C
    sub Bar()
        a.Goobar(i,
                 jj,
                 kkkk,
                 lll,
                 mm,
                 n)
    end sub
end class",
"class C
    sub Bar()
        a.Goobar(
            i,
            jj,
            kkkk,
            lll,
            mm,
            n)
    end sub
end class",
"class C
    sub Bar()
        a.Goobar(i,
            jj,
            kkkk,
            lll,
            mm,
            n)
    end sub
end class",
"class C
    sub Bar()
        a.Goobar(i, jj, kkkk, lll, mm, n)
    end sub
end class",
"class C
    sub Bar()
        a.Goobar(
            i, jj, kkkk, lll, mm, n)
    end sub
end class",
"class C
    sub Bar()
        a.Goobar(i, jj, kkkk,
                 lll, mm, n)
    end sub
end class",
"class C
    sub Bar()
        a.Goobar(
            i, jj, kkkk, lll,
            mm, n)
    end sub
end class",
"class C
    sub Bar()
        a.Goobar(i, jj, kkkk,
            lll, mm, n)
    end sub
end class")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsWrapping)>
        Public Async Function Test_DoNotOfferExistingOption1() As Task
            Await TestAllWrappingCasesAsync(
"class C
    sub Bar()
        a.[||]Goobar(iiiii,
                 jjjjj,
                 kkkkk,
                 lllll,
                 mmmmm,
                 nnnnn)
    end sub
end class",
GetIndentionColumn(30),
"class C
    sub Bar()
        a.Goobar(
            iiiii,
            jjjjj,
            kkkkk,
            lllll,
            mmmmm,
            nnnnn)
    end sub
end class",
"class C
    sub Bar()
        a.Goobar(iiiii,
            jjjjj,
            kkkkk,
            lllll,
            mmmmm,
            nnnnn)
    end sub
end class",
"class C
    sub Bar()
        a.Goobar(iiiii, jjjjj, kkkkk, lllll, mmmmm, nnnnn)
    end sub
end class",
"class C
    sub Bar()
        a.Goobar(
            iiiii, jjjjj, kkkkk, lllll, mmmmm, nnnnn)
    end sub
end class",
"class C
    sub Bar()
        a.Goobar(iiiii, jjjjj,
                 kkkkk, lllll,
                 mmmmm, nnnnn)
    end sub
end class",
"class C
    sub Bar()
        a.Goobar(
            iiiii, jjjjj,
            kkkkk, lllll,
            mmmmm, nnnnn)
    end sub
end class",
"class C
    sub Bar()
        a.Goobar(iiiii, jjjjj,
            kkkkk, lllll,
            mmmmm, nnnnn)
    end sub
end class")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsWrapping)>
        Public Async Function Test_DoNotOfferExistingOption2() As Task
            Await TestAllWrappingCasesAsync(
"class C
    sub Bar()
        a.Goobar([||]
            i,
            jj,
            kkkk,
            lll,
            mm,
            n)
    end sub
end class",
GetIndentionColumn(30),
"class C
    sub Bar()
        a.Goobar(i,
                 jj,
                 kkkk,
                 lll,
                 mm,
                 n)
    end sub
end class",
"class C
    sub Bar()
        a.Goobar(i,
            jj,
            kkkk,
            lll,
            mm,
            n)
    end sub
end class",
"class C
    sub Bar()
        a.Goobar(i, jj, kkkk, lll, mm, n)
    end sub
end class",
"class C
    sub Bar()
        a.Goobar(
            i, jj, kkkk, lll, mm, n)
    end sub
end class",
"class C
    sub Bar()
        a.Goobar(i, jj, kkkk,
                 lll, mm, n)
    end sub
end class",
"class C
    sub Bar()
        a.Goobar(
            i, jj, kkkk, lll,
            mm, n)
    end sub
end class",
"class C
    sub Bar()
        a.Goobar(i, jj, kkkk,
            lll, mm, n)
    end sub
end class")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsWrapping)>
        Public Async Function TestInObjectCreation1() As Task
            Await TestInRegularAndScript1Async(
"class C
    sub Goo()
        var v = [||]new Bar(a, b, c)
    end sub
end class",
"class C
    sub Goo()
        var v = new Bar(a,
                        b,
                        c)
    end sub
end class")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsWrapping)>
        Public Async Function TestInObjectCreation2() As Task
            Await TestInRegularAndScript1Async(
"class C
    sub Goo()
        var v = new Bar([||]a, b, c)
    end sub
end class",
"class C
    sub Goo()
        var v = new Bar(a,
                        b,
                        c)
    end sub
end class")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsWrapping)>
        Public Async Function TestInConstructorInitializer1() As Task
            Await TestInRegularAndScript1Async(
"class C
    public sub new()
        mybase.new([||]a, b, c)
    end sub
end class",
"class C
    public sub new()
        mybase.new(a,
                   b,
                   c)
    end sub
end class")
        End Function
    End Class
End Namespace
