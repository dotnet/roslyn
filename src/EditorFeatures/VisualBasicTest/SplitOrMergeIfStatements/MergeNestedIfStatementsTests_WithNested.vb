' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.UnitTests.SplitOrMergeIfStatements
    Partial Public NotInheritable Class MergeNestedIfStatementsTests

        <Fact>
        Public Async Function MergedOnOuterIf() As Task
            Await TestInRegularAndScriptAsync(
"class C
    sub M(a as boolean, b as boolean)
        [||]if a then
            if b then
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

        <Theory>
        <InlineData("[||]elseif a then")>
        <InlineData("el[||]seif a then")>
        <InlineData("elseif[||] a then")>
        <InlineData("elseif a [||]then")>
        <InlineData("elseif a th[||]en")>
        <InlineData("elseif a then[||]")>
        <InlineData("[|elseif|] a then")>
        <InlineData("[|elseif a then|]")>
        Public Async Function MergedOnOuterElseIfSpans(elseIfLine As String) As Task
            Await TestInRegularAndScriptAsync(
$"class C
    sub M(a as boolean, b as boolean)
        if true then
        {elseIfLine}
            if b then
            end if
        end if
    end sub
end class",
"class C
    sub M(a as boolean, b as boolean)
        if true then
        elseif a AndAlso b then
        end if
    end sub
end class")
        End Function

        <Fact>
        Public Async Function MergedOnOuterElseIfExtendedStatementSelection() As Task
            Await TestInRegularAndScriptAsync(
"class C
    sub M(a as boolean, b as boolean)
        if true then
[|        elseif a then
|]            if b then
            end if
        end if
    end sub
end class",
"class C
    sub M(a as boolean, b as boolean)
        if true then
        elseif a AndAlso b then
        end if
    end sub
end class")
        End Function

        <Fact>
        Public Async Function MergedOnOuterElseIfFullSelection() As Task
            Await TestInRegularAndScriptAsync(
"class C
    sub M(a as boolean, b as boolean)
        if true then
[|        elseif a then
            if b then
            end if
        end if
|]    end sub
end class",
"class C
    sub M(a as boolean, b as boolean)
        if true then
        elseif a AndAlso b then
        end if
    end sub
end class")
        End Function

        <Fact>
        Public Async Function MergedOnOuterElseIfFullSelectionWithElseClause() As Task
            Await TestInRegularAndScriptAsync(
"class C
    sub M(a as boolean, b as boolean)
        if true then
        [|elseif a then
            if b then
                System.Console.WriteLine()
            else
            end if
        else
        end if|]
    end sub
end class",
"class C
    sub M(a as boolean, b as boolean)
        if true then
        elseif a AndAlso b then
            System.Console.WriteLine()
        else
        end if
    end sub
end class")
        End Function

        <Fact>
        Public Async Function NotMergedOnOuterElseIfFullSelectionWithoutElseClause() As Task
            Await TestMissingInRegularAndScriptAsync(
"class C
    sub M(a as boolean, b as boolean)
        if true then
        [|elseif a then
            if b then
                System.Console.WriteLine()
            else
            end if|]
        else
        end if
    end sub
end class")
        End Function

        <Fact>
        Public Async Function NotMergedOnOuterElseIfFullSelectionWithParentIf() As Task
            Await TestMissingInRegularAndScriptAsync(
"class C
    sub M(a as boolean, b as boolean)
        [|if true then
        elseif a then
            if b then
                System.Console.WriteLine()
            else
            end if
        else
        end if|]
    end sub
end class")
        End Function

        <Theory>
        <InlineData("elseif [||]a then")>
        <InlineData("[|else|]if a then")>
        <InlineData("[|elseif a|] then")>
        <InlineData("elseif [|a|] then")>
        <InlineData("elseif a [|then|]")>
        Public Async Function NotMergedOnOuterElseIfSpans(elseIfLine As String) As Task
            Await TestMissingInRegularAndScriptAsync(
$"class C
    sub M(a as boolean, b as boolean)
        if true then
        {elseIfLine}
            if b then
            end if
        end if
    end sub
end class")
        End Function

        <Fact>
        Public Async Function NotMergedOnOuterElseIfOverreachingSelection() As Task
            Await TestMissingInRegularAndScriptAsync(
"class C
    sub M(a as boolean, b as boolean)
        if true then
        [|elseif a then
        |]    if b then
            end if
        end if
    end sub
end class")
        End Function

        <Fact>
        Public Async Function NotMergedOnOuterElseIfEndStatementSelection() As Task
            Await TestMissingInRegularAndScriptAsync(
"class C
    sub M(a as boolean, b as boolean)
        if true then
        elseif a then
            if b then
            end if
        [|end if|]
    end sub
end class")
        End Function

        <Fact>
        Public Async Function NotMergedOnOuterElseIfEndStatementCaret() As Task
            Await TestMissingInRegularAndScriptAsync(
"class C
    sub M(a as boolean, b as boolean)
        if true then
        elseif a then
            if b then
            end if
        [||]end if
    end sub
end class")
        End Function

        <Fact>
        Public Async Function MergedOnMiddleIfMergableWithNestedOnly() As Task
            Const Initial As String =
"class C
    sub M(a as boolean, b as boolean, c as boolean)
        if a then
            [||]if b then
                if c then
                    System.Console.WriteLine()
                end if
            end if
            return
        end if
    end sub
end class"
            Const Expected As String =
"class C
    sub M(a as boolean, b as boolean, c as boolean)
        if a then
            if b AndAlso c then
                System.Console.WriteLine()
            end if
            return
        end if
    end sub
end class"

            Await TestActionCountAsync(Initial, 1)
            Await TestInRegularAndScriptAsync(Initial, Expected)
        End Function

        <Fact>
        Public Async Function MergedOnMiddleIfMergableWithOuterOnly() As Task
            Const Initial As String =
"class C
    sub M(a as boolean, b as boolean, c as boolean)
        if a then
            [||]if b then
                if c then
                    System.Console.WriteLine()
                end if
                return
            end if
        end if
    end sub
end class"
            Const Expected As String =
"class C
    sub M(a as boolean, b as boolean, c as boolean)
        if a AndAlso b then
            if c then
                System.Console.WriteLine()
            end if
            return
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
            [||]if b then
                if c then
                    System.Console.WriteLine()
                end if
            end if
        end if
    end sub
end class"
            Const Expected1 As String =
"class C
    sub M(a as boolean, b as boolean, c as boolean)
        if a AndAlso b then
            if c then
                System.Console.WriteLine()
            end if
        end if
    end sub
end class"
            Const Expected2 As String =
"class C
    sub M(a as boolean, b as boolean, c as boolean)
        if a then
            if b AndAlso c then
                System.Console.WriteLine()
            end if
        end if
    end sub
end class"

            Await TestActionCountAsync(Initial, 2)
            Await TestInRegularAndScriptAsync(Initial, Expected1, index:=0)
            Await TestInRegularAndScriptAsync(Initial, Expected2, index:=1)
        End Function

    End Class
End Namespace
