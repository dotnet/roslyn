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
ILocalFunctionOperation (Symbol: System.Int32 Local(System.Int32 p1)) (OperationKind.LocalFunction, IsStatement, Type: null) (Syntax: 'int Local(i ... }')
  IBlockOperation (1 statements) (OperationKind.Block, IsStatement, Type: null) (Syntax: '{ ... }')
    IReturnOperation (OperationKind.Return, IsStatement, Type: null) (Syntax: 'return x++;')
      ReturnedValue: 
        IIncrementOrDecrementOperation (Postfix) (OperationKind.Increment, IsExpression, Type: System.Int32) (Syntax: 'x++')
          Target: 
            IParameterReferenceOperation: x (OperationKind.ParameterReference, IsExpression, Type: System.Int32) (Syntax: 'x')
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
ILocalFunctionOperation (Symbol: System.Int32 Local(System.Int32 p1)) (OperationKind.LocalFunction, IsStatement, Type: null) (Syntax: 'int Local(i ... p1) => x++;')
  IBlockOperation (1 statements) (OperationKind.Block, IsStatement, Type: null) (Syntax: '=> x++')
    IReturnOperation (OperationKind.Return, IsStatement, Type: null, IsImplicit) (Syntax: 'x++')
      ReturnedValue: 
        IIncrementOrDecrementOperation (Postfix) (OperationKind.Increment, IsExpression, Type: System.Int32) (Syntax: 'x++')
          Target: 
            IParameterReferenceOperation: x (OperationKind.ParameterReference, IsExpression, Type: System.Int32) (Syntax: 'x')
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
ILocalFunctionOperation (Symbol: System.Int32 Local(System.Int32 x)) (OperationKind.LocalFunction, IsStatement, Type: null) (Syntax: 'int Local(int x) => x++;')
  IBlockOperation (1 statements) (OperationKind.Block, IsStatement, Type: null) (Syntax: '=> x++')
    IReturnOperation (OperationKind.Return, IsStatement, Type: null, IsImplicit) (Syntax: 'x++')
      ReturnedValue: 
        IIncrementOrDecrementOperation (Postfix) (OperationKind.Increment, IsExpression, Type: System.Int32) (Syntax: 'x++')
          Target: 
            IParameterReferenceOperation: x (OperationKind.ParameterReference, IsExpression, Type: System.Int32) (Syntax: 'x')
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
ILocalFunctionOperation (Symbol: System.Int32 Local(System.Int32 y)) (OperationKind.LocalFunction, IsStatement, Type: null) (Syntax: 'int Local(i ... ) => x + y;')
  IBlockOperation (1 statements) (OperationKind.Block, IsStatement, Type: null) (Syntax: '=> x + y')
    IReturnOperation (OperationKind.Return, IsStatement, Type: null, IsImplicit) (Syntax: 'x + y')
      ReturnedValue: 
        IBinaryOperation (BinaryOperatorKind.Add) (OperationKind.BinaryOperator, IsExpression, Type: System.Int32) (Syntax: 'x + y')
          Left: 
            IParameterReferenceOperation: x (OperationKind.ParameterReference, IsExpression, Type: System.Int32) (Syntax: 'x')
          Right: 
            IParameterReferenceOperation: y (OperationKind.ParameterReference, IsExpression, Type: System.Int32) (Syntax: 'y')
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
ILocalFunctionOperation (Symbol: System.Int32 Local3(System.Int32 p1)) (OperationKind.LocalFunction, IsStatement, Type: null) (Syntax: 'int Local3( ... Local2(p1);')
  IBlockOperation (1 statements) (OperationKind.Block, IsStatement, Type: null) (Syntax: '=> x + Local2(p1)')
    IReturnOperation (OperationKind.Return, IsStatement, Type: null, IsImplicit) (Syntax: 'x + Local2(p1)')
      ReturnedValue: 
        IBinaryOperation (BinaryOperatorKind.Add) (OperationKind.BinaryOperator, IsExpression, Type: System.Int32) (Syntax: 'x + Local2(p1)')
          Left: 
            ILocalReferenceOperation: x (OperationKind.LocalReference, IsExpression, Type: System.Int32) (Syntax: 'x')
          Right: 
            IInvocationOperation (System.Int32 Local2(System.Int32 p1)) (OperationKind.Invocation, IsExpression, Type: System.Int32) (Syntax: 'Local2(p1)')
              Instance Receiver: 
                null
              Arguments(1):
                  IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: p1) (OperationKind.Argument, Type: System.Int32) (Syntax: 'p1')
                    IParameterReferenceOperation: p1 (OperationKind.ParameterReference, IsExpression, Type: System.Int32) (Syntax: 'p1')
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
ILocalFunctionOperation (Symbol: System.Int32 Local(System.Int32 p1)) (OperationKind.LocalFunction, IsStatement, Type: null) (Syntax: 'int Local(i ... al(x + p1);')
  IBlockOperation (1 statements) (OperationKind.Block, IsStatement, Type: null) (Syntax: '=> Local(x + p1)')
    IReturnOperation (OperationKind.Return, IsStatement, Type: null, IsImplicit) (Syntax: 'Local(x + p1)')
      ReturnedValue: 
        IInvocationOperation (System.Int32 Local(System.Int32 p1)) (OperationKind.Invocation, IsExpression, Type: System.Int32) (Syntax: 'Local(x + p1)')
          Instance Receiver: 
            null
          Arguments(1):
              IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: p1) (OperationKind.Argument, Type: System.Int32) (Syntax: 'x + p1')
                IBinaryOperation (BinaryOperatorKind.Add) (OperationKind.BinaryOperator, IsExpression, Type: System.Int32) (Syntax: 'x + p1')
                  Left: 
                    IParameterReferenceOperation: x (OperationKind.ParameterReference, IsExpression, Type: System.Int32) (Syntax: 'x')
                  Right: 
                    IParameterReferenceOperation: p1 (OperationKind.ParameterReference, IsExpression, Type: System.Int32) (Syntax: 'p1')
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
ILocalFunctionOperation (Symbol: System.Threading.Tasks.Task<System.Int32> LocalAsync(System.Int32 p1)) (OperationKind.LocalFunction, IsStatement, Type: null) (Syntax: 'async Task< ... }')
  IBlockOperation (2 statements) (OperationKind.Block, IsStatement, Type: null) (Syntax: '{ ... }')
    IExpressionStatementOperation (OperationKind.ExpressionStatement, IsStatement, Type: null) (Syntax: 'await Task.Delay(0);')
      Expression: 
        IAwaitOperation (OperationKind.Await, IsExpression, Type: System.Void) (Syntax: 'await Task.Delay(0)')
          Expression: 
            IInvocationOperation (System.Threading.Tasks.Task System.Threading.Tasks.Task.Delay(System.Int32 millisecondsDelay)) (OperationKind.Invocation, IsExpression, Type: System.Threading.Tasks.Task) (Syntax: 'Task.Delay(0)')
              Instance Receiver: 
                null
              Arguments(1):
                  IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: millisecondsDelay) (OperationKind.Argument, Type: System.Int32) (Syntax: '0')
                    ILiteralOperation (OperationKind.Literal, IsExpression, Type: System.Int32, Constant: 0) (Syntax: '0')
                    InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                    OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
    IReturnOperation (OperationKind.Return, IsStatement, Type: null) (Syntax: 'return x + p1;')
      ReturnedValue: 
        IBinaryOperation (BinaryOperatorKind.Add) (OperationKind.BinaryOperator, IsExpression, Type: System.Int32) (Syntax: 'x + p1')
          Left: 
            IParameterReferenceOperation: x (OperationKind.ParameterReference, IsExpression, Type: System.Int32) (Syntax: 'x')
          Right: 
            IParameterReferenceOperation: p1 (OperationKind.ParameterReference, IsExpression, Type: System.Int32) (Syntax: 'p1')
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
ILocalFunctionOperation (Symbol: System.Int32 Local()) (OperationKind.LocalFunction, IsStatement, Type: null) (Syntax: 'int Local() => x;')
  IBlockOperation (1 statements) (OperationKind.Block, IsStatement, Type: null) (Syntax: '=> x')
    IReturnOperation (OperationKind.Return, IsStatement, Type: null, IsImplicit) (Syntax: 'x')
      ReturnedValue: 
        ILocalReferenceOperation: x (OperationKind.LocalReference, IsExpression, Type: System.Int32) (Syntax: 'x')
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
ILocalFunctionOperation (Symbol: void F()) (OperationKind.LocalFunction, IsStatement, Type: null) (Syntax: 'void F() => x++;')
  IBlockOperation (2 statements) (OperationKind.Block, IsStatement, Type: null) (Syntax: '=> x++')
    IExpressionStatementOperation (OperationKind.ExpressionStatement, IsStatement, Type: null, IsImplicit) (Syntax: 'x++')
      Expression: 
        IIncrementOrDecrementOperation (Postfix) (OperationKind.Increment, IsExpression, Type: System.Int32) (Syntax: 'x++')
          Target: 
            ILocalReferenceOperation: x (OperationKind.LocalReference, IsExpression, Type: System.Int32) (Syntax: 'x')
    IReturnOperation (OperationKind.Return, IsStatement, Type: null, IsImplicit) (Syntax: '=> x++')
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
ILocalFunctionOperation (Symbol: void F(out System.Int32 y)) (OperationKind.LocalFunction, IsStatement, Type: null) (Syntax: 'void F(out  ... ) => y = p;')
  IBlockOperation (2 statements) (OperationKind.Block, IsStatement, Type: null) (Syntax: '=> y = p')
    IExpressionStatementOperation (OperationKind.ExpressionStatement, IsStatement, Type: null, IsImplicit) (Syntax: 'y = p')
      Expression: 
        ISimpleAssignmentOperation (OperationKind.SimpleAssignment, IsExpression, Type: System.Int32) (Syntax: 'y = p')
          Left: 
            IParameterReferenceOperation: y (OperationKind.ParameterReference, IsExpression, Type: System.Int32) (Syntax: 'y')
          Right: 
            IParameterReferenceOperation: p (OperationKind.ParameterReference, IsExpression, Type: System.Int32) (Syntax: 'p')
    IReturnOperation (OperationKind.Return, IsStatement, Type: null, IsImplicit) (Syntax: '=> y = p')
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
ILocalFunctionOperation (Symbol: void F(out System.Int32 y)) (OperationKind.LocalFunction, IsStatement, Type: null, IsInvalid) (Syntax: 'void F(out int y) => ;')
  IBlockOperation (2 statements) (OperationKind.Block, IsStatement, Type: null, IsInvalid) (Syntax: '=> ')
    IExpressionStatementOperation (OperationKind.ExpressionStatement, IsStatement, Type: null, IsInvalid, IsImplicit) (Syntax: '')
      Expression: 
        IInvalidOperation (OperationKind.Invalid, IsExpression, Type: null, IsInvalid) (Syntax: '')
          Children(0)
    IReturnOperation (OperationKind.Return, IsStatement, Type: null, IsInvalid, IsImplicit) (Syntax: '=> ')
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
ILocalFunctionOperation (Symbol: void F()) (OperationKind.LocalFunction, IsStatement, Type: null, IsInvalid) (Syntax: 'void F( { }')
  IBlockOperation (1 statements) (OperationKind.Block, IsStatement, Type: null, IsInvalid) (Syntax: '{ }')
    IReturnOperation (OperationKind.Return, IsStatement, Type: null, IsInvalid, IsImplicit) (Syntax: '{ }')
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
ILocalFunctionOperation (Symbol: X F()) (OperationKind.LocalFunction, IsStatement, Type: null, IsInvalid) (Syntax: 'X F() { }')
  IBlockOperation (0 statements) (OperationKind.Block, IsStatement, Type: null) (Syntax: '{ }')
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
