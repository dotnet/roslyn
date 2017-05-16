﻿// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.CSharp.Test.Utilities;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Test.Utilities;
using System.Linq;
using Xunit;

namespace Microsoft.CodeAnalysis.CSharp.UnitTests
{
    public partial class IOperationTests : SemanticModelTestBase
    {
        [Fact(Skip = "https://github.com/dotnet/roslyn/issues/18077"), WorkItem(17607, "https://github.com/dotnet/roslyn/issues/17607")]
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
IVariableDeclarationStatement (2 variables) (OperationKind.VariableDeclarationStatement, IsInvalid) (Syntax: 'int x, ( 1 );')
  IVariableDeclaration: System.Int32 x (OperationKind.VariableDeclaration) (Syntax: 'x')
  IVariableDeclaration: System.Int32  (OperationKind.VariableDeclaration, IsInvalid) (Syntax: '( 1 ')
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
                Diagnostic(ErrorCode.ERR_SyntaxError, "(").WithArguments("[", "(").WithLocation(8, 26),
                // CS1003: Syntax error, ']' expected
                //         /*<bind>*/int x, ( 1 );/*</bind>*/
                Diagnostic(ErrorCode.ERR_SyntaxError, ")").WithArguments("]", ")").WithLocation(8, 30),
                // CS0168: The variable 'x' is declared but never used
                //         /*<bind>*/int x, ( 1 );/*</bind>*/
                Diagnostic(ErrorCode.WRN_UnreferencedVar, "x").WithArguments("x").WithLocation(8, 23)
            };

            VerifyOperationTreeAndDiagnosticsForTest<LocalDeclarationStatementSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }

        [Fact(Skip = "https://github.com/dotnet/roslyn/issues/18080"), WorkItem(17607, "https://github.com/dotnet/roslyn/issues/17607")]
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
IOperation:  (OperationKind.None, IsInvalid) (Syntax: 'switch (Pro ... }')
";
            var expectedDiagnostics = new DiagnosticDescription[] {
                // CS0119: 'Program' is a type, which is not valid in the given context
                //         /*<bind>*/switch (Program)
                Diagnostic(ErrorCode.ERR_BadSKunknown, "Program").WithArguments("Program", "type").WithLocation(8, 27),
                // CS0029: Cannot implicitly convert type 'int' to 'Program'
                //             case 1:
                Diagnostic(ErrorCode.ERR_NoImplicitConv, "1").WithArguments("int", "Program").WithLocation(10, 18)
            };

            VerifyOperationTreeAndDiagnosticsForTest<SwitchStatementSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }

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
ISwitchStatement (1 cases) (OperationKind.SwitchStatement, IsInvalid) (Syntax: 'switch (x.T ... }')
  Switch expression: IInvocationExpression (virtual System.String System.Object.ToString()) (OperationKind.InvocationExpression, Type: System.String) (Syntax: 'x.ToString()')
      Instance Receiver: ILocalReferenceExpression: x (OperationKind.LocalReferenceExpression, Type: Program) (Syntax: 'x')
  Sections: ISwitchCase (1 case clauses, 1 statements) (OperationKind.SwitchCase, IsInvalid) (Syntax: 'case 1: ... break;')
        Clauses: ISingleValueCaseClause (Equality operator kind: BinaryOperationKind.StringEquals) (CaseKind.SingleValue) (OperationKind.SingleValueCaseClause, IsInvalid) (Syntax: 'case 1:')
            IConversionExpression (ConversionKind.Invalid, Implicit) (OperationKind.ConversionExpression, Type: System.String, IsInvalid) (Syntax: '1')
              ILiteralExpression (Text: 1) (OperationKind.LiteralExpression, Type: System.Int32, Constant: 1) (Syntax: '1')
        Body: IBranchStatement (BranchKind.Break) (OperationKind.BranchStatement) (Syntax: 'break;')
";
            var expectedDiagnostics = new DiagnosticDescription[] {
                // CS0029: Cannot implicitly convert type 'int' to 'string'
                //             case 1:
                Diagnostic(ErrorCode.ERR_NoImplicitConv, "1").WithArguments("int", "string").WithLocation(11, 18)
            };

            VerifyOperationTreeAndDiagnosticsForTest<SwitchStatementSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }

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
IIfStatement (OperationKind.IfStatement, IsInvalid) (Syntax: 'if (x = nul ... }')
  Condition: IConversionExpression (ConversionKind.Invalid, Implicit) (OperationKind.ConversionExpression, Type: System.Boolean, IsInvalid) (Syntax: 'x = null')
      IAssignmentExpression (OperationKind.AssignmentExpression, Type: Program) (Syntax: 'x = null')
        Left: ILocalReferenceExpression: x (OperationKind.LocalReferenceExpression, Type: Program) (Syntax: 'x')
        Right: IConversionExpression (ConversionKind.Cast, Implicit) (OperationKind.ConversionExpression, Type: Program, Constant: null) (Syntax: 'null')
            ILiteralExpression (Text: null) (OperationKind.LiteralExpression, Type: null, Constant: null) (Syntax: 'null')
  IfTrue: IBlockStatement (0 statements) (OperationKind.BlockStatement) (Syntax: '{ ... }')
";
            var expectedDiagnostics = new DiagnosticDescription[] {
                // CS0029: Cannot implicitly convert type 'Program' to 'bool'
                //         /*<bind>*/if (x = null)
                Diagnostic(ErrorCode.ERR_NoImplicitConv, "x = null").WithArguments("Program", "bool").WithLocation(9, 23)
            };

            VerifyOperationTreeAndDiagnosticsForTest<IfStatementSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }

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
IIfStatement (OperationKind.IfStatement, IsInvalid) (Syntax: 'if () ... else')
  Condition: IConversionExpression (ConversionKind.Invalid, Implicit) (OperationKind.ConversionExpression, Type: System.Boolean, IsInvalid) (Syntax: '')
      IInvalidExpression (OperationKind.InvalidExpression, Type: ?, IsInvalid) (Syntax: '')
  IfTrue: IBlockStatement (0 statements) (OperationKind.BlockStatement) (Syntax: '{ ... }')
  IfFalse: IIfStatement (OperationKind.IfStatement, IsInvalid) (Syntax: 'if (x) x; ... else')
      Condition: IConversionExpression (ConversionKind.Invalid, Implicit) (OperationKind.ConversionExpression, Type: System.Boolean, IsInvalid) (Syntax: 'x')
          ILocalReferenceExpression: x (OperationKind.LocalReferenceExpression, Type: Program) (Syntax: 'x')
      IfTrue: IExpressionStatement (OperationKind.ExpressionStatement, IsInvalid) (Syntax: 'x;')
          ILocalReferenceExpression: x (OperationKind.LocalReferenceExpression, Type: Program) (Syntax: 'x')
      IfFalse: IExpressionStatement (OperationKind.ExpressionStatement, IsInvalid) (Syntax: '')
          IInvalidExpression (OperationKind.InvalidExpression, Type: ?, IsInvalid) (Syntax: '')
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
                // CS0201: Only assignment, call, increment, decrement, and new object expressions can be used as a statement
                //         else if (x) x;
                Diagnostic(ErrorCode.ERR_IllegalStatement, "x").WithLocation(12, 21)
            };

            VerifyOperationTreeAndDiagnosticsForTest<IfStatementSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }

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
IForLoopStatement (LoopKind.For) (OperationKind.LoopStatement, IsInvalid) (Syntax: 'for (P; x;) ... }')
  Condition: IConversionExpression (ConversionKind.Invalid, Implicit) (OperationKind.ConversionExpression, Type: System.Boolean, IsInvalid) (Syntax: 'x')
      ILocalReferenceExpression: x (OperationKind.LocalReferenceExpression, Type: Program) (Syntax: 'x')
  Before: IExpressionStatement (OperationKind.ExpressionStatement, IsInvalid) (Syntax: 'P')
      IInvalidExpression (OperationKind.InvalidExpression, Type: ?, IsInvalid) (Syntax: 'P')
  Body: IBlockStatement (0 statements) (OperationKind.BlockStatement) (Syntax: '{ ... }')
";
            var expectedDiagnostics = new DiagnosticDescription[] {
                // CS0103: The name 'P' does not exist in the current context
                //         /*<bind>*/for (P; x;)
                Diagnostic(ErrorCode.ERR_NameNotInContext, "P").WithArguments("P").WithLocation(9, 24),
                // CS0201: Only assignment, call, increment, decrement, and new object expressions can be used as a statement
                //         /*<bind>*/for (P; x;)
                Diagnostic(ErrorCode.ERR_IllegalStatement, "P").WithLocation(9, 24),
                // CS0029: Cannot implicitly convert type 'Program' to 'bool'
                //         /*<bind>*/for (P; x;)
                Diagnostic(ErrorCode.ERR_NoImplicitConv, "x").WithArguments("Program", "bool").WithLocation(9, 27)
            };

            VerifyOperationTreeAndDiagnosticsForTest<ForStatementSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }

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
IInvalidStatement (OperationKind.InvalidStatement, IsInvalid) (Syntax: 'goto case 1;')
  Children(1): ILiteralExpression (Text: 1) (OperationKind.LiteralExpression, Type: System.Int32, Constant: 1) (Syntax: '1')
";
            var expectedDiagnostics = new DiagnosticDescription[] {
                // CS0159: No such label 'case 1:' within the scope of the goto statement
                //                 /*<bind>*/goto case 1;/*</bind>*/
                Diagnostic(ErrorCode.ERR_LabelNotFound, "goto case 1;").WithArguments("case 1:").WithLocation(11, 27)
            };

            VerifyOperationTreeAndDiagnosticsForTest<GotoStatementSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }

        [Fact(Skip = "https://github.com/dotnet/roslyn/issues/18225"), WorkItem(17607, "https://github.com/dotnet/roslyn/issues/17607")]
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
IInvalidStatement (OperationKind.InvalidStatement, IsInvalid) (Syntax: 'goto case 1;')
  ILiteralExpression (Text: 1) (OperationKind.LiteralExpression, Type: System.Int32, Constant: 1) (Syntax: '1')
";
            var expectedDiagnostics = new DiagnosticDescription[] {
                // CS0153: A goto case is only valid inside a switch statement
                //         /*<bind>*/goto case 1;/*</bind>*/
                Diagnostic(ErrorCode.ERR_InvalidGotoCase, "goto case 1;").WithLocation(8, 19)
            };

            VerifyOperationTreeAndDiagnosticsForTest<GotoStatementSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }

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
IInvalidStatement (OperationKind.InvalidStatement, IsInvalid) (Syntax: 'break;')
";
            var expectedDiagnostics = new DiagnosticDescription[] {
                // CS0139: No enclosing loop out of which to break or continue
                //         /*<bind>*/break;/*</bind>*/
                Diagnostic(ErrorCode.ERR_NoBreakOrCont, "break;").WithLocation(8, 19)
            };

            VerifyOperationTreeAndDiagnosticsForTest<BreakStatementSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }

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
IInvalidStatement (OperationKind.InvalidStatement, IsInvalid) (Syntax: 'continue;')
";
            var expectedDiagnostics = new DiagnosticDescription[] {
                // CS0139: No enclosing loop out of which to break or continue
                //         /*<bind>*/continue;/*</bind>*/
                Diagnostic(ErrorCode.ERR_NoBreakOrCont, "continue;").WithLocation(8, 19)
            };

            VerifyOperationTreeAndDiagnosticsForTest<ContinueStatementSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }
    }
}