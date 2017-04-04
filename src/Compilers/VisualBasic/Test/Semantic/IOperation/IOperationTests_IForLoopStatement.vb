' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports Microsoft.CodeAnalysis.Semantics
Imports Microsoft.CodeAnalysis.Test.Utilities
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax
Imports Roslyn.Test.Utilities

Namespace Microsoft.CodeAnalysis.VisualBasic.UnitTests.Semantics

    Partial Public Class IOperationTests
        Inherits SemanticModelTestBase
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
IForLoopStatement (LoopKind.For) (OperationKind.LoopStatement)
  Condition: IBinaryOperatorExpression (BinaryOperationKind.IntegerLessThanOrEqual) (OperationKind.BinaryOperatorExpression, Type: System.Boolean)
      Left: ILocalReferenceExpression: i (OperationKind.LocalReferenceExpression, Type: System.Int32)
      Right: ISyntheticLocalReferenceExpression (SynthesizedLocalKind.ForLoopLimitValue) (OperationKind.SyntheticLocalReferenceExpression, Type: System.Int32)
  Before: IExpressionStatement (OperationKind.ExpressionStatement)
      IAssignmentExpression (OperationKind.AssignmentExpression, Type: System.Int32)
        Left: ILocalReferenceExpression: i (OperationKind.LocalReferenceExpression, Type: System.Int32)
        Right: ILiteralExpression (Text: 0) (OperationKind.LiteralExpression, Type: System.Int32, Constant: 0)
    IExpressionStatement (OperationKind.ExpressionStatement)
      IAssignmentExpression (OperationKind.AssignmentExpression, Type: System.Int32)
        Left: ISyntheticLocalReferenceExpression (SynthesizedLocalKind.ForLoopLimitValue) (OperationKind.SyntheticLocalReferenceExpression, Type: System.Int32)
        Right: IBinaryOperatorExpression (BinaryOperationKind.IntegerSubtract) (OperationKind.BinaryOperatorExpression, Type: System.Int32)
            Left: IIndexedPropertyReferenceExpression: ReadOnly Property System.Array.Length As System.Int32 (OperationKind.PropertyReferenceExpression, Type: System.Int32)
               (OperationKind.PropertyReferenceExpression, Type: System.Int32)
                Instance Receiver: ILocalReferenceExpression: myarray (OperationKind.LocalReferenceExpression, Type: System.Int32())
            Right: ILiteralExpression (Text: 1) (OperationKind.LiteralExpression, Type: System.Int32, Constant: 1)
  AtLoopBottom: IExpressionStatement (OperationKind.ExpressionStatement)
      ICompoundAssignmentExpression (BinaryOperationKind.IntegerAdd) (OperationKind.CompoundAssignmentExpression, Type: System.Int32)
        Left: ILocalReferenceExpression: i (OperationKind.LocalReferenceExpression, Type: System.Int32)
        Right: IConversionExpression (ConversionKind.Basic, Explicit) (OperationKind.ConversionExpression, Type: System.Int32, Constant: 1)
            ILiteralExpression (OperationKind.LiteralExpression, Type: System.Int32, Constant: 1)
  IBlockStatement (1 statements) (OperationKind.BlockStatement)
    IExpressionStatement (OperationKind.ExpressionStatement)
      IInvocationExpression (static Sub System.Console.WriteLine(value As System.Int32)) (OperationKind.InvocationExpression, Type: System.Void)
        IArgument (Matching Parameter: value) (OperationKind.Argument)
          IArrayElementReferenceExpression (OperationKind.ArrayElementReferenceExpression, Type: System.Int32)
            ILocalReferenceExpression: myarray (OperationKind.LocalReferenceExpression, Type: System.Int32())
            Indices: ILocalReferenceExpression: i (OperationKind.LocalReferenceExpression, Type: System.Int32)
]]>.Value

            VerifyOperationTreeForTest(Of ForBlockSyntax)(source, expectedOperationTree)
        End Sub

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
IForLoopStatement (LoopKind.For) (OperationKind.LoopStatement)
  Condition: IConditionalChoiceExpression (OperationKind.ConditionalChoiceExpression, Type: System.Boolean)
      Condition: IBinaryOperatorExpression (BinaryOperationKind.IntegerGreaterThanOrEqual) (OperationKind.BinaryOperatorExpression, Type: System.Boolean)
          Left: ISyntheticLocalReferenceExpression (SynthesizedLocalKind.ForLoopStepValue) (OperationKind.SyntheticLocalReferenceExpression, Type: System.Int32)
          Right: ILiteralExpression (Text: 0) (OperationKind.LiteralExpression, Type: System.Int32, Constant: 0)
      IfTrue: IBinaryOperatorExpression (BinaryOperationKind.IntegerLessThanOrEqual) (OperationKind.BinaryOperatorExpression, Type: System.Boolean)
          Left: ILocalReferenceExpression: i (OperationKind.LocalReferenceExpression, Type: System.Int32)
          Right: ISyntheticLocalReferenceExpression (SynthesizedLocalKind.ForLoopLimitValue) (OperationKind.SyntheticLocalReferenceExpression, Type: System.Int32)
      IfFalse: IBinaryOperatorExpression (BinaryOperationKind.IntegerGreaterThanOrEqual) (OperationKind.BinaryOperatorExpression, Type: System.Boolean)
          Left: ILocalReferenceExpression: i (OperationKind.LocalReferenceExpression, Type: System.Int32)
          Right: ISyntheticLocalReferenceExpression (SynthesizedLocalKind.ForLoopLimitValue) (OperationKind.SyntheticLocalReferenceExpression, Type: System.Int32)
  Before: IExpressionStatement (OperationKind.ExpressionStatement)
      IAssignmentExpression (OperationKind.AssignmentExpression, Type: System.Int32)
        Left: ILocalReferenceExpression: i (OperationKind.LocalReferenceExpression, Type: System.Int32)
        Right: ILiteralExpression (Text: 0) (OperationKind.LiteralExpression, Type: System.Int32, Constant: 0)
    IExpressionStatement (OperationKind.ExpressionStatement)
      IAssignmentExpression (OperationKind.AssignmentExpression, Type: System.Int32)
        Left: ISyntheticLocalReferenceExpression (SynthesizedLocalKind.ForLoopLimitValue) (OperationKind.SyntheticLocalReferenceExpression, Type: System.Int32)
        Right: IConversionExpression (ConversionKind.Basic, Implicit) (OperationKind.ConversionExpression, Type: System.Int32)
            ILiteralExpression (OperationKind.LiteralExpression, Type: System.String, Constant: 1)
    IExpressionStatement (OperationKind.ExpressionStatement)
      IAssignmentExpression (OperationKind.AssignmentExpression, Type: System.Int32)
        Left: ISyntheticLocalReferenceExpression (SynthesizedLocalKind.ForLoopStepValue) (OperationKind.SyntheticLocalReferenceExpression, Type: System.Int32)
        Right: IConversionExpression (ConversionKind.Basic, Implicit) (OperationKind.ConversionExpression, Type: System.Int32)
            ILocalReferenceExpression: s (OperationKind.LocalReferenceExpression, Type: System.Double)
  AtLoopBottom: IExpressionStatement (OperationKind.ExpressionStatement)
      ICompoundAssignmentExpression (BinaryOperationKind.IntegerAdd) (OperationKind.CompoundAssignmentExpression, Type: System.Int32)
        Left: ILocalReferenceExpression: i (OperationKind.LocalReferenceExpression, Type: System.Int32)
        Right: ISyntheticLocalReferenceExpression (SynthesizedLocalKind.ForLoopStepValue) (OperationKind.SyntheticLocalReferenceExpression, Type: System.Int32)
  IBlockStatement (1 statements) (OperationKind.BlockStatement)
    IExpressionStatement (OperationKind.ExpressionStatement)
      IInvocationExpression (static Sub System.Console.WriteLine(value As System.Int32)) (OperationKind.InvocationExpression, Type: System.Void)
        IArgument (Matching Parameter: value) (OperationKind.Argument)
          IArrayElementReferenceExpression (OperationKind.ArrayElementReferenceExpression, Type: System.Int32)
            ILocalReferenceExpression: myarray (OperationKind.LocalReferenceExpression, Type: System.Int32())
            Indices: ILocalReferenceExpression: i (OperationKind.LocalReferenceExpression, Type: System.Int32)
]]>.Value

            VerifyOperationTreeForTest(Of ForBlockSyntax)(source, expectedOperationTree)
        End Sub

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
IForLoopStatement (LoopKind.For) (OperationKind.LoopStatement)
  Condition: IConditionalChoiceExpression (OperationKind.ConditionalChoiceExpression, Type: System.Boolean)
      Condition: IBinaryOperatorExpression (BinaryOperationKind.FloatingGreaterThanOrEqual) (OperationKind.BinaryOperatorExpression, Type: System.Boolean)
          Left: ISyntheticLocalReferenceExpression (SynthesizedLocalKind.ForLoopStepValue) (OperationKind.SyntheticLocalReferenceExpression, Type: System.Double)
          Right: ILiteralExpression (Text: 0) (OperationKind.LiteralExpression, Type: System.Double, Constant: 0)
      IfTrue: IBinaryOperatorExpression (BinaryOperationKind.FloatingLessThanOrEqual) (OperationKind.BinaryOperatorExpression, Type: System.Boolean)
          Left: ILocalReferenceExpression: i (OperationKind.LocalReferenceExpression, Type: System.Double)
          Right: IConversionExpression (ConversionKind.Basic, Implicit) (OperationKind.ConversionExpression, Type: System.Double, Constant: 0)
              ILiteralExpression (Text: 0) (OperationKind.LiteralExpression, Type: System.Int32, Constant: 0)
      IfFalse: IBinaryOperatorExpression (BinaryOperationKind.FloatingGreaterThanOrEqual) (OperationKind.BinaryOperatorExpression, Type: System.Boolean)
          Left: ILocalReferenceExpression: i (OperationKind.LocalReferenceExpression, Type: System.Double)
          Right: IConversionExpression (ConversionKind.Basic, Implicit) (OperationKind.ConversionExpression, Type: System.Double, Constant: 0)
              ILiteralExpression (Text: 0) (OperationKind.LiteralExpression, Type: System.Int32, Constant: 0)
  Before: IExpressionStatement (OperationKind.ExpressionStatement)
      IAssignmentExpression (OperationKind.AssignmentExpression, Type: System.Double)
        Left: ILocalReferenceExpression: i (OperationKind.LocalReferenceExpression, Type: System.Double)
        Right: IConversionExpression (ConversionKind.Basic, Implicit) (OperationKind.ConversionExpression, Type: System.Double, Constant: 2)
            ILiteralExpression (Text: 2) (OperationKind.LiteralExpression, Type: System.Int32, Constant: 2)
    IExpressionStatement (OperationKind.ExpressionStatement)
      IAssignmentExpression (OperationKind.AssignmentExpression, Type: System.Double)
        Left: ISyntheticLocalReferenceExpression (SynthesizedLocalKind.ForLoopStepValue) (OperationKind.SyntheticLocalReferenceExpression, Type: System.Double)
        Right: ILocalReferenceExpression: s (OperationKind.LocalReferenceExpression, Type: System.Double)
  AtLoopBottom: IExpressionStatement (OperationKind.ExpressionStatement)
      ICompoundAssignmentExpression (BinaryOperationKind.FloatingAdd) (OperationKind.CompoundAssignmentExpression, Type: System.Double)
        Left: ILocalReferenceExpression: i (OperationKind.LocalReferenceExpression, Type: System.Double)
        Right: ISyntheticLocalReferenceExpression (SynthesizedLocalKind.ForLoopStepValue) (OperationKind.SyntheticLocalReferenceExpression, Type: System.Double)
  IBlockStatement (1 statements) (OperationKind.BlockStatement)
    IExpressionStatement (OperationKind.ExpressionStatement)
      IInvocationExpression (static Sub System.Console.WriteLine(value As System.Double)) (OperationKind.InvocationExpression, Type: System.Void)
        IArgument (Matching Parameter: value) (OperationKind.Argument)
          ILocalReferenceExpression: i (OperationKind.LocalReferenceExpression, Type: System.Double)
]]>.Value

            VerifyOperationTreeForTest(Of ForBlockSyntax)(source, expectedOperationTree)
        End Sub

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
IForLoopStatement (LoopKind.For) (OperationKind.LoopStatement)
  Condition: IConditionalChoiceExpression (OperationKind.ConditionalChoiceExpression, Type: System.Boolean)
      Condition: IBinaryOperatorExpression (BinaryOperationKind.ObjectGreaterThanOrEqual) (OperationKind.BinaryOperatorExpression, Type: System.Boolean)
          Left: ISyntheticLocalReferenceExpression (SynthesizedLocalKind.ForLoopStepValue) (OperationKind.SyntheticLocalReferenceExpression, Type: System.Object)
          Right: ILiteralExpression (Text: 1) (OperationKind.LiteralExpression, Type: System.Object, Constant: 1)
      IfTrue: IBinaryOperatorExpression (BinaryOperationKind.ObjectLessThanOrEqual) (OperationKind.BinaryOperatorExpression, Type: System.Boolean)
          Left: ILocalReferenceExpression: ctrlVar (OperationKind.LocalReferenceExpression, Type: System.Object)
          Right: ISyntheticLocalReferenceExpression (SynthesizedLocalKind.ForLoopLimitValue) (OperationKind.SyntheticLocalReferenceExpression, Type: System.Object)
      IfFalse: IBinaryOperatorExpression (BinaryOperationKind.ObjectGreaterThanOrEqual) (OperationKind.BinaryOperatorExpression, Type: System.Boolean)
          Left: ILocalReferenceExpression: ctrlVar (OperationKind.LocalReferenceExpression, Type: System.Object)
          Right: ISyntheticLocalReferenceExpression (SynthesizedLocalKind.ForLoopLimitValue) (OperationKind.SyntheticLocalReferenceExpression, Type: System.Object)
  Before: IExpressionStatement (OperationKind.ExpressionStatement)
      IAssignmentExpression (OperationKind.AssignmentExpression, Type: System.Object)
        Left: ILocalReferenceExpression: ctrlVar (OperationKind.LocalReferenceExpression, Type: System.Object)
        Right: ILocalReferenceExpression: initValue (OperationKind.LocalReferenceExpression, Type: System.Object)
    IExpressionStatement (OperationKind.ExpressionStatement)
      IAssignmentExpression (OperationKind.AssignmentExpression, Type: System.Object)
        Left: ISyntheticLocalReferenceExpression (SynthesizedLocalKind.ForLoopLimitValue) (OperationKind.SyntheticLocalReferenceExpression, Type: System.Object)
        Right: ILocalReferenceExpression: limit (OperationKind.LocalReferenceExpression, Type: System.Object)
    IExpressionStatement (OperationKind.ExpressionStatement)
      IAssignmentExpression (OperationKind.AssignmentExpression, Type: System.Object)
        Left: ISyntheticLocalReferenceExpression (SynthesizedLocalKind.ForLoopStepValue) (OperationKind.SyntheticLocalReferenceExpression, Type: System.Object)
        Right: ILocalReferenceExpression: stp (OperationKind.LocalReferenceExpression, Type: System.Object)
  AtLoopBottom: IExpressionStatement (OperationKind.ExpressionStatement)
      ICompoundAssignmentExpression (BinaryOperationKind.ObjectAdd) (OperationKind.CompoundAssignmentExpression, Type: System.Object)
        Left: ILocalReferenceExpression: ctrlVar (OperationKind.LocalReferenceExpression, Type: System.Object)
        Right: ISyntheticLocalReferenceExpression (SynthesizedLocalKind.ForLoopStepValue) (OperationKind.SyntheticLocalReferenceExpression, Type: System.Object)
  IBlockStatement (1 statements) (OperationKind.BlockStatement)
    IExpressionStatement (OperationKind.ExpressionStatement)
      IInvocationExpression (static Sub System.Console.WriteLine(value As System.Object)) (OperationKind.InvocationExpression, Type: System.Void)
        IArgument (Matching Parameter: value) (OperationKind.Argument)
          ILocalReferenceExpression: ctrlVar (OperationKind.LocalReferenceExpression, Type: System.Object)
]]>.Value

            VerifyOperationTreeForTest(Of ForBlockSyntax)(source, expectedOperationTree)
        End Sub

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
IForLoopStatement (LoopKind.For) (OperationKind.LoopStatement)
  Condition: IBinaryOperatorExpression (BinaryOperationKind.IntegerLessThanOrEqual) (OperationKind.BinaryOperatorExpression, Type: System.Boolean)
      Left: ILocalReferenceExpression: AVarName (OperationKind.LocalReferenceExpression, Type: System.Int32)
      Right: ILiteralExpression (Text: 2) (OperationKind.LiteralExpression, Type: System.Int32, Constant: 2)
  Before: IExpressionStatement (OperationKind.ExpressionStatement)
      IAssignmentExpression (OperationKind.AssignmentExpression, Type: System.Int32)
        Left: ILocalReferenceExpression: AVarName (OperationKind.LocalReferenceExpression, Type: System.Int32)
        Right: ILiteralExpression (Text: 1) (OperationKind.LiteralExpression, Type: System.Int32, Constant: 1)
  AtLoopBottom: IExpressionStatement (OperationKind.ExpressionStatement)
      ICompoundAssignmentExpression (BinaryOperationKind.IntegerAdd) (OperationKind.CompoundAssignmentExpression, Type: System.Int32)
        Left: ILocalReferenceExpression: AVarName (OperationKind.LocalReferenceExpression, Type: System.Int32)
        Right: IConversionExpression (ConversionKind.Basic, Explicit) (OperationKind.ConversionExpression, Type: System.Int32, Constant: 1)
            ILiteralExpression (OperationKind.LiteralExpression, Type: System.Int32, Constant: 1)
  IBlockStatement (1 statements) (OperationKind.BlockStatement)
    IForLoopStatement (LoopKind.For) (OperationKind.LoopStatement)
      Condition: IBinaryOperatorExpression (BinaryOperationKind.IntegerLessThanOrEqual) (OperationKind.BinaryOperatorExpression, Type: System.Boolean)
          Left: ILocalReferenceExpression: B (OperationKind.LocalReferenceExpression, Type: System.Int32)
          Right: ILiteralExpression (Text: 2) (OperationKind.LiteralExpression, Type: System.Int32, Constant: 2)
      Before: IExpressionStatement (OperationKind.ExpressionStatement)
          IAssignmentExpression (OperationKind.AssignmentExpression, Type: System.Int32)
            Left: ILocalReferenceExpression: B (OperationKind.LocalReferenceExpression, Type: System.Int32)
            Right: ILiteralExpression (Text: 1) (OperationKind.LiteralExpression, Type: System.Int32, Constant: 1)
      AtLoopBottom: IExpressionStatement (OperationKind.ExpressionStatement)
          ICompoundAssignmentExpression (BinaryOperationKind.IntegerAdd) (OperationKind.CompoundAssignmentExpression, Type: System.Int32)
            Left: ILocalReferenceExpression: B (OperationKind.LocalReferenceExpression, Type: System.Int32)
            Right: IConversionExpression (ConversionKind.Basic, Explicit) (OperationKind.ConversionExpression, Type: System.Int32, Constant: 1)
                ILiteralExpression (OperationKind.LiteralExpression, Type: System.Int32, Constant: 1)
      IBlockStatement (1 statements) (OperationKind.BlockStatement)
        IForLoopStatement (LoopKind.For) (OperationKind.LoopStatement)
          Condition: IBinaryOperatorExpression (BinaryOperationKind.IntegerLessThanOrEqual) (OperationKind.BinaryOperatorExpression, Type: System.Boolean)
              Left: ILocalReferenceExpression: C (OperationKind.LocalReferenceExpression, Type: System.Int32)
              Right: ILiteralExpression (Text: 2) (OperationKind.LiteralExpression, Type: System.Int32, Constant: 2)
          Before: IExpressionStatement (OperationKind.ExpressionStatement)
              IAssignmentExpression (OperationKind.AssignmentExpression, Type: System.Int32)
                Left: ILocalReferenceExpression: C (OperationKind.LocalReferenceExpression, Type: System.Int32)
                Right: ILiteralExpression (Text: 1) (OperationKind.LiteralExpression, Type: System.Int32, Constant: 1)
          AtLoopBottom: IExpressionStatement (OperationKind.ExpressionStatement)
              ICompoundAssignmentExpression (BinaryOperationKind.IntegerAdd) (OperationKind.CompoundAssignmentExpression, Type: System.Int32)
                Left: ILocalReferenceExpression: C (OperationKind.LocalReferenceExpression, Type: System.Int32)
                Right: IConversionExpression (ConversionKind.Basic, Explicit) (OperationKind.ConversionExpression, Type: System.Int32, Constant: 1)
                    ILiteralExpression (OperationKind.LiteralExpression, Type: System.Int32, Constant: 1)
          IBlockStatement (1 statements) (OperationKind.BlockStatement)
            IForLoopStatement (LoopKind.For) (OperationKind.LoopStatement)
              Condition: IBinaryOperatorExpression (BinaryOperationKind.IntegerLessThanOrEqual) (OperationKind.BinaryOperatorExpression, Type: System.Boolean)
                  Left: ILocalReferenceExpression: D (OperationKind.LocalReferenceExpression, Type: System.Int32)
                  Right: ILiteralExpression (Text: 2) (OperationKind.LiteralExpression, Type: System.Int32, Constant: 2)
              Before: IExpressionStatement (OperationKind.ExpressionStatement)
                  IAssignmentExpression (OperationKind.AssignmentExpression, Type: System.Int32)
                    Left: ILocalReferenceExpression: D (OperationKind.LocalReferenceExpression, Type: System.Int32)
                    Right: ILiteralExpression (Text: 1) (OperationKind.LiteralExpression, Type: System.Int32, Constant: 1)
              AtLoopBottom: IExpressionStatement (OperationKind.ExpressionStatement)
                  ICompoundAssignmentExpression (BinaryOperationKind.IntegerAdd) (OperationKind.CompoundAssignmentExpression, Type: System.Int32)
                    Left: ILocalReferenceExpression: D (OperationKind.LocalReferenceExpression, Type: System.Int32)
                    Right: IConversionExpression (ConversionKind.Basic, Explicit) (OperationKind.ConversionExpression, Type: System.Int32, Constant: 1)
                        ILiteralExpression (OperationKind.LiteralExpression, Type: System.Int32, Constant: 1)
              IBlockStatement (0 statements) (OperationKind.BlockStatement)
]]>.Value

            VerifyOperationTreeForTest(Of ForBlockSyntax)(source, expectedOperationTree)
        End Sub

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
IForLoopStatement (LoopKind.For) (OperationKind.LoopStatement)
  Condition: IBinaryOperatorExpression (BinaryOperationKind.IntegerLessThanOrEqual) (OperationKind.BinaryOperatorExpression, Type: System.Boolean)
      Left: ILocalReferenceExpression: I (OperationKind.LocalReferenceExpression, Type: System.Int32)
      Right: ILiteralExpression (Text: 2) (OperationKind.LiteralExpression, Type: System.Int32, Constant: 2)
  Before: IExpressionStatement (OperationKind.ExpressionStatement)
      IAssignmentExpression (OperationKind.AssignmentExpression, Type: System.Int32)
        Left: ILocalReferenceExpression: I (OperationKind.LocalReferenceExpression, Type: System.Int32)
        Right: ILiteralExpression (Text: 1) (OperationKind.LiteralExpression, Type: System.Int32, Constant: 1)
  AtLoopBottom: IExpressionStatement (OperationKind.ExpressionStatement)
      ICompoundAssignmentExpression (BinaryOperationKind.IntegerAdd) (OperationKind.CompoundAssignmentExpression, Type: System.Int32)
        Left: ILocalReferenceExpression: I (OperationKind.LocalReferenceExpression, Type: System.Int32)
        Right: IConversionExpression (ConversionKind.Basic, Explicit) (OperationKind.ConversionExpression, Type: System.Int32, Constant: 1)
            ILiteralExpression (OperationKind.LiteralExpression, Type: System.Int32, Constant: 1)
  IBlockStatement (1 statements) (OperationKind.BlockStatement)
    IForLoopStatement (LoopKind.For) (OperationKind.LoopStatement)
      Condition: IBinaryOperatorExpression (BinaryOperationKind.IntegerLessThanOrEqual) (OperationKind.BinaryOperatorExpression, Type: System.Boolean)
          Left: ILocalReferenceExpression: J (OperationKind.LocalReferenceExpression, Type: System.Int32)
          Right: ILiteralExpression (Text: 2) (OperationKind.LiteralExpression, Type: System.Int32, Constant: 2)
      Before: IExpressionStatement (OperationKind.ExpressionStatement)
          IAssignmentExpression (OperationKind.AssignmentExpression, Type: System.Int32)
            Left: ILocalReferenceExpression: J (OperationKind.LocalReferenceExpression, Type: System.Int32)
            Right: ILiteralExpression (Text: 1) (OperationKind.LiteralExpression, Type: System.Int32, Constant: 1)
      AtLoopBottom: IExpressionStatement (OperationKind.ExpressionStatement)
          ICompoundAssignmentExpression (BinaryOperationKind.IntegerAdd) (OperationKind.CompoundAssignmentExpression, Type: System.Int32)
            Left: ILocalReferenceExpression: J (OperationKind.LocalReferenceExpression, Type: System.Int32)
            Right: IConversionExpression (ConversionKind.Basic, Explicit) (OperationKind.ConversionExpression, Type: System.Int32, Constant: 1)
                ILiteralExpression (OperationKind.LiteralExpression, Type: System.Int32, Constant: 1)
      IBlockStatement (2 statements) (OperationKind.BlockStatement)
        IExpressionStatement (OperationKind.ExpressionStatement)
          IAssignmentExpression (OperationKind.AssignmentExpression, Type: System.Int32)
            Left: ILocalReferenceExpression: I (OperationKind.LocalReferenceExpression, Type: System.Int32)
            Right: ILiteralExpression (Text: 3) (OperationKind.LiteralExpression, Type: System.Int32, Constant: 3)
        IExpressionStatement (OperationKind.ExpressionStatement)
          IInvocationExpression (static Sub System.Console.WriteLine(value As System.Int32)) (OperationKind.InvocationExpression, Type: System.Void)
            IArgument (Matching Parameter: value) (OperationKind.Argument)
              ILocalReferenceExpression: I (OperationKind.LocalReferenceExpression, Type: System.Int32)
]]>.Value

            VerifyOperationTreeForTest(Of ForBlockSyntax)(source, expectedOperationTree)
        End Sub

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
IForLoopStatement (LoopKind.For) (OperationKind.LoopStatement)
  Condition: IBinaryOperatorExpression (BinaryOperationKind.IntegerLessThanOrEqual) (OperationKind.BinaryOperatorExpression, Type: System.Boolean)
      Left: ILocalReferenceExpression: I (OperationKind.LocalReferenceExpression, Type: System.Int32)
      Right: ILiteralExpression (Text: 2) (OperationKind.LiteralExpression, Type: System.Int32, Constant: 2)
  Before: IExpressionStatement (OperationKind.ExpressionStatement)
      IAssignmentExpression (OperationKind.AssignmentExpression, Type: System.Int32)
        Left: ILocalReferenceExpression: I (OperationKind.LocalReferenceExpression, Type: System.Int32)
        Right: ILiteralExpression (Text: 1) (OperationKind.LiteralExpression, Type: System.Int32, Constant: 1)
  AtLoopBottom: IExpressionStatement (OperationKind.ExpressionStatement)
      ICompoundAssignmentExpression (BinaryOperationKind.IntegerAdd) (OperationKind.CompoundAssignmentExpression, Type: System.Int32)
        Left: ILocalReferenceExpression: I (OperationKind.LocalReferenceExpression, Type: System.Int32)
        Right: IConversionExpression (ConversionKind.Basic, Explicit) (OperationKind.ConversionExpression, Type: System.Int32, Constant: 1)
            ILiteralExpression (OperationKind.LiteralExpression, Type: System.Int32, Constant: 1)
  IBlockStatement (1 statements) (OperationKind.BlockStatement)
    IForLoopStatement (LoopKind.For) (OperationKind.LoopStatement)
      Condition: IBinaryOperatorExpression (BinaryOperationKind.IntegerLessThanOrEqual) (OperationKind.BinaryOperatorExpression, Type: System.Boolean)
          Left: ILocalReferenceExpression: J (OperationKind.LocalReferenceExpression, Type: System.Int32)
          Right: ILiteralExpression (Text: 2) (OperationKind.LiteralExpression, Type: System.Int32, Constant: 2)
      Before: IExpressionStatement (OperationKind.ExpressionStatement)
          IAssignmentExpression (OperationKind.AssignmentExpression, Type: System.Int32)
            Left: ILocalReferenceExpression: J (OperationKind.LocalReferenceExpression, Type: System.Int32)
            Right: IBinaryOperatorExpression (BinaryOperationKind.IntegerAdd) (OperationKind.BinaryOperatorExpression, Type: System.Int32)
                Left: ILocalReferenceExpression: I (OperationKind.LocalReferenceExpression, Type: System.Int32)
                Right: ILiteralExpression (Text: 1) (OperationKind.LiteralExpression, Type: System.Int32, Constant: 1)
      AtLoopBottom: IExpressionStatement (OperationKind.ExpressionStatement)
          ICompoundAssignmentExpression (BinaryOperationKind.IntegerAdd) (OperationKind.CompoundAssignmentExpression, Type: System.Int32)
            Left: ILocalReferenceExpression: J (OperationKind.LocalReferenceExpression, Type: System.Int32)
            Right: IConversionExpression (ConversionKind.Basic, Explicit) (OperationKind.ConversionExpression, Type: System.Int32, Constant: 1)
                ILiteralExpression (OperationKind.LiteralExpression, Type: System.Int32, Constant: 1)
      IBlockStatement (1 statements) (OperationKind.BlockStatement)
        IExpressionStatement (OperationKind.ExpressionStatement)
          IInvocationExpression (static Sub System.Console.WriteLine(value As System.Int32)) (OperationKind.InvocationExpression, Type: System.Void)
            IArgument (Matching Parameter: value) (OperationKind.Argument)
              ILocalReferenceExpression: J (OperationKind.LocalReferenceExpression, Type: System.Int32)
]]>.Value

            VerifyOperationTreeForTest(Of ForBlockSyntax)(source, expectedOperationTree)
        End Sub

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
IForLoopStatement (LoopKind.For) (OperationKind.LoopStatement)
  Condition: IBinaryOperatorExpression (BinaryOperationKind.IntegerLessThanOrEqual) (OperationKind.BinaryOperatorExpression, Type: System.Boolean)
      Left: ILocalReferenceExpression: I (OperationKind.LocalReferenceExpression, Type: System.Int32)
      Right: ILiteralExpression (Text: 2) (OperationKind.LiteralExpression, Type: System.Int32, Constant: 2)
  Before: IExpressionStatement (OperationKind.ExpressionStatement)
      IAssignmentExpression (OperationKind.AssignmentExpression, Type: System.Int32)
        Left: ILocalReferenceExpression: I (OperationKind.LocalReferenceExpression, Type: System.Int32)
        Right: ILiteralExpression (Text: 1) (OperationKind.LiteralExpression, Type: System.Int32, Constant: 1)
  AtLoopBottom: IExpressionStatement (OperationKind.ExpressionStatement)
      ICompoundAssignmentExpression (BinaryOperationKind.IntegerAdd) (OperationKind.CompoundAssignmentExpression, Type: System.Int32)
        Left: ILocalReferenceExpression: I (OperationKind.LocalReferenceExpression, Type: System.Int32)
        Right: IConversionExpression (ConversionKind.Basic, Explicit) (OperationKind.ConversionExpression, Type: System.Int32, Constant: 1)
            ILiteralExpression (OperationKind.LiteralExpression, Type: System.Int32, Constant: 1)
  IBlockStatement (2 statements) (OperationKind.BlockStatement)
    IForLoopStatement (LoopKind.For) (OperationKind.LoopStatement)
      Condition: IBinaryOperatorExpression (BinaryOperationKind.IntegerLessThanOrEqual) (OperationKind.BinaryOperatorExpression, Type: System.Boolean)
          Left: ILocalReferenceExpression: J (OperationKind.LocalReferenceExpression, Type: System.Int32)
          Right: ILiteralExpression (Text: 2) (OperationKind.LiteralExpression, Type: System.Int32, Constant: 2)
      Before: IExpressionStatement (OperationKind.ExpressionStatement)
          IAssignmentExpression (OperationKind.AssignmentExpression, Type: System.Int32)
            Left: ILocalReferenceExpression: J (OperationKind.LocalReferenceExpression, Type: System.Int32)
            Right: ILiteralExpression (Text: 1) (OperationKind.LiteralExpression, Type: System.Int32, Constant: 1)
      AtLoopBottom: IExpressionStatement (OperationKind.ExpressionStatement)
          ICompoundAssignmentExpression (BinaryOperationKind.IntegerAdd) (OperationKind.CompoundAssignmentExpression, Type: System.Int32)
            Left: ILocalReferenceExpression: J (OperationKind.LocalReferenceExpression, Type: System.Int32)
            Right: IConversionExpression (ConversionKind.Basic, Explicit) (OperationKind.ConversionExpression, Type: System.Int32, Constant: 1)
                ILiteralExpression (OperationKind.LiteralExpression, Type: System.Int32, Constant: 1)
      IBlockStatement (1 statements) (OperationKind.BlockStatement)
        IBranchStatement (BranchKind.Break, Label: exit) (OperationKind.BranchStatement)
    IExpressionStatement (OperationKind.ExpressionStatement)
      IInvocationExpression (static Sub System.Console.WriteLine(value As System.Int32)) (OperationKind.InvocationExpression, Type: System.Void)
        IArgument (Matching Parameter: value) (OperationKind.Argument)
          ILocalReferenceExpression: I (OperationKind.LocalReferenceExpression, Type: System.Int32)
]]>.Value

            VerifyOperationTreeForTest(Of ForBlockSyntax)(source, expectedOperationTree)
        End Sub

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
IForLoopStatement (LoopKind.For) (OperationKind.LoopStatement)
  Condition: IBinaryOperatorExpression (BinaryOperationKind.EnumLessThanOrEqual) (OperationKind.BinaryOperatorExpression, Type: System.Boolean)
      Left: ILocalReferenceExpression: x (OperationKind.LocalReferenceExpression, Type: e1)
      Right: IFieldReferenceExpression: e1.c (Static) (OperationKind.FieldReferenceExpression, Type: e1, Constant: 2)
  Before: IExpressionStatement (OperationKind.ExpressionStatement)
      IAssignmentExpression (OperationKind.AssignmentExpression, Type: e1)
        Left: ILocalReferenceExpression: x (OperationKind.LocalReferenceExpression, Type: e1)
        Right: IFieldReferenceExpression: e1.a (Static) (OperationKind.FieldReferenceExpression, Type: e1, Constant: 0)
  AtLoopBottom: IExpressionStatement (OperationKind.ExpressionStatement)
      ICompoundAssignmentExpression (BinaryOperationKind.EnumAdd) (OperationKind.CompoundAssignmentExpression, Type: e1)
        Left: ILocalReferenceExpression: x (OperationKind.LocalReferenceExpression, Type: e1)
        Right: IConversionExpression (ConversionKind.Basic, Explicit) (OperationKind.ConversionExpression, Type: e1, Constant: 1)
            ILiteralExpression (OperationKind.LiteralExpression, Type: System.Int32, Constant: 1)
  IBlockStatement (0 statements) (OperationKind.BlockStatement)
]]>.Value

            VerifyOperationTreeForTest(Of ForBlockSyntax)(source, expectedOperationTree)
        End Sub

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
    Public Sub Foo()
        For i As Integer = P1(30 + i) To 30'BIND:"For i As Integer = P1(30 + i) To 30"
        Next
    End Sub
End Class
    ]]>.Value

            Dim expectedOperationTree = <![CDATA[
IForLoopStatement (LoopKind.For) (OperationKind.LoopStatement)
  Condition: IBinaryOperatorExpression (BinaryOperationKind.IntegerLessThanOrEqual) (OperationKind.BinaryOperatorExpression, Type: System.Boolean)
      Left: ILocalReferenceExpression: i (OperationKind.LocalReferenceExpression, Type: System.Int32)
      Right: ILiteralExpression (Text: 30) (OperationKind.LiteralExpression, Type: System.Int32, Constant: 30)
  Before: IExpressionStatement (OperationKind.ExpressionStatement)
      IAssignmentExpression (OperationKind.AssignmentExpression, Type: System.Int32)
        Left: ILocalReferenceExpression: i (OperationKind.LocalReferenceExpression, Type: System.Int32)
        Right: IConversionExpression (ConversionKind.Basic, Implicit) (OperationKind.ConversionExpression, Type: System.Int32)
            IIndexedPropertyReferenceExpression: Property MyClass1.P1(x As System.Int64) As System.Byte (OperationKind.IndexedPropertyReferenceExpression, Type: System.Byte)
             (OperationKind.IndexedPropertyReferenceExpression, Type: System.Byte)
              Instance Receiver: IInstanceReferenceExpression (InstanceReferenceKind.Implicit) (OperationKind.InstanceReferenceExpression, Type: MyClass1)
  AtLoopBottom: IExpressionStatement (OperationKind.ExpressionStatement)
      ICompoundAssignmentExpression (BinaryOperationKind.IntegerAdd) (OperationKind.CompoundAssignmentExpression, Type: System.Int32)
        Left: ILocalReferenceExpression: i (OperationKind.LocalReferenceExpression, Type: System.Int32)
        Right: IConversionExpression (ConversionKind.Basic, Explicit) (OperationKind.ConversionExpression, Type: System.Int32, Constant: 1)
            ILiteralExpression (OperationKind.LiteralExpression, Type: System.Int32, Constant: 1)
  IBlockStatement (0 statements) (OperationKind.BlockStatement)
]]>.Value

            VerifyOperationTreeForTest(Of ForBlockSyntax)(source, expectedOperationTree)
        End Sub

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
IForLoopStatement (LoopKind.For) (OperationKind.LoopStatement)
  Condition: IBinaryOperatorExpression (BinaryOperationKind.IntegerLessThanOrEqual) (OperationKind.BinaryOperatorExpression, Type: System.Boolean)
      Left: ILocalReferenceExpression: global_x (OperationKind.LocalReferenceExpression, Type: System.Int32)
      Right: ILiteralExpression (Text: 10) (OperationKind.LiteralExpression, Type: System.Int32, Constant: 10)
  Before: IExpressionStatement (OperationKind.ExpressionStatement)
      IAssignmentExpression (OperationKind.AssignmentExpression, Type: System.Int32)
        Left: ILocalReferenceExpression: global_x (OperationKind.LocalReferenceExpression, Type: System.Int32)
        Right: IConversionExpression (ConversionKind.Basic, Implicit) (OperationKind.ConversionExpression, Type: System.Int32, Constant: 20)
            IFieldReferenceExpression: MyClass1.global_y As System.Int64 (Static) (OperationKind.FieldReferenceExpression, Type: System.Int64, Constant: 20)
  AtLoopBottom: IExpressionStatement (OperationKind.ExpressionStatement)
      ICompoundAssignmentExpression (BinaryOperationKind.IntegerAdd) (OperationKind.CompoundAssignmentExpression, Type: System.Int32)
        Left: ILocalReferenceExpression: global_x (OperationKind.LocalReferenceExpression, Type: System.Int32)
        Right: IConversionExpression (ConversionKind.Basic, Explicit) (OperationKind.ConversionExpression, Type: System.Int32, Constant: 1)
            ILiteralExpression (OperationKind.LiteralExpression, Type: System.Int32, Constant: 1)
  IBlockStatement (0 statements) (OperationKind.BlockStatement)
]]>.Value

            VerifyOperationTreeForTest(Of ForBlockSyntax)(source, expectedOperationTree)
        End Sub

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
IForLoopStatement (LoopKind.For) (OperationKind.LoopStatement)
  Condition: IBinaryOperatorExpression (BinaryOperationKind.IntegerLessThanOrEqual) (OperationKind.BinaryOperatorExpression, Type: System.Boolean)
      Left: ILocalReferenceExpression: x (OperationKind.LocalReferenceExpression, Type: System.Int32)
      Right: ILiteralExpression (Text: 10) (OperationKind.LiteralExpression, Type: System.Int32, Constant: 10)
  Before: IExpressionStatement (OperationKind.ExpressionStatement)
      IAssignmentExpression (OperationKind.AssignmentExpression, Type: System.Int32)
        Left: ILocalReferenceExpression: x (OperationKind.LocalReferenceExpression, Type: System.Int32)
        Right: ILiteralExpression (Text: 0) (OperationKind.LiteralExpression, Type: System.Int32, Constant: 0)
  AtLoopBottom: IExpressionStatement (OperationKind.ExpressionStatement)
      ICompoundAssignmentExpression (BinaryOperationKind.IntegerAdd) (OperationKind.CompoundAssignmentExpression, Type: System.Int32)
        Left: ILocalReferenceExpression: x (OperationKind.LocalReferenceExpression, Type: System.Int32)
        Right: IConversionExpression (ConversionKind.Basic, Explicit) (OperationKind.ConversionExpression, Type: System.Int32, Constant: 1)
            ILiteralExpression (OperationKind.LiteralExpression, Type: System.Int32, Constant: 1)
  IBlockStatement (0 statements) (OperationKind.BlockStatement)
]]>.Value

            VerifyOperationTreeForTest(Of ForBlockSyntax)(source, expectedOperationTree)
        End Sub

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
IForLoopStatement (LoopKind.For) (OperationKind.LoopStatement)
  Condition: IBinaryOperatorExpression (BinaryOperationKind.IntegerLessThanOrEqual) (OperationKind.BinaryOperatorExpression, Type: System.Boolean)
      Left: ILocalReferenceExpression: Y (OperationKind.LocalReferenceExpression, Type: System.Int32)
      Right: ILiteralExpression (Text: 2) (OperationKind.LiteralExpression, Type: System.Int32, Constant: 2)
  Before: IExpressionStatement (OperationKind.ExpressionStatement)
      IAssignmentExpression (OperationKind.AssignmentExpression, Type: System.Int32)
        Left: ILocalReferenceExpression: Y (OperationKind.LocalReferenceExpression, Type: System.Int32)
        Right: ILiteralExpression (Text: 1) (OperationKind.LiteralExpression, Type: System.Int32, Constant: 1)
  AtLoopBottom: IExpressionStatement (OperationKind.ExpressionStatement)
      ICompoundAssignmentExpression (BinaryOperationKind.IntegerAdd) (OperationKind.CompoundAssignmentExpression, Type: System.Int32)
        Left: ILocalReferenceExpression: Y (OperationKind.LocalReferenceExpression, Type: System.Int32)
        Right: IConversionExpression (ConversionKind.Basic, Explicit) (OperationKind.ConversionExpression, Type: System.Int32, Constant: 1)
            ILiteralExpression (OperationKind.LiteralExpression, Type: System.Int32, Constant: 1)
  IBlockStatement (0 statements) (OperationKind.BlockStatement)
]]>.Value

            VerifyOperationTreeForTest(Of ForBlockSyntax)(source, expectedOperationTree)
        End Sub

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
IForLoopStatement (LoopKind.For) (OperationKind.LoopStatement)
  Condition: IBinaryOperatorExpression (BinaryOperationKind.IntegerLessThanOrEqual) (OperationKind.BinaryOperatorExpression, Type: System.Boolean)
      Left: ILocalReferenceExpression: element1 (OperationKind.LocalReferenceExpression, Type: System.Int32)
      Right: ILiteralExpression (Text: 42) (OperationKind.LiteralExpression, Type: System.Int32, Constant: 42)
  Before: IExpressionStatement (OperationKind.ExpressionStatement)
      IAssignmentExpression (OperationKind.AssignmentExpression, Type: System.Int32)
        Left: ILocalReferenceExpression: element1 (OperationKind.LocalReferenceExpression, Type: System.Int32)
        Right: ILiteralExpression (Text: 23) (OperationKind.LiteralExpression, Type: System.Int32, Constant: 23)
  AtLoopBottom: IExpressionStatement (OperationKind.ExpressionStatement)
      ICompoundAssignmentExpression (BinaryOperationKind.IntegerAdd) (OperationKind.CompoundAssignmentExpression, Type: System.Int32)
        Left: ILocalReferenceExpression: element1 (OperationKind.LocalReferenceExpression, Type: System.Int32)
        Right: IConversionExpression (ConversionKind.Basic, Explicit) (OperationKind.ConversionExpression, Type: System.Int32, Constant: 1)
            ILiteralExpression (OperationKind.LiteralExpression, Type: System.Int32, Constant: 1)
  IBlockStatement (0 statements) (OperationKind.BlockStatement)
]]>.Value

            VerifyOperationTreeForTest(Of ForBlockSyntax)(source, expectedOperationTree)
        End Sub

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
IForLoopStatement (LoopKind.For) (OperationKind.LoopStatement)
  Condition: IBinaryOperatorExpression (BinaryOperationKind.IntegerLessThanOrEqual) (OperationKind.BinaryOperatorExpression, Type: System.Boolean)
      Left: ILocalReferenceExpression: i (OperationKind.LocalReferenceExpression, Type: System.Int32)
      Right: ILiteralExpression (Text: 5) (OperationKind.LiteralExpression, Type: System.Int32, Constant: 5)
  Before: IExpressionStatement (OperationKind.ExpressionStatement)
      IAssignmentExpression (OperationKind.AssignmentExpression, Type: System.Int32)
        Left: ILocalReferenceExpression: i (OperationKind.LocalReferenceExpression, Type: System.Int32)
        Right: ILiteralExpression (Text: 0) (OperationKind.LiteralExpression, Type: System.Int32, Constant: 0)
  AtLoopBottom: IExpressionStatement (OperationKind.ExpressionStatement)
      ICompoundAssignmentExpression (BinaryOperationKind.IntegerAdd) (OperationKind.CompoundAssignmentExpression, Type: System.Int32)
        Left: ILocalReferenceExpression: i (OperationKind.LocalReferenceExpression, Type: System.Int32)
        Right: IConversionExpression (ConversionKind.Basic, Explicit) (OperationKind.ConversionExpression, Type: System.Int32, Constant: 1)
            ILiteralExpression (OperationKind.LiteralExpression, Type: System.Int32, Constant: 1)
  IBlockStatement (1 statements) (OperationKind.BlockStatement)
    IIfStatement (OperationKind.IfStatement)
      Condition: IBinaryOperatorExpression (BinaryOperationKind.IntegerEquals) (OperationKind.BinaryOperatorExpression, Type: System.Boolean)
          Left: IBinaryOperatorExpression (BinaryOperationKind.IntegerRemainder) (OperationKind.BinaryOperatorExpression, Type: System.Int32)
              Left: ILocalReferenceExpression: i (OperationKind.LocalReferenceExpression, Type: System.Int32)
              Right: ILiteralExpression (Text: 2) (OperationKind.LiteralExpression, Type: System.Int32, Constant: 2)
          Right: ILiteralExpression (Text: 0) (OperationKind.LiteralExpression, Type: System.Int32, Constant: 0)
      IBlockStatement (1 statements) (OperationKind.BlockStatement)
        IBranchStatement (BranchKind.Continue, Label: continue) (OperationKind.BranchStatement)
]]>.Value

            VerifyOperationTreeForTest(Of ForBlockSyntax)(source, expectedOperationTree)
        End Sub

        <Fact(), WorkItem(17602, "https://github.com/dotnet/roslyn/issues/17602")>
        Public Sub IForLoopStatement_ForInvalid()
            Dim source = <![CDATA[
Option Infer On

Module Program
    Sub Main()
        For X = 1 To 10
        Next
    End Sub

    Sub Main1()
        For Each X In {1, 2, 3}'BIND:"For Each X In {1, 2, 3}"
        Next
    End Sub

End Module

Module M
    Public X As Integer
End Module

Module N
    Public X As Integer
End Module

    ]]>.Value

            Dim expectedOperationTree = <![CDATA[
IForEachLoopStatement (Iteration variable: null) (LoopKind.ForEach) (OperationKind.LoopStatement, IsInvalid)
  Collection: IConversionExpression (ConversionKind.Basic, Implicit) (OperationKind.ConversionExpression, Type: System.Collections.IEnumerable)
      IArrayCreationExpression (Dimension sizes: 1, Element Type: System.Int32) (OperationKind.ArrayCreationExpression, Type: System.Int32())
        ILiteralExpression (OperationKind.LiteralExpression, Type: System.Int32, Constant: 3)
        IArrayInitializer (OperationKind.ArrayInitializer)
          ILiteralExpression (Text: 1) (OperationKind.LiteralExpression, Type: System.Int32, Constant: 1)
          ILiteralExpression (Text: 2) (OperationKind.LiteralExpression, Type: System.Int32, Constant: 2)
          ILiteralExpression (Text: 3) (OperationKind.LiteralExpression, Type: System.Int32, Constant: 3)
  IBlockStatement (0 statements) (OperationKind.BlockStatement)
]]>.Value

            VerifyOperationTreeForTest(Of ForEachBlockSyntax)(source, expectedOperationTree)
        End Sub

    End Class
End Namespace
