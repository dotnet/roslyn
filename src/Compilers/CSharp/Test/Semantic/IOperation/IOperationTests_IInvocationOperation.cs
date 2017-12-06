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
        public void IInvocation_StaticMethodWithInstanceReceiver()
        {
            string source = @"
class C
{
    static void M1() { }

    public static void M2()
    {
        var c = new C();
        /*<bind>*/c.M1()/*</bind>*/;
    }
}
";
            string expectedOperationTree = @"
IInvocationOperation ( void C.M1()) (OperationKind.Invocation, Type: System.Void, IsInvalid) (Syntax: 'c.M1()')
  Instance Receiver: 
    ILocalReferenceOperation: c (OperationKind.LocalReference, Type: C, IsInvalid) (Syntax: 'c')
  Arguments(0)
";
            var expectedDiagnostics = new DiagnosticDescription[] {
                // CS0176: Member 'C.M1()' cannot be accessed with an instance reference; qualify it with a type name instead
                //         /*<bind>*/c.M1()/*</bind>*/;
                Diagnostic(ErrorCode.ERR_ObjectProhibited, "c.M1").WithArguments("C.M1()").WithLocation(9, 19)
            };

            VerifyOperationTreeAndDiagnosticsForTest<InvocationExpressionSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation)]
        [Fact]
        public void IInvocation_StaticMethodAccessOnType()
        {
            string source = @"
class C
{
    static void M1() { }

    public static void M2()
    {
        /*<bind>*/C.M1()/*</bind>*/;
    }
}
";
            string expectedOperationTree = @"
IInvocationOperation (void C.M1()) (OperationKind.Invocation, Type: System.Void) (Syntax: 'C.M1()')
  Instance Receiver: 
    null
  Arguments(0)
";
            var expectedDiagnostics = DiagnosticDescription.None;

            VerifyOperationTreeAndDiagnosticsForTest<InvocationExpressionSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation)]
        [Fact]
        public void IInvocation_InstanceMethodAccessOnType()
        {
            string source = @"
class C
{
    void M1() { }

    public static void M2()
    {
        /*<bind>*/C.M1()/*</bind>*/;
    }
}
";
            string expectedOperationTree = @"
IInvocationOperation (void C.M1()) (OperationKind.Invocation, Type: System.Void, IsInvalid) (Syntax: 'C.M1()')
  Instance Receiver: 
    null
  Arguments(0)
";
            var expectedDiagnostics = new DiagnosticDescription[] {
                // CS0120: An object reference is required for the non-static field, method, or property 'C.M1()'
                //         /*<bind>*/C.M1()/*</bind>*/;
                Diagnostic(ErrorCode.ERR_ObjectRequired, "C.M1").WithArguments("C.M1()").WithLocation(8, 19)
            };

            VerifyOperationTreeAndDiagnosticsForTest<InvocationExpressionSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }
    }
}
