' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports Microsoft.CodeAnalysis.CodeFixes
Imports Microsoft.CodeAnalysis.Diagnostics
Imports Microsoft.CodeAnalysis.Editor.VisualBasic.UnitTests.Diagnostics
Imports Microsoft.CodeAnalysis.SimplifyBooleanExpression
Imports Microsoft.CodeAnalysis.VisualBasic.SimplifyBooleanExpression

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.UnitTests.SimplifyBooleanExpression
    <Trait(Traits.Feature, Traits.Features.CodeActionsSimplifyConditional)>
    Partial Public Class SimplifyConditionalTests
        Inherits AbstractVisualBasicDiagnosticProviderBasedUserDiagnosticTest

        Friend Overrides Function CreateDiagnosticProviderAndFixer(workspace As Workspace) As (DiagnosticAnalyzer, CodeFixProvider)
            Return (New VisualBasicSimplifyConditionalDiagnosticAnalyzer(), New SimplifyConditionalCodeFixProvider())
        End Function

        <Fact>
        Public Async Function TestSimpleCase() As Task
            Await TestInRegularAndScript1Async(
"
imports System

class C
    function M() as boolean
        return [|if(X() AndAlso Y(), true, false)|]
    end function

    private function X() as boolean
    private function Y() as boolean
end class",
"
imports System

class C
    function M() as boolean
        return X() AndAlso Y()
    end function

    private function X() as boolean
    private function Y() as boolean
end class")
        End Function

        <Fact>
        Public Async Function TestSimpleNegatedCase() As Task
            Await TestInRegularAndScript1Async(
"
imports System

class C
    function M() as boolean
        return [|if(X() AndAlso Y(), false, true)|]
    end function

    private function X() as boolean
    private function Y() as boolean
end class",
"
imports System

class C
    function M() as boolean
        return Not X() OrElse Not Y()
    end function

    private function X() as boolean
    private function Y() as boolean
end class")
        End Function

        <Fact>
        Public Async Function TestMustBeBool1() As Task
            Await TestMissingInRegularAndScriptAsync(
"
imports System

class C
    function M() as string
        return [|if(X() AndAlso Y(), """", null)|]
    end function

    private function X() as boolean
    private function Y() as boolean
end class")
        End Function

        <Fact>
        Public Async Function TestMustBeBool2() As Task
            Await TestMissingInRegularAndScriptAsync(
"
imports System

class C
    function M() as string
        return [|if(X() AndAlso Y(), null, """")|]
    end function

    private function X() as boolean
    private function Y() as boolean
end class")
        End Function

        <Fact>
        Public Async Function TestNotWithTrueTrue() As Task
            Await TestInRegularAndScript1Async(
"
imports System

class C
    function M() as boolean
        return [|if(X() AndAlso Y(), true, true)|]
    end function

    private function X() as boolean
    private function Y() as boolean
end class",
"
imports System

class C
    function M() as boolean
        return X() AndAlso Y() OrElse true
    end function

    private function X() as boolean
    private function Y() as boolean
end class")
        End Function

        <Fact>
        Public Async Function TestNotWithFalseFalse() As Task
            Await TestInRegularAndScript1Async(
"
imports System

class C
    function M() as boolean
        return [|if(X() AndAlso Y(), false, false)|]
    end function

    private function X() as boolean
    private function Y() as boolean
end class",
"
imports System

class C
    function M() as boolean
        return X() AndAlso Y() AndAlso false
    end function

    private function X() as boolean
    private function Y() as boolean
end class")
        End Function

        <Fact>
        Public Async Function TestWhenTrueIsTrueAndWhenFalseIsUnknown() As Task
            Await TestInRegularAndScript1Async(
"
Imports System

class C
    function M() as string
        return [|if(X(), true, Y())|]
    end function

    private function X() as boolean
    private function Y() as boolean
end class",
"
Imports System

class C
    function M() as string
        return X() OrElse Y()
    end function

    private function X() as boolean
    private function Y() as boolean
end class")
        End Function

        <Fact>
        Public Async Function TestWhenTrueIsFalseAndWhenFalseIsUnknown() As Task
            Await TestInRegularAndScript1Async(
"
Imports System

class C
    function M() as string
        return [|if(X(), false, Y())|]
    end function

    private function X() as boolean
    private function Y() as boolean
end class",
"
Imports System

class C
    function M() as string
        return Not X() AndAlso Y()
    end function

    private function X() as boolean
    private function Y() as boolean
end class")
        End Function

        <Fact>
        Public Async Function TestWhenTrueIsUnknownAndWhenFalseIsTrue() As Task
            Await TestInRegularAndScript1Async(
"
Imports System

class C
    function M() as string
        return [|If(X(), Y(), true)|]
    end function

    private function X() as boolean
    private function Y() as boolean
end class",
"
Imports System

class C
    function M() as string
        return Not X() OrElse Y()
    end function

    private function X() as boolean
    private function Y() as boolean
end class")
        End Function

        <Fact>
        Public Async Function TestWhenTrueIsUnknownAndWhenFalseIsFalse() As Task
            Await TestInRegularAndScript1Async(
"
Imports System

class C
    function M() as string
        return [|If(X(), Y(), false)|]
    end function

    private function X() as boolean
    private function Y() as boolean
end class",
"
Imports System

class C
    function M() as string
        return X() AndAlso Y()
    end function

    private function X() as boolean
    private function Y() as boolean
end class")
        End Function

        <Fact, WorkItem("https://github.com/dotnet/roslyn/issues/57472")>
        Public Async Function TestValueEqualityOnReferenceType1() As Task
            Await TestInRegularAndScript1Async(
"
Imports System

class C
    sub M()
        Dim name As String = ""goober""
        Dim hasName As Boolean = [|If(name = """", True, False)|]
    end sub
end class",
"
Imports System

class C
    sub M()
        Dim name As String = ""goober""
        Dim hasName As Boolean = name = """"
    end sub
end class")
        End Function

        <Fact, WorkItem("https://github.com/dotnet/roslyn/issues/57472")>
        Public Async Function TestValueEqualityOnReferenceType2() As Task
            Await TestInRegularAndScript1Async(
"
Imports System

class C
    sub M()
        Dim name As String = ""goober""
        Dim hasName As Boolean = [|If(name = """", False, True)|]
    end sub
end class",
"
Imports System

class C
    sub M()
        Dim name As String = ""goober""
        Dim hasName As Boolean = name <> """"
    end sub
end class")
        End Function

        <Fact, WorkItem("https://github.com/dotnet/roslyn/issues/57472")>
        Public Async Function TestValueEqualityOnReferenceType3() As Task
            Await TestInRegularAndScript1Async(
"
Imports System

class C
    sub M()
        Dim name As String = ""goober""
        Dim hasName As Boolean = [|If(name Is """", True, False)|]
    end sub
end class",
"
Imports System

class C
    sub M()
        Dim name As String = ""goober""
        Dim hasName As Boolean = name Is """"
    end sub
end class")
        End Function

        <Fact, WorkItem("https://github.com/dotnet/roslyn/issues/57472")>
        Public Async Function TestValueEqualityOnReferenceType4() As Task
            Await TestInRegularAndScript1Async(
"
Imports System

class C
    sub M()
        Dim name As String = ""goober""
        Dim hasName As Boolean = [|If(name IsNot """", False, True)|]
    end sub
end class",
"
Imports System

class C
    sub M()
        Dim name As String = ""goober""
        Dim hasName As Boolean = name Is """"
    end sub
end class")
        End Function
    End Class
End Namespace
