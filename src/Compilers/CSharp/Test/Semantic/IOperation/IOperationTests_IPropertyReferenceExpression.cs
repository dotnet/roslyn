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
        public void IPropertyReferenceExpression_PropertyReferenceInDerivedTypeUsesDerivedTypeAsInstanceType()
        {
            string source = @"
class C
{
    void M1()
    {
        C2 c2 = new C2() { /*<bind>*/P1 = 1/*</bind>*/ };
    }
}

class C1
{
    public virtual int P1 { get; set; }
}

class C2 : C1
{
}
";
            string expectedOperationTree = @"
IPropertyReferenceExpression: System.Int32 C1.P1 { get; } (OperationKind.PropertyReferenceExpression, Type: System.Int32) (Syntax: 'c2.P1')
  Instance Receiver: ILocalReferenceExpression: c2 (OperationKind.LocalReferenceExpression, Type: C2) (Syntax: 'c2')
";
            var expectedDiagnostics = DiagnosticDescription.None;

            VerifyOperationTreeAndDiagnosticsForTest<AssignmentExpressionSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }

    }
}
