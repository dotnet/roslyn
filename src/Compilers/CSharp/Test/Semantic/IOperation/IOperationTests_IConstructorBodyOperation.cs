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
            // No body, initializer without declarations
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
            // Block body, initializer without declarations
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
            // Expression body, initializer without declarations
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
Block[B2] - Exit [UnReachable]
    Predecessors (0)
    Statements (0)
");
        }

        [CompilerTrait(CompilerFeature.Dataflow)]
        [Fact]
        public void ConstructorBody_05()
        {
            // Block body, no initializer
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
            // Expression body, no initializer
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
Block[B2] - Exit [UnReachable]
    Predecessors (0)
    Statements (0)
");
        }

        [CompilerTrait(CompilerFeature.Dataflow)]
        [Fact]
        public void ConstructorBody_07()
        {
            // Block and expression body, no initializer
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
}

Block[B3] - Exit [UnReachable]
    Predecessors (0)
    Statements (0)
");
        }

        [Fact]
        public void ConstructorBody_08()
        {
            // No body, no initializer
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
            // Block and expression body, initializer without declarations
            string source = @"
class C
{
    public C(int i1, int i2, int j1, int j2) : base()
    { i1 = i2; }
    => j1 = j2;
}
";
            var compilation = CreateCompilation(source, parseOptions: TestOptions.RegularWithFlowAnalysisFeature);

            compilation.VerifyDiagnostics(
                // (4,5): error CS8057: Block bodies and expression bodies cannot both be provided.
                //     public C(int i1, int i2, int j1, int j2) : base()
                Diagnostic(ErrorCode.ERR_BlockBodyAndExpressionBody, @"public C(int i1, int i2, int j1, int j2) : base()
    { i1 = i2; }
    => j1 = j2;").WithLocation(4, 5));

            var tree = compilation.SyntaxTrees.Single();
            var node1 = tree.GetRoot().DescendantNodes().OfType<ConstructorDeclarationSyntax>().Single();

            VerifyFlowGraph(compilation, node1, expectedFlowGraph:
@"
Block[B0] - Entry
    Statements (0)
    Next (Regular) Block[B1]
Block[B1] - Block
    Predecessors: [B0]
    Statements (2)
        IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null, IsInvalid, IsImplicit) (Syntax: ': base()')
          Expression: 
            IInvocationOperation ( System.Object..ctor()) (OperationKind.Invocation, Type: System.Void, IsInvalid) (Syntax: ': base()')
              Instance Receiver: 
                IInstanceReferenceOperation (ReferenceKind: ContainingTypeInstance) (OperationKind.InstanceReference, Type: System.Object, IsInvalid, IsImplicit) (Syntax: ': base()')
              Arguments(0)

        IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null, IsInvalid) (Syntax: 'i1 = i2;')
          Expression: 
            ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Int32, IsInvalid) (Syntax: 'i1 = i2')
              Left: 
                IParameterReferenceOperation: i1 (OperationKind.ParameterReference, Type: System.Int32, IsInvalid) (Syntax: 'i1')
              Right: 
                IParameterReferenceOperation: i2 (OperationKind.ParameterReference, Type: System.Int32, IsInvalid) (Syntax: 'i2')

    Next (Regular) Block[B3]

.erroneous body {R1}
{
    Block[B2] - Block [UnReachable]
        Predecessors (0)
        Statements (1)
            IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null, IsInvalid, IsImplicit) (Syntax: 'j1 = j2')
              Expression: 
                ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Int32, IsInvalid) (Syntax: 'j1 = j2')
                  Left: 
                    IParameterReferenceOperation: j1 (OperationKind.ParameterReference, Type: System.Int32, IsInvalid) (Syntax: 'j1')
                  Right: 
                    IParameterReferenceOperation: j2 (OperationKind.ParameterReference, Type: System.Int32, IsInvalid) (Syntax: 'j2')

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
        public void ConstructorBody_10()
        {
            // Verify block body with a return statement, followed by throw in expression body.
            // This caught an assert when attempting to link current basic block which was already linked to exit.
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

            VerifyFlowGraph(compilation, node1, expectedFlowGraph:
@"
Block[B0] - Entry
    Statements (0)
    Next (Regular) Block[B2]

.erroneous body {R1}
{
    Block[B1] - Block [UnReachable]
        Predecessors (0)
        Statements (0)
        Next (Throw) Block[null]
            ILiteralOperation (OperationKind.Literal, Type: null, Constant: null, IsInvalid) (Syntax: 'null')
}

Block[B2] - Exit
    Predecessors: [B0]
    Statements (0)
");
        }

        [CompilerTrait(CompilerFeature.Dataflow)]
        [Fact]
        public void ConstructorBody_11()
        {
            // Block body, initializer with declarations
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
        public void ConstructorBody_12()
        {
            // Expression body, initializer with declarations
            string source = @"
class C : Base
{
    C(int p) : base(out var i)
    => p = i;
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

            IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null, IsImplicit) (Syntax: 'p = i')
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
        public void ConstructorBody_13()
        {
            // No body, initializer with declarations
            string source = @"
class C : Base
{
    C() : base(out var i)
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

            compilation.VerifyDiagnostics(
                // (4,26): error CS1002: ; expected
                //     C() : base(out var i)
                Diagnostic(ErrorCode.ERR_SemicolonExpected, "").WithLocation(4, 26),
                // (4,5): error CS0501: 'C.C()' must declare a body because it is not marked abstract, extern, or partial
                //     C() : base(out var i)
                Diagnostic(ErrorCode.ERR_ConcreteMissingBody, "C").WithArguments("C.C()").WithLocation(4, 5));

            var tree = compilation.SyntaxTrees.Single();
            var node1 = tree.GetRoot().DescendantNodes().OfType<ConstructorDeclarationSyntax>().First();

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
        Statements (1)
            IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null, IsInvalid, IsImplicit) (Syntax: ': base(out var i)')
              Expression: 
                IInvocationOperation ( Base..ctor(out System.Int32 i)) (OperationKind.Invocation, Type: System.Void, IsInvalid) (Syntax: ': base(out var i)')
                  Instance Receiver: 
                    IInstanceReferenceOperation (ReferenceKind: ContainingTypeInstance) (OperationKind.InstanceReference, Type: Base, IsInvalid, IsImplicit) (Syntax: ': base(out var i)')
                  Arguments(1):
                      IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: i) (OperationKind.Argument, Type: null) (Syntax: 'out var i')
                        IDeclarationExpressionOperation (OperationKind.DeclarationExpression, Type: System.Int32) (Syntax: 'var i')
                          ILocalReferenceOperation: i (IsDeclaration: True) (OperationKind.LocalReference, Type: System.Int32) (Syntax: 'i')
                        InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                        OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)

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
        public void ConstructorBody_14()
        {
            // Block and expression body, initializer with declarations
            string source = @"
class C : Base
{
    C(int j1, int j2) : base(out var i1, out var i2)
    { i1 = j1; }
    => j2 = i2;
}

class Base
{
    protected Base(out int i1, out int i2)
    {
        i1 = 1;
        i2 = 1;
    }
}
";
            var compilation = CreateCompilation(source, parseOptions: TestOptions.RegularWithFlowAnalysisFeature);

            compilation.VerifyDiagnostics(
                // (4,5): error CS8057: Block bodies and expression bodies cannot both be provided.
                //     C(int j1, int j2) : base(out var i1, out var i2)
                Diagnostic(ErrorCode.ERR_BlockBodyAndExpressionBody, @"C(int j1, int j2) : base(out var i1, out var i2)
    { i1 = j1; }
    => j2 = i2;").WithLocation(4, 5));

            var tree = compilation.SyntaxTrees.Single();
            var node1 = tree.GetRoot().DescendantNodes().OfType<ConstructorDeclarationSyntax>().First();

            VerifyFlowGraph(compilation, node1, expectedFlowGraph:
@"
Block[B0] - Entry
    Statements (0)
    Next (Regular) Block[B1]
        Entering: {R1}

.locals {R1}
{
    Locals: [System.Int32 i1] [System.Int32 i2]
    Block[B1] - Block
        Predecessors: [B0]
        Statements (2)
            IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null, IsInvalid, IsImplicit) (Syntax: ': base(out  ... out var i2)')
              Expression: 
                IInvocationOperation ( Base..ctor(out System.Int32 i1, out System.Int32 i2)) (OperationKind.Invocation, Type: System.Void, IsInvalid) (Syntax: ': base(out  ... out var i2)')
                  Instance Receiver: 
                    IInstanceReferenceOperation (ReferenceKind: ContainingTypeInstance) (OperationKind.InstanceReference, Type: Base, IsInvalid, IsImplicit) (Syntax: ': base(out  ... out var i2)')
                  Arguments(2):
                      IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: i1) (OperationKind.Argument, Type: null, IsInvalid) (Syntax: 'out var i1')
                        IDeclarationExpressionOperation (OperationKind.DeclarationExpression, Type: System.Int32, IsInvalid) (Syntax: 'var i1')
                          ILocalReferenceOperation: i1 (IsDeclaration: True) (OperationKind.LocalReference, Type: System.Int32, IsInvalid) (Syntax: 'i1')
                        InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                        OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                      IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: i2) (OperationKind.Argument, Type: null, IsInvalid) (Syntax: 'out var i2')
                        IDeclarationExpressionOperation (OperationKind.DeclarationExpression, Type: System.Int32, IsInvalid) (Syntax: 'var i2')
                          ILocalReferenceOperation: i2 (IsDeclaration: True) (OperationKind.LocalReference, Type: System.Int32, IsInvalid) (Syntax: 'i2')
                        InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                        OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)

            IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null, IsInvalid) (Syntax: 'i1 = j1;')
              Expression: 
                ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Int32, IsInvalid) (Syntax: 'i1 = j1')
                  Left: 
                    ILocalReferenceOperation: i1 (OperationKind.LocalReference, Type: System.Int32, IsInvalid) (Syntax: 'i1')
                  Right: 
                    IParameterReferenceOperation: j1 (OperationKind.ParameterReference, Type: System.Int32, IsInvalid) (Syntax: 'j1')

        Next (Regular) Block[B3]
            Leaving: {R1}

    .erroneous body {R2}
    {
        Block[B2] - Block [UnReachable]
            Predecessors (0)
            Statements (1)
                IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null, IsInvalid, IsImplicit) (Syntax: 'j2 = i2')
                  Expression: 
                    ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Int32, IsInvalid) (Syntax: 'j2 = i2')
                      Left: 
                        IParameterReferenceOperation: j2 (OperationKind.ParameterReference, Type: System.Int32, IsInvalid) (Syntax: 'j2')
                      Right: 
                        ILocalReferenceOperation: i2 (OperationKind.LocalReference, Type: System.Int32, IsInvalid) (Syntax: 'i2')

            Next (Regular) Block[B3]
                Leaving: {R2} {R1}
    }
}

Block[B3] - Exit
    Predecessors: [B1] [B2]
    Statements (0)
");
        }

        [CompilerTrait(CompilerFeature.Dataflow)]
        [Fact]
        public void ConstructorBody_15()
        {
            // Verify "this" initializer with control flow in initializer.
            string source = @"
class C
{
    C(int? i, int j, int k, int p) : this(i ?? j) 
    {
        p = k;
    }

    C(int i)
    {
    }
}
";
            var compilation = CreateCompilation(source, parseOptions: TestOptions.RegularWithFlowAnalysisFeature);

            compilation.VerifyDiagnostics();

            var tree = compilation.SyntaxTrees.Single();
            var node1 = tree.GetRoot().DescendantNodes().OfType<ConstructorDeclarationSyntax>().First();

            VerifyFlowGraph(compilation, node1, expectedFlowGraph:
@"
Block[B0] - Entry
    Statements (0)
    Next (Regular) Block[B1]
Block[B1] - Block
    Predecessors: [B0]
    Statements (2)
        IFlowCaptureOperation: 0 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: ': this(i ?? j)')
          Value: 
            IInstanceReferenceOperation (ReferenceKind: ContainingTypeInstance) (OperationKind.InstanceReference, Type: C, IsImplicit) (Syntax: ': this(i ?? j)')

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
        IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null, IsImplicit) (Syntax: ': this(i ?? j)')
          Expression: 
            IInvocationOperation ( C..ctor(System.Int32 i)) (OperationKind.Invocation, Type: System.Void) (Syntax: ': this(i ?? j)')
              Instance Receiver: 
                IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: C, IsImplicit) (Syntax: ': this(i ?? j)')
              Arguments(1):
                  IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: i) (OperationKind.Argument, Type: null) (Syntax: 'i ?? j')
                    IFlowCaptureReferenceOperation: 2 (OperationKind.FlowCaptureReference, Type: System.Int32, IsImplicit) (Syntax: 'i ?? j')
                    InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                    OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)

        IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'p = k;')
          Expression: 
            ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Int32) (Syntax: 'p = k')
              Left: 
                IParameterReferenceOperation: p (OperationKind.ParameterReference, Type: System.Int32) (Syntax: 'p')
              Right: 
                IParameterReferenceOperation: k (OperationKind.ParameterReference, Type: System.Int32) (Syntax: 'k')

    Next (Regular) Block[B5]
Block[B5] - Exit
    Predecessors: [B4]
    Statements (0)
");
        }
    }
}
