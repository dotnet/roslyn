' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports Microsoft.CodeAnalysis.CodeRefactorings
Imports Microsoft.CodeAnalysis.Editor.VisualBasic.UnitTests.CodeRefactorings
Imports Microsoft.CodeAnalysis.VisualBasic.SplitOrMergeIfStatements

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.UnitTests.SplitOrMergeIfStatements
    <Trait(Traits.Feature, Traits.Features.CodeActionsMergeConsecutiveIfStatements)>
    Partial Public NotInheritable Class MergeConsecutiveIfStatementsTests
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

    End Class
End Namespace
