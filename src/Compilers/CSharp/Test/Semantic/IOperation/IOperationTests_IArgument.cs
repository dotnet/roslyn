// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.CSharp.Test.Utilities;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.CSharp.UnitTests
{
    public partial class IOperationTests : SemanticModelTestBase
    {
        [Fact]
        public void ExplicitSimpleArgument()
        {
            string source = @"
class P
{
    static void M1()
    {
        /*<bind>*/M2(1, """")/*</bind>*/;
    }

    static void M2(int x, string y) { }
}
";
            string expectedOperationTree = @"
IInvocationExpression (static void P.M2(System.Int32 x, System.String y)) (OperationKind.InvocationExpression, Type: System.Void)
  IArgument (ArgumentKind.Positional Matching Parameter: x) (OperationKind.Argument)
    ILiteralExpression (Text: 1) (OperationKind.LiteralExpression, Type: System.Int32, Constant: 1)
  IArgument (ArgumentKind.Positional Matching Parameter: y) (OperationKind.Argument)
    ILiteralExpression (OperationKind.LiteralExpression, Type: System.String, Constant: )
";

            VerifyOperationTreeForTest<InvocationExpressionSyntax>(source, expectedOperationTree);
        }

        [Fact]
        public void DefaultSimpleArgument()
        {
            string source = @"
class P
{
    static void M1()
    {
        /*<bind>*/M2(1)/*</bind>*/;
    }

    static void M2(int x, string y = null) { }
}
";
            string expectedOperationTree = @"
IInvocationExpression (static void P.M2(System.Int32 x, [System.String y = null])) (OperationKind.InvocationExpression, Type: System.Void)
  IArgument (ArgumentKind.Positional Matching Parameter: x) (OperationKind.Argument)
    ILiteralExpression (Text: 1) (OperationKind.LiteralExpression, Type: System.Int32, Constant: 1)
  IArgument (ArgumentKind.DefaultValue Matching Parameter: y) (OperationKind.Argument)
    ILiteralExpression (Text: null) (OperationKind.LiteralExpression, Type: System.String, Constant: null)
";

            VerifyOperationTreeForTest<InvocationExpressionSyntax>(source, expectedOperationTree);
        }

        [Fact]
        public void DefaultAndExplicitNamedArguments()
        {
            string source = @"
class P
{
    static void M1()
    {
        /*<bind>*/M2(1, z: 10)/*</bind>*/;
    }

    static void M2(int x = 1, int y = 2, int z = 3) { }
}
";
            string expectedOperationTree = @"
IInvocationExpression (static void P.M2([System.Int32 x = 1], [System.Int32 y = 2], [System.Int32 z = 3])) (OperationKind.InvocationExpression, Type: System.Void)
  IArgument (ArgumentKind.Positional Matching Parameter: x) (OperationKind.Argument)
    ILiteralExpression (Text: 1) (OperationKind.LiteralExpression, Type: System.Int32, Constant: 1)
  IArgument (ArgumentKind.Named Matching Parameter: z) (OperationKind.Argument)
    ILiteralExpression (Text: 10) (OperationKind.LiteralExpression, Type: System.Int32, Constant: 10)
  IArgument (ArgumentKind.DefaultValue Matching Parameter: y) (OperationKind.Argument)
    ILiteralExpression (Text: 2) (OperationKind.LiteralExpression, Type: System.Int32, Constant: 2)
";

            VerifyOperationTreeForTest<InvocationExpressionSyntax>(source, expectedOperationTree);
        }                         

        [Fact]
        public void ExplicitNamedArgument()
        {
            string source = @"
class P
{
    static void M1()
    {
        /*<bind>*/M2(x: 1, y: """")/*</bind>*/;
    }

    static void M2(int x, string y = null) { }
}
";
            string expectedOperationTree = @"
IInvocationExpression (static void P.M2(System.Int32 x, [System.String y = null])) (OperationKind.InvocationExpression, Type: System.Void)
  IArgument (ArgumentKind.Named Matching Parameter: x) (OperationKind.Argument)
    ILiteralExpression (Text: 1) (OperationKind.LiteralExpression, Type: System.Int32, Constant: 1)
  IArgument (ArgumentKind.Named Matching Parameter: y) (OperationKind.Argument)
    ILiteralExpression (OperationKind.LiteralExpression, Type: System.String, Constant: )
";

            VerifyOperationTreeForTest<InvocationExpressionSyntax>(source, expectedOperationTree);
        }

        [Fact]
        public void OutOfOrderExplicitNamedArgument()
        {
            string source = @"
class P
{
    static void M1()
    {
        /*<bind>*/M2(y: """", x: 1)/*</bind>*/;
    }

    static void M2(int x = 1, string y = null) { }
}
";
            string expectedOperationTree = @"
IInvocationExpression (static void P.M2([System.Int32 x = 1], [System.String y = null])) (OperationKind.InvocationExpression, Type: System.Void)
  IArgument (ArgumentKind.Named Matching Parameter: y) (OperationKind.Argument)
    ILiteralExpression (OperationKind.LiteralExpression, Type: System.String, Constant: )
  IArgument (ArgumentKind.Named Matching Parameter: x) (OperationKind.Argument)
    ILiteralExpression (Text: 1) (OperationKind.LiteralExpression, Type: System.Int32, Constant: 1)
";

            VerifyOperationTreeForTest<InvocationExpressionSyntax>(source, expectedOperationTree);
        }

        [Fact]
        public void DefaultNamedArgument()
        {
            string source = @"
class P
{
    static void M1()
    {
        /*<bind>*/M2(y: """")/*</bind>*/;
    }

    static void M2(int x = 1, string y = null) { }
}
";
            string expectedOperationTree = @"
IInvocationExpression (static void P.M2([System.Int32 x = 1], [System.String y = null])) (OperationKind.InvocationExpression, Type: System.Void)
  IArgument (ArgumentKind.Named Matching Parameter: y) (OperationKind.Argument)
    ILiteralExpression (OperationKind.LiteralExpression, Type: System.String, Constant: )
  IArgument (ArgumentKind.DefaultValue Matching Parameter: x) (OperationKind.Argument)
    ILiteralExpression (Text: 1) (OperationKind.LiteralExpression, Type: System.Int32, Constant: 1)
";

            VerifyOperationTreeForTest<InvocationExpressionSyntax>(source, expectedOperationTree);
        }
        [Fact]
        public void RefAndOutArguments()
        {
            string source = @"
class P
{
    void M1()
    {
        int a = 1;
        int b;
        /*<bind>*/M2(ref a, out b)/*</bind>*/;
    }

    void M2(ref int x, out int y) { y = 10; }
}
";
            string expectedOperationTree = @"
IInvocationExpression ( void P.M2(ref System.Int32 x, out System.Int32 y)) (OperationKind.InvocationExpression, Type: System.Void)
  Instance Receiver: IInstanceReferenceExpression (InstanceReferenceKind.Implicit) (OperationKind.InstanceReferenceExpression, Type: P)
  IArgument (ArgumentKind.Positional Matching Parameter: x) (OperationKind.Argument)
    ILocalReferenceExpression: a (OperationKind.LocalReferenceExpression, Type: System.Int32)
  IArgument (ArgumentKind.Positional Matching Parameter: y) (OperationKind.Argument)
    ILocalReferenceExpression: b (OperationKind.LocalReferenceExpression, Type: System.Int32)
";                       

            VerifyOperationTreeForTest<InvocationExpressionSyntax>(source, expectedOperationTree);
        }

        [Fact]
        public void NamedRefAndOutArguments()
        {
            string source = @"
class P
{
    void M1()
    {
        int a = 1;
        int b;
        /*<bind>*/M2(y: out b, x: ref a)/*</bind>*/;
    }

    void M2(ref int x, out int y) { y = 10; }
}
";
            string expectedOperationTree = @"
IInvocationExpression ( void P.M2(ref System.Int32 x, out System.Int32 y)) (OperationKind.InvocationExpression, Type: System.Void)
  Instance Receiver: IInstanceReferenceExpression (InstanceReferenceKind.Implicit) (OperationKind.InstanceReferenceExpression, Type: P)
  IArgument (ArgumentKind.Named Matching Parameter: y) (OperationKind.Argument)
    ILocalReferenceExpression: b (OperationKind.LocalReferenceExpression, Type: System.Int32)
  IArgument (ArgumentKind.Named Matching Parameter: x) (OperationKind.Argument)
    ILocalReferenceExpression: a (OperationKind.LocalReferenceExpression, Type: System.Int32)
";                 

            VerifyOperationTreeForTest<InvocationExpressionSyntax>(source, expectedOperationTree);
        }      

        [Fact]
        public void OmittedParamsArrayArgument()
        {
            string source = @"
class P
{
    static void M1(string[] args)
    {
        /*<bind>*/M2("""")/*</bind>*/;
    }

    static void M2(string str, params int[] array) { }
}
";
            string expectedOperationTree = @"
IInvocationExpression (static void P.M2(System.String str, params System.Int32[] array)) (OperationKind.InvocationExpression, Type: System.Void)
  IArgument (ArgumentKind.Positional Matching Parameter: str) (OperationKind.Argument)
    ILiteralExpression (OperationKind.LiteralExpression, Type: System.String, Constant: )
  IArgument (ArgumentKind.ParamArray Matching Parameter: array) (OperationKind.Argument)
    IArrayCreationExpression (Dimension sizes: 1, Element Type: System.Int32) (OperationKind.ArrayCreationExpression, Type: System.Int32[])
      ILiteralExpression (Text: 0) (OperationKind.LiteralExpression, Type: null, Constant: 0)
      IArrayInitializer (OperationKind.ArrayInitializer)
";

            VerifyOperationTreeForTest<InvocationExpressionSyntax>(source, expectedOperationTree);
        }

        [Fact]
        public void ParamsArrayArguments()
        {
            string source = @"
class P
{
    static void M1(string[] args)
    {
        /*<bind>*/M2("""", 1, 2)/*</bind>*/;
    }

    static void M2(string str, params int[] array) { }
}
";
            string expectedOperationTree = @"
IInvocationExpression (static void P.M2(System.String str, params System.Int32[] array)) (OperationKind.InvocationExpression, Type: System.Void)
  IArgument (ArgumentKind.Positional Matching Parameter: str) (OperationKind.Argument)
    ILiteralExpression (OperationKind.LiteralExpression, Type: System.String, Constant: )
  IArgument (ArgumentKind.ParamArray Matching Parameter: array) (OperationKind.Argument)
    IArrayCreationExpression (Dimension sizes: 1, Element Type: System.Int32) (OperationKind.ArrayCreationExpression, Type: System.Int32[])
      ILiteralExpression (Text: 2) (OperationKind.LiteralExpression, Type: null, Constant: 2)
      IArrayInitializer (OperationKind.ArrayInitializer)
        ILiteralExpression (Text: 1) (OperationKind.LiteralExpression, Type: System.Int32, Constant: 1)
        ILiteralExpression (Text: 2) (OperationKind.LiteralExpression, Type: System.Int32, Constant: 2)
";

            VerifyOperationTreeForTest<InvocationExpressionSyntax>(source, expectedOperationTree);
        }

        [Fact]
        public void ArrayAsParamsArrayArgument()
        {
            string source = @"
class P
{
    static void M1(string[] args)
    {
        /*<bind>*/M2("""", new int[] { 1, 2 })/*</bind>*/;
    }

    static void M2(string str, params int[] array) { }
}
";
            string expectedOperationTree = @"
IInvocationExpression (static void P.M2(System.String str, params System.Int32[] array)) (OperationKind.InvocationExpression, Type: System.Void)
  IArgument (ArgumentKind.Positional Matching Parameter: str) (OperationKind.Argument)
    ILiteralExpression (OperationKind.LiteralExpression, Type: System.String, Constant: )
  IArgument (ArgumentKind.Positional Matching Parameter: array) (OperationKind.Argument)
    IArrayCreationExpression (Dimension sizes: 1, Element Type: System.Int32) (OperationKind.ArrayCreationExpression, Type: System.Int32[])
      ILiteralExpression (OperationKind.LiteralExpression, Type: System.Int32, Constant: 2)
      IArrayInitializer (OperationKind.ArrayInitializer)
        ILiteralExpression (Text: 1) (OperationKind.LiteralExpression, Type: System.Int32, Constant: 1)
        ILiteralExpression (Text: 2) (OperationKind.LiteralExpression, Type: System.Int32, Constant: 2)
";

            VerifyOperationTreeForTest<InvocationExpressionSyntax>(source, expectedOperationTree);
        }

        [Fact]
        public void NamedParamsArrayArgument()
        {
            string source = @"
class P
{
    static void M1(string[] args)
    {
        /*<bind>*/M2(array: 1, str: """")/*</bind>*/;
    }

    static void M2(string str, params int[] array) { }
}
";
            string expectedOperationTree = @"
IInvocationExpression (static void P.M2(System.String str, params System.Int32[] array)) (OperationKind.InvocationExpression, Type: System.Void)
  IArgument (ArgumentKind.ParamArray Matching Parameter: array) (OperationKind.Argument)
    IArrayCreationExpression (Dimension sizes: 1, Element Type: System.Int32) (OperationKind.ArrayCreationExpression, Type: System.Int32[])
      ILiteralExpression (Text: 1) (OperationKind.LiteralExpression, Type: null, Constant: 1)
      IArrayInitializer (OperationKind.ArrayInitializer)
        ILiteralExpression (Text: 1) (OperationKind.LiteralExpression, Type: System.Int32, Constant: 1)
  IArgument (ArgumentKind.Named Matching Parameter: str) (OperationKind.Argument)
    ILiteralExpression (OperationKind.LiteralExpression, Type: System.String, Constant: )
";

            VerifyOperationTreeForTest<InvocationExpressionSyntax>(source, expectedOperationTree);
        }

        [Fact]
        public void ArrayAsNamedParamsArrayArgument()
        {
            string source = @"
class P
{
    static void M1(string[] args)
    {
        /*<bind>*/M2(array: new[] { 1 }, str: """")/*</bind>*/;
    }

    static void M2(string str, params int[] array) { }
}
";
            string expectedOperationTree = @"
IInvocationExpression (static void P.M2(System.String str, params System.Int32[] array)) (OperationKind.InvocationExpression, Type: System.Void)
  IArgument (ArgumentKind.Named Matching Parameter: array) (OperationKind.Argument)
    IArrayCreationExpression (Dimension sizes: 1, Element Type: System.Int32) (OperationKind.ArrayCreationExpression, Type: System.Int32[])
      ILiteralExpression (OperationKind.LiteralExpression, Type: System.Int32, Constant: 1)
      IArrayInitializer (OperationKind.ArrayInitializer)
        ILiteralExpression (Text: 1) (OperationKind.LiteralExpression, Type: System.Int32, Constant: 1)
  IArgument (ArgumentKind.Named Matching Parameter: str) (OperationKind.Argument)
    ILiteralExpression (OperationKind.LiteralExpression, Type: System.String, Constant: )
";

            VerifyOperationTreeForTest<InvocationExpressionSyntax>(source, expectedOperationTree);
        }
    }
}
