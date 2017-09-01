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
        [CompilerTrait(CompilerFeature.IOperation)]
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

        [CompilerTrait(CompilerFeature.IOperation)]
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

        [CompilerTrait(CompilerFeature.IOperation)]
        [Fact]
        public void IConditionalGotoStatement_FromWhile()
        {
            string source = @"
class C
{
    /*<bind>*/
    static void Method(int p)
    {
        while (p > 0) p = 0;
    }
    /*</bind>*/
}
";
            string expectedOperationTree = @"
IBlockStatement (1 statements) (OperationKind.BlockStatement) (Syntax: '{ ... }')
  IBlockStatement (6 statements) (OperationKind.BlockStatement) (Syntax: 'while (p > 0) p = 0;')
    IBranchStatement (BranchKind.GoTo, Label: label_0) (OperationKind.BranchStatement) (Syntax: 'while (p > 0) p = 0;')
    ILabeledStatement (Label: label_1) (OperationKind.LabeledStatement) (Syntax: 'while (p > 0) p = 0;')
      Statement: null
    IExpressionStatement (OperationKind.ExpressionStatement) (Syntax: 'p = 0;')
      Expression: ISimpleAssignmentExpression (OperationKind.SimpleAssignmentExpression, Type: System.Int32) (Syntax: 'p = 0')
          Left: IParameterReferenceExpression: p (OperationKind.ParameterReferenceExpression, Type: System.Int32) (Syntax: 'p')
          Right: ILiteralExpression (OperationKind.LiteralExpression, Type: System.Int32, Constant: 0) (Syntax: '0')
    ILabeledStatement (Label: label_0) (OperationKind.LabeledStatement) (Syntax: 'while (p > 0) p = 0;')
      Statement: null
    IConditionalGotoStatement (JumpIfTrue: True, Target: label_1) (OperationKind.ConditionalGotoStatement) (Syntax: 'p > 0')
      Condition: IBinaryOperatorExpression (BinaryOperatorKind.GreaterThan) (OperationKind.BinaryOperatorExpression, Type: System.Boolean) (Syntax: 'p > 0')
          Left: IParameterReferenceExpression: p (OperationKind.ParameterReferenceExpression, Type: System.Int32) (Syntax: 'p')
          Right: ILiteralExpression (OperationKind.LiteralExpression, Type: System.Int32, Constant: 0) (Syntax: '0')
    ILabeledStatement (Label: label_2) (OperationKind.LabeledStatement) (Syntax: 'while (p > 0) p = 0;')
      Statement: null
";
            var expectedDiagnostics = DiagnosticDescription.None;
            VerifyOperationTreeAndDiagnosticsForTest<BaseMethodDeclarationSyntax>(source, expectedOperationTree, expectedDiagnostics, useLoweredTree: true);
        }

        [CompilerTrait(CompilerFeature.IOperation)]
        [Fact]
        public void IConditionalGotoStatement_FromDoWhile()
        {
            string source = @"
class C
{
    /*<bind>*/
    static void Method(int p)
    {
        do p = 0; while (p > 0);
    }
    /*</bind>*/
}
";
            string expectedOperationTree = @"
IBlockStatement (1 statements) (OperationKind.BlockStatement) (Syntax: '{ ... }')
  IBlockStatement (5 statements) (OperationKind.BlockStatement) (Syntax: 'do p = 0; while (p > 0);')
    ILabeledStatement (Label: label_0) (OperationKind.LabeledStatement) (Syntax: 'do p = 0; while (p > 0);')
      Statement: null
    IExpressionStatement (OperationKind.ExpressionStatement) (Syntax: 'p = 0;')
      Expression: ISimpleAssignmentExpression (OperationKind.SimpleAssignmentExpression, Type: System.Int32) (Syntax: 'p = 0')
          Left: IParameterReferenceExpression: p (OperationKind.ParameterReferenceExpression, Type: System.Int32) (Syntax: 'p')
          Right: ILiteralExpression (OperationKind.LiteralExpression, Type: System.Int32, Constant: 0) (Syntax: '0')
    ILabeledStatement (Label: label_1) (OperationKind.LabeledStatement) (Syntax: 'do p = 0; while (p > 0);')
      Statement: null
    IConditionalGotoStatement (JumpIfTrue: True, Target: label_0) (OperationKind.ConditionalGotoStatement) (Syntax: 'do p = 0; while (p > 0);')
      Condition: IBinaryOperatorExpression (BinaryOperatorKind.GreaterThan) (OperationKind.BinaryOperatorExpression, Type: System.Boolean) (Syntax: 'p > 0')
          Left: IParameterReferenceExpression: p (OperationKind.ParameterReferenceExpression, Type: System.Int32) (Syntax: 'p')
          Right: ILiteralExpression (OperationKind.LiteralExpression, Type: System.Int32, Constant: 0) (Syntax: '0')
    ILabeledStatement (Label: label_2) (OperationKind.LabeledStatement) (Syntax: 'do p = 0; while (p > 0);')
      Statement: null
";
            var expectedDiagnostics = DiagnosticDescription.None;
            VerifyOperationTreeAndDiagnosticsForTest<BaseMethodDeclarationSyntax>(source, expectedOperationTree, expectedDiagnostics, useLoweredTree: true);
        }

        [CompilerTrait(CompilerFeature.IOperation)]
        [Fact(Skip = "https://github.com/dotnet/roslyn/issues/21866")]
        public void IConditionalGotoStatement_FromFor()
        {
            string source = @"
class C
{
    /*<bind>*/
    static void Method(int p)
    {
        for (var i = 0; i < p; ++i) p = 0;
    }
    /*</bind>*/
}
";
            string expectedOperationTree = @"
IBlockStatement (1 statements) (OperationKind.BlockStatement) (Syntax: '{ ... }')
  IBlockStatement (9 statements, 1 locals) (OperationKind.BlockStatement) (Syntax: 'for (var i  ... ++i) p = 0;')
    Locals: Local_1: System.Int32 i
    IExpressionStatement (OperationKind.ExpressionStatement) (Syntax: 'i = 0')
      Expression: ISimpleAssignmentExpression (OperationKind.SimpleAssignmentExpression, Type: System.Int32) (Syntax: 'i = 0')
          Left: ILocalReferenceExpression: i (OperationKind.LocalReferenceExpression, Type: System.Int32) (Syntax: 'i = 0')
          Right: ILiteralExpression (OperationKind.LiteralExpression, Type: System.Int32, Constant: 0) (Syntax: '0')
    IBranchStatement (BranchKind.GoTo, Label: label_0) (OperationKind.BranchStatement) (Syntax: 'for (var i  ... ++i) p = 0;')
    ILabeledStatement (Label: label_1) (OperationKind.LabeledStatement) (Syntax: 'for (var i  ... ++i) p = 0;')
      Statement: null
    IExpressionStatement (OperationKind.ExpressionStatement) (Syntax: 'p = 0;')
      Expression: ISimpleAssignmentExpression (OperationKind.SimpleAssignmentExpression, Type: System.Int32) (Syntax: 'p = 0')
          Left: IParameterReferenceExpression: p (OperationKind.ParameterReferenceExpression, Type: System.Int32) (Syntax: 'p')
          Right: ILiteralExpression (OperationKind.LiteralExpression, Type: System.Int32, Constant: 0) (Syntax: '0')
    ILabeledStatement (Label: label_2) (OperationKind.LabeledStatement) (Syntax: 'for (var i  ... ++i) p = 0;')
      Statement: null
    IExpressionStatement (OperationKind.ExpressionStatement) (Syntax: '++i')
      Expression: ISequenceExpression (OperationKind.SequenceExpression) (Syntax: '++i')
          Locals: Local_1: System.Int32 ?
          SideEffects(2):
              ISimpleAssignmentExpression (OperationKind.SimpleAssignmentExpression, Type: System.Int32) (Syntax: '++i')
                Left: ILocalReferenceExpression:  (OperationKind.LocalReferenceExpression, Type: System.Int32) (Syntax: '++i')
                Right: IBinaryOperatorExpression (BinaryOperatorKind.Add) (OperationKind.BinaryOperatorExpression, Type: System.Int32) (Syntax: '++i')
                    Left: ILocalReferenceExpression: i (OperationKind.LocalReferenceExpression, Type: System.Int32) (Syntax: 'i')
                    Right: ILiteralExpression (OperationKind.LiteralExpression, Type: System.Int32, Constant: 1) (Syntax: '++i')
              ISimpleAssignmentExpression (OperationKind.SimpleAssignmentExpression, Type: System.Int32) (Syntax: '++i')
                Left: ILocalReferenceExpression: i (OperationKind.LocalReferenceExpression, Type: System.Int32) (Syntax: 'i')
                Right: ILocalReferenceExpression:  (OperationKind.LocalReferenceExpression, Type: System.Int32) (Syntax: '++i')
          Value: ILocalReferenceExpression:  (OperationKind.LocalReferenceExpression, Type: System.Int32) (Syntax: '++i')
    ILabeledStatement (Label: label_0) (OperationKind.LabeledStatement) (Syntax: 'for (var i  ... ++i) p = 0;')
      Statement: null
    IConditionalGotoStatement (JumpIfTrue: True, Target: label_1) (OperationKind.ConditionalGotoStatement) (Syntax: 'i < p')
      Condition: IBinaryOperatorExpression (BinaryOperatorKind.LessThan) (OperationKind.BinaryOperatorExpression, Type: System.Boolean) (Syntax: 'i < p')
          Left: ILocalReferenceExpression: i (OperationKind.LocalReferenceExpression, Type: System.Int32) (Syntax: 'i')
          Right: IParameterReferenceExpression: p (OperationKind.ParameterReferenceExpression, Type: System.Int32) (Syntax: 'p')
    ILabeledStatement (Label: label_3) (OperationKind.LabeledStatement) (Syntax: 'for (var i  ... ++i) p = 0;')
      Statement: null
";
            var expectedDiagnostics = DiagnosticDescription.None;
            VerifyOperationTreeAndDiagnosticsForTest<BaseMethodDeclarationSyntax>(source, expectedOperationTree, expectedDiagnostics, useLoweredTree: true);
        }
    }
}
