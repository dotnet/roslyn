' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports Microsoft.CodeAnalysis.CodeRefactorings
Imports Microsoft.CodeAnalysis.Editor.UnitTests.Workspaces
Imports Microsoft.CodeAnalysis.Editor.VisualBasic.UnitTests.CodeRefactorings
Imports Microsoft.CodeAnalysis.VisualBasic.SplitOrMergeIfStatements

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.UnitTests.SplitOrMergeIfStatements
    <Trait(Traits.Feature, Traits.Features.CodeActionsMergeNestedIfStatements)>
    Partial Public NotInheritable Class MergeNestedIfStatementsTests
        Inherits AbstractVisualBasicCodeActionTest

        Protected Overrides Function CreateCodeRefactoringProvider(workspace As EditorTestWorkspace, parameters As TestParameters) As CodeRefactoringProvider
            Return New VisualBasicMergeNestedIfStatementsCodeRefactoringProvider()
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
        Public Async Function MergedOnNestedIfSpans(ifLine As String) As Task
            Await TestInRegularAndScriptAsync(
$"class C
    sub M(a as boolean, b as boolean)
        if a then
            {ifLine}
            end if
        end if
    end sub
end class",
"class C
    sub M(a as boolean, b as boolean)
        if a AndAlso b then
        end if
    end sub
end class")
        End Function

        <Fact>
        Public Async Function MergedOnNestedIfExtendedStatementSelection() As Task
            Await TestInRegularAndScriptAsync(
"class C
    sub M(a as boolean, b as boolean)
        if a then
[|            if b then
|]            end if
        end if
    end sub
end class",
"class C
    sub M(a as boolean, b as boolean)
        if a AndAlso b then
        end if
    end sub
end class")
        End Function

        <Fact>
        Public Async Function MergedOnNestedIfFullSelection() As Task
            Await TestInRegularAndScriptAsync(
"class C
    sub M(a as boolean, b as boolean)
        if a then
[|            if b then
            end if
|]        end if
    end sub
end class",
"class C
    sub M(a as boolean, b as boolean)
        if a AndAlso b then
        end if
    end sub
end class")
        End Function

        <Fact>
        Public Async Function MergedOnNestedIfFullSelectionWithElseClause() As Task
            Await TestInRegularAndScriptAsync(
"class C
    sub M(a as boolean, b as boolean)
        if a then
            [|if b then
                System.Console.WriteLine()
            else
            end if|]
        else
        end if
    end sub
end class",
"class C
    sub M(a as boolean, b as boolean)
        if a AndAlso b then
            System.Console.WriteLine()
        else
        end if
    end sub
end class")
        End Function

        <Fact>
        Public Async Function NotMergedOnNestedIfFullSelectionWithoutElseClause() As Task
            Await TestMissingInRegularAndScriptAsync(
"class C
    sub M(a as boolean, b as boolean)
        if a then
            [|if b then
                System.Console.WriteLine()|]
            else
            end if
        else
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
        Public Async Function NotMergedOnNestedIfSpans(ifLine As String) As Task
            Await TestMissingInRegularAndScriptAsync(
$"class C
    sub M(a as boolean, b as boolean)
        if a then
            {ifLine}
            end if
        end if
    end sub
end class")
        End Function

        <Fact>
        Public Async Function NotMergedOnNestedIfOverreachingSelection() As Task
            Await TestMissingInRegularAndScriptAsync(
"class C
    sub M(a as boolean, b as boolean)
        if a then
            [|if b then
            |]end if
        end if
    end sub
end class")
        End Function

        <Fact>
        Public Async Function NotMergedOnNestedIfBodyStatementSelection() As Task
            Await TestMissingInRegularAndScriptAsync(
"class C
    sub M(a as boolean, b as boolean)
        if a then
            if b then
                [|return|]
            end if
        end if
    end sub
end class")
        End Function

        <Fact>
        Public Async Function NotMergedOnNestedIfEndStatementSelection() As Task
            Await TestMissingInRegularAndScriptAsync(
"class C
    sub M(a as boolean, b as boolean)
        if a then
            if b then
            [|end if|]
        end if
    end sub
end class")
        End Function

        <Fact>
        Public Async Function NotMergedOnNestedIfEndStatementCaret() As Task
            Await TestMissingInRegularAndScriptAsync(
"class C
    sub M(a as boolean, b as boolean)
        if a then
            if b then
            [||]end if
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
        Public Async Function NotMergedOnSingleLineIf() As Task
            Await TestMissingInRegularAndScriptAsync(
"class C
    sub M(a as boolean, b as boolean)
        if a then
            [||]if b then System.Console.WriteLine()
        end if
    end sub
end class")
        End Function

        <Fact>
        Public Async Function MergedWithAndAlsoExpressions() As Task
            Await TestInRegularAndScriptAsync(
"class C
    sub M(a as boolean, b as boolean, c as boolean, d as boolean)
        if a andalso b then
            [||]if c andalso d then
            end if
        end if
    end sub
end class",
"class C
    sub M(a as boolean, b as boolean, c as boolean, d as boolean)
        if a andalso b AndAlso c andalso d then
        end if
    end sub
end class")
        End Function

        <Fact>
        Public Async Function MergedWithOrElseExpressionParenthesized1() As Task
            Await TestInRegularAndScriptAsync(
"class C
    sub M(a as boolean, b as boolean, c as boolean, d as boolean)
        if a orelse b then
            [||]if c andalso d then
            end if
        end if
    end sub
end class",
"class C
    sub M(a as boolean, b as boolean, c as boolean, d as boolean)
        if (a orelse b) AndAlso c andalso d then
        end if
    end sub
end class")
        End Function

        <Fact>
        Public Async Function MergedWithOrElseExpressionParenthesized2() As Task
            Await TestInRegularAndScriptAsync(
"class C
    sub M(a as boolean, b as boolean, c as boolean, d as boolean)
        if a andalso b then
            [||]if c orelse d then
            end if
        end if
    end sub
end class",
"class C
    sub M(a as boolean, b as boolean, c as boolean, d as boolean)
        if a andalso b AndAlso (c orelse d) then
        end if
    end sub
end class")
        End Function

        <Fact>
        Public Async Function MergedWithEqualsExpressionNotParenthesized1() As Task
            Await TestInRegularAndScriptAsync(
"class C
    sub M(a as boolean, b as boolean, c as boolean, d as boolean)
        if a = b then
            [||]if c andalso d then
            end if
        end if
    end sub
end class",
"class C
    sub M(a as boolean, b as boolean, c as boolean, d as boolean)
        if a = b AndAlso c andalso d then
        end if
    end sub
end class")
        End Function

        <Fact>
        Public Async Function MergedWithEqualsExpressionNotParenthesized2() As Task
            Await TestInRegularAndScriptAsync(
"class C
    sub M(a as boolean, b as boolean, c as boolean, d as boolean)
        if a andalso b then
            [||]if c = d then
            end if
        end if
    end sub
end class",
"class C
    sub M(a as boolean, b as boolean, c as boolean, d as boolean)
        if a andalso b AndAlso c = d then
        end if
    end sub
end class")
        End Function

        <Fact>
        Public Async Function MergedWithMixedExpressions1() As Task
            Await TestInRegularAndScriptAsync(
"class C
    sub M(a as boolean, b as boolean, c as boolean, d as boolean)
        if a orelse b andalso c then
            [||]if c = d then
            end if
        end if
    end sub
end class",
"class C
    sub M(a as boolean, b as boolean, c as boolean, d as boolean)
        if (a orelse b andalso c) AndAlso c = d then
        end if
    end sub
end class")
        End Function

        <Fact>
        Public Async Function MergedWithMixedExpressions2() As Task
            Await TestInRegularAndScriptAsync(
"class C
    sub M(a as boolean, b as boolean, c as boolean, d as boolean)
        if a = b then
            [||]if b andalso c orelse d then
            end if
        end if
    end sub
end class",
"class C
    sub M(a as boolean, b as boolean, c as boolean, d as boolean)
        if a = b AndAlso (b andalso c orelse d) then
        end if
    end sub
end class")
        End Function

        <Fact>
        Public Async Function NotMergedWithNestedIfInsideWhileLoop() As Task
            ' Do not consider the while loop to be a simple block (as might be suggested by some language-agnostic helpers).
            Await TestMissingInRegularAndScriptAsync(
"class C
    sub M(a as boolean, b as boolean)
        if a then
            while true
                [||]if b then
                end if
            end while
        end if
    end sub
end class")
        End Function

        <Fact>
        Public Async Function NotMergedWithNestedIfInsideUsingStatement() As Task
            ' Do not consider the using statement to be a simple block (as might be suggested by some language-agnostic helpers).
            Await TestMissingInRegularAndScriptAsync(
"class C
    sub M(a as boolean, b as boolean)
        if a then
            using nothing
                [||]if b then
                end if
            end using
        end if
    end sub
end class")
        End Function

        <Fact>
        Public Async Function MergedWithStatements() As Task
            Await TestInRegularAndScriptAsync(
"class C
    sub M(a as boolean, b as boolean)
        if a then
            [||]if b then
                System.Console.WriteLine(a)
                System.Console.WriteLine(b)
            end if
        end if
    end sub
end class",
"class C
    sub M(a as boolean, b as boolean)
        if a AndAlso b then
            System.Console.WriteLine(a)
            System.Console.WriteLine(b)
        end if
    end sub
end class")
        End Function

        <Fact>
        Public Async Function NotMergedWithUnmatchingElseClauseOnNestedIf() As Task
            Await TestMissingInRegularAndScriptAsync(
"class C
    sub M(a as boolean, b as boolean)
        if a then
            [||]if b then
                System.Console.WriteLine(a andalso b)
            else
                System.Console.WriteLine()
            end if
        end if
    end sub
end class")
        End Function

        <Fact>
        Public Async Function NotMergedWithUnmatchingElseIfClauseOnNestedIf() As Task
            Await TestMissingInRegularAndScriptAsync(
"class C
    sub M(a as boolean, b as boolean)
        if a then
            [||]if b then
                System.Console.WriteLine(a andalso b)
            else if a then
                System.Console.WriteLine(a)
            end if
        end if
    end sub
end class")
        End Function

        <Fact>
        Public Async Function NotMergedWithUnmatchingElseIfElseClausesOnNestedIf() As Task
            Await TestMissingInRegularAndScriptAsync(
"class C
    sub M(a as boolean, b as boolean)
        if a then
            [||]if b then
                System.Console.WriteLine(a andalso b)
            else if a then
                System.Console.WriteLine(a)
            else
                System.Console.WriteLine()
            end if
        end if
    end sub
end class")
        End Function

        <Fact>
        Public Async Function NotMergedWithUnmatchingElseClauseOnOuterIf() As Task
            Await TestMissingInRegularAndScriptAsync(
"class C
    sub M(a as boolean, b as boolean)
        if a then
            [||]if b then
                System.Console.WriteLine(a andalso b)
            end if
        else
            System.Console.WriteLine()
        end if
    end sub
end class")
        End Function

        <Fact>
        Public Async Function NotMergedWithUnmatchingElseIfClauseOnOuterIf() As Task
            Await TestMissingInRegularAndScriptAsync(
"class C
    sub M(a as boolean, b as boolean)
        if a then
            [||]if b then
                System.Console.WriteLine(a andalso b)
            end if
        else if a then
            System.Console.WriteLine(a)
        end if
    end sub
end class")
        End Function

        <Fact>
        Public Async Function NotMergedWithUnmatchingElseIfElseClausesOnOuterIf() As Task
            Await TestMissingInRegularAndScriptAsync(
"class C
    sub M(a as boolean, b as boolean)
        if a then
            [||]if b then
                System.Console.WriteLine(a andalso b)
            end if
        else if a then
            System.Console.WriteLine(a)
        else
            System.Console.WriteLine()
        end if
    end sub
end class")
        End Function

        <Fact>
        Public Async Function NotMergedWithUnmatchingElseIfElseClauses1() As Task
            Await TestMissingInRegularAndScriptAsync(
"class C
    sub M(a as boolean, b as boolean)
        if a then
            [||]if b then
                System.Console.WriteLine(a andalso b)
            else if a then
                System.Console.WriteLine()
            end if
        else
            System.Console.WriteLine()
        end if
    end sub
end class")
        End Function

        <Fact>
        Public Async Function NotMergedWithUnmatchingElseIfElseClauses2() As Task
            Await TestMissingInRegularAndScriptAsync(
"class C
    sub M(a as boolean, b as boolean)
        if a then
            [||]if b then
                System.Console.WriteLine(a andalso b)
            else
                System.Console.WriteLine()
            end if
        else if a then
            System.Console.WriteLine()
        end if
    end sub
end class")
        End Function

        <Fact>
        Public Async Function NotMergedWithUnmatchingElseIfClauses1() As Task
            Await TestMissingInRegularAndScriptAsync(
"class C
    sub M(a as boolean, b as boolean)
        if a then
            [||]if b then
                System.Console.WriteLine(a andalso b)
            else if a then
                System.Console.WriteLine()
            end if
        else if b then
            System.Console.WriteLine()
        end if
    end sub
end class")
        End Function

        <Fact>
        Public Async Function NotMergedWithUnmatchingElseIfClauses2() As Task
            Await TestMissingInRegularAndScriptAsync(
"class C
    sub M(a as boolean, b as boolean)
        if a then
            [||]if b then
                System.Console.WriteLine(a andalso b)
            else if a then
                System.Console.WriteLine(a)
            end if
        else if a then
            System.Console.WriteLine(b)
        end if
    end sub
end class")
        End Function

        <Fact>
        Public Async Function NotMergedWithUnmatchingElseClauses1() As Task
            Await TestMissingInRegularAndScriptAsync(
"class C
    sub M(a as boolean, b as boolean)
        if a then
            [||]if b then
                System.Console.WriteLine(a andalso b)
            else
                System.Console.WriteLine(a)
            end if
        else
            System.Console.WriteLine(b)
        end if
    end sub
end class")
        End Function

        <Fact>
        Public Async Function NotMergedWithUnmatchingElseClauses2() As Task
            ' Do not consider the using statement to be a simple block (as might be suggested by some language-agnostic helpers).
            Await TestMissingInRegularAndScriptAsync(
"class C
    sub M(a as boolean, b as boolean)
        if a then
            [||]if b then
                System.Console.WriteLine(a andalso b)
            else
                System.Console.WriteLine(a)
            end if
        else
            using nothing
                System.Console.WriteLine(a)
            end using
        end if
    end sub
end class")
        End Function

        <Fact>
        Public Async Function NotMergedIntoElseIfWithUnmatchingElseIfClauses1() As Task
            Await TestMissingInRegularAndScriptAsync(
"class C
    sub M(a as boolean, b as boolean)
        if a orelse b then
            System.Console.WriteLine()
        elseif a then
            [||]if b then
                System.Console.WriteLine(a andalso b)
            elseif a then
                System.Console.WriteLine()
            end if
        elseif b then
            System.Console.WriteLine()
        end if
    end sub
end class")
        End Function

        <Fact>
        Public Async Function NotMergedIntoElseIfWithUnmatchingElseIfClauses2() As Task
            Await TestMissingInRegularAndScriptAsync(
"class C
    sub M(a as boolean, b as boolean)
        if a orelse b then
            System.Console.WriteLine()
        elseif a then
            [||]if b then
                System.Console.WriteLine(a andalso b)
            elseif a then
                System.Console.WriteLine(a)
            end if
        elseif a then
            System.Console.WriteLine(b)
        end if
    end sub
end class")
        End Function

        <Fact>
        Public Async Function NotMergedIntoElseIfWithUnmatchingElseClauses1() As Task
            Await TestMissingInRegularAndScriptAsync(
"class C
    sub M(a as boolean, b as boolean)
        if a orelse b then
            System.Console.WriteLine()
        elseif a then
            [||]if b then
                System.Console.WriteLine(a andalso b)
            else
                System.Console.WriteLine(a)
            end if
        else
            System.Console.WriteLine(b)
        end if
    end sub
end class")
        End Function

        <Fact>
        Public Async Function NotMergedIntoElseIfWithUnmatchingElseClauses2() As Task
            ' Do not consider the using statement to be a simple block (as might be suggested by some language-agnostic helpers).
            Await TestMissingInRegularAndScriptAsync(
"class C
    sub M(a as boolean, b as boolean)
        if a orelse b then
            System.Console.WriteLine()
        elseif a then
            [||]if b then
                System.Console.WriteLine(a andalso b)
            else
                System.Console.WriteLine(a)
            end if
        else
            using nothing
                System.Console.WriteLine(a)
            end using
        end if
    end sub
end class")
        End Function

        <Fact>
        Public Async Function MergedWithMatchingElseClauses() As Task
            Await TestInRegularAndScriptAsync(
"class C
    sub M(a as boolean, b as boolean)
        if a then
            [||]if b then
                System.Console.WriteLine(a andalso b)
            else
                System.Console.WriteLine(a)
            end if
        else
            System.Console.WriteLine(a)
        end if
    end sub
end class",
"class C
    sub M(a as boolean, b as boolean)
        if a AndAlso b then
            System.Console.WriteLine(a andalso b)
        else
            System.Console.WriteLine(a)
        end if
    end sub
end class")
        End Function

        <Fact>
        Public Async Function MergedWithMatchingElseIfClauses() As Task
            Await TestInRegularAndScriptAsync(
"class C
    sub M(a as boolean, b as boolean)
        if a then
            [||]if b then
                System.Console.WriteLine(a andalso b)
            elseif a then
                System.Console.WriteLine(a)
            end if
        elseif a then
            System.Console.WriteLine(a)
        end if
    end sub
end class",
"class C
    sub M(a as boolean, b as boolean)
        if a AndAlso b then
            System.Console.WriteLine(a andalso b)
        elseif a then
            System.Console.WriteLine(a)
        end if
    end sub
end class")
        End Function

        <Fact>
        Public Async Function MergedWithMatchingElseIfElseClauses() As Task
            Await TestInRegularAndScriptAsync(
"class C
    sub M(a as boolean, b as boolean)
        if a then
            [||]if b then
                System.Console.WriteLine(a andalso b)
            elseif a then
                System.Console.WriteLine(a)
            else
                System.Console.WriteLine()
            end if
        elseif a then
            System.Console.WriteLine(a)
        else
            System.Console.WriteLine()
        end if
    end sub
end class",
"class C
    sub M(a as boolean, b as boolean)
        if a AndAlso b then
            System.Console.WriteLine(a andalso b)
        elseif a then
            System.Console.WriteLine(a)
        else
            System.Console.WriteLine()
        end if
    end sub
end class")
        End Function

        <Fact>
        Public Async Function MergedIntoElseIfWithMatchingElseClauses() As Task
            Await TestInRegularAndScriptAsync(
"class C
    sub M(a as boolean, b as boolean)
        if a orelse b then
            System.Console.WriteLine()
        elseif a then
            [||]if b then
                System.Console.WriteLine(a andalso b)
            else
                System.Console.WriteLine(a)
            end if
        else
            System.Console.WriteLine(a)
        end if
    end sub
end class",
"class C
    sub M(a as boolean, b as boolean)
        if a orelse b then
            System.Console.WriteLine()
        elseif a AndAlso b then
            System.Console.WriteLine(a andalso b)
        else
            System.Console.WriteLine(a)
        end if
    end sub
end class")
        End Function

        <Fact>
        Public Async Function MergedIntoElseIfWithMatchingElseIfClauses() As Task
            Await TestInRegularAndScriptAsync(
"class C
    sub M(a as boolean, b as boolean)
        if a orelse b then
            System.Console.WriteLine()
        elseif a then
            [||]if b then
                System.Console.WriteLine(a andalso b)
            elseif a then
                System.Console.WriteLine(a)
            end if
        elseif a then
            System.Console.WriteLine(a)
        end if
    end sub
end class",
"class C
    sub M(a as boolean, b as boolean)
        if a orelse b then
            System.Console.WriteLine()
        elseif a AndAlso b then
            System.Console.WriteLine(a andalso b)
        elseif a then
            System.Console.WriteLine(a)
        end if
    end sub
end class")
        End Function

        <Fact>
        Public Async Function MergedIntoElseIfWithMatchingElseIfElseClauses() As Task
            Await TestInRegularAndScriptAsync(
"class C
    sub M(a as boolean, b as boolean)
        if a orelse b then
            System.Console.WriteLine()
        elseif a then
            [||]if b then
                System.Console.WriteLine(a andalso b)
            elseif a then
                System.Console.WriteLine(a)
            else
                System.Console.WriteLine()
            end if
        elseif a then
            System.Console.WriteLine(a)
        else
            System.Console.WriteLine()
        end if
    end sub
end class",
"class C
    sub M(a as boolean, b as boolean)
        if a orelse b then
            System.Console.WriteLine()
        elseif a AndAlso b then
            System.Console.WriteLine(a andalso b)
        elseif a then
            System.Console.WriteLine(a)
        else
            System.Console.WriteLine()
        end if
    end sub
end class")
        End Function

        <Fact>
        Public Async Function NotMergedWithExtraUnmatchingStatementBelowNestedIf() As Task
            Await TestMissingInRegularAndScriptAsync(
"class C
    sub M(a as boolean, b as boolean)
        if a then
            [||]if b then
                System.Console.WriteLine(a andalso b)
            else
                System.Console.WriteLine(a)
            end if

            System.Console.WriteLine(b)
        else
            System.Console.WriteLine(a)
        end if
    end sub
end class")
        End Function

        <Fact>
        Public Async Function MergedWithExtraUnmatchingStatementBelowOuterIf() As Task
            Await TestInRegularAndScriptAsync(
"class C
    sub M(a as boolean, b as boolean)
        if a then
            [||]if b then
                System.Console.WriteLine(a andalso b)
            else
                System.Console.WriteLine(a)
            end if
        else
            System.Console.WriteLine(a)
        end if

        System.Console.WriteLine(b)
    end sub
end class",
"class C
    sub M(a as boolean, b as boolean)
        if a AndAlso b then
            System.Console.WriteLine(a andalso b)
        else
            System.Console.WriteLine(a)
        end if

        System.Console.WriteLine(b)
    end sub
end class")
        End Function

        <Fact>
        Public Async Function NotMergedWithExtraUnmatchingStatementsIfControlFlowContinues() As Task
            Await TestMissingInRegularAndScriptAsync(
"class C
    sub M(a as boolean, b as boolean)
        if a then
            [||]if b then
                System.Console.WriteLine(a andalso b)
            else
                System.Console.WriteLine(a)
            end if

            System.Console.WriteLine(a)
            System.Console.WriteLine(b)
        else
            System.Console.WriteLine(a)
        end if

        System.Console.WriteLine(b)
        System.Console.WriteLine(a)
    end sub
end class")
        End Function

        <Fact>
        Public Async Function NotMergedWithExtraUnmatchingStatementsIfControlFlowQuits() As Task
            Await TestMissingInRegularAndScriptAsync(
"class C
    sub M(a as boolean, b as boolean)
        if a then
            [||]if b then
                System.Console.WriteLine(a andalso b)
            else
                System.Console.WriteLine(a)
            end if

            throw new System.Exception()
        else
            System.Console.WriteLine(a)
        end if

        return
    end sub
end class")
        End Function

        <Fact>
        Public Async Function NotMergedWithExtraPrecedingMatchingStatementsIfControlFlowQuits() As Task
            Await TestMissingInRegularAndScriptAsync(
"class C
    sub M(a as boolean, b as boolean)
        return

        if a then
            return

            [||]if b then
                System.Console.WriteLine(a andalso b)
            else
                System.Console.WriteLine(a)
            end if
        else
            System.Console.WriteLine(a)
        end if
    end sub
end class")
        End Function

        <Fact>
        Public Async Function NotMergedWithExtraMatchingStatementsIfControlFlowContinues1() As Task
            Await TestMissingInRegularAndScriptAsync(
"class C
    sub M(a as boolean, b as boolean)
        if a then
            [||]if b then
                System.Console.WriteLine(a andalso b)
            else
                System.Console.WriteLine(a)
            end if

            System.Console.WriteLine(a)
            System.Console.WriteLine(b)
        else
            System.Console.WriteLine(a)
        end if

        System.Console.WriteLine(a)
        System.Console.WriteLine(b)
    end sub
end class")
        End Function

        <Fact>
        Public Async Function NotMergedWithExtraMatchingStatementsIfControlFlowContinues2() As Task
            Await TestMissingInRegularAndScriptAsync(
"class C
    sub M(a as boolean, b as boolean)
        if a then
            [||]if b then
                System.Console.WriteLine(a andalso b)
            else
                System.Console.WriteLine(a)
            end if

            if a then
                return
            end if
        else
            System.Console.WriteLine(a)
        end if

        if a then
            return
        end if
    end sub
end class")
        End Function

        <Fact>
        Public Async Function NotMergedWithExtraMatchingStatementsIfControlFlowContinues3() As Task
            Await TestMissingInRegularAndScriptAsync(
"class C
    sub M(a as boolean, b as boolean)
        while a <> b
            if a then
                [||]if b then
                    System.Console.WriteLine(a andalso b)
                else
                    System.Console.WriteLine(a)
                end if

                while a <> b
                    continue while
                end while
            else
                System.Console.WriteLine(a)
            end if

            while a <> b
                continue while
            end while
        end while
    end sub
end class")
        End Function

        <Fact>
        Public Async Function NotMergedIntoElseIfWithExtraMatchingStatementsIfControlFlowContinues() As Task
            Await TestMissingInRegularAndScriptAsync(
"class C
    sub M(a as boolean, b as boolean)
        if a = b then
        elseif a orelse b then
        elseif a then
            [||]if b then
                System.Console.WriteLine(a andalso b)
            else
                System.Console.WriteLine(a)
            end if

            System.Console.WriteLine()
        else
            System.Console.WriteLine(a)
        end if

        System.Console.WriteLine()
    end sub
end class")
        End Function

        <Fact>
        Public Async Function MergedWithExtraMatchingStatementsIfControlFlowQuits1() As Task
            Await TestInRegularAndScriptAsync(
"class C
    sub M(a as boolean, b as boolean)
        if a then
            [||]if b then
                System.Console.WriteLine(a andalso b)
            else
                System.Console.WriteLine(a)
            end if

            return
        else
            System.Console.WriteLine(a)
        end if

        return
    end sub
end class",
"class C
    sub M(a as boolean, b as boolean)
        if a AndAlso b then
            System.Console.WriteLine(a andalso b)
        else
            System.Console.WriteLine(a)
        end if

        return
    end sub
end class")
        End Function

        <Fact>
        Public Async Function MergedWithExtraMatchingStatementsIfControlFlowQuits2() As Task
            Await TestInRegularAndScriptAsync(
"class C
    sub M(a as boolean, b as boolean)
        if a then
            [||]if b then
                System.Console.WriteLine(a andalso b)
            else
                System.Console.WriteLine(a)
            end if

            System.Console.WriteLine(a)
            throw new System.Exception()
        else
            System.Console.WriteLine(a)
        end if

        System.Console.WriteLine(a)
        throw new System.Exception()
        System.Console.WriteLine(b)
    end sub
end class",
"class C
    sub M(a as boolean, b as boolean)
        if a AndAlso b then
            System.Console.WriteLine(a andalso b)
        else
            System.Console.WriteLine(a)
        end if

        System.Console.WriteLine(a)
        throw new System.Exception()
        System.Console.WriteLine(b)
    end sub
end class")
        End Function

        <Fact>
        Public Async Function MergedWithExtraMatchingStatementsIfControlFlowQuits3() As Task
            Await TestInRegularAndScriptAsync(
"class C
    sub M(a as boolean, b as boolean)
        while a <> b
            if a then
                [||]if b then
                    System.Console.WriteLine(a andalso b)
                else
                    System.Console.WriteLine(a)
                end if

                continue while
            else
                System.Console.WriteLine(a)
            end if

            continue while
        end while
    end sub
end class",
"class C
    sub M(a as boolean, b as boolean)
        while a <> b
            if a AndAlso b then
                System.Console.WriteLine(a andalso b)
            else
                System.Console.WriteLine(a)
            end if

            continue while
        end while
    end sub
end class")
        End Function

        <Fact>
        Public Async Function MergedWithExtraMatchingStatementsIfControlFlowQuits4() As Task
            Await TestInRegularAndScriptAsync(
"class C
    sub M(a as boolean, b as boolean)
        while a <> b
            System.Console.WriteLine()

            if a then
                [||]if b then
                    System.Console.WriteLine(a andalso b)
                else
                    System.Console.WriteLine(a)
                end if

                if a then
                    continue while
                else
                    exit while
                end if
            else
                System.Console.WriteLine(a)
            end if

            if a then
                continue while
            else
                exit while
            end if
        end while
    end sub
end class",
"class C
    sub M(a as boolean, b as boolean)
        while a <> b
            System.Console.WriteLine()

            if a AndAlso b then
                System.Console.WriteLine(a andalso b)
            else
                System.Console.WriteLine(a)
            end if

            if a then
                continue while
            else
                exit while
            end if
        end while
    end sub
end class")
        End Function

        <Fact>
        Public Async Function MergedWithExtraMatchingStatementsIfControlFlowQuitsInCaseBlock() As Task
            Await TestInRegularAndScriptAsync(
"class C
    sub M(a as boolean, b as boolean)
        select a
            case else
                System.Console.WriteLine()

                if a then
                    [||]if b then
                        System.Console.WriteLine(a andalso b)
                    end if

                    exit select
                end if

                exit select
        end select
    end sub
end class",
"class C
    sub M(a as boolean, b as boolean)
        select a
            case else
                System.Console.WriteLine()

                if a AndAlso b then
                    System.Console.WriteLine(a andalso b)
                end if

                exit select
        end select
    end sub
end class")
        End Function

        <Fact>
        Public Async Function MergedIntoElseIfWithExtraMatchingStatementsIfControlFlowQuits() As Task
            Await TestInRegularAndScriptAsync(
"class C
    sub M(a as boolean, b as boolean)
        if a = b then
        elseif a orelse b then
        elseif a then
            [||]if b then
                System.Console.WriteLine(a andalso b)
            else
                System.Console.WriteLine(a)
            end if

            return
        else
            System.Console.WriteLine(a)
        end if

        return
    end sub
end class",
"class C
    sub M(a as boolean, b as boolean)
        if a = b then
        elseif a orelse b then
        elseif a AndAlso b then
            System.Console.WriteLine(a andalso b)
        else
            System.Console.WriteLine(a)
        end if

        return
    end sub
end class")
        End Function

        <Fact>
        Public Async Function NotMergedWithExtraMatchingStatementInOuterScopeOfUsingBlockIfControlFlowQuits() As Task
            Await TestMissingInRegularAndScriptAsync(
"class C
    sub M(a as boolean, b as boolean)
        using nothing
            if a then
                [||]if b then
                    System.Console.WriteLine(a andalso b)
                end if

                return
            end if
        end using

        return
    end sub
end class")
        End Function

        <Fact>
        Public Async Function NotMergedIntoElseIfWithExtraMatchingStatementInOuterScopeOfUsingBlockIfControlFlowQuits() As Task
            Await TestMissingInRegularAndScriptAsync(
"class C
    sub M(a as boolean, b as boolean)
        using nothing
            if a orelse b then
                System.Console.WriteLine()
            elseif a then
                [||]if b then
                    System.Console.WriteLine(a andalso b)
                end if

                return
            end if
        end using

        return
    end sub
end class")
        End Function
    End Class
End Namespace
