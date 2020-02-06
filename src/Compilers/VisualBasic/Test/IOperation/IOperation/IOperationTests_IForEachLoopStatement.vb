' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

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
IForEachLoopOperation (LoopKind.ForEach, Continue Label Id: 0, Exit Label Id: 1) (OperationKind.Loop, Type: null) (Syntax: 'For Each s  ... Next')
  Locals: Local_1: s As System.String
  LoopControlVariable: 
    IVariableDeclaratorOperation (Symbol: s As System.String) (OperationKind.VariableDeclarator, Type: null) (Syntax: 's As String')
      Initializer: 
        null
  Collection: 
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
IForEachLoopOperation (LoopKind.ForEach, Continue Label Id: 0, Exit Label Id: 1) (OperationKind.Loop, Type: null) (Syntax: 'For Each it ... Next')
  Locals: Local_1: item As System.String
  LoopControlVariable: 
    IVariableDeclaratorOperation (Symbol: item As System.String) (OperationKind.VariableDeclarator, Type: null) (Syntax: 'item As String')
      Initializer: 
        null
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
IForEachLoopOperation (LoopKind.ForEach, Continue Label Id: 0, Exit Label Id: 1) (OperationKind.Loop, Type: null) (Syntax: 'For Each y  ... Next')
  Locals: Local_1: y As System.Char
  LoopControlVariable: 
    IVariableDeclaratorOperation (Symbol: y As System.Char) (OperationKind.VariableDeclarator, Type: null) (Syntax: 'y As Char')
      Initializer: 
        null
  Collection: 
    ILocalReferenceOperation: x (OperationKind.LocalReference, Type: System.String) (Syntax: 'x')
  Body: 
    IBlockOperation (1 statements) (OperationKind.Block, Type: null, IsImplicit) (Syntax: 'For Each y  ... Next')
      IConditionalOperation (OperationKind.Conditional, Type: null) (Syntax: 'If y = "B"c ... End If')
        Condition: 
          IBinaryOperation (BinaryOperatorKind.Equals, Checked) (OperationKind.Binary, Type: System.Boolean) (Syntax: 'y = "B"c')
            Left: 
              ILocalReferenceOperation: y (OperationKind.LocalReference, Type: System.Char) (Syntax: 'y')
            Right: 
              ILiteralOperation (OperationKind.Literal, Type: System.Char, Constant: B) (Syntax: '"B"c')
        WhenTrue: 
          IBlockOperation (1 statements) (OperationKind.Block, Type: null, IsImplicit) (Syntax: 'If y = "B"c ... End If')
            IBranchOperation (BranchKind.Break, Label Id: 1) (OperationKind.Branch, Type: null) (Syntax: 'Exit For')
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
IForEachLoopOperation (LoopKind.ForEach, Continue Label Id: 0, Exit Label Id: 1) (OperationKind.Loop, Type: null) (Syntax: 'For Each x  ... Next y, x')
  Locals: Local_1: x As System.String
  LoopControlVariable: 
    IVariableDeclaratorOperation (Symbol: x As System.String) (OperationKind.VariableDeclarator, Type: null) (Syntax: 'x As String')
      Initializer: 
        null
  Collection: 
    ILocalReferenceOperation: S (OperationKind.LocalReference, Type: System.String()) (Syntax: 'S')
  Body: 
    IBlockOperation (1 statements) (OperationKind.Block, Type: null, IsImplicit) (Syntax: 'For Each x  ... Next y, x')
      IForEachLoopOperation (LoopKind.ForEach, Continue Label Id: 2, Exit Label Id: 3) (OperationKind.Loop, Type: null) (Syntax: 'For Each y  ... Next y, x')
        Locals: Local_1: y As System.Char
        LoopControlVariable: 
          IVariableDeclaratorOperation (Symbol: y As System.Char) (OperationKind.VariableDeclarator, Type: null) (Syntax: 'y As Char')
            Initializer: 
              null
        Collection: 
          ILocalReferenceOperation: x (OperationKind.LocalReference, Type: System.String) (Syntax: 'x')
        Body: 
          IBlockOperation (2 statements) (OperationKind.Block, Type: null, IsImplicit) (Syntax: 'For Each y  ... Next y, x')
            IConditionalOperation (OperationKind.Conditional, Type: null) (Syntax: 'If y = "B"c ... End If')
              Condition: 
                IBinaryOperation (BinaryOperatorKind.Equals, Checked) (OperationKind.Binary, Type: System.Boolean) (Syntax: 'y = "B"c')
                  Left: 
                    ILocalReferenceOperation: y (OperationKind.LocalReference, Type: System.Char) (Syntax: 'y')
                  Right: 
                    ILiteralOperation (OperationKind.Literal, Type: System.Char, Constant: B) (Syntax: '"B"c')
              WhenTrue: 
                IBlockOperation (1 statements) (OperationKind.Block, Type: null, IsImplicit) (Syntax: 'If y = "B"c ... End If')
                  IBranchOperation (BranchKind.Continue, Label Id: 2) (OperationKind.Branch, Type: null) (Syntax: 'Continue For')
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
IForEachLoopOperation (LoopKind.ForEach, Continue Label Id: 0, Exit Label Id: 1) (OperationKind.Loop, Type: null) (Syntax: 'For Each y  ... Next')
  Locals: Local_1: y As System.Int32
  LoopControlVariable: 
    IVariableDeclaratorOperation (Symbol: y As System.Int32) (OperationKind.VariableDeclarator, Type: null) (Syntax: 'y As Integer')
      Initializer: 
        null
  Collection: 
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
IForEachLoopOperation (LoopKind.ForEach, Continue Label Id: 0, Exit Label Id: 1) (OperationKind.Loop, Type: null) (Syntax: 'For Each x  ... Next')
  Locals: Local_1: x As System.String
  LoopControlVariable: 
    IVariableDeclaratorOperation (Symbol: x As System.String) (OperationKind.VariableDeclarator, Type: null) (Syntax: 'x As String')
      Initializer: 
        null
  Collection: 
    ILocalReferenceOperation: S (OperationKind.LocalReference, Type: System.String()) (Syntax: 'S')
  Body: 
    IBlockOperation (1 statements) (OperationKind.Block, Type: null, IsImplicit) (Syntax: 'For Each x  ... Next')
      IForEachLoopOperation (LoopKind.ForEach, Continue Label Id: 2, Exit Label Id: 3) (OperationKind.Loop, Type: null) (Syntax: 'For Each y  ... Next')
        Locals: Local_1: y As System.Char
        LoopControlVariable: 
          IVariableDeclaratorOperation (Symbol: y As System.Char) (OperationKind.VariableDeclarator, Type: null) (Syntax: 'y As Char')
            Initializer: 
              null
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
IForEachLoopOperation (LoopKind.ForEach, Continue Label Id: 0, Exit Label Id: 1) (OperationKind.Loop, Type: null) (Syntax: 'For Each x  ... Next')
  Locals: Local_1: x As System.Object
  LoopControlVariable: 
    IVariableDeclaratorOperation (Symbol: x As System.Object) (OperationKind.VariableDeclarator, Type: null) (Syntax: 'x')
      Initializer: 
        null
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
IForEachLoopOperation (LoopKind.ForEach, Continue Label Id: 0, Exit Label Id: 1) (OperationKind.Loop, Type: null) (Syntax: 'For Each y  ... Next')
  Locals: Local_1: y As System.Char
  LoopControlVariable: 
    IVariableDeclaratorOperation (Symbol: y As System.Char) (OperationKind.VariableDeclarator, Type: null) (Syntax: 'y')
      Initializer: 
        null
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
IForEachLoopOperation (LoopKind.ForEach, Continue Label Id: 0, Exit Label Id: 1) (OperationKind.Loop, Type: null) (Syntax: 'For Each x  ... Next')
  Locals: Local_1: x As System.Object
  LoopControlVariable: 
    IVariableDeclaratorOperation (Symbol: x As System.Object) (OperationKind.VariableDeclarator, Type: null) (Syntax: 'x')
      Initializer: 
        null
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
IForEachLoopOperation (LoopKind.ForEach, Continue Label Id: 0, Exit Label Id: 1) (OperationKind.Loop, Type: null) (Syntax: 'For Each co ... Next')
  Locals: Local_1: country As <anonymous type: Key CountryName As System.String, Key CustomersInCountry As System.Collections.Generic.IEnumerable(Of Customer), Key Count As System.Int32>
  LoopControlVariable: 
    IVariableDeclaratorOperation (Symbol: country As <anonymous type: Key CountryName As System.String, Key CustomersInCountry As System.Collections.Generic.IEnumerable(Of Customer), Key Count As System.Int32>) (OperationKind.VariableDeclarator, Type: null) (Syntax: 'country')
      Initializer: 
        null
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
                  IBinaryOperation (BinaryOperatorKind.Concatenate, Checked) (OperationKind.Binary, Type: System.String) (Syntax: 'country.Cou ... untry.Count')
                    Left: 
                      IBinaryOperation (BinaryOperatorKind.Concatenate, Checked) (OperationKind.Binary, Type: System.String) (Syntax: 'country.Cou ... & " count="')
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
      IForEachLoopOperation (LoopKind.ForEach, Continue Label Id: 2, Exit Label Id: 3) (OperationKind.Loop, Type: null) (Syntax: 'For Each cu ... Next')
        Locals: Local_1: customer As Customer
        LoopControlVariable: 
          IVariableDeclaratorOperation (Symbol: customer As Customer) (OperationKind.VariableDeclarator, Type: null) (Syntax: 'customer')
            Initializer: 
              null
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
                        IBinaryOperation (BinaryOperatorKind.Concatenate, Checked) (OperationKind.Binary, Type: System.String) (Syntax: '"   " & cus ... stomer.City')
                          Left: 
                            IBinaryOperation (BinaryOperatorKind.Concatenate, Checked) (OperationKind.Binary, Type: System.String) (Syntax: '"   " & cus ... Name & "  "')
                              Left: 
                                IBinaryOperation (BinaryOperatorKind.Concatenate, Checked) (OperationKind.Binary, Type: System.String) (Syntax: '"   " & cus ... CompanyName')
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
IForEachLoopOperation (LoopKind.ForEach, Continue Label Id: 0, Exit Label Id: 1) (OperationKind.Loop, Type: null) (Syntax: 'For Each [C ... Next')
  Locals: Local_1: Custom As System.Int32
  LoopControlVariable: 
    IVariableDeclaratorOperation (Symbol: Custom As System.Int32) (OperationKind.VariableDeclarator, Type: null) (Syntax: '[Custom]')
      Initializer: 
        null
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
      IBranchOperation (BranchKind.Break, Label Id: 1) (OperationKind.Branch, Type: null) (Syntax: 'Exit For')
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
IForEachLoopOperation (LoopKind.ForEach, Continue Label Id: 0, Exit Label Id: 1) (OperationKind.Loop, Type: null) (Syntax: 'For Each x  ... Next')
  Locals: Local_1: x As System.Object
  LoopControlVariable: 
    IVariableDeclaratorOperation (Symbol: x As System.Object) (OperationKind.VariableDeclarator, Type: null) (Syntax: 'x')
      Initializer: 
        null
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
IForEachLoopOperation (LoopKind.ForEach, Continue Label Id: 0, Exit Label Id: 1) (OperationKind.Loop, Type: null) (Syntax: 'For Each x  ... Next')
  Locals: Local_1: x As System.Int32
  LoopControlVariable: 
    IVariableDeclaratorOperation (Symbol: x As System.Int32) (OperationKind.VariableDeclarator, Type: null) (Syntax: 'x')
      Initializer: 
        null
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
        Public Sub IForEachLoopStatement_Lambda()
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
IForEachLoopOperation (LoopKind.ForEach, Continue Label Id: 0, Exit Label Id: 1) (OperationKind.Loop, Type: null) (Syntax: 'For Each el ... ambda_local')
  LoopControlVariable: 
    ILocalReferenceOperation: element_lambda_local (OperationKind.LocalReference, Type: System.Int32) (Syntax: 'element_lambda_local')
  Collection: 
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
IForEachLoopOperation (LoopKind.ForEach, Continue Label Id: 0, Exit Label Id: 1) (OperationKind.Loop, Type: null, IsInvalid) (Syntax: 'For Each el ... Next')
  Locals: Local_1: element As System.Int32
  LoopControlVariable: 
    IVariableDeclaratorOperation (Symbol: element As System.Int32) (OperationKind.VariableDeclarator, Type: null) (Syntax: 'element As Integer')
      Initializer: 
        null
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
IForEachLoopOperation (LoopKind.ForEach, Continue Label Id: 0, Exit Label Id: 1) (OperationKind.Loop, Type: null) (Syntax: 'For Each s  ... Next')
  Locals: Local_1: s As System.String
  LoopControlVariable: 
    IVariableDeclaratorOperation (Symbol: s As System.String) (OperationKind.VariableDeclarator, Type: null) (Syntax: 's As String')
      Initializer: 
        null
  Collection: 
    ILocalReferenceOperation: arr (OperationKind.LocalReference, Type: System.String()) (Syntax: 'arr')
  Body: 
    IBlockOperation (2 statements) (OperationKind.Block, Type: null, IsImplicit) (Syntax: 'For Each s  ... Next')
      IConditionalOperation (OperationKind.Conditional, Type: null) (Syntax: 'If (s = "on ... End If')
        Condition: 
          IParenthesizedOperation (OperationKind.Parenthesized, Type: System.Boolean) (Syntax: '(s = "one")')
            Operand: 
              IBinaryOperation (BinaryOperatorKind.Equals, Checked) (OperationKind.Binary, Type: System.Boolean) (Syntax: 's = "one"')
                Left: 
                  ILocalReferenceOperation: s (OperationKind.LocalReference, Type: System.String) (Syntax: 's')
                Right: 
                  ILiteralOperation (OperationKind.Literal, Type: System.String, Constant: "one") (Syntax: '"one"')
        WhenTrue: 
          IBlockOperation (1 statements) (OperationKind.Block, Type: null, IsImplicit) (Syntax: 'If (s = "on ... End If')
            IThrowOperation (OperationKind.Throw, Type: null) (Syntax: 'Throw New Exception')
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
IForEachLoopOperation (LoopKind.ForEach, Continue Label Id: 0, Exit Label Id: 1) (OperationKind.Loop, Type: null) (Syntax: 'For Each o  ... Next')
  Locals: Local_1: o As System.Object
  LoopControlVariable: 
    IVariableDeclaratorOperation (Symbol: o As System.Object) (OperationKind.VariableDeclarator, Type: null) (Syntax: 'o')
      Initializer: 
        null
  Collection: 
    IParameterReferenceOperation: c (OperationKind.ParameterReference, Type: System.Object()) (Syntax: 'c')
  Body: 
    IBlockOperation (1 statements) (OperationKind.Block, Type: null, IsImplicit) (Syntax: 'For Each o  ... Next')
      IConditionalOperation (OperationKind.Conditional, Type: null) (Syntax: 'If o IsNot  ... Return True')
        Condition: 
          IBinaryOperation (BinaryOperatorKind.NotEquals) (OperationKind.Binary, Type: System.Boolean) (Syntax: 'o IsNot Nothing')
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
IForEachLoopOperation (LoopKind.ForEach, Continue Label Id: 0, Exit Label Id: 1) (OperationKind.Loop, Type: null) (Syntax: 'For Each X  ... Next X')
  LoopControlVariable: 
    IFieldReferenceOperation: C.X As System.Int32 (OperationKind.FieldReference, Type: System.Int32) (Syntax: 'X')
      Instance Receiver: 
        IInstanceReferenceOperation (ReferenceKind: ContainingTypeInstance) (OperationKind.InstanceReference, Type: C, IsImplicit) (Syntax: 'X')
  Collection: 
    IParameterReferenceOperation: args (OperationKind.ParameterReference, Type: System.Int32()) (Syntax: 'args')
  Body: 
    IBlockOperation (0 statements) (OperationKind.Block, Type: null, IsImplicit) (Syntax: 'For Each X  ... Next X')
  NextVariables(1):
      IFieldReferenceOperation: C.X As System.Int32 (OperationKind.FieldReference, Type: System.Int32) (Syntax: 'X')
        Instance Receiver: 
          IInstanceReferenceOperation (ReferenceKind: ContainingTypeInstance) (OperationKind.InstanceReference, Type: C, IsImplicit) (Syntax: 'X')
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
IForEachLoopOperation (LoopKind.ForEach, Continue Label Id: 0, Exit Label Id: 1) (OperationKind.Loop, Type: null) (Syntax: 'For Each c. ... Next c.X')
  LoopControlVariable: 
    IFieldReferenceOperation: C.X As System.Int32 (OperationKind.FieldReference, Type: System.Int32) (Syntax: 'c.X')
      Instance Receiver: 
        IParameterReferenceOperation: c (OperationKind.ParameterReference, Type: C) (Syntax: 'c')
  Collection: 
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

        <CompilerTrait(CompilerFeature.IOperation)>
        <Fact(), WorkItem(17602, "https://github.com/dotnet/roslyn/issues/17602")>
        Public Sub IForEachLoopStatement_CastArrayToIEnumerable()
            Dim source = <![CDATA[
Option Infer On
Imports System.Collections

Class Program
    Public Shared Sub Main(args As String(), i As String)
        For Each arg As String In DirectCast(args, IEnumerable)'BIND:"For Each arg As String In DirectCast(args, IEnumerable)"
            i = arg
        Next
    End Sub
End Class
    ]]>.Value

            Dim expectedOperationTree = <![CDATA[
IForEachLoopOperation (LoopKind.ForEach, Continue Label Id: 0, Exit Label Id: 1) (OperationKind.Loop, Type: null) (Syntax: 'For Each ar ... Next')
  Locals: Local_1: arg As System.String
  LoopControlVariable: 
    IVariableDeclaratorOperation (Symbol: arg As System.String) (OperationKind.VariableDeclarator, Type: null) (Syntax: 'arg As String')
      Initializer: 
        null
  Collection: 
    IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.Collections.IEnumerable) (Syntax: 'DirectCast( ... Enumerable)')
      Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: True, IsUserDefined: False) (MethodSymbol: null)
      Operand: 
        IParameterReferenceOperation: args (OperationKind.ParameterReference, Type: System.String()) (Syntax: 'args')
  Body: 
    IBlockOperation (1 statements) (OperationKind.Block, Type: null, IsImplicit) (Syntax: 'For Each ar ... Next')
      IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'i = arg')
        Expression: 
          ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.String, IsImplicit) (Syntax: 'i = arg')
            Left: 
              IParameterReferenceOperation: i (OperationKind.ParameterReference, Type: System.String) (Syntax: 'i')
            Right: 
              ILocalReferenceOperation: arg (OperationKind.LocalReference, Type: System.String) (Syntax: 'arg')
  NextVariables(0)
]]>.Value

            Dim expectedDiagnostics = String.Empty

            VerifyOperationTreeAndDiagnosticsForTest(Of ForEachBlockSyntax)(source, expectedOperationTree, expectedDiagnostics)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation)>
        <Fact(), WorkItem(17602, "https://github.com/dotnet/roslyn/issues/17602")>
        Public Sub IForEachLoopStatement_CastCollectionToIEnumerable()
            Dim source = <![CDATA[
Option Infer On
Imports System.Collections.Generic

Class Program
    Public Shared Sub Main(args As List(Of String), i As String)
        For Each arg As String In DirectCast(args, IEnumerable(Of String))'BIND:"For Each arg As String In DirectCast(args, IEnumerable(Of String))"
            i = arg
        Next
    End Sub
End Class
    ]]>.Value

            Dim expectedOperationTree = <![CDATA[
IForEachLoopOperation (LoopKind.ForEach, Continue Label Id: 0, Exit Label Id: 1) (OperationKind.Loop, Type: null) (Syntax: 'For Each ar ... Next')
  Locals: Local_1: arg As System.String
  LoopControlVariable: 
    IVariableDeclaratorOperation (Symbol: arg As System.String) (OperationKind.VariableDeclarator, Type: null) (Syntax: 'arg As String')
      Initializer: 
        null
  Collection: 
    IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.Collections.Generic.IEnumerable(Of System.String)) (Syntax: 'DirectCast( ... Of String))')
      Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: True, IsUserDefined: False) (MethodSymbol: null)
      Operand: 
        IParameterReferenceOperation: args (OperationKind.ParameterReference, Type: System.Collections.Generic.List(Of System.String)) (Syntax: 'args')
  Body: 
    IBlockOperation (1 statements) (OperationKind.Block, Type: null, IsImplicit) (Syntax: 'For Each ar ... Next')
      IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'i = arg')
        Expression: 
          ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.String, IsImplicit) (Syntax: 'i = arg')
            Left: 
              IParameterReferenceOperation: i (OperationKind.ParameterReference, Type: System.String) (Syntax: 'i')
            Right: 
              ILocalReferenceOperation: arg (OperationKind.LocalReference, Type: System.String) (Syntax: 'arg')
  NextVariables(0)
]]>.Value

            Dim expectedDiagnostics = String.Empty

            VerifyOperationTreeAndDiagnosticsForTest(Of ForEachBlockSyntax)(source, expectedOperationTree, expectedDiagnostics)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation)>
        <Fact(), WorkItem(17602, "https://github.com/dotnet/roslyn/issues/17602")>
        Public Sub IForEachLoopStatement_InvalidLoopControlVariableDeclaration()
            Dim source = <![CDATA[
Imports System
Imports System.Collections.Generic
Imports System.Linq

Class C
    Sub M(args As Integer())
        Dim i as Integer = 0
        For Each i as Integer In args'BIND:"For Each i as Integer In args"
        Next i
    End Sub
End Class]]>.Value

            Dim expectedOperationTree = <![CDATA[
IForEachLoopOperation (LoopKind.ForEach, Continue Label Id: 0, Exit Label Id: 1) (OperationKind.Loop, Type: null, IsInvalid) (Syntax: 'For Each i  ... Next i')
  Locals: Local_1: i As System.Int32
  LoopControlVariable: 
    IVariableDeclaratorOperation (Symbol: i As System.Int32) (OperationKind.VariableDeclarator, Type: null, IsInvalid) (Syntax: 'i as Integer')
      Initializer: 
        null
  Collection: 
    IParameterReferenceOperation: args (OperationKind.ParameterReference, Type: System.Int32()) (Syntax: 'args')
  Body: 
    IBlockOperation (0 statements) (OperationKind.Block, Type: null, IsInvalid, IsImplicit) (Syntax: 'For Each i  ... Next i')
  NextVariables(1):
      ILocalReferenceOperation: i (OperationKind.LocalReference, Type: System.Int32) (Syntax: 'i')
]]>.Value

            Dim expectedDiagnostics = <![CDATA[
BC30616: Variable 'i' hides a variable in an enclosing block.
        For Each i as Integer In args'BIND:"For Each i as Integer In args"
                 ~
]]>.Value

            VerifyOperationTreeAndDiagnosticsForTest(Of ForEachBlockSyntax)(source, expectedOperationTree, expectedDiagnostics)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation, CompilerFeature.Dataflow)>
        <Fact()>
        Public Sub ForEachFlow_01()
            Dim source = <![CDATA[
Imports System
Public Class C
    Sub M(x As C(), y as C(), result As C) 'BIND:"Sub M"
        For Each z in If(x, y)
            result = z
        Next
    End Sub
End Class]]>.Value

            Dim expectedDiagnostics = String.Empty

            Dim expectedFlowGraph = <![CDATA[
Block[B0] - Entry
    Statements (0)
    Next (Regular) Block[B1]
        Entering: {R1} {R2} {R3}

.locals {R1}
{
    CaptureIds: [2]
    .locals {R2}
    {
        CaptureIds: [1]
        .locals {R3}
        {
            CaptureIds: [0]
            Block[B1] - Block
                Predecessors: [B0]
                Statements (1)
                    IFlowCaptureOperation: 0 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'x')
                      Value: 
                        IParameterReferenceOperation: x (OperationKind.ParameterReference, Type: C()) (Syntax: 'x')

                Jump if True (Regular) to Block[B3]
                    IIsNullOperation (OperationKind.IsNull, Type: System.Boolean, IsImplicit) (Syntax: 'x')
                      Operand: 
                        IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: C(), IsImplicit) (Syntax: 'x')
                    Leaving: {R3}

                Next (Regular) Block[B2]
            Block[B2] - Block
                Predecessors: [B1]
                Statements (1)
                    IFlowCaptureOperation: 1 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'x')
                      Value: 
                        IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: C(), IsImplicit) (Syntax: 'x')

                Next (Regular) Block[B4]
                    Leaving: {R3}
        }

        Block[B3] - Block
            Predecessors: [B1]
            Statements (1)
                IFlowCaptureOperation: 1 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'y')
                  Value: 
                    IParameterReferenceOperation: y (OperationKind.ParameterReference, Type: C()) (Syntax: 'y')

            Next (Regular) Block[B4]
        Block[B4] - Block
            Predecessors: [B2] [B3]
            Statements (1)
                IFlowCaptureOperation: 2 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'If(x, y)')
                  Value: 
                    IInvocationOperation ( Function System.Array.GetEnumerator() As System.Collections.IEnumerator) (OperationKind.Invocation, Type: System.Collections.IEnumerator, IsImplicit) (Syntax: 'If(x, y)')
                      Instance Receiver: 
                        IFlowCaptureReferenceOperation: 1 (OperationKind.FlowCaptureReference, Type: C(), IsImplicit) (Syntax: 'If(x, y)')
                      Arguments(0)

            Next (Regular) Block[B5]
                Leaving: {R2}
    }

    Block[B5] - Block
        Predecessors: [B4] [B6]
        Statements (0)
        Jump if False (Regular) to Block[B7]
            IInvocationOperation (virtual Function System.Collections.IEnumerator.MoveNext() As System.Boolean) (OperationKind.Invocation, Type: System.Boolean, IsImplicit) (Syntax: 'If(x, y)')
              Instance Receiver: 
                IFlowCaptureReferenceOperation: 2 (OperationKind.FlowCaptureReference, Type: System.Collections.IEnumerator, IsImplicit) (Syntax: 'If(x, y)')
              Arguments(0)
            Leaving: {R1}

        Next (Regular) Block[B6]
            Entering: {R4}

    .locals {R4}
    {
        Locals: [z As C]
        Block[B6] - Block
            Predecessors: [B5]
            Statements (2)
                ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: null, IsImplicit) (Syntax: 'z')
                  Left: 
                    ILocalReferenceOperation: z (IsDeclaration: True) (OperationKind.LocalReference, Type: C, IsImplicit) (Syntax: 'z')
                  Right: 
                    IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: C, IsImplicit) (Syntax: 'z')
                      Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: True, IsUserDefined: False) (MethodSymbol: null)
                        (NarrowingReference)
                      Operand: 
                        IPropertyReferenceOperation: ReadOnly Property System.Collections.IEnumerator.Current As System.Object (OperationKind.PropertyReference, Type: System.Object, IsImplicit) (Syntax: 'z')
                          Instance Receiver: 
                            IFlowCaptureReferenceOperation: 2 (OperationKind.FlowCaptureReference, Type: System.Collections.IEnumerator, IsImplicit) (Syntax: 'If(x, y)')

                IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'result = z')
                  Expression: 
                    ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: C, IsImplicit) (Syntax: 'result = z')
                      Left: 
                        IParameterReferenceOperation: result (OperationKind.ParameterReference, Type: C) (Syntax: 'result')
                      Right: 
                        ILocalReferenceOperation: z (OperationKind.LocalReference, Type: C) (Syntax: 'z')

            Next (Regular) Block[B5]
                Leaving: {R4}
    }
}

Block[B7] - Exit
    Predecessors: [B5]
    Statements (0)
]]>.Value

            VerifyFlowGraphAndDiagnosticsForTest(Of MethodBlockSyntax)(source, expectedFlowGraph, expectedDiagnostics)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation, CompilerFeature.Dataflow)>
        <Fact()>
        Public Sub ForEachFlow_02()
            Dim source = <![CDATA[
Imports System
Public Class C
    Sub M(x As String) 'BIND:"Sub M"
        For Each z As String in x
            z?.ToString()
        Next
    End Sub
End Class]]>.Value

            Dim expectedDiagnostics = String.Empty

            Dim expectedFlowGraph = <![CDATA[
Block[B0] - Entry
    Statements (0)
    Next (Regular) Block[B1]
        Entering: {R1}

.locals {R1}
{
    CaptureIds: [0]
    Block[B1] - Block
        Predecessors: [B0]
        Statements (1)
            IFlowCaptureOperation: 0 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'x')
              Value: 
                IInvocationOperation ( Function System.String.GetEnumerator() As System.CharEnumerator) (OperationKind.Invocation, Type: System.CharEnumerator, IsImplicit) (Syntax: 'x')
                  Instance Receiver: 
                    IParameterReferenceOperation: x (OperationKind.ParameterReference, Type: System.String) (Syntax: 'x')
                  Arguments(0)

        Next (Regular) Block[B2]
            Entering: {R2} {R3}

    .try {R2, R3}
    {
        Block[B2] - Block
            Predecessors: [B1] [B4] [B5]
            Statements (0)
            Jump if False (Regular) to Block[B9]
                IInvocationOperation ( Function System.CharEnumerator.MoveNext() As System.Boolean) (OperationKind.Invocation, Type: System.Boolean, IsImplicit) (Syntax: 'x')
                  Instance Receiver: 
                    IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: System.CharEnumerator, IsImplicit) (Syntax: 'x')
                  Arguments(0)
                Finalizing: {R6}
                Leaving: {R3} {R2} {R1}

            Next (Regular) Block[B3]
                Entering: {R4}

        .locals {R4}
        {
            Locals: [z As System.String]
            Block[B3] - Block
                Predecessors: [B2]
                Statements (1)
                    ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: null, IsImplicit) (Syntax: 'z As String')
                      Left: 
                        ILocalReferenceOperation: z (IsDeclaration: True) (OperationKind.LocalReference, Type: System.String, IsImplicit) (Syntax: 'z As String')
                      Right: 
                        IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.String, IsImplicit) (Syntax: 'z As String')
                          Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                            (WideningString)
                          Operand: 
                            IPropertyReferenceOperation: ReadOnly Property System.CharEnumerator.Current As System.Char (OperationKind.PropertyReference, Type: System.Char, IsImplicit) (Syntax: 'z As String')
                              Instance Receiver: 
                                IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: System.CharEnumerator, IsImplicit) (Syntax: 'x')

                Next (Regular) Block[B4]
                    Entering: {R5}

            .locals {R5}
            {
                CaptureIds: [1]
                Block[B4] - Block
                    Predecessors: [B3]
                    Statements (1)
                        IFlowCaptureOperation: 1 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'z')
                          Value: 
                            ILocalReferenceOperation: z (OperationKind.LocalReference, Type: System.String) (Syntax: 'z')

                    Jump if True (Regular) to Block[B2]
                        IIsNullOperation (OperationKind.IsNull, Type: System.Boolean, IsImplicit) (Syntax: 'z')
                          Operand: 
                            IFlowCaptureReferenceOperation: 1 (OperationKind.FlowCaptureReference, Type: System.String, IsImplicit) (Syntax: 'z')
                        Leaving: {R5} {R4}

                    Next (Regular) Block[B5]
                Block[B5] - Block
                    Predecessors: [B4]
                    Statements (1)
                        IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'z?.ToString()')
                          Expression: 
                            IInvocationOperation (virtual Function System.String.ToString() As System.String) (OperationKind.Invocation, Type: System.String) (Syntax: '.ToString()')
                              Instance Receiver: 
                                IFlowCaptureReferenceOperation: 1 (OperationKind.FlowCaptureReference, Type: System.String, IsImplicit) (Syntax: 'z')
                              Arguments(0)

                    Next (Regular) Block[B2]
                        Leaving: {R5} {R4}
            }
        }
    }
    .finally {R6}
    {
        Block[B6] - Block
            Predecessors (0)
            Statements (0)
            Jump if True (Regular) to Block[B8]
                IIsNullOperation (OperationKind.IsNull, Type: System.Boolean, IsImplicit) (Syntax: 'x')
                  Operand: 
                    IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: System.CharEnumerator, IsImplicit) (Syntax: 'x')

            Next (Regular) Block[B7]
        Block[B7] - Block
            Predecessors: [B6]
            Statements (1)
                IInvocationOperation (virtual Sub System.IDisposable.Dispose()) (OperationKind.Invocation, Type: System.Void, IsImplicit) (Syntax: 'x')
                  Instance Receiver: 
                    IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.IDisposable, IsImplicit) (Syntax: 'x')
                      Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: True, IsUserDefined: False) (MethodSymbol: null)
                        (WideningReference)
                      Operand: 
                        IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: System.CharEnumerator, IsImplicit) (Syntax: 'x')
                  Arguments(0)

            Next (Regular) Block[B8]
        Block[B8] - Block
            Predecessors: [B6] [B7]
            Statements (0)
            Next (StructuredExceptionHandling) Block[null]
    }
}

Block[B9] - Exit
    Predecessors: [B2]
    Statements (0)
]]>.Value

            VerifyFlowGraphAndDiagnosticsForTest(Of MethodBlockSyntax)(source, expectedFlowGraph, expectedDiagnostics)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation, CompilerFeature.Dataflow)>
        <Fact()>
        Public Sub ForEachFlow_03()
            Dim source = <![CDATA[
Imports System
Public Class C
    Sub M(x As Integer(,), result As Long) 'BIND:"Sub M"
        For Each z As Long in x
            result = z
        Next
    End Sub
End Class]]>.Value

            Dim expectedDiagnostics = String.Empty

            Dim expectedFlowGraph = <![CDATA[
Block[B0] - Entry
    Statements (0)
    Next (Regular) Block[B1]
        Entering: {R1}

.locals {R1}
{
    CaptureIds: [0]
    Block[B1] - Block
        Predecessors: [B0]
        Statements (1)
            IFlowCaptureOperation: 0 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'x')
              Value: 
                IInvocationOperation ( Function System.Array.GetEnumerator() As System.Collections.IEnumerator) (OperationKind.Invocation, Type: System.Collections.IEnumerator, IsImplicit) (Syntax: 'x')
                  Instance Receiver: 
                    IParameterReferenceOperation: x (OperationKind.ParameterReference, Type: System.Int32(,)) (Syntax: 'x')
                  Arguments(0)

        Next (Regular) Block[B2]
    Block[B2] - Block
        Predecessors: [B1] [B3]
        Statements (0)
        Jump if False (Regular) to Block[B4]
            IInvocationOperation (virtual Function System.Collections.IEnumerator.MoveNext() As System.Boolean) (OperationKind.Invocation, Type: System.Boolean, IsImplicit) (Syntax: 'x')
              Instance Receiver: 
                IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: System.Collections.IEnumerator, IsImplicit) (Syntax: 'x')
              Arguments(0)
            Leaving: {R1}

        Next (Regular) Block[B3]
            Entering: {R2}

    .locals {R2}
    {
        Locals: [z As System.Int64]
        Block[B3] - Block
            Predecessors: [B2]
            Statements (2)
                ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: null, IsImplicit) (Syntax: 'z As Long')
                  Left: 
                    ILocalReferenceOperation: z (IsDeclaration: True) (OperationKind.LocalReference, Type: System.Int64, IsImplicit) (Syntax: 'z As Long')
                  Right: 
                    IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.Int64, IsImplicit) (Syntax: 'z As Long')
                      Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: True, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                        (WideningNumeric)
                      Operand: 
                        IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.Int32, IsImplicit) (Syntax: 'z As Long')
                          Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                            (NarrowingValue)
                          Operand: 
                            IPropertyReferenceOperation: ReadOnly Property System.Collections.IEnumerator.Current As System.Object (OperationKind.PropertyReference, Type: System.Object, IsImplicit) (Syntax: 'z As Long')
                              Instance Receiver: 
                                IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: System.Collections.IEnumerator, IsImplicit) (Syntax: 'x')

                IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'result = z')
                  Expression: 
                    ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Int64, IsImplicit) (Syntax: 'result = z')
                      Left: 
                        IParameterReferenceOperation: result (OperationKind.ParameterReference, Type: System.Int64) (Syntax: 'result')
                      Right: 
                        ILocalReferenceOperation: z (OperationKind.LocalReference, Type: System.Int64) (Syntax: 'z')

            Next (Regular) Block[B2]
                Leaving: {R2}
    }
}

Block[B4] - Exit
    Predecessors: [B2]
    Statements (0)
]]>.Value

            VerifyFlowGraphAndDiagnosticsForTest(Of MethodBlockSyntax)(source, expectedFlowGraph, expectedDiagnostics)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation, CompilerFeature.Dataflow)>
        <Fact()>
        Public Sub ForEachFlow_04()
            Dim source = <![CDATA[
Imports System
Public Class C
    Sub M(x As Enumerable, result As Long) 'BIND:"Sub M"
        For Each z As Long in x
            result = z
        Next
    End Sub
End Class
Public Structure Enumerable
    Public Function GetEnumerator() As Enumerator
        Return New Enumerator()
    End Function
End Structure

Public Structure Enumerator
    Public ReadOnly Property Current As Integer
        Get
            Return 1
        End Get
    End Property

    Public Function MoveNext() As Boolean
        Return False
    End Function
End Structure
]]>.Value

            Dim expectedDiagnostics = String.Empty

            Dim expectedFlowGraph = <![CDATA[
Block[B0] - Entry
    Statements (0)
    Next (Regular) Block[B1]
        Entering: {R1}

.locals {R1}
{
    CaptureIds: [0]
    Block[B1] - Block
        Predecessors: [B0]
        Statements (1)
            IFlowCaptureOperation: 0 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'x')
              Value: 
                IInvocationOperation ( Function Enumerable.GetEnumerator() As Enumerator) (OperationKind.Invocation, Type: Enumerator, IsImplicit) (Syntax: 'x')
                  Instance Receiver: 
                    IParameterReferenceOperation: x (OperationKind.ParameterReference, Type: Enumerable) (Syntax: 'x')
                  Arguments(0)

        Next (Regular) Block[B2]
    Block[B2] - Block
        Predecessors: [B1] [B3]
        Statements (0)
        Jump if False (Regular) to Block[B4]
            IInvocationOperation ( Function Enumerator.MoveNext() As System.Boolean) (OperationKind.Invocation, Type: System.Boolean, IsImplicit) (Syntax: 'x')
              Instance Receiver: 
                IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: Enumerator, IsImplicit) (Syntax: 'x')
              Arguments(0)
            Leaving: {R1}

        Next (Regular) Block[B3]
            Entering: {R2}

    .locals {R2}
    {
        Locals: [z As System.Int64]
        Block[B3] - Block
            Predecessors: [B2]
            Statements (2)
                ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: null, IsImplicit) (Syntax: 'z As Long')
                  Left: 
                    ILocalReferenceOperation: z (IsDeclaration: True) (OperationKind.LocalReference, Type: System.Int64, IsImplicit) (Syntax: 'z As Long')
                  Right: 
                    IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.Int64, IsImplicit) (Syntax: 'z As Long')
                      Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: True, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                        (WideningNumeric)
                      Operand: 
                        IPropertyReferenceOperation: ReadOnly Property Enumerator.Current As System.Int32 (OperationKind.PropertyReference, Type: System.Int32, IsImplicit) (Syntax: 'z As Long')
                          Instance Receiver: 
                            IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: Enumerator, IsImplicit) (Syntax: 'x')

                IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'result = z')
                  Expression: 
                    ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Int64, IsImplicit) (Syntax: 'result = z')
                      Left: 
                        IParameterReferenceOperation: result (OperationKind.ParameterReference, Type: System.Int64) (Syntax: 'result')
                      Right: 
                        ILocalReferenceOperation: z (OperationKind.LocalReference, Type: System.Int64) (Syntax: 'z')

            Next (Regular) Block[B2]
                Leaving: {R2}
    }
}

Block[B4] - Exit
    Predecessors: [B2]
    Statements (0)
]]>.Value

            VerifyFlowGraphAndDiagnosticsForTest(Of MethodBlockSyntax)(source, expectedFlowGraph, expectedDiagnostics)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation, CompilerFeature.Dataflow)>
        <Fact()>
        Public Sub ForEachFlow_05()
            Dim source = <![CDATA[
Imports System
Public Class C
    Sub M(x As Enumerable, result As Integer) 'BIND:"Sub M"
        For Each z As Integer in x
            result = z
        Next
    End Sub
End Class
Public Structure Enumerable
    Public Function GetEnumerator() As Enumerator
        Return New Enumerator()
    End Function
End Structure

Public Structure Enumerator
    Implements System.IDisposable

    Public ReadOnly Property Current As Integer
        Get
            Return 1
        End Get
    End Property

    Public Sub Dispose() Implements IDisposable.Dispose
    End Sub

    Public Function MoveNext() As Boolean
        Return False
    End Function
End Structure
]]>.Value

            Dim expectedDiagnostics = String.Empty

            Dim expectedFlowGraph = <![CDATA[
Block[B0] - Entry
    Statements (0)
    Next (Regular) Block[B1]
        Entering: {R1}

.locals {R1}
{
    CaptureIds: [0]
    Block[B1] - Block
        Predecessors: [B0]
        Statements (1)
            IFlowCaptureOperation: 0 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'x')
              Value: 
                IInvocationOperation ( Function Enumerable.GetEnumerator() As Enumerator) (OperationKind.Invocation, Type: Enumerator, IsImplicit) (Syntax: 'x')
                  Instance Receiver: 
                    IParameterReferenceOperation: x (OperationKind.ParameterReference, Type: Enumerable) (Syntax: 'x')
                  Arguments(0)

        Next (Regular) Block[B2]
            Entering: {R2} {R3}

    .try {R2, R3}
    {
        Block[B2] - Block
            Predecessors: [B1] [B3]
            Statements (0)
            Jump if False (Regular) to Block[B5]
                IInvocationOperation ( Function Enumerator.MoveNext() As System.Boolean) (OperationKind.Invocation, Type: System.Boolean, IsImplicit) (Syntax: 'x')
                  Instance Receiver: 
                    IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: Enumerator, IsImplicit) (Syntax: 'x')
                  Arguments(0)
                Finalizing: {R5}
                Leaving: {R3} {R2} {R1}

            Next (Regular) Block[B3]
                Entering: {R4}

        .locals {R4}
        {
            Locals: [z As System.Int32]
            Block[B3] - Block
                Predecessors: [B2]
                Statements (2)
                    ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: null, IsImplicit) (Syntax: 'z As Integer')
                      Left: 
                        ILocalReferenceOperation: z (IsDeclaration: True) (OperationKind.LocalReference, Type: System.Int32, IsImplicit) (Syntax: 'z As Integer')
                      Right: 
                        IPropertyReferenceOperation: ReadOnly Property Enumerator.Current As System.Int32 (OperationKind.PropertyReference, Type: System.Int32, IsImplicit) (Syntax: 'z As Integer')
                          Instance Receiver: 
                            IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: Enumerator, IsImplicit) (Syntax: 'x')

                    IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'result = z')
                      Expression: 
                        ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Int32, IsImplicit) (Syntax: 'result = z')
                          Left: 
                            IParameterReferenceOperation: result (OperationKind.ParameterReference, Type: System.Int32) (Syntax: 'result')
                          Right: 
                            ILocalReferenceOperation: z (OperationKind.LocalReference, Type: System.Int32) (Syntax: 'z')

                Next (Regular) Block[B2]
                    Leaving: {R4}
        }
    }
    .finally {R5}
    {
        Block[B4] - Block
            Predecessors (0)
            Statements (1)
                IInvocationOperation (virtual Sub System.IDisposable.Dispose()) (OperationKind.Invocation, Type: System.Void, IsImplicit) (Syntax: 'x')
                  Instance Receiver: 
                    IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.IDisposable, IsImplicit) (Syntax: 'x')
                      Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                        (WideningValue)
                      Operand: 
                        IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: Enumerator, IsImplicit) (Syntax: 'x')
                  Arguments(0)

            Next (StructuredExceptionHandling) Block[null]
    }
}

Block[B5] - Exit
    Predecessors: [B2]
    Statements (0)
]]>.Value

            VerifyFlowGraphAndDiagnosticsForTest(Of MethodBlockSyntax)(source, expectedFlowGraph, expectedDiagnostics)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation, CompilerFeature.Dataflow)>
        <Fact()>
        Public Sub ForEachFlow_06()
            Dim source = <![CDATA[
Imports System
Public Class C
    Sub M(x As System.Collections.Generic.IEnumerable(Of Integer), result As Integer) 'BIND:"Sub M"
        For Each z in x
            result = z
        Next
    End Sub
End Class
]]>.Value

            Dim expectedDiagnostics = String.Empty

            Dim expectedFlowGraph = <![CDATA[
Block[B0] - Entry
    Statements (0)
    Next (Regular) Block[B1]
        Entering: {R1}

.locals {R1}
{
    CaptureIds: [0]
    Block[B1] - Block
        Predecessors: [B0]
        Statements (1)
            IFlowCaptureOperation: 0 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'x')
              Value: 
                IInvocationOperation (virtual Function System.Collections.Generic.IEnumerable(Of System.Int32).GetEnumerator() As System.Collections.Generic.IEnumerator(Of System.Int32)) (OperationKind.Invocation, Type: System.Collections.Generic.IEnumerator(Of System.Int32), IsImplicit) (Syntax: 'x')
                  Instance Receiver: 
                    IParameterReferenceOperation: x (OperationKind.ParameterReference, Type: System.Collections.Generic.IEnumerable(Of System.Int32)) (Syntax: 'x')
                  Arguments(0)

        Next (Regular) Block[B2]
            Entering: {R2} {R3}

    .try {R2, R3}
    {
        Block[B2] - Block
            Predecessors: [B1] [B3]
            Statements (0)
            Jump if False (Regular) to Block[B7]
                IInvocationOperation (virtual Function System.Collections.IEnumerator.MoveNext() As System.Boolean) (OperationKind.Invocation, Type: System.Boolean, IsImplicit) (Syntax: 'x')
                  Instance Receiver: 
                    IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: System.Collections.Generic.IEnumerator(Of System.Int32), IsImplicit) (Syntax: 'x')
                  Arguments(0)
                Finalizing: {R5}
                Leaving: {R3} {R2} {R1}

            Next (Regular) Block[B3]
                Entering: {R4}

        .locals {R4}
        {
            Locals: [z As System.Int32]
            Block[B3] - Block
                Predecessors: [B2]
                Statements (2)
                    ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: null, IsImplicit) (Syntax: 'z')
                      Left: 
                        ILocalReferenceOperation: z (IsDeclaration: True) (OperationKind.LocalReference, Type: System.Int32, IsImplicit) (Syntax: 'z')
                      Right: 
                        IPropertyReferenceOperation: ReadOnly Property System.Collections.Generic.IEnumerator(Of System.Int32).Current As System.Int32 (OperationKind.PropertyReference, Type: System.Int32, IsImplicit) (Syntax: 'z')
                          Instance Receiver: 
                            IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: System.Collections.Generic.IEnumerator(Of System.Int32), IsImplicit) (Syntax: 'x')

                    IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'result = z')
                      Expression: 
                        ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Int32, IsImplicit) (Syntax: 'result = z')
                          Left: 
                            IParameterReferenceOperation: result (OperationKind.ParameterReference, Type: System.Int32) (Syntax: 'result')
                          Right: 
                            ILocalReferenceOperation: z (OperationKind.LocalReference, Type: System.Int32) (Syntax: 'z')

                Next (Regular) Block[B2]
                    Leaving: {R4}
        }
    }
    .finally {R5}
    {
        Block[B4] - Block
            Predecessors (0)
            Statements (0)
            Jump if True (Regular) to Block[B6]
                IIsNullOperation (OperationKind.IsNull, Type: System.Boolean, IsImplicit) (Syntax: 'x')
                  Operand: 
                    IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: System.Collections.Generic.IEnumerator(Of System.Int32), IsImplicit) (Syntax: 'x')

            Next (Regular) Block[B5]
        Block[B5] - Block
            Predecessors: [B4]
            Statements (1)
                IInvocationOperation (virtual Sub System.IDisposable.Dispose()) (OperationKind.Invocation, Type: System.Void, IsImplicit) (Syntax: 'x')
                  Instance Receiver: 
                    IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.IDisposable, IsImplicit) (Syntax: 'x')
                      Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: True, IsUserDefined: False) (MethodSymbol: null)
                        (WideningReference)
                      Operand: 
                        IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: System.Collections.Generic.IEnumerator(Of System.Int32), IsImplicit) (Syntax: 'x')
                  Arguments(0)

            Next (Regular) Block[B6]
        Block[B6] - Block
            Predecessors: [B4] [B5]
            Statements (0)
            Next (StructuredExceptionHandling) Block[null]
    }
}

Block[B7] - Exit
    Predecessors: [B2]
    Statements (0)
]]>.Value

            VerifyFlowGraphAndDiagnosticsForTest(Of MethodBlockSyntax)(source, expectedFlowGraph, expectedDiagnostics)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation, CompilerFeature.Dataflow)>
        <Fact()>
        Public Sub ForEachFlow_11()
            Dim source = <![CDATA[
Imports System
Public Class C
    Sub M(x As Object, result As Object) 'BIND:"Sub M"
        For Each z in x
            result = z
        Next
    End Sub
End Class
]]>.Value

            Dim expectedDiagnostics = String.Empty

            Dim expectedFlowGraph = <![CDATA[
Block[B0] - Entry
    Statements (0)
    Next (Regular) Block[B1]
        Entering: {R1}

.locals {R1}
{
    CaptureIds: [0]
    Block[B1] - Block
        Predecessors: [B0]
        Statements (1)
            IFlowCaptureOperation: 0 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'x')
              Value: 
                IInvocationOperation (virtual Function System.Collections.IEnumerable.GetEnumerator() As System.Collections.IEnumerator) (OperationKind.Invocation, Type: System.Collections.IEnumerator, IsImplicit) (Syntax: 'x')
                  Instance Receiver: 
                    IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.Collections.IEnumerable, IsImplicit) (Syntax: 'x')
                      Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: True, IsUserDefined: False) (MethodSymbol: null)
                        (NarrowingReference)
                      Operand: 
                        IParameterReferenceOperation: x (OperationKind.ParameterReference, Type: System.Object) (Syntax: 'x')
                  Arguments(0)

        Next (Regular) Block[B2]
            Entering: {R2} {R3}

    .try {R2, R3}
    {
        Block[B2] - Block
            Predecessors: [B1] [B3]
            Statements (0)
            Jump if False (Regular) to Block[B7]
                IInvocationOperation (virtual Function System.Collections.IEnumerator.MoveNext() As System.Boolean) (OperationKind.Invocation, Type: System.Boolean, IsImplicit) (Syntax: 'x')
                  Instance Receiver: 
                    IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: System.Collections.IEnumerator, IsImplicit) (Syntax: 'x')
                  Arguments(0)
                Finalizing: {R5}
                Leaving: {R3} {R2} {R1}

            Next (Regular) Block[B3]
                Entering: {R4}

        .locals {R4}
        {
            Locals: [z As System.Object]
            Block[B3] - Block
                Predecessors: [B2]
                Statements (2)
                    ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: null, IsImplicit) (Syntax: 'z')
                      Left: 
                        ILocalReferenceOperation: z (IsDeclaration: True) (OperationKind.LocalReference, Type: System.Object, IsImplicit) (Syntax: 'z')
                      Right: 
                        IPropertyReferenceOperation: ReadOnly Property System.Collections.IEnumerator.Current As System.Object (OperationKind.PropertyReference, Type: System.Object, IsImplicit) (Syntax: 'z')
                          Instance Receiver: 
                            IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: System.Collections.IEnumerator, IsImplicit) (Syntax: 'x')

                    IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'result = z')
                      Expression: 
                        ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Object, IsImplicit) (Syntax: 'result = z')
                          Left: 
                            IParameterReferenceOperation: result (OperationKind.ParameterReference, Type: System.Object) (Syntax: 'result')
                          Right: 
                            ILocalReferenceOperation: z (OperationKind.LocalReference, Type: System.Object) (Syntax: 'z')

                Next (Regular) Block[B2]
                    Leaving: {R4}
        }
    }
    .finally {R5}
    {
        CaptureIds: [1]
        Block[B4] - Block
            Predecessors (0)
            Statements (1)
                IFlowCaptureOperation: 1 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'x')
                  Value: 
                    IConversionOperation (TryCast: True, Unchecked) (OperationKind.Conversion, Type: System.IDisposable, IsImplicit) (Syntax: 'x')
                      Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: True, IsUserDefined: False) (MethodSymbol: null)
                        (NarrowingReference)
                      Operand: 
                        IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: System.Collections.IEnumerator, IsImplicit) (Syntax: 'x')

            Jump if True (Regular) to Block[B6]
                IIsNullOperation (OperationKind.IsNull, Type: System.Boolean, IsImplicit) (Syntax: 'x')
                  Operand: 
                    IFlowCaptureReferenceOperation: 1 (OperationKind.FlowCaptureReference, Type: System.IDisposable, IsImplicit) (Syntax: 'x')

            Next (Regular) Block[B5]
        Block[B5] - Block
            Predecessors: [B4]
            Statements (1)
                IInvocationOperation (virtual Sub System.IDisposable.Dispose()) (OperationKind.Invocation, Type: System.Void, IsImplicit) (Syntax: 'x')
                  Instance Receiver: 
                    IFlowCaptureReferenceOperation: 1 (OperationKind.FlowCaptureReference, Type: System.IDisposable, IsImplicit) (Syntax: 'x')
                  Arguments(0)

            Next (Regular) Block[B6]
        Block[B6] - Block
            Predecessors: [B4] [B5]
            Statements (0)
            Next (StructuredExceptionHandling) Block[null]
    }
}

Block[B7] - Exit
    Predecessors: [B2]
    Statements (0)
]]>.Value

            VerifyFlowGraphAndDiagnosticsForTest(Of MethodBlockSyntax)(source, expectedFlowGraph, expectedDiagnostics)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation, CompilerFeature.Dataflow)>
        <Fact()>
        Public Sub ForEachFlow_12()
            Dim source = <![CDATA[
Imports System
Public Class C
    Sub M(x As C, result As Integer) 'BIND:"Sub M"
        For Each z in x
            result = z
        Next
    End Sub
End Class
]]>.Value

            Dim expectedDiagnostics = <![CDATA[
BC32023: Expression is of type 'C', which is not a collection type.
        For Each z in x
                      ~
]]>.Value

            Dim expectedFlowGraph = <![CDATA[
Block[B0] - Entry
    Statements (0)
    Next (Regular) Block[B1]
Block[B1] - Block
    Predecessors: [B0]
    Statements (1)
        IInvalidOperation (OperationKind.Invalid, Type: null, IsInvalid, IsImplicit) (Syntax: 'x')
          Children(1):
              IInvalidOperation (OperationKind.Invalid, Type: C, IsInvalid, IsImplicit) (Syntax: 'x')
                Children(1):
                    IParameterReferenceOperation: x (OperationKind.ParameterReference, Type: C, IsInvalid) (Syntax: 'x')

    Next (Regular) Block[B2]
Block[B2] - Block
    Predecessors: [B1] [B3]
    Statements (0)
    Jump if False (Regular) to Block[B4]
        IInvalidOperation (OperationKind.Invalid, Type: System.Boolean, IsInvalid, IsImplicit) (Syntax: 'x')
          Children(1):
              IInvalidOperation (OperationKind.Invalid, Type: null, IsInvalid, IsImplicit) (Syntax: 'x')
                Children(0)

    Next (Regular) Block[B3]
        Entering: {R1}

.locals {R1}
{
    Locals: [z As System.Object]
    Block[B3] - Block
        Predecessors: [B2]
        Statements (2)
            ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: null, IsImplicit) (Syntax: 'z')
              Left: 
                ILocalReferenceOperation: z (IsDeclaration: True) (OperationKind.LocalReference, Type: System.Object, IsImplicit) (Syntax: 'z')
              Right: 
                IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.Object, IsInvalid, IsImplicit) (Syntax: 'x')
                  Conversion: CommonConversion (Exists: False, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                    (DelegateRelaxationLevelNone)
                  Operand: 
                    IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: null, IsInvalid, IsImplicit) (Syntax: 'x')
                      Conversion: CommonConversion (Exists: False, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                        (DelegateRelaxationLevelNone)
                      Operand: 
                        IInvalidOperation (OperationKind.Invalid, Type: null, IsInvalid, IsImplicit) (Syntax: 'x')
                          Children(1):
                              IInvalidOperation (OperationKind.Invalid, Type: null, IsInvalid, IsImplicit) (Syntax: 'x')
                                Children(0)

            IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'result = z')
              Expression: 
                ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Int32, IsImplicit) (Syntax: 'result = z')
                  Left: 
                    IParameterReferenceOperation: result (OperationKind.ParameterReference, Type: System.Int32) (Syntax: 'result')
                  Right: 
                    IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.Int32, IsImplicit) (Syntax: 'z')
                      Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                        (NarrowingValue)
                      Operand: 
                        ILocalReferenceOperation: z (OperationKind.LocalReference, Type: System.Object) (Syntax: 'z')

        Next (Regular) Block[B2]
            Leaving: {R1}
}

Block[B4] - Exit
    Predecessors: [B2]
    Statements (0)
]]>.Value

            VerifyFlowGraphAndDiagnosticsForTest(Of MethodBlockSyntax)(source, expectedFlowGraph, expectedDiagnostics)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation, CompilerFeature.Dataflow)>
        <Fact()>
        Public Sub ForEachFlow_15()
            Dim source = <![CDATA[
Imports System
Public NotInheritable Class C
    Sub M() 'BIND:"Sub M"
        For Each If(GetC(), Me).F in Me
        Next
    End Sub
   
    Public F As C

    Public ReadOnly Property Current As C
        Get
            Return Nothing
        End Get
    End Property

    Public Function MoveNext() As Boolean
        Return False
    End Function

    Public Shared Function GetC() As C
        Return Nothing
    End Function
End Class

Module Ext
    <Runtime.CompilerServices.Extension>
    Public Function GetEnumerator(this As C) As C
        Return this
    End Function
End Module
]]>.Value

            Dim expectedDiagnostics = String.Empty

            Dim expectedFlowGraph = <![CDATA[
Block[B0] - Entry
    Statements (0)
    Next (Regular) Block[B1]
        Entering: {R1}

.locals {R1}
{
    CaptureIds: [0]
    Block[B1] - Block
        Predecessors: [B0]
        Statements (1)
            IFlowCaptureOperation: 0 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'Me')
              Value: 
                IInvocationOperation ( Function C.GetEnumerator() As C) (OperationKind.Invocation, Type: C, IsImplicit) (Syntax: 'Me')
                  Instance Receiver: 
                    IInstanceReferenceOperation (ReferenceKind: ContainingTypeInstance) (OperationKind.InstanceReference, Type: C) (Syntax: 'Me')
                  Arguments(0)

        Next (Regular) Block[B2]
    Block[B2] - Block
        Predecessors: [B1] [B6]
        Statements (0)
        Jump if False (Regular) to Block[B7]
            IInvocationOperation ( Function C.MoveNext() As System.Boolean) (OperationKind.Invocation, Type: System.Boolean, IsImplicit) (Syntax: 'Me')
              Instance Receiver: 
                IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: C, IsImplicit) (Syntax: 'Me')
              Arguments(0)
            Leaving: {R1}

        Next (Regular) Block[B3]
            Entering: {R2} {R3}

    .locals {R2}
    {
        CaptureIds: [2]
        .locals {R3}
        {
            CaptureIds: [1]
            Block[B3] - Block
                Predecessors: [B2]
                Statements (1)
                    IFlowCaptureOperation: 1 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'GetC()')
                      Value: 
                        IInvocationOperation (Function C.GetC() As C) (OperationKind.Invocation, Type: C) (Syntax: 'GetC()')
                          Instance Receiver: 
                            null
                          Arguments(0)

                Jump if True (Regular) to Block[B5]
                    IIsNullOperation (OperationKind.IsNull, Type: System.Boolean, IsImplicit) (Syntax: 'GetC()')
                      Operand: 
                        IFlowCaptureReferenceOperation: 1 (OperationKind.FlowCaptureReference, Type: C, IsImplicit) (Syntax: 'GetC()')
                    Leaving: {R3}

                Next (Regular) Block[B4]
            Block[B4] - Block
                Predecessors: [B3]
                Statements (1)
                    IFlowCaptureOperation: 2 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'GetC()')
                      Value: 
                        IFlowCaptureReferenceOperation: 1 (OperationKind.FlowCaptureReference, Type: C, IsImplicit) (Syntax: 'GetC()')

                Next (Regular) Block[B6]
                    Leaving: {R3}
        }

        Block[B5] - Block
            Predecessors: [B3]
            Statements (1)
                IFlowCaptureOperation: 2 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'Me')
                  Value: 
                    IInstanceReferenceOperation (ReferenceKind: ContainingTypeInstance) (OperationKind.InstanceReference, Type: C) (Syntax: 'Me')

            Next (Regular) Block[B6]
        Block[B6] - Block
            Predecessors: [B4] [B5]
            Statements (1)
                ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: C, IsImplicit) (Syntax: 'If(GetC(), Me).F')
                  Left: 
                    IFieldReferenceOperation: C.F As C (OperationKind.FieldReference, Type: C) (Syntax: 'If(GetC(), Me).F')
                      Instance Receiver: 
                        IFlowCaptureReferenceOperation: 2 (OperationKind.FlowCaptureReference, Type: C, IsImplicit) (Syntax: 'If(GetC(), Me)')
                  Right: 
                    IPropertyReferenceOperation: ReadOnly Property C.Current As C (OperationKind.PropertyReference, Type: C, IsImplicit) (Syntax: 'If(GetC(), Me).F')
                      Instance Receiver: 
                        IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: C, IsImplicit) (Syntax: 'Me')

            Next (Regular) Block[B2]
                Leaving: {R2}
    }
}

Block[B7] - Exit
    Predecessors: [B2]
    Statements (0)
]]>.Value

            VerifyFlowGraphAndDiagnosticsForTest(Of MethodBlockSyntax)(source, expectedFlowGraph, expectedDiagnostics)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation, CompilerFeature.Dataflow)>
        <Fact()>
        Public Sub ForEachFlow_16()
            Dim source = <![CDATA[
Imports System
Public Class C
    Sub M(x As System.Collections.IEnumerable, result As Object) 'BIND:"Sub M"
        For Each z in x
            result = z
        Next
    End Sub
End Class
]]>.Value

            Dim expectedDiagnostics = String.Empty

            Dim expectedFlowGraph = <![CDATA[
Block[B0] - Entry
    Statements (0)
    Next (Regular) Block[B1]
        Entering: {R1}

.locals {R1}
{
    CaptureIds: [0]
    Block[B1] - Block
        Predecessors: [B0]
        Statements (1)
            IFlowCaptureOperation: 0 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'x')
              Value: 
                IInvocationOperation (virtual Function System.Collections.IEnumerable.GetEnumerator() As System.Collections.IEnumerator) (OperationKind.Invocation, Type: System.Collections.IEnumerator, IsImplicit) (Syntax: 'x')
                  Instance Receiver: 
                    IParameterReferenceOperation: x (OperationKind.ParameterReference, Type: System.Collections.IEnumerable) (Syntax: 'x')
                  Arguments(0)

        Next (Regular) Block[B2]
            Entering: {R2} {R3}

    .try {R2, R3}
    {
        Block[B2] - Block
            Predecessors: [B1] [B3]
            Statements (0)
            Jump if False (Regular) to Block[B7]
                IInvocationOperation (virtual Function System.Collections.IEnumerator.MoveNext() As System.Boolean) (OperationKind.Invocation, Type: System.Boolean, IsImplicit) (Syntax: 'x')
                  Instance Receiver: 
                    IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: System.Collections.IEnumerator, IsImplicit) (Syntax: 'x')
                  Arguments(0)
                Finalizing: {R5}
                Leaving: {R3} {R2} {R1}

            Next (Regular) Block[B3]
                Entering: {R4}

        .locals {R4}
        {
            Locals: [z As System.Object]
            Block[B3] - Block
                Predecessors: [B2]
                Statements (2)
                    ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: null, IsImplicit) (Syntax: 'z')
                      Left: 
                        ILocalReferenceOperation: z (IsDeclaration: True) (OperationKind.LocalReference, Type: System.Object, IsImplicit) (Syntax: 'z')
                      Right: 
                        IPropertyReferenceOperation: ReadOnly Property System.Collections.IEnumerator.Current As System.Object (OperationKind.PropertyReference, Type: System.Object, IsImplicit) (Syntax: 'z')
                          Instance Receiver: 
                            IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: System.Collections.IEnumerator, IsImplicit) (Syntax: 'x')

                    IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'result = z')
                      Expression: 
                        ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Object, IsImplicit) (Syntax: 'result = z')
                          Left: 
                            IParameterReferenceOperation: result (OperationKind.ParameterReference, Type: System.Object) (Syntax: 'result')
                          Right: 
                            ILocalReferenceOperation: z (OperationKind.LocalReference, Type: System.Object) (Syntax: 'z')

                Next (Regular) Block[B2]
                    Leaving: {R4}
        }
    }
    .finally {R5}
    {
        CaptureIds: [1]
        Block[B4] - Block
            Predecessors (0)
            Statements (1)
                IFlowCaptureOperation: 1 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'x')
                  Value: 
                    IConversionOperation (TryCast: True, Unchecked) (OperationKind.Conversion, Type: System.IDisposable, IsImplicit) (Syntax: 'x')
                      Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: True, IsUserDefined: False) (MethodSymbol: null)
                        (NarrowingReference)
                      Operand: 
                        IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: System.Collections.IEnumerator, IsImplicit) (Syntax: 'x')

            Jump if True (Regular) to Block[B6]
                IIsNullOperation (OperationKind.IsNull, Type: System.Boolean, IsImplicit) (Syntax: 'x')
                  Operand: 
                    IFlowCaptureReferenceOperation: 1 (OperationKind.FlowCaptureReference, Type: System.IDisposable, IsImplicit) (Syntax: 'x')

            Next (Regular) Block[B5]
        Block[B5] - Block
            Predecessors: [B4]
            Statements (1)
                IInvocationOperation (virtual Sub System.IDisposable.Dispose()) (OperationKind.Invocation, Type: System.Void, IsImplicit) (Syntax: 'x')
                  Instance Receiver: 
                    IFlowCaptureReferenceOperation: 1 (OperationKind.FlowCaptureReference, Type: System.IDisposable, IsImplicit) (Syntax: 'x')
                  Arguments(0)

            Next (Regular) Block[B6]
        Block[B6] - Block
            Predecessors: [B4] [B5]
            Statements (0)
            Next (StructuredExceptionHandling) Block[null]
    }
}

Block[B7] - Exit
    Predecessors: [B2]
    Statements (0)
]]>.Value

            VerifyFlowGraphAndDiagnosticsForTest(Of MethodBlockSyntax)(source, expectedFlowGraph, expectedDiagnostics)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation, CompilerFeature.Dataflow)>
        <Fact()>
        Public Sub ForEachFlow_17()
            Dim source = <![CDATA[
Imports System
Public Class C
    Sub M(x As Integer(), result As Long) 'BIND:"Sub M"
        For Each z As Long in x
            result = z
        Next
    End Sub
End Class]]>.Value

            Dim expectedDiagnostics = String.Empty

            Dim expectedFlowGraph = <![CDATA[
Block[B0] - Entry
    Statements (0)
    Next (Regular) Block[B1]
        Entering: {R1}

.locals {R1}
{
    CaptureIds: [0]
    Block[B1] - Block
        Predecessors: [B0]
        Statements (1)
            IFlowCaptureOperation: 0 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'x')
              Value: 
                IInvocationOperation ( Function System.Array.GetEnumerator() As System.Collections.IEnumerator) (OperationKind.Invocation, Type: System.Collections.IEnumerator, IsImplicit) (Syntax: 'x')
                  Instance Receiver: 
                    IParameterReferenceOperation: x (OperationKind.ParameterReference, Type: System.Int32()) (Syntax: 'x')
                  Arguments(0)

        Next (Regular) Block[B2]
    Block[B2] - Block
        Predecessors: [B1] [B3]
        Statements (0)
        Jump if False (Regular) to Block[B4]
            IInvocationOperation (virtual Function System.Collections.IEnumerator.MoveNext() As System.Boolean) (OperationKind.Invocation, Type: System.Boolean, IsImplicit) (Syntax: 'x')
              Instance Receiver: 
                IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: System.Collections.IEnumerator, IsImplicit) (Syntax: 'x')
              Arguments(0)
            Leaving: {R1}

        Next (Regular) Block[B3]
            Entering: {R2}

    .locals {R2}
    {
        Locals: [z As System.Int64]
        Block[B3] - Block
            Predecessors: [B2]
            Statements (2)
                ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: null, IsImplicit) (Syntax: 'z As Long')
                  Left: 
                    ILocalReferenceOperation: z (IsDeclaration: True) (OperationKind.LocalReference, Type: System.Int64, IsImplicit) (Syntax: 'z As Long')
                  Right: 
                    IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.Int64, IsImplicit) (Syntax: 'z As Long')
                      Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: True, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                        (WideningNumeric)
                      Operand: 
                        IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.Int32, IsImplicit) (Syntax: 'z As Long')
                          Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                            (NarrowingValue)
                          Operand: 
                            IPropertyReferenceOperation: ReadOnly Property System.Collections.IEnumerator.Current As System.Object (OperationKind.PropertyReference, Type: System.Object, IsImplicit) (Syntax: 'z As Long')
                              Instance Receiver: 
                                IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: System.Collections.IEnumerator, IsImplicit) (Syntax: 'x')

                IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'result = z')
                  Expression: 
                    ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Int64, IsImplicit) (Syntax: 'result = z')
                      Left: 
                        IParameterReferenceOperation: result (OperationKind.ParameterReference, Type: System.Int64) (Syntax: 'result')
                      Right: 
                        ILocalReferenceOperation: z (OperationKind.LocalReference, Type: System.Int64) (Syntax: 'z')

            Next (Regular) Block[B2]
                Leaving: {R2}
    }
}

Block[B4] - Exit
    Predecessors: [B2]
    Statements (0)
]]>.Value

            VerifyFlowGraphAndDiagnosticsForTest(Of MethodBlockSyntax)(source, expectedFlowGraph, expectedDiagnostics)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation, CompilerFeature.Dataflow)>
        <Fact()>
        Public Sub ForEachFlow_18()
            Dim source = <![CDATA[
Imports System
Public NotInheritable Class C
    Sub M(result as Boolean) 'BIND:"Sub M"
        For Each x in Me
            If x
                Continue For
            End If

            result = x
        Next
    End Sub
   
    Public Function GetEnumerator() As C
        Return Me
    End Function

    Public ReadOnly Property Current As Boolean
        Get
            Return Nothing
        End Get
    End Property

    Public Function MoveNext() As Boolean
        Return False
    End Function

    Public Shared Function GetC() As C
        Return Nothing
    End Function
End Class
]]>.Value

            Dim expectedDiagnostics = String.Empty

            Dim expectedFlowGraph = <![CDATA[
Block[B0] - Entry
    Statements (0)
    Next (Regular) Block[B1]
        Entering: {R1}

.locals {R1}
{
    CaptureIds: [0]
    Block[B1] - Block
        Predecessors: [B0]
        Statements (1)
            IFlowCaptureOperation: 0 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'Me')
              Value: 
                IInvocationOperation ( Function C.GetEnumerator() As C) (OperationKind.Invocation, Type: C, IsImplicit) (Syntax: 'Me')
                  Instance Receiver: 
                    IInstanceReferenceOperation (ReferenceKind: ContainingTypeInstance) (OperationKind.InstanceReference, Type: C) (Syntax: 'Me')
                  Arguments(0)

        Next (Regular) Block[B2]
    Block[B2] - Block
        Predecessors: [B1] [B3] [B4]
        Statements (0)
        Jump if False (Regular) to Block[B5]
            IInvocationOperation ( Function C.MoveNext() As System.Boolean) (OperationKind.Invocation, Type: System.Boolean, IsImplicit) (Syntax: 'Me')
              Instance Receiver: 
                IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: C, IsImplicit) (Syntax: 'Me')
              Arguments(0)
            Leaving: {R1}

        Next (Regular) Block[B3]
            Entering: {R2}

    .locals {R2}
    {
        Locals: [x As System.Boolean]
        Block[B3] - Block
            Predecessors: [B2]
            Statements (1)
                ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: null, IsImplicit) (Syntax: 'x')
                  Left: 
                    ILocalReferenceOperation: x (IsDeclaration: True) (OperationKind.LocalReference, Type: System.Boolean, IsImplicit) (Syntax: 'x')
                  Right: 
                    IPropertyReferenceOperation: ReadOnly Property C.Current As System.Boolean (OperationKind.PropertyReference, Type: System.Boolean, IsImplicit) (Syntax: 'x')
                      Instance Receiver: 
                        IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: C, IsImplicit) (Syntax: 'Me')

            Jump if False (Regular) to Block[B4]
                ILocalReferenceOperation: x (OperationKind.LocalReference, Type: System.Boolean) (Syntax: 'x')

            Next (Regular) Block[B2]
                Leaving: {R2}
        Block[B4] - Block
            Predecessors: [B3]
            Statements (1)
                IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'result = x')
                  Expression: 
                    ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Boolean, IsImplicit) (Syntax: 'result = x')
                      Left: 
                        IParameterReferenceOperation: result (OperationKind.ParameterReference, Type: System.Boolean) (Syntax: 'result')
                      Right: 
                        ILocalReferenceOperation: x (OperationKind.LocalReference, Type: System.Boolean) (Syntax: 'x')

            Next (Regular) Block[B2]
                Leaving: {R2}
    }
}

Block[B5] - Exit
    Predecessors: [B2]
    Statements (0)
]]>.Value

            VerifyFlowGraphAndDiagnosticsForTest(Of MethodBlockSyntax)(source, expectedFlowGraph, expectedDiagnostics)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation, CompilerFeature.Dataflow)>
        <Fact()>
        Public Sub ForEachFlow_19()
            Dim source = <![CDATA[
Imports System
Public NotInheritable Class C
    Sub M(result as Boolean) 'BIND:"Sub M"
        For Each x in Me
            If x
                Exit For
            End If

            result = x
        Next
    End Sub
   
    Public Function GetEnumerator() As C
        Return Me
    End Function

    Public ReadOnly Property Current As Boolean
        Get
            Return Nothing
        End Get
    End Property

    Public Function MoveNext() As Boolean
        Return False
    End Function

    Public Shared Function GetC() As C
        Return Nothing
    End Function
End Class
]]>.Value

            Dim expectedDiagnostics = String.Empty

            Dim expectedFlowGraph = <![CDATA[
Block[B0] - Entry
    Statements (0)
    Next (Regular) Block[B1]
        Entering: {R1}

.locals {R1}
{
    CaptureIds: [0]
    Block[B1] - Block
        Predecessors: [B0]
        Statements (1)
            IFlowCaptureOperation: 0 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'Me')
              Value: 
                IInvocationOperation ( Function C.GetEnumerator() As C) (OperationKind.Invocation, Type: C, IsImplicit) (Syntax: 'Me')
                  Instance Receiver: 
                    IInstanceReferenceOperation (ReferenceKind: ContainingTypeInstance) (OperationKind.InstanceReference, Type: C) (Syntax: 'Me')
                  Arguments(0)

        Next (Regular) Block[B2]
    Block[B2] - Block
        Predecessors: [B1] [B4]
        Statements (0)
        Jump if False (Regular) to Block[B5]
            IInvocationOperation ( Function C.MoveNext() As System.Boolean) (OperationKind.Invocation, Type: System.Boolean, IsImplicit) (Syntax: 'Me')
              Instance Receiver: 
                IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: C, IsImplicit) (Syntax: 'Me')
              Arguments(0)
            Leaving: {R1}

        Next (Regular) Block[B3]
            Entering: {R2}

    .locals {R2}
    {
        Locals: [x As System.Boolean]
        Block[B3] - Block
            Predecessors: [B2]
            Statements (1)
                ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: null, IsImplicit) (Syntax: 'x')
                  Left: 
                    ILocalReferenceOperation: x (IsDeclaration: True) (OperationKind.LocalReference, Type: System.Boolean, IsImplicit) (Syntax: 'x')
                  Right: 
                    IPropertyReferenceOperation: ReadOnly Property C.Current As System.Boolean (OperationKind.PropertyReference, Type: System.Boolean, IsImplicit) (Syntax: 'x')
                      Instance Receiver: 
                        IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: C, IsImplicit) (Syntax: 'Me')

            Jump if False (Regular) to Block[B4]
                ILocalReferenceOperation: x (OperationKind.LocalReference, Type: System.Boolean) (Syntax: 'x')

            Next (Regular) Block[B5]
                Leaving: {R2} {R1}
        Block[B4] - Block
            Predecessors: [B3]
            Statements (1)
                IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'result = x')
                  Expression: 
                    ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Boolean, IsImplicit) (Syntax: 'result = x')
                      Left: 
                        IParameterReferenceOperation: result (OperationKind.ParameterReference, Type: System.Boolean) (Syntax: 'result')
                      Right: 
                        ILocalReferenceOperation: x (OperationKind.LocalReference, Type: System.Boolean) (Syntax: 'x')

            Next (Regular) Block[B2]
                Leaving: {R2}
    }
}

Block[B5] - Exit
    Predecessors: [B2] [B3]
    Statements (0)
]]>.Value

            VerifyFlowGraphAndDiagnosticsForTest(Of MethodBlockSyntax)(source, expectedFlowGraph, expectedDiagnostics)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation, CompilerFeature.Dataflow)>
        <Fact()>
        Public Sub ForEachFlow_20()
            Dim source = <![CDATA[
Imports System
Public NotInheritable Class C
    Sub M(y as Boolean, result as Boolean) 'BIND:"Sub M"
        For Each x in Me
            While y
                result = y
                Continue For
            End While

            result = x
        Next
    End Sub
   
    Public Function GetEnumerator() As C
        Return Me
    End Function

    Public ReadOnly Property Current As Boolean
        Get
            Return Nothing
        End Get
    End Property

    Public Function MoveNext() As Boolean
        Return False
    End Function

    Public Shared Function GetC() As C
        Return Nothing
    End Function
End Class
]]>.Value

            Dim expectedDiagnostics = String.Empty

            Dim expectedFlowGraph = <![CDATA[
Block[B0] - Entry
    Statements (0)
    Next (Regular) Block[B1]
        Entering: {R1}

.locals {R1}
{
    CaptureIds: [0]
    Block[B1] - Block
        Predecessors: [B0]
        Statements (1)
            IFlowCaptureOperation: 0 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'Me')
              Value: 
                IInvocationOperation ( Function C.GetEnumerator() As C) (OperationKind.Invocation, Type: C, IsImplicit) (Syntax: 'Me')
                  Instance Receiver: 
                    IInstanceReferenceOperation (ReferenceKind: ContainingTypeInstance) (OperationKind.InstanceReference, Type: C) (Syntax: 'Me')
                  Arguments(0)

        Next (Regular) Block[B2]
    Block[B2] - Block
        Predecessors: [B1] [B4] [B5]
        Statements (0)
        Jump if False (Regular) to Block[B6]
            IInvocationOperation ( Function C.MoveNext() As System.Boolean) (OperationKind.Invocation, Type: System.Boolean, IsImplicit) (Syntax: 'Me')
              Instance Receiver: 
                IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: C, IsImplicit) (Syntax: 'Me')
              Arguments(0)
            Leaving: {R1}

        Next (Regular) Block[B3]
            Entering: {R2}

    .locals {R2}
    {
        Locals: [x As System.Boolean]
        Block[B3] - Block
            Predecessors: [B2]
            Statements (1)
                ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: null, IsImplicit) (Syntax: 'x')
                  Left: 
                    ILocalReferenceOperation: x (IsDeclaration: True) (OperationKind.LocalReference, Type: System.Boolean, IsImplicit) (Syntax: 'x')
                  Right: 
                    IPropertyReferenceOperation: ReadOnly Property C.Current As System.Boolean (OperationKind.PropertyReference, Type: System.Boolean, IsImplicit) (Syntax: 'x')
                      Instance Receiver: 
                        IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: C, IsImplicit) (Syntax: 'Me')

            Jump if False (Regular) to Block[B5]
                IParameterReferenceOperation: y (OperationKind.ParameterReference, Type: System.Boolean) (Syntax: 'y')

            Next (Regular) Block[B4]
        Block[B4] - Block
            Predecessors: [B3]
            Statements (1)
                IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'result = y')
                  Expression: 
                    ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Boolean, IsImplicit) (Syntax: 'result = y')
                      Left: 
                        IParameterReferenceOperation: result (OperationKind.ParameterReference, Type: System.Boolean) (Syntax: 'result')
                      Right: 
                        IParameterReferenceOperation: y (OperationKind.ParameterReference, Type: System.Boolean) (Syntax: 'y')

            Next (Regular) Block[B2]
                Leaving: {R2}
        Block[B5] - Block
            Predecessors: [B3]
            Statements (1)
                IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'result = x')
                  Expression: 
                    ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Boolean, IsImplicit) (Syntax: 'result = x')
                      Left: 
                        IParameterReferenceOperation: result (OperationKind.ParameterReference, Type: System.Boolean) (Syntax: 'result')
                      Right: 
                        ILocalReferenceOperation: x (OperationKind.LocalReference, Type: System.Boolean) (Syntax: 'x')

            Next (Regular) Block[B2]
                Leaving: {R2}
    }
}

Block[B6] - Exit
    Predecessors: [B2]
    Statements (0)
]]>.Value

            VerifyFlowGraphAndDiagnosticsForTest(Of MethodBlockSyntax)(source, expectedFlowGraph, expectedDiagnostics)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation, CompilerFeature.Dataflow)>
        <Fact()>
        Public Sub ForEachFlow_21()
            Dim source = <![CDATA[
Imports System
Public NotInheritable Class C
    Sub M(y as Boolean, result as Boolean) 'BIND:"Sub M"
        For Each x in Me
            While y
                result = y
                Exit For
            End While

            result = x
        Next
    End Sub
   
    Public Function GetEnumerator() As C
        Return Me
    End Function

    Public ReadOnly Property Current As Boolean
        Get
            Return Nothing
        End Get
    End Property

    Public Function MoveNext() As Boolean
        Return False
    End Function

    Public Shared Function GetC() As C
        Return Nothing
    End Function
End Class
]]>.Value

            Dim expectedDiagnostics = String.Empty

            Dim expectedFlowGraph = <![CDATA[
Block[B0] - Entry
    Statements (0)
    Next (Regular) Block[B1]
        Entering: {R1}

.locals {R1}
{
    CaptureIds: [0]
    Block[B1] - Block
        Predecessors: [B0]
        Statements (1)
            IFlowCaptureOperation: 0 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'Me')
              Value: 
                IInvocationOperation ( Function C.GetEnumerator() As C) (OperationKind.Invocation, Type: C, IsImplicit) (Syntax: 'Me')
                  Instance Receiver: 
                    IInstanceReferenceOperation (ReferenceKind: ContainingTypeInstance) (OperationKind.InstanceReference, Type: C) (Syntax: 'Me')
                  Arguments(0)

        Next (Regular) Block[B2]
    Block[B2] - Block
        Predecessors: [B1] [B5]
        Statements (0)
        Jump if False (Regular) to Block[B6]
            IInvocationOperation ( Function C.MoveNext() As System.Boolean) (OperationKind.Invocation, Type: System.Boolean, IsImplicit) (Syntax: 'Me')
              Instance Receiver: 
                IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: C, IsImplicit) (Syntax: 'Me')
              Arguments(0)
            Leaving: {R1}

        Next (Regular) Block[B3]
            Entering: {R2}

    .locals {R2}
    {
        Locals: [x As System.Boolean]
        Block[B3] - Block
            Predecessors: [B2]
            Statements (1)
                ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: null, IsImplicit) (Syntax: 'x')
                  Left: 
                    ILocalReferenceOperation: x (IsDeclaration: True) (OperationKind.LocalReference, Type: System.Boolean, IsImplicit) (Syntax: 'x')
                  Right: 
                    IPropertyReferenceOperation: ReadOnly Property C.Current As System.Boolean (OperationKind.PropertyReference, Type: System.Boolean, IsImplicit) (Syntax: 'x')
                      Instance Receiver: 
                        IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: C, IsImplicit) (Syntax: 'Me')

            Jump if False (Regular) to Block[B5]
                IParameterReferenceOperation: y (OperationKind.ParameterReference, Type: System.Boolean) (Syntax: 'y')

            Next (Regular) Block[B4]
        Block[B4] - Block
            Predecessors: [B3]
            Statements (1)
                IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'result = y')
                  Expression: 
                    ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Boolean, IsImplicit) (Syntax: 'result = y')
                      Left: 
                        IParameterReferenceOperation: result (OperationKind.ParameterReference, Type: System.Boolean) (Syntax: 'result')
                      Right: 
                        IParameterReferenceOperation: y (OperationKind.ParameterReference, Type: System.Boolean) (Syntax: 'y')

            Next (Regular) Block[B6]
                Leaving: {R2} {R1}
        Block[B5] - Block
            Predecessors: [B3]
            Statements (1)
                IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'result = x')
                  Expression: 
                    ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Boolean, IsImplicit) (Syntax: 'result = x')
                      Left: 
                        IParameterReferenceOperation: result (OperationKind.ParameterReference, Type: System.Boolean) (Syntax: 'result')
                      Right: 
                        ILocalReferenceOperation: x (OperationKind.LocalReference, Type: System.Boolean) (Syntax: 'x')

            Next (Regular) Block[B2]
                Leaving: {R2}
    }
}

Block[B6] - Exit
    Predecessors: [B2] [B4]
    Statements (0)
]]>.Value

            VerifyFlowGraphAndDiagnosticsForTest(Of MethodBlockSyntax)(source, expectedFlowGraph, expectedDiagnostics)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation, CompilerFeature.Dataflow)>
        <Fact()>
        Public Sub ForEachFlow_22()
            Dim source = <![CDATA[
Imports System
Public Class C
    Sub M() 'BIND:"Sub M"
        For Each z As Long in If(GetC(), GetC())
        Next
    End Sub

    Shared Function GetC() As C
        Return Nothing
    End Function

    Shared Function GetEnumerator() As C
        Return Nothing
    End Function

    Shared Function MoveNext() As Boolean
        Return Nothing
    End Function

    Shared ReadOnly Property Current As Integer
        Get
            Return 1
        End Get
    End Property
End Class]]>.Value

            Dim expectedDiagnostics = <![CDATA[
BC42025: Access of shared member, constant member, enum member or nested type through an instance; qualifying expression will not be evaluated.
        For Each z As Long in If(GetC(), GetC())
                              ~~~~~~~~~~~~~~~~~~
BC42025: Access of shared member, constant member, enum member or nested type through an instance; qualifying expression will not be evaluated.
        For Each z As Long in If(GetC(), GetC())
                              ~~~~~~~~~~~~~~~~~~
BC42025: Access of shared member, constant member, enum member or nested type through an instance; qualifying expression will not be evaluated.
        For Each z As Long in If(GetC(), GetC())
                              ~~~~~~~~~~~~~~~~~~
]]>.Value
            Dim expectedFlowGraph = <![CDATA[
Block[B0] - Entry
    Statements (0)
    Next (Regular) Block[B1]
        Entering: {R1}

.locals {R1}
{
    CaptureIds: [0]
    Block[B1] - Block
        Predecessors: [B0]
        Statements (1)
            IFlowCaptureOperation: 0 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'If(GetC(), GetC())')
              Value: 
                IInvocationOperation (Function C.GetEnumerator() As C) (OperationKind.Invocation, Type: C, IsImplicit) (Syntax: 'If(GetC(), GetC())')
                  Instance Receiver: 
                    null
                  Arguments(0)

        Next (Regular) Block[B2]
    Block[B2] - Block
        Predecessors: [B1] [B3]
        Statements (0)
        Jump if False (Regular) to Block[B4]
            IInvocationOperation (Function C.MoveNext() As System.Boolean) (OperationKind.Invocation, Type: System.Boolean, IsImplicit) (Syntax: 'If(GetC(), GetC())')
              Instance Receiver: 
                null
              Arguments(0)
            Leaving: {R1}

        Next (Regular) Block[B3]
            Entering: {R2}

    .locals {R2}
    {
        Locals: [z As System.Int64]
        Block[B3] - Block
            Predecessors: [B2]
            Statements (1)
                ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: null, IsImplicit) (Syntax: 'z As Long')
                  Left: 
                    ILocalReferenceOperation: z (IsDeclaration: True) (OperationKind.LocalReference, Type: System.Int64, IsImplicit) (Syntax: 'z As Long')
                  Right: 
                    IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.Int64, IsImplicit) (Syntax: 'z As Long')
                      Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: True, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                        (WideningNumeric)
                      Operand: 
                        IPropertyReferenceOperation: ReadOnly Property C.Current As System.Int32 (Static) (OperationKind.PropertyReference, Type: System.Int32, IsImplicit) (Syntax: 'z As Long')
                          Instance Receiver: 
                            null

            Next (Regular) Block[B2]
                Leaving: {R2}
    }
}

Block[B4] - Exit
    Predecessors: [B2]
    Statements (0)
]]>.Value

            VerifyFlowGraphAndDiagnosticsForTest(Of MethodBlockSyntax)(source, expectedFlowGraph, expectedDiagnostics)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation, CompilerFeature.Dataflow)>
        <Fact()>
        Public Sub ForEachFlow_23()
            Dim source = <![CDATA[
Imports System
Public Class C
    Sub M(x As C) 'BIND:"Sub M"
        For Each z in x
        Next
    End Sub

    Function GetEnumerator(<System.Runtime.CompilerServices.CallerMemberName> Optional member As String = "") As C
        Return Nothing
    End Function

    Function MoveNext(Optional s As String = "ABC") As Boolean
        Return Nothing
    End Function

    ReadOnly Property Current(Optional d As Double = 123.45) As Integer
        Get
            Return 1
        End Get
    End Property
End Class]]>.Value

            Dim expectedDiagnostics = String.Empty

            Dim expectedFlowGraph = <![CDATA[
Block[B0] - Entry
    Statements (0)
    Next (Regular) Block[B1]
        Entering: {R1}

.locals {R1}
{
    CaptureIds: [0]
    Block[B1] - Block
        Predecessors: [B0]
        Statements (1)
            IFlowCaptureOperation: 0 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'x')
              Value: 
                IInvocationOperation ( Function C.GetEnumerator([member As System.String = ""]) As C) (OperationKind.Invocation, Type: C, IsImplicit) (Syntax: 'x')
                  Instance Receiver: 
                    IParameterReferenceOperation: x (OperationKind.ParameterReference, Type: C) (Syntax: 'x')
                  Arguments(1):
                      IArgumentOperation (ArgumentKind.DefaultValue, Matching Parameter: member) (OperationKind.Argument, Type: null, IsImplicit) (Syntax: 'x')
                        ILiteralOperation (OperationKind.Literal, Type: System.String, Constant: "M", IsImplicit) (Syntax: 'x')
                        InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                        OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)

        Next (Regular) Block[B2]
    Block[B2] - Block
        Predecessors: [B1] [B3]
        Statements (0)
        Jump if False (Regular) to Block[B4]
            IInvocationOperation ( Function C.MoveNext([s As System.String = "ABC"]) As System.Boolean) (OperationKind.Invocation, Type: System.Boolean, IsImplicit) (Syntax: 'x')
              Instance Receiver: 
                IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: C, IsImplicit) (Syntax: 'x')
              Arguments(1):
                  IArgumentOperation (ArgumentKind.DefaultValue, Matching Parameter: s) (OperationKind.Argument, Type: null, IsImplicit) (Syntax: 'x')
                    ILiteralOperation (OperationKind.Literal, Type: System.String, Constant: "ABC", IsImplicit) (Syntax: 'x')
                    InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                    OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
            Leaving: {R1}

        Next (Regular) Block[B3]
            Entering: {R2}

    .locals {R2}
    {
        Locals: [z As System.Int32]
        Block[B3] - Block
            Predecessors: [B2]
            Statements (1)
                ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: null, IsImplicit) (Syntax: 'z')
                  Left: 
                    ILocalReferenceOperation: z (IsDeclaration: True) (OperationKind.LocalReference, Type: System.Int32, IsImplicit) (Syntax: 'z')
                  Right: 
                    IPropertyReferenceOperation: ReadOnly Property C.Current([d As System.Double = 123.45]) As System.Int32 (OperationKind.PropertyReference, Type: System.Int32, IsImplicit) (Syntax: 'z')
                      Instance Receiver: 
                        IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: C, IsImplicit) (Syntax: 'x')
                      Arguments(1):
                          IArgumentOperation (ArgumentKind.DefaultValue, Matching Parameter: d) (OperationKind.Argument, Type: null, IsImplicit) (Syntax: 'x')
                            ILiteralOperation (OperationKind.Literal, Type: System.Double, Constant: 123.45, IsImplicit) (Syntax: 'x')
                            InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                            OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)

            Next (Regular) Block[B2]
                Leaving: {R2}
    }
}

Block[B4] - Exit
    Predecessors: [B2]
    Statements (0)
]]>.Value

            VerifyFlowGraphAndDiagnosticsForTest(Of MethodBlockSyntax)(source, expectedFlowGraph, expectedDiagnostics, useLatestFramework:=True)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation, CompilerFeature.Dataflow)>
        <Fact()>
        Public Sub ForEachFlow_24()
            Dim source = <![CDATA[
Imports System
Public NotInheritable Class C
    Sub M(result as Integer) 'BIND:"Sub M"
        For Each x As Integer in GetC(x)
            result = x
        Next
    End Sub

    Public Function GetC(ByRef x as Integer) As C
        Return Me
    End Function
   
    Public Function GetEnumerator() As C
        Return Me
    End Function

    Public ReadOnly Property Current As Integer
        Get
            Return Nothing
        End Get
    End Property

    Public Function MoveNext() As Boolean
        Return False
    End Function

    Public Shared Function GetC() As C
        Return Nothing
    End Function
End Class
]]>.Value

            Dim expectedDiagnostics = String.Empty

            Dim expectedFlowGraph = <![CDATA[
Block[B0] - Entry
    Statements (0)
    Next (Regular) Block[B1]
        Entering: {R1} {R2}

.locals {R1}
{
    CaptureIds: [0]
    .locals {R2}
    {
        Locals: [x As System.Int32]
        Block[B1] - Block
            Predecessors: [B0]
            Statements (1)
                IFlowCaptureOperation: 0 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'GetC(x)')
                  Value: 
                    IInvocationOperation ( Function C.GetEnumerator() As C) (OperationKind.Invocation, Type: C, IsImplicit) (Syntax: 'GetC(x)')
                      Instance Receiver: 
                        IInvocationOperation ( Function C.GetC(ByRef x As System.Int32) As C) (OperationKind.Invocation, Type: C) (Syntax: 'GetC(x)')
                          Instance Receiver: 
                            IInstanceReferenceOperation (ReferenceKind: ContainingTypeInstance) (OperationKind.InstanceReference, Type: C, IsImplicit) (Syntax: 'GetC')
                          Arguments(1):
                              IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: x) (OperationKind.Argument, Type: null) (Syntax: 'x')
                                ILocalReferenceOperation: x (OperationKind.LocalReference, Type: System.Int32) (Syntax: 'x')
                                InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                                OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                      Arguments(0)

            Next (Regular) Block[B2]
                Leaving: {R2}
    }

    Block[B2] - Block
        Predecessors: [B1] [B3]
        Statements (0)
        Jump if False (Regular) to Block[B4]
            IInvocationOperation ( Function C.MoveNext() As System.Boolean) (OperationKind.Invocation, Type: System.Boolean, IsImplicit) (Syntax: 'GetC(x)')
              Instance Receiver: 
                IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: C, IsImplicit) (Syntax: 'GetC(x)')
              Arguments(0)
            Leaving: {R1}

        Next (Regular) Block[B3]
            Entering: {R3}

    .locals {R3}
    {
        Locals: [x As System.Int32]
        Block[B3] - Block
            Predecessors: [B2]
            Statements (2)
                ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: null, IsImplicit) (Syntax: 'x As Integer')
                  Left: 
                    ILocalReferenceOperation: x (IsDeclaration: True) (OperationKind.LocalReference, Type: System.Int32, IsImplicit) (Syntax: 'x As Integer')
                  Right: 
                    IPropertyReferenceOperation: ReadOnly Property C.Current As System.Int32 (OperationKind.PropertyReference, Type: System.Int32, IsImplicit) (Syntax: 'x As Integer')
                      Instance Receiver: 
                        IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: C, IsImplicit) (Syntax: 'GetC(x)')

                IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'result = x')
                  Expression: 
                    ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Int32, IsImplicit) (Syntax: 'result = x')
                      Left: 
                        IParameterReferenceOperation: result (OperationKind.ParameterReference, Type: System.Int32) (Syntax: 'result')
                      Right: 
                        ILocalReferenceOperation: x (OperationKind.LocalReference, Type: System.Int32) (Syntax: 'x')

            Next (Regular) Block[B2]
                Leaving: {R3}
    }
}

Block[B4] - Exit
    Predecessors: [B2]
    Statements (0)
]]>.Value

            VerifyFlowGraphAndDiagnosticsForTest(Of MethodBlockSyntax)(source, expectedFlowGraph, expectedDiagnostics)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation, CompilerFeature.Dataflow)>
        <Fact()>
        Public Sub ForEachFlow_25()
            Dim source = <![CDATA[
Imports System
Public NotInheritable Class C
    Sub M(result as C) 'BIND:"Sub M"
        For Each x As C in x
            result = x
        Next
    End Sub

    Public Function GetEnumerator() As C
        Return Me
    End Function

    Public ReadOnly Property Current As C
        Get
            Return Nothing
        End Get
    End Property

    Public Function MoveNext() As Boolean
        Return False
    End Function

    Public Shared Function GetC() As C
        Return Nothing
    End Function
End Class
]]>.Value

            Dim expectedDiagnostics = <![CDATA[
BC42104: Variable 'x' is used before it has been assigned a value. A null reference exception could result at runtime.
        For Each x As C in x
                           ~
]]>.Value

            Dim expectedFlowGraph = <![CDATA[
Block[B0] - Entry
    Statements (0)
    Next (Regular) Block[B1]
        Entering: {R1} {R2}

.locals {R1}
{
    CaptureIds: [0]
    .locals {R2}
    {
        Locals: [x As C]
        Block[B1] - Block
            Predecessors: [B0]
            Statements (1)
                IFlowCaptureOperation: 0 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'x')
                  Value: 
                    IInvocationOperation ( Function C.GetEnumerator() As C) (OperationKind.Invocation, Type: C, IsImplicit) (Syntax: 'x')
                      Instance Receiver: 
                        ILocalReferenceOperation: x (OperationKind.LocalReference, Type: C) (Syntax: 'x')
                      Arguments(0)

            Next (Regular) Block[B2]
                Leaving: {R2}
    }

    Block[B2] - Block
        Predecessors: [B1] [B3]
        Statements (0)
        Jump if False (Regular) to Block[B4]
            IInvocationOperation ( Function C.MoveNext() As System.Boolean) (OperationKind.Invocation, Type: System.Boolean, IsImplicit) (Syntax: 'x')
              Instance Receiver: 
                IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: C, IsImplicit) (Syntax: 'x')
              Arguments(0)
            Leaving: {R1}

        Next (Regular) Block[B3]
            Entering: {R3}

    .locals {R3}
    {
        Locals: [x As C]
        Block[B3] - Block
            Predecessors: [B2]
            Statements (2)
                ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: null, IsImplicit) (Syntax: 'x As C')
                  Left: 
                    ILocalReferenceOperation: x (IsDeclaration: True) (OperationKind.LocalReference, Type: C, IsImplicit) (Syntax: 'x As C')
                  Right: 
                    IPropertyReferenceOperation: ReadOnly Property C.Current As C (OperationKind.PropertyReference, Type: C, IsImplicit) (Syntax: 'x As C')
                      Instance Receiver: 
                        IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: C, IsImplicit) (Syntax: 'x')

                IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'result = x')
                  Expression: 
                    ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: C, IsImplicit) (Syntax: 'result = x')
                      Left: 
                        IParameterReferenceOperation: result (OperationKind.ParameterReference, Type: C) (Syntax: 'result')
                      Right: 
                        ILocalReferenceOperation: x (OperationKind.LocalReference, Type: C) (Syntax: 'x')

            Next (Regular) Block[B2]
                Leaving: {R3}
    }
}

Block[B4] - Exit
    Predecessors: [B2]
    Statements (0)
]]>.Value

            VerifyFlowGraphAndDiagnosticsForTest(Of MethodBlockSyntax)(source, expectedFlowGraph, expectedDiagnostics)
        End Sub
    End Class
End Namespace
