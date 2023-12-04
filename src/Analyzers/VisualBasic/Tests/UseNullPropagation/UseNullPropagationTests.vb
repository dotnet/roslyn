' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports Microsoft.CodeAnalysis.CodeFixes
Imports Microsoft.CodeAnalysis.Diagnostics
Imports Microsoft.CodeAnalysis.Editor.VisualBasic.UnitTests.Diagnostics
Imports Microsoft.CodeAnalysis.VisualBasic.UseNullPropagation

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.UnitTests.UseNullPropagation
    <Trait(Traits.Feature, Traits.Features.CodeActionsUseNullPropagation)>
    Partial Public Class UseNullPropagationTests
        Inherits AbstractVisualBasicDiagnosticProviderBasedUserDiagnosticTest

        Friend Overrides Function CreateDiagnosticProviderAndFixer(workspace As Workspace) As (DiagnosticAnalyzer, CodeFixProvider)
            Return (New VisualBasicUseNullPropagationDiagnosticAnalyzer(),
                    New VisualBasicUseNullPropagationCodeFixProvider())
        End Function

        <Fact>
        Public Async Function TestLeft_Equals() As Task
            Await TestInRegularAndScriptAsync(
"
Imports System

Class C
    Sub M(o As Object)
        Dim v = [||]If (o Is Nothing, Nothing, o.ToString())
    End Sub
End Class",
"
Imports System

Class C
    Sub M(o As Object)
        Dim v = o?.ToString()
    End Sub
End Class")
        End Function

        <Fact>
        Public Async Function TestMissingInVB12() As Task
            Await TestMissingAsync(
"
Imports System

Class C
    Sub M(o As Object)
        Dim v = [||]If (o Is Nothing, Nothing, o.ToString())
    End Sub
End Class", New TestParameters(VisualBasicParseOptions.Default.WithLanguageVersion(LanguageVersion.VisualBasic12)))
        End Function

        <Fact>
        Public Async Function TestMissingInVB12_IfStatement() As Task
            Await TestMissingAsync(
"
Imports System

Class C
    Sub M(o As Object)
        [||]If (o IsNot Nothing)
            o.ToString()
        End If
    End Sub
End Class", New TestParameters(VisualBasicParseOptions.Default.WithLanguageVersion(LanguageVersion.VisualBasic12)))
        End Function

        <Fact>
        Public Async Function TestRight_Equals() As Task
            Await TestInRegularAndScriptAsync(
"
Imports System

Class C
    Sub M(o As Object)
        Dim v = [||]If (Nothing Is o, Nothing, o.ToString()
    End Sub
End Class",
"
Imports System

Class C
    Sub M(o As Object)
        Dim v = o?.ToString()
    End Sub
End Class")
        End Function

        <Fact>
        Public Async Function TestLeft_NotEquals() As Task
            Await TestInRegularAndScriptAsync(
"
Imports System

Class C
    Sub M(o As Object)
        Dim v = [||]If (o IsNot Nothing, o.ToString(), Nothing)
    End Sub
End Class",
"
Imports System

Class C
    Sub M(o As Object)
        Dim v = o?.ToString()
    End Sub
End Class")
        End Function

        <Fact>
        Public Async Function TestLeft_NotEquals_IfStatement() As Task
            Await TestInRegularAndScriptAsync(
"
Imports System

Class C
    Sub M(o As Object)
        [|If|] (o IsNot Nothing)
            o.ToString()
        End If
    End Sub
End Class",
"
Imports System

Class C
    Sub M(o As Object)
        o?.ToString()
    End Sub
End Class")
        End Function

        <Fact>
        Public Async Function TestIfStatement_NotIfTrue() As Task
            Await TestMissingInRegularAndScriptAsync(
"
Imports System

Class C
    Sub M(o As Object)
        [||]If True
            o.ToString()
        End If
    End Sub
End Class")
        End Function

        <Fact>
        Public Async Function TestIfStatement_NotWithElse() As Task
            Await TestMissingInRegularAndScriptAsync(
"
Imports System

Class C
    Sub M(o As Object)
        [||]If (o IsNot Nothing)
            o.ToString()
        Else
        End If
    End Sub
End Class")
        End Function

        <Fact>
        Public Async Function TestIfStatement_NotWithElseIf() As Task
            Await TestMissingInRegularAndScriptAsync(
"
Imports System

Class C
    Sub M(o As Object)
        [||]If (o IsNot Nothing)
            o.ToString()
        ElseIf (o IsNot Nothing)
        End If
    End Sub
End Class")
        End Function

        <Fact>
        Public Async Function TestIfStatement_NotIfTrueInsideElse() As Task
            Await TestMissingInRegularAndScriptAsync(
"
Imports System

Class C
    Sub M(o As Object)
        If True
        Else
            [||]If True
                o.ToString()
            End If
        End If
    End Sub
End Class")
        End Function

        <Fact>
        Public Async Function TestIfStatement_NotWithMultipleStatements() As Task
            Await TestMissingInRegularAndScriptAsync(
"
Imports System

Class C
    Sub M(o As Object)
        [||]If (o IsNot Nothing)
            o.ToString()
            o.ToString()
        End If
    End Sub
End Class")
        End Function

        <Fact>
        Public Async Function TestWithNullableType() As Task
            Await TestInRegularAndScriptAsync(
"
Imports System

Class C
    Dim f As Integer?
    Sub M(C c)
        Dim v = [||]If (c IsNot Nothing, c.f, Nothing)
    End Sub
End Class",
"
Imports System

Class C
    Dim f As Integer?
    Sub M(C c)
        Dim v = c?.f
    End Sub
End Class")
        End Function

        <Fact>
        Public Async Function TestWithNullableType_IfStatement() As Task
            Await TestInRegularAndScriptAsync(
"
Imports System

Class C
    Dim f As Integer?
    Sub M(C c)
        [||]If (c IsNot Nothing)
            c.f?.ToString()
        End If
    End Sub
End Class",
"
Imports System

Class C
    Dim f As Integer?
    Sub M(C c)
        c?.f?.ToString()
    End Sub
End Class")
        End Function

        <Fact>
        Public Async Function TestWithNullableTypeAndObjectCast() As Task
            Await TestInRegularAndScriptAsync(
"
Imports System

Class C
    Dim f As Integer?
    Sub M(C c)
        Dim v = [||]If (DirectCast(c, Object) IsNot Nothing, c.f, Nothing)
    End Sub
End Class",
"
Imports System

Class C
    Dim f As Integer?
    Sub M(C c)
        Dim v = c?.f
    End Sub
End Class")
        End Function

        <Fact>
        Public Async Function TestWithNullableTypeAndObjectCast_IfStatement() As Task
            Await TestInRegularAndScriptAsync(
"
Imports System

Class C
    Dim f As Integer?
    Sub M(C c)
        [||]If (DirectCast(c, Object) IsNot Nothing)
            c.f?.ToString()
        End If
    End Sub
End Class",
"
Imports System

Class C
    Dim f As Integer?
    Sub M(C c)
        c?.f?.ToString()
    End Sub
End Class")
        End Function

        <Fact>
        Public Async Function TestRight_NotEquals() As Task
            Await TestInRegularAndScriptAsync(
"
Imports System

Class C
    Sub M(o As Object)
        Dim v = [||]If (Nothing IsNot o, o.ToString(), Nothing)
    End Sub
End Class",
"
Imports System

Class C
    Sub M(o As Object)
        Dim v = o?.ToString()
    End Sub
End Class")
        End Function

        <Fact>
        Public Async Function TestRight_NotEquals_IfStatement() As Task
            Await TestInRegularAndScriptAsync(
"
Imports System

Class C
    Sub M(o As Object)
        [||]If (Nothing IsNot o)
            o.ToString()
        End If
    End Sub
End Class",
"
Imports System

Class C
    Sub M(o As Object)
        o?.ToString()
    End Sub
End Class")
        End Function

        <Fact>
        Public Async Function TestIndexer() As Task
            Await TestInRegularAndScriptAsync(
"
Imports System

Class C
    Sub M(o As Object)
        Dim v = [||]If (o Is Nothing, Nothing, o(0))
    End Sub
End Class",
"
Imports System

Class C
    Sub M(o As Object)
        Dim v = o?(0)
    End Sub
End Class")
        End Function

        <Fact>
        Public Async Function TestIndexer_IfStatement() As Task
            Await TestInRegularAndScriptAsync(
"
Imports System

Class C
    Sub M(o As Object)
        [||]If (o IsNot Nothing)
            o(0)
        End If
    End Sub
End Class",
"
Imports System

Class C
    Sub M(o As Object)
        o?(0)
    End Sub
End Class")
        End Function

        <Fact>
        Public Async Function TestConditionalAccess() As Task
            Await TestInRegularAndScriptAsync(
"
Imports System

Class C
    Sub M(o As Object)
        Dim v = [||]If (o Is Nothing, Nothing, o.B?.C)
    End Sub
End Class",
"
Imports System

Class C
    Sub M(o As Object)
        Dim v = o?.B?.C
    End Sub
End Class")
        End Function

        <Fact>
        Public Async Function TestConditionalAccess_IfStatement() As Task
            Await TestInRegularAndScriptAsync(
"
Imports System

Class C
    Sub M(o As Object)
        [||]If (o IsNot Nothing)
            o.B?.C
        End If
    End Sub
End Class",
"
Imports System

Class C
    Sub M(o As Object)
        o?.B?.C
    End Sub
End Class")
        End Function

        <Fact>
        Public Async Function TestMemberAccess() As Task
            Await TestInRegularAndScriptAsync(
"
Imports System

Class C
    Sub M(o As Object)
        Dim v = [||]If (o Is Nothing, Nothing, o.B)
    End Sub
End Class",
"
Imports System

Class C
    Sub M(o As Object)
        Dim v = o?.B
    End Sub
End Class")
        End Function

        <Fact>
        Public Async Function TestMemberAccess_IfStatement() As Task
            Await TestInRegularAndScriptAsync(
"
Imports System

Class C
    Sub M(o As Object)
        [||]If (o IsNot Nothing)
            o.B
        End If
    End Sub
End Class",
"
Imports System

Class C
    Sub M(o As Object)
        o?.B
    End Sub
End Class")
        End Function

        <Fact>
        Public Async Function TestMissingOnSimpleMatch() As Task
            Await TestMissingInRegularAndScriptAsync(
"
Imports System

Class C
    Sub M(o As Object)
        Dim v = [||]If (o Is Nothing, Nothing, o)
    End Sub
End Class")
        End Function

        <Fact>
        Public Async Function TestParenthesizedCondition() As Task
            Await TestInRegularAndScriptAsync(
"
Imports System

Class C
    Sub M(o As Object)
        Dim v = [||]If ((o Is Nothing), Nothing, o.ToString())
    End Sub
End Class",
"
Imports System

Class C
    Sub M(o As Object)
        Dim v = o?.ToString()
    End Sub
End Class")
        End Function

        <Fact>
        Public Async Function TestParenthesizedCondition_IfStatement() As Task
            Await TestInRegularAndScriptAsync(
"
Imports System

Class C
    Sub M(o As Object)
        [||]If ((o IsNot Nothing))
            o.ToString()
        End If
    End Sub
End Class",
"
Imports System

Class C
    Sub M(o As Object)
        o?.ToString()
    End Sub
End Class")
        End Function

        <Fact>
        Public Async Function TestFixAll1() As Task
            Await TestInRegularAndScriptAsync(
"
Imports System

Class C
    Sub M(o As Object)
        Dim v1 = {|FixAllInDocument:If|} (o Is Nothing, Nothing, o.ToString())
        Dim v2 = If (o IsNot Nothing, o.ToString(), Nothing)
    End Sub
End Class",
"
Imports System

Class C
    Sub M(o As Object)
        Dim v1 = o?.ToString()
        Dim v2 = o?.ToString()
    End Sub
End Class")
        End Function

        <Fact>
        Public Async Function TestFixAll2() As Task
            Await TestInRegularAndScriptAsync(
"
Imports System

Class C
    void M(object o1, object o2)
        Dim v1 = {|FixAllInDocument:If|} (o1 Is Nothing, Nothing, o1.ToString(If(o2 Is Nothing, Nothing, o2.ToString()))
    End Sub
End Class",
"
Imports System

Class C
    void M(object o1, object o2)
        Dim v1 = o1?.ToString(o2?.ToString())
    End Sub
End Class")
        End Function

        <Fact>
        Public Async Function TestNullable1() As Task
            Await TestMissingAsync(
"
Imports System

Class C
    Function M(o As String) As Integer?
        return [||]If (o Is Nothing, Nothing, o.Length)
    End Function
End Class")
        End Function

        <Fact>
        Public Async Function TestNullable2() As Task
            Await TestMissingAsync(
"
Imports System

Class C
    Sub M(o As String)
        Dim x = [||]If (o Is Nothing, Nothing, o.Length)
    End Sub
End Class")
        End Function

        <Fact, WorkItem("https://github.com/dotnet/roslyn/issues/23043")>
        Public Async Function TestWithNullableTypeAndReferenceEquals1() As Task
            Await TestInRegularAndScriptAsync(
"
Imports System

Class C
    Sub M(o As Object)
        Dim v = [||]If (ReferenceEquals(o, Nothing), Nothing, o.ToString())
    End Sub
End Class",
"
Imports System

Class C
    Sub M(o As Object)
        Dim v = o?.ToString()
    End Sub
End Class")
        End Function

        <Fact, WorkItem("https://github.com/dotnet/roslyn/issues/23043")>
        Public Async Function TestWithNullableTypeAndReferenceEquals1_IfStatement() As Task
            Await TestInRegularAndScriptAsync(
"
Imports System

Class C
    Sub M(o As Object)
        [||]If (not ReferenceEquals(o, Nothing))
            o.ToString()
        End If
    End Sub
End Class",
"
Imports System

Class C
    Sub M(o As Object)
        o?.ToString()
    End Sub
End Class")
        End Function

        <Fact, WorkItem("https://github.com/dotnet/roslyn/issues/23043")>
        Public Async Function TestWithNullableTypeAndReferenceEquals2() As Task
            Await TestInRegularAndScriptAsync(
"
Imports System

Class C
    Sub M(o As Object)
        Dim v = [||]If (ReferenceEquals(Nothing, o), Nothing, o.ToString())
    End Sub
End Class",
"
Imports System

Class C
    Sub M(o As Object)
        Dim v = o?.ToString()
    End Sub
End Class")
        End Function

        <Fact, WorkItem("https://github.com/dotnet/roslyn/issues/23043")>
        Public Async Function TestWithNullableTypeAndReferenceEqualsOtherValue1() As Task
            Await TestMissingInRegularAndScriptAsync(
"
Imports System

Class C
    Sub M(o As Object, other as Object)
        Dim v = [||]If (ReferenceEquals(o, other), Nothing, o.ToString())
    End Sub
End Class")
        End Function

        <Fact, WorkItem("https://github.com/dotnet/roslyn/issues/23043")>
        Public Async Function TestWithNullableTypeAndReferenceEqualsOtherValue2() As Task
            Await TestMissingInRegularAndScriptAsync(
"
Imports System

Class C
    Sub M(o As Object, other as Object)
        Dim v = [||]If (ReferenceEquals(other, o), Nothing, o.ToString())
    End Sub
End Class")
        End Function

        <Fact, WorkItem("https://github.com/dotnet/roslyn/issues/23043")>
        Public Async Function TestWithNullableTypeAndReferenceEqualsWithObject1() As Task
            Await TestInRegularAndScriptAsync(
"
Imports System

Class C
    Sub M(o As Object)
        Dim v = [||]If (Object.ReferenceEquals(o, Nothing), Nothing, o.ToString())
    End Sub
End Class",
"
Imports System

Class C
    Sub M(o As Object)
        Dim v = o?.ToString()
    End Sub
End Class")
        End Function

        <Fact, WorkItem("https://github.com/dotnet/roslyn/issues/23043")>
        Public Async Function TestWithNullableTypeAndReferenceEqualsWithObject2() As Task
            Await TestInRegularAndScriptAsync(
"
Imports System

Class C
    Sub M(o As Object)
        Dim v = [||]If (Object.ReferenceEquals(Nothing, o), Nothing, o.ToString())
    End Sub
End Class",
"
Imports System

Class C
    Sub M(o As Object)
        Dim v = o?.ToString()
    End Sub
End Class")
        End Function

        <Fact, WorkItem("https://github.com/dotnet/roslyn/issues/23043")>
        Public Async Function TestWithNullableTypeAndReferenceEqualsOtherValueWithObject1() As Task
            Await TestMissingInRegularAndScriptAsync(
"
Imports System

Class C
    Sub M(o As Object, other as Object)
        Dim v = [||]If (Object.ReferenceEquals(o, other), Nothing, o.ToString())
    End Sub
End Class")
        End Function

        <Fact, WorkItem("https://github.com/dotnet/roslyn/issues/23043")>
        Public Async Function TestWithNullableTypeAndReferenceEqualsOtherValueWithObject2() As Task
            Await TestMissingInRegularAndScriptAsync(
"
Imports System

Class C
    Sub M(o As Object, other as Object)
        Dim v = [||]If (Object.ReferenceEquals(other, o), Nothing, o.ToString())
    End Sub
End Class")
        End Function

        <Fact, WorkItem("https://github.com/dotnet/roslyn/issues/23043")>
        Public Async Function TestWithNullableTypeAndReferenceEqualsWithOmittedArgument1() As Task
            Await TestMissingInRegularAndScriptAsync(
"
Imports System

Class C
    Sub M(o As Object)
        Dim v = [||]If (Object.ReferenceEquals(o, ), Nothing, o.ToString())
    End Sub
End Class")
        End Function

        <Fact, WorkItem("https://github.com/dotnet/roslyn/issues/23043")>
        Public Async Function TestWithNullableTypeAndReferenceEqualsWithOmittedArgument2() As Task
            Await TestMissingInRegularAndScriptAsync(
"
Imports System

Class C
    Sub M(o As Object)
        Dim v = [||]If (Object.ReferenceEquals(, Nothing), Nothing, o.ToString())
    End Sub
End Class")
        End Function

        <Fact, WorkItem("https://github.com/dotnet/roslyn/issues/23043")>
        Public Async Function TestWithNullableTypeAndLogicalNotReferenceEquals1() As Task
            Await TestInRegularAndScriptAsync(
"
Imports System

Class C
    Sub M(o As Object)
        Dim v = [||]If (Not ReferenceEquals(o, Nothing), o.ToString(), Nothing)
    End Sub
End Class",
"
Imports System

Class C
    Sub M(o As Object)
        Dim v = o?.ToString()
    End Sub
End Class")
        End Function

        <Fact, WorkItem("https://github.com/dotnet/roslyn/issues/23043")>
        Public Async Function TestWithNullableTypeAndLogicalNotReferenceEquals2() As Task
            Await TestInRegularAndScriptAsync(
"
Imports System

Class C
    Sub M(o As Object)
        Dim v = [||]If (Not ReferenceEquals(Nothing, o), o.ToString(), Nothing)
    End Sub
End Class",
"
Imports System

Class C
    Sub M(o As Object)
        Dim v = o?.ToString()
    End Sub
End Class")
        End Function

        <Fact, WorkItem("https://github.com/dotnet/roslyn/issues/23043")>
        Public Async Function TestWithNullableTypeAndLogicalNotReferenceEqualsOtherValue1() As Task
            Await TestMissingInRegularAndScriptAsync(
"
Imports System

Class C
    Sub M(o As Object, other as Object)
        Dim v = [||]If (Not ReferenceEquals(o, other), o.ToString(), Nothing)
    End Sub
End Class")
        End Function

        <Fact, WorkItem("https://github.com/dotnet/roslyn/issues/23043")>
        Public Async Function TestWithNullableTypeAndLogicalNotReferenceEqualsOtherValue2() As Task
            Await TestMissingInRegularAndScriptAsync(
"
Imports System

Class C
    Sub M(o As Object, other as Object)
        Dim v = [||]If (Not ReferenceEquals(other, o), o.ToString(), Nothing)
    End Sub
End Class")
        End Function

        <Fact, WorkItem("https://github.com/dotnet/roslyn/issues/23043")>
        Public Async Function TestWithNullableTypeAndLogicalNotReferenceEqualsWithObject1() As Task
            Await TestInRegularAndScriptAsync(
"
Imports System

Class C
    Sub M(o As Object)
        Dim v = [||]If (Not Object.ReferenceEquals(o, Nothing), o.ToString(), Nothing)
    End Sub
End Class",
"
Imports System

Class C
    Sub M(o As Object)
        Dim v = o?.ToString()
    End Sub
End Class")
        End Function

        <Fact, WorkItem("https://github.com/dotnet/roslyn/issues/23043")>
        Public Async Function TestWithNullableTypeAndLogicalNotReferenceEqualsWithObject2() As Task
            Await TestInRegularAndScriptAsync(
"
Imports System

Class C
    Sub M(o As Object)
        Dim v = [||]If (Not Object.ReferenceEquals(Nothing, o), o.ToString(), Nothing)
    End Sub
End Class",
"
Imports System

Class C
    Sub M(o As Object)
        Dim v = o?.ToString()
    End Sub
End Class")
        End Function

        <Fact, WorkItem("https://github.com/dotnet/roslyn/issues/23043")>
        Public Async Function TestWithNullableTypeAndLogicalNotReferenceEqualsOtherValueWithObject1() As Task
            Await TestMissingInRegularAndScriptAsync(
"
Imports System

Class C
    Sub M(o As Object, other as Object)
        Dim v = [||]If (Not Object.ReferenceEquals(o, other), o.ToString(), Nothing)
    End Sub
End Class")
        End Function

        <Fact, WorkItem("https://github.com/dotnet/roslyn/issues/23043")>
        Public Async Function TestWithNullableTypeAndLogicalNotReferenceEqualsOtherValueWithObject2() As Task
            Await TestMissingInRegularAndScriptAsync(
"
Imports System

Class C
    Sub M(o As Object, other as Object)
        Dim v = [||]If (Not Object.ReferenceEquals(other, o), o.ToString(), Nothing)
    End Sub
End Class")
        End Function

        <Fact, WorkItem("https://github.com/dotnet/roslyn/issues/23043")>
        Public Async Function TestEqualsWithLogicalNot() As Task
            Await TestInRegularAndScriptAsync(
"
Imports System

Class C
    Sub M(o As Object)
        Dim v = [||]If (Not (o Is Nothing), o.ToString(), Nothing)
    End Sub
End Class",
"
Imports System

Class C
    Sub M(o As Object)
        Dim v = o?.ToString()
    End Sub
End Class")
        End Function

        <Fact, WorkItem("https://github.com/dotnet/roslyn/issues/23043")>
        Public Async Function TestEqualsWithLogicalNot_IfStatement() As Task
            Await TestInRegularAndScriptAsync(
"
Imports System

Class C
    Sub M(o As Object)
        [||]If (Not (o Is Nothing))
            o.ToString()
        End If
    End Sub
End Class",
"
Imports System

Class C
    Sub M(o As Object)
        o?.ToString()
    End Sub
End Class")
        End Function

        <Fact, WorkItem("https://github.com/dotnet/roslyn/issues/23043")>
        Public Async Function TestNotEqualsWithLogicalNot() As Task
            Await TestInRegularAndScriptAsync(
"
Imports System

Class C
    Sub M(o As Object)
        Dim v = [||]If (Not (o IsNot Nothing), Nothing, o.ToString())
    End Sub
End Class",
"
Imports System

Class C
    Sub M(o As Object)
        Dim v = o?.ToString()
    End Sub
End Class")
        End Function

        <Fact, WorkItem("https://github.com/dotnet/roslyn/issues/23043")>
        Public Async Function TestEqualsOtherValueWithLogicalNot() As Task
            Await TestMissingInRegularAndScriptAsync(
"
Imports System

Class C
    Sub M(o As Object, other as Object)
        Dim v = [||]If (Not (o Is other), o.ToString(), Nothing)
    End Sub
End Class")
        End Function

        <Fact, WorkItem("https://github.com/dotnet/roslyn/issues/23043")>
        Public Async Function TestNotEqualsOtherValueWithLogicalNot() As Task
            Await TestMissingInRegularAndScriptAsync(
"
Imports System

Class C
    Sub M(o As Object, other as Object)
        Dim v = [||]If (Not (o IsNot other), Nothing, o.ToString())
    End Sub
End Class")
        End Function

        <Fact, WorkItem("https://github.com/dotnet/roslyn/issues/33992")>
        Public Async Function TestExpressionTree1() As Task
            Await TestMissingInRegularAndScriptAsync(
"
Imports System
Imports System.Linq

Public Class Class1

    Public Sub Foo()
        Dim q = From item In Enumerable.Empty(Of (x As Integer?, y As Integer?)?)().AsQueryable()
                Select [||]If(item Is Nothing, Nothing, item.Value.x)
    End Sub
End Class")
        End Function

        <Fact, WorkItem("https://github.com/dotnet/roslyn/issues/33992")>
        Public Async Function TestExpressionTree2() As Task
            Await TestMissingInRegularAndScriptAsync(
"
Imports System
Imports System.Linq

Public Class Class1

    Public Sub Foo()
        Dim q = From item In Enumerable.Empty(Of (x As Integer?, y As Integer?)?)().AsQueryable()
                Where [||]If(item Is Nothing, Nothing, item.Value.x) > 0
                Select item
    End Sub
End Class")
        End Function

        <Fact, WorkItem("https://github.com/dotnet/roslyn/issues/33992")>
        Public Async Function TestExpressionTree3() As Task
            Await TestMissingInRegularAndScriptAsync(
"
Imports System
Imports System.Linq

Public Class Class1

    Public Sub Foo()
        Dim q = From item In Enumerable.Empty(Of (x As Integer?, y As Integer?)?)().AsQueryable()
                Let x = [||]If(item Is Nothing, Nothing, item.Value.x)
                Select x
    End Sub
End Class")
        End Function

        <Fact, WorkItem("https://github.com/dotnet/roslyn/issues/63557")>
        Public Async Function TestNotWithColorColorStaticCase() As Task
            Await TestMissingInRegularAndScriptAsync(
"
Imports System

class D
    public shared sub StaticMethod()
    end sub
    public sub InstanceMethod()
    end sub
end class

public class C
    public property D as D

    public sub Test()
        [||]if D IsNot Nothing
            D.StaticMethod()
        end if
    end sub
end class")
        End Function

        <Fact, WorkItem("https://github.com/dotnet/roslyn/issues/63557")>
        Public Async Function TestWithColorColorInstanceCase() As Task
            Await TestInRegularAndScript1Async(
"
Imports System

class D
    public shared sub StaticMethod()
    end sub
    public sub InstanceMethod()
    end sub
end class

public class C
    public property D as D

    public sub Test()
        [|if|] D IsNot Nothing
            D.InstanceMethod()
        end if
    end sub
end class",
"
Imports System

class D
    public shared sub StaticMethod()
    end sub
    public sub InstanceMethod()
    end sub
end class

public class C
    public property D as D

    public sub Test()
        D?.InstanceMethod()
    end sub
end class")
        End Function

        <Fact>
        Public Async Function TestElseIf() As Task
            ' Subject to improve
            Await TestMissingInRegularAndScriptAsync(
"
Class C
    Sub M(s as String)
        If True Then
        ElseIf s [||]IsNot Nothing
            s.ToString()
        End If
    End Sub
End Class")
        End Function
    End Class
End Namespace
