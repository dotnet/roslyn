// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.CSharp.UnitTests
{
    public class IOperationTests_IIsTypeExpression : SemanticModelTestBase
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
IIsTypeOperation (OperationKind.IsType, Type: System.Boolean) (Syntax: 'o is string')
  Operand: 
    ILocalReferenceOperation: o (OperationKind.LocalReference, Type: System.Object) (Syntax: 'o')
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
IIsTypeOperation (OperationKind.IsType, Type: System.Boolean) (Syntax: 'myInt is int')
  Operand: 
    ILocalReferenceOperation: myInt (OperationKind.LocalReference, Type: System.Int32) (Syntax: 'myInt')
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
IIsTypeOperation (OperationKind.IsType, Type: System.Boolean) (Syntax: 'o is TestType')
  Operand: 
    ILocalReferenceOperation: o (OperationKind.LocalReference, Type: System.Object) (Syntax: 'o')
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
IIsTypeOperation (OperationKind.IsType, Type: System.Boolean) (Syntax: 'null is TestType')
  Operand: 
    ILiteralOperation (OperationKind.Literal, Type: null, Constant: null) (Syntax: 'null')
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
enum @color
{ }
";
            string expectedOperationTree = @"
IIsTypeOperation (OperationKind.IsType, Type: System.Boolean) (Syntax: '1 is color')
  Operand: 
    ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 1) (Syntax: '1')
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
IIsTypeOperation (OperationKind.IsType, Type: System.Boolean) (Syntax: 't is int')
  Operand: 
    IParameterReferenceOperation: t (OperationKind.ParameterReference, Type: T) (Syntax: 't')
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
IIsTypeOperation (OperationKind.IsType, Type: System.Boolean) (Syntax: 'u is object')
  Operand: 
    IParameterReferenceOperation: u (OperationKind.ParameterReference, Type: U) (Syntax: 'u')
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
IIsTypeOperation (OperationKind.IsType, Type: System.Boolean) (Syntax: 't is U')
  Operand: 
    IParameterReferenceOperation: t (OperationKind.ParameterReference, Type: T) (Syntax: 't')
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
IIsTypeOperation (OperationKind.IsType, Type: System.Boolean) (Syntax: 't is T')
  Operand: 
    IParameterReferenceOperation: t (OperationKind.ParameterReference, Type: T) (Syntax: 't')
  IsType: T
";
            var expectedDiagnostics = DiagnosticDescription.None;

            VerifyOperationTreeAndDiagnosticsForTest<BinaryExpressionSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation, CompilerFeature.Dataflow)]
        [Fact]
        public void IsTypeFlow_01()
        {
            string source = @"
class C
{
    public static void M2(C1 c, bool b)
    /*<bind>*/{
        b = c is C1;
    }/*</bind>*/
    public class C1 { }
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
        IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'b = c is C1;')
          Expression: 
            ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Boolean) (Syntax: 'b = c is C1')
              Left: 
                IParameterReferenceOperation: b (OperationKind.ParameterReference, Type: System.Boolean) (Syntax: 'b')
              Right: 
                IIsTypeOperation (OperationKind.IsType, Type: System.Boolean) (Syntax: 'c is C1')
                  Operand: 
                    IParameterReferenceOperation: c (OperationKind.ParameterReference, Type: C.C1) (Syntax: 'c')
                  IsType: C.C1

    Next (Regular) Block[B2]
Block[B2] - Exit
    Predecessors: [B1]
    Statements (0)
";
            VerifyFlowGraphAndDiagnosticsForTest<BlockSyntax>(source, expectedFlowGraph, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation, CompilerFeature.Dataflow)]
        [Fact]
        public void IsTypeFlow_02()
        {
            string source = @"
class C
{
    public static void M2(C1 c1, C1 c2, bool b)
    /*<bind>*/{
        b = (c1 ?? c2) is C1;
    }/*</bind>*/
    public class C1 { }
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
    CaptureIds: [0] [2]
    Block[B1] - Block
        Predecessors: [B0]
        Statements (1)
            IFlowCaptureOperation: 0 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'b')
              Value: 
                IParameterReferenceOperation: b (OperationKind.ParameterReference, Type: System.Boolean) (Syntax: 'b')

        Next (Regular) Block[B2]
            Entering: {R2}

    .locals {R2}
    {
        CaptureIds: [1]
        Block[B2] - Block
            Predecessors: [B1]
            Statements (1)
                IFlowCaptureOperation: 1 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'c1')
                  Value: 
                    IParameterReferenceOperation: c1 (OperationKind.ParameterReference, Type: C.C1) (Syntax: 'c1')

            Jump if True (Regular) to Block[B4]
                IIsNullOperation (OperationKind.IsNull, Type: System.Boolean, IsImplicit) (Syntax: 'c1')
                  Operand: 
                    IFlowCaptureReferenceOperation: 1 (OperationKind.FlowCaptureReference, Type: C.C1, IsImplicit) (Syntax: 'c1')
                Leaving: {R2}

            Next (Regular) Block[B3]
        Block[B3] - Block
            Predecessors: [B2]
            Statements (1)
                IFlowCaptureOperation: 2 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'c1')
                  Value: 
                    IFlowCaptureReferenceOperation: 1 (OperationKind.FlowCaptureReference, Type: C.C1, IsImplicit) (Syntax: 'c1')

            Next (Regular) Block[B5]
                Leaving: {R2}
    }

    Block[B4] - Block
        Predecessors: [B2]
        Statements (1)
            IFlowCaptureOperation: 2 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'c2')
              Value: 
                IParameterReferenceOperation: c2 (OperationKind.ParameterReference, Type: C.C1) (Syntax: 'c2')

        Next (Regular) Block[B5]
    Block[B5] - Block
        Predecessors: [B3] [B4]
        Statements (1)
            IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'b = (c1 ?? c2) is C1;')
              Expression: 
                ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Boolean) (Syntax: 'b = (c1 ?? c2) is C1')
                  Left: 
                    IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: System.Boolean, IsImplicit) (Syntax: 'b')
                  Right: 
                    IIsTypeOperation (OperationKind.IsType, Type: System.Boolean) (Syntax: '(c1 ?? c2) is C1')
                      Operand: 
                        IFlowCaptureReferenceOperation: 2 (OperationKind.FlowCaptureReference, Type: C.C1, IsImplicit) (Syntax: 'c1 ?? c2')
                      IsType: C.C1

        Next (Regular) Block[B6]
            Leaving: {R1}
}

Block[B6] - Exit
    Predecessors: [B5]
    Statements (0)
";
            VerifyFlowGraphAndDiagnosticsForTest<BlockSyntax>(source, expectedFlowGraph, expectedDiagnostics);
        }
    }
}
