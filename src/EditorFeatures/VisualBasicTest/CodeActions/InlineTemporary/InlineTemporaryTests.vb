Option Strict Off
' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Threading.Tasks
Imports Microsoft.CodeAnalysis.VisualBasic.CodeRefactorings.InlineTemporary

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.UnitTests.CodeRefactorings.InlineTemporary
    Public Class InlineTemporaryTests
        Inherits AbstractVisualBasicCodeActionTest

        Protected Overrides Function CreateCodeRefactoringProvider(workspace As Workspace) As Object
            Return New InlineTemporaryCodeRefactoringProvider()
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsInlineTemporary)>
        Public Async Function TestNotWithNoInitializer1() As Task
            Dim code =
<MethodBody>
Dim [||]i As Integer
Console.WriteLine(i)
</MethodBody>

            Await TestMissingAsync(code)
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsInlineTemporary)>
        Public Async Function TestNotWithNoInitializer2() As Task
            Dim code =
<MethodBody>
Dim i As Integer = 0, [||]j As Integer
Console.WriteLine(j)
</MethodBody>

            Await TestMissingAsync(code)
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsInlineTemporary)>
        Public Async Function TestNotWithNoInitializer3() As Task
            Dim code =
<MethodBody>
Dim i As Integer = 0, j As Integer = 1, [||]k As Integer
Console.WriteLine(k)
</MethodBody>

            Await TestMissingAsync(code)
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsInlineTemporary)>
        Public Async Function TestNotWithNoReference1() As Task
            Dim code =
<MethodBody>
Dim [||]i As Integer = 0
Console.WriteLine(0)
</MethodBody>

            Await TestMissingAsync(code)
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsInlineTemporary)>
        Public Async Function TestNotWithNoReference2() As Task
            Dim code =
<MethodBody>
Dim i As Integer = 0, [||]j As Integer = 1
Console.WriteLine(i)
</MethodBody>

            Await TestMissingAsync(code)
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsInlineTemporary)>
        Public Async Function TestNotWithNoReference3() As Task
            Dim code =
<MethodBody>
Dim i As Integer = 0, j As Integer = 1, [||]k As Integer = 2
Console.WriteLine(i + j)
</MethodBody>

            Await TestMissingAsync(code)
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsInlineTemporary)>
        Public Async Function TestNotOnField() As Task
            Dim code =
<ClassDeclaration>
Dim [||]i As Integer = 0

Sub M()
    Console.WriteLine(i)
End Sub
</ClassDeclaration>

            Await TestMissingAsync(code)
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsInlineTemporary)>
        Public Async Function TestSingleDeclarator() As Task
            Dim code =
<MethodBody>
Dim [||]i As Integer = 0
Console.WriteLine(i)
</MethodBody>

            Dim expected =
<MethodBody>
Console.WriteLine(0)
</MethodBody>

            Await TestAsync(code, expected, compareTokens:=False)
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsInlineTemporary)>
        Public Async Function TestSingleDeclaratorDontRemoveLeadingTrivia1() As Task
            Dim code =
<File>
Imports System
Class C1
    Sub M()
#If True Then
        Stop
#End If

        Dim [||]i As Integer = 0
        Console.WriteLine(i)
    End Sub
End Class
</File>

            Dim expected =
<File>
Imports System
Class C1
    Sub M()
#If True Then
        Stop
#End If

        Console.WriteLine(0)
    End Sub
End Class
</File>

            Await TestAsync(code, expected, compareTokens:=False)
        End Function

        <WorkItem(545259)>
        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsInlineTemporary)>
        Public Async Function TestSingleDeclaratorDontRemoveLeadingTrivia2() As Task
            Dim code =
<File>
Imports System
Class C1
    Sub M()
        Dim [||]i As Integer = 0
#If True Then
        Console.WriteLine(i)
#End If
    End Sub
End Class
</File>

            Dim expected =
<File>
Imports System
Class C1
    Sub M()
#If True Then
        Console.WriteLine(0)
#End If
    End Sub
End Class
</File>

            Await TestAsync(code, expected, compareTokens:=False)
        End Function

        <WorkItem(540330)>
        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsInlineTemporary)>
        Public Async Function TestSingleDeclaratorDontMoveNextStatement() As Task
            Dim code =
<File>
Module Program
    Sub Main()
        Dim x As Integer = 10 : Dim [||]y As Integer = 5
        Console.Write(x + y)
    End Sub
End Module
</File>

            Dim expected =
<File>
Module Program
    Sub Main()
        Dim x As Integer = 10
        Console.Write(x + 5)
    End Sub
End Module
</File>

            Await TestAsync(code, expected, compareTokens:=False)
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsInlineTemporary)>
        Public Async Function TestSingleDeclaratorInPropertyGetter() As Task
            Dim code =
<PropertyGetter>
Dim [||]i As Integer = 0
Console.WriteLine(i)
</PropertyGetter>

            Dim expected =
<PropertyGetter>
Console.WriteLine(0)
</PropertyGetter>

            Await TestAsync(code, expected, compareTokens:=False)
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsInlineTemporary)>
        Public Async Function TestTwoDeclarators1() As Task
            Dim code =
<MethodBody>
Dim [||]i As Integer = 0, j As Integer = 1
Console.WriteLine(i)
</MethodBody>

            Dim expected =
<MethodBody>
Dim j As Integer = 1
Console.WriteLine(0)
</MethodBody>

            Await TestAsync(code, expected, compareTokens:=False)
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsInlineTemporary)>
        Public Async Function TestTwoDeclarators2() As Task
            Dim code =
<MethodBody>
Dim i As Integer = 0, [||]j As Integer = 1
Console.WriteLine(j)
</MethodBody>

            Dim expected =
<MethodBody>
Dim i As Integer = 0
Console.WriteLine(1)
</MethodBody>

            Await TestAsync(code, expected, compareTokens:=False)
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsInlineTemporary)>
        Public Async Function TestThreeDeclarators1() As Task
            Dim code =
<MethodBody>
Dim [||]i As Integer = 0, j As Integer = 1, k As Integer = 2
Console.WriteLine(i)
</MethodBody>

            Dim expected =
<MethodBody>
Dim j As Integer = 1, k As Integer = 2
Console.WriteLine(0)
</MethodBody>

            Await TestAsync(code, expected, compareTokens:=False)
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsInlineTemporary)>
        Public Async Function TestThreeDeclarators2() As Task
            Dim code =
<MethodBody>
Dim i As Integer = 0, [||]j As Integer = 1, k As Integer = 2
Console.WriteLine(j)
</MethodBody>

            Dim expected =
<MethodBody>
Dim i As Integer = 0, k As Integer = 2
Console.WriteLine(1)
</MethodBody>

            Await TestAsync(code, expected, compareTokens:=False)
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsInlineTemporary)>
        Public Async Function TestThreeDeclarators3() As Task
            Dim code =
<MethodBody>
Dim i As Integer = 0, j As Integer = 1, [||]k As Integer = 2
Console.WriteLine(k)
</MethodBody>

            Dim expected =
<MethodBody>
Dim i As Integer = 0, j As Integer = 1
Console.WriteLine(2)
</MethodBody>

            Await TestAsync(code, expected, compareTokens:=False)
        End Function

        <WorkItem(545704)>
        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsInlineTemporary)>
        Public Async Function TestThreeDeclarators4() As Task
            Dim code =
<MethodBody>
Dim x, z[||] As New Integer, y As Integer
x.ToString()
z.ToString()
</MethodBody>

            Dim expected =
<MethodBody>
Dim x As New Integer, y As Integer
x.ToString()
Call New Integer.ToString()
</MethodBody>

            Await TestAsync(code, expected, compareTokens:=False)
        End Function

        <WorkItem(16601, "DevDiv_Projects/Roslyn")>
        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsInlineTemporary)>
        Public Async Function TestInlineIntoNextDeclarator() As Task
            Dim code =
<MethodBody>
Dim [||]x As Action = Sub() Console.WriteLine(), y = x
</MethodBody>

            Dim expected =
<MethodBody>
Dim y = CType(Sub() Console.WriteLine(), Action)
</MethodBody>

            Await TestAsync(code, expected, compareTokens:=False)
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsInlineTemporary)>
        Public Async Function TestTwoNames1() As Task
            Dim code =
<MethodBody>
Dim [||]i, j As New String(" "c, 10)
Console.WriteLine(i)
</MethodBody>

            Dim expected =
<MethodBody>
Dim j As New String(" "c, 10)
Console.WriteLine(New String(" "c, 10))
</MethodBody>

            Await TestAsync(code, expected, compareTokens:=False)
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsInlineTemporary)>
        Public Async Function TestTwoNames2() As Task
            Dim code =
<MethodBody>
Dim i, [||]j As New String(" "c, 10)
Console.WriteLine(j)
</MethodBody>

            Dim expected =
<MethodBody>
Dim i As New String(" "c, 10)
Console.WriteLine(New String(" "c, 10))
</MethodBody>

            Await TestAsync(code, expected, compareTokens:=False)
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsInlineTemporary)>
        Public Async Function TestThreeNames1() As Task
            Dim code =
<MethodBody>
Dim [||]i, j, k As New String(" "c, 10)
Console.WriteLine(i)
</MethodBody>

            Dim expected =
<MethodBody>
Dim j, k As New String(" "c, 10)
Console.WriteLine(New String(" "c, 10))
</MethodBody>

            Await TestAsync(code, expected, compareTokens:=False)
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsInlineTemporary)>
        Public Async Function TestThreeNames2() As Task
            Dim code =
<MethodBody>
Dim i, [||]j, k As New String(" "c, 10)
Console.WriteLine(j)
</MethodBody>

            Dim expected =
<MethodBody>
Dim i, k As New String(" "c, 10)
Console.WriteLine(New String(" "c, 10))
</MethodBody>

            Await TestAsync(code, expected, compareTokens:=False)
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsInlineTemporary)>
        Public Async Function TestThreeNames3() As Task
            Dim code =
<MethodBody>
Dim i, j, [||]k As New String(" "c, 10)
Console.WriteLine(k)
</MethodBody>

            Dim expected =
<MethodBody>
Dim i, j As New String(" "c, 10)
Console.WriteLine(New String(" "c, 10))
</MethodBody>

            Await TestAsync(code, expected, compareTokens:=False)
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsInlineTemporary)>
        Public Async Function TestInlineIntoExpression1() As Task
            Dim code =
<MethodBody>
Dim [||]i As Integer = 0
Dim j As Integer = 1
Dim k As Integer = 2 + 3
Console.WriteLine(i + j * k)
</MethodBody>

            Dim expected =
<MethodBody>
Dim j As Integer = 1
Dim k As Integer = 2 + 3
Console.WriteLine(0 + j * k)
</MethodBody>

            Await TestAsync(code, expected, compareTokens:=False)
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsInlineTemporary)>
        Public Async Function TestInlineIntoExpression2() As Task
            Dim code =
<MethodBody>
Dim i As Integer = 0
Dim [||]j As Integer = 1
Dim k As Integer = 2 + 3
Console.WriteLine(i + j * k)
</MethodBody>

            Dim expected =
<MethodBody>
Dim i As Integer = 0
Dim k As Integer = 2 + 3
Console.WriteLine(i + 1 * k)
</MethodBody>

            Await TestAsync(code, expected, compareTokens:=False)
        End Function

        <Fact(Skip:="551797"), Trait(Traits.Feature, Traits.Features.CodeActionsInlineTemporary)>
        <WorkItem(551797)>
        Public Async Function TestInlineIntoExpression3() As Task
            Dim code =
<MethodBody>
Dim x[||] As Int32 = New Int32
Console.Write(x + 10)
</MethodBody>

            Dim expected =
<MethodBody>
Console.Write(New Int32 + 10)
</MethodBody>

            Await TestAsync(code, expected, compareTokens:=False)
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsInlineTemporary)>
        Public Async Function TestInlineIntoExpressionAsParenthesized() As Task
            Dim code =
<MethodBody>
Dim i As Integer = 0
Dim j As Integer = 1
Dim [||]k As Integer = 2 + 3
Console.WriteLine(i + j * k)
</MethodBody>

            Dim expected =
<MethodBody>
Dim i As Integer = 0
Dim j As Integer = 1
Console.WriteLine(i + j * (2 + 3))
</MethodBody>

            Await TestAsync(code, expected, compareTokens:=False)
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsInlineTemporary)>
        Public Async Function TestInlineIntoMemberAccess1() As Task
            Dim code =
<MethodBody>
Dim [||]s As New String(" "c, 10)
Console.WriteLine(s.Length)
</MethodBody>

            Dim expected =
<MethodBody>
Console.WriteLine(New String(" "c, 10).Length)
</MethodBody>

            Await TestAsync(code, expected, compareTokens:=False)
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsInlineTemporary)>
        Public Async Function TestInlineIntoMemberAccess2() As Task
            Dim code =
<MethodBody>
Dim [||]s As String = "a" &amp; "b"
Console.WriteLine(s.Length)
</MethodBody>

            Dim expected =
<MethodBody>
Console.WriteLine(("a" &amp; "b").Length)
</MethodBody>

            Await TestAsync(code, expected, compareTokens:=False)
        End Function

        <WorkItem(540374)>
        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsInlineTemporary)>
        Public Async Function TestInlineIntoMemberAccess3() As Task
            Dim code =
<MethodBody>
Dim [||]i As Integer = New String(" "c, 10).Length
Console.Write(i)
</MethodBody>

            Dim expected =
<MethodBody>
Console.Write(New String(" "c, 10).Length)
</MethodBody>

            Await TestAsync(code, expected, compareTokens:=False)
        End Function

        <WorkItem(541965)>
        <WorkItem(551797)>
        <WpfFact(Skip:="551797"), Trait(Traits.Feature, Traits.Features.CodeActionsInlineTemporary)>
        Public Async Function TestInlineIntoMemberAccess4() As Task
            Dim code =
<MethodBody>
Dim x[||] As Int32 = New Int32
Call x.ToString
</MethodBody>

            Dim expected =
<MethodBody>
Call New Int32().ToString
</MethodBody>

            Await TestAsync(code, expected, compareTokens:=False)
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsInlineTemporary)>
        Public Async Function TestInlineIntoMemberAccess5() As Task
            Dim code =
<ClassDeclaration>
Function GetString() As String
    Return Nothing
End Function

Sub Test()
    Dim [||]s As String = GetString
    Call s.ToString
End Sub
</ClassDeclaration>

            Dim expected =
<ClassDeclaration>
Function GetString() As String
    Return Nothing
End Function

Sub Test()
    Call GetString.ToString
End Sub
</ClassDeclaration>

            Await TestAsync(code, expected, compareTokens:=False)
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsInlineTemporary)>
        Public Async Function TestInlineIntoMemberAccess6() As Task
            Dim code =
<ClassDeclaration>
Function GetString() As String
    Return Nothing
End Function

Sub Test()
    Dim [||]s As String = GetString()
    Call s.ToString
End Sub
</ClassDeclaration>

            Dim expected =
<ClassDeclaration>
Function GetString() As String
    Return Nothing
End Function

Sub Test()
    Call GetString().ToString
End Sub
</ClassDeclaration>

            Await TestAsync(code, expected, compareTokens:=False)
        End Function

        <WorkItem(542060)>
        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsInlineTemporary)>
        Public Async Function TestInlineIntoMemberAccess7() As Task
            Dim code =
<MethodBody>
Dim z[||] As IEnumerable(Of Char) = From x In "ABC" Select x
Console.WriteLine(z.First())
</MethodBody>

            Dim expected =
<MethodBody>
Console.WriteLine((From x In "ABC" Select x).First())
</MethodBody>

            Await TestAsync(code, expected, compareTokens:=False)
        End Function

        <WorkItem(546726)>
        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsInlineTemporary)>
        Public Async Function TestInlineIntoMemberAccess8() As Task
            Dim code =
<MethodBody>
Dim x[||] As New List(Of Integer)
x.ToString()
</MethodBody>

            Dim expected =
<MethodBody>
Call New List(Of Integer)().ToString()
</MethodBody>

            Await TestAsync(code, expected, compareTokens:=False)
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsInlineTemporary)>
        Public Async Function TestInlineWithCast1() As Task
            Dim code =
<ClassDeclaration>
Sub Foo(o As Object)
End Sub
Sub Foo(i As Integer)
End Sub

Sub Test()
    Dim [||]i As Object = 1
    Foo(i)
End Sub
</ClassDeclaration>

            Dim expected =
<ClassDeclaration>
Sub Foo(o As Object)
End Sub
Sub Foo(i As Integer)
End Sub

Sub Test()
    Foo(CObj(1))
End Sub
</ClassDeclaration>

            Await TestAsync(code, expected, compareTokens:=False)
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsInlineTemporary)>
        Public Async Function TestInlineWithCast2() As Task
            Dim code =
<ClassDeclaration>
Sub Foo(l As Long)
End Sub
Sub Foo(i As Integer)
End Sub

Sub Test()
    Dim [||]i As Long = 1
    Foo(i)
End Sub
</ClassDeclaration>

            Dim expected =
<ClassDeclaration>
Sub Foo(l As Long)
End Sub
Sub Foo(i As Integer)
End Sub

Sub Test()
    Foo(CLng(1))
End Sub
</ClassDeclaration>

            Await TestAsync(code, expected, compareTokens:=False)
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsInlineTemporary)>
        Public Async Function TestInlineWithCast3() As Task
            Dim code =
<ClassDeclaration>
Sub Foo(l As Long)
End Sub
Sub Foo(i As Integer)
End Sub

Sub Test()
    Dim [||]i As Long = CByte(1)
    Foo(i)
End Sub
</ClassDeclaration>

            Dim expected =
<ClassDeclaration>
Sub Foo(l As Long)
End Sub
Sub Foo(i As Integer)
End Sub

Sub Test()
    Foo(CLng(CByte(1)))
End Sub
</ClassDeclaration>

            Await TestAsync(code, expected, compareTokens:=False)
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsInlineTemporary)>
        Public Async Function TestInlineWithCast4() As Task
            Dim code =
<ClassDeclaration>
Sub Foo(o As Object)
End Sub
Sub Foo(s As String)
End Sub

Sub Test()
    Dim [||]s As String = Nothing
    Foo(s)
End Sub
</ClassDeclaration>

            Dim expected =
<ClassDeclaration>
Sub Foo(o As Object)
End Sub
Sub Foo(s As String)
End Sub

Sub Test()
    Foo(Nothing)
End Sub
</ClassDeclaration>

            Await TestAsync(code, expected, compareTokens:=False)
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsInlineTemporary)>
        Public Async Function TestInlineWithCast5() As Task
            Dim code =
<ClassDeclaration>
Sub Foo(o As Object)
End Sub
Sub Foo(s As String)
End Sub

Sub Test()
    Dim [||]o As Object = Nothing
    Foo(o)
End Sub
</ClassDeclaration>

            Dim expected =
<ClassDeclaration>
Sub Foo(o As Object)
End Sub
Sub Foo(s As String)
End Sub

Sub Test()
    Foo(CObj(Nothing))
End Sub
</ClassDeclaration>

            Await TestAsync(code, expected, compareTokens:=False)
        End Function

        <WorkItem(544981)>
        <WorkItem(568917)>
        <WpfFact(), Trait(Traits.Feature, Traits.Features.CodeActionsInlineTemporary)>
        Public Async Function TestInlineWithCast6() As Task
            Dim code =
<File>
Option Strict On
 
Class M
    Sub Main()
        Dim x[||] As Long = 1
        Dim y As System.IComparable(Of Long) = x
    End Sub
End Class
</File>

            Dim expected =
<File>
Option Strict On
 
Class M
    Sub Main()
        Dim y As System.IComparable(Of Long) = CLng(1)
    End Sub
End Class
</File>

            Await TestAsync(code, expected, compareTokens:=False)
        End Function

        <WorkItem(544982)>
        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsInlineTemporary)>
        Public Async Function TestInlineWithCast7() As Task
            Dim code =
<File>
Option Strict On
Imports System
Module M
    Sub Foo()
        Dim x[||] As Long() = {1, 2, 3}
        Dim y = x
        Dim z As IComparable(Of Long) = y(0)
    End Sub
End Module
</File>

            Dim expected =
<File>
Option Strict On
Imports System
Module M
    Sub Foo()
        Dim y = CType({1, 2, 3}, Long())
        Dim z As IComparable(Of Long) = y(0)
    End Sub
End Module
</File>

            Await TestAsync(code, expected, compareTokens:=False)
        End Function

        <WorkItem(545130)>
        <WorkItem(568917)>
        <WpfFact(), Trait(Traits.Feature, Traits.Features.CodeActionsInlineTemporary)>
        Public Async Function TestInlineWithCast8() As Task
            Dim code =
<File>
Option Strict On
Class M
    Sub Main()
        Dim x[||] As Long? = 1
        Dim y As System.IComparable(Of Long) = x
    End Sub
End Class
</File>

            Dim expected =
<File>
Option Strict On
Class M
    Sub Main()
        Dim y As System.IComparable(Of Long) = CType(1, Long?)
    End Sub
End Class
</File>

            Await TestAsync(code, expected, compareTokens:=False)
        End Function

        <WorkItem(545162)>
        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsInlineTemporary)>
        Public Async Function TestInlineWithCast9() As Task
            Dim code =
<File>
Module Program
    Sub Main()
        Dim x As Integer() = {1}
        Dim [||]y = If(True, x, x)
        y(0) = 1
    End Sub
End Module
</File>

            Dim expected =
<File>
Module Program
    Sub Main()
        Dim x As Integer() = {1}
        CType(If(True, x, x), Integer())(0) = 1
    End Sub
End Module
</File>

            Await TestAsync(code, expected, compareTokens:=False)
        End Function

        <WorkItem(545177)>
        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsInlineTemporary)>
        Public Async Function TestInlineWithCast10() As Task
            Dim code =
<File>
Imports System
Module Program
    Sub Main()
        Dim [||]x As Action = AddressOf Console.WriteLine
        x()
    End Sub
End Module
</File>

            Dim expected =
<File>
Imports System
Module Program
    Sub Main()
        CType(AddressOf Console.WriteLine, Action)()
    End Sub
End Module
</File>

            Await TestAsync(code, expected, compareTokens:=False)
        End Function

        <WorkItem(545600)>
        <WorkItem(568917)>
        <WpfFact(), Trait(Traits.Feature, Traits.Features.CodeActionsInlineTemporary)>
        Public Async Function TestInlineWithCast11() As Task
            Dim code =
<File>
Option Strict On

Imports System

Public Class X
    Shared Sub Main()
        Dim a[||] = Sub() Return
        Dim x As X = a
    End Sub

    Public Shared Widening Operator CType(ByVal x As Action) As X
    End Operator
End Class
</File>

            Dim expected =
<File>
Option Strict On

Imports System

Public Class X
    Shared Sub Main()
        Dim x As X = CType(Sub() Return, Action)
    End Sub

    Public Shared Widening Operator CType(ByVal x As Action) As X
    End Operator
End Class
</File>

            Await TestAsync(code, expected, compareTokens:=False)
        End Function

        <WorkItem(545601)>
        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsInlineTemporary)>
        Public Async Function TestInlineWithCast12() As Task
            Dim code =
<File>
Module M
    Function Foo(Of T)(x As T, y As T) As T
    End Function
    Sub Main()
        Dim [||]x As Long = 1
        Dim y As IComparable(Of Long) = Foo(x, x)
    End Sub
End Module
</File>

            Dim expected =
<File>
Module M
    Function Foo(Of T)(x As T, y As T) As T
    End Function
    Sub Main()
        Dim y As IComparable(Of Long) = Foo(Of Long)(1, 1)
    End Sub
End Module
</File>

            Await TestAsync(code, expected, compareTokens:=False)
        End Function

        <WorkItem(568917)>
        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsInlineTemporary)>
        Public Async Function TestInlineWithCast13() As Task
            Dim code =
<File>
Option Strict On
Module M
    Sub Main(args As String())
        Dim [||]x() As Long? = {1}
        For Each y As System.IComparable(Of Long) In x
            Console.WriteLine(y)
        Next
        Console.Read()
    End Sub
End Module
</File>

            Dim expected =
<File>
Option Strict On
Module M
    Sub Main(args As String())
        For Each y As System.IComparable(Of Long) In CType({1}, Long?())
            Console.WriteLine(y)
        Next
        Console.Read()
    End Sub
End Module
</File>

            Await TestAsync(code, expected, compareTokens:=False)
        End Function

        <WorkItem(546700)>
        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsInlineTemporary)>
        Public Async Function TestInlineIntoExpressionHole1() As Task
            Dim code =
<MethodBody>
Dim s[||] = Sub() If True Then Else
Dim x = &lt;x &lt;%= s %&gt;/&gt;
</MethodBody>

            Dim expected =
<MethodBody>
Dim x = &lt;x &lt;%= Sub() If True Then Else %&gt;/&gt;
</MethodBody>

            Await TestAsync(code, expected, compareTokens:=False)
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsInlineTemporary)>
        Public Async Function TestInlineIntoExpressionHole2() As Task
            Dim code =
<MethodBody>
Dim s[||] As Action = Sub() If True Then Else
Dim x = &lt;x &lt;%= s %&gt;/&gt;
</MethodBody>

            Dim expected =
<MethodBody>
Dim x = &lt;x &lt;%= Sub() If True Then Else %&gt;/&gt;
</MethodBody>

            Await TestAsync(code, expected, compareTokens:=False)
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsInlineTemporary)>
        Public Async Function TestInlineLambda1() As Task
            Dim code =
<MethodBody>
Dim [||]f As Func(Of Integer) = Function() 1
Dim i = f.Invoke()
</MethodBody>

            Dim expected =
<MethodBody>
Dim i = (Function() 1).Invoke()
</MethodBody>

            Await TestAsync(code, expected, compareTokens:=False)
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsInlineTemporary)>
        Public Async Function TestInlineLambda2() As Task
            Dim code =
<MethodBody>
Dim [||]f As Func(Of Integer) = Function()
                                    Return 1
                                End Function
Dim i = f.Invoke()
</MethodBody>

            Dim expected =
<MethodBody>
Dim i = Function()
            Return 1
        End Function.Invoke()
</MethodBody>

            Await TestAsync(code, expected, compareTokens:=False)
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsInlineTemporary)>
        Public Async Function TestInlineInsideLambda() As Task
            Dim code =
<MethodBody>
Dim f As Func(Of Integer) = Function()
                                Dim [||]x As Integer = 0
                                Console.WriteLine(x)
                            End Function
</MethodBody>

            Dim expected =
<MethodBody>
Dim f As Func(Of Integer) = Function()
                                Console.WriteLine(0)
                            End Function
</MethodBody>

            Await TestAsync(code, expected, compareTokens:=False)
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsInlineTemporary)>
        Public Async Function TestInlineIntoLambda() As Task
            Dim code =
<MethodBody>
Dim [||]x As Integer = 0
Dim f As Func(Of Integer) = Function()
                                Console.WriteLine(x)
                            End Function
</MethodBody>

            Dim expected =
<MethodBody>
Dim f As Func(Of Integer) = Function()
                                Console.WriteLine(0)
                            End Function
</MethodBody>

            Await TestAsync(code, expected, compareTokens:=False)
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsInlineTemporary)>
        Public Async Function TestDontInlineTrailingComment() As Task
            Dim code =
<MethodBody>
Dim [||]i As Integer = 1 + 1 ' First
Console.WriteLine(i * 2)
</MethodBody>

            Dim expected =
<MethodBody>
' First
Console.WriteLine((1 + 1) * 2)
</MethodBody>

            Await TestAsync(code, expected, compareTokens:=False)
        End Function

        <WorkItem(545544)>
        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsInlineTemporary)>
        Public Async Function TestDontRemoveLineBreakAfterComment() As Task
            Dim code =
<MethodBody>
Dim [||]x = 1 ' comment
Dim y = x
</MethodBody>

            Dim expected =
<MethodBody>
' comment
Dim y = 1
</MethodBody>

            Await TestAsync(code, expected, compareTokens:=False)
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsInlineTemporary)>
        Public Async Function TestRemoveTrailingColon() As Task
            Dim code =
<MethodBody>
Dim [||]i As Integer = 1 + 1 : Dim j As Integer = 2 ' First
Console.WriteLine(i * j)
</MethodBody>

            Dim expected =
<MethodBody>
Dim j As Integer = 2 ' First
Console.WriteLine((1 + 1) * j)
</MethodBody>

            Await TestAsync(code, expected, compareTokens:=False)
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsInlineTemporary)>
        Public Async Function TestDontInsertUnnecessaryCast1() As Task
            Dim code =
<MethodBody>
Dim [||]i As Object = 1 + 1
Dim j As Integer = i
</MethodBody>

            Dim expected =
<MethodBody>
Dim j As Integer = 1 + 1
</MethodBody>

            Await TestAsync(code, expected, compareTokens:=False)
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsInlineTemporary)>
        Public Async Function TestDontInsertUnnecessaryCast2() As Task
            Dim code =
<MethodBody>
Dim [||]i As Integer = 1 + 1
Dim j As Integer = i * 2
Console.WriteLine(j)
</MethodBody>

            Dim expected =
<MethodBody>
Dim j As Integer = (1 + 1) * 2
Console.WriteLine(j)
</MethodBody>

            Await TestAsync(code, expected, compareTokens:=False)
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsInlineTemporary)>
        Public Async Function TestDontInsertUnnecessaryCast3() As Task
            Dim code =
<MethodBody>
Dim [||]x As Action = Sub()
                      End Sub
Dim y As Action = x
</MethodBody>

            Dim expected =
<MethodBody>
Dim y As Action = Sub()
                  End Sub
</MethodBody>

            Await TestAsync(code, expected, compareTokens:=False)
        End Function

        <WorkItem(543215)>
        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsInlineTemporary)>
        Public Async Function TestDontInsertUnnecessaryCast4() As Task
            Dim code =
<ClassDeclaration>
Sub S
    Dim [||]t = New With {.Name = ""}
    M(t)
End Sub

Sub M(o As Object)
End Sub
</ClassDeclaration>

            Dim expected =
<ClassDeclaration>
Sub S
    M(New With {.Name = ""})
End Sub

Sub M(o As Object)
End Sub
</ClassDeclaration>

            Await TestAsync(code, expected, compareTokens:=False)
        End Function

        <WorkItem(543280)>
        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsInlineTemporary)>
        Public Async Function TestDontInsertUnnecessaryCast5() As Task
            Dim code =
<File>
Option Strict On
Imports System
 
Module Program
    Sub Main
        Dim [||]x = Sub() Return
        x.Invoke()
    End Sub
End Module
</File>

            Dim expected =
<File>
Option Strict On
Imports System
 
Module Program
    Sub Main
        Call (Sub() Return).Invoke()
    End Sub
End Module
</File>

            Await TestAsync(code, expected, compareTokens:=False)
        End Function

        <WorkItem(544973)>
        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsInlineTemporary)>
        Public Async Function TestDontInsertUnnecessaryCast6() As Task
            Dim code =
<File>
Option Infer On
Option Strict On
 
Module M
    Sub Main()
        Dim x[||] = Function() 1
        Dim y = x
        Dim z = y()
    End Sub
End Module
</File>

            Dim expected =
<File>
Option Infer On
Option Strict On
 
Module M
    Sub Main()
        Dim y = Function() 1
        Dim z = y()
    End Sub
End Module
</File>

            Await TestAsync(code, expected, compareTokens:=False)
        End Function

        <WorkItem(545975)>
        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsInlineTemporary)>
        Public Async Function TestDontInsertUnnecessaryCast7() As Task
            Dim code =
<File>
Imports System
Module M
    Sub Main()
        Dim e1[||] As Exception = New ArgumentException()
        Dim t1 = e1.GetType()
    End Sub
End Module
</File>

            Dim expected =
<File>
Imports System
Module M
    Sub Main()
        Dim t1 = New ArgumentException().GetType()
    End Sub
End Module
</File>

            Await TestAsync(code, expected, compareTokens:=False)
        End Function

        <WorkItem(545846)>
        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsInlineTemporary)>
        Public Async Function TestDontInsertUnnecessaryCast8() As Task
            Dim markup =
<File>
Option Strict On
Imports System.Collections.Generic
Module M
    Sub Main()
        Dim x[||] = {1}
        Dim y = New List(Of Integer()) From {{x}}
    End Sub
End Module
</File>

            Dim expected =
<File>
Option Strict On
Imports System.Collections.Generic
Module M
    Sub Main()
        Dim y = New List(Of Integer()) From {{{1}}}
    End Sub
End Module
</File>

            Await TestAsync(markup, expected)
        End Function

        <WorkItem(545624), WorkItem(799045)>
        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsInlineTemporary)>
        Public Async Function TestDontInsertUnnecessaryCast9() As Task
            Dim markup =
<File>
Imports System.Collections.Generic
Imports System.Linq

Module Program
    Sub Main()
        Dim p[||] = {1, 2, 3}
        Dim z = p.ToList
    End Sub
End Module
</File>

            Dim expected =
<File>
Imports System.Collections.Generic
Imports System.Linq

Module Program
    Sub Main()
        Dim z = {1, 2, 3}.ToList
    End Sub
End Module
</File>

            Await TestAsync(markup, expected)
        End Function

        <WorkItem(530068)>
        <WpfFact, Trait(Traits.Feature, Traits.Features.Simplification)>
        Public Async Function TestDontInsertUnnecessaryCast10() As Task
            Dim markup =
<File>
Imports System
Class X
    Shared Sub Main()
        Dim [||]x As Object = New X()
        Dim s = x.ToString()
    End Sub
    Public Overrides Function ToString() As String
        Return MyBase.ToString()
    End Function
End Class
</File>

            Dim expected =
<File>
Imports System
Class X
    Shared Sub Main()
        Dim s = New X().ToString()
    End Sub
    Public Overrides Function ToString() As String
        Return MyBase.ToString()
    End Function
End Class
</File>

            Await TestAsync(markup, expected)
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsInlineTemporary)>
        Public Async Function TestInsertCallIfNecessary1() As Task
            Dim code =
<MethodBody>
Dim [||]x = New Exception()
x.ToString
</MethodBody>

            Dim expected =
<MethodBody>
Call New Exception().ToString
</MethodBody>

            Await TestAsync(code, expected, compareTokens:=False)
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsInlineTemporary)>
        Public Async Function TestInsertCallIfNecessary2() As Task
            Dim code =
<MethodBody>
Dim [||]x = New Exception
x.ToString
</MethodBody>

            Dim expected =
<MethodBody>
Call New Exception().ToString
</MethodBody>

            Await TestAsync(code, expected, compareTokens:=False)
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsInlineTemporary)>
        Public Async Function TestInsertCallIfNecessary3() As Task
            Dim code =
<MethodBody>
Dim [||]s As Action = Sub() Exit Sub
s
</MethodBody>

            Dim expected =
<MethodBody>
Call (Sub() Exit Sub)
</MethodBody>

            Await TestAsync(code, expected, compareTokens:=False)
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsInlineTemporary)>
        Public Async Function TestInsertCallIfNecessary4() As Task
            Dim code =
<MethodBody>
Dim [||]q = From x in "abc"
q.Distinct()
</MethodBody>

            Dim expected =
<MethodBody>
Call (From x in "abc").Distinct()
</MethodBody>

            Await TestAsync(code, expected, compareTokens:=False)
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsInlineTemporary)>
        Public Async Function TestInsertCallIfNecessary5() As Task
            Dim code =
<MethodBody>
Dim [||]s = "abc"
s.ToLower()
</MethodBody>

            Dim expected =
<MethodBody>
Call "abc".ToLower()
</MethodBody>

            Await TestAsync(code, expected, compareTokens:=False)
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsInlineTemporary)>
        Public Async Function TestInsertCallIfNecessary6() As Task
            Dim code =
<MethodBody>
Dim [||]x = 1
x.ToString()
</MethodBody>

            Dim expected =
<MethodBody>
Call 1.ToString()
</MethodBody>

            Await TestAsync(code, expected, compareTokens:=False)
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsInlineTemporary)>
        Public Async Function TestInsertCallIfNecessary7() As Task
            Dim code =
<MethodBody>
Dim [||]x = 1 + 1
x.ToString()
</MethodBody>

            Dim expected =
<MethodBody>
Call (1 + 1).ToString()
</MethodBody>

            Await TestAsync(code, expected, compareTokens:=False)
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsInlineTemporary)>
        Public Async Function TestInsertCallIfNecessary8() As Task
            Dim code =
<MethodBody>
Dim [||]x = New Exception().Message
x.ToString
</MethodBody>

            Dim expected =
<MethodBody>
Call New Exception().Message.ToString
</MethodBody>

            Await TestAsync(code, expected, compareTokens:=False)
        End Function

        <WorkItem(542819)>
        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsInlineTemporary)>
        Public Async Function TestInsertCallIfNecessary9() As Task
            Dim code =
<MethodBody>
Dim [||]x = If(True, 1, 2)
x.ToString
</MethodBody>

            Dim expected =
<MethodBody>
Call If(True, 1, 2).ToString
</MethodBody>

            Await TestAsync(code, expected, compareTokens:=False)
        End Function

        <WorkItem(542819)>
        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsInlineTemporary)>
        Public Async Function TestInsertCallIfNecessary10() As Task
            Dim code =
<MethodBody>
Dim [||]x = If(Nothing, "")
x.ToString
</MethodBody>

            Dim expected =
<MethodBody>
Call If(Nothing, "").ToString
</MethodBody>

            Await TestAsync(code, expected, compareTokens:=False)
        End Function

        <WorkItem(542667)>
        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsInlineTemporary)>
        Public Async Function TestParenthesizeIfNecessary1() As Task
            Dim code =
<MethodBody>
Dim [||]x = From y In "" Select y
Dim a = x, b
</MethodBody>

            Dim expected =
<MethodBody>
Dim a = (From y In "" Select y), b
</MethodBody>

            Await TestAsync(code, expected, compareTokens:=False)
        End Function

        <WorkItem(542667)>
        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsInlineTemporary)>
        Public Async Function TestParenthesizeIfNecessary2() As Task
            Dim code =
<MethodBody>
Dim [||]x = From y In "" Select y
Dim a = Nothing, b = x
</MethodBody>

            Dim expected =
<MethodBody>
Dim a = Nothing, b = From y In "" Select y
</MethodBody>

            Await TestAsync(code, expected, compareTokens:=False)
        End Function

        <WorkItem(542667)>
        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsInlineTemporary)>
        Public Async Function TestParenthesizeIfNecessary3() As Task
            Dim code =
<MethodBody>
Dim [||]x As Func(Of IEnumerable(Of Char)) = Function() From y In "" Select y
Dim a = x, b
</MethodBody>

            Dim expected =
<MethodBody>
Dim a = CType((Function() From y In "" Select y), Func(Of IEnumerable(Of Char))), b
</MethodBody>
            Await TestAsync(code, expected, compareTokens:=False)
        End Function

        <WorkItem(542096)>
        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsInlineTemporary)>
        Public Async Function TestParenthesizeIfNecessary4() As Task
            Dim code =
<MethodBody>
Dim [||]z As IEnumerable(Of Char) = From x In "ABC" Select x
Dim y = New IEnumerable(Of Char)() {z, z}
</MethodBody>

            Dim expected =
<MethodBody>
Dim y = New IEnumerable(Of Char)() {(From x In "ABC" Select x), From x In "ABC" Select x}
</MethodBody>

            Await TestAsync(code, expected, compareTokens:=False)
        End Function

        <WorkItem(542096)>
        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsInlineTemporary)>
        Public Async Function TestParenthesizeIfNecessary5() As Task
            Dim code =
<MethodBody>
Dim [||]z As IEnumerable(Of Char) = From x In "ABC" Select x
Dim y = New IEnumerable(Of Char)() {(From x In "ABC" Select x), z}
</MethodBody>

            Dim expected =
<MethodBody>
Dim y = New IEnumerable(Of Char)() {(From x In "ABC" Select x), From x In "ABC" Select x}
</MethodBody>

            Await TestAsync(code, expected, compareTokens:=False)
        End Function

        <WorkItem(542096)>
        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsInlineTemporary)>
        Public Async Function TestParenthesizeIfNecessary6() As Task
            Dim code =
<ModuleDeclaration>
Sub Foo()
    Dim [||]z As IEnumerable(Of Char) = From x In "ABC" Select x ' Inline z
    Bar(z, z)
End Sub

Sub Bar(Of T)(x As T, y As T)
End Sub
</ModuleDeclaration>

            Dim expected =
<ModuleDeclaration>
Sub Foo()
    ' Inline z
    Bar((From x In "ABC" Select x), From x In "ABC" Select x)
End Sub

Sub Bar(Of T)(x As T, y As T)
End Sub
</ModuleDeclaration>

            Await TestAsync(code, expected, compareTokens:=False)
        End Function

        <WorkItem(542795)>
        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsInlineTemporary)>
        Public Async Function TestParenthesizeIfNecessary7() As Task
            Dim code =
<ModuleDeclaration>
Sub Foo()
    Dim [||]z As Func(Of IEnumerable(Of Char)) = Function() From x In "ABC" Select x
    Bar(z, z)
End Sub

Sub Bar(x As Func(Of IEnumerable(Of Char)), y As Func(Of IEnumerable(Of Char)))
End Sub
</ModuleDeclaration>

            Dim expected =
<ModuleDeclaration>
Sub Foo()
    Bar((Function() From x In "ABC" Select x), Function() From x In "ABC" Select x)
End Sub

Sub Bar(x As Func(Of IEnumerable(Of Char)), y As Func(Of IEnumerable(Of Char)))
End Sub
</ModuleDeclaration>

            Await TestAsync(code, expected, compareTokens:=False)
        End Function

        <WorkItem(542667)>
        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsInlineTemporary)>
        Public Async Function TestParenthesizeIfNecessary8() As Task
            Dim code =
<MethodBody>
Dim [||]x = From y In "" Select y Order By y
Dim a = x, b
</MethodBody>

            Dim expected =
<MethodBody>
Dim a = (From y In "" Select y Order By y), b
</MethodBody>

            Await TestAsync(code, expected, compareTokens:=False)
        End Function

        <WorkItem(542795)>
        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsInlineTemporary)>
        Public Async Function TestParenthesizeIfNecessary9() As Task
            Dim code =
<ModuleDeclaration>
Sub Foo()
    Dim [||]z As Func(Of IEnumerable(Of IEnumerable(Of Char))) = Function() From x In "ABC" Select From y In "ABC" Select y
    Bar(z, z)
End Sub

Sub Bar(x As Func(Of IEnumerable(Of IEnumerable(Of Char))), y As Func(Of IEnumerable(Of IEnumerable(Of Char))))
End Sub
</ModuleDeclaration>

            Dim expected =
<ModuleDeclaration>
Sub Foo()
    Bar((Function() From x In "ABC" Select From y In "ABC" Select y), Function() From x In "ABC" Select From y In "ABC" Select y)
End Sub

Sub Bar(x As Func(Of IEnumerable(Of IEnumerable(Of Char))), y As Func(Of IEnumerable(Of IEnumerable(Of Char))))
End Sub
</ModuleDeclaration>

            Await TestAsync(code, expected, compareTokens:=False)
        End Function

        <WorkItem(542840)>
        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsInlineTemporary)>
        Public Async Function TestParenthesizeIfNecessary10() As Task
            Dim code =
<MethodBody>
Dim [||]x As Collections.ArrayList = New Collections.ArrayList()
Dim y = x(0)
</MethodBody>

            Dim expected =
<MethodBody>
Dim y = (New Collections.ArrayList())(0)
</MethodBody>

            Await TestAsync(code, expected, compareTokens:=False)
        End Function

        <WorkItem(542842)>
        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsInlineTemporary)>
        Public Async Function TestParenthesizeIfNecessary11() As Task
            Dim code =
<MethodBody>
Dim [||]y As Action = Sub() If True Then Dim x
Dim a As Action = y, b = a
</MethodBody>

            Dim expected =
<MethodBody>
Dim a As Action = (Sub() If True Then Dim x), b = a
</MethodBody>

            Await TestAsync(code, expected, compareTokens:=False)
        End Function

        <WorkItem(542667)>
        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsInlineTemporary)>
        Public Async Function TestParenthesizeIfNecessary12() As Task
            Dim code =
<MethodBody>
Dim [||]x = From y In "" Select y Order By y Ascending
Dim a = x, b
</MethodBody>

            Dim expected =
<MethodBody>
Dim a = (From y In "" Select y Order By y Ascending), b
</MethodBody>

            Await TestAsync(code, expected, compareTokens:=False)
        End Function

        <WorkItem(542840)>
        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsInlineTemporary)>
        Public Async Function TestParenthesizeIfNecessary13() As Task
            Dim code =
<MethodBody>
Dim [||]x As Collections.ArrayList = New Collections.ArrayList
Dim y = x(0)
</MethodBody>

            Dim expected =
<MethodBody>
Dim y = (New Collections.ArrayList)(0)
</MethodBody>

            Await TestAsync(code, expected, compareTokens:=False)
        End Function

        <WorkItem(542931)>
        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsInlineTemporary)>
        Public Async Function TestParenthesizeIfNecessary14() As Task
            Dim code =
<MethodBody>
Dim [||]q = From x In ""
Dim p = From y In "", z In q Distinct
</MethodBody>

            Dim expected =
<MethodBody>
Dim p = From y In "", z In (From x In "") Distinct
</MethodBody>

            Await TestAsync(code, expected, compareTokens:=False)
        End Function

        <WorkItem(542989)>
        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsInlineTemporary)>
        Public Async Function TestParenthesizeIfNecessary15() As Task
            Dim code =
<MethodBody>
Dim [||]z = From x In "" Group By x Into Count
Dim y = z(0)
</MethodBody>

            Dim expected =
<MethodBody>
Dim y = (From x In "" Group By x Into Count)(0)
</MethodBody>

            Await TestAsync(code, expected, compareTokens:=False)
        End Function

        <WorkItem(542990)>
        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsInlineTemporary)>
        Public Async Function TestParenthesizeIfNecessary16() As Task
            Dim code =
<MethodBody>
Dim [||]x = Function() Console.ReadLine
Dim y As String = x()
</MethodBody>

            Dim expected =
<MethodBody>
Dim y As String = (Function() Console.ReadLine)()
</MethodBody>

            Await TestAsync(code, expected, compareTokens:=False)
        End Function

        <WorkItem(542997)>
        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsInlineTemporary)>
        Public Async Function TestParenthesizeIfNecessary17() As Task
            Dim code =
<MethodBody>
Dim [||]s = Sub() Return
Dim q = From x In "" Select z = s Distinct
</MethodBody>

            Dim expected =
<MethodBody>
Dim q = From x In "" Select z = (Sub() Return) Distinct
</MethodBody>

            Await TestAsync(code, expected, compareTokens:=False)
        End Function

        <WorkItem(542997)>
        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsInlineTemporary)>
        Public Async Function TestParenthesizeIfNecessary18() As Task
            Dim code =
<MethodBody>
Dim [||]s = Sub() Return
Dim q = From x In "" Select z = s _
        Distinct
</MethodBody>

            Dim expected =
<MethodBody>
Dim q = From x In "" Select z = (Sub() Return) _
        Distinct
</MethodBody>

            Await TestAsync(code, expected, compareTokens:=False)
        End Function

        <WorkItem(542997)>
        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsInlineTemporary)>
        Public Async Function TestParenthesizeIfNecessary19() As Task
            Dim code =
<MethodBody>
Dim [||]s = Sub() Return
Dim q = From x In "" Select z = s
</MethodBody>

            Dim expected =
<MethodBody>
Dim q = From x In "" Select z = Sub() Return
</MethodBody>

            Await TestAsync(code, expected, compareTokens:=False)
        End Function

        <WorkItem(529694)>
        <WpfFact(), Trait(Traits.Feature, Traits.Features.CodeActionsInlineTemporary)>
        Public Async Function TestParenthesizeIfNecessary20() As Task
            Dim code =
<MethodBody>
With ""
    Dim x = From c In "" Distinct
    Dim y[||] = 1
    .ToLower()
    Console.WriteLine(y)
End With
</MethodBody>

            Dim expected =
<MethodBody>
With ""
    Dim x = From c In "" Distinct
    Call .ToLower()
    Console.WriteLine(1)
End With
</MethodBody>

            Await TestAsync(code, expected, compareTokens:=False)
        End Function

        <WorkItem(545571)>
        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsInlineTemporary)>
        Public Async Function TestParenthesizeIfNecessary21() As Task
            Dim code =
<MethodBody>
Dim y[||] = Sub() Exit Sub
y.Invoke()
</MethodBody>

            Dim expected =
<MethodBody>
Call (Sub() Exit Sub).Invoke()
</MethodBody>

            Await TestAsync(code, expected, compareTokens:=False)
        End Function

        <WorkItem(545849)>
        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsInlineTemporary)>
        Public Async Function TestParenthesizeIfNecessary22() As Task
            Dim code =
<MethodBody>
Dim x[||] = {Sub() Return}
Dim y = {x}
Console.WriteLine(y.Rank)
</MethodBody>

            Dim expected =
<MethodBody>
Dim y = {({Sub() Return})}
Console.WriteLine(y.Rank)
</MethodBody>

            Await TestAsync(code, expected, compareTokens:=False)
        End Function

        <WorkItem(531578)>
        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsInlineTemporary)>
        Public Async Function TestParenthesizeIfNecessary23() As Task
            Dim code =
<File>
Imports System
Imports System.Linq
Imports System.Text
Module Program
    Sub Main()
        With New StringBuilder
            Dim x = From c In "" Distinct
            Dim [||]y = 1
            .Length = 0
            Console.WriteLine(y)
        End With
    End Sub
End Module
</File>

            Dim expected =
<File>
Imports System
Imports System.Linq
Imports System.Text
Module Program
    Sub Main()
        With New StringBuilder
            Dim x = (From c In "" Distinct)
            .Length = 0
            Console.WriteLine(1)
        End With
    End Sub
End Module
</File>

            Await TestAsync(code, expected, compareTokens:=False)
        End Function

        <WorkItem(531582)>
        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsInlineTemporary)>
        Public Async Function TestParenthesizeIfNecessary24() As Task
            Dim code =
<MethodBody>
Dim [||]x = From z In ""
Dim y = x
Select 1
End Select
</MethodBody>

            Dim expected =
<MethodBody>
Dim y = (From z In "")
Select 1
End Select
</MethodBody>

            Await TestAsync(code, expected, compareTokens:=False)
        End Function

        <WorkItem(549182)>
        <WorkItem(549191)>
        <WorkItem(545730)>
        <WpfFact(), Trait(Traits.Feature, Traits.Features.CodeActionsInlineTemporary)>
        Public Async Function TestUnparenthesizeIfNecessary1() As Task
            Dim code =
<File>
Module A
    Sub Main()
        Dim y[||] = Preserve.X
        ReDim y(0)
    End Sub
End Module

Module Preserve
    Property X As Integer()
End Module
</File>

            Dim expected =
<File>
Module A
    Sub Main()
        ReDim [Preserve].X()(0)
    End Sub
End Module

Module Preserve
    Property X As Integer()
End Module
</File>

            Await TestAsync(code, expected, compareTokens:=False)
        End Function

        <WorkItem(542985)>
        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsInlineTemporary)>
        Public Async Function TestAddExplicitArgumentListIfNecessary1() As Task
            Dim code =
<ModuleDeclaration>
Sub Main()
    Dim [||]x = Foo
    Dim y As Integer = x(0)
End Sub

Function Foo As Integer()
End Function

Function Foo(x As Integer) As Integer()
End Function
</ModuleDeclaration>

            Dim expected =
<ModuleDeclaration>
Sub Main()
    Dim y As Integer = Foo()(0)
End Sub

Function Foo As Integer()
End Function

Function Foo(x As Integer) As Integer()
End Function
</ModuleDeclaration>

            Await TestAsync(code, expected, compareTokens:=False)
        End Function

        <WorkItem(542985)>
        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsInlineTemporary)>
        Public Async Function TestAddExplicitArgumentListIfNecessary2() As Task
            Dim code =
<ModuleDeclaration>
Sub Main()
    Dim [||]x = Foo(Of Integer)
    Dim y As Integer = x(0)
End Sub

Function Foo(Of T) As Integer()
End Function

Function Foo(Of T)(x As Integer) As Integer()
End Function
</ModuleDeclaration>

            Dim expected =
<ModuleDeclaration>
Sub Main()
    Dim y As Integer = Foo(Of Integer)()(0)
End Sub

Function Foo(Of T) As Integer()
End Function

Function Foo(Of T)(x As Integer) As Integer()
End Function
</ModuleDeclaration>

            Await TestAsync(code, expected, compareTokens:=False)
        End Function

        <WorkItem(542985)>
        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsInlineTemporary)>
        Public Async Function TestAddExplicitArgumentListIfNecessary3() As Task
            Dim code =
<ModuleDeclaration>
Sub Main()
    Dim [||]x = Foo
    Dim y As Integer = x(0)
End Sub

Property Foo As Integer()

ReadOnly Property Foo(x As Integer) As Integer()
    Get
    End Get
End Property
</ModuleDeclaration>

            Dim expected =
<ModuleDeclaration>
Sub Main()
    Dim y As Integer = Foo()(0)
End Sub

Property Foo As Integer()

ReadOnly Property Foo(x As Integer) As Integer()
    Get
    End Get
End Property
</ModuleDeclaration>

            Await TestAsync(code, expected, compareTokens:=False)
        End Function

        <WorkItem(545174)>
        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsInlineTemporary)>
        Public Async Function TestAddExplicitArgumentListIfNecessary4() As Task
            Dim code =
<ModuleDeclaration>
Module Program
    Sub Main()
        Dim x As Action = Sub() Console.WriteLine("Hello")
        Dim [||]y = x : y : Console.WriteLine()
    End Sub
End Module
</ModuleDeclaration>

            Dim expected =
<ModuleDeclaration>
Module Program
    Sub Main()
        Dim x As Action = Sub() Console.WriteLine("Hello")
        x() : Console.WriteLine()
    End Sub
End Module
</ModuleDeclaration>

            Await TestAsync(code, expected, compareTokens:=False)
        End Function

        <WorkItem(529542)>
        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsInlineTemporary)>
        Public Async Function TestAddExplicitArgumentListIfNecessary5() As Task
            Dim code =
<ModuleDeclaration>
Module Program
    Sub Main()
        Dim [||]y = x
        y
    End Sub

    Property x As Action
End Module
</ModuleDeclaration>

            Dim expected =
<ModuleDeclaration>
Module Program
    Sub Main()
        x()()
    End Sub

    Property x As Action
End Module
</ModuleDeclaration>

            Await TestAsync(code, expected, compareTokens:=False)
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsInlineTemporary)>
        Public Async Function TestConflict_Assignment() As Task
            Dim code =
<MethodBody>
Dim [||]i As Integer = 1
i = 2
Console.WriteLine(i)
</MethodBody>

            Dim expected =
<MethodBody>
Dim i As Integer = 1
{|Conflict:i|} = 2
Console.WriteLine(1)
</MethodBody>

            Await TestAsync(code, expected, compareTokens:=False)
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsInlineTemporary)>
        Public Async Function TestConflict_AddAssignment() As Task
            Dim code =
<MethodBody>
Dim [||]i As Integer = 1
i += 2
Console.WriteLine(i)
</MethodBody>

            Dim expected =
<MethodBody>
Dim i As Integer = 1
{|Conflict:i|} += 2
Console.WriteLine(1)
</MethodBody>

            Await TestAsync(code, expected, compareTokens:=False)
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsInlineTemporary)>
        Public Async Function TestConflict_SubtractAssignment() As Task
            Dim code =
<MethodBody>
Dim [||]i As Integer = 1
i -= 2
Console.WriteLine(i)
</MethodBody>

            Dim expected =
<MethodBody>
Dim i As Integer = 1
{|Conflict:i|} -= 2
Console.WriteLine(1)
</MethodBody>

            Await TestAsync(code, expected, compareTokens:=False)
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsInlineTemporary)>
        Public Async Function TestConflict_MultiplyAssignment() As Task
            Dim code =
<MethodBody>
Dim [||]i As Integer = 1
i *= 2
Console.WriteLine(i)
</MethodBody>

            Dim expected =
<MethodBody>
Dim i As Integer = 1
{|Conflict:i|} *= 2
Console.WriteLine(1)
</MethodBody>

            Await TestAsync(code, expected, compareTokens:=False)
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsInlineTemporary)>
        Public Async Function TestConflict_DivideAssignment1() As Task
            Dim code =
<MethodBody>
Dim [||]i As Integer = 1
i /= 2
Console.WriteLine(i)
</MethodBody>

            Dim expected =
<MethodBody>
Dim i As Integer = 1
{|Conflict:i|} /= 2
Console.WriteLine(1)
</MethodBody>

            Await TestAsync(code, expected, compareTokens:=False)
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsInlineTemporary)>
        Public Async Function TestConflict_IntegerDivideAssignment() As Task
            Dim code =
<MethodBody>
Dim [||]i As Integer = 1
i \= 2
Console.WriteLine(i)
</MethodBody>

            Dim expected =
<MethodBody>
Dim i As Integer = 1
{|Conflict:i|} \= 2
Console.WriteLine(1)
</MethodBody>

            Await TestAsync(code, expected, compareTokens:=False)
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsInlineTemporary)>
        Public Async Function TestConflict_ConcatenateAssignment() As Task
            Dim code =
<MethodBody>
Dim [||]i As Integer = 1
i &amp;= 2
Console.WriteLine(i)
</MethodBody>

            Dim expected =
<MethodBody>
Dim i As Integer = 1
{|Conflict:i|} &amp;= 2
Console.WriteLine(1)
</MethodBody>

            Await TestAsync(code, expected, compareTokens:=False)
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsInlineTemporary)>
        Public Async Function TestConflict_LeftShiftAssignment() As Task
            Dim code =
<MethodBody>
Dim [||]i As Integer = 1
i &lt;&lt;= 2
Console.WriteLine(i)
</MethodBody>

            Dim expected =
<MethodBody>
Dim i As Integer = 1
{|Conflict:i|} &lt;&lt;= 2
Console.WriteLine(1)
</MethodBody>

            Await TestAsync(code, expected, compareTokens:=False)
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsInlineTemporary)>
        Public Async Function TestConflict_RightShiftAssignment() As Task
            Dim code =
<MethodBody>
Dim [||]i As Integer = 1
i &gt;&gt;= 2
Console.WriteLine(i)
</MethodBody>

            Dim expected =
<MethodBody>
Dim i As Integer = 1
{|Conflict:i|} &gt;&gt;= 2
Console.WriteLine(1)
</MethodBody>

            Await TestAsync(code, expected, compareTokens:=False)
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsInlineTemporary)>
        Public Async Function TestConflict_PowerAssignment() As Task
            Dim code =
<MethodBody>
Dim [||]i As Integer = 1
i ^= 2
Console.WriteLine(i)
</MethodBody>

            Dim expected =
<MethodBody>
Dim i As Integer = 1
{|Conflict:i|} ^= 2
Console.WriteLine(1)
</MethodBody>

            Await TestAsync(code, expected, compareTokens:=False)
        End Function

        <WorkItem(529627)>
        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsInlineTemporary)>
        Public Async Function TestConflict_ByRefLiteral() As Task
            Dim code =
<File>
Module Program
    Sub Main(args As String())
        Dim bar[||] As String = "TEST"
        foo(bar)
        Console.WriteLine(bar)
    End Sub

    Private Sub foo(ByRef bar As String)
        bar = "foo"
    End Sub
End Module
</File>

            Dim expected =
<File>
Module Program
    Sub Main(args As String())
        Dim bar As String = "TEST"
        foo({|Conflict:bar|})
        Console.WriteLine("TEST")
    End Sub

    Private Sub foo(ByRef bar As String)
        bar = "foo"
    End Sub
End Module
</File>

            Await TestAsync(code, expected, compareTokens:=False)
        End Function

        <WorkItem(545342)>
        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsInlineTemporary)>
        Public Async Function TestConflict_UsedBeforeDeclaration() As Task

            Dim code =
<File>
Module Program
    Sub Main(args As String())
        Dim x = y
        Dim y[||] = 45
    End Sub
End Module
</File>

            Dim expected =
<File>
Module Program
    Sub Main(args As String())
        Dim x = {|Conflict:y|}
        Dim y = 45
    End Sub
End Module
</File>

            Await TestAsync(code, expected, compareTokens:=False)
        End Function

        <WorkItem(545398)>
        <WorkItem(568917)>
        <WpfFact(), Trait(Traits.Feature, Traits.Features.CodeActionsInlineTemporary)>
        Public Async Function TestInsertCorrectCastsForAssignmentStatement1() As Task
            Dim code =
<File>
Option Explicit Off

Module Program
    Sub Main(args As String())
        Dim y[||] As Integer = q
        z = y
    End Sub
End Module
</File>

            Dim expected =
<File>
Option Explicit Off

Module Program
    Sub Main(args As String())
        z = CInt(q)
    End Sub
End Module
</File>

            Await TestAsync(code, expected, compareTokens:=False)
        End Function

        <WorkItem(545398)>
        <WorkItem(568917)>
        <WpfFact(), Trait(Traits.Feature, Traits.Features.CodeActionsInlineTemporary)>
        Public Async Function TestInsertCorrectCastsForAssignmentStatement2() As Task
            Dim code =
<File>
Option Explicit Off

Module Program
    Sub Main(args As String())
        Dim y2[||] As Integer = q2
        Dim z2 As Object = y2
    End Sub
End Module
</File>

            Dim expected =
<File>
Option Explicit Off

Module Program
    Sub Main(args As String())
        Dim z2 As Object = CInt(q2)
    End Sub
End Module
</File>

            Await TestAsync(code, expected, compareTokens:=False)
        End Function

        <WorkItem(545398)>
        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsInlineTemporary)>
        Public Async Function TestInsertCorrectCastsForAssignmentStatement3() As Task
            Dim code =
<File>
Option Infer Off

Module Program
    Sub Main(args As String())
        Dim y[||] As Integer = 42
        Dim z = y
    End Sub
End Module
</File>

            Dim expected =
<File>
Option Infer Off

Module Program
    Sub Main(args As String())
        Dim z = 42
    End Sub
End Module
</File>

            Await TestAsync(code, expected, compareTokens:=False)
        End Function

        <WorkItem(545539)>
        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsInlineTemporary)>
        Public Async Function TestDontOverparenthesizeXmlAttributeAccessExpression() As Task
            Dim code =
<File>
Imports System.Xml.Linq
Module M
    Sub Main()
        ' Inline a
        Dim [||]a = &lt;x/&gt;.@a
        Dim b = a : Return
    End Sub
End Module
</File>

            Dim expected =
<File>
Imports System.Xml.Linq
Module M
    Sub Main()
        ' Inline a
        Dim b = &lt;x/&gt;.@a : Return
    End Sub
End Module
</File>

            Await TestAsync(code, expected, compareTokens:=False)
        End Function

        <WorkItem(546069)>
        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsInlineTemporary)>
        Public Async Function TestBrokenVariableDeclarator() As Task
            Dim code =
<File>
Module M
    Sub Main()
        Dim [||]a(10 = {0,0}
        System.Console.WriteLine(a)
    End Sub
End Module
</File>

            Await TestMissingAsync(code)
        End Function

        <WorkItem(546658)>
        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsInlineTemporary)>
        Public Async Function TestDontInlineInUnterminatedBlock() As Task
            Dim markup =
<File>
Interface IFoo
    Function Foo(baz As IBaz) As IBar
End Interface
 
Interface IBar
End Interface
 
Interface IBaz
End Interface
 
Module M
    Dim foo As IFoo
 
    Sub M()
        Using nonexistent
            Dim [||]localFoo = foo
        Dim baz As IBaz
        Dim result = localFoo.Foo(baz)
    End Sub
End Module
</File>

            Await TestMissingAsync(markup)
        End Function

        <WorkItem(547152)>
        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsInlineTemporary)>
        Public Async Function TestEscapeKeywordsIfNeeded1() As Task
            Dim code =
<File>
Imports System.Linq
Module Program
    Sub Main(args As String())
        Dim y = From x In "" Distinct, [||]z = 1
        Take()
        Console.WriteLine(z)
    End Sub
    Sub Take()
    End Sub
End Module
</File>

            Dim expected =
<File>
Imports System.Linq
Module Program
    Sub Main(args As String())
        Dim y = From x In "" Distinct
        [Take]()
        Console.WriteLine(1)
    End Sub
    Sub Take()
    End Sub
End Module
</File>

            Await TestAsync(code, expected, compareTokens:=False)
        End Function

        <WorkItem(531473)>
        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsInlineTemporary)>
        Public Async Function TestEscapeKeywordsIfNeeded2() As Task
            Dim code =
<File>
Imports System.Linq
Module Program
    Sub Main()
        Dim [||]x = From z In ""
        Dim y = x
        Take()
    End Sub

    Sub Take()
    End Sub
End Module
</File>

            Dim expected =
<File>
Imports System.Linq
Module Program
    Sub Main()
        Dim y = From z In ""
        [Take]()
    End Sub

    Sub Take()
    End Sub
End Module
</File>

            Await TestAsync(code, expected, compareTokens:=False)
        End Function

        <WorkItem(531473)>
        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsInlineTemporary)>
        Public Async Function TestEscapeKeywordsIfNeeded3() As Task
            Dim code =
<File>
Imports System.Linq
Module Program
    Sub Main()
        Dim y = From x In ""
        Dim [||]z = 1
        Take()
        Console.WriteLine(z)
    End Sub
    Sub Take()
    End Sub
End Module
</File>

            Dim expected =
<File>
Imports System.Linq
Module Program
    Sub Main()
        Dim y = From x In ""
        [Take]()
        Console.WriteLine(1)
    End Sub
    Sub Take()
    End Sub
End Module
</File>

            Await TestAsync(code, expected, compareTokens:=False)
        End Function

        <WorkItem(547153)>
        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsInlineTemporary)>
        Public Async Function TestEscapeKeywordsIfNeeded4() As Task
            Dim code =
<File>
Imports System.Linq
Module Program
    Sub Main()
        Dim [||]x = Take(Of Integer)()
        Dim y = From z In ""
        x.ToString()
    End Sub

    Function Take(Of T)()
    End Function
End Module
</File>

            Dim expected =
<File>
Imports System.Linq
Module Program
    Sub Main()
        Dim y = From z In ""
        [Take](Of Integer)().ToString()
    End Sub

    Function Take(Of T)()
    End Function
End Module
</File>

            Await TestAsync(code, expected, compareTokens:=False)
        End Function

        <WorkItem(531584)>
        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsInlineTemporary)>
        Public Async Function TestEscapeKeywordsIfNeeded5() As Task
            Dim code =
<File>
Imports System.Linq
Module Program
    Sub Main()
        Dim y = From x In ""
 _
        Dim z[||] = 1
        Take()
        Dim t = z
    End Sub
    Sub Take()
    End Sub
End Module
</File>

            Dim expected =
<File>
Imports System.Linq
Module Program
    Sub Main()
        Dim y = From x In ""
 _
        [Take]()
        Dim t = 1
    End Sub
    Sub Take()
    End Sub
End Module
</File>

            Await TestAsync(code, expected, compareTokens:=False)
        End Function

        <WorkItem(601123)>
        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsInlineTemporary)>
        Public Async Function TestEscapeKeywordsIfNeeded6() As Task
            Dim code =
<File>
Module M
    Sub F()
        Dim a[||] = Await()
        Dim b = Await()
        Dim singleW = Async Function() a
        Dim singleWo = Function() a

        Dim MultiW = Async Function()
                         System.Console.WriteLine("Nothing")
                         Return a
                     End Function
        Dim MultiWo = Function()
                          System.Console.WriteLine("Nothing")
                          Return a
                      End Function
    End Sub
    Function Await() As String
    End Function
End Module
</File>

            Dim expected =
<File>
Module M
    Sub F()
        Dim b = Await()
        Dim singleW = Async Function() [Await]()
        Dim singleWo = Function() Await()

        Dim MultiW = Async Function()
                         System.Console.WriteLine("Nothing")
                         Return [Await]()
                     End Function
        Dim MultiWo = Function()
                          System.Console.WriteLine("Nothing")
                          Return Await()
                      End Function
    End Sub
    Function Await() As String
    End Function
End Module
</File>

            Await TestAsync(code, expected, compareTokens:=False)
        End Function

        <WorkItem(580495)>
        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsInlineTemporary)>
        Public Async Function TestParenthesizeLambdaIfNeeded01() As Task
            Dim code =
<File>
Module Program
    Sub Main()
        Dim x[||] = Sub() If True Then Dim y = Sub(z As Integer)
                                           End Sub
        x.Invoke()
    End Sub
End Module
</File>

            Dim expected =
<File>
Module Program
    Sub Main()
        Call (Sub() If True Then Dim y = Sub(z As Integer)
                                         End Sub).Invoke()
    End Sub
End Module
</File>

            Await TestAsync(code, expected, compareTokens:=False)
        End Function

        <WorkItem(607520)>
        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsInlineTemporary)>
        Public Async Function TestParenthesizeLambdaIfNeeded02() As Task
            Dim code =
<File>
Module Program
    Sub Main()
        Dim x[||] = Sub() If True Then Dim y = Sub(z As Integer)
                                           End Sub
        x()
    End Sub
End Module
</File>

            Dim expected =
<File>
Module Program
    Sub Main()
        Call (Sub() If True Then Dim y = Sub(z As Integer)
                                         End Sub)()
    End Sub
End Module
</File>

            Await TestAsync(code, expected, compareTokens:=False)
        End Function

        <WorkItem(607520)>
        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsInlineTemporary)>
        Public Async Function TestParenthesizeLambdaIfNeeded03() As Task
            Dim code =
<File>
Module Program
    Sub Main()
        Dim increment1[||] = Function(x) x + 1
        Console.WriteLine(increment1(1))
    End Sub
End Module
</File>

            Dim expected =
<File>
Module Program
    Sub Main()
        Console.WriteLine((Function(x) x + 1)(1))
    End Sub
End Module
</File>

            Await TestAsync(code, expected, compareTokens:=False)
        End Function

        <WorkItem(621407)>
        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsInlineTemporary)>
        Public Async Function TestParenthesizeLambdaIfNeeded04() As Task
            Dim code =
<File>
Module Program
    Sub Main()
        Dim x[||] = Sub() If True Then Dim y = Sub()
                                           End Sub
        Dim z As Boolean = x Is Nothing
    End Sub
End Module
</File>

            Dim expected =
<File>
Module Program
    Sub Main()
        Dim z As Boolean = (Sub() If True Then Dim y = Sub()
                                                       End Sub) Is Nothing
    End Sub
End Module
</File>
            Await TestAsync(code, expected, compareTokens:=False)
        End Function

        <WorkItem(608208)>
        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsInlineTemporary)>
        Public Async Function TestParenthesizeLambdaIfNeeded05() As Task
            Dim code =
<File>
Module Program
    Sub Main()
        Dim a(0)
        Dim b[||] = Sub() ReDim a(1)
        Dim c = b, d
    End Sub
End Module
</File>

            Dim expected =
<File>
Module Program
    Sub Main()
        Dim a(0)
        Dim c = (Sub() ReDim a(1)), d
    End Sub
End Module
</File>
            Await TestAsync(code, expected, compareTokens:=False)
        End Function

        <WorkItem(621407)>
        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsInlineTemporary)>
        Public Async Function TestParenthesizeLambdaIfNeeded06() As Task
            Dim code =
<File>
Module Program
    Sub Main()
        Dim x[||] = Sub() If True Then Dim y = Sub()
                                           End Sub
        Dim z As Boolean = TypeOf x Is Object
    End Sub
End Module
</File>

            Dim expected =
<File>
Module Program
    Sub Main()
        Dim z As Boolean = TypeOf (Sub() If True Then Dim y = Sub()
                                                              End Sub) Is Object
    End Sub
End Module
</File>
            Await TestAsync(code, expected, compareTokens:=False)
        End Function

        <WorkItem(621407)>
        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsInlineTemporary)>
        Public Async Function TestParenthesizeLambdaIfNeeded06_1() As Task
            Dim code =
<File>
Module Program
    Sub Main()
        Dim x[||] = Sub() If True Then Dim y = Sub()
                                           End Sub
        Dim z As Boolean = TypeOf x IsNot Object
    End Sub
End Module
</File>

            Dim expected =
<File>
Module Program
    Sub Main()
        Dim z As Boolean = TypeOf (Sub() If True Then Dim y = Sub()
                                                              End Sub) IsNot Object
    End Sub
End Module
</File>
            Await TestAsync(code, expected, compareTokens:=False)
        End Function

        <WorkItem(608995)>
        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsInlineTemporary)>
        Public Async Function TestParenthesizeLambdaIfNeeded07() As Task
            Dim code =
<File>
Module M
    Sub Main()
        Dim x = Sub() If True Then, [||]y = 1
        Dim z = y
    End Sub
End Module

</File>

            Dim expected =
<File>
Module M
    Sub Main()
        Dim x = (Sub() If True Then)
        Dim z = 1
    End Sub
End Module
</File>
            Await TestAsync(code, expected, compareTokens:=False)
        End Function

        <WorkItem(588344)>
        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsInlineTemporary)>
        Public Async Function TestParenthesizeXmlLiteralExpressionIfNeeded() As Task
            Dim code =
<File>
Module M
    Sub Foo()
        Dim x[||] = &lt;x/&gt;.GetHashCode
        Dim y = 1 &lt; x
        Dim z = x
    End Sub
End Module
</File>

            Dim expected =
<File>
Module M
    Sub Foo()
        Dim y = 1 &lt; (&lt;x/&gt;.GetHashCode)
        Dim z = &lt;x/&gt;.GetHashCode
    End Sub
End Module
</File>

            Await TestAsync(code, expected, compareTokens:=False)
        End Function

        <WorkItem(608204)>
        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsInlineTemporary)>
        Public Async Function TestParenthesizeQueryExpressionIfFollowedBySelect() As Task
            Dim code =
<File>
Module Program
    Sub Main()
        Dim y[||] = Function() From x In ""
        Dim z = y
        Select 1
        End Select
    End Sub
End Module
</File>

            Dim expected =
<File>
Module Program
    Sub Main()
        Dim z = (Function() From x In "")
        Select 1
        End Select
    End Sub
End Module
</File>

            Await TestAsync(code, expected, compareTokens:=False)
        End Function

        <WorkItem(635364)>
        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsInlineTemporary)>
        Public Async Function TestParenthesizeQueryExpressionIfFollowedBySelect_635364() As Task
            Dim code =
<File>
Imports System
Imports System.Collections.Generic
Imports System.Linq
Module Program
    Sub Main()
        Dim y[||] = Nothing Is From x In ""
        Dim z = y
        Select 1
        End Select
    End Sub
End Module
</File>

            Dim expected =
<File>
Imports System
Imports System.Collections.Generic
Imports System.Linq
Module Program
    Sub Main()
        Dim z = (Nothing Is From x In "")
        Select 1
        End Select
    End Sub
End Module
</File>

            Await TestAsync(code, expected, compareTokens:=False)
        End Function

        <WorkItem(635373)>
        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsInlineTemporary)>
        Public Async Function TestParenthesizeQueryExpressionIfFollowedBySelect_635373() As Task
            Dim code =
<File>
Imports System.Runtime.CompilerServices
Module Program
    Sub Main()
        Dim a As Action
        Dim y[||] = Sub() Call From x In a
        Dim z = y
        Select 1
        End Select
    End Sub
    &lt;Extension&gt;
    Function [Select](p As Action, q As Func(Of Integer, Integer)) As Action
    End Function
End Module
</File>

            Dim expected =
<File>
Imports System.Runtime.CompilerServices
Module Program
    Sub Main()
        Dim a As Action
        Dim z = (Sub() Call From x In a)
        Select 1
        End Select
    End Sub
    &lt;Extension&gt;
    Function [Select](p As Action, q As Func(Of Integer, Integer)) As Action
    End Function
End Module
</File>

            Await TestAsync(code, expected, compareTokens:=False)
        End Function

        <WorkItem(608202)>
        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsInlineTemporary)>
        Public Async Function TestParenthesizeQueryExpressionIfEndingWithDistinct() As Task
            Dim code =
<File>
Imports System
Imports System.Collections
Imports System.Linq
Module Program
    Sub Main()
        With New Hashtable
            Dim x[||] = From c In "" Distinct
            Dim y = x
            !A = !B
        End With
    End Sub
End Module
</File>

            Dim expected =
<File>
Imports System
Imports System.Collections
Imports System.Linq
Module Program
    Sub Main()
        With New Hashtable
            Dim y = (From c In "" Distinct)
            !A = !B
        End With
    End Sub
End Module
</File>

            Await TestAsync(code, expected, compareTokens:=False)
        End Function

        <WorkItem(530129)>
        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsInlineTemporary)>
        Public Async Function TestConvertsDelegateInvocationToLabel() As Task
            Dim code =
<File>
Imports System
Imports System.Collections
Imports System.Linq
Module Program
    Sub Main()
        Dim x As Action = Sub() Console.WriteLine("Hello")
        Dim y[||] = 1 : x() : Console.WriteLine(y)
    End Sub
End Module
</File>

            Dim expected =
<File>
Imports System
Imports System.Collections
Imports System.Linq
Module Program
    Sub Main()
        Dim x As Action = Sub() Console.WriteLine("Hello")
        x() : Console.WriteLine(1)
    End Sub
End Module
</File>

            Await TestAsync(code, expected, compareTokens:=False)
        End Function

        <WorkItem(529796)>
        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsInlineTemporary)>
        Public Async Function TestConvertExtensionMethodInvocationToPlainStaticMethodInvocationIfNecessaryToKeepCorrectOverloadResolution() As Task
            Dim code =
<File>
Option Strict On

Imports System.Runtime.CompilerServices

Module Program
    Sub Main()
        Dim s = ""
        Dim y[||] = 1
        s.Foo(y)
    End Sub
End Module

Module M
    Sub Main()
    End Sub
    &lt;Extension>
    Sub Foo(x As String, ByRef y As Long)
    End Sub
End Module

Module N
    &lt;Extension>
    Sub Foo(x As String, y As Long)
    End Sub
End Module
</File>

            Dim expected =
<File>
Option Strict On

Imports System.Runtime.CompilerServices

Module Program
    Sub Main()
        Dim s = ""
        N.Foo(s, 1)
    End Sub
End Module

Module M
    Sub Main()
    End Sub
    &lt;Extension>
    Sub Foo(x As String, ByRef y As Long)
    End Sub
End Module

Module N
    &lt;Extension>
    Sub Foo(x As String, y As Long)
    End Sub
End Module
</File>

            Await TestAsync(code, expected, compareTokens:=False)
        End Function

        <WorkItem(601907)>
        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsInlineTemporary)>
        Public Async Function TestEscapeContextualKeywordAfterQueryEndingWithXmlDocumentEvenWithMultipleEmptyLines() As Task
            Dim code =
<File>
Imports System.Xml
Imports System.Linq

Module M
    Sub Main()
        Dim x[||] = From y In ""
                Select &lt;?xml version="1.0"?>
                       &lt;root/>
        Dim z = x

        Distinct()
    End Sub

    Sub Distinct()
    End Sub
End Module
</File>

            Dim expected =
<File>
Imports System.Xml
Imports System.Linq

Module M
    Sub Main()
        Dim z[||] = From y In ""
                Select &lt;?xml version="1.0"?>
                       &lt;root/>

        [Distinct]()
    End Sub

    Sub Distinct()
    End Sub
End Module
</File>

            Await TestAsync(code, expected, compareTokens:=False)
        End Function

        <WorkItem(530903)>
        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsInlineTemporary)>
        Public Async Function TestInlineTempShouldParenthesizeExpressionIfNeeded() As Task
            Dim code =
<File>
Module Program
    Sub Main()
        Dim a(0)
        Dim b[||] = Sub() ReDim a(1) ' Inline b
        Dim c = b, d = 1
    End Sub
End Module
</File>

            Dim expected =
<File>
Module Program
    Sub Main()
        Dim a(0)
        ' Inline b
        Dim c = (Sub() ReDim a(1)), d = 1
    End Sub
End Module
</File>

            Await TestAsync(code, expected, compareTokens:=False)
        End Function

        <WorkItem(530945)>
        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsInlineTemporary)>
        Public Async Function TestInlineTempShouldParenthesizeLambdaExpressionIfNeeded() As Task
            Dim code =
<File>
Module Program
    Sub Main()
        If True Then
            Dim x[||] = Sub() If True Then Return ' Inline x
            Dim y = x : ElseIf True Then
            Return
        End If
    End Sub
End Module
</File>

            Dim expected =
<File>
Module Program
    Sub Main()
        If True Then
            ' Inline x
            Dim y = (Sub() If True Then Return) : ElseIf True Then
            Return
        End If
    End Sub
End Module
</File>

            Await TestAsync(code, expected, compareTokens:=False)
        End Function

        <WorkItem(530926)>
        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsInlineTemporary)>
        Public Async Function TestInlineTempShouldNotAddUnnecessaryCallKeyword() As Task
            Dim code =
<File>
Module Program
    Sub Main()
        Dim x[||] = Long.MinValue
        x.ToString()
    End Sub
End Module
</File>

            Dim expected =
<File>
Module Program
    Sub Main()
        Long.MinValue.ToString()
    End Sub
End Module
</File>

            Await TestAsync(code, expected, compareTokens:=False)
        End Function

        <WorkItem(529833)>
        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsInlineTemporary)>
        Public Async Function TestInlineTempChangesSymbolInfoForInlinedExpression() As Task
            Dim code =
<File>
Option Strict On

Module M
    Sub Main()
        With ""
            Dim x[||] = .Equals("", "", StringComparison.InvariantCulture) ' Inline x
            Dim y = New List(Of String) With {.Capacity = x.GetHashCode}
        End With
    End Sub
End Module
</File>

            Dim expected =
<File>
Option Strict On

Module M
    Sub Main()
        With ""
            Dim x = .Equals("", "", StringComparison.InvariantCulture) ' Inline x
            Dim y = New List(Of String) With {.Capacity = CBool({|Conflict:.Equals("", "", StringComparison.InvariantCulture)|}).GetHashCode}
        End With
    End Sub
End Module
</File>

            Await TestAsync(code, expected, compareTokens:=False)
        End Function

        <WorkItem(529833)>
        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsInlineTemporary)>
        Public Async Function TestInlineTempWithUserDefinedOperator() As Task
            Dim code =
<File>
Option Strict On
Imports System

Public Class X
    Shared Sub Main()
        Dim a[||] As X = 42
        Console.WriteLine(a)
    End Sub

    Public Shared Operator +(x As X, y As X) As X
        Console.WriteLine("+ Operator Invoked")
        Return x
    End Operator

    Public Shared Widening Operator CType(ByVal x As Integer) As X
        Console.WriteLine("Widening Operator Invoked")
        Return New X()
    End Operator
End Class
</File>

            Dim expected =
<File>
Option Strict On
Imports System

Public Class X
    Shared Sub Main()
        Console.WriteLine(CType(42, X))
    End Sub

    Public Shared Operator +(x As X, y As X) As X
        Console.WriteLine("+ Operator Invoked")
        Return x
    End Operator

    Public Shared Widening Operator CType(ByVal x As Integer) As X
        Console.WriteLine("Widening Operator Invoked")
        Return New X()
    End Operator
End Class
</File>

            Await TestAsync(code, expected, compareTokens:=False)
        End Function

        <WorkItem(529833)>
        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsInlineTemporary)>
        Public Async Function TestInlineTempWithUserDefinedOperator2() As Task
            Dim code =
<File>
Option Strict On
Imports System

Public Class X
    Shared Sub Main()
        Dim a[||] As X = 42
        Console.WriteLine(a + a)
    End Sub

    Public Shared Operator +(x As X, y As X) As X
        Console.WriteLine("+ Operator Invoked")
        Return x
    End Operator

    Public Shared Widening Operator CType(ByVal x As Integer) As X
        Console.WriteLine("Widening Operator Invoked")
        Return New X()
    End Operator
End Class
</File>

            Dim expected =
<File>
Option Strict On
Imports System

Public Class X
    Shared Sub Main()
        Console.WriteLine(42 + CType(42, X))
    End Sub

    Public Shared Operator +(x As X, y As X) As X
        Console.WriteLine("+ Operator Invoked")
        Return x
    End Operator

    Public Shared Widening Operator CType(ByVal x As Integer) As X
        Console.WriteLine("Widening Operator Invoked")
        Return New X()
    End Operator
End Class
</File>

            Await TestAsync(code, expected, compareTokens:=False)
        End Function

        <WorkItem(529840)>
        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsInlineTemporary)>
        Public Async Function TestBugfix_529840_DetectSemanticChangesAtInlineSite() As Task
            Dim code =
<File>
Public Class A
    Public Shared Sub Main()
        Dim a[||] = New A() ' Inline a
        Foo(a)
    End Sub

    Private Shared Sub Foo(x As Long)
        Console.WriteLine(x)
    End Sub

    Public Shared Widening Operator CType(x As A) As Integer
        Return 1
    End Operator

    Public Shared Narrowing Operator CType(x As A) As Long
        Return 2
    End Operator
End Class
</File>

            Dim expected =
<File>
Public Class A
    Public Shared Sub Main()
        ' Inline a
        Foo(New A())
    End Sub

    Private Shared Sub Foo(x As Long)
        Console.WriteLine(x)
    End Sub

    Public Shared Widening Operator CType(x As A) As Integer
        Return 1
    End Operator

    Public Shared Narrowing Operator CType(x As A) As Long
        Return 2
    End Operator
End Class
</File>

            Await TestAsync(code, expected, compareTokens:=False)
        End Function

        <WorkItem(718152)>
        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsInlineTemporary)>
        Public Async Function TestBugfix_718152_DontRemoveParenthesisForAwaitExpression() As Task
            Dim code =
<File>
Imports System
Imports System.Collections.Generic
Imports System.Linq
Imports System.Threading.Tasks
Class X
    Public Async Sub Test(i As Integer)
        Dim s[||] = Await Task.Run(Function() i)
        i = s.ToString()
        Console.WriteLine(i)
    End Sub
End Class
</File>

            Dim expected =
<File>
Imports System
Imports System.Collections.Generic
Imports System.Linq
Imports System.Threading.Tasks
Class X
    Public Async Sub Test(i As Integer)
        i = (Await Task.Run(Function() i)).ToString()
        Console.WriteLine(i)
    End Sub
End Class
</File>

            Await TestAsync(code, expected, compareTokens:=False)
        End Function

        <WorkItem(718152)>
        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsInlineTemporary)>
        Public Async Function TestBugfix_718152_RemoveParenthesisForAwaitExpression() As Task
            Dim code =
<File>
Imports System
Imports System.Collections.Generic
Imports System.Linq
Imports System.Threading.Tasks
Class X
    Public Async Sub Test(i As Integer)
        Dim s[||] = Await Task.Run(Function() i)
        Foo(s, 5)
        Console.WriteLine(i)
    End Sub
    Public Sub Foo(i1 as Integer, i2 as Integer)
    End Sub
End Class
</File>

            Dim expected =
<File>
Imports System
Imports System.Collections.Generic
Imports System.Linq
Imports System.Threading.Tasks
Class X
    Public Async Sub Test(i As Integer)
        Foo(Await Task.Run(Function() i), 5)
        Console.WriteLine(i)
    End Sub
    Public Sub Foo(i1 as Integer, i2 as Integer)
    End Sub
End Class
</File>

            Await TestAsync(code, expected, compareTokens:=False)
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsInlineTemporary)>
        Public Async Function TestNameOfExpressionAtStartOfStatement() As Task
            Dim code =
<File>
Class C
    Sub M(i As Integer)
        Dim s[||] = NameOf(i)
        s.ToString()
    End Sub
End Class
</File>

            Dim expected =
<File>
Class C
    Sub M(i As Integer)
        Call NameOf(i).ToString()
    End Sub
End Class
</File>

            Await TestAsync(code, expected, compareTokens:=False)
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsInlineTemporary)>
        Public Async Function TestSimpleConditionalAccess() As Task
            Dim code =
<File>
Class C
    Sub M(args As String())
        Dim [|x|] = args.Length.ToString()
        Dim y = x?.ToString()
        Dim y1 = x?!dictionarykey
        Dim y2 = x?.&lt;xmlelement&gt;
        Dim y3 = x?...&lt;xmldescendant&gt;
        Dim y4 = x?.@xmlattribute
    End Sub
End Class
</File>

            Dim expected =
<File>
Class C
    Sub M(args As String())
        Dim y = args.Length.ToString()?.ToString()
        Dim y1 = args.Length.ToString()?!dictionarykey
        Dim y2 = args.Length.ToString()?.&lt;xmlelement&gt;
        Dim y3 = args.Length.ToString()?...&lt;xmldescendant&gt;
        Dim y4 = args.Length.ToString()?.@xmlattribute
    End Sub
End Class
</File>

            Await TestAsync(code, expected, compareTokens:=False)
        End Function

        <WorkItem(1025, "https://github.com/dotnet/roslyn/issues/1025")>
        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsInlineTemporary)>
        Public Async Function TestConditionalAccessWithConversion() As Task
            Dim code =
<File>
Class C
    Function M(args As String()) As Boolean
        Dim [|x|] = args(0)
        Return x?.Length = 0
    End Function
End Class
</File>

            Dim expected =
<File>
Class C
    Function M(args As String()) As Boolean
        Return args(0)?.Length = 0
    End Function
End Class
</File>

            Await TestAsync(code, expected, compareTokens:=False)
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsInlineTemporary)>
        Public Async Function TestConditionalAccessWithConditionalExpression() As Task
            Dim code =
<File>
Class C
    Sub M(args As String())
        Dim [|x|] = If(args(0)?.Length, 10)
        Dim y = If(x = 10, 10, 4)
    End Sub
End Class
</File>

            Dim expected =
<File>
Class C
    Sub M(args As String())
        Dim y = If(If(args(0)?.Length, 10) = 10, 10, 4)
    End Sub
End Class
</File>

            Await TestAsync(code, expected, compareTokens:=False)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsInlineTemporary)>
        <WorkItem(2593, "https://github.com/dotnet/roslyn/issues/2593")>
        Public Async Function TestConditionalAccessWithExtensionMethodInvocation() As Task
            Dim code =
<File><![CDATA[
Imports System.Collections.Generic
Imports System.Linq
Imports System.Runtime.CompilerServices

Module M
    <Extension()>
    Public Function Something(cust As C) As IEnumerable(Of String)
        Throw New NotImplementedException()
    End Function
End Module

Class C
    Private Function GetAssemblyIdentity(types As IEnumerable(Of C)) As Object
        For Each t In types
            Dim [|assembly|] = t?.Something().First()
            Dim identity = assembly?.ToArray()
        Next
        Return Nothing
    End Function
End Class]]>
</File>

            Dim expected =
<File><![CDATA[
Imports System.Collections.Generic
Imports System.Linq
Imports System.Runtime.CompilerServices

Module M
    <Extension()>
    Public Function Something(cust As C) As IEnumerable(Of String)
        Throw New NotImplementedException()
    End Function
End Module

Class C
    Private Function GetAssemblyIdentity(types As IEnumerable(Of C)) As Object
        For Each t In types
            Dim identity = (t?.Something().First())?.ToArray()
        Next
        Return Nothing
    End Function
End Class]]>
</File>

            Await TestAsync(code, expected, compareTokens:=False)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsInlineTemporary)>
        <WorkItem(2593, "https://github.com/dotnet/roslyn/issues/2593")>
        Public Async Function TestConditionalAccessWithExtensionMethodInvocation_2() As Task
            Dim code =
<File><![CDATA[
Imports System.Runtime.CompilerServices

Module M
    <Extension()>
    Public Function Something(cust As C) As IEnumerable(Of String)
        Throw New NotImplementedException()
    End Function

    <Extension()>
    Public Function Something2(cust As C) As Func(Of C)
        Throw New NotImplementedException()
    End Function
End Module

Class C
    Private Function GetAssemblyIdentity(types As IEnumerable(Of C)) As Object
        For Each t In types
            Dim [|assembly|] = t?.Something2?()?.Something().First()
            Dim identity = (assembly)?.ToArray()
        Next
        Return Nothing
    End Function
End Class]]>
</File>

            Dim expected =
<File><![CDATA[
Imports System.Runtime.CompilerServices

Module M
    <Extension()>
    Public Function Something(cust As C) As IEnumerable(Of String)
        Throw New NotImplementedException()
    End Function

    <Extension()>
    Public Function Something2(cust As C) As Func(Of C)
        Throw New NotImplementedException()
    End Function
End Module

Class C
    Private Function GetAssemblyIdentity(types As IEnumerable(Of C)) As Object
        For Each t In types
            Dim identity = ((t?.Something2?()?.Something().First()))?.ToArray()
        Next
        Return Nothing
    End Function
End Class]]>
</File>

            Await TestAsync(code, expected, compareTokens:=False)
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.CodeActionsInlineTemporary)>
        Public Async Function TestXmlLiteral() As Task
            Dim code =
<File>
Class C
    Sub M(args As String())
        Dim [|x|] = &lt;xml&gt;Hello&lt;/xml&gt;
        Dim y = x.&lt;xmlelement&gt;
        Dim y1 = x?.&lt;xmlelement&gt;
    End Sub
End Class
</File>

            Dim expected =
<File>
Class C
    Sub M(args As String())
        Dim y = &lt;xml&gt;Hello&lt;/xml&gt;.&lt;xmlelement&gt;
        Dim y1 = &lt;xml&gt;Hello&lt;/xml&gt;?.&lt;xmlelement&gt;
    End Sub
End Class
</File>

            Await TestAsync(code, expected, compareTokens:=False)
        End Function

        <WorkItem(2671, "https://github.com/dotnet/roslyn/issues/2671")>
        <WpfFact(), Trait(Traits.Feature, Traits.Features.CodeActionsInlineTemporary)>
        Public Async Function TestReplaceReferencesInWithBlocks() As Task
            Dim code =
<MethodBody>
Dim [||]s As String = "test"
With s
    .ToLower()
End With
</MethodBody>

            Dim expected =
<MethodBody>
With "test"
    Call .ToLower()
End With
</MethodBody>
            ' Introduction of the Call keyword in this scenario is by design, see bug 529694.
            Await TestAsync(code, expected, compareTokens:=False)
        End Function

        <WorkItem(4583, "https://github.com/dotnet/roslyn/issues/4583")>
        <WpfFact(), Trait(Traits.Feature, Traits.Features.CodeActionsInlineTemporary)>
        Public Async Function TestDontParenthesizeInterpolatedStringWithNoInterpolation() As Task
            Dim code =
<MethodBody>
Dim [||]s1 = $"hello"
Dim s2 = AscW(s1)
</MethodBody>

            Dim expected =
<MethodBody>
Dim s2 = AscW($"hello")
</MethodBody>

            Await TestAsync(code, expected, compareTokens:=False)
        End Function

        <WorkItem(4583, "https://github.com/dotnet/roslyn/issues/4583")>
        <WpfFact(), Trait(Traits.Feature, Traits.Features.CodeActionsInlineTemporary)>
        Public Async Function TestDontParenthesizeInterpolatedStringWithInterpolation() As Task
            Dim code =
<MethodBody>
Dim x = 42
Dim [||]s1 = $"hello {x}"
Dim s2 = AscW(s1)
</MethodBody>

            Dim expected =
<MethodBody>
Dim x = 42
Dim s2 = AscW($"hello {x}")
</MethodBody>

            Await TestAsync(code, expected, compareTokens:=False)
        End Function

        <WorkItem(4583, "https://github.com/dotnet/roslyn/issues/4583")>
        <WpfFact(), Trait(Traits.Feature, Traits.Features.CodeActionsInlineTemporary)>
        Public Async Function TestInlineFormattableStringIntoCallSiteRequiringFormattableString() As Task
            Dim code = "
Imports System
" & FormattableStringType & "
Class C
    Sub M(s As FormattableString)
    End Sub

    Sub N(x As Integer, y As Integer)
        Dim [||]s As FormattableString = $""{x}, {y}""
        M(s)
    End Sub
End Class
"

            Dim expected = "
Imports System
" & FormattableStringType & "
Class C
    Sub M(s As FormattableString)
    End Sub

    Sub N(x As Integer, y As Integer)
        M($""{x}, {y}"")
    End Sub
End Class
"

            Await TestAsync(code, expected, compareTokens:=False)
        End Function

        <WorkItem(4624, "https://github.com/dotnet/roslyn/issues/4624")>
        <WpfFact(), Trait(Traits.Feature, Traits.Features.CodeActionsInlineTemporary)>
        Public Async Function TestInlineFormattableStringIntoCallSiteWithFormattableStringOverload() As Task
            Dim code = "
Imports System
" & FormattableStringType & "
Class C
    Sub M(s As String)
    End Sub

    Sub M(s As FormattableString)
    End Sub

    Sub N(x As Integer, y As Integer)
        Dim [||]s As FormattableString = $""{x}, {y}""
        M(s)
    End Sub
End Class
"

            Dim expected = "
Imports System
" & FormattableStringType & "
Class C
    Sub M(s As String)
    End Sub

    Sub M(s As FormattableString)
    End Sub

    Sub N(x As Integer, y As Integer)
        M(CType($""{x}, {y}"", FormattableString))
    End Sub
End Class
"

            Await TestAsync(code, expected, compareTokens:=False)
        End Function

    End Class
End Namespace
