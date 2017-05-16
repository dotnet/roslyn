// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.CSharp.UnitTests
{
    public partial class IOperationTests : SemanticModelTestBase
    {
        [Fact]
        public void PositionalArgument()
        {
            string source = @"
class P
{
    static void M1()
    {
        /*<bind>*/M2(1, 2.0)/*</bind>*/;
    }

    static void M2(int x, double y) { }
}
";
            string expectedOperationTree = @"
IInvocationExpression (static void P.M2(System.Int32 x, System.Double y)) (OperationKind.InvocationExpression, Type: System.Void) (Syntax: 'M2(1, 2.0)')
  Arguments(2): IArgument (ArgumentKind.Explicit, Matching Parameter: x) (OperationKind.Argument) (Syntax: '1')
      ILiteralExpression (Text: 1) (OperationKind.LiteralExpression, Type: System.Int32, Constant: 1) (Syntax: '1')
    IArgument (ArgumentKind.Explicit, Matching Parameter: y) (OperationKind.Argument) (Syntax: '2.0')
      ILiteralExpression (OperationKind.LiteralExpression, Type: System.Double, Constant: 2) (Syntax: '2.0')
";
            var expectedDiagnostics = DiagnosticDescription.None;

            VerifyOperationTreeAndDiagnosticsForTest<InvocationExpressionSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }

        [Fact(Skip = "https://github.com/dotnet/roslyn/issues/18549")]
        public void PositionalArgumentWithDefaultValue()
        {
            string source = @"
class P
{
    static void M1()
    {
        /*<bind>*/M2(1)/*</bind>*/;
    }

    static void M2(int x, double y = 0.0) { }
}
";
            string expectedOperationTree = @"
IInvocationExpression (static void P.M2(System.Int32 x, [System.Double y = 0])) (OperationKind.InvocationExpression, Type: System.Void) (Syntax: 'M2(1)')
  Arguments(2): IArgument (ArgumentKind.Explicit, Matching Parameter: x) (OperationKind.Argument) (Syntax: '1')
      ILiteralExpression (Text: 1) (OperationKind.LiteralExpression, Type: System.Int32, Constant: 1) (Syntax: '1')
    IArgument (ArgumentKind.DefaultValue, Matching Parameter: y) (OperationKind.Argument) (Syntax: 'M2(1)')
      ILiteralExpression (Text: 0) (OperationKind.LiteralExpression, Type: System.Double, Constant: 0) (Syntax: 'M2(1)')
";
            var expectedDiagnostics = DiagnosticDescription.None;

            VerifyOperationTreeAndDiagnosticsForTest<InvocationExpressionSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }

        [Fact]
        public void NamedArgumentListedInParameterOrder()
        {
            string source = @"
class P
{
    static void M1()
    {
        /*<bind>*/M2(x: 1, y: 9.9)/*</bind>*/;
    }

    static void M2(int x, double y = 0.0) { }
}
";
            string expectedOperationTree = @"
IInvocationExpression (static void P.M2(System.Int32 x, [System.Double y = 0])) (OperationKind.InvocationExpression, Type: System.Void) (Syntax: 'M2(x: 1, y: 9.9)')
  Arguments(2): IArgument (ArgumentKind.Explicit, Matching Parameter: x) (OperationKind.Argument) (Syntax: '1')
      ILiteralExpression (Text: 1) (OperationKind.LiteralExpression, Type: System.Int32, Constant: 1) (Syntax: '1')
    IArgument (ArgumentKind.Explicit, Matching Parameter: y) (OperationKind.Argument) (Syntax: '9.9')
      ILiteralExpression (Text: 9.9) (OperationKind.LiteralExpression, Type: System.Double, Constant: 9.9) (Syntax: '9.9')
";
            var expectedDiagnostics = DiagnosticDescription.None;

            VerifyOperationTreeAndDiagnosticsForTest<InvocationExpressionSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }

        [Fact(Skip = "https://github.com/dotnet/roslyn/issues/18549")]
        public void NamedArgumentListedOutOfParameterOrder()
        {
            string source = @"
class P
{
    static void M1()
    {
        /*<bind>*/M2(y: 9.9, x: 1)/*</bind>*/;
    }

    static void M2(int x, double y = 0.0) { }
}
";
            string expectedOperationTree = @"
IInvocationExpression (static void P.M2(System.Int32 x, [System.Double y = 0])) (OperationKind.InvocationExpression, Type: System.Void) (Syntax: 'M2(y: 9.9, x: 1)')
  Arguments(2): IArgument (ArgumentKind.Explicit, Matching Parameter: y) (OperationKind.Argument) (Syntax: '9.9')
      ILiteralExpression (Text: 9.9) (OperationKind.LiteralExpression, Type: System.Double, Constant: 9.9) (Syntax: '9.9')
    IArgument (ArgumentKind.Explicit, Matching Parameter: x) (OperationKind.Argument) (Syntax: '1')
      ILiteralExpression (Text: 1) (OperationKind.LiteralExpression, Type: System.Int32, Constant: 1) (Syntax: '1')
";
            var expectedDiagnostics = DiagnosticDescription.None;

            VerifyOperationTreeAndDiagnosticsForTest<InvocationExpressionSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }

        [Fact(Skip = "https://github.com/dotnet/roslyn/issues/18549")]
        public void NamedArgumentInParameterOrderWithDefaultValue()
        {
            string source = @"
class P
{
    static void M1()
    {
        /*<bind>*/M2(y: 0, z: 2)/*</bind>*/;
    }

    static void M2(int x = 1, int y = 2, int z = 3) { }
}
";
            string expectedOperationTree = @"
IInvocationExpression (static void P.M2([System.Int32 x = 1], [System.Int32 y = 2], [System.Int32 z = 3])) (OperationKind.InvocationExpression, Type: System.Void) (Syntax: 'M2(y: 0, z: 2)')
  Arguments(3): IArgument (ArgumentKind.Explicit, Matching Parameter: y) (OperationKind.Argument) (Syntax: '0')
      ILiteralExpression (Text: 0) (OperationKind.LiteralExpression, Type: System.Int32, Constant: 0) (Syntax: '0')
    IArgument (ArgumentKind.Explicit, Matching Parameter: z) (OperationKind.Argument) (Syntax: '2')
      ILiteralExpression (Text: 2) (OperationKind.LiteralExpression, Type: System.Int32, Constant: 2) (Syntax: '2')
    IArgument (ArgumentKind.DefaultValue, Matching Parameter: x) (OperationKind.Argument) (Syntax: 'M2(y: 0, z: 2)')
      ILiteralExpression (Text: 1) (OperationKind.LiteralExpression, Type: System.Int32, Constant: 1) (Syntax: 'M2(y: 0, z: 2)')
";
            var expectedDiagnostics = DiagnosticDescription.None;

            VerifyOperationTreeAndDiagnosticsForTest<InvocationExpressionSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }

        [Fact(Skip = "https://github.com/dotnet/roslyn/issues/18549")]
        public void NamedArgumentOutOfParameterOrderWithDefaultValue()
        {
            string source = @"
class P
{
    static void M1()
    {
        /*<bind>*/M2(z: 2, x: 9)/*</bind>*/;
    }

    static void M2(int x = 1, int y = 2, int z = 3) { }
}
";
            string expectedOperationTree = @"
IInvocationExpression (static void P.M2([System.Int32 x = 1], [System.Int32 y = 2], [System.Int32 z = 3])) (OperationKind.InvocationExpression, Type: System.Void) (Syntax: 'M2(z: 2, x: 9)')
  Arguments(3): IArgument (ArgumentKind.Explicit, Matching Parameter: z) (OperationKind.Argument) (Syntax: '2')
      ILiteralExpression (Text: 2) (OperationKind.LiteralExpression, Type: System.Int32, Constant: 2) (Syntax: '2')
    IArgument (ArgumentKind.Explicit, Matching Parameter: x) (OperationKind.Argument) (Syntax: '9')
      ILiteralExpression (Text: 9) (OperationKind.LiteralExpression, Type: System.Int32, Constant: 9) (Syntax: '9')
    IArgument (ArgumentKind.DefaultValue, Matching Parameter: y) (OperationKind.Argument) (Syntax: 'M2(z: 2, x: 9)')
      ILiteralExpression (Text: 2) (OperationKind.LiteralExpression, Type: System.Int32, Constant: 2) (Syntax: 'M2(z: 2, x: 9)')
";
            var expectedDiagnostics = DiagnosticDescription.None;

            VerifyOperationTreeAndDiagnosticsForTest<InvocationExpressionSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }

        [Fact(Skip = "https://github.com/dotnet/roslyn/issues/18549")]
        public void NamedAndPositionalArgumentsWithDefaultValue()
        {
            string source = @"
class P
{
    static void M1()
    {
        /*<bind>*/M2(9, z: 10);/*</bind>*/
    }

    static void M2(int x = 1, int y = 2, int z = 3) { }
}
";
            string expectedOperationTree = @"
IExpressionStatement (OperationKind.ExpressionStatement) (Syntax: 'M2(9, z: 10);')
  IInvocationExpression (static void P.M2([System.Int32 x = 1], [System.Int32 y = 2], [System.Int32 z = 3])) (OperationKind.InvocationExpression, Type: System.Void) (Syntax: 'M2(9, z: 10)')
    Arguments(3): IArgument (ArgumentKind.Explicit, Matching Parameter: x) (OperationKind.Argument) (Syntax: '9')
        ILiteralExpression (Text: 9) (OperationKind.LiteralExpression, Type: System.Int32, Constant: 9) (Syntax: '9')
      IArgument (ArgumentKind.Explicit, Matching Parameter: z) (OperationKind.Argument) (Syntax: '10')
        ILiteralExpression (Text: 10) (OperationKind.LiteralExpression, Type: System.Int32, Constant: 10) (Syntax: '10')
      IArgument (ArgumentKind.DefaultValue, Matching Parameter: y) (OperationKind.Argument) (Syntax: 'M2(9, z: 10)')
        ILiteralExpression (Text: 2) (OperationKind.LiteralExpression, Type: System.Int32, Constant: 2) (Syntax: 'M2(9, z: 10)')
";
            var expectedDiagnostics = DiagnosticDescription.None;

            VerifyOperationTreeAndDiagnosticsForTest<ExpressionStatementSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }

        [Fact]
        public void PositionalRefAndOutArguments()
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
IInvocationExpression ( void P.M2(ref System.Int32 x, out System.Int32 y)) (OperationKind.InvocationExpression, Type: System.Void) (Syntax: 'M2(ref a, out b)')
  Instance Receiver: IInstanceReferenceExpression (InstanceReferenceKind.Implicit) (OperationKind.InstanceReferenceExpression, Type: P) (Syntax: 'M2')
  Arguments(2): IArgument (ArgumentKind.Explicit, Matching Parameter: x) (OperationKind.Argument) (Syntax: 'a')
      ILocalReferenceExpression: a (OperationKind.LocalReferenceExpression, Type: System.Int32) (Syntax: 'a')
    IArgument (ArgumentKind.Explicit, Matching Parameter: y) (OperationKind.Argument) (Syntax: 'b')
      ILocalReferenceExpression: b (OperationKind.LocalReferenceExpression, Type: System.Int32) (Syntax: 'b')
";
            var expectedDiagnostics = DiagnosticDescription.None;

            VerifyOperationTreeAndDiagnosticsForTest<InvocationExpressionSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }

        [Fact]
        public void NamedRefAndOutArgumentsInParameterOrder()
        {
            string source = @"
class P
{
    void M1()
    {
        int a = 1;
        int b;
        /*<bind>*/M2(x: ref a, y: out b)/*</bind>*/;
    }

    void M2(ref int x, out int y) { y = 10; }
}
";
            string expectedOperationTree = @"
IInvocationExpression ( void P.M2(ref System.Int32 x, out System.Int32 y)) (OperationKind.InvocationExpression, Type: System.Void) (Syntax: 'M2(x: ref a, y: out b)')
  Instance Receiver: IInstanceReferenceExpression (InstanceReferenceKind.Implicit) (OperationKind.InstanceReferenceExpression, Type: P) (Syntax: 'M2')
  Arguments(2): IArgument (ArgumentKind.Explicit, Matching Parameter: x) (OperationKind.Argument) (Syntax: 'a')
      ILocalReferenceExpression: a (OperationKind.LocalReferenceExpression, Type: System.Int32) (Syntax: 'a')
    IArgument (ArgumentKind.Explicit, Matching Parameter: y) (OperationKind.Argument) (Syntax: 'b')
      ILocalReferenceExpression: b (OperationKind.LocalReferenceExpression, Type: System.Int32) (Syntax: 'b')
";
            var expectedDiagnostics = DiagnosticDescription.None;

            VerifyOperationTreeAndDiagnosticsForTest<InvocationExpressionSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }

        [Fact(Skip = "https://github.com/dotnet/roslyn/issues/18549")]
        public void NamedRefAndOutArgumentsOutOfParameterOrder()
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
            string expectedOperationTree = @"";
            var expectedDiagnostics = DiagnosticDescription.None;

            VerifyOperationTreeAndDiagnosticsForTest<InvocationExpressionSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }

        [Fact(Skip = "https://github.com/dotnet/roslyn/issues/18549")]
        public void DefaultValueOfNewStruct()
        {
            string source = @"
class P
{
    void M1()
    {
        /*<bind>*/M2()/*</bind>*/;
    }

    void M2(S sobj = new S()) { }
}

struct S { }
";
            string expectedOperationTree = @"";
            var expectedDiagnostics = DiagnosticDescription.None;

            VerifyOperationTreeAndDiagnosticsForTest<InvocationExpressionSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }

        [Fact(Skip = "https://github.com/dotnet/roslyn/issues/18549")]
        public void DefaultValueOfDefaultStruct()
        {
            string source = @"
class P
{
    void M1()
    {
        /*<bind>*/M2()/*</bind>*/;
    }

    void M2(S sobj = default(S)) { }
}

struct S { }
";
            string expectedOperationTree = @"";
            var expectedDiagnostics = DiagnosticDescription.None;

            VerifyOperationTreeAndDiagnosticsForTest<InvocationExpressionSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }

        [Fact(Skip = "https://github.com/dotnet/roslyn/issues/18549")]
        public void DefaultValueOfConstant()
        {
            string source = @"
class P
{
    const double Pi = 3.14;
    void M1()
    {
        /*<bind>*/M2()/*</bind>*/;
    }

    void M2(double s = Pi) { }
}
";
            string expectedOperationTree = @"
IInvocationExpression ( void P.M2([System.Double s = 3.14])) (OperationKind.InvocationExpression, Type: System.Void) (Syntax: 'M2()')
  Instance Receiver: IInstanceReferenceExpression (InstanceReferenceKind.Implicit) (OperationKind.InstanceReferenceExpression, Type: P) (Syntax: 'M2')
  Arguments(1): IArgument (ArgumentKind.DefaultValue, Matching Parameter: s) (OperationKind.Argument) (Syntax: 'M2()')
      ILiteralExpression (Text: 3.14) (OperationKind.LiteralExpression, Type: System.Double, Constant: 3.14) (Syntax: 'M2()')
";
            var expectedDiagnostics = DiagnosticDescription.None;

            VerifyOperationTreeAndDiagnosticsForTest<InvocationExpressionSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }

        [Fact]
        public void PositionalArgumentForExtensionMethod()
        {
            string source = @"
class P
{
    void M1()
    {
        /*<bind>*/this.E1(1, 2)/*</bind>*/;
    }
}

static class Extensions
{
    public static void E1(this P p, int x = 0, int y = 0)
    { }
}
";
            string expectedOperationTree = @"
IInvocationExpression (static void Extensions.E1(this P p, [System.Int32 x = 0], [System.Int32 y = 0])) (OperationKind.InvocationExpression, Type: System.Void) (Syntax: 'this.E1(1, 2)')
  Arguments(3): IArgument (ArgumentKind.Explicit, Matching Parameter: p) (OperationKind.Argument) (Syntax: 'this')
      IInstanceReferenceExpression (InstanceReferenceKind.Explicit) (OperationKind.InstanceReferenceExpression, Type: P) (Syntax: 'this')
    IArgument (ArgumentKind.Explicit, Matching Parameter: x) (OperationKind.Argument) (Syntax: '1')
      ILiteralExpression (Text: 1) (OperationKind.LiteralExpression, Type: System.Int32, Constant: 1) (Syntax: '1')
    IArgument (ArgumentKind.Explicit, Matching Parameter: y) (OperationKind.Argument) (Syntax: '2')
      ILiteralExpression (Text: 2) (OperationKind.LiteralExpression, Type: System.Int32, Constant: 2) (Syntax: '2')
";
            var expectedDiagnostics = DiagnosticDescription.None;

            VerifyOperationTreeAndDiagnosticsForTest<InvocationExpressionSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }

        [Fact(Skip = "https://github.com/dotnet/roslyn/issues/18549")]
        public void NamedArgumentOutOfParameterOrderForExtensionMethod()
        {
            string source = @"
class P
{
    void M1()
    {
        /*<bind>*/this.E1(y: 1, x: 2);/*</bind>*/
    }
}

static class Extensions
{
    public static void E1(this P p, int x = 0, int y = 0)
    { }
}
";
            string expectedOperationTree = @"
IExpressionStatement (OperationKind.ExpressionStatement) (Syntax: 'this.E1(y: 1, x: 2);')
  IInvocationExpression (static void Extensions.E1(this P p, [System.Int32 x = 0], [System.Int32 y = 0])) (OperationKind.InvocationExpression, Type: System.Void) (Syntax: 'this.E1(y: 1, x: 2)')
    Arguments(3): IArgument (ArgumentKind.Explicit, Matching Parameter: p) (OperationKind.Argument) (Syntax: 'this')
        IInstanceReferenceExpression (InstanceReferenceKind.Explicit) (OperationKind.InstanceReferenceExpression, Type: P) (Syntax: 'this')
      IArgument (ArgumentKind.Explicit, Matching Parameter: y) (OperationKind.Argument) (Syntax: '1')
        ILiteralExpression (Text: 1) (OperationKind.LiteralExpression, Type: System.Int32, Constant: 1) (Syntax: '1')
      IArgument (ArgumentKind.Explicit, Matching Parameter: x) (OperationKind.Argument) (Syntax: '2')
        ILiteralExpression (Text: 2) (OperationKind.LiteralExpression, Type: System.Int32, Constant: 2) (Syntax: '2')
";
            var expectedDiagnostics = DiagnosticDescription.None;

            VerifyOperationTreeAndDiagnosticsForTest<ExpressionStatementSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }

        [Fact(Skip = "https://github.com/dotnet/roslyn/issues/18549")]
        public void NamedArgumentWithDefaultValueForExtensionMethod()
        {
            string source = @"
class P
{
    void M1()
    {
        /*<bind>*/this.E1(y: 1)/*</bind>*/;
    }
}

static class Extensions
{
    public static void E1(this P p, int x = 0, int y = 0)
    { }
}
";
            string expectedOperationTree = @"
IInvocationExpression (static void Extensions.E1(this P p, [System.Int32 x = 0], [System.Int32 y = 0])) (OperationKind.InvocationExpression, Type: System.Void) (Syntax: 'this.E1(y: 1)')
  Arguments(3): IArgument (ArgumentKind.Explicit, Matching Parameter: p) (OperationKind.Argument) (Syntax: 'this')
      IInstanceReferenceExpression (InstanceReferenceKind.Explicit) (OperationKind.InstanceReferenceExpression, Type: P) (Syntax: 'this')
    IArgument (ArgumentKind.Explicit, Matching Parameter: y) (OperationKind.Argument) (Syntax: '1')
      ILiteralExpression (Text: 1) (OperationKind.LiteralExpression, Type: System.Int32, Constant: 1) (Syntax: '1')
    IArgument (ArgumentKind.DefaultValue, Matching Parameter: x) (OperationKind.Argument) (Syntax: 'this.E1(y: 1)')
      ILiteralExpression (Text: 0) (OperationKind.LiteralExpression, Type: System.Int32, Constant: 0) (Syntax: 'this.E1(y: 1)')
";
            var expectedDiagnostics = DiagnosticDescription.None;

            VerifyOperationTreeAndDiagnosticsForTest<InvocationExpressionSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }

        [Fact]
        public void ParamsArrayArgumentInNormalForm()
        {
            string source = @"
class P
{
    void M1()
    {
        var a = new[] { 0.0 };
        /*<bind>*/M2(1, a)/*</bind>*/;
    }

    void M2(int x, params double[] array) { }
}
";
            string expectedOperationTree = @"
IInvocationExpression ( void P.M2(System.Int32 x, params System.Double[] array)) (OperationKind.InvocationExpression, Type: System.Void) (Syntax: 'M2(1, a)')
  Instance Receiver: IInstanceReferenceExpression (InstanceReferenceKind.Implicit) (OperationKind.InstanceReferenceExpression, Type: P) (Syntax: 'M2')
  Arguments(2): IArgument (ArgumentKind.Explicit, Matching Parameter: x) (OperationKind.Argument) (Syntax: '1')
      ILiteralExpression (Text: 1) (OperationKind.LiteralExpression, Type: System.Int32, Constant: 1) (Syntax: '1')
    IArgument (ArgumentKind.Explicit, Matching Parameter: array) (OperationKind.Argument) (Syntax: 'a')
      ILocalReferenceExpression: a (OperationKind.LocalReferenceExpression, Type: System.Double[]) (Syntax: 'a')
";
            var expectedDiagnostics = DiagnosticDescription.None;

            VerifyOperationTreeAndDiagnosticsForTest<InvocationExpressionSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }

        [Fact]
        public void ParamsArrayArgumentInExpandedForm()
        {
            string source = @"
class P
{
    void M1()
    {
        /*<bind>*/M2(1, 0.1, 0.2)/*</bind>*/;
    }

    void M2(int x, params double[] array) { }
}
";
            string expectedOperationTree = @"
IInvocationExpression ( void P.M2(System.Int32 x, params System.Double[] array)) (OperationKind.InvocationExpression, Type: System.Void) (Syntax: 'M2(1, 0.1, 0.2)')
  Instance Receiver: IInstanceReferenceExpression (InstanceReferenceKind.Implicit) (OperationKind.InstanceReferenceExpression, Type: P) (Syntax: 'M2')
  Arguments(2): IArgument (ArgumentKind.Explicit, Matching Parameter: x) (OperationKind.Argument) (Syntax: '1')
      ILiteralExpression (Text: 1) (OperationKind.LiteralExpression, Type: System.Int32, Constant: 1) (Syntax: '1')
    IArgument (ArgumentKind.ParamArray, Matching Parameter: array) (OperationKind.Argument) (Syntax: '0.1')
      IArrayCreationExpression (Element Type: System.Double) (OperationKind.ArrayCreationExpression, Type: System.Double[]) (Syntax: '0.1')
        Dimension Sizes(1): ILiteralExpression (Text: 2) (OperationKind.LiteralExpression, Type: null, Constant: 2) (Syntax: '0.1')
        Initializer: IArrayInitializer (2 elements) (OperationKind.ArrayInitializer) (Syntax: '0.1')
            Element Values(2): ILiteralExpression (Text: 0.1) (OperationKind.LiteralExpression, Type: System.Double, Constant: 0.1) (Syntax: '0.1')
              ILiteralExpression (Text: 0.2) (OperationKind.LiteralExpression, Type: System.Double, Constant: 0.2) (Syntax: '0.2')
";
            var expectedDiagnostics = DiagnosticDescription.None;

            VerifyOperationTreeAndDiagnosticsForTest<InvocationExpressionSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }

        [Fact(Skip = "https://github.com/dotnet/roslyn/issues/18549")]
        public void ParamsArrayArgumentInExpandedFormWithNoArgument()
        {
            string source = @"
class P
{
    void M1()
    {
        /*<bind>*/M2(1)/*</bind>*/;
    }

    void M2(int x, params double[] array) { }
}
";
            string expectedOperationTree = @"
IInvocationExpression ( void P.M2(System.Int32 x, params System.Double[] array)) (OperationKind.InvocationExpression, Type: System.Void) (Syntax: 'M2(1)')
  Instance Receiver: IInstanceReferenceExpression (InstanceReferenceKind.Implicit) (OperationKind.InstanceReferenceExpression, Type: P) (Syntax: 'M2')
  Arguments(2): IArgument (ArgumentKind.Explicit, Matching Parameter: x) (OperationKind.Argument) (Syntax: '1')
      ILiteralExpression (Text: 1) (OperationKind.LiteralExpression, Type: System.Int32, Constant: 1) (Syntax: '1')
    IArgument (ArgumentKind.ParamArray, Matching Parameter: array) (OperationKind.Argument) (Syntax: 'M2(1)')
      IArrayCreationExpression (Element Type: System.Double) (OperationKind.ArrayCreationExpression, Type: System.Double[]) (Syntax: 'M2(1)')
        Dimension Sizes(1): ILiteralExpression (Text: 0) (OperationKind.LiteralExpression, Type: null, Constant: 0) (Syntax: 'M2(1)')
        Initializer: IArrayInitializer (OperationKind.ArrayInitializer) (Syntax: 'M2(1)')
";
            var expectedDiagnostics = DiagnosticDescription.None;

            VerifyOperationTreeAndDiagnosticsForTest<InvocationExpressionSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }

        [Fact(Skip = "https://github.com/dotnet/roslyn/issues/18549")]
        public void DefaultValueAndParamsArrayArgumentInExpandedFormWithNoArgument()
        {
            string source = @"
class P
{
    void M1()
    {
        var a = new[] { 0.0 };
        /*<bind>*/M2()/*</bind>*/;
    }

    void M2(int x = 0, params double[] array) { }
}
";
            string expectedOperationTree = @"
IInvocationExpression ( void P.M2([System.Int32 x = 0], params System.Double[] array)) (OperationKind.InvocationExpression, Type: System.Void) (Syntax: 'M2()')
  Instance Receiver: IInstanceReferenceExpression (InstanceReferenceKind.Implicit) (OperationKind.InstanceReferenceExpression, Type: P) (Syntax: 'M2')
  Arguments(2): IArgument (ArgumentKind.DefaultValue, Matching Parameter: x) (OperationKind.Argument) (Syntax: 'M2()')
      ILiteralExpression (Text: 0) (OperationKind.LiteralExpression, Type: System.Int32, Constant: 0) (Syntax: 'M2()')
    IArgument (ArgumentKind.ParamArray, Matching Parameter: array) (OperationKind.Argument) (Syntax: 'M2()')
      IArrayCreationExpression (Element Type: System.Double) (OperationKind.ArrayCreationExpression, Type: System.Double[]) (Syntax: 'M2()')
        Dimension Sizes(1): ILiteralExpression (Text: 0) (OperationKind.LiteralExpression, Type: null, Constant: 0) (Syntax: 'M2()')
        Initializer: IArrayInitializer (OperationKind.ArrayInitializer) (Syntax: 'M2()')
";
            var expectedDiagnostics = DiagnosticDescription.None;

            VerifyOperationTreeAndDiagnosticsForTest<InvocationExpressionSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }

        [Fact(Skip = "https://github.com/dotnet/roslyn/issues/18549")]
        public void DefaultValueAndNamedParamsArrayArgumentInNormalForm()
        {
            string source = @"
class P
{
    void M1()
    {
        var a = new[] { 0.0 };
        /*<bind>*/M2(array: a)/*</bind>*/;
    }

    void M2(int x = 0, params double[] array) { }
}
";
            string expectedOperationTree = @"
IInvocationExpression ( void P.M2([System.Int32 x = 0], params System.Double[] array)) (OperationKind.InvocationExpression, Type: System.Void) (Syntax: 'M2(array: a)')
  Instance Receiver: IInstanceReferenceExpression (InstanceReferenceKind.Implicit) (OperationKind.InstanceReferenceExpression, Type: P) (Syntax: 'M2')
  Arguments(2): IArgument (ArgumentKind.Explicit, Matching Parameter: array) (OperationKind.Argument) (Syntax: 'a')
      ILocalReferenceExpression: a (OperationKind.LocalReferenceExpression, Type: System.Double[]) (Syntax: 'a')
    IArgument (ArgumentKind.DefaultValue, Matching Parameter: x) (OperationKind.Argument) (Syntax: 'M2(array: a)')
      ILiteralExpression (Text: 0) (OperationKind.LiteralExpression, Type: System.Int32, Constant: 0) (Syntax: 'M2(array: a)')
";
            var expectedDiagnostics = DiagnosticDescription.None;

            VerifyOperationTreeAndDiagnosticsForTest<InvocationExpressionSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }

        [Fact(Skip = "https://github.com/dotnet/roslyn/issues/18549")]
        public void DefaultValueAndNamedParamsArrayArgumentInExpandedForm()
        {
            string source = @"
class P
{
    void M1()
    {
        /*<bind>*/M2(array: 1)/*</bind>*/;
    }

    void M2(int x = 0, params double[] array) { }
}
";
            string expectedOperationTree = @"
IInvocationExpression ( void P.M2([System.Int32 x = 0], params System.Double[] array)) (OperationKind.InvocationExpression, Type: System.Void) (Syntax: 'M2(array: 1)')
  Instance Receiver: IInstanceReferenceExpression (InstanceReferenceKind.Implicit) (OperationKind.InstanceReferenceExpression, Type: P) (Syntax: 'M2')
  Arguments(2): IArgument (ArgumentKind.ParamArray, Matching Parameter: array) (OperationKind.Argument) (Syntax: '1')
      IArrayCreationExpression (Element Type: System.Double) (OperationKind.ArrayCreationExpression, Type: System.Double[]) (Syntax: '1')
        Dimension Sizes(1): ILiteralExpression (Text: 1) (OperationKind.LiteralExpression, Type: null, Constant: 1) (Syntax: '1')
        Initializer: IArrayInitializer (OperationKind.ArrayInitializer) (Syntax: '1')
            Element Values(1): IConversionExpression (ConversionKind.CSharp, Implicit) (OperationKind.ConversionExpression, Type: System.Double, Constant: 1) (Syntax: '1')
                ILiteralExpression (Text: 1) (OperationKind.LiteralExpression, Type: System.Int32, Constant: 1) (Syntax: '1')
    IArgument (ArgumentKind.DefaultValue, Matching Parameter: x) (OperationKind.Argument) (Syntax: 'M2(array: 1)')
      ILiteralExpression (Text: 0) (OperationKind.LiteralExpression, Type: System.Int32, Constant: 0) (Syntax: 'M2(array: 1)')
";
            var expectedDiagnostics = DiagnosticDescription.None;

            VerifyOperationTreeAndDiagnosticsForTest<InvocationExpressionSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }

        [Fact]
        public void PositionalArgumentAndNamedParamsArrayArgumentInNormalForm()
        {
            string source = @"
class P
{
    void M1()
    {
        var a = new[] { 0.0 };
        /*<bind>*/M2(1, array: a)/*</bind>*/;
    }

    void M2(int x = 0, params double[] array) { }
}
";
            string expectedOperationTree = @"
IInvocationExpression ( void P.M2([System.Int32 x = 0], params System.Double[] array)) (OperationKind.InvocationExpression, Type: System.Void) (Syntax: 'M2(1, array: a)')
  Instance Receiver: IInstanceReferenceExpression (InstanceReferenceKind.Implicit) (OperationKind.InstanceReferenceExpression, Type: P) (Syntax: 'M2')
  Arguments(2): IArgument (ArgumentKind.Explicit, Matching Parameter: x) (OperationKind.Argument) (Syntax: '1')
      ILiteralExpression (Text: 1) (OperationKind.LiteralExpression, Type: System.Int32, Constant: 1) (Syntax: '1')
    IArgument (ArgumentKind.Explicit, Matching Parameter: array) (OperationKind.Argument) (Syntax: 'a')
      ILocalReferenceExpression: a (OperationKind.LocalReferenceExpression, Type: System.Double[]) (Syntax: 'a')
";
            var expectedDiagnostics = DiagnosticDescription.None;

            VerifyOperationTreeAndDiagnosticsForTest<InvocationExpressionSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }

        [Fact(Skip = "https://github.com/dotnet/roslyn/issues/18549")]
        public void PositionalArgumentAndNamedParamsArrayArgumentInExpandedForm()
        {
            string source = @"
class P
{
    void M1()
    {
        /*<bind>*/M2(1, array: 1)/*</bind>*/;
    }

    void M2(int x = 0, params double[] array) { }
}
";
            string expectedOperationTree = @"
IInvocationExpression ( void P.M2([System.Int32 x = 0], params System.Double[] array)) (OperationKind.InvocationExpression, Type: System.Void) (Syntax: 'M2(1, array: 1)')
  Instance Receiver: IInstanceReferenceExpression (InstanceReferenceKind.Implicit) (OperationKind.InstanceReferenceExpression, Type: P) (Syntax: 'M2')
  Arguments(2): IArgument (ArgumentKind.Explicit, Matching Parameter: x) (OperationKind.Argument) (Syntax: '1')
      ILiteralExpression (Text: 1) (OperationKind.LiteralExpression, Type: System.Int32, Constant: 1) (Syntax: '1')
    IArgument (ArgumentKind.ParamArray, Matching Parameter: array) (OperationKind.Argument) (Syntax: '1')
      IArrayCreationExpression (Element Type: System.Double) (OperationKind.ArrayCreationExpression, Type: System.Double[]) (Syntax: '1')
        Dimension Sizes(1): ILiteralExpression (Text: 1) (OperationKind.LiteralExpression, Type: null, Constant: 1) (Syntax: '1')
        Initializer: IArrayInitializer (OperationKind.ArrayInitializer) (Syntax: '1')
            Element Values(1): IConversionExpression (ConversionKind.CSharp, Implicit) (OperationKind.ConversionExpression, Type: System.Double, Constant: 1) (Syntax: '1')
                ILiteralExpression (Text: 1) (OperationKind.LiteralExpression, Type: System.Int32, Constant: 1) (Syntax: '1')
";
            var expectedDiagnostics = DiagnosticDescription.None;

            VerifyOperationTreeAndDiagnosticsForTest<InvocationExpressionSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }

        [Fact(Skip = "https://github.com/dotnet/roslyn/issues/18549")]
        public void NamedArgumentAndNamedParamsArrayArgumentInNormalFormOutOfParameterOrder()
        {
            string source = @"
class P
{
    void M1()
    {
        var a = new[] { 0.0 };
        /*<bind>*/M2(array: a, x: 1);/*</bind>*/
    }

    void M2(int x = 0, params double[] array) { }
}
";
            string expectedOperationTree = @"
IExpressionStatement (OperationKind.ExpressionStatement) (Syntax: 'M2(array: a, x: 1);')
  IInvocationExpression ( void P.M2([System.Int32 x = 0], params System.Double[] array)) (OperationKind.InvocationExpression, Type: System.Void) (Syntax: 'M2(array: a, x: 1)')
    Instance Receiver: IInstanceReferenceExpression (InstanceReferenceKind.Implicit) (OperationKind.InstanceReferenceExpression, Type: P) (Syntax: 'M2')
    Arguments(2): IArgument (ArgumentKind.Explicit, Matching Parameter: array) (OperationKind.Argument) (Syntax: 'a')
        ILocalReferenceExpression: a (OperationKind.LocalReferenceExpression, Type: System.Double[]) (Syntax: 'a')
      IArgument (ArgumentKind.Explicit, Matching Parameter: x) (OperationKind.Argument) (Syntax: '1')
        ILiteralExpression (Text: 1) (OperationKind.LiteralExpression, Type: System.Int32, Constant: 1) (Syntax: '1')
";
            var expectedDiagnostics = DiagnosticDescription.None;

            VerifyOperationTreeAndDiagnosticsForTest<ExpressionStatementSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }

        [Fact(Skip = "https://github.com/dotnet/roslyn/issues/18549")]
        public void NamedArgumentAndNamedParamsArrayArgumentInExpandedFormOutOfParameterOrder()
        {
            string source = @"
class P
{
    void M1()
    {
        /*<bind>*/M2(array: 1, x: 10)/*</bind>*/;
    }

    void M2(int x = 0, params double[] array) { }
}
";
            string expectedOperationTree = @"
IInvocationExpression ( void P.M2([System.Int32 x = 0], params System.Double[] array)) (OperationKind.InvocationExpression, Type: System.Void) (Syntax: 'M2(array: 1, x: 10)')
  Instance Receiver: IInstanceReferenceExpression (InstanceReferenceKind.Implicit) (OperationKind.InstanceReferenceExpression, Type: P) (Syntax: 'M2')
  Arguments(2): IArgument (ArgumentKind.ParamArray, Matching Parameter: array) (OperationKind.Argument) (Syntax: '1')
      IArrayCreationExpression (Element Type: System.Double) (OperationKind.ArrayCreationExpression, Type: System.Double[]) (Syntax: '1')
        Dimension Sizes(1): ILiteralExpression (Text: 1) (OperationKind.LiteralExpression, Type: null, Constant: 1) (Syntax: '1')
        Initializer: IArrayInitializer (OperationKind.ArrayInitializer) (Syntax: '1')
            Element Values(1): IConversionExpression (ConversionKind.CSharp, Implicit) (OperationKind.ConversionExpression, Type: System.Double, Constant: 1) (Syntax: '1')
                ILiteralExpression (Text: 1) (OperationKind.LiteralExpression, Type: System.Int32, Constant: 1) (Syntax: '1')
    IArgument (ArgumentKind.Explicit, Matching Parameter: x) (OperationKind.Argument) (Syntax: '10')
      ILiteralExpression (Text: 10) (OperationKind.LiteralExpression, Type: System.Int32, Constant: 10) (Syntax: '10')
";
            var expectedDiagnostics = DiagnosticDescription.None;

            VerifyOperationTreeAndDiagnosticsForTest<InvocationExpressionSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }
    }
}