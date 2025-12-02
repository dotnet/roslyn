// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System.Linq;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.CSharp.UnitTests
{
    [CompilerTrait(CompilerFeature.IOperation)]
    public class IOperationTests_ICoalesceAssignmentOperation : SemanticModelTestBase
    {
        [Fact]
        public void CoalesceAssignment_SimpleCase()
        {

            string source = @"
class C
{
    static void M(object o1, object o2)
    {
        /*<bind>*/o1 ??= o2/*</bind>*/;
    }
}
";
            string expectedOperationTree = @"
ICoalesceAssignmentOperation (OperationKind.CoalesceAssignment, Type: System.Object) (Syntax: 'o1 ??= o2')
  Target: 
    IParameterReferenceOperation: o1 (OperationKind.ParameterReference, Type: System.Object) (Syntax: 'o1')
  Value: 
    IParameterReferenceOperation: o2 (OperationKind.ParameterReference, Type: System.Object) (Syntax: 'o2')
";
            var expectedDiagnostics = DiagnosticDescription.None;

            VerifyOperationTreeAndDiagnosticsForTest<AssignmentExpressionSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }

        [Fact]
        public void CoalesceAssignment_WithConversion()
        {
            string source = @"
class C
{
    static void M(object o1, string s1)
    {
        /*<bind>*/o1 ??= s1/*</bind>*/;
    }
}
";
            string expectedOperationTree = @"
ICoalesceAssignmentOperation (OperationKind.CoalesceAssignment, Type: System.Object) (Syntax: 'o1 ??= s1')
  Target: 
    IParameterReferenceOperation: o1 (OperationKind.ParameterReference, Type: System.Object) (Syntax: 'o1')
  Value: 
    IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.Object, IsImplicit) (Syntax: 's1')
      Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: True, IsUserDefined: False) (MethodSymbol: null)
      Operand: 
        IParameterReferenceOperation: s1 (OperationKind.ParameterReference, Type: System.String) (Syntax: 's1')
";
            var expectedDiagnostics = DiagnosticDescription.None;

            VerifyOperationTreeAndDiagnosticsForTest<AssignmentExpressionSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }

        [Fact]
        public void CoalesceAssignment_NoConversion()
        {
            string source = @"
class C
{
    static void M(C c1, string s1)
    {
        /*<bind>*/c1 ??= s1/*</bind>*/;
    }
}
";
            string expectedOperationTree = @"
ICoalesceAssignmentOperation (OperationKind.CoalesceAssignment, Type: ?, IsInvalid) (Syntax: 'c1 ??= s1')
  Target: 
    IParameterReferenceOperation: c1 (OperationKind.ParameterReference, Type: C, IsInvalid) (Syntax: 'c1')
  Value: 
    IParameterReferenceOperation: s1 (OperationKind.ParameterReference, Type: System.String, IsInvalid) (Syntax: 's1')
";
            var expectedDiagnostics = new DiagnosticDescription[] {
                // file.cs(6,19): error CS0019: Operator '??=' cannot be applied to operands of type 'C' and 'string'
                //         /*<bind>*/c1 ??= s1/*</bind>*/;
                Diagnostic(ErrorCode.ERR_BadBinaryOps, "c1 ??= s1").WithArguments("??=", "C", "string").WithLocation(6, 19)
            };

            VerifyOperationTreeAndDiagnosticsForTest<AssignmentExpressionSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }

        [Fact]
        public void CoalesceAssignment_ValueTypeLeft()
        {
            string source = @"
class C
{
    static void M(int i1, string s1)
    {
        /*<bind>*/i1 ??= s1/*</bind>*/;
    }
}
";
            string expectedOperationTree = @"
ICoalesceAssignmentOperation (OperationKind.CoalesceAssignment, Type: ?, IsInvalid) (Syntax: 'i1 ??= s1')
  Target: 
    IParameterReferenceOperation: i1 (OperationKind.ParameterReference, Type: System.Int32, IsInvalid) (Syntax: 'i1')
  Value: 
    IParameterReferenceOperation: s1 (OperationKind.ParameterReference, Type: System.String, IsInvalid) (Syntax: 's1')
";
            var expectedDiagnostics = new DiagnosticDescription[] {
                // file.cs(6,19): error CS0019: Operator '??=' cannot be applied to operands of type 'int' and 'string'
                //         /*<bind>*/i1 ??= s1/*</bind>*/;
                Diagnostic(ErrorCode.ERR_BadBinaryOps, "i1 ??= s1").WithArguments("??=", "int", "string").WithLocation(6, 19)
            };

            VerifyOperationTreeAndDiagnosticsForTest<AssignmentExpressionSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }

        [Fact]
        public void CoalesceAssignment_MissingLeftAndRight()
        {

            string source = @"
class C
{
    static void M()
    {
        /*<bind>*/o1 ??= o2/*</bind>*/;
    }
}
";
            string expectedOperationTree = @"
ICoalesceAssignmentOperation (OperationKind.CoalesceAssignment, Type: ?, IsInvalid) (Syntax: 'o1 ??= o2')
  Target: 
    IInvalidOperation (OperationKind.Invalid, Type: ?, IsInvalid) (Syntax: 'o1')
      Children(0)
  Value: 
    IInvalidOperation (OperationKind.Invalid, Type: ?, IsInvalid) (Syntax: 'o2')
      Children(0)
";
            var expectedDiagnostics = new DiagnosticDescription[] {
                // file.cs(6,19): error CS0103: The name 'o1' does not exist in the current context
                //         /*<bind>*/o1 ??= o2/*</bind>*/;
                Diagnostic(ErrorCode.ERR_NameNotInContext, "o1").WithArguments("o1").WithLocation(6, 19),
                // file.cs(6,26): error CS0103: The name 'o2' does not exist in the current context
                //         /*<bind>*/o1 ??= o2/*</bind>*/;
                Diagnostic(ErrorCode.ERR_NameNotInContext, "o2").WithArguments("o2").WithLocation(6, 26)
            };

            VerifyOperationTreeAndDiagnosticsForTest<AssignmentExpressionSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }

        [Fact]
        public void CoalesceAssignment_AsExpression()
        {

            string source = @"
class C
{
    static void M(object o1, object o2)
    {
        /*<bind>*/M2(o1 ??= o2)/*</bind>*/;
    }
    static void M2(object o) {}
}
";
            string expectedOperationTree = @"
IInvocationOperation (void C.M2(System.Object o)) (OperationKind.Invocation, Type: System.Void) (Syntax: 'M2(o1 ??= o2)')
  Instance Receiver: 
    null
  Arguments(1):
      IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: o) (OperationKind.Argument, Type: null) (Syntax: 'o1 ??= o2')
        ICoalesceAssignmentOperation (OperationKind.CoalesceAssignment, Type: System.Object) (Syntax: 'o1 ??= o2')
          Target: 
            IParameterReferenceOperation: o1 (OperationKind.ParameterReference, Type: System.Object) (Syntax: 'o1')
          Value: 
            IParameterReferenceOperation: o2 (OperationKind.ParameterReference, Type: System.Object) (Syntax: 'o2')
        InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
        OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
";
            var expectedDiagnostics = DiagnosticDescription.None;

            VerifyOperationTreeAndDiagnosticsForTest<InvocationExpressionSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }

        [Fact]
        public void CoalesceAssignment_CheckedDynamic()
        {

            string source = @"
class C
{
    static void M(dynamic d1, dynamic d2)
    {
        checked
        {
            /*<bind>*/d1 ??= d2/*</bind>*/;
        }
    }
}
";
            string expectedOperationTree = @"
ICoalesceAssignmentOperation (OperationKind.CoalesceAssignment, Type: dynamic) (Syntax: 'd1 ??= d2')
  Target: 
    IParameterReferenceOperation: d1 (OperationKind.ParameterReference, Type: dynamic) (Syntax: 'd1')
  Value: 
    IParameterReferenceOperation: d2 (OperationKind.ParameterReference, Type: dynamic) (Syntax: 'd2')
";
            var expectedDiagnostics = DiagnosticDescription.None;

            VerifyOperationTreeAndDiagnosticsForTest<AssignmentExpressionSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }

        [Fact]
        public void CoalesceAssignment_NullValueAndTarget()
        {
            var comp = CreateCompilation(@"
class C
{
    static void M()
    {
        /*<bind>*/??=/*</bind>*/;
    }
}
");
            string expectedOperationTree = @"
ICoalesceAssignmentOperation (OperationKind.CoalesceAssignment, Type: ?, IsInvalid) (Syntax: '/*<bind>*/? ... /*</bind>*/')
  Target: 
    IInvalidOperation (OperationKind.Invalid, Type: null) (Syntax: '')
      Children(0)
  Value: 
    IInvalidOperation (OperationKind.Invalid, Type: null, IsInvalid) (Syntax: '')
      Children(0)
";
            var expectedDiagnostics = new DiagnosticDescription[] {
                // file.cs(5,6): error CS1525: Invalid expression term '??='
                //     {
                Diagnostic(ErrorCode.ERR_InvalidExprTerm, "").WithArguments("??=").WithLocation(5, 6),
                // file.cs(6,33): error CS1525: Invalid expression term ';'
                //         /*<bind>*/??=/*</bind>*/;
                Diagnostic(ErrorCode.ERR_InvalidExprTerm, ";").WithArguments(";").WithLocation(6, 33)
            };

            VerifyOperationTreeAndDiagnosticsForTest<AssignmentExpressionSyntax>(comp, expectedOperationTree, expectedDiagnostics);

            var tree = comp.SyntaxTrees[0];
            var m = tree.GetRoot().DescendantNodes().OfType<MethodDeclarationSyntax>().Single();
            VerifyFlowGraph(comp, m, @"
Block[B0] - Entry
    Statements (0)
    Next (Regular) Block[B1]
        Entering: {R1}
.locals {R1}
{
    CaptureIds: [0]
    Block[B1] - Block
        Predecessors: [B0]
        Statements (1)
            IFlowCaptureOperation: 0 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: '')
              Value: 
                IInvalidOperation (OperationKind.Invalid, Type: null) (Syntax: '')
                  Children(0)
        Next (Regular) Block[B2]
            Entering: {R2}
    .locals {R2}
    {
        CaptureIds: [1]
        Block[B2] - Block
            Predecessors: [B1]
            Statements (1)
                IFlowCaptureOperation: 1 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: '')
                  Value: 
                    IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: null, IsImplicit) (Syntax: '')
            Jump if True (Regular) to Block[B3]
                IIsNullOperation (OperationKind.IsNull, Type: System.Boolean, IsImplicit) (Syntax: '')
                  Operand: 
                    IFlowCaptureReferenceOperation: 1 (OperationKind.FlowCaptureReference, Type: null, IsImplicit) (Syntax: '')
                Leaving: {R2}
            Next (Regular) Block[B4]
                Leaving: {R2} {R1}
    }
    Block[B3] - Block
        Predecessors: [B2]
        Statements (1)
            ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: ?, IsInvalid, IsImplicit) (Syntax: '/*<bind>*/? ... /*</bind>*/')
              Left: 
                IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: null, IsImplicit) (Syntax: '')
              Right: 
                IInvalidOperation (OperationKind.Invalid, Type: null, IsInvalid) (Syntax: '')
                  Children(0)
        Next (Regular) Block[B4]
            Leaving: {R1}
}
Block[B4] - Exit
    Predecessors: [B2] [B3]
    Statements (0)
");
        }

        [Fact, CompilerTrait(CompilerFeature.Dataflow)]
        public void CoalesceAssignmentFlow_NullableValueTypeTarget()
        {
            var comp = CreateCompilation(@"
class C
{
    static void M(int? i1, int i2)
    /*<bind>*/{
        i1 ??= i2;
    }/*</bind>*/
}
");

            VerifyOperationTreeAndDiagnosticsForTest<BlockSyntax>(comp, @"
IBlockOperation (1 statements) (OperationKind.Block, Type: null) (Syntax: '{ ... }')
  IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'i1 ??= i2;')
    Expression: 
      ICoalesceAssignmentOperation (OperationKind.CoalesceAssignment, Type: System.Int32) (Syntax: 'i1 ??= i2')
        Target: 
          IParameterReferenceOperation: i1 (OperationKind.ParameterReference, Type: System.Int32?) (Syntax: 'i1')
        Value: 
          IParameterReferenceOperation: i2 (OperationKind.ParameterReference, Type: System.Int32) (Syntax: 'i2')
", expectedDiagnostics: DiagnosticDescription.None);

            string expectedFlowGraph = @"
Block[B0] - Entry
    Statements (0)
    Next (Regular) Block[B1]
        Entering: {R1}
.locals {R1}
{
    CaptureIds: [0] [1]
    Block[B1] - Block
        Predecessors: [B0]
        Statements (2)
            IFlowCaptureOperation: 0 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'i1')
              Value: 
                IParameterReferenceOperation: i1 (OperationKind.ParameterReference, Type: System.Int32?) (Syntax: 'i1')
            IFlowCaptureOperation: 1 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'i1')
              Value: 
                IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: System.Int32?, IsImplicit) (Syntax: 'i1')
        Jump if False (Regular) to Block[B2]
            IInvocationOperation ( System.Boolean System.Int32?.HasValue.get) (OperationKind.Invocation, Type: System.Boolean, IsImplicit) (Syntax: 'i1')
              Instance Receiver: 
                IFlowCaptureReferenceOperation: 1 (OperationKind.FlowCaptureReference, Type: System.Int32?, IsImplicit) (Syntax: 'i1')
              Arguments(0)
        Next (Regular) Block[B3]
            Leaving: {R1}
    Block[B2] - Block
        Predecessors: [B1]
        Statements (1)
            ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Int32?, IsImplicit) (Syntax: 'i1 ??= i2')
              Left: 
                IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: System.Int32?, IsImplicit) (Syntax: 'i1')
              Right: 
                IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.Int32?, IsImplicit) (Syntax: 'i2')
                  Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                    (ImplicitNullable)
                  Operand: 
                    IParameterReferenceOperation: i2 (OperationKind.ParameterReference, Type: System.Int32) (Syntax: 'i2')
        Next (Regular) Block[B3]
            Leaving: {R1}
}
Block[B3] - Exit
    Predecessors: [B1] [B2]
    Statements (0)";

            VerifyFlowGraphForTest<BlockSyntax>(comp, expectedFlowGraph);
        }

        [Fact, CompilerTrait(CompilerFeature.Dataflow)]
        public void CoalesceAssignmentFlow_WhenNullConversion()
        {
            string source = @"
class C
{
    static void M(object o1, string s1)
    /*<bind>*/{
        o1 ??= s1;
    }/*</bind>*/
}
";
            string expectedFlowGraph = @"
Block[B0] - Entry
    Statements (0)
    Next (Regular) Block[B1]
        Entering: {R1}

.locals {R1}
{
    CaptureIds: [0]
    Block[B1] - Block
        Predecessors: [B0]
        Statements (1)
            IFlowCaptureOperation: 0 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'o1')
              Value: 
                IParameterReferenceOperation: o1 (OperationKind.ParameterReference, Type: System.Object) (Syntax: 'o1')

        Next (Regular) Block[B2]
            Entering: {R2}

    .locals {R2}
    {
        CaptureIds: [1]
        Block[B2] - Block
            Predecessors: [B1]
            Statements (1)
                IFlowCaptureOperation: 1 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'o1')
                  Value: 
                    IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: System.Object, IsImplicit) (Syntax: 'o1')

            Jump if True (Regular) to Block[B3]
                IIsNullOperation (OperationKind.IsNull, Type: System.Boolean, IsImplicit) (Syntax: 'o1')
                  Operand: 
                    IFlowCaptureReferenceOperation: 1 (OperationKind.FlowCaptureReference, Type: System.Object, IsImplicit) (Syntax: 'o1')
                Leaving: {R2}

            Next (Regular) Block[B4]
                Leaving: {R2} {R1}
    }

    Block[B3] - Block
        Predecessors: [B2]
        Statements (1)
            ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Object, IsImplicit) (Syntax: 'o1 ??= s1')
              Left: 
                IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: System.Object, IsImplicit) (Syntax: 'o1')
              Right: 
                IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.Object, IsImplicit) (Syntax: 's1')
                  Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: True, IsUserDefined: False) (MethodSymbol: null)
                    (ImplicitReference)
                  Operand: 
                    IParameterReferenceOperation: s1 (OperationKind.ParameterReference, Type: System.String) (Syntax: 's1')

        Next (Regular) Block[B4]
            Leaving: {R1}
}

Block[B4] - Exit
    Predecessors: [B2] [B3]
    Statements (0)
";
            var expectedDiagnostics = DiagnosticDescription.None;

            VerifyFlowGraphAndDiagnosticsForTest<BlockSyntax>(source, expectedFlowGraph, expectedDiagnostics);
        }

        [Fact, CompilerTrait(CompilerFeature.Dataflow)]
        public void CoalesceAssignmentFlow_WhenNullHasFlow()
        {
            string source = @"
class C
{
    static void M(object o1, string s1, string s2)
    /*<bind>*/{
        o1 ??= s1 ?? s2;
    }/*</bind>*/
}
";
            string expectedFlowGraph = @"
Block[B0] - Entry
    Statements (0)
    Next (Regular) Block[B1]
        Entering: {R1}

.locals {R1}
{
    CaptureIds: [0]
    Block[B1] - Block
        Predecessors: [B0]
        Statements (1)
            IFlowCaptureOperation: 0 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'o1')
              Value: 
                IParameterReferenceOperation: o1 (OperationKind.ParameterReference, Type: System.Object) (Syntax: 'o1')

        Next (Regular) Block[B2]
            Entering: {R2}

    .locals {R2}
    {
        CaptureIds: [1]
        Block[B2] - Block
            Predecessors: [B1]
            Statements (1)
                IFlowCaptureOperation: 1 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'o1')
                  Value: 
                    IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: System.Object, IsImplicit) (Syntax: 'o1')

            Jump if True (Regular) to Block[B3]
                IIsNullOperation (OperationKind.IsNull, Type: System.Boolean, IsImplicit) (Syntax: 'o1')
                  Operand: 
                    IFlowCaptureReferenceOperation: 1 (OperationKind.FlowCaptureReference, Type: System.Object, IsImplicit) (Syntax: 'o1')
                Leaving: {R2}
                Entering: {R3} {R4}

            Next (Regular) Block[B7]
                Leaving: {R2} {R1}
    }
    .locals {R3}
    {
        CaptureIds: [3]
        .locals {R4}
        {
            CaptureIds: [2]
            Block[B3] - Block
                Predecessors: [B2]
                Statements (1)
                    IFlowCaptureOperation: 2 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 's1')
                      Value: 
                        IParameterReferenceOperation: s1 (OperationKind.ParameterReference, Type: System.String) (Syntax: 's1')

                Jump if True (Regular) to Block[B5]
                    IIsNullOperation (OperationKind.IsNull, Type: System.Boolean, IsImplicit) (Syntax: 's1')
                      Operand: 
                        IFlowCaptureReferenceOperation: 2 (OperationKind.FlowCaptureReference, Type: System.String, IsImplicit) (Syntax: 's1')
                    Leaving: {R4}

                Next (Regular) Block[B4]
            Block[B4] - Block
                Predecessors: [B3]
                Statements (1)
                    IFlowCaptureOperation: 3 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 's1')
                      Value: 
                        IFlowCaptureReferenceOperation: 2 (OperationKind.FlowCaptureReference, Type: System.String, IsImplicit) (Syntax: 's1')

                Next (Regular) Block[B6]
                    Leaving: {R4}
        }

        Block[B5] - Block
            Predecessors: [B3]
            Statements (1)
                IFlowCaptureOperation: 3 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 's2')
                  Value: 
                    IParameterReferenceOperation: s2 (OperationKind.ParameterReference, Type: System.String) (Syntax: 's2')

            Next (Regular) Block[B6]
        Block[B6] - Block
            Predecessors: [B4] [B5]
            Statements (1)
                ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Object, IsImplicit) (Syntax: 'o1 ??= s1 ?? s2')
                  Left: 
                    IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: System.Object, IsImplicit) (Syntax: 'o1')
                  Right: 
                    IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.Object, IsImplicit) (Syntax: 's1 ?? s2')
                      Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: True, IsUserDefined: False) (MethodSymbol: null)
                        (ImplicitReference)
                      Operand: 
                        IFlowCaptureReferenceOperation: 3 (OperationKind.FlowCaptureReference, Type: System.String, IsImplicit) (Syntax: 's1 ?? s2')

            Next (Regular) Block[B7]
                Leaving: {R3} {R1}
    }
}

Block[B7] - Exit
    Predecessors: [B2] [B6]
    Statements (0)
";
            var expectedDiagnostics = DiagnosticDescription.None;

            VerifyFlowGraphAndDiagnosticsForTest<BlockSyntax>(source, expectedFlowGraph, expectedDiagnostics);
        }

        [Fact, CompilerTrait(CompilerFeature.Dataflow)]
        public void CoalesceAssignmentFlow_BothSidesHaveFlow()
        {
            string source = @"
class C
{
    static void M(object o1, object o2, string s1, string s2)
    /*<bind>*/{
        (o1 ?? o2) ??= (s1 ?? s2);
    }/*</bind>*/
}
";
            string expectedFlowGraph = @"
Block[B0] - Entry
    Statements (0)
    Next (Regular) Block[B1]
        Entering: {R1} {R2} {R3}

.locals {R1}
{
    CaptureIds: [2]
    .locals {R2}
    {
        CaptureIds: [1]
        .locals {R3}
        {
            CaptureIds: [0]
            Block[B1] - Block
                Predecessors: [B0]
                Statements (1)
                    IFlowCaptureOperation: 0 (OperationKind.FlowCapture, Type: null, IsInvalid, IsImplicit) (Syntax: 'o1')
                      Value:
                        IParameterReferenceOperation: o1 (OperationKind.ParameterReference, Type: System.Object, IsInvalid) (Syntax: 'o1')

                Jump if True (Regular) to Block[B3]
                    IIsNullOperation (OperationKind.IsNull, Type: System.Boolean, IsInvalid, IsImplicit) (Syntax: 'o1')
                      Operand:
                        IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: System.Object, IsInvalid, IsImplicit) (Syntax: 'o1')
                    Leaving: {R3}

                Next (Regular) Block[B2]
            Block[B2] - Block
                Predecessors: [B1]
                Statements (1)
                    IFlowCaptureOperation: 1 (OperationKind.FlowCapture, Type: null, IsInvalid, IsImplicit) (Syntax: 'o1')
                      Value:
                        IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: System.Object, IsInvalid, IsImplicit) (Syntax: 'o1')

                Next (Regular) Block[B4]
                    Leaving: {R3}
        }

        Block[B3] - Block
            Predecessors: [B1]
            Statements (1)
                IFlowCaptureOperation: 1 (OperationKind.FlowCapture, Type: null, IsInvalid, IsImplicit) (Syntax: 'o2')
                  Value:
                    IParameterReferenceOperation: o2 (OperationKind.ParameterReference, Type: System.Object, IsInvalid) (Syntax: 'o2')

            Next (Regular) Block[B4]
        Block[B4] - Block
            Predecessors: [B2] [B3]
            Statements (1)
                IFlowCaptureOperation: 2 (OperationKind.FlowCapture, Type: null, IsInvalid, IsImplicit) (Syntax: 'o1 ?? o2')
                  Value:
                    IInvalidOperation (OperationKind.Invalid, Type: System.Object, IsInvalid, IsImplicit) (Syntax: 'o1 ?? o2')
                      Children(1):
                          IFlowCaptureReferenceOperation: 1 (OperationKind.FlowCaptureReference, Type: System.Object, IsInvalid, IsImplicit) (Syntax: 'o1 ?? o2')

            Next (Regular) Block[B5]
                Leaving: {R2}
                Entering: {R4}
    }
    .locals {R4}
    {
        CaptureIds: [3]
        Block[B5] - Block
            Predecessors: [B4]
            Statements (1)
                IFlowCaptureOperation: 3 (OperationKind.FlowCapture, Type: null, IsInvalid, IsImplicit) (Syntax: 'o1 ?? o2')
                  Value:
                    IFlowCaptureReferenceOperation: 2 (OperationKind.FlowCaptureReference, Type: System.Object, IsInvalid, IsImplicit) (Syntax: 'o1 ?? o2')

            Jump if True (Regular) to Block[B6]
                IIsNullOperation (OperationKind.IsNull, Type: System.Boolean, IsInvalid, IsImplicit) (Syntax: 'o1 ?? o2')
                  Operand:
                    IFlowCaptureReferenceOperation: 3 (OperationKind.FlowCaptureReference, Type: System.Object, IsInvalid, IsImplicit) (Syntax: 'o1 ?? o2')
                Leaving: {R4}
                Entering: {R5} {R6}

            Next (Regular) Block[B10]
                Leaving: {R4} {R1}
    }
    .locals {R5}
    {
        CaptureIds: [5]
        .locals {R6}
        {
            CaptureIds: [4]
            Block[B6] - Block
                Predecessors: [B5]
                Statements (1)
                    IFlowCaptureOperation: 4 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 's1')
                      Value:
                        IParameterReferenceOperation: s1 (OperationKind.ParameterReference, Type: System.String) (Syntax: 's1')

                Jump if True (Regular) to Block[B8]
                    IIsNullOperation (OperationKind.IsNull, Type: System.Boolean, IsImplicit) (Syntax: 's1')
                      Operand:
                        IFlowCaptureReferenceOperation: 4 (OperationKind.FlowCaptureReference, Type: System.String, IsImplicit) (Syntax: 's1')
                    Leaving: {R6}

                Next (Regular) Block[B7]
            Block[B7] - Block
                Predecessors: [B6]
                Statements (1)
                    IFlowCaptureOperation: 5 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 's1')
                      Value:
                        IFlowCaptureReferenceOperation: 4 (OperationKind.FlowCaptureReference, Type: System.String, IsImplicit) (Syntax: 's1')

                Next (Regular) Block[B9]
                    Leaving: {R6}
        }

        Block[B8] - Block
            Predecessors: [B6]
            Statements (1)
                IFlowCaptureOperation: 5 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 's2')
                  Value:
                    IParameterReferenceOperation: s2 (OperationKind.ParameterReference, Type: System.String) (Syntax: 's2')

            Next (Regular) Block[B9]
        Block[B9] - Block
            Predecessors: [B7] [B8]
            Statements (1)
                ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Object, IsInvalid, IsImplicit) (Syntax: '(o1 ?? o2)  ...  (s1 ?? s2)')
                  Left:
                    IFlowCaptureReferenceOperation: 2 (OperationKind.FlowCaptureReference, Type: System.Object, IsInvalid, IsImplicit) (Syntax: 'o1 ?? o2')
                  Right:
                    IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.Object, IsImplicit) (Syntax: 's1 ?? s2')
                      Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: True, IsUserDefined: False) (MethodSymbol: null)
                        (ImplicitReference)
                      Operand:
                        IFlowCaptureReferenceOperation: 5 (OperationKind.FlowCaptureReference, Type: System.String, IsImplicit) (Syntax: 's1 ?? s2')

            Next (Regular) Block[B10]
                Leaving: {R5} {R1}
    }
}

Block[B10] - Exit
    Predecessors: [B5] [B9]
    Statements (0)
";
            var expectedDiagnostics = new DiagnosticDescription[] {
                // file.cs(6,10): error CS0131: The left-hand side of an assignment must be a variable, property or indexer
                //         (o1 ?? o2) ??= (s1 ?? s2);
                Diagnostic(ErrorCode.ERR_AssgLvalueExpected, "o1 ?? o2").WithLocation(6, 10)
            };

            VerifyFlowGraphAndDiagnosticsForTest<BlockSyntax>(source, expectedFlowGraph, expectedDiagnostics);
        }

        [Fact, CompilerTrait(CompilerFeature.Dataflow)]
        public void CoalesceAssignmentFlow_NestedUse()
        {
            string source = @"
class C
{
    static void M(object o1, object o2, object o3)
    /*<bind>*/{
        o1 ??= (o2 ??= o3);
    }/*</bind>*/
}
";
            string expectedFlowGraph = @"
Block[B0] - Entry
    Statements (0)
    Next (Regular) Block[B1]
        Entering: {R1}

.locals {R1}
{
    CaptureIds: [0]
    Block[B1] - Block
        Predecessors: [B0]
        Statements (1)
            IFlowCaptureOperation: 0 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'o1')
              Value: 
                IParameterReferenceOperation: o1 (OperationKind.ParameterReference, Type: System.Object) (Syntax: 'o1')

        Next (Regular) Block[B2]
            Entering: {R2}

    .locals {R2}
    {
        CaptureIds: [1]
        Block[B2] - Block
            Predecessors: [B1]
            Statements (1)
                IFlowCaptureOperation: 1 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'o1')
                  Value: 
                    IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: System.Object, IsImplicit) (Syntax: 'o1')

            Jump if True (Regular) to Block[B3]
                IIsNullOperation (OperationKind.IsNull, Type: System.Boolean, IsImplicit) (Syntax: 'o1')
                  Operand: 
                    IFlowCaptureReferenceOperation: 1 (OperationKind.FlowCaptureReference, Type: System.Object, IsImplicit) (Syntax: 'o1')
                Leaving: {R2}
                Entering: {R3} {R4}

            Next (Regular) Block[B8]
                Leaving: {R2} {R1}
    }
    .locals {R3}
    {
        CaptureIds: [4]
        .locals {R4}
        {
            CaptureIds: [2]
            Block[B3] - Block
                Predecessors: [B2]
                Statements (1)
                    IFlowCaptureOperation: 2 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'o2')
                      Value: 
                        IParameterReferenceOperation: o2 (OperationKind.ParameterReference, Type: System.Object) (Syntax: 'o2')

                Next (Regular) Block[B4]
                    Entering: {R5}

            .locals {R5}
            {
                CaptureIds: [3]
                Block[B4] - Block
                    Predecessors: [B3]
                    Statements (1)
                        IFlowCaptureOperation: 3 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'o2')
                          Value: 
                            IFlowCaptureReferenceOperation: 2 (OperationKind.FlowCaptureReference, Type: System.Object, IsImplicit) (Syntax: 'o2')

                    Jump if True (Regular) to Block[B6]
                        IIsNullOperation (OperationKind.IsNull, Type: System.Boolean, IsImplicit) (Syntax: 'o2')
                          Operand: 
                            IFlowCaptureReferenceOperation: 3 (OperationKind.FlowCaptureReference, Type: System.Object, IsImplicit) (Syntax: 'o2')
                        Leaving: {R5}

                    Next (Regular) Block[B5]
                Block[B5] - Block
                    Predecessors: [B4]
                    Statements (1)
                        IFlowCaptureOperation: 4 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'o2 ??= o3')
                          Value: 
                            IFlowCaptureReferenceOperation: 3 (OperationKind.FlowCaptureReference, Type: System.Object, IsImplicit) (Syntax: 'o2')

                    Next (Regular) Block[B7]
                        Leaving: {R5} {R4}
            }

            Block[B6] - Block
                Predecessors: [B4]
                Statements (1)
                    IFlowCaptureOperation: 4 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'o2 ??= o3')
                      Value: 
                        ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Object, IsImplicit) (Syntax: 'o2 ??= o3')
                          Left: 
                            IFlowCaptureReferenceOperation: 2 (OperationKind.FlowCaptureReference, Type: System.Object, IsImplicit) (Syntax: 'o2')
                          Right: 
                            IParameterReferenceOperation: o3 (OperationKind.ParameterReference, Type: System.Object) (Syntax: 'o3')

                Next (Regular) Block[B7]
                    Leaving: {R4}
        }

        Block[B7] - Block
            Predecessors: [B5] [B6]
            Statements (1)
                ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Object, IsImplicit) (Syntax: 'o1 ??= (o2 ??= o3)')
                  Left: 
                    IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: System.Object, IsImplicit) (Syntax: 'o1')
                  Right: 
                    IFlowCaptureReferenceOperation: 4 (OperationKind.FlowCaptureReference, Type: System.Object, IsImplicit) (Syntax: 'o2 ??= o3')

            Next (Regular) Block[B8]
                Leaving: {R3} {R1}
    }
}

Block[B8] - Exit
    Predecessors: [B2] [B7]
    Statements (0)
";
            var expectedDiagnostics = DiagnosticDescription.None;

            VerifyFlowGraphAndDiagnosticsForTest<BlockSyntax>(source, expectedFlowGraph, expectedDiagnostics);
        }

        [Fact]
        public void CoalesceAssignmentOperation_PropertyAssignment_ReferenceTypes()
        {
            var source = @"
class C
{
    object Prop { get; set; }
    void M(C c)
    /*<bind>*/{
        c.Prop ??= null;
    }/*</bind>*/
}";

            var expectedFlowGraph = @"
Block[B0] - Entry
    Statements (0)
    Next (Regular) Block[B1]
        Entering: {R1}
.locals {R1}
{
    CaptureIds: [0]
    Block[B1] - Block
        Predecessors: [B0]
        Statements (1)
            IFlowCaptureOperation: 0 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'c.Prop')
              Value: 
                IPropertyReferenceOperation: System.Object C.Prop { get; set; } (OperationKind.PropertyReference, Type: System.Object) (Syntax: 'c.Prop')
                  Instance Receiver: 
                    IParameterReferenceOperation: c (OperationKind.ParameterReference, Type: C) (Syntax: 'c')
        Next (Regular) Block[B2]
            Entering: {R2}
    .locals {R2}
    {
        CaptureIds: [1]
        Block[B2] - Block
            Predecessors: [B1]
            Statements (1)
                IFlowCaptureOperation: 1 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'c.Prop')
                  Value: 
                    IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: System.Object, IsImplicit) (Syntax: 'c.Prop')
            Jump if True (Regular) to Block[B3]
                IIsNullOperation (OperationKind.IsNull, Type: System.Boolean, IsImplicit) (Syntax: 'c.Prop')
                  Operand: 
                    IFlowCaptureReferenceOperation: 1 (OperationKind.FlowCaptureReference, Type: System.Object, IsImplicit) (Syntax: 'c.Prop')
                Leaving: {R2}
            Next (Regular) Block[B4]
                Leaving: {R2} {R1}
    }
    Block[B3] - Block
        Predecessors: [B2]
        Statements (1)
            ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Object, IsImplicit) (Syntax: 'c.Prop ??= null')
              Left: 
                IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: System.Object, IsImplicit) (Syntax: 'c.Prop')
              Right: 
                IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.Object, Constant: null, IsImplicit) (Syntax: 'null')
                  Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: True, IsUserDefined: False) (MethodSymbol: null)
                    (ImplicitReference)
                  Operand: 
                    ILiteralOperation (OperationKind.Literal, Type: null, Constant: null) (Syntax: 'null')
        Next (Regular) Block[B4]
            Leaving: {R1}
}
Block[B4] - Exit
    Predecessors: [B2] [B3]
    Statements (0)
";

            var expectedDiagnostics = DiagnosticDescription.None;

            VerifyFlowGraphAndDiagnosticsForTest<BlockSyntax>(source, expectedFlowGraph, expectedDiagnostics);
        }

        [Fact]
        public void CoalesceAssignmentOperation_PropertyAssignment_NullableValueTypes()
        {
            var source = @"
using System;
class C
{
    int? Prop { get; set; }
    void M(C c)
    /*<bind>*/{
        Console.WriteLine(c.Prop ??= 1);
    }/*</bind>*/
}";

            var expectedFlowGraph = @"
Block[B0] - Entry
    Statements (0)
    Next (Regular) Block[B1]
        Entering: {R1} {R2}
.locals {R1}
{
    CaptureIds: [2]
    .locals {R2}
    {
        CaptureIds: [0] [1] [3]
        Block[B1] - Block
            Predecessors: [B0]
            Statements (3)
                IFlowCaptureOperation: 0 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'c.Prop')
                  Value: 
                    IPropertyReferenceOperation: System.Int32? C.Prop { get; set; } (OperationKind.PropertyReference, Type: System.Int32?) (Syntax: 'c.Prop')
                      Instance Receiver: 
                        IParameterReferenceOperation: c (OperationKind.ParameterReference, Type: C) (Syntax: 'c')
                IFlowCaptureOperation: 1 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'c.Prop')
                  Value: 
                    IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: System.Int32?, IsImplicit) (Syntax: 'c.Prop')
                IFlowCaptureOperation: 3 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'c.Prop')
                  Value: 
                    IInvocationOperation ( System.Int32 System.Int32?.GetValueOrDefault()) (OperationKind.Invocation, Type: System.Int32, IsImplicit) (Syntax: 'c.Prop')
                      Instance Receiver: 
                        IFlowCaptureReferenceOperation: 1 (OperationKind.FlowCaptureReference, Type: System.Int32?, IsImplicit) (Syntax: 'c.Prop')
                      Arguments(0)
            Jump if False (Regular) to Block[B3]
                IInvocationOperation ( System.Boolean System.Int32?.HasValue.get) (OperationKind.Invocation, Type: System.Boolean, IsImplicit) (Syntax: 'c.Prop')
                  Instance Receiver: 
                    IFlowCaptureReferenceOperation: 1 (OperationKind.FlowCaptureReference, Type: System.Int32?, IsImplicit) (Syntax: 'c.Prop')
                  Arguments(0)
                Entering: {R3}
            Next (Regular) Block[B2]
        Block[B2] - Block
            Predecessors: [B1]
            Statements (1)
                IFlowCaptureOperation: 2 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'c.Prop ??= 1')
                  Value: 
                    IFlowCaptureReferenceOperation: 3 (OperationKind.FlowCaptureReference, Type: System.Int32?, IsImplicit) (Syntax: 'c.Prop')
            Next (Regular) Block[B4]
                Leaving: {R2}
        .locals {R3}
        {
            CaptureIds: [4]
            Block[B3] - Block
                Predecessors: [B1]
                Statements (3)
                    IFlowCaptureOperation: 4 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: '1')
                      Value: 
                        ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 1) (Syntax: '1')
                    IFlowCaptureOperation: 2 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'c.Prop ??= 1')
                      Value: 
                        IFlowCaptureReferenceOperation: 4 (OperationKind.FlowCaptureReference, Type: System.Int32, Constant: 1, IsImplicit) (Syntax: '1')
                    ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Int32?, IsImplicit) (Syntax: 'c.Prop ??= 1')
                      Left: 
                        IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: System.Int32?, IsImplicit) (Syntax: 'c.Prop')
                      Right: 
                        IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.Int32?, IsImplicit) (Syntax: '1')
                          Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                            (ImplicitNullable)
                          Operand: 
                            IFlowCaptureReferenceOperation: 4 (OperationKind.FlowCaptureReference, Type: System.Int32, Constant: 1, IsImplicit) (Syntax: '1')
                Next (Regular) Block[B4]
                    Leaving: {R3} {R2}
        }
    }
    Block[B4] - Block
        Predecessors: [B2] [B3]
        Statements (1)
            IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'Console.Wri ... rop ??= 1);')
              Expression: 
                IInvocationOperation (void System.Console.WriteLine(System.Int32 value)) (OperationKind.Invocation, Type: System.Void) (Syntax: 'Console.Wri ... Prop ??= 1)')
                  Instance Receiver: 
                    null
                  Arguments(1):
                      IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: value) (OperationKind.Argument, Type: null) (Syntax: 'c.Prop ??= 1')
                        IFlowCaptureReferenceOperation: 2 (OperationKind.FlowCaptureReference, Type: System.Int32, IsImplicit) (Syntax: 'c.Prop ??= 1')
                        InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                        OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
        Next (Regular) Block[B5]
            Leaving: {R1}
}
Block[B5] - Exit
    Predecessors: [B4]
    Statements (0)
";

            var expectedDiagnostics = DiagnosticDescription.None;

            VerifyFlowGraphAndDiagnosticsForTest<BlockSyntax>(source, expectedFlowGraph, expectedDiagnostics);
        }
    }
}
