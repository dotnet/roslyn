' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports Microsoft.CodeAnalysis.Test.Utilities
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax
Imports Roslyn.Test.Utilities

Namespace Microsoft.CodeAnalysis.VisualBasic.UnitTests.Semantics

    Partial Public Class IOperationTests
        Inherits SemanticModelTestBase

        <CompilerTrait(CompilerFeature.IOperation)>
        <Fact()>
        Public Sub IConditionalAccessExpression_SimpleMethodAccess()
            Dim source = <![CDATA[
Option Strict On

Public Class C1
    Public Sub M1()
        Dim o As New Object
        o?.ToString()'BIND:"o?.ToString()"
    End Sub
End Class]]>.Value

            Dim expectedOperationTree = <![CDATA[
IConditionalAccessOperation (OperationKind.ConditionalAccess, Type: System.Void) (Syntax: 'o?.ToString()')
  Operation: 
    ILocalReferenceOperation: o (OperationKind.LocalReference, Type: System.Object) (Syntax: 'o')
  WhenNotNull: 
    IInvocationOperation (virtual Function System.Object.ToString() As System.String) (OperationKind.Invocation, Type: System.String) (Syntax: '.ToString()')
      Instance Receiver: 
        IConditionalAccessInstanceOperation (OperationKind.ConditionalAccessInstance, Type: System.Object, IsImplicit) (Syntax: 'o')
      Arguments(0)
]]>.Value

            Dim expectedDiagnostics = String.Empty

            VerifyOperationTreeAndDiagnosticsForTest(Of ConditionalAccessExpressionSyntax)(source, expectedOperationTree, expectedDiagnostics)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation)>
        <Fact()>
        Public Sub IConditionalAccessExpression_SimplePropertyAccess()
            Dim source = <![CDATA[
Option Strict On

Public Class C1

    Public ReadOnly Property Prop1 As Integer

    Public Sub M1()
        Dim c1 As C1 = Nothing
        Dim propValue = c1?.Prop1'BIND:"c1?.Prop1"
    End Sub
End Class]]>.Value

            Dim expectedOperationTree = <![CDATA[
IConditionalAccessOperation (OperationKind.ConditionalAccess, Type: System.Nullable(Of System.Int32)) (Syntax: 'c1?.Prop1')
  Operation: 
    ILocalReferenceOperation: c1 (OperationKind.LocalReference, Type: C1) (Syntax: 'c1')
  WhenNotNull: 
    IPropertyReferenceOperation: ReadOnly Property C1.Prop1 As System.Int32 (OperationKind.PropertyReference, Type: System.Int32) (Syntax: '.Prop1')
      Instance Receiver: 
        IConditionalAccessInstanceOperation (OperationKind.ConditionalAccessInstance, Type: C1, IsImplicit) (Syntax: 'c1')
]]>.Value

            Dim expectedDiagnostics = String.Empty

            VerifyOperationTreeAndDiagnosticsForTest(Of ConditionalAccessExpressionSyntax)(source, expectedOperationTree, expectedDiagnostics)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation)>
        <Fact()>
        <WorkItem(23009, "https://github.com/dotnet/roslyn/issues/23009")>
        Public Sub IConditionalAccessExpression_ErrorReceiver()
            Dim source = <![CDATA[
Option Strict On

Public Class C1
    Public Sub M1()
        MyBase?.ToString()'BIND:"MyBase?.ToString()"
    End Sub
End Class]]>.Value

            Dim expectedOperationTree = <![CDATA[
IConditionalAccessOperation (OperationKind.ConditionalAccess, Type: System.Void, IsInvalid) (Syntax: 'MyBase?.ToString()')
  Operation: 
    IInstanceReferenceOperation (ReferenceKind: ContainingTypeInstance) (OperationKind.InstanceReference, Type: System.Object, IsInvalid) (Syntax: 'MyBase')
  WhenNotNull: 
    IInvocationOperation (virtual Function System.Object.ToString() As System.String) (OperationKind.Invocation, Type: System.String) (Syntax: '.ToString()')
      Instance Receiver: 
        IConditionalAccessInstanceOperation (OperationKind.ConditionalAccessInstance, Type: System.Object, IsInvalid, IsImplicit) (Syntax: 'MyBase')
      Arguments(0)
]]>.Value

            Dim expectedDiagnostics = <![CDATA[
BC32027: 'MyBase' must be followed by '.' and an identifier.
        MyBase?.ToString()'BIND:"MyBase?.ToString()"
        ~~~~~~
]]>.Value

            VerifyOperationTreeAndDiagnosticsForTest(Of ConditionalAccessExpressionSyntax)(source, expectedOperationTree, expectedDiagnostics)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation, CompilerFeature.Dataflow)>
        <Fact()>
        Public Sub ConditionalAccessFlow_06()
            Dim source = <![CDATA[
Imports System
Public Class C
    Sub M(x As S1?) 'BIND:"Sub M"
        x?.P1() = Nothing
    End Sub
End Class

Public Structure S1
    Property P1() As Integer
End Structure
]]>.Value

            Dim expectedDiagnostics = <![CDATA[
BC30068: Expression is a value and therefore cannot be the target of an assignment.
        x?.P1() = Nothing
        ~~~~~~~
]]>.Value

            Dim expectedFlowGraph = <![CDATA[
Block[B0] - Entry
    Statements (0)
    Next (Regular) Block[B1]
Block[B1] - Block
    Predecessors: [B0]
    Statements (1)
        IFlowCaptureOperation: 0 (OperationKind.FlowCapture, Type: null, IsInvalid, IsImplicit) (Syntax: 'x')
          Value: 
            IParameterReferenceOperation: x (OperationKind.ParameterReference, Type: System.Nullable(Of S1), IsInvalid) (Syntax: 'x')

    Jump if True (Regular) to Block[B3]
        IIsNullOperation (OperationKind.IsNull, Type: System.Boolean, IsInvalid, IsImplicit) (Syntax: 'x')
          Operand: 
            IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: System.Nullable(Of S1), IsInvalid, IsImplicit) (Syntax: 'x')

    Next (Regular) Block[B2]
Block[B2] - Block
    Predecessors: [B1]
    Statements (1)
        IFlowCaptureOperation: 1 (OperationKind.FlowCapture, Type: null, IsInvalid, IsImplicit) (Syntax: '.P1()')
          Value: 
            IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.Nullable(Of System.Int32), IsInvalid, IsImplicit) (Syntax: '.P1()')
              Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                (WideningNullable)
              Operand: 
                IPropertyReferenceOperation: Property S1.P1 As System.Int32 (OperationKind.PropertyReference, Type: System.Int32, IsInvalid) (Syntax: '.P1()')
                  Instance Receiver: 
                    IInvocationOperation ( Function System.Nullable(Of S1).GetValueOrDefault() As S1) (OperationKind.Invocation, Type: S1, IsInvalid, IsImplicit) (Syntax: 'x')
                      Instance Receiver: 
                        IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: System.Nullable(Of S1), IsInvalid, IsImplicit) (Syntax: 'x')
                      Arguments(0)

    Next (Regular) Block[B4]
Block[B3] - Block
    Predecessors: [B1]
    Statements (1)
        IFlowCaptureOperation: 1 (OperationKind.FlowCapture, Type: null, IsInvalid, IsImplicit) (Syntax: 'x')
          Value: 
            IDefaultValueOperation (OperationKind.DefaultValue, Type: System.Nullable(Of System.Int32), IsInvalid, IsImplicit) (Syntax: 'x')

    Next (Regular) Block[B4]
Block[B4] - Block
    Predecessors: [B2] [B3]
    Statements (1)
        IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null, IsInvalid) (Syntax: 'x?.P1() = Nothing')
          Expression: 
            ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: ?, IsInvalid, IsImplicit) (Syntax: 'x?.P1() = Nothing')
              Left: 
                IInvalidOperation (OperationKind.Invalid, Type: ?, IsInvalid, IsImplicit) (Syntax: 'x?.P1()')
                  Children(1):
                      IFlowCaptureReferenceOperation: 1 (OperationKind.FlowCaptureReference, Type: System.Nullable(Of System.Int32), IsInvalid, IsImplicit) (Syntax: 'x?.P1()')
              Right: 
                IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.Object, Constant: null, IsImplicit) (Syntax: 'Nothing')
                  Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                    (WideningNothingLiteral)
                  Operand: 
                    ILiteralOperation (OperationKind.Literal, Type: null, Constant: null) (Syntax: 'Nothing')

    Next (Regular) Block[B5]
Block[B5] - Exit
    Predecessors: [B4]
    Statements (0)
]]>.Value

            VerifyFlowGraphAndDiagnosticsForTest(Of MethodBlockSyntax)(source, expectedFlowGraph, expectedDiagnostics)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation, CompilerFeature.Dataflow)>
        <Fact()>
        Public Sub ConditionalAccessFlow_07()
            Dim source = <![CDATA[
Imports System
Public Class C
    Sub M(x As S1?) 'BIND:"Sub M"
        x?.P1 = Nothing
    End Sub
End Class

Public Structure S1
    Property P1() As Integer
End Structure
]]>.Value

            Dim expectedDiagnostics = <![CDATA[
BC30068: Expression is a value and therefore cannot be the target of an assignment.
        x?.P1 = Nothing
        ~~~~~
]]>.Value

            Dim expectedFlowGraph = <![CDATA[
Block[B0] - Entry
    Statements (0)
    Next (Regular) Block[B1]
Block[B1] - Block
    Predecessors: [B0]
    Statements (1)
        IFlowCaptureOperation: 0 (OperationKind.FlowCapture, Type: null, IsInvalid, IsImplicit) (Syntax: 'x')
          Value: 
            IParameterReferenceOperation: x (OperationKind.ParameterReference, Type: System.Nullable(Of S1), IsInvalid) (Syntax: 'x')

    Jump if True (Regular) to Block[B3]
        IIsNullOperation (OperationKind.IsNull, Type: System.Boolean, IsInvalid, IsImplicit) (Syntax: 'x')
          Operand: 
            IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: System.Nullable(Of S1), IsInvalid, IsImplicit) (Syntax: 'x')

    Next (Regular) Block[B2]
Block[B2] - Block
    Predecessors: [B1]
    Statements (1)
        IFlowCaptureOperation: 1 (OperationKind.FlowCapture, Type: null, IsInvalid, IsImplicit) (Syntax: '.P1')
          Value: 
            IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.Nullable(Of System.Int32), IsInvalid, IsImplicit) (Syntax: '.P1')
              Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                (WideningNullable)
              Operand: 
                IPropertyReferenceOperation: Property S1.P1 As System.Int32 (OperationKind.PropertyReference, Type: System.Int32, IsInvalid) (Syntax: '.P1')
                  Instance Receiver: 
                    IInvocationOperation ( Function System.Nullable(Of S1).GetValueOrDefault() As S1) (OperationKind.Invocation, Type: S1, IsInvalid, IsImplicit) (Syntax: 'x')
                      Instance Receiver: 
                        IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: System.Nullable(Of S1), IsInvalid, IsImplicit) (Syntax: 'x')
                      Arguments(0)

    Next (Regular) Block[B4]
Block[B3] - Block
    Predecessors: [B1]
    Statements (1)
        IFlowCaptureOperation: 1 (OperationKind.FlowCapture, Type: null, IsInvalid, IsImplicit) (Syntax: 'x')
          Value: 
            IDefaultValueOperation (OperationKind.DefaultValue, Type: System.Nullable(Of System.Int32), IsInvalid, IsImplicit) (Syntax: 'x')

    Next (Regular) Block[B4]
Block[B4] - Block
    Predecessors: [B2] [B3]
    Statements (1)
        IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null, IsInvalid) (Syntax: 'x?.P1 = Nothing')
          Expression: 
            ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: ?, IsInvalid, IsImplicit) (Syntax: 'x?.P1 = Nothing')
              Left: 
                IInvalidOperation (OperationKind.Invalid, Type: ?, IsInvalid, IsImplicit) (Syntax: 'x?.P1')
                  Children(1):
                      IFlowCaptureReferenceOperation: 1 (OperationKind.FlowCaptureReference, Type: System.Nullable(Of System.Int32), IsInvalid, IsImplicit) (Syntax: 'x?.P1')
              Right: 
                IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.Object, Constant: null, IsImplicit) (Syntax: 'Nothing')
                  Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                    (WideningNothingLiteral)
                  Operand: 
                    ILiteralOperation (OperationKind.Literal, Type: null, Constant: null) (Syntax: 'Nothing')

    Next (Regular) Block[B5]
Block[B5] - Exit
    Predecessors: [B4]
    Statements (0)
]]>.Value

            VerifyFlowGraphAndDiagnosticsForTest(Of MethodBlockSyntax)(source, expectedFlowGraph, expectedDiagnostics)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation, CompilerFeature.Dataflow)>
        <Fact()>
        Public Sub ConditionalAccessFlow_08()
            Dim source = <![CDATA[
Imports System
Public Class C
    Sub M(x As S1?) 'BIND:"Sub M"
        x?.F1 = Nothing
    End Sub
End Class

Public Structure S1
    Public F1 As Integer
End Structure
]]>.Value

            Dim expectedDiagnostics = <![CDATA[
BC30068: Expression is a value and therefore cannot be the target of an assignment.
        x?.F1 = Nothing
        ~~~~~
]]>.Value

            Dim expectedFlowGraph = <![CDATA[
Block[B0] - Entry
    Statements (0)
    Next (Regular) Block[B1]
Block[B1] - Block
    Predecessors: [B0]
    Statements (1)
        IFlowCaptureOperation: 0 (OperationKind.FlowCapture, Type: null, IsInvalid, IsImplicit) (Syntax: 'x')
          Value: 
            IParameterReferenceOperation: x (OperationKind.ParameterReference, Type: System.Nullable(Of S1), IsInvalid) (Syntax: 'x')

    Jump if True (Regular) to Block[B3]
        IIsNullOperation (OperationKind.IsNull, Type: System.Boolean, IsInvalid, IsImplicit) (Syntax: 'x')
          Operand: 
            IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: System.Nullable(Of S1), IsInvalid, IsImplicit) (Syntax: 'x')

    Next (Regular) Block[B2]
Block[B2] - Block
    Predecessors: [B1]
    Statements (1)
        IFlowCaptureOperation: 1 (OperationKind.FlowCapture, Type: null, IsInvalid, IsImplicit) (Syntax: '.F1')
          Value: 
            IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.Nullable(Of System.Int32), IsInvalid, IsImplicit) (Syntax: '.F1')
              Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                (WideningNullable)
              Operand: 
                IFieldReferenceOperation: S1.F1 As System.Int32 (OperationKind.FieldReference, Type: System.Int32, IsInvalid) (Syntax: '.F1')
                  Instance Receiver: 
                    IInvocationOperation ( Function System.Nullable(Of S1).GetValueOrDefault() As S1) (OperationKind.Invocation, Type: S1, IsInvalid, IsImplicit) (Syntax: 'x')
                      Instance Receiver: 
                        IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: System.Nullable(Of S1), IsInvalid, IsImplicit) (Syntax: 'x')
                      Arguments(0)

    Next (Regular) Block[B4]
Block[B3] - Block
    Predecessors: [B1]
    Statements (1)
        IFlowCaptureOperation: 1 (OperationKind.FlowCapture, Type: null, IsInvalid, IsImplicit) (Syntax: 'x')
          Value: 
            IDefaultValueOperation (OperationKind.DefaultValue, Type: System.Nullable(Of System.Int32), IsInvalid, IsImplicit) (Syntax: 'x')

    Next (Regular) Block[B4]
Block[B4] - Block
    Predecessors: [B2] [B3]
    Statements (1)
        IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null, IsInvalid) (Syntax: 'x?.F1 = Nothing')
          Expression: 
            ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: ?, IsInvalid, IsImplicit) (Syntax: 'x?.F1 = Nothing')
              Left: 
                IInvalidOperation (OperationKind.Invalid, Type: ?, IsInvalid, IsImplicit) (Syntax: 'x?.F1')
                  Children(1):
                      IFlowCaptureReferenceOperation: 1 (OperationKind.FlowCaptureReference, Type: System.Nullable(Of System.Int32), IsInvalid, IsImplicit) (Syntax: 'x?.F1')
              Right: 
                IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.Object, Constant: null, IsImplicit) (Syntax: 'Nothing')
                  Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                    (WideningNothingLiteral)
                  Operand: 
                    ILiteralOperation (OperationKind.Literal, Type: null, Constant: null) (Syntax: 'Nothing')

    Next (Regular) Block[B5]
Block[B5] - Exit
    Predecessors: [B4]
    Statements (0)
]]>.Value

            VerifyFlowGraphAndDiagnosticsForTest(Of MethodBlockSyntax)(source, expectedFlowGraph, expectedDiagnostics)
        End Sub

    End Class
End Namespace
