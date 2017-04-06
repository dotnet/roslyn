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
        /*<bind>*/M2(1, 2.0)/*</bind>*/;
    }

    static void M2(int x, double y) { }
}
";
            string expectedOperationTree = @"
IInvocationExpression (static void P.M2(System.Int32 x, System.Double y)) (OperationKind.InvocationExpression, Type: System.Void) (Syntax: 'M2(1, 2.0)')
  Arguments(2): IArgument (ArgumentKind.Explicit Matching Parameter: x) (OperationKind.Argument) (Syntax: '1')
      ILiteralExpression (Text: 1) (OperationKind.LiteralExpression, Type: System.Int32, Constant: 1) (Syntax: '1')
    IArgument (ArgumentKind.Explicit Matching Parameter: y) (OperationKind.Argument) (Syntax: '2.0')
      ILiteralExpression (OperationKind.LiteralExpression, Type: System.Double, Constant: 2) (Syntax: '2.0')
";
            var expectedDiagnostics = DiagnosticDescription.None;

            VerifyOperationTreeAndDiagnosticsForTest<InvocationExpressionSyntax>(source, expectedOperationTree, expectedDiagnostics);
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

    static void M2(int x, double y = 0.0) { }
}
";
            string expectedOperationTree = @"
IInvocationExpression (static void P.M2(System.Int32 x, [System.Double y = 0])) (OperationKind.InvocationExpression, Type: System.Void) (Syntax: 'M2(1)')
  Arguments(2): IArgument (ArgumentKind.Explicit Matching Parameter: x) (OperationKind.Argument) (Syntax: '1')
      ILiteralExpression (Text: 1) (OperationKind.LiteralExpression, Type: System.Int32, Constant: 1) (Syntax: '1')
    IArgument (ArgumentKind.DefaultValue Matching Parameter: y) (OperationKind.Argument) (Syntax: 'M2(1)')
      ILiteralExpression (Text: 0) (OperationKind.LiteralExpression, Type: System.Double, Constant: 0) (Syntax: 'M2(1)')
";
            var expectedDiagnostics = DiagnosticDescription.None;

            VerifyOperationTreeAndDiagnosticsForTest<InvocationExpressionSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }                    

        [Fact]
        public void ExplicitNamedArgument()
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
  Arguments(2): IArgument (ArgumentKind.Explicit Matching Parameter: x) (OperationKind.Argument) (Syntax: '1')
      ILiteralExpression (Text: 1) (OperationKind.LiteralExpression, Type: System.Int32, Constant: 1) (Syntax: '1')
    IArgument (ArgumentKind.Explicit Matching Parameter: y) (OperationKind.Argument) (Syntax: '9.9')
      ILiteralExpression (Text: 9.9) (OperationKind.LiteralExpression, Type: System.Double, Constant: 9.9) (Syntax: '9.9')
";
            var expectedDiagnostics = DiagnosticDescription.None;

            VerifyOperationTreeAndDiagnosticsForTest<InvocationExpressionSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }


        [Fact]
        public void OutOfOrderExplicitNamedArgument()
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
  Arguments(2): IArgument (ArgumentKind.Explicit Matching Parameter: y) (OperationKind.Argument) (Syntax: '9.9')
      ILiteralExpression (Text: 9.9) (OperationKind.LiteralExpression, Type: System.Double, Constant: 9.9) (Syntax: '9.9')
    IArgument (ArgumentKind.Explicit Matching Parameter: x) (OperationKind.Argument) (Syntax: '1')
      ILiteralExpression (Text: 1) (OperationKind.LiteralExpression, Type: System.Int32, Constant: 1) (Syntax: '1')
";
            var expectedDiagnostics = DiagnosticDescription.None;

            VerifyOperationTreeAndDiagnosticsForTest<InvocationExpressionSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }

        [Fact]
        public void DefaultNamedArgument()
        {
            string source = @"
class P
{
    static void M1()
    {
        /*<bind>*/M2(y: 9.9)/*</bind>*/;
    }

    static void M2(int x = 1, double y = 0.0) { }
}
";
            string expectedOperationTree = @"
IInvocationExpression (static void P.M2([System.Int32 x = 1], [System.Double y = 0])) (OperationKind.InvocationExpression, Type: System.Void) (Syntax: 'M2(y: 9.9)')
  Arguments(2): IArgument (ArgumentKind.Explicit Matching Parameter: y) (OperationKind.Argument) (Syntax: '9.9')
      ILiteralExpression (Text: 9.9) (OperationKind.LiteralExpression, Type: System.Double, Constant: 9.9) (Syntax: '9.9')
    IArgument (ArgumentKind.DefaultValue Matching Parameter: x) (OperationKind.Argument) (Syntax: 'M2(y: 9.9)')
      ILiteralExpression (Text: 1) (OperationKind.LiteralExpression, Type: System.Int32, Constant: 1) (Syntax: 'M2(y: 9.9)')
";
            var expectedDiagnostics = DiagnosticDescription.None;

            VerifyOperationTreeAndDiagnosticsForTest<InvocationExpressionSyntax>(source, expectedOperationTree, expectedDiagnostics);
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
IInvocationExpression (static void P.M2([System.Int32 x = 1], [System.Int32 y = 2], [System.Int32 z = 3])) (OperationKind.InvocationExpression, Type: System.Void) (Syntax: 'M2(1, z: 10)')
  Arguments(3): IArgument (ArgumentKind.Explicit Matching Parameter: x) (OperationKind.Argument) (Syntax: '1')
      ILiteralExpression (Text: 1) (OperationKind.LiteralExpression, Type: System.Int32, Constant: 1) (Syntax: '1')
    IArgument (ArgumentKind.Explicit Matching Parameter: z) (OperationKind.Argument) (Syntax: '10')
      ILiteralExpression (Text: 10) (OperationKind.LiteralExpression, Type: System.Int32, Constant: 10) (Syntax: '10')
    IArgument (ArgumentKind.DefaultValue Matching Parameter: y) (OperationKind.Argument) (Syntax: 'M2(1, z: 10)')
      ILiteralExpression (Text: 2) (OperationKind.LiteralExpression, Type: System.Int32, Constant: 2) (Syntax: 'M2(1, z: 10)')
";
            var expectedDiagnostics = DiagnosticDescription.None;

            VerifyOperationTreeAndDiagnosticsForTest<InvocationExpressionSyntax>(source, expectedOperationTree, expectedDiagnostics);
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
IInvocationExpression ( void P.M2(ref System.Int32 x, out System.Int32 y)) (OperationKind.InvocationExpression, Type: System.Void) (Syntax: 'M2(ref a, out b)')
  Instance Receiver: IInstanceReferenceExpression (InstanceReferenceKind.Implicit) (OperationKind.InstanceReferenceExpression, Type: P) (Syntax: 'M2')
  Arguments(2): IArgument (ArgumentKind.Explicit Matching Parameter: x) (OperationKind.Argument) (Syntax: 'a')
      ILocalReferenceExpression: a (OperationKind.LocalReferenceExpression, Type: System.Int32) (Syntax: 'a')
    IArgument (ArgumentKind.Explicit Matching Parameter: y) (OperationKind.Argument) (Syntax: 'b')
      ILocalReferenceExpression: b (OperationKind.LocalReferenceExpression, Type: System.Int32) (Syntax: 'b')
";
            var expectedDiagnostics = DiagnosticDescription.None;

            VerifyOperationTreeAndDiagnosticsForTest<InvocationExpressionSyntax>(source, expectedOperationTree, expectedDiagnostics);
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
IInvocationExpression ( void P.M2(ref System.Int32 x, out System.Int32 y)) (OperationKind.InvocationExpression, Type: System.Void) (Syntax: 'M2(y: out b, x: ref a)')
  Instance Receiver: IInstanceReferenceExpression (InstanceReferenceKind.Implicit) (OperationKind.InstanceReferenceExpression, Type: P) (Syntax: 'M2')
  Arguments(2): IArgument (ArgumentKind.Explicit Matching Parameter: y) (OperationKind.Argument) (Syntax: 'b')
      ILocalReferenceExpression: b (OperationKind.LocalReferenceExpression, Type: System.Int32) (Syntax: 'b')
    IArgument (ArgumentKind.Explicit Matching Parameter: x) (OperationKind.Argument) (Syntax: 'a')
      ILocalReferenceExpression: a (OperationKind.LocalReferenceExpression, Type: System.Int32) (Syntax: 'a')
";
            var expectedDiagnostics = DiagnosticDescription.None;

            VerifyOperationTreeAndDiagnosticsForTest<InvocationExpressionSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }

        [Fact]
        public void OmittedParamsArrayArgument()
        {
            string source = @"
class P
{
    void M1()
    {
        /*<bind>*/M2(null)/*</bind>*/;
    }

    void M2(string str, params int[] array) { }
}
";
            string expectedOperationTree = @"
IInvocationExpression ( void P.M2(System.String str, params System.Int32[] array)) (OperationKind.InvocationExpression, Type: System.Void) (Syntax: 'M2(null)')
  Instance Receiver: IInstanceReferenceExpression (InstanceReferenceKind.Implicit) (OperationKind.InstanceReferenceExpression, Type: P) (Syntax: 'M2')
  Arguments(2): IArgument (ArgumentKind.Explicit Matching Parameter: str) (OperationKind.Argument) (Syntax: 'null')
      IConversionExpression (ConversionKind.Cast, Implicit) (OperationKind.ConversionExpression, Type: System.String, Constant: null) (Syntax: 'null')
        ILiteralExpression (Text: null) (OperationKind.LiteralExpression, Type: null, Constant: null) (Syntax: 'null')
    IArgument (ArgumentKind.ParamArray Matching Parameter: array) (OperationKind.Argument) (Syntax: 'M2(null)')
      IArrayCreationExpression (Element Type: System.Int32) (OperationKind.ArrayCreationExpression, Type: System.Int32[]) (Syntax: 'M2(null)')
        Dimension Sizes(1): ILiteralExpression (Text: 0) (OperationKind.LiteralExpression, Type: null, Constant: 0) (Syntax: 'M2(null)')
        Initializer: IArrayInitializer (OperationKind.ArrayInitializer) (Syntax: 'M2(null)')
";
            var expectedDiagnostics = DiagnosticDescription.None;

            VerifyOperationTreeAndDiagnosticsForTest<InvocationExpressionSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }

        [Fact]
        public void ParamsArrayArguments()
        {
            string source = @"
class P
{
    void M1()
    {
        /*<bind>*/M2(null, 1, 2)/*</bind>*/;
    }

    void M2(string str, params int[] array) { }
}
";
            string expectedOperationTree = @"
IInvocationExpression ( void P.M2(System.String str, params System.Int32[] array)) (OperationKind.InvocationExpression, Type: System.Void) (Syntax: 'M2(null, 1, 2)')
  Instance Receiver: IInstanceReferenceExpression (InstanceReferenceKind.Implicit) (OperationKind.InstanceReferenceExpression, Type: P) (Syntax: 'M2')
  Arguments(2): IArgument (ArgumentKind.Explicit Matching Parameter: str) (OperationKind.Argument) (Syntax: 'null')
      IConversionExpression (ConversionKind.Cast, Implicit) (OperationKind.ConversionExpression, Type: System.String, Constant: null) (Syntax: 'null')
        ILiteralExpression (Text: null) (OperationKind.LiteralExpression, Type: null, Constant: null) (Syntax: 'null')
    IArgument (ArgumentKind.ParamArray Matching Parameter: array) (OperationKind.Argument) (Syntax: '1')
      IArrayCreationExpression (Element Type: System.Int32) (OperationKind.ArrayCreationExpression, Type: System.Int32[]) (Syntax: '1')
        Dimension Sizes(1): ILiteralExpression (Text: 2) (OperationKind.LiteralExpression, Type: null, Constant: 2) (Syntax: '1')
        Initializer: IArrayInitializer (OperationKind.ArrayInitializer) (Syntax: '1')
            Element Values(2): ILiteralExpression (Text: 1) (OperationKind.LiteralExpression, Type: System.Int32, Constant: 1) (Syntax: '1')
              ILiteralExpression (Text: 2) (OperationKind.LiteralExpression, Type: System.Int32, Constant: 2) (Syntax: '2')
";
            var expectedDiagnostics = DiagnosticDescription.None;

            VerifyOperationTreeAndDiagnosticsForTest<InvocationExpressionSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }

        [Fact]
        public void ArrayAsParamsArrayArgument()
        {
            string source = @"
class P
{
    void M1()
    {
        /*<bind>*/M2(null, new[] { 1, 2 })/*</bind>*/;
    }

    void M2(string str, params int[] array) { }
}
";
            string expectedOperationTree = @"
IInvocationExpression ( void P.M2(System.String str, params System.Int32[] array)) (OperationKind.InvocationExpression, Type: System.Void) (Syntax: 'M2(null, new[] { 1, 2 })')
  Instance Receiver: IInstanceReferenceExpression (InstanceReferenceKind.Implicit) (OperationKind.InstanceReferenceExpression, Type: P) (Syntax: 'M2')
  Arguments(2): IArgument (ArgumentKind.Explicit Matching Parameter: str) (OperationKind.Argument) (Syntax: 'null')
      IConversionExpression (ConversionKind.Cast, Implicit) (OperationKind.ConversionExpression, Type: System.String, Constant: null) (Syntax: 'null')
        ILiteralExpression (Text: null) (OperationKind.LiteralExpression, Type: null, Constant: null) (Syntax: 'null')
    IArgument (ArgumentKind.Explicit Matching Parameter: array) (OperationKind.Argument) (Syntax: 'new[] { 1, 2 }')
      IArrayCreationExpression (Element Type: System.Int32) (OperationKind.ArrayCreationExpression, Type: System.Int32[]) (Syntax: 'new[] { 1, 2 }')
        Dimension Sizes(1): ILiteralExpression (OperationKind.LiteralExpression, Type: System.Int32, Constant: 2) (Syntax: 'new[] { 1, 2 }')
        Initializer: IArrayInitializer (OperationKind.ArrayInitializer) (Syntax: '{ 1, 2 }')
            Element Values(2): ILiteralExpression (Text: 1) (OperationKind.LiteralExpression, Type: System.Int32, Constant: 1) (Syntax: '1')
              ILiteralExpression (Text: 2) (OperationKind.LiteralExpression, Type: System.Int32, Constant: 2) (Syntax: '2')
";
            var expectedDiagnostics = DiagnosticDescription.None;

            VerifyOperationTreeAndDiagnosticsForTest<InvocationExpressionSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }

        [Fact]
        public void NamedParamsArrayArgument()
        {
            string source = @"
class P
{
    void M1()
    {
        /*<bind>*/M2(array: 1, str: null)/*</bind>*/;
    }

    void M2(string str, params int[] array) { }
}
";
            string expectedOperationTree = @"
IInvocationExpression ( void P.M2(System.String str, params System.Int32[] array)) (OperationKind.InvocationExpression, Type: System.Void) (Syntax: 'M2(array: 1, str: null)')
  Instance Receiver: IInstanceReferenceExpression (InstanceReferenceKind.Implicit) (OperationKind.InstanceReferenceExpression, Type: P) (Syntax: 'M2')
  Arguments(2): IArgument (ArgumentKind.ParamArray Matching Parameter: array) (OperationKind.Argument) (Syntax: '1')
      IArrayCreationExpression (Element Type: System.Int32) (OperationKind.ArrayCreationExpression, Type: System.Int32[]) (Syntax: '1')
        Dimension Sizes(1): ILiteralExpression (Text: 1) (OperationKind.LiteralExpression, Type: null, Constant: 1) (Syntax: '1')
        Initializer: IArrayInitializer (OperationKind.ArrayInitializer) (Syntax: '1')
            Element Values(1): ILiteralExpression (Text: 1) (OperationKind.LiteralExpression, Type: System.Int32, Constant: 1) (Syntax: '1')
    IArgument (ArgumentKind.Explicit Matching Parameter: str) (OperationKind.Argument) (Syntax: 'null')
      IConversionExpression (ConversionKind.Cast, Implicit) (OperationKind.ConversionExpression, Type: System.String, Constant: null) (Syntax: 'null')
        ILiteralExpression (Text: null) (OperationKind.LiteralExpression, Type: null, Constant: null) (Syntax: 'null')
";
            var expectedDiagnostics = DiagnosticDescription.None;

            VerifyOperationTreeAndDiagnosticsForTest<InvocationExpressionSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }

        [Fact]
        public void ArrayAsNamedParamsArrayArgument()
        {
            string source = @"
class P
{
    void M1()
    {
        /*<bind>*/M2(array: new[] { 1, 2 }, str: null)/*</bind>*/;
    }

    void M2(string str, params int[] array) { }
}
";
            string expectedOperationTree = @"
IInvocationExpression ( void P.M2(System.String str, params System.Int32[] array)) (OperationKind.InvocationExpression, Type: System.Void) (Syntax: 'M2(array: n ...  str: null)')
  Instance Receiver: IInstanceReferenceExpression (InstanceReferenceKind.Implicit) (OperationKind.InstanceReferenceExpression, Type: P) (Syntax: 'M2')
  Arguments(2): IArgument (ArgumentKind.Explicit Matching Parameter: array) (OperationKind.Argument) (Syntax: 'new[] { 1, 2 }')
      IArrayCreationExpression (Element Type: System.Int32) (OperationKind.ArrayCreationExpression, Type: System.Int32[]) (Syntax: 'new[] { 1, 2 }')
        Dimension Sizes(1): ILiteralExpression (OperationKind.LiteralExpression, Type: System.Int32, Constant: 2) (Syntax: 'new[] { 1, 2 }')
        Initializer: IArrayInitializer (OperationKind.ArrayInitializer) (Syntax: '{ 1, 2 }')
            Element Values(2): ILiteralExpression (Text: 1) (OperationKind.LiteralExpression, Type: System.Int32, Constant: 1) (Syntax: '1')
              ILiteralExpression (Text: 2) (OperationKind.LiteralExpression, Type: System.Int32, Constant: 2) (Syntax: '2')
    IArgument (ArgumentKind.Explicit Matching Parameter: str) (OperationKind.Argument) (Syntax: 'null')
      IConversionExpression (ConversionKind.Cast, Implicit) (OperationKind.ConversionExpression, Type: System.String, Constant: null) (Syntax: 'null')
        ILiteralExpression (Text: null) (OperationKind.LiteralExpression, Type: null, Constant: null) (Syntax: 'null')
";
            var expectedDiagnostics = DiagnosticDescription.None;

            VerifyOperationTreeAndDiagnosticsForTest<InvocationExpressionSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }
    }
}
