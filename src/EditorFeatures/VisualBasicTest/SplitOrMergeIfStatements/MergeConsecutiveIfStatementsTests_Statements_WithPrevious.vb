' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.UnitTests.SplitOrMergeIfStatements
    Partial Public NotInheritable Class MergeConsecutiveIfStatementsTests

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
        Public Async Function MergedIntoPreviousStatementOnIfFullSelectionWithoutElseClause() As Task
            Await TestInRegularAndScriptAsync(
"class C
    sub M(a as boolean, b as boolean)
        if a then
            return
        end if
        [|if b then
            return|]
        else
        end if
    end sub
end class",
"class C
    sub M(a as boolean, b as boolean)
        if a OrElse b then
            return
        else
        end if
    end sub
end class")
        End Function

        <Fact>
        Public Async Function MergedIntoPreviousStatementOnIfExtendedFullSelectionWithoutElseClause() As Task
            Await TestInRegularAndScriptAsync(
"class C
    sub M(a as boolean, b as boolean)
        if a then
            return
        end if
[|        if b then
            return
|]        else
        end if
    end sub
end class",
"class C
    sub M(a as boolean, b as boolean)
        if a OrElse b then
            return
        else
        end if
    end sub
end class")
        End Function

        <Fact>
        Public Async Function NotMergedIntoPreviousStatementOnIfFullSelectionWithElseClause() As Task
            Await TestMissingInRegularAndScriptAsync(
"class C
    sub M(a as boolean, b as boolean)
        if a then
            return
        end if
        [|if b then
            return
        else
        end if|]
    end sub
end class")
        End Function

        <Fact>
        Public Async Function NotMergedIntoPreviousStatementOnIfExtendedFullSelectionWithElseClause() As Task
            Await TestMissingInRegularAndScriptAsync(
"class C
    sub M(a as boolean, b as boolean)
        if a then
            return
        end if
[|        if b then
            return
        else
        end if
|]    end sub
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
            Await TestInRegularAndScriptAsync(
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
end class",
"class C
    sub M(a as boolean, b as boolean)
        if a then
            return
        elseif true then
            return
        end if

        if b OrElse true then
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

    End Class
End Namespace
