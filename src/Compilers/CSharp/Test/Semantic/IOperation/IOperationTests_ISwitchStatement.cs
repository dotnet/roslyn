// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Test.Utilities;
using Xunit;


namespace Microsoft.CodeAnalysis.CSharp.UnitTests
{
    public partial class IOperationTests : SemanticModelTestBase
    {
        [Fact, WorkItem(17600, "https://github.com/dotnet/roslyn/issues/17600")]
        public void ISwitchStatement_Simple()
        {
            string source = @"
using System;

class Test
{
    void F()
    {
        int number = 1;
        /*<bind>*/switch (number)
        {
            case 0:
                break;
        }/*</bind>*/
    }   
}
";
            string expectedOperationTree = @"
ISwitchStatement (1 cases) (OperationKind.SwitchStatement) (Syntax: 'switch (num ... }')
  Switch expression: 
    ILocalReferenceExpression: number (OperationKind.LocalReferenceExpression, Type: System.Int32) (Syntax: 'number')
  Sections:
      ISwitchCase (1 case clauses, 1 statements) (OperationKind.SwitchCase) (Syntax: 'case 0: ... break;')
          Clauses:
              ISingleValueCaseClause (CaseKind.SingleValue) (OperationKind.CaseClause) (Syntax: 'case 0:')
                Value: 
                  ILiteralExpression (OperationKind.LiteralExpression, Type: System.Int32, Constant: 0) (Syntax: '0')
          Body:
              IBranchStatement (BranchKind.Break) (OperationKind.BranchStatement) (Syntax: 'break;')
";
            var expectedDiagnostics = DiagnosticDescription.None;

            VerifyOperationTreeAndDiagnosticsForTest<SwitchStatementSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }

        [Fact, WorkItem(17600, "https://github.com/dotnet/roslyn/issues/17600")]
        public void ISwitchStatement_SwitchSectionWithMultipleCaseClauses()
        {
            string source = @"
using System;

class Test
{
    void F()
    {
        int number = 1;
        /*<bind>*/switch (number)
        {
            case 0:
            case 1:
                break;
        }/*</bind>*/
    }   
}
";
            string expectedOperationTree = @"
ISwitchStatement (1 cases) (OperationKind.SwitchStatement) (Syntax: 'switch (num ... }')
  Switch expression: 
    ILocalReferenceExpression: number (OperationKind.LocalReferenceExpression, Type: System.Int32) (Syntax: 'number')
  Sections:
      ISwitchCase (2 case clauses, 1 statements) (OperationKind.SwitchCase) (Syntax: 'case 0: ... break;')
          Clauses:
              ISingleValueCaseClause (CaseKind.SingleValue) (OperationKind.CaseClause) (Syntax: 'case 0:')
                Value: 
                  ILiteralExpression (OperationKind.LiteralExpression, Type: System.Int32, Constant: 0) (Syntax: '0')
              ISingleValueCaseClause (CaseKind.SingleValue) (OperationKind.CaseClause) (Syntax: 'case 1:')
                Value: 
                  ILiteralExpression (OperationKind.LiteralExpression, Type: System.Int32, Constant: 1) (Syntax: '1')
          Body:
              IBranchStatement (BranchKind.Break) (OperationKind.BranchStatement) (Syntax: 'break;')
";
            var expectedDiagnostics = DiagnosticDescription.None;

            VerifyOperationTreeAndDiagnosticsForTest<SwitchStatementSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }

        [Fact, WorkItem(17600, "https://github.com/dotnet/roslyn/issues/17600")]
        public void ISwitchStatement_MultipleSwitchSections()
        {
            string source = @"
using System;

class Test
{
    void M(int number)
    {
        /*<bind>*/switch (number)
        {
            case 0:
                break;
            case 1:
                break;
        }/*</bind>*/
    }
}
";
            string expectedOperationTree = @"
ISwitchStatement (2 cases) (OperationKind.SwitchStatement) (Syntax: 'switch (num ... }')
  Switch expression: 
    IParameterReferenceExpression: number (OperationKind.ParameterReferenceExpression, Type: System.Int32) (Syntax: 'number')
  Sections:
      ISwitchCase (1 case clauses, 1 statements) (OperationKind.SwitchCase) (Syntax: 'case 0: ... break;')
          Clauses:
              ISingleValueCaseClause (CaseKind.SingleValue) (OperationKind.CaseClause) (Syntax: 'case 0:')
                Value: 
                  ILiteralExpression (OperationKind.LiteralExpression, Type: System.Int32, Constant: 0) (Syntax: '0')
          Body:
              IBranchStatement (BranchKind.Break) (OperationKind.BranchStatement) (Syntax: 'break;')
      ISwitchCase (1 case clauses, 1 statements) (OperationKind.SwitchCase) (Syntax: 'case 1: ... break;')
          Clauses:
              ISingleValueCaseClause (CaseKind.SingleValue) (OperationKind.CaseClause) (Syntax: 'case 1:')
                Value: 
                  ILiteralExpression (OperationKind.LiteralExpression, Type: System.Int32, Constant: 1) (Syntax: '1')
          Body:
              IBranchStatement (BranchKind.Break) (OperationKind.BranchStatement) (Syntax: 'break;')
";
            var expectedDiagnostics = DiagnosticDescription.None;

            VerifyOperationTreeAndDiagnosticsForTest<SwitchStatementSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }

        [Fact, WorkItem(17600, "https://github.com/dotnet/roslyn/issues/17600")]
        public void ISwitchStatement_ConversionInSwitchGoverningExpression()
        {
            string source = @"
using System;

class Test
{
    void M(double number)
    {
        /*<bind>*/switch ((int)number)
        {
            case 0:
                break;
            case 1:
                break;
        }/*</bind>*/
    }
}
";
            string expectedOperationTree = @"
ISwitchStatement (2 cases) (OperationKind.SwitchStatement) (Syntax: 'switch ((in ... }')
  Switch expression: 
    IConversionExpression (Explicit, TryCast: False, Unchecked) (OperationKind.ConversionExpression, Type: System.Int32) (Syntax: '(int)number')
      Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: True, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
      Operand: 
        IParameterReferenceExpression: number (OperationKind.ParameterReferenceExpression, Type: System.Double) (Syntax: 'number')
  Sections:
      ISwitchCase (1 case clauses, 1 statements) (OperationKind.SwitchCase) (Syntax: 'case 0: ... break;')
          Clauses:
              ISingleValueCaseClause (CaseKind.SingleValue) (OperationKind.CaseClause) (Syntax: 'case 0:')
                Value: 
                  ILiteralExpression (OperationKind.LiteralExpression, Type: System.Int32, Constant: 0) (Syntax: '0')
          Body:
              IBranchStatement (BranchKind.Break) (OperationKind.BranchStatement) (Syntax: 'break;')
      ISwitchCase (1 case clauses, 1 statements) (OperationKind.SwitchCase) (Syntax: 'case 1: ... break;')
          Clauses:
              ISingleValueCaseClause (CaseKind.SingleValue) (OperationKind.CaseClause) (Syntax: 'case 1:')
                Value: 
                  ILiteralExpression (OperationKind.LiteralExpression, Type: System.Int32, Constant: 1) (Syntax: '1')
          Body:
              IBranchStatement (BranchKind.Break) (OperationKind.BranchStatement) (Syntax: 'break;')
";
            var expectedDiagnostics = DiagnosticDescription.None;

            VerifyOperationTreeAndDiagnosticsForTest<SwitchStatementSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }

        [Fact, WorkItem(17600, "https://github.com/dotnet/roslyn/issues/17600")]
        public void ISwitchStatement_CaseLabelWithImplicitConversionToSwitchGoverningType()
        {
            string source = @"
using System;

class Test
{
    void M(byte number)
    {
        /*<bind>*/switch (number)
        {
            case 0:
                break;
            case 1:
                break;
        }/*</bind>*/
    }
}
";
            string expectedOperationTree = @"
ISwitchStatement (2 cases) (OperationKind.SwitchStatement) (Syntax: 'switch (num ... }')
  Switch expression: 
    IParameterReferenceExpression: number (OperationKind.ParameterReferenceExpression, Type: System.Byte) (Syntax: 'number')
  Sections:
      ISwitchCase (1 case clauses, 1 statements) (OperationKind.SwitchCase) (Syntax: 'case 0: ... break;')
          Clauses:
              ISingleValueCaseClause (CaseKind.SingleValue) (OperationKind.CaseClause) (Syntax: 'case 0:')
                Value: 
                  IConversionExpression (Implicit, TryCast: False, Unchecked) (OperationKind.ConversionExpression, Type: System.Byte, Constant: 0, IsImplicit) (Syntax: '0')
                    Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                    Operand: 
                      ILiteralExpression (OperationKind.LiteralExpression, Type: System.Int32, Constant: 0) (Syntax: '0')
          Body:
              IBranchStatement (BranchKind.Break) (OperationKind.BranchStatement) (Syntax: 'break;')
      ISwitchCase (1 case clauses, 1 statements) (OperationKind.SwitchCase) (Syntax: 'case 1: ... break;')
          Clauses:
              ISingleValueCaseClause (CaseKind.SingleValue) (OperationKind.CaseClause) (Syntax: 'case 1:')
                Value: 
                  IConversionExpression (Implicit, TryCast: False, Unchecked) (OperationKind.ConversionExpression, Type: System.Byte, Constant: 1, IsImplicit) (Syntax: '1')
                    Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                    Operand: 
                      ILiteralExpression (OperationKind.LiteralExpression, Type: System.Int32, Constant: 1) (Syntax: '1')
          Body:
              IBranchStatement (BranchKind.Break) (OperationKind.BranchStatement) (Syntax: 'break;')
";
            var expectedDiagnostics = DiagnosticDescription.None;

            VerifyOperationTreeAndDiagnosticsForTest<SwitchStatementSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }

        [Fact, WorkItem(17600, "https://github.com/dotnet/roslyn/issues/17600")]
        public void ISwitchStatement_CaseLabelWithExplicitConversionToSwitchGoverningType()
        {
            string source = @"
using System;

class Test
{
    const double j = 2.2;
    void M(int number)
    {
        /*<bind>*/switch (number)
        {
            case (int)0.1:
                break;
            case (int)j:
                break;
        }/*</bind>*/
    }
}
";
            string expectedOperationTree = @"
ISwitchStatement (2 cases) (OperationKind.SwitchStatement) (Syntax: 'switch (num ... }')
  Switch expression: 
    IParameterReferenceExpression: number (OperationKind.ParameterReferenceExpression, Type: System.Int32) (Syntax: 'number')
  Sections:
      ISwitchCase (1 case clauses, 1 statements) (OperationKind.SwitchCase) (Syntax: 'case (int)0 ... break;')
          Clauses:
              ISingleValueCaseClause (CaseKind.SingleValue) (OperationKind.CaseClause) (Syntax: 'case (int)0.1:')
                Value: 
                  IConversionExpression (Explicit, TryCast: False, Unchecked) (OperationKind.ConversionExpression, Type: System.Int32, Constant: 0) (Syntax: '(int)0.1')
                    Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: True, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                    Operand: 
                      ILiteralExpression (OperationKind.LiteralExpression, Type: System.Double, Constant: 0.1) (Syntax: '0.1')
          Body:
              IBranchStatement (BranchKind.Break) (OperationKind.BranchStatement) (Syntax: 'break;')
      ISwitchCase (1 case clauses, 1 statements) (OperationKind.SwitchCase) (Syntax: 'case (int)j ... break;')
          Clauses:
              ISingleValueCaseClause (CaseKind.SingleValue) (OperationKind.CaseClause) (Syntax: 'case (int)j:')
                Value: 
                  IConversionExpression (Explicit, TryCast: False, Unchecked) (OperationKind.ConversionExpression, Type: System.Int32, Constant: 2) (Syntax: '(int)j')
                    Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: True, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                    Operand: 
                      IFieldReferenceExpression: System.Double Test.j (Static) (OperationKind.FieldReferenceExpression, Type: System.Double, Constant: 2.2) (Syntax: 'j')
                        Instance Receiver: 
                          null
          Body:
              IBranchStatement (BranchKind.Break) (OperationKind.BranchStatement) (Syntax: 'break;')
";
            var expectedDiagnostics = DiagnosticDescription.None;

            VerifyOperationTreeAndDiagnosticsForTest<SwitchStatementSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }

        [Fact, WorkItem(17600, "https://github.com/dotnet/roslyn/issues/17600")]
        public void ISwitchStatement_Nested()
        {
            string source = @"
using System;

class Test
{
    void F()
    {
        int[] array = { 4, 10, 14 };
        /*<bind>*/switch (array[0])
        {
            case 3:
                Console.WriteLine(3); // Not reached.
                break;

            case 4:
                Console.WriteLine(4);
                // ... Use nested switch.
                switch (array[1])
                {
                    case 10:
                        Console.WriteLine(10);
                        break;
                }
                break;
        }/*</bind>*/
    }
}
";
            string expectedOperationTree = @"
ISwitchStatement (2 cases) (OperationKind.SwitchStatement) (Syntax: 'switch (arr ... }')
  Switch expression: 
    IArrayElementReferenceExpression (OperationKind.ArrayElementReferenceExpression, Type: System.Int32) (Syntax: 'array[0]')
      Array reference: 
        ILocalReferenceExpression: array (OperationKind.LocalReferenceExpression, Type: System.Int32[]) (Syntax: 'array')
      Indices(1):
          ILiteralExpression (OperationKind.LiteralExpression, Type: System.Int32, Constant: 0) (Syntax: '0')
  Sections:
      ISwitchCase (1 case clauses, 2 statements) (OperationKind.SwitchCase) (Syntax: 'case 3: ... break;')
          Clauses:
              ISingleValueCaseClause (CaseKind.SingleValue) (OperationKind.CaseClause) (Syntax: 'case 3:')
                Value: 
                  ILiteralExpression (OperationKind.LiteralExpression, Type: System.Int32, Constant: 3) (Syntax: '3')
          Body:
              IExpressionStatement (OperationKind.ExpressionStatement) (Syntax: 'Console.WriteLine(3);')
                Expression: 
                  IInvocationExpression (void System.Console.WriteLine(System.Int32 value)) (OperationKind.InvocationExpression, Type: System.Void) (Syntax: 'Console.WriteLine(3)')
                    Instance Receiver: 
                      null
                    Arguments(1):
                        IArgument (ArgumentKind.Explicit, Matching Parameter: value) (OperationKind.Argument) (Syntax: '3')
                          ILiteralExpression (OperationKind.LiteralExpression, Type: System.Int32, Constant: 3) (Syntax: '3')
                          InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                          OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
              IBranchStatement (BranchKind.Break) (OperationKind.BranchStatement) (Syntax: 'break;')
      ISwitchCase (1 case clauses, 3 statements) (OperationKind.SwitchCase) (Syntax: 'case 4: ... break;')
          Clauses:
              ISingleValueCaseClause (CaseKind.SingleValue) (OperationKind.CaseClause) (Syntax: 'case 4:')
                Value: 
                  ILiteralExpression (OperationKind.LiteralExpression, Type: System.Int32, Constant: 4) (Syntax: '4')
          Body:
              IExpressionStatement (OperationKind.ExpressionStatement) (Syntax: 'Console.WriteLine(4);')
                Expression: 
                  IInvocationExpression (void System.Console.WriteLine(System.Int32 value)) (OperationKind.InvocationExpression, Type: System.Void) (Syntax: 'Console.WriteLine(4)')
                    Instance Receiver: 
                      null
                    Arguments(1):
                        IArgument (ArgumentKind.Explicit, Matching Parameter: value) (OperationKind.Argument) (Syntax: '4')
                          ILiteralExpression (OperationKind.LiteralExpression, Type: System.Int32, Constant: 4) (Syntax: '4')
                          InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                          OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
              ISwitchStatement (1 cases) (OperationKind.SwitchStatement) (Syntax: 'switch (arr ... }')
                Switch expression: 
                  IArrayElementReferenceExpression (OperationKind.ArrayElementReferenceExpression, Type: System.Int32) (Syntax: 'array[1]')
                    Array reference: 
                      ILocalReferenceExpression: array (OperationKind.LocalReferenceExpression, Type: System.Int32[]) (Syntax: 'array')
                    Indices(1):
                        ILiteralExpression (OperationKind.LiteralExpression, Type: System.Int32, Constant: 1) (Syntax: '1')
                Sections:
                    ISwitchCase (1 case clauses, 2 statements) (OperationKind.SwitchCase) (Syntax: 'case 10: ... break;')
                        Clauses:
                            ISingleValueCaseClause (CaseKind.SingleValue) (OperationKind.CaseClause) (Syntax: 'case 10:')
                              Value: 
                                ILiteralExpression (OperationKind.LiteralExpression, Type: System.Int32, Constant: 10) (Syntax: '10')
                        Body:
                            IExpressionStatement (OperationKind.ExpressionStatement) (Syntax: 'Console.WriteLine(10);')
                              Expression: 
                                IInvocationExpression (void System.Console.WriteLine(System.Int32 value)) (OperationKind.InvocationExpression, Type: System.Void) (Syntax: 'Console.WriteLine(10)')
                                  Instance Receiver: 
                                    null
                                  Arguments(1):
                                      IArgument (ArgumentKind.Explicit, Matching Parameter: value) (OperationKind.Argument) (Syntax: '10')
                                        ILiteralExpression (OperationKind.LiteralExpression, Type: System.Int32, Constant: 10) (Syntax: '10')
                                        InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                                        OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                            IBranchStatement (BranchKind.Break) (OperationKind.BranchStatement) (Syntax: 'break;')
              IBranchStatement (BranchKind.Break) (OperationKind.BranchStatement) (Syntax: 'break;')
";
            var expectedDiagnostics = DiagnosticDescription.None;

            VerifyOperationTreeAndDiagnosticsForTest<SwitchStatementSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }

        [Fact, WorkItem(17600, "https://github.com/dotnet/roslyn/issues/17600")]
        public void ISwitchStatement_FallThroughError()
        {
            string source = @"
using System;

class Program
{
    void F()
    {
        int value = 0;
        /*<bind>*/switch (value)
        {
            case 0:
                Console.WriteLine(""Zero"");
            case 1:
                Console.WriteLine(""One"");
                break;
        }/*</bind>*/
    }
}
";
            string expectedOperationTree = @"
ISwitchStatement (2 cases) (OperationKind.SwitchStatement, IsInvalid) (Syntax: 'switch (val ... }')
  Switch expression: 
    ILocalReferenceExpression: value (OperationKind.LocalReferenceExpression, Type: System.Int32) (Syntax: 'value')
  Sections:
      ISwitchCase (1 case clauses, 1 statements) (OperationKind.SwitchCase, IsInvalid) (Syntax: 'case 0: ... ne(""Zero"");')
          Clauses:
              ISingleValueCaseClause (CaseKind.SingleValue) (OperationKind.CaseClause, IsInvalid) (Syntax: 'case 0:')
                Value: 
                  ILiteralExpression (OperationKind.LiteralExpression, Type: System.Int32, Constant: 0, IsInvalid) (Syntax: '0')
          Body:
              IExpressionStatement (OperationKind.ExpressionStatement) (Syntax: 'Console.Wri ... ne(""Zero"");')
                Expression: 
                  IInvocationExpression (void System.Console.WriteLine(System.String value)) (OperationKind.InvocationExpression, Type: System.Void) (Syntax: 'Console.Wri ... ine(""Zero"")')
                    Instance Receiver: 
                      null
                    Arguments(1):
                        IArgument (ArgumentKind.Explicit, Matching Parameter: value) (OperationKind.Argument) (Syntax: '""Zero""')
                          ILiteralExpression (OperationKind.LiteralExpression, Type: System.String, Constant: ""Zero"") (Syntax: '""Zero""')
                          InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                          OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
      ISwitchCase (1 case clauses, 2 statements) (OperationKind.SwitchCase) (Syntax: 'case 1: ... break;')
          Clauses:
              ISingleValueCaseClause (CaseKind.SingleValue) (OperationKind.CaseClause) (Syntax: 'case 1:')
                Value: 
                  ILiteralExpression (OperationKind.LiteralExpression, Type: System.Int32, Constant: 1) (Syntax: '1')
          Body:
              IExpressionStatement (OperationKind.ExpressionStatement) (Syntax: 'Console.Wri ... ine(""One"");')
                Expression: 
                  IInvocationExpression (void System.Console.WriteLine(System.String value)) (OperationKind.InvocationExpression, Type: System.Void) (Syntax: 'Console.WriteLine(""One"")')
                    Instance Receiver: 
                      null
                    Arguments(1):
                        IArgument (ArgumentKind.Explicit, Matching Parameter: value) (OperationKind.Argument) (Syntax: '""One""')
                          ILiteralExpression (OperationKind.LiteralExpression, Type: System.String, Constant: ""One"") (Syntax: '""One""')
                          InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                          OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
              IBranchStatement (BranchKind.Break) (OperationKind.BranchStatement) (Syntax: 'break;')
";

            var expectedDiagnostics = new DiagnosticDescription[] {
                // CS0163: Control cannot fall through from one case label ('case 0:') to another
                //             case 0:
                Diagnostic(ErrorCode.ERR_SwitchFallThrough, "case 0:").WithArguments("case 0:").WithLocation(11, 13)
            };

            VerifyOperationTreeAndDiagnosticsForTest<SwitchStatementSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }

        [Fact, WorkItem(17600, "https://github.com/dotnet/roslyn/issues/17600")]
        public void ISwitchStatement_DuplicateCaseLabels()
        {
            string source = @"
using System;

class Program
{
    void F()
    {
        short number = 0;
        /*<bind>*/switch (number)
        {
            case 0:
                Console.WriteLine(""ZERO"");
                return;
            case 0:
                Console.WriteLine(""ZERO"");
                return;
            case 1:
                Console.WriteLine(""ONE"");
                return;
        }/*</bind>*/
    }
}
";
            string expectedOperationTree = @"
ISwitchStatement (3 cases) (OperationKind.SwitchStatement, IsInvalid) (Syntax: 'switch (num ... }')
  Switch expression: 
    ILocalReferenceExpression: number (OperationKind.LocalReferenceExpression, Type: System.Int16) (Syntax: 'number')
  Sections:
      ISwitchCase (1 case clauses, 2 statements) (OperationKind.SwitchCase) (Syntax: 'case 0: ... return;')
          Clauses:
              ISingleValueCaseClause (CaseKind.SingleValue) (OperationKind.CaseClause) (Syntax: 'case 0:')
                Value: 
                  IConversionExpression (Implicit, TryCast: False, Unchecked) (OperationKind.ConversionExpression, Type: System.Int16, Constant: 0, IsImplicit) (Syntax: '0')
                    Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                    Operand: 
                      ILiteralExpression (OperationKind.LiteralExpression, Type: System.Int32, Constant: 0) (Syntax: '0')
          Body:
              IExpressionStatement (OperationKind.ExpressionStatement) (Syntax: 'Console.Wri ... ne(""ZERO"");')
                Expression: 
                  IInvocationExpression (void System.Console.WriteLine(System.String value)) (OperationKind.InvocationExpression, Type: System.Void) (Syntax: 'Console.Wri ... ine(""ZERO"")')
                    Instance Receiver: 
                      null
                    Arguments(1):
                        IArgument (ArgumentKind.Explicit, Matching Parameter: value) (OperationKind.Argument) (Syntax: '""ZERO""')
                          ILiteralExpression (OperationKind.LiteralExpression, Type: System.String, Constant: ""ZERO"") (Syntax: '""ZERO""')
                          InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                          OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
              IReturnStatement (OperationKind.ReturnStatement) (Syntax: 'return;')
                ReturnedValue: 
                  null
      ISwitchCase (1 case clauses, 2 statements) (OperationKind.SwitchCase, IsInvalid) (Syntax: 'case 0: ... return;')
          Clauses:
              ISingleValueCaseClause (CaseKind.SingleValue) (OperationKind.CaseClause, IsInvalid) (Syntax: 'case 0:')
                Value: 
                  IConversionExpression (Implicit, TryCast: False, Unchecked) (OperationKind.ConversionExpression, Type: System.Int16, Constant: 0, IsInvalid, IsImplicit) (Syntax: '0')
                    Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                    Operand: 
                      ILiteralExpression (OperationKind.LiteralExpression, Type: System.Int32, Constant: 0, IsInvalid) (Syntax: '0')
          Body:
              IExpressionStatement (OperationKind.ExpressionStatement) (Syntax: 'Console.Wri ... ne(""ZERO"");')
                Expression: 
                  IInvocationExpression (void System.Console.WriteLine(System.String value)) (OperationKind.InvocationExpression, Type: System.Void) (Syntax: 'Console.Wri ... ine(""ZERO"")')
                    Instance Receiver: 
                      null
                    Arguments(1):
                        IArgument (ArgumentKind.Explicit, Matching Parameter: value) (OperationKind.Argument) (Syntax: '""ZERO""')
                          ILiteralExpression (OperationKind.LiteralExpression, Type: System.String, Constant: ""ZERO"") (Syntax: '""ZERO""')
                          InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                          OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
              IReturnStatement (OperationKind.ReturnStatement) (Syntax: 'return;')
                ReturnedValue: 
                  null
      ISwitchCase (1 case clauses, 2 statements) (OperationKind.SwitchCase) (Syntax: 'case 1: ... return;')
          Clauses:
              ISingleValueCaseClause (CaseKind.SingleValue) (OperationKind.CaseClause) (Syntax: 'case 1:')
                Value: 
                  IConversionExpression (Implicit, TryCast: False, Unchecked) (OperationKind.ConversionExpression, Type: System.Int16, Constant: 1, IsImplicit) (Syntax: '1')
                    Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                    Operand: 
                      ILiteralExpression (OperationKind.LiteralExpression, Type: System.Int32, Constant: 1) (Syntax: '1')
          Body:
              IExpressionStatement (OperationKind.ExpressionStatement) (Syntax: 'Console.Wri ... ine(""ONE"");')
                Expression: 
                  IInvocationExpression (void System.Console.WriteLine(System.String value)) (OperationKind.InvocationExpression, Type: System.Void) (Syntax: 'Console.WriteLine(""ONE"")')
                    Instance Receiver: 
                      null
                    Arguments(1):
                        IArgument (ArgumentKind.Explicit, Matching Parameter: value) (OperationKind.Argument) (Syntax: '""ONE""')
                          ILiteralExpression (OperationKind.LiteralExpression, Type: System.String, Constant: ""ONE"") (Syntax: '""ONE""')
                          InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                          OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
              IReturnStatement (OperationKind.ReturnStatement) (Syntax: 'return;')
                ReturnedValue: 
                  null
";

            var expectedDiagnostics = new DiagnosticDescription[] {
                // CS0152: The switch statement contains multiple cases with the label value '0'
                //             case 0:
                Diagnostic(ErrorCode.ERR_DuplicateCaseLabel, "case 0:").WithArguments("0").WithLocation(14, 13)
            };

            VerifyOperationTreeAndDiagnosticsForTest<SwitchStatementSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }

        [Fact, WorkItem(17600, "https://github.com/dotnet/roslyn/issues/17600")]
        public void ISwitchStatement_ConstantValueRequired()
        {
            string source = @"
using System;

class Program
{
    void F()
    {
        int number = 0;
        int test = 10;
        /*<bind>*/switch (number)
        {
            case test + 1:
                Console.WriteLine(100);
                return;
            case 0:
                Console.WriteLine(0);
                return;
        }/*</bind>*/
    }
}
";
            string expectedOperationTree = @"
ISwitchStatement (2 cases) (OperationKind.SwitchStatement, IsInvalid) (Syntax: 'switch (num ... }')
  Switch expression: 
    ILocalReferenceExpression: number (OperationKind.LocalReferenceExpression, Type: System.Int32) (Syntax: 'number')
  Sections:
      ISwitchCase (1 case clauses, 2 statements) (OperationKind.SwitchCase, IsInvalid) (Syntax: 'case test + ... return;')
          Clauses:
              ISingleValueCaseClause (CaseKind.SingleValue) (OperationKind.CaseClause, IsInvalid) (Syntax: 'case test + 1:')
                Value: 
                  IBinaryOperatorExpression (BinaryOperatorKind.Add) (OperationKind.BinaryOperatorExpression, Type: System.Int32, IsInvalid) (Syntax: 'test + 1')
                    Left: 
                      ILocalReferenceExpression: test (OperationKind.LocalReferenceExpression, Type: System.Int32, IsInvalid) (Syntax: 'test')
                    Right: 
                      ILiteralExpression (OperationKind.LiteralExpression, Type: System.Int32, Constant: 1, IsInvalid) (Syntax: '1')
          Body:
              IExpressionStatement (OperationKind.ExpressionStatement) (Syntax: 'Console.WriteLine(100);')
                Expression: 
                  IInvocationExpression (void System.Console.WriteLine(System.Int32 value)) (OperationKind.InvocationExpression, Type: System.Void) (Syntax: 'Console.WriteLine(100)')
                    Instance Receiver: 
                      null
                    Arguments(1):
                        IArgument (ArgumentKind.Explicit, Matching Parameter: value) (OperationKind.Argument) (Syntax: '100')
                          ILiteralExpression (OperationKind.LiteralExpression, Type: System.Int32, Constant: 100) (Syntax: '100')
                          InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                          OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
              IReturnStatement (OperationKind.ReturnStatement) (Syntax: 'return;')
                ReturnedValue: 
                  null
      ISwitchCase (1 case clauses, 2 statements) (OperationKind.SwitchCase) (Syntax: 'case 0: ... return;')
          Clauses:
              ISingleValueCaseClause (CaseKind.SingleValue) (OperationKind.CaseClause) (Syntax: 'case 0:')
                Value: 
                  ILiteralExpression (OperationKind.LiteralExpression, Type: System.Int32, Constant: 0) (Syntax: '0')
          Body:
              IExpressionStatement (OperationKind.ExpressionStatement) (Syntax: 'Console.WriteLine(0);')
                Expression: 
                  IInvocationExpression (void System.Console.WriteLine(System.Int32 value)) (OperationKind.InvocationExpression, Type: System.Void) (Syntax: 'Console.WriteLine(0)')
                    Instance Receiver: 
                      null
                    Arguments(1):
                        IArgument (ArgumentKind.Explicit, Matching Parameter: value) (OperationKind.Argument) (Syntax: '0')
                          ILiteralExpression (OperationKind.LiteralExpression, Type: System.Int32, Constant: 0) (Syntax: '0')
                          InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                          OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
              IReturnStatement (OperationKind.ReturnStatement) (Syntax: 'return;')
                ReturnedValue: 
                  null
";
            var expectedDiagnostics = new DiagnosticDescription[] {
                // CS0150: A constant value is expected
                //             case test + 1:
                Diagnostic(ErrorCode.ERR_ConstantExpected, "test + 1").WithLocation(12, 18)
            };

            VerifyOperationTreeAndDiagnosticsForTest<SwitchStatementSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }

        [Fact, WorkItem(17600, "https://github.com/dotnet/roslyn/issues/17600")]
        public void ISwitchStatement_EnumType()
        {
            string source = @"
using System;
class Program
{
    public enum State { Active = 1, Inactive = 2 }
    void F()
    {
        State state = State.Active;

        /*<bind>*/switch (state)
        {
            case State.Active:
                break;
        }/*</bind>*/
    }
}
";
            string expectedOperationTree = @"
ISwitchStatement (1 cases) (OperationKind.SwitchStatement) (Syntax: 'switch (sta ... }')
  Switch expression: 
    ILocalReferenceExpression: state (OperationKind.LocalReferenceExpression, Type: Program.State) (Syntax: 'state')
  Sections:
      ISwitchCase (1 case clauses, 1 statements) (OperationKind.SwitchCase) (Syntax: 'case State. ... break;')
          Clauses:
              ISingleValueCaseClause (CaseKind.SingleValue) (OperationKind.CaseClause) (Syntax: 'case State.Active:')
                Value: 
                  IFieldReferenceExpression: Program.State.Active (Static) (OperationKind.FieldReferenceExpression, Type: Program.State, Constant: 1) (Syntax: 'State.Active')
                    Instance Receiver: 
                      null
          Body:
              IBranchStatement (BranchKind.Break) (OperationKind.BranchStatement) (Syntax: 'break;')
";
            var expectedDiagnostics = DiagnosticDescription.None;

            VerifyOperationTreeAndDiagnosticsForTest<SwitchStatementSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }

        [Fact, WorkItem(17600, "https://github.com/dotnet/roslyn/issues/17600")]
        public void ISwitchStatement_DefaultLabel()
        {
            string source = @"
using System;

class Test
{
    void F(int number)
    {
        /*<bind>*/switch (number)
        {
            default:
                break;
        }/*</bind>*/
    }
}
";
            string expectedOperationTree = @"
ISwitchStatement (1 cases) (OperationKind.SwitchStatement) (Syntax: 'switch (num ... }')
  Switch expression: 
    IParameterReferenceExpression: number (OperationKind.ParameterReferenceExpression, Type: System.Int32) (Syntax: 'number')
  Sections:
      ISwitchCase (1 case clauses, 1 statements) (OperationKind.SwitchCase) (Syntax: 'default: ... break;')
          Clauses:
              IDefaultCaseClause (CaseKind.Default) (OperationKind.CaseClause) (Syntax: 'default:')
          Body:
              IBranchStatement (BranchKind.Break) (OperationKind.BranchStatement) (Syntax: 'break;')
";
            var expectedDiagnostics = DiagnosticDescription.None;

            VerifyOperationTreeAndDiagnosticsForTest<SwitchStatementSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }

        [Fact, WorkItem(17600, "https://github.com/dotnet/roslyn/issues/17600")]
        public void ISwitchStatement_GotoCaseStatement()
        {
            string source = @"
using System;

class Test
{
    void M(int number)
    {
        /*<bind>*/switch (number)
        {
            case 0:
                goto case 1;
            case 1:
                break;
        }/*</bind>*/
    }
}
";
            string expectedOperationTree = @"
ISwitchStatement (2 cases) (OperationKind.SwitchStatement) (Syntax: 'switch (num ... }')
  Switch expression: 
    IParameterReferenceExpression: number (OperationKind.ParameterReferenceExpression, Type: System.Int32) (Syntax: 'number')
  Sections:
      ISwitchCase (1 case clauses, 1 statements) (OperationKind.SwitchCase) (Syntax: 'case 0: ... oto case 1;')
          Clauses:
              ISingleValueCaseClause (CaseKind.SingleValue) (OperationKind.CaseClause) (Syntax: 'case 0:')
                Value: 
                  ILiteralExpression (OperationKind.LiteralExpression, Type: System.Int32, Constant: 0) (Syntax: '0')
          Body:
              IBranchStatement (BranchKind.GoTo, Label: case 1:) (OperationKind.BranchStatement) (Syntax: 'goto case 1;')
      ISwitchCase (1 case clauses, 1 statements) (OperationKind.SwitchCase) (Syntax: 'case 1: ... break;')
          Clauses:
              ISingleValueCaseClause (CaseKind.SingleValue) (OperationKind.CaseClause) (Syntax: 'case 1:')
                Value: 
                  ILiteralExpression (OperationKind.LiteralExpression, Type: System.Int32, Constant: 1) (Syntax: '1')
          Body:
              IBranchStatement (BranchKind.Break) (OperationKind.BranchStatement) (Syntax: 'break;')
";
            var expectedDiagnostics = DiagnosticDescription.None;

            VerifyOperationTreeAndDiagnosticsForTest<SwitchStatementSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }

        [Fact, WorkItem(17600, "https://github.com/dotnet/roslyn/issues/17600")]
        public void ISwitchStatement_GoverningExpressionWithLocalDeclarataion()
        {
            string source = @"
using System;

class Test
{
    void M()
    {
        /*<bind>*/switch (M2(out var number))
        {
            case 0:
                break;
        }/*</bind>*/
    }

    int M2(out int number)
    {
        number = 0;
        return 1;
    }
}
";
            string expectedOperationTree = @"
ISwitchStatement (1 cases) (OperationKind.SwitchStatement) (Syntax: 'switch (M2( ... }')
  Switch expression: 
    IInvocationExpression ( System.Int32 Test.M2(out System.Int32 number)) (OperationKind.InvocationExpression, Type: System.Int32) (Syntax: 'M2(out var number)')
      Instance Receiver: 
        IInstanceReferenceExpression (OperationKind.InstanceReferenceExpression, Type: Test, IsImplicit) (Syntax: 'M2')
      Arguments(1):
          IArgument (ArgumentKind.Explicit, Matching Parameter: number) (OperationKind.Argument) (Syntax: 'out var number')
            ILocalReferenceExpression: number (IsDeclaration: True) (OperationKind.LocalReferenceExpression, Type: System.Int32) (Syntax: 'var number')
            InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
            OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
  Sections:
      ISwitchCase (1 case clauses, 1 statements) (OperationKind.SwitchCase) (Syntax: 'case 0: ... break;')
          Clauses:
              ISingleValueCaseClause (CaseKind.SingleValue) (OperationKind.CaseClause) (Syntax: 'case 0:')
                Value: 
                  ILiteralExpression (OperationKind.LiteralExpression, Type: System.Int32, Constant: 0) (Syntax: '0')
          Body:
              IBranchStatement (BranchKind.Break) (OperationKind.BranchStatement) (Syntax: 'break;')
";
            var expectedDiagnostics = DiagnosticDescription.None;

            VerifyOperationTreeAndDiagnosticsForTest<SwitchStatementSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }

        [Fact, WorkItem(17600, "https://github.com/dotnet/roslyn/issues/17600")]
        public void ISwitchStatement_SwitchSectionWithLocalDeclarataion()
        {
            string source = @"
using System;

class Test
{
    int M(int number)
    {
        /*<bind>*/switch (number)
        {
            case 0:
            case 1:
                int number2 = 2;
                return number2;
        }/*</bind>*/

        return 0;
    }
}
";
            string expectedOperationTree = @"
ISwitchStatement (1 cases) (OperationKind.SwitchStatement) (Syntax: 'switch (num ... }')
  Switch expression: 
    IParameterReferenceExpression: number (OperationKind.ParameterReferenceExpression, Type: System.Int32) (Syntax: 'number')
  Sections:
      ISwitchCase (2 case clauses, 2 statements) (OperationKind.SwitchCase) (Syntax: 'case 0: ... rn number2;')
          Clauses:
              ISingleValueCaseClause (CaseKind.SingleValue) (OperationKind.CaseClause) (Syntax: 'case 0:')
                Value: 
                  ILiteralExpression (OperationKind.LiteralExpression, Type: System.Int32, Constant: 0) (Syntax: '0')
              ISingleValueCaseClause (CaseKind.SingleValue) (OperationKind.CaseClause) (Syntax: 'case 1:')
                Value: 
                  ILiteralExpression (OperationKind.LiteralExpression, Type: System.Int32, Constant: 1) (Syntax: '1')
          Body:
              IVariableDeclarationStatement (1 declarations) (OperationKind.VariableDeclarationStatement) (Syntax: 'int number2 = 2;')
                IVariableDeclaration (1 variables) (OperationKind.VariableDeclaration) (Syntax: 'number2 = 2')
                  Variables: Local_1: System.Int32 number2
                  Initializer: 
                    ILiteralExpression (OperationKind.LiteralExpression, Type: System.Int32, Constant: 2) (Syntax: '2')
              IReturnStatement (OperationKind.ReturnStatement) (Syntax: 'return number2;')
                ReturnedValue: 
                  ILocalReferenceExpression: number2 (OperationKind.LocalReferenceExpression, Type: System.Int32) (Syntax: 'number2')
";
            var expectedDiagnostics = DiagnosticDescription.None;

            VerifyOperationTreeAndDiagnosticsForTest<SwitchStatementSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }

        [Fact, WorkItem(17600, "https://github.com/dotnet/roslyn/issues/17600")]
        public void ISwitchStatement_SimpleCaseClauseWithLocalDeclarataion()
        {
            string source = @"
using System;

class Test
{
    void M(int number)
    {
        /*<bind>*/switch (number)
        {
            case 0:
            case M2(out var number2):
                break;
        }/*</bind>*/
    }

    int M2(out int number)
    {
        number = 0;
        return 1;
    }
}
";
            string expectedOperationTree = @"
ISwitchStatement (1 cases) (OperationKind.SwitchStatement, IsInvalid) (Syntax: 'switch (num ... }')
  Switch expression: 
    IParameterReferenceExpression: number (OperationKind.ParameterReferenceExpression, Type: System.Int32) (Syntax: 'number')
  Sections:
      ISwitchCase (2 case clauses, 1 statements) (OperationKind.SwitchCase, IsInvalid) (Syntax: 'case 0: ... break;')
          Clauses:
              ISingleValueCaseClause (CaseKind.SingleValue) (OperationKind.CaseClause) (Syntax: 'case 0:')
                Value: 
                  ILiteralExpression (OperationKind.LiteralExpression, Type: System.Int32, Constant: 0) (Syntax: '0')
              ISingleValueCaseClause (CaseKind.SingleValue) (OperationKind.CaseClause, IsInvalid) (Syntax: 'case M2(out ... r number2):')
                Value: 
                  IInvocationExpression ( System.Int32 Test.M2(out System.Int32 number)) (OperationKind.InvocationExpression, Type: System.Int32, IsInvalid) (Syntax: 'M2(out var number2)')
                    Instance Receiver: 
                      IInstanceReferenceExpression (OperationKind.InstanceReferenceExpression, Type: Test, IsInvalid, IsImplicit) (Syntax: 'M2')
                    Arguments(1):
                        IArgument (ArgumentKind.Explicit, Matching Parameter: number) (OperationKind.Argument, IsInvalid) (Syntax: 'out var number2')
                          ILocalReferenceExpression: number2 (IsDeclaration: True) (OperationKind.LocalReferenceExpression, Type: System.Int32, IsInvalid) (Syntax: 'var number2')
                          InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                          OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
          Body:
              IBranchStatement (BranchKind.Break) (OperationKind.BranchStatement) (Syntax: 'break;')
";
            var expectedDiagnostics = new DiagnosticDescription[] {
                // CS0150: A constant value is expected
                //             case M2(out var number2):
                Diagnostic(ErrorCode.ERR_ConstantExpected, "M2(out var number2)").WithLocation(11, 18)
            };

            VerifyOperationTreeAndDiagnosticsForTest<SwitchStatementSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }

        [Fact, WorkItem(17600, "https://github.com/dotnet/roslyn/issues/17600")]
        public void ISwitchStatement_DuplicateLocalDeclarationAcrossSwitchSections()
        {
            string source = @"
using System;

class Test
{
    int M(int number)
    {
        /*<bind>*/switch (number)
        {
            case 0:
                int number2 = 1;
                return number2;
            case 1:
                int number2 = 2;
                return number2;
        }/*</bind>*/

        return 0;
    }
}
";
            string expectedOperationTree = @"
ISwitchStatement (2 cases) (OperationKind.SwitchStatement, IsInvalid) (Syntax: 'switch (num ... }')
  Switch expression: 
    IParameterReferenceExpression: number (OperationKind.ParameterReferenceExpression, Type: System.Int32) (Syntax: 'number')
  Sections:
      ISwitchCase (1 case clauses, 2 statements) (OperationKind.SwitchCase) (Syntax: 'case 0: ... rn number2;')
          Clauses:
              ISingleValueCaseClause (CaseKind.SingleValue) (OperationKind.CaseClause) (Syntax: 'case 0:')
                Value: 
                  ILiteralExpression (OperationKind.LiteralExpression, Type: System.Int32, Constant: 0) (Syntax: '0')
          Body:
              IVariableDeclarationStatement (1 declarations) (OperationKind.VariableDeclarationStatement) (Syntax: 'int number2 = 1;')
                IVariableDeclaration (1 variables) (OperationKind.VariableDeclaration) (Syntax: 'number2 = 1')
                  Variables: Local_1: System.Int32 number2
                  Initializer: 
                    ILiteralExpression (OperationKind.LiteralExpression, Type: System.Int32, Constant: 1) (Syntax: '1')
              IReturnStatement (OperationKind.ReturnStatement) (Syntax: 'return number2;')
                ReturnedValue: 
                  ILocalReferenceExpression: number2 (OperationKind.LocalReferenceExpression, Type: System.Int32) (Syntax: 'number2')
      ISwitchCase (1 case clauses, 2 statements) (OperationKind.SwitchCase, IsInvalid) (Syntax: 'case 1: ... rn number2;')
          Clauses:
              ISingleValueCaseClause (CaseKind.SingleValue) (OperationKind.CaseClause) (Syntax: 'case 1:')
                Value: 
                  ILiteralExpression (OperationKind.LiteralExpression, Type: System.Int32, Constant: 1) (Syntax: '1')
          Body:
              IVariableDeclarationStatement (1 declarations) (OperationKind.VariableDeclarationStatement, IsInvalid) (Syntax: 'int number2 = 2;')
                IVariableDeclaration (1 variables) (OperationKind.VariableDeclaration, IsInvalid) (Syntax: 'number2 = 2')
                  Variables: Local_1: System.Int32 number2
                  Initializer: 
                    ILiteralExpression (OperationKind.LiteralExpression, Type: System.Int32, Constant: 2) (Syntax: '2')
              IReturnStatement (OperationKind.ReturnStatement, IsInvalid) (Syntax: 'return number2;')
                ReturnedValue: 
                  ILocalReferenceExpression: number2 (OperationKind.LocalReferenceExpression, Type: System.Int32, IsInvalid) (Syntax: 'number2')
";
            var expectedDiagnostics = new DiagnosticDescription[] {
                // CS0128: A local variable or function named 'number2' is already defined in this scope
                //                 int number2 = 2;
                Diagnostic(ErrorCode.ERR_LocalDuplicate, "number2").WithArguments("number2").WithLocation(14, 21),
                // CS0165: Use of unassigned local variable 'number2'
                //                 return number2;
                Diagnostic(ErrorCode.ERR_UseDefViolation, "number2").WithArguments("number2").WithLocation(15, 24),
                // CS0219: The variable 'number2' is assigned but its value is never used
                //                 int number2 = 2;
                Diagnostic(ErrorCode.WRN_UnreferencedVarAssg, "number2").WithArguments("number2").WithLocation(14, 21)
            };

            VerifyOperationTreeAndDiagnosticsForTest<SwitchStatementSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }

        [Fact, WorkItem(17600, "https://github.com/dotnet/roslyn/issues/17600")]
        public void ISwitchStatement_NoExpression()
        {
            string source = @"
using System;

class Test
{
    void M()
    {
        /*<bind>*/switch ()
        {
            case 0:
                break;
        }/*</bind>*/
    }
}
";
            string expectedOperationTree = @"
ISwitchStatement (1 cases) (OperationKind.SwitchStatement, IsInvalid) (Syntax: 'switch () ... }')
  Switch expression: 
    IInvalidExpression (OperationKind.InvalidExpression, Type: null, IsInvalid, IsImplicit) (Syntax: '')
      Children(1):
          IInvalidExpression (OperationKind.InvalidExpression, Type: null, IsInvalid) (Syntax: '')
            Children(0)
  Sections:
      ISwitchCase (1 case clauses, 1 statements) (OperationKind.SwitchCase) (Syntax: 'case 0: ... break;')
          Clauses:
              IPatternCaseClause (Label Symbol: case 0:) (CaseKind.Pattern) (OperationKind.CaseClause) (Syntax: 'case 0:')
                Pattern: 
                  IConstantPattern (OperationKind.ConstantPattern) (Syntax: 'case 0:')
                    Value: 
                      ILiteralExpression (OperationKind.LiteralExpression, Type: System.Int32, Constant: 0) (Syntax: '0')
                Guard Expression: 
                  null
          Body:
              IBranchStatement (BranchKind.Break) (OperationKind.BranchStatement) (Syntax: 'break;')
";
            var expectedDiagnostics = new DiagnosticDescription[] {
                // CS1525: Invalid expression term ')'
                //         /*<bind>*/switch ()
                Diagnostic(ErrorCode.ERR_InvalidExprTerm, ")").WithArguments(")").WithLocation(8, 27)
            };

            VerifyOperationTreeAndDiagnosticsForTest<SwitchStatementSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }

        [Fact, WorkItem(17600, "https://github.com/dotnet/roslyn/issues/17600")]
        public void ISwitchStatement_NoSections()
        {
            string source = @"
using System;

class Test
{
    void M(int number)
    {
        /*<bind>*/switch (number)
        {
        }/*</bind>*/
    }
}
";
            string expectedOperationTree = @"
ISwitchStatement (0 cases) (OperationKind.SwitchStatement) (Syntax: 'switch (num ... }')
  Switch expression: 
    IParameterReferenceExpression: number (OperationKind.ParameterReferenceExpression, Type: System.Int32) (Syntax: 'number')
  Sections(0)
";
            var expectedDiagnostics = new DiagnosticDescription[] {
                // CS1522: Empty switch block
                //         {
                Diagnostic(ErrorCode.WRN_EmptySwitch, "{").WithLocation(9, 9)
            };

            VerifyOperationTreeAndDiagnosticsForTest<SwitchStatementSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }

        [Fact, WorkItem(17600, "https://github.com/dotnet/roslyn/issues/17600")]
        public void ISwitchStatement_GoverningExpressionWithErrorType()
        {
            string source = @"
using System;

class Test
{
    void M()
    {
        /*<bind>*/switch (number)
        {
            case 0:
                break;
        }/*</bind>*/
    }
}
";
            string expectedOperationTree = @"
ISwitchStatement (1 cases) (OperationKind.SwitchStatement, IsInvalid) (Syntax: 'switch (num ... }')
  Switch expression: 
    IInvalidExpression (OperationKind.InvalidExpression, Type: ?, IsInvalid, IsImplicit) (Syntax: 'number')
      Children(1):
          IInvalidExpression (OperationKind.InvalidExpression, Type: ?, IsInvalid) (Syntax: 'number')
            Children(0)
  Sections:
      ISwitchCase (1 case clauses, 1 statements) (OperationKind.SwitchCase) (Syntax: 'case 0: ... break;')
          Clauses:
              IPatternCaseClause (Label Symbol: case 0:) (CaseKind.Pattern) (OperationKind.CaseClause) (Syntax: 'case 0:')
                Pattern: 
                  IConstantPattern (OperationKind.ConstantPattern) (Syntax: 'case 0:')
                    Value: 
                      ILiteralExpression (OperationKind.LiteralExpression, Type: System.Int32, Constant: 0) (Syntax: '0')
                Guard Expression: 
                  null
          Body:
              IBranchStatement (BranchKind.Break) (OperationKind.BranchStatement) (Syntax: 'break;')
";
            var expectedDiagnostics = new DiagnosticDescription[] {
                // CS0103: The name 'number' does not exist in the current context
                //         /*<bind>*/switch (number)
                Diagnostic(ErrorCode.ERR_NameNotInContext, "number").WithArguments("number").WithLocation(8, 27)
            };

            VerifyOperationTreeAndDiagnosticsForTest<SwitchStatementSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }

        [Fact, WorkItem(17600, "https://github.com/dotnet/roslyn/issues/17600")]
        public void ISwitchStatement_GetOperationForGoverningExpression()
        {
            string source = @"
using System;

class Test
{
    void M(int number)
    {
        switch (/*<bind>*/number/*</bind>*/)
        {
            case 0:
                break;
        }
    }
}
";
            string expectedOperationTree = @"
IParameterReferenceExpression: number (OperationKind.ParameterReferenceExpression, Type: System.Int32) (Syntax: 'number')
";
            var expectedDiagnostics = DiagnosticDescription.None;

            VerifyOperationTreeAndDiagnosticsForTest<IdentifierNameSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }

        [Fact, WorkItem(17600, "https://github.com/dotnet/roslyn/issues/17600")]
        public void ISwitchStatement_GetOperationForSwitchSection()
        {
            string source = @"
using System;

class Test
{
    void M(int number)
    {
        switch (number)
        {
            /*<bind>*/case 0:
                break;/*</bind>*/
        }
    }
}
";
            string expectedOperationTree = @"
ISwitchCase (1 case clauses, 1 statements) (OperationKind.SwitchCase) (Syntax: 'case 0: ... break;')
    Clauses:
        ISingleValueCaseClause (CaseKind.SingleValue) (OperationKind.CaseClause) (Syntax: 'case 0:')
          Value: 
            ILiteralExpression (OperationKind.LiteralExpression, Type: System.Int32, Constant: 0) (Syntax: '0')
    Body:
        IBranchStatement (BranchKind.Break) (OperationKind.BranchStatement) (Syntax: 'break;')
";
            var expectedDiagnostics = DiagnosticDescription.None;

            VerifyOperationTreeAndDiagnosticsForTest<SwitchSectionSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }

        [Fact, WorkItem(17600, "https://github.com/dotnet/roslyn/issues/17600")]
        public void ISwitchStatement_GetOperationForCaseLabel()
        {
            string source = @"
using System;

class Test
{
    void M(int number)
    {
        switch (number)
        {
            /*<bind>*/case 0:/*</bind>*/
                break;
        }
    }
}
";
            string expectedOperationTree = @"
ISingleValueCaseClause (CaseKind.SingleValue) (OperationKind.CaseClause) (Syntax: 'case 0:')
  Value: 
    ILiteralExpression (OperationKind.LiteralExpression, Type: System.Int32, Constant: 0) (Syntax: '0')
";
            var expectedDiagnostics = DiagnosticDescription.None;

            VerifyOperationTreeAndDiagnosticsForTest<CaseSwitchLabelSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }
    }
}
