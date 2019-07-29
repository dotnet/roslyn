// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.CSharp.UnitTests
{
    [CompilerTrait(CompilerFeature.IOperation)]
    public partial class IOperationTests : SemanticModelTestBase
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
