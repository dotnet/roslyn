// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.CSharp.UnitTests
{
    public class IOperationTests_IInstanceReferenceTests : SemanticModelTestBase
    {
        [CompilerTrait(CompilerFeature.IOperation)]
        [Fact]
        public void IInstanceReferenceExpression_SimpleBaseReference()
        {
            string source = @"
using System;

public class C1
{
    public virtual void M1() { }
}

public class C2 : C1
{
    public override void M1()
    {
        /*<bind>*/base/*</bind>*/.M1();
    }
}
";
            string expectedOperationTree = @"
IInstanceReferenceOperation (ReferenceKind: ContainingTypeInstance) (OperationKind.InstanceReference, Type: C1) (Syntax: 'base')
";
            var expectedDiagnostics = DiagnosticDescription.None;

            VerifyOperationTreeAndDiagnosticsForTest<BaseExpressionSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation)]
        [Fact]
        public void IInstanceReferenceExpression_BaseNoMemberReference()
        {
            string source = @"
using System;

public class C1
{
    public virtual void M1()
    {
        /*<bind>*/base/*</bind>*/.M1();
    }
}
";
            string expectedOperationTree = @"
IInstanceReferenceOperation (ReferenceKind: ContainingTypeInstance) (OperationKind.InstanceReference, Type: System.Object) (Syntax: 'base')
";
            var expectedDiagnostics = new DiagnosticDescription[] {
                // CS0117: 'object' does not contain a definition for 'M1'
                //         /*<bind>*/base/*</bind>*/.M1();
                Diagnostic(ErrorCode.ERR_NoSuchMember, "M1").WithArguments("object", "M1").WithLocation(8, 35)
            };

            VerifyOperationTreeAndDiagnosticsForTest<BaseExpressionSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }

    }
}
