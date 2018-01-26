// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Linq;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.CSharp.Test.Utilities;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.CSharp.UnitTests
{
    public partial class IOperationTests : SemanticModelTestBase
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

            var compilation = CreateStandardCompilation(source, parseOptions: TestOptions.RegularWithFlowAnalysisFeature);

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
Block[0] - Entry
    Statements (0)
    Next Block[1]
Block[1] - Block
    Predecessors (1)
        [0]
    Statements (2)
        IFlowCaptureOperation: 0 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'result')
          Value: 
            IParameterReferenceOperation: result (OperationKind.ParameterReference, Type: System.Int32) (Syntax: 'result')

        IFlowCaptureOperation: 1 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'input')
          Value: 
            IParameterReferenceOperation: input (OperationKind.ParameterReference, Type: System.Int32?) (Syntax: 'input')

    Jump if Null to Block[3]
        IFlowCaptureReferenceOperation: 1 (OperationKind.FlowCaptureReference, Type: System.Int32?, IsImplicit) (Syntax: 'input')

    Next Block[2]
Block[2] - Block
    Predecessors (1)
        [1]
    Statements (1)
        IFlowCaptureOperation: 2 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'input')
          Value: 
            IInvocationOperation ( System.Int32 System.Int32?.GetValueOrDefault()) (OperationKind.Invocation, Type: System.Int32, IsImplicit) (Syntax: 'input')
              Instance Receiver: 
                IFlowCaptureReferenceOperation: 1 (OperationKind.FlowCaptureReference, Type: System.Int32?, IsImplicit) (Syntax: 'input')
              Arguments(0)

    Next Block[4]
Block[3] - Block
    Predecessors (1)
        [1]
    Statements (1)
        IFlowCaptureOperation: 2 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'alternative')
          Value: 
            IParameterReferenceOperation: alternative (OperationKind.ParameterReference, Type: System.Int32) (Syntax: 'alternative')

    Next Block[4]
Block[4] - Block
    Predecessors (2)
        [2]
        [3]
    Statements (1)
        IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'result = in ... lternative;')
          Expression: 
            ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Int32) (Syntax: 'result = in ... alternative')
              Left: 
                IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: System.Int32, IsImplicit) (Syntax: 'result')
              Right: 
                IFlowCaptureReferenceOperation: 2 (OperationKind.FlowCaptureReference, Type: System.Int32, IsImplicit) (Syntax: 'input ?? alternative')

    Next Block[5]
Block[5] - Exit
    Predecessors (1)
        [4]
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

            var compilation = CreateStandardCompilation(source, parseOptions: TestOptions.RegularWithFlowAnalysisFeature);

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
Block[0] - Entry
    Statements (0)
    Next Block[1]
Block[1] - Block
    Predecessors (1)
        [0]
    Statements (2)
        IFlowCaptureOperation: 0 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'result')
          Value: 
            IParameterReferenceOperation: result (OperationKind.ParameterReference, Type: System.Int64) (Syntax: 'result')

        IFlowCaptureOperation: 1 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'input')
          Value: 
            IParameterReferenceOperation: input (OperationKind.ParameterReference, Type: System.Int32?) (Syntax: 'input')

    Jump if Null to Block[3]
        IFlowCaptureReferenceOperation: 1 (OperationKind.FlowCaptureReference, Type: System.Int32?, IsImplicit) (Syntax: 'input')

    Next Block[2]
Block[2] - Block
    Predecessors (1)
        [1]
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

    Next Block[4]
Block[3] - Block
    Predecessors (1)
        [1]
    Statements (1)
        IFlowCaptureOperation: 2 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'alternative')
          Value: 
            IParameterReferenceOperation: alternative (OperationKind.ParameterReference, Type: System.Int64) (Syntax: 'alternative')

    Next Block[4]
Block[4] - Block
    Predecessors (2)
        [2]
        [3]
    Statements (1)
        IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'result = in ... lternative;')
          Expression: 
            ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Int64) (Syntax: 'result = in ... alternative')
              Left: 
                IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: System.Int64, IsImplicit) (Syntax: 'result')
              Right: 
                IFlowCaptureReferenceOperation: 2 (OperationKind.FlowCaptureReference, Type: System.Int64, IsImplicit) (Syntax: 'input ?? alternative')

    Next Block[5]
Block[5] - Exit
    Predecessors (1)
        [4]
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

            var compilation = CreateStandardCompilation(source, parseOptions: TestOptions.RegularWithFlowAnalysisFeature);

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
Block[0] - Entry
    Statements (0)
    Next Block[1]
Block[1] - Block
    Predecessors (1)
        [0]
    Statements (2)
        IFlowCaptureOperation: 0 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'result')
          Value: 
            IParameterReferenceOperation: result (OperationKind.ParameterReference, Type: System.Int64?) (Syntax: 'result')

        IFlowCaptureOperation: 1 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'input')
          Value: 
            IParameterReferenceOperation: input (OperationKind.ParameterReference, Type: System.Int32?) (Syntax: 'input')

    Jump if Null to Block[3]
        IFlowCaptureReferenceOperation: 1 (OperationKind.FlowCaptureReference, Type: System.Int32?, IsImplicit) (Syntax: 'input')

    Next Block[2]
Block[2] - Block
    Predecessors (1)
        [1]
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

    Next Block[4]
Block[3] - Block
    Predecessors (1)
        [1]
    Statements (1)
        IFlowCaptureOperation: 2 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'alternative')
          Value: 
            IParameterReferenceOperation: alternative (OperationKind.ParameterReference, Type: System.Int64?) (Syntax: 'alternative')

    Next Block[4]
Block[4] - Block
    Predecessors (2)
        [2]
        [3]
    Statements (1)
        IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'result = in ... lternative;')
          Expression: 
            ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Int64?) (Syntax: 'result = in ... alternative')
              Left: 
                IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: System.Int64?, IsImplicit) (Syntax: 'result')
              Right: 
                IFlowCaptureReferenceOperation: 2 (OperationKind.FlowCaptureReference, Type: System.Int64?, IsImplicit) (Syntax: 'input ?? alternative')

    Next Block[5]
Block[5] - Exit
    Predecessors (1)
        [4]
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

            var compilation = CreateStandardCompilation(source, parseOptions: TestOptions.RegularWithFlowAnalysisFeature);

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
Block[0] - Entry
    Statements (0)
    Next Block[1]
Block[1] - Block
    Predecessors (1)
        [0]
    Statements (2)
        IFlowCaptureOperation: 0 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'result')
          Value: 
            IParameterReferenceOperation: result (OperationKind.ParameterReference, Type: System.Object) (Syntax: 'result')

        IFlowCaptureOperation: 1 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'input')
          Value: 
            IParameterReferenceOperation: input (OperationKind.ParameterReference, Type: System.String) (Syntax: 'input')

    Jump if Null to Block[3]
        IFlowCaptureReferenceOperation: 1 (OperationKind.FlowCaptureReference, Type: System.String, IsImplicit) (Syntax: 'input')

    Next Block[2]
Block[2] - Block
    Predecessors (1)
        [1]
    Statements (1)
        IFlowCaptureOperation: 2 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'input')
          Value: 
            IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.Object, IsImplicit) (Syntax: 'input')
              Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: True, IsUserDefined: False) (MethodSymbol: null)
                (ImplicitReference)
              Operand: 
                IFlowCaptureReferenceOperation: 1 (OperationKind.FlowCaptureReference, Type: System.String, IsImplicit) (Syntax: 'input')

    Next Block[4]
Block[3] - Block
    Predecessors (1)
        [1]
    Statements (1)
        IFlowCaptureOperation: 2 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'alternative')
          Value: 
            IParameterReferenceOperation: alternative (OperationKind.ParameterReference, Type: System.Object) (Syntax: 'alternative')

    Next Block[4]
Block[4] - Block
    Predecessors (2)
        [2]
        [3]
    Statements (1)
        IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'result = in ... lternative;')
          Expression: 
            ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Object) (Syntax: 'result = in ... alternative')
              Left: 
                IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: System.Object, IsImplicit) (Syntax: 'result')
              Right: 
                IFlowCaptureReferenceOperation: 2 (OperationKind.FlowCaptureReference, Type: System.Object, IsImplicit) (Syntax: 'input ?? alternative')

    Next Block[5]
Block[5] - Exit
    Predecessors (1)
        [4]
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

            var compilation = CreateStandardCompilation(source, parseOptions: TestOptions.RegularWithFlowAnalysisFeature);

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
Block[0] - Entry
    Statements (0)
    Next Block[1]
Block[1] - Block
    Predecessors (1)
        [0]
    Statements (2)
        IFlowCaptureOperation: 0 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'result')
          Value: 
            IParameterReferenceOperation: result (OperationKind.ParameterReference, Type: System.Object) (Syntax: 'result')

        IFlowCaptureOperation: 1 (OperationKind.FlowCapture, Type: null, IsInvalid, IsImplicit) (Syntax: 'input')
          Value: 
            IParameterReferenceOperation: input (OperationKind.ParameterReference, Type: System.Int32?, IsInvalid) (Syntax: 'input')

    Jump if Null to Block[3]
        IFlowCaptureReferenceOperation: 1 (OperationKind.FlowCaptureReference, Type: System.Int32?, IsInvalid, IsImplicit) (Syntax: 'input')

    Next Block[2]
Block[2] - Block
    Predecessors (1)
        [1]
    Statements (1)
        IFlowCaptureOperation: 2 (OperationKind.FlowCapture, Type: null, IsInvalid, IsImplicit) (Syntax: 'input')
          Value: 
            IInvalidOperation (OperationKind.Invalid, Type: ?, IsInvalid, IsImplicit) (Syntax: 'input')
              Children(1):
                  IFlowCaptureReferenceOperation: 1 (OperationKind.FlowCaptureReference, Type: System.Int32?, IsInvalid, IsImplicit) (Syntax: 'input')

    Next Block[4]
Block[3] - Block
    Predecessors (1)
        [1]
    Statements (1)
        IFlowCaptureOperation: 2 (OperationKind.FlowCapture, Type: null, IsInvalid, IsImplicit) (Syntax: 'alternative')
          Value: 
            IParameterReferenceOperation: alternative (OperationKind.ParameterReference, Type: System.DateTime, IsInvalid) (Syntax: 'alternative')

    Next Block[4]
Block[4] - Block
    Predecessors (2)
        [2]
        [3]
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

    Next Block[5]
Block[5] - Exit
    Predecessors (1)
        [4]
    Statements (0)
";
            VerifyFlowGraphForTest<BlockSyntax>(compilation, expectedGraph);
        }

        [CompilerTrait(CompilerFeature.IOperation, CompilerFeature.Dataflow)]
        [Fact]
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

            var compilation = CreateCompilationWithMscorlibAndSystemCore(source, references: new[] { CSharpRef }, parseOptions: TestOptions.RegularWithFlowAnalysisFeature);

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
Block[0] - Entry
    Statements (0)
    Next Block[1]
Block[1] - Block
    Predecessors (1)
        [0]
    Statements (2)
        IFlowCaptureOperation: 0 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'result')
          Value: 
            IParameterReferenceOperation: result (OperationKind.ParameterReference, Type: dynamic) (Syntax: 'result')

        IFlowCaptureOperation: 1 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'input')
          Value: 
            IParameterReferenceOperation: input (OperationKind.ParameterReference, Type: System.Int32?) (Syntax: 'input')

    Jump if Null to Block[3]
        IFlowCaptureReferenceOperation: 1 (OperationKind.FlowCaptureReference, Type: System.Int32?, IsImplicit) (Syntax: 'input')

    Next Block[2]
Block[2] - Block
    Predecessors (1)
        [1]
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

    Next Block[4]
Block[3] - Block
    Predecessors (1)
        [1]
    Statements (1)
        IFlowCaptureOperation: 2 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'alternative')
          Value: 
            IParameterReferenceOperation: alternative (OperationKind.ParameterReference, Type: dynamic) (Syntax: 'alternative')

    Next Block[4]
Block[4] - Block
    Predecessors (2)
        [2]
        [3]
    Statements (1)
        IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'result = in ... lternative;')
          Expression: 
            ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: dynamic) (Syntax: 'result = in ... alternative')
              Left: 
                IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: dynamic, IsImplicit) (Syntax: 'result')
              Right: 
                IFlowCaptureReferenceOperation: 2 (OperationKind.FlowCaptureReference, Type: dynamic, IsImplicit) (Syntax: 'input ?? alternative')

    Next Block[5]
Block[5] - Exit
    Predecessors (1)
        [4]
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

            var compilation = CreateCompilationWithMscorlibAndSystemCore(source, references: new[] { CSharpRef }, parseOptions: TestOptions.RegularWithFlowAnalysisFeature);

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
Block[0] - Entry
    Statements (0)
    Next Block[1]
Block[1] - Block
    Predecessors (1)
        [0]
    Statements (2)
        IFlowCaptureOperation: 0 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'result')
          Value: 
            IParameterReferenceOperation: result (OperationKind.ParameterReference, Type: dynamic) (Syntax: 'result')

        IFlowCaptureOperation: 1 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'null')
          Value: 
            ILiteralOperation (OperationKind.Literal, Type: null, Constant: null) (Syntax: 'null')

    Jump if Null to Block[3]
        IFlowCaptureReferenceOperation: 1 (OperationKind.FlowCaptureReference, Type: null, Constant: null, IsImplicit) (Syntax: 'null')

    Next Block[2]
Block[2] - Block
    Predecessors (1)
        [1]
    Statements (1)
        IFlowCaptureOperation: 2 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'null')
          Value: 
            IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: dynamic, IsImplicit) (Syntax: 'null')
              Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: True, IsUserDefined: False) (MethodSymbol: null)
                (ImplicitReference)
              Operand: 
                IFlowCaptureReferenceOperation: 1 (OperationKind.FlowCaptureReference, Type: null, Constant: null, IsImplicit) (Syntax: 'null')

    Next Block[4]
Block[3] - Block
    Predecessors (1)
        [1]
    Statements (1)
        IFlowCaptureOperation: 2 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'alternative')
          Value: 
            IParameterReferenceOperation: alternative (OperationKind.ParameterReference, Type: dynamic) (Syntax: 'alternative')

    Next Block[4]
Block[4] - Block
    Predecessors (2)
        [2]
        [3]
    Statements (1)
        IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'result = nu ... lternative;')
          Expression: 
            ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: dynamic) (Syntax: 'result = nu ... alternative')
              Left: 
                IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: dynamic, IsImplicit) (Syntax: 'result')
              Right: 
                IFlowCaptureReferenceOperation: 2 (OperationKind.FlowCaptureReference, Type: dynamic, IsImplicit) (Syntax: 'null ?? alternative')

    Next Block[5]
Block[5] - Exit
    Predecessors (1)
        [4]
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

            var compilation = CreateStandardCompilation(source, parseOptions: TestOptions.RegularWithFlowAnalysisFeature);

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
Block[0] - Entry
    Statements (0)
    Next Block[1]
Block[1] - Block
    Predecessors (1)
        [0]
    Statements (2)
        IFlowCaptureOperation: 0 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'result')
          Value: 
            IParameterReferenceOperation: result (OperationKind.ParameterReference, Type: System.Int32) (Syntax: 'result')

        IFlowCaptureOperation: 1 (OperationKind.FlowCapture, Type: null, IsInvalid, IsImplicit) (Syntax: 'null')
          Value: 
            ILiteralOperation (OperationKind.Literal, Type: null, Constant: null, IsInvalid) (Syntax: 'null')

    Jump if Null to Block[3]
        IFlowCaptureReferenceOperation: 1 (OperationKind.FlowCaptureReference, Type: null, Constant: null, IsInvalid, IsImplicit) (Syntax: 'null')

    Next Block[2]
Block[2] - Block
    Predecessors (1)
        [1]
    Statements (1)
        IFlowCaptureOperation: 2 (OperationKind.FlowCapture, Type: null, IsInvalid, IsImplicit) (Syntax: 'null')
          Value: 
            IInvalidOperation (OperationKind.Invalid, Type: ?, IsInvalid, IsImplicit) (Syntax: 'null')
              Children(1):
                  IFlowCaptureReferenceOperation: 1 (OperationKind.FlowCaptureReference, Type: null, Constant: null, IsInvalid, IsImplicit) (Syntax: 'null')

    Next Block[4]
Block[3] - Block
    Predecessors (1)
        [1]
    Statements (1)
        IFlowCaptureOperation: 2 (OperationKind.FlowCapture, Type: null, IsInvalid, IsImplicit) (Syntax: 'alternative')
          Value: 
            IParameterReferenceOperation: alternative (OperationKind.ParameterReference, Type: System.Int32, IsInvalid) (Syntax: 'alternative')

    Next Block[4]
Block[4] - Block
    Predecessors (2)
        [2]
        [3]
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

    Next Block[5]
Block[5] - Exit
    Predecessors (1)
        [4]
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

            var compilation = CreateStandardCompilation(source, parseOptions: TestOptions.RegularWithFlowAnalysisFeature);

            compilation.VerifyDiagnostics();

            var tree = compilation.SyntaxTrees.Single();
            var node = tree.GetRoot().DescendantNodes().OfType<BinaryExpressionSyntax>().Single();

            compilation.VerifyOperationTree(node, expectedOperationTree:
@"
ICoalesceOperation (OperationKind.Coalesce, Type: System.Int32?) (Syntax: 'null ?? alternative')
  Expression: 
    ILiteralOperation (OperationKind.Literal, Type: null, Constant: null) (Syntax: 'null')
  ValueConversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
    (DefaultOrNullLiteral)
  WhenNull: 
    IParameterReferenceOperation: alternative (OperationKind.ParameterReference, Type: System.Int32?) (Syntax: 'alternative')
");

            string expectedGraph = @"
Block[0] - Entry
    Statements (0)
    Next Block[1]
Block[1] - Block
    Predecessors (1)
        [0]
    Statements (2)
        IFlowCaptureOperation: 0 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'result')
          Value: 
            IParameterReferenceOperation: result (OperationKind.ParameterReference, Type: System.Int32?) (Syntax: 'result')

        IFlowCaptureOperation: 1 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'null')
          Value: 
            ILiteralOperation (OperationKind.Literal, Type: null, Constant: null) (Syntax: 'null')

    Jump if Null to Block[3]
        IFlowCaptureReferenceOperation: 1 (OperationKind.FlowCaptureReference, Type: null, Constant: null, IsImplicit) (Syntax: 'null')

    Next Block[2]
Block[2] - Block
    Predecessors (1)
        [1]
    Statements (1)
        IFlowCaptureOperation: 2 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'null')
          Value: 
            IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.Int32?, IsImplicit) (Syntax: 'null')
              Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                (DefaultOrNullLiteral)
              Operand: 
                IFlowCaptureReferenceOperation: 1 (OperationKind.FlowCaptureReference, Type: null, Constant: null, IsImplicit) (Syntax: 'null')

    Next Block[4]
Block[3] - Block
    Predecessors (1)
        [1]
    Statements (1)
        IFlowCaptureOperation: 2 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'alternative')
          Value: 
            IParameterReferenceOperation: alternative (OperationKind.ParameterReference, Type: System.Int32?) (Syntax: 'alternative')

    Next Block[4]
Block[4] - Block
    Predecessors (2)
        [2]
        [3]
    Statements (1)
        IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'result = nu ... lternative;')
          Expression: 
            ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Int32?) (Syntax: 'result = nu ... alternative')
              Left: 
                IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: System.Int32?, IsImplicit) (Syntax: 'result')
              Right: 
                IFlowCaptureReferenceOperation: 2 (OperationKind.FlowCaptureReference, Type: System.Int32?, IsImplicit) (Syntax: 'null ?? alternative')

    Next Block[5]
Block[5] - Exit
    Predecessors (1)
        [4]
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

            var compilation = CreateStandardCompilation(source, parseOptions: TestOptions.RegularWithFlowAnalysisFeature);

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
Block[0] - Entry
    Statements (0)
    Next Block[1]
Block[1] - Block
    Predecessors (1)
        [0]
    Statements (2)
        IFlowCaptureOperation: 0 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'result')
          Value: 
            IParameterReferenceOperation: result (OperationKind.ParameterReference, Type: System.Int32?) (Syntax: 'result')

        IFlowCaptureOperation: 1 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'input')
          Value: 
            IParameterReferenceOperation: input (OperationKind.ParameterReference, Type: System.Int32?) (Syntax: 'input')

    Jump if Null to Block[3]
        IFlowCaptureReferenceOperation: 1 (OperationKind.FlowCaptureReference, Type: System.Int32?, IsImplicit) (Syntax: 'input')

    Next Block[2]
Block[2] - Block
    Predecessors (1)
        [1]
    Statements (1)
        IFlowCaptureOperation: 2 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'input')
          Value: 
            IFlowCaptureReferenceOperation: 1 (OperationKind.FlowCaptureReference, Type: System.Int32?, IsImplicit) (Syntax: 'input')

    Next Block[4]
Block[3] - Block
    Predecessors (1)
        [1]
    Statements (1)
        IFlowCaptureOperation: 2 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'alternative')
          Value: 
            IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.Int32?, IsImplicit) (Syntax: 'alternative')
              Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                (ImplicitNullable)
              Operand: 
                IParameterReferenceOperation: alternative (OperationKind.ParameterReference, Type: System.Byte?) (Syntax: 'alternative')

    Next Block[4]
Block[4] - Block
    Predecessors (2)
        [2]
        [3]
    Statements (1)
        IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'result = in ... lternative;')
          Expression: 
            ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Int32?) (Syntax: 'result = in ... alternative')
              Left: 
                IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: System.Int32?, IsImplicit) (Syntax: 'result')
              Right: 
                IFlowCaptureReferenceOperation: 2 (OperationKind.FlowCaptureReference, Type: System.Int32?, IsImplicit) (Syntax: 'input ?? alternative')

    Next Block[5]
Block[5] - Exit
    Predecessors (1)
        [4]
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

            var compilation = CreateStandardCompilation(source, parseOptions: TestOptions.RegularWithFlowAnalysisFeature);

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
Block[0] - Entry
    Statements (0)
    Next Block[1]
Block[1] - Block
    Predecessors (1)
        [0]
    Statements (2)
        IFlowCaptureOperation: 0 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'result')
          Value: 
            IParameterReferenceOperation: result (OperationKind.ParameterReference, Type: System.Int32?) (Syntax: 'result')

        IFlowCaptureOperation: 1 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'input')
          Value: 
            IParameterReferenceOperation: input (OperationKind.ParameterReference, Type: System.Int32?) (Syntax: 'input')

    Jump if Null to Block[3]
        IFlowCaptureReferenceOperation: 1 (OperationKind.FlowCaptureReference, Type: System.Int32?, IsImplicit) (Syntax: 'input')

    Next Block[2]
Block[2] - Block
    Predecessors (1)
        [1]
    Statements (1)
        IFlowCaptureOperation: 2 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'input')
          Value: 
            IFlowCaptureReferenceOperation: 1 (OperationKind.FlowCaptureReference, Type: System.Int32?, IsImplicit) (Syntax: 'input')

    Next Block[4]
Block[3] - Block
    Predecessors (1)
        [1]
    Statements (1)
        IFlowCaptureOperation: 2 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'alternative')
          Value: 
            IParameterReferenceOperation: alternative (OperationKind.ParameterReference, Type: System.Int32?) (Syntax: 'alternative')

    Next Block[4]
Block[4] - Block
    Predecessors (2)
        [2]
        [3]
    Statements (1)
        IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'result = in ... lternative;')
          Expression: 
            ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Int32?) (Syntax: 'result = in ... alternative')
              Left: 
                IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: System.Int32?, IsImplicit) (Syntax: 'result')
              Right: 
                IFlowCaptureReferenceOperation: 2 (OperationKind.FlowCaptureReference, Type: System.Int32?, IsImplicit) (Syntax: 'input ?? alternative')

    Next Block[5]
Block[5] - Exit
    Predecessors (1)
        [4]
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

            var compilation = CreateStandardCompilation(source, parseOptions: TestOptions.RegularWithFlowAnalysisFeature);

            compilation.VerifyDiagnostics();

            string expectedGraph = @"
Block[0] - Entry
    Statements (0)
    Next Block[1]
Block[1] - Block
    Predecessors (1)
        [0]
    Statements (2)
        IFlowCaptureOperation: 0 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'result')
          Value: 
            IParameterReferenceOperation: result (OperationKind.ParameterReference, Type: System.Int32?) (Syntax: 'result')

        IFlowCaptureOperation: 1 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'input1')
          Value: 
            IParameterReferenceOperation: input1 (OperationKind.ParameterReference, Type: System.Int32?) (Syntax: 'input1')

    Jump if Null to Block[3]
        IFlowCaptureReferenceOperation: 1 (OperationKind.FlowCaptureReference, Type: System.Int32?, IsImplicit) (Syntax: 'input1')

    Next Block[2]
Block[2] - Block
    Predecessors (1)
        [1]
    Statements (1)
        IFlowCaptureOperation: 2 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'input1')
          Value: 
            IFlowCaptureReferenceOperation: 1 (OperationKind.FlowCaptureReference, Type: System.Int32?, IsImplicit) (Syntax: 'input1')

    Next Block[4]
Block[3] - Block
    Predecessors (1)
        [1]
    Statements (1)
        IFlowCaptureOperation: 2 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'alternative1')
          Value: 
            IParameterReferenceOperation: alternative1 (OperationKind.ParameterReference, Type: System.Int32?) (Syntax: 'alternative1')

    Next Block[4]
Block[4] - Block
    Predecessors (2)
        [2]
        [3]
    Statements (0)
    Jump if Null to Block[6]
        IFlowCaptureReferenceOperation: 2 (OperationKind.FlowCaptureReference, Type: System.Int32?, IsImplicit) (Syntax: 'input1 ?? alternative1')

    Next Block[5]
Block[5] - Block
    Predecessors (1)
        [4]
    Statements (1)
        IFlowCaptureOperation: 3 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'input1 ?? alternative1')
          Value: 
            IFlowCaptureReferenceOperation: 2 (OperationKind.FlowCaptureReference, Type: System.Int32?, IsImplicit) (Syntax: 'input1 ?? alternative1')

    Next Block[9]
Block[6] - Block
    Predecessors (1)
        [4]
    Statements (1)
        IFlowCaptureOperation: 4 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'input2')
          Value: 
            IParameterReferenceOperation: input2 (OperationKind.ParameterReference, Type: System.Int32?) (Syntax: 'input2')

    Jump if Null to Block[8]
        IFlowCaptureReferenceOperation: 4 (OperationKind.FlowCaptureReference, Type: System.Int32?, IsImplicit) (Syntax: 'input2')

    Next Block[7]
Block[7] - Block
    Predecessors (1)
        [6]
    Statements (1)
        IFlowCaptureOperation: 3 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'input2')
          Value: 
            IFlowCaptureReferenceOperation: 4 (OperationKind.FlowCaptureReference, Type: System.Int32?, IsImplicit) (Syntax: 'input2')

    Next Block[9]
Block[8] - Block
    Predecessors (1)
        [6]
    Statements (1)
        IFlowCaptureOperation: 3 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'alternative2')
          Value: 
            IParameterReferenceOperation: alternative2 (OperationKind.ParameterReference, Type: System.Int32?) (Syntax: 'alternative2')

    Next Block[9]
Block[9] - Block
    Predecessors (3)
        [5]
        [7]
        [8]
    Statements (1)
        IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'result = (i ... ernative2);')
          Expression: 
            ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Int32?) (Syntax: 'result = (i ... ternative2)')
              Left: 
                IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: System.Int32?, IsImplicit) (Syntax: 'result')
              Right: 
                IFlowCaptureReferenceOperation: 3 (OperationKind.FlowCaptureReference, Type: System.Int32?, IsImplicit) (Syntax: '(input1 ??  ... ternative2)')

    Next Block[10]
Block[10] - Exit
    Predecessors (1)
        [9]
    Statements (0)
";
            VerifyFlowGraphForTest<BlockSyntax>(compilation, expectedGraph);
        }
    }
}
