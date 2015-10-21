// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.CSharp.UnitTests
{
    public partial class IOperationTests : SemanticModelTestBase
    {
        [Fact]
        public void TestLocalFunction_ContainingMethodParameterReference()
        {
            string source = @"
class C
{
    public void M(int x)
    {
        /*<bind>*/int Local(int p1)
        {
            return x++;
        }/*</bind>*/

        Local(0);
    }
}
";
            string expectedOperationTree = @"
ILocalFunctionStatement (Local Function: System.Int32 Local(System.Int32 p1)) (OperationKind.LocalFunctionStatement) (Syntax: 'int Local(i ... }')
  IBlockStatement (1 statements) (OperationKind.BlockStatement) (Syntax: '{ ... }')
    IReturnStatement (OperationKind.ReturnStatement) (Syntax: 'return x++;')
      IIncrementExpression (UnaryOperandKind.IntegerPostfixIncrement) (OperationKind.IncrementExpression, Type: System.Int32) (Syntax: 'x++')
        Left: IParameterReferenceExpression: x (OperationKind.ParameterReferenceExpression, Type: System.Int32) (Syntax: 'x')
";
            var expectedDiagnostics = DiagnosticDescription.None;

            VerifyOperationTreeAndDiagnosticsForTest<LocalFunctionStatementSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }

        [Fact]
        public void TestLocalFunction_ContainingMethodParameterReference_ExpressionBodied()
        {
            string source = @"
class C
{
    public void M(int x)
    {
        /*<bind>*/int Local(int p1) => x++;/*</bind>*/
        Local(0);
    }
}
";
            string expectedOperationTree = @"
ILocalFunctionStatement (Local Function: System.Int32 Local(System.Int32 p1)) (OperationKind.LocalFunctionStatement) (Syntax: 'int Local(i ... p1) => x++;')
  IBlockStatement (1 statements) (OperationKind.BlockStatement) (Syntax: '=> x++')
    IReturnStatement (OperationKind.ReturnStatement) (Syntax: 'x++')
      IIncrementExpression (UnaryOperandKind.IntegerPostfixIncrement) (OperationKind.IncrementExpression, Type: System.Int32) (Syntax: 'x++')
        Left: IParameterReferenceExpression: x (OperationKind.ParameterReferenceExpression, Type: System.Int32) (Syntax: 'x')
";
            var expectedDiagnostics = DiagnosticDescription.None;

            VerifyOperationTreeAndDiagnosticsForTest<LocalFunctionStatementSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }

        [Fact]
        public void TestLocalFunction_LocalFunctionParameterReference()
        {
            string source = @"
class C
{
    public void M()
    {
        /*<bind>*/int Local(int x) => x++;/*</bind>*/
        Local(0);
    }
}
";
            string expectedOperationTree = @"
ILocalFunctionStatement (Local Function: System.Int32 Local(System.Int32 x)) (OperationKind.LocalFunctionStatement) (Syntax: 'int Local(int x) => x++;')
  IBlockStatement (1 statements) (OperationKind.BlockStatement) (Syntax: '=> x++')
    IReturnStatement (OperationKind.ReturnStatement) (Syntax: 'x++')
      IIncrementExpression (UnaryOperandKind.IntegerPostfixIncrement) (OperationKind.IncrementExpression, Type: System.Int32) (Syntax: 'x++')
        Left: IParameterReferenceExpression: x (OperationKind.ParameterReferenceExpression, Type: System.Int32) (Syntax: 'x')
";
            var expectedDiagnostics = DiagnosticDescription.None;

            VerifyOperationTreeAndDiagnosticsForTest<LocalFunctionStatementSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }

        [Fact]
        public void TestLocalFunction_ContainingLocalFunctionParameterReference()
        {
            string source = @"
class C
{
    public void M()
    {
        int LocalOuter (int x)
        {
            /*<bind>*/int Local(int y) => x + y;/*</bind>*/
            return Local(x);
        }

        LocalOuter(0);
    }
}
";
            string expectedOperationTree = @"
ILocalFunctionStatement (Local Function: System.Int32 Local(System.Int32 y)) (OperationKind.LocalFunctionStatement) (Syntax: 'int Local(i ... ) => x + y;')
  IBlockStatement (1 statements) (OperationKind.BlockStatement) (Syntax: '=> x + y')
    IReturnStatement (OperationKind.ReturnStatement) (Syntax: 'x + y')
      IBinaryOperatorExpression (BinaryOperationKind.IntegerAdd) (OperationKind.BinaryOperatorExpression, Type: System.Int32) (Syntax: 'x + y')
        Left: IParameterReferenceExpression: x (OperationKind.ParameterReferenceExpression, Type: System.Int32) (Syntax: 'x')
        Right: IParameterReferenceExpression: y (OperationKind.ParameterReferenceExpression, Type: System.Int32) (Syntax: 'y')
";
            var expectedDiagnostics = DiagnosticDescription.None;

            VerifyOperationTreeAndDiagnosticsForTest<LocalFunctionStatementSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }

        [Fact]
        public void TestLocalFunction_LocalFunctionReference()
        {
            string source = @"
class C
{
    public void M()
    {
        int x;
        int Local(int p1) => x++;
        int Local2(int p1) => Local(p1);
        /*<bind>*/int Local3(int p1) => x + Local2(p1);/*</bind>*/

        Local3(x = 0);
    }
}
";
string expectedOperationTree = @"
ILocalFunctionStatement (Local Function: System.Int32 Local3(System.Int32 p1)) (OperationKind.LocalFunctionStatement) (Syntax: 'int Local3( ... Local2(p1);')
  IBlockStatement (1 statements) (OperationKind.BlockStatement) (Syntax: '=> x + Local2(p1)')
    IReturnStatement (OperationKind.ReturnStatement) (Syntax: 'x + Local2(p1)')
      IBinaryOperatorExpression (BinaryOperationKind.IntegerAdd) (OperationKind.BinaryOperatorExpression, Type: System.Int32) (Syntax: 'x + Local2(p1)')
        Left: ILocalReferenceExpression: x (OperationKind.LocalReferenceExpression, Type: System.Int32) (Syntax: 'x')
        Right: IInvocationExpression (System.Int32 Local2(System.Int32 p1)) (OperationKind.InvocationExpression, Type: System.Int32) (Syntax: 'Local2(p1)')
            Instance Receiver: null
            Arguments(1):
                IArgument (ArgumentKind.Explicit, Matching Parameter: p1) (OperationKind.Argument) (Syntax: 'p1')
                  IParameterReferenceExpression: p1 (OperationKind.ParameterReferenceExpression, Type: System.Int32) (Syntax: 'p1')
                  InConversion: null
                  OutConversion: null
";
            var expectedDiagnostics = DiagnosticDescription.None;

            VerifyOperationTreeAndDiagnosticsForTest<LocalFunctionStatementSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }

        [Fact]
        public void TestLocalFunction_Recursion()
        {
            string source = @"
class C
{
    public void M(int x)
    {
        /*<bind>*/int Local(int p1) => Local(x + p1);/*</bind>*/
    }
}
";
string expectedOperationTree = @"
ILocalFunctionStatement (Local Function: System.Int32 Local(System.Int32 p1)) (OperationKind.LocalFunctionStatement) (Syntax: 'int Local(i ... al(x + p1);')
  IBlockStatement (1 statements) (OperationKind.BlockStatement) (Syntax: '=> Local(x + p1)')
    IReturnStatement (OperationKind.ReturnStatement) (Syntax: 'Local(x + p1)')
      IInvocationExpression (System.Int32 Local(System.Int32 p1)) (OperationKind.InvocationExpression, Type: System.Int32) (Syntax: 'Local(x + p1)')
        Instance Receiver: null
        Arguments(1):
            IArgument (ArgumentKind.Explicit, Matching Parameter: p1) (OperationKind.Argument) (Syntax: 'x + p1')
              IBinaryOperatorExpression (BinaryOperationKind.IntegerAdd) (OperationKind.BinaryOperatorExpression, Type: System.Int32) (Syntax: 'x + p1')
                Left: IParameterReferenceExpression: x (OperationKind.ParameterReferenceExpression, Type: System.Int32) (Syntax: 'x')
                Right: IParameterReferenceExpression: p1 (OperationKind.ParameterReferenceExpression, Type: System.Int32) (Syntax: 'p1')
              InConversion: null
              OutConversion: null
";
            var expectedDiagnostics = DiagnosticDescription.None;

            VerifyOperationTreeAndDiagnosticsForTest<LocalFunctionStatementSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }

        [Fact]
        public void TestLocalFunction_Async()
        {
            string source = @"
using System.Threading.Tasks;

class C
{
    public void M(int x)
    {
        /*<bind>*/async Task<int> LocalAsync(int p1)
        {
            await Task.Delay(0);
            return x + p1;
        }/*</bind>*/

        LocalAsync(0).Wait();
    }
}
";
string expectedOperationTree = @"
ILocalFunctionStatement (Local Function: System.Threading.Tasks.Task<System.Int32> LocalAsync(System.Int32 p1)) (OperationKind.LocalFunctionStatement) (Syntax: 'async Task< ... }')
  IBlockStatement (2 statements) (OperationKind.BlockStatement) (Syntax: '{ ... }')
    IExpressionStatement (OperationKind.ExpressionStatement) (Syntax: 'await Task.Delay(0);')
      IAwaitExpression (OperationKind.AwaitExpression, Type: System.Void) (Syntax: 'await Task.Delay(0)')
        IInvocationExpression (System.Threading.Tasks.Task System.Threading.Tasks.Task.Delay(System.Int32 millisecondsDelay)) (OperationKind.InvocationExpression, Type: System.Threading.Tasks.Task) (Syntax: 'Task.Delay(0)')
          Instance Receiver: null
          Arguments(1):
              IArgument (ArgumentKind.Explicit, Matching Parameter: millisecondsDelay) (OperationKind.Argument) (Syntax: '0')
                ILiteralExpression (Text: 0) (OperationKind.LiteralExpression, Type: System.Int32, Constant: 0) (Syntax: '0')
                InConversion: null
                OutConversion: null
    IReturnStatement (OperationKind.ReturnStatement) (Syntax: 'return x + p1;')
      IBinaryOperatorExpression (BinaryOperationKind.IntegerAdd) (OperationKind.BinaryOperatorExpression, Type: System.Int32) (Syntax: 'x + p1')
        Left: IParameterReferenceExpression: x (OperationKind.ParameterReferenceExpression, Type: System.Int32) (Syntax: 'x')
        Right: IParameterReferenceExpression: p1 (OperationKind.ParameterReferenceExpression, Type: System.Int32) (Syntax: 'p1')
";
            var expectedDiagnostics = DiagnosticDescription.None;

            VerifyOperationTreeAndDiagnosticsForTest<LocalFunctionStatementSyntax>(source, expectedOperationTree, expectedDiagnostics, additionalReferences: new[] { MscorlibRef_v46 });
        }

        [Fact]
        public void TestLocalFunction_CaptureForEachVar()
        {
            string source = @"
class C
{
    public void M(int[] array)
    {
        foreach (var x in array)
        {
            /*<bind>*/int Local() => x;/*</bind>*/
            Local();
        }
    }
}
";
            string expectedOperationTree = @"
ILocalFunctionStatement (Local Function: System.Int32 Local()) (OperationKind.LocalFunctionStatement) (Syntax: 'int Local() => x;')
  IBlockStatement (1 statements) (OperationKind.BlockStatement) (Syntax: '=> x')
    IReturnStatement (OperationKind.ReturnStatement) (Syntax: 'x')
      ILocalReferenceExpression: x (OperationKind.LocalReferenceExpression, Type: System.Int32) (Syntax: 'x')
";
            var expectedDiagnostics = DiagnosticDescription.None;

            VerifyOperationTreeAndDiagnosticsForTest<LocalFunctionStatementSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }

        [Fact]
        public void TestLocalFunction_UseOfUnusedVar()
        {
            string source = @"
class C
{
    void M()
    {
        Foo();
        int x = 0;
        /*<bind>*/void Foo() => x++;/*</bind>*/
    }
}
";
string expectedOperationTree = @"
ILocalFunctionStatement (Local Function: void Foo()) (OperationKind.LocalFunctionStatement) (Syntax: 'void Foo() => x++;')
  IBlockStatement (2 statements) (OperationKind.BlockStatement) (Syntax: '=> x++')
    IExpressionStatement (OperationKind.ExpressionStatement) (Syntax: 'x++')
      IIncrementExpression (UnaryOperandKind.IntegerPostfixIncrement) (OperationKind.IncrementExpression, Type: System.Int32) (Syntax: 'x++')
        Left: ILocalReferenceExpression: x (OperationKind.LocalReferenceExpression, Type: System.Int32) (Syntax: 'x')
    IReturnStatement (OperationKind.ReturnStatement) (Syntax: '=> x++')
    null
";
            var expectedDiagnostics = new DiagnosticDescription[] {
                // CS0165: Use of unassigned local variable 'x'
                //         Foo();
                Diagnostic(ErrorCode.ERR_UseDefViolation, "Foo()").WithArguments("x").WithLocation(6, 9)
            };

            VerifyOperationTreeAndDiagnosticsForTest<LocalFunctionStatementSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }

        [Fact]
        public void TestLocalFunction_OutVar()
        {
            string source = @"
class C
{
    void M(int p)
    {
        int x;
        /*<bind>*/void Foo(out int y) => y = p;/*</bind>*/
        Foo(out x);
    }
}
";
string expectedOperationTree = @"
ILocalFunctionStatement (Local Function: void Foo(out System.Int32 y)) (OperationKind.LocalFunctionStatement) (Syntax: 'void Foo(ou ... ) => y = p;')
  IBlockStatement (2 statements) (OperationKind.BlockStatement) (Syntax: '=> y = p')
    IExpressionStatement (OperationKind.ExpressionStatement) (Syntax: 'y = p')
      ISimpleAssignmentExpression (OperationKind.SimpleAssignmentExpression, Type: System.Int32) (Syntax: 'y = p')
        Left: IParameterReferenceExpression: y (OperationKind.ParameterReferenceExpression, Type: System.Int32) (Syntax: 'y')
        Right: IParameterReferenceExpression: p (OperationKind.ParameterReferenceExpression, Type: System.Int32) (Syntax: 'p')
    IReturnStatement (OperationKind.ReturnStatement) (Syntax: '=> y = p')
    null
";
            var expectedDiagnostics = DiagnosticDescription.None;

            VerifyOperationTreeAndDiagnosticsForTest<LocalFunctionStatementSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }

        [Fact]
        public void TestInvalidLocalFunction_MissingBody()
        {
            string source = @"
class C
{
    void M(int p)
    {
        /*<bind>*/void Foo(out int y) => ;/*</bind>*/
    }
}
";
string expectedOperationTree = @"
ILocalFunctionStatement (Local Function: void Foo(out System.Int32 y)) (OperationKind.LocalFunctionStatement, IsInvalid) (Syntax: 'void Foo(out int y) => ;')
  IBlockStatement (2 statements) (OperationKind.BlockStatement, IsInvalid) (Syntax: '=> ')
    IExpressionStatement (OperationKind.ExpressionStatement, IsInvalid) (Syntax: '')
      IInvalidExpression (OperationKind.InvalidExpression, Type: ?, IsInvalid) (Syntax: '')
        Children(0)
    IReturnStatement (OperationKind.ReturnStatement) (Syntax: '=> ')
    null
";
            var expectedDiagnostics = new DiagnosticDescription[] {
                // CS1525: Invalid expression term ';'
                //         /*<bind>*/void Foo(out int y) => ;/*</bind>*/
                Diagnostic(ErrorCode.ERR_InvalidExprTerm, ";").WithArguments(";").WithLocation(6, 42),
                // CS0177: The out parameter 'y' must be assigned to before control leaves the current method
                //         /*<bind>*/void Foo(out int y) => ;/*</bind>*/
                Diagnostic(ErrorCode.ERR_ParamUnassigned, "Foo").WithArguments("y").WithLocation(6, 24),
                // CS8321: The local function 'Foo' is declared but never used
                //         /*<bind>*/void Foo(out int y) => ;/*</bind>*/
                Diagnostic(ErrorCode.WRN_UnreferencedLocalFunction, "Foo").WithArguments("Foo").WithLocation(6, 24)
            };

            VerifyOperationTreeAndDiagnosticsForTest<LocalFunctionStatementSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }

        [Fact]
        public void TestInvalidLocalFunction_MissingParameters()
        {
            string source = @"
class C
{
    void M(int p)
    {
        /*<bind>*/void Foo( { }/*</bind>*/;
    }
}
";
string expectedOperationTree = @"
ILocalFunctionStatement (Local Function: void Foo()) (OperationKind.LocalFunctionStatement) (Syntax: 'void Foo( { }')
  IBlockStatement (1 statements) (OperationKind.BlockStatement) (Syntax: '{ }')
    IReturnStatement (OperationKind.ReturnStatement) (Syntax: '{ }')
    null
";
            var expectedDiagnostics = new DiagnosticDescription[] {
                // CS1026: ) expected
                //         /*<bind>*/void Foo( { }/*</bind>*/;
                Diagnostic(ErrorCode.ERR_CloseParenExpected, "{").WithLocation(6, 29),
                // CS8321: The local function 'Foo' is declared but never used
                //         /*<bind>*/void Foo( { }/*</bind>*/;
                Diagnostic(ErrorCode.WRN_UnreferencedLocalFunction, "Foo").WithArguments("Foo").WithLocation(6, 24)
            };

            VerifyOperationTreeAndDiagnosticsForTest<LocalFunctionStatementSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }

        [Fact]
        public void TestInvalidLocalFunction_InvalidReturnType()
        {
            string source = @"
class C
{
    void M(int p)
    {
        /*<bind>*/X Foo() { }/*</bind>*/;
    }
}
";
            string expectedOperationTree = @"
ILocalFunctionStatement (Local Function: X Foo()) (OperationKind.LocalFunctionStatement, IsInvalid) (Syntax: 'X Foo() { }')
  IBlockStatement (0 statements) (OperationKind.BlockStatement) (Syntax: '{ }')
";
            var expectedDiagnostics = new DiagnosticDescription[] {
                // CS0161: 'Foo()': not all code paths return a value
                //         /*<bind>*/X Foo() { }/*</bind>*/;
                Diagnostic(ErrorCode.ERR_ReturnExpected, "Foo").WithArguments("Foo()").WithLocation(6, 21),
                // CS0246: The type or namespace name 'X' could not be found (are you missing a using directive or an assembly reference?)
                //         /*<bind>*/X Foo() { }/*</bind>*/;
                Diagnostic(ErrorCode.ERR_SingleTypeNameNotFound, "X").WithArguments("X").WithLocation(6, 19),
                // CS8321: The local function 'Foo' is declared but never used
                //         /*<bind>*/X Foo() { }/*</bind>*/;
                Diagnostic(ErrorCode.WRN_UnreferencedLocalFunction, "Foo").WithArguments("Foo").WithLocation(6, 21)
            };

            VerifyOperationTreeAndDiagnosticsForTest<LocalFunctionStatementSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }
    }
}
