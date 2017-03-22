' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports Microsoft.CodeAnalysis.Semantics
Imports Microsoft.CodeAnalysis.Test.Utilities
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax
Imports Roslyn.Test.Utilities

Namespace Microsoft.CodeAnalysis.VisualBasic.UnitTests.Semantics

    Partial Public Class IOperationTests
        Inherits SemanticModelTestBase

        <Fact(Skip:="https://github.com/dotnet/roslyn/issues/18077"), WorkItem(17607, "https://github.com/dotnet/roslyn/issues/17607")>
        Public Sub InvalidVariableDeclarationStatement()
            Dim source = <![CDATA[

Class Program
    Private Shared Sub Main(args As String())
        Dim x, 1 As Integer'BIND:"Dim x, 1 As Integer"
    End Sub
End Class

    ]]>.Value

            Dim expectedOperationTree = <![CDATA[
IVariableDeclarationStatement (2 variables) (OperationKind.VariableDeclarationStatement, IsInvalid)
  IVariableDeclaration: x As System.Int32 (OperationKind.VariableDeclaration)
  IVariableDeclaration:  As System.Int32 (OperationKind.VariableDeclaration, IsInvalid)
]]>.Value

            VerifyOperationTreeForTest(Of LocalDeclarationStatementSyntax)(source, expectedOperationTree)
        End Sub

        <Fact, WorkItem(17607, "https://github.com/dotnet/roslyn/issues/17607")>
        Public Sub InvalidSwitchStatementExpression()
            Dim source = <![CDATA[

Class Program
    Private Shared Sub Main(args As String())
        Select Case Program'BIND:"Select Case Program"
            Case 1
        End Select
    End Sub
End Class

    ]]>.Value

            Dim expectedOperationTree = <![CDATA[
ISwitchStatement (1 cases) (OperationKind.SwitchStatement, IsInvalid)
  Switch expression: IOperation:  (OperationKind.None, IsInvalid)
  ISwitchCase (1 case clauses, 1 statements) (OperationKind.SwitchCase, IsInvalid)
    Case clauses: ISingleValueCaseClause (Equality operator kind: BinaryOperationKind.Invalid) (CaseKind.SingleValue) (OperationKind.SingleValueCaseClause, IsInvalid)
    Body: IBlockStatement (0 statements) (OperationKind.BlockStatement)
]]>.Value

            VerifyOperationTreeForTest(Of SelectBlockSyntax)(source, expectedOperationTree)
        End Sub

        <Fact, WorkItem(17607, "https://github.com/dotnet/roslyn/issues/17607")>
        Public Sub InvalidSwitchStatementCaseLabel()
            Dim source = <![CDATA[
Class Program
    Private Shared Sub Main(args As String())
        Dim x = New Program()
        Select Case x.ToString()'BIND:"Select Case x.ToString()"
            Case x
                Exit Select
        End Select
    End Sub
End Class
    ]]>.Value

            ' IOperation tree might be affected with https://github.com/dotnet/roslyn/issues/18089
            Dim expectedOperationTree = <![CDATA[
ISwitchStatement (1 cases) (OperationKind.SwitchStatement, IsInvalid)
  Switch expression: IInvocationExpression (virtual Function System.Object.ToString() As System.String) (OperationKind.InvocationExpression, Type: System.String)
      Instance Receiver: ILocalReferenceExpression: x (OperationKind.LocalReferenceExpression, Type: Program)
  ISwitchCase (1 case clauses, 1 statements) (OperationKind.SwitchCase, IsInvalid)
    Case clauses: ISingleValueCaseClause (Equality operator kind: BinaryOperationKind.Invalid) (CaseKind.SingleValue) (OperationKind.SingleValueCaseClause, IsInvalid)
    Body: IBlockStatement (1 statements) (OperationKind.BlockStatement)
        IBranchStatement (BranchKind.Break, Label: exit) (OperationKind.BranchStatement)
]]>.Value

            VerifyOperationTreeForTest(Of SelectBlockSyntax)(source, expectedOperationTree)
        End Sub

        <Fact, WorkItem(17607, "https://github.com/dotnet/roslyn/issues/17607")>
        Public Sub InvalidIfStatement()
            Dim source = <![CDATA[
Class Program
    Private Shared Sub Main(args As String())
        Dim x = New Program()
        If x = Nothing Then'BIND:"If x = Nothing Then"
        End If
    End Sub
End Class
    ]]>.Value

            Dim expectedOperationTree = <![CDATA[
IIfStatement (OperationKind.IfStatement, IsInvalid)
  Condition: IConversionExpression (ConversionKind.Basic, Implicit) (OperationKind.ConversionExpression, Type: System.Boolean, IsInvalid)
      IBinaryOperatorExpression (BinaryOperationKind.Invalid) (OperationKind.BinaryOperatorExpression, Type: ?, IsInvalid)
        Left: ILocalReferenceExpression: x (OperationKind.LocalReferenceExpression, Type: Program)
        Right: IConversionExpression (ConversionKind.Basic, Implicit) (OperationKind.ConversionExpression, Type: Program, Constant: null)
            ILiteralExpression (OperationKind.LiteralExpression, Type: null, Constant: null)
  IBlockStatement (0 statements) (OperationKind.BlockStatement)
]]>.Value

            VerifyOperationTreeForTest(Of MultiLineIfBlockSyntax)(source, expectedOperationTree)
        End Sub

        <Fact, WorkItem(17607, "https://github.com/dotnet/roslyn/issues/17607")>
        Public Sub InvalidIfStatement()
            Dim source = <![CDATA[
Class Program
    Private Shared Sub Main(args As String())
        Dim x = New Program()
        If Then'BIND:"If Then"
        ElseIf x Then
            x
        Else
    End Sub
End Class
    ]]>.Value

            Dim expectedOperationTree = <![CDATA[
IIfStatement (OperationKind.IfStatement, IsInvalid)
  Condition: IConversionExpression (ConversionKind.Basic, Implicit) (OperationKind.ConversionExpression, Type: System.Boolean, IsInvalid)
      IInvalidExpression (OperationKind.InvalidExpression, Type: ?, IsInvalid)
  IBlockStatement (0 statements) (OperationKind.BlockStatement)
  IIfStatement (OperationKind.IfStatement, IsInvalid)
    Condition: IConversionExpression (ConversionKind.Basic, Implicit) (OperationKind.ConversionExpression, Type: System.Boolean, IsInvalid)
        ILocalReferenceExpression: x (OperationKind.LocalReferenceExpression, Type: Program)
    IBlockStatement (1 statements) (OperationKind.BlockStatement, IsInvalid)
      IExpressionStatement (OperationKind.ExpressionStatement, IsInvalid)
        IInvalidExpression (OperationKind.InvalidExpression, Type: ?, IsInvalid)
    IBlockStatement (0 statements) (OperationKind.BlockStatement)
]]>.Value

            VerifyOperationTreeForTest(Of MultiLineIfBlockSyntax)(source, expectedOperationTree)
        End Sub

        <Fact, WorkItem(17607, "https://github.com/dotnet/roslyn/issues/17607")>
        Public Sub InvalidForStatement_MissingConditionAndStep()
            Dim source = <![CDATA[
Imports System
Imports System.Collections.Generic
Imports System.Linq

Module Program
    Sub Main(args As String())
        For i As Integer = 0'BIND:"For i As Integer = 0"
        Next i
    End Sub
End Module
    ]]>.Value

            Dim expectedOperationTree = <![CDATA[
IForLoopStatement (LoopKind.For) (OperationKind.LoopStatement, IsInvalid)
  Condition: IBinaryOperatorExpression (BinaryOperationKind.IntegerLessThanOrEqual) (OperationKind.BinaryOperatorExpression, Type: System.Boolean)
      Left: ILocalReferenceExpression: i (OperationKind.LocalReferenceExpression, Type: System.Int32)
      Right: ISyntheticLocalReferenceExpression (SynthesizedLocalKind.ForLoopLimitValue) (OperationKind.SyntheticLocalReferenceExpression, Type: System.Int32)
  Before: IExpressionStatement (OperationKind.ExpressionStatement)
      IAssignmentExpression (OperationKind.AssignmentExpression, Type: System.Int32)
        Left: ILocalReferenceExpression: i (OperationKind.LocalReferenceExpression, Type: System.Int32)
        Right: ILiteralExpression (Text: 0) (OperationKind.LiteralExpression, Type: System.Int32, Constant: 0)
    IExpressionStatement (OperationKind.ExpressionStatement, IsInvalid)
      IAssignmentExpression (OperationKind.AssignmentExpression, Type: System.Int32, IsInvalid)
        Left: ISyntheticLocalReferenceExpression (SynthesizedLocalKind.ForLoopLimitValue) (OperationKind.SyntheticLocalReferenceExpression, Type: System.Int32)
        Right: IConversionExpression (ConversionKind.Basic, Implicit) (OperationKind.ConversionExpression, Type: System.Int32, IsInvalid)
            IInvalidExpression (OperationKind.InvalidExpression, Type: ?, IsInvalid)
  AtLoopBottom: IExpressionStatement (OperationKind.ExpressionStatement)
      ICompoundAssignmentExpression (BinaryOperationKind.IntegerAdd) (OperationKind.CompoundAssignmentExpression, Type: System.Int32)
        Left: ILocalReferenceExpression: i (OperationKind.LocalReferenceExpression, Type: System.Int32)
        Right: IConversionExpression (ConversionKind.Basic, Explicit) (OperationKind.ConversionExpression, Type: System.Int32, Constant: 1)
            ILiteralExpression (OperationKind.LiteralExpression, Type: System.Int32, Constant: 1)
  IBlockStatement (0 statements) (OperationKind.BlockStatement)
]]>.Value

            VerifyOperationTreeForTest(Of ForBlockSyntax)(source, expectedOperationTree)
        End Sub

        <Fact, WorkItem(17607, "https://github.com/dotnet/roslyn/issues/17607")>
        Public Sub InvalidForStatement_MissingConditionAndInitialization()
            Dim source = <![CDATA[
Imports System
Imports System.Collections.Generic
Imports System.Linq

Module Program
    Sub Main(args As String())
        For Step (Method() + 1)'BIND:"For Step (Method() + 1)"
        Next
    End Sub

    Private Function Method() As Integer
        Return 0
    End Function
End Module
    ]]>.Value

            ' IOperation tree might be affected by https://github.com/dotnet/roslyn/issues/18112
            Dim expectedOperationTree = <![CDATA[
IForLoopStatement (LoopKind.For) (OperationKind.LoopStatement, IsInvalid)
  Condition: IConditionalChoiceExpression (OperationKind.ConditionalChoiceExpression, Type: System.Boolean)
      Condition: IBinaryOperatorExpression (BinaryOperationKind.ObjectGreaterThanOrEqual) (OperationKind.BinaryOperatorExpression, Type: System.Boolean)
          Left: ISyntheticLocalReferenceExpression (SynthesizedLocalKind.ForLoopStepValue) (OperationKind.SyntheticLocalReferenceExpression, Type: System.Object)
          Right: ILiteralExpression (Text: 1) (OperationKind.LiteralExpression, Type: System.Object, Constant: 1)
      IfTrue: IBinaryOperatorExpression (BinaryOperationKind.ObjectLessThanOrEqual) (OperationKind.BinaryOperatorExpression, Type: System.Boolean)
          Left: ILocalReferenceExpression:  (OperationKind.LocalReferenceExpression, Type: System.Object)
          Right: ISyntheticLocalReferenceExpression (SynthesizedLocalKind.ForLoopLimitValue) (OperationKind.SyntheticLocalReferenceExpression, Type: System.Object)
      IfFalse: IBinaryOperatorExpression (BinaryOperationKind.ObjectGreaterThanOrEqual) (OperationKind.BinaryOperatorExpression, Type: System.Boolean)
          Left: ILocalReferenceExpression:  (OperationKind.LocalReferenceExpression, Type: System.Object)
          Right: ISyntheticLocalReferenceExpression (SynthesizedLocalKind.ForLoopLimitValue) (OperationKind.SyntheticLocalReferenceExpression, Type: System.Object)
  Before: IExpressionStatement (OperationKind.ExpressionStatement, IsInvalid)
      IAssignmentExpression (OperationKind.AssignmentExpression, Type: System.Object, IsInvalid)
        Left: ILocalReferenceExpression:  (OperationKind.LocalReferenceExpression, Type: System.Object)
        Right: IConversionExpression (ConversionKind.Basic, Implicit) (OperationKind.ConversionExpression, Type: System.Object, IsInvalid)
            IInvalidExpression (OperationKind.InvalidExpression, Type: ?, IsInvalid)
    IExpressionStatement (OperationKind.ExpressionStatement, IsInvalid)
      IAssignmentExpression (OperationKind.AssignmentExpression, Type: System.Object, IsInvalid)
        Left: ISyntheticLocalReferenceExpression (SynthesizedLocalKind.ForLoopLimitValue) (OperationKind.SyntheticLocalReferenceExpression, Type: System.Object)
        Right: IConversionExpression (ConversionKind.Basic, Implicit) (OperationKind.ConversionExpression, Type: System.Object, IsInvalid)
            IInvalidExpression (OperationKind.InvalidExpression, Type: ?, IsInvalid)
    IExpressionStatement (OperationKind.ExpressionStatement)
      IAssignmentExpression (OperationKind.AssignmentExpression, Type: System.Object)
        Left: ISyntheticLocalReferenceExpression (SynthesizedLocalKind.ForLoopStepValue) (OperationKind.SyntheticLocalReferenceExpression, Type: System.Object)
        Right: IConversionExpression (ConversionKind.Basic, Explicit) (OperationKind.ConversionExpression, Type: System.Object)
            ILiteralExpression (OperationKind.LiteralExpression, Type: System.Int32, Constant: 1)
  AtLoopBottom: IExpressionStatement (OperationKind.ExpressionStatement)
      ICompoundAssignmentExpression (BinaryOperationKind.ObjectAdd) (OperationKind.CompoundAssignmentExpression, Type: System.Object)
        Left: ILocalReferenceExpression:  (OperationKind.LocalReferenceExpression, Type: System.Object)
        Right: ISyntheticLocalReferenceExpression (SynthesizedLocalKind.ForLoopStepValue) (OperationKind.SyntheticLocalReferenceExpression, Type: System.Object)
  IBlockStatement (0 statements) (OperationKind.BlockStatement)
]]>.Value

            VerifyOperationTreeForTest(Of ForBlockSyntax)(source, expectedOperationTree)
        End Sub

        <Fact, WorkItem(17607, "https://github.com/dotnet/roslyn/issues/17607")>
        Public Sub InvalidForStatement_InvalidConditionAndStep()
            Dim source = <![CDATA[
Imports System
Imports System.Collections.Generic
Imports System.Linq

Module Program
    Sub Main(args As String())
        For i As Integer = 0 To Program Step x'BIND:"For i As Integer = 0 To Program Step x"
        Next i
    End Sub
End Module
    ]]>.Value

            Dim expectedOperationTree = <![CDATA[
IForLoopStatement (LoopKind.For) (OperationKind.LoopStatement, IsInvalid)
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
    IExpressionStatement (OperationKind.ExpressionStatement, IsInvalid)
      IAssignmentExpression (OperationKind.AssignmentExpression, Type: System.Int32, IsInvalid)
        Left: ISyntheticLocalReferenceExpression (SynthesizedLocalKind.ForLoopLimitValue) (OperationKind.SyntheticLocalReferenceExpression, Type: System.Int32)
        Right: IConversionExpression (ConversionKind.Basic, Implicit) (OperationKind.ConversionExpression, Type: System.Int32, IsInvalid)
            IOperation:  (OperationKind.None, IsInvalid)
    IExpressionStatement (OperationKind.ExpressionStatement, IsInvalid)
      IAssignmentExpression (OperationKind.AssignmentExpression, Type: System.Int32, IsInvalid)
        Left: ISyntheticLocalReferenceExpression (SynthesizedLocalKind.ForLoopStepValue) (OperationKind.SyntheticLocalReferenceExpression, Type: System.Int32)
        Right: IConversionExpression (ConversionKind.Basic, Implicit) (OperationKind.ConversionExpression, Type: System.Int32, IsInvalid)
            IInvalidExpression (OperationKind.InvalidExpression, Type: ?, IsInvalid)
  AtLoopBottom: IExpressionStatement (OperationKind.ExpressionStatement)
      ICompoundAssignmentExpression (BinaryOperationKind.IntegerAdd) (OperationKind.CompoundAssignmentExpression, Type: System.Int32)
        Left: ILocalReferenceExpression: i (OperationKind.LocalReferenceExpression, Type: System.Int32)
        Right: ISyntheticLocalReferenceExpression (SynthesizedLocalKind.ForLoopStepValue) (OperationKind.SyntheticLocalReferenceExpression, Type: System.Int32)
  IBlockStatement (0 statements) (OperationKind.BlockStatement)
]]>.Value

            VerifyOperationTreeForTest(Of ForBlockSyntax)(source, expectedOperationTree)
        End Sub
    End Class
End Namespace
