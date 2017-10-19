// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.CSharp.UnitTests
{
    public partial class IOperationTests : SemanticModelTestBase
    {
        [CompilerTrait(CompilerFeature.IOperation)]
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
ILocalFunctionStatement (Symbol: System.Int32 Local(System.Int32 p1)) ([0] OperationKind.LocalFunctionStatement) (Syntax: 'int Local(i ... }')
  IBlockStatement (1 statements) ([0] OperationKind.BlockStatement) (Syntax: '{ ... }')
    IReturnStatement ([0] OperationKind.ReturnStatement) (Syntax: 'return x++;')
      ReturnedValue: 
        IIncrementOrDecrementExpression (Postfix) ([0] OperationKind.IncrementExpression, Type: System.Int32) (Syntax: 'x++')
          Target: 
            IParameterReferenceExpression: x ([0] OperationKind.ParameterReferenceExpression, Type: System.Int32) (Syntax: 'x')
";
            var expectedDiagnostics = DiagnosticDescription.None;

            VerifyOperationTreeAndDiagnosticsForTest<LocalFunctionStatementSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation)]
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
ILocalFunctionStatement (Symbol: System.Int32 Local(System.Int32 p1)) ([0] OperationKind.LocalFunctionStatement) (Syntax: 'int Local(i ... p1) => x++;')
  IBlockStatement (1 statements) ([0] OperationKind.BlockStatement) (Syntax: '=> x++')
    IReturnStatement ([0] OperationKind.ReturnStatement, IsImplicit) (Syntax: 'x++')
      ReturnedValue: 
        IIncrementOrDecrementExpression (Postfix) ([0] OperationKind.IncrementExpression, Type: System.Int32) (Syntax: 'x++')
          Target: 
            IParameterReferenceExpression: x ([0] OperationKind.ParameterReferenceExpression, Type: System.Int32) (Syntax: 'x')
";
            var expectedDiagnostics = DiagnosticDescription.None;

            VerifyOperationTreeAndDiagnosticsForTest<LocalFunctionStatementSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation)]
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
ILocalFunctionStatement (Symbol: System.Int32 Local(System.Int32 x)) ([0] OperationKind.LocalFunctionStatement) (Syntax: 'int Local(int x) => x++;')
  IBlockStatement (1 statements) ([0] OperationKind.BlockStatement) (Syntax: '=> x++')
    IReturnStatement ([0] OperationKind.ReturnStatement, IsImplicit) (Syntax: 'x++')
      ReturnedValue: 
        IIncrementOrDecrementExpression (Postfix) ([0] OperationKind.IncrementExpression, Type: System.Int32) (Syntax: 'x++')
          Target: 
            IParameterReferenceExpression: x ([0] OperationKind.ParameterReferenceExpression, Type: System.Int32) (Syntax: 'x')
";
            var expectedDiagnostics = DiagnosticDescription.None;

            VerifyOperationTreeAndDiagnosticsForTest<LocalFunctionStatementSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation)]
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
ILocalFunctionStatement (Symbol: System.Int32 Local(System.Int32 y)) ([0] OperationKind.LocalFunctionStatement) (Syntax: 'int Local(i ... ) => x + y;')
  IBlockStatement (1 statements) ([0] OperationKind.BlockStatement) (Syntax: '=> x + y')
    IReturnStatement ([0] OperationKind.ReturnStatement, IsImplicit) (Syntax: 'x + y')
      ReturnedValue: 
        IBinaryOperatorExpression (BinaryOperatorKind.Add) ([0] OperationKind.BinaryOperatorExpression, Type: System.Int32) (Syntax: 'x + y')
          Left: 
            IParameterReferenceExpression: x ([0] OperationKind.ParameterReferenceExpression, Type: System.Int32) (Syntax: 'x')
          Right: 
            IParameterReferenceExpression: y ([1] OperationKind.ParameterReferenceExpression, Type: System.Int32) (Syntax: 'y')
";
            var expectedDiagnostics = DiagnosticDescription.None;

            VerifyOperationTreeAndDiagnosticsForTest<LocalFunctionStatementSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation)]
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
ILocalFunctionStatement (Symbol: System.Int32 Local3(System.Int32 p1)) ([3] OperationKind.LocalFunctionStatement) (Syntax: 'int Local3( ... Local2(p1);')
  IBlockStatement (1 statements) ([0] OperationKind.BlockStatement) (Syntax: '=> x + Local2(p1)')
    IReturnStatement ([0] OperationKind.ReturnStatement, IsImplicit) (Syntax: 'x + Local2(p1)')
      ReturnedValue: 
        IBinaryOperatorExpression (BinaryOperatorKind.Add) ([0] OperationKind.BinaryOperatorExpression, Type: System.Int32) (Syntax: 'x + Local2(p1)')
          Left: 
            ILocalReferenceExpression: x ([0] OperationKind.LocalReferenceExpression, Type: System.Int32) (Syntax: 'x')
          Right: 
            IInvocationExpression (System.Int32 Local2(System.Int32 p1)) ([1] OperationKind.InvocationExpression, Type: System.Int32) (Syntax: 'Local2(p1)')
              Instance Receiver: 
                null
              Arguments(1):
                  IArgument (ArgumentKind.Explicit, Matching Parameter: p1) ([0] OperationKind.Argument) (Syntax: 'p1')
                    IParameterReferenceExpression: p1 ([0] OperationKind.ParameterReferenceExpression, Type: System.Int32) (Syntax: 'p1')
                    InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                    OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
";
            var expectedDiagnostics = DiagnosticDescription.None;

            VerifyOperationTreeAndDiagnosticsForTest<LocalFunctionStatementSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation)]
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
ILocalFunctionStatement (Symbol: System.Int32 Local(System.Int32 p1)) ([0] OperationKind.LocalFunctionStatement) (Syntax: 'int Local(i ... al(x + p1);')
  IBlockStatement (1 statements) ([0] OperationKind.BlockStatement) (Syntax: '=> Local(x + p1)')
    IReturnStatement ([0] OperationKind.ReturnStatement, IsImplicit) (Syntax: 'Local(x + p1)')
      ReturnedValue: 
        IInvocationExpression (System.Int32 Local(System.Int32 p1)) ([0] OperationKind.InvocationExpression, Type: System.Int32) (Syntax: 'Local(x + p1)')
          Instance Receiver: 
            null
          Arguments(1):
              IArgument (ArgumentKind.Explicit, Matching Parameter: p1) ([0] OperationKind.Argument) (Syntax: 'x + p1')
                IBinaryOperatorExpression (BinaryOperatorKind.Add) ([0] OperationKind.BinaryOperatorExpression, Type: System.Int32) (Syntax: 'x + p1')
                  Left: 
                    IParameterReferenceExpression: x ([0] OperationKind.ParameterReferenceExpression, Type: System.Int32) (Syntax: 'x')
                  Right: 
                    IParameterReferenceExpression: p1 ([1] OperationKind.ParameterReferenceExpression, Type: System.Int32) (Syntax: 'p1')
                InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
";
            var expectedDiagnostics = DiagnosticDescription.None;

            VerifyOperationTreeAndDiagnosticsForTest<LocalFunctionStatementSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation)]
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
ILocalFunctionStatement (Symbol: System.Threading.Tasks.Task<System.Int32> LocalAsync(System.Int32 p1)) ([0] OperationKind.LocalFunctionStatement) (Syntax: 'async Task< ... }')
  IBlockStatement (2 statements) ([0] OperationKind.BlockStatement) (Syntax: '{ ... }')
    IExpressionStatement ([0] OperationKind.ExpressionStatement) (Syntax: 'await Task.Delay(0);')
      Expression: 
        IAwaitExpression ([0] OperationKind.AwaitExpression, Type: System.Void) (Syntax: 'await Task.Delay(0)')
          Expression: 
            IInvocationExpression (System.Threading.Tasks.Task System.Threading.Tasks.Task.Delay(System.Int32 millisecondsDelay)) ([0] OperationKind.InvocationExpression, Type: System.Threading.Tasks.Task) (Syntax: 'Task.Delay(0)')
              Instance Receiver: 
                null
              Arguments(1):
                  IArgument (ArgumentKind.Explicit, Matching Parameter: millisecondsDelay) ([0] OperationKind.Argument) (Syntax: '0')
                    ILiteralExpression ([0] OperationKind.LiteralExpression, Type: System.Int32, Constant: 0) (Syntax: '0')
                    InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                    OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
    IReturnStatement ([1] OperationKind.ReturnStatement) (Syntax: 'return x + p1;')
      ReturnedValue: 
        IBinaryOperatorExpression (BinaryOperatorKind.Add) ([0] OperationKind.BinaryOperatorExpression, Type: System.Int32) (Syntax: 'x + p1')
          Left: 
            IParameterReferenceExpression: x ([0] OperationKind.ParameterReferenceExpression, Type: System.Int32) (Syntax: 'x')
          Right: 
            IParameterReferenceExpression: p1 ([1] OperationKind.ParameterReferenceExpression, Type: System.Int32) (Syntax: 'p1')
";
            var expectedDiagnostics = DiagnosticDescription.None;

            VerifyOperationTreeAndDiagnosticsForTest<LocalFunctionStatementSyntax>(source, expectedOperationTree, expectedDiagnostics, useLatestFrameworkReferences: true);
        }

        [CompilerTrait(CompilerFeature.IOperation)]
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
ILocalFunctionStatement (Symbol: System.Int32 Local()) ([0] OperationKind.LocalFunctionStatement) (Syntax: 'int Local() => x;')
  IBlockStatement (1 statements) ([0] OperationKind.BlockStatement) (Syntax: '=> x')
    IReturnStatement ([0] OperationKind.ReturnStatement, IsImplicit) (Syntax: 'x')
      ReturnedValue: 
        ILocalReferenceExpression: x ([0] OperationKind.LocalReferenceExpression, Type: System.Int32) (Syntax: 'x')
";
            var expectedDiagnostics = DiagnosticDescription.None;

            VerifyOperationTreeAndDiagnosticsForTest<LocalFunctionStatementSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation)]
        [Fact]
        public void TestLocalFunction_UseOfUnusedVar()
        {
            string source = @"
class C
{
    void M()
    {
        F();
        int x = 0;
        /*<bind>*/void F() => x++;/*</bind>*/
    }
}
";
            string expectedOperationTree = @"
ILocalFunctionStatement (Symbol: void F()) ([2] OperationKind.LocalFunctionStatement) (Syntax: 'void F() => x++;')
  IBlockStatement (2 statements) ([0] OperationKind.BlockStatement) (Syntax: '=> x++')
    IExpressionStatement ([0] OperationKind.ExpressionStatement, IsImplicit) (Syntax: 'x++')
      Expression: 
        IIncrementOrDecrementExpression (Postfix) ([0] OperationKind.IncrementExpression, Type: System.Int32) (Syntax: 'x++')
          Target: 
            ILocalReferenceExpression: x ([0] OperationKind.LocalReferenceExpression, Type: System.Int32) (Syntax: 'x')
    IReturnStatement ([1] OperationKind.ReturnStatement, IsImplicit) (Syntax: '=> x++')
      ReturnedValue: 
        null
";
            var expectedDiagnostics = new DiagnosticDescription[] {
                // CS0165: Use of unassigned local variable 'x'
                //         F();
                Diagnostic(ErrorCode.ERR_UseDefViolation, "F()").WithArguments("x").WithLocation(6, 9)
            };

            VerifyOperationTreeAndDiagnosticsForTest<LocalFunctionStatementSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation)]
        [Fact]
        public void TestLocalFunction_OutVar()
        {
            string source = @"
class C
{
    void M(int p)
    {
        int x;
        /*<bind>*/void F(out int y) => y = p;/*</bind>*/
        F(out x);
    }
}
";
            string expectedOperationTree = @"
ILocalFunctionStatement (Symbol: void F(out System.Int32 y)) ([1] OperationKind.LocalFunctionStatement) (Syntax: 'void F(out  ... ) => y = p;')
  IBlockStatement (2 statements) ([0] OperationKind.BlockStatement) (Syntax: '=> y = p')
    IExpressionStatement ([0] OperationKind.ExpressionStatement, IsImplicit) (Syntax: 'y = p')
      Expression: 
        ISimpleAssignmentExpression ([0] OperationKind.SimpleAssignmentExpression, Type: System.Int32) (Syntax: 'y = p')
          Left: 
            IParameterReferenceExpression: y ([0] OperationKind.ParameterReferenceExpression, Type: System.Int32) (Syntax: 'y')
          Right: 
            IParameterReferenceExpression: p ([1] OperationKind.ParameterReferenceExpression, Type: System.Int32) (Syntax: 'p')
    IReturnStatement ([1] OperationKind.ReturnStatement, IsImplicit) (Syntax: '=> y = p')
      ReturnedValue: 
        null
";
            var expectedDiagnostics = DiagnosticDescription.None;

            VerifyOperationTreeAndDiagnosticsForTest<LocalFunctionStatementSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation)]
        [Fact]
        public void TestInvalidLocalFunction_MissingBody()
        {
            string source = @"
class C
{
    void M(int p)
    {
        /*<bind>*/void F(out int y) => ;/*</bind>*/
    }
}
";
            string expectedOperationTree = @"
ILocalFunctionStatement (Symbol: void F(out System.Int32 y)) ([0] OperationKind.LocalFunctionStatement, IsInvalid) (Syntax: 'void F(out int y) => ;')
  IBlockStatement (2 statements) ([0] OperationKind.BlockStatement, IsInvalid) (Syntax: '=> ')
    IExpressionStatement ([0] OperationKind.ExpressionStatement, IsInvalid, IsImplicit) (Syntax: '')
      Expression: 
        IInvalidExpression ([0] OperationKind.InvalidExpression, Type: null, IsInvalid) (Syntax: '')
          Children(0)
    IReturnStatement ([1] OperationKind.ReturnStatement, IsInvalid, IsImplicit) (Syntax: '=> ')
      ReturnedValue: 
        null
";
            var expectedDiagnostics = new DiagnosticDescription[] {
                // file.cs(6,40): error CS1525: Invalid expression term ';'
                //         /*<bind>*/void F(out int y) => ;/*</bind>*/
                Diagnostic(ErrorCode.ERR_InvalidExprTerm, ";").WithArguments(";").WithLocation(6, 40),
                // file.cs(6,24): error CS0177: The out parameter 'y' must be assigned to before control leaves the current method
                //         /*<bind>*/void F(out int y) => ;/*</bind>*/
                Diagnostic(ErrorCode.ERR_ParamUnassigned, "F").WithArguments("y").WithLocation(6, 24),
                // file.cs(6,24): warning CS8321: The local function 'F' is declared but never used
                //         /*<bind>*/void F(out int y) => ;/*</bind>*/
                Diagnostic(ErrorCode.WRN_UnreferencedLocalFunction, "F").WithArguments("F").WithLocation(6, 24)
            };

            VerifyOperationTreeAndDiagnosticsForTest<LocalFunctionStatementSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation)]
        [Fact]
        public void TestInvalidLocalFunction_MissingParameters()
        {
            string source = @"
class C
{
    void M(int p)
    {
        /*<bind>*/void F( { }/*</bind>*/;
    }
}
";
            string expectedOperationTree = @"
ILocalFunctionStatement (Symbol: void F()) ([0] OperationKind.LocalFunctionStatement, IsInvalid) (Syntax: 'void F( { }')
  IBlockStatement (1 statements) ([0] OperationKind.BlockStatement, IsInvalid) (Syntax: '{ }')
    IReturnStatement ([0] OperationKind.ReturnStatement, IsInvalid, IsImplicit) (Syntax: '{ }')
      ReturnedValue: 
        null
";
            var expectedDiagnostics = new DiagnosticDescription[] {
                // file.cs(6,27): error CS1026: ) expected
                //         /*<bind>*/void F( { }/*</bind>*/;
                Diagnostic(ErrorCode.ERR_CloseParenExpected, "{").WithLocation(6, 27),
                // file.cs(6,24): warning CS8321: The local function 'F' is declared but never used
                //         /*<bind>*/void F( { }/*</bind>*/;
                Diagnostic(ErrorCode.WRN_UnreferencedLocalFunction, "F").WithArguments("F").WithLocation(6, 24)
            };

            VerifyOperationTreeAndDiagnosticsForTest<LocalFunctionStatementSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation)]
        [Fact]
        public void TestInvalidLocalFunction_InvalidReturnType()
        {
            string source = @"
class C
{
    void M(int p)
    {
        /*<bind>*/X F() { }/*</bind>*/;
    }
}
";
            string expectedOperationTree = @"
ILocalFunctionStatement (Symbol: X F()) ([0] OperationKind.LocalFunctionStatement, IsInvalid) (Syntax: 'X F() { }')
  IBlockStatement (0 statements) ([0] OperationKind.BlockStatement) (Syntax: '{ }')
";
            var expectedDiagnostics = new DiagnosticDescription[] {
                // CS0161: 'F()': not all code paths return a value
                //         /*<bind>*/X F() { }/*</bind>*/;
                Diagnostic(ErrorCode.ERR_ReturnExpected, "F").WithArguments("F()").WithLocation(6, 21),
                // CS0246: The type or namespace name 'X' could not be found (are you missing a using directive or an assembly reference?)
                //         /*<bind>*/X F() { }/*</bind>*/;
                Diagnostic(ErrorCode.ERR_SingleTypeNameNotFound, "X").WithArguments("X").WithLocation(6, 19),
                // CS8321: The local function 'F' is declared but never used
                //         /*<bind>*/X F() { }/*</bind>*/;
                Diagnostic(ErrorCode.WRN_UnreferencedLocalFunction, "F").WithArguments("F").WithLocation(6, 21)
            };

            VerifyOperationTreeAndDiagnosticsForTest<LocalFunctionStatementSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }
    }
}
