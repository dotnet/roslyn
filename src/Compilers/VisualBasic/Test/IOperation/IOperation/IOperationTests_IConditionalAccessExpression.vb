' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

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
        Entering: {R1} {R2}

.locals {R1}
{
    CaptureIds: [0]
    .locals {R2}
    {
        CaptureIds: [1]
        Block[B1] - Block
            Predecessors: [B0]
            Statements (1)
                IFlowCaptureOperation: 1 (OperationKind.FlowCapture, Type: null, IsInvalid, IsImplicit) (Syntax: 'x')
                  Value: 
                    IParameterReferenceOperation: x (OperationKind.ParameterReference, Type: System.Nullable(Of S1), IsInvalid) (Syntax: 'x')

            Jump if True (Regular) to Block[B3]
                IIsNullOperation (OperationKind.IsNull, Type: System.Boolean, IsInvalid, IsImplicit) (Syntax: 'x')
                  Operand: 
                    IFlowCaptureReferenceOperation: 1 (OperationKind.FlowCaptureReference, Type: System.Nullable(Of S1), IsInvalid, IsImplicit) (Syntax: 'x')
                Leaving: {R2}

            Next (Regular) Block[B2]
        Block[B2] - Block
            Predecessors: [B1]
            Statements (1)
                IFlowCaptureOperation: 0 (OperationKind.FlowCapture, Type: null, IsInvalid, IsImplicit) (Syntax: '.P1()')
                  Value: 
                    IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.Nullable(Of System.Int32), IsInvalid, IsImplicit) (Syntax: '.P1()')
                      Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                        (WideningNullable)
                      Operand: 
                        IPropertyReferenceOperation: Property S1.P1 As System.Int32 (OperationKind.PropertyReference, Type: System.Int32, IsInvalid) (Syntax: '.P1()')
                          Instance Receiver: 
                            IInvocationOperation ( Function System.Nullable(Of S1).GetValueOrDefault() As S1) (OperationKind.Invocation, Type: S1, IsInvalid, IsImplicit) (Syntax: 'x')
                              Instance Receiver: 
                                IFlowCaptureReferenceOperation: 1 (OperationKind.FlowCaptureReference, Type: System.Nullable(Of S1), IsInvalid, IsImplicit) (Syntax: 'x')
                              Arguments(0)

            Next (Regular) Block[B4]
                Leaving: {R2}
    }

    Block[B3] - Block
        Predecessors: [B1]
        Statements (1)
            IFlowCaptureOperation: 0 (OperationKind.FlowCapture, Type: null, IsInvalid, IsImplicit) (Syntax: 'x')
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
                          IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: System.Nullable(Of System.Int32), IsInvalid, IsImplicit) (Syntax: 'x?.P1()')
                  Right: 
                    IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.Object, Constant: null, IsImplicit) (Syntax: 'Nothing')
                      Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                        (WideningNothingLiteral)
                      Operand: 
                        ILiteralOperation (OperationKind.Literal, Type: null, Constant: null) (Syntax: 'Nothing')

        Next (Regular) Block[B5]
            Leaving: {R1}
}

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
        Entering: {R1} {R2}

.locals {R1}
{
    CaptureIds: [0]
    .locals {R2}
    {
        CaptureIds: [1]
        Block[B1] - Block
            Predecessors: [B0]
            Statements (1)
                IFlowCaptureOperation: 1 (OperationKind.FlowCapture, Type: null, IsInvalid, IsImplicit) (Syntax: 'x')
                  Value: 
                    IParameterReferenceOperation: x (OperationKind.ParameterReference, Type: System.Nullable(Of S1), IsInvalid) (Syntax: 'x')

            Jump if True (Regular) to Block[B3]
                IIsNullOperation (OperationKind.IsNull, Type: System.Boolean, IsInvalid, IsImplicit) (Syntax: 'x')
                  Operand: 
                    IFlowCaptureReferenceOperation: 1 (OperationKind.FlowCaptureReference, Type: System.Nullable(Of S1), IsInvalid, IsImplicit) (Syntax: 'x')
                Leaving: {R2}

            Next (Regular) Block[B2]
        Block[B2] - Block
            Predecessors: [B1]
            Statements (1)
                IFlowCaptureOperation: 0 (OperationKind.FlowCapture, Type: null, IsInvalid, IsImplicit) (Syntax: '.P1')
                  Value: 
                    IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.Nullable(Of System.Int32), IsInvalid, IsImplicit) (Syntax: '.P1')
                      Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                        (WideningNullable)
                      Operand: 
                        IPropertyReferenceOperation: Property S1.P1 As System.Int32 (OperationKind.PropertyReference, Type: System.Int32, IsInvalid) (Syntax: '.P1')
                          Instance Receiver: 
                            IInvocationOperation ( Function System.Nullable(Of S1).GetValueOrDefault() As S1) (OperationKind.Invocation, Type: S1, IsInvalid, IsImplicit) (Syntax: 'x')
                              Instance Receiver: 
                                IFlowCaptureReferenceOperation: 1 (OperationKind.FlowCaptureReference, Type: System.Nullable(Of S1), IsInvalid, IsImplicit) (Syntax: 'x')
                              Arguments(0)

            Next (Regular) Block[B4]
                Leaving: {R2}
    }

    Block[B3] - Block
        Predecessors: [B1]
        Statements (1)
            IFlowCaptureOperation: 0 (OperationKind.FlowCapture, Type: null, IsInvalid, IsImplicit) (Syntax: 'x')
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
                          IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: System.Nullable(Of System.Int32), IsInvalid, IsImplicit) (Syntax: 'x?.P1')
                  Right: 
                    IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.Object, Constant: null, IsImplicit) (Syntax: 'Nothing')
                      Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                        (WideningNothingLiteral)
                      Operand: 
                        ILiteralOperation (OperationKind.Literal, Type: null, Constant: null) (Syntax: 'Nothing')

        Next (Regular) Block[B5]
            Leaving: {R1}
}

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
        Entering: {R1} {R2}

.locals {R1}
{
    CaptureIds: [0]
    .locals {R2}
    {
        CaptureIds: [1]
        Block[B1] - Block
            Predecessors: [B0]
            Statements (1)
                IFlowCaptureOperation: 1 (OperationKind.FlowCapture, Type: null, IsInvalid, IsImplicit) (Syntax: 'x')
                  Value: 
                    IParameterReferenceOperation: x (OperationKind.ParameterReference, Type: System.Nullable(Of S1), IsInvalid) (Syntax: 'x')

            Jump if True (Regular) to Block[B3]
                IIsNullOperation (OperationKind.IsNull, Type: System.Boolean, IsInvalid, IsImplicit) (Syntax: 'x')
                  Operand: 
                    IFlowCaptureReferenceOperation: 1 (OperationKind.FlowCaptureReference, Type: System.Nullable(Of S1), IsInvalid, IsImplicit) (Syntax: 'x')
                Leaving: {R2}

            Next (Regular) Block[B2]
        Block[B2] - Block
            Predecessors: [B1]
            Statements (1)
                IFlowCaptureOperation: 0 (OperationKind.FlowCapture, Type: null, IsInvalid, IsImplicit) (Syntax: '.F1')
                  Value: 
                    IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.Nullable(Of System.Int32), IsInvalid, IsImplicit) (Syntax: '.F1')
                      Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                        (WideningNullable)
                      Operand: 
                        IFieldReferenceOperation: S1.F1 As System.Int32 (OperationKind.FieldReference, Type: System.Int32, IsInvalid) (Syntax: '.F1')
                          Instance Receiver: 
                            IInvocationOperation ( Function System.Nullable(Of S1).GetValueOrDefault() As S1) (OperationKind.Invocation, Type: S1, IsInvalid, IsImplicit) (Syntax: 'x')
                              Instance Receiver: 
                                IFlowCaptureReferenceOperation: 1 (OperationKind.FlowCaptureReference, Type: System.Nullable(Of S1), IsInvalid, IsImplicit) (Syntax: 'x')
                              Arguments(0)

            Next (Regular) Block[B4]
                Leaving: {R2}
    }

    Block[B3] - Block
        Predecessors: [B1]
        Statements (1)
            IFlowCaptureOperation: 0 (OperationKind.FlowCapture, Type: null, IsInvalid, IsImplicit) (Syntax: 'x')
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
                          IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: System.Nullable(Of System.Int32), IsInvalid, IsImplicit) (Syntax: 'x?.F1')
                  Right: 
                    IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.Object, Constant: null, IsImplicit) (Syntax: 'Nothing')
                      Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                        (WideningNothingLiteral)
                      Operand: 
                        ILiteralOperation (OperationKind.Literal, Type: null, Constant: null) (Syntax: 'Nothing')

        Next (Regular) Block[B5]
            Leaving: {R1}
}

Block[B5] - Exit
    Predecessors: [B4]
    Statements (0)
]]>.Value

            VerifyFlowGraphAndDiagnosticsForTest(Of MethodBlockSyntax)(source, expectedFlowGraph, expectedDiagnostics)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation, CompilerFeature.Dataflow)>
        <Fact()>
        Public Sub ConditionalAccessFlow_09()
            Dim source = <![CDATA[
Imports System
Public Class C
    Sub M(c1 as C, c2 As C, result As C) 'BIND:"Sub M"
        result = c1?.P1.F1(c2?.P1)?.P1
    End Sub

    Property P1 As C
    Function F1(arg As Object) As C
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
    CaptureIds: [0] [1]
    Block[B1] - Block
        Predecessors: [B0]
        Statements (1)
            IFlowCaptureOperation: 0 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'result')
              Value: 
                IParameterReferenceOperation: result (OperationKind.ParameterReference, Type: C) (Syntax: 'result')

        Next (Regular) Block[B2]
            Entering: {R2} {R3} {R4}
    .locals {R2}
    {
        CaptureIds: [6]
        .locals {R3}
        {
            CaptureIds: [3] [4]
            .locals {R4}
            {
                CaptureIds: [2]
                Block[B2] - Block
                    Predecessors: [B1]
                    Statements (1)
                        IFlowCaptureOperation: 2 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'c1')
                          Value: 
                            IParameterReferenceOperation: c1 (OperationKind.ParameterReference, Type: C) (Syntax: 'c1')

                    Jump if True (Regular) to Block[B10]
                        IIsNullOperation (OperationKind.IsNull, Type: System.Boolean, IsImplicit) (Syntax: 'c1')
                          Operand: 
                            IFlowCaptureReferenceOperation: 2 (OperationKind.FlowCaptureReference, Type: C, IsImplicit) (Syntax: 'c1')
                        Leaving: {R4} {R3} {R2}

                    Next (Regular) Block[B3]
                Block[B3] - Block
                    Predecessors: [B2]
                    Statements (1)
                        IFlowCaptureOperation: 3 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: '.P1')
                          Value: 
                            IPropertyReferenceOperation: Property C.P1 As C (OperationKind.PropertyReference, Type: C) (Syntax: '.P1')
                              Instance Receiver: 
                                IFlowCaptureReferenceOperation: 2 (OperationKind.FlowCaptureReference, Type: C, IsImplicit) (Syntax: 'c1')

                    Next (Regular) Block[B4]
                        Leaving: {R4}
                        Entering: {R5}
            }
            .locals {R5}
            {
                CaptureIds: [5]
                Block[B4] - Block
                    Predecessors: [B3]
                    Statements (1)
                        IFlowCaptureOperation: 5 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'c2')
                          Value: 
                            IParameterReferenceOperation: c2 (OperationKind.ParameterReference, Type: C) (Syntax: 'c2')

                    Jump if True (Regular) to Block[B6]
                        IIsNullOperation (OperationKind.IsNull, Type: System.Boolean, IsImplicit) (Syntax: 'c2')
                          Operand: 
                            IFlowCaptureReferenceOperation: 5 (OperationKind.FlowCaptureReference, Type: C, IsImplicit) (Syntax: 'c2')
                        Leaving: {R5}

                    Next (Regular) Block[B5]
                Block[B5] - Block
                    Predecessors: [B4]
                    Statements (1)
                        IFlowCaptureOperation: 4 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: '.P1')
                          Value: 
                            IPropertyReferenceOperation: Property C.P1 As C (OperationKind.PropertyReference, Type: C) (Syntax: '.P1')
                              Instance Receiver: 
                                IFlowCaptureReferenceOperation: 5 (OperationKind.FlowCaptureReference, Type: C, IsImplicit) (Syntax: 'c2')

                    Next (Regular) Block[B7]
                        Leaving: {R5}
            }
            Block[B6] - Block
                Predecessors: [B4]
                Statements (1)
                    IFlowCaptureOperation: 4 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'c2')
                      Value: 
                        IDefaultValueOperation (OperationKind.DefaultValue, Type: C, Constant: null, IsImplicit) (Syntax: 'c2')

                Next (Regular) Block[B7]
            Block[B7] - Block
                Predecessors: [B5] [B6]
                Statements (1)
                    IFlowCaptureOperation: 6 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: '.P1.F1(c2?.P1)')
                      Value: 
                        IInvocationOperation ( Function C.F1(arg As System.Object) As C) (OperationKind.Invocation, Type: C) (Syntax: '.P1.F1(c2?.P1)')
                          Instance Receiver: 
                            IFlowCaptureReferenceOperation: 3 (OperationKind.FlowCaptureReference, Type: C, IsImplicit) (Syntax: '.P1')
                          Arguments(1):
                              IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: arg) (OperationKind.Argument, Type: null) (Syntax: 'c2?.P1')
                                IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.Object, IsImplicit) (Syntax: 'c2?.P1')
                                  Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: True, IsUserDefined: False) (MethodSymbol: null)
                                    (WideningReference)
                                  Operand: 
                                    IFlowCaptureReferenceOperation: 4 (OperationKind.FlowCaptureReference, Type: C, IsImplicit) (Syntax: 'c2?.P1')
                                InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                                OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)

                Next (Regular) Block[B8]
                    Leaving: {R3}
        }
        Block[B8] - Block
            Predecessors: [B7]
            Statements (0)
            Jump if True (Regular) to Block[B10]
                IIsNullOperation (OperationKind.IsNull, Type: System.Boolean, IsImplicit) (Syntax: '.P1.F1(c2?.P1)')
                  Operand: 
                    IFlowCaptureReferenceOperation: 6 (OperationKind.FlowCaptureReference, Type: C, IsImplicit) (Syntax: '.P1.F1(c2?.P1)')
                Leaving: {R2}

            Next (Regular) Block[B9]
        Block[B9] - Block
            Predecessors: [B8]
            Statements (1)
                IFlowCaptureOperation: 1 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: '.P1')
                  Value: 
                    IPropertyReferenceOperation: Property C.P1 As C (OperationKind.PropertyReference, Type: C) (Syntax: '.P1')
                      Instance Receiver: 
                        IFlowCaptureReferenceOperation: 6 (OperationKind.FlowCaptureReference, Type: C, IsImplicit) (Syntax: '.P1.F1(c2?.P1)')

            Next (Regular) Block[B11]
                Leaving: {R2}
    }
    Block[B10] - Block
        Predecessors: [B2] [B8]
        Statements (1)
            IFlowCaptureOperation: 1 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'c1?.P1.F1(c2?.P1)?.P1')
              Value: 
                IDefaultValueOperation (OperationKind.DefaultValue, Type: C, Constant: null, IsImplicit) (Syntax: 'c1?.P1.F1(c2?.P1)?.P1')

        Next (Regular) Block[B11]
    Block[B11] - Block
        Predecessors: [B9] [B10]
        Statements (1)
            IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'result = c1 ... c2?.P1)?.P1')
              Expression: 
                ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: C, IsImplicit) (Syntax: 'result = c1 ... c2?.P1)?.P1')
                  Left: 
                    IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: C, IsImplicit) (Syntax: 'result')
                  Right: 
                    IFlowCaptureReferenceOperation: 1 (OperationKind.FlowCaptureReference, Type: C, IsImplicit) (Syntax: 'c1?.P1.F1(c2?.P1)?.P1')

        Next (Regular) Block[B12]
            Leaving: {R1}
}
Block[B12] - Exit
    Predecessors: [B11]
    Statements (0)
]]>.Value

            VerifyFlowGraphAndDiagnosticsForTest(Of MethodBlockSyntax)(source, expectedFlowGraph, expectedDiagnostics)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation, CompilerFeature.Dataflow)>
        <Fact()>
        Public Sub ConditionalAccessFlow_10()
            Dim source =
<compilation>
    <file name="a.vb"><![CDATA[
Imports System
Imports System.Collections.Generic
Imports System.Linq
Imports System.Xml.Linq
Public Class C
    Public X As XElement
    Sub M(c As C, result As Object) 'BIND:"Sub M"
        result = c?.X?.@a1
    End Sub
End Class
]]>
    </file>
</compilation>

            Dim expectedDiagnostics = String.Empty

            Dim expectedFlowGraph = <![CDATA[
Block[B0] - Entry
    Statements (0)
    Next (Regular) Block[B1]
        Entering: {R1}
.locals {R1}
{
    CaptureIds: [0] [3]
    Block[B1] - Block
        Predecessors: [B0]
        Statements (1)
            IFlowCaptureOperation: 0 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'result')
              Value: 
                IParameterReferenceOperation: result (OperationKind.ParameterReference, Type: System.Object) (Syntax: 'result')
        Next (Regular) Block[B2]
            Entering: {R2} {R3}
    .locals {R2}
    {
        CaptureIds: [2]
        .locals {R3}
        {
            CaptureIds: [1]
            Block[B2] - Block
                Predecessors: [B1]
                Statements (1)
                    IFlowCaptureOperation: 1 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'c')
                      Value: 
                        IParameterReferenceOperation: c (OperationKind.ParameterReference, Type: C) (Syntax: 'c')
                Jump if True (Regular) to Block[B6]
                    IIsNullOperation (OperationKind.IsNull, Type: System.Boolean, IsImplicit) (Syntax: 'c')
                      Operand: 
                        IFlowCaptureReferenceOperation: 1 (OperationKind.FlowCaptureReference, Type: C, IsImplicit) (Syntax: 'c')
                    Leaving: {R3} {R2}
                Next (Regular) Block[B3]
            Block[B3] - Block
                Predecessors: [B2]
                Statements (1)
                    IFlowCaptureOperation: 2 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: '.X')
                      Value: 
                        IFieldReferenceOperation: C.X As System.Xml.Linq.XElement (OperationKind.FieldReference, Type: System.Xml.Linq.XElement) (Syntax: '.X')
                          Instance Receiver: 
                            IFlowCaptureReferenceOperation: 1 (OperationKind.FlowCaptureReference, Type: C, IsImplicit) (Syntax: 'c')
                Next (Regular) Block[B4]
                    Leaving: {R3}
        }
        Block[B4] - Block
            Predecessors: [B3]
            Statements (0)
            Jump if True (Regular) to Block[B6]
                IIsNullOperation (OperationKind.IsNull, Type: System.Boolean, IsImplicit) (Syntax: '.X')
                  Operand: 
                    IFlowCaptureReferenceOperation: 2 (OperationKind.FlowCaptureReference, Type: System.Xml.Linq.XElement, IsImplicit) (Syntax: '.X')
                Leaving: {R2}
            Next (Regular) Block[B5]
        Block[B5] - Block
            Predecessors: [B4]
            Statements (1)
                IFlowCaptureOperation: 3 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: '.@a1')
                  Value: 
                    IOperation:  (OperationKind.None, Type: System.String) (Syntax: '.@a1')
            Next (Regular) Block[B7]
                Leaving: {R2}
    }
    Block[B6] - Block
        Predecessors: [B2] [B4]
        Statements (1)
            IFlowCaptureOperation: 3 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'c?.X?.@a1')
              Value: 
                IDefaultValueOperation (OperationKind.DefaultValue, Type: System.String, Constant: null, IsImplicit) (Syntax: 'c?.X?.@a1')
        Next (Regular) Block[B7]
    Block[B7] - Block
        Predecessors: [B5] [B6]
        Statements (1)
            IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'result = c?.X?.@a1')
              Expression: 
                ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Object, IsImplicit) (Syntax: 'result = c?.X?.@a1')
                  Left: 
                    IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: System.Object, IsImplicit) (Syntax: 'result')
                  Right: 
                    IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.Object, IsImplicit) (Syntax: 'c?.X?.@a1')
                      Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: True, IsUserDefined: False) (MethodSymbol: null)
                        (WideningReference)
                      Operand: 
                        IFlowCaptureReferenceOperation: 3 (OperationKind.FlowCaptureReference, Type: System.String, IsImplicit) (Syntax: 'c?.X?.@a1')
        Next (Regular) Block[B8]
            Leaving: {R1}
}
Block[B8] - Exit
    Predecessors: [B7]
    Statements (0)
]]>.Value

            Dim comp = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntime(source, Net451XmlReferences)
            VerifyFlowGraphAndDiagnosticsForTest(Of MethodBlockSyntax)(comp, expectedFlowGraph, expectedDiagnostics)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation, CompilerFeature.Dataflow)>
        <Fact()>
        Public Sub ConditionalAccessFlow_11()
            Dim source =
<compilation>
    <file name="a.vb"><![CDATA[
Imports System
Imports System.Collections.Generic
Imports System.Linq
Imports System.Xml.Linq
Public Class C
    Public X As XElement
    Sub M(c As C, result As Object) 'BIND:"Sub M"
        result = c?.X.@a1
    End Sub
End Class
]]>
    </file>
</compilation>

            Dim expectedDiagnostics = String.Empty

            Dim expectedFlowGraph = <![CDATA[
Block[B0] - Entry
    Statements (0)
    Next (Regular) Block[B1]
        Entering: {R1}

.locals {R1}
{
    CaptureIds: [0] [2]
    Block[B1] - Block
        Predecessors: [B0]
        Statements (1)
            IFlowCaptureOperation: 0 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'result')
              Value: 
                IParameterReferenceOperation: result (OperationKind.ParameterReference, Type: System.Object) (Syntax: 'result')

        Next (Regular) Block[B2]
            Entering: {R2}

    .locals {R2}
    {
        CaptureIds: [1]
        Block[B2] - Block
            Predecessors: [B1]
            Statements (1)
                IFlowCaptureOperation: 1 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'c')
                  Value: 
                    IParameterReferenceOperation: c (OperationKind.ParameterReference, Type: C) (Syntax: 'c')

            Jump if True (Regular) to Block[B4]
                IIsNullOperation (OperationKind.IsNull, Type: System.Boolean, IsImplicit) (Syntax: 'c')
                  Operand: 
                    IFlowCaptureReferenceOperation: 1 (OperationKind.FlowCaptureReference, Type: C, IsImplicit) (Syntax: 'c')
                Leaving: {R2}

            Next (Regular) Block[B3]
        Block[B3] - Block
            Predecessors: [B2]
            Statements (1)
                IFlowCaptureOperation: 2 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: '.X.@a1')
                  Value: 
                    IOperation:  (OperationKind.None, Type: System.String) (Syntax: '.X.@a1')

            Next (Regular) Block[B5]
                Leaving: {R2}
    }

    Block[B4] - Block
        Predecessors: [B2]
        Statements (1)
            IFlowCaptureOperation: 2 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'c')
              Value: 
                IDefaultValueOperation (OperationKind.DefaultValue, Type: System.String, Constant: null, IsImplicit) (Syntax: 'c')

        Next (Regular) Block[B5]
    Block[B5] - Block
        Predecessors: [B3] [B4]
        Statements (1)
            IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'result = c?.X.@a1')
              Expression: 
                ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Object, IsImplicit) (Syntax: 'result = c?.X.@a1')
                  Left: 
                    IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: System.Object, IsImplicit) (Syntax: 'result')
                  Right: 
                    IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.Object, IsImplicit) (Syntax: 'c?.X.@a1')
                      Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: True, IsUserDefined: False) (MethodSymbol: null)
                        (WideningReference)
                      Operand: 
                        IFlowCaptureReferenceOperation: 2 (OperationKind.FlowCaptureReference, Type: System.String, IsImplicit) (Syntax: 'c?.X.@a1')

        Next (Regular) Block[B6]
            Leaving: {R1}
}

Block[B6] - Exit
    Predecessors: [B5]
    Statements (0)
]]>.Value

            Dim comp = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntime(source, Net451XmlReferences)
            VerifyFlowGraphAndDiagnosticsForTest(Of MethodBlockSyntax)(comp, expectedFlowGraph, expectedDiagnostics)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation, CompilerFeature.Dataflow)>
        <Fact()>
        Public Sub ConditionalAccessFlow_12()
            Dim source =
<compilation>
    <file name="a.vb"><![CDATA[
Imports System
Imports System.Collections.Generic
Imports System.Linq
Imports System.Xml.Linq
Public Class C
    Public X As XElement
    Sub M(c As C, result As Object) 'BIND:"Sub M"
        result = c.X?.@a1
    End Sub
End Class
]]>
    </file>
</compilation>

            Dim expectedDiagnostics = String.Empty

            Dim expectedFlowGraph = <![CDATA[
Block[B0] - Entry
    Statements (0)
    Next (Regular) Block[B1]
        Entering: {R1}

.locals {R1}
{
    CaptureIds: [0] [2]
    Block[B1] - Block
        Predecessors: [B0]
        Statements (1)
            IFlowCaptureOperation: 0 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'result')
              Value: 
                IParameterReferenceOperation: result (OperationKind.ParameterReference, Type: System.Object) (Syntax: 'result')

        Next (Regular) Block[B2]
            Entering: {R2}

    .locals {R2}
    {
        CaptureIds: [1]
        Block[B2] - Block
            Predecessors: [B1]
            Statements (1)
                IFlowCaptureOperation: 1 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'c.X')
                  Value: 
                    IFieldReferenceOperation: C.X As System.Xml.Linq.XElement (OperationKind.FieldReference, Type: System.Xml.Linq.XElement) (Syntax: 'c.X')
                      Instance Receiver: 
                        IParameterReferenceOperation: c (OperationKind.ParameterReference, Type: C) (Syntax: 'c')

            Jump if True (Regular) to Block[B4]
                IIsNullOperation (OperationKind.IsNull, Type: System.Boolean, IsImplicit) (Syntax: 'c.X')
                  Operand: 
                    IFlowCaptureReferenceOperation: 1 (OperationKind.FlowCaptureReference, Type: System.Xml.Linq.XElement, IsImplicit) (Syntax: 'c.X')
                Leaving: {R2}

            Next (Regular) Block[B3]
        Block[B3] - Block
            Predecessors: [B2]
            Statements (1)
                IFlowCaptureOperation: 2 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: '.@a1')
                  Value: 
                    IOperation:  (OperationKind.None, Type: System.String) (Syntax: '.@a1')

            Next (Regular) Block[B5]
                Leaving: {R2}
    }

    Block[B4] - Block
        Predecessors: [B2]
        Statements (1)
            IFlowCaptureOperation: 2 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'c.X')
              Value: 
                IDefaultValueOperation (OperationKind.DefaultValue, Type: System.String, Constant: null, IsImplicit) (Syntax: 'c.X')

        Next (Regular) Block[B5]
    Block[B5] - Block
        Predecessors: [B3] [B4]
        Statements (1)
            IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'result = c.X?.@a1')
              Expression: 
                ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Object, IsImplicit) (Syntax: 'result = c.X?.@a1')
                  Left: 
                    IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: System.Object, IsImplicit) (Syntax: 'result')
                  Right: 
                    IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.Object, IsImplicit) (Syntax: 'c.X?.@a1')
                      Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: True, IsUserDefined: False) (MethodSymbol: null)
                        (WideningReference)
                      Operand: 
                        IFlowCaptureReferenceOperation: 2 (OperationKind.FlowCaptureReference, Type: System.String, IsImplicit) (Syntax: 'c.X?.@a1')

        Next (Regular) Block[B6]
            Leaving: {R1}
}

Block[B6] - Exit
    Predecessors: [B5]
    Statements (0)
]]>.Value

            Dim comp = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntime(source, Net451XmlReferences)
            VerifyFlowGraphAndDiagnosticsForTest(Of MethodBlockSyntax)(comp, expectedFlowGraph, expectedDiagnostics)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation, CompilerFeature.Dataflow)>
        <Fact()>
        Public Sub ConditionalAccessFlow_13()
            Dim source =
<compilation>
    <file name="a.vb"><![CDATA[
Imports System
Imports System.Collections.Generic
Imports System.Linq
Imports System.Xml.Linq
Public Class C
    Public X As XElement
    Sub M(c As C, result As Object) 'BIND:"Sub M"
        result = c?.X?.@a1?.<e1>
    End Sub
End Class
]]>
    </file>
</compilation>

            Dim expectedDiagnostics = <![CDATA[
BC36807: XML elements cannot be selected from type 'String'.
        result = c?.X?.@a1?.<e1>
                           ~~~~~
]]>.Value

            Dim expectedFlowGraph = <![CDATA[
Block[B0] - Entry
    Statements (0)
    Next (Regular) Block[B1]
        Entering: {R1}

.locals {R1}
{
    CaptureIds: [0] [3]
    Block[B1] - Block
        Predecessors: [B0]
        Statements (1)
            IFlowCaptureOperation: 0 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'result')
              Value: 
                IParameterReferenceOperation: result (OperationKind.ParameterReference, Type: System.Object) (Syntax: 'result')

        Next (Regular) Block[B2]
            Entering: {R2} {R3}

    .locals {R2}
    {
        CaptureIds: [2]
        .locals {R3}
        {
            CaptureIds: [1]
            Block[B2] - Block
                Predecessors: [B1]
                Statements (1)
                    IFlowCaptureOperation: 1 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'c')
                      Value: 
                        IParameterReferenceOperation: c (OperationKind.ParameterReference, Type: C) (Syntax: 'c')

                Jump if True (Regular) to Block[B8]
                    IIsNullOperation (OperationKind.IsNull, Type: System.Boolean, IsImplicit) (Syntax: 'c')
                      Operand: 
                        IFlowCaptureReferenceOperation: 1 (OperationKind.FlowCaptureReference, Type: C, IsImplicit) (Syntax: 'c')
                    Leaving: {R3} {R2}

                Next (Regular) Block[B3]
            Block[B3] - Block
                Predecessors: [B2]
                Statements (1)
                    IFlowCaptureOperation: 2 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: '.X')
                      Value: 
                        IFieldReferenceOperation: C.X As System.Xml.Linq.XElement (OperationKind.FieldReference, Type: System.Xml.Linq.XElement) (Syntax: '.X')
                          Instance Receiver: 
                            IFlowCaptureReferenceOperation: 1 (OperationKind.FlowCaptureReference, Type: C, IsImplicit) (Syntax: 'c')

                Next (Regular) Block[B4]
                    Leaving: {R3}
        }

        Block[B4] - Block
            Predecessors: [B3]
            Statements (0)
            Jump if True (Regular) to Block[B8]
                IIsNullOperation (OperationKind.IsNull, Type: System.Boolean, IsImplicit) (Syntax: '.X')
                  Operand: 
                    IFlowCaptureReferenceOperation: 2 (OperationKind.FlowCaptureReference, Type: System.Xml.Linq.XElement, IsImplicit) (Syntax: '.X')
                Leaving: {R2}

            Next (Regular) Block[B5]
                Entering: {R4}

        .locals {R4}
        {
            CaptureIds: [4]
            Block[B5] - Block
                Predecessors: [B4]
                Statements (1)
                    IFlowCaptureOperation: 4 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: '.@a1')
                      Value: 
                        IOperation:  (OperationKind.None, Type: System.String) (Syntax: '.@a1')

                Jump if True (Regular) to Block[B7]
                    IIsNullOperation (OperationKind.IsNull, Type: System.Boolean, IsImplicit) (Syntax: '.@a1')
                      Operand: 
                        IFlowCaptureReferenceOperation: 4 (OperationKind.FlowCaptureReference, Type: System.String, IsImplicit) (Syntax: '.@a1')
                    Leaving: {R4}

                Next (Regular) Block[B6]
            Block[B6] - Block
                Predecessors: [B5]
                Statements (1)
                    IFlowCaptureOperation: 3 (OperationKind.FlowCapture, Type: null, IsInvalid, IsImplicit) (Syntax: '.<e1>')
                      Value: 
                        IOperation:  (OperationKind.None, Type: ?, IsInvalid) (Syntax: '.<e1>')

                Next (Regular) Block[B9]
                    Leaving: {R4} {R2}
        }

        Block[B7] - Block
            Predecessors: [B5]
            Statements (1)
                IFlowCaptureOperation: 3 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: '.@a1')
                  Value: 
                    IDefaultValueOperation (OperationKind.DefaultValue, Type: ?, Constant: null, IsImplicit) (Syntax: '.@a1')

            Next (Regular) Block[B9]
                Leaving: {R2}
    }

    Block[B8] - Block
        Predecessors: [B2] [B4]
        Statements (1)
            IFlowCaptureOperation: 3 (OperationKind.FlowCapture, Type: null, IsInvalid, IsImplicit) (Syntax: 'c?.X?.@a1?.<e1>')
              Value: 
                IDefaultValueOperation (OperationKind.DefaultValue, Type: ?, Constant: null, IsInvalid, IsImplicit) (Syntax: 'c?.X?.@a1?.<e1>')

        Next (Regular) Block[B9]
    Block[B9] - Block
        Predecessors: [B6] [B7] [B8]
        Statements (1)
            IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null, IsInvalid) (Syntax: 'result = c?.X?.@a1?.<e1>')
              Expression: 
                ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Object, IsInvalid, IsImplicit) (Syntax: 'result = c?.X?.@a1?.<e1>')
                  Left: 
                    IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: System.Object, IsImplicit) (Syntax: 'result')
                  Right: 
                    IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.Object, IsInvalid, IsImplicit) (Syntax: 'c?.X?.@a1?.<e1>')
                      Conversion: CommonConversion (Exists: False, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                        (DelegateRelaxationLevelNone)
                      Operand: 
                        IFlowCaptureReferenceOperation: 3 (OperationKind.FlowCaptureReference, Type: ?, IsInvalid, IsImplicit) (Syntax: 'c?.X?.@a1?.<e1>')

        Next (Regular) Block[B10]
            Leaving: {R1}
}

Block[B10] - Exit
    Predecessors: [B9]
    Statements (0)
]]>.Value

            Dim comp = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntime(source, Net451XmlReferences)
            VerifyFlowGraphAndDiagnosticsForTest(Of MethodBlockSyntax)(comp, expectedFlowGraph, expectedDiagnostics)
        End Sub
    End Class
End Namespace
