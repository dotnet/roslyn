' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports Microsoft.CodeAnalysis.Semantics
Imports Microsoft.CodeAnalysis.Test.Utilities
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax
Imports Roslyn.Test.Utilities

Namespace Microsoft.CodeAnalysis.VisualBasic.UnitTests.Semantics

    Partial Public Class IOperationTests
        Inherits SemanticModelTestBase

        <Fact(), WorkItem(17602, "https://github.com/dotnet/roslyn/issues/17602")>
        Public Sub IForEachLoopStatement_SimpleForLoopsTest()
            Dim source = <![CDATA[
Imports System
Class C
    Shared Sub Main()
        Dim arr As String() = New String(1) {}
        arr(0) = "one"
        arr(1) = "two"
        For Each s As String In arr'BIND:"For Each s As String In arr"
            Console.WriteLine(s)
        Next
    End Sub
End Class
    ]]>.Value

            Dim expectedOperationTree = <![CDATA[
IForEachLoopStatement (Iteration variable: s As System.String) (LoopKind.ForEach) (OperationKind.LoopStatement)
  Collection: IConversionExpression (ConversionKind.Basic, Implicit) (OperationKind.ConversionExpression, Type: System.Collections.IEnumerable)
      ILocalReferenceExpression: arr (OperationKind.LocalReferenceExpression, Type: System.String())
  IBlockStatement (1 statements) (OperationKind.BlockStatement)
    IExpressionStatement (OperationKind.ExpressionStatement)
      IInvocationExpression (static Sub System.Console.WriteLine(value As System.String)) (OperationKind.InvocationExpression, Type: System.Void)
        IArgument (Matching Parameter: value) (OperationKind.Argument)
          ILocalReferenceExpression: s (OperationKind.LocalReferenceExpression, Type: System.String)
]]>.Value

            VerifyOperationTreeForTest(Of ForEachBlockSyntax)(source, expectedOperationTree)
        End Sub

        <Fact(), WorkItem(17602, "https://github.com/dotnet/roslyn/issues/17602")>
        Public Sub IForEachLoopStatement_WithList()
            Dim source = <![CDATA[
Class Program
    Private Shared Sub Main(args As String())
        Dim list As New System.Collections.Generic.List(Of String)()
        list.Add("a")
        list.Add("b")
        list.Add("c")
        For Each item As String In list'BIND:"For Each item As String In list"
            System.Console.WriteLine(item)
        Next

    End Sub
End Class
    ]]>.Value

            Dim expectedOperationTree = <![CDATA[
IForEachLoopStatement (Iteration variable: item As System.String) (LoopKind.ForEach) (OperationKind.LoopStatement)
  Collection: ILocalReferenceExpression: list (OperationKind.LocalReferenceExpression, Type: System.Collections.Generic.List(Of System.String))
  IBlockStatement (1 statements) (OperationKind.BlockStatement)
    IExpressionStatement (OperationKind.ExpressionStatement)
      IInvocationExpression (static Sub System.Console.WriteLine(value As System.String)) (OperationKind.InvocationExpression, Type: System.Void)
        IArgument (Matching Parameter: value) (OperationKind.Argument)
          ILocalReferenceExpression: item (OperationKind.LocalReferenceExpression, Type: System.String)
]]>.Value

            VerifyOperationTreeForTest(Of ForEachBlockSyntax)(source, expectedOperationTree)
        End Sub

        <Fact(), WorkItem(17602, "https://github.com/dotnet/roslyn/issues/17602")>
        Public Sub IForEachLoopStatement_WithBreak()
            Dim source = <![CDATA[
Class C
    Public Shared Sub Main()
        Dim S As String() = New String() {"ABC", "XYZ"}
        For Each x As String In S
            For Each y As Char In x'BIND:"For Each y As Char In x"
                If y = "B"c Then
                    Exit For
                Else
                    System.Console.WriteLine(y)
                End If
            Next
        Next
    End Sub
End Class

    ]]>.Value

            Dim expectedOperationTree = <![CDATA[
IForEachLoopStatement (Iteration variable: y As System.Char) (LoopKind.ForEach) (OperationKind.LoopStatement)
  Collection: ILocalReferenceExpression: x (OperationKind.LocalReferenceExpression, Type: System.String)
  IBlockStatement (1 statements) (OperationKind.BlockStatement)
    IIfStatement (OperationKind.IfStatement)
      Condition: IBinaryOperatorExpression (BinaryOperationKind.IntegerEquals) (OperationKind.BinaryOperatorExpression, Type: System.Boolean)
          Left: ILocalReferenceExpression: y (OperationKind.LocalReferenceExpression, Type: System.Char)
          Right: ILiteralExpression (OperationKind.LiteralExpression, Type: System.Char, Constant: B)
      IBlockStatement (1 statements) (OperationKind.BlockStatement)
        IBranchStatement (BranchKind.Break, Label: exit) (OperationKind.BranchStatement)
      IBlockStatement (1 statements) (OperationKind.BlockStatement)
        IExpressionStatement (OperationKind.ExpressionStatement)
          IInvocationExpression (static Sub System.Console.WriteLine(value As System.Char)) (OperationKind.InvocationExpression, Type: System.Void)
            IArgument (Matching Parameter: value) (OperationKind.Argument)
              ILocalReferenceExpression: y (OperationKind.LocalReferenceExpression, Type: System.Char)
]]>.Value

            VerifyOperationTreeForTest(Of ForEachBlockSyntax)(source, expectedOperationTree)
        End Sub

        <Fact(), WorkItem(17602, "https://github.com/dotnet/roslyn/issues/17602")>
        Public Sub IForEachLoopStatement_WithContinue()
            Dim source = <![CDATA[
Class C
    Public Shared Sub Main()
        Dim S As String() = New String() {"ABC", "XYZ"}
        For Each x As String In S'BIND:"For Each x As String In S"
            For Each y As Char In x
                If y = "B"c Then
                    Continue For
                End If
                System.Console.WriteLine(y)
        Next y, x
    End Sub
End Class

    ]]>.Value

            Dim expectedOperationTree = <![CDATA[
IForEachLoopStatement (Iteration variable: x As System.String) (LoopKind.ForEach) (OperationKind.LoopStatement)
  Collection: IConversionExpression (ConversionKind.Basic, Implicit) (OperationKind.ConversionExpression, Type: System.Collections.IEnumerable)
      ILocalReferenceExpression: S (OperationKind.LocalReferenceExpression, Type: System.String())
  IBlockStatement (1 statements) (OperationKind.BlockStatement)
    IForEachLoopStatement (Iteration variable: y As System.Char) (LoopKind.ForEach) (OperationKind.LoopStatement)
      Collection: ILocalReferenceExpression: x (OperationKind.LocalReferenceExpression, Type: System.String)
      IBlockStatement (2 statements) (OperationKind.BlockStatement)
        IIfStatement (OperationKind.IfStatement)
          Condition: IBinaryOperatorExpression (BinaryOperationKind.IntegerEquals) (OperationKind.BinaryOperatorExpression, Type: System.Boolean)
              Left: ILocalReferenceExpression: y (OperationKind.LocalReferenceExpression, Type: System.Char)
              Right: ILiteralExpression (OperationKind.LiteralExpression, Type: System.Char, Constant: B)
          IBlockStatement (1 statements) (OperationKind.BlockStatement)
            IBranchStatement (BranchKind.Continue, Label: continue) (OperationKind.BranchStatement)
        IExpressionStatement (OperationKind.ExpressionStatement)
          IInvocationExpression (static Sub System.Console.WriteLine(value As System.Char)) (OperationKind.InvocationExpression, Type: System.Void)
            IArgument (Matching Parameter: value) (OperationKind.Argument)
              ILocalReferenceExpression: y (OperationKind.LocalReferenceExpression, Type: System.Char)
]]>.Value

            VerifyOperationTreeForTest(Of ForEachBlockSyntax)(source, expectedOperationTree)
        End Sub

        <Fact(), WorkItem(17602, "https://github.com/dotnet/roslyn/issues/17602")>
        Public Sub IForEachLoopStatement_Nested()
            Dim source = <![CDATA[
Class C
    Shared Sub Main()
        Dim c(3)() As Integer
        For Each x As Integer() In c
            ReDim x(3)
            For i As Integer = 0 To 3
                x(i) = i
            Next
            For Each y As Integer In x'BIND:"For Each y As Integer In x"
                System.Console.WriteLine(y)
            Next
        Next
    End Sub
End Class

    ]]>.Value

            Dim expectedOperationTree = <![CDATA[
IForEachLoopStatement (Iteration variable: y As System.Int32) (LoopKind.ForEach) (OperationKind.LoopStatement)
  Collection: IConversionExpression (ConversionKind.Basic, Implicit) (OperationKind.ConversionExpression, Type: System.Collections.IEnumerable)
      ILocalReferenceExpression: x (OperationKind.LocalReferenceExpression, Type: System.Int32())
  IBlockStatement (1 statements) (OperationKind.BlockStatement)
    IExpressionStatement (OperationKind.ExpressionStatement)
      IInvocationExpression (static Sub System.Console.WriteLine(value As System.Int32)) (OperationKind.InvocationExpression, Type: System.Void)
        IArgument (Matching Parameter: value) (OperationKind.Argument)
          ILocalReferenceExpression: y (OperationKind.LocalReferenceExpression, Type: System.Int32)
]]>.Value

            VerifyOperationTreeForTest(Of ForEachBlockSyntax)(source, expectedOperationTree)
        End Sub

        <Fact(), WorkItem(17602, "https://github.com/dotnet/roslyn/issues/17602")>
        Public Sub IForEachLoopStatement_Nested1()
            Dim source = <![CDATA[
Class C
    Public Shared Sub Main()
        Dim S As String() = New String() {"ABC", "XYZ"}
        For Each x As String In S'BIND:"For Each x As String In S"
            For Each y As Char In x
                System.Console.WriteLine(y)
            Next
        Next
    End Sub
End Class

    ]]>.Value

            Dim expectedOperationTree = <![CDATA[
IForEachLoopStatement (Iteration variable: x As System.String) (LoopKind.ForEach) (OperationKind.LoopStatement)
  Collection: IConversionExpression (ConversionKind.Basic, Implicit) (OperationKind.ConversionExpression, Type: System.Collections.IEnumerable)
      ILocalReferenceExpression: S (OperationKind.LocalReferenceExpression, Type: System.String())
  IBlockStatement (1 statements) (OperationKind.BlockStatement)
    IForEachLoopStatement (Iteration variable: y As System.Char) (LoopKind.ForEach) (OperationKind.LoopStatement)
      Collection: ILocalReferenceExpression: x (OperationKind.LocalReferenceExpression, Type: System.String)
      IBlockStatement (1 statements) (OperationKind.BlockStatement)
        IExpressionStatement (OperationKind.ExpressionStatement)
          IInvocationExpression (static Sub System.Console.WriteLine(value As System.Char)) (OperationKind.InvocationExpression, Type: System.Void)
            IArgument (Matching Parameter: value) (OperationKind.Argument)
              ILocalReferenceExpression: y (OperationKind.LocalReferenceExpression, Type: System.Char)
]]>.Value

            VerifyOperationTreeForTest(Of ForEachBlockSyntax)(source, expectedOperationTree)
        End Sub

        <Fact(), WorkItem(17602, "https://github.com/dotnet/roslyn/issues/17602")>
        Public Sub IForEachLoopStatement_Interface()
            Dim source = <![CDATA[
Option Infer On

Class C
    Public Shared Sub Main()
        For Each x In New Enumerable()'BIND:"For Each x In New Enumerable()"
            System.Console.WriteLine(x)
        Next
    End Sub
End Class

Class Enumerable
    Implements System.Collections.IEnumerable
    Private Function System_Collections_IEnumerable_GetEnumerator() As System.Collections.IEnumerator Implements System.Collections.IEnumerable.GetEnumerator
        Dim list As New System.Collections.Generic.List(Of Integer)()
        list.Add(3)
        list.Add(2)
        list.Add(1)
        Return list.GetEnumerator()
    End Function
End Class
    ]]>.Value

            Dim expectedOperationTree = <![CDATA[
IForEachLoopStatement (Iteration variable: x As System.Object) (LoopKind.ForEach) (OperationKind.LoopStatement)
  Collection: IConversionExpression (ConversionKind.Basic, Implicit) (OperationKind.ConversionExpression, Type: System.Collections.IEnumerable)
      IObjectCreationExpression (Constructor: Sub Enumerable..ctor()) (OperationKind.ObjectCreationExpression, Type: Enumerable)
  IBlockStatement (1 statements) (OperationKind.BlockStatement)
    IExpressionStatement (OperationKind.ExpressionStatement)
      IInvocationExpression (static Sub System.Console.WriteLine(value As System.Object)) (OperationKind.InvocationExpression, Type: System.Void)
        IArgument (Matching Parameter: value) (OperationKind.Argument)
          ILocalReferenceExpression: x (OperationKind.LocalReferenceExpression, Type: System.Object)
]]>.Value

            VerifyOperationTreeForTest(Of ForEachBlockSyntax)(source, expectedOperationTree)
        End Sub

        <Fact(), WorkItem(17602, "https://github.com/dotnet/roslyn/issues/17602")>
        Public Sub IForEachLoopStatement_Struct()
            Dim source = <![CDATA[
Option Infer On

Imports System.Collections

Class C
    Public Shared Sub Main()
        For Each x In New Enumerable()'BIND:"For Each x In New Enumerable()"
            System.Console.WriteLine(x)
        Next
    End Sub
End Class

Structure Enumerable
    Public Function GetEnumerator() As IEnumerator
        Return New Integer() {1, 2, 3}.GetEnumerator()
    End Function
End Structure
    ]]>.Value

            Dim expectedOperationTree = <![CDATA[
IForEachLoopStatement (Iteration variable: x As System.Object) (LoopKind.ForEach) (OperationKind.LoopStatement)
  Collection: IObjectCreationExpression (Constructor: Sub Enumerable..ctor()) (OperationKind.ObjectCreationExpression, Type: Enumerable)
  IBlockStatement (1 statements) (OperationKind.BlockStatement)
    IExpressionStatement (OperationKind.ExpressionStatement)
      IInvocationExpression (static Sub System.Console.WriteLine(value As System.Object)) (OperationKind.InvocationExpression, Type: System.Void)
        IArgument (Matching Parameter: value) (OperationKind.Argument)
          ILocalReferenceExpression: x (OperationKind.LocalReferenceExpression, Type: System.Object)
]]>.Value

            VerifyOperationTreeForTest(Of ForEachBlockSyntax)(source, expectedOperationTree)
        End Sub

        <Fact(), WorkItem(17602, "https://github.com/dotnet/roslyn/issues/17602")>
        Public Sub IForEachLoopStatement_ConstantNull()
            Dim source = <![CDATA[
Option Infer On
Class Program
    Public Shared Sub Main()
        Const s As String = Nothing
        For Each y In TryCast(s, String)'BIND:"For Each y In TryCast(s, String)"
            System.Console.WriteLine(y)
        Next
    End Sub
End Class
    ]]>.Value

            Dim expectedOperationTree = <![CDATA[
IForEachLoopStatement (Iteration variable: y As System.Char) (LoopKind.ForEach) (OperationKind.LoopStatement)
  Collection: IConversionExpression (ConversionKind.TryCast, Explicit) (OperationKind.ConversionExpression, Type: System.String, Constant: null)
      ILocalReferenceExpression: s (OperationKind.LocalReferenceExpression, Type: System.String, Constant: null)
  IBlockStatement (1 statements) (OperationKind.BlockStatement)
    IExpressionStatement (OperationKind.ExpressionStatement)
      IInvocationExpression (static Sub System.Console.WriteLine(value As System.Char)) (OperationKind.InvocationExpression, Type: System.Void)
        IArgument (Matching Parameter: value) (OperationKind.Argument)
          ILocalReferenceExpression: y (OperationKind.LocalReferenceExpression, Type: System.Char)
]]>.Value

            VerifyOperationTreeForTest(Of ForEachBlockSyntax)(source, expectedOperationTree)
        End Sub

        <Fact(), WorkItem(17602, "https://github.com/dotnet/roslyn/issues/17602")>
        Public Sub IForEachLoopStatement_IterateStruct()
            Dim source = <![CDATA[
Option Infer On
Imports System.Collections
Class C
    Public Shared Sub Main()
        For Each x In New Enumerable()'BIND:"For Each x In New Enumerable()"
            System.Console.WriteLine(x)
        Next
    End Sub
End Class
Structure Enumerable
    Implements IEnumerable
    Private Function IEnumerable_GetEnumerator() As IEnumerator Implements IEnumerable.GetEnumerator
        Return New Integer() {1, 2, 3}.GetEnumerator()
    End Function
End Structure
    ]]>.Value

            Dim expectedOperationTree = <![CDATA[
IForEachLoopStatement (Iteration variable: x As System.Object) (LoopKind.ForEach) (OperationKind.LoopStatement)
  Collection: IConversionExpression (ConversionKind.Basic, Implicit) (OperationKind.ConversionExpression, Type: System.Collections.IEnumerable)
      IObjectCreationExpression (Constructor: Sub Enumerable..ctor()) (OperationKind.ObjectCreationExpression, Type: Enumerable)
  IBlockStatement (1 statements) (OperationKind.BlockStatement)
    IExpressionStatement (OperationKind.ExpressionStatement)
      IInvocationExpression (static Sub System.Console.WriteLine(value As System.Object)) (OperationKind.InvocationExpression, Type: System.Void)
        IArgument (Matching Parameter: value) (OperationKind.Argument)
          ILocalReferenceExpression: x (OperationKind.LocalReferenceExpression, Type: System.Object)
]]>.Value

            VerifyOperationTreeForTest(Of ForEachBlockSyntax)(source, expectedOperationTree)
        End Sub

        <Fact(), WorkItem(17602, "https://github.com/dotnet/roslyn/issues/17602")>
        Public Sub IForEachLoopStatement_QueryExpression()
            Dim source = <![CDATA[
Option Infer On
Imports System.Collections
Class C
    Public Shared Sub Main()
        For Each x In New Enumerable()'BIND:"For Each x In New Enumerable()"
            System.Console.WriteLine(x)
        Next
    End Sub
End Class
Structure Enumerable
    Implements IEnumerable
    Private Function IEnumerable_GetEnumerator() As IEnumerator Implements IEnumerable.GetEnumerator
        Return New Integer() {1, 2, 3}.GetEnumerator()
    End Function
End Structure
    ]]>.Value

            Dim expectedOperationTree = <![CDATA[
IForEachLoopStatement (Iteration variable: x As System.Object) (LoopKind.ForEach) (OperationKind.LoopStatement)
  Collection: IConversionExpression (ConversionKind.Basic, Implicit) (OperationKind.ConversionExpression, Type: System.Collections.IEnumerable)
      IObjectCreationExpression (Constructor: Sub Enumerable..ctor()) (OperationKind.ObjectCreationExpression, Type: Enumerable)
  IBlockStatement (1 statements) (OperationKind.BlockStatement)
    IExpressionStatement (OperationKind.ExpressionStatement)
      IInvocationExpression (static Sub System.Console.WriteLine(value As System.Object)) (OperationKind.InvocationExpression, Type: System.Void)
        IArgument (Matching Parameter: value) (OperationKind.Argument)
          ILocalReferenceExpression: x (OperationKind.LocalReferenceExpression, Type: System.Object)
]]>.Value

            VerifyOperationTreeForTest(Of ForEachBlockSyntax)(source, expectedOperationTree)
        End Sub

        <Fact(), WorkItem(17602, "https://github.com/dotnet/roslyn/issues/17602")>
        Public Sub IForEachLoopStatement_Multidimensional()
            Dim source = <![CDATA[
Option Strict On
Imports System

Module Program
    Sub Main()

        Dim k(,) = {{1}, {1}}
        For Each [Custom] In k'BIND:"For Each [Custom] In k"
            Console.Write(VerifyStaticType([Custom], GetType(Integer)))
            Console.Write(VerifyStaticType([Custom], GetType(Object)))
            Exit For
        Next
    End Sub

    Function VerifyStaticType(Of T)(ByVal x As T, ByVal y As System.Type) As Boolean
        Return GetType(T) Is y
    End Function
End Module

    ]]>.Value

            Dim expectedOperationTree = <![CDATA[
IForEachLoopStatement (Iteration variable: Custom As System.Int32) (LoopKind.ForEach) (OperationKind.LoopStatement)
  Collection: ILocalReferenceExpression: k (OperationKind.LocalReferenceExpression, Type: System.Int32(,))
  IBlockStatement (3 statements) (OperationKind.BlockStatement)
    IExpressionStatement (OperationKind.ExpressionStatement)
      IInvocationExpression (static Sub System.Console.Write(value As System.Boolean)) (OperationKind.InvocationExpression, Type: System.Void)
        IArgument (Matching Parameter: value) (OperationKind.Argument)
          IInvocationExpression (static Function Program.VerifyStaticType(Of System.Int32)(x As System.Int32, y As System.Type) As System.Boolean) (OperationKind.InvocationExpression, Type: System.Boolean)
            IArgument (Matching Parameter: x) (OperationKind.Argument)
              ILocalReferenceExpression: Custom (OperationKind.LocalReferenceExpression, Type: System.Int32)
            IArgument (Matching Parameter: y) (OperationKind.Argument)
              IOperation:  (OperationKind.None)
    IExpressionStatement (OperationKind.ExpressionStatement)
      IInvocationExpression (static Sub System.Console.Write(value As System.Boolean)) (OperationKind.InvocationExpression, Type: System.Void)
        IArgument (Matching Parameter: value) (OperationKind.Argument)
          IInvocationExpression (static Function Program.VerifyStaticType(Of System.Int32)(x As System.Int32, y As System.Type) As System.Boolean) (OperationKind.InvocationExpression, Type: System.Boolean)
            IArgument (Matching Parameter: x) (OperationKind.Argument)
              ILocalReferenceExpression: Custom (OperationKind.LocalReferenceExpression, Type: System.Int32)
            IArgument (Matching Parameter: y) (OperationKind.Argument)
              IOperation:  (OperationKind.None)
    IBranchStatement (BranchKind.Break, Label: exit) (OperationKind.BranchStatement)
]]>.Value

            VerifyOperationTreeForTest(Of ForEachBlockSyntax)(source, expectedOperationTree)
        End Sub

        <Fact(), WorkItem(17602, "https://github.com/dotnet/roslyn/issues/17602")>
        Public Sub IForEachLoopStatement_LateBinding()
            Dim source = <![CDATA[
Option Strict Off
Imports System
Class C
    Shared Sub Main()
        Dim o As Object = {1, 2, 3}
        For Each x In o'BIND:"For Each x In o"
            Console.WriteLine(x)
        Next
    End Sub
End Class

    ]]>.Value

            Dim expectedOperationTree = <![CDATA[
IForEachLoopStatement (Iteration variable: x As System.Object) (LoopKind.ForEach) (OperationKind.LoopStatement)
  Collection: IConversionExpression (ConversionKind.Basic, Implicit) (OperationKind.ConversionExpression, Type: System.Collections.IEnumerable)
      ILocalReferenceExpression: o (OperationKind.LocalReferenceExpression, Type: System.Object)
  IBlockStatement (1 statements) (OperationKind.BlockStatement)
    IExpressionStatement (OperationKind.ExpressionStatement)
      IInvocationExpression (static Sub System.Console.WriteLine(value As System.Object)) (OperationKind.InvocationExpression, Type: System.Void)
        IArgument (Matching Parameter: value) (OperationKind.Argument)
          ILocalReferenceExpression: x (OperationKind.LocalReferenceExpression, Type: System.Object)
]]>.Value

            VerifyOperationTreeForTest(Of ForEachBlockSyntax)(source, expectedOperationTree)
        End Sub

        <Fact(), WorkItem(17602, "https://github.com/dotnet/roslyn/issues/17602")>
        Public Sub IForEachLoopStatement_Pattern()
            Dim source = <![CDATA[
Option Infer On
Class C
    Public Shared Sub Main()
        For Each x In New Enumerable()'BIND:"For Each x In New Enumerable()"
            System.Console.WriteLine(x)
        Next
    End Sub
End Class

Class Enumerable
    Public Function GetEnumerator() As Enumerator
        Return New Enumerator()
    End Function
End Class

Class Enumerator
    Private x As Integer = 0
    Public ReadOnly Property Current() As Integer
        Get
            Return x
        End Get
    End Property
    Public Function MoveNext() As Boolean
        Return System.Threading.Interlocked.Increment(x) & lt; 4
    End Function
End Class
    ]]>.Value

            Dim expectedOperationTree = <![CDATA[
IForEachLoopStatement (Iteration variable: x As System.Int32) (LoopKind.ForEach) (OperationKind.LoopStatement)
  Collection: IObjectCreationExpression (Constructor: Sub Enumerable..ctor()) (OperationKind.ObjectCreationExpression, Type: Enumerable)
  IBlockStatement (1 statements) (OperationKind.BlockStatement)
    IExpressionStatement (OperationKind.ExpressionStatement)
      IInvocationExpression (static Sub System.Console.WriteLine(value As System.Int32)) (OperationKind.InvocationExpression, Type: System.Void)
        IArgument (Matching Parameter: value) (OperationKind.Argument)
          ILocalReferenceExpression: x (OperationKind.LocalReferenceExpression, Type: System.Int32)
]]>.Value

            VerifyOperationTreeForTest(Of ForEachBlockSyntax)(source, expectedOperationTree)
        End Sub

        <Fact(), WorkItem(17602, "https://github.com/dotnet/roslyn/issues/17602")>
        Public Sub IForEachLoopStatement_lamda()
            Dim source = <![CDATA[
Option Strict On
Option Infer On

Imports System

Class C1
    Private element_lambda_field As Integer

    Public Shared Sub Main()
        Dim c1 As New C1()
        c1.DoStuff()
    End Sub

    Public Sub DoStuff()
        Dim arr As Integer() = New Integer(1) {}
        arr(0) = 23
        arr(1) = 42

        Dim myDelegate As Action = Sub()
                                       Dim element_lambda_local As Integer
                                       For Each element_lambda_local In arr'BIND:"For Each element_lambda_local In arr"
                                           Console.WriteLine(element_lambda_local)
                                       Next element_lambda_local


                                   End Sub

        myDelegate.Invoke()
    End Sub
End Class

    ]]>.Value

            Dim expectedOperationTree = <![CDATA[
IForEachLoopStatement (Iteration variable: element_lambda_local As System.Int32) (LoopKind.ForEach) (OperationKind.LoopStatement)
  Collection: IConversionExpression (ConversionKind.Basic, Implicit) (OperationKind.ConversionExpression, Type: System.Collections.IEnumerable)
      ILocalReferenceExpression: arr (OperationKind.LocalReferenceExpression, Type: System.Int32())
  IBlockStatement (1 statements) (OperationKind.BlockStatement)
    IExpressionStatement (OperationKind.ExpressionStatement)
      IInvocationExpression (static Sub System.Console.WriteLine(value As System.Int32)) (OperationKind.InvocationExpression, Type: System.Void)
        IArgument (Matching Parameter: value) (OperationKind.Argument)
          ILocalReferenceExpression: element_lambda_local (OperationKind.LocalReferenceExpression, Type: System.Int32)
]]>.Value

            VerifyOperationTreeForTest(Of ForEachBlockSyntax)(source, expectedOperationTree)
        End Sub

        <Fact(), WorkItem(17602, "https://github.com/dotnet/roslyn/issues/17602")>
        Public Sub IForEachLoopStatement_InvalidConverstion()
            Dim source = <![CDATA[
Imports System

Class C1
    Public Shared Sub Main()
        For Each element As Integer In "Hello World."'BIND:"For Each element As Integer In "Hello World.""
            Console.WriteLine(element)
        Next
    End Sub
End Class
    ]]>.Value

            Dim expectedOperationTree = <![CDATA[
IForEachLoopStatement (Iteration variable: element As System.Int32) (LoopKind.ForEach) (OperationKind.LoopStatement)
  Collection: ILiteralExpression (OperationKind.LiteralExpression, Type: System.String, Constant: Hello World.)
  IBlockStatement (1 statements) (OperationKind.BlockStatement)
    IExpressionStatement (OperationKind.ExpressionStatement)
      IInvocationExpression (static Sub System.Console.WriteLine(value As System.Int32)) (OperationKind.InvocationExpression, Type: System.Void)
        IArgument (Matching Parameter: value) (OperationKind.Argument)
          ILocalReferenceExpression: element (OperationKind.LocalReferenceExpression, Type: System.Int32)
]]>.Value

            VerifyOperationTreeForTest(Of ForEachBlockSyntax)(source, expectedOperationTree)
        End Sub

        <Fact(), WorkItem(17602, "https://github.com/dotnet/roslyn/issues/17602")>
        Public Sub IForEachLoopStatement_Throw()
            Dim source = <![CDATA[
Imports System
Class C
    Shared Sub Main()
        Dim arr As String() = New String(1) {}
        arr(0) = "one"
        arr(1) = "two"
        For Each s As String In arr'BIND:"For Each s As String In arr"
            If (s = "one") Then
                Throw New Exception
            End If
            Console.WriteLine(s)
        Next
    End Sub
End Class
    ]]>.Value

            Dim expectedOperationTree = <![CDATA[
IForEachLoopStatement (Iteration variable: s As System.String) (LoopKind.ForEach) (OperationKind.LoopStatement)
  Collection: IConversionExpression (ConversionKind.Basic, Implicit) (OperationKind.ConversionExpression, Type: System.Collections.IEnumerable)
      ILocalReferenceExpression: arr (OperationKind.LocalReferenceExpression, Type: System.String())
  IBlockStatement (2 statements) (OperationKind.BlockStatement)
    IIfStatement (OperationKind.IfStatement)
      Condition: IParenthesizedExpression (OperationKind.ParenthesizedExpression, Type: System.Boolean)
          IBinaryOperatorExpression (BinaryOperationKind.StringEquals) (OperationKind.BinaryOperatorExpression, Type: System.Boolean)
            Left: ILocalReferenceExpression: s (OperationKind.LocalReferenceExpression, Type: System.String)
            Right: ILiteralExpression (OperationKind.LiteralExpression, Type: System.String, Constant: one)
      IBlockStatement (1 statements) (OperationKind.BlockStatement)
        IThrowStatement (OperationKind.ThrowStatement)
          IObjectCreationExpression (Constructor: Sub System.Exception..ctor()) (OperationKind.ObjectCreationExpression, Type: System.Exception)
    IExpressionStatement (OperationKind.ExpressionStatement)
      IInvocationExpression (static Sub System.Console.WriteLine(value As System.String)) (OperationKind.InvocationExpression, Type: System.Void)
        IArgument (Matching Parameter: value) (OperationKind.Argument)
          ILocalReferenceExpression: s (OperationKind.LocalReferenceExpression, Type: System.String)
]]>.Value

            VerifyOperationTreeForTest(Of ForEachBlockSyntax)(source, expectedOperationTree)
        End Sub

        <Fact(), WorkItem(17602, "https://github.com/dotnet/roslyn/issues/17602")>
        Public Sub IForEachLoopStatement_FuncCall()
            Dim source = <![CDATA[
Option Infer On

Class C1
    Public Function foo() As Integer()
        Return New Integer() {1, 2, 3}
    End Function
End Class

Module M
    Public Sub Main()

        Dim used1, used2 As Integer

        For Each used1 In New C1().foo()'BIND:"For Each used1 In New C1().foo()"
            used2 = 23
        Next used1
    End Sub
End Module
    ]]>.Value

            Dim expectedOperationTree = <![CDATA[
IForEachLoopStatement (Iteration variable: used1 As System.Int32) (LoopKind.ForEach) (OperationKind.LoopStatement)
  Collection: IConversionExpression (ConversionKind.Basic, Implicit) (OperationKind.ConversionExpression, Type: System.Collections.IEnumerable)
      IInvocationExpression ( Function C1.foo() As System.Int32()) (OperationKind.InvocationExpression, Type: System.Int32())
        Instance Receiver: IObjectCreationExpression (Constructor: Sub C1..ctor()) (OperationKind.ObjectCreationExpression, Type: C1)
  IBlockStatement (1 statements) (OperationKind.BlockStatement)
    IExpressionStatement (OperationKind.ExpressionStatement)
      IAssignmentExpression (OperationKind.AssignmentExpression, Type: System.Int32)
        Left: ILocalReferenceExpression: used2 (OperationKind.LocalReferenceExpression, Type: System.Int32)
        Right: ILiteralExpression (Text: 23) (OperationKind.LiteralExpression, Type: System.Int32, Constant: 23)
]]>.Value

            VerifyOperationTreeForTest(Of ForEachBlockSyntax)(source, expectedOperationTree)
        End Sub

        <Fact(), WorkItem(17602, "https://github.com/dotnet/roslyn/issues/17602")>
        Public Sub IForEachLoopStatement_WithReturn()
            Dim source = <![CDATA[
Class C
    Private F As Object
    Shared Function M(c As Object()) As Boolean
        For Each o In c'BIND:"For Each o In c"
            If o IsNot Nothing Then Return True
        Next
        Return False
    End Function
End Class
    ]]>.Value

            Dim expectedOperationTree = <![CDATA[
IForEachLoopStatement (Iteration variable: o As System.Object) (LoopKind.ForEach) (OperationKind.LoopStatement)
  Collection: IConversionExpression (ConversionKind.Basic, Implicit) (OperationKind.ConversionExpression, Type: System.Collections.IEnumerable)
      IParameterReferenceExpression: c (OperationKind.ParameterReferenceExpression, Type: System.Object())
  IBlockStatement (1 statements) (OperationKind.BlockStatement)
    IIfStatement (OperationKind.IfStatement)
      Condition: IBinaryOperatorExpression (BinaryOperationKind.ObjectNotEquals) (OperationKind.BinaryOperatorExpression, Type: System.Boolean)
          Left: ILocalReferenceExpression: o (OperationKind.LocalReferenceExpression, Type: System.Object)
          Right: IConversionExpression (ConversionKind.Basic, Implicit) (OperationKind.ConversionExpression, Type: System.Object, Constant: null)
              ILiteralExpression (OperationKind.LiteralExpression, Type: null, Constant: null)
      IBlockStatement (1 statements) (OperationKind.BlockStatement)
        IReturnStatement (OperationKind.ReturnStatement)
          ILiteralExpression (Text: True) (OperationKind.LiteralExpression, Type: System.Boolean, Constant: True)
]]>.Value

            VerifyOperationTreeForTest(Of ForEachBlockSyntax)(source, expectedOperationTree)
        End Sub

    End Class
End Namespace

