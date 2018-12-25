// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.CSharp.UnitTests
{
    public partial class IOperationTests_Patterns : SemanticModelTestBase
    {
        [CompilerTrait(CompilerFeature.IOperation, CompilerFeature.Patterns)]
        [Fact]
        public void SwitchExpression_Basic()
        {
            string source = @"
using System;
class X
{
    void M(int? x, object y)
    {
        y = /*<bind>*/x switch { 1 => 2, 3 => 4, _ => 5 }/*</bind>*/;
    }
}
";
            string expectedOperationTree = @"
ISwitchExpressionOperation (3 arms)
  Value: 
    IParameterReferenceOperation: x (OperationKind.ParameterReference, Type: System.Int32?) (Syntax: 'x')
  Arms(3):
      ISwitchExpressionArmOperation (0 locals)
        Pattern: 
          IConstantPatternOperation (OperationKind.ConstantPattern, Type: null) (Syntax: '1')
            Value: 
              ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 1) (Syntax: '1')
        Value: 
          ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 2) (Syntax: '2')
      ISwitchExpressionArmOperation (0 locals)
        Pattern: 
          IConstantPatternOperation (OperationKind.ConstantPattern, Type: null) (Syntax: '3')
            Value: 
              ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 3) (Syntax: '3')
        Value: 
          ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 4) (Syntax: '4')
      ISwitchExpressionArmOperation (0 locals)
        Pattern: 
          IDiscardPatternOperation () (OperationKind.DiscardPattern, Type: null) (Syntax: '_')
        Value: 
          ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 5) (Syntax: '5')
";
            var expectedDiagnostics = DiagnosticDescription.None;

            VerifyOperationTreeAndDiagnosticsForTest<SwitchExpressionSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }
    }
}
