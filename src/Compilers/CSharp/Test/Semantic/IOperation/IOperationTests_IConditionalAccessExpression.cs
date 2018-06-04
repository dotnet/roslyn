// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.CSharp.Test.Utilities;
using Microsoft.CodeAnalysis.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.CSharp.UnitTests
{
    public partial class IOperationTests : SemanticModelTestBase
    {
        [CompilerTrait(CompilerFeature.IOperation)]
        [Fact]
        public void IConditionalAccessExpression_SimpleMethodAccess()
        {
            string source = @"
using System;

public class C1
{
    public void M()
    {
        var o = new object();
        /*<bind>*/o?.ToString()/*</bind>*/;
    }
}
";
            string expectedOperationTree = @"
IConditionalAccessOperation (OperationKind.ConditionalAccess, Type: System.String) (Syntax: 'o?.ToString()')
  Operation: 
    ILocalReferenceOperation: o (OperationKind.LocalReference, Type: System.Object) (Syntax: 'o')
  WhenNotNull: 
    IInvocationOperation (virtual System.String System.Object.ToString()) (OperationKind.Invocation, Type: System.String) (Syntax: '.ToString()')
      Instance Receiver: 
        IConditionalAccessInstanceOperation (OperationKind.ConditionalAccessInstance, Type: System.Object, IsImplicit) (Syntax: 'o')
      Arguments(0)
";
            var expectedDiagnostics = DiagnosticDescription.None;

            VerifyOperationTreeAndDiagnosticsForTest<ConditionalAccessExpressionSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation)]
        [Fact]
        public void IConditionalAccessExpression_SimplePropertyAccess()
        {
            string source = @"
using System;

public class C1
{
    int Prop1 { get; }
    public void M()
    {
        C1 c1 = null;
        var prop = /*<bind>*/c1?.Prop1/*</bind>*/;
    }
}
";
            string expectedOperationTree = @"
IConditionalAccessOperation (OperationKind.ConditionalAccess, Type: System.Int32?) (Syntax: 'c1?.Prop1')
  Operation: 
    ILocalReferenceOperation: c1 (OperationKind.LocalReference, Type: C1) (Syntax: 'c1')
  WhenNotNull: 
    IPropertyReferenceOperation: System.Int32 C1.Prop1 { get; } (OperationKind.PropertyReference, Type: System.Int32) (Syntax: '.Prop1')
      Instance Receiver: 
        IConditionalAccessInstanceOperation (OperationKind.ConditionalAccessInstance, Type: C1, IsImplicit) (Syntax: 'c1')
";
            var expectedDiagnostics = DiagnosticDescription.None;

            VerifyOperationTreeAndDiagnosticsForTest<ConditionalAccessExpressionSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation, CompilerFeature.Dataflow)]
        [Fact]
        public void ConditionalAccessFlow_01()
        {
            string source = @"
class P
{
    void M1(System.Array input, int? result)
/*<bind>*/{
        result = input?.Length;
    }/*</bind>*/
}
";
            string expectedGraph = @"
Block[B0] - Entry
    Statements (0)
    Next (Regular) Block[B1]
Block[B1] - Block
    Predecessors: [B0]
    Statements (2)
        IFlowCaptureOperation: 0 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'result')
          Value: 
            IParameterReferenceOperation: result (OperationKind.ParameterReference, Type: System.Int32?) (Syntax: 'result')

        IFlowCaptureOperation: 1 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'input')
          Value: 
            IParameterReferenceOperation: input (OperationKind.ParameterReference, Type: System.Array) (Syntax: 'input')

    Jump if True (Regular) to Block[B3]
        IIsNullOperation (OperationKind.IsNull, Type: System.Boolean, IsImplicit) (Syntax: 'input')
          Operand: 
            IFlowCaptureReferenceOperation: 1 (OperationKind.FlowCaptureReference, Type: System.Array, IsImplicit) (Syntax: 'input')

    Next (Regular) Block[B2]
Block[B2] - Block
    Predecessors: [B1]
    Statements (1)
        IFlowCaptureOperation: 2 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: '.Length')
          Value: 
            IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.Int32?, IsImplicit) (Syntax: '.Length')
              Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                (ImplicitNullable)
              Operand: 
                IPropertyReferenceOperation: System.Int32 System.Array.Length { get; } (OperationKind.PropertyReference, Type: System.Int32) (Syntax: '.Length')
                  Instance Receiver: 
                    IFlowCaptureReferenceOperation: 1 (OperationKind.FlowCaptureReference, Type: System.Array, IsImplicit) (Syntax: 'input')

    Next (Regular) Block[B4]
Block[B3] - Block
    Predecessors: [B1]
    Statements (1)
        IFlowCaptureOperation: 2 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'input')
          Value: 
            IDefaultValueOperation (OperationKind.DefaultValue, Type: System.Int32?, IsImplicit) (Syntax: 'input')

    Next (Regular) Block[B4]
Block[B4] - Block
    Predecessors: [B2] [B3]
    Statements (1)
        IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'result = input?.Length;')
          Expression: 
            ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Int32?) (Syntax: 'result = input?.Length')
              Left: 
                IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: System.Int32?, IsImplicit) (Syntax: 'result')
              Right: 
                IFlowCaptureReferenceOperation: 2 (OperationKind.FlowCaptureReference, Type: System.Int32?, IsImplicit) (Syntax: 'input?.Length')

    Next (Regular) Block[B5]
Block[B5] - Exit
    Predecessors: [B4]
    Statements (0)
";
            var expectedDiagnostics = DiagnosticDescription.None;

            VerifyFlowGraphAndDiagnosticsForTest<BlockSyntax>(source, expectedGraph, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation, CompilerFeature.Dataflow)]
        [Fact]
        public void ConditionalAccessFlow_02()
        {
            string source = @"
class P
{
    void M1(int? input, string result)
/*<bind>*/{
        result = input?.ToString();
    }/*</bind>*/
}
";
            string expectedGraph = @"
Block[B0] - Entry
    Statements (0)
    Next (Regular) Block[B1]
Block[B1] - Block
    Predecessors: [B0]
    Statements (2)
        IFlowCaptureOperation: 0 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'result')
          Value: 
            IParameterReferenceOperation: result (OperationKind.ParameterReference, Type: System.String) (Syntax: 'result')

        IFlowCaptureOperation: 1 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'input')
          Value: 
            IParameterReferenceOperation: input (OperationKind.ParameterReference, Type: System.Int32?) (Syntax: 'input')

    Jump if True (Regular) to Block[B3]
        IIsNullOperation (OperationKind.IsNull, Type: System.Boolean, IsImplicit) (Syntax: 'input')
          Operand: 
            IFlowCaptureReferenceOperation: 1 (OperationKind.FlowCaptureReference, Type: System.Int32?, IsImplicit) (Syntax: 'input')

    Next (Regular) Block[B2]
Block[B2] - Block
    Predecessors: [B1]
    Statements (1)
        IFlowCaptureOperation: 2 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: '.ToString()')
          Value: 
            IInvocationOperation (virtual System.String System.Int32.ToString()) (OperationKind.Invocation, Type: System.String) (Syntax: '.ToString()')
              Instance Receiver: 
                IInvocationOperation ( System.Int32 System.Int32?.GetValueOrDefault()) (OperationKind.Invocation, Type: System.Int32, IsImplicit) (Syntax: 'input')
                  Instance Receiver: 
                    IFlowCaptureReferenceOperation: 1 (OperationKind.FlowCaptureReference, Type: System.Int32?, IsImplicit) (Syntax: 'input')
                  Arguments(0)
              Arguments(0)

    Next (Regular) Block[B4]
Block[B3] - Block
    Predecessors: [B1]
    Statements (1)
        IFlowCaptureOperation: 2 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'input')
          Value: 
            IDefaultValueOperation (OperationKind.DefaultValue, Type: System.String, Constant: null, IsImplicit) (Syntax: 'input')

    Next (Regular) Block[B4]
Block[B4] - Block
    Predecessors: [B2] [B3]
    Statements (1)
        IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'result = in ... ToString();')
          Expression: 
            ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.String) (Syntax: 'result = in ... .ToString()')
              Left: 
                IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: System.String, IsImplicit) (Syntax: 'result')
              Right: 
                IFlowCaptureReferenceOperation: 2 (OperationKind.FlowCaptureReference, Type: System.String, IsImplicit) (Syntax: 'input?.ToString()')

    Next (Regular) Block[B5]
Block[B5] - Exit
    Predecessors: [B4]
    Statements (0)
";
            var expectedDiagnostics = DiagnosticDescription.None;

            VerifyFlowGraphAndDiagnosticsForTest<BlockSyntax>(source, expectedGraph, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation, CompilerFeature.Dataflow)]
        [Fact]
        public void ConditionalAccessFlow_03()
        {
            string source = @"
class P
{
    void M1(P input, int? result)
/*<bind>*/{
        result = input?.Access();
    }/*</bind>*/

    int? Access() => null;
}
";
            string expectedGraph = @"
Block[B0] - Entry
    Statements (0)
    Next (Regular) Block[B1]
Block[B1] - Block
    Predecessors: [B0]
    Statements (2)
        IFlowCaptureOperation: 0 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'result')
          Value: 
            IParameterReferenceOperation: result (OperationKind.ParameterReference, Type: System.Int32?) (Syntax: 'result')

        IFlowCaptureOperation: 1 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'input')
          Value: 
            IParameterReferenceOperation: input (OperationKind.ParameterReference, Type: P) (Syntax: 'input')

    Jump if True (Regular) to Block[B3]
        IIsNullOperation (OperationKind.IsNull, Type: System.Boolean, IsImplicit) (Syntax: 'input')
          Operand: 
            IFlowCaptureReferenceOperation: 1 (OperationKind.FlowCaptureReference, Type: P, IsImplicit) (Syntax: 'input')

    Next (Regular) Block[B2]
Block[B2] - Block
    Predecessors: [B1]
    Statements (1)
        IFlowCaptureOperation: 2 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: '.Access()')
          Value: 
            IInvocationOperation ( System.Int32? P.Access()) (OperationKind.Invocation, Type: System.Int32?) (Syntax: '.Access()')
              Instance Receiver: 
                IFlowCaptureReferenceOperation: 1 (OperationKind.FlowCaptureReference, Type: P, IsImplicit) (Syntax: 'input')
              Arguments(0)

    Next (Regular) Block[B4]
Block[B3] - Block
    Predecessors: [B1]
    Statements (1)
        IFlowCaptureOperation: 2 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'input')
          Value: 
            IDefaultValueOperation (OperationKind.DefaultValue, Type: System.Int32?, IsImplicit) (Syntax: 'input')

    Next (Regular) Block[B4]
Block[B4] - Block
    Predecessors: [B2] [B3]
    Statements (1)
        IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'result = in ... ?.Access();')
          Expression: 
            ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Int32?) (Syntax: 'result = input?.Access()')
              Left: 
                IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: System.Int32?, IsImplicit) (Syntax: 'result')
              Right: 
                IFlowCaptureReferenceOperation: 2 (OperationKind.FlowCaptureReference, Type: System.Int32?, IsImplicit) (Syntax: 'input?.Access()')

    Next (Regular) Block[B5]
Block[B5] - Exit
    Predecessors: [B4]
    Statements (0)
";
            var expectedDiagnostics = DiagnosticDescription.None;

            VerifyFlowGraphAndDiagnosticsForTest<BlockSyntax>(source, expectedGraph, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation, CompilerFeature.Dataflow)]
        [Fact]
        public void ConditionalAccessFlow_04()
        {
            string source = @"
class P
{
    void M1(P input, P result)
/*<bind>*/{
        result = (input?[11]?.Access1())?[22]?.Access2();
    }/*</bind>*/

    P this[int x] => null;
    P[] Access1() => null;
    P Access2() => null;
}
";
            string expectedGraph = @"
Block[B0] - Entry
    Statements (0)
    Next (Regular) Block[B1]
Block[B1] - Block
    Predecessors: [B0]
    Statements (2)
        IFlowCaptureOperation: 0 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'result')
          Value: 
            IParameterReferenceOperation: result (OperationKind.ParameterReference, Type: P) (Syntax: 'result')

        IFlowCaptureOperation: 1 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'input')
          Value: 
            IParameterReferenceOperation: input (OperationKind.ParameterReference, Type: P) (Syntax: 'input')

    Jump if True (Regular) to Block[B4]
        IIsNullOperation (OperationKind.IsNull, Type: System.Boolean, IsImplicit) (Syntax: 'input')
          Operand: 
            IFlowCaptureReferenceOperation: 1 (OperationKind.FlowCaptureReference, Type: P, IsImplicit) (Syntax: 'input')

    Next (Regular) Block[B2]
Block[B2] - Block
    Predecessors: [B1]
    Statements (1)
        IFlowCaptureOperation: 2 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: '[11]')
          Value: 
            IPropertyReferenceOperation: P P.this[System.Int32 x] { get; } (OperationKind.PropertyReference, Type: P) (Syntax: '[11]')
              Instance Receiver: 
                IFlowCaptureReferenceOperation: 1 (OperationKind.FlowCaptureReference, Type: P, IsImplicit) (Syntax: 'input')
              Arguments(1):
                  IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: x) (OperationKind.Argument, Type: null) (Syntax: '11')
                    ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 11) (Syntax: '11')
                    InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                    OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)

    Jump if True (Regular) to Block[B4]
        IIsNullOperation (OperationKind.IsNull, Type: System.Boolean, IsImplicit) (Syntax: '[11]')
          Operand: 
            IFlowCaptureReferenceOperation: 2 (OperationKind.FlowCaptureReference, Type: P, IsImplicit) (Syntax: '[11]')

    Next (Regular) Block[B3]
Block[B3] - Block
    Predecessors: [B2]
    Statements (1)
        IFlowCaptureOperation: 3 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: '.Access1()')
          Value: 
            IInvocationOperation ( P[] P.Access1()) (OperationKind.Invocation, Type: P[]) (Syntax: '.Access1()')
              Instance Receiver: 
                IFlowCaptureReferenceOperation: 2 (OperationKind.FlowCaptureReference, Type: P, IsImplicit) (Syntax: '[11]')
              Arguments(0)

    Next (Regular) Block[B5]
Block[B4] - Block
    Predecessors: [B1] [B2]
    Statements (1)
        IFlowCaptureOperation: 3 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'input?[11]?.Access1()')
          Value: 
            IDefaultValueOperation (OperationKind.DefaultValue, Type: P[], Constant: null, IsImplicit) (Syntax: 'input?[11]?.Access1()')

    Next (Regular) Block[B5]
Block[B5] - Block
    Predecessors: [B3] [B4]
    Statements (0)
    Jump if True (Regular) to Block[B8]
        IIsNullOperation (OperationKind.IsNull, Type: System.Boolean, IsImplicit) (Syntax: 'input?[11]?.Access1()')
          Operand: 
            IFlowCaptureReferenceOperation: 3 (OperationKind.FlowCaptureReference, Type: P[], IsImplicit) (Syntax: 'input?[11]?.Access1()')

    Next (Regular) Block[B6]
Block[B6] - Block
    Predecessors: [B5]
    Statements (1)
        IFlowCaptureOperation: 4 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: '[22]')
          Value: 
            IArrayElementReferenceOperation (OperationKind.ArrayElementReference, Type: P) (Syntax: '[22]')
              Array reference: 
                IFlowCaptureReferenceOperation: 3 (OperationKind.FlowCaptureReference, Type: P[], IsImplicit) (Syntax: 'input?[11]?.Access1()')
              Indices(1):
                  ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 22) (Syntax: '22')

    Jump if True (Regular) to Block[B8]
        IIsNullOperation (OperationKind.IsNull, Type: System.Boolean, IsImplicit) (Syntax: '[22]')
          Operand: 
            IFlowCaptureReferenceOperation: 4 (OperationKind.FlowCaptureReference, Type: P, IsImplicit) (Syntax: '[22]')

    Next (Regular) Block[B7]
Block[B7] - Block
    Predecessors: [B6]
    Statements (1)
        IFlowCaptureOperation: 5 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: '.Access2()')
          Value: 
            IInvocationOperation ( P P.Access2()) (OperationKind.Invocation, Type: P) (Syntax: '.Access2()')
              Instance Receiver: 
                IFlowCaptureReferenceOperation: 4 (OperationKind.FlowCaptureReference, Type: P, IsImplicit) (Syntax: '[22]')
              Arguments(0)

    Next (Regular) Block[B9]
Block[B8] - Block
    Predecessors: [B5] [B6]
    Statements (1)
        IFlowCaptureOperation: 5 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: '(input?[11] ... ?.Access2()')
          Value: 
            IDefaultValueOperation (OperationKind.DefaultValue, Type: P, Constant: null, IsImplicit) (Syntax: '(input?[11] ... ?.Access2()')

    Next (Regular) Block[B9]
Block[B9] - Block
    Predecessors: [B7] [B8]
    Statements (1)
        IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'result = (i ... .Access2();')
          Expression: 
            ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: P) (Syntax: 'result = (i ... ?.Access2()')
              Left: 
                IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: P, IsImplicit) (Syntax: 'result')
              Right: 
                IFlowCaptureReferenceOperation: 5 (OperationKind.FlowCaptureReference, Type: P, IsImplicit) (Syntax: '(input?[11] ... ?.Access2()')

    Next (Regular) Block[B10]
Block[B10] - Exit
    Predecessors: [B9]
    Statements (0)
";
            var expectedDiagnostics = DiagnosticDescription.None;

            VerifyFlowGraphAndDiagnosticsForTest<BlockSyntax>(source, expectedGraph, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation, CompilerFeature.Dataflow)]
        [Fact]
        public void ConditionalAccessFlow_05()
        {
            string source = @"
struct P
{
    void M1(P? input, P? result)
/*<bind>*/{
        result = (input?.Access1()?[11])?[22]?.Access2();
    }/*</bind>*/

    P? this[int x] => default;
    P[] Access1() => default;
    P Access2() => default;
}
";
            string expectedGraph = @"
Block[B0] - Entry
    Statements (0)
    Next (Regular) Block[B1]
Block[B1] - Block
    Predecessors: [B0]
    Statements (2)
        IFlowCaptureOperation: 0 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'result')
          Value: 
            IParameterReferenceOperation: result (OperationKind.ParameterReference, Type: P?) (Syntax: 'result')

        IFlowCaptureOperation: 1 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'input')
          Value: 
            IParameterReferenceOperation: input (OperationKind.ParameterReference, Type: P?) (Syntax: 'input')

    Jump if True (Regular) to Block[B4]
        IIsNullOperation (OperationKind.IsNull, Type: System.Boolean, IsImplicit) (Syntax: 'input')
          Operand: 
            IFlowCaptureReferenceOperation: 1 (OperationKind.FlowCaptureReference, Type: P?, IsImplicit) (Syntax: 'input')

    Next (Regular) Block[B2]
Block[B2] - Block
    Predecessors: [B1]
    Statements (1)
        IFlowCaptureOperation: 2 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: '.Access1()')
          Value: 
            IInvocationOperation ( P[] P.Access1()) (OperationKind.Invocation, Type: P[]) (Syntax: '.Access1()')
              Instance Receiver: 
                IInvocationOperation ( P P?.GetValueOrDefault()) (OperationKind.Invocation, Type: P, IsImplicit) (Syntax: 'input')
                  Instance Receiver: 
                    IFlowCaptureReferenceOperation: 1 (OperationKind.FlowCaptureReference, Type: P?, IsImplicit) (Syntax: 'input')
                  Arguments(0)
              Arguments(0)

    Jump if True (Regular) to Block[B4]
        IIsNullOperation (OperationKind.IsNull, Type: System.Boolean, IsImplicit) (Syntax: '.Access1()')
          Operand: 
            IFlowCaptureReferenceOperation: 2 (OperationKind.FlowCaptureReference, Type: P[], IsImplicit) (Syntax: '.Access1()')

    Next (Regular) Block[B3]
Block[B3] - Block
    Predecessors: [B2]
    Statements (1)
        IFlowCaptureOperation: 3 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: '[11]')
          Value: 
            IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: P?, IsImplicit) (Syntax: '[11]')
              Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                (ImplicitNullable)
              Operand: 
                IArrayElementReferenceOperation (OperationKind.ArrayElementReference, Type: P) (Syntax: '[11]')
                  Array reference: 
                    IFlowCaptureReferenceOperation: 2 (OperationKind.FlowCaptureReference, Type: P[], IsImplicit) (Syntax: '.Access1()')
                  Indices(1):
                      ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 11) (Syntax: '11')

    Next (Regular) Block[B5]
Block[B4] - Block
    Predecessors: [B1] [B2]
    Statements (1)
        IFlowCaptureOperation: 3 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'input?.Access1()?[11]')
          Value: 
            IDefaultValueOperation (OperationKind.DefaultValue, Type: P?, IsImplicit) (Syntax: 'input?.Access1()?[11]')

    Next (Regular) Block[B5]
Block[B5] - Block
    Predecessors: [B3] [B4]
    Statements (0)
    Jump if True (Regular) to Block[B8]
        IIsNullOperation (OperationKind.IsNull, Type: System.Boolean, IsImplicit) (Syntax: 'input?.Access1()?[11]')
          Operand: 
            IFlowCaptureReferenceOperation: 3 (OperationKind.FlowCaptureReference, Type: P?, IsImplicit) (Syntax: 'input?.Access1()?[11]')

    Next (Regular) Block[B6]
Block[B6] - Block
    Predecessors: [B5]
    Statements (1)
        IFlowCaptureOperation: 4 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: '[22]')
          Value: 
            IPropertyReferenceOperation: P? P.this[System.Int32 x] { get; } (OperationKind.PropertyReference, Type: P?) (Syntax: '[22]')
              Instance Receiver: 
                IInvocationOperation ( P P?.GetValueOrDefault()) (OperationKind.Invocation, Type: P, IsImplicit) (Syntax: 'input?.Access1()?[11]')
                  Instance Receiver: 
                    IFlowCaptureReferenceOperation: 3 (OperationKind.FlowCaptureReference, Type: P?, IsImplicit) (Syntax: 'input?.Access1()?[11]')
                  Arguments(0)
              Arguments(1):
                  IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: x) (OperationKind.Argument, Type: null) (Syntax: '22')
                    ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 22) (Syntax: '22')
                    InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                    OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)

    Jump if True (Regular) to Block[B8]
        IIsNullOperation (OperationKind.IsNull, Type: System.Boolean, IsImplicit) (Syntax: '[22]')
          Operand: 
            IFlowCaptureReferenceOperation: 4 (OperationKind.FlowCaptureReference, Type: P?, IsImplicit) (Syntax: '[22]')

    Next (Regular) Block[B7]
Block[B7] - Block
    Predecessors: [B6]
    Statements (1)
        IFlowCaptureOperation: 5 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: '.Access2()')
          Value: 
            IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: P?, IsImplicit) (Syntax: '.Access2()')
              Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                (ImplicitNullable)
              Operand: 
                IInvocationOperation ( P P.Access2()) (OperationKind.Invocation, Type: P) (Syntax: '.Access2()')
                  Instance Receiver: 
                    IInvocationOperation ( P P?.GetValueOrDefault()) (OperationKind.Invocation, Type: P, IsImplicit) (Syntax: '[22]')
                      Instance Receiver: 
                        IFlowCaptureReferenceOperation: 4 (OperationKind.FlowCaptureReference, Type: P?, IsImplicit) (Syntax: '[22]')
                      Arguments(0)
                  Arguments(0)

    Next (Regular) Block[B9]
Block[B8] - Block
    Predecessors: [B5] [B6]
    Statements (1)
        IFlowCaptureOperation: 5 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: '(input?.Acc ... ?.Access2()')
          Value: 
            IDefaultValueOperation (OperationKind.DefaultValue, Type: P?, IsImplicit) (Syntax: '(input?.Acc ... ?.Access2()')

    Next (Regular) Block[B9]
Block[B9] - Block
    Predecessors: [B7] [B8]
    Statements (1)
        IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'result = (i ... .Access2();')
          Expression: 
            ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: P?) (Syntax: 'result = (i ... ?.Access2()')
              Left: 
                IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: P?, IsImplicit) (Syntax: 'result')
              Right: 
                IFlowCaptureReferenceOperation: 5 (OperationKind.FlowCaptureReference, Type: P?, IsImplicit) (Syntax: '(input?.Acc ... ?.Access2()')

    Next (Regular) Block[B10]
Block[B10] - Exit
    Predecessors: [B9]
    Statements (0)
";
            var expectedDiagnostics = DiagnosticDescription.None;

            VerifyFlowGraphAndDiagnosticsForTest<BlockSyntax>(source, expectedGraph, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation, CompilerFeature.Dataflow)]
        [Fact]
        public void ConditionalAccessFlow_06()
        {
            string source = @"
struct P
{
    void M1(S1? x)
/*<bind>*/{
        x?.P1 = 0;
    }/*</bind>*/
}

public struct S1
{
    public int P1 { get; set; }
}";
            string expectedGraph = @"
Block[B0] - Entry
    Statements (0)
    Next (Regular) Block[B1]
Block[B1] - Block
    Predecessors: [B0]
    Statements (1)
        IFlowCaptureOperation: 0 (OperationKind.FlowCapture, Type: null, IsInvalid, IsImplicit) (Syntax: 'x')
          Value: 
            IParameterReferenceOperation: x (OperationKind.ParameterReference, Type: S1?, IsInvalid) (Syntax: 'x')

    Jump if True (Regular) to Block[B3]
        IIsNullOperation (OperationKind.IsNull, Type: System.Boolean, IsInvalid, IsImplicit) (Syntax: 'x')
          Operand: 
            IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: S1?, IsInvalid, IsImplicit) (Syntax: 'x')

    Next (Regular) Block[B2]
Block[B2] - Block
    Predecessors: [B1]
    Statements (1)
        IFlowCaptureOperation: 1 (OperationKind.FlowCapture, Type: null, IsInvalid, IsImplicit) (Syntax: '.P1')
          Value: 
            IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.Int32?, IsInvalid, IsImplicit) (Syntax: '.P1')
              Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                (ImplicitNullable)
              Operand: 
                IPropertyReferenceOperation: System.Int32 S1.P1 { get; set; } (OperationKind.PropertyReference, Type: System.Int32, IsInvalid) (Syntax: '.P1')
                  Instance Receiver: 
                    IInvocationOperation ( S1 S1?.GetValueOrDefault()) (OperationKind.Invocation, Type: S1, IsInvalid, IsImplicit) (Syntax: 'x')
                      Instance Receiver: 
                        IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: S1?, IsInvalid, IsImplicit) (Syntax: 'x')
                      Arguments(0)

    Next (Regular) Block[B4]
Block[B3] - Block
    Predecessors: [B1]
    Statements (1)
        IFlowCaptureOperation: 1 (OperationKind.FlowCapture, Type: null, IsInvalid, IsImplicit) (Syntax: 'x')
          Value: 
            IDefaultValueOperation (OperationKind.DefaultValue, Type: System.Int32?, IsInvalid, IsImplicit) (Syntax: 'x')

    Next (Regular) Block[B4]
Block[B4] - Block
    Predecessors: [B2] [B3]
    Statements (1)
        IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null, IsInvalid) (Syntax: 'x?.P1 = 0;')
          Expression: 
            ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Int32?, IsInvalid) (Syntax: 'x?.P1 = 0')
              Left: 
                IInvalidOperation (OperationKind.Invalid, Type: System.Int32?, IsInvalid, IsImplicit) (Syntax: 'x?.P1')
                  Children(1):
                      IFlowCaptureReferenceOperation: 1 (OperationKind.FlowCaptureReference, Type: System.Int32?, IsInvalid, IsImplicit) (Syntax: 'x?.P1')
              Right: 
                ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 0) (Syntax: '0')

    Next (Regular) Block[B5]
Block[B5] - Exit
    Predecessors: [B4]
    Statements (0)
";
            var expectedDiagnostics = new[] {
                // file.cs(6,9): error CS0131: The left-hand side of an assignment must be a variable, property or indexer
                //         x?.P1 = 0;
                Diagnostic(ErrorCode.ERR_AssgLvalueExpected, "x?.P1").WithLocation(6, 9)
            };

            VerifyFlowGraphAndDiagnosticsForTest<BlockSyntax>(source, expectedGraph, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation, CompilerFeature.Dataflow)]
        [Fact]
        public void ConditionalAccessFlow_07()
        {
            string source = @"
struct P
{
    void M1(S1? x)
/*<bind>*/{
        x?.P1 = 0;
    }/*</bind>*/
}

public struct S1
{
    public int P1;
}";
            string expectedGraph = @"
Block[B0] - Entry
    Statements (0)
    Next (Regular) Block[B1]
Block[B1] - Block
    Predecessors: [B0]
    Statements (1)
        IFlowCaptureOperation: 0 (OperationKind.FlowCapture, Type: null, IsInvalid, IsImplicit) (Syntax: 'x')
          Value: 
            IParameterReferenceOperation: x (OperationKind.ParameterReference, Type: S1?, IsInvalid) (Syntax: 'x')

    Jump if True (Regular) to Block[B3]
        IIsNullOperation (OperationKind.IsNull, Type: System.Boolean, IsInvalid, IsImplicit) (Syntax: 'x')
          Operand: 
            IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: S1?, IsInvalid, IsImplicit) (Syntax: 'x')

    Next (Regular) Block[B2]
Block[B2] - Block
    Predecessors: [B1]
    Statements (1)
        IFlowCaptureOperation: 1 (OperationKind.FlowCapture, Type: null, IsInvalid, IsImplicit) (Syntax: '.P1')
          Value: 
            IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.Int32?, IsInvalid, IsImplicit) (Syntax: '.P1')
              Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                (ImplicitNullable)
              Operand: 
                IFieldReferenceOperation: System.Int32 S1.P1 (OperationKind.FieldReference, Type: System.Int32, IsInvalid) (Syntax: '.P1')
                  Instance Receiver: 
                    IInvocationOperation ( S1 S1?.GetValueOrDefault()) (OperationKind.Invocation, Type: S1, IsInvalid, IsImplicit) (Syntax: 'x')
                      Instance Receiver: 
                        IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: S1?, IsInvalid, IsImplicit) (Syntax: 'x')
                      Arguments(0)

    Next (Regular) Block[B4]
Block[B3] - Block
    Predecessors: [B1]
    Statements (1)
        IFlowCaptureOperation: 1 (OperationKind.FlowCapture, Type: null, IsInvalid, IsImplicit) (Syntax: 'x')
          Value: 
            IDefaultValueOperation (OperationKind.DefaultValue, Type: System.Int32?, IsInvalid, IsImplicit) (Syntax: 'x')

    Next (Regular) Block[B4]
Block[B4] - Block
    Predecessors: [B2] [B3]
    Statements (1)
        IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null, IsInvalid) (Syntax: 'x?.P1 = 0;')
          Expression: 
            ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Int32?, IsInvalid) (Syntax: 'x?.P1 = 0')
              Left: 
                IInvalidOperation (OperationKind.Invalid, Type: System.Int32?, IsInvalid, IsImplicit) (Syntax: 'x?.P1')
                  Children(1):
                      IFlowCaptureReferenceOperation: 1 (OperationKind.FlowCaptureReference, Type: System.Int32?, IsInvalid, IsImplicit) (Syntax: 'x?.P1')
              Right: 
                ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 0) (Syntax: '0')

    Next (Regular) Block[B5]
Block[B5] - Exit
    Predecessors: [B4]
    Statements (0)
";
            var expectedDiagnostics = new[] {
                // file.cs(6,9): error CS0131: The left-hand side of an assignment must be a variable, property or indexer
                //         x?.P1 = 0;
                Diagnostic(ErrorCode.ERR_AssgLvalueExpected, "x?.P1").WithLocation(6, 9)
            };

            VerifyFlowGraphAndDiagnosticsForTest<BlockSyntax>(source, expectedGraph, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation, CompilerFeature.Dataflow)]
        [Fact]
        public void ConditionalAccessFlow_08()
        {
            string source = @"
struct P
{
    void M1(P? input, int? result)
/*<bind>*/{
        result = input?.Length;
    }/*</bind>*/

    public int Length { get; }
}
";
            var compilation = CreateCompilationWithMscorlib45(source, parseOptions: TestOptions.RegularWithFlowAnalysisFeature);
            compilation.MakeMemberMissing(SpecialMember.System_Nullable_T_GetValueOrDefault);

            string expectedGraph = @"
Block[B0] - Entry
    Statements (0)
    Next (Regular) Block[B1]
Block[B1] - Block
    Predecessors: [B0]
    Statements (2)
        IFlowCaptureOperation: 0 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'result')
          Value: 
            IParameterReferenceOperation: result (OperationKind.ParameterReference, Type: System.Int32?) (Syntax: 'result')

        IFlowCaptureOperation: 1 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'input')
          Value: 
            IParameterReferenceOperation: input (OperationKind.ParameterReference, Type: P?) (Syntax: 'input')

    Jump if True (Regular) to Block[B3]
        IIsNullOperation (OperationKind.IsNull, Type: System.Boolean, IsImplicit) (Syntax: 'input')
          Operand: 
            IFlowCaptureReferenceOperation: 1 (OperationKind.FlowCaptureReference, Type: P?, IsImplicit) (Syntax: 'input')

    Next (Regular) Block[B2]
Block[B2] - Block
    Predecessors: [B1]
    Statements (1)
        IFlowCaptureOperation: 2 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: '.Length')
          Value: 
            IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.Int32?, IsImplicit) (Syntax: '.Length')
              Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                (ImplicitNullable)
              Operand: 
                IPropertyReferenceOperation: System.Int32 P.Length { get; } (OperationKind.PropertyReference, Type: System.Int32) (Syntax: '.Length')
                  Instance Receiver: 
                    IInvalidOperation (OperationKind.Invalid, Type: P, IsImplicit) (Syntax: 'input')
                      Children(1):
                          IFlowCaptureReferenceOperation: 1 (OperationKind.FlowCaptureReference, Type: P?, IsImplicit) (Syntax: 'input')

    Next (Regular) Block[B4]
Block[B3] - Block
    Predecessors: [B1]
    Statements (1)
        IFlowCaptureOperation: 2 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'input')
          Value: 
            IDefaultValueOperation (OperationKind.DefaultValue, Type: System.Int32?, IsImplicit) (Syntax: 'input')

    Next (Regular) Block[B4]
Block[B4] - Block
    Predecessors: [B2] [B3]
    Statements (1)
        IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'result = input?.Length;')
          Expression: 
            ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Int32?) (Syntax: 'result = input?.Length')
              Left: 
                IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: System.Int32?, IsImplicit) (Syntax: 'result')
              Right: 
                IFlowCaptureReferenceOperation: 2 (OperationKind.FlowCaptureReference, Type: System.Int32?, IsImplicit) (Syntax: 'input?.Length')

    Next (Regular) Block[B5]
Block[B5] - Exit
    Predecessors: [B4]
    Statements (0)
";
            var expectedDiagnostics = DiagnosticDescription.None;

            VerifyFlowGraphAndDiagnosticsForTest<BlockSyntax>(compilation, expectedGraph, expectedDiagnostics);
        }
    }
}
