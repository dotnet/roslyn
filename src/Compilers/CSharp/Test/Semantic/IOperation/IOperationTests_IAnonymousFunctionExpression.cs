// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Linq;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Semantics;
using Microsoft.CodeAnalysis.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.CSharp.UnitTests
{
    public partial class IOperationTests : SemanticModelTestBase
    {
        [CompilerTrait(CompilerFeature.IOperation)]
        [Fact]
        public void IAnonymousFunctionExpression_BoundLambda_HasValidLambdaExpressionTree()
        {
            string source = @"
using System;

class Program
{
    static void Main(string[] args)
    {
        /*<bind>*/Action x = () => F();/*</bind>*/
    }

    static void F()
    {
    }
}
";
            string expectedOperationTree = @"
IVariableDeclarationStatement (1 declarations) (OperationKind.VariableDeclarationStatement) (Syntax: 'Action x = () => F();')
  IVariableDeclaration (1 variables) (OperationKind.VariableDeclaration) (Syntax: 'x = () => F()')
    Variables: Local_1: System.Action x
    Initializer: IConversionExpression (Implicit, TryCast: False, Unchecked) (OperationKind.ConversionExpression, Type: System.Action) (Syntax: '() => F()')
        Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
        Operand: IAnonymousFunctionExpression (Symbol: lambda expression) (OperationKind.AnonymousFunctionExpression, Type: null) (Syntax: '() => F()')
            IBlockStatement (2 statements) (OperationKind.BlockStatement) (Syntax: 'F()')
              IExpressionStatement (OperationKind.ExpressionStatement) (Syntax: 'F()')
                Expression: IInvocationExpression (void Program.F()) (OperationKind.InvocationExpression, Type: System.Void) (Syntax: 'F()')
                    Instance Receiver: null
                    Arguments(0)
              IReturnStatement (OperationKind.ReturnStatement) (Syntax: 'F()')
                ReturnedValue: null
";
            var expectedDiagnostics = DiagnosticDescription.None;

            VerifyOperationTreeAndDiagnosticsForTest<LocalDeclarationStatementSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation)]
        [Fact]
        public void IAnonymousFunctionExpression_BoundLambda_HasValidLambdaExpressionTree2()
        {
            string source = @"
using System;

class Program
{
    static void Main(string[] args)
    {
        Action x = /*<bind>*/() => F();/*</bind>*/
    }

    static void F()
    {
    }
}
";
            string expectedOperationTree = @"
IAnonymousFunctionExpression (Symbol: lambda expression) (OperationKind.AnonymousFunctionExpression, Type: null) (Syntax: '() => F()')
    IBlockStatement (2 statements) (OperationKind.BlockStatement) (Syntax: 'F()')
        IExpressionStatement (OperationKind.ExpressionStatement) (Syntax: 'F()')
        Expression: IInvocationExpression (void Program.F()) (OperationKind.InvocationExpression, Type: System.Void) (Syntax: 'F()')
            Instance Receiver: null
            Arguments(0)
        IReturnStatement (OperationKind.ReturnStatement) (Syntax: 'F()')
        ReturnedValue: null
";
            var expectedDiagnostics = DiagnosticDescription.None;

            VerifyOperationTreeAndDiagnosticsForTest<ParenthesizedLambdaExpressionSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation)]
        [Fact]
        public void IAnonymousFunctionExpression_UnboundLambdaAsVar_HasValidLambdaExpressionTree()
        {
            string source = @"
using System;

class Program
{
    static void Main(string[] args)
    {
        /*<bind>*/var x = () => F();/*</bind>*/
    }

    static void F()
    {
    }
}
";
            string expectedOperationTree = @"
IVariableDeclarationStatement (1 declarations) (OperationKind.VariableDeclarationStatement, IsInvalid) (Syntax: 'var x = () => F();')
  IVariableDeclaration (1 variables) (OperationKind.VariableDeclaration, IsInvalid) (Syntax: 'x = () => F()')
    Variables: Local_1: var x
    Initializer: IAnonymousFunctionExpression (Symbol: lambda expression) (OperationKind.AnonymousFunctionExpression, Type: null, IsInvalid) (Syntax: '() => F()')
        IBlockStatement (1 statements) (OperationKind.BlockStatement, IsInvalid) (Syntax: 'F()')
          IExpressionStatement (OperationKind.ExpressionStatement, IsInvalid) (Syntax: 'F()')
            Expression: IInvocationExpression (void Program.F()) (OperationKind.InvocationExpression, Type: System.Void, IsInvalid) (Syntax: 'F()')
                Instance Receiver: null
                Arguments(0)
";
            var expectedDiagnostics = new DiagnosticDescription[] {
                // CS0815: Cannot assign lambda expression to an implicitly-typed variable
                //         /*<bind>*/var x = () => F();/*</bind>*/
                Diagnostic(ErrorCode.ERR_ImplicitlyTypedVariableAssignedBadValue, "x = () => F()").WithArguments("lambda expression").WithLocation(8, 23),
            };

            VerifyOperationTreeAndDiagnosticsForTest<LocalDeclarationStatementSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation)]
        [Fact]
        public void IAnonymousFunctionExpression_UnboundLambdaAsDelegate_HasValidLambdaExpressionTree()
        {
            string source = @"
using System;

class Program
{
    static void Main(string[] args)
    {
        /*<bind>*/Action<int> x = () => F();/*</bind>*/
    }

    static void F()
    {
    }
}
";
            string expectedOperationTree = @"
IVariableDeclarationStatement (1 declarations) (OperationKind.VariableDeclarationStatement, IsInvalid) (Syntax: 'Action<int> ...  () => F();')
  IVariableDeclaration (1 variables) (OperationKind.VariableDeclaration, IsInvalid) (Syntax: 'x = () => F()')
    Variables: Local_1: System.Action<System.Int32> x
    Initializer: IConversionExpression (Implicit, TryCast: False, Unchecked) (OperationKind.ConversionExpression, Type: System.Action<System.Int32>, IsInvalid) (Syntax: '() => F()')
        Conversion: CommonConversion (Exists: False, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
        Operand: IAnonymousFunctionExpression (Symbol: lambda expression) (OperationKind.AnonymousFunctionExpression, Type: null, IsInvalid) (Syntax: '() => F()')
            IBlockStatement (1 statements) (OperationKind.BlockStatement, IsInvalid) (Syntax: 'F()')
              IExpressionStatement (OperationKind.ExpressionStatement, IsInvalid) (Syntax: 'F()')
                Expression: IInvocationExpression (void Program.F()) (OperationKind.InvocationExpression, Type: System.Void, IsInvalid) (Syntax: 'F()')
                    Instance Receiver: null
                    Arguments(0)
";
            var expectedDiagnostics = new DiagnosticDescription[] {
                // CS1593: Delegate 'Action<int>' does not take 0 arguments
                //         Action<int> x /*<bind>*/= () => F()/*</bind>*/;
                Diagnostic(ErrorCode.ERR_BadDelArgCount, "() => F()").WithArguments("System.Action<int>", "0").WithLocation(8, 35)
            };

            VerifyOperationTreeAndDiagnosticsForTest<LocalDeclarationStatementSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation)]
        [Fact]
        public void IAnonymousFunctionExpression_UnboundLambda_ReferenceEquality()
        {
            string source = @"
using System;

class Program
{
    static void Main(string[] args)
    {
        /*<bind>*/var x = () => F();/*</bind>*/
    }

    static void F()
    {
    }
}
";

            var compilation = CreateCompilationWithMscorlibAndSystemCore(source);
            var syntaxTree = compilation.SyntaxTrees[0];
            var semanticModel = compilation.GetSemanticModel(syntaxTree);

            var variableDeclaration = syntaxTree.GetRoot().DescendantNodes().OfType<LocalDeclarationStatementSyntax>().Single();
            var lambdaSyntax = (LambdaExpressionSyntax)variableDeclaration.Declaration.Variables.Single().Initializer.Value;

            var variableDeclarationOperation = (IVariableDeclarationStatement)semanticModel.GetOperationInternal(variableDeclaration);
            var variableTreeLambdaOperation = (IAnonymousFunctionExpression)variableDeclarationOperation.Declarations.Single().Initializer;
            var lambdaOperation = (IAnonymousFunctionExpression)semanticModel.GetOperationInternal(lambdaSyntax);

            // Assert that both ways of getting to the lambda (requesting the lambda directly, and requesting via the lambda syntax)
            // return the same bound node.
            Assert.Same(variableTreeLambdaOperation, lambdaOperation);

            var variableDeclarationOperationSecondRequest = (IVariableDeclarationStatement)semanticModel.GetOperationInternal(variableDeclaration);
            var variableTreeLambdaOperationSecondRequest = (IAnonymousFunctionExpression)variableDeclarationOperation.Declarations.Single().Initializer;
            var lambdaOperationSecondRequest = (IAnonymousFunctionExpression)semanticModel.GetOperationInternal(lambdaSyntax);

            // Assert that, when request the variable declaration or the lambda for a second time, there is no rebinding of the
            // underlying UnboundLambda, and we get the same IAnonymousFunctionExpression as before
            Assert.Same(variableTreeLambdaOperation, variableTreeLambdaOperationSecondRequest);
            Assert.Same(lambdaOperation, lambdaOperationSecondRequest);
        }
    }
}
