// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.CSharp.UnitTests
{
    public partial class IOperationTests : SemanticModelTestBase
    {
        [CompilerTrait(CompilerFeature.IOperation)]
        [Fact]
        public void DynamicInvocation_DynamicArgument()
        {
            string source = @"
class C
{
    void M(C c, dynamic d)
    {
        /*<bind>*/c.M2(d)/*</bind>*/;
    }

    public void M2(int i)
    {
    }
}
";
            string expectedOperationTree = @"
IDynamicInvocationExpression (OperationKind.DynamicInvocationExpression, Type: dynamic) (Syntax: 'c.M2(d)')
  Expression: IOperation:  (OperationKind.None) (Syntax: 'c.M2')
  ApplicableSymbols(1):
    Symbol: void C.M2(System.Int32 i)
  Arguments(1):
      IParameterReferenceExpression: d (OperationKind.ParameterReferenceExpression, Type: dynamic) (Syntax: 'd')
  ArgumentNames(0)
  ArgumentRefKinds(0)
";
            var expectedDiagnostics = DiagnosticDescription.None;

            VerifyOperationTreeAndDiagnosticsForTest<InvocationExpressionSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation)]
        [Fact]
        public void DynamicInvocation_MultipleApplicableSymbols()
        {
            string source = @"
class C
{
    void M(C c, dynamic d)
    {
        var x = /*<bind>*/c.M2(d)/*</bind>*/;
    }

    public void M2(int i)
    {
    }

    public void M2(long i)
    {
    }
}
";
            string expectedOperationTree = @"
IDynamicInvocationExpression (OperationKind.DynamicInvocationExpression, Type: dynamic) (Syntax: 'c.M2(d)')
  Expression: IOperation:  (OperationKind.None) (Syntax: 'c.M2')
  ApplicableSymbols(2):
    Symbol: void C.M2(System.Int32 i)
    Symbol: void C.M2(System.Int64 i)
  Arguments(1):
      IParameterReferenceExpression: d (OperationKind.ParameterReferenceExpression, Type: dynamic) (Syntax: 'd')
  ArgumentNames(0)
  ArgumentRefKinds(0)
";
            var expectedDiagnostics = DiagnosticDescription.None;

            VerifyOperationTreeAndDiagnosticsForTest<InvocationExpressionSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation)]
        [Fact]
        public void DynamicInvocation_MultipleArgumentsAndApplicableSymbols()
        {
            string source = @"
class C
{
    void M(C c, dynamic d)
    {
        char ch = 'c';
        var x = /*<bind>*/c.M2(d, ch)/*</bind>*/;
    }

    public void M2(int i, char ch)
    {
    }

    public void M2(long i, char ch)
    {
    }
}
";
            string expectedOperationTree = @"
IDynamicInvocationExpression (OperationKind.DynamicInvocationExpression, Type: dynamic) (Syntax: 'c.M2(d, ch)')
  Expression: IOperation:  (OperationKind.None) (Syntax: 'c.M2')
  ApplicableSymbols(2):
    Symbol: void C.M2(System.Int32 i, System.Char ch)
    Symbol: void C.M2(System.Int64 i, System.Char ch)
  Arguments(2):
      IParameterReferenceExpression: d (OperationKind.ParameterReferenceExpression, Type: dynamic) (Syntax: 'd')
      ILocalReferenceExpression: ch (OperationKind.LocalReferenceExpression, Type: System.Char) (Syntax: 'ch')
  ArgumentNames(0)
  ArgumentRefKinds(0)
";
            var expectedDiagnostics = DiagnosticDescription.None;

            VerifyOperationTreeAndDiagnosticsForTest<InvocationExpressionSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation)]
        [Fact]
        public void DynamicInvocation_ArgumentNames()
        {
            string source = @"
class C
{
    void M(C c, dynamic d, dynamic e)
    {
        var x = /*<bind>*/c.M2(i: d, ch: e)/*</bind>*/;
    }

    public void M2(int i, char ch)
    {
    }

    public void M2(long i, char ch)
    {
    }
}
";
            string expectedOperationTree = @"
IDynamicInvocationExpression (OperationKind.DynamicInvocationExpression, Type: dynamic) (Syntax: 'c.M2(i: d, ch: e)')
  Expression: IOperation:  (OperationKind.None) (Syntax: 'c.M2')
  ApplicableSymbols(2):
    Symbol: void C.M2(System.Int32 i, System.Char ch)
    Symbol: void C.M2(System.Int64 i, System.Char ch)
  Arguments(2):
      IParameterReferenceExpression: d (OperationKind.ParameterReferenceExpression, Type: dynamic) (Syntax: 'd')
      IParameterReferenceExpression: e (OperationKind.ParameterReferenceExpression, Type: dynamic) (Syntax: 'e')
  ArgumentNames(2):
    ""i""
    ""ch""
  ArgumentRefKinds(0)
";
            var expectedDiagnostics = DiagnosticDescription.None;

            VerifyOperationTreeAndDiagnosticsForTest<InvocationExpressionSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation)]
        [Fact]
        public void DynamicInvocation_ArgumentRefKinds()
        {
            string source = @"
class C
{
    void M(C c, object d, dynamic e)
    {
        int k;
        var x = /*<bind>*/c.M2(ref d, out k, e)/*</bind>*/;
    }

    public void M2(ref object i, out int j, char c)
    {
        j = 0;
    }
}
";
            string expectedOperationTree = @"
IDynamicInvocationExpression (OperationKind.DynamicInvocationExpression, Type: dynamic) (Syntax: 'c.M2(ref d, out k, e)')
  Expression: IOperation:  (OperationKind.None) (Syntax: 'c.M2')
  ApplicableSymbols(1):
    Symbol: void C.M2(ref System.Object i, out System.Int32 j, System.Char c)
  Arguments(3):
      IParameterReferenceExpression: d (OperationKind.ParameterReferenceExpression, Type: System.Object) (Syntax: 'd')
      ILocalReferenceExpression: k (OperationKind.LocalReferenceExpression, Type: System.Int32) (Syntax: 'k')
      IParameterReferenceExpression: e (OperationKind.ParameterReferenceExpression, Type: dynamic) (Syntax: 'e')
  ArgumentNames(0)
  ArgumentRefKinds(3):
    Ref
    Out
    None
";
            var expectedDiagnostics = DiagnosticDescription.None;

            VerifyOperationTreeAndDiagnosticsForTest<InvocationExpressionSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation)]
        [Fact]
        public void DynamicInvocation_DelegateInvocation()
        {
            string source = @"
using System;

class C
{
    public Action<object> F;
    void M(dynamic i)
    {
        var x = /*<bind>*/F(i)/*</bind>*/;
    }
}
";
            string expectedOperationTree = @"
IDynamicInvocationExpression (OperationKind.DynamicInvocationExpression, Type: dynamic) (Syntax: 'F(i)')
  Expression: IFieldReferenceExpression: System.Action<System.Object> C.F (OperationKind.FieldReferenceExpression, Type: System.Action<System.Object>) (Syntax: 'F')
      Instance Receiver: IInstanceReferenceExpression (OperationKind.InstanceReferenceExpression, Type: C) (Syntax: 'F')
  ApplicableSymbols(1):
    Symbol: void System.Action<System.Object>.Invoke(System.Object obj)
  Arguments(1):
      IParameterReferenceExpression: i (OperationKind.ParameterReferenceExpression, Type: dynamic) (Syntax: 'i')
  ArgumentNames(0)
  ArgumentRefKinds(0)
";
            var expectedDiagnostics = new DiagnosticDescription[] {
                // CS0649: Field 'C.F' is never assigned to, and will always have its default value null
                //     public Action<object> F;
                Diagnostic(ErrorCode.WRN_UnassignedInternalField, "F").WithArguments("C.F", "null").WithLocation(6, 27)
            };

            VerifyOperationTreeAndDiagnosticsForTest<InvocationExpressionSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation)]
        [Fact]
        public void DynamicInvocation_WithDynamicReceiver()
        {
            string source = @"
class C
{
    void M(dynamic d, int i)
    {
        var x = /*<bind>*/d(i)/*</bind>*/;
    }
}
";
            string expectedOperationTree = @"
IDynamicInvocationExpression (OperationKind.DynamicInvocationExpression, Type: dynamic) (Syntax: 'd(i)')
  Expression: IParameterReferenceExpression: d (OperationKind.ParameterReferenceExpression, Type: dynamic) (Syntax: 'd')
  ApplicableSymbols(0)
  Arguments(1):
      IParameterReferenceExpression: i (OperationKind.ParameterReferenceExpression, Type: System.Int32) (Syntax: 'i')
  ArgumentNames(0)
  ArgumentRefKinds(0)
";
            var expectedDiagnostics = DiagnosticDescription.None;

            VerifyOperationTreeAndDiagnosticsForTest<InvocationExpressionSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation)]
        [Fact]
        public void DynamicInvocation_WithDynamicMemberReceiver()
        {
            string source = @"
class C
{
    void M(dynamic c, int i)
    {
        var x = /*<bind>*/c.M2(i)/*</bind>*/;
    }
}
";
            string expectedOperationTree = @"
IDynamicInvocationExpression (OperationKind.DynamicInvocationExpression, Type: dynamic) (Syntax: 'c.M2(i)')
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

            VerifyOperationTreeAndDiagnosticsForTest<InvocationExpressionSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation)]
        [Fact]
        public void DynamicInvocation_WithDynamicTypedMemberReceiver()
        {
            string source = @"
class C
{
    dynamic M2 = null;
    void M(C c, int i)
    {
        var x = /*<bind>*/c.M2(i)/*</bind>*/;
    }
}
";
            string expectedOperationTree = @"
IDynamicInvocationExpression (OperationKind.DynamicInvocationExpression, Type: dynamic) (Syntax: 'c.M2(i)')
  Expression: IFieldReferenceExpression: dynamic C.M2 (OperationKind.FieldReferenceExpression, Type: dynamic) (Syntax: 'c.M2')
      Instance Receiver: IParameterReferenceExpression: c (OperationKind.ParameterReferenceExpression, Type: C) (Syntax: 'c')
  ApplicableSymbols(0)
  Arguments(1):
      IParameterReferenceExpression: i (OperationKind.ParameterReferenceExpression, Type: System.Int32) (Syntax: 'i')
  ArgumentNames(0)
  ArgumentRefKinds(0)
";
            var expectedDiagnostics = DiagnosticDescription.None;

            VerifyOperationTreeAndDiagnosticsForTest<InvocationExpressionSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation)]
        [Fact]
        public void DynamicInvocation_AllFields()
        {
            string source = @"
class C
{
    void M(C c, dynamic d)
    {
        int i = 0;
        var x = /*<bind>*/c.M2(ref i, c: d)/*</bind>*/;
    }

    public void M2(ref int i, char c)
    {
    }

    public void M2(ref int i, long c)
    {
    }
}
";
            string expectedOperationTree = @"
IDynamicInvocationExpression (OperationKind.DynamicInvocationExpression, Type: dynamic) (Syntax: 'c.M2(ref i, c: d)')
  Expression: IOperation:  (OperationKind.None) (Syntax: 'c.M2')
  ApplicableSymbols(2):
    Symbol: void C.M2(ref System.Int32 i, System.Char c)
    Symbol: void C.M2(ref System.Int32 i, System.Int64 c)
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
            var expectedDiagnostics = DiagnosticDescription.None;

            VerifyOperationTreeAndDiagnosticsForTest<InvocationExpressionSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation)]
        [Fact]
        public void DynamicInvocation_ErrorBadDynamicMethodArgLambda()
        {
            string source = @"
using System;

class C
{
    public void M(C c)
    {
        dynamic y = null;
        var x = /*<bind>*/c.M2(delegate { }, y)/*</bind>*/;
    }

    public void M2(Action a, Action y)
    {
    }
}
";
            string expectedOperationTree = @"
IDynamicInvocationExpression (OperationKind.DynamicInvocationExpression, Type: dynamic, IsInvalid) (Syntax: 'c.M2(delegate { }, y)')
  Expression: IOperation:  (OperationKind.None) (Syntax: 'c.M2')
  ApplicableSymbols(1):
    Symbol: void C.M2(System.Action a, System.Action y)
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
                //         var x = /*<bind>*/c.M2(delegate { }, y)/*</bind>*/;
                Diagnostic(ErrorCode.ERR_BadDynamicMethodArgLambda, "delegate { }").WithLocation(9, 32)
            };

            VerifyOperationTreeAndDiagnosticsForTest<InvocationExpressionSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation)]
        [Fact]
        public void DynamicInvocation_OverloadResolutionFailure()
        {
            string source = @"
class C
{
    void M(C c, dynamic d)
    {
        var x = /*<bind>*/c.M2(d)/*</bind>*/;
    }

    public void M2()
    {
    }

    public void M2(int i, int j)
    {
    }
}
";
            string expectedOperationTree = @"
IInvocationExpression ( void C.M2()) (OperationKind.InvocationExpression, Type: System.Void, IsInvalid) (Syntax: 'c.M2(d)')
  Instance Receiver: IParameterReferenceExpression: c (OperationKind.ParameterReferenceExpression, Type: C, IsInvalid) (Syntax: 'c')
  Arguments(1):
      IArgument (ArgumentKind.Explicit, Matching Parameter: null) (OperationKind.Argument, IsInvalid) (Syntax: 'd')
        IParameterReferenceExpression: d (OperationKind.ParameterReferenceExpression, Type: dynamic, IsInvalid) (Syntax: 'd')
        InConversion: null
        OutConversion: null
";
            var expectedDiagnostics = new DiagnosticDescription[] {
                // CS7036: There is no argument given that corresponds to the required formal parameter 'j' of 'C.M2(int, int)'
                //         var x = /*<bind>*/c.M2(d)/*</bind>*/;
                Diagnostic(ErrorCode.ERR_NoCorrespondingArgument, "M2").WithArguments("j", "C.M2(int, int)").WithLocation(6, 29),
                // CS0815: Cannot assign void to an implicitly-typed variable
                //         var x = /*<bind>*/c.M2(d)/*</bind>*/;
                Diagnostic(ErrorCode.ERR_ImplicitlyTypedVariableAssignedBadValue, "x = /*<bind>*/c.M2(d)").WithArguments("void").WithLocation(6, 13)
            };

            VerifyOperationTreeAndDiagnosticsForTest<InvocationExpressionSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }
    }
}
