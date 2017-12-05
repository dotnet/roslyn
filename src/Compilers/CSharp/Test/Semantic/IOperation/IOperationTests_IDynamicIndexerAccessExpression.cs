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
IDynamicIndexerAccessOperation (OperationKind.DynamicIndexerAccess, Type: dynamic) (Syntax: 'c[d]')
  Expression: 
    IParameterReferenceOperation: c (OperationKind.ParameterReference, Type: C) (Syntax: 'c')
  Arguments(1):
      IParameterReferenceOperation: d (OperationKind.ParameterReference, Type: dynamic) (Syntax: 'd')
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
IDynamicIndexerAccessOperation (OperationKind.DynamicIndexerAccess, Type: dynamic) (Syntax: 'c[d]')
  Expression: 
    IParameterReferenceOperation: c (OperationKind.ParameterReference, Type: C) (Syntax: 'c')
  Arguments(1):
      IParameterReferenceOperation: d (OperationKind.ParameterReference, Type: dynamic) (Syntax: 'd')
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
IDynamicIndexerAccessOperation (OperationKind.DynamicIndexerAccess, Type: dynamic) (Syntax: 'c[d, ch]')
  Expression: 
    IParameterReferenceOperation: c (OperationKind.ParameterReference, Type: C) (Syntax: 'c')
  Arguments(2):
      IParameterReferenceOperation: d (OperationKind.ParameterReference, Type: dynamic) (Syntax: 'd')
      ILocalReferenceOperation: ch (OperationKind.LocalReference, Type: System.Char) (Syntax: 'ch')
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
IDynamicIndexerAccessOperation (OperationKind.DynamicIndexerAccess, Type: dynamic) (Syntax: 'c[i: d, ch: e]')
  Expression: 
    IParameterReferenceOperation: c (OperationKind.ParameterReference, Type: C) (Syntax: 'c')
  Arguments(2):
      IParameterReferenceOperation: d (OperationKind.ParameterReference, Type: dynamic) (Syntax: 'd')
      IParameterReferenceOperation: e (OperationKind.ParameterReference, Type: dynamic) (Syntax: 'e')
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
IDynamicIndexerAccessOperation (OperationKind.DynamicIndexerAccess, Type: dynamic) (Syntax: 'c[i: d, ch: ref e]')
  Expression: 
    IParameterReferenceOperation: c (OperationKind.ParameterReference, Type: C) (Syntax: 'c')
  Arguments(2):
      IParameterReferenceOperation: d (OperationKind.ParameterReference, Type: dynamic) (Syntax: 'd')
      IParameterReferenceOperation: e (OperationKind.ParameterReference, Type: dynamic) (Syntax: 'e')
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
}
";
            string expectedOperationTree = @"
IDynamicIndexerAccessOperation (OperationKind.DynamicIndexerAccess, Type: dynamic) (Syntax: 'd[i]')
  Expression: 
    IParameterReferenceOperation: d (OperationKind.ParameterReference, Type: dynamic) (Syntax: 'd')
  Arguments(1):
      IParameterReferenceOperation: i (OperationKind.ParameterReference, Type: System.Int32) (Syntax: 'i')
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
}
";
            string expectedOperationTree = @"
IDynamicIndexerAccessOperation (OperationKind.DynamicIndexerAccess, Type: dynamic) (Syntax: 'c.M2[i]')
  Expression: 
    IDynamicMemberReferenceOperation (Member Name: ""M2"", Containing Type: null) (OperationKind.DynamicMemberReference, Type: dynamic) (Syntax: 'c.M2')
      Type Arguments(0)
      Instance Receiver: 
        IParameterReferenceOperation: c (OperationKind.ParameterReference, Type: dynamic) (Syntax: 'c')
  Arguments(1):
      IParameterReferenceOperation: i (OperationKind.ParameterReference, Type: System.Int32) (Syntax: 'i')
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

    void M(C c, int i)
    {
        var x = /*<bind>*/c.M2[i]/*</bind>*/;
    }
}
";
            string expectedOperationTree = @"
IDynamicIndexerAccessOperation (OperationKind.DynamicIndexerAccess, Type: dynamic) (Syntax: 'c.M2[i]')
  Expression: 
    IFieldReferenceOperation: dynamic C.M2 (OperationKind.FieldReference, Type: dynamic) (Syntax: 'c.M2')
      Instance Receiver: 
        IParameterReferenceOperation: c (OperationKind.ParameterReference, Type: C) (Syntax: 'c')
  Arguments(1):
      IParameterReferenceOperation: i (OperationKind.ParameterReference, Type: System.Int32) (Syntax: 'i')
  ArgumentNames(0)
  ArgumentRefKinds(0)
";
            var expectedDiagnostics = DiagnosticDescription.None;

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
IDynamicIndexerAccessOperation (OperationKind.DynamicIndexerAccess, Type: dynamic) (Syntax: 'c[ref i, c: d]')
  Expression: 
    IParameterReferenceOperation: c (OperationKind.ParameterReference, Type: C) (Syntax: 'c')
  Arguments(2):
      ILocalReferenceOperation: i (OperationKind.LocalReference, Type: System.Int32) (Syntax: 'i')
      IParameterReferenceOperation: d (OperationKind.ParameterReference, Type: dynamic) (Syntax: 'd')
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
IDynamicIndexerAccessOperation (OperationKind.DynamicIndexerAccess, Type: dynamic, IsInvalid) (Syntax: 'c[delegate { }, y]')
  Expression: 
    IParameterReferenceOperation: c (OperationKind.ParameterReference, Type: C) (Syntax: 'c')
  Arguments(2):
      IAnonymousFunctionOperation (Symbol: lambda expression) (OperationKind.AnonymousFunction, Type: null, IsInvalid) (Syntax: 'delegate { }')
        IBlockOperation (1 statements) (OperationKind.Block, Type: null, IsInvalid) (Syntax: '{ }')
          IReturnOperation (OperationKind.Return, Type: null, IsInvalid, IsImplicit) (Syntax: '{ }')
            ReturnedValue: 
              null
      ILocalReferenceOperation: y (OperationKind.LocalReference, Type: dynamic) (Syntax: 'y')
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
IInvalidOperation (OperationKind.Invalid, Type: System.Int32, IsInvalid) (Syntax: 'c[d]')
  Children(2):
      IParameterReferenceOperation: c (OperationKind.ParameterReference, Type: C, IsInvalid) (Syntax: 'c')
      IParameterReferenceOperation: d (OperationKind.ParameterReference, Type: dynamic, IsInvalid) (Syntax: 'd')
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
