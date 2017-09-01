// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.CSharp.UnitTests
{
    public partial class IOperationTests : SemanticModelTestBase
    {
        [CompilerTrait(CompilerFeature.IOperation)]
        [Fact]
        public void DynamicIndexerAccessExpression_DynamicArgument()
        {
            string source = @"
class C
{
    void M(C c, dynamic d)
    {
        var x = /*<bind>*/c[d]/*</bind>*/;
    }

    public int this[int i] => 0;
}
";
            string expectedOperationTree = @"
IDynamicIndexerAccessExpression (OperationKind.DynamicIndexerAccessExpression, Type: dynamic) (Syntax: 'c[d]')
  Expression: IParameterReferenceExpression: c (OperationKind.ParameterReferenceExpression, Type: C) (Syntax: 'c')
  ApplicableSymbols(1):
    Symbol: System.Int32 C.this[System.Int32 i] { get; }
  Arguments(1):
      IParameterReferenceExpression: d (OperationKind.ParameterReferenceExpression, Type: dynamic) (Syntax: 'd')
  ArgumentNames(0)
  ArgumentRefKinds(0)
";
            var expectedDiagnostics = DiagnosticDescription.None;

            VerifyOperationTreeAndDiagnosticsForTest<ElementAccessExpressionSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation)]
        [Fact]
        public void DynamicIndexerAccessExpression_MultipleApplicableSymbols()
        {
            string source = @"
class C
{
    void M(C c, dynamic d)
    {
        var x = /*<bind>*/c[d]/*</bind>*/;
    }

    public int this[int i] => 0;

    public int this[long i] => 0;
}
";
            string expectedOperationTree = @"
IDynamicIndexerAccessExpression (OperationKind.DynamicIndexerAccessExpression, Type: dynamic) (Syntax: 'c[d]')
  Expression: IParameterReferenceExpression: c (OperationKind.ParameterReferenceExpression, Type: C) (Syntax: 'c')
  ApplicableSymbols(2):
    Symbol: System.Int32 C.this[System.Int32 i] { get; }
    Symbol: System.Int32 C.this[System.Int64 i] { get; }
  Arguments(1):
      IParameterReferenceExpression: d (OperationKind.ParameterReferenceExpression, Type: dynamic) (Syntax: 'd')
  ArgumentNames(0)
  ArgumentRefKinds(0)
";
            var expectedDiagnostics = DiagnosticDescription.None;

            VerifyOperationTreeAndDiagnosticsForTest<ElementAccessExpressionSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation)]
        [Fact]
        public void DynamicIndexerAccessExpression_MultipleArgumentsAndApplicableSymbols()
        {
            string source = @"
class C
{
    void M(C c, dynamic d)
    {
        char ch = 'c';
        var x = /*<bind>*/c[d, ch]/*</bind>*/;
    }

    public int this[int i, char ch] => 0;

    public int this[long i, char ch] => 0;
}
";
            string expectedOperationTree = @"
IDynamicIndexerAccessExpression (OperationKind.DynamicIndexerAccessExpression, Type: dynamic) (Syntax: 'c[d, ch]')
  Expression: IParameterReferenceExpression: c (OperationKind.ParameterReferenceExpression, Type: C) (Syntax: 'c')
  ApplicableSymbols(2):
    Symbol: System.Int32 C.this[System.Int32 i, System.Char ch] { get; }
    Symbol: System.Int32 C.this[System.Int64 i, System.Char ch] { get; }
  Arguments(2):
      IParameterReferenceExpression: d (OperationKind.ParameterReferenceExpression, Type: dynamic) (Syntax: 'd')
      ILocalReferenceExpression: ch (OperationKind.LocalReferenceExpression, Type: System.Char) (Syntax: 'ch')
  ArgumentNames(0)
  ArgumentRefKinds(0)
";
            var expectedDiagnostics = DiagnosticDescription.None;

            VerifyOperationTreeAndDiagnosticsForTest<ElementAccessExpressionSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation)]
        [Fact]
        public void DynamicIndexerAccessExpression_ArgumentNames()
        {
            string source = @"
class C
{
    void M(C c, dynamic d, dynamic e)
    {
        var x = /*<bind>*/c[i: d, ch: e]/*</bind>*/;
    }

    public int this[int i, char ch] => 0;

    public int this[long i, char ch] => 0;
}
";
            string expectedOperationTree = @"
IDynamicIndexerAccessExpression (OperationKind.DynamicIndexerAccessExpression, Type: dynamic) (Syntax: 'c[i: d, ch: e]')
  Expression: IParameterReferenceExpression: c (OperationKind.ParameterReferenceExpression, Type: C) (Syntax: 'c')
  ApplicableSymbols(2):
    Symbol: System.Int32 C.this[System.Int32 i, System.Char ch] { get; }
    Symbol: System.Int32 C.this[System.Int64 i, System.Char ch] { get; }
  Arguments(2):
      IParameterReferenceExpression: d (OperationKind.ParameterReferenceExpression, Type: dynamic) (Syntax: 'd')
      IParameterReferenceExpression: e (OperationKind.ParameterReferenceExpression, Type: dynamic) (Syntax: 'e')
  ArgumentNames(2):
    ""i""
    ""ch""
  ArgumentRefKinds(0)
";
            var expectedDiagnostics = DiagnosticDescription.None;

            VerifyOperationTreeAndDiagnosticsForTest<ElementAccessExpressionSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation)]
        [Fact]
        public void DynamicIndexerAccessExpression_ArgumentRefKinds()
        {
            string source = @"
class C
{
    void M(C c, dynamic d, dynamic e)
    {
        var x = /*<bind>*/c[i: d, ch: ref e]/*</bind>*/;
    }

    public int this[int i, ref dynamic ch] => 0;
}
";
            string expectedOperationTree = @"
IDynamicIndexerAccessExpression (OperationKind.DynamicIndexerAccessExpression, Type: dynamic) (Syntax: 'c[i: d, ch: ref e]')
  Expression: IParameterReferenceExpression: c (OperationKind.ParameterReferenceExpression, Type: C) (Syntax: 'c')
  ApplicableSymbols(1):
    Symbol: System.Int32 C.this[System.Int32 i, ref dynamic ch] { get; }
  Arguments(2):
      IParameterReferenceExpression: d (OperationKind.ParameterReferenceExpression, Type: dynamic) (Syntax: 'd')
      IParameterReferenceExpression: e (OperationKind.ParameterReferenceExpression, Type: dynamic) (Syntax: 'e')
  ArgumentNames(2):
    ""i""
    ""ch""
  ArgumentRefKinds(2):
    None
    Ref
";
            var expectedDiagnostics = new DiagnosticDescription[] {
                // CS0631: ref and out are not valid in this context
                //     public int this[int i, ref dynamic ch] => 0;
                Diagnostic(ErrorCode.ERR_IllegalRefParam, "ref").WithLocation(9, 28)
            };

            VerifyOperationTreeAndDiagnosticsForTest<ElementAccessExpressionSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation)]
        [Fact]
        public void DynamicIndexerAccessExpression_WithDynamicReceiver()
        {
            string source = @"
class C
{
    void M(dynamic d, int i)
    {
        var x = /*<bind>*/d[i]/*</bind>*/;
    }

    public int this[int i] => 0;
}
";
            string expectedOperationTree = @"
IDynamicIndexerAccessExpression (OperationKind.DynamicIndexerAccessExpression, Type: dynamic) (Syntax: 'd[i]')
  Expression: IParameterReferenceExpression: d (OperationKind.ParameterReferenceExpression, Type: dynamic) (Syntax: 'd')
  ApplicableSymbols(0)
  Arguments(1):
      IParameterReferenceExpression: i (OperationKind.ParameterReferenceExpression, Type: System.Int32) (Syntax: 'i')
  ArgumentNames(0)
  ArgumentRefKinds(0)
";
            var expectedDiagnostics = DiagnosticDescription.None;

            VerifyOperationTreeAndDiagnosticsForTest<ElementAccessExpressionSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation)]
        [Fact]
        public void DynamicIndexerAccessExpression_WithDynamicMemberReceiver()
        {
            string source = @"
class C
{
    void M(dynamic c, int i)
    {
        var x = /*<bind>*/c.M2[i]/*</bind>*/;
    }

    public int this[int i] => 0;
}
";
            string expectedOperationTree = @"
IDynamicIndexerAccessExpression (OperationKind.DynamicIndexerAccessExpression, Type: dynamic) (Syntax: 'c.M2[i]')
  Expression: IDynamicMemberReferenceExpression (Member Name: ""M2"", Containing Type: null) (OperationKind.DynamicMemberReferenceExpression, Type: dynamic) (Syntax: 'c.M2')
      Type Arguments(0)
      Instance Receiver: IParameterReferenceExpression: c (OperationKind.ParameterReferenceExpression, Type: dynamic) (Syntax: 'c')
  ApplicableSymbols(0)
  Arguments(1):
      IParameterReferenceExpression: i (OperationKind.ParameterReferenceExpression, Type: System.Int32) (Syntax: 'i')
  ArgumentNames(0)
  ArgumentRefKinds(0)
";
            var expectedDiagnostics = DiagnosticDescription.None;

            VerifyOperationTreeAndDiagnosticsForTest<ElementAccessExpressionSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation)]
        [Fact]
        public void DynamicIndexerAccessExpression_WithDynamicTypedMemberReceiver()
        {
            string source = @"
class C
{
    dynamic M2 = null;

    void M(dynamic c, int i)
    {
        var x = /*<bind>*/c.M2[i]/*</bind>*/;
    }
}
";
            string expectedOperationTree = @"
IDynamicIndexerAccessExpression (OperationKind.DynamicIndexerAccessExpression, Type: dynamic) (Syntax: 'c.M2[i]')
  Expression: IDynamicMemberReferenceExpression (Member Name: ""M2"", Containing Type: null) (OperationKind.DynamicMemberReferenceExpression, Type: dynamic) (Syntax: 'c.M2')
      Type Arguments(0)
      Instance Receiver: IParameterReferenceExpression: c (OperationKind.ParameterReferenceExpression, Type: dynamic) (Syntax: 'c')
  ApplicableSymbols(0)
  Arguments(1):
      IParameterReferenceExpression: i (OperationKind.ParameterReferenceExpression, Type: System.Int32) (Syntax: 'i')
  ArgumentNames(0)
  ArgumentRefKinds(0)
";
            var expectedDiagnostics = new DiagnosticDescription[] {
                // CS0414: The field 'C.M2' is assigned but its value is never used
                //     dynamic M2 = null;
                Diagnostic(ErrorCode.WRN_UnreferencedFieldAssg, "M2").WithArguments("C.M2").WithLocation(4, 13)
            };

            VerifyOperationTreeAndDiagnosticsForTest<ElementAccessExpressionSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation)]
        [Fact]
        public void DynamicIndexerAccessExpression_AllFields()
        {
            string source = @"
class C
{
    void M(C c, dynamic d)
    {
        int i = 0;
        var x = /*<bind>*/c[ref i, c: d]/*</bind>*/;
    }

    public int this[ref int i, char c] => 0;
    public int this[ref int i, long c] => 0;
}
";
            string expectedOperationTree = @"
IDynamicIndexerAccessExpression (OperationKind.DynamicIndexerAccessExpression, Type: dynamic) (Syntax: 'c[ref i, c: d]')
  Expression: IParameterReferenceExpression: c (OperationKind.ParameterReferenceExpression, Type: C) (Syntax: 'c')
  ApplicableSymbols(2):
    Symbol: System.Int32 C.this[ref System.Int32 i, System.Char c] { get; }
    Symbol: System.Int32 C.this[ref System.Int32 i, System.Int64 c] { get; }
  Arguments(2):
      ILocalReferenceExpression: i (OperationKind.LocalReferenceExpression, Type: System.Int32) (Syntax: 'i')
      IParameterReferenceExpression: d (OperationKind.ParameterReferenceExpression, Type: dynamic) (Syntax: 'd')
  ArgumentNames(2):
    ""null""
    ""c""
  ArgumentRefKinds(2):
    Ref
    None
";
            var expectedDiagnostics = new DiagnosticDescription[] {
                // CS0631: ref and out are not valid in this context
                //     public int this[ref int i, char c] => 0;
                Diagnostic(ErrorCode.ERR_IllegalRefParam, "ref").WithLocation(10, 21),
                // CS0631: ref and out are not valid in this context
                //     public int this[ref int i, long c] => 0;
                Diagnostic(ErrorCode.ERR_IllegalRefParam, "ref").WithLocation(11, 21)
            };

            VerifyOperationTreeAndDiagnosticsForTest<ElementAccessExpressionSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation)]
        [Fact]
        public void DynamicIndexerAccessExpression_ErrorBadDynamicMethodArgLambda()
        {
            string source = @"
using System;

class C
{
    public void M(C c)
    {
        dynamic y = null;
        var x = /*<bind>*/c[delegate { }, y]/*</bind>*/;
    }

    public int this[Action a, Action y] => 0;
}
";
            string expectedOperationTree = @"
IDynamicIndexerAccessExpression (OperationKind.DynamicIndexerAccessExpression, Type: dynamic, IsInvalid) (Syntax: 'c[delegate { }, y]')
  Expression: IParameterReferenceExpression: c (OperationKind.ParameterReferenceExpression, Type: C) (Syntax: 'c')
  ApplicableSymbols(1):
    Symbol: System.Int32 C.this[System.Action a, System.Action y] { get; }
  Arguments(2):
      IAnonymousFunctionExpression (Symbol: lambda expression) (OperationKind.AnonymousFunctionExpression, Type: null, IsInvalid) (Syntax: 'delegate { }')
        IBlockStatement (1 statements) (OperationKind.BlockStatement, IsInvalid) (Syntax: '{ }')
          IReturnStatement (OperationKind.ReturnStatement, IsInvalid) (Syntax: '{ }')
            ReturnedValue: null
      ILocalReferenceExpression: y (OperationKind.LocalReferenceExpression, Type: dynamic) (Syntax: 'y')
  ArgumentNames(0)
  ArgumentRefKinds(0)
";
            var expectedDiagnostics = new DiagnosticDescription[] {
                // CS1977: Cannot use a lambda expression as an argument to a dynamically dispatched operation without first casting it to a delegate or expression tree type.
                //         var x = /*<bind>*/c[delegate { }, y]/*</bind>*/;
                Diagnostic(ErrorCode.ERR_BadDynamicMethodArgLambda, "delegate { }").WithLocation(9, 29)
            };

            VerifyOperationTreeAndDiagnosticsForTest<ElementAccessExpressionSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation)]
        [Fact]
        public void DynamicIndexerAccessExpression_OverloadResolutionFailure()
        {
            string source = @"
using System;

class C
{
    void M(C c, dynamic d)
    {
        var x = /*<bind>*/c[d]/*</bind>*/;
    }

    public int this[int i, int j] => 0;
    public int this[int i, int j, int k] => 0;
}
";
            string expectedOperationTree = @"
IPropertyReferenceExpression: System.Int32 C.this { } (OperationKind.PropertyReferenceExpression, Type: System.Int32, IsInvalid) (Syntax: 'c[d]')
  Instance Receiver: IParameterReferenceExpression: c (OperationKind.ParameterReferenceExpression, Type: C, IsInvalid) (Syntax: 'c')
  Arguments(1):
      IArgument (ArgumentKind.Explicit, Matching Parameter: null) (OperationKind.Argument, IsInvalid) (Syntax: 'd')
        IParameterReferenceExpression: d (OperationKind.ParameterReferenceExpression, Type: dynamic, IsInvalid) (Syntax: 'd')
        InConversion: null
        OutConversion: null
";
            var expectedDiagnostics = new DiagnosticDescription[] {
                // CS1501: No overload for method 'this' takes 1 arguments
                //         var x = /*<bind>*/c[d]/*</bind>*/;
                Diagnostic(ErrorCode.ERR_BadArgCount, "c[d]").WithArguments("this", "1").WithLocation(8, 27)
            };

            VerifyOperationTreeAndDiagnosticsForTest<ElementAccessExpressionSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }
    }
}
