// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Test.Utilities;
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
ILocalFunctionOperation (Symbol: System.Int32 Local(System.Int32 p1)) (OperationKind.LocalFunction, Type: null) (Syntax: 'int Local(i ... }')
  IBlockOperation (1 statements) (OperationKind.Block, Type: null) (Syntax: '{ ... }')
    IReturnOperation (OperationKind.Return, Type: null) (Syntax: 'return x++;')
      ReturnedValue: 
        IIncrementOrDecrementOperation (Postfix) (OperationKind.Increment, Type: System.Int32) (Syntax: 'x++')
          Target: 
            IParameterReferenceOperation: x (OperationKind.ParameterReference, Type: System.Int32) (Syntax: 'x')
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
ILocalFunctionOperation (Symbol: System.Int32 Local(System.Int32 p1)) (OperationKind.LocalFunction, Type: null) (Syntax: 'int Local(i ... p1) => x++;')
  IBlockOperation (1 statements) (OperationKind.Block, Type: null) (Syntax: '=> x++')
    IReturnOperation (OperationKind.Return, Type: null, IsImplicit) (Syntax: 'x++')
      ReturnedValue: 
        IIncrementOrDecrementOperation (Postfix) (OperationKind.Increment, Type: System.Int32) (Syntax: 'x++')
          Target: 
            IParameterReferenceOperation: x (OperationKind.ParameterReference, Type: System.Int32) (Syntax: 'x')
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
ILocalFunctionOperation (Symbol: System.Int32 Local(System.Int32 x)) (OperationKind.LocalFunction, Type: null) (Syntax: 'int Local(int x) => x++;')
  IBlockOperation (1 statements) (OperationKind.Block, Type: null) (Syntax: '=> x++')
    IReturnOperation (OperationKind.Return, Type: null, IsImplicit) (Syntax: 'x++')
      ReturnedValue: 
        IIncrementOrDecrementOperation (Postfix) (OperationKind.Increment, Type: System.Int32) (Syntax: 'x++')
          Target: 
            IParameterReferenceOperation: x (OperationKind.ParameterReference, Type: System.Int32) (Syntax: 'x')
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
ILocalFunctionOperation (Symbol: System.Int32 Local(System.Int32 y)) (OperationKind.LocalFunction, Type: null) (Syntax: 'int Local(i ... ) => x + y;')
  IBlockOperation (1 statements) (OperationKind.Block, Type: null) (Syntax: '=> x + y')
    IReturnOperation (OperationKind.Return, Type: null, IsImplicit) (Syntax: 'x + y')
      ReturnedValue: 
        IBinaryOperation (BinaryOperatorKind.Add) (OperationKind.BinaryOperator, Type: System.Int32) (Syntax: 'x + y')
          Left: 
            IParameterReferenceOperation: x (OperationKind.ParameterReference, Type: System.Int32) (Syntax: 'x')
          Right: 
            IParameterReferenceOperation: y (OperationKind.ParameterReference, Type: System.Int32) (Syntax: 'y')
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
ILocalFunctionOperation (Symbol: System.Int32 Local3(System.Int32 p1)) (OperationKind.LocalFunction, Type: null) (Syntax: 'int Local3( ... Local2(p1);')
  IBlockOperation (1 statements) (OperationKind.Block, Type: null) (Syntax: '=> x + Local2(p1)')
    IReturnOperation (OperationKind.Return, Type: null, IsImplicit) (Syntax: 'x + Local2(p1)')
      ReturnedValue: 
        IBinaryOperation (BinaryOperatorKind.Add) (OperationKind.BinaryOperator, Type: System.Int32) (Syntax: 'x + Local2(p1)')
          Left: 
            ILocalReferenceOperation: x (OperationKind.LocalReference, Type: System.Int32) (Syntax: 'x')
          Right: 
            IInvocationOperation (System.Int32 Local2(System.Int32 p1)) (OperationKind.Invocation, Type: System.Int32) (Syntax: 'Local2(p1)')
              Instance Receiver: 
                null
              Arguments(1):
                  IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: p1) (OperationKind.Argument, Type: null) (Syntax: 'p1')
                    IParameterReferenceOperation: p1 (OperationKind.ParameterReference, Type: System.Int32) (Syntax: 'p1')
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
ILocalFunctionOperation (Symbol: System.Int32 Local(System.Int32 p1)) (OperationKind.LocalFunction, Type: null) (Syntax: 'int Local(i ... al(x + p1);')
  IBlockOperation (1 statements) (OperationKind.Block, Type: null) (Syntax: '=> Local(x + p1)')
    IReturnOperation (OperationKind.Return, Type: null, IsImplicit) (Syntax: 'Local(x + p1)')
      ReturnedValue: 
        IInvocationOperation (System.Int32 Local(System.Int32 p1)) (OperationKind.Invocation, Type: System.Int32) (Syntax: 'Local(x + p1)')
          Instance Receiver: 
            null
          Arguments(1):
              IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: p1) (OperationKind.Argument, Type: null) (Syntax: 'x + p1')
                IBinaryOperation (BinaryOperatorKind.Add) (OperationKind.BinaryOperator, Type: System.Int32) (Syntax: 'x + p1')
                  Left: 
                    IParameterReferenceOperation: x (OperationKind.ParameterReference, Type: System.Int32) (Syntax: 'x')
                  Right: 
                    IParameterReferenceOperation: p1 (OperationKind.ParameterReference, Type: System.Int32) (Syntax: 'p1')
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
ILocalFunctionOperation (Symbol: System.Threading.Tasks.Task<System.Int32> LocalAsync(System.Int32 p1)) (OperationKind.LocalFunction, Type: null) (Syntax: 'async Task< ... }')
  IBlockOperation (2 statements) (OperationKind.Block, Type: null) (Syntax: '{ ... }')
    IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'await Task.Delay(0);')
      Expression: 
        IAwaitOperation (OperationKind.Await, Type: System.Void) (Syntax: 'await Task.Delay(0)')
          Expression: 
            IInvocationOperation (System.Threading.Tasks.Task System.Threading.Tasks.Task.Delay(System.Int32 millisecondsDelay)) (OperationKind.Invocation, Type: System.Threading.Tasks.Task) (Syntax: 'Task.Delay(0)')
              Instance Receiver: 
                null
              Arguments(1):
                  IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: millisecondsDelay) (OperationKind.Argument, Type: null) (Syntax: '0')
                    ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 0) (Syntax: '0')
                    InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                    OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
    IReturnOperation (OperationKind.Return, Type: null) (Syntax: 'return x + p1;')
      ReturnedValue: 
        IBinaryOperation (BinaryOperatorKind.Add) (OperationKind.BinaryOperator, Type: System.Int32) (Syntax: 'x + p1')
          Left: 
            IParameterReferenceOperation: x (OperationKind.ParameterReference, Type: System.Int32) (Syntax: 'x')
          Right: 
            IParameterReferenceOperation: p1 (OperationKind.ParameterReference, Type: System.Int32) (Syntax: 'p1')
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
ILocalFunctionOperation (Symbol: System.Int32 Local()) (OperationKind.LocalFunction, Type: null) (Syntax: 'int Local() => x;')
  IBlockOperation (1 statements) (OperationKind.Block, Type: null) (Syntax: '=> x')
    IReturnOperation (OperationKind.Return, Type: null, IsImplicit) (Syntax: 'x')
      ReturnedValue: 
        ILocalReferenceOperation: x (OperationKind.LocalReference, Type: System.Int32) (Syntax: 'x')
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
ILocalFunctionOperation (Symbol: void F()) (OperationKind.LocalFunction, Type: null) (Syntax: 'void F() => x++;')
  IBlockOperation (2 statements) (OperationKind.Block, Type: null) (Syntax: '=> x++')
    IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null, IsImplicit) (Syntax: 'x++')
      Expression: 
        IIncrementOrDecrementOperation (Postfix) (OperationKind.Increment, Type: System.Int32) (Syntax: 'x++')
          Target: 
            ILocalReferenceOperation: x (OperationKind.LocalReference, Type: System.Int32) (Syntax: 'x')
    IReturnOperation (OperationKind.Return, Type: null, IsImplicit) (Syntax: '=> x++')
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
ILocalFunctionOperation (Symbol: void F(out System.Int32 y)) (OperationKind.LocalFunction, Type: null) (Syntax: 'void F(out  ... ) => y = p;')
  IBlockOperation (2 statements) (OperationKind.Block, Type: null) (Syntax: '=> y = p')
    IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null, IsImplicit) (Syntax: 'y = p')
      Expression: 
        ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Int32) (Syntax: 'y = p')
          Left: 
            IParameterReferenceOperation: y (OperationKind.ParameterReference, Type: System.Int32) (Syntax: 'y')
          Right: 
            IParameterReferenceOperation: p (OperationKind.ParameterReference, Type: System.Int32) (Syntax: 'p')
    IReturnOperation (OperationKind.Return, Type: null, IsImplicit) (Syntax: '=> y = p')
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
ILocalFunctionOperation (Symbol: void F(out System.Int32 y)) (OperationKind.LocalFunction, Type: null, IsInvalid) (Syntax: 'void F(out int y) => ;')
  IBlockOperation (2 statements) (OperationKind.Block, Type: null, IsInvalid) (Syntax: '=> ')
    IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null, IsInvalid, IsImplicit) (Syntax: '')
      Expression: 
        IInvalidOperation (OperationKind.Invalid, Type: null, IsInvalid) (Syntax: '')
          Children(0)
    IReturnOperation (OperationKind.Return, Type: null, IsInvalid, IsImplicit) (Syntax: '=> ')
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
ILocalFunctionOperation (Symbol: void F()) (OperationKind.LocalFunction, Type: null, IsInvalid) (Syntax: 'void F( { }')
  IBlockOperation (1 statements) (OperationKind.Block, Type: null, IsInvalid) (Syntax: '{ }')
    IReturnOperation (OperationKind.Return, Type: null, IsInvalid, IsImplicit) (Syntax: '{ }')
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
ILocalFunctionOperation (Symbol: X F()) (OperationKind.LocalFunction, Type: null, IsInvalid) (Syntax: 'X F() { }')
  IBlockOperation (0 statements) (OperationKind.Block, Type: null) (Syntax: '{ }')
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

        [CompilerTrait(CompilerFeature.IOperation)]
        [Fact, WorkItem(24650, "https://github.com/dotnet/roslyn/issues/24650")]
        public void TestInvalidLocalFunction_ExpressionAndBlockBody()
        {
            string source = @"
class C
{
    void M(int p)
    {
        /*<bind>*/object F() => new object(); { return null; }/*</bind>*/;
    }
}
";
            string expectedOperationTree = @"
ILocalFunctionOperation (Symbol: System.Object F()) (OperationKind.LocalFunction, Type: null) (Syntax: 'object F()  ... w object();')
  IBlockOperation (1 statements) (OperationKind.Block, Type: null) (Syntax: '=> new object()')
    IReturnOperation (OperationKind.Return, Type: null, IsImplicit) (Syntax: 'new object()')
      ReturnedValue: 
        IObjectCreationOperation (Constructor: System.Object..ctor()) (OperationKind.ObjectCreation, Type: System.Object) (Syntax: 'new object()')
          Arguments(0)
          Initializer: 
            null
";
            var expectedDiagnostics = new DiagnosticDescription[] {
                // file.cs(6,49): error CS0127: Since 'C.M(int)' returns void, a return keyword must not be followed by an object expression
                //         /*<bind>*/object F() => new object(); { return new object(); }/*</bind>*/;
                Diagnostic(ErrorCode.ERR_RetNoObjectRequired, "return").WithArguments("C.M(int)").WithLocation(6, 49),
                // file.cs(6,26): warning CS8321: The local function 'F' is declared but never used
                //         /*<bind>*/object F() => new object(); { return new object(); }/*</bind>*/;
                Diagnostic(ErrorCode.WRN_UnreferencedLocalFunction, "F").WithArguments("F").WithLocation(6, 26)
            };

            VerifyOperationTreeAndDiagnosticsForTest<LocalFunctionStatementSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation)]
        [Fact, WorkItem(24650, "https://github.com/dotnet/roslyn/issues/24650")]
        public void TestInvalidLocalFunction_BlockAndExpressionBody()
        {
            string source = @"
class C
{
    void M(int p)
    {
        /*<bind>*/object F() { return new object(); } => null;/*</bind>*/;
    }
}
";
            string expectedOperationTree = @"
ILocalFunctionOperation (Symbol: System.Object F()) (OperationKind.LocalFunction, Type: null, IsInvalid) (Syntax: 'object F()  ...  } => null;')
  Body: 
    IBlockOperation (1 statements) (OperationKind.Block, Type: null, IsInvalid) (Syntax: '{ return new object(); }')
      IReturnOperation (OperationKind.Return, Type: null, IsInvalid) (Syntax: 'return new object();')
        ReturnedValue: 
          IObjectCreationOperation (Constructor: System.Object..ctor()) (OperationKind.ObjectCreation, Type: System.Object, IsInvalid) (Syntax: 'new object()')
            Arguments(0)
            Initializer: 
              null
  IgnoredBody: 
    IBlockOperation (1 statements) (OperationKind.Block, Type: null, IsInvalid) (Syntax: '=> null')
      IReturnOperation (OperationKind.Return, Type: null, IsInvalid, IsImplicit) (Syntax: 'null')
        ReturnedValue: 
          IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.Object, Constant: null, IsInvalid, IsImplicit) (Syntax: 'null')
            Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: True, IsUserDefined: False) (MethodSymbol: null)
            Operand: 
              ILiteralOperation (OperationKind.Literal, Type: null, Constant: null, IsInvalid) (Syntax: 'null')
";
            var expectedDiagnostics = new DiagnosticDescription[] {
                // error CS8057: Block bodies and expression bodies cannot both be provided.
                //         /*<bind>*/object F() { return new object(); } => null;/*</bind>*/;
                Diagnostic(ErrorCode.ERR_BlockBodyAndExpressionBody, "object F() { return new object(); } => null;").WithLocation(6, 19),
                // warning CS8321: The local function 'F' is declared but never used
                //         /*<bind>*/object F() { return new object(); } => null;/*</bind>*/;
                Diagnostic(ErrorCode.WRN_UnreferencedLocalFunction, "F").WithArguments("F").WithLocation(6, 26)
            };

            VerifyOperationTreeAndDiagnosticsForTest<LocalFunctionStatementSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation)]
        [Fact, WorkItem(24650, "https://github.com/dotnet/roslyn/issues/24650")]
        public void TestLocalFunction_ExpressionBodyInnerMember()
        {
            string source = @"
class C
{
    public void M(int x)
    {
        int Local(int p1) /*<bind>*/=> x++/*</bind>*/;
        Local(0);
    }
}
";
            string expectedOperationTree = @"
IBlockOperation (1 statements) (OperationKind.Block, Type: null) (Syntax: '=> x++')
  IReturnOperation (OperationKind.Return, Type: null, IsImplicit) (Syntax: 'x++')
    ReturnedValue: 
      IIncrementOrDecrementOperation (Postfix) (OperationKind.Increment, Type: System.Int32) (Syntax: 'x++')
        Target: 
          IParameterReferenceOperation: x (OperationKind.ParameterReference, Type: System.Int32) (Syntax: 'x')
";
            var expectedDiagnostics = DiagnosticDescription.None;

            VerifyOperationTreeAndDiagnosticsForTest<ArrowExpressionClauseSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation, CompilerFeature.Dataflow)]
        [Fact]
        public void LocalFunctionFlow_01()
        {
            string source = @"
struct C
{
    void M()
/*<bind>*/{
        void local(bool result, bool input)
        {
            result = input;
        }

        local(false, true);
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
    Methods: [void local(System.Boolean result, System.Boolean input)]
    Block[B1] - Block
        Predecessors: [B0]
        Statements (1)
            IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'local(false, true);')
              Expression: 
                IInvocationOperation (void local(System.Boolean result, System.Boolean input)) (OperationKind.Invocation, Type: System.Void) (Syntax: 'local(false, true)')
                  Instance Receiver: 
                    null
                  Arguments(2):
                      IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: result) (OperationKind.Argument, Type: null) (Syntax: 'false')
                        ILiteralOperation (OperationKind.Literal, Type: System.Boolean, Constant: False) (Syntax: 'false')
                        InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                        OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                      IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: input) (OperationKind.Argument, Type: null) (Syntax: 'true')
                        ILiteralOperation (OperationKind.Literal, Type: System.Boolean, Constant: True) (Syntax: 'true')
                        InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                        OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)

        Next (Regular) Block[B2]
            Leaving: {R1}
    
    {   void local(System.Boolean result, System.Boolean input)
    
        Block[B0#0R1] - Entry
            Statements (0)
            Next (Regular) Block[B1#0R1]
        Block[B1#0R1] - Block
            Predecessors: [B0#0R1]
            Statements (1)
                IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'result = input;')
                  Expression: 
                    ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Boolean) (Syntax: 'result = input')
                      Left: 
                        IParameterReferenceOperation: result (OperationKind.ParameterReference, Type: System.Boolean) (Syntax: 'result')
                      Right: 
                        IParameterReferenceOperation: input (OperationKind.ParameterReference, Type: System.Boolean) (Syntax: 'input')

            Next (Regular) Block[B2#0R1]
        Block[B2#0R1] - Exit
            Predecessors: [B1#0R1]
            Statements (0)
    }
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
        public void LocalFunctionFlow_02()
        {
            string source = @"
#pragma warning disable CS8321
struct C
{
    void M()
/*<bind>*/{
        void local(bool result, bool input)
        {
            result = input;
        }
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
    Methods: [void local(System.Boolean result, System.Boolean input)]
    Block[B1] - Block
        Predecessors: [B0]
        Statements (0)
        Next (Regular) Block[B2]
            Leaving: {R1}
    
    {   void local(System.Boolean result, System.Boolean input)
    
        Block[B0#0R1] - Entry
            Statements (0)
            Next (Regular) Block[B1#0R1]
        Block[B1#0R1] - Block
            Predecessors: [B0#0R1]
            Statements (1)
                IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'result = input;')
                  Expression: 
                    ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Boolean) (Syntax: 'result = input')
                      Left: 
                        IParameterReferenceOperation: result (OperationKind.ParameterReference, Type: System.Boolean) (Syntax: 'result')
                      Right: 
                        IParameterReferenceOperation: input (OperationKind.ParameterReference, Type: System.Boolean) (Syntax: 'input')

            Next (Regular) Block[B2#0R1]
        Block[B2#0R1] - Exit
            Predecessors: [B1#0R1]
            Statements (0)
    }
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
        public void LocalFunctionFlow_03()
        {
            string source = @"
#pragma warning disable CS8321
struct C
{
    void M()
/*<bind>*/{
        void local(bool result, bool input)
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
    Methods: [void local(System.Boolean result, System.Boolean input)]
    Block[B1] - Block
        Predecessors: [B0]
        Statements (0)
        Next (Regular) Block[B2]
            Leaving: {R1}
    
    {   void local(System.Boolean result, System.Boolean input)
    
        Block[B0#0R1] - Entry
            Statements (0)
            Next (Regular) Block[B1#0R1]
        Block[B1#0R1] - Exit
            Predecessors: [B0#0R1]
            Statements (0)
    }
}

Block[B2] - Exit
    Predecessors: [B1]
    Statements (0)
";
            var expectedDiagnostics = new[] {
                // file.cs(7,44): error CS1002: ; expected
                //         void local(bool result, bool input)
                Diagnostic(ErrorCode.ERR_SemicolonExpected, "").WithLocation(7, 44),
                // file.cs(7,14): error CS8112: 'local(bool, bool)' is a local function and must therefore always have a body.
                //         void local(bool result, bool input)
                Diagnostic(ErrorCode.ERR_LocalFunctionMissingBody, "local").WithArguments("local(bool, bool)").WithLocation(7, 14)
            };

            VerifyFlowGraphAndDiagnosticsForTest<BlockSyntax>(source, expectedGraph, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation, CompilerFeature.Dataflow)]
        [Fact]
        public void LocalFunctionFlow_04()
        {
            string source = @"
#pragma warning disable CS8321
struct C
{
    void M()
/*<bind>*/{
        void local(bool result, bool input1, bool input2)
        {
            result = input1;
        } 
        => result = input2;
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
    Methods: [void local(System.Boolean result, System.Boolean input1, System.Boolean input2)]
    Block[B1] - Block
        Predecessors: [B0]
        Statements (0)
        Next (Regular) Block[B2]
            Leaving: {R1}
    
    {   void local(System.Boolean result, System.Boolean input1, System.Boolean input2)
    
        Block[B0#0R1] - Entry
            Statements (0)
            Next (Regular) Block[B1#0R1]
        Block[B1#0R1] - Block
            Predecessors: [B0#0R1]
            Statements (1)
                IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null, IsInvalid) (Syntax: 'result = input1;')
                  Expression: 
                    ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Boolean, IsInvalid) (Syntax: 'result = input1')
                      Left: 
                        IParameterReferenceOperation: result (OperationKind.ParameterReference, Type: System.Boolean, IsInvalid) (Syntax: 'result')
                      Right: 
                        IParameterReferenceOperation: input1 (OperationKind.ParameterReference, Type: System.Boolean, IsInvalid) (Syntax: 'input1')

            Next (Regular) Block[B3#0R1]

        .erroneous body {R1#0R1}
        {
            Block[B2#0R1] - Block [UnReachable]
                Predecessors (0)
                Statements (1)
                    IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null, IsInvalid, IsImplicit) (Syntax: 'result = input2')
                      Expression: 
                        ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Boolean, IsInvalid) (Syntax: 'result = input2')
                          Left: 
                            IParameterReferenceOperation: result (OperationKind.ParameterReference, Type: System.Boolean, IsInvalid) (Syntax: 'result')
                          Right: 
                            IParameterReferenceOperation: input2 (OperationKind.ParameterReference, Type: System.Boolean, IsInvalid) (Syntax: 'input2')

                Next (Regular) Block[B3#0R1]
                    Leaving: {R1#0R1}
        }

        Block[B3#0R1] - Exit
            Predecessors: [B1#0R1] [B2#0R1]
            Statements (0)
    }
}

Block[B2] - Exit
    Predecessors: [B1]
    Statements (0)
";
            var expectedDiagnostics = new[] {
                // file.cs(7,9): error CS8057: Block bodies and expression bodies cannot both be provided.
                //         void local(bool result, bool input1, bool input2)
                Diagnostic(ErrorCode.ERR_BlockBodyAndExpressionBody, @"void local(bool result, bool input1, bool input2)
        {
            result = input1;
        } 
        => result = input2;").WithLocation(7, 9)
            };

            VerifyFlowGraphAndDiagnosticsForTest<BlockSyntax>(source, expectedGraph, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation, CompilerFeature.Dataflow)]
        [Fact]
        public void LocalFunctionFlow_05()
        {
            string source = @"
#pragma warning disable CS8321
struct C
{
    void M()
/*<bind>*/{
        void local1(bool result1, bool input1)
        {
            result1 = input1;
        }
        void local2(bool result2, bool input2) => result2 = input2;
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
    Methods: [void local1(System.Boolean result1, System.Boolean input1)] [void local2(System.Boolean result2, System.Boolean input2)]
    Block[B1] - Block
        Predecessors: [B0]
        Statements (0)
        Next (Regular) Block[B2]
            Leaving: {R1}
    
    {   void local1(System.Boolean result1, System.Boolean input1)
    
        Block[B0#0R1] - Entry
            Statements (0)
            Next (Regular) Block[B1#0R1]
        Block[B1#0R1] - Block
            Predecessors: [B0#0R1]
            Statements (1)
                IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'result1 = input1;')
                  Expression: 
                    ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Boolean) (Syntax: 'result1 = input1')
                      Left: 
                        IParameterReferenceOperation: result1 (OperationKind.ParameterReference, Type: System.Boolean) (Syntax: 'result1')
                      Right: 
                        IParameterReferenceOperation: input1 (OperationKind.ParameterReference, Type: System.Boolean) (Syntax: 'input1')

            Next (Regular) Block[B2#0R1]
        Block[B2#0R1] - Exit
            Predecessors: [B1#0R1]
            Statements (0)
    }
    
    {   void local2(System.Boolean result2, System.Boolean input2)
    
        Block[B0#1R1] - Entry
            Statements (0)
            Next (Regular) Block[B1#1R1]
        Block[B1#1R1] - Block
            Predecessors: [B0#1R1]
            Statements (1)
                IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null, IsImplicit) (Syntax: 'result2 = input2')
                  Expression: 
                    ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Boolean) (Syntax: 'result2 = input2')
                      Left: 
                        IParameterReferenceOperation: result2 (OperationKind.ParameterReference, Type: System.Boolean) (Syntax: 'result2')
                      Right: 
                        IParameterReferenceOperation: input2 (OperationKind.ParameterReference, Type: System.Boolean) (Syntax: 'input2')

            Next (Regular) Block[B2#1R1]
        Block[B2#1R1] - Exit
            Predecessors: [B1#1R1]
            Statements (0)
    }
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
        public void LocalFunctionFlow_06()
        {
            string source = @"
struct C
{
    void M(int input)
/*<bind>*/{
        int result;
        local1(input);

        int local1(int input1)
        {
            int i = local1(input1);
            result = local1(i);
            return result;
        }

        local1(result);
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
    Locals: [System.Int32 result]
    Methods: [System.Int32 local1(System.Int32 input1)]
    Block[B1] - Block
        Predecessors: [B0]
        Statements (2)
            IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'local1(input);')
              Expression: 
                IInvocationOperation (System.Int32 local1(System.Int32 input1)) (OperationKind.Invocation, Type: System.Int32) (Syntax: 'local1(input)')
                  Instance Receiver: 
                    null
                  Arguments(1):
                      IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: input1) (OperationKind.Argument, Type: null) (Syntax: 'input')
                        IParameterReferenceOperation: input (OperationKind.ParameterReference, Type: System.Int32) (Syntax: 'input')
                        InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                        OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)

            IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'local1(result);')
              Expression: 
                IInvocationOperation (System.Int32 local1(System.Int32 input1)) (OperationKind.Invocation, Type: System.Int32) (Syntax: 'local1(result)')
                  Instance Receiver: 
                    null
                  Arguments(1):
                      IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: input1) (OperationKind.Argument, Type: null) (Syntax: 'result')
                        ILocalReferenceOperation: result (OperationKind.LocalReference, Type: System.Int32) (Syntax: 'result')
                        InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                        OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)

        Next (Regular) Block[B2]
            Leaving: {R1}
    
    {   System.Int32 local1(System.Int32 input1)
    
        Block[B0#0R1] - Entry
            Statements (0)
            Next (Regular) Block[B1#0R1]
                Entering: {R1#0R1}

        .locals {R1#0R1}
        {
            Locals: [System.Int32 i]
            Block[B1#0R1] - Block
                Predecessors: [B0#0R1]
                Statements (2)
                    ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Int32, IsImplicit) (Syntax: 'i = local1(input1)')
                      Left: 
                        ILocalReferenceOperation: i (IsDeclaration: True) (OperationKind.LocalReference, Type: System.Int32, IsImplicit) (Syntax: 'i = local1(input1)')
                      Right: 
                        IInvocationOperation (System.Int32 local1(System.Int32 input1)) (OperationKind.Invocation, Type: System.Int32) (Syntax: 'local1(input1)')
                          Instance Receiver: 
                            null
                          Arguments(1):
                              IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: input1) (OperationKind.Argument, Type: null) (Syntax: 'input1')
                                IParameterReferenceOperation: input1 (OperationKind.ParameterReference, Type: System.Int32) (Syntax: 'input1')
                                InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                                OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)

                    IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'result = local1(i);')
                      Expression: 
                        ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Int32) (Syntax: 'result = local1(i)')
                          Left: 
                            ILocalReferenceOperation: result (OperationKind.LocalReference, Type: System.Int32) (Syntax: 'result')
                          Right: 
                            IInvocationOperation (System.Int32 local1(System.Int32 input1)) (OperationKind.Invocation, Type: System.Int32) (Syntax: 'local1(i)')
                              Instance Receiver: 
                                null
                              Arguments(1):
                                  IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: input1) (OperationKind.Argument, Type: null) (Syntax: 'i')
                                    ILocalReferenceOperation: i (OperationKind.LocalReference, Type: System.Int32) (Syntax: 'i')
                                    InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                                    OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)

                Next (Return) Block[B2#0R1]
                    ILocalReferenceOperation: result (OperationKind.LocalReference, Type: System.Int32) (Syntax: 'result')
                    Leaving: {R1#0R1}
        }

        Block[B2#0R1] - Exit
            Predecessors: [B1#0R1]
            Statements (0)
    }
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
        public void LocalFunctionFlow_07()
        {
            string source = @"
#pragma warning disable CS8321
struct C
{
    void M()
/*<bind>*/{
        try
        {
            void local(bool result, bool input)
            {
                result = input;
            }
        }
        finally
        {}
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
    Methods: [void local(System.Boolean result, System.Boolean input)]
    Block[B1] - Block
        Predecessors: [B0]
        Statements (0)
        Next (Regular) Block[B2]
            Leaving: {R1}
    
    {   void local(System.Boolean result, System.Boolean input)
    
        Block[B0#0R1] - Entry
            Statements (0)
            Next (Regular) Block[B1#0R1]
        Block[B1#0R1] - Block
            Predecessors: [B0#0R1]
            Statements (1)
                IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'result = input;')
                  Expression: 
                    ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Boolean) (Syntax: 'result = input')
                      Left: 
                        IParameterReferenceOperation: result (OperationKind.ParameterReference, Type: System.Boolean) (Syntax: 'result')
                      Right: 
                        IParameterReferenceOperation: input (OperationKind.ParameterReference, Type: System.Boolean) (Syntax: 'input')

            Next (Regular) Block[B2#0R1]
        Block[B2#0R1] - Exit
            Predecessors: [B1#0R1]
            Statements (0)
    }
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
        public void LocalFunctionFlow_08()
        {
            string source = @"
#pragma warning disable CS8321
struct C
{
    void M()
/*<bind>*/{
        int i = 0;

        void local1(int input1)
        {
            input1 = 1;
            i++;

            void local2(bool input2)
            {
                input2 = true;
                i++;
            }
        }
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
    Methods: [void local1(System.Int32 input1)]
    Block[B1] - Block
        Predecessors: [B0]
        Statements (1)
            ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Int32, IsImplicit) (Syntax: 'i = 0')
              Left: 
                ILocalReferenceOperation: i (IsDeclaration: True) (OperationKind.LocalReference, Type: System.Int32, IsImplicit) (Syntax: 'i = 0')
              Right: 
                ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 0) (Syntax: '0')

        Next (Regular) Block[B2]
            Leaving: {R1}
    
    {   void local1(System.Int32 input1)
    
        Block[B0#0R1] - Entry
            Statements (0)
            Next (Regular) Block[B1#0R1]
                Entering: {R1#0R1}

        .locals {R1#0R1}
        {
            Methods: [void local2(System.Boolean input2)]
            Block[B1#0R1] - Block
                Predecessors: [B0#0R1]
                Statements (2)
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

                Next (Regular) Block[B2#0R1]
                    Leaving: {R1#0R1}
            
            {   void local2(System.Boolean input2)
            
                Block[B0#0R1#0R1] - Entry
                    Statements (0)
                    Next (Regular) Block[B1#0R1#0R1]
                Block[B1#0R1#0R1] - Block
                    Predecessors: [B0#0R1#0R1]
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

                    Next (Regular) Block[B2#0R1#0R1]
                Block[B2#0R1#0R1] - Exit
                    Predecessors: [B1#0R1#0R1]
                    Statements (0)
            }
        }

        Block[B2#0R1] - Exit
            Predecessors: [B1#0R1]
            Statements (0)
    }
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
        public void LocalFunctionFlow_09()
        {
            string source = @"
#pragma warning disable CS8321
class C
{
    void M(C x, C y, C z)
/*<bind>*/{
        x = y ?? z;

        void local(C result, C input1, C input2)
        {
            result = input1 ?? input2;
        }
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
    Methods: [void local(C result, C input1, C input2)]
    Block[B1] - Block
        Predecessors: [B0]
        Statements (2)
            IFlowCaptureOperation: 0 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'x')
              Value: 
                IParameterReferenceOperation: x (OperationKind.ParameterReference, Type: C) (Syntax: 'x')

            IFlowCaptureOperation: 1 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'y')
              Value: 
                IParameterReferenceOperation: y (OperationKind.ParameterReference, Type: C) (Syntax: 'y')

        Jump if True (Regular) to Block[B3]
            IIsNullOperation (OperationKind.IsNull, Type: System.Boolean, IsImplicit) (Syntax: 'y')
              Operand: 
                IFlowCaptureReferenceOperation: 1 (OperationKind.FlowCaptureReference, Type: C, IsImplicit) (Syntax: 'y')

        Next (Regular) Block[B2]
    Block[B2] - Block
        Predecessors: [B1]
        Statements (1)
            IFlowCaptureOperation: 2 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'y')
              Value: 
                IFlowCaptureReferenceOperation: 1 (OperationKind.FlowCaptureReference, Type: C, IsImplicit) (Syntax: 'y')

        Next (Regular) Block[B4]
    Block[B3] - Block
        Predecessors: [B1]
        Statements (1)
            IFlowCaptureOperation: 2 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'z')
              Value: 
                IParameterReferenceOperation: z (OperationKind.ParameterReference, Type: C) (Syntax: 'z')

        Next (Regular) Block[B4]
    Block[B4] - Block
        Predecessors: [B2] [B3]
        Statements (1)
            IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'x = y ?? z;')
              Expression: 
                ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: C) (Syntax: 'x = y ?? z')
                  Left: 
                    IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: C, IsImplicit) (Syntax: 'x')
                  Right: 
                    IFlowCaptureReferenceOperation: 2 (OperationKind.FlowCaptureReference, Type: C, IsImplicit) (Syntax: 'y ?? z')

        Next (Regular) Block[B5]
            Leaving: {R1}
    
    {   void local(C result, C input1, C input2)
    
        Block[B0#0R1] - Entry
            Statements (0)
            Next (Regular) Block[B1#0R1]
        Block[B1#0R1] - Block
            Predecessors: [B0#0R1]
            Statements (2)
                IFlowCaptureOperation: 3 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'result')
                  Value: 
                    IParameterReferenceOperation: result (OperationKind.ParameterReference, Type: C) (Syntax: 'result')

                IFlowCaptureOperation: 4 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'input1')
                  Value: 
                    IParameterReferenceOperation: input1 (OperationKind.ParameterReference, Type: C) (Syntax: 'input1')

            Jump if True (Regular) to Block[B3#0R1]
                IIsNullOperation (OperationKind.IsNull, Type: System.Boolean, IsImplicit) (Syntax: 'input1')
                  Operand: 
                    IFlowCaptureReferenceOperation: 4 (OperationKind.FlowCaptureReference, Type: C, IsImplicit) (Syntax: 'input1')

            Next (Regular) Block[B2#0R1]
        Block[B2#0R1] - Block
            Predecessors: [B1#0R1]
            Statements (1)
                IFlowCaptureOperation: 5 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'input1')
                  Value: 
                    IFlowCaptureReferenceOperation: 4 (OperationKind.FlowCaptureReference, Type: C, IsImplicit) (Syntax: 'input1')

            Next (Regular) Block[B4#0R1]
        Block[B3#0R1] - Block
            Predecessors: [B1#0R1]
            Statements (1)
                IFlowCaptureOperation: 5 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'input2')
                  Value: 
                    IParameterReferenceOperation: input2 (OperationKind.ParameterReference, Type: C) (Syntax: 'input2')

            Next (Regular) Block[B4#0R1]
        Block[B4#0R1] - Block
            Predecessors: [B2#0R1] [B3#0R1]
            Statements (1)
                IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'result = in ...  ?? input2;')
                  Expression: 
                    ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: C) (Syntax: 'result = in ... 1 ?? input2')
                      Left: 
                        IFlowCaptureReferenceOperation: 3 (OperationKind.FlowCaptureReference, Type: C, IsImplicit) (Syntax: 'result')
                      Right: 
                        IFlowCaptureReferenceOperation: 5 (OperationKind.FlowCaptureReference, Type: C, IsImplicit) (Syntax: 'input1 ?? input2')

            Next (Regular) Block[B5#0R1]
        Block[B5#0R1] - Exit
            Predecessors: [B4#0R1]
            Statements (0)
    }
}

Block[B5] - Exit
    Predecessors: [B4]
    Statements (0)
";
            var expectedDiagnostics = DiagnosticDescription.None;

            VerifyFlowGraphAndDiagnosticsForTest<BlockSyntax>(source, expectedGraph, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation, CompilerFeature.Dataflow)]
        [Fact]
        public void LocalFunctionFlow_10()
        {
            string source = @"
#pragma warning disable CS8321
class C
{
    void M()
/*<bind>*/{
        void local1(C result1, C input11, C input12)
        {
            result1 = input11 ?? input12;
        }
        void local2(C result2, C input21, C input22)
        {
            result2 = input21 ?? input22;
        }
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
    Methods: [void local1(C result1, C input11, C input12)] [void local2(C result2, C input21, C input22)]
    Block[B1] - Block
        Predecessors: [B0]
        Statements (0)
        Next (Regular) Block[B2]
            Leaving: {R1}
    
    {   void local1(C result1, C input11, C input12)
    
        Block[B0#0R1] - Entry
            Statements (0)
            Next (Regular) Block[B1#0R1]
        Block[B1#0R1] - Block
            Predecessors: [B0#0R1]
            Statements (2)
                IFlowCaptureOperation: 0 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'result1')
                  Value: 
                    IParameterReferenceOperation: result1 (OperationKind.ParameterReference, Type: C) (Syntax: 'result1')

                IFlowCaptureOperation: 1 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'input11')
                  Value: 
                    IParameterReferenceOperation: input11 (OperationKind.ParameterReference, Type: C) (Syntax: 'input11')

            Jump if True (Regular) to Block[B3#0R1]
                IIsNullOperation (OperationKind.IsNull, Type: System.Boolean, IsImplicit) (Syntax: 'input11')
                  Operand: 
                    IFlowCaptureReferenceOperation: 1 (OperationKind.FlowCaptureReference, Type: C, IsImplicit) (Syntax: 'input11')

            Next (Regular) Block[B2#0R1]
        Block[B2#0R1] - Block
            Predecessors: [B1#0R1]
            Statements (1)
                IFlowCaptureOperation: 2 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'input11')
                  Value: 
                    IFlowCaptureReferenceOperation: 1 (OperationKind.FlowCaptureReference, Type: C, IsImplicit) (Syntax: 'input11')

            Next (Regular) Block[B4#0R1]
        Block[B3#0R1] - Block
            Predecessors: [B1#0R1]
            Statements (1)
                IFlowCaptureOperation: 2 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'input12')
                  Value: 
                    IParameterReferenceOperation: input12 (OperationKind.ParameterReference, Type: C) (Syntax: 'input12')

            Next (Regular) Block[B4#0R1]
        Block[B4#0R1] - Block
            Predecessors: [B2#0R1] [B3#0R1]
            Statements (1)
                IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'result1 = i ... ?? input12;')
                  Expression: 
                    ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: C) (Syntax: 'result1 = i ...  ?? input12')
                      Left: 
                        IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: C, IsImplicit) (Syntax: 'result1')
                      Right: 
                        IFlowCaptureReferenceOperation: 2 (OperationKind.FlowCaptureReference, Type: C, IsImplicit) (Syntax: 'input11 ?? input12')

            Next (Regular) Block[B5#0R1]
        Block[B5#0R1] - Exit
            Predecessors: [B4#0R1]
            Statements (0)
    }
    
    {   void local2(C result2, C input21, C input22)
    
        Block[B0#1R1] - Entry
            Statements (0)
            Next (Regular) Block[B1#1R1]
        Block[B1#1R1] - Block
            Predecessors: [B0#1R1]
            Statements (2)
                IFlowCaptureOperation: 3 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'result2')
                  Value: 
                    IParameterReferenceOperation: result2 (OperationKind.ParameterReference, Type: C) (Syntax: 'result2')

                IFlowCaptureOperation: 4 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'input21')
                  Value: 
                    IParameterReferenceOperation: input21 (OperationKind.ParameterReference, Type: C) (Syntax: 'input21')

            Jump if True (Regular) to Block[B3#1R1]
                IIsNullOperation (OperationKind.IsNull, Type: System.Boolean, IsImplicit) (Syntax: 'input21')
                  Operand: 
                    IFlowCaptureReferenceOperation: 4 (OperationKind.FlowCaptureReference, Type: C, IsImplicit) (Syntax: 'input21')

            Next (Regular) Block[B2#1R1]
        Block[B2#1R1] - Block
            Predecessors: [B1#1R1]
            Statements (1)
                IFlowCaptureOperation: 5 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'input21')
                  Value: 
                    IFlowCaptureReferenceOperation: 4 (OperationKind.FlowCaptureReference, Type: C, IsImplicit) (Syntax: 'input21')

            Next (Regular) Block[B4#1R1]
        Block[B3#1R1] - Block
            Predecessors: [B1#1R1]
            Statements (1)
                IFlowCaptureOperation: 5 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'input22')
                  Value: 
                    IParameterReferenceOperation: input22 (OperationKind.ParameterReference, Type: C) (Syntax: 'input22')

            Next (Regular) Block[B4#1R1]
        Block[B4#1R1] - Block
            Predecessors: [B2#1R1] [B3#1R1]
            Statements (1)
                IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'result2 = i ... ?? input22;')
                  Expression: 
                    ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: C) (Syntax: 'result2 = i ...  ?? input22')
                      Left: 
                        IFlowCaptureReferenceOperation: 3 (OperationKind.FlowCaptureReference, Type: C, IsImplicit) (Syntax: 'result2')
                      Right: 
                        IFlowCaptureReferenceOperation: 5 (OperationKind.FlowCaptureReference, Type: C, IsImplicit) (Syntax: 'input21 ?? input22')

            Next (Regular) Block[B5#1R1]
        Block[B5#1R1] - Exit
            Predecessors: [B4#1R1]
            Statements (0)
    }
}

Block[B2] - Exit
    Predecessors: [B1]
    Statements (0)
";
            var expectedDiagnostics = DiagnosticDescription.None;

            VerifyFlowGraphAndDiagnosticsForTest<BlockSyntax>(source, expectedGraph, expectedDiagnostics);
        }
    }
}
