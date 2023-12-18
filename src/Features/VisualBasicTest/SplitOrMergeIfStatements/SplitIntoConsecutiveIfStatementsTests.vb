' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

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

        <Theory>
        <InlineData("a [||]orelse b")>
        <InlineData("a ore[||]lse b")>
        <InlineData("a orelse[||] b")>
        <InlineData("a [|orelse|] b")>
        Public Async Function SplitOnOrElseOperatorSpans(condition As String) As Task
            Await TestInRegularAndScriptAsync(
$"class C
    sub M(a as boolean, b as boolean)
        if {condition} then
        end if
    end sub
end class",
"class C
    sub M(a as boolean, b as boolean)
        if a then
        ElseIf b then
        end if
    end sub
end class")
        End Function

        <Theory>
        <InlineData("a [|or|]else b")>
        <InlineData("a[| orelse|] b")>
        <InlineData("a[||] orelse b")>
        Public Async Function NotSplitOnOrElseOperatorSpans(condition As String) As Task
            Await TestMissingInRegularAndScriptAsync(
$"class C
    sub M(a as boolean, b as boolean)
        if {condition} then
        end if
    end sub
end class")
        End Function

        <Fact>
        Public Async Function NotSplitOnIfKeyword() As Task
            Await TestMissingInRegularAndScriptAsync(
"class C
    sub M(a as boolean, b as boolean)
        [||]if a orelse b then
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
        Public Async Function NotSplitOnSingleLineIf() As Task
            Await TestMissingInRegularAndScriptAsync(
"class C
    sub M(a as boolean, b as boolean)
        if a [||]orelse b then System.Console.WriteLine()
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
        ElseIf b orelse c orelse d then
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
        ElseIf c orelse d then
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
        ElseIf d then
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
        ElseIf (b orelse c) orelse d then
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
        ElseIf d then
        end if
    end sub
end class")
        End Function

        <Fact>
        Public Async Function SplitWithMixedAndAlsoExpression1() As Task
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
        ElseIf b andalso c then
        end if
    end sub
end class")
        End Function

        <Fact>
        Public Async Function SplitWithMixedAndAlsoExpression2() As Task
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
        ElseIf c then
        end if
    end sub
end class")
        End Function

        <Fact>
        Public Async Function NotSplitWithMixedExclusiveOrExpression1() As Task
            Await TestMissingInRegularAndScriptAsync(
"class C
    sub M(a as boolean, b as boolean, c as boolean, d as boolean)
        if a [||]orelse b xor c then
        end if
    end sub
end class")
        End Function

        <Fact>
        Public Async Function NotSplitWithMixedExclusiveOrExpression2() As Task
            Await TestMissingInRegularAndScriptAsync(
"class C
    sub M(a as boolean, b as boolean, c as boolean, d as boolean)
        if a xor b [||]orelse c then
        end if
    end sub
end class")
        End Function

        <Fact>
        Public Async Function SplitWithMixedExclusiveOrExpressionInsideParentheses1() As Task
            Await TestInRegularAndScriptAsync(
"class C
    sub M(a as boolean, b as boolean, c as boolean, d as boolean)
        if a [||]orelse (b xor c) then
        end if
    end sub
end class",
"class C
    sub M(a as boolean, b as boolean, c as boolean, d as boolean)
        if a then
        ElseIf (b xor c) then
        end if
    end sub
end class")
        End Function

        <Fact>
        Public Async Function SplitWithMixedExclusiveOrExpressionInsideParentheses2() As Task
            Await TestInRegularAndScriptAsync(
"class C
    sub M(a as boolean, b as boolean, c as boolean, d as boolean)
        if (a xor b) [||]orelse c then
        end if
    end sub
end class",
"class C
    sub M(a as boolean, b as boolean, c as boolean, d as boolean)
        if (a xor b) then
        ElseIf c then
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
        ElseIf b then
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
        ElseIf b then
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
        ElseIf b then
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
        ElseIf b then
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
        ElseIf b then
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
        elseif b then
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

        if b then
            return
        end if
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

        if b then
            throw new System.Exception()
        end if
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

            if b then
                continue while
            end if
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

            if b then
                if a then
                    continue while
                else
                    exit while
                end if
            end if
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

                if b then
                    exit select
                end if

                exit select
        end select
    end sub
end class")
        End Function

        <Fact>
        Public Async Function SplitNotIntoSeparateStatementsIfControlFlowContinues1() As Task
            ' Even though there are no statements inside, we still can't split this into separate statements
            ' because it would change the semantics from short-circuiting to always evaluating the second condition,
            ' breaking code like 'If a Is Nothing OrElse a.InstanceMethod()'.
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
        ElseIf b then
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
        ElseIf b then
            if a then
                return
            end if
        end if
    end sub
end class")
        End Function

        <Fact>
        Public Async Function SplitNotIntoSeparateStatementsIfControlFlowContinues3() As Task
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
        ElseIf b then
            while a
                exit while
            end while
        end if
    end sub
end class")
        End Function

        <Fact>
        Public Async Function SplitNotIntoSeparateStatementsIfControlFlowContinues4() As Task
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
            ElseIf b then
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
        ElseIf b then
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
        ElseIf b then
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
        ElseIf b then
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
        elseif b then
            return
        end if
    end sub
end class")
        End Function
    End Class
End Namespace
