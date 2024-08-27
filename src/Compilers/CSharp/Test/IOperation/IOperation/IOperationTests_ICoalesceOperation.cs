// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System.Linq;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.CSharp.Test.Utilities;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.CSharp.UnitTests
{
    public class IOperationTests_ICoalesceOperation : SemanticModelTestBase
    {
        [CompilerTrait(CompilerFeature.IOperation, CompilerFeature.Dataflow)]
        [Fact]
        public void CoalesceOperation_01()
        {
            var source = @"
class C
{
    void F(int? input, int alternative, int result)
    /*<bind>*/{
        result = input ?? alternative;
    }/*</bind>*/
}";

            var compilation = CreateCompilation(source);

            compilation.VerifyDiagnostics();

            var tree = compilation.SyntaxTrees.Single();
            var node = tree.GetRoot().DescendantNodes().OfType<BinaryExpressionSyntax>().Single();

            compilation.VerifyOperationTree(node, expectedOperationTree:
@"
ICoalesceOperation (OperationKind.Coalesce, Type: System.Int32) (Syntax: 'input ?? alternative')
  Expression: 
    IParameterReferenceOperation: input (OperationKind.ParameterReference, Type: System.Int32?) (Syntax: 'input')
  ValueConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
    (Identity)
  WhenNull: 
    IParameterReferenceOperation: alternative (OperationKind.ParameterReference, Type: System.Int32) (Syntax: 'alternative')
");

            string expectedGraph = @"
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
                IParameterReferenceOperation: result (OperationKind.ParameterReference, Type: System.Int32) (Syntax: 'result')

        Next (Regular) Block[B2]
            Entering: {R2}

    .locals {R2}
    {
        CaptureIds: [1]
        Block[B2] - Block
            Predecessors: [B1]
            Statements (1)
                IFlowCaptureOperation: 1 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'input')
                  Value: 
                    IParameterReferenceOperation: input (OperationKind.ParameterReference, Type: System.Int32?) (Syntax: 'input')

            Jump if True (Regular) to Block[B4]
                IIsNullOperation (OperationKind.IsNull, Type: System.Boolean, IsImplicit) (Syntax: 'input')
                  Operand: 
                    IFlowCaptureReferenceOperation: 1 (OperationKind.FlowCaptureReference, Type: System.Int32?, IsImplicit) (Syntax: 'input')
                Leaving: {R2}

            Next (Regular) Block[B3]
        Block[B3] - Block
            Predecessors: [B2]
            Statements (1)
                IFlowCaptureOperation: 2 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'input')
                  Value: 
                    IInvocationOperation ( System.Int32 System.Int32?.GetValueOrDefault()) (OperationKind.Invocation, Type: System.Int32, IsImplicit) (Syntax: 'input')
                      Instance Receiver: 
                        IFlowCaptureReferenceOperation: 1 (OperationKind.FlowCaptureReference, Type: System.Int32?, IsImplicit) (Syntax: 'input')
                      Arguments(0)

            Next (Regular) Block[B5]
                Leaving: {R2}
    }

    Block[B4] - Block
        Predecessors: [B2]
        Statements (1)
            IFlowCaptureOperation: 2 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'alternative')
              Value: 
                IParameterReferenceOperation: alternative (OperationKind.ParameterReference, Type: System.Int32) (Syntax: 'alternative')

        Next (Regular) Block[B5]
    Block[B5] - Block
        Predecessors: [B3] [B4]
        Statements (1)
            IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'result = in ... lternative;')
              Expression: 
                ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Int32) (Syntax: 'result = in ... alternative')
                  Left: 
                    IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: System.Int32, IsImplicit) (Syntax: 'result')
                  Right: 
                    IFlowCaptureReferenceOperation: 2 (OperationKind.FlowCaptureReference, Type: System.Int32, IsImplicit) (Syntax: 'input ?? alternative')

        Next (Regular) Block[B6]
            Leaving: {R1}
}

Block[B6] - Exit
    Predecessors: [B5]
    Statements (0)
";
            VerifyFlowGraphForTest<BlockSyntax>(compilation, expectedGraph);
        }

        [CompilerTrait(CompilerFeature.IOperation, CompilerFeature.Dataflow)]
        [Fact]
        public void CoalesceOperation_02()
        {
            var source = @"
class C
{
    void F(int? input, long alternative, long result)
    /*<bind>*/{
        result = input ?? alternative;
    }/*</bind>*/
}";

            var compilation = CreateCompilation(source);

            compilation.VerifyDiagnostics();

            var tree = compilation.SyntaxTrees.Single();
            var node = tree.GetRoot().DescendantNodes().OfType<BinaryExpressionSyntax>().Single();

            compilation.VerifyOperationTree(node, expectedOperationTree:
@"
ICoalesceOperation (OperationKind.Coalesce, Type: System.Int64) (Syntax: 'input ?? alternative')
  Expression: 
    IParameterReferenceOperation: input (OperationKind.ParameterReference, Type: System.Int32?) (Syntax: 'input')
  ValueConversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: True, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
    (ImplicitNumeric)
  WhenNull: 
    IParameterReferenceOperation: alternative (OperationKind.ParameterReference, Type: System.Int64) (Syntax: 'alternative')
");

            string expectedGraph = @"
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
                IParameterReferenceOperation: result (OperationKind.ParameterReference, Type: System.Int64) (Syntax: 'result')

        Next (Regular) Block[B2]
            Entering: {R2}

    .locals {R2}
    {
        CaptureIds: [1]
        Block[B2] - Block
            Predecessors: [B1]
            Statements (1)
                IFlowCaptureOperation: 1 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'input')
                  Value: 
                    IParameterReferenceOperation: input (OperationKind.ParameterReference, Type: System.Int32?) (Syntax: 'input')

            Jump if True (Regular) to Block[B4]
                IIsNullOperation (OperationKind.IsNull, Type: System.Boolean, IsImplicit) (Syntax: 'input')
                  Operand: 
                    IFlowCaptureReferenceOperation: 1 (OperationKind.FlowCaptureReference, Type: System.Int32?, IsImplicit) (Syntax: 'input')
                Leaving: {R2}

            Next (Regular) Block[B3]
        Block[B3] - Block
            Predecessors: [B2]
            Statements (1)
                IFlowCaptureOperation: 2 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'input')
                  Value: 
                    IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.Int64, IsImplicit) (Syntax: 'input')
                      Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: True, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                        (ImplicitNumeric)
                      Operand: 
                        IInvocationOperation ( System.Int32 System.Int32?.GetValueOrDefault()) (OperationKind.Invocation, Type: System.Int32, IsImplicit) (Syntax: 'input')
                          Instance Receiver: 
                            IFlowCaptureReferenceOperation: 1 (OperationKind.FlowCaptureReference, Type: System.Int32?, IsImplicit) (Syntax: 'input')
                          Arguments(0)

            Next (Regular) Block[B5]
                Leaving: {R2}
    }

    Block[B4] - Block
        Predecessors: [B2]
        Statements (1)
            IFlowCaptureOperation: 2 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'alternative')
              Value: 
                IParameterReferenceOperation: alternative (OperationKind.ParameterReference, Type: System.Int64) (Syntax: 'alternative')

        Next (Regular) Block[B5]
    Block[B5] - Block
        Predecessors: [B3] [B4]
        Statements (1)
            IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'result = in ... lternative;')
              Expression: 
                ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Int64) (Syntax: 'result = in ... alternative')
                  Left: 
                    IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: System.Int64, IsImplicit) (Syntax: 'result')
                  Right: 
                    IFlowCaptureReferenceOperation: 2 (OperationKind.FlowCaptureReference, Type: System.Int64, IsImplicit) (Syntax: 'input ?? alternative')

        Next (Regular) Block[B6]
            Leaving: {R1}
}

Block[B6] - Exit
    Predecessors: [B5]
    Statements (0)
";
            VerifyFlowGraphForTest<BlockSyntax>(compilation, expectedGraph);
        }

        [CompilerTrait(CompilerFeature.IOperation, CompilerFeature.Dataflow)]
        [Fact]
        public void CoalesceOperation_03()
        {
            var source = @"
class C
{
    void F(int? input, long? alternative, long? result)
    /*<bind>*/{
        result = input ?? alternative;
    }/*</bind>*/
}";

            var compilation = CreateCompilation(source);

            compilation.VerifyDiagnostics();

            var tree = compilation.SyntaxTrees.Single();
            var node = tree.GetRoot().DescendantNodes().OfType<BinaryExpressionSyntax>().Single();

            compilation.VerifyOperationTree(node, expectedOperationTree:
@"
ICoalesceOperation (OperationKind.Coalesce, Type: System.Int64?) (Syntax: 'input ?? alternative')
  Expression: 
    IParameterReferenceOperation: input (OperationKind.ParameterReference, Type: System.Int32?) (Syntax: 'input')
  ValueConversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
    (ImplicitNullable)
  WhenNull: 
    IParameterReferenceOperation: alternative (OperationKind.ParameterReference, Type: System.Int64?) (Syntax: 'alternative')
");

            string expectedGraph = @"
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
                IParameterReferenceOperation: result (OperationKind.ParameterReference, Type: System.Int64?) (Syntax: 'result')

        Next (Regular) Block[B2]
            Entering: {R2}

    .locals {R2}
    {
        CaptureIds: [1]
        Block[B2] - Block
            Predecessors: [B1]
            Statements (1)
                IFlowCaptureOperation: 1 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'input')
                  Value: 
                    IParameterReferenceOperation: input (OperationKind.ParameterReference, Type: System.Int32?) (Syntax: 'input')

            Jump if True (Regular) to Block[B4]
                IIsNullOperation (OperationKind.IsNull, Type: System.Boolean, IsImplicit) (Syntax: 'input')
                  Operand: 
                    IFlowCaptureReferenceOperation: 1 (OperationKind.FlowCaptureReference, Type: System.Int32?, IsImplicit) (Syntax: 'input')
                Leaving: {R2}

            Next (Regular) Block[B3]
        Block[B3] - Block
            Predecessors: [B2]
            Statements (1)
                IFlowCaptureOperation: 2 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'input')
                  Value: 
                    IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.Int64?, IsImplicit) (Syntax: 'input')
                      Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                        (ImplicitNullable)
                      Operand: 
                        IInvocationOperation ( System.Int32 System.Int32?.GetValueOrDefault()) (OperationKind.Invocation, Type: System.Int32, IsImplicit) (Syntax: 'input')
                          Instance Receiver: 
                            IFlowCaptureReferenceOperation: 1 (OperationKind.FlowCaptureReference, Type: System.Int32?, IsImplicit) (Syntax: 'input')
                          Arguments(0)

            Next (Regular) Block[B5]
                Leaving: {R2}
    }

    Block[B4] - Block
        Predecessors: [B2]
        Statements (1)
            IFlowCaptureOperation: 2 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'alternative')
              Value: 
                IParameterReferenceOperation: alternative (OperationKind.ParameterReference, Type: System.Int64?) (Syntax: 'alternative')

        Next (Regular) Block[B5]
    Block[B5] - Block
        Predecessors: [B3] [B4]
        Statements (1)
            IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'result = in ... lternative;')
              Expression: 
                ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Int64?) (Syntax: 'result = in ... alternative')
                  Left: 
                    IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: System.Int64?, IsImplicit) (Syntax: 'result')
                  Right: 
                    IFlowCaptureReferenceOperation: 2 (OperationKind.FlowCaptureReference, Type: System.Int64?, IsImplicit) (Syntax: 'input ?? alternative')

        Next (Regular) Block[B6]
            Leaving: {R1}
}

Block[B6] - Exit
    Predecessors: [B5]
    Statements (0)
";
            VerifyFlowGraphForTest<BlockSyntax>(compilation, expectedGraph);
        }

        [CompilerTrait(CompilerFeature.IOperation, CompilerFeature.Dataflow)]
        [Fact]
        public void CoalesceOperation_04()
        {
            var source = @"
class C
{
    void F(string input, object alternative, object result)
    /*<bind>*/{
        result = input ?? alternative;
    }/*</bind>*/
}";

            var compilation = CreateCompilation(source);

            compilation.VerifyDiagnostics();

            var tree = compilation.SyntaxTrees.Single();
            var node = tree.GetRoot().DescendantNodes().OfType<BinaryExpressionSyntax>().Single();

            compilation.VerifyOperationTree(node, expectedOperationTree:
@"
ICoalesceOperation (OperationKind.Coalesce, Type: System.Object) (Syntax: 'input ?? alternative')
  Expression: 
    IParameterReferenceOperation: input (OperationKind.ParameterReference, Type: System.String) (Syntax: 'input')
  ValueConversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: True, IsUserDefined: False) (MethodSymbol: null)
    (ImplicitReference)
  WhenNull: 
    IParameterReferenceOperation: alternative (OperationKind.ParameterReference, Type: System.Object) (Syntax: 'alternative')
");

            string expectedGraph = @"
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
                IFlowCaptureOperation: 1 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'input')
                  Value: 
                    IParameterReferenceOperation: input (OperationKind.ParameterReference, Type: System.String) (Syntax: 'input')

            Jump if True (Regular) to Block[B4]
                IIsNullOperation (OperationKind.IsNull, Type: System.Boolean, IsImplicit) (Syntax: 'input')
                  Operand: 
                    IFlowCaptureReferenceOperation: 1 (OperationKind.FlowCaptureReference, Type: System.String, IsImplicit) (Syntax: 'input')
                Leaving: {R2}

            Next (Regular) Block[B3]
        Block[B3] - Block
            Predecessors: [B2]
            Statements (1)
                IFlowCaptureOperation: 2 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'input')
                  Value: 
                    IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.Object, IsImplicit) (Syntax: 'input')
                      Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: True, IsUserDefined: False) (MethodSymbol: null)
                        (ImplicitReference)
                      Operand: 
                        IFlowCaptureReferenceOperation: 1 (OperationKind.FlowCaptureReference, Type: System.String, IsImplicit) (Syntax: 'input')

            Next (Regular) Block[B5]
                Leaving: {R2}
    }

    Block[B4] - Block
        Predecessors: [B2]
        Statements (1)
            IFlowCaptureOperation: 2 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'alternative')
              Value: 
                IParameterReferenceOperation: alternative (OperationKind.ParameterReference, Type: System.Object) (Syntax: 'alternative')

        Next (Regular) Block[B5]
    Block[B5] - Block
        Predecessors: [B3] [B4]
        Statements (1)
            IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'result = in ... lternative;')
              Expression: 
                ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Object) (Syntax: 'result = in ... alternative')
                  Left: 
                    IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: System.Object, IsImplicit) (Syntax: 'result')
                  Right: 
                    IFlowCaptureReferenceOperation: 2 (OperationKind.FlowCaptureReference, Type: System.Object, IsImplicit) (Syntax: 'input ?? alternative')

        Next (Regular) Block[B6]
            Leaving: {R1}
}

Block[B6] - Exit
    Predecessors: [B5]
    Statements (0)
";
            VerifyFlowGraphForTest<BlockSyntax>(compilation, expectedGraph);
        }

        [CompilerTrait(CompilerFeature.IOperation, CompilerFeature.Dataflow)]
        [Fact]
        public void CoalesceOperation_05()
        {
            var source = @"
class C
{
    void F(int? input, System.DateTime alternative, object result)
    /*<bind>*/{
        result = input ?? alternative;
    }/*</bind>*/
}";

            var compilation = CreateCompilation(source);

            compilation.VerifyDiagnostics(
                // (6,18): error CS0019: Operator '??' cannot be applied to operands of type 'int?' and 'DateTime'
                //         result = input ?? alternative;
                Diagnostic(ErrorCode.ERR_BadBinaryOps, "input ?? alternative").WithArguments("??", "int?", "System.DateTime").WithLocation(6, 18)
                );

            var tree = compilation.SyntaxTrees.Single();
            var node = tree.GetRoot().DescendantNodes().OfType<BinaryExpressionSyntax>().Single();

            compilation.VerifyOperationTree(node, expectedOperationTree:
@"
ICoalesceOperation (OperationKind.Coalesce, Type: ?, IsInvalid) (Syntax: 'input ?? alternative')
  Expression: 
    IParameterReferenceOperation: input (OperationKind.ParameterReference, Type: System.Int32?, IsInvalid) (Syntax: 'input')
  ValueConversion: CommonConversion (Exists: False, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
    (NoConversion)
  WhenNull: 
    IParameterReferenceOperation: alternative (OperationKind.ParameterReference, Type: System.DateTime, IsInvalid) (Syntax: 'alternative')
");

            string expectedGraph = @"
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
                IFlowCaptureOperation: 1 (OperationKind.FlowCapture, Type: null, IsInvalid, IsImplicit) (Syntax: 'input')
                  Value: 
                    IParameterReferenceOperation: input (OperationKind.ParameterReference, Type: System.Int32?, IsInvalid) (Syntax: 'input')

            Jump if True (Regular) to Block[B4]
                IIsNullOperation (OperationKind.IsNull, Type: System.Boolean, IsInvalid, IsImplicit) (Syntax: 'input')
                  Operand: 
                    IFlowCaptureReferenceOperation: 1 (OperationKind.FlowCaptureReference, Type: System.Int32?, IsInvalid, IsImplicit) (Syntax: 'input')
                Leaving: {R2}

            Next (Regular) Block[B3]
        Block[B3] - Block
            Predecessors: [B2]
            Statements (1)
                IFlowCaptureOperation: 2 (OperationKind.FlowCapture, Type: null, IsInvalid, IsImplicit) (Syntax: 'input')
                  Value: 
                    IInvalidOperation (OperationKind.Invalid, Type: ?, IsInvalid, IsImplicit) (Syntax: 'input')
                      Children(1):
                          IFlowCaptureReferenceOperation: 1 (OperationKind.FlowCaptureReference, Type: System.Int32?, IsInvalid, IsImplicit) (Syntax: 'input')

            Next (Regular) Block[B5]
                Leaving: {R2}
    }

    Block[B4] - Block
        Predecessors: [B2]
        Statements (1)
            IFlowCaptureOperation: 2 (OperationKind.FlowCapture, Type: null, IsInvalid, IsImplicit) (Syntax: 'alternative')
              Value: 
                IParameterReferenceOperation: alternative (OperationKind.ParameterReference, Type: System.DateTime, IsInvalid) (Syntax: 'alternative')

        Next (Regular) Block[B5]
    Block[B5] - Block
        Predecessors: [B3] [B4]
        Statements (1)
            IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null, IsInvalid) (Syntax: 'result = in ... lternative;')
              Expression: 
                ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Object, IsInvalid) (Syntax: 'result = in ... alternative')
                  Left: 
                    IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: System.Object, IsImplicit) (Syntax: 'result')
                  Right: 
                    IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.Object, IsInvalid, IsImplicit) (Syntax: 'input ?? alternative')
                      Conversion: CommonConversion (Exists: False, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                        (NoConversion)
                      Operand: 
                        IFlowCaptureReferenceOperation: 2 (OperationKind.FlowCaptureReference, Type: ?, IsInvalid, IsImplicit) (Syntax: 'input ?? alternative')

        Next (Regular) Block[B6]
            Leaving: {R1}
}

Block[B6] - Exit
    Predecessors: [B5]
    Statements (0)
";
            VerifyFlowGraphForTest<BlockSyntax>(compilation, expectedGraph);
        }

        [CompilerTrait(CompilerFeature.IOperation, CompilerFeature.Dataflow)]
        [Fact, WorkItem(60059, "https://github.com/dotnet/roslyn/issues/60059")]
        public void CoalesceOperation_06()
        {
            var source = @"
class C
{
    void F(int? input, dynamic alternative, dynamic result)
    /*<bind>*/{
        result = input ?? alternative;
    }/*</bind>*/
}";
            var compilation = CreateCompilation(source, references: new[] { CSharpRef }, targetFramework: TargetFramework.Mscorlib40AndSystemCore);

            compilation.VerifyDiagnostics();

            var tree = compilation.SyntaxTrees.Single();
            var node = tree.GetRoot().DescendantNodes().OfType<BinaryExpressionSyntax>().Single();

            compilation.VerifyOperationTree(node, expectedOperationTree:
@"
ICoalesceOperation (OperationKind.Coalesce, Type: dynamic) (Syntax: 'input ?? alternative')
  Expression: 
    IParameterReferenceOperation: input (OperationKind.ParameterReference, Type: System.Int32?) (Syntax: 'input')
  ValueConversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
    (Boxing)
  WhenNull: 
    IParameterReferenceOperation: alternative (OperationKind.ParameterReference, Type: dynamic) (Syntax: 'alternative')
");

            string expectedGraph = @"
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
                IParameterReferenceOperation: result (OperationKind.ParameterReference, Type: dynamic) (Syntax: 'result')

        Next (Regular) Block[B2]
            Entering: {R2}

    .locals {R2}
    {
        CaptureIds: [1]
        Block[B2] - Block
            Predecessors: [B1]
            Statements (1)
                IFlowCaptureOperation: 1 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'input')
                  Value: 
                    IParameterReferenceOperation: input (OperationKind.ParameterReference, Type: System.Int32?) (Syntax: 'input')

            Jump if True (Regular) to Block[B4]
                IIsNullOperation (OperationKind.IsNull, Type: System.Boolean, IsImplicit) (Syntax: 'input')
                  Operand: 
                    IFlowCaptureReferenceOperation: 1 (OperationKind.FlowCaptureReference, Type: System.Int32?, IsImplicit) (Syntax: 'input')
                Leaving: {R2}

            Next (Regular) Block[B3]
        Block[B3] - Block
            Predecessors: [B2]
            Statements (1)
                IFlowCaptureOperation: 2 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'input')
                  Value: 
                    IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: dynamic, IsImplicit) (Syntax: 'input')
                      Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                        (Boxing)
                      Operand: 
                        IInvocationOperation ( System.Int32 System.Int32?.GetValueOrDefault()) (OperationKind.Invocation, Type: System.Int32, IsImplicit) (Syntax: 'input')
                          Instance Receiver: 
                            IFlowCaptureReferenceOperation: 1 (OperationKind.FlowCaptureReference, Type: System.Int32?, IsImplicit) (Syntax: 'input')
                          Arguments(0)

            Next (Regular) Block[B5]
                Leaving: {R2}
    }

    Block[B4] - Block
        Predecessors: [B2]
        Statements (1)
            IFlowCaptureOperation: 2 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'alternative')
              Value: 
                IParameterReferenceOperation: alternative (OperationKind.ParameterReference, Type: dynamic) (Syntax: 'alternative')

        Next (Regular) Block[B5]
    Block[B5] - Block
        Predecessors: [B3] [B4]
        Statements (1)
            IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'result = in ... lternative;')
              Expression: 
                ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: dynamic) (Syntax: 'result = in ... alternative')
                  Left: 
                    IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: dynamic, IsImplicit) (Syntax: 'result')
                  Right: 
                    IFlowCaptureReferenceOperation: 2 (OperationKind.FlowCaptureReference, Type: dynamic, IsImplicit) (Syntax: 'input ?? alternative')

        Next (Regular) Block[B6]
            Leaving: {R1}
}

Block[B6] - Exit
    Predecessors: [B5]
    Statements (0)
";
            VerifyFlowGraphForTest<BlockSyntax>(compilation, expectedGraph);
        }

        [CompilerTrait(CompilerFeature.IOperation, CompilerFeature.Dataflow)]
        [Fact]
        public void CoalesceOperation_07()
        {
            var source = @"
class C
{
    void F(dynamic alternative, dynamic result)
    /*<bind>*/{
        result = null ?? alternative;
    }/*</bind>*/
}";

            var compilation = CreateCompilation(source, references: new[] { CSharpRef }, targetFramework: TargetFramework.Mscorlib40AndSystemCore);

            compilation.VerifyDiagnostics();

            var tree = compilation.SyntaxTrees.Single();
            var node = tree.GetRoot().DescendantNodes().OfType<BinaryExpressionSyntax>().Single();

            compilation.VerifyOperationTree(node, expectedOperationTree:
@"
ICoalesceOperation (OperationKind.Coalesce, Type: dynamic) (Syntax: 'null ?? alternative')
  Expression: 
    ILiteralOperation (OperationKind.Literal, Type: null, Constant: null) (Syntax: 'null')
  ValueConversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: True, IsUserDefined: False) (MethodSymbol: null)
    (ImplicitReference)
  WhenNull: 
    IParameterReferenceOperation: alternative (OperationKind.ParameterReference, Type: dynamic) (Syntax: 'alternative')
");

            string expectedGraph = @"
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
                IParameterReferenceOperation: result (OperationKind.ParameterReference, Type: dynamic) (Syntax: 'result')

        Next (Regular) Block[B2]
            Entering: {R2}

    .locals {R2}
    {
        CaptureIds: [1]
        Block[B2] - Block
            Predecessors: [B1]
            Statements (1)
                IFlowCaptureOperation: 1 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'null')
                  Value: 
                    ILiteralOperation (OperationKind.Literal, Type: null, Constant: null) (Syntax: 'null')

            Jump if True (Regular) to Block[B4]
                IIsNullOperation (OperationKind.IsNull, Type: System.Boolean, Constant: True, IsImplicit) (Syntax: 'null')
                  Operand: 
                    IFlowCaptureReferenceOperation: 1 (OperationKind.FlowCaptureReference, Type: null, Constant: null, IsImplicit) (Syntax: 'null')
                Leaving: {R2}

            Next (Regular) Block[B3]
        Block[B3] - Block [UnReachable]
            Predecessors: [B2]
            Statements (1)
                IFlowCaptureOperation: 2 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'null')
                  Value: 
                    IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: dynamic, IsImplicit) (Syntax: 'null')
                      Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: True, IsUserDefined: False) (MethodSymbol: null)
                        (ImplicitReference)
                      Operand: 
                        IFlowCaptureReferenceOperation: 1 (OperationKind.FlowCaptureReference, Type: null, Constant: null, IsImplicit) (Syntax: 'null')

            Next (Regular) Block[B5]
                Leaving: {R2}
    }

    Block[B4] - Block
        Predecessors: [B2]
        Statements (1)
            IFlowCaptureOperation: 2 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'alternative')
              Value: 
                IParameterReferenceOperation: alternative (OperationKind.ParameterReference, Type: dynamic) (Syntax: 'alternative')

        Next (Regular) Block[B5]
    Block[B5] - Block
        Predecessors: [B3] [B4]
        Statements (1)
            IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'result = nu ... lternative;')
              Expression: 
                ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: dynamic) (Syntax: 'result = nu ... alternative')
                  Left: 
                    IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: dynamic, IsImplicit) (Syntax: 'result')
                  Right: 
                    IFlowCaptureReferenceOperation: 2 (OperationKind.FlowCaptureReference, Type: dynamic, IsImplicit) (Syntax: 'null ?? alternative')

        Next (Regular) Block[B6]
            Leaving: {R1}
}

Block[B6] - Exit
    Predecessors: [B5]
    Statements (0)
";
            VerifyFlowGraphForTest<BlockSyntax>(compilation, expectedGraph);
        }

        [CompilerTrait(CompilerFeature.IOperation, CompilerFeature.Dataflow)]
        [Fact]
        public void CoalesceOperation_08()
        {
            var source = @"
class C
{
    void F(int alternative, int result)
    /*<bind>*/{
        result = null ?? alternative;
    }/*</bind>*/
}";

            var compilation = CreateCompilation(source);

            compilation.VerifyDiagnostics(
                // (6,18): error CS0019: Operator '??' cannot be applied to operands of type '<null>' and 'int'
                //         result = null ?? alternative;
                Diagnostic(ErrorCode.ERR_BadBinaryOps, "null ?? alternative").WithArguments("??", "<null>", "int").WithLocation(6, 18)
                );

            var tree = compilation.SyntaxTrees.Single();
            var node = tree.GetRoot().DescendantNodes().OfType<BinaryExpressionSyntax>().Single();

            compilation.VerifyOperationTree(node, expectedOperationTree:
@"
ICoalesceOperation (OperationKind.Coalesce, Type: ?, IsInvalid) (Syntax: 'null ?? alternative')
  Expression: 
    ILiteralOperation (OperationKind.Literal, Type: null, Constant: null, IsInvalid) (Syntax: 'null')
  ValueConversion: CommonConversion (Exists: False, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
    (NoConversion)
  WhenNull: 
    IParameterReferenceOperation: alternative (OperationKind.ParameterReference, Type: System.Int32, IsInvalid) (Syntax: 'alternative')
");

            string expectedGraph = @"
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
                IParameterReferenceOperation: result (OperationKind.ParameterReference, Type: System.Int32) (Syntax: 'result')

        Next (Regular) Block[B2]
            Entering: {R2}

    .locals {R2}
    {
        CaptureIds: [1]
        Block[B2] - Block
            Predecessors: [B1]
            Statements (1)
                IFlowCaptureOperation: 1 (OperationKind.FlowCapture, Type: null, IsInvalid, IsImplicit) (Syntax: 'null')
                  Value: 
                    ILiteralOperation (OperationKind.Literal, Type: null, Constant: null, IsInvalid) (Syntax: 'null')

            Jump if True (Regular) to Block[B4]
                IIsNullOperation (OperationKind.IsNull, Type: System.Boolean, Constant: True, IsInvalid, IsImplicit) (Syntax: 'null')
                  Operand: 
                    IFlowCaptureReferenceOperation: 1 (OperationKind.FlowCaptureReference, Type: null, Constant: null, IsInvalid, IsImplicit) (Syntax: 'null')
                Leaving: {R2}

            Next (Regular) Block[B3]
        Block[B3] - Block [UnReachable]
            Predecessors: [B2]
            Statements (1)
                IFlowCaptureOperation: 2 (OperationKind.FlowCapture, Type: null, IsInvalid, IsImplicit) (Syntax: 'null')
                  Value: 
                    IInvalidOperation (OperationKind.Invalid, Type: ?, IsInvalid, IsImplicit) (Syntax: 'null')
                      Children(1):
                          IFlowCaptureReferenceOperation: 1 (OperationKind.FlowCaptureReference, Type: null, Constant: null, IsInvalid, IsImplicit) (Syntax: 'null')

            Next (Regular) Block[B5]
                Leaving: {R2}
    }

    Block[B4] - Block
        Predecessors: [B2]
        Statements (1)
            IFlowCaptureOperation: 2 (OperationKind.FlowCapture, Type: null, IsInvalid, IsImplicit) (Syntax: 'alternative')
              Value: 
                IParameterReferenceOperation: alternative (OperationKind.ParameterReference, Type: System.Int32, IsInvalid) (Syntax: 'alternative')

        Next (Regular) Block[B5]
    Block[B5] - Block
        Predecessors: [B3] [B4]
        Statements (1)
            IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null, IsInvalid) (Syntax: 'result = nu ... lternative;')
              Expression: 
                ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Int32, IsInvalid) (Syntax: 'result = nu ... alternative')
                  Left: 
                    IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: System.Int32, IsImplicit) (Syntax: 'result')
                  Right: 
                    IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.Int32, IsInvalid, IsImplicit) (Syntax: 'null ?? alternative')
                      Conversion: CommonConversion (Exists: False, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                        (NoConversion)
                      Operand: 
                        IFlowCaptureReferenceOperation: 2 (OperationKind.FlowCaptureReference, Type: ?, IsInvalid, IsImplicit) (Syntax: 'null ?? alternative')

        Next (Regular) Block[B6]
            Leaving: {R1}
}

Block[B6] - Exit
    Predecessors: [B5]
    Statements (0)
";
            VerifyFlowGraphForTest<BlockSyntax>(compilation, expectedGraph);
        }

        [CompilerTrait(CompilerFeature.IOperation, CompilerFeature.Dataflow)]
        [Fact]
        public void CoalesceOperation_09()
        {
            var source = @"
class C
{
    void F(int? alternative, int? result)
    /*<bind>*/{
        result = null ?? alternative;
    }/*</bind>*/
}";

            var compilation = CreateCompilation(source);

            compilation.VerifyDiagnostics();

            var tree = compilation.SyntaxTrees.Single();
            var node = tree.GetRoot().DescendantNodes().OfType<BinaryExpressionSyntax>().Single();

            compilation.VerifyOperationTree(node, expectedOperationTree:
@"
ICoalesceOperation (OperationKind.Coalesce, Type: System.Int32?) (Syntax: 'null ?? alternative')
  Expression: 
    ILiteralOperation (OperationKind.Literal, Type: null, Constant: null) (Syntax: 'null')
  ValueConversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
    (NullLiteral)
  WhenNull: 
    IParameterReferenceOperation: alternative (OperationKind.ParameterReference, Type: System.Int32?) (Syntax: 'alternative')
");

            string expectedGraph = @"
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
                IParameterReferenceOperation: result (OperationKind.ParameterReference, Type: System.Int32?) (Syntax: 'result')
        Next (Regular) Block[B2]
            Entering: {R2}
    .locals {R2}
    {
        CaptureIds: [1]
        Block[B2] - Block
            Predecessors: [B1]
            Statements (1)
                IFlowCaptureOperation: 1 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'null')
                  Value: 
                    ILiteralOperation (OperationKind.Literal, Type: null, Constant: null) (Syntax: 'null')
            Jump if True (Regular) to Block[B4]
                IIsNullOperation (OperationKind.IsNull, Type: System.Boolean, Constant: True, IsImplicit) (Syntax: 'null')
                  Operand: 
                    IFlowCaptureReferenceOperation: 1 (OperationKind.FlowCaptureReference, Type: null, Constant: null, IsImplicit) (Syntax: 'null')
                Leaving: {R2}
            Next (Regular) Block[B3]
        Block[B3] - Block [UnReachable]
            Predecessors: [B2]
            Statements (1)
                IFlowCaptureOperation: 2 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'null')
                  Value: 
                    IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.Int32?, IsImplicit) (Syntax: 'null')
                      Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                        (NullLiteral)
                      Operand: 
                        IFlowCaptureReferenceOperation: 1 (OperationKind.FlowCaptureReference, Type: null, Constant: null, IsImplicit) (Syntax: 'null')
            Next (Regular) Block[B5]
                Leaving: {R2}
    }
    Block[B4] - Block
        Predecessors: [B2]
        Statements (1)
            IFlowCaptureOperation: 2 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'alternative')
              Value: 
                IParameterReferenceOperation: alternative (OperationKind.ParameterReference, Type: System.Int32?) (Syntax: 'alternative')
        Next (Regular) Block[B5]
    Block[B5] - Block
        Predecessors: [B3] [B4]
        Statements (1)
            IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'result = nu ... lternative;')
              Expression: 
                ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Int32?) (Syntax: 'result = nu ... alternative')
                  Left: 
                    IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: System.Int32?, IsImplicit) (Syntax: 'result')
                  Right: 
                    IFlowCaptureReferenceOperation: 2 (OperationKind.FlowCaptureReference, Type: System.Int32?, IsImplicit) (Syntax: 'null ?? alternative')
        Next (Regular) Block[B6]
            Leaving: {R1}
}
Block[B6] - Exit
    Predecessors: [B5]
    Statements (0)
";
            VerifyFlowGraphForTest<BlockSyntax>(compilation, expectedGraph);
        }

        [CompilerTrait(CompilerFeature.IOperation, CompilerFeature.Dataflow)]
        [Fact]
        public void CoalesceOperation_10()
        {
            var source = @"
class C
{
    void F(int? input, byte? alternative, int? result)
    /*<bind>*/{
        result = input ?? alternative;
    }/*</bind>*/
}";

            var compilation = CreateCompilation(source);

            compilation.VerifyDiagnostics();

            var tree = compilation.SyntaxTrees.Single();
            var node = tree.GetRoot().DescendantNodes().OfType<BinaryExpressionSyntax>().Single();

            compilation.VerifyOperationTree(node, expectedOperationTree:
@"
ICoalesceOperation (OperationKind.Coalesce, Type: System.Int32?) (Syntax: 'input ?? alternative')
  Expression: 
    IParameterReferenceOperation: input (OperationKind.ParameterReference, Type: System.Int32?) (Syntax: 'input')
  ValueConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
    (Identity)
  WhenNull: 
    IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.Int32?, IsImplicit) (Syntax: 'alternative')
      Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
      Operand: 
        IParameterReferenceOperation: alternative (OperationKind.ParameterReference, Type: System.Byte?) (Syntax: 'alternative')
");

            string expectedGraph = @"
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
                IParameterReferenceOperation: result (OperationKind.ParameterReference, Type: System.Int32?) (Syntax: 'result')

        Next (Regular) Block[B2]
            Entering: {R2}

    .locals {R2}
    {
        CaptureIds: [1]
        Block[B2] - Block
            Predecessors: [B1]
            Statements (1)
                IFlowCaptureOperation: 1 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'input')
                  Value: 
                    IParameterReferenceOperation: input (OperationKind.ParameterReference, Type: System.Int32?) (Syntax: 'input')

            Jump if True (Regular) to Block[B4]
                IIsNullOperation (OperationKind.IsNull, Type: System.Boolean, IsImplicit) (Syntax: 'input')
                  Operand: 
                    IFlowCaptureReferenceOperation: 1 (OperationKind.FlowCaptureReference, Type: System.Int32?, IsImplicit) (Syntax: 'input')
                Leaving: {R2}

            Next (Regular) Block[B3]
        Block[B3] - Block
            Predecessors: [B2]
            Statements (1)
                IFlowCaptureOperation: 2 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'input')
                  Value: 
                    IFlowCaptureReferenceOperation: 1 (OperationKind.FlowCaptureReference, Type: System.Int32?, IsImplicit) (Syntax: 'input')

            Next (Regular) Block[B5]
                Leaving: {R2}
    }

    Block[B4] - Block
        Predecessors: [B2]
        Statements (1)
            IFlowCaptureOperation: 2 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'alternative')
              Value: 
                IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.Int32?, IsImplicit) (Syntax: 'alternative')
                  Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                    (ImplicitNullable)
                  Operand: 
                    IParameterReferenceOperation: alternative (OperationKind.ParameterReference, Type: System.Byte?) (Syntax: 'alternative')

        Next (Regular) Block[B5]
    Block[B5] - Block
        Predecessors: [B3] [B4]
        Statements (1)
            IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'result = in ... lternative;')
              Expression: 
                ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Int32?) (Syntax: 'result = in ... alternative')
                  Left: 
                    IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: System.Int32?, IsImplicit) (Syntax: 'result')
                  Right: 
                    IFlowCaptureReferenceOperation: 2 (OperationKind.FlowCaptureReference, Type: System.Int32?, IsImplicit) (Syntax: 'input ?? alternative')

        Next (Regular) Block[B6]
            Leaving: {R1}
}

Block[B6] - Exit
    Predecessors: [B5]
    Statements (0)
";
            VerifyFlowGraphForTest<BlockSyntax>(compilation, expectedGraph);
        }

        [CompilerTrait(CompilerFeature.IOperation, CompilerFeature.Dataflow)]
        [Fact]
        public void CoalesceOperation_11()
        {
            var source = @"
class C
{
    void F(int? input, int? alternative, int? result)
    /*<bind>*/{
        result = input ?? alternative;
    }/*</bind>*/
}";

            var compilation = CreateCompilation(source);

            compilation.VerifyDiagnostics();

            var tree = compilation.SyntaxTrees.Single();
            var node = tree.GetRoot().DescendantNodes().OfType<BinaryExpressionSyntax>().Single();

            compilation.VerifyOperationTree(node, expectedOperationTree:
@"
ICoalesceOperation (OperationKind.Coalesce, Type: System.Int32?) (Syntax: 'input ?? alternative')
  Expression: 
    IParameterReferenceOperation: input (OperationKind.ParameterReference, Type: System.Int32?) (Syntax: 'input')
  ValueConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
    (Identity)
  WhenNull: 
    IParameterReferenceOperation: alternative (OperationKind.ParameterReference, Type: System.Int32?) (Syntax: 'alternative')
");

            string expectedGraph = @"
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
                IParameterReferenceOperation: result (OperationKind.ParameterReference, Type: System.Int32?) (Syntax: 'result')

        Next (Regular) Block[B2]
            Entering: {R2}

    .locals {R2}
    {
        CaptureIds: [1]
        Block[B2] - Block
            Predecessors: [B1]
            Statements (1)
                IFlowCaptureOperation: 1 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'input')
                  Value: 
                    IParameterReferenceOperation: input (OperationKind.ParameterReference, Type: System.Int32?) (Syntax: 'input')

            Jump if True (Regular) to Block[B4]
                IIsNullOperation (OperationKind.IsNull, Type: System.Boolean, IsImplicit) (Syntax: 'input')
                  Operand: 
                    IFlowCaptureReferenceOperation: 1 (OperationKind.FlowCaptureReference, Type: System.Int32?, IsImplicit) (Syntax: 'input')
                Leaving: {R2}

            Next (Regular) Block[B3]
        Block[B3] - Block
            Predecessors: [B2]
            Statements (1)
                IFlowCaptureOperation: 2 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'input')
                  Value: 
                    IFlowCaptureReferenceOperation: 1 (OperationKind.FlowCaptureReference, Type: System.Int32?, IsImplicit) (Syntax: 'input')

            Next (Regular) Block[B5]
                Leaving: {R2}
    }

    Block[B4] - Block
        Predecessors: [B2]
        Statements (1)
            IFlowCaptureOperation: 2 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'alternative')
              Value: 
                IParameterReferenceOperation: alternative (OperationKind.ParameterReference, Type: System.Int32?) (Syntax: 'alternative')

        Next (Regular) Block[B5]
    Block[B5] - Block
        Predecessors: [B3] [B4]
        Statements (1)
            IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'result = in ... lternative;')
              Expression: 
                ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Int32?) (Syntax: 'result = in ... alternative')
                  Left: 
                    IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: System.Int32?, IsImplicit) (Syntax: 'result')
                  Right: 
                    IFlowCaptureReferenceOperation: 2 (OperationKind.FlowCaptureReference, Type: System.Int32?, IsImplicit) (Syntax: 'input ?? alternative')

        Next (Regular) Block[B6]
            Leaving: {R1}
}

Block[B6] - Exit
    Predecessors: [B5]
    Statements (0)
";
            VerifyFlowGraphForTest<BlockSyntax>(compilation, expectedGraph);
        }

        [CompilerTrait(CompilerFeature.IOperation, CompilerFeature.Dataflow)]
        [Fact]
        public void CoalesceOperation_12()
        {
            var source = @"
class C
{
    void F(int? input1, int? alternative1, int? input2, int? alternative2, int? result)
    /*<bind>*/{
        result = (input1 ?? alternative1) ?? (input2 ?? alternative2);
    }/*</bind>*/
}";

            var compilation = CreateCompilation(source);

            compilation.VerifyDiagnostics();

            string expectedGraph = @"
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
                IParameterReferenceOperation: result (OperationKind.ParameterReference, Type: System.Int32?) (Syntax: 'result')

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
                    IFlowCaptureOperation: 1 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'input1')
                      Value: 
                        IParameterReferenceOperation: input1 (OperationKind.ParameterReference, Type: System.Int32?) (Syntax: 'input1')

                Jump if True (Regular) to Block[B4]
                    IIsNullOperation (OperationKind.IsNull, Type: System.Boolean, IsImplicit) (Syntax: 'input1')
                      Operand: 
                        IFlowCaptureReferenceOperation: 1 (OperationKind.FlowCaptureReference, Type: System.Int32?, IsImplicit) (Syntax: 'input1')
                    Leaving: {R3}

                Next (Regular) Block[B3]
            Block[B3] - Block
                Predecessors: [B2]
                Statements (1)
                    IFlowCaptureOperation: 2 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'input1')
                      Value: 
                        IFlowCaptureReferenceOperation: 1 (OperationKind.FlowCaptureReference, Type: System.Int32?, IsImplicit) (Syntax: 'input1')

                Next (Regular) Block[B5]
                    Leaving: {R3}
        }

        Block[B4] - Block
            Predecessors: [B2]
            Statements (1)
                IFlowCaptureOperation: 2 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'alternative1')
                  Value: 
                    IParameterReferenceOperation: alternative1 (OperationKind.ParameterReference, Type: System.Int32?) (Syntax: 'alternative1')

            Next (Regular) Block[B5]
        Block[B5] - Block
            Predecessors: [B3] [B4]
            Statements (0)
            Jump if True (Regular) to Block[B7]
                IIsNullOperation (OperationKind.IsNull, Type: System.Boolean, IsImplicit) (Syntax: 'input1 ?? alternative1')
                  Operand: 
                    IFlowCaptureReferenceOperation: 2 (OperationKind.FlowCaptureReference, Type: System.Int32?, IsImplicit) (Syntax: 'input1 ?? alternative1')
                Leaving: {R2}
                Entering: {R4}

            Next (Regular) Block[B6]
        Block[B6] - Block
            Predecessors: [B5]
            Statements (1)
                IFlowCaptureOperation: 3 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'input1 ?? alternative1')
                  Value: 
                    IFlowCaptureReferenceOperation: 2 (OperationKind.FlowCaptureReference, Type: System.Int32?, IsImplicit) (Syntax: 'input1 ?? alternative1')

            Next (Regular) Block[B10]
                Leaving: {R2}
    }
    .locals {R4}
    {
        CaptureIds: [4]
        Block[B7] - Block
            Predecessors: [B5]
            Statements (1)
                IFlowCaptureOperation: 4 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'input2')
                  Value: 
                    IParameterReferenceOperation: input2 (OperationKind.ParameterReference, Type: System.Int32?) (Syntax: 'input2')

            Jump if True (Regular) to Block[B9]
                IIsNullOperation (OperationKind.IsNull, Type: System.Boolean, IsImplicit) (Syntax: 'input2')
                  Operand: 
                    IFlowCaptureReferenceOperation: 4 (OperationKind.FlowCaptureReference, Type: System.Int32?, IsImplicit) (Syntax: 'input2')
                Leaving: {R4}

            Next (Regular) Block[B8]
        Block[B8] - Block
            Predecessors: [B7]
            Statements (1)
                IFlowCaptureOperation: 3 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'input2')
                  Value: 
                    IFlowCaptureReferenceOperation: 4 (OperationKind.FlowCaptureReference, Type: System.Int32?, IsImplicit) (Syntax: 'input2')

            Next (Regular) Block[B10]
                Leaving: {R4}
    }

    Block[B9] - Block
        Predecessors: [B7]
        Statements (1)
            IFlowCaptureOperation: 3 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'alternative2')
              Value: 
                IParameterReferenceOperation: alternative2 (OperationKind.ParameterReference, Type: System.Int32?) (Syntax: 'alternative2')

        Next (Regular) Block[B10]
    Block[B10] - Block
        Predecessors: [B6] [B8] [B9]
        Statements (1)
            IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'result = (i ... ernative2);')
              Expression: 
                ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Int32?) (Syntax: 'result = (i ... ternative2)')
                  Left: 
                    IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: System.Int32?, IsImplicit) (Syntax: 'result')
                  Right: 
                    IFlowCaptureReferenceOperation: 3 (OperationKind.FlowCaptureReference, Type: System.Int32?, IsImplicit) (Syntax: '(input1 ??  ... ternative2)')

        Next (Regular) Block[B11]
            Leaving: {R1}
}

Block[B11] - Exit
    Predecessors: [B10]
    Statements (0)
";
            VerifyFlowGraphForTest<BlockSyntax>(compilation, expectedGraph);
        }

        [CompilerTrait(CompilerFeature.IOperation, CompilerFeature.Dataflow)]
        [Fact]
        public void CoalesceOperation_13()
        {
            var source = @"
class C
{
    const string input = ""a"";

    void F(object alternative, object result)
    /*<bind>*/{
        result = input ?? alternative;
    }/*</bind>*/
}";

            var compilation = CreateCompilation(source);

            compilation.VerifyDiagnostics();

            string expectedGraph = @"
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
                IFlowCaptureOperation: 1 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'input')
                  Value: 
                    IFieldReferenceOperation: System.String C.input (Static) (OperationKind.FieldReference, Type: System.String, Constant: ""a"") (Syntax: 'input')
                      Instance Receiver: 
                        null

            Jump if True (Regular) to Block[B4]
                IIsNullOperation (OperationKind.IsNull, Type: System.Boolean, Constant: False, IsImplicit) (Syntax: 'input')
                  Operand: 
                    IFlowCaptureReferenceOperation: 1 (OperationKind.FlowCaptureReference, Type: System.String, Constant: ""a"", IsImplicit) (Syntax: 'input')
                Leaving: {R2}

            Next (Regular) Block[B3]
        Block[B3] - Block
            Predecessors: [B2]
            Statements (1)
                IFlowCaptureOperation: 2 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'input')
                  Value: 
                    IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.Object, IsImplicit) (Syntax: 'input')
                      Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: True, IsUserDefined: False) (MethodSymbol: null)
                        (ImplicitReference)
                      Operand: 
                        IFlowCaptureReferenceOperation: 1 (OperationKind.FlowCaptureReference, Type: System.String, Constant: ""a"", IsImplicit) (Syntax: 'input')

            Next (Regular) Block[B5]
                Leaving: {R2}
    }

    Block[B4] - Block [UnReachable]
        Predecessors: [B2]
        Statements (1)
            IFlowCaptureOperation: 2 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'alternative')
              Value: 
                IParameterReferenceOperation: alternative (OperationKind.ParameterReference, Type: System.Object) (Syntax: 'alternative')

        Next (Regular) Block[B5]
    Block[B5] - Block
        Predecessors: [B3] [B4]
        Statements (1)
            IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'result = in ... lternative;')
              Expression: 
                ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Object) (Syntax: 'result = in ... alternative')
                  Left: 
                    IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: System.Object, IsImplicit) (Syntax: 'result')
                  Right: 
                    IFlowCaptureReferenceOperation: 2 (OperationKind.FlowCaptureReference, Type: System.Object, IsImplicit) (Syntax: 'input ?? alternative')

        Next (Regular) Block[B6]
            Leaving: {R1}
}

Block[B6] - Exit
    Predecessors: [B5]
    Statements (0)
";
            VerifyFlowGraphForTest<BlockSyntax>(compilation, expectedGraph);
        }

        [CompilerTrait(CompilerFeature.IOperation, CompilerFeature.Dataflow)]
        [Fact]
        public void CoalesceOperation_14()
        {
            string source = @"
class P
{
    void M1(int? i, int j, int result)
    /*<bind>*/{
        result = i ?? j;
    }/*</bind>*/
}
";
            var compilation = CreateCompilationWithMscorlib461(source);
            compilation.MakeMemberMissing(SpecialMember.System_Nullable_T_GetValueOrDefault);

            string expectedGraph = @"
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
                IParameterReferenceOperation: result (OperationKind.ParameterReference, Type: System.Int32) (Syntax: 'result')

        Next (Regular) Block[B2]
            Entering: {R2}

    .locals {R2}
    {
        CaptureIds: [1]
        Block[B2] - Block
            Predecessors: [B1]
            Statements (1)
                IFlowCaptureOperation: 1 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'i')
                  Value: 
                    IParameterReferenceOperation: i (OperationKind.ParameterReference, Type: System.Int32?) (Syntax: 'i')

            Jump if True (Regular) to Block[B4]
                IIsNullOperation (OperationKind.IsNull, Type: System.Boolean, IsImplicit) (Syntax: 'i')
                  Operand: 
                    IFlowCaptureReferenceOperation: 1 (OperationKind.FlowCaptureReference, Type: System.Int32?, IsImplicit) (Syntax: 'i')
                Leaving: {R2}

            Next (Regular) Block[B3]
        Block[B3] - Block
            Predecessors: [B2]
            Statements (1)
                IFlowCaptureOperation: 2 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'i')
                  Value: 
                    IInvalidOperation (OperationKind.Invalid, Type: System.Int32, IsImplicit) (Syntax: 'i')
                      Children(1):
                          IFlowCaptureReferenceOperation: 1 (OperationKind.FlowCaptureReference, Type: System.Int32?, IsImplicit) (Syntax: 'i')

            Next (Regular) Block[B5]
                Leaving: {R2}
    }

    Block[B4] - Block
        Predecessors: [B2]
        Statements (1)
            IFlowCaptureOperation: 2 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'j')
              Value: 
                IParameterReferenceOperation: j (OperationKind.ParameterReference, Type: System.Int32) (Syntax: 'j')

        Next (Regular) Block[B5]
    Block[B5] - Block
        Predecessors: [B3] [B4]
        Statements (1)
            IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'result = i ?? j;')
              Expression: 
                ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Int32) (Syntax: 'result = i ?? j')
                  Left: 
                    IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: System.Int32, IsImplicit) (Syntax: 'result')
                  Right: 
                    IFlowCaptureReferenceOperation: 2 (OperationKind.FlowCaptureReference, Type: System.Int32, IsImplicit) (Syntax: 'i ?? j')

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
    }
}
