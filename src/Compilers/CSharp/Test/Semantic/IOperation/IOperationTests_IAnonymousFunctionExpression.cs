// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Linq;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Operations;
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
IVariableDeclarationGroupOperation (1 declarations) (OperationKind.VariableDeclarationGroup, Type: null) (Syntax: 'Action x = () => F();')
  IVariableDeclarationOperation (1 declarators) (OperationKind.VariableDeclaration, Type: null) (Syntax: 'Action x = () => F()')
    Declarators:
        IVariableDeclaratorOperation (Symbol: System.Action x) (OperationKind.VariableDeclarator, Type: null) (Syntax: 'x = () => F()')
          Initializer: 
            IVariableInitializerOperation (OperationKind.VariableInitializer, Type: null) (Syntax: '= () => F()')
              IDelegateCreationOperation (OperationKind.DelegateCreation, Type: System.Action, IsImplicit) (Syntax: '() => F()')
                Target: 
                  IAnonymousFunctionOperation (Symbol: lambda expression) (OperationKind.AnonymousFunction, Type: null) (Syntax: '() => F()')
                    IBlockOperation (2 statements) (OperationKind.Block, Type: null, IsImplicit) (Syntax: 'F()')
                      IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null, IsImplicit) (Syntax: 'F()')
                        Expression: 
                          IInvocationOperation (void Program.F()) (OperationKind.Invocation, Type: System.Void) (Syntax: 'F()')
                            Instance Receiver: 
                              null
                            Arguments(0)
                      IReturnOperation (OperationKind.Return, Type: null, IsImplicit) (Syntax: 'F()')
                        ReturnedValue: 
                          null
          IgnoredArguments(0)
    Initializer: 
      null
";
            var expectedDiagnostics = DiagnosticDescription.None;

            VerifyOperationTreeAndDiagnosticsForTest<LocalDeclarationStatementSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation)]
        [Fact]
        public void IAnonymousFunctionExpression_BoundLambda_HasValidLambdaExpressionTree_JustBindingLambdaReturnsOnlyIAnonymousFunctionExpression()
        {
            string source = @"
using System;

class Program
{
    static void Main(string[] args)
    {
        Action x = /*<bind>*/() => F()/*</bind>*/;
    }

    static void F()
    {
    }
}
";
            string expectedOperationTree = @"
IAnonymousFunctionOperation (Symbol: lambda expression) (OperationKind.AnonymousFunction, Type: null) (Syntax: '() => F()')
  IBlockOperation (2 statements) (OperationKind.Block, Type: null, IsImplicit) (Syntax: 'F()')
    IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null, IsImplicit) (Syntax: 'F()')
      Expression: 
        IInvocationOperation (void Program.F()) (OperationKind.Invocation, Type: System.Void) (Syntax: 'F()')
          Instance Receiver: 
            null
          Arguments(0)
    IReturnOperation (OperationKind.Return, Type: null, IsImplicit) (Syntax: 'F()')
      ReturnedValue: 
        null
";
            var expectedDiagnostics = DiagnosticDescription.None;

            VerifyOperationTreeAndDiagnosticsForTest<ParenthesizedLambdaExpressionSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation)]
        [Fact]
        public void IAnonymousFunctionExpression_BoundLambda_HasValidLambdaExpressionTree_ExplicitCast()
        {
            string source = @"
using System;

class Program
{
    static void Main(string[] args)
    {
        /*<bind>*/Action x = (Action)(() => F());/*</bind>*/
    }

    static void F()
    {
    }
}
";
            string expectedOperationTree = @"
IVariableDeclarationGroupOperation (1 declarations) (OperationKind.VariableDeclarationGroup, Type: null) (Syntax: 'Action x =  ... () => F());')
  IVariableDeclarationOperation (1 declarators) (OperationKind.VariableDeclaration, Type: null) (Syntax: 'Action x =  ... (() => F())')
    Declarators:
        IVariableDeclaratorOperation (Symbol: System.Action x) (OperationKind.VariableDeclarator, Type: null) (Syntax: 'x = (Action)(() => F())')
          Initializer: 
            IVariableInitializerOperation (OperationKind.VariableInitializer, Type: null) (Syntax: '= (Action)(() => F())')
              IDelegateCreationOperation (OperationKind.DelegateCreation, Type: System.Action) (Syntax: '(Action)(() => F())')
                Target: 
                  IAnonymousFunctionOperation (Symbol: lambda expression) (OperationKind.AnonymousFunction, Type: null) (Syntax: '() => F()')
                    IBlockOperation (2 statements) (OperationKind.Block, Type: null, IsImplicit) (Syntax: 'F()')
                      IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null, IsImplicit) (Syntax: 'F()')
                        Expression: 
                          IInvocationOperation (void Program.F()) (OperationKind.Invocation, Type: System.Void) (Syntax: 'F()')
                            Instance Receiver: 
                              null
                            Arguments(0)
                      IReturnOperation (OperationKind.Return, Type: null, IsImplicit) (Syntax: 'F()')
                        ReturnedValue: 
                          null
          IgnoredArguments(0)
    Initializer: 
      null
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
        Action x = /*<bind>*/() => F()/*</bind>*/;
    }

    static void F()
    {
    }
}
";
            string expectedOperationTree = @"
IAnonymousFunctionOperation (Symbol: lambda expression) (OperationKind.AnonymousFunction, Type: null) (Syntax: '() => F()')
  IBlockOperation (2 statements) (OperationKind.Block, Type: null, IsImplicit) (Syntax: 'F()')
    IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null, IsImplicit) (Syntax: 'F()')
      Expression: 
        IInvocationOperation (void Program.F()) (OperationKind.Invocation, Type: System.Void) (Syntax: 'F()')
          Instance Receiver: 
            null
          Arguments(0)
    IReturnOperation (OperationKind.Return, Type: null, IsImplicit) (Syntax: 'F()')
      ReturnedValue: 
        null
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
IVariableDeclarationGroupOperation (1 declarations) (OperationKind.VariableDeclarationGroup, Type: null, IsInvalid) (Syntax: 'var x = () => F();')
  IVariableDeclarationOperation (1 declarators) (OperationKind.VariableDeclaration, Type: null, IsInvalid) (Syntax: 'var x = () => F()')
    Declarators:
        IVariableDeclaratorOperation (Symbol: var x) (OperationKind.VariableDeclarator, Type: null, IsInvalid) (Syntax: 'x = () => F()')
          Initializer: 
            IVariableInitializerOperation (OperationKind.VariableInitializer, Type: null, IsInvalid) (Syntax: '= () => F()')
              IAnonymousFunctionOperation (Symbol: lambda expression) (OperationKind.AnonymousFunction, Type: null, IsInvalid) (Syntax: '() => F()')
                IBlockOperation (1 statements) (OperationKind.Block, Type: null, IsInvalid, IsImplicit) (Syntax: 'F()')
                  IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null, IsInvalid, IsImplicit) (Syntax: 'F()')
                    Expression: 
                      IInvocationOperation (void Program.F()) (OperationKind.Invocation, Type: System.Void, IsInvalid) (Syntax: 'F()')
                        Instance Receiver: 
                          null
                        Arguments(0)
          IgnoredArguments(0)
    Initializer: 
      null
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
IVariableDeclarationGroupOperation (1 declarations) (OperationKind.VariableDeclarationGroup, Type: null, IsInvalid) (Syntax: 'Action<int> ...  () => F();')
  IVariableDeclarationOperation (1 declarators) (OperationKind.VariableDeclaration, Type: null, IsInvalid) (Syntax: 'Action<int> ... = () => F()')
    Declarators:
        IVariableDeclaratorOperation (Symbol: System.Action<System.Int32> x) (OperationKind.VariableDeclarator, Type: null, IsInvalid) (Syntax: 'x = () => F()')
          Initializer: 
            IVariableInitializerOperation (OperationKind.VariableInitializer, Type: null, IsInvalid) (Syntax: '= () => F()')
              IDelegateCreationOperation (OperationKind.DelegateCreation, Type: System.Action<System.Int32>, IsInvalid, IsImplicit) (Syntax: '() => F()')
                Target: 
                  IAnonymousFunctionOperation (Symbol: lambda expression) (OperationKind.AnonymousFunction, Type: null, IsInvalid) (Syntax: '() => F()')
                    IBlockOperation (1 statements) (OperationKind.Block, Type: null, IsInvalid, IsImplicit) (Syntax: 'F()')
                      IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null, IsInvalid, IsImplicit) (Syntax: 'F()')
                        Expression: 
                          IInvocationOperation (void Program.F()) (OperationKind.Invocation, Type: System.Void, IsInvalid) (Syntax: 'F()')
                            Instance Receiver: 
                              null
                            Arguments(0)
          IgnoredArguments(0)
    Initializer: 
      null
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
        public void IAnonymousFunctionExpression_UnboundLambdaAsDelegate_HasValidLambdaExpressionTree_ExplicitValidCast()
        {
            string source = @"
using System;

class Program
{
    static void Main(string[] args)
    {
        /*<bind>*/Action<int> x = (Action)(() => F());/*</bind>*/
    }

    static void F()
    {
    }
}
";
            string expectedOperationTree = @"
IVariableDeclarationGroupOperation (1 declarations) (OperationKind.VariableDeclarationGroup, Type: null, IsInvalid) (Syntax: 'Action<int> ... () => F());')
  IVariableDeclarationOperation (1 declarators) (OperationKind.VariableDeclaration, Type: null, IsInvalid) (Syntax: 'Action<int> ... (() => F())')
    Declarators:
        IVariableDeclaratorOperation (Symbol: System.Action<System.Int32> x) (OperationKind.VariableDeclarator, Type: null, IsInvalid) (Syntax: 'x = (Action)(() => F())')
          Initializer: 
            IVariableInitializerOperation (OperationKind.VariableInitializer, Type: null, IsInvalid) (Syntax: '= (Action)(() => F())')
              IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.Action<System.Int32>, IsInvalid, IsImplicit) (Syntax: '(Action)(() => F())')
                Conversion: CommonConversion (Exists: False, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                Operand: 
                  IDelegateCreationOperation (OperationKind.DelegateCreation, Type: System.Action, IsInvalid) (Syntax: '(Action)(() => F())')
                    Target: 
                      IAnonymousFunctionOperation (Symbol: lambda expression) (OperationKind.AnonymousFunction, Type: null, IsInvalid) (Syntax: '() => F()')
                        IBlockOperation (2 statements) (OperationKind.Block, Type: null, IsInvalid, IsImplicit) (Syntax: 'F()')
                          IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null, IsInvalid, IsImplicit) (Syntax: 'F()')
                            Expression: 
                              IInvocationOperation (void Program.F()) (OperationKind.Invocation, Type: System.Void, IsInvalid) (Syntax: 'F()')
                                Instance Receiver: 
                                  null
                                Arguments(0)
                          IReturnOperation (OperationKind.Return, Type: null, IsInvalid, IsImplicit) (Syntax: 'F()')
                            ReturnedValue: 
                              null
          IgnoredArguments(0)
    Initializer: 
      null
";
            var expectedDiagnostics = new DiagnosticDescription[] {
                // CS0029: Cannot implicitly convert type 'System.Action' to 'System.Action<int>'
                //         /*<bind>*/Action<int> x = (Action)(() => F());/*</bind>*/
                Diagnostic(ErrorCode.ERR_NoImplicitConv, "(Action)(() => F())").WithArguments("System.Action", "System.Action<int>").WithLocation(8, 35)
            };

            VerifyOperationTreeAndDiagnosticsForTest<LocalDeclarationStatementSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation)]
        [Fact]
        public void IAnonymousFunctionExpression_UnboundLambdaAsDelegate_HasValidLambdaExpressionTree_ExplicitInvalidCast()
        {
            string source = @"
using System;

class Program
{
    static void Main(string[] args)
    {
        /*<bind>*/Action<int> x = (Action<int>)(() => F());/*</bind>*/
    }

    static void F()
    {
    }
}
";
            string expectedOperationTree = @"
IVariableDeclarationGroupOperation (1 declarations) (OperationKind.VariableDeclarationGroup, Type: null, IsInvalid) (Syntax: 'Action<int> ... () => F());')
  IVariableDeclarationOperation (1 declarators) (OperationKind.VariableDeclaration, Type: null, IsInvalid) (Syntax: 'Action<int> ... (() => F())')
    Declarators:
        IVariableDeclaratorOperation (Symbol: System.Action<System.Int32> x) (OperationKind.VariableDeclarator, Type: null, IsInvalid) (Syntax: 'x = (Action ... (() => F())')
          Initializer: 
            IVariableInitializerOperation (OperationKind.VariableInitializer, Type: null, IsInvalid) (Syntax: '= (Action<i ... (() => F())')
              IDelegateCreationOperation (OperationKind.DelegateCreation, Type: System.Action<System.Int32>, IsInvalid) (Syntax: '(Action<int>)(() => F())')
                Target: 
                  IAnonymousFunctionOperation (Symbol: lambda expression) (OperationKind.AnonymousFunction, Type: null, IsInvalid) (Syntax: '() => F()')
                    IBlockOperation (1 statements) (OperationKind.Block, Type: null, IsInvalid, IsImplicit) (Syntax: 'F()')
                      IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null, IsInvalid, IsImplicit) (Syntax: 'F()')
                        Expression: 
                          IInvocationOperation (void Program.F()) (OperationKind.Invocation, Type: System.Void, IsInvalid) (Syntax: 'F()')
                            Instance Receiver: 
                              null
                            Arguments(0)
          IgnoredArguments(0)
    Initializer: 
      null
";
            var expectedDiagnostics = new DiagnosticDescription[] {
                // CS1593: Delegate 'Action<int>' does not take 0 arguments
                //         /*<bind>*/Action<int> x = (Action<int>)(() => F());/*</bind>*/
                Diagnostic(ErrorCode.ERR_BadDelArgCount, "() => F()").WithArguments("System.Action<int>", "0").WithLocation(8, 49)
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

            var variableDeclarationGroupOperation = (IVariableDeclarationGroupOperation)semanticModel.GetOperationInternal(variableDeclaration);
            var variableTreeLambdaOperation = (IAnonymousFunctionOperation)variableDeclarationGroupOperation.Declarations.Single().Declarators.Single().Initializer.Value;
            var lambdaOperation = (IAnonymousFunctionOperation)semanticModel.GetOperationInternal(lambdaSyntax);

            // Assert that both ways of getting to the lambda (requesting the lambda directly, and requesting via the lambda syntax)
            // return the same bound node.
            Assert.Same(variableTreeLambdaOperation, lambdaOperation);

            var variableDeclarationGroupOperationSecondRequest = (IVariableDeclarationGroupOperation)semanticModel.GetOperationInternal(variableDeclaration);
            var variableTreeLambdaOperationSecondRequest = (IAnonymousFunctionOperation)variableDeclarationGroupOperation.Declarations.Single().Declarators.Single(). Initializer.Value;
            var lambdaOperationSecondRequest = (IAnonymousFunctionOperation)semanticModel.GetOperationInternal(lambdaSyntax);

            // Assert that, when request the variable declaration or the lambda for a second time, there is no rebinding of the
            // underlying UnboundLambda, and we get the same IAnonymousFunctionExpression as before
            Assert.Same(variableTreeLambdaOperation, variableTreeLambdaOperationSecondRequest);
            Assert.Same(lambdaOperation, lambdaOperationSecondRequest);
        }
    }
}
