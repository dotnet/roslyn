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
        Public Sub IForLoopStatement_SimpleForLoopsTest()
            Dim source = <![CDATA[
Public Class MyClass1
    Public Shared Sub Main()
        Dim myarray As Integer() = New Integer(2) {1, 2, 3}
        For i As Integer = 0 To myarray.Length - 1'BIND:"For i As Integer = 0 To myarray.Length - 1"
            System.Console.WriteLine(myarray(i))
        Next
    End Sub
End Class
    ]]>.Value

            Dim expectedOperationTree = <![CDATA[
IForToLoopOperation (LoopKind.ForTo, Continue Label Id: 0, Exit Label Id: 1) (OperationKind.Loop, Type: null) (Syntax: 'For i As In ... Next')
  Locals: Local_1: i As System.Int32
  LoopControlVariable: 
    IVariableDeclaratorOperation (Symbol: i As System.Int32) (OperationKind.VariableDeclarator, Type: null) (Syntax: 'i As Integer')
      Initializer: 
        null
  InitialValue: 
    ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 0) (Syntax: '0')
  LimitValue: 
    IBinaryOperation (BinaryOperatorKind.Subtract, Checked) (OperationKind.BinaryOperator, Type: System.Int32) (Syntax: 'myarray.Length - 1')
      Left: 
        IPropertyReferenceOperation: ReadOnly Property System.Array.Length As System.Int32 (OperationKind.PropertyReference, Type: System.Int32) (Syntax: 'myarray.Length')
          Instance Receiver: 
            ILocalReferenceOperation: myarray (OperationKind.LocalReference, Type: System.Int32()) (Syntax: 'myarray')
      Right: 
        ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 1) (Syntax: '1')
  StepValue: 
    IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.Int32, Constant: 1, IsImplicit) (Syntax: 'For i As In ... Next')
      Conversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
      Operand: 
        ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 1, IsImplicit) (Syntax: 'For i As In ... Next')
  Body: 
    IBlockOperation (1 statements) (OperationKind.Block, Type: null, IsImplicit) (Syntax: 'For i As In ... Next')
      IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'System.Cons ... myarray(i))')
        Expression: 
          IInvocationOperation (Sub System.Console.WriteLine(value As System.Int32)) (OperationKind.Invocation, Type: System.Void) (Syntax: 'System.Cons ... myarray(i))')
            Instance Receiver: 
              null
            Arguments(1):
                IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: value) (OperationKind.Argument, Type: null) (Syntax: 'myarray(i)')
                  IArrayElementReferenceOperation (OperationKind.ArrayElementReference, Type: System.Int32) (Syntax: 'myarray(i)')
                    Array reference: 
                      ILocalReferenceOperation: myarray (OperationKind.LocalReference, Type: System.Int32()) (Syntax: 'myarray')
                    Indices(1):
                        ILocalReferenceOperation: i (OperationKind.LocalReference, Type: System.Int32) (Syntax: 'i')
                  InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                  OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
  NextVariables(0)
]]>.Value

            VerifyOperationTreeForTest(Of ForBlockSyntax)(source, expectedOperationTree)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation)>
        <Fact(), WorkItem(17602, "https://github.com/dotnet/roslyn/issues/17602")>
        Public Sub IForLoopStatement_SimpleForLoopsTestConversion()
            Dim source = <![CDATA[
Option Strict Off
Public Class MyClass1
    Public Shared Sub Main()
        Dim myarray As Integer() = New Integer(1) {}
        myarray(0) = 1
        myarray(1) = 2

        Dim s As Double = 1.1

        For i As Integer = 0 To "1" Step s'BIND:"For i As Integer = 0 To "1" Step s"
            System.Console.WriteLine(myarray(i))
        Next

    End Sub
End Class
    ]]>.Value

            Dim expectedOperationTree = <![CDATA[
IForToLoopOperation (LoopKind.ForTo, Continue Label Id: 0, Exit Label Id: 1) (OperationKind.Loop, Type: null) (Syntax: 'For i As In ... Next')
  Locals: Local_1: i As System.Int32
  LoopControlVariable: 
    IVariableDeclaratorOperation (Symbol: i As System.Int32) (OperationKind.VariableDeclarator, Type: null) (Syntax: 'i As Integer')
      Initializer: 
        null
  InitialValue: 
    ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 0) (Syntax: '0')
  LimitValue: 
    IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.Int32, IsImplicit) (Syntax: '"1"')
      Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
      Operand: 
        ILiteralOperation (OperationKind.Literal, Type: System.String, Constant: "1") (Syntax: '"1"')
  StepValue: 
    IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.Int32, IsImplicit) (Syntax: 's')
      Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: True, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
      Operand: 
        ILocalReferenceOperation: s (OperationKind.LocalReference, Type: System.Double) (Syntax: 's')
  Body: 
    IBlockOperation (1 statements) (OperationKind.Block, Type: null, IsImplicit) (Syntax: 'For i As In ... Next')
      IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'System.Cons ... myarray(i))')
        Expression: 
          IInvocationOperation (Sub System.Console.WriteLine(value As System.Int32)) (OperationKind.Invocation, Type: System.Void) (Syntax: 'System.Cons ... myarray(i))')
            Instance Receiver: 
              null
            Arguments(1):
                IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: value) (OperationKind.Argument, Type: null) (Syntax: 'myarray(i)')
                  IArrayElementReferenceOperation (OperationKind.ArrayElementReference, Type: System.Int32) (Syntax: 'myarray(i)')
                    Array reference: 
                      ILocalReferenceOperation: myarray (OperationKind.LocalReference, Type: System.Int32()) (Syntax: 'myarray')
                    Indices(1):
                        ILocalReferenceOperation: i (OperationKind.LocalReference, Type: System.Int32) (Syntax: 'i')
                  InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                  OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
  NextVariables(0)
]]>.Value

            VerifyOperationTreeForTest(Of ForBlockSyntax)(source, expectedOperationTree)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation)>
        <Fact(), WorkItem(17602, "https://github.com/dotnet/roslyn/issues/17602")>
        Public Sub IForLoopStatement_ForLoopStepIsFloatNegativeVar()
            Dim source = <![CDATA[
Option Strict On
Public Class MyClass1
    Public Shared Sub Main()
        Dim s As Double = -1.1

        For i As Double = 2 To 0 Step s'BIND:"For i As Double = 2 To 0 Step s"
            System.Console.WriteLine(i)
        Next

    End Sub
End Class
    ]]>.Value

            Dim expectedOperationTree = <![CDATA[
IForToLoopOperation (LoopKind.ForTo, Continue Label Id: 0, Exit Label Id: 1) (OperationKind.Loop, Type: null) (Syntax: 'For i As Do ... Next')
  Locals: Local_1: i As System.Double
  LoopControlVariable: 
    IVariableDeclaratorOperation (Symbol: i As System.Double) (OperationKind.VariableDeclarator, Type: null) (Syntax: 'i As Double')
      Initializer: 
        null
  InitialValue: 
    IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.Double, Constant: 2, IsImplicit) (Syntax: '2')
      Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: True, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
      Operand: 
        ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 2) (Syntax: '2')
  LimitValue: 
    IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.Double, Constant: 0, IsImplicit) (Syntax: '0')
      Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: True, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
      Operand: 
        ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 0) (Syntax: '0')
  StepValue: 
    ILocalReferenceOperation: s (OperationKind.LocalReference, Type: System.Double) (Syntax: 's')
  Body: 
    IBlockOperation (1 statements) (OperationKind.Block, Type: null, IsImplicit) (Syntax: 'For i As Do ... Next')
      IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'System.Cons ... riteLine(i)')
        Expression: 
          IInvocationOperation (Sub System.Console.WriteLine(value As System.Double)) (OperationKind.Invocation, Type: System.Void) (Syntax: 'System.Cons ... riteLine(i)')
            Instance Receiver: 
              null
            Arguments(1):
                IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: value) (OperationKind.Argument, Type: null) (Syntax: 'i')
                  ILocalReferenceOperation: i (OperationKind.LocalReference, Type: System.Double) (Syntax: 'i')
                  InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                  OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
  NextVariables(0)
]]>.Value

            VerifyOperationTreeForTest(Of ForBlockSyntax)(source, expectedOperationTree)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation)>
        <Fact(), WorkItem(17602, "https://github.com/dotnet/roslyn/issues/17602")>
        Public Sub IForLoopStatement_ForLoopObject()
            Dim source = <![CDATA[
Option Strict On
Public Class MyClass1
    Public Shared Sub Main()

        Dim ctrlVar As Object
        Dim initValue As Object = 0
        Dim limit As Object = 2
        Dim stp As Object = 1

        For ctrlVar = initValue To limit Step stp'BIND:"For ctrlVar = initValue To limit Step stp"
            System.Console.WriteLine(ctrlVar)
        Next

    End Sub
End Class
    ]]>.Value

            Dim expectedOperationTree = <![CDATA[
IForToLoopOperation (LoopKind.ForTo, Continue Label Id: 0, Exit Label Id: 1) (OperationKind.Loop, Type: null) (Syntax: 'For ctrlVar ... Next')
  LoopControlVariable: 
    ILocalReferenceOperation: ctrlVar (OperationKind.LocalReference, Type: System.Object) (Syntax: 'ctrlVar')
  InitialValue: 
    ILocalReferenceOperation: initValue (OperationKind.LocalReference, Type: System.Object) (Syntax: 'initValue')
  LimitValue: 
    ILocalReferenceOperation: limit (OperationKind.LocalReference, Type: System.Object) (Syntax: 'limit')
  StepValue: 
    ILocalReferenceOperation: stp (OperationKind.LocalReference, Type: System.Object) (Syntax: 'stp')
  Body: 
    IBlockOperation (1 statements) (OperationKind.Block, Type: null, IsImplicit) (Syntax: 'For ctrlVar ... Next')
      IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'System.Cons ... ne(ctrlVar)')
        Expression: 
          IInvocationOperation (Sub System.Console.WriteLine(value As System.Object)) (OperationKind.Invocation, Type: System.Void) (Syntax: 'System.Cons ... ne(ctrlVar)')
            Instance Receiver: 
              null
            Arguments(1):
                IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: value) (OperationKind.Argument, Type: null) (Syntax: 'ctrlVar')
                  ILocalReferenceOperation: ctrlVar (OperationKind.LocalReference, Type: System.Object) (Syntax: 'ctrlVar')
                  InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                  OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
  NextVariables(0)
]]>.Value

            VerifyOperationTreeForTest(Of ForBlockSyntax)(source, expectedOperationTree)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation)>
        <Fact(), WorkItem(17602, "https://github.com/dotnet/roslyn/issues/17602")>
        Public Sub IForLoopStatement_ForLoopNested()
            Dim source = <![CDATA[
Option Strict On
Option Infer On
Public Class MyClass1
    Public Shared Sub Main()
        For AVarName = 1 To 2'BIND:"For AVarName = 1 To 2"
            For B = 1 To 2
                For C = 1 To 2
                    For D = 1 To 2
                    Next D
                Next C
            Next B
        Next AVarName
    End Sub
End Class

    ]]>.Value

            Dim expectedOperationTree = <![CDATA[
IForToLoopOperation (LoopKind.ForTo, Continue Label Id: 0, Exit Label Id: 1) (OperationKind.Loop, Type: null) (Syntax: 'For AVarNam ... xt AVarName')
  Locals: Local_1: AVarName As System.Int32
  LoopControlVariable: 
    IVariableDeclaratorOperation (Symbol: AVarName As System.Int32) (OperationKind.VariableDeclarator, Type: null) (Syntax: 'AVarName')
      Initializer: 
        null
  InitialValue: 
    ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 1) (Syntax: '1')
  LimitValue: 
    ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 2) (Syntax: '2')
  StepValue: 
    IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.Int32, Constant: 1, IsImplicit) (Syntax: 'For AVarNam ... xt AVarName')
      Conversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
      Operand: 
        ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 1, IsImplicit) (Syntax: 'For AVarNam ... xt AVarName')
  Body: 
    IBlockOperation (1 statements) (OperationKind.Block, Type: null, IsImplicit) (Syntax: 'For AVarNam ... xt AVarName')
      IForToLoopOperation (LoopKind.ForTo, Continue Label Id: 2, Exit Label Id: 3) (OperationKind.Loop, Type: null) (Syntax: 'For B = 1 T ... Next B')
        Locals: Local_1: B As System.Int32
        LoopControlVariable: 
          IVariableDeclaratorOperation (Symbol: B As System.Int32) (OperationKind.VariableDeclarator, Type: null) (Syntax: 'B')
            Initializer: 
              null
        InitialValue: 
          ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 1) (Syntax: '1')
        LimitValue: 
          ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 2) (Syntax: '2')
        StepValue: 
          IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.Int32, Constant: 1, IsImplicit) (Syntax: 'For B = 1 T ... Next B')
            Conversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
            Operand: 
              ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 1, IsImplicit) (Syntax: 'For B = 1 T ... Next B')
        Body: 
          IBlockOperation (1 statements) (OperationKind.Block, Type: null, IsImplicit) (Syntax: 'For B = 1 T ... Next B')
            IForToLoopOperation (LoopKind.ForTo, Continue Label Id: 4, Exit Label Id: 5) (OperationKind.Loop, Type: null) (Syntax: 'For C = 1 T ... Next C')
              Locals: Local_1: C As System.Int32
              LoopControlVariable: 
                IVariableDeclaratorOperation (Symbol: C As System.Int32) (OperationKind.VariableDeclarator, Type: null) (Syntax: 'C')
                  Initializer: 
                    null
              InitialValue: 
                ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 1) (Syntax: '1')
              LimitValue: 
                ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 2) (Syntax: '2')
              StepValue: 
                IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.Int32, Constant: 1, IsImplicit) (Syntax: 'For C = 1 T ... Next C')
                  Conversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                  Operand: 
                    ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 1, IsImplicit) (Syntax: 'For C = 1 T ... Next C')
              Body: 
                IBlockOperation (1 statements) (OperationKind.Block, Type: null, IsImplicit) (Syntax: 'For C = 1 T ... Next C')
                  IForToLoopOperation (LoopKind.ForTo, Continue Label Id: 6, Exit Label Id: 7) (OperationKind.Loop, Type: null) (Syntax: 'For D = 1 T ... Next D')
                    Locals: Local_1: D As System.Int32
                    LoopControlVariable: 
                      IVariableDeclaratorOperation (Symbol: D As System.Int32) (OperationKind.VariableDeclarator, Type: null) (Syntax: 'D')
                        Initializer: 
                          null
                    InitialValue: 
                      ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 1) (Syntax: '1')
                    LimitValue: 
                      ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 2) (Syntax: '2')
                    StepValue: 
                      IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.Int32, Constant: 1, IsImplicit) (Syntax: 'For D = 1 T ... Next D')
                        Conversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                        Operand: 
                          ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 1, IsImplicit) (Syntax: 'For D = 1 T ... Next D')
                    Body: 
                      IBlockOperation (0 statements) (OperationKind.Block, Type: null, IsImplicit) (Syntax: 'For D = 1 T ... Next D')
                    NextVariables(1):
                        ILocalReferenceOperation: D (OperationKind.LocalReference, Type: System.Int32) (Syntax: 'D')
              NextVariables(1):
                  ILocalReferenceOperation: C (OperationKind.LocalReference, Type: System.Int32) (Syntax: 'C')
        NextVariables(1):
            ILocalReferenceOperation: B (OperationKind.LocalReference, Type: System.Int32) (Syntax: 'B')
  NextVariables(1):
      ILocalReferenceOperation: AVarName (OperationKind.LocalReference, Type: System.Int32) (Syntax: 'AVarName')
]]>.Value

            VerifyOperationTreeForTest(Of ForBlockSyntax)(source, expectedOperationTree)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation)>
        <Fact(), WorkItem(17602, "https://github.com/dotnet/roslyn/issues/17602")>
        Public Sub IForLoopStatement_ChangeOuterVarInInnerFor()
            Dim source = <![CDATA[
Option Strict On
Option Infer On
Public Class MyClass1
    Public Shared Sub Main()
        For I = 1 To 2'BIND:"For I = 1 To 2"
            For J = 1 To 2
                I = 3
                System.Console.WriteLine(I)
            Next
        Next
    End Sub
End Class


    ]]>.Value

            Dim expectedOperationTree = <![CDATA[
IForToLoopOperation (LoopKind.ForTo, Continue Label Id: 0, Exit Label Id: 1) (OperationKind.Loop, Type: null) (Syntax: 'For I = 1 T ... Next')
  Locals: Local_1: I As System.Int32
  LoopControlVariable: 
    IVariableDeclaratorOperation (Symbol: I As System.Int32) (OperationKind.VariableDeclarator, Type: null) (Syntax: 'I')
      Initializer: 
        null
  InitialValue: 
    ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 1) (Syntax: '1')
  LimitValue: 
    ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 2) (Syntax: '2')
  StepValue: 
    IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.Int32, Constant: 1, IsImplicit) (Syntax: 'For I = 1 T ... Next')
      Conversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
      Operand: 
        ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 1, IsImplicit) (Syntax: 'For I = 1 T ... Next')
  Body: 
    IBlockOperation (1 statements) (OperationKind.Block, Type: null, IsImplicit) (Syntax: 'For I = 1 T ... Next')
      IForToLoopOperation (LoopKind.ForTo, Continue Label Id: 2, Exit Label Id: 3) (OperationKind.Loop, Type: null) (Syntax: 'For J = 1 T ... Next')
        Locals: Local_1: J As System.Int32
        LoopControlVariable: 
          IVariableDeclaratorOperation (Symbol: J As System.Int32) (OperationKind.VariableDeclarator, Type: null) (Syntax: 'J')
            Initializer: 
              null
        InitialValue: 
          ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 1) (Syntax: '1')
        LimitValue: 
          ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 2) (Syntax: '2')
        StepValue: 
          IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.Int32, Constant: 1, IsImplicit) (Syntax: 'For J = 1 T ... Next')
            Conversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
            Operand: 
              ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 1, IsImplicit) (Syntax: 'For J = 1 T ... Next')
        Body: 
          IBlockOperation (2 statements) (OperationKind.Block, Type: null, IsImplicit) (Syntax: 'For J = 1 T ... Next')
            IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'I = 3')
              Expression: 
                ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Int32, IsImplicit) (Syntax: 'I = 3')
                  Left: 
                    ILocalReferenceOperation: I (OperationKind.LocalReference, Type: System.Int32) (Syntax: 'I')
                  Right: 
                    ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 3) (Syntax: '3')
            IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'System.Cons ... riteLine(I)')
              Expression: 
                IInvocationOperation (Sub System.Console.WriteLine(value As System.Int32)) (OperationKind.Invocation, Type: System.Void) (Syntax: 'System.Cons ... riteLine(I)')
                  Instance Receiver: 
                    null
                  Arguments(1):
                      IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: value) (OperationKind.Argument, Type: null) (Syntax: 'I')
                        ILocalReferenceOperation: I (OperationKind.LocalReference, Type: System.Int32) (Syntax: 'I')
                        InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                        OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
        NextVariables(0)
  NextVariables(0)
]]>.Value

            VerifyOperationTreeForTest(Of ForBlockSyntax)(source, expectedOperationTree)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation)>
        <Fact(), WorkItem(17602, "https://github.com/dotnet/roslyn/issues/17602")>
        Public Sub IForLoopStatement_InnerForRefOuterForVar()
            Dim source = <![CDATA[
Option Strict On
Option Infer On
Public Class MyClass1
    Public Shared Sub Main()
        For I = 1 To 2'BIND:"For I = 1 To 2"
            For J = I + 1 To 2
                System.Console.WriteLine(J)
            Next
        Next
    End Sub
End Class


    ]]>.Value

            Dim expectedOperationTree = <![CDATA[
IForToLoopOperation (LoopKind.ForTo, Continue Label Id: 0, Exit Label Id: 1) (OperationKind.Loop, Type: null) (Syntax: 'For I = 1 T ... Next')
  Locals: Local_1: I As System.Int32
  LoopControlVariable: 
    IVariableDeclaratorOperation (Symbol: I As System.Int32) (OperationKind.VariableDeclarator, Type: null) (Syntax: 'I')
      Initializer: 
        null
  InitialValue: 
    ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 1) (Syntax: '1')
  LimitValue: 
    ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 2) (Syntax: '2')
  StepValue: 
    IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.Int32, Constant: 1, IsImplicit) (Syntax: 'For I = 1 T ... Next')
      Conversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
      Operand: 
        ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 1, IsImplicit) (Syntax: 'For I = 1 T ... Next')
  Body: 
    IBlockOperation (1 statements) (OperationKind.Block, Type: null, IsImplicit) (Syntax: 'For I = 1 T ... Next')
      IForToLoopOperation (LoopKind.ForTo, Continue Label Id: 2, Exit Label Id: 3) (OperationKind.Loop, Type: null) (Syntax: 'For J = I + ... Next')
        Locals: Local_1: J As System.Int32
        LoopControlVariable: 
          IVariableDeclaratorOperation (Symbol: J As System.Int32) (OperationKind.VariableDeclarator, Type: null) (Syntax: 'J')
            Initializer: 
              null
        InitialValue: 
          IBinaryOperation (BinaryOperatorKind.Add, Checked) (OperationKind.BinaryOperator, Type: System.Int32) (Syntax: 'I + 1')
            Left: 
              ILocalReferenceOperation: I (OperationKind.LocalReference, Type: System.Int32) (Syntax: 'I')
            Right: 
              ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 1) (Syntax: '1')
        LimitValue: 
          ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 2) (Syntax: '2')
        StepValue: 
          IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.Int32, Constant: 1, IsImplicit) (Syntax: 'For J = I + ... Next')
            Conversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
            Operand: 
              ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 1, IsImplicit) (Syntax: 'For J = I + ... Next')
        Body: 
          IBlockOperation (1 statements) (OperationKind.Block, Type: null, IsImplicit) (Syntax: 'For J = I + ... Next')
            IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'System.Cons ... riteLine(J)')
              Expression: 
                IInvocationOperation (Sub System.Console.WriteLine(value As System.Int32)) (OperationKind.Invocation, Type: System.Void) (Syntax: 'System.Cons ... riteLine(J)')
                  Instance Receiver: 
                    null
                  Arguments(1):
                      IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: value) (OperationKind.Argument, Type: null) (Syntax: 'J')
                        ILocalReferenceOperation: J (OperationKind.LocalReference, Type: System.Int32) (Syntax: 'J')
                        InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                        OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
        NextVariables(0)
  NextVariables(0)
]]>.Value

            VerifyOperationTreeForTest(Of ForBlockSyntax)(source, expectedOperationTree)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation)>
        <Fact(), WorkItem(17602, "https://github.com/dotnet/roslyn/issues/17602")>
        Public Sub IForLoopStatement_ExitNestedFor()
            Dim source = <![CDATA[
Option Strict On
Option Infer On
Public Class MyClass1
    Public Shared Sub Main()
        For I = 1 To 2'BIND:"For I = 1 To 2"
            For J = 1 To 2
                Exit For
            Next
            System.Console.WriteLine(I)
        Next
    End Sub
End Class

    ]]>.Value

            Dim expectedOperationTree = <![CDATA[
IForToLoopOperation (LoopKind.ForTo, Continue Label Id: 0, Exit Label Id: 1) (OperationKind.Loop, Type: null) (Syntax: 'For I = 1 T ... Next')
  Locals: Local_1: I As System.Int32
  LoopControlVariable: 
    IVariableDeclaratorOperation (Symbol: I As System.Int32) (OperationKind.VariableDeclarator, Type: null) (Syntax: 'I')
      Initializer: 
        null
  InitialValue: 
    ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 1) (Syntax: '1')
  LimitValue: 
    ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 2) (Syntax: '2')
  StepValue: 
    IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.Int32, Constant: 1, IsImplicit) (Syntax: 'For I = 1 T ... Next')
      Conversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
      Operand: 
        ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 1, IsImplicit) (Syntax: 'For I = 1 T ... Next')
  Body: 
    IBlockOperation (2 statements) (OperationKind.Block, Type: null, IsImplicit) (Syntax: 'For I = 1 T ... Next')
      IForToLoopOperation (LoopKind.ForTo, Continue Label Id: 2, Exit Label Id: 3) (OperationKind.Loop, Type: null) (Syntax: 'For J = 1 T ... Next')
        Locals: Local_1: J As System.Int32
        LoopControlVariable: 
          IVariableDeclaratorOperation (Symbol: J As System.Int32) (OperationKind.VariableDeclarator, Type: null) (Syntax: 'J')
            Initializer: 
              null
        InitialValue: 
          ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 1) (Syntax: '1')
        LimitValue: 
          ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 2) (Syntax: '2')
        StepValue: 
          IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.Int32, Constant: 1, IsImplicit) (Syntax: 'For J = 1 T ... Next')
            Conversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
            Operand: 
              ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 1, IsImplicit) (Syntax: 'For J = 1 T ... Next')
        Body: 
          IBlockOperation (1 statements) (OperationKind.Block, Type: null, IsImplicit) (Syntax: 'For J = 1 T ... Next')
            IBranchOperation (BranchKind.Break, Label Id: 3) (OperationKind.Branch, Type: null) (Syntax: 'Exit For')
        NextVariables(0)
      IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'System.Cons ... riteLine(I)')
        Expression: 
          IInvocationOperation (Sub System.Console.WriteLine(value As System.Int32)) (OperationKind.Invocation, Type: System.Void) (Syntax: 'System.Cons ... riteLine(I)')
            Instance Receiver: 
              null
            Arguments(1):
                IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: value) (OperationKind.Argument, Type: null) (Syntax: 'I')
                  ILocalReferenceOperation: I (OperationKind.LocalReference, Type: System.Int32) (Syntax: 'I')
                  InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                  OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
  NextVariables(0)
]]>.Value

            VerifyOperationTreeForTest(Of ForBlockSyntax)(source, expectedOperationTree)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation)>
        <Fact(), WorkItem(17602, "https://github.com/dotnet/roslyn/issues/17602")>
        Public Sub IForLoopStatement_EnumAsStart()
            Dim source = <![CDATA[
Option Strict Off
Option Infer Off
Public Class MyClass1
    Public Shared Sub Main()
        For x As e1 = e1.a To e1.c'BIND:"For x As e1 = e1.a To e1.c"
        Next
    End Sub
End Class
Enum e1
    a
    b
    c
End Enum
    ]]>.Value

            Dim expectedOperationTree = <![CDATA[
IForToLoopOperation (LoopKind.ForTo, Continue Label Id: 0, Exit Label Id: 1) (OperationKind.Loop, Type: null) (Syntax: 'For x As e1 ... Next')
  Locals: Local_1: x As e1
  LoopControlVariable: 
    IVariableDeclaratorOperation (Symbol: x As e1) (OperationKind.VariableDeclarator, Type: null) (Syntax: 'x As e1')
      Initializer: 
        null
  InitialValue: 
    IFieldReferenceOperation: e1.a (Static) (OperationKind.FieldReference, Type: e1, Constant: 0) (Syntax: 'e1.a')
      Instance Receiver: 
        null
  LimitValue: 
    IFieldReferenceOperation: e1.c (Static) (OperationKind.FieldReference, Type: e1, Constant: 2) (Syntax: 'e1.c')
      Instance Receiver: 
        null
  StepValue: 
    IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: e1, Constant: 1, IsImplicit) (Syntax: 'For x As e1 ... Next')
      Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: True, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
      Operand: 
        ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 1, IsImplicit) (Syntax: 'For x As e1 ... Next')
  Body: 
    IBlockOperation (0 statements) (OperationKind.Block, Type: null, IsImplicit) (Syntax: 'For x As e1 ... Next')
  NextVariables(0)
]]>.Value

            VerifyOperationTreeForTest(Of ForBlockSyntax)(source, expectedOperationTree)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation)>
        <Fact(), WorkItem(17602, "https://github.com/dotnet/roslyn/issues/17602")>
        Public Sub IForLoopStatement_PropertyAsStart()
            Dim source = <![CDATA[
Option Strict Off
Option Infer Off
Public Class MyClass1
    Property P1(ByVal x As Long) As Byte
        Get
            Return x - 10
        End Get
        Set(ByVal Value As Byte)
        End Set
    End Property
    Public Shared Sub Main()
    End Sub
    Public Sub F()
        For i As Integer = P1(30 + i) To 30'BIND:"For i As Integer = P1(30 + i) To 30"
        Next
    End Sub
End Class
    ]]>.Value

            Dim expectedOperationTree = <![CDATA[
IForToLoopOperation (LoopKind.ForTo, Continue Label Id: 0, Exit Label Id: 1) (OperationKind.Loop, Type: null) (Syntax: 'For i As In ... Next')
  Locals: Local_1: i As System.Int32
  LoopControlVariable: 
    IVariableDeclaratorOperation (Symbol: i As System.Int32) (OperationKind.VariableDeclarator, Type: null) (Syntax: 'i As Integer')
      Initializer: 
        null
  InitialValue: 
    IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.Int32, IsImplicit) (Syntax: 'P1(30 + i)')
      Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: True, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
      Operand: 
        IPropertyReferenceOperation: Property MyClass1.P1(x As System.Int64) As System.Byte (OperationKind.PropertyReference, Type: System.Byte) (Syntax: 'P1(30 + i)')
          Instance Receiver: 
            IInstanceReferenceOperation (OperationKind.InstanceReference, Type: MyClass1, IsImplicit) (Syntax: 'P1')
          Arguments(1):
              IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: x) (OperationKind.Argument, Type: null) (Syntax: '30 + i')
                IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.Int64, IsImplicit) (Syntax: '30 + i')
                  Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: True, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                  Operand: 
                    IBinaryOperation (BinaryOperatorKind.Add, Checked) (OperationKind.BinaryOperator, Type: System.Int32) (Syntax: '30 + i')
                      Left: 
                        ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 30) (Syntax: '30')
                      Right: 
                        ILocalReferenceOperation: i (OperationKind.LocalReference, Type: System.Int32) (Syntax: 'i')
                InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
  LimitValue: 
    ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 30) (Syntax: '30')
  StepValue: 
    IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.Int32, Constant: 1, IsImplicit) (Syntax: 'For i As In ... Next')
      Conversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
      Operand: 
        ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 1, IsImplicit) (Syntax: 'For i As In ... Next')
  Body: 
    IBlockOperation (0 statements) (OperationKind.Block, Type: null, IsImplicit) (Syntax: 'For i As In ... Next')
  NextVariables(0)
]]>.Value

            VerifyOperationTreeForTest(Of ForBlockSyntax)(source, expectedOperationTree)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation)>
        <Fact(), WorkItem(17602, "https://github.com/dotnet/roslyn/issues/17602")>
        Public Sub IForLoopStatement_FieldNameAsIteration()
            Dim source = <![CDATA[
Option Strict Off
Option Infer On
Public Class MyClass1
    Dim global_x As Integer = 10
    Const global_y As Long = 20
    Public Shared Sub Main()
        For global_x As Integer = global_y To 10'BIND:"For global_x As Integer = global_y To 10"
        Next
    End Sub
End Class

    ]]>.Value

            Dim expectedOperationTree = <![CDATA[
IForToLoopOperation (LoopKind.ForTo, Continue Label Id: 0, Exit Label Id: 1) (OperationKind.Loop, Type: null) (Syntax: 'For global_ ... Next')
  Locals: Local_1: global_x As System.Int32
  LoopControlVariable: 
    IVariableDeclaratorOperation (Symbol: global_x As System.Int32) (OperationKind.VariableDeclarator, Type: null) (Syntax: 'global_x As Integer')
      Initializer: 
        null
  InitialValue: 
    IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.Int32, Constant: 20, IsImplicit) (Syntax: 'global_y')
      Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: True, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
      Operand: 
        IFieldReferenceOperation: MyClass1.global_y As System.Int64 (Static) (OperationKind.FieldReference, Type: System.Int64, Constant: 20) (Syntax: 'global_y')
          Instance Receiver: 
            null
  LimitValue: 
    ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 10) (Syntax: '10')
  StepValue: 
    IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.Int32, Constant: 1, IsImplicit) (Syntax: 'For global_ ... Next')
      Conversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
      Operand: 
        ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 1, IsImplicit) (Syntax: 'For global_ ... Next')
  Body: 
    IBlockOperation (0 statements) (OperationKind.Block, Type: null, IsImplicit) (Syntax: 'For global_ ... Next')
  NextVariables(0)
]]>.Value

            VerifyOperationTreeForTest(Of ForBlockSyntax)(source, expectedOperationTree)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation)>
        <Fact(), WorkItem(17602, "https://github.com/dotnet/roslyn/issues/17602")>
        Public Sub IForLoopStatement_SingleLine()
            Dim source = <![CDATA[
Option Strict On
Public Class MyClass1
    Public Shared Sub Main()
        For x As Integer = 0 To 10 : Next'BIND:"For x As Integer = 0 To 10 : Next"
    End Sub
End Class

    ]]>.Value

            Dim expectedOperationTree = <![CDATA[
IForToLoopOperation (LoopKind.ForTo, Continue Label Id: 0, Exit Label Id: 1) (OperationKind.Loop, Type: null) (Syntax: 'For x As In ... o 10 : Next')
  Locals: Local_1: x As System.Int32
  LoopControlVariable: 
    IVariableDeclaratorOperation (Symbol: x As System.Int32) (OperationKind.VariableDeclarator, Type: null) (Syntax: 'x As Integer')
      Initializer: 
        null
  InitialValue: 
    ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 0) (Syntax: '0')
  LimitValue: 
    ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 10) (Syntax: '10')
  StepValue: 
    IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.Int32, Constant: 1, IsImplicit) (Syntax: 'For x As In ... o 10 : Next')
      Conversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
      Operand: 
        ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 1, IsImplicit) (Syntax: 'For x As In ... o 10 : Next')
  Body: 
    IBlockOperation (0 statements) (OperationKind.Block, Type: null, IsImplicit) (Syntax: 'For x As In ... o 10 : Next')
  NextVariables(0)
]]>.Value

            VerifyOperationTreeForTest(Of ForBlockSyntax)(source, expectedOperationTree)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation)>
        <Fact(), WorkItem(17602, "https://github.com/dotnet/roslyn/issues/17602")>
        Public Sub IForLoopStatement_VarDeclOutOfForeach()
            Dim source = <![CDATA[
Option Strict On
Option Infer On
Public Class MyClass1
    Public Shared Sub Main()
        Dim Y As Integer
        For Y = 1 To 2'BIND:"For Y = 1 To 2"
        Next
    End Sub
End Class
    ]]>.Value

            Dim expectedOperationTree = <![CDATA[
IForToLoopOperation (LoopKind.ForTo, Continue Label Id: 0, Exit Label Id: 1) (OperationKind.Loop, Type: null) (Syntax: 'For Y = 1 T ... Next')
  LoopControlVariable: 
    ILocalReferenceOperation: Y (OperationKind.LocalReference, Type: System.Int32) (Syntax: 'Y')
  InitialValue: 
    ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 1) (Syntax: '1')
  LimitValue: 
    ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 2) (Syntax: '2')
  StepValue: 
    IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.Int32, Constant: 1, IsImplicit) (Syntax: 'For Y = 1 T ... Next')
      Conversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
      Operand: 
        ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 1, IsImplicit) (Syntax: 'For Y = 1 T ... Next')
  Body: 
    IBlockOperation (0 statements) (OperationKind.Block, Type: null, IsImplicit) (Syntax: 'For Y = 1 T ... Next')
  NextVariables(0)
]]>.Value

            VerifyOperationTreeForTest(Of ForBlockSyntax)(source, expectedOperationTree)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation)>
        <Fact(), WorkItem(17602, "https://github.com/dotnet/roslyn/issues/17602")>
        Public Sub IForLoopStatement_GetDeclaredSymbolOfForStatement()
            Dim source = <![CDATA[
Option Strict On
Option Infer On

Imports System
Imports System.Collection

Class C1
    Public Shared Sub Main()
        For element1 = 23 To 42'BIND:"For element1 = 23 To 42"
        Next

        For element2 As Integer = 23 To 42
        Next
    End Sub
End Class

    ]]>.Value

            Dim expectedOperationTree = <![CDATA[
IForToLoopOperation (LoopKind.ForTo, Continue Label Id: 0, Exit Label Id: 1) (OperationKind.Loop, Type: null) (Syntax: 'For element ... Next')
  Locals: Local_1: element1 As System.Int32
  LoopControlVariable: 
    IVariableDeclaratorOperation (Symbol: element1 As System.Int32) (OperationKind.VariableDeclarator, Type: null) (Syntax: 'element1')
      Initializer: 
        null
  InitialValue: 
    ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 23) (Syntax: '23')
  LimitValue: 
    ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 42) (Syntax: '42')
  StepValue: 
    IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.Int32, Constant: 1, IsImplicit) (Syntax: 'For element ... Next')
      Conversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
      Operand: 
        ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 1, IsImplicit) (Syntax: 'For element ... Next')
  Body: 
    IBlockOperation (0 statements) (OperationKind.Block, Type: null, IsImplicit) (Syntax: 'For element ... Next')
  NextVariables(0)
]]>.Value

            VerifyOperationTreeForTest(Of ForBlockSyntax)(source, expectedOperationTree)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation)>
        <Fact(), WorkItem(17602, "https://github.com/dotnet/roslyn/issues/17602")>
        Public Sub IForLoopStatement_ForLoopContinue()
            Dim source = <![CDATA[
Option Strict On
Option Infer On

Imports System
Imports System.Collection

Class C1
    Public Shared Sub Main()
        For i As Integer = 0 To 5'BIND:"For i As Integer = 0 To 5"
            If i Mod 2 = 0 Then
                Continue For
            End If
        Next
    End Sub
End Class

    ]]>.Value

            Dim expectedOperationTree = <![CDATA[
IForToLoopOperation (LoopKind.ForTo, Continue Label Id: 0, Exit Label Id: 1) (OperationKind.Loop, Type: null) (Syntax: 'For i As In ... Next')
  Locals: Local_1: i As System.Int32
  LoopControlVariable: 
    IVariableDeclaratorOperation (Symbol: i As System.Int32) (OperationKind.VariableDeclarator, Type: null) (Syntax: 'i As Integer')
      Initializer: 
        null
  InitialValue: 
    ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 0) (Syntax: '0')
  LimitValue: 
    ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 5) (Syntax: '5')
  StepValue: 
    IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.Int32, Constant: 1, IsImplicit) (Syntax: 'For i As In ... Next')
      Conversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
      Operand: 
        ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 1, IsImplicit) (Syntax: 'For i As In ... Next')
  Body: 
    IBlockOperation (1 statements) (OperationKind.Block, Type: null, IsImplicit) (Syntax: 'For i As In ... Next')
      IConditionalOperation (OperationKind.Conditional, Type: null) (Syntax: 'If i Mod 2  ... End If')
        Condition: 
          IBinaryOperation (BinaryOperatorKind.Equals, Checked) (OperationKind.BinaryOperator, Type: System.Boolean) (Syntax: 'i Mod 2 = 0')
            Left: 
              IBinaryOperation (BinaryOperatorKind.Remainder, Checked) (OperationKind.BinaryOperator, Type: System.Int32) (Syntax: 'i Mod 2')
                Left: 
                  ILocalReferenceOperation: i (OperationKind.LocalReference, Type: System.Int32) (Syntax: 'i')
                Right: 
                  ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 2) (Syntax: '2')
            Right: 
              ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 0) (Syntax: '0')
        WhenTrue: 
          IBlockOperation (1 statements) (OperationKind.Block, Type: null, IsImplicit) (Syntax: 'If i Mod 2  ... End If')
            IBranchOperation (BranchKind.Continue, Label Id: 0) (OperationKind.Branch, Type: null) (Syntax: 'Continue For')
        WhenFalse: 
          null
  NextVariables(0)
]]>.Value

            VerifyOperationTreeForTest(Of ForBlockSyntax)(source, expectedOperationTree)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation)>
        <Fact(), WorkItem(17602, "https://github.com/dotnet/roslyn/issues/17602")>
        Public Sub IForLoopStatement_ForReverse()
            Dim source = <![CDATA[
Option Infer On

Module Program
    Sub Main()
        For X = 10 To 0'BIND:"For X = 10 To 0"
        Next
    End Sub
End Module

Module M
    Public X As Integer
End Module

]]>.Value

            Dim expectedOperationTree = <![CDATA[
IForToLoopOperation (LoopKind.ForTo, Continue Label Id: 0, Exit Label Id: 1) (OperationKind.Loop, Type: null) (Syntax: 'For X = 10  ... Next')
  LoopControlVariable: 
    IFieldReferenceOperation: M.X As System.Int32 (Static) (OperationKind.FieldReference, Type: System.Int32) (Syntax: 'X')
      Instance Receiver: 
        null
  InitialValue: 
    ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 10) (Syntax: '10')
  LimitValue: 
    ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 0) (Syntax: '0')
  StepValue: 
    IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.Int32, Constant: 1, IsImplicit) (Syntax: 'For X = 10  ... Next')
      Conversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
      Operand: 
        ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 1, IsImplicit) (Syntax: 'For X = 10  ... Next')
  Body: 
    IBlockOperation (0 statements) (OperationKind.Block, Type: null, IsImplicit) (Syntax: 'For X = 10  ... Next')
  NextVariables(0)
]]>.Value

            Dim expectedDiagnostics = String.Empty

            VerifyOperationTreeAndDiagnosticsForTest(Of ForBlockSyntax)(source, expectedOperationTree, expectedDiagnostics)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation)>
        <Fact(), WorkItem(17602, "https://github.com/dotnet/roslyn/issues/17602")>
        Public Sub IForLoopStatement_InValid()
            Dim source = <![CDATA[
Option Infer On

Module Program
    Sub Main()
        For X = 10 To 0'BIND:"For X = 10 To 0"
        Next
    End Sub
End Module

Module M
    Public X As String
End Module

]]>.Value

            Dim expectedOperationTree = <![CDATA[
IForToLoopOperation (LoopKind.ForTo, Continue Label Id: 0, Exit Label Id: 1) (OperationKind.Loop, Type: null, IsInvalid) (Syntax: 'For X = 10  ... Next')
  LoopControlVariable: 
    IFieldReferenceOperation: M.X As System.String (Static) (OperationKind.FieldReference, Type: System.String, IsInvalid) (Syntax: 'X')
      Instance Receiver: 
        null
  InitialValue: 
    ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 10) (Syntax: '10')
  LimitValue: 
    ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 0) (Syntax: '0')
  StepValue: 
    ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 1, IsInvalid, IsImplicit) (Syntax: 'For X = 10  ... Next')
  Body: 
    IBlockOperation (0 statements) (OperationKind.Block, Type: null, IsInvalid, IsImplicit) (Syntax: 'For X = 10  ... Next')
  NextVariables(0)
]]>.Value

            Dim expectedDiagnostics = <![CDATA[
BC30337: 'For' loop control variable cannot be of type 'String' because the type does not support the required operators.
        For X = 10 To 0'BIND:"For X = 10 To 0"
            ~
]]>.Value

            VerifyOperationTreeAndDiagnosticsForTest(Of ForBlockSyntax)(source, expectedOperationTree, expectedDiagnostics)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation)>
        <Fact(), WorkItem(17602, "https://github.com/dotnet/roslyn/issues/17602")>
        Public Sub IForLoopStatement_ForCombined()
            Dim source = <![CDATA[
Option Infer On

Module Program
    Sub Main(args As String())
        For A = 1 To 2'BIND:"For A = 1 To 2"
            For B = A To 2
        Next B, A
    End Sub
End Module]]>.Value

            Dim expectedOperationTree = <![CDATA[
IForToLoopOperation (LoopKind.ForTo, Continue Label Id: 0, Exit Label Id: 1) (OperationKind.Loop, Type: null) (Syntax: 'For A = 1 T ... Next B, A')
  Locals: Local_1: A As System.Int32
  LoopControlVariable: 
    IVariableDeclaratorOperation (Symbol: A As System.Int32) (OperationKind.VariableDeclarator, Type: null) (Syntax: 'A')
      Initializer: 
        null
  InitialValue: 
    ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 1) (Syntax: '1')
  LimitValue: 
    ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 2) (Syntax: '2')
  StepValue: 
    IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.Int32, Constant: 1, IsImplicit) (Syntax: 'For A = 1 T ... Next B, A')
      Conversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
      Operand: 
        ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 1, IsImplicit) (Syntax: 'For A = 1 T ... Next B, A')
  Body: 
    IBlockOperation (1 statements) (OperationKind.Block, Type: null, IsImplicit) (Syntax: 'For A = 1 T ... Next B, A')
      IForToLoopOperation (LoopKind.ForTo, Continue Label Id: 2, Exit Label Id: 3) (OperationKind.Loop, Type: null) (Syntax: 'For B = A T ... Next B, A')
        Locals: Local_1: B As System.Int32
        LoopControlVariable: 
          IVariableDeclaratorOperation (Symbol: B As System.Int32) (OperationKind.VariableDeclarator, Type: null) (Syntax: 'B')
            Initializer: 
              null
        InitialValue: 
          ILocalReferenceOperation: A (OperationKind.LocalReference, Type: System.Int32) (Syntax: 'A')
        LimitValue: 
          ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 2) (Syntax: '2')
        StepValue: 
          IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.Int32, Constant: 1, IsImplicit) (Syntax: 'For B = A T ... Next B, A')
            Conversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
            Operand: 
              ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 1, IsImplicit) (Syntax: 'For B = A T ... Next B, A')
        Body: 
          IBlockOperation (0 statements) (OperationKind.Block, Type: null, IsImplicit) (Syntax: 'For B = A T ... Next B, A')
        NextVariables(2):
            ILocalReferenceOperation: B (OperationKind.LocalReference, Type: System.Int32) (Syntax: 'B')
            ILocalReferenceOperation: A (OperationKind.LocalReference, Type: System.Int32) (Syntax: 'A')
  NextVariables(0)
]]>.Value

            Dim expectedDiagnostics = String.Empty

            VerifyOperationTreeAndDiagnosticsForTest(Of ForBlockSyntax)(source, expectedOperationTree, expectedDiagnostics)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation)>
        <Fact>
        Public Sub VerifyForToLoop1()
            Dim source = <![CDATA[
Structure C
    Sub F()
        Dim x As Integer
        Dim y As Integer = 16
        For x = 12 To y 'BIND:"For x = 12 To y"
        Next
    End Sub
End Structure
]]>.Value

            Dim expectedOperationTree = <![CDATA[
IForToLoopOperation (LoopKind.ForTo, Continue Label Id: 0, Exit Label Id: 1) (OperationKind.Loop, Type: null) (Syntax: 'For x = 12  ... Next')
  LoopControlVariable: 
    ILocalReferenceOperation: x (OperationKind.LocalReference, Type: System.Int32) (Syntax: 'x')
  InitialValue: 
    ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 12) (Syntax: '12')
  LimitValue: 
    ILocalReferenceOperation: y (OperationKind.LocalReference, Type: System.Int32) (Syntax: 'y')
  StepValue: 
    IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.Int32, Constant: 1, IsImplicit) (Syntax: 'For x = 12  ... Next')
      Conversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
      Operand: 
        ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 1, IsImplicit) (Syntax: 'For x = 12  ... Next')
  Body: 
    IBlockOperation (0 statements) (OperationKind.Block, Type: null, IsImplicit) (Syntax: 'For x = 12  ... Next')
  NextVariables(0)
]]>.Value

            VerifyOperationTreeForTest(Of ForBlockSyntax)(source, expectedOperationTree)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation)>
        <Fact>
        Public Sub VerifyForToLoop2()
            Dim source = <![CDATA[
Structure C
    Sub F()
        Dim x As Integer?
        Dim y As Integer = 16
        For x = 12 To y 'BIND:"For x = 12 To y"
        Next
    End Sub
End Structure
]]>.Value

            Dim expectedOperationTree = <![CDATA[
IForToLoopOperation (LoopKind.ForTo, Continue Label Id: 0, Exit Label Id: 1) (OperationKind.Loop, Type: null) (Syntax: 'For x = 12  ... Next')
  LoopControlVariable: 
    ILocalReferenceOperation: x (OperationKind.LocalReference, Type: System.Nullable(Of System.Int32)) (Syntax: 'x')
  InitialValue: 
    IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.Nullable(Of System.Int32), IsImplicit) (Syntax: '12')
      Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
      Operand: 
        ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 12) (Syntax: '12')
  LimitValue: 
    IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.Nullable(Of System.Int32), IsImplicit) (Syntax: 'y')
      Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
      Operand: 
        ILocalReferenceOperation: y (OperationKind.LocalReference, Type: System.Int32) (Syntax: 'y')
  StepValue: 
    IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.Nullable(Of System.Int32), IsImplicit) (Syntax: 'For x = 12  ... Next')
      Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
      Operand: 
        ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 1, IsImplicit) (Syntax: 'For x = 12  ... Next')
  Body: 
    IBlockOperation (0 statements) (OperationKind.Block, Type: null, IsImplicit) (Syntax: 'For x = 12  ... Next')
  NextVariables(0)
]]>.Value

            VerifyOperationTreeForTest(Of ForBlockSyntax)(source, expectedOperationTree)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation)>
        <Fact>
        Public Sub VerifyForToLoop3()
            Dim source = <![CDATA[
Structure C
    Sub F()
        Dim x As Integer
        Dim y As Integer? = 16
        For x = 12 To y 'BIND:"For x = 12 To y"
        Next
    End Sub
End Structure
]]>.Value

            Dim expectedOperationTree = <![CDATA[
IForToLoopOperation (LoopKind.ForTo, Continue Label Id: 0, Exit Label Id: 1) (OperationKind.Loop, Type: null) (Syntax: 'For x = 12  ... Next')
  LoopControlVariable: 
    ILocalReferenceOperation: x (OperationKind.LocalReference, Type: System.Int32) (Syntax: 'x')
  InitialValue: 
    ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 12) (Syntax: '12')
  LimitValue: 
    IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.Int32, IsImplicit) (Syntax: 'y')
      Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
      Operand: 
        ILocalReferenceOperation: y (OperationKind.LocalReference, Type: System.Nullable(Of System.Int32)) (Syntax: 'y')
  StepValue: 
    IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.Int32, Constant: 1, IsImplicit) (Syntax: 'For x = 12  ... Next')
      Conversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
      Operand: 
        ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 1, IsImplicit) (Syntax: 'For x = 12  ... Next')
  Body: 
    IBlockOperation (0 statements) (OperationKind.Block, Type: null, IsImplicit) (Syntax: 'For x = 12  ... Next')
  NextVariables(0)
]]>.Value

            VerifyOperationTreeForTest(Of ForBlockSyntax)(source, expectedOperationTree)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation)>
        <Fact>
        Public Sub VerifyForToLoop4()
            Dim source = <![CDATA[
Structure C
    Sub F()
        Dim x As Integer?
        Dim y As Integer? = 16
        For x = 12 To y 'BIND:"For x = 12 To y"
        Next
    End Sub
End Structure
]]>.Value

            Dim expectedOperationTree = <![CDATA[
IForToLoopOperation (LoopKind.ForTo, Continue Label Id: 0, Exit Label Id: 1) (OperationKind.Loop, Type: null) (Syntax: 'For x = 12  ... Next')
  LoopControlVariable: 
    ILocalReferenceOperation: x (OperationKind.LocalReference, Type: System.Nullable(Of System.Int32)) (Syntax: 'x')
  InitialValue: 
    IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.Nullable(Of System.Int32), IsImplicit) (Syntax: '12')
      Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
      Operand: 
        ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 12) (Syntax: '12')
  LimitValue: 
    ILocalReferenceOperation: y (OperationKind.LocalReference, Type: System.Nullable(Of System.Int32)) (Syntax: 'y')
  StepValue: 
    IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.Nullable(Of System.Int32), IsImplicit) (Syntax: 'For x = 12  ... Next')
      Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
      Operand: 
        ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 1, IsImplicit) (Syntax: 'For x = 12  ... Next')
  Body: 
    IBlockOperation (0 statements) (OperationKind.Block, Type: null, IsImplicit) (Syntax: 'For x = 12  ... Next')
  NextVariables(0)
]]>.Value

            VerifyOperationTreeForTest(Of ForBlockSyntax)(source, expectedOperationTree)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation)>
        <Fact>
        Public Sub VerifyForToLoop5()
            Dim source = <![CDATA[
Structure C
    Sub F()
        Dim x As Integer
        Dim y As Integer = 16
        Dim s As Integer? = nothing
        For x = 12 To y Step s 'BIND:"For x = 12 To y Step s"
        Next
    End Sub
End Structure
]]>.Value

            Dim expectedOperationTree = <![CDATA[
IForToLoopOperation (LoopKind.ForTo, Continue Label Id: 0, Exit Label Id: 1) (OperationKind.Loop, Type: null) (Syntax: 'For x = 12  ... Next')
  LoopControlVariable: 
    ILocalReferenceOperation: x (OperationKind.LocalReference, Type: System.Int32) (Syntax: 'x')
  InitialValue: 
    ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 12) (Syntax: '12')
  LimitValue: 
    ILocalReferenceOperation: y (OperationKind.LocalReference, Type: System.Int32) (Syntax: 'y')
  StepValue: 
    IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.Int32, IsImplicit) (Syntax: 's')
      Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
      Operand: 
        ILocalReferenceOperation: s (OperationKind.LocalReference, Type: System.Nullable(Of System.Int32)) (Syntax: 's')
  Body: 
    IBlockOperation (0 statements) (OperationKind.Block, Type: null, IsImplicit) (Syntax: 'For x = 12  ... Next')
  NextVariables(0)
]]>.Value

            VerifyOperationTreeForTest(Of ForBlockSyntax)(source, expectedOperationTree)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation)>
        <Fact>
        Public Sub VerifyForToLoop6()
            Dim source = <![CDATA[
Structure C
    Sub F()
        Dim x As Integer?
        Dim y As Integer = 16
        Dim s As Integer? = nothing
        For x = 12 To y Step s 'BIND:"For x = 12 To y Step s"
        Next
    End Sub
End Structure
]]>.Value

            Dim expectedOperationTree = <![CDATA[
IForToLoopOperation (LoopKind.ForTo, Continue Label Id: 0, Exit Label Id: 1) (OperationKind.Loop, Type: null) (Syntax: 'For x = 12  ... Next')
  LoopControlVariable: 
    ILocalReferenceOperation: x (OperationKind.LocalReference, Type: System.Nullable(Of System.Int32)) (Syntax: 'x')
  InitialValue: 
    IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.Nullable(Of System.Int32), IsImplicit) (Syntax: '12')
      Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
      Operand: 
        ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 12) (Syntax: '12')
  LimitValue: 
    IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.Nullable(Of System.Int32), IsImplicit) (Syntax: 'y')
      Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
      Operand: 
        ILocalReferenceOperation: y (OperationKind.LocalReference, Type: System.Int32) (Syntax: 'y')
  StepValue: 
    ILocalReferenceOperation: s (OperationKind.LocalReference, Type: System.Nullable(Of System.Int32)) (Syntax: 's')
  Body: 
    IBlockOperation (0 statements) (OperationKind.Block, Type: null, IsImplicit) (Syntax: 'For x = 12  ... Next')
  NextVariables(0)
]]>.Value

            VerifyOperationTreeForTest(Of ForBlockSyntax)(source, expectedOperationTree)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation)>
        <Fact>
        Public Sub VerifyForToLoop7()
            Dim source = <![CDATA[
Structure C
    Sub F()
        Dim x As Integer
        Dim y As Integer? = 16
        Dim s As Integer? = nothing
        For x = 12 To y Step s 'BIND:"For x = 12 To y Step s"
        Next
    End Sub
End Structure
]]>.Value

            Dim expectedOperationTree = <![CDATA[
IForToLoopOperation (LoopKind.ForTo, Continue Label Id: 0, Exit Label Id: 1) (OperationKind.Loop, Type: null) (Syntax: 'For x = 12  ... Next')
  LoopControlVariable: 
    ILocalReferenceOperation: x (OperationKind.LocalReference, Type: System.Int32) (Syntax: 'x')
  InitialValue: 
    ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 12) (Syntax: '12')
  LimitValue: 
    IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.Int32, IsImplicit) (Syntax: 'y')
      Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
      Operand: 
        ILocalReferenceOperation: y (OperationKind.LocalReference, Type: System.Nullable(Of System.Int32)) (Syntax: 'y')
  StepValue: 
    IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.Int32, IsImplicit) (Syntax: 's')
      Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
      Operand: 
        ILocalReferenceOperation: s (OperationKind.LocalReference, Type: System.Nullable(Of System.Int32)) (Syntax: 's')
  Body: 
    IBlockOperation (0 statements) (OperationKind.Block, Type: null, IsImplicit) (Syntax: 'For x = 12  ... Next')
  NextVariables(0)
]]>.Value

            VerifyOperationTreeForTest(Of ForBlockSyntax)(source, expectedOperationTree)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation)>
        <Fact>
        Public Sub VerifyForToLoop8()
            Dim source = <![CDATA[
Structure C
    Sub F()
        Dim x As Integer?
        Dim y As Integer? = 16
        Dim s As Integer? = nothing
        For x = 12 To y Step s 'BIND:"For x = 12 To y Step s"
        Next
    End Sub
End Structure
]]>.Value

            Dim expectedOperationTree = <![CDATA[
IForToLoopOperation (LoopKind.ForTo, Continue Label Id: 0, Exit Label Id: 1) (OperationKind.Loop, Type: null) (Syntax: 'For x = 12  ... Next')
  LoopControlVariable: 
    ILocalReferenceOperation: x (OperationKind.LocalReference, Type: System.Nullable(Of System.Int32)) (Syntax: 'x')
  InitialValue: 
    IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.Nullable(Of System.Int32), IsImplicit) (Syntax: '12')
      Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
      Operand: 
        ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 12) (Syntax: '12')
  LimitValue: 
    ILocalReferenceOperation: y (OperationKind.LocalReference, Type: System.Nullable(Of System.Int32)) (Syntax: 'y')
  StepValue: 
    ILocalReferenceOperation: s (OperationKind.LocalReference, Type: System.Nullable(Of System.Int32)) (Syntax: 's')
  Body: 
    IBlockOperation (0 statements) (OperationKind.Block, Type: null, IsImplicit) (Syntax: 'For x = 12  ... Next')
  NextVariables(0)
]]>.Value

            VerifyOperationTreeForTest(Of ForBlockSyntax)(source, expectedOperationTree)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation)>
        <Fact>
        Public Sub VerifyForToLoop_FieldAsIterationVariable()
            Dim source = <![CDATA[
Imports System
Imports System.Collections.Generic
Imports System.Linq

Class C
    Private X As Integer = 0
    Sub M()
        For X = 0 To 10'BIND:"For X = 0 To 10"
        Next X
    End Sub
End Class]]>.Value

            Dim expectedOperationTree = <![CDATA[
IForToLoopOperation (LoopKind.ForTo, Continue Label Id: 0, Exit Label Id: 1) (OperationKind.Loop, Type: null) (Syntax: 'For X = 0 T ... Next X')
  LoopControlVariable: 
    IFieldReferenceOperation: C.X As System.Int32 (OperationKind.FieldReference, Type: System.Int32) (Syntax: 'X')
      Instance Receiver: 
        IInstanceReferenceOperation (OperationKind.InstanceReference, Type: C, IsImplicit) (Syntax: 'X')
  InitialValue: 
    ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 0) (Syntax: '0')
  LimitValue: 
    ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 10) (Syntax: '10')
  StepValue: 
    IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.Int32, Constant: 1, IsImplicit) (Syntax: 'For X = 0 T ... Next X')
      Conversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
      Operand: 
        ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 1, IsImplicit) (Syntax: 'For X = 0 T ... Next X')
  Body: 
    IBlockOperation (0 statements) (OperationKind.Block, Type: null, IsImplicit) (Syntax: 'For X = 0 T ... Next X')
  NextVariables(1):
      IFieldReferenceOperation: C.X As System.Int32 (OperationKind.FieldReference, Type: System.Int32) (Syntax: 'X')
        Instance Receiver: 
          IInstanceReferenceOperation (OperationKind.InstanceReference, Type: C, IsImplicit) (Syntax: 'X')
]]>.Value

            Dim expectedDiagnostics = String.Empty

            VerifyOperationTreeAndDiagnosticsForTest(Of ForBlockSyntax)(source, expectedOperationTree, expectedDiagnostics)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation)>
        <Fact>
        Public Sub VerifyForToLoop_FieldWithExplicitReceiverAsIterationVariable()
            Dim source = <![CDATA[
Imports System
Imports System.Collections.Generic
Imports System.Linq

Class C
    Private X As Integer = 0
    Sub M(c As C)
        For c.X = 0 To 10'BIND:"For c.X = 0 To 10"
        Next c.X
    End Sub
End Class]]>.Value

            Dim expectedOperationTree = <![CDATA[
IForToLoopOperation (LoopKind.ForTo, Continue Label Id: 0, Exit Label Id: 1) (OperationKind.Loop, Type: null) (Syntax: 'For c.X = 0 ... Next c.X')
  LoopControlVariable: 
    IFieldReferenceOperation: C.X As System.Int32 (OperationKind.FieldReference, Type: System.Int32) (Syntax: 'c.X')
      Instance Receiver: 
        IParameterReferenceOperation: c (OperationKind.ParameterReference, Type: C) (Syntax: 'c')
  InitialValue: 
    ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 0) (Syntax: '0')
  LimitValue: 
    ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 10) (Syntax: '10')
  StepValue: 
    IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.Int32, Constant: 1, IsImplicit) (Syntax: 'For c.X = 0 ... Next c.X')
      Conversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
      Operand: 
        ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 1, IsImplicit) (Syntax: 'For c.X = 0 ... Next c.X')
  Body: 
    IBlockOperation (0 statements) (OperationKind.Block, Type: null, IsImplicit) (Syntax: 'For c.X = 0 ... Next c.X')
  NextVariables(1):
      IFieldReferenceOperation: C.X As System.Int32 (OperationKind.FieldReference, Type: System.Int32) (Syntax: 'c.X')
        Instance Receiver: 
          IParameterReferenceOperation: c (OperationKind.ParameterReference, Type: C) (Syntax: 'c')
]]>.Value

            Dim expectedDiagnostics = String.Empty

            VerifyOperationTreeAndDiagnosticsForTest(Of ForBlockSyntax)(source, expectedOperationTree, expectedDiagnostics)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation)>
        <Fact>
        Public Sub VerifyForToLoop_InvalidLoopControlVariableDeclaration()
            Dim source = <![CDATA[
Imports System
Imports System.Collections.Generic
Imports System.Linq

Class C
    Sub M()
        Dim i as Integer = 0
        For i as Integer = 0 To 10'BIND:"For i as Integer = 0 To 10"
        Next i
    End Sub
End Class]]>.Value

            Dim expectedOperationTree = <![CDATA[
IForToLoopOperation (LoopKind.ForTo, Continue Label Id: 0, Exit Label Id: 1) (OperationKind.Loop, Type: null, IsInvalid) (Syntax: 'For i as In ... Next i')
  Locals: Local_1: i As System.Int32
  LoopControlVariable: 
    IVariableDeclaratorOperation (Symbol: i As System.Int32) (OperationKind.VariableDeclarator, Type: null, IsInvalid) (Syntax: 'i as Integer')
      Initializer: 
        null
  InitialValue: 
    ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 0) (Syntax: '0')
  LimitValue: 
    ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 10) (Syntax: '10')
  StepValue: 
    IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.Int32, Constant: 1, IsInvalid, IsImplicit) (Syntax: 'For i as In ... Next i')
      Conversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
      Operand: 
        ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 1, IsInvalid, IsImplicit) (Syntax: 'For i as In ... Next i')
  Body: 
    IBlockOperation (0 statements) (OperationKind.Block, Type: null, IsInvalid, IsImplicit) (Syntax: 'For i as In ... Next i')
  NextVariables(1):
      ILocalReferenceOperation: i (OperationKind.LocalReference, Type: System.Int32) (Syntax: 'i')
]]>.Value

            Dim expectedDiagnostics = <![CDATA[
BC30616: Variable 'i' hides a variable in an enclosing block.
        For i as Integer = 0 To 10'BIND:"For i as Integer = 0 To 10"
            ~
]]>.Value

            VerifyOperationTreeAndDiagnosticsForTest(Of ForBlockSyntax)(source, expectedOperationTree, expectedDiagnostics)
        End Sub
    End Class
End Namespace
