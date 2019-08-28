' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.UnitTests.SplitOrMergeIfStatements
    Partial Public NotInheritable Class MergeConsecutiveIfStatementsTests

        <Theory>
        <InlineData("[||]if a then")>
        <InlineData("i[||]f a then")>
        <InlineData("if[||] a then")>
        <InlineData("if a [||]then")>
        <InlineData("if a th[||]en")>
        <InlineData("if a then[||]")>
        <InlineData("[|if|] a then")>
        <InlineData("[|if a then|]")>
        Public Async Function MergedOnIfSpans(ifLine As String) As Task
            Await TestInRegularAndScriptAsync(
$"class C
    sub M(a as boolean, b as boolean)
        {ifLine}
        elseif b then
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
        Public Async Function MergedOnIfExtendedStatementSelection() As Task
            Await TestInRegularAndScriptAsync(
"class C
    sub M(a as boolean, b as boolean)
[|        if a then
|]        elseif b then
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
        Public Async Function MergedOnIfFullSelectionWithoutElseIfClause() As Task
            Await TestInRegularAndScriptAsync(
"class C
    sub M(a as boolean, b as boolean)
        [|if a then
            System.Console.WriteLine()|]
        elseif b then
            System.Console.WriteLine()
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
        Public Async Function MergedOnIfExtendedFullSelectionWithoutElseIfClause() As Task
            Await TestInRegularAndScriptAsync(
"class C
    sub M(a as boolean, b as boolean)
[|        if a then
            System.Console.WriteLine()
|]        elseif b then
            System.Console.WriteLine()
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
        Public Async Function NotMergedOnIfFullSelectionWithElseIfClause() As Task
            Await TestMissingInRegularAndScriptAsync(
"class C
    sub M(a as boolean, b as boolean)
        [|if a then
            System.Console.WriteLine()
        elseif b then
            System.Console.WriteLine()|]
        else
        end if
    end sub
end class")
        End Function

        <Fact>
        Public Async Function NotMergedOnIfExtendedFullSelectionWithElseIfClause() As Task
            Await TestMissingInRegularAndScriptAsync(
"class C
    sub M(a as boolean, b as boolean)
[|        if a then
            System.Console.WriteLine()
        elseif b then
            System.Console.WriteLine()
|]        else
        end if
    end sub
end class")
        End Function

        <Fact>
        Public Async Function NotMergedOnIfFullSelectionWithElseIfElseClauses() As Task
            Await TestMissingInRegularAndScriptAsync(
"class C
    sub M(a as boolean, b as boolean)
        [|if a then
            System.Console.WriteLine()
        elseif b then
            System.Console.WriteLine()
        else
        end if|]
    end sub
end class")
        End Function

        <Fact>
        Public Async Function NotMergedOnIfExtendedFullSelectionWithElseIfElseClauses() As Task
            Await TestMissingInRegularAndScriptAsync(
"class C
    sub M(a as boolean, b as boolean)
[|        if a then
            System.Console.WriteLine()
        elseif b then
            System.Console.WriteLine()
        else
        end if
|]    end sub
end class")
        End Function

        <Theory>
        <InlineData("if [||]a then")>
        <InlineData("[|i|]f a then")>
        <InlineData("[|if a|] then")>
        <InlineData("if [|a|] then")>
        <InlineData("if a [|then|]")>
        Public Async Function NotMergedOnIfSpans(ifLine As String) As Task
            Await TestMissingInRegularAndScriptAsync(
$"class C
    sub M(a as boolean, b as boolean)
        {ifLine}
        elseif b then
        end if
    end sub
end class")
        End Function

        <Fact>
        Public Async Function NotMergedOnIfOverreachingSelection() As Task
            Await TestMissingInRegularAndScriptAsync(
"class C
    sub M(a as boolean, b as boolean)
        [|if a then
        |]elseif b then
        end if
    end sub
end class")
        End Function

        <Fact>
        Public Async Function NotMergedOnIfBodyStatementSelection() As Task
            Await TestMissingInRegularAndScriptAsync(
"class C
    sub M(a as boolean, b as boolean)
        if a then
            [|return|]
        elseif b then
            return
        end if
    end sub
end class")
        End Function

        <Fact>
        Public Async Function MergedOnMiddleIfMergableWithNextOnly() As Task
            Const Initial As String =
"class C
    sub M(a as boolean, b as boolean, c as boolean)
        if a then
            System.Console.WriteLine(nothing)
        [||]elseif b then
            System.Console.WriteLine()
        elseif c then
            System.Console.WriteLine()
        end if
    end sub
end class"
            Const Expected As String =
"class C
    sub M(a as boolean, b as boolean, c as boolean)
        if a then
            System.Console.WriteLine(nothing)
        elseif b OrElse c then
            System.Console.WriteLine()
        end if
    end sub
end class"

            Await TestActionCountAsync(Initial, 1)
            Await TestInRegularAndScriptAsync(Initial, Expected)
        End Function

        <Fact>
        Public Async Function MergedOnMiddleIfMergableWithPreviousOnly() As Task
            Const Initial As String =
"class C
    sub M(a as boolean, b as boolean, c as boolean)
        if a then
            System.Console.WriteLine()
        [||]elseif b then
            System.Console.WriteLine()
        elseif c then
            System.Console.WriteLine(nothing)
        end if
    end sub
end class"
            Const Expected As String =
"class C
    sub M(a as boolean, b as boolean, c as boolean)
        if a OrElse b then
            System.Console.WriteLine()
        elseif c then
            System.Console.WriteLine(nothing)
        end if
    end sub
end class"

            Await TestActionCountAsync(Initial, 1)
            Await TestInRegularAndScriptAsync(Initial, Expected)
        End Function

        <Fact>
        Public Async Function MergedOnMiddleIfMergableWithBoth() As Task
            Const Initial As String =
"class C
    sub M(a as boolean, b as boolean, c as boolean)
        if a then
            System.Console.WriteLine()
        [||]elseif b then
            System.Console.WriteLine()
        elseif c then
            System.Console.WriteLine()
        end if
    end sub
end class"
            Const Expected1 As String =
"class C
    sub M(a as boolean, b as boolean, c as boolean)
        if a OrElse b then
            System.Console.WriteLine()
        elseif c then
            System.Console.WriteLine()
        end if
    end sub
end class"
            Const Expected2 As String =
"class C
    sub M(a as boolean, b as boolean, c as boolean)
        if a then
            System.Console.WriteLine()
        elseif b OrElse c then
            System.Console.WriteLine()
        end if
    end sub
end class"

            Await TestActionCountAsync(Initial, 2)
            Await TestInRegularAndScriptAsync(Initial, Expected1, index:=0)
            Await TestInRegularAndScriptAsync(Initial, Expected2, index:=1)
        End Function

    End Class
End Namespace
