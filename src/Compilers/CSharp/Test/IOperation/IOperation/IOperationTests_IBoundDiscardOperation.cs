// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.CSharp.UnitTests
{
    [CompilerTrait(CompilerFeature.IOperation)]
    public class IOperationTests_IBoundDiscardOperation : SemanticModelTestBase
    {
        [Fact]
        public void DiscardExpression_AsAssignment()
        {
            string source = @"
class C
{
    int P { get; }

    void M()
    {
        /*<bind>*/_/*</bind>*/ = P;
    }
}
";
            string expectedOperationTree = @"
IDiscardOperation (Symbol: System.Int32 _) (OperationKind.Discard, Type: System.Int32) (Syntax: '_')
";
            var expectedDiagnostics = DiagnosticDescription.None;

            VerifyOperationTreeAndDiagnosticsForTest<IdentifierNameSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }

        [Fact]
        public void DiscardExpression_AsAssignment_EntireStatement()
        {
            string source = @"
class C
{
    int P { get; }

    void M()
    {
        /*<bind>*/_ = P;/*</bind>*/
    }
}
";
            string expectedOperationTree = @"
IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: '_ = P;')
  Expression: 
    ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Int32) (Syntax: '_ = P')
      Left: 
        IDiscardOperation (Symbol: System.Int32 _) (OperationKind.Discard, Type: System.Int32) (Syntax: '_')
      Right: 
        IPropertyReferenceOperation: System.Int32 C.P { get; } (OperationKind.PropertyReference, Type: System.Int32) (Syntax: 'P')
          Instance Receiver: 
            IInstanceReferenceOperation (ReferenceKind: ContainingTypeInstance) (OperationKind.InstanceReference, Type: C, IsImplicit) (Syntax: 'P')
";
            var expectedDiagnostics = DiagnosticDescription.None;

            VerifyOperationTreeAndDiagnosticsForTest<ExpressionStatementSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }
    }
}
