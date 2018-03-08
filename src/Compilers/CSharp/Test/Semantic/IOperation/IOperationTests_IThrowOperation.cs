// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.CSharp.Test.Utilities;
using Microsoft.CodeAnalysis.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.CSharp.UnitTests
{
    public partial class IOperationTests : SemanticModelTestBase
    {

        [CompilerTrait(CompilerFeature.IOperation, CompilerFeature.Dataflow)]
        [Fact]
        public void ThrowFlow_01()
        {
            var source = @"
class C
{
    void F()
    /*<bind>*/{
        throw;
    }/*</bind>*/
}";

            var compilation = CreateCompilation(source, parseOptions: TestOptions.RegularWithFlowAnalysisFeature);

            compilation.VerifyDiagnostics(
                // (6,9): error CS0156: A throw statement with no arguments is not allowed outside of a catch clause
                //         throw;
                Diagnostic(ErrorCode.ERR_BadEmptyThrow, "throw").WithLocation(6, 9)
                );

            string expectedGraph = @"
Block[B0] - Entry
    Statements (0)
    Next (Regular) Block[B1]
Block[B1] - Block
    Predecessors: [B0]
    Statements (0)
    Next (ReThrow) Block[null]
Block[B2] - Exit
    Predecessors (0)
    Statements (0)
";
            VerifyFlowGraphForTest<BlockSyntax>(compilation, expectedGraph);
        }

        [CompilerTrait(CompilerFeature.IOperation, CompilerFeature.Dataflow)]
        [Fact]
        public void ThrowFlow_02()
        {
            var source = @"
class C
{
    void F(int x)
    /*<bind>*/{
        x = 1;
        throw;
        x = 2;
    }/*</bind>*/
}";

            var compilation = CreateCompilation(source, parseOptions: TestOptions.RegularWithFlowAnalysisFeature);

            compilation.VerifyDiagnostics(
                // (7,9): error CS0156: A throw statement with no arguments is not allowed outside of a catch clause
                //         throw;
                Diagnostic(ErrorCode.ERR_BadEmptyThrow, "throw").WithLocation(7, 9),
                // (8,9): warning CS0162: Unreachable code detected
                //         x = 2;
                Diagnostic(ErrorCode.WRN_UnreachableCode, "x").WithLocation(8, 9)
                );

            string expectedGraph = @"
Block[B0] - Entry
    Statements (0)
    Next (Regular) Block[B1]
Block[B1] - Block
    Predecessors: [B0]
    Statements (1)
        IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'x = 1;')
          Expression: 
            ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Int32) (Syntax: 'x = 1')
              Left: 
                IParameterReferenceOperation: x (OperationKind.ParameterReference, Type: System.Int32) (Syntax: 'x')
              Right: 
                ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 1) (Syntax: '1')

    Next (ReThrow) Block[null]
Block[B2] - Block
    Predecessors (0)
    Statements (1)
        IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'x = 2;')
          Expression: 
            ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Int32) (Syntax: 'x = 2')
              Left: 
                IParameterReferenceOperation: x (OperationKind.ParameterReference, Type: System.Int32) (Syntax: 'x')
              Right: 
                ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 2) (Syntax: '2')

    Next (Regular) Block[B3]
Block[B3] - Exit
    Predecessors: [B2]
    Statements (0)
";
            VerifyFlowGraphForTest<BlockSyntax>(compilation, expectedGraph);
        }

        [CompilerTrait(CompilerFeature.IOperation, CompilerFeature.Dataflow)]
        [Fact]
        public void ThrowFlow_03()
        {
            var source = @"
class C
{
    void F(System.Exception ex)
    /*<bind>*/{
        throw ex;
    }/*</bind>*/
}";

            var compilation = CreateCompilation(source, parseOptions: TestOptions.RegularWithFlowAnalysisFeature);

            compilation.VerifyDiagnostics();

            string expectedGraph = @"
Block[B0] - Entry
    Statements (0)
    Next (Regular) Block[B1]
Block[B1] - Block
    Predecessors: [B0]
    Statements (0)
    Next (Throw) Block[null]
        IParameterReferenceOperation: ex (OperationKind.ParameterReference, Type: System.Exception) (Syntax: 'ex')
Block[B2] - Exit
    Predecessors (0)
    Statements (0)
";
            VerifyFlowGraphForTest<BlockSyntax>(compilation, expectedGraph);
        }

        [CompilerTrait(CompilerFeature.IOperation, CompilerFeature.Dataflow)]
        [Fact]
        public void ThrowFlow_04()
        {
            var source = @"
class C
{
    void F(System.Exception ex)
    /*<bind>*/{
        int x = 1;
        throw ex;
        x = 2;
    }/*</bind>*/
}";

            var compilation = CreateCompilation(source, parseOptions: TestOptions.RegularWithFlowAnalysisFeature);

            compilation.VerifyDiagnostics(
                // (8,9): warning CS0162: Unreachable code detected
                //         x = 2;
                Diagnostic(ErrorCode.WRN_UnreachableCode, "x").WithLocation(8, 9),
                // (6,13): warning CS0219: The variable 'x' is assigned but its value is never used
                //         int x = 1;
                Diagnostic(ErrorCode.WRN_UnreferencedVarAssg, "x").WithArguments("x").WithLocation(6, 13)
                );

            string expectedGraph = @"
Block[B0] - Entry
    Statements (0)
    Next (Regular) Block[B1]
        Entering: {R1}

.locals {R1}
{
    Locals: [System.Int32 x]
    Block[B1] - Block
        Predecessors: [B0]
        Statements (1)
            ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Int32, IsImplicit) (Syntax: 'x = 1')
              Left: 
                ILocalReferenceOperation: x (IsDeclaration: True) (OperationKind.LocalReference, Type: System.Int32, IsImplicit) (Syntax: 'x = 1')
              Right: 
                ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 1) (Syntax: '1')

        Next (Throw) Block[null]
            IParameterReferenceOperation: ex (OperationKind.ParameterReference, Type: System.Exception) (Syntax: 'ex')
    Block[B2] - Block
        Predecessors (0)
        Statements (1)
            IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'x = 2;')
              Expression: 
                ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Int32) (Syntax: 'x = 2')
                  Left: 
                    ILocalReferenceOperation: x (OperationKind.LocalReference, Type: System.Int32) (Syntax: 'x')
                  Right: 
                    ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 2) (Syntax: '2')

        Next (Regular) Block[B3]
            Leaving: {R1}
}

Block[B3] - Exit
    Predecessors: [B2]
    Statements (0)
";
            VerifyFlowGraphForTest<BlockSyntax>(compilation, expectedGraph);
        }

        [CompilerTrait(CompilerFeature.IOperation, CompilerFeature.Dataflow)]
        [Fact]
        public void ThrowFlow_05()
        {
            var source = @"
class C
{
    void F(int x, System.Exception ex)
    /*<bind>*/{
        x = throw ex + x;
    }/*</bind>*/
}";

            var compilation = CreateCompilation(source, parseOptions: TestOptions.RegularWithFlowAnalysisFeature);

            compilation.VerifyDiagnostics(
                // (6,13): error CS8115: A throw expression is not allowed in this context.
                //         x = throw ex + x;
                Diagnostic(ErrorCode.ERR_ThrowMisplaced, "throw").WithLocation(6, 13),
                // (6,19): error CS0019: Operator '+' cannot be applied to operands of type 'Exception' and 'int'
                //         x = throw ex + x;
                Diagnostic(ErrorCode.ERR_BadBinaryOps, "ex + x").WithArguments("+", "System.Exception", "int").WithLocation(6, 19)
                );

            string expectedGraph = @"
Block[B0] - Entry
    Statements (0)
    Next (Regular) Block[B1]
Block[B1] - Block
    Predecessors: [B0]
    Statements (1)
        IFlowCaptureOperation: 0 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'x')
          Value: 
            IParameterReferenceOperation: x (OperationKind.ParameterReference, Type: System.Int32) (Syntax: 'x')

    Next (Throw) Block[null]
        IBinaryOperation (BinaryOperatorKind.Add) (OperationKind.BinaryOperator, Type: ?, IsInvalid) (Syntax: 'ex + x')
          Left: 
            IParameterReferenceOperation: ex (OperationKind.ParameterReference, Type: System.Exception, IsInvalid) (Syntax: 'ex')
          Right: 
            IParameterReferenceOperation: x (OperationKind.ParameterReference, Type: System.Int32, IsInvalid) (Syntax: 'x')
Block[B2] - Block
    Predecessors (0)
    Statements (1)
        IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null, IsInvalid) (Syntax: 'x = throw ex + x;')
          Expression: 
            ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Int32, IsInvalid) (Syntax: 'x = throw ex + x')
              Left: 
                IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: System.Int32, IsImplicit) (Syntax: 'x')
              Right: 
                IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.Int32, IsInvalid, IsImplicit) (Syntax: 'throw ex + x')
                  Conversion: CommonConversion (Exists: False, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                    (NoConversion)
                  Operand: 
                    IInvalidOperation (OperationKind.Invalid, Type: ?, IsInvalid, IsImplicit) (Syntax: 'throw ex + x')
                      Children(1):
                          IOperation:  (OperationKind.None, Type: null, IsInvalid, IsImplicit) (Syntax: 'throw ex + x')

    Next (Regular) Block[B3]
Block[B3] - Exit
    Predecessors: [B2]
    Statements (0)
";
            VerifyFlowGraphForTest<BlockSyntax>(compilation, expectedGraph);
        }

        [CompilerTrait(CompilerFeature.IOperation, CompilerFeature.Dataflow)]
        [Fact]
        public void ThrowFlow_06()
        {
            var source = @"
class C
{
    void F(int x, System.Exception ex)
    /*<bind>*/{
        x = (throw ex) + x;
    }/*</bind>*/
}";

            var compilation = CreateCompilation(source, parseOptions: TestOptions.RegularWithFlowAnalysisFeature);

            compilation.VerifyDiagnostics(
                // (6,14): error CS8115: A throw expression is not allowed in this context.
                //         x = (throw ex) + x;
                Diagnostic(ErrorCode.ERR_ThrowMisplaced, "throw").WithLocation(6, 14)
                );

            string expectedGraph = @"
Block[B0] - Entry
    Statements (0)
    Next (Regular) Block[B1]
Block[B1] - Block
    Predecessors: [B0]
    Statements (1)
        IFlowCaptureOperation: 0 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'x')
          Value: 
            IParameterReferenceOperation: x (OperationKind.ParameterReference, Type: System.Int32) (Syntax: 'x')

    Next (Throw) Block[null]
        IParameterReferenceOperation: ex (OperationKind.ParameterReference, Type: System.Exception) (Syntax: 'ex')
Block[B2] - Block
    Predecessors (0)
    Statements (1)
        IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null, IsInvalid) (Syntax: 'x = (throw ex) + x;')
          Expression: 
            ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Int32, IsInvalid) (Syntax: 'x = (throw ex) + x')
              Left: 
                IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: System.Int32, IsImplicit) (Syntax: 'x')
              Right: 
                IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.Int32, IsInvalid, IsImplicit) (Syntax: '(throw ex) + x')
                  Conversion: CommonConversion (Exists: False, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                    (NoConversion)
                  Operand: 
                    IBinaryOperation (BinaryOperatorKind.Add) (OperationKind.BinaryOperator, Type: ?, IsInvalid) (Syntax: '(throw ex) + x')
                      Left: 
                        IInvalidOperation (OperationKind.Invalid, Type: ?, IsInvalid, IsImplicit) (Syntax: 'throw ex')
                          Children(1):
                              IOperation:  (OperationKind.None, Type: null, IsInvalid, IsImplicit) (Syntax: 'throw ex')
                      Right: 
                        IParameterReferenceOperation: x (OperationKind.ParameterReference, Type: System.Int32) (Syntax: 'x')

    Next (Regular) Block[B3]
Block[B3] - Exit
    Predecessors: [B2]
    Statements (0)
";
            VerifyFlowGraphForTest<BlockSyntax>(compilation, expectedGraph);
        }

        [CompilerTrait(CompilerFeature.IOperation, CompilerFeature.Dataflow)]
        [Fact]
        public void ThrowFlow_07()
        {
            var source = @"
class C
{
    void F(int x, System.Exception ex)
    /*<bind>*/{
        x = x + throw ex;
    }/*</bind>*/
}";

            var compilation = CreateCompilation(source, parseOptions: TestOptions.RegularWithFlowAnalysisFeature);

            compilation.VerifyDiagnostics(
                // (6,17): error CS1525: Invalid expression term 'throw'
                //         x = x + throw ex;
                Diagnostic(ErrorCode.ERR_InvalidExprTerm, "throw ex").WithArguments("throw").WithLocation(6, 17)
                );

            string expectedGraph = @"
Block[B0] - Entry
    Statements (0)
    Next (Regular) Block[B1]
Block[B1] - Block
    Predecessors: [B0]
    Statements (2)
        IFlowCaptureOperation: 0 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'x')
          Value: 
            IParameterReferenceOperation: x (OperationKind.ParameterReference, Type: System.Int32) (Syntax: 'x')

        IFlowCaptureOperation: 1 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'x')
          Value: 
            IParameterReferenceOperation: x (OperationKind.ParameterReference, Type: System.Int32) (Syntax: 'x')

    Next (Throw) Block[null]
        IParameterReferenceOperation: ex (OperationKind.ParameterReference, Type: System.Exception, IsInvalid) (Syntax: 'ex')
Block[B2] - Block
    Predecessors (0)
    Statements (1)
        IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null, IsInvalid) (Syntax: 'x = x + throw ex;')
          Expression: 
            ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Int32, IsInvalid) (Syntax: 'x = x + throw ex')
              Left: 
                IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: System.Int32, IsImplicit) (Syntax: 'x')
              Right: 
                IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.Int32, IsInvalid, IsImplicit) (Syntax: 'x + throw ex')
                  Conversion: CommonConversion (Exists: False, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                    (NoConversion)
                  Operand: 
                    IBinaryOperation (BinaryOperatorKind.Add) (OperationKind.BinaryOperator, Type: ?, IsInvalid) (Syntax: 'x + throw ex')
                      Left: 
                        IFlowCaptureReferenceOperation: 1 (OperationKind.FlowCaptureReference, Type: System.Int32, IsImplicit) (Syntax: 'x')
                      Right: 
                        IInvalidOperation (OperationKind.Invalid, Type: ?, IsInvalid, IsImplicit) (Syntax: 'throw ex')
                          Children(1):
                              IOperation:  (OperationKind.None, Type: null, IsInvalid, IsImplicit) (Syntax: 'throw ex')

    Next (Regular) Block[B3]
Block[B3] - Exit
    Predecessors: [B2]
    Statements (0)
";
            VerifyFlowGraphForTest<BlockSyntax>(compilation, expectedGraph);
        }

        [CompilerTrait(CompilerFeature.IOperation, CompilerFeature.Dataflow)]
        [Fact]
        public void ThrowFlow_08()
        {
            var source = @"
class C
{
    void F(int x, System.Exception ex)
    /*<bind>*/{
        x = x + (throw ex);
    }/*</bind>*/
}";

            var compilation = CreateCompilation(source, parseOptions: TestOptions.RegularWithFlowAnalysisFeature);

            compilation.VerifyDiagnostics(
                // (6,18): error CS8115: A throw expression is not allowed in this context.
                //         x = x + (throw ex);
                Diagnostic(ErrorCode.ERR_ThrowMisplaced, "throw").WithLocation(6, 18)
                );

            string expectedGraph = @"
Block[B0] - Entry
    Statements (0)
    Next (Regular) Block[B1]
Block[B1] - Block
    Predecessors: [B0]
    Statements (2)
        IFlowCaptureOperation: 0 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'x')
          Value: 
            IParameterReferenceOperation: x (OperationKind.ParameterReference, Type: System.Int32) (Syntax: 'x')

        IFlowCaptureOperation: 1 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'x')
          Value: 
            IParameterReferenceOperation: x (OperationKind.ParameterReference, Type: System.Int32) (Syntax: 'x')

    Next (Throw) Block[null]
        IParameterReferenceOperation: ex (OperationKind.ParameterReference, Type: System.Exception) (Syntax: 'ex')
Block[B2] - Block
    Predecessors (0)
    Statements (1)
        IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null, IsInvalid) (Syntax: 'x = x + (throw ex);')
          Expression: 
            ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Int32, IsInvalid) (Syntax: 'x = x + (throw ex)')
              Left: 
                IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: System.Int32, IsImplicit) (Syntax: 'x')
              Right: 
                IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.Int32, IsInvalid, IsImplicit) (Syntax: 'x + (throw ex)')
                  Conversion: CommonConversion (Exists: False, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                    (NoConversion)
                  Operand: 
                    IBinaryOperation (BinaryOperatorKind.Add) (OperationKind.BinaryOperator, Type: ?, IsInvalid) (Syntax: 'x + (throw ex)')
                      Left: 
                        IFlowCaptureReferenceOperation: 1 (OperationKind.FlowCaptureReference, Type: System.Int32, IsImplicit) (Syntax: 'x')
                      Right: 
                        IInvalidOperation (OperationKind.Invalid, Type: ?, IsInvalid, IsImplicit) (Syntax: 'throw ex')
                          Children(1):
                              IOperation:  (OperationKind.None, Type: null, IsInvalid, IsImplicit) (Syntax: 'throw ex')

    Next (Regular) Block[B3]
Block[B3] - Exit
    Predecessors: [B2]
    Statements (0)
";
            VerifyFlowGraphForTest<BlockSyntax>(compilation, expectedGraph);
        }

        [CompilerTrait(CompilerFeature.IOperation, CompilerFeature.Dataflow)]
        [Fact]
        public void ThrowFlow_09()
        {
            var source = @"
class C
{
    void F(object x, object y, System.Exception ex)
    /*<bind>*/{
        x = y ?? throw ex;
    }/*</bind>*/
}";

            var compilation = CreateCompilation(source, parseOptions: TestOptions.RegularWithFlowAnalysisFeature);

            compilation.VerifyDiagnostics();

            string expectedGraph = @"
Block[B0] - Entry
    Statements (0)
    Next (Regular) Block[B1]
Block[B1] - Block
    Predecessors: [B0]
    Statements (2)
        IFlowCaptureOperation: 0 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'x')
          Value: 
            IParameterReferenceOperation: x (OperationKind.ParameterReference, Type: System.Object) (Syntax: 'x')

        IFlowCaptureOperation: 1 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'y')
          Value: 
            IParameterReferenceOperation: y (OperationKind.ParameterReference, Type: System.Object) (Syntax: 'y')

    Jump if True (Regular) to Block[B3]
        IIsNullOperation (OperationKind.IsNull, Type: System.Boolean, IsImplicit) (Syntax: 'y')
          Operand: 
            IFlowCaptureReferenceOperation: 1 (OperationKind.FlowCaptureReference, Type: System.Object, IsImplicit) (Syntax: 'y')

    Next (Regular) Block[B2]
Block[B2] - Block
    Predecessors: [B1]
    Statements (1)
        IFlowCaptureOperation: 2 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'y')
          Value: 
            IFlowCaptureReferenceOperation: 1 (OperationKind.FlowCaptureReference, Type: System.Object, IsImplicit) (Syntax: 'y')

    Next (Regular) Block[B5]
Block[B3] - Block
    Predecessors: [B1]
    Statements (0)
    Next (Throw) Block[null]
        IParameterReferenceOperation: ex (OperationKind.ParameterReference, Type: System.Exception) (Syntax: 'ex')
Block[B4] - Block
    Predecessors (0)
    Statements (1)
        IFlowCaptureOperation: 2 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'throw ex')
          Value: 
            IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.Object, IsImplicit) (Syntax: 'throw ex')
              Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                (ImplicitThrow)
              Operand: 
                IOperation:  (OperationKind.None, Type: null, IsImplicit) (Syntax: 'throw ex')

    Next (Regular) Block[B5]
Block[B5] - Block
    Predecessors: [B2] [B4]
    Statements (1)
        IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'x = y ?? throw ex;')
          Expression: 
            ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Object) (Syntax: 'x = y ?? throw ex')
              Left: 
                IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: System.Object, IsImplicit) (Syntax: 'x')
              Right: 
                IFlowCaptureReferenceOperation: 2 (OperationKind.FlowCaptureReference, Type: System.Object, IsImplicit) (Syntax: 'y ?? throw ex')

    Next (Regular) Block[B6]
Block[B6] - Exit
    Predecessors: [B5]
    Statements (0)
";
            VerifyFlowGraphForTest<BlockSyntax>(compilation, expectedGraph);
        }

        [CompilerTrait(CompilerFeature.IOperation, CompilerFeature.Dataflow)]
        [Fact]
        public void ThrowFlow_10()
        {
            var source = @"
class C
{
    void F(object x, object y, object z, System.Exception ex)
    /*<bind>*/{
        M(x, y ?? throw ex, z);
    }/*</bind>*/

    static void M(object x, object y, object z){}
}";

            var compilation = CreateCompilation(source, parseOptions: TestOptions.RegularWithFlowAnalysisFeature);

            compilation.VerifyDiagnostics();

            string expectedGraph = @"
Block[B0] - Entry
    Statements (0)
    Next (Regular) Block[B1]
Block[B1] - Block
    Predecessors: [B0]
    Statements (2)
        IFlowCaptureOperation: 0 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'x')
          Value: 
            IParameterReferenceOperation: x (OperationKind.ParameterReference, Type: System.Object) (Syntax: 'x')

        IFlowCaptureOperation: 1 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'y')
          Value: 
            IParameterReferenceOperation: y (OperationKind.ParameterReference, Type: System.Object) (Syntax: 'y')

    Jump if True (Regular) to Block[B3]
        IIsNullOperation (OperationKind.IsNull, Type: System.Boolean, IsImplicit) (Syntax: 'y')
          Operand: 
            IFlowCaptureReferenceOperation: 1 (OperationKind.FlowCaptureReference, Type: System.Object, IsImplicit) (Syntax: 'y')

    Next (Regular) Block[B2]
Block[B2] - Block
    Predecessors: [B1]
    Statements (1)
        IFlowCaptureOperation: 2 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'y')
          Value: 
            IFlowCaptureReferenceOperation: 1 (OperationKind.FlowCaptureReference, Type: System.Object, IsImplicit) (Syntax: 'y')

    Next (Regular) Block[B5]
Block[B3] - Block
    Predecessors: [B1]
    Statements (0)
    Next (Throw) Block[null]
        IParameterReferenceOperation: ex (OperationKind.ParameterReference, Type: System.Exception) (Syntax: 'ex')
Block[B4] - Block
    Predecessors (0)
    Statements (1)
        IFlowCaptureOperation: 2 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'throw ex')
          Value: 
            IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.Object, IsImplicit) (Syntax: 'throw ex')
              Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                (ImplicitThrow)
              Operand: 
                IOperation:  (OperationKind.None, Type: null, IsImplicit) (Syntax: 'throw ex')

    Next (Regular) Block[B5]
Block[B5] - Block
    Predecessors: [B2] [B4]
    Statements (1)
        IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'M(x, y ?? throw ex, z);')
          Expression: 
            IInvocationOperation (void C.M(System.Object x, System.Object y, System.Object z)) (OperationKind.Invocation, Type: System.Void) (Syntax: 'M(x, y ?? throw ex, z)')
              Instance Receiver: 
                null
              Arguments(3):
                  IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: x) (OperationKind.Argument, Type: null) (Syntax: 'x')
                    IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: System.Object, IsImplicit) (Syntax: 'x')
                    InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                    OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                  IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: y) (OperationKind.Argument, Type: null) (Syntax: 'y ?? throw ex')
                    IFlowCaptureReferenceOperation: 2 (OperationKind.FlowCaptureReference, Type: System.Object, IsImplicit) (Syntax: 'y ?? throw ex')
                    InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                    OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                  IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: z) (OperationKind.Argument, Type: null) (Syntax: 'z')
                    IParameterReferenceOperation: z (OperationKind.ParameterReference, Type: System.Object) (Syntax: 'z')
                    InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                    OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)

    Next (Regular) Block[B6]
Block[B6] - Exit
    Predecessors: [B5]
    Statements (0)
";
            VerifyFlowGraphForTest<BlockSyntax>(compilation, expectedGraph);
        }

        [CompilerTrait(CompilerFeature.IOperation, CompilerFeature.Dataflow)]
        [Fact]
        public void ThrowFlow_11()
        {
            var source = @"
class C
{
    void F(int u)
    /*<bind>*/{
        try
        {
            u = 1;
        }
        catch
        {
            throw;
        }
    }/*</bind>*/

    static void M(object x, object y, object z){}
}";

            var compilation = CreateCompilation(source, parseOptions: TestOptions.RegularWithFlowAnalysisFeature);

            compilation.VerifyDiagnostics();

            string expectedGraph = @"
Block[B0] - Entry
    Statements (0)
    Next (Regular) Block[B1]
        Entering: {R1} {R2}

.try {R1, R2}
{
    Block[B1] - Block
        Predecessors: [B0]
        Statements (1)
            IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'u = 1;')
              Expression: 
                ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Int32) (Syntax: 'u = 1')
                  Left: 
                    IParameterReferenceOperation: u (OperationKind.ParameterReference, Type: System.Int32) (Syntax: 'u')
                  Right: 
                    ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 1) (Syntax: '1')

        Next (Regular) Block[B3]
            Leaving: {R2} {R1}
}
.catch {R3} (System.Object)
{
    Block[B2] - Block
        Predecessors (0)
        Statements (0)
        Next (ReThrow) Block[null]
}

Block[B3] - Exit
    Predecessors: [B1]
    Statements (0)
";
            VerifyFlowGraphForTest<BlockSyntax>(compilation, expectedGraph);
        }

        [CompilerTrait(CompilerFeature.IOperation, CompilerFeature.Dataflow)]
        [Fact]
        public void ThrowFlow_12()
        {
            var source = @"
class C
{
    void F(int u)
    /*<bind>*/{
        try
        {
            u = 1;
        }
        catch
        {
            u = 2;
            throw;
            u = 3;
        }
    }/*</bind>*/

    static void M(object x, object y, object z){}
}";

            var compilation = CreateCompilation(source, parseOptions: TestOptions.RegularWithFlowAnalysisFeature);

            compilation.VerifyDiagnostics(
                // (14,13): warning CS0162: Unreachable code detected
                //             u = 3;
                Diagnostic(ErrorCode.WRN_UnreachableCode, "u").WithLocation(14, 13)
                );

            string expectedGraph = @"
Block[B0] - Entry
    Statements (0)
    Next (Regular) Block[B1]
        Entering: {R1} {R2}

.try {R1, R2}
{
    Block[B1] - Block
        Predecessors: [B0]
        Statements (1)
            IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'u = 1;')
              Expression: 
                ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Int32) (Syntax: 'u = 1')
                  Left: 
                    IParameterReferenceOperation: u (OperationKind.ParameterReference, Type: System.Int32) (Syntax: 'u')
                  Right: 
                    ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 1) (Syntax: '1')

        Next (Regular) Block[B4]
            Leaving: {R2} {R1}
}
.catch {R3} (System.Object)
{
    Block[B2] - Block
        Predecessors (0)
        Statements (1)
            IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'u = 2;')
              Expression: 
                ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Int32) (Syntax: 'u = 2')
                  Left: 
                    IParameterReferenceOperation: u (OperationKind.ParameterReference, Type: System.Int32) (Syntax: 'u')
                  Right: 
                    ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 2) (Syntax: '2')

        Next (ReThrow) Block[null]
    Block[B3] - Block
        Predecessors (0)
        Statements (1)
            IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'u = 3;')
              Expression: 
                ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Int32) (Syntax: 'u = 3')
                  Left: 
                    IParameterReferenceOperation: u (OperationKind.ParameterReference, Type: System.Int32) (Syntax: 'u')
                  Right: 
                    ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 3) (Syntax: '3')

        Next (Regular) Block[B4]
            Leaving: {R3} {R1}
}

Block[B4] - Exit
    Predecessors: [B1] [B3]
    Statements (0)
";
            VerifyFlowGraphForTest<BlockSyntax>(compilation, expectedGraph);
        }

        [CompilerTrait(CompilerFeature.IOperation, CompilerFeature.Dataflow)]
        [Fact]
        public void ThrowFlow_13()
        {
            var source = @"
class C
{
    void F(object x, object y, object z, int u)
    /*<bind>*/{
        try
        {
            u = 1;
        }
        catch
        {
            M(x, (y ?? throw), z);
        }
    }/*</bind>*/

    static void M(object x, object y, object z){}
}
";

            var compilation = CreateCompilation(source, parseOptions: TestOptions.RegularWithFlowAnalysisFeature);

            compilation.VerifyDiagnostics(
                // (12,29): error CS1525: Invalid expression term ')'
                //             M(x, (y ?? throw), z);
                Diagnostic(ErrorCode.ERR_InvalidExprTerm, ")").WithArguments(")").WithLocation(12, 29)
                );

            string expectedGraph = @"
Block[B0] - Entry
    Statements (0)
    Next (Regular) Block[B1]
        Entering: {R1} {R2}

.try {R1, R2}
{
    Block[B1] - Block
        Predecessors: [B0]
        Statements (1)
            IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'u = 1;')
              Expression: 
                ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Int32) (Syntax: 'u = 1')
                  Left: 
                    IParameterReferenceOperation: u (OperationKind.ParameterReference, Type: System.Int32) (Syntax: 'u')
                  Right: 
                    ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 1) (Syntax: '1')

        Next (Regular) Block[B7]
            Leaving: {R2} {R1}
}
.catch {R3} (System.Object)
{
    Block[B2] - Block
        Predecessors (0)
        Statements (1)
            IFlowCaptureOperation: 0 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'y')
              Value: 
                IParameterReferenceOperation: y (OperationKind.ParameterReference, Type: System.Object) (Syntax: 'y')

        Jump if True (Regular) to Block[B4]
            IIsNullOperation (OperationKind.IsNull, Type: System.Boolean, IsImplicit) (Syntax: 'y')
              Operand: 
                IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: System.Object, IsImplicit) (Syntax: 'y')

        Next (Regular) Block[B3]
    Block[B3] - Block
        Predecessors: [B2]
        Statements (1)
            IFlowCaptureOperation: 1 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'y')
              Value: 
                IInvalidOperation (OperationKind.Invalid, Type: ?, IsImplicit) (Syntax: 'y')
                  Children(1):
                      IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: System.Object, IsImplicit) (Syntax: 'y')

        Next (Regular) Block[B6]
    Block[B4] - Block
        Predecessors: [B2]
        Statements (0)
        Next (Throw) Block[null]
            IInvalidOperation (OperationKind.Invalid, Type: null, IsInvalid) (Syntax: '')
              Children(0)
    Block[B5] - Block
        Predecessors (0)
        Statements (1)
            IFlowCaptureOperation: 1 (OperationKind.FlowCapture, Type: null, IsInvalid, IsImplicit) (Syntax: 'throw')
              Value: 
                IInvalidOperation (OperationKind.Invalid, Type: ?, IsInvalid, IsImplicit) (Syntax: 'throw')
                  Children(1):
                      IOperation:  (OperationKind.None, Type: null, IsInvalid, IsImplicit) (Syntax: 'throw')

        Next (Regular) Block[B6]
    Block[B6] - Block
        Predecessors: [B3] [B5]
        Statements (1)
            IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null, IsInvalid) (Syntax: 'M(x, (y ?? throw), z);')
              Expression: 
                IInvalidOperation (OperationKind.Invalid, Type: System.Void, IsInvalid) (Syntax: 'M(x, (y ?? throw), z)')
                  Children(3):
                      IParameterReferenceOperation: x (OperationKind.ParameterReference, Type: System.Object) (Syntax: 'x')
                      IFlowCaptureReferenceOperation: 1 (OperationKind.FlowCaptureReference, Type: ?, IsInvalid, IsImplicit) (Syntax: 'y ?? throw')
                      IParameterReferenceOperation: z (OperationKind.ParameterReference, Type: System.Object) (Syntax: 'z')

        Next (Regular) Block[B7]
            Leaving: {R3} {R1}
}

Block[B7] - Exit
    Predecessors: [B1] [B6]
    Statements (0)
";
            VerifyFlowGraphForTest<BlockSyntax>(compilation, expectedGraph);
        }

        [CompilerTrait(CompilerFeature.IOperation, CompilerFeature.Dataflow)]
        [Fact]
        public void ThrowFlow_14()
        {
            var source = @"
class C
{
    void F(System.Exception ex)
    /*<bind>*/{
        ex = null;
        goto label1;
label1:
        throw ex;
    }/*</bind>*/
}";

            var compilation = CreateCompilation(source, parseOptions: TestOptions.RegularWithFlowAnalysisFeature);

            compilation.VerifyDiagnostics();
            
            string expectedGraph = @"
Block[B0] - Entry
    Statements (0)
    Next (Regular) Block[B1]
Block[B1] - Block
    Predecessors: [B0]
    Statements (1)
        IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'ex = null;')
          Expression: 
            ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Exception) (Syntax: 'ex = null')
              Left: 
                IParameterReferenceOperation: ex (OperationKind.ParameterReference, Type: System.Exception) (Syntax: 'ex')
              Right: 
                IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.Exception, Constant: null, IsImplicit) (Syntax: 'null')
                  Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: True, IsUserDefined: False) (MethodSymbol: null)
                    (ImplicitReference)
                  Operand: 
                    ILiteralOperation (OperationKind.Literal, Type: null, Constant: null) (Syntax: 'null')

    Next (Throw) Block[null]
        IParameterReferenceOperation: ex (OperationKind.ParameterReference, Type: System.Exception) (Syntax: 'ex')
Block[B2] - Exit
    Predecessors (0)
    Statements (0)
";
            VerifyFlowGraphForTest<BlockSyntax>(compilation, expectedGraph);
        }

        [CompilerTrait(CompilerFeature.IOperation, CompilerFeature.Dataflow)]
        [Fact]
        public void ThrowFlow_15()
        {
            var source = @"
class C
{
    void F(int x)
    /*<bind>*/{
        try
        {
            x = 1;
        }
        catch
        {
            x = 2;
            goto label1;
label1:
            throw;
        }
    }/*</bind>*/
}";

            var compilation = CreateCompilation(source, parseOptions: TestOptions.RegularWithFlowAnalysisFeature);

            compilation.VerifyDiagnostics();
            
            string expectedGraph = @"
Block[B0] - Entry
    Statements (0)
    Next (Regular) Block[B1]
        Entering: {R1} {R2}

.try {R1, R2}
{
    Block[B1] - Block
        Predecessors: [B0]
        Statements (1)
            IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'x = 1;')
              Expression: 
                ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Int32) (Syntax: 'x = 1')
                  Left: 
                    IParameterReferenceOperation: x (OperationKind.ParameterReference, Type: System.Int32) (Syntax: 'x')
                  Right: 
                    ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 1) (Syntax: '1')

        Next (Regular) Block[B3]
            Leaving: {R2} {R1}
}
.catch {R3} (System.Object)
{
    Block[B2] - Block
        Predecessors (0)
        Statements (1)
            IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'x = 2;')
              Expression: 
                ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Int32) (Syntax: 'x = 2')
                  Left: 
                    IParameterReferenceOperation: x (OperationKind.ParameterReference, Type: System.Int32) (Syntax: 'x')
                  Right: 
                    ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 2) (Syntax: '2')

        Next (ReThrow) Block[null]
}

Block[B3] - Exit
    Predecessors: [B1]
    Statements (0)
";
            VerifyFlowGraphForTest<BlockSyntax>(compilation, expectedGraph);
        }

        [CompilerTrait(CompilerFeature.IOperation, CompilerFeature.Dataflow)]
        [Fact]
        public void ThrowFlow_16()
        {
            var source = @"
class C
{
    void F(System.Exception ex, bool a)
    /*<bind>*/{
        if (a) goto label1;
label1:
        throw ex;
    }/*</bind>*/
}";

            var compilation = CreateCompilation(source, parseOptions: TestOptions.RegularWithFlowAnalysisFeature);

            compilation.VerifyDiagnostics();
            
            string expectedGraph = @"
Block[B0] - Entry
    Statements (0)
    Next (Regular) Block[B1]
Block[B1] - Block
    Predecessors: [B0]
    Statements (0)
    Jump if False (Regular) to Block[B2]
        IParameterReferenceOperation: a (OperationKind.ParameterReference, Type: System.Boolean) (Syntax: 'a')

    Next (Regular) Block[B2]
Block[B2] - Block
    Predecessors: [B1]
    Statements (0)
    Next (Throw) Block[null]
        IParameterReferenceOperation: ex (OperationKind.ParameterReference, Type: System.Exception) (Syntax: 'ex')
Block[B3] - Exit
    Predecessors (0)
    Statements (0)
";
            VerifyFlowGraphForTest<BlockSyntax>(compilation, expectedGraph);
        }

        [CompilerTrait(CompilerFeature.IOperation, CompilerFeature.Dataflow)]
        [Fact]
        public void ThrowFlow_17()
        {
            var source = @"
class C
{
    void F(int x, bool a)
    /*<bind>*/{
        try
        {
            x = 1;
        }
        catch
        {
            if (a) goto label1;
label1:
            throw;
        }
    }/*</bind>*/
}";

            var compilation = CreateCompilation(source, parseOptions: TestOptions.RegularWithFlowAnalysisFeature);

            compilation.VerifyDiagnostics();
            
            string expectedGraph = @"
Block[B0] - Entry
    Statements (0)
    Next (Regular) Block[B1]
        Entering: {R1} {R2}

.try {R1, R2}
{
    Block[B1] - Block
        Predecessors: [B0]
        Statements (1)
            IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'x = 1;')
              Expression: 
                ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Int32) (Syntax: 'x = 1')
                  Left: 
                    IParameterReferenceOperation: x (OperationKind.ParameterReference, Type: System.Int32) (Syntax: 'x')
                  Right: 
                    ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 1) (Syntax: '1')

        Next (Regular) Block[B3]
            Leaving: {R2} {R1}
}
.catch {R3} (System.Object)
{
    Block[B2] - Block
        Predecessors (0)
        Statements (0)
        Jump if False (ReThrow) to Block[null]
            IParameterReferenceOperation: a (OperationKind.ParameterReference, Type: System.Boolean) (Syntax: 'a')

        Next (ReThrow) Block[null]
}

Block[B3] - Exit
    Predecessors: [B1]
    Statements (0)
";
            VerifyFlowGraphForTest<BlockSyntax>(compilation, expectedGraph);
        }

        [CompilerTrait(CompilerFeature.IOperation, CompilerFeature.Dataflow)]
        [Fact]
        public void ThrowFlow_18()
        {
            var source = @"
#pragma warning disable CS0168
#pragma warning disable CS0219
class C
{
    void F(System.Exception ex)
    /*<bind>*/{
        {
            int x = 1;
        }

        throw ex;
    }/*</bind>*/
}";

            var compilation = CreateCompilation(source, parseOptions: TestOptions.RegularWithFlowAnalysisFeature);

            compilation.VerifyDiagnostics();

            string expectedGraph = @"
Block[B0] - Entry
    Statements (0)
    Next (Regular) Block[B1]
        Entering: {R1}

.locals {R1}
{
    Locals: [System.Int32 x]
    Block[B1] - Block
        Predecessors: [B0]
        Statements (1)
            ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Int32, IsImplicit) (Syntax: 'x = 1')
              Left: 
                ILocalReferenceOperation: x (IsDeclaration: True) (OperationKind.LocalReference, Type: System.Int32, IsImplicit) (Syntax: 'x = 1')
              Right: 
                ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 1) (Syntax: '1')

        Next (Regular) Block[B2]
            Leaving: {R1}
}

Block[B2] - Block
    Predecessors: [B1]
    Statements (0)
    Next (Throw) Block[null]
        IParameterReferenceOperation: ex (OperationKind.ParameterReference, Type: System.Exception) (Syntax: 'ex')
Block[B3] - Exit
    Predecessors (0)
    Statements (0)
";
            VerifyFlowGraphForTest<BlockSyntax>(compilation, expectedGraph);
        }

        [CompilerTrait(CompilerFeature.IOperation, CompilerFeature.Dataflow)]
        [Fact]
        public void ThrowFlow_19()
        {
            var source = @"
#pragma warning disable CS0168
#pragma warning disable CS0219
class C
{
    void F(int x)
    /*<bind>*/{
        try
        {
            x = 1;
        }
        catch
        {
            {
                int y = 1;
            }

            throw;
        }
    }/*</bind>*/
}";

            var compilation = CreateCompilation(source, parseOptions: TestOptions.RegularWithFlowAnalysisFeature);

            compilation.VerifyDiagnostics();

            string expectedGraph = @"
Block[B0] - Entry
    Statements (0)
    Next (Regular) Block[B1]
        Entering: {R1} {R2}

.try {R1, R2}
{
    Block[B1] - Block
        Predecessors: [B0]
        Statements (1)
            IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'x = 1;')
              Expression: 
                ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Int32) (Syntax: 'x = 1')
                  Left: 
                    IParameterReferenceOperation: x (OperationKind.ParameterReference, Type: System.Int32) (Syntax: 'x')
                  Right: 
                    ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 1) (Syntax: '1')

        Next (Regular) Block[B4]
            Leaving: {R2} {R1}
}
.catch {R3} (System.Object)
{
    .locals {R4}
    {
        Locals: [System.Int32 y]
        Block[B2] - Block
            Predecessors (0)
            Statements (1)
                ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Int32, IsImplicit) (Syntax: 'y = 1')
                  Left: 
                    ILocalReferenceOperation: y (IsDeclaration: True) (OperationKind.LocalReference, Type: System.Int32, IsImplicit) (Syntax: 'y = 1')
                  Right: 
                    ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 1) (Syntax: '1')

            Next (Regular) Block[B3]
                Leaving: {R4}
    }

    Block[B3] - Block
        Predecessors: [B2]
        Statements (0)
        Next (ReThrow) Block[null]
}

Block[B4] - Exit
    Predecessors: [B1]
    Statements (0)
";
            VerifyFlowGraphForTest<BlockSyntax>(compilation, expectedGraph);
        }

        [CompilerTrait(CompilerFeature.IOperation, CompilerFeature.Dataflow)]
        [Fact]
        public void ThrowFlow_20()
        {
            var source = @"
class C
{
    void F(System.Exception ex, int x)
    /*<bind>*/{
        try
        {
            x = 1;
        }
        catch
        {
label1:
            throw ex;
            x = 2;
            goto label1;
        }
    }/*</bind>*/
}";

            var compilation = CreateCompilation(source, parseOptions: TestOptions.RegularWithFlowAnalysisFeature);

            compilation.VerifyDiagnostics(
                // (14,13): warning CS0162: Unreachable code detected
                //             x = 2;
                Diagnostic(ErrorCode.WRN_UnreachableCode, "x").WithLocation(14, 13)
                );

            string expectedGraph = @"
Block[B0] - Entry
    Statements (0)
    Next (Regular) Block[B1]
        Entering: {R1} {R2}

.try {R1, R2}
{
    Block[B1] - Block
        Predecessors: [B0]
        Statements (1)
            IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'x = 1;')
              Expression: 
                ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Int32) (Syntax: 'x = 1')
                  Left: 
                    IParameterReferenceOperation: x (OperationKind.ParameterReference, Type: System.Int32) (Syntax: 'x')
                  Right: 
                    ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 1) (Syntax: '1')

        Next (Regular) Block[B4]
            Leaving: {R2} {R1}
}
.catch {R3} (System.Object)
{
    Block[B2] - Block
        Predecessors: [B3]
        Statements (0)
        Next (Throw) Block[null]
            IParameterReferenceOperation: ex (OperationKind.ParameterReference, Type: System.Exception) (Syntax: 'ex')
    Block[B3] - Block
        Predecessors (0)
        Statements (1)
            IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'x = 2;')
              Expression: 
                ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Int32) (Syntax: 'x = 2')
                  Left: 
                    IParameterReferenceOperation: x (OperationKind.ParameterReference, Type: System.Int32) (Syntax: 'x')
                  Right: 
                    ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 2) (Syntax: '2')

        Next (Regular) Block[B2]
}

Block[B4] - Exit
    Predecessors: [B1]
    Statements (0)
";
            VerifyFlowGraphForTest<BlockSyntax>(compilation, expectedGraph);
        }

        [CompilerTrait(CompilerFeature.IOperation, CompilerFeature.Dataflow)]
        [Fact]
        public void ThrowFlow_21()
        {
            var source = @"
class C
{
    void F(int x)
    /*<bind>*/{
        try
        {
            x = 1;
        }
        catch
        {
label1:
            throw;
            x = 2;
            goto label1;
        }
    }/*</bind>*/
}";

            var compilation = CreateCompilation(source, parseOptions: TestOptions.RegularWithFlowAnalysisFeature);

            compilation.VerifyDiagnostics(
                // (14,13): warning CS0162: Unreachable code detected
                //             x = 2;
                Diagnostic(ErrorCode.WRN_UnreachableCode, "x").WithLocation(14, 13)
                );

            string expectedGraph = @"
Block[B0] - Entry
    Statements (0)
    Next (Regular) Block[B1]
        Entering: {R1} {R2}

.try {R1, R2}
{
    Block[B1] - Block
        Predecessors: [B0]
        Statements (1)
            IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'x = 1;')
              Expression: 
                ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Int32) (Syntax: 'x = 1')
                  Left: 
                    IParameterReferenceOperation: x (OperationKind.ParameterReference, Type: System.Int32) (Syntax: 'x')
                  Right: 
                    ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 1) (Syntax: '1')

        Next (Regular) Block[B4]
            Leaving: {R2} {R1}
}
.catch {R3} (System.Object)
{
    Block[B2] - Block
        Predecessors: [B3]
        Statements (0)
        Next (ReThrow) Block[null]
    Block[B3] - Block
        Predecessors (0)
        Statements (1)
            IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'x = 2;')
              Expression: 
                ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Int32) (Syntax: 'x = 2')
                  Left: 
                    IParameterReferenceOperation: x (OperationKind.ParameterReference, Type: System.Int32) (Syntax: 'x')
                  Right: 
                    ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 2) (Syntax: '2')

        Next (Regular) Block[B2]
}

Block[B4] - Exit
    Predecessors: [B1]
    Statements (0)
";
            VerifyFlowGraphForTest<BlockSyntax>(compilation, expectedGraph);
        }

        [CompilerTrait(CompilerFeature.IOperation, CompilerFeature.Dataflow)]
        [Fact]
        public void ThrowFlow_22()
        {
            var source = @"
class C
{
    int F(bool a, System.Exception ex1, System.Exception ex2)
    /*<bind>*/{
        if (a) throw ex1;
        goto label1;

label1:
        goto label2;
label2:
        throw ex2;
    }/*</bind>*/
}";

            var compilation = CreateCompilation(source, parseOptions: TestOptions.RegularWithFlowAnalysisFeature);

            compilation.VerifyDiagnostics();
            
            string expectedGraph = @"
Block[B0] - Entry
    Statements (0)
    Next (Regular) Block[B1]
Block[B1] - Block
    Predecessors: [B0]
    Statements (0)
    Jump if False (Regular) to Block[B2]
        IParameterReferenceOperation: a (OperationKind.ParameterReference, Type: System.Boolean) (Syntax: 'a')

    Next (Throw) Block[null]
        IParameterReferenceOperation: ex1 (OperationKind.ParameterReference, Type: System.Exception) (Syntax: 'ex1')
Block[B2] - Block
    Predecessors: [B1]
    Statements (0)
    Next (Throw) Block[null]
        IParameterReferenceOperation: ex2 (OperationKind.ParameterReference, Type: System.Exception) (Syntax: 'ex2')
Block[B3] - Exit
    Predecessors (0)
    Statements (0)
";
            VerifyFlowGraphForTest<BlockSyntax>(compilation, expectedGraph);
        }

        [CompilerTrait(CompilerFeature.IOperation, CompilerFeature.Dataflow)]
        [Fact]
        public void ThrowFlow_23()
        {
            var source = @"
class C
{
    void F(int x, System.Exception ex1, System.Exception ex2, bool a)
    /*<bind>*/{
        x = 1;
        goto label2;
label1:
        if (a) throw ex1;
        throw ex2;
label2:
        goto label1;
    }/*</bind>*/
}";

            var compilation = CreateCompilation(source, parseOptions: TestOptions.RegularWithFlowAnalysisFeature);

            compilation.VerifyDiagnostics();

            string expectedGraph = @"
Block[B0] - Entry
    Statements (0)
    Next (Regular) Block[B1]
Block[B1] - Block
    Predecessors: [B0]
    Statements (1)
        IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'x = 1;')
          Expression: 
            ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Int32) (Syntax: 'x = 1')
              Left: 
                IParameterReferenceOperation: x (OperationKind.ParameterReference, Type: System.Int32) (Syntax: 'x')
              Right: 
                ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 1) (Syntax: '1')

    Jump if False (Regular) to Block[B2]
        IParameterReferenceOperation: a (OperationKind.ParameterReference, Type: System.Boolean) (Syntax: 'a')

    Next (Throw) Block[null]
        IParameterReferenceOperation: ex1 (OperationKind.ParameterReference, Type: System.Exception) (Syntax: 'ex1')
Block[B2] - Block
    Predecessors: [B1]
    Statements (0)
    Next (Throw) Block[null]
        IParameterReferenceOperation: ex2 (OperationKind.ParameterReference, Type: System.Exception) (Syntax: 'ex2')
Block[B3] - Exit
    Predecessors (0)
    Statements (0)
";
            VerifyFlowGraphForTest<BlockSyntax>(compilation, expectedGraph);
        }

        [CompilerTrait(CompilerFeature.IOperation, CompilerFeature.Dataflow)]
        [Fact]
        public void ThrowFlow_24()
        {
            var source = @"
class C
{
    void F(int x, bool a)
    /*<bind>*/{
        try
        {
            x = 1;
        }
        catch
        {
            x = 2;
            goto label2;
label1:
            if (a) throw;
            throw;
label2:
            goto label1;
        }
    }/*</bind>*/
}";

            var compilation = CreateCompilation(source, parseOptions: TestOptions.RegularWithFlowAnalysisFeature);

            compilation.VerifyDiagnostics();

            string expectedGraph = @"
Block[B0] - Entry
    Statements (0)
    Next (Regular) Block[B1]
        Entering: {R1} {R2}

.try {R1, R2}
{
    Block[B1] - Block
        Predecessors: [B0]
        Statements (1)
            IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'x = 1;')
              Expression: 
                ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Int32) (Syntax: 'x = 1')
                  Left: 
                    IParameterReferenceOperation: x (OperationKind.ParameterReference, Type: System.Int32) (Syntax: 'x')
                  Right: 
                    ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 1) (Syntax: '1')

        Next (Regular) Block[B3]
            Leaving: {R2} {R1}
}
.catch {R3} (System.Object)
{
    Block[B2] - Block
        Predecessors (0)
        Statements (1)
            IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'x = 2;')
              Expression: 
                ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Int32) (Syntax: 'x = 2')
                  Left: 
                    IParameterReferenceOperation: x (OperationKind.ParameterReference, Type: System.Int32) (Syntax: 'x')
                  Right: 
                    ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 2) (Syntax: '2')

        Jump if False (ReThrow) to Block[null]
            IParameterReferenceOperation: a (OperationKind.ParameterReference, Type: System.Boolean) (Syntax: 'a')

        Next (ReThrow) Block[null]
}

Block[B3] - Exit
    Predecessors: [B1]
    Statements (0)
";
            VerifyFlowGraphForTest<BlockSyntax>(compilation, expectedGraph);
        }

        [CompilerTrait(CompilerFeature.IOperation, CompilerFeature.Dataflow)]
        [Fact]
        public void ThrowFlow_25()
        {
            var source = @"
class C
{
    void F(int x, bool a)
    /*<bind>*/{
        try
        {
            x = 1;
        }
        catch
        {
            x = 2;
            goto label2;
label1:
            if (a) throw;
            return;
label2:
            goto label1;
        }
    }/*</bind>*/
}";

            var compilation = CreateCompilation(source, parseOptions: TestOptions.RegularWithFlowAnalysisFeature);

            compilation.VerifyDiagnostics();

            string expectedGraph = @"
Block[B0] - Entry
    Statements (0)
    Next (Regular) Block[B1]
        Entering: {R1} {R2}

.try {R1, R2}
{
    Block[B1] - Block
        Predecessors: [B0]
        Statements (1)
            IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'x = 1;')
              Expression: 
                ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Int32) (Syntax: 'x = 1')
                  Left: 
                    IParameterReferenceOperation: x (OperationKind.ParameterReference, Type: System.Int32) (Syntax: 'x')
                  Right: 
                    ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 1) (Syntax: '1')

        Next (Regular) Block[B3]
            Leaving: {R2} {R1}
}
.catch {R3} (System.Object)
{
    Block[B2] - Block
        Predecessors (0)
        Statements (1)
            IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'x = 2;')
              Expression: 
                ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Int32) (Syntax: 'x = 2')
                  Left: 
                    IParameterReferenceOperation: x (OperationKind.ParameterReference, Type: System.Int32) (Syntax: 'x')
                  Right: 
                    ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 2) (Syntax: '2')

        Jump if False (Regular) to Block[B3]
            IParameterReferenceOperation: a (OperationKind.ParameterReference, Type: System.Boolean) (Syntax: 'a')
            Leaving: {R3} {R1}

        Next (ReThrow) Block[null]
}

Block[B3] - Exit
    Predecessors: [B1] [B2]
    Statements (0)
";
            VerifyFlowGraphForTest<BlockSyntax>(compilation, expectedGraph);
        }

        [CompilerTrait(CompilerFeature.IOperation, CompilerFeature.Dataflow)]
        [Fact]
        public void ThrowFlow_26()
        {
            var source = @"
class C
{
    void F(int x, bool a)
    /*<bind>*/{
        try
        {
            x = 1;
        }
        catch
        {
            x = 2;
            goto label2;
label1:
            if (a) return;
            throw;
label2:
            goto label1;
        }
    }/*</bind>*/
}";

            var compilation = CreateCompilation(source, parseOptions: TestOptions.RegularWithFlowAnalysisFeature);

            compilation.VerifyDiagnostics();

            string expectedGraph = @"
Block[B0] - Entry
    Statements (0)
    Next (Regular) Block[B1]
        Entering: {R1} {R2}

.try {R1, R2}
{
    Block[B1] - Block
        Predecessors: [B0]
        Statements (1)
            IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'x = 1;')
              Expression: 
                ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Int32) (Syntax: 'x = 1')
                  Left: 
                    IParameterReferenceOperation: x (OperationKind.ParameterReference, Type: System.Int32) (Syntax: 'x')
                  Right: 
                    ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 1) (Syntax: '1')

        Next (Regular) Block[B3]
            Leaving: {R2} {R1}
}
.catch {R3} (System.Object)
{
    Block[B2] - Block
        Predecessors (0)
        Statements (1)
            IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'x = 2;')
              Expression: 
                ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Int32) (Syntax: 'x = 2')
                  Left: 
                    IParameterReferenceOperation: x (OperationKind.ParameterReference, Type: System.Int32) (Syntax: 'x')
                  Right: 
                    ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 2) (Syntax: '2')

        Jump if False (ReThrow) to Block[null]
            IParameterReferenceOperation: a (OperationKind.ParameterReference, Type: System.Boolean) (Syntax: 'a')

        Next (Regular) Block[B3]
            Leaving: {R3} {R1}
}

Block[B3] - Exit
    Predecessors: [B1] [B2]
    Statements (0)
";
            VerifyFlowGraphForTest<BlockSyntax>(compilation, expectedGraph);
        }

        [CompilerTrait(CompilerFeature.IOperation, CompilerFeature.Dataflow)]
        [Fact]
        public void ThrowFlow_27()
        {
            var source = @"
class C
{
    void F(int u)
    /*<bind>*/{
        try
        {
            u = 1;
        }
        finally
        {
            throw;
        }
    }/*</bind>*/
}";

            var compilation = CreateCompilation(source, parseOptions: TestOptions.RegularWithFlowAnalysisFeature);

            compilation.VerifyDiagnostics(
                // (12,13): error CS0156: A throw statement with no arguments is not allowed outside of a catch clause
                //             throw;
                Diagnostic(ErrorCode.ERR_BadEmptyThrow, "throw").WithLocation(12, 13)
                );

            string expectedGraph = @"
Block[B0] - Entry
    Statements (0)
    Next (Regular) Block[B1]
        Entering: {R1} {R2}

.try {R1, R2}
{
    Block[B1] - Block
        Predecessors: [B0]
        Statements (1)
            IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'u = 1;')
              Expression: 
                ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Int32) (Syntax: 'u = 1')
                  Left: 
                    IParameterReferenceOperation: u (OperationKind.ParameterReference, Type: System.Int32) (Syntax: 'u')
                  Right: 
                    ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 1) (Syntax: '1')

        Next (Regular) Block[B3]
            Finalizing: {R3}
            Leaving: {R2} {R1}
}
.finally {R3}
{
    Block[B2] - Block
        Predecessors (0)
        Statements (0)
        Next (ReThrow) Block[null]
}

Block[B3] - Exit
    Predecessors: [B1]
    Statements (0)
";
            VerifyFlowGraphForTest<BlockSyntax>(compilation, expectedGraph);
        }

        [CompilerTrait(CompilerFeature.IOperation, CompilerFeature.Dataflow)]
        [Fact]
        public void ThrowFlow_28()
        {
            var source = @"
class C
{
    void F(int u, System.Exception ex)
    /*<bind>*/{
        try
        {
            u = 1;
        }
        finally
        {
            throw ex;
        }
    }/*</bind>*/
}";

            var compilation = CreateCompilation(source, parseOptions: TestOptions.RegularWithFlowAnalysisFeature);

            compilation.VerifyDiagnostics();

            string expectedGraph = @"
Block[B0] - Entry
    Statements (0)
    Next (Regular) Block[B1]
        Entering: {R1} {R2}

.try {R1, R2}
{
    Block[B1] - Block
        Predecessors: [B0]
        Statements (1)
            IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'u = 1;')
              Expression: 
                ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Int32) (Syntax: 'u = 1')
                  Left: 
                    IParameterReferenceOperation: u (OperationKind.ParameterReference, Type: System.Int32) (Syntax: 'u')
                  Right: 
                    ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 1) (Syntax: '1')

        Next (Regular) Block[B3]
            Finalizing: {R3}
            Leaving: {R2} {R1}
}
.finally {R3}
{
    Block[B2] - Block
        Predecessors (0)
        Statements (0)
        Next (Throw) Block[null]
            IParameterReferenceOperation: ex (OperationKind.ParameterReference, Type: System.Exception) (Syntax: 'ex')
}

Block[B3] - Exit
    Predecessors: [B1]
    Statements (0)
";
            VerifyFlowGraphForTest<BlockSyntax>(compilation, expectedGraph);
        }

        // PROTOTYPE(dataflow): Port these tests to VB
    }
}
