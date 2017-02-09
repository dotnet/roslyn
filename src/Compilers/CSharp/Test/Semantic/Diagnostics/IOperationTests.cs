// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis.CSharp.Test.Utilities;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Test.Utilities;
using Xunit;
using System.Linq;

namespace Microsoft.CodeAnalysis.CSharp.UnitTests
{
    public class IOperationTests : CompilingTestBase
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
            var compilation = CreateCompilationWithMscorlib(text, options: TestOptions.ReleaseExe, parseOptions: TestOptions.Regular);
            compilation.VerifyDiagnostics(
                // (7,15): error CS1503: Argument 1: cannot convert from 'object' to 'int'
                //         Test2(new object(), null);
                Diagnostic(ErrorCode.ERR_BadArgType, "new object()").WithArguments("1", "object", "int").WithLocation(7, 15)
                );

            var tree = compilation.SyntaxTrees.Single();
            var nodes = tree.GetRoot().DescendantNodes().OfType<InvocationExpressionSyntax>().ToArray();

            compilation.VerifyOperationTree(nodes[0], expectedOperationTree:
@"IInvocationExpression (static void Cls.Test1(params System.Int32[] x)) (OperationKind.InvocationExpression, Type: System.Void)
  IArgument (Matching Parameter: x) (OperationKind.Argument)
    IConversionExpression (ConversionKind.Cast, Implicit) (OperationKind.ConversionExpression, Type: System.Int32[], Constant: null)
      ILiteralExpression (Text: null) (OperationKind.LiteralExpression, Type: null, Constant: null)");

            compilation.VerifyOperationTree(nodes[1], expectedOperationTree:
@"IInvocationExpression (static void Cls.Test2(System.Int32 y, params System.Int32[] x)) (OperationKind.InvocationExpression, Type: System.Void, IsInvalid)
  IArgument (Matching Parameter: y) (OperationKind.Argument)
    IObjectCreationExpression (Constructor: System.Object..ctor()) (OperationKind.ObjectCreationExpression, Type: System.Object)
  IArgument (Matching Parameter: x) (OperationKind.Argument)
    ILiteralExpression (Text: null) (OperationKind.LiteralExpression, Type: null, Constant: null)");
        }
    }
}