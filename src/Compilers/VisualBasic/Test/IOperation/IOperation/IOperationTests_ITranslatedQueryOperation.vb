' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports Microsoft.CodeAnalysis.Test.Utilities
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.VisualBasic.UnitTests.Semantics
    Partial Public Class IOperationTests
        Inherits SemanticModelTestBase

        <CompilerTrait(CompilerFeature.IOperation)>
        <Fact>
        Public Sub Aggregate_01()
            Dim source = <![CDATA[
Imports System
Imports System.Linq

Public Structure C

    Sub M(a As Integer(), result As object)
        result = Aggregate y In a Into Count(), Sum(y) 'BIND:"Aggregate y In a Into Count(), Sum(y)"
    End Sub
End Structure
]]>.Value

            Dim expectedOperationTree = <![CDATA[
ITranslatedQueryOperation (OperationKind.TranslatedQuery, Type: <anonymous type: Key Count As System.Int32, Key Sum As System.Int32>) (Syntax: 'Aggregate y ... t(), Sum(y)')
  Expression: 
    IAggregateQueryOperation (OperationKind.None, Type: <anonymous type: Key Count As System.Int32, Key Sum As System.Int32>) (Syntax: 'Aggregate y ... t(), Sum(y)')
      Group: 
        IParameterReferenceOperation: a (OperationKind.ParameterReference, Type: System.Int32()) (Syntax: 'a')
      Aggregation: 
        IAnonymousObjectCreationOperation (OperationKind.AnonymousObjectCreation, Type: <anonymous type: Key Count As System.Int32, Key Sum As System.Int32>, IsImplicit) (Syntax: 'Aggregate y ... t(), Sum(y)')
          Initializers(2):
              IInvocationOperation ( Function System.Collections.Generic.IEnumerable(Of System.Int32).Count() As System.Int32) (OperationKind.Invocation, Type: System.Int32, IsImplicit) (Syntax: 'Count()')
                Instance Receiver: 
                  IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.Collections.Generic.IEnumerable(Of System.Int32), IsImplicit) (Syntax: 'Aggregate y ... t(), Sum(y)')
                    Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: True, IsUserDefined: False) (MethodSymbol: null)
                    Operand: 
                      IPlaceholderOperation (OperationKind.None, Type: System.Int32(), IsImplicit) (Syntax: 'Aggregate y ... t(), Sum(y)')
                Arguments(0)
              IInvocationOperation ( Function System.Collections.Generic.IEnumerable(Of System.Int32).Sum(selector As System.Func(Of System.Int32, System.Int32)) As System.Int32) (OperationKind.Invocation, Type: System.Int32, IsImplicit) (Syntax: 'Sum(y)')
                Instance Receiver: 
                  IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.Collections.Generic.IEnumerable(Of System.Int32), IsImplicit) (Syntax: 'Aggregate y ... t(), Sum(y)')
                    Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: True, IsUserDefined: False) (MethodSymbol: null)
                    Operand: 
                      IPlaceholderOperation (OperationKind.None, Type: System.Int32(), IsImplicit) (Syntax: 'Aggregate y ... t(), Sum(y)')
                Arguments(1):
                    IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: selector) (OperationKind.Argument, Type: null, IsImplicit) (Syntax: 'y')
                      IDelegateCreationOperation (OperationKind.DelegateCreation, Type: System.Func(Of System.Int32, System.Int32), IsImplicit) (Syntax: 'y')
                        Target: 
                          IAnonymousFunctionOperation (Symbol: Function (y As System.Int32) As System.Int32) (OperationKind.AnonymousFunction, Type: null, IsImplicit) (Syntax: 'y')
                            IBlockOperation (1 statements) (OperationKind.Block, Type: null, IsImplicit) (Syntax: 'y')
                              IReturnOperation (OperationKind.Return, Type: null, IsImplicit) (Syntax: 'y')
                                ReturnedValue: 
                                  IParameterReferenceOperation: y (OperationKind.ParameterReference, Type: System.Int32) (Syntax: 'y')
                      InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                      OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
]]>.Value

            VerifyOperationTreeForTest(Of QueryExpressionSyntax)(source, expectedOperationTree)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation, CompilerFeature.Dataflow)>
        <Fact()>
        Public Sub AggregateFlow_01()
            Dim source = <![CDATA[
Imports System
Imports System.Linq

Public Structure C

    Sub M(a As Integer(), result As object) 'BIND:"Sub M"
        result = Aggregate y In a Into Count(), Sum(y)
    End Sub
End Structure
]]>.Value

            Dim expectedDiagnostics = String.Empty

            Dim expectedGraph = <![CDATA[
Block[B0] - Entry
    Statements (0)
    Next (Regular) Block[B1]
Block[B1] - Block
    Predecessors: [B0]
    Statements (5)
        IFlowCaptureOperation: 0 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'result')
          Value: 
            IParameterReferenceOperation: result (OperationKind.ParameterReference, Type: System.Object) (Syntax: 'result')

        IFlowCaptureOperation: 1 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'a')
          Value: 
            IParameterReferenceOperation: a (OperationKind.ParameterReference, Type: System.Int32()) (Syntax: 'a')

        IFlowCaptureOperation: 2 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'Count()')
          Value: 
            IInvocationOperation ( Function System.Collections.Generic.IEnumerable(Of System.Int32).Count() As System.Int32) (OperationKind.Invocation, Type: System.Int32, IsImplicit) (Syntax: 'Count()')
              Instance Receiver: 
                IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.Collections.Generic.IEnumerable(Of System.Int32), IsImplicit) (Syntax: 'Aggregate y ... t(), Sum(y)')
                  Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: True, IsUserDefined: False) (MethodSymbol: null)
                    (WideningReference)
                  Operand: 
                    IFlowCaptureReferenceOperation: 1 (OperationKind.FlowCaptureReference, Type: System.Int32(), IsImplicit) (Syntax: 'a')
              Arguments(0)

        IFlowCaptureOperation: 3 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'Sum(y)')
          Value: 
            IInvocationOperation ( Function System.Collections.Generic.IEnumerable(Of System.Int32).Sum(selector As System.Func(Of System.Int32, System.Int32)) As System.Int32) (OperationKind.Invocation, Type: System.Int32, IsImplicit) (Syntax: 'Sum(y)')
              Instance Receiver: 
                IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.Collections.Generic.IEnumerable(Of System.Int32), IsImplicit) (Syntax: 'Aggregate y ... t(), Sum(y)')
                  Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: True, IsUserDefined: False) (MethodSymbol: null)
                    (WideningReference)
                  Operand: 
                    IFlowCaptureReferenceOperation: 1 (OperationKind.FlowCaptureReference, Type: System.Int32(), IsImplicit) (Syntax: 'a')
              Arguments(1):
                  IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: selector) (OperationKind.Argument, Type: null, IsImplicit) (Syntax: 'y')
                    IDelegateCreationOperation (OperationKind.DelegateCreation, Type: System.Func(Of System.Int32, System.Int32), IsImplicit) (Syntax: 'y')
                      Target: 
                        IFlowAnonymousFunctionOperation (Symbol: Function (y As System.Int32) As System.Int32) (OperationKind.FlowAnonymousFunction, Type: null, IsImplicit) (Syntax: 'y')
                        {
                            Block[B0#A0] - Entry
                                Statements (0)
                                Next (Regular) Block[B1#A0]
                            Block[B1#A0] - Block
                                Predecessors: [B0#A0]
                                Statements (0)
                                Next (Return) Block[B2#A0]
                                    IParameterReferenceOperation: y (OperationKind.ParameterReference, Type: System.Int32) (Syntax: 'y')
                            Block[B2#A0] - Exit
                                Predecessors: [B1#A0]
                                Statements (0)
                        }
                    InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                    OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)

        IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'result = Ag ... t(), Sum(y)')
          Expression: 
            ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Object, IsImplicit) (Syntax: 'result = Ag ... t(), Sum(y)')
              Left: 
                IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: System.Object, IsImplicit) (Syntax: 'result')
              Right: 
                IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.Object, IsImplicit) (Syntax: 'Aggregate y ... t(), Sum(y)')
                  Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: True, IsUserDefined: False) (MethodSymbol: null)
                    (WideningReference)
                  Operand: 
                    ITranslatedQueryOperation (OperationKind.TranslatedQuery, Type: <anonymous type: Key Count As System.Int32, Key Sum As System.Int32>) (Syntax: 'Aggregate y ... t(), Sum(y)')
                      Expression: 
                        IAnonymousObjectCreationOperation (OperationKind.AnonymousObjectCreation, Type: <anonymous type: Key Count As System.Int32, Key Sum As System.Int32>, IsImplicit) (Syntax: 'Aggregate y ... t(), Sum(y)')
                          Initializers(2):
                              IFlowCaptureReferenceOperation: 2 (OperationKind.FlowCaptureReference, Type: System.Int32, IsImplicit) (Syntax: 'Count()')
                              IFlowCaptureReferenceOperation: 3 (OperationKind.FlowCaptureReference, Type: System.Int32, IsImplicit) (Syntax: 'Sum(y)')

    Next (Regular) Block[B2]
Block[B2] - Exit
    Predecessors: [B1]
    Statements (0)
]]>.Value

            VerifyFlowGraphAndDiagnosticsForTest(Of MethodBlockSyntax)(source, expectedGraph, expectedDiagnostics)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation, CompilerFeature.Dataflow)>
        <Fact()>
        Public Sub AggregateFlow_02()
            Dim source = <![CDATA[
Imports System
Imports System.Linq

Public Structure C

    Sub M(a As Integer(), b As Integer(), result As object) 'BIND:"Sub M"
        result = Aggregate y In If(a, b) Into Count(), Sum(y)
    End Sub
End Structure
]]>.Value

            Dim expectedDiagnostics = String.Empty

            Dim expectedGraph = <![CDATA[
Block[B0] - Entry
    Statements (0)
    Next (Regular) Block[B1]
Block[B1] - Block
    Predecessors: [B0]
    Statements (2)
        IFlowCaptureOperation: 0 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'result')
          Value: 
            IParameterReferenceOperation: result (OperationKind.ParameterReference, Type: System.Object) (Syntax: 'result')

        IFlowCaptureOperation: 1 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'a')
          Value: 
            IParameterReferenceOperation: a (OperationKind.ParameterReference, Type: System.Int32()) (Syntax: 'a')

    Jump if True (Regular) to Block[B3]
        IIsNullOperation (OperationKind.IsNull, Type: System.Boolean, IsImplicit) (Syntax: 'a')
          Operand: 
            IFlowCaptureReferenceOperation: 1 (OperationKind.FlowCaptureReference, Type: System.Int32(), IsImplicit) (Syntax: 'a')

    Next (Regular) Block[B2]
Block[B2] - Block
    Predecessors: [B1]
    Statements (1)
        IFlowCaptureOperation: 2 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'a')
          Value: 
            IFlowCaptureReferenceOperation: 1 (OperationKind.FlowCaptureReference, Type: System.Int32(), IsImplicit) (Syntax: 'a')

    Next (Regular) Block[B4]
Block[B3] - Block
    Predecessors: [B1]
    Statements (1)
        IFlowCaptureOperation: 2 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'b')
          Value: 
            IParameterReferenceOperation: b (OperationKind.ParameterReference, Type: System.Int32()) (Syntax: 'b')

    Next (Regular) Block[B4]
Block[B4] - Block
    Predecessors: [B2] [B3]
    Statements (3)
        IFlowCaptureOperation: 3 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'Count()')
          Value: 
            IInvocationOperation ( Function System.Collections.Generic.IEnumerable(Of System.Int32).Count() As System.Int32) (OperationKind.Invocation, Type: System.Int32, IsImplicit) (Syntax: 'Count()')
              Instance Receiver: 
                IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.Collections.Generic.IEnumerable(Of System.Int32), IsImplicit) (Syntax: 'Aggregate y ... t(), Sum(y)')
                  Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: True, IsUserDefined: False) (MethodSymbol: null)
                    (WideningReference)
                  Operand: 
                    IFlowCaptureReferenceOperation: 2 (OperationKind.FlowCaptureReference, Type: System.Int32(), IsImplicit) (Syntax: 'If(a, b)')
              Arguments(0)

        IFlowCaptureOperation: 4 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'Sum(y)')
          Value: 
            IInvocationOperation ( Function System.Collections.Generic.IEnumerable(Of System.Int32).Sum(selector As System.Func(Of System.Int32, System.Int32)) As System.Int32) (OperationKind.Invocation, Type: System.Int32, IsImplicit) (Syntax: 'Sum(y)')
              Instance Receiver: 
                IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.Collections.Generic.IEnumerable(Of System.Int32), IsImplicit) (Syntax: 'Aggregate y ... t(), Sum(y)')
                  Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: True, IsUserDefined: False) (MethodSymbol: null)
                    (WideningReference)
                  Operand: 
                    IFlowCaptureReferenceOperation: 2 (OperationKind.FlowCaptureReference, Type: System.Int32(), IsImplicit) (Syntax: 'If(a, b)')
              Arguments(1):
                  IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: selector) (OperationKind.Argument, Type: null, IsImplicit) (Syntax: 'y')
                    IDelegateCreationOperation (OperationKind.DelegateCreation, Type: System.Func(Of System.Int32, System.Int32), IsImplicit) (Syntax: 'y')
                      Target: 
                        IFlowAnonymousFunctionOperation (Symbol: Function (y As System.Int32) As System.Int32) (OperationKind.FlowAnonymousFunction, Type: null, IsImplicit) (Syntax: 'y')
                        {
                            Block[B0#A0] - Entry
                                Statements (0)
                                Next (Regular) Block[B1#A0]
                            Block[B1#A0] - Block
                                Predecessors: [B0#A0]
                                Statements (0)
                                Next (Return) Block[B2#A0]
                                    IParameterReferenceOperation: y (OperationKind.ParameterReference, Type: System.Int32) (Syntax: 'y')
                            Block[B2#A0] - Exit
                                Predecessors: [B1#A0]
                                Statements (0)
                        }
                    InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                    OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)

        IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'result = Ag ... t(), Sum(y)')
          Expression: 
            ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Object, IsImplicit) (Syntax: 'result = Ag ... t(), Sum(y)')
              Left: 
                IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: System.Object, IsImplicit) (Syntax: 'result')
              Right: 
                IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.Object, IsImplicit) (Syntax: 'Aggregate y ... t(), Sum(y)')
                  Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: True, IsUserDefined: False) (MethodSymbol: null)
                    (WideningReference)
                  Operand: 
                    ITranslatedQueryOperation (OperationKind.TranslatedQuery, Type: <anonymous type: Key Count As System.Int32, Key Sum As System.Int32>) (Syntax: 'Aggregate y ... t(), Sum(y)')
                      Expression: 
                        IAnonymousObjectCreationOperation (OperationKind.AnonymousObjectCreation, Type: <anonymous type: Key Count As System.Int32, Key Sum As System.Int32>, IsImplicit) (Syntax: 'Aggregate y ... t(), Sum(y)')
                          Initializers(2):
                              IFlowCaptureReferenceOperation: 3 (OperationKind.FlowCaptureReference, Type: System.Int32, IsImplicit) (Syntax: 'Count()')
                              IFlowCaptureReferenceOperation: 4 (OperationKind.FlowCaptureReference, Type: System.Int32, IsImplicit) (Syntax: 'Sum(y)')

    Next (Regular) Block[B5]
Block[B5] - Exit
    Predecessors: [B4]
    Statements (0)
]]>.Value

            VerifyFlowGraphAndDiagnosticsForTest(Of MethodBlockSyntax)(source, expectedGraph, expectedDiagnostics)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation, CompilerFeature.Dataflow)>
        <Fact()>
        Public Sub TranslatedQueryFlow_01()
            Dim source = <![CDATA[
Imports System
Imports System.Collections.Generic
Imports System.Linq

Public Class C
    Public Sub M1(o As Object, a As List(Of Integer)) 'BIND:"Public Sub M1(o As Object, a As List(Of Integer))"
        o = From x In a
            Select 0
    End Sub
End Class
]]>.Value

            Dim expectedDiagnostics = String.Empty

            Dim expectedGraph = <![CDATA[
Block[B0] - Entry
    Statements (0)
    Next (Regular) Block[B1]
Block[B1] - Block
    Predecessors: [B0]
    Statements (1)
        IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'o = From x  ... Select 0')
          Expression: 
            ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Object, IsImplicit) (Syntax: 'o = From x  ... Select 0')
              Left: 
                IParameterReferenceOperation: o (OperationKind.ParameterReference, Type: System.Object) (Syntax: 'o')
              Right: 
                IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.Object, IsImplicit) (Syntax: 'From x In a ... Select 0')
                  Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: True, IsUserDefined: False) (MethodSymbol: null)
                    (WideningReference)
                  Operand: 
                    ITranslatedQueryOperation (OperationKind.TranslatedQuery, Type: System.Collections.Generic.IEnumerable(Of System.Int32)) (Syntax: 'From x In a ... Select 0')
                      Expression: 
                        IInvocationOperation ( Function System.Collections.Generic.IEnumerable(Of System.Int32).Select(Of System.Int32)(selector As System.Func(Of System.Int32, System.Int32)) As System.Collections.Generic.IEnumerable(Of System.Int32)) (OperationKind.Invocation, Type: System.Collections.Generic.IEnumerable(Of System.Int32), IsImplicit) (Syntax: 'Select 0')
                          Instance Receiver: 
                            IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.Collections.Generic.IEnumerable(Of System.Int32), IsImplicit) (Syntax: 'x In a')
                              Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: True, IsUserDefined: False) (MethodSymbol: null)
                                (WideningReference)
                              Operand: 
                                IParameterReferenceOperation: a (OperationKind.ParameterReference, Type: System.Collections.Generic.List(Of System.Int32)) (Syntax: 'a')
                          Arguments(1):
                              IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: selector) (OperationKind.Argument, Type: null, IsImplicit) (Syntax: '0')
                                IDelegateCreationOperation (OperationKind.DelegateCreation, Type: System.Func(Of System.Int32, System.Int32), IsImplicit) (Syntax: '0')
                                  Target: 
                                    IFlowAnonymousFunctionOperation (Symbol: Function (x As System.Int32) As System.Int32) (OperationKind.FlowAnonymousFunction, Type: null, IsImplicit) (Syntax: '0')
                                    {
                                        Block[B0#A0] - Entry
                                            Statements (0)
                                            Next (Regular) Block[B1#A0]
                                        Block[B1#A0] - Block
                                            Predecessors: [B0#A0]
                                            Statements (0)
                                            Next (Return) Block[B2#A0]
                                                ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 0) (Syntax: '0')
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

            VerifyFlowGraphAndDiagnosticsForTest(Of MethodBlockSyntax)(source, expectedGraph, expectedDiagnostics)
        End Sub

        <CompilerTrait(CompilerFeature.IOperation, CompilerFeature.Dataflow)>
        <Fact()>
        Public Sub TranslatedQueryFlow_02()
            Dim source = <![CDATA[
Imports System
Imports System.Collections.Generic
Imports System.Linq

Public Class C
    Public Sub M1(o As Object, d1 As List(Of Integer), d2 As List(Of Integer))'BIND:"Public Sub M1(o As Object, d1 As List(Of Integer), d2 As List(Of Integer))"
        o = From x In If(d1,d2)
            Select 0
    End Sub
End Class
]]>.Value

            Dim expectedDiagnostics = String.Empty

            Dim expectedGraph = <![CDATA[
Block[B0] - Entry
    Statements (0)
    Next (Regular) Block[B1]
Block[B1] - Block
    Predecessors: [B0]
    Statements (2)
        IFlowCaptureOperation: 0 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'o')
          Value: 
            IParameterReferenceOperation: o (OperationKind.ParameterReference, Type: System.Object) (Syntax: 'o')

        IFlowCaptureOperation: 1 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'd1')
          Value: 
            IParameterReferenceOperation: d1 (OperationKind.ParameterReference, Type: System.Collections.Generic.List(Of System.Int32)) (Syntax: 'd1')

    Jump if True (Regular) to Block[B3]
        IIsNullOperation (OperationKind.IsNull, Type: System.Boolean, IsImplicit) (Syntax: 'd1')
          Operand: 
            IFlowCaptureReferenceOperation: 1 (OperationKind.FlowCaptureReference, Type: System.Collections.Generic.List(Of System.Int32), IsImplicit) (Syntax: 'd1')

    Next (Regular) Block[B2]
Block[B2] - Block
    Predecessors: [B1]
    Statements (1)
        IFlowCaptureOperation: 2 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'd1')
          Value: 
            IFlowCaptureReferenceOperation: 1 (OperationKind.FlowCaptureReference, Type: System.Collections.Generic.List(Of System.Int32), IsImplicit) (Syntax: 'd1')

    Next (Regular) Block[B4]
Block[B3] - Block
    Predecessors: [B1]
    Statements (1)
        IFlowCaptureOperation: 2 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'd2')
          Value: 
            IParameterReferenceOperation: d2 (OperationKind.ParameterReference, Type: System.Collections.Generic.List(Of System.Int32)) (Syntax: 'd2')

    Next (Regular) Block[B4]
Block[B4] - Block
    Predecessors: [B2] [B3]
    Statements (1)
        IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'o = From x  ... Select 0')
          Expression: 
            ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Object, IsImplicit) (Syntax: 'o = From x  ... Select 0')
              Left: 
                IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: System.Object, IsImplicit) (Syntax: 'o')
              Right: 
                IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.Object, IsImplicit) (Syntax: 'From x In I ... Select 0')
                  Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: True, IsUserDefined: False) (MethodSymbol: null)
                    (WideningReference)
                  Operand: 
                    ITranslatedQueryOperation (OperationKind.TranslatedQuery, Type: System.Collections.Generic.IEnumerable(Of System.Int32)) (Syntax: 'From x In I ... Select 0')
                      Expression: 
                        IInvocationOperation ( Function System.Collections.Generic.IEnumerable(Of System.Int32).Select(Of System.Int32)(selector As System.Func(Of System.Int32, System.Int32)) As System.Collections.Generic.IEnumerable(Of System.Int32)) (OperationKind.Invocation, Type: System.Collections.Generic.IEnumerable(Of System.Int32), IsImplicit) (Syntax: 'Select 0')
                          Instance Receiver: 
                            IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.Collections.Generic.IEnumerable(Of System.Int32), IsImplicit) (Syntax: 'x In If(d1,d2)')
                              Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: True, IsUserDefined: False) (MethodSymbol: null)
                                (WideningReference)
                              Operand: 
                                IFlowCaptureReferenceOperation: 2 (OperationKind.FlowCaptureReference, Type: System.Collections.Generic.List(Of System.Int32), IsImplicit) (Syntax: 'If(d1,d2)')
                          Arguments(1):
                              IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: selector) (OperationKind.Argument, Type: null, IsImplicit) (Syntax: '0')
                                IDelegateCreationOperation (OperationKind.DelegateCreation, Type: System.Func(Of System.Int32, System.Int32), IsImplicit) (Syntax: '0')
                                  Target: 
                                    IFlowAnonymousFunctionOperation (Symbol: Function (x As System.Int32) As System.Int32) (OperationKind.FlowAnonymousFunction, Type: null, IsImplicit) (Syntax: '0')
                                    {
                                        Block[B0#A0] - Entry
                                            Statements (0)
                                            Next (Regular) Block[B1#A0]
                                        Block[B1#A0] - Block
                                            Predecessors: [B0#A0]
                                            Statements (0)
                                            Next (Return) Block[B2#A0]
                                                ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 0) (Syntax: '0')
                                        Block[B2#A0] - Exit
                                            Predecessors: [B1#A0]
                                            Statements (0)
                                    }
                                InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                                OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)

    Next (Regular) Block[B5]
Block[B5] - Exit
    Predecessors: [B4]
    Statements (0)
]]>.Value

            VerifyFlowGraphAndDiagnosticsForTest(Of MethodBlockSyntax)(source, expectedGraph, expectedDiagnostics)
        End Sub
    End Class
End Namespace
