// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.CSharp.Test.Utilities;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.CSharp.UnitTests
{
    public partial class IOperationTests : SemanticModelTestBase
    {
        [CompilerTrait(CompilerFeature.IOperation)]
        [Fact]
        public void ConditionalExpression_01()
        {
            string source = @"
class P
{
    private void M()
    {
        int i = 0;
        int j = 2;
        var z = (/*<bind>*/true ? i : j/*</bind>*/);
    }
}
";
            string expectedOperationTree = @"
IConditionalOperation (OperationKind.Conditional, Type: System.Int32) (Syntax: 'true ? i : j')
  Condition: 
    ILiteralOperation (OperationKind.Literal, Type: System.Boolean, Constant: True) (Syntax: 'true')
  WhenTrue: 
    ILocalReferenceOperation: i (OperationKind.LocalReference, Type: System.Int32) (Syntax: 'i')
  WhenFalse: 
    ILocalReferenceOperation: j (OperationKind.LocalReference, Type: System.Int32) (Syntax: 'j')
";
            var expectedDiagnostics = DiagnosticDescription.None;

            VerifyOperationTreeAndDiagnosticsForTest<ConditionalExpressionSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation)]
        [Fact]
        public void ConditionalExpression_02()
        {
            string source = @"
class P
{
    private void M()
    {
        int i = 0;
        int j = 2;
        (/*<bind>*/true ? ref i : ref j/*</bind>*/) = 4;
    }
}
";
            string expectedOperationTree = @"
IConditionalOperation (IsRef) (OperationKind.Conditional, Type: System.Int32) (Syntax: 'true ? ref i : ref j')
  Condition: 
    ILiteralOperation (OperationKind.Literal, Type: System.Boolean, Constant: True) (Syntax: 'true')
  WhenTrue: 
    ILocalReferenceOperation: i (OperationKind.LocalReference, Type: System.Int32) (Syntax: 'i')
  WhenFalse: 
    ILocalReferenceOperation: j (OperationKind.LocalReference, Type: System.Int32) (Syntax: 'j')
";
            var expectedDiagnostics = DiagnosticDescription.None;

            VerifyOperationTreeAndDiagnosticsForTest<ConditionalExpressionSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }
    }
}
