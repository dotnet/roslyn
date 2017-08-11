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
IForLoopStatement (LoopKind.For) (OperationKind.LoopStatement) (Syntax: 'For i As In ... Next')
  Condition: IBinaryOperatorExpression (BinaryOperationKind.IntegerLessThanOrEqual) (OperationKind.BinaryOperatorExpression, Type: System.Boolean) (Syntax: 'myarray.Length - 1')
      Left: ILocalReferenceExpression: i (OperationKind.LocalReferenceExpression, Type: System.Int32) (Syntax: 'i As Integer')
      Right: ISyntheticLocalReferenceExpression (SynthesizedLocalKind.ForLoopLimitValue) (OperationKind.SyntheticLocalReferenceExpression, Type: System.Int32) (Syntax: 'myarray.Length - 1')
  Before:
      IExpressionStatement (OperationKind.ExpressionStatement) (Syntax: '0')
        Expression: ISimpleAssignmentExpression (OperationKind.SimpleAssignmentExpression, Type: System.Int32) (Syntax: '0')
            Left: ILocalReferenceExpression: i (OperationKind.LocalReferenceExpression, Type: System.Int32) (Syntax: 'i As Integer')
            Right: ILiteralExpression (Text: 0) (OperationKind.LiteralExpression, Type: System.Int32, Constant: 0) (Syntax: '0')
      IExpressionStatement (OperationKind.ExpressionStatement) (Syntax: 'myarray.Length - 1')
        Expression: ISimpleAssignmentExpression (OperationKind.SimpleAssignmentExpression, Type: System.Int32) (Syntax: 'myarray.Length - 1')
            Left: ISyntheticLocalReferenceExpression (SynthesizedLocalKind.ForLoopLimitValue) (OperationKind.SyntheticLocalReferenceExpression, Type: System.Int32) (Syntax: 'myarray.Length - 1')
            Right: IBinaryOperatorExpression (BinaryOperationKind.IntegerSubtract) (OperationKind.BinaryOperatorExpression, Type: System.Int32) (Syntax: 'myarray.Length - 1')
                Left: IPropertyReferenceExpression: ReadOnly Property System.Array.Length As System.Int32 (OperationKind.PropertyReferenceExpression, Type: System.Int32) (Syntax: 'myarray.Length')
                    Instance Receiver: ILocalReferenceExpression: myarray (OperationKind.LocalReferenceExpression, Type: System.Int32()) (Syntax: 'myarray')
                Right: ILiteralExpression (Text: 1) (OperationKind.LiteralExpression, Type: System.Int32, Constant: 1) (Syntax: '1')
  AtLoopBottom:
      IExpressionStatement (OperationKind.ExpressionStatement) (Syntax: 'For i As In ... Next')
        Expression: ICompoundAssignmentExpression (BinaryOperationKind.IntegerAdd) (OperationKind.CompoundAssignmentExpression, Type: System.Int32) (Syntax: 'For i As In ... Next')
            Left: ILocalReferenceExpression: i (OperationKind.LocalReferenceExpression, Type: System.Int32) (Syntax: 'i As Integer')
            Right: IConversionExpression (Explicit, TryCast: False, Unchecked) (OperationKind.ConversionExpression, Type: System.Int32, Constant: 1) (Syntax: 'For i As In ... Next')
                Conversion: CommonConversion (Exists: False, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                Operand: ILiteralExpression (OperationKind.LiteralExpression, Type: System.Int32, Constant: 1) (Syntax: 'For i As In ... Next')
  Body: IBlockStatement (1 statements) (OperationKind.BlockStatement) (Syntax: 'For i As In ... Next')
      IExpressionStatement (OperationKind.ExpressionStatement) (Syntax: 'System.Cons ... myarray(i))')
        Expression: IInvocationExpression (Sub System.Console.WriteLine(value As System.Int32)) (OperationKind.InvocationExpression, Type: System.Void) (Syntax: 'System.Cons ... myarray(i))')
            Instance Receiver: null
            Arguments(1):
                IArgument (ArgumentKind.Explicit, Matching Parameter: value) (OperationKind.Argument) (Syntax: 'myarray(i)')
                  IArrayElementReferenceExpression (OperationKind.ArrayElementReferenceExpression, Type: System.Int32) (Syntax: 'myarray(i)')
                    Array reference: ILocalReferenceExpression: myarray (OperationKind.LocalReferenceExpression, Type: System.Int32()) (Syntax: 'myarray')
                    Indices(1):
                        ILocalReferenceExpression: i (OperationKind.LocalReferenceExpression, Type: System.Int32) (Syntax: 'i')
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
IForLoopStatement (LoopKind.For) (OperationKind.LoopStatement) (Syntax: 'For i As In ... Next')
  Condition: IConditionalChoiceExpression (OperationKind.ConditionalChoiceExpression, Type: System.Boolean) (Syntax: '"1"')
      Condition: IBinaryOperatorExpression (BinaryOperationKind.IntegerGreaterThanOrEqual) (OperationKind.BinaryOperatorExpression, Type: System.Boolean) (Syntax: 's')
          Left: ISyntheticLocalReferenceExpression (SynthesizedLocalKind.ForLoopStepValue) (OperationKind.SyntheticLocalReferenceExpression, Type: System.Int32) (Syntax: 's')
          Right: ILiteralExpression (Text: 0) (OperationKind.LiteralExpression, Type: System.Int32, Constant: 0) (Syntax: 's')
      IfTrue: IBinaryOperatorExpression (BinaryOperationKind.IntegerLessThanOrEqual) (OperationKind.BinaryOperatorExpression, Type: System.Boolean) (Syntax: '"1"')
          Left: ILocalReferenceExpression: i (OperationKind.LocalReferenceExpression, Type: System.Int32) (Syntax: 'i As Integer')
          Right: ISyntheticLocalReferenceExpression (SynthesizedLocalKind.ForLoopLimitValue) (OperationKind.SyntheticLocalReferenceExpression, Type: System.Int32) (Syntax: '"1"')
      IfFalse: IBinaryOperatorExpression (BinaryOperationKind.IntegerGreaterThanOrEqual) (OperationKind.BinaryOperatorExpression, Type: System.Boolean) (Syntax: '"1"')
          Left: ILocalReferenceExpression: i (OperationKind.LocalReferenceExpression, Type: System.Int32) (Syntax: 'i As Integer')
          Right: ISyntheticLocalReferenceExpression (SynthesizedLocalKind.ForLoopLimitValue) (OperationKind.SyntheticLocalReferenceExpression, Type: System.Int32) (Syntax: '"1"')
  Before:
      IExpressionStatement (OperationKind.ExpressionStatement) (Syntax: '0')
        Expression: ISimpleAssignmentExpression (OperationKind.SimpleAssignmentExpression, Type: System.Int32) (Syntax: '0')
            Left: ILocalReferenceExpression: i (OperationKind.LocalReferenceExpression, Type: System.Int32) (Syntax: 'i As Integer')
            Right: ILiteralExpression (Text: 0) (OperationKind.LiteralExpression, Type: System.Int32, Constant: 0) (Syntax: '0')
      IExpressionStatement (OperationKind.ExpressionStatement) (Syntax: '"1"')
        Expression: ISimpleAssignmentExpression (OperationKind.SimpleAssignmentExpression, Type: System.Int32) (Syntax: '"1"')
            Left: ISyntheticLocalReferenceExpression (SynthesizedLocalKind.ForLoopLimitValue) (OperationKind.SyntheticLocalReferenceExpression, Type: System.Int32) (Syntax: '"1"')
            Right: IConversionExpression (Implicit, TryCast: False, Unchecked) (OperationKind.ConversionExpression, Type: System.Int32) (Syntax: '"1"')
                Conversion: CommonConversion (Exists: False, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                Operand: ILiteralExpression (OperationKind.LiteralExpression, Type: System.String, Constant: "1") (Syntax: '"1"')
      IExpressionStatement (OperationKind.ExpressionStatement) (Syntax: 's')
        Expression: ISimpleAssignmentExpression (OperationKind.SimpleAssignmentExpression, Type: System.Int32) (Syntax: 's')
            Left: ISyntheticLocalReferenceExpression (SynthesizedLocalKind.ForLoopStepValue) (OperationKind.SyntheticLocalReferenceExpression, Type: System.Int32) (Syntax: 's')
            Right: IConversionExpression (Implicit, TryCast: False, Unchecked) (OperationKind.ConversionExpression, Type: System.Int32) (Syntax: 's')
                Conversion: CommonConversion (Exists: False, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                Operand: ILocalReferenceExpression: s (OperationKind.LocalReferenceExpression, Type: System.Double) (Syntax: 's')
  AtLoopBottom:
      IExpressionStatement (OperationKind.ExpressionStatement) (Syntax: 's')
        Expression: ICompoundAssignmentExpression (BinaryOperationKind.IntegerAdd) (OperationKind.CompoundAssignmentExpression, Type: System.Int32) (Syntax: 's')
            Left: ILocalReferenceExpression: i (OperationKind.LocalReferenceExpression, Type: System.Int32) (Syntax: 'i As Integer')
            Right: ISyntheticLocalReferenceExpression (SynthesizedLocalKind.ForLoopStepValue) (OperationKind.SyntheticLocalReferenceExpression, Type: System.Int32) (Syntax: 's')
  Body: IBlockStatement (1 statements) (OperationKind.BlockStatement) (Syntax: 'For i As In ... Next')
      IExpressionStatement (OperationKind.ExpressionStatement) (Syntax: 'System.Cons ... myarray(i))')
        Expression: IInvocationExpression (Sub System.Console.WriteLine(value As System.Int32)) (OperationKind.InvocationExpression, Type: System.Void) (Syntax: 'System.Cons ... myarray(i))')
            Instance Receiver: null
            Arguments(1):
                IArgument (ArgumentKind.Explicit, Matching Parameter: value) (OperationKind.Argument) (Syntax: 'myarray(i)')
                  IArrayElementReferenceExpression (OperationKind.ArrayElementReferenceExpression, Type: System.Int32) (Syntax: 'myarray(i)')
                    Array reference: ILocalReferenceExpression: myarray (OperationKind.LocalReferenceExpression, Type: System.Int32()) (Syntax: 'myarray')
                    Indices(1):
                        ILocalReferenceExpression: i (OperationKind.LocalReferenceExpression, Type: System.Int32) (Syntax: 'i')
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
IForLoopStatement (LoopKind.For) (OperationKind.LoopStatement) (Syntax: 'For i As Do ... Next')
  Condition: IConditionalChoiceExpression (OperationKind.ConditionalChoiceExpression, Type: System.Boolean) (Syntax: '0')
      Condition: IBinaryOperatorExpression (BinaryOperationKind.FloatingGreaterThanOrEqual) (OperationKind.BinaryOperatorExpression, Type: System.Boolean) (Syntax: 's')
          Left: ISyntheticLocalReferenceExpression (SynthesizedLocalKind.ForLoopStepValue) (OperationKind.SyntheticLocalReferenceExpression, Type: System.Double) (Syntax: 's')
          Right: ILiteralExpression (Text: 0) (OperationKind.LiteralExpression, Type: System.Double, Constant: 0) (Syntax: 's')
      IfTrue: IBinaryOperatorExpression (BinaryOperationKind.FloatingLessThanOrEqual) (OperationKind.BinaryOperatorExpression, Type: System.Boolean) (Syntax: '0')
          Left: ILocalReferenceExpression: i (OperationKind.LocalReferenceExpression, Type: System.Double) (Syntax: 'i As Double')
          Right: IConversionExpression (Implicit, TryCast: False, Unchecked) (OperationKind.ConversionExpression, Type: System.Double, Constant: 0) (Syntax: '0')
              Conversion: CommonConversion (Exists: False, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
              Operand: ILiteralExpression (Text: 0) (OperationKind.LiteralExpression, Type: System.Int32, Constant: 0) (Syntax: '0')
      IfFalse: IBinaryOperatorExpression (BinaryOperationKind.FloatingGreaterThanOrEqual) (OperationKind.BinaryOperatorExpression, Type: System.Boolean) (Syntax: '0')
          Left: ILocalReferenceExpression: i (OperationKind.LocalReferenceExpression, Type: System.Double) (Syntax: 'i As Double')
          Right: IConversionExpression (Implicit, TryCast: False, Unchecked) (OperationKind.ConversionExpression, Type: System.Double, Constant: 0) (Syntax: '0')
              Conversion: CommonConversion (Exists: False, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
              Operand: ILiteralExpression (Text: 0) (OperationKind.LiteralExpression, Type: System.Int32, Constant: 0) (Syntax: '0')
  Before:
      IExpressionStatement (OperationKind.ExpressionStatement) (Syntax: '2')
        Expression: ISimpleAssignmentExpression (OperationKind.SimpleAssignmentExpression, Type: System.Double) (Syntax: '2')
            Left: ILocalReferenceExpression: i (OperationKind.LocalReferenceExpression, Type: System.Double) (Syntax: 'i As Double')
            Right: IConversionExpression (Implicit, TryCast: False, Unchecked) (OperationKind.ConversionExpression, Type: System.Double, Constant: 2) (Syntax: '2')
                Conversion: CommonConversion (Exists: False, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                Operand: ILiteralExpression (Text: 2) (OperationKind.LiteralExpression, Type: System.Int32, Constant: 2) (Syntax: '2')
      IExpressionStatement (OperationKind.ExpressionStatement) (Syntax: 's')
        Expression: ISimpleAssignmentExpression (OperationKind.SimpleAssignmentExpression, Type: System.Double) (Syntax: 's')
            Left: ISyntheticLocalReferenceExpression (SynthesizedLocalKind.ForLoopStepValue) (OperationKind.SyntheticLocalReferenceExpression, Type: System.Double) (Syntax: 's')
            Right: ILocalReferenceExpression: s (OperationKind.LocalReferenceExpression, Type: System.Double) (Syntax: 's')
  AtLoopBottom:
      IExpressionStatement (OperationKind.ExpressionStatement) (Syntax: 's')
        Expression: ICompoundAssignmentExpression (BinaryOperationKind.FloatingAdd) (OperationKind.CompoundAssignmentExpression, Type: System.Double) (Syntax: 's')
            Left: ILocalReferenceExpression: i (OperationKind.LocalReferenceExpression, Type: System.Double) (Syntax: 'i As Double')
            Right: ISyntheticLocalReferenceExpression (SynthesizedLocalKind.ForLoopStepValue) (OperationKind.SyntheticLocalReferenceExpression, Type: System.Double) (Syntax: 's')
  Body: IBlockStatement (1 statements) (OperationKind.BlockStatement) (Syntax: 'For i As Do ... Next')
      IExpressionStatement (OperationKind.ExpressionStatement) (Syntax: 'System.Cons ... riteLine(i)')
        Expression: IInvocationExpression (Sub System.Console.WriteLine(value As System.Double)) (OperationKind.InvocationExpression, Type: System.Void) (Syntax: 'System.Cons ... riteLine(i)')
            Instance Receiver: null
            Arguments(1):
                IArgument (ArgumentKind.Explicit, Matching Parameter: value) (OperationKind.Argument) (Syntax: 'i')
                  ILocalReferenceExpression: i (OperationKind.LocalReferenceExpression, Type: System.Double) (Syntax: 'i')
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
IForLoopStatement (LoopKind.For) (OperationKind.LoopStatement) (Syntax: 'For ctrlVar ... Next')
  Condition: IConditionalChoiceExpression (OperationKind.ConditionalChoiceExpression, Type: System.Boolean) (Syntax: 'limit')
      Condition: IBinaryOperatorExpression (BinaryOperationKind.ObjectGreaterThanOrEqual) (OperationKind.BinaryOperatorExpression, Type: System.Boolean) (Syntax: 'stp')
          Left: ISyntheticLocalReferenceExpression (SynthesizedLocalKind.ForLoopStepValue) (OperationKind.SyntheticLocalReferenceExpression, Type: System.Object) (Syntax: 'stp')
          Right: ILiteralExpression (Text: 1) (OperationKind.LiteralExpression, Type: System.Object, Constant: 1) (Syntax: 'stp')
      IfTrue: IBinaryOperatorExpression (BinaryOperationKind.ObjectLessThanOrEqual) (OperationKind.BinaryOperatorExpression, Type: System.Boolean) (Syntax: 'limit')
          Left: ILocalReferenceExpression: ctrlVar (OperationKind.LocalReferenceExpression, Type: System.Object) (Syntax: 'ctrlVar')
          Right: ISyntheticLocalReferenceExpression (SynthesizedLocalKind.ForLoopLimitValue) (OperationKind.SyntheticLocalReferenceExpression, Type: System.Object) (Syntax: 'limit')
      IfFalse: IBinaryOperatorExpression (BinaryOperationKind.ObjectGreaterThanOrEqual) (OperationKind.BinaryOperatorExpression, Type: System.Boolean) (Syntax: 'limit')
          Left: ILocalReferenceExpression: ctrlVar (OperationKind.LocalReferenceExpression, Type: System.Object) (Syntax: 'ctrlVar')
          Right: ISyntheticLocalReferenceExpression (SynthesizedLocalKind.ForLoopLimitValue) (OperationKind.SyntheticLocalReferenceExpression, Type: System.Object) (Syntax: 'limit')
  Before:
      IExpressionStatement (OperationKind.ExpressionStatement) (Syntax: 'initValue')
        Expression: ISimpleAssignmentExpression (OperationKind.SimpleAssignmentExpression, Type: System.Object) (Syntax: 'initValue')
            Left: ILocalReferenceExpression: ctrlVar (OperationKind.LocalReferenceExpression, Type: System.Object) (Syntax: 'ctrlVar')
            Right: ILocalReferenceExpression: initValue (OperationKind.LocalReferenceExpression, Type: System.Object) (Syntax: 'initValue')
      IExpressionStatement (OperationKind.ExpressionStatement) (Syntax: 'limit')
        Expression: ISimpleAssignmentExpression (OperationKind.SimpleAssignmentExpression, Type: System.Object) (Syntax: 'limit')
            Left: ISyntheticLocalReferenceExpression (SynthesizedLocalKind.ForLoopLimitValue) (OperationKind.SyntheticLocalReferenceExpression, Type: System.Object) (Syntax: 'limit')
            Right: ILocalReferenceExpression: limit (OperationKind.LocalReferenceExpression, Type: System.Object) (Syntax: 'limit')
      IExpressionStatement (OperationKind.ExpressionStatement) (Syntax: 'stp')
        Expression: ISimpleAssignmentExpression (OperationKind.SimpleAssignmentExpression, Type: System.Object) (Syntax: 'stp')
            Left: ISyntheticLocalReferenceExpression (SynthesizedLocalKind.ForLoopStepValue) (OperationKind.SyntheticLocalReferenceExpression, Type: System.Object) (Syntax: 'stp')
            Right: ILocalReferenceExpression: stp (OperationKind.LocalReferenceExpression, Type: System.Object) (Syntax: 'stp')
  AtLoopBottom:
      IExpressionStatement (OperationKind.ExpressionStatement) (Syntax: 'stp')
        Expression: ICompoundAssignmentExpression (BinaryOperationKind.ObjectAdd) (OperationKind.CompoundAssignmentExpression, Type: System.Object) (Syntax: 'stp')
            Left: ILocalReferenceExpression: ctrlVar (OperationKind.LocalReferenceExpression, Type: System.Object) (Syntax: 'ctrlVar')
            Right: ISyntheticLocalReferenceExpression (SynthesizedLocalKind.ForLoopStepValue) (OperationKind.SyntheticLocalReferenceExpression, Type: System.Object) (Syntax: 'stp')
  Body: IBlockStatement (1 statements) (OperationKind.BlockStatement) (Syntax: 'For ctrlVar ... Next')
      IExpressionStatement (OperationKind.ExpressionStatement) (Syntax: 'System.Cons ... ne(ctrlVar)')
        Expression: IInvocationExpression (Sub System.Console.WriteLine(value As System.Object)) (OperationKind.InvocationExpression, Type: System.Void) (Syntax: 'System.Cons ... ne(ctrlVar)')
            Instance Receiver: null
            Arguments(1):
                IArgument (ArgumentKind.Explicit, Matching Parameter: value) (OperationKind.Argument) (Syntax: 'ctrlVar')
                  ILocalReferenceExpression: ctrlVar (OperationKind.LocalReferenceExpression, Type: System.Object) (Syntax: 'ctrlVar')
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
IForLoopStatement (LoopKind.For) (OperationKind.LoopStatement) (Syntax: 'For AVarNam ... xt AVarName')
  Condition: IBinaryOperatorExpression (BinaryOperationKind.IntegerLessThanOrEqual) (OperationKind.BinaryOperatorExpression, Type: System.Boolean) (Syntax: '2')
      Left: ILocalReferenceExpression: AVarName (OperationKind.LocalReferenceExpression, Type: System.Int32) (Syntax: 'AVarName')
      Right: ILiteralExpression (Text: 2) (OperationKind.LiteralExpression, Type: System.Int32, Constant: 2) (Syntax: '2')
  Before:
      IExpressionStatement (OperationKind.ExpressionStatement) (Syntax: '1')
        Expression: ISimpleAssignmentExpression (OperationKind.SimpleAssignmentExpression, Type: System.Int32) (Syntax: '1')
            Left: ILocalReferenceExpression: AVarName (OperationKind.LocalReferenceExpression, Type: System.Int32) (Syntax: 'AVarName')
            Right: ILiteralExpression (Text: 1) (OperationKind.LiteralExpression, Type: System.Int32, Constant: 1) (Syntax: '1')
  AtLoopBottom:
      IExpressionStatement (OperationKind.ExpressionStatement) (Syntax: 'For AVarNam ... xt AVarName')
        Expression: ICompoundAssignmentExpression (BinaryOperationKind.IntegerAdd) (OperationKind.CompoundAssignmentExpression, Type: System.Int32) (Syntax: 'For AVarNam ... xt AVarName')
            Left: ILocalReferenceExpression: AVarName (OperationKind.LocalReferenceExpression, Type: System.Int32) (Syntax: 'AVarName')
            Right: IConversionExpression (Explicit, TryCast: False, Unchecked) (OperationKind.ConversionExpression, Type: System.Int32, Constant: 1) (Syntax: 'For AVarNam ... xt AVarName')
                Conversion: CommonConversion (Exists: False, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                Operand: ILiteralExpression (OperationKind.LiteralExpression, Type: System.Int32, Constant: 1) (Syntax: 'For AVarNam ... xt AVarName')
  Body: IBlockStatement (1 statements) (OperationKind.BlockStatement) (Syntax: 'For AVarNam ... xt AVarName')
      IForLoopStatement (LoopKind.For) (OperationKind.LoopStatement) (Syntax: 'For B = 1 T ... Next B')
        Condition: IBinaryOperatorExpression (BinaryOperationKind.IntegerLessThanOrEqual) (OperationKind.BinaryOperatorExpression, Type: System.Boolean) (Syntax: '2')
            Left: ILocalReferenceExpression: B (OperationKind.LocalReferenceExpression, Type: System.Int32) (Syntax: 'B')
            Right: ILiteralExpression (Text: 2) (OperationKind.LiteralExpression, Type: System.Int32, Constant: 2) (Syntax: '2')
        Before:
            IExpressionStatement (OperationKind.ExpressionStatement) (Syntax: '1')
              Expression: ISimpleAssignmentExpression (OperationKind.SimpleAssignmentExpression, Type: System.Int32) (Syntax: '1')
                  Left: ILocalReferenceExpression: B (OperationKind.LocalReferenceExpression, Type: System.Int32) (Syntax: 'B')
                  Right: ILiteralExpression (Text: 1) (OperationKind.LiteralExpression, Type: System.Int32, Constant: 1) (Syntax: '1')
        AtLoopBottom:
            IExpressionStatement (OperationKind.ExpressionStatement) (Syntax: 'For B = 1 T ... Next B')
              Expression: ICompoundAssignmentExpression (BinaryOperationKind.IntegerAdd) (OperationKind.CompoundAssignmentExpression, Type: System.Int32) (Syntax: 'For B = 1 T ... Next B')
                  Left: ILocalReferenceExpression: B (OperationKind.LocalReferenceExpression, Type: System.Int32) (Syntax: 'B')
                  Right: IConversionExpression (Explicit, TryCast: False, Unchecked) (OperationKind.ConversionExpression, Type: System.Int32, Constant: 1) (Syntax: 'For B = 1 T ... Next B')
                      Conversion: CommonConversion (Exists: False, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                      Operand: ILiteralExpression (OperationKind.LiteralExpression, Type: System.Int32, Constant: 1) (Syntax: 'For B = 1 T ... Next B')
        Body: IBlockStatement (1 statements) (OperationKind.BlockStatement) (Syntax: 'For B = 1 T ... Next B')
            IForLoopStatement (LoopKind.For) (OperationKind.LoopStatement) (Syntax: 'For C = 1 T ... Next C')
              Condition: IBinaryOperatorExpression (BinaryOperationKind.IntegerLessThanOrEqual) (OperationKind.BinaryOperatorExpression, Type: System.Boolean) (Syntax: '2')
                  Left: ILocalReferenceExpression: C (OperationKind.LocalReferenceExpression, Type: System.Int32) (Syntax: 'C')
                  Right: ILiteralExpression (Text: 2) (OperationKind.LiteralExpression, Type: System.Int32, Constant: 2) (Syntax: '2')
              Before:
                  IExpressionStatement (OperationKind.ExpressionStatement) (Syntax: '1')
                    Expression: ISimpleAssignmentExpression (OperationKind.SimpleAssignmentExpression, Type: System.Int32) (Syntax: '1')
                        Left: ILocalReferenceExpression: C (OperationKind.LocalReferenceExpression, Type: System.Int32) (Syntax: 'C')
                        Right: ILiteralExpression (Text: 1) (OperationKind.LiteralExpression, Type: System.Int32, Constant: 1) (Syntax: '1')
              AtLoopBottom:
                  IExpressionStatement (OperationKind.ExpressionStatement) (Syntax: 'For C = 1 T ... Next C')
                    Expression: ICompoundAssignmentExpression (BinaryOperationKind.IntegerAdd) (OperationKind.CompoundAssignmentExpression, Type: System.Int32) (Syntax: 'For C = 1 T ... Next C')
                        Left: ILocalReferenceExpression: C (OperationKind.LocalReferenceExpression, Type: System.Int32) (Syntax: 'C')
                        Right: IConversionExpression (Explicit, TryCast: False, Unchecked) (OperationKind.ConversionExpression, Type: System.Int32, Constant: 1) (Syntax: 'For C = 1 T ... Next C')
                            Conversion: CommonConversion (Exists: False, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                            Operand: ILiteralExpression (OperationKind.LiteralExpression, Type: System.Int32, Constant: 1) (Syntax: 'For C = 1 T ... Next C')
              Body: IBlockStatement (1 statements) (OperationKind.BlockStatement) (Syntax: 'For C = 1 T ... Next C')
                  IForLoopStatement (LoopKind.For) (OperationKind.LoopStatement) (Syntax: 'For D = 1 T ... Next D')
                    Condition: IBinaryOperatorExpression (BinaryOperationKind.IntegerLessThanOrEqual) (OperationKind.BinaryOperatorExpression, Type: System.Boolean) (Syntax: '2')
                        Left: ILocalReferenceExpression: D (OperationKind.LocalReferenceExpression, Type: System.Int32) (Syntax: 'D')
                        Right: ILiteralExpression (Text: 2) (OperationKind.LiteralExpression, Type: System.Int32, Constant: 2) (Syntax: '2')
                    Before:
                        IExpressionStatement (OperationKind.ExpressionStatement) (Syntax: '1')
                          Expression: ISimpleAssignmentExpression (OperationKind.SimpleAssignmentExpression, Type: System.Int32) (Syntax: '1')
                              Left: ILocalReferenceExpression: D (OperationKind.LocalReferenceExpression, Type: System.Int32) (Syntax: 'D')
                              Right: ILiteralExpression (Text: 1) (OperationKind.LiteralExpression, Type: System.Int32, Constant: 1) (Syntax: '1')
                    AtLoopBottom:
                        IExpressionStatement (OperationKind.ExpressionStatement) (Syntax: 'For D = 1 T ... Next D')
                          Expression: ICompoundAssignmentExpression (BinaryOperationKind.IntegerAdd) (OperationKind.CompoundAssignmentExpression, Type: System.Int32) (Syntax: 'For D = 1 T ... Next D')
                              Left: ILocalReferenceExpression: D (OperationKind.LocalReferenceExpression, Type: System.Int32) (Syntax: 'D')
                              Right: IConversionExpression (Explicit, TryCast: False, Unchecked) (OperationKind.ConversionExpression, Type: System.Int32, Constant: 1) (Syntax: 'For D = 1 T ... Next D')
                                  Conversion: CommonConversion (Exists: False, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                                  Operand: ILiteralExpression (OperationKind.LiteralExpression, Type: System.Int32, Constant: 1) (Syntax: 'For D = 1 T ... Next D')
                    Body: IBlockStatement (0 statements) (OperationKind.BlockStatement) (Syntax: 'For D = 1 T ... Next D')
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
IForLoopStatement (LoopKind.For) (OperationKind.LoopStatement) (Syntax: 'For I = 1 T ... Next')
  Condition: IBinaryOperatorExpression (BinaryOperationKind.IntegerLessThanOrEqual) (OperationKind.BinaryOperatorExpression, Type: System.Boolean) (Syntax: '2')
      Left: ILocalReferenceExpression: I (OperationKind.LocalReferenceExpression, Type: System.Int32) (Syntax: 'I')
      Right: ILiteralExpression (Text: 2) (OperationKind.LiteralExpression, Type: System.Int32, Constant: 2) (Syntax: '2')
  Before:
      IExpressionStatement (OperationKind.ExpressionStatement) (Syntax: '1')
        Expression: ISimpleAssignmentExpression (OperationKind.SimpleAssignmentExpression, Type: System.Int32) (Syntax: '1')
            Left: ILocalReferenceExpression: I (OperationKind.LocalReferenceExpression, Type: System.Int32) (Syntax: 'I')
            Right: ILiteralExpression (Text: 1) (OperationKind.LiteralExpression, Type: System.Int32, Constant: 1) (Syntax: '1')
  AtLoopBottom:
      IExpressionStatement (OperationKind.ExpressionStatement) (Syntax: 'For I = 1 T ... Next')
        Expression: ICompoundAssignmentExpression (BinaryOperationKind.IntegerAdd) (OperationKind.CompoundAssignmentExpression, Type: System.Int32) (Syntax: 'For I = 1 T ... Next')
            Left: ILocalReferenceExpression: I (OperationKind.LocalReferenceExpression, Type: System.Int32) (Syntax: 'I')
            Right: IConversionExpression (Explicit, TryCast: False, Unchecked) (OperationKind.ConversionExpression, Type: System.Int32, Constant: 1) (Syntax: 'For I = 1 T ... Next')
                Conversion: CommonConversion (Exists: False, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                Operand: ILiteralExpression (OperationKind.LiteralExpression, Type: System.Int32, Constant: 1) (Syntax: 'For I = 1 T ... Next')
  Body: IBlockStatement (1 statements) (OperationKind.BlockStatement) (Syntax: 'For I = 1 T ... Next')
      IForLoopStatement (LoopKind.For) (OperationKind.LoopStatement) (Syntax: 'For J = 1 T ... Next')
        Condition: IBinaryOperatorExpression (BinaryOperationKind.IntegerLessThanOrEqual) (OperationKind.BinaryOperatorExpression, Type: System.Boolean) (Syntax: '2')
            Left: ILocalReferenceExpression: J (OperationKind.LocalReferenceExpression, Type: System.Int32) (Syntax: 'J')
            Right: ILiteralExpression (Text: 2) (OperationKind.LiteralExpression, Type: System.Int32, Constant: 2) (Syntax: '2')
        Before:
            IExpressionStatement (OperationKind.ExpressionStatement) (Syntax: '1')
              Expression: ISimpleAssignmentExpression (OperationKind.SimpleAssignmentExpression, Type: System.Int32) (Syntax: '1')
                  Left: ILocalReferenceExpression: J (OperationKind.LocalReferenceExpression, Type: System.Int32) (Syntax: 'J')
                  Right: ILiteralExpression (Text: 1) (OperationKind.LiteralExpression, Type: System.Int32, Constant: 1) (Syntax: '1')
        AtLoopBottom:
            IExpressionStatement (OperationKind.ExpressionStatement) (Syntax: 'For J = 1 T ... Next')
              Expression: ICompoundAssignmentExpression (BinaryOperationKind.IntegerAdd) (OperationKind.CompoundAssignmentExpression, Type: System.Int32) (Syntax: 'For J = 1 T ... Next')
                  Left: ILocalReferenceExpression: J (OperationKind.LocalReferenceExpression, Type: System.Int32) (Syntax: 'J')
                  Right: IConversionExpression (Explicit, TryCast: False, Unchecked) (OperationKind.ConversionExpression, Type: System.Int32, Constant: 1) (Syntax: 'For J = 1 T ... Next')
                      Conversion: CommonConversion (Exists: False, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                      Operand: ILiteralExpression (OperationKind.LiteralExpression, Type: System.Int32, Constant: 1) (Syntax: 'For J = 1 T ... Next')
        Body: IBlockStatement (2 statements) (OperationKind.BlockStatement) (Syntax: 'For J = 1 T ... Next')
            IExpressionStatement (OperationKind.ExpressionStatement) (Syntax: 'I = 3')
              Expression: ISimpleAssignmentExpression (OperationKind.SimpleAssignmentExpression, Type: System.Int32) (Syntax: 'I = 3')
                  Left: ILocalReferenceExpression: I (OperationKind.LocalReferenceExpression, Type: System.Int32) (Syntax: 'I')
                  Right: ILiteralExpression (Text: 3) (OperationKind.LiteralExpression, Type: System.Int32, Constant: 3) (Syntax: '3')
            IExpressionStatement (OperationKind.ExpressionStatement) (Syntax: 'System.Cons ... riteLine(I)')
              Expression: IInvocationExpression (Sub System.Console.WriteLine(value As System.Int32)) (OperationKind.InvocationExpression, Type: System.Void) (Syntax: 'System.Cons ... riteLine(I)')
                  Instance Receiver: null
                  Arguments(1):
                      IArgument (ArgumentKind.Explicit, Matching Parameter: value) (OperationKind.Argument) (Syntax: 'I')
                        ILocalReferenceExpression: I (OperationKind.LocalReferenceExpression, Type: System.Int32) (Syntax: 'I')
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
IForLoopStatement (LoopKind.For) (OperationKind.LoopStatement) (Syntax: 'For I = 1 T ... Next')
  Condition: IBinaryOperatorExpression (BinaryOperationKind.IntegerLessThanOrEqual) (OperationKind.BinaryOperatorExpression, Type: System.Boolean) (Syntax: '2')
      Left: ILocalReferenceExpression: I (OperationKind.LocalReferenceExpression, Type: System.Int32) (Syntax: 'I')
      Right: ILiteralExpression (Text: 2) (OperationKind.LiteralExpression, Type: System.Int32, Constant: 2) (Syntax: '2')
  Before:
      IExpressionStatement (OperationKind.ExpressionStatement) (Syntax: '1')
        Expression: ISimpleAssignmentExpression (OperationKind.SimpleAssignmentExpression, Type: System.Int32) (Syntax: '1')
            Left: ILocalReferenceExpression: I (OperationKind.LocalReferenceExpression, Type: System.Int32) (Syntax: 'I')
            Right: ILiteralExpression (Text: 1) (OperationKind.LiteralExpression, Type: System.Int32, Constant: 1) (Syntax: '1')
  AtLoopBottom:
      IExpressionStatement (OperationKind.ExpressionStatement) (Syntax: 'For I = 1 T ... Next')
        Expression: ICompoundAssignmentExpression (BinaryOperationKind.IntegerAdd) (OperationKind.CompoundAssignmentExpression, Type: System.Int32) (Syntax: 'For I = 1 T ... Next')
            Left: ILocalReferenceExpression: I (OperationKind.LocalReferenceExpression, Type: System.Int32) (Syntax: 'I')
            Right: IConversionExpression (Explicit, TryCast: False, Unchecked) (OperationKind.ConversionExpression, Type: System.Int32, Constant: 1) (Syntax: 'For I = 1 T ... Next')
                Conversion: CommonConversion (Exists: False, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                Operand: ILiteralExpression (OperationKind.LiteralExpression, Type: System.Int32, Constant: 1) (Syntax: 'For I = 1 T ... Next')
  Body: IBlockStatement (1 statements) (OperationKind.BlockStatement) (Syntax: 'For I = 1 T ... Next')
      IForLoopStatement (LoopKind.For) (OperationKind.LoopStatement) (Syntax: 'For J = I + ... Next')
        Condition: IBinaryOperatorExpression (BinaryOperationKind.IntegerLessThanOrEqual) (OperationKind.BinaryOperatorExpression, Type: System.Boolean) (Syntax: '2')
            Left: ILocalReferenceExpression: J (OperationKind.LocalReferenceExpression, Type: System.Int32) (Syntax: 'J')
            Right: ILiteralExpression (Text: 2) (OperationKind.LiteralExpression, Type: System.Int32, Constant: 2) (Syntax: '2')
        Before:
            IExpressionStatement (OperationKind.ExpressionStatement) (Syntax: 'I + 1')
              Expression: ISimpleAssignmentExpression (OperationKind.SimpleAssignmentExpression, Type: System.Int32) (Syntax: 'I + 1')
                  Left: ILocalReferenceExpression: J (OperationKind.LocalReferenceExpression, Type: System.Int32) (Syntax: 'J')
                  Right: IBinaryOperatorExpression (BinaryOperationKind.IntegerAdd) (OperationKind.BinaryOperatorExpression, Type: System.Int32) (Syntax: 'I + 1')
                      Left: ILocalReferenceExpression: I (OperationKind.LocalReferenceExpression, Type: System.Int32) (Syntax: 'I')
                      Right: ILiteralExpression (Text: 1) (OperationKind.LiteralExpression, Type: System.Int32, Constant: 1) (Syntax: '1')
        AtLoopBottom:
            IExpressionStatement (OperationKind.ExpressionStatement) (Syntax: 'For J = I + ... Next')
              Expression: ICompoundAssignmentExpression (BinaryOperationKind.IntegerAdd) (OperationKind.CompoundAssignmentExpression, Type: System.Int32) (Syntax: 'For J = I + ... Next')
                  Left: ILocalReferenceExpression: J (OperationKind.LocalReferenceExpression, Type: System.Int32) (Syntax: 'J')
                  Right: IConversionExpression (Explicit, TryCast: False, Unchecked) (OperationKind.ConversionExpression, Type: System.Int32, Constant: 1) (Syntax: 'For J = I + ... Next')
                      Conversion: CommonConversion (Exists: False, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                      Operand: ILiteralExpression (OperationKind.LiteralExpression, Type: System.Int32, Constant: 1) (Syntax: 'For J = I + ... Next')
        Body: IBlockStatement (1 statements) (OperationKind.BlockStatement) (Syntax: 'For J = I + ... Next')
            IExpressionStatement (OperationKind.ExpressionStatement) (Syntax: 'System.Cons ... riteLine(J)')
              Expression: IInvocationExpression (Sub System.Console.WriteLine(value As System.Int32)) (OperationKind.InvocationExpression, Type: System.Void) (Syntax: 'System.Cons ... riteLine(J)')
                  Instance Receiver: null
                  Arguments(1):
                      IArgument (ArgumentKind.Explicit, Matching Parameter: value) (OperationKind.Argument) (Syntax: 'J')
                        ILocalReferenceExpression: J (OperationKind.LocalReferenceExpression, Type: System.Int32) (Syntax: 'J')
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
IForLoopStatement (LoopKind.For) (OperationKind.LoopStatement) (Syntax: 'For I = 1 T ... Next')
  Condition: IBinaryOperatorExpression (BinaryOperationKind.IntegerLessThanOrEqual) (OperationKind.BinaryOperatorExpression, Type: System.Boolean) (Syntax: '2')
      Left: ILocalReferenceExpression: I (OperationKind.LocalReferenceExpression, Type: System.Int32) (Syntax: 'I')
      Right: ILiteralExpression (Text: 2) (OperationKind.LiteralExpression, Type: System.Int32, Constant: 2) (Syntax: '2')
  Before:
      IExpressionStatement (OperationKind.ExpressionStatement) (Syntax: '1')
        Expression: ISimpleAssignmentExpression (OperationKind.SimpleAssignmentExpression, Type: System.Int32) (Syntax: '1')
            Left: ILocalReferenceExpression: I (OperationKind.LocalReferenceExpression, Type: System.Int32) (Syntax: 'I')
            Right: ILiteralExpression (Text: 1) (OperationKind.LiteralExpression, Type: System.Int32, Constant: 1) (Syntax: '1')
  AtLoopBottom:
      IExpressionStatement (OperationKind.ExpressionStatement) (Syntax: 'For I = 1 T ... Next')
        Expression: ICompoundAssignmentExpression (BinaryOperationKind.IntegerAdd) (OperationKind.CompoundAssignmentExpression, Type: System.Int32) (Syntax: 'For I = 1 T ... Next')
            Left: ILocalReferenceExpression: I (OperationKind.LocalReferenceExpression, Type: System.Int32) (Syntax: 'I')
            Right: IConversionExpression (Explicit, TryCast: False, Unchecked) (OperationKind.ConversionExpression, Type: System.Int32, Constant: 1) (Syntax: 'For I = 1 T ... Next')
                Conversion: CommonConversion (Exists: False, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                Operand: ILiteralExpression (OperationKind.LiteralExpression, Type: System.Int32, Constant: 1) (Syntax: 'For I = 1 T ... Next')
  Body: IBlockStatement (2 statements) (OperationKind.BlockStatement) (Syntax: 'For I = 1 T ... Next')
      IForLoopStatement (LoopKind.For) (OperationKind.LoopStatement) (Syntax: 'For J = 1 T ... Next')
        Condition: IBinaryOperatorExpression (BinaryOperationKind.IntegerLessThanOrEqual) (OperationKind.BinaryOperatorExpression, Type: System.Boolean) (Syntax: '2')
            Left: ILocalReferenceExpression: J (OperationKind.LocalReferenceExpression, Type: System.Int32) (Syntax: 'J')
            Right: ILiteralExpression (Text: 2) (OperationKind.LiteralExpression, Type: System.Int32, Constant: 2) (Syntax: '2')
        Before:
            IExpressionStatement (OperationKind.ExpressionStatement) (Syntax: '1')
              Expression: ISimpleAssignmentExpression (OperationKind.SimpleAssignmentExpression, Type: System.Int32) (Syntax: '1')
                  Left: ILocalReferenceExpression: J (OperationKind.LocalReferenceExpression, Type: System.Int32) (Syntax: 'J')
                  Right: ILiteralExpression (Text: 1) (OperationKind.LiteralExpression, Type: System.Int32, Constant: 1) (Syntax: '1')
        AtLoopBottom:
            IExpressionStatement (OperationKind.ExpressionStatement) (Syntax: 'For J = 1 T ... Next')
              Expression: ICompoundAssignmentExpression (BinaryOperationKind.IntegerAdd) (OperationKind.CompoundAssignmentExpression, Type: System.Int32) (Syntax: 'For J = 1 T ... Next')
                  Left: ILocalReferenceExpression: J (OperationKind.LocalReferenceExpression, Type: System.Int32) (Syntax: 'J')
                  Right: IConversionExpression (Explicit, TryCast: False, Unchecked) (OperationKind.ConversionExpression, Type: System.Int32, Constant: 1) (Syntax: 'For J = 1 T ... Next')
                      Conversion: CommonConversion (Exists: False, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                      Operand: ILiteralExpression (OperationKind.LiteralExpression, Type: System.Int32, Constant: 1) (Syntax: 'For J = 1 T ... Next')
        Body: IBlockStatement (1 statements) (OperationKind.BlockStatement) (Syntax: 'For J = 1 T ... Next')
            IBranchStatement (BranchKind.Break, Label: exit) (OperationKind.BranchStatement) (Syntax: 'Exit For')
      IExpressionStatement (OperationKind.ExpressionStatement) (Syntax: 'System.Cons ... riteLine(I)')
        Expression: IInvocationExpression (Sub System.Console.WriteLine(value As System.Int32)) (OperationKind.InvocationExpression, Type: System.Void) (Syntax: 'System.Cons ... riteLine(I)')
            Instance Receiver: null
            Arguments(1):
                IArgument (ArgumentKind.Explicit, Matching Parameter: value) (OperationKind.Argument) (Syntax: 'I')
                  ILocalReferenceExpression: I (OperationKind.LocalReferenceExpression, Type: System.Int32) (Syntax: 'I')
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
IForLoopStatement (LoopKind.For) (OperationKind.LoopStatement) (Syntax: 'For x As e1 ... Next')
  Condition: IBinaryOperatorExpression (BinaryOperationKind.EnumLessThanOrEqual) (OperationKind.BinaryOperatorExpression, Type: System.Boolean) (Syntax: 'e1.c')
      Left: ILocalReferenceExpression: x (OperationKind.LocalReferenceExpression, Type: e1) (Syntax: 'x As e1')
      Right: IFieldReferenceExpression: e1.c (Static) (OperationKind.FieldReferenceExpression, Type: e1, Constant: 2) (Syntax: 'e1.c')
          Instance Receiver: null
  Before:
      IExpressionStatement (OperationKind.ExpressionStatement) (Syntax: 'e1.a')
        Expression: ISimpleAssignmentExpression (OperationKind.SimpleAssignmentExpression, Type: e1) (Syntax: 'e1.a')
            Left: ILocalReferenceExpression: x (OperationKind.LocalReferenceExpression, Type: e1) (Syntax: 'x As e1')
            Right: IFieldReferenceExpression: e1.a (Static) (OperationKind.FieldReferenceExpression, Type: e1, Constant: 0) (Syntax: 'e1.a')
                Instance Receiver: null
  AtLoopBottom:
      IExpressionStatement (OperationKind.ExpressionStatement) (Syntax: 'For x As e1 ... Next')
        Expression: ICompoundAssignmentExpression (BinaryOperationKind.EnumAdd) (OperationKind.CompoundAssignmentExpression, Type: e1) (Syntax: 'For x As e1 ... Next')
            Left: ILocalReferenceExpression: x (OperationKind.LocalReferenceExpression, Type: e1) (Syntax: 'x As e1')
            Right: IConversionExpression (Explicit, TryCast: False, Unchecked) (OperationKind.ConversionExpression, Type: e1, Constant: 1) (Syntax: 'For x As e1 ... Next')
                Conversion: CommonConversion (Exists: False, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                Operand: ILiteralExpression (OperationKind.LiteralExpression, Type: System.Int32, Constant: 1) (Syntax: 'For x As e1 ... Next')
  Body: IBlockStatement (0 statements) (OperationKind.BlockStatement) (Syntax: 'For x As e1 ... Next')
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
IForLoopStatement (LoopKind.For) (OperationKind.LoopStatement) (Syntax: 'For i As In ... Next')
  Condition: IBinaryOperatorExpression (BinaryOperationKind.IntegerLessThanOrEqual) (OperationKind.BinaryOperatorExpression, Type: System.Boolean) (Syntax: '30')
      Left: ILocalReferenceExpression: i (OperationKind.LocalReferenceExpression, Type: System.Int32) (Syntax: 'i As Integer')
      Right: ILiteralExpression (Text: 30) (OperationKind.LiteralExpression, Type: System.Int32, Constant: 30) (Syntax: '30')
  Before:
      IExpressionStatement (OperationKind.ExpressionStatement) (Syntax: 'P1(30 + i)')
        Expression: ISimpleAssignmentExpression (OperationKind.SimpleAssignmentExpression, Type: System.Int32) (Syntax: 'P1(30 + i)')
            Left: ILocalReferenceExpression: i (OperationKind.LocalReferenceExpression, Type: System.Int32) (Syntax: 'i As Integer')
            Right: IConversionExpression (Implicit, TryCast: False, Unchecked) (OperationKind.ConversionExpression, Type: System.Int32) (Syntax: 'P1(30 + i)')
                Conversion: CommonConversion (Exists: False, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                Operand: IPropertyReferenceExpression: Property MyClass1.P1(x As System.Int64) As System.Byte (OperationKind.PropertyReferenceExpression, Type: System.Byte) (Syntax: 'P1(30 + i)')
                    Instance Receiver: IInstanceReferenceExpression (InstanceReferenceKind.Implicit) (OperationKind.InstanceReferenceExpression, Type: MyClass1) (Syntax: 'P1')
                    Arguments(1):
                        IArgument (ArgumentKind.Explicit, Matching Parameter: x) (OperationKind.Argument) (Syntax: '30 + i')
                          IConversionExpression (Implicit, TryCast: False, Unchecked) (OperationKind.ConversionExpression, Type: System.Int64) (Syntax: '30 + i')
                            Conversion: CommonConversion (Exists: False, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                            Operand: IBinaryOperatorExpression (BinaryOperationKind.IntegerAdd) (OperationKind.BinaryOperatorExpression, Type: System.Int32) (Syntax: '30 + i')
                                Left: ILiteralExpression (Text: 30) (OperationKind.LiteralExpression, Type: System.Int32, Constant: 30) (Syntax: '30')
                                Right: ILocalReferenceExpression: i (OperationKind.LocalReferenceExpression, Type: System.Int32) (Syntax: 'i')
  AtLoopBottom:
      IExpressionStatement (OperationKind.ExpressionStatement) (Syntax: 'For i As In ... Next')
        Expression: ICompoundAssignmentExpression (BinaryOperationKind.IntegerAdd) (OperationKind.CompoundAssignmentExpression, Type: System.Int32) (Syntax: 'For i As In ... Next')
            Left: ILocalReferenceExpression: i (OperationKind.LocalReferenceExpression, Type: System.Int32) (Syntax: 'i As Integer')
            Right: IConversionExpression (Explicit, TryCast: False, Unchecked) (OperationKind.ConversionExpression, Type: System.Int32, Constant: 1) (Syntax: 'For i As In ... Next')
                Conversion: CommonConversion (Exists: False, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                Operand: ILiteralExpression (OperationKind.LiteralExpression, Type: System.Int32, Constant: 1) (Syntax: 'For i As In ... Next')
  Body: IBlockStatement (0 statements) (OperationKind.BlockStatement) (Syntax: 'For i As In ... Next')
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
IForLoopStatement (LoopKind.For) (OperationKind.LoopStatement) (Syntax: 'For global_ ... Next')
  Condition: IBinaryOperatorExpression (BinaryOperationKind.IntegerLessThanOrEqual) (OperationKind.BinaryOperatorExpression, Type: System.Boolean) (Syntax: '10')
      Left: ILocalReferenceExpression: global_x (OperationKind.LocalReferenceExpression, Type: System.Int32) (Syntax: 'global_x As Integer')
      Right: ILiteralExpression (Text: 10) (OperationKind.LiteralExpression, Type: System.Int32, Constant: 10) (Syntax: '10')
  Before:
      IExpressionStatement (OperationKind.ExpressionStatement) (Syntax: 'global_y')
        Expression: ISimpleAssignmentExpression (OperationKind.SimpleAssignmentExpression, Type: System.Int32) (Syntax: 'global_y')
            Left: ILocalReferenceExpression: global_x (OperationKind.LocalReferenceExpression, Type: System.Int32) (Syntax: 'global_x As Integer')
            Right: IConversionExpression (Implicit, TryCast: False, Unchecked) (OperationKind.ConversionExpression, Type: System.Int32, Constant: 20) (Syntax: 'global_y')
                Conversion: CommonConversion (Exists: False, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                Operand: IFieldReferenceExpression: MyClass1.global_y As System.Int64 (Static) (OperationKind.FieldReferenceExpression, Type: System.Int64, Constant: 20) (Syntax: 'global_y')
                    Instance Receiver: null
  AtLoopBottom:
      IExpressionStatement (OperationKind.ExpressionStatement) (Syntax: 'For global_ ... Next')
        Expression: ICompoundAssignmentExpression (BinaryOperationKind.IntegerAdd) (OperationKind.CompoundAssignmentExpression, Type: System.Int32) (Syntax: 'For global_ ... Next')
            Left: ILocalReferenceExpression: global_x (OperationKind.LocalReferenceExpression, Type: System.Int32) (Syntax: 'global_x As Integer')
            Right: IConversionExpression (Explicit, TryCast: False, Unchecked) (OperationKind.ConversionExpression, Type: System.Int32, Constant: 1) (Syntax: 'For global_ ... Next')
                Conversion: CommonConversion (Exists: False, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                Operand: ILiteralExpression (OperationKind.LiteralExpression, Type: System.Int32, Constant: 1) (Syntax: 'For global_ ... Next')
  Body: IBlockStatement (0 statements) (OperationKind.BlockStatement) (Syntax: 'For global_ ... Next')
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
IForLoopStatement (LoopKind.For) (OperationKind.LoopStatement) (Syntax: 'For x As In ... o 10 : Next')
  Condition: IBinaryOperatorExpression (BinaryOperationKind.IntegerLessThanOrEqual) (OperationKind.BinaryOperatorExpression, Type: System.Boolean) (Syntax: '10')
      Left: ILocalReferenceExpression: x (OperationKind.LocalReferenceExpression, Type: System.Int32) (Syntax: 'x As Integer')
      Right: ILiteralExpression (Text: 10) (OperationKind.LiteralExpression, Type: System.Int32, Constant: 10) (Syntax: '10')
  Before:
      IExpressionStatement (OperationKind.ExpressionStatement) (Syntax: '0')
        Expression: ISimpleAssignmentExpression (OperationKind.SimpleAssignmentExpression, Type: System.Int32) (Syntax: '0')
            Left: ILocalReferenceExpression: x (OperationKind.LocalReferenceExpression, Type: System.Int32) (Syntax: 'x As Integer')
            Right: ILiteralExpression (Text: 0) (OperationKind.LiteralExpression, Type: System.Int32, Constant: 0) (Syntax: '0')
  AtLoopBottom:
      IExpressionStatement (OperationKind.ExpressionStatement) (Syntax: 'For x As In ... o 10 : Next')
        Expression: ICompoundAssignmentExpression (BinaryOperationKind.IntegerAdd) (OperationKind.CompoundAssignmentExpression, Type: System.Int32) (Syntax: 'For x As In ... o 10 : Next')
            Left: ILocalReferenceExpression: x (OperationKind.LocalReferenceExpression, Type: System.Int32) (Syntax: 'x As Integer')
            Right: IConversionExpression (Explicit, TryCast: False, Unchecked) (OperationKind.ConversionExpression, Type: System.Int32, Constant: 1) (Syntax: 'For x As In ... o 10 : Next')
                Conversion: CommonConversion (Exists: False, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                Operand: ILiteralExpression (OperationKind.LiteralExpression, Type: System.Int32, Constant: 1) (Syntax: 'For x As In ... o 10 : Next')
  Body: IBlockStatement (0 statements) (OperationKind.BlockStatement) (Syntax: 'For x As In ... o 10 : Next')
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
IForLoopStatement (LoopKind.For) (OperationKind.LoopStatement) (Syntax: 'For Y = 1 T ... Next')
  Condition: IBinaryOperatorExpression (BinaryOperationKind.IntegerLessThanOrEqual) (OperationKind.BinaryOperatorExpression, Type: System.Boolean) (Syntax: '2')
      Left: ILocalReferenceExpression: Y (OperationKind.LocalReferenceExpression, Type: System.Int32) (Syntax: 'Y')
      Right: ILiteralExpression (Text: 2) (OperationKind.LiteralExpression, Type: System.Int32, Constant: 2) (Syntax: '2')
  Before:
      IExpressionStatement (OperationKind.ExpressionStatement) (Syntax: '1')
        Expression: ISimpleAssignmentExpression (OperationKind.SimpleAssignmentExpression, Type: System.Int32) (Syntax: '1')
            Left: ILocalReferenceExpression: Y (OperationKind.LocalReferenceExpression, Type: System.Int32) (Syntax: 'Y')
            Right: ILiteralExpression (Text: 1) (OperationKind.LiteralExpression, Type: System.Int32, Constant: 1) (Syntax: '1')
  AtLoopBottom:
      IExpressionStatement (OperationKind.ExpressionStatement) (Syntax: 'For Y = 1 T ... Next')
        Expression: ICompoundAssignmentExpression (BinaryOperationKind.IntegerAdd) (OperationKind.CompoundAssignmentExpression, Type: System.Int32) (Syntax: 'For Y = 1 T ... Next')
            Left: ILocalReferenceExpression: Y (OperationKind.LocalReferenceExpression, Type: System.Int32) (Syntax: 'Y')
            Right: IConversionExpression (Explicit, TryCast: False, Unchecked) (OperationKind.ConversionExpression, Type: System.Int32, Constant: 1) (Syntax: 'For Y = 1 T ... Next')
                Conversion: CommonConversion (Exists: False, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                Operand: ILiteralExpression (OperationKind.LiteralExpression, Type: System.Int32, Constant: 1) (Syntax: 'For Y = 1 T ... Next')
  Body: IBlockStatement (0 statements) (OperationKind.BlockStatement) (Syntax: 'For Y = 1 T ... Next')
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
IForLoopStatement (LoopKind.For) (OperationKind.LoopStatement) (Syntax: 'For element ... Next')
  Condition: IBinaryOperatorExpression (BinaryOperationKind.IntegerLessThanOrEqual) (OperationKind.BinaryOperatorExpression, Type: System.Boolean) (Syntax: '42')
      Left: ILocalReferenceExpression: element1 (OperationKind.LocalReferenceExpression, Type: System.Int32) (Syntax: 'element1')
      Right: ILiteralExpression (Text: 42) (OperationKind.LiteralExpression, Type: System.Int32, Constant: 42) (Syntax: '42')
  Before:
      IExpressionStatement (OperationKind.ExpressionStatement) (Syntax: '23')
        Expression: ISimpleAssignmentExpression (OperationKind.SimpleAssignmentExpression, Type: System.Int32) (Syntax: '23')
            Left: ILocalReferenceExpression: element1 (OperationKind.LocalReferenceExpression, Type: System.Int32) (Syntax: 'element1')
            Right: ILiteralExpression (Text: 23) (OperationKind.LiteralExpression, Type: System.Int32, Constant: 23) (Syntax: '23')
  AtLoopBottom:
      IExpressionStatement (OperationKind.ExpressionStatement) (Syntax: 'For element ... Next')
        Expression: ICompoundAssignmentExpression (BinaryOperationKind.IntegerAdd) (OperationKind.CompoundAssignmentExpression, Type: System.Int32) (Syntax: 'For element ... Next')
            Left: ILocalReferenceExpression: element1 (OperationKind.LocalReferenceExpression, Type: System.Int32) (Syntax: 'element1')
            Right: IConversionExpression (Explicit, TryCast: False, Unchecked) (OperationKind.ConversionExpression, Type: System.Int32, Constant: 1) (Syntax: 'For element ... Next')
                Conversion: CommonConversion (Exists: False, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                Operand: ILiteralExpression (OperationKind.LiteralExpression, Type: System.Int32, Constant: 1) (Syntax: 'For element ... Next')
  Body: IBlockStatement (0 statements) (OperationKind.BlockStatement) (Syntax: 'For element ... Next')
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
IForLoopStatement (LoopKind.For) (OperationKind.LoopStatement) (Syntax: 'For i As In ... Next')
  Condition: IBinaryOperatorExpression (BinaryOperationKind.IntegerLessThanOrEqual) (OperationKind.BinaryOperatorExpression, Type: System.Boolean) (Syntax: '5')
      Left: ILocalReferenceExpression: i (OperationKind.LocalReferenceExpression, Type: System.Int32) (Syntax: 'i As Integer')
      Right: ILiteralExpression (Text: 5) (OperationKind.LiteralExpression, Type: System.Int32, Constant: 5) (Syntax: '5')
  Before:
      IExpressionStatement (OperationKind.ExpressionStatement) (Syntax: '0')
        Expression: ISimpleAssignmentExpression (OperationKind.SimpleAssignmentExpression, Type: System.Int32) (Syntax: '0')
            Left: ILocalReferenceExpression: i (OperationKind.LocalReferenceExpression, Type: System.Int32) (Syntax: 'i As Integer')
            Right: ILiteralExpression (Text: 0) (OperationKind.LiteralExpression, Type: System.Int32, Constant: 0) (Syntax: '0')
  AtLoopBottom:
      IExpressionStatement (OperationKind.ExpressionStatement) (Syntax: 'For i As In ... Next')
        Expression: ICompoundAssignmentExpression (BinaryOperationKind.IntegerAdd) (OperationKind.CompoundAssignmentExpression, Type: System.Int32) (Syntax: 'For i As In ... Next')
            Left: ILocalReferenceExpression: i (OperationKind.LocalReferenceExpression, Type: System.Int32) (Syntax: 'i As Integer')
            Right: IConversionExpression (Explicit, TryCast: False, Unchecked) (OperationKind.ConversionExpression, Type: System.Int32, Constant: 1) (Syntax: 'For i As In ... Next')
                Conversion: CommonConversion (Exists: False, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                Operand: ILiteralExpression (OperationKind.LiteralExpression, Type: System.Int32, Constant: 1) (Syntax: 'For i As In ... Next')
  Body: IBlockStatement (1 statements) (OperationKind.BlockStatement) (Syntax: 'For i As In ... Next')
      IIfStatement (OperationKind.IfStatement) (Syntax: 'If i Mod 2  ... End If')
        Condition: IBinaryOperatorExpression (BinaryOperationKind.IntegerEquals) (OperationKind.BinaryOperatorExpression, Type: System.Boolean) (Syntax: 'i Mod 2 = 0')
            Left: IBinaryOperatorExpression (BinaryOperationKind.IntegerRemainder) (OperationKind.BinaryOperatorExpression, Type: System.Int32) (Syntax: 'i Mod 2')
                Left: ILocalReferenceExpression: i (OperationKind.LocalReferenceExpression, Type: System.Int32) (Syntax: 'i')
                Right: ILiteralExpression (Text: 2) (OperationKind.LiteralExpression, Type: System.Int32, Constant: 2) (Syntax: '2')
            Right: ILiteralExpression (Text: 0) (OperationKind.LiteralExpression, Type: System.Int32, Constant: 0) (Syntax: '0')
        IfTrue: IBlockStatement (1 statements) (OperationKind.BlockStatement) (Syntax: 'If i Mod 2  ... End If')
            IBranchStatement (BranchKind.Continue, Label: continue) (OperationKind.BranchStatement) (Syntax: 'Continue For')
        IfFalse: null
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
IForLoopStatement (LoopKind.For) (OperationKind.LoopStatement) (Syntax: 'For X = 10  ... Next')
  Condition: IBinaryOperatorExpression (BinaryOperationKind.IntegerLessThanOrEqual) (OperationKind.BinaryOperatorExpression, Type: System.Boolean) (Syntax: '0')
      Left: IFieldReferenceExpression: M.X As System.Int32 (Static) (OperationKind.FieldReferenceExpression, Type: System.Int32) (Syntax: 'X')
          Instance Receiver: null
      Right: ILiteralExpression (Text: 0) (OperationKind.LiteralExpression, Type: System.Int32, Constant: 0) (Syntax: '0')
  Before:
      IExpressionStatement (OperationKind.ExpressionStatement) (Syntax: '10')
        Expression: ISimpleAssignmentExpression (OperationKind.SimpleAssignmentExpression, Type: System.Int32) (Syntax: '10')
            Left: IFieldReferenceExpression: M.X As System.Int32 (Static) (OperationKind.FieldReferenceExpression, Type: System.Int32) (Syntax: 'X')
                Instance Receiver: null
            Right: ILiteralExpression (Text: 10) (OperationKind.LiteralExpression, Type: System.Int32, Constant: 10) (Syntax: '10')
  AtLoopBottom:
      IExpressionStatement (OperationKind.ExpressionStatement) (Syntax: 'For X = 10  ... Next')
        Expression: ICompoundAssignmentExpression (BinaryOperationKind.IntegerAdd) (OperationKind.CompoundAssignmentExpression, Type: System.Int32) (Syntax: 'For X = 10  ... Next')
            Left: IFieldReferenceExpression: M.X As System.Int32 (Static) (OperationKind.FieldReferenceExpression, Type: System.Int32) (Syntax: 'X')
                Instance Receiver: null
            Right: IConversionExpression (Explicit, TryCast: False, Unchecked) (OperationKind.ConversionExpression, Type: System.Int32, Constant: 1) (Syntax: 'For X = 10  ... Next')
                Conversion: CommonConversion (Exists: False, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                Operand: ILiteralExpression (OperationKind.LiteralExpression, Type: System.Int32, Constant: 1) (Syntax: 'For X = 10  ... Next')
  Body: IBlockStatement (0 statements) (OperationKind.BlockStatement) (Syntax: 'For X = 10  ... Next')
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
IForLoopStatement (LoopKind.For) (OperationKind.LoopStatement, IsInvalid) (Syntax: 'For X = 10  ... Next')
  Condition: IBinaryOperatorExpression (BinaryOperationKind.Invalid) (OperationKind.BinaryOperatorExpression, Type: System.Boolean) (Syntax: '0')
      Left: IFieldReferenceExpression: M.X As System.String (Static) (OperationKind.FieldReferenceExpression, Type: System.String, IsInvalid) (Syntax: 'X')
          Instance Receiver: null
      Right: ILiteralExpression (Text: 0) (OperationKind.LiteralExpression, Type: System.Int32, Constant: 0) (Syntax: '0')
  Before:
      IExpressionStatement (OperationKind.ExpressionStatement) (Syntax: '10')
        Expression: ISimpleAssignmentExpression (OperationKind.SimpleAssignmentExpression, Type: System.String) (Syntax: '10')
            Left: IFieldReferenceExpression: M.X As System.String (Static) (OperationKind.FieldReferenceExpression, Type: System.String, IsInvalid) (Syntax: 'X')
                Instance Receiver: null
            Right: ILiteralExpression (Text: 10) (OperationKind.LiteralExpression, Type: System.Int32, Constant: 10) (Syntax: '10')
  AtLoopBottom:
      IExpressionStatement (OperationKind.ExpressionStatement, IsInvalid) (Syntax: 'For X = 10  ... Next')
        Expression: ICompoundAssignmentExpression (BinaryOperationKind.Invalid) (OperationKind.CompoundAssignmentExpression, Type: System.String, IsInvalid) (Syntax: 'For X = 10  ... Next')
            Left: IFieldReferenceExpression: M.X As System.String (Static) (OperationKind.FieldReferenceExpression, Type: System.String, IsInvalid) (Syntax: 'X')
                Instance Receiver: null
            Right: ILiteralExpression (OperationKind.LiteralExpression, Type: System.Int32, Constant: 1, IsInvalid) (Syntax: 'For X = 10  ... Next')
  Body: IBlockStatement (0 statements) (OperationKind.BlockStatement, IsInvalid) (Syntax: 'For X = 10  ... Next')
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
IForLoopStatement (LoopKind.For) (OperationKind.LoopStatement) (Syntax: 'For A = 1 T ... Next B, A')
  Condition: IBinaryOperatorExpression (BinaryOperationKind.IntegerLessThanOrEqual) (OperationKind.BinaryOperatorExpression, Type: System.Boolean) (Syntax: '2')
      Left: ILocalReferenceExpression: A (OperationKind.LocalReferenceExpression, Type: System.Int32) (Syntax: 'A')
      Right: ILiteralExpression (Text: 2) (OperationKind.LiteralExpression, Type: System.Int32, Constant: 2) (Syntax: '2')
  Before:
      IExpressionStatement (OperationKind.ExpressionStatement) (Syntax: '1')
        Expression: ISimpleAssignmentExpression (OperationKind.SimpleAssignmentExpression, Type: System.Int32) (Syntax: '1')
            Left: ILocalReferenceExpression: A (OperationKind.LocalReferenceExpression, Type: System.Int32) (Syntax: 'A')
            Right: ILiteralExpression (Text: 1) (OperationKind.LiteralExpression, Type: System.Int32, Constant: 1) (Syntax: '1')
  AtLoopBottom:
      IExpressionStatement (OperationKind.ExpressionStatement) (Syntax: 'For A = 1 T ... Next B, A')
        Expression: ICompoundAssignmentExpression (BinaryOperationKind.IntegerAdd) (OperationKind.CompoundAssignmentExpression, Type: System.Int32) (Syntax: 'For A = 1 T ... Next B, A')
            Left: ILocalReferenceExpression: A (OperationKind.LocalReferenceExpression, Type: System.Int32) (Syntax: 'A')
            Right: IConversionExpression (Explicit, TryCast: False, Unchecked) (OperationKind.ConversionExpression, Type: System.Int32, Constant: 1) (Syntax: 'For A = 1 T ... Next B, A')
                Conversion: CommonConversion (Exists: False, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                Operand: ILiteralExpression (OperationKind.LiteralExpression, Type: System.Int32, Constant: 1) (Syntax: 'For A = 1 T ... Next B, A')
  Body: IBlockStatement (1 statements) (OperationKind.BlockStatement) (Syntax: 'For A = 1 T ... Next B, A')
      IForLoopStatement (LoopKind.For) (OperationKind.LoopStatement) (Syntax: 'For B = A T ... Next B, A')
        Condition: IBinaryOperatorExpression (BinaryOperationKind.IntegerLessThanOrEqual) (OperationKind.BinaryOperatorExpression, Type: System.Boolean) (Syntax: '2')
            Left: ILocalReferenceExpression: B (OperationKind.LocalReferenceExpression, Type: System.Int32) (Syntax: 'B')
            Right: ILiteralExpression (Text: 2) (OperationKind.LiteralExpression, Type: System.Int32, Constant: 2) (Syntax: '2')
        Before:
            IExpressionStatement (OperationKind.ExpressionStatement) (Syntax: 'A')
              Expression: ISimpleAssignmentExpression (OperationKind.SimpleAssignmentExpression, Type: System.Int32) (Syntax: 'A')
                  Left: ILocalReferenceExpression: B (OperationKind.LocalReferenceExpression, Type: System.Int32) (Syntax: 'B')
                  Right: ILocalReferenceExpression: A (OperationKind.LocalReferenceExpression, Type: System.Int32) (Syntax: 'A')
        AtLoopBottom:
            IExpressionStatement (OperationKind.ExpressionStatement) (Syntax: 'For B = A T ... Next B, A')
              Expression: ICompoundAssignmentExpression (BinaryOperationKind.IntegerAdd) (OperationKind.CompoundAssignmentExpression, Type: System.Int32) (Syntax: 'For B = A T ... Next B, A')
                  Left: ILocalReferenceExpression: B (OperationKind.LocalReferenceExpression, Type: System.Int32) (Syntax: 'B')
                  Right: IConversionExpression (Explicit, TryCast: False, Unchecked) (OperationKind.ConversionExpression, Type: System.Int32, Constant: 1) (Syntax: 'For B = A T ... Next B, A')
                      Conversion: CommonConversion (Exists: False, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                      Operand: ILiteralExpression (OperationKind.LiteralExpression, Type: System.Int32, Constant: 1) (Syntax: 'For B = A T ... Next B, A')
        Body: IBlockStatement (0 statements) (OperationKind.BlockStatement) (Syntax: 'For B = A T ... Next B, A')
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
IForLoopStatement (LoopKind.For) (OperationKind.LoopStatement) (Syntax: 'For x = 12  ... Next')
  Condition: IBinaryOperatorExpression (BinaryOperationKind.IntegerLessThanOrEqual) (OperationKind.BinaryOperatorExpression, Type: System.Boolean) (Syntax: 'y')
      Left: ILocalReferenceExpression: x (OperationKind.LocalReferenceExpression, Type: System.Int32) (Syntax: 'x')
      Right: ISyntheticLocalReferenceExpression (SynthesizedLocalKind.ForLoopLimitValue) (OperationKind.SyntheticLocalReferenceExpression, Type: System.Int32) (Syntax: 'y')
  Before:
      IExpressionStatement (OperationKind.ExpressionStatement) (Syntax: '12')
        Expression: ISimpleAssignmentExpression (OperationKind.SimpleAssignmentExpression, Type: System.Int32) (Syntax: '12')
            Left: ILocalReferenceExpression: x (OperationKind.LocalReferenceExpression, Type: System.Int32) (Syntax: 'x')
            Right: ILiteralExpression (Text: 12) (OperationKind.LiteralExpression, Type: System.Int32, Constant: 12) (Syntax: '12')
      IExpressionStatement (OperationKind.ExpressionStatement) (Syntax: 'y')
        Expression: ISimpleAssignmentExpression (OperationKind.SimpleAssignmentExpression, Type: System.Int32) (Syntax: 'y')
            Left: ISyntheticLocalReferenceExpression (SynthesizedLocalKind.ForLoopLimitValue) (OperationKind.SyntheticLocalReferenceExpression, Type: System.Int32) (Syntax: 'y')
            Right: ILocalReferenceExpression: y (OperationKind.LocalReferenceExpression, Type: System.Int32) (Syntax: 'y')
  AtLoopBottom:
      IExpressionStatement (OperationKind.ExpressionStatement) (Syntax: 'For x = 12  ... Next')
        Expression: ICompoundAssignmentExpression (BinaryOperationKind.IntegerAdd) (OperationKind.CompoundAssignmentExpression, Type: System.Int32) (Syntax: 'For x = 12  ... Next')
            Left: ILocalReferenceExpression: x (OperationKind.LocalReferenceExpression, Type: System.Int32) (Syntax: 'x')
            Right: IConversionExpression (Explicit, TryCast: False, Unchecked) (OperationKind.ConversionExpression, Type: System.Int32, Constant: 1) (Syntax: 'For x = 12  ... Next')
                Conversion: CommonConversion (Exists: False, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                Operand: ILiteralExpression (OperationKind.LiteralExpression, Type: System.Int32, Constant: 1) (Syntax: 'For x = 12  ... Next')
  Body: IBlockStatement (0 statements) (OperationKind.BlockStatement) (Syntax: 'For x = 12  ... Next')
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
IForLoopStatement (LoopKind.For) (OperationKind.LoopStatement) (Syntax: 'For x = 12  ... Next')
  Condition: IConditionalChoiceExpression (OperationKind.ConditionalChoiceExpression, Type: System.Boolean) (Syntax: 'y')
      Condition: IBinaryOperatorExpression (BinaryOperationKind.IntegerGreaterThanOrEqual-IsLifted) (OperationKind.BinaryOperatorExpression, Type: System.Boolean) (Syntax: 'For x = 12  ... Next')
          Left: ISyntheticLocalReferenceExpression (SynthesizedLocalKind.ForLoopStepValue) (OperationKind.SyntheticLocalReferenceExpression, Type: System.Nullable(Of System.Int32)) (Syntax: 'For x = 12  ... Next')
          Right: ILiteralExpression (OperationKind.LiteralExpression, Type: System.Nullable(Of System.Int32), Constant: null) (Syntax: 'For x = 12  ... Next')
      IfTrue: IBinaryOperatorExpression (BinaryOperationKind.IntegerLessThanOrEqual-IsLifted) (OperationKind.BinaryOperatorExpression, Type: System.Boolean) (Syntax: 'y')
          Left: ILocalReferenceExpression: x (OperationKind.LocalReferenceExpression, Type: System.Nullable(Of System.Int32)) (Syntax: 'x')
          Right: ISyntheticLocalReferenceExpression (SynthesizedLocalKind.ForLoopLimitValue) (OperationKind.SyntheticLocalReferenceExpression, Type: System.Nullable(Of System.Int32)) (Syntax: 'y')
      IfFalse: IBinaryOperatorExpression (BinaryOperationKind.IntegerGreaterThanOrEqual-IsLifted) (OperationKind.BinaryOperatorExpression, Type: System.Boolean) (Syntax: 'y')
          Left: ILocalReferenceExpression: x (OperationKind.LocalReferenceExpression, Type: System.Nullable(Of System.Int32)) (Syntax: 'x')
          Right: ISyntheticLocalReferenceExpression (SynthesizedLocalKind.ForLoopLimitValue) (OperationKind.SyntheticLocalReferenceExpression, Type: System.Nullable(Of System.Int32)) (Syntax: 'y')
  Before:
      IExpressionStatement (OperationKind.ExpressionStatement) (Syntax: '12')
        Expression: ISimpleAssignmentExpression (OperationKind.SimpleAssignmentExpression, Type: System.Nullable(Of System.Int32)) (Syntax: '12')
            Left: ILocalReferenceExpression: x (OperationKind.LocalReferenceExpression, Type: System.Nullable(Of System.Int32)) (Syntax: 'x')
            Right: IConversionExpression (Implicit, TryCast: False, Unchecked) (OperationKind.ConversionExpression, Type: System.Nullable(Of System.Int32)) (Syntax: '12')
                Conversion: CommonConversion (Exists: False, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                Operand: ILiteralExpression (Text: 12) (OperationKind.LiteralExpression, Type: System.Int32, Constant: 12) (Syntax: '12')
      IExpressionStatement (OperationKind.ExpressionStatement) (Syntax: 'y')
        Expression: ISimpleAssignmentExpression (OperationKind.SimpleAssignmentExpression, Type: System.Nullable(Of System.Int32)) (Syntax: 'y')
            Left: ISyntheticLocalReferenceExpression (SynthesizedLocalKind.ForLoopLimitValue) (OperationKind.SyntheticLocalReferenceExpression, Type: System.Nullable(Of System.Int32)) (Syntax: 'y')
            Right: IConversionExpression (Implicit, TryCast: False, Unchecked) (OperationKind.ConversionExpression, Type: System.Nullable(Of System.Int32)) (Syntax: 'y')
                Conversion: CommonConversion (Exists: False, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                Operand: ILocalReferenceExpression: y (OperationKind.LocalReferenceExpression, Type: System.Int32) (Syntax: 'y')
      IExpressionStatement (OperationKind.ExpressionStatement) (Syntax: 'For x = 12  ... Next')
        Expression: ISimpleAssignmentExpression (OperationKind.SimpleAssignmentExpression, Type: System.Nullable(Of System.Int32)) (Syntax: 'For x = 12  ... Next')
            Left: ISyntheticLocalReferenceExpression (SynthesizedLocalKind.ForLoopStepValue) (OperationKind.SyntheticLocalReferenceExpression, Type: System.Nullable(Of System.Int32)) (Syntax: 'For x = 12  ... Next')
            Right: IConversionExpression (Explicit, TryCast: False, Unchecked) (OperationKind.ConversionExpression, Type: System.Nullable(Of System.Int32)) (Syntax: 'For x = 12  ... Next')
                Conversion: CommonConversion (Exists: False, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                Operand: ILiteralExpression (OperationKind.LiteralExpression, Type: System.Int32, Constant: 1) (Syntax: 'For x = 12  ... Next')
  AtLoopBottom:
      IExpressionStatement (OperationKind.ExpressionStatement) (Syntax: 'For x = 12  ... Next')
        Expression: ICompoundAssignmentExpression (BinaryOperationKind.IntegerAdd-IsLifted) (OperationKind.CompoundAssignmentExpression, Type: System.Nullable(Of System.Int32)) (Syntax: 'For x = 12  ... Next')
            Left: ILocalReferenceExpression: x (OperationKind.LocalReferenceExpression, Type: System.Nullable(Of System.Int32)) (Syntax: 'x')
            Right: ISyntheticLocalReferenceExpression (SynthesizedLocalKind.ForLoopStepValue) (OperationKind.SyntheticLocalReferenceExpression, Type: System.Nullable(Of System.Int32)) (Syntax: 'For x = 12  ... Next')
  Body: IBlockStatement (0 statements) (OperationKind.BlockStatement) (Syntax: 'For x = 12  ... Next')
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
IForLoopStatement (LoopKind.For) (OperationKind.LoopStatement) (Syntax: 'For x = 12  ... Next')
  Condition: IBinaryOperatorExpression (BinaryOperationKind.IntegerLessThanOrEqual) (OperationKind.BinaryOperatorExpression, Type: System.Boolean) (Syntax: 'y')
      Left: ILocalReferenceExpression: x (OperationKind.LocalReferenceExpression, Type: System.Int32) (Syntax: 'x')
      Right: ISyntheticLocalReferenceExpression (SynthesizedLocalKind.ForLoopLimitValue) (OperationKind.SyntheticLocalReferenceExpression, Type: System.Int32) (Syntax: 'y')
  Before:
      IExpressionStatement (OperationKind.ExpressionStatement) (Syntax: '12')
        Expression: ISimpleAssignmentExpression (OperationKind.SimpleAssignmentExpression, Type: System.Int32) (Syntax: '12')
            Left: ILocalReferenceExpression: x (OperationKind.LocalReferenceExpression, Type: System.Int32) (Syntax: 'x')
            Right: ILiteralExpression (Text: 12) (OperationKind.LiteralExpression, Type: System.Int32, Constant: 12) (Syntax: '12')
      IExpressionStatement (OperationKind.ExpressionStatement) (Syntax: 'y')
        Expression: ISimpleAssignmentExpression (OperationKind.SimpleAssignmentExpression, Type: System.Int32) (Syntax: 'y')
            Left: ISyntheticLocalReferenceExpression (SynthesizedLocalKind.ForLoopLimitValue) (OperationKind.SyntheticLocalReferenceExpression, Type: System.Int32) (Syntax: 'y')
            Right: IConversionExpression (Implicit, TryCast: False, Unchecked) (OperationKind.ConversionExpression, Type: System.Int32) (Syntax: 'y')
                Conversion: CommonConversion (Exists: False, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                Operand: ILocalReferenceExpression: y (OperationKind.LocalReferenceExpression, Type: System.Nullable(Of System.Int32)) (Syntax: 'y')
  AtLoopBottom:
      IExpressionStatement (OperationKind.ExpressionStatement) (Syntax: 'For x = 12  ... Next')
        Expression: ICompoundAssignmentExpression (BinaryOperationKind.IntegerAdd) (OperationKind.CompoundAssignmentExpression, Type: System.Int32) (Syntax: 'For x = 12  ... Next')
            Left: ILocalReferenceExpression: x (OperationKind.LocalReferenceExpression, Type: System.Int32) (Syntax: 'x')
            Right: IConversionExpression (Explicit, TryCast: False, Unchecked) (OperationKind.ConversionExpression, Type: System.Int32, Constant: 1) (Syntax: 'For x = 12  ... Next')
                Conversion: CommonConversion (Exists: False, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                Operand: ILiteralExpression (OperationKind.LiteralExpression, Type: System.Int32, Constant: 1) (Syntax: 'For x = 12  ... Next')
  Body: IBlockStatement (0 statements) (OperationKind.BlockStatement) (Syntax: 'For x = 12  ... Next')
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
IForLoopStatement (LoopKind.For) (OperationKind.LoopStatement) (Syntax: 'For x = 12  ... Next')
  Condition: IConditionalChoiceExpression (OperationKind.ConditionalChoiceExpression, Type: System.Boolean) (Syntax: 'y')
      Condition: IBinaryOperatorExpression (BinaryOperationKind.IntegerGreaterThanOrEqual-IsLifted) (OperationKind.BinaryOperatorExpression, Type: System.Boolean) (Syntax: 'For x = 12  ... Next')
          Left: ISyntheticLocalReferenceExpression (SynthesizedLocalKind.ForLoopStepValue) (OperationKind.SyntheticLocalReferenceExpression, Type: System.Nullable(Of System.Int32)) (Syntax: 'For x = 12  ... Next')
          Right: ILiteralExpression (OperationKind.LiteralExpression, Type: System.Nullable(Of System.Int32), Constant: null) (Syntax: 'For x = 12  ... Next')
      IfTrue: IBinaryOperatorExpression (BinaryOperationKind.IntegerLessThanOrEqual-IsLifted) (OperationKind.BinaryOperatorExpression, Type: System.Boolean) (Syntax: 'y')
          Left: ILocalReferenceExpression: x (OperationKind.LocalReferenceExpression, Type: System.Nullable(Of System.Int32)) (Syntax: 'x')
          Right: ISyntheticLocalReferenceExpression (SynthesizedLocalKind.ForLoopLimitValue) (OperationKind.SyntheticLocalReferenceExpression, Type: System.Nullable(Of System.Int32)) (Syntax: 'y')
      IfFalse: IBinaryOperatorExpression (BinaryOperationKind.IntegerGreaterThanOrEqual-IsLifted) (OperationKind.BinaryOperatorExpression, Type: System.Boolean) (Syntax: 'y')
          Left: ILocalReferenceExpression: x (OperationKind.LocalReferenceExpression, Type: System.Nullable(Of System.Int32)) (Syntax: 'x')
          Right: ISyntheticLocalReferenceExpression (SynthesizedLocalKind.ForLoopLimitValue) (OperationKind.SyntheticLocalReferenceExpression, Type: System.Nullable(Of System.Int32)) (Syntax: 'y')
  Before:
      IExpressionStatement (OperationKind.ExpressionStatement) (Syntax: '12')
        Expression: ISimpleAssignmentExpression (OperationKind.SimpleAssignmentExpression, Type: System.Nullable(Of System.Int32)) (Syntax: '12')
            Left: ILocalReferenceExpression: x (OperationKind.LocalReferenceExpression, Type: System.Nullable(Of System.Int32)) (Syntax: 'x')
            Right: IConversionExpression (Implicit, TryCast: False, Unchecked) (OperationKind.ConversionExpression, Type: System.Nullable(Of System.Int32)) (Syntax: '12')
                Conversion: CommonConversion (Exists: False, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                Operand: ILiteralExpression (Text: 12) (OperationKind.LiteralExpression, Type: System.Int32, Constant: 12) (Syntax: '12')
      IExpressionStatement (OperationKind.ExpressionStatement) (Syntax: 'y')
        Expression: ISimpleAssignmentExpression (OperationKind.SimpleAssignmentExpression, Type: System.Nullable(Of System.Int32)) (Syntax: 'y')
            Left: ISyntheticLocalReferenceExpression (SynthesizedLocalKind.ForLoopLimitValue) (OperationKind.SyntheticLocalReferenceExpression, Type: System.Nullable(Of System.Int32)) (Syntax: 'y')
            Right: ILocalReferenceExpression: y (OperationKind.LocalReferenceExpression, Type: System.Nullable(Of System.Int32)) (Syntax: 'y')
      IExpressionStatement (OperationKind.ExpressionStatement) (Syntax: 'For x = 12  ... Next')
        Expression: ISimpleAssignmentExpression (OperationKind.SimpleAssignmentExpression, Type: System.Nullable(Of System.Int32)) (Syntax: 'For x = 12  ... Next')
            Left: ISyntheticLocalReferenceExpression (SynthesizedLocalKind.ForLoopStepValue) (OperationKind.SyntheticLocalReferenceExpression, Type: System.Nullable(Of System.Int32)) (Syntax: 'For x = 12  ... Next')
            Right: IConversionExpression (Explicit, TryCast: False, Unchecked) (OperationKind.ConversionExpression, Type: System.Nullable(Of System.Int32)) (Syntax: 'For x = 12  ... Next')
                Conversion: CommonConversion (Exists: False, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                Operand: ILiteralExpression (OperationKind.LiteralExpression, Type: System.Int32, Constant: 1) (Syntax: 'For x = 12  ... Next')
  AtLoopBottom:
      IExpressionStatement (OperationKind.ExpressionStatement) (Syntax: 'For x = 12  ... Next')
        Expression: ICompoundAssignmentExpression (BinaryOperationKind.IntegerAdd-IsLifted) (OperationKind.CompoundAssignmentExpression, Type: System.Nullable(Of System.Int32)) (Syntax: 'For x = 12  ... Next')
            Left: ILocalReferenceExpression: x (OperationKind.LocalReferenceExpression, Type: System.Nullable(Of System.Int32)) (Syntax: 'x')
            Right: ISyntheticLocalReferenceExpression (SynthesizedLocalKind.ForLoopStepValue) (OperationKind.SyntheticLocalReferenceExpression, Type: System.Nullable(Of System.Int32)) (Syntax: 'For x = 12  ... Next')
  Body: IBlockStatement (0 statements) (OperationKind.BlockStatement) (Syntax: 'For x = 12  ... Next')
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
IForLoopStatement (LoopKind.For) (OperationKind.LoopStatement) (Syntax: 'For x = 12  ... Next')
  Condition: IConditionalChoiceExpression (OperationKind.ConditionalChoiceExpression, Type: System.Boolean) (Syntax: 'y')
      Condition: IBinaryOperatorExpression (BinaryOperationKind.IntegerGreaterThanOrEqual) (OperationKind.BinaryOperatorExpression, Type: System.Boolean) (Syntax: 's')
          Left: ISyntheticLocalReferenceExpression (SynthesizedLocalKind.ForLoopStepValue) (OperationKind.SyntheticLocalReferenceExpression, Type: System.Int32) (Syntax: 's')
          Right: ILiteralExpression (Text: 0) (OperationKind.LiteralExpression, Type: System.Int32, Constant: 0) (Syntax: 's')
      IfTrue: IBinaryOperatorExpression (BinaryOperationKind.IntegerLessThanOrEqual) (OperationKind.BinaryOperatorExpression, Type: System.Boolean) (Syntax: 'y')
          Left: ILocalReferenceExpression: x (OperationKind.LocalReferenceExpression, Type: System.Int32) (Syntax: 'x')
          Right: ISyntheticLocalReferenceExpression (SynthesizedLocalKind.ForLoopLimitValue) (OperationKind.SyntheticLocalReferenceExpression, Type: System.Int32) (Syntax: 'y')
      IfFalse: IBinaryOperatorExpression (BinaryOperationKind.IntegerGreaterThanOrEqual) (OperationKind.BinaryOperatorExpression, Type: System.Boolean) (Syntax: 'y')
          Left: ILocalReferenceExpression: x (OperationKind.LocalReferenceExpression, Type: System.Int32) (Syntax: 'x')
          Right: ISyntheticLocalReferenceExpression (SynthesizedLocalKind.ForLoopLimitValue) (OperationKind.SyntheticLocalReferenceExpression, Type: System.Int32) (Syntax: 'y')
  Before:
      IExpressionStatement (OperationKind.ExpressionStatement) (Syntax: '12')
        Expression: ISimpleAssignmentExpression (OperationKind.SimpleAssignmentExpression, Type: System.Int32) (Syntax: '12')
            Left: ILocalReferenceExpression: x (OperationKind.LocalReferenceExpression, Type: System.Int32) (Syntax: 'x')
            Right: ILiteralExpression (Text: 12) (OperationKind.LiteralExpression, Type: System.Int32, Constant: 12) (Syntax: '12')
      IExpressionStatement (OperationKind.ExpressionStatement) (Syntax: 'y')
        Expression: ISimpleAssignmentExpression (OperationKind.SimpleAssignmentExpression, Type: System.Int32) (Syntax: 'y')
            Left: ISyntheticLocalReferenceExpression (SynthesizedLocalKind.ForLoopLimitValue) (OperationKind.SyntheticLocalReferenceExpression, Type: System.Int32) (Syntax: 'y')
            Right: ILocalReferenceExpression: y (OperationKind.LocalReferenceExpression, Type: System.Int32) (Syntax: 'y')
      IExpressionStatement (OperationKind.ExpressionStatement) (Syntax: 's')
        Expression: ISimpleAssignmentExpression (OperationKind.SimpleAssignmentExpression, Type: System.Int32) (Syntax: 's')
            Left: ISyntheticLocalReferenceExpression (SynthesizedLocalKind.ForLoopStepValue) (OperationKind.SyntheticLocalReferenceExpression, Type: System.Int32) (Syntax: 's')
            Right: IConversionExpression (Implicit, TryCast: False, Unchecked) (OperationKind.ConversionExpression, Type: System.Int32) (Syntax: 's')
                Conversion: CommonConversion (Exists: False, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                Operand: ILocalReferenceExpression: s (OperationKind.LocalReferenceExpression, Type: System.Nullable(Of System.Int32)) (Syntax: 's')
  AtLoopBottom:
      IExpressionStatement (OperationKind.ExpressionStatement) (Syntax: 's')
        Expression: ICompoundAssignmentExpression (BinaryOperationKind.IntegerAdd) (OperationKind.CompoundAssignmentExpression, Type: System.Int32) (Syntax: 's')
            Left: ILocalReferenceExpression: x (OperationKind.LocalReferenceExpression, Type: System.Int32) (Syntax: 'x')
            Right: ISyntheticLocalReferenceExpression (SynthesizedLocalKind.ForLoopStepValue) (OperationKind.SyntheticLocalReferenceExpression, Type: System.Int32) (Syntax: 's')
  Body: IBlockStatement (0 statements) (OperationKind.BlockStatement) (Syntax: 'For x = 12  ... Next')
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
IForLoopStatement (LoopKind.For) (OperationKind.LoopStatement) (Syntax: 'For x = 12  ... Next')
  Condition: IConditionalChoiceExpression (OperationKind.ConditionalChoiceExpression, Type: System.Boolean) (Syntax: 'y')
      Condition: IBinaryOperatorExpression (BinaryOperationKind.IntegerGreaterThanOrEqual-IsLifted) (OperationKind.BinaryOperatorExpression, Type: System.Boolean) (Syntax: 's')
          Left: ISyntheticLocalReferenceExpression (SynthesizedLocalKind.ForLoopStepValue) (OperationKind.SyntheticLocalReferenceExpression, Type: System.Nullable(Of System.Int32)) (Syntax: 's')
          Right: ILiteralExpression (OperationKind.LiteralExpression, Type: System.Nullable(Of System.Int32), Constant: null) (Syntax: 's')
      IfTrue: IBinaryOperatorExpression (BinaryOperationKind.IntegerLessThanOrEqual-IsLifted) (OperationKind.BinaryOperatorExpression, Type: System.Boolean) (Syntax: 'y')
          Left: ILocalReferenceExpression: x (OperationKind.LocalReferenceExpression, Type: System.Nullable(Of System.Int32)) (Syntax: 'x')
          Right: ISyntheticLocalReferenceExpression (SynthesizedLocalKind.ForLoopLimitValue) (OperationKind.SyntheticLocalReferenceExpression, Type: System.Nullable(Of System.Int32)) (Syntax: 'y')
      IfFalse: IBinaryOperatorExpression (BinaryOperationKind.IntegerGreaterThanOrEqual-IsLifted) (OperationKind.BinaryOperatorExpression, Type: System.Boolean) (Syntax: 'y')
          Left: ILocalReferenceExpression: x (OperationKind.LocalReferenceExpression, Type: System.Nullable(Of System.Int32)) (Syntax: 'x')
          Right: ISyntheticLocalReferenceExpression (SynthesizedLocalKind.ForLoopLimitValue) (OperationKind.SyntheticLocalReferenceExpression, Type: System.Nullable(Of System.Int32)) (Syntax: 'y')
  Before:
      IExpressionStatement (OperationKind.ExpressionStatement) (Syntax: '12')
        Expression: ISimpleAssignmentExpression (OperationKind.SimpleAssignmentExpression, Type: System.Nullable(Of System.Int32)) (Syntax: '12')
            Left: ILocalReferenceExpression: x (OperationKind.LocalReferenceExpression, Type: System.Nullable(Of System.Int32)) (Syntax: 'x')
            Right: IConversionExpression (Implicit, TryCast: False, Unchecked) (OperationKind.ConversionExpression, Type: System.Nullable(Of System.Int32)) (Syntax: '12')
                Conversion: CommonConversion (Exists: False, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                Operand: ILiteralExpression (Text: 12) (OperationKind.LiteralExpression, Type: System.Int32, Constant: 12) (Syntax: '12')
      IExpressionStatement (OperationKind.ExpressionStatement) (Syntax: 'y')
        Expression: ISimpleAssignmentExpression (OperationKind.SimpleAssignmentExpression, Type: System.Nullable(Of System.Int32)) (Syntax: 'y')
            Left: ISyntheticLocalReferenceExpression (SynthesizedLocalKind.ForLoopLimitValue) (OperationKind.SyntheticLocalReferenceExpression, Type: System.Nullable(Of System.Int32)) (Syntax: 'y')
            Right: IConversionExpression (Implicit, TryCast: False, Unchecked) (OperationKind.ConversionExpression, Type: System.Nullable(Of System.Int32)) (Syntax: 'y')
                Conversion: CommonConversion (Exists: False, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                Operand: ILocalReferenceExpression: y (OperationKind.LocalReferenceExpression, Type: System.Int32) (Syntax: 'y')
      IExpressionStatement (OperationKind.ExpressionStatement) (Syntax: 's')
        Expression: ISimpleAssignmentExpression (OperationKind.SimpleAssignmentExpression, Type: System.Nullable(Of System.Int32)) (Syntax: 's')
            Left: ISyntheticLocalReferenceExpression (SynthesizedLocalKind.ForLoopStepValue) (OperationKind.SyntheticLocalReferenceExpression, Type: System.Nullable(Of System.Int32)) (Syntax: 's')
            Right: ILocalReferenceExpression: s (OperationKind.LocalReferenceExpression, Type: System.Nullable(Of System.Int32)) (Syntax: 's')
  AtLoopBottom:
      IExpressionStatement (OperationKind.ExpressionStatement) (Syntax: 's')
        Expression: ICompoundAssignmentExpression (BinaryOperationKind.IntegerAdd-IsLifted) (OperationKind.CompoundAssignmentExpression, Type: System.Nullable(Of System.Int32)) (Syntax: 's')
            Left: ILocalReferenceExpression: x (OperationKind.LocalReferenceExpression, Type: System.Nullable(Of System.Int32)) (Syntax: 'x')
            Right: ISyntheticLocalReferenceExpression (SynthesizedLocalKind.ForLoopStepValue) (OperationKind.SyntheticLocalReferenceExpression, Type: System.Nullable(Of System.Int32)) (Syntax: 's')
  Body: IBlockStatement (0 statements) (OperationKind.BlockStatement) (Syntax: 'For x = 12  ... Next')
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
IForLoopStatement (LoopKind.For) (OperationKind.LoopStatement) (Syntax: 'For x = 12  ... Next')
  Condition: IConditionalChoiceExpression (OperationKind.ConditionalChoiceExpression, Type: System.Boolean) (Syntax: 'y')
      Condition: IBinaryOperatorExpression (BinaryOperationKind.IntegerGreaterThanOrEqual) (OperationKind.BinaryOperatorExpression, Type: System.Boolean) (Syntax: 's')
          Left: ISyntheticLocalReferenceExpression (SynthesizedLocalKind.ForLoopStepValue) (OperationKind.SyntheticLocalReferenceExpression, Type: System.Int32) (Syntax: 's')
          Right: ILiteralExpression (Text: 0) (OperationKind.LiteralExpression, Type: System.Int32, Constant: 0) (Syntax: 's')
      IfTrue: IBinaryOperatorExpression (BinaryOperationKind.IntegerLessThanOrEqual) (OperationKind.BinaryOperatorExpression, Type: System.Boolean) (Syntax: 'y')
          Left: ILocalReferenceExpression: x (OperationKind.LocalReferenceExpression, Type: System.Int32) (Syntax: 'x')
          Right: ISyntheticLocalReferenceExpression (SynthesizedLocalKind.ForLoopLimitValue) (OperationKind.SyntheticLocalReferenceExpression, Type: System.Int32) (Syntax: 'y')
      IfFalse: IBinaryOperatorExpression (BinaryOperationKind.IntegerGreaterThanOrEqual) (OperationKind.BinaryOperatorExpression, Type: System.Boolean) (Syntax: 'y')
          Left: ILocalReferenceExpression: x (OperationKind.LocalReferenceExpression, Type: System.Int32) (Syntax: 'x')
          Right: ISyntheticLocalReferenceExpression (SynthesizedLocalKind.ForLoopLimitValue) (OperationKind.SyntheticLocalReferenceExpression, Type: System.Int32) (Syntax: 'y')
  Before:
      IExpressionStatement (OperationKind.ExpressionStatement) (Syntax: '12')
        Expression: ISimpleAssignmentExpression (OperationKind.SimpleAssignmentExpression, Type: System.Int32) (Syntax: '12')
            Left: ILocalReferenceExpression: x (OperationKind.LocalReferenceExpression, Type: System.Int32) (Syntax: 'x')
            Right: ILiteralExpression (Text: 12) (OperationKind.LiteralExpression, Type: System.Int32, Constant: 12) (Syntax: '12')
      IExpressionStatement (OperationKind.ExpressionStatement) (Syntax: 'y')
        Expression: ISimpleAssignmentExpression (OperationKind.SimpleAssignmentExpression, Type: System.Int32) (Syntax: 'y')
            Left: ISyntheticLocalReferenceExpression (SynthesizedLocalKind.ForLoopLimitValue) (OperationKind.SyntheticLocalReferenceExpression, Type: System.Int32) (Syntax: 'y')
            Right: IConversionExpression (Implicit, TryCast: False, Unchecked) (OperationKind.ConversionExpression, Type: System.Int32) (Syntax: 'y')
                Conversion: CommonConversion (Exists: False, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                Operand: ILocalReferenceExpression: y (OperationKind.LocalReferenceExpression, Type: System.Nullable(Of System.Int32)) (Syntax: 'y')
      IExpressionStatement (OperationKind.ExpressionStatement) (Syntax: 's')
        Expression: ISimpleAssignmentExpression (OperationKind.SimpleAssignmentExpression, Type: System.Int32) (Syntax: 's')
            Left: ISyntheticLocalReferenceExpression (SynthesizedLocalKind.ForLoopStepValue) (OperationKind.SyntheticLocalReferenceExpression, Type: System.Int32) (Syntax: 's')
            Right: IConversionExpression (Implicit, TryCast: False, Unchecked) (OperationKind.ConversionExpression, Type: System.Int32) (Syntax: 's')
                Conversion: CommonConversion (Exists: False, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                Operand: ILocalReferenceExpression: s (OperationKind.LocalReferenceExpression, Type: System.Nullable(Of System.Int32)) (Syntax: 's')
  AtLoopBottom:
      IExpressionStatement (OperationKind.ExpressionStatement) (Syntax: 's')
        Expression: ICompoundAssignmentExpression (BinaryOperationKind.IntegerAdd) (OperationKind.CompoundAssignmentExpression, Type: System.Int32) (Syntax: 's')
            Left: ILocalReferenceExpression: x (OperationKind.LocalReferenceExpression, Type: System.Int32) (Syntax: 'x')
            Right: ISyntheticLocalReferenceExpression (SynthesizedLocalKind.ForLoopStepValue) (OperationKind.SyntheticLocalReferenceExpression, Type: System.Int32) (Syntax: 's')
  Body: IBlockStatement (0 statements) (OperationKind.BlockStatement) (Syntax: 'For x = 12  ... Next')
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
IForLoopStatement (LoopKind.For) (OperationKind.LoopStatement) (Syntax: 'For x = 12  ... Next')
  Condition: IConditionalChoiceExpression (OperationKind.ConditionalChoiceExpression, Type: System.Boolean) (Syntax: 'y')
      Condition: IBinaryOperatorExpression (BinaryOperationKind.IntegerGreaterThanOrEqual-IsLifted) (OperationKind.BinaryOperatorExpression, Type: System.Boolean) (Syntax: 's')
          Left: ISyntheticLocalReferenceExpression (SynthesizedLocalKind.ForLoopStepValue) (OperationKind.SyntheticLocalReferenceExpression, Type: System.Nullable(Of System.Int32)) (Syntax: 's')
          Right: ILiteralExpression (OperationKind.LiteralExpression, Type: System.Nullable(Of System.Int32), Constant: null) (Syntax: 's')
      IfTrue: IBinaryOperatorExpression (BinaryOperationKind.IntegerLessThanOrEqual-IsLifted) (OperationKind.BinaryOperatorExpression, Type: System.Boolean) (Syntax: 'y')
          Left: ILocalReferenceExpression: x (OperationKind.LocalReferenceExpression, Type: System.Nullable(Of System.Int32)) (Syntax: 'x')
          Right: ISyntheticLocalReferenceExpression (SynthesizedLocalKind.ForLoopLimitValue) (OperationKind.SyntheticLocalReferenceExpression, Type: System.Nullable(Of System.Int32)) (Syntax: 'y')
      IfFalse: IBinaryOperatorExpression (BinaryOperationKind.IntegerGreaterThanOrEqual-IsLifted) (OperationKind.BinaryOperatorExpression, Type: System.Boolean) (Syntax: 'y')
          Left: ILocalReferenceExpression: x (OperationKind.LocalReferenceExpression, Type: System.Nullable(Of System.Int32)) (Syntax: 'x')
          Right: ISyntheticLocalReferenceExpression (SynthesizedLocalKind.ForLoopLimitValue) (OperationKind.SyntheticLocalReferenceExpression, Type: System.Nullable(Of System.Int32)) (Syntax: 'y')
  Before:
      IExpressionStatement (OperationKind.ExpressionStatement) (Syntax: '12')
        Expression: ISimpleAssignmentExpression (OperationKind.SimpleAssignmentExpression, Type: System.Nullable(Of System.Int32)) (Syntax: '12')
            Left: ILocalReferenceExpression: x (OperationKind.LocalReferenceExpression, Type: System.Nullable(Of System.Int32)) (Syntax: 'x')
            Right: IConversionExpression (Implicit, TryCast: False, Unchecked) (OperationKind.ConversionExpression, Type: System.Nullable(Of System.Int32)) (Syntax: '12')
                Conversion: CommonConversion (Exists: False, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                Operand: ILiteralExpression (Text: 12) (OperationKind.LiteralExpression, Type: System.Int32, Constant: 12) (Syntax: '12')
      IExpressionStatement (OperationKind.ExpressionStatement) (Syntax: 'y')
        Expression: ISimpleAssignmentExpression (OperationKind.SimpleAssignmentExpression, Type: System.Nullable(Of System.Int32)) (Syntax: 'y')
            Left: ISyntheticLocalReferenceExpression (SynthesizedLocalKind.ForLoopLimitValue) (OperationKind.SyntheticLocalReferenceExpression, Type: System.Nullable(Of System.Int32)) (Syntax: 'y')
            Right: ILocalReferenceExpression: y (OperationKind.LocalReferenceExpression, Type: System.Nullable(Of System.Int32)) (Syntax: 'y')
      IExpressionStatement (OperationKind.ExpressionStatement) (Syntax: 's')
        Expression: ISimpleAssignmentExpression (OperationKind.SimpleAssignmentExpression, Type: System.Nullable(Of System.Int32)) (Syntax: 's')
            Left: ISyntheticLocalReferenceExpression (SynthesizedLocalKind.ForLoopStepValue) (OperationKind.SyntheticLocalReferenceExpression, Type: System.Nullable(Of System.Int32)) (Syntax: 's')
            Right: ILocalReferenceExpression: s (OperationKind.LocalReferenceExpression, Type: System.Nullable(Of System.Int32)) (Syntax: 's')
  AtLoopBottom:
      IExpressionStatement (OperationKind.ExpressionStatement) (Syntax: 's')
        Expression: ICompoundAssignmentExpression (BinaryOperationKind.IntegerAdd-IsLifted) (OperationKind.CompoundAssignmentExpression, Type: System.Nullable(Of System.Int32)) (Syntax: 's')
            Left: ILocalReferenceExpression: x (OperationKind.LocalReferenceExpression, Type: System.Nullable(Of System.Int32)) (Syntax: 'x')
            Right: ISyntheticLocalReferenceExpression (SynthesizedLocalKind.ForLoopStepValue) (OperationKind.SyntheticLocalReferenceExpression, Type: System.Nullable(Of System.Int32)) (Syntax: 's')
  Body: IBlockStatement (0 statements) (OperationKind.BlockStatement) (Syntax: 'For x = 12  ... Next')
]]>.Value

            VerifyOperationTreeForTest(Of ForBlockSyntax)(source, expectedOperationTree)
        End Sub
    End Class
End Namespace
