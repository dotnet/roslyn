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

        <Theory>
        <InlineData("[||]elseif b then")>
        <InlineData("el[||]seif b then")>
        <InlineData("elseif[||] b then")>
        <InlineData("elseif b [||]then")>
        <InlineData("elseif b th[||]en")>
        <InlineData("elseif b then[||]")>
        <InlineData("[|elseif|] b then")>
        <InlineData("[|elseif b then|]")>
        Public Async Function MergedOnElseIfSpans(elseIfLine As String) As Task
            Await TestInRegularAndScriptAsync(
$"class C
    sub M(a as boolean, b as boolean)
        if a then
        {elseIfLine}
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
        Public Async Function MergedOnElseIfExtendedStatementSelection() As Task
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
        Public Async Function MergedOnElseIfFullSelectionWithoutElseClause() As Task
            Await TestInRegularAndScriptAsync(
"class C
    sub M(a as boolean, b as boolean)
        if a then
            System.Console.WriteLine()
        [|elseif b then
            System.Console.WriteLine()|]
        else
        end if
    end sub
end class",
"class C
    sub M(a as boolean, b as boolean)
        if a OrElse b then
            System.Console.WriteLine()
        else
        end if
    end sub
end class")
        End Function

        <Fact>
        Public Async Function MergedOnElseIfExtendedFullSelectionWithoutElseClause() As Task
            Await TestInRegularAndScriptAsync(
"class C
    sub M(a as boolean, b as boolean)
        if a then
            System.Console.WriteLine()
[|        elseif b then
            System.Console.WriteLine()
|]        else
        end if
    end sub
end class",
"class C
    sub M(a as boolean, b as boolean)
        if a OrElse b then
            System.Console.WriteLine()
        else
        end if
    end sub
end class")
        End Function

        <Fact>
        Public Async Function NotMergedOnElseIfFullSelectionWithElseClause() As Task
            Await TestMissingInRegularAndScriptAsync(
"class C
    sub M(a as boolean, b as boolean)
        if a then
            System.Console.WriteLine()
        [|elseif b then
            System.Console.WriteLine()
        else
        end if|]
    end sub
end class")
        End Function

        <Fact>
        Public Async Function NotMergedOnElseIfExtendedFullSelectionWithElseClause() As Task
            Await TestMissingInRegularAndScriptAsync(
"class C
    sub M(a as boolean, b as boolean)
        if a then
            System.Console.WriteLine()
[|        elseif b then
            System.Console.WriteLine()
        else
        end if
|]    end sub
end class")
        End Function

        <Theory>
        <InlineData("elseif [||]b then")>
        <InlineData("[|else|]if b then")>
        <InlineData("[|elseif b|] then")>
        <InlineData("elseif [|b|] then")>
        <InlineData("elseif b [|then|]")>
        Public Async Function NotMergedOnElseIfSpans(elseIfLine As String) As Task
            Await TestMissingInRegularAndScriptAsync(
$"class C
    sub M(a as boolean, b as boolean)
        if a then
        {elseIfLine}
        end if
    end sub
end class")
        End Function

        <Fact>
        Public Async Function NotMergedOnElseIfOverreachingSelection() As Task
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
        Public Async Function MergedIntoParentWithStatements() As Task
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
        Public Async Function NotMergedIntoParentWithUnmatchingStatements1() As Task
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
        Public Async Function NotMergedIntoParentWithUnmatchingStatements2() As Task
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
        Public Async Function NotMergedIntoParentWithUnmatchingStatements3() As Task
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
        Public Async Function NotMergedIntoParentWithUnmatchingStatements4() As Task
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
        Public Async Function MergedIntoParentWithElseStatements() As Task
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
        Public Async Function MergedIntoParentWithElseNestedIfStatements() As Task
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
        Public Async Function MergedIntoParentWithElseIfElse() As Task
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
        Public Async Function MergedIntoParentPartOfElseIf() As Task
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
        Public Async Function MergedIntoPreviousStatementIfControlFlowQuits1() As Task
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
        Public Async Function MergedIntoPreviousStatementIfControlFlowQuits2() As Task
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
        Public Async Function MergedIntoPreviousStatementIfControlFlowQuits3() As Task
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
        Public Async Function MergedIntoPreviousStatementIfControlFlowQuits4() As Task
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
        Public Async Function MergedIntoPreviousStatementIfControlFlowQuitsInCaseBlock() As Task
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
        Public Async Function MergedIntoPreviousStatementIncludingElseClauseIfControlFlowQuits() As Task
            Await TestInRegularAndScriptAsync(
"class C
    sub M(a as boolean, b as boolean)
        if a then
            return
        end if

        [||]if b then
            return
        else
            System.Console.WriteLine()
        end if
    end sub
end class",
"class C
    sub M(a as boolean, b as boolean)
        if a OrElse b then
            return
        else
            System.Console.WriteLine()
        end if
    end sub
end class")
        End Function

        <Fact>
        Public Async Function MergedIntoPreviousStatementIncludingElseIfClauseIfControlFlowQuits() As Task
            Await TestInRegularAndScriptAsync(
"class C
    sub M(a as boolean, b as boolean)
        if a then
            return
        end if

        [||]if b then
            return
        elseif a andalso b then
            System.Console.WriteLine()
        end if
    end sub
end class",
"class C
    sub M(a as boolean, b as boolean)
        if a OrElse b then
            return
        elseif a andalso b then
            System.Console.WriteLine()
        end if
    end sub
end class")
        End Function

        <Fact>
        Public Async Function NotMergedIntoPreviousStatementIfControlFlowContinues1() As Task
            ' Even though there are no statements inside, we still can't merge these into one statement
            ' because it would change the semantics from always evaluating the second condition to short-circuiting.
            Await TestMissingInRegularAndScriptAsync(
"class C
    sub M(a as boolean, b as boolean)
        if a then
        end if

        [||]if b then
        end if
    end sub
end class")
        End Function

        <Fact>
        Public Async Function NotMergedIntoPreviousStatementIfControlFlowContinues2() As Task
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
        Public Async Function NotMergedIntoPreviousStatementIfControlFlowContinues3() As Task
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
        Public Async Function NotMergedIntoPreviousStatementIfControlFlowContinues4() As Task
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
        Public Async Function NotMergedIntoPreviousStatementIfControlFlowContinues5() As Task
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
        Public Async Function NotMergedIntoPreviousStatementWithUnmatchingStatementsIfControlFlowQuits() As Task
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
        Public Async Function NotMergedIntoPreviousStatementThatHasElseClauseIfControlFlowQuits1() As Task
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
        Public Async Function NotMergedIntoPreviousStatementThatHasElseIfClauseIfControlFlowQuits1() As Task
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
        Public Async Function NotMergedIntoPreviousStatementThatHasElseClauseIfControlFlowQuits2() As Task
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
        Public Async Function NotMergedIntoPreviousStatementThatHasElseIfClauseIfControlFlowQuits2() As Task
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
        Public Async Function NotMergedIntoPreviousStatementAsPartOfElseIfIfControlFlowQuits() As Task
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

        <Fact>
        Public Async Function NotMergedSingleLineIfIntoPreviousStatementIfControlFlowQuits() As Task
            Await TestMissingAsync(
"class C
    sub M(a as boolean, b as boolean)
        if a then
            return
        end if

        [||]if b then return
    end sub
end class")
        End Function

        <Fact>
        Public Async Function NotMergedIntoPreviousStatementSingleLineIfIfControlFlowQuits() As Task
            Await TestMissingAsync(
"class C
    sub M(a as boolean, b as boolean)
        if a then return

        [||]if b then
            return
        end if
    end sub
end class")
        End Function

        <Theory>
        <InlineData("[||]if b then")>
        <InlineData("i[||]f b then")>
        <InlineData("if[||] b then")>
        <InlineData("if b [||]then")>
        <InlineData("if b th[||]en")>
        <InlineData("if b then[||]")>
        <InlineData("[|if|] b then")>
        <InlineData("[|if b then|]")>
        Public Async Function MergedIntoPreviousStatementOnIfSpans(ifLine As String) As Task
            Await TestInRegularAndScriptAsync(
$"class C
    sub M(a as boolean, b as boolean)
        if a then
            return
        end if
        {ifLine}
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
        Public Async Function MergedIntoPreviousStatementOnIfExtendedStatementSelection() As Task
            Await TestInRegularAndScriptAsync(
"class C
    sub M(a as boolean, b as boolean)
        if a then
            return
        end if
[|        if b then
|]            return
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
        Public Async Function MergedIntoPreviousStatementOnIfFullSelection() As Task
            Await TestInRegularAndScriptAsync(
"class C
    sub M(a as boolean, b as boolean)
        if a then
            return
        end if
        [|if b then
            return
        end if|]
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
        Public Async Function MergedIntoPreviousStatementOnIfExtendedFullSelection() As Task
            Await TestInRegularAndScriptAsync(
"class C
    sub M(a as boolean, b as boolean)
        if a then
            return
        end if
[|        if b then
            return
        end if
|]    end sub
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
        Public Async Function MergedIntoPreviousStatementOnIfFullSelectionWithElseClause() As Task
            Await TestInRegularAndScriptAsync(
"class C
    sub M(a as boolean, b as boolean)
        if a then
            return
        end if
        [|if b then
            return
        else if a then
        end if|]
    end sub
end class",
"class C
    sub M(a as boolean, b as boolean)
        if a OrElse b then
            return
        else if a then
        end if
    end sub
end class")
        End Function

        <Fact>
        Public Async Function NotMergedIntoPreviousStatementOnIfFullSelectionWithoutElseClause() As Task
            Await TestMissingInRegularAndScriptAsync(
"class C
    sub M(a as boolean, b as boolean)
        if a then
            return
        end if
        [|if b then
            return|]
        else if a then
        end if
    end sub
end class")
        End Function

        <Theory>
        <InlineData("if [||]b then")>
        <InlineData("[|i|]f b then")>
        <InlineData("[|if b|] then")>
        <InlineData("if [|b|] then")>
        <InlineData("if b [|then|]")>
        Public Async Function NotMergedIntoPreviousStatementOnIfSpans(ifLine As String) As Task
            Await TestMissingInRegularAndScriptAsync(
$"class C
    sub M(a as boolean, b as boolean)
        if a then
            return
        end if
        {ifLine}
            return
        end if
    end sub
end class")
        End Function

        <Fact>
        Public Async Function NotMergedIntoPreviousStatementOnIfOverreachingSelection() As Task
            Await TestMissingInRegularAndScriptAsync(
"class C
    sub M(a as boolean, b as boolean)
        if a then
            return
        end if
        [|if b then
        |]    return
        end if
    end sub
end class")
        End Function

        <Fact>
        Public Async Function NotMergedIntoPreviousStatementOnIfBodyStatementSelection() As Task
            Await TestMissingInRegularAndScriptAsync(
"class C
    sub M(a as boolean, b as boolean)
        if a then
            return
        end if
        if b then
            [|return|]
        end if
    end sub
end class")
        End Function
    End Class
End Namespace
