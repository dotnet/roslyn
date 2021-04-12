' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports Microsoft.CodeAnalysis.CodeRefactorings
Imports Microsoft.CodeAnalysis.Editor.VisualBasic.UnitTests.CodeRefactorings
Imports Microsoft.CodeAnalysis.VisualBasic.SplitOrMergeIfStatements

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.UnitTests.SplitOrMergeIfStatements
    <Trait(Traits.Feature, Traits.Features.CodeActionsSplitIntoNestedIfStatements)>
    Public NotInheritable Class SplitIntoNestedIfStatementsTests
        Inherits AbstractVisualBasicCodeActionTest

        Protected Overrides Function CreateCodeRefactoringProvider(workspace As Workspace, parameters As TestParameters) As CodeRefactoringProvider
            Return New VisualBasicSplitIntoNestedIfStatementsCodeRefactoringProvider()
        End Function

        <Theory>
        <InlineData("a [||]andalso b")>
        <InlineData("a an[||]dalso b")>
        <InlineData("a andalso[||] b")>
        <InlineData("a [|andalso|] b")>
        Public Async Function SplitOnAndAlsoOperatorSpans(condition As String) As Task
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
            if b then
            end if
        end if
    end sub
end class")
        End Function

        <Theory>
        <InlineData("a [|and|]also b")>
        <InlineData("a[| andalso|] b")>
        <InlineData("a[||] andalso b")>
        Public Async Function NotSplitOnAndAlsoOperatorSpans(condition As String) As Task
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
        [||]if a andalso b then
        end if
    end sub
end class")
        End Function

        <Fact>
        Public Async Function NotSplitOnOrElseOperator() As Task
            Await TestMissingInRegularAndScriptAsync(
"class C
    sub M(a as boolean, b as boolean)
        if a [||]orelse b then
        end if
    end sub
end class")
        End Function

        <Fact>
        Public Async Function NotSplitOnBitwiseAndOperator() As Task
            Await TestMissingInRegularAndScriptAsync(
"class C
    sub M(a as boolean, b as boolean)
        if a [||]and b then
        end if
    end sub
end class")
        End Function

        <Fact>
        Public Async Function NotSplitOnAndAlsoOperatorOutsideIfStatement() As Task
            Await TestMissingInRegularAndScriptAsync(
"class C
    sub M(a as boolean, b as boolean)
        dim v = a [||]andalso b
    end sub
end class")
        End Function

        <Fact>
        Public Async Function NotSplitOnAndAlsoOperatorInIfStatementBody() As Task
            Await TestMissingInRegularAndScriptAsync(
"class C
    sub M(a as boolean, b as boolean)
        if a andalso b then
            a [||]andalso b
        end if
    end sub
end class")
        End Function

        <Fact>
        Public Async Function NotSplitOnSingleLineIf() As Task
            Await TestMissingInRegularAndScriptAsync(
"class C
    sub M(a as boolean, b as boolean)
        if a [||]andalso b then System.Console.WriteLine()
    end sub
end class")
        End Function

        <Fact>
        Public Async Function SplitWithChainedAndAlsoExpression1() As Task
            Await TestInRegularAndScriptAsync(
"class C
    sub M(a as boolean, b as boolean, c as boolean, d as boolean)
        if a [||]andalso b andalso c andalso d then
        end if
    end sub
end class",
"class C
    sub M(a as boolean, b as boolean, c as boolean, d as boolean)
        if a then
            if b andalso c andalso d then
            end if
        end if
    end sub
end class")
        End Function

        <Fact>
        Public Async Function SplitWithChainedAndAlsoExpression2() As Task
            Await TestInRegularAndScriptAsync(
"class C
    sub M(a as boolean, b as boolean, c as boolean, d as boolean)
        if a andalso b [||]andalso c andalso d then
        end if
    end sub
end class",
"class C
    sub M(a as boolean, b as boolean, c as boolean, d as boolean)
        if a andalso b then
            if c andalso d then
            end if
        end if
    end sub
end class")
        End Function

        <Fact>
        Public Async Function SplitWithChainedAndAlsoExpression3() As Task
            Await TestInRegularAndScriptAsync(
"class C
    sub M(a as boolean, b as boolean, c as boolean, d as boolean)
        if a andalso b andalso c [||]andalso d then
        end if
    end sub
end class",
"class C
    sub M(a as boolean, b as boolean, c as boolean, d as boolean)
        if a andalso b andalso c then
            if d then
            end if
        end if
    end sub
end class")
        End Function

        <Fact>
        Public Async Function NotSplitInsideParentheses1() As Task
            Await TestMissingInRegularAndScriptAsync(
"class C
    sub M(a as boolean, b as boolean, c as boolean, d as boolean)
        if (a [||]andalso b) andalso c andalso d then
        end if
    end sub
end class")
        End Function

        <Fact>
        Public Async Function NotSplitInsideParentheses2() As Task
            Await TestMissingInRegularAndScriptAsync(
"class C
    sub M(a as boolean, b as boolean, c as boolean, d as boolean)
        if a andalso b andalso (c [||]andalso d) then
        end if
    end sub
end class")
        End Function

        <Fact>
        Public Async Function NotSplitInsideParentheses3() As Task
            Await TestMissingInRegularAndScriptAsync(
"class C
    sub M(a as boolean, b as boolean, c as boolean, d as boolean)
        if (a andalso b [||]andalso c andalso d) then
        end if
    end sub
end class")
        End Function

        <Fact>
        Public Async Function SplitWithOtherExpressionInsideParentheses1() As Task
            Await TestInRegularAndScriptAsync(
"class C
    sub M(a as boolean, b as boolean, c as boolean, d as boolean)
        if a [||]andalso (b andalso c) andalso d then
        end if
    end sub
end class",
"class C
    sub M(a as boolean, b as boolean, c as boolean, d as boolean)
        if a then
            if (b andalso c) andalso d then
            end if
        end if
    end sub
end class")
        End Function

        <Fact>
        Public Async Function SplitWithOtherExpressionInsideParentheses2() As Task
            Await TestInRegularAndScriptAsync(
"class C
    sub M(a as boolean, b as boolean, c as boolean, d as boolean)
        if a andalso (b andalso c) [||]andalso d then
        end if
    end sub
end class",
"class C
    sub M(a as boolean, b as boolean, c as boolean, d as boolean)
        if a andalso (b andalso c) then
            if d then
            end if
        end if
    end sub
end class")
        End Function

        <Fact>
        Public Async Function NotSplitWithMixedOrElseExpression1() As Task
            Await TestMissingInRegularAndScriptAsync(
"class C
    sub M(a as boolean, b as boolean, c as boolean)
        if a [||]andalso b orelse c then
        end if
    end sub
end class")
        End Function

        <Fact>
        Public Async Function NotSplitWithMixedOrElseExpression2() As Task
            Await TestMissingInRegularAndScriptAsync(
"class C
    sub M(a as boolean, b as boolean, c as boolean)
        if a orelse b [||]andalso c then
        end if
    end sub
end class")
        End Function

        <Fact>
        Public Async Function SplitWithMixedOrElseExpressionInsideParentheses1() As Task
            Await TestInRegularAndScriptAsync(
"class C
    sub M(a as boolean, b as boolean, c as boolean)
        if a [||]andalso (b orelse c) then
        end if
    end sub
end class",
"class C
    sub M(a as boolean, b as boolean, c as boolean)
        if a then
            if (b orelse c) then
            end if
        end if
    end sub
end class")
        End Function

        <Fact>
        Public Async Function SplitWithMixedOrElseExpressionInsideParentheses2() As Task
            Await TestInRegularAndScriptAsync(
"class C
    sub M(a as boolean, b as boolean, c as boolean)
        if (a orelse b) [||]andalso c then
        end if
    end sub
end class",
"class C
    sub M(a as boolean, b as boolean, c as boolean)
        if (a orelse b) then
            if c then
            end if
        end if
    end sub
end class")
        End Function

        <Fact>
        Public Async Function SplitWithMixedEqualsExpression1() As Task
            Await TestInRegularAndScriptAsync(
"class C
    sub M(a as boolean, b as boolean, c as boolean)
        if a [||]andalso b = c then
        end if
    end sub
end class",
"class C
    sub M(a as boolean, b as boolean, c as boolean)
        if a then
            if b = c then
            end if
        end if
    end sub
end class")
        End Function

        <Fact>
        Public Async Function SplitWithMixedEqualsExpression2() As Task
            Await TestInRegularAndScriptAsync(
"class C
    sub M(a as boolean, b as boolean, c as boolean)
        if a = b [||]andalso c then
        end if
    end sub
end class",
"class C
    sub M(a as boolean, b as boolean, c as boolean)
        if a = b then
            if c then
            end if
        end if
    end sub
end class")
        End Function

        <Fact>
        Public Async Function SplitWithStatement() As Task
            Await TestInRegularAndScriptAsync(
"class C
    sub M(a as boolean, b as boolean)
        if a [||]andalso b then
            System.Console.WriteLine(a andalso b)
        end if
    end sub
end class",
"class C
    sub M(a as boolean, b as boolean)
        if a then
            if b then
                System.Console.WriteLine(a andalso b)
            end if
        end if
    end sub
end class")
        End Function

        <Fact>
        Public Async Function SplitWithNestedIfStatement() As Task
            Await TestInRegularAndScriptAsync(
"class C
    sub M(a as boolean, b as boolean)
        if a [||]andalso b then
            if true
            end if
        end if
    end sub
end class",
"class C
    sub M(a as boolean, b as boolean)
        if a then
            if b then
                if true
                end if
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
        if a [||]andalso b then
            System.Console.WriteLine()
        else
            System.Console.WriteLine(a andalso b)
        end if
    end sub
end class",
"class C
    sub M(a as boolean, b as boolean)
        if a then
            if b then
                System.Console.WriteLine()
            else
                System.Console.WriteLine(a andalso b)
            end if
        else
            System.Console.WriteLine(a andalso b)
        end if
    end sub
end class")
        End Function

        <Fact>
        Public Async Function SplitWithElseNestedIfStatement() As Task
            Await TestInRegularAndScriptAsync(
"class C
    sub M(a as boolean, b as boolean)
        if a [||]andalso b then
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
            if b then
                System.Console.WriteLine()
            else
                if true
                end if
            end if
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
        if a [||]andalso b then
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
            if b then
                System.Console.WriteLine()
            elseif a then
                System.Console.WriteLine(a)
            else
                System.Console.WriteLine(b)
            end if
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
        elseif a [||]andalso b then
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
            if b then
                System.Console.WriteLine(a)
            else
                System.Console.WriteLine(b)
            end if
        else
            System.Console.WriteLine(b)
        end if
    end sub
end class")
        End Function

        <Fact>
        Public Async Function SplitAsPartOfElseIfElseIf() As Task
            Await TestInRegularAndScriptAsync(
"class C
    sub M(a as boolean, b as boolean)
        if true then
            System.Console.WriteLine()
        elseif a [||]andalso b then
            System.Console.WriteLine(a)
        elseif a orelse b
            System.Console.WriteLine(b)
        end if
    end sub
end class",
"class C
    sub M(a as boolean, b as boolean)
        if true then
            System.Console.WriteLine()
        elseif a then
            if b then
                System.Console.WriteLine(a)
            elseif a orelse b
                System.Console.WriteLine(b)
            end if
        elseif a orelse b
            System.Console.WriteLine(b)
        end if
    end sub
end class")
        End Function
    End Class
End Namespace
