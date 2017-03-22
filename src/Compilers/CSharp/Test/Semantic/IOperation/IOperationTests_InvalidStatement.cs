// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

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
IVariableDeclarationStatement (2 variables) (OperationKind.VariableDeclarationStatement, IsInvalid)
  IVariableDeclaration: System.Int32 x (OperationKind.VariableDeclaration)
  IVariableDeclaration: System.Int32  (OperationKind.VariableDeclaration, IsInvalid)
";
            VerifyOperationTreeForTest<LocalDeclarationStatementSyntax>(source, expectedOperationTree);
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
        /*<bind>*/switch(Program)
        {
            case 1:
                break;
        }/*</bind>*/
    }
}
";
            // Operation tree must contain the operations for switch case sections.
            string expectedOperationTree = @"
";
            VerifyOperationTreeForTest<SwitchStatementSyntax>(source, expectedOperationTree);
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
            // IOperation tree might be affected with https://github.com/dotnet/roslyn/issues/18089
            string expectedOperationTree = @"
ISwitchStatement (1 cases) (OperationKind.SwitchStatement, IsInvalid)
  Switch expression: IInvocationExpression (virtual System.String System.Object.ToString()) (OperationKind.InvocationExpression, Type: System.String)
      Instance Receiver: ILocalReferenceExpression: x (OperationKind.LocalReferenceExpression, Type: Program)
  ISwitchCase (1 case clauses, 1 statements) (OperationKind.SwitchCase, IsInvalid)
    Case clauses: ISingleValueCaseClause (Equality operator kind: BinaryOperationKind.StringEquals) (CaseKind.SingleValue) (OperationKind.SingleValueCaseClause, IsInvalid)
        IConversionExpression (ConversionKind.Invalid, Implicit) (OperationKind.ConversionExpression, Type: System.String, IsInvalid)
          ILiteralExpression (Text: 1) (OperationKind.LiteralExpression, Type: System.Int32, Constant: 1)
    Body: IBranchStatement (BranchKind.Break) (OperationKind.BranchStatement)
";
            VerifyOperationTreeForTest<SwitchStatementSyntax>(source, expectedOperationTree);
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
IIfStatement (OperationKind.IfStatement, IsInvalid)
  Condition: IConversionExpression (ConversionKind.Invalid, Implicit) (OperationKind.ConversionExpression, Type: System.Boolean, IsInvalid)
      IAssignmentExpression (OperationKind.AssignmentExpression, Type: Program)
        Left: ILocalReferenceExpression: x (OperationKind.LocalReferenceExpression, Type: Program)
        Right: IConversionExpression (ConversionKind.Cast, Implicit) (OperationKind.ConversionExpression, Type: Program, Constant: null)
            ILiteralExpression (Text: null) (OperationKind.LiteralExpression, Type: null, Constant: null)
  IBlockStatement (0 statements) (OperationKind.BlockStatement)
";
            VerifyOperationTreeForTest<IfStatementSyntax>(source, expectedOperationTree);
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
IIfStatement (OperationKind.IfStatement, IsInvalid)
  Condition: IConversionExpression (ConversionKind.Invalid, Implicit) (OperationKind.ConversionExpression, Type: System.Boolean, IsInvalid)
      IInvalidExpression (OperationKind.InvalidExpression, Type: ?, IsInvalid)
  IBlockStatement (0 statements) (OperationKind.BlockStatement)
  IIfStatement (OperationKind.IfStatement, IsInvalid)
    Condition: IConversionExpression (ConversionKind.Invalid, Implicit) (OperationKind.ConversionExpression, Type: System.Boolean, IsInvalid)
        ILocalReferenceExpression: x (OperationKind.LocalReferenceExpression, Type: Program)
    IExpressionStatement (OperationKind.ExpressionStatement, IsInvalid)
      ILocalReferenceExpression: x (OperationKind.LocalReferenceExpression, Type: Program)
    IExpressionStatement (OperationKind.ExpressionStatement, IsInvalid)
      IInvalidExpression (OperationKind.InvalidExpression, Type: ?, IsInvalid)
";
            VerifyOperationTreeForTest<IfStatementSyntax>(source, expectedOperationTree);
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
        /*<bind>*/for (P; x; )
        {
        }/*</bind>*/
    }
}
";
            string expectedOperationTree = @"
IForLoopStatement (LoopKind.For) (OperationKind.LoopStatement, IsInvalid)
  Condition: IConversionExpression (ConversionKind.Invalid, Implicit) (OperationKind.ConversionExpression, Type: System.Boolean, IsInvalid)
      ILocalReferenceExpression: x (OperationKind.LocalReferenceExpression, Type: Program)
  Before: IExpressionStatement (OperationKind.ExpressionStatement, IsInvalid)
      IInvalidExpression (OperationKind.InvalidExpression, Type: ?, IsInvalid)
  IBlockStatement (0 statements) (OperationKind.BlockStatement)
";
            VerifyOperationTreeForTest<ForStatementSyntax>(source, expectedOperationTree);
        }
    }
}