' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports Microsoft.CodeAnalysis.CodeRefactorings
Imports Microsoft.CodeAnalysis.Editor.UnitTests.Workspaces
Imports Microsoft.CodeAnalysis.VisualBasic.Wrapping

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.UnitTests.Wrapping
    <Trait(Traits.Feature, Traits.Features.CodeActionsWrapping)>
    Public Class ParameterWrappingTests
        Inherits AbstractWrappingTests

        Protected Overrides Function CreateCodeRefactoringProvider(workspace As EditorTestWorkspace, parameters As TestParameters) As CodeRefactoringProvider
            Return New VisualBasicWrappingCodeRefactoringProvider()
        End Function

        <Fact>
        Public Async Function TestMissingWithSyntaxError() As Task
            Await TestMissingAsync(
"class C
    sub Goobar([||]i as integer, j as integer {
    end sub
end class")
        End Function

        <Fact>
        Public Async Function TestAvailableWithSyntaxErrorAfter() As Task
            Await TestInRegularAndScript1Async(
"class C
    function Goobar([||]i as integer, j as integer) as
    end function
end class",
"class C
    function Goobar(i as integer,
                    j as integer) as
    end function
end class")
        End Function

        <Fact>
        Public Async Function TestMissingWithSelection() As Task
            Await TestMissingAsync(
"class C
    sub Goobar(i as [|integer|], j as integer)
    end sub
end class")
        End Function

        <Fact>
        Public Async Function TestMissingInBody() As Task
            Await TestMissingAsync(
"class C
    sub Goobar(i as integer, j as integer)
        [||]
    end sub
end class")
        End Function

        <Fact>
        Public Async Function TestMissingInAttributes() As Task
            Await TestMissingAsync(
"class C
    [||]<Attr>
    sub Goobar(i as integer, j as integer)
    end sub
end class")
        End Function

        <Fact>
        Public Async Function TestMissingWithSingleParameter() As Task
            Await TestMissingAsync(
"class C
    sub Goobar([||]i as integer)
    end sub
end class")
        End Function

        <Fact>
        Public Async Function TestMissingWithMultiLineParameter() As Task
            Await TestMissingAsync(
"class C
    sub Goobar([||]i as integer, optional j as integer =
        nothing)
    end sub
end class")
        End Function

        <Fact>
        Public Async Function TestInHeader1() As Task
            Await TestInRegularAndScript1Async(
"class C
    [||]sub Goobar(i as integer, j as integer)
    end sub
end class",
"class C
    sub Goobar(i as integer,
               j as integer)
    end sub
end class")
        End Function

        <Fact>
        Public Async Function TestInHeader2() As Task
            Await TestInRegularAndScript1Async(
"class C
    sub [||]Goobar(i as integer, j as integer)
    end sub
end class",
"class C
    sub Goobar(i as integer,
               j as integer)
    end sub
end class")
        End Function

        <Fact>
        Public Async Function TestInHeader3() As Task
            Await TestInRegularAndScript1Async(
"class C
    [||]public sub Goobar(i as integer, j as integer)
    end sub
end class",
"class C
    public sub Goobar(i as integer,
                      j as integer)
    end sub
end class")
        End Function

        <Fact>
        Public Async Function TestInHeader4() As Task
            Await TestInRegularAndScript1Async(
"class C
    public sub Goobar(i as integer, j as integer)[||]
    end sub
end class",
"class C
    public sub Goobar(i as integer,
                      j as integer)
    end sub
end class")
        End Function

        <Fact>
        Public Async Function TestTwoParamWrappingCases() As Task
            Await TestAllWrappingCasesAsync(
"class C
    sub Goobar([||]i as integer, j as integer)
    end sub
end class",
"class C
    sub Goobar(i as integer,
               j as integer)
    end sub
end class",
"class C
    sub Goobar(
            i as integer,
            j as integer)
    end sub
end class",
"class C
    sub Goobar(i as integer,
            j as integer)
    end sub
end class",
"class C
    sub Goobar(
            i as integer, j as integer)
    end sub
end class")
        End Function

        <Fact>
        Public Async Function TestThreeParamWrappingCases() As Task
            Await TestAllWrappingCasesAsync(
"class C
    sub Goobar([||]i as integer, j as integer, k as integer)
    end sub
end class",
"class C
    sub Goobar(i as integer,
               j as integer,
               k as integer)
    end sub
end class",
"class C
    sub Goobar(
            i as integer,
            j as integer,
            k as integer)
    end sub
end class",
"class C
    sub Goobar(i as integer,
            j as integer,
            k as integer)
    end sub
end class",
"class C
    sub Goobar(
            i as integer, j as integer, k as integer)
    end sub
end class")
        End Function

        <Fact>
        Public Async Function Test_AllOptions_NoInitialMatches() As Task
            Await TestAllWrappingCasesAsync(
"class C
    sub Goobar([||]
        i as integer,
            j as integer,
                k as integer)
    end sub
end class",
"class C
    sub Goobar(i as integer,
               j as integer,
               k as integer)
    end sub
end class",
"class C
    sub Goobar(
            i as integer,
            j as integer,
            k as integer)
    end sub
end class",
"class C
    sub Goobar(i as integer,
            j as integer,
            k as integer)
    end sub
end class",
"class C
    sub Goobar(i as integer, j as integer, k as integer)
    end sub
end class",
"class C
    sub Goobar(
            i as integer, j as integer, k as integer)
    end sub
end class")
        End Function

        <Fact>
        Public Async Function Test_LongWrapping_ShortIds() As Task
            Await TestAllWrappingCasesAsync(
"class C
    sub Goobar([||]
        i as integer, j as integer, k as integer, l as integer, m as integer,
        n as integer)
    end sub
end class",
GetIndentionColumn(45),
"class C
    sub Goobar(i as integer,
               j as integer,
               k as integer,
               l as integer,
               m as integer,
               n as integer)
    end sub
end class",
"class C
    sub Goobar(
            i as integer,
            j as integer,
            k as integer,
            l as integer,
            m as integer,
            n as integer)
    end sub
end class",
"class C
    sub Goobar(i as integer,
            j as integer,
            k as integer,
            l as integer,
            m as integer,
            n as integer)
    end sub
end class",
"class C
    sub Goobar(i as integer, j as integer, k as integer, l as integer, m as integer, n as integer)
    end sub
end class",
"class C
    sub Goobar(
            i as integer, j as integer, k as integer, l as integer, m as integer, n as integer)
    end sub
end class",
"class C
    sub Goobar(i as integer, j as integer,
               k as integer, l as integer,
               m as integer, n as integer)
    end sub
end class",
"class C
    sub Goobar(
            i as integer, j as integer,
            k as integer, l as integer,
            m as integer, n as integer)
    end sub
end class",
"class C
    sub Goobar(i as integer, j as integer,
            k as integer, l as integer,
            m as integer, n as integer)
    end sub
end class")
        End Function

        <Fact>
        Public Async Function Test_LongWrapping_VariadicLengthIds() As Task
            Await TestAllWrappingCasesAsync(
"class C
    sub Goobar([||]
        i as integer, jj as integer, kkkk as integer, llllllll as integer, mmmmmmmmmmmmmmmm as integer,
        nnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnn as integer)
    end sub
end class",
GetIndentionColumn(45),
"class C
    sub Goobar(i as integer,
               jj as integer,
               kkkk as integer,
               llllllll as integer,
               mmmmmmmmmmmmmmmm as integer,
               nnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnn as integer)
    end sub
end class",
"class C
    sub Goobar(
            i as integer,
            jj as integer,
            kkkk as integer,
            llllllll as integer,
            mmmmmmmmmmmmmmmm as integer,
            nnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnn as integer)
    end sub
end class",
"class C
    sub Goobar(i as integer,
            jj as integer,
            kkkk as integer,
            llllllll as integer,
            mmmmmmmmmmmmmmmm as integer,
            nnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnn as integer)
    end sub
end class",
"class C
    sub Goobar(i as integer, jj as integer, kkkk as integer, llllllll as integer, mmmmmmmmmmmmmmmm as integer, nnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnn as integer)
    end sub
end class",
"class C
    sub Goobar(
            i as integer, jj as integer, kkkk as integer, llllllll as integer, mmmmmmmmmmmmmmmm as integer, nnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnn as integer)
    end sub
end class",
"class C
    sub Goobar(i as integer, jj as integer,
               kkkk as integer,
               llllllll as integer,
               mmmmmmmmmmmmmmmm as integer,
               nnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnn as integer)
    end sub
end class",
"class C
    sub Goobar(
            i as integer, jj as integer,
            kkkk as integer,
            llllllll as integer,
            mmmmmmmmmmmmmmmm as integer,
            nnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnn as integer)
    end sub
end class",
"class C
    sub Goobar(i as integer, jj as integer,
            kkkk as integer,
            llllllll as integer,
            mmmmmmmmmmmmmmmm as integer,
            nnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnn as integer)
    end sub
end class")
        End Function

        <Fact>
        Public Async Function Test_DoNotOfferLongWrappingOptionThatAlreadyAppeared() As Task
            Await TestAllWrappingCasesAsync(
"class C
    sub Goobar([||]
        iiiii as integer, jjjjj as integer, kkkkk as integer, lllll as integer, mmmmm as integer,
        nnnnn as integer)
    end sub
end class",
GetIndentionColumn(48),
"class C
    sub Goobar(iiiii as integer,
               jjjjj as integer,
               kkkkk as integer,
               lllll as integer,
               mmmmm as integer,
               nnnnn as integer)
    end sub
end class",
"class C
    sub Goobar(
            iiiii as integer,
            jjjjj as integer,
            kkkkk as integer,
            lllll as integer,
            mmmmm as integer,
            nnnnn as integer)
    end sub
end class",
"class C
    sub Goobar(iiiii as integer,
            jjjjj as integer,
            kkkkk as integer,
            lllll as integer,
            mmmmm as integer,
            nnnnn as integer)
    end sub
end class",
"class C
    sub Goobar(iiiii as integer, jjjjj as integer, kkkkk as integer, lllll as integer, mmmmm as integer, nnnnn as integer)
    end sub
end class",
"class C
    sub Goobar(
            iiiii as integer, jjjjj as integer, kkkkk as integer, lllll as integer, mmmmm as integer, nnnnn as integer)
    end sub
end class",
"class C
    sub Goobar(
            iiiii as integer, jjjjj as integer,
            kkkkk as integer, lllll as integer,
            mmmmm as integer, nnnnn as integer)
    end sub
end class",
"class C
    sub Goobar(iiiii as integer,
            jjjjj as integer, kkkkk as integer,
            lllll as integer, mmmmm as integer,
            nnnnn as integer)
    end sub
end class")
        End Function

        <Fact>
        Public Async Function Test_DoNotOfferAllLongWrappingOptionThatAlreadyAppeared() As Task
            Await TestAllWrappingCasesAsync(
"class C
    sub Goobar([||]
        iiiii as integer, jjjjj as integer, kkkkk as integer, lllll as integer, mmmmm as integer,
        nnnnn as integer)
    end sub
end class",
GetIndentionColumn(20),
"class C
    sub Goobar(iiiii as integer,
               jjjjj as integer,
               kkkkk as integer,
               lllll as integer,
               mmmmm as integer,
               nnnnn as integer)
    end sub
end class",
"class C
    sub Goobar(
            iiiii as integer,
            jjjjj as integer,
            kkkkk as integer,
            lllll as integer,
            mmmmm as integer,
            nnnnn as integer)
    end sub
end class",
"class C
    sub Goobar(iiiii as integer,
            jjjjj as integer,
            kkkkk as integer,
            lllll as integer,
            mmmmm as integer,
            nnnnn as integer)
    end sub
end class",
"class C
    sub Goobar(iiiii as integer, jjjjj as integer, kkkkk as integer, lllll as integer, mmmmm as integer, nnnnn as integer)
    end sub
end class",
"class C
    sub Goobar(
            iiiii as integer, jjjjj as integer, kkkkk as integer, lllll as integer, mmmmm as integer, nnnnn as integer)
    end sub
end class")
        End Function

        <Fact>
        Public Async Function Test_LongWrapping_VariadicLengthIds2() As Task
            Await TestAllWrappingCasesAsync(
"class C
    sub Goobar([||]
        i as integer, jj as integer, kkkk as integer, lll as integer, mm as integer,
        n as integer)
    end sub
end class",
GetIndentionColumn(50),
"class C
    sub Goobar(i as integer,
               jj as integer,
               kkkk as integer,
               lll as integer,
               mm as integer,
               n as integer)
    end sub
end class",
"class C
    sub Goobar(
            i as integer,
            jj as integer,
            kkkk as integer,
            lll as integer,
            mm as integer,
            n as integer)
    end sub
end class",
"class C
    sub Goobar(i as integer,
            jj as integer,
            kkkk as integer,
            lll as integer,
            mm as integer,
            n as integer)
    end sub
end class",
"class C
    sub Goobar(i as integer, jj as integer, kkkk as integer, lll as integer, mm as integer, n as integer)
    end sub
end class",
"class C
    sub Goobar(
            i as integer, jj as integer, kkkk as integer, lll as integer, mm as integer, n as integer)
    end sub
end class",
"class C
    sub Goobar(i as integer, jj as integer,
               kkkk as integer, lll as integer,
               mm as integer, n as integer)
    end sub
end class",
"class C
    sub Goobar(
            i as integer, jj as integer,
            kkkk as integer, lll as integer,
            mm as integer, n as integer)
    end sub
end class",
"class C
    sub Goobar(i as integer, jj as integer,
            kkkk as integer, lll as integer,
            mm as integer, n as integer)
    end sub
end class")
        End Function

        <Fact>
        Public Async Function Test_DoNotOfferExistingOption1() As Task
            Await TestAllWrappingCasesAsync(
"class C
    sub Goobar([||]i as integer,
               jj as integer,
               kkkk as integer,
               lll as integer,
               mm as integer,
               n as integer)
    end sub
end class",
GetIndentionColumn(50),
"class C
    sub Goobar(
            i as integer,
            jj as integer,
            kkkk as integer,
            lll as integer,
            mm as integer,
            n as integer)
    end sub
end class",
"class C
    sub Goobar(i as integer,
            jj as integer,
            kkkk as integer,
            lll as integer,
            mm as integer,
            n as integer)
    end sub
end class",
"class C
    sub Goobar(i as integer, jj as integer, kkkk as integer, lll as integer, mm as integer, n as integer)
    end sub
end class",
"class C
    sub Goobar(
            i as integer, jj as integer, kkkk as integer, lll as integer, mm as integer, n as integer)
    end sub
end class",
"class C
    sub Goobar(i as integer, jj as integer,
               kkkk as integer, lll as integer,
               mm as integer, n as integer)
    end sub
end class",
"class C
    sub Goobar(
            i as integer, jj as integer,
            kkkk as integer, lll as integer,
            mm as integer, n as integer)
    end sub
end class",
"class C
    sub Goobar(i as integer, jj as integer,
            kkkk as integer, lll as integer,
            mm as integer, n as integer)
    end sub
end class")
        End Function

        <Fact>
        Public Async Function Test_DoNotOfferExistingOption2() As Task
            Await TestAllWrappingCasesAsync(
"class C
    sub Goobar([||]
            i as integer,
            jj as integer,
            kkkk as integer,
            lll as integer,
            mm as integer,
            n as integer)
    end sub
end class",
GetIndentionColumn(45),
"class C
    sub Goobar(i as integer,
               jj as integer,
               kkkk as integer,
               lll as integer,
               mm as integer,
               n as integer)
    end sub
end class",
"class C
    sub Goobar(i as integer,
            jj as integer,
            kkkk as integer,
            lll as integer,
            mm as integer,
            n as integer)
    end sub
end class",
"class C
    sub Goobar(i as integer, jj as integer, kkkk as integer, lll as integer, mm as integer, n as integer)
    end sub
end class",
"class C
    sub Goobar(
            i as integer, jj as integer, kkkk as integer, lll as integer, mm as integer, n as integer)
    end sub
end class",
"class C
    sub Goobar(i as integer, jj as integer,
               kkkk as integer,
               lll as integer, mm as integer,
               n as integer)
    end sub
end class",
"class C
    sub Goobar(
            i as integer, jj as integer,
            kkkk as integer, lll as integer,
            mm as integer, n as integer)
    end sub
end class",
"class C
    sub Goobar(i as integer, jj as integer,
            kkkk as integer, lll as integer,
            mm as integer, n as integer)
    end sub
end class")
        End Function

        <Fact>
        Public Async Function TestInConstructor() As Task
            Await TestInRegularAndScript1Async(
"class C
    public [||]sub new(i as integer, j as integer)
    end sub
end class",
"class C
    public sub new(i as integer,
                   j as integer)
    end sub
end class")
        End Function

        <Fact>
        Public Async Function TestInIndexer() As Task
            Await TestInRegularAndScript1Async(
"class C
    public readonly property [||]P(i as integer, j as integer) as integer
    end property
end class",
"class C
    public readonly property P(i as integer,
                               j as integer) as integer
    end property
end class")
        End Function

        <Fact>
        Public Async Function TestInOperator() As Task
            Await TestInRegularAndScript1Async(
"class C
    public shared operator [||]+(c1 as C, c2 as C) as integer
    end operator
end class",
"class C
    public shared operator +(c1 as C,
                             c2 as C) as integer
    end operator
end class")
        End Function

        <Fact>
        Public Async Function TestInDelegate() As Task
            Await TestInRegularAndScript1Async(
"class C
    public delegate function [||]D(c1 as C, c2 as C) as integer
end class",
"class C
    public delegate function D(c1 as C,
                               c2 as C) as integer
end class")
        End Function

        <Fact>
        Public Async Function TestInParenthesizedLambda() As Task
            Await TestInRegularAndScript1Async(
"class C
    sub Goobar()
        dim v = sub ([||]c as C, d as C)
                end sub
    end sub
end class",
"class C
    sub Goobar()
        dim v = sub (c as C,
                     d as C)
                end sub
    end sub
end class")
        End Function

        <Fact>
        Public Async Function TestInParenthesizedLambda2() As Task
            Await TestInRegularAndScript1Async(
"class C
    sub Goobar()
        dim v = sub ([||]c, d)
                end sub
    end sub
end class",
"class C
    sub Goobar()
        dim v = sub (c,
                     d)
                end sub
    end sub
end class")
        End Function

        <Fact>
        Public Async Function TestNotOnSimpleLambda() As Task
            Await TestMissingAsync(
"class C
    sub Goobar()
    {
        var v = [||]c => {
        end function
    end sub
end class")
        End Function

        <Fact, WorkItem("https://github.com/dotnet/roslyn/issues/63732")>
        Public Async Function TestMissingStartToken1() As Task
            Await TestMissingAsync(
"class C
    sub Goobar [||])
    end sub
end class")
        End Function

        <Fact, WorkItem("https://github.com/dotnet/roslyn/issues/63732")>
        Public Async Function TestMissingStartToken2() As Task
            Await TestMissingAsync(
"class C
    sub Goobar [||]i as integer, j as integer)
    end sub
end class")
        End Function

        <Fact, WorkItem("https://github.com/dotnet/roslyn/issues/63732")>
        Public Async Function TestMissingEndToken1() As Task
            Await TestMissingAsync(
"class C
    sub Goobar([||]
    end sub
end class")
        End Function

        <Fact, WorkItem("https://github.com/dotnet/roslyn/issues/63732")>
        Public Async Function TestMissingEndToken2() As Task
            Await TestMissingAsync(
"class C
    sub Goobar([||]i as integer, j as integer
    end sub
end class")
        End Function
    End Class
End Namespace
