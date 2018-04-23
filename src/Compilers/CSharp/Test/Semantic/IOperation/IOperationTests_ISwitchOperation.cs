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
                  IDeclarationPatternOperation (Declared Symbol: System.Int32 x) (OperationKind.DeclarationPattern, Type: null) (Syntax: 'int x')
                Guard Expression: 
                  null
              IPatternCaseClauseOperation (Label Id: 2) (CaseKind.Pattern) (OperationKind.CaseClause, Type: null) (Syntax: 'case long y:')
                Pattern: 
                  IDeclarationPatternOperation (Declared Symbol: System.Int64 y) (OperationKind.DeclarationPattern, Type: null) (Syntax: 'long y')
                Guard Expression: 
                  null
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
                  IDeclarationPatternOperation (Declared Symbol: System.Int32 y) (OperationKind.DeclarationPattern, Type: null) (Syntax: 'int y')
                Guard Expression: 
                  null
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
                          IBinaryOperation (BinaryOperatorKind.Add) (OperationKind.BinaryOperator, Type: System.Int32) (Syntax: 'x + y')
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
    }
}
