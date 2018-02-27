// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Linq;
using Microsoft.CodeAnalysis.CSharp.Syntax;
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
            var compilation = CreateStandardCompilation(source);

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

        [Fact]
        public void ConstructorBody_02()
        {
            string source = @"
class C
{
    public C() : base()
}
";
            var compilation = CreateStandardCompilation(source);

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
            IInstanceReferenceOperation (OperationKind.InstanceReference, Type: System.Object, IsInvalid, IsImplicit) (Syntax: ': base()')
          Arguments(0)
  BlockBody: 
    null
  ExpressionBody: 
    null
");
        }

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
            var compilation = CreateStandardCompilation(source);

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
            IInstanceReferenceOperation (OperationKind.InstanceReference, Type: System.Object, IsImplicit) (Syntax: ': base()')
          Arguments(0)
  BlockBody: 
    IBlockOperation (1 statements) (OperationKind.Block, Type: null) (Syntax: '{ throw null; }')
      IThrowOperation (OperationKind.Throw, Type: null) (Syntax: 'throw null;')
        ILiteralOperation (OperationKind.Literal, Type: null, Constant: null) (Syntax: 'null')
  ExpressionBody: 
    null
");
        }

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
            var compilation = CreateStandardCompilation(source);

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
            IInstanceReferenceOperation (OperationKind.InstanceReference, Type: System.Object, IsImplicit) (Syntax: ': base()')
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
        }

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
            var compilation = CreateStandardCompilation(source);

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
        }

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
            var compilation = CreateStandardCompilation(source);

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
        }

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
            var compilation = CreateStandardCompilation(source);

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
            var compilation = CreateStandardCompilation(source);

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
    }
}
