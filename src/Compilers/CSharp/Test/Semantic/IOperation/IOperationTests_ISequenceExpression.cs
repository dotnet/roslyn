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
        [Fact(Skip = "https://github.com/dotnet/roslyn/issues/21866")]
        public void ISequenceExpression_FromPostIncrement()
        {
            string source = @"
class C
{
    /*<bind>*/
    static void Method(int p)
    {
        p++;
    }
    /*</bind>*/
}
";
            string expectedOperationTree = @"
IBlockStatement (1 statements) (OperationKind.BlockStatement) (Syntax: '{ ... }')
  IExpressionStatement (OperationKind.ExpressionStatement) (Syntax: 'p++;')
    Expression: ISequenceExpression (OperationKind.SequenceExpression) (Syntax: 'p++')
        Locals: Local_1: System.Int32 ?
        SideEffects(2):
            ISimpleAssignmentExpression (OperationKind.SimpleAssignmentExpression, Type: System.Int32) (Syntax: 'p++')
              Left: ILocalReferenceExpression:  (OperationKind.LocalReferenceExpression, Type: System.Int32) (Syntax: 'p++')
              Right: IParameterReferenceExpression: p (OperationKind.ParameterReferenceExpression, Type: System.Int32) (Syntax: 'p')
            ISimpleAssignmentExpression (OperationKind.SimpleAssignmentExpression, Type: System.Int32) (Syntax: 'p++')
              Left: IParameterReferenceExpression: p (OperationKind.ParameterReferenceExpression, Type: System.Int32) (Syntax: 'p')
              Right: IBinaryOperatorExpression (BinaryOperatorKind.Add) (OperationKind.BinaryOperatorExpression, Type: System.Int32) (Syntax: 'p++')
                  Left: ILocalReferenceExpression:  (OperationKind.LocalReferenceExpression, Type: System.Int32) (Syntax: 'p++')
                  Right: ILiteralExpression (OperationKind.LiteralExpression, Type: System.Int32, Constant: 1) (Syntax: 'p++')
        Value: ILocalReferenceExpression:  (OperationKind.LocalReferenceExpression, Type: System.Int32) (Syntax: 'p++')
";
            var expectedDiagnostics = DiagnosticDescription.None;
            VerifyOperationTreeAndDiagnosticsForTest<BaseMethodDeclarationSyntax>(source, expectedOperationTree, expectedDiagnostics, useLoweredTree: true);
        }

        [CompilerTrait(CompilerFeature.IOperation)]
        [Fact(Skip = "https://github.com/dotnet/roslyn/issues/21866")]
        public void ISequenceExpression_FromPostDecrement()
        {
            string source = @"
class C
{
    /*<bind>*/
    static void Method(int p)
    {
        p--;
    }
    /*</bind>*/
}
";
            string expectedOperationTree = @"
IBlockStatement (1 statements) (OperationKind.BlockStatement) (Syntax: '{ ... }')
  IExpressionStatement (OperationKind.ExpressionStatement) (Syntax: 'p--;')
    Expression: ISequenceExpression (OperationKind.SequenceExpression) (Syntax: 'p--')
        Locals: Local_1: System.Int32 ?
        SideEffects(2):
            ISimpleAssignmentExpression (OperationKind.SimpleAssignmentExpression, Type: System.Int32) (Syntax: 'p--')
              Left: ILocalReferenceExpression:  (OperationKind.LocalReferenceExpression, Type: System.Int32) (Syntax: 'p--')
              Right: IParameterReferenceExpression: p (OperationKind.ParameterReferenceExpression, Type: System.Int32) (Syntax: 'p')
            ISimpleAssignmentExpression (OperationKind.SimpleAssignmentExpression, Type: System.Int32) (Syntax: 'p--')
              Left: IParameterReferenceExpression: p (OperationKind.ParameterReferenceExpression, Type: System.Int32) (Syntax: 'p')
              Right: IBinaryOperatorExpression (BinaryOperatorKind.Subtract) (OperationKind.BinaryOperatorExpression, Type: System.Int32) (Syntax: 'p--')
                  Left: ILocalReferenceExpression:  (OperationKind.LocalReferenceExpression, Type: System.Int32) (Syntax: 'p--')
                  Right: ILiteralExpression (OperationKind.LiteralExpression, Type: System.Int32, Constant: 1) (Syntax: 'p--')
        Value: ILocalReferenceExpression:  (OperationKind.LocalReferenceExpression, Type: System.Int32) (Syntax: 'p--')
";
            var expectedDiagnostics = DiagnosticDescription.None;
            VerifyOperationTreeAndDiagnosticsForTest<BaseMethodDeclarationSyntax>(source, expectedOperationTree, expectedDiagnostics, useLoweredTree: true);
        }

        [CompilerTrait(CompilerFeature.IOperation)]
        [Fact(Skip = "https://github.com/dotnet/roslyn/issues/21866")]
        public void ISequenceExpression_FromPreIncrement()
        {
            string source = @"
class C
{
    /*<bind>*/
    static void Method(int p)
    {
        ++p;
    }
    /*</bind>*/
}
";
            string expectedOperationTree = @"
IBlockStatement (1 statements) (OperationKind.BlockStatement) (Syntax: '{ ... }')
  IExpressionStatement (OperationKind.ExpressionStatement) (Syntax: '++p;')
    Expression: ISequenceExpression (OperationKind.SequenceExpression) (Syntax: '++p')
        Locals: Local_1: System.Int32 ?
        SideEffects(2):
            ISimpleAssignmentExpression (OperationKind.SimpleAssignmentExpression, Type: System.Int32) (Syntax: '++p')
              Left: ILocalReferenceExpression:  (OperationKind.LocalReferenceExpression, Type: System.Int32) (Syntax: '++p')
              Right: IBinaryOperatorExpression (BinaryOperatorKind.Add) (OperationKind.BinaryOperatorExpression, Type: System.Int32) (Syntax: '++p')
                  Left: IParameterReferenceExpression: p (OperationKind.ParameterReferenceExpression, Type: System.Int32) (Syntax: 'p')
                  Right: ILiteralExpression (OperationKind.LiteralExpression, Type: System.Int32, Constant: 1) (Syntax: '++p')
            ISimpleAssignmentExpression (OperationKind.SimpleAssignmentExpression, Type: System.Int32) (Syntax: '++p')
              Left: IParameterReferenceExpression: p (OperationKind.ParameterReferenceExpression, Type: System.Int32) (Syntax: 'p')
              Right: ILocalReferenceExpression:  (OperationKind.LocalReferenceExpression, Type: System.Int32) (Syntax: '++p')
        Value: ILocalReferenceExpression:  (OperationKind.LocalReferenceExpression, Type: System.Int32) (Syntax: '++p')
";
            var expectedDiagnostics = DiagnosticDescription.None;
            VerifyOperationTreeAndDiagnosticsForTest<BaseMethodDeclarationSyntax>(source, expectedOperationTree, expectedDiagnostics, useLoweredTree: true);
        }

        [CompilerTrait(CompilerFeature.IOperation)]
        [Fact(Skip = "https://github.com/dotnet/roslyn/issues/21866")]
        public void ISequenceExpression_FromPreDecrement()
        {
            string source = @"
class C
{
    /*<bind>*/
    static void Method(int p)
    {
        --p;
    }
    /*</bind>*/
}
";
            string expectedOperationTree = @"
IBlockStatement (1 statements) (OperationKind.BlockStatement) (Syntax: '{ ... }')
  IExpressionStatement (OperationKind.ExpressionStatement) (Syntax: '--p;')
    Expression: ISequenceExpression (OperationKind.SequenceExpression) (Syntax: '--p')
        Locals: Local_1: System.Int32 ?
        SideEffects(2):
            ISimpleAssignmentExpression (OperationKind.SimpleAssignmentExpression, Type: System.Int32) (Syntax: '--p')
              Left: ILocalReferenceExpression:  (OperationKind.LocalReferenceExpression, Type: System.Int32) (Syntax: '--p')
              Right: IBinaryOperatorExpression (BinaryOperatorKind.Subtract) (OperationKind.BinaryOperatorExpression, Type: System.Int32) (Syntax: '--p')
                  Left: IParameterReferenceExpression: p (OperationKind.ParameterReferenceExpression, Type: System.Int32) (Syntax: 'p')
                  Right: ILiteralExpression (OperationKind.LiteralExpression, Type: System.Int32, Constant: 1) (Syntax: '--p')
            ISimpleAssignmentExpression (OperationKind.SimpleAssignmentExpression, Type: System.Int32) (Syntax: '--p')
              Left: IParameterReferenceExpression: p (OperationKind.ParameterReferenceExpression, Type: System.Int32) (Syntax: 'p')
              Right: ILocalReferenceExpression:  (OperationKind.LocalReferenceExpression, Type: System.Int32) (Syntax: '--p')
        Value: ILocalReferenceExpression:  (OperationKind.LocalReferenceExpression, Type: System.Int32) (Syntax: '--p')
";
            var expectedDiagnostics = DiagnosticDescription.None;
            VerifyOperationTreeAndDiagnosticsForTest<BaseMethodDeclarationSyntax>(source, expectedOperationTree, expectedDiagnostics, useLoweredTree: true);
        }
    }
}
