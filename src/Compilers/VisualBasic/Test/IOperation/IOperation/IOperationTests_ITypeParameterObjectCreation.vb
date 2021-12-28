' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports Microsoft.CodeAnalysis.Test.Utilities
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.VisualBasic.UnitTests.Semantics

    Partial Public Class IOperationTests
        Inherits SemanticModelTestBase

        <CompilerTrait(CompilerFeature.IOperation)>
        <Fact()>
        Public Sub TypeParameterObjectCreation_Simple()
            Dim source = <![CDATA[
Class C1
    Sub M(Of T As {C1, New})(t1 As T)
        t1 = New T()'BIND:"New T()"
    End Sub
End Class]]>.Value

            Dim expectedDiagnostics = String.Empty

            Dim expectedOperationTree = <![CDATA[
ITypeParameterObjectCreationOperation (OperationKind.TypeParameterObjectCreation, Type: T) (Syntax: 'New T()')
  Initializer: 
    null
]]>.Value

            VerifyOperationTreeAndDiagnosticsForTest(Of ObjectCreationExpressionSyntax)(source, expectedOperationTree, expectedDiagnostics)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation)>
        <Fact()>
        Public Sub TypeParameterObjectCreation_WithObjectInitializer()
            Dim source = <![CDATA[
Class C1
    Sub M(Of T As {C1, New})(t1 As T)
        t1 = New T() With {.P1 = 1}'BIND:"New T() With {.P1 = 1}"
    End Sub
    Public Property P1 As Integer
End Class]]>.Value

            Dim expectedDiagnostics = String.Empty

            Dim expectedOperationTree = <![CDATA[
ITypeParameterObjectCreationOperation (OperationKind.TypeParameterObjectCreation, Type: T) (Syntax: 'New T() With {.P1 = 1}')
  Initializer: 
    IObjectOrCollectionInitializerOperation (OperationKind.ObjectOrCollectionInitializer, Type: T) (Syntax: 'With {.P1 = 1}')
      Initializers(1):
          ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Void) (Syntax: '.P1 = 1')
            Left: 
              IPropertyReferenceOperation: Property C1.P1 As System.Int32 (OperationKind.PropertyReference, Type: System.Int32) (Syntax: 'P1')
                Instance Receiver: 
                  IInstanceReferenceOperation (ReferenceKind: ImplicitReceiver) (OperationKind.InstanceReference, Type: T, IsImplicit) (Syntax: 'New T() With {.P1 = 1}')
            Right: 
              ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 1) (Syntax: '1')
]]>.Value

            VerifyOperationTreeAndDiagnosticsForTest(Of ObjectCreationExpressionSyntax)(source, expectedOperationTree, expectedDiagnostics)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation)>
        <Fact()>
        Public Sub TypeParameterObjectCreation_WithCollectionInitializer()
            Dim source = <![CDATA[
Imports System.Collections.Generic
Class C1
    Sub M(Of T As {C1, IEnumerable(Of Integer), New})(t1 As T)
        t1 = New T() From {1}'BIND:"New T() From {1}"
    End Sub
    Public Sub Add(i As Integer)
    End Sub
End Class]]>.Value

            Dim expectedDiagnostics = String.Empty

            Dim expectedOperationTree = <![CDATA[
ITypeParameterObjectCreationOperation (OperationKind.TypeParameterObjectCreation, Type: T) (Syntax: 'New T() From {1}')
  Initializer: 
    IObjectOrCollectionInitializerOperation (OperationKind.ObjectOrCollectionInitializer, Type: T) (Syntax: 'From {1}')
      Initializers(1):
          IInvocationOperation ( Sub C1.Add(i As System.Int32)) (OperationKind.Invocation, Type: System.Void, IsImplicit) (Syntax: '1')
            Instance Receiver: 
              IInstanceReferenceOperation (ReferenceKind: ImplicitReceiver) (OperationKind.InstanceReference, Type: T, IsImplicit) (Syntax: 'New T() From {1}')
            Arguments(1):
                IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: i) (OperationKind.Argument, Type: null, IsImplicit) (Syntax: '1')
                  ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 1) (Syntax: '1')
                  InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                  OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
]]>.Value

            VerifyOperationTreeAndDiagnosticsForTest(Of ObjectCreationExpressionSyntax)(source, expectedOperationTree, expectedDiagnostics)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation)>
        <Fact()>
        Public Sub TypeParameterObjectCreation_MissingNewConstraint()
            Dim source = <![CDATA[
Imports System
Imports System.Collections
Imports System.Collections.Generic
Class C1
    Sub M(Of T As {C1, IEnumerable(Of Integer)})(t1 As T)
        t1 = New T() From {1}'BIND:"New T() From {1}"
    End Sub
    Public Sub Add(i As Integer)
    End Sub
End Class]]>.Value

            Dim expectedDiagnostics = <![CDATA[
BC32046: 'New' cannot be used on a type parameter that does not have a 'New' constraint.
        t1 = New T() From {1}'BIND:"New T() From {1}"
                 ~
]]>.Value

            ' Asserts that no ITypeParameterObjectCreationOperation is generated in this tree
            Dim expectedOperationTree = <![CDATA[
IInvalidOperation (OperationKind.Invalid, Type: T, IsInvalid) (Syntax: 'New T() From {1}')
  Children(1):
      IObjectOrCollectionInitializerOperation (OperationKind.ObjectOrCollectionInitializer, Type: T) (Syntax: 'From {1}')
        Initializers(1):
            IInvocationOperation ( Sub C1.Add(i As System.Int32)) (OperationKind.Invocation, Type: System.Void, IsImplicit) (Syntax: '1')
              Instance Receiver: 
                IInstanceReferenceOperation (ReferenceKind: ImplicitReceiver) (OperationKind.InstanceReference, Type: T, IsInvalid, IsImplicit) (Syntax: 'New T() From {1}')
              Arguments(1):
                  IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: i) (OperationKind.Argument, Type: null, IsImplicit) (Syntax: '1')
                    ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 1) (Syntax: '1')
                    InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                    OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
]]>.Value

            VerifyOperationTreeAndDiagnosticsForTest(Of ObjectCreationExpressionSyntax)(source, expectedOperationTree, expectedDiagnostics)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation, CompilerFeature.Dataflow)>
        <Fact()>
        Public Sub TypeParameterObjectCreationFlow_01()
            Dim source = <![CDATA[
Imports System
Imports System.Collections
Imports System.Collections.Generic
Class C1
    Sub M(Of T As {C1, IEnumerable(Of Integer), New})(t1 As T, b As Boolean)'BIND:"Sub M(Of T As {C1, IEnumerable(Of Integer), New})(t1 As T, b As Boolean)"
        t1 = New T() From {1, If(b, 2, 3)}
    End Sub
    Public Sub Add(i As Integer)
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
    CaptureIds: [0] [1]
    Block[B1] - Block
        Predecessors: [B0]
        Statements (3)
            IFlowCaptureOperation: 0 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 't1')
              Value: 
                IParameterReferenceOperation: t1 (OperationKind.ParameterReference, Type: T) (Syntax: 't1')

            IFlowCaptureOperation: 1 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'New T() Fro ... f(b, 2, 3)}')
              Value: 
                ITypeParameterObjectCreationOperation (OperationKind.TypeParameterObjectCreation, Type: T) (Syntax: 'New T() Fro ... f(b, 2, 3)}')
                  Initializer: 
                    null

            IInvocationOperation ( Sub C1.Add(i As System.Int32)) (OperationKind.Invocation, Type: System.Void, IsImplicit) (Syntax: '1')
              Instance Receiver: 
                IFlowCaptureReferenceOperation: 1 (OperationKind.FlowCaptureReference, Type: T, IsImplicit) (Syntax: 'New T() Fro ... f(b, 2, 3)}')
              Arguments(1):
                  IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: i) (OperationKind.Argument, Type: null, IsImplicit) (Syntax: '1')
                    ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 1) (Syntax: '1')
                    InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                    OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)

        Next (Regular) Block[B2]
            Entering: {R2}

    .locals {R2}
    {
        CaptureIds: [2]
        Block[B2] - Block
            Predecessors: [B1]
            Statements (0)
            Jump if False (Regular) to Block[B4]
                IParameterReferenceOperation: b (OperationKind.ParameterReference, Type: System.Boolean) (Syntax: 'b')

            Next (Regular) Block[B3]
        Block[B3] - Block
            Predecessors: [B2]
            Statements (1)
                IFlowCaptureOperation: 2 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: '2')
                  Value: 
                    ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 2) (Syntax: '2')

            Next (Regular) Block[B5]
        Block[B4] - Block
            Predecessors: [B2]
            Statements (1)
                IFlowCaptureOperation: 2 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: '3')
                  Value: 
                    ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 3) (Syntax: '3')

            Next (Regular) Block[B5]
        Block[B5] - Block
            Predecessors: [B3] [B4]
            Statements (1)
                IInvocationOperation ( Sub C1.Add(i As System.Int32)) (OperationKind.Invocation, Type: System.Void, IsImplicit) (Syntax: 'If(b, 2, 3)')
                  Instance Receiver: 
                    IFlowCaptureReferenceOperation: 1 (OperationKind.FlowCaptureReference, Type: T, IsImplicit) (Syntax: 'New T() Fro ... f(b, 2, 3)}')
                  Arguments(1):
                      IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: i) (OperationKind.Argument, Type: null, IsImplicit) (Syntax: 'If(b, 2, 3)')
                        IFlowCaptureReferenceOperation: 2 (OperationKind.FlowCaptureReference, Type: System.Int32, IsImplicit) (Syntax: 'If(b, 2, 3)')
                        InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                        OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)

            Next (Regular) Block[B6]
                Leaving: {R2}
    }

    Block[B6] - Block
        Predecessors: [B5]
        Statements (1)
            IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 't1 = New T( ... f(b, 2, 3)}')
              Expression: 
                ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: T, IsImplicit) (Syntax: 't1 = New T( ... f(b, 2, 3)}')
                  Left: 
                    IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: T, IsImplicit) (Syntax: 't1')
                  Right: 
                    IFlowCaptureReferenceOperation: 1 (OperationKind.FlowCaptureReference, Type: T, IsImplicit) (Syntax: 'New T() Fro ... f(b, 2, 3)}')

        Next (Regular) Block[B7]
            Leaving: {R1}
}

Block[B7] - Exit
    Predecessors: [B6]
    Statements (0)
]]>.Value

            VerifyFlowGraphAndDiagnosticsForTest(Of MethodBlockSyntax)(source, expectedFlowGraph, expectedDiagnostics)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation, CompilerFeature.Dataflow)>
        <Fact()>
        Public Sub TypeParameterObjectCreationFlow_02()
            Dim source = <![CDATA[
Imports System
Imports System.Collections
Imports System.Collections.Generic
Class C1
    Sub M(Of T As {C1, New})(t1 As T, b As Boolean)'BIND:"Sub M(Of T As {C1, New})(t1 As T, b As Boolean)"
        t1 = New T() With {.I1 = 1, .I2 = If(b, 2, 3)}
    End Sub
    Public Property I1 As Integer
    Public Property I2 As Integer
End Class]]>.Value

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
        Statements (3)
            IFlowCaptureOperation: 0 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 't1')
              Value: 
                IParameterReferenceOperation: t1 (OperationKind.ParameterReference, Type: T) (Syntax: 't1')

            IFlowCaptureOperation: 1 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'New T() Wit ... f(b, 2, 3)}')
              Value: 
                ITypeParameterObjectCreationOperation (OperationKind.TypeParameterObjectCreation, Type: T) (Syntax: 'New T() Wit ... f(b, 2, 3)}')
                  Initializer: 
                    null

            ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Void) (Syntax: '.I1 = 1')
              Left: 
                IPropertyReferenceOperation: Property C1.I1 As System.Int32 (OperationKind.PropertyReference, Type: System.Int32) (Syntax: 'I1')
                  Instance Receiver: 
                    IFlowCaptureReferenceOperation: 1 (OperationKind.FlowCaptureReference, Type: T, IsImplicit) (Syntax: 'New T() Wit ... f(b, 2, 3)}')
              Right: 
                ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 1) (Syntax: '1')

        Next (Regular) Block[B2]
            Entering: {R2}

    .locals {R2}
    {
        CaptureIds: [2]
        Block[B2] - Block
            Predecessors: [B1]
            Statements (0)
            Jump if False (Regular) to Block[B4]
                IParameterReferenceOperation: b (OperationKind.ParameterReference, Type: System.Boolean) (Syntax: 'b')

            Next (Regular) Block[B3]
        Block[B3] - Block
            Predecessors: [B2]
            Statements (1)
                IFlowCaptureOperation: 2 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: '2')
                  Value: 
                    ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 2) (Syntax: '2')

            Next (Regular) Block[B5]
        Block[B4] - Block
            Predecessors: [B2]
            Statements (1)
                IFlowCaptureOperation: 2 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: '3')
                  Value: 
                    ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 3) (Syntax: '3')

            Next (Regular) Block[B5]
        Block[B5] - Block
            Predecessors: [B3] [B4]
            Statements (1)
                ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Void) (Syntax: '.I2 = If(b, 2, 3)')
                  Left: 
                    IPropertyReferenceOperation: Property C1.I2 As System.Int32 (OperationKind.PropertyReference, Type: System.Int32) (Syntax: 'I2')
                      Instance Receiver: 
                        IFlowCaptureReferenceOperation: 1 (OperationKind.FlowCaptureReference, Type: T, IsImplicit) (Syntax: 'New T() Wit ... f(b, 2, 3)}')
                  Right: 
                    IFlowCaptureReferenceOperation: 2 (OperationKind.FlowCaptureReference, Type: System.Int32, IsImplicit) (Syntax: 'If(b, 2, 3)')

            Next (Regular) Block[B6]
                Leaving: {R2}
    }

    Block[B6] - Block
        Predecessors: [B5]
        Statements (1)
            IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 't1 = New T( ... f(b, 2, 3)}')
              Expression: 
                ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: T, IsImplicit) (Syntax: 't1 = New T( ... f(b, 2, 3)}')
                  Left: 
                    IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: T, IsImplicit) (Syntax: 't1')
                  Right: 
                    IFlowCaptureReferenceOperation: 1 (OperationKind.FlowCaptureReference, Type: T, IsImplicit) (Syntax: 'New T() Wit ... f(b, 2, 3)}')

        Next (Regular) Block[B7]
            Leaving: {R1}
}

Block[B7] - Exit
    Predecessors: [B6]
    Statements (0)
]]>.Value

            VerifyFlowGraphAndDiagnosticsForTest(Of MethodBlockSyntax)(source, expectedFlowGraph, expectedDiagnostics)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation, CompilerFeature.Dataflow)>
        <Fact()>
        Public Sub TypeParameterObjectCreationFlow_03()
            Dim source = <![CDATA[
Imports System
Imports System.Collections
Imports System.Collections.Generic
Class C1
    Sub M(Of T As {C1, IEnumerable(Of Integer), New})(t1 As T, b As Boolean)'BIND:"Sub M(Of T As {C1, IEnumerable(Of Integer), New})(t1 As T, b As Boolean)"
        t1 = New T() From {1, 2}
    End Sub
    Public Sub Add(i As Integer)
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
    CaptureIds: [0] [1]
    Block[B1] - Block
        Predecessors: [B0]
        Statements (5)
            IFlowCaptureOperation: 0 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 't1')
              Value: 
                IParameterReferenceOperation: t1 (OperationKind.ParameterReference, Type: T) (Syntax: 't1')

            IFlowCaptureOperation: 1 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'New T() From {1, 2}')
              Value: 
                ITypeParameterObjectCreationOperation (OperationKind.TypeParameterObjectCreation, Type: T) (Syntax: 'New T() From {1, 2}')
                  Initializer: 
                    null

            IInvocationOperation ( Sub C1.Add(i As System.Int32)) (OperationKind.Invocation, Type: System.Void, IsImplicit) (Syntax: '1')
              Instance Receiver: 
                IFlowCaptureReferenceOperation: 1 (OperationKind.FlowCaptureReference, Type: T, IsImplicit) (Syntax: 'New T() From {1, 2}')
              Arguments(1):
                  IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: i) (OperationKind.Argument, Type: null, IsImplicit) (Syntax: '1')
                    ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 1) (Syntax: '1')
                    InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                    OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)

            IInvocationOperation ( Sub C1.Add(i As System.Int32)) (OperationKind.Invocation, Type: System.Void, IsImplicit) (Syntax: '2')
              Instance Receiver: 
                IFlowCaptureReferenceOperation: 1 (OperationKind.FlowCaptureReference, Type: T, IsImplicit) (Syntax: 'New T() From {1, 2}')
              Arguments(1):
                  IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: i) (OperationKind.Argument, Type: null, IsImplicit) (Syntax: '2')
                    ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 2) (Syntax: '2')
                    InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                    OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)

            IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 't1 = New T() From {1, 2}')
              Expression: 
                ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: T, IsImplicit) (Syntax: 't1 = New T() From {1, 2}')
                  Left: 
                    IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: T, IsImplicit) (Syntax: 't1')
                  Right: 
                    IFlowCaptureReferenceOperation: 1 (OperationKind.FlowCaptureReference, Type: T, IsImplicit) (Syntax: 'New T() From {1, 2}')

        Next (Regular) Block[B2]
            Leaving: {R1}
}

Block[B2] - Exit
    Predecessors: [B1]
    Statements (0)
]]>.Value

            VerifyFlowGraphAndDiagnosticsForTest(Of MethodBlockSyntax)(source, expectedFlowGraph, expectedDiagnostics)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation, CompilerFeature.Dataflow)>
        <Fact()>
        Public Sub TypeParameterObjectCreationFlow_04()
            Dim source = <![CDATA[
Imports System
Imports System.Collections
Imports System.Collections.Generic
Class C1
    Sub M(Of T As {C1, IEnumerable(Of Integer)})(t1 As T, b As Boolean)'BIND:"Sub M(Of T As {C1, IEnumerable(Of Integer)})(t1 As T, b As Boolean)"
        t1 = New T() From {1, 2}
    End Sub
    Public Sub Add(i As Integer)
    End Sub
End Class]]>.Value

            Dim expectedDiagnostics = <![CDATA[
BC32046: 'New' cannot be used on a type parameter that does not have a 'New' constraint.
        t1 = New T() From {1, 2}
                 ~
]]>.Value

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
        Statements (5)
            IFlowCaptureOperation: 0 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 't1')
              Value: 
                IParameterReferenceOperation: t1 (OperationKind.ParameterReference, Type: T) (Syntax: 't1')

            IFlowCaptureOperation: 1 (OperationKind.FlowCapture, Type: null, IsInvalid, IsImplicit) (Syntax: 'New T() From {1, 2}')
              Value: 
                IInvalidOperation (OperationKind.Invalid, Type: T, IsInvalid) (Syntax: 'New T() From {1, 2}')
                  Children(0)

            IInvocationOperation ( Sub C1.Add(i As System.Int32)) (OperationKind.Invocation, Type: System.Void, IsImplicit) (Syntax: '1')
              Instance Receiver: 
                IFlowCaptureReferenceOperation: 1 (OperationKind.FlowCaptureReference, Type: T, IsInvalid, IsImplicit) (Syntax: 'New T() From {1, 2}')
              Arguments(1):
                  IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: i) (OperationKind.Argument, Type: null, IsImplicit) (Syntax: '1')
                    ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 1) (Syntax: '1')
                    InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                    OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)

            IInvocationOperation ( Sub C1.Add(i As System.Int32)) (OperationKind.Invocation, Type: System.Void, IsImplicit) (Syntax: '2')
              Instance Receiver: 
                IFlowCaptureReferenceOperation: 1 (OperationKind.FlowCaptureReference, Type: T, IsInvalid, IsImplicit) (Syntax: 'New T() From {1, 2}')
              Arguments(1):
                  IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: i) (OperationKind.Argument, Type: null, IsImplicit) (Syntax: '2')
                    ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 2) (Syntax: '2')
                    InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                    OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)

            IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null, IsInvalid) (Syntax: 't1 = New T() From {1, 2}')
              Expression: 
                ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: T, IsInvalid, IsImplicit) (Syntax: 't1 = New T() From {1, 2}')
                  Left: 
                    IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: T, IsImplicit) (Syntax: 't1')
                  Right: 
                    IFlowCaptureReferenceOperation: 1 (OperationKind.FlowCaptureReference, Type: T, IsInvalid, IsImplicit) (Syntax: 'New T() From {1, 2}')

        Next (Regular) Block[B2]
            Leaving: {R1}
}

Block[B2] - Exit
    Predecessors: [B1]
    Statements (0)
]]>.Value

            VerifyFlowGraphAndDiagnosticsForTest(Of MethodBlockSyntax)(source, expectedFlowGraph, expectedDiagnostics)
        End Sub
    End Class
End Namespace
