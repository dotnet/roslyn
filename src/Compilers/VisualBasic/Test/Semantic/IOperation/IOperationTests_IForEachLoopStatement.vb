' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports Microsoft.CodeAnalysis.Operations
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
IForEachLoopOperation (LoopKind.ForEach) (OperationKind.Loop, Type: null) (Syntax: 'For Each s  ... Next')
  Locals: Local_1: s As System.String
  LoopControlVariable: 
    ILocalReferenceOperation: s (OperationKind.LocalReference, Type: System.String) (Syntax: 's As String')
  Collection: 
    IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.Collections.IEnumerable, IsImplicit) (Syntax: 'arr')
      Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: True, IsUserDefined: False) (MethodSymbol: null)
      Operand: 
        ILocalReferenceOperation: arr (OperationKind.LocalReference, Type: System.String()) (Syntax: 'arr')
  Body: 
    IBlockOperation (1 statements) (OperationKind.Block, Type: null, IsImplicit) (Syntax: 'For Each s  ... Next')
      IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'Console.WriteLine(s)')
        Expression: 
          IInvocationOperation (Sub System.Console.WriteLine(value As System.String)) (OperationKind.Invocation, Type: System.Void) (Syntax: 'Console.WriteLine(s)')
            Instance Receiver: 
              null
            Arguments(1):
                IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: value) (OperationKind.Argument, Type: null) (Syntax: 's')
                  ILocalReferenceOperation: s (OperationKind.LocalReference, Type: System.String) (Syntax: 's')
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
IForEachLoopOperation (LoopKind.ForEach) (OperationKind.Loop, Type: null) (Syntax: 'For Each it ... Next')
  Locals: Local_1: item As System.String
  LoopControlVariable: 
    ILocalReferenceOperation: item (OperationKind.LocalReference, Type: System.String) (Syntax: 'item As String')
  Collection: 
    ILocalReferenceOperation: list (OperationKind.LocalReference, Type: System.Collections.Generic.List(Of System.String)) (Syntax: 'list')
  Body: 
    IBlockOperation (1 statements) (OperationKind.Block, Type: null, IsImplicit) (Syntax: 'For Each it ... Next')
      IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'System.Cons ... eLine(item)')
        Expression: 
          IInvocationOperation (Sub System.Console.WriteLine(value As System.String)) (OperationKind.Invocation, Type: System.Void) (Syntax: 'System.Cons ... eLine(item)')
            Instance Receiver: 
              null
            Arguments(1):
                IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: value) (OperationKind.Argument, Type: null) (Syntax: 'item')
                  ILocalReferenceOperation: item (OperationKind.LocalReference, Type: System.String) (Syntax: 'item')
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
IForEachLoopOperation (LoopKind.ForEach) (OperationKind.Loop, Type: null) (Syntax: 'For Each y  ... Next')
  Locals: Local_1: y As System.Char
  LoopControlVariable: 
    ILocalReferenceOperation: y (OperationKind.LocalReference, Type: System.Char) (Syntax: 'y As Char')
  Collection: 
    ILocalReferenceOperation: x (OperationKind.LocalReference, Type: System.String) (Syntax: 'x')
  Body: 
    IBlockOperation (1 statements) (OperationKind.Block, Type: null, IsImplicit) (Syntax: 'For Each y  ... Next')
      IConditionalOperation (OperationKind.Conditional, Type: null) (Syntax: 'If y = "B"c ... End If')
        Condition: 
          IBinaryOperation (BinaryOperatorKind.Equals, Checked) (OperationKind.BinaryOperator, Type: System.Boolean) (Syntax: 'y = "B"c')
            Left: 
              ILocalReferenceOperation: y (OperationKind.LocalReference, Type: System.Char) (Syntax: 'y')
            Right: 
              ILiteralOperation (OperationKind.Literal, Type: System.Char, Constant: B) (Syntax: '"B"c')
        WhenTrue: 
          IBlockOperation (1 statements) (OperationKind.Block, Type: null, IsImplicit) (Syntax: 'If y = "B"c ... End If')
            IBranchOperation (BranchKind.Break, Label: exit) (OperationKind.Branch, Type: null) (Syntax: 'Exit For')
        WhenFalse: 
          IBlockOperation (1 statements) (OperationKind.Block, Type: null) (Syntax: 'Else ... riteLine(y)')
            IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'System.Cons ... riteLine(y)')
              Expression: 
                IInvocationOperation (Sub System.Console.WriteLine(value As System.Char)) (OperationKind.Invocation, Type: System.Void) (Syntax: 'System.Cons ... riteLine(y)')
                  Instance Receiver: 
                    null
                  Arguments(1):
                      IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: value) (OperationKind.Argument, Type: null) (Syntax: 'y')
                        ILocalReferenceOperation: y (OperationKind.LocalReference, Type: System.Char) (Syntax: 'y')
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
IForEachLoopOperation (LoopKind.ForEach) (OperationKind.Loop, Type: null) (Syntax: 'For Each x  ... Next y, x')
  Locals: Local_1: x As System.String
  LoopControlVariable: 
    ILocalReferenceOperation: x (OperationKind.LocalReference, Type: System.String) (Syntax: 'x As String')
  Collection: 
    IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.Collections.IEnumerable, IsImplicit) (Syntax: 'S')
      Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: True, IsUserDefined: False) (MethodSymbol: null)
      Operand: 
        ILocalReferenceOperation: S (OperationKind.LocalReference, Type: System.String()) (Syntax: 'S')
  Body: 
    IBlockOperation (1 statements) (OperationKind.Block, Type: null, IsImplicit) (Syntax: 'For Each x  ... Next y, x')
      IForEachLoopOperation (LoopKind.ForEach) (OperationKind.Loop, Type: null) (Syntax: 'For Each y  ... Next y, x')
        Locals: Local_1: y As System.Char
        LoopControlVariable: 
          ILocalReferenceOperation: y (OperationKind.LocalReference, Type: System.Char) (Syntax: 'y As Char')
        Collection: 
          ILocalReferenceOperation: x (OperationKind.LocalReference, Type: System.String) (Syntax: 'x')
        Body: 
          IBlockOperation (2 statements) (OperationKind.Block, Type: null, IsImplicit) (Syntax: 'For Each y  ... Next y, x')
            IConditionalOperation (OperationKind.Conditional, Type: null) (Syntax: 'If y = "B"c ... End If')
              Condition: 
                IBinaryOperation (BinaryOperatorKind.Equals, Checked) (OperationKind.BinaryOperator, Type: System.Boolean) (Syntax: 'y = "B"c')
                  Left: 
                    ILocalReferenceOperation: y (OperationKind.LocalReference, Type: System.Char) (Syntax: 'y')
                  Right: 
                    ILiteralOperation (OperationKind.Literal, Type: System.Char, Constant: B) (Syntax: '"B"c')
              WhenTrue: 
                IBlockOperation (1 statements) (OperationKind.Block, Type: null, IsImplicit) (Syntax: 'If y = "B"c ... End If')
                  IBranchOperation (BranchKind.Continue, Label: continue) (OperationKind.Branch, Type: null) (Syntax: 'Continue For')
              WhenFalse: 
                null
            IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'System.Cons ... riteLine(y)')
              Expression: 
                IInvocationOperation (Sub System.Console.WriteLine(value As System.Char)) (OperationKind.Invocation, Type: System.Void) (Syntax: 'System.Cons ... riteLine(y)')
                  Instance Receiver: 
                    null
                  Arguments(1):
                      IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: value) (OperationKind.Argument, Type: null) (Syntax: 'y')
                        ILocalReferenceOperation: y (OperationKind.LocalReference, Type: System.Char) (Syntax: 'y')
                        InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                        OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
        NextVariables(2):
            ILocalReferenceOperation: y (OperationKind.LocalReference, Type: System.Char) (Syntax: 'y')
            ILocalReferenceOperation: x (OperationKind.LocalReference, Type: System.String) (Syntax: 'x')
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
IForEachLoopOperation (LoopKind.ForEach) (OperationKind.Loop, Type: null) (Syntax: 'For Each y  ... Next')
  Locals: Local_1: y As System.Int32
  LoopControlVariable: 
    ILocalReferenceOperation: y (OperationKind.LocalReference, Type: System.Int32) (Syntax: 'y As Integer')
  Collection: 
    IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.Collections.IEnumerable, IsImplicit) (Syntax: 'x')
      Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: True, IsUserDefined: False) (MethodSymbol: null)
      Operand: 
        ILocalReferenceOperation: x (OperationKind.LocalReference, Type: System.Int32()) (Syntax: 'x')
  Body: 
    IBlockOperation (1 statements) (OperationKind.Block, Type: null, IsImplicit) (Syntax: 'For Each y  ... Next')
      IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'System.Cons ... riteLine(y)')
        Expression: 
          IInvocationOperation (Sub System.Console.WriteLine(value As System.Int32)) (OperationKind.Invocation, Type: System.Void) (Syntax: 'System.Cons ... riteLine(y)')
            Instance Receiver: 
              null
            Arguments(1):
                IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: value) (OperationKind.Argument, Type: null) (Syntax: 'y')
                  ILocalReferenceOperation: y (OperationKind.LocalReference, Type: System.Int32) (Syntax: 'y')
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
IForEachLoopOperation (LoopKind.ForEach) (OperationKind.Loop, Type: null) (Syntax: 'For Each x  ... Next')
  Locals: Local_1: x As System.String
  LoopControlVariable: 
    ILocalReferenceOperation: x (OperationKind.LocalReference, Type: System.String) (Syntax: 'x As String')
  Collection: 
    IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.Collections.IEnumerable, IsImplicit) (Syntax: 'S')
      Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: True, IsUserDefined: False) (MethodSymbol: null)
      Operand: 
        ILocalReferenceOperation: S (OperationKind.LocalReference, Type: System.String()) (Syntax: 'S')
  Body: 
    IBlockOperation (1 statements) (OperationKind.Block, Type: null, IsImplicit) (Syntax: 'For Each x  ... Next')
      IForEachLoopOperation (LoopKind.ForEach) (OperationKind.Loop, Type: null) (Syntax: 'For Each y  ... Next')
        Locals: Local_1: y As System.Char
        LoopControlVariable: 
          ILocalReferenceOperation: y (OperationKind.LocalReference, Type: System.Char) (Syntax: 'y As Char')
        Collection: 
          ILocalReferenceOperation: x (OperationKind.LocalReference, Type: System.String) (Syntax: 'x')
        Body: 
          IBlockOperation (1 statements) (OperationKind.Block, Type: null, IsImplicit) (Syntax: 'For Each y  ... Next')
            IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'System.Cons ... riteLine(y)')
              Expression: 
                IInvocationOperation (Sub System.Console.WriteLine(value As System.Char)) (OperationKind.Invocation, Type: System.Void) (Syntax: 'System.Cons ... riteLine(y)')
                  Instance Receiver: 
                    null
                  Arguments(1):
                      IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: value) (OperationKind.Argument, Type: null) (Syntax: 'y')
                        ILocalReferenceOperation: y (OperationKind.LocalReference, Type: System.Char) (Syntax: 'y')
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
IForEachLoopOperation (LoopKind.ForEach) (OperationKind.Loop, Type: null) (Syntax: 'For Each x  ... Next')
  Locals: Local_1: x As System.Object
  LoopControlVariable: 
    ILocalReferenceOperation: x (OperationKind.LocalReference, Type: System.Object) (Syntax: 'x')
  Collection: 
    IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.Collections.IEnumerable, IsImplicit) (Syntax: 'New Enumerable()')
      Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: True, IsUserDefined: False) (MethodSymbol: null)
      Operand: 
        IObjectCreationOperation (Constructor: Sub Enumerable..ctor()) (OperationKind.ObjectCreation, Type: Enumerable) (Syntax: 'New Enumerable()')
          Arguments(0)
          Initializer: 
            null
  Body: 
    IBlockOperation (1 statements) (OperationKind.Block, Type: null, IsImplicit) (Syntax: 'For Each x  ... Next')
      IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'System.Cons ... riteLine(x)')
        Expression: 
          IInvocationOperation (Sub System.Console.WriteLine(value As System.Object)) (OperationKind.Invocation, Type: System.Void) (Syntax: 'System.Cons ... riteLine(x)')
            Instance Receiver: 
              null
            Arguments(1):
                IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: value) (OperationKind.Argument, Type: null) (Syntax: 'x')
                  ILocalReferenceOperation: x (OperationKind.LocalReference, Type: System.Object) (Syntax: 'x')
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
IForEachLoopOperation (LoopKind.ForEach) (OperationKind.Loop, Type: null) (Syntax: 'For Each y  ... Next')
  Locals: Local_1: y As System.Char
  LoopControlVariable: 
    ILocalReferenceOperation: y (OperationKind.LocalReference, Type: System.Char) (Syntax: 'y')
  Collection: 
    ILocalReferenceOperation: s (OperationKind.LocalReference, Type: System.String, Constant: null) (Syntax: 's')
  Body: 
    IBlockOperation (1 statements) (OperationKind.Block, Type: null, IsImplicit) (Syntax: 'For Each y  ... Next')
      IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'System.Cons ... riteLine(y)')
        Expression: 
          IInvocationOperation (Sub System.Console.WriteLine(value As System.Char)) (OperationKind.Invocation, Type: System.Void) (Syntax: 'System.Cons ... riteLine(y)')
            Instance Receiver: 
              null
            Arguments(1):
                IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: value) (OperationKind.Argument, Type: null) (Syntax: 'y')
                  ILocalReferenceOperation: y (OperationKind.LocalReference, Type: System.Char) (Syntax: 'y')
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
IForEachLoopOperation (LoopKind.ForEach) (OperationKind.Loop, Type: null) (Syntax: 'For Each x  ... Next')
  Locals: Local_1: x As System.Object
  LoopControlVariable: 
    ILocalReferenceOperation: x (OperationKind.LocalReference, Type: System.Object) (Syntax: 'x')
  Collection: 
    IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.Collections.IEnumerable, IsImplicit) (Syntax: 'New Enumerable()')
      Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
      Operand: 
        IObjectCreationOperation (Constructor: Sub Enumerable..ctor()) (OperationKind.ObjectCreation, Type: Enumerable) (Syntax: 'New Enumerable()')
          Arguments(0)
          Initializer: 
            null
  Body: 
    IBlockOperation (1 statements) (OperationKind.Block, Type: null, IsImplicit) (Syntax: 'For Each x  ... Next')
      IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'System.Cons ... riteLine(x)')
        Expression: 
          IInvocationOperation (Sub System.Console.WriteLine(value As System.Object)) (OperationKind.Invocation, Type: System.Void) (Syntax: 'System.Cons ... riteLine(x)')
            Instance Receiver: 
              null
            Arguments(1):
                IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: value) (OperationKind.Argument, Type: null) (Syntax: 'x')
                  ILocalReferenceOperation: x (OperationKind.LocalReference, Type: System.Object) (Syntax: 'x')
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
IForEachLoopOperation (LoopKind.ForEach) (OperationKind.Loop, Type: null) (Syntax: 'For Each co ... Next')
  Locals: Local_1: country As <anonymous type: Key CountryName As System.String, Key CustomersInCountry As System.Collections.Generic.IEnumerable(Of Customer), Key Count As System.Int32>
  LoopControlVariable: 
    ILocalReferenceOperation: country (OperationKind.LocalReference, Type: <anonymous type: Key CountryName As System.String, Key CustomersInCountry As System.Collections.Generic.IEnumerable(Of Customer), Key Count As System.Int32>) (Syntax: 'country')
  Collection: 
    ILocalReferenceOperation: countries (OperationKind.LocalReference, Type: System.Linq.IOrderedEnumerable(Of <anonymous type: Key CountryName As System.String, Key CustomersInCountry As System.Collections.Generic.IEnumerable(Of Customer), Key Count As System.Int32>)) (Syntax: 'countries')
  Body: 
    IBlockOperation (2 statements) (OperationKind.Block, Type: null, IsImplicit) (Syntax: 'For Each co ... Next')
      IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'Debug.Write ... ntry.Count)')
        Expression: 
          IInvocationOperation (Sub System.Diagnostics.Debug.WriteLine(message As System.String)) (OperationKind.Invocation, Type: System.Void) (Syntax: 'Debug.Write ... ntry.Count)')
            Instance Receiver: 
              null
            Arguments(1):
                IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: message) (OperationKind.Argument, Type: null) (Syntax: 'country.Cou ... untry.Count')
                  IBinaryOperation (BinaryOperatorKind.Concatenate, Checked) (OperationKind.BinaryOperator, Type: System.String) (Syntax: 'country.Cou ... untry.Count')
                    Left: 
                      IBinaryOperation (BinaryOperatorKind.Concatenate, Checked) (OperationKind.BinaryOperator, Type: System.String) (Syntax: 'country.Cou ... & " count="')
                        Left: 
                          IPropertyReferenceOperation: ReadOnly Property <anonymous type: Key CountryName As System.String, Key CustomersInCountry As System.Collections.Generic.IEnumerable(Of Customer), Key Count As System.Int32>.CountryName As System.String (OperationKind.PropertyReference, Type: System.String) (Syntax: 'country.CountryName')
                            Instance Receiver: 
                              ILocalReferenceOperation: country (OperationKind.LocalReference, Type: <anonymous type: Key CountryName As System.String, Key CustomersInCountry As System.Collections.Generic.IEnumerable(Of Customer), Key Count As System.Int32>) (Syntax: 'country')
                        Right: 
                          ILiteralOperation (OperationKind.Literal, Type: System.String, Constant: " count=") (Syntax: '" count="')
                    Right: 
                      IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.String, IsImplicit) (Syntax: 'country.Count')
                        Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                        Operand: 
                          IPropertyReferenceOperation: ReadOnly Property <anonymous type: Key CountryName As System.String, Key CustomersInCountry As System.Collections.Generic.IEnumerable(Of Customer), Key Count As System.Int32>.Count As System.Int32 (OperationKind.PropertyReference, Type: System.Int32) (Syntax: 'country.Count')
                            Instance Receiver: 
                              ILocalReferenceOperation: country (OperationKind.LocalReference, Type: <anonymous type: Key CountryName As System.String, Key CustomersInCountry As System.Collections.Generic.IEnumerable(Of Customer), Key Count As System.Int32>) (Syntax: 'country')
                  InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                  OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
      IForEachLoopOperation (LoopKind.ForEach) (OperationKind.Loop, Type: null) (Syntax: 'For Each cu ... Next')
        Locals: Local_1: customer As Customer
        LoopControlVariable: 
          ILocalReferenceOperation: customer (OperationKind.LocalReference, Type: Customer) (Syntax: 'customer')
        Collection: 
          IPropertyReferenceOperation: ReadOnly Property <anonymous type: Key CountryName As System.String, Key CustomersInCountry As System.Collections.Generic.IEnumerable(Of Customer), Key Count As System.Int32>.CustomersInCountry As System.Collections.Generic.IEnumerable(Of Customer) (OperationKind.PropertyReference, Type: System.Collections.Generic.IEnumerable(Of Customer)) (Syntax: 'country.Cus ... rsInCountry')
            Instance Receiver: 
              ILocalReferenceOperation: country (OperationKind.LocalReference, Type: <anonymous type: Key CountryName As System.String, Key CustomersInCountry As System.Collections.Generic.IEnumerable(Of Customer), Key Count As System.Int32>) (Syntax: 'country')
        Body: 
          IBlockOperation (1 statements) (OperationKind.Block, Type: null, IsImplicit) (Syntax: 'For Each cu ... Next')
            IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'Debug.Write ... tomer.City)')
              Expression: 
                IInvocationOperation (Sub System.Diagnostics.Debug.WriteLine(message As System.String)) (OperationKind.Invocation, Type: System.Void) (Syntax: 'Debug.Write ... tomer.City)')
                  Instance Receiver: 
                    null
                  Arguments(1):
                      IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: message) (OperationKind.Argument, Type: null) (Syntax: '"   " & cus ... stomer.City')
                        IBinaryOperation (BinaryOperatorKind.Concatenate, Checked) (OperationKind.BinaryOperator, Type: System.String) (Syntax: '"   " & cus ... stomer.City')
                          Left: 
                            IBinaryOperation (BinaryOperatorKind.Concatenate, Checked) (OperationKind.BinaryOperator, Type: System.String) (Syntax: '"   " & cus ... Name & "  "')
                              Left: 
                                IBinaryOperation (BinaryOperatorKind.Concatenate, Checked) (OperationKind.BinaryOperator, Type: System.String) (Syntax: '"   " & cus ... CompanyName')
                                  Left: 
                                    ILiteralOperation (OperationKind.Literal, Type: System.String, Constant: "   ") (Syntax: '"   "')
                                  Right: 
                                    IPropertyReferenceOperation: Property Customer.CompanyName As System.String (OperationKind.PropertyReference, Type: System.String) (Syntax: 'customer.CompanyName')
                                      Instance Receiver: 
                                        ILocalReferenceOperation: customer (OperationKind.LocalReference, Type: Customer) (Syntax: 'customer')
                              Right: 
                                ILiteralOperation (OperationKind.Literal, Type: System.String, Constant: "  ") (Syntax: '"  "')
                          Right: 
                            IPropertyReferenceOperation: Property Customer.City As System.String (OperationKind.PropertyReference, Type: System.String) (Syntax: 'customer.City')
                              Instance Receiver: 
                                ILocalReferenceOperation: customer (OperationKind.LocalReference, Type: Customer) (Syntax: 'customer')
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
IForEachLoopOperation (LoopKind.ForEach) (OperationKind.Loop, Type: null) (Syntax: 'For Each [C ... Next')
  Locals: Local_1: Custom As System.Int32
  LoopControlVariable: 
    ILocalReferenceOperation: Custom (OperationKind.LocalReference, Type: System.Int32) (Syntax: '[Custom]')
  Collection: 
    ILocalReferenceOperation: k (OperationKind.LocalReference, Type: System.Int32(,)) (Syntax: 'k')
  Body: 
    IBlockOperation (3 statements) (OperationKind.Block, Type: null, IsImplicit) (Syntax: 'For Each [C ... Next')
      IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'Console.Wri ... (Integer)))')
        Expression: 
          IInvocationOperation (Sub System.Console.Write(value As System.Boolean)) (OperationKind.Invocation, Type: System.Void) (Syntax: 'Console.Wri ... (Integer)))')
            Instance Receiver: 
              null
            Arguments(1):
                IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: value) (OperationKind.Argument, Type: null) (Syntax: 'VerifyStati ... e(Integer))')
                  IInvocationOperation (Function Program.VerifyStaticType(Of System.Int32)(x As System.Int32, y As System.Type) As System.Boolean) (OperationKind.Invocation, Type: System.Boolean) (Syntax: 'VerifyStati ... e(Integer))')
                    Instance Receiver: 
                      null
                    Arguments(2):
                        IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: x) (OperationKind.Argument, Type: null) (Syntax: '[Custom]')
                          ILocalReferenceOperation: Custom (OperationKind.LocalReference, Type: System.Int32) (Syntax: '[Custom]')
                          InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                          OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                        IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: y) (OperationKind.Argument, Type: null) (Syntax: 'GetType(Integer)')
                          ITypeOfOperation (OperationKind.TypeOf, Type: System.Type) (Syntax: 'GetType(Integer)')
                            TypeOperand: System.Int32
                          InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                          OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                  InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                  OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
      IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'Console.Wri ... e(Object)))')
        Expression: 
          IInvocationOperation (Sub System.Console.Write(value As System.Boolean)) (OperationKind.Invocation, Type: System.Void) (Syntax: 'Console.Wri ... e(Object)))')
            Instance Receiver: 
              null
            Arguments(1):
                IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: value) (OperationKind.Argument, Type: null) (Syntax: 'VerifyStati ... pe(Object))')
                  IInvocationOperation (Function Program.VerifyStaticType(Of System.Int32)(x As System.Int32, y As System.Type) As System.Boolean) (OperationKind.Invocation, Type: System.Boolean) (Syntax: 'VerifyStati ... pe(Object))')
                    Instance Receiver: 
                      null
                    Arguments(2):
                        IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: x) (OperationKind.Argument, Type: null) (Syntax: '[Custom]')
                          ILocalReferenceOperation: Custom (OperationKind.LocalReference, Type: System.Int32) (Syntax: '[Custom]')
                          InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                          OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                        IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: y) (OperationKind.Argument, Type: null) (Syntax: 'GetType(Object)')
                          ITypeOfOperation (OperationKind.TypeOf, Type: System.Type) (Syntax: 'GetType(Object)')
                            TypeOperand: System.Object
                          InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                          OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                  InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                  OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
      IBranchOperation (BranchKind.Break, Label: exit) (OperationKind.Branch, Type: null) (Syntax: 'Exit For')
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
IForEachLoopOperation (LoopKind.ForEach) (OperationKind.Loop, Type: null) (Syntax: 'For Each x  ... Next')
  Locals: Local_1: x As System.Object
  LoopControlVariable: 
    ILocalReferenceOperation: x (OperationKind.LocalReference, Type: System.Object) (Syntax: 'x')
  Collection: 
    IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.Collections.IEnumerable, IsImplicit) (Syntax: 'o')
      Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: True, IsUserDefined: False) (MethodSymbol: null)
      Operand: 
        ILocalReferenceOperation: o (OperationKind.LocalReference, Type: System.Object) (Syntax: 'o')
  Body: 
    IBlockOperation (1 statements) (OperationKind.Block, Type: null, IsImplicit) (Syntax: 'For Each x  ... Next')
      IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'Console.WriteLine(x)')
        Expression: 
          IInvocationOperation (Sub System.Console.WriteLine(value As System.Object)) (OperationKind.Invocation, Type: System.Void) (Syntax: 'Console.WriteLine(x)')
            Instance Receiver: 
              null
            Arguments(1):
                IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: value) (OperationKind.Argument, Type: null) (Syntax: 'x')
                  ILocalReferenceOperation: x (OperationKind.LocalReference, Type: System.Object) (Syntax: 'x')
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
IForEachLoopOperation (LoopKind.ForEach) (OperationKind.Loop, Type: null) (Syntax: 'For Each x  ... Next')
  Locals: Local_1: x As System.Int32
  LoopControlVariable: 
    ILocalReferenceOperation: x (OperationKind.LocalReference, Type: System.Int32) (Syntax: 'x')
  Collection: 
    IObjectCreationOperation (Constructor: Sub Enumerable..ctor()) (OperationKind.ObjectCreation, Type: Enumerable) (Syntax: 'New Enumerable()')
      Arguments(0)
      Initializer: 
        null
  Body: 
    IBlockOperation (1 statements) (OperationKind.Block, Type: null, IsImplicit) (Syntax: 'For Each x  ... Next')
      IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'System.Cons ... riteLine(x)')
        Expression: 
          IInvocationOperation (Sub System.Console.WriteLine(value As System.Int32)) (OperationKind.Invocation, Type: System.Void) (Syntax: 'System.Cons ... riteLine(x)')
            Instance Receiver: 
              null
            Arguments(1):
                IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: value) (OperationKind.Argument, Type: null) (Syntax: 'x')
                  ILocalReferenceOperation: x (OperationKind.LocalReference, Type: System.Int32) (Syntax: 'x')
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
IForEachLoopOperation (LoopKind.ForEach) (OperationKind.Loop, Type: null) (Syntax: 'For Each el ... ambda_local')
  LoopControlVariable: 
    ILocalReferenceOperation: element_lambda_local (OperationKind.LocalReference, Type: System.Int32) (Syntax: 'element_lambda_local')
  Collection: 
    IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.Collections.IEnumerable, IsImplicit) (Syntax: 'arr')
      Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: True, IsUserDefined: False) (MethodSymbol: null)
      Operand: 
        ILocalReferenceOperation: arr (OperationKind.LocalReference, Type: System.Int32()) (Syntax: 'arr')
  Body: 
    IBlockOperation (1 statements) (OperationKind.Block, Type: null, IsImplicit) (Syntax: 'For Each el ... ambda_local')
      IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'Console.Wri ... mbda_local)')
        Expression: 
          IInvocationOperation (Sub System.Console.WriteLine(value As System.Int32)) (OperationKind.Invocation, Type: System.Void) (Syntax: 'Console.Wri ... mbda_local)')
            Instance Receiver: 
              null
            Arguments(1):
                IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: value) (OperationKind.Argument, Type: null) (Syntax: 'element_lambda_local')
                  ILocalReferenceOperation: element_lambda_local (OperationKind.LocalReference, Type: System.Int32) (Syntax: 'element_lambda_local')
                  InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                  OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
  NextVariables(1):
      ILocalReferenceOperation: element_lambda_local (OperationKind.LocalReference, Type: System.Int32) (Syntax: 'element_lambda_local')
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
IForEachLoopOperation (LoopKind.ForEach) (OperationKind.Loop, Type: null, IsInvalid) (Syntax: 'For Each el ... Next')
  Locals: Local_1: element As System.Int32
  LoopControlVariable: 
    ILocalReferenceOperation: element (OperationKind.LocalReference, Type: System.Int32) (Syntax: 'element As Integer')
  Collection: 
    ILiteralOperation (OperationKind.Literal, Type: System.String, Constant: "Hello World.", IsInvalid) (Syntax: '"Hello World."')
  Body: 
    IBlockOperation (1 statements) (OperationKind.Block, Type: null, IsInvalid, IsImplicit) (Syntax: 'For Each el ... Next')
      IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'Console.Wri ... ne(element)')
        Expression: 
          IInvocationOperation (Sub System.Console.WriteLine(value As System.Int32)) (OperationKind.Invocation, Type: System.Void) (Syntax: 'Console.Wri ... ne(element)')
            Instance Receiver: 
              null
            Arguments(1):
                IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: value) (OperationKind.Argument, Type: null) (Syntax: 'element')
                  ILocalReferenceOperation: element (OperationKind.LocalReference, Type: System.Int32) (Syntax: 'element')
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
IForEachLoopOperation (LoopKind.ForEach) (OperationKind.Loop, Type: null) (Syntax: 'For Each s  ... Next')
  Locals: Local_1: s As System.String
  LoopControlVariable: 
    ILocalReferenceOperation: s (OperationKind.LocalReference, Type: System.String) (Syntax: 's As String')
  Collection: 
    IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.Collections.IEnumerable, IsImplicit) (Syntax: 'arr')
      Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: True, IsUserDefined: False) (MethodSymbol: null)
      Operand: 
        ILocalReferenceOperation: arr (OperationKind.LocalReference, Type: System.String()) (Syntax: 'arr')
  Body: 
    IBlockOperation (2 statements) (OperationKind.Block, Type: null, IsImplicit) (Syntax: 'For Each s  ... Next')
      IConditionalOperation (OperationKind.Conditional, Type: null) (Syntax: 'If (s = "on ... End If')
        Condition: 
          IParenthesizedOperation (OperationKind.Parenthesized, Type: System.Boolean) (Syntax: '(s = "one")')
            Operand: 
              IBinaryOperation (BinaryOperatorKind.Equals, Checked) (OperationKind.BinaryOperator, Type: System.Boolean) (Syntax: 's = "one"')
                Left: 
                  ILocalReferenceOperation: s (OperationKind.LocalReference, Type: System.String) (Syntax: 's')
                Right: 
                  ILiteralOperation (OperationKind.Literal, Type: System.String, Constant: "one") (Syntax: '"one"')
        WhenTrue: 
          IBlockOperation (1 statements) (OperationKind.Block, Type: null, IsImplicit) (Syntax: 'If (s = "on ... End If')
            IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'Throw New Exception')
              Expression: 
                IThrowOperation (OperationKind.Throw, Type: System.Exception, IsImplicit) (Syntax: 'Throw New Exception')
                  IObjectCreationOperation (Constructor: Sub System.Exception..ctor()) (OperationKind.ObjectCreation, Type: System.Exception) (Syntax: 'New Exception')
                    Arguments(0)
                    Initializer: 
                      null
        WhenFalse: 
          null
      IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'Console.WriteLine(s)')
        Expression: 
          IInvocationOperation (Sub System.Console.WriteLine(value As System.String)) (OperationKind.Invocation, Type: System.Void) (Syntax: 'Console.WriteLine(s)')
            Instance Receiver: 
              null
            Arguments(1):
                IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: value) (OperationKind.Argument, Type: null) (Syntax: 's')
                  ILocalReferenceOperation: s (OperationKind.LocalReference, Type: System.String) (Syntax: 's')
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
IForEachLoopOperation (LoopKind.ForEach) (OperationKind.Loop, Type: null) (Syntax: 'For Each o  ... Next')
  Locals: Local_1: o As System.Object
  LoopControlVariable: 
    ILocalReferenceOperation: o (OperationKind.LocalReference, Type: System.Object) (Syntax: 'o')
  Collection: 
    IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.Collections.IEnumerable, IsImplicit) (Syntax: 'c')
      Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: True, IsUserDefined: False) (MethodSymbol: null)
      Operand: 
        IParameterReferenceOperation: c (OperationKind.ParameterReference, Type: System.Object()) (Syntax: 'c')
  Body: 
    IBlockOperation (1 statements) (OperationKind.Block, Type: null, IsImplicit) (Syntax: 'For Each o  ... Next')
      IConditionalOperation (OperationKind.Conditional, Type: null) (Syntax: 'If o IsNot  ... Return True')
        Condition: 
          IBinaryOperation (BinaryOperatorKind.NotEquals) (OperationKind.BinaryOperator, Type: System.Boolean) (Syntax: 'o IsNot Nothing')
            Left: 
              ILocalReferenceOperation: o (OperationKind.LocalReference, Type: System.Object) (Syntax: 'o')
            Right: 
              IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.Object, Constant: null, IsImplicit) (Syntax: 'Nothing')
                Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                Operand: 
                  ILiteralOperation (OperationKind.Literal, Type: null, Constant: null) (Syntax: 'Nothing')
        WhenTrue: 
          IBlockOperation (1 statements) (OperationKind.Block, Type: null, IsImplicit) (Syntax: 'If o IsNot  ... Return True')
            IReturnOperation (OperationKind.Return, Type: null) (Syntax: 'Return True')
              ReturnedValue: 
                ILiteralOperation (OperationKind.Literal, Type: System.Boolean, Constant: True) (Syntax: 'True')
        WhenFalse: 
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
IForEachLoopOperation (LoopKind.ForEach) (OperationKind.Loop, Type: null) (Syntax: 'For Each X  ... Next X')
  LoopControlVariable: 
    IFieldReferenceOperation: C.X As System.Int32 (OperationKind.FieldReference, Type: System.Int32) (Syntax: 'X')
      Instance Receiver: 
        IInstanceReferenceOperation (OperationKind.InstanceReference, Type: C, IsImplicit) (Syntax: 'X')
  Collection: 
    IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.Collections.IEnumerable, IsImplicit) (Syntax: 'args')
      Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: True, IsUserDefined: False) (MethodSymbol: null)
      Operand: 
        IParameterReferenceOperation: args (OperationKind.ParameterReference, Type: System.Int32()) (Syntax: 'args')
  Body: 
    IBlockOperation (0 statements) (OperationKind.Block, Type: null, IsImplicit) (Syntax: 'For Each X  ... Next X')
  NextVariables(1):
      IFieldReferenceOperation: C.X As System.Int32 (OperationKind.FieldReference, Type: System.Int32) (Syntax: 'X')
        Instance Receiver: 
          IInstanceReferenceOperation (OperationKind.InstanceReference, Type: C, IsImplicit) (Syntax: 'X')
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
IForEachLoopOperation (LoopKind.ForEach) (OperationKind.Loop, Type: null) (Syntax: 'For Each c. ... Next c.X')
  LoopControlVariable: 
    IFieldReferenceOperation: C.X As System.Int32 (OperationKind.FieldReference, Type: System.Int32) (Syntax: 'c.X')
      Instance Receiver: 
        IParameterReferenceOperation: c (OperationKind.ParameterReference, Type: C) (Syntax: 'c')
  Collection: 
    IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.Collections.IEnumerable, IsImplicit) (Syntax: 'args')
      Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: True, IsUserDefined: False) (MethodSymbol: null)
      Operand: 
        IParameterReferenceOperation: args (OperationKind.ParameterReference, Type: System.Int32()) (Syntax: 'args')
  Body: 
    IBlockOperation (0 statements) (OperationKind.Block, Type: null, IsImplicit) (Syntax: 'For Each c. ... Next c.X')
  NextVariables(1):
      IFieldReferenceOperation: C.X As System.Int32 (OperationKind.FieldReference, Type: System.Int32) (Syntax: 'c.X')
        Instance Receiver: 
          IParameterReferenceOperation: c (OperationKind.ParameterReference, Type: C) (Syntax: 'c')
]]>.Value

            Dim expectedDiagnostics = String.Empty

            VerifyOperationTreeAndDiagnosticsForTest(Of ForEachBlockSyntax)(source, expectedOperationTree, expectedDiagnostics)
        End Sub

    End Class
End Namespace
