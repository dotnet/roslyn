' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

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
        Public Async Function MergedIntoNextStatementOnIfSpans(ifLine As String) As Task
            Await TestInRegularAndScriptAsync(
$"class C
    sub M(a as boolean, b as boolean)
        {ifLine}
            return
        end if
        if b then
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
        Public Async Function MergedIntoNextStatementOnIfExtendedStatementSelection() As Task
            Await TestInRegularAndScriptAsync(
"class C
    sub M(a as boolean, b as boolean)
[|        if a then
|]            return
        end if
        if b then
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

        <Theory>
        <InlineData("if [||]a then")>
        <InlineData("[|i|]f a then")>
        <InlineData("[|if a|] then")>
        <InlineData("if [|a|] then")>
        <InlineData("if a [|then|]")>
        Public Async Function NotMergedIntoNextStatementOnIfSpans(ifLine As String) As Task
            Await TestMissingInRegularAndScriptAsync(
$"class C
    sub M(a as boolean, b as boolean)
        {ifLine}
            return
        end if
        if b then
            return
        end if
    end sub
end class")
        End Function

        <Fact>
        Public Async Function NotMergedIntoNextStatementOnIfOverreachingSelection() As Task
            Await TestMissingInRegularAndScriptAsync(
"class C
    sub M(a as boolean, b as boolean)
        [|if a then
        |]    return
        end if
        if b then
            return
        end if
    end sub
end class")
        End Function

        <Fact>
        Public Async Function NotMergedIntoNextStatementOnIfBodyStatementSelection() As Task
            Await TestMissingInRegularAndScriptAsync(
"class C
    sub M(a as boolean, b as boolean)
        if a then
            [|return|]
        end if
        if b then
            return
        end if
    end sub
end class")
        End Function

        <Fact>
        Public Async Function MergedIntoStatementOnMiddleIfMergableWithNextOnly() As Task
            Const Initial As String =
"class C
    sub M(a as boolean, b as boolean, c as boolean)
        if a then
            return
        else
            return
        end if

        [||]if b then
            return
        end if

        if c then
            return
        end if
    end sub
end class"
            Const Expected As String =
"class C
    sub M(a as boolean, b as boolean, c as boolean)
        if a then
            return
        else
            return
        end if

        if b OrElse c then
            return
        end if
    end sub
end class"

            Await TestActionCountAsync(Initial, 1)
            Await TestInRegularAndScriptAsync(Initial, Expected)
        End Function

        <Fact>
        Public Async Function MergedIntoStatementOnMiddleIfMergableWithPreviousOnly() As Task
            Const Initial As String =
"class C
    sub M(a as boolean, b as boolean, c as boolean)
        if a then
            return
        end if

        [||]if b then
            return
        else
            return
        end if

        if c then
            return
        end if
    end sub
end class"
            Const Expected As String =
"class C
    sub M(a as boolean, b as boolean, c as boolean)
        if a OrElse b then
            return
        else
            return
        end if

        if c then
            return
        end if
    end sub
end class"

            Await TestActionCountAsync(Initial, 1)
            Await TestInRegularAndScriptAsync(Initial, Expected)
        End Function

        <Fact>
        Public Async Function MergedIntoStatementOnMiddleIfMergableWithBoth() As Task
            Const Initial As String =
"class C
    sub M(a as boolean, b as boolean, c as boolean)
        if a then
            return
        end if

        [||]if b then
            return
        end if

        if c then
            return
        end if
    end sub
end class"
            Const Expected1 As String =
"class C
    sub M(a as boolean, b as boolean, c as boolean)
        if a OrElse b then
            return
        end if

        if c then
            return
        end if
    end sub
end class"
            Const Expected2 As String =
"class C
    sub M(a as boolean, b as boolean, c as boolean)
        if a then
            return
        end if

        if b OrElse c then
            return
        end if
    end sub
end class"

            Await TestActionCountAsync(Initial, 2)
            Await TestInRegularAndScriptAsync(Initial, Expected1, index:=0)
            Await TestInRegularAndScriptAsync(Initial, Expected2, index:=1)
        End Function

    End Class
End Namespace
