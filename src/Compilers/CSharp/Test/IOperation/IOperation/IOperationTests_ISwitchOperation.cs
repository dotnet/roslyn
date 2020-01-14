// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.CSharp.Test.Utilities;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.CSharp.UnitTests
{
    public partial class IOperationTests_Patterns : SemanticModelTestBase
    {
        [CompilerTrait(CompilerFeature.IOperation)]
        [Fact]
        public void LocalsInSwitch_01()
        {
            string source = @"
using System;

class Program
{
    static void M(int input)
    {
        /*<bind>*/switch (input)
        {
            case 1:
                break;
        }/*</bind>*/
    }
}
";
            string expectedOperationTree = @"
ISwitchOperation (1 cases, Exit Label Id: 0) (OperationKind.Switch, Type: null) (Syntax: 'switch (inp ... }')
  Switch expression: 
    IParameterReferenceOperation: input (OperationKind.ParameterReference, Type: System.Int32) (Syntax: 'input')
  Sections:
      ISwitchCaseOperation (1 case clauses, 1 statements) (OperationKind.SwitchCase, Type: null) (Syntax: 'case 1: ... break;')
          Clauses:
              ISingleValueCaseClauseOperation (Label Id: 1) (CaseKind.SingleValue) (OperationKind.CaseClause, Type: null) (Syntax: 'case 1:')
                Value: 
                  ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 1) (Syntax: '1')
          Body:
              IBranchOperation (BranchKind.Break, Label Id: 0) (OperationKind.Branch, Type: null) (Syntax: 'break;')
";
            var expectedDiagnostics = DiagnosticDescription.None;

            VerifyOperationTreeAndDiagnosticsForTest<SwitchStatementSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation)]
        [Fact]
        public void LocalsInSwitch_02()
        {
            string source = @"
using System;

class Program
{
    static void M(int input)
    {
        /*<bind>*/switch (input)
        {
            case 1:
                var x = 3;
                input = x;
                break;
        }/*</bind>*/
    }
}
";
            string expectedOperationTree = @"
ISwitchOperation (1 cases, Exit Label Id: 0) (OperationKind.Switch, Type: null) (Syntax: 'switch (inp ... }')
  Switch expression: 
    IParameterReferenceOperation: input (OperationKind.ParameterReference, Type: System.Int32) (Syntax: 'input')
  Locals: Local_1: System.Int32 x
  Sections:
      ISwitchCaseOperation (1 case clauses, 3 statements) (OperationKind.SwitchCase, Type: null) (Syntax: 'case 1: ... break;')
          Clauses:
              ISingleValueCaseClauseOperation (Label Id: 1) (CaseKind.SingleValue) (OperationKind.CaseClause, Type: null) (Syntax: 'case 1:')
                Value: 
                  ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 1) (Syntax: '1')
          Body:
              IVariableDeclarationGroupOperation (1 declarations) (OperationKind.VariableDeclarationGroup, Type: null) (Syntax: 'var x = 3;')
                IVariableDeclarationOperation (1 declarators) (OperationKind.VariableDeclaration, Type: null) (Syntax: 'var x = 3')
                  Declarators:
                      IVariableDeclaratorOperation (Symbol: System.Int32 x) (OperationKind.VariableDeclarator, Type: null) (Syntax: 'x = 3')
                        Initializer: 
                          IVariableInitializerOperation (OperationKind.VariableInitializer, Type: null) (Syntax: '= 3')
                            ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 3) (Syntax: '3')
                  Initializer: 
                    null
              IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'input = x;')
                Expression: 
                  ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Int32) (Syntax: 'input = x')
                    Left: 
                      IParameterReferenceOperation: input (OperationKind.ParameterReference, Type: System.Int32) (Syntax: 'input')
                    Right: 
                      ILocalReferenceOperation: x (OperationKind.LocalReference, Type: System.Int32) (Syntax: 'x')
              IBranchOperation (BranchKind.Break, Label Id: 0) (OperationKind.Branch, Type: null) (Syntax: 'break;')
";
            var expectedDiagnostics = DiagnosticDescription.None;

            VerifyOperationTreeAndDiagnosticsForTest<SwitchStatementSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation)]
        [Fact]
        public void LocalsInSwitch_03()
        {
            string source = @"
using System;

class Program
{
    static void M(object input)
    {
        /*<bind>*/switch (input)
        {
            case int x:
            case long y:
                break;
        }/*</bind>*/
    }
}
";
            string expectedOperationTree = @"
ISwitchOperation (1 cases, Exit Label Id: 0) (OperationKind.Switch, Type: null) (Syntax: 'switch (inp ... }')
  Switch expression: 
    IParameterReferenceOperation: input (OperationKind.ParameterReference, Type: System.Object) (Syntax: 'input')
  Sections:
      ISwitchCaseOperation (2 case clauses, 1 statements) (OperationKind.SwitchCase, Type: null) (Syntax: 'case int x: ... break;')
        Locals: Local_1: System.Int32 x
          Local_2: System.Int64 y
          Clauses:
              IPatternCaseClauseOperation (Label Id: 1) (CaseKind.Pattern) (OperationKind.CaseClause, Type: null) (Syntax: 'case int x:')
                Pattern: 
                  IDeclarationPatternOperation (OperationKind.DeclarationPattern, Type: null) (Syntax: 'int x') (InputType: System.Object, DeclaredSymbol: System.Int32 x, MatchesNull: False)
              IPatternCaseClauseOperation (Label Id: 2) (CaseKind.Pattern) (OperationKind.CaseClause, Type: null) (Syntax: 'case long y:')
                Pattern: 
                  IDeclarationPatternOperation (OperationKind.DeclarationPattern, Type: null) (Syntax: 'long y') (InputType: System.Object, DeclaredSymbol: System.Int64 y, MatchesNull: False)
          Body:
              IBranchOperation (BranchKind.Break, Label Id: 0) (OperationKind.Branch, Type: null) (Syntax: 'break;')
";
            var expectedDiagnostics = DiagnosticDescription.None;

            VerifyOperationTreeAndDiagnosticsForTest<SwitchStatementSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation)]
        [Fact]
        public void LocalsInSwitch_04()
        {
            string source = @"
using System;

class Program
{
    static void M(object input)
    {
        /*<bind>*/switch (input)
        {
            case int y:
                var x = 3;
                input = x + y;
                break;
        }/*</bind>*/
    }
}
";
            string expectedOperationTree = @"
ISwitchOperation (1 cases, Exit Label Id: 0) (OperationKind.Switch, Type: null) (Syntax: 'switch (inp ... }')
  Switch expression: 
    IParameterReferenceOperation: input (OperationKind.ParameterReference, Type: System.Object) (Syntax: 'input')
  Locals: Local_1: System.Int32 x
  Sections:
      ISwitchCaseOperation (1 case clauses, 3 statements) (OperationKind.SwitchCase, Type: null) (Syntax: 'case int y: ... break;')
        Locals: Local_1: System.Int32 y
          Clauses:
              IPatternCaseClauseOperation (Label Id: 1) (CaseKind.Pattern) (OperationKind.CaseClause, Type: null) (Syntax: 'case int y:')
                Pattern: 
                  IDeclarationPatternOperation (OperationKind.DeclarationPattern, Type: null) (Syntax: 'int y') (InputType: System.Object, DeclaredSymbol: System.Int32 y, MatchesNull: False)
          Body:
              IVariableDeclarationGroupOperation (1 declarations) (OperationKind.VariableDeclarationGroup, Type: null) (Syntax: 'var x = 3;')
                IVariableDeclarationOperation (1 declarators) (OperationKind.VariableDeclaration, Type: null) (Syntax: 'var x = 3')
                  Declarators:
                      IVariableDeclaratorOperation (Symbol: System.Int32 x) (OperationKind.VariableDeclarator, Type: null) (Syntax: 'x = 3')
                        Initializer: 
                          IVariableInitializerOperation (OperationKind.VariableInitializer, Type: null) (Syntax: '= 3')
                            ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 3) (Syntax: '3')
                  Initializer: 
                    null
              IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'input = x + y;')
                Expression: 
                  ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Object) (Syntax: 'input = x + y')
                    Left: 
                      IParameterReferenceOperation: input (OperationKind.ParameterReference, Type: System.Object) (Syntax: 'input')
                    Right: 
                      IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.Object, IsImplicit) (Syntax: 'x + y')
                        Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                        Operand: 
                          IBinaryOperation (BinaryOperatorKind.Add) (OperationKind.Binary, Type: System.Int32) (Syntax: 'x + y')
                            Left: 
                              ILocalReferenceOperation: x (OperationKind.LocalReference, Type: System.Int32) (Syntax: 'x')
                            Right: 
                              ILocalReferenceOperation: y (OperationKind.LocalReference, Type: System.Int32) (Syntax: 'y')
              IBranchOperation (BranchKind.Break, Label Id: 0) (OperationKind.Branch, Type: null) (Syntax: 'break;')
";
            var expectedDiagnostics = DiagnosticDescription.None;

            VerifyOperationTreeAndDiagnosticsForTest<SwitchStatementSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation)]
        [Fact]
        public void LabelsInSwitch_01()
        {
            string source = @"
using System;

class Program
{
    static void M(int input)
    {
        /*<bind>*/switch (input)
        {
            case 1:
                goto case 2;
            case 2:
                break;
        }/*</bind>*/
    }
}
";
            string expectedOperationTree = @"
ISwitchOperation (2 cases, Exit Label Id: 0) (OperationKind.Switch, Type: null) (Syntax: 'switch (inp ... }')
  Switch expression: 
    IParameterReferenceOperation: input (OperationKind.ParameterReference, Type: System.Int32) (Syntax: 'input')
  Sections:
      ISwitchCaseOperation (1 case clauses, 1 statements) (OperationKind.SwitchCase, Type: null) (Syntax: 'case 1: ... oto case 2;')
          Clauses:
              ISingleValueCaseClauseOperation (Label Id: 1) (CaseKind.SingleValue) (OperationKind.CaseClause, Type: null) (Syntax: 'case 1:')
                Value: 
                  ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 1) (Syntax: '1')
          Body:
              IBranchOperation (BranchKind.GoTo, Label Id: 2) (OperationKind.Branch, Type: null) (Syntax: 'goto case 2;')
      ISwitchCaseOperation (1 case clauses, 1 statements) (OperationKind.SwitchCase, Type: null) (Syntax: 'case 2: ... break;')
          Clauses:
              ISingleValueCaseClauseOperation (Label Id: 2) (CaseKind.SingleValue) (OperationKind.CaseClause, Type: null) (Syntax: 'case 2:')
                Value: 
                  ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 2) (Syntax: '2')
          Body:
              IBranchOperation (BranchKind.Break, Label Id: 0) (OperationKind.Branch, Type: null) (Syntax: 'break;')
";
            var expectedDiagnostics = DiagnosticDescription.None;

            VerifyOperationTreeAndDiagnosticsForTest<SwitchStatementSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation)]
        [Fact]
        public void LabelsInSwitch_02()
        {
            string source = @"
using System;

class Program
{
    static void M(int input)
    {
        /*<bind>*/switch (input)
        {
            case 1:
                goto default;
            default:
                break;
        }/*</bind>*/
    }
}
";
            string expectedOperationTree = @"
ISwitchOperation (2 cases, Exit Label Id: 0) (OperationKind.Switch, Type: null) (Syntax: 'switch (inp ... }')
  Switch expression: 
    IParameterReferenceOperation: input (OperationKind.ParameterReference, Type: System.Int32) (Syntax: 'input')
  Sections:
      ISwitchCaseOperation (1 case clauses, 1 statements) (OperationKind.SwitchCase, Type: null) (Syntax: 'case 1: ... to default;')
          Clauses:
              ISingleValueCaseClauseOperation (Label Id: 1) (CaseKind.SingleValue) (OperationKind.CaseClause, Type: null) (Syntax: 'case 1:')
                Value: 
                  ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 1) (Syntax: '1')
          Body:
              IBranchOperation (BranchKind.GoTo, Label Id: 2) (OperationKind.Branch, Type: null) (Syntax: 'goto default;')
      ISwitchCaseOperation (1 case clauses, 1 statements) (OperationKind.SwitchCase, Type: null) (Syntax: 'default: ... break;')
          Clauses:
              IDefaultCaseClauseOperation (Label Id: 2) (CaseKind.Default) (OperationKind.CaseClause, Type: null) (Syntax: 'default:')
          Body:
              IBranchOperation (BranchKind.Break, Label Id: 0) (OperationKind.Branch, Type: null) (Syntax: 'break;')
";
            var expectedDiagnostics = DiagnosticDescription.None;

            VerifyOperationTreeAndDiagnosticsForTest<SwitchStatementSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation, CompilerFeature.Dataflow)]
        [Fact]
        public void SwitchFlow_01()
        {
            string source = @"
public sealed class MyClass
{
    void M(bool result, int input)
    /*<bind>*/{
        switch (input)
        {
            default:
                result = true;
                break;
        }
    }/*</bind>*/
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
    CaptureIds: [0]
    Block[B1] - Block
        Predecessors: [B0]
        Statements (2)
            IFlowCaptureOperation: 0 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'input')
              Value: 
                IParameterReferenceOperation: input (OperationKind.ParameterReference, Type: System.Int32) (Syntax: 'input')

            IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'result = true;')
              Expression: 
                ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Boolean) (Syntax: 'result = true')
                  Left: 
                    IParameterReferenceOperation: result (OperationKind.ParameterReference, Type: System.Boolean) (Syntax: 'result')
                  Right: 
                    ILiteralOperation (OperationKind.Literal, Type: System.Boolean, Constant: True) (Syntax: 'true')

        Next (Regular) Block[B2]
            Leaving: {R1}
}

Block[B2] - Exit
    Predecessors: [B1]
    Statements (0)
";
            VerifyFlowGraphAndDiagnosticsForTest<BlockSyntax>(source, expectedFlowGraph, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation, CompilerFeature.Dataflow)]
        [Fact]
        public void SwitchFlow_02()
        {
            string source = @"
public sealed class MyClass
{
    void M(bool result, int input)
    /*<bind>*/{
        switch (input)
        {
            default:
                result = true;
                break;
            case 1:
                result = false;
                break;
        }
    }/*</bind>*/
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
    CaptureIds: [0]
    Block[B1] - Block
        Predecessors: [B0]
        Statements (1)
            IFlowCaptureOperation: 0 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'input')
              Value: 
                IParameterReferenceOperation: input (OperationKind.ParameterReference, Type: System.Int32) (Syntax: 'input')

        Jump if False (Regular) to Block[B2]
            IBinaryOperation (BinaryOperatorKind.Equals) (OperationKind.Binary, Type: System.Boolean, IsImplicit) (Syntax: '1')
              Left: 
                IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: System.Int32, IsImplicit) (Syntax: 'input')
              Right: 
                ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 1) (Syntax: '1')

        Next (Regular) Block[B3]
    Block[B2] - Block
        Predecessors: [B1]
        Statements (1)
            IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'result = true;')
              Expression: 
                ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Boolean) (Syntax: 'result = true')
                  Left: 
                    IParameterReferenceOperation: result (OperationKind.ParameterReference, Type: System.Boolean) (Syntax: 'result')
                  Right: 
                    ILiteralOperation (OperationKind.Literal, Type: System.Boolean, Constant: True) (Syntax: 'true')

        Next (Regular) Block[B4]
            Leaving: {R1}
    Block[B3] - Block
        Predecessors: [B1]
        Statements (1)
            IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'result = false;')
              Expression: 
                ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Boolean) (Syntax: 'result = false')
                  Left: 
                    IParameterReferenceOperation: result (OperationKind.ParameterReference, Type: System.Boolean) (Syntax: 'result')
                  Right: 
                    ILiteralOperation (OperationKind.Literal, Type: System.Boolean, Constant: False) (Syntax: 'false')

        Next (Regular) Block[B4]
            Leaving: {R1}
}

Block[B4] - Exit
    Predecessors: [B2] [B3]
    Statements (0)
";
            VerifyFlowGraphAndDiagnosticsForTest<BlockSyntax>(source, expectedFlowGraph, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation, CompilerFeature.Dataflow)]
        [Fact]
        public void SwitchFlow_03()
        {
            string source = @"
public sealed class MyClass
{
    void M(bool result, int input)
    /*<bind>*/{
        switch (input)
        {
            case 1:
                result = false;
                break;
            default:
                result = true;
                break;
        }
    }/*</bind>*/
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
    CaptureIds: [0]
    Block[B1] - Block
        Predecessors: [B0]
        Statements (1)
            IFlowCaptureOperation: 0 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'input')
              Value: 
                IParameterReferenceOperation: input (OperationKind.ParameterReference, Type: System.Int32) (Syntax: 'input')

        Jump if False (Regular) to Block[B3]
            IBinaryOperation (BinaryOperatorKind.Equals) (OperationKind.Binary, Type: System.Boolean, IsImplicit) (Syntax: '1')
              Left: 
                IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: System.Int32, IsImplicit) (Syntax: 'input')
              Right: 
                ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 1) (Syntax: '1')

        Next (Regular) Block[B2]
    Block[B2] - Block
        Predecessors: [B1]
        Statements (1)
            IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'result = false;')
              Expression: 
                ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Boolean) (Syntax: 'result = false')
                  Left: 
                    IParameterReferenceOperation: result (OperationKind.ParameterReference, Type: System.Boolean) (Syntax: 'result')
                  Right: 
                    ILiteralOperation (OperationKind.Literal, Type: System.Boolean, Constant: False) (Syntax: 'false')

        Next (Regular) Block[B4]
            Leaving: {R1}
    Block[B3] - Block
        Predecessors: [B1]
        Statements (1)
            IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'result = true;')
              Expression: 
                ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Boolean) (Syntax: 'result = true')
                  Left: 
                    IParameterReferenceOperation: result (OperationKind.ParameterReference, Type: System.Boolean) (Syntax: 'result')
                  Right: 
                    ILiteralOperation (OperationKind.Literal, Type: System.Boolean, Constant: True) (Syntax: 'true')

        Next (Regular) Block[B4]
            Leaving: {R1}
}

Block[B4] - Exit
    Predecessors: [B2] [B3]
    Statements (0)
";
            VerifyFlowGraphAndDiagnosticsForTest<BlockSyntax>(source, expectedFlowGraph, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation, CompilerFeature.Dataflow)]
        [Fact]
        public void SwitchFlow_04()
        {
            string source = @"
public sealed class MyClass
{
    void M(bool result, int input)
    /*<bind>*/{
        switch (input)
        {
            case 1:
                result = false;
                break;
        }
    }/*</bind>*/
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
    CaptureIds: [0]
    Block[B1] - Block
        Predecessors: [B0]
        Statements (1)
            IFlowCaptureOperation: 0 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'input')
              Value: 
                IParameterReferenceOperation: input (OperationKind.ParameterReference, Type: System.Int32) (Syntax: 'input')

        Jump if False (Regular) to Block[B3]
            IBinaryOperation (BinaryOperatorKind.Equals) (OperationKind.Binary, Type: System.Boolean, IsImplicit) (Syntax: '1')
              Left: 
                IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: System.Int32, IsImplicit) (Syntax: 'input')
              Right: 
                ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 1) (Syntax: '1')
            Leaving: {R1}

        Next (Regular) Block[B2]
    Block[B2] - Block
        Predecessors: [B1]
        Statements (1)
            IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'result = false;')
              Expression: 
                ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Boolean) (Syntax: 'result = false')
                  Left: 
                    IParameterReferenceOperation: result (OperationKind.ParameterReference, Type: System.Boolean) (Syntax: 'result')
                  Right: 
                    ILiteralOperation (OperationKind.Literal, Type: System.Boolean, Constant: False) (Syntax: 'false')

        Next (Regular) Block[B3]
            Leaving: {R1}
}

Block[B3] - Exit
    Predecessors: [B1] [B2]
    Statements (0)
";
            VerifyFlowGraphAndDiagnosticsForTest<BlockSyntax>(source, expectedFlowGraph, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation, CompilerFeature.Dataflow)]
        [Fact]
        public void SwitchFlow_05()
        {
            string source = @"
public sealed class MyClass
{
    void M(bool result, int input)
    /*<bind>*/{
        switch (input)
        {
            case 0:
                bool x = true;
                result = x;
                break;
            case 1:
                result = false;
                break;
        }
    }/*</bind>*/
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
    CaptureIds: [0]
    Block[B1] - Block
        Predecessors: [B0]
        Statements (1)
            IFlowCaptureOperation: 0 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'input')
              Value: 
                IParameterReferenceOperation: input (OperationKind.ParameterReference, Type: System.Int32) (Syntax: 'input')

        Next (Regular) Block[B2]
            Entering: {R2}

    .locals {R2}
    {
        Locals: [System.Boolean x]
        Block[B2] - Block
            Predecessors: [B1]
            Statements (0)
            Jump if False (Regular) to Block[B4]
                IBinaryOperation (BinaryOperatorKind.Equals) (OperationKind.Binary, Type: System.Boolean, IsImplicit) (Syntax: '0')
                  Left: 
                    IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: System.Int32, IsImplicit) (Syntax: 'input')
                  Right: 
                    ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 0) (Syntax: '0')

            Next (Regular) Block[B3]
        Block[B3] - Block
            Predecessors: [B2]
            Statements (2)
                ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Boolean, IsImplicit) (Syntax: 'x = true')
                  Left: 
                    ILocalReferenceOperation: x (IsDeclaration: True) (OperationKind.LocalReference, Type: System.Boolean, IsImplicit) (Syntax: 'x = true')
                  Right: 
                    ILiteralOperation (OperationKind.Literal, Type: System.Boolean, Constant: True) (Syntax: 'true')

                IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'result = x;')
                  Expression: 
                    ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Boolean) (Syntax: 'result = x')
                      Left: 
                        IParameterReferenceOperation: result (OperationKind.ParameterReference, Type: System.Boolean) (Syntax: 'result')
                      Right: 
                        ILocalReferenceOperation: x (OperationKind.LocalReference, Type: System.Boolean) (Syntax: 'x')

            Next (Regular) Block[B6]
                Leaving: {R2} {R1}
        Block[B4] - Block
            Predecessors: [B2]
            Statements (0)
            Jump if False (Regular) to Block[B6]
                IBinaryOperation (BinaryOperatorKind.Equals) (OperationKind.Binary, Type: System.Boolean, IsImplicit) (Syntax: '1')
                  Left: 
                    IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: System.Int32, IsImplicit) (Syntax: 'input')
                  Right: 
                    ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 1) (Syntax: '1')
                Leaving: {R2} {R1}

            Next (Regular) Block[B5]
        Block[B5] - Block
            Predecessors: [B4]
            Statements (1)
                IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'result = false;')
                  Expression: 
                    ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Boolean) (Syntax: 'result = false')
                      Left: 
                        IParameterReferenceOperation: result (OperationKind.ParameterReference, Type: System.Boolean) (Syntax: 'result')
                      Right: 
                        ILiteralOperation (OperationKind.Literal, Type: System.Boolean, Constant: False) (Syntax: 'false')

            Next (Regular) Block[B6]
                Leaving: {R2} {R1}
    }
}

Block[B6] - Exit
    Predecessors: [B3] [B4] [B5]
    Statements (0)
";
            VerifyFlowGraphAndDiagnosticsForTest<BlockSyntax>(source, expectedFlowGraph, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation, CompilerFeature.Dataflow)]
        [Fact]
        public void SwitchFlow_06()
        {
            string source = @"
public sealed class MyClass
{
    void M(bool result, int input)
    /*<bind>*/{
        switch (input)
        {
            default:
            case 1:
                result = false;
                break;
        }
    }/*</bind>*/
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
    CaptureIds: [0]
    Block[B1] - Block
        Predecessors: [B0]
        Statements (1)
            IFlowCaptureOperation: 0 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'input')
              Value: 
                IParameterReferenceOperation: input (OperationKind.ParameterReference, Type: System.Int32) (Syntax: 'input')

        Jump if False (Regular) to Block[B2]
            IBinaryOperation (BinaryOperatorKind.Equals) (OperationKind.Binary, Type: System.Boolean, IsImplicit) (Syntax: '1')
              Left: 
                IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: System.Int32, IsImplicit) (Syntax: 'input')
              Right: 
                ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 1) (Syntax: '1')

        Next (Regular) Block[B2]
    Block[B2] - Block
        Predecessors: [B1*2]
        Statements (1)
            IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'result = false;')
              Expression: 
                ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Boolean) (Syntax: 'result = false')
                  Left: 
                    IParameterReferenceOperation: result (OperationKind.ParameterReference, Type: System.Boolean) (Syntax: 'result')
                  Right: 
                    ILiteralOperation (OperationKind.Literal, Type: System.Boolean, Constant: False) (Syntax: 'false')

        Next (Regular) Block[B3]
            Leaving: {R1}
}

Block[B3] - Exit
    Predecessors: [B2]
    Statements (0)
";
            VerifyFlowGraphAndDiagnosticsForTest<BlockSyntax>(source, expectedFlowGraph, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation, CompilerFeature.Dataflow)]
        [Fact]
        public void SwitchFlow_07()
        {
            string source = @"
public sealed class MyClass
{
    void M(bool result, int input)
    /*<bind>*/{
        switch (input)
        {
            case 1:
            default:
                result = false;
                break;
        }
    }/*</bind>*/
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
    CaptureIds: [0]
    Block[B1] - Block
        Predecessors: [B0]
        Statements (1)
            IFlowCaptureOperation: 0 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'input')
              Value: 
                IParameterReferenceOperation: input (OperationKind.ParameterReference, Type: System.Int32) (Syntax: 'input')

        Jump if False (Regular) to Block[B2]
            IBinaryOperation (BinaryOperatorKind.Equals) (OperationKind.Binary, Type: System.Boolean, IsImplicit) (Syntax: '1')
              Left: 
                IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: System.Int32, IsImplicit) (Syntax: 'input')
              Right: 
                ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 1) (Syntax: '1')

        Next (Regular) Block[B2]
    Block[B2] - Block
        Predecessors: [B1*2]
        Statements (1)
            IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'result = false;')
              Expression: 
                ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Boolean) (Syntax: 'result = false')
                  Left: 
                    IParameterReferenceOperation: result (OperationKind.ParameterReference, Type: System.Boolean) (Syntax: 'result')
                  Right: 
                    ILiteralOperation (OperationKind.Literal, Type: System.Boolean, Constant: False) (Syntax: 'false')

        Next (Regular) Block[B3]
            Leaving: {R1}
}

Block[B3] - Exit
    Predecessors: [B2]
    Statements (0)
";
            VerifyFlowGraphAndDiagnosticsForTest<BlockSyntax>(source, expectedFlowGraph, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation, CompilerFeature.Dataflow)]
        [Fact]
        public void SwitchFlow_08()
        {
            string source = @"
public sealed class MyClass
{
    void M(bool result, int input)
    /*<bind>*/{
        switch (input)
        {
            case 2:
            default:
            case 1:
                result = false;
                break;
        }
    }/*</bind>*/
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
    CaptureIds: [0]
    Block[B1] - Block
        Predecessors: [B0]
        Statements (1)
            IFlowCaptureOperation: 0 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'input')
              Value: 
                IParameterReferenceOperation: input (OperationKind.ParameterReference, Type: System.Int32) (Syntax: 'input')

        Jump if False (Regular) to Block[B2]
            IBinaryOperation (BinaryOperatorKind.Equals) (OperationKind.Binary, Type: System.Boolean, IsImplicit) (Syntax: '2')
              Left: 
                IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: System.Int32, IsImplicit) (Syntax: 'input')
              Right: 
                ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 2) (Syntax: '2')

        Next (Regular) Block[B3]
    Block[B2] - Block
        Predecessors: [B1]
        Statements (0)
        Jump if False (Regular) to Block[B3]
            IBinaryOperation (BinaryOperatorKind.Equals) (OperationKind.Binary, Type: System.Boolean, IsImplicit) (Syntax: '1')
              Left: 
                IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: System.Int32, IsImplicit) (Syntax: 'input')
              Right: 
                ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 1) (Syntax: '1')

        Next (Regular) Block[B3]
    Block[B3] - Block
        Predecessors: [B1] [B2*2]
        Statements (1)
            IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'result = false;')
              Expression: 
                ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Boolean) (Syntax: 'result = false')
                  Left: 
                    IParameterReferenceOperation: result (OperationKind.ParameterReference, Type: System.Boolean) (Syntax: 'result')
                  Right: 
                    ILiteralOperation (OperationKind.Literal, Type: System.Boolean, Constant: False) (Syntax: 'false')

        Next (Regular) Block[B4]
            Leaving: {R1}
}

Block[B4] - Exit
    Predecessors: [B3]
    Statements (0)
";
            VerifyFlowGraphAndDiagnosticsForTest<BlockSyntax>(source, expectedFlowGraph, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation, CompilerFeature.Dataflow)]
        [Fact]
        public void SwitchFlow_09()
        {
            string source = @"
public sealed class MyClass
{
    void M(bool result, int input)
    /*<bind>*/{
        switch (input)
        {
            case 1:
                goto case 3;
            case 2:
                goto default;
            case 3:
                result = true;
                break;
            default:
                result = false;
                break;
        }
    }/*</bind>*/
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
    CaptureIds: [0]
    Block[B1] - Block
        Predecessors: [B0]
        Statements (1)
            IFlowCaptureOperation: 0 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'input')
              Value: 
                IParameterReferenceOperation: input (OperationKind.ParameterReference, Type: System.Int32) (Syntax: 'input')

        Jump if False (Regular) to Block[B2]
            IBinaryOperation (BinaryOperatorKind.Equals) (OperationKind.Binary, Type: System.Boolean, IsImplicit) (Syntax: '1')
              Left: 
                IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: System.Int32, IsImplicit) (Syntax: 'input')
              Right: 
                ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 1) (Syntax: '1')

        Next (Regular) Block[B4]
    Block[B2] - Block
        Predecessors: [B1]
        Statements (0)
        Jump if False (Regular) to Block[B3]
            IBinaryOperation (BinaryOperatorKind.Equals) (OperationKind.Binary, Type: System.Boolean, IsImplicit) (Syntax: '2')
              Left: 
                IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: System.Int32, IsImplicit) (Syntax: 'input')
              Right: 
                ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 2) (Syntax: '2')

        Next (Regular) Block[B5]
    Block[B3] - Block
        Predecessors: [B2]
        Statements (0)
        Jump if False (Regular) to Block[B5]
            IBinaryOperation (BinaryOperatorKind.Equals) (OperationKind.Binary, Type: System.Boolean, IsImplicit) (Syntax: '3')
              Left: 
                IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: System.Int32, IsImplicit) (Syntax: 'input')
              Right: 
                ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 3) (Syntax: '3')

        Next (Regular) Block[B4]
    Block[B4] - Block
        Predecessors: [B1] [B3]
        Statements (1)
            IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'result = true;')
              Expression: 
                ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Boolean) (Syntax: 'result = true')
                  Left: 
                    IParameterReferenceOperation: result (OperationKind.ParameterReference, Type: System.Boolean) (Syntax: 'result')
                  Right: 
                    ILiteralOperation (OperationKind.Literal, Type: System.Boolean, Constant: True) (Syntax: 'true')

        Next (Regular) Block[B6]
            Leaving: {R1}
    Block[B5] - Block
        Predecessors: [B2] [B3]
        Statements (1)
            IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'result = false;')
              Expression: 
                ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Boolean) (Syntax: 'result = false')
                  Left: 
                    IParameterReferenceOperation: result (OperationKind.ParameterReference, Type: System.Boolean) (Syntax: 'result')
                  Right: 
                    ILiteralOperation (OperationKind.Literal, Type: System.Boolean, Constant: False) (Syntax: 'false')

        Next (Regular) Block[B6]
            Leaving: {R1}
}

Block[B6] - Exit
    Predecessors: [B4] [B5]
    Statements (0)
";
            VerifyFlowGraphAndDiagnosticsForTest<BlockSyntax>(source, expectedFlowGraph, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation, CompilerFeature.Dataflow)]
        [Fact]
        public void SwitchFlow_10()
        {
            string source = @"
public sealed class MyClass
{
    void M(bool result, int input)
    /*<bind>*/{
        switch (input)
        {
            case 1:
                goto case 3;
        }
    }/*</bind>*/
}
";
            var expectedDiagnostics = new[] {
                // file.cs(9,17): error CS0159: No such label 'case 3:' within the scope of the goto statement
                //                 goto case 3;
                Diagnostic(ErrorCode.ERR_LabelNotFound, "goto case 3;").WithArguments("case 3:").WithLocation(9, 17),
                // file.cs(8,13): error CS8070: Control cannot fall out of switch from final case label ('case 1:')
                //             case 1:
                Diagnostic(ErrorCode.ERR_SwitchFallOut, "case 1:").WithArguments("case 1:").WithLocation(8, 13)
            };

            string expectedFlowGraph = @"
Block[B0] - Entry
    Statements (0)
    Next (Regular) Block[B1]
        Entering: {R1}

.locals {R1}
{
    CaptureIds: [0]
    Block[B1] - Block
        Predecessors: [B0]
        Statements (1)
            IFlowCaptureOperation: 0 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'input')
              Value: 
                IParameterReferenceOperation: input (OperationKind.ParameterReference, Type: System.Int32) (Syntax: 'input')

        Jump if False (Regular) to Block[B3]
            IBinaryOperation (BinaryOperatorKind.Equals) (OperationKind.Binary, Type: System.Boolean, IsInvalid, IsImplicit) (Syntax: '1')
              Left: 
                IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: System.Int32, IsImplicit) (Syntax: 'input')
              Right: 
                ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 1, IsInvalid) (Syntax: '1')
            Leaving: {R1}

        Next (Regular) Block[B2]
    Block[B2] - Block
        Predecessors: [B1]
        Statements (2)
            ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 3, IsInvalid) (Syntax: '3')

            IInvalidOperation (OperationKind.Invalid, Type: null, IsInvalid) (Syntax: 'goto case 3;')
              Children(0)

        Next (Regular) Block[B3]
            Leaving: {R1}
}

Block[B3] - Exit
    Predecessors: [B1] [B2]
    Statements (0)
";
            VerifyFlowGraphAndDiagnosticsForTest<BlockSyntax>(source, expectedFlowGraph, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation, CompilerFeature.Dataflow)]
        [Fact]
        public void SwitchFlow_11()
        {
            string source = @"
public sealed class MyClass
{
    void M(bool result, long input)
    /*<bind>*/{
        switch (input)
        {
            default:
                result = false;
                break;
            case 1:
                result = result;
                break;
            default:
                result = true;
                break;
        }
    }/*</bind>*/
}
";
            var expectedDiagnostics = new[] {
                // file.cs(14,13): error CS0152: The switch statement contains multiple cases with the label value 'default'
                //             default:
                Diagnostic(ErrorCode.ERR_DuplicateCaseLabel, "default:").WithArguments("default").WithLocation(14, 13),
                // file.cs(12,17): warning CS1717: Assignment made to same variable; did you mean to assign something else?
                //                 result = result;
                Diagnostic(ErrorCode.WRN_AssignmentToSelf, "result = result").WithLocation(12, 17)
            };

            string expectedFlowGraph = @"
Block[B0] - Entry
    Statements (0)
    Next (Regular) Block[B1]
        Entering: {R1}

.locals {R1}
{
    CaptureIds: [0]
    Block[B1] - Block
        Predecessors: [B0]
        Statements (1)
            IFlowCaptureOperation: 0 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'input')
              Value: 
                IParameterReferenceOperation: input (OperationKind.ParameterReference, Type: System.Int64) (Syntax: 'input')

        Jump if False (Regular) to Block[B2]
            IBinaryOperation (BinaryOperatorKind.Equals) (OperationKind.Binary, Type: System.Boolean, IsImplicit) (Syntax: '1')
              Left: 
                IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: System.Int64, IsImplicit) (Syntax: 'input')
              Right: 
                IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.Int64, Constant: 1, IsImplicit) (Syntax: '1')
                  Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: True, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                    (ImplicitNumeric)
                  Operand: 
                    ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 1) (Syntax: '1')

        Next (Regular) Block[B3]
    Block[B2] - Block
        Predecessors: [B1]
        Statements (1)
            IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'result = false;')
              Expression: 
                ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Boolean) (Syntax: 'result = false')
                  Left: 
                    IParameterReferenceOperation: result (OperationKind.ParameterReference, Type: System.Boolean) (Syntax: 'result')
                  Right: 
                    ILiteralOperation (OperationKind.Literal, Type: System.Boolean, Constant: False) (Syntax: 'false')

        Next (Regular) Block[B5]
            Leaving: {R1}
    Block[B3] - Block
        Predecessors: [B1]
        Statements (1)
            IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'result = result;')
              Expression: 
                ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Boolean) (Syntax: 'result = result')
                  Left: 
                    IParameterReferenceOperation: result (OperationKind.ParameterReference, Type: System.Boolean) (Syntax: 'result')
                  Right: 
                    IParameterReferenceOperation: result (OperationKind.ParameterReference, Type: System.Boolean) (Syntax: 'result')

        Next (Regular) Block[B5]
            Leaving: {R1}
    Block[B4] - Block [UnReachable]
        Predecessors (0)
        Statements (1)
            IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'result = true;')
              Expression: 
                ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Boolean) (Syntax: 'result = true')
                  Left: 
                    IParameterReferenceOperation: result (OperationKind.ParameterReference, Type: System.Boolean) (Syntax: 'result')
                  Right: 
                    ILiteralOperation (OperationKind.Literal, Type: System.Boolean, Constant: True) (Syntax: 'true')

        Next (Regular) Block[B5]
            Leaving: {R1}
}

Block[B5] - Exit
    Predecessors: [B2] [B3] [B4]
    Statements (0)
";
            VerifyFlowGraphAndDiagnosticsForTest<BlockSyntax>(source, expectedFlowGraph, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation, CompilerFeature.Dataflow)]
        [Fact]
        public void SwitchFlow_12()
        {
            string source = @"
public sealed class MyClass
{
    void M(bool result, int input)
    /*<bind>*/{
        switch (input)
        {
            case 1L:
                result = false;
                break;
        }
    }/*</bind>*/
}
";
            var expectedDiagnostics = new[] {
                // file.cs(8,18): error CS0266: Cannot implicitly convert type 'long' to 'int'. An explicit conversion exists (are you missing a cast?)
                //             case 1L:
                Diagnostic(ErrorCode.ERR_NoImplicitConvCast, "1L").WithArguments("long", "int").WithLocation(8, 18)
            };

            string expectedFlowGraph = @"
Block[B0] - Entry
    Statements (0)
    Next (Regular) Block[B1]
        Entering: {R1}

.locals {R1}
{
    CaptureIds: [0]
    Block[B1] - Block
        Predecessors: [B0]
        Statements (1)
            IFlowCaptureOperation: 0 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'input')
              Value: 
                IParameterReferenceOperation: input (OperationKind.ParameterReference, Type: System.Int32) (Syntax: 'input')

        Jump if False (Regular) to Block[B3]
            IBinaryOperation (BinaryOperatorKind.Equals) (OperationKind.Binary, Type: System.Boolean, IsInvalid, IsImplicit) (Syntax: '1L')
              Left: 
                IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: System.Int32, IsImplicit) (Syntax: 'input')
              Right: 
                IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.Int32, Constant: 1, IsInvalid, IsImplicit) (Syntax: '1L')
                  Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: True, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                    (ExplicitNumeric)
                  Operand: 
                    ILiteralOperation (OperationKind.Literal, Type: System.Int64, Constant: 1, IsInvalid) (Syntax: '1L')
            Leaving: {R1}

        Next (Regular) Block[B2]
    Block[B2] - Block
        Predecessors: [B1]
        Statements (1)
            IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'result = false;')
              Expression: 
                ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Boolean) (Syntax: 'result = false')
                  Left: 
                    IParameterReferenceOperation: result (OperationKind.ParameterReference, Type: System.Boolean) (Syntax: 'result')
                  Right: 
                    ILiteralOperation (OperationKind.Literal, Type: System.Boolean, Constant: False) (Syntax: 'false')

        Next (Regular) Block[B3]
            Leaving: {R1}
}

Block[B3] - Exit
    Predecessors: [B1] [B2]
    Statements (0)
";
            VerifyFlowGraphAndDiagnosticsForTest<BlockSyntax>(source, expectedFlowGraph, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation, CompilerFeature.Dataflow)]
        [Fact]
        public void SwitchFlow_13()
        {
            string source = @"
public sealed class MyClass
{
    void M(bool result, MyClass input, MyClass other)
    /*<bind>*/{
        switch (input)
        {
            case other:
                result = false;
                break;
        }
    }/*</bind>*/

    public static bool operator ==(MyClass x, MyClass y) => false;
    public static bool operator !=(MyClass x, MyClass y) => true;
    public override bool Equals(object obj)
    {
        return base.Equals(obj);
    }
    public override int GetHashCode()
    {
        return base.GetHashCode();
    }
}
";
            var expectedDiagnostics = new[] {
                // file.cs(8,18): error CS0150: A constant value is expected
                //             case other:
                Diagnostic(ErrorCode.ERR_ConstantExpected, "other").WithLocation(8, 18)
            };

            string expectedFlowGraph = @"
Block[B0] - Entry
    Statements (0)
    Next (Regular) Block[B1]
        Entering: {R1}

.locals {R1}
{
    CaptureIds: [0]
    Block[B1] - Block
        Predecessors: [B0]
        Statements (1)
            IFlowCaptureOperation: 0 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'input')
              Value: 
                IParameterReferenceOperation: input (OperationKind.ParameterReference, Type: MyClass) (Syntax: 'input')

        Jump if False (Regular) to Block[B3]
            IIsPatternOperation (OperationKind.IsPattern, Type: System.Boolean, IsInvalid, IsImplicit) (Syntax: 'case other:')
              Value: 
                IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: MyClass, IsImplicit) (Syntax: 'input')
              Pattern: 
                IConstantPatternOperation (OperationKind.ConstantPattern, Type: null, IsInvalid, IsImplicit) (Syntax: 'case other:') (InputType: MyClass)
                  Value: 
                    IParameterReferenceOperation: other (OperationKind.ParameterReference, Type: MyClass, IsInvalid) (Syntax: 'other')
            Leaving: {R1}

        Next (Regular) Block[B2]
    Block[B2] - Block
        Predecessors: [B1]
        Statements (1)
            IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'result = false;')
              Expression: 
                ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Boolean) (Syntax: 'result = false')
                  Left: 
                    IParameterReferenceOperation: result (OperationKind.ParameterReference, Type: System.Boolean) (Syntax: 'result')
                  Right: 
                    ILiteralOperation (OperationKind.Literal, Type: System.Boolean, Constant: False) (Syntax: 'false')

        Next (Regular) Block[B3]
            Leaving: {R1}
}

Block[B3] - Exit
    Predecessors: [B1] [B2]
    Statements (0)
";
            VerifyFlowGraphAndDiagnosticsForTest<BlockSyntax>(source, expectedFlowGraph, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation, CompilerFeature.Dataflow)]
        [Fact]
        public void SwitchFlow_14()
        {
            string source = @"
public sealed class MyClass
{
    void M(bool result, MyClass input)
    /*<bind>*/{
        switch (input)
        {
            case 1:
                result = false;
                break;
        }
    }/*</bind>*/

    public static bool operator ==(MyClass x, long y) => false;
    public static bool operator !=(MyClass x, long y) => true;
    public override bool Equals(object obj)
    {
        return base.Equals(obj);
    }
    public override int GetHashCode()
    {
        return base.GetHashCode();
    }
}
";
            var expectedDiagnostics = new[] {
                // file.cs(8,18): error CS0029: Cannot implicitly convert type 'int' to 'MyClass'
                //             case 1:
                Diagnostic(ErrorCode.ERR_NoImplicitConv, "1").WithArguments("int", "MyClass").WithLocation(8, 18)
            };

            string expectedFlowGraph = @"
Block[B0] - Entry
    Statements (0)
    Next (Regular) Block[B1]
        Entering: {R1}

.locals {R1}
{
    CaptureIds: [0]
    Block[B1] - Block
        Predecessors: [B0]
        Statements (1)
            IFlowCaptureOperation: 0 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'input')
              Value: 
                IParameterReferenceOperation: input (OperationKind.ParameterReference, Type: MyClass) (Syntax: 'input')

        Jump if False (Regular) to Block[B3]
            IIsPatternOperation (OperationKind.IsPattern, Type: System.Boolean, IsInvalid, IsImplicit) (Syntax: 'case 1:')
              Value: 
                IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: MyClass, IsImplicit) (Syntax: 'input')
              Pattern: 
                IConstantPatternOperation (OperationKind.ConstantPattern, Type: null, IsInvalid, IsImplicit) (Syntax: 'case 1:') (InputType: MyClass)
                  Value: 
                    IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: MyClass, IsInvalid, IsImplicit) (Syntax: '1')
                      Conversion: CommonConversion (Exists: False, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                        (NoConversion)
                      Operand: 
                        ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 1, IsInvalid) (Syntax: '1')
            Leaving: {R1}

        Next (Regular) Block[B2]
    Block[B2] - Block
        Predecessors: [B1]
        Statements (1)
            IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'result = false;')
              Expression: 
                ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Boolean) (Syntax: 'result = false')
                  Left: 
                    IParameterReferenceOperation: result (OperationKind.ParameterReference, Type: System.Boolean) (Syntax: 'result')
                  Right: 
                    ILiteralOperation (OperationKind.Literal, Type: System.Boolean, Constant: False) (Syntax: 'false')

        Next (Regular) Block[B3]
            Leaving: {R1}
}

Block[B3] - Exit
    Predecessors: [B1] [B2]
    Statements (0)
";
            VerifyFlowGraphAndDiagnosticsForTest<BlockSyntax>(source, expectedFlowGraph, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation, CompilerFeature.Dataflow)]
        [Fact]
        public void SwitchFlow_15()
        {
            string source = @"
public sealed class MyClass
{
    void M(bool result, MyClass input)
    /*<bind>*/{
        switch (input)
        {
            case 1:
                result = false;
                break;
        }
    }/*</bind>*/

    public static implicit operator MyClass(long x) => null;
}
";
            var expectedDiagnostics = new[] {
                // file.cs(8,18): error CS0150: A constant value is expected
                //             case 1:
                Diagnostic(ErrorCode.ERR_ConstantExpected, "1").WithLocation(8, 18)
            };

            string expectedFlowGraph = @"
Block[B0] - Entry
    Statements (0)
    Next (Regular) Block[B1]
        Entering: {R1}

.locals {R1}
{
    CaptureIds: [0]
    Block[B1] - Block
        Predecessors: [B0]
        Statements (1)
            IFlowCaptureOperation: 0 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'input')
              Value: 
                IParameterReferenceOperation: input (OperationKind.ParameterReference, Type: MyClass) (Syntax: 'input')

        Jump if False (Regular) to Block[B3]
            IIsPatternOperation (OperationKind.IsPattern, Type: System.Boolean, IsInvalid, IsImplicit) (Syntax: 'case 1:')
              Value: 
                IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: MyClass, IsImplicit) (Syntax: 'input')
              Pattern: 
                IConstantPatternOperation (OperationKind.ConstantPattern, Type: null, IsInvalid, IsImplicit) (Syntax: 'case 1:') (InputType: MyClass)
                  Value: 
                    IConversionOperation (TryCast: False, Unchecked) (OperatorMethod: MyClass MyClass.op_Implicit(System.Int64 x)) (OperationKind.Conversion, Type: MyClass, IsInvalid, IsImplicit) (Syntax: '1')
                      Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: True) (MethodSymbol: MyClass MyClass.op_Implicit(System.Int64 x))
                        (ImplicitUserDefined)
                      Operand: 
                        IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.Int64, Constant: 1, IsInvalid, IsImplicit) (Syntax: '1')
                          Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: True, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                            (ImplicitNumeric)
                          Operand: 
                            ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 1, IsInvalid) (Syntax: '1')
            Leaving: {R1}

        Next (Regular) Block[B2]
    Block[B2] - Block
        Predecessors: [B1]
        Statements (1)
            IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'result = false;')
              Expression: 
                ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Boolean) (Syntax: 'result = false')
                  Left: 
                    IParameterReferenceOperation: result (OperationKind.ParameterReference, Type: System.Boolean) (Syntax: 'result')
                  Right: 
                    ILiteralOperation (OperationKind.Literal, Type: System.Boolean, Constant: False) (Syntax: 'false')

        Next (Regular) Block[B3]
            Leaving: {R1}
}

Block[B3] - Exit
    Predecessors: [B1] [B2]
    Statements (0)
";
            VerifyFlowGraphAndDiagnosticsForTest<BlockSyntax>(source, expectedFlowGraph, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation, CompilerFeature.Dataflow)]
        [Fact]
        public void SwitchFlow_16()
        {
            string source = @"
public sealed class MyClass
{
    void M(bool result)
    /*<bind>*/{
            case 1:
                result = false;
        }
    }/*</bind>*/
}
";
            var expectedDiagnostics = new[] {
                // file.cs(5,16): error CS1513: } expected
                //     /*<bind>*/{
                Diagnostic(ErrorCode.ERR_RbraceExpected, "").WithLocation(5, 16),
                // file.cs(6,19): error CS1002: ; expected
                //             case 1:
                Diagnostic(ErrorCode.ERR_SemicolonExpected, ":").WithLocation(6, 19),
                // file.cs(6,19): error CS1513: } expected
                //             case 1:
                Diagnostic(ErrorCode.ERR_RbraceExpected, ":").WithLocation(6, 19),
                // file.cs(10,1): error CS1022: Type or namespace definition, or end-of-file expected
                // }
                Diagnostic(ErrorCode.ERR_EOFExpected, "}").WithLocation(10, 1)
            };

            string expectedFlowGraph = @"
Block[B0] - Entry
    Statements (0)
    Next (Regular) Block[B1]
Block[B1] - Block
    Predecessors: [B0]
    Statements (2)
        IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null, IsInvalid) (Syntax: '1')
          Expression: 
            ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 1, IsInvalid) (Syntax: '1')

        IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'result = false;')
          Expression: 
            ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Boolean) (Syntax: 'result = false')
              Left: 
                IParameterReferenceOperation: result (OperationKind.ParameterReference, Type: System.Boolean) (Syntax: 'result')
              Right: 
                ILiteralOperation (OperationKind.Literal, Type: System.Boolean, Constant: False) (Syntax: 'false')

    Next (Regular) Block[B2]
Block[B2] - Exit
    Predecessors: [B1]
    Statements (0)
";
            VerifyFlowGraphAndDiagnosticsForTest<BlockSyntax>(source, expectedFlowGraph, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation, CompilerFeature.Dataflow)]
        [Fact]
        public void SwitchFlow_17()
        {
            string source = @"
public sealed class MyClass
{
    void M(bool result)
    /*<bind>*/{
            default:
                result = false;
        }
    }/*</bind>*/
}
";
            var expectedDiagnostics = new[] {
                // file.cs(6,13): error CS8716: There is no target type for the default literal.
                //             default:
                Diagnostic(ErrorCode.ERR_DefaultLiteralNoTargetType, "default").WithLocation(6, 13),
                // file.cs(6,20): error CS1002: ; expected
                //             default:
                Diagnostic(ErrorCode.ERR_SemicolonExpected, ":").WithLocation(6, 20),
                // file.cs(6,20): error CS1513: } expected
                //             default:
                Diagnostic(ErrorCode.ERR_RbraceExpected, ":").WithLocation(6, 20),
                // file.cs(10,1): error CS1022: Type or namespace definition, or end-of-file expected
                // }
                Diagnostic(ErrorCode.ERR_EOFExpected, "}").WithLocation(10, 1)
            };

            string expectedFlowGraph = @"
Block[B0] - Entry
    Statements (0)
    Next (Regular) Block[B1]
Block[B1] - Block
    Predecessors: [B0]
    Statements (2)
        IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null, IsInvalid) (Syntax: 'default')
          Expression: 
            IDefaultValueOperation (OperationKind.DefaultValue, Type: ?, IsInvalid) (Syntax: 'default')
        IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'result = false;')
          Expression: 
            ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Boolean) (Syntax: 'result = false')
              Left: 
                IParameterReferenceOperation: result (OperationKind.ParameterReference, Type: System.Boolean) (Syntax: 'result')
              Right: 
                ILiteralOperation (OperationKind.Literal, Type: System.Boolean, Constant: False) (Syntax: 'false')
    Next (Regular) Block[B2]
Block[B2] - Exit
    Predecessors: [B1]
    Statements (0)
";
            VerifyFlowGraphAndDiagnosticsForTest<BlockSyntax>(source, expectedFlowGraph, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation, CompilerFeature.Dataflow)]
        [Fact]
        public void SwitchFlow_18()
        {
            string source = @"
public sealed class MyClass
{
    void M(bool result, int? input)
    /*<bind>*/{
        switch (input)
        {
            case 1:
                result = false;
                break;
        }
    }/*</bind>*/
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
    CaptureIds: [0]
    Block[B1] - Block
        Predecessors: [B0]
        Statements (1)
            IFlowCaptureOperation: 0 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'input')
              Value: 
                IParameterReferenceOperation: input (OperationKind.ParameterReference, Type: System.Int32?) (Syntax: 'input')

        Jump if False (Regular) to Block[B3]
            IBinaryOperation (BinaryOperatorKind.Equals, IsLifted) (OperationKind.Binary, Type: System.Boolean, IsImplicit) (Syntax: '1')
              Left: 
                IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: System.Int32?, IsImplicit) (Syntax: 'input')
              Right: 
                IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.Int32?, IsImplicit) (Syntax: '1')
                  Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                    (ImplicitNullable)
                  Operand: 
                    ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 1) (Syntax: '1')
            Leaving: {R1}

        Next (Regular) Block[B2]
    Block[B2] - Block
        Predecessors: [B1]
        Statements (1)
            IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'result = false;')
              Expression: 
                ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Boolean) (Syntax: 'result = false')
                  Left: 
                    IParameterReferenceOperation: result (OperationKind.ParameterReference, Type: System.Boolean) (Syntax: 'result')
                  Right: 
                    ILiteralOperation (OperationKind.Literal, Type: System.Boolean, Constant: False) (Syntax: 'false')

        Next (Regular) Block[B3]
            Leaving: {R1}
}

Block[B3] - Exit
    Predecessors: [B1] [B2]
    Statements (0)
";
            VerifyFlowGraphAndDiagnosticsForTest<BlockSyntax>(source, expectedFlowGraph, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation, CompilerFeature.Dataflow)]
        [Fact]
        public void SwitchFlow_19()
        {
            string source = @"
public sealed class MyClass
{
    void M(bool result, int? input)
    /*<bind>*/{
        switch (input)
        {
            case null:
                result = false;
                break;
        }
    }/*</bind>*/
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
    CaptureIds: [0]
    Block[B1] - Block
        Predecessors: [B0]
        Statements (1)
            IFlowCaptureOperation: 0 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'input')
              Value: 
                IParameterReferenceOperation: input (OperationKind.ParameterReference, Type: System.Int32?) (Syntax: 'input')
        Jump if False (Regular) to Block[B3]
            IBinaryOperation (BinaryOperatorKind.Equals, IsLifted) (OperationKind.Binary, Type: System.Boolean, IsImplicit) (Syntax: 'null')
              Left: 
                IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: System.Int32?, IsImplicit) (Syntax: 'input')
              Right: 
                IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.Int32?, Constant: null, IsImplicit) (Syntax: 'null')
                  Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                    (NullLiteral)
                  Operand: 
                    ILiteralOperation (OperationKind.Literal, Type: null, Constant: null) (Syntax: 'null')
            Leaving: {R1}
        Next (Regular) Block[B2]
    Block[B2] - Block
        Predecessors: [B1]
        Statements (1)
            IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'result = false;')
              Expression: 
                ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Boolean) (Syntax: 'result = false')
                  Left: 
                    IParameterReferenceOperation: result (OperationKind.ParameterReference, Type: System.Boolean) (Syntax: 'result')
                  Right: 
                    ILiteralOperation (OperationKind.Literal, Type: System.Boolean, Constant: False) (Syntax: 'false')
        Next (Regular) Block[B3]
            Leaving: {R1}
}
Block[B3] - Exit
    Predecessors: [B1] [B2]
    Statements (0)
";
            VerifyFlowGraphAndDiagnosticsForTest<BlockSyntax>(source, expectedFlowGraph, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation, CompilerFeature.Dataflow)]
        [Fact]
        public void SwitchFlow_20()
        {
            string source = @"
public sealed class MyClass
{
    void M(bool result, int? input, int? other)
    /*<bind>*/{
        switch (input)
        {
            case other:
                result = false;
                break;
        }
    }/*</bind>*/
}
";
            var expectedDiagnostics = new[] {
                // file.cs(8,18): error CS0150: A constant value is expected
                //             case other:
                Diagnostic(ErrorCode.ERR_ConstantExpected, "other").WithLocation(8, 18)
            };

            string expectedFlowGraph = @"
Block[B0] - Entry
    Statements (0)
    Next (Regular) Block[B1]
        Entering: {R1}

.locals {R1}
{
    CaptureIds: [0]
    Block[B1] - Block
        Predecessors: [B0]
        Statements (1)
            IFlowCaptureOperation: 0 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'input')
              Value: 
                IParameterReferenceOperation: input (OperationKind.ParameterReference, Type: System.Int32?) (Syntax: 'input')

        Jump if False (Regular) to Block[B3]
            IBinaryOperation (BinaryOperatorKind.Equals, IsLifted) (OperationKind.Binary, Type: System.Boolean, IsInvalid, IsImplicit) (Syntax: 'other')
              Left: 
                IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: System.Int32?, IsImplicit) (Syntax: 'input')
              Right: 
                IParameterReferenceOperation: other (OperationKind.ParameterReference, Type: System.Int32?, IsInvalid) (Syntax: 'other')
            Leaving: {R1}

        Next (Regular) Block[B2]
    Block[B2] - Block
        Predecessors: [B1]
        Statements (1)
            IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'result = false;')
              Expression: 
                ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Boolean) (Syntax: 'result = false')
                  Left: 
                    IParameterReferenceOperation: result (OperationKind.ParameterReference, Type: System.Boolean) (Syntax: 'result')
                  Right: 
                    ILiteralOperation (OperationKind.Literal, Type: System.Boolean, Constant: False) (Syntax: 'false')

        Next (Regular) Block[B3]
            Leaving: {R1}
}

Block[B3] - Exit
    Predecessors: [B1] [B2]
    Statements (0)
";
            VerifyFlowGraphAndDiagnosticsForTest<BlockSyntax>(source, expectedFlowGraph, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation, CompilerFeature.Dataflow)]
        [Fact]
        public void SwitchFlow_21()
        {
            string source = @"
public sealed class MyClass
{
    void M(bool result, int input, int? other)
    /*<bind>*/{
        switch (input)
        {
            case other:
                result = false;
                break;
        }
    }/*</bind>*/
}
";
            var expectedDiagnostics = new[] {
                // file.cs(8,18): error CS0266: Cannot implicitly convert type 'int?' to 'int'. An explicit conversion exists (are you missing a cast?)
                //             case other:
                Diagnostic(ErrorCode.ERR_NoImplicitConvCast, "other").WithArguments("int?", "int").WithLocation(8, 18),
                // file.cs(8,18): error CS0150: A constant value is expected
                //             case other:
                Diagnostic(ErrorCode.ERR_ConstantExpected, "other").WithLocation(8, 18)
            };

            string expectedFlowGraph = @"
Block[B0] - Entry
    Statements (0)
    Next (Regular) Block[B1]
        Entering: {R1}

.locals {R1}
{
    CaptureIds: [0]
    Block[B1] - Block
        Predecessors: [B0]
        Statements (1)
            IFlowCaptureOperation: 0 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'input')
              Value: 
                IParameterReferenceOperation: input (OperationKind.ParameterReference, Type: System.Int32) (Syntax: 'input')

        Jump if False (Regular) to Block[B3]
            IBinaryOperation (BinaryOperatorKind.Equals) (OperationKind.Binary, Type: System.Boolean, IsInvalid, IsImplicit) (Syntax: 'other')
              Left: 
                IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: System.Int32, IsImplicit) (Syntax: 'input')
              Right: 
                IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.Int32, IsInvalid, IsImplicit) (Syntax: 'other')
                  Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                    (ExplicitNullable)
                  Operand: 
                    IParameterReferenceOperation: other (OperationKind.ParameterReference, Type: System.Int32?, IsInvalid) (Syntax: 'other')
            Leaving: {R1}

        Next (Regular) Block[B2]
    Block[B2] - Block
        Predecessors: [B1]
        Statements (1)
            IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'result = false;')
              Expression: 
                ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Boolean) (Syntax: 'result = false')
                  Left: 
                    IParameterReferenceOperation: result (OperationKind.ParameterReference, Type: System.Boolean) (Syntax: 'result')
                  Right: 
                    ILiteralOperation (OperationKind.Literal, Type: System.Boolean, Constant: False) (Syntax: 'false')

        Next (Regular) Block[B3]
            Leaving: {R1}
}

Block[B3] - Exit
    Predecessors: [B1] [B2]
    Statements (0)
";
            VerifyFlowGraphAndDiagnosticsForTest<BlockSyntax>(source, expectedFlowGraph, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation, CompilerFeature.Dataflow)]
        [Fact]
        public void SwitchFlow_22()
        {
            string source = @"
public sealed class MyClass
{
    void M(bool result, dynamic input)
    /*<bind>*/{
        switch (input)
        {
            case 1:
                result = false;
                break;
        }
    }/*</bind>*/
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
    CaptureIds: [0]
    Block[B1] - Block
        Predecessors: [B0]
        Statements (1)
            IFlowCaptureOperation: 0 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'input')
              Value: 
                IParameterReferenceOperation: input (OperationKind.ParameterReference, Type: dynamic) (Syntax: 'input')

        Jump if False (Regular) to Block[B3]
            IIsPatternOperation (OperationKind.IsPattern, Type: System.Boolean, IsImplicit) (Syntax: 'case 1:')
              Value: 
                IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: dynamic, IsImplicit) (Syntax: 'input')
              Pattern: 
                IConstantPatternOperation (OperationKind.ConstantPattern, Type: null, IsImplicit) (Syntax: 'case 1:') (InputType: dynamic)
                  Value: 
                    ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 1) (Syntax: '1')
            Leaving: {R1}

        Next (Regular) Block[B2]
    Block[B2] - Block
        Predecessors: [B1]
        Statements (1)
            IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'result = false;')
              Expression: 
                ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Boolean) (Syntax: 'result = false')
                  Left: 
                    IParameterReferenceOperation: result (OperationKind.ParameterReference, Type: System.Boolean) (Syntax: 'result')
                  Right: 
                    ILiteralOperation (OperationKind.Literal, Type: System.Boolean, Constant: False) (Syntax: 'false')

        Next (Regular) Block[B3]
            Leaving: {R1}
}

Block[B3] - Exit
    Predecessors: [B1] [B2]
    Statements (0)
";
            VerifyFlowGraphAndDiagnosticsForTest<BlockSyntax>(source, expectedFlowGraph, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation, CompilerFeature.Dataflow)]
        [Fact]
        public void SwitchIOperation_022()
        {
            string source = @"
public sealed class MyClass
{
    void M(bool result, dynamic input)
    {
        /*<bind>*/switch (input)
        {
            case 1:
                result = false;
                break;
        }/*</bind>*/
    }
}
";
            var expectedDiagnostics = DiagnosticDescription.None;
            var expectedOperationTree =
@"
ISwitchOperation (1 cases, Exit Label Id: 0) (OperationKind.Switch, Type: null) (Syntax: 'switch (inp ... }')
  Switch expression: 
    IParameterReferenceOperation: input (OperationKind.ParameterReference, Type: dynamic) (Syntax: 'input')
  Sections:
      ISwitchCaseOperation (1 case clauses, 2 statements) (OperationKind.SwitchCase, Type: null) (Syntax: 'case 1: ... break;')
          Clauses:
              IPatternCaseClauseOperation (Label Id: 1) (CaseKind.Pattern) (OperationKind.CaseClause, Type: null) (Syntax: 'case 1:')
                Pattern: 
                  IConstantPatternOperation (OperationKind.ConstantPattern, Type: null, IsImplicit) (Syntax: 'case 1:') (InputType: dynamic)
                    Value: 
                      ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 1) (Syntax: '1')
          Body:
              IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'result = false;')
                Expression: 
                  ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Boolean) (Syntax: 'result = false')
                    Left: 
                      IParameterReferenceOperation: result (OperationKind.ParameterReference, Type: System.Boolean) (Syntax: 'result')
                    Right: 
                      ILiteralOperation (OperationKind.Literal, Type: System.Boolean, Constant: False) (Syntax: 'false')
              IBranchOperation (BranchKind.Break, Label Id: 0) (OperationKind.Branch, Type: null) (Syntax: 'break;')
";
            VerifyOperationTreeAndDiagnosticsForTest<SwitchStatementSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation, CompilerFeature.Dataflow)]
        [Fact]
        public void SwitchFlow_23()
        {
            string source = @"
public sealed class MyClass
{
    void M(bool result, dynamic input, dynamic other)
    /*<bind>*/{
        switch (input)
        {
            case other:
                result = false;
                break;
        }
    }/*</bind>*/
}
";
            var expectedDiagnostics = new[] {
                // file.cs(8,18): error CS0150: A constant value is expected
                //             case other:
                Diagnostic(ErrorCode.ERR_ConstantExpected, "other").WithLocation(8, 18)
            };

            string expectedFlowGraph = @"
Block[B0] - Entry
    Statements (0)
    Next (Regular) Block[B1]
        Entering: {R1}

.locals {R1}
{
    CaptureIds: [0]
    Block[B1] - Block
        Predecessors: [B0]
        Statements (1)
            IFlowCaptureOperation: 0 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'input')
              Value: 
                IParameterReferenceOperation: input (OperationKind.ParameterReference, Type: dynamic) (Syntax: 'input')

        Jump if False (Regular) to Block[B3]
            IIsPatternOperation (OperationKind.IsPattern, Type: System.Boolean, IsInvalid, IsImplicit) (Syntax: 'case other:')
              Value: 
                IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: dynamic, IsImplicit) (Syntax: 'input')
              Pattern: 
                IConstantPatternOperation (OperationKind.ConstantPattern, Type: null, IsInvalid, IsImplicit) (Syntax: 'case other:') (InputType: dynamic)
                  Value: 
                    IParameterReferenceOperation: other (OperationKind.ParameterReference, Type: dynamic, IsInvalid) (Syntax: 'other')
            Leaving: {R1}

        Next (Regular) Block[B2]
    Block[B2] - Block
        Predecessors: [B1]
        Statements (1)
            IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'result = false;')
              Expression: 
                ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Boolean) (Syntax: 'result = false')
                  Left: 
                    IParameterReferenceOperation: result (OperationKind.ParameterReference, Type: System.Boolean) (Syntax: 'result')
                  Right: 
                    ILiteralOperation (OperationKind.Literal, Type: System.Boolean, Constant: False) (Syntax: 'false')

        Next (Regular) Block[B3]
            Leaving: {R1}
}

Block[B3] - Exit
    Predecessors: [B1] [B2]
    Statements (0)
";
            VerifyFlowGraphAndDiagnosticsForTest<BlockSyntax>(source, expectedFlowGraph, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation, CompilerFeature.Dataflow)]
        [Fact]
        public void SwitchFlow_24()
        {
            string source = @"
public sealed class MyClass
{
    void M(bool result, int? input1, int input2)
    /*<bind>*/{
        switch (input1 ?? input2)
        {
            case 1:
                result = false;
                break;
        }
    }/*</bind>*/
}
";
            var expectedDiagnostics = DiagnosticDescription.None;

            string expectedFlowGraph = @"
Block[B0] - Entry
    Statements (0)
    Next (Regular) Block[B1]
        Entering: {R1} {R2}

.locals {R1}
{
    CaptureIds: [1]
    .locals {R2}
    {
        CaptureIds: [0]
        Block[B1] - Block
            Predecessors: [B0]
            Statements (1)
                IFlowCaptureOperation: 0 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'input1')
                  Value: 
                    IParameterReferenceOperation: input1 (OperationKind.ParameterReference, Type: System.Int32?) (Syntax: 'input1')

            Jump if True (Regular) to Block[B3]
                IIsNullOperation (OperationKind.IsNull, Type: System.Boolean, IsImplicit) (Syntax: 'input1')
                  Operand: 
                    IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: System.Int32?, IsImplicit) (Syntax: 'input1')
                Leaving: {R2}

            Next (Regular) Block[B2]
        Block[B2] - Block
            Predecessors: [B1]
            Statements (1)
                IFlowCaptureOperation: 1 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'input1')
                  Value: 
                    IInvocationOperation ( System.Int32 System.Int32?.GetValueOrDefault()) (OperationKind.Invocation, Type: System.Int32, IsImplicit) (Syntax: 'input1')
                      Instance Receiver: 
                        IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: System.Int32?, IsImplicit) (Syntax: 'input1')
                      Arguments(0)

            Next (Regular) Block[B4]
                Leaving: {R2}
    }

    Block[B3] - Block
        Predecessors: [B1]
        Statements (1)
            IFlowCaptureOperation: 1 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'input2')
              Value: 
                IParameterReferenceOperation: input2 (OperationKind.ParameterReference, Type: System.Int32) (Syntax: 'input2')

        Next (Regular) Block[B4]
    Block[B4] - Block
        Predecessors: [B2] [B3]
        Statements (0)
        Jump if False (Regular) to Block[B6]
            IBinaryOperation (BinaryOperatorKind.Equals) (OperationKind.Binary, Type: System.Boolean, IsImplicit) (Syntax: '1')
              Left: 
                IFlowCaptureReferenceOperation: 1 (OperationKind.FlowCaptureReference, Type: System.Int32, IsImplicit) (Syntax: 'input1 ?? input2')
              Right: 
                ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 1) (Syntax: '1')
            Leaving: {R1}

        Next (Regular) Block[B5]
    Block[B5] - Block
        Predecessors: [B4]
        Statements (1)
            IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'result = false;')
              Expression: 
                ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Boolean) (Syntax: 'result = false')
                  Left: 
                    IParameterReferenceOperation: result (OperationKind.ParameterReference, Type: System.Boolean) (Syntax: 'result')
                  Right: 
                    ILiteralOperation (OperationKind.Literal, Type: System.Boolean, Constant: False) (Syntax: 'false')

        Next (Regular) Block[B6]
            Leaving: {R1}
}

Block[B6] - Exit
    Predecessors: [B4] [B5]
    Statements (0)
";
            VerifyFlowGraphAndDiagnosticsForTest<BlockSyntax>(source, expectedFlowGraph, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation, CompilerFeature.Dataflow)]
        [Fact]
        public void SwitchFlow_25()
        {
            string source = @"
public sealed class MyClass
{
    void M(bool result, int? input1, int input2, int input3)
    /*<bind>*/{
        switch (input3)
        {
            case input1 ?? input2:
                result = false;
                break;
        }
    }/*</bind>*/
}
";
            var expectedDiagnostics = new[] {
                // file.cs(8,18): error CS0150: A constant value is expected
                //             case input1 ?? input2:
                Diagnostic(ErrorCode.ERR_ConstantExpected, "input1 ?? input2").WithLocation(8, 18)
            };

            string expectedFlowGraph = @"
Block[B0] - Entry
    Statements (0)
    Next (Regular) Block[B1]
        Entering: {R1}

.locals {R1}
{
    CaptureIds: [0]
    Block[B1] - Block
        Predecessors: [B0]
        Statements (1)
            IFlowCaptureOperation: 0 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'input3')
              Value: 
                IParameterReferenceOperation: input3 (OperationKind.ParameterReference, Type: System.Int32) (Syntax: 'input3')

        Next (Regular) Block[B2]
            Entering: {R2} {R3}

    .locals {R2}
    {
        CaptureIds: [2]
        .locals {R3}
        {
            CaptureIds: [1]
            Block[B2] - Block
                Predecessors: [B1]
                Statements (1)
                    IFlowCaptureOperation: 1 (OperationKind.FlowCapture, Type: null, IsInvalid, IsImplicit) (Syntax: 'input1')
                      Value: 
                        IParameterReferenceOperation: input1 (OperationKind.ParameterReference, Type: System.Int32?, IsInvalid) (Syntax: 'input1')

                Jump if True (Regular) to Block[B4]
                    IIsNullOperation (OperationKind.IsNull, Type: System.Boolean, IsInvalid, IsImplicit) (Syntax: 'input1')
                      Operand: 
                        IFlowCaptureReferenceOperation: 1 (OperationKind.FlowCaptureReference, Type: System.Int32?, IsInvalid, IsImplicit) (Syntax: 'input1')
                    Leaving: {R3}

                Next (Regular) Block[B3]
            Block[B3] - Block
                Predecessors: [B2]
                Statements (1)
                    IFlowCaptureOperation: 2 (OperationKind.FlowCapture, Type: null, IsInvalid, IsImplicit) (Syntax: 'input1')
                      Value: 
                        IInvocationOperation ( System.Int32 System.Int32?.GetValueOrDefault()) (OperationKind.Invocation, Type: System.Int32, IsInvalid, IsImplicit) (Syntax: 'input1')
                          Instance Receiver: 
                            IFlowCaptureReferenceOperation: 1 (OperationKind.FlowCaptureReference, Type: System.Int32?, IsInvalid, IsImplicit) (Syntax: 'input1')
                          Arguments(0)

                Next (Regular) Block[B5]
                    Leaving: {R3}
        }

        Block[B4] - Block
            Predecessors: [B2]
            Statements (1)
                IFlowCaptureOperation: 2 (OperationKind.FlowCapture, Type: null, IsInvalid, IsImplicit) (Syntax: 'input2')
                  Value: 
                    IParameterReferenceOperation: input2 (OperationKind.ParameterReference, Type: System.Int32, IsInvalid) (Syntax: 'input2')

            Next (Regular) Block[B5]
        Block[B5] - Block
            Predecessors: [B3] [B4]
            Statements (0)
            Jump if False (Regular) to Block[B7]
                IBinaryOperation (BinaryOperatorKind.Equals) (OperationKind.Binary, Type: System.Boolean, IsInvalid, IsImplicit) (Syntax: 'input1 ?? input2')
                  Left: 
                    IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: System.Int32, IsImplicit) (Syntax: 'input3')
                  Right: 
                    IFlowCaptureReferenceOperation: 2 (OperationKind.FlowCaptureReference, Type: System.Int32, IsInvalid, IsImplicit) (Syntax: 'input1 ?? input2')
                Leaving: {R2} {R1}

            Next (Regular) Block[B6]
                Leaving: {R2}
    }

    Block[B6] - Block
        Predecessors: [B5]
        Statements (1)
            IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'result = false;')
              Expression: 
                ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Boolean) (Syntax: 'result = false')
                  Left: 
                    IParameterReferenceOperation: result (OperationKind.ParameterReference, Type: System.Boolean) (Syntax: 'result')
                  Right: 
                    ILiteralOperation (OperationKind.Literal, Type: System.Boolean, Constant: False) (Syntax: 'false')

        Next (Regular) Block[B7]
            Leaving: {R1}
}

Block[B7] - Exit
    Predecessors: [B5] [B6]
    Statements (0)
";
            VerifyFlowGraphAndDiagnosticsForTest<BlockSyntax>(source, expectedFlowGraph, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation, CompilerFeature.Dataflow)]
        [Fact]
        public void SwitchFlow_26()
        {
            string source = @"
public sealed class MyClass
{
    void M(int input1, MyClass input2)
    /*<bind>*/{
        switch (input1)
        {
            case 1:
                input2?.ToString();
                break;
        }
    }/*</bind>*/
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
    CaptureIds: [0]
    Block[B1] - Block
        Predecessors: [B0]
        Statements (1)
            IFlowCaptureOperation: 0 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'input1')
              Value: 
                IParameterReferenceOperation: input1 (OperationKind.ParameterReference, Type: System.Int32) (Syntax: 'input1')

        Jump if False (Regular) to Block[B4]
            IBinaryOperation (BinaryOperatorKind.Equals) (OperationKind.Binary, Type: System.Boolean, IsImplicit) (Syntax: '1')
              Left: 
                IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: System.Int32, IsImplicit) (Syntax: 'input1')
              Right: 
                ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 1) (Syntax: '1')
            Leaving: {R1}

        Next (Regular) Block[B2]
            Entering: {R2}

    .locals {R2}
    {
        CaptureIds: [1]
        Block[B2] - Block
            Predecessors: [B1]
            Statements (1)
                IFlowCaptureOperation: 1 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'input2')
                  Value: 
                    IParameterReferenceOperation: input2 (OperationKind.ParameterReference, Type: MyClass) (Syntax: 'input2')

            Jump if True (Regular) to Block[B4]
                IIsNullOperation (OperationKind.IsNull, Type: System.Boolean, IsImplicit) (Syntax: 'input2')
                  Operand: 
                    IFlowCaptureReferenceOperation: 1 (OperationKind.FlowCaptureReference, Type: MyClass, IsImplicit) (Syntax: 'input2')
                Leaving: {R2} {R1}

            Next (Regular) Block[B3]
        Block[B3] - Block
            Predecessors: [B2]
            Statements (1)
                IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'input2?.ToString();')
                  Expression: 
                    IInvocationOperation (virtual System.String System.Object.ToString()) (OperationKind.Invocation, Type: System.String) (Syntax: '.ToString()')
                      Instance Receiver: 
                        IFlowCaptureReferenceOperation: 1 (OperationKind.FlowCaptureReference, Type: MyClass, IsImplicit) (Syntax: 'input2')
                      Arguments(0)

            Next (Regular) Block[B4]
                Leaving: {R2} {R1}
    }
}

Block[B4] - Exit
    Predecessors: [B1] [B2] [B3]
    Statements (0)
";
            VerifyFlowGraphAndDiagnosticsForTest<BlockSyntax>(source, expectedFlowGraph, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation, CompilerFeature.Dataflow)]
        [Fact]
        public void SwitchFlow_27()
        {
            string source = @"
public sealed class MyClass
{
    void M(bool result, object input)
    /*<bind>*/{
        switch (input)
        {
            case 1:
                result = false;
                break;
        }
    }/*</bind>*/
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
    CaptureIds: [0]
    Block[B1] - Block
        Predecessors: [B0]
        Statements (1)
            IFlowCaptureOperation: 0 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'input')
              Value: 
                IParameterReferenceOperation: input (OperationKind.ParameterReference, Type: System.Object) (Syntax: 'input')

        Jump if False (Regular) to Block[B3]
            IIsPatternOperation (OperationKind.IsPattern, Type: System.Boolean, IsImplicit) (Syntax: 'case 1:')
              Value: 
                IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: System.Object, IsImplicit) (Syntax: 'input')
              Pattern: 
                IConstantPatternOperation (OperationKind.ConstantPattern, Type: null, IsImplicit) (Syntax: 'case 1:') (InputType: System.Object)
                  Value: 
                    ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 1) (Syntax: '1')
            Leaving: {R1}

        Next (Regular) Block[B2]
    Block[B2] - Block
        Predecessors: [B1]
        Statements (1)
            IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'result = false;')
              Expression: 
                ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Boolean) (Syntax: 'result = false')
                  Left: 
                    IParameterReferenceOperation: result (OperationKind.ParameterReference, Type: System.Boolean) (Syntax: 'result')
                  Right: 
                    ILiteralOperation (OperationKind.Literal, Type: System.Boolean, Constant: False) (Syntax: 'false')

        Next (Regular) Block[B3]
            Leaving: {R1}
}

Block[B3] - Exit
    Predecessors: [B1] [B2]
    Statements (0)
";
            VerifyFlowGraphAndDiagnosticsForTest<BlockSyntax>(source, expectedFlowGraph, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation, CompilerFeature.Dataflow)]
        [Fact]
        public void SwitchFlow_28()
        {
            string source = @"
public sealed class MyClass
{
    void M(bool result, object input, int? other)
    /*<bind>*/{
        switch (input)
        {
            case other ?? 1:
                result = false;
                break;
        }
    }/*</bind>*/
}
";
            var expectedDiagnostics = new[] {
                // file.cs(8,18): error CS0150: A constant value is expected
                //             case other ?? 1:
                Diagnostic(ErrorCode.ERR_ConstantExpected, "other ?? 1").WithLocation(8, 18)
            };

            string expectedFlowGraph = @"
Block[B0] - Entry
    Statements (0)
    Next (Regular) Block[B1]
        Entering: {R1}

.locals {R1}
{
    CaptureIds: [0]
    Block[B1] - Block
        Predecessors: [B0]
        Statements (1)
            IFlowCaptureOperation: 0 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'input')
              Value: 
                IParameterReferenceOperation: input (OperationKind.ParameterReference, Type: System.Object) (Syntax: 'input')

        Next (Regular) Block[B2]
            Entering: {R2} {R3}

    .locals {R2}
    {
        CaptureIds: [2]
        .locals {R3}
        {
            CaptureIds: [1]
            Block[B2] - Block
                Predecessors: [B1]
                Statements (1)
                    IFlowCaptureOperation: 1 (OperationKind.FlowCapture, Type: null, IsInvalid, IsImplicit) (Syntax: 'other')
                      Value: 
                        IParameterReferenceOperation: other (OperationKind.ParameterReference, Type: System.Int32?, IsInvalid) (Syntax: 'other')

                Jump if True (Regular) to Block[B4]
                    IIsNullOperation (OperationKind.IsNull, Type: System.Boolean, IsInvalid, IsImplicit) (Syntax: 'other')
                      Operand: 
                        IFlowCaptureReferenceOperation: 1 (OperationKind.FlowCaptureReference, Type: System.Int32?, IsInvalid, IsImplicit) (Syntax: 'other')
                    Leaving: {R3}

                Next (Regular) Block[B3]
            Block[B3] - Block
                Predecessors: [B2]
                Statements (1)
                    IFlowCaptureOperation: 2 (OperationKind.FlowCapture, Type: null, IsInvalid, IsImplicit) (Syntax: 'other')
                      Value: 
                        IInvocationOperation ( System.Int32 System.Int32?.GetValueOrDefault()) (OperationKind.Invocation, Type: System.Int32, IsInvalid, IsImplicit) (Syntax: 'other')
                          Instance Receiver: 
                            IFlowCaptureReferenceOperation: 1 (OperationKind.FlowCaptureReference, Type: System.Int32?, IsInvalid, IsImplicit) (Syntax: 'other')
                          Arguments(0)

                Next (Regular) Block[B5]
                    Leaving: {R3}
        }

        Block[B4] - Block
            Predecessors: [B2]
            Statements (1)
                IFlowCaptureOperation: 2 (OperationKind.FlowCapture, Type: null, IsInvalid, IsImplicit) (Syntax: '1')
                  Value: 
                    ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 1, IsInvalid) (Syntax: '1')

            Next (Regular) Block[B5]
        Block[B5] - Block
            Predecessors: [B3] [B4]
            Statements (0)
            Jump if False (Regular) to Block[B7]
                IIsPatternOperation (OperationKind.IsPattern, Type: System.Boolean, IsInvalid, IsImplicit) (Syntax: 'case other ?? 1:')
                  Value: 
                    IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: System.Object, IsImplicit) (Syntax: 'input')
                  Pattern: 
                    IConstantPatternOperation (OperationKind.ConstantPattern, Type: null, IsInvalid, IsImplicit) (Syntax: 'case other ?? 1:') (InputType: System.Object)
                      Value: 
                        IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.Object, IsInvalid, IsImplicit) (Syntax: 'other ?? 1')
                          Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                            (Boxing)
                          Operand: 
                            IFlowCaptureReferenceOperation: 2 (OperationKind.FlowCaptureReference, Type: System.Int32, IsInvalid, IsImplicit) (Syntax: 'other ?? 1')
                Leaving: {R2} {R1}

            Next (Regular) Block[B6]
                Leaving: {R2}
    }

    Block[B6] - Block
        Predecessors: [B5]
        Statements (1)
            IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'result = false;')
              Expression: 
                ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Boolean) (Syntax: 'result = false')
                  Left: 
                    IParameterReferenceOperation: result (OperationKind.ParameterReference, Type: System.Boolean) (Syntax: 'result')
                  Right: 
                    ILiteralOperation (OperationKind.Literal, Type: System.Boolean, Constant: False) (Syntax: 'false')

        Next (Regular) Block[B7]
            Leaving: {R1}
}

Block[B7] - Exit
    Predecessors: [B5] [B6]
    Statements (0)
";
            VerifyFlowGraphAndDiagnosticsForTest<BlockSyntax>(source, expectedFlowGraph, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation, CompilerFeature.Dataflow)]
        [Fact]
        public void SwitchFlow_29()
        {
            string source = @"
public sealed class MyClass
{
    void M(bool result, object input)
    /*<bind>*/{
        switch (input)
        {
            case int x:
                result = false;
                break;
        }
    }/*</bind>*/
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
    CaptureIds: [0]
    Block[B1] - Block
        Predecessors: [B0]
        Statements (1)
            IFlowCaptureOperation: 0 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'input')
              Value: 
                IParameterReferenceOperation: input (OperationKind.ParameterReference, Type: System.Object) (Syntax: 'input')

        Next (Regular) Block[B2]
            Entering: {R2}

    .locals {R2}
    {
        Locals: [System.Int32 x]
        Block[B2] - Block
            Predecessors: [B1]
            Statements (0)
            Jump if False (Regular) to Block[B4]
                IIsPatternOperation (OperationKind.IsPattern, Type: System.Boolean, IsImplicit) (Syntax: 'int x')
                  Value: 
                    IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: System.Object, IsImplicit) (Syntax: 'input')
                  Pattern: 
                    IDeclarationPatternOperation (OperationKind.DeclarationPattern, Type: null) (Syntax: 'int x') (InputType: System.Object, DeclaredSymbol: System.Int32 x, MatchesNull: False)
                Leaving: {R2} {R1}

            Next (Regular) Block[B3]
        Block[B3] - Block
            Predecessors: [B2]
            Statements (1)
                IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'result = false;')
                  Expression: 
                    ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Boolean) (Syntax: 'result = false')
                      Left: 
                        IParameterReferenceOperation: result (OperationKind.ParameterReference, Type: System.Boolean) (Syntax: 'result')
                      Right: 
                        ILiteralOperation (OperationKind.Literal, Type: System.Boolean, Constant: False) (Syntax: 'false')

            Next (Regular) Block[B4]
                Leaving: {R2} {R1}
    }
}

Block[B4] - Exit
    Predecessors: [B2] [B3]
    Statements (0)
";
            VerifyFlowGraphAndDiagnosticsForTest<BlockSyntax>(source, expectedFlowGraph, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation, CompilerFeature.Dataflow)]
        [Fact]
        public void SwitchFlow_30()
        {
            string source = @"
public sealed class MyClass
{
    void M(bool result, object input1, bool other2, bool other3, bool other4)
    /*<bind>*/{
        switch (input1)
        {
            case int x when (other2 ? other3 : other4) :
                result = false;
                break;
        }
    }/*</bind>*/
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
    CaptureIds: [0]
    Block[B1] - Block
        Predecessors: [B0]
        Statements (1)
            IFlowCaptureOperation: 0 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'input1')
              Value: 
                IParameterReferenceOperation: input1 (OperationKind.ParameterReference, Type: System.Object) (Syntax: 'input1')

        Next (Regular) Block[B2]
            Entering: {R2}

    .locals {R2}
    {
        Locals: [System.Int32 x]
        Block[B2] - Block
            Predecessors: [B1]
            Statements (0)
            Jump if False (Regular) to Block[B7]
                IIsPatternOperation (OperationKind.IsPattern, Type: System.Boolean, IsImplicit) (Syntax: 'int x')
                  Value: 
                    IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: System.Object, IsImplicit) (Syntax: 'input1')
                  Pattern: 
                    IDeclarationPatternOperation (OperationKind.DeclarationPattern, Type: null) (Syntax: 'int x') (InputType: System.Object, DeclaredSymbol: System.Int32 x, MatchesNull: False)
                Leaving: {R2} {R1}

            Next (Regular) Block[B3]
        Block[B3] - Block
            Predecessors: [B2]
            Statements (0)
            Jump if False (Regular) to Block[B5]
                IParameterReferenceOperation: other2 (OperationKind.ParameterReference, Type: System.Boolean) (Syntax: 'other2')

            Next (Regular) Block[B4]
        Block[B4] - Block
            Predecessors: [B3]
            Statements (0)
            Jump if False (Regular) to Block[B7]
                IParameterReferenceOperation: other3 (OperationKind.ParameterReference, Type: System.Boolean) (Syntax: 'other3')
                Leaving: {R2} {R1}

            Next (Regular) Block[B6]
        Block[B5] - Block
            Predecessors: [B3]
            Statements (0)
            Jump if False (Regular) to Block[B7]
                IParameterReferenceOperation: other4 (OperationKind.ParameterReference, Type: System.Boolean) (Syntax: 'other4')
                Leaving: {R2} {R1}

            Next (Regular) Block[B6]
        Block[B6] - Block
            Predecessors: [B4] [B5]
            Statements (1)
                IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'result = false;')
                  Expression: 
                    ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Boolean) (Syntax: 'result = false')
                      Left: 
                        IParameterReferenceOperation: result (OperationKind.ParameterReference, Type: System.Boolean) (Syntax: 'result')
                      Right: 
                        ILiteralOperation (OperationKind.Literal, Type: System.Boolean, Constant: False) (Syntax: 'false')

            Next (Regular) Block[B7]
                Leaving: {R2} {R1}
    }
}

Block[B7] - Exit
    Predecessors: [B2] [B4] [B5] [B6]
    Statements (0)
";
            VerifyFlowGraphAndDiagnosticsForTest<BlockSyntax>(source, expectedFlowGraph, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation, CompilerFeature.Dataflow)]
        [Fact]
        public void SwitchFlow_31()
        {
            string source = @"
public sealed class MyClass
{
    void M(bool result, object input)
    /*<bind>*/{
        switch (input)
        {
            case 1:
                result = false;
                break;
            case 2:
                result = true;
                break;
        }
    }/*</bind>*/
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
    CaptureIds: [0]
    Block[B1] - Block
        Predecessors: [B0]
        Statements (1)
            IFlowCaptureOperation: 0 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'input')
              Value: 
                IParameterReferenceOperation: input (OperationKind.ParameterReference, Type: System.Object) (Syntax: 'input')

        Jump if False (Regular) to Block[B3]
            IIsPatternOperation (OperationKind.IsPattern, Type: System.Boolean, IsImplicit) (Syntax: 'case 1:')
              Value: 
                IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: System.Object, IsImplicit) (Syntax: 'input')
              Pattern: 
                IConstantPatternOperation (OperationKind.ConstantPattern, Type: null, IsImplicit) (Syntax: 'case 1:') (InputType: System.Object)
                  Value: 
                    ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 1) (Syntax: '1')

        Next (Regular) Block[B2]
    Block[B2] - Block
        Predecessors: [B1]
        Statements (1)
            IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'result = false;')
              Expression: 
                ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Boolean) (Syntax: 'result = false')
                  Left: 
                    IParameterReferenceOperation: result (OperationKind.ParameterReference, Type: System.Boolean) (Syntax: 'result')
                  Right: 
                    ILiteralOperation (OperationKind.Literal, Type: System.Boolean, Constant: False) (Syntax: 'false')

        Next (Regular) Block[B5]
            Leaving: {R1}
    Block[B3] - Block
        Predecessors: [B1]
        Statements (0)
        Jump if False (Regular) to Block[B5]
            IIsPatternOperation (OperationKind.IsPattern, Type: System.Boolean, IsImplicit) (Syntax: 'case 2:')
              Value: 
                IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: System.Object, IsImplicit) (Syntax: 'input')
              Pattern: 
                IConstantPatternOperation (OperationKind.ConstantPattern, Type: null, IsImplicit) (Syntax: 'case 2:') (InputType: System.Object)
                  Value: 
                    ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 2) (Syntax: '2')
            Leaving: {R1}

        Next (Regular) Block[B4]
    Block[B4] - Block
        Predecessors: [B3]
        Statements (1)
            IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'result = true;')
              Expression: 
                ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Boolean) (Syntax: 'result = true')
                  Left: 
                    IParameterReferenceOperation: result (OperationKind.ParameterReference, Type: System.Boolean) (Syntax: 'result')
                  Right: 
                    ILiteralOperation (OperationKind.Literal, Type: System.Boolean, Constant: True) (Syntax: 'true')

        Next (Regular) Block[B5]
            Leaving: {R1}
}

Block[B5] - Exit
    Predecessors: [B2] [B3] [B4]
    Statements (0)
";
            VerifyFlowGraphAndDiagnosticsForTest<BlockSyntax>(source, expectedFlowGraph, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation, CompilerFeature.Dataflow)]
        [Fact]
        public void SwitchFlow_32()
        {
            string source = @"
public sealed class MyClass
{
    void M(bool result, object input, bool? guard)
    /*<bind>*/{
        switch (input)
        {
            case 1 when guard ?? throw null:
                result = false;
                break;
        }
    }/*</bind>*/
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
        CaptureIds: [0]
        Block[B1] - Block
            Predecessors: [B0]
            Statements (1)
                IFlowCaptureOperation: 0 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'input')
                  Value: 
                    IParameterReferenceOperation: input (OperationKind.ParameterReference, Type: System.Object) (Syntax: 'input')
            Jump if False (Regular) to Block[B6]
                IIsPatternOperation (OperationKind.IsPattern, Type: System.Boolean, IsImplicit) (Syntax: '1')
                  Value: 
                    IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: System.Object, IsImplicit) (Syntax: 'input')
                  Pattern: 
                    IConstantPatternOperation (OperationKind.ConstantPattern, Type: null) (Syntax: '1') (InputType: System.Object)
                      Value: 
                        ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 1) (Syntax: '1')
                Leaving: {R1}
            Next (Regular) Block[B2]
                Entering: {R2}
        .locals {R2}
        {
            CaptureIds: [1]
            Block[B2] - Block
                Predecessors: [B1]
                Statements (1)
                    IFlowCaptureOperation: 1 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'guard')
                      Value: 
                        IParameterReferenceOperation: guard (OperationKind.ParameterReference, Type: System.Boolean?) (Syntax: 'guard')
                Jump if True (Regular) to Block[B4]
                    IIsNullOperation (OperationKind.IsNull, Type: System.Boolean, IsImplicit) (Syntax: 'guard')
                      Operand: 
                        IFlowCaptureReferenceOperation: 1 (OperationKind.FlowCaptureReference, Type: System.Boolean?, IsImplicit) (Syntax: 'guard')
                    Leaving: {R2}
                Next (Regular) Block[B3]
            Block[B3] - Block
                Predecessors: [B2]
                Statements (0)
                Jump if False (Regular) to Block[B6]
                    IInvocationOperation ( System.Boolean System.Boolean?.GetValueOrDefault()) (OperationKind.Invocation, Type: System.Boolean, IsImplicit) (Syntax: 'guard')
                      Instance Receiver: 
                        IFlowCaptureReferenceOperation: 1 (OperationKind.FlowCaptureReference, Type: System.Boolean?, IsImplicit) (Syntax: 'guard')
                      Arguments(0)
                    Leaving: {R2} {R1}
                Next (Regular) Block[B5]
                    Leaving: {R2}
        }
        Block[B4] - Block
            Predecessors: [B2]
            Statements (0)
            Next (Throw) Block[null]
                IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.Exception, Constant: null, IsImplicit) (Syntax: 'null')
                  Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: True, IsUserDefined: False) (MethodSymbol: null)
                    (ImplicitReference)
                  Operand: 
                    ILiteralOperation (OperationKind.Literal, Type: null, Constant: null) (Syntax: 'null')
        Block[B5] - Block
            Predecessors: [B3]
            Statements (1)
                IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'result = false;')
                  Expression: 
                    ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Boolean) (Syntax: 'result = false')
                      Left: 
                        IParameterReferenceOperation: result (OperationKind.ParameterReference, Type: System.Boolean) (Syntax: 'result')
                      Right: 
                        ILiteralOperation (OperationKind.Literal, Type: System.Boolean, Constant: False) (Syntax: 'false')
            Next (Regular) Block[B6]
                Leaving: {R1}
    }
    Block[B6] - Exit
        Predecessors: [B1] [B3] [B5]
        Statements (0)
";
            VerifyFlowGraphAndDiagnosticsForTest<BlockSyntax>(source, expectedFlowGraph, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation, CompilerFeature.Dataflow)]
        [Fact]
        public void SwitchFlow_33()
        {
            string source = @"
public sealed class MyClass
{
    void M(bool result, object input)
    /*<bind>*/{
        switch (input)
        {
            case 1 when guard:
                result = false;
                break;
        }
    }/*</bind>*/
}
";
            var expectedDiagnostics = new[] {
                // file.cs(8,25): error CS0103: The name 'guard' does not exist in the current context
                //             case 1 when guard:
                Diagnostic(ErrorCode.ERR_NameNotInContext, "guard").WithArguments("guard").WithLocation(8, 25)
            };

            string expectedFlowGraph = @"
Block[B0] - Entry
    Statements (0)
    Next (Regular) Block[B1]
        Entering: {R1}

.locals {R1}
{
    CaptureIds: [0]
    Block[B1] - Block
        Predecessors: [B0]
        Statements (1)
            IFlowCaptureOperation: 0 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'input')
              Value: 
                IParameterReferenceOperation: input (OperationKind.ParameterReference, Type: System.Object) (Syntax: 'input')

        Jump if False (Regular) to Block[B4]
            IIsPatternOperation (OperationKind.IsPattern, Type: System.Boolean, IsImplicit) (Syntax: '1')
              Value: 
                IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: System.Object, IsImplicit) (Syntax: 'input')
              Pattern: 
                IConstantPatternOperation (OperationKind.ConstantPattern, Type: null) (Syntax: '1') (InputType: System.Object)
                  Value: 
                    ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 1) (Syntax: '1')
            Leaving: {R1}

        Next (Regular) Block[B2]
    Block[B2] - Block
        Predecessors: [B1]
        Statements (0)
        Jump if False (Regular) to Block[B4]
            IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.Boolean, IsInvalid, IsImplicit) (Syntax: 'guard')
              Conversion: CommonConversion (Exists: False, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                (NoConversion)
              Operand: 
                IInvalidOperation (OperationKind.Invalid, Type: ?, IsInvalid) (Syntax: 'guard')
                  Children(0)
            Leaving: {R1}

        Next (Regular) Block[B3]
    Block[B3] - Block
        Predecessors: [B2]
        Statements (1)
            IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'result = false;')
              Expression: 
                ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Boolean) (Syntax: 'result = false')
                  Left: 
                    IParameterReferenceOperation: result (OperationKind.ParameterReference, Type: System.Boolean) (Syntax: 'result')
                  Right: 
                    ILiteralOperation (OperationKind.Literal, Type: System.Boolean, Constant: False) (Syntax: 'false')

        Next (Regular) Block[B4]
            Leaving: {R1}
}

Block[B4] - Exit
    Predecessors: [B1] [B2] [B3]
    Statements (0)
";
            VerifyFlowGraphAndDiagnosticsForTest<BlockSyntax>(source, expectedFlowGraph, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation, CompilerFeature.Dataflow)]
        [Fact]
        public void SwitchFlow_34()
        {
            string source = @"
public sealed class MyClass
{
    void M(bool result, int input)
    /*<bind>*/{
        switch (input)
        {
            case 1+TakeOutParam(3, out MyClass x1):
                result = false;
                break;
        }
    }/*</bind>*/
    int TakeOutParam(int a, out MyClass b)
    {
        b = default;
        return a;
    }
}
";
            var expectedDiagnostics = new DiagnosticDescription[] {
                // file.cs(8,18): error CS0150: A constant value is expected
                //             case 1+TakeOutParam(3, out MyClass x1):
                Diagnostic(ErrorCode.ERR_ConstantExpected, "1+TakeOutParam(3, out MyClass x1)").WithLocation(8, 18)
            };

            string expectedFlowGraph = @"
Block[B0] - Entry
    Statements (0)
    Next (Regular) Block[B1]
        Entering: {R1}

.locals {R1}
{
    CaptureIds: [0]
    Block[B1] - Block
        Predecessors: [B0]
        Statements (1)
            IFlowCaptureOperation: 0 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'input')
              Value: 
                IParameterReferenceOperation: input (OperationKind.ParameterReference, Type: System.Int32) (Syntax: 'input')

        Next (Regular) Block[B2]
            Entering: {R2}

    .locals {R2}
    {
        Locals: [MyClass x1]
        Block[B2] - Block
            Predecessors: [B1]
            Statements (0)
            Jump if False (Regular) to Block[B4]
                IBinaryOperation (BinaryOperatorKind.Equals) (OperationKind.Binary, Type: System.Boolean, IsInvalid, IsImplicit) (Syntax: '1+TakeOutPa ... MyClass x1)')
                  Left: 
                    IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: System.Int32, IsImplicit) (Syntax: 'input')
                  Right: 
                    IBinaryOperation (BinaryOperatorKind.Add) (OperationKind.Binary, Type: System.Int32, IsInvalid) (Syntax: '1+TakeOutPa ... MyClass x1)')
                      Left: 
                        ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 1, IsInvalid) (Syntax: '1')
                      Right: 
                        IInvocationOperation ( System.Int32 MyClass.TakeOutParam(System.Int32 a, out MyClass b)) (OperationKind.Invocation, Type: System.Int32, IsInvalid) (Syntax: 'TakeOutPara ... MyClass x1)')
                          Instance Receiver: 
                            IInstanceReferenceOperation (ReferenceKind: ContainingTypeInstance) (OperationKind.InstanceReference, Type: MyClass, IsInvalid, IsImplicit) (Syntax: 'TakeOutParam')
                          Arguments(2):
                              IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: a) (OperationKind.Argument, Type: null, IsInvalid) (Syntax: '3')
                                ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 3, IsInvalid) (Syntax: '3')
                                InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                                OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                              IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: b) (OperationKind.Argument, Type: null, IsInvalid) (Syntax: 'out MyClass x1')
                                IDeclarationExpressionOperation (OperationKind.DeclarationExpression, Type: MyClass, IsInvalid) (Syntax: 'MyClass x1')
                                  ILocalReferenceOperation: x1 (IsDeclaration: True) (OperationKind.LocalReference, Type: MyClass, IsInvalid) (Syntax: 'x1')
                                InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                                OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                Leaving: {R2} {R1}

            Next (Regular) Block[B3]
        Block[B3] - Block
            Predecessors: [B2]
            Statements (1)
                IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'result = false;')
                  Expression: 
                    ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Boolean) (Syntax: 'result = false')
                      Left: 
                        IParameterReferenceOperation: result (OperationKind.ParameterReference, Type: System.Boolean) (Syntax: 'result')
                      Right: 
                        ILiteralOperation (OperationKind.Literal, Type: System.Boolean, Constant: False) (Syntax: 'false')

            Next (Regular) Block[B4]
                Leaving: {R2} {R1}
    }
}

Block[B4] - Exit
    Predecessors: [B2] [B3]
    Statements (0)
";
            VerifyFlowGraphAndDiagnosticsForTest<BlockSyntax>(source, expectedFlowGraph, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation, CompilerFeature.Dataflow)]
        [Fact]
        public void SwitchFlow_35()
        {
            string source = @"
public sealed class MyClass
{
    void M(bool result, int? input)
    /*<bind>*/{
        switch (input)
        {
            case 1+(input is int x1 ? x1 : 0):
                result = false;
                break;
        }
    }/*</bind>*/
}
";
            var expectedDiagnostics = new DiagnosticDescription[] {
                // file.cs(8,18): error CS0150: A constant value is expected
                //             case 1+(input is int x1 ? x1 : 0):
                Diagnostic(ErrorCode.ERR_ConstantExpected, "1+(input is int x1 ? x1 : 0)").WithLocation(8, 18)
            };

            string expectedFlowGraph = @"
Block[B0] - Entry
    Statements (0)
    Next (Regular) Block[B1]
        Entering: {R1}

.locals {R1}
{
    CaptureIds: [0]
    Block[B1] - Block
        Predecessors: [B0]
        Statements (1)
            IFlowCaptureOperation: 0 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'input')
              Value: 
                IParameterReferenceOperation: input (OperationKind.ParameterReference, Type: System.Int32?) (Syntax: 'input')

        Next (Regular) Block[B2]
            Entering: {R2} {R3}

    .locals {R2}
    {
        Locals: [System.Int32 x1]
        .locals {R3}
        {
            CaptureIds: [1] [2]
            Block[B2] - Block
                Predecessors: [B1]
                Statements (1)
                    IFlowCaptureOperation: 1 (OperationKind.FlowCapture, Type: null, IsInvalid, IsImplicit) (Syntax: '1')
                      Value: 
                        ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 1, IsInvalid) (Syntax: '1')

                Jump if False (Regular) to Block[B4]
                    IIsPatternOperation (OperationKind.IsPattern, Type: System.Boolean, IsInvalid) (Syntax: 'input is int x1')
                      Value: 
                        IParameterReferenceOperation: input (OperationKind.ParameterReference, Type: System.Int32?, IsInvalid) (Syntax: 'input')
                      Pattern: 
                        IDeclarationPatternOperation (OperationKind.DeclarationPattern, Type: null, IsInvalid) (Syntax: 'int x1') (InputType: System.Int32?, DeclaredSymbol: System.Int32 x1, MatchesNull: False)

                Next (Regular) Block[B3]
            Block[B3] - Block
                Predecessors: [B2]
                Statements (1)
                    IFlowCaptureOperation: 2 (OperationKind.FlowCapture, Type: null, IsInvalid, IsImplicit) (Syntax: 'x1')
                      Value: 
                        ILocalReferenceOperation: x1 (OperationKind.LocalReference, Type: System.Int32, IsInvalid) (Syntax: 'x1')

                Next (Regular) Block[B5]
            Block[B4] - Block
                Predecessors: [B2]
                Statements (1)
                    IFlowCaptureOperation: 2 (OperationKind.FlowCapture, Type: null, IsInvalid, IsImplicit) (Syntax: '0')
                      Value: 
                        ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 0, IsInvalid) (Syntax: '0')

                Next (Regular) Block[B5]
            Block[B5] - Block
                Predecessors: [B3] [B4]
                Statements (0)
                Jump if False (Regular) to Block[B7]
                    IBinaryOperation (BinaryOperatorKind.Equals, IsLifted) (OperationKind.Binary, Type: System.Boolean, IsInvalid, IsImplicit) (Syntax: '1+(input is ... 1 ? x1 : 0)')
                      Left: 
                        IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: System.Int32?, IsImplicit) (Syntax: 'input')
                      Right: 
                        IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.Int32?, IsInvalid, IsImplicit) (Syntax: '1+(input is ... 1 ? x1 : 0)')
                          Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                            (ImplicitNullable)
                          Operand: 
                            IBinaryOperation (BinaryOperatorKind.Add) (OperationKind.Binary, Type: System.Int32, IsInvalid) (Syntax: '1+(input is ... 1 ? x1 : 0)')
                              Left: 
                                IFlowCaptureReferenceOperation: 1 (OperationKind.FlowCaptureReference, Type: System.Int32, Constant: 1, IsInvalid, IsImplicit) (Syntax: '1')
                              Right: 
                                IFlowCaptureReferenceOperation: 2 (OperationKind.FlowCaptureReference, Type: System.Int32, IsInvalid, IsImplicit) (Syntax: 'input is int x1 ? x1 : 0')
                    Leaving: {R3} {R2} {R1}

                Next (Regular) Block[B6]
                    Leaving: {R3}
        }

        Block[B6] - Block
            Predecessors: [B5]
            Statements (1)
                IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'result = false;')
                  Expression: 
                    ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Boolean) (Syntax: 'result = false')
                      Left: 
                        IParameterReferenceOperation: result (OperationKind.ParameterReference, Type: System.Boolean) (Syntax: 'result')
                      Right: 
                        ILiteralOperation (OperationKind.Literal, Type: System.Boolean, Constant: False) (Syntax: 'false')

            Next (Regular) Block[B7]
                Leaving: {R2} {R1}
    }
}

Block[B7] - Exit
    Predecessors: [B5] [B6]
    Statements (0)
";
            VerifyFlowGraphAndDiagnosticsForTest<BlockSyntax>(source, expectedFlowGraph, expectedDiagnostics);
        }


        [CompilerTrait(CompilerFeature.IOperation, CompilerFeature.Patterns, CompilerFeature.Dataflow)]
        [Fact]
        public void EmptySwitchExpressionFlow()
        {
            string source = @"
class Program
{
    public static void Main()
    /*<bind>*/{
        var r = 1 switch { };
    }/*</bind>*/
}
";
            var expectedDiagnostics = new[] {
                // file.cs(6,19): warning CS8509: The switch expression does not handle all possible values of its input type (it is not exhaustive).
                //         var r = 1 switch { };
                Diagnostic(ErrorCode.WRN_SwitchExpressionNotExhaustive, "switch").WithLocation(6, 19),
                // file.cs(6,19): error CS8506: No best type was found for the switch expression.
                //         var r = 1 switch { };
                Diagnostic(ErrorCode.ERR_SwitchExpressionNoBestType, "switch").WithLocation(6, 19)
            };

            string expectedFlowGraph = @"
Block[B0] - Entry
    Statements (0)
    Next (Regular) Block[B1]
        Entering: {R1} {R2}
.locals {R1}
{
    Locals: [? r]
    CaptureIds: [0]
    .locals {R2}
    {
        CaptureIds: [1]
        Block[B1] - Block
            Predecessors: [B0]
            Statements (1)
                IFlowCaptureOperation: 1 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: '1')
                  Value: 
                    ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 1) (Syntax: '1')
            Next (Regular) Block[B2]
                Leaving: {R2}
    }
    Block[B2] - Block
        Predecessors: [B1]
        Statements (0)
        Next (Throw) Block[null]
            IObjectCreationOperation (Constructor: System.InvalidOperationException..ctor()) (OperationKind.ObjectCreation, Type: System.InvalidOperationException, IsInvalid, IsImplicit) (Syntax: '1 switch { }')
              Arguments(0)
              Initializer: 
                null
    Block[B3] - Block [UnReachable]
        Predecessors (0)
        Statements (1)
            ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: ?, IsInvalid, IsImplicit) (Syntax: 'r = 1 switch { }')
              Left: 
                ILocalReferenceOperation: r (IsDeclaration: True) (OperationKind.LocalReference, Type: ?, IsInvalid, IsImplicit) (Syntax: 'r = 1 switch { }')
              Right: 
                IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: ?, IsInvalid, IsImplicit) (Syntax: '1 switch { }')
        Next (Regular) Block[B4]
            Leaving: {R1}
}
Block[B4] - Exit [UnReachable]
    Predecessors: [B3]
    Statements (0)
";
            VerifyFlowGraphAndDiagnosticsForTest<BlockSyntax>(source, expectedFlowGraph, expectedDiagnostics);
        }
    }
}
