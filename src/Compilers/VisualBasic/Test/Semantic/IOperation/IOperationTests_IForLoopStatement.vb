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
IForToLoopStatement (LoopKind.ForTo) (OperationKind.LoopStatement) (Syntax: 'For i As In ... Next')
  Locals: Local_1: i As System.Int32
  LoopControlVariable: ILocalReferenceExpression: i (OperationKind.LocalReferenceExpression, Type: System.Int32) (Syntax: 'i As Integer')
  InitialValue: ILiteralExpression (OperationKind.LiteralExpression, Type: System.Int32, Constant: 0) (Syntax: '0')
  LimitValue: IBinaryOperatorExpression (BinaryOperatorKind.Subtract, Checked) (OperationKind.BinaryOperatorExpression, Type: System.Int32) (Syntax: 'myarray.Length - 1')
      Left: IPropertyReferenceExpression: ReadOnly Property System.Array.Length As System.Int32 (OperationKind.PropertyReferenceExpression, Type: System.Int32) (Syntax: 'myarray.Length')
          Instance Receiver: ILocalReferenceExpression: myarray (OperationKind.LocalReferenceExpression, Type: System.Int32()) (Syntax: 'myarray')
      Right: ILiteralExpression (OperationKind.LiteralExpression, Type: System.Int32, Constant: 1) (Syntax: '1')
  StepValue: IConversionExpression (Explicit, TryCast: False, Unchecked) (OperationKind.ConversionExpression, Type: System.Int32, Constant: 1) (Syntax: 'For i As In ... Next')
      Conversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
      Operand: ILiteralExpression (OperationKind.LiteralExpression, Type: System.Int32, Constant: 1) (Syntax: 'For i As In ... Next')
  Body: IBlockStatement (1 statements) (OperationKind.BlockStatement) (Syntax: 'For i As In ... Next')
      IExpressionStatement (OperationKind.ExpressionStatement) (Syntax: 'System.Cons ... myarray(i))')
        Expression: IInvocationExpression (Sub System.Console.WriteLine(value As System.Int32)) (OperationKind.InvocationExpression, Type: System.Void) (Syntax: 'System.Cons ... myarray(i))')
            Instance Receiver: null
            Arguments(1):
                IArgument (ArgumentKind.Explicit, Matching Parameter: value) (OperationKind.Argument) (Syntax: 'myarray(i)')
                  IArrayElementReferenceExpression (OperationKind.None) (Syntax: 'myarray(i)')
                    Array reference: ILocalReferenceExpression: myarray (OperationKind.LocalReferenceExpression, Type: System.Int32()) (Syntax: 'myarray')
                    Indices(1):
                        ILocalReferenceExpression: i (OperationKind.LocalReferenceExpression, Type: System.Int32) (Syntax: 'i')
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
IForToLoopStatement (LoopKind.ForTo) (OperationKind.LoopStatement) (Syntax: 'For i As In ... Next')
  Locals: Local_1: i As System.Int32
  LoopControlVariable: ILocalReferenceExpression: i (OperationKind.LocalReferenceExpression, Type: System.Int32) (Syntax: 'i As Integer')
  InitialValue: ILiteralExpression (OperationKind.LiteralExpression, Type: System.Int32, Constant: 0) (Syntax: '0')
  LimitValue: IConversionExpression (Implicit, TryCast: False, Unchecked) (OperationKind.ConversionExpression, Type: System.Int32) (Syntax: '"1"')
      Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
      Operand: ILiteralExpression (OperationKind.LiteralExpression, Type: System.String, Constant: "1") (Syntax: '"1"')
  StepValue: IConversionExpression (Implicit, TryCast: False, Unchecked) (OperationKind.ConversionExpression, Type: System.Int32) (Syntax: 's')
      Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: True, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
      Operand: ILocalReferenceExpression: s (OperationKind.LocalReferenceExpression, Type: System.Double) (Syntax: 's')
  Body: IBlockStatement (1 statements) (OperationKind.BlockStatement) (Syntax: 'For i As In ... Next')
      IExpressionStatement (OperationKind.ExpressionStatement) (Syntax: 'System.Cons ... myarray(i))')
        Expression: IInvocationExpression (Sub System.Console.WriteLine(value As System.Int32)) (OperationKind.InvocationExpression, Type: System.Void) (Syntax: 'System.Cons ... myarray(i))')
            Instance Receiver: null
            Arguments(1):
                IArgument (ArgumentKind.Explicit, Matching Parameter: value) (OperationKind.Argument) (Syntax: 'myarray(i)')
                  IArrayElementReferenceExpression (OperationKind.None) (Syntax: 'myarray(i)')
                    Array reference: ILocalReferenceExpression: myarray (OperationKind.LocalReferenceExpression, Type: System.Int32()) (Syntax: 'myarray')
                    Indices(1):
                        ILocalReferenceExpression: i (OperationKind.LocalReferenceExpression, Type: System.Int32) (Syntax: 'i')
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
IForToLoopStatement (LoopKind.ForTo) (OperationKind.LoopStatement) (Syntax: 'For i As Do ... Next')
  Locals: Local_1: i As System.Double
  LoopControlVariable: ILocalReferenceExpression: i (OperationKind.LocalReferenceExpression, Type: System.Double) (Syntax: 'i As Double')
  InitialValue: IConversionExpression (Implicit, TryCast: False, Unchecked) (OperationKind.ConversionExpression, Type: System.Double, Constant: 2) (Syntax: '2')
      Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: True, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
      Operand: ILiteralExpression (OperationKind.LiteralExpression, Type: System.Int32, Constant: 2) (Syntax: '2')
  LimitValue: IConversionExpression (Implicit, TryCast: False, Unchecked) (OperationKind.ConversionExpression, Type: System.Double, Constant: 0) (Syntax: '0')
      Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: True, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
      Operand: ILiteralExpression (OperationKind.LiteralExpression, Type: System.Int32, Constant: 0) (Syntax: '0')
  StepValue: ILocalReferenceExpression: s (OperationKind.LocalReferenceExpression, Type: System.Double) (Syntax: 's')
  Body: IBlockStatement (1 statements) (OperationKind.BlockStatement) (Syntax: 'For i As Do ... Next')
      IExpressionStatement (OperationKind.ExpressionStatement) (Syntax: 'System.Cons ... riteLine(i)')
        Expression: IInvocationExpression (Sub System.Console.WriteLine(value As System.Double)) (OperationKind.InvocationExpression, Type: System.Void) (Syntax: 'System.Cons ... riteLine(i)')
            Instance Receiver: null
            Arguments(1):
                IArgument (ArgumentKind.Explicit, Matching Parameter: value) (OperationKind.Argument) (Syntax: 'i')
                  ILocalReferenceExpression: i (OperationKind.LocalReferenceExpression, Type: System.Double) (Syntax: 'i')
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
IForToLoopStatement (LoopKind.ForTo) (OperationKind.LoopStatement) (Syntax: 'For ctrlVar ... Next')
  LoopControlVariable: ILocalReferenceExpression: ctrlVar (OperationKind.LocalReferenceExpression, Type: System.Object) (Syntax: 'ctrlVar')
  InitialValue: ILocalReferenceExpression: initValue (OperationKind.LocalReferenceExpression, Type: System.Object) (Syntax: 'initValue')
  LimitValue: ILocalReferenceExpression: limit (OperationKind.LocalReferenceExpression, Type: System.Object) (Syntax: 'limit')
  StepValue: ILocalReferenceExpression: stp (OperationKind.LocalReferenceExpression, Type: System.Object) (Syntax: 'stp')
  Body: IBlockStatement (1 statements) (OperationKind.BlockStatement) (Syntax: 'For ctrlVar ... Next')
      IExpressionStatement (OperationKind.ExpressionStatement) (Syntax: 'System.Cons ... ne(ctrlVar)')
        Expression: IInvocationExpression (Sub System.Console.WriteLine(value As System.Object)) (OperationKind.InvocationExpression, Type: System.Void) (Syntax: 'System.Cons ... ne(ctrlVar)')
            Instance Receiver: null
            Arguments(1):
                IArgument (ArgumentKind.Explicit, Matching Parameter: value) (OperationKind.Argument) (Syntax: 'ctrlVar')
                  ILocalReferenceExpression: ctrlVar (OperationKind.LocalReferenceExpression, Type: System.Object) (Syntax: 'ctrlVar')
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
IForToLoopStatement (LoopKind.ForTo) (OperationKind.LoopStatement) (Syntax: 'For AVarNam ... xt AVarName')
  Locals: Local_1: AVarName As System.Int32
  LoopControlVariable: ILocalReferenceExpression: AVarName (OperationKind.LocalReferenceExpression, Type: System.Int32) (Syntax: 'AVarName')
  InitialValue: ILiteralExpression (OperationKind.LiteralExpression, Type: System.Int32, Constant: 1) (Syntax: '1')
  LimitValue: ILiteralExpression (OperationKind.LiteralExpression, Type: System.Int32, Constant: 2) (Syntax: '2')
  StepValue: IConversionExpression (Explicit, TryCast: False, Unchecked) (OperationKind.ConversionExpression, Type: System.Int32, Constant: 1) (Syntax: 'For AVarNam ... xt AVarName')
      Conversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
      Operand: ILiteralExpression (OperationKind.LiteralExpression, Type: System.Int32, Constant: 1) (Syntax: 'For AVarNam ... xt AVarName')
  Body: IBlockStatement (1 statements) (OperationKind.BlockStatement) (Syntax: 'For AVarNam ... xt AVarName')
      IForToLoopStatement (LoopKind.ForTo) (OperationKind.LoopStatement) (Syntax: 'For B = 1 T ... Next B')
        Locals: Local_1: B As System.Int32
        LoopControlVariable: ILocalReferenceExpression: B (OperationKind.LocalReferenceExpression, Type: System.Int32) (Syntax: 'B')
        InitialValue: ILiteralExpression (OperationKind.LiteralExpression, Type: System.Int32, Constant: 1) (Syntax: '1')
        LimitValue: ILiteralExpression (OperationKind.LiteralExpression, Type: System.Int32, Constant: 2) (Syntax: '2')
        StepValue: IConversionExpression (Explicit, TryCast: False, Unchecked) (OperationKind.ConversionExpression, Type: System.Int32, Constant: 1) (Syntax: 'For B = 1 T ... Next B')
            Conversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
            Operand: ILiteralExpression (OperationKind.LiteralExpression, Type: System.Int32, Constant: 1) (Syntax: 'For B = 1 T ... Next B')
        Body: IBlockStatement (1 statements) (OperationKind.BlockStatement) (Syntax: 'For B = 1 T ... Next B')
            IForToLoopStatement (LoopKind.ForTo) (OperationKind.LoopStatement) (Syntax: 'For C = 1 T ... Next C')
              Locals: Local_1: C As System.Int32
              LoopControlVariable: ILocalReferenceExpression: C (OperationKind.LocalReferenceExpression, Type: System.Int32) (Syntax: 'C')
              InitialValue: ILiteralExpression (OperationKind.LiteralExpression, Type: System.Int32, Constant: 1) (Syntax: '1')
              LimitValue: ILiteralExpression (OperationKind.LiteralExpression, Type: System.Int32, Constant: 2) (Syntax: '2')
              StepValue: IConversionExpression (Explicit, TryCast: False, Unchecked) (OperationKind.ConversionExpression, Type: System.Int32, Constant: 1) (Syntax: 'For C = 1 T ... Next C')
                  Conversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                  Operand: ILiteralExpression (OperationKind.LiteralExpression, Type: System.Int32, Constant: 1) (Syntax: 'For C = 1 T ... Next C')
              Body: IBlockStatement (1 statements) (OperationKind.BlockStatement) (Syntax: 'For C = 1 T ... Next C')
                  IForToLoopStatement (LoopKind.ForTo) (OperationKind.LoopStatement) (Syntax: 'For D = 1 T ... Next D')
                    Locals: Local_1: D As System.Int32
                    LoopControlVariable: ILocalReferenceExpression: D (OperationKind.LocalReferenceExpression, Type: System.Int32) (Syntax: 'D')
                    InitialValue: ILiteralExpression (OperationKind.LiteralExpression, Type: System.Int32, Constant: 1) (Syntax: '1')
                    LimitValue: ILiteralExpression (OperationKind.LiteralExpression, Type: System.Int32, Constant: 2) (Syntax: '2')
                    StepValue: IConversionExpression (Explicit, TryCast: False, Unchecked) (OperationKind.ConversionExpression, Type: System.Int32, Constant: 1) (Syntax: 'For D = 1 T ... Next D')
                        Conversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                        Operand: ILiteralExpression (OperationKind.LiteralExpression, Type: System.Int32, Constant: 1) (Syntax: 'For D = 1 T ... Next D')
                    Body: IBlockStatement (0 statements) (OperationKind.BlockStatement) (Syntax: 'For D = 1 T ... Next D')
                    NextVariables(1):
                        ILocalReferenceExpression: D (OperationKind.LocalReferenceExpression, Type: System.Int32) (Syntax: 'D')
              NextVariables(1):
                  ILocalReferenceExpression: C (OperationKind.LocalReferenceExpression, Type: System.Int32) (Syntax: 'C')
        NextVariables(1):
            ILocalReferenceExpression: B (OperationKind.LocalReferenceExpression, Type: System.Int32) (Syntax: 'B')
  NextVariables(1):
      ILocalReferenceExpression: AVarName (OperationKind.LocalReferenceExpression, Type: System.Int32) (Syntax: 'AVarName')
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
IForToLoopStatement (LoopKind.ForTo) (OperationKind.LoopStatement) (Syntax: 'For I = 1 T ... Next')
  Locals: Local_1: I As System.Int32
  LoopControlVariable: ILocalReferenceExpression: I (OperationKind.LocalReferenceExpression, Type: System.Int32) (Syntax: 'I')
  InitialValue: ILiteralExpression (OperationKind.LiteralExpression, Type: System.Int32, Constant: 1) (Syntax: '1')
  LimitValue: ILiteralExpression (OperationKind.LiteralExpression, Type: System.Int32, Constant: 2) (Syntax: '2')
  StepValue: IConversionExpression (Explicit, TryCast: False, Unchecked) (OperationKind.ConversionExpression, Type: System.Int32, Constant: 1) (Syntax: 'For I = 1 T ... Next')
      Conversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
      Operand: ILiteralExpression (OperationKind.LiteralExpression, Type: System.Int32, Constant: 1) (Syntax: 'For I = 1 T ... Next')
  Body: IBlockStatement (1 statements) (OperationKind.BlockStatement) (Syntax: 'For I = 1 T ... Next')
      IForToLoopStatement (LoopKind.ForTo) (OperationKind.LoopStatement) (Syntax: 'For J = 1 T ... Next')
        Locals: Local_1: J As System.Int32
        LoopControlVariable: ILocalReferenceExpression: J (OperationKind.LocalReferenceExpression, Type: System.Int32) (Syntax: 'J')
        InitialValue: ILiteralExpression (OperationKind.LiteralExpression, Type: System.Int32, Constant: 1) (Syntax: '1')
        LimitValue: ILiteralExpression (OperationKind.LiteralExpression, Type: System.Int32, Constant: 2) (Syntax: '2')
        StepValue: IConversionExpression (Explicit, TryCast: False, Unchecked) (OperationKind.ConversionExpression, Type: System.Int32, Constant: 1) (Syntax: 'For J = 1 T ... Next')
            Conversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
            Operand: ILiteralExpression (OperationKind.LiteralExpression, Type: System.Int32, Constant: 1) (Syntax: 'For J = 1 T ... Next')
        Body: IBlockStatement (2 statements) (OperationKind.BlockStatement) (Syntax: 'For J = 1 T ... Next')
            IExpressionStatement (OperationKind.ExpressionStatement) (Syntax: 'I = 3')
              Expression: ISimpleAssignmentExpression (OperationKind.SimpleAssignmentExpression, Type: System.Int32) (Syntax: 'I = 3')
                  Left: ILocalReferenceExpression: I (OperationKind.LocalReferenceExpression, Type: System.Int32) (Syntax: 'I')
                  Right: ILiteralExpression (OperationKind.LiteralExpression, Type: System.Int32, Constant: 3) (Syntax: '3')
            IExpressionStatement (OperationKind.ExpressionStatement) (Syntax: 'System.Cons ... riteLine(I)')
              Expression: IInvocationExpression (Sub System.Console.WriteLine(value As System.Int32)) (OperationKind.InvocationExpression, Type: System.Void) (Syntax: 'System.Cons ... riteLine(I)')
                  Instance Receiver: null
                  Arguments(1):
                      IArgument (ArgumentKind.Explicit, Matching Parameter: value) (OperationKind.Argument) (Syntax: 'I')
                        ILocalReferenceExpression: I (OperationKind.LocalReferenceExpression, Type: System.Int32) (Syntax: 'I')
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
IForToLoopStatement (LoopKind.ForTo) (OperationKind.LoopStatement) (Syntax: 'For I = 1 T ... Next')
  Locals: Local_1: I As System.Int32
  LoopControlVariable: ILocalReferenceExpression: I (OperationKind.LocalReferenceExpression, Type: System.Int32) (Syntax: 'I')
  InitialValue: ILiteralExpression (OperationKind.LiteralExpression, Type: System.Int32, Constant: 1) (Syntax: '1')
  LimitValue: ILiteralExpression (OperationKind.LiteralExpression, Type: System.Int32, Constant: 2) (Syntax: '2')
  StepValue: IConversionExpression (Explicit, TryCast: False, Unchecked) (OperationKind.ConversionExpression, Type: System.Int32, Constant: 1) (Syntax: 'For I = 1 T ... Next')
      Conversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
      Operand: ILiteralExpression (OperationKind.LiteralExpression, Type: System.Int32, Constant: 1) (Syntax: 'For I = 1 T ... Next')
  Body: IBlockStatement (1 statements) (OperationKind.BlockStatement) (Syntax: 'For I = 1 T ... Next')
      IForToLoopStatement (LoopKind.ForTo) (OperationKind.LoopStatement) (Syntax: 'For J = I + ... Next')
        Locals: Local_1: J As System.Int32
        LoopControlVariable: ILocalReferenceExpression: J (OperationKind.LocalReferenceExpression, Type: System.Int32) (Syntax: 'J')
        InitialValue: IBinaryOperatorExpression (BinaryOperatorKind.Add, Checked) (OperationKind.BinaryOperatorExpression, Type: System.Int32) (Syntax: 'I + 1')
            Left: ILocalReferenceExpression: I (OperationKind.LocalReferenceExpression, Type: System.Int32) (Syntax: 'I')
            Right: ILiteralExpression (OperationKind.LiteralExpression, Type: System.Int32, Constant: 1) (Syntax: '1')
        LimitValue: ILiteralExpression (OperationKind.LiteralExpression, Type: System.Int32, Constant: 2) (Syntax: '2')
        StepValue: IConversionExpression (Explicit, TryCast: False, Unchecked) (OperationKind.ConversionExpression, Type: System.Int32, Constant: 1) (Syntax: 'For J = I + ... Next')
            Conversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
            Operand: ILiteralExpression (OperationKind.LiteralExpression, Type: System.Int32, Constant: 1) (Syntax: 'For J = I + ... Next')
        Body: IBlockStatement (1 statements) (OperationKind.BlockStatement) (Syntax: 'For J = I + ... Next')
            IExpressionStatement (OperationKind.ExpressionStatement) (Syntax: 'System.Cons ... riteLine(J)')
              Expression: IInvocationExpression (Sub System.Console.WriteLine(value As System.Int32)) (OperationKind.InvocationExpression, Type: System.Void) (Syntax: 'System.Cons ... riteLine(J)')
                  Instance Receiver: null
                  Arguments(1):
                      IArgument (ArgumentKind.Explicit, Matching Parameter: value) (OperationKind.Argument) (Syntax: 'J')
                        ILocalReferenceExpression: J (OperationKind.LocalReferenceExpression, Type: System.Int32) (Syntax: 'J')
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
IForToLoopStatement (LoopKind.ForTo) (OperationKind.LoopStatement) (Syntax: 'For I = 1 T ... Next')
  Locals: Local_1: I As System.Int32
  LoopControlVariable: ILocalReferenceExpression: I (OperationKind.LocalReferenceExpression, Type: System.Int32) (Syntax: 'I')
  InitialValue: ILiteralExpression (OperationKind.LiteralExpression, Type: System.Int32, Constant: 1) (Syntax: '1')
  LimitValue: ILiteralExpression (OperationKind.LiteralExpression, Type: System.Int32, Constant: 2) (Syntax: '2')
  StepValue: IConversionExpression (Explicit, TryCast: False, Unchecked) (OperationKind.ConversionExpression, Type: System.Int32, Constant: 1) (Syntax: 'For I = 1 T ... Next')
      Conversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
      Operand: ILiteralExpression (OperationKind.LiteralExpression, Type: System.Int32, Constant: 1) (Syntax: 'For I = 1 T ... Next')
  Body: IBlockStatement (2 statements) (OperationKind.BlockStatement) (Syntax: 'For I = 1 T ... Next')
      IForToLoopStatement (LoopKind.ForTo) (OperationKind.LoopStatement) (Syntax: 'For J = 1 T ... Next')
        Locals: Local_1: J As System.Int32
        LoopControlVariable: ILocalReferenceExpression: J (OperationKind.LocalReferenceExpression, Type: System.Int32) (Syntax: 'J')
        InitialValue: ILiteralExpression (OperationKind.LiteralExpression, Type: System.Int32, Constant: 1) (Syntax: '1')
        LimitValue: ILiteralExpression (OperationKind.LiteralExpression, Type: System.Int32, Constant: 2) (Syntax: '2')
        StepValue: IConversionExpression (Explicit, TryCast: False, Unchecked) (OperationKind.ConversionExpression, Type: System.Int32, Constant: 1) (Syntax: 'For J = 1 T ... Next')
            Conversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
            Operand: ILiteralExpression (OperationKind.LiteralExpression, Type: System.Int32, Constant: 1) (Syntax: 'For J = 1 T ... Next')
        Body: IBlockStatement (1 statements) (OperationKind.BlockStatement) (Syntax: 'For J = 1 T ... Next')
            IBranchStatement (BranchKind.Break, Label: exit) (OperationKind.BranchStatement) (Syntax: 'Exit For')
        NextVariables(0)
      IExpressionStatement (OperationKind.ExpressionStatement) (Syntax: 'System.Cons ... riteLine(I)')
        Expression: IInvocationExpression (Sub System.Console.WriteLine(value As System.Int32)) (OperationKind.InvocationExpression, Type: System.Void) (Syntax: 'System.Cons ... riteLine(I)')
            Instance Receiver: null
            Arguments(1):
                IArgument (ArgumentKind.Explicit, Matching Parameter: value) (OperationKind.Argument) (Syntax: 'I')
                  ILocalReferenceExpression: I (OperationKind.LocalReferenceExpression, Type: System.Int32) (Syntax: 'I')
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
IForToLoopStatement (LoopKind.ForTo) (OperationKind.LoopStatement) (Syntax: 'For x As e1 ... Next')
  Locals: Local_1: x As e1
  LoopControlVariable: ILocalReferenceExpression: x (OperationKind.LocalReferenceExpression, Type: e1) (Syntax: 'x As e1')
  InitialValue: IFieldReferenceExpression: e1.a (Static) (OperationKind.FieldReferenceExpression, Type: e1, Constant: 0) (Syntax: 'e1.a')
      Instance Receiver: null
  LimitValue: IFieldReferenceExpression: e1.c (Static) (OperationKind.FieldReferenceExpression, Type: e1, Constant: 2) (Syntax: 'e1.c')
      Instance Receiver: null
  StepValue: IConversionExpression (Explicit, TryCast: False, Unchecked) (OperationKind.ConversionExpression, Type: e1, Constant: 1) (Syntax: 'For x As e1 ... Next')
      Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: True, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
      Operand: ILiteralExpression (OperationKind.LiteralExpression, Type: System.Int32, Constant: 1) (Syntax: 'For x As e1 ... Next')
  Body: IBlockStatement (0 statements) (OperationKind.BlockStatement) (Syntax: 'For x As e1 ... Next')
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
IForToLoopStatement (LoopKind.ForTo) (OperationKind.LoopStatement) (Syntax: 'For i As In ... Next')
  Locals: Local_1: i As System.Int32
  LoopControlVariable: ILocalReferenceExpression: i (OperationKind.LocalReferenceExpression, Type: System.Int32) (Syntax: 'i As Integer')
  InitialValue: IConversionExpression (Implicit, TryCast: False, Unchecked) (OperationKind.ConversionExpression, Type: System.Int32) (Syntax: 'P1(30 + i)')
      Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: True, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
      Operand: IPropertyReferenceExpression: Property MyClass1.P1(x As System.Int64) As System.Byte (OperationKind.PropertyReferenceExpression, Type: System.Byte) (Syntax: 'P1(30 + i)')
          Instance Receiver: IInstanceReferenceExpression (OperationKind.InstanceReferenceExpression, Type: MyClass1) (Syntax: 'P1')
          Arguments(1):
              IArgument (ArgumentKind.Explicit, Matching Parameter: x) (OperationKind.Argument) (Syntax: '30 + i')
                IConversionExpression (Implicit, TryCast: False, Unchecked) (OperationKind.ConversionExpression, Type: System.Int64) (Syntax: '30 + i')
                  Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: True, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                  Operand: IBinaryOperatorExpression (BinaryOperatorKind.Add, Checked) (OperationKind.BinaryOperatorExpression, Type: System.Int32) (Syntax: '30 + i')
                      Left: ILiteralExpression (OperationKind.LiteralExpression, Type: System.Int32, Constant: 30) (Syntax: '30')
                      Right: ILocalReferenceExpression: i (OperationKind.LocalReferenceExpression, Type: System.Int32) (Syntax: 'i')
                InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
  LimitValue: ILiteralExpression (OperationKind.LiteralExpression, Type: System.Int32, Constant: 30) (Syntax: '30')
  StepValue: IConversionExpression (Explicit, TryCast: False, Unchecked) (OperationKind.ConversionExpression, Type: System.Int32, Constant: 1) (Syntax: 'For i As In ... Next')
      Conversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
      Operand: ILiteralExpression (OperationKind.LiteralExpression, Type: System.Int32, Constant: 1) (Syntax: 'For i As In ... Next')
  Body: IBlockStatement (0 statements) (OperationKind.BlockStatement) (Syntax: 'For i As In ... Next')
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
IForToLoopStatement (LoopKind.ForTo) (OperationKind.LoopStatement) (Syntax: 'For global_ ... Next')
  Locals: Local_1: global_x As System.Int32
  LoopControlVariable: ILocalReferenceExpression: global_x (OperationKind.LocalReferenceExpression, Type: System.Int32) (Syntax: 'global_x As Integer')
  InitialValue: IConversionExpression (Implicit, TryCast: False, Unchecked) (OperationKind.ConversionExpression, Type: System.Int32, Constant: 20) (Syntax: 'global_y')
      Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: True, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
      Operand: IFieldReferenceExpression: MyClass1.global_y As System.Int64 (Static) (OperationKind.FieldReferenceExpression, Type: System.Int64, Constant: 20) (Syntax: 'global_y')
          Instance Receiver: null
  LimitValue: ILiteralExpression (OperationKind.LiteralExpression, Type: System.Int32, Constant: 10) (Syntax: '10')
  StepValue: IConversionExpression (Explicit, TryCast: False, Unchecked) (OperationKind.ConversionExpression, Type: System.Int32, Constant: 1) (Syntax: 'For global_ ... Next')
      Conversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
      Operand: ILiteralExpression (OperationKind.LiteralExpression, Type: System.Int32, Constant: 1) (Syntax: 'For global_ ... Next')
  Body: IBlockStatement (0 statements) (OperationKind.BlockStatement) (Syntax: 'For global_ ... Next')
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
IForToLoopStatement (LoopKind.ForTo) (OperationKind.LoopStatement) (Syntax: 'For x As In ... o 10 : Next')
  Locals: Local_1: x As System.Int32
  LoopControlVariable: ILocalReferenceExpression: x (OperationKind.LocalReferenceExpression, Type: System.Int32) (Syntax: 'x As Integer')
  InitialValue: ILiteralExpression (OperationKind.LiteralExpression, Type: System.Int32, Constant: 0) (Syntax: '0')
  LimitValue: ILiteralExpression (OperationKind.LiteralExpression, Type: System.Int32, Constant: 10) (Syntax: '10')
  StepValue: IConversionExpression (Explicit, TryCast: False, Unchecked) (OperationKind.ConversionExpression, Type: System.Int32, Constant: 1) (Syntax: 'For x As In ... o 10 : Next')
      Conversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
      Operand: ILiteralExpression (OperationKind.LiteralExpression, Type: System.Int32, Constant: 1) (Syntax: 'For x As In ... o 10 : Next')
  Body: IBlockStatement (0 statements) (OperationKind.BlockStatement) (Syntax: 'For x As In ... o 10 : Next')
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
IForToLoopStatement (LoopKind.ForTo) (OperationKind.LoopStatement) (Syntax: 'For Y = 1 T ... Next')
  LoopControlVariable: ILocalReferenceExpression: Y (OperationKind.LocalReferenceExpression, Type: System.Int32) (Syntax: 'Y')
  InitialValue: ILiteralExpression (OperationKind.LiteralExpression, Type: System.Int32, Constant: 1) (Syntax: '1')
  LimitValue: ILiteralExpression (OperationKind.LiteralExpression, Type: System.Int32, Constant: 2) (Syntax: '2')
  StepValue: IConversionExpression (Explicit, TryCast: False, Unchecked) (OperationKind.ConversionExpression, Type: System.Int32, Constant: 1) (Syntax: 'For Y = 1 T ... Next')
      Conversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
      Operand: ILiteralExpression (OperationKind.LiteralExpression, Type: System.Int32, Constant: 1) (Syntax: 'For Y = 1 T ... Next')
  Body: IBlockStatement (0 statements) (OperationKind.BlockStatement) (Syntax: 'For Y = 1 T ... Next')
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
IForToLoopStatement (LoopKind.ForTo) (OperationKind.LoopStatement) (Syntax: 'For element ... Next')
  Locals: Local_1: element1 As System.Int32
  LoopControlVariable: ILocalReferenceExpression: element1 (OperationKind.LocalReferenceExpression, Type: System.Int32) (Syntax: 'element1')
  InitialValue: ILiteralExpression (OperationKind.LiteralExpression, Type: System.Int32, Constant: 23) (Syntax: '23')
  LimitValue: ILiteralExpression (OperationKind.LiteralExpression, Type: System.Int32, Constant: 42) (Syntax: '42')
  StepValue: IConversionExpression (Explicit, TryCast: False, Unchecked) (OperationKind.ConversionExpression, Type: System.Int32, Constant: 1) (Syntax: 'For element ... Next')
      Conversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
      Operand: ILiteralExpression (OperationKind.LiteralExpression, Type: System.Int32, Constant: 1) (Syntax: 'For element ... Next')
  Body: IBlockStatement (0 statements) (OperationKind.BlockStatement) (Syntax: 'For element ... Next')
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
IForToLoopStatement (LoopKind.ForTo) (OperationKind.LoopStatement) (Syntax: 'For i As In ... Next')
  Locals: Local_1: i As System.Int32
  LoopControlVariable: ILocalReferenceExpression: i (OperationKind.LocalReferenceExpression, Type: System.Int32) (Syntax: 'i As Integer')
  InitialValue: ILiteralExpression (OperationKind.LiteralExpression, Type: System.Int32, Constant: 0) (Syntax: '0')
  LimitValue: ILiteralExpression (OperationKind.LiteralExpression, Type: System.Int32, Constant: 5) (Syntax: '5')
  StepValue: IConversionExpression (Explicit, TryCast: False, Unchecked) (OperationKind.ConversionExpression, Type: System.Int32, Constant: 1) (Syntax: 'For i As In ... Next')
      Conversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
      Operand: ILiteralExpression (OperationKind.LiteralExpression, Type: System.Int32, Constant: 1) (Syntax: 'For i As In ... Next')
  Body: IBlockStatement (1 statements) (OperationKind.BlockStatement) (Syntax: 'For i As In ... Next')
      IIfStatement (OperationKind.IfStatement) (Syntax: 'If i Mod 2  ... End If')
        Condition: IBinaryOperatorExpression (BinaryOperatorKind.Equals, Checked) (OperationKind.BinaryOperatorExpression, Type: System.Boolean) (Syntax: 'i Mod 2 = 0')
            Left: IBinaryOperatorExpression (BinaryOperatorKind.Remainder, Checked) (OperationKind.BinaryOperatorExpression, Type: System.Int32) (Syntax: 'i Mod 2')
                Left: ILocalReferenceExpression: i (OperationKind.LocalReferenceExpression, Type: System.Int32) (Syntax: 'i')
                Right: ILiteralExpression (OperationKind.LiteralExpression, Type: System.Int32, Constant: 2) (Syntax: '2')
            Right: ILiteralExpression (OperationKind.LiteralExpression, Type: System.Int32, Constant: 0) (Syntax: '0')
        IfTrue: IBlockStatement (1 statements) (OperationKind.BlockStatement) (Syntax: 'If i Mod 2  ... End If')
            IBranchStatement (BranchKind.Continue, Label: continue) (OperationKind.BranchStatement) (Syntax: 'Continue For')
        IfFalse: null
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
IForToLoopStatement (LoopKind.ForTo) (OperationKind.LoopStatement) (Syntax: 'For X = 10  ... Next')
  LoopControlVariable: IFieldReferenceExpression: M.X As System.Int32 (Static) (OperationKind.FieldReferenceExpression, Type: System.Int32) (Syntax: 'X')
      Instance Receiver: null
  InitialValue: ILiteralExpression (OperationKind.LiteralExpression, Type: System.Int32, Constant: 10) (Syntax: '10')
  LimitValue: ILiteralExpression (OperationKind.LiteralExpression, Type: System.Int32, Constant: 0) (Syntax: '0')
  StepValue: IConversionExpression (Explicit, TryCast: False, Unchecked) (OperationKind.ConversionExpression, Type: System.Int32, Constant: 1) (Syntax: 'For X = 10  ... Next')
      Conversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
      Operand: ILiteralExpression (OperationKind.LiteralExpression, Type: System.Int32, Constant: 1) (Syntax: 'For X = 10  ... Next')
  Body: IBlockStatement (0 statements) (OperationKind.BlockStatement) (Syntax: 'For X = 10  ... Next')
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
IForToLoopStatement (LoopKind.ForTo) (OperationKind.LoopStatement, IsInvalid) (Syntax: 'For X = 10  ... Next')
  LoopControlVariable: IFieldReferenceExpression: M.X As System.String (Static) (OperationKind.FieldReferenceExpression, Type: System.String, IsInvalid) (Syntax: 'X')
      Instance Receiver: null
  InitialValue: ILiteralExpression (OperationKind.LiteralExpression, Type: System.Int32, Constant: 10) (Syntax: '10')
  LimitValue: ILiteralExpression (OperationKind.LiteralExpression, Type: System.Int32, Constant: 0) (Syntax: '0')
  StepValue: ILiteralExpression (OperationKind.LiteralExpression, Type: System.Int32, Constant: 1, IsInvalid) (Syntax: 'For X = 10  ... Next')
  Body: IBlockStatement (0 statements) (OperationKind.BlockStatement, IsInvalid) (Syntax: 'For X = 10  ... Next')
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
IForToLoopStatement (LoopKind.ForTo) (OperationKind.LoopStatement) (Syntax: 'For A = 1 T ... Next B, A')
  Locals: Local_1: A As System.Int32
  LoopControlVariable: ILocalReferenceExpression: A (OperationKind.LocalReferenceExpression, Type: System.Int32) (Syntax: 'A')
  InitialValue: ILiteralExpression (OperationKind.LiteralExpression, Type: System.Int32, Constant: 1) (Syntax: '1')
  LimitValue: ILiteralExpression (OperationKind.LiteralExpression, Type: System.Int32, Constant: 2) (Syntax: '2')
  StepValue: IConversionExpression (Explicit, TryCast: False, Unchecked) (OperationKind.ConversionExpression, Type: System.Int32, Constant: 1) (Syntax: 'For A = 1 T ... Next B, A')
      Conversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
      Operand: ILiteralExpression (OperationKind.LiteralExpression, Type: System.Int32, Constant: 1) (Syntax: 'For A = 1 T ... Next B, A')
  Body: IBlockStatement (1 statements) (OperationKind.BlockStatement) (Syntax: 'For A = 1 T ... Next B, A')
      IForToLoopStatement (LoopKind.ForTo) (OperationKind.LoopStatement) (Syntax: 'For B = A T ... Next B, A')
        Locals: Local_1: B As System.Int32
        LoopControlVariable: ILocalReferenceExpression: B (OperationKind.LocalReferenceExpression, Type: System.Int32) (Syntax: 'B')
        InitialValue: ILocalReferenceExpression: A (OperationKind.LocalReferenceExpression, Type: System.Int32) (Syntax: 'A')
        LimitValue: ILiteralExpression (OperationKind.LiteralExpression, Type: System.Int32, Constant: 2) (Syntax: '2')
        StepValue: IConversionExpression (Explicit, TryCast: False, Unchecked) (OperationKind.ConversionExpression, Type: System.Int32, Constant: 1) (Syntax: 'For B = A T ... Next B, A')
            Conversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
            Operand: ILiteralExpression (OperationKind.LiteralExpression, Type: System.Int32, Constant: 1) (Syntax: 'For B = A T ... Next B, A')
        Body: IBlockStatement (0 statements) (OperationKind.BlockStatement) (Syntax: 'For B = A T ... Next B, A')
        NextVariables(2):
            ILocalReferenceExpression: B (OperationKind.LocalReferenceExpression, Type: System.Int32) (Syntax: 'B')
            ILocalReferenceExpression: A (OperationKind.LocalReferenceExpression, Type: System.Int32) (Syntax: 'A')
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
IForToLoopStatement (LoopKind.ForTo) (OperationKind.LoopStatement) (Syntax: 'For x = 12  ... Next')
  LoopControlVariable: ILocalReferenceExpression: x (OperationKind.LocalReferenceExpression, Type: System.Int32) (Syntax: 'x')
  InitialValue: ILiteralExpression (OperationKind.LiteralExpression, Type: System.Int32, Constant: 12) (Syntax: '12')
  LimitValue: ILocalReferenceExpression: y (OperationKind.LocalReferenceExpression, Type: System.Int32) (Syntax: 'y')
  StepValue: IConversionExpression (Explicit, TryCast: False, Unchecked) (OperationKind.ConversionExpression, Type: System.Int32, Constant: 1) (Syntax: 'For x = 12  ... Next')
      Conversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
      Operand: ILiteralExpression (OperationKind.LiteralExpression, Type: System.Int32, Constant: 1) (Syntax: 'For x = 12  ... Next')
  Body: IBlockStatement (0 statements) (OperationKind.BlockStatement) (Syntax: 'For x = 12  ... Next')
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
IForToLoopStatement (LoopKind.ForTo) (OperationKind.LoopStatement) (Syntax: 'For x = 12  ... Next')
  LoopControlVariable: ILocalReferenceExpression: x (OperationKind.LocalReferenceExpression, Type: System.Nullable(Of System.Int32)) (Syntax: 'x')
  InitialValue: IConversionExpression (Implicit, TryCast: False, Unchecked) (OperationKind.ConversionExpression, Type: System.Nullable(Of System.Int32)) (Syntax: '12')
      Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
      Operand: ILiteralExpression (OperationKind.LiteralExpression, Type: System.Int32, Constant: 12) (Syntax: '12')
  LimitValue: IConversionExpression (Implicit, TryCast: False, Unchecked) (OperationKind.ConversionExpression, Type: System.Nullable(Of System.Int32)) (Syntax: 'y')
      Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
      Operand: ILocalReferenceExpression: y (OperationKind.LocalReferenceExpression, Type: System.Int32) (Syntax: 'y')
  StepValue: IConversionExpression (Explicit, TryCast: False, Unchecked) (OperationKind.ConversionExpression, Type: System.Nullable(Of System.Int32)) (Syntax: 'For x = 12  ... Next')
      Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
      Operand: ILiteralExpression (OperationKind.LiteralExpression, Type: System.Int32, Constant: 1) (Syntax: 'For x = 12  ... Next')
  Body: IBlockStatement (0 statements) (OperationKind.BlockStatement) (Syntax: 'For x = 12  ... Next')
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
IForToLoopStatement (LoopKind.ForTo) (OperationKind.LoopStatement) (Syntax: 'For x = 12  ... Next')
  LoopControlVariable: ILocalReferenceExpression: x (OperationKind.LocalReferenceExpression, Type: System.Int32) (Syntax: 'x')
  InitialValue: ILiteralExpression (OperationKind.LiteralExpression, Type: System.Int32, Constant: 12) (Syntax: '12')
  LimitValue: IConversionExpression (Implicit, TryCast: False, Unchecked) (OperationKind.ConversionExpression, Type: System.Int32) (Syntax: 'y')
      Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
      Operand: ILocalReferenceExpression: y (OperationKind.LocalReferenceExpression, Type: System.Nullable(Of System.Int32)) (Syntax: 'y')
  StepValue: IConversionExpression (Explicit, TryCast: False, Unchecked) (OperationKind.ConversionExpression, Type: System.Int32, Constant: 1) (Syntax: 'For x = 12  ... Next')
      Conversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
      Operand: ILiteralExpression (OperationKind.LiteralExpression, Type: System.Int32, Constant: 1) (Syntax: 'For x = 12  ... Next')
  Body: IBlockStatement (0 statements) (OperationKind.BlockStatement) (Syntax: 'For x = 12  ... Next')
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
IForToLoopStatement (LoopKind.ForTo) (OperationKind.LoopStatement) (Syntax: 'For x = 12  ... Next')
  LoopControlVariable: ILocalReferenceExpression: x (OperationKind.LocalReferenceExpression, Type: System.Nullable(Of System.Int32)) (Syntax: 'x')
  InitialValue: IConversionExpression (Implicit, TryCast: False, Unchecked) (OperationKind.ConversionExpression, Type: System.Nullable(Of System.Int32)) (Syntax: '12')
      Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
      Operand: ILiteralExpression (OperationKind.LiteralExpression, Type: System.Int32, Constant: 12) (Syntax: '12')
  LimitValue: ILocalReferenceExpression: y (OperationKind.LocalReferenceExpression, Type: System.Nullable(Of System.Int32)) (Syntax: 'y')
  StepValue: IConversionExpression (Explicit, TryCast: False, Unchecked) (OperationKind.ConversionExpression, Type: System.Nullable(Of System.Int32)) (Syntax: 'For x = 12  ... Next')
      Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
      Operand: ILiteralExpression (OperationKind.LiteralExpression, Type: System.Int32, Constant: 1) (Syntax: 'For x = 12  ... Next')
  Body: IBlockStatement (0 statements) (OperationKind.BlockStatement) (Syntax: 'For x = 12  ... Next')
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
IForToLoopStatement (LoopKind.ForTo) (OperationKind.LoopStatement) (Syntax: 'For x = 12  ... Next')
  LoopControlVariable: ILocalReferenceExpression: x (OperationKind.LocalReferenceExpression, Type: System.Int32) (Syntax: 'x')
  InitialValue: ILiteralExpression (OperationKind.LiteralExpression, Type: System.Int32, Constant: 12) (Syntax: '12')
  LimitValue: ILocalReferenceExpression: y (OperationKind.LocalReferenceExpression, Type: System.Int32) (Syntax: 'y')
  StepValue: IConversionExpression (Implicit, TryCast: False, Unchecked) (OperationKind.ConversionExpression, Type: System.Int32) (Syntax: 's')
      Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
      Operand: ILocalReferenceExpression: s (OperationKind.LocalReferenceExpression, Type: System.Nullable(Of System.Int32)) (Syntax: 's')
  Body: IBlockStatement (0 statements) (OperationKind.BlockStatement) (Syntax: 'For x = 12  ... Next')
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
IForToLoopStatement (LoopKind.ForTo) (OperationKind.LoopStatement) (Syntax: 'For x = 12  ... Next')
  LoopControlVariable: ILocalReferenceExpression: x (OperationKind.LocalReferenceExpression, Type: System.Nullable(Of System.Int32)) (Syntax: 'x')
  InitialValue: IConversionExpression (Implicit, TryCast: False, Unchecked) (OperationKind.ConversionExpression, Type: System.Nullable(Of System.Int32)) (Syntax: '12')
      Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
      Operand: ILiteralExpression (OperationKind.LiteralExpression, Type: System.Int32, Constant: 12) (Syntax: '12')
  LimitValue: IConversionExpression (Implicit, TryCast: False, Unchecked) (OperationKind.ConversionExpression, Type: System.Nullable(Of System.Int32)) (Syntax: 'y')
      Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
      Operand: ILocalReferenceExpression: y (OperationKind.LocalReferenceExpression, Type: System.Int32) (Syntax: 'y')
  StepValue: ILocalReferenceExpression: s (OperationKind.LocalReferenceExpression, Type: System.Nullable(Of System.Int32)) (Syntax: 's')
  Body: IBlockStatement (0 statements) (OperationKind.BlockStatement) (Syntax: 'For x = 12  ... Next')
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
IForToLoopStatement (LoopKind.ForTo) (OperationKind.LoopStatement) (Syntax: 'For x = 12  ... Next')
  LoopControlVariable: ILocalReferenceExpression: x (OperationKind.LocalReferenceExpression, Type: System.Int32) (Syntax: 'x')
  InitialValue: ILiteralExpression (OperationKind.LiteralExpression, Type: System.Int32, Constant: 12) (Syntax: '12')
  LimitValue: IConversionExpression (Implicit, TryCast: False, Unchecked) (OperationKind.ConversionExpression, Type: System.Int32) (Syntax: 'y')
      Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
      Operand: ILocalReferenceExpression: y (OperationKind.LocalReferenceExpression, Type: System.Nullable(Of System.Int32)) (Syntax: 'y')
  StepValue: IConversionExpression (Implicit, TryCast: False, Unchecked) (OperationKind.ConversionExpression, Type: System.Int32) (Syntax: 's')
      Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
      Operand: ILocalReferenceExpression: s (OperationKind.LocalReferenceExpression, Type: System.Nullable(Of System.Int32)) (Syntax: 's')
  Body: IBlockStatement (0 statements) (OperationKind.BlockStatement) (Syntax: 'For x = 12  ... Next')
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
IForToLoopStatement (LoopKind.ForTo) (OperationKind.LoopStatement) (Syntax: 'For x = 12  ... Next')
  LoopControlVariable: ILocalReferenceExpression: x (OperationKind.LocalReferenceExpression, Type: System.Nullable(Of System.Int32)) (Syntax: 'x')
  InitialValue: IConversionExpression (Implicit, TryCast: False, Unchecked) (OperationKind.ConversionExpression, Type: System.Nullable(Of System.Int32)) (Syntax: '12')
      Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
      Operand: ILiteralExpression (OperationKind.LiteralExpression, Type: System.Int32, Constant: 12) (Syntax: '12')
  LimitValue: ILocalReferenceExpression: y (OperationKind.LocalReferenceExpression, Type: System.Nullable(Of System.Int32)) (Syntax: 'y')
  StepValue: ILocalReferenceExpression: s (OperationKind.LocalReferenceExpression, Type: System.Nullable(Of System.Int32)) (Syntax: 's')
  Body: IBlockStatement (0 statements) (OperationKind.BlockStatement) (Syntax: 'For x = 12  ... Next')
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
IForToLoopStatement (LoopKind.ForTo) (OperationKind.LoopStatement) (Syntax: 'For X = 0 T ... Next X')
  LoopControlVariable: IFieldReferenceExpression: C.X As System.Int32 (OperationKind.FieldReferenceExpression, Type: System.Int32) (Syntax: 'X')
      Instance Receiver: IInstanceReferenceExpression (OperationKind.InstanceReferenceExpression, Type: C) (Syntax: 'X')
  InitialValue: ILiteralExpression (OperationKind.LiteralExpression, Type: System.Int32, Constant: 0) (Syntax: '0')
  LimitValue: ILiteralExpression (OperationKind.LiteralExpression, Type: System.Int32, Constant: 10) (Syntax: '10')
  StepValue: IConversionExpression (Explicit, TryCast: False, Unchecked) (OperationKind.ConversionExpression, Type: System.Int32, Constant: 1) (Syntax: 'For X = 0 T ... Next X')
      Conversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
      Operand: ILiteralExpression (OperationKind.LiteralExpression, Type: System.Int32, Constant: 1) (Syntax: 'For X = 0 T ... Next X')
  Body: IBlockStatement (0 statements) (OperationKind.BlockStatement) (Syntax: 'For X = 0 T ... Next X')
  NextVariables(1):
      IFieldReferenceExpression: C.X As System.Int32 (OperationKind.FieldReferenceExpression, Type: System.Int32) (Syntax: 'X')
        Instance Receiver: IInstanceReferenceExpression (OperationKind.InstanceReferenceExpression, Type: C) (Syntax: 'X')
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
IForToLoopStatement (LoopKind.ForTo) (OperationKind.LoopStatement) (Syntax: 'For c.X = 0 ... Next c.X')
  LoopControlVariable: IFieldReferenceExpression: C.X As System.Int32 (OperationKind.FieldReferenceExpression, Type: System.Int32) (Syntax: 'c.X')
      Instance Receiver: IParameterReferenceExpression: c (OperationKind.ParameterReferenceExpression, Type: C) (Syntax: 'c')
  InitialValue: ILiteralExpression (OperationKind.LiteralExpression, Type: System.Int32, Constant: 0) (Syntax: '0')
  LimitValue: ILiteralExpression (OperationKind.LiteralExpression, Type: System.Int32, Constant: 10) (Syntax: '10')
  StepValue: IConversionExpression (Explicit, TryCast: False, Unchecked) (OperationKind.ConversionExpression, Type: System.Int32, Constant: 1) (Syntax: 'For c.X = 0 ... Next c.X')
      Conversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
      Operand: ILiteralExpression (OperationKind.LiteralExpression, Type: System.Int32, Constant: 1) (Syntax: 'For c.X = 0 ... Next c.X')
  Body: IBlockStatement (0 statements) (OperationKind.BlockStatement) (Syntax: 'For c.X = 0 ... Next c.X')
  NextVariables(1):
      IFieldReferenceExpression: C.X As System.Int32 (OperationKind.FieldReferenceExpression, Type: System.Int32) (Syntax: 'c.X')
        Instance Receiver: IParameterReferenceExpression: c (OperationKind.ParameterReferenceExpression, Type: C) (Syntax: 'c')
]]>.Value

            Dim expectedDiagnostics = String.Empty

            VerifyOperationTreeAndDiagnosticsForTest(Of ForBlockSyntax)(source, expectedOperationTree, expectedDiagnostics)
        End Sub
    End Class
End Namespace
