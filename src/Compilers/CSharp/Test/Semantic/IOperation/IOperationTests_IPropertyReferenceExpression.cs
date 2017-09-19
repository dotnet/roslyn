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
        public void IPropertyReferenceExpression_PropertyReferenceInDerivedTypeUsesBaseTypeAsInstanceType()
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
ISimpleAssignmentExpression (OperationKind.SimpleAssignmentExpression, Type: System.Int32) (Syntax: 'P1 = 1')
  Left: IPropertyReferenceExpression: System.Int32 C1.P1 { get; set; } (OperationKind.PropertyReferenceExpression, Type: System.Int32) (Syntax: 'P1')
      Instance Receiver: IInstanceReferenceExpression (OperationKind.InstanceReferenceExpression, Type: C1) (Syntax: 'P1')
  Right: ILiteralExpression (OperationKind.LiteralExpression, Type: System.Int32, Constant: 1) (Syntax: '1')
";
            var expectedDiagnostics = DiagnosticDescription.None;

            VerifyOperationTreeAndDiagnosticsForTest<AssignmentExpressionSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }

    }
}
