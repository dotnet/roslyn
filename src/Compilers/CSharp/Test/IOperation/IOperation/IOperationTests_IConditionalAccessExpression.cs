// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.CSharp.UnitTests
{
    public class IOperationTests_IConditionalAccessExpression : SemanticModelTestBase
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

        [CompilerTrait(CompilerFeature.IOperation)]
        [Fact]
        public void IConditionalAccessExpression_NestedConditionalAndNonConditionalAccesses()
        {
            string source = @"
using System;

public class C1
{
    C1 Prop1 { get; }
    C1 M1(C1 arg) => null;
    public void M(C1 c1, C1 c2, C1 result)
    /*<bind>*/{
        result = c1?.Prop1.M1(c2?.Prop1)?.Prop1;
    }/*</bind>*/
}
";
            string expectedOperationTree = @"
IBlockOperation (1 statements) (OperationKind.Block, Type: null) (Syntax: '{ ... }')
  IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'result = c1 ... p1)?.Prop1;')
    Expression: 
      ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: C1) (Syntax: 'result = c1 ... op1)?.Prop1')
        Left: 
          IParameterReferenceOperation: result (OperationKind.ParameterReference, Type: C1) (Syntax: 'result')
        Right: 
          IConditionalAccessOperation (OperationKind.ConditionalAccess, Type: C1) (Syntax: 'c1?.Prop1.M ... op1)?.Prop1')
            Operation: 
              IParameterReferenceOperation: c1 (OperationKind.ParameterReference, Type: C1) (Syntax: 'c1')
            WhenNotNull: 
              IConditionalAccessOperation (OperationKind.ConditionalAccess, Type: C1) (Syntax: '.Prop1.M1(c ... op1)?.Prop1')
                Operation: 
                  IInvocationOperation ( C1 C1.M1(C1 arg)) (OperationKind.Invocation, Type: C1) (Syntax: '.Prop1.M1(c2?.Prop1)')
                    Instance Receiver: 
                      IPropertyReferenceOperation: C1 C1.Prop1 { get; } (OperationKind.PropertyReference, Type: C1) (Syntax: '.Prop1')
                        Instance Receiver: 
                          IConditionalAccessInstanceOperation (OperationKind.ConditionalAccessInstance, Type: C1, IsImplicit) (Syntax: 'c1')
                    Arguments(1):
                        IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: arg) (OperationKind.Argument, Type: null) (Syntax: 'c2?.Prop1')
                          IConditionalAccessOperation (OperationKind.ConditionalAccess, Type: C1) (Syntax: 'c2?.Prop1')
                            Operation: 
                              IParameterReferenceOperation: c2 (OperationKind.ParameterReference, Type: C1) (Syntax: 'c2')
                            WhenNotNull: 
                              IPropertyReferenceOperation: C1 C1.Prop1 { get; } (OperationKind.PropertyReference, Type: C1) (Syntax: '.Prop1')
                                Instance Receiver: 
                                  IConditionalAccessInstanceOperation (OperationKind.ConditionalAccessInstance, Type: C1, IsImplicit) (Syntax: 'c2')
                          InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                          OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                WhenNotNull: 
                  IPropertyReferenceOperation: C1 C1.Prop1 { get; } (OperationKind.PropertyReference, Type: C1) (Syntax: '.Prop1')
                    Instance Receiver: 
                      IConditionalAccessInstanceOperation (OperationKind.ConditionalAccessInstance, Type: C1, IsImplicit) (Syntax: '.Prop1.M1(c2?.Prop1)')
";
            var expectedDiagnostics = DiagnosticDescription.None;

            var comp = CreateCompilation(source);
            VerifyOperationTreeAndDiagnosticsForTest<BlockSyntax>(comp, expectedOperationTree, expectedDiagnostics);

            var expectedFlowGraph = @"
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
                IParameterReferenceOperation: result (OperationKind.ParameterReference, Type: C1) (Syntax: 'result')
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
                            IParameterReferenceOperation: c1 (OperationKind.ParameterReference, Type: C1) (Syntax: 'c1')
                    Jump if True (Regular) to Block[B10]
                        IIsNullOperation (OperationKind.IsNull, Type: System.Boolean, IsImplicit) (Syntax: 'c1')
                          Operand: 
                            IFlowCaptureReferenceOperation: 2 (OperationKind.FlowCaptureReference, Type: C1, IsImplicit) (Syntax: 'c1')
                        Leaving: {R4} {R3} {R2}
                    Next (Regular) Block[B3]
                Block[B3] - Block
                    Predecessors: [B2]
                    Statements (1)
                        IFlowCaptureOperation: 3 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: '.Prop1')
                          Value: 
                            IPropertyReferenceOperation: C1 C1.Prop1 { get; } (OperationKind.PropertyReference, Type: C1) (Syntax: '.Prop1')
                              Instance Receiver: 
                                IFlowCaptureReferenceOperation: 2 (OperationKind.FlowCaptureReference, Type: C1, IsImplicit) (Syntax: 'c1')
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
                            IParameterReferenceOperation: c2 (OperationKind.ParameterReference, Type: C1) (Syntax: 'c2')
                    Jump if True (Regular) to Block[B6]
                        IIsNullOperation (OperationKind.IsNull, Type: System.Boolean, IsImplicit) (Syntax: 'c2')
                          Operand: 
                            IFlowCaptureReferenceOperation: 5 (OperationKind.FlowCaptureReference, Type: C1, IsImplicit) (Syntax: 'c2')
                        Leaving: {R5}
                    Next (Regular) Block[B5]
                Block[B5] - Block
                    Predecessors: [B4]
                    Statements (1)
                        IFlowCaptureOperation: 4 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: '.Prop1')
                          Value: 
                            IPropertyReferenceOperation: C1 C1.Prop1 { get; } (OperationKind.PropertyReference, Type: C1) (Syntax: '.Prop1')
                              Instance Receiver: 
                                IFlowCaptureReferenceOperation: 5 (OperationKind.FlowCaptureReference, Type: C1, IsImplicit) (Syntax: 'c2')
                    Next (Regular) Block[B7]
                        Leaving: {R5}
            }
            Block[B6] - Block
                Predecessors: [B4]
                Statements (1)
                    IFlowCaptureOperation: 4 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'c2')
                      Value: 
                        IDefaultValueOperation (OperationKind.DefaultValue, Type: C1, Constant: null, IsImplicit) (Syntax: 'c2')
                Next (Regular) Block[B7]
            Block[B7] - Block
                Predecessors: [B5] [B6]
                Statements (1)
                    IFlowCaptureOperation: 6 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: '.Prop1.M1(c2?.Prop1)')
                      Value: 
                        IInvocationOperation ( C1 C1.M1(C1 arg)) (OperationKind.Invocation, Type: C1) (Syntax: '.Prop1.M1(c2?.Prop1)')
                          Instance Receiver: 
                            IFlowCaptureReferenceOperation: 3 (OperationKind.FlowCaptureReference, Type: C1, IsImplicit) (Syntax: '.Prop1')
                          Arguments(1):
                              IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: arg) (OperationKind.Argument, Type: null) (Syntax: 'c2?.Prop1')
                                IFlowCaptureReferenceOperation: 4 (OperationKind.FlowCaptureReference, Type: C1, IsImplicit) (Syntax: 'c2?.Prop1')
                                InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                                OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                Next (Regular) Block[B8]
                    Leaving: {R3}
        }
        Block[B8] - Block
            Predecessors: [B7]
            Statements (0)
            Jump if True (Regular) to Block[B10]
                IIsNullOperation (OperationKind.IsNull, Type: System.Boolean, IsImplicit) (Syntax: '.Prop1.M1(c2?.Prop1)')
                  Operand: 
                    IFlowCaptureReferenceOperation: 6 (OperationKind.FlowCaptureReference, Type: C1, IsImplicit) (Syntax: '.Prop1.M1(c2?.Prop1)')
                Leaving: {R2}
            Next (Regular) Block[B9]
        Block[B9] - Block
            Predecessors: [B8]
            Statements (1)
                IFlowCaptureOperation: 1 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: '.Prop1')
                  Value: 
                    IPropertyReferenceOperation: C1 C1.Prop1 { get; } (OperationKind.PropertyReference, Type: C1) (Syntax: '.Prop1')
                      Instance Receiver: 
                        IFlowCaptureReferenceOperation: 6 (OperationKind.FlowCaptureReference, Type: C1, IsImplicit) (Syntax: '.Prop1.M1(c2?.Prop1)')
            Next (Regular) Block[B11]
                Leaving: {R2}
    }
    Block[B10] - Block
        Predecessors: [B2] [B8]
        Statements (1)
            IFlowCaptureOperation: 1 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'c1?.Prop1.M ... op1)?.Prop1')
              Value: 
                IDefaultValueOperation (OperationKind.DefaultValue, Type: C1, Constant: null, IsImplicit) (Syntax: 'c1?.Prop1.M ... op1)?.Prop1')
        Next (Regular) Block[B11]
    Block[B11] - Block
        Predecessors: [B9] [B10]
        Statements (1)
            IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'result = c1 ... p1)?.Prop1;')
              Expression: 
                ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: C1) (Syntax: 'result = c1 ... op1)?.Prop1')
                  Left: 
                    IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: C1, IsImplicit) (Syntax: 'result')
                  Right: 
                    IFlowCaptureReferenceOperation: 1 (OperationKind.FlowCaptureReference, Type: C1, IsImplicit) (Syntax: 'c1?.Prop1.M ... op1)?.Prop1')
        Next (Regular) Block[B12]
            Leaving: {R1}
}
Block[B12] - Exit
    Predecessors: [B11]
    Statements (0)
";

            VerifyFlowGraphForTest<BlockSyntax>(comp, expectedFlowGraph);
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
        Entering: {R1}

.locals {R1}
{
    CaptureIds: [0] [1]
    Block[B1] - Block
        Predecessors: [B0]
        Statements (1)
            IFlowCaptureOperation: 0 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'result')
              Value: 
                IParameterReferenceOperation: result (OperationKind.ParameterReference, Type: System.Int32?) (Syntax: 'result')

        Next (Regular) Block[B2]
            Entering: {R2}

    .locals {R2}
    {
        CaptureIds: [2]
        Block[B2] - Block
            Predecessors: [B1]
            Statements (1)
                IFlowCaptureOperation: 2 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'input')
                  Value: 
                    IParameterReferenceOperation: input (OperationKind.ParameterReference, Type: System.Array) (Syntax: 'input')

            Jump if True (Regular) to Block[B4]
                IIsNullOperation (OperationKind.IsNull, Type: System.Boolean, IsImplicit) (Syntax: 'input')
                  Operand: 
                    IFlowCaptureReferenceOperation: 2 (OperationKind.FlowCaptureReference, Type: System.Array, IsImplicit) (Syntax: 'input')
                Leaving: {R2}

            Next (Regular) Block[B3]
        Block[B3] - Block
            Predecessors: [B2]
            Statements (1)
                IFlowCaptureOperation: 1 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: '.Length')
                  Value: 
                    IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.Int32?, IsImplicit) (Syntax: '.Length')
                      Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                        (ImplicitNullable)
                      Operand: 
                        IPropertyReferenceOperation: System.Int32 System.Array.Length { get; } (OperationKind.PropertyReference, Type: System.Int32) (Syntax: '.Length')
                          Instance Receiver: 
                            IFlowCaptureReferenceOperation: 2 (OperationKind.FlowCaptureReference, Type: System.Array, IsImplicit) (Syntax: 'input')

            Next (Regular) Block[B5]
                Leaving: {R2}
    }

    Block[B4] - Block
        Predecessors: [B2]
        Statements (1)
            IFlowCaptureOperation: 1 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'input')
              Value: 
                IDefaultValueOperation (OperationKind.DefaultValue, Type: System.Int32?, IsImplicit) (Syntax: 'input')

        Next (Regular) Block[B5]
    Block[B5] - Block
        Predecessors: [B3] [B4]
        Statements (1)
            IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'result = input?.Length;')
              Expression: 
                ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Int32?) (Syntax: 'result = input?.Length')
                  Left: 
                    IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: System.Int32?, IsImplicit) (Syntax: 'result')
                  Right: 
                    IFlowCaptureReferenceOperation: 1 (OperationKind.FlowCaptureReference, Type: System.Int32?, IsImplicit) (Syntax: 'input?.Length')

        Next (Regular) Block[B6]
            Leaving: {R1}
}

Block[B6] - Exit
    Predecessors: [B5]
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
        Entering: {R1}

.locals {R1}
{
    CaptureIds: [0] [1]
    Block[B1] - Block
        Predecessors: [B0]
        Statements (1)
            IFlowCaptureOperation: 0 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'result')
              Value: 
                IParameterReferenceOperation: result (OperationKind.ParameterReference, Type: System.String) (Syntax: 'result')

        Next (Regular) Block[B2]
            Entering: {R2}
    .locals {R2}
    {
        CaptureIds: [2]
        Block[B2] - Block
            Predecessors: [B1]
            Statements (1)
                IFlowCaptureOperation: 2 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'input')
                  Value: 
                    IParameterReferenceOperation: input (OperationKind.ParameterReference, Type: System.Int32?) (Syntax: 'input')

            Jump if True (Regular) to Block[B4]
                IIsNullOperation (OperationKind.IsNull, Type: System.Boolean, IsImplicit) (Syntax: 'input')
                  Operand: 
                    IFlowCaptureReferenceOperation: 2 (OperationKind.FlowCaptureReference, Type: System.Int32?, IsImplicit) (Syntax: 'input')
                Leaving: {R2}

            Next (Regular) Block[B3]
        Block[B3] - Block
            Predecessors: [B2]
            Statements (1)
                IFlowCaptureOperation: 1 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: '.ToString()')
                  Value: 
                    IInvocationOperation (virtual System.String System.Int32.ToString()) (OperationKind.Invocation, Type: System.String) (Syntax: '.ToString()')
                      Instance Receiver: 
                        IInvocationOperation ( System.Int32 System.Int32?.GetValueOrDefault()) (OperationKind.Invocation, Type: System.Int32, IsImplicit) (Syntax: 'input')
                          Instance Receiver: 
                            IFlowCaptureReferenceOperation: 2 (OperationKind.FlowCaptureReference, Type: System.Int32?, IsImplicit) (Syntax: 'input')
                          Arguments(0)
                      Arguments(0)

            Next (Regular) Block[B5]
                Leaving: {R2}
    }

    Block[B4] - Block
        Predecessors: [B2]
        Statements (1)
            IFlowCaptureOperation: 1 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'input')
              Value: 
                IDefaultValueOperation (OperationKind.DefaultValue, Type: System.String, Constant: null, IsImplicit) (Syntax: 'input')
        Next (Regular) Block[B5]
    Block[B5] - Block
        Predecessors: [B3] [B4]
        Statements (1)
            IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'result = in ... ToString();')
              Expression: 
                ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.String) (Syntax: 'result = in ... .ToString()')
                  Left: 
                    IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: System.String, IsImplicit) (Syntax: 'result')
                  Right: 
                    IFlowCaptureReferenceOperation: 1 (OperationKind.FlowCaptureReference, Type: System.String, IsImplicit) (Syntax: 'input?.ToString()')

        Next (Regular) Block[B6]
            Leaving: {R1}
}

Block[B6] - Exit
    Predecessors: [B5]
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
        Entering: {R1}

.locals {R1}
{
    CaptureIds: [0] [1]
    Block[B1] - Block
        Predecessors: [B0]
        Statements (1)
            IFlowCaptureOperation: 0 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'result')
              Value: 
                IParameterReferenceOperation: result (OperationKind.ParameterReference, Type: System.Int32?) (Syntax: 'result')

        Next (Regular) Block[B2]
            Entering: {R2}

    .locals {R2}
    {
        CaptureIds: [2]
        Block[B2] - Block
            Predecessors: [B1]
            Statements (1)
                IFlowCaptureOperation: 2 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'input')
                  Value: 
                    IParameterReferenceOperation: input (OperationKind.ParameterReference, Type: P) (Syntax: 'input')

            Jump if True (Regular) to Block[B4]
                IIsNullOperation (OperationKind.IsNull, Type: System.Boolean, IsImplicit) (Syntax: 'input')
                  Operand: 
                    IFlowCaptureReferenceOperation: 2 (OperationKind.FlowCaptureReference, Type: P, IsImplicit) (Syntax: 'input')
                Leaving: {R2}

            Next (Regular) Block[B3]
        Block[B3] - Block
            Predecessors: [B2]
            Statements (1)
                IFlowCaptureOperation: 1 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: '.Access()')
                  Value: 
                    IInvocationOperation ( System.Int32? P.Access()) (OperationKind.Invocation, Type: System.Int32?) (Syntax: '.Access()')
                      Instance Receiver: 
                        IFlowCaptureReferenceOperation: 2 (OperationKind.FlowCaptureReference, Type: P, IsImplicit) (Syntax: 'input')
                      Arguments(0)

            Next (Regular) Block[B5]
                Leaving: {R2}
    }

    Block[B4] - Block
        Predecessors: [B2]
        Statements (1)
            IFlowCaptureOperation: 1 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'input')
              Value: 
                IDefaultValueOperation (OperationKind.DefaultValue, Type: System.Int32?, IsImplicit) (Syntax: 'input')

        Next (Regular) Block[B5]
    Block[B5] - Block
        Predecessors: [B3] [B4]
        Statements (1)
            IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'result = in ... ?.Access();')
              Expression: 
                ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Int32?) (Syntax: 'result = input?.Access()')
                  Left: 
                    IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: System.Int32?, IsImplicit) (Syntax: 'result')
                  Right: 
                    IFlowCaptureReferenceOperation: 1 (OperationKind.FlowCaptureReference, Type: System.Int32?, IsImplicit) (Syntax: 'input?.Access()')

        Next (Regular) Block[B6]
            Leaving: {R1}
}

Block[B6] - Exit
    Predecessors: [B5]
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
        Entering: {R1}

.locals {R1}
{
    CaptureIds: [0] [1]
    Block[B1] - Block
        Predecessors: [B0]
        Statements (1)
            IFlowCaptureOperation: 0 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'result')
              Value: 
                IParameterReferenceOperation: result (OperationKind.ParameterReference, Type: P) (Syntax: 'result')

        Next (Regular) Block[B2]
            Entering: {R2} {R3} {R4} {R5}

    .locals {R2}
    {
        CaptureIds: [5]
        .locals {R3}
        {
            CaptureIds: [2]
            .locals {R4}
            {
                CaptureIds: [4]
                .locals {R5}
                {
                    CaptureIds: [3]
                    Block[B2] - Block
                        Predecessors: [B1]
                        Statements (1)
                            IFlowCaptureOperation: 3 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'input')
                              Value: 
                                IParameterReferenceOperation: input (OperationKind.ParameterReference, Type: P) (Syntax: 'input')

                        Jump if True (Regular) to Block[B6]
                            IIsNullOperation (OperationKind.IsNull, Type: System.Boolean, IsImplicit) (Syntax: 'input')
                              Operand: 
                                IFlowCaptureReferenceOperation: 3 (OperationKind.FlowCaptureReference, Type: P, IsImplicit) (Syntax: 'input')
                            Leaving: {R5} {R4}

                        Next (Regular) Block[B3]
                    Block[B3] - Block
                        Predecessors: [B2]
                        Statements (1)
                            IFlowCaptureOperation: 4 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: '[11]')
                              Value: 
                                IPropertyReferenceOperation: P P.this[System.Int32 x] { get; } (OperationKind.PropertyReference, Type: P) (Syntax: '[11]')
                                  Instance Receiver: 
                                    IFlowCaptureReferenceOperation: 3 (OperationKind.FlowCaptureReference, Type: P, IsImplicit) (Syntax: 'input')
                                  Arguments(1):
                                      IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: x) (OperationKind.Argument, Type: null) (Syntax: '11')
                                        ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 11) (Syntax: '11')
                                        InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                                        OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)

                        Next (Regular) Block[B4]
                            Leaving: {R5}
                }

                Block[B4] - Block
                    Predecessors: [B3]
                    Statements (0)
                    Jump if True (Regular) to Block[B6]
                        IIsNullOperation (OperationKind.IsNull, Type: System.Boolean, IsImplicit) (Syntax: '[11]')
                          Operand: 
                            IFlowCaptureReferenceOperation: 4 (OperationKind.FlowCaptureReference, Type: P, IsImplicit) (Syntax: '[11]')
                        Leaving: {R4}

                    Next (Regular) Block[B5]
                Block[B5] - Block
                    Predecessors: [B4]
                    Statements (1)
                        IFlowCaptureOperation: 2 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: '.Access1()')
                          Value: 
                            IInvocationOperation ( P[] P.Access1()) (OperationKind.Invocation, Type: P[]) (Syntax: '.Access1()')
                              Instance Receiver: 
                                IFlowCaptureReferenceOperation: 4 (OperationKind.FlowCaptureReference, Type: P, IsImplicit) (Syntax: '[11]')
                              Arguments(0)

                    Next (Regular) Block[B7]
                        Leaving: {R4}
            }

            Block[B6] - Block
                Predecessors: [B2] [B4]
                Statements (1)
                    IFlowCaptureOperation: 2 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'input?[11]?.Access1()')
                      Value: 
                        IDefaultValueOperation (OperationKind.DefaultValue, Type: P[], Constant: null, IsImplicit) (Syntax: 'input?[11]?.Access1()')

                Next (Regular) Block[B7]
            Block[B7] - Block
                Predecessors: [B5] [B6]
                Statements (0)
                Jump if True (Regular) to Block[B11]
                    IIsNullOperation (OperationKind.IsNull, Type: System.Boolean, IsImplicit) (Syntax: 'input?[11]?.Access1()')
                      Operand: 
                        IFlowCaptureReferenceOperation: 2 (OperationKind.FlowCaptureReference, Type: P[], IsImplicit) (Syntax: 'input?[11]?.Access1()')
                    Leaving: {R3} {R2}

                Next (Regular) Block[B8]
            Block[B8] - Block
                Predecessors: [B7]
                Statements (1)
                    IFlowCaptureOperation: 5 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: '[22]')
                      Value: 
                        IArrayElementReferenceOperation (OperationKind.ArrayElementReference, Type: P) (Syntax: '[22]')
                          Array reference: 
                            IFlowCaptureReferenceOperation: 2 (OperationKind.FlowCaptureReference, Type: P[], IsImplicit) (Syntax: 'input?[11]?.Access1()')
                          Indices(1):
                              ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 22) (Syntax: '22')

                Next (Regular) Block[B9]
                    Leaving: {R3}
        }

        Block[B9] - Block
            Predecessors: [B8]
            Statements (0)
            Jump if True (Regular) to Block[B11]
                IIsNullOperation (OperationKind.IsNull, Type: System.Boolean, IsImplicit) (Syntax: '[22]')
                  Operand: 
                    IFlowCaptureReferenceOperation: 5 (OperationKind.FlowCaptureReference, Type: P, IsImplicit) (Syntax: '[22]')
                Leaving: {R2}

            Next (Regular) Block[B10]
        Block[B10] - Block
            Predecessors: [B9]
            Statements (1)
                IFlowCaptureOperation: 1 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: '.Access2()')
                  Value: 
                    IInvocationOperation ( P P.Access2()) (OperationKind.Invocation, Type: P) (Syntax: '.Access2()')
                      Instance Receiver: 
                        IFlowCaptureReferenceOperation: 5 (OperationKind.FlowCaptureReference, Type: P, IsImplicit) (Syntax: '[22]')
                      Arguments(0)

            Next (Regular) Block[B12]
                Leaving: {R2}
    }

    Block[B11] - Block
        Predecessors: [B7] [B9]
        Statements (1)
            IFlowCaptureOperation: 1 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: '(input?[11] ... ?.Access2()')
              Value: 
                IDefaultValueOperation (OperationKind.DefaultValue, Type: P, Constant: null, IsImplicit) (Syntax: '(input?[11] ... ?.Access2()')

        Next (Regular) Block[B12]
    Block[B12] - Block
        Predecessors: [B10] [B11]
        Statements (1)
            IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'result = (i ... .Access2();')
              Expression: 
                ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: P) (Syntax: 'result = (i ... ?.Access2()')
                  Left: 
                    IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: P, IsImplicit) (Syntax: 'result')
                  Right: 
                    IFlowCaptureReferenceOperation: 1 (OperationKind.FlowCaptureReference, Type: P, IsImplicit) (Syntax: '(input?[11] ... ?.Access2()')

        Next (Regular) Block[B13]
            Leaving: {R1}
}

Block[B13] - Exit
    Predecessors: [B12]
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
        Entering: {R1}

.locals {R1}
{
    CaptureIds: [0] [1]
    Block[B1] - Block
        Predecessors: [B0]
        Statements (1)
            IFlowCaptureOperation: 0 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'result')
              Value: 
                IParameterReferenceOperation: result (OperationKind.ParameterReference, Type: P?) (Syntax: 'result')

        Next (Regular) Block[B2]
            Entering: {R2} {R3} {R4} {R5}

    .locals {R2}
    {
        CaptureIds: [5]
        .locals {R3}
        {
            CaptureIds: [2]
            .locals {R4}
            {
                CaptureIds: [4]
                .locals {R5}
                {
                    CaptureIds: [3]
                    Block[B2] - Block
                        Predecessors: [B1]
                        Statements (1)
                            IFlowCaptureOperation: 3 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'input')
                              Value: 
                                IParameterReferenceOperation: input (OperationKind.ParameterReference, Type: P?) (Syntax: 'input')

                        Jump if True (Regular) to Block[B6]
                            IIsNullOperation (OperationKind.IsNull, Type: System.Boolean, IsImplicit) (Syntax: 'input')
                              Operand: 
                                IFlowCaptureReferenceOperation: 3 (OperationKind.FlowCaptureReference, Type: P?, IsImplicit) (Syntax: 'input')
                            Leaving: {R5} {R4}

                        Next (Regular) Block[B3]
                    Block[B3] - Block
                        Predecessors: [B2]
                        Statements (1)
                            IFlowCaptureOperation: 4 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: '.Access1()')
                              Value: 
                                IInvocationOperation ( P[] P.Access1()) (OperationKind.Invocation, Type: P[]) (Syntax: '.Access1()')
                                  Instance Receiver: 
                                    IInvocationOperation ( P P?.GetValueOrDefault()) (OperationKind.Invocation, Type: P, IsImplicit) (Syntax: 'input')
                                      Instance Receiver: 
                                        IFlowCaptureReferenceOperation: 3 (OperationKind.FlowCaptureReference, Type: P?, IsImplicit) (Syntax: 'input')
                                      Arguments(0)
                                  Arguments(0)

                        Next (Regular) Block[B4]
                            Leaving: {R5}
                }

                Block[B4] - Block
                    Predecessors: [B3]
                    Statements (0)
                    Jump if True (Regular) to Block[B6]
                        IIsNullOperation (OperationKind.IsNull, Type: System.Boolean, IsImplicit) (Syntax: '.Access1()')
                          Operand: 
                            IFlowCaptureReferenceOperation: 4 (OperationKind.FlowCaptureReference, Type: P[], IsImplicit) (Syntax: '.Access1()')
                        Leaving: {R4}

                    Next (Regular) Block[B5]
                Block[B5] - Block
                    Predecessors: [B4]
                    Statements (1)
                        IFlowCaptureOperation: 2 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: '[11]')
                          Value: 
                            IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: P?, IsImplicit) (Syntax: '[11]')
                              Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                                (ImplicitNullable)
                              Operand: 
                                IArrayElementReferenceOperation (OperationKind.ArrayElementReference, Type: P) (Syntax: '[11]')
                                  Array reference: 
                                    IFlowCaptureReferenceOperation: 4 (OperationKind.FlowCaptureReference, Type: P[], IsImplicit) (Syntax: '.Access1()')
                                  Indices(1):
                                      ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 11) (Syntax: '11')

                    Next (Regular) Block[B7]
                        Leaving: {R4}
            }

            Block[B6] - Block
                Predecessors: [B2] [B4]
                Statements (1)
                    IFlowCaptureOperation: 2 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'input?.Access1()?[11]')
                      Value: 
                        IDefaultValueOperation (OperationKind.DefaultValue, Type: P?, IsImplicit) (Syntax: 'input?.Access1()?[11]')

                Next (Regular) Block[B7]
            Block[B7] - Block
                Predecessors: [B5] [B6]
                Statements (0)
                Jump if True (Regular) to Block[B11]
                    IIsNullOperation (OperationKind.IsNull, Type: System.Boolean, IsImplicit) (Syntax: 'input?.Access1()?[11]')
                      Operand: 
                        IFlowCaptureReferenceOperation: 2 (OperationKind.FlowCaptureReference, Type: P?, IsImplicit) (Syntax: 'input?.Access1()?[11]')
                    Leaving: {R3} {R2}

                Next (Regular) Block[B8]
            Block[B8] - Block
                Predecessors: [B7]
                Statements (1)
                    IFlowCaptureOperation: 5 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: '[22]')
                      Value: 
                        IPropertyReferenceOperation: P? P.this[System.Int32 x] { get; } (OperationKind.PropertyReference, Type: P?) (Syntax: '[22]')
                          Instance Receiver: 
                            IInvocationOperation ( P P?.GetValueOrDefault()) (OperationKind.Invocation, Type: P, IsImplicit) (Syntax: 'input?.Access1()?[11]')
                              Instance Receiver: 
                                IFlowCaptureReferenceOperation: 2 (OperationKind.FlowCaptureReference, Type: P?, IsImplicit) (Syntax: 'input?.Access1()?[11]')
                              Arguments(0)
                          Arguments(1):
                              IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: x) (OperationKind.Argument, Type: null) (Syntax: '22')
                                ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 22) (Syntax: '22')
                                InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                                OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)

                Next (Regular) Block[B9]
                    Leaving: {R3}
        }

        Block[B9] - Block
            Predecessors: [B8]
            Statements (0)
            Jump if True (Regular) to Block[B11]
                IIsNullOperation (OperationKind.IsNull, Type: System.Boolean, IsImplicit) (Syntax: '[22]')
                  Operand: 
                    IFlowCaptureReferenceOperation: 5 (OperationKind.FlowCaptureReference, Type: P?, IsImplicit) (Syntax: '[22]')
                Leaving: {R2}

            Next (Regular) Block[B10]
        Block[B10] - Block
            Predecessors: [B9]
            Statements (1)
                IFlowCaptureOperation: 1 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: '.Access2()')
                  Value: 
                    IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: P?, IsImplicit) (Syntax: '.Access2()')
                      Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                        (ImplicitNullable)
                      Operand: 
                        IInvocationOperation ( P P.Access2()) (OperationKind.Invocation, Type: P) (Syntax: '.Access2()')
                          Instance Receiver: 
                            IInvocationOperation ( P P?.GetValueOrDefault()) (OperationKind.Invocation, Type: P, IsImplicit) (Syntax: '[22]')
                              Instance Receiver: 
                                IFlowCaptureReferenceOperation: 5 (OperationKind.FlowCaptureReference, Type: P?, IsImplicit) (Syntax: '[22]')
                              Arguments(0)
                          Arguments(0)

            Next (Regular) Block[B12]
                Leaving: {R2}
    }

    Block[B11] - Block
        Predecessors: [B7] [B9]
        Statements (1)
            IFlowCaptureOperation: 1 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: '(input?.Acc ... ?.Access2()')
              Value: 
                IDefaultValueOperation (OperationKind.DefaultValue, Type: P?, IsImplicit) (Syntax: '(input?.Acc ... ?.Access2()')

        Next (Regular) Block[B12]
    Block[B12] - Block
        Predecessors: [B10] [B11]
        Statements (1)
            IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'result = (i ... .Access2();')
              Expression: 
                ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: P?) (Syntax: 'result = (i ... ?.Access2()')
                  Left: 
                    IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: P?, IsImplicit) (Syntax: 'result')
                  Right: 
                    IFlowCaptureReferenceOperation: 1 (OperationKind.FlowCaptureReference, Type: P?, IsImplicit) (Syntax: '(input?.Acc ... ?.Access2()')

        Next (Regular) Block[B13]
            Leaving: {R1}
}

Block[B13] - Exit
    Predecessors: [B12]
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
                    IParameterReferenceOperation: x (OperationKind.ParameterReference, Type: S1?, IsInvalid) (Syntax: 'x')

            Jump if True (Regular) to Block[B3]
                IIsNullOperation (OperationKind.IsNull, Type: System.Boolean, IsInvalid, IsImplicit) (Syntax: 'x')
                  Operand: 
                    IFlowCaptureReferenceOperation: 1 (OperationKind.FlowCaptureReference, Type: S1?, IsInvalid, IsImplicit) (Syntax: 'x')
                Leaving: {R2}

            Next (Regular) Block[B2]
        Block[B2] - Block
            Predecessors: [B1]
            Statements (1)
                IFlowCaptureOperation: 0 (OperationKind.FlowCapture, Type: null, IsInvalid, IsImplicit) (Syntax: '.P1')
                  Value: 
                    IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.Int32?, IsInvalid, IsImplicit) (Syntax: '.P1')
                      Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                        (ImplicitNullable)
                      Operand: 
                        IPropertyReferenceOperation: System.Int32 S1.P1 { get; set; } (OperationKind.PropertyReference, Type: System.Int32, IsInvalid) (Syntax: '.P1')
                          Instance Receiver: 
                            IInvocationOperation ( S1 S1?.GetValueOrDefault()) (OperationKind.Invocation, Type: S1, IsInvalid, IsImplicit) (Syntax: 'x')
                              Instance Receiver: 
                                IFlowCaptureReferenceOperation: 1 (OperationKind.FlowCaptureReference, Type: S1?, IsInvalid, IsImplicit) (Syntax: 'x')
                              Arguments(0)

            Next (Regular) Block[B4]
                Leaving: {R2}
    }

    Block[B3] - Block
        Predecessors: [B1]
        Statements (1)
            IFlowCaptureOperation: 0 (OperationKind.FlowCapture, Type: null, IsInvalid, IsImplicit) (Syntax: 'x')
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
                          IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: System.Int32?, IsInvalid, IsImplicit) (Syntax: 'x?.P1')
                  Right: 
                    ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 0) (Syntax: '0')

        Next (Regular) Block[B5]
            Leaving: {R1}
}

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
                    IParameterReferenceOperation: x (OperationKind.ParameterReference, Type: S1?, IsInvalid) (Syntax: 'x')

            Jump if True (Regular) to Block[B3]
                IIsNullOperation (OperationKind.IsNull, Type: System.Boolean, IsInvalid, IsImplicit) (Syntax: 'x')
                  Operand: 
                    IFlowCaptureReferenceOperation: 1 (OperationKind.FlowCaptureReference, Type: S1?, IsInvalid, IsImplicit) (Syntax: 'x')
                Leaving: {R2}

            Next (Regular) Block[B2]
        Block[B2] - Block
            Predecessors: [B1]
            Statements (1)
                IFlowCaptureOperation: 0 (OperationKind.FlowCapture, Type: null, IsInvalid, IsImplicit) (Syntax: '.P1')
                  Value: 
                    IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.Int32?, IsInvalid, IsImplicit) (Syntax: '.P1')
                      Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                        (ImplicitNullable)
                      Operand: 
                        IFieldReferenceOperation: System.Int32 S1.P1 (OperationKind.FieldReference, Type: System.Int32, IsInvalid) (Syntax: '.P1')
                          Instance Receiver: 
                            IInvocationOperation ( S1 S1?.GetValueOrDefault()) (OperationKind.Invocation, Type: S1, IsInvalid, IsImplicit) (Syntax: 'x')
                              Instance Receiver: 
                                IFlowCaptureReferenceOperation: 1 (OperationKind.FlowCaptureReference, Type: S1?, IsInvalid, IsImplicit) (Syntax: 'x')
                              Arguments(0)

            Next (Regular) Block[B4]
                Leaving: {R2}
    }

    Block[B3] - Block
        Predecessors: [B1]
        Statements (1)
            IFlowCaptureOperation: 0 (OperationKind.FlowCapture, Type: null, IsInvalid, IsImplicit) (Syntax: 'x')
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
                          IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: System.Int32?, IsInvalid, IsImplicit) (Syntax: 'x?.P1')
                  Right: 
                    ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 0) (Syntax: '0')

        Next (Regular) Block[B5]
            Leaving: {R1}
}

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
            var compilation = CreateCompilationWithMscorlib45(source);
            compilation.MakeMemberMissing(SpecialMember.System_Nullable_T_GetValueOrDefault);

            string expectedGraph = @"
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
                IParameterReferenceOperation: result (OperationKind.ParameterReference, Type: System.Int32?) (Syntax: 'result')

        Next (Regular) Block[B2]
            Entering: {R2}

    .locals {R2}
    {
        CaptureIds: [2]
        Block[B2] - Block
            Predecessors: [B1]
            Statements (1)
                IFlowCaptureOperation: 2 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'input')
                  Value: 
                    IParameterReferenceOperation: input (OperationKind.ParameterReference, Type: P?) (Syntax: 'input')

            Jump if True (Regular) to Block[B4]
                IIsNullOperation (OperationKind.IsNull, Type: System.Boolean, IsImplicit) (Syntax: 'input')
                  Operand: 
                    IFlowCaptureReferenceOperation: 2 (OperationKind.FlowCaptureReference, Type: P?, IsImplicit) (Syntax: 'input')
                Leaving: {R2}

            Next (Regular) Block[B3]
        Block[B3] - Block
            Predecessors: [B2]
            Statements (1)
                IFlowCaptureOperation: 1 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: '.Length')
                  Value: 
                    IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.Int32?, IsImplicit) (Syntax: '.Length')
                      Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                        (ImplicitNullable)
                      Operand: 
                        IPropertyReferenceOperation: System.Int32 P.Length { get; } (OperationKind.PropertyReference, Type: System.Int32) (Syntax: '.Length')
                          Instance Receiver: 
                            IInvalidOperation (OperationKind.Invalid, Type: P, IsImplicit) (Syntax: 'input')
                              Children(1):
                                  IFlowCaptureReferenceOperation: 2 (OperationKind.FlowCaptureReference, Type: P?, IsImplicit) (Syntax: 'input')

            Next (Regular) Block[B5]
                Leaving: {R2}
    }

    Block[B4] - Block
        Predecessors: [B2]
        Statements (1)
            IFlowCaptureOperation: 1 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'input')
              Value: 
                IDefaultValueOperation (OperationKind.DefaultValue, Type: System.Int32?, IsImplicit) (Syntax: 'input')

        Next (Regular) Block[B5]
    Block[B5] - Block
        Predecessors: [B3] [B4]
        Statements (1)
            IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'result = input?.Length;')
              Expression: 
                ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Int32?) (Syntax: 'result = input?.Length')
                  Left: 
                    IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: System.Int32?, IsImplicit) (Syntax: 'result')
                  Right: 
                    IFlowCaptureReferenceOperation: 1 (OperationKind.FlowCaptureReference, Type: System.Int32?, IsImplicit) (Syntax: 'input?.Length')

        Next (Regular) Block[B6]
            Leaving: {R1}
}

Block[B6] - Exit
    Predecessors: [B5]
    Statements (0)
";
            var expectedDiagnostics = DiagnosticDescription.None;

            VerifyFlowGraphAndDiagnosticsForTest<BlockSyntax>(compilation, expectedGraph, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation, CompilerFeature.Dataflow)]
        [Fact]
        public void ConditionalAccessFlow_09()
        {
            string source = @"
class C
{
    void M1(C input1, C input2, C input3)
/*<bind>*/{
        input1?.M(input2?.M(input3?.M(null)));
    }/*</bind>*/

    public string M(string x) => x;
}
";
            string expectedGraph = @"
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
            IFlowCaptureOperation: 0 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'input1')
              Value: 
                IParameterReferenceOperation: input1 (OperationKind.ParameterReference, Type: C) (Syntax: 'input1')

        Jump if True (Regular) to Block[B9]
            IIsNullOperation (OperationKind.IsNull, Type: System.Boolean, IsImplicit) (Syntax: 'input1')
              Operand: 
                IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: C, IsImplicit) (Syntax: 'input1')
            Leaving: {R1}

        Next (Regular) Block[B2]
            Entering: {R2}

    .locals {R2}
    {
        CaptureIds: [2] [3]
        Block[B2] - Block
            Predecessors: [B1]
            Statements (1)
                IFlowCaptureOperation: 2 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'input2')
                  Value: 
                    IParameterReferenceOperation: input2 (OperationKind.ParameterReference, Type: C) (Syntax: 'input2')

            Jump if True (Regular) to Block[B7]
                IIsNullOperation (OperationKind.IsNull, Type: System.Boolean, IsImplicit) (Syntax: 'input2')
                  Operand: 
                    IFlowCaptureReferenceOperation: 2 (OperationKind.FlowCaptureReference, Type: C, IsImplicit) (Syntax: 'input2')
                Leaving: {R2}

            Next (Regular) Block[B3]
                Entering: {R3}

        .locals {R3}
        {
            CaptureIds: [4]
            Block[B3] - Block
                Predecessors: [B2]
                Statements (1)
                    IFlowCaptureOperation: 4 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'input3')
                      Value: 
                        IParameterReferenceOperation: input3 (OperationKind.ParameterReference, Type: C) (Syntax: 'input3')

                Jump if True (Regular) to Block[B5]
                    IIsNullOperation (OperationKind.IsNull, Type: System.Boolean, IsImplicit) (Syntax: 'input3')
                      Operand: 
                        IFlowCaptureReferenceOperation: 4 (OperationKind.FlowCaptureReference, Type: C, IsImplicit) (Syntax: 'input3')
                    Leaving: {R3}

                Next (Regular) Block[B4]
            Block[B4] - Block
                Predecessors: [B3]
                Statements (1)
                    IFlowCaptureOperation: 3 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: '.M(null)')
                      Value: 
                        IInvocationOperation ( System.String C.M(System.String x)) (OperationKind.Invocation, Type: System.String) (Syntax: '.M(null)')
                          Instance Receiver: 
                            IFlowCaptureReferenceOperation: 4 (OperationKind.FlowCaptureReference, Type: C, IsImplicit) (Syntax: 'input3')
                          Arguments(1):
                              IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: x) (OperationKind.Argument, Type: null) (Syntax: 'null')
                                IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.String, Constant: null, IsImplicit) (Syntax: 'null')
                                  Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: True, IsUserDefined: False) (MethodSymbol: null)
                                    (ImplicitReference)
                                  Operand: 
                                    ILiteralOperation (OperationKind.Literal, Type: null, Constant: null) (Syntax: 'null')
                                InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                                OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)

                Next (Regular) Block[B6]
                    Leaving: {R3}
        }

        Block[B5] - Block
            Predecessors: [B3]
            Statements (1)
                IFlowCaptureOperation: 3 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'input3')
                  Value: 
                    IDefaultValueOperation (OperationKind.DefaultValue, Type: System.String, Constant: null, IsImplicit) (Syntax: 'input3')

            Next (Regular) Block[B6]
        Block[B6] - Block
            Predecessors: [B4] [B5]
            Statements (1)
                IFlowCaptureOperation: 1 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: '.M(input3?.M(null))')
                  Value: 
                    IInvocationOperation ( System.String C.M(System.String x)) (OperationKind.Invocation, Type: System.String) (Syntax: '.M(input3?.M(null))')
                      Instance Receiver: 
                        IFlowCaptureReferenceOperation: 2 (OperationKind.FlowCaptureReference, Type: C, IsImplicit) (Syntax: 'input2')
                      Arguments(1):
                          IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: x) (OperationKind.Argument, Type: null) (Syntax: 'input3?.M(null)')
                            IFlowCaptureReferenceOperation: 3 (OperationKind.FlowCaptureReference, Type: System.String, IsImplicit) (Syntax: 'input3?.M(null)')
                            InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                            OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)

            Next (Regular) Block[B8]
                Leaving: {R2}
    }

    Block[B7] - Block
        Predecessors: [B2]
        Statements (1)
            IFlowCaptureOperation: 1 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'input2')
              Value: 
                IDefaultValueOperation (OperationKind.DefaultValue, Type: System.String, Constant: null, IsImplicit) (Syntax: 'input2')

        Next (Regular) Block[B8]
    Block[B8] - Block
        Predecessors: [B6] [B7]
        Statements (1)
            IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'input1?.M(i ... .M(null)));')
              Expression: 
                IInvocationOperation ( System.String C.M(System.String x)) (OperationKind.Invocation, Type: System.String) (Syntax: '.M(input2?. ... ?.M(null)))')
                  Instance Receiver: 
                    IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: C, IsImplicit) (Syntax: 'input1')
                  Arguments(1):
                      IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: x) (OperationKind.Argument, Type: null) (Syntax: 'input2?.M(i ... 3?.M(null))')
                        IFlowCaptureReferenceOperation: 1 (OperationKind.FlowCaptureReference, Type: System.String, IsImplicit) (Syntax: 'input2?.M(i ... 3?.M(null))')
                        InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                        OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)

        Next (Regular) Block[B9]
            Leaving: {R1}
}

Block[B9] - Exit
    Predecessors: [B1] [B8]
    Statements (0)
";
            var expectedDiagnostics = DiagnosticDescription.None;

            VerifyFlowGraphAndDiagnosticsForTest<BlockSyntax>(source, expectedGraph, expectedDiagnostics);
        }

        [Fact]
        public void NestedConditionalAccess()
        {
            var comp = CreateCompilation(@"
class C
{
    C Prop { get; set; }
    object M2(C c) => throw null;
    void M(C c)
    /*<bind>*/{
        _ = c?.Prop.M2(c?.Prop);
    }/*</bind>*/
}");

            VerifyFlowGraphAndDiagnosticsForTest<BlockSyntax>(comp, @"
Block[B0] - Entry
    Statements (0)
    Next (Regular) Block[B1]
        Entering: {R1} {R2} {R3}
.locals {R1}
{
    CaptureIds: [0]
    .locals {R2}
    {
        CaptureIds: [2] [3]
        .locals {R3}
        {
            CaptureIds: [1]
            Block[B1] - Block
                Predecessors: [B0]
                Statements (1)
                    IFlowCaptureOperation: 1 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'c')
                      Value: 
                        IParameterReferenceOperation: c (OperationKind.ParameterReference, Type: C) (Syntax: 'c')
                Jump if True (Regular) to Block[B7]
                    IIsNullOperation (OperationKind.IsNull, Type: System.Boolean, IsImplicit) (Syntax: 'c')
                      Operand: 
                        IFlowCaptureReferenceOperation: 1 (OperationKind.FlowCaptureReference, Type: C, IsImplicit) (Syntax: 'c')
                    Leaving: {R3} {R2}
                Next (Regular) Block[B2]
            Block[B2] - Block
                Predecessors: [B1]
                Statements (1)
                    IFlowCaptureOperation: 2 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: '.Prop')
                      Value: 
                        IPropertyReferenceOperation: C C.Prop { get; set; } (OperationKind.PropertyReference, Type: C) (Syntax: '.Prop')
                          Instance Receiver: 
                            IFlowCaptureReferenceOperation: 1 (OperationKind.FlowCaptureReference, Type: C, IsImplicit) (Syntax: 'c')
                Next (Regular) Block[B3]
                    Leaving: {R3}
                    Entering: {R4}
        }
        .locals {R4}
        {
            CaptureIds: [4]
            Block[B3] - Block
                Predecessors: [B2]
                Statements (1)
                    IFlowCaptureOperation: 4 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'c')
                      Value: 
                        IParameterReferenceOperation: c (OperationKind.ParameterReference, Type: C) (Syntax: 'c')
                Jump if True (Regular) to Block[B5]
                    IIsNullOperation (OperationKind.IsNull, Type: System.Boolean, IsImplicit) (Syntax: 'c')
                      Operand: 
                        IFlowCaptureReferenceOperation: 4 (OperationKind.FlowCaptureReference, Type: C, IsImplicit) (Syntax: 'c')
                    Leaving: {R4}
                Next (Regular) Block[B4]
            Block[B4] - Block
                Predecessors: [B3]
                Statements (1)
                    IFlowCaptureOperation: 3 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: '.Prop')
                      Value: 
                        IPropertyReferenceOperation: C C.Prop { get; set; } (OperationKind.PropertyReference, Type: C) (Syntax: '.Prop')
                          Instance Receiver: 
                            IFlowCaptureReferenceOperation: 4 (OperationKind.FlowCaptureReference, Type: C, IsImplicit) (Syntax: 'c')
                Next (Regular) Block[B6]
                    Leaving: {R4}
        }
        Block[B5] - Block
            Predecessors: [B3]
            Statements (1)
                IFlowCaptureOperation: 3 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'c')
                  Value: 
                    IDefaultValueOperation (OperationKind.DefaultValue, Type: C, Constant: null, IsImplicit) (Syntax: 'c')
            Next (Regular) Block[B6]
        Block[B6] - Block
            Predecessors: [B4] [B5]
            Statements (1)
                IFlowCaptureOperation: 0 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: '.Prop.M2(c?.Prop)')
                  Value: 
                    IInvocationOperation ( System.Object C.M2(C c)) (OperationKind.Invocation, Type: System.Object) (Syntax: '.Prop.M2(c?.Prop)')
                      Instance Receiver: 
                        IFlowCaptureReferenceOperation: 2 (OperationKind.FlowCaptureReference, Type: C, IsImplicit) (Syntax: '.Prop')
                      Arguments(1):
                          IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: c) (OperationKind.Argument, Type: null) (Syntax: 'c?.Prop')
                            IFlowCaptureReferenceOperation: 3 (OperationKind.FlowCaptureReference, Type: C, IsImplicit) (Syntax: 'c?.Prop')
                            InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                            OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
            Next (Regular) Block[B8]
                Leaving: {R2}
    }
    Block[B7] - Block
        Predecessors: [B1]
        Statements (1)
            IFlowCaptureOperation: 0 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'c')
              Value: 
                IDefaultValueOperation (OperationKind.DefaultValue, Type: System.Object, Constant: null, IsImplicit) (Syntax: 'c')
        Next (Regular) Block[B8]
    Block[B8] - Block
        Predecessors: [B6] [B7]
        Statements (1)
            IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: '_ = c?.Prop.M2(c?.Prop);')
              Expression: 
                ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Object) (Syntax: '_ = c?.Prop.M2(c?.Prop)')
                  Left: 
                    IDiscardOperation (Symbol: System.Object _) (OperationKind.Discard, Type: System.Object) (Syntax: '_')
                  Right: 
                    IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: System.Object, IsImplicit) (Syntax: 'c?.Prop.M2(c?.Prop)')
        Next (Regular) Block[B9]
            Leaving: {R1}
}
Block[B9] - Exit
    Predecessors: [B8]
    Statements (0)
", DiagnosticDescription.None);
        }

        [Fact]
        public void InvalidConditionalAccess_01()
        {
            var code = @"
class C
{
    void M()
    /*<bind>*/{
        _ = 123?[1, 2];
    }/*</bind>*/
}
";

            var expectedDiagnostics = new[]
            {
                // file.cs(6,16): error CS0023: Operator '?' cannot be applied to operand of type 'int'
                //         _ = 123?[1, 2];
                Diagnostic(ErrorCode.ERR_BadUnaryOp, "?").WithArguments("?", "int").WithLocation(6, 16)
             };

            VerifyFlowGraphAndDiagnosticsForTest<BlockSyntax>(code, @"
Block[B0] - Entry
    Statements (0)
    Next (Regular) Block[B1]
        Entering: {R1} {R2}
.locals {R1}
{
    CaptureIds: [0]
    .locals {R2}
    {
        CaptureIds: [1] [2] [3]
        Block[B1] - Block
            Predecessors: [B0]
            Statements (3)
                IFlowCaptureOperation: 1 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: '1')
                  Value: 
                    ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 1) (Syntax: '1')
                IFlowCaptureOperation: 2 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: '2')
                  Value: 
                    ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 2) (Syntax: '2')
                IFlowCaptureOperation: 3 (OperationKind.FlowCapture, Type: null, IsInvalid, IsImplicit) (Syntax: '123')
                  Value: 
                    IInvalidOperation (OperationKind.Invalid, Type: ?, IsInvalid, IsImplicit) (Syntax: '123')
                      Children(1):
                          ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 123, IsInvalid) (Syntax: '123')
            Jump if True (Regular) to Block[B3]
                IIsNullOperation (OperationKind.IsNull, Type: System.Boolean, IsInvalid, IsImplicit) (Syntax: '123')
                  Operand: 
                    IFlowCaptureReferenceOperation: 3 (OperationKind.FlowCaptureReference, Type: ?, IsInvalid, IsImplicit) (Syntax: '123')
                Leaving: {R2}
            Next (Regular) Block[B2]
        Block[B2] - Block
            Predecessors: [B1]
            Statements (1)
                IFlowCaptureOperation: 0 (OperationKind.FlowCapture, Type: null, IsInvalid, IsImplicit) (Syntax: '[1, 2]')
                  Value: 
                    IInvalidOperation (OperationKind.Invalid, Type: ?, IsInvalid) (Syntax: '[1, 2]')
                      Children(3):
                          IFlowCaptureReferenceOperation: 1 (OperationKind.FlowCaptureReference, Type: System.Int32, Constant: 1, IsImplicit) (Syntax: '1')
                          IFlowCaptureReferenceOperation: 2 (OperationKind.FlowCaptureReference, Type: System.Int32, Constant: 2, IsImplicit) (Syntax: '2')
                          IFlowCaptureReferenceOperation: 3 (OperationKind.FlowCaptureReference, Type: ?, IsInvalid, IsImplicit) (Syntax: '123')
            Next (Regular) Block[B4]
                Leaving: {R2}
    }
    Block[B3] - Block
        Predecessors: [B1]
        Statements (1)
            IFlowCaptureOperation: 0 (OperationKind.FlowCapture, Type: null, IsInvalid, IsImplicit) (Syntax: '123')
              Value: 
                IDefaultValueOperation (OperationKind.DefaultValue, Type: ?, Constant: null, IsInvalid, IsImplicit) (Syntax: '123')
        Next (Regular) Block[B4]
    Block[B4] - Block
        Predecessors: [B2] [B3]
        Statements (1)
            IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null, IsInvalid) (Syntax: '_ = 123?[1, 2];')
              Expression: 
                ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: ?, IsInvalid) (Syntax: '_ = 123?[1, 2]')
                  Left: 
                    IDiscardOperation (Symbol: ? _) (OperationKind.Discard, Type: ?) (Syntax: '_')
                  Right: 
                    IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: ?, IsInvalid, IsImplicit) (Syntax: '123?[1, 2]')
        Next (Regular) Block[B5]
            Leaving: {R1}
}
Block[B5] - Exit
    Predecessors: [B4]
    Statements (0)
", expectedDiagnostics);
        }

        [Fact]
        public void InvalidConditionalAccess_02()
        {
            var code = @"
class C
{
    void M()
    /*<bind>*/{
        _ = 123?[1, 2].ToString();
    }/*</bind>*/
}
";

            var expectedDiagnostics = new[]
            {
                // file.cs(6,16): error CS0023: Operator '?' cannot be applied to operand of type 'int'
                //         _ = 123?[1, 2];
                Diagnostic(ErrorCode.ERR_BadUnaryOp, "?").WithArguments("?", "int").WithLocation(6, 16)
            };

            VerifyFlowGraphAndDiagnosticsForTest<BlockSyntax>(code, @"
Block[B0] - Entry
    Statements (0)
    Next (Regular) Block[B1]
        Entering: {R1} {R2}
.locals {R1}
{
    CaptureIds: [0]
    .locals {R2}
    {
        CaptureIds: [1] [2] [3]
        Block[B1] - Block
            Predecessors: [B0]
            Statements (3)
                IFlowCaptureOperation: 1 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: '1')
                  Value: 
                    ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 1) (Syntax: '1')
                IFlowCaptureOperation: 2 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: '2')
                  Value: 
                    ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 2) (Syntax: '2')
                IFlowCaptureOperation: 3 (OperationKind.FlowCapture, Type: null, IsInvalid, IsImplicit) (Syntax: '123')
                  Value: 
                    IInvalidOperation (OperationKind.Invalid, Type: ?, IsInvalid, IsImplicit) (Syntax: '123')
                      Children(1):
                          ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 123, IsInvalid) (Syntax: '123')
            Jump if True (Regular) to Block[B3]
                IIsNullOperation (OperationKind.IsNull, Type: System.Boolean, IsInvalid, IsImplicit) (Syntax: '123')
                  Operand: 
                    IFlowCaptureReferenceOperation: 3 (OperationKind.FlowCaptureReference, Type: ?, IsInvalid, IsImplicit) (Syntax: '123')
                Leaving: {R2}
            Next (Regular) Block[B2]
        Block[B2] - Block
            Predecessors: [B1]
            Statements (1)
                IFlowCaptureOperation: 0 (OperationKind.FlowCapture, Type: null, IsInvalid, IsImplicit) (Syntax: '[1, 2].ToString()')
                  Value: 
                    IInvalidOperation (OperationKind.Invalid, Type: ?, IsInvalid) (Syntax: '[1, 2].ToString()')
                      Children(1):
                          IOperation:  (OperationKind.None, Type: null, IsInvalid) (Syntax: '[1, 2].ToString')
                            Children(1):
                                IInvalidOperation (OperationKind.Invalid, Type: ?, IsInvalid) (Syntax: '[1, 2]')
                                  Children(3):
                                      IFlowCaptureReferenceOperation: 1 (OperationKind.FlowCaptureReference, Type: System.Int32, Constant: 1, IsImplicit) (Syntax: '1')
                                      IFlowCaptureReferenceOperation: 2 (OperationKind.FlowCaptureReference, Type: System.Int32, Constant: 2, IsImplicit) (Syntax: '2')
                                      IFlowCaptureReferenceOperation: 3 (OperationKind.FlowCaptureReference, Type: ?, IsInvalid, IsImplicit) (Syntax: '123')
            Next (Regular) Block[B4]
                Leaving: {R2}
    }
    Block[B3] - Block
        Predecessors: [B1]
        Statements (1)
            IFlowCaptureOperation: 0 (OperationKind.FlowCapture, Type: null, IsInvalid, IsImplicit) (Syntax: '123')
              Value: 
                IDefaultValueOperation (OperationKind.DefaultValue, Type: ?, Constant: null, IsInvalid, IsImplicit) (Syntax: '123')
        Next (Regular) Block[B4]
    Block[B4] - Block
        Predecessors: [B2] [B3]
        Statements (1)
            IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null, IsInvalid) (Syntax: '_ = 123?[1, ... ToString();')
              Expression: 
                ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: ?, IsInvalid) (Syntax: '_ = 123?[1, ... .ToString()')
                  Left: 
                    IDiscardOperation (Symbol: ? _) (OperationKind.Discard, Type: ?) (Syntax: '_')
                  Right: 
                    IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: ?, IsInvalid, IsImplicit) (Syntax: '123?[1, 2].ToString()')
        Next (Regular) Block[B5]
            Leaving: {R1}
}
Block[B5] - Exit
    Predecessors: [B4]
    Statements (0)
", expectedDiagnostics);
        }
    }
}
