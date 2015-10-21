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
    static void Main()
    {
        int number = 1;
        /*<bind>*/switch (number)
        {
            case 0:
                Console.WriteLine(""0"");
                break;
            case 1:
                Console.WriteLine(""1"");
                break;
        }/*</bind>*/
    }
   
}
";
string expectedOperationTree = @"
ISwitchStatement (2 cases) (OperationKind.SwitchStatement) (Syntax: 'switch (num ... }')
  Switch expression: ILocalReferenceExpression: number (OperationKind.LocalReferenceExpression, Type: System.Int32) (Syntax: 'number')
  Sections:
      ISwitchCase (1 case clauses, 2 statements) (OperationKind.SwitchCase) (Syntax: 'case 0: ... break;')
          Clauses:
              ISingleValueCaseClause (Equality operator kind: BinaryOperationKind.IntegerEquals) (CaseKind.SingleValue) (OperationKind.SingleValueCaseClause) (Syntax: 'case 0:')
                Value: ILiteralExpression (Text: 0) (OperationKind.LiteralExpression, Type: System.Int32, Constant: 0) (Syntax: '0')
          Body:
              IExpressionStatement (OperationKind.ExpressionStatement) (Syntax: 'Console.WriteLine(""0"");')
                Expression: IInvocationExpression (void System.Console.WriteLine(System.String value)) (OperationKind.InvocationExpression, Type: System.Void) (Syntax: 'Console.WriteLine(""0"")')
                    Instance Receiver: null
                    Arguments(1):
                        IArgument (ArgumentKind.Explicit, Matching Parameter: value) (OperationKind.Argument) (Syntax: '""0""')
                          ILiteralExpression (OperationKind.LiteralExpression, Type: System.String, Constant: ""0"") (Syntax: '""0""')
                          InConversion: null
                          OutConversion: null
              IBranchStatement (BranchKind.Break) (OperationKind.BranchStatement) (Syntax: 'break;')
      ISwitchCase (1 case clauses, 2 statements) (OperationKind.SwitchCase) (Syntax: 'case 1: ... break;')
          Clauses:
              ISingleValueCaseClause (Equality operator kind: BinaryOperationKind.IntegerEquals) (CaseKind.SingleValue) (OperationKind.SingleValueCaseClause) (Syntax: 'case 1:')
                Value: ILiteralExpression (Text: 1) (OperationKind.LiteralExpression, Type: System.Int32, Constant: 1) (Syntax: '1')
          Body:
              IExpressionStatement (OperationKind.ExpressionStatement) (Syntax: 'Console.WriteLine(""1"");')
                Expression: IInvocationExpression (void System.Console.WriteLine(System.String value)) (OperationKind.InvocationExpression, Type: System.Void) (Syntax: 'Console.WriteLine(""1"")')
                    Instance Receiver: null
                    Arguments(1):
                        IArgument (ArgumentKind.Explicit, Matching Parameter: value) (OperationKind.Argument) (Syntax: '""1""')
                          ILiteralExpression (OperationKind.LiteralExpression, Type: System.String, Constant: ""1"") (Syntax: '""1""')
                          InConversion: null
                          OutConversion: null
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
    static void Main()
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
  Switch expression: IArrayElementReferenceExpression (OperationKind.ArrayElementReferenceExpression, Type: System.Int32) (Syntax: 'array[0]')
      Array reference: ILocalReferenceExpression: array (OperationKind.LocalReferenceExpression, Type: System.Int32[]) (Syntax: 'array')
      Indices(1):
          ILiteralExpression (Text: 0) (OperationKind.LiteralExpression, Type: System.Int32, Constant: 0) (Syntax: '0')
  Sections:
      ISwitchCase (1 case clauses, 2 statements) (OperationKind.SwitchCase) (Syntax: 'case 3: ... break;')
          Clauses:
              ISingleValueCaseClause (Equality operator kind: BinaryOperationKind.IntegerEquals) (CaseKind.SingleValue) (OperationKind.SingleValueCaseClause) (Syntax: 'case 3:')
                Value: ILiteralExpression (Text: 3) (OperationKind.LiteralExpression, Type: System.Int32, Constant: 3) (Syntax: '3')
          Body:
              IExpressionStatement (OperationKind.ExpressionStatement) (Syntax: 'Console.WriteLine(3);')
                Expression: IInvocationExpression (void System.Console.WriteLine(System.Int32 value)) (OperationKind.InvocationExpression, Type: System.Void) (Syntax: 'Console.WriteLine(3)')
                    Instance Receiver: null
                    Arguments(1):
                        IArgument (ArgumentKind.Explicit, Matching Parameter: value) (OperationKind.Argument) (Syntax: '3')
                          ILiteralExpression (Text: 3) (OperationKind.LiteralExpression, Type: System.Int32, Constant: 3) (Syntax: '3')
                          InConversion: null
                          OutConversion: null
              IBranchStatement (BranchKind.Break) (OperationKind.BranchStatement) (Syntax: 'break;')
      ISwitchCase (1 case clauses, 3 statements) (OperationKind.SwitchCase) (Syntax: 'case 4: ... break;')
          Clauses:
              ISingleValueCaseClause (Equality operator kind: BinaryOperationKind.IntegerEquals) (CaseKind.SingleValue) (OperationKind.SingleValueCaseClause) (Syntax: 'case 4:')
                Value: ILiteralExpression (Text: 4) (OperationKind.LiteralExpression, Type: System.Int32, Constant: 4) (Syntax: '4')
          Body:
              IExpressionStatement (OperationKind.ExpressionStatement) (Syntax: 'Console.WriteLine(4);')
                Expression: IInvocationExpression (void System.Console.WriteLine(System.Int32 value)) (OperationKind.InvocationExpression, Type: System.Void) (Syntax: 'Console.WriteLine(4)')
                    Instance Receiver: null
                    Arguments(1):
                        IArgument (ArgumentKind.Explicit, Matching Parameter: value) (OperationKind.Argument) (Syntax: '4')
                          ILiteralExpression (Text: 4) (OperationKind.LiteralExpression, Type: System.Int32, Constant: 4) (Syntax: '4')
                          InConversion: null
                          OutConversion: null
              ISwitchStatement (1 cases) (OperationKind.SwitchStatement) (Syntax: 'switch (arr ... }')
                Switch expression: IArrayElementReferenceExpression (OperationKind.ArrayElementReferenceExpression, Type: System.Int32) (Syntax: 'array[1]')
                    Array reference: ILocalReferenceExpression: array (OperationKind.LocalReferenceExpression, Type: System.Int32[]) (Syntax: 'array')
                    Indices(1):
                        ILiteralExpression (Text: 1) (OperationKind.LiteralExpression, Type: System.Int32, Constant: 1) (Syntax: '1')
                Sections:
                    ISwitchCase (1 case clauses, 2 statements) (OperationKind.SwitchCase) (Syntax: 'case 10: ... break;')
                        Clauses:
                            ISingleValueCaseClause (Equality operator kind: BinaryOperationKind.IntegerEquals) (CaseKind.SingleValue) (OperationKind.SingleValueCaseClause) (Syntax: 'case 10:')
                              Value: ILiteralExpression (Text: 10) (OperationKind.LiteralExpression, Type: System.Int32, Constant: 10) (Syntax: '10')
                        Body:
                            IExpressionStatement (OperationKind.ExpressionStatement) (Syntax: 'Console.WriteLine(10);')
                              Expression: IInvocationExpression (void System.Console.WriteLine(System.Int32 value)) (OperationKind.InvocationExpression, Type: System.Void) (Syntax: 'Console.WriteLine(10)')
                                  Instance Receiver: null
                                  Arguments(1):
                                      IArgument (ArgumentKind.Explicit, Matching Parameter: value) (OperationKind.Argument) (Syntax: '10')
                                        ILiteralExpression (Text: 10) (OperationKind.LiteralExpression, Type: System.Int32, Constant: 10) (Syntax: '10')
                                        InConversion: null
                                        OutConversion: null
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
    static void Main()
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
  Switch expression: ILocalReferenceExpression: value (OperationKind.LocalReferenceExpression, Type: System.Int32) (Syntax: 'value')
  Sections:
      ISwitchCase (1 case clauses, 1 statements) (OperationKind.SwitchCase, IsInvalid) (Syntax: 'case 0: ... ne(""Zero"");')
          Clauses:
              ISingleValueCaseClause (Equality operator kind: BinaryOperationKind.IntegerEquals) (CaseKind.SingleValue) (OperationKind.SingleValueCaseClause, IsInvalid) (Syntax: 'case 0:')
                Value: ILiteralExpression (Text: 0) (OperationKind.LiteralExpression, Type: System.Int32, Constant: 0, IsInvalid) (Syntax: '0')
          Body:
              IExpressionStatement (OperationKind.ExpressionStatement) (Syntax: 'Console.Wri ... ne(""Zero"");')
                Expression: IInvocationExpression (void System.Console.WriteLine(System.String value)) (OperationKind.InvocationExpression, Type: System.Void) (Syntax: 'Console.Wri ... ine(""Zero"")')
                    Instance Receiver: null
                    Arguments(1):
                        IArgument (ArgumentKind.Explicit, Matching Parameter: value) (OperationKind.Argument) (Syntax: '""Zero""')
                          ILiteralExpression (OperationKind.LiteralExpression, Type: System.String, Constant: ""Zero"") (Syntax: '""Zero""')
                          InConversion: null
                          OutConversion: null
      ISwitchCase (1 case clauses, 2 statements) (OperationKind.SwitchCase) (Syntax: 'case 1: ... break;')
          Clauses:
              ISingleValueCaseClause (Equality operator kind: BinaryOperationKind.IntegerEquals) (CaseKind.SingleValue) (OperationKind.SingleValueCaseClause) (Syntax: 'case 1:')
                Value: ILiteralExpression (Text: 1) (OperationKind.LiteralExpression, Type: System.Int32, Constant: 1) (Syntax: '1')
          Body:
              IExpressionStatement (OperationKind.ExpressionStatement) (Syntax: 'Console.Wri ... ine(""One"");')
                Expression: IInvocationExpression (void System.Console.WriteLine(System.String value)) (OperationKind.InvocationExpression, Type: System.Void) (Syntax: 'Console.WriteLine(""One"")')
                    Instance Receiver: null
                    Arguments(1):
                        IArgument (ArgumentKind.Explicit, Matching Parameter: value) (OperationKind.Argument) (Syntax: '""One""')
                          ILiteralExpression (OperationKind.LiteralExpression, Type: System.String, Constant: ""One"") (Syntax: '""One""')
                          InConversion: null
                          OutConversion: null
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
        public void ISwitchStatement_Duplicate()
        {
            string source = @"
using System;

class Program
{
    static void Main()
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
  Switch expression: ILocalReferenceExpression: number (OperationKind.LocalReferenceExpression, Type: System.Int16) (Syntax: 'number')
  Sections:
      ISwitchCase (1 case clauses, 2 statements) (OperationKind.SwitchCase) (Syntax: 'case 0: ... return;')
          Clauses:
              ISingleValueCaseClause (Equality operator kind: BinaryOperationKind.IntegerEquals) (CaseKind.SingleValue) (OperationKind.SingleValueCaseClause) (Syntax: 'case 0:')
                Value: IConversionExpression (ConversionKind.CSharp, Implicit) (OperationKind.ConversionExpression, Type: System.Int16, Constant: 0) (Syntax: '0')
                    Operand: ILiteralExpression (Text: 0) (OperationKind.LiteralExpression, Type: System.Int32, Constant: 0) (Syntax: '0')
          Body:
              IExpressionStatement (OperationKind.ExpressionStatement) (Syntax: 'Console.Wri ... ne(""ZERO"");')
                Expression: IInvocationExpression (void System.Console.WriteLine(System.String value)) (OperationKind.InvocationExpression, Type: System.Void) (Syntax: 'Console.Wri ... ine(""ZERO"")')
                    Instance Receiver: null
                    Arguments(1):
                        IArgument (ArgumentKind.Explicit, Matching Parameter: value) (OperationKind.Argument) (Syntax: '""ZERO""')
                          ILiteralExpression (OperationKind.LiteralExpression, Type: System.String, Constant: ""ZERO"") (Syntax: '""ZERO""')
                          InConversion: null
                          OutConversion: null
              IReturnStatement (OperationKind.ReturnStatement) (Syntax: 'return;')
                ReturnedValue: null
      ISwitchCase (1 case clauses, 2 statements) (OperationKind.SwitchCase, IsInvalid) (Syntax: 'case 0: ... return;')
          Clauses:
              ISingleValueCaseClause (Equality operator kind: BinaryOperationKind.IntegerEquals) (CaseKind.SingleValue) (OperationKind.SingleValueCaseClause, IsInvalid) (Syntax: 'case 0:')
                Value: IConversionExpression (ConversionKind.CSharp, Implicit) (OperationKind.ConversionExpression, Type: System.Int16, Constant: 0, IsInvalid) (Syntax: '0')
                    Operand: ILiteralExpression (Text: 0) (OperationKind.LiteralExpression, Type: System.Int32, Constant: 0, IsInvalid) (Syntax: '0')
          Body:
              IExpressionStatement (OperationKind.ExpressionStatement) (Syntax: 'Console.Wri ... ne(""ZERO"");')
                Expression: IInvocationExpression (void System.Console.WriteLine(System.String value)) (OperationKind.InvocationExpression, Type: System.Void) (Syntax: 'Console.Wri ... ine(""ZERO"")')
                    Instance Receiver: null
                    Arguments(1):
                        IArgument (ArgumentKind.Explicit, Matching Parameter: value) (OperationKind.Argument) (Syntax: '""ZERO""')
                          ILiteralExpression (OperationKind.LiteralExpression, Type: System.String, Constant: ""ZERO"") (Syntax: '""ZERO""')
                          InConversion: null
                          OutConversion: null
              IReturnStatement (OperationKind.ReturnStatement) (Syntax: 'return;')
                ReturnedValue: null
      ISwitchCase (1 case clauses, 2 statements) (OperationKind.SwitchCase) (Syntax: 'case 1: ... return;')
          Clauses:
              ISingleValueCaseClause (Equality operator kind: BinaryOperationKind.IntegerEquals) (CaseKind.SingleValue) (OperationKind.SingleValueCaseClause) (Syntax: 'case 1:')
                Value: IConversionExpression (ConversionKind.CSharp, Implicit) (OperationKind.ConversionExpression, Type: System.Int16, Constant: 1) (Syntax: '1')
                    Operand: ILiteralExpression (Text: 1) (OperationKind.LiteralExpression, Type: System.Int32, Constant: 1) (Syntax: '1')
          Body:
              IExpressionStatement (OperationKind.ExpressionStatement) (Syntax: 'Console.Wri ... ine(""ONE"");')
                Expression: IInvocationExpression (void System.Console.WriteLine(System.String value)) (OperationKind.InvocationExpression, Type: System.Void) (Syntax: 'Console.WriteLine(""ONE"")')
                    Instance Receiver: null
                    Arguments(1):
                        IArgument (ArgumentKind.Explicit, Matching Parameter: value) (OperationKind.Argument) (Syntax: '""ONE""')
                          ILiteralExpression (OperationKind.LiteralExpression, Type: System.String, Constant: ""ONE"") (Syntax: '""ONE""')
                          InConversion: null
                          OutConversion: null
              IReturnStatement (OperationKind.ReturnStatement) (Syntax: 'return;')
                ReturnedValue: null
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
    static void Main()
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
  Switch expression: ILocalReferenceExpression: number (OperationKind.LocalReferenceExpression, Type: System.Int32) (Syntax: 'number')
  Sections:
      ISwitchCase (1 case clauses, 2 statements) (OperationKind.SwitchCase, IsInvalid) (Syntax: 'case test + ... return;')
          Clauses:
              ISingleValueCaseClause (Equality operator kind: BinaryOperationKind.IntegerEquals) (CaseKind.SingleValue) (OperationKind.SingleValueCaseClause, IsInvalid) (Syntax: 'case test + 1:')
                Value: IBinaryOperatorExpression (BinaryOperationKind.IntegerAdd) (OperationKind.BinaryOperatorExpression, Type: System.Int32, IsInvalid) (Syntax: 'test + 1')
                    Left: ILocalReferenceExpression: test (OperationKind.LocalReferenceExpression, Type: System.Int32, IsInvalid) (Syntax: 'test')
                    Right: ILiteralExpression (Text: 1) (OperationKind.LiteralExpression, Type: System.Int32, Constant: 1, IsInvalid) (Syntax: '1')
          Body:
              IExpressionStatement (OperationKind.ExpressionStatement) (Syntax: 'Console.WriteLine(100);')
                Expression: IInvocationExpression (void System.Console.WriteLine(System.Int32 value)) (OperationKind.InvocationExpression, Type: System.Void) (Syntax: 'Console.WriteLine(100)')
                    Instance Receiver: null
                    Arguments(1):
                        IArgument (ArgumentKind.Explicit, Matching Parameter: value) (OperationKind.Argument) (Syntax: '100')
                          ILiteralExpression (Text: 100) (OperationKind.LiteralExpression, Type: System.Int32, Constant: 100) (Syntax: '100')
                          InConversion: null
                          OutConversion: null
              IReturnStatement (OperationKind.ReturnStatement) (Syntax: 'return;')
                ReturnedValue: null
      ISwitchCase (1 case clauses, 2 statements) (OperationKind.SwitchCase) (Syntax: 'case 0: ... return;')
          Clauses:
              ISingleValueCaseClause (Equality operator kind: BinaryOperationKind.IntegerEquals) (CaseKind.SingleValue) (OperationKind.SingleValueCaseClause) (Syntax: 'case 0:')
                Value: ILiteralExpression (Text: 0) (OperationKind.LiteralExpression, Type: System.Int32, Constant: 0) (Syntax: '0')
          Body:
              IExpressionStatement (OperationKind.ExpressionStatement) (Syntax: 'Console.WriteLine(0);')
                Expression: IInvocationExpression (void System.Console.WriteLine(System.Int32 value)) (OperationKind.InvocationExpression, Type: System.Void) (Syntax: 'Console.WriteLine(0)')
                    Instance Receiver: null
                    Arguments(1):
                        IArgument (ArgumentKind.Explicit, Matching Parameter: value) (OperationKind.Argument) (Syntax: '0')
                          ILiteralExpression (Text: 0) (OperationKind.LiteralExpression, Type: System.Int32, Constant: 0) (Syntax: '0')
                          InConversion: null
                          OutConversion: null
              IReturnStatement (OperationKind.ReturnStatement) (Syntax: 'return;')
                ReturnedValue: null
";
            var expectedDiagnostics = new DiagnosticDescription[] {
                // CS0150: A constant value is expected
                //             case test + 1:
                Diagnostic(ErrorCode.ERR_ConstantExpected, "test + 1").WithLocation(12, 18)
            };

            VerifyOperationTreeAndDiagnosticsForTest<SwitchStatementSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }

        /// <summary>
        /// Switch statement doesn't work patern matching , tracking bug https://github.com/dotnet/roslyn/issues/19794 
        /// </summary>
        [Fact, WorkItem(17600, "https://github.com/dotnet/roslyn/issues/17600")]
        public void ISwitchStatement_PatternMatching()
        {
            string source = @"
using System;

class Animal
{
    public int size;
}

class Bird : Animal
{
    public int color;
}

class Program
{
    static void Test(Animal animal)
    {
        /*<bind>*/switch (animal)
        {
            case Bird b:
                Console.WriteLine($""BIRD color = {b.color}"");
                break;
            case Animal a:
                Console.WriteLine($""ANIMAL size = {a.size}"");
                break;
        }/*</bind>*/
    }

    static void Main()
    {
        Bird bird = new Bird();
        bird.color = 5;
        Animal animal = new Animal();
        animal.size = 10;

        Test(bird);
        Test(animal);
    }
}
";
string expectedOperationTree = @"
ISwitchStatement (2 cases) (OperationKind.SwitchStatement) (Syntax: 'switch (ani ... }')
  Switch expression: IParameterReferenceExpression: animal (OperationKind.ParameterReferenceExpression, Type: Animal) (Syntax: 'animal')
  Sections:
      ISwitchCase (1 case clauses, 2 statements) (OperationKind.SwitchCase) (Syntax: 'case Bird b ... break;')
          Clauses:
              IPatternCaseClause (Label Symbol: case Bird b:) (CaseKind.Pattern) (OperationKind.PatternCaseClause) (Syntax: 'case Bird b:')
                Pattern: IDeclarationPattern (Declared Symbol: Bird b) (OperationKind.DeclarationPattern) (Syntax: 'Bird b')
                Guard Expression: null
          Body:
              IExpressionStatement (OperationKind.ExpressionStatement) (Syntax: 'Console.Wri ... b.color}"");')
                Expression: IInvocationExpression (void System.Console.WriteLine(System.String value)) (OperationKind.InvocationExpression, Type: System.Void) (Syntax: 'Console.Wri ... {b.color}"")')
                    Instance Receiver: null
                    Arguments(1):
                        IArgument (ArgumentKind.Explicit, Matching Parameter: value) (OperationKind.Argument) (Syntax: '$""BIRD colo ...  {b.color}""')
                          IInterpolatedStringExpression (OperationKind.InterpolatedStringExpression, Type: System.String) (Syntax: '$""BIRD colo ...  {b.color}""')
                            Parts(2):
                                IInterpolatedStringText (OperationKind.InterpolatedStringText) (Syntax: 'BIRD color = ')
                                  Text: ILiteralExpression (Text: BIRD color = ) (OperationKind.LiteralExpression, Type: System.String, Constant: ""BIRD color = "") (Syntax: 'BIRD color = ')
                                IInterpolation (OperationKind.Interpolation) (Syntax: '{b.color}')
                                  Expression: IFieldReferenceExpression: System.Int32 Bird.color (OperationKind.FieldReferenceExpression, Type: System.Int32) (Syntax: 'b.color')
                                      Instance Receiver: ILocalReferenceExpression: b (OperationKind.LocalReferenceExpression, Type: Bird) (Syntax: 'b')
                                  Alignment: null
                                  FormatString: null
                          InConversion: null
                          OutConversion: null
              IBranchStatement (BranchKind.Break) (OperationKind.BranchStatement) (Syntax: 'break;')
      ISwitchCase (1 case clauses, 2 statements) (OperationKind.SwitchCase) (Syntax: 'case Animal ... break;')
          Clauses:
              IPatternCaseClause (Label Symbol: case Animal a:) (CaseKind.Pattern) (OperationKind.PatternCaseClause) (Syntax: 'case Animal a:')
                Pattern: IDeclarationPattern (Declared Symbol: Animal a) (OperationKind.DeclarationPattern) (Syntax: 'Animal a')
                Guard Expression: null
          Body:
              IExpressionStatement (OperationKind.ExpressionStatement) (Syntax: 'Console.Wri ... {a.size}"");')
                Expression: IInvocationExpression (void System.Console.WriteLine(System.String value)) (OperationKind.InvocationExpression, Type: System.Void) (Syntax: 'Console.Wri ...  {a.size}"")')
                    Instance Receiver: null
                    Arguments(1):
                        IArgument (ArgumentKind.Explicit, Matching Parameter: value) (OperationKind.Argument) (Syntax: '$""ANIMAL si ... = {a.size}""')
                          IInterpolatedStringExpression (OperationKind.InterpolatedStringExpression, Type: System.String) (Syntax: '$""ANIMAL si ... = {a.size}""')
                            Parts(2):
                                IInterpolatedStringText (OperationKind.InterpolatedStringText) (Syntax: 'ANIMAL size = ')
                                  Text: ILiteralExpression (Text: ANIMAL size = ) (OperationKind.LiteralExpression, Type: System.String, Constant: ""ANIMAL size = "") (Syntax: 'ANIMAL size = ')
                                IInterpolation (OperationKind.Interpolation) (Syntax: '{a.size}')
                                  Expression: IFieldReferenceExpression: System.Int32 Animal.size (OperationKind.FieldReferenceExpression, Type: System.Int32) (Syntax: 'a.size')
                                      Instance Receiver: ILocalReferenceExpression: a (OperationKind.LocalReferenceExpression, Type: Animal) (Syntax: 'a')
                                  Alignment: null
                                  FormatString: null
                          InConversion: null
                          OutConversion: null
              IBranchStatement (BranchKind.Break) (OperationKind.BranchStatement) (Syntax: 'break;')
";
            var expectedDiagnostics = DiagnosticDescription.None;

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
    static void Main()
    {
        State state = State.Active;

        /*<bind>*/switch (state)
        {
            case State.Active:
                Console.WriteLine(""A"");
                break;
            case State.Inactive:
                Console.WriteLine(""I"");
                break;
            default:
                throw new Exception(String.Format(""Unknown state: {0}"", state));
        }/*</bind>*/
    }
}
";
string expectedOperationTree = @"
ISwitchStatement (3 cases) (OperationKind.SwitchStatement) (Syntax: 'switch (sta ... }')
  Switch expression: ILocalReferenceExpression: state (OperationKind.LocalReferenceExpression, Type: Program.State) (Syntax: 'state')
  Sections:
      ISwitchCase (1 case clauses, 2 statements) (OperationKind.SwitchCase) (Syntax: 'case State. ... break;')
          Clauses:
              ISingleValueCaseClause (Equality operator kind: BinaryOperationKind.EnumEquals) (CaseKind.SingleValue) (OperationKind.SingleValueCaseClause) (Syntax: 'case State.Active:')
                Value: IFieldReferenceExpression: Program.State.Active (Static) (OperationKind.FieldReferenceExpression, Type: Program.State, Constant: 1) (Syntax: 'State.Active')
                    Instance Receiver: null
          Body:
              IExpressionStatement (OperationKind.ExpressionStatement) (Syntax: 'Console.WriteLine(""A"");')
                Expression: IInvocationExpression (void System.Console.WriteLine(System.String value)) (OperationKind.InvocationExpression, Type: System.Void) (Syntax: 'Console.WriteLine(""A"")')
                    Instance Receiver: null
                    Arguments(1):
                        IArgument (ArgumentKind.Explicit, Matching Parameter: value) (OperationKind.Argument) (Syntax: '""A""')
                          ILiteralExpression (OperationKind.LiteralExpression, Type: System.String, Constant: ""A"") (Syntax: '""A""')
                          InConversion: null
                          OutConversion: null
              IBranchStatement (BranchKind.Break) (OperationKind.BranchStatement) (Syntax: 'break;')
      ISwitchCase (1 case clauses, 2 statements) (OperationKind.SwitchCase) (Syntax: 'case State. ... break;')
          Clauses:
              ISingleValueCaseClause (Equality operator kind: BinaryOperationKind.EnumEquals) (CaseKind.SingleValue) (OperationKind.SingleValueCaseClause) (Syntax: 'case State.Inactive:')
                Value: IFieldReferenceExpression: Program.State.Inactive (Static) (OperationKind.FieldReferenceExpression, Type: Program.State, Constant: 2) (Syntax: 'State.Inactive')
                    Instance Receiver: null
          Body:
              IExpressionStatement (OperationKind.ExpressionStatement) (Syntax: 'Console.WriteLine(""I"");')
                Expression: IInvocationExpression (void System.Console.WriteLine(System.String value)) (OperationKind.InvocationExpression, Type: System.Void) (Syntax: 'Console.WriteLine(""I"")')
                    Instance Receiver: null
                    Arguments(1):
                        IArgument (ArgumentKind.Explicit, Matching Parameter: value) (OperationKind.Argument) (Syntax: '""I""')
                          ILiteralExpression (OperationKind.LiteralExpression, Type: System.String, Constant: ""I"") (Syntax: '""I""')
                          InConversion: null
                          OutConversion: null
              IBranchStatement (BranchKind.Break) (OperationKind.BranchStatement) (Syntax: 'break;')
      ISwitchCase (1 case clauses, 1 statements) (OperationKind.SwitchCase) (Syntax: 'default: ... "", state));')
          Clauses:
              IDefaultCaseClause (CaseKind.Default) (OperationKind.DefaultCaseClause) (Syntax: 'default:')
          Body:
              IThrowStatement (OperationKind.ThrowStatement) (Syntax: 'throw new E ... "", state));')
                ThrownObject: IObjectCreationExpression (Constructor: System.Exception..ctor(System.String message)) (OperationKind.ObjectCreationExpression, Type: System.Exception) (Syntax: 'new Excepti ... }"", state))')
                    Arguments(1):
                        IArgument (ArgumentKind.Explicit, Matching Parameter: message) (OperationKind.Argument) (Syntax: 'String.Form ... 0}"", state)')
                          IInvocationExpression (System.String System.String.Format(System.String format, System.Object arg0)) (OperationKind.InvocationExpression, Type: System.String) (Syntax: 'String.Form ... 0}"", state)')
                            Instance Receiver: null
                            Arguments(2):
                                IArgument (ArgumentKind.Explicit, Matching Parameter: format) (OperationKind.Argument) (Syntax: '""Unknown state: {0}""')
                                  ILiteralExpression (OperationKind.LiteralExpression, Type: System.String, Constant: ""Unknown state: {0}"") (Syntax: '""Unknown state: {0}""')
                                  InConversion: null
                                  OutConversion: null
                                IArgument (ArgumentKind.Explicit, Matching Parameter: arg0) (OperationKind.Argument) (Syntax: 'state')
                                  IConversionExpression (ConversionKind.Cast, Implicit) (OperationKind.ConversionExpression, Type: System.Object) (Syntax: 'state')
                                    Operand: ILocalReferenceExpression: state (OperationKind.LocalReferenceExpression, Type: Program.State) (Syntax: 'state')
                                  InConversion: null
                                  OutConversion: null
                          InConversion: null
                          OutConversion: null
                    Initializer: null
";
            var expectedDiagnostics = DiagnosticDescription.None;

            VerifyOperationTreeAndDiagnosticsForTest<SwitchStatementSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }


    }
}
