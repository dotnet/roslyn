// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.CSharp.Test.Utilities;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.CSharp.UnitTests
{
    public class IOperationTests_IAwaitExpression : SemanticModelTestBase
    {
        [CompilerTrait(CompilerFeature.IOperation)]
        [Fact]
        public void TestAwaitExpression()
        {
            string source = @"
using System.Threading.Tasks;

class C
{
    static async Task M()
    {
        /*<bind>*/await M2()/*</bind>*/;
    }

    static Task M2() => null;
}
";
            string expectedOperationTree = @"
IAwaitOperation (OperationKind.Await, Type: System.Void) (Syntax: 'await M2()')
  Expression: 
    IInvocationOperation (System.Threading.Tasks.Task C.M2()) (OperationKind.Invocation, Type: System.Threading.Tasks.Task) (Syntax: 'M2()')
      Instance Receiver: 
        null
      Arguments(0)
";
            var expectedDiagnostics = DiagnosticDescription.None;

            VerifyOperationTreeAndDiagnosticsForTest<AwaitExpressionSyntax>(source, expectedOperationTree, expectedDiagnostics, targetFramework: TargetFramework.Mscorlib46Extended);
        }

        [CompilerTrait(CompilerFeature.IOperation)]
        [Fact]
        public void TestAwaitExpression_ParameterReference()
        {
            string source = @"
using System.Threading.Tasks;

class C
{
    static async Task M(Task t)
    {
        /*<bind>*/await t/*</bind>*/;
    }
}
";
            string expectedOperationTree = @"
IAwaitOperation (OperationKind.Await, Type: System.Void) (Syntax: 'await t')
  Expression: 
    IParameterReferenceOperation: t (OperationKind.ParameterReference, Type: System.Threading.Tasks.Task) (Syntax: 't')
";
            var expectedDiagnostics = DiagnosticDescription.None;

            VerifyOperationTreeAndDiagnosticsForTest<AwaitExpressionSyntax>(source, expectedOperationTree, expectedDiagnostics, targetFramework: TargetFramework.Mscorlib46Extended);
        }

        [CompilerTrait(CompilerFeature.IOperation)]
        [Fact]
        public void TestAwaitExpression_InLambda()
        {
            string source = @"
using System;
using System.Threading.Tasks;

class C
{
    static async Task M(Task<int> t)
    {
        Func<Task> f = async () => /*<bind>*/await t/*</bind>*/;
        await f();
    }
}
";
            string expectedOperationTree = @"
IAwaitOperation (OperationKind.Await, Type: System.Int32) (Syntax: 'await t')
  Expression: 
    IParameterReferenceOperation: t (OperationKind.ParameterReference, Type: System.Threading.Tasks.Task<System.Int32>) (Syntax: 't')
";
            var expectedDiagnostics = DiagnosticDescription.None;

            VerifyOperationTreeAndDiagnosticsForTest<AwaitExpressionSyntax>(source, expectedOperationTree, expectedDiagnostics, targetFramework: TargetFramework.Mscorlib46Extended);
        }

        [CompilerTrait(CompilerFeature.IOperation)]
        [Fact]
        public void TestAwaitExpression_ErrorArgument()
        {
            string source = @"
using System;
using System.Threading.Tasks;

class C
{
    static async Task M()
    {
        /*<bind>*/await UndefinedTask/*</bind>*/;
    }
}
";
            string expectedOperationTree = @"
IAwaitOperation (OperationKind.Await, Type: ?, IsInvalid) (Syntax: 'await UndefinedTask')
  Expression: 
    IInvalidOperation (OperationKind.Invalid, Type: ?, IsInvalid) (Syntax: 'UndefinedTask')
      Children(0)
";
            var expectedDiagnostics = new DiagnosticDescription[] {
                // CS0103: The name 'UndefinedTask' does not exist in the current context
                //         /*<bind>*/await UndefinedTask/*</bind>*/;
                Diagnostic(ErrorCode.ERR_NameNotInContext, "UndefinedTask").WithArguments("UndefinedTask").WithLocation(9, 25)
            };

            VerifyOperationTreeAndDiagnosticsForTest<AwaitExpressionSyntax>(source, expectedOperationTree, expectedDiagnostics, targetFramework: TargetFramework.Mscorlib46Extended);
        }

        [CompilerTrait(CompilerFeature.IOperation)]
        [Fact]
        public void TestAwaitExpression_ValueArgument()
        {
            string source = @"
using System;
using System.Threading.Tasks;

class C
{
    static async Task M(int i)
    {
        /*<bind>*/await i/*</bind>*/;
    }
}
";
            string expectedOperationTree = @"
IAwaitOperation (OperationKind.Await, Type: ?, IsInvalid) (Syntax: 'await i')
  Expression: 
    IParameterReferenceOperation: i (OperationKind.ParameterReference, Type: System.Int32, IsInvalid) (Syntax: 'i')
";
            var expectedDiagnostics = new DiagnosticDescription[] {
                // CS1061: 'int' does not contain a definition for 'GetAwaiter' and no extension method 'GetAwaiter' accepting a first argument of type 'int' could be found (are you missing a using directive or an assembly reference?)
                //         /*<bind>*/await i/*</bind>*/;
                Diagnostic(ErrorCode.ERR_NoSuchMemberOrExtension, "await i").WithArguments("int", "GetAwaiter").WithLocation(9, 19)
            };

            VerifyOperationTreeAndDiagnosticsForTest<AwaitExpressionSyntax>(source, expectedOperationTree, expectedDiagnostics, targetFramework: TargetFramework.Mscorlib46Extended);
        }

        [CompilerTrait(CompilerFeature.IOperation)]
        [Fact]
        public void TestAwaitExpression_MissingArgument()
        {
            string source = @"
using System;
using System.Threading.Tasks;

class C
{
    static async Task M()
    {
        /*<bind>*/await /*</bind>*/;
    }
}
";
            string expectedOperationTree = @"
IAwaitOperation (OperationKind.Await, Type: ?, IsInvalid) (Syntax: 'await /*</bind>*/')
  Expression: 
    IInvalidOperation (OperationKind.Invalid, Type: null, IsInvalid) (Syntax: '')
      Children(0)
";
            var expectedDiagnostics = new DiagnosticDescription[] {
                // CS1525: Invalid expression term ';'
                //         /*<bind>*/await /*</bind>*/;
                Diagnostic(ErrorCode.ERR_InvalidExprTerm, ";").WithArguments(";").WithLocation(9, 36)
            };

            VerifyOperationTreeAndDiagnosticsForTest<AwaitExpressionSyntax>(source, expectedOperationTree, expectedDiagnostics, targetFramework: TargetFramework.Mscorlib46Extended);
        }

        [CompilerTrait(CompilerFeature.IOperation)]
        [Fact]
        public void TestAwaitExpression_NonAsyncMethod()
        {
            string source = @"
using System;
using System.Threading.Tasks;

class C
{
    static void M(Task<int> t)
    {
        /*<bind>*/await t;/*</bind>*/
    }
}
";
            string expectedOperationTree = @"
IVariableDeclarationGroupOperation (1 declarations) (OperationKind.VariableDeclarationGroup, Type: null, IsInvalid) (Syntax: 'await t;')
  IVariableDeclarationOperation (1 declarators) (OperationKind.VariableDeclaration, Type: null, IsInvalid) (Syntax: 'await t')
    Declarators:
        IVariableDeclaratorOperation (Symbol: await t) (OperationKind.VariableDeclarator, Type: null, IsInvalid) (Syntax: 't')
          Initializer: 
            null
    Initializer: 
      null
";
            var expectedDiagnostics = new DiagnosticDescription[] {
                // CS0246: The type or namespace name 'await' could not be found (are you missing a using directive or an assembly reference?)
                //         /*<bind>*/await t;/*</bind>*/
                Diagnostic(ErrorCode.ERR_SingleTypeNameNotFound, "await").WithArguments("await").WithLocation(9, 19),
                // CS0136: A local or parameter named 't' cannot be declared in this scope because that name is used in an enclosing local scope to define a local or parameter
                //         /*<bind>*/await t;/*</bind>*/
                Diagnostic(ErrorCode.ERR_LocalIllegallyOverrides, "t").WithArguments("t").WithLocation(9, 25),
                // CS0168: The variable 't' is declared but never used
                //         /*<bind>*/await t;/*</bind>*/
                Diagnostic(ErrorCode.WRN_UnreferencedVar, "t").WithArguments("t").WithLocation(9, 25)
            };

            VerifyOperationTreeAndDiagnosticsForTest<LocalDeclarationStatementSyntax>(source, expectedOperationTree, expectedDiagnostics, targetFramework: TargetFramework.Mscorlib46Extended);
        }

        [CompilerTrait(CompilerFeature.IOperation, CompilerFeature.Dataflow, CompilerFeature.AsyncStreams)]
        [Fact]
        public void AwaitFlow_AsyncIterator()
        {
            string source = @"
using System.Threading.Tasks;

class C
{
    static async System.Collections.Generic.IAsyncEnumerable<int> M()
    /*<bind>*/{
        await M2();
        yield return 42;
    }/*</bind>*/

    static Task M2() => null;
}
";
            var expectedDiagnostics = new[] {
                // file.cs(24,32): error CS0234: The type or namespace name 'ValueTask<>' does not exist in the namespace 'System.Threading.Tasks' (are you missing an assembly reference?)
                //         System.Threading.Tasks.ValueTask<bool> MoveNextAsync();
                Diagnostic(ErrorCode.ERR_DottedTypeNameNotFoundInNS, "ValueTask<bool>").WithArguments("ValueTask<>", "System.Threading.Tasks").WithLocation(24, 32),
                // file.cs(32,32): error CS0234: The type or namespace name 'ValueTask' does not exist in the namespace 'System.Threading.Tasks' (are you missing an assembly reference?)
                //         System.Threading.Tasks.ValueTask DisposeAsync();
                Diagnostic(ErrorCode.ERR_DottedTypeNameNotFoundInNS, "ValueTask").WithArguments("ValueTask", "System.Threading.Tasks").WithLocation(32, 32)
            };

            string expectedFlowGraph = @"
Block[B0] - Entry
    Statements (0)
    Next (Regular) Block[B1]
Block[B1] - Block
    Predecessors: [B0]
    Statements (2)
        IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'await M2();')
          Expression: 
            IAwaitOperation (OperationKind.Await, Type: System.Void) (Syntax: 'await M2()')
              Expression: 
                IInvocationOperation (System.Threading.Tasks.Task C.M2()) (OperationKind.Invocation, Type: System.Threading.Tasks.Task) (Syntax: 'M2()')
                  Instance Receiver: 
                    null
                  Arguments(0)

        IReturnOperation (OperationKind.YieldReturn, Type: null) (Syntax: 'yield return 42;')
          ReturnedValue: 
            ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 42) (Syntax: '42')

    Next (Regular) Block[B2]
Block[B2] - Exit
    Predecessors: [B1]
    Statements (0)
";
            VerifyFlowGraphAndDiagnosticsForTest<BlockSyntax>(source + s_IAsyncEnumerable, expectedFlowGraph, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation, CompilerFeature.Dataflow)]
        [Fact]
        public void AwaitFlow_01()
        {
            string source = @"
using System.Threading.Tasks;

class C
{
    static async Task M()
    /*<bind>*/{

        await M2();
    }/*</bind>*/

    static Task M2() => null;
}
";
            var expectedDiagnostics = DiagnosticDescription.None;

            string expectedFlowGraph = @"
Block[B0] - Entry
    Statements (0)
    Next (Regular) Block[B1]
Block[B1] - Block
    Predecessors: [B0]
    Statements (1)
        IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'await M2();')
          Expression: 
            IAwaitOperation (OperationKind.Await, Type: System.Void) (Syntax: 'await M2()')
              Expression: 
                IInvocationOperation (System.Threading.Tasks.Task C.M2()) (OperationKind.Invocation, Type: System.Threading.Tasks.Task) (Syntax: 'M2()')
                  Instance Receiver: 
                    null
                  Arguments(0)

    Next (Regular) Block[B2]
Block[B2] - Exit
    Predecessors: [B1]
    Statements (0)
";
            VerifyFlowGraphAndDiagnosticsForTest<BlockSyntax>(source, expectedFlowGraph, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation, CompilerFeature.Dataflow)]
        [Fact]
        public void AwaitFlow_02()
        {
            string source = @"
using System.Threading.Tasks;

class C
{
    static async Task M(bool b, int i)
    /*<bind>*/{

        i = b ? await M2(2) : await M2(3);
    }/*</bind>*/

    static Task<int> M2(int i) => Task.FromResult<int>(i);
}
";
            var expectedDiagnostics = DiagnosticDescription.None;

            string expectedFlowGraph = @"
Block[B0] - Entry
    Statements (0)
    Next (Regular) Block[B1]
        Entering: {R1}

.locals {R1}
{
    CaptureIds: [0] [1]
    Block[B1] - Block
        Predecessors: [B0]
        Statements (1)
            IFlowCaptureOperation: 0 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'i')
              Value: 
                IParameterReferenceOperation: i (OperationKind.ParameterReference, Type: System.Int32) (Syntax: 'i')

        Jump if False (Regular) to Block[B3]
            IParameterReferenceOperation: b (OperationKind.ParameterReference, Type: System.Boolean) (Syntax: 'b')

        Next (Regular) Block[B2]
    Block[B2] - Block
        Predecessors: [B1]
        Statements (1)
            IFlowCaptureOperation: 1 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'await M2(2)')
              Value: 
                IAwaitOperation (OperationKind.Await, Type: System.Int32) (Syntax: 'await M2(2)')
                  Expression: 
                    IInvocationOperation (System.Threading.Tasks.Task<System.Int32> C.M2(System.Int32 i)) (OperationKind.Invocation, Type: System.Threading.Tasks.Task<System.Int32>) (Syntax: 'M2(2)')
                      Instance Receiver: 
                        null
                      Arguments(1):
                          IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: i) (OperationKind.Argument, Type: null) (Syntax: '2')
                            ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 2) (Syntax: '2')
                            InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                            OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)

        Next (Regular) Block[B4]
    Block[B3] - Block
        Predecessors: [B1]
        Statements (1)
            IFlowCaptureOperation: 1 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'await M2(3)')
              Value: 
                IAwaitOperation (OperationKind.Await, Type: System.Int32) (Syntax: 'await M2(3)')
                  Expression: 
                    IInvocationOperation (System.Threading.Tasks.Task<System.Int32> C.M2(System.Int32 i)) (OperationKind.Invocation, Type: System.Threading.Tasks.Task<System.Int32>) (Syntax: 'M2(3)')
                      Instance Receiver: 
                        null
                      Arguments(1):
                          IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: i) (OperationKind.Argument, Type: null) (Syntax: '3')
                            ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 3) (Syntax: '3')
                            InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                            OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)

        Next (Regular) Block[B4]
    Block[B4] - Block
        Predecessors: [B2] [B3]
        Statements (1)
            IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'i = b ? awa ... wait M2(3);')
              Expression: 
                ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Int32) (Syntax: 'i = b ? awa ... await M2(3)')
                  Left: 
                    IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: System.Int32, IsImplicit) (Syntax: 'i')
                  Right: 
                    IFlowCaptureReferenceOperation: 1 (OperationKind.FlowCaptureReference, Type: System.Int32, IsImplicit) (Syntax: 'b ? await M ... await M2(3)')

        Next (Regular) Block[B5]
            Leaving: {R1}
}

Block[B5] - Exit
    Predecessors: [B4]
    Statements (0)
";
            VerifyFlowGraphAndDiagnosticsForTest<BlockSyntax>(source, expectedFlowGraph, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation, CompilerFeature.Dataflow)]
        [Fact]
        public void AwaitFlow_03()
        {
            string source = @"
using System.Threading.Tasks;

class C
{
    static async Task M(bool b, int i)
    /*<bind>*/{

        i = await M2(b ? 2 : 3);
    }/*</bind>*/

    static Task<int> M2(int i) => Task.FromResult<int>(i);
}
";
            var expectedDiagnostics = DiagnosticDescription.None;

            string expectedFlowGraph = @"
Block[B0] - Entry
    Statements (0)
    Next (Regular) Block[B1]
        Entering: {R1}

.locals {R1}
{
    CaptureIds: [0] [1]
    Block[B1] - Block
        Predecessors: [B0]
        Statements (1)
            IFlowCaptureOperation: 0 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'i')
              Value: 
                IParameterReferenceOperation: i (OperationKind.ParameterReference, Type: System.Int32) (Syntax: 'i')

        Jump if False (Regular) to Block[B3]
            IParameterReferenceOperation: b (OperationKind.ParameterReference, Type: System.Boolean) (Syntax: 'b')

        Next (Regular) Block[B2]
    Block[B2] - Block
        Predecessors: [B1]
        Statements (1)
            IFlowCaptureOperation: 1 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: '2')
              Value: 
                ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 2) (Syntax: '2')

        Next (Regular) Block[B4]
    Block[B3] - Block
        Predecessors: [B1]
        Statements (1)
            IFlowCaptureOperation: 1 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: '3')
              Value: 
                ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 3) (Syntax: '3')

        Next (Regular) Block[B4]
    Block[B4] - Block
        Predecessors: [B2] [B3]
        Statements (1)
            IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'i = await M2(b ? 2 : 3);')
              Expression: 
                ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Int32) (Syntax: 'i = await M2(b ? 2 : 3)')
                  Left: 
                    IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: System.Int32, IsImplicit) (Syntax: 'i')
                  Right: 
                    IAwaitOperation (OperationKind.Await, Type: System.Int32) (Syntax: 'await M2(b ? 2 : 3)')
                      Expression: 
                        IInvocationOperation (System.Threading.Tasks.Task<System.Int32> C.M2(System.Int32 i)) (OperationKind.Invocation, Type: System.Threading.Tasks.Task<System.Int32>) (Syntax: 'M2(b ? 2 : 3)')
                          Instance Receiver: 
                            null
                          Arguments(1):
                              IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: i) (OperationKind.Argument, Type: null) (Syntax: 'b ? 2 : 3')
                                IFlowCaptureReferenceOperation: 1 (OperationKind.FlowCaptureReference, Type: System.Int32, IsImplicit) (Syntax: 'b ? 2 : 3')
                                InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                                OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)

        Next (Regular) Block[B5]
            Leaving: {R1}
}

Block[B5] - Exit
    Predecessors: [B4]
    Statements (0)
";
            VerifyFlowGraphAndDiagnosticsForTest<BlockSyntax>(source, expectedFlowGraph, expectedDiagnostics);
        }
    }
}
