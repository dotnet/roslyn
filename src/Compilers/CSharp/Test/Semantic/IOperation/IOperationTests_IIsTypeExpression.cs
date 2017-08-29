// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.CSharp.Test.Utilities;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.CSharp.UnitTests
{
    public partial class IOperationTests : SemanticModelTestBase
    {
        [CompilerTrait(CompilerFeature.IOperation)]
        [Fact]
        public void TestIsOperator()
        {
            string source = @"
namespace TestIsOperator
{
    class TestType
    {
    }

    class C
    {
        static void Main(string myStr)
        /*<bind>*/{
            object o = myStr;
            bool b = o is string;

            int myInt = 3;
            b = myInt is int;

            TestType tt = null;
            o = tt;
            b = o is TestType;

            b = null is TestType;
        }/*</bind>*/
    }
}
";
            string expectedOperationTree = @"
IBlockStatement (8 statements, 4 locals) (OperationKind.BlockStatement) (Syntax: '{ ... }')
  Locals: Local_1: System.Object o
    Local_2: System.Boolean b
    Local_3: System.Int32 myInt
    Local_4: TestIsOperator.TestType tt
  IVariableDeclarationStatement (1 declarations) (OperationKind.VariableDeclarationStatement) (Syntax: 'object o = myStr;')
    IVariableDeclaration (1 variables) (OperationKind.VariableDeclaration) (Syntax: 'object o = myStr;')
      Variables: Local_1: System.Object o
      Initializer: IConversionExpression (Implicit, TryCast: False, Unchecked) (OperationKind.ConversionExpression, Type: System.Object) (Syntax: 'myStr')
          Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: True, IsUserDefined: False) (MethodSymbol: null)
          Operand: IParameterReferenceExpression: myStr (OperationKind.ParameterReferenceExpression, Type: System.String) (Syntax: 'myStr')
  IVariableDeclarationStatement (1 declarations) (OperationKind.VariableDeclarationStatement) (Syntax: 'bool b = o is string;')
    IVariableDeclaration (1 variables) (OperationKind.VariableDeclaration) (Syntax: 'bool b = o is string;')
      Variables: Local_1: System.Boolean b
      Initializer: IIsTypeExpression (OperationKind.IsTypeExpression, Type: System.Boolean) (Syntax: 'o is string')
          Operand: ILocalReferenceExpression: o (OperationKind.LocalReferenceExpression, Type: System.Object) (Syntax: 'o')
          IsType: System.String
  IVariableDeclarationStatement (1 declarations) (OperationKind.VariableDeclarationStatement) (Syntax: 'int myInt = 3;')
    IVariableDeclaration (1 variables) (OperationKind.VariableDeclaration) (Syntax: 'int myInt = 3;')
      Variables: Local_1: System.Int32 myInt
      Initializer: ILiteralExpression (OperationKind.LiteralExpression, Type: System.Int32, Constant: 3) (Syntax: '3')
  IExpressionStatement (OperationKind.ExpressionStatement) (Syntax: 'b = myInt is int;')
    Expression: ISimpleAssignmentExpression (OperationKind.SimpleAssignmentExpression, Type: System.Boolean) (Syntax: 'b = myInt is int')
        Left: ILocalReferenceExpression: b (OperationKind.LocalReferenceExpression, Type: System.Boolean) (Syntax: 'b')
        Right: IIsTypeExpression (OperationKind.IsTypeExpression, Type: System.Boolean) (Syntax: 'myInt is int')
            Operand: ILocalReferenceExpression: myInt (OperationKind.LocalReferenceExpression, Type: System.Int32) (Syntax: 'myInt')
            IsType: System.Int32
  IVariableDeclarationStatement (1 declarations) (OperationKind.VariableDeclarationStatement) (Syntax: 'TestType tt = null;')
    IVariableDeclaration (1 variables) (OperationKind.VariableDeclaration) (Syntax: 'TestType tt = null;')
      Variables: Local_1: TestIsOperator.TestType tt
      Initializer: IConversionExpression (Implicit, TryCast: False, Unchecked) (OperationKind.ConversionExpression, Type: TestIsOperator.TestType, Constant: null) (Syntax: 'null')
          Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: True, IsUserDefined: False) (MethodSymbol: null)
          Operand: ILiteralExpression (OperationKind.LiteralExpression, Type: null, Constant: null) (Syntax: 'null')
  IExpressionStatement (OperationKind.ExpressionStatement) (Syntax: 'o = tt;')
    Expression: ISimpleAssignmentExpression (OperationKind.SimpleAssignmentExpression, Type: System.Object) (Syntax: 'o = tt')
        Left: ILocalReferenceExpression: o (OperationKind.LocalReferenceExpression, Type: System.Object) (Syntax: 'o')
        Right: IConversionExpression (Implicit, TryCast: False, Unchecked) (OperationKind.ConversionExpression, Type: System.Object) (Syntax: 'tt')
            Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: True, IsUserDefined: False) (MethodSymbol: null)
            Operand: ILocalReferenceExpression: tt (OperationKind.LocalReferenceExpression, Type: TestIsOperator.TestType) (Syntax: 'tt')
  IExpressionStatement (OperationKind.ExpressionStatement) (Syntax: 'b = o is TestType;')
    Expression: ISimpleAssignmentExpression (OperationKind.SimpleAssignmentExpression, Type: System.Boolean) (Syntax: 'b = o is TestType')
        Left: ILocalReferenceExpression: b (OperationKind.LocalReferenceExpression, Type: System.Boolean) (Syntax: 'b')
        Right: IIsTypeExpression (OperationKind.IsTypeExpression, Type: System.Boolean) (Syntax: 'o is TestType')
            Operand: ILocalReferenceExpression: o (OperationKind.LocalReferenceExpression, Type: System.Object) (Syntax: 'o')
            IsType: TestIsOperator.TestType
  IExpressionStatement (OperationKind.ExpressionStatement) (Syntax: 'b = null is TestType;')
    Expression: ISimpleAssignmentExpression (OperationKind.SimpleAssignmentExpression, Type: System.Boolean) (Syntax: 'b = null is TestType')
        Left: ILocalReferenceExpression: b (OperationKind.LocalReferenceExpression, Type: System.Boolean) (Syntax: 'b')
        Right: IIsTypeExpression (OperationKind.IsTypeExpression, Type: System.Boolean) (Syntax: 'null is TestType')
            Operand: ILiteralExpression (OperationKind.LiteralExpression, Type: null, Constant: null) (Syntax: 'null')
            IsType: TestIsOperator.TestType";

            var expectedDiagnostics = new DiagnosticDescription[] {
                // CS0183: The given expression is always of the provided ('int') type
                //             b = myInt is int;
                Diagnostic(ErrorCode.WRN_IsAlwaysTrue, "myInt is int").WithArguments("int").WithLocation(16, 17),
                // CS0184: The given expression is never of the provided ('TestType') type
                //             b = null is TestType;
                Diagnostic(ErrorCode.WRN_IsAlwaysFalse, "null is TestType").WithArguments("TestIsOperator.TestType").WithLocation(22, 17)
            };

            VerifyOperationTreeAndDiagnosticsForTest<BlockSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation)]
        [Fact]
        public void TestIsOperatorGeneric()
        {
            string source = @"
namespace TestIsOperatorGeneric
{
    class C
    {
        public static void Main() { }
        public static void M<T, U, W>(T t, U u)
            where T : class
            where U : class
        /*<bind>*/{
            bool test = t is int;
            test = u is object;
            test = t is U;
            test = t is T;
        }/*</bind>*/
    }
}
";
            string expectedOperationTree = @"
IBlockStatement (4 statements, 1 locals) (OperationKind.BlockStatement) (Syntax: '{ ... }')
  Locals: Local_1: System.Boolean test
  IVariableDeclarationStatement (1 declarations) (OperationKind.VariableDeclarationStatement) (Syntax: 'bool test = t is int;')
    IVariableDeclaration (1 variables) (OperationKind.VariableDeclaration) (Syntax: 'bool test = t is int;')
      Variables: Local_1: System.Boolean test
      Initializer: IIsTypeExpression (OperationKind.IsTypeExpression, Type: System.Boolean) (Syntax: 't is int')
          Operand: IParameterReferenceExpression: t (OperationKind.ParameterReferenceExpression, Type: T) (Syntax: 't')
          IsType: System.Int32
  IExpressionStatement (OperationKind.ExpressionStatement) (Syntax: 'test = u is object;')
    Expression: ISimpleAssignmentExpression (OperationKind.SimpleAssignmentExpression, Type: System.Boolean) (Syntax: 'test = u is object')
        Left: ILocalReferenceExpression: test (OperationKind.LocalReferenceExpression, Type: System.Boolean) (Syntax: 'test')
        Right: IIsTypeExpression (OperationKind.IsTypeExpression, Type: System.Boolean) (Syntax: 'u is object')
            Operand: IParameterReferenceExpression: u (OperationKind.ParameterReferenceExpression, Type: U) (Syntax: 'u')
            IsType: System.Object
  IExpressionStatement (OperationKind.ExpressionStatement) (Syntax: 'test = t is U;')
    Expression: ISimpleAssignmentExpression (OperationKind.SimpleAssignmentExpression, Type: System.Boolean) (Syntax: 'test = t is U')
        Left: ILocalReferenceExpression: test (OperationKind.LocalReferenceExpression, Type: System.Boolean) (Syntax: 'test')
        Right: IIsTypeExpression (OperationKind.IsTypeExpression, Type: System.Boolean) (Syntax: 't is U')
            Operand: IParameterReferenceExpression: t (OperationKind.ParameterReferenceExpression, Type: T) (Syntax: 't')
            IsType: U
  IExpressionStatement (OperationKind.ExpressionStatement) (Syntax: 'test = t is T;')
    Expression: ISimpleAssignmentExpression (OperationKind.SimpleAssignmentExpression, Type: System.Boolean) (Syntax: 'test = t is T')
        Left: ILocalReferenceExpression: test (OperationKind.LocalReferenceExpression, Type: System.Boolean) (Syntax: 'test')
        Right: IIsTypeExpression (OperationKind.IsTypeExpression, Type: System.Boolean) (Syntax: 't is T')
            Operand: IParameterReferenceExpression: t (OperationKind.ParameterReferenceExpression, Type: T) (Syntax: 't')
            IsType: T
";
            var expectedDiagnostics = DiagnosticDescription.None;

            VerifyOperationTreeAndDiagnosticsForTest<BlockSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }
    }
}
