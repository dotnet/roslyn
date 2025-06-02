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
IForToLoopOperation (LoopKind.ForTo, Continue Label Id: 0, Exit Label Id: 1, Checked) (OperationKind.Loop, Type: null) (Syntax: 'For i As In ... Next')
  Locals: Local_1: i As System.Int32
  LoopControlVariable: 
    IVariableDeclaratorOperation (Symbol: i As System.Int32) (OperationKind.VariableDeclarator, Type: null) (Syntax: 'i As Integer')
      Initializer: 
        null
  InitialValue: 
    ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 0) (Syntax: '0')
  LimitValue: 
    IBinaryOperation (BinaryOperatorKind.Subtract, Checked) (OperationKind.Binary, Type: System.Int32) (Syntax: 'myarray.Length - 1')
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

            VerifyOperationTreeForTest(Of ForBlockSyntax)(source, expectedOperationTree, TestOptions.ReleaseDll.WithOverflowChecks(False))
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
IForToLoopOperation (LoopKind.ForTo, Continue Label Id: 0, Exit Label Id: 1, Checked) (OperationKind.Loop, Type: null) (Syntax: 'For i As Do ... Next')
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
IForToLoopOperation (LoopKind.ForTo, Continue Label Id: 0, Exit Label Id: 1, Checked) (OperationKind.Loop, Type: null) (Syntax: 'For ctrlVar ... Next')
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
IForToLoopOperation (LoopKind.ForTo, Continue Label Id: 0, Exit Label Id: 1, Checked) (OperationKind.Loop, Type: null) (Syntax: 'For AVarNam ... xt AVarName')
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
      IForToLoopOperation (LoopKind.ForTo, Continue Label Id: 2, Exit Label Id: 3, Checked) (OperationKind.Loop, Type: null) (Syntax: 'For B = 1 T ... Next B')
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
            IForToLoopOperation (LoopKind.ForTo, Continue Label Id: 4, Exit Label Id: 5, Checked) (OperationKind.Loop, Type: null) (Syntax: 'For C = 1 T ... Next C')
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
                  IForToLoopOperation (LoopKind.ForTo, Continue Label Id: 6, Exit Label Id: 7, Checked) (OperationKind.Loop, Type: null) (Syntax: 'For D = 1 T ... Next D')
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
IForToLoopOperation (LoopKind.ForTo, Continue Label Id: 0, Exit Label Id: 1, Checked) (OperationKind.Loop, Type: null) (Syntax: 'For I = 1 T ... Next')
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
      IForToLoopOperation (LoopKind.ForTo, Continue Label Id: 2, Exit Label Id: 3, Checked) (OperationKind.Loop, Type: null) (Syntax: 'For J = 1 T ... Next')
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
IForToLoopOperation (LoopKind.ForTo, Continue Label Id: 0, Exit Label Id: 1, Checked) (OperationKind.Loop, Type: null) (Syntax: 'For I = 1 T ... Next')
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
      IForToLoopOperation (LoopKind.ForTo, Continue Label Id: 2, Exit Label Id: 3, Checked) (OperationKind.Loop, Type: null) (Syntax: 'For J = I + ... Next')
        Locals: Local_1: J As System.Int32
        LoopControlVariable: 
          IVariableDeclaratorOperation (Symbol: J As System.Int32) (OperationKind.VariableDeclarator, Type: null) (Syntax: 'J')
            Initializer: 
              null
        InitialValue: 
          IBinaryOperation (BinaryOperatorKind.Add, Checked) (OperationKind.Binary, Type: System.Int32) (Syntax: 'I + 1')
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
IForToLoopOperation (LoopKind.ForTo, Continue Label Id: 0, Exit Label Id: 1, Checked) (OperationKind.Loop, Type: null) (Syntax: 'For I = 1 T ... Next')
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
      IForToLoopOperation (LoopKind.ForTo, Continue Label Id: 2, Exit Label Id: 3, Checked) (OperationKind.Loop, Type: null) (Syntax: 'For J = 1 T ... Next')
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
IForToLoopOperation (LoopKind.ForTo, Continue Label Id: 0, Exit Label Id: 1, Checked) (OperationKind.Loop, Type: null) (Syntax: 'For x As e1 ... Next')
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
IForToLoopOperation (LoopKind.ForTo, Continue Label Id: 0, Exit Label Id: 1, Checked) (OperationKind.Loop, Type: null) (Syntax: 'For i As In ... Next')
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
            IInstanceReferenceOperation (ReferenceKind: ContainingTypeInstance) (OperationKind.InstanceReference, Type: MyClass1, IsImplicit) (Syntax: 'P1')
          Arguments(1):
              IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: x) (OperationKind.Argument, Type: null) (Syntax: '30 + i')
                IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.Int64, IsImplicit) (Syntax: '30 + i')
                  Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: True, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                  Operand: 
                    IBinaryOperation (BinaryOperatorKind.Add, Checked) (OperationKind.Binary, Type: System.Int32) (Syntax: '30 + i')
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
IForToLoopOperation (LoopKind.ForTo, Continue Label Id: 0, Exit Label Id: 1, Checked) (OperationKind.Loop, Type: null) (Syntax: 'For global_ ... Next')
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
IForToLoopOperation (LoopKind.ForTo, Continue Label Id: 0, Exit Label Id: 1, Checked) (OperationKind.Loop, Type: null) (Syntax: 'For x As In ... o 10 : Next')
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
IForToLoopOperation (LoopKind.ForTo, Continue Label Id: 0, Exit Label Id: 1, Checked) (OperationKind.Loop, Type: null) (Syntax: 'For Y = 1 T ... Next')
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
IForToLoopOperation (LoopKind.ForTo, Continue Label Id: 0, Exit Label Id: 1, Checked) (OperationKind.Loop, Type: null) (Syntax: 'For element ... Next')
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
IForToLoopOperation (LoopKind.ForTo, Continue Label Id: 0, Exit Label Id: 1, Checked) (OperationKind.Loop, Type: null) (Syntax: 'For i As In ... Next')
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
          IBinaryOperation (BinaryOperatorKind.Equals, Checked) (OperationKind.Binary, Type: System.Boolean) (Syntax: 'i Mod 2 = 0')
            Left: 
              IBinaryOperation (BinaryOperatorKind.Remainder, Checked) (OperationKind.Binary, Type: System.Int32) (Syntax: 'i Mod 2')
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
IForToLoopOperation (LoopKind.ForTo, Continue Label Id: 0, Exit Label Id: 1, Checked) (OperationKind.Loop, Type: null) (Syntax: 'For X = 10  ... Next')
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
IForToLoopOperation (LoopKind.ForTo, Continue Label Id: 0, Exit Label Id: 1, Checked) (OperationKind.Loop, Type: null, IsInvalid) (Syntax: 'For X = 10  ... Next')
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
IForToLoopOperation (LoopKind.ForTo, Continue Label Id: 0, Exit Label Id: 1, Checked) (OperationKind.Loop, Type: null) (Syntax: 'For A = 1 T ... Next B, A')
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
      IForToLoopOperation (LoopKind.ForTo, Continue Label Id: 2, Exit Label Id: 3, Checked) (OperationKind.Loop, Type: null) (Syntax: 'For B = A T ... Next B, A')
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
IForToLoopOperation (LoopKind.ForTo, Continue Label Id: 0, Exit Label Id: 1, Checked) (OperationKind.Loop, Type: null) (Syntax: 'For x = 12  ... Next')
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
IForToLoopOperation (LoopKind.ForTo, Continue Label Id: 0, Exit Label Id: 1, Checked) (OperationKind.Loop, Type: null) (Syntax: 'For x = 12  ... Next')
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
IForToLoopOperation (LoopKind.ForTo, Continue Label Id: 0, Exit Label Id: 1, Checked) (OperationKind.Loop, Type: null) (Syntax: 'For x = 12  ... Next')
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
IForToLoopOperation (LoopKind.ForTo, Continue Label Id: 0, Exit Label Id: 1, Checked) (OperationKind.Loop, Type: null) (Syntax: 'For x = 12  ... Next')
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
IForToLoopOperation (LoopKind.ForTo, Continue Label Id: 0, Exit Label Id: 1, Checked) (OperationKind.Loop, Type: null) (Syntax: 'For x = 12  ... Next')
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
IForToLoopOperation (LoopKind.ForTo, Continue Label Id: 0, Exit Label Id: 1, Checked) (OperationKind.Loop, Type: null) (Syntax: 'For x = 12  ... Next')
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
IForToLoopOperation (LoopKind.ForTo, Continue Label Id: 0, Exit Label Id: 1, Checked) (OperationKind.Loop, Type: null) (Syntax: 'For x = 12  ... Next')
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
IForToLoopOperation (LoopKind.ForTo, Continue Label Id: 0, Exit Label Id: 1, Checked) (OperationKind.Loop, Type: null) (Syntax: 'For x = 12  ... Next')
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
IForToLoopOperation (LoopKind.ForTo, Continue Label Id: 0, Exit Label Id: 1, Checked) (OperationKind.Loop, Type: null) (Syntax: 'For X = 0 T ... Next X')
  LoopControlVariable: 
    IFieldReferenceOperation: C.X As System.Int32 (OperationKind.FieldReference, Type: System.Int32) (Syntax: 'X')
      Instance Receiver: 
        IInstanceReferenceOperation (ReferenceKind: ContainingTypeInstance) (OperationKind.InstanceReference, Type: C, IsImplicit) (Syntax: 'X')
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
          IInstanceReferenceOperation (ReferenceKind: ContainingTypeInstance) (OperationKind.InstanceReference, Type: C, IsImplicit) (Syntax: 'X')
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
IForToLoopOperation (LoopKind.ForTo, Continue Label Id: 0, Exit Label Id: 1, Checked) (OperationKind.Loop, Type: null) (Syntax: 'For c.X = 0 ... Next c.X')
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
IForToLoopOperation (LoopKind.ForTo, Continue Label Id: 0, Exit Label Id: 1, Checked) (OperationKind.Loop, Type: null, IsInvalid) (Syntax: 'For i as In ... Next i')
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

        <CompilerTrait(CompilerFeature.IOperation, CompilerFeature.Dataflow)>
        <Fact()>
        Public Sub ForToFlow_01()
            Dim source = <![CDATA[
Imports System
Public Class C
    Sub M(result As Integer) 'BIND:"Sub M"
        For i As UInteger = 0UI To 2UI
            result = if(i > 0UI, 3, 4) 
        Next
    End Sub
End Class]]>.Value

            Dim expectedDiagnostics = String.Empty

            Dim expectedFlowGraph = <![CDATA[
Block[B0] - Entry
    Statements (0)
    Next (Regular) Block[B1]
        Entering: {R1} {R2}

.locals {R1}
{
    Locals: [i As System.UInt32]
    CaptureIds: [2] [3]
    .locals {R2}
    {
        CaptureIds: [0] [1]
        Block[B1] - Block
            Predecessors: [B0]
            Statements (5)
                IFlowCaptureOperation: 0 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'i As UInteger')
                  Value: 
                    ILocalReferenceOperation: i (IsDeclaration: True) (OperationKind.LocalReference, Type: System.UInt32, IsImplicit) (Syntax: 'i As UInteger')

                IFlowCaptureOperation: 1 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: '0UI')
                  Value: 
                    ILiteralOperation (OperationKind.Literal, Type: System.UInt32, Constant: 0) (Syntax: '0UI')

                IFlowCaptureOperation: 2 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: '2UI')
                  Value: 
                    ILiteralOperation (OperationKind.Literal, Type: System.UInt32, Constant: 2) (Syntax: '2UI')

                IFlowCaptureOperation: 3 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'For i As UI ... Next')
                  Value: 
                    IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.UInt32, Constant: 1, IsImplicit) (Syntax: 'For i As UI ... Next')
                      Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: True, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                        (WideningNumeric, InvolvesNarrowingFromNumericConstant)
                      Operand: 
                        ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 1, IsImplicit) (Syntax: 'For i As UI ... Next')

                ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: null, IsImplicit) (Syntax: '0UI')
                  Left: 
                    IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: System.UInt32, IsImplicit) (Syntax: 'i As UInteger')
                  Right: 
                    IFlowCaptureReferenceOperation: 1 (OperationKind.FlowCaptureReference, Type: System.UInt32, Constant: 0, IsImplicit) (Syntax: '0UI')

            Next (Regular) Block[B2]
                Leaving: {R2}
    }

    Block[B2] - Block
        Predecessors: [B1] [B7]
        Statements (0)
        Jump if False (Regular) to Block[B8]
            IBinaryOperation (BinaryOperatorKind.LessThanOrEqual) (OperationKind.Binary, Type: System.Boolean, IsImplicit) (Syntax: '2UI')
              Left: 
                ILocalReferenceOperation: i (IsDeclaration: True) (OperationKind.LocalReference, Type: System.UInt32, IsImplicit) (Syntax: 'i As UInteger')
              Right: 
                IFlowCaptureReferenceOperation: 2 (OperationKind.FlowCaptureReference, Type: System.UInt32, Constant: 2, IsImplicit) (Syntax: '2UI')
            Leaving: {R1}

        Next (Regular) Block[B3]
            Entering: {R3}

    .locals {R3}
    {
        CaptureIds: [4] [5]
        Block[B3] - Block
            Predecessors: [B2]
            Statements (1)
                IFlowCaptureOperation: 4 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'result')
                  Value: 
                    IParameterReferenceOperation: result (OperationKind.ParameterReference, Type: System.Int32) (Syntax: 'result')

            Jump if False (Regular) to Block[B5]
                IBinaryOperation (BinaryOperatorKind.GreaterThan, Checked) (OperationKind.Binary, Type: System.Boolean) (Syntax: 'i > 0UI')
                  Left: 
                    ILocalReferenceOperation: i (OperationKind.LocalReference, Type: System.UInt32) (Syntax: 'i')
                  Right: 
                    ILiteralOperation (OperationKind.Literal, Type: System.UInt32, Constant: 0) (Syntax: '0UI')

            Next (Regular) Block[B4]
        Block[B4] - Block
            Predecessors: [B3]
            Statements (1)
                IFlowCaptureOperation: 5 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: '3')
                  Value: 
                    ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 3) (Syntax: '3')

            Next (Regular) Block[B6]
        Block[B5] - Block
            Predecessors: [B3]
            Statements (1)
                IFlowCaptureOperation: 5 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: '4')
                  Value: 
                    ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 4) (Syntax: '4')

            Next (Regular) Block[B6]
        Block[B6] - Block
            Predecessors: [B4] [B5]
            Statements (1)
                IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'result = if ...  0UI, 3, 4)')
                  Expression: 
                    ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Int32, IsImplicit) (Syntax: 'result = if ...  0UI, 3, 4)')
                      Left: 
                        IFlowCaptureReferenceOperation: 4 (OperationKind.FlowCaptureReference, Type: System.Int32, IsImplicit) (Syntax: 'result')
                      Right: 
                        IFlowCaptureReferenceOperation: 5 (OperationKind.FlowCaptureReference, Type: System.Int32, IsImplicit) (Syntax: 'if(i > 0UI, 3, 4)')

            Next (Regular) Block[B7]
                Leaving: {R3}
    }

    Block[B7] - Block
        Predecessors: [B6]
        Statements (1)
            ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: null, IsImplicit) (Syntax: 'i As UInteger')
              Left: 
                ILocalReferenceOperation: i (IsDeclaration: True) (OperationKind.LocalReference, Type: System.UInt32, IsImplicit) (Syntax: 'i As UInteger')
              Right: 
                IBinaryOperation (BinaryOperatorKind.Add, Checked) (OperationKind.Binary, Type: System.UInt32, IsImplicit) (Syntax: 'For i As UI ... Next')
                  Left: 
                    ILocalReferenceOperation: i (IsDeclaration: True) (OperationKind.LocalReference, Type: System.UInt32, IsImplicit) (Syntax: 'i As UInteger')
                  Right: 
                    IFlowCaptureReferenceOperation: 3 (OperationKind.FlowCaptureReference, Type: System.UInt32, Constant: 1, IsImplicit) (Syntax: 'For i As UI ... Next')

        Next (Regular) Block[B2]
}

Block[B8] - Exit
    Predecessors: [B2]
    Statements (0)
]]>.Value

            VerifyFlowGraphAndDiagnosticsForTest(Of MethodBlockSyntax)(source, expectedFlowGraph, expectedDiagnostics)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation, CompilerFeature.Dataflow)>
        <Fact()>
        Public Sub ForToFlow_02()
            Dim source = <![CDATA[
Imports System
Public Class C
    Sub M(i As Integer, result As Integer) 'BIND:"Sub M"
        For i = 0 To 4 Step 2
            result = i
        Next
    End Sub
End Class]]>.Value

            Dim expectedDiagnostics = String.Empty

            Dim expectedFlowGraph = <![CDATA[
Block[B0] - Entry
    Statements (0)
    Next (Regular) Block[B1]
        Entering: {R1} {R2}

.locals {R1}
{
    CaptureIds: [2] [3]
    .locals {R2}
    {
        CaptureIds: [0] [1]
        Block[B1] - Block
            Predecessors: [B0]
            Statements (5)
                IFlowCaptureOperation: 0 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'i')
                  Value: 
                    IParameterReferenceOperation: i (OperationKind.ParameterReference, Type: System.Int32) (Syntax: 'i')

                IFlowCaptureOperation: 1 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: '0')
                  Value: 
                    ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 0) (Syntax: '0')

                IFlowCaptureOperation: 2 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: '4')
                  Value: 
                    ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 4) (Syntax: '4')

                IFlowCaptureOperation: 3 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: '2')
                  Value: 
                    ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 2) (Syntax: '2')

                ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: null, IsImplicit) (Syntax: '0')
                  Left: 
                    IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: System.Int32, IsImplicit) (Syntax: 'i')
                  Right: 
                    IFlowCaptureReferenceOperation: 1 (OperationKind.FlowCaptureReference, Type: System.Int32, Constant: 0, IsImplicit) (Syntax: '0')

            Next (Regular) Block[B2]
                Leaving: {R2}
    }

    Block[B2] - Block
        Predecessors: [B1] [B3]
        Statements (0)
        Jump if False (Regular) to Block[B4]
            IBinaryOperation (BinaryOperatorKind.LessThanOrEqual) (OperationKind.Binary, Type: System.Boolean, IsImplicit) (Syntax: '4')
              Left: 
                IParameterReferenceOperation: i (OperationKind.ParameterReference, Type: System.Int32, IsImplicit) (Syntax: 'i')
              Right: 
                IFlowCaptureReferenceOperation: 2 (OperationKind.FlowCaptureReference, Type: System.Int32, Constant: 4, IsImplicit) (Syntax: '4')
            Leaving: {R1}

        Next (Regular) Block[B3]
    Block[B3] - Block
        Predecessors: [B2]
        Statements (2)
            IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'result = i')
              Expression: 
                ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Int32, IsImplicit) (Syntax: 'result = i')
                  Left: 
                    IParameterReferenceOperation: result (OperationKind.ParameterReference, Type: System.Int32) (Syntax: 'result')
                  Right: 
                    IParameterReferenceOperation: i (OperationKind.ParameterReference, Type: System.Int32) (Syntax: 'i')

            ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: null, IsImplicit) (Syntax: 'i')
              Left: 
                IParameterReferenceOperation: i (OperationKind.ParameterReference, Type: System.Int32, IsImplicit) (Syntax: 'i')
              Right: 
                IBinaryOperation (BinaryOperatorKind.Add) (OperationKind.Binary, Type: System.Int32, IsImplicit) (Syntax: '2')
                  Left: 
                    IParameterReferenceOperation: i (OperationKind.ParameterReference, Type: System.Int32, IsImplicit) (Syntax: 'i')
                  Right: 
                    IFlowCaptureReferenceOperation: 3 (OperationKind.FlowCaptureReference, Type: System.Int32, Constant: 2, IsImplicit) (Syntax: '2')

        Next (Regular) Block[B2]
}

Block[B4] - Exit
    Predecessors: [B2]
    Statements (0)
]]>.Value

            VerifyFlowGraphAndDiagnosticsForTest(Of MethodBlockSyntax)(source, expectedFlowGraph, expectedDiagnostics, TestOptions.ReleaseDll.WithOverflowChecks(False))
        End Sub

        <CompilerTrait(CompilerFeature.IOperation, CompilerFeature.Dataflow)>
        <Fact()>
        Public Sub ForToFlow_03()
            Dim source = <![CDATA[
Imports System
Public Class C
    Sub M(i As Decimal, result As Decimal) 'BIND:"Sub M"
        For i = 3D To 0D Step -1D
            result = i
        Next
    End Sub
End Class]]>.Value

            Dim expectedDiagnostics = String.Empty

            Dim expectedFlowGraph = <![CDATA[
Block[B0] - Entry
    Statements (0)
    Next (Regular) Block[B1]
        Entering: {R1} {R2}

.locals {R1}
{
    CaptureIds: [2] [3]
    .locals {R2}
    {
        CaptureIds: [0] [1]
        Block[B1] - Block
            Predecessors: [B0]
            Statements (5)
                IFlowCaptureOperation: 0 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'i')
                  Value: 
                    IParameterReferenceOperation: i (OperationKind.ParameterReference, Type: System.Decimal) (Syntax: 'i')

                IFlowCaptureOperation: 1 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: '3D')
                  Value: 
                    ILiteralOperation (OperationKind.Literal, Type: System.Decimal, Constant: 3) (Syntax: '3D')

                IFlowCaptureOperation: 2 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: '0D')
                  Value: 
                    ILiteralOperation (OperationKind.Literal, Type: System.Decimal, Constant: 0) (Syntax: '0D')

                IFlowCaptureOperation: 3 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: '-1D')
                  Value: 
                    IUnaryOperation (UnaryOperatorKind.Minus, Checked) (OperationKind.Unary, Type: System.Decimal, Constant: -1) (Syntax: '-1D')
                      Operand: 
                        ILiteralOperation (OperationKind.Literal, Type: System.Decimal, Constant: 1) (Syntax: '1D')

                ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: null, IsImplicit) (Syntax: '3D')
                  Left: 
                    IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: System.Decimal, IsImplicit) (Syntax: 'i')
                  Right: 
                    IFlowCaptureReferenceOperation: 1 (OperationKind.FlowCaptureReference, Type: System.Decimal, Constant: 3, IsImplicit) (Syntax: '3D')

            Next (Regular) Block[B2]
                Leaving: {R2}
    }

    Block[B2] - Block
        Predecessors: [B1] [B3]
        Statements (0)
        Jump if False (Regular) to Block[B4]
            IBinaryOperation (BinaryOperatorKind.GreaterThanOrEqual) (OperationKind.Binary, Type: System.Boolean, IsImplicit) (Syntax: '0D')
              Left: 
                IParameterReferenceOperation: i (OperationKind.ParameterReference, Type: System.Decimal, IsImplicit) (Syntax: 'i')
              Right: 
                IFlowCaptureReferenceOperation: 2 (OperationKind.FlowCaptureReference, Type: System.Decimal, Constant: 0, IsImplicit) (Syntax: '0D')
            Leaving: {R1}

        Next (Regular) Block[B3]
    Block[B3] - Block
        Predecessors: [B2]
        Statements (2)
            IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'result = i')
              Expression: 
                ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Decimal, IsImplicit) (Syntax: 'result = i')
                  Left: 
                    IParameterReferenceOperation: result (OperationKind.ParameterReference, Type: System.Decimal) (Syntax: 'result')
                  Right: 
                    IParameterReferenceOperation: i (OperationKind.ParameterReference, Type: System.Decimal) (Syntax: 'i')

            ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: null, IsImplicit) (Syntax: 'i')
              Left: 
                IParameterReferenceOperation: i (OperationKind.ParameterReference, Type: System.Decimal, IsImplicit) (Syntax: 'i')
              Right: 
                IBinaryOperation (BinaryOperatorKind.Add, Checked) (OperationKind.Binary, Type: System.Decimal, IsImplicit) (Syntax: '-1D')
                  Left: 
                    IParameterReferenceOperation: i (OperationKind.ParameterReference, Type: System.Decimal, IsImplicit) (Syntax: 'i')
                  Right: 
                    IFlowCaptureReferenceOperation: 3 (OperationKind.FlowCaptureReference, Type: System.Decimal, Constant: -1, IsImplicit) (Syntax: '-1D')

        Next (Regular) Block[B2]
}

Block[B4] - Exit
    Predecessors: [B2]
    Statements (0)
]]>.Value

            VerifyFlowGraphAndDiagnosticsForTest(Of MethodBlockSyntax)(source, expectedFlowGraph, expectedDiagnostics)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation, CompilerFeature.Dataflow)>
        <Fact()>
        Public Sub ForToFlow_04()
            Dim source = <![CDATA[
Imports System
Public Class C
    Sub M(i As Short, [step] As Short, result As Short) 'BIND:"Sub M"
        For i = 0S To 4S Step [step]
            result = i
        Next
    End Sub
End Class]]>.Value

            Dim expectedDiagnostics = String.Empty

            Dim expectedFlowGraph = <![CDATA[
Block[B0] - Entry
    Statements (0)
    Next (Regular) Block[B1]
        Entering: {R1} {R2}

.locals {R1}
{
    CaptureIds: [2] [3]
    .locals {R2}
    {
        CaptureIds: [0] [1]
        Block[B1] - Block
            Predecessors: [B0]
            Statements (5)
                IFlowCaptureOperation: 0 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'i')
                  Value: 
                    IParameterReferenceOperation: i (OperationKind.ParameterReference, Type: System.Int16) (Syntax: 'i')

                IFlowCaptureOperation: 1 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: '0S')
                  Value: 
                    ILiteralOperation (OperationKind.Literal, Type: System.Int16, Constant: 0) (Syntax: '0S')

                IFlowCaptureOperation: 2 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: '4S')
                  Value: 
                    ILiteralOperation (OperationKind.Literal, Type: System.Int16, Constant: 4) (Syntax: '4S')

                IFlowCaptureOperation: 3 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: '[step]')
                  Value: 
                    IParameterReferenceOperation: step (OperationKind.ParameterReference, Type: System.Int16) (Syntax: '[step]')

                ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: null, IsImplicit) (Syntax: '0S')
                  Left: 
                    IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: System.Int16, IsImplicit) (Syntax: 'i')
                  Right: 
                    IFlowCaptureReferenceOperation: 1 (OperationKind.FlowCaptureReference, Type: System.Int16, Constant: 0, IsImplicit) (Syntax: '0S')

            Next (Regular) Block[B2]
                Leaving: {R2}
    }

    Block[B2] - Block
        Predecessors: [B1] [B3]
        Statements (0)
        Jump if False (Regular) to Block[B4]
            IBinaryOperation (BinaryOperatorKind.LessThanOrEqual) (OperationKind.Binary, Type: System.Boolean, IsImplicit) (Syntax: '4S')
              Left: 
                IBinaryOperation (BinaryOperatorKind.ExclusiveOr) (OperationKind.Binary, Type: System.Int16, IsImplicit) (Syntax: 'i')
                  Left: 
                    IBinaryOperation (BinaryOperatorKind.RightShift) (OperationKind.Binary, Type: System.Int16, IsImplicit) (Syntax: 'i')
                      Left: 
                        IFlowCaptureReferenceOperation: 3 (OperationKind.FlowCaptureReference, Type: System.Int16, IsImplicit) (Syntax: '[step]')
                      Right: 
                        ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 15, IsImplicit) (Syntax: 'i')
                  Right: 
                    IParameterReferenceOperation: i (OperationKind.ParameterReference, Type: System.Int16, IsImplicit) (Syntax: 'i')
              Right: 
                IBinaryOperation (BinaryOperatorKind.ExclusiveOr) (OperationKind.Binary, Type: System.Int16, IsImplicit) (Syntax: '4S')
                  Left: 
                    IBinaryOperation (BinaryOperatorKind.RightShift) (OperationKind.Binary, Type: System.Int16, IsImplicit) (Syntax: '4S')
                      Left: 
                        IFlowCaptureReferenceOperation: 3 (OperationKind.FlowCaptureReference, Type: System.Int16, IsImplicit) (Syntax: '[step]')
                      Right: 
                        ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 15, IsImplicit) (Syntax: '4S')
                  Right: 
                    IFlowCaptureReferenceOperation: 2 (OperationKind.FlowCaptureReference, Type: System.Int16, Constant: 4, IsImplicit) (Syntax: '4S')
            Leaving: {R1}

        Next (Regular) Block[B3]
    Block[B3] - Block
        Predecessors: [B2]
        Statements (2)
            IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'result = i')
              Expression: 
                ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Int16, IsImplicit) (Syntax: 'result = i')
                  Left: 
                    IParameterReferenceOperation: result (OperationKind.ParameterReference, Type: System.Int16) (Syntax: 'result')
                  Right: 
                    IParameterReferenceOperation: i (OperationKind.ParameterReference, Type: System.Int16) (Syntax: 'i')

            ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: null, IsImplicit) (Syntax: 'i')
              Left: 
                IParameterReferenceOperation: i (OperationKind.ParameterReference, Type: System.Int16, IsImplicit) (Syntax: 'i')
              Right: 
                IBinaryOperation (BinaryOperatorKind.Add, Checked) (OperationKind.Binary, Type: System.Int16, IsImplicit) (Syntax: '[step]')
                  Left: 
                    IParameterReferenceOperation: i (OperationKind.ParameterReference, Type: System.Int16, IsImplicit) (Syntax: 'i')
                  Right: 
                    IFlowCaptureReferenceOperation: 3 (OperationKind.FlowCaptureReference, Type: System.Int16, IsImplicit) (Syntax: '[step]')

        Next (Regular) Block[B2]
}

Block[B4] - Exit
    Predecessors: [B2]
    Statements (0)
]]>.Value

            VerifyFlowGraphAndDiagnosticsForTest(Of MethodBlockSyntax)(source, expectedFlowGraph, expectedDiagnostics)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation, CompilerFeature.Dataflow)>
        <Fact()>
        Public Sub ForToFlow_05()
            Dim source = <![CDATA[
Imports System
Public Class C
    Sub M(i As Decimal, [step] As Decimal, result As Decimal) 'BIND:"Sub M"
        For i = 3D To 0D Step [step]
            result = i
        Next
    End Sub
End Class]]>.Value

            Dim expectedDiagnostics = String.Empty

            Dim expectedFlowGraph = <![CDATA[
Block[B0] - Entry
    Statements (0)
    Next (Regular) Block[B1]
        Entering: {R1} {R2}

.locals {R1}
{
    CaptureIds: [2] [3] [4]
    .locals {R2}
    {
        CaptureIds: [0] [1]
        Block[B1] - Block
            Predecessors: [B0]
            Statements (6)
                IFlowCaptureOperation: 0 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'i')
                  Value: 
                    IParameterReferenceOperation: i (OperationKind.ParameterReference, Type: System.Decimal) (Syntax: 'i')

                IFlowCaptureOperation: 1 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: '3D')
                  Value: 
                    ILiteralOperation (OperationKind.Literal, Type: System.Decimal, Constant: 3) (Syntax: '3D')

                IFlowCaptureOperation: 2 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: '0D')
                  Value: 
                    ILiteralOperation (OperationKind.Literal, Type: System.Decimal, Constant: 0) (Syntax: '0D')

                IFlowCaptureOperation: 3 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: '[step]')
                  Value: 
                    IParameterReferenceOperation: step (OperationKind.ParameterReference, Type: System.Decimal) (Syntax: '[step]')

                IFlowCaptureOperation: 4 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: '[step]')
                  Value: 
                    IBinaryOperation (BinaryOperatorKind.GreaterThanOrEqual) (OperationKind.Binary, Type: System.Boolean, IsImplicit) (Syntax: '[step]')
                      Left: 
                        IFlowCaptureReferenceOperation: 3 (OperationKind.FlowCaptureReference, Type: System.Decimal, IsImplicit) (Syntax: '[step]')
                      Right: 
                        ILiteralOperation (OperationKind.Literal, Type: System.Decimal, Constant: 0, IsImplicit) (Syntax: '[step]')

                ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: null, IsImplicit) (Syntax: '3D')
                  Left: 
                    IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: System.Decimal, IsImplicit) (Syntax: 'i')
                  Right: 
                    IFlowCaptureReferenceOperation: 1 (OperationKind.FlowCaptureReference, Type: System.Decimal, Constant: 3, IsImplicit) (Syntax: '3D')

            Next (Regular) Block[B2]
                Leaving: {R2}
                Entering: {R3}
    }
    .locals {R3}
    {
        CaptureIds: [5]
        Block[B2] - Block
            Predecessors: [B1] [B5]
            Statements (1)
                IFlowCaptureOperation: 5 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'i')
                  Value: 
                    IParameterReferenceOperation: i (OperationKind.ParameterReference, Type: System.Decimal, IsImplicit) (Syntax: 'i')

            Jump if False (Regular) to Block[B4]
                IFlowCaptureReferenceOperation: 4 (OperationKind.FlowCaptureReference, Type: System.Boolean, IsImplicit) (Syntax: '[step]')

            Next (Regular) Block[B3]
        Block[B3] - Block
            Predecessors: [B2]
            Statements (0)
            Jump if False (Regular) to Block[B6]
                IBinaryOperation (BinaryOperatorKind.LessThanOrEqual) (OperationKind.Binary, Type: System.Boolean, IsImplicit) (Syntax: '0D')
                  Left: 
                    IFlowCaptureReferenceOperation: 5 (OperationKind.FlowCaptureReference, Type: System.Decimal, IsImplicit) (Syntax: 'i')
                  Right: 
                    IFlowCaptureReferenceOperation: 2 (OperationKind.FlowCaptureReference, Type: System.Decimal, Constant: 0, IsImplicit) (Syntax: '0D')
                Leaving: {R3} {R1}

            Next (Regular) Block[B5]
                Leaving: {R3}
        Block[B4] - Block
            Predecessors: [B2]
            Statements (0)
            Jump if False (Regular) to Block[B6]
                IBinaryOperation (BinaryOperatorKind.GreaterThanOrEqual) (OperationKind.Binary, Type: System.Boolean, IsImplicit) (Syntax: '0D')
                  Left: 
                    IFlowCaptureReferenceOperation: 5 (OperationKind.FlowCaptureReference, Type: System.Decimal, IsImplicit) (Syntax: 'i')
                  Right: 
                    IFlowCaptureReferenceOperation: 2 (OperationKind.FlowCaptureReference, Type: System.Decimal, Constant: 0, IsImplicit) (Syntax: '0D')
                Leaving: {R3} {R1}

            Next (Regular) Block[B5]
                Leaving: {R3}
    }

    Block[B5] - Block
        Predecessors: [B3] [B4]
        Statements (2)
            IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'result = i')
              Expression: 
                ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Decimal, IsImplicit) (Syntax: 'result = i')
                  Left: 
                    IParameterReferenceOperation: result (OperationKind.ParameterReference, Type: System.Decimal) (Syntax: 'result')
                  Right: 
                    IParameterReferenceOperation: i (OperationKind.ParameterReference, Type: System.Decimal) (Syntax: 'i')

            ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: null, IsImplicit) (Syntax: 'i')
              Left: 
                IParameterReferenceOperation: i (OperationKind.ParameterReference, Type: System.Decimal, IsImplicit) (Syntax: 'i')
              Right: 
                IBinaryOperation (BinaryOperatorKind.Add, Checked) (OperationKind.Binary, Type: System.Decimal, IsImplicit) (Syntax: '[step]')
                  Left: 
                    IParameterReferenceOperation: i (OperationKind.ParameterReference, Type: System.Decimal, IsImplicit) (Syntax: 'i')
                  Right: 
                    IFlowCaptureReferenceOperation: 3 (OperationKind.FlowCaptureReference, Type: System.Decimal, IsImplicit) (Syntax: '[step]')

        Next (Regular) Block[B2]
            Entering: {R3}
}

Block[B6] - Exit
    Predecessors: [B3] [B4]
    Statements (0)
]]>.Value

            VerifyFlowGraphAndDiagnosticsForTest(Of MethodBlockSyntax)(source, expectedFlowGraph, expectedDiagnostics)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation, CompilerFeature.Dataflow)>
        <Fact()>
        Public Sub ForToFlow_06()
            Dim source = <![CDATA[
Imports System
Public Enum MyEnum As UShort
    One = 1
End Enum
Public Class C
    Sub M(i as MyEnum, init As MyEnum, limit As MyEnum, [step] as MyEnum, result As MyEnum) 'BIND:"Sub M"
        For i = init To limit Step [step]
            result = i
        Next
    End Sub
End Class]]>.Value

            Dim expectedDiagnostics = String.Empty

            Dim expectedFlowGraph = <![CDATA[
Block[B0] - Entry
    Statements (0)
    Next (Regular) Block[B1]
        Entering: {R1} {R2}

.locals {R1}
{
    CaptureIds: [2] [3]
    .locals {R2}
    {
        CaptureIds: [0] [1]
        Block[B1] - Block
            Predecessors: [B0]
            Statements (5)
                IFlowCaptureOperation: 0 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'i')
                  Value: 
                    IParameterReferenceOperation: i (OperationKind.ParameterReference, Type: MyEnum) (Syntax: 'i')

                IFlowCaptureOperation: 1 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'init')
                  Value: 
                    IParameterReferenceOperation: init (OperationKind.ParameterReference, Type: MyEnum) (Syntax: 'init')

                IFlowCaptureOperation: 2 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'limit')
                  Value: 
                    IParameterReferenceOperation: limit (OperationKind.ParameterReference, Type: MyEnum) (Syntax: 'limit')

                IFlowCaptureOperation: 3 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: '[step]')
                  Value: 
                    IParameterReferenceOperation: step (OperationKind.ParameterReference, Type: MyEnum) (Syntax: '[step]')

                ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: null, IsImplicit) (Syntax: 'init')
                  Left: 
                    IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: MyEnum, IsImplicit) (Syntax: 'i')
                  Right: 
                    IFlowCaptureReferenceOperation: 1 (OperationKind.FlowCaptureReference, Type: MyEnum, IsImplicit) (Syntax: 'init')

            Next (Regular) Block[B2]
                Leaving: {R2}
    }

    Block[B2] - Block
        Predecessors: [B1] [B3]
        Statements (0)
        Jump if False (Regular) to Block[B4]
            IBinaryOperation (BinaryOperatorKind.LessThanOrEqual) (OperationKind.Binary, Type: System.Boolean, IsImplicit) (Syntax: 'limit')
              Left: 
                IParameterReferenceOperation: i (OperationKind.ParameterReference, Type: MyEnum, IsImplicit) (Syntax: 'i')
              Right: 
                IFlowCaptureReferenceOperation: 2 (OperationKind.FlowCaptureReference, Type: MyEnum, IsImplicit) (Syntax: 'limit')
            Leaving: {R1}

        Next (Regular) Block[B3]
    Block[B3] - Block
        Predecessors: [B2]
        Statements (2)
            IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'result = i')
              Expression: 
                ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: MyEnum, IsImplicit) (Syntax: 'result = i')
                  Left: 
                    IParameterReferenceOperation: result (OperationKind.ParameterReference, Type: MyEnum) (Syntax: 'result')
                  Right: 
                    IParameterReferenceOperation: i (OperationKind.ParameterReference, Type: MyEnum) (Syntax: 'i')

            ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: null, IsImplicit) (Syntax: 'i')
              Left: 
                IParameterReferenceOperation: i (OperationKind.ParameterReference, Type: MyEnum, IsImplicit) (Syntax: 'i')
              Right: 
                IBinaryOperation (BinaryOperatorKind.Add, Checked) (OperationKind.Binary, Type: MyEnum, IsImplicit) (Syntax: '[step]')
                  Left: 
                    IParameterReferenceOperation: i (OperationKind.ParameterReference, Type: MyEnum, IsImplicit) (Syntax: 'i')
                  Right: 
                    IFlowCaptureReferenceOperation: 3 (OperationKind.FlowCaptureReference, Type: MyEnum, IsImplicit) (Syntax: '[step]')

        Next (Regular) Block[B2]
}

Block[B4] - Exit
    Predecessors: [B2]
    Statements (0)
]]>.Value

            VerifyFlowGraphAndDiagnosticsForTest(Of MethodBlockSyntax)(source, expectedFlowGraph, expectedDiagnostics)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation, CompilerFeature.Dataflow)>
        <Fact()>
        Public Sub ForToFlow_07()
            Dim source = <![CDATA[
Imports System
Public Enum MyEnum As SByte
    One = 1
End Enum
Public Class C
    Sub M(i as MyEnum, init As MyEnum, limit As MyEnum, [step] as MyEnum, result As MyEnum) 'BIND:"Sub M"
        For i = init To limit Step [step]
            result = i
        Next
    End Sub
End Class]]>.Value

            Dim expectedDiagnostics = String.Empty

            Dim expectedFlowGraph = <![CDATA[
Block[B0] - Entry
    Statements (0)
    Next (Regular) Block[B1]
        Entering: {R1} {R2}

.locals {R1}
{
    CaptureIds: [2] [3]
    .locals {R2}
    {
        CaptureIds: [0] [1]
        Block[B1] - Block
            Predecessors: [B0]
            Statements (5)
                IFlowCaptureOperation: 0 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'i')
                  Value: 
                    IParameterReferenceOperation: i (OperationKind.ParameterReference, Type: MyEnum) (Syntax: 'i')

                IFlowCaptureOperation: 1 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'init')
                  Value: 
                    IParameterReferenceOperation: init (OperationKind.ParameterReference, Type: MyEnum) (Syntax: 'init')

                IFlowCaptureOperation: 2 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'limit')
                  Value: 
                    IParameterReferenceOperation: limit (OperationKind.ParameterReference, Type: MyEnum) (Syntax: 'limit')

                IFlowCaptureOperation: 3 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: '[step]')
                  Value: 
                    IParameterReferenceOperation: step (OperationKind.ParameterReference, Type: MyEnum) (Syntax: '[step]')

                ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: null, IsImplicit) (Syntax: 'init')
                  Left: 
                    IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: MyEnum, IsImplicit) (Syntax: 'i')
                  Right: 
                    IFlowCaptureReferenceOperation: 1 (OperationKind.FlowCaptureReference, Type: MyEnum, IsImplicit) (Syntax: 'init')

            Next (Regular) Block[B2]
                Leaving: {R2}
    }

    Block[B2] - Block
        Predecessors: [B1] [B3]
        Statements (0)
        Jump if False (Regular) to Block[B4]
            IBinaryOperation (BinaryOperatorKind.LessThanOrEqual) (OperationKind.Binary, Type: System.Boolean, IsImplicit) (Syntax: 'limit')
              Left: 
                IBinaryOperation (BinaryOperatorKind.ExclusiveOr) (OperationKind.Binary, Type: MyEnum, IsImplicit) (Syntax: 'i')
                  Left: 
                    IBinaryOperation (BinaryOperatorKind.RightShift) (OperationKind.Binary, Type: MyEnum, IsImplicit) (Syntax: 'i')
                      Left: 
                        IFlowCaptureReferenceOperation: 3 (OperationKind.FlowCaptureReference, Type: MyEnum, IsImplicit) (Syntax: '[step]')
                      Right: 
                        ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 7, IsImplicit) (Syntax: 'i')
                  Right: 
                    IParameterReferenceOperation: i (OperationKind.ParameterReference, Type: MyEnum, IsImplicit) (Syntax: 'i')
              Right: 
                IBinaryOperation (BinaryOperatorKind.ExclusiveOr) (OperationKind.Binary, Type: MyEnum, IsImplicit) (Syntax: 'limit')
                  Left: 
                    IBinaryOperation (BinaryOperatorKind.RightShift) (OperationKind.Binary, Type: MyEnum, IsImplicit) (Syntax: 'limit')
                      Left: 
                        IFlowCaptureReferenceOperation: 3 (OperationKind.FlowCaptureReference, Type: MyEnum, IsImplicit) (Syntax: '[step]')
                      Right: 
                        ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 7, IsImplicit) (Syntax: 'limit')
                  Right: 
                    IFlowCaptureReferenceOperation: 2 (OperationKind.FlowCaptureReference, Type: MyEnum, IsImplicit) (Syntax: 'limit')
            Leaving: {R1}

        Next (Regular) Block[B3]
    Block[B3] - Block
        Predecessors: [B2]
        Statements (2)
            IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'result = i')
              Expression: 
                ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: MyEnum, IsImplicit) (Syntax: 'result = i')
                  Left: 
                    IParameterReferenceOperation: result (OperationKind.ParameterReference, Type: MyEnum) (Syntax: 'result')
                  Right: 
                    IParameterReferenceOperation: i (OperationKind.ParameterReference, Type: MyEnum) (Syntax: 'i')

            ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: null, IsImplicit) (Syntax: 'i')
              Left: 
                IParameterReferenceOperation: i (OperationKind.ParameterReference, Type: MyEnum, IsImplicit) (Syntax: 'i')
              Right: 
                IBinaryOperation (BinaryOperatorKind.Add, Checked) (OperationKind.Binary, Type: MyEnum, IsImplicit) (Syntax: '[step]')
                  Left: 
                    IParameterReferenceOperation: i (OperationKind.ParameterReference, Type: MyEnum, IsImplicit) (Syntax: 'i')
                  Right: 
                    IFlowCaptureReferenceOperation: 3 (OperationKind.FlowCaptureReference, Type: MyEnum, IsImplicit) (Syntax: '[step]')

        Next (Regular) Block[B2]
}

Block[B4] - Exit
    Predecessors: [B2]
    Statements (0)
]]>.Value

            VerifyFlowGraphAndDiagnosticsForTest(Of MethodBlockSyntax)(source, expectedFlowGraph, expectedDiagnostics)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation, CompilerFeature.Dataflow)>
        <Fact()>
        Public Sub ForToFlow_08()
            Dim source = <![CDATA[
Imports System
Public Enum MyEnum As Long
    MinusOne = -1
End Enum
Public Class C
    Sub M(i as MyEnum, init As MyEnum, limit As MyEnum, result As MyEnum) 'BIND:"Sub M"
        For i = init To limit Step MyEnum.MinusOne
            result = i
        Next
    End Sub
End Class]]>.Value

            Dim expectedDiagnostics = String.Empty

            Dim expectedFlowGraph = <![CDATA[
Block[B0] - Entry
    Statements (0)
    Next (Regular) Block[B1]
        Entering: {R1} {R2}

.locals {R1}
{
    CaptureIds: [2] [3]
    .locals {R2}
    {
        CaptureIds: [0] [1]
        Block[B1] - Block
            Predecessors: [B0]
            Statements (5)
                IFlowCaptureOperation: 0 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'i')
                  Value: 
                    IParameterReferenceOperation: i (OperationKind.ParameterReference, Type: MyEnum) (Syntax: 'i')

                IFlowCaptureOperation: 1 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'init')
                  Value: 
                    IParameterReferenceOperation: init (OperationKind.ParameterReference, Type: MyEnum) (Syntax: 'init')

                IFlowCaptureOperation: 2 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'limit')
                  Value: 
                    IParameterReferenceOperation: limit (OperationKind.ParameterReference, Type: MyEnum) (Syntax: 'limit')

                IFlowCaptureOperation: 3 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'MyEnum.MinusOne')
                  Value: 
                    IFieldReferenceOperation: MyEnum.MinusOne (Static) (OperationKind.FieldReference, Type: MyEnum, Constant: -1) (Syntax: 'MyEnum.MinusOne')
                      Instance Receiver: 
                        null

                ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: null, IsImplicit) (Syntax: 'init')
                  Left: 
                    IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: MyEnum, IsImplicit) (Syntax: 'i')
                  Right: 
                    IFlowCaptureReferenceOperation: 1 (OperationKind.FlowCaptureReference, Type: MyEnum, IsImplicit) (Syntax: 'init')

            Next (Regular) Block[B2]
                Leaving: {R2}
    }

    Block[B2] - Block
        Predecessors: [B1] [B3]
        Statements (0)
        Jump if False (Regular) to Block[B4]
            IBinaryOperation (BinaryOperatorKind.GreaterThanOrEqual) (OperationKind.Binary, Type: System.Boolean, IsImplicit) (Syntax: 'limit')
              Left: 
                IParameterReferenceOperation: i (OperationKind.ParameterReference, Type: MyEnum, IsImplicit) (Syntax: 'i')
              Right: 
                IFlowCaptureReferenceOperation: 2 (OperationKind.FlowCaptureReference, Type: MyEnum, IsImplicit) (Syntax: 'limit')
            Leaving: {R1}

        Next (Regular) Block[B3]
    Block[B3] - Block
        Predecessors: [B2]
        Statements (2)
            IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'result = i')
              Expression: 
                ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: MyEnum, IsImplicit) (Syntax: 'result = i')
                  Left: 
                    IParameterReferenceOperation: result (OperationKind.ParameterReference, Type: MyEnum) (Syntax: 'result')
                  Right: 
                    IParameterReferenceOperation: i (OperationKind.ParameterReference, Type: MyEnum) (Syntax: 'i')

            ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: null, IsImplicit) (Syntax: 'i')
              Left: 
                IParameterReferenceOperation: i (OperationKind.ParameterReference, Type: MyEnum, IsImplicit) (Syntax: 'i')
              Right: 
                IBinaryOperation (BinaryOperatorKind.Add, Checked) (OperationKind.Binary, Type: MyEnum, IsImplicit) (Syntax: 'MyEnum.MinusOne')
                  Left: 
                    IParameterReferenceOperation: i (OperationKind.ParameterReference, Type: MyEnum, IsImplicit) (Syntax: 'i')
                  Right: 
                    IFlowCaptureReferenceOperation: 3 (OperationKind.FlowCaptureReference, Type: MyEnum, Constant: -1, IsImplicit) (Syntax: 'MyEnum.MinusOne')

        Next (Regular) Block[B2]
}

Block[B4] - Exit
    Predecessors: [B2]
    Statements (0)
]]>.Value

            VerifyFlowGraphAndDiagnosticsForTest(Of MethodBlockSyntax)(source, expectedFlowGraph, expectedDiagnostics)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation, CompilerFeature.Dataflow)>
        <Fact()>
        Public Sub ForToFlow_09()
            Dim source = <![CDATA[
Imports System
Public Class C
    Sub M(i As Integer?, init As Integer?, limit As Integer?, [step] As Integer?, result As Integer?) 'BIND:"Sub M"
        For i = init To limit Step [step]
            result = i
        Next
    End Sub
End Class]]>.Value

            Dim expectedDiagnostics = String.Empty

            Dim expectedFlowGraph = <![CDATA[
Block[B0] - Entry
    Statements (0)
    Next (Regular) Block[B1]
        Entering: {R1} {R2}

.locals {R1}
{
    CaptureIds: [2] [3] [4]
    .locals {R2}
    {
        CaptureIds: [0] [1]
        Block[B1] - Block
            Predecessors: [B0]
            Statements (4)
                IFlowCaptureOperation: 0 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'i')
                  Value: 
                    IParameterReferenceOperation: i (OperationKind.ParameterReference, Type: System.Nullable(Of System.Int32)) (Syntax: 'i')

                IFlowCaptureOperation: 1 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'init')
                  Value: 
                    IParameterReferenceOperation: init (OperationKind.ParameterReference, Type: System.Nullable(Of System.Int32)) (Syntax: 'init')

                IFlowCaptureOperation: 2 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'limit')
                  Value: 
                    IParameterReferenceOperation: limit (OperationKind.ParameterReference, Type: System.Nullable(Of System.Int32)) (Syntax: 'limit')

                IFlowCaptureOperation: 3 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: '[step]')
                  Value: 
                    IParameterReferenceOperation: step (OperationKind.ParameterReference, Type: System.Nullable(Of System.Int32)) (Syntax: '[step]')

            Jump if False (Regular) to Block[B3]
                IIsNullOperation (OperationKind.IsNull, Type: System.Boolean, IsImplicit) (Syntax: '[step]')
                  Operand: 
                    IFlowCaptureReferenceOperation: 3 (OperationKind.FlowCaptureReference, Type: System.Nullable(Of System.Int32), IsImplicit) (Syntax: '[step]')

            Next (Regular) Block[B2]
        Block[B2] - Block
            Predecessors: [B1]
            Statements (1)
                IFlowCaptureOperation: 4 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: '[step]')
                  Value: 
                    ILiteralOperation (OperationKind.Literal, Type: System.Boolean, Constant: False, IsImplicit) (Syntax: '[step]')

            Next (Regular) Block[B4]
        Block[B3] - Block
            Predecessors: [B1]
            Statements (1)
                IFlowCaptureOperation: 4 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: '[step]')
                  Value: 
                    IBinaryOperation (BinaryOperatorKind.GreaterThanOrEqual) (OperationKind.Binary, Type: System.Boolean, IsImplicit) (Syntax: '[step]')
                      Left: 
                        IInvocationOperation ( Function System.Nullable(Of System.Int32).GetValueOrDefault() As System.Int32) (OperationKind.Invocation, Type: System.Int32, IsImplicit) (Syntax: '[step]')
                          Instance Receiver: 
                            IFlowCaptureReferenceOperation: 3 (OperationKind.FlowCaptureReference, Type: System.Nullable(Of System.Int32), IsImplicit) (Syntax: '[step]')
                          Arguments(0)
                      Right: 
                        ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 0, IsImplicit) (Syntax: '[step]')

            Next (Regular) Block[B4]
        Block[B4] - Block
            Predecessors: [B2] [B3]
            Statements (1)
                ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: null, IsImplicit) (Syntax: 'init')
                  Left: 
                    IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: System.Nullable(Of System.Int32), IsImplicit) (Syntax: 'i')
                  Right: 
                    IFlowCaptureReferenceOperation: 1 (OperationKind.FlowCaptureReference, Type: System.Nullable(Of System.Int32), IsImplicit) (Syntax: 'init')

            Next (Regular) Block[B5]
                Leaving: {R2}
    }

    Block[B5] - Block
        Predecessors: [B4] [B11] [B12]
        Statements (0)
        Jump if False (Regular) to Block[B6]
            IBinaryOperation (BinaryOperatorKind.Or) (OperationKind.Binary, Type: System.Boolean, IsImplicit) (Syntax: '[step]')
              Left: 
                IIsNullOperation (OperationKind.IsNull, Type: System.Boolean, IsImplicit) (Syntax: 'limit')
                  Operand: 
                    IFlowCaptureReferenceOperation: 2 (OperationKind.FlowCaptureReference, Type: System.Nullable(Of System.Int32), IsImplicit) (Syntax: 'limit')
              Right: 
                IIsNullOperation (OperationKind.IsNull, Type: System.Boolean, IsImplicit) (Syntax: 'i')
                  Operand: 
                    IParameterReferenceOperation: i (OperationKind.ParameterReference, Type: System.Nullable(Of System.Int32), IsImplicit) (Syntax: 'i')
            Entering: {R3}

        Next (Regular) Block[B13]
            Leaving: {R1}

    .locals {R3}
    {
        CaptureIds: [5]
        Block[B6] - Block
            Predecessors: [B5]
            Statements (1)
                IFlowCaptureOperation: 5 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'i')
                  Value: 
                    IInvocationOperation ( Function System.Nullable(Of System.Int32).GetValueOrDefault() As System.Int32) (OperationKind.Invocation, Type: System.Int32, IsImplicit) (Syntax: 'i')
                      Instance Receiver: 
                        IParameterReferenceOperation: i (OperationKind.ParameterReference, Type: System.Nullable(Of System.Int32), IsImplicit) (Syntax: 'i')
                      Arguments(0)

            Jump if False (Regular) to Block[B8]
                IFlowCaptureReferenceOperation: 4 (OperationKind.FlowCaptureReference, Type: System.Boolean, IsImplicit) (Syntax: '[step]')

            Next (Regular) Block[B7]
        Block[B7] - Block
            Predecessors: [B6]
            Statements (0)
            Jump if False (Regular) to Block[B13]
                IBinaryOperation (BinaryOperatorKind.LessThanOrEqual) (OperationKind.Binary, Type: System.Boolean, IsImplicit) (Syntax: 'limit')
                  Left: 
                    IFlowCaptureReferenceOperation: 5 (OperationKind.FlowCaptureReference, Type: System.Int32, IsImplicit) (Syntax: 'i')
                  Right: 
                    IInvocationOperation ( Function System.Nullable(Of System.Int32).GetValueOrDefault() As System.Int32) (OperationKind.Invocation, Type: System.Int32, IsImplicit) (Syntax: 'limit')
                      Instance Receiver: 
                        IFlowCaptureReferenceOperation: 2 (OperationKind.FlowCaptureReference, Type: System.Nullable(Of System.Int32), IsImplicit) (Syntax: 'limit')
                      Arguments(0)
                Leaving: {R3} {R1}

            Next (Regular) Block[B9]
                Leaving: {R3}
        Block[B8] - Block
            Predecessors: [B6]
            Statements (0)
            Jump if False (Regular) to Block[B13]
                IBinaryOperation (BinaryOperatorKind.GreaterThanOrEqual) (OperationKind.Binary, Type: System.Boolean, IsImplicit) (Syntax: 'limit')
                  Left: 
                    IFlowCaptureReferenceOperation: 5 (OperationKind.FlowCaptureReference, Type: System.Int32, IsImplicit) (Syntax: 'i')
                  Right: 
                    IInvocationOperation ( Function System.Nullable(Of System.Int32).GetValueOrDefault() As System.Int32) (OperationKind.Invocation, Type: System.Int32, IsImplicit) (Syntax: 'limit')
                      Instance Receiver: 
                        IFlowCaptureReferenceOperation: 2 (OperationKind.FlowCaptureReference, Type: System.Nullable(Of System.Int32), IsImplicit) (Syntax: 'limit')
                      Arguments(0)
                Leaving: {R3} {R1}

            Next (Regular) Block[B9]
                Leaving: {R3}
    }

    Block[B9] - Block
        Predecessors: [B7] [B8]
        Statements (1)
            IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'result = i')
              Expression: 
                ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Nullable(Of System.Int32), IsImplicit) (Syntax: 'result = i')
                  Left: 
                    IParameterReferenceOperation: result (OperationKind.ParameterReference, Type: System.Nullable(Of System.Int32)) (Syntax: 'result')
                  Right: 
                    IParameterReferenceOperation: i (OperationKind.ParameterReference, Type: System.Nullable(Of System.Int32)) (Syntax: 'i')

        Next (Regular) Block[B10]
            Entering: {R4}

    .locals {R4}
    {
        CaptureIds: [6]
        Block[B10] - Block
            Predecessors: [B9]
            Statements (1)
                IFlowCaptureOperation: 6 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'i')
                  Value: 
                    IParameterReferenceOperation: i (OperationKind.ParameterReference, Type: System.Nullable(Of System.Int32), IsImplicit) (Syntax: 'i')

            Jump if False (Regular) to Block[B12]
                IBinaryOperation (BinaryOperatorKind.Or) (OperationKind.Binary, Type: System.Boolean, IsImplicit) (Syntax: '[step]')
                  Left: 
                    IIsNullOperation (OperationKind.IsNull, Type: System.Boolean, IsImplicit) (Syntax: '[step]')
                      Operand: 
                        IFlowCaptureReferenceOperation: 3 (OperationKind.FlowCaptureReference, Type: System.Nullable(Of System.Int32), IsImplicit) (Syntax: '[step]')
                  Right: 
                    IIsNullOperation (OperationKind.IsNull, Type: System.Boolean, IsImplicit) (Syntax: 'i')
                      Operand: 
                        IParameterReferenceOperation: i (OperationKind.ParameterReference, Type: System.Nullable(Of System.Int32), IsImplicit) (Syntax: 'i')

            Next (Regular) Block[B11]
        Block[B11] - Block
            Predecessors: [B10]
            Statements (1)
                ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: null, IsImplicit) (Syntax: 'i')
                  Left: 
                    IFlowCaptureReferenceOperation: 6 (OperationKind.FlowCaptureReference, Type: System.Nullable(Of System.Int32), IsImplicit) (Syntax: 'i')
                  Right: 
                    IDefaultValueOperation (OperationKind.DefaultValue, Type: System.Nullable(Of System.Int32), IsImplicit) (Syntax: 'i')

            Next (Regular) Block[B5]
                Leaving: {R4}
        Block[B12] - Block
            Predecessors: [B10]
            Statements (1)
                ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: null, IsImplicit) (Syntax: 'i')
                  Left: 
                    IFlowCaptureReferenceOperation: 6 (OperationKind.FlowCaptureReference, Type: System.Nullable(Of System.Int32), IsImplicit) (Syntax: 'i')
                  Right: 
                    IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.Nullable(Of System.Int32), IsImplicit) (Syntax: '[step]')
                      Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                        (WideningNullable)
                      Operand: 
                        IBinaryOperation (BinaryOperatorKind.Add, Checked) (OperationKind.Binary, Type: System.Int32, IsImplicit) (Syntax: '[step]')
                          Left: 
                            IInvocationOperation ( Function System.Nullable(Of System.Int32).GetValueOrDefault() As System.Int32) (OperationKind.Invocation, Type: System.Int32, IsImplicit) (Syntax: 'i')
                              Instance Receiver: 
                                IParameterReferenceOperation: i (OperationKind.ParameterReference, Type: System.Nullable(Of System.Int32), IsImplicit) (Syntax: 'i')
                              Arguments(0)
                          Right: 
                            IInvocationOperation ( Function System.Nullable(Of System.Int32).GetValueOrDefault() As System.Int32) (OperationKind.Invocation, Type: System.Int32, IsImplicit) (Syntax: '[step]')
                              Instance Receiver: 
                                IFlowCaptureReferenceOperation: 3 (OperationKind.FlowCaptureReference, Type: System.Nullable(Of System.Int32), IsImplicit) (Syntax: '[step]')
                              Arguments(0)

            Next (Regular) Block[B5]
                Leaving: {R4}
    }
}

Block[B13] - Exit
    Predecessors: [B5] [B7] [B8]
    Statements (0)
]]>.Value

            VerifyFlowGraphAndDiagnosticsForTest(Of MethodBlockSyntax)(source, expectedFlowGraph, expectedDiagnostics)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation, CompilerFeature.Dataflow)>
        <Fact()>
        Public Sub ForToFlow_10()
            Dim source = <![CDATA[
Imports System
Public Class C
    Sub M(i As C, init As C, limit As C, [step] As C, result As C) 'BIND:"Sub M"
        For i = init To limit Step [step]
            result = i
        Next
    End Sub
End Class]]>.Value

            Dim expectedDiagnostics = <![CDATA[
BC33038: Type 'C' must define operator '+' to be used in a 'For' statement.
        For i = init To limit Step [step]
        ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
BC33038: Type 'C' must define operator '-' to be used in a 'For' statement.
        For i = init To limit Step [step]
        ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
BC33038: Type 'C' must define operator '<=' to be used in a 'For' statement.
        For i = init To limit Step [step]
        ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
BC33038: Type 'C' must define operator '>=' to be used in a 'For' statement.
        For i = init To limit Step [step]
        ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
]]>.Value
            Dim expectedFlowGraph = <![CDATA[
Block[B0] - Entry
    Statements (0)
    Next (Regular) Block[B1]
        Entering: {R1} {R2}

.locals {R1}
{
    CaptureIds: [2] [3]
    .locals {R2}
    {
        CaptureIds: [0] [1]
        Block[B1] - Block
            Predecessors: [B0]
            Statements (5)
                IFlowCaptureOperation: 0 (OperationKind.FlowCapture, Type: null, IsInvalid, IsImplicit) (Syntax: 'i')
                  Value: 
                    IParameterReferenceOperation: i (OperationKind.ParameterReference, Type: C, IsInvalid) (Syntax: 'i')

                IFlowCaptureOperation: 1 (OperationKind.FlowCapture, Type: null, IsInvalid, IsImplicit) (Syntax: 'init')
                  Value: 
                    IParameterReferenceOperation: init (OperationKind.ParameterReference, Type: C, IsInvalid) (Syntax: 'init')

                IFlowCaptureOperation: 2 (OperationKind.FlowCapture, Type: null, IsInvalid, IsImplicit) (Syntax: 'limit')
                  Value: 
                    IParameterReferenceOperation: limit (OperationKind.ParameterReference, Type: C, IsInvalid) (Syntax: 'limit')

                IFlowCaptureOperation: 3 (OperationKind.FlowCapture, Type: null, IsInvalid, IsImplicit) (Syntax: '[step]')
                  Value: 
                    IParameterReferenceOperation: step (OperationKind.ParameterReference, Type: C, IsInvalid) (Syntax: '[step]')

                ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: null, IsInvalid, IsImplicit) (Syntax: 'init')
                  Left: 
                    IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: C, IsInvalid, IsImplicit) (Syntax: 'i')
                  Right: 
                    IFlowCaptureReferenceOperation: 1 (OperationKind.FlowCaptureReference, Type: C, IsInvalid, IsImplicit) (Syntax: 'init')

            Next (Regular) Block[B2]
                Leaving: {R2}
    }

    Block[B2] - Block
        Predecessors: [B1] [B3]
        Statements (0)
        Jump if False (Regular) to Block[B4]
            IInvalidOperation (OperationKind.Invalid, Type: System.Boolean, IsInvalid, IsImplicit) (Syntax: 'limit')
              Children(2):
                  IParameterReferenceOperation: i (OperationKind.ParameterReference, Type: C, IsInvalid, IsImplicit) (Syntax: 'i')
                  IFlowCaptureReferenceOperation: 2 (OperationKind.FlowCaptureReference, Type: C, IsInvalid, IsImplicit) (Syntax: 'limit')
            Leaving: {R1}

        Next (Regular) Block[B3]
    Block[B3] - Block
        Predecessors: [B2]
        Statements (2)
            IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'result = i')
              Expression: 
                ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: C, IsImplicit) (Syntax: 'result = i')
                  Left: 
                    IParameterReferenceOperation: result (OperationKind.ParameterReference, Type: C) (Syntax: 'result')
                  Right: 
                    IParameterReferenceOperation: i (OperationKind.ParameterReference, Type: C) (Syntax: 'i')

            ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: null, IsInvalid, IsImplicit) (Syntax: 'i')
              Left: 
                IParameterReferenceOperation: i (OperationKind.ParameterReference, Type: C, IsInvalid, IsImplicit) (Syntax: 'i')
              Right: 
                IBinaryOperation (BinaryOperatorKind.Add, Checked) (OperationKind.Binary, Type: C, IsInvalid, IsImplicit) (Syntax: '[step]')
                  Left: 
                    IParameterReferenceOperation: i (OperationKind.ParameterReference, Type: C, IsInvalid, IsImplicit) (Syntax: 'i')
                  Right: 
                    IFlowCaptureReferenceOperation: 3 (OperationKind.FlowCaptureReference, Type: C, IsInvalid, IsImplicit) (Syntax: '[step]')

        Next (Regular) Block[B2]
}

Block[B4] - Exit
    Predecessors: [B2]
    Statements (0)
]]>.Value

            VerifyFlowGraphAndDiagnosticsForTest(Of MethodBlockSyntax)(source, expectedFlowGraph, expectedDiagnostics)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation, CompilerFeature.Dataflow)>
        <Fact()>
        Public Sub ForToFlow_11()
            Dim source = <![CDATA[
Imports System
Public Structure C
    Sub M(i As C?, init As C?, limit As C?, [step] As C?, result As C?) 'BIND:"Sub M"
        For i = init To limit Step [step]
            result = i
        Next
    End Sub
End Structure]]>.Value

            Dim expectedDiagnostics = <![CDATA[
BC33038: Type 'C?' must define operator '+' to be used in a 'For' statement.
        For i = init To limit Step [step]
        ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
BC33038: Type 'C?' must define operator '-' to be used in a 'For' statement.
        For i = init To limit Step [step]
        ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
BC33038: Type 'C?' must define operator '<=' to be used in a 'For' statement.
        For i = init To limit Step [step]
        ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
BC33038: Type 'C?' must define operator '>=' to be used in a 'For' statement.
        For i = init To limit Step [step]
        ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
]]>.Value
            Dim expectedFlowGraph = <![CDATA[
Block[B0] - Entry
    Statements (0)
    Next (Regular) Block[B1]
        Entering: {R1} {R2}

.locals {R1}
{
    CaptureIds: [2] [3]
    .locals {R2}
    {
        CaptureIds: [0] [1]
        Block[B1] - Block
            Predecessors: [B0]
            Statements (5)
                IFlowCaptureOperation: 0 (OperationKind.FlowCapture, Type: null, IsInvalid, IsImplicit) (Syntax: 'i')
                  Value: 
                    IParameterReferenceOperation: i (OperationKind.ParameterReference, Type: System.Nullable(Of C), IsInvalid) (Syntax: 'i')

                IFlowCaptureOperation: 1 (OperationKind.FlowCapture, Type: null, IsInvalid, IsImplicit) (Syntax: 'init')
                  Value: 
                    IParameterReferenceOperation: init (OperationKind.ParameterReference, Type: System.Nullable(Of C), IsInvalid) (Syntax: 'init')

                IFlowCaptureOperation: 2 (OperationKind.FlowCapture, Type: null, IsInvalid, IsImplicit) (Syntax: 'limit')
                  Value: 
                    IParameterReferenceOperation: limit (OperationKind.ParameterReference, Type: System.Nullable(Of C), IsInvalid) (Syntax: 'limit')

                IFlowCaptureOperation: 3 (OperationKind.FlowCapture, Type: null, IsInvalid, IsImplicit) (Syntax: '[step]')
                  Value: 
                    IParameterReferenceOperation: step (OperationKind.ParameterReference, Type: System.Nullable(Of C), IsInvalid) (Syntax: '[step]')

                ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: null, IsInvalid, IsImplicit) (Syntax: 'init')
                  Left: 
                    IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: System.Nullable(Of C), IsInvalid, IsImplicit) (Syntax: 'i')
                  Right: 
                    IFlowCaptureReferenceOperation: 1 (OperationKind.FlowCaptureReference, Type: System.Nullable(Of C), IsInvalid, IsImplicit) (Syntax: 'init')

            Next (Regular) Block[B2]
                Leaving: {R2}
    }

    Block[B2] - Block
        Predecessors: [B1] [B5] [B6]
        Statements (0)
        Jump if False (Regular) to Block[B7]
            IInvalidOperation (OperationKind.Invalid, Type: System.Boolean, IsInvalid, IsImplicit) (Syntax: 'limit')
              Children(2):
                  IParameterReferenceOperation: i (OperationKind.ParameterReference, Type: System.Nullable(Of C), IsInvalid, IsImplicit) (Syntax: 'i')
                  IFlowCaptureReferenceOperation: 2 (OperationKind.FlowCaptureReference, Type: System.Nullable(Of C), IsInvalid, IsImplicit) (Syntax: 'limit')
            Leaving: {R1}

        Next (Regular) Block[B3]
    Block[B3] - Block
        Predecessors: [B2]
        Statements (1)
            IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'result = i')
              Expression: 
                ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Nullable(Of C), IsImplicit) (Syntax: 'result = i')
                  Left: 
                    IParameterReferenceOperation: result (OperationKind.ParameterReference, Type: System.Nullable(Of C)) (Syntax: 'result')
                  Right: 
                    IParameterReferenceOperation: i (OperationKind.ParameterReference, Type: System.Nullable(Of C)) (Syntax: 'i')

        Next (Regular) Block[B4]
            Entering: {R3}

    .locals {R3}
    {
        CaptureIds: [4]
        Block[B4] - Block
            Predecessors: [B3]
            Statements (1)
                IFlowCaptureOperation: 4 (OperationKind.FlowCapture, Type: null, IsInvalid, IsImplicit) (Syntax: 'i')
                  Value: 
                    IParameterReferenceOperation: i (OperationKind.ParameterReference, Type: System.Nullable(Of C), IsInvalid, IsImplicit) (Syntax: 'i')

            Jump if False (Regular) to Block[B6]
                IBinaryOperation (BinaryOperatorKind.Or) (OperationKind.Binary, Type: System.Boolean, IsInvalid, IsImplicit) (Syntax: '[step]')
                  Left: 
                    IIsNullOperation (OperationKind.IsNull, Type: System.Boolean, IsInvalid, IsImplicit) (Syntax: '[step]')
                      Operand: 
                        IFlowCaptureReferenceOperation: 3 (OperationKind.FlowCaptureReference, Type: System.Nullable(Of C), IsInvalid, IsImplicit) (Syntax: '[step]')
                  Right: 
                    IIsNullOperation (OperationKind.IsNull, Type: System.Boolean, IsInvalid, IsImplicit) (Syntax: 'i')
                      Operand: 
                        IParameterReferenceOperation: i (OperationKind.ParameterReference, Type: System.Nullable(Of C), IsInvalid, IsImplicit) (Syntax: 'i')

            Next (Regular) Block[B5]
        Block[B5] - Block
            Predecessors: [B4]
            Statements (1)
                ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: null, IsInvalid, IsImplicit) (Syntax: 'i')
                  Left: 
                    IFlowCaptureReferenceOperation: 4 (OperationKind.FlowCaptureReference, Type: System.Nullable(Of C), IsInvalid, IsImplicit) (Syntax: 'i')
                  Right: 
                    IDefaultValueOperation (OperationKind.DefaultValue, Type: System.Nullable(Of C), IsInvalid, IsImplicit) (Syntax: 'i')

            Next (Regular) Block[B2]
                Leaving: {R3}
        Block[B6] - Block
            Predecessors: [B4]
            Statements (1)
                ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: null, IsInvalid, IsImplicit) (Syntax: 'i')
                  Left: 
                    IFlowCaptureReferenceOperation: 4 (OperationKind.FlowCaptureReference, Type: System.Nullable(Of C), IsInvalid, IsImplicit) (Syntax: 'i')
                  Right: 
                    IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.Nullable(Of C), IsInvalid, IsImplicit) (Syntax: '[step]')
                      Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                        (WideningNullable)
                      Operand: 
                        IBinaryOperation (BinaryOperatorKind.Add, Checked) (OperationKind.Binary, Type: C, IsInvalid, IsImplicit) (Syntax: '[step]')
                          Left: 
                            IInvocationOperation ( Function System.Nullable(Of C).GetValueOrDefault() As C) (OperationKind.Invocation, Type: C, IsInvalid, IsImplicit) (Syntax: 'i')
                              Instance Receiver: 
                                IParameterReferenceOperation: i (OperationKind.ParameterReference, Type: System.Nullable(Of C), IsInvalid, IsImplicit) (Syntax: 'i')
                              Arguments(0)
                          Right: 
                            IInvocationOperation ( Function System.Nullable(Of C).GetValueOrDefault() As C) (OperationKind.Invocation, Type: C, IsInvalid, IsImplicit) (Syntax: '[step]')
                              Instance Receiver: 
                                IFlowCaptureReferenceOperation: 3 (OperationKind.FlowCaptureReference, Type: System.Nullable(Of C), IsInvalid, IsImplicit) (Syntax: '[step]')
                              Arguments(0)

            Next (Regular) Block[B2]
                Leaving: {R3}
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
        Public Sub ForToFlow_12()
            Dim source = <![CDATA[
Imports System
Public Class C1
    Sub M(i As C1, init As C2, limit As C3, [step] As C4, result As C1) 'BIND:"Sub M"
        For i = init To limit Step [step]
            result = i
        Next
    End Sub
End Class
Public Class C2
End Class
Public Class C3
End Class
Public Class C4
End Class
]]>.Value

            Dim expectedDiagnostics = <![CDATA[
BC30311: Value of type 'C2' cannot be converted to 'C1'.
        For i = init To limit Step [step]
                ~~~~
BC30311: Value of type 'C3' cannot be converted to 'C1'.
        For i = init To limit Step [step]
                        ~~~~~
BC30311: Value of type 'C4' cannot be converted to 'C1'.
        For i = init To limit Step [step]
                                   ~~~~~~
]]>.Value
            Dim expectedFlowGraph = <![CDATA[
Block[B0] - Entry
    Statements (0)
    Next (Regular) Block[B1]
        Entering: {R1} {R2}

.locals {R1}
{
    CaptureIds: [2] [3]
    .locals {R2}
    {
        CaptureIds: [0] [1]
        Block[B1] - Block
            Predecessors: [B0]
            Statements (5)
                IFlowCaptureOperation: 0 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'i')
                  Value: 
                    IParameterReferenceOperation: i (OperationKind.ParameterReference, Type: C1) (Syntax: 'i')

                IFlowCaptureOperation: 1 (OperationKind.FlowCapture, Type: null, IsInvalid, IsImplicit) (Syntax: 'init')
                  Value: 
                    IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: C1, IsInvalid, IsImplicit) (Syntax: 'init')
                      Conversion: CommonConversion (Exists: False, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                        (DelegateRelaxationLevelNone)
                      Operand: 
                        IParameterReferenceOperation: init (OperationKind.ParameterReference, Type: C2, IsInvalid) (Syntax: 'init')

                IFlowCaptureOperation: 2 (OperationKind.FlowCapture, Type: null, IsInvalid, IsImplicit) (Syntax: 'limit')
                  Value: 
                    IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: C1, IsInvalid, IsImplicit) (Syntax: 'limit')
                      Conversion: CommonConversion (Exists: False, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                        (DelegateRelaxationLevelNone)
                      Operand: 
                        IParameterReferenceOperation: limit (OperationKind.ParameterReference, Type: C3, IsInvalid) (Syntax: 'limit')

                IFlowCaptureOperation: 3 (OperationKind.FlowCapture, Type: null, IsInvalid, IsImplicit) (Syntax: '[step]')
                  Value: 
                    IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: C1, IsInvalid, IsImplicit) (Syntax: '[step]')
                      Conversion: CommonConversion (Exists: False, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                        (DelegateRelaxationLevelNone)
                      Operand: 
                        IParameterReferenceOperation: step (OperationKind.ParameterReference, Type: C4, IsInvalid) (Syntax: '[step]')

                ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: null, IsInvalid, IsImplicit) (Syntax: 'init')
                  Left: 
                    IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: C1, IsImplicit) (Syntax: 'i')
                  Right: 
                    IFlowCaptureReferenceOperation: 1 (OperationKind.FlowCaptureReference, Type: C1, IsInvalid, IsImplicit) (Syntax: 'init')

            Next (Regular) Block[B2]
                Leaving: {R2}
    }

    Block[B2] - Block
        Predecessors: [B1] [B3]
        Statements (0)
        Jump if False (Regular) to Block[B4]
            IInvalidOperation (OperationKind.Invalid, Type: System.Boolean, IsInvalid, IsImplicit) (Syntax: 'limit')
              Children(2):
                  IParameterReferenceOperation: i (OperationKind.ParameterReference, Type: C1, IsImplicit) (Syntax: 'i')
                  IFlowCaptureReferenceOperation: 2 (OperationKind.FlowCaptureReference, Type: C1, IsInvalid, IsImplicit) (Syntax: 'limit')
            Leaving: {R1}

        Next (Regular) Block[B3]
    Block[B3] - Block
        Predecessors: [B2]
        Statements (2)
            IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'result = i')
              Expression: 
                ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: C1, IsImplicit) (Syntax: 'result = i')
                  Left: 
                    IParameterReferenceOperation: result (OperationKind.ParameterReference, Type: C1) (Syntax: 'result')
                  Right: 
                    IParameterReferenceOperation: i (OperationKind.ParameterReference, Type: C1) (Syntax: 'i')

            ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: null, IsImplicit) (Syntax: 'i')
              Left: 
                IParameterReferenceOperation: i (OperationKind.ParameterReference, Type: C1, IsImplicit) (Syntax: 'i')
              Right: 
                IBinaryOperation (BinaryOperatorKind.Add, Checked) (OperationKind.Binary, Type: C1, IsInvalid, IsImplicit) (Syntax: '[step]')
                  Left: 
                    IParameterReferenceOperation: i (OperationKind.ParameterReference, Type: C1, IsImplicit) (Syntax: 'i')
                  Right: 
                    IFlowCaptureReferenceOperation: 3 (OperationKind.FlowCaptureReference, Type: C1, IsInvalid, IsImplicit) (Syntax: '[step]')

        Next (Regular) Block[B2]
}

Block[B4] - Exit
    Predecessors: [B2]
    Statements (0)
]]>.Value

            VerifyFlowGraphAndDiagnosticsForTest(Of MethodBlockSyntax)(source, expectedFlowGraph, expectedDiagnostics)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation, CompilerFeature.Dataflow)>
        <Fact()>
        Public Sub ForToFlow_13()
            Dim source = <![CDATA[
Imports System
Public Enum MyEnum As Integer
    MinusOne = -1
End Enum
Public Class C
    Sub M(i As MyEnum?, init As MyEnum?, limit As MyEnum?, [step] As MyEnum?, result As MyEnum?) 'BIND:"Sub M"
        For i = init To limit Step [step]
            result = i
        Next
    End Sub
End Class]]>.Value

            Dim expectedDiagnostics = String.Empty

            Dim expectedFlowGraph = <![CDATA[
Block[B0] - Entry
    Statements (0)
    Next (Regular) Block[B1]
        Entering: {R1} {R2}

.locals {R1}
{
    CaptureIds: [2] [3] [4]
    .locals {R2}
    {
        CaptureIds: [0] [1]
        Block[B1] - Block
            Predecessors: [B0]
            Statements (4)
                IFlowCaptureOperation: 0 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'i')
                  Value: 
                    IParameterReferenceOperation: i (OperationKind.ParameterReference, Type: System.Nullable(Of MyEnum)) (Syntax: 'i')

                IFlowCaptureOperation: 1 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'init')
                  Value: 
                    IParameterReferenceOperation: init (OperationKind.ParameterReference, Type: System.Nullable(Of MyEnum)) (Syntax: 'init')

                IFlowCaptureOperation: 2 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'limit')
                  Value: 
                    IParameterReferenceOperation: limit (OperationKind.ParameterReference, Type: System.Nullable(Of MyEnum)) (Syntax: 'limit')

                IFlowCaptureOperation: 3 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: '[step]')
                  Value: 
                    IParameterReferenceOperation: step (OperationKind.ParameterReference, Type: System.Nullable(Of MyEnum)) (Syntax: '[step]')

            Jump if False (Regular) to Block[B3]
                IIsNullOperation (OperationKind.IsNull, Type: System.Boolean, IsImplicit) (Syntax: '[step]')
                  Operand: 
                    IFlowCaptureReferenceOperation: 3 (OperationKind.FlowCaptureReference, Type: System.Nullable(Of MyEnum), IsImplicit) (Syntax: '[step]')

            Next (Regular) Block[B2]
        Block[B2] - Block
            Predecessors: [B1]
            Statements (1)
                IFlowCaptureOperation: 4 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: '[step]')
                  Value: 
                    ILiteralOperation (OperationKind.Literal, Type: System.Boolean, Constant: False, IsImplicit) (Syntax: '[step]')

            Next (Regular) Block[B4]
        Block[B3] - Block
            Predecessors: [B1]
            Statements (1)
                IFlowCaptureOperation: 4 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: '[step]')
                  Value: 
                    IBinaryOperation (BinaryOperatorKind.GreaterThanOrEqual) (OperationKind.Binary, Type: System.Boolean, IsImplicit) (Syntax: '[step]')
                      Left: 
                        IInvocationOperation ( Function System.Nullable(Of MyEnum).GetValueOrDefault() As MyEnum) (OperationKind.Invocation, Type: MyEnum, IsImplicit) (Syntax: '[step]')
                          Instance Receiver: 
                            IFlowCaptureReferenceOperation: 3 (OperationKind.FlowCaptureReference, Type: System.Nullable(Of MyEnum), IsImplicit) (Syntax: '[step]')
                          Arguments(0)
                      Right: 
                        ILiteralOperation (OperationKind.Literal, Type: MyEnum, Constant: 0, IsImplicit) (Syntax: '[step]')

            Next (Regular) Block[B4]
        Block[B4] - Block
            Predecessors: [B2] [B3]
            Statements (1)
                ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: null, IsImplicit) (Syntax: 'init')
                  Left: 
                    IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: System.Nullable(Of MyEnum), IsImplicit) (Syntax: 'i')
                  Right: 
                    IFlowCaptureReferenceOperation: 1 (OperationKind.FlowCaptureReference, Type: System.Nullable(Of MyEnum), IsImplicit) (Syntax: 'init')

            Next (Regular) Block[B5]
                Leaving: {R2}
    }

    Block[B5] - Block
        Predecessors: [B4] [B11] [B12]
        Statements (0)
        Jump if False (Regular) to Block[B6]
            IBinaryOperation (BinaryOperatorKind.Or) (OperationKind.Binary, Type: System.Boolean, IsImplicit) (Syntax: '[step]')
              Left: 
                IIsNullOperation (OperationKind.IsNull, Type: System.Boolean, IsImplicit) (Syntax: 'limit')
                  Operand: 
                    IFlowCaptureReferenceOperation: 2 (OperationKind.FlowCaptureReference, Type: System.Nullable(Of MyEnum), IsImplicit) (Syntax: 'limit')
              Right: 
                IIsNullOperation (OperationKind.IsNull, Type: System.Boolean, IsImplicit) (Syntax: 'i')
                  Operand: 
                    IParameterReferenceOperation: i (OperationKind.ParameterReference, Type: System.Nullable(Of MyEnum), IsImplicit) (Syntax: 'i')
            Entering: {R3}

        Next (Regular) Block[B13]
            Leaving: {R1}

    .locals {R3}
    {
        CaptureIds: [5]
        Block[B6] - Block
            Predecessors: [B5]
            Statements (1)
                IFlowCaptureOperation: 5 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'i')
                  Value: 
                    IInvocationOperation ( Function System.Nullable(Of MyEnum).GetValueOrDefault() As MyEnum) (OperationKind.Invocation, Type: MyEnum, IsImplicit) (Syntax: 'i')
                      Instance Receiver: 
                        IParameterReferenceOperation: i (OperationKind.ParameterReference, Type: System.Nullable(Of MyEnum), IsImplicit) (Syntax: 'i')
                      Arguments(0)

            Jump if False (Regular) to Block[B8]
                IFlowCaptureReferenceOperation: 4 (OperationKind.FlowCaptureReference, Type: System.Boolean, IsImplicit) (Syntax: '[step]')

            Next (Regular) Block[B7]
        Block[B7] - Block
            Predecessors: [B6]
            Statements (0)
            Jump if False (Regular) to Block[B13]
                IBinaryOperation (BinaryOperatorKind.LessThanOrEqual) (OperationKind.Binary, Type: System.Boolean, IsImplicit) (Syntax: 'limit')
                  Left: 
                    IFlowCaptureReferenceOperation: 5 (OperationKind.FlowCaptureReference, Type: MyEnum, IsImplicit) (Syntax: 'i')
                  Right: 
                    IInvocationOperation ( Function System.Nullable(Of MyEnum).GetValueOrDefault() As MyEnum) (OperationKind.Invocation, Type: MyEnum, IsImplicit) (Syntax: 'limit')
                      Instance Receiver: 
                        IFlowCaptureReferenceOperation: 2 (OperationKind.FlowCaptureReference, Type: System.Nullable(Of MyEnum), IsImplicit) (Syntax: 'limit')
                      Arguments(0)
                Leaving: {R3} {R1}

            Next (Regular) Block[B9]
                Leaving: {R3}
        Block[B8] - Block
            Predecessors: [B6]
            Statements (0)
            Jump if False (Regular) to Block[B13]
                IBinaryOperation (BinaryOperatorKind.GreaterThanOrEqual) (OperationKind.Binary, Type: System.Boolean, IsImplicit) (Syntax: 'limit')
                  Left: 
                    IFlowCaptureReferenceOperation: 5 (OperationKind.FlowCaptureReference, Type: MyEnum, IsImplicit) (Syntax: 'i')
                  Right: 
                    IInvocationOperation ( Function System.Nullable(Of MyEnum).GetValueOrDefault() As MyEnum) (OperationKind.Invocation, Type: MyEnum, IsImplicit) (Syntax: 'limit')
                      Instance Receiver: 
                        IFlowCaptureReferenceOperation: 2 (OperationKind.FlowCaptureReference, Type: System.Nullable(Of MyEnum), IsImplicit) (Syntax: 'limit')
                      Arguments(0)
                Leaving: {R3} {R1}

            Next (Regular) Block[B9]
                Leaving: {R3}
    }

    Block[B9] - Block
        Predecessors: [B7] [B8]
        Statements (1)
            IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'result = i')
              Expression: 
                ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Nullable(Of MyEnum), IsImplicit) (Syntax: 'result = i')
                  Left: 
                    IParameterReferenceOperation: result (OperationKind.ParameterReference, Type: System.Nullable(Of MyEnum)) (Syntax: 'result')
                  Right: 
                    IParameterReferenceOperation: i (OperationKind.ParameterReference, Type: System.Nullable(Of MyEnum)) (Syntax: 'i')

        Next (Regular) Block[B10]
            Entering: {R4}

    .locals {R4}
    {
        CaptureIds: [6]
        Block[B10] - Block
            Predecessors: [B9]
            Statements (1)
                IFlowCaptureOperation: 6 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'i')
                  Value: 
                    IParameterReferenceOperation: i (OperationKind.ParameterReference, Type: System.Nullable(Of MyEnum), IsImplicit) (Syntax: 'i')

            Jump if False (Regular) to Block[B12]
                IBinaryOperation (BinaryOperatorKind.Or) (OperationKind.Binary, Type: System.Boolean, IsImplicit) (Syntax: '[step]')
                  Left: 
                    IIsNullOperation (OperationKind.IsNull, Type: System.Boolean, IsImplicit) (Syntax: '[step]')
                      Operand: 
                        IFlowCaptureReferenceOperation: 3 (OperationKind.FlowCaptureReference, Type: System.Nullable(Of MyEnum), IsImplicit) (Syntax: '[step]')
                  Right: 
                    IIsNullOperation (OperationKind.IsNull, Type: System.Boolean, IsImplicit) (Syntax: 'i')
                      Operand: 
                        IParameterReferenceOperation: i (OperationKind.ParameterReference, Type: System.Nullable(Of MyEnum), IsImplicit) (Syntax: 'i')

            Next (Regular) Block[B11]
        Block[B11] - Block
            Predecessors: [B10]
            Statements (1)
                ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: null, IsImplicit) (Syntax: 'i')
                  Left: 
                    IFlowCaptureReferenceOperation: 6 (OperationKind.FlowCaptureReference, Type: System.Nullable(Of MyEnum), IsImplicit) (Syntax: 'i')
                  Right: 
                    IDefaultValueOperation (OperationKind.DefaultValue, Type: System.Nullable(Of MyEnum), IsImplicit) (Syntax: 'i')

            Next (Regular) Block[B5]
                Leaving: {R4}
        Block[B12] - Block
            Predecessors: [B10]
            Statements (1)
                ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: null, IsImplicit) (Syntax: 'i')
                  Left: 
                    IFlowCaptureReferenceOperation: 6 (OperationKind.FlowCaptureReference, Type: System.Nullable(Of MyEnum), IsImplicit) (Syntax: 'i')
                  Right: 
                    IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.Nullable(Of MyEnum), IsImplicit) (Syntax: '[step]')
                      Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                        (WideningNullable)
                      Operand: 
                        IBinaryOperation (BinaryOperatorKind.Add, Checked) (OperationKind.Binary, Type: MyEnum, IsImplicit) (Syntax: '[step]')
                          Left: 
                            IInvocationOperation ( Function System.Nullable(Of MyEnum).GetValueOrDefault() As MyEnum) (OperationKind.Invocation, Type: MyEnum, IsImplicit) (Syntax: 'i')
                              Instance Receiver: 
                                IParameterReferenceOperation: i (OperationKind.ParameterReference, Type: System.Nullable(Of MyEnum), IsImplicit) (Syntax: 'i')
                              Arguments(0)
                          Right: 
                            IInvocationOperation ( Function System.Nullable(Of MyEnum).GetValueOrDefault() As MyEnum) (OperationKind.Invocation, Type: MyEnum, IsImplicit) (Syntax: '[step]')
                              Instance Receiver: 
                                IFlowCaptureReferenceOperation: 3 (OperationKind.FlowCaptureReference, Type: System.Nullable(Of MyEnum), IsImplicit) (Syntax: '[step]')
                              Arguments(0)

            Next (Regular) Block[B5]
                Leaving: {R4}
    }
}

Block[B13] - Exit
    Predecessors: [B5] [B7] [B8]
    Statements (0)
]]>.Value

            VerifyFlowGraphAndDiagnosticsForTest(Of MethodBlockSyntax)(source, expectedFlowGraph, expectedDiagnostics)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation, CompilerFeature.Dataflow)>
        <Fact>
        Public Sub ForToFlow_14()
            Dim source = <![CDATA[
Imports System
Public Class C
    Sub M(c1 As C, c2 As C, bInit As Boolean, init1 As Integer, init2 As Integer, bLimit As Boolean, limit1 As Integer, limit2 As Integer, result As Boolean) 'BIND:"Sub M"
        For If(c1, c2).i = If(bInit, init1, init2) To If(bLimit, limit1, limit2) Step 2
            result = true
        Next
    End Sub

    Public i As Integer
End Class]]>.Value

            Dim expectedDiagnostics = String.Empty

            Dim expectedFlowGraph = <![CDATA[
Block[B0] - Entry
    Statements (0)
    Next (Regular) Block[B1]
        Entering: {R1} {R2} {R3} {R4}

.locals {R1}
{
    CaptureIds: [4] [5]
    .locals {R2}
    {
        CaptureIds: [2] [3]
        .locals {R3}
        {
            CaptureIds: [1]
            .locals {R4}
            {
                CaptureIds: [0]
                Block[B1] - Block
                    Predecessors: [B0]
                    Statements (1)
                        IFlowCaptureOperation: 0 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'c1')
                          Value: 
                            IParameterReferenceOperation: c1 (OperationKind.ParameterReference, Type: C) (Syntax: 'c1')

                    Jump if True (Regular) to Block[B3]
                        IIsNullOperation (OperationKind.IsNull, Type: System.Boolean, IsImplicit) (Syntax: 'c1')
                          Operand: 
                            IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: C, IsImplicit) (Syntax: 'c1')
                        Leaving: {R4}

                    Next (Regular) Block[B2]
                Block[B2] - Block
                    Predecessors: [B1]
                    Statements (1)
                        IFlowCaptureOperation: 1 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'c1')
                          Value: 
                            IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: C, IsImplicit) (Syntax: 'c1')

                    Next (Regular) Block[B4]
                        Leaving: {R4}
            }

            Block[B3] - Block
                Predecessors: [B1]
                Statements (1)
                    IFlowCaptureOperation: 1 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'c2')
                      Value: 
                        IParameterReferenceOperation: c2 (OperationKind.ParameterReference, Type: C) (Syntax: 'c2')

                Next (Regular) Block[B4]
            Block[B4] - Block
                Predecessors: [B2] [B3]
                Statements (1)
                    IFlowCaptureOperation: 2 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'If(c1, c2).i')
                      Value: 
                        IFieldReferenceOperation: C.i As System.Int32 (OperationKind.FieldReference, Type: System.Int32) (Syntax: 'If(c1, c2).i')
                          Instance Receiver: 
                            IFlowCaptureReferenceOperation: 1 (OperationKind.FlowCaptureReference, Type: C, IsImplicit) (Syntax: 'If(c1, c2)')

                Next (Regular) Block[B5]
                    Leaving: {R3}
        }

        Block[B5] - Block
            Predecessors: [B4]
            Statements (0)
            Jump if False (Regular) to Block[B7]
                IParameterReferenceOperation: bInit (OperationKind.ParameterReference, Type: System.Boolean) (Syntax: 'bInit')

            Next (Regular) Block[B6]
        Block[B6] - Block
            Predecessors: [B5]
            Statements (1)
                IFlowCaptureOperation: 3 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'init1')
                  Value: 
                    IParameterReferenceOperation: init1 (OperationKind.ParameterReference, Type: System.Int32) (Syntax: 'init1')

            Next (Regular) Block[B8]
        Block[B7] - Block
            Predecessors: [B5]
            Statements (1)
                IFlowCaptureOperation: 3 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'init2')
                  Value: 
                    IParameterReferenceOperation: init2 (OperationKind.ParameterReference, Type: System.Int32) (Syntax: 'init2')

            Next (Regular) Block[B8]
        Block[B8] - Block
            Predecessors: [B6] [B7]
            Statements (0)
            Jump if False (Regular) to Block[B10]
                IParameterReferenceOperation: bLimit (OperationKind.ParameterReference, Type: System.Boolean) (Syntax: 'bLimit')

            Next (Regular) Block[B9]
        Block[B9] - Block
            Predecessors: [B8]
            Statements (1)
                IFlowCaptureOperation: 4 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'limit1')
                  Value: 
                    IParameterReferenceOperation: limit1 (OperationKind.ParameterReference, Type: System.Int32) (Syntax: 'limit1')

            Next (Regular) Block[B11]
        Block[B10] - Block
            Predecessors: [B8]
            Statements (1)
                IFlowCaptureOperation: 4 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'limit2')
                  Value: 
                    IParameterReferenceOperation: limit2 (OperationKind.ParameterReference, Type: System.Int32) (Syntax: 'limit2')

            Next (Regular) Block[B11]
        Block[B11] - Block
            Predecessors: [B9] [B10]
            Statements (2)
                IFlowCaptureOperation: 5 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: '2')
                  Value: 
                    ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 2) (Syntax: '2')

                ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: null, IsImplicit) (Syntax: 'If(bInit, init1, init2)')
                  Left: 
                    IFlowCaptureReferenceOperation: 2 (OperationKind.FlowCaptureReference, Type: System.Int32, IsImplicit) (Syntax: 'If(c1, c2).i')
                  Right: 
                    IFlowCaptureReferenceOperation: 3 (OperationKind.FlowCaptureReference, Type: System.Int32, IsImplicit) (Syntax: 'If(bInit, init1, init2)')

            Next (Regular) Block[B12]
                Leaving: {R2}
                Entering: {R5} {R6}
    }
    .locals {R5}
    {
        CaptureIds: [7]
        .locals {R6}
        {
            CaptureIds: [6]
            Block[B12] - Block
                Predecessors: [B11] [B24]
                Statements (1)
                    IFlowCaptureOperation: 6 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'c1')
                      Value: 
                        IParameterReferenceOperation: c1 (OperationKind.ParameterReference, Type: C, IsImplicit) (Syntax: 'c1')

                Jump if True (Regular) to Block[B14]
                    IIsNullOperation (OperationKind.IsNull, Type: System.Boolean, IsImplicit) (Syntax: 'c1')
                      Operand: 
                        IFlowCaptureReferenceOperation: 6 (OperationKind.FlowCaptureReference, Type: C, IsImplicit) (Syntax: 'c1')
                    Leaving: {R6}

                Next (Regular) Block[B13]
            Block[B13] - Block
                Predecessors: [B12]
                Statements (1)
                    IFlowCaptureOperation: 7 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'c1')
                      Value: 
                        IFlowCaptureReferenceOperation: 6 (OperationKind.FlowCaptureReference, Type: C, IsImplicit) (Syntax: 'c1')

                Next (Regular) Block[B15]
                    Leaving: {R6}
        }

        Block[B14] - Block
            Predecessors: [B12]
            Statements (1)
                IFlowCaptureOperation: 7 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'c2')
                  Value: 
                    IParameterReferenceOperation: c2 (OperationKind.ParameterReference, Type: C, IsImplicit) (Syntax: 'c2')

            Next (Regular) Block[B15]
        Block[B15] - Block
            Predecessors: [B13] [B14]
            Statements (0)
            Jump if False (Regular) to Block[B25]
                IBinaryOperation (BinaryOperatorKind.LessThanOrEqual) (OperationKind.Binary, Type: System.Boolean, IsImplicit) (Syntax: 'If(bLimit,  ... t1, limit2)')
                  Left: 
                    IFieldReferenceOperation: C.i As System.Int32 (OperationKind.FieldReference, Type: System.Int32, IsImplicit) (Syntax: 'If(c1, c2).i')
                      Instance Receiver: 
                        IFlowCaptureReferenceOperation: 7 (OperationKind.FlowCaptureReference, Type: C, IsImplicit) (Syntax: 'If(c1, c2)')
                  Right: 
                    IFlowCaptureReferenceOperation: 4 (OperationKind.FlowCaptureReference, Type: System.Int32, IsImplicit) (Syntax: 'If(bLimit,  ... t1, limit2)')
                Leaving: {R5} {R1}

            Next (Regular) Block[B16]
                Leaving: {R5}
    }

    Block[B16] - Block
        Predecessors: [B15]
        Statements (1)
            IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'result = true')
              Expression: 
                ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Boolean, IsImplicit) (Syntax: 'result = true')
                  Left: 
                    IParameterReferenceOperation: result (OperationKind.ParameterReference, Type: System.Boolean) (Syntax: 'result')
                  Right: 
                    ILiteralOperation (OperationKind.Literal, Type: System.Boolean, Constant: True) (Syntax: 'true')

        Next (Regular) Block[B17]
            Entering: {R7} {R8} {R9}

    .locals {R7}
    {
        CaptureIds: [10] [12]
        .locals {R8}
        {
            CaptureIds: [9]
            .locals {R9}
            {
                CaptureIds: [8]
                Block[B17] - Block
                    Predecessors: [B16]
                    Statements (1)
                        IFlowCaptureOperation: 8 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'c1')
                          Value: 
                            IParameterReferenceOperation: c1 (OperationKind.ParameterReference, Type: C, IsImplicit) (Syntax: 'c1')

                    Jump if True (Regular) to Block[B19]
                        IIsNullOperation (OperationKind.IsNull, Type: System.Boolean, IsImplicit) (Syntax: 'c1')
                          Operand: 
                            IFlowCaptureReferenceOperation: 8 (OperationKind.FlowCaptureReference, Type: C, IsImplicit) (Syntax: 'c1')
                        Leaving: {R9}

                    Next (Regular) Block[B18]
                Block[B18] - Block
                    Predecessors: [B17]
                    Statements (1)
                        IFlowCaptureOperation: 9 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'c1')
                          Value: 
                            IFlowCaptureReferenceOperation: 8 (OperationKind.FlowCaptureReference, Type: C, IsImplicit) (Syntax: 'c1')

                    Next (Regular) Block[B20]
                        Leaving: {R9}
            }

            Block[B19] - Block
                Predecessors: [B17]
                Statements (1)
                    IFlowCaptureOperation: 9 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'c2')
                      Value: 
                        IParameterReferenceOperation: c2 (OperationKind.ParameterReference, Type: C, IsImplicit) (Syntax: 'c2')

                Next (Regular) Block[B20]
            Block[B20] - Block
                Predecessors: [B18] [B19]
                Statements (1)
                    IFlowCaptureOperation: 10 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'If(c1, c2).i')
                      Value: 
                        IFieldReferenceOperation: C.i As System.Int32 (OperationKind.FieldReference, Type: System.Int32, IsImplicit) (Syntax: 'If(c1, c2).i')
                          Instance Receiver: 
                            IFlowCaptureReferenceOperation: 9 (OperationKind.FlowCaptureReference, Type: C, IsImplicit) (Syntax: 'If(c1, c2)')

                Next (Regular) Block[B21]
                    Leaving: {R8}
                    Entering: {R10}
        }
        .locals {R10}
        {
            CaptureIds: [11]
            Block[B21] - Block
                Predecessors: [B20]
                Statements (1)
                    IFlowCaptureOperation: 11 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'c1')
                      Value: 
                        IParameterReferenceOperation: c1 (OperationKind.ParameterReference, Type: C, IsImplicit) (Syntax: 'c1')

                Jump if True (Regular) to Block[B23]
                    IIsNullOperation (OperationKind.IsNull, Type: System.Boolean, IsImplicit) (Syntax: 'c1')
                      Operand: 
                        IFlowCaptureReferenceOperation: 11 (OperationKind.FlowCaptureReference, Type: C, IsImplicit) (Syntax: 'c1')
                    Leaving: {R10}

                Next (Regular) Block[B22]
            Block[B22] - Block
                Predecessors: [B21]
                Statements (1)
                    IFlowCaptureOperation: 12 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'c1')
                      Value: 
                        IFlowCaptureReferenceOperation: 11 (OperationKind.FlowCaptureReference, Type: C, IsImplicit) (Syntax: 'c1')

                Next (Regular) Block[B24]
                    Leaving: {R10}
        }

        Block[B23] - Block
            Predecessors: [B21]
            Statements (1)
                IFlowCaptureOperation: 12 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'c2')
                  Value: 
                    IParameterReferenceOperation: c2 (OperationKind.ParameterReference, Type: C, IsImplicit) (Syntax: 'c2')

            Next (Regular) Block[B24]
        Block[B24] - Block
            Predecessors: [B22] [B23]
            Statements (1)
                ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: null, IsImplicit) (Syntax: 'If(c1, c2).i')
                  Left: 
                    IFlowCaptureReferenceOperation: 10 (OperationKind.FlowCaptureReference, Type: System.Int32, IsImplicit) (Syntax: 'If(c1, c2).i')
                  Right: 
                    IBinaryOperation (BinaryOperatorKind.Add) (OperationKind.Binary, Type: System.Int32, IsImplicit) (Syntax: '2')
                      Left: 
                        IFieldReferenceOperation: C.i As System.Int32 (OperationKind.FieldReference, Type: System.Int32, IsImplicit) (Syntax: 'If(c1, c2).i')
                          Instance Receiver: 
                            IFlowCaptureReferenceOperation: 12 (OperationKind.FlowCaptureReference, Type: C, IsImplicit) (Syntax: 'If(c1, c2)')
                      Right: 
                        IFlowCaptureReferenceOperation: 5 (OperationKind.FlowCaptureReference, Type: System.Int32, Constant: 2, IsImplicit) (Syntax: '2')

            Next (Regular) Block[B12]
                Leaving: {R7}
                Entering: {R5} {R6}
    }
}

Block[B25] - Exit
    Predecessors: [B15]
    Statements (0)
]]>.Value

            VerifyFlowGraphAndDiagnosticsForTest(Of MethodBlockSyntax)(source, expectedFlowGraph, expectedDiagnostics, TestOptions.ReleaseDll.WithOverflowChecks(False))
        End Sub

        <CompilerTrait(CompilerFeature.IOperation, CompilerFeature.Dataflow)>
        <Fact>
        Public Sub ForToFlow_15()
            Dim source = <![CDATA[
Imports System
Public Class C
    Sub M(c1 As C, c2 As C, bInit As Boolean, init1 As Integer, init2 As Integer, bLimit As Boolean, limit1 As Integer, limit2 As Integer, result As Boolean) 'BIND:"Sub M"
        For If(c1, c2).i = If(bInit, init1, init2) To If(bLimit, limit1, limit2) Step -2
            result = true
        Next
    End Sub

    Public i As Integer
End Class]]>.Value

            Dim expectedDiagnostics = String.Empty

            Dim expectedFlowGraph = <![CDATA[
Block[B0] - Entry
    Statements (0)
    Next (Regular) Block[B1]
        Entering: {R1} {R2} {R3} {R4}

.locals {R1}
{
    CaptureIds: [4] [5]
    .locals {R2}
    {
        CaptureIds: [2] [3]
        .locals {R3}
        {
            CaptureIds: [1]
            .locals {R4}
            {
                CaptureIds: [0]
                Block[B1] - Block
                    Predecessors: [B0]
                    Statements (1)
                        IFlowCaptureOperation: 0 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'c1')
                          Value: 
                            IParameterReferenceOperation: c1 (OperationKind.ParameterReference, Type: C) (Syntax: 'c1')

                    Jump if True (Regular) to Block[B3]
                        IIsNullOperation (OperationKind.IsNull, Type: System.Boolean, IsImplicit) (Syntax: 'c1')
                          Operand: 
                            IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: C, IsImplicit) (Syntax: 'c1')
                        Leaving: {R4}

                    Next (Regular) Block[B2]
                Block[B2] - Block
                    Predecessors: [B1]
                    Statements (1)
                        IFlowCaptureOperation: 1 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'c1')
                          Value: 
                            IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: C, IsImplicit) (Syntax: 'c1')

                    Next (Regular) Block[B4]
                        Leaving: {R4}
            }

            Block[B3] - Block
                Predecessors: [B1]
                Statements (1)
                    IFlowCaptureOperation: 1 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'c2')
                      Value: 
                        IParameterReferenceOperation: c2 (OperationKind.ParameterReference, Type: C) (Syntax: 'c2')

                Next (Regular) Block[B4]
            Block[B4] - Block
                Predecessors: [B2] [B3]
                Statements (1)
                    IFlowCaptureOperation: 2 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'If(c1, c2).i')
                      Value: 
                        IFieldReferenceOperation: C.i As System.Int32 (OperationKind.FieldReference, Type: System.Int32) (Syntax: 'If(c1, c2).i')
                          Instance Receiver: 
                            IFlowCaptureReferenceOperation: 1 (OperationKind.FlowCaptureReference, Type: C, IsImplicit) (Syntax: 'If(c1, c2)')

                Next (Regular) Block[B5]
                    Leaving: {R3}
        }

        Block[B5] - Block
            Predecessors: [B4]
            Statements (0)
            Jump if False (Regular) to Block[B7]
                IParameterReferenceOperation: bInit (OperationKind.ParameterReference, Type: System.Boolean) (Syntax: 'bInit')

            Next (Regular) Block[B6]
        Block[B6] - Block
            Predecessors: [B5]
            Statements (1)
                IFlowCaptureOperation: 3 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'init1')
                  Value: 
                    IParameterReferenceOperation: init1 (OperationKind.ParameterReference, Type: System.Int32) (Syntax: 'init1')

            Next (Regular) Block[B8]
        Block[B7] - Block
            Predecessors: [B5]
            Statements (1)
                IFlowCaptureOperation: 3 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'init2')
                  Value: 
                    IParameterReferenceOperation: init2 (OperationKind.ParameterReference, Type: System.Int32) (Syntax: 'init2')

            Next (Regular) Block[B8]
        Block[B8] - Block
            Predecessors: [B6] [B7]
            Statements (0)
            Jump if False (Regular) to Block[B10]
                IParameterReferenceOperation: bLimit (OperationKind.ParameterReference, Type: System.Boolean) (Syntax: 'bLimit')

            Next (Regular) Block[B9]
        Block[B9] - Block
            Predecessors: [B8]
            Statements (1)
                IFlowCaptureOperation: 4 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'limit1')
                  Value: 
                    IParameterReferenceOperation: limit1 (OperationKind.ParameterReference, Type: System.Int32) (Syntax: 'limit1')

            Next (Regular) Block[B11]
        Block[B10] - Block
            Predecessors: [B8]
            Statements (1)
                IFlowCaptureOperation: 4 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'limit2')
                  Value: 
                    IParameterReferenceOperation: limit2 (OperationKind.ParameterReference, Type: System.Int32) (Syntax: 'limit2')

            Next (Regular) Block[B11]
        Block[B11] - Block
            Predecessors: [B9] [B10]
            Statements (2)
                IFlowCaptureOperation: 5 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: '-2')
                  Value: 
                    IUnaryOperation (UnaryOperatorKind.Minus, Checked) (OperationKind.Unary, Type: System.Int32, Constant: -2) (Syntax: '-2')
                      Operand: 
                        ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 2) (Syntax: '2')

                ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: null, IsImplicit) (Syntax: 'If(bInit, init1, init2)')
                  Left: 
                    IFlowCaptureReferenceOperation: 2 (OperationKind.FlowCaptureReference, Type: System.Int32, IsImplicit) (Syntax: 'If(c1, c2).i')
                  Right: 
                    IFlowCaptureReferenceOperation: 3 (OperationKind.FlowCaptureReference, Type: System.Int32, IsImplicit) (Syntax: 'If(bInit, init1, init2)')

            Next (Regular) Block[B12]
                Leaving: {R2}
                Entering: {R5} {R6}
    }
    .locals {R5}
    {
        CaptureIds: [7]
        .locals {R6}
        {
            CaptureIds: [6]
            Block[B12] - Block
                Predecessors: [B11] [B24]
                Statements (1)
                    IFlowCaptureOperation: 6 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'c1')
                      Value: 
                        IParameterReferenceOperation: c1 (OperationKind.ParameterReference, Type: C, IsImplicit) (Syntax: 'c1')

                Jump if True (Regular) to Block[B14]
                    IIsNullOperation (OperationKind.IsNull, Type: System.Boolean, IsImplicit) (Syntax: 'c1')
                      Operand: 
                        IFlowCaptureReferenceOperation: 6 (OperationKind.FlowCaptureReference, Type: C, IsImplicit) (Syntax: 'c1')
                    Leaving: {R6}

                Next (Regular) Block[B13]
            Block[B13] - Block
                Predecessors: [B12]
                Statements (1)
                    IFlowCaptureOperation: 7 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'c1')
                      Value: 
                        IFlowCaptureReferenceOperation: 6 (OperationKind.FlowCaptureReference, Type: C, IsImplicit) (Syntax: 'c1')

                Next (Regular) Block[B15]
                    Leaving: {R6}
        }

        Block[B14] - Block
            Predecessors: [B12]
            Statements (1)
                IFlowCaptureOperation: 7 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'c2')
                  Value: 
                    IParameterReferenceOperation: c2 (OperationKind.ParameterReference, Type: C, IsImplicit) (Syntax: 'c2')

            Next (Regular) Block[B15]
        Block[B15] - Block
            Predecessors: [B13] [B14]
            Statements (0)
            Jump if False (Regular) to Block[B25]
                IBinaryOperation (BinaryOperatorKind.GreaterThanOrEqual) (OperationKind.Binary, Type: System.Boolean, IsImplicit) (Syntax: 'If(bLimit,  ... t1, limit2)')
                  Left: 
                    IFieldReferenceOperation: C.i As System.Int32 (OperationKind.FieldReference, Type: System.Int32, IsImplicit) (Syntax: 'If(c1, c2).i')
                      Instance Receiver: 
                        IFlowCaptureReferenceOperation: 7 (OperationKind.FlowCaptureReference, Type: C, IsImplicit) (Syntax: 'If(c1, c2)')
                  Right: 
                    IFlowCaptureReferenceOperation: 4 (OperationKind.FlowCaptureReference, Type: System.Int32, IsImplicit) (Syntax: 'If(bLimit,  ... t1, limit2)')
                Leaving: {R5} {R1}

            Next (Regular) Block[B16]
                Leaving: {R5}
    }

    Block[B16] - Block
        Predecessors: [B15]
        Statements (1)
            IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'result = true')
              Expression: 
                ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Boolean, IsImplicit) (Syntax: 'result = true')
                  Left: 
                    IParameterReferenceOperation: result (OperationKind.ParameterReference, Type: System.Boolean) (Syntax: 'result')
                  Right: 
                    ILiteralOperation (OperationKind.Literal, Type: System.Boolean, Constant: True) (Syntax: 'true')

        Next (Regular) Block[B17]
            Entering: {R7} {R8} {R9}

    .locals {R7}
    {
        CaptureIds: [10] [12]
        .locals {R8}
        {
            CaptureIds: [9]
            .locals {R9}
            {
                CaptureIds: [8]
                Block[B17] - Block
                    Predecessors: [B16]
                    Statements (1)
                        IFlowCaptureOperation: 8 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'c1')
                          Value: 
                            IParameterReferenceOperation: c1 (OperationKind.ParameterReference, Type: C, IsImplicit) (Syntax: 'c1')

                    Jump if True (Regular) to Block[B19]
                        IIsNullOperation (OperationKind.IsNull, Type: System.Boolean, IsImplicit) (Syntax: 'c1')
                          Operand: 
                            IFlowCaptureReferenceOperation: 8 (OperationKind.FlowCaptureReference, Type: C, IsImplicit) (Syntax: 'c1')
                        Leaving: {R9}

                    Next (Regular) Block[B18]
                Block[B18] - Block
                    Predecessors: [B17]
                    Statements (1)
                        IFlowCaptureOperation: 9 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'c1')
                          Value: 
                            IFlowCaptureReferenceOperation: 8 (OperationKind.FlowCaptureReference, Type: C, IsImplicit) (Syntax: 'c1')

                    Next (Regular) Block[B20]
                        Leaving: {R9}
            }

            Block[B19] - Block
                Predecessors: [B17]
                Statements (1)
                    IFlowCaptureOperation: 9 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'c2')
                      Value: 
                        IParameterReferenceOperation: c2 (OperationKind.ParameterReference, Type: C, IsImplicit) (Syntax: 'c2')

                Next (Regular) Block[B20]
            Block[B20] - Block
                Predecessors: [B18] [B19]
                Statements (1)
                    IFlowCaptureOperation: 10 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'If(c1, c2).i')
                      Value: 
                        IFieldReferenceOperation: C.i As System.Int32 (OperationKind.FieldReference, Type: System.Int32, IsImplicit) (Syntax: 'If(c1, c2).i')
                          Instance Receiver: 
                            IFlowCaptureReferenceOperation: 9 (OperationKind.FlowCaptureReference, Type: C, IsImplicit) (Syntax: 'If(c1, c2)')

                Next (Regular) Block[B21]
                    Leaving: {R8}
                    Entering: {R10}
        }
        .locals {R10}
        {
            CaptureIds: [11]
            Block[B21] - Block
                Predecessors: [B20]
                Statements (1)
                    IFlowCaptureOperation: 11 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'c1')
                      Value: 
                        IParameterReferenceOperation: c1 (OperationKind.ParameterReference, Type: C, IsImplicit) (Syntax: 'c1')

                Jump if True (Regular) to Block[B23]
                    IIsNullOperation (OperationKind.IsNull, Type: System.Boolean, IsImplicit) (Syntax: 'c1')
                      Operand: 
                        IFlowCaptureReferenceOperation: 11 (OperationKind.FlowCaptureReference, Type: C, IsImplicit) (Syntax: 'c1')
                    Leaving: {R10}

                Next (Regular) Block[B22]
            Block[B22] - Block
                Predecessors: [B21]
                Statements (1)
                    IFlowCaptureOperation: 12 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'c1')
                      Value: 
                        IFlowCaptureReferenceOperation: 11 (OperationKind.FlowCaptureReference, Type: C, IsImplicit) (Syntax: 'c1')

                Next (Regular) Block[B24]
                    Leaving: {R10}
        }

        Block[B23] - Block
            Predecessors: [B21]
            Statements (1)
                IFlowCaptureOperation: 12 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'c2')
                  Value: 
                    IParameterReferenceOperation: c2 (OperationKind.ParameterReference, Type: C, IsImplicit) (Syntax: 'c2')

            Next (Regular) Block[B24]
        Block[B24] - Block
            Predecessors: [B22] [B23]
            Statements (1)
                ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: null, IsImplicit) (Syntax: 'If(c1, c2).i')
                  Left: 
                    IFlowCaptureReferenceOperation: 10 (OperationKind.FlowCaptureReference, Type: System.Int32, IsImplicit) (Syntax: 'If(c1, c2).i')
                  Right: 
                    IBinaryOperation (BinaryOperatorKind.Add, Checked) (OperationKind.Binary, Type: System.Int32, IsImplicit) (Syntax: '-2')
                      Left: 
                        IFieldReferenceOperation: C.i As System.Int32 (OperationKind.FieldReference, Type: System.Int32, IsImplicit) (Syntax: 'If(c1, c2).i')
                          Instance Receiver: 
                            IFlowCaptureReferenceOperation: 12 (OperationKind.FlowCaptureReference, Type: C, IsImplicit) (Syntax: 'If(c1, c2)')
                      Right: 
                        IFlowCaptureReferenceOperation: 5 (OperationKind.FlowCaptureReference, Type: System.Int32, Constant: -2, IsImplicit) (Syntax: '-2')

            Next (Regular) Block[B12]
                Leaving: {R7}
                Entering: {R5} {R6}
    }
}

Block[B25] - Exit
    Predecessors: [B15]
    Statements (0)
]]>.Value

            VerifyFlowGraphAndDiagnosticsForTest(Of MethodBlockSyntax)(source, expectedFlowGraph, expectedDiagnostics)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation, CompilerFeature.Dataflow)>
        <Fact>
        Public Sub ForToFlow_16()
            Dim source = <![CDATA[
Imports System
Public Class C
    Sub M(c1 As C, c2 As C, bInit As Boolean, init1 As Integer, init2 As Integer, bLimit As Boolean, limit1 As Integer, limit2 As Integer, bStep As Boolean, step1 As Integer, step2 As Integer, result As Boolean) 'BIND:"Sub M"
        For If(c1, c2).i = If(bInit, init1, init2) To If(bLimit, limit1, limit2) Step If(bStep, step1, step2)
            result = true
        Next
    End Sub

    Public i As Integer
End Class]]>.Value

            Dim expectedDiagnostics = String.Empty

            Dim expectedFlowGraph = <![CDATA[
Block[B0] - Entry
    Statements (0)
    Next (Regular) Block[B1]
        Entering: {R1} {R2} {R3} {R4}

.locals {R1}
{
    CaptureIds: [4] [5]
    .locals {R2}
    {
        CaptureIds: [2] [3]
        .locals {R3}
        {
            CaptureIds: [1]
            .locals {R4}
            {
                CaptureIds: [0]
                Block[B1] - Block
                    Predecessors: [B0]
                    Statements (1)
                        IFlowCaptureOperation: 0 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'c1')
                          Value: 
                            IParameterReferenceOperation: c1 (OperationKind.ParameterReference, Type: C) (Syntax: 'c1')

                    Jump if True (Regular) to Block[B3]
                        IIsNullOperation (OperationKind.IsNull, Type: System.Boolean, IsImplicit) (Syntax: 'c1')
                          Operand: 
                            IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: C, IsImplicit) (Syntax: 'c1')
                        Leaving: {R4}

                    Next (Regular) Block[B2]
                Block[B2] - Block
                    Predecessors: [B1]
                    Statements (1)
                        IFlowCaptureOperation: 1 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'c1')
                          Value: 
                            IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: C, IsImplicit) (Syntax: 'c1')

                    Next (Regular) Block[B4]
                        Leaving: {R4}
            }

            Block[B3] - Block
                Predecessors: [B1]
                Statements (1)
                    IFlowCaptureOperation: 1 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'c2')
                      Value: 
                        IParameterReferenceOperation: c2 (OperationKind.ParameterReference, Type: C) (Syntax: 'c2')

                Next (Regular) Block[B4]
            Block[B4] - Block
                Predecessors: [B2] [B3]
                Statements (1)
                    IFlowCaptureOperation: 2 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'If(c1, c2).i')
                      Value: 
                        IFieldReferenceOperation: C.i As System.Int32 (OperationKind.FieldReference, Type: System.Int32) (Syntax: 'If(c1, c2).i')
                          Instance Receiver: 
                            IFlowCaptureReferenceOperation: 1 (OperationKind.FlowCaptureReference, Type: C, IsImplicit) (Syntax: 'If(c1, c2)')

                Next (Regular) Block[B5]
                    Leaving: {R3}
        }

        Block[B5] - Block
            Predecessors: [B4]
            Statements (0)
            Jump if False (Regular) to Block[B7]
                IParameterReferenceOperation: bInit (OperationKind.ParameterReference, Type: System.Boolean) (Syntax: 'bInit')

            Next (Regular) Block[B6]
        Block[B6] - Block
            Predecessors: [B5]
            Statements (1)
                IFlowCaptureOperation: 3 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'init1')
                  Value: 
                    IParameterReferenceOperation: init1 (OperationKind.ParameterReference, Type: System.Int32) (Syntax: 'init1')

            Next (Regular) Block[B8]
        Block[B7] - Block
            Predecessors: [B5]
            Statements (1)
                IFlowCaptureOperation: 3 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'init2')
                  Value: 
                    IParameterReferenceOperation: init2 (OperationKind.ParameterReference, Type: System.Int32) (Syntax: 'init2')

            Next (Regular) Block[B8]
        Block[B8] - Block
            Predecessors: [B6] [B7]
            Statements (0)
            Jump if False (Regular) to Block[B10]
                IParameterReferenceOperation: bLimit (OperationKind.ParameterReference, Type: System.Boolean) (Syntax: 'bLimit')

            Next (Regular) Block[B9]
        Block[B9] - Block
            Predecessors: [B8]
            Statements (1)
                IFlowCaptureOperation: 4 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'limit1')
                  Value: 
                    IParameterReferenceOperation: limit1 (OperationKind.ParameterReference, Type: System.Int32) (Syntax: 'limit1')

            Next (Regular) Block[B11]
        Block[B10] - Block
            Predecessors: [B8]
            Statements (1)
                IFlowCaptureOperation: 4 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'limit2')
                  Value: 
                    IParameterReferenceOperation: limit2 (OperationKind.ParameterReference, Type: System.Int32) (Syntax: 'limit2')

            Next (Regular) Block[B11]
        Block[B11] - Block
            Predecessors: [B9] [B10]
            Statements (0)
            Jump if False (Regular) to Block[B13]
                IParameterReferenceOperation: bStep (OperationKind.ParameterReference, Type: System.Boolean) (Syntax: 'bStep')

            Next (Regular) Block[B12]
        Block[B12] - Block
            Predecessors: [B11]
            Statements (1)
                IFlowCaptureOperation: 5 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'step1')
                  Value: 
                    IParameterReferenceOperation: step1 (OperationKind.ParameterReference, Type: System.Int32) (Syntax: 'step1')

            Next (Regular) Block[B14]
        Block[B13] - Block
            Predecessors: [B11]
            Statements (1)
                IFlowCaptureOperation: 5 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'step2')
                  Value: 
                    IParameterReferenceOperation: step2 (OperationKind.ParameterReference, Type: System.Int32) (Syntax: 'step2')

            Next (Regular) Block[B14]
        Block[B14] - Block
            Predecessors: [B12] [B13]
            Statements (1)
                ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: null, IsImplicit) (Syntax: 'If(bInit, init1, init2)')
                  Left: 
                    IFlowCaptureReferenceOperation: 2 (OperationKind.FlowCaptureReference, Type: System.Int32, IsImplicit) (Syntax: 'If(c1, c2).i')
                  Right: 
                    IFlowCaptureReferenceOperation: 3 (OperationKind.FlowCaptureReference, Type: System.Int32, IsImplicit) (Syntax: 'If(bInit, init1, init2)')

            Next (Regular) Block[B15]
                Leaving: {R2}
                Entering: {R5} {R6}
    }
    .locals {R5}
    {
        CaptureIds: [7]
        .locals {R6}
        {
            CaptureIds: [6]
            Block[B15] - Block
                Predecessors: [B14] [B27]
                Statements (1)
                    IFlowCaptureOperation: 6 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'c1')
                      Value: 
                        IParameterReferenceOperation: c1 (OperationKind.ParameterReference, Type: C, IsImplicit) (Syntax: 'c1')

                Jump if True (Regular) to Block[B17]
                    IIsNullOperation (OperationKind.IsNull, Type: System.Boolean, IsImplicit) (Syntax: 'c1')
                      Operand: 
                        IFlowCaptureReferenceOperation: 6 (OperationKind.FlowCaptureReference, Type: C, IsImplicit) (Syntax: 'c1')
                    Leaving: {R6}

                Next (Regular) Block[B16]
            Block[B16] - Block
                Predecessors: [B15]
                Statements (1)
                    IFlowCaptureOperation: 7 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'c1')
                      Value: 
                        IFlowCaptureReferenceOperation: 6 (OperationKind.FlowCaptureReference, Type: C, IsImplicit) (Syntax: 'c1')

                Next (Regular) Block[B18]
                    Leaving: {R6}
        }

        Block[B17] - Block
            Predecessors: [B15]
            Statements (1)
                IFlowCaptureOperation: 7 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'c2')
                  Value: 
                    IParameterReferenceOperation: c2 (OperationKind.ParameterReference, Type: C, IsImplicit) (Syntax: 'c2')

            Next (Regular) Block[B18]
        Block[B18] - Block
            Predecessors: [B16] [B17]
            Statements (0)
            Jump if False (Regular) to Block[B28]
                IBinaryOperation (BinaryOperatorKind.LessThanOrEqual) (OperationKind.Binary, Type: System.Boolean, IsImplicit) (Syntax: 'If(bLimit,  ... t1, limit2)')
                  Left: 
                    IBinaryOperation (BinaryOperatorKind.ExclusiveOr) (OperationKind.Binary, Type: System.Int32, IsImplicit) (Syntax: 'If(c1, c2).i')
                      Left: 
                        IBinaryOperation (BinaryOperatorKind.RightShift) (OperationKind.Binary, Type: System.Int32, IsImplicit) (Syntax: 'If(c1, c2).i')
                          Left: 
                            IFlowCaptureReferenceOperation: 5 (OperationKind.FlowCaptureReference, Type: System.Int32, IsImplicit) (Syntax: 'If(bStep, step1, step2)')
                          Right: 
                            ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 31, IsImplicit) (Syntax: 'If(c1, c2).i')
                      Right: 
                        IFieldReferenceOperation: C.i As System.Int32 (OperationKind.FieldReference, Type: System.Int32, IsImplicit) (Syntax: 'If(c1, c2).i')
                          Instance Receiver: 
                            IFlowCaptureReferenceOperation: 7 (OperationKind.FlowCaptureReference, Type: C, IsImplicit) (Syntax: 'If(c1, c2)')
                  Right: 
                    IBinaryOperation (BinaryOperatorKind.ExclusiveOr) (OperationKind.Binary, Type: System.Int32, IsImplicit) (Syntax: 'If(bLimit,  ... t1, limit2)')
                      Left: 
                        IBinaryOperation (BinaryOperatorKind.RightShift) (OperationKind.Binary, Type: System.Int32, IsImplicit) (Syntax: 'If(bLimit,  ... t1, limit2)')
                          Left: 
                            IFlowCaptureReferenceOperation: 5 (OperationKind.FlowCaptureReference, Type: System.Int32, IsImplicit) (Syntax: 'If(bStep, step1, step2)')
                          Right: 
                            ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 31, IsImplicit) (Syntax: 'If(bLimit,  ... t1, limit2)')
                      Right: 
                        IFlowCaptureReferenceOperation: 4 (OperationKind.FlowCaptureReference, Type: System.Int32, IsImplicit) (Syntax: 'If(bLimit,  ... t1, limit2)')
                Leaving: {R5} {R1}

            Next (Regular) Block[B19]
                Leaving: {R5}
    }

    Block[B19] - Block
        Predecessors: [B18]
        Statements (1)
            IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'result = true')
              Expression: 
                ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Boolean, IsImplicit) (Syntax: 'result = true')
                  Left: 
                    IParameterReferenceOperation: result (OperationKind.ParameterReference, Type: System.Boolean) (Syntax: 'result')
                  Right: 
                    ILiteralOperation (OperationKind.Literal, Type: System.Boolean, Constant: True) (Syntax: 'true')

        Next (Regular) Block[B20]
            Entering: {R7} {R8} {R9}

    .locals {R7}
    {
        CaptureIds: [10] [12]
        .locals {R8}
        {
            CaptureIds: [9]
            .locals {R9}
            {
                CaptureIds: [8]
                Block[B20] - Block
                    Predecessors: [B19]
                    Statements (1)
                        IFlowCaptureOperation: 8 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'c1')
                          Value: 
                            IParameterReferenceOperation: c1 (OperationKind.ParameterReference, Type: C, IsImplicit) (Syntax: 'c1')

                    Jump if True (Regular) to Block[B22]
                        IIsNullOperation (OperationKind.IsNull, Type: System.Boolean, IsImplicit) (Syntax: 'c1')
                          Operand: 
                            IFlowCaptureReferenceOperation: 8 (OperationKind.FlowCaptureReference, Type: C, IsImplicit) (Syntax: 'c1')
                        Leaving: {R9}

                    Next (Regular) Block[B21]
                Block[B21] - Block
                    Predecessors: [B20]
                    Statements (1)
                        IFlowCaptureOperation: 9 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'c1')
                          Value: 
                            IFlowCaptureReferenceOperation: 8 (OperationKind.FlowCaptureReference, Type: C, IsImplicit) (Syntax: 'c1')

                    Next (Regular) Block[B23]
                        Leaving: {R9}
            }

            Block[B22] - Block
                Predecessors: [B20]
                Statements (1)
                    IFlowCaptureOperation: 9 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'c2')
                      Value: 
                        IParameterReferenceOperation: c2 (OperationKind.ParameterReference, Type: C, IsImplicit) (Syntax: 'c2')

                Next (Regular) Block[B23]
            Block[B23] - Block
                Predecessors: [B21] [B22]
                Statements (1)
                    IFlowCaptureOperation: 10 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'If(c1, c2).i')
                      Value: 
                        IFieldReferenceOperation: C.i As System.Int32 (OperationKind.FieldReference, Type: System.Int32, IsImplicit) (Syntax: 'If(c1, c2).i')
                          Instance Receiver: 
                            IFlowCaptureReferenceOperation: 9 (OperationKind.FlowCaptureReference, Type: C, IsImplicit) (Syntax: 'If(c1, c2)')

                Next (Regular) Block[B24]
                    Leaving: {R8}
                    Entering: {R10}
        }
        .locals {R10}
        {
            CaptureIds: [11]
            Block[B24] - Block
                Predecessors: [B23]
                Statements (1)
                    IFlowCaptureOperation: 11 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'c1')
                      Value: 
                        IParameterReferenceOperation: c1 (OperationKind.ParameterReference, Type: C, IsImplicit) (Syntax: 'c1')

                Jump if True (Regular) to Block[B26]
                    IIsNullOperation (OperationKind.IsNull, Type: System.Boolean, IsImplicit) (Syntax: 'c1')
                      Operand: 
                        IFlowCaptureReferenceOperation: 11 (OperationKind.FlowCaptureReference, Type: C, IsImplicit) (Syntax: 'c1')
                    Leaving: {R10}

                Next (Regular) Block[B25]
            Block[B25] - Block
                Predecessors: [B24]
                Statements (1)
                    IFlowCaptureOperation: 12 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'c1')
                      Value: 
                        IFlowCaptureReferenceOperation: 11 (OperationKind.FlowCaptureReference, Type: C, IsImplicit) (Syntax: 'c1')

                Next (Regular) Block[B27]
                    Leaving: {R10}
        }

        Block[B26] - Block
            Predecessors: [B24]
            Statements (1)
                IFlowCaptureOperation: 12 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'c2')
                  Value: 
                    IParameterReferenceOperation: c2 (OperationKind.ParameterReference, Type: C, IsImplicit) (Syntax: 'c2')

            Next (Regular) Block[B27]
        Block[B27] - Block
            Predecessors: [B25] [B26]
            Statements (1)
                ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: null, IsImplicit) (Syntax: 'If(c1, c2).i')
                  Left: 
                    IFlowCaptureReferenceOperation: 10 (OperationKind.FlowCaptureReference, Type: System.Int32, IsImplicit) (Syntax: 'If(c1, c2).i')
                  Right: 
                    IBinaryOperation (BinaryOperatorKind.Add, Checked) (OperationKind.Binary, Type: System.Int32, IsImplicit) (Syntax: 'If(bStep, step1, step2)')
                      Left: 
                        IFieldReferenceOperation: C.i As System.Int32 (OperationKind.FieldReference, Type: System.Int32, IsImplicit) (Syntax: 'If(c1, c2).i')
                          Instance Receiver: 
                            IFlowCaptureReferenceOperation: 12 (OperationKind.FlowCaptureReference, Type: C, IsImplicit) (Syntax: 'If(c1, c2)')
                      Right: 
                        IFlowCaptureReferenceOperation: 5 (OperationKind.FlowCaptureReference, Type: System.Int32, IsImplicit) (Syntax: 'If(bStep, step1, step2)')

            Next (Regular) Block[B15]
                Leaving: {R7}
                Entering: {R5} {R6}
    }
}

Block[B28] - Exit
    Predecessors: [B18]
    Statements (0)
]]>.Value

            VerifyFlowGraphAndDiagnosticsForTest(Of MethodBlockSyntax)(source, expectedFlowGraph, expectedDiagnostics)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation, CompilerFeature.Dataflow)>
        <Fact>
        Public Sub ForToFlow_17()
            Dim source = <![CDATA[
Imports System
Public Class C
    Sub M(c1 As C, c2 As C, bInit As Boolean, init1 As Double, init2 As Double, bLimit As Boolean, limit1 As Double, limit2 As Double, bStep As Boolean, step1 As Double, step2 As Double, result As Boolean) 'BIND:"Sub M"
        For If(c1, c2).i = If(bInit, init1, init2) To If(bLimit, limit1, limit2) Step If(bStep, step1, step2)
            result = true
        Next
    End Sub

    Public i As Double
End Class]]>.Value

            Dim expectedDiagnostics = String.Empty

            Dim expectedFlowGraph = <![CDATA[
Block[B0] - Entry
    Statements (0)
    Next (Regular) Block[B1]
        Entering: {R1} {R2} {R3} {R4}

.locals {R1}
{
    CaptureIds: [4] [5] [6]
    .locals {R2}
    {
        CaptureIds: [2] [3]
        .locals {R3}
        {
            CaptureIds: [1]
            .locals {R4}
            {
                CaptureIds: [0]
                Block[B1] - Block
                    Predecessors: [B0]
                    Statements (1)
                        IFlowCaptureOperation: 0 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'c1')
                          Value: 
                            IParameterReferenceOperation: c1 (OperationKind.ParameterReference, Type: C) (Syntax: 'c1')

                    Jump if True (Regular) to Block[B3]
                        IIsNullOperation (OperationKind.IsNull, Type: System.Boolean, IsImplicit) (Syntax: 'c1')
                          Operand: 
                            IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: C, IsImplicit) (Syntax: 'c1')
                        Leaving: {R4}

                    Next (Regular) Block[B2]
                Block[B2] - Block
                    Predecessors: [B1]
                    Statements (1)
                        IFlowCaptureOperation: 1 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'c1')
                          Value: 
                            IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: C, IsImplicit) (Syntax: 'c1')

                    Next (Regular) Block[B4]
                        Leaving: {R4}
            }

            Block[B3] - Block
                Predecessors: [B1]
                Statements (1)
                    IFlowCaptureOperation: 1 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'c2')
                      Value: 
                        IParameterReferenceOperation: c2 (OperationKind.ParameterReference, Type: C) (Syntax: 'c2')

                Next (Regular) Block[B4]
            Block[B4] - Block
                Predecessors: [B2] [B3]
                Statements (1)
                    IFlowCaptureOperation: 2 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'If(c1, c2).i')
                      Value: 
                        IFieldReferenceOperation: C.i As System.Double (OperationKind.FieldReference, Type: System.Double) (Syntax: 'If(c1, c2).i')
                          Instance Receiver: 
                            IFlowCaptureReferenceOperation: 1 (OperationKind.FlowCaptureReference, Type: C, IsImplicit) (Syntax: 'If(c1, c2)')

                Next (Regular) Block[B5]
                    Leaving: {R3}
        }

        Block[B5] - Block
            Predecessors: [B4]
            Statements (0)
            Jump if False (Regular) to Block[B7]
                IParameterReferenceOperation: bInit (OperationKind.ParameterReference, Type: System.Boolean) (Syntax: 'bInit')

            Next (Regular) Block[B6]
        Block[B6] - Block
            Predecessors: [B5]
            Statements (1)
                IFlowCaptureOperation: 3 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'init1')
                  Value: 
                    IParameterReferenceOperation: init1 (OperationKind.ParameterReference, Type: System.Double) (Syntax: 'init1')

            Next (Regular) Block[B8]
        Block[B7] - Block
            Predecessors: [B5]
            Statements (1)
                IFlowCaptureOperation: 3 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'init2')
                  Value: 
                    IParameterReferenceOperation: init2 (OperationKind.ParameterReference, Type: System.Double) (Syntax: 'init2')

            Next (Regular) Block[B8]
        Block[B8] - Block
            Predecessors: [B6] [B7]
            Statements (0)
            Jump if False (Regular) to Block[B10]
                IParameterReferenceOperation: bLimit (OperationKind.ParameterReference, Type: System.Boolean) (Syntax: 'bLimit')

            Next (Regular) Block[B9]
        Block[B9] - Block
            Predecessors: [B8]
            Statements (1)
                IFlowCaptureOperation: 4 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'limit1')
                  Value: 
                    IParameterReferenceOperation: limit1 (OperationKind.ParameterReference, Type: System.Double) (Syntax: 'limit1')

            Next (Regular) Block[B11]
        Block[B10] - Block
            Predecessors: [B8]
            Statements (1)
                IFlowCaptureOperation: 4 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'limit2')
                  Value: 
                    IParameterReferenceOperation: limit2 (OperationKind.ParameterReference, Type: System.Double) (Syntax: 'limit2')

            Next (Regular) Block[B11]
        Block[B11] - Block
            Predecessors: [B9] [B10]
            Statements (0)
            Jump if False (Regular) to Block[B13]
                IParameterReferenceOperation: bStep (OperationKind.ParameterReference, Type: System.Boolean) (Syntax: 'bStep')

            Next (Regular) Block[B12]
        Block[B12] - Block
            Predecessors: [B11]
            Statements (1)
                IFlowCaptureOperation: 5 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'step1')
                  Value: 
                    IParameterReferenceOperation: step1 (OperationKind.ParameterReference, Type: System.Double) (Syntax: 'step1')

            Next (Regular) Block[B14]
        Block[B13] - Block
            Predecessors: [B11]
            Statements (1)
                IFlowCaptureOperation: 5 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'step2')
                  Value: 
                    IParameterReferenceOperation: step2 (OperationKind.ParameterReference, Type: System.Double) (Syntax: 'step2')

            Next (Regular) Block[B14]
        Block[B14] - Block
            Predecessors: [B12] [B13]
            Statements (2)
                IFlowCaptureOperation: 6 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'If(bStep, step1, step2)')
                  Value: 
                    IBinaryOperation (BinaryOperatorKind.GreaterThanOrEqual) (OperationKind.Binary, Type: System.Boolean, IsImplicit) (Syntax: 'If(bStep, step1, step2)')
                      Left: 
                        IFlowCaptureReferenceOperation: 5 (OperationKind.FlowCaptureReference, Type: System.Double, IsImplicit) (Syntax: 'If(bStep, step1, step2)')
                      Right: 
                        ILiteralOperation (OperationKind.Literal, Type: System.Double, Constant: 0, IsImplicit) (Syntax: 'If(bStep, step1, step2)')

                ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: null, IsImplicit) (Syntax: 'If(bInit, init1, init2)')
                  Left: 
                    IFlowCaptureReferenceOperation: 2 (OperationKind.FlowCaptureReference, Type: System.Double, IsImplicit) (Syntax: 'If(c1, c2).i')
                  Right: 
                    IFlowCaptureReferenceOperation: 3 (OperationKind.FlowCaptureReference, Type: System.Double, IsImplicit) (Syntax: 'If(bInit, init1, init2)')

            Next (Regular) Block[B15]
                Leaving: {R2}
                Entering: {R5} {R6} {R7}
    }
    .locals {R5}
    {
        CaptureIds: [9]
        .locals {R6}
        {
            CaptureIds: [8]
            .locals {R7}
            {
                CaptureIds: [7]
                Block[B15] - Block
                    Predecessors: [B14] [B30]
                    Statements (1)
                        IFlowCaptureOperation: 7 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'c1')
                          Value: 
                            IParameterReferenceOperation: c1 (OperationKind.ParameterReference, Type: C, IsImplicit) (Syntax: 'c1')

                    Jump if True (Regular) to Block[B17]
                        IIsNullOperation (OperationKind.IsNull, Type: System.Boolean, IsImplicit) (Syntax: 'c1')
                          Operand: 
                            IFlowCaptureReferenceOperation: 7 (OperationKind.FlowCaptureReference, Type: C, IsImplicit) (Syntax: 'c1')
                        Leaving: {R7}

                    Next (Regular) Block[B16]
                Block[B16] - Block
                    Predecessors: [B15]
                    Statements (1)
                        IFlowCaptureOperation: 8 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'c1')
                          Value: 
                            IFlowCaptureReferenceOperation: 7 (OperationKind.FlowCaptureReference, Type: C, IsImplicit) (Syntax: 'c1')

                    Next (Regular) Block[B18]
                        Leaving: {R7}
            }

            Block[B17] - Block
                Predecessors: [B15]
                Statements (1)
                    IFlowCaptureOperation: 8 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'c2')
                      Value: 
                        IParameterReferenceOperation: c2 (OperationKind.ParameterReference, Type: C, IsImplicit) (Syntax: 'c2')

                Next (Regular) Block[B18]
            Block[B18] - Block
                Predecessors: [B16] [B17]
                Statements (1)
                    IFlowCaptureOperation: 9 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'If(c1, c2).i')
                      Value: 
                        IFieldReferenceOperation: C.i As System.Double (OperationKind.FieldReference, Type: System.Double, IsImplicit) (Syntax: 'If(c1, c2).i')
                          Instance Receiver: 
                            IFlowCaptureReferenceOperation: 8 (OperationKind.FlowCaptureReference, Type: C, IsImplicit) (Syntax: 'If(c1, c2)')

                Next (Regular) Block[B19]
                    Leaving: {R6}
        }

        Block[B19] - Block
            Predecessors: [B18]
            Statements (0)
            Jump if False (Regular) to Block[B21]
                IFlowCaptureReferenceOperation: 6 (OperationKind.FlowCaptureReference, Type: System.Boolean, IsImplicit) (Syntax: 'If(bStep, step1, step2)')

            Next (Regular) Block[B20]
        Block[B20] - Block
            Predecessors: [B19]
            Statements (0)
            Jump if False (Regular) to Block[B31]
                IBinaryOperation (BinaryOperatorKind.LessThanOrEqual) (OperationKind.Binary, Type: System.Boolean, IsImplicit) (Syntax: 'If(bLimit,  ... t1, limit2)')
                  Left: 
                    IFlowCaptureReferenceOperation: 9 (OperationKind.FlowCaptureReference, Type: System.Double, IsImplicit) (Syntax: 'If(c1, c2).i')
                  Right: 
                    IFlowCaptureReferenceOperation: 4 (OperationKind.FlowCaptureReference, Type: System.Double, IsImplicit) (Syntax: 'If(bLimit,  ... t1, limit2)')
                Leaving: {R5} {R1}

            Next (Regular) Block[B22]
                Leaving: {R5}
        Block[B21] - Block
            Predecessors: [B19]
            Statements (0)
            Jump if False (Regular) to Block[B31]
                IBinaryOperation (BinaryOperatorKind.GreaterThanOrEqual) (OperationKind.Binary, Type: System.Boolean, IsImplicit) (Syntax: 'If(bLimit,  ... t1, limit2)')
                  Left: 
                    IFlowCaptureReferenceOperation: 9 (OperationKind.FlowCaptureReference, Type: System.Double, IsImplicit) (Syntax: 'If(c1, c2).i')
                  Right: 
                    IFlowCaptureReferenceOperation: 4 (OperationKind.FlowCaptureReference, Type: System.Double, IsImplicit) (Syntax: 'If(bLimit,  ... t1, limit2)')
                Leaving: {R5} {R1}

            Next (Regular) Block[B22]
                Leaving: {R5}
    }

    Block[B22] - Block
        Predecessors: [B20] [B21]
        Statements (1)
            IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'result = true')
              Expression: 
                ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Boolean, IsImplicit) (Syntax: 'result = true')
                  Left: 
                    IParameterReferenceOperation: result (OperationKind.ParameterReference, Type: System.Boolean) (Syntax: 'result')
                  Right: 
                    ILiteralOperation (OperationKind.Literal, Type: System.Boolean, Constant: True) (Syntax: 'true')

        Next (Regular) Block[B23]
            Entering: {R8} {R9} {R10}

    .locals {R8}
    {
        CaptureIds: [12] [14]
        .locals {R9}
        {
            CaptureIds: [11]
            .locals {R10}
            {
                CaptureIds: [10]
                Block[B23] - Block
                    Predecessors: [B22]
                    Statements (1)
                        IFlowCaptureOperation: 10 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'c1')
                          Value: 
                            IParameterReferenceOperation: c1 (OperationKind.ParameterReference, Type: C, IsImplicit) (Syntax: 'c1')

                    Jump if True (Regular) to Block[B25]
                        IIsNullOperation (OperationKind.IsNull, Type: System.Boolean, IsImplicit) (Syntax: 'c1')
                          Operand: 
                            IFlowCaptureReferenceOperation: 10 (OperationKind.FlowCaptureReference, Type: C, IsImplicit) (Syntax: 'c1')
                        Leaving: {R10}

                    Next (Regular) Block[B24]
                Block[B24] - Block
                    Predecessors: [B23]
                    Statements (1)
                        IFlowCaptureOperation: 11 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'c1')
                          Value: 
                            IFlowCaptureReferenceOperation: 10 (OperationKind.FlowCaptureReference, Type: C, IsImplicit) (Syntax: 'c1')

                    Next (Regular) Block[B26]
                        Leaving: {R10}
            }

            Block[B25] - Block
                Predecessors: [B23]
                Statements (1)
                    IFlowCaptureOperation: 11 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'c2')
                      Value: 
                        IParameterReferenceOperation: c2 (OperationKind.ParameterReference, Type: C, IsImplicit) (Syntax: 'c2')

                Next (Regular) Block[B26]
            Block[B26] - Block
                Predecessors: [B24] [B25]
                Statements (1)
                    IFlowCaptureOperation: 12 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'If(c1, c2).i')
                      Value: 
                        IFieldReferenceOperation: C.i As System.Double (OperationKind.FieldReference, Type: System.Double, IsImplicit) (Syntax: 'If(c1, c2).i')
                          Instance Receiver: 
                            IFlowCaptureReferenceOperation: 11 (OperationKind.FlowCaptureReference, Type: C, IsImplicit) (Syntax: 'If(c1, c2)')

                Next (Regular) Block[B27]
                    Leaving: {R9}
                    Entering: {R11}
        }
        .locals {R11}
        {
            CaptureIds: [13]
            Block[B27] - Block
                Predecessors: [B26]
                Statements (1)
                    IFlowCaptureOperation: 13 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'c1')
                      Value: 
                        IParameterReferenceOperation: c1 (OperationKind.ParameterReference, Type: C, IsImplicit) (Syntax: 'c1')

                Jump if True (Regular) to Block[B29]
                    IIsNullOperation (OperationKind.IsNull, Type: System.Boolean, IsImplicit) (Syntax: 'c1')
                      Operand: 
                        IFlowCaptureReferenceOperation: 13 (OperationKind.FlowCaptureReference, Type: C, IsImplicit) (Syntax: 'c1')
                    Leaving: {R11}

                Next (Regular) Block[B28]
            Block[B28] - Block
                Predecessors: [B27]
                Statements (1)
                    IFlowCaptureOperation: 14 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'c1')
                      Value: 
                        IFlowCaptureReferenceOperation: 13 (OperationKind.FlowCaptureReference, Type: C, IsImplicit) (Syntax: 'c1')

                Next (Regular) Block[B30]
                    Leaving: {R11}
        }

        Block[B29] - Block
            Predecessors: [B27]
            Statements (1)
                IFlowCaptureOperation: 14 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'c2')
                  Value: 
                    IParameterReferenceOperation: c2 (OperationKind.ParameterReference, Type: C, IsImplicit) (Syntax: 'c2')

            Next (Regular) Block[B30]
        Block[B30] - Block
            Predecessors: [B28] [B29]
            Statements (1)
                ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: null, IsImplicit) (Syntax: 'If(c1, c2).i')
                  Left: 
                    IFlowCaptureReferenceOperation: 12 (OperationKind.FlowCaptureReference, Type: System.Double, IsImplicit) (Syntax: 'If(c1, c2).i')
                  Right: 
                    IBinaryOperation (BinaryOperatorKind.Add, Checked) (OperationKind.Binary, Type: System.Double, IsImplicit) (Syntax: 'If(bStep, step1, step2)')
                      Left: 
                        IFieldReferenceOperation: C.i As System.Double (OperationKind.FieldReference, Type: System.Double, IsImplicit) (Syntax: 'If(c1, c2).i')
                          Instance Receiver: 
                            IFlowCaptureReferenceOperation: 14 (OperationKind.FlowCaptureReference, Type: C, IsImplicit) (Syntax: 'If(c1, c2)')
                      Right: 
                        IFlowCaptureReferenceOperation: 5 (OperationKind.FlowCaptureReference, Type: System.Double, IsImplicit) (Syntax: 'If(bStep, step1, step2)')

            Next (Regular) Block[B15]
                Leaving: {R8}
                Entering: {R5} {R6} {R7}
    }
}

Block[B31] - Exit
    Predecessors: [B20] [B21]
    Statements (0)
]]>.Value

            VerifyFlowGraphAndDiagnosticsForTest(Of MethodBlockSyntax)(source, expectedFlowGraph, expectedDiagnostics)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation, CompilerFeature.Dataflow)>
        <Fact>
        Public Sub ForToFlow_18()
            Dim source = <![CDATA[
Imports System
Public Class C
    Sub M(c1 As C, c2 As C, bInit As Boolean, init1 As Double?, init2 As Double?, bLimit As Boolean, limit1 As Double?, limit2 As Double?, bStep As Boolean, step1 As Double?, step2 As Double?, result As Boolean) 'BIND:"Sub M"
        For If(c1, c2).i = If(bInit, init1, init2) To If(bLimit, limit1, limit2) Step If(bStep, step1, step2)
            result = true
        Next
    End Sub

    Public i As Double?
End Class]]>.Value

            Dim expectedDiagnostics = String.Empty

            Dim expectedFlowGraph = <![CDATA[
Block[B0] - Entry
    Statements (0)
    Next (Regular) Block[B1]
        Entering: {R1} {R2} {R3} {R4}

.locals {R1}
{
    CaptureIds: [4] [5] [6]
    .locals {R2}
    {
        CaptureIds: [2] [3]
        .locals {R3}
        {
            CaptureIds: [1]
            .locals {R4}
            {
                CaptureIds: [0]
                Block[B1] - Block
                    Predecessors: [B0]
                    Statements (1)
                        IFlowCaptureOperation: 0 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'c1')
                          Value: 
                            IParameterReferenceOperation: c1 (OperationKind.ParameterReference, Type: C) (Syntax: 'c1')

                    Jump if True (Regular) to Block[B3]
                        IIsNullOperation (OperationKind.IsNull, Type: System.Boolean, IsImplicit) (Syntax: 'c1')
                          Operand: 
                            IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: C, IsImplicit) (Syntax: 'c1')
                        Leaving: {R4}

                    Next (Regular) Block[B2]
                Block[B2] - Block
                    Predecessors: [B1]
                    Statements (1)
                        IFlowCaptureOperation: 1 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'c1')
                          Value: 
                            IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: C, IsImplicit) (Syntax: 'c1')

                    Next (Regular) Block[B4]
                        Leaving: {R4}
            }

            Block[B3] - Block
                Predecessors: [B1]
                Statements (1)
                    IFlowCaptureOperation: 1 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'c2')
                      Value: 
                        IParameterReferenceOperation: c2 (OperationKind.ParameterReference, Type: C) (Syntax: 'c2')

                Next (Regular) Block[B4]
            Block[B4] - Block
                Predecessors: [B2] [B3]
                Statements (1)
                    IFlowCaptureOperation: 2 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'If(c1, c2).i')
                      Value: 
                        IFieldReferenceOperation: C.i As System.Nullable(Of System.Double) (OperationKind.FieldReference, Type: System.Nullable(Of System.Double)) (Syntax: 'If(c1, c2).i')
                          Instance Receiver: 
                            IFlowCaptureReferenceOperation: 1 (OperationKind.FlowCaptureReference, Type: C, IsImplicit) (Syntax: 'If(c1, c2)')

                Next (Regular) Block[B5]
                    Leaving: {R3}
        }

        Block[B5] - Block
            Predecessors: [B4]
            Statements (0)
            Jump if False (Regular) to Block[B7]
                IParameterReferenceOperation: bInit (OperationKind.ParameterReference, Type: System.Boolean) (Syntax: 'bInit')

            Next (Regular) Block[B6]
        Block[B6] - Block
            Predecessors: [B5]
            Statements (1)
                IFlowCaptureOperation: 3 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'init1')
                  Value: 
                    IParameterReferenceOperation: init1 (OperationKind.ParameterReference, Type: System.Nullable(Of System.Double)) (Syntax: 'init1')

            Next (Regular) Block[B8]
        Block[B7] - Block
            Predecessors: [B5]
            Statements (1)
                IFlowCaptureOperation: 3 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'init2')
                  Value: 
                    IParameterReferenceOperation: init2 (OperationKind.ParameterReference, Type: System.Nullable(Of System.Double)) (Syntax: 'init2')

            Next (Regular) Block[B8]
        Block[B8] - Block
            Predecessors: [B6] [B7]
            Statements (0)
            Jump if False (Regular) to Block[B10]
                IParameterReferenceOperation: bLimit (OperationKind.ParameterReference, Type: System.Boolean) (Syntax: 'bLimit')

            Next (Regular) Block[B9]
        Block[B9] - Block
            Predecessors: [B8]
            Statements (1)
                IFlowCaptureOperation: 4 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'limit1')
                  Value: 
                    IParameterReferenceOperation: limit1 (OperationKind.ParameterReference, Type: System.Nullable(Of System.Double)) (Syntax: 'limit1')

            Next (Regular) Block[B11]
        Block[B10] - Block
            Predecessors: [B8]
            Statements (1)
                IFlowCaptureOperation: 4 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'limit2')
                  Value: 
                    IParameterReferenceOperation: limit2 (OperationKind.ParameterReference, Type: System.Nullable(Of System.Double)) (Syntax: 'limit2')

            Next (Regular) Block[B11]
        Block[B11] - Block
            Predecessors: [B9] [B10]
            Statements (0)
            Jump if False (Regular) to Block[B13]
                IParameterReferenceOperation: bStep (OperationKind.ParameterReference, Type: System.Boolean) (Syntax: 'bStep')

            Next (Regular) Block[B12]
        Block[B12] - Block
            Predecessors: [B11]
            Statements (1)
                IFlowCaptureOperation: 5 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'step1')
                  Value: 
                    IParameterReferenceOperation: step1 (OperationKind.ParameterReference, Type: System.Nullable(Of System.Double)) (Syntax: 'step1')

            Next (Regular) Block[B14]
        Block[B13] - Block
            Predecessors: [B11]
            Statements (1)
                IFlowCaptureOperation: 5 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'step2')
                  Value: 
                    IParameterReferenceOperation: step2 (OperationKind.ParameterReference, Type: System.Nullable(Of System.Double)) (Syntax: 'step2')

            Next (Regular) Block[B14]
        Block[B14] - Block
            Predecessors: [B12] [B13]
            Statements (0)
            Jump if False (Regular) to Block[B16]
                IIsNullOperation (OperationKind.IsNull, Type: System.Boolean, IsImplicit) (Syntax: 'If(bStep, step1, step2)')
                  Operand: 
                    IFlowCaptureReferenceOperation: 5 (OperationKind.FlowCaptureReference, Type: System.Nullable(Of System.Double), IsImplicit) (Syntax: 'If(bStep, step1, step2)')

            Next (Regular) Block[B15]
        Block[B15] - Block
            Predecessors: [B14]
            Statements (1)
                IFlowCaptureOperation: 6 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'If(bStep, step1, step2)')
                  Value: 
                    ILiteralOperation (OperationKind.Literal, Type: System.Boolean, Constant: False, IsImplicit) (Syntax: 'If(bStep, step1, step2)')

            Next (Regular) Block[B17]
        Block[B16] - Block
            Predecessors: [B14]
            Statements (1)
                IFlowCaptureOperation: 6 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'If(bStep, step1, step2)')
                  Value: 
                    IBinaryOperation (BinaryOperatorKind.GreaterThanOrEqual) (OperationKind.Binary, Type: System.Boolean, IsImplicit) (Syntax: 'If(bStep, step1, step2)')
                      Left: 
                        IInvocationOperation ( Function System.Nullable(Of System.Double).GetValueOrDefault() As System.Double) (OperationKind.Invocation, Type: System.Double, IsImplicit) (Syntax: 'If(bStep, step1, step2)')
                          Instance Receiver: 
                            IFlowCaptureReferenceOperation: 5 (OperationKind.FlowCaptureReference, Type: System.Nullable(Of System.Double), IsImplicit) (Syntax: 'If(bStep, step1, step2)')
                          Arguments(0)
                      Right: 
                        ILiteralOperation (OperationKind.Literal, Type: System.Double, Constant: 0, IsImplicit) (Syntax: 'If(bStep, step1, step2)')

            Next (Regular) Block[B17]
        Block[B17] - Block
            Predecessors: [B15] [B16]
            Statements (1)
                ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: null, IsImplicit) (Syntax: 'If(bInit, init1, init2)')
                  Left: 
                    IFlowCaptureReferenceOperation: 2 (OperationKind.FlowCaptureReference, Type: System.Nullable(Of System.Double), IsImplicit) (Syntax: 'If(c1, c2).i')
                  Right: 
                    IFlowCaptureReferenceOperation: 3 (OperationKind.FlowCaptureReference, Type: System.Nullable(Of System.Double), IsImplicit) (Syntax: 'If(bInit, init1, init2)')

            Next (Regular) Block[B18]
                Leaving: {R2}
                Entering: {R5} {R6}
    }
    .locals {R5}
    {
        CaptureIds: [8]
        .locals {R6}
        {
            CaptureIds: [7]
            Block[B18] - Block
                Predecessors: [B17] [B38] [B42]
                Statements (1)
                    IFlowCaptureOperation: 7 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'c1')
                      Value: 
                        IParameterReferenceOperation: c1 (OperationKind.ParameterReference, Type: C, IsImplicit) (Syntax: 'c1')

                Jump if True (Regular) to Block[B20]
                    IIsNullOperation (OperationKind.IsNull, Type: System.Boolean, IsImplicit) (Syntax: 'c1')
                      Operand: 
                        IFlowCaptureReferenceOperation: 7 (OperationKind.FlowCaptureReference, Type: C, IsImplicit) (Syntax: 'c1')
                    Leaving: {R6}

                Next (Regular) Block[B19]
            Block[B19] - Block
                Predecessors: [B18]
                Statements (1)
                    IFlowCaptureOperation: 8 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'c1')
                      Value: 
                        IFlowCaptureReferenceOperation: 7 (OperationKind.FlowCaptureReference, Type: C, IsImplicit) (Syntax: 'c1')

                Next (Regular) Block[B21]
                    Leaving: {R6}
        }

        Block[B20] - Block
            Predecessors: [B18]
            Statements (1)
                IFlowCaptureOperation: 8 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'c2')
                  Value: 
                    IParameterReferenceOperation: c2 (OperationKind.ParameterReference, Type: C, IsImplicit) (Syntax: 'c2')

            Next (Regular) Block[B21]
        Block[B21] - Block
            Predecessors: [B19] [B20]
            Statements (0)
            Jump if False (Regular) to Block[B22]
                IBinaryOperation (BinaryOperatorKind.Or) (OperationKind.Binary, Type: System.Boolean, IsImplicit) (Syntax: 'If(bStep, step1, step2)')
                  Left: 
                    IIsNullOperation (OperationKind.IsNull, Type: System.Boolean, IsImplicit) (Syntax: 'If(bLimit,  ... t1, limit2)')
                      Operand: 
                        IFlowCaptureReferenceOperation: 4 (OperationKind.FlowCaptureReference, Type: System.Nullable(Of System.Double), IsImplicit) (Syntax: 'If(bLimit,  ... t1, limit2)')
                  Right: 
                    IIsNullOperation (OperationKind.IsNull, Type: System.Boolean, IsImplicit) (Syntax: 'If(c1, c2).i')
                      Operand: 
                        IFieldReferenceOperation: C.i As System.Nullable(Of System.Double) (OperationKind.FieldReference, Type: System.Nullable(Of System.Double), IsImplicit) (Syntax: 'If(c1, c2).i')
                          Instance Receiver: 
                            IFlowCaptureReferenceOperation: 8 (OperationKind.FlowCaptureReference, Type: C, IsImplicit) (Syntax: 'If(c1, c2)')
                Leaving: {R5}
                Entering: {R7} {R8} {R9}

            Next (Regular) Block[B43]
                Leaving: {R5} {R1}
    }
    .locals {R7}
    {
        CaptureIds: [11]
        .locals {R8}
        {
            CaptureIds: [10]
            .locals {R9}
            {
                CaptureIds: [9]
                Block[B22] - Block
                    Predecessors: [B21]
                    Statements (1)
                        IFlowCaptureOperation: 9 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'c1')
                          Value: 
                            IParameterReferenceOperation: c1 (OperationKind.ParameterReference, Type: C, IsImplicit) (Syntax: 'c1')

                    Jump if True (Regular) to Block[B24]
                        IIsNullOperation (OperationKind.IsNull, Type: System.Boolean, IsImplicit) (Syntax: 'c1')
                          Operand: 
                            IFlowCaptureReferenceOperation: 9 (OperationKind.FlowCaptureReference, Type: C, IsImplicit) (Syntax: 'c1')
                        Leaving: {R9}

                    Next (Regular) Block[B23]
                Block[B23] - Block
                    Predecessors: [B22]
                    Statements (1)
                        IFlowCaptureOperation: 10 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'c1')
                          Value: 
                            IFlowCaptureReferenceOperation: 9 (OperationKind.FlowCaptureReference, Type: C, IsImplicit) (Syntax: 'c1')

                    Next (Regular) Block[B25]
                        Leaving: {R9}
            }

            Block[B24] - Block
                Predecessors: [B22]
                Statements (1)
                    IFlowCaptureOperation: 10 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'c2')
                      Value: 
                        IParameterReferenceOperation: c2 (OperationKind.ParameterReference, Type: C, IsImplicit) (Syntax: 'c2')

                Next (Regular) Block[B25]
            Block[B25] - Block
                Predecessors: [B23] [B24]
                Statements (1)
                    IFlowCaptureOperation: 11 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'If(c1, c2).i')
                      Value: 
                        IInvocationOperation ( Function System.Nullable(Of System.Double).GetValueOrDefault() As System.Double) (OperationKind.Invocation, Type: System.Double, IsImplicit) (Syntax: 'If(c1, c2).i')
                          Instance Receiver: 
                            IFieldReferenceOperation: C.i As System.Nullable(Of System.Double) (OperationKind.FieldReference, Type: System.Nullable(Of System.Double), IsImplicit) (Syntax: 'If(c1, c2).i')
                              Instance Receiver: 
                                IFlowCaptureReferenceOperation: 10 (OperationKind.FlowCaptureReference, Type: C, IsImplicit) (Syntax: 'If(c1, c2)')
                          Arguments(0)

                Next (Regular) Block[B26]
                    Leaving: {R8}
        }

        Block[B26] - Block
            Predecessors: [B25]
            Statements (0)
            Jump if False (Regular) to Block[B28]
                IFlowCaptureReferenceOperation: 6 (OperationKind.FlowCaptureReference, Type: System.Boolean, IsImplicit) (Syntax: 'If(bStep, step1, step2)')

            Next (Regular) Block[B27]
        Block[B27] - Block
            Predecessors: [B26]
            Statements (0)
            Jump if False (Regular) to Block[B43]
                IBinaryOperation (BinaryOperatorKind.LessThanOrEqual) (OperationKind.Binary, Type: System.Boolean, IsImplicit) (Syntax: 'If(bLimit,  ... t1, limit2)')
                  Left: 
                    IFlowCaptureReferenceOperation: 11 (OperationKind.FlowCaptureReference, Type: System.Double, IsImplicit) (Syntax: 'If(c1, c2).i')
                  Right: 
                    IInvocationOperation ( Function System.Nullable(Of System.Double).GetValueOrDefault() As System.Double) (OperationKind.Invocation, Type: System.Double, IsImplicit) (Syntax: 'If(bLimit,  ... t1, limit2)')
                      Instance Receiver: 
                        IFlowCaptureReferenceOperation: 4 (OperationKind.FlowCaptureReference, Type: System.Nullable(Of System.Double), IsImplicit) (Syntax: 'If(bLimit,  ... t1, limit2)')
                      Arguments(0)
                Leaving: {R7} {R1}

            Next (Regular) Block[B29]
                Leaving: {R7}
        Block[B28] - Block
            Predecessors: [B26]
            Statements (0)
            Jump if False (Regular) to Block[B43]
                IBinaryOperation (BinaryOperatorKind.GreaterThanOrEqual) (OperationKind.Binary, Type: System.Boolean, IsImplicit) (Syntax: 'If(bLimit,  ... t1, limit2)')
                  Left: 
                    IFlowCaptureReferenceOperation: 11 (OperationKind.FlowCaptureReference, Type: System.Double, IsImplicit) (Syntax: 'If(c1, c2).i')
                  Right: 
                    IInvocationOperation ( Function System.Nullable(Of System.Double).GetValueOrDefault() As System.Double) (OperationKind.Invocation, Type: System.Double, IsImplicit) (Syntax: 'If(bLimit,  ... t1, limit2)')
                      Instance Receiver: 
                        IFlowCaptureReferenceOperation: 4 (OperationKind.FlowCaptureReference, Type: System.Nullable(Of System.Double), IsImplicit) (Syntax: 'If(bLimit,  ... t1, limit2)')
                      Arguments(0)
                Leaving: {R7} {R1}

            Next (Regular) Block[B29]
                Leaving: {R7}
    }

    Block[B29] - Block
        Predecessors: [B27] [B28]
        Statements (1)
            IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'result = true')
              Expression: 
                ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Boolean, IsImplicit) (Syntax: 'result = true')
                  Left: 
                    IParameterReferenceOperation: result (OperationKind.ParameterReference, Type: System.Boolean) (Syntax: 'result')
                  Right: 
                    ILiteralOperation (OperationKind.Literal, Type: System.Boolean, Constant: True) (Syntax: 'true')

        Next (Regular) Block[B30]
            Entering: {R10} {R11} {R12}

    .locals {R10}
    {
        CaptureIds: [14]
        .locals {R11}
        {
            CaptureIds: [13]
            .locals {R12}
            {
                CaptureIds: [12]
                Block[B30] - Block
                    Predecessors: [B29]
                    Statements (1)
                        IFlowCaptureOperation: 12 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'c1')
                          Value: 
                            IParameterReferenceOperation: c1 (OperationKind.ParameterReference, Type: C, IsImplicit) (Syntax: 'c1')

                    Jump if True (Regular) to Block[B32]
                        IIsNullOperation (OperationKind.IsNull, Type: System.Boolean, IsImplicit) (Syntax: 'c1')
                          Operand: 
                            IFlowCaptureReferenceOperation: 12 (OperationKind.FlowCaptureReference, Type: C, IsImplicit) (Syntax: 'c1')
                        Leaving: {R12}

                    Next (Regular) Block[B31]
                Block[B31] - Block
                    Predecessors: [B30]
                    Statements (1)
                        IFlowCaptureOperation: 13 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'c1')
                          Value: 
                            IFlowCaptureReferenceOperation: 12 (OperationKind.FlowCaptureReference, Type: C, IsImplicit) (Syntax: 'c1')

                    Next (Regular) Block[B33]
                        Leaving: {R12}
            }

            Block[B32] - Block
                Predecessors: [B30]
                Statements (1)
                    IFlowCaptureOperation: 13 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'c2')
                      Value: 
                        IParameterReferenceOperation: c2 (OperationKind.ParameterReference, Type: C, IsImplicit) (Syntax: 'c2')

                Next (Regular) Block[B33]
            Block[B33] - Block
                Predecessors: [B31] [B32]
                Statements (1)
                    IFlowCaptureOperation: 14 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'If(c1, c2).i')
                      Value: 
                        IFieldReferenceOperation: C.i As System.Nullable(Of System.Double) (OperationKind.FieldReference, Type: System.Nullable(Of System.Double), IsImplicit) (Syntax: 'If(c1, c2).i')
                          Instance Receiver: 
                            IFlowCaptureReferenceOperation: 13 (OperationKind.FlowCaptureReference, Type: C, IsImplicit) (Syntax: 'If(c1, c2)')

                Next (Regular) Block[B34]
                    Leaving: {R11}
                    Entering: {R13} {R14}
        }
        .locals {R13}
        {
            CaptureIds: [16]
            .locals {R14}
            {
                CaptureIds: [15]
                Block[B34] - Block
                    Predecessors: [B33]
                    Statements (1)
                        IFlowCaptureOperation: 15 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'c1')
                          Value: 
                            IParameterReferenceOperation: c1 (OperationKind.ParameterReference, Type: C, IsImplicit) (Syntax: 'c1')

                    Jump if True (Regular) to Block[B36]
                        IIsNullOperation (OperationKind.IsNull, Type: System.Boolean, IsImplicit) (Syntax: 'c1')
                          Operand: 
                            IFlowCaptureReferenceOperation: 15 (OperationKind.FlowCaptureReference, Type: C, IsImplicit) (Syntax: 'c1')
                        Leaving: {R14}

                    Next (Regular) Block[B35]
                Block[B35] - Block
                    Predecessors: [B34]
                    Statements (1)
                        IFlowCaptureOperation: 16 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'c1')
                          Value: 
                            IFlowCaptureReferenceOperation: 15 (OperationKind.FlowCaptureReference, Type: C, IsImplicit) (Syntax: 'c1')

                    Next (Regular) Block[B37]
                        Leaving: {R14}
            }

            Block[B36] - Block
                Predecessors: [B34]
                Statements (1)
                    IFlowCaptureOperation: 16 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'c2')
                      Value: 
                        IParameterReferenceOperation: c2 (OperationKind.ParameterReference, Type: C, IsImplicit) (Syntax: 'c2')

                Next (Regular) Block[B37]
            Block[B37] - Block
                Predecessors: [B35] [B36]
                Statements (0)
                Jump if False (Regular) to Block[B39]
                    IBinaryOperation (BinaryOperatorKind.Or) (OperationKind.Binary, Type: System.Boolean, IsImplicit) (Syntax: 'If(bStep, step1, step2)')
                      Left: 
                        IIsNullOperation (OperationKind.IsNull, Type: System.Boolean, IsImplicit) (Syntax: 'If(bStep, step1, step2)')
                          Operand: 
                            IFlowCaptureReferenceOperation: 5 (OperationKind.FlowCaptureReference, Type: System.Nullable(Of System.Double), IsImplicit) (Syntax: 'If(bStep, step1, step2)')
                      Right: 
                        IIsNullOperation (OperationKind.IsNull, Type: System.Boolean, IsImplicit) (Syntax: 'If(c1, c2).i')
                          Operand: 
                            IFieldReferenceOperation: C.i As System.Nullable(Of System.Double) (OperationKind.FieldReference, Type: System.Nullable(Of System.Double), IsImplicit) (Syntax: 'If(c1, c2).i')
                              Instance Receiver: 
                                IFlowCaptureReferenceOperation: 16 (OperationKind.FlowCaptureReference, Type: C, IsImplicit) (Syntax: 'If(c1, c2)')
                    Leaving: {R13}
                    Entering: {R15} {R16}

                Next (Regular) Block[B38]
                    Leaving: {R13}
        }

        Block[B38] - Block
            Predecessors: [B37]
            Statements (1)
                ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: null, IsImplicit) (Syntax: 'If(c1, c2).i')
                  Left: 
                    IFlowCaptureReferenceOperation: 14 (OperationKind.FlowCaptureReference, Type: System.Nullable(Of System.Double), IsImplicit) (Syntax: 'If(c1, c2).i')
                  Right: 
                    IDefaultValueOperation (OperationKind.DefaultValue, Type: System.Nullable(Of System.Double), IsImplicit) (Syntax: 'If(c1, c2).i')

            Next (Regular) Block[B18]
                Leaving: {R10}
                Entering: {R5} {R6}

        .locals {R15}
        {
            CaptureIds: [18]
            .locals {R16}
            {
                CaptureIds: [17]
                Block[B39] - Block
                    Predecessors: [B37]
                    Statements (1)
                        IFlowCaptureOperation: 17 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'c1')
                          Value: 
                            IParameterReferenceOperation: c1 (OperationKind.ParameterReference, Type: C, IsImplicit) (Syntax: 'c1')

                    Jump if True (Regular) to Block[B41]
                        IIsNullOperation (OperationKind.IsNull, Type: System.Boolean, IsImplicit) (Syntax: 'c1')
                          Operand: 
                            IFlowCaptureReferenceOperation: 17 (OperationKind.FlowCaptureReference, Type: C, IsImplicit) (Syntax: 'c1')
                        Leaving: {R16}

                    Next (Regular) Block[B40]
                Block[B40] - Block
                    Predecessors: [B39]
                    Statements (1)
                        IFlowCaptureOperation: 18 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'c1')
                          Value: 
                            IFlowCaptureReferenceOperation: 17 (OperationKind.FlowCaptureReference, Type: C, IsImplicit) (Syntax: 'c1')

                    Next (Regular) Block[B42]
                        Leaving: {R16}
            }

            Block[B41] - Block
                Predecessors: [B39]
                Statements (1)
                    IFlowCaptureOperation: 18 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'c2')
                      Value: 
                        IParameterReferenceOperation: c2 (OperationKind.ParameterReference, Type: C, IsImplicit) (Syntax: 'c2')

                Next (Regular) Block[B42]
            Block[B42] - Block
                Predecessors: [B40] [B41]
                Statements (1)
                    ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: null, IsImplicit) (Syntax: 'If(c1, c2).i')
                      Left: 
                        IFlowCaptureReferenceOperation: 14 (OperationKind.FlowCaptureReference, Type: System.Nullable(Of System.Double), IsImplicit) (Syntax: 'If(c1, c2).i')
                      Right: 
                        IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.Nullable(Of System.Double), IsImplicit) (Syntax: 'If(bStep, step1, step2)')
                          Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                            (WideningNullable)
                          Operand: 
                            IBinaryOperation (BinaryOperatorKind.Add, Checked) (OperationKind.Binary, Type: System.Double, IsImplicit) (Syntax: 'If(bStep, step1, step2)')
                              Left: 
                                IInvocationOperation ( Function System.Nullable(Of System.Double).GetValueOrDefault() As System.Double) (OperationKind.Invocation, Type: System.Double, IsImplicit) (Syntax: 'If(c1, c2).i')
                                  Instance Receiver: 
                                    IFieldReferenceOperation: C.i As System.Nullable(Of System.Double) (OperationKind.FieldReference, Type: System.Nullable(Of System.Double), IsImplicit) (Syntax: 'If(c1, c2).i')
                                      Instance Receiver: 
                                        IFlowCaptureReferenceOperation: 18 (OperationKind.FlowCaptureReference, Type: C, IsImplicit) (Syntax: 'If(c1, c2)')
                                  Arguments(0)
                              Right: 
                                IInvocationOperation ( Function System.Nullable(Of System.Double).GetValueOrDefault() As System.Double) (OperationKind.Invocation, Type: System.Double, IsImplicit) (Syntax: 'If(bStep, step1, step2)')
                                  Instance Receiver: 
                                    IFlowCaptureReferenceOperation: 5 (OperationKind.FlowCaptureReference, Type: System.Nullable(Of System.Double), IsImplicit) (Syntax: 'If(bStep, step1, step2)')
                                  Arguments(0)

                Next (Regular) Block[B18]
                    Leaving: {R15} {R10}
                    Entering: {R5} {R6}
        }
    }
}

Block[B43] - Exit
    Predecessors: [B21] [B27] [B28]
    Statements (0)
]]>.Value

            VerifyFlowGraphAndDiagnosticsForTest(Of MethodBlockSyntax)(source, expectedFlowGraph, expectedDiagnostics)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation, CompilerFeature.Dataflow)>
        <Fact()>
        Public Sub ForToFlow_19()
            Dim source = <![CDATA[
Imports System
Public Class C
    Sub M(i As Integer, result As Integer) 'BIND:"Sub M"
        For i = 0 To 4 Step 2
            if result = i
                Exit For
            End If
        Next
    End Sub
End Class]]>.Value

            Dim expectedDiagnostics = String.Empty

            Dim expectedFlowGraph = <![CDATA[
Block[B0] - Entry
    Statements (0)
    Next (Regular) Block[B1]
        Entering: {R1} {R2}

.locals {R1}
{
    CaptureIds: [2] [3]
    .locals {R2}
    {
        CaptureIds: [0] [1]
        Block[B1] - Block
            Predecessors: [B0]
            Statements (5)
                IFlowCaptureOperation: 0 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'i')
                  Value: 
                    IParameterReferenceOperation: i (OperationKind.ParameterReference, Type: System.Int32) (Syntax: 'i')

                IFlowCaptureOperation: 1 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: '0')
                  Value: 
                    ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 0) (Syntax: '0')

                IFlowCaptureOperation: 2 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: '4')
                  Value: 
                    ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 4) (Syntax: '4')

                IFlowCaptureOperation: 3 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: '2')
                  Value: 
                    ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 2) (Syntax: '2')

                ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: null, IsImplicit) (Syntax: '0')
                  Left: 
                    IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: System.Int32, IsImplicit) (Syntax: 'i')
                  Right: 
                    IFlowCaptureReferenceOperation: 1 (OperationKind.FlowCaptureReference, Type: System.Int32, Constant: 0, IsImplicit) (Syntax: '0')

            Next (Regular) Block[B2]
                Leaving: {R2}
    }

    Block[B2] - Block
        Predecessors: [B1] [B4]
        Statements (0)
        Jump if False (Regular) to Block[B5]
            IBinaryOperation (BinaryOperatorKind.LessThanOrEqual) (OperationKind.Binary, Type: System.Boolean, IsImplicit) (Syntax: '4')
              Left: 
                IParameterReferenceOperation: i (OperationKind.ParameterReference, Type: System.Int32, IsImplicit) (Syntax: 'i')
              Right: 
                IFlowCaptureReferenceOperation: 2 (OperationKind.FlowCaptureReference, Type: System.Int32, Constant: 4, IsImplicit) (Syntax: '4')
            Leaving: {R1}

        Next (Regular) Block[B3]
    Block[B3] - Block
        Predecessors: [B2]
        Statements (0)
        Jump if False (Regular) to Block[B4]
            IBinaryOperation (BinaryOperatorKind.Equals) (OperationKind.Binary, Type: System.Boolean) (Syntax: 'result = i')
              Left: 
                IParameterReferenceOperation: result (OperationKind.ParameterReference, Type: System.Int32) (Syntax: 'result')
              Right: 
                IParameterReferenceOperation: i (OperationKind.ParameterReference, Type: System.Int32) (Syntax: 'i')

        Next (Regular) Block[B5]
            Leaving: {R1}
    Block[B4] - Block
        Predecessors: [B3]
        Statements (1)
            ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: null, IsImplicit) (Syntax: 'i')
              Left: 
                IParameterReferenceOperation: i (OperationKind.ParameterReference, Type: System.Int32, IsImplicit) (Syntax: 'i')
              Right: 
                IBinaryOperation (BinaryOperatorKind.Add) (OperationKind.Binary, Type: System.Int32, IsImplicit) (Syntax: '2')
                  Left: 
                    IParameterReferenceOperation: i (OperationKind.ParameterReference, Type: System.Int32, IsImplicit) (Syntax: 'i')
                  Right: 
                    IFlowCaptureReferenceOperation: 3 (OperationKind.FlowCaptureReference, Type: System.Int32, Constant: 2, IsImplicit) (Syntax: '2')

        Next (Regular) Block[B2]
}

Block[B5] - Exit
    Predecessors: [B2] [B3]
    Statements (0)
]]>.Value

            VerifyFlowGraphAndDiagnosticsForTest(Of MethodBlockSyntax)(source, expectedFlowGraph, expectedDiagnostics, TestOptions.ReleaseDll.WithOverflowChecks(False))
        End Sub

        <CompilerTrait(CompilerFeature.IOperation, CompilerFeature.Dataflow)>
        <Fact()>
        Public Sub ForToFlow_20()
            Dim source = <![CDATA[
Imports System
Public Class C
    Sub M(i As Integer, result As Integer) 'BIND:"Sub M"
        For i = 0 To 4 Step 2
            if result = i
                Continue For
            End If

            result = i
        Next
    End Sub
End Class]]>.Value

            Dim expectedDiagnostics = String.Empty

            Dim expectedFlowGraph = <![CDATA[
Block[B0] - Entry
    Statements (0)
    Next (Regular) Block[B1]
        Entering: {R1} {R2}

.locals {R1}
{
    CaptureIds: [2] [3]
    .locals {R2}
    {
        CaptureIds: [0] [1]
        Block[B1] - Block
            Predecessors: [B0]
            Statements (5)
                IFlowCaptureOperation: 0 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'i')
                  Value: 
                    IParameterReferenceOperation: i (OperationKind.ParameterReference, Type: System.Int32) (Syntax: 'i')

                IFlowCaptureOperation: 1 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: '0')
                  Value: 
                    ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 0) (Syntax: '0')

                IFlowCaptureOperation: 2 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: '4')
                  Value: 
                    ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 4) (Syntax: '4')

                IFlowCaptureOperation: 3 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: '2')
                  Value: 
                    ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 2) (Syntax: '2')

                ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: null, IsImplicit) (Syntax: '0')
                  Left: 
                    IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: System.Int32, IsImplicit) (Syntax: 'i')
                  Right: 
                    IFlowCaptureReferenceOperation: 1 (OperationKind.FlowCaptureReference, Type: System.Int32, Constant: 0, IsImplicit) (Syntax: '0')

            Next (Regular) Block[B2]
                Leaving: {R2}
    }

    Block[B2] - Block
        Predecessors: [B1] [B5]
        Statements (0)
        Jump if False (Regular) to Block[B6]
            IBinaryOperation (BinaryOperatorKind.LessThanOrEqual) (OperationKind.Binary, Type: System.Boolean, IsImplicit) (Syntax: '4')
              Left: 
                IParameterReferenceOperation: i (OperationKind.ParameterReference, Type: System.Int32, IsImplicit) (Syntax: 'i')
              Right: 
                IFlowCaptureReferenceOperation: 2 (OperationKind.FlowCaptureReference, Type: System.Int32, Constant: 4, IsImplicit) (Syntax: '4')
            Leaving: {R1}

        Next (Regular) Block[B3]
    Block[B3] - Block
        Predecessors: [B2]
        Statements (0)
        Jump if False (Regular) to Block[B4]
            IBinaryOperation (BinaryOperatorKind.Equals) (OperationKind.Binary, Type: System.Boolean) (Syntax: 'result = i')
              Left: 
                IParameterReferenceOperation: result (OperationKind.ParameterReference, Type: System.Int32) (Syntax: 'result')
              Right: 
                IParameterReferenceOperation: i (OperationKind.ParameterReference, Type: System.Int32) (Syntax: 'i')

        Next (Regular) Block[B5]
    Block[B4] - Block
        Predecessors: [B3]
        Statements (1)
            IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'result = i')
              Expression: 
                ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Int32, IsImplicit) (Syntax: 'result = i')
                  Left: 
                    IParameterReferenceOperation: result (OperationKind.ParameterReference, Type: System.Int32) (Syntax: 'result')
                  Right: 
                    IParameterReferenceOperation: i (OperationKind.ParameterReference, Type: System.Int32) (Syntax: 'i')

        Next (Regular) Block[B5]
    Block[B5] - Block
        Predecessors: [B3] [B4]
        Statements (1)
            ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: null, IsImplicit) (Syntax: 'i')
              Left: 
                IParameterReferenceOperation: i (OperationKind.ParameterReference, Type: System.Int32, IsImplicit) (Syntax: 'i')
              Right: 
                IBinaryOperation (BinaryOperatorKind.Add) (OperationKind.Binary, Type: System.Int32, IsImplicit) (Syntax: '2')
                  Left: 
                    IParameterReferenceOperation: i (OperationKind.ParameterReference, Type: System.Int32, IsImplicit) (Syntax: 'i')
                  Right: 
                    IFlowCaptureReferenceOperation: 3 (OperationKind.FlowCaptureReference, Type: System.Int32, Constant: 2, IsImplicit) (Syntax: '2')

        Next (Regular) Block[B2]
}

Block[B6] - Exit
    Predecessors: [B2]
    Statements (0)
]]>.Value

            VerifyFlowGraphAndDiagnosticsForTest(Of MethodBlockSyntax)(source, expectedFlowGraph, expectedDiagnostics, TestOptions.ReleaseDll.WithOverflowChecks(False))
        End Sub

        <CompilerTrait(CompilerFeature.IOperation, CompilerFeature.Dataflow)>
        <Fact()>
        Public Sub ForToFlow_21()
            Dim source = <![CDATA[
Imports System
Public Class C
    Sub M(i As C, init As C, limit As C, [step] As C, result As C) 'BIND:"Sub M"
        For i = init To limit Step [step]
            result = i
        Next
    End Sub

    Public Shared Operator <=(x As C, y As C) As Boolean
        Return False
    End Operator

    Public Shared Operator -(x As C, y As C) As C
        Return Nothing
    End Operator

    Public Shared Operator +(x As C, y As C) As C
        Return Nothing
    End Operator
End Class
]]>.Value

            Dim expectedDiagnostics = <![CDATA[
BC33038: Type 'C' must define operator '>=' to be used in a 'For' statement.
        For i = init To limit Step [step]
        ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
BC33033: Matching '>=' operator is required for 'Public Shared Operator <=(x As C, y As C) As Boolean'.
    Public Shared Operator <=(x As C, y As C) As Boolean
                           ~~
]]>.Value

            Dim expectedFlowGraph = <![CDATA[
Block[B0] - Entry
    Statements (0)
    Next (Regular) Block[B1]
        Entering: {R1} {R2}

.locals {R1}
{
    CaptureIds: [2] [3]
    .locals {R2}
    {
        CaptureIds: [0] [1]
        Block[B1] - Block
            Predecessors: [B0]
            Statements (5)
                IFlowCaptureOperation: 0 (OperationKind.FlowCapture, Type: null, IsInvalid, IsImplicit) (Syntax: 'i')
                  Value: 
                    IParameterReferenceOperation: i (OperationKind.ParameterReference, Type: C, IsInvalid) (Syntax: 'i')

                IFlowCaptureOperation: 1 (OperationKind.FlowCapture, Type: null, IsInvalid, IsImplicit) (Syntax: 'init')
                  Value: 
                    IParameterReferenceOperation: init (OperationKind.ParameterReference, Type: C, IsInvalid) (Syntax: 'init')

                IFlowCaptureOperation: 2 (OperationKind.FlowCapture, Type: null, IsInvalid, IsImplicit) (Syntax: 'limit')
                  Value: 
                    IParameterReferenceOperation: limit (OperationKind.ParameterReference, Type: C, IsInvalid) (Syntax: 'limit')

                IFlowCaptureOperation: 3 (OperationKind.FlowCapture, Type: null, IsInvalid, IsImplicit) (Syntax: '[step]')
                  Value: 
                    IParameterReferenceOperation: step (OperationKind.ParameterReference, Type: C, IsInvalid) (Syntax: '[step]')

                ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: null, IsInvalid, IsImplicit) (Syntax: 'init')
                  Left: 
                    IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: C, IsInvalid, IsImplicit) (Syntax: 'i')
                  Right: 
                    IFlowCaptureReferenceOperation: 1 (OperationKind.FlowCaptureReference, Type: C, IsInvalid, IsImplicit) (Syntax: 'init')

            Next (Regular) Block[B2]
                Leaving: {R2}
    }

    Block[B2] - Block
        Predecessors: [B1] [B3]
        Statements (0)
        Jump if False (Regular) to Block[B4]
            IInvalidOperation (OperationKind.Invalid, Type: System.Boolean, IsInvalid, IsImplicit) (Syntax: 'limit')
              Children(2):
                  IParameterReferenceOperation: i (OperationKind.ParameterReference, Type: C, IsInvalid, IsImplicit) (Syntax: 'i')
                  IFlowCaptureReferenceOperation: 2 (OperationKind.FlowCaptureReference, Type: C, IsInvalid, IsImplicit) (Syntax: 'limit')
            Leaving: {R1}

        Next (Regular) Block[B3]
    Block[B3] - Block
        Predecessors: [B2]
        Statements (2)
            IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'result = i')
              Expression: 
                ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: C, IsImplicit) (Syntax: 'result = i')
                  Left: 
                    IParameterReferenceOperation: result (OperationKind.ParameterReference, Type: C) (Syntax: 'result')
                  Right: 
                    IParameterReferenceOperation: i (OperationKind.ParameterReference, Type: C) (Syntax: 'i')

            ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: null, IsInvalid, IsImplicit) (Syntax: 'i')
              Left: 
                IParameterReferenceOperation: i (OperationKind.ParameterReference, Type: C, IsInvalid, IsImplicit) (Syntax: 'i')
              Right: 
                IBinaryOperation (BinaryOperatorKind.Add, Checked) (OperationKind.Binary, Type: C, IsInvalid, IsImplicit) (Syntax: '[step]')
                  Left: 
                    IParameterReferenceOperation: i (OperationKind.ParameterReference, Type: C, IsInvalid, IsImplicit) (Syntax: 'i')
                  Right: 
                    IFlowCaptureReferenceOperation: 3 (OperationKind.FlowCaptureReference, Type: C, IsInvalid, IsImplicit) (Syntax: '[step]')

        Next (Regular) Block[B2]
}

Block[B4] - Exit
    Predecessors: [B2]
    Statements (0)
]]>.Value

            VerifyFlowGraphAndDiagnosticsForTest(Of MethodBlockSyntax)(source, expectedFlowGraph, expectedDiagnostics)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation, CompilerFeature.Dataflow)>
        <Fact()>
        Public Sub ForToFlow_22()
            Dim source = <![CDATA[
Imports System
Public Class C
    Sub M(i As C, init As C, limit As C, [step] As C, result As C) 'BIND:"Sub M"
        For i = init To limit Step [step]
            result = i
        Next
    End Sub

    Public Shared Operator >=(x As C, y As C) As Boolean
        Return False
    End Operator

    Public Shared Operator -(x As C, y As C) As C
        Return Nothing
    End Operator

    Public Shared Operator +(x As C, y As C) As C
        Return Nothing
    End Operator
End Class
]]>.Value

            Dim expectedDiagnostics = <![CDATA[
BC33038: Type 'C' must define operator '<=' to be used in a 'For' statement.
        For i = init To limit Step [step]
        ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
BC33033: Matching '<=' operator is required for 'Public Shared Operator >=(x As C, y As C) As Boolean'.
    Public Shared Operator >=(x As C, y As C) As Boolean
                           ~~
]]>.Value

            Dim expectedFlowGraph = <![CDATA[
Block[B0] - Entry
    Statements (0)
    Next (Regular) Block[B1]
        Entering: {R1} {R2}

.locals {R1}
{
    CaptureIds: [2] [3]
    .locals {R2}
    {
        CaptureIds: [0] [1]
        Block[B1] - Block
            Predecessors: [B0]
            Statements (5)
                IFlowCaptureOperation: 0 (OperationKind.FlowCapture, Type: null, IsInvalid, IsImplicit) (Syntax: 'i')
                  Value: 
                    IParameterReferenceOperation: i (OperationKind.ParameterReference, Type: C, IsInvalid) (Syntax: 'i')

                IFlowCaptureOperation: 1 (OperationKind.FlowCapture, Type: null, IsInvalid, IsImplicit) (Syntax: 'init')
                  Value: 
                    IParameterReferenceOperation: init (OperationKind.ParameterReference, Type: C, IsInvalid) (Syntax: 'init')

                IFlowCaptureOperation: 2 (OperationKind.FlowCapture, Type: null, IsInvalid, IsImplicit) (Syntax: 'limit')
                  Value: 
                    IParameterReferenceOperation: limit (OperationKind.ParameterReference, Type: C, IsInvalid) (Syntax: 'limit')

                IFlowCaptureOperation: 3 (OperationKind.FlowCapture, Type: null, IsInvalid, IsImplicit) (Syntax: '[step]')
                  Value: 
                    IParameterReferenceOperation: step (OperationKind.ParameterReference, Type: C, IsInvalid) (Syntax: '[step]')

                ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: null, IsInvalid, IsImplicit) (Syntax: 'init')
                  Left: 
                    IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: C, IsInvalid, IsImplicit) (Syntax: 'i')
                  Right: 
                    IFlowCaptureReferenceOperation: 1 (OperationKind.FlowCaptureReference, Type: C, IsInvalid, IsImplicit) (Syntax: 'init')

            Next (Regular) Block[B2]
                Leaving: {R2}
    }

    Block[B2] - Block
        Predecessors: [B1] [B3]
        Statements (0)
        Jump if False (Regular) to Block[B4]
            IInvalidOperation (OperationKind.Invalid, Type: System.Boolean, IsInvalid, IsImplicit) (Syntax: 'limit')
              Children(2):
                  IParameterReferenceOperation: i (OperationKind.ParameterReference, Type: C, IsInvalid, IsImplicit) (Syntax: 'i')
                  IFlowCaptureReferenceOperation: 2 (OperationKind.FlowCaptureReference, Type: C, IsInvalid, IsImplicit) (Syntax: 'limit')
            Leaving: {R1}

        Next (Regular) Block[B3]
    Block[B3] - Block
        Predecessors: [B2]
        Statements (2)
            IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'result = i')
              Expression: 
                ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: C, IsImplicit) (Syntax: 'result = i')
                  Left: 
                    IParameterReferenceOperation: result (OperationKind.ParameterReference, Type: C) (Syntax: 'result')
                  Right: 
                    IParameterReferenceOperation: i (OperationKind.ParameterReference, Type: C) (Syntax: 'i')

            ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: null, IsInvalid, IsImplicit) (Syntax: 'i')
              Left: 
                IParameterReferenceOperation: i (OperationKind.ParameterReference, Type: C, IsInvalid, IsImplicit) (Syntax: 'i')
              Right: 
                IBinaryOperation (BinaryOperatorKind.Add, Checked) (OperationKind.Binary, Type: C, IsInvalid, IsImplicit) (Syntax: '[step]')
                  Left: 
                    IParameterReferenceOperation: i (OperationKind.ParameterReference, Type: C, IsInvalid, IsImplicit) (Syntax: 'i')
                  Right: 
                    IFlowCaptureReferenceOperation: 3 (OperationKind.FlowCaptureReference, Type: C, IsInvalid, IsImplicit) (Syntax: '[step]')

        Next (Regular) Block[B2]
}

Block[B4] - Exit
    Predecessors: [B2]
    Statements (0)
]]>.Value

            VerifyFlowGraphAndDiagnosticsForTest(Of MethodBlockSyntax)(source, expectedFlowGraph, expectedDiagnostics)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation, CompilerFeature.Dataflow)>
        <Fact()>
        Public Sub ForToFlow_23()
            Dim source = <![CDATA[
Imports System
Public Class C
    Sub M(i As C, init As C, limit As C, [step] As C, result As C) 'BIND:"Sub M"
        For i = init To limit Step [step]
            result = i
        Next
    End Sub

    Public Shared Operator >=(x As C, y As C) As Boolean
        Return False
    End Operator

    Public Shared Operator <=(x As C, y As C) As Boolean
        Return False
    End Operator

    Public Shared Operator +(x As C, y As C) As C
        Return Nothing
    End Operator
End Class
]]>.Value

            Dim expectedDiagnostics = <![CDATA[
BC33038: Type 'C' must define operator '-' to be used in a 'For' statement.
        For i = init To limit Step [step]
        ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
]]>.Value

            Dim expectedFlowGraph = <![CDATA[
Block[B0] - Entry
    Statements (0)
    Next (Regular) Block[B1]
        Entering: {R1} {R2}

.locals {R1}
{
    CaptureIds: [2] [3]
    .locals {R2}
    {
        CaptureIds: [0] [1]
        Block[B1] - Block
            Predecessors: [B0]
            Statements (5)
                IFlowCaptureOperation: 0 (OperationKind.FlowCapture, Type: null, IsInvalid, IsImplicit) (Syntax: 'i')
                  Value: 
                    IParameterReferenceOperation: i (OperationKind.ParameterReference, Type: C, IsInvalid) (Syntax: 'i')

                IFlowCaptureOperation: 1 (OperationKind.FlowCapture, Type: null, IsInvalid, IsImplicit) (Syntax: 'init')
                  Value: 
                    IParameterReferenceOperation: init (OperationKind.ParameterReference, Type: C, IsInvalid) (Syntax: 'init')

                IFlowCaptureOperation: 2 (OperationKind.FlowCapture, Type: null, IsInvalid, IsImplicit) (Syntax: 'limit')
                  Value: 
                    IParameterReferenceOperation: limit (OperationKind.ParameterReference, Type: C, IsInvalid) (Syntax: 'limit')

                IFlowCaptureOperation: 3 (OperationKind.FlowCapture, Type: null, IsInvalid, IsImplicit) (Syntax: '[step]')
                  Value: 
                    IParameterReferenceOperation: step (OperationKind.ParameterReference, Type: C, IsInvalid) (Syntax: '[step]')

                ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: null, IsInvalid, IsImplicit) (Syntax: 'init')
                  Left: 
                    IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: C, IsInvalid, IsImplicit) (Syntax: 'i')
                  Right: 
                    IFlowCaptureReferenceOperation: 1 (OperationKind.FlowCaptureReference, Type: C, IsInvalid, IsImplicit) (Syntax: 'init')

            Next (Regular) Block[B2]
                Leaving: {R2}
    }

    Block[B2] - Block
        Predecessors: [B1] [B3]
        Statements (0)
        Jump if False (Regular) to Block[B4]
            IInvalidOperation (OperationKind.Invalid, Type: System.Boolean, IsInvalid, IsImplicit) (Syntax: 'limit')
              Children(2):
                  IParameterReferenceOperation: i (OperationKind.ParameterReference, Type: C, IsInvalid, IsImplicit) (Syntax: 'i')
                  IFlowCaptureReferenceOperation: 2 (OperationKind.FlowCaptureReference, Type: C, IsInvalid, IsImplicit) (Syntax: 'limit')
            Leaving: {R1}

        Next (Regular) Block[B3]
    Block[B3] - Block
        Predecessors: [B2]
        Statements (2)
            IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'result = i')
              Expression: 
                ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: C, IsImplicit) (Syntax: 'result = i')
                  Left: 
                    IParameterReferenceOperation: result (OperationKind.ParameterReference, Type: C) (Syntax: 'result')
                  Right: 
                    IParameterReferenceOperation: i (OperationKind.ParameterReference, Type: C) (Syntax: 'i')

            ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: null, IsInvalid, IsImplicit) (Syntax: 'i')
              Left: 
                IParameterReferenceOperation: i (OperationKind.ParameterReference, Type: C, IsInvalid, IsImplicit) (Syntax: 'i')
              Right: 
                IBinaryOperation (BinaryOperatorKind.Add, Checked) (OperationKind.Binary, Type: C, IsInvalid, IsImplicit) (Syntax: '[step]')
                  Left: 
                    IParameterReferenceOperation: i (OperationKind.ParameterReference, Type: C, IsInvalid, IsImplicit) (Syntax: 'i')
                  Right: 
                    IFlowCaptureReferenceOperation: 3 (OperationKind.FlowCaptureReference, Type: C, IsInvalid, IsImplicit) (Syntax: '[step]')

        Next (Regular) Block[B2]
}

Block[B4] - Exit
    Predecessors: [B2]
    Statements (0)
]]>.Value

            VerifyFlowGraphAndDiagnosticsForTest(Of MethodBlockSyntax)(source, expectedFlowGraph, expectedDiagnostics)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation, CompilerFeature.Dataflow)>
        <Fact()>
        Public Sub ForToFlow_24()
            Dim source = <![CDATA[
Imports System
Public Class C
    Sub M(i As C, init As C, limit As C, [step] As C, result As C) 'BIND:"Sub M"
        For i = init To limit Step [step]
            result = i
        Next
    End Sub

    Public Shared Operator >=(x As C, y As C) As Boolean
        Return False
    End Operator

    Public Shared Operator <=(x As C, y As C) As Boolean
        Return False
    End Operator

    Public Shared Operator -(x As C, y As C) As C
        Return Nothing
    End Operator
End Class
]]>.Value

            Dim expectedDiagnostics = <![CDATA[
BC33038: Type 'C' must define operator '+' to be used in a 'For' statement.
        For i = init To limit Step [step]
        ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
]]>.Value

            Dim expectedFlowGraph = <![CDATA[
Block[B0] - Entry
    Statements (0)
    Next (Regular) Block[B1]
        Entering: {R1} {R2}

.locals {R1}
{
    CaptureIds: [2] [3]
    .locals {R2}
    {
        CaptureIds: [0] [1]
        Block[B1] - Block
            Predecessors: [B0]
            Statements (5)
                IFlowCaptureOperation: 0 (OperationKind.FlowCapture, Type: null, IsInvalid, IsImplicit) (Syntax: 'i')
                  Value: 
                    IParameterReferenceOperation: i (OperationKind.ParameterReference, Type: C, IsInvalid) (Syntax: 'i')

                IFlowCaptureOperation: 1 (OperationKind.FlowCapture, Type: null, IsInvalid, IsImplicit) (Syntax: 'init')
                  Value: 
                    IParameterReferenceOperation: init (OperationKind.ParameterReference, Type: C, IsInvalid) (Syntax: 'init')

                IFlowCaptureOperation: 2 (OperationKind.FlowCapture, Type: null, IsInvalid, IsImplicit) (Syntax: 'limit')
                  Value: 
                    IParameterReferenceOperation: limit (OperationKind.ParameterReference, Type: C, IsInvalid) (Syntax: 'limit')

                IFlowCaptureOperation: 3 (OperationKind.FlowCapture, Type: null, IsInvalid, IsImplicit) (Syntax: '[step]')
                  Value: 
                    IParameterReferenceOperation: step (OperationKind.ParameterReference, Type: C, IsInvalid) (Syntax: '[step]')

                ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: null, IsInvalid, IsImplicit) (Syntax: 'init')
                  Left: 
                    IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: C, IsInvalid, IsImplicit) (Syntax: 'i')
                  Right: 
                    IFlowCaptureReferenceOperation: 1 (OperationKind.FlowCaptureReference, Type: C, IsInvalid, IsImplicit) (Syntax: 'init')

            Next (Regular) Block[B2]
                Leaving: {R2}
    }

    Block[B2] - Block
        Predecessors: [B1] [B3]
        Statements (0)
        Jump if False (Regular) to Block[B4]
            IInvalidOperation (OperationKind.Invalid, Type: System.Boolean, IsInvalid, IsImplicit) (Syntax: 'limit')
              Children(2):
                  IParameterReferenceOperation: i (OperationKind.ParameterReference, Type: C, IsInvalid, IsImplicit) (Syntax: 'i')
                  IFlowCaptureReferenceOperation: 2 (OperationKind.FlowCaptureReference, Type: C, IsInvalid, IsImplicit) (Syntax: 'limit')
            Leaving: {R1}

        Next (Regular) Block[B3]
    Block[B3] - Block
        Predecessors: [B2]
        Statements (2)
            IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'result = i')
              Expression: 
                ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: C, IsImplicit) (Syntax: 'result = i')
                  Left: 
                    IParameterReferenceOperation: result (OperationKind.ParameterReference, Type: C) (Syntax: 'result')
                  Right: 
                    IParameterReferenceOperation: i (OperationKind.ParameterReference, Type: C) (Syntax: 'i')

            ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: null, IsInvalid, IsImplicit) (Syntax: 'i')
              Left: 
                IParameterReferenceOperation: i (OperationKind.ParameterReference, Type: C, IsInvalid, IsImplicit) (Syntax: 'i')
              Right: 
                IBinaryOperation (BinaryOperatorKind.Add, Checked) (OperationKind.Binary, Type: C, IsInvalid, IsImplicit) (Syntax: '[step]')
                  Left: 
                    IParameterReferenceOperation: i (OperationKind.ParameterReference, Type: C, IsInvalid, IsImplicit) (Syntax: 'i')
                  Right: 
                    IFlowCaptureReferenceOperation: 3 (OperationKind.FlowCaptureReference, Type: C, IsInvalid, IsImplicit) (Syntax: '[step]')

        Next (Regular) Block[B2]
}

Block[B4] - Exit
    Predecessors: [B2]
    Statements (0)
]]>.Value

            VerifyFlowGraphAndDiagnosticsForTest(Of MethodBlockSyntax)(source, expectedFlowGraph, expectedDiagnostics)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation, CompilerFeature.Dataflow)>
        <Fact()>
        Public Sub ForToFlow_25()
            Dim source = <![CDATA[
Imports System
Public Class C
    Sub M(i As C, init As C, limit As C, [step] As C, result As C) 'BIND:"Sub M"
        For i = init To limit Step [step]
            result = i
        Next
    End Sub

    Public Shared Operator >=(x As C, y As C) As Boolean
        Return False
    End Operator

    Public Shared Operator <=(x As C, y As C) As Boolean
        Return False
    End Operator

    Public Shared Operator -(x As C, y As C) As C
        Return Nothing
    End Operator

    Public Shared Operator +(x As C, y As C) As C
        Return Nothing
    End Operator
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
    CaptureIds: [2] [3] [4]
    .locals {R2}
    {
        CaptureIds: [0] [1]
        Block[B1] - Block
            Predecessors: [B0]
            Statements (6)
                IFlowCaptureOperation: 0 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'i')
                  Value: 
                    IParameterReferenceOperation: i (OperationKind.ParameterReference, Type: C) (Syntax: 'i')

                IFlowCaptureOperation: 1 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'init')
                  Value: 
                    IParameterReferenceOperation: init (OperationKind.ParameterReference, Type: C) (Syntax: 'init')

                IFlowCaptureOperation: 2 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'limit')
                  Value: 
                    IParameterReferenceOperation: limit (OperationKind.ParameterReference, Type: C) (Syntax: 'limit')

                IFlowCaptureOperation: 3 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: '[step]')
                  Value: 
                    IParameterReferenceOperation: step (OperationKind.ParameterReference, Type: C) (Syntax: '[step]')

                IFlowCaptureOperation: 4 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'For i = ini ... Step [step]')
                  Value: 
                    IBinaryOperation (BinaryOperatorKind.GreaterThanOrEqual, Checked) (OperatorMethod: Function C.op_GreaterThanOrEqual(x As C, y As C) As System.Boolean) (OperationKind.Binary, Type: System.Boolean, IsImplicit) (Syntax: 'For i = ini ... Step [step]')
                      Left: 
                        IFlowCaptureReferenceOperation: 3 (OperationKind.FlowCaptureReference, Type: C, IsImplicit) (Syntax: '[step]')
                      Right: 
                        IBinaryOperation (BinaryOperatorKind.Subtract, Checked) (OperatorMethod: Function C.op_Subtraction(x As C, y As C) As C) (OperationKind.Binary, Type: C, IsImplicit) (Syntax: 'For i = ini ... Step [step]')
                          Left: 
                            IFlowCaptureReferenceOperation: 3 (OperationKind.FlowCaptureReference, Type: C, IsImplicit) (Syntax: '[step]')
                          Right: 
                            IFlowCaptureReferenceOperation: 3 (OperationKind.FlowCaptureReference, Type: C, IsImplicit) (Syntax: '[step]')

                ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: null, IsImplicit) (Syntax: 'init')
                  Left: 
                    IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: C, IsImplicit) (Syntax: 'i')
                  Right: 
                    IFlowCaptureReferenceOperation: 1 (OperationKind.FlowCaptureReference, Type: C, IsImplicit) (Syntax: 'init')

            Next (Regular) Block[B2]
                Leaving: {R2}
                Entering: {R3}
    }
    .locals {R3}
    {
        CaptureIds: [5]
        Block[B2] - Block
            Predecessors: [B1] [B5]
            Statements (1)
                IFlowCaptureOperation: 5 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'i')
                  Value: 
                    IParameterReferenceOperation: i (OperationKind.ParameterReference, Type: C, IsImplicit) (Syntax: 'i')

            Jump if False (Regular) to Block[B4]
                IFlowCaptureReferenceOperation: 4 (OperationKind.FlowCaptureReference, Type: System.Boolean, IsImplicit) (Syntax: 'For i = ini ... Step [step]')

            Next (Regular) Block[B3]
        Block[B3] - Block
            Predecessors: [B2]
            Statements (0)
            Jump if False (Regular) to Block[B6]
                IBinaryOperation (BinaryOperatorKind.LessThanOrEqual, Checked) (OperatorMethod: Function C.op_LessThanOrEqual(x As C, y As C) As System.Boolean) (OperationKind.Binary, Type: System.Boolean, IsImplicit) (Syntax: 'For i = ini ... Step [step]')
                  Left: 
                    IFlowCaptureReferenceOperation: 5 (OperationKind.FlowCaptureReference, Type: C, IsImplicit) (Syntax: 'i')
                  Right: 
                    IFlowCaptureReferenceOperation: 2 (OperationKind.FlowCaptureReference, Type: C, IsImplicit) (Syntax: 'limit')
                Leaving: {R3} {R1}

            Next (Regular) Block[B5]
                Leaving: {R3}
        Block[B4] - Block
            Predecessors: [B2]
            Statements (0)
            Jump if False (Regular) to Block[B6]
                IBinaryOperation (BinaryOperatorKind.GreaterThanOrEqual, Checked) (OperatorMethod: Function C.op_GreaterThanOrEqual(x As C, y As C) As System.Boolean) (OperationKind.Binary, Type: System.Boolean, IsImplicit) (Syntax: 'For i = ini ... Step [step]')
                  Left: 
                    IFlowCaptureReferenceOperation: 5 (OperationKind.FlowCaptureReference, Type: C, IsImplicit) (Syntax: 'i')
                  Right: 
                    IFlowCaptureReferenceOperation: 2 (OperationKind.FlowCaptureReference, Type: C, IsImplicit) (Syntax: 'limit')
                Leaving: {R3} {R1}

            Next (Regular) Block[B5]
                Leaving: {R3}
    }

    Block[B5] - Block
        Predecessors: [B3] [B4]
        Statements (2)
            IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'result = i')
              Expression: 
                ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: C, IsImplicit) (Syntax: 'result = i')
                  Left: 
                    IParameterReferenceOperation: result (OperationKind.ParameterReference, Type: C) (Syntax: 'result')
                  Right: 
                    IParameterReferenceOperation: i (OperationKind.ParameterReference, Type: C) (Syntax: 'i')

            ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: null, IsImplicit) (Syntax: 'i')
              Left: 
                IParameterReferenceOperation: i (OperationKind.ParameterReference, Type: C, IsImplicit) (Syntax: 'i')
              Right: 
                IBinaryOperation (BinaryOperatorKind.Add, Checked) (OperatorMethod: Function C.op_Addition(x As C, y As C) As C) (OperationKind.Binary, Type: C, IsImplicit) (Syntax: 'For i = ini ... Step [step]')
                  Left: 
                    IParameterReferenceOperation: i (OperationKind.ParameterReference, Type: C, IsImplicit) (Syntax: 'i')
                  Right: 
                    IFlowCaptureReferenceOperation: 3 (OperationKind.FlowCaptureReference, Type: C, IsImplicit) (Syntax: '[step]')

        Next (Regular) Block[B2]
            Entering: {R3}
}

Block[B6] - Exit
    Predecessors: [B3] [B4]
    Statements (0)
]]>.Value

            VerifyFlowGraphAndDiagnosticsForTest(Of MethodBlockSyntax)(source, expectedFlowGraph, expectedDiagnostics)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation, CompilerFeature.Dataflow)>
        <Fact()>
        Public Sub ForToFlow_26()
            Dim source = <![CDATA[
Imports System
Public Class C
    Sub M(i As C, init As C, limit As C, [step] As C, result As C) 'BIND:"Sub M"
        For i = init To limit Step [step]
            result = i
        Next
    End Sub

    Public Shared Operator >=(x As C, y As C) As C
        Return Nothing
    End Operator

    Public Shared Operator <=(x As C, y As C) As C
        Return Nothing
    End Operator

    Public Shared Operator -(x As C, y As C) As C
        Return Nothing
    End Operator

    Public Shared Operator +(x As C, y As C) As C
        Return Nothing
    End Operator
End Class
]]>.Value

            Dim expectedDiagnostics = <![CDATA[
BC30311: Value of type 'C' cannot be converted to 'Boolean'.
        For i = init To limit Step [step]
        ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
]]>.Value

            Dim expectedFlowGraph = <![CDATA[
Block[B0] - Entry
    Statements (0)
    Next (Regular) Block[B1]
        Entering: {R1} {R2}

.locals {R1}
{
    CaptureIds: [2] [3] [4]
    .locals {R2}
    {
        CaptureIds: [0] [1]
        Block[B1] - Block
            Predecessors: [B0]
            Statements (6)
                IFlowCaptureOperation: 0 (OperationKind.FlowCapture, Type: null, IsInvalid, IsImplicit) (Syntax: 'i')
                  Value: 
                    IParameterReferenceOperation: i (OperationKind.ParameterReference, Type: C, IsInvalid) (Syntax: 'i')

                IFlowCaptureOperation: 1 (OperationKind.FlowCapture, Type: null, IsInvalid, IsImplicit) (Syntax: 'init')
                  Value: 
                    IParameterReferenceOperation: init (OperationKind.ParameterReference, Type: C, IsInvalid) (Syntax: 'init')

                IFlowCaptureOperation: 2 (OperationKind.FlowCapture, Type: null, IsInvalid, IsImplicit) (Syntax: 'limit')
                  Value: 
                    IParameterReferenceOperation: limit (OperationKind.ParameterReference, Type: C, IsInvalid) (Syntax: 'limit')

                IFlowCaptureOperation: 3 (OperationKind.FlowCapture, Type: null, IsInvalid, IsImplicit) (Syntax: '[step]')
                  Value: 
                    IParameterReferenceOperation: step (OperationKind.ParameterReference, Type: C, IsInvalid) (Syntax: '[step]')

                IFlowCaptureOperation: 4 (OperationKind.FlowCapture, Type: null, IsInvalid, IsImplicit) (Syntax: 'For i = ini ... Step [step]')
                  Value: 
                    IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.Boolean, IsInvalid, IsImplicit) (Syntax: 'For i = ini ... Step [step]')
                      Conversion: CommonConversion (Exists: False, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                        (DelegateRelaxationLevelNone)
                      Operand: 
                        IBinaryOperation (BinaryOperatorKind.GreaterThanOrEqual, Checked) (OperatorMethod: Function C.op_GreaterThanOrEqual(x As C, y As C) As C) (OperationKind.Binary, Type: C, IsInvalid, IsImplicit) (Syntax: 'For i = ini ... Step [step]')
                          Left: 
                            IFlowCaptureReferenceOperation: 3 (OperationKind.FlowCaptureReference, Type: C, IsInvalid, IsImplicit) (Syntax: '[step]')
                          Right: 
                            IBinaryOperation (BinaryOperatorKind.Subtract, Checked) (OperatorMethod: Function C.op_Subtraction(x As C, y As C) As C) (OperationKind.Binary, Type: C, IsInvalid, IsImplicit) (Syntax: 'For i = ini ... Step [step]')
                              Left: 
                                IFlowCaptureReferenceOperation: 3 (OperationKind.FlowCaptureReference, Type: C, IsInvalid, IsImplicit) (Syntax: '[step]')
                              Right: 
                                IFlowCaptureReferenceOperation: 3 (OperationKind.FlowCaptureReference, Type: C, IsInvalid, IsImplicit) (Syntax: '[step]')

                ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: null, IsInvalid, IsImplicit) (Syntax: 'init')
                  Left: 
                    IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: C, IsInvalid, IsImplicit) (Syntax: 'i')
                  Right: 
                    IFlowCaptureReferenceOperation: 1 (OperationKind.FlowCaptureReference, Type: C, IsInvalid, IsImplicit) (Syntax: 'init')

            Next (Regular) Block[B2]
                Leaving: {R2}
                Entering: {R3}
    }
    .locals {R3}
    {
        CaptureIds: [5]
        Block[B2] - Block
            Predecessors: [B1] [B5]
            Statements (1)
                IFlowCaptureOperation: 5 (OperationKind.FlowCapture, Type: null, IsInvalid, IsImplicit) (Syntax: 'i')
                  Value: 
                    IParameterReferenceOperation: i (OperationKind.ParameterReference, Type: C, IsInvalid, IsImplicit) (Syntax: 'i')

            Jump if False (Regular) to Block[B4]
                IFlowCaptureReferenceOperation: 4 (OperationKind.FlowCaptureReference, Type: System.Boolean, IsInvalid, IsImplicit) (Syntax: 'For i = ini ... Step [step]')

            Next (Regular) Block[B3]
        Block[B3] - Block
            Predecessors: [B2]
            Statements (0)
            Jump if False (Regular) to Block[B6]
                IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.Boolean, IsInvalid, IsImplicit) (Syntax: 'For i = ini ... Step [step]')
                  Conversion: CommonConversion (Exists: False, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                    (DelegateRelaxationLevelNone)
                  Operand: 
                    IBinaryOperation (BinaryOperatorKind.LessThanOrEqual, Checked) (OperatorMethod: Function C.op_LessThanOrEqual(x As C, y As C) As C) (OperationKind.Binary, Type: C, IsInvalid, IsImplicit) (Syntax: 'For i = ini ... Step [step]')
                      Left: 
                        IFlowCaptureReferenceOperation: 5 (OperationKind.FlowCaptureReference, Type: C, IsInvalid, IsImplicit) (Syntax: 'i')
                      Right: 
                        IFlowCaptureReferenceOperation: 2 (OperationKind.FlowCaptureReference, Type: C, IsInvalid, IsImplicit) (Syntax: 'limit')
                Leaving: {R3} {R1}

            Next (Regular) Block[B5]
                Leaving: {R3}
        Block[B4] - Block
            Predecessors: [B2]
            Statements (0)
            Jump if False (Regular) to Block[B6]
                IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.Boolean, IsInvalid, IsImplicit) (Syntax: 'For i = ini ... Step [step]')
                  Conversion: CommonConversion (Exists: False, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                    (DelegateRelaxationLevelNone)
                  Operand: 
                    IBinaryOperation (BinaryOperatorKind.GreaterThanOrEqual, Checked) (OperatorMethod: Function C.op_GreaterThanOrEqual(x As C, y As C) As C) (OperationKind.Binary, Type: C, IsInvalid, IsImplicit) (Syntax: 'For i = ini ... Step [step]')
                      Left: 
                        IFlowCaptureReferenceOperation: 5 (OperationKind.FlowCaptureReference, Type: C, IsInvalid, IsImplicit) (Syntax: 'i')
                      Right: 
                        IFlowCaptureReferenceOperation: 2 (OperationKind.FlowCaptureReference, Type: C, IsInvalid, IsImplicit) (Syntax: 'limit')
                Leaving: {R3} {R1}

            Next (Regular) Block[B5]
                Leaving: {R3}
    }

    Block[B5] - Block
        Predecessors: [B3] [B4]
        Statements (2)
            IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'result = i')
              Expression: 
                ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: C, IsImplicit) (Syntax: 'result = i')
                  Left: 
                    IParameterReferenceOperation: result (OperationKind.ParameterReference, Type: C) (Syntax: 'result')
                  Right: 
                    IParameterReferenceOperation: i (OperationKind.ParameterReference, Type: C) (Syntax: 'i')

            ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: null, IsInvalid, IsImplicit) (Syntax: 'i')
              Left: 
                IParameterReferenceOperation: i (OperationKind.ParameterReference, Type: C, IsInvalid, IsImplicit) (Syntax: 'i')
              Right: 
                IBinaryOperation (BinaryOperatorKind.Add, Checked) (OperatorMethod: Function C.op_Addition(x As C, y As C) As C) (OperationKind.Binary, Type: C, IsInvalid, IsImplicit) (Syntax: 'For i = ini ... Step [step]')
                  Left: 
                    IParameterReferenceOperation: i (OperationKind.ParameterReference, Type: C, IsInvalid, IsImplicit) (Syntax: 'i')
                  Right: 
                    IFlowCaptureReferenceOperation: 3 (OperationKind.FlowCaptureReference, Type: C, IsInvalid, IsImplicit) (Syntax: '[step]')

        Next (Regular) Block[B2]
            Entering: {R3}
}

Block[B6] - Exit
    Predecessors: [B3] [B4]
    Statements (0)
]]>.Value

            VerifyFlowGraphAndDiagnosticsForTest(Of MethodBlockSyntax)(source, expectedFlowGraph, expectedDiagnostics)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation, CompilerFeature.Dataflow)>
        <Fact()>
        Public Sub ForToFlow_27()
            Dim source = <![CDATA[
Imports System
Public Class C
    Sub M(i As C, init As C, limit As C, [step] As C, result As C) 'BIND:"Sub M"
        For i = init To limit Step [step]
            result = i
        Next
    End Sub

    Public Shared Operator >=(x As C, y As C) As C
        Return Nothing
    End Operator

    Public Shared Operator <=(x As C, y As C) As C
        Return Nothing
    End Operator

    Public Shared Operator -(x As C, y As C) As C
        Return Nothing
    End Operator

    Public Shared Operator +(x As C, y As C) As C
        Return Nothing
    End Operator

    Public Shared Operator IsTrue(x As C) As Boolean
        Return Nothing
    End Operator

    Public Shared Operator IsFalse(x As C) As Boolean
        Return Nothing
    End Operator
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
    CaptureIds: [2] [3] [4]
    .locals {R2}
    {
        CaptureIds: [0] [1]
        Block[B1] - Block
            Predecessors: [B0]
            Statements (6)
                IFlowCaptureOperation: 0 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'i')
                  Value: 
                    IParameterReferenceOperation: i (OperationKind.ParameterReference, Type: C) (Syntax: 'i')

                IFlowCaptureOperation: 1 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'init')
                  Value: 
                    IParameterReferenceOperation: init (OperationKind.ParameterReference, Type: C) (Syntax: 'init')

                IFlowCaptureOperation: 2 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'limit')
                  Value: 
                    IParameterReferenceOperation: limit (OperationKind.ParameterReference, Type: C) (Syntax: 'limit')

                IFlowCaptureOperation: 3 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: '[step]')
                  Value: 
                    IParameterReferenceOperation: step (OperationKind.ParameterReference, Type: C) (Syntax: '[step]')

                IFlowCaptureOperation: 4 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'For i = ini ... Step [step]')
                  Value: 
                    IUnaryOperation (UnaryOperatorKind.True) (OperatorMethod: Function C.op_True(x As C) As System.Boolean) (OperationKind.Unary, Type: System.Boolean, IsImplicit) (Syntax: 'For i = ini ... Step [step]')
                      Operand: 
                        IBinaryOperation (BinaryOperatorKind.GreaterThanOrEqual, Checked) (OperatorMethod: Function C.op_GreaterThanOrEqual(x As C, y As C) As C) (OperationKind.Binary, Type: C, IsImplicit) (Syntax: 'For i = ini ... Step [step]')
                          Left: 
                            IFlowCaptureReferenceOperation: 3 (OperationKind.FlowCaptureReference, Type: C, IsImplicit) (Syntax: '[step]')
                          Right: 
                            IBinaryOperation (BinaryOperatorKind.Subtract, Checked) (OperatorMethod: Function C.op_Subtraction(x As C, y As C) As C) (OperationKind.Binary, Type: C, IsImplicit) (Syntax: 'For i = ini ... Step [step]')
                              Left: 
                                IFlowCaptureReferenceOperation: 3 (OperationKind.FlowCaptureReference, Type: C, IsImplicit) (Syntax: '[step]')
                              Right: 
                                IFlowCaptureReferenceOperation: 3 (OperationKind.FlowCaptureReference, Type: C, IsImplicit) (Syntax: '[step]')

                ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: null, IsImplicit) (Syntax: 'init')
                  Left: 
                    IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: C, IsImplicit) (Syntax: 'i')
                  Right: 
                    IFlowCaptureReferenceOperation: 1 (OperationKind.FlowCaptureReference, Type: C, IsImplicit) (Syntax: 'init')

            Next (Regular) Block[B2]
                Leaving: {R2}
                Entering: {R3}
    }
    .locals {R3}
    {
        CaptureIds: [5]
        Block[B2] - Block
            Predecessors: [B1] [B5]
            Statements (1)
                IFlowCaptureOperation: 5 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'i')
                  Value: 
                    IParameterReferenceOperation: i (OperationKind.ParameterReference, Type: C, IsImplicit) (Syntax: 'i')

            Jump if False (Regular) to Block[B4]
                IFlowCaptureReferenceOperation: 4 (OperationKind.FlowCaptureReference, Type: System.Boolean, IsImplicit) (Syntax: 'For i = ini ... Step [step]')

            Next (Regular) Block[B3]
        Block[B3] - Block
            Predecessors: [B2]
            Statements (0)
            Jump if False (Regular) to Block[B6]
                IUnaryOperation (UnaryOperatorKind.True) (OperatorMethod: Function C.op_True(x As C) As System.Boolean) (OperationKind.Unary, Type: System.Boolean, IsImplicit) (Syntax: 'For i = ini ... Step [step]')
                  Operand: 
                    IBinaryOperation (BinaryOperatorKind.LessThanOrEqual, Checked) (OperatorMethod: Function C.op_LessThanOrEqual(x As C, y As C) As C) (OperationKind.Binary, Type: C, IsImplicit) (Syntax: 'For i = ini ... Step [step]')
                      Left: 
                        IFlowCaptureReferenceOperation: 5 (OperationKind.FlowCaptureReference, Type: C, IsImplicit) (Syntax: 'i')
                      Right: 
                        IFlowCaptureReferenceOperation: 2 (OperationKind.FlowCaptureReference, Type: C, IsImplicit) (Syntax: 'limit')
                Leaving: {R3} {R1}

            Next (Regular) Block[B5]
                Leaving: {R3}
        Block[B4] - Block
            Predecessors: [B2]
            Statements (0)
            Jump if False (Regular) to Block[B6]
                IUnaryOperation (UnaryOperatorKind.True) (OperatorMethod: Function C.op_True(x As C) As System.Boolean) (OperationKind.Unary, Type: System.Boolean, IsImplicit) (Syntax: 'For i = ini ... Step [step]')
                  Operand: 
                    IBinaryOperation (BinaryOperatorKind.GreaterThanOrEqual, Checked) (OperatorMethod: Function C.op_GreaterThanOrEqual(x As C, y As C) As C) (OperationKind.Binary, Type: C, IsImplicit) (Syntax: 'For i = ini ... Step [step]')
                      Left: 
                        IFlowCaptureReferenceOperation: 5 (OperationKind.FlowCaptureReference, Type: C, IsImplicit) (Syntax: 'i')
                      Right: 
                        IFlowCaptureReferenceOperation: 2 (OperationKind.FlowCaptureReference, Type: C, IsImplicit) (Syntax: 'limit')
                Leaving: {R3} {R1}

            Next (Regular) Block[B5]
                Leaving: {R3}
    }

    Block[B5] - Block
        Predecessors: [B3] [B4]
        Statements (2)
            IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'result = i')
              Expression: 
                ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: C, IsImplicit) (Syntax: 'result = i')
                  Left: 
                    IParameterReferenceOperation: result (OperationKind.ParameterReference, Type: C) (Syntax: 'result')
                  Right: 
                    IParameterReferenceOperation: i (OperationKind.ParameterReference, Type: C) (Syntax: 'i')

            ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: null, IsImplicit) (Syntax: 'i')
              Left: 
                IParameterReferenceOperation: i (OperationKind.ParameterReference, Type: C, IsImplicit) (Syntax: 'i')
              Right: 
                IBinaryOperation (BinaryOperatorKind.Add, Checked) (OperatorMethod: Function C.op_Addition(x As C, y As C) As C) (OperationKind.Binary, Type: C, IsImplicit) (Syntax: 'For i = ini ... Step [step]')
                  Left: 
                    IParameterReferenceOperation: i (OperationKind.ParameterReference, Type: C, IsImplicit) (Syntax: 'i')
                  Right: 
                    IFlowCaptureReferenceOperation: 3 (OperationKind.FlowCaptureReference, Type: C, IsImplicit) (Syntax: '[step]')

        Next (Regular) Block[B2]
            Entering: {R3}
}

Block[B6] - Exit
    Predecessors: [B3] [B4]
    Statements (0)
]]>.Value

            VerifyFlowGraphAndDiagnosticsForTest(Of MethodBlockSyntax)(source, expectedFlowGraph, expectedDiagnostics)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation, CompilerFeature.Dataflow)>
        <Fact()>
        Public Sub ForToFlow_28()
            Dim source = <![CDATA[
Imports System
Public Structure C
    Sub M(i As C?, init As C?, limit As C?, [step] As C?, result As C?) 'BIND:"Sub M"
        For i = init To limit Step [step]
            result = i
        Next
    End Sub

    Public Shared Operator >=(x As C, y As C) As Boolean
        Return False
    End Operator

    Public Shared Operator <=(x As C, y As C) As Boolean
        Return False
    End Operator

    Public Shared Operator -(x As C, y As C) As C
        Return Nothing
    End Operator

    Public Shared Operator +(x As C, y As C) As C
        Return Nothing
    End Operator
End Structure
]]>.Value

            Dim expectedDiagnostics = String.Empty

            Dim expectedFlowGraph = <![CDATA[
Block[B0] - Entry
    Statements (0)
    Next (Regular) Block[B1]
        Entering: {R1} {R2}

.locals {R1}
{
    CaptureIds: [2] [3] [4]
    .locals {R2}
    {
        CaptureIds: [0] [1]
        Block[B1] - Block
            Predecessors: [B0]
            Statements (6)
                IFlowCaptureOperation: 0 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'i')
                  Value: 
                    IParameterReferenceOperation: i (OperationKind.ParameterReference, Type: System.Nullable(Of C)) (Syntax: 'i')

                IFlowCaptureOperation: 1 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'init')
                  Value: 
                    IParameterReferenceOperation: init (OperationKind.ParameterReference, Type: System.Nullable(Of C)) (Syntax: 'init')

                IFlowCaptureOperation: 2 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'limit')
                  Value: 
                    IParameterReferenceOperation: limit (OperationKind.ParameterReference, Type: System.Nullable(Of C)) (Syntax: 'limit')

                IFlowCaptureOperation: 3 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: '[step]')
                  Value: 
                    IParameterReferenceOperation: step (OperationKind.ParameterReference, Type: System.Nullable(Of C)) (Syntax: '[step]')

                IFlowCaptureOperation: 4 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'For i = ini ... Step [step]')
                  Value: 
                    IInvocationOperation ( Function System.Nullable(Of System.Boolean).GetValueOrDefault() As System.Boolean) (OperationKind.Invocation, Type: System.Boolean, IsImplicit) (Syntax: 'For i = ini ... Step [step]')
                      Instance Receiver: 
                        IBinaryOperation (BinaryOperatorKind.GreaterThanOrEqual, IsLifted, Checked) (OperatorMethod: Function C.op_GreaterThanOrEqual(x As C, y As C) As System.Boolean) (OperationKind.Binary, Type: System.Nullable(Of System.Boolean), IsImplicit) (Syntax: 'For i = ini ... Step [step]')
                          Left: 
                            IFlowCaptureReferenceOperation: 3 (OperationKind.FlowCaptureReference, Type: System.Nullable(Of C), IsImplicit) (Syntax: '[step]')
                          Right: 
                            IBinaryOperation (BinaryOperatorKind.Subtract, IsLifted, Checked) (OperatorMethod: Function C.op_Subtraction(x As C, y As C) As C) (OperationKind.Binary, Type: System.Nullable(Of C), IsImplicit) (Syntax: 'For i = ini ... Step [step]')
                              Left: 
                                IFlowCaptureReferenceOperation: 3 (OperationKind.FlowCaptureReference, Type: System.Nullable(Of C), IsImplicit) (Syntax: '[step]')
                              Right: 
                                IFlowCaptureReferenceOperation: 3 (OperationKind.FlowCaptureReference, Type: System.Nullable(Of C), IsImplicit) (Syntax: '[step]')
                      Arguments(0)

                ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: null, IsImplicit) (Syntax: 'init')
                  Left: 
                    IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: System.Nullable(Of C), IsImplicit) (Syntax: 'i')
                  Right: 
                    IFlowCaptureReferenceOperation: 1 (OperationKind.FlowCaptureReference, Type: System.Nullable(Of C), IsImplicit) (Syntax: 'init')

            Next (Regular) Block[B2]
                Leaving: {R2}
                Entering: {R3}
    }
    .locals {R3}
    {
        CaptureIds: [5]
        Block[B2] - Block
            Predecessors: [B1] [B5]
            Statements (1)
                IFlowCaptureOperation: 5 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'i')
                  Value: 
                    IParameterReferenceOperation: i (OperationKind.ParameterReference, Type: System.Nullable(Of C), IsImplicit) (Syntax: 'i')

            Jump if False (Regular) to Block[B4]
                IFlowCaptureReferenceOperation: 4 (OperationKind.FlowCaptureReference, Type: System.Boolean, IsImplicit) (Syntax: 'For i = ini ... Step [step]')

            Next (Regular) Block[B3]
        Block[B3] - Block
            Predecessors: [B2]
            Statements (0)
            Jump if False (Regular) to Block[B6]
                IInvocationOperation ( Function System.Nullable(Of System.Boolean).GetValueOrDefault() As System.Boolean) (OperationKind.Invocation, Type: System.Boolean, IsImplicit) (Syntax: 'For i = ini ... Step [step]')
                  Instance Receiver: 
                    IBinaryOperation (BinaryOperatorKind.LessThanOrEqual, IsLifted, Checked) (OperatorMethod: Function C.op_LessThanOrEqual(x As C, y As C) As System.Boolean) (OperationKind.Binary, Type: System.Nullable(Of System.Boolean), IsImplicit) (Syntax: 'For i = ini ... Step [step]')
                      Left: 
                        IFlowCaptureReferenceOperation: 5 (OperationKind.FlowCaptureReference, Type: System.Nullable(Of C), IsImplicit) (Syntax: 'i')
                      Right: 
                        IFlowCaptureReferenceOperation: 2 (OperationKind.FlowCaptureReference, Type: System.Nullable(Of C), IsImplicit) (Syntax: 'limit')
                  Arguments(0)
                Leaving: {R3} {R1}

            Next (Regular) Block[B5]
                Leaving: {R3}
        Block[B4] - Block
            Predecessors: [B2]
            Statements (0)
            Jump if False (Regular) to Block[B6]
                IInvocationOperation ( Function System.Nullable(Of System.Boolean).GetValueOrDefault() As System.Boolean) (OperationKind.Invocation, Type: System.Boolean, IsImplicit) (Syntax: 'For i = ini ... Step [step]')
                  Instance Receiver: 
                    IBinaryOperation (BinaryOperatorKind.GreaterThanOrEqual, IsLifted, Checked) (OperatorMethod: Function C.op_GreaterThanOrEqual(x As C, y As C) As System.Boolean) (OperationKind.Binary, Type: System.Nullable(Of System.Boolean), IsImplicit) (Syntax: 'For i = ini ... Step [step]')
                      Left: 
                        IFlowCaptureReferenceOperation: 5 (OperationKind.FlowCaptureReference, Type: System.Nullable(Of C), IsImplicit) (Syntax: 'i')
                      Right: 
                        IFlowCaptureReferenceOperation: 2 (OperationKind.FlowCaptureReference, Type: System.Nullable(Of C), IsImplicit) (Syntax: 'limit')
                  Arguments(0)
                Leaving: {R3} {R1}

            Next (Regular) Block[B5]
                Leaving: {R3}
    }

    Block[B5] - Block
        Predecessors: [B3] [B4]
        Statements (2)
            IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'result = i')
              Expression: 
                ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Nullable(Of C), IsImplicit) (Syntax: 'result = i')
                  Left: 
                    IParameterReferenceOperation: result (OperationKind.ParameterReference, Type: System.Nullable(Of C)) (Syntax: 'result')
                  Right: 
                    IParameterReferenceOperation: i (OperationKind.ParameterReference, Type: System.Nullable(Of C)) (Syntax: 'i')

            ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: null, IsImplicit) (Syntax: 'i')
              Left: 
                IParameterReferenceOperation: i (OperationKind.ParameterReference, Type: System.Nullable(Of C), IsImplicit) (Syntax: 'i')
              Right: 
                IBinaryOperation (BinaryOperatorKind.Add, IsLifted, Checked) (OperatorMethod: Function C.op_Addition(x As C, y As C) As C) (OperationKind.Binary, Type: System.Nullable(Of C), IsImplicit) (Syntax: 'For i = ini ... Step [step]')
                  Left: 
                    IParameterReferenceOperation: i (OperationKind.ParameterReference, Type: System.Nullable(Of C), IsImplicit) (Syntax: 'i')
                  Right: 
                    IFlowCaptureReferenceOperation: 3 (OperationKind.FlowCaptureReference, Type: System.Nullable(Of C), IsImplicit) (Syntax: '[step]')

        Next (Regular) Block[B2]
            Entering: {R3}
}

Block[B6] - Exit
    Predecessors: [B3] [B4]
    Statements (0)
]]>.Value

            VerifyFlowGraphAndDiagnosticsForTest(Of MethodBlockSyntax)(source, expectedFlowGraph, expectedDiagnostics)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation, CompilerFeature.Dataflow)>
        <Fact()>
        Public Sub ForToFlow_29()
            Dim source = <![CDATA[
Imports System
Public Class C
    Sub M(c1 As C, c2 As C, bInit As Boolean, init1 As C, init2 As C, bLimit As Boolean, limit1 As C, limit2 As C, bStep As Boolean, step1 As C, step2 As C, result As Boolean) 'BIND:"Sub M"
        For If(c1, c2).i = If(bInit, init1, init2) To If(bLimit, limit1, limit2) Step If(bStep, step1, step2)
            result = true
        Next
    End Sub

    Public i As C

    Public Shared Operator >=(x As C, y As C) As Boolean
        Return False
    End Operator

    Public Shared Operator <=(x As C, y As C) As Boolean
        Return False
    End Operator

    Public Shared Operator -(x As C, y As C) As C
        Return Nothing
    End Operator

    Public Shared Operator +(x As C, y As C) As C
        Return Nothing
    End Operator
End Class
]]>.Value

            Dim expectedDiagnostics = String.Empty

            Dim expectedFlowGraph = <![CDATA[
Block[B0] - Entry
    Statements (0)
    Next (Regular) Block[B1]
        Entering: {R1} {R2} {R3} {R4}

.locals {R1}
{
    CaptureIds: [4] [5] [6]
    .locals {R2}
    {
        CaptureIds: [2] [3]
        .locals {R3}
        {
            CaptureIds: [1]
            .locals {R4}
            {
                CaptureIds: [0]
                Block[B1] - Block
                    Predecessors: [B0]
                    Statements (1)
                        IFlowCaptureOperation: 0 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'c1')
                          Value: 
                            IParameterReferenceOperation: c1 (OperationKind.ParameterReference, Type: C) (Syntax: 'c1')

                    Jump if True (Regular) to Block[B3]
                        IIsNullOperation (OperationKind.IsNull, Type: System.Boolean, IsImplicit) (Syntax: 'c1')
                          Operand: 
                            IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: C, IsImplicit) (Syntax: 'c1')
                        Leaving: {R4}

                    Next (Regular) Block[B2]
                Block[B2] - Block
                    Predecessors: [B1]
                    Statements (1)
                        IFlowCaptureOperation: 1 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'c1')
                          Value: 
                            IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: C, IsImplicit) (Syntax: 'c1')

                    Next (Regular) Block[B4]
                        Leaving: {R4}
            }

            Block[B3] - Block
                Predecessors: [B1]
                Statements (1)
                    IFlowCaptureOperation: 1 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'c2')
                      Value: 
                        IParameterReferenceOperation: c2 (OperationKind.ParameterReference, Type: C) (Syntax: 'c2')

                Next (Regular) Block[B4]
            Block[B4] - Block
                Predecessors: [B2] [B3]
                Statements (1)
                    IFlowCaptureOperation: 2 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'If(c1, c2).i')
                      Value: 
                        IFieldReferenceOperation: C.i As C (OperationKind.FieldReference, Type: C) (Syntax: 'If(c1, c2).i')
                          Instance Receiver: 
                            IFlowCaptureReferenceOperation: 1 (OperationKind.FlowCaptureReference, Type: C, IsImplicit) (Syntax: 'If(c1, c2)')

                Next (Regular) Block[B5]
                    Leaving: {R3}
        }

        Block[B5] - Block
            Predecessors: [B4]
            Statements (0)
            Jump if False (Regular) to Block[B7]
                IParameterReferenceOperation: bInit (OperationKind.ParameterReference, Type: System.Boolean) (Syntax: 'bInit')

            Next (Regular) Block[B6]
        Block[B6] - Block
            Predecessors: [B5]
            Statements (1)
                IFlowCaptureOperation: 3 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'init1')
                  Value: 
                    IParameterReferenceOperation: init1 (OperationKind.ParameterReference, Type: C) (Syntax: 'init1')

            Next (Regular) Block[B8]
        Block[B7] - Block
            Predecessors: [B5]
            Statements (1)
                IFlowCaptureOperation: 3 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'init2')
                  Value: 
                    IParameterReferenceOperation: init2 (OperationKind.ParameterReference, Type: C) (Syntax: 'init2')

            Next (Regular) Block[B8]
        Block[B8] - Block
            Predecessors: [B6] [B7]
            Statements (0)
            Jump if False (Regular) to Block[B10]
                IParameterReferenceOperation: bLimit (OperationKind.ParameterReference, Type: System.Boolean) (Syntax: 'bLimit')

            Next (Regular) Block[B9]
        Block[B9] - Block
            Predecessors: [B8]
            Statements (1)
                IFlowCaptureOperation: 4 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'limit1')
                  Value: 
                    IParameterReferenceOperation: limit1 (OperationKind.ParameterReference, Type: C) (Syntax: 'limit1')

            Next (Regular) Block[B11]
        Block[B10] - Block
            Predecessors: [B8]
            Statements (1)
                IFlowCaptureOperation: 4 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'limit2')
                  Value: 
                    IParameterReferenceOperation: limit2 (OperationKind.ParameterReference, Type: C) (Syntax: 'limit2')

            Next (Regular) Block[B11]
        Block[B11] - Block
            Predecessors: [B9] [B10]
            Statements (0)
            Jump if False (Regular) to Block[B13]
                IParameterReferenceOperation: bStep (OperationKind.ParameterReference, Type: System.Boolean) (Syntax: 'bStep')

            Next (Regular) Block[B12]
        Block[B12] - Block
            Predecessors: [B11]
            Statements (1)
                IFlowCaptureOperation: 5 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'step1')
                  Value: 
                    IParameterReferenceOperation: step1 (OperationKind.ParameterReference, Type: C) (Syntax: 'step1')

            Next (Regular) Block[B14]
        Block[B13] - Block
            Predecessors: [B11]
            Statements (1)
                IFlowCaptureOperation: 5 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'step2')
                  Value: 
                    IParameterReferenceOperation: step2 (OperationKind.ParameterReference, Type: C) (Syntax: 'step2')

            Next (Regular) Block[B14]
        Block[B14] - Block
            Predecessors: [B12] [B13]
            Statements (2)
                IFlowCaptureOperation: 6 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'For If(c1,  ... ep1, step2)')
                  Value: 
                    IBinaryOperation (BinaryOperatorKind.GreaterThanOrEqual, Checked) (OperatorMethod: Function C.op_GreaterThanOrEqual(x As C, y As C) As System.Boolean) (OperationKind.Binary, Type: System.Boolean, IsImplicit) (Syntax: 'For If(c1,  ... ep1, step2)')
                      Left: 
                        IFlowCaptureReferenceOperation: 5 (OperationKind.FlowCaptureReference, Type: C, IsImplicit) (Syntax: 'If(bStep, step1, step2)')
                      Right: 
                        IBinaryOperation (BinaryOperatorKind.Subtract, Checked) (OperatorMethod: Function C.op_Subtraction(x As C, y As C) As C) (OperationKind.Binary, Type: C, IsImplicit) (Syntax: 'For If(c1,  ... ep1, step2)')
                          Left: 
                            IFlowCaptureReferenceOperation: 5 (OperationKind.FlowCaptureReference, Type: C, IsImplicit) (Syntax: 'If(bStep, step1, step2)')
                          Right: 
                            IFlowCaptureReferenceOperation: 5 (OperationKind.FlowCaptureReference, Type: C, IsImplicit) (Syntax: 'If(bStep, step1, step2)')

                ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: null, IsImplicit) (Syntax: 'If(bInit, init1, init2)')
                  Left: 
                    IFlowCaptureReferenceOperation: 2 (OperationKind.FlowCaptureReference, Type: C, IsImplicit) (Syntax: 'If(c1, c2).i')
                  Right: 
                    IFlowCaptureReferenceOperation: 3 (OperationKind.FlowCaptureReference, Type: C, IsImplicit) (Syntax: 'If(bInit, init1, init2)')

            Next (Regular) Block[B15]
                Leaving: {R2}
                Entering: {R5} {R6} {R7}
    }
    .locals {R5}
    {
        CaptureIds: [9]
        .locals {R6}
        {
            CaptureIds: [8]
            .locals {R7}
            {
                CaptureIds: [7]
                Block[B15] - Block
                    Predecessors: [B14] [B30]
                    Statements (1)
                        IFlowCaptureOperation: 7 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'c1')
                          Value: 
                            IParameterReferenceOperation: c1 (OperationKind.ParameterReference, Type: C, IsImplicit) (Syntax: 'c1')

                    Jump if True (Regular) to Block[B17]
                        IIsNullOperation (OperationKind.IsNull, Type: System.Boolean, IsImplicit) (Syntax: 'c1')
                          Operand: 
                            IFlowCaptureReferenceOperation: 7 (OperationKind.FlowCaptureReference, Type: C, IsImplicit) (Syntax: 'c1')
                        Leaving: {R7}

                    Next (Regular) Block[B16]
                Block[B16] - Block
                    Predecessors: [B15]
                    Statements (1)
                        IFlowCaptureOperation: 8 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'c1')
                          Value: 
                            IFlowCaptureReferenceOperation: 7 (OperationKind.FlowCaptureReference, Type: C, IsImplicit) (Syntax: 'c1')

                    Next (Regular) Block[B18]
                        Leaving: {R7}
            }

            Block[B17] - Block
                Predecessors: [B15]
                Statements (1)
                    IFlowCaptureOperation: 8 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'c2')
                      Value: 
                        IParameterReferenceOperation: c2 (OperationKind.ParameterReference, Type: C, IsImplicit) (Syntax: 'c2')

                Next (Regular) Block[B18]
            Block[B18] - Block
                Predecessors: [B16] [B17]
                Statements (1)
                    IFlowCaptureOperation: 9 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'If(c1, c2).i')
                      Value: 
                        IFieldReferenceOperation: C.i As C (OperationKind.FieldReference, Type: C, IsImplicit) (Syntax: 'If(c1, c2).i')
                          Instance Receiver: 
                            IFlowCaptureReferenceOperation: 8 (OperationKind.FlowCaptureReference, Type: C, IsImplicit) (Syntax: 'If(c1, c2)')

                Next (Regular) Block[B19]
                    Leaving: {R6}
        }

        Block[B19] - Block
            Predecessors: [B18]
            Statements (0)
            Jump if False (Regular) to Block[B21]
                IFlowCaptureReferenceOperation: 6 (OperationKind.FlowCaptureReference, Type: System.Boolean, IsImplicit) (Syntax: 'For If(c1,  ... ep1, step2)')

            Next (Regular) Block[B20]
        Block[B20] - Block
            Predecessors: [B19]
            Statements (0)
            Jump if False (Regular) to Block[B31]
                IBinaryOperation (BinaryOperatorKind.LessThanOrEqual, Checked) (OperatorMethod: Function C.op_LessThanOrEqual(x As C, y As C) As System.Boolean) (OperationKind.Binary, Type: System.Boolean, IsImplicit) (Syntax: 'For If(c1,  ... ep1, step2)')
                  Left: 
                    IFlowCaptureReferenceOperation: 9 (OperationKind.FlowCaptureReference, Type: C, IsImplicit) (Syntax: 'If(c1, c2).i')
                  Right: 
                    IFlowCaptureReferenceOperation: 4 (OperationKind.FlowCaptureReference, Type: C, IsImplicit) (Syntax: 'If(bLimit,  ... t1, limit2)')
                Leaving: {R5} {R1}

            Next (Regular) Block[B22]
                Leaving: {R5}
        Block[B21] - Block
            Predecessors: [B19]
            Statements (0)
            Jump if False (Regular) to Block[B31]
                IBinaryOperation (BinaryOperatorKind.GreaterThanOrEqual, Checked) (OperatorMethod: Function C.op_GreaterThanOrEqual(x As C, y As C) As System.Boolean) (OperationKind.Binary, Type: System.Boolean, IsImplicit) (Syntax: 'For If(c1,  ... ep1, step2)')
                  Left: 
                    IFlowCaptureReferenceOperation: 9 (OperationKind.FlowCaptureReference, Type: C, IsImplicit) (Syntax: 'If(c1, c2).i')
                  Right: 
                    IFlowCaptureReferenceOperation: 4 (OperationKind.FlowCaptureReference, Type: C, IsImplicit) (Syntax: 'If(bLimit,  ... t1, limit2)')
                Leaving: {R5} {R1}

            Next (Regular) Block[B22]
                Leaving: {R5}
    }

    Block[B22] - Block
        Predecessors: [B20] [B21]
        Statements (1)
            IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'result = true')
              Expression: 
                ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Boolean, IsImplicit) (Syntax: 'result = true')
                  Left: 
                    IParameterReferenceOperation: result (OperationKind.ParameterReference, Type: System.Boolean) (Syntax: 'result')
                  Right: 
                    ILiteralOperation (OperationKind.Literal, Type: System.Boolean, Constant: True) (Syntax: 'true')

        Next (Regular) Block[B23]
            Entering: {R8} {R9} {R10}

    .locals {R8}
    {
        CaptureIds: [12] [14]
        .locals {R9}
        {
            CaptureIds: [11]
            .locals {R10}
            {
                CaptureIds: [10]
                Block[B23] - Block
                    Predecessors: [B22]
                    Statements (1)
                        IFlowCaptureOperation: 10 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'c1')
                          Value: 
                            IParameterReferenceOperation: c1 (OperationKind.ParameterReference, Type: C, IsImplicit) (Syntax: 'c1')

                    Jump if True (Regular) to Block[B25]
                        IIsNullOperation (OperationKind.IsNull, Type: System.Boolean, IsImplicit) (Syntax: 'c1')
                          Operand: 
                            IFlowCaptureReferenceOperation: 10 (OperationKind.FlowCaptureReference, Type: C, IsImplicit) (Syntax: 'c1')
                        Leaving: {R10}

                    Next (Regular) Block[B24]
                Block[B24] - Block
                    Predecessors: [B23]
                    Statements (1)
                        IFlowCaptureOperation: 11 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'c1')
                          Value: 
                            IFlowCaptureReferenceOperation: 10 (OperationKind.FlowCaptureReference, Type: C, IsImplicit) (Syntax: 'c1')

                    Next (Regular) Block[B26]
                        Leaving: {R10}
            }

            Block[B25] - Block
                Predecessors: [B23]
                Statements (1)
                    IFlowCaptureOperation: 11 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'c2')
                      Value: 
                        IParameterReferenceOperation: c2 (OperationKind.ParameterReference, Type: C, IsImplicit) (Syntax: 'c2')

                Next (Regular) Block[B26]
            Block[B26] - Block
                Predecessors: [B24] [B25]
                Statements (1)
                    IFlowCaptureOperation: 12 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'If(c1, c2).i')
                      Value: 
                        IFieldReferenceOperation: C.i As C (OperationKind.FieldReference, Type: C, IsImplicit) (Syntax: 'If(c1, c2).i')
                          Instance Receiver: 
                            IFlowCaptureReferenceOperation: 11 (OperationKind.FlowCaptureReference, Type: C, IsImplicit) (Syntax: 'If(c1, c2)')

                Next (Regular) Block[B27]
                    Leaving: {R9}
                    Entering: {R11}
        }
        .locals {R11}
        {
            CaptureIds: [13]
            Block[B27] - Block
                Predecessors: [B26]
                Statements (1)
                    IFlowCaptureOperation: 13 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'c1')
                      Value: 
                        IParameterReferenceOperation: c1 (OperationKind.ParameterReference, Type: C, IsImplicit) (Syntax: 'c1')

                Jump if True (Regular) to Block[B29]
                    IIsNullOperation (OperationKind.IsNull, Type: System.Boolean, IsImplicit) (Syntax: 'c1')
                      Operand: 
                        IFlowCaptureReferenceOperation: 13 (OperationKind.FlowCaptureReference, Type: C, IsImplicit) (Syntax: 'c1')
                    Leaving: {R11}

                Next (Regular) Block[B28]
            Block[B28] - Block
                Predecessors: [B27]
                Statements (1)
                    IFlowCaptureOperation: 14 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'c1')
                      Value: 
                        IFlowCaptureReferenceOperation: 13 (OperationKind.FlowCaptureReference, Type: C, IsImplicit) (Syntax: 'c1')

                Next (Regular) Block[B30]
                    Leaving: {R11}
        }

        Block[B29] - Block
            Predecessors: [B27]
            Statements (1)
                IFlowCaptureOperation: 14 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'c2')
                  Value: 
                    IParameterReferenceOperation: c2 (OperationKind.ParameterReference, Type: C, IsImplicit) (Syntax: 'c2')

            Next (Regular) Block[B30]
        Block[B30] - Block
            Predecessors: [B28] [B29]
            Statements (1)
                ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: null, IsImplicit) (Syntax: 'If(c1, c2).i')
                  Left: 
                    IFlowCaptureReferenceOperation: 12 (OperationKind.FlowCaptureReference, Type: C, IsImplicit) (Syntax: 'If(c1, c2).i')
                  Right: 
                    IBinaryOperation (BinaryOperatorKind.Add, Checked) (OperatorMethod: Function C.op_Addition(x As C, y As C) As C) (OperationKind.Binary, Type: C, IsImplicit) (Syntax: 'For If(c1,  ... ep1, step2)')
                      Left: 
                        IFieldReferenceOperation: C.i As C (OperationKind.FieldReference, Type: C, IsImplicit) (Syntax: 'If(c1, c2).i')
                          Instance Receiver: 
                            IFlowCaptureReferenceOperation: 14 (OperationKind.FlowCaptureReference, Type: C, IsImplicit) (Syntax: 'If(c1, c2)')
                      Right: 
                        IFlowCaptureReferenceOperation: 5 (OperationKind.FlowCaptureReference, Type: C, IsImplicit) (Syntax: 'If(bStep, step1, step2)')

            Next (Regular) Block[B15]
                Leaving: {R8}
                Entering: {R5} {R6} {R7}
    }
}

Block[B31] - Exit
    Predecessors: [B20] [B21]
    Statements (0)
]]>.Value

            VerifyFlowGraphAndDiagnosticsForTest(Of MethodBlockSyntax)(source, expectedFlowGraph, expectedDiagnostics)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation, CompilerFeature.Dataflow)>
        <Fact()>
        Public Sub ForToFlow_30()
            Dim source = <![CDATA[
Imports System
Public Class C1
    Sub M(c1 As C1, c2 As C1, bInit As Boolean, init1 As C?, init2 As C?, bLimit As Boolean, limit1 As C?, limit2 As C?, bStep As Boolean, step1 As C?, step2 As C?, result As Boolean) 'BIND:"Sub M"
        For If(c1, c2).i = If(bInit, init1, init2) To If(bLimit, limit1, limit2) Step If(bStep, step1, step2)
            result = true
        Next
    End Sub

    Public i As C?
End Class

Public Structure C

    Public Shared Operator >=(x As C, y As C) As Boolean
        Return False
    End Operator

    Public Shared Operator <=(x As C, y As C) As Boolean
        Return False
    End Operator

    Public Shared Operator -(x As C, y As C) As C
        Return Nothing
    End Operator

    Public Shared Operator +(x As C, y As C) As C
        Return Nothing
    End Operator
End Structure
]]>.Value

            Dim expectedDiagnostics = String.Empty

            Dim expectedFlowGraph = <![CDATA[
Block[B0] - Entry
    Statements (0)
    Next (Regular) Block[B1]
        Entering: {R1} {R2} {R3} {R4}

.locals {R1}
{
    CaptureIds: [4] [5] [6]
    .locals {R2}
    {
        CaptureIds: [2] [3]
        .locals {R3}
        {
            CaptureIds: [1]
            .locals {R4}
            {
                CaptureIds: [0]
                Block[B1] - Block
                    Predecessors: [B0]
                    Statements (1)
                        IFlowCaptureOperation: 0 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'c1')
                          Value: 
                            IParameterReferenceOperation: c1 (OperationKind.ParameterReference, Type: C1) (Syntax: 'c1')

                    Jump if True (Regular) to Block[B3]
                        IIsNullOperation (OperationKind.IsNull, Type: System.Boolean, IsImplicit) (Syntax: 'c1')
                          Operand: 
                            IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: C1, IsImplicit) (Syntax: 'c1')
                        Leaving: {R4}

                    Next (Regular) Block[B2]
                Block[B2] - Block
                    Predecessors: [B1]
                    Statements (1)
                        IFlowCaptureOperation: 1 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'c1')
                          Value: 
                            IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: C1, IsImplicit) (Syntax: 'c1')

                    Next (Regular) Block[B4]
                        Leaving: {R4}
            }

            Block[B3] - Block
                Predecessors: [B1]
                Statements (1)
                    IFlowCaptureOperation: 1 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'c2')
                      Value: 
                        IParameterReferenceOperation: c2 (OperationKind.ParameterReference, Type: C1) (Syntax: 'c2')

                Next (Regular) Block[B4]
            Block[B4] - Block
                Predecessors: [B2] [B3]
                Statements (1)
                    IFlowCaptureOperation: 2 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'If(c1, c2).i')
                      Value: 
                        IFieldReferenceOperation: C1.i As System.Nullable(Of C) (OperationKind.FieldReference, Type: System.Nullable(Of C)) (Syntax: 'If(c1, c2).i')
                          Instance Receiver: 
                            IFlowCaptureReferenceOperation: 1 (OperationKind.FlowCaptureReference, Type: C1, IsImplicit) (Syntax: 'If(c1, c2)')

                Next (Regular) Block[B5]
                    Leaving: {R3}
        }

        Block[B5] - Block
            Predecessors: [B4]
            Statements (0)
            Jump if False (Regular) to Block[B7]
                IParameterReferenceOperation: bInit (OperationKind.ParameterReference, Type: System.Boolean) (Syntax: 'bInit')

            Next (Regular) Block[B6]
        Block[B6] - Block
            Predecessors: [B5]
            Statements (1)
                IFlowCaptureOperation: 3 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'init1')
                  Value: 
                    IParameterReferenceOperation: init1 (OperationKind.ParameterReference, Type: System.Nullable(Of C)) (Syntax: 'init1')

            Next (Regular) Block[B8]
        Block[B7] - Block
            Predecessors: [B5]
            Statements (1)
                IFlowCaptureOperation: 3 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'init2')
                  Value: 
                    IParameterReferenceOperation: init2 (OperationKind.ParameterReference, Type: System.Nullable(Of C)) (Syntax: 'init2')

            Next (Regular) Block[B8]
        Block[B8] - Block
            Predecessors: [B6] [B7]
            Statements (0)
            Jump if False (Regular) to Block[B10]
                IParameterReferenceOperation: bLimit (OperationKind.ParameterReference, Type: System.Boolean) (Syntax: 'bLimit')

            Next (Regular) Block[B9]
        Block[B9] - Block
            Predecessors: [B8]
            Statements (1)
                IFlowCaptureOperation: 4 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'limit1')
                  Value: 
                    IParameterReferenceOperation: limit1 (OperationKind.ParameterReference, Type: System.Nullable(Of C)) (Syntax: 'limit1')

            Next (Regular) Block[B11]
        Block[B10] - Block
            Predecessors: [B8]
            Statements (1)
                IFlowCaptureOperation: 4 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'limit2')
                  Value: 
                    IParameterReferenceOperation: limit2 (OperationKind.ParameterReference, Type: System.Nullable(Of C)) (Syntax: 'limit2')

            Next (Regular) Block[B11]
        Block[B11] - Block
            Predecessors: [B9] [B10]
            Statements (0)
            Jump if False (Regular) to Block[B13]
                IParameterReferenceOperation: bStep (OperationKind.ParameterReference, Type: System.Boolean) (Syntax: 'bStep')

            Next (Regular) Block[B12]
        Block[B12] - Block
            Predecessors: [B11]
            Statements (1)
                IFlowCaptureOperation: 5 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'step1')
                  Value: 
                    IParameterReferenceOperation: step1 (OperationKind.ParameterReference, Type: System.Nullable(Of C)) (Syntax: 'step1')

            Next (Regular) Block[B14]
        Block[B13] - Block
            Predecessors: [B11]
            Statements (1)
                IFlowCaptureOperation: 5 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'step2')
                  Value: 
                    IParameterReferenceOperation: step2 (OperationKind.ParameterReference, Type: System.Nullable(Of C)) (Syntax: 'step2')

            Next (Regular) Block[B14]
        Block[B14] - Block
            Predecessors: [B12] [B13]
            Statements (2)
                IFlowCaptureOperation: 6 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'For If(c1,  ... ep1, step2)')
                  Value: 
                    IInvocationOperation ( Function System.Nullable(Of System.Boolean).GetValueOrDefault() As System.Boolean) (OperationKind.Invocation, Type: System.Boolean, IsImplicit) (Syntax: 'For If(c1,  ... ep1, step2)')
                      Instance Receiver: 
                        IBinaryOperation (BinaryOperatorKind.GreaterThanOrEqual, IsLifted, Checked) (OperatorMethod: Function C.op_GreaterThanOrEqual(x As C, y As C) As System.Boolean) (OperationKind.Binary, Type: System.Nullable(Of System.Boolean), IsImplicit) (Syntax: 'For If(c1,  ... ep1, step2)')
                          Left: 
                            IFlowCaptureReferenceOperation: 5 (OperationKind.FlowCaptureReference, Type: System.Nullable(Of C), IsImplicit) (Syntax: 'If(bStep, step1, step2)')
                          Right: 
                            IBinaryOperation (BinaryOperatorKind.Subtract, IsLifted, Checked) (OperatorMethod: Function C.op_Subtraction(x As C, y As C) As C) (OperationKind.Binary, Type: System.Nullable(Of C), IsImplicit) (Syntax: 'For If(c1,  ... ep1, step2)')
                              Left: 
                                IFlowCaptureReferenceOperation: 5 (OperationKind.FlowCaptureReference, Type: System.Nullable(Of C), IsImplicit) (Syntax: 'If(bStep, step1, step2)')
                              Right: 
                                IFlowCaptureReferenceOperation: 5 (OperationKind.FlowCaptureReference, Type: System.Nullable(Of C), IsImplicit) (Syntax: 'If(bStep, step1, step2)')
                      Arguments(0)

                ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: null, IsImplicit) (Syntax: 'If(bInit, init1, init2)')
                  Left: 
                    IFlowCaptureReferenceOperation: 2 (OperationKind.FlowCaptureReference, Type: System.Nullable(Of C), IsImplicit) (Syntax: 'If(c1, c2).i')
                  Right: 
                    IFlowCaptureReferenceOperation: 3 (OperationKind.FlowCaptureReference, Type: System.Nullable(Of C), IsImplicit) (Syntax: 'If(bInit, init1, init2)')

            Next (Regular) Block[B15]
                Leaving: {R2}
                Entering: {R5} {R6} {R7}
    }
    .locals {R5}
    {
        CaptureIds: [9]
        .locals {R6}
        {
            CaptureIds: [8]
            .locals {R7}
            {
                CaptureIds: [7]
                Block[B15] - Block
                    Predecessors: [B14] [B30]
                    Statements (1)
                        IFlowCaptureOperation: 7 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'c1')
                          Value: 
                            IParameterReferenceOperation: c1 (OperationKind.ParameterReference, Type: C1, IsImplicit) (Syntax: 'c1')

                    Jump if True (Regular) to Block[B17]
                        IIsNullOperation (OperationKind.IsNull, Type: System.Boolean, IsImplicit) (Syntax: 'c1')
                          Operand: 
                            IFlowCaptureReferenceOperation: 7 (OperationKind.FlowCaptureReference, Type: C1, IsImplicit) (Syntax: 'c1')
                        Leaving: {R7}

                    Next (Regular) Block[B16]
                Block[B16] - Block
                    Predecessors: [B15]
                    Statements (1)
                        IFlowCaptureOperation: 8 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'c1')
                          Value: 
                            IFlowCaptureReferenceOperation: 7 (OperationKind.FlowCaptureReference, Type: C1, IsImplicit) (Syntax: 'c1')

                    Next (Regular) Block[B18]
                        Leaving: {R7}
            }

            Block[B17] - Block
                Predecessors: [B15]
                Statements (1)
                    IFlowCaptureOperation: 8 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'c2')
                      Value: 
                        IParameterReferenceOperation: c2 (OperationKind.ParameterReference, Type: C1, IsImplicit) (Syntax: 'c2')

                Next (Regular) Block[B18]
            Block[B18] - Block
                Predecessors: [B16] [B17]
                Statements (1)
                    IFlowCaptureOperation: 9 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'If(c1, c2).i')
                      Value: 
                        IFieldReferenceOperation: C1.i As System.Nullable(Of C) (OperationKind.FieldReference, Type: System.Nullable(Of C), IsImplicit) (Syntax: 'If(c1, c2).i')
                          Instance Receiver: 
                            IFlowCaptureReferenceOperation: 8 (OperationKind.FlowCaptureReference, Type: C1, IsImplicit) (Syntax: 'If(c1, c2)')

                Next (Regular) Block[B19]
                    Leaving: {R6}
        }

        Block[B19] - Block
            Predecessors: [B18]
            Statements (0)
            Jump if False (Regular) to Block[B21]
                IFlowCaptureReferenceOperation: 6 (OperationKind.FlowCaptureReference, Type: System.Boolean, IsImplicit) (Syntax: 'For If(c1,  ... ep1, step2)')

            Next (Regular) Block[B20]
        Block[B20] - Block
            Predecessors: [B19]
            Statements (0)
            Jump if False (Regular) to Block[B31]
                IInvocationOperation ( Function System.Nullable(Of System.Boolean).GetValueOrDefault() As System.Boolean) (OperationKind.Invocation, Type: System.Boolean, IsImplicit) (Syntax: 'For If(c1,  ... ep1, step2)')
                  Instance Receiver: 
                    IBinaryOperation (BinaryOperatorKind.LessThanOrEqual, IsLifted, Checked) (OperatorMethod: Function C.op_LessThanOrEqual(x As C, y As C) As System.Boolean) (OperationKind.Binary, Type: System.Nullable(Of System.Boolean), IsImplicit) (Syntax: 'For If(c1,  ... ep1, step2)')
                      Left: 
                        IFlowCaptureReferenceOperation: 9 (OperationKind.FlowCaptureReference, Type: System.Nullable(Of C), IsImplicit) (Syntax: 'If(c1, c2).i')
                      Right: 
                        IFlowCaptureReferenceOperation: 4 (OperationKind.FlowCaptureReference, Type: System.Nullable(Of C), IsImplicit) (Syntax: 'If(bLimit,  ... t1, limit2)')
                  Arguments(0)
                Leaving: {R5} {R1}

            Next (Regular) Block[B22]
                Leaving: {R5}
        Block[B21] - Block
            Predecessors: [B19]
            Statements (0)
            Jump if False (Regular) to Block[B31]
                IInvocationOperation ( Function System.Nullable(Of System.Boolean).GetValueOrDefault() As System.Boolean) (OperationKind.Invocation, Type: System.Boolean, IsImplicit) (Syntax: 'For If(c1,  ... ep1, step2)')
                  Instance Receiver: 
                    IBinaryOperation (BinaryOperatorKind.GreaterThanOrEqual, IsLifted, Checked) (OperatorMethod: Function C.op_GreaterThanOrEqual(x As C, y As C) As System.Boolean) (OperationKind.Binary, Type: System.Nullable(Of System.Boolean), IsImplicit) (Syntax: 'For If(c1,  ... ep1, step2)')
                      Left: 
                        IFlowCaptureReferenceOperation: 9 (OperationKind.FlowCaptureReference, Type: System.Nullable(Of C), IsImplicit) (Syntax: 'If(c1, c2).i')
                      Right: 
                        IFlowCaptureReferenceOperation: 4 (OperationKind.FlowCaptureReference, Type: System.Nullable(Of C), IsImplicit) (Syntax: 'If(bLimit,  ... t1, limit2)')
                  Arguments(0)
                Leaving: {R5} {R1}

            Next (Regular) Block[B22]
                Leaving: {R5}
    }

    Block[B22] - Block
        Predecessors: [B20] [B21]
        Statements (1)
            IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'result = true')
              Expression: 
                ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Boolean, IsImplicit) (Syntax: 'result = true')
                  Left: 
                    IParameterReferenceOperation: result (OperationKind.ParameterReference, Type: System.Boolean) (Syntax: 'result')
                  Right: 
                    ILiteralOperation (OperationKind.Literal, Type: System.Boolean, Constant: True) (Syntax: 'true')

        Next (Regular) Block[B23]
            Entering: {R8} {R9} {R10}

    .locals {R8}
    {
        CaptureIds: [12] [14]
        .locals {R9}
        {
            CaptureIds: [11]
            .locals {R10}
            {
                CaptureIds: [10]
                Block[B23] - Block
                    Predecessors: [B22]
                    Statements (1)
                        IFlowCaptureOperation: 10 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'c1')
                          Value: 
                            IParameterReferenceOperation: c1 (OperationKind.ParameterReference, Type: C1, IsImplicit) (Syntax: 'c1')

                    Jump if True (Regular) to Block[B25]
                        IIsNullOperation (OperationKind.IsNull, Type: System.Boolean, IsImplicit) (Syntax: 'c1')
                          Operand: 
                            IFlowCaptureReferenceOperation: 10 (OperationKind.FlowCaptureReference, Type: C1, IsImplicit) (Syntax: 'c1')
                        Leaving: {R10}

                    Next (Regular) Block[B24]
                Block[B24] - Block
                    Predecessors: [B23]
                    Statements (1)
                        IFlowCaptureOperation: 11 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'c1')
                          Value: 
                            IFlowCaptureReferenceOperation: 10 (OperationKind.FlowCaptureReference, Type: C1, IsImplicit) (Syntax: 'c1')

                    Next (Regular) Block[B26]
                        Leaving: {R10}
            }

            Block[B25] - Block
                Predecessors: [B23]
                Statements (1)
                    IFlowCaptureOperation: 11 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'c2')
                      Value: 
                        IParameterReferenceOperation: c2 (OperationKind.ParameterReference, Type: C1, IsImplicit) (Syntax: 'c2')

                Next (Regular) Block[B26]
            Block[B26] - Block
                Predecessors: [B24] [B25]
                Statements (1)
                    IFlowCaptureOperation: 12 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'If(c1, c2).i')
                      Value: 
                        IFieldReferenceOperation: C1.i As System.Nullable(Of C) (OperationKind.FieldReference, Type: System.Nullable(Of C), IsImplicit) (Syntax: 'If(c1, c2).i')
                          Instance Receiver: 
                            IFlowCaptureReferenceOperation: 11 (OperationKind.FlowCaptureReference, Type: C1, IsImplicit) (Syntax: 'If(c1, c2)')

                Next (Regular) Block[B27]
                    Leaving: {R9}
                    Entering: {R11}
        }
        .locals {R11}
        {
            CaptureIds: [13]
            Block[B27] - Block
                Predecessors: [B26]
                Statements (1)
                    IFlowCaptureOperation: 13 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'c1')
                      Value: 
                        IParameterReferenceOperation: c1 (OperationKind.ParameterReference, Type: C1, IsImplicit) (Syntax: 'c1')

                Jump if True (Regular) to Block[B29]
                    IIsNullOperation (OperationKind.IsNull, Type: System.Boolean, IsImplicit) (Syntax: 'c1')
                      Operand: 
                        IFlowCaptureReferenceOperation: 13 (OperationKind.FlowCaptureReference, Type: C1, IsImplicit) (Syntax: 'c1')
                    Leaving: {R11}

                Next (Regular) Block[B28]
            Block[B28] - Block
                Predecessors: [B27]
                Statements (1)
                    IFlowCaptureOperation: 14 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'c1')
                      Value: 
                        IFlowCaptureReferenceOperation: 13 (OperationKind.FlowCaptureReference, Type: C1, IsImplicit) (Syntax: 'c1')

                Next (Regular) Block[B30]
                    Leaving: {R11}
        }

        Block[B29] - Block
            Predecessors: [B27]
            Statements (1)
                IFlowCaptureOperation: 14 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'c2')
                  Value: 
                    IParameterReferenceOperation: c2 (OperationKind.ParameterReference, Type: C1, IsImplicit) (Syntax: 'c2')

            Next (Regular) Block[B30]
        Block[B30] - Block
            Predecessors: [B28] [B29]
            Statements (1)
                ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: null, IsImplicit) (Syntax: 'If(c1, c2).i')
                  Left: 
                    IFlowCaptureReferenceOperation: 12 (OperationKind.FlowCaptureReference, Type: System.Nullable(Of C), IsImplicit) (Syntax: 'If(c1, c2).i')
                  Right: 
                    IBinaryOperation (BinaryOperatorKind.Add, IsLifted, Checked) (OperatorMethod: Function C.op_Addition(x As C, y As C) As C) (OperationKind.Binary, Type: System.Nullable(Of C), IsImplicit) (Syntax: 'For If(c1,  ... ep1, step2)')
                      Left: 
                        IFieldReferenceOperation: C1.i As System.Nullable(Of C) (OperationKind.FieldReference, Type: System.Nullable(Of C), IsImplicit) (Syntax: 'If(c1, c2).i')
                          Instance Receiver: 
                            IFlowCaptureReferenceOperation: 14 (OperationKind.FlowCaptureReference, Type: C1, IsImplicit) (Syntax: 'If(c1, c2)')
                      Right: 
                        IFlowCaptureReferenceOperation: 5 (OperationKind.FlowCaptureReference, Type: System.Nullable(Of C), IsImplicit) (Syntax: 'If(bStep, step1, step2)')

            Next (Regular) Block[B15]
                Leaving: {R8}
                Entering: {R5} {R6} {R7}
    }
}

Block[B31] - Exit
    Predecessors: [B20] [B21]
    Statements (0)
]]>.Value

            VerifyFlowGraphAndDiagnosticsForTest(Of MethodBlockSyntax)(source, expectedFlowGraph, expectedDiagnostics)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation, CompilerFeature.Dataflow)>
        <Fact()>
        Public Sub ForToFlow_31()
            Dim source = <![CDATA[
Imports System
Public Class C
    Sub M(i As Object, init As Object, limit As Object, result As Object) 'BIND:"Sub M"
        For i = init To limit
            result = i
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
    Locals: [<anonymous local> As System.Object]
    Block[B1] - Block
        Predecessors: [B0]
        Statements (0)
        Jump if False (Regular) to Block[B3]
            IInvocationOperation (Function Microsoft.VisualBasic.CompilerServices.ObjectFlowControl.ForLoopControl.ForLoopInitObj(Counter As System.Object, Start As System.Object, Limit As System.Object, StepValue As System.Object, ByRef LoopForResult As System.Object, ByRef CounterResult As System.Object) As System.Boolean) (OperationKind.Invocation, Type: System.Boolean, IsImplicit) (Syntax: 'limit')
              Instance Receiver: 
                null
              Arguments(6):
                  IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: Counter) (OperationKind.Argument, Type: null, IsImplicit) (Syntax: 'i')
                    IParameterReferenceOperation: i (OperationKind.ParameterReference, Type: System.Object) (Syntax: 'i')
                    InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                    OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                  IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: Start) (OperationKind.Argument, Type: null, IsImplicit) (Syntax: 'init')
                    IParameterReferenceOperation: init (OperationKind.ParameterReference, Type: System.Object) (Syntax: 'init')
                    InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                    OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                  IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: Limit) (OperationKind.Argument, Type: null, IsImplicit) (Syntax: 'limit')
                    IParameterReferenceOperation: limit (OperationKind.ParameterReference, Type: System.Object) (Syntax: 'limit')
                    InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                    OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                  IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: StepValue) (OperationKind.Argument, Type: null, IsImplicit) (Syntax: 'For i = ini ... Next')
                    IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.Object, IsImplicit) (Syntax: 'For i = ini ... Next')
                      Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                        (WideningValue)
                      Operand: 
                        ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 1, IsImplicit) (Syntax: 'For i = ini ... Next')
                    InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                    OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                  IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: LoopForResult) (OperationKind.Argument, Type: null, IsImplicit) (Syntax: 'i')
                    ILocalReferenceOperation:  (IsDeclaration: True) (OperationKind.LocalReference, Type: System.Object, IsImplicit) (Syntax: 'i')
                    InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                    OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                  IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: CounterResult) (OperationKind.Argument, Type: null, IsImplicit) (Syntax: 'i')
                    IParameterReferenceOperation: i (OperationKind.ParameterReference, Type: System.Object, IsImplicit) (Syntax: 'i')
                    InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                    OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
            Leaving: {R1}

        Next (Regular) Block[B2]
    Block[B2] - Block
        Predecessors: [B1] [B2]
        Statements (1)
            IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'result = i')
              Expression: 
                ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Object, IsImplicit) (Syntax: 'result = i')
                  Left: 
                    IParameterReferenceOperation: result (OperationKind.ParameterReference, Type: System.Object) (Syntax: 'result')
                  Right: 
                    IParameterReferenceOperation: i (OperationKind.ParameterReference, Type: System.Object) (Syntax: 'i')

        Jump if False (Regular) to Block[B3]
            IInvocationOperation (Function Microsoft.VisualBasic.CompilerServices.ObjectFlowControl.ForLoopControl.ForNextCheckObj(Counter As System.Object, LoopObj As System.Object, ByRef CounterResult As System.Object) As System.Boolean) (OperationKind.Invocation, Type: System.Boolean, IsImplicit) (Syntax: 'limit')
              Instance Receiver: 
                null
              Arguments(3):
                  IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: Counter) (OperationKind.Argument, Type: null, IsImplicit) (Syntax: 'limit')
                    IParameterReferenceOperation: i (OperationKind.ParameterReference, Type: System.Object, IsImplicit) (Syntax: 'i')
                    InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                    OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                  IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: LoopObj) (OperationKind.Argument, Type: null, IsImplicit) (Syntax: 'limit')
                    ILocalReferenceOperation:  (OperationKind.LocalReference, Type: System.Object, IsImplicit) (Syntax: 'i')
                    InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                    OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                  IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: CounterResult) (OperationKind.Argument, Type: null, IsImplicit) (Syntax: 'limit')
                    IParameterReferenceOperation: i (OperationKind.ParameterReference, Type: System.Object, IsImplicit) (Syntax: 'i')
                    InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                    OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
            Leaving: {R1}

        Next (Regular) Block[B2]
}

Block[B3] - Exit
    Predecessors: [B1] [B2]
    Statements (0)
]]>.Value

            VerifyFlowGraphAndDiagnosticsForTest(Of MethodBlockSyntax)(source, expectedFlowGraph, expectedDiagnostics)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation, CompilerFeature.Dataflow)>
        <Fact()>
        Public Sub ForToFlow_32()
            Dim source = <![CDATA[
Imports System
Public Class C
    Sub M(init As Object, limit As Object, [step] as Object, result As Object) 'BIND:"Sub M"
        For i = init To limit Step [step]
            result = i
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
    Locals: [<anonymous local> As System.Object] [i As System.Object]
    Block[B1] - Block
        Predecessors: [B0]
        Statements (0)
        Jump if False (Regular) to Block[B3]
            IInvocationOperation (Function Microsoft.VisualBasic.CompilerServices.ObjectFlowControl.ForLoopControl.ForLoopInitObj(Counter As System.Object, Start As System.Object, Limit As System.Object, StepValue As System.Object, ByRef LoopForResult As System.Object, ByRef CounterResult As System.Object) As System.Boolean) (OperationKind.Invocation, Type: System.Boolean, IsImplicit) (Syntax: 'limit')
              Instance Receiver: 
                null
              Arguments(6):
                  IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: Counter) (OperationKind.Argument, Type: null, IsImplicit) (Syntax: 'i')
                    ILocalReferenceOperation: i (IsDeclaration: True) (OperationKind.LocalReference, Type: System.Object, IsImplicit) (Syntax: 'i')
                    InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                    OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                  IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: Start) (OperationKind.Argument, Type: null, IsImplicit) (Syntax: 'init')
                    IParameterReferenceOperation: init (OperationKind.ParameterReference, Type: System.Object) (Syntax: 'init')
                    InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                    OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                  IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: Limit) (OperationKind.Argument, Type: null, IsImplicit) (Syntax: 'limit')
                    IParameterReferenceOperation: limit (OperationKind.ParameterReference, Type: System.Object) (Syntax: 'limit')
                    InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                    OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                  IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: StepValue) (OperationKind.Argument, Type: null, IsImplicit) (Syntax: '[step]')
                    IParameterReferenceOperation: step (OperationKind.ParameterReference, Type: System.Object) (Syntax: '[step]')
                    InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                    OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                  IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: LoopForResult) (OperationKind.Argument, Type: null, IsImplicit) (Syntax: 'i')
                    ILocalReferenceOperation:  (IsDeclaration: True) (OperationKind.LocalReference, Type: System.Object, IsImplicit) (Syntax: 'i')
                    InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                    OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                  IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: CounterResult) (OperationKind.Argument, Type: null, IsImplicit) (Syntax: 'i')
                    ILocalReferenceOperation: i (IsDeclaration: True) (OperationKind.LocalReference, Type: System.Object, IsImplicit) (Syntax: 'i')
                    InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                    OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
            Leaving: {R1}

        Next (Regular) Block[B2]
    Block[B2] - Block
        Predecessors: [B1] [B2]
        Statements (1)
            IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'result = i')
              Expression: 
                ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Object, IsImplicit) (Syntax: 'result = i')
                  Left: 
                    IParameterReferenceOperation: result (OperationKind.ParameterReference, Type: System.Object) (Syntax: 'result')
                  Right: 
                    ILocalReferenceOperation: i (OperationKind.LocalReference, Type: System.Object) (Syntax: 'i')

        Jump if False (Regular) to Block[B3]
            IInvocationOperation (Function Microsoft.VisualBasic.CompilerServices.ObjectFlowControl.ForLoopControl.ForNextCheckObj(Counter As System.Object, LoopObj As System.Object, ByRef CounterResult As System.Object) As System.Boolean) (OperationKind.Invocation, Type: System.Boolean, IsImplicit) (Syntax: 'limit')
              Instance Receiver: 
                null
              Arguments(3):
                  IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: Counter) (OperationKind.Argument, Type: null, IsImplicit) (Syntax: 'limit')
                    ILocalReferenceOperation: i (IsDeclaration: True) (OperationKind.LocalReference, Type: System.Object, IsImplicit) (Syntax: 'i')
                    InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                    OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                  IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: LoopObj) (OperationKind.Argument, Type: null, IsImplicit) (Syntax: 'limit')
                    ILocalReferenceOperation:  (OperationKind.LocalReference, Type: System.Object, IsImplicit) (Syntax: 'i')
                    InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                    OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                  IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: CounterResult) (OperationKind.Argument, Type: null, IsImplicit) (Syntax: 'limit')
                    ILocalReferenceOperation: i (IsDeclaration: True) (OperationKind.LocalReference, Type: System.Object, IsImplicit) (Syntax: 'i')
                    InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                    OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
            Leaving: {R1}

        Next (Regular) Block[B2]
}

Block[B3] - Exit
    Predecessors: [B1] [B2]
    Statements (0)
]]>.Value

            VerifyFlowGraphAndDiagnosticsForTest(Of MethodBlockSyntax)(source, expectedFlowGraph, expectedDiagnostics)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation, CompilerFeature.Dataflow)>
        <Fact()>
        Public Sub ForToFlow_33()
            Dim source = <![CDATA[
Imports System
Public Class C
    Sub M(c1 As C, c2 As C, bInit As Boolean, init1 As Object, init2 As Object, bLimit As Boolean, limit1 As Object, limit2 As Object, bStep As Boolean, step1 As Object, step2 As Object, result As Boolean) 'BIND:"Sub M"
        For If(c1, c2).i = If(bInit, init1, init2) To If(bLimit, limit1, limit2) Step If(bStep, step1, step2)
            result = true
        Next
    End Sub

    Public i As Object
End Class
]]>.Value

            Dim expectedDiagnostics = String.Empty

            Dim expectedFlowGraph = <![CDATA[
Block[B0] - Entry
    Statements (0)
    Next (Regular) Block[B1]
        Entering: {R1} {R2} {R3} {R4}

.locals {R1}
{
    Locals: [<anonymous local> As System.Object]
    .locals {R2}
    {
        CaptureIds: [2] [3] [4] [5] [7]
        .locals {R3}
        {
            CaptureIds: [1]
            .locals {R4}
            {
                CaptureIds: [0]
                Block[B1] - Block
                    Predecessors: [B0]
                    Statements (1)
                        IFlowCaptureOperation: 0 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'c1')
                          Value: 
                            IParameterReferenceOperation: c1 (OperationKind.ParameterReference, Type: C) (Syntax: 'c1')

                    Jump if True (Regular) to Block[B3]
                        IIsNullOperation (OperationKind.IsNull, Type: System.Boolean, IsImplicit) (Syntax: 'c1')
                          Operand: 
                            IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: C, IsImplicit) (Syntax: 'c1')
                        Leaving: {R4}

                    Next (Regular) Block[B2]
                Block[B2] - Block
                    Predecessors: [B1]
                    Statements (1)
                        IFlowCaptureOperation: 1 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'c1')
                          Value: 
                            IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: C, IsImplicit) (Syntax: 'c1')

                    Next (Regular) Block[B4]
                        Leaving: {R4}
            }

            Block[B3] - Block
                Predecessors: [B1]
                Statements (1)
                    IFlowCaptureOperation: 1 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'c2')
                      Value: 
                        IParameterReferenceOperation: c2 (OperationKind.ParameterReference, Type: C) (Syntax: 'c2')

                Next (Regular) Block[B4]
            Block[B4] - Block
                Predecessors: [B2] [B3]
                Statements (1)
                    IFlowCaptureOperation: 2 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'If(c1, c2).i')
                      Value: 
                        IFieldReferenceOperation: C.i As System.Object (OperationKind.FieldReference, Type: System.Object) (Syntax: 'If(c1, c2).i')
                          Instance Receiver: 
                            IFlowCaptureReferenceOperation: 1 (OperationKind.FlowCaptureReference, Type: C, IsImplicit) (Syntax: 'If(c1, c2)')

                Next (Regular) Block[B5]
                    Leaving: {R3}
        }

        Block[B5] - Block
            Predecessors: [B4]
            Statements (0)
            Jump if False (Regular) to Block[B7]
                IParameterReferenceOperation: bInit (OperationKind.ParameterReference, Type: System.Boolean) (Syntax: 'bInit')

            Next (Regular) Block[B6]
        Block[B6] - Block
            Predecessors: [B5]
            Statements (1)
                IFlowCaptureOperation: 3 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'init1')
                  Value: 
                    IParameterReferenceOperation: init1 (OperationKind.ParameterReference, Type: System.Object) (Syntax: 'init1')

            Next (Regular) Block[B8]
        Block[B7] - Block
            Predecessors: [B5]
            Statements (1)
                IFlowCaptureOperation: 3 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'init2')
                  Value: 
                    IParameterReferenceOperation: init2 (OperationKind.ParameterReference, Type: System.Object) (Syntax: 'init2')

            Next (Regular) Block[B8]
        Block[B8] - Block
            Predecessors: [B6] [B7]
            Statements (0)
            Jump if False (Regular) to Block[B10]
                IParameterReferenceOperation: bLimit (OperationKind.ParameterReference, Type: System.Boolean) (Syntax: 'bLimit')

            Next (Regular) Block[B9]
        Block[B9] - Block
            Predecessors: [B8]
            Statements (1)
                IFlowCaptureOperation: 4 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'limit1')
                  Value: 
                    IParameterReferenceOperation: limit1 (OperationKind.ParameterReference, Type: System.Object) (Syntax: 'limit1')

            Next (Regular) Block[B11]
        Block[B10] - Block
            Predecessors: [B8]
            Statements (1)
                IFlowCaptureOperation: 4 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'limit2')
                  Value: 
                    IParameterReferenceOperation: limit2 (OperationKind.ParameterReference, Type: System.Object) (Syntax: 'limit2')

            Next (Regular) Block[B11]
        Block[B11] - Block
            Predecessors: [B9] [B10]
            Statements (0)
            Jump if False (Regular) to Block[B13]
                IParameterReferenceOperation: bStep (OperationKind.ParameterReference, Type: System.Boolean) (Syntax: 'bStep')

            Next (Regular) Block[B12]
        Block[B12] - Block
            Predecessors: [B11]
            Statements (1)
                IFlowCaptureOperation: 5 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'step1')
                  Value: 
                    IParameterReferenceOperation: step1 (OperationKind.ParameterReference, Type: System.Object) (Syntax: 'step1')

            Next (Regular) Block[B14]
                Entering: {R5}
        Block[B13] - Block
            Predecessors: [B11]
            Statements (1)
                IFlowCaptureOperation: 5 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'step2')
                  Value: 
                    IParameterReferenceOperation: step2 (OperationKind.ParameterReference, Type: System.Object) (Syntax: 'step2')

            Next (Regular) Block[B14]
                Entering: {R5}

        .locals {R5}
        {
            CaptureIds: [6]
            Block[B14] - Block
                Predecessors: [B12] [B13]
                Statements (1)
                    IFlowCaptureOperation: 6 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'c1')
                      Value: 
                        IParameterReferenceOperation: c1 (OperationKind.ParameterReference, Type: C, IsImplicit) (Syntax: 'c1')

                Jump if True (Regular) to Block[B16]
                    IIsNullOperation (OperationKind.IsNull, Type: System.Boolean, IsImplicit) (Syntax: 'c1')
                      Operand: 
                        IFlowCaptureReferenceOperation: 6 (OperationKind.FlowCaptureReference, Type: C, IsImplicit) (Syntax: 'c1')
                    Leaving: {R5}

                Next (Regular) Block[B15]
            Block[B15] - Block
                Predecessors: [B14]
                Statements (1)
                    IFlowCaptureOperation: 7 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'c1')
                      Value: 
                        IFlowCaptureReferenceOperation: 6 (OperationKind.FlowCaptureReference, Type: C, IsImplicit) (Syntax: 'c1')

                Next (Regular) Block[B17]
                    Leaving: {R5}
        }

        Block[B16] - Block
            Predecessors: [B14]
            Statements (1)
                IFlowCaptureOperation: 7 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'c2')
                  Value: 
                    IParameterReferenceOperation: c2 (OperationKind.ParameterReference, Type: C, IsImplicit) (Syntax: 'c2')

            Next (Regular) Block[B17]
        Block[B17] - Block
            Predecessors: [B15] [B16]
            Statements (0)
            Jump if False (Regular) to Block[B27]
                IInvocationOperation (Function Microsoft.VisualBasic.CompilerServices.ObjectFlowControl.ForLoopControl.ForLoopInitObj(Counter As System.Object, Start As System.Object, Limit As System.Object, StepValue As System.Object, ByRef LoopForResult As System.Object, ByRef CounterResult As System.Object) As System.Boolean) (OperationKind.Invocation, Type: System.Boolean, IsImplicit) (Syntax: 'If(bLimit,  ... t1, limit2)')
                  Instance Receiver: 
                    null
                  Arguments(6):
                      IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: Counter) (OperationKind.Argument, Type: null, IsImplicit) (Syntax: 'If(c1, c2).i')
                        IFlowCaptureReferenceOperation: 2 (OperationKind.FlowCaptureReference, Type: System.Object, IsImplicit) (Syntax: 'If(c1, c2).i')
                        InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                        OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                      IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: Start) (OperationKind.Argument, Type: null, IsImplicit) (Syntax: 'If(bInit, init1, init2)')
                        IFlowCaptureReferenceOperation: 3 (OperationKind.FlowCaptureReference, Type: System.Object, IsImplicit) (Syntax: 'If(bInit, init1, init2)')
                        InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                        OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                      IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: Limit) (OperationKind.Argument, Type: null, IsImplicit) (Syntax: 'If(bLimit,  ... t1, limit2)')
                        IFlowCaptureReferenceOperation: 4 (OperationKind.FlowCaptureReference, Type: System.Object, IsImplicit) (Syntax: 'If(bLimit,  ... t1, limit2)')
                        InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                        OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                      IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: StepValue) (OperationKind.Argument, Type: null, IsImplicit) (Syntax: 'If(bStep, step1, step2)')
                        IFlowCaptureReferenceOperation: 5 (OperationKind.FlowCaptureReference, Type: System.Object, IsImplicit) (Syntax: 'If(bStep, step1, step2)')
                        InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                        OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                      IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: LoopForResult) (OperationKind.Argument, Type: null, IsImplicit) (Syntax: 'If(c1, c2).i')
                        ILocalReferenceOperation:  (IsDeclaration: True) (OperationKind.LocalReference, Type: System.Object, IsImplicit) (Syntax: 'If(c1, c2).i')
                        InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                        OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                      IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: CounterResult) (OperationKind.Argument, Type: null, IsImplicit) (Syntax: 'If(c1, c2).i')
                        IFieldReferenceOperation: C.i As System.Object (OperationKind.FieldReference, Type: System.Object, IsImplicit) (Syntax: 'If(c1, c2).i')
                          Instance Receiver: 
                            IFlowCaptureReferenceOperation: 7 (OperationKind.FlowCaptureReference, Type: C, IsImplicit) (Syntax: 'If(c1, c2)')
                        InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                        OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                Leaving: {R2} {R1}

            Next (Regular) Block[B26]
                Leaving: {R2}
    }
    .locals {R6}
    {
        CaptureIds: [10] [12]
        .locals {R7}
        {
            CaptureIds: [9]
            .locals {R8}
            {
                CaptureIds: [8]
                Block[B18] - Block
                    Predecessors: [B26]
                    Statements (1)
                        IFlowCaptureOperation: 8 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'c1')
                          Value: 
                            IParameterReferenceOperation: c1 (OperationKind.ParameterReference, Type: C, IsImplicit) (Syntax: 'c1')

                    Jump if True (Regular) to Block[B20]
                        IIsNullOperation (OperationKind.IsNull, Type: System.Boolean, IsImplicit) (Syntax: 'c1')
                          Operand: 
                            IFlowCaptureReferenceOperation: 8 (OperationKind.FlowCaptureReference, Type: C, IsImplicit) (Syntax: 'c1')
                        Leaving: {R8}

                    Next (Regular) Block[B19]
                Block[B19] - Block
                    Predecessors: [B18]
                    Statements (1)
                        IFlowCaptureOperation: 9 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'c1')
                          Value: 
                            IFlowCaptureReferenceOperation: 8 (OperationKind.FlowCaptureReference, Type: C, IsImplicit) (Syntax: 'c1')

                    Next (Regular) Block[B21]
                        Leaving: {R8}
            }

            Block[B20] - Block
                Predecessors: [B18]
                Statements (1)
                    IFlowCaptureOperation: 9 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'c2')
                      Value: 
                        IParameterReferenceOperation: c2 (OperationKind.ParameterReference, Type: C, IsImplicit) (Syntax: 'c2')

                Next (Regular) Block[B21]
            Block[B21] - Block
                Predecessors: [B19] [B20]
                Statements (1)
                    IFlowCaptureOperation: 10 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'If(c1, c2).i')
                      Value: 
                        IFieldReferenceOperation: C.i As System.Object (OperationKind.FieldReference, Type: System.Object, IsImplicit) (Syntax: 'If(c1, c2).i')
                          Instance Receiver: 
                            IFlowCaptureReferenceOperation: 9 (OperationKind.FlowCaptureReference, Type: C, IsImplicit) (Syntax: 'If(c1, c2)')

                Next (Regular) Block[B22]
                    Leaving: {R7}
                    Entering: {R9}
        }
        .locals {R9}
        {
            CaptureIds: [11]
            Block[B22] - Block
                Predecessors: [B21]
                Statements (1)
                    IFlowCaptureOperation: 11 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'c1')
                      Value: 
                        IParameterReferenceOperation: c1 (OperationKind.ParameterReference, Type: C, IsImplicit) (Syntax: 'c1')

                Jump if True (Regular) to Block[B24]
                    IIsNullOperation (OperationKind.IsNull, Type: System.Boolean, IsImplicit) (Syntax: 'c1')
                      Operand: 
                        IFlowCaptureReferenceOperation: 11 (OperationKind.FlowCaptureReference, Type: C, IsImplicit) (Syntax: 'c1')
                    Leaving: {R9}

                Next (Regular) Block[B23]
            Block[B23] - Block
                Predecessors: [B22]
                Statements (1)
                    IFlowCaptureOperation: 12 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'c1')
                      Value: 
                        IFlowCaptureReferenceOperation: 11 (OperationKind.FlowCaptureReference, Type: C, IsImplicit) (Syntax: 'c1')

                Next (Regular) Block[B25]
                    Leaving: {R9}
        }

        Block[B24] - Block
            Predecessors: [B22]
            Statements (1)
                IFlowCaptureOperation: 12 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'c2')
                  Value: 
                    IParameterReferenceOperation: c2 (OperationKind.ParameterReference, Type: C, IsImplicit) (Syntax: 'c2')

            Next (Regular) Block[B25]
        Block[B25] - Block
            Predecessors: [B23] [B24]
            Statements (0)
            Jump if False (Regular) to Block[B27]
                IInvocationOperation (Function Microsoft.VisualBasic.CompilerServices.ObjectFlowControl.ForLoopControl.ForNextCheckObj(Counter As System.Object, LoopObj As System.Object, ByRef CounterResult As System.Object) As System.Boolean) (OperationKind.Invocation, Type: System.Boolean, IsImplicit) (Syntax: 'If(bLimit,  ... t1, limit2)')
                  Instance Receiver: 
                    null
                  Arguments(3):
                      IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: Counter) (OperationKind.Argument, Type: null, IsImplicit) (Syntax: 'If(bLimit,  ... t1, limit2)')
                        IFlowCaptureReferenceOperation: 10 (OperationKind.FlowCaptureReference, Type: System.Object, IsImplicit) (Syntax: 'If(c1, c2).i')
                        InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                        OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                      IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: LoopObj) (OperationKind.Argument, Type: null, IsImplicit) (Syntax: 'If(bLimit,  ... t1, limit2)')
                        ILocalReferenceOperation:  (OperationKind.LocalReference, Type: System.Object, IsImplicit) (Syntax: 'If(c1, c2).i')
                        InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                        OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                      IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: CounterResult) (OperationKind.Argument, Type: null, IsImplicit) (Syntax: 'If(bLimit,  ... t1, limit2)')
                        IFieldReferenceOperation: C.i As System.Object (OperationKind.FieldReference, Type: System.Object, IsImplicit) (Syntax: 'If(c1, c2).i')
                          Instance Receiver: 
                            IFlowCaptureReferenceOperation: 12 (OperationKind.FlowCaptureReference, Type: C, IsImplicit) (Syntax: 'If(c1, c2)')
                        InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                        OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                Leaving: {R6} {R1}

            Next (Regular) Block[B26]
                Leaving: {R6}
    }

    Block[B26] - Block
        Predecessors: [B17] [B25]
        Statements (1)
            IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'result = true')
              Expression: 
                ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Boolean, IsImplicit) (Syntax: 'result = true')
                  Left: 
                    IParameterReferenceOperation: result (OperationKind.ParameterReference, Type: System.Boolean) (Syntax: 'result')
                  Right: 
                    ILiteralOperation (OperationKind.Literal, Type: System.Boolean, Constant: True) (Syntax: 'true')

        Next (Regular) Block[B18]
            Entering: {R6} {R7} {R8}
}

Block[B27] - Exit
    Predecessors: [B17] [B25]
    Statements (0)
]]>.Value

            VerifyFlowGraphAndDiagnosticsForTest(Of MethodBlockSyntax)(source, expectedFlowGraph, expectedDiagnostics)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation, CompilerFeature.Dataflow)>
        <Fact()>
        Public Sub ForToFlow_34()
            Dim source = <![CDATA[
Imports System
Public Class C
    Sub M(c1 As C, c2 As C, init1 As Object, limit1 As Object, step1 As Object, result As Boolean) 'BIND:"Sub M"
        For If(c1, c2).i = init1 To limit1 Step step1
            result = true
        Next
    End Sub

    Public i As Object
End Class
]]>.Value

            Dim expectedDiagnostics = String.Empty

            Dim expectedFlowGraph = <![CDATA[
Block[B0] - Entry
    Statements (0)
    Next (Regular) Block[B1]
        Entering: {R1} {R2} {R3} {R4}

.locals {R1}
{
    Locals: [<anonymous local> As System.Object]
    .locals {R2}
    {
        CaptureIds: [2] [3] [4] [5] [7]
        .locals {R3}
        {
            CaptureIds: [1]
            .locals {R4}
            {
                CaptureIds: [0]
                Block[B1] - Block
                    Predecessors: [B0]
                    Statements (1)
                        IFlowCaptureOperation: 0 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'c1')
                          Value: 
                            IParameterReferenceOperation: c1 (OperationKind.ParameterReference, Type: C) (Syntax: 'c1')

                    Jump if True (Regular) to Block[B3]
                        IIsNullOperation (OperationKind.IsNull, Type: System.Boolean, IsImplicit) (Syntax: 'c1')
                          Operand: 
                            IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: C, IsImplicit) (Syntax: 'c1')
                        Leaving: {R4}

                    Next (Regular) Block[B2]
                Block[B2] - Block
                    Predecessors: [B1]
                    Statements (1)
                        IFlowCaptureOperation: 1 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'c1')
                          Value: 
                            IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: C, IsImplicit) (Syntax: 'c1')

                    Next (Regular) Block[B4]
                        Leaving: {R4}
            }

            Block[B3] - Block
                Predecessors: [B1]
                Statements (1)
                    IFlowCaptureOperation: 1 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'c2')
                      Value: 
                        IParameterReferenceOperation: c2 (OperationKind.ParameterReference, Type: C) (Syntax: 'c2')

                Next (Regular) Block[B4]
            Block[B4] - Block
                Predecessors: [B2] [B3]
                Statements (1)
                    IFlowCaptureOperation: 2 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'If(c1, c2).i')
                      Value: 
                        IFieldReferenceOperation: C.i As System.Object (OperationKind.FieldReference, Type: System.Object) (Syntax: 'If(c1, c2).i')
                          Instance Receiver: 
                            IFlowCaptureReferenceOperation: 1 (OperationKind.FlowCaptureReference, Type: C, IsImplicit) (Syntax: 'If(c1, c2)')

                Next (Regular) Block[B5]
                    Leaving: {R3}
        }

        Block[B5] - Block
            Predecessors: [B4]
            Statements (3)
                IFlowCaptureOperation: 3 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'init1')
                  Value: 
                    IParameterReferenceOperation: init1 (OperationKind.ParameterReference, Type: System.Object) (Syntax: 'init1')

                IFlowCaptureOperation: 4 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'limit1')
                  Value: 
                    IParameterReferenceOperation: limit1 (OperationKind.ParameterReference, Type: System.Object) (Syntax: 'limit1')

                IFlowCaptureOperation: 5 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'step1')
                  Value: 
                    IParameterReferenceOperation: step1 (OperationKind.ParameterReference, Type: System.Object) (Syntax: 'step1')

            Next (Regular) Block[B6]
                Entering: {R5}

        .locals {R5}
        {
            CaptureIds: [6]
            Block[B6] - Block
                Predecessors: [B5]
                Statements (1)
                    IFlowCaptureOperation: 6 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'c1')
                      Value: 
                        IParameterReferenceOperation: c1 (OperationKind.ParameterReference, Type: C, IsImplicit) (Syntax: 'c1')

                Jump if True (Regular) to Block[B8]
                    IIsNullOperation (OperationKind.IsNull, Type: System.Boolean, IsImplicit) (Syntax: 'c1')
                      Operand: 
                        IFlowCaptureReferenceOperation: 6 (OperationKind.FlowCaptureReference, Type: C, IsImplicit) (Syntax: 'c1')
                    Leaving: {R5}

                Next (Regular) Block[B7]
            Block[B7] - Block
                Predecessors: [B6]
                Statements (1)
                    IFlowCaptureOperation: 7 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'c1')
                      Value: 
                        IFlowCaptureReferenceOperation: 6 (OperationKind.FlowCaptureReference, Type: C, IsImplicit) (Syntax: 'c1')

                Next (Regular) Block[B9]
                    Leaving: {R5}
        }

        Block[B8] - Block
            Predecessors: [B6]
            Statements (1)
                IFlowCaptureOperation: 7 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'c2')
                  Value: 
                    IParameterReferenceOperation: c2 (OperationKind.ParameterReference, Type: C, IsImplicit) (Syntax: 'c2')

            Next (Regular) Block[B9]
        Block[B9] - Block
            Predecessors: [B7] [B8]
            Statements (0)
            Jump if False (Regular) to Block[B19]
                IInvocationOperation (Function Microsoft.VisualBasic.CompilerServices.ObjectFlowControl.ForLoopControl.ForLoopInitObj(Counter As System.Object, Start As System.Object, Limit As System.Object, StepValue As System.Object, ByRef LoopForResult As System.Object, ByRef CounterResult As System.Object) As System.Boolean) (OperationKind.Invocation, Type: System.Boolean, IsImplicit) (Syntax: 'limit1')
                  Instance Receiver: 
                    null
                  Arguments(6):
                      IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: Counter) (OperationKind.Argument, Type: null, IsImplicit) (Syntax: 'If(c1, c2).i')
                        IFlowCaptureReferenceOperation: 2 (OperationKind.FlowCaptureReference, Type: System.Object, IsImplicit) (Syntax: 'If(c1, c2).i')
                        InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                        OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                      IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: Start) (OperationKind.Argument, Type: null, IsImplicit) (Syntax: 'init1')
                        IFlowCaptureReferenceOperation: 3 (OperationKind.FlowCaptureReference, Type: System.Object, IsImplicit) (Syntax: 'init1')
                        InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                        OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                      IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: Limit) (OperationKind.Argument, Type: null, IsImplicit) (Syntax: 'limit1')
                        IFlowCaptureReferenceOperation: 4 (OperationKind.FlowCaptureReference, Type: System.Object, IsImplicit) (Syntax: 'limit1')
                        InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                        OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                      IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: StepValue) (OperationKind.Argument, Type: null, IsImplicit) (Syntax: 'step1')
                        IFlowCaptureReferenceOperation: 5 (OperationKind.FlowCaptureReference, Type: System.Object, IsImplicit) (Syntax: 'step1')
                        InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                        OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                      IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: LoopForResult) (OperationKind.Argument, Type: null, IsImplicit) (Syntax: 'If(c1, c2).i')
                        ILocalReferenceOperation:  (IsDeclaration: True) (OperationKind.LocalReference, Type: System.Object, IsImplicit) (Syntax: 'If(c1, c2).i')
                        InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                        OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                      IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: CounterResult) (OperationKind.Argument, Type: null, IsImplicit) (Syntax: 'If(c1, c2).i')
                        IFieldReferenceOperation: C.i As System.Object (OperationKind.FieldReference, Type: System.Object, IsImplicit) (Syntax: 'If(c1, c2).i')
                          Instance Receiver: 
                            IFlowCaptureReferenceOperation: 7 (OperationKind.FlowCaptureReference, Type: C, IsImplicit) (Syntax: 'If(c1, c2)')
                        InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                        OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                Leaving: {R2} {R1}

            Next (Regular) Block[B18]
                Leaving: {R2}
    }
    .locals {R6}
    {
        CaptureIds: [10] [12]
        .locals {R7}
        {
            CaptureIds: [9]
            .locals {R8}
            {
                CaptureIds: [8]
                Block[B10] - Block
                    Predecessors: [B18]
                    Statements (1)
                        IFlowCaptureOperation: 8 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'c1')
                          Value: 
                            IParameterReferenceOperation: c1 (OperationKind.ParameterReference, Type: C, IsImplicit) (Syntax: 'c1')

                    Jump if True (Regular) to Block[B12]
                        IIsNullOperation (OperationKind.IsNull, Type: System.Boolean, IsImplicit) (Syntax: 'c1')
                          Operand: 
                            IFlowCaptureReferenceOperation: 8 (OperationKind.FlowCaptureReference, Type: C, IsImplicit) (Syntax: 'c1')
                        Leaving: {R8}

                    Next (Regular) Block[B11]
                Block[B11] - Block
                    Predecessors: [B10]
                    Statements (1)
                        IFlowCaptureOperation: 9 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'c1')
                          Value: 
                            IFlowCaptureReferenceOperation: 8 (OperationKind.FlowCaptureReference, Type: C, IsImplicit) (Syntax: 'c1')

                    Next (Regular) Block[B13]
                        Leaving: {R8}
            }

            Block[B12] - Block
                Predecessors: [B10]
                Statements (1)
                    IFlowCaptureOperation: 9 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'c2')
                      Value: 
                        IParameterReferenceOperation: c2 (OperationKind.ParameterReference, Type: C, IsImplicit) (Syntax: 'c2')

                Next (Regular) Block[B13]
            Block[B13] - Block
                Predecessors: [B11] [B12]
                Statements (1)
                    IFlowCaptureOperation: 10 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'If(c1, c2).i')
                      Value: 
                        IFieldReferenceOperation: C.i As System.Object (OperationKind.FieldReference, Type: System.Object, IsImplicit) (Syntax: 'If(c1, c2).i')
                          Instance Receiver: 
                            IFlowCaptureReferenceOperation: 9 (OperationKind.FlowCaptureReference, Type: C, IsImplicit) (Syntax: 'If(c1, c2)')

                Next (Regular) Block[B14]
                    Leaving: {R7}
                    Entering: {R9}
        }
        .locals {R9}
        {
            CaptureIds: [11]
            Block[B14] - Block
                Predecessors: [B13]
                Statements (1)
                    IFlowCaptureOperation: 11 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'c1')
                      Value: 
                        IParameterReferenceOperation: c1 (OperationKind.ParameterReference, Type: C, IsImplicit) (Syntax: 'c1')

                Jump if True (Regular) to Block[B16]
                    IIsNullOperation (OperationKind.IsNull, Type: System.Boolean, IsImplicit) (Syntax: 'c1')
                      Operand: 
                        IFlowCaptureReferenceOperation: 11 (OperationKind.FlowCaptureReference, Type: C, IsImplicit) (Syntax: 'c1')
                    Leaving: {R9}

                Next (Regular) Block[B15]
            Block[B15] - Block
                Predecessors: [B14]
                Statements (1)
                    IFlowCaptureOperation: 12 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'c1')
                      Value: 
                        IFlowCaptureReferenceOperation: 11 (OperationKind.FlowCaptureReference, Type: C, IsImplicit) (Syntax: 'c1')

                Next (Regular) Block[B17]
                    Leaving: {R9}
        }

        Block[B16] - Block
            Predecessors: [B14]
            Statements (1)
                IFlowCaptureOperation: 12 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'c2')
                  Value: 
                    IParameterReferenceOperation: c2 (OperationKind.ParameterReference, Type: C, IsImplicit) (Syntax: 'c2')

            Next (Regular) Block[B17]
        Block[B17] - Block
            Predecessors: [B15] [B16]
            Statements (0)
            Jump if False (Regular) to Block[B19]
                IInvocationOperation (Function Microsoft.VisualBasic.CompilerServices.ObjectFlowControl.ForLoopControl.ForNextCheckObj(Counter As System.Object, LoopObj As System.Object, ByRef CounterResult As System.Object) As System.Boolean) (OperationKind.Invocation, Type: System.Boolean, IsImplicit) (Syntax: 'limit1')
                  Instance Receiver: 
                    null
                  Arguments(3):
                      IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: Counter) (OperationKind.Argument, Type: null, IsImplicit) (Syntax: 'limit1')
                        IFlowCaptureReferenceOperation: 10 (OperationKind.FlowCaptureReference, Type: System.Object, IsImplicit) (Syntax: 'If(c1, c2).i')
                        InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                        OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                      IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: LoopObj) (OperationKind.Argument, Type: null, IsImplicit) (Syntax: 'limit1')
                        ILocalReferenceOperation:  (OperationKind.LocalReference, Type: System.Object, IsImplicit) (Syntax: 'If(c1, c2).i')
                        InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                        OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                      IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: CounterResult) (OperationKind.Argument, Type: null, IsImplicit) (Syntax: 'limit1')
                        IFieldReferenceOperation: C.i As System.Object (OperationKind.FieldReference, Type: System.Object, IsImplicit) (Syntax: 'If(c1, c2).i')
                          Instance Receiver: 
                            IFlowCaptureReferenceOperation: 12 (OperationKind.FlowCaptureReference, Type: C, IsImplicit) (Syntax: 'If(c1, c2)')
                        InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                        OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                Leaving: {R6} {R1}

            Next (Regular) Block[B18]
                Leaving: {R6}
    }

    Block[B18] - Block
        Predecessors: [B9] [B17]
        Statements (1)
            IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'result = true')
              Expression: 
                ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Boolean, IsImplicit) (Syntax: 'result = true')
                  Left: 
                    IParameterReferenceOperation: result (OperationKind.ParameterReference, Type: System.Boolean) (Syntax: 'result')
                  Right: 
                    ILiteralOperation (OperationKind.Literal, Type: System.Boolean, Constant: True) (Syntax: 'true')

        Next (Regular) Block[B10]
            Entering: {R6} {R7} {R8}
}

Block[B19] - Exit
    Predecessors: [B9] [B17]
    Statements (0)
]]>.Value

            VerifyFlowGraphAndDiagnosticsForTest(Of MethodBlockSyntax)(source, expectedFlowGraph, expectedDiagnostics)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation, CompilerFeature.Dataflow)>
        <Fact()>
        Public Sub ForToFlow_35()
            Dim source = <![CDATA[
Imports System
Public Class C
    Sub M(i As Object, bInit As Boolean, init1 As Object, init2 As Object, limit1 As Object, step1 As Object, result As Boolean) 'BIND:"Sub M"
        For i = If(bInit, init1, init2) To limit1 Step step1
            result = true
        Next
    End Sub
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
    Locals: [<anonymous local> As System.Object]
    .locals {R2}
    {
        CaptureIds: [0] [1]
        Block[B1] - Block
            Predecessors: [B0]
            Statements (1)
                IFlowCaptureOperation: 0 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'i')
                  Value: 
                    IParameterReferenceOperation: i (OperationKind.ParameterReference, Type: System.Object) (Syntax: 'i')

            Jump if False (Regular) to Block[B3]
                IParameterReferenceOperation: bInit (OperationKind.ParameterReference, Type: System.Boolean) (Syntax: 'bInit')

            Next (Regular) Block[B2]
        Block[B2] - Block
            Predecessors: [B1]
            Statements (1)
                IFlowCaptureOperation: 1 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'init1')
                  Value: 
                    IParameterReferenceOperation: init1 (OperationKind.ParameterReference, Type: System.Object) (Syntax: 'init1')

            Next (Regular) Block[B4]
        Block[B3] - Block
            Predecessors: [B1]
            Statements (1)
                IFlowCaptureOperation: 1 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'init2')
                  Value: 
                    IParameterReferenceOperation: init2 (OperationKind.ParameterReference, Type: System.Object) (Syntax: 'init2')

            Next (Regular) Block[B4]
        Block[B4] - Block
            Predecessors: [B2] [B3]
            Statements (0)
            Jump if False (Regular) to Block[B6]
                IInvocationOperation (Function Microsoft.VisualBasic.CompilerServices.ObjectFlowControl.ForLoopControl.ForLoopInitObj(Counter As System.Object, Start As System.Object, Limit As System.Object, StepValue As System.Object, ByRef LoopForResult As System.Object, ByRef CounterResult As System.Object) As System.Boolean) (OperationKind.Invocation, Type: System.Boolean, IsImplicit) (Syntax: 'limit1')
                  Instance Receiver: 
                    null
                  Arguments(6):
                      IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: Counter) (OperationKind.Argument, Type: null, IsImplicit) (Syntax: 'i')
                        IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: System.Object, IsImplicit) (Syntax: 'i')
                        InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                        OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                      IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: Start) (OperationKind.Argument, Type: null, IsImplicit) (Syntax: 'If(bInit, init1, init2)')
                        IFlowCaptureReferenceOperation: 1 (OperationKind.FlowCaptureReference, Type: System.Object, IsImplicit) (Syntax: 'If(bInit, init1, init2)')
                        InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                        OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                      IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: Limit) (OperationKind.Argument, Type: null, IsImplicit) (Syntax: 'limit1')
                        IParameterReferenceOperation: limit1 (OperationKind.ParameterReference, Type: System.Object) (Syntax: 'limit1')
                        InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                        OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                      IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: StepValue) (OperationKind.Argument, Type: null, IsImplicit) (Syntax: 'step1')
                        IParameterReferenceOperation: step1 (OperationKind.ParameterReference, Type: System.Object) (Syntax: 'step1')
                        InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                        OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                      IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: LoopForResult) (OperationKind.Argument, Type: null, IsImplicit) (Syntax: 'i')
                        ILocalReferenceOperation:  (IsDeclaration: True) (OperationKind.LocalReference, Type: System.Object, IsImplicit) (Syntax: 'i')
                        InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                        OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                      IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: CounterResult) (OperationKind.Argument, Type: null, IsImplicit) (Syntax: 'i')
                        IParameterReferenceOperation: i (OperationKind.ParameterReference, Type: System.Object, IsImplicit) (Syntax: 'i')
                        InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                        OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                Leaving: {R2} {R1}

            Next (Regular) Block[B5]
                Leaving: {R2}
    }

    Block[B5] - Block
        Predecessors: [B4] [B5]
        Statements (1)
            IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'result = true')
              Expression: 
                ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Boolean, IsImplicit) (Syntax: 'result = true')
                  Left: 
                    IParameterReferenceOperation: result (OperationKind.ParameterReference, Type: System.Boolean) (Syntax: 'result')
                  Right: 
                    ILiteralOperation (OperationKind.Literal, Type: System.Boolean, Constant: True) (Syntax: 'true')

        Jump if False (Regular) to Block[B6]
            IInvocationOperation (Function Microsoft.VisualBasic.CompilerServices.ObjectFlowControl.ForLoopControl.ForNextCheckObj(Counter As System.Object, LoopObj As System.Object, ByRef CounterResult As System.Object) As System.Boolean) (OperationKind.Invocation, Type: System.Boolean, IsImplicit) (Syntax: 'limit1')
              Instance Receiver: 
                null
              Arguments(3):
                  IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: Counter) (OperationKind.Argument, Type: null, IsImplicit) (Syntax: 'limit1')
                    IParameterReferenceOperation: i (OperationKind.ParameterReference, Type: System.Object, IsImplicit) (Syntax: 'i')
                    InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                    OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                  IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: LoopObj) (OperationKind.Argument, Type: null, IsImplicit) (Syntax: 'limit1')
                    ILocalReferenceOperation:  (OperationKind.LocalReference, Type: System.Object, IsImplicit) (Syntax: 'i')
                    InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                    OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                  IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: CounterResult) (OperationKind.Argument, Type: null, IsImplicit) (Syntax: 'limit1')
                    IParameterReferenceOperation: i (OperationKind.ParameterReference, Type: System.Object, IsImplicit) (Syntax: 'i')
                    InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                    OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
            Leaving: {R1}

        Next (Regular) Block[B5]
}

Block[B6] - Exit
    Predecessors: [B4] [B5]
    Statements (0)
]]>.Value

            VerifyFlowGraphAndDiagnosticsForTest(Of MethodBlockSyntax)(source, expectedFlowGraph, expectedDiagnostics)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation, CompilerFeature.Dataflow)>
        <Fact()>
        Public Sub ForToFlow_36()
            Dim source = <![CDATA[
Imports System
Public Class C
    Sub M(i As Object, init1 As Object, bLimit As Boolean, limit1 As Object, limit2 As Object, step1 As Object, result As Boolean) 'BIND:"Sub M"
        For i = init1 To If(bLimit, limit1, limit2) Step step1
            result = true
        Next
    End Sub
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
    Locals: [<anonymous local> As System.Object]
    .locals {R2}
    {
        CaptureIds: [0] [1] [2]
        Block[B1] - Block
            Predecessors: [B0]
            Statements (2)
                IFlowCaptureOperation: 0 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'i')
                  Value: 
                    IParameterReferenceOperation: i (OperationKind.ParameterReference, Type: System.Object) (Syntax: 'i')

                IFlowCaptureOperation: 1 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'init1')
                  Value: 
                    IParameterReferenceOperation: init1 (OperationKind.ParameterReference, Type: System.Object) (Syntax: 'init1')

            Jump if False (Regular) to Block[B3]
                IParameterReferenceOperation: bLimit (OperationKind.ParameterReference, Type: System.Boolean) (Syntax: 'bLimit')

            Next (Regular) Block[B2]
        Block[B2] - Block
            Predecessors: [B1]
            Statements (1)
                IFlowCaptureOperation: 2 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'limit1')
                  Value: 
                    IParameterReferenceOperation: limit1 (OperationKind.ParameterReference, Type: System.Object) (Syntax: 'limit1')

            Next (Regular) Block[B4]
        Block[B3] - Block
            Predecessors: [B1]
            Statements (1)
                IFlowCaptureOperation: 2 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'limit2')
                  Value: 
                    IParameterReferenceOperation: limit2 (OperationKind.ParameterReference, Type: System.Object) (Syntax: 'limit2')

            Next (Regular) Block[B4]
        Block[B4] - Block
            Predecessors: [B2] [B3]
            Statements (0)
            Jump if False (Regular) to Block[B6]
                IInvocationOperation (Function Microsoft.VisualBasic.CompilerServices.ObjectFlowControl.ForLoopControl.ForLoopInitObj(Counter As System.Object, Start As System.Object, Limit As System.Object, StepValue As System.Object, ByRef LoopForResult As System.Object, ByRef CounterResult As System.Object) As System.Boolean) (OperationKind.Invocation, Type: System.Boolean, IsImplicit) (Syntax: 'If(bLimit,  ... t1, limit2)')
                  Instance Receiver: 
                    null
                  Arguments(6):
                      IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: Counter) (OperationKind.Argument, Type: null, IsImplicit) (Syntax: 'i')
                        IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: System.Object, IsImplicit) (Syntax: 'i')
                        InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                        OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                      IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: Start) (OperationKind.Argument, Type: null, IsImplicit) (Syntax: 'init1')
                        IFlowCaptureReferenceOperation: 1 (OperationKind.FlowCaptureReference, Type: System.Object, IsImplicit) (Syntax: 'init1')
                        InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                        OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                      IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: Limit) (OperationKind.Argument, Type: null, IsImplicit) (Syntax: 'If(bLimit,  ... t1, limit2)')
                        IFlowCaptureReferenceOperation: 2 (OperationKind.FlowCaptureReference, Type: System.Object, IsImplicit) (Syntax: 'If(bLimit,  ... t1, limit2)')
                        InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                        OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                      IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: StepValue) (OperationKind.Argument, Type: null, IsImplicit) (Syntax: 'step1')
                        IParameterReferenceOperation: step1 (OperationKind.ParameterReference, Type: System.Object) (Syntax: 'step1')
                        InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                        OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                      IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: LoopForResult) (OperationKind.Argument, Type: null, IsImplicit) (Syntax: 'i')
                        ILocalReferenceOperation:  (IsDeclaration: True) (OperationKind.LocalReference, Type: System.Object, IsImplicit) (Syntax: 'i')
                        InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                        OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                      IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: CounterResult) (OperationKind.Argument, Type: null, IsImplicit) (Syntax: 'i')
                        IParameterReferenceOperation: i (OperationKind.ParameterReference, Type: System.Object, IsImplicit) (Syntax: 'i')
                        InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                        OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                Leaving: {R2} {R1}

            Next (Regular) Block[B5]
                Leaving: {R2}
    }

    Block[B5] - Block
        Predecessors: [B4] [B5]
        Statements (1)
            IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'result = true')
              Expression: 
                ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Boolean, IsImplicit) (Syntax: 'result = true')
                  Left: 
                    IParameterReferenceOperation: result (OperationKind.ParameterReference, Type: System.Boolean) (Syntax: 'result')
                  Right: 
                    ILiteralOperation (OperationKind.Literal, Type: System.Boolean, Constant: True) (Syntax: 'true')

        Jump if False (Regular) to Block[B6]
            IInvocationOperation (Function Microsoft.VisualBasic.CompilerServices.ObjectFlowControl.ForLoopControl.ForNextCheckObj(Counter As System.Object, LoopObj As System.Object, ByRef CounterResult As System.Object) As System.Boolean) (OperationKind.Invocation, Type: System.Boolean, IsImplicit) (Syntax: 'If(bLimit,  ... t1, limit2)')
              Instance Receiver: 
                null
              Arguments(3):
                  IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: Counter) (OperationKind.Argument, Type: null, IsImplicit) (Syntax: 'If(bLimit,  ... t1, limit2)')
                    IParameterReferenceOperation: i (OperationKind.ParameterReference, Type: System.Object, IsImplicit) (Syntax: 'i')
                    InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                    OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                  IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: LoopObj) (OperationKind.Argument, Type: null, IsImplicit) (Syntax: 'If(bLimit,  ... t1, limit2)')
                    ILocalReferenceOperation:  (OperationKind.LocalReference, Type: System.Object, IsImplicit) (Syntax: 'i')
                    InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                    OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                  IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: CounterResult) (OperationKind.Argument, Type: null, IsImplicit) (Syntax: 'If(bLimit,  ... t1, limit2)')
                    IParameterReferenceOperation: i (OperationKind.ParameterReference, Type: System.Object, IsImplicit) (Syntax: 'i')
                    InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                    OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
            Leaving: {R1}

        Next (Regular) Block[B5]
}

Block[B6] - Exit
    Predecessors: [B4] [B5]
    Statements (0)
]]>.Value

            VerifyFlowGraphAndDiagnosticsForTest(Of MethodBlockSyntax)(source, expectedFlowGraph, expectedDiagnostics)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation, CompilerFeature.Dataflow)>
        <Fact()>
        Public Sub ForToFlow_37()
            Dim source = <![CDATA[
Imports System
Public Class C
    Sub M(i As Object, init1 As Object, limit1 As Object,  bStep As Boolean, step1 As Object, step2 As Object, result As Boolean) 'BIND:"Sub M"
        For i = init1 To limit1 Step If(bStep, step1, step2)
            result = true
        Next
    End Sub
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
    Locals: [<anonymous local> As System.Object]
    .locals {R2}
    {
        CaptureIds: [0] [1] [2] [3]
        Block[B1] - Block
            Predecessors: [B0]
            Statements (3)
                IFlowCaptureOperation: 0 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'i')
                  Value: 
                    IParameterReferenceOperation: i (OperationKind.ParameterReference, Type: System.Object) (Syntax: 'i')

                IFlowCaptureOperation: 1 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'init1')
                  Value: 
                    IParameterReferenceOperation: init1 (OperationKind.ParameterReference, Type: System.Object) (Syntax: 'init1')

                IFlowCaptureOperation: 2 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'limit1')
                  Value: 
                    IParameterReferenceOperation: limit1 (OperationKind.ParameterReference, Type: System.Object) (Syntax: 'limit1')

            Jump if False (Regular) to Block[B3]
                IParameterReferenceOperation: bStep (OperationKind.ParameterReference, Type: System.Boolean) (Syntax: 'bStep')

            Next (Regular) Block[B2]
        Block[B2] - Block
            Predecessors: [B1]
            Statements (1)
                IFlowCaptureOperation: 3 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'step1')
                  Value: 
                    IParameterReferenceOperation: step1 (OperationKind.ParameterReference, Type: System.Object) (Syntax: 'step1')

            Next (Regular) Block[B4]
        Block[B3] - Block
            Predecessors: [B1]
            Statements (1)
                IFlowCaptureOperation: 3 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'step2')
                  Value: 
                    IParameterReferenceOperation: step2 (OperationKind.ParameterReference, Type: System.Object) (Syntax: 'step2')

            Next (Regular) Block[B4]
        Block[B4] - Block
            Predecessors: [B2] [B3]
            Statements (0)
            Jump if False (Regular) to Block[B6]
                IInvocationOperation (Function Microsoft.VisualBasic.CompilerServices.ObjectFlowControl.ForLoopControl.ForLoopInitObj(Counter As System.Object, Start As System.Object, Limit As System.Object, StepValue As System.Object, ByRef LoopForResult As System.Object, ByRef CounterResult As System.Object) As System.Boolean) (OperationKind.Invocation, Type: System.Boolean, IsImplicit) (Syntax: 'limit1')
                  Instance Receiver: 
                    null
                  Arguments(6):
                      IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: Counter) (OperationKind.Argument, Type: null, IsImplicit) (Syntax: 'i')
                        IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: System.Object, IsImplicit) (Syntax: 'i')
                        InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                        OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                      IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: Start) (OperationKind.Argument, Type: null, IsImplicit) (Syntax: 'init1')
                        IFlowCaptureReferenceOperation: 1 (OperationKind.FlowCaptureReference, Type: System.Object, IsImplicit) (Syntax: 'init1')
                        InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                        OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                      IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: Limit) (OperationKind.Argument, Type: null, IsImplicit) (Syntax: 'limit1')
                        IFlowCaptureReferenceOperation: 2 (OperationKind.FlowCaptureReference, Type: System.Object, IsImplicit) (Syntax: 'limit1')
                        InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                        OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                      IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: StepValue) (OperationKind.Argument, Type: null, IsImplicit) (Syntax: 'If(bStep, step1, step2)')
                        IFlowCaptureReferenceOperation: 3 (OperationKind.FlowCaptureReference, Type: System.Object, IsImplicit) (Syntax: 'If(bStep, step1, step2)')
                        InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                        OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                      IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: LoopForResult) (OperationKind.Argument, Type: null, IsImplicit) (Syntax: 'i')
                        ILocalReferenceOperation:  (IsDeclaration: True) (OperationKind.LocalReference, Type: System.Object, IsImplicit) (Syntax: 'i')
                        InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                        OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                      IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: CounterResult) (OperationKind.Argument, Type: null, IsImplicit) (Syntax: 'i')
                        IParameterReferenceOperation: i (OperationKind.ParameterReference, Type: System.Object, IsImplicit) (Syntax: 'i')
                        InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                        OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                Leaving: {R2} {R1}

            Next (Regular) Block[B5]
                Leaving: {R2}
    }

    Block[B5] - Block
        Predecessors: [B4] [B5]
        Statements (1)
            IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'result = true')
              Expression: 
                ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Boolean, IsImplicit) (Syntax: 'result = true')
                  Left: 
                    IParameterReferenceOperation: result (OperationKind.ParameterReference, Type: System.Boolean) (Syntax: 'result')
                  Right: 
                    ILiteralOperation (OperationKind.Literal, Type: System.Boolean, Constant: True) (Syntax: 'true')

        Jump if False (Regular) to Block[B6]
            IInvocationOperation (Function Microsoft.VisualBasic.CompilerServices.ObjectFlowControl.ForLoopControl.ForNextCheckObj(Counter As System.Object, LoopObj As System.Object, ByRef CounterResult As System.Object) As System.Boolean) (OperationKind.Invocation, Type: System.Boolean, IsImplicit) (Syntax: 'limit1')
              Instance Receiver: 
                null
              Arguments(3):
                  IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: Counter) (OperationKind.Argument, Type: null, IsImplicit) (Syntax: 'limit1')
                    IParameterReferenceOperation: i (OperationKind.ParameterReference, Type: System.Object, IsImplicit) (Syntax: 'i')
                    InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                    OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                  IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: LoopObj) (OperationKind.Argument, Type: null, IsImplicit) (Syntax: 'limit1')
                    ILocalReferenceOperation:  (OperationKind.LocalReference, Type: System.Object, IsImplicit) (Syntax: 'i')
                    InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                    OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                  IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: CounterResult) (OperationKind.Argument, Type: null, IsImplicit) (Syntax: 'limit1')
                    IParameterReferenceOperation: i (OperationKind.ParameterReference, Type: System.Object, IsImplicit) (Syntax: 'i')
                    InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                    OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
            Leaving: {R1}

        Next (Regular) Block[B5]
}

Block[B6] - Exit
    Predecessors: [B4] [B5]
    Statements (0)
]]>.Value

            VerifyFlowGraphAndDiagnosticsForTest(Of MethodBlockSyntax)(source, expectedFlowGraph, expectedDiagnostics)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation, CompilerFeature.Dataflow)>
        <Fact()>
        Public Sub ForToFlow_38()
            Dim source = <![CDATA[
Imports System
Public Class C
    Sub M(i As Object, init As Object, limit As Object, result As Object) 'BIND:"Sub M"
        For i = init To limit
            result = i
        Next
    End Sub
End Class
]]>.Value

            Dim compilation = CreateCompilationWithMscorlib461(source, options:=TestOptions.ReleaseDebugDll)

            Dim expectedDiagnostics = String.Empty

            Dim expectedFlowGraph = <![CDATA[
Block[B0] - Entry
    Statements (0)
    Next (Regular) Block[B1]
        Entering: {R1}

.locals {R1}
{
    Locals: [<anonymous local> As System.Object]
    Block[B1] - Block
        Predecessors: [B0]
        Statements (0)
        Jump if False (Regular) to Block[B3]
            IInvalidOperation (OperationKind.Invalid, Type: System.Boolean, IsImplicit) (Syntax: 'limit')
              Children(5):
                  IParameterReferenceOperation: i (OperationKind.ParameterReference, Type: System.Object) (Syntax: 'i')
                  IParameterReferenceOperation: init (OperationKind.ParameterReference, Type: System.Object) (Syntax: 'init')
                  IParameterReferenceOperation: limit (OperationKind.ParameterReference, Type: System.Object) (Syntax: 'limit')
                  IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.Object, IsImplicit) (Syntax: 'For i = ini ... Next')
                    Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                      (WideningValue)
                    Operand: 
                      ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 1, IsImplicit) (Syntax: 'For i = ini ... Next')
                  ILocalReferenceOperation:  (IsDeclaration: True) (OperationKind.LocalReference, Type: System.Object, IsImplicit) (Syntax: 'i')
            Leaving: {R1}

        Next (Regular) Block[B2]
    Block[B2] - Block
        Predecessors: [B1] [B2]
        Statements (1)
            IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'result = i')
              Expression: 
                ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Object, IsImplicit) (Syntax: 'result = i')
                  Left: 
                    IParameterReferenceOperation: result (OperationKind.ParameterReference, Type: System.Object) (Syntax: 'result')
                  Right: 
                    IParameterReferenceOperation: i (OperationKind.ParameterReference, Type: System.Object) (Syntax: 'i')

        Jump if False (Regular) to Block[B3]
            IInvalidOperation (OperationKind.Invalid, Type: System.Boolean, IsImplicit) (Syntax: 'limit')
              Children(2):
                  IParameterReferenceOperation: i (OperationKind.ParameterReference, Type: System.Object, IsImplicit) (Syntax: 'i')
                  ILocalReferenceOperation:  (OperationKind.LocalReference, Type: System.Object, IsImplicit) (Syntax: 'i')
            Leaving: {R1}

        Next (Regular) Block[B2]
}

Block[B3] - Exit
    Predecessors: [B1] [B2]
    Statements (0)
]]>.Value

            VerifyFlowGraphAndDiagnosticsForTest(Of MethodBlockSyntax)(compilation, expectedFlowGraph, expectedDiagnostics)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation, CompilerFeature.Dataflow)>
        <Fact()>
        Public Sub ForToFlow_39()
            Dim source = <![CDATA[
Imports System
Public Class C
    Sub M(i As Integer?, init As Integer?, limit As Integer?, [step] As Integer?, result As Integer?) 'BIND:"Sub M"
        For i = init To limit Step [step]
            result = i
        Next
    End Sub
End Class
]]>.Value

            Dim compilation = CreateCompilationWithMscorlib461(source, options:=TestOptions.ReleaseDebugDll)
            compilation.MakeMemberMissing(SpecialMember.System_Nullable_T_GetValueOrDefault)

            Dim expectedDiagnostics = String.Empty

            Dim expectedFlowGraph = <![CDATA[
Block[B0] - Entry
    Statements (0)
    Next (Regular) Block[B1]
        Entering: {R1} {R2}

.locals {R1}
{
    CaptureIds: [2] [3] [4]
    .locals {R2}
    {
        CaptureIds: [0] [1]
        Block[B1] - Block
            Predecessors: [B0]
            Statements (4)
                IFlowCaptureOperation: 0 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'i')
                  Value: 
                    IParameterReferenceOperation: i (OperationKind.ParameterReference, Type: System.Nullable(Of System.Int32)) (Syntax: 'i')

                IFlowCaptureOperation: 1 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'init')
                  Value: 
                    IParameterReferenceOperation: init (OperationKind.ParameterReference, Type: System.Nullable(Of System.Int32)) (Syntax: 'init')

                IFlowCaptureOperation: 2 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'limit')
                  Value: 
                    IParameterReferenceOperation: limit (OperationKind.ParameterReference, Type: System.Nullable(Of System.Int32)) (Syntax: 'limit')

                IFlowCaptureOperation: 3 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: '[step]')
                  Value: 
                    IParameterReferenceOperation: step (OperationKind.ParameterReference, Type: System.Nullable(Of System.Int32)) (Syntax: '[step]')

            Jump if False (Regular) to Block[B3]
                IIsNullOperation (OperationKind.IsNull, Type: System.Boolean, IsImplicit) (Syntax: '[step]')
                  Operand: 
                    IFlowCaptureReferenceOperation: 3 (OperationKind.FlowCaptureReference, Type: System.Nullable(Of System.Int32), IsImplicit) (Syntax: '[step]')

            Next (Regular) Block[B2]
        Block[B2] - Block
            Predecessors: [B1]
            Statements (1)
                IFlowCaptureOperation: 4 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: '[step]')
                  Value: 
                    ILiteralOperation (OperationKind.Literal, Type: System.Boolean, Constant: False, IsImplicit) (Syntax: '[step]')

            Next (Regular) Block[B4]
        Block[B3] - Block
            Predecessors: [B1]
            Statements (1)
                IFlowCaptureOperation: 4 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: '[step]')
                  Value: 
                    IBinaryOperation (BinaryOperatorKind.GreaterThanOrEqual) (OperationKind.Binary, Type: System.Boolean, IsImplicit) (Syntax: '[step]')
                      Left: 
                        IInvalidOperation (OperationKind.Invalid, Type: System.Int32, IsImplicit) (Syntax: '[step]')
                          Children(1):
                              IFlowCaptureReferenceOperation: 3 (OperationKind.FlowCaptureReference, Type: System.Nullable(Of System.Int32), IsImplicit) (Syntax: '[step]')
                      Right: 
                        ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 0, IsImplicit) (Syntax: '[step]')

            Next (Regular) Block[B4]
        Block[B4] - Block
            Predecessors: [B2] [B3]
            Statements (1)
                ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: null, IsImplicit) (Syntax: 'init')
                  Left: 
                    IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: System.Nullable(Of System.Int32), IsImplicit) (Syntax: 'i')
                  Right: 
                    IFlowCaptureReferenceOperation: 1 (OperationKind.FlowCaptureReference, Type: System.Nullable(Of System.Int32), IsImplicit) (Syntax: 'init')

            Next (Regular) Block[B5]
                Leaving: {R2}
    }

    Block[B5] - Block
        Predecessors: [B4] [B11] [B12]
        Statements (0)
        Jump if False (Regular) to Block[B6]
            IBinaryOperation (BinaryOperatorKind.Or) (OperationKind.Binary, Type: System.Boolean, IsImplicit) (Syntax: '[step]')
              Left: 
                IIsNullOperation (OperationKind.IsNull, Type: System.Boolean, IsImplicit) (Syntax: 'limit')
                  Operand: 
                    IFlowCaptureReferenceOperation: 2 (OperationKind.FlowCaptureReference, Type: System.Nullable(Of System.Int32), IsImplicit) (Syntax: 'limit')
              Right: 
                IIsNullOperation (OperationKind.IsNull, Type: System.Boolean, IsImplicit) (Syntax: 'i')
                  Operand: 
                    IParameterReferenceOperation: i (OperationKind.ParameterReference, Type: System.Nullable(Of System.Int32), IsImplicit) (Syntax: 'i')
            Entering: {R3}

        Next (Regular) Block[B13]
            Leaving: {R1}

    .locals {R3}
    {
        CaptureIds: [5]
        Block[B6] - Block
            Predecessors: [B5]
            Statements (1)
                IFlowCaptureOperation: 5 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'i')
                  Value: 
                    IInvalidOperation (OperationKind.Invalid, Type: System.Int32, IsImplicit) (Syntax: 'i')
                      Children(1):
                          IParameterReferenceOperation: i (OperationKind.ParameterReference, Type: System.Nullable(Of System.Int32), IsImplicit) (Syntax: 'i')

            Jump if False (Regular) to Block[B8]
                IFlowCaptureReferenceOperation: 4 (OperationKind.FlowCaptureReference, Type: System.Boolean, IsImplicit) (Syntax: '[step]')

            Next (Regular) Block[B7]
        Block[B7] - Block
            Predecessors: [B6]
            Statements (0)
            Jump if False (Regular) to Block[B13]
                IBinaryOperation (BinaryOperatorKind.LessThanOrEqual) (OperationKind.Binary, Type: System.Boolean, IsImplicit) (Syntax: 'limit')
                  Left: 
                    IFlowCaptureReferenceOperation: 5 (OperationKind.FlowCaptureReference, Type: System.Int32, IsImplicit) (Syntax: 'i')
                  Right: 
                    IInvalidOperation (OperationKind.Invalid, Type: System.Int32, IsImplicit) (Syntax: 'limit')
                      Children(1):
                          IFlowCaptureReferenceOperation: 2 (OperationKind.FlowCaptureReference, Type: System.Nullable(Of System.Int32), IsImplicit) (Syntax: 'limit')
                Leaving: {R3} {R1}

            Next (Regular) Block[B9]
                Leaving: {R3}
        Block[B8] - Block
            Predecessors: [B6]
            Statements (0)
            Jump if False (Regular) to Block[B13]
                IBinaryOperation (BinaryOperatorKind.GreaterThanOrEqual) (OperationKind.Binary, Type: System.Boolean, IsImplicit) (Syntax: 'limit')
                  Left: 
                    IFlowCaptureReferenceOperation: 5 (OperationKind.FlowCaptureReference, Type: System.Int32, IsImplicit) (Syntax: 'i')
                  Right: 
                    IInvalidOperation (OperationKind.Invalid, Type: System.Int32, IsImplicit) (Syntax: 'limit')
                      Children(1):
                          IFlowCaptureReferenceOperation: 2 (OperationKind.FlowCaptureReference, Type: System.Nullable(Of System.Int32), IsImplicit) (Syntax: 'limit')
                Leaving: {R3} {R1}

            Next (Regular) Block[B9]
                Leaving: {R3}
    }

    Block[B9] - Block
        Predecessors: [B7] [B8]
        Statements (1)
            IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'result = i')
              Expression: 
                ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Nullable(Of System.Int32), IsImplicit) (Syntax: 'result = i')
                  Left: 
                    IParameterReferenceOperation: result (OperationKind.ParameterReference, Type: System.Nullable(Of System.Int32)) (Syntax: 'result')
                  Right: 
                    IParameterReferenceOperation: i (OperationKind.ParameterReference, Type: System.Nullable(Of System.Int32)) (Syntax: 'i')

        Next (Regular) Block[B10]
            Entering: {R4}

    .locals {R4}
    {
        CaptureIds: [6]
        Block[B10] - Block
            Predecessors: [B9]
            Statements (1)
                IFlowCaptureOperation: 6 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'i')
                  Value: 
                    IParameterReferenceOperation: i (OperationKind.ParameterReference, Type: System.Nullable(Of System.Int32), IsImplicit) (Syntax: 'i')

            Jump if False (Regular) to Block[B12]
                IBinaryOperation (BinaryOperatorKind.Or) (OperationKind.Binary, Type: System.Boolean, IsImplicit) (Syntax: '[step]')
                  Left: 
                    IIsNullOperation (OperationKind.IsNull, Type: System.Boolean, IsImplicit) (Syntax: '[step]')
                      Operand: 
                        IFlowCaptureReferenceOperation: 3 (OperationKind.FlowCaptureReference, Type: System.Nullable(Of System.Int32), IsImplicit) (Syntax: '[step]')
                  Right: 
                    IIsNullOperation (OperationKind.IsNull, Type: System.Boolean, IsImplicit) (Syntax: 'i')
                      Operand: 
                        IParameterReferenceOperation: i (OperationKind.ParameterReference, Type: System.Nullable(Of System.Int32), IsImplicit) (Syntax: 'i')

            Next (Regular) Block[B11]
        Block[B11] - Block
            Predecessors: [B10]
            Statements (1)
                ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: null, IsImplicit) (Syntax: 'i')
                  Left: 
                    IFlowCaptureReferenceOperation: 6 (OperationKind.FlowCaptureReference, Type: System.Nullable(Of System.Int32), IsImplicit) (Syntax: 'i')
                  Right: 
                    IDefaultValueOperation (OperationKind.DefaultValue, Type: System.Nullable(Of System.Int32), IsImplicit) (Syntax: 'i')

            Next (Regular) Block[B5]
                Leaving: {R4}
        Block[B12] - Block
            Predecessors: [B10]
            Statements (1)
                ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: null, IsImplicit) (Syntax: 'i')
                  Left: 
                    IFlowCaptureReferenceOperation: 6 (OperationKind.FlowCaptureReference, Type: System.Nullable(Of System.Int32), IsImplicit) (Syntax: 'i')
                  Right: 
                    IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.Nullable(Of System.Int32), IsImplicit) (Syntax: '[step]')
                      Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                        (WideningNullable)
                      Operand: 
                        IBinaryOperation (BinaryOperatorKind.Add, Checked) (OperationKind.Binary, Type: System.Int32, IsImplicit) (Syntax: '[step]')
                          Left: 
                            IInvalidOperation (OperationKind.Invalid, Type: System.Int32, IsImplicit) (Syntax: 'i')
                              Children(1):
                                  IParameterReferenceOperation: i (OperationKind.ParameterReference, Type: System.Nullable(Of System.Int32), IsImplicit) (Syntax: 'i')
                          Right: 
                            IInvalidOperation (OperationKind.Invalid, Type: System.Int32, IsImplicit) (Syntax: '[step]')
                              Children(1):
                                  IFlowCaptureReferenceOperation: 3 (OperationKind.FlowCaptureReference, Type: System.Nullable(Of System.Int32), IsImplicit) (Syntax: '[step]')

            Next (Regular) Block[B5]
                Leaving: {R4}
    }
}

Block[B13] - Exit
    Predecessors: [B5] [B7] [B8]
    Statements (0)
]]>.Value

            VerifyFlowGraphAndDiagnosticsForTest(Of MethodBlockSyntax)(compilation, expectedFlowGraph, expectedDiagnostics)
        End Sub
    End Class
End Namespace
