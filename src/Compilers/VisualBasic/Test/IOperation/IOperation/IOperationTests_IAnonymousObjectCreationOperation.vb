' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports Microsoft.CodeAnalysis.Test.Utilities
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax
Imports Roslyn.Test.Utilities

Namespace Microsoft.CodeAnalysis.VisualBasic.UnitTests.Semantics

    Partial Public Class IOperationTests
        Inherits SemanticModelTestBase

        <CompilerTrait(CompilerFeature.IOperation, CompilerFeature.Dataflow)>
        <Fact()>
        Public Sub AnonymousObjectCreation_NoControlFlow_01()
            Dim source = <![CDATA[
Imports System

Class C
    Sub M(p As Object, i As Integer, j As Integer)'BIND:"Sub M(p As Object, i As Integer, j As Integer)"
        p = New With { Key .a = i, Key .b = j }
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
    CaptureIds: [0] [1] [2]
    Block[B1] - Block
        Predecessors: [B0]
        Statements (4)
            IFlowCaptureOperation: 0 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'p')
              Value: 
                IParameterReferenceOperation: p (OperationKind.ParameterReference, Type: System.Object) (Syntax: 'p')

            IFlowCaptureOperation: 1 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'i')
              Value: 
                IParameterReferenceOperation: i (OperationKind.ParameterReference, Type: System.Int32) (Syntax: 'i')

            IFlowCaptureOperation: 2 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'j')
              Value: 
                IParameterReferenceOperation: j (OperationKind.ParameterReference, Type: System.Int32) (Syntax: 'j')

            IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'p = New Wit ... ey .b = j }')
              Expression: 
                ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Object, IsImplicit) (Syntax: 'p = New Wit ... ey .b = j }')
                  Left: 
                    IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: System.Object, IsImplicit) (Syntax: 'p')
                  Right: 
                    IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.Object, IsImplicit) (Syntax: 'New With {  ... ey .b = j }')
                      Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: True, IsUserDefined: False) (MethodSymbol: null)
                        (WideningReference)
                      Operand: 
                        IAnonymousObjectCreationOperation (OperationKind.AnonymousObjectCreation, Type: <anonymous type: Key a As System.Int32, Key b As System.Int32>) (Syntax: 'New With {  ... ey .b = j }')
                          Initializers(2):
                              ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Int32) (Syntax: 'Key .a = i')
                                Left: 
                                  IPropertyReferenceOperation: ReadOnly Property <anonymous type: Key a As System.Int32, Key b As System.Int32>.a As System.Int32 (OperationKind.PropertyReference, Type: System.Int32) (Syntax: 'a')
                                    Instance Receiver: 
                                      IInstanceReferenceOperation (ReferenceKind: ImplicitReceiver) (OperationKind.InstanceReference, Type: <anonymous type: Key a As System.Int32, Key b As System.Int32>, IsImplicit) (Syntax: 'New With {  ... ey .b = j }')
                                Right: 
                                  IFlowCaptureReferenceOperation: 1 (OperationKind.FlowCaptureReference, Type: System.Int32, IsImplicit) (Syntax: 'i')
                              ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Int32) (Syntax: 'Key .b = j')
                                Left: 
                                  IPropertyReferenceOperation: ReadOnly Property <anonymous type: Key a As System.Int32, Key b As System.Int32>.b As System.Int32 (OperationKind.PropertyReference, Type: System.Int32) (Syntax: 'b')
                                    Instance Receiver: 
                                      IInstanceReferenceOperation (ReferenceKind: ImplicitReceiver) (OperationKind.InstanceReference, Type: <anonymous type: Key a As System.Int32, Key b As System.Int32>, IsImplicit) (Syntax: 'New With {  ... ey .b = j }')
                                Right: 
                                  IFlowCaptureReferenceOperation: 2 (OperationKind.FlowCaptureReference, Type: System.Int32, IsImplicit) (Syntax: 'j')

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
        Public Sub AnonymousObjectCreation_NoControlFlow_02()
            ' Verify initializers that are not simple assignments.
            Dim source = <![CDATA[
Imports System

Class B
    Public j As Integer = 0
End Class

Class C
    Private b As New B()
    Sub M(p As Object, i As Integer, j As Integer)'BIND:"Sub M(p As Object, i As Integer, j As Integer)"
        p = New With {i, b.j}
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
    CaptureIds: [0] [1] [2]
    Block[B1] - Block
        Predecessors: [B0]
        Statements (4)
            IFlowCaptureOperation: 0 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'p')
              Value: 
                IParameterReferenceOperation: p (OperationKind.ParameterReference, Type: System.Object) (Syntax: 'p')

            IFlowCaptureOperation: 1 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'i')
              Value: 
                IParameterReferenceOperation: i (OperationKind.ParameterReference, Type: System.Int32) (Syntax: 'i')

            IFlowCaptureOperation: 2 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'b.j')
              Value: 
                IFieldReferenceOperation: B.j As System.Int32 (OperationKind.FieldReference, Type: System.Int32) (Syntax: 'b.j')
                  Instance Receiver: 
                    IFieldReferenceOperation: C.b As B (OperationKind.FieldReference, Type: B) (Syntax: 'b')
                      Instance Receiver: 
                        IInstanceReferenceOperation (ReferenceKind: ContainingTypeInstance) (OperationKind.InstanceReference, Type: C, IsImplicit) (Syntax: 'b')

            IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'p = New With {i, b.j}')
              Expression: 
                ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Object, IsImplicit) (Syntax: 'p = New With {i, b.j}')
                  Left: 
                    IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: System.Object, IsImplicit) (Syntax: 'p')
                  Right: 
                    IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.Object, IsImplicit) (Syntax: 'New With {i, b.j}')
                      Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: True, IsUserDefined: False) (MethodSymbol: null)
                        (WideningReference)
                      Operand: 
                        IAnonymousObjectCreationOperation (OperationKind.AnonymousObjectCreation, Type: <anonymous type: i As System.Int32, j As System.Int32>) (Syntax: 'New With {i, b.j}')
                          Initializers(2):
                              ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Int32, IsImplicit) (Syntax: 'i')
                                Left: 
                                  IPropertyReferenceOperation: Property <anonymous type: i As System.Int32, j As System.Int32>.i As System.Int32 (OperationKind.PropertyReference, Type: System.Int32, IsImplicit) (Syntax: 'i')
                                    Instance Receiver: 
                                      IInstanceReferenceOperation (ReferenceKind: ImplicitReceiver) (OperationKind.InstanceReference, Type: <anonymous type: i As System.Int32, j As System.Int32>, IsImplicit) (Syntax: 'New With {i, b.j}')
                                Right: 
                                  IFlowCaptureReferenceOperation: 1 (OperationKind.FlowCaptureReference, Type: System.Int32, IsImplicit) (Syntax: 'i')
                              ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Int32, IsImplicit) (Syntax: 'b.j')
                                Left: 
                                  IPropertyReferenceOperation: Property <anonymous type: i As System.Int32, j As System.Int32>.j As System.Int32 (OperationKind.PropertyReference, Type: System.Int32, IsImplicit) (Syntax: 'b.j')
                                    Instance Receiver: 
                                      IInstanceReferenceOperation (ReferenceKind: ImplicitReceiver) (OperationKind.InstanceReference, Type: <anonymous type: i As System.Int32, j As System.Int32>, IsImplicit) (Syntax: 'New With {i, b.j}')
                                Right: 
                                  IFlowCaptureReferenceOperation: 2 (OperationKind.FlowCaptureReference, Type: System.Int32, IsImplicit) (Syntax: 'b.j')

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
        Public Sub AnonymousObjectCreation_NoControlFlow_03()
            ' Verify initializers that are mix of simple assignments and non-assignments.
            Dim source = <![CDATA[
Imports System

Class B
    Public j As Integer = 0
End Class

Class C
    Private b As New B()
    Sub M(p As Object, i As Integer, j As Integer)'BIND:"Sub M(p As Object, i As Integer, j As Integer)"
        p = New With {i, .b = j}
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
    CaptureIds: [0] [1] [2]
    Block[B1] - Block
        Predecessors: [B0]
        Statements (4)
            IFlowCaptureOperation: 0 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'p')
              Value: 
                IParameterReferenceOperation: p (OperationKind.ParameterReference, Type: System.Object) (Syntax: 'p')

            IFlowCaptureOperation: 1 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'i')
              Value: 
                IParameterReferenceOperation: i (OperationKind.ParameterReference, Type: System.Int32) (Syntax: 'i')

            IFlowCaptureOperation: 2 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'j')
              Value: 
                IParameterReferenceOperation: j (OperationKind.ParameterReference, Type: System.Int32) (Syntax: 'j')

            IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'p = New With {i, .b = j}')
              Expression: 
                ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Object, IsImplicit) (Syntax: 'p = New With {i, .b = j}')
                  Left: 
                    IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: System.Object, IsImplicit) (Syntax: 'p')
                  Right: 
                    IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.Object, IsImplicit) (Syntax: 'New With {i, .b = j}')
                      Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: True, IsUserDefined: False) (MethodSymbol: null)
                        (WideningReference)
                      Operand: 
                        IAnonymousObjectCreationOperation (OperationKind.AnonymousObjectCreation, Type: <anonymous type: i As System.Int32, b As System.Int32>) (Syntax: 'New With {i, .b = j}')
                          Initializers(2):
                              ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Int32, IsImplicit) (Syntax: 'i')
                                Left: 
                                  IPropertyReferenceOperation: Property <anonymous type: i As System.Int32, b As System.Int32>.i As System.Int32 (OperationKind.PropertyReference, Type: System.Int32, IsImplicit) (Syntax: 'i')
                                    Instance Receiver: 
                                      IInstanceReferenceOperation (ReferenceKind: ImplicitReceiver) (OperationKind.InstanceReference, Type: <anonymous type: i As System.Int32, b As System.Int32>, IsImplicit) (Syntax: 'New With {i, .b = j}')
                                Right: 
                                  IFlowCaptureReferenceOperation: 1 (OperationKind.FlowCaptureReference, Type: System.Int32, IsImplicit) (Syntax: 'i')
                              ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Int32) (Syntax: '.b = j')
                                Left: 
                                  IPropertyReferenceOperation: Property <anonymous type: i As System.Int32, b As System.Int32>.b As System.Int32 (OperationKind.PropertyReference, Type: System.Int32) (Syntax: 'b')
                                    Instance Receiver: 
                                      IInstanceReferenceOperation (ReferenceKind: ImplicitReceiver) (OperationKind.InstanceReference, Type: <anonymous type: i As System.Int32, b As System.Int32>, IsImplicit) (Syntax: 'New With {i, .b = j}')
                                Right: 
                                  IFlowCaptureReferenceOperation: 2 (OperationKind.FlowCaptureReference, Type: System.Int32, IsImplicit) (Syntax: 'j')

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
        Public Sub AnonymousObjectCreation_NoControlFlow_04()
            ' Verify anonymous object creation in query.
            Dim source = <![CDATA[
Imports System
Imports System.Collections.Generic
Imports System.Linq

Class C
    Sub M(p As Object, a As List(Of Integer), b As List(Of String))'BIND:"Sub M(p As Object, a As List(Of Integer), b As List(Of String))"
        p = From x In a From y In b
    End Sub
End Class
]]>.Value

            Dim expectedDiagnostics = String.Empty

            Dim expectedFlowGraph = <![CDATA[
Block[B0] - Entry
    Statements (0)
    Next (Regular) Block[B1]
Block[B1] - Block
    Predecessors: [B0]
    Statements (1)
        IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'p = From x  ... From y In b')
          Expression: 
            ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Object, IsImplicit) (Syntax: 'p = From x  ... From y In b')
              Left: 
                IParameterReferenceOperation: p (OperationKind.ParameterReference, Type: System.Object) (Syntax: 'p')
              Right: 
                IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.Object, IsImplicit) (Syntax: 'From x In a From y In b')
                  Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: True, IsUserDefined: False) (MethodSymbol: null)
                    (WideningReference)
                  Operand: 
                    ITranslatedQueryOperation (OperationKind.TranslatedQuery, Type: System.Collections.Generic.IEnumerable(Of <anonymous type: Key x As System.Int32, Key y As System.String>)) (Syntax: 'From x In a From y In b')
                      Expression: 
                        IInvocationOperation ( Function System.Collections.Generic.IEnumerable(Of System.Int32).SelectMany(Of System.String, <anonymous type: Key x As System.Int32, Key y As System.String>)(collectionSelector As System.Func(Of System.Int32, System.Collections.Generic.IEnumerable(Of System.String)), resultSelector As System.Func(Of System.Int32, System.String, <anonymous type: Key x As System.Int32, Key y As System.String>)) As System.Collections.Generic.IEnumerable(Of <anonymous type: Key x As System.Int32, Key y As System.String>)) (OperationKind.Invocation, Type: System.Collections.Generic.IEnumerable(Of <anonymous type: Key x As System.Int32, Key y As System.String>), IsImplicit) (Syntax: 'y In b')
                          Instance Receiver: 
                            IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.Collections.Generic.IEnumerable(Of System.Int32), IsImplicit) (Syntax: 'x In a')
                              Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: True, IsUserDefined: False) (MethodSymbol: null)
                                (WideningReference)
                              Operand: 
                                IParameterReferenceOperation: a (OperationKind.ParameterReference, Type: System.Collections.Generic.List(Of System.Int32)) (Syntax: 'a')
                          Arguments(2):
                              IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: collectionSelector) (OperationKind.Argument, Type: null, IsImplicit) (Syntax: 'b')
                                IDelegateCreationOperation (OperationKind.DelegateCreation, Type: System.Func(Of System.Int32, System.Collections.Generic.IEnumerable(Of System.String)), IsImplicit) (Syntax: 'b')
                                  Target: 
                                    IFlowAnonymousFunctionOperation (Symbol: Function (x As System.Int32) As System.Collections.Generic.IEnumerable(Of System.String)) (OperationKind.FlowAnonymousFunction, Type: null, IsImplicit) (Syntax: 'b')
                                    {
                                        Block[B0#A0] - Entry
                                            Statements (0)
                                            Next (Regular) Block[B1#A0]
                                        Block[B1#A0] - Block
                                            Predecessors: [B0#A0]
                                            Statements (0)
                                            Next (Return) Block[B2#A0]
                                                IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.Collections.Generic.IEnumerable(Of System.String), IsImplicit) (Syntax: 'y In b')
                                                  Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: True, IsUserDefined: False) (MethodSymbol: null)
                                                    (WideningReference)
                                                  Operand: 
                                                    IParameterReferenceOperation: b (OperationKind.ParameterReference, Type: System.Collections.Generic.List(Of System.String)) (Syntax: 'b')
                                        Block[B2#A0] - Exit
                                            Predecessors: [B1#A0]
                                            Statements (0)
                                    }
                                InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                                OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                              IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: resultSelector) (OperationKind.Argument, Type: null, IsImplicit) (Syntax: 'From y In b')
                                IDelegateCreationOperation (OperationKind.DelegateCreation, Type: System.Func(Of System.Int32, System.String, <anonymous type: Key x As System.Int32, Key y As System.String>), IsImplicit) (Syntax: 'From y In b')
                                  Target: 
                                    IFlowAnonymousFunctionOperation (Symbol: Function (x As System.Int32, y As System.String) As <anonymous type: Key x As System.Int32, Key y As System.String>) (OperationKind.FlowAnonymousFunction, Type: null, IsImplicit) (Syntax: 'From y In b')
                                    {
                                        Block[B0#A1] - Entry
                                            Statements (0)
                                            Next (Regular) Block[B1#A1]
                                                Entering: {R1#A1}

                                        .locals {R1#A1}
                                        {
                                            CaptureIds: [0] [1]
                                            Block[B1#A1] - Block
                                                Predecessors: [B0#A1]
                                                Statements (2)
                                                    IFlowCaptureOperation: 0 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'x')
                                                      Value: 
                                                        IParameterReferenceOperation: x (OperationKind.ParameterReference, Type: System.Int32, IsImplicit) (Syntax: 'x')

                                                    IFlowCaptureOperation: 1 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'y')
                                                      Value: 
                                                        IParameterReferenceOperation: y (OperationKind.ParameterReference, Type: System.String, IsImplicit) (Syntax: 'y')

                                                Next (Return) Block[B2#A1]
                                                    IAnonymousObjectCreationOperation (OperationKind.AnonymousObjectCreation, Type: <anonymous type: Key x As System.Int32, Key y As System.String>, IsImplicit) (Syntax: 'y In b')
                                                      Initializers(2):
                                                          ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Int32, IsImplicit) (Syntax: 'x In a')
                                                            Left: 
                                                              IPropertyReferenceOperation: ReadOnly Property <anonymous type: Key x As System.Int32, Key y As System.String>.x As System.Int32 (OperationKind.PropertyReference, Type: System.Int32, IsImplicit) (Syntax: 'x')
                                                                Instance Receiver: 
                                                                  IInstanceReferenceOperation (ReferenceKind: ImplicitReceiver) (OperationKind.InstanceReference, Type: <anonymous type: Key x As System.Int32, Key y As System.String>, IsImplicit) (Syntax: 'y In b')
                                                            Right: 
                                                              IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: System.Int32, IsImplicit) (Syntax: 'x')
                                                          ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.String, IsImplicit) (Syntax: 'y In b')
                                                            Left: 
                                                              IPropertyReferenceOperation: ReadOnly Property <anonymous type: Key x As System.Int32, Key y As System.String>.y As System.String (OperationKind.PropertyReference, Type: System.String, IsImplicit) (Syntax: 'y')
                                                                Instance Receiver: 
                                                                  IInstanceReferenceOperation (ReferenceKind: ImplicitReceiver) (OperationKind.InstanceReference, Type: <anonymous type: Key x As System.Int32, Key y As System.String>, IsImplicit) (Syntax: 'y In b')
                                                            Right: 
                                                              IFlowCaptureReferenceOperation: 1 (OperationKind.FlowCaptureReference, Type: System.String, IsImplicit) (Syntax: 'y')
                                                    Leaving: {R1#A1}
                                        }

                                        Block[B2#A1] - Exit
                                            Predecessors: [B1#A1]
                                            Statements (0)
                                    }
                                InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                                OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)

    Next (Regular) Block[B2]
Block[B2] - Exit
    Predecessors: [B1]
    Statements (0)
]]>.Value

            VerifyFlowGraphAndDiagnosticsForTest(Of MethodBlockSyntax)(source, expectedFlowGraph, expectedDiagnostics)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation, CompilerFeature.Dataflow)>
        <Fact()>
        Public Sub AnonymousObjectCreation_NoControlFlow_05()
            ' Verify anonymous object creation in query with transparent identifiers.
            Dim source = <![CDATA[
Imports System
Imports System.Collections.Generic
Imports System.Linq

Class C
    Sub M(p As Object, a As List(Of Integer))'BIND:"Sub M(p As Object, a As List(Of Integer))"
        p = From x In a Let y = x
    End Sub
End Class
]]>.Value

            Dim expectedDiagnostics = String.Empty

            Dim expectedFlowGraph = <![CDATA[
Block[B0] - Entry
    Statements (0)
    Next (Regular) Block[B1]
Block[B1] - Block
    Predecessors: [B0]
    Statements (1)
        IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'p = From x  ... a Let y = x')
          Expression: 
            ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Object, IsImplicit) (Syntax: 'p = From x  ... a Let y = x')
              Left: 
                IParameterReferenceOperation: p (OperationKind.ParameterReference, Type: System.Object) (Syntax: 'p')
              Right: 
                IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.Object, IsImplicit) (Syntax: 'From x In a Let y = x')
                  Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: True, IsUserDefined: False) (MethodSymbol: null)
                    (WideningReference)
                  Operand: 
                    ITranslatedQueryOperation (OperationKind.TranslatedQuery, Type: System.Collections.Generic.IEnumerable(Of <anonymous type: Key x As System.Int32, Key y As System.Int32>)) (Syntax: 'From x In a Let y = x')
                      Expression: 
                        IInvocationOperation ( Function System.Collections.Generic.IEnumerable(Of System.Int32).Select(Of <anonymous type: Key x As System.Int32, Key y As System.Int32>)(selector As System.Func(Of System.Int32, <anonymous type: Key x As System.Int32, Key y As System.Int32>)) As System.Collections.Generic.IEnumerable(Of <anonymous type: Key x As System.Int32, Key y As System.Int32>)) (OperationKind.Invocation, Type: System.Collections.Generic.IEnumerable(Of <anonymous type: Key x As System.Int32, Key y As System.Int32>), IsImplicit) (Syntax: 'y = x')
                          Instance Receiver: 
                            IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.Collections.Generic.IEnumerable(Of System.Int32), IsImplicit) (Syntax: 'x In a')
                              Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: True, IsUserDefined: False) (MethodSymbol: null)
                                (WideningReference)
                              Operand: 
                                IParameterReferenceOperation: a (OperationKind.ParameterReference, Type: System.Collections.Generic.List(Of System.Int32)) (Syntax: 'a')
                          Arguments(1):
                              IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: selector) (OperationKind.Argument, Type: null, IsImplicit) (Syntax: 'x')
                                IDelegateCreationOperation (OperationKind.DelegateCreation, Type: System.Func(Of System.Int32, <anonymous type: Key x As System.Int32, Key y As System.Int32>), IsImplicit) (Syntax: 'x')
                                  Target: 
                                    IFlowAnonymousFunctionOperation (Symbol: Function (x As System.Int32) As <anonymous type: Key x As System.Int32, Key y As System.Int32>) (OperationKind.FlowAnonymousFunction, Type: null, IsImplicit) (Syntax: 'x')
                                    {
                                        Block[B0#A0] - Entry
                                            Statements (0)
                                            Next (Regular) Block[B1#A0]
                                                Entering: {R1#A0}

                                        .locals {R1#A0}
                                        {
                                            CaptureIds: [0] [1]
                                            Block[B1#A0] - Block
                                                Predecessors: [B0#A0]
                                                Statements (2)
                                                    IFlowCaptureOperation: 0 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'x')
                                                      Value: 
                                                        IParameterReferenceOperation: x (OperationKind.ParameterReference, Type: System.Int32, IsImplicit) (Syntax: 'x')

                                                    IFlowCaptureOperation: 1 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'x')
                                                      Value: 
                                                        IParameterReferenceOperation: x (OperationKind.ParameterReference, Type: System.Int32) (Syntax: 'x')

                                                Next (Return) Block[B2#A0]
                                                    IAnonymousObjectCreationOperation (OperationKind.AnonymousObjectCreation, Type: <anonymous type: Key x As System.Int32, Key y As System.Int32>, IsImplicit) (Syntax: 'y = x')
                                                      Initializers(2):
                                                          ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Int32, IsImplicit) (Syntax: 'x In a')
                                                            Left: 
                                                              IPropertyReferenceOperation: ReadOnly Property <anonymous type: Key x As System.Int32, Key y As System.Int32>.x As System.Int32 (OperationKind.PropertyReference, Type: System.Int32, IsImplicit) (Syntax: 'x')
                                                                Instance Receiver: 
                                                                  IInstanceReferenceOperation (ReferenceKind: ImplicitReceiver) (OperationKind.InstanceReference, Type: <anonymous type: Key x As System.Int32, Key y As System.Int32>, IsImplicit) (Syntax: 'y = x')
                                                            Right: 
                                                              IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: System.Int32, IsImplicit) (Syntax: 'x')
                                                          ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Int32, IsImplicit) (Syntax: 'y = x')
                                                            Left: 
                                                              IPropertyReferenceOperation: ReadOnly Property <anonymous type: Key x As System.Int32, Key y As System.Int32>.y As System.Int32 (OperationKind.PropertyReference, Type: System.Int32, IsImplicit) (Syntax: 'x')
                                                                Instance Receiver: 
                                                                  IInstanceReferenceOperation (ReferenceKind: ImplicitReceiver) (OperationKind.InstanceReference, Type: <anonymous type: Key x As System.Int32, Key y As System.Int32>, IsImplicit) (Syntax: 'y = x')
                                                            Right: 
                                                              IFlowCaptureReferenceOperation: 1 (OperationKind.FlowCaptureReference, Type: System.Int32, IsImplicit) (Syntax: 'x')
                                                    Leaving: {R1#A0}
                                        }

                                        Block[B2#A0] - Exit
                                            Predecessors: [B1#A0]
                                            Statements (0)
                                    }
                                InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                                OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)

    Next (Regular) Block[B2]
Block[B2] - Exit
    Predecessors: [B1]
    Statements (0)
]]>.Value

            VerifyFlowGraphAndDiagnosticsForTest(Of MethodBlockSyntax)(source, expectedFlowGraph, expectedDiagnostics)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation, CompilerFeature.Dataflow)>
        <Fact()>
        Public Sub AnonymousObjectCreation_NoControlFlow_06()
            ' Verify anonymous object creation nested in object creation initializer.
            ' Also verify property reference with implicit reference on the right side of an assignment.
            Dim source = <![CDATA[
Imports System

Class C
    Public a As Integer = 1
    Public b As Object

    Sub M(p As Object, i1 As Integer, i2 As Integer)'BIND:"Sub M(p As Object, i1 As Integer, i2 As Integer)"
        p = New C() With {.a = i1, .b = New With {.a = i2, .b = .a}}
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
    CaptureIds: [0] [1]
    Block[B1] - Block
        Predecessors: [B0]
        Statements (3)
            IFlowCaptureOperation: 0 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'p')
              Value: 
                IParameterReferenceOperation: p (OperationKind.ParameterReference, Type: System.Object) (Syntax: 'p')

            IFlowCaptureOperation: 1 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'New C() Wit ... , .b = .a}}')
              Value: 
                IObjectCreationOperation (Constructor: Sub C..ctor()) (OperationKind.ObjectCreation, Type: C) (Syntax: 'New C() Wit ... , .b = .a}}')
                  Arguments(0)
                  Initializer: 
                    null

            ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Int32) (Syntax: '.a = i1')
              Left: 
                IFieldReferenceOperation: C.a As System.Int32 (OperationKind.FieldReference, Type: System.Int32) (Syntax: 'a')
                  Instance Receiver: 
                    IFlowCaptureReferenceOperation: 1 (OperationKind.FlowCaptureReference, Type: C, IsImplicit) (Syntax: 'New C() Wit ... , .b = .a}}')
              Right: 
                IParameterReferenceOperation: i1 (OperationKind.ParameterReference, Type: System.Int32) (Syntax: 'i1')

        Next (Regular) Block[B2]
            Entering: {R2}

    .locals {R2}
    {
        CaptureIds: [2]
        Block[B2] - Block
            Predecessors: [B1]
            Statements (2)
                IFlowCaptureOperation: 2 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'i2')
                  Value: 
                    IParameterReferenceOperation: i2 (OperationKind.ParameterReference, Type: System.Int32) (Syntax: 'i2')

                ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Object) (Syntax: '.b = New Wi ... 2, .b = .a}')
                  Left: 
                    IFieldReferenceOperation: C.b As System.Object (OperationKind.FieldReference, Type: System.Object) (Syntax: 'b')
                      Instance Receiver: 
                        IFlowCaptureReferenceOperation: 1 (OperationKind.FlowCaptureReference, Type: C, IsImplicit) (Syntax: 'New C() Wit ... , .b = .a}}')
                  Right: 
                    IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.Object, IsImplicit) (Syntax: 'New With {. ... 2, .b = .a}')
                      Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: True, IsUserDefined: False) (MethodSymbol: null)
                        (WideningReference)
                      Operand: 
                        IAnonymousObjectCreationOperation (OperationKind.AnonymousObjectCreation, Type: <anonymous type: a As System.Int32, b As System.Int32>) (Syntax: 'New With {. ... 2, .b = .a}')
                          Initializers(2):
                              ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Int32) (Syntax: '.a = i2')
                                Left: 
                                  IPropertyReferenceOperation: Property <anonymous type: a As System.Int32, b As System.Int32>.a As System.Int32 (OperationKind.PropertyReference, Type: System.Int32) (Syntax: 'a')
                                    Instance Receiver: 
                                      IInstanceReferenceOperation (ReferenceKind: ImplicitReceiver) (OperationKind.InstanceReference, Type: <anonymous type: a As System.Int32, b As System.Int32>, IsImplicit) (Syntax: 'New With {. ... 2, .b = .a}')
                                Right: 
                                  IFlowCaptureReferenceOperation: 2 (OperationKind.FlowCaptureReference, Type: System.Int32, IsImplicit) (Syntax: 'i2')
                              ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Int32) (Syntax: '.b = .a')
                                Left: 
                                  IPropertyReferenceOperation: Property <anonymous type: a As System.Int32, b As System.Int32>.b As System.Int32 (OperationKind.PropertyReference, Type: System.Int32) (Syntax: 'b')
                                    Instance Receiver: 
                                      IInstanceReferenceOperation (ReferenceKind: ImplicitReceiver) (OperationKind.InstanceReference, Type: <anonymous type: a As System.Int32, b As System.Int32>, IsImplicit) (Syntax: 'New With {. ... 2, .b = .a}')
                                Right: 
                                  IFlowCaptureReferenceOperation: 2 (OperationKind.FlowCaptureReference, Type: System.Int32, IsImplicit) (Syntax: '.a')

            Next (Regular) Block[B3]
                Leaving: {R2}
    }

    Block[B3] - Block
        Predecessors: [B2]
        Statements (1)
            IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'p = New C() ... , .b = .a}}')
              Expression: 
                ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Object, IsImplicit) (Syntax: 'p = New C() ... , .b = .a}}')
                  Left: 
                    IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: System.Object, IsImplicit) (Syntax: 'p')
                  Right: 
                    IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.Object, IsImplicit) (Syntax: 'New C() Wit ... , .b = .a}}')
                      Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: True, IsUserDefined: False) (MethodSymbol: null)
                        (WideningReference)
                      Operand: 
                        IFlowCaptureReferenceOperation: 1 (OperationKind.FlowCaptureReference, Type: C, IsImplicit) (Syntax: 'New C() Wit ... , .b = .a}}')

        Next (Regular) Block[B4]
            Leaving: {R1}
}

Block[B4] - Exit
    Predecessors: [B3]
    Statements (0)
]]>.Value

            VerifyFlowGraphAndDiagnosticsForTest(Of MethodBlockSyntax)(source, expectedFlowGraph, expectedDiagnostics)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation, CompilerFeature.Dataflow)>
        <Fact()>
        Public Sub AnonymousObjectCreation_NoControlFlow_07()
            ' Verify anonymous object creation nested in anonymous object creation initializer.
            ' Also verify property reference with implicit reference on the right side of an assignment.
            Dim source = <![CDATA[
Imports System

Class C
    Sub M(p As Object, i1 As Integer, i2 As Integer)'BIND:"Sub M(p As Object, i1 As Integer, i2 As Integer)"
        p = New With {.a = i1, .b = New With {.a = i2, .b = .a}}
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
    CaptureIds: [0] [1] [3]
    Block[B1] - Block
        Predecessors: [B0]
        Statements (2)
            IFlowCaptureOperation: 0 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'p')
              Value: 
                IParameterReferenceOperation: p (OperationKind.ParameterReference, Type: System.Object) (Syntax: 'p')

            IFlowCaptureOperation: 1 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'i1')
              Value: 
                IParameterReferenceOperation: i1 (OperationKind.ParameterReference, Type: System.Int32) (Syntax: 'i1')

        Next (Regular) Block[B2]
            Entering: {R2}

    .locals {R2}
    {
        CaptureIds: [2]
        Block[B2] - Block
            Predecessors: [B1]
            Statements (2)
                IFlowCaptureOperation: 2 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'i2')
                  Value: 
                    IParameterReferenceOperation: i2 (OperationKind.ParameterReference, Type: System.Int32) (Syntax: 'i2')

                IFlowCaptureOperation: 3 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'New With {. ... 2, .b = .a}')
                  Value: 
                    IAnonymousObjectCreationOperation (OperationKind.AnonymousObjectCreation, Type: <anonymous type: a As System.Int32, b As System.Int32>) (Syntax: 'New With {. ... 2, .b = .a}')
                      Initializers(2):
                          ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Int32) (Syntax: '.a = i2')
                            Left: 
                              IPropertyReferenceOperation: Property <anonymous type: a As System.Int32, b As System.Int32>.a As System.Int32 (OperationKind.PropertyReference, Type: System.Int32) (Syntax: 'a')
                                Instance Receiver: 
                                  IInstanceReferenceOperation (ReferenceKind: ImplicitReceiver) (OperationKind.InstanceReference, Type: <anonymous type: a As System.Int32, b As System.Int32>, IsImplicit) (Syntax: 'New With {. ... 2, .b = .a}')
                            Right: 
                              IFlowCaptureReferenceOperation: 2 (OperationKind.FlowCaptureReference, Type: System.Int32, IsImplicit) (Syntax: 'i2')
                          ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Int32) (Syntax: '.b = .a')
                            Left: 
                              IPropertyReferenceOperation: Property <anonymous type: a As System.Int32, b As System.Int32>.b As System.Int32 (OperationKind.PropertyReference, Type: System.Int32) (Syntax: 'b')
                                Instance Receiver: 
                                  IInstanceReferenceOperation (ReferenceKind: ImplicitReceiver) (OperationKind.InstanceReference, Type: <anonymous type: a As System.Int32, b As System.Int32>, IsImplicit) (Syntax: 'New With {. ... 2, .b = .a}')
                            Right: 
                              IFlowCaptureReferenceOperation: 2 (OperationKind.FlowCaptureReference, Type: System.Int32, IsImplicit) (Syntax: '.a')

            Next (Regular) Block[B3]
                Leaving: {R2}
    }

    Block[B3] - Block
        Predecessors: [B2]
        Statements (1)
            IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'p = New Wit ... , .b = .a}}')
              Expression: 
                ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Object, IsImplicit) (Syntax: 'p = New Wit ... , .b = .a}}')
                  Left: 
                    IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: System.Object, IsImplicit) (Syntax: 'p')
                  Right: 
                    IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.Object, IsImplicit) (Syntax: 'New With {. ... , .b = .a}}')
                      Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: True, IsUserDefined: False) (MethodSymbol: null)
                        (WideningReference)
                      Operand: 
                        IAnonymousObjectCreationOperation (OperationKind.AnonymousObjectCreation, Type: <anonymous type: a As System.Int32, b As <anonymous type: a As System.Int32, b As System.Int32>>) (Syntax: 'New With {. ... , .b = .a}}')
                          Initializers(2):
                              ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Int32) (Syntax: '.a = i1')
                                Left: 
                                  IPropertyReferenceOperation: Property <anonymous type: a As System.Int32, b As <anonymous type: a As System.Int32, b As System.Int32>>.a As System.Int32 (OperationKind.PropertyReference, Type: System.Int32) (Syntax: 'a')
                                    Instance Receiver: 
                                      IInstanceReferenceOperation (ReferenceKind: ImplicitReceiver) (OperationKind.InstanceReference, Type: <anonymous type: a As System.Int32, b As <anonymous type: a As System.Int32, b As System.Int32>>, IsImplicit) (Syntax: 'New With {. ... , .b = .a}}')
                                Right: 
                                  IFlowCaptureReferenceOperation: 1 (OperationKind.FlowCaptureReference, Type: System.Int32, IsImplicit) (Syntax: 'i1')
                              ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: <anonymous type: a As System.Int32, b As System.Int32>) (Syntax: '.b = New Wi ... 2, .b = .a}')
                                Left: 
                                  IPropertyReferenceOperation: Property <anonymous type: a As System.Int32, b As <anonymous type: a As System.Int32, b As System.Int32>>.b As <anonymous type: a As System.Int32, b As System.Int32> (OperationKind.PropertyReference, Type: <anonymous type: a As System.Int32, b As System.Int32>) (Syntax: 'b')
                                    Instance Receiver: 
                                      IInstanceReferenceOperation (ReferenceKind: ImplicitReceiver) (OperationKind.InstanceReference, Type: <anonymous type: a As System.Int32, b As <anonymous type: a As System.Int32, b As System.Int32>>, IsImplicit) (Syntax: 'New With {. ... , .b = .a}}')
                                Right: 
                                  IFlowCaptureReferenceOperation: 3 (OperationKind.FlowCaptureReference, Type: <anonymous type: a As System.Int32, b As System.Int32>, IsImplicit) (Syntax: 'New With {. ... 2, .b = .a}')

        Next (Regular) Block[B4]
            Leaving: {R1}
}

Block[B4] - Exit
    Predecessors: [B3]
    Statements (0)
]]>.Value

            VerifyFlowGraphAndDiagnosticsForTest(Of MethodBlockSyntax)(source, expectedFlowGraph, expectedDiagnostics)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation, CompilerFeature.Dataflow)>
        <Fact()>
        Public Sub AnonymousObjectCreation_NoControlFlow_08()
            Dim source = <![CDATA[
Imports System

Class C
    Sub M(p As Object)'BIND:"Sub M(p As Object)"
        p = New With {.a = .b, .b = .b}
    End Sub
End Class
]]>.Value

            Dim expectedDiagnostics = <![CDATA[
BC36559: Anonymous type member property 'b' cannot be used to infer the type of another member property because the type of 'b' is not yet established.
        p = New With {.a = .b, .b = .b}
                           ~~
BC36559: Anonymous type member property 'b' cannot be used to infer the type of another member property because the type of 'b' is not yet established.
        p = New With {.a = .b, .b = .b}
                                    ~~
]]>.Value

            Dim expectedFlowGraph = <![CDATA[
Block[B0] - Entry
    Statements (0)
    Next (Regular) Block[B1]
        Entering: {R1}

.locals {R1}
{
    CaptureIds: [0] [1] [2]
    Block[B1] - Block
        Predecessors: [B0]
        Statements (4)
            IFlowCaptureOperation: 0 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'p')
              Value: 
                IParameterReferenceOperation: p (OperationKind.ParameterReference, Type: System.Object) (Syntax: 'p')

            IFlowCaptureOperation: 1 (OperationKind.FlowCapture, Type: null, IsInvalid, IsImplicit) (Syntax: '.b')
              Value: 
                IInvalidOperation (OperationKind.Invalid, Type: ?, IsInvalid) (Syntax: '.b')
                  Children(0)

            IFlowCaptureOperation: 2 (OperationKind.FlowCapture, Type: null, IsInvalid, IsImplicit) (Syntax: '.b')
              Value: 
                IInvalidOperation (OperationKind.Invalid, Type: ?, IsInvalid) (Syntax: '.b')
                  Children(0)

            IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null, IsInvalid) (Syntax: 'p = New Wit ... b, .b = .b}')
              Expression: 
                ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Object, IsInvalid, IsImplicit) (Syntax: 'p = New Wit ... b, .b = .b}')
                  Left: 
                    IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: System.Object, IsImplicit) (Syntax: 'p')
                  Right: 
                    IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.Object, IsInvalid, IsImplicit) (Syntax: 'New With {. ... b, .b = .b}')
                      Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: True, IsUserDefined: False) (MethodSymbol: null)
                        (WideningReference)
                      Operand: 
                        IAnonymousObjectCreationOperation (OperationKind.AnonymousObjectCreation, Type: <anonymous type: a As ?, b As ?>, IsInvalid) (Syntax: 'New With {. ... b, .b = .b}')
                          Initializers(2):
                              ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: ?, IsInvalid) (Syntax: '.a = .b')
                                Left: 
                                  IPropertyReferenceOperation: Property <anonymous type: a As ?, b As ?>.a As ? (OperationKind.PropertyReference, Type: ?) (Syntax: 'a')
                                    Instance Receiver: 
                                      IInstanceReferenceOperation (ReferenceKind: ImplicitReceiver) (OperationKind.InstanceReference, Type: <anonymous type: a As ?, b As ?>, IsInvalid, IsImplicit) (Syntax: 'New With {. ... b, .b = .b}')
                                Right: 
                                  IFlowCaptureReferenceOperation: 1 (OperationKind.FlowCaptureReference, Type: ?, IsInvalid, IsImplicit) (Syntax: '.b')
                              ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: ?, IsInvalid) (Syntax: '.b = .b')
                                Left: 
                                  IPropertyReferenceOperation: Property <anonymous type: a As ?, b As ?>.b As ? (OperationKind.PropertyReference, Type: ?) (Syntax: 'b')
                                    Instance Receiver: 
                                      IInstanceReferenceOperation (ReferenceKind: ImplicitReceiver) (OperationKind.InstanceReference, Type: <anonymous type: a As ?, b As ?>, IsInvalid, IsImplicit) (Syntax: 'New With {. ... b, .b = .b}')
                                Right: 
                                  IFlowCaptureReferenceOperation: 2 (OperationKind.FlowCaptureReference, Type: ?, IsInvalid, IsImplicit) (Syntax: '.b')

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
        Public Sub AnonymousObjectCreation_NoControlFlow_09()
            ' Verify anonymous object creation with no initializers.
            Dim source = <![CDATA[
Imports System

Class C
    Sub M(p As Object)'BIND:"Sub M(p As Object)"
        p = New With { }
    End Sub
End Class
]]>.Value

            Dim expectedDiagnostics = <![CDATA[
BC36574: Anonymous type must contain at least one member.
        p = New With { }
                     ~
]]>.Value

            Dim expectedFlowGraph = <![CDATA[
Block[B0] - Entry
    Statements (0)
    Next (Regular) Block[B1]
Block[B1] - Block
    Predecessors: [B0]
    Statements (1)
        IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null, IsInvalid) (Syntax: 'p = New With { }')
          Expression: 
            ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Object, IsInvalid, IsImplicit) (Syntax: 'p = New With { }')
              Left: 
                IParameterReferenceOperation: p (OperationKind.ParameterReference, Type: System.Object) (Syntax: 'p')
              Right: 
                IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.Object, IsInvalid, IsImplicit) (Syntax: 'New With { }')
                  Conversion: CommonConversion (Exists: False, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                    (DelegateRelaxationLevelNone)
                  Operand: 
                    IInvalidOperation (OperationKind.Invalid, Type: ?, IsInvalid) (Syntax: 'New With { }')
                      Children(0)

    Next (Regular) Block[B2]
Block[B2] - Exit
    Predecessors: [B1]
    Statements (0)
]]>.Value

            VerifyFlowGraphAndDiagnosticsForTest(Of MethodBlockSyntax)(source, expectedFlowGraph, expectedDiagnostics)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation, CompilerFeature.Dataflow)>
        <Fact()>
        Public Sub AnonymousObjectCreation_NoControlFlow_Error01()
            ' Duplicate property name, ensure we have same number of initializers as properties.
            Dim source = <![CDATA[
Imports System

Class C
    Sub M(p As Object, i1 As Integer, i2 As Integer) 'BIND:"Sub M(p As Object, i1 As Integer, i2 As Integer)"
        p = New With {.i = i1, .i = i2, .j = .i}
    End Sub
End Class
]]>.Value

            Dim expectedDiagnostics = <![CDATA[
BC36547: Anonymous type member or property 'i' is already declared.
        p = New With {.i = i1, .i = i2, .j = .i}
                               ~~~~~~~
]]>.Value

            Dim expectedFlowGraph = <![CDATA[
Block[B0] - Entry
    Statements (0)
    Next (Regular) Block[B1]
        Entering: {R1}

.locals {R1}
{
    CaptureIds: [0] [1] [2]
    Block[B1] - Block
        Predecessors: [B0]
        Statements (4)
            IFlowCaptureOperation: 0 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'p')
              Value: 
                IParameterReferenceOperation: p (OperationKind.ParameterReference, Type: System.Object) (Syntax: 'p')

            IFlowCaptureOperation: 1 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'i1')
              Value: 
                IParameterReferenceOperation: i1 (OperationKind.ParameterReference, Type: System.Int32) (Syntax: 'i1')

            IFlowCaptureOperation: 2 (OperationKind.FlowCapture, Type: null, IsInvalid, IsImplicit) (Syntax: 'i2')
              Value: 
                IParameterReferenceOperation: i2 (OperationKind.ParameterReference, Type: System.Int32, IsInvalid) (Syntax: 'i2')

            IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null, IsInvalid) (Syntax: 'p = New Wit ... 2, .j = .i}')
              Expression: 
                ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Object, IsInvalid, IsImplicit) (Syntax: 'p = New Wit ... 2, .j = .i}')
                  Left: 
                    IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: System.Object, IsImplicit) (Syntax: 'p')
                  Right: 
                    IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.Object, IsInvalid, IsImplicit) (Syntax: 'New With {. ... 2, .j = .i}')
                      Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: True, IsUserDefined: False) (MethodSymbol: null)
                        (WideningReference)
                      Operand: 
                        IAnonymousObjectCreationOperation (OperationKind.AnonymousObjectCreation, Type: <anonymous type: i As System.Int32, i As System.Int32, j As System.Int32>, IsInvalid) (Syntax: 'New With {. ... 2, .j = .i}')
                          Initializers(3):
                              ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Int32) (Syntax: '.i = i1')
                                Left: 
                                  IPropertyReferenceOperation: Property <anonymous type: i As System.Int32, i As System.Int32, j As System.Int32>.i As System.Int32 (OperationKind.PropertyReference, Type: System.Int32) (Syntax: 'i')
                                    Instance Receiver: 
                                      IInstanceReferenceOperation (ReferenceKind: ImplicitReceiver) (OperationKind.InstanceReference, Type: <anonymous type: i As System.Int32, i As System.Int32, j As System.Int32>, IsInvalid, IsImplicit) (Syntax: 'New With {. ... 2, .j = .i}')
                                Right: 
                                  IFlowCaptureReferenceOperation: 1 (OperationKind.FlowCaptureReference, Type: System.Int32, IsImplicit) (Syntax: 'i1')
                              ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Int32, IsInvalid) (Syntax: '.i = i2')
                                Left: 
                                  IPropertyReferenceOperation: Property <anonymous type: i As System.Int32, i As System.Int32, j As System.Int32>.i As System.Int32 (OperationKind.PropertyReference, Type: System.Int32, IsInvalid) (Syntax: 'i')
                                    Instance Receiver: 
                                      IInstanceReferenceOperation (ReferenceKind: ImplicitReceiver) (OperationKind.InstanceReference, Type: <anonymous type: i As System.Int32, i As System.Int32, j As System.Int32>, IsInvalid, IsImplicit) (Syntax: 'New With {. ... 2, .j = .i}')
                                Right: 
                                  IFlowCaptureReferenceOperation: 2 (OperationKind.FlowCaptureReference, Type: System.Int32, IsInvalid, IsImplicit) (Syntax: 'i2')
                              ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Int32) (Syntax: '.j = .i')
                                Left: 
                                  IPropertyReferenceOperation: Property <anonymous type: i As System.Int32, i As System.Int32, j As System.Int32>.j As System.Int32 (OperationKind.PropertyReference, Type: System.Int32) (Syntax: 'j')
                                    Instance Receiver: 
                                      IInstanceReferenceOperation (ReferenceKind: ImplicitReceiver) (OperationKind.InstanceReference, Type: <anonymous type: i As System.Int32, i As System.Int32, j As System.Int32>, IsInvalid, IsImplicit) (Syntax: 'New With {. ... 2, .j = .i}')
                                Right: 
                                  IFlowCaptureReferenceOperation: 2 (OperationKind.FlowCaptureReference, Type: System.Int32, IsImplicit) (Syntax: '.i')

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
        Public Sub AnonymousObjectCreation_NoControlFlow_Error02()
            ' Missing value for property assignment.
            Dim source = <![CDATA[
Imports System

Class C
    Sub M(p As Object)'BIND:"Sub M(p As Object)"
        p = New With {.a = }
    End Sub
End Class
]]>.Value

            Dim expectedDiagnostics = <![CDATA[
BC30201: Expression expected.
        p = New With {.a = }
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
        Statements (3)
            IFlowCaptureOperation: 0 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'p')
              Value: 
                IParameterReferenceOperation: p (OperationKind.ParameterReference, Type: System.Object) (Syntax: 'p')

            IFlowCaptureOperation: 1 (OperationKind.FlowCapture, Type: null, IsInvalid, IsImplicit) (Syntax: '')
              Value: 
                IInvalidOperation (OperationKind.Invalid, Type: null, IsInvalid) (Syntax: '')
                  Children(0)

            IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null, IsInvalid) (Syntax: 'p = New With {.a = }')
              Expression: 
                ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Object, IsInvalid, IsImplicit) (Syntax: 'p = New With {.a = }')
                  Left: 
                    IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: System.Object, IsImplicit) (Syntax: 'p')
                  Right: 
                    IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.Object, IsInvalid, IsImplicit) (Syntax: 'New With {.a = }')
                      Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: True, IsUserDefined: False) (MethodSymbol: null)
                        (WideningReference)
                      Operand: 
                        IAnonymousObjectCreationOperation (OperationKind.AnonymousObjectCreation, Type: <anonymous type: a As ?>, IsInvalid) (Syntax: 'New With {.a = }')
                          Initializers(1):
                              ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: ?, IsInvalid) (Syntax: '.a = ')
                                Left: 
                                  IPropertyReferenceOperation: Property <anonymous type: a As ?>.a As ? (OperationKind.PropertyReference, Type: ?) (Syntax: 'a')
                                    Instance Receiver: 
                                      IInstanceReferenceOperation (ReferenceKind: ImplicitReceiver) (OperationKind.InstanceReference, Type: <anonymous type: a As ?>, IsInvalid, IsImplicit) (Syntax: 'New With {.a = }')
                                Right: 
                                  IFlowCaptureReferenceOperation: 1 (OperationKind.FlowCaptureReference, Type: null, IsInvalid, IsImplicit) (Syntax: '')

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
        Public Sub AnonymousObjectCreation_NoControlFlow_Error03()
            ' Invalid expression as initializer target, ensure we don't drop this expression from the flow graph.
            Dim source = <![CDATA[
Imports System

Class C
    Sub M(p As Object, i As Integer, j As Integer) 'BIND:"Sub M(p As Object, i As Integer, j As Integer)"
        p = New With {.M2(i) = j}
    End Sub
    Function M2(i As Integer) As Integer
        Return 0
    End Function
End Class
]]>.Value

            Dim expectedDiagnostics = <![CDATA[
BC30984: '=' expected (object initializer).
        p = New With {.M2(i) = j}
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
        Statements (3)
            IFlowCaptureOperation: 0 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'p')
              Value: 
                IParameterReferenceOperation: p (OperationKind.ParameterReference, Type: System.Object) (Syntax: 'p')

            IFlowCaptureOperation: 1 (OperationKind.FlowCapture, Type: null, IsInvalid, IsImplicit) (Syntax: '(i) = j')
              Value: 
                IBinaryOperation (BinaryOperatorKind.Equals, Checked) (OperationKind.Binary, Type: System.Boolean, IsInvalid) (Syntax: '(i) = j')
                  Left: 
                    IParenthesizedOperation (OperationKind.Parenthesized, Type: System.Int32, IsInvalid) (Syntax: '(i)')
                      Operand: 
                        IParameterReferenceOperation: i (OperationKind.ParameterReference, Type: System.Int32) (Syntax: 'i')
                  Right: 
                    IParameterReferenceOperation: j (OperationKind.ParameterReference, Type: System.Int32) (Syntax: 'j')

            IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null, IsInvalid) (Syntax: 'p = New Wit ... .M2(i) = j}')
              Expression: 
                ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Object, IsInvalid, IsImplicit) (Syntax: 'p = New Wit ... .M2(i) = j}')
                  Left: 
                    IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: System.Object, IsImplicit) (Syntax: 'p')
                  Right: 
                    IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.Object, IsInvalid, IsImplicit) (Syntax: 'New With {.M2(i) = j}')
                      Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: True, IsUserDefined: False) (MethodSymbol: null)
                        (WideningReference)
                      Operand: 
                        IAnonymousObjectCreationOperation (OperationKind.AnonymousObjectCreation, Type: <anonymous type: M2 As System.Boolean>, IsInvalid) (Syntax: 'New With {.M2(i) = j}')
                          Initializers(1):
                              ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Boolean, IsInvalid) (Syntax: '.M2(i) = j')
                                Left: 
                                  IPropertyReferenceOperation: Property <anonymous type: M2 As System.Boolean>.M2 As System.Boolean (OperationKind.PropertyReference, Type: System.Boolean, IsInvalid) (Syntax: 'M2')
                                    Instance Receiver: 
                                      IInstanceReferenceOperation (ReferenceKind: ImplicitReceiver) (OperationKind.InstanceReference, Type: <anonymous type: M2 As System.Boolean>, IsInvalid, IsImplicit) (Syntax: 'New With {.M2(i) = j}')
                                Right: 
                                  IFlowCaptureReferenceOperation: 1 (OperationKind.FlowCaptureReference, Type: System.Boolean, IsInvalid, IsImplicit) (Syntax: '(i) = j')

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
        Public Sub AnonymousObjectCreation_NoControlFlow_Error04()
            ' Property reference with argument as an assignment target.
            Dim source = <![CDATA[
Imports System

Class C
    Sub M(p As Object, i As Integer, j As Integer) 'BIND:"Sub M(p As Object, i As Integer, j As Integer)"
        p = New With {.a(i) = j}
    End Sub
End Class
]]>.Value

            Dim expectedDiagnostics = <![CDATA[
BC30984: '=' expected (object initializer).
        p = New With {.a(i) = j}
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
        Statements (3)
            IFlowCaptureOperation: 0 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'p')
              Value: 
                IParameterReferenceOperation: p (OperationKind.ParameterReference, Type: System.Object) (Syntax: 'p')

            IFlowCaptureOperation: 1 (OperationKind.FlowCapture, Type: null, IsInvalid, IsImplicit) (Syntax: '(i) = j')
              Value: 
                IBinaryOperation (BinaryOperatorKind.Equals, Checked) (OperationKind.Binary, Type: System.Boolean, IsInvalid) (Syntax: '(i) = j')
                  Left: 
                    IParenthesizedOperation (OperationKind.Parenthesized, Type: System.Int32, IsInvalid) (Syntax: '(i)')
                      Operand: 
                        IParameterReferenceOperation: i (OperationKind.ParameterReference, Type: System.Int32) (Syntax: 'i')
                  Right: 
                    IParameterReferenceOperation: j (OperationKind.ParameterReference, Type: System.Int32) (Syntax: 'j')

            IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null, IsInvalid) (Syntax: 'p = New With {.a(i) = j}')
              Expression: 
                ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Object, IsInvalid, IsImplicit) (Syntax: 'p = New With {.a(i) = j}')
                  Left: 
                    IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: System.Object, IsImplicit) (Syntax: 'p')
                  Right: 
                    IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.Object, IsInvalid, IsImplicit) (Syntax: 'New With {.a(i) = j}')
                      Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: True, IsUserDefined: False) (MethodSymbol: null)
                        (WideningReference)
                      Operand: 
                        IAnonymousObjectCreationOperation (OperationKind.AnonymousObjectCreation, Type: <anonymous type: a As System.Boolean>, IsInvalid) (Syntax: 'New With {.a(i) = j}')
                          Initializers(1):
                              ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Boolean, IsInvalid) (Syntax: '.a(i) = j')
                                Left: 
                                  IPropertyReferenceOperation: Property <anonymous type: a As System.Boolean>.a As System.Boolean (OperationKind.PropertyReference, Type: System.Boolean, IsInvalid) (Syntax: 'a')
                                    Instance Receiver: 
                                      IInstanceReferenceOperation (ReferenceKind: ImplicitReceiver) (OperationKind.InstanceReference, Type: <anonymous type: a As System.Boolean>, IsInvalid, IsImplicit) (Syntax: 'New With {.a(i) = j}')
                                Right: 
                                  IFlowCaptureReferenceOperation: 1 (OperationKind.FlowCaptureReference, Type: System.Boolean, IsInvalid, IsImplicit) (Syntax: '(i) = j')

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
        Public Sub AnonymousObjectCreation_ControlFlowInFirstInitializer()
            Dim source = <![CDATA[
Imports System

Class C
    Sub M(p As Object, i1? As Integer, i2 As Integer, j As Integer)'BIND:"Sub M(p As Object, i1? As Integer, i2 As Integer, j As Integer)"
        p = New With { .a = If(i1, i2), .b = j }
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
    CaptureIds: [0] [2] [3]
    Block[B1] - Block
        Predecessors: [B0]
        Statements (1)
            IFlowCaptureOperation: 0 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'p')
              Value: 
                IParameterReferenceOperation: p (OperationKind.ParameterReference, Type: System.Object) (Syntax: 'p')

        Next (Regular) Block[B2]
            Entering: {R2}

    .locals {R2}
    {
        CaptureIds: [1]
        Block[B2] - Block
            Predecessors: [B1]
            Statements (1)
                IFlowCaptureOperation: 1 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'i1')
                  Value: 
                    IParameterReferenceOperation: i1 (OperationKind.ParameterReference, Type: System.Nullable(Of System.Int32)) (Syntax: 'i1')

            Jump if True (Regular) to Block[B4]
                IIsNullOperation (OperationKind.IsNull, Type: System.Boolean, IsImplicit) (Syntax: 'i1')
                  Operand: 
                    IFlowCaptureReferenceOperation: 1 (OperationKind.FlowCaptureReference, Type: System.Nullable(Of System.Int32), IsImplicit) (Syntax: 'i1')
                Leaving: {R2}

            Next (Regular) Block[B3]
        Block[B3] - Block
            Predecessors: [B2]
            Statements (1)
                IFlowCaptureOperation: 2 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'i1')
                  Value: 
                    IInvocationOperation ( Function System.Nullable(Of System.Int32).GetValueOrDefault() As System.Int32) (OperationKind.Invocation, Type: System.Int32, IsImplicit) (Syntax: 'i1')
                      Instance Receiver: 
                        IFlowCaptureReferenceOperation: 1 (OperationKind.FlowCaptureReference, Type: System.Nullable(Of System.Int32), IsImplicit) (Syntax: 'i1')
                      Arguments(0)

            Next (Regular) Block[B5]
                Leaving: {R2}
    }

    Block[B4] - Block
        Predecessors: [B2]
        Statements (1)
            IFlowCaptureOperation: 2 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'i2')
              Value: 
                IParameterReferenceOperation: i2 (OperationKind.ParameterReference, Type: System.Int32) (Syntax: 'i2')

        Next (Regular) Block[B5]
    Block[B5] - Block
        Predecessors: [B3] [B4]
        Statements (2)
            IFlowCaptureOperation: 3 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'j')
              Value: 
                IParameterReferenceOperation: j (OperationKind.ParameterReference, Type: System.Int32) (Syntax: 'j')

            IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'p = New Wit ... ), .b = j }')
              Expression: 
                ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Object, IsImplicit) (Syntax: 'p = New Wit ... ), .b = j }')
                  Left: 
                    IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: System.Object, IsImplicit) (Syntax: 'p')
                  Right: 
                    IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.Object, IsImplicit) (Syntax: 'New With {  ... ), .b = j }')
                      Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: True, IsUserDefined: False) (MethodSymbol: null)
                        (WideningReference)
                      Operand: 
                        IAnonymousObjectCreationOperation (OperationKind.AnonymousObjectCreation, Type: <anonymous type: a As System.Int32, b As System.Int32>) (Syntax: 'New With {  ... ), .b = j }')
                          Initializers(2):
                              ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Int32) (Syntax: '.a = If(i1, i2)')
                                Left: 
                                  IPropertyReferenceOperation: Property <anonymous type: a As System.Int32, b As System.Int32>.a As System.Int32 (OperationKind.PropertyReference, Type: System.Int32) (Syntax: 'a')
                                    Instance Receiver: 
                                      IInstanceReferenceOperation (ReferenceKind: ImplicitReceiver) (OperationKind.InstanceReference, Type: <anonymous type: a As System.Int32, b As System.Int32>, IsImplicit) (Syntax: 'New With {  ... ), .b = j }')
                                Right: 
                                  IFlowCaptureReferenceOperation: 2 (OperationKind.FlowCaptureReference, Type: System.Int32, IsImplicit) (Syntax: 'If(i1, i2)')
                              ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Int32) (Syntax: '.b = j')
                                Left: 
                                  IPropertyReferenceOperation: Property <anonymous type: a As System.Int32, b As System.Int32>.b As System.Int32 (OperationKind.PropertyReference, Type: System.Int32) (Syntax: 'b')
                                    Instance Receiver: 
                                      IInstanceReferenceOperation (ReferenceKind: ImplicitReceiver) (OperationKind.InstanceReference, Type: <anonymous type: a As System.Int32, b As System.Int32>, IsImplicit) (Syntax: 'New With {  ... ), .b = j }')
                                Right: 
                                  IFlowCaptureReferenceOperation: 3 (OperationKind.FlowCaptureReference, Type: System.Int32, IsImplicit) (Syntax: 'j')

        Next (Regular) Block[B6]
            Leaving: {R1}
}

Block[B6] - Exit
    Predecessors: [B5]
    Statements (0)
]]>.Value

            VerifyFlowGraphAndDiagnosticsForTest(Of MethodBlockSyntax)(source, expectedFlowGraph, expectedDiagnostics)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation, CompilerFeature.Dataflow)>
        <Fact()>
        Public Sub AnonymousObjectCreation_ControlFlowInSecondInitializer()
            Dim source = <![CDATA[
Imports System

Class C
    Sub M(p As Object, i1? As Integer, i2 As Integer, j As Integer)'BIND:"Sub M(p As Object, i1? As Integer, i2 As Integer, j As Integer)"
        p = New With { .a = j, .b = If(i1, i2)}
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
    CaptureIds: [0] [1] [3]
    Block[B1] - Block
        Predecessors: [B0]
        Statements (2)
            IFlowCaptureOperation: 0 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'p')
              Value: 
                IParameterReferenceOperation: p (OperationKind.ParameterReference, Type: System.Object) (Syntax: 'p')

            IFlowCaptureOperation: 1 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'j')
              Value: 
                IParameterReferenceOperation: j (OperationKind.ParameterReference, Type: System.Int32) (Syntax: 'j')

        Next (Regular) Block[B2]
            Entering: {R2}

    .locals {R2}
    {
        CaptureIds: [2]
        Block[B2] - Block
            Predecessors: [B1]
            Statements (1)
                IFlowCaptureOperation: 2 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'i1')
                  Value: 
                    IParameterReferenceOperation: i1 (OperationKind.ParameterReference, Type: System.Nullable(Of System.Int32)) (Syntax: 'i1')

            Jump if True (Regular) to Block[B4]
                IIsNullOperation (OperationKind.IsNull, Type: System.Boolean, IsImplicit) (Syntax: 'i1')
                  Operand: 
                    IFlowCaptureReferenceOperation: 2 (OperationKind.FlowCaptureReference, Type: System.Nullable(Of System.Int32), IsImplicit) (Syntax: 'i1')
                Leaving: {R2}

            Next (Regular) Block[B3]
        Block[B3] - Block
            Predecessors: [B2]
            Statements (1)
                IFlowCaptureOperation: 3 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'i1')
                  Value: 
                    IInvocationOperation ( Function System.Nullable(Of System.Int32).GetValueOrDefault() As System.Int32) (OperationKind.Invocation, Type: System.Int32, IsImplicit) (Syntax: 'i1')
                      Instance Receiver: 
                        IFlowCaptureReferenceOperation: 2 (OperationKind.FlowCaptureReference, Type: System.Nullable(Of System.Int32), IsImplicit) (Syntax: 'i1')
                      Arguments(0)

            Next (Regular) Block[B5]
                Leaving: {R2}
    }

    Block[B4] - Block
        Predecessors: [B2]
        Statements (1)
            IFlowCaptureOperation: 3 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'i2')
              Value: 
                IParameterReferenceOperation: i2 (OperationKind.ParameterReference, Type: System.Int32) (Syntax: 'i2')

        Next (Regular) Block[B5]
    Block[B5] - Block
        Predecessors: [B3] [B4]
        Statements (1)
            IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'p = New Wit ... If(i1, i2)}')
              Expression: 
                ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Object, IsImplicit) (Syntax: 'p = New Wit ... If(i1, i2)}')
                  Left: 
                    IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: System.Object, IsImplicit) (Syntax: 'p')
                  Right: 
                    IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.Object, IsImplicit) (Syntax: 'New With {  ... If(i1, i2)}')
                      Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: True, IsUserDefined: False) (MethodSymbol: null)
                        (WideningReference)
                      Operand: 
                        IAnonymousObjectCreationOperation (OperationKind.AnonymousObjectCreation, Type: <anonymous type: a As System.Int32, b As System.Int32>) (Syntax: 'New With {  ... If(i1, i2)}')
                          Initializers(2):
                              ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Int32) (Syntax: '.a = j')
                                Left: 
                                  IPropertyReferenceOperation: Property <anonymous type: a As System.Int32, b As System.Int32>.a As System.Int32 (OperationKind.PropertyReference, Type: System.Int32) (Syntax: 'a')
                                    Instance Receiver: 
                                      IInstanceReferenceOperation (ReferenceKind: ImplicitReceiver) (OperationKind.InstanceReference, Type: <anonymous type: a As System.Int32, b As System.Int32>, IsImplicit) (Syntax: 'New With {  ... If(i1, i2)}')
                                Right: 
                                  IFlowCaptureReferenceOperation: 1 (OperationKind.FlowCaptureReference, Type: System.Int32, IsImplicit) (Syntax: 'j')
                              ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Int32) (Syntax: '.b = If(i1, i2)')
                                Left: 
                                  IPropertyReferenceOperation: Property <anonymous type: a As System.Int32, b As System.Int32>.b As System.Int32 (OperationKind.PropertyReference, Type: System.Int32) (Syntax: 'b')
                                    Instance Receiver: 
                                      IInstanceReferenceOperation (ReferenceKind: ImplicitReceiver) (OperationKind.InstanceReference, Type: <anonymous type: a As System.Int32, b As System.Int32>, IsImplicit) (Syntax: 'New With {  ... If(i1, i2)}')
                                Right: 
                                  IFlowCaptureReferenceOperation: 3 (OperationKind.FlowCaptureReference, Type: System.Int32, IsImplicit) (Syntax: 'If(i1, i2)')

        Next (Regular) Block[B6]
            Leaving: {R1}
}

Block[B6] - Exit
    Predecessors: [B5]
    Statements (0)
]]>.Value

            VerifyFlowGraphAndDiagnosticsForTest(Of MethodBlockSyntax)(source, expectedFlowGraph, expectedDiagnostics)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation, CompilerFeature.Dataflow)>
        <Fact()>
        Public Sub AnonymousObjectCreation_ControlFlowInMultipleInitializers()
            Dim source = <![CDATA[
Imports System

Class C
    Sub M(p As Object, i1? As Integer, i2 As Integer, j1 As Integer?, j2 As Integer)'BIND:"Sub M(p As Object, i1? As Integer, i2 As Integer, j1 As Integer?, j2 As Integer)"
        p = New With { .a = If(i1, i2), .b = If(j1, j2)}
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
    CaptureIds: [0] [2] [4]
    Block[B1] - Block
        Predecessors: [B0]
        Statements (1)
            IFlowCaptureOperation: 0 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'p')
              Value: 
                IParameterReferenceOperation: p (OperationKind.ParameterReference, Type: System.Object) (Syntax: 'p')

        Next (Regular) Block[B2]
            Entering: {R2}

    .locals {R2}
    {
        CaptureIds: [1]
        Block[B2] - Block
            Predecessors: [B1]
            Statements (1)
                IFlowCaptureOperation: 1 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'i1')
                  Value: 
                    IParameterReferenceOperation: i1 (OperationKind.ParameterReference, Type: System.Nullable(Of System.Int32)) (Syntax: 'i1')

            Jump if True (Regular) to Block[B4]
                IIsNullOperation (OperationKind.IsNull, Type: System.Boolean, IsImplicit) (Syntax: 'i1')
                  Operand: 
                    IFlowCaptureReferenceOperation: 1 (OperationKind.FlowCaptureReference, Type: System.Nullable(Of System.Int32), IsImplicit) (Syntax: 'i1')
                Leaving: {R2}

            Next (Regular) Block[B3]
        Block[B3] - Block
            Predecessors: [B2]
            Statements (1)
                IFlowCaptureOperation: 2 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'i1')
                  Value: 
                    IInvocationOperation ( Function System.Nullable(Of System.Int32).GetValueOrDefault() As System.Int32) (OperationKind.Invocation, Type: System.Int32, IsImplicit) (Syntax: 'i1')
                      Instance Receiver: 
                        IFlowCaptureReferenceOperation: 1 (OperationKind.FlowCaptureReference, Type: System.Nullable(Of System.Int32), IsImplicit) (Syntax: 'i1')
                      Arguments(0)

            Next (Regular) Block[B5]
                Leaving: {R2}
                Entering: {R3}
    }

    Block[B4] - Block
        Predecessors: [B2]
        Statements (1)
            IFlowCaptureOperation: 2 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'i2')
              Value: 
                IParameterReferenceOperation: i2 (OperationKind.ParameterReference, Type: System.Int32) (Syntax: 'i2')

        Next (Regular) Block[B5]
            Entering: {R3}

    .locals {R3}
    {
        CaptureIds: [3]
        Block[B5] - Block
            Predecessors: [B3] [B4]
            Statements (1)
                IFlowCaptureOperation: 3 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'j1')
                  Value: 
                    IParameterReferenceOperation: j1 (OperationKind.ParameterReference, Type: System.Nullable(Of System.Int32)) (Syntax: 'j1')

            Jump if True (Regular) to Block[B7]
                IIsNullOperation (OperationKind.IsNull, Type: System.Boolean, IsImplicit) (Syntax: 'j1')
                  Operand: 
                    IFlowCaptureReferenceOperation: 3 (OperationKind.FlowCaptureReference, Type: System.Nullable(Of System.Int32), IsImplicit) (Syntax: 'j1')
                Leaving: {R3}

            Next (Regular) Block[B6]
        Block[B6] - Block
            Predecessors: [B5]
            Statements (1)
                IFlowCaptureOperation: 4 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'j1')
                  Value: 
                    IInvocationOperation ( Function System.Nullable(Of System.Int32).GetValueOrDefault() As System.Int32) (OperationKind.Invocation, Type: System.Int32, IsImplicit) (Syntax: 'j1')
                      Instance Receiver: 
                        IFlowCaptureReferenceOperation: 3 (OperationKind.FlowCaptureReference, Type: System.Nullable(Of System.Int32), IsImplicit) (Syntax: 'j1')
                      Arguments(0)

            Next (Regular) Block[B8]
                Leaving: {R3}
    }

    Block[B7] - Block
        Predecessors: [B5]
        Statements (1)
            IFlowCaptureOperation: 4 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'j2')
              Value: 
                IParameterReferenceOperation: j2 (OperationKind.ParameterReference, Type: System.Int32) (Syntax: 'j2')

        Next (Regular) Block[B8]
    Block[B8] - Block
        Predecessors: [B6] [B7]
        Statements (1)
            IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'p = New Wit ... If(j1, j2)}')
              Expression: 
                ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Object, IsImplicit) (Syntax: 'p = New Wit ... If(j1, j2)}')
                  Left: 
                    IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: System.Object, IsImplicit) (Syntax: 'p')
                  Right: 
                    IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.Object, IsImplicit) (Syntax: 'New With {  ... If(j1, j2)}')
                      Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: True, IsUserDefined: False) (MethodSymbol: null)
                        (WideningReference)
                      Operand: 
                        IAnonymousObjectCreationOperation (OperationKind.AnonymousObjectCreation, Type: <anonymous type: a As System.Int32, b As System.Int32>) (Syntax: 'New With {  ... If(j1, j2)}')
                          Initializers(2):
                              ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Int32) (Syntax: '.a = If(i1, i2)')
                                Left: 
                                  IPropertyReferenceOperation: Property <anonymous type: a As System.Int32, b As System.Int32>.a As System.Int32 (OperationKind.PropertyReference, Type: System.Int32) (Syntax: 'a')
                                    Instance Receiver: 
                                      IInstanceReferenceOperation (ReferenceKind: ImplicitReceiver) (OperationKind.InstanceReference, Type: <anonymous type: a As System.Int32, b As System.Int32>, IsImplicit) (Syntax: 'New With {  ... If(j1, j2)}')
                                Right: 
                                  IFlowCaptureReferenceOperation: 2 (OperationKind.FlowCaptureReference, Type: System.Int32, IsImplicit) (Syntax: 'If(i1, i2)')
                              ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Int32) (Syntax: '.b = If(j1, j2)')
                                Left: 
                                  IPropertyReferenceOperation: Property <anonymous type: a As System.Int32, b As System.Int32>.b As System.Int32 (OperationKind.PropertyReference, Type: System.Int32) (Syntax: 'b')
                                    Instance Receiver: 
                                      IInstanceReferenceOperation (ReferenceKind: ImplicitReceiver) (OperationKind.InstanceReference, Type: <anonymous type: a As System.Int32, b As System.Int32>, IsImplicit) (Syntax: 'New With {  ... If(j1, j2)}')
                                Right: 
                                  IFlowCaptureReferenceOperation: 4 (OperationKind.FlowCaptureReference, Type: System.Int32, IsImplicit) (Syntax: 'If(j1, j2)')

        Next (Regular) Block[B9]
            Leaving: {R1}
}

Block[B9] - Exit
    Predecessors: [B8]
    Statements (0)
]]>.Value

            VerifyFlowGraphAndDiagnosticsForTest(Of MethodBlockSyntax)(source, expectedFlowGraph, expectedDiagnostics)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation, CompilerFeature.Dataflow)>
        <Fact()>
        Public Sub AnonymousObjectCreation_LambdaContext()
            Dim source = <![CDATA[
Imports System

Class C
    Sub M(c1 As C, c2 As C)'BIND:"Sub M"
        Dim x, y As New With { .a = c1, .b = Function() If (c2, .a) }
    End Sub
End Class
]]>.Value

            Dim expectedDiagnostics = <![CDATA[
BC36549: Anonymous type property 'a' cannot be used in the definition of a lambda expression within the same initialization list.
        Dim x, y As New With { .a = c1, .b = Function() If (c2, .a) }
                                                                ~~
BC36549: Anonymous type property 'a' cannot be used in the definition of a lambda expression within the same initialization list.
        Dim x, y As New With { .a = c1, .b = Function() If (c2, .a) }
                                                                ~~
]]>.Value

            Dim expectedFlowGraph = <![CDATA[
Block[B0] - Entry
    Statements (0)
    Next (Regular) Block[B1]
        Entering: {R1} {R2}

.locals {R1}
{
    Locals: [x As <anonymous type: a As C, b As Function <generated method>() As ?>] [y As <anonymous type: a As C, b As Function <generated method>() As ?>]
    .locals {R2}
    {
        CaptureIds: [0] [1]
        Block[B1] - Block
            Predecessors: [B0]
            Statements (3)
                IFlowCaptureOperation: 0 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'c1')
                  Value: 
                    IParameterReferenceOperation: c1 (OperationKind.ParameterReference, Type: C) (Syntax: 'c1')

                IFlowCaptureOperation: 1 (OperationKind.FlowCapture, Type: null, IsInvalid, IsImplicit) (Syntax: 'Function() If (c2, .a)')
                  Value: 
                    IDelegateCreationOperation (OperationKind.DelegateCreation, Type: Function <generated method>() As ?, IsInvalid, IsImplicit) (Syntax: 'Function() If (c2, .a)')
                      Target: 
                        IFlowAnonymousFunctionOperation (Symbol: Function () As ?) (OperationKind.FlowAnonymousFunction, Type: null, IsInvalid) (Syntax: 'Function() If (c2, .a)')
                        {
                            Block[B0#A0] - Entry
                                Statements (0)
                                Next (Regular) Block[B1#A0]
                                    Entering: {R1#A0} {R2#A0}

                            .locals {R1#A0}
                            {
                                Locals: [<anonymous local> As ?]
                                CaptureIds: [5]
                                .locals {R2#A0}
                                {
                                    CaptureIds: [4]
                                    Block[B1#A0] - Block
                                        Predecessors: [B0#A0]
                                        Statements (1)
                                            IFlowCaptureOperation: 4 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'c2')
                                              Value: 
                                                IParameterReferenceOperation: c2 (OperationKind.ParameterReference, Type: C) (Syntax: 'c2')

                                        Jump if True (Regular) to Block[B3#A0]
                                            IIsNullOperation (OperationKind.IsNull, Type: System.Boolean, IsImplicit) (Syntax: 'c2')
                                              Operand: 
                                                IFlowCaptureReferenceOperation: 4 (OperationKind.FlowCaptureReference, Type: C, IsImplicit) (Syntax: 'c2')
                                            Leaving: {R2#A0}

                                        Next (Regular) Block[B2#A0]
                                    Block[B2#A0] - Block
                                        Predecessors: [B1#A0]
                                        Statements (1)
                                            IFlowCaptureOperation: 5 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'c2')
                                              Value: 
                                                IFlowCaptureReferenceOperation: 4 (OperationKind.FlowCaptureReference, Type: C, IsImplicit) (Syntax: 'c2')

                                        Next (Regular) Block[B4#A0]
                                            Leaving: {R2#A0}
                                }

                                Block[B3#A0] - Block
                                    Predecessors: [B1#A0]
                                    Statements (1)
                                        IFlowCaptureOperation: 5 (OperationKind.FlowCapture, Type: null, IsInvalid, IsImplicit) (Syntax: '.a')
                                          Value: 
                                            IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: C, IsInvalid, IsImplicit) (Syntax: '.a')

                                    Next (Regular) Block[B4#A0]
                                Block[B4#A0] - Block
                                    Predecessors: [B2#A0] [B3#A0]
                                    Statements (0)
                                    Next (Return) Block[B5#A0]
                                        IFlowCaptureReferenceOperation: 5 (OperationKind.FlowCaptureReference, Type: C, IsInvalid, IsImplicit) (Syntax: 'If (c2, .a)')
                                        Leaving: {R1#A0}
                            }

                            Block[B5#A0] - Exit
                                Predecessors: [B4#A0]
                                Statements (0)
                        }

                ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: <anonymous type: a As C, b As Function <generated method>() As ?>, IsInvalid, IsImplicit) (Syntax: 'x, y As New ...  (c2, .a) }')
                  Left: 
                    ILocalReferenceOperation: x (IsDeclaration: True) (OperationKind.LocalReference, Type: <anonymous type: a As C, b As Function <generated method>() As ?>, IsImplicit) (Syntax: 'x')
                  Right: 
                    IAnonymousObjectCreationOperation (OperationKind.AnonymousObjectCreation, Type: <anonymous type: a As C, b As Function <generated method>() As ?>, IsInvalid) (Syntax: 'New With {  ...  (c2, .a) }')
                      Initializers(2):
                          ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: C) (Syntax: '.a = c1')
                            Left: 
                              IPropertyReferenceOperation: Property <anonymous type: a As C, b As Function <generated method>() As ?>.a As C (OperationKind.PropertyReference, Type: C) (Syntax: 'a')
                                Instance Receiver: 
                                  IInstanceReferenceOperation (ReferenceKind: ImplicitReceiver) (OperationKind.InstanceReference, Type: <anonymous type: a As C, b As Function <generated method>() As ?>, IsInvalid, IsImplicit) (Syntax: 'New With {  ...  (c2, .a) }')
                            Right: 
                              IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: C, IsImplicit) (Syntax: 'c1')
                          ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: Function <generated method>() As ?, IsInvalid) (Syntax: '.b = Functi ... If (c2, .a)')
                            Left: 
                              IPropertyReferenceOperation: Property <anonymous type: a As C, b As Function <generated method>() As ?>.b As <generated method> (OperationKind.PropertyReference, Type: Function <generated method>() As ?) (Syntax: 'b')
                                Instance Receiver: 
                                  IInstanceReferenceOperation (ReferenceKind: ImplicitReceiver) (OperationKind.InstanceReference, Type: <anonymous type: a As C, b As Function <generated method>() As ?>, IsInvalid, IsImplicit) (Syntax: 'New With {  ...  (c2, .a) }')
                            Right: 
                              IFlowCaptureReferenceOperation: 1 (OperationKind.FlowCaptureReference, Type: Function <generated method>() As ?, IsInvalid, IsImplicit) (Syntax: 'Function() If (c2, .a)')

            Next (Regular) Block[B2]
                Leaving: {R2}
                Entering: {R3}
    }
    .locals {R3}
    {
        CaptureIds: [2] [3]
        Block[B2] - Block
            Predecessors: [B1]
            Statements (3)
                IFlowCaptureOperation: 2 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'c1')
                  Value: 
                    IParameterReferenceOperation: c1 (OperationKind.ParameterReference, Type: C) (Syntax: 'c1')

                IFlowCaptureOperation: 3 (OperationKind.FlowCapture, Type: null, IsInvalid, IsImplicit) (Syntax: 'Function() If (c2, .a)')
                  Value: 
                    IDelegateCreationOperation (OperationKind.DelegateCreation, Type: Function <generated method>() As ?, IsInvalid, IsImplicit) (Syntax: 'Function() If (c2, .a)')
                      Target: 
                        IFlowAnonymousFunctionOperation (Symbol: Function () As ?) (OperationKind.FlowAnonymousFunction, Type: null, IsInvalid) (Syntax: 'Function() If (c2, .a)')
                        {
                            Block[B0#A1] - Entry
                                Statements (0)
                                Next (Regular) Block[B1#A1]
                                    Entering: {R1#A1} {R2#A1}

                            .locals {R1#A1}
                            {
                                Locals: [<anonymous local> As ?]
                                CaptureIds: [7]
                                .locals {R2#A1}
                                {
                                    CaptureIds: [6]
                                    Block[B1#A1] - Block
                                        Predecessors: [B0#A1]
                                        Statements (1)
                                            IFlowCaptureOperation: 6 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'c2')
                                              Value: 
                                                IParameterReferenceOperation: c2 (OperationKind.ParameterReference, Type: C) (Syntax: 'c2')

                                        Jump if True (Regular) to Block[B3#A1]
                                            IIsNullOperation (OperationKind.IsNull, Type: System.Boolean, IsImplicit) (Syntax: 'c2')
                                              Operand: 
                                                IFlowCaptureReferenceOperation: 6 (OperationKind.FlowCaptureReference, Type: C, IsImplicit) (Syntax: 'c2')
                                            Leaving: {R2#A1}

                                        Next (Regular) Block[B2#A1]
                                    Block[B2#A1] - Block
                                        Predecessors: [B1#A1]
                                        Statements (1)
                                            IFlowCaptureOperation: 7 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'c2')
                                              Value: 
                                                IFlowCaptureReferenceOperation: 6 (OperationKind.FlowCaptureReference, Type: C, IsImplicit) (Syntax: 'c2')

                                        Next (Regular) Block[B4#A1]
                                            Leaving: {R2#A1}
                                }

                                Block[B3#A1] - Block
                                    Predecessors: [B1#A1]
                                    Statements (1)
                                        IFlowCaptureOperation: 7 (OperationKind.FlowCapture, Type: null, IsInvalid, IsImplicit) (Syntax: '.a')
                                          Value: 
                                            IFlowCaptureReferenceOperation: 2 (OperationKind.FlowCaptureReference, Type: C, IsInvalid, IsImplicit) (Syntax: '.a')

                                    Next (Regular) Block[B4#A1]
                                Block[B4#A1] - Block
                                    Predecessors: [B2#A1] [B3#A1]
                                    Statements (0)
                                    Next (Return) Block[B5#A1]
                                        IFlowCaptureReferenceOperation: 7 (OperationKind.FlowCaptureReference, Type: C, IsInvalid, IsImplicit) (Syntax: 'If (c2, .a)')
                                        Leaving: {R1#A1}
                            }

                            Block[B5#A1] - Exit
                                Predecessors: [B4#A1]
                                Statements (0)
                        }

                ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: <anonymous type: a As C, b As Function <generated method>() As ?>, IsInvalid, IsImplicit) (Syntax: 'x, y As New ...  (c2, .a) }')
                  Left: 
                    ILocalReferenceOperation: y (IsDeclaration: True) (OperationKind.LocalReference, Type: <anonymous type: a As C, b As Function <generated method>() As ?>, IsImplicit) (Syntax: 'y')
                  Right: 
                    IAnonymousObjectCreationOperation (OperationKind.AnonymousObjectCreation, Type: <anonymous type: a As C, b As Function <generated method>() As ?>, IsInvalid) (Syntax: 'New With {  ...  (c2, .a) }')
                      Initializers(2):
                          ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: C) (Syntax: '.a = c1')
                            Left: 
                              IPropertyReferenceOperation: Property <anonymous type: a As C, b As Function <generated method>() As ?>.a As C (OperationKind.PropertyReference, Type: C) (Syntax: 'a')
                                Instance Receiver: 
                                  IInstanceReferenceOperation (ReferenceKind: ImplicitReceiver) (OperationKind.InstanceReference, Type: <anonymous type: a As C, b As Function <generated method>() As ?>, IsInvalid, IsImplicit) (Syntax: 'New With {  ...  (c2, .a) }')
                            Right: 
                              IFlowCaptureReferenceOperation: 2 (OperationKind.FlowCaptureReference, Type: C, IsImplicit) (Syntax: 'c1')
                          ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: Function <generated method>() As ?, IsInvalid) (Syntax: '.b = Functi ... If (c2, .a)')
                            Left: 
                              IPropertyReferenceOperation: Property <anonymous type: a As C, b As Function <generated method>() As ?>.b As <generated method> (OperationKind.PropertyReference, Type: Function <generated method>() As ?) (Syntax: 'b')
                                Instance Receiver: 
                                  IInstanceReferenceOperation (ReferenceKind: ImplicitReceiver) (OperationKind.InstanceReference, Type: <anonymous type: a As C, b As Function <generated method>() As ?>, IsInvalid, IsImplicit) (Syntax: 'New With {  ...  (c2, .a) }')
                            Right: 
                              IFlowCaptureReferenceOperation: 3 (OperationKind.FlowCaptureReference, Type: Function <generated method>() As ?, IsInvalid, IsImplicit) (Syntax: 'Function() If (c2, .a)')

            Next (Regular) Block[B3]
                Leaving: {R3} {R1}
    }
}

Block[B3] - Exit
    Predecessors: [B2]
    Statements (0)
]]>.Value

            VerifyFlowGraphAndDiagnosticsForTest(Of MethodBlockSyntax)(source, expectedFlowGraph, expectedDiagnostics)
        End Sub

        <Fact()>
        Public Sub ObjectCreation_WithMissingMember()
            Dim source = <![CDATA[
Module Program
    Sub Main() 'BIND:"Sub Main()"
        Dim item = New C() With {.}
    End Sub
End Module
Class C
End Class
]]>.Value
            Dim expectedDiagnostics = <![CDATA[
BC30201: Expression expected.
        Dim item = New C() With {.}
                                  ~
BC30203: Identifier expected.
        Dim item = New C() With {.}
                                  ~
BC30984: '=' expected (object initializer).
        Dim item = New C() With {.}
                                  ~
]]>.Value
            Dim expectedOperationTree = <![CDATA[
IBlockOperation (3 statements, 1 locals) (OperationKind.Block, Type: null, IsInvalid) (Syntax: 'Sub Main()  ... End Sub')
  Locals: Local_1: item As C
  IVariableDeclarationGroupOperation (1 declarations) (OperationKind.VariableDeclarationGroup, Type: null, IsInvalid) (Syntax: 'Dim item =  ... () With {.}')
    IVariableDeclarationOperation (1 declarators) (OperationKind.VariableDeclaration, Type: null, IsInvalid) (Syntax: 'item = New C() With {.}')
      Declarators:
          IVariableDeclaratorOperation (Symbol: item As C) (OperationKind.VariableDeclarator, Type: null) (Syntax: 'item')
            Initializer:
              null
      Initializer:
        IVariableInitializerOperation (OperationKind.VariableInitializer, Type: null, IsInvalid) (Syntax: '= New C() With {.}')
          IObjectCreationOperation (Constructor: Sub C..ctor()) (OperationKind.ObjectCreation, Type: C, IsInvalid) (Syntax: 'New C() With {.}')
            Arguments(0)
            Initializer:
              IObjectOrCollectionInitializerOperation (OperationKind.ObjectOrCollectionInitializer, Type: C, IsInvalid) (Syntax: 'With {.}')
                Initializers(1):
                    ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: ?, IsInvalid) (Syntax: '.')
                      Left:
                        IInvalidOperation (OperationKind.Invalid, Type: ?, IsInvalid, IsImplicit) (Syntax: '.')
                          Children(0)
                      Right:
                        IInvalidOperation (OperationKind.Invalid, Type: null, IsInvalid) (Syntax: '')
                          Children(0)
  ILabeledOperation (Label: exit) (OperationKind.Labeled, Type: null, IsImplicit) (Syntax: 'End Sub')
    Statement:
      null
  IReturnOperation (OperationKind.Return, Type: null, IsImplicit) (Syntax: 'End Sub')
    ReturnedValue:
      null
]]>.Value

            Dim expectedFlowGraph = <![CDATA[
Block[B0] - Entry
    Statements (0)
    Next (Regular) Block[B1]
        Entering: {R1}
.locals {R1}
{
    Locals: [item As C]
    CaptureIds: [0]
    Block[B1] - Block
        Predecessors: [B0]
        Statements (3)
            IFlowCaptureOperation: 0 (OperationKind.FlowCapture, Type: null, IsInvalid, IsImplicit) (Syntax: 'New C() With {.}')
              Value:
                IObjectCreationOperation (Constructor: Sub C..ctor()) (OperationKind.ObjectCreation, Type: C, IsInvalid) (Syntax: 'New C() With {.}')
                  Arguments(0)
                  Initializer:
                    null
            ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: ?, IsInvalid) (Syntax: '.')
              Left:
                IInvalidOperation (OperationKind.Invalid, Type: ?, IsInvalid, IsImplicit) (Syntax: '.')
                  Children(0)
              Right:
                IInvalidOperation (OperationKind.Invalid, Type: null, IsInvalid) (Syntax: '')
                  Children(0)
            ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: C, IsInvalid, IsImplicit) (Syntax: 'item = New C() With {.}')
              Left:
                ILocalReferenceOperation: item (IsDeclaration: True) (OperationKind.LocalReference, Type: C, IsImplicit) (Syntax: 'item')
              Right:
                IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: C, IsInvalid, IsImplicit) (Syntax: 'New C() With {.}')
        Next (Regular) Block[B2]
            Leaving: {R1}
}
Block[B2] - Exit
    Predecessors: [B1]
    Statements (0)
]]>.Value
            VerifyOperationTreeAndDiagnosticsForTest(Of MethodBlockSyntax)(source, expectedOperationTree, expectedDiagnostics)
            VerifyFlowGraphAndDiagnosticsForTest(Of MethodBlockSyntax)(source, expectedFlowGraph, expectedDiagnostics)
        End Sub

        <Fact()>
        Public Sub AnonymousObjectCreation_WithMissingMember_01()
            Dim source = <![CDATA[
Module Program
    Sub Main() 'BIND:"Sub Main()"
        Dim item = New With {.}
    End Sub
End Module
]]>.Value
            Dim expectedDiagnostics = <![CDATA[
BC30201: Expression expected.
        Dim item = New With {.}
                              ~
BC30203: Identifier expected.
        Dim item = New With {.}
                              ~
BC30984: '=' expected (object initializer).
        Dim item = New With {.}
                              ~
]]>.Value
            Dim expectedOperationTree = <![CDATA[
IBlockOperation (3 statements, 1 locals) (OperationKind.Block, Type: null, IsInvalid) (Syntax: 'Sub Main()  ... End Sub')
  Locals: Local_1: item As <anonymous type: $0 As ?>
  IVariableDeclarationGroupOperation (1 declarations) (OperationKind.VariableDeclarationGroup, Type: null, IsInvalid) (Syntax: 'Dim item = New With {.}')
    IVariableDeclarationOperation (1 declarators) (OperationKind.VariableDeclaration, Type: null, IsInvalid) (Syntax: 'item = New With {.}')
      Declarators:
          IVariableDeclaratorOperation (Symbol: item As <anonymous type: $0 As ?>) (OperationKind.VariableDeclarator, Type: null) (Syntax: 'item')
            Initializer:
              null
      Initializer:
        IVariableInitializerOperation (OperationKind.VariableInitializer, Type: null, IsInvalid) (Syntax: '= New With {.}')
          IAnonymousObjectCreationOperation (OperationKind.AnonymousObjectCreation, Type: <anonymous type: $0 As ?>, IsInvalid) (Syntax: 'New With {.}')
            Initializers(1):
                ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: ?, IsInvalid) (Syntax: '.')
                  Left:
                    IPropertyReferenceOperation: Property <anonymous type: $0 As ?>.$0 As ? (OperationKind.PropertyReference, Type: ?, IsInvalid) (Syntax: '')
                      Instance Receiver:
                        IInstanceReferenceOperation (ReferenceKind: ImplicitReceiver) (OperationKind.InstanceReference, Type: <anonymous type: $0 As ?>, IsInvalid, IsImplicit) (Syntax: 'New With {.}')
                  Right:
                    IInvalidOperation (OperationKind.Invalid, Type: null, IsInvalid) (Syntax: '')
                      Children(0)
  ILabeledOperation (Label: exit) (OperationKind.Labeled, Type: null, IsImplicit) (Syntax: 'End Sub')
    Statement:
      null
  IReturnOperation (OperationKind.Return, Type: null, IsImplicit) (Syntax: 'End Sub')
    ReturnedValue:
      null
]]>.Value
            Dim expectedFlowGraph = <![CDATA[
Block[B0] - Entry
    Statements (0)
    Next (Regular) Block[B1]
        Entering: {R1}
.locals {R1}
{
    Locals: [item As <anonymous type: $0 As ?>]
    CaptureIds: [0]
    Block[B1] - Block
        Predecessors: [B0]
        Statements (2)
            IFlowCaptureOperation: 0 (OperationKind.FlowCapture, Type: null, IsInvalid, IsImplicit) (Syntax: '')
              Value:
                IInvalidOperation (OperationKind.Invalid, Type: null, IsInvalid) (Syntax: '')
                  Children(0)
            ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: <anonymous type: $0 As ?>, IsInvalid, IsImplicit) (Syntax: 'item = New With {.}')
              Left:
                ILocalReferenceOperation: item (IsDeclaration: True) (OperationKind.LocalReference, Type: <anonymous type: $0 As ?>, IsImplicit) (Syntax: 'item')
              Right:
                IAnonymousObjectCreationOperation (OperationKind.AnonymousObjectCreation, Type: <anonymous type: $0 As ?>, IsInvalid) (Syntax: 'New With {.}')
                  Initializers(1):
                      ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: ?, IsInvalid) (Syntax: '.')
                        Left:
                          IPropertyReferenceOperation: Property <anonymous type: $0 As ?>.$0 As ? (OperationKind.PropertyReference, Type: ?, IsInvalid) (Syntax: '')
                            Instance Receiver:
                              IInstanceReferenceOperation (ReferenceKind: ImplicitReceiver) (OperationKind.InstanceReference, Type: <anonymous type: $0 As ?>, IsInvalid, IsImplicit) (Syntax: 'New With {.}')
                        Right:
                          IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: null, IsInvalid, IsImplicit) (Syntax: '')
        Next (Regular) Block[B2]
            Leaving: {R1}
}
Block[B2] - Exit
    Predecessors: [B1]
    Statements (0)
]]>.Value
            VerifyOperationTreeAndDiagnosticsForTest(Of MethodBlockSyntax)(source, expectedOperationTree, expectedDiagnostics)
            VerifyFlowGraphAndDiagnosticsForTest(Of MethodBlockSyntax)(source, expectedFlowGraph, expectedDiagnostics)

            Dim comp = CreateCompilation(source)
            Dim tree = comp.SyntaxTrees.Single()
            Dim model = comp.GetSemanticModel(tree)
            Dim node = tree.GetRoot().DescendantNodes().OfType(Of NamedFieldInitializerSyntax).Single()
            Dim symbolInfo = model.GetSymbolInfo(node.Name)
            Assert.Null(symbolInfo.Symbol)
        End Sub

        <Fact()>
        Public Sub AnonymousObjectCreation_WithMissingMember_02()
            Dim source = <![CDATA[
Module Program
    Sub Main() 'BIND:"Sub Main()"
        Dim item = New With {.a = 1, .b = .}
    End Sub
End Module
]]>.Value
            Dim expectedDiagnostics = <![CDATA[
    BC30203: Identifier expected.
        Dim item = New With {.a = 1, .b = .}
                                           ~
]]>.Value
            Dim expectedOperationTree = <![CDATA[
IBlockOperation (3 statements, 1 locals) (OperationKind.Block, Type: null, IsInvalid) (Syntax: 'Sub Main()  ... End Sub')
  Locals: Local_1: item As <anonymous type: a As System.Int32, b As ?>
  IVariableDeclarationGroupOperation (1 declarations) (OperationKind.VariableDeclarationGroup, Type: null, IsInvalid) (Syntax: 'Dim item =  ...  1, .b = .}')
    IVariableDeclarationOperation (1 declarators) (OperationKind.VariableDeclaration, Type: null, IsInvalid) (Syntax: 'item = New  ...  1, .b = .}')
      Declarators:
          IVariableDeclaratorOperation (Symbol: item As <anonymous type: a As System.Int32, b As ?>) (OperationKind.VariableDeclarator, Type: null) (Syntax: 'item')
            Initializer:
              null
      Initializer:
        IVariableInitializerOperation (OperationKind.VariableInitializer, Type: null, IsInvalid) (Syntax: '= New With  ...  1, .b = .}')
          IAnonymousObjectCreationOperation (OperationKind.AnonymousObjectCreation, Type: <anonymous type: a As System.Int32, b As ?>, IsInvalid) (Syntax: 'New With {. ...  1, .b = .}')
            Initializers(2):
                ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Int32, Constant: 1) (Syntax: '.a = 1')
                  Left:
                    IPropertyReferenceOperation: Property <anonymous type: a As System.Int32, b As ?>.a As System.Int32 (OperationKind.PropertyReference, Type: System.Int32) (Syntax: 'a')
                      Instance Receiver:
                        IInstanceReferenceOperation (ReferenceKind: ImplicitReceiver) (OperationKind.InstanceReference, Type: <anonymous type: a As System.Int32, b As ?>, IsInvalid, IsImplicit) (Syntax: 'New With {. ...  1, .b = .}')
                  Right:
                    ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 1) (Syntax: '1')
                ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: ?, IsInvalid) (Syntax: '.b = .')
                  Left:
                    IPropertyReferenceOperation: Property <anonymous type: a As System.Int32, b As ?>.b As ? (OperationKind.PropertyReference, Type: ?) (Syntax: 'b')
                      Instance Receiver:
                        IInstanceReferenceOperation (ReferenceKind: ImplicitReceiver) (OperationKind.InstanceReference, Type: <anonymous type: a As System.Int32, b As ?>, IsInvalid, IsImplicit) (Syntax: 'New With {. ...  1, .b = .}')
                  Right:
                    IInvalidOperation (OperationKind.Invalid, Type: ?, IsInvalid) (Syntax: '.')
                      Children(0)
  ILabeledOperation (Label: exit) (OperationKind.Labeled, Type: null, IsImplicit) (Syntax: 'End Sub')
    Statement:
      null
  IReturnOperation (OperationKind.Return, Type: null, IsImplicit) (Syntax: 'End Sub')
    ReturnedValue:
      null
]]>.Value
            Dim expectedFlowGraph = <![CDATA[
Block[B0] - Entry
    Statements (0)
    Next (Regular) Block[B1]
        Entering: {R1}
.locals {R1}
{
    Locals: [item As <anonymous type: a As System.Int32, b As ?>]
    CaptureIds: [0] [1]
    Block[B1] - Block
        Predecessors: [B0]
        Statements (3)
            IFlowCaptureOperation: 0 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: '1')
              Value:
                ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 1) (Syntax: '1')
            IFlowCaptureOperation: 1 (OperationKind.FlowCapture, Type: null, IsInvalid, IsImplicit) (Syntax: '.')
              Value:
                IInvalidOperation (OperationKind.Invalid, Type: ?, IsInvalid) (Syntax: '.')
                  Children(0)
            ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: <anonymous type: a As System.Int32, b As ?>, IsInvalid, IsImplicit) (Syntax: 'item = New  ...  1, .b = .}')
              Left:
                ILocalReferenceOperation: item (IsDeclaration: True) (OperationKind.LocalReference, Type: <anonymous type: a As System.Int32, b As ?>, IsImplicit) (Syntax: 'item')
              Right:
                IAnonymousObjectCreationOperation (OperationKind.AnonymousObjectCreation, Type: <anonymous type: a As System.Int32, b As ?>, IsInvalid) (Syntax: 'New With {. ...  1, .b = .}')
                  Initializers(2):
                      ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Int32, Constant: 1) (Syntax: '.a = 1')
                        Left:
                          IPropertyReferenceOperation: Property <anonymous type: a As System.Int32, b As ?>.a As System.Int32 (OperationKind.PropertyReference, Type: System.Int32) (Syntax: 'a')
                            Instance Receiver:
                              IInstanceReferenceOperation (ReferenceKind: ImplicitReceiver) (OperationKind.InstanceReference, Type: <anonymous type: a As System.Int32, b As ?>, IsInvalid, IsImplicit) (Syntax: 'New With {. ...  1, .b = .}')
                        Right:
                          IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: System.Int32, Constant: 1, IsImplicit) (Syntax: '1')
                      ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: ?, IsInvalid) (Syntax: '.b = .')
                        Left:
                          IPropertyReferenceOperation: Property <anonymous type: a As System.Int32, b As ?>.b As ? (OperationKind.PropertyReference, Type: ?) (Syntax: 'b')
                            Instance Receiver:
                              IInstanceReferenceOperation (ReferenceKind: ImplicitReceiver) (OperationKind.InstanceReference, Type: <anonymous type: a As System.Int32, b As ?>, IsInvalid, IsImplicit) (Syntax: 'New With {. ...  1, .b = .}')
                        Right:
                          IFlowCaptureReferenceOperation: 1 (OperationKind.FlowCaptureReference, Type: ?, IsInvalid, IsImplicit) (Syntax: '.')
        Next (Regular) Block[B2]
            Leaving: {R1}
}
Block[B2] - Exit
    Predecessors: [B1]
    Statements (0)
]]>.Value
            VerifyOperationTreeAndDiagnosticsForTest(Of MethodBlockSyntax)(source, expectedOperationTree, expectedDiagnostics)
            VerifyFlowGraphAndDiagnosticsForTest(Of MethodBlockSyntax)(source, expectedFlowGraph, expectedDiagnostics)

            Dim comp = CreateCompilation(source)
            Dim tree = comp.SyntaxTrees.Single()
            Dim model = comp.GetSemanticModel(tree)
            Dim nodes = tree.GetRoot().DescendantNodes().OfType(Of NamedFieldInitializerSyntax).ToArray()
            Dim symbolInfo = model.GetSymbolInfo(nodes(0).Name)
            Assert.Equal("Property <anonymous type: a As System.Int32, b As ?>.a As System.Int32", symbolInfo.Symbol.ToTestDisplayString())
            symbolInfo = model.GetSymbolInfo(nodes(1).Name)
            Assert.Equal("Property <anonymous type: a As System.Int32, b As ?>.b As ?", symbolInfo.Symbol.ToTestDisplayString())
        End Sub
    End Class
End Namespace
