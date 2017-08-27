// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis.CSharp.Test.Utilities;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Test.Utilities;
using Xunit;
using System.Linq;
using Microsoft.CodeAnalysis.Semantics;

namespace Microsoft.CodeAnalysis.CSharp.UnitTests
{
    public partial class IOperationTests : SemanticModelTestBase
    {
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
@"IInvocationExpression (static void Cls.Test1(params System.Int32[] x)) (OperationKind.InvocationExpression, Type: System.Void) (Syntax: 'Test1(null)')
  Arguments(1): IArgument (ArgumentKind.Explicit, Matching Parameter: x) (OperationKind.Argument) (Syntax: 'null')
      IConversionExpression (ConversionKind.Cast, Implicit) (OperationKind.ConversionExpression, Type: System.Int32[], Constant: null) (Syntax: 'null')
        ILiteralExpression (Text: null) (OperationKind.LiteralExpression, Type: null, Constant: null) (Syntax: 'null')");

            compilation.VerifyOperationTree(nodes[1], expectedOperationTree:
@"IInvocationExpression (static void Cls.Test2(System.Int32 y, params System.Int32[] x)) (OperationKind.InvocationExpression, Type: System.Void, IsInvalid) (Syntax: 'Test2(new o ... ct(), null)')
  Arguments(2): IArgument (ArgumentKind.Explicit, Matching Parameter: null) (OperationKind.Argument, IsInvalid) (Syntax: 'new object()')
      IObjectCreationExpression (Constructor: System.Object..ctor()) (OperationKind.ObjectCreationExpression, Type: System.Object) (Syntax: 'new object()')
    IArgument (ArgumentKind.Explicit, Matching Parameter: null) (OperationKind.Argument, IsInvalid) (Syntax: 'null')
      ILiteralExpression (Text: null) (OperationKind.LiteralExpression, Type: null, Constant: null) (Syntax: 'null')");
        }

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
            Assert.Equal(OperationKind.None, operation1.Kind);
            Assert.False(operation1 is IAssignmentExpression);

            Assert.Equal("(x, y, z) = new C()", assignments[1].ToString());
            IOperation operation2 = model.GetOperation(assignments[1]);
            Assert.NotNull(operation2);
            Assert.Equal(OperationKind.None, operation2.Kind);
            Assert.False(operation2 is IAssignmentExpression);

            Assert.Equal("var (a, b) = (1, 2)", assignments[2].ToString());
            IOperation operation3 = model.GetOperation(assignments[2]);
            Assert.NotNull(operation3);
            Assert.Equal(OperationKind.None, operation3.Kind);
            Assert.False(operation3 is IAssignmentExpression);
        }
    }
}
