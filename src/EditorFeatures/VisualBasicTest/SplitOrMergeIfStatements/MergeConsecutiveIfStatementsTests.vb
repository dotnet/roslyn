' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports Microsoft.CodeAnalysis.CodeRefactorings
Imports Microsoft.CodeAnalysis.Editor.VisualBasic.UnitTests.CodeRefactorings
Imports Microsoft.CodeAnalysis.VisualBasic.SplitOrMergeIfStatements

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.UnitTests.SplitOrMergeIfStatements
    <Trait(Traits.Feature, Traits.Features.CodeActionsMergeConsecutiveIfStatements)>
    Public NotInheritable Class MergeConsecutiveIfStatementsTests
        Inherits AbstractVisualBasicCodeActionTest

        Protected Overrides Function CreateCodeRefactoringProvider(workspace As Workspace, parameters As TestParameters) As CodeRefactoringProvider
            Return New VisualBasicMergeConsecutiveIfStatementsCodeRefactoringProvider()
        End Function

        <Fact>
        Public Async Function MergedOnElseIfCaret1() As Task
            Await TestInRegularAndScriptAsync(
"class C
    sub M(a as boolean, b as boolean)
        if a then
        [||]elseif b then
        end if
    end sub
end class",
"class C
    sub M(a as boolean, b as boolean)
        if a OrElse b then
        end if
    end sub
end class")
        End Function

        <Fact>
        Public Async Function MergedOnElseIfCaret2() As Task
            Await TestInRegularAndScriptAsync(
"class C
    sub M(a as boolean, b as boolean)
        if a then
        el[||]seif b then
        end if
    end sub
end class",
"class C
    sub M(a as boolean, b as boolean)
        if a OrElse b then
        end if
    end sub
end class")
        End Function

        <Fact>
        Public Async Function MergedOnElseIfCaret3() As Task
            Await TestInRegularAndScriptAsync(
"class C
    sub M(a as boolean, b as boolean)
        if a then
        elseif[||] b then
        end if
    end sub
end class",
"class C
    sub M(a as boolean, b as boolean)
        if a OrElse b then
        end if
    end sub
end class")
        End Function

        <Fact>
        Public Async Function MergedOnElseIfCaret4() As Task
            Await TestInRegularAndScriptAsync(
"class C
    sub M(a as boolean, b as boolean)
        if a then
        elseif b [||]then
        end if
    end sub
end class",
"class C
    sub M(a as boolean, b as boolean)
        if a OrElse b then
        end if
    end sub
end class")
        End Function

        <Fact>
        Public Async Function MergedOnElseIfCaret5() As Task
            Await TestInRegularAndScriptAsync(
"class C
    sub M(a as boolean, b as boolean)
        if a then
        elseif b then[||]
        end if
    end sub
end class",
"class C
    sub M(a as boolean, b as boolean)
        if a OrElse b then
        end if
    end sub
end class")
        End Function

        <Fact>
        Public Async Function MergedOnElseIfElseIfKeywordSelection() As Task
            Await TestInRegularAndScriptAsync(
"class C
    sub M(a as boolean, b as boolean)
        if a then
        [|elseif|] b then
        end if
    end sub
end class",
"class C
    sub M(a as boolean, b as boolean)
        if a OrElse b then
        end if
    end sub
end class")
        End Function

        <Fact>
        Public Async Function MergedOnElseIfStatementSelection1() As Task
            Await TestInRegularAndScriptAsync(
"class C
    sub M(a as boolean, b as boolean)
        if a then
        [|elseif b then|]
        end if
    end sub
end class",
"class C
    sub M(a as boolean, b as boolean)
        if a OrElse b then
        end if
    end sub
end class")
        End Function

        <Fact>
        Public Async Function MergedOnElseIfStatementSelection2() As Task
            Await TestInRegularAndScriptAsync(
"class C
    sub M(a as boolean, b as boolean)
        if a then
[|        elseif b then
|]        end if
    end sub
end class",
"class C
    sub M(a as boolean, b as boolean)
        if a OrElse b then
        end if
    end sub
end class")
        End Function

        <Fact>
        Public Async Function MergedOnElseIfFullSelection() As Task
            Await TestInRegularAndScriptAsync(
"class C
    sub M(a as boolean, b as boolean)
        if a then
            return
[|        elseif b then
            return
|]        end if
    end sub
end class",
"class C
    sub M(a as boolean, b as boolean)
        if a OrElse b then
            return
        end if
    end sub
end class")
        End Function

        <Fact>
        Public Async Function NotMergedOnElseIfThenKeywordSelection() As Task
            Await TestMissingInRegularAndScriptAsync(
"class C
    sub M(a as boolean, b as boolean)
        if a then
        elseif b [|then|]
        end if
    end sub
end class")
        End Function

        <Fact>
        Public Async Function NotMergedOnElseIfElseIfKeywordPartialSelection() As Task
            Await TestMissingInRegularAndScriptAsync(
"class C
    sub M(a as boolean, b as boolean)
        if a then
        [|else|]if b then
        end if
    end sub
end class")
        End Function

        <Fact>
        Public Async Function NotMergedOnElseIfOverreachingSelection1() As Task
            Await TestMissingInRegularAndScriptAsync(
"class C
    sub M(a as boolean, b as boolean)
        if a then
        [|elseif b|] then
        end if
    end sub
end class")
        End Function

        <Fact>
        Public Async Function NotMergedOnElseIfOverreachingSelection2() As Task
            Await TestMissingInRegularAndScriptAsync(
"class C
    sub M(a as boolean, b as boolean)
        if a then
        [|elseif b then
        |]end if
    end sub
end class")
        End Function

        <Fact>
        Public Async Function NotMergedOnElseIfConditionSelection() As Task
            Await TestMissingInRegularAndScriptAsync(
"class C
    sub M(a as boolean, b as boolean)
        if a then
        elseif [|b|] then
        end if
    end sub
end class")
        End Function

        <Fact>
        Public Async Function NotMergedOnElseIfBodyStatementSelection() As Task
            Await TestMissingInRegularAndScriptAsync(
"class C
    sub M(a as boolean, b as boolean)
        if a then
            return
        elseif b then
            [|return|]
        end if
    end sub
end class")
        End Function

        <Fact>
        Public Async Function NotMergedOnEndIfStatementSelection() As Task
            Await TestMissingInRegularAndScriptAsync(
"class C
    sub M(a as boolean, b as boolean)
        if a then
        elseif b then
        [|end if|]
    end sub
end class")
        End Function

        <Fact>
        Public Async Function NotMergedOnElseIfConditionCaret() As Task
            Await TestMissingInRegularAndScriptAsync(
"class C
    sub M(a as boolean, b as boolean)
        if a then
        elseif [||]b then
        end if
    end sub
end class")
        End Function

        <Fact>
        Public Async Function NotMergedOnEndIfStatementCaret() As Task
            Await TestMissingInRegularAndScriptAsync(
"class C
    sub M(a as boolean, b as boolean)
        if a then
        elseif b then
        [||]end if
    end sub
end class")
        End Function

        <Fact>
        Public Async Function NotMergedOnParentIf() As Task
            Await TestMissingInRegularAndScriptAsync(
"class C
    sub M(a as boolean, b as boolean)
        [||]if a then
        elseif b then
        end if
    end sub
end class")
        End Function

        <Fact>
        Public Async Function NotMergedOnSingleIf() As Task
            Await TestMissingInRegularAndScriptAsync(
"class C
    sub M(a as boolean, b as boolean)
        [||]if b then
        end if
    end sub
end class")
        End Function

        <Fact>
        Public Async Function MergedWithOrElseExpressions() As Task
            Await TestInRegularAndScriptAsync(
"class C
    sub M(a as boolean, b as boolean, c as boolean, d as boolean)
        if a orelse b then
        [||]elseif c orelse d then
        end if
    end sub
end class",
"class C
    sub M(a as boolean, b as boolean, c as boolean, d as boolean)
        if a orelse b OrElse c orelse d then
        end if
    end sub
end class")
        End Function

        <Fact>
        Public Async Function MergedWithAndAlsoExpressionNotParenthesized1() As Task
            Await TestInRegularAndScriptAsync(
"class C
    sub M(a as boolean, b as boolean, c as boolean, d as boolean)
        if a andalso b then
        [||]elseif c orelse d then
        end if
    end sub
end class",
"class C
    sub M(a as boolean, b as boolean, c as boolean, d as boolean)
        if a andalso b OrElse c orelse d then
        end if
    end sub
end class")
        End Function

        <Fact>
        Public Async Function MergedWithAndAlsoExpressionNotParenthesized2() As Task
            Await TestInRegularAndScriptAsync(
"class C
    sub M(a as boolean, b as boolean, c as boolean, d as boolean)
        if a orelse b then
        [||]elseif c andalso d then
        end if
    end sub
end class",
"class C
    sub M(a as boolean, b as boolean, c as boolean, d as boolean)
        if a orelse b OrElse c andalso d then
        end if
    end sub
end class")
        End Function

        <Fact>
        Public Async Function MergedWithExclusiveOrExpressionParenthesized1() As Task
            Await TestInRegularAndScriptAsync(
"class C
    sub M(a as boolean, b as boolean, c as boolean, d as boolean)
        if a xor b then
        [||]elseif c = d then
        end if
    end sub
end class",
"class C
    sub M(a as boolean, b as boolean, c as boolean, d as boolean)
        if (a xor b) OrElse c = d then
        end if
    end sub
end class")
        End Function

        <Fact>
        Public Async Function MergedWithExclusiveOrExpressionParenthesized2() As Task
            Await TestInRegularAndScriptAsync(
"class C
    sub M(a as boolean, b as boolean, c as boolean, d as boolean)
        if a = b then
        [||]elseif c xor d then
        end if
    end sub
end class",
"class C
    sub M(a as boolean, b as boolean, c as boolean, d as boolean)
        if a = b OrElse (c xor d) then
        end if
    end sub
end class")
        End Function

        <Fact>
        Public Async Function MergedWithParentWithStatements() As Task
            Await TestInRegularAndScriptAsync(
"class C
    sub M(a as boolean, b as boolean)
        if a then
            System.Console.WriteLine(a)
            System.Console.WriteLine(b)
        [||]elseif b then
            System.Console.WriteLine(a)
            System.Console.WriteLine(b)
        end if
    end sub
end class",
"class C
    sub M(a as boolean, b as boolean)
        if a OrElse b then
            System.Console.WriteLine(a)
            System.Console.WriteLine(b)
        end if
    end sub
end class")
        End Function

        <Fact>
        Public Async Function NotMergedWithParentWithUnmatchingStatements1() As Task
            Await TestMissingInRegularAndScriptAsync(
"class C
    sub M(a as boolean, b as boolean)
        if a then
            System.Console.WriteLine(a)
            System.Console.WriteLine(b)
        [||]elseif b then
            System.Console.WriteLine(a)
            System.Console.WriteLine(a)
        end if
    end sub
end class")
        End Function

        <Fact>
        Public Async Function NotMergedWithParentWithUnmatchingStatements2() As Task
            Await TestMissingInRegularAndScriptAsync(
"class C
    sub M(a as boolean, b as boolean)
        if a then
            System.Console.WriteLine(a)
            System.Console.WriteLine(b)
        [||]elseif b then
            System.Console.WriteLine(a)
        end if
    end sub
end class")
        End Function

        <Fact>
        Public Async Function NotMergedWithParentWithUnmatchingStatements3() As Task
            Await TestMissingInRegularAndScriptAsync(
"class C
    sub M(a as boolean, b as boolean)
        if a then
            System.Console.WriteLine(a)
        [||]elseif b then
            System.Console.WriteLine(a)
            System.Console.WriteLine(b)
        end if
    end sub
end class")
        End Function

        <Fact>
        Public Async Function NotMergedWithParentWithUnmatchingStatements4() As Task
            ' Do not consider the using statement to be a simple block (as might be suggested by some language-agnostic helpers).
            Await TestMissingInRegularAndScriptAsync(
"class C
    sub M(a as boolean, b as boolean)
        if a then
            System.Console.WriteLine(a)
        [||]elseif b then
            using nothing
                System.Console.WriteLine(a)
            end using
        end if
    end sub
end class")
        End Function

        <Fact>
        Public Async Function MergedWithParentWithElseStatements() As Task
            Await TestInRegularAndScriptAsync(
"class C
    sub M(a as boolean, b as boolean)
        if a then
            System.Console.WriteLine()
        [||]elseif b then
            System.Console.WriteLine()
        else
            System.Console.WriteLine(a)
            System.Console.WriteLine(b)
        end if
    end sub
end class",
"class C
    sub M(a as boolean, b as boolean)
        if a OrElse b then
            System.Console.WriteLine()
        else
            System.Console.WriteLine(a)
            System.Console.WriteLine(b)
        end if
    end sub
end class")
        End Function

        <Fact>
        Public Async Function MergedWithParentWithElseNestedIfStatements() As Task
            Await TestInRegularAndScriptAsync(
"class C
    sub M(a as boolean, b as boolean)
        if a then
            System.Console.WriteLine()
        [||]elseif b then
            System.Console.WriteLine()
        elseif a then
            System.Console.WriteLine(a)
            System.Console.WriteLine(b)
        end if
    end sub
end class",
"class C
    sub M(a as boolean, b as boolean)
        if a OrElse b then
            System.Console.WriteLine()
        elseif a then
            System.Console.WriteLine(a)
            System.Console.WriteLine(b)
        end if
    end sub
end class")
        End Function

        <Fact>
        Public Async Function MergedWithParentWithElseIfElse() As Task
            Await TestInRegularAndScriptAsync(
"class C
    sub M(a as boolean, b as boolean)
        if a then
            System.Console.WriteLine()
        [||]elseif b then
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
        if a OrElse b then
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
        Public Async Function MergedWithParentPartOfElseIf() As Task
            Await TestInRegularAndScriptAsync(
"class C
    sub M(a as boolean, b as boolean)
        if a then
            System.Console.WriteLine()
        elseif b then
            System.Console.WriteLine(a)
        [||]elseif a then
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
        elseif b OrElse a then
            System.Console.WriteLine(a)
        else
            System.Console.WriteLine(b)
        end if
    end sub
end class")
        End Function

        <Fact>
        Public Async Function MergedWithPreviousStatementIfContainsNoStatements() As Task
            Await TestInRegularAndScriptAsync(
"class C
    sub M(a as boolean, b as boolean)
        if a then
        end if

        [||]if b then
        end if
    end sub
end class",
"class C
    sub M(a as boolean, b as boolean)
        if a OrElse b then
        end if
    end sub
end class")
        End Function

        <Fact>
        Public Async Function MergedWithPreviousStatementIfControlFlowQuits1() As Task
            Await TestInRegularAndScriptAsync(
"class C
    sub M(a as boolean, b as boolean)
        if a then
            return
        end if

        [||]if b then
            return
        end if
    end sub
end class",
"class C
    sub M(a as boolean, b as boolean)
        if a OrElse b then
            return
        end if
    end sub
end class")
        End Function

        <Fact>
        Public Async Function MergedWithPreviousStatementIfControlFlowQuits2() As Task
            Await TestInRegularAndScriptAsync(
"class C
    sub M(a as boolean, b as boolean)
        if a then
            throw new System.Exception()
        end if

        [||]if b then
            throw new System.Exception()
        end if
    end sub
end class",
"class C
    sub M(a as boolean, b as boolean)
        if a OrElse b then
            throw new System.Exception()
        end if
    end sub
end class")
        End Function

        <Fact>
        Public Async Function MergedWithPreviousStatementIfControlFlowQuits3() As Task
            Await TestInRegularAndScriptAsync(
"class C
    sub M(a as boolean, b as boolean)
        while true
            if a then
                continue while
            end if

            [||]if b then
                continue while
            end if
        end while
    end sub
end class",
"class C
    sub M(a as boolean, b as boolean)
        while true
            if a OrElse b then
                continue while
            end if
        end while
    end sub
end class")
        End Function

        <Fact>
        Public Async Function MergedWithPreviousStatementIfControlFlowQuits4() As Task
            Await TestInRegularAndScriptAsync(
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

            [||]if b then
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
            if a OrElse b then
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
        Public Async Function MergedWithPreviousStatementIfControlFlowQuitsInCaseBlock() As Task
            Await TestInRegularAndScriptAsync(
"class C
    sub M(a as boolean, b as boolean)
        select a
            case true
                if a then
                    exit select
                end if

                [||]if b then
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
                if a OrElse b then
                    exit select
                end if

                exit select
        end select
    end sub
end class")
        End Function

        <Fact>
        Public Async Function NotMergedWithPreviousStatementIfControlFlowContinues1() As Task
            Await TestMissingInRegularAndScriptAsync(
"class C
    sub M(a as boolean, b as boolean)
        if a then
            System.Console.WriteLine()
        end if

        [||]if b then
            System.Console.WriteLine()
        end if
    end sub
end class")
        End Function

        <Fact>
        Public Async Function NotMergedWithPreviousStatementIfControlFlowContinues2() As Task
            Await TestMissingInRegularAndScriptAsync(
"class C
    sub M(a as boolean, b as boolean)
        if a then
            if a then
                return
            end if
        end if

        [||]if b then
            if a then
                return
            end if
        end if
    end sub
end class")
        End Function

        <Fact>
        Public Async Function NotMergedWithPreviousStatementIfControlFlowContinues3() As Task
            Await TestMissingInRegularAndScriptAsync(
"class C
    sub M(a as boolean, b as boolean)
        if a then
            while a
                exit while
            end while
        end if

        [||]if b then
            while a
                exit while
            end while
        end if
    end sub
end class")
        End Function

        <Fact>
        Public Async Function NotMergedWithPreviousStatementIfControlFlowContinues4() As Task
            Await TestMissingInRegularAndScriptAsync(
"class C
    sub M(a as boolean, b as boolean)
        while a <> b
            if a then
                while a
                    continue while
                end while
            end if

            [||]if b then
                while a
                    continue while
                end while
            end if
        end while
    end sub
end class")
        End Function

        <Fact>
        Public Async Function NotMergedWithPreviousStatementWithUnmatchingStatementsIfControlFlowQuits() As Task
            Await TestMissingInRegularAndScriptAsync(
"class C
    sub M(a as boolean, b as boolean)
        if a then
            return
        end if

        [||]if b then
            throw new System.Exception()
        end if
    end sub
end class")
        End Function

        <Fact>
        Public Async Function NotMergedWithPreviousStatementWithElseClauseIfControlFlowQuits1() As Task
            Await TestMissingInRegularAndScriptAsync(
"class C
    sub M(a as boolean, b as boolean)
        if a then
            return
        end if

        [||]if b then
            return
        else
            return
        end if
    end sub
end class")
        End Function

        <Fact>
        Public Async Function NotMergedWithPreviousStatementWithElseIfClauseIfControlFlowQuits1() As Task
            Await TestMissingInRegularAndScriptAsync(
"class C
    sub M(a as boolean, b as boolean)
        if a then
            return
        end if

        [||]if b then
            return
        elseif true then
            return
        end if
    end sub
end class")
        End Function

        <Fact>
        Public Async Function NotMergedWithPreviousStatementWithElseClauseIfControlFlowQuits2() As Task
            Await TestMissingInRegularAndScriptAsync(
"class C
    sub M(a as boolean, b as boolean)
        if a then
            return
        else
            return
        end if

        [||]if b then
            return
        end if
    end sub
end class")
        End Function

        <Fact>
        Public Async Function NotMergedWithPreviousStatementWithElseIfClauseIfControlFlowQuits2() As Task
            Await TestMissingInRegularAndScriptAsync(
"class C
    sub M(a as boolean, b as boolean)
        if a then
            return
        elseif true then
            return
        end if

        [||]if b then
            return
        end if
    end sub
end class")
        End Function

        <Fact>
        Public Async Function NotMergedWithPreviousStatementWithElseClauseIfControlFlowQuits3() As Task
            Await TestMissingInRegularAndScriptAsync(
"class C
    sub M(a as boolean, b as boolean)
        if a then
            return
        else
            return
        end if

        [||]if b then
            return
        else
            return
        end if
    end sub
end class")
        End Function

        <Fact>
        Public Async Function NotMergedWithPreviousStatementWithElseIfClauseIfControlFlowQuits3() As Task
            Await TestMissingInRegularAndScriptAsync(
"class C
    sub M(a as boolean, b as boolean)
        if a then
            return
        elseif true then
            return
        end if

        [||]if b then
            return
        elseif true then
            return
        end if
    end sub
end class")
        End Function

        <Fact>
        Public Async Function NotMergedWithPreviousStatementAsPartOfElseIfIfControlFlowQuits() As Task
            Await TestMissingInRegularAndScriptAsync(
"class C
    sub M(a as boolean, b as boolean)
        if a then
            return
        end if

        if a then
        [||]elseif b then
            return
        end if
    end sub
end class")
        End Function
    End Class
End Namespace
