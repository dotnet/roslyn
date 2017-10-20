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
        public void TestIsOperator_ObjectExpressionStringType()
        {
            string source = @"
namespace TestIsOperator
{
    class TestType
    {
    }

    class C
    {
        static void M(string myStr)
        {
            object o = myStr;
            bool b = /*<bind>*/o is string/*</bind>*/;
        }
    }
}
";
            string expectedOperationTree = @"
IIsTypeOperation (OperationKind.IsType, IsExpression, Type: System.Boolean) (Syntax: 'o is string')
  Operand: 
    ILocalReferenceOperation: o (OperationKind.LocalReference, IsExpression, Type: System.Object) (Syntax: 'o')
  IsType: System.String
";
            var expectedDiagnostics = DiagnosticDescription.None;

            VerifyOperationTreeAndDiagnosticsForTest<BinaryExpressionSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation)]
        [Fact]
        public void TestIsOperator_IntExpressionIntType()
        {
            string source = @"
namespace TestIsOperator
{
    class TestType
    {
    }

    class C
    {
        static void M(string myStr)
        {
            int myInt = 3;
            bool b = /*<bind>*/myInt is int/*</bind>*/;
        }
    }
}
";
            string expectedOperationTree = @"
IIsTypeOperation (OperationKind.IsType, IsExpression, Type: System.Boolean) (Syntax: 'myInt is int')
  Operand: 
    ILocalReferenceOperation: myInt (OperationKind.LocalReference, IsExpression, Type: System.Int32) (Syntax: 'myInt')
  IsType: System.Int32
";
            var expectedDiagnostics = new DiagnosticDescription[] {
                // CS0183: The given expression is always of the provided ('int') type
                //             bool b = /*<bind>*/myInt is int/*</bind>*/;
                Diagnostic(ErrorCode.WRN_IsAlwaysTrue, "myInt is int").WithArguments("int").WithLocation(13, 32)
            };

            VerifyOperationTreeAndDiagnosticsForTest<BinaryExpressionSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation)]
        [Fact]
        public void TestIsOperator_ObjectExpressionUserDefinedType()
        {
            string source = @"
namespace TestIsOperator
{
    class TestType
    {
    }

    class C
    {
        static void M(string myStr)
        {
            TestType tt = null;
            object o = tt;
            bool b = /*<bind>*/o is TestType/*</bind>*/;
        }
    }
}
";
            string expectedOperationTree = @"
IIsTypeOperation (OperationKind.IsType, IsExpression, Type: System.Boolean) (Syntax: 'o is TestType')
  Operand: 
    ILocalReferenceOperation: o (OperationKind.LocalReference, IsExpression, Type: System.Object) (Syntax: 'o')
  IsType: TestIsOperator.TestType
";
            var expectedDiagnostics = DiagnosticDescription.None;

            VerifyOperationTreeAndDiagnosticsForTest<BinaryExpressionSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation)]
        [Fact]
        public void TestIsOperator_NullExpressionUserDefinedType()
        {
            string source = @"
namespace TestIsOperator
{
    class TestType
    {
    }

    class C
    {
        static void M(string myStr)
        {
            TestType tt = null;
            object o = tt;
            bool b = /*<bind>*/null is TestType/*</bind>*/;
        }
    }
}
";
            string expectedOperationTree = @"
IIsTypeOperation (OperationKind.IsType, IsExpression, Type: System.Boolean) (Syntax: 'null is TestType')
  Operand: 
    ILiteralOperation (OperationKind.Literal, IsExpression, Type: null, Constant: null) (Syntax: 'null')
  IsType: TestIsOperator.TestType
";
            var expectedDiagnostics = new DiagnosticDescription[] {
                // CS0184: The given expression is never of the provided ('TestType') type
                //             bool b = /*<bind>*/null is TestType/*</bind>*/;
                Diagnostic(ErrorCode.WRN_IsAlwaysFalse, "null is TestType").WithArguments("TestIsOperator.TestType").WithLocation(14, 32)
            };

            VerifyOperationTreeAndDiagnosticsForTest<BinaryExpressionSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation)]
        [Fact]
        public void TestIsOperator_IntExpressionEnumType()
        {
            string source = @"
class IsTest
{
    static void Main()
    {
        var b = /*<bind>*/1 is color/*</bind>*/;
        System.Console.WriteLine(b);
    }
}
enum color
{ }
";
            string expectedOperationTree = @"
IIsTypeOperation (OperationKind.IsType, IsExpression, Type: System.Boolean) (Syntax: '1 is color')
  Operand: 
    ILiteralOperation (OperationKind.Literal, IsExpression, Type: System.Int32, Constant: 1) (Syntax: '1')
  IsType: color
";
            var expectedDiagnostics = new DiagnosticDescription[] {
                // CS0184: The given expression is never of the provided ('color') type
                //         var b = /*<bind>*/1 is color/*</bind>*/;
                Diagnostic(ErrorCode.WRN_IsAlwaysFalse, "1 is color").WithArguments("color").WithLocation(6, 27)
            };

            VerifyOperationTreeAndDiagnosticsForTest<BinaryExpressionSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation)]
        [Fact]
        public void TestIsOperatorGeneric_TypeParameterExpressionIntType()
        {
            string source = @"
namespace TestIsOperatorGeneric
{
    class C
    {
        public static void M<T, U, W>(T t, U u)
            where T : class
            where U : class
        {
            bool test = /*<bind>*/t is int/*</bind>*/;
        }
    }
}
";
            string expectedOperationTree = @"
IIsTypeOperation (OperationKind.IsType, IsExpression, Type: System.Boolean) (Syntax: 't is int')
  Operand: 
    IParameterReferenceOperation: t (OperationKind.ParameterReference, IsExpression, Type: T) (Syntax: 't')
  IsType: System.Int32
";
            var expectedDiagnostics = DiagnosticDescription.None;

            VerifyOperationTreeAndDiagnosticsForTest<BinaryExpressionSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation)]
        [Fact]
        public void TestIsOperatorGeneric_TypeParameterExpressionObjectType()
        {
            string source = @"
namespace TestIsOperatorGeneric
{
    class C
    {
        public static void M<T, U, W>(T t, U u)
            where T : class
            where U : class
        {
            bool test = /*<bind>*/u is object/*</bind>*/;
        }
    }
}
";
            string expectedOperationTree = @"
IIsTypeOperation (OperationKind.IsType, IsExpression, Type: System.Boolean) (Syntax: 'u is object')
  Operand: 
    IParameterReferenceOperation: u (OperationKind.ParameterReference, IsExpression, Type: U) (Syntax: 'u')
  IsType: System.Object
";
            var expectedDiagnostics = DiagnosticDescription.None;

            VerifyOperationTreeAndDiagnosticsForTest<BinaryExpressionSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation)]
        [Fact]
        public void TestIsOperatorGeneric_TypeParameterExpressionDifferentTypeParameterType()
        {
            string source = @"
namespace TestIsOperatorGeneric
{
    class C
    {
        public static void M<T, U, W>(T t, U u)
            where T : class
            where U : class
        {
            bool test = /*<bind>*/t is U/*</bind>*/;
        }
    }
}
";
            string expectedOperationTree = @"
IIsTypeOperation (OperationKind.IsType, IsExpression, Type: System.Boolean) (Syntax: 't is U')
  Operand: 
    IParameterReferenceOperation: t (OperationKind.ParameterReference, IsExpression, Type: T) (Syntax: 't')
  IsType: U
";
            var expectedDiagnostics = DiagnosticDescription.None;

            VerifyOperationTreeAndDiagnosticsForTest<BinaryExpressionSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation)]
        [Fact]
        public void TestIsOperatorGeneric_TypeParameterExpressionSameTypeParameterType()
        {
            string source = @"
namespace TestIsOperatorGeneric
{
    class C
    {
        public static void M<T, U, W>(T t, U u)
            where T : class
            where U : class
        {
            bool test = /*<bind>*/t is T/*</bind>*/;
        }
    }
}
";
            string expectedOperationTree = @"
IIsTypeOperation (OperationKind.IsType, IsExpression, Type: System.Boolean) (Syntax: 't is T')
  Operand: 
    IParameterReferenceOperation: t (OperationKind.ParameterReference, IsExpression, Type: T) (Syntax: 't')
  IsType: T
";
            var expectedDiagnostics = DiagnosticDescription.None;

            VerifyOperationTreeAndDiagnosticsForTest<BinaryExpressionSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }
    }
}
