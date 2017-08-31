// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Diagnostics;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Semantics;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.CSharp.UnitTests
{
    public partial class IOperationTests : SemanticModelTestBase
    {
        [Fact]
        public void IConditionalGotoStatement_FromIf()
        {
            string source = @"
class C
{
    /*<bind>*/
    static void Method(int p)
    {
        if (p < 0) p = 0;
    }
    /*</bind>*/
}
";
            string expectedOperationTree = @"
IBlockStatement (1 statements) (OperationKind.BlockStatement) (Syntax: '{ ... }')
  IBlockStatement (3 statements) (OperationKind.BlockStatement) (Syntax: 'if (p < 0) p = 0;')
    IConditionalGotoStatement (JumpIfTrue: False, Target: label_0) (OperationKind.ConditionalGotoStatement) (Syntax: 'p < 0')
      Condition: IBinaryOperatorExpression (BinaryOperatorKind.LessThan) (OperationKind.BinaryOperatorExpression, Type: System.Boolean) (Syntax: 'p < 0')
          Left: IParameterReferenceExpression: p (OperationKind.ParameterReferenceExpression, Type: System.Int32) (Syntax: 'p')
          Right: ILiteralExpression (OperationKind.LiteralExpression, Type: System.Int32, Constant: 0) (Syntax: '0')
    IExpressionStatement (OperationKind.ExpressionStatement) (Syntax: 'p = 0;')
      Expression: ISimpleAssignmentExpression (OperationKind.SimpleAssignmentExpression, Type: System.Int32) (Syntax: 'p = 0')
          Left: IParameterReferenceExpression: p (OperationKind.ParameterReferenceExpression, Type: System.Int32) (Syntax: 'p')
          Right: ILiteralExpression (OperationKind.LiteralExpression, Type: System.Int32, Constant: 0) (Syntax: '0')
    ILabeledStatement (Label: label_0) (OperationKind.LabeledStatement) (Syntax: 'if (p < 0) p = 0;')
      Statement: null
";
            var expectedDiagnostics = DiagnosticDescription.None;
            VerifyOperationTreeAndDiagnosticsForTest<BaseMethodDeclarationSyntax>(source, expectedOperationTree, expectedDiagnostics, useLoweredTree: true);
        }

        [Fact]
        public void IConditionalGotoStatement_FromIfElse()
        {
            string source = @"
class C
{
    /*<bind>*/
    static void Method(int p)
    {
        if (p < 0) p = 0;
        else p = 1;
    }
    /*</bind>*/
}
";
            string expectedOperationTree = @"
IBlockStatement (1 statements) (OperationKind.BlockStatement) (Syntax: '{ ... }')
  IBlockStatement (6 statements) (OperationKind.BlockStatement) (Syntax: 'if (p < 0)  ... else p = 1;')
    IConditionalGotoStatement (JumpIfTrue: False, Target: label_0) (OperationKind.ConditionalGotoStatement) (Syntax: 'p < 0')
      Condition: IBinaryOperatorExpression (BinaryOperatorKind.LessThan) (OperationKind.BinaryOperatorExpression, Type: System.Boolean) (Syntax: 'p < 0')
          Left: IParameterReferenceExpression: p (OperationKind.ParameterReferenceExpression, Type: System.Int32) (Syntax: 'p')
          Right: ILiteralExpression (OperationKind.LiteralExpression, Type: System.Int32, Constant: 0) (Syntax: '0')
    IExpressionStatement (OperationKind.ExpressionStatement) (Syntax: 'p = 0;')
      Expression: ISimpleAssignmentExpression (OperationKind.SimpleAssignmentExpression, Type: System.Int32) (Syntax: 'p = 0')
          Left: IParameterReferenceExpression: p (OperationKind.ParameterReferenceExpression, Type: System.Int32) (Syntax: 'p')
          Right: ILiteralExpression (OperationKind.LiteralExpression, Type: System.Int32, Constant: 0) (Syntax: '0')
    IBranchStatement (BranchKind.GoTo, Label: label_1) (OperationKind.BranchStatement) (Syntax: 'if (p < 0)  ... else p = 1;')
    ILabeledStatement (Label: label_0) (OperationKind.LabeledStatement) (Syntax: 'if (p < 0)  ... else p = 1;')
      Statement: null
    IExpressionStatement (OperationKind.ExpressionStatement) (Syntax: 'p = 1;')
      Expression: ISimpleAssignmentExpression (OperationKind.SimpleAssignmentExpression, Type: System.Int32) (Syntax: 'p = 1')
          Left: IParameterReferenceExpression: p (OperationKind.ParameterReferenceExpression, Type: System.Int32) (Syntax: 'p')
          Right: ILiteralExpression (OperationKind.LiteralExpression, Type: System.Int32, Constant: 1) (Syntax: '1')
    ILabeledStatement (Label: label_1) (OperationKind.LabeledStatement) (Syntax: 'if (p < 0)  ... else p = 1;')
      Statement: null
";
            var expectedDiagnostics = DiagnosticDescription.None;
            VerifyOperationTreeAndDiagnosticsForTest<BaseMethodDeclarationSyntax>(source, expectedOperationTree, expectedDiagnostics, useLoweredTree: true);
        }
    }
}
