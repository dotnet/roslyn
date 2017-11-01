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
        public void IInvocation_StaticMethodWithInstanceReciever()
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
    }
}
