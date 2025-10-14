' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Globalization
Imports Microsoft.CodeAnalysis.CodeRefactorings
Imports Microsoft.CodeAnalysis.Editor.VisualBasic.UnitTests.CodeRefactorings
Imports Microsoft.CodeAnalysis.VisualBasic.InitializeParameter

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.UnitTests.InitializeParameter
    <Trait(Traits.Feature, Traits.Features.CodeActionsInitializeParameter)>
    Public Class AddParameterCheckTests
        Inherits AbstractVisualBasicCodeActionTest

        Protected Overrides Function CreateCodeRefactoringProvider(workspace As EditorTestWorkspace, parameters As TestParameters) As CodeRefactoringProvider
            Return New VisualBasicAddParameterCheckCodeRefactoringProvider()
        End Function

        <Fact>
        Public Async Function TestSimpleReferenceType() As Task
            Await TestInRegularAndScriptAsync(
"
Imports System

class C
    public sub new([||]s as string)
    end sub
end class",
"
Imports System

class C
    public sub new(s as string)
        If s Is Nothing Then
            Throw New ArgumentNullException(NameOf(s))
        End If
    end sub
end class")
        End Function

        <Fact>
        Public Async Function TestNullable() As Task
            Await TestInRegularAndScriptAsync(
"
Imports System

class C
    public sub new([||]i as integer?)
    end sub
end class",
"
Imports System

class C
    public sub new(i as integer?)
        If i Is Nothing Then
            Throw New ArgumentNullException(NameOf(i))
        End If
    end sub
end class")
        End Function

        <Fact, WorkItem("https://github.com/dotnet/roslyn/issues/47030")>
        Public Async Function TestOnByRefParameter() As Task
            Await TestInRegularAndScriptAsync(
"
class C
    public sub new([||]byref s as string)
    end sub
end class",
"
Imports System

class C
    public sub new(byref s as string)
        If s Is Nothing Then
            Throw New ArgumentNullException(NameOf(s))
        End If
    end sub
end class")
        End Function

        <Fact, WorkItem("https://github.com/dotnet/roslyn/issues/47030")>
        Public Async Function TestOnOutByRefParameter() As Task
            Await TestInRegularAndScriptAsync(
"
Imports System.Runtime.InteropServices
class C
    public sub new([||]<Out> byref s as string)
    end sub
end class",
"
Imports System
Imports System.Runtime.InteropServices
class C
    public sub new(<Out> byref s as string)
        If s Is Nothing Then
            Throw New ArgumentNullException(NameOf(s))
        End If
    end sub
end class")
        End Function

        <Fact>
        Public Async Function TestNotOnValueType() As Task
            Await TestMissingInRegularAndScriptAsync(
"
Imports System

class C
    public sub new([||]i as DateTime)
    end sub
end class")
        End Function

        <Fact>
        Public Async Function TestNotOnInterfaceParameter() As Task
            Await TestMissingInRegularAndScriptAsync(
"
Imports System

interface I
    sub M([||]s as string)
end class")
        End Function

        <Fact>
        Public Async Function TestNotOnAbstractParameter() As Task
            Await TestMissingInRegularAndScriptAsync(
"
Imports System

class C
    mustoverride sub M([||]s as string)
end class")
        End Function

        <Fact>
        Public Async Function TestDoNotUpdateExistingFieldAssignment() As Task
            Await TestInRegularAndScriptAsync(
"
Imports System

class C
    private _s as string 

    public sub new([||]s as string)
        _s = s
    end sub
end class",
"
Imports System

class C
    private _s as string 

    public sub new(s as string)
        If s Is Nothing Then
            Throw New ArgumentNullException(NameOf(s))
        End If

        _s = s
    end sub
end class")
        End Function

        <Fact>
        Public Async Function TestDoNotUpdateExistingPropertyAssignment() As Task
            Await TestInRegularAndScriptAsync(
"
Imports System

class C
    private property S as string

    public sub new([||]s as string)
        Me.S = s
    end sub
end class",
"
Imports System

class C
    private property S as string

    public sub new(s as string)
        If s Is Nothing Then
            Throw New ArgumentNullException(NameOf(s))
        End If

        Me.S = s
    end sub
end class")
        End Function

        <Fact, WorkItem("https://github.com/dotnet/roslyn/issues/70004")>
        Public Async Function TestInsertAfterExistingNullCheck1() As Task
            Await TestInRegularAndScriptAsync(
"
Imports System

class C
    public sub new(a as string, [||]s as string)
        If a is nothing
        End If
    end sub
end class",
"
Imports System

class C
    public sub new(a as string, s as string)
        If a is nothing
        End If

        If s Is Nothing Then
            Throw New ArgumentNullException(NameOf(s))
        End If
    end sub
end class")
        End Function

        <Fact>
        Public Async Function TestInsertBeforeExistingNullCheck1() As Task
            Await TestInRegularAndScriptAsync(
"
Imports System

class C
    public sub new([||]a as string, s as string)
        If s Is Nothing Then
        End If
    end sub
end class",
"
Imports System

class C
    public sub new(a as string, s as string)
        If a Is Nothing Then
            Throw New ArgumentNullException(NameOf(a))
        End If

        If s Is Nothing Then
        End If
    end sub
end class")
        End Function

        <Fact>
        Public Async Function TestMissingWithExistingNullCheck1() As Task
            Await TestMissingInRegularAndScriptAsync(
"
Imports System

class C
    public sub new([||]s as string)
        If s Is Nothing Then
            Throw New ArgumentNullException()
        End If
    end sub
end class")
        End Function

        <Fact>
        Public Async Function TestMissingWithExistingNullCheck3() As Task
            Await TestMissingInRegularAndScriptAsync(
"
Imports System

class C
    public sub new([||]s as string)
        If String.IsNullOrEmpty(s)
        End If
    end sub
end class")
        End Function

        <Fact>
        Public Async Function TestMissingWithExistingNullCheck4() As Task
            Await TestMissingInRegularAndScriptAsync(
"
Imports System

class C
    public sub new([||]s as string)
        If String.IsNullOrWhiteSpace(s)
        End If
    end sub
end class")
        End Function

        <Fact, WorkItem("https://github.com/dotnet/roslyn/issues/20983")>
        Public Async Function TestMissingWithExistingNullCheckInLambda() As Task
            Await TestMissingInRegularAndScriptAsync(
"
Imports System

class C
    public sub new()
        dim f = sub ([||]s as string)
                    If s Is Nothing Then
                        Throw New ArgumentNullException()
                    End If
                end sub
    end sub
end class")
        End Function

        <Fact>
        Public Async Function TestInMethod() As Task
            Await TestInRegularAndScriptAsync(
"
Imports System

class C
    sub F([||]s as string)
    end sub
end class",
"
Imports System

class C
    sub F(s as string)
        If s Is Nothing Then
            Throw New ArgumentNullException(NameOf(s))
        End If
    end sub
end class")
        End Function

        <Fact, WorkItem("https://github.com/dotnet/roslyn/issues/20983")>
        Public Async Function TestOnMultiLineSubLambdaParameter() As Task
            Await TestInRegularAndScriptAsync(
"
Imports System

class C
    public sub new()
        dim f = sub ([||]s as string)
                end sub
    end sub
end class",
"
Imports System

class C
    public sub new()
        dim f = sub (s as string)
                    If s Is Nothing Then
                        Throw New ArgumentNullException(NameOf(s))
                    End If
                end sub
    end sub
end class")
        End Function

        <Fact, WorkItem("https://github.com/dotnet/roslyn/issues/20983")>
        Public Async Function TestOnMultiLineFunctionLambdaParameter() As Task
            Await TestInRegularAndScriptAsync(
"
Imports System

class C
    public sub new()
        dim f = function ([||]s as string)
                    return 0
                end function
    end sub
end class",
"
Imports System

class C
    public sub new()
        dim f = function (s as string)
                    If s Is Nothing Then
                        Throw New ArgumentNullException(NameOf(s))
                    End If

                    return 0
                end function
    end sub
end class")
        End Function

        <Fact, WorkItem("https://github.com/dotnet/roslyn/issues/20983")>
        Public Async Function TestOnSingleLineSubLambdaParameter() As Task
            Await TestInRegularAndScriptAsync(
"
Imports System

class C
    public sub new()
        dim f = sub ([||]x) console.writeline(x)
    end sub
end class",
"
Imports System

class C
    public sub new()
        dim f = sub (x)
                    If x Is Nothing Then
                        Throw New ArgumentNullException(NameOf(x))
                    End If

                    console.writeline(x)
                End Sub
    end sub
end class")
        End Function

        <Fact, WorkItem("https://github.com/dotnet/roslyn/issues/20983")>
        Public Async Function TestOnSingleLineFunctionLambdaParameter() As Task
            Await TestInRegularAndScriptAsync(
"
Imports System

class C
    public sub new()
        dim f = function ([||]x) x
    end sub
end class",
"
Imports System

class C
    public sub new()
        dim f = function (x)
                    If x Is Nothing Then
                        Throw New ArgumentNullException(NameOf(x))
                    End If

                    Return x
                End Function
    end sub
end class")
        End Function

        <Fact>
        Public Async Function TestNotOnPropertyParameter() As Task
            Await TestMissingInRegularAndScriptAsync(
"
class C
    readonly property P([||]s as string)
        get
        end get
    end property
end class")
        End Function

        <Fact>
        Public Async Function TestNotOnIndexerParameter() As Task
            Await TestMissingInRegularAndScriptAsync(
"
class C
    default readonly property I([||]s as string)
        get
        end get
    end property
end class")
        End Function

        <Fact>
        Public Async Function TestSpecialStringCheck1() As Task
            Await TestInRegularAndScriptAsync(
"
Imports System

class C
    public sub new([||]s as string)
    end sub
end class",
$"
Imports System

class C
    public sub new(s as string)
        If String.IsNullOrEmpty(s) Then
            Throw New ArgumentException($""{String.Format(FeaturesResources._0_cannot_be_null_or_empty, "{NameOf(s)}").Replace("""", """""")}"", NameOf(s))
        End If
    end sub
end class", index:=1)
        End Function

        <Fact>
        Public Async Function TestSpecialStringCheck2() As Task
            Await TestInRegularAndScriptAsync(
"
Imports System

class C
    public sub new([||]s as string)
    end sub
end class",
$"
Imports System

class C
    public sub new(s as string)
        If String.IsNullOrWhiteSpace(s) Then
            Throw New ArgumentException($""{String.Format(FeaturesResources._0_cannot_be_null_or_whitespace, "{NameOf(s)}").Replace("""", """""")}"", NameOf(s))
        End If
    end sub
end class", index:=2)
        End Function

        <Fact, WorkItem("https://github.com/dotnet/roslyn/issues/51338")>
        Public Async Function TestSpecialStringCheck3() As Task
            Dim culture = CultureInfo.CurrentUICulture

            Try
                CultureInfo.CurrentUICulture = New CultureInfo("de-DE", useUserOverride:=False)

                Await TestInRegularAndScriptAsync(
    "
Imports System

class C
    public sub new([||]s as string)
    end sub
end class",
    $"
Imports System

class C
    public sub new(s as string)
        If String.IsNullOrEmpty(s) Then
            Throw New ArgumentException($""{String.Format(FeaturesResources._0_cannot_be_null_or_empty, "{NameOf(s)}").Replace("""", """""")}"", NameOf(s))
        End If
    end sub
end class", index:=1)
            Finally
                CultureInfo.CurrentUICulture = culture
            End Try
        End Function

        <Fact, WorkItem("https://github.com/dotnet/roslyn/issues/70004")>
        Public Async Function TestMultiNullableParameters() As Task
            Await TestInRegularAndScriptAsync(
"
Imports System

class C
    public sub new([||]a as string, b as string, c as string)
    end sub
end class",
$"
Imports System

class C
    public sub new(a as string, b as string, c as string)
        If String.IsNullOrEmpty(a) Then
            Throw New ArgumentException($""{String.Format(FeaturesResources._0_cannot_be_null_or_empty, "{NameOf(a)}").Replace("""", """""")}"", NameOf(a))
        End If

        If String.IsNullOrEmpty(b) Then
            Throw New ArgumentException($""{String.Format(FeaturesResources._0_cannot_be_null_or_empty, "{NameOf(b)}").Replace("""", """""")}"", NameOf(b))
        End If

        If String.IsNullOrEmpty(c) Then
            Throw New ArgumentException($""{String.Format(FeaturesResources._0_cannot_be_null_or_empty, "{NameOf(c)}").Replace("""", """""")}"", NameOf(c))
        End If
    end sub
end class", index:=3)
        End Function

        <Fact, WorkItem("https://github.com/dotnet/roslyn/issues/70004")>
        Public Async Function TestMultiNullableWithCursorOnNonNullable() As Task
            Await TestInRegularAndScriptAsync(
"
Imports System

class C
    public sub new([||]a as boolean, b as string, c as object)
    end sub
end class",
$"
Imports System

class C
    public sub new(a as boolean, b as string, c as object)
        If String.IsNullOrEmpty(b) Then
            Throw New ArgumentException($""{String.Format(FeaturesResources._0_cannot_be_null_or_empty, "{NameOf(b)}").Replace("""", """""")}"", NameOf(b))
        End If

        If c Is Nothing Then
            Throw New ArgumentNullException(NameOf(c))
        End If
    end sub
end class", index:=0)
        End Function

        <Fact, WorkItem("https://github.com/dotnet/roslyn/issues/29190")>
        Public Async Function TestSimpleReferenceTypeWithParameterNameSelected1() As Task
            Await TestInRegularAndScriptAsync(
"
Imports System

class C
    public sub new([|s|] as string)
    end sub
end class",
"
Imports System

class C
    public sub new(s as string)
        If s Is Nothing Then
            Throw New ArgumentNullException(NameOf(s))
        End If
    end sub
end class")
        End Function

        <Fact, WorkItem("https://github.com/dotnet/roslyn/issues/29333")>
        Public Async Function TestLambdaWithIncorrectNumberOfParameters() As Task
            Await TestMissingInRegularAndScriptAsync(
"
Class C
    Sub M(a As Action(Of Integer, Integer))
        M(Sub(x[||]
    End Sub
End Class")
        End Function

        <Fact, WorkItem("https://github.com/dotnet/roslyn/issues/52383")>
        Public Async Function TestImportSystem() As Task
            Await TestInRegularAndScriptAsync(
"
Class C
    Sub M([||]s As String)

    End Sub
End Class",
"
Imports System

Class C
    Sub M(s As String)
        If s Is Nothing Then
            Throw New ArgumentNullException(NameOf(s))
        End If
    End Sub
End Class")
        End Function
    End Class
End Namespace
