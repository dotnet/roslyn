' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports Microsoft.CodeAnalysis.CodeRefactorings
Imports Microsoft.CodeAnalysis.Editor.VisualBasic.UnitTests.CodeRefactorings
Imports Microsoft.CodeAnalysis.VisualBasic.SplitOrMergeIfStatements

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.UnitTests.SplitOrMergeIfStatements
    <Trait(Traits.Feature, Traits.Features.CodeActionsSplitIntoConsecutiveIfStatements)>
    Public NotInheritable Class SplitIntoConsecutiveIfStatementsTests
        Inherits AbstractVisualBasicCodeActionTest

        Protected Overrides Function CreateCodeRefactoringProvider(workspace As Workspace, parameters As TestParameters) As CodeRefactoringProvider
            Return New VisualBasicSplitIntoConsecutiveIfStatementsCodeRefactoringProvider()
        End Function

        <Fact>
        Public Async Function SplitOnOrElseOperatorCaret1() As Task
            Await TestInRegularAndScriptAsync(
"class C
    sub M(a as boolean, b as boolean)
        if a [||]orelse b then
        end if
    end sub
end class",
"class C
    sub M(a as boolean, b as boolean)
        if a then
        ElseIf b Then
        end if
    end sub
end class")
        End Function

        <Fact>
        Public Async Function SplitOnOrElseOperatorCaret2() As Task
            Await TestInRegularAndScriptAsync(
"class C
    sub M(a as boolean, b as boolean)
        if a ore[||]lse b then
        end if
    end sub
end class",
"class C
    sub M(a as boolean, b as boolean)
        if a then
        ElseIf b Then
        end if
    end sub
end class")
        End Function

        <Fact>
        Public Async Function SplitOnOrElseOperatorCaret3() As Task
            Await TestInRegularAndScriptAsync(
"class C
    sub M(a as boolean, b as boolean)
        if a orelse[||] b then
        end if
    end sub
end class",
"class C
    sub M(a as boolean, b as boolean)
        if a then
        ElseIf b Then
        end if
    end sub
end class")
        End Function

        <Fact>
        Public Async Function SplitOnOrElseOperatorSelection() As Task
            Await TestInRegularAndScriptAsync(
"class C
    sub M(a as boolean, b as boolean)
        if a [|orelse|] b then
        end if
    end sub
end class",
"class C
    sub M(a as boolean, b as boolean)
        if a then
        ElseIf b Then
        end if
    end sub
end class")
        End Function

        <Fact>
        Public Async Function NotSplitOnOrElseOperatorPartialSelection() As Task
            Await TestMissingInRegularAndScriptAsync(
"class C
    sub M(a as boolean, b as boolean)
        if a [|or|]else b then
        end if
    end sub
end class")
        End Function

        <Fact>
        Public Async Function NotSplitOnOrElseOperatorOverreachingSelection() As Task
            Await TestMissingInRegularAndScriptAsync(
"class C
    sub M(a as boolean, b as boolean)
        if a[| orelse|] b then
        end if
    end sub
end class")
        End Function

        <Fact>
        Public Async Function NotSplitOnOperandCaret() As Task
            Await TestMissingInRegularAndScriptAsync(
"class C
    sub M(a as boolean, b as boolean)
        if a[||] orelse b then
        end if
    end sub
end class")
        End Function

        <Fact>
        Public Async Function NotSplitOnAndAlsoOperator() As Task
            Await TestMissingInRegularAndScriptAsync(
"class C
    sub M(a as boolean, b as boolean)
        if a [||]andalso b then
        end if
    end sub
end class")
        End Function

        <Fact>
        Public Async Function NotSplitOnBitwiseOrOperator() As Task
            Await TestMissingInRegularAndScriptAsync(
"class C
    sub M(a as boolean, b as boolean)
        if a [||]or b then
        end if
    end sub
end class")
        End Function

        <Fact>
        Public Async Function NotSplitOnOrElseOperatorOutsideIfStatement() As Task
            Await TestMissingInRegularAndScriptAsync(
"class C
    sub M(a as boolean, b as boolean)
        dim v = a [||]orelse b
    end sub
end class")
        End Function

        <Fact>
        Public Async Function NotSplitOnOrElseOperatorInIfStatementBody() As Task
            Await TestMissingInRegularAndScriptAsync(
"class C
    sub M(a as boolean, b as boolean)
        if a orelse b then
            a [||]orelse b
        end if
    end sub
end class")
        End Function

        <Fact>
        Public Async Function SplitWithChainedOrElseExpression1() As Task
            Await TestInRegularAndScriptAsync(
"class C
    sub M(a as boolean, b as boolean, c as boolean, d as boolean)
        if a [||]orelse b orelse c orelse d then
        end if
    end sub
end class",
"class C
    sub M(a as boolean, b as boolean, c as boolean, d as boolean)
        if a then
        ElseIf b orelse c orelse d Then
        end if
    end sub
end class")
        End Function

        <Fact>
        Public Async Function SplitWithChainedOrElseExpression2() As Task
            Await TestInRegularAndScriptAsync(
"class C
    sub M(a as boolean, b as boolean, c as boolean, d as boolean)
        if a orelse b [||]orelse c orelse d then
        end if
    end sub
end class",
"class C
    sub M(a as boolean, b as boolean, c as boolean, d as boolean)
        if a orelse b then
        ElseIf c orelse d Then
        end if
    end sub
end class")
        End Function

        <Fact>
        Public Async Function SplitWithChainedOrElseExpression3() As Task
            Await TestInRegularAndScriptAsync(
"class C
    sub M(a as boolean, b as boolean, c as boolean, d as boolean)
        if a orelse b orelse c [||]orelse d then
        end if
    end sub
end class",
"class C
    sub M(a as boolean, b as boolean, c as boolean, d as boolean)
        if a orelse b orelse c then
        ElseIf d Then
        end if
    end sub
end class")
        End Function

        <Fact>
        Public Async Function NotSplitInsideParentheses1() As Task
            Await TestMissingInRegularAndScriptAsync(
"class C
    sub M(a as boolean, b as boolean, c as boolean, d as boolean)
        if (a [||]orelse b) orelse c orelse d then
        end if
    end sub
end class")
        End Function

        <Fact>
        Public Async Function NotSplitInsideParentheses2() As Task
            Await TestMissingInRegularAndScriptAsync(
"class C
    sub M(a as boolean, b as boolean, c as boolean, d as boolean)
        if a orelse b orelse (c [||]orelse d) then
        end if
    end sub
end class")
        End Function

        <Fact>
        Public Async Function NotSplitInsideParentheses3() As Task
            Await TestMissingInRegularAndScriptAsync(
"class C
    sub M(a as boolean, b as boolean, c as boolean, d as boolean)
        if (a orelse b [||]orelse c orelse d) then
        end if
    end sub
end class")
        End Function

        <Fact>
        Public Async Function SplitWithOtherExpressionInsideParentheses1() As Task
            Await TestInRegularAndScriptAsync(
"class C
    sub M(a as boolean, b as boolean, c as boolean, d as boolean)
        if a [||]orelse (b orelse c) orelse d then
        end if
    end sub
end class",
"class C
    sub M(a as boolean, b as boolean, c as boolean, d as boolean)
        if a then
        ElseIf (b orelse c) orelse d Then
        end if
    end sub
end class")
        End Function

        <Fact>
        Public Async Function SplitWithOtherExpressionInsideParentheses2() As Task
            Await TestInRegularAndScriptAsync(
"class C
    sub M(a as boolean, b as boolean, c as boolean, d as boolean)
        if a orelse (b orelse c) [||]orelse d then
        end if
    end sub
end class",
"class C
    sub M(a as boolean, b as boolean, c as boolean, d as boolean)
        if a orelse (b orelse c) then
        ElseIf d Then
        end if
    end sub
end class")
        End Function

        <Fact>
        Public Async Function SplitWithMixedAndAlsoOrElseExpressions1() As Task
            Await TestInRegularAndScriptAsync(
"class C
    sub M(a as boolean, b as boolean, c as boolean, d as boolean)
        if a [||]orelse b andalso c then
        end if
    end sub
end class",
"class C
    sub M(a as boolean, b as boolean, c as boolean, d as boolean)
        if a then
        ElseIf b andalso c Then
        end if
    end sub
end class")
        End Function

        <Fact>
        Public Async Function SplitWithMixedAndAlsoOrElseExpressions2() As Task
            Await TestInRegularAndScriptAsync(
"class C
    sub M(a as boolean, b as boolean, c as boolean, d as boolean)
        if a andalso b [||]orelse c then
        end if
    end sub
end class",
"class C
    sub M(a as boolean, b as boolean, c as boolean, d as boolean)
        if a andalso b then
        ElseIf c Then
        end if
    end sub
end class")
        End Function

        <Fact>
        Public Async Function SplitWithStatement() As Task
            Await TestInRegularAndScriptAsync(
"class C
    sub M(a as boolean, b as boolean)
        if a [||]orelse b then
            System.Console.WriteLine(a orelse b)
        end if
    end sub
end class",
"class C
    sub M(a as boolean, b as boolean)
        if a then
            System.Console.WriteLine(a orelse b)
        ElseIf b Then
            System.Console.WriteLine(a orelse b)
        end if
    end sub
end class")
        End Function

        <Fact>
        Public Async Function SplitWithNestedIfStatement() As Task
            Await TestInRegularAndScriptAsync(
"class C
    sub M(a as boolean, b as boolean)
        if a [||]orelse b then
            if true
            end if
        end if
    end sub
end class",
"class C
    sub M(a as boolean, b as boolean)
        if a then
            if true
            end if
        ElseIf b Then

            if true
            end if
        end if
    end sub
end class")
        End Function

        <Fact>
        Public Async Function SplitWithElseStatement() As Task
            Await TestInRegularAndScriptAsync(
"class C
    sub M(a as boolean, b as boolean)
        if a [||]orelse b then
            System.Console.WriteLine()
        else
            System.Console.WriteLine(a orelse b)
        end if
    end sub
end class",
"class C
    sub M(a as boolean, b as boolean)
        if a then
            System.Console.WriteLine()
        ElseIf b Then
            System.Console.WriteLine()
        else
            System.Console.WriteLine(a orelse b)
        end if
    end sub
end class")
        End Function

        <Fact>
        Public Async Function SplitWithElseNestedIfStatement() As Task
            Await TestInRegularAndScriptAsync(
"class C
    sub M(a as boolean, b as boolean)
        if a [||]orelse b then
            System.Console.WriteLine()
        else
            if true
            end if
        end if
    end sub
end class",
"class C
    sub M(a as boolean, b as boolean)
        if a then
            System.Console.WriteLine()
        ElseIf b Then
            System.Console.WriteLine()
        else
            if true
            end if
        end if
    end sub
end class")
        End Function

        <Fact>
        Public Async Function SplitWithElseIfElse() As Task
            Await TestInRegularAndScriptAsync(
"class C
    sub M(a as boolean, b as boolean)
        if a [||]orelse b then
            System.Console.WriteLine()
        elseif a then
            System.Console.WriteLine(a)
        else
            System.Console.WriteLine(b)
        end if
    end sub
end class",
"class C
    sub M(a as boolean, b as boolean)
        if a then
            System.Console.WriteLine()
        ElseIf b Then
            System.Console.WriteLine()
        elseif a then
            System.Console.WriteLine(a)
        else
            System.Console.WriteLine(b)
        end if
    end sub
end class")
        End Function

        <Fact>
        Public Async Function SplitAsPartOfElseIfElse() As Task
            Await TestInRegularAndScriptAsync(
"class C
    sub M(a as boolean, b as boolean)
        if true then
            System.Console.WriteLine()
        elseif a [||]orelse b then
            System.Console.WriteLine(a)
        else
            System.Console.WriteLine(b)
        end if
    end sub
end class",
"class C
    sub M(a as boolean, b as boolean)
        if true then
            System.Console.WriteLine()
        elseif a then
            System.Console.WriteLine(a)
        ElseIf b Then
            System.Console.WriteLine(a)
        else
            System.Console.WriteLine(b)
        end if
    end sub
end class")
        End Function

        <Fact>
        Public Async Function SplitIntoSeparateStatementsIfControlFlowQuits1() As Task
            Await TestInRegularAndScriptAsync(
"class C
    sub M(a as boolean, b as boolean)
        if a [||]orelse b then
            return
        end if
    end sub
end class",
"class C
    sub M(a as boolean, b as boolean)
        if a then
            return
        end if

        If b Then
            return
        End If
    end sub
end class")
        End Function

        <Fact>
        Public Async Function SplitIntoSeparateStatementsIfControlFlowQuits2() As Task
            Await TestInRegularAndScriptAsync(
"class C
    sub M(a as boolean, b as boolean)
        if a [||]orelse b then
            throw new System.Exception()
        end if
    end sub
end class",
"class C
    sub M(a as boolean, b as boolean)
        if a then
            throw new System.Exception()
        end if

        If b Then
            throw new System.Exception()
        End If
    end sub
end class")
        End Function

        <Fact>
        Public Async Function SplitIntoSeparateStatementsIfControlFlowQuits3() As Task
            Await TestInRegularAndScriptAsync(
"class C
    sub M(a as boolean, b as boolean)
        while true
            if a [||]orelse b then
                continue while
            end if
        end while
    end sub
end class",
"class C
    sub M(a as boolean, b as boolean)
        while true
            if a then
                continue while
            end if

            If b Then
                continue while
            End If
        end while
    end sub
end class")
        End Function

        <Fact>
        Public Async Function SplitIntoSeparateStatementsIfControlFlowQuits4() As Task
            Await TestInRegularAndScriptAsync(
"class C
    sub M(a as boolean, b as boolean)
        while true
            if a [||]orelse b then
                if a then
                    continue while
                else
                    exit while
                end if
            end if
        end while
    end sub
end class",
"class C
    sub M(a as boolean, b as boolean)
        while true
            if a then
                if a then
                    continue while
                else
                    exit while
                end if
            end if

            If b Then

                if a then
                    continue while
                else
                    exit while
                end if
            End If
        end while
    end sub
end class")
        End Function

        <Fact>
        Public Async Function SplitIntoSeparateStatementsIfControlFlowQuitsInCaseBlock() As Task
            Await TestInRegularAndScriptAsync(
"class C
    sub M(a as boolean, b as boolean)
        select a
            case true
                if a [||]orelse b then
                    exit select
                end if
                
                exit select
        end select
    end sub
end class",
"class C
    sub M(a as boolean, b as boolean)
        select a
            case true
                if a then
                    exit select
                end if

                If b Then
                    exit select
                End If

                exit select
        end select
    end sub
end class")
        End Function

        <Fact>
        Public Async Function SplitNotIntoSeparateStatementsIfControlFlowContinues1() As Task
            Await TestInRegularAndScriptAsync(
"class C
    sub M(a as boolean, b as boolean)
        if a [||]orelse b then
            if a then
                return
            end if
        end if
    end sub
end class",
"class C
    sub M(a as boolean, b as boolean)
        if a then
            if a then
                return
            end if
        ElseIf b Then

            if a then
                return
            end if
        end if
    end sub
end class")
        End Function

        <Fact>
        Public Async Function SplitNotIntoSeparateStatementsIfControlFlowContinues2() As Task
            Await TestInRegularAndScriptAsync(
"class C
    sub M(a as boolean, b as boolean)
        if a [||]orelse b then
            while a
                exit while
            end while
        end if
    end sub
end class",
"class C
    sub M(a as boolean, b as boolean)
        if a then
            while a
                exit while
            end while
        ElseIf b Then

            while a
                exit while
            end while
        end if
    end sub
end class")
        End Function

        <Fact>
        Public Async Function SplitNotIntoSeparateStatementsIfControlFlowContinues3() As Task
            Await TestInRegularAndScriptAsync(
"class C
    sub M(a as boolean, b as boolean)
        while a <> b
            if a [||]orelse b then
                while a
                    continue while
                end while
            end if
        end while
    end sub
end class",
"class C
    sub M(a as boolean, b as boolean)
        while a <> b
            if a then
                while a
                    continue while
                end while
            ElseIf b Then

                while a
                    continue while
                end while
            end if
        end while
    end sub
end class")
        End Function

        <Fact>
        Public Async Function SplitNotIntoSeparateStatementsWithElseIfControlFlowQuits() As Task
            Await TestInRegularAndScriptAsync(
"class C
    sub M(a as boolean, b as boolean)
        if a [||]orelse b then
            return
        else
            return
        end if
    end sub
end class",
"class C
    sub M(a as boolean, b as boolean)
        if a then
            return
        ElseIf b Then
            return
        else
            return
        end if
    end sub
end class")
        End Function

        <Fact>
        Public Async Function SplitNotIntoSeparateStatementsWithElseIfIfControlFlowQuits() As Task
            Await TestInRegularAndScriptAsync(
"class C
    sub M(a as boolean, b as boolean)
        if a [||]orelse b then
            return
        else if a then
            return
        end if
    end sub
end class",
"class C
    sub M(a as boolean, b as boolean)
        if a then
            return
        ElseIf b Then
            return
        else if a then
            return
        end if
    end sub
end class")
        End Function

        <Fact>
        Public Async Function SplitNotIntoSeparateStatementsWithElseIfElseIfControlFlowQuits() As Task
            Await TestInRegularAndScriptAsync(
"class C
    sub M(a as boolean, b as boolean)
        if a [||]orelse b then
            return
        else if a then
            return
        else
            return
        end if
    end sub
end class",
"class C
    sub M(a as boolean, b as boolean)
        if a then
            return
        ElseIf b Then
            return
        else if a then
            return
        else
            return
        end if
    end sub
end class")
        End Function

        <Fact>
        Public Async Function SplitNotIntoSeparateStatementsAsPartOfElseIfIfControlFlowQuits() As Task
            Await TestInRegularAndScriptAsync(
"class C
    sub M(a as boolean, b as boolean)
        if true then
            return
        elseif a [||]orelse b then
            return
        end if
    end sub
end class",
"class C
    sub M(a as boolean, b as boolean)
        if true then
            return
        elseif a then
            return
        ElseIf b Then
            return
        end if
    end sub
end class")
        End Function
    End Class
End Namespace
