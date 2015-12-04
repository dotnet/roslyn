' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Threading.Tasks
Imports Microsoft.CodeAnalysis.Text
Imports Roslyn.Test.Utilities

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.UnitTests.ExtractMethod
    Partial Public Class ExtractMethodTests
        Public Class SelectionValidator
            <WpfFact, Trait(Traits.Feature, Traits.Features.ExtractMethod)>
            Public Async Function TestSelectionTest1() As Task
                Dim code = <text>{|b:Imports System|}</text>
                Await TestSelectionAsync(code, expectedFail:=True)
            End Function

            <WpfFact, Trait(Traits.Feature, Traits.Features.ExtractMethod)>
            Public Async Function TestSelectionTest2() As Task
                Dim code = <text>{|b:Namespace A|}
End Namespace</text>
                Await TestSelectionAsync(code, expectedFail:=True)
            End Function

            <WpfFact, Trait(Traits.Feature, Traits.Features.ExtractMethod)>
            Public Async Function TestSelectionTest3() As Task
                Dim code = <text>Namespace {|b:A|}
End Namespace</text>
                Await TestSelectionAsync(code, expectedFail:=True)
            End Function

            <WpfFact, Trait(Traits.Feature, Traits.Features.ExtractMethod)>
            Public Async Function TestSelectionTest4() As Task
                Dim code = <text>{|b:Class|} A
End Class</text>
                Await TestSelectionAsync(code, expectedFail:=True)
            End Function

            <WpfFact, Trait(Traits.Feature, Traits.Features.ExtractMethod)>
            Public Async Function TestSelectionTest5() As Task
                Dim code = <text>Class {|b:A|}
End Class</text>
                Await TestSelectionAsync(code, expectedFail:=True)
            End Function

            <WpfFact, Trait(Traits.Feature, Traits.Features.ExtractMethod)>
            Public Async Function TestSelectionTest6() As Task
                Dim code = <text>Class A
    Implements {|b:IDisposable|}
End Class</text>
                Await TestSelectionAsync(code, expectedFail:=True)
            End Function

            <WpfFact, Trait(Traits.Feature, Traits.Features.ExtractMethod)>
            Public Async Function TestSelectionTest7() As Task
                Dim code = <text>Class A
    Inherits {|b:Object|}
End Class</text>
                Await TestSelectionAsync(code, expectedFail:=True)
            End Function

            <WpfFact, Trait(Traits.Feature, Traits.Features.ExtractMethod)>
            Public Async Function TestSelectionTest8() As Task
                Dim code = <text>Class A(Of {|b:T|})
End Class</text>
                Await TestSelectionAsync(code, expectedFail:=True)
            End Function

            <WpfFact, Trait(Traits.Feature, Traits.Features.ExtractMethod)>
            Public Async Function TestSelectionTest9() As Task
                Dim code = <text>Class A(Of T As {|b:IDisposable|})
End Class</text>
                Await TestSelectionAsync(code, expectedFail:=True)
            End Function

            <WpfFact, Trait(Traits.Feature, Traits.Features.ExtractMethod)>
            Public Async Function TestSelectionTest10() As Task
                Dim code = <text>Class A(Of T As {IComparable, {|b:IDisposable|}})
End Class</text>
                Await TestSelectionAsync(code, expectedFail:=True)
            End Function

            <WpfFact, Trait(Traits.Feature, Traits.Features.ExtractMethod)>
            Public Async Function TestSelectionTest11() As Task
                Dim code = <text>Class A
    Function Method() As {|b:A|}
    End Function
End Class</text>
                Await TestSelectionAsync(code, expectedFail:=True)
            End Function

            <WpfFact, Trait(Traits.Feature, Traits.Features.ExtractMethod)>
            Public Async Function TestSelectionTest12() As Task
                Dim code = <text>Class A
    Function Method(a As {|b:A|}) As A
    End Function
End Class</text>
                Await TestSelectionAsync(code, expectedFail:=True)
            End Function

            <WpfFact, Trait(Traits.Feature, Traits.Features.ExtractMethod)>
            Public Async Function TestSelectionTest13() As Task
                Dim code = <text>Class A
    Function Method({|b:a|} As A) As A
    End Function
End Class</text>
                Await TestSelectionAsync(code, expectedFail:=True)
            End Function

            <WpfFact, Trait(Traits.Feature, Traits.Features.ExtractMethod)>
            Public Async Function TestSelectionTest14() As Task
                Dim code = <text>Class A
    &lt;{|b:Foo()|}&gt;
    Function Method(a As A) As A
    End Function
End Class</text>
                Await TestSelectionAsync(code, expectedFail:=True)
            End Function

            <WpfFact, Trait(Traits.Feature, Traits.Features.ExtractMethod)>
            Public Async Function TestSelectionTest15() As Task
                Dim code = <text>Class A
    &lt;Foo({|b:A|}:=1)&gt;
    Function Method(a As A) As A
    End Function
End Class</text>
                Await TestSelectionAsync(code, expectedFail:=True)
            End Function

            <WpfFact, Trait(Traits.Feature, Traits.Features.ExtractMethod)>
            Public Async Function TestSelectionTest16() As Task
                Dim code = <text>Class A
    &lt;Foo(A:={|b:1|})&gt;
    Function Method(a As A) As A
    End Function
End Class</text>
                Await TestSelectionAsync(code, expectedFail:=True)
            End Function

            <WpfFact, Trait(Traits.Feature, Traits.Features.ExtractMethod)>
            Public Async Function TestSelectionTest17() As Task
                Dim code = <text>Class A
    Dim {|b:i|} as Integer = 1
End Class</text>
                Await TestSelectionAsync(code, expectedFail:=True)
            End Function

            <WpfFact, Trait(Traits.Feature, Traits.Features.ExtractMethod)>
            Public Async Function TestSelectionTest18() As Task
                Dim code = <text>Class A
    Dim i as {|b:Integer|} = 1
End Class</text>
                Await TestSelectionAsync(code, expectedFail:=True)
            End Function

            <WpfFact, Trait(Traits.Feature, Traits.Features.ExtractMethod)>
            Public Async Function TestSelectionTest19() As Task
                Dim code = <text>Class A
    Const i as Integer = {|b:1|}
End Class</text>
                Await TestSelectionAsync(code, expectedFail:=True)
            End Function

            <WpfFact, Trait(Traits.Feature, Traits.Features.ExtractMethod)>
            Public Async Function TestSelectionTest20() As Task
                Dim code = <text>Class A
    Const i as Integer = {|r:{|b:1 + |}2|}
End Class</text>
                Await TestSelectionAsync(code, expectedFail:=True)
            End Function

            <WpfFact, Trait(Traits.Feature, Traits.Features.ExtractMethod)>
            Public Async Function TestSelectionTest21() As Task
                Dim code = <text>Class A
    Const i as {|b:Integer = 1 + |}2
End Class</text>
                Await TestSelectionAsync(code, expectedFail:=True)
            End Function

            <WpfFact, Trait(Traits.Feature, Traits.Features.ExtractMethod)>
            Public Async Function TestSelectionTest22() As Task
                Dim code = <text>Class A
    Sub Method1()
        {|b:Dim i As Integer = 1
    End Sub

    Sub Method2()
        Dim b As Integer = 2|}
    End Sub
End Class</text>
                Await TestSelectionAsync(code, expectedFail:=True)
            End Function

            <WpfFact, Trait(Traits.Feature, Traits.Features.ExtractMethod)>
            Public Async Function TestSelectionTest23() As Task
                Dim code = <text>Class A
    Sub Method1()
        {|b:Dim i As Integer = 1
    End Sub|}
End Class</text>
                Await TestSelectionAsync(code, expectedFail:=True)
            End Function

            <WpfFact, Trait(Traits.Feature, Traits.Features.ExtractMethod)>
            Public Async Function TestSelectionTest24() As Task
                Dim code = <text>Class A
    Sub Method1()
#Region "A"
        {|b:Dim i As Integer = 1|}
#End Region
    End Sub
End Class</text>
                Await TestSelectionAsync(code)
            End Function

            <WpfFact, Trait(Traits.Feature, Traits.Features.ExtractMethod)>
            Public Async Function TestSelectionTest25() As Task
                Dim code = <text>Class A
    Sub Method1()
{|b:#Region "A"
        Dim i As Integer = 1|}
#End Region
    End Sub
End Class</text>
                Await TestSelectionAsync(code, expectedFail:=True)
            End Function

            <WpfFact, Trait(Traits.Feature, Traits.Features.ExtractMethod)>
            Public Async Function TestSelectionTest26() As Task
                Dim code = <text>Class A
    Sub Method1()
#Region "A"
        {|b:Dim i As Integer = 1
#End Region|}
    End Sub
End Class</text>
                Await TestSelectionAsync(code, expectedFail:=True)
            End Function

            <WpfFact, Trait(Traits.Feature, Traits.Features.ExtractMethod)>
            Public Async Function TestSelectionTest27() As Task
                Dim code = <text>Class A
    Sub Method1()
#Region "A"
{|b:#End Region
        Dim i As Integer = 1|}
    End Sub
End Class</text>
                Await TestSelectionAsync(code, expectedFail:=True)
            End Function

            <WpfFact, Trait(Traits.Feature, Traits.Features.ExtractMethod)>
            Public Async Function TestSelectionTest28() As Task
                Dim code = <text>Class A
    Sub Method1()
#If True Then
        {|b:Dim i As Integer = 1
#End if|}
    End Sub
End Class</text>
                Await TestSelectionAsync(code, expectedFail:=True)
            End Function

            <WpfFact, Trait(Traits.Feature, Traits.Features.ExtractMethod)>
            Public Async Function TestSelectionTest29() As Task
                Dim code = <text>Class A
    Sub Method1()
{|b:#If True Then
        Dim i As Integer = 1|}
#End if
    End Sub
End Class</text>
                Await TestSelectionAsync(code, expectedFail:=True)
            End Function

            <WpfFact, Trait(Traits.Feature, Traits.Features.ExtractMethod)>
            Public Async Function TestSelectionTest30() As Task
                Dim code = <text>Class A
    Sub Method1()
#If True Then
{|b:#End If
        Dim i As Integer = 1|}
    End Sub
End Class</text>
                Await TestSelectionAsync(code, expectedFail:=True)
            End Function

            <WpfFact, Trait(Traits.Feature, Traits.Features.ExtractMethod)>
            Public Async Function TestSelectionTest31() As Task
                Dim code = <text>Class A
    Sub Method1()
#If True Then
{|b:#Else
        Dim i As Integer = 1|}
#End If
    End Sub
End Class</text>
                Await TestSelectionAsync(code, expectedFail:=True)
            End Function

            <WpfFact, Trait(Traits.Feature, Traits.Features.ExtractMethod)>
            Public Async Function TestSelectionTest32() As Task
                Dim code = <text>Class A
    Sub Method1()
#If False Then
{|b:#ElseIf True Then
        Dim i As Integer = 1|}
#End If
    End Sub
End Class</text>
                Await TestSelectionAsync(code, expectedFail:=True)
            End Function

            <WpfFact, Trait(Traits.Feature, Traits.Features.ExtractMethod)>
            Public Async Function TestSelectionTest33() As Task
                Dim code = <text>Class A
    Sub Method1()
{|b:#If True Then
        Dim i As Integer = 1
#End if|}
    End Sub
End Class</text>
                Await TestSelectionAsync(code)
            End Function

            <WpfFact, Trait(Traits.Feature, Traits.Features.ExtractMethod)>
            Public Async Function TestSelectionTest34() As Task
                Dim code = <text>Class A
    Sub Method1()
{|b:#Region "A"
        Dim i As Integer = 1
#End Region|}
    End Sub
End Class</text>
                Await TestSelectionAsync(code)
            End Function

            <WpfFact, Trait(Traits.Feature, Traits.Features.ExtractMethod)>
            Public Async Function TestSelectionTest35() As Task
                Dim code = <text>Class A
    Sub Method()
        {|b:' test|}
    End Sub
End Class</text>
                Await TestSelectionAsync(code, expectedFail:=True)
            End Function

            <WpfFact, Trait(Traits.Feature, Traits.Features.ExtractMethod)>
            Public Async Function TestSelectionTest36() As Task
                Dim code = <text>Class A
    Function Method() As IEnumerable(Of Integer)
        {|r:{|b:Yield Return 1;|}|}
    End Function
End Class</text>
                Await TestSelectionAsync(code, expectedFail:=True)
            End Function

            <WpfFact, Trait(Traits.Feature, Traits.Features.ExtractMethod)>
            Public Async Function TestSelectionTest37() As Task
                Dim code = <text>Class A
    Sub Method()
        Try
        Catch
            {|b:Throw|}
        End Try
    End Sub
End Class</text>
                Await TestSelectionAsync(code, expectedFail:=True)
            End Function

            <WpfFact, Trait(Traits.Feature, Traits.Features.ExtractMethod)>
            Public Async Function TestSelectionTest38() As Task
                Dim code = <text>Class A
    Sub Method()
        Try
        Catch
            {|b:Throw new Exception()|}
        End Try
    End Sub
End Class</text>
                Await TestSelectionAsync(code)
            End Function

            <WorkItem(540082)>
            <WpfFact, Trait(Traits.Feature, Traits.Features.ExtractMethod)>
            Public Async Function TestSelectionTest39() As Task
                Dim code = <text>Class A
    Sub Method()
        {|r:{|b:System|}.Console.WriteLine(1)|}
    End Sub
End Class</text>
                Await TestSelectionAsync(code)
            End Function

            <WorkItem(540082)>
            <WpfFact, Trait(Traits.Feature, Traits.Features.ExtractMethod)>
            Public Async Function TestSelectionTest40() As Task
                Dim code = <text>Class A
    Sub Method()
        {|r:{|b:System.Console|}.WriteLine(1)|}
    End Sub
End Class</text>
                Await TestSelectionAsync(code)
            End Function

            <WorkItem(540082)>
            <WpfFact, Trait(Traits.Feature, Traits.Features.ExtractMethod)>
            Public Async Function TestSelectionTest41() As Task
                Dim code = <text>Class A
    Sub Method()
        {|r:{|b:System.Console.WriteLine|}(1)|}
    End Sub
End Class</text>
                Await TestSelectionAsync(code)
            End Function

            <WorkItem(540082)>
            <WpfFact, Trait(Traits.Feature, Traits.Features.ExtractMethod)>
            Public Async Function TestSelectionTest42() As Task
                Dim code = <text>Class A
    Sub Method()
{|r:        System.{|b:Console.WriteLine|}(1)|}
    End Sub
End Class</text>
                Await TestSelectionAsync(code)
            End Function

            <WorkItem(540082)>
            <WpfFact, Trait(Traits.Feature, Traits.Features.ExtractMethod)>
            Public Async Function TestSelectionTest43() As Task
                Dim code = <text>Class A
    Sub Method()
{|r:        System.{|b:Console|}.WriteLine(1)|}
    End Sub
End Class</text>
                Await TestSelectionAsync(code)
            End Function

            <WorkItem(540082)>
            <WpfFact, Trait(Traits.Feature, Traits.Features.ExtractMethod)>
            Public Async Function TestSelectionTest44() As Task
                Dim code = <text>Class A
    Sub Method()
{|r:        System.Console.{|b:WriteLine|}(1)|}
    End Sub
End Class</text>
                Await TestSelectionAsync(code)
            End Function

            <WorkItem(539397)>
            <WpfFact, Trait(Traits.Feature, Traits.Features.ExtractMethod)>
            Public Async Function TestSelectionTest45() As Task
                Dim code = <text>Imports System
Module Module1
    Sub Main(args As String())
{|r:        Call {|b:printToConsoleWindow|}|}
    End Sub

    Sub printToConsoleWindow()
        Console.WriteLine("Hi")
    End Sub
End Module
</text>
                Await TestSelectionAsync(code)
            End Function

            <WorkItem(539242)>
            <WpfFact, Trait(Traits.Feature, Traits.Features.ExtractMethod)>
            Public Async Function TestSelectionTest46() As Task
                Dim code = <text>Module Program
    Sub Main(args As String())
        Dim startingScores1(,) As Short = {|r:New Short(1, 2) {{|b:{10, 10, 10}|}, {10, 10, 10}}|}
       Dim ticTacToe = {{0, 0, 0}, {0, 0, 0}, {0, 0, 0}}
    End Sub
End Module</text>
                Await TestSelectionAsync(code)
            End Function

            <WorkItem(539242)>
            <WpfFact, Trait(Traits.Feature, Traits.Features.ExtractMethod)>
            Public Async Function TestSelectionTest47() As Task
                Dim code = <text>Module Program
    Sub Main(args As String())
        Dim startingScores1(,) As Short = New Short(1, 2) {{10, 10, 10}, {10, 10, 10}}
        Dim ticTacToe = {|r:{{|b:{0, 0, 0}|}, {0, 0, 0}, {0, 0, 0}}|}
    End Sub
End Module</text>
                Await TestSelectionAsync(code)
            End Function

            <WorkItem(540375)>
            <WpfFact, Trait(Traits.Feature, Traits.Features.ExtractMethod)>
            Public Async Function TestSelectIfThatAlwaysReturns() As Task
                Dim code = <text>Module Program
    Sub Main(args As String())
        {|b:If True Then
            Return
        End If|}
        Console.Write(5)
    End Sub
End Module</text>
                Await TestSelectionAsync(code)
            End Function

            <WorkItem(540375)>
            <WpfFact, Trait(Traits.Feature, Traits.Features.ExtractMethod)>
            Public Async Function TestSelectConstIfWithReturn() As Task
                Dim code = <text>Class A
    Public Sub Method1()
        Const b As Boolean = True
        {|b:If b Then
            Return
        End If|}
        Console.WriteLine()
    End Sub
End Class</text>
                Await TestSelectionAsync(code)
            End Function

            <WpfFact, Trait(Traits.Feature, Traits.Features.ExtractMethod)>
            Public Async Function TestSelectReturnButNotAllCodePathsContainAReturn() As Task
                Dim code = <text>Imports System
Class A
    Public Sub Method1(b1 As Boolean, b2 As Boolean)
        If b1 Then
            {|b:If b2 Then
                Return
            End If
            Console.WriteLine()|}
        End If
        Console.WriteLine()
    End Sub
End Class</text>
                Await TestSelectionAsync(code, expectedFail:=True)
            End Function

            <WpfFact, Trait(Traits.Feature, Traits.Features.ExtractMethod)>
            Public Async Function TestSelectIfBranchWhereNotAllPathsReturn() As Task
                Dim code = <text>Imports System

Class A
    Private Function Method8(i As Integer) As Integer
        {|b:If i > 100 Then
            Return System.Math.Max(System.Threading.Interlocked.Increment(i), i - 1)
        ElseIf i > 90 Then
            Return System.Math.Max(System.Threading.Interlocked.Decrement(i), i + 1)
        Else
            i = 1
        End If|}
        Return i
    End Function
End Class</text>
                Await TestSelectionAsync(code, expectedFail:=True)
            End Function

            <WpfFact, Trait(Traits.Feature, Traits.Features.ExtractMethod)>
            Public Async Function TestSelectLValueOfPlusEqualsOperator() As Task
                Dim code = <text>Imports System

Class A
    Private Function Method() As Integer
        Dim i As Integer = 0
        {|r:{|b:i|} += 1|}
        Return i
    End Function
End Class</text>
                Await TestSelectionAsync(code)
            End Function

            <WorkItem(10071, "DevDiv_Projects/Roslyn")>
            <WpfFact, Trait(Traits.Feature, Traits.Features.ExtractMethod)>
            Public Async Function TestSelectRValueOfPlusEqualsOperator() As Task
                Dim code = <text>Imports System

Class A
    Private Function Method() As Integer
        Dim i As Integer = 0
        i += {|b:1|}
        Return i
    End Function
End Class</text>
                Await TestSelectionAsync(code)
            End Function

            <WpfFact, Trait(Traits.Feature, Traits.Features.ExtractMethod)>
            Public Async Function TestSelectAddressOfOperator() As Task
                Dim code = <text>Delegate Sub SimpleDelegate()
Module Test
    Sub F()
        System.Console.WriteLine("Test.F")
    End Sub

    Sub Main()
{|r:        Dim d As SimpleDelegate = {|b:AddressOf|} F|}
        d()
    End Sub
End Module</text>
                Await TestSelectionAsync(code)
            End Function

            <WpfFact, Trait(Traits.Feature, Traits.Features.ExtractMethod)>
            Public Async Function TestSelectOperandOfAddressOfOperator() As Task
                Dim code = <text>Delegate Sub SimpleDelegate()
Module Test
    Sub F()
        System.Console.WriteLine("Test.F")
    End Sub

    Sub Main()
        Dim d As SimpleDelegate = {|r:AddressOf {|b:F|}|}
        d()
    End Sub
End Module</text>
                Await TestSelectionAsync(code)
            End Function

            <WpfFact, Trait(Traits.Feature, Traits.Features.ExtractMethod)>
            Public Async Function TestSelectInvalidSubexpressionToExpand() As Task
                Dim code = <text>Class A
    Public Sub method(a As Integer, b As Integer, c As Integer)
{|r:        Dim d = a + {|b:b + c|}|}
    End Sub
End Class</text>
                Await TestSelectionAsync(code)
            End Function

            <WpfFact, Trait(Traits.Feature, Traits.Features.ExtractMethod)>
            Public Async Function TestSelectValidSubexpressionAndHenceDontExpand() As Task
                Dim code = <text>Class A
    Public Sub method(a As Integer, b As Integer, c As Integer)
        Dim d = {|b:a + b|} + c
    End Sub
End Class</text>
                Await TestSelectionAsync(code)
            End Function

            <WpfFact, Trait(Traits.Feature, Traits.Features.ExtractMethod)>
            Public Async Function TestSelectLHSOfMinusEqualsOperator() As Task
                Dim code = <text>Class A
    Public Sub method(a As Integer, b As Integer)
        {|r:{|b:a|} -= b|}
    End Sub
End Class</text>
                Await TestSelectionAsync(code)
            End Function

            <WorkItem(540463)>
            <WpfFact, Trait(Traits.Feature, Traits.Features.ExtractMethod)>
            Public Async Function TestSelectInnerBlockPartially() As Task
                Dim code = <text>Imports System.Collections

Class A
    Private Sub method()
        Dim ar As ArrayList = Nothing
        For Each var As Object In ar
            {|r:{|b:System.Console.WriteLine()
            For Each var2 As Object In ar
                System.Console.WriteLine()|}
            Next|}
        Next
    End Sub
End Class</text>
                Await TestSelectionAsync(code)
            End Function

            <WpfFact, Trait(Traits.Feature, Traits.Features.ExtractMethod)>
            Public Async Function TestSelectInnerBlockPartially2() As Task
                Dim code = <text>Imports System
Imports System.Collections

Class A
    Private Sub method()
        While True
            Dim i As Integer = 0
{|r:            If i = 0 Then
                Console.WriteLine(){|b:
            End If
            Console.WriteLine()|}|}
        End While
    End Sub
End Class</text>
                Await TestSelectionAsync(code)
            End Function

            <WorkItem(540463)>
            <WpfFact, Trait(Traits.Feature, Traits.Features.ExtractMethod)>
            Public Async Function TestSelectAcrossBlocks1() As Task
                Dim code = <text>Imports System.Collections

Class A
    Private Sub method()
        If True Then
{|r:            For i As Integer = 0 To 99
                {|b:System.Console.WriteLine()
            Next
            System.Console.WriteLine()|}|}
        End If
    End Sub
End Class</text>
                Await TestSelectionAsync(code)
            End Function

            <WpfFact, Trait(Traits.Feature, Traits.Features.ExtractMethod)>
            Public Async Function TestSelectMethodParameters() As Task
                Dim code = <text>Imports System.Collections

Class A
    Private Sub method()
        Dim x1 As Double = 10
        Dim y1 As Double = 20
        Dim z1 As Double = 30
        Dim ret As Double = {|r:sum({|b:x1, y1, z1|})|}
    End Sub
    Private Function sum(ByRef x As Double, y As Double, z As Double) As Double
        x = x + 1
        Return x + y + z
    End Function
End Class</text>
                Await TestSelectionAsync(code)
            End Function

            <WpfFact, Trait(Traits.Feature, Traits.Features.ExtractMethod)>
            Public Async Function TestSelectChainedInvocations1() As Task
                Dim code = <text>Imports System.Collections

Class Test
    Private Class B
        Public Function c() As Integer
            Return 100
        End Function
    End Class
    Private Class A
        Public b As New B()
    End Class

    Private Sub method()
        Dim a As New A()
        {|b:a.b|}.c()
    End Sub
End Class</text>
                Await TestSelectionAsync(code)
            End Function

            <WpfFact, Trait(Traits.Feature, Traits.Features.ExtractMethod)>
            Public Async Function TestSelectChainedInvocations2() As Task
                Dim code = <text>Imports System.Collections

Class Test
    Private Class B
        Public Function c() As Integer
            Return 100
        End Function
    End Class
    Private Class A
        Public b As New B()
    End Class

    Private Sub method()
        Dim a As New A()
{|r:        a.{|b:b.c()|}|}
    End Sub
End Class</text>
                Await TestSelectionAsync(code)
            End Function

            <WorkItem(540471)>
            <WpfFact, Trait(Traits.Feature, Traits.Features.ExtractMethod)>
            Public Async Function TestBugFix6737() As Task
                Dim code = <text>Module Program
    Sub Main(args As String())
        Dim x As Integer
        x = 20
{|b:Foo|}:
        x = 10
    End Sub
End Module</text>
                Await TestSelectionAsync(code, expectedFail:=True)
            End Function

            <WorkItem(540471)>
            <WpfFact, Trait(Traits.Feature, Traits.Features.ExtractMethod)>
            Public Async Function TestSelectLabel() As Task
                Dim code = <text>Module Program
    Sub Main(args As String())
        Dim x As Integer
        x = 20
        GoTo Foo
        x = 24
{|b:Foo:|}
        x = 10
    End Sub</text>
                Await TestSelectionAsync(code, expectedFail:=True)
            End Function

            <WpfFact, Trait(Traits.Feature, Traits.Features.ExtractMethod)>
            Public Async Function TestSelectGotoStatement() As Task
                Dim code = <text>Class Program
    Function F(x As Integer) As Integer
{|r:        {|b:If x >= 0 Then
            GoTo x
        End If|}|}
        x = -x
x:
        Return x
    End Function
End Class</text>
                Await TestSelectionAsync(code, expectedFail:=True)
            End Function

            <WpfFact, Trait(Traits.Feature, Traits.Features.ExtractMethod)>
            Public Async Function TestSelectGotoStatement1() As Task
                Dim code = <text>Class Program
    Function F(x As Integer) As Integer
{|r:        If x >= 0 Then
            {|b:GoTo x|}
        End If|}
        x = -x
x:
        Return x
    End Function
End Class</text>
                Await TestSelectionAsync(code, expectedFail:=True)
            End Function

            <WpfFact, Trait(Traits.Feature, Traits.Features.ExtractMethod)>
            Public Async Function TestSelectGotoWithLabel() As Task
                Dim code = <text>Class Program
    Function F(x As Integer) As Integer
        {|b:If x >= 0 Then
            GoTo x
        End If
        x = -x
x:
        Return x|}
    End Function
End Class</text>
                Await TestSelectionAsync(code)
            End Function

            <WorkItem(540471)>
            <WpfFact, Trait(Traits.Feature, Traits.Features.ExtractMethod)>
            Public Async Function TestSelectGotoWithLabel1() As Task
                Dim code = <text>Class Program
    Function F(x As Integer) As Integer
        {|b:If x >= 0 Then
            GoTo x
        End If
        x = -x
x:|}
        Return x
    End Function
End Class</text>
                Await TestSelectionAsync(code)
            End Function

            <WorkItem(540497)>
            <WpfFact, Trait(Traits.Feature, Traits.Features.ExtractMethod)>
            Public Async Function TestSelectAutoPropInitializer() As Task
                Dim code = <text>Class B
    Property ID() As Integer = {|b:1|}
End Class</text>
                Await TestSelectionAsync(code)
            End Function

            <WpfFact, Trait(Traits.Feature, Traits.Features.ExtractMethod)>
            Public Async Function TestSelectCollectionInitializer() As Task
                Dim code = <text>Class B
    Dim list = New List(Of String) From {{|b:"abc"|}, "def", "ghi"}
End Class</text>
                Await TestSelectionAsync(code)
            End Function

            <WpfFact, Trait(Traits.Feature, Traits.Features.ExtractMethod)>
            Public Async Function TestSelectCollectionInitializer1() As Task
                Dim code = <text>Class B
    Dim list = New List(Of String) From {|r:{{|b:"abc"|}, "def", "ghi"}|}
End Class</text>
                Await TestSelectionAsync(code)
            End Function

            <WorkItem(6626, "DevDiv_Projects/Roslyn")>
            <WpfFact, Trait(Traits.Feature, Traits.Features.ExtractMethod)>
            Public Async Function TestSelectSectionBeforeUnreachableCode() As Task
                Dim code = <text>Module Program
    Sub Main(args As String())
        {|b:Dim x As Integer
        x = 1|}
        Return
        Dim y As Integer = x
    End Sub
End Module</text>
                Await TestSelectionAsync(code)
            End Function

            <WorkItem(540200)>
            <WpfFact, Trait(Traits.Feature, Traits.Features.ExtractMethod)>
            Public Async Function TestBugFix6376() As Task
                Dim code = <text>Imports System
Imports System.Collections.Generic
Imports System.Linq

Module Program
    Sub Main(args As String())
        : [|Main|](Nothing)
    End Sub
End Module</text>
                Dim expected = <text>Imports System
Imports System.Collections.Generic
Imports System.Linq

Module Program
    Sub Main(args As String())
        : NewMethod()
    End Sub

    Private Sub NewMethod()
        Main(Nothing)
    End Sub
End Module</text>
                Await TestExtractMethodAsync(code, expected)
            End Function

            <WorkItem(540465)>
            <WpfFact, Trait(Traits.Feature, Traits.Features.ExtractMethod)>
            Public Async Function TestBugFix6731() As Task
                Dim code = <text>Imports System
Imports System.Collections
 
Class A
    Private Sub method()
        While True
            Dim i As Integer = 0
            If {|b:i|} = 0 Then
                Console.WriteLine()
            End If
            Console.WriteLine()
        End While
    End Sub
End Class</text>
                Await TestSelectionAsync(code)
            End Function

            <WorkItem(540481)>
            <WpfFact, Trait(Traits.Feature, Traits.Features.ExtractMethod)>
            Public Async Function TestBugFix6750() As Task
                Dim code = <text>Imports System
Imports System.Collections

Class A
    Private Sub method()
        Dim a As Integer() = new Integer({|b:1|}) { }
    End Sub
End Class</text>
                Await TestSelectionAsync(code)
            End Function

            <WorkItem(540481)>
            <WpfFact, Trait(Traits.Feature, Traits.Features.ExtractMethod)>
            Public Async Function TestBugFix6750_1() As Task
                Dim code = <text>Imports System
Imports System.Collections

Class A
    Private Sub method()
        Dim a As Integer() = {|r:new Integer({|b:1|}) { 1, 2 }|}
    End Sub
End Class</text>
                Await TestSelectionAsync(code)
            End Function

            <WorkItem(10071, "DevDiv_Projects/Roslyn")>
            <WorkItem(544602)>
            <WpfFact, Trait(Traits.Feature, Traits.Features.ExtractMethod)>
            Public Async Function TestDontCrash() As Task
                Await IterateAllAsync(TestResource.AllInOneVisualBasicCode)
            End Function

            <WorkItem(541091)>
            <WpfFact, Trait(Traits.Feature, Traits.Features.ExtractMethod)>
            Public Async Function TestBugFix7660_1() As Task
                Dim code = <text>Class Program
End Class

Class SomeOtherClass
    Sub M()
        Dim p As New [|Program|]
    End Sub
End Class</text>

                Dim expected = <text>Class Program
End Class

Class SomeOtherClass
    Sub M()
        NewMethod()
    End Sub

    Private Shared Sub NewMethod()
        Dim p As New Program
    End Sub
End Class</text>


                Await TestExtractMethodAsync(code, expected)
            End Function

            <WorkItem(541091)>
            <WpfFact, Trait(Traits.Feature, Traits.Features.ExtractMethod)>
            Public Async Function TestBugFix7660_2() As Task
                Dim code = <text>Class Program
End Class

Class SomeOtherClass
    Sub M()
        Dim p As [|New Program|]
    End Sub
End Class</text>

                Dim expected = <text>Class Program
End Class

Class SomeOtherClass
    Sub M()
        NewMethod()
    End Sub

    Private Shared Sub NewMethod()
        Dim p As New Program
    End Sub
End Class</text>

                Await TestExtractMethodAsync(code, expected)
            End Function

            <WorkItem(541091)>
            <WpfFact, Trait(Traits.Feature, Traits.Features.ExtractMethod)>
            Public Async Function TestBugFix7660_3() As Task
                Dim code = <text>Class Program
End Class

Class SomeOtherClass
    Sub M()
        Dim p As New [|Program|]
        Console.WriteLine(p)
    End Sub
End Class</text>

                Dim expected = <text>Class Program
End Class

Class SomeOtherClass
    Sub M()
        Dim p As Program = NewMethod()
        Console.WriteLine(p)
    End Sub

    Private Shared Function NewMethod() As Program
        Dim p As New Program
        Return p
    End Function
End Class</text>

                Await TestExtractMethodAsync(code, expected)
            End Function

            <WorkItem(541620)>
            <WpfFact, Trait(Traits.Feature, Traits.Features.ExtractMethod)>
            Public Async Function TestCatchVariable() As Task
                Dim code = <text>Class SomeOtherClass
    Sub M()
{|r:        Try
        Catch {|b:ex|} As Exception
        End Try|}
    End Sub
End Class</text>

                Await TestSelectionAsync(code)
            End Function

            <WorkItem(541695)>
            <WpfFact, Trait(Traits.Feature, Traits.Features.ExtractMethod)>
            Public Async Function TestEmptySelectionWithMissingToken() As Task
                Dim code = <text>Module Program
    Sub Main(args As String())
 
        End{|b:
        |}
End Module
</text>

                Await TestSelectionAsync(code, expectedFail:=True)
            End Function

            <WorkItem(541411)>
            <WpfFact, Trait(Traits.Feature, Traits.Features.ExtractMethod)>
            Public Async Function TestControlVariableInForStatement() As Task
                Dim code = <text>Module Program
    Sub Main(ByVal args() As String)
        Dim i As Integer
{|r:        For {|b:i|} = 0 To 2
            System.Console.WriteLine("In the For Loop")
        Next i|}
  End Sub
End Module
</text>

                Await TestSelectionAsync(code)
            End Function

            <WorkItem(541411)>
            <WpfFact, Trait(Traits.Feature, Traits.Features.ExtractMethod)>
            Public Async Function TestControlVariableInForEachStatement() As Task
                Dim code = <text>Module Program
    Sub Main(args As String())
        Dim i As Integer
{|r:        For Each {|b:i|} In {1, 2}
        Next i|}
    End Sub
End Module
</text>
                Await TestSelectionAsync(code)
            End Function

            <WorkItem(541416)>
            <WpfFact, Trait(Traits.Feature, Traits.Features.ExtractMethod)>
            Public Async Function TestControlVariablesInNextStatement() As Task
                Dim code = <text>Module Program
    Sub Main(ByVal args() As String)
        Dim i As Integer
{|r:        For i = 0 To 2
            System.Console.WriteLine("In the For Loop")
        Next {|b:i|}|}
    End Sub
End Module
</text>
                Await TestSelectionAsync(code)
            End Function

            <WorkItem(528654)>
            <WpfFact, Trait(Traits.Feature, Traits.Features.ExtractMethod)>
            Public Async Function TestReDimSelectionValidator() As Task
                Dim code = <text>Module M
    Sub Main()
        Dim x(2)
{|r:        ReDim {|b:x(3)|}|}
    End Sub
End Module</text>
                Await TestSelectionAsync(code)
            End Function

            <WorkItem(542248)>
            <WpfFact, Trait(Traits.Feature, Traits.Features.ExtractMethod)>
            Public Async Function TestInvalidCode_NoOuterType() As Task
                Dim code = <text>Imports System
Imports System.Collections.Generic
Imports System.Linq
&gt;Attribute(Attribute(Attribute(
Module Program
Sub Main(args As String())
    {|b:For Each (var I In foo)|}

End Sub
End Module
</text>
                Await TestSelectionAsync(code, expectedFail:=True)
            End Function

            <WorkItem(542274)>
            <WpfFact, Trait(Traits.Feature, Traits.Features.ExtractMethod)>
            Public Async Function TestObjectMemberInitializer1() As Task
                Dim code = <text>Imports System
Imports System.Collections.Generic
Imports System.Linq
 
Module Program
    Sub Main(args As String())
{|r:        Dim ao = New With {.a = 1, .b = {|b:1 +.a|}}|}
    End Sub
End Module</text>

                Await TestSelectionAsync(code)
            End Function

            <WorkItem(542274)>
            <WpfFact, Trait(Traits.Feature, Traits.Features.ExtractMethod)>
            Public Async Function TestObjectMemberInitializer2() As Task
                Dim code = <text>Imports System
Imports System.Collections.Generic
Imports System.Linq
 
Module Program
    Sub Main(args As String())
        Dim ao = New With {.a = 1, .b = {|b:1|} +.a}
    End Sub
End Module</text>

                Await TestSelectionAsync(code)
            End Function

            <WorkItem(542274)>
            <WpfFact, Trait(Traits.Feature, Traits.Features.ExtractMethod)>
            Public Async Function TestObjectMemberInitializer3() As Task
                Dim code = <text>Imports System
Imports System.Collections.Generic
Imports System.Linq
 
Module Program
    Sub Main(args As String())
{|r:        Dim ao = New With {.a = 1, .b = 1 + {|b:.a|}}|}
    End Sub
End Module</text>

                Await TestSelectionAsync(code)
            End Function

            <WorkItem(542274)>
            <WpfFact, Trait(Traits.Feature, Traits.Features.ExtractMethod)>
            Public Async Function TestObjectMemberInitializer4() As Task
                Dim code = <text>Imports System
Imports System.Collections.Generic
Imports System.Linq
 
Module Program
    Sub Main(args As String())
{|r:        Dim ao = New With {.a = 1, {|b:.x|} = 1 +.a}|}
    End Sub
End Module</text>

                Await TestSelectionAsync(code)
            End Function

            <WorkItem(542274)>
            <WpfFact, Trait(Traits.Feature, Traits.Features.ExtractMethod)>
            Public Async Function TestObjectMemberInitializer5() As Task
                Dim code = <text>Imports System
Imports System.Collections.Generic
Imports System.Linq
 
Module Program
    Sub Main(args As String())
        Dim ao = New With {.a = 1, .x = 1 +.a, .h = {|b:H()|}}
    End Sub

    Function H() As Integer
        Return 1
    End Function
End Module</text>

                Await TestSelectionAsync(code)
            End Function

            <WorkItem(542274)>
            <WpfFact, Trait(Traits.Feature, Traits.Features.ExtractMethod)>
            Public Async Function TestObjectMemberInitializer6() As Task
                Dim code = <text>Imports System
Imports System.Collections.Generic
Imports System.Linq
 
Module Program
    Sub Main(args As String())
{|r:        Dim ao = New With {.a = 1, .x = 1 +.a, {|b:H()|}}|}
    End Sub

    Function H() As Integer
        Return 1
    End Function
End Module</text>

                Await TestSelectionAsync(code)
            End Function

            <WpfFact, Trait(Traits.Feature, Traits.Features.ExtractMethod)>
            Public Async Function TestMethodCallInvalidSelection() As Task
                Dim code = <text>Imports System.Threading
Module Program
    Sub Main()
        GetInitialSelection{|b:Info(Canc|}ellationToken.None)
    End Sub
    Private Sub GetInitialSelectionInfo(cancellationToken As CancellationToken)
    End Sub
End Module</text>
                Await TestSelectionAsync(code, expectedFail:=True)
            End Function

            <WpfFact, Trait(Traits.Feature, Traits.Features.ExtractMethod)>
            Public Async Function TestMultiLineLambda() As Task
                Dim code = <text>Imports System
Imports System.Collections.Generic
Imports System.Linq

Module Program
    Sub Main(args As String())
        Dim f As Func(Of Boolean, Integer) = {|r:{|b:Function(f1 As Boolean) As Integer
                                                 Return 1
                                             |}End Function|}
    End Sub
End Module</text>
                Await TestSelectionAsync(code)
            End Function

            <WpfFact, Trait(Traits.Feature, Traits.Features.ExtractMethod)>
            Public Async Function TestNullableTypeName() As Task
                Dim code = <text>
Module Program
    Sub Main(args As String())
{|r:        Dim f As {|b:DateTime|}?|}
    End Sub
End Module</text>
                Await TestSelectionAsync(code)
            End Function

            <WpfFact, Trait(Traits.Feature, Traits.Features.ExtractMethod)>
            Public Async Function TestPredefinedTypeInsideGetType() As Task
                Dim code = <text>
Class C
    Sub S()
        Dim f = {|r:GetType({|b:Integer|})|}
    End Sub
End Class</text>
                Await TestSelectionAsync(code)
            End Function

            <WpfFact, Trait(Traits.Feature, Traits.Features.ExtractMethod)>
            Public Async Function TestIdentifierNameInsideGetType() As Task
                Dim code = <text>
Class C
    Sub S()
        Dim f = {|r:GetType({|b:C|})|}
    End Sub
End Class</text>
                Await TestSelectionAsync(code)
            End Function

            <WpfFact, Trait(Traits.Feature, Traits.Features.ExtractMethod)>
            Public Async Function TestQualifiedNameInsideGetType() As Task
                Dim code = <text>
Namespace N
    Class C
        Sub S()
            Dim f = {|r:GetType(N.{|b:C|})|}
        End Sub
    End Class
End Namespace</text>
                Await TestSelectionAsync(code)
            End Function

            <WpfFact, Trait(Traits.Feature, Traits.Features.ExtractMethod)>
            Public Async Function TestQualifiedNameInsideTypeOfIs() As Task
                Dim code = <text>
Namespace N
    Class C
        Sub S()
            Dim f = {|r:TypeOf foo Is N.{|b:C|}|}
        End Sub
    End Class
End Namespace</text>
                Await TestSelectionAsync(code)
            End Function


            <WpfFact, Trait(Traits.Feature, Traits.Features.ExtractMethod)>
            Public Async Function TestQualifiedNameInsideArrayCreationExpression() As Task
                Dim code = <text>
Namespace N
    Class C
        Sub S()
            Dim f = {|r:New N.{|b:C|}() {}|}
        End Sub
    End Class
End Namespace</text>
                Await TestSelectionAsync(code)
            End Function

            <WpfFact, Trait(Traits.Feature, Traits.Features.ExtractMethod)>
            Public Async Function TestQualifiedNameInsideCastExpression() As Task
                Dim code = <text>
Namespace N
    Class C
        Sub S()
            Dim f = {|r:CType(foo, N.{|b:C|})|}
        End Sub
    End Class
End Namespace</text>
                Await TestSelectionAsync(code)
            End Function

            <WpfFact, Trait(Traits.Feature, Traits.Features.ExtractMethod)>
            Public Async Function TestQualifiedNameInsideArrayType() As Task
                Dim code = <text>
Namespace N
    Class C
        Sub S()
{|r:            Dim f As N.{|b:C|}()|}
        End Sub
    End Class
End Namespace</text>
                Await TestSelectionAsync(code)
            End Function

            <WpfFact, Trait(Traits.Feature, Traits.Features.ExtractMethod)>
            Public Async Function TestQualifiedNameInsideAsClause() As Task
                Dim code = <text>
Namespace N
    Class C
        Sub S()
{|r:            Dim f As N.{|b:C|}|}
        End Sub
    End Class
End Namespace</text>
                Await TestSelectionAsync(code)
            End Function

            <WpfFact, Trait(Traits.Feature, Traits.Features.ExtractMethod)>
            <WorkItem(542800)>
            Public Async Function TestXmlNode() As Task
                Dim code = <text>
Namespace N
    Class C
        Sub S()
            Dim x = {|r:{|b:&lt;x&gt;&lt;/x&gt;|}|}
        End Sub
    End Class
End Namespace</text>
                Await TestSelectionAsync(code)
            End Function

            <WpfFact, Trait(Traits.Feature, Traits.Features.ExtractMethod)>
            Public Async Function TestVisitStructure() As Task
                Dim code = <text>
Structure P
    Sub M()
        {|r:{|b:Dim x = 2
p:|}|}
    End Sub
End Structure</text>
                Await TestSelectionAsync(code)
            End Function

            <WpfFact, Trait(Traits.Feature, Traits.Features.ExtractMethod)>
            Public Async Function TestPropertyBlock() As Task
                Dim code = <text>
Class C
    Property P As String
        Get
            Return {|r:{|b:2|}|}
        End Get
    End Property
End Class</text>
                Await TestSelectionAsync(code)
            End Function

            <WpfFact, Trait(Traits.Feature, Traits.Features.ExtractMethod)>
            Public Async Function TestPropertyBlock2() As Task
                Dim code = <text>
Class C
    ReadOnly Property P As String
        Get
            {|r:{|b:Return 2
x:|}|}
        End Get
    End Property
End Class</text>
                Await TestSelectionAsync(code)
            End Function

            <WpfFact, Trait(Traits.Feature, Traits.Features.ExtractMethod)>
            Public Async Function TestUsingBlock1() As Task
                Dim code = <text>
        {|r:{|b:Using|} New C
        End Using|}
</text>
                Await TestInMethodAsync(code)
            End Function

            <WpfFact, Trait(Traits.Feature, Traits.Features.ExtractMethod)>
            Public Async Function TestUsingBlock2() As Task
                Dim code = <text>
        {|r:{|b:Using New C
        End Using|}|}
</text>
                Await TestInMethodAsync(code)
            End Function

            <WpfFact, Trait(Traits.Feature, Traits.Features.ExtractMethod)>
            Public Async Function TestSyncLockBlock1() As Task
                Dim code = <text>
        {|r:{|b:SyncLock|} New C
        End SyncLock|}
</text>
                Await TestInMethodAsync(code)
            End Function

            <WpfFact, Trait(Traits.Feature, Traits.Features.ExtractMethod)>
            Public Async Function TestWithBlock1() As Task
                Dim code = <text>
        {|r:{|b:With|} New C
            .Foo = 0
        End With|}
</text>
                Await TestInMethodAsync(code)
            End Function

            <WpfFact, Trait(Traits.Feature, Traits.Features.ExtractMethod)>
            <WorkItem(10203, "DevDiv_Projects/Roslyn")>
            Public Async Function TestStopStatement() As Task
                Dim code = <text>
        {|r:{|b:Stop|}|}
</text>
                Await TestInMethodAsync(code, expectedFail:=True)
            End Function

            <WpfFact, Trait(Traits.Feature, Traits.Features.ExtractMethod)>
            <WorkItem(10203, "DevDiv_Projects/Roslyn")>
            Public Async Function TestEndStatement() As Task
                Dim code = <text>
        {|r:{|b:End|}|}
</text>
                Await TestInMethodAsync(code, expectedFail:=True)
            End Function

            <WpfFact, Trait(Traits.Feature, Traits.Features.ExtractMethod)>
            Public Async Function TestContinueStatement() As Task
                Dim code = <text>
{|r:        While True
            Continue {|b:While|}
        End While|}
</text>
                Await TestInMethodAsync(code)
            End Function

            <WpfFact, Trait(Traits.Feature, Traits.Features.ExtractMethod)>
            Public Async Function TestTernaryConditional() As Task
                Dim code = <text>
        Dim f = {|r:{|b:If(True, 1, 0)|}|}
</text>
                Await TestInMethodAsync(code)
            End Function

            <WpfFact, Trait(Traits.Feature, Traits.Features.ExtractMethod)>
            Public Async Function TestSingleLineIf() As Task
                Dim code = <text>
{|r:        If True {|b:Then|} Return|}
</text>
                Await TestInMethodAsync(code)
            End Function

            <WpfFact, Trait(Traits.Feature, Traits.Features.ExtractMethod)>
            Public Async Function TestSingleLineElse() As Task
                Dim code = <text>
{|r:        If True Then Return {|b:Else|} End|}
</text>
                Await TestInMethodAsync(code)
            End Function

            <WpfFact, Trait(Traits.Feature, Traits.Features.ExtractMethod)>
            Public Async Function TestElsePart() As Task
                Dim code = <text>
{|r:        If True Then
            Return
        {|b:Else|}
            End
        End If|}
</text>
                Await TestInMethodAsync(code)
            End Function

            <WpfFact, Trait(Traits.Feature, Traits.Features.ExtractMethod)>
            Public Async Function TestTryPart() As Task
                Dim code = <text>
        {|r:{|b:Try|}
        Finally
        End Try|}
</text>
                Await TestInMethodAsync(code)
            End Function

            <WpfFact, Trait(Traits.Feature, Traits.Features.ExtractMethod)>
            Public Async Function TestFinallyPart() As Task
                Dim code = <text>
{|r:        Try
        {|b:Finally|}
        End Try|}
</text>
                Await TestInMethodAsync(code)
            End Function

            <WpfFact, Trait(Traits.Feature, Traits.Features.ExtractMethod)>
            Public Async Function TestCatchFilterClause() As Task
                Dim code = <text>
{|r:        Try
        Catch e As Exception {|b:When|} True
        End Try|}
</text>
                Await TestInMethodAsync(code)
            End Function

            <WpfFact, Trait(Traits.Feature, Traits.Features.ExtractMethod)>
            Public Async Function TestOnErrorGoto() As Task
                Dim code = <text>
{|r:        On Error {|b:GoTo|} foo|}
foo:
</text>
                Await TestInMethodAsync(code, expectedFail:=True)
            End Function

            <WpfFact, Trait(Traits.Feature, Traits.Features.ExtractMethod)>
            Public Async Function TestOnErrorResumeNext() As Task
                Dim code = <text>
{|r:        On Error {|b:Resume|} Next|}
</text>
                Await TestInMethodAsync(code, expectedFail:=True)
            End Function

            <WpfFact, Trait(Traits.Feature, Traits.Features.ExtractMethod)>
            Public Async Function TestErrorStatement() As Task
                Dim code = <text>
        {|r:{|b:Error|} 5|}
</text>
                Await TestInMethodAsync(code)
            End Function

            <WpfFact, Trait(Traits.Feature, Traits.Features.ExtractMethod)>
            Public Async Function TestResumeStatement() As Task
                Dim code = <text>
        {|r:{|b:Resume|} foo|}
</text>
                Await TestInMethodAsync(code, expectedFail:=True)
            End Function

            <WpfFact, Trait(Traits.Feature, Traits.Features.ExtractMethod)>
            Public Async Function TestSelectStatement() As Task
                Dim code = <text>
        {|r:{|b:Select|} Case foo
        End Select|}
</text>
                Await TestInMethodAsync(code)
            End Function

            <WpfFact, Trait(Traits.Feature, Traits.Features.ExtractMethod)>
            Public Async Function TestCaseBlock() As Task
                Dim code = <text>
{|r:        Select Case foo
            {|b:Case|} Nothing
        End Select|}
</text>
                Await TestInMethodAsync(code)
            End Function

            <WpfFact, Trait(Traits.Feature, Traits.Features.ExtractMethod)>
            Public Async Function TestDoLoop() As Task
                Dim code = <text>
        {|r:{|b:Do|}
            Loop|}
</text>
                Await TestInMethodAsync(code)
            End Function

            <WpfFact, Trait(Traits.Feature, Traits.Features.ExtractMethod)>
            Public Async Function TestLoopStatement() As Task
                Dim code = <text>
{|r:        Do
            {|b:Loop|}|}
</text>
                Await TestInMethodAsync(code)
            End Function

            <WpfFact, Trait(Traits.Feature, Traits.Features.ExtractMethod)>
            Public Async Function TestWhile() As Task
                Dim code = <text>
        {|r:{|b:While|} True
            End While|}
</text>
                Await TestInMethodAsync(code)
            End Function

            <WpfFact, Trait(Traits.Feature, Traits.Features.ExtractMethod)>
            Public Async Function TestErase() As Task
                Dim code = <text>
        {|r:{|b:Erase|} Nothing|}
</text>
                Await TestInMethodAsync(code)
            End Function

            <WpfFact, Trait(Traits.Feature, Traits.Features.ExtractMethod)>
            Public Async Function TestPredefinedCast() As Task
                Dim code = <text>
        Dim f = {|r:{|b:CInt(4)|}|}
</text>
                Await TestInMethodAsync(code)
            End Function

            <WorkItem(542859)>
            <WpfFact, Trait(Traits.Feature, Traits.Features.ExtractMethod)>
            Public Async Function TestIdentifierInCallStatement() As Task
                Dim code = <text>
        Dim v3 = {|r:CInt({|b:S|})|}
</text>
                Await TestInMethodAsync(code)
            End Function

            <WorkItem(542884)>
            <WpfFact, Trait(Traits.Feature, Traits.Features.ExtractMethod)>
            Public Async Function TestInferredFieldInitializer() As Task
                Dim code = <text>
        Dim loc = 2
{|r:        Dim anon = New With {Key {|b:loc|}}|}
</text>
                Await TestInMethodAsync(code)
            End Function

            <WorkItem(542938)>
            <WpfFact, Trait(Traits.Feature, Traits.Features.ExtractMethod)>
            Public Async Function TestMyBaseExpression() As Task
                Dim code = <text>
        {|r:{|b:MyBase|}.Equals(Nothing)|}
</text>
                Await TestInMethodAsync(code)
            End Function

            <WorkItem(542938)>
            <WpfFact, Trait(Traits.Feature, Traits.Features.ExtractMethod)>
            Public Async Function TestMyClassExpression() As Task
                Dim code = <text>
        {|r:{|b:MyClass|}.Equals(Nothing)|}
</text>
                Await TestInMethodAsync(code)
            End Function

            <WorkItem(543019)>
            <WpfFact, Trait(Traits.Feature, Traits.Features.ExtractMethod)>
            Public Async Function TestCatchStatement() As Task
                Dim code = <text>Imports System
Module Program
    Sub Main(args As String())
        Dim s As color
{|r:        Try
        Catch ex As Exception {|b:When|} s = color.blue
            Console.Write("Exception")
        End Try|}
    End Sub
End Module
Enum color
    blue
End Enum</text>
                Await TestSelectionAsync(code)
            End Function

            <WorkItem(543184)>
            <WpfFact, Trait(Traits.Feature, Traits.Features.ExtractMethod)>
            Public Async Function TestRangeVariable() As Task
                Dim code = <text>Imports System
Imports System.Collections.Generic
Imports System.Linq

Module Program
    Sub Main(args As String())
        Dim i2 = {|r:From i10 In New Integer() {} Group By {|b:i10|} Into Count|}
    End Sub
End Module</text>
                Await TestSelectionAsync(code)
            End Function

            <WorkItem(543184)>
            <WpfFact, Trait(Traits.Feature, Traits.Features.ExtractMethod)>
            Public Async Function TestRangeVariable2() As Task
                Dim code = <text>Imports System
Imports System.Collections.Generic
Imports System.Linq

Module Program
    Sub Main(args As String())
        Dim i2 = {|r:From i10 In New Integer() {} From i20 In New Integer() {} Select i10, {|b:i20|}|}
    End Sub
End Module</text>
                Await TestSelectionAsync(code)
            End Function

            <WorkItem(543244)>
            <WpfFact, Trait(Traits.Feature, Traits.Features.ExtractMethod)>
            Public Async Function TestMultipleNamesWithInitializerLocalDecl() As Task
                Dim code = <text>
Module M1
    WriteOnly Property Age() As Integer
        Set(ByVal Value As Integer)
            Dim a, b, c As {|b:Object =|} New Object()
lab1:
            SyncLock a
                GoTo lab1
            End SyncLock
            Console.WriteLine(b)
            Console.WriteLine(c)
        End Set
    End Property
End Module</text>
                Await TestSelectionAsync(code, expectedFail:=True)
            End Function

            <WorkItem(543184)>
            <WpfFact, Trait(Traits.Feature, Traits.Features.ExtractMethod)>
            Public Async Function TestRangeVariable3() As Task
                Dim code = <text>Imports System
Imports System.Collections.Generic
Imports System.Linq

Module Program
    Sub Main(args As String())
        Dim arr1 = Enumerable.Range(3, 4)
        Dim v1 = {|r:From x1 In arr1
                 From x4 In arr1
                 Select x1, {|b:x4|}|}
    End Sub
End Module</text>

                Await TestSelectionAsync(code)
            End Function

            <WorkItem(543685)>
            <WpfFact, Trait(Traits.Feature, Traits.Features.ExtractMethod)>
            Public Async Function TestAnonymousLambda() As Task
                Dim code = <text>
Imports System

Module S1
    Public Function Foo(Of T)() As System.Func(Of System.Func(Of T))
        Dim x2 = {|b:Function()
                     Return 5
                 End Function|}
        Return Nothing
    End Function

End Module</text>

                Await TestSelectionAsync(code)
            End Function

            <WorkItem(544374)>
            <WpfFact, Trait(Traits.Feature, Traits.Features.ExtractMethod)>
            Public Async Function TestDotNameFieldInitializer() As Task
                Dim code = <text>Imports System
 
Public Class C1
    Public FieldInt As Long
    Public FieldStr As String
 
    Public Property PropInt As Integer
End Class
 
Public Class C2
    Public Shared Sub Main
        Dim x = {|r:New C1() With {.FieldStr = {|b:.FieldInt|}.ToString()}|} 'BIND2:"23"
    End Sub
End Class
 
</text>

                Await TestSelectionAsync(code)
            End Function

            <WorkItem(545379)>
            <WpfFact, Trait(Traits.Feature, Traits.Features.ExtractMethod)>
            Public Async Function TestLambdaWithTrailingStatementTerminator() As Task
                Dim code = <text>Imports System
Module S1
    Public Function Foo(Of T)() As System.Func(Of System.Func(Of T))
        Dim x2 = {|b:Function()
                     Return
                 End Function
            |}Return Nothing
    End Function
End Module</text>

                Await TestSelectionAsync(code)
            End Function

            <WorkItem(530771)>
            <WpfFact, Trait(Traits.Feature, Traits.Features.ExtractMethod)>
            Public Async Function TestImplicitMemberAccessInMultipleStatements() As Task
                Dim code = <text>Module Program
    Class SomeType
        Public a As Integer
        Public b As Integer
        Public c As Integer
    End Class

    Sub Main(args As String())

        Dim st = New SomeType

        With st
            {|b:.a = 1 : .b = 2 : .c = 2|}
        End With
    End Sub
End Module
</text>

                Await TestSelectionAsync(code, expectedFail:=True)
            End Function
        End Class
    End Class
End Namespace
