// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.CSharp.Test.Utilities;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Test.Utilities;
using System.Linq;
using Xunit;

namespace Microsoft.CodeAnalysis.CSharp.UnitTests
{
    public class IOperationTests_InvalidStatement : SemanticModelTestBase
    {
        [CompilerTrait(CompilerFeature.IOperation)]
        [Fact, WorkItem(17607, "https://github.com/dotnet/roslyn/issues/17607")]
        public void InvalidVariableDeclarationStatement()
        {
            string source = @"
using System;

class Program
{
    static void Main(string[] args)
    {
        /*<bind>*/int x, ( 1 );/*</bind>*/
    }
}
";
            string expectedOperationTree = @"
IVariableDeclarationGroupOperation (1 declarations) (OperationKind.VariableDeclarationGroup, Type: null, IsInvalid) (Syntax: 'int x, ( 1 );')
  IVariableDeclarationOperation (2 declarators) (OperationKind.VariableDeclaration, Type: null, IsInvalid) (Syntax: 'int x, ( 1 ')
    Declarators:
        IVariableDeclaratorOperation (Symbol: System.Int32 x) (OperationKind.VariableDeclarator, Type: null) (Syntax: 'x')
          Initializer: 
            null
        IVariableDeclaratorOperation (Symbol: System.Int32 ) (OperationKind.VariableDeclarator, Type: null, IsInvalid) (Syntax: '( 1 ')
          Initializer: 
            null
          IgnoredArguments(1):
              ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 1, IsInvalid) (Syntax: '1')
    Initializer: 
      null
";
            var expectedDiagnostics = new DiagnosticDescription[] {
                // CS1001: Identifier expected
                //         /*<bind>*/int x, ( 1 );/*</bind>*/
                Diagnostic(ErrorCode.ERR_IdentifierExpected, "(").WithLocation(8, 26),
                // CS1528: Expected ; or = (cannot specify constructor arguments in declaration)
                //         /*<bind>*/int x, ( 1 );/*</bind>*/
                Diagnostic(ErrorCode.ERR_BadVarDecl, "( 1 ").WithLocation(8, 26),
                // CS1003: Syntax error, '[' expected
                //         /*<bind>*/int x, ( 1 );/*</bind>*/
                Diagnostic(ErrorCode.ERR_SyntaxError, "(").WithArguments("[").WithLocation(8, 26),
                // CS1003: Syntax error, ']' expected
                //         /*<bind>*/int x, ( 1 );/*</bind>*/
                Diagnostic(ErrorCode.ERR_SyntaxError, ")").WithArguments("]").WithLocation(8, 30),
                // CS0168: The variable 'x' is declared but never used
                //         /*<bind>*/int x, ( 1 );/*</bind>*/
                Diagnostic(ErrorCode.WRN_UnreferencedVar, "x").WithArguments("x").WithLocation(8, 23)
            };

            VerifyOperationTreeAndDiagnosticsForTest<LocalDeclarationStatementSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation)]
        [Fact, WorkItem(17607, "https://github.com/dotnet/roslyn/issues/17607")]
        public void InvalidSwitchStatementExpression()
        {
            string source = @"
using System;

class Program
{
    static void Main(string[] args)
    {
        /*<bind>*/switch (Program)
        {
            case 1:
                break;
        }/*</bind>*/
    }
}
";
            string expectedOperationTree = @"
ISwitchOperation (1 cases, Exit Label Id: 0) (OperationKind.Switch, Type: null, IsInvalid) (Syntax: 'switch (Pro ... }')
  Switch expression: 
    IInvalidOperation (OperationKind.Invalid, Type: Program, IsInvalid, IsImplicit) (Syntax: 'Program')
      Children(1):
          IOperation:  (OperationKind.None, Type: Program, IsInvalid) (Syntax: 'Program')
  Sections:
      ISwitchCaseOperation (1 case clauses, 1 statements) (OperationKind.SwitchCase, Type: null, IsInvalid) (Syntax: 'case 1: ... break;')
          Clauses:
              IPatternCaseClauseOperation (Label Id: 1) (CaseKind.Pattern) (OperationKind.CaseClause, Type: null, IsInvalid) (Syntax: 'case 1:')
                Pattern: 
                  IConstantPatternOperation (OperationKind.ConstantPattern, Type: null, IsInvalid, IsImplicit) (Syntax: '1') (InputType: Program, NarrowedType: Program)
                    Value: 
                      IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: Program, IsInvalid, IsImplicit) (Syntax: '1')
                        Conversion: CommonConversion (Exists: False, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                        Operand: 
                          ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 1, IsInvalid) (Syntax: '1')
          Body:
              IBranchOperation (BranchKind.Break, Label Id: 0) (OperationKind.Branch, Type: null) (Syntax: 'break;')
";
            var expectedDiagnostics = new DiagnosticDescription[] {
                // file.cs(8,27): error CS0119: 'Program' is a type, which is not valid in the given context
                //         /*<bind>*/switch (Program)
                Diagnostic(ErrorCode.ERR_BadSKunknown, "Program").WithArguments("Program", "type").WithLocation(8, 27),
                // file.cs(10,18): error CS0029: Cannot implicitly convert type 'int' to 'Program'
                //             case 1:
                Diagnostic(ErrorCode.ERR_NoImplicitConv, "1").WithArguments("int", "Program").WithLocation(10, 18)
            };

            VerifyOperationTreeAndDiagnosticsForTest<SwitchStatementSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation)]
        [Fact, WorkItem(17607, "https://github.com/dotnet/roslyn/issues/17607")]
        public void InvalidSwitchStatementCaseLabel()
        {
            string source = @"
using System;

class Program
{
    static void Main(string[] args)
    {
        var x = new Program();
        /*<bind>*/switch (x.ToString())
        {
            case 1:
                break;
        }/*</bind>*/
    }
}
";
            string expectedOperationTree = @"
ISwitchOperation (1 cases, Exit Label Id: 0) (OperationKind.Switch, Type: null, IsInvalid) (Syntax: 'switch (x.T ... }')
  Switch expression: 
    IInvocationOperation (virtual System.String System.Object.ToString()) (OperationKind.Invocation, Type: System.String) (Syntax: 'x.ToString()')
      Instance Receiver: 
        ILocalReferenceOperation: x (OperationKind.LocalReference, Type: Program) (Syntax: 'x')
      Arguments(0)
  Sections:
      ISwitchCaseOperation (1 case clauses, 1 statements) (OperationKind.SwitchCase, Type: null, IsInvalid) (Syntax: 'case 1: ... break;')
          Clauses:
              ISingleValueCaseClauseOperation (Label Id: 1) (CaseKind.SingleValue) (OperationKind.CaseClause, Type: null, IsInvalid) (Syntax: 'case 1:')
                Value: 
                  IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.String, IsInvalid, IsImplicit) (Syntax: '1')
                    Conversion: CommonConversion (Exists: False, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                    Operand: 
                      ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 1, IsInvalid) (Syntax: '1')
          Body:
              IBranchOperation (BranchKind.Break, Label Id: 0) (OperationKind.Branch, Type: null) (Syntax: 'break;')
";
            var expectedDiagnostics = new DiagnosticDescription[] {
                // CS0029: Cannot implicitly convert type 'int' to 'string'
                //             case 1:
                Diagnostic(ErrorCode.ERR_NoImplicitConv, "1").WithArguments("int", "string").WithLocation(11, 18)
            };

            VerifyOperationTreeAndDiagnosticsForTest<SwitchStatementSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation)]
        [Fact, WorkItem(17607, "https://github.com/dotnet/roslyn/issues/17607")]
        public void InvalidIfStatement()
        {
            string source = @"
using System;

class Program
{
    static void Main(string[] args)
    {
        var x = new Program();
        /*<bind>*/if (x = null)
        {
        }/*</bind>*/
    }
}
";
            string expectedOperationTree = @"
IConditionalOperation (OperationKind.Conditional, Type: null, IsInvalid) (Syntax: 'if (x = nul ... }')
  Condition: 
    IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.Boolean, IsInvalid, IsImplicit) (Syntax: 'x = null')
      Conversion: CommonConversion (Exists: False, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
      Operand: 
        ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: Program, IsInvalid) (Syntax: 'x = null')
          Left: 
            ILocalReferenceOperation: x (OperationKind.LocalReference, Type: Program, IsInvalid) (Syntax: 'x')
          Right: 
            IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: Program, Constant: null, IsInvalid, IsImplicit) (Syntax: 'null')
              Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: True, IsUserDefined: False) (MethodSymbol: null)
              Operand: 
                ILiteralOperation (OperationKind.Literal, Type: null, Constant: null, IsInvalid) (Syntax: 'null')
  WhenTrue: 
    IBlockOperation (0 statements) (OperationKind.Block, Type: null) (Syntax: '{ ... }')
  WhenFalse: 
    null
";
            var expectedDiagnostics = new DiagnosticDescription[] {
                // CS0029: Cannot implicitly convert type 'Program' to 'bool'
                //         /*<bind>*/if (x = null)
                Diagnostic(ErrorCode.ERR_NoImplicitConv, "x = null").WithArguments("Program", "bool").WithLocation(9, 23)
            };

            VerifyOperationTreeAndDiagnosticsForTest<IfStatementSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation)]
        [Fact, WorkItem(17607, "https://github.com/dotnet/roslyn/issues/17607")]
        public void InvalidIfElseStatement()
        {
            string source = @"
using System;

class Program
{
    static void Main(string[] args)
    {
        var x = new Program();
        /*<bind>*/if ()
        {
        }
        else if (x) x;
        else
/*</bind>*/    }
}
";
            string expectedOperationTree = @"
IConditionalOperation (OperationKind.Conditional, Type: null, IsInvalid) (Syntax: 'if () ... else')
  Condition: 
    IInvalidOperation (OperationKind.Invalid, Type: null, IsInvalid) (Syntax: '')
      Children(0)
  WhenTrue: 
    IBlockOperation (0 statements) (OperationKind.Block, Type: null) (Syntax: '{ ... }')
  WhenFalse: 
    IConditionalOperation (OperationKind.Conditional, Type: null, IsInvalid) (Syntax: 'if (x) x; ... else')
      Condition: 
        IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.Boolean, IsInvalid, IsImplicit) (Syntax: 'x')
          Conversion: CommonConversion (Exists: False, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
          Operand: 
            ILocalReferenceOperation: x (OperationKind.LocalReference, Type: Program, IsInvalid) (Syntax: 'x')
      WhenTrue: 
        IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null, IsInvalid) (Syntax: 'x;')
          Expression: 
            ILocalReferenceOperation: x (OperationKind.LocalReference, Type: Program, IsInvalid) (Syntax: 'x')
      WhenFalse: 
        IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: '')
          Expression: 
            IInvalidOperation (OperationKind.Invalid, Type: null) (Syntax: '')
              Children(0)
";
            var expectedDiagnostics = new DiagnosticDescription[] {
                // CS1525: Invalid expression term ')'
                //         /*<bind>*/if ()
                Diagnostic(ErrorCode.ERR_InvalidExprTerm, ")").WithArguments(")").WithLocation(9, 23),
                // CS1525: Invalid expression term '}'
                //         else
                Diagnostic(ErrorCode.ERR_InvalidExprTerm, "").WithArguments("}").WithLocation(13, 13),
                // CS1002: ; expected
                //         else
                Diagnostic(ErrorCode.ERR_SemicolonExpected, "").WithLocation(13, 13),
                // CS0029: Cannot implicitly convert type 'Program' to 'bool'
                //         else if (x) x;
                Diagnostic(ErrorCode.ERR_NoImplicitConv, "x").WithArguments("Program", "bool").WithLocation(12, 18),
                // CS0201: Only assignment, call, increment, decrement, await, and new object expressions can be used as a statement
                //         else if (x) x;
                Diagnostic(ErrorCode.ERR_IllegalStatement, "x").WithLocation(12, 21)
            };

            VerifyOperationTreeAndDiagnosticsForTest<IfStatementSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation)]
        [Fact, WorkItem(17607, "https://github.com/dotnet/roslyn/issues/17607")]
        public void InvalidForStatement()
        {
            string source = @"
using System;

class Program
{
    static void Main(string[] args)
    {
        var x = new Program();
        /*<bind>*/for (P; x;)
        {
        }/*</bind>*/
    }
}
";
            string expectedOperationTree = @"
IForLoopOperation (LoopKind.For, Continue Label Id: 0, Exit Label Id: 1) (OperationKind.Loop, Type: null, IsInvalid) (Syntax: 'for (P; x;) ... }')
  Condition: 
    IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.Boolean, IsInvalid, IsImplicit) (Syntax: 'x')
      Conversion: CommonConversion (Exists: False, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
      Operand: 
        ILocalReferenceOperation: x (OperationKind.LocalReference, Type: Program, IsInvalid) (Syntax: 'x')
  Before:
      IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null, IsInvalid, IsImplicit) (Syntax: 'P')
        Expression: 
          IInvalidOperation (OperationKind.Invalid, Type: ?, IsInvalid) (Syntax: 'P')
            Children(0)
  AtLoopBottom(0)
  Body: 
    IBlockOperation (0 statements) (OperationKind.Block, Type: null) (Syntax: '{ ... }')
";
            var expectedDiagnostics = new DiagnosticDescription[] {
                // CS0103: The name 'P' does not exist in the current context
                //         /*<bind>*/for (P; x;)
                Diagnostic(ErrorCode.ERR_NameNotInContext, "P").WithArguments("P").WithLocation(9, 24),
                // CS0201: Only assignment, call, increment, decrement, await, and new object expressions can be used as a statement
                //         /*<bind>*/for (P; x;)
                Diagnostic(ErrorCode.ERR_IllegalStatement, "P").WithLocation(9, 24),
                // CS0029: Cannot implicitly convert type 'Program' to 'bool'
                //         /*<bind>*/for (P; x;)
                Diagnostic(ErrorCode.ERR_NoImplicitConv, "x").WithArguments("Program", "bool").WithLocation(9, 27)
            };

            VerifyOperationTreeAndDiagnosticsForTest<ForStatementSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation)]
        [Fact, WorkItem(17607, "https://github.com/dotnet/roslyn/issues/17607")]
        public void InvalidGotoCaseStatement_MissingLabel()
        {
            string source = @"
using System;

class Program
{
    static void Main(string[] args)
    {
        switch (args.Length)
        {
            case 0:
                /*<bind>*/goto case 1;/*</bind>*/
                break;
        }
    }
}
";
            string expectedOperationTree = @"
IInvalidOperation (OperationKind.Invalid, Type: null, IsInvalid) (Syntax: 'goto case 1;')
  Children(1):
      ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 1, IsInvalid) (Syntax: '1')
";
            var expectedDiagnostics = new DiagnosticDescription[] {
                // CS0159: No such label 'case 1:' within the scope of the goto statement
                //                 /*<bind>*/goto case 1;/*</bind>*/
                Diagnostic(ErrorCode.ERR_LabelNotFound, "goto case 1;").WithArguments("case 1:").WithLocation(11, 27)
            };

            VerifyOperationTreeAndDiagnosticsForTest<GotoStatementSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation)]
        [Fact, WorkItem(40714, "https://github.com/dotnet/roslyn/issues/40714")]
        public void InvalidGotoCaseStatement_BadLabel()
        {
            string source = @"
using System;

class Program
{
    static void Main(string[] args)
    {
        switch (args[0], args[1])
        {
            case (string s1, string s2) _:
                /*<bind>*/goto case args is (var x1, var x2);/*</bind>*/
                x1 = x2;
            case (string str, null) _:
                break;
        }
    }
}
";
            string expectedOperationTree = @"
IInvalidOperation (OperationKind.Invalid, Type: null, IsInvalid) (Syntax: 'goto case a ... 1, var x2);')
  Children(1):
      IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: (System.String, System.String), IsInvalid, IsImplicit) (Syntax: 'args is (var x1, var x2)')
        Conversion: CommonConversion (Exists: False, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
        Operand: 
          IIsPatternOperation (OperationKind.IsPattern, Type: System.Boolean, IsInvalid) (Syntax: 'args is (var x1, var x2)')
            Value: 
              IParameterReferenceOperation: args (OperationKind.ParameterReference, Type: System.String[], IsInvalid) (Syntax: 'args')
            Pattern: 
              IRecursivePatternOperation (OperationKind.RecursivePattern, Type: null, IsInvalid) (Syntax: '(var x1, var x2)') (InputType: System.String[], NarrowedType: System.String[], DeclaredSymbol: null, MatchedType: System.String[], DeconstructSymbol: null)
                DeconstructionSubpatterns (2):
                    IDeclarationPatternOperation (OperationKind.DeclarationPattern, Type: null, IsInvalid) (Syntax: 'var x1') (InputType: ?, NarrowedType: ?, DeclaredSymbol: ?? x1, MatchesNull: True)
                    IDeclarationPatternOperation (OperationKind.DeclarationPattern, Type: null, IsInvalid) (Syntax: 'var x2') (InputType: ?, NarrowedType: ?, DeclaredSymbol: ?? x2, MatchesNull: True)
                PropertySubpatterns (0)
";
            var expectedDiagnostics = new DiagnosticDescription[] {
                // file.cs(10,13): error CS0163: Control cannot fall through from one case label ('case (string s1, string s2) _:') to another
                //             case (string s1, string s2) _:
                Diagnostic(ErrorCode.ERR_SwitchFallThrough, "case (string s1, string s2) _:").WithArguments("case (string s1, string s2) _:").WithLocation(10, 13),
                // file.cs(11,27): error CS0029: Cannot implicitly convert type 'bool' to '(string, string)'
                //                 /*<bind>*/goto case args is (var x1, var x2);/*</bind>*/
                Diagnostic(ErrorCode.ERR_NoImplicitConv, "goto case args is (var x1, var x2);").WithArguments("bool", "(string, string)").WithLocation(11, 27),
                // file.cs(11,45): error CS1061: 'string[]' does not contain a definition for 'Deconstruct' and no accessible extension method 'Deconstruct' accepting a first argument of type 'string[]' could be found (are you missing a using directive or an assembly reference?)
                //                 /*<bind>*/goto case args is (var x1, var x2);/*</bind>*/
                Diagnostic(ErrorCode.ERR_NoSuchMemberOrExtension, "(var x1, var x2)").WithArguments("string[]", "Deconstruct").WithLocation(11, 45),
                // file.cs(11,45): error CS8129: No suitable 'Deconstruct' instance or extension method was found for type 'string[]', with 2 out parameters and a void return type.
                //                 /*<bind>*/goto case args is (var x1, var x2);/*</bind>*/
                Diagnostic(ErrorCode.ERR_MissingDeconstruct, "(var x1, var x2)").WithArguments("string[]", "2").WithLocation(11, 45)
            };

            VerifyOperationTreeAndDiagnosticsForTest<GotoStatementSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation)]
        [Fact, WorkItem(17607, "https://github.com/dotnet/roslyn/issues/17607")]
        public void InvalidGotoCaseStatement_OutsideSwitchStatement()
        {
            string source = @"
using System;

class Program
{
    static void Main(string[] args)
    {
        /*<bind>*/goto case 1;/*</bind>*/
    }
}
";
            string expectedOperationTree = @"
IInvalidOperation (OperationKind.Invalid, Type: null, IsInvalid) (Syntax: 'goto case 1;')
  Children(1):
      ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 1, IsInvalid) (Syntax: '1')
";
            var expectedDiagnostics = new DiagnosticDescription[] {
                // CS0153: A goto case is only valid inside a switch statement
                //         /*<bind>*/goto case 1;/*</bind>*/
                Diagnostic(ErrorCode.ERR_InvalidGotoCase, "goto case 1;").WithLocation(8, 19)
            };

            VerifyOperationTreeAndDiagnosticsForTest<GotoStatementSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation)]
        [Fact, WorkItem(17607, "https://github.com/dotnet/roslyn/issues/17607")]
        public void InvalidBreakStatement_OutsideLoopOrSwitch()
        {
            string source = @"
using System;

class Program
{
    static void Main(string[] args)
    {
        /*<bind>*/break;/*</bind>*/
    }
}
";
            string expectedOperationTree = @"
IInvalidOperation (OperationKind.Invalid, Type: null, IsInvalid) (Syntax: 'break;')
  Children(0)
";
            var expectedDiagnostics = new DiagnosticDescription[] {
                // CS0139: No enclosing loop out of which to break or continue
                //         /*<bind>*/break;/*</bind>*/
                Diagnostic(ErrorCode.ERR_NoBreakOrCont, "break;").WithLocation(8, 19)
            };

            VerifyOperationTreeAndDiagnosticsForTest<BreakStatementSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation)]
        [Fact, WorkItem(17607, "https://github.com/dotnet/roslyn/issues/17607")]
        public void InvalidContinueStatement_OutsideLoopOrSwitch()
        {
            string source = @"
using System;

class Program
{
    static void Main(string[] args)
    {
        /*<bind>*/continue;/*</bind>*/
    }
}
";
            string expectedOperationTree = @"
IInvalidOperation (OperationKind.Invalid, Type: null, IsInvalid) (Syntax: 'continue;')
  Children(0)
";
            var expectedDiagnostics = new DiagnosticDescription[] {
                // CS0139: No enclosing loop out of which to break or continue
                //         /*<bind>*/continue;/*</bind>*/
                Diagnostic(ErrorCode.ERR_NoBreakOrCont, "continue;").WithLocation(8, 19)
            };

            VerifyOperationTreeAndDiagnosticsForTest<ContinueStatementSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation, CompilerFeature.Dataflow)]
        [Fact]
        public void InvalidStatementFlow_01()
        {
            string source = @"
using System;

class Program
{
    static void Main(string[] args)
    /*<bind>*/{
        switch (args.Length)
        {
            case 0:
                /*<bind>*/goto case 1;/*</bind>*/
                break;
        }
    }/*</bind>*/
}
";
            var expectedDiagnostics = new DiagnosticDescription[] {
                // file.cs(11,27): error CS0159: No such label 'case 1:' within the scope of the goto statement
                //                 /*<bind>*/goto case 1;/*</bind>*/
                Diagnostic(ErrorCode.ERR_LabelNotFound, "goto case 1;").WithArguments("case 1:").WithLocation(11, 27)
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
            IFlowCaptureOperation: 0 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'args.Length')
              Value: 
                IPropertyReferenceOperation: System.Int32 System.Array.Length { get; } (OperationKind.PropertyReference, Type: System.Int32) (Syntax: 'args.Length')
                  Instance Receiver: 
                    IParameterReferenceOperation: args (OperationKind.ParameterReference, Type: System.String[]) (Syntax: 'args')

        Jump if False (Regular) to Block[B3]
            IBinaryOperation (BinaryOperatorKind.Equals) (OperationKind.Binary, Type: System.Boolean, IsImplicit) (Syntax: '0')
              Left: 
                IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: System.Int32, IsImplicit) (Syntax: 'args.Length')
              Right: 
                ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 0) (Syntax: '0')
            Leaving: {R1}

        Next (Regular) Block[B2]
    Block[B2] - Block
        Predecessors: [B1]
        Statements (2)
            ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 1, IsInvalid) (Syntax: '1')

            IInvalidOperation (OperationKind.Invalid, Type: null, IsInvalid) (Syntax: 'goto case 1;')
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
    }
}
