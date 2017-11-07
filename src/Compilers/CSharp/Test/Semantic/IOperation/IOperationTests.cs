// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Linq;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.CSharp.Test.Utilities;
using Microsoft.CodeAnalysis.Operations;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.CSharp.UnitTests
{
    [CompilerTrait(CompilerFeature.IOperation)]
    public partial class IOperationTests : SemanticModelTestBase
    {
        [CompilerTrait(CompilerFeature.IOperation)]
        [Fact]
        [WorkItem(382240, "https://devdiv.visualstudio.com/DevDiv/_workitems?id=382240")]
        public void NullInPlaceOfParamArray()
        {
            var text = @"
public class Cls
{
    public static void Main()
    {
        Test1(null);
        Test2(new object(), null);
    }

    static void Test1(params int[] x)
    {
    }

    static void Test2(int y, params int[] x)
    {
    }
}";
            var compilation = CreateStandardCompilation(text, options: TestOptions.ReleaseExe, parseOptions: TestOptions.Regular);
            compilation.VerifyDiagnostics(
                // (7,15): error CS1503: Argument 1: cannot convert from 'object' to 'int'
                //         Test2(new object(), null);
                Diagnostic(ErrorCode.ERR_BadArgType, "new object()").WithArguments("1", "object", "int").WithLocation(7, 15)
                );

            var tree = compilation.SyntaxTrees.Single();
            var nodes = tree.GetRoot().DescendantNodes().OfType<InvocationExpressionSyntax>().ToArray();

            compilation.VerifyOperationTree(nodes[0], expectedOperationTree:
@"IInvocationOperation (void Cls.Test1(params System.Int32[] x)) (OperationKind.Invocation, Type: System.Void) (Syntax: 'Test1(null)')
  Instance Receiver: 
    null
  Arguments(1):
      IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: x) (OperationKind.Argument, Type: null) (Syntax: 'null')
        IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.Int32[], Constant: null, IsImplicit) (Syntax: 'null')
          Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: True, IsUserDefined: False) (MethodSymbol: null)
          Operand: 
            ILiteralOperation (OperationKind.Literal, Type: null, Constant: null) (Syntax: 'null')
        InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
        OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
");

            compilation.VerifyOperationTree(nodes[1], expectedOperationTree:
@"IInvalidOperation (OperationKind.Invalid, Type: System.Void, IsInvalid) (Syntax: 'Test2(new o ... ct(), null)')
  Children(2):
      IObjectCreationOperation (Constructor: System.Object..ctor()) (OperationKind.ObjectCreation, Type: System.Object, IsInvalid) (Syntax: 'new object()')
        Arguments(0)
        Initializer: 
          null
      ILiteralOperation (OperationKind.Literal, Type: null, Constant: null) (Syntax: 'null')");
        }

        [CompilerTrait(CompilerFeature.IOperation)]
        [Fact]
        public void DeconstructionAssignmentFromTuple()
        {
            var text = @"
public class C
{
    public static void M()
    {
        int x, y, z;
        (x, y, z) = (1, 2, 3);
        (x, y, z) = new C();
        var (a, b) = (1, 2);
    }
    public void Deconstruct(out int a, out int b, out int c)
    {
        a = b = c = 1;
    }
}";
            var compilation = CreateStandardCompilation(text, references: new[] { ValueTupleRef, SystemRuntimeFacadeRef }, parseOptions: TestOptions.RegularWithIOperationFeature);
            compilation.VerifyDiagnostics();
            var tree = compilation.SyntaxTrees.Single();
            var model = compilation.GetSemanticModel(tree);

            var assignments = tree.GetRoot().DescendantNodes().OfType<AssignmentExpressionSyntax>().ToArray();
            Assert.Equal("(x, y, z) = (1, 2, 3)", assignments[0].ToString());
            IOperation operation1 = model.GetOperation(assignments[0]);
            Assert.NotNull(operation1);
            Assert.Equal(OperationKind.DeconstructionAssignment, operation1.Kind);
            Assert.False(operation1 is ISimpleAssignmentOperation);

            Assert.Equal("(x, y, z) = new C()", assignments[1].ToString());
            IOperation operation2 = model.GetOperation(assignments[1]);
            Assert.NotNull(operation2);
            Assert.Equal(OperationKind.DeconstructionAssignment, operation2.Kind);
            Assert.False(operation2 is ISimpleAssignmentOperation);

            Assert.Equal("var (a, b) = (1, 2)", assignments[2].ToString());
            IOperation operation3 = model.GetOperation(assignments[2]);
            Assert.NotNull(operation3);
            Assert.Equal(OperationKind.DeconstructionAssignment, operation3.Kind);
            Assert.False(operation3 is ISimpleAssignmentOperation);
        }

        [CompilerTrait(CompilerFeature.IOperation)]
        [Fact]
        public void TestClone()
        {
            var sourceCode = TestResource.AllInOneCSharpCode;

            var compilation = CreateStandardCompilation(sourceCode, new[] { SystemRef, SystemCoreRef, ValueTupleRef, SystemRuntimeFacadeRef }, sourceFileName: "file.cs");
            var tree = compilation.SyntaxTrees[0];
            var model = compilation.GetSemanticModel(tree);

            VerifyClone(model);
        }

        [CompilerTrait(CompilerFeature.IOperation)]
        [WorkItem(22964, "https://github.com/dotnet/roslyn/issues/22964")]
        [Fact]
        public void GlobalStatement_Parent()
        {
            var source =
@"
System.Console.WriteLine();
";
            var compilation = CreateStandardCompilation(source, options: TestOptions.ReleaseExe.WithScriptClassName("Script"), parseOptions: TestOptions.Script);
            compilation.VerifyDiagnostics();

            var tree = compilation.SyntaxTrees.Single();
            var statement = tree.GetRoot().DescendantNodes().OfType<StatementSyntax>().Single();
            var model = compilation.GetSemanticModel(tree);
            var operation = model.GetOperationInternal(statement);

            Assert.Equal(OperationKind.ExpressionStatement, operation.Kind);
            Assert.Null(operation.Parent);
        }

        [CompilerTrait(CompilerFeature.IOperation)]
        [Fact]
        public void TestParentOperations()
        {
            var sourceCode = TestResource.AllInOneCSharpCode;
            
            var compilation = CreateStandardCompilation(sourceCode, new[] { SystemRef, SystemCoreRef, ValueTupleRef, SystemRuntimeFacadeRef }, sourceFileName: "file.cs");
            var tree = compilation.SyntaxTrees[0];
            var model = compilation.GetSemanticModel(tree);

            VerifyParentOperations(model);
        }

        [CompilerTrait(CompilerFeature.IOperation)]
        [WorkItem(23001, "https://github.com/dotnet/roslyn/issues/23001")]
        [Fact]
        public void TestGetOperationForQualifiedName()
        {
            var text = @"using System;

public class Test
{
    class A
    {
        public B b;
    }
    class B
    {
    }
    
    void M(A a)
    {
        int x2 = /*<bind>*/a.b/*</bind>*/;
    }
}
";
            var comp = CreateStandardCompilation(text, parseOptions: TestOptions.RegularWithIOperationFeature);
            var tree = comp.SyntaxTrees.Single();
            var model = comp.GetSemanticModel(tree);

            // Verify we return non-null operation only for topmost member access expression.
            var expr = (MemberAccessExpressionSyntax)GetExprSyntaxForBinding(GetExprSyntaxList(tree));
            Assert.Equal("a.b", expr.ToString());
            var operation = model.GetOperation(expr);
            Assert.NotNull(operation);
            Assert.Equal(OperationKind.FieldReference, operation.Kind);
            var fieldOperation = (IFieldReferenceOperation)operation;
            Assert.Equal("b", fieldOperation.Field.Name);

            // Verify we return null operation for child nodes of member access expression.
            Assert.Null(model.GetOperation(expr.Name));
        }
    }
}
