// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.CSharp.Test.Utilities;
using Microsoft.CodeAnalysis.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.CSharp.UnitTests
{
    public partial class IOperationTests : SemanticModelTestBase
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
IAwaitExpression (OperationKind.AwaitExpression, Type: System.Void) (Syntax: 'await M2()')
  Expression: 
    IInvocationExpression (System.Threading.Tasks.Task C.M2()) (OperationKind.InvocationExpression, Type: System.Threading.Tasks.Task) (Syntax: 'M2()')
      Instance Receiver: 
        null
      Arguments(0)
";
            var expectedDiagnostics = DiagnosticDescription.None;

            VerifyOperationTreeAndDiagnosticsForTest<AwaitExpressionSyntax>(source, expectedOperationTree, expectedDiagnostics, useLatestFrameworkReferences: true);
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
IAwaitExpression (OperationKind.AwaitExpression, Type: System.Void) (Syntax: 'await t')
  Expression: 
    IParameterReferenceExpression: t (OperationKind.ParameterReferenceExpression, Type: System.Threading.Tasks.Task) (Syntax: 't')
";
            var expectedDiagnostics = DiagnosticDescription.None;

            VerifyOperationTreeAndDiagnosticsForTest<AwaitExpressionSyntax>(source, expectedOperationTree, expectedDiagnostics, useLatestFrameworkReferences: true);
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
IAwaitExpression (OperationKind.AwaitExpression, Type: System.Int32) (Syntax: 'await t')
  Expression: 
    IParameterReferenceExpression: t (OperationKind.ParameterReferenceExpression, Type: System.Threading.Tasks.Task<System.Int32>) (Syntax: 't')
";
            var expectedDiagnostics = DiagnosticDescription.None;

            VerifyOperationTreeAndDiagnosticsForTest<AwaitExpressionSyntax>(source, expectedOperationTree, expectedDiagnostics, useLatestFrameworkReferences: true);
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
IAwaitExpression (OperationKind.AwaitExpression, Type: ?, IsInvalid) (Syntax: 'await UndefinedTask')
  Expression: 
    IInvalidExpression (OperationKind.InvalidExpression, Type: ?, IsInvalid) (Syntax: 'UndefinedTask')
      Children(0)
";
            var expectedDiagnostics = new DiagnosticDescription[] {
                // CS0103: The name 'UndefinedTask' does not exist in the current context
                //         /*<bind>*/await UndefinedTask/*</bind>*/;
                Diagnostic(ErrorCode.ERR_NameNotInContext, "UndefinedTask").WithArguments("UndefinedTask").WithLocation(9, 25)
            };

            VerifyOperationTreeAndDiagnosticsForTest<AwaitExpressionSyntax>(source, expectedOperationTree, expectedDiagnostics, useLatestFrameworkReferences: true);
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
IAwaitExpression (OperationKind.AwaitExpression, Type: ?, IsInvalid) (Syntax: 'await i')
  Expression: 
    IParameterReferenceExpression: i (OperationKind.ParameterReferenceExpression, Type: System.Int32, IsInvalid) (Syntax: 'i')
";
            var expectedDiagnostics = new DiagnosticDescription[] {
                // CS1061: 'int' does not contain a definition for 'GetAwaiter' and no extension method 'GetAwaiter' accepting a first argument of type 'int' could be found (are you missing a using directive or an assembly reference?)
                //         /*<bind>*/await i/*</bind>*/;
                Diagnostic(ErrorCode.ERR_NoSuchMemberOrExtension, "await i").WithArguments("int", "GetAwaiter").WithLocation(9, 19)
            };

            VerifyOperationTreeAndDiagnosticsForTest<AwaitExpressionSyntax>(source, expectedOperationTree, expectedDiagnostics, useLatestFrameworkReferences: true);
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
IAwaitExpression (OperationKind.AwaitExpression, Type: ?, IsInvalid) (Syntax: 'await /*</bind>*/')
  Expression: 
    IInvalidExpression (OperationKind.InvalidExpression, Type: null, IsInvalid) (Syntax: '')
      Children(0)
";
            var expectedDiagnostics = new DiagnosticDescription[] {
                // CS1525: Invalid expression term ';'
                //         /*<bind>*/await /*</bind>*/;
                Diagnostic(ErrorCode.ERR_InvalidExprTerm, ";").WithArguments(";").WithLocation(9, 36)
            };

            VerifyOperationTreeAndDiagnosticsForTest<AwaitExpressionSyntax>(source, expectedOperationTree, expectedDiagnostics, useLatestFrameworkReferences: true);
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
IVariableDeclarationStatement (1 declarations) (OperationKind.VariableDeclarationStatement, IsInvalid) (Syntax: 'await t;')
  IVariableDeclaration (1 variables) (OperationKind.VariableDeclaration, IsInvalid) (Syntax: 't')
    Variables: Local_1: await t
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

            VerifyOperationTreeAndDiagnosticsForTest<LocalDeclarationStatementSyntax>(source, expectedOperationTree, expectedDiagnostics, useLatestFrameworkReferences: true);
        }
    }
}
