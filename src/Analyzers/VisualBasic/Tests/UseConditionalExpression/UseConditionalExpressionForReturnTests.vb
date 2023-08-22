' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports Microsoft.CodeAnalysis.CodeFixes
Imports Microsoft.CodeAnalysis.Diagnostics
Imports Microsoft.CodeAnalysis.Editor.VisualBasic.UnitTests.Diagnostics
Imports Microsoft.CodeAnalysis.VisualBasic.UseConditionalExpression

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.UnitTests.UseConditionalExpression
    <Trait(Traits.Feature, Traits.Features.CodeActionsUseConditionalExpression)>
    Partial Public Class UseConditionalExpressionForReturnTests
        Inherits AbstractVisualBasicDiagnosticProviderBasedUserDiagnosticTest

        Friend Overrides Function CreateDiagnosticProviderAndFixer(workspace As Workspace) As (DiagnosticAnalyzer, CodeFixProvider)
            Return (New VisualBasicUseConditionalExpressionForReturnDiagnosticAnalyzer(),
                New VisualBasicUseConditionalExpressionForReturnCodeFixProvider())
        End Function

        <Fact>
        Public Async Function TestOnSimpleReturn() As Task
            Await TestInRegularAndScriptAsync(
"
class C
    function M() as integer
        [||]if true
            return 0
        else
            return 1
        end if
    end function
end class",
"
class C
    function M() as integer
        Return If(true, 0, 1)
    end function
end class")
        End Function

        <Fact, WorkItem("https://github.com/dotnet/roslyn/issues/43291")>
        Public Async Function TestNotWithThrow1() As Task
            Await TestMissingAsync(
"
class C
    function M() as integer
        [||]if true
            return 0
        else
            throw new System.Exception()
        end if
    end function
end class")
        End Function

        <Fact>
        Public Async Function TestNotWithThrow2() As Task
            Await TestMissingAsync(
"
class C
    function M() as integer
        [||]if true
            throw new System.Exception()
        else
            return 1
        end if
    end function
end class")
        End Function

        <Fact>
        Public Async Function TestOnSimpleReturnNoBlocks() As Task
            Await TestInRegularAndScriptAsync(
"
class C
    function M() as integer
        [||]if true
            return 0
        else
            return 1
        end if
    end function
end class",
"
class C
    function M() as integer
        Return If(true, 0, 1)
    end function
end class")
        End Function

        <Fact>
        Public Async Function TestOnSimpleReturnNoBlocks_NotInBlock() As Task
            Await TestInRegularAndScriptAsync(
"
class C
    function M() as integer
        if true
            [||]if true
                return 0
            else
                return 1
            end if
        end if
    end function
end class",
"
class C
    function M() as integer
        if true
            Return If(true, 0, 1)
        end if
    end function
end class")
        End Function

        <Fact>
        Public Async Function TestMissingReturnValue1() As Task
            Await TestMissingInRegularAndScriptAsync(
"
class C
    function M() as integer
        [||]if true
            return 0
        else
            return
        end if
    end function
end class")
        End Function

        <Fact>
        Public Async Function TestMissingReturnValue2() As Task
            Await TestMissingInRegularAndScriptAsync(
"
class C
    function M() as integer
        [||]if true
            return
        else
            return 1
        end if
    end function
end class")
        End Function

        <Fact>
        Public Async Function TestMissingReturnValue3() As Task
            Await TestMissingInRegularAndScriptAsync(
"
class C
    function M() as integer
        [||]if true
            return
        else
            return
        end if
    end function
end class")
        End Function

        <Fact>
        Public Async Function TestWithNoElseBlockButFollowingReturn() As Task
            Await TestInRegularAndScriptAsync(
"
class C
    function M() as integer
        [||]if true
            return 0
        end if

        return 1
    end function
end class",
"
class C
    function M() as integer
        Return If(true, 0, 1)
    end function
end class")
        End Function

        <Fact>
        Public Async Function TestMissingWithoutElse() As Task
            Await TestMissingInRegularAndScriptAsync(
"
class C
    function M() as integer
        [||]if true
            return 0
        end if
    end function
end class")
        End Function

        <Fact>
        Public Async Function TestConversion1() As Task
            Await TestInRegularAndScriptAsync(
"
class C
    function M() as object
        [||]if true
            return ""a""
        else
            return ""b""
        end if
    end function
end class",
"
class C
    function M() as object
        Return If(true, ""a"", ""b"")
    end function
end class")
        End Function

        <Fact>
        Public Async Function TestConversion2() As Task
            Await TestInRegularAndScriptAsync(
"
class C
    function M() as string
        [||]if true
            return ""a""
        else
            return nothing
        end if
    end function
end class",
"
class C
    function M() as string
        Return If(true, ""a"", nothing)
    end function
end class")
        End Function

        <Fact>
        Public Async Function TestConversion3() As Task
            Await TestInRegularAndScriptAsync(
"
class C
    function M() as string
        [||]if true
            return nothing
        else
            return nothing
        end if
    end function
end class",
"
class C
    function M() as string
        Return If(true, nothing, DirectCast(nothing, String))
    end function
end class")
        End Function

        <Fact>
        Public Async Function TestKeepTriviaAroundIf() As Task
            Await TestInRegularAndScriptAsync(
"
class C
    function M() as integer
        ' leading
        [||]if true
            return 0
        else
            return 1
        end if ' trailing
    end function
end class",
"
class C
    function M() as integer
        ' leading
        Return If(true, 0, 1) ' trailing
    end function
end class")
        End Function

        <Fact>
        Public Async Function TestFixAll1() As Task
            Await TestInRegularAndScriptAsync(
"
class C
    function M() as integer
        {|FixAllInDocument:if|} true
            return 0
        else
            return 1
        end if

        if true
            return 2
        end if

        return 3
    end function
end class",
"
class C
    function M() as integer
        Return If(true, 0, 1)

        Return If(true, 2, 3)
    end function
end class")
        End Function

        <Fact>
        Public Async Function TestMultiLine1() As Task
            Await TestInRegularAndScriptAsync(
"
class C
    function M() as integer
        [||]if true
            return Foo(
                1, 2, 3)
        else
            return 1
        end if
    end function
end class",
"
class C
    function M() as integer
        Return If(true,
            Foo(
                1, 2, 3),
            1)
    end function
end class")
        End Function

        <Fact>
        Public Async Function TestMultiLine2() As Task
            Await TestInRegularAndScriptAsync(
"
class C
    function M() as integer
        [||]if true
            return 0
        else
            return Foo(
                1, 2, 3)
        end if
    end function
end class",
"
class C
    function M() as integer
        Return If(true,
            0,
            Foo(
                1, 2, 3))
    end function
end class")
        End Function

        <Fact>
        Public Async Function TestMultiLine3() As Task
            Await TestInRegularAndScriptAsync(
"
class C
    function M() as integer
        [||]if true
            return Foo(
                1, 2, 3)
        else
            return Foo(
                4, 5, 6)
        end if
    end function
end class",
"
class C
    function M() as integer
        Return If(true,
            Foo(
                1, 2, 3),
            Foo(
                4, 5, 6))
    end function
end class")
        End Function

        <Fact, WorkItem("https://github.com/dotnet/roslyn/issues/27960")>
        Public Async Function TestOnYield() As Task
            Await TestInRegularAndScriptAsync(
"
class C
    iterator function M() as integer
        [||]if true
            yield 0
        else
            yield 1
        end if
    end function
end class",
"
class C
    iterator function M() as integer
        Yield If(true, 0, 1)
    end function
end class")
        End Function

        <Fact, WorkItem("https://github.com/dotnet/roslyn/issues/27960")>
        Public Async Function TestOnYield_IEnumerableReturnType() As Task
            Await TestInRegularAndScriptAsync(
"
imports system.collections.generic

class C
    iterator function M() as IEnumerable(of integer)
        [||]if true
            yield 0
        else
            yield 1
        end if
    end function
end class",
"
imports system.collections.generic

class C
    iterator function M() as IEnumerable(of integer)
        Yield If(true, 0, 1)
    end function
end class")
        End Function

        <Fact, WorkItem("https://github.com/dotnet/roslyn/issues/36117")>
        Public Async Function TestMissingWhenCrossingPreprocessorDirective1() As Task
            Await TestMissingInRegularAndScriptAsync(
"
class C
    function M() as integer
        dim check as boolean = true
#if true
        [||]if check
            return 3
#end if
        return 2
    end function
end class")
        End Function

        <Fact, WorkItem("https://github.com/dotnet/roslyn/issues/36117")>
        Public Async Function TestMissingWhenCrossingPreprocessorDirective2() As Task
            Await TestMissingInRegularAndScriptAsync(
"
class C
    function M() as integer
        dim check as boolean = true
#if true
        [||]if check
            return 3
        end if
#end if
        return 2
    end function
end class")
        End Function
    End Class
End Namespace
