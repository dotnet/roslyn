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
        public void ConstructorBody_01()
        {
            string source = @"
class C
{
    public C()
}
";
            var compilation = CreateCompilation(source);

            compilation.VerifyDiagnostics(
                // file.cs(4,15): error CS1002: ; expected
                //     public C()
                Diagnostic(ErrorCode.ERR_SemicolonExpected, "").WithLocation(4, 15),
                // file.cs(4,12): error CS0501: 'C.C()' must declare a body because it is not marked abstract, extern, or partial
                //     public C()
                Diagnostic(ErrorCode.ERR_ConcreteMissingBody, "C").WithArguments("C.C()").WithLocation(4, 12)
                );

            var tree = compilation.SyntaxTrees.Single();
            var model = compilation.GetSemanticModel(tree);

            var node1 = tree.GetRoot().DescendantNodes().OfType<ConstructorDeclarationSyntax>().Single();
            Assert.Null(model.GetOperation(node1));
        }

        [CompilerTrait(CompilerFeature.Dataflow)]
        [Fact]
        public void ConstructorBody_02()
        {
            string source = @"
class C
{
    public C() : base()
}
";
            var compilation = CreateCompilation(source, parseOptions: TestOptions.RegularWithFlowAnalysisFeature);

            compilation.VerifyDiagnostics(
                // (4,24): error CS1002: ; expected
                //     public C() : base()
                Diagnostic(ErrorCode.ERR_SemicolonExpected, "").WithLocation(4, 24),
                // (4,12): error CS0501: 'C.C()' must declare a body because it is not marked abstract, extern, or partial
                //     public C() : base()
                Diagnostic(ErrorCode.ERR_ConcreteMissingBody, "C").WithArguments("C.C()").WithLocation(4, 12)
                );

            var tree = compilation.SyntaxTrees.Single();
            var node1 = tree.GetRoot().DescendantNodes().OfType<ConstructorDeclarationSyntax>().Single();

            compilation.VerifyOperationTree(node1, expectedOperationTree:
@"
IConstructorBodyOperation (OperationKind.ConstructorBodyOperation, Type: null, IsInvalid) (Syntax: 'public C() : base()
')
  Initializer: 
    IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null, IsInvalid, IsImplicit) (Syntax: ': base()')
      Expression: 
        IInvocationOperation ( System.Object..ctor()) (OperationKind.Invocation, Type: System.Void, IsInvalid) (Syntax: ': base()')
          Instance Receiver: 
            IInstanceReferenceOperation (ReferenceKind: ContainingTypeInstance) (OperationKind.InstanceReference, Type: System.Object, IsInvalid, IsImplicit) (Syntax: ': base()')
          Arguments(0)
  BlockBody: 
    null
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
        IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null, IsInvalid, IsImplicit) (Syntax: ': base()')
          Expression: 
            IInvocationOperation ( System.Object..ctor()) (OperationKind.Invocation, Type: System.Void, IsInvalid) (Syntax: ': base()')
              Instance Receiver: 
                IInstanceReferenceOperation (ReferenceKind: ContainingTypeInstance) (OperationKind.InstanceReference, Type: System.Object, IsInvalid, IsImplicit) (Syntax: ': base()')
              Arguments(0)

    Next (Regular) Block[B2]
Block[B2] - Exit
    Predecessors: [B1]
    Statements (0)
");
        }

        [CompilerTrait(CompilerFeature.Dataflow)]
        [Fact]
        public void ConstructorBody_03()
        {
            string source = @"
class C
{
    public C() : base()
    { throw null; }
}
";
            var compilation = CreateCompilation(source, parseOptions: TestOptions.RegularWithFlowAnalysisFeature);

            compilation.VerifyDiagnostics();

            var tree = compilation.SyntaxTrees.Single();
            var node1 = tree.GetRoot().DescendantNodes().OfType<ConstructorDeclarationSyntax>().Single();

            compilation.VerifyOperationTree(node1, expectedOperationTree:
@"
IConstructorBodyOperation (OperationKind.ConstructorBodyOperation, Type: null) (Syntax: 'public C()  ... row null; }')
  Initializer: 
    IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null, IsImplicit) (Syntax: ': base()')
      Expression: 
        IInvocationOperation ( System.Object..ctor()) (OperationKind.Invocation, Type: System.Void) (Syntax: ': base()')
          Instance Receiver: 
            IInstanceReferenceOperation (ReferenceKind: ContainingTypeInstance) (OperationKind.InstanceReference, Type: System.Object, IsImplicit) (Syntax: ': base()')
          Arguments(0)
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
    Statements (1)
        IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null, IsImplicit) (Syntax: ': base()')
          Expression: 
            IInvocationOperation ( System.Object..ctor()) (OperationKind.Invocation, Type: System.Void) (Syntax: ': base()')
              Instance Receiver: 
                IInstanceReferenceOperation (ReferenceKind: ContainingTypeInstance) (OperationKind.InstanceReference, Type: System.Object, IsImplicit) (Syntax: ': base()')
              Arguments(0)

    Next (Throw) Block[null]
        ILiteralOperation (OperationKind.Literal, Type: null, Constant: null) (Syntax: 'null')
Block[B2] - Exit [UnReachable]
    Predecessors (0)
    Statements (0)
");
        }

        [CompilerTrait(CompilerFeature.Dataflow)]
        [Fact]
        public void ConstructorBody_04()
        {
            string source = @"
class C
{
    public C() : base()
    => throw null;
}
";
            var compilation = CreateCompilation(source, parseOptions: TestOptions.RegularWithFlowAnalysisFeature);

            compilation.VerifyDiagnostics();

            var tree = compilation.SyntaxTrees.Single();
            var node1 = tree.GetRoot().DescendantNodes().OfType<ConstructorDeclarationSyntax>().Single();

            compilation.VerifyOperationTree(node1, expectedOperationTree:
@"
IConstructorBodyOperation (OperationKind.ConstructorBodyOperation, Type: null) (Syntax: 'public C()  ... throw null;')
  Initializer: 
    IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null, IsImplicit) (Syntax: ': base()')
      Expression: 
        IInvocationOperation ( System.Object..ctor()) (OperationKind.Invocation, Type: System.Void) (Syntax: ': base()')
          Instance Receiver: 
            IInstanceReferenceOperation (ReferenceKind: ContainingTypeInstance) (OperationKind.InstanceReference, Type: System.Object, IsImplicit) (Syntax: ': base()')
          Arguments(0)
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
    Statements (1)
        IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null, IsImplicit) (Syntax: ': base()')
          Expression: 
            IInvocationOperation ( System.Object..ctor()) (OperationKind.Invocation, Type: System.Void) (Syntax: ': base()')
              Instance Receiver: 
                IInstanceReferenceOperation (ReferenceKind: ContainingTypeInstance) (OperationKind.InstanceReference, Type: System.Object, IsImplicit) (Syntax: ': base()')
              Arguments(0)

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
        public void ConstructorBody_05()
        {
            string source = @"
class C
{
    public C()
    { throw null; }
}
";
            var compilation = CreateCompilation(source, parseOptions: TestOptions.RegularWithFlowAnalysisFeature);

            compilation.VerifyDiagnostics();

            var tree = compilation.SyntaxTrees.Single();
            var node1 = tree.GetRoot().DescendantNodes().OfType<ConstructorDeclarationSyntax>().Single();

            compilation.VerifyOperationTree(node1, expectedOperationTree:
@"
IConstructorBodyOperation (OperationKind.ConstructorBodyOperation, Type: null) (Syntax: 'public C() ... row null; }')
  Initializer: 
    null
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
        public void ConstructorBody_06()
        {
            string source = @"
class C
{
    public C()
    => throw null;
}
";
            var compilation = CreateCompilation(source, parseOptions: TestOptions.RegularWithFlowAnalysisFeature);

            compilation.VerifyDiagnostics();

            var tree = compilation.SyntaxTrees.Single();
            var node1 = tree.GetRoot().DescendantNodes().OfType<ConstructorDeclarationSyntax>().Single();

            compilation.VerifyOperationTree(node1, expectedOperationTree:
@"
IConstructorBodyOperation (OperationKind.ConstructorBodyOperation, Type: null) (Syntax: 'public C() ... throw null;')
  Initializer: 
    null
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
        public void ConstructorBody_07()
        {
            string source = @"
class C
{
    public C()
    { throw null; }
    => throw null;
}
";
            var compilation = CreateCompilation(source, parseOptions: TestOptions.RegularWithFlowAnalysisFeature);

            compilation.VerifyDiagnostics(
                // (4,5): error CS8057: Block bodies and expression bodies cannot both be provided.
                //     public C()
                Diagnostic(ErrorCode.ERR_BlockBodyAndExpressionBody, @"public C()
    { throw null; }
    => throw null;").WithLocation(4, 5)
                );

            var tree = compilation.SyntaxTrees.Single();
            var node1 = tree.GetRoot().DescendantNodes().OfType<ConstructorDeclarationSyntax>().Single();

            compilation.VerifyOperationTree(node1, expectedOperationTree:
@"
IConstructorBodyOperation (OperationKind.ConstructorBodyOperation, Type: null, IsInvalid) (Syntax: 'public C() ... throw null;')
  Initializer: 
    null
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

        [Fact]
        public void ConstructorBody_08()
        {
            string source = @"
class C
{
    public C();
}
";
            var compilation = CreateCompilation(source);

            compilation.VerifyDiagnostics(
                // file.cs(4,12): error CS0501: 'C.C()' must declare a body because it is not marked abstract, extern, or partial
                //     public C();
                Diagnostic(ErrorCode.ERR_ConcreteMissingBody, "C").WithArguments("C.C()").WithLocation(4, 12)
                );

            var tree = compilation.SyntaxTrees.Single();
            var model = compilation.GetSemanticModel(tree);

            var node1 = tree.GetRoot().DescendantNodes().OfType<ConstructorDeclarationSyntax>().Single();
            Assert.Null(model.GetOperation(node1));
        }

        [CompilerTrait(CompilerFeature.Dataflow)]
        [Fact]
        public void ConstructorBody_09()
        {
            string source = @"
class C
{
    public C() : base()
    { throw null; }
    => throw null;
}
";
            var compilation = CreateCompilation(source, parseOptions: TestOptions.RegularWithFlowAnalysisFeature);

            compilation.VerifyDiagnostics(
                // (4,5): error CS8057: Block bodies and expression bodies cannot both be provided.
                //     public C() : base()
                Diagnostic(ErrorCode.ERR_BlockBodyAndExpressionBody, @"public C() : base()
    { throw null; }
    => throw null;").WithLocation(4, 5)
                );

            var tree = compilation.SyntaxTrees.Single();
            var node1 = tree.GetRoot().DescendantNodes().OfType<ConstructorDeclarationSyntax>().Single();

            compilation.VerifyOperationTree(node1, expectedOperationTree:
@"
IConstructorBodyOperation (OperationKind.ConstructorBodyOperation, Type: null, IsInvalid) (Syntax: 'public C()  ... throw null;')
  Initializer: 
    IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null, IsInvalid, IsImplicit) (Syntax: ': base()')
      Expression: 
        IInvocationOperation ( System.Object..ctor()) (OperationKind.Invocation, Type: System.Void, IsInvalid) (Syntax: ': base()')
          Instance Receiver: 
            IInstanceReferenceOperation (ReferenceKind: ContainingTypeInstance) (OperationKind.InstanceReference, Type: System.Object, IsInvalid, IsImplicit) (Syntax: ': base()')
          Arguments(0)
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
    Statements (1)
        IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null, IsInvalid, IsImplicit) (Syntax: ': base()')
          Expression: 
            IInvocationOperation ( System.Object..ctor()) (OperationKind.Invocation, Type: System.Void, IsInvalid) (Syntax: ': base()')
              Instance Receiver: 
                IInstanceReferenceOperation (ReferenceKind: ContainingTypeInstance) (OperationKind.InstanceReference, Type: System.Object, IsInvalid, IsImplicit) (Syntax: ': base()')
              Arguments(0)

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
        public void ConstructorBody_10()
        {
            string source = @"
class C
{
    public C()
    { return; }
    => throw null;
}
";
            var compilation = CreateCompilation(source, parseOptions: TestOptions.RegularWithFlowAnalysisFeature);

            compilation.VerifyDiagnostics(
                // (4,5): error CS8057: Block bodies and expression bodies cannot both be provided.
                //     public C()
                Diagnostic(ErrorCode.ERR_BlockBodyAndExpressionBody, @"public C()
    { return; }
    => throw null;").WithLocation(4, 5)
            );

            var tree = compilation.SyntaxTrees.Single();
            var node1 = tree.GetRoot().DescendantNodes().OfType<ConstructorDeclarationSyntax>().Single();

            compilation.VerifyOperationTree(node1, expectedOperationTree:
@"
IConstructorBodyOperation (OperationKind.ConstructorBodyOperation, Type: null, IsInvalid) (Syntax: 'public C() ... throw null;')
  Initializer: 
    null
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
        public void ConstructorBody_11()
        {
            string source = @"
class B
{
    protected B(int i) { }
}

class C : B
{
    public C(int i, int j) : base(i)
    { j = i; }
}
";
            var compilation = CreateCompilation(source, parseOptions: TestOptions.RegularWithFlowAnalysisFeature);

            compilation.VerifyDiagnostics();

            var tree = compilation.SyntaxTrees.Single();
            var node1 = tree.GetRoot().DescendantNodes().OfType<ConstructorDeclarationSyntax>().Last();

            compilation.VerifyOperationTree(node1, expectedOperationTree:
@"
IConstructorBodyOperation (OperationKind.ConstructorBodyOperation, Type: null) (Syntax: 'public C(in ... { j = i; }')
  Initializer: 
    IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null, IsImplicit) (Syntax: ': base(i)')
      Expression: 
        IInvocationOperation ( B..ctor(System.Int32 i)) (OperationKind.Invocation, Type: System.Void) (Syntax: ': base(i)')
          Instance Receiver: 
            IInstanceReferenceOperation (ReferenceKind: ContainingTypeInstance) (OperationKind.InstanceReference, Type: B, IsImplicit) (Syntax: ': base(i)')
          Arguments(1):
              IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: i) (OperationKind.Argument, Type: null) (Syntax: 'i')
                IParameterReferenceOperation: i (OperationKind.ParameterReference, Type: System.Int32) (Syntax: 'i')
                InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
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
    Statements (2)
        IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null, IsImplicit) (Syntax: ': base(i)')
          Expression: 
            IInvocationOperation ( B..ctor(System.Int32 i)) (OperationKind.Invocation, Type: System.Void) (Syntax: ': base(i)')
              Instance Receiver: 
                IInstanceReferenceOperation (ReferenceKind: ContainingTypeInstance) (OperationKind.InstanceReference, Type: B, IsImplicit) (Syntax: ': base(i)')
              Arguments(1):
                  IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: i) (OperationKind.Argument, Type: null) (Syntax: 'i')
                    IParameterReferenceOperation: i (OperationKind.ParameterReference, Type: System.Int32) (Syntax: 'i')
                    InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                    OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)

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
        public void ConstructorBody_12()
        {
            string source = @"
class B
{
    protected B(int i) { }
}

class C : B
{
    public C(int i, int j) : base(i)
    => j = i;
}
";
            var compilation = CreateCompilation(source, parseOptions: TestOptions.RegularWithFlowAnalysisFeature);

            compilation.VerifyDiagnostics();

            var tree = compilation.SyntaxTrees.Single();
            var node1 = tree.GetRoot().DescendantNodes().OfType<ConstructorDeclarationSyntax>().Last();

            compilation.VerifyOperationTree(node1, expectedOperationTree:
@"
IConstructorBodyOperation (OperationKind.ConstructorBodyOperation, Type: null) (Syntax: 'public C(in ... => j = i;')
  Initializer: 
    IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null, IsImplicit) (Syntax: ': base(i)')
      Expression: 
        IInvocationOperation ( B..ctor(System.Int32 i)) (OperationKind.Invocation, Type: System.Void) (Syntax: ': base(i)')
          Instance Receiver: 
            IInstanceReferenceOperation (ReferenceKind: ContainingTypeInstance) (OperationKind.InstanceReference, Type: B, IsImplicit) (Syntax: ': base(i)')
          Arguments(1):
              IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: i) (OperationKind.Argument, Type: null) (Syntax: 'i')
                IParameterReferenceOperation: i (OperationKind.ParameterReference, Type: System.Int32) (Syntax: 'i')
                InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
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
    Statements (2)
        IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null, IsImplicit) (Syntax: ': base(i)')
          Expression: 
            IInvocationOperation ( B..ctor(System.Int32 i)) (OperationKind.Invocation, Type: System.Void) (Syntax: ': base(i)')
              Instance Receiver: 
                IInstanceReferenceOperation (ReferenceKind: ContainingTypeInstance) (OperationKind.InstanceReference, Type: B, IsImplicit) (Syntax: ': base(i)')
              Arguments(1):
                  IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: i) (OperationKind.Argument, Type: null) (Syntax: 'i')
                    IParameterReferenceOperation: i (OperationKind.ParameterReference, Type: System.Int32) (Syntax: 'i')
                    InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                    OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)

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
        public void ConstructorBody_13()
        {
            string source = @"
class B
{
    protected B(int i) { }
}

class C : B
{
    public C(int i, int j) : base(i)
    { j = i; }
    => j = i;
}
";
            var compilation = CreateCompilation(source, parseOptions: TestOptions.RegularWithFlowAnalysisFeature);

            compilation.VerifyDiagnostics(
                // (9,5): error CS8057: Block bodies and expression bodies cannot both be provided.
                //     public C(int i, int j) : base(i)
                Diagnostic(ErrorCode.ERR_BlockBodyAndExpressionBody, @"public C(int i, int j) : base(i)
    { j = i; }
    => j = i;").WithLocation(9, 5)
            );

            var tree = compilation.SyntaxTrees.Single();
            var node1 = tree.GetRoot().DescendantNodes().OfType<ConstructorDeclarationSyntax>().Last();

            compilation.VerifyOperationTree(node1, expectedOperationTree:
@"
IConstructorBodyOperation (OperationKind.ConstructorBodyOperation, Type: null, IsInvalid) (Syntax: 'public C(in ... => j = i;')
  Initializer: 
    IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null, IsInvalid, IsImplicit) (Syntax: ': base(i)')
      Expression: 
        IInvocationOperation ( B..ctor(System.Int32 i)) (OperationKind.Invocation, Type: System.Void, IsInvalid) (Syntax: ': base(i)')
          Instance Receiver: 
            IInstanceReferenceOperation (ReferenceKind: ContainingTypeInstance) (OperationKind.InstanceReference, Type: B, IsInvalid, IsImplicit) (Syntax: ': base(i)')
          Arguments(1):
              IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: i) (OperationKind.Argument, Type: null, IsInvalid) (Syntax: 'i')
                IParameterReferenceOperation: i (OperationKind.ParameterReference, Type: System.Int32, IsInvalid) (Syntax: 'i')
                InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
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
    Statements (2)
        IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null, IsInvalid, IsImplicit) (Syntax: ': base(i)')
          Expression: 
            IInvocationOperation ( B..ctor(System.Int32 i)) (OperationKind.Invocation, Type: System.Void, IsInvalid) (Syntax: ': base(i)')
              Instance Receiver: 
                IInstanceReferenceOperation (ReferenceKind: ContainingTypeInstance) (OperationKind.InstanceReference, Type: B, IsInvalid, IsImplicit) (Syntax: ': base(i)')
              Arguments(1):
                  IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: i) (OperationKind.Argument, Type: null, IsInvalid) (Syntax: 'i')
                    IParameterReferenceOperation: i (OperationKind.ParameterReference, Type: System.Int32, IsInvalid) (Syntax: 'i')
                    InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                    OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)

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
        public void ConstructorBody_14()
        {
            string source = @"
class B
{
    protected B(int i) { }
}

class C : B
{
    public C(int i, int j) : base(M(out int x))
    { j = i; }

    private static int M(out int x)
    {
        x = 0;
        return x;
    }
}
";
            var compilation = CreateCompilation(source, parseOptions: TestOptions.RegularWithFlowAnalysisFeature);

            compilation.VerifyDiagnostics();

            var tree = compilation.SyntaxTrees.Single();
            var node1 = tree.GetRoot().DescendantNodes().OfType<ConstructorDeclarationSyntax>().Last();

            compilation.VerifyOperationTree(node1, expectedOperationTree:
@"
IConstructorBodyOperation (OperationKind.ConstructorBodyOperation, Type: null) (Syntax: 'public C(in ... { j = i; }')
  Locals: Local_1: System.Int32 x
  Initializer: 
    IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null, IsImplicit) (Syntax: ': base(M(out int x))')
      Expression: 
        IInvocationOperation ( B..ctor(System.Int32 i)) (OperationKind.Invocation, Type: System.Void) (Syntax: ': base(M(out int x))')
          Instance Receiver: 
            IInstanceReferenceOperation (ReferenceKind: ContainingTypeInstance) (OperationKind.InstanceReference, Type: B, IsImplicit) (Syntax: ': base(M(out int x))')
          Arguments(1):
              IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: i) (OperationKind.Argument, Type: null) (Syntax: 'M(out int x)')
                IInvocationOperation (System.Int32 C.M(out System.Int32 x)) (OperationKind.Invocation, Type: System.Int32) (Syntax: 'M(out int x)')
                  Instance Receiver: 
                    null
                  Arguments(1):
                      IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: x) (OperationKind.Argument, Type: null) (Syntax: 'out int x')
                        IDeclarationExpressionOperation (OperationKind.DeclarationExpression, Type: System.Int32) (Syntax: 'int x')
                          ILocalReferenceOperation: x (IsDeclaration: True) (OperationKind.LocalReference, Type: System.Int32) (Syntax: 'x')
                        InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                        OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
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
        Entering: {R1}

.locals {R1}
{
    Locals: [System.Int32 x]
    Block[B1] - Block
        Predecessors: [B0]
        Statements (2)
            IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null, IsImplicit) (Syntax: ': base(M(out int x))')
              Expression: 
                IInvocationOperation ( B..ctor(System.Int32 i)) (OperationKind.Invocation, Type: System.Void) (Syntax: ': base(M(out int x))')
                  Instance Receiver: 
                    IInstanceReferenceOperation (ReferenceKind: ContainingTypeInstance) (OperationKind.InstanceReference, Type: B, IsImplicit) (Syntax: ': base(M(out int x))')
                  Arguments(1):
                      IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: i) (OperationKind.Argument, Type: null) (Syntax: 'M(out int x)')
                        IInvocationOperation (System.Int32 C.M(out System.Int32 x)) (OperationKind.Invocation, Type: System.Int32) (Syntax: 'M(out int x)')
                          Instance Receiver: 
                            null
                          Arguments(1):
                              IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: x) (OperationKind.Argument, Type: null) (Syntax: 'out int x')
                                IDeclarationExpressionOperation (OperationKind.DeclarationExpression, Type: System.Int32) (Syntax: 'int x')
                                  ILocalReferenceOperation: x (IsDeclaration: True) (OperationKind.LocalReference, Type: System.Int32) (Syntax: 'x')
                                InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                                OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                        InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                        OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)

            IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'j = i;')
              Expression: 
                ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Int32) (Syntax: 'j = i')
                  Left: 
                    IParameterReferenceOperation: j (OperationKind.ParameterReference, Type: System.Int32) (Syntax: 'j')
                  Right: 
                    IParameterReferenceOperation: i (OperationKind.ParameterReference, Type: System.Int32) (Syntax: 'i')

        Next (Regular) Block[B2]
            Leaving: {R1}
}

Block[B2] - Exit
    Predecessors: [B1]
    Statements (0)
");
        }

        [CompilerTrait(CompilerFeature.Dataflow)]
        [Fact]
        public void ConstructorBody_15()
        {
            string source = @"
class C
{
    public C()
    { }
}
";
            var compilation = CreateCompilation(source, parseOptions: TestOptions.RegularWithFlowAnalysisFeature);

            compilation.VerifyDiagnostics();

            var tree = compilation.SyntaxTrees.Single();
            var node1 = tree.GetRoot().DescendantNodes().OfType<ConstructorDeclarationSyntax>().Single();

            compilation.VerifyOperationTree(node1, expectedOperationTree:
@"
IConstructorBodyOperation (OperationKind.ConstructorBodyOperation, Type: null) (Syntax: 'public C() ... { }')
  Initializer: 
    null
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
        public void ConstructorBody_16()
        {
            string source = @"
class C : Base
{
    C(int p) : base(out var i)
    {
        p = i;
    }
}

class Base
{
    protected Base(out int i)
    {
        i = 1;
    }
}
";
            var compilation = CreateCompilation(source, parseOptions: TestOptions.RegularWithFlowAnalysisFeature);

            compilation.VerifyDiagnostics();

            var tree = compilation.SyntaxTrees.Single();
            var node1 = tree.GetRoot().DescendantNodes().OfType<ConstructorDeclarationSyntax>().First();

            compilation.VerifyOperationTree(node1, expectedOperationTree:
@"
IConstructorBodyOperation (OperationKind.ConstructorBodyOperation, Type: null) (Syntax: 'C(int p) :  ... }')
  Locals: Local_1: System.Int32 i
  Initializer: 
    IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null, IsImplicit) (Syntax: ': base(out var i)')
      Expression: 
        IInvocationOperation ( Base..ctor(out System.Int32 i)) (OperationKind.Invocation, Type: System.Void) (Syntax: ': base(out var i)')
          Instance Receiver: 
            IInstanceReferenceOperation (ReferenceKind: ContainingTypeInstance) (OperationKind.InstanceReference, Type: Base, IsImplicit) (Syntax: ': base(out var i)')
          Arguments(1):
              IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: i) (OperationKind.Argument, Type: null) (Syntax: 'out var i')
                IDeclarationExpressionOperation (OperationKind.DeclarationExpression, Type: System.Int32) (Syntax: 'var i')
                  ILocalReferenceOperation: i (IsDeclaration: True) (OperationKind.LocalReference, Type: System.Int32) (Syntax: 'i')
                InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
  BlockBody: 
    IBlockOperation (1 statements) (OperationKind.Block, Type: null) (Syntax: '{ ... }')
      IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'p = i;')
        Expression: 
          ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Int32) (Syntax: 'p = i')
            Left: 
              IParameterReferenceOperation: p (OperationKind.ParameterReference, Type: System.Int32) (Syntax: 'p')
            Right: 
              ILocalReferenceOperation: i (OperationKind.LocalReference, Type: System.Int32) (Syntax: 'i')
  ExpressionBody: 
    null
");

            VerifyFlowGraph(compilation, node1, expectedFlowGraph:
@"
Block[B0] - Entry
    Statements (0)
    Next (Regular) Block[B1]
        Entering: {R1}

.locals {R1}
{
    Locals: [System.Int32 i]
    Block[B1] - Block
        Predecessors: [B0]
        Statements (2)
            IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null, IsImplicit) (Syntax: ': base(out var i)')
              Expression: 
                IInvocationOperation ( Base..ctor(out System.Int32 i)) (OperationKind.Invocation, Type: System.Void) (Syntax: ': base(out var i)')
                  Instance Receiver: 
                    IInstanceReferenceOperation (ReferenceKind: ContainingTypeInstance) (OperationKind.InstanceReference, Type: Base, IsImplicit) (Syntax: ': base(out var i)')
                  Arguments(1):
                      IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: i) (OperationKind.Argument, Type: null) (Syntax: 'out var i')
                        IDeclarationExpressionOperation (OperationKind.DeclarationExpression, Type: System.Int32) (Syntax: 'var i')
                          ILocalReferenceOperation: i (IsDeclaration: True) (OperationKind.LocalReference, Type: System.Int32) (Syntax: 'i')
                        InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                        OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)

            IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'p = i;')
              Expression: 
                ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Int32) (Syntax: 'p = i')
                  Left: 
                    IParameterReferenceOperation: p (OperationKind.ParameterReference, Type: System.Int32) (Syntax: 'p')
                  Right: 
                    ILocalReferenceOperation: i (OperationKind.LocalReference, Type: System.Int32) (Syntax: 'i')

        Next (Regular) Block[B2]
            Leaving: {R1}
}

Block[B2] - Exit
    Predecessors: [B1]
    Statements (0)
");
        }

        [CompilerTrait(CompilerFeature.Dataflow)]
        [Fact]
        public void ConstructorBody_17()
        {
            string source = @"
class C : Base
{
    C(int? i, int j, int p) : base(i ?? j) 
    {
        p = j;
    }
}

class Base
{
    protected Base(int i)
    {
    }
}
";
            var compilation = CreateCompilation(source, parseOptions: TestOptions.RegularWithFlowAnalysisFeature);

            compilation.VerifyDiagnostics();

            var tree = compilation.SyntaxTrees.Single();
            var node1 = tree.GetRoot().DescendantNodes().OfType<ConstructorDeclarationSyntax>().First();

            compilation.VerifyOperationTree(node1, expectedOperationTree:
@"
IConstructorBodyOperation (OperationKind.ConstructorBodyOperation, Type: null) (Syntax: 'C(int? i, i ... }')
  Initializer: 
    IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null, IsImplicit) (Syntax: ': base(i ?? j)')
      Expression: 
        IInvocationOperation ( Base..ctor(System.Int32 i)) (OperationKind.Invocation, Type: System.Void) (Syntax: ': base(i ?? j)')
          Instance Receiver: 
            IInstanceReferenceOperation (ReferenceKind: ContainingTypeInstance) (OperationKind.InstanceReference, Type: Base, IsImplicit) (Syntax: ': base(i ?? j)')
          Arguments(1):
              IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: i) (OperationKind.Argument, Type: null) (Syntax: 'i ?? j')
                ICoalesceOperation (OperationKind.Coalesce, Type: System.Int32) (Syntax: 'i ?? j')
                  Expression: 
                    IParameterReferenceOperation: i (OperationKind.ParameterReference, Type: System.Int32?) (Syntax: 'i')
                  ValueConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                    (Identity)
                  WhenNull: 
                    IParameterReferenceOperation: j (OperationKind.ParameterReference, Type: System.Int32) (Syntax: 'j')
                InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
  BlockBody: 
    IBlockOperation (1 statements) (OperationKind.Block, Type: null) (Syntax: '{ ... }')
      IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'p = j;')
        Expression: 
          ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Int32) (Syntax: 'p = j')
            Left: 
              IParameterReferenceOperation: p (OperationKind.ParameterReference, Type: System.Int32) (Syntax: 'p')
            Right: 
              IParameterReferenceOperation: j (OperationKind.ParameterReference, Type: System.Int32) (Syntax: 'j')
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
    Statements (2)
        IFlowCaptureOperation: 0 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: ': base(i ?? j)')
          Value: 
            IInstanceReferenceOperation (ReferenceKind: ContainingTypeInstance) (OperationKind.InstanceReference, Type: Base, IsImplicit) (Syntax: ': base(i ?? j)')

        IFlowCaptureOperation: 1 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'i')
          Value: 
            IParameterReferenceOperation: i (OperationKind.ParameterReference, Type: System.Int32?) (Syntax: 'i')

    Jump if True (Regular) to Block[B3]
        IIsNullOperation (OperationKind.IsNull, Type: System.Boolean, IsImplicit) (Syntax: 'i')
          Operand: 
            IFlowCaptureReferenceOperation: 1 (OperationKind.FlowCaptureReference, Type: System.Int32?, IsImplicit) (Syntax: 'i')

    Next (Regular) Block[B2]
Block[B2] - Block
    Predecessors: [B1]
    Statements (1)
        IFlowCaptureOperation: 2 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'i')
          Value: 
            IInvocationOperation ( System.Int32 System.Int32?.GetValueOrDefault()) (OperationKind.Invocation, Type: System.Int32, IsImplicit) (Syntax: 'i')
              Instance Receiver: 
                IFlowCaptureReferenceOperation: 1 (OperationKind.FlowCaptureReference, Type: System.Int32?, IsImplicit) (Syntax: 'i')
              Arguments(0)

    Next (Regular) Block[B4]
Block[B3] - Block
    Predecessors: [B1]
    Statements (1)
        IFlowCaptureOperation: 2 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'j')
          Value: 
            IParameterReferenceOperation: j (OperationKind.ParameterReference, Type: System.Int32) (Syntax: 'j')

    Next (Regular) Block[B4]
Block[B4] - Block
    Predecessors: [B2] [B3]
    Statements (2)
        IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null, IsImplicit) (Syntax: ': base(i ?? j)')
          Expression: 
            IInvocationOperation ( Base..ctor(System.Int32 i)) (OperationKind.Invocation, Type: System.Void) (Syntax: ': base(i ?? j)')
              Instance Receiver: 
                IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: Base, IsImplicit) (Syntax: ': base(i ?? j)')
              Arguments(1):
                  IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: i) (OperationKind.Argument, Type: null) (Syntax: 'i ?? j')
                    IFlowCaptureReferenceOperation: 2 (OperationKind.FlowCaptureReference, Type: System.Int32, IsImplicit) (Syntax: 'i ?? j')
                    InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                    OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)

        IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'p = j;')
          Expression: 
            ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Int32) (Syntax: 'p = j')
              Left: 
                IParameterReferenceOperation: p (OperationKind.ParameterReference, Type: System.Int32) (Syntax: 'p')
              Right: 
                IParameterReferenceOperation: j (OperationKind.ParameterReference, Type: System.Int32) (Syntax: 'j')

    Next (Regular) Block[B5]
Block[B5] - Exit
    Predecessors: [B4]
    Statements (0)
");
        }

        [CompilerTrait(CompilerFeature.Dataflow)]
        [Fact]
        public void ConstructorBody_18()
        {
            string source = @"
class C
{
    C(int j, int p) : this() 
    {
        p = j;
    }

    C()
    {
    }
}
";
            var compilation = CreateCompilation(source, parseOptions: TestOptions.RegularWithFlowAnalysisFeature);

            compilation.VerifyDiagnostics();

            var tree = compilation.SyntaxTrees.Single();
            var node1 = tree.GetRoot().DescendantNodes().OfType<ConstructorDeclarationSyntax>().First();

            compilation.VerifyOperationTree(node1, expectedOperationTree:
@"
IConstructorBodyOperation (OperationKind.ConstructorBodyOperation, Type: null) (Syntax: 'C(int j, in ... }')
  Initializer: 
    IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null, IsImplicit) (Syntax: ': this()')
      Expression: 
        IInvocationOperation ( C..ctor()) (OperationKind.Invocation, Type: System.Void) (Syntax: ': this()')
          Instance Receiver: 
            IInstanceReferenceOperation (ReferenceKind: ContainingTypeInstance) (OperationKind.InstanceReference, Type: C, IsImplicit) (Syntax: ': this()')
          Arguments(0)
  BlockBody: 
    IBlockOperation (1 statements) (OperationKind.Block, Type: null) (Syntax: '{ ... }')
      IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'p = j;')
        Expression: 
          ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Int32) (Syntax: 'p = j')
            Left: 
              IParameterReferenceOperation: p (OperationKind.ParameterReference, Type: System.Int32) (Syntax: 'p')
            Right: 
              IParameterReferenceOperation: j (OperationKind.ParameterReference, Type: System.Int32) (Syntax: 'j')
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
    Statements (2)
        IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null, IsImplicit) (Syntax: ': this()')
          Expression: 
            IInvocationOperation ( C..ctor()) (OperationKind.Invocation, Type: System.Void) (Syntax: ': this()')
              Instance Receiver: 
                IInstanceReferenceOperation (ReferenceKind: ContainingTypeInstance) (OperationKind.InstanceReference, Type: C, IsImplicit) (Syntax: ': this()')
              Arguments(0)

        IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'p = j;')
          Expression: 
            ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Int32) (Syntax: 'p = j')
              Left: 
                IParameterReferenceOperation: p (OperationKind.ParameterReference, Type: System.Int32) (Syntax: 'p')
              Right: 
                IParameterReferenceOperation: j (OperationKind.ParameterReference, Type: System.Int32) (Syntax: 'j')

    Next (Regular) Block[B2]
Block[B2] - Exit
    Predecessors: [B1]
    Statements (0)
");
        }
    }
}
