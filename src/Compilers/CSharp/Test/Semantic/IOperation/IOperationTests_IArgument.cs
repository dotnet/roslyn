﻿// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Semantics;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.CSharp.UnitTests
{
    public partial class IOperationTests : SemanticModelTestBase
    {
        [Fact]
        public void NoArgument()
        {
            string source = @"
class P
{
    static void M1()
    {
        /*<bind>*/M2()/*</bind>*/;
    }
    static void M2() { }
}
";
            string expectedOperationTree = @"
IInvocationExpression (void P.M2()) (OperationKind.InvocationExpression, Type: System.Void) (Syntax: 'M2()')
  Instance Receiver: null
  Arguments(0)
";
            var expectedDiagnostics = DiagnosticDescription.None;

            VerifyOperationTreeAndDiagnosticsForTest<InvocationExpressionSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }

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
IInvocationExpression (void P.M2(System.Int32 x, System.Double y)) (OperationKind.InvocationExpression, Type: System.Void) (Syntax: 'M2(1, 2.0)')
  Instance Receiver: null
  Arguments(2):
      IArgument (ArgumentKind.Explicit, Matching Parameter: x) (OperationKind.Argument) (Syntax: '1')
        ILiteralExpression (Text: 1) (OperationKind.LiteralExpression, Type: System.Int32, Constant: 1) (Syntax: '1')
        InConversion: null
        OutConversion: null
      IArgument (ArgumentKind.Explicit, Matching Parameter: y) (OperationKind.Argument) (Syntax: '2.0')
        ILiteralExpression (OperationKind.LiteralExpression, Type: System.Double, Constant: 2) (Syntax: '2.0')
        InConversion: null
        OutConversion: null
";
            var expectedDiagnostics = DiagnosticDescription.None;

            VerifyOperationTreeAndDiagnosticsForTest<InvocationExpressionSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }

        [Fact]
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
IInvocationExpression (void P.M2(System.Int32 x, [System.Double y = 0])) (OperationKind.InvocationExpression, Type: System.Void) (Syntax: 'M2(1)')
  Instance Receiver: null
  Arguments(2):
      IArgument (ArgumentKind.Explicit, Matching Parameter: x) (OperationKind.Argument) (Syntax: '1')
        ILiteralExpression (Text: 1) (OperationKind.LiteralExpression, Type: System.Int32, Constant: 1) (Syntax: '1')
        InConversion: null
        OutConversion: null
      IArgument (ArgumentKind.DefaultValue, Matching Parameter: y) (OperationKind.Argument) (Syntax: 'M2(1)')
        ILiteralExpression (OperationKind.LiteralExpression, Type: System.Double, Constant: 0) (Syntax: 'M2(1)')
        InConversion: null
        OutConversion: null
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
IInvocationExpression (void P.M2(System.Int32 x, [System.Double y = 0])) (OperationKind.InvocationExpression, Type: System.Void) (Syntax: 'M2(x: 1, y: 9.9)')
  Instance Receiver: null
  Arguments(2):
      IArgument (ArgumentKind.Explicit, Matching Parameter: x) (OperationKind.Argument) (Syntax: '1')
        ILiteralExpression (Text: 1) (OperationKind.LiteralExpression, Type: System.Int32, Constant: 1) (Syntax: '1')
        InConversion: null
        OutConversion: null
      IArgument (ArgumentKind.Explicit, Matching Parameter: y) (OperationKind.Argument) (Syntax: '9.9')
        ILiteralExpression (Text: 9.9) (OperationKind.LiteralExpression, Type: System.Double, Constant: 9.9) (Syntax: '9.9')
        InConversion: null
        OutConversion: null
";
            var expectedDiagnostics = DiagnosticDescription.None;

            VerifyOperationTreeAndDiagnosticsForTest<InvocationExpressionSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }

        [Fact]
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
IInvocationExpression (void P.M2(System.Int32 x, [System.Double y = 0])) (OperationKind.InvocationExpression, Type: System.Void) (Syntax: 'M2(y: 9.9, x: 1)')
  Instance Receiver: null
  Arguments(2):
      IArgument (ArgumentKind.Explicit, Matching Parameter: y) (OperationKind.Argument) (Syntax: '9.9')
        ILiteralExpression (Text: 9.9) (OperationKind.LiteralExpression, Type: System.Double, Constant: 9.9) (Syntax: '9.9')
        InConversion: null
        OutConversion: null
      IArgument (ArgumentKind.Explicit, Matching Parameter: x) (OperationKind.Argument) (Syntax: '1')
        ILiteralExpression (Text: 1) (OperationKind.LiteralExpression, Type: System.Int32, Constant: 1) (Syntax: '1')
        InConversion: null
        OutConversion: null
";
            var expectedDiagnostics = DiagnosticDescription.None;

            VerifyOperationTreeAndDiagnosticsForTest<InvocationExpressionSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }

        [Fact(Skip = "https://github.com/dotnet/roslyn/issues/21079")]
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
  Arguments(3):
IArgument (ArgumentKind.Explicit, Matching Parameter: y) (OperationKind.Argument) (Syntax: '0')
      ILiteralExpression (Text: 0) (OperationKind.LiteralExpression, Type: System.Int32, Constant: 0) (Syntax: '0')
    IArgument (ArgumentKind.Explicit, Matching Parameter: z) (OperationKind.Argument) (Syntax: '2')
      ILiteralExpression (Text: 2) (OperationKind.LiteralExpression, Type: System.Int32, Constant: 2) (Syntax: '2')
    IArgument (ArgumentKind.DefaultValue, Matching Parameter: x) (OperationKind.Argument) (Syntax: 'M2(y: 0, z: 2)')
      ILiteralExpression (Text: 1) (OperationKind.LiteralExpression, Type: System.Int32, Constant: 1) (Syntax: 'M2(y: 0, z: 2)')
";
            var expectedDiagnostics = DiagnosticDescription.None;

            VerifyOperationTreeAndDiagnosticsForTest<InvocationExpressionSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }

        [Fact]
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
IInvocationExpression (void P.M2([System.Int32 x = 1], [System.Int32 y = 2], [System.Int32 z = 3])) (OperationKind.InvocationExpression, Type: System.Void) (Syntax: 'M2(z: 2, x: 9)')
  Instance Receiver: null
  Arguments(3):
      IArgument (ArgumentKind.Explicit, Matching Parameter: z) (OperationKind.Argument) (Syntax: '2')
        ILiteralExpression (Text: 2) (OperationKind.LiteralExpression, Type: System.Int32, Constant: 2) (Syntax: '2')
        InConversion: null
        OutConversion: null
      IArgument (ArgumentKind.Explicit, Matching Parameter: x) (OperationKind.Argument) (Syntax: '9')
        ILiteralExpression (Text: 9) (OperationKind.LiteralExpression, Type: System.Int32, Constant: 9) (Syntax: '9')
        InConversion: null
        OutConversion: null
      IArgument (ArgumentKind.DefaultValue, Matching Parameter: y) (OperationKind.Argument) (Syntax: 'M2(z: 2, x: 9)')
        ILiteralExpression (OperationKind.LiteralExpression, Type: System.Int32, Constant: 2) (Syntax: 'M2(z: 2, x: 9)')
        InConversion: null
        OutConversion: null
";
            var expectedDiagnostics = DiagnosticDescription.None;

            VerifyOperationTreeAndDiagnosticsForTest<InvocationExpressionSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }

        [Fact]
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
  Expression: IInvocationExpression (void P.M2([System.Int32 x = 1], [System.Int32 y = 2], [System.Int32 z = 3])) (OperationKind.InvocationExpression, Type: System.Void) (Syntax: 'M2(9, z: 10)')
      Instance Receiver: null
      Arguments(3):
          IArgument (ArgumentKind.Explicit, Matching Parameter: x) (OperationKind.Argument) (Syntax: '9')
            ILiteralExpression (Text: 9) (OperationKind.LiteralExpression, Type: System.Int32, Constant: 9) (Syntax: '9')
            InConversion: null
            OutConversion: null
          IArgument (ArgumentKind.Explicit, Matching Parameter: z) (OperationKind.Argument) (Syntax: '10')
            ILiteralExpression (Text: 10) (OperationKind.LiteralExpression, Type: System.Int32, Constant: 10) (Syntax: '10')
            InConversion: null
            OutConversion: null
          IArgument (ArgumentKind.DefaultValue, Matching Parameter: y) (OperationKind.Argument) (Syntax: 'M2(9, z: 10)')
            ILiteralExpression (OperationKind.LiteralExpression, Type: System.Int32, Constant: 2) (Syntax: 'M2(9, z: 10)')
            InConversion: null
            OutConversion: null
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
  Arguments(2):
      IArgument (ArgumentKind.Explicit, Matching Parameter: x) (OperationKind.Argument) (Syntax: 'a')
        ILocalReferenceExpression: a (OperationKind.LocalReferenceExpression, Type: System.Int32) (Syntax: 'a')
        InConversion: null
        OutConversion: null
      IArgument (ArgumentKind.Explicit, Matching Parameter: y) (OperationKind.Argument) (Syntax: 'b')
        ILocalReferenceExpression: b (OperationKind.LocalReferenceExpression, Type: System.Int32) (Syntax: 'b')
        InConversion: null
        OutConversion: null
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
  Arguments(2):
      IArgument (ArgumentKind.Explicit, Matching Parameter: x) (OperationKind.Argument) (Syntax: 'a')
        ILocalReferenceExpression: a (OperationKind.LocalReferenceExpression, Type: System.Int32) (Syntax: 'a')
        InConversion: null
        OutConversion: null
      IArgument (ArgumentKind.Explicit, Matching Parameter: y) (OperationKind.Argument) (Syntax: 'b')
        ILocalReferenceExpression: b (OperationKind.LocalReferenceExpression, Type: System.Int32) (Syntax: 'b')
        InConversion: null
        OutConversion: null";
            var expectedDiagnostics = DiagnosticDescription.None;

            VerifyOperationTreeAndDiagnosticsForTest<InvocationExpressionSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }

        [Fact]
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
            string expectedOperationTree = @"
IInvocationExpression ( void P.M2(ref System.Int32 x, out System.Int32 y)) (OperationKind.InvocationExpression, Type: System.Void) (Syntax: 'M2(y: out b, x: ref a)')
  Instance Receiver: IInstanceReferenceExpression (InstanceReferenceKind.Implicit) (OperationKind.InstanceReferenceExpression, Type: P) (Syntax: 'M2')
  Arguments(2):
      IArgument (ArgumentKind.Explicit, Matching Parameter: y) (OperationKind.Argument) (Syntax: 'b')
        ILocalReferenceExpression: b (OperationKind.LocalReferenceExpression, Type: System.Int32) (Syntax: 'b')
        InConversion: null
        OutConversion: null
      IArgument (ArgumentKind.Explicit, Matching Parameter: x) (OperationKind.Argument) (Syntax: 'a')
        ILocalReferenceExpression: a (OperationKind.LocalReferenceExpression, Type: System.Int32) (Syntax: 'a')
        InConversion: null
        OutConversion: null
";
            var expectedDiagnostics = DiagnosticDescription.None;

            VerifyOperationTreeAndDiagnosticsForTest<InvocationExpressionSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }

        [Fact]
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
            string expectedOperationTree = @"
IInvocationExpression ( void P.M2([S sobj = default(S)])) (OperationKind.InvocationExpression, Type: System.Void) (Syntax: 'M2()')
  Instance Receiver: IInstanceReferenceExpression (InstanceReferenceKind.Implicit) (OperationKind.InstanceReferenceExpression, Type: P) (Syntax: 'M2')
  Arguments(1):
      IArgument (ArgumentKind.DefaultValue, Matching Parameter: sobj) (OperationKind.Argument) (Syntax: 'M2()')
        IDefaultValueExpression (OperationKind.DefaultValueExpression, Type: S) (Syntax: 'M2()')
        InConversion: null
        OutConversion: null";
            var expectedDiagnostics = DiagnosticDescription.None;

            VerifyOperationTreeAndDiagnosticsForTest<InvocationExpressionSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }

        [Fact]
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
            string expectedOperationTree = @"
IInvocationExpression ( void P.M2([S sobj = default(S)])) (OperationKind.InvocationExpression, Type: System.Void) (Syntax: 'M2()')
  Instance Receiver: IInstanceReferenceExpression (InstanceReferenceKind.Implicit) (OperationKind.InstanceReferenceExpression, Type: P) (Syntax: 'M2')
  Arguments(1):
      IArgument (ArgumentKind.DefaultValue, Matching Parameter: sobj) (OperationKind.Argument) (Syntax: 'M2()')
        IDefaultValueExpression (OperationKind.DefaultValueExpression, Type: S) (Syntax: 'M2()')
        InConversion: null
        OutConversion: null";
            var expectedDiagnostics = DiagnosticDescription.None;

            VerifyOperationTreeAndDiagnosticsForTest<InvocationExpressionSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }

        [Fact]
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
  Arguments(1):
      IArgument (ArgumentKind.DefaultValue, Matching Parameter: s) (OperationKind.Argument) (Syntax: 'M2()')
        ILiteralExpression (OperationKind.LiteralExpression, Type: System.Double, Constant: 3.14) (Syntax: 'M2()')
        InConversion: null
        OutConversion: null
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
IInvocationExpression (void Extensions.E1(this P p, [System.Int32 x = 0], [System.Int32 y = 0])) (OperationKind.InvocationExpression, Type: System.Void) (Syntax: 'this.E1(1, 2)')
  Instance Receiver: null
  Arguments(3):
      IArgument (ArgumentKind.Explicit, Matching Parameter: p) (OperationKind.Argument) (Syntax: 'this')
        IInstanceReferenceExpression (InstanceReferenceKind.Explicit) (OperationKind.InstanceReferenceExpression, Type: P) (Syntax: 'this')
        InConversion: null
        OutConversion: null
      IArgument (ArgumentKind.Explicit, Matching Parameter: x) (OperationKind.Argument) (Syntax: '1')
        ILiteralExpression (Text: 1) (OperationKind.LiteralExpression, Type: System.Int32, Constant: 1) (Syntax: '1')
        InConversion: null
        OutConversion: null
      IArgument (ArgumentKind.Explicit, Matching Parameter: y) (OperationKind.Argument) (Syntax: '2')
        ILiteralExpression (Text: 2) (OperationKind.LiteralExpression, Type: System.Int32, Constant: 2) (Syntax: '2')
        InConversion: null
        OutConversion: null
";
            var expectedDiagnostics = DiagnosticDescription.None;

            VerifyOperationTreeAndDiagnosticsForTest<InvocationExpressionSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }

        [Fact]
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
  Expression: IInvocationExpression (void Extensions.E1(this P p, [System.Int32 x = 0], [System.Int32 y = 0])) (OperationKind.InvocationExpression, Type: System.Void) (Syntax: 'this.E1(y: 1, x: 2)')
      Instance Receiver: null
      Arguments(3):
          IArgument (ArgumentKind.Explicit, Matching Parameter: p) (OperationKind.Argument) (Syntax: 'this')
            IInstanceReferenceExpression (InstanceReferenceKind.Explicit) (OperationKind.InstanceReferenceExpression, Type: P) (Syntax: 'this')
            InConversion: null
            OutConversion: null
          IArgument (ArgumentKind.Explicit, Matching Parameter: y) (OperationKind.Argument) (Syntax: '1')
            ILiteralExpression (Text: 1) (OperationKind.LiteralExpression, Type: System.Int32, Constant: 1) (Syntax: '1')
            InConversion: null
            OutConversion: null
          IArgument (ArgumentKind.Explicit, Matching Parameter: x) (OperationKind.Argument) (Syntax: '2')
            ILiteralExpression (Text: 2) (OperationKind.LiteralExpression, Type: System.Int32, Constant: 2) (Syntax: '2')
            InConversion: null
            OutConversion: null
";
            var expectedDiagnostics = DiagnosticDescription.None;

            VerifyOperationTreeAndDiagnosticsForTest<ExpressionStatementSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }

        [Fact]
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
IInvocationExpression (void Extensions.E1(this P p, [System.Int32 x = 0], [System.Int32 y = 0])) (OperationKind.InvocationExpression, Type: System.Void) (Syntax: 'this.E1(y: 1)')
  Instance Receiver: null
  Arguments(3):
      IArgument (ArgumentKind.Explicit, Matching Parameter: p) (OperationKind.Argument) (Syntax: 'this')
        IInstanceReferenceExpression (InstanceReferenceKind.Explicit) (OperationKind.InstanceReferenceExpression, Type: P) (Syntax: 'this')
        InConversion: null
        OutConversion: null
      IArgument (ArgumentKind.Explicit, Matching Parameter: y) (OperationKind.Argument) (Syntax: '1')
        ILiteralExpression (Text: 1) (OperationKind.LiteralExpression, Type: System.Int32, Constant: 1) (Syntax: '1')
        InConversion: null
        OutConversion: null
      IArgument (ArgumentKind.DefaultValue, Matching Parameter: x) (OperationKind.Argument) (Syntax: 'this.E1(y: 1)')
        ILiteralExpression (OperationKind.LiteralExpression, Type: System.Int32, Constant: 0) (Syntax: 'this.E1(y: 1)')
        InConversion: null
        OutConversion: null
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
  Arguments(2):
      IArgument (ArgumentKind.Explicit, Matching Parameter: x) (OperationKind.Argument) (Syntax: '1')
        ILiteralExpression (Text: 1) (OperationKind.LiteralExpression, Type: System.Int32, Constant: 1) (Syntax: '1')
        InConversion: null
        OutConversion: null
      IArgument (ArgumentKind.Explicit, Matching Parameter: array) (OperationKind.Argument) (Syntax: 'a')
        ILocalReferenceExpression: a (OperationKind.LocalReferenceExpression, Type: System.Double[]) (Syntax: 'a')
        InConversion: null
        OutConversion: null
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
  Arguments(2):
      IArgument (ArgumentKind.Explicit, Matching Parameter: x) (OperationKind.Argument) (Syntax: '1')
        ILiteralExpression (Text: 1) (OperationKind.LiteralExpression, Type: System.Int32, Constant: 1) (Syntax: '1')
        InConversion: null
        OutConversion: null
      IArgument (ArgumentKind.ParamArray, Matching Parameter: array) (OperationKind.Argument) (Syntax: 'M2(1, 0.1, 0.2)')
        IArrayCreationExpression (Element Type: System.Double) (OperationKind.ArrayCreationExpression, Type: System.Double[]) (Syntax: 'M2(1, 0.1, 0.2)')
          Dimension Sizes(1):
              ILiteralExpression (OperationKind.LiteralExpression, Type: System.Int32, Constant: 2) (Syntax: 'M2(1, 0.1, 0.2)')
          Initializer: IArrayInitializer (2 elements) (OperationKind.ArrayInitializer) (Syntax: 'M2(1, 0.1, 0.2)')
              Element Values(2):
                  ILiteralExpression (Text: 0.1) (OperationKind.LiteralExpression, Type: System.Double, Constant: 0.1) (Syntax: '0.1')
                  ILiteralExpression (Text: 0.2) (OperationKind.LiteralExpression, Type: System.Double, Constant: 0.2) (Syntax: '0.2')
        InConversion: null
        OutConversion: null
";
            var expectedDiagnostics = DiagnosticDescription.None;

            VerifyOperationTreeAndDiagnosticsForTest<InvocationExpressionSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }

        [Fact]
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
  Arguments(2):
      IArgument (ArgumentKind.Explicit, Matching Parameter: x) (OperationKind.Argument) (Syntax: '1')
        ILiteralExpression (Text: 1) (OperationKind.LiteralExpression, Type: System.Int32, Constant: 1) (Syntax: '1')
        InConversion: null
        OutConversion: null
      IArgument (ArgumentKind.ParamArray, Matching Parameter: array) (OperationKind.Argument) (Syntax: 'M2(1)')
        IArrayCreationExpression (Element Type: System.Double) (OperationKind.ArrayCreationExpression, Type: System.Double[]) (Syntax: 'M2(1)')
          Dimension Sizes(1):
              ILiteralExpression (OperationKind.LiteralExpression, Type: System.Int32, Constant: 0) (Syntax: 'M2(1)')
          Initializer: IArrayInitializer (0 elements) (OperationKind.ArrayInitializer) (Syntax: 'M2(1)')
              Element Values(0)
        InConversion: null
        OutConversion: null
";
            var expectedDiagnostics = DiagnosticDescription.None;

            VerifyOperationTreeAndDiagnosticsForTest<InvocationExpressionSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }

        [Fact]
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
  Arguments(2):
      IArgument (ArgumentKind.DefaultValue, Matching Parameter: x) (OperationKind.Argument) (Syntax: 'M2()')
        ILiteralExpression (OperationKind.LiteralExpression, Type: System.Int32, Constant: 0) (Syntax: 'M2()')
        InConversion: null
        OutConversion: null
      IArgument (ArgumentKind.ParamArray, Matching Parameter: array) (OperationKind.Argument) (Syntax: 'M2()')
        IArrayCreationExpression (Element Type: System.Double) (OperationKind.ArrayCreationExpression, Type: System.Double[]) (Syntax: 'M2()')
          Dimension Sizes(1):
              ILiteralExpression (OperationKind.LiteralExpression, Type: System.Int32, Constant: 0) (Syntax: 'M2()')
          Initializer: IArrayInitializer (0 elements) (OperationKind.ArrayInitializer) (Syntax: 'M2()')
              Element Values(0)
        InConversion: null
        OutConversion: null
";
            var expectedDiagnostics = DiagnosticDescription.None;

            VerifyOperationTreeAndDiagnosticsForTest<InvocationExpressionSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }

        [Fact]
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
  Arguments(2):
      IArgument (ArgumentKind.Explicit, Matching Parameter: array) (OperationKind.Argument) (Syntax: 'a')
        ILocalReferenceExpression: a (OperationKind.LocalReferenceExpression, Type: System.Double[]) (Syntax: 'a')
        InConversion: null
        OutConversion: null
      IArgument (ArgumentKind.DefaultValue, Matching Parameter: x) (OperationKind.Argument) (Syntax: 'M2(array: a)')
        ILiteralExpression (OperationKind.LiteralExpression, Type: System.Int32, Constant: 0) (Syntax: 'M2(array: a)')
        InConversion: null
        OutConversion: null
";
            var expectedDiagnostics = DiagnosticDescription.None;

            VerifyOperationTreeAndDiagnosticsForTest<InvocationExpressionSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }

        [Fact]
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
  Arguments(2):
      IArgument (ArgumentKind.ParamArray, Matching Parameter: array) (OperationKind.Argument) (Syntax: 'M2(array: 1)')
        IArrayCreationExpression (Element Type: System.Double) (OperationKind.ArrayCreationExpression, Type: System.Double[]) (Syntax: 'M2(array: 1)')
          Dimension Sizes(1):
              ILiteralExpression (OperationKind.LiteralExpression, Type: System.Int32, Constant: 1) (Syntax: 'M2(array: 1)')
          Initializer: IArrayInitializer (1 elements) (OperationKind.ArrayInitializer) (Syntax: 'M2(array: 1)')
              Element Values(1):
                  IConversionExpression (ConversionKind.CSharp, Implicit) (OperationKind.ConversionExpression, Type: System.Double, Constant: 1) (Syntax: '1')
                    Operand: ILiteralExpression (Text: 1) (OperationKind.LiteralExpression, Type: System.Int32, Constant: 1) (Syntax: '1')
        InConversion: null
        OutConversion: null
      IArgument (ArgumentKind.DefaultValue, Matching Parameter: x) (OperationKind.Argument) (Syntax: 'M2(array: 1)')
        ILiteralExpression (OperationKind.LiteralExpression, Type: System.Int32, Constant: 0) (Syntax: 'M2(array: 1)')
        InConversion: null
        OutConversion: null
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
  Arguments(2):
      IArgument (ArgumentKind.Explicit, Matching Parameter: x) (OperationKind.Argument) (Syntax: '1')
        ILiteralExpression (Text: 1) (OperationKind.LiteralExpression, Type: System.Int32, Constant: 1) (Syntax: '1')
        InConversion: null
        OutConversion: null
      IArgument (ArgumentKind.Explicit, Matching Parameter: array) (OperationKind.Argument) (Syntax: 'a')
        ILocalReferenceExpression: a (OperationKind.LocalReferenceExpression, Type: System.Double[]) (Syntax: 'a')
        InConversion: null
        OutConversion: null
";
            var expectedDiagnostics = DiagnosticDescription.None;

            VerifyOperationTreeAndDiagnosticsForTest<InvocationExpressionSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }

        [Fact]
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
  Arguments(2):
      IArgument (ArgumentKind.Explicit, Matching Parameter: x) (OperationKind.Argument) (Syntax: '1')
        ILiteralExpression (Text: 1) (OperationKind.LiteralExpression, Type: System.Int32, Constant: 1) (Syntax: '1')
        InConversion: null
        OutConversion: null
      IArgument (ArgumentKind.ParamArray, Matching Parameter: array) (OperationKind.Argument) (Syntax: 'M2(1, array: 1)')
        IArrayCreationExpression (Element Type: System.Double) (OperationKind.ArrayCreationExpression, Type: System.Double[]) (Syntax: 'M2(1, array: 1)')
          Dimension Sizes(1):
              ILiteralExpression (OperationKind.LiteralExpression, Type: System.Int32, Constant: 1) (Syntax: 'M2(1, array: 1)')
          Initializer: IArrayInitializer (1 elements) (OperationKind.ArrayInitializer) (Syntax: 'M2(1, array: 1)')
              Element Values(1):
                  IConversionExpression (ConversionKind.CSharp, Implicit) (OperationKind.ConversionExpression, Type: System.Double, Constant: 1) (Syntax: '1')
                    Operand: ILiteralExpression (Text: 1) (OperationKind.LiteralExpression, Type: System.Int32, Constant: 1) (Syntax: '1')
        InConversion: null
        OutConversion: null
";
            var expectedDiagnostics = DiagnosticDescription.None;

            VerifyOperationTreeAndDiagnosticsForTest<InvocationExpressionSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }

        [Fact]
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
  Expression: IInvocationExpression ( void P.M2([System.Int32 x = 0], params System.Double[] array)) (OperationKind.InvocationExpression, Type: System.Void) (Syntax: 'M2(array: a, x: 1)')
      Instance Receiver: IInstanceReferenceExpression (InstanceReferenceKind.Implicit) (OperationKind.InstanceReferenceExpression, Type: P) (Syntax: 'M2')
      Arguments(2):
          IArgument (ArgumentKind.Explicit, Matching Parameter: array) (OperationKind.Argument) (Syntax: 'a')
            ILocalReferenceExpression: a (OperationKind.LocalReferenceExpression, Type: System.Double[]) (Syntax: 'a')
            InConversion: null
            OutConversion: null
          IArgument (ArgumentKind.Explicit, Matching Parameter: x) (OperationKind.Argument) (Syntax: '1')
            ILiteralExpression (Text: 1) (OperationKind.LiteralExpression, Type: System.Int32, Constant: 1) (Syntax: '1')
            InConversion: null
            OutConversion: null
";
            var expectedDiagnostics = DiagnosticDescription.None;

            VerifyOperationTreeAndDiagnosticsForTest<ExpressionStatementSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }

        [Fact]
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
  Arguments(2):
      IArgument (ArgumentKind.ParamArray, Matching Parameter: array) (OperationKind.Argument) (Syntax: 'M2(array: 1, x: 10)')
        IArrayCreationExpression (Element Type: System.Double) (OperationKind.ArrayCreationExpression, Type: System.Double[]) (Syntax: 'M2(array: 1, x: 10)')
          Dimension Sizes(1):
              ILiteralExpression (OperationKind.LiteralExpression, Type: System.Int32, Constant: 1) (Syntax: 'M2(array: 1, x: 10)')
          Initializer: IArrayInitializer (1 elements) (OperationKind.ArrayInitializer) (Syntax: 'M2(array: 1, x: 10)')
              Element Values(1):
                  IConversionExpression (ConversionKind.CSharp, Implicit) (OperationKind.ConversionExpression, Type: System.Double, Constant: 1) (Syntax: '1')
                    Operand: ILiteralExpression (Text: 1) (OperationKind.LiteralExpression, Type: System.Int32, Constant: 1) (Syntax: '1')
        InConversion: null
        OutConversion: null
      IArgument (ArgumentKind.Explicit, Matching Parameter: x) (OperationKind.Argument) (Syntax: '10')
        ILiteralExpression (Text: 10) (OperationKind.LiteralExpression, Type: System.Int32, Constant: 10) (Syntax: '10')
        InConversion: null
        OutConversion: null
";
            var expectedDiagnostics = DiagnosticDescription.None;

            VerifyOperationTreeAndDiagnosticsForTest<InvocationExpressionSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }

        [Fact]
        public void CallerInfoAttributesInvokedInMethod()
        {
            string source = @"
using System.Runtime.CompilerServices;

class P
{
    void M1()
    {
        /*<bind>*/M2()/*</bind>*/;
    }

    void M2(
        [CallerMemberName] string memberName = null,
        [CallerFilePath] string sourceFilePath = null,
        [CallerLineNumber] int sourceLineNumber = 0)
    { }
}
";
            string expectedOperationTree = @"
IInvocationExpression ( void P.M2([System.String memberName = null], [System.String sourceFilePath = null], [System.Int32 sourceLineNumber = 0])) (OperationKind.InvocationExpression, Type: System.Void) (Syntax: 'M2()')
  Instance Receiver: IInstanceReferenceExpression (InstanceReferenceKind.Implicit) (OperationKind.InstanceReferenceExpression, Type: P) (Syntax: 'M2')
  Arguments(3):
      IArgument (ArgumentKind.DefaultValue, Matching Parameter: memberName) (OperationKind.Argument) (Syntax: 'M2()')
        ILiteralExpression (OperationKind.LiteralExpression, Type: System.String, Constant: ""M1"") (Syntax: 'M2()')
        InConversion: null
        OutConversion: null
      IArgument (ArgumentKind.DefaultValue, Matching Parameter: sourceFilePath) (OperationKind.Argument) (Syntax: 'M2()')
        ILiteralExpression (OperationKind.LiteralExpression, Type: System.String, Constant: ""file.cs"") (Syntax: 'M2()')
        InConversion: null
        OutConversion: null
      IArgument (ArgumentKind.DefaultValue, Matching Parameter: sourceLineNumber) (OperationKind.Argument) (Syntax: 'M2()')
        ILiteralExpression (OperationKind.LiteralExpression, Type: System.Int32, Constant: 8) (Syntax: 'M2()')
        InConversion: null
        OutConversion: null
";
            var expectedDiagnostics = DiagnosticDescription.None;

            VerifyOperationTreeAndDiagnosticsForTest<InvocationExpressionSyntax>(source, expectedOperationTree, expectedDiagnostics, additionalReferences: new[] { MscorlibRef_v46 });
        }

        [Fact]
        public void CallerInfoAttributesInvokedInProperty()
        {
            string source = @"
using System.Runtime.CompilerServices;

class P
{
    bool M1 => /*<bind>*/M2()/*</bind>*/;

    bool M2(
        [CallerMemberName] string memberName = null,
        [CallerFilePath] string sourceFilePath = null,
        [CallerLineNumber] int sourceLineNumber = 0)
    { 
        return true;
    }
}
";
            string expectedOperationTree = @"
IInvocationExpression ( System.Boolean P.M2([System.String memberName = null], [System.String sourceFilePath = null], [System.Int32 sourceLineNumber = 0])) (OperationKind.InvocationExpression, Type: System.Boolean) (Syntax: 'M2()')
  Instance Receiver: IInstanceReferenceExpression (InstanceReferenceKind.Implicit) (OperationKind.InstanceReferenceExpression, Type: P) (Syntax: 'M2')
  Arguments(3):
      IArgument (ArgumentKind.DefaultValue, Matching Parameter: memberName) (OperationKind.Argument) (Syntax: 'M2()')
        ILiteralExpression (OperationKind.LiteralExpression, Type: System.String, Constant: ""M1"") (Syntax: 'M2()')
        InConversion: null
        OutConversion: null
      IArgument (ArgumentKind.DefaultValue, Matching Parameter: sourceFilePath) (OperationKind.Argument) (Syntax: 'M2()')
        ILiteralExpression (OperationKind.LiteralExpression, Type: System.String, Constant: ""file.cs"") (Syntax: 'M2()')
        InConversion: null
        OutConversion: null
      IArgument (ArgumentKind.DefaultValue, Matching Parameter: sourceLineNumber) (OperationKind.Argument) (Syntax: 'M2()')
        ILiteralExpression (OperationKind.LiteralExpression, Type: System.Int32, Constant: 6) (Syntax: 'M2()')
        InConversion: null
        OutConversion: null
";
            var expectedDiagnostics = DiagnosticDescription.None;

            VerifyOperationTreeAndDiagnosticsForTest<InvocationExpressionSyntax>(source, expectedOperationTree, expectedDiagnostics, additionalReferences: new[] { MscorlibRef_v46 });
        }

        [Fact]
        public void CallerInfoAttributesInvokedInFieldInitializer()
        {
            string source = @"
using System.Runtime.CompilerServices;

class P
{
    bool field = /*<bind>*/M2()/*</bind>*/;

    static bool M2(
        [CallerMemberName] string memberName = null,
        [CallerFilePath] string sourceFilePath = null,
        [CallerLineNumber] int sourceLineNumber = 0)
    {
        return true;
    }
}
";
            string expectedOperationTree = @"
IInvocationExpression (System.Boolean P.M2([System.String memberName = null], [System.String sourceFilePath = null], [System.Int32 sourceLineNumber = 0])) (OperationKind.InvocationExpression, Type: System.Boolean) (Syntax: 'M2()')
  Instance Receiver: null
  Arguments(3):
      IArgument (ArgumentKind.DefaultValue, Matching Parameter: memberName) (OperationKind.Argument) (Syntax: 'M2()')
        ILiteralExpression (OperationKind.LiteralExpression, Type: System.String, Constant: ""field"") (Syntax: 'M2()')
        InConversion: null
        OutConversion: null
      IArgument (ArgumentKind.DefaultValue, Matching Parameter: sourceFilePath) (OperationKind.Argument) (Syntax: 'M2()')
        ILiteralExpression (OperationKind.LiteralExpression, Type: System.String, Constant: ""file.cs"") (Syntax: 'M2()')
        InConversion: null
        OutConversion: null
      IArgument (ArgumentKind.DefaultValue, Matching Parameter: sourceLineNumber) (OperationKind.Argument) (Syntax: 'M2()')
        ILiteralExpression (OperationKind.LiteralExpression, Type: System.Int32, Constant: 6) (Syntax: 'M2()')
        InConversion: null
        OutConversion: null
";
            var expectedDiagnostics = DiagnosticDescription.None;

            VerifyOperationTreeAndDiagnosticsForTest<InvocationExpressionSyntax>(source, expectedOperationTree, expectedDiagnostics, additionalReferences: new[] { MscorlibRef_v46 });
        }

        [Fact]
        public void CallerInfoAttributesInvokedInEventMethods()
        {
            string source = @"
using System;
using System.Runtime.CompilerServices;

class P
{
    public event EventHandler MyEvent
    {
        add
        {
            /*<bind>*/M2()/*</bind>*/;
        }

        remove
        {
            M2();
        }
    }

    static bool M2(
        [CallerMemberName] string memberName = null,
        [CallerFilePath] string sourceFilePath = null,
        [CallerLineNumber] int sourceLineNumber = 0)
    {
        return true;
    }
}
";
            string expectedOperationTree = @"
IInvocationExpression (System.Boolean P.M2([System.String memberName = null], [System.String sourceFilePath = null], [System.Int32 sourceLineNumber = 0])) (OperationKind.InvocationExpression, Type: System.Boolean) (Syntax: 'M2()')
  Instance Receiver: null
  Arguments(3):
      IArgument (ArgumentKind.DefaultValue, Matching Parameter: memberName) (OperationKind.Argument) (Syntax: 'M2()')
        ILiteralExpression (OperationKind.LiteralExpression, Type: System.String, Constant: ""MyEvent"") (Syntax: 'M2()')
        InConversion: null
        OutConversion: null
      IArgument (ArgumentKind.DefaultValue, Matching Parameter: sourceFilePath) (OperationKind.Argument) (Syntax: 'M2()')
        ILiteralExpression (OperationKind.LiteralExpression, Type: System.String, Constant: ""file.cs"") (Syntax: 'M2()')
        InConversion: null
        OutConversion: null
      IArgument (ArgumentKind.DefaultValue, Matching Parameter: sourceLineNumber) (OperationKind.Argument) (Syntax: 'M2()')
        ILiteralExpression (OperationKind.LiteralExpression, Type: System.Int32, Constant: 11) (Syntax: 'M2()')
        InConversion: null
        OutConversion: null
";
            var expectedDiagnostics = DiagnosticDescription.None;

            VerifyOperationTreeAndDiagnosticsForTest<InvocationExpressionSyntax>(source, expectedOperationTree, expectedDiagnostics, additionalReferences: new[] { MscorlibRef_v46 });
        }

        [Fact]
        public void ExtraArgument()
        {
            string source = @"
class P
{
    void M1()
    {
        /*<bind>*/M2(1, 2)/*</bind>*/;
    }

    void M2(int x = 0)
    { }
}
";
string expectedOperationTree = @"
IInvocationExpression ( void P.M2([System.Int32 x = 0])) (OperationKind.InvocationExpression, Type: System.Void, IsInvalid) (Syntax: 'M2(1, 2)')
  Instance Receiver: IInstanceReferenceExpression (InstanceReferenceKind.Implicit) (OperationKind.InstanceReferenceExpression, Type: P, IsInvalid) (Syntax: 'M2')
  Arguments(2):
      IArgument (ArgumentKind.Explicit, Matching Parameter: null) (OperationKind.Argument) (Syntax: '1')
        ILiteralExpression (Text: 1) (OperationKind.LiteralExpression, Type: System.Int32, Constant: 1) (Syntax: '1')
        InConversion: null
        OutConversion: null
      IArgument (ArgumentKind.Explicit, Matching Parameter: null) (OperationKind.Argument) (Syntax: '2')
        ILiteralExpression (Text: 2) (OperationKind.LiteralExpression, Type: System.Int32, Constant: 2) (Syntax: '2')
        InConversion: null
        OutConversion: null
";
            var expectedDiagnostics = new DiagnosticDescription[] {
                // CS1501: No overload for method 'M2' takes 2 arguments
                //         /*<bind>*/M2(1, 2)/*</bind>*/;
                Diagnostic(ErrorCode.ERR_BadArgCount, "M2").WithArguments("M2", "2").WithLocation(6, 19)
            };

            VerifyOperationTreeAndDiagnosticsForTest<InvocationExpressionSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }

        [Fact]
        public void WrongArgumentType()
        {
            string source = @"
class P
{
    void M1()
    {
        /*<bind>*/M2(1)/*</bind>*/;
    }

    void M2(string x )
    { }
}
";
string expectedOperationTree = @"
IInvocationExpression ( void P.M2(System.String x)) (OperationKind.InvocationExpression, Type: System.Void, IsInvalid) (Syntax: 'M2(1)')
  Instance Receiver: IInstanceReferenceExpression (InstanceReferenceKind.Implicit) (OperationKind.InstanceReferenceExpression, Type: P) (Syntax: 'M2')
  Arguments(1):
      IArgument (ArgumentKind.Explicit, Matching Parameter: null) (OperationKind.Argument, IsInvalid) (Syntax: '1')
        ILiteralExpression (Text: 1) (OperationKind.LiteralExpression, Type: System.Int32, Constant: 1, IsInvalid) (Syntax: '1')
        InConversion: null
        OutConversion: null
";
            var expectedDiagnostics = new DiagnosticDescription[]
            {
                // file.cs(6,22): error CS1503: Argument 1: cannot convert from 'int' to 'string'
                //         /*<bind>*/M2(1)/*</bind>*/;
                Diagnostic(ErrorCode.ERR_BadArgType, "1").WithArguments("1", "int", "string").WithLocation(6, 22)
            };

            VerifyOperationTreeAndDiagnosticsForTest<InvocationExpressionSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }

        [Fact]
        public void VarArgsCall()
        {
            string source = @"
using System;

public class P
{
    void M()
    {
        /*<bind>*/Console.Write(""{0} {1} {2} {3} {4}"", 1, 2, 3, 4, __arglist(5))/*</bind>*/;
    }
}
";
string expectedOperationTree = @"
IInvocationExpression (void System.Console.Write(System.String format, System.Object arg0, System.Object arg1, System.Object arg2, System.Object arg3, __arglist)) (OperationKind.InvocationExpression, Type: System.Void) (Syntax: 'Console.Wri ... arglist(5))')
  Instance Receiver: null
  Arguments(6):
      IArgument (ArgumentKind.Explicit, Matching Parameter: format) (OperationKind.Argument) (Syntax: '""{0} {1} {2} {3} {4}""')
        ILiteralExpression (OperationKind.LiteralExpression, Type: System.String, Constant: ""{0} {1} {2} {3} {4}"") (Syntax: '""{0} {1} {2} {3} {4}""')
        InConversion: null
        OutConversion: null
      IArgument (ArgumentKind.Explicit, Matching Parameter: arg0) (OperationKind.Argument) (Syntax: '1')
        IConversionExpression (ConversionKind.Cast, Implicit) (OperationKind.ConversionExpression, Type: System.Object) (Syntax: '1')
          Operand: ILiteralExpression (Text: 1) (OperationKind.LiteralExpression, Type: System.Int32, Constant: 1) (Syntax: '1')
        InConversion: null
        OutConversion: null
      IArgument (ArgumentKind.Explicit, Matching Parameter: arg1) (OperationKind.Argument) (Syntax: '2')
        IConversionExpression (ConversionKind.Cast, Implicit) (OperationKind.ConversionExpression, Type: System.Object) (Syntax: '2')
          Operand: ILiteralExpression (Text: 2) (OperationKind.LiteralExpression, Type: System.Int32, Constant: 2) (Syntax: '2')
        InConversion: null
        OutConversion: null
      IArgument (ArgumentKind.Explicit, Matching Parameter: arg2) (OperationKind.Argument) (Syntax: '3')
        IConversionExpression (ConversionKind.Cast, Implicit) (OperationKind.ConversionExpression, Type: System.Object) (Syntax: '3')
          Operand: ILiteralExpression (Text: 3) (OperationKind.LiteralExpression, Type: System.Int32, Constant: 3) (Syntax: '3')
        InConversion: null
        OutConversion: null
      IArgument (ArgumentKind.Explicit, Matching Parameter: arg3) (OperationKind.Argument) (Syntax: '4')
        IConversionExpression (ConversionKind.Cast, Implicit) (OperationKind.ConversionExpression, Type: System.Object) (Syntax: '4')
          Operand: ILiteralExpression (Text: 4) (OperationKind.LiteralExpression, Type: System.Int32, Constant: 4) (Syntax: '4')
        InConversion: null
        OutConversion: null
      IArgument (ArgumentKind.Explicit, Matching Parameter: null) (OperationKind.Argument) (Syntax: '__arglist(5)')
        IOperation:  (OperationKind.None) (Syntax: '__arglist(5)')
          Children(1):
              ILiteralExpression (Text: 5) (OperationKind.LiteralExpression, Type: System.Int32, Constant: 5) (Syntax: '5')
        InConversion: null
        OutConversion: null
";
            var expectedDiagnostics = DiagnosticDescription.None;

            VerifyOperationTreeAndDiagnosticsForTest<InvocationExpressionSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }

        [Fact]
        public void InvalidConversionForDefaultArgument_InSource()
        {
            string source = @"
class P
{
    void M1()
    {
        /*<bind>*/M2()/*</bind>*/;
    }

    void M2(int x = ""string"")
    { }
}
";
string expectedOperationTree = @"
IInvocationExpression ( void P.M2([System.Int32 x = default(System.Int32)])) (OperationKind.InvocationExpression, Type: System.Void) (Syntax: 'M2()')
  Instance Receiver: IInstanceReferenceExpression (InstanceReferenceKind.Implicit) (OperationKind.InstanceReferenceExpression, Type: P) (Syntax: 'M2')
  Arguments(1):
      IArgument (ArgumentKind.DefaultValue, Matching Parameter: x) (OperationKind.Argument) (Syntax: 'M2()')
        ILiteralExpression (OperationKind.LiteralExpression, Type: System.Int32, Constant: null) (Syntax: 'M2()')
        InConversion: null
        OutConversion: null
";
            var expectedDiagnostics = new DiagnosticDescription[] {
                // CS1750: A value of type 'string' cannot be used as a default parameter because there are no standard conversions to type 'int'
                //     void M2(int x = "string")
                Diagnostic(ErrorCode.ERR_NoConversionForDefaultParam, "x").WithArguments("string", "int")
            };

            VerifyOperationTreeAndDiagnosticsForTest<InvocationExpressionSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }

        [Fact]
        public void AssigningToIndexer()
        {
            string source = @"
class P
{
    private int _number = 0;
    public int this[int index]
    {
        get { return _number; }
        set { _number = value; }
    }

    void M1()
    {
        /*<bind>*/this[10]/*</bind>*/ = 9;
    }
}
";
            string expectedOperationTree = @"
IPropertyReferenceExpression: System.Int32 P.this[System.Int32 index] { get; set; } (OperationKind.PropertyReferenceExpression, Type: System.Int32) (Syntax: 'this[10]')
  Instance Receiver: IInstanceReferenceExpression (InstanceReferenceKind.Explicit) (OperationKind.InstanceReferenceExpression, Type: P) (Syntax: 'this')
  Arguments(1):
      IArgument (ArgumentKind.Explicit, Matching Parameter: index) (OperationKind.Argument) (Syntax: '10')
        ILiteralExpression (Text: 10) (OperationKind.LiteralExpression, Type: System.Int32, Constant: 10) (Syntax: '10')
        InConversion: null
        OutConversion: null
";
            var expectedDiagnostics = DiagnosticDescription.None;

            VerifyOperationTreeAndDiagnosticsForTest<ElementAccessExpressionSyntax>(source, expectedOperationTree, expectedDiagnostics, AdditionalOperationTreeVerifier: IndexerAccessArgumentVerifier.Verify);
        }

        [Fact]
        public void ReadingFromIndexer()
        {
            string source = @"
class P
{
    private int _number = 0;
    public int this[int index]
    {
        get { return _number; }
        set { _number = value; }
    }

    void M1()
    {
        var x = /*<bind>*/this[10]/*</bind>*/;
    }
}
";
            string expectedOperationTree = @"
IPropertyReferenceExpression: System.Int32 P.this[System.Int32 index] { get; set; } (OperationKind.PropertyReferenceExpression, Type: System.Int32) (Syntax: 'this[10]')
  Instance Receiver: IInstanceReferenceExpression (InstanceReferenceKind.Explicit) (OperationKind.InstanceReferenceExpression, Type: P) (Syntax: 'this')
  Arguments(1):
      IArgument (ArgumentKind.Explicit, Matching Parameter: index) (OperationKind.Argument) (Syntax: '10')
        ILiteralExpression (Text: 10) (OperationKind.LiteralExpression, Type: System.Int32, Constant: 10) (Syntax: '10')
        InConversion: null
        OutConversion: null
";
            var expectedDiagnostics = DiagnosticDescription.None;

            VerifyOperationTreeAndDiagnosticsForTest<ElementAccessExpressionSyntax>(source, expectedOperationTree, expectedDiagnostics, AdditionalOperationTreeVerifier: IndexerAccessArgumentVerifier.Verify);
        }

        [Fact]
        public void DefaultArgumentForIndexerGetter()
        {
            string source = @"
class P
{
    private int _number = 0;
    public int this[int i = 1, int j = 2]
    {
        get { return _number; }
        set { _number = i + j; }
    }

    void M1()
    {
        var x = /*<bind>*/this[j:10]/*</bind>*/;
    }
}
";
            string expectedOperationTree = @"
IPropertyReferenceExpression: System.Int32 P.this[[System.Int32 i = 1], [System.Int32 j = 2]] { get; set; } (OperationKind.PropertyReferenceExpression, Type: System.Int32) (Syntax: 'this[j:10]')
  Instance Receiver: IInstanceReferenceExpression (InstanceReferenceKind.Explicit) (OperationKind.InstanceReferenceExpression, Type: P) (Syntax: 'this')
  Arguments(2):
      IArgument (ArgumentKind.Explicit, Matching Parameter: j) (OperationKind.Argument) (Syntax: '10')
        ILiteralExpression (Text: 10) (OperationKind.LiteralExpression, Type: System.Int32, Constant: 10) (Syntax: '10')
        InConversion: null
        OutConversion: null
      IArgument (ArgumentKind.DefaultValue, Matching Parameter: i) (OperationKind.Argument) (Syntax: 'this[j:10]')
        ILiteralExpression (OperationKind.LiteralExpression, Type: System.Int32, Constant: 1) (Syntax: 'this[j:10]')
        InConversion: null
        OutConversion: null
";

            var expectedDiagnostics = DiagnosticDescription.None;

            VerifyOperationTreeAndDiagnosticsForTest<ElementAccessExpressionSyntax>(source, expectedOperationTree, expectedDiagnostics, AdditionalOperationTreeVerifier: IndexerAccessArgumentVerifier.Verify);
        }

        [Fact]
        public void ReadingFromWriteOnlyIndexer()
        {
            string source = @"
class P
{
    private int _number = 0;
    public int this[int index]
    {
        set { _number = value; }
    }

    void M1()
    {
        var x = /*<bind>*/this[10]/*</bind>*/;
    }
}
";
string expectedOperationTree = @"
IPropertyReferenceExpression: System.Int32 P.this[System.Int32 index] { set; } (OperationKind.PropertyReferenceExpression, Type: System.Int32, IsInvalid) (Syntax: 'this[10]')
  Instance Receiver: IInstanceReferenceExpression (InstanceReferenceKind.Explicit) (OperationKind.InstanceReferenceExpression, Type: P, IsInvalid) (Syntax: 'this')
  Arguments(1):
      IArgument (ArgumentKind.Explicit, Matching Parameter: null) (OperationKind.Argument, IsInvalid) (Syntax: '10')
        ILiteralExpression (Text: 10) (OperationKind.LiteralExpression, Type: System.Int32, Constant: 10, IsInvalid) (Syntax: '10')
        InConversion: null
        OutConversion: null
";
            var expectedDiagnostics = new DiagnosticDescription[] {               
                // file.cs(12,27): error CS0154: The property or indexer 'P.this[int]' cannot be used in this context because it lacks the get accessor
                //         var x = /*<bind>*/this[10]/*</bind>*/;
                Diagnostic(ErrorCode.ERR_PropertyLacksGet, "this[10]").WithArguments("P.this[int]").WithLocation(12, 27)
            };

            VerifyOperationTreeAndDiagnosticsForTest<ElementAccessExpressionSyntax>(source, expectedOperationTree, expectedDiagnostics, AdditionalOperationTreeVerifier: IndexerAccessArgumentVerifier.Verify);
        }

        [Fact]
        public void AssigningToReadOnlyIndexer()
        {
            string source = @"
class P
{
    private int _number = 0;
    public int this[int index]
    {
        get { return _number; }
    }

    void M1()
    {
        /*<bind>*/this[10]/*</bind>*/ = 9;
    }
}
";
string expectedOperationTree = @"
IPropertyReferenceExpression: System.Int32 P.this[System.Int32 index] { get; } (OperationKind.PropertyReferenceExpression, Type: System.Int32, IsInvalid) (Syntax: 'this[10]')
  Instance Receiver: IInstanceReferenceExpression (InstanceReferenceKind.Explicit) (OperationKind.InstanceReferenceExpression, Type: P, IsInvalid) (Syntax: 'this')
  Arguments(1):
      IArgument (ArgumentKind.Explicit, Matching Parameter: null) (OperationKind.Argument, IsInvalid) (Syntax: '10')
        ILiteralExpression (Text: 10) (OperationKind.LiteralExpression, Type: System.Int32, Constant: 10, IsInvalid) (Syntax: '10')
        InConversion: null
        OutConversion: null
";

            var expectedDiagnostics = new DiagnosticDescription[] {
                // file.cs(12,19): error CS0200: Property or indexer 'P.this[int]' cannot be assigned to -- it is read only
                //         /*<bind>*/this[10]/*</bind>*/ = 9;
                Diagnostic(ErrorCode.ERR_AssgReadonlyProp, "this[10]").WithArguments("P.this[int]").WithLocation(12, 19)
            };

            VerifyOperationTreeAndDiagnosticsForTest<ElementAccessExpressionSyntax>(source, expectedOperationTree, expectedDiagnostics, AdditionalOperationTreeVerifier: IndexerAccessArgumentVerifier.Verify);
        }

        [Fact]
        public void OverridingIndexerWithDefaultArgument()
        {
            string source = @"
class Base
{
    public virtual int this[int x = 0, int y = 1]
    {
        set { }
        get { System.Console.Write(y); return 0; }
    }
}

class Derived : Base
{
    public override int this[int x = 8, int y = 9]
    {
        set { }
    }
}

internal class P
{
    static void Main()
    {
        var d = new Derived();
        var x = /*<bind>*/d[0]/*</bind>*/;
    }
}
";
            string expectedOperationTree = @"
IPropertyReferenceExpression: System.Int32 Derived.this[[System.Int32 x = 8], [System.Int32 y = 9]] { set; } (OperationKind.PropertyReferenceExpression, Type: System.Int32) (Syntax: 'd[0]')
  Instance Receiver: ILocalReferenceExpression: d (OperationKind.LocalReferenceExpression, Type: Derived) (Syntax: 'd')
  Arguments(2):
      IArgument (ArgumentKind.Explicit, Matching Parameter: x) (OperationKind.Argument) (Syntax: '0')
        ILiteralExpression (Text: 0) (OperationKind.LiteralExpression, Type: System.Int32, Constant: 0) (Syntax: '0')
        InConversion: null
        OutConversion: null
      IArgument (ArgumentKind.DefaultValue, Matching Parameter: y) (OperationKind.Argument) (Syntax: 'd[0]')
        ILiteralExpression (OperationKind.LiteralExpression, Type: System.Int32, Constant: 1) (Syntax: 'd[0]')
        InConversion: null
        OutConversion: null
";
            var expectedDiagnostics = DiagnosticDescription.None;

            string expectedOutput = @"1";

            VerifyOperationTreeAndDiagnosticsForTest<ElementAccessExpressionSyntax>(source, expectedOperationTree, expectedDiagnostics, AdditionalOperationTreeVerifier: IndexerAccessArgumentVerifier.Verify);

            CompileAndVerify(new[] { source }, new[] { SystemRef }, expectedOutput: expectedOutput);
        }

        [Fact]
        public void OmittedParamArrayArgumentInIndexerAccess()
        {
            string source = @"
class P
{
    public int this[int x, params int[] y]
    {
        set { }
        get { return 0; }
    }

    public void M()
    {
        /*<bind>*/this[0]/*</bind>*/ = 0;
    }
}
";
            string expectedOperationTree = @"
IPropertyReferenceExpression: System.Int32 P.this[System.Int32 x, params System.Int32[] y] { get; set; } (OperationKind.PropertyReferenceExpression, Type: System.Int32) (Syntax: 'this[0]')
  Instance Receiver: IInstanceReferenceExpression (InstanceReferenceKind.Explicit) (OperationKind.InstanceReferenceExpression, Type: P) (Syntax: 'this')
  Arguments(2):
      IArgument (ArgumentKind.Explicit, Matching Parameter: x) (OperationKind.Argument) (Syntax: '0')
        ILiteralExpression (Text: 0) (OperationKind.LiteralExpression, Type: System.Int32, Constant: 0) (Syntax: '0')
        InConversion: null
        OutConversion: null
      IArgument (ArgumentKind.ParamArray, Matching Parameter: y) (OperationKind.Argument) (Syntax: 'this[0]')
        IArrayCreationExpression (Element Type: System.Int32) (OperationKind.ArrayCreationExpression, Type: System.Int32[]) (Syntax: 'this[0]')
          Dimension Sizes(1):
              ILiteralExpression (OperationKind.LiteralExpression, Type: System.Int32, Constant: 0) (Syntax: 'this[0]')
          Initializer: IArrayInitializer (0 elements) (OperationKind.ArrayInitializer) (Syntax: 'this[0]')
              Element Values(0)
        InConversion: null
        OutConversion: null
";
            var expectedDiagnostics = DiagnosticDescription.None;

            VerifyOperationTreeAndDiagnosticsForTest<ElementAccessExpressionSyntax>(source, expectedOperationTree, expectedDiagnostics, AdditionalOperationTreeVerifier: IndexerAccessArgumentVerifier.Verify);
        }

        [Fact]
        public void AssigningToReturnsByRefIndexer()
        {
            string source = @"
class P
{
    ref int this[int x]
    {
        get => throw null;
    }

    public void M()
    {
        /*<bind>*/this[0]/*</bind>*/ = 0;
    }
}
";
            string expectedOperationTree = @"
IPropertyReferenceExpression: ref System.Int32 P.this[System.Int32 x] { get; } (OperationKind.PropertyReferenceExpression, Type: System.Int32) (Syntax: 'this[0]')
  Instance Receiver: IInstanceReferenceExpression (InstanceReferenceKind.Explicit) (OperationKind.InstanceReferenceExpression, Type: P) (Syntax: 'this')
  Arguments(1):
      IArgument (ArgumentKind.Explicit, Matching Parameter: x) (OperationKind.Argument) (Syntax: '0')
        ILiteralExpression (Text: 0) (OperationKind.LiteralExpression, Type: System.Int32, Constant: 0) (Syntax: '0')
        InConversion: null
        OutConversion: null
";
            var expectedDiagnostics = DiagnosticDescription.None;

            VerifyOperationTreeAndDiagnosticsForTest<ElementAccessExpressionSyntax>(source, expectedOperationTree, expectedDiagnostics, AdditionalOperationTreeVerifier: IndexerAccessArgumentVerifier.Verify);
        }

        [ClrOnlyFact(ClrOnlyReason.Ilasm)]
        public void AssigningToIndexer_UsingDefaultArgumentFromSetter()
        {
            var il = @"
.class public auto ansi beforefieldinit P
       extends [mscorlib]System.Object
{
  .custom instance void [mscorlib]System.Reflection.DefaultMemberAttribute::.ctor(string) 
           = {string('Item')}

  .method public hidebysig specialname rtspecialname 
          instance void  .ctor() cil managed
  {
    // Code size       8 (0x8)
    .maxstack  8
    IL_0000:  ldarg.0
    IL_0001:  call       instance void [mscorlib]System.Object::.ctor()
    IL_0006:  nop
    IL_0007:  ret
  } // end of method P::.ctor

  .method public hidebysig specialname instance int32 
          get_Item([opt] int32 i,
                   [opt] int32 j) cil managed
  {
    .param [1] = int32(0x00000001)
    .param [2] = int32(0x00000002)
    // Code size       35 (0x23)
    .maxstack  3
    .locals init ([0] int32 V_0)
    IL_0000:  nop
    IL_0001:  ldstr      ""{0} {1}""
    IL_0006:  ldarg.1
    IL_0007:  box        [mscorlib]System.Int32
    IL_000c:  ldarg.2
    IL_000d:  box        [mscorlib]System.Int32
    IL_0012:  call       string [mscorlib]System.String::Format(string,
                                                                object,
                                                                object)
    IL_0017:  call       void [mscorlib]System.Console::WriteLine(string)
    IL_001c:  nop
    IL_001d:  ldc.i4.0
    IL_001e:  stloc.0
    IL_001f:  br.s       IL_0021
    IL_0021:  ldloc.0
    IL_0022:  ret
  } // end of method P::get_Item
  
  .method public hidebysig specialname instance void 
          set_Item([opt] int32 i,
                   [opt] int32 j,
                   int32 'value') cil managed
  {
    .param [1] = int32(0x00000003)
    .param [2] = int32(0x00000004)
    // Code size       30 (0x1e)
    .maxstack  8
    IL_0000:  nop
    IL_0001:  ldstr      ""{0} {1}""
    IL_0006:  ldarg.1
    IL_0007:  box        [mscorlib]System.Int32
    IL_000c:  ldarg.2
    IL_000d:  box        [mscorlib]System.Int32
    IL_0012:  call       string [mscorlib]System.String::Format(string,
                                                                object,
                                                                object)
    IL_0017:  call       void [mscorlib]System.Console::WriteLine(string)
    IL_001c:  nop
    IL_001d:  ret
  } // end of method P::set_Item


  .property instance int32 Item(int32,
                                int32)
  {
    .get instance int32 P::get_Item(int32,
                                    int32)
    .set instance void P::set_Item(int32,
                                   int32,
                                   int32)
  } // end of property P::Item
} // end of class P
";

            var csharp = @"
class C
{
    public static void Main(string[] args)
    {
         P p = new P();
         /*<bind>*/p[10]/*</bind>*/ = 9;
    }
}
";
            string expectedOperationTree = @"
IPropertyReferenceExpression: System.Int32 P.this[[System.Int32 i = 3], [System.Int32 j = 4]] { get; set; } (OperationKind.PropertyReferenceExpression, Type: System.Int32) (Syntax: 'p[10]')
  Instance Receiver: ILocalReferenceExpression: p (OperationKind.LocalReferenceExpression, Type: P) (Syntax: 'p')
  Arguments(2):
      IArgument (ArgumentKind.Explicit, Matching Parameter: i) (OperationKind.Argument) (Syntax: '10')
        ILiteralExpression (Text: 10) (OperationKind.LiteralExpression, Type: System.Int32, Constant: 10) (Syntax: '10')
        InConversion: null
        OutConversion: null
      IArgument (ArgumentKind.DefaultValue, Matching Parameter: j) (OperationKind.Argument) (Syntax: 'p[10]')
        ILiteralExpression (OperationKind.LiteralExpression, Type: System.Int32, Constant: 4) (Syntax: 'p[10]')
        InConversion: null
        OutConversion: null
";
            var expectedDiagnostics = DiagnosticDescription.None;
            var expectedOutput = @"10 4
";

            var ilReference = VerifyOperationTreeAndDiagnosticsForTestWithIL<ElementAccessExpressionSyntax>(csharp, il, expectedOperationTree, expectedDiagnostics, AdditionalOperationTreeVerifier: IndexerAccessArgumentVerifier.Verify);

            CompileAndVerify(new[] { csharp }, new[] { SystemRef, ilReference }, expectedOutput: expectedOutput);
        }

        [ClrOnlyFact(ClrOnlyReason.Ilasm)]
        public void ReadFromIndexer_UsingDefaultArgumentFromGetter()
        {
            var il = @"
.class public auto ansi beforefieldinit P
       extends [mscorlib]System.Object
{
  .custom instance void [mscorlib]System.Reflection.DefaultMemberAttribute::.ctor(string) 
           = {string('Item')}

  .method public hidebysig specialname rtspecialname 
          instance void  .ctor() cil managed
  {
    // Code size       8 (0x8)
    .maxstack  8
    IL_0000:  ldarg.0
    IL_0001:  call       instance void [mscorlib]System.Object::.ctor()
    IL_0006:  nop
    IL_0007:  ret
  } // end of method P::.ctor

  .method public hidebysig specialname instance int32 
          get_Item([opt] int32 i,
                   [opt] int32 j) cil managed
  {
    .param [1] = int32(0x00000001)
    .param [2] = int32(0x00000002)
    // Code size       35 (0x23)
    .maxstack  3
    .locals init ([0] int32 V_0)
    IL_0000:  nop
    IL_0001:  ldstr      ""{0} {1}""
    IL_0006:  ldarg.1
    IL_0007:  box        [mscorlib]System.Int32
    IL_000c:  ldarg.2
    IL_000d:  box        [mscorlib]System.Int32
    IL_0012:  call       string [mscorlib]System.String::Format(string,
                                                                object,
                                                                object)
    IL_0017:  call       void [mscorlib]System.Console::WriteLine(string)
    IL_001c:  nop
    IL_001d:  ldc.i4.0
    IL_001e:  stloc.0
    IL_001f:  br.s       IL_0021
    IL_0021:  ldloc.0
    IL_0022:  ret
  } // end of method P::get_Item
  
  .method public hidebysig specialname instance void 
          set_Item([opt] int32 i,
                   [opt] int32 j,
                   int32 'value') cil managed
  {    
    .param [1] = int32(0x00000003)
    .param [2] = int32(0x00000004)
    // Code size       30 (0x1e)
    .maxstack  8
    IL_0000:  nop
    IL_0001:  ldstr      ""{0} {1}""
    IL_0006:  ldarg.1
    IL_0007:  box        [mscorlib]System.Int32
    IL_000c:  ldarg.2
    IL_000d:  box        [mscorlib]System.Int32
    IL_0012:  call       string [mscorlib]System.String::Format(string,
                                                                object,
                                                                object)
    IL_0017:  call       void [mscorlib]System.Console::WriteLine(string)
    IL_001c:  nop
    IL_001d:  ret
  } // end of method P::set_Item


  .property instance int32 Item(int32,
                                int32)
  {
    .get instance int32 P::get_Item(int32,
                                    int32)
    .set instance void P::set_Item(int32,
                                   int32,
                                   int32)
  } // end of property P::Item
} // end of class P
";

            var csharp = @"
class C
{
    public static void Main(string[] args)
    {
         P p = new P();
         var x = /*<bind>*/p[10]/*</bind>*/;
    }
}
";
            string expectedOperationTree = @"
IPropertyReferenceExpression: System.Int32 P.this[[System.Int32 i = 3], [System.Int32 j = 4]] { get; set; } (OperationKind.PropertyReferenceExpression, Type: System.Int32) (Syntax: 'p[10]')
  Instance Receiver: ILocalReferenceExpression: p (OperationKind.LocalReferenceExpression, Type: P) (Syntax: 'p')
  Arguments(2):
      IArgument (ArgumentKind.Explicit, Matching Parameter: i) (OperationKind.Argument) (Syntax: '10')
        ILiteralExpression (Text: 10) (OperationKind.LiteralExpression, Type: System.Int32, Constant: 10) (Syntax: '10')
        InConversion: null
        OutConversion: null
      IArgument (ArgumentKind.DefaultValue, Matching Parameter: j) (OperationKind.Argument) (Syntax: 'p[10]')
        ILiteralExpression (OperationKind.LiteralExpression, Type: System.Int32, Constant: 2) (Syntax: 'p[10]')
        InConversion: null
        OutConversion: null
";
            var expectedDiagnostics = DiagnosticDescription.None;

            var expectedOutput = @"10 2
";

            var ilReference = VerifyOperationTreeAndDiagnosticsForTestWithIL<ElementAccessExpressionSyntax>(csharp, il, expectedOperationTree, expectedDiagnostics, AdditionalOperationTreeVerifier: IndexerAccessArgumentVerifier.Verify);

            CompileAndVerify(new[] { csharp }, new[] { SystemRef, ilReference }, expectedOutput: expectedOutput);
        }

        [ClrOnlyFact(ClrOnlyReason.Ilasm)]
        public void IndexerAccess_LHSOfCompoundAssignment()
        {
            var il = @"
.class public auto ansi beforefieldinit P
       extends [mscorlib]System.Object
{
  .custom instance void [mscorlib]System.Reflection.DefaultMemberAttribute::.ctor(string) 
           = {string('Item')}

  .method public hidebysig specialname rtspecialname 
          instance void  .ctor() cil managed
  {
    // Code size       8 (0x8)
    .maxstack  8
    IL_0000:  ldarg.0
    IL_0001:  call       instance void [mscorlib]System.Object::.ctor()
    IL_0006:  nop
    IL_0007:  ret
  } // end of method P::.ctor

  .method public hidebysig specialname instance int32 
          get_Item([opt] int32 i,
                   [opt] int32 j) cil managed
  {
    .param [1] = int32(0x00000001)
    .param [2] = int32(0x00000002)
    // Code size       35 (0x23)
    .maxstack  3
    .locals init ([0] int32 V_0)
    IL_0000:  nop
    IL_0001:  ldstr      ""{0} {1}""
    IL_0006:  ldarg.1
    IL_0007:  box        [mscorlib]System.Int32
    IL_000c:  ldarg.2
    IL_000d:  box        [mscorlib]System.Int32
    IL_0012:  call       string [mscorlib]System.String::Format(string,
                                                                object,
                                                                object)
    IL_0017:  call       void [mscorlib]System.Console::WriteLine(string)
    IL_001c:  nop
    IL_001d:  ldc.i4.0
    IL_001e:  stloc.0
    IL_001f:  br.s       IL_0021
    IL_0021:  ldloc.0
    IL_0022:  ret
  } // end of method P::get_Item
  
  .method public hidebysig specialname instance void 
          set_Item([opt] int32 i,
                   [opt] int32 j,
                   int32 'value') cil managed
  { 
    .param [1] = int32(0x00000003)
    .param [2] = int32(0x00000004)
    // Code size       30 (0x1e)
    .maxstack  8
    IL_0000:  nop
    IL_0001:  ldstr      ""{0} {1}""
    IL_0006:  ldarg.1
    IL_0007:  box        [mscorlib]System.Int32
    IL_000c:  ldarg.2
    IL_000d:  box        [mscorlib]System.Int32
    IL_0012:  call       string [mscorlib]System.String::Format(string,
                                                                object,
                                                                object)
    IL_0017:  call       void [mscorlib]System.Console::WriteLine(string)
    IL_001c:  nop
    IL_001d:  ret
  } // end of method P::set_Item


  .property instance int32 Item(int32,
                                int32)
  {
    .get instance int32 P::get_Item(int32,
                                    int32)
    .set instance void P::set_Item(int32,
                                   int32,
                                   int32)
  } // end of property P::Item
} // end of class P
";

            var csharp = @"
class C
{
    public static void Main(string[] args)
    {
         P p = new P();
         /*<bind>*/p[10]/*</bind>*/ += 99;
    }
}
";
            string expectedOperationTree = @"
IPropertyReferenceExpression: System.Int32 P.this[[System.Int32 i = 3], [System.Int32 j = 4]] { get; set; } (OperationKind.PropertyReferenceExpression, Type: System.Int32) (Syntax: 'p[10]')
  Instance Receiver: ILocalReferenceExpression: p (OperationKind.LocalReferenceExpression, Type: P) (Syntax: 'p')
  Arguments(2):
      IArgument (ArgumentKind.Explicit, Matching Parameter: i) (OperationKind.Argument) (Syntax: '10')
        ILiteralExpression (Text: 10) (OperationKind.LiteralExpression, Type: System.Int32, Constant: 10) (Syntax: '10')
        InConversion: null
        OutConversion: null
      IArgument (ArgumentKind.DefaultValue, Matching Parameter: j) (OperationKind.Argument) (Syntax: 'p[10]')
        ILiteralExpression (OperationKind.LiteralExpression, Type: System.Int32, Constant: 2) (Syntax: 'p[10]')
        InConversion: null
        OutConversion: null
";
            var expectedDiagnostics = DiagnosticDescription.None;

            var expectedOutput = @"10 2
10 2
";

            var ilReference = VerifyOperationTreeAndDiagnosticsForTestWithIL<ElementAccessExpressionSyntax>(csharp, il, expectedOperationTree, expectedDiagnostics, AdditionalOperationTreeVerifier: IndexerAccessArgumentVerifier.Verify);

            CompileAndVerify(new[] { csharp }, new[] { SystemRef, ilReference }, expectedOutput: expectedOutput);
        }

        [ClrOnlyFact(ClrOnlyReason.Ilasm)]
        public void InvalidConversionForDefaultArgument_InIL()
        {
            var il = @"
.class public auto ansi beforefieldinit P
       extends [mscorlib]System.Object
{
  .method public hidebysig specialname rtspecialname 
          instance void  .ctor() cil managed
  {
    // Code size       8 (0x8)
    .maxstack  8
    IL_0000:  ldarg.0
    IL_0001:  call       instance void [mscorlib]System.Object::.ctor()
    IL_0006:  nop
    IL_0007:  ret
  } // end of method P::.ctor

  .method public hidebysig instance void  M1([opt] int32 s) cil managed
  {
    .param [1] = ""abc""
    // Code size       2 (0x2)
    .maxstack  8
    IL_0000: nop
    IL_0001:  ret
  } // end of method P::M1
} // end of class P 
";

            var csharp = @"
class C
{
    public void M2()
    {
         P p = new P();
         /*<bind>*/p.M1()/*</bind>*/;
    }
}
";
string expectedOperationTree = @"
IInvocationExpression ( void P.M1([System.Int32 s = ""abc""])) (OperationKind.InvocationExpression, Type: System.Void) (Syntax: 'p.M1()')
  Instance Receiver: ILocalReferenceExpression: p (OperationKind.LocalReferenceExpression, Type: P) (Syntax: 'p')
  Arguments(1):
      IArgument (ArgumentKind.DefaultValue, Matching Parameter: s) (OperationKind.Argument) (Syntax: 'p.M1()')
        IConversionExpression (ConversionKind.Invalid, Implicit) (OperationKind.ConversionExpression, Type: System.Int32) (Syntax: 'p.M1()')
          Operand: ILiteralExpression (OperationKind.LiteralExpression, Type: System.String, Constant: ""abc"") (Syntax: 'p.M1()')
        InConversion: null
        OutConversion: null
";
            var expectedDiagnostics = DiagnosticDescription.None;

            VerifyOperationTreeAndDiagnosticsForTestWithIL<InvocationExpressionSyntax>(csharp, il, expectedOperationTree, expectedDiagnostics);
        }

        [Fact, WorkItem(20330, "https://github.com/dotnet/roslyn/issues/20330")]
        public void DefaultValueNullForNullableTypeParameterWithMissingNullableReference()
        {
            string source = @"
class P
{
    static void M1()
    {
        /*<bind>*/M2()/*</bind>*/;
    }

    static void M2(bool? x = null)
    {
    }
}
";
string expectedOperationTree = @"
IInvocationExpression (void P.M2([System.Boolean[missing]? x = null])) (OperationKind.InvocationExpression, Type: System.Void[missing], IsInvalid) (Syntax: 'M2()')
  Instance Receiver: null
  Arguments(0)
";

            var expectedDiagnostics = new DiagnosticDescription[] { 
                // (3,7): error CS0518: Predefined type 'System.Object' is not defined or imported
                // class P
                Diagnostic(ErrorCode.ERR_PredefinedTypeNotFound, "P").WithArguments("System.Object").WithLocation(2, 7),
                // (10,20): error CS0518: Predefined type 'System.Nullable`1' is not defined or imported
                //     static void M2(bool? x = null)
                Diagnostic(ErrorCode.ERR_PredefinedTypeNotFound, "bool?").WithArguments("System.Nullable`1").WithLocation(9, 20),
                // (10,20): error CS0518: Predefined type 'System.Boolean' is not defined or imported
                //     static void M2(bool? x = null)
                Diagnostic(ErrorCode.ERR_PredefinedTypeNotFound, "bool").WithArguments("System.Boolean").WithLocation(9, 20),
                // (10,12): error CS0518: Predefined type 'System.Void' is not defined or imported
                //     static void M2(bool? x = null)
                Diagnostic(ErrorCode.ERR_PredefinedTypeNotFound, "void").WithArguments("System.Void").WithLocation(9, 12),
                // (5,12): error CS0518: Predefined type 'System.Void' is not defined or imported
                //     static void M1()
                Diagnostic(ErrorCode.ERR_PredefinedTypeNotFound, "void").WithArguments("System.Void").WithLocation(4, 12),
                // (7,19): error CS0518: Predefined type 'System.Object' is not defined or imported
                //         /*<bind>*/M2()/*</bind>*/;
                Diagnostic(ErrorCode.ERR_PredefinedTypeNotFound, "M2").WithArguments("System.Object").WithLocation(6, 19),
                // (3,7): error CS1729: 'object' does not contain a constructor that takes 0 arguments
                // class P
                Diagnostic(ErrorCode.ERR_BadCtorArgCount, "P").WithArguments("object", "0").WithLocation(2, 7)
            };

            var compilation = CreateCompilation(source, options: Test.Utilities.TestOptions.ReleaseDll);
            VerifyOperationTreeAndDiagnosticsForTest<InvocationExpressionSyntax>(compilation, expectedOperationTree, expectedDiagnostics);
        }

        [Fact, WorkItem(20330, "https://github.com/dotnet/roslyn/issues/20330")]
        public void DefaultValueWithParameterErrorType()
        {
            string source = @"
class P
{
    static void M1()
    {
        /*<bind>*/M2(1)/*</bind>*/;
    }

    static void M2(int x, S s = 0)
    {
    }
}
";
string expectedOperationTree = @"
IInvocationExpression (void P.M2(System.Int32 x, [S s = null])) (OperationKind.InvocationExpression, Type: System.Void) (Syntax: 'M2(1)')
  Instance Receiver: null
  Arguments(1):
      IArgument (ArgumentKind.Explicit, Matching Parameter: null) (OperationKind.Argument) (Syntax: '1')
        ILiteralExpression (Text: 1) (OperationKind.LiteralExpression, Type: System.Int32, Constant: 1) (Syntax: '1')
        InConversion: null
        OutConversion: null
";

            var expectedDiagnostics = new DiagnosticDescription[] { 
                      // file.cs(9,27): error CS0246: The type or namespace name 'S' could not be found (are you missing a using directive or an assembly reference?)
                      //     static void M2(int x, S s = 0)
                      Diagnostic(ErrorCode.ERR_SingleTypeNameNotFound, "S").WithArguments("S").WithLocation(9, 27),
                      // file.cs(9,29): error CS1750: A value of type 'int' cannot be used as a default parameter because there are no standard conversions to type 'S'
                      //     static void M2(int x, S s = 0)
                      Diagnostic(ErrorCode.ERR_NoConversionForDefaultParam, "s").WithArguments("int", "S").WithLocation(9, 29)
            };

            VerifyOperationTreeAndDiagnosticsForTest<InvocationExpressionSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }

        [Fact]
        [WorkItem(18722, "https://github.com/dotnet/roslyn/issues/18722")]
        public void DefaultValueForGenericWithUndefinedTypeArgument()
        {
            // TODO: https://github.com/dotnet/roslyn/issues/18722
            //       This should be treated as invalid invocation.
            string source = @"
class P
{
    static void M1()
    {
        /*<bind>*/M2(1)/*</bind>*/;
    }

    static void M2(int x, G<S> s = null)
    {
    }
}

class G<T>
{
}
";
            string expectedOperationTree = @"
IInvocationExpression (void P.M2(System.Int32 x, [G<S> s = null])) (OperationKind.InvocationExpression, Type: System.Void) (Syntax: 'M2(1)')
  Instance Receiver: null
  Arguments(2):
      IArgument (ArgumentKind.Explicit, Matching Parameter: x) (OperationKind.Argument) (Syntax: '1')
        ILiteralExpression (Text: 1) (OperationKind.LiteralExpression, Type: System.Int32, Constant: 1) (Syntax: '1')
        InConversion: null
        OutConversion: null
      IArgument (ArgumentKind.DefaultValue, Matching Parameter: s) (OperationKind.Argument) (Syntax: 'M2(1)')
        ILiteralExpression (OperationKind.LiteralExpression, Type: G<S>, Constant: null) (Syntax: 'M2(1)')
        InConversion: null
        OutConversion: null
";

            var expectedDiagnostics = new DiagnosticDescription[] { 
                      // file.cs(9,29): error CS0246: The type or namespace name 'S' could not be found (are you missing a using directive or an assembly reference?)
                      //     static void M2(int x, G<S> s = null)
                      Diagnostic(ErrorCode.ERR_SingleTypeNameNotFound, "S").WithArguments("S").WithLocation(9, 29)
            };

            VerifyOperationTreeAndDiagnosticsForTest<InvocationExpressionSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }

        [Fact]
        [WorkItem(18722, "https://github.com/dotnet/roslyn/issues/18722")]
        public void DefaultValueForNullableGenericWithUndefinedTypeArgument()
        {
            // TODO: https://github.com/dotnet/roslyn/issues/18722
            //       This should be treated as invalid invocation.
            string source = @"
class P
{
    static void M1()
    {
        /*<bind>*/M2(1)/*</bind>*/;
    }

    static void M2(int x, G<S>? s = null)
    {
    }
}

struct G<T>
{
}
";
            string expectedOperationTree = @"
IInvocationExpression (void P.M2(System.Int32 x, [G<S>? s = null])) (OperationKind.InvocationExpression, Type: System.Void) (Syntax: 'M2(1)')
  Instance Receiver: null
  Arguments(2):
      IArgument (ArgumentKind.Explicit, Matching Parameter: x) (OperationKind.Argument) (Syntax: '1')
        ILiteralExpression (Text: 1) (OperationKind.LiteralExpression, Type: System.Int32, Constant: 1) (Syntax: '1')
        InConversion: null
        OutConversion: null
      IArgument (ArgumentKind.DefaultValue, Matching Parameter: s) (OperationKind.Argument) (Syntax: 'M2(1)')
        IDefaultValueExpression (OperationKind.DefaultValueExpression, Type: G<S>?) (Syntax: 'M2(1)')
        InConversion: null
        OutConversion: null
";

            var expectedDiagnostics = new DiagnosticDescription[] { 
                      // file.cs(9,29): error CS0246: The type or namespace name 'S' could not be found (are you missing a using directive or an assembly reference?)
                      //     static void M2(int x, G<S> s = null)
                      Diagnostic(ErrorCode.ERR_SingleTypeNameNotFound, "S").WithArguments("S").WithLocation(9, 29)
            };

            VerifyOperationTreeAndDiagnosticsForTest<InvocationExpressionSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }

        private class IndexerAccessArgumentVerifier : OperationWalker
        {
            private readonly Compilation _compilation;

            private IndexerAccessArgumentVerifier(Compilation compilation)
            {
                _compilation = compilation;
            }

            public static void Verify(IOperation operation, Compilation compilation, SyntaxNode syntaxNode)
            {
                new IndexerAccessArgumentVerifier(compilation).Visit(operation);
            }

            public override void VisitPropertyReferenceExpression(IPropertyReferenceExpression operation)
            {
                if (operation.HasErrors(_compilation) || operation.ArgumentsInEvaluationOrder.Length == 0)
                {
                    return;
                }

                // Check if the parameter symbol for argument is corresponding to indexer instead of accessor.
                var indexerSymbol = operation.Property;
                foreach (var argument in operation.ArgumentsInEvaluationOrder)
                {
                    if (!argument.HasErrors(_compilation))
                    {
                        Assert.Same(indexerSymbol, argument.Parameter.ContainingSymbol);
                    }
                }
            }
        }
    }
}
