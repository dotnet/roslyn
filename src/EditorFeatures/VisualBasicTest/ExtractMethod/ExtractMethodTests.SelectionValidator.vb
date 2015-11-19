' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports Microsoft.CodeAnalysis.Text
Imports Roslyn.Test.Utilities

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.UnitTests.ExtractMethod
    Partial Public Class ExtractMethodTests
        Public Class SelectionValidator
            <WpfFact, Trait(Traits.Feature, Traits.Features.ExtractMethod)>
            Public Sub SelectionTest1()
                Dim code = <text>{|b:Imports System|}</text>
                TestSelection(code, expectedFail:=True)
            End Sub

            <WpfFact, Trait(Traits.Feature, Traits.Features.ExtractMethod)>
            Public Sub SelectionTest2()
                Dim code = <text>{|b:Namespace A|}
End Namespace</text>
                TestSelection(code, expectedFail:=True)
            End Sub

            <WpfFact, Trait(Traits.Feature, Traits.Features.ExtractMethod)>
            Public Sub SelectionTest3()
                Dim code = <text>Namespace {|b:A|}
End Namespace</text>
                TestSelection(code, expectedFail:=True)
            End Sub

            <WpfFact, Trait(Traits.Feature, Traits.Features.ExtractMethod)>
            Public Sub SelectionTest4()
                Dim code = <text>{|b:Class|} A
End Class</text>
                TestSelection(code, expectedFail:=True)
            End Sub

            <WpfFact, Trait(Traits.Feature, Traits.Features.ExtractMethod)>
            Public Sub SelectionTest5()
                Dim code = <text>Class {|b:A|}
End Class</text>
                TestSelection(code, expectedFail:=True)
            End Sub

            <WpfFact, Trait(Traits.Feature, Traits.Features.ExtractMethod)>
            Public Sub SelectionTest6()
                Dim code = <text>Class A
    Implements {|b:IDisposable|}
End Class</text>
                TestSelection(code, expectedFail:=True)
            End Sub

            <WpfFact, Trait(Traits.Feature, Traits.Features.ExtractMethod)>
            Public Sub SelectionTest7()
                Dim code = <text>Class A
    Inherits {|b:Object|}
End Class</text>
                TestSelection(code, expectedFail:=True)
            End Sub

            <WpfFact, Trait(Traits.Feature, Traits.Features.ExtractMethod)>
            Public Sub SelectionTest8()
                Dim code = <text>Class A(Of {|b:T|})
End Class</text>
                TestSelection(code, expectedFail:=True)
            End Sub

            <WpfFact, Trait(Traits.Feature, Traits.Features.ExtractMethod)>
            Public Sub SelectionTest9()
                Dim code = <text>Class A(Of T As {|b:IDisposable|})
End Class</text>
                TestSelection(code, expectedFail:=True)
            End Sub

            <WpfFact, Trait(Traits.Feature, Traits.Features.ExtractMethod)>
            Public Sub SelectionTest10()
                Dim code = <text>Class A(Of T As {IComparable, {|b:IDisposable|}})
End Class</text>
                TestSelection(code, expectedFail:=True)
            End Sub

            <WpfFact, Trait(Traits.Feature, Traits.Features.ExtractMethod)>
            Public Sub SelectionTest11()
                Dim code = <text>Class A
    Function Method() As {|b:A|}
    End Function
End Class</text>
                TestSelection(code, expectedFail:=True)
            End Sub

            <WpfFact, Trait(Traits.Feature, Traits.Features.ExtractMethod)>
            Public Sub SelectionTest12()
                Dim code = <text>Class A
    Function Method(a As {|b:A|}) As A
    End Function
End Class</text>
                TestSelection(code, expectedFail:=True)
            End Sub

            <WpfFact, Trait(Traits.Feature, Traits.Features.ExtractMethod)>
            Public Sub SelectionTest13()
                Dim code = <text>Class A
    Function Method({|b:a|} As A) As A
    End Function
End Class</text>
                TestSelection(code, expectedFail:=True)
            End Sub

            <WpfFact, Trait(Traits.Feature, Traits.Features.ExtractMethod)>
            Public Sub SelectionTest14()
                Dim code = <text>Class A
    &lt;{|b:Foo()|}&gt;
    Function Method(a As A) As A
    End Function
End Class</text>
                TestSelection(code, expectedFail:=True)
            End Sub

            <WpfFact, Trait(Traits.Feature, Traits.Features.ExtractMethod)>
            Public Sub SelectionTest15()
                Dim code = <text>Class A
    &lt;Foo({|b:A|}:=1)&gt;
    Function Method(a As A) As A
    End Function
End Class</text>
                TestSelection(code, expectedFail:=True)
            End Sub

            <WpfFact, Trait(Traits.Feature, Traits.Features.ExtractMethod)>
            Public Sub SelectionTest16()
                Dim code = <text>Class A
    &lt;Foo(A:={|b:1|})&gt;
    Function Method(a As A) As A
    End Function
End Class</text>
                TestSelection(code, expectedFail:=True)
            End Sub

            <WpfFact, Trait(Traits.Feature, Traits.Features.ExtractMethod)>
            Public Sub SelectionTest17()
                Dim code = <text>Class A
    Dim {|b:i|} as Integer = 1
End Class</text>
                TestSelection(code, expectedFail:=True)
            End Sub

            <WpfFact, Trait(Traits.Feature, Traits.Features.ExtractMethod)>
            Public Sub SelectionTest18()
                Dim code = <text>Class A
    Dim i as {|b:Integer|} = 1
End Class</text>
                TestSelection(code, expectedFail:=True)
            End Sub

            <WpfFact, Trait(Traits.Feature, Traits.Features.ExtractMethod)>
            Public Sub SelectionTest19()
                Dim code = <text>Class A
    Const i as Integer = {|b:1|}
End Class</text>
                TestSelection(code, expectedFail:=True)
            End Sub

            <WpfFact, Trait(Traits.Feature, Traits.Features.ExtractMethod)>
            Public Sub SelectionTest20()
                Dim code = <text>Class A
    Const i as Integer = {|r:{|b:1 + |}2|}
End Class</text>
                TestSelection(code, expectedFail:=True)
            End Sub

            <WpfFact, Trait(Traits.Feature, Traits.Features.ExtractMethod)>
            Public Sub SelectionTest21()
                Dim code = <text>Class A
    Const i as {|b:Integer = 1 + |}2
End Class</text>
                TestSelection(code, expectedFail:=True)
            End Sub

            <WpfFact, Trait(Traits.Feature, Traits.Features.ExtractMethod)>
            Public Sub SelectionTest22()
                Dim code = <text>Class A
    Sub Method1()
        {|b:Dim i As Integer = 1
    End Sub

    Sub Method2()
        Dim b As Integer = 2|}
    End Sub
End Class</text>
                TestSelection(code, expectedFail:=True)
            End Sub

            <WpfFact, Trait(Traits.Feature, Traits.Features.ExtractMethod)>
            Public Sub SelectionTest23()
                Dim code = <text>Class A
    Sub Method1()
        {|b:Dim i As Integer = 1
    End Sub|}
End Class</text>
                TestSelection(code, expectedFail:=True)
            End Sub

            <WpfFact, Trait(Traits.Feature, Traits.Features.ExtractMethod)>
            Public Sub SelectionTest24()
                Dim code = <text>Class A
    Sub Method1()
#Region "A"
        {|b:Dim i As Integer = 1|}
#End Region
    End Sub
End Class</text>
                TestSelection(code)
            End Sub

            <WpfFact, Trait(Traits.Feature, Traits.Features.ExtractMethod)>
            Public Sub SelectionTest25()
                Dim code = <text>Class A
    Sub Method1()
{|b:#Region "A"
        Dim i As Integer = 1|}
#End Region
    End Sub
End Class</text>
                TestSelection(code, expectedFail:=True)
            End Sub

            <WpfFact, Trait(Traits.Feature, Traits.Features.ExtractMethod)>
            Public Sub SelectionTest26()
                Dim code = <text>Class A
    Sub Method1()
#Region "A"
        {|b:Dim i As Integer = 1
#End Region|}
    End Sub
End Class</text>
                TestSelection(code, expectedFail:=True)
            End Sub

            <WpfFact, Trait(Traits.Feature, Traits.Features.ExtractMethod)>
            Public Sub SelectionTest27()
                Dim code = <text>Class A
    Sub Method1()
#Region "A"
{|b:#End Region
        Dim i As Integer = 1|}
    End Sub
End Class</text>
                TestSelection(code, expectedFail:=True)
            End Sub

            <WpfFact, Trait(Traits.Feature, Traits.Features.ExtractMethod)>
            Public Sub SelectionTest28()
                Dim code = <text>Class A
    Sub Method1()
#If True Then
        {|b:Dim i As Integer = 1
#End if|}
    End Sub
End Class</text>
                TestSelection(code, expectedFail:=True)
            End Sub

            <WpfFact, Trait(Traits.Feature, Traits.Features.ExtractMethod)>
            Public Sub SelectionTest29()
                Dim code = <text>Class A
    Sub Method1()
{|b:#If True Then
        Dim i As Integer = 1|}
#End if
    End Sub
End Class</text>
                TestSelection(code, expectedFail:=True)
            End Sub

            <WpfFact, Trait(Traits.Feature, Traits.Features.ExtractMethod)>
            Public Sub SelectionTest30()
                Dim code = <text>Class A
    Sub Method1()
#If True Then
{|b:#End If
        Dim i As Integer = 1|}
    End Sub
End Class</text>
                TestSelection(code, expectedFail:=True)
            End Sub

            <WpfFact, Trait(Traits.Feature, Traits.Features.ExtractMethod)>
            Public Sub SelectionTest31()
                Dim code = <text>Class A
    Sub Method1()
#If True Then
{|b:#Else
        Dim i As Integer = 1|}
#End If
    End Sub
End Class</text>
                TestSelection(code, expectedFail:=True)
            End Sub

            <WpfFact, Trait(Traits.Feature, Traits.Features.ExtractMethod)>
            Public Sub SelectionTest32()
                Dim code = <text>Class A
    Sub Method1()
#If False Then
{|b:#ElseIf True Then
        Dim i As Integer = 1|}
#End If
    End Sub
End Class</text>
                TestSelection(code, expectedFail:=True)
            End Sub

            <WpfFact, Trait(Traits.Feature, Traits.Features.ExtractMethod)>
            Public Sub SelectionTest33()
                Dim code = <text>Class A
    Sub Method1()
{|b:#If True Then
        Dim i As Integer = 1
#End if|}
    End Sub
End Class</text>
                TestSelection(code)
            End Sub

            <WpfFact, Trait(Traits.Feature, Traits.Features.ExtractMethod)>
            Public Sub SelectionTest34()
                Dim code = <text>Class A
    Sub Method1()
{|b:#Region "A"
        Dim i As Integer = 1
#End Region|}
    End Sub
End Class</text>
                TestSelection(code)
            End Sub

            <WpfFact, Trait(Traits.Feature, Traits.Features.ExtractMethod)>
            Public Sub SelectionTest35()
                Dim code = <text>Class A
    Sub Method()
        {|b:' test|}
    End Sub
End Class</text>
                TestSelection(code, expectedFail:=True)
            End Sub

            <WpfFact, Trait(Traits.Feature, Traits.Features.ExtractMethod)>
            Public Sub SelectionTest36()
                Dim code = <text>Class A
    Function Method() As IEnumerable(Of Integer)
        {|r:{|b:Yield Return 1;|}|}
    End Function
End Class</text>
                TestSelection(code, expectedFail:=True)
            End Sub

            <WpfFact, Trait(Traits.Feature, Traits.Features.ExtractMethod)>
            Public Sub SelectionTest37()
                Dim code = <text>Class A
    Sub Method()
        Try
        Catch
            {|b:Throw|}
        End Try
    End Sub
End Class</text>
                TestSelection(code, expectedFail:=True)
            End Sub

            <WpfFact, Trait(Traits.Feature, Traits.Features.ExtractMethod)>
            Public Sub SelectionTest38()
                Dim code = <text>Class A
    Sub Method()
        Try
        Catch
            {|b:Throw new Exception()|}
        End Try
    End Sub
End Class</text>
                TestSelection(code)
            End Sub

            <WorkItem(540082)>
            <WpfFact, Trait(Traits.Feature, Traits.Features.ExtractMethod)>
            Public Sub SelectionTest39()
                Dim code = <text>Class A
    Sub Method()
        {|r:{|b:System|}.Console.WriteLine(1)|}
    End Sub
End Class</text>
                TestSelection(code)
            End Sub

            <WorkItem(540082)>
            <WpfFact, Trait(Traits.Feature, Traits.Features.ExtractMethod)>
            Public Sub SelectionTest40()
                Dim code = <text>Class A
    Sub Method()
        {|r:{|b:System.Console|}.WriteLine(1)|}
    End Sub
End Class</text>
                TestSelection(code)
            End Sub

            <WorkItem(540082)>
            <WpfFact, Trait(Traits.Feature, Traits.Features.ExtractMethod)>
            Public Sub SelectionTest41()
                Dim code = <text>Class A
    Sub Method()
        {|r:{|b:System.Console.WriteLine|}(1)|}
    End Sub
End Class</text>
                TestSelection(code)
            End Sub

            <WorkItem(540082)>
            <WpfFact, Trait(Traits.Feature, Traits.Features.ExtractMethod)>
            Public Sub SelectionTest42()
                Dim code = <text>Class A
    Sub Method()
{|r:        System.{|b:Console.WriteLine|}(1)|}
    End Sub
End Class</text>
                TestSelection(code)
            End Sub

            <WorkItem(540082)>
            <WpfFact, Trait(Traits.Feature, Traits.Features.ExtractMethod)>
            Public Sub SelectionTest43()
                Dim code = <text>Class A
    Sub Method()
{|r:        System.{|b:Console|}.WriteLine(1)|}
    End Sub
End Class</text>
                TestSelection(code)
            End Sub

            <WorkItem(540082)>
            <WpfFact, Trait(Traits.Feature, Traits.Features.ExtractMethod)>
            Public Sub SelectionTest44()
                Dim code = <text>Class A
    Sub Method()
{|r:        System.Console.{|b:WriteLine|}(1)|}
    End Sub
End Class</text>
                TestSelection(code)
            End Sub

            <WorkItem(539397)>
            <WpfFact, Trait(Traits.Feature, Traits.Features.ExtractMethod)>
            Public Sub SelectionTest45()
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
                TestSelection(code)
            End Sub

            <WorkItem(539242)>
            <WpfFact, Trait(Traits.Feature, Traits.Features.ExtractMethod)>
            Public Sub SelectionTest46()
                Dim code = <text>Module Program
    Sub Main(args As String())
        Dim startingScores1(,) As Short = {|r:New Short(1, 2) {{|b:{10, 10, 10}|}, {10, 10, 10}}|}
       Dim ticTacToe = {{0, 0, 0}, {0, 0, 0}, {0, 0, 0}}
    End Sub
End Module</text>
                TestSelection(code)
            End Sub

            <WorkItem(539242)>
            <WpfFact, Trait(Traits.Feature, Traits.Features.ExtractMethod)>
            Public Sub SelectionTest47()
                Dim code = <text>Module Program
    Sub Main(args As String())
        Dim startingScores1(,) As Short = New Short(1, 2) {{10, 10, 10}, {10, 10, 10}}
        Dim ticTacToe = {|r:{{|b:{0, 0, 0}|}, {0, 0, 0}, {0, 0, 0}}|}
    End Sub
End Module</text>
                TestSelection(code)
            End Sub

            <WorkItem(540375)>
            <WpfFact, Trait(Traits.Feature, Traits.Features.ExtractMethod)>
            Public Sub SelectIfThatAlwaysReturns()
                Dim code = <text>Module Program
    Sub Main(args As String())
        {|b:If True Then
            Return
        End If|}
        Console.Write(5)
    End Sub
End Module</text>
                TestSelection(code)
            End Sub

            <WorkItem(540375)>
            <WpfFact, Trait(Traits.Feature, Traits.Features.ExtractMethod)>
            Public Sub SelectConstIfWithReturn()
                Dim code = <text>Class A
    Public Sub Method1()
        Const b As Boolean = True
        {|b:If b Then
            Return
        End If|}
        Console.WriteLine()
    End Sub
End Class</text>
                TestSelection(code)
            End Sub

            <WpfFact, Trait(Traits.Feature, Traits.Features.ExtractMethod)>
            Public Sub SelectReturnButNotAllCodePathsContainAReturn()
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
                TestSelection(code, expectedFail:=True)
            End Sub

            <WpfFact, Trait(Traits.Feature, Traits.Features.ExtractMethod)>
            Public Sub SelectIfBranchWhereNotAllPathsReturn()
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
                TestSelection(code, expectedFail:=True)
            End Sub

            <WpfFact, Trait(Traits.Feature, Traits.Features.ExtractMethod)>
            Public Sub SelectLValueOfPlusEqualsOperator()
                Dim code = <text>Imports System

Class A
    Private Function Method() As Integer
        Dim i As Integer = 0
        {|r:{|b:i|} += 1|}
        Return i
    End Function
End Class</text>
                TestSelection(code)
            End Sub

            <WorkItem(10071, "DevDiv_Projects/Roslyn")>
            <WpfFact, Trait(Traits.Feature, Traits.Features.ExtractMethod)>
            Public Sub SelectRValueOfPlusEqualsOperator()
                Dim code = <text>Imports System

Class A
    Private Function Method() As Integer
        Dim i As Integer = 0
        i += {|b:1|}
        Return i
    End Function
End Class</text>
                TestSelection(code)
            End Sub

            <WpfFact, Trait(Traits.Feature, Traits.Features.ExtractMethod)>
            Public Sub SelectAddressOfOperator()
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
                TestSelection(code)
            End Sub

            <WpfFact, Trait(Traits.Feature, Traits.Features.ExtractMethod)>
            Public Sub SelectOperandOfAddressOfOperator()
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
                TestSelection(code)
            End Sub

            <WpfFact, Trait(Traits.Feature, Traits.Features.ExtractMethod)>
            Public Sub SelectInvalidSubexpressionToExpand()
                Dim code = <text>Class A
    Public Sub method(a As Integer, b As Integer, c As Integer)
{|r:        Dim d = a + {|b:b + c|}|}
    End Sub
End Class</text>
                TestSelection(code)
            End Sub

            <WpfFact, Trait(Traits.Feature, Traits.Features.ExtractMethod)>
            Public Sub SelectValidSubexpressionAndHenceDontExpand()
                Dim code = <text>Class A
    Public Sub method(a As Integer, b As Integer, c As Integer)
        Dim d = {|b:a + b|} + c
    End Sub
End Class</text>
                TestSelection(code)
            End Sub

            <WpfFact, Trait(Traits.Feature, Traits.Features.ExtractMethod)>
            Public Sub SelectLHSOfMinusEqualsOperator()
                Dim code = <text>Class A
    Public Sub method(a As Integer, b As Integer)
        {|r:{|b:a|} -= b|}
    End Sub
End Class</text>
                TestSelection(code)
            End Sub

            <WorkItem(540463)>
            <WpfFact, Trait(Traits.Feature, Traits.Features.ExtractMethod)>
            Public Sub SelectInnerBlockPartially()
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
                TestSelection(code)
            End Sub

            <WpfFact, Trait(Traits.Feature, Traits.Features.ExtractMethod)>
            Public Sub SelectInnerBlockPartially2()
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
                TestSelection(code)
            End Sub

            <WorkItem(540463)>
            <WpfFact, Trait(Traits.Feature, Traits.Features.ExtractMethod)>
            Public Sub SelectAcrossBlocks1()
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
                TestSelection(code)
            End Sub

            <WpfFact, Trait(Traits.Feature, Traits.Features.ExtractMethod)>
            Public Sub SelectMethodParameters()
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
                TestSelection(code)
            End Sub

            <WpfFact, Trait(Traits.Feature, Traits.Features.ExtractMethod)>
            Public Sub SelectChainedInvocations1()
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
                TestSelection(code)
            End Sub

            <WpfFact, Trait(Traits.Feature, Traits.Features.ExtractMethod)>
            Public Sub SelectChainedInvocations2()
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
                TestSelection(code)
            End Sub

            <WorkItem(540471)>
            <WpfFact, Trait(Traits.Feature, Traits.Features.ExtractMethod)>
            Public Sub BugFix6737()
                Dim code = <text>Module Program
    Sub Main(args As String())
        Dim x As Integer
        x = 20
{|b:Foo|}:
        x = 10
    End Sub
End Module</text>
                TestSelection(code, expectedFail:=True)
            End Sub

            <WorkItem(540471)>
            <WpfFact, Trait(Traits.Feature, Traits.Features.ExtractMethod)>
            Public Sub SelectLabel()
                Dim code = <text>Module Program
    Sub Main(args As String())
        Dim x As Integer
        x = 20
        GoTo Foo
        x = 24
{|b:Foo:|}
        x = 10
    End Sub</text>
                TestSelection(code, expectedFail:=True)
            End Sub

            <WpfFact, Trait(Traits.Feature, Traits.Features.ExtractMethod)>
            Public Sub SelectGotoStatement()
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
                TestSelection(code, expectedFail:=True)
            End Sub

            <WpfFact, Trait(Traits.Feature, Traits.Features.ExtractMethod)>
            Public Sub SelectGotoStatement1()
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
                TestSelection(code, expectedFail:=True)
            End Sub

            <WpfFact, Trait(Traits.Feature, Traits.Features.ExtractMethod)>
            Public Sub SelectGotoWithLabel()
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
                TestSelection(code)
            End Sub

            <WorkItem(540471)>
            <WpfFact, Trait(Traits.Feature, Traits.Features.ExtractMethod)>
            Public Sub SelectGotoWithLabel1()
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
                TestSelection(code)
            End Sub

            <WorkItem(540497)>
            <WpfFact, Trait(Traits.Feature, Traits.Features.ExtractMethod)>
            Public Sub SelectAutoPropInitializer()
                Dim code = <text>Class B
    Property ID() As Integer = {|b:1|}
End Class</text>
                TestSelection(code)
            End Sub

            <WpfFact, Trait(Traits.Feature, Traits.Features.ExtractMethod)>
            Public Sub SelectCollectionInitializer()
                Dim code = <text>Class B
    Dim list = New List(Of String) From {{|b:"abc"|}, "def", "ghi"}
End Class</text>
                TestSelection(code)
            End Sub

            <WpfFact, Trait(Traits.Feature, Traits.Features.ExtractMethod)>
            Public Sub SelectCollectionInitializer1()
                Dim code = <text>Class B
    Dim list = New List(Of String) From {|r:{{|b:"abc"|}, "def", "ghi"}|}
End Class</text>
                TestSelection(code)
            End Sub

            <WorkItem(6626, "DevDiv_Projects/Roslyn")>
            <WpfFact, Trait(Traits.Feature, Traits.Features.ExtractMethod)>
            Public Sub SelectSectionBeforeUnreachableCode()
                Dim code = <text>Module Program
    Sub Main(args As String())
        {|b:Dim x As Integer
        x = 1|}
        Return
        Dim y As Integer = x
    End Sub
End Module</text>
                TestSelection(code)
            End Sub

            <WorkItem(540200)>
            <WpfFact, Trait(Traits.Feature, Traits.Features.ExtractMethod)>
            Public Sub BugFix6376()
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
                TestExtractMethod(code, expected)
            End Sub

            <WorkItem(540465)>
            <WpfFact, Trait(Traits.Feature, Traits.Features.ExtractMethod)>
            Public Sub BugFix6731()
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
                TestSelection(code)
            End Sub

            <WorkItem(540481)>
            <WpfFact, Trait(Traits.Feature, Traits.Features.ExtractMethod)>
            Public Sub BugFix6750()
                Dim code = <text>Imports System
Imports System.Collections

Class A
    Private Sub method()
        Dim a As Integer() = new Integer({|b:1|}) { }
    End Sub
End Class</text>
                TestSelection(code)
            End Sub

            <WorkItem(540481)>
            <WpfFact, Trait(Traits.Feature, Traits.Features.ExtractMethod)>
            Public Sub BugFix6750_1()
                Dim code = <text>Imports System
Imports System.Collections

Class A
    Private Sub method()
        Dim a As Integer() = {|r:new Integer({|b:1|}) { 1, 2 }|}
    End Sub
End Class</text>
                TestSelection(code)
            End Sub

            <WorkItem(10071, "DevDiv_Projects/Roslyn")>
            <WorkItem(544602)>
            <WpfFact, Trait(Traits.Feature, Traits.Features.ExtractMethod)>
            Public Sub DontCrash()
                IterateAll(TestResource.AllInOneVisualBasicCode)
            End Sub

            <WorkItem(541091)>
            <WpfFact, Trait(Traits.Feature, Traits.Features.ExtractMethod)>
            Public Sub BugFix7660_1()
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


                TestExtractMethod(code, expected)
            End Sub

            <WorkItem(541091)>
            <WpfFact, Trait(Traits.Feature, Traits.Features.ExtractMethod)>
            Public Sub BugFix7660_2()
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

                TestExtractMethod(code, expected)
            End Sub

            <WorkItem(541091)>
            <WpfFact, Trait(Traits.Feature, Traits.Features.ExtractMethod)>
            Public Sub BugFix7660_3()
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

                TestExtractMethod(code, expected)
            End Sub

            <WorkItem(541620)>
            <WpfFact, Trait(Traits.Feature, Traits.Features.ExtractMethod)>
            Public Sub CatchVariable()
                Dim code = <text>Class SomeOtherClass
    Sub M()
{|r:        Try
        Catch {|b:ex|} As Exception
        End Try|}
    End Sub
End Class</text>

                TestSelection(code)
            End Sub

            <WorkItem(541695)>
            <WpfFact, Trait(Traits.Feature, Traits.Features.ExtractMethod)>
            Public Sub EmptySelectionWithMissingToken()
                Dim code = <text>Module Program
    Sub Main(args As String())
 
        End{|b:
        |}
End Module
</text>

                TestSelection(code, expectedFail:=True)
            End Sub

            <WorkItem(541411)>
            <WpfFact, Trait(Traits.Feature, Traits.Features.ExtractMethod)>
            Public Sub ControlVariableInForStatement()
                Dim code = <text>Module Program
    Sub Main(ByVal args() As String)
        Dim i As Integer
{|r:        For {|b:i|} = 0 To 2
            System.Console.WriteLine("In the For Loop")
        Next i|}
  End Sub
End Module
</text>

                TestSelection(code)
            End Sub

            <WorkItem(541411)>
            <WpfFact, Trait(Traits.Feature, Traits.Features.ExtractMethod)>
            Public Sub ControlVariableInForEachStatement()
                Dim code = <text>Module Program
    Sub Main(args As String())
        Dim i As Integer
{|r:        For Each {|b:i|} In {1, 2}
        Next i|}
    End Sub
End Module
</text>
                TestSelection(code)
            End Sub

            <WorkItem(541416)>
            <WpfFact, Trait(Traits.Feature, Traits.Features.ExtractMethod)>
            Public Sub ControlVariablesInNextStatement()
                Dim code = <text>Module Program
    Sub Main(ByVal args() As String)
        Dim i As Integer
{|r:        For i = 0 To 2
            System.Console.WriteLine("In the For Loop")
        Next {|b:i|}|}
    End Sub
End Module
</text>
                TestSelection(code)
            End Sub

            <WorkItem(528654)>
            <WpfFact, Trait(Traits.Feature, Traits.Features.ExtractMethod)>
            Public Sub ReDimSelectionValidator()
                Dim code = <text>Module M
    Sub Main()
        Dim x(2)
{|r:        ReDim {|b:x(3)|}|}
    End Sub
End Module</text>
                TestSelection(code)
            End Sub

            <WorkItem(542248)>
            <WpfFact, Trait(Traits.Feature, Traits.Features.ExtractMethod)>
            Public Sub InvalidCode_NoOuterType()
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
                TestSelection(code, expectedFail:=True)
            End Sub

            <WorkItem(542274)>
            <WpfFact, Trait(Traits.Feature, Traits.Features.ExtractMethod)>
            Public Sub ObjectMemberInitializer1()
                Dim code = <text>Imports System
Imports System.Collections.Generic
Imports System.Linq
 
Module Program
    Sub Main(args As String())
{|r:        Dim ao = New With {.a = 1, .b = {|b:1 +.a|}}|}
    End Sub
End Module</text>

                TestSelection(code)
            End Sub

            <WorkItem(542274)>
            <WpfFact, Trait(Traits.Feature, Traits.Features.ExtractMethod)>
            Public Sub ObjectMemberInitializer2()
                Dim code = <text>Imports System
Imports System.Collections.Generic
Imports System.Linq
 
Module Program
    Sub Main(args As String())
        Dim ao = New With {.a = 1, .b = {|b:1|} +.a}
    End Sub
End Module</text>

                TestSelection(code)
            End Sub

            <WorkItem(542274)>
            <WpfFact, Trait(Traits.Feature, Traits.Features.ExtractMethod)>
            Public Sub ObjectMemberInitializer3()
                Dim code = <text>Imports System
Imports System.Collections.Generic
Imports System.Linq
 
Module Program
    Sub Main(args As String())
{|r:        Dim ao = New With {.a = 1, .b = 1 + {|b:.a|}}|}
    End Sub
End Module</text>

                TestSelection(code)
            End Sub

            <WorkItem(542274)>
            <WpfFact, Trait(Traits.Feature, Traits.Features.ExtractMethod)>
            Public Sub ObjectMemberInitializer4()
                Dim code = <text>Imports System
Imports System.Collections.Generic
Imports System.Linq
 
Module Program
    Sub Main(args As String())
{|r:        Dim ao = New With {.a = 1, {|b:.x|} = 1 +.a}|}
    End Sub
End Module</text>

                TestSelection(code)
            End Sub

            <WorkItem(542274)>
            <WpfFact, Trait(Traits.Feature, Traits.Features.ExtractMethod)>
            Public Sub ObjectMemberInitializer5()
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

                TestSelection(code)
            End Sub

            <WorkItem(542274)>
            <WpfFact, Trait(Traits.Feature, Traits.Features.ExtractMethod)>
            Public Sub ObjectMemberInitializer6()
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

                TestSelection(code)
            End Sub

            <WpfFact, Trait(Traits.Feature, Traits.Features.ExtractMethod)>
            Public Sub TestMethodCallInvalidSelection()
                Dim code = <text>Imports System.Threading
Module Program
    Sub Main()
        GetInitialSelection{|b:Info(Canc|}ellationToken.None)
    End Sub
    Private Sub GetInitialSelectionInfo(cancellationToken As CancellationToken)
    End Sub
End Module</text>
                TestSelection(code, expectedFail:=True)
            End Sub

            <WpfFact, Trait(Traits.Feature, Traits.Features.ExtractMethod)>
            Public Sub TestMultiLineLambda()
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
                TestSelection(code)
            End Sub

            <WpfFact, Trait(Traits.Feature, Traits.Features.ExtractMethod)>
            Public Sub TestNullableTypeName()
                Dim code = <text>
Module Program
    Sub Main(args As String())
{|r:        Dim f As {|b:DateTime|}?|}
    End Sub
End Module</text>
                TestSelection(code)
            End Sub

            <WpfFact, Trait(Traits.Feature, Traits.Features.ExtractMethod)>
            Public Sub TestPredefinedTypeInsideGetType()
                Dim code = <text>
Class C
    Sub S()
        Dim f = {|r:GetType({|b:Integer|})|}
    End Sub
End Class</text>
                TestSelection(code)
            End Sub

            <WpfFact, Trait(Traits.Feature, Traits.Features.ExtractMethod)>
            Public Sub TestIdentifierNameInsideGetType()
                Dim code = <text>
Class C
    Sub S()
        Dim f = {|r:GetType({|b:C|})|}
    End Sub
End Class</text>
                TestSelection(code)
            End Sub

            <WpfFact, Trait(Traits.Feature, Traits.Features.ExtractMethod)>
            Public Sub TestQualifiedNameInsideGetType()
                Dim code = <text>
Namespace N
    Class C
        Sub S()
            Dim f = {|r:GetType(N.{|b:C|})|}
        End Sub
    End Class
End Namespace</text>
                TestSelection(code)
            End Sub

            <WpfFact, Trait(Traits.Feature, Traits.Features.ExtractMethod)>
            Public Sub TestQualifiedNameInsideTypeOfIs()
                Dim code = <text>
Namespace N
    Class C
        Sub S()
            Dim f = {|r:TypeOf foo Is N.{|b:C|}|}
        End Sub
    End Class
End Namespace</text>
                TestSelection(code)
            End Sub

            
            <WpfFact, Trait(Traits.Feature, Traits.Features.ExtractMethod)>
            Public Sub TestQualifiedNameInsideArrayCreationExpression()
                Dim code = <text>
Namespace N
    Class C
        Sub S()
            Dim f = {|r:New N.{|b:C|}() {}|}
        End Sub
    End Class
End Namespace</text>
                TestSelection(code)
            End Sub

            <WpfFact, Trait(Traits.Feature, Traits.Features.ExtractMethod)>
            Public Sub TestQualifiedNameInsideCastExpression()
                Dim code = <text>
Namespace N
    Class C
        Sub S()
            Dim f = {|r:CType(foo, N.{|b:C|})|}
        End Sub
    End Class
End Namespace</text>
                TestSelection(code)
            End Sub

            <WpfFact, Trait(Traits.Feature, Traits.Features.ExtractMethod)>
            Public Sub TestQualifiedNameInsideArrayType()
                Dim code = <text>
Namespace N
    Class C
        Sub S()
{|r:            Dim f As N.{|b:C|}()|}
        End Sub
    End Class
End Namespace</text>
                TestSelection(code)
            End Sub

            <WpfFact, Trait(Traits.Feature, Traits.Features.ExtractMethod)>
            Public Sub TestQualifiedNameInsideAsClause()
                Dim code = <text>
Namespace N
    Class C
        Sub S()
{|r:            Dim f As N.{|b:C|}|}
        End Sub
    End Class
End Namespace</text>
                TestSelection(code)
            End Sub

            <WpfFact, Trait(Traits.Feature, Traits.Features.ExtractMethod)>
            <WorkItem(542800)>
            Public Sub TestXmlNode()
                Dim code = <text>
Namespace N
    Class C
        Sub S()
            Dim x = {|r:{|b:&lt;x&gt;&lt;/x&gt;|}|}
        End Sub
    End Class
End Namespace</text>
                TestSelection(code)
            End Sub

            <WpfFact, Trait(Traits.Feature, Traits.Features.ExtractMethod)>
            Public Sub TestVisitStructure()
                Dim code = <text>
Structure P
    Sub M()
        {|r:{|b:Dim x = 2
p:|}|}
    End Sub
End Structure</text>
                TestSelection(code)
            End Sub

            <WpfFact, Trait(Traits.Feature, Traits.Features.ExtractMethod)>
            Public Sub TestPropertyBlock()
                Dim code = <text>
Class C
    Property P As String
        Get
            Return {|r:{|b:2|}|}
        End Get
    End Property
End Class</text>
                TestSelection(code)
            End Sub

            <WpfFact, Trait(Traits.Feature, Traits.Features.ExtractMethod)>
            Public Sub TestPropertyBlock2()
                Dim code = <text>
Class C
    ReadOnly Property P As String
        Get
            {|r:{|b:Return 2
x:|}|}
        End Get
    End Property
End Class</text>
                TestSelection(code)
            End Sub

            <WpfFact, Trait(Traits.Feature, Traits.Features.ExtractMethod)>
            Public Sub TestUsingBlock1()
                Dim code = <text>
        {|r:{|b:Using|} New C
        End Using|}
</text>
                TestInMethod(code)
            End Sub

            <WpfFact, Trait(Traits.Feature, Traits.Features.ExtractMethod)>
            Public Sub TestUsingBlock2()
                Dim code = <text>
        {|r:{|b:Using New C
        End Using|}|}
</text>
                TestInMethod(code)
            End Sub

            <WpfFact, Trait(Traits.Feature, Traits.Features.ExtractMethod)>
            Public Sub TestSyncLockBlock1()
                Dim code = <text>
        {|r:{|b:SyncLock|} New C
        End SyncLock|}
</text>
                TestInMethod(code)
            End Sub

            <WpfFact, Trait(Traits.Feature, Traits.Features.ExtractMethod)>
            Public Sub TestWithBlock1()
                Dim code = <text>
        {|r:{|b:With|} New C
            .Foo = 0
        End With|}
</text>
                TestInMethod(code)
            End Sub

            <WpfFact, Trait(Traits.Feature, Traits.Features.ExtractMethod)>
            <WorkItem(10203, "DevDiv_Projects/Roslyn")>
            Public Sub TestStopStatement()
                Dim code = <text>
        {|r:{|b:Stop|}|}
</text>
                TestInMethod(code, expectedFail:=True)
            End Sub

            <WpfFact, Trait(Traits.Feature, Traits.Features.ExtractMethod)>
            <WorkItem(10203, "DevDiv_Projects/Roslyn")>
            Public Sub TestEndStatement()
                Dim code = <text>
        {|r:{|b:End|}|}
</text>
                TestInMethod(code, expectedFail:=True)
            End Sub

            <WpfFact, Trait(Traits.Feature, Traits.Features.ExtractMethod)>
            Public Sub TestContinueStatement()
                Dim code = <text>
{|r:        While True
            Continue {|b:While|}
        End While|}
</text>
                TestInMethod(code)
            End Sub

            <WpfFact, Trait(Traits.Feature, Traits.Features.ExtractMethod)>
            Public Sub TestTernaryConditional()
                Dim code = <text>
        Dim f = {|r:{|b:If(True, 1, 0)|}|}
</text>
                TestInMethod(code)
            End Sub

            <WpfFact, Trait(Traits.Feature, Traits.Features.ExtractMethod)>
            Public Sub TestSingleLineIf()
                Dim code = <text>
{|r:        If True {|b:Then|} Return|}
</text>
                TestInMethod(code)
            End Sub

            <WpfFact, Trait(Traits.Feature, Traits.Features.ExtractMethod)>
            Public Sub TestSingleLineElse()
                Dim code = <text>
{|r:        If True Then Return {|b:Else|} End|}
</text>
                TestInMethod(code)
            End Sub

            <WpfFact, Trait(Traits.Feature, Traits.Features.ExtractMethod)>
            Public Sub TestElsePart()
                Dim code = <text>
{|r:        If True Then
            Return
        {|b:Else|}
            End
        End If|}
</text>
                TestInMethod(code)
            End Sub

            <WpfFact, Trait(Traits.Feature, Traits.Features.ExtractMethod)>
            Public Sub TestTryPart()
                Dim code = <text>
        {|r:{|b:Try|}
        Finally
        End Try|}
</text>
                TestInMethod(code)
            End Sub

            <WpfFact, Trait(Traits.Feature, Traits.Features.ExtractMethod)>
            Public Sub TestFinallyPart()
                Dim code = <text>
{|r:        Try
        {|b:Finally|}
        End Try|}
</text>
                TestInMethod(code)
            End Sub

            <WpfFact, Trait(Traits.Feature, Traits.Features.ExtractMethod)>
            Public Sub TestCatchFilterClause()
                Dim code = <text>
{|r:        Try
        Catch e As Exception {|b:When|} True
        End Try|}
</text>
                TestInMethod(code)
            End Sub

            <WpfFact, Trait(Traits.Feature, Traits.Features.ExtractMethod)>
            Public Sub TestOnErrorGoto()
                Dim code = <text>
{|r:        On Error {|b:GoTo|} foo|}
foo:
</text>
                TestInMethod(code, expectedFail:=True)
            End Sub

            <WpfFact, Trait(Traits.Feature, Traits.Features.ExtractMethod)>
            Public Sub TestOnErrorResumeNext()
                Dim code = <text>
{|r:        On Error {|b:Resume|} Next|}
</text>
                TestInMethod(code, expectedFail:=True)
            End Sub

            <WpfFact, Trait(Traits.Feature, Traits.Features.ExtractMethod)>
            Public Sub TestErrorStatement()
                Dim code = <text>
        {|r:{|b:Error|} 5|}
</text>
                TestInMethod(code)
            End Sub

            <WpfFact, Trait(Traits.Feature, Traits.Features.ExtractMethod)>
            Public Sub TestResumeStatement()
                Dim code = <text>
        {|r:{|b:Resume|} foo|}
</text>
                TestInMethod(code, expectedFail:=True)
            End Sub

            <WpfFact, Trait(Traits.Feature, Traits.Features.ExtractMethod)>
            Public Sub TestSelectStatement()
                Dim code = <text>
        {|r:{|b:Select|} Case foo
        End Select|}
</text>
                TestInMethod(code)
            End Sub

            <WpfFact, Trait(Traits.Feature, Traits.Features.ExtractMethod)>
            Public Sub TestCaseBlock()
                Dim code = <text>
{|r:        Select Case foo
            {|b:Case|} Nothing
        End Select|}
</text>
                TestInMethod(code)
            End Sub

            <WpfFact, Trait(Traits.Feature, Traits.Features.ExtractMethod)>
            Public Sub TestDoLoop()
                Dim code = <text>
        {|r:{|b:Do|}
            Loop|}
</text>
                TestInMethod(code)
            End Sub

            <WpfFact, Trait(Traits.Feature, Traits.Features.ExtractMethod)>
            Public Sub TestLoopStatement()
                Dim code = <text>
{|r:        Do
            {|b:Loop|}|}
</text>
                TestInMethod(code)
            End Sub

            <WpfFact, Trait(Traits.Feature, Traits.Features.ExtractMethod)>
            Public Sub TestWhile()
                Dim code = <text>
        {|r:{|b:While|} True
            End While|}
</text>
                TestInMethod(code)
            End Sub

            <WpfFact, Trait(Traits.Feature, Traits.Features.ExtractMethod)>
            Public Sub TestErase()
                Dim code = <text>
        {|r:{|b:Erase|} Nothing|}
</text>
                TestInMethod(code)
            End Sub

            <WpfFact, Trait(Traits.Feature, Traits.Features.ExtractMethod)>
            Public Sub TestPredefinedCast()
                Dim code = <text>
        Dim f = {|r:{|b:CInt(4)|}|}
</text>
                TestInMethod(code)
            End Sub

            <WorkItem(542859)>
            <WpfFact, Trait(Traits.Feature, Traits.Features.ExtractMethod)>
            Public Sub IdentifierInCallStatement()
                Dim code = <text>
        Dim v3 = {|r:CInt({|b:S|})|}
</text>
                TestInMethod(code)
            End Sub

            <WorkItem(542884)>
            <WpfFact, Trait(Traits.Feature, Traits.Features.ExtractMethod)>
            Public Sub InferredFieldInitializer()
                Dim code = <text>
        Dim loc = 2
{|r:        Dim anon = New With {Key {|b:loc|}}|}
</text>
                TestInMethod(code)
            End Sub

            <WorkItem(542938)>
            <WpfFact, Trait(Traits.Feature, Traits.Features.ExtractMethod)>
            Public Sub MyBaseExpression()
                Dim code = <text>
        {|r:{|b:MyBase|}.Equals(Nothing)|}
</text>
                TestInMethod(code)
            End Sub

            <WorkItem(542938)>
            <WpfFact, Trait(Traits.Feature, Traits.Features.ExtractMethod)>
            Public Sub MyClassExpression()
                Dim code = <text>
        {|r:{|b:MyClass|}.Equals(Nothing)|}
</text>
                TestInMethod(code)
            End Sub

            <WorkItem(543019)>
            <WpfFact, Trait(Traits.Feature, Traits.Features.ExtractMethod)>
            Public Sub CatchStatement()
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
                TestSelection(code)
            End Sub

            <WorkItem(543184)>
            <WpfFact, Trait(Traits.Feature, Traits.Features.ExtractMethod)>
            Public Sub RangeVariable()
                Dim code = <text>Imports System
Imports System.Collections.Generic
Imports System.Linq

Module Program
    Sub Main(args As String())
        Dim i2 = {|r:From i10 In New Integer() {} Group By {|b:i10|} Into Count|}
    End Sub
End Module</text>
                TestSelection(code)
            End Sub

            <WorkItem(543184)>
            <WpfFact, Trait(Traits.Feature, Traits.Features.ExtractMethod)>
            Public Sub RangeVariable2()
                Dim code = <text>Imports System
Imports System.Collections.Generic
Imports System.Linq

Module Program
    Sub Main(args As String())
        Dim i2 = {|r:From i10 In New Integer() {} From i20 In New Integer() {} Select i10, {|b:i20|}|}
    End Sub
End Module</text>
                TestSelection(code)
            End Sub

            <WorkItem(543244)>
            <WpfFact, Trait(Traits.Feature, Traits.Features.ExtractMethod)>
            Public Sub MultipleNamesWithInitializerLocalDecl()
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
                TestSelection(code, expectedFail:=True)
            End Sub

            <WorkItem(543184)>
            <WpfFact, Trait(Traits.Feature, Traits.Features.ExtractMethod)>
            Public Sub RangeVariable3()
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

                TestSelection(code)
            End Sub

            <WorkItem(543685)>
            <WpfFact, Trait(Traits.Feature, Traits.Features.ExtractMethod)>
            Public Sub AnonymousLambda()
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

                TestSelection(code)
            End Sub

            <WorkItem(544374)>
            <WpfFact, Trait(Traits.Feature, Traits.Features.ExtractMethod)>
            Public Sub DotNameFieldInitializer()
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

                TestSelection(code)
            End Sub

            <WorkItem(545379)>
            <WpfFact, Trait(Traits.Feature, Traits.Features.ExtractMethod)>
            Public Sub LambdaWithTrailingStatementTerminator()
                Dim code = <text>Imports System
Module S1
    Public Function Foo(Of T)() As System.Func(Of System.Func(Of T))
        Dim x2 = {|b:Function()
                     Return
                 End Function
            |}Return Nothing
    End Function
End Module</text>

                TestSelection(code)
            End Sub

            <WorkItem(530771)>
            <WpfFact, Trait(Traits.Feature, Traits.Features.ExtractMethod)>
            Public Sub TestImplicitMemberAccessInMultipleStatements()
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

                TestSelection(code, expectedFail:=True)
            End Sub
        End Class
    End Class
End Namespace
