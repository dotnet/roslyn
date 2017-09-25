' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports Microsoft.CodeAnalysis.Semantics
Imports Microsoft.CodeAnalysis.Test.Utilities
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax
Imports Roslyn.Test.Utilities

Namespace Microsoft.CodeAnalysis.VisualBasic.UnitTests.Semantics

    Partial Public Class IOperationTests
        Inherits SemanticModelTestBase

        <CompilerTrait(CompilerFeature.IOperation)>
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
IForEachLoopStatement (LoopKind.ForEach) (OperationKind.LoopStatement, Language: Visual Basic) (Syntax: 'For Each s  ... Next')
  Locals: Local_1: s As System.String
  LoopControlVariable: 
    ILocalReferenceExpression: s (OperationKind.LocalReferenceExpression, Type: System.String, Language: Visual Basic) (Syntax: 's As String')
  Collection: 
    IConversionExpression (Implicit, TryCast: False, Unchecked) (OperationKind.ConversionExpression, Type: System.Collections.IEnumerable, Language: Visual Basic) (Syntax: 'arr')
      Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: True, IsUserDefined: False) (MethodSymbol: null)
      Operand: 
        ILocalReferenceExpression: arr (OperationKind.LocalReferenceExpression, Type: System.String(), Language: Visual Basic) (Syntax: 'arr')
  Body: 
    IBlockStatement (1 statements) (OperationKind.BlockStatement, Language: Visual Basic) (Syntax: 'For Each s  ... Next')
      IExpressionStatement (OperationKind.ExpressionStatement, Language: Visual Basic) (Syntax: 'Console.WriteLine(s)')
        Expression: 
          IInvocationExpression (Sub System.Console.WriteLine(value As System.String)) (OperationKind.InvocationExpression, Type: System.Void, Language: Visual Basic) (Syntax: 'Console.WriteLine(s)')
            Instance Receiver: 
            null
            Arguments(1):
                IArgument (ArgumentKind.Explicit, Matching Parameter: value) (OperationKind.Argument, Language: Visual Basic) (Syntax: 's')
                  ILocalReferenceExpression: s (OperationKind.LocalReferenceExpression, Type: System.String, Language: Visual Basic) (Syntax: 's')
                  InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                  OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
  NextVariables(0)
]]>.Value

            VerifyOperationTreeForTest(Of ForEachBlockSyntax)(source, expectedOperationTree)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation)>
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
IForEachLoopStatement (LoopKind.ForEach) (OperationKind.LoopStatement, Language: Visual Basic) (Syntax: 'For Each it ... Next')
  Locals: Local_1: item As System.String
  LoopControlVariable: 
    ILocalReferenceExpression: item (OperationKind.LocalReferenceExpression, Type: System.String, Language: Visual Basic) (Syntax: 'item As String')
  Collection: 
    ILocalReferenceExpression: list (OperationKind.LocalReferenceExpression, Type: System.Collections.Generic.List(Of System.String), Language: Visual Basic) (Syntax: 'list')
  Body: 
    IBlockStatement (1 statements) (OperationKind.BlockStatement, Language: Visual Basic) (Syntax: 'For Each it ... Next')
      IExpressionStatement (OperationKind.ExpressionStatement, Language: Visual Basic) (Syntax: 'System.Cons ... eLine(item)')
        Expression: 
          IInvocationExpression (Sub System.Console.WriteLine(value As System.String)) (OperationKind.InvocationExpression, Type: System.Void, Language: Visual Basic) (Syntax: 'System.Cons ... eLine(item)')
            Instance Receiver: 
            null
            Arguments(1):
                IArgument (ArgumentKind.Explicit, Matching Parameter: value) (OperationKind.Argument, Language: Visual Basic) (Syntax: 'item')
                  ILocalReferenceExpression: item (OperationKind.LocalReferenceExpression, Type: System.String, Language: Visual Basic) (Syntax: 'item')
                  InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                  OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
  NextVariables(0)
]]>.Value

            VerifyOperationTreeForTest(Of ForEachBlockSyntax)(source, expectedOperationTree)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation)>
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
IForEachLoopStatement (LoopKind.ForEach) (OperationKind.LoopStatement, Language: Visual Basic) (Syntax: 'For Each y  ... Next')
  Locals: Local_1: y As System.Char
  LoopControlVariable: 
    ILocalReferenceExpression: y (OperationKind.LocalReferenceExpression, Type: System.Char, Language: Visual Basic) (Syntax: 'y As Char')
  Collection: 
    ILocalReferenceExpression: x (OperationKind.LocalReferenceExpression, Type: System.String, Language: Visual Basic) (Syntax: 'x')
  Body: 
    IBlockStatement (1 statements) (OperationKind.BlockStatement, Language: Visual Basic) (Syntax: 'For Each y  ... Next')
      IIfStatement (OperationKind.IfStatement, Language: Visual Basic) (Syntax: 'If y = "B"c ... End If')
        Condition: 
          IBinaryOperatorExpression (BinaryOperatorKind.Equals, Checked) (OperationKind.BinaryOperatorExpression, Type: System.Boolean, Language: Visual Basic) (Syntax: 'y = "B"c')
            Left: 
              ILocalReferenceExpression: y (OperationKind.LocalReferenceExpression, Type: System.Char, Language: Visual Basic) (Syntax: 'y')
            Right: 
              ILiteralExpression (OperationKind.LiteralExpression, Type: System.Char, Constant: B, Language: Visual Basic) (Syntax: '"B"c')
        IfTrue: 
          IBlockStatement (1 statements) (OperationKind.BlockStatement, Language: Visual Basic) (Syntax: 'If y = "B"c ... End If')
            IBranchStatement (BranchKind.Break, Label: exit) (OperationKind.BranchStatement, Language: Visual Basic) (Syntax: 'Exit For')
        IfFalse: 
          IBlockStatement (1 statements) (OperationKind.BlockStatement, Language: Visual Basic) (Syntax: 'Else ... riteLine(y)')
            IExpressionStatement (OperationKind.ExpressionStatement, Language: Visual Basic) (Syntax: 'System.Cons ... riteLine(y)')
              Expression: 
                IInvocationExpression (Sub System.Console.WriteLine(value As System.Char)) (OperationKind.InvocationExpression, Type: System.Void, Language: Visual Basic) (Syntax: 'System.Cons ... riteLine(y)')
                  Instance Receiver: 
                  null
                  Arguments(1):
                      IArgument (ArgumentKind.Explicit, Matching Parameter: value) (OperationKind.Argument, Language: Visual Basic) (Syntax: 'y')
                        ILocalReferenceExpression: y (OperationKind.LocalReferenceExpression, Type: System.Char, Language: Visual Basic) (Syntax: 'y')
                        InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                        OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
  NextVariables(0)
]]>.Value

            VerifyOperationTreeForTest(Of ForEachBlockSyntax)(source, expectedOperationTree)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation)>
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
IForEachLoopStatement (LoopKind.ForEach) (OperationKind.LoopStatement, Language: Visual Basic) (Syntax: 'For Each x  ... Next y, x')
  Locals: Local_1: x As System.String
  LoopControlVariable: 
    ILocalReferenceExpression: x (OperationKind.LocalReferenceExpression, Type: System.String, Language: Visual Basic) (Syntax: 'x As String')
  Collection: 
    IConversionExpression (Implicit, TryCast: False, Unchecked) (OperationKind.ConversionExpression, Type: System.Collections.IEnumerable, Language: Visual Basic) (Syntax: 'S')
      Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: True, IsUserDefined: False) (MethodSymbol: null)
      Operand: 
        ILocalReferenceExpression: S (OperationKind.LocalReferenceExpression, Type: System.String(), Language: Visual Basic) (Syntax: 'S')
  Body: 
    IBlockStatement (1 statements) (OperationKind.BlockStatement, Language: Visual Basic) (Syntax: 'For Each x  ... Next y, x')
      IForEachLoopStatement (LoopKind.ForEach) (OperationKind.LoopStatement, Language: Visual Basic) (Syntax: 'For Each y  ... Next y, x')
        Locals: Local_1: y As System.Char
        LoopControlVariable: 
          ILocalReferenceExpression: y (OperationKind.LocalReferenceExpression, Type: System.Char, Language: Visual Basic) (Syntax: 'y As Char')
        Collection: 
          ILocalReferenceExpression: x (OperationKind.LocalReferenceExpression, Type: System.String, Language: Visual Basic) (Syntax: 'x')
        Body: 
          IBlockStatement (2 statements) (OperationKind.BlockStatement, Language: Visual Basic) (Syntax: 'For Each y  ... Next y, x')
            IIfStatement (OperationKind.IfStatement, Language: Visual Basic) (Syntax: 'If y = "B"c ... End If')
              Condition: 
                IBinaryOperatorExpression (BinaryOperatorKind.Equals, Checked) (OperationKind.BinaryOperatorExpression, Type: System.Boolean, Language: Visual Basic) (Syntax: 'y = "B"c')
                  Left: 
                    ILocalReferenceExpression: y (OperationKind.LocalReferenceExpression, Type: System.Char, Language: Visual Basic) (Syntax: 'y')
                  Right: 
                    ILiteralExpression (OperationKind.LiteralExpression, Type: System.Char, Constant: B, Language: Visual Basic) (Syntax: '"B"c')
              IfTrue: 
                IBlockStatement (1 statements) (OperationKind.BlockStatement, Language: Visual Basic) (Syntax: 'If y = "B"c ... End If')
                  IBranchStatement (BranchKind.Continue, Label: continue) (OperationKind.BranchStatement, Language: Visual Basic) (Syntax: 'Continue For')
              IfFalse: 
              null
            IExpressionStatement (OperationKind.ExpressionStatement, Language: Visual Basic) (Syntax: 'System.Cons ... riteLine(y)')
              Expression: 
                IInvocationExpression (Sub System.Console.WriteLine(value As System.Char)) (OperationKind.InvocationExpression, Type: System.Void, Language: Visual Basic) (Syntax: 'System.Cons ... riteLine(y)')
                  Instance Receiver: 
                  null
                  Arguments(1):
                      IArgument (ArgumentKind.Explicit, Matching Parameter: value) (OperationKind.Argument, Language: Visual Basic) (Syntax: 'y')
                        ILocalReferenceExpression: y (OperationKind.LocalReferenceExpression, Type: System.Char, Language: Visual Basic) (Syntax: 'y')
                        InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                        OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
        NextVariables(2):
            ILocalReferenceExpression: y (OperationKind.LocalReferenceExpression, Type: System.Char, Language: Visual Basic) (Syntax: 'y')
            ILocalReferenceExpression: x (OperationKind.LocalReferenceExpression, Type: System.String, Language: Visual Basic) (Syntax: 'x')
  NextVariables(0)
]]>.Value

            VerifyOperationTreeForTest(Of ForEachBlockSyntax)(source, expectedOperationTree)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation)>
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
IForEachLoopStatement (LoopKind.ForEach) (OperationKind.LoopStatement, Language: Visual Basic) (Syntax: 'For Each y  ... Next')
  Locals: Local_1: y As System.Int32
  LoopControlVariable: 
    ILocalReferenceExpression: y (OperationKind.LocalReferenceExpression, Type: System.Int32, Language: Visual Basic) (Syntax: 'y As Integer')
  Collection: 
    IConversionExpression (Implicit, TryCast: False, Unchecked) (OperationKind.ConversionExpression, Type: System.Collections.IEnumerable, Language: Visual Basic) (Syntax: 'x')
      Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: True, IsUserDefined: False) (MethodSymbol: null)
      Operand: 
        ILocalReferenceExpression: x (OperationKind.LocalReferenceExpression, Type: System.Int32(), Language: Visual Basic) (Syntax: 'x')
  Body: 
    IBlockStatement (1 statements) (OperationKind.BlockStatement, Language: Visual Basic) (Syntax: 'For Each y  ... Next')
      IExpressionStatement (OperationKind.ExpressionStatement, Language: Visual Basic) (Syntax: 'System.Cons ... riteLine(y)')
        Expression: 
          IInvocationExpression (Sub System.Console.WriteLine(value As System.Int32)) (OperationKind.InvocationExpression, Type: System.Void, Language: Visual Basic) (Syntax: 'System.Cons ... riteLine(y)')
            Instance Receiver: 
            null
            Arguments(1):
                IArgument (ArgumentKind.Explicit, Matching Parameter: value) (OperationKind.Argument, Language: Visual Basic) (Syntax: 'y')
                  ILocalReferenceExpression: y (OperationKind.LocalReferenceExpression, Type: System.Int32, Language: Visual Basic) (Syntax: 'y')
                  InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                  OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
  NextVariables(0)
]]>.Value

            VerifyOperationTreeForTest(Of ForEachBlockSyntax)(source, expectedOperationTree)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation)>
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
IForEachLoopStatement (LoopKind.ForEach) (OperationKind.LoopStatement, Language: Visual Basic) (Syntax: 'For Each x  ... Next')
  Locals: Local_1: x As System.String
  LoopControlVariable: 
    ILocalReferenceExpression: x (OperationKind.LocalReferenceExpression, Type: System.String, Language: Visual Basic) (Syntax: 'x As String')
  Collection: 
    IConversionExpression (Implicit, TryCast: False, Unchecked) (OperationKind.ConversionExpression, Type: System.Collections.IEnumerable, Language: Visual Basic) (Syntax: 'S')
      Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: True, IsUserDefined: False) (MethodSymbol: null)
      Operand: 
        ILocalReferenceExpression: S (OperationKind.LocalReferenceExpression, Type: System.String(), Language: Visual Basic) (Syntax: 'S')
  Body: 
    IBlockStatement (1 statements) (OperationKind.BlockStatement, Language: Visual Basic) (Syntax: 'For Each x  ... Next')
      IForEachLoopStatement (LoopKind.ForEach) (OperationKind.LoopStatement, Language: Visual Basic) (Syntax: 'For Each y  ... Next')
        Locals: Local_1: y As System.Char
        LoopControlVariable: 
          ILocalReferenceExpression: y (OperationKind.LocalReferenceExpression, Type: System.Char, Language: Visual Basic) (Syntax: 'y As Char')
        Collection: 
          ILocalReferenceExpression: x (OperationKind.LocalReferenceExpression, Type: System.String, Language: Visual Basic) (Syntax: 'x')
        Body: 
          IBlockStatement (1 statements) (OperationKind.BlockStatement, Language: Visual Basic) (Syntax: 'For Each y  ... Next')
            IExpressionStatement (OperationKind.ExpressionStatement, Language: Visual Basic) (Syntax: 'System.Cons ... riteLine(y)')
              Expression: 
                IInvocationExpression (Sub System.Console.WriteLine(value As System.Char)) (OperationKind.InvocationExpression, Type: System.Void, Language: Visual Basic) (Syntax: 'System.Cons ... riteLine(y)')
                  Instance Receiver: 
                  null
                  Arguments(1):
                      IArgument (ArgumentKind.Explicit, Matching Parameter: value) (OperationKind.Argument, Language: Visual Basic) (Syntax: 'y')
                        ILocalReferenceExpression: y (OperationKind.LocalReferenceExpression, Type: System.Char, Language: Visual Basic) (Syntax: 'y')
                        InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                        OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
        NextVariables(0)
  NextVariables(0)
]]>.Value

            VerifyOperationTreeForTest(Of ForEachBlockSyntax)(source, expectedOperationTree)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation)>
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
IForEachLoopStatement (LoopKind.ForEach) (OperationKind.LoopStatement, Language: Visual Basic) (Syntax: 'For Each x  ... Next')
  Locals: Local_1: x As System.Object
  LoopControlVariable: 
    ILocalReferenceExpression: x (OperationKind.LocalReferenceExpression, Type: System.Object, Language: Visual Basic) (Syntax: 'x')
  Collection: 
    IConversionExpression (Implicit, TryCast: False, Unchecked) (OperationKind.ConversionExpression, Type: System.Collections.IEnumerable, Language: Visual Basic) (Syntax: 'New Enumerable()')
      Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: True, IsUserDefined: False) (MethodSymbol: null)
      Operand: 
        IObjectCreationExpression (Constructor: Sub Enumerable..ctor()) (OperationKind.ObjectCreationExpression, Type: Enumerable, Language: Visual Basic) (Syntax: 'New Enumerable()')
          Arguments(0)
          Initializer: 
          null
  Body: 
    IBlockStatement (1 statements) (OperationKind.BlockStatement, Language: Visual Basic) (Syntax: 'For Each x  ... Next')
      IExpressionStatement (OperationKind.ExpressionStatement, Language: Visual Basic) (Syntax: 'System.Cons ... riteLine(x)')
        Expression: 
          IInvocationExpression (Sub System.Console.WriteLine(value As System.Object)) (OperationKind.InvocationExpression, Type: System.Void, Language: Visual Basic) (Syntax: 'System.Cons ... riteLine(x)')
            Instance Receiver: 
            null
            Arguments(1):
                IArgument (ArgumentKind.Explicit, Matching Parameter: value) (OperationKind.Argument, Language: Visual Basic) (Syntax: 'x')
                  ILocalReferenceExpression: x (OperationKind.LocalReferenceExpression, Type: System.Object, Language: Visual Basic) (Syntax: 'x')
                  InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                  OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
  NextVariables(0)
]]>.Value

            VerifyOperationTreeForTest(Of ForEachBlockSyntax)(source, expectedOperationTree)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation)>
        <Fact(), WorkItem(17602, "https://github.com/dotnet/roslyn/issues/17602")>
        Public Sub IForEachLoopStatement_String()
            Dim source = <![CDATA[
Option Infer On
Class Program
    Public Shared Sub Main()
        Const s As String = Nothing
        For Each y In s'BIND:"For Each y In s"
            System.Console.WriteLine(y)
        Next
    End Sub
End Class
    ]]>.Value

            Dim expectedOperationTree = <![CDATA[
IForEachLoopStatement (LoopKind.ForEach) (OperationKind.LoopStatement, Language: Visual Basic) (Syntax: 'For Each y  ... Next')
  Locals: Local_1: y As System.Char
  LoopControlVariable: 
    ILocalReferenceExpression: y (OperationKind.LocalReferenceExpression, Type: System.Char, Language: Visual Basic) (Syntax: 'y')
  Collection: 
    ILocalReferenceExpression: s (OperationKind.LocalReferenceExpression, Type: System.String, Constant: null, Language: Visual Basic) (Syntax: 's')
  Body: 
    IBlockStatement (1 statements) (OperationKind.BlockStatement, Language: Visual Basic) (Syntax: 'For Each y  ... Next')
      IExpressionStatement (OperationKind.ExpressionStatement, Language: Visual Basic) (Syntax: 'System.Cons ... riteLine(y)')
        Expression: 
          IInvocationExpression (Sub System.Console.WriteLine(value As System.Char)) (OperationKind.InvocationExpression, Type: System.Void, Language: Visual Basic) (Syntax: 'System.Cons ... riteLine(y)')
            Instance Receiver: 
            null
            Arguments(1):
                IArgument (ArgumentKind.Explicit, Matching Parameter: value) (OperationKind.Argument, Language: Visual Basic) (Syntax: 'y')
                  ILocalReferenceExpression: y (OperationKind.LocalReferenceExpression, Type: System.Char, Language: Visual Basic) (Syntax: 'y')
                  InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                  OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
  NextVariables(0)
]]>.Value

            VerifyOperationTreeForTest(Of ForEachBlockSyntax)(source, expectedOperationTree)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation)>
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
IForEachLoopStatement (LoopKind.ForEach) (OperationKind.LoopStatement, Language: Visual Basic) (Syntax: 'For Each x  ... Next')
  Locals: Local_1: x As System.Object
  LoopControlVariable: 
    ILocalReferenceExpression: x (OperationKind.LocalReferenceExpression, Type: System.Object, Language: Visual Basic) (Syntax: 'x')
  Collection: 
    IConversionExpression (Implicit, TryCast: False, Unchecked) (OperationKind.ConversionExpression, Type: System.Collections.IEnumerable, Language: Visual Basic) (Syntax: 'New Enumerable()')
      Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
      Operand: 
        IObjectCreationExpression (Constructor: Sub Enumerable..ctor()) (OperationKind.ObjectCreationExpression, Type: Enumerable, Language: Visual Basic) (Syntax: 'New Enumerable()')
          Arguments(0)
          Initializer: 
          null
  Body: 
    IBlockStatement (1 statements) (OperationKind.BlockStatement, Language: Visual Basic) (Syntax: 'For Each x  ... Next')
      IExpressionStatement (OperationKind.ExpressionStatement, Language: Visual Basic) (Syntax: 'System.Cons ... riteLine(x)')
        Expression: 
          IInvocationExpression (Sub System.Console.WriteLine(value As System.Object)) (OperationKind.InvocationExpression, Type: System.Void, Language: Visual Basic) (Syntax: 'System.Cons ... riteLine(x)')
            Instance Receiver: 
            null
            Arguments(1):
                IArgument (ArgumentKind.Explicit, Matching Parameter: value) (OperationKind.Argument, Language: Visual Basic) (Syntax: 'x')
                  ILocalReferenceExpression: x (OperationKind.LocalReferenceExpression, Type: System.Object, Language: Visual Basic) (Syntax: 'x')
                  InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                  OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
  NextVariables(0)
]]>.Value

            VerifyOperationTreeForTest(Of ForEachBlockSyntax)(source, expectedOperationTree)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation)>
        <Fact(), WorkItem(17602, "https://github.com/dotnet/roslyn/issues/17602")>
        Public Sub IForEachLoopStatement_QueryExpression()
            Dim source = <![CDATA[
Imports System
Imports System.Collections.Generic
Imports System.Diagnostics
Imports System.Linq

Module Program
    Sub Main(args As String())
        ' Obtain a list of customers.
        Dim customers As List(Of Customer) = GetCustomers()

        ' Return customers that are grouped based on country.
        Dim countries = From cust In customers
                        Order By cust.Country, cust.City
                        Group By CountryName = cust.Country
                        Into CustomersInCountry = Group, Count()
                        Order By CountryName

        ' Output the results.
        For Each country In countries'BIND:"For Each country In countries"
            Debug.WriteLine(country.CountryName & " count=" & country.Count)

            For Each customer In country.CustomersInCountry
                Debug.WriteLine("   " & customer.CompanyName & "  " & customer.City)
            Next
        Next
    End Sub

    Private Function GetCustomers() As List(Of Customer)
        Return New List(Of Customer) From
            {
                New Customer With {.CustomerID = 1, .CompanyName = "C", .City = "H", .Country = "C"},
                New Customer With {.CustomerID = 2, .CompanyName = "M", .City = "R", .Country = "U"},
                New Customer With {.CustomerID = 3, .CompanyName = "F", .City = "V", .Country = "C"}
            }
    End Function
End Module

Class Customer
    Public Property CustomerID As Integer
    Public Property CompanyName As String
    Public Property City As String
    Public Property Country As String
End Class

]]>.Value

            Dim expectedOperationTree = <![CDATA[
IForEachLoopStatement (LoopKind.ForEach) (OperationKind.LoopStatement, Language: Visual Basic) (Syntax: 'For Each co ... Next')
  Locals: Local_1: country As <anonymous type: Key CountryName As System.String, Key CustomersInCountry As System.Collections.Generic.IEnumerable(Of Customer), Key Count As System.Int32>
  LoopControlVariable: 
    ILocalReferenceExpression: country (OperationKind.LocalReferenceExpression, Type: <anonymous type: Key CountryName As System.String, Key CustomersInCountry As System.Collections.Generic.IEnumerable(Of Customer), Key Count As System.Int32>, Language: Visual Basic) (Syntax: 'country')
  Collection: 
    ILocalReferenceExpression: countries (OperationKind.LocalReferenceExpression, Type: System.Linq.IOrderedEnumerable(Of <anonymous type: Key CountryName As System.String, Key CustomersInCountry As System.Collections.Generic.IEnumerable(Of Customer), Key Count As System.Int32>), Language: Visual Basic) (Syntax: 'countries')
  Body: 
    IBlockStatement (2 statements) (OperationKind.BlockStatement, Language: Visual Basic) (Syntax: 'For Each co ... Next')
      IExpressionStatement (OperationKind.ExpressionStatement, Language: Visual Basic) (Syntax: 'Debug.Write ... ntry.Count)')
        Expression: 
          IInvocationExpression (Sub System.Diagnostics.Debug.WriteLine(message As System.String)) (OperationKind.InvocationExpression, Type: System.Void, Language: Visual Basic) (Syntax: 'Debug.Write ... ntry.Count)')
            Instance Receiver: 
            null
            Arguments(1):
                IArgument (ArgumentKind.Explicit, Matching Parameter: message) (OperationKind.Argument, Language: Visual Basic) (Syntax: 'country.Cou ... untry.Count')
                  IBinaryOperatorExpression (BinaryOperatorKind.Concatenate, Checked) (OperationKind.BinaryOperatorExpression, Type: System.String, Language: Visual Basic) (Syntax: 'country.Cou ... untry.Count')
                    Left: 
                      IBinaryOperatorExpression (BinaryOperatorKind.Concatenate, Checked) (OperationKind.BinaryOperatorExpression, Type: System.String, Language: Visual Basic) (Syntax: 'country.Cou ... & " count="')
                        Left: 
                          IPropertyReferenceExpression: ReadOnly Property <anonymous type: Key CountryName As System.String, Key CustomersInCountry As System.Collections.Generic.IEnumerable(Of Customer), Key Count As System.Int32>.CountryName As System.String (OperationKind.PropertyReferenceExpression, Type: System.String, Language: Visual Basic) (Syntax: 'country.CountryName')
                            Instance Receiver: 
                              ILocalReferenceExpression: country (OperationKind.LocalReferenceExpression, Type: <anonymous type: Key CountryName As System.String, Key CustomersInCountry As System.Collections.Generic.IEnumerable(Of Customer), Key Count As System.Int32>, Language: Visual Basic) (Syntax: 'country')
                        Right: 
                          ILiteralExpression (OperationKind.LiteralExpression, Type: System.String, Constant: " count=", Language: Visual Basic) (Syntax: '" count="')
                    Right: 
                      IConversionExpression (Explicit, TryCast: False, Unchecked) (OperationKind.ConversionExpression, Type: System.String, Language: Visual Basic) (Syntax: 'country.Count')
                        Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                        Operand: 
                          IPropertyReferenceExpression: ReadOnly Property <anonymous type: Key CountryName As System.String, Key CustomersInCountry As System.Collections.Generic.IEnumerable(Of Customer), Key Count As System.Int32>.Count As System.Int32 (OperationKind.PropertyReferenceExpression, Type: System.Int32, Language: Visual Basic) (Syntax: 'country.Count')
                            Instance Receiver: 
                              ILocalReferenceExpression: country (OperationKind.LocalReferenceExpression, Type: <anonymous type: Key CountryName As System.String, Key CustomersInCountry As System.Collections.Generic.IEnumerable(Of Customer), Key Count As System.Int32>, Language: Visual Basic) (Syntax: 'country')
                  InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                  OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
      IForEachLoopStatement (LoopKind.ForEach) (OperationKind.LoopStatement, Language: Visual Basic) (Syntax: 'For Each cu ... Next')
        Locals: Local_1: customer As Customer
        LoopControlVariable: 
          ILocalReferenceExpression: customer (OperationKind.LocalReferenceExpression, Type: Customer, Language: Visual Basic) (Syntax: 'customer')
        Collection: 
          IPropertyReferenceExpression: ReadOnly Property <anonymous type: Key CountryName As System.String, Key CustomersInCountry As System.Collections.Generic.IEnumerable(Of Customer), Key Count As System.Int32>.CustomersInCountry As System.Collections.Generic.IEnumerable(Of Customer) (OperationKind.PropertyReferenceExpression, Type: System.Collections.Generic.IEnumerable(Of Customer), Language: Visual Basic) (Syntax: 'country.Cus ... rsInCountry')
            Instance Receiver: 
              ILocalReferenceExpression: country (OperationKind.LocalReferenceExpression, Type: <anonymous type: Key CountryName As System.String, Key CustomersInCountry As System.Collections.Generic.IEnumerable(Of Customer), Key Count As System.Int32>, Language: Visual Basic) (Syntax: 'country')
        Body: 
          IBlockStatement (1 statements) (OperationKind.BlockStatement, Language: Visual Basic) (Syntax: 'For Each cu ... Next')
            IExpressionStatement (OperationKind.ExpressionStatement, Language: Visual Basic) (Syntax: 'Debug.Write ... tomer.City)')
              Expression: 
                IInvocationExpression (Sub System.Diagnostics.Debug.WriteLine(message As System.String)) (OperationKind.InvocationExpression, Type: System.Void, Language: Visual Basic) (Syntax: 'Debug.Write ... tomer.City)')
                  Instance Receiver: 
                  null
                  Arguments(1):
                      IArgument (ArgumentKind.Explicit, Matching Parameter: message) (OperationKind.Argument, Language: Visual Basic) (Syntax: '"   " & cus ... stomer.City')
                        IBinaryOperatorExpression (BinaryOperatorKind.Concatenate, Checked) (OperationKind.BinaryOperatorExpression, Type: System.String, Language: Visual Basic) (Syntax: '"   " & cus ... stomer.City')
                          Left: 
                            IBinaryOperatorExpression (BinaryOperatorKind.Concatenate, Checked) (OperationKind.BinaryOperatorExpression, Type: System.String, Language: Visual Basic) (Syntax: '"   " & cus ... Name & "  "')
                              Left: 
                                IBinaryOperatorExpression (BinaryOperatorKind.Concatenate, Checked) (OperationKind.BinaryOperatorExpression, Type: System.String, Language: Visual Basic) (Syntax: '"   " & cus ... CompanyName')
                                  Left: 
                                    ILiteralExpression (OperationKind.LiteralExpression, Type: System.String, Constant: "   ", Language: Visual Basic) (Syntax: '"   "')
                                  Right: 
                                    IPropertyReferenceExpression: Property Customer.CompanyName As System.String (OperationKind.PropertyReferenceExpression, Type: System.String, Language: Visual Basic) (Syntax: 'customer.CompanyName')
                                      Instance Receiver: 
                                        ILocalReferenceExpression: customer (OperationKind.LocalReferenceExpression, Type: Customer, Language: Visual Basic) (Syntax: 'customer')
                              Right: 
                                ILiteralExpression (OperationKind.LiteralExpression, Type: System.String, Constant: "  ", Language: Visual Basic) (Syntax: '"  "')
                          Right: 
                            IPropertyReferenceExpression: Property Customer.City As System.String (OperationKind.PropertyReferenceExpression, Type: System.String, Language: Visual Basic) (Syntax: 'customer.City')
                              Instance Receiver: 
                                ILocalReferenceExpression: customer (OperationKind.LocalReferenceExpression, Type: Customer, Language: Visual Basic) (Syntax: 'customer')
                        InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                        OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
        NextVariables(0)
  NextVariables(0)
]]>.Value

            Dim expectedDiagnostics = String.Empty

            VerifyOperationTreeAndDiagnosticsForTest(Of ForEachBlockSyntax)(source, expectedOperationTree, expectedDiagnostics)
        End Sub



        <CompilerTrait(CompilerFeature.IOperation)>
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
IForEachLoopStatement (LoopKind.ForEach) (OperationKind.LoopStatement, Language: Visual Basic) (Syntax: 'For Each [C ... Next')
  Locals: Local_1: Custom As System.Int32
  LoopControlVariable: 
    ILocalReferenceExpression: Custom (OperationKind.LocalReferenceExpression, Type: System.Int32, Language: Visual Basic) (Syntax: '[Custom]')
  Collection: 
    ILocalReferenceExpression: k (OperationKind.LocalReferenceExpression, Type: System.Int32(,), Language: Visual Basic) (Syntax: 'k')
  Body: 
    IBlockStatement (3 statements) (OperationKind.BlockStatement, Language: Visual Basic) (Syntax: 'For Each [C ... Next')
      IExpressionStatement (OperationKind.ExpressionStatement, Language: Visual Basic) (Syntax: 'Console.Wri ... (Integer)))')
        Expression: 
          IInvocationExpression (Sub System.Console.Write(value As System.Boolean)) (OperationKind.InvocationExpression, Type: System.Void, Language: Visual Basic) (Syntax: 'Console.Wri ... (Integer)))')
            Instance Receiver: 
            null
            Arguments(1):
                IArgument (ArgumentKind.Explicit, Matching Parameter: value) (OperationKind.Argument, Language: Visual Basic) (Syntax: 'VerifyStati ... e(Integer))')
                  IInvocationExpression (Function Program.VerifyStaticType(Of System.Int32)(x As System.Int32, y As System.Type) As System.Boolean) (OperationKind.InvocationExpression, Type: System.Boolean, Language: Visual Basic) (Syntax: 'VerifyStati ... e(Integer))')
                    Instance Receiver: 
                    null
                    Arguments(2):
                        IArgument (ArgumentKind.Explicit, Matching Parameter: x) (OperationKind.Argument, Language: Visual Basic) (Syntax: '[Custom]')
                          ILocalReferenceExpression: Custom (OperationKind.LocalReferenceExpression, Type: System.Int32, Language: Visual Basic) (Syntax: '[Custom]')
                          InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                          OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                        IArgument (ArgumentKind.Explicit, Matching Parameter: y) (OperationKind.Argument, Language: Visual Basic) (Syntax: 'GetType(Integer)')
                          ITypeOfExpression (OperationKind.TypeOfExpression, Type: System.Type, Language: Visual Basic) (Syntax: 'GetType(Integer)')
                            TypeOperand: System.Int32
                          InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                          OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                  InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                  OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
      IExpressionStatement (OperationKind.ExpressionStatement, Language: Visual Basic) (Syntax: 'Console.Wri ... e(Object)))')
        Expression: 
          IInvocationExpression (Sub System.Console.Write(value As System.Boolean)) (OperationKind.InvocationExpression, Type: System.Void, Language: Visual Basic) (Syntax: 'Console.Wri ... e(Object)))')
            Instance Receiver: 
            null
            Arguments(1):
                IArgument (ArgumentKind.Explicit, Matching Parameter: value) (OperationKind.Argument, Language: Visual Basic) (Syntax: 'VerifyStati ... pe(Object))')
                  IInvocationExpression (Function Program.VerifyStaticType(Of System.Int32)(x As System.Int32, y As System.Type) As System.Boolean) (OperationKind.InvocationExpression, Type: System.Boolean, Language: Visual Basic) (Syntax: 'VerifyStati ... pe(Object))')
                    Instance Receiver: 
                    null
                    Arguments(2):
                        IArgument (ArgumentKind.Explicit, Matching Parameter: x) (OperationKind.Argument, Language: Visual Basic) (Syntax: '[Custom]')
                          ILocalReferenceExpression: Custom (OperationKind.LocalReferenceExpression, Type: System.Int32, Language: Visual Basic) (Syntax: '[Custom]')
                          InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                          OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                        IArgument (ArgumentKind.Explicit, Matching Parameter: y) (OperationKind.Argument, Language: Visual Basic) (Syntax: 'GetType(Object)')
                          ITypeOfExpression (OperationKind.TypeOfExpression, Type: System.Type, Language: Visual Basic) (Syntax: 'GetType(Object)')
                            TypeOperand: System.Object
                          InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                          OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                  InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                  OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
      IBranchStatement (BranchKind.Break, Label: exit) (OperationKind.BranchStatement, Language: Visual Basic) (Syntax: 'Exit For')
  NextVariables(0)
]]>.Value

            VerifyOperationTreeForTest(Of ForEachBlockSyntax)(source, expectedOperationTree)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation)>
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
IForEachLoopStatement (LoopKind.ForEach) (OperationKind.LoopStatement, Language: Visual Basic) (Syntax: 'For Each x  ... Next')
  Locals: Local_1: x As System.Object
  LoopControlVariable: 
    ILocalReferenceExpression: x (OperationKind.LocalReferenceExpression, Type: System.Object, Language: Visual Basic) (Syntax: 'x')
  Collection: 
    IConversionExpression (Implicit, TryCast: False, Unchecked) (OperationKind.ConversionExpression, Type: System.Collections.IEnumerable, Language: Visual Basic) (Syntax: 'o')
      Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: True, IsUserDefined: False) (MethodSymbol: null)
      Operand: 
        ILocalReferenceExpression: o (OperationKind.LocalReferenceExpression, Type: System.Object, Language: Visual Basic) (Syntax: 'o')
  Body: 
    IBlockStatement (1 statements) (OperationKind.BlockStatement, Language: Visual Basic) (Syntax: 'For Each x  ... Next')
      IExpressionStatement (OperationKind.ExpressionStatement, Language: Visual Basic) (Syntax: 'Console.WriteLine(x)')
        Expression: 
          IInvocationExpression (Sub System.Console.WriteLine(value As System.Object)) (OperationKind.InvocationExpression, Type: System.Void, Language: Visual Basic) (Syntax: 'Console.WriteLine(x)')
            Instance Receiver: 
            null
            Arguments(1):
                IArgument (ArgumentKind.Explicit, Matching Parameter: value) (OperationKind.Argument, Language: Visual Basic) (Syntax: 'x')
                  ILocalReferenceExpression: x (OperationKind.LocalReferenceExpression, Type: System.Object, Language: Visual Basic) (Syntax: 'x')
                  InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                  OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
  NextVariables(0)
]]>.Value

            VerifyOperationTreeForTest(Of ForEachBlockSyntax)(source, expectedOperationTree)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation)>
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
IForEachLoopStatement (LoopKind.ForEach) (OperationKind.LoopStatement, Language: Visual Basic) (Syntax: 'For Each x  ... Next')
  Locals: Local_1: x As System.Int32
  LoopControlVariable: 
    ILocalReferenceExpression: x (OperationKind.LocalReferenceExpression, Type: System.Int32, Language: Visual Basic) (Syntax: 'x')
  Collection: 
    IObjectCreationExpression (Constructor: Sub Enumerable..ctor()) (OperationKind.ObjectCreationExpression, Type: Enumerable, Language: Visual Basic) (Syntax: 'New Enumerable()')
      Arguments(0)
      Initializer: 
      null
  Body: 
    IBlockStatement (1 statements) (OperationKind.BlockStatement, Language: Visual Basic) (Syntax: 'For Each x  ... Next')
      IExpressionStatement (OperationKind.ExpressionStatement, Language: Visual Basic) (Syntax: 'System.Cons ... riteLine(x)')
        Expression: 
          IInvocationExpression (Sub System.Console.WriteLine(value As System.Int32)) (OperationKind.InvocationExpression, Type: System.Void, Language: Visual Basic) (Syntax: 'System.Cons ... riteLine(x)')
            Instance Receiver: 
            null
            Arguments(1):
                IArgument (ArgumentKind.Explicit, Matching Parameter: value) (OperationKind.Argument, Language: Visual Basic) (Syntax: 'x')
                  ILocalReferenceExpression: x (OperationKind.LocalReferenceExpression, Type: System.Int32, Language: Visual Basic) (Syntax: 'x')
                  InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                  OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
  NextVariables(0)
]]>.Value

            VerifyOperationTreeForTest(Of ForEachBlockSyntax)(source, expectedOperationTree)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation)>
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
IForEachLoopStatement (LoopKind.ForEach) (OperationKind.LoopStatement, Language: Visual Basic) (Syntax: 'For Each el ... ambda_local')
  LoopControlVariable: 
    ILocalReferenceExpression: element_lambda_local (OperationKind.LocalReferenceExpression, Type: System.Int32, Language: Visual Basic) (Syntax: 'element_lambda_local')
  Collection: 
    IConversionExpression (Implicit, TryCast: False, Unchecked) (OperationKind.ConversionExpression, Type: System.Collections.IEnumerable, Language: Visual Basic) (Syntax: 'arr')
      Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: True, IsUserDefined: False) (MethodSymbol: null)
      Operand: 
        ILocalReferenceExpression: arr (OperationKind.LocalReferenceExpression, Type: System.Int32(), Language: Visual Basic) (Syntax: 'arr')
  Body: 
    IBlockStatement (1 statements) (OperationKind.BlockStatement, Language: Visual Basic) (Syntax: 'For Each el ... ambda_local')
      IExpressionStatement (OperationKind.ExpressionStatement, Language: Visual Basic) (Syntax: 'Console.Wri ... mbda_local)')
        Expression: 
          IInvocationExpression (Sub System.Console.WriteLine(value As System.Int32)) (OperationKind.InvocationExpression, Type: System.Void, Language: Visual Basic) (Syntax: 'Console.Wri ... mbda_local)')
            Instance Receiver: 
            null
            Arguments(1):
                IArgument (ArgumentKind.Explicit, Matching Parameter: value) (OperationKind.Argument, Language: Visual Basic) (Syntax: 'element_lambda_local')
                  ILocalReferenceExpression: element_lambda_local (OperationKind.LocalReferenceExpression, Type: System.Int32, Language: Visual Basic) (Syntax: 'element_lambda_local')
                  InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                  OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
  NextVariables(1):
      ILocalReferenceExpression: element_lambda_local (OperationKind.LocalReferenceExpression, Type: System.Int32, Language: Visual Basic) (Syntax: 'element_lambda_local')
]]>.Value

            VerifyOperationTreeForTest(Of ForEachBlockSyntax)(source, expectedOperationTree)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation)>
        <Fact(), WorkItem(17602, "https://github.com/dotnet/roslyn/issues/17602")>
        Public Sub IForEachLoopStatement_InvalidConversion()
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
IForEachLoopStatement (LoopKind.ForEach) (OperationKind.LoopStatement, IsInvalid, Language: Visual Basic) (Syntax: 'For Each el ... Next')
  Locals: Local_1: element As System.Int32
  LoopControlVariable: 
    ILocalReferenceExpression: element (OperationKind.LocalReferenceExpression, Type: System.Int32, Language: Visual Basic) (Syntax: 'element As Integer')
  Collection: 
    ILiteralExpression (OperationKind.LiteralExpression, Type: System.String, Constant: "Hello World.", IsInvalid, Language: Visual Basic) (Syntax: '"Hello World."')
  Body: 
    IBlockStatement (1 statements) (OperationKind.BlockStatement, IsInvalid, Language: Visual Basic) (Syntax: 'For Each el ... Next')
      IExpressionStatement (OperationKind.ExpressionStatement, Language: Visual Basic) (Syntax: 'Console.Wri ... ne(element)')
        Expression: 
          IInvocationExpression (Sub System.Console.WriteLine(value As System.Int32)) (OperationKind.InvocationExpression, Type: System.Void, Language: Visual Basic) (Syntax: 'Console.Wri ... ne(element)')
            Instance Receiver: 
            null
            Arguments(1):
                IArgument (ArgumentKind.Explicit, Matching Parameter: value) (OperationKind.Argument, Language: Visual Basic) (Syntax: 'element')
                  ILocalReferenceExpression: element (OperationKind.LocalReferenceExpression, Type: System.Int32, Language: Visual Basic) (Syntax: 'element')
                  InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                  OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
  NextVariables(0)
]]>.Value

            VerifyOperationTreeForTest(Of ForEachBlockSyntax)(source, expectedOperationTree)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation)>
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
IForEachLoopStatement (LoopKind.ForEach) (OperationKind.LoopStatement, Language: Visual Basic) (Syntax: 'For Each s  ... Next')
  Locals: Local_1: s As System.String
  LoopControlVariable: 
    ILocalReferenceExpression: s (OperationKind.LocalReferenceExpression, Type: System.String, Language: Visual Basic) (Syntax: 's As String')
  Collection: 
    IConversionExpression (Implicit, TryCast: False, Unchecked) (OperationKind.ConversionExpression, Type: System.Collections.IEnumerable, Language: Visual Basic) (Syntax: 'arr')
      Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: True, IsUserDefined: False) (MethodSymbol: null)
      Operand: 
        ILocalReferenceExpression: arr (OperationKind.LocalReferenceExpression, Type: System.String(), Language: Visual Basic) (Syntax: 'arr')
  Body: 
    IBlockStatement (2 statements) (OperationKind.BlockStatement, Language: Visual Basic) (Syntax: 'For Each s  ... Next')
      IIfStatement (OperationKind.IfStatement, Language: Visual Basic) (Syntax: 'If (s = "on ... End If')
        Condition: 
          IParenthesizedExpression (OperationKind.ParenthesizedExpression, Type: System.Boolean, Language: Visual Basic) (Syntax: '(s = "one")')
            Operand: 
              IBinaryOperatorExpression (BinaryOperatorKind.Equals, Checked) (OperationKind.BinaryOperatorExpression, Type: System.Boolean, Language: Visual Basic) (Syntax: 's = "one"')
                Left: 
                  ILocalReferenceExpression: s (OperationKind.LocalReferenceExpression, Type: System.String, Language: Visual Basic) (Syntax: 's')
                Right: 
                  ILiteralExpression (OperationKind.LiteralExpression, Type: System.String, Constant: "one", Language: Visual Basic) (Syntax: '"one"')
        IfTrue: 
          IBlockStatement (1 statements) (OperationKind.BlockStatement, Language: Visual Basic) (Syntax: 'If (s = "on ... End If')
            IExpressionStatement (OperationKind.ExpressionStatement, Language: Visual Basic) (Syntax: 'Throw New Exception')
              Expression: 
                IThrowExpression (OperationKind.ThrowExpression, Type: System.Exception, Language: Visual Basic) (Syntax: 'Throw New Exception')
                  IObjectCreationExpression (Constructor: Sub System.Exception..ctor()) (OperationKind.ObjectCreationExpression, Type: System.Exception, Language: Visual Basic) (Syntax: 'New Exception')
                    Arguments(0)
                    Initializer: 
                    null
        IfFalse: 
        null
      IExpressionStatement (OperationKind.ExpressionStatement, Language: Visual Basic) (Syntax: 'Console.WriteLine(s)')
        Expression: 
          IInvocationExpression (Sub System.Console.WriteLine(value As System.String)) (OperationKind.InvocationExpression, Type: System.Void, Language: Visual Basic) (Syntax: 'Console.WriteLine(s)')
            Instance Receiver: 
            null
            Arguments(1):
                IArgument (ArgumentKind.Explicit, Matching Parameter: value) (OperationKind.Argument, Language: Visual Basic) (Syntax: 's')
                  ILocalReferenceExpression: s (OperationKind.LocalReferenceExpression, Type: System.String, Language: Visual Basic) (Syntax: 's')
                  InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                  OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
  NextVariables(0)
]]>.Value

            VerifyOperationTreeForTest(Of ForEachBlockSyntax)(source, expectedOperationTree)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation)>
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
IForEachLoopStatement (LoopKind.ForEach) (OperationKind.LoopStatement, Language: Visual Basic) (Syntax: 'For Each o  ... Next')
  Locals: Local_1: o As System.Object
  LoopControlVariable: 
    ILocalReferenceExpression: o (OperationKind.LocalReferenceExpression, Type: System.Object, Language: Visual Basic) (Syntax: 'o')
  Collection: 
    IConversionExpression (Implicit, TryCast: False, Unchecked) (OperationKind.ConversionExpression, Type: System.Collections.IEnumerable, Language: Visual Basic) (Syntax: 'c')
      Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: True, IsUserDefined: False) (MethodSymbol: null)
      Operand: 
        IParameterReferenceExpression: c (OperationKind.ParameterReferenceExpression, Type: System.Object(), Language: Visual Basic) (Syntax: 'c')
  Body: 
    IBlockStatement (1 statements) (OperationKind.BlockStatement, Language: Visual Basic) (Syntax: 'For Each o  ... Next')
      IIfStatement (OperationKind.IfStatement, Language: Visual Basic) (Syntax: 'If o IsNot  ... Return True')
        Condition: 
          IBinaryOperatorExpression (BinaryOperatorKind.NotEquals) (OperationKind.BinaryOperatorExpression, Type: System.Boolean, Language: Visual Basic) (Syntax: 'o IsNot Nothing')
            Left: 
              ILocalReferenceExpression: o (OperationKind.LocalReferenceExpression, Type: System.Object, Language: Visual Basic) (Syntax: 'o')
            Right: 
              IConversionExpression (Implicit, TryCast: False, Unchecked) (OperationKind.ConversionExpression, Type: System.Object, Constant: null, Language: Visual Basic) (Syntax: 'Nothing')
                Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                Operand: 
                  ILiteralExpression (OperationKind.LiteralExpression, Type: null, Constant: null, Language: Visual Basic) (Syntax: 'Nothing')
        IfTrue: 
          IBlockStatement (1 statements) (OperationKind.BlockStatement, Language: Visual Basic) (Syntax: 'If o IsNot  ... Return True')
            IReturnStatement (OperationKind.ReturnStatement, Language: Visual Basic) (Syntax: 'Return True')
              ReturnedValue: 
                ILiteralExpression (OperationKind.LiteralExpression, Type: System.Boolean, Constant: True, Language: Visual Basic) (Syntax: 'True')
        IfFalse: 
        null
  NextVariables(0)
]]>.Value

            VerifyOperationTreeForTest(Of ForEachBlockSyntax)(source, expectedOperationTree)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation)>
        <Fact(), WorkItem(17602, "https://github.com/dotnet/roslyn/issues/17602")>
        Public Sub IForEachLoopStatement_FieldAsIterationVariable()
            Dim source = <![CDATA[
Imports System
Imports System.Collections.Generic
Imports System.Linq

Class C
    Private X As Integer = 0
    Sub M(args As Integer())
        For Each X In args'BIND:"For Each X In args"
        Next X
    End Sub
End Class]]>.Value

            Dim expectedOperationTree = <![CDATA[
IForEachLoopStatement (LoopKind.ForEach) (OperationKind.LoopStatement, Language: Visual Basic) (Syntax: 'For Each X  ... Next X')
  LoopControlVariable: 
    IFieldReferenceExpression: C.X As System.Int32 (OperationKind.FieldReferenceExpression, Type: System.Int32, Language: Visual Basic) (Syntax: 'X')
      Instance Receiver: 
        IInstanceReferenceExpression (OperationKind.InstanceReferenceExpression, Type: C, IsImplicit, Language: Visual Basic) (Syntax: 'X')
  Collection: 
    IConversionExpression (Implicit, TryCast: False, Unchecked) (OperationKind.ConversionExpression, Type: System.Collections.IEnumerable, Language: Visual Basic) (Syntax: 'args')
      Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: True, IsUserDefined: False) (MethodSymbol: null)
      Operand: 
        IParameterReferenceExpression: args (OperationKind.ParameterReferenceExpression, Type: System.Int32(), Language: Visual Basic) (Syntax: 'args')
  Body: 
    IBlockStatement (0 statements) (OperationKind.BlockStatement, Language: Visual Basic) (Syntax: 'For Each X  ... Next X')
  NextVariables(1):
      IFieldReferenceExpression: C.X As System.Int32 (OperationKind.FieldReferenceExpression, Type: System.Int32, Language: Visual Basic) (Syntax: 'X')
        Instance Receiver: 
          IInstanceReferenceExpression (OperationKind.InstanceReferenceExpression, Type: C, IsImplicit, Language: Visual Basic) (Syntax: 'X')
]]>.Value

            Dim expectedDiagnostics = String.Empty

            VerifyOperationTreeAndDiagnosticsForTest(Of ForEachBlockSyntax)(source, expectedOperationTree, expectedDiagnostics)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation)>
        <Fact(), WorkItem(17602, "https://github.com/dotnet/roslyn/issues/17602")>
        Public Sub IForEachLoopStatement_FieldWithExplicitReceiverAsIterationVariable()
            Dim source = <![CDATA[
Imports System
Imports System.Collections.Generic
Imports System.Linq

Class C
    Private X As Integer = 0
    Sub M(c As C, args As Integer())
        For Each c.X In args'BIND:"For Each c.X In args"
        Next c.X
    End Sub
End Class]]>.Value

            Dim expectedOperationTree = <![CDATA[
IForEachLoopStatement (LoopKind.ForEach) (OperationKind.LoopStatement, Language: Visual Basic) (Syntax: 'For Each c. ... Next c.X')
  LoopControlVariable: 
    IFieldReferenceExpression: C.X As System.Int32 (OperationKind.FieldReferenceExpression, Type: System.Int32, Language: Visual Basic) (Syntax: 'c.X')
      Instance Receiver: 
        IParameterReferenceExpression: c (OperationKind.ParameterReferenceExpression, Type: C, Language: Visual Basic) (Syntax: 'c')
  Collection: 
    IConversionExpression (Implicit, TryCast: False, Unchecked) (OperationKind.ConversionExpression, Type: System.Collections.IEnumerable, Language: Visual Basic) (Syntax: 'args')
      Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: True, IsUserDefined: False) (MethodSymbol: null)
      Operand: 
        IParameterReferenceExpression: args (OperationKind.ParameterReferenceExpression, Type: System.Int32(), Language: Visual Basic) (Syntax: 'args')
  Body: 
    IBlockStatement (0 statements) (OperationKind.BlockStatement, Language: Visual Basic) (Syntax: 'For Each c. ... Next c.X')
  NextVariables(1):
      IFieldReferenceExpression: C.X As System.Int32 (OperationKind.FieldReferenceExpression, Type: System.Int32, Language: Visual Basic) (Syntax: 'c.X')
        Instance Receiver: 
          IParameterReferenceExpression: c (OperationKind.ParameterReferenceExpression, Type: C, Language: Visual Basic) (Syntax: 'c')
]]>.Value

            Dim expectedDiagnostics = String.Empty

            VerifyOperationTreeAndDiagnosticsForTest(Of ForEachBlockSyntax)(source, expectedOperationTree, expectedDiagnostics)
        End Sub

    End Class
End Namespace
