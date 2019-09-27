// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Linq;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.CSharp.Test.Utilities;
using Microsoft.CodeAnalysis.FlowAnalysis;
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

            var compilation = CreateCompilationWithMscorlib40AndSystemCore(source);
            var syntaxTree = compilation.SyntaxTrees[0];
            var semanticModel = compilation.GetSemanticModel(syntaxTree);

            var variableDeclaration = syntaxTree.GetRoot().DescendantNodes().OfType<LocalDeclarationStatementSyntax>().Single();
            var lambdaSyntax = (LambdaExpressionSyntax)variableDeclaration.Declaration.Variables.Single().Initializer.Value;

            var variableDeclarationGroupOperation = (IVariableDeclarationGroupOperation)semanticModel.GetOperation(variableDeclaration);
            var variableTreeLambdaOperation = (IAnonymousFunctionOperation)variableDeclarationGroupOperation.Declarations.Single().Declarators.Single().Initializer.Value;
            var lambdaOperation = (IAnonymousFunctionOperation)semanticModel.GetOperation(lambdaSyntax);

            // Assert that both ways of getting to the lambda (requesting the lambda directly, and requesting via the lambda syntax)
            // return the same bound node.
            Assert.Same(variableTreeLambdaOperation, lambdaOperation);

            var variableDeclarationGroupOperationSecondRequest = (IVariableDeclarationGroupOperation)semanticModel.GetOperation(variableDeclaration);
            var variableTreeLambdaOperationSecondRequest = (IAnonymousFunctionOperation)variableDeclarationGroupOperation.Declarations.Single().Declarators.Single().Initializer.Value;
            var lambdaOperationSecondRequest = (IAnonymousFunctionOperation)semanticModel.GetOperation(lambdaSyntax);

            // Assert that, when request the variable declaration or the lambda for a second time, there is no rebinding of the
            // underlying UnboundLambda, and we get the same IAnonymousFunctionExpression as before
            Assert.Same(variableTreeLambdaOperation, variableTreeLambdaOperationSecondRequest);
            Assert.Same(lambdaOperation, lambdaOperationSecondRequest);
        }

        [CompilerTrait(CompilerFeature.IOperation, CompilerFeature.Dataflow)]
        [Fact]
        public void LambdaFlow_01()
        {
            string source = @"
struct C
{
    void M(System.Action<bool, bool> d)
/*<bind>*/{
        d = (bool result, bool input) =>
        {
            result = input;
        };

        d(false, true);
    }/*</bind>*/
}
";
            string expectedGraph = @"
Block[B0] - Entry
    Statements (0)
    Next (Regular) Block[B1]
Block[B1] - Block
    Predecessors: [B0]
    Statements (2)
        IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'd = (bool r ... };')
          Expression: 
            ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Action<System.Boolean, System.Boolean>) (Syntax: 'd = (bool r ... }')
              Left: 
                IParameterReferenceOperation: d (OperationKind.ParameterReference, Type: System.Action<System.Boolean, System.Boolean>) (Syntax: 'd')
              Right: 
                IDelegateCreationOperation (OperationKind.DelegateCreation, Type: System.Action<System.Boolean, System.Boolean>, IsImplicit) (Syntax: '(bool resul ... }')
                  Target: 
                    IFlowAnonymousFunctionOperation (Symbol: lambda expression) (OperationKind.FlowAnonymousFunction, Type: null) (Syntax: '(bool resul ... }')
                    {
                        Block[B0#A0] - Entry
                            Statements (0)
                            Next (Regular) Block[B1#A0]
                        Block[B1#A0] - Block
                            Predecessors: [B0#A0]
                            Statements (1)
                                IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'result = input;')
                                  Expression: 
                                    ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Boolean) (Syntax: 'result = input')
                                      Left: 
                                        IParameterReferenceOperation: result (OperationKind.ParameterReference, Type: System.Boolean) (Syntax: 'result')
                                      Right: 
                                        IParameterReferenceOperation: input (OperationKind.ParameterReference, Type: System.Boolean) (Syntax: 'input')

                            Next (Regular) Block[B2#A0]
                        Block[B2#A0] - Exit
                            Predecessors: [B1#A0]
                            Statements (0)
                    }

        IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'd(false, true);')
          Expression: 
            IInvocationOperation (virtual void System.Action<System.Boolean, System.Boolean>.Invoke(System.Boolean arg1, System.Boolean arg2)) (OperationKind.Invocation, Type: System.Void) (Syntax: 'd(false, true)')
              Instance Receiver: 
                IParameterReferenceOperation: d (OperationKind.ParameterReference, Type: System.Action<System.Boolean, System.Boolean>) (Syntax: 'd')
              Arguments(2):
                  IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: arg1) (OperationKind.Argument, Type: null) (Syntax: 'false')
                    ILiteralOperation (OperationKind.Literal, Type: System.Boolean, Constant: False) (Syntax: 'false')
                    InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                    OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                  IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: arg2) (OperationKind.Argument, Type: null) (Syntax: 'true')
                    ILiteralOperation (OperationKind.Literal, Type: System.Boolean, Constant: True) (Syntax: 'true')
                    InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                    OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)

    Next (Regular) Block[B2]
Block[B2] - Exit
    Predecessors: [B1]
    Statements (0)
";
            var expectedDiagnostics = DiagnosticDescription.None;

            VerifyFlowGraphAndDiagnosticsForTest<BlockSyntax>(source, expectedGraph, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation, CompilerFeature.Dataflow)]
        [Fact]
        public void LambdaFlow_02()
        {
            string source = @"
struct C
{
    void M(System.Action<bool, bool> d1, System.Action<bool, bool> d2)
/*<bind>*/{
        d1 = (bool result1, bool input1) =>
        {
            result1 = input1;
        };
        d2 = (bool result2, bool input2) => result2 = input2;
    }/*</bind>*/
}
";
            string expectedGraph = @"
Block[B0] - Entry
    Statements (0)
    Next (Regular) Block[B1]
Block[B1] - Block
    Predecessors: [B0]
    Statements (2)
        IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'd1 = (bool  ... };')
          Expression: 
            ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Action<System.Boolean, System.Boolean>) (Syntax: 'd1 = (bool  ... }')
              Left: 
                IParameterReferenceOperation: d1 (OperationKind.ParameterReference, Type: System.Action<System.Boolean, System.Boolean>) (Syntax: 'd1')
              Right: 
                IDelegateCreationOperation (OperationKind.DelegateCreation, Type: System.Action<System.Boolean, System.Boolean>, IsImplicit) (Syntax: '(bool resul ... }')
                  Target: 
                    IFlowAnonymousFunctionOperation (Symbol: lambda expression) (OperationKind.FlowAnonymousFunction, Type: null) (Syntax: '(bool resul ... }')
                    {
                        Block[B0#A0] - Entry
                            Statements (0)
                            Next (Regular) Block[B1#A0]
                        Block[B1#A0] - Block
                            Predecessors: [B0#A0]
                            Statements (1)
                                IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'result1 = input1;')
                                  Expression: 
                                    ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Boolean) (Syntax: 'result1 = input1')
                                      Left: 
                                        IParameterReferenceOperation: result1 (OperationKind.ParameterReference, Type: System.Boolean) (Syntax: 'result1')
                                      Right: 
                                        IParameterReferenceOperation: input1 (OperationKind.ParameterReference, Type: System.Boolean) (Syntax: 'input1')

                            Next (Regular) Block[B2#A0]
                        Block[B2#A0] - Exit
                            Predecessors: [B1#A0]
                            Statements (0)
                    }

        IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'd2 = (bool  ... 2 = input2;')
          Expression: 
            ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Action<System.Boolean, System.Boolean>) (Syntax: 'd2 = (bool  ... t2 = input2')
              Left: 
                IParameterReferenceOperation: d2 (OperationKind.ParameterReference, Type: System.Action<System.Boolean, System.Boolean>) (Syntax: 'd2')
              Right: 
                IDelegateCreationOperation (OperationKind.DelegateCreation, Type: System.Action<System.Boolean, System.Boolean>, IsImplicit) (Syntax: '(bool resul ... t2 = input2')
                  Target: 
                    IFlowAnonymousFunctionOperation (Symbol: lambda expression) (OperationKind.FlowAnonymousFunction, Type: null) (Syntax: '(bool resul ... t2 = input2')
                    {
                        Block[B0#A1] - Entry
                            Statements (0)
                            Next (Regular) Block[B1#A1]
                        Block[B1#A1] - Block
                            Predecessors: [B0#A1]
                            Statements (1)
                                IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null, IsImplicit) (Syntax: 'result2 = input2')
                                  Expression: 
                                    ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Boolean) (Syntax: 'result2 = input2')
                                      Left: 
                                        IParameterReferenceOperation: result2 (OperationKind.ParameterReference, Type: System.Boolean) (Syntax: 'result2')
                                      Right: 
                                        IParameterReferenceOperation: input2 (OperationKind.ParameterReference, Type: System.Boolean) (Syntax: 'input2')

                            Next (Regular) Block[B2#A1]
                        Block[B2#A1] - Exit
                            Predecessors: [B1#A1]
                            Statements (0)
                    }

    Next (Regular) Block[B2]
Block[B2] - Exit
    Predecessors: [B1]
    Statements (0)
";
            var expectedDiagnostics = DiagnosticDescription.None;

            VerifyFlowGraphAndDiagnosticsForTest<BlockSyntax>(source, expectedGraph, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation, CompilerFeature.Dataflow)]
        [Fact]
        public void LambdaFlow_03()
        {
            string source = @"
struct C
{
    void M(System.Action<int> d1, System.Action<bool> d2)
/*<bind>*/{
        int i = 0;

        d1 = (int input1) =>
        {
            input1 = 1;
            i++;

            d2 = (bool input2) =>
            {
                input2 = true;
                i++;
            };
        };
    }/*</bind>*/
}
";
            string expectedGraph = @"
Block[B0] - Entry
    Statements (0)
    Next (Regular) Block[B1]
        Entering: {R1}

.locals {R1}
{
    Locals: [System.Int32 i]
    Block[B1] - Block
        Predecessors: [B0]
        Statements (2)
            ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Int32, IsImplicit) (Syntax: 'i = 0')
              Left: 
                ILocalReferenceOperation: i (IsDeclaration: True) (OperationKind.LocalReference, Type: System.Int32, IsImplicit) (Syntax: 'i = 0')
              Right: 
                ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 0) (Syntax: '0')

            IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'd1 = (int i ... };')
              Expression: 
                ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Action<System.Int32>) (Syntax: 'd1 = (int i ... }')
                  Left: 
                    IParameterReferenceOperation: d1 (OperationKind.ParameterReference, Type: System.Action<System.Int32>) (Syntax: 'd1')
                  Right: 
                    IDelegateCreationOperation (OperationKind.DelegateCreation, Type: System.Action<System.Int32>, IsImplicit) (Syntax: '(int input1 ... }')
                      Target: 
                        IFlowAnonymousFunctionOperation (Symbol: lambda expression) (OperationKind.FlowAnonymousFunction, Type: null) (Syntax: '(int input1 ... }')
                        {
                            Block[B0#A0] - Entry
                                Statements (0)
                                Next (Regular) Block[B1#A0]
                            Block[B1#A0] - Block
                                Predecessors: [B0#A0]
                                Statements (3)
                                    IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'input1 = 1;')
                                      Expression: 
                                        ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Int32) (Syntax: 'input1 = 1')
                                          Left: 
                                            IParameterReferenceOperation: input1 (OperationKind.ParameterReference, Type: System.Int32) (Syntax: 'input1')
                                          Right: 
                                            ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 1) (Syntax: '1')

                                    IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'i++;')
                                      Expression: 
                                        IIncrementOrDecrementOperation (Postfix) (OperationKind.Increment, Type: System.Int32) (Syntax: 'i++')
                                          Target: 
                                            ILocalReferenceOperation: i (OperationKind.LocalReference, Type: System.Int32) (Syntax: 'i')

                                    IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'd2 = (bool  ... };')
                                      Expression: 
                                        ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Action<System.Boolean>) (Syntax: 'd2 = (bool  ... }')
                                          Left: 
                                            IParameterReferenceOperation: d2 (OperationKind.ParameterReference, Type: System.Action<System.Boolean>) (Syntax: 'd2')
                                          Right: 
                                            IDelegateCreationOperation (OperationKind.DelegateCreation, Type: System.Action<System.Boolean>, IsImplicit) (Syntax: '(bool input ... }')
                                              Target: 
                                                IFlowAnonymousFunctionOperation (Symbol: lambda expression) (OperationKind.FlowAnonymousFunction, Type: null) (Syntax: '(bool input ... }')
                                                {
                                                    Block[B0#A0#A0] - Entry
                                                        Statements (0)
                                                        Next (Regular) Block[B1#A0#A0]
                                                    Block[B1#A0#A0] - Block
                                                        Predecessors: [B0#A0#A0]
                                                        Statements (2)
                                                            IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'input2 = true;')
                                                              Expression: 
                                                                ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Boolean) (Syntax: 'input2 = true')
                                                                  Left: 
                                                                    IParameterReferenceOperation: input2 (OperationKind.ParameterReference, Type: System.Boolean) (Syntax: 'input2')
                                                                  Right: 
                                                                    ILiteralOperation (OperationKind.Literal, Type: System.Boolean, Constant: True) (Syntax: 'true')

                                                            IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'i++;')
                                                              Expression: 
                                                                IIncrementOrDecrementOperation (Postfix) (OperationKind.Increment, Type: System.Int32) (Syntax: 'i++')
                                                                  Target: 
                                                                    ILocalReferenceOperation: i (OperationKind.LocalReference, Type: System.Int32) (Syntax: 'i')

                                                        Next (Regular) Block[B2#A0#A0]
                                                    Block[B2#A0#A0] - Exit
                                                        Predecessors: [B1#A0#A0]
                                                        Statements (0)
                                                }

                                Next (Regular) Block[B2#A0]
                            Block[B2#A0] - Exit
                                Predecessors: [B1#A0]
                                Statements (0)
                        }

        Next (Regular) Block[B2]
            Leaving: {R1}
}

Block[B2] - Exit
    Predecessors: [B1]
    Statements (0)
";
            var expectedDiagnostics = DiagnosticDescription.None;

            VerifyFlowGraphAndDiagnosticsForTest<BlockSyntax>(source, expectedGraph, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation, CompilerFeature.Dataflow)]
        [Fact]
        public void LambdaFlow_04()
        {
            string source = @"
struct C
{
    void M(System.Action d1, System.Action<bool, bool> d2)
/*<bind>*/{
        d1 = () =>
        {
            d2 = (bool result1, bool input1) =>
            {
                result1 = input1;
            
            };
        };

        void local(bool result2, bool input2) => result2 = input2;
    }/*</bind>*/
}
";

            var compilation = CreateCompilation(source);
            var tree = compilation.SyntaxTrees.Single();
            var semanticModel = compilation.GetSemanticModel(tree);
            var graphM = ControlFlowGraph.Create((IMethodBodyOperation)semanticModel.GetOperation(tree.GetRoot().DescendantNodes().OfType<MethodDeclarationSyntax>().Single()));

            Assert.NotNull(graphM);
            Assert.Null(graphM.Parent);

            IFlowAnonymousFunctionOperation lambdaD1 = getLambda(graphM);

            Assert.Throws<ArgumentNullException>(() => graphM.GetLocalFunctionControlFlowGraph(null));
            Assert.Throws<ArgumentOutOfRangeException>(() => graphM.GetLocalFunctionControlFlowGraph(lambdaD1.Symbol));
            Assert.Throws<ArgumentNullException>(() => graphM.GetLocalFunctionControlFlowGraphInScope(null));
            Assert.Throws<ArgumentOutOfRangeException>(() => graphM.GetLocalFunctionControlFlowGraphInScope(lambdaD1.Symbol));

            var graphD1 = graphM.GetAnonymousFunctionControlFlowGraph(lambdaD1);
            Assert.NotNull(graphD1);
            Assert.Same(graphM, graphD1.Parent);
            var graphD1_FromExtension = graphM.GetAnonymousFunctionControlFlowGraphInScope(lambdaD1);
            Assert.Same(graphD1, graphD1_FromExtension);

            IFlowAnonymousFunctionOperation lambdaD2 = getLambda(graphD1);
            var graphD2 = graphD1.GetAnonymousFunctionControlFlowGraph(lambdaD2);
            Assert.NotNull(graphD2);
            Assert.Same(graphD1, graphD2.Parent);

            Assert.Throws<ArgumentNullException>(() => graphM.GetAnonymousFunctionControlFlowGraph(null));
            Assert.Throws<ArgumentOutOfRangeException>(() => graphM.GetAnonymousFunctionControlFlowGraph(lambdaD2));
            Assert.Throws<ArgumentNullException>(() => graphM.GetAnonymousFunctionControlFlowGraphInScope(null));
            Assert.Throws<ArgumentOutOfRangeException>(() => graphM.GetAnonymousFunctionControlFlowGraphInScope(lambdaD2));

            IFlowAnonymousFunctionOperation getLambda(ControlFlowGraph graph)
            {
                return graph.Blocks.SelectMany(b => b.Operations.SelectMany(o => o.DescendantsAndSelf())).OfType<IFlowAnonymousFunctionOperation>().Single();
            }
        }

        [CompilerTrait(CompilerFeature.IOperation, CompilerFeature.Dataflow)]
        [Fact]
        public void LambdaFlow_05()
        {
            string source = @"
struct C
{
    void M(System.Action d1, System.Action d2)
/*<bind>*/{
        d1 = () => { };
        d2 = () =>
        {
            d1();
        };
    }/*</bind>*/
}
";

            var compilation = CreateCompilation(source);
            var tree = compilation.SyntaxTrees.Single();
            var semanticModel = compilation.GetSemanticModel(tree);
            var graphM = ControlFlowGraph.Create((IMethodBodyOperation)semanticModel.GetOperation(tree.GetRoot().DescendantNodes().OfType<MethodDeclarationSyntax>().Single()));

            Assert.NotNull(graphM);
            Assert.Null(graphM.Parent);

            IFlowAnonymousFunctionOperation lambdaD1 = getLambda(graphM, index: 0);
            Assert.NotNull(lambdaD1);
            IFlowAnonymousFunctionOperation lambdaD2 = getLambda(graphM, index: 1);
            Assert.NotNull(lambdaD2);

            Assert.Throws<ArgumentNullException>(() => graphM.GetLocalFunctionControlFlowGraph(null));
            Assert.Throws<ArgumentOutOfRangeException>(() => graphM.GetLocalFunctionControlFlowGraph(lambdaD1.Symbol));
            Assert.Throws<ArgumentNullException>(() => graphM.GetLocalFunctionControlFlowGraphInScope(null));
            Assert.Throws<ArgumentOutOfRangeException>(() => graphM.GetLocalFunctionControlFlowGraphInScope(lambdaD1.Symbol));

            var graphD1 = graphM.GetAnonymousFunctionControlFlowGraph(lambdaD1);
            Assert.NotNull(graphD1);
            Assert.Same(graphM, graphD1.Parent);
            var graphD2 = graphM.GetAnonymousFunctionControlFlowGraph(lambdaD2);
            Assert.NotNull(graphD2);
            Assert.Same(graphM, graphD2.Parent);

            var graphD1_FromExtension = graphM.GetAnonymousFunctionControlFlowGraphInScope(lambdaD1);
            Assert.Same(graphD1, graphD1_FromExtension);

            Assert.Throws<ArgumentOutOfRangeException>(() => graphD2.GetAnonymousFunctionControlFlowGraph(lambdaD1));
            graphD1_FromExtension = graphD2.GetAnonymousFunctionControlFlowGraphInScope(lambdaD1);
            Assert.Same(graphD1, graphD1_FromExtension);

            IFlowAnonymousFunctionOperation getLambda(ControlFlowGraph graph, int index)
            {
                return graph.Blocks.SelectMany(b => b.Operations.SelectMany(o => o.DescendantsAndSelf())).OfType<IFlowAnonymousFunctionOperation>().ElementAt(index);
            }
        }
    }
}
