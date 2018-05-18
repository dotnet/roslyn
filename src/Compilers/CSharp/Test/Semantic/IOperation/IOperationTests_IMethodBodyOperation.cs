// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Linq;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.CSharp.Test.Utilities;
using Microsoft.CodeAnalysis.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.CSharp.UnitTests
{
    [CompilerTrait(CompilerFeature.IOperation)]
    public partial class IOperationTests : SemanticModelTestBase
    {
        [Fact]
        public void RegularMethodBody_01()
        {
            string source = @"
abstract class C
{
    public abstract void M();
}
";
            var compilation = CreateCompilation(source);

            compilation.VerifyDiagnostics();

            var tree = compilation.SyntaxTrees.Single();
            var model = compilation.GetSemanticModel(tree);

            var node1 = tree.GetRoot().DescendantNodes().OfType<BaseMethodDeclarationSyntax>().Single();
            Assert.Null(model.GetOperation(node1));
        }

        [CompilerTrait(CompilerFeature.Dataflow)]
        [Fact]
        public void RegularMethodBody_02()
        {
            string source = @"
class C
{
    public void M()
    { throw null; }
}
";
            var compilation = CreateCompilation(source, parseOptions: TestOptions.RegularWithFlowAnalysisFeature);

            compilation.VerifyDiagnostics();

            var tree = compilation.SyntaxTrees.Single();
            var node1 = tree.GetRoot().DescendantNodes().OfType<BaseMethodDeclarationSyntax>().Single();

            compilation.VerifyOperationTree(node1, expectedOperationTree:
@"
IMethodBodyOperation (OperationKind.MethodBodyOperation, Type: null) (Syntax: 'public void ... row null; }')
  BlockBody: 
    IBlockOperation (1 statements) (OperationKind.Block, Type: null) (Syntax: '{ throw null; }')
      IThrowOperation (OperationKind.Throw, Type: null) (Syntax: 'throw null;')
        ILiteralOperation (OperationKind.Literal, Type: null, Constant: null) (Syntax: 'null')
  ExpressionBody: 
    null
");

            VerifyFlowGraph(compilation, node1, expectedFlowGraph:
@"
Block[B0] - Entry
    Statements (0)
    Next (Regular) Block[B1]
Block[B1] - Block
    Predecessors: [B0]
    Statements (0)
    Next (Throw) Block[null]
        ILiteralOperation (OperationKind.Literal, Type: null, Constant: null) (Syntax: 'null')
Block[B2] - Exit [UnReachable]
    Predecessors (0)
    Statements (0)
");
        }

        [CompilerTrait(CompilerFeature.Dataflow)]
        [Fact]
        public void RegularMethodBody_03()
        {
            string source = @"
class C
{
    public void M() 
    => throw null;
}
";
            var compilation = CreateCompilation(source, parseOptions: TestOptions.RegularWithFlowAnalysisFeature);

            compilation.VerifyDiagnostics();

            var tree = compilation.SyntaxTrees.Single();
            var node1 = tree.GetRoot().DescendantNodes().OfType<BaseMethodDeclarationSyntax>().Single();

            compilation.VerifyOperationTree(node1, expectedOperationTree:
@"
IMethodBodyOperation (OperationKind.MethodBodyOperation, Type: null) (Syntax: 'public void ... throw null;')
  BlockBody: 
    null
  ExpressionBody: 
    IBlockOperation (1 statements) (OperationKind.Block, Type: null) (Syntax: '=> throw null')
      IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null, IsImplicit) (Syntax: 'throw null')
        Expression: 
          IThrowOperation (OperationKind.Throw, Type: null) (Syntax: 'throw null')
            ILiteralOperation (OperationKind.Literal, Type: null, Constant: null) (Syntax: 'null')
");

            VerifyFlowGraph(compilation, node1, expectedFlowGraph:
@"
Block[B0] - Entry
    Statements (0)
    Next (Regular) Block[B1]
Block[B1] - Block
    Predecessors: [B0]
    Statements (0)
    Next (Throw) Block[null]
        ILiteralOperation (OperationKind.Literal, Type: null, Constant: null) (Syntax: 'null')
Block[B2] - Block [UnReachable]
    Predecessors (0)
    Statements (1)
        IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null, IsImplicit) (Syntax: 'throw null')
          Expression: 
            IOperation:  (OperationKind.None, Type: null, IsImplicit) (Syntax: 'throw null')

    Next (Regular) Block[B3]
Block[B3] - Exit [UnReachable]
    Predecessors: [B2]
    Statements (0)
");
        }

        [CompilerTrait(CompilerFeature.Dataflow)]
        [Fact]
        public void RegularMethodBody_04()
        {
            string source = @"
class C
{
    public void M()
    { throw null; }
    => throw null;
}
";
            var compilation = CreateCompilation(source, parseOptions: TestOptions.RegularWithFlowAnalysisFeature);

            compilation.VerifyDiagnostics(
                // (4,5): error CS8057: Block bodies and expression bodies cannot both be provided.
                //     public void M()
                Diagnostic(ErrorCode.ERR_BlockBodyAndExpressionBody, @"public void M()
    { throw null; }
    => throw null;").WithLocation(4, 5)
                );

            var tree = compilation.SyntaxTrees.Single();
            var node1 = tree.GetRoot().DescendantNodes().OfType<BaseMethodDeclarationSyntax>().Single();

            compilation.VerifyOperationTree(node1, expectedOperationTree:
@"
IMethodBodyOperation (OperationKind.MethodBodyOperation, Type: null, IsInvalid) (Syntax: 'public void ... throw null;')
  BlockBody: 
    IBlockOperation (1 statements) (OperationKind.Block, Type: null, IsInvalid) (Syntax: '{ throw null; }')
      IThrowOperation (OperationKind.Throw, Type: null, IsInvalid) (Syntax: 'throw null;')
        ILiteralOperation (OperationKind.Literal, Type: null, Constant: null, IsInvalid) (Syntax: 'null')
  ExpressionBody: 
    IBlockOperation (1 statements) (OperationKind.Block, Type: null, IsInvalid) (Syntax: '=> throw null')
      IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null, IsInvalid, IsImplicit) (Syntax: 'throw null')
        Expression: 
          IThrowOperation (OperationKind.Throw, Type: null, IsInvalid) (Syntax: 'throw null')
            ILiteralOperation (OperationKind.Literal, Type: null, Constant: null, IsInvalid) (Syntax: 'null')
");

            VerifyFlowGraph(compilation, node1, expectedFlowGraph:
@"
Block[B0] - Entry
    Statements (0)
    Next (Regular) Block[B1]
Block[B1] - Block
    Predecessors: [B0]
    Statements (0)
    Next (Throw) Block[null]
        ILiteralOperation (OperationKind.Literal, Type: null, Constant: null, IsInvalid) (Syntax: 'null')

.erroneous body {R1}
{
    Block[B2] - Block [UnReachable]
        Predecessors (0)
        Statements (0)
        Next (Throw) Block[null]
            ILiteralOperation (OperationKind.Literal, Type: null, Constant: null, IsInvalid) (Syntax: 'null')
    Block[B3] - Block [UnReachable]
        Predecessors (0)
        Statements (1)
            IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null, IsInvalid, IsImplicit) (Syntax: 'throw null')
              Expression: 
                IOperation:  (OperationKind.None, Type: null, IsInvalid, IsImplicit) (Syntax: 'throw null')

        Next (Regular) Block[B4]
            Leaving: {R1}
}

Block[B4] - Exit [UnReachable]
    Predecessors: [B3]
    Statements (0)
");
        }

        [CompilerTrait(CompilerFeature.Dataflow)]
        [Fact]
        public void RegularMethodBody_05()
        {
            string source = @"
class C
{
    public void M(int i, int j)
    { j = i; }
}
";
            var compilation = CreateCompilation(source, parseOptions: TestOptions.RegularWithFlowAnalysisFeature);

            compilation.VerifyDiagnostics();

            var tree = compilation.SyntaxTrees.Single();
            var node1 = tree.GetRoot().DescendantNodes().OfType<BaseMethodDeclarationSyntax>().Single();

            compilation.VerifyOperationTree(node1, expectedOperationTree:
@"
IMethodBodyOperation (OperationKind.MethodBodyOperation, Type: null) (Syntax: 'public void ... { j = i; }')
  BlockBody: 
    IBlockOperation (1 statements) (OperationKind.Block, Type: null) (Syntax: '{ j = i; }')
      IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'j = i;')
        Expression: 
          ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Int32) (Syntax: 'j = i')
            Left: 
              IParameterReferenceOperation: j (OperationKind.ParameterReference, Type: System.Int32) (Syntax: 'j')
            Right: 
              IParameterReferenceOperation: i (OperationKind.ParameterReference, Type: System.Int32) (Syntax: 'i')
  ExpressionBody: 
    null
");

            VerifyFlowGraph(compilation, node1, expectedFlowGraph:
@"
Block[B0] - Entry
    Statements (0)
    Next (Regular) Block[B1]
Block[B1] - Block
    Predecessors: [B0]
    Statements (1)
        IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'j = i;')
          Expression: 
            ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Int32) (Syntax: 'j = i')
              Left: 
                IParameterReferenceOperation: j (OperationKind.ParameterReference, Type: System.Int32) (Syntax: 'j')
              Right: 
                IParameterReferenceOperation: i (OperationKind.ParameterReference, Type: System.Int32) (Syntax: 'i')

    Next (Regular) Block[B2]
Block[B2] - Exit
    Predecessors: [B1]
    Statements (0)
");
        }

        [CompilerTrait(CompilerFeature.Dataflow)]
        [Fact]
        public void RegularMethodBody_06()
        {
            string source = @"
class C
{
    public void M(int i, int j)
    => j = i;
}
";
            var compilation = CreateCompilation(source, parseOptions: TestOptions.RegularWithFlowAnalysisFeature);

            compilation.VerifyDiagnostics();

            var tree = compilation.SyntaxTrees.Single();
            var node1 = tree.GetRoot().DescendantNodes().OfType<BaseMethodDeclarationSyntax>().Single();

            compilation.VerifyOperationTree(node1, expectedOperationTree:
@"
IMethodBodyOperation (OperationKind.MethodBodyOperation, Type: null) (Syntax: 'public void ... => j = i;')
  BlockBody: 
    null
  ExpressionBody: 
    IBlockOperation (1 statements) (OperationKind.Block, Type: null) (Syntax: '=> j = i')
      IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null, IsImplicit) (Syntax: 'j = i')
        Expression: 
          ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Int32) (Syntax: 'j = i')
            Left: 
              IParameterReferenceOperation: j (OperationKind.ParameterReference, Type: System.Int32) (Syntax: 'j')
            Right: 
              IParameterReferenceOperation: i (OperationKind.ParameterReference, Type: System.Int32) (Syntax: 'i')
");

            VerifyFlowGraph(compilation, node1, expectedFlowGraph:
@"
Block[B0] - Entry
    Statements (0)
    Next (Regular) Block[B1]
Block[B1] - Block
    Predecessors: [B0]
    Statements (1)
        IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null, IsImplicit) (Syntax: 'j = i')
          Expression: 
            ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Int32) (Syntax: 'j = i')
              Left: 
                IParameterReferenceOperation: j (OperationKind.ParameterReference, Type: System.Int32) (Syntax: 'j')
              Right: 
                IParameterReferenceOperation: i (OperationKind.ParameterReference, Type: System.Int32) (Syntax: 'i')

    Next (Regular) Block[B2]
Block[B2] - Exit
    Predecessors: [B1]
    Statements (0)
");
        }

        [CompilerTrait(CompilerFeature.Dataflow)]
        [Fact]
        public void RegularMethodBody_07()
        {
            string source = @"
class C
{
    public void M(int i, int j)
    { j = i; }
    => j = i;
}
";
            var compilation = CreateCompilation(source, parseOptions: TestOptions.RegularWithFlowAnalysisFeature);

            compilation.VerifyDiagnostics(
                // (4,5): error CS8057: Block bodies and expression bodies cannot both be provided.
                //     public void M(int i, int j)
                Diagnostic(ErrorCode.ERR_BlockBodyAndExpressionBody, @"public void M(int i, int j)
    { j = i; }
    => j = i;").WithLocation(4, 5));

            var tree = compilation.SyntaxTrees.Single();
            var node1 = tree.GetRoot().DescendantNodes().OfType<BaseMethodDeclarationSyntax>().Single();

            compilation.VerifyOperationTree(node1, expectedOperationTree:
@"
IMethodBodyOperation (OperationKind.MethodBodyOperation, Type: null, IsInvalid) (Syntax: 'public void ... => j = i;')
  BlockBody: 
    IBlockOperation (1 statements) (OperationKind.Block, Type: null, IsInvalid) (Syntax: '{ j = i; }')
      IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null, IsInvalid) (Syntax: 'j = i;')
        Expression: 
          ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Int32, IsInvalid) (Syntax: 'j = i')
            Left: 
              IParameterReferenceOperation: j (OperationKind.ParameterReference, Type: System.Int32, IsInvalid) (Syntax: 'j')
            Right: 
              IParameterReferenceOperation: i (OperationKind.ParameterReference, Type: System.Int32, IsInvalid) (Syntax: 'i')
  ExpressionBody: 
    IBlockOperation (1 statements) (OperationKind.Block, Type: null, IsInvalid) (Syntax: '=> j = i')
      IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null, IsInvalid, IsImplicit) (Syntax: 'j = i')
        Expression: 
          ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Int32, IsInvalid) (Syntax: 'j = i')
            Left: 
              IParameterReferenceOperation: j (OperationKind.ParameterReference, Type: System.Int32, IsInvalid) (Syntax: 'j')
            Right: 
              IParameterReferenceOperation: i (OperationKind.ParameterReference, Type: System.Int32, IsInvalid) (Syntax: 'i')
");

            VerifyFlowGraph(compilation, node1, expectedFlowGraph:
@"
Block[B0] - Entry
    Statements (0)
    Next (Regular) Block[B1]
Block[B1] - Block
    Predecessors: [B0]
    Statements (1)
        IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null, IsInvalid) (Syntax: 'j = i;')
          Expression: 
            ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Int32, IsInvalid) (Syntax: 'j = i')
              Left: 
                IParameterReferenceOperation: j (OperationKind.ParameterReference, Type: System.Int32, IsInvalid) (Syntax: 'j')
              Right: 
                IParameterReferenceOperation: i (OperationKind.ParameterReference, Type: System.Int32, IsInvalid) (Syntax: 'i')

    Next (Regular) Block[B3]

.erroneous body {R1}
{
    Block[B2] - Block [UnReachable]
        Predecessors (0)
        Statements (1)
            IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null, IsInvalid, IsImplicit) (Syntax: 'j = i')
              Expression: 
                ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Int32, IsInvalid) (Syntax: 'j = i')
                  Left: 
                    IParameterReferenceOperation: j (OperationKind.ParameterReference, Type: System.Int32, IsInvalid) (Syntax: 'j')
                  Right: 
                    IParameterReferenceOperation: i (OperationKind.ParameterReference, Type: System.Int32, IsInvalid) (Syntax: 'i')

        Next (Regular) Block[B3]
            Leaving: {R1}
}

Block[B3] - Exit
    Predecessors: [B1] [B2]
    Statements (0)
");
        }

        [CompilerTrait(CompilerFeature.Dataflow)]
        [Fact]
        public void RegularMethodBody_08()
        {
            string source = @"
class C
{
    public void M()
    { return; }
    => throw null;
}
";
            var compilation = CreateCompilation(source, parseOptions: TestOptions.RegularWithFlowAnalysisFeature);

            compilation.VerifyDiagnostics(
                // (4,5): error CS8057: Block bodies and expression bodies cannot both be provided.
                //     public void M()
                Diagnostic(ErrorCode.ERR_BlockBodyAndExpressionBody, @"public void M()
    { return; }
    => throw null;").WithLocation(4, 5));

            var tree = compilation.SyntaxTrees.Single();
            var node1 = tree.GetRoot().DescendantNodes().OfType<BaseMethodDeclarationSyntax>().Single();

            compilation.VerifyOperationTree(node1, expectedOperationTree:
@"
IMethodBodyOperation (OperationKind.MethodBodyOperation, Type: null, IsInvalid) (Syntax: 'public void ... throw null;')
  BlockBody: 
    IBlockOperation (1 statements) (OperationKind.Block, Type: null, IsInvalid) (Syntax: '{ return; }')
      IReturnOperation (OperationKind.Return, Type: null, IsInvalid) (Syntax: 'return;')
        ReturnedValue: 
          null
  ExpressionBody: 
    IBlockOperation (1 statements) (OperationKind.Block, Type: null, IsInvalid) (Syntax: '=> throw null')
      IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null, IsInvalid, IsImplicit) (Syntax: 'throw null')
        Expression: 
          IThrowOperation (OperationKind.Throw, Type: null, IsInvalid) (Syntax: 'throw null')
            ILiteralOperation (OperationKind.Literal, Type: null, Constant: null, IsInvalid) (Syntax: 'null')
");

            VerifyFlowGraph(compilation, node1, expectedFlowGraph:
@"
Block[B0] - Entry
    Statements (0)
    Next (Regular) Block[B3]

.erroneous body {R1}
{
    Block[B1] - Block [UnReachable]
        Predecessors (0)
        Statements (0)
        Next (Throw) Block[null]
            ILiteralOperation (OperationKind.Literal, Type: null, Constant: null, IsInvalid) (Syntax: 'null')
    Block[B2] - Block [UnReachable]
        Predecessors (0)
        Statements (1)
            IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null, IsInvalid, IsImplicit) (Syntax: 'throw null')
              Expression: 
                IOperation:  (OperationKind.None, Type: null, IsInvalid, IsImplicit) (Syntax: 'throw null')

        Next (Regular) Block[B3]
            Leaving: {R1}
}

Block[B3] - Exit
    Predecessors: [B0] [B2]
    Statements (0)
");
        }

        [CompilerTrait(CompilerFeature.Dataflow)]
        [Fact]
        public void RegularMethodBody_09()
        {
            string source = @"
class C
{
    public void M()
    { }
}
";
            var compilation = CreateCompilation(source, parseOptions: TestOptions.RegularWithFlowAnalysisFeature);

            compilation.VerifyDiagnostics();

            var tree = compilation.SyntaxTrees.Single();
            var node1 = tree.GetRoot().DescendantNodes().OfType<BaseMethodDeclarationSyntax>().Single();

            compilation.VerifyOperationTree(node1, expectedOperationTree:
@"
IMethodBodyOperation (OperationKind.MethodBodyOperation, Type: null) (Syntax: 'public void ... { }')
  BlockBody: 
    IBlockOperation (0 statements) (OperationKind.Block, Type: null) (Syntax: '{ }')
  ExpressionBody: 
    null
");

            VerifyFlowGraph(compilation, node1, expectedFlowGraph:
@"
Block[B0] - Entry
    Statements (0)
    Next (Regular) Block[B1]
Block[B1] - Exit
    Predecessors: [B0]
    Statements (0)
");
        }

        [CompilerTrait(CompilerFeature.Dataflow)]
        [Fact]
        public void RegularMethodBody_10()
        {
            string source = @"
class C
{
    public void M()
    { }
    => M2(out int x);

    void M2(out int x) => x = 0;
}
";
            var compilation = CreateCompilation(source, parseOptions: TestOptions.RegularWithFlowAnalysisFeature);

            compilation.VerifyDiagnostics(
                // (4,5): error CS8057: Block bodies and expression bodies cannot both be provided.
                //     public void M()
                Diagnostic(ErrorCode.ERR_BlockBodyAndExpressionBody, @"public void M()
    { }
    => M2(out int x);").WithLocation(4, 5));

            var tree = compilation.SyntaxTrees.Single();
            var node1 = tree.GetRoot().DescendantNodes().OfType<BaseMethodDeclarationSyntax>().First();

            compilation.VerifyOperationTree(node1, expectedOperationTree:
@"
IMethodBodyOperation (OperationKind.MethodBodyOperation, Type: null, IsInvalid) (Syntax: 'public void ... out int x);')
  BlockBody: 
    IBlockOperation (0 statements) (OperationKind.Block, Type: null, IsInvalid) (Syntax: '{ }')
  ExpressionBody: 
    IBlockOperation (1 statements, 1 locals) (OperationKind.Block, Type: null, IsInvalid) (Syntax: '=> M2(out int x)')
      Locals: Local_1: System.Int32 x
      IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null, IsInvalid, IsImplicit) (Syntax: 'M2(out int x)')
        Expression: 
          IInvocationOperation ( void C.M2(out System.Int32 x)) (OperationKind.Invocation, Type: System.Void, IsInvalid) (Syntax: 'M2(out int x)')
            Instance Receiver: 
              IInstanceReferenceOperation (ReferenceKind: ContainingTypeInstance) (OperationKind.InstanceReference, Type: C, IsInvalid, IsImplicit) (Syntax: 'M2')
            Arguments(1):
                IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: x) (OperationKind.Argument, Type: null, IsInvalid) (Syntax: 'out int x')
                  IDeclarationExpressionOperation (OperationKind.DeclarationExpression, Type: System.Int32, IsInvalid) (Syntax: 'int x')
                    ILocalReferenceOperation: x (IsDeclaration: True) (OperationKind.LocalReference, Type: System.Int32, IsInvalid) (Syntax: 'x')
                  InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                  OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
");

            VerifyFlowGraph(compilation, node1, expectedFlowGraph:
@"
Block[B0] - Entry
    Statements (0)
    Next (Regular) Block[B2]

.erroneous body {R1}
{
    Locals: [System.Int32 x]
    Block[B1] - Block [UnReachable]
        Predecessors (0)
        Statements (1)
            IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null, IsInvalid, IsImplicit) (Syntax: 'M2(out int x)')
              Expression: 
                IInvocationOperation ( void C.M2(out System.Int32 x)) (OperationKind.Invocation, Type: System.Void, IsInvalid) (Syntax: 'M2(out int x)')
                  Instance Receiver: 
                    IInstanceReferenceOperation (ReferenceKind: ContainingTypeInstance) (OperationKind.InstanceReference, Type: C, IsInvalid, IsImplicit) (Syntax: 'M2')
                  Arguments(1):
                      IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: x) (OperationKind.Argument, Type: null, IsInvalid) (Syntax: 'out int x')
                        IDeclarationExpressionOperation (OperationKind.DeclarationExpression, Type: System.Int32, IsInvalid) (Syntax: 'int x')
                          ILocalReferenceOperation: x (IsDeclaration: True) (OperationKind.LocalReference, Type: System.Int32, IsInvalid) (Syntax: 'x')
                        InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                        OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)

        Next (Regular) Block[B2]
            Leaving: {R1}
}

Block[B2] - Exit
    Predecessors: [B0] [B1]
    Statements (0)
");
        }

        [Fact]
        public void OperatorBody_01()
        {
            string source = @"
abstract class C
{
    public static C operator ! (C x);
}
";
            var compilation = CreateCompilation(source);

            compilation.VerifyDiagnostics(
                // (4,30): error CS0501: 'C.operator !(C)' must declare a body because it is not marked abstract, extern, or partial
                //     public static C operator ! (C x);
                Diagnostic(ErrorCode.ERR_ConcreteMissingBody, "!").WithArguments("C.operator !(C)").WithLocation(4, 30)
                );

            var tree = compilation.SyntaxTrees.Single();
            var model = compilation.GetSemanticModel(tree);

            var node1 = tree.GetRoot().DescendantNodes().OfType<BaseMethodDeclarationSyntax>().Single();
            Assert.Null(model.GetOperation(node1));
        }

        [Fact]
        public void OperatorMethodBody_02()
        {
            string source = @"
class C
{
    public static C operator ! (C x)
    { throw null; }
}
";
            var compilation = CreateCompilation(source);

            compilation.VerifyDiagnostics();

            var tree = compilation.SyntaxTrees.Single();
            var node1 = tree.GetRoot().DescendantNodes().OfType<BaseMethodDeclarationSyntax>().Single();

            compilation.VerifyOperationTree(node1, expectedOperationTree:
@"
IMethodBodyOperation (OperationKind.MethodBodyOperation, Type: null) (Syntax: 'public stat ... row null; }')
  BlockBody: 
    IBlockOperation (1 statements) (OperationKind.Block, Type: null) (Syntax: '{ throw null; }')
      IThrowOperation (OperationKind.Throw, Type: null) (Syntax: 'throw null;')
        ILiteralOperation (OperationKind.Literal, Type: null, Constant: null) (Syntax: 'null')
  ExpressionBody: 
    null
");
        }

        [Fact]
        public void OperatorMethodBody_03()
        {
            string source = @"
class C
{
    public static C operator ! (C x)
    => throw null;
}
";
            var compilation = CreateCompilation(source);

            compilation.VerifyDiagnostics();

            var tree = compilation.SyntaxTrees.Single();
            var node1 = tree.GetRoot().DescendantNodes().OfType<BaseMethodDeclarationSyntax>().Single();

            compilation.VerifyOperationTree(node1, expectedOperationTree:
@"
IMethodBodyOperation (OperationKind.MethodBodyOperation, Type: null) (Syntax: 'public stat ... throw null;')
  BlockBody: 
    null
  ExpressionBody: 
    IBlockOperation (1 statements) (OperationKind.Block, Type: null) (Syntax: '=> throw null')
      IReturnOperation (OperationKind.Return, Type: null, IsImplicit) (Syntax: 'throw null')
        ReturnedValue: 
          IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: C, IsImplicit) (Syntax: 'throw null')
            Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
            Operand: 
              IThrowOperation (OperationKind.Throw, Type: null) (Syntax: 'throw null')
                ILiteralOperation (OperationKind.Literal, Type: null, Constant: null) (Syntax: 'null')
");
        }

        [Fact]
        public void OperatorMethodBody_04()
        {
            string source = @"
class C
{
    public static C operator ! (C x)
    { throw null; }
    => throw null;
}
";
            var compilation = CreateCompilation(source);

            compilation.VerifyDiagnostics(
                // (4,5): error CS8057: Block bodies and expression bodies cannot both be provided.
                //     public static C operator ! (C x)
                Diagnostic(ErrorCode.ERR_BlockBodyAndExpressionBody, @"public static C operator ! (C x)
    { throw null; }
    => throw null;").WithLocation(4, 5)
                );

            var tree = compilation.SyntaxTrees.Single();
            var node1 = tree.GetRoot().DescendantNodes().OfType<BaseMethodDeclarationSyntax>().Single();

            compilation.VerifyOperationTree(node1, expectedOperationTree:
@"
IMethodBodyOperation (OperationKind.MethodBodyOperation, Type: null, IsInvalid) (Syntax: 'public stat ... throw null;')
  BlockBody: 
    IBlockOperation (1 statements) (OperationKind.Block, Type: null, IsInvalid) (Syntax: '{ throw null; }')
      IThrowOperation (OperationKind.Throw, Type: null, IsInvalid) (Syntax: 'throw null;')
        ILiteralOperation (OperationKind.Literal, Type: null, Constant: null, IsInvalid) (Syntax: 'null')
  ExpressionBody: 
    IBlockOperation (1 statements) (OperationKind.Block, Type: null, IsInvalid) (Syntax: '=> throw null')
      IReturnOperation (OperationKind.Return, Type: null, IsInvalid, IsImplicit) (Syntax: 'throw null')
        ReturnedValue: 
          IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: C, IsInvalid, IsImplicit) (Syntax: 'throw null')
            Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
            Operand: 
              IThrowOperation (OperationKind.Throw, Type: null, IsInvalid) (Syntax: 'throw null')
                ILiteralOperation (OperationKind.Literal, Type: null, Constant: null, IsInvalid) (Syntax: 'null')
");
        }

        [Fact]
        public void ConversionBody_01()
        {
            string source = @"
abstract class C
{
    public static implicit operator int(C x);
}
";
            var compilation = CreateCompilation(source);

            compilation.VerifyDiagnostics(
                // (4,37): error CS0501: 'C.implicit operator int(C)' must declare a body because it is not marked abstract, extern, or partial
                //     public static implicit operator int(C x);
                Diagnostic(ErrorCode.ERR_ConcreteMissingBody, "int").WithArguments("C.implicit operator int(C)").WithLocation(4, 37)
                );

            var tree = compilation.SyntaxTrees.Single();
            var model = compilation.GetSemanticModel(tree);

            var node1 = tree.GetRoot().DescendantNodes().OfType<BaseMethodDeclarationSyntax>().Single();
            Assert.Null(model.GetOperation(node1));
        }

        [Fact]
        public void ConversionMethodBody_02()
        {
            string source = @"
class C
{
    public static implicit operator int(C x)
    { throw null; }
}
";
            var compilation = CreateCompilation(source);

            compilation.VerifyDiagnostics();

            var tree = compilation.SyntaxTrees.Single();
            var node1 = tree.GetRoot().DescendantNodes().OfType<BaseMethodDeclarationSyntax>().Single();

            compilation.VerifyOperationTree(node1, expectedOperationTree:
@"
IMethodBodyOperation (OperationKind.MethodBodyOperation, Type: null) (Syntax: 'public stat ... row null; }')
  BlockBody: 
    IBlockOperation (1 statements) (OperationKind.Block, Type: null) (Syntax: '{ throw null; }')
      IThrowOperation (OperationKind.Throw, Type: null) (Syntax: 'throw null;')
        ILiteralOperation (OperationKind.Literal, Type: null, Constant: null) (Syntax: 'null')
  ExpressionBody: 
    null
");
        }

        [Fact]
        public void ConversionMethodBody_03()
        {
            string source = @"
class C
{
    public static implicit operator int(C x)
    => throw null;
}
";
            var compilation = CreateCompilation(source);

            compilation.VerifyDiagnostics();

            var tree = compilation.SyntaxTrees.Single();
            var node1 = tree.GetRoot().DescendantNodes().OfType<BaseMethodDeclarationSyntax>().Single();

            compilation.VerifyOperationTree(node1, expectedOperationTree:
@"
IMethodBodyOperation (OperationKind.MethodBodyOperation, Type: null) (Syntax: 'public stat ... throw null;')
  BlockBody: 
    null
  ExpressionBody: 
    IBlockOperation (1 statements) (OperationKind.Block, Type: null) (Syntax: '=> throw null')
      IReturnOperation (OperationKind.Return, Type: null, IsImplicit) (Syntax: 'throw null')
        ReturnedValue: 
          IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.Int32, IsImplicit) (Syntax: 'throw null')
            Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
            Operand: 
              IThrowOperation (OperationKind.Throw, Type: null) (Syntax: 'throw null')
                ILiteralOperation (OperationKind.Literal, Type: null, Constant: null) (Syntax: 'null')
");
        }

        [Fact]
        public void ConversionMethodBody_04()
        {
            string source = @"
class C
{
    public static implicit operator int(C x)
    { throw null; }
    => throw null;
}
";
            var compilation = CreateCompilation(source);

            compilation.VerifyDiagnostics(
                // (4,5): error CS8057: Block bodies and expression bodies cannot both be provided.
                //     public static implicit operator int(C x)
                Diagnostic(ErrorCode.ERR_BlockBodyAndExpressionBody, @"public static implicit operator int(C x)
    { throw null; }
    => throw null;").WithLocation(4, 5)
                );

            var tree = compilation.SyntaxTrees.Single();
            var node1 = tree.GetRoot().DescendantNodes().OfType<BaseMethodDeclarationSyntax>().Single();

            compilation.VerifyOperationTree(node1, expectedOperationTree:
@"
IMethodBodyOperation (OperationKind.MethodBodyOperation, Type: null, IsInvalid) (Syntax: 'public stat ... throw null;')
  BlockBody: 
    IBlockOperation (1 statements) (OperationKind.Block, Type: null, IsInvalid) (Syntax: '{ throw null; }')
      IThrowOperation (OperationKind.Throw, Type: null, IsInvalid) (Syntax: 'throw null;')
        ILiteralOperation (OperationKind.Literal, Type: null, Constant: null, IsInvalid) (Syntax: 'null')
  ExpressionBody: 
    IBlockOperation (1 statements) (OperationKind.Block, Type: null, IsInvalid) (Syntax: '=> throw null')
      IReturnOperation (OperationKind.Return, Type: null, IsInvalid, IsImplicit) (Syntax: 'throw null')
        ReturnedValue: 
          IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.Int32, IsInvalid, IsImplicit) (Syntax: 'throw null')
            Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
            Operand: 
              IThrowOperation (OperationKind.Throw, Type: null, IsInvalid) (Syntax: 'throw null')
                ILiteralOperation (OperationKind.Literal, Type: null, Constant: null, IsInvalid) (Syntax: 'null')
");
        }

        [Fact]
        public void DestructorBody_01()
        {
            string source = @"
abstract class C
{
    ~C();
}
";
            var compilation = CreateCompilation(source);

            compilation.VerifyDiagnostics(
                // (4,6): error CS0501: 'C.~C()' must declare a body because it is not marked abstract, extern, or partial
                //     ~C();
                Diagnostic(ErrorCode.ERR_ConcreteMissingBody, "C").WithArguments("C.~C()").WithLocation(4, 6)
                );

            var tree = compilation.SyntaxTrees.Single();
            var model = compilation.GetSemanticModel(tree);

            var node1 = tree.GetRoot().DescendantNodes().OfType<BaseMethodDeclarationSyntax>().Single();
            Assert.Null(model.GetOperation(node1));
        }

        [Fact]
        public void DestructorBody_02()
        {
            string source = @"
class C
{
    ~C()
    { throw null; }
}
";
            var compilation = CreateCompilation(source);

            compilation.VerifyDiagnostics();

            var tree = compilation.SyntaxTrees.Single();
            var node1 = tree.GetRoot().DescendantNodes().OfType<BaseMethodDeclarationSyntax>().Single();

            compilation.VerifyOperationTree(node1, expectedOperationTree:
@"
IMethodBodyOperation (OperationKind.MethodBodyOperation, Type: null) (Syntax: '~C() ... row null; }')
  BlockBody: 
    IBlockOperation (1 statements) (OperationKind.Block, Type: null) (Syntax: '{ throw null; }')
      IThrowOperation (OperationKind.Throw, Type: null) (Syntax: 'throw null;')
        ILiteralOperation (OperationKind.Literal, Type: null, Constant: null) (Syntax: 'null')
  ExpressionBody: 
    null
");
        }

        [Fact]
        public void DestructorBody_03()
        {
            string source = @"
class C
{
    ~C()
    => throw null;
}
";
            var compilation = CreateCompilation(source);

            compilation.VerifyDiagnostics();

            var tree = compilation.SyntaxTrees.Single();
            var node1 = tree.GetRoot().DescendantNodes().OfType<BaseMethodDeclarationSyntax>().Single();

            compilation.VerifyOperationTree(node1, expectedOperationTree:
@"
IMethodBodyOperation (OperationKind.MethodBodyOperation, Type: null) (Syntax: '~C() ... throw null;')
  BlockBody: 
    null
  ExpressionBody: 
    IBlockOperation (1 statements) (OperationKind.Block, Type: null) (Syntax: '=> throw null')
      IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null, IsImplicit) (Syntax: 'throw null')
        Expression: 
          IThrowOperation (OperationKind.Throw, Type: null) (Syntax: 'throw null')
            ILiteralOperation (OperationKind.Literal, Type: null, Constant: null) (Syntax: 'null')
");
        }

        [Fact]
        public void DestructorBody_04()
        {
            string source = @"
class C
{
    ~C()
    { throw null; }
    => throw null;
}
";
            var compilation = CreateCompilation(source);

            compilation.VerifyDiagnostics(
                // (4,5): error CS8057: Block bodies and expression bodies cannot both be provided.
                //     ~C()
                Diagnostic(ErrorCode.ERR_BlockBodyAndExpressionBody, @"~C()
    { throw null; }
    => throw null;").WithLocation(4, 5)
                );

            var tree = compilation.SyntaxTrees.Single();
            var node1 = tree.GetRoot().DescendantNodes().OfType<BaseMethodDeclarationSyntax>().Single();

            compilation.VerifyOperationTree(node1, expectedOperationTree:
@"
IMethodBodyOperation (OperationKind.MethodBodyOperation, Type: null, IsInvalid) (Syntax: '~C() ... throw null;')
  BlockBody: 
    IBlockOperation (1 statements) (OperationKind.Block, Type: null, IsInvalid) (Syntax: '{ throw null; }')
      IThrowOperation (OperationKind.Throw, Type: null, IsInvalid) (Syntax: 'throw null;')
        ILiteralOperation (OperationKind.Literal, Type: null, Constant: null, IsInvalid) (Syntax: 'null')
  ExpressionBody: 
    IBlockOperation (1 statements) (OperationKind.Block, Type: null, IsInvalid) (Syntax: '=> throw null')
      IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null, IsInvalid, IsImplicit) (Syntax: 'throw null')
        Expression: 
          IThrowOperation (OperationKind.Throw, Type: null, IsInvalid) (Syntax: 'throw null')
            ILiteralOperation (OperationKind.Literal, Type: null, Constant: null, IsInvalid) (Syntax: 'null')
");
        }

        [Fact]
        public void AccessorBody_01()
        {
            string source = @"
abstract class C
{
    abstract protected int P { get; }
}
";
            var compilation = CreateCompilation(source);

            compilation.VerifyDiagnostics();

            var tree = compilation.SyntaxTrees.Single();
            var model = compilation.GetSemanticModel(tree);

            var node1 = tree.GetRoot().DescendantNodes().OfType<AccessorDeclarationSyntax>().Single();
            Assert.Null(model.GetOperation(node1));
        }

        [Fact]
        public void AccessorBody_02()
        {
            string source = @"
class C
{
    int P 
    { 
        set
        { throw null; }
    }
}
";
            var compilation = CreateCompilation(source);

            compilation.VerifyDiagnostics();

            var tree = compilation.SyntaxTrees.Single();
            var node1 = tree.GetRoot().DescendantNodes().OfType<AccessorDeclarationSyntax>().Single();

            compilation.VerifyOperationTree(node1, expectedOperationTree:
@"
IMethodBodyOperation (OperationKind.MethodBodyOperation, Type: null) (Syntax: 'set ... row null; }')
  BlockBody: 
    IBlockOperation (1 statements) (OperationKind.Block, Type: null) (Syntax: '{ throw null; }')
      IThrowOperation (OperationKind.Throw, Type: null) (Syntax: 'throw null;')
        ILiteralOperation (OperationKind.Literal, Type: null, Constant: null) (Syntax: 'null')
  ExpressionBody: 
    null
");
        }

        [Fact]
        public void AccessorBody_03()
        {
            string source = @"
class C
{
    event System.Action E
    {
        add => throw null;
        remove {throw null;}
    }
}
";
            var compilation = CreateCompilation(source);

            compilation.VerifyDiagnostics();

            var tree = compilation.SyntaxTrees.Single();
            var node1 = tree.GetRoot().DescendantNodes().OfType<AccessorDeclarationSyntax>().First();

            compilation.VerifyOperationTree(node1, expectedOperationTree:
@"
IMethodBodyOperation (OperationKind.MethodBodyOperation, Type: null) (Syntax: 'add => throw null;')
  BlockBody: 
    null
  ExpressionBody: 
    IBlockOperation (1 statements) (OperationKind.Block, Type: null) (Syntax: '=> throw null')
      IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null, IsImplicit) (Syntax: 'throw null')
        Expression: 
          IThrowOperation (OperationKind.Throw, Type: null) (Syntax: 'throw null')
            ILiteralOperation (OperationKind.Literal, Type: null, Constant: null) (Syntax: 'null')
");
        }

        [Fact]
        public void AccessorBody_04()
        {
            string source = @"
class C
{
    event System.Action E
    {
        remove 
        { throw null; }
        => throw null;
        add { throw null;}
    }
}
";
            var compilation = CreateCompilation(source);

            compilation.VerifyDiagnostics(
                // (6,9): error CS8057: Block bodies and expression bodies cannot both be provided.
                //         remove 
                Diagnostic(ErrorCode.ERR_BlockBodyAndExpressionBody, @"remove 
        { throw null; }
        => throw null;").WithLocation(6, 9)
                );

            var tree = compilation.SyntaxTrees.Single();
            var node1 = tree.GetRoot().DescendantNodes().OfType<AccessorDeclarationSyntax>().First();

            compilation.VerifyOperationTree(node1, expectedOperationTree:
@"
IMethodBodyOperation (OperationKind.MethodBodyOperation, Type: null, IsInvalid) (Syntax: 'remove ... throw null;')
  BlockBody: 
    IBlockOperation (1 statements) (OperationKind.Block, Type: null, IsInvalid) (Syntax: '{ throw null; }')
      IThrowOperation (OperationKind.Throw, Type: null, IsInvalid) (Syntax: 'throw null;')
        ILiteralOperation (OperationKind.Literal, Type: null, Constant: null, IsInvalid) (Syntax: 'null')
  ExpressionBody: 
    IBlockOperation (1 statements) (OperationKind.Block, Type: null, IsInvalid) (Syntax: '=> throw null')
      IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null, IsInvalid, IsImplicit) (Syntax: 'throw null')
        Expression: 
          IThrowOperation (OperationKind.Throw, Type: null, IsInvalid) (Syntax: 'throw null')
            ILiteralOperation (OperationKind.Literal, Type: null, Constant: null, IsInvalid) (Syntax: 'null')
");
        }

        [Fact]
        public void AccessorBody_05()
        {
            string source = @"
abstract class C
{
    int P { get; } => throw null;
}
";
            var compilation = CreateCompilation(source);

            compilation.VerifyDiagnostics(
                // (4,5): error CS8057: Block bodies and expression bodies cannot both be provided.
                //     int P { get; } => throw null;
                Diagnostic(ErrorCode.ERR_BlockBodyAndExpressionBody, "int P { get; } => throw null;").WithLocation(4, 5)
                );

            var tree = compilation.SyntaxTrees.Single();
            var model = compilation.GetSemanticModel(tree);

            var node1 = tree.GetRoot().DescendantNodes().OfType<AccessorDeclarationSyntax>().Single();
            Assert.Null(model.GetOperation(node1));
        }
    }
}
