// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Test.Utilities;
using Xunit;


namespace Microsoft.CodeAnalysis.CSharp.UnitTests
{
    public partial class IOperationTests : SemanticModelTestBase
    {
        [CompilerTrait(CompilerFeature.IOperation)]
        [Fact, WorkItem(17602, "https://github.com/dotnet/roslyn/issues/17602")]
        public void IForLoopStatement_ForSimpleLoop()
        {
            string source = @"
class C
{
    static void Main()
    {
        int x = 3;
        /*<bind>*/for (int i = 0; i < 3; i = i + 1)
        {
            x = x * 3;
        }/*</bind>*/
        System.Console.Write(""{0}"", x);
    }
}
";
            string expectedOperationTree = @"
IForLoopOperation (LoopKind.For) (OperationKind.Loop, Type: null) (Syntax: 'for (int i  ... }')
  Locals: Local_1: System.Int32 i
  Condition: 
    IBinaryOperation (BinaryOperatorKind.LessThan) (OperationKind.BinaryOperator, Type: System.Boolean) (Syntax: 'i < 3')
      Left: 
        ILocalReferenceOperation: i (OperationKind.LocalReference, Type: System.Int32) (Syntax: 'i')
      Right: 
        ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 3) (Syntax: '3')
  Before:
      IVariableDeclarationGroupOperation (1 declarations) (OperationKind.VariableDeclarationGroup, Type: null, IsImplicit) (Syntax: 'int i = 0')
        IVariableDeclarationOperation (1 declarators) (OperationKind.VariableDeclaration, Type: null) (Syntax: 'int i = 0')
          Declarators:
              IVariableDeclaratorOperation (Symbol: System.Int32 i) (OperationKind.VariableDeclarator, Type: null) (Syntax: 'i = 0')
                Initializer: 
                  IVariableInitializerOperation (OperationKind.VariableInitializer, Type: null) (Syntax: '= 0')
                    ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 0) (Syntax: '0')
          Initializer: 
            null
  AtLoopBottom:
      IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null, IsImplicit) (Syntax: 'i = i + 1')
        Expression: 
          ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Int32) (Syntax: 'i = i + 1')
            Left: 
              ILocalReferenceOperation: i (OperationKind.LocalReference, Type: System.Int32) (Syntax: 'i')
            Right: 
              IBinaryOperation (BinaryOperatorKind.Add) (OperationKind.BinaryOperator, Type: System.Int32) (Syntax: 'i + 1')
                Left: 
                  ILocalReferenceOperation: i (OperationKind.LocalReference, Type: System.Int32) (Syntax: 'i')
                Right: 
                  ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 1) (Syntax: '1')
  Body: 
    IBlockOperation (1 statements) (OperationKind.Block, Type: null) (Syntax: '{ ... }')
      IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'x = x * 3;')
        Expression: 
          ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Int32) (Syntax: 'x = x * 3')
            Left: 
              ILocalReferenceOperation: x (OperationKind.LocalReference, Type: System.Int32) (Syntax: 'x')
            Right: 
              IBinaryOperation (BinaryOperatorKind.Multiply) (OperationKind.BinaryOperator, Type: System.Int32) (Syntax: 'x * 3')
                Left: 
                  ILocalReferenceOperation: x (OperationKind.LocalReference, Type: System.Int32) (Syntax: 'x')
                Right: 
                  ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 3) (Syntax: '3')
";
            VerifyOperationTreeForTest<ForStatementSyntax>(source, expectedOperationTree);
        }

        [CompilerTrait(CompilerFeature.IOperation)]
        [Fact, WorkItem(17602, "https://github.com/dotnet/roslyn/issues/17602")]
        public void IForLoopStatement_TrueCondition()
        {
            string source = @"
class C
{
    static void Main()
    {
        int i = 0;
        int j;
        /*<bind>*/for (j = 0; true; j = j + 1)
        {
            i = i + 1;
            break;
        }/*</bind>*/
        System.Console.Write(""{0},{1}"", i, j);
    }
}
";
            string expectedOperationTree = @"
IForLoopOperation (LoopKind.For) (OperationKind.Loop, Type: null) (Syntax: 'for (j = 0; ... }')
  Condition: 
    ILiteralOperation (OperationKind.Literal, Type: System.Boolean, Constant: True) (Syntax: 'true')
  Before:
      IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null, IsImplicit) (Syntax: 'j = 0')
        Expression: 
          ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Int32) (Syntax: 'j = 0')
            Left: 
              ILocalReferenceOperation: j (OperationKind.LocalReference, Type: System.Int32) (Syntax: 'j')
            Right: 
              ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 0) (Syntax: '0')
  AtLoopBottom:
      IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null, IsImplicit) (Syntax: 'j = j + 1')
        Expression: 
          ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Int32) (Syntax: 'j = j + 1')
            Left: 
              ILocalReferenceOperation: j (OperationKind.LocalReference, Type: System.Int32) (Syntax: 'j')
            Right: 
              IBinaryOperation (BinaryOperatorKind.Add) (OperationKind.BinaryOperator, Type: System.Int32) (Syntax: 'j + 1')
                Left: 
                  ILocalReferenceOperation: j (OperationKind.LocalReference, Type: System.Int32) (Syntax: 'j')
                Right: 
                  ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 1) (Syntax: '1')
  Body: 
    IBlockOperation (2 statements) (OperationKind.Block, Type: null) (Syntax: '{ ... }')
      IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'i = i + 1;')
        Expression: 
          ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Int32) (Syntax: 'i = i + 1')
            Left: 
              ILocalReferenceOperation: i (OperationKind.LocalReference, Type: System.Int32) (Syntax: 'i')
            Right: 
              IBinaryOperation (BinaryOperatorKind.Add) (OperationKind.BinaryOperator, Type: System.Int32) (Syntax: 'i + 1')
                Left: 
                  ILocalReferenceOperation: i (OperationKind.LocalReference, Type: System.Int32) (Syntax: 'i')
                Right: 
                  ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 1) (Syntax: '1')
      IBranchOperation (BranchKind.Break) (OperationKind.Branch, Type: null) (Syntax: 'break;')
";
            VerifyOperationTreeForTest<ForStatementSyntax>(source, expectedOperationTree);
        }

        [CompilerTrait(CompilerFeature.IOperation)]
        [Fact, WorkItem(17602, "https://github.com/dotnet/roslyn/issues/17602")]
        public void IForLoopStatement_FalseCondition()
        {
            string source = @"
class C
{
    static void Main()
    {
        int i = 0;
        int j;
        /*<bind>*/for (j = 0; false; j = j + 1)
        {
            i = i + 1;
            break;
        }/*</bind>*/
        System.Console.Write(""{0},{1}"", i, j);
    }
}
";
            string expectedOperationTree = @"
IForLoopOperation (LoopKind.For) (OperationKind.Loop, Type: null) (Syntax: 'for (j = 0; ... }')
  Condition: 
    ILiteralOperation (OperationKind.Literal, Type: System.Boolean, Constant: False) (Syntax: 'false')
  Before:
      IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null, IsImplicit) (Syntax: 'j = 0')
        Expression: 
          ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Int32) (Syntax: 'j = 0')
            Left: 
              ILocalReferenceOperation: j (OperationKind.LocalReference, Type: System.Int32) (Syntax: 'j')
            Right: 
              ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 0) (Syntax: '0')
  AtLoopBottom:
      IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null, IsImplicit) (Syntax: 'j = j + 1')
        Expression: 
          ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Int32) (Syntax: 'j = j + 1')
            Left: 
              ILocalReferenceOperation: j (OperationKind.LocalReference, Type: System.Int32) (Syntax: 'j')
            Right: 
              IBinaryOperation (BinaryOperatorKind.Add) (OperationKind.BinaryOperator, Type: System.Int32) (Syntax: 'j + 1')
                Left: 
                  ILocalReferenceOperation: j (OperationKind.LocalReference, Type: System.Int32) (Syntax: 'j')
                Right: 
                  ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 1) (Syntax: '1')
  Body: 
    IBlockOperation (2 statements) (OperationKind.Block, Type: null) (Syntax: '{ ... }')
      IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'i = i + 1;')
        Expression: 
          ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Int32) (Syntax: 'i = i + 1')
            Left: 
              ILocalReferenceOperation: i (OperationKind.LocalReference, Type: System.Int32) (Syntax: 'i')
            Right: 
              IBinaryOperation (BinaryOperatorKind.Add) (OperationKind.BinaryOperator, Type: System.Int32) (Syntax: 'i + 1')
                Left: 
                  ILocalReferenceOperation: i (OperationKind.LocalReference, Type: System.Int32) (Syntax: 'i')
                Right: 
                  ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 1) (Syntax: '1')
      IBranchOperation (BranchKind.Break) (OperationKind.Branch, Type: null) (Syntax: 'break;')
";
            VerifyOperationTreeForTest<ForStatementSyntax>(source, expectedOperationTree);
        }

        [CompilerTrait(CompilerFeature.IOperation)]
        [Fact, WorkItem(17602, "https://github.com/dotnet/roslyn/issues/17602")]
        public void IForLoopStatement_WithContinue()
        {
            string source = @"
class C
{
    static void Main()
    {
        int i;
        int j;
        /*<bind>*/for (i = 0, j = 0; i < 5; i = i + 1)
        {
            if (i > 2) continue;
            j = j + 1;
        }/*</bind>*/
        System.Console.Write(""{0},{1}, "", i, j);
    }
}
";
            string expectedOperationTree = @"
IForLoopOperation (LoopKind.For) (OperationKind.Loop, Type: null) (Syntax: 'for (i = 0, ... }')
  Condition: 
    IBinaryOperation (BinaryOperatorKind.LessThan) (OperationKind.BinaryOperator, Type: System.Boolean) (Syntax: 'i < 5')
      Left: 
        ILocalReferenceOperation: i (OperationKind.LocalReference, Type: System.Int32) (Syntax: 'i')
      Right: 
        ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 5) (Syntax: '5')
  Before:
      IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null, IsImplicit) (Syntax: 'i = 0')
        Expression: 
          ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Int32) (Syntax: 'i = 0')
            Left: 
              ILocalReferenceOperation: i (OperationKind.LocalReference, Type: System.Int32) (Syntax: 'i')
            Right: 
              ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 0) (Syntax: '0')
      IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null, IsImplicit) (Syntax: 'j = 0')
        Expression: 
          ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Int32) (Syntax: 'j = 0')
            Left: 
              ILocalReferenceOperation: j (OperationKind.LocalReference, Type: System.Int32) (Syntax: 'j')
            Right: 
              ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 0) (Syntax: '0')
  AtLoopBottom:
      IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null, IsImplicit) (Syntax: 'i = i + 1')
        Expression: 
          ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Int32) (Syntax: 'i = i + 1')
            Left: 
              ILocalReferenceOperation: i (OperationKind.LocalReference, Type: System.Int32) (Syntax: 'i')
            Right: 
              IBinaryOperation (BinaryOperatorKind.Add) (OperationKind.BinaryOperator, Type: System.Int32) (Syntax: 'i + 1')
                Left: 
                  ILocalReferenceOperation: i (OperationKind.LocalReference, Type: System.Int32) (Syntax: 'i')
                Right: 
                  ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 1) (Syntax: '1')
  Body: 
    IBlockOperation (2 statements) (OperationKind.Block, Type: null) (Syntax: '{ ... }')
      IConditionalOperation (OperationKind.Conditional, Type: null) (Syntax: 'if (i > 2) continue;')
        Condition: 
          IBinaryOperation (BinaryOperatorKind.GreaterThan) (OperationKind.BinaryOperator, Type: System.Boolean) (Syntax: 'i > 2')
            Left: 
              ILocalReferenceOperation: i (OperationKind.LocalReference, Type: System.Int32) (Syntax: 'i')
            Right: 
              ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 2) (Syntax: '2')
        WhenTrue: 
          IBranchOperation (BranchKind.Continue) (OperationKind.Branch, Type: null) (Syntax: 'continue;')
        WhenFalse: 
          null
      IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'j = j + 1;')
        Expression: 
          ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Int32) (Syntax: 'j = j + 1')
            Left: 
              ILocalReferenceOperation: j (OperationKind.LocalReference, Type: System.Int32) (Syntax: 'j')
            Right: 
              IBinaryOperation (BinaryOperatorKind.Add) (OperationKind.BinaryOperator, Type: System.Int32) (Syntax: 'j + 1')
                Left: 
                  ILocalReferenceOperation: j (OperationKind.LocalReference, Type: System.Int32) (Syntax: 'j')
                Right: 
                  ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 1) (Syntax: '1')
";
            VerifyOperationTreeForTest<ForStatementSyntax>(source, expectedOperationTree);
        }

        [CompilerTrait(CompilerFeature.IOperation)]
        [Fact, WorkItem(17602, "https://github.com/dotnet/roslyn/issues/17602")]
        public void IForLoopStatement_WithBreak()
        {
            string source = @"
class C
{
    static void Main()
    {
        int i;
        int j;
        /*<bind>*/for (i = 0, j = 0; i < 5; i = i + 1)
        {
            if (i > 3) break;
            j = j + 1;
        }/*</bind>*/
        System.Console.Write(""{0}, {1}"", i, j);
    }
}
";
            string expectedOperationTree = @"
IForLoopOperation (LoopKind.For) (OperationKind.Loop, Type: null) (Syntax: 'for (i = 0, ... }')
  Condition: 
    IBinaryOperation (BinaryOperatorKind.LessThan) (OperationKind.BinaryOperator, Type: System.Boolean) (Syntax: 'i < 5')
      Left: 
        ILocalReferenceOperation: i (OperationKind.LocalReference, Type: System.Int32) (Syntax: 'i')
      Right: 
        ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 5) (Syntax: '5')
  Before:
      IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null, IsImplicit) (Syntax: 'i = 0')
        Expression: 
          ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Int32) (Syntax: 'i = 0')
            Left: 
              ILocalReferenceOperation: i (OperationKind.LocalReference, Type: System.Int32) (Syntax: 'i')
            Right: 
              ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 0) (Syntax: '0')
      IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null, IsImplicit) (Syntax: 'j = 0')
        Expression: 
          ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Int32) (Syntax: 'j = 0')
            Left: 
              ILocalReferenceOperation: j (OperationKind.LocalReference, Type: System.Int32) (Syntax: 'j')
            Right: 
              ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 0) (Syntax: '0')
  AtLoopBottom:
      IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null, IsImplicit) (Syntax: 'i = i + 1')
        Expression: 
          ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Int32) (Syntax: 'i = i + 1')
            Left: 
              ILocalReferenceOperation: i (OperationKind.LocalReference, Type: System.Int32) (Syntax: 'i')
            Right: 
              IBinaryOperation (BinaryOperatorKind.Add) (OperationKind.BinaryOperator, Type: System.Int32) (Syntax: 'i + 1')
                Left: 
                  ILocalReferenceOperation: i (OperationKind.LocalReference, Type: System.Int32) (Syntax: 'i')
                Right: 
                  ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 1) (Syntax: '1')
  Body: 
    IBlockOperation (2 statements) (OperationKind.Block, Type: null) (Syntax: '{ ... }')
      IConditionalOperation (OperationKind.Conditional, Type: null) (Syntax: 'if (i > 3) break;')
        Condition: 
          IBinaryOperation (BinaryOperatorKind.GreaterThan) (OperationKind.BinaryOperator, Type: System.Boolean) (Syntax: 'i > 3')
            Left: 
              ILocalReferenceOperation: i (OperationKind.LocalReference, Type: System.Int32) (Syntax: 'i')
            Right: 
              ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 3) (Syntax: '3')
        WhenTrue: 
          IBranchOperation (BranchKind.Break) (OperationKind.Branch, Type: null) (Syntax: 'break;')
        WhenFalse: 
          null
      IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'j = j + 1;')
        Expression: 
          ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Int32) (Syntax: 'j = j + 1')
            Left: 
              ILocalReferenceOperation: j (OperationKind.LocalReference, Type: System.Int32) (Syntax: 'j')
            Right: 
              IBinaryOperation (BinaryOperatorKind.Add) (OperationKind.BinaryOperator, Type: System.Int32) (Syntax: 'j + 1')
                Left: 
                  ILocalReferenceOperation: j (OperationKind.LocalReference, Type: System.Int32) (Syntax: 'j')
                Right: 
                  ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 1) (Syntax: '1')
";
            VerifyOperationTreeForTest<ForStatementSyntax>(source, expectedOperationTree);
        }

        [CompilerTrait(CompilerFeature.IOperation)]
        [Fact, WorkItem(17602, "https://github.com/dotnet/roslyn/issues/17602")]
        public void IForLoopStatement_WithNoStatement()
        {
            string source = @"
class C
{
    static void Main()
    {
        int i = 0;
        /*<bind>*/for (;;)
        {
            if (i > 4) break;
            i = i + 2;
        }/*</bind>*/
        System.Console.Write(""{0}"", i);
    }
}
";
            string expectedOperationTree = @"
IForLoopOperation (LoopKind.For) (OperationKind.Loop, Type: null) (Syntax: 'for (;;) ... }')
  Condition: 
    null
  Before(0)
  AtLoopBottom(0)
  Body: 
    IBlockOperation (2 statements) (OperationKind.Block, Type: null) (Syntax: '{ ... }')
      IConditionalOperation (OperationKind.Conditional, Type: null) (Syntax: 'if (i > 4) break;')
        Condition: 
          IBinaryOperation (BinaryOperatorKind.GreaterThan) (OperationKind.BinaryOperator, Type: System.Boolean) (Syntax: 'i > 4')
            Left: 
              ILocalReferenceOperation: i (OperationKind.LocalReference, Type: System.Int32) (Syntax: 'i')
            Right: 
              ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 4) (Syntax: '4')
        WhenTrue: 
          IBranchOperation (BranchKind.Break) (OperationKind.Branch, Type: null) (Syntax: 'break;')
        WhenFalse: 
          null
      IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'i = i + 2;')
        Expression: 
          ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Int32) (Syntax: 'i = i + 2')
            Left: 
              ILocalReferenceOperation: i (OperationKind.LocalReference, Type: System.Int32) (Syntax: 'i')
            Right: 
              IBinaryOperation (BinaryOperatorKind.Add) (OperationKind.BinaryOperator, Type: System.Int32) (Syntax: 'i + 2')
                Left: 
                  ILocalReferenceOperation: i (OperationKind.LocalReference, Type: System.Int32) (Syntax: 'i')
                Right: 
                  ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 2) (Syntax: '2')
";
            VerifyOperationTreeForTest<ForStatementSyntax>(source, expectedOperationTree);
        }

        [CompilerTrait(CompilerFeature.IOperation)]
        [Fact, WorkItem(17602, "https://github.com/dotnet/roslyn/issues/17602")]
        public void IForLoopStatement_MultipleInitializer()
        {
            string source = @"
class C
{
    static void Main()
    {
        int i = 0;
        int j = 0;
        /*<bind>*/for (i = i + 1, i = i + 1; j < 2; i = i + 2, j = j + 1)
        {
        }/*</bind>*/
        System.Console.Write(""{0}"", i);
    }
}
";
            string expectedOperationTree = @"
IForLoopOperation (LoopKind.For) (OperationKind.Loop, Type: null) (Syntax: 'for (i = i  ... }')
  Condition: 
    IBinaryOperation (BinaryOperatorKind.LessThan) (OperationKind.BinaryOperator, Type: System.Boolean) (Syntax: 'j < 2')
      Left: 
        ILocalReferenceOperation: j (OperationKind.LocalReference, Type: System.Int32) (Syntax: 'j')
      Right: 
        ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 2) (Syntax: '2')
  Before:
      IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null, IsImplicit) (Syntax: 'i = i + 1')
        Expression: 
          ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Int32) (Syntax: 'i = i + 1')
            Left: 
              ILocalReferenceOperation: i (OperationKind.LocalReference, Type: System.Int32) (Syntax: 'i')
            Right: 
              IBinaryOperation (BinaryOperatorKind.Add) (OperationKind.BinaryOperator, Type: System.Int32) (Syntax: 'i + 1')
                Left: 
                  ILocalReferenceOperation: i (OperationKind.LocalReference, Type: System.Int32) (Syntax: 'i')
                Right: 
                  ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 1) (Syntax: '1')
      IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null, IsImplicit) (Syntax: 'i = i + 1')
        Expression: 
          ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Int32) (Syntax: 'i = i + 1')
            Left: 
              ILocalReferenceOperation: i (OperationKind.LocalReference, Type: System.Int32) (Syntax: 'i')
            Right: 
              IBinaryOperation (BinaryOperatorKind.Add) (OperationKind.BinaryOperator, Type: System.Int32) (Syntax: 'i + 1')
                Left: 
                  ILocalReferenceOperation: i (OperationKind.LocalReference, Type: System.Int32) (Syntax: 'i')
                Right: 
                  ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 1) (Syntax: '1')
  AtLoopBottom:
      IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null, IsImplicit) (Syntax: 'i = i + 2')
        Expression: 
          ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Int32) (Syntax: 'i = i + 2')
            Left: 
              ILocalReferenceOperation: i (OperationKind.LocalReference, Type: System.Int32) (Syntax: 'i')
            Right: 
              IBinaryOperation (BinaryOperatorKind.Add) (OperationKind.BinaryOperator, Type: System.Int32) (Syntax: 'i + 2')
                Left: 
                  ILocalReferenceOperation: i (OperationKind.LocalReference, Type: System.Int32) (Syntax: 'i')
                Right: 
                  ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 2) (Syntax: '2')
      IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null, IsImplicit) (Syntax: 'j = j + 1')
        Expression: 
          ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Int32) (Syntax: 'j = j + 1')
            Left: 
              ILocalReferenceOperation: j (OperationKind.LocalReference, Type: System.Int32) (Syntax: 'j')
            Right: 
              IBinaryOperation (BinaryOperatorKind.Add) (OperationKind.BinaryOperator, Type: System.Int32) (Syntax: 'j + 1')
                Left: 
                  ILocalReferenceOperation: j (OperationKind.LocalReference, Type: System.Int32) (Syntax: 'j')
                Right: 
                  ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 1) (Syntax: '1')
  Body: 
    IBlockOperation (0 statements) (OperationKind.Block, Type: null) (Syntax: '{ ... }')
";
            VerifyOperationTreeForTest<ForStatementSyntax>(source, expectedOperationTree);
        }

        [CompilerTrait(CompilerFeature.IOperation)]
        [Fact, WorkItem(17602, "https://github.com/dotnet/roslyn/issues/17602")]
        public void IForLoopStatement_InitializerMissing()
        {
            string source = @"
class C
{
    static void Main(string[] args)
    {
        int i = 1;
        /*<bind>*/for (; i < 10; i = i + 1)
        {
        }/*</bind>*/
    }
}

";
            string expectedOperationTree = @"
IForLoopOperation (LoopKind.For) (OperationKind.Loop, Type: null) (Syntax: 'for (; i <  ... }')
  Condition: 
    IBinaryOperation (BinaryOperatorKind.LessThan) (OperationKind.BinaryOperator, Type: System.Boolean) (Syntax: 'i < 10')
      Left: 
        ILocalReferenceOperation: i (OperationKind.LocalReference, Type: System.Int32) (Syntax: 'i')
      Right: 
        ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 10) (Syntax: '10')
  Before(0)
  AtLoopBottom:
      IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null, IsImplicit) (Syntax: 'i = i + 1')
        Expression: 
          ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Int32) (Syntax: 'i = i + 1')
            Left: 
              ILocalReferenceOperation: i (OperationKind.LocalReference, Type: System.Int32) (Syntax: 'i')
            Right: 
              IBinaryOperation (BinaryOperatorKind.Add) (OperationKind.BinaryOperator, Type: System.Int32) (Syntax: 'i + 1')
                Left: 
                  ILocalReferenceOperation: i (OperationKind.LocalReference, Type: System.Int32) (Syntax: 'i')
                Right: 
                  ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 1) (Syntax: '1')
  Body: 
    IBlockOperation (0 statements) (OperationKind.Block, Type: null) (Syntax: '{ ... }')
";
            VerifyOperationTreeForTest<ForStatementSyntax>(source, expectedOperationTree);
        }

        [CompilerTrait(CompilerFeature.IOperation)]
        [Fact, WorkItem(17602, "https://github.com/dotnet/roslyn/issues/17602")]
        public void IForLoopStatement_DecreasingIterator()
        {
            string source = @"
class C
{
    static void Main(string[] args)
    {
        /*<bind>*/for (int k = 200; k > 100; k = k - 1)
        {
        }/*</bind>*/
    }
}

";
            string expectedOperationTree = @"
IForLoopOperation (LoopKind.For) (OperationKind.Loop, Type: null) (Syntax: 'for (int k  ... }')
  Locals: Local_1: System.Int32 k
  Condition: 
    IBinaryOperation (BinaryOperatorKind.GreaterThan) (OperationKind.BinaryOperator, Type: System.Boolean) (Syntax: 'k > 100')
      Left: 
        ILocalReferenceOperation: k (OperationKind.LocalReference, Type: System.Int32) (Syntax: 'k')
      Right: 
        ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 100) (Syntax: '100')
  Before:
      IVariableDeclarationGroupOperation (1 declarations) (OperationKind.VariableDeclarationGroup, Type: null, IsImplicit) (Syntax: 'int k = 200')
        IVariableDeclarationOperation (1 declarators) (OperationKind.VariableDeclaration, Type: null) (Syntax: 'int k = 200')
          Declarators:
              IVariableDeclaratorOperation (Symbol: System.Int32 k) (OperationKind.VariableDeclarator, Type: null) (Syntax: 'k = 200')
                Initializer: 
                  IVariableInitializerOperation (OperationKind.VariableInitializer, Type: null) (Syntax: '= 200')
                    ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 200) (Syntax: '200')
          Initializer: 
            null
  AtLoopBottom:
      IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null, IsImplicit) (Syntax: 'k = k - 1')
        Expression: 
          ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Int32) (Syntax: 'k = k - 1')
            Left: 
              ILocalReferenceOperation: k (OperationKind.LocalReference, Type: System.Int32) (Syntax: 'k')
            Right: 
              IBinaryOperation (BinaryOperatorKind.Subtract) (OperationKind.BinaryOperator, Type: System.Int32) (Syntax: 'k - 1')
                Left: 
                  ILocalReferenceOperation: k (OperationKind.LocalReference, Type: System.Int32) (Syntax: 'k')
                Right: 
                  ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 1) (Syntax: '1')
  Body: 
    IBlockOperation (0 statements) (OperationKind.Block, Type: null) (Syntax: '{ ... }')
";
            VerifyOperationTreeForTest<ForStatementSyntax>(source, expectedOperationTree);
        }

        [CompilerTrait(CompilerFeature.IOperation)]
        [Fact, WorkItem(17602, "https://github.com/dotnet/roslyn/issues/17602")]
        public void IForLoopStatement_MethodCall()
        {
            string source = @"
class C
{
    static void Main(string[] args)
    {
        /*<bind>*/for (Initializer(); Conditional(); Iterator())
        {
        }/*</bind>*/
    }
    public static int Initializer() { return 1; }
    public static bool Conditional()
    { return true; }
    public static int Iterator() { return 1; }
}

";
            string expectedOperationTree = @"
IForLoopOperation (LoopKind.For) (OperationKind.Loop, Type: null) (Syntax: 'for (Initia ... }')
  Condition: 
    IInvocationOperation (System.Boolean C.Conditional()) (OperationKind.Invocation, Type: System.Boolean) (Syntax: 'Conditional()')
      Instance Receiver: 
        null
      Arguments(0)
  Before:
      IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null, IsImplicit) (Syntax: 'Initializer()')
        Expression: 
          IInvocationOperation (System.Int32 C.Initializer()) (OperationKind.Invocation, Type: System.Int32) (Syntax: 'Initializer()')
            Instance Receiver: 
              null
            Arguments(0)
  AtLoopBottom:
      IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null, IsImplicit) (Syntax: 'Iterator()')
        Expression: 
          IInvocationOperation (System.Int32 C.Iterator()) (OperationKind.Invocation, Type: System.Int32) (Syntax: 'Iterator()')
            Instance Receiver: 
              null
            Arguments(0)
  Body: 
    IBlockOperation (0 statements) (OperationKind.Block, Type: null) (Syntax: '{ ... }')
";
            VerifyOperationTreeForTest<ForStatementSyntax>(source, expectedOperationTree);
        }

        [CompilerTrait(CompilerFeature.IOperation)]
        [Fact, WorkItem(17602, "https://github.com/dotnet/roslyn/issues/17602")]
        public void IForLoopStatement_MissingForBody()
        {
            string source = @"
class C
{
    static void Main(string[] args)
    {
        /*<bind>*/for (int i = 10; i < 100; i = i + 1) ;/*</bind>*/
    }
}
";
            string expectedOperationTree = @"
IForLoopOperation (LoopKind.For) (OperationKind.Loop, Type: null) (Syntax: 'for (int i  ...  = i + 1) ;')
  Locals: Local_1: System.Int32 i
  Condition: 
    IBinaryOperation (BinaryOperatorKind.LessThan) (OperationKind.BinaryOperator, Type: System.Boolean) (Syntax: 'i < 100')
      Left: 
        ILocalReferenceOperation: i (OperationKind.LocalReference, Type: System.Int32) (Syntax: 'i')
      Right: 
        ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 100) (Syntax: '100')
  Before:
      IVariableDeclarationGroupOperation (1 declarations) (OperationKind.VariableDeclarationGroup, Type: null, IsImplicit) (Syntax: 'int i = 10')
        IVariableDeclarationOperation (1 declarators) (OperationKind.VariableDeclaration, Type: null) (Syntax: 'int i = 10')
          Declarators:
              IVariableDeclaratorOperation (Symbol: System.Int32 i) (OperationKind.VariableDeclarator, Type: null) (Syntax: 'i = 10')
                Initializer: 
                  IVariableInitializerOperation (OperationKind.VariableInitializer, Type: null) (Syntax: '= 10')
                    ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 10) (Syntax: '10')
          Initializer: 
            null
  AtLoopBottom:
      IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null, IsImplicit) (Syntax: 'i = i + 1')
        Expression: 
          ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Int32) (Syntax: 'i = i + 1')
            Left: 
              ILocalReferenceOperation: i (OperationKind.LocalReference, Type: System.Int32) (Syntax: 'i')
            Right: 
              IBinaryOperation (BinaryOperatorKind.Add) (OperationKind.BinaryOperator, Type: System.Int32) (Syntax: 'i + 1')
                Left: 
                  ILocalReferenceOperation: i (OperationKind.LocalReference, Type: System.Int32) (Syntax: 'i')
                Right: 
                  ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 1) (Syntax: '1')
  Body: 
    IEmptyOperation (OperationKind.Empty, Type: null) (Syntax: ';')
";
            VerifyOperationTreeForTest<ForStatementSyntax>(source, expectedOperationTree);
        }

        [CompilerTrait(CompilerFeature.IOperation)]
        [Fact, WorkItem(17602, "https://github.com/dotnet/roslyn/issues/17602")]
        public void IForLoopStatement_Nested()
        {
            string source = @"
class C
{
    static void Main(string[] args)
    {
        /*<bind>*/for (int i = 0; i < 100; i = i + 1)
        {
            for (int j = 0; j < 10; j = j + 1)
            {
            }
        }/*</bind>*/
    }
}
";
            string expectedOperationTree = @"
IForLoopOperation (LoopKind.For) (OperationKind.Loop, Type: null) (Syntax: 'for (int i  ... }')
  Locals: Local_1: System.Int32 i
  Condition: 
    IBinaryOperation (BinaryOperatorKind.LessThan) (OperationKind.BinaryOperator, Type: System.Boolean) (Syntax: 'i < 100')
      Left: 
        ILocalReferenceOperation: i (OperationKind.LocalReference, Type: System.Int32) (Syntax: 'i')
      Right: 
        ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 100) (Syntax: '100')
  Before:
      IVariableDeclarationGroupOperation (1 declarations) (OperationKind.VariableDeclarationGroup, Type: null, IsImplicit) (Syntax: 'int i = 0')
        IVariableDeclarationOperation (1 declarators) (OperationKind.VariableDeclaration, Type: null) (Syntax: 'int i = 0')
          Declarators:
              IVariableDeclaratorOperation (Symbol: System.Int32 i) (OperationKind.VariableDeclarator, Type: null) (Syntax: 'i = 0')
                Initializer: 
                  IVariableInitializerOperation (OperationKind.VariableInitializer, Type: null) (Syntax: '= 0')
                    ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 0) (Syntax: '0')
          Initializer: 
            null
  AtLoopBottom:
      IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null, IsImplicit) (Syntax: 'i = i + 1')
        Expression: 
          ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Int32) (Syntax: 'i = i + 1')
            Left: 
              ILocalReferenceOperation: i (OperationKind.LocalReference, Type: System.Int32) (Syntax: 'i')
            Right: 
              IBinaryOperation (BinaryOperatorKind.Add) (OperationKind.BinaryOperator, Type: System.Int32) (Syntax: 'i + 1')
                Left: 
                  ILocalReferenceOperation: i (OperationKind.LocalReference, Type: System.Int32) (Syntax: 'i')
                Right: 
                  ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 1) (Syntax: '1')
  Body: 
    IBlockOperation (1 statements) (OperationKind.Block, Type: null) (Syntax: '{ ... }')
      IForLoopOperation (LoopKind.For) (OperationKind.Loop, Type: null) (Syntax: 'for (int j  ... }')
        Locals: Local_1: System.Int32 j
        Condition: 
          IBinaryOperation (BinaryOperatorKind.LessThan) (OperationKind.BinaryOperator, Type: System.Boolean) (Syntax: 'j < 10')
            Left: 
              ILocalReferenceOperation: j (OperationKind.LocalReference, Type: System.Int32) (Syntax: 'j')
            Right: 
              ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 10) (Syntax: '10')
        Before:
            IVariableDeclarationGroupOperation (1 declarations) (OperationKind.VariableDeclarationGroup, Type: null, IsImplicit) (Syntax: 'int j = 0')
              IVariableDeclarationOperation (1 declarators) (OperationKind.VariableDeclaration, Type: null) (Syntax: 'int j = 0')
                Declarators:
                    IVariableDeclaratorOperation (Symbol: System.Int32 j) (OperationKind.VariableDeclarator, Type: null) (Syntax: 'j = 0')
                      Initializer: 
                        IVariableInitializerOperation (OperationKind.VariableInitializer, Type: null) (Syntax: '= 0')
                          ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 0) (Syntax: '0')
                Initializer: 
                  null
        AtLoopBottom:
            IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null, IsImplicit) (Syntax: 'j = j + 1')
              Expression: 
                ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Int32) (Syntax: 'j = j + 1')
                  Left: 
                    ILocalReferenceOperation: j (OperationKind.LocalReference, Type: System.Int32) (Syntax: 'j')
                  Right: 
                    IBinaryOperation (BinaryOperatorKind.Add) (OperationKind.BinaryOperator, Type: System.Int32) (Syntax: 'j + 1')
                      Left: 
                        ILocalReferenceOperation: j (OperationKind.LocalReference, Type: System.Int32) (Syntax: 'j')
                      Right: 
                        ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 1) (Syntax: '1')
        Body: 
          IBlockOperation (0 statements) (OperationKind.Block, Type: null) (Syntax: '{ ... }')
";
            VerifyOperationTreeForTest<ForStatementSyntax>(source, expectedOperationTree);
        }

        [CompilerTrait(CompilerFeature.IOperation)]
        [Fact, WorkItem(17602, "https://github.com/dotnet/roslyn/issues/17602")]
        public void IForLoopStatement_ChangeOuterVariableInInnerLoop()
        {
            string source = @"
class C
{
    static void Main(string[] args)
    {
        /*<bind>*/for (int i = 0; i < 10; i = i + 1)
        {
            for (int j = 0; j < 10; j = j + 1)
            {
                i = 1;
            }
        }/*</bind>*/
    }
}

";
            string expectedOperationTree = @"
IForLoopOperation (LoopKind.For) (OperationKind.Loop, Type: null) (Syntax: 'for (int i  ... }')
  Locals: Local_1: System.Int32 i
  Condition: 
    IBinaryOperation (BinaryOperatorKind.LessThan) (OperationKind.BinaryOperator, Type: System.Boolean) (Syntax: 'i < 10')
      Left: 
        ILocalReferenceOperation: i (OperationKind.LocalReference, Type: System.Int32) (Syntax: 'i')
      Right: 
        ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 10) (Syntax: '10')
  Before:
      IVariableDeclarationGroupOperation (1 declarations) (OperationKind.VariableDeclarationGroup, Type: null, IsImplicit) (Syntax: 'int i = 0')
        IVariableDeclarationOperation (1 declarators) (OperationKind.VariableDeclaration, Type: null) (Syntax: 'int i = 0')
          Declarators:
              IVariableDeclaratorOperation (Symbol: System.Int32 i) (OperationKind.VariableDeclarator, Type: null) (Syntax: 'i = 0')
                Initializer: 
                  IVariableInitializerOperation (OperationKind.VariableInitializer, Type: null) (Syntax: '= 0')
                    ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 0) (Syntax: '0')
          Initializer: 
            null
  AtLoopBottom:
      IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null, IsImplicit) (Syntax: 'i = i + 1')
        Expression: 
          ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Int32) (Syntax: 'i = i + 1')
            Left: 
              ILocalReferenceOperation: i (OperationKind.LocalReference, Type: System.Int32) (Syntax: 'i')
            Right: 
              IBinaryOperation (BinaryOperatorKind.Add) (OperationKind.BinaryOperator, Type: System.Int32) (Syntax: 'i + 1')
                Left: 
                  ILocalReferenceOperation: i (OperationKind.LocalReference, Type: System.Int32) (Syntax: 'i')
                Right: 
                  ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 1) (Syntax: '1')
  Body: 
    IBlockOperation (1 statements) (OperationKind.Block, Type: null) (Syntax: '{ ... }')
      IForLoopOperation (LoopKind.For) (OperationKind.Loop, Type: null) (Syntax: 'for (int j  ... }')
        Locals: Local_1: System.Int32 j
        Condition: 
          IBinaryOperation (BinaryOperatorKind.LessThan) (OperationKind.BinaryOperator, Type: System.Boolean) (Syntax: 'j < 10')
            Left: 
              ILocalReferenceOperation: j (OperationKind.LocalReference, Type: System.Int32) (Syntax: 'j')
            Right: 
              ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 10) (Syntax: '10')
        Before:
            IVariableDeclarationGroupOperation (1 declarations) (OperationKind.VariableDeclarationGroup, Type: null, IsImplicit) (Syntax: 'int j = 0')
              IVariableDeclarationOperation (1 declarators) (OperationKind.VariableDeclaration, Type: null) (Syntax: 'int j = 0')
                Declarators:
                    IVariableDeclaratorOperation (Symbol: System.Int32 j) (OperationKind.VariableDeclarator, Type: null) (Syntax: 'j = 0')
                      Initializer: 
                        IVariableInitializerOperation (OperationKind.VariableInitializer, Type: null) (Syntax: '= 0')
                          ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 0) (Syntax: '0')
                Initializer: 
                  null
        AtLoopBottom:
            IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null, IsImplicit) (Syntax: 'j = j + 1')
              Expression: 
                ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Int32) (Syntax: 'j = j + 1')
                  Left: 
                    ILocalReferenceOperation: j (OperationKind.LocalReference, Type: System.Int32) (Syntax: 'j')
                  Right: 
                    IBinaryOperation (BinaryOperatorKind.Add) (OperationKind.BinaryOperator, Type: System.Int32) (Syntax: 'j + 1')
                      Left: 
                        ILocalReferenceOperation: j (OperationKind.LocalReference, Type: System.Int32) (Syntax: 'j')
                      Right: 
                        ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 1) (Syntax: '1')
        Body: 
          IBlockOperation (1 statements) (OperationKind.Block, Type: null) (Syntax: '{ ... }')
            IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'i = 1;')
              Expression: 
                ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Int32) (Syntax: 'i = 1')
                  Left: 
                    ILocalReferenceOperation: i (OperationKind.LocalReference, Type: System.Int32) (Syntax: 'i')
                  Right: 
                    ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 1) (Syntax: '1')
";
            VerifyOperationTreeForTest<ForStatementSyntax>(source, expectedOperationTree);
        }

        [CompilerTrait(CompilerFeature.IOperation)]
        [Fact, WorkItem(17602, "https://github.com/dotnet/roslyn/issues/17602")]
        public void IForLoopStatement_InnerLoopRefOuterIteration()
        {
            string source = @"
class C
{
    static void Main(string[] args)
    {
        /*<bind>*/for (int i = 0; i < 5; i = i + 1)
        {
            for (int j = i + 1; i < j; j = j - 1)
            {
            }
        }/*</bind>*/
    }
}

";
            string expectedOperationTree = @"
IForLoopOperation (LoopKind.For) (OperationKind.Loop, Type: null) (Syntax: 'for (int i  ... }')
  Locals: Local_1: System.Int32 i
  Condition: 
    IBinaryOperation (BinaryOperatorKind.LessThan) (OperationKind.BinaryOperator, Type: System.Boolean) (Syntax: 'i < 5')
      Left: 
        ILocalReferenceOperation: i (OperationKind.LocalReference, Type: System.Int32) (Syntax: 'i')
      Right: 
        ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 5) (Syntax: '5')
  Before:
      IVariableDeclarationGroupOperation (1 declarations) (OperationKind.VariableDeclarationGroup, Type: null, IsImplicit) (Syntax: 'int i = 0')
        IVariableDeclarationOperation (1 declarators) (OperationKind.VariableDeclaration, Type: null) (Syntax: 'int i = 0')
          Declarators:
              IVariableDeclaratorOperation (Symbol: System.Int32 i) (OperationKind.VariableDeclarator, Type: null) (Syntax: 'i = 0')
                Initializer: 
                  IVariableInitializerOperation (OperationKind.VariableInitializer, Type: null) (Syntax: '= 0')
                    ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 0) (Syntax: '0')
          Initializer: 
            null
  AtLoopBottom:
      IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null, IsImplicit) (Syntax: 'i = i + 1')
        Expression: 
          ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Int32) (Syntax: 'i = i + 1')
            Left: 
              ILocalReferenceOperation: i (OperationKind.LocalReference, Type: System.Int32) (Syntax: 'i')
            Right: 
              IBinaryOperation (BinaryOperatorKind.Add) (OperationKind.BinaryOperator, Type: System.Int32) (Syntax: 'i + 1')
                Left: 
                  ILocalReferenceOperation: i (OperationKind.LocalReference, Type: System.Int32) (Syntax: 'i')
                Right: 
                  ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 1) (Syntax: '1')
  Body: 
    IBlockOperation (1 statements) (OperationKind.Block, Type: null) (Syntax: '{ ... }')
      IForLoopOperation (LoopKind.For) (OperationKind.Loop, Type: null) (Syntax: 'for (int j  ... }')
        Locals: Local_1: System.Int32 j
        Condition: 
          IBinaryOperation (BinaryOperatorKind.LessThan) (OperationKind.BinaryOperator, Type: System.Boolean) (Syntax: 'i < j')
            Left: 
              ILocalReferenceOperation: i (OperationKind.LocalReference, Type: System.Int32) (Syntax: 'i')
            Right: 
              ILocalReferenceOperation: j (OperationKind.LocalReference, Type: System.Int32) (Syntax: 'j')
        Before:
            IVariableDeclarationGroupOperation (1 declarations) (OperationKind.VariableDeclarationGroup, Type: null, IsImplicit) (Syntax: 'int j = i + 1')
              IVariableDeclarationOperation (1 declarators) (OperationKind.VariableDeclaration, Type: null) (Syntax: 'int j = i + 1')
                Declarators:
                    IVariableDeclaratorOperation (Symbol: System.Int32 j) (OperationKind.VariableDeclarator, Type: null) (Syntax: 'j = i + 1')
                      Initializer: 
                        IVariableInitializerOperation (OperationKind.VariableInitializer, Type: null) (Syntax: '= i + 1')
                          IBinaryOperation (BinaryOperatorKind.Add) (OperationKind.BinaryOperator, Type: System.Int32) (Syntax: 'i + 1')
                            Left: 
                              ILocalReferenceOperation: i (OperationKind.LocalReference, Type: System.Int32) (Syntax: 'i')
                            Right: 
                              ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 1) (Syntax: '1')
                Initializer: 
                  null
        AtLoopBottom:
            IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null, IsImplicit) (Syntax: 'j = j - 1')
              Expression: 
                ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Int32) (Syntax: 'j = j - 1')
                  Left: 
                    ILocalReferenceOperation: j (OperationKind.LocalReference, Type: System.Int32) (Syntax: 'j')
                  Right: 
                    IBinaryOperation (BinaryOperatorKind.Subtract) (OperationKind.BinaryOperator, Type: System.Int32) (Syntax: 'j - 1')
                      Left: 
                        ILocalReferenceOperation: j (OperationKind.LocalReference, Type: System.Int32) (Syntax: 'j')
                      Right: 
                        ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 1) (Syntax: '1')
        Body: 
          IBlockOperation (0 statements) (OperationKind.Block, Type: null) (Syntax: '{ ... }')
";
            VerifyOperationTreeForTest<ForStatementSyntax>(source, expectedOperationTree);
        }

        [CompilerTrait(CompilerFeature.IOperation)]
        [Fact, WorkItem(17602, "https://github.com/dotnet/roslyn/issues/17602")]
        public void IForLoopStatement_BreakFromNestedLoop()
        {
            string source = @"
class C
{
    static void Main(string[] args)
    {
        /*<bind>*/for (int i = 0; i < 5; i = i + 1)
        {
            for (int j = 0; j < 10; j = j + 1)
            {
                if (j == 5)
                    break;
            }
        }/*</bind>*/
    }
}

";
            string expectedOperationTree = @"
IForLoopOperation (LoopKind.For) (OperationKind.Loop, Type: null) (Syntax: 'for (int i  ... }')
  Locals: Local_1: System.Int32 i
  Condition: 
    IBinaryOperation (BinaryOperatorKind.LessThan) (OperationKind.BinaryOperator, Type: System.Boolean) (Syntax: 'i < 5')
      Left: 
        ILocalReferenceOperation: i (OperationKind.LocalReference, Type: System.Int32) (Syntax: 'i')
      Right: 
        ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 5) (Syntax: '5')
  Before:
      IVariableDeclarationGroupOperation (1 declarations) (OperationKind.VariableDeclarationGroup, Type: null, IsImplicit) (Syntax: 'int i = 0')
        IVariableDeclarationOperation (1 declarators) (OperationKind.VariableDeclaration, Type: null) (Syntax: 'int i = 0')
          Declarators:
              IVariableDeclaratorOperation (Symbol: System.Int32 i) (OperationKind.VariableDeclarator, Type: null) (Syntax: 'i = 0')
                Initializer: 
                  IVariableInitializerOperation (OperationKind.VariableInitializer, Type: null) (Syntax: '= 0')
                    ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 0) (Syntax: '0')
          Initializer: 
            null
  AtLoopBottom:
      IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null, IsImplicit) (Syntax: 'i = i + 1')
        Expression: 
          ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Int32) (Syntax: 'i = i + 1')
            Left: 
              ILocalReferenceOperation: i (OperationKind.LocalReference, Type: System.Int32) (Syntax: 'i')
            Right: 
              IBinaryOperation (BinaryOperatorKind.Add) (OperationKind.BinaryOperator, Type: System.Int32) (Syntax: 'i + 1')
                Left: 
                  ILocalReferenceOperation: i (OperationKind.LocalReference, Type: System.Int32) (Syntax: 'i')
                Right: 
                  ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 1) (Syntax: '1')
  Body: 
    IBlockOperation (1 statements) (OperationKind.Block, Type: null) (Syntax: '{ ... }')
      IForLoopOperation (LoopKind.For) (OperationKind.Loop, Type: null) (Syntax: 'for (int j  ... }')
        Locals: Local_1: System.Int32 j
        Condition: 
          IBinaryOperation (BinaryOperatorKind.LessThan) (OperationKind.BinaryOperator, Type: System.Boolean) (Syntax: 'j < 10')
            Left: 
              ILocalReferenceOperation: j (OperationKind.LocalReference, Type: System.Int32) (Syntax: 'j')
            Right: 
              ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 10) (Syntax: '10')
        Before:
            IVariableDeclarationGroupOperation (1 declarations) (OperationKind.VariableDeclarationGroup, Type: null, IsImplicit) (Syntax: 'int j = 0')
              IVariableDeclarationOperation (1 declarators) (OperationKind.VariableDeclaration, Type: null) (Syntax: 'int j = 0')
                Declarators:
                    IVariableDeclaratorOperation (Symbol: System.Int32 j) (OperationKind.VariableDeclarator, Type: null) (Syntax: 'j = 0')
                      Initializer: 
                        IVariableInitializerOperation (OperationKind.VariableInitializer, Type: null) (Syntax: '= 0')
                          ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 0) (Syntax: '0')
                Initializer: 
                  null
        AtLoopBottom:
            IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null, IsImplicit) (Syntax: 'j = j + 1')
              Expression: 
                ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Int32) (Syntax: 'j = j + 1')
                  Left: 
                    ILocalReferenceOperation: j (OperationKind.LocalReference, Type: System.Int32) (Syntax: 'j')
                  Right: 
                    IBinaryOperation (BinaryOperatorKind.Add) (OperationKind.BinaryOperator, Type: System.Int32) (Syntax: 'j + 1')
                      Left: 
                        ILocalReferenceOperation: j (OperationKind.LocalReference, Type: System.Int32) (Syntax: 'j')
                      Right: 
                        ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 1) (Syntax: '1')
        Body: 
          IBlockOperation (1 statements) (OperationKind.Block, Type: null) (Syntax: '{ ... }')
            IConditionalOperation (OperationKind.Conditional, Type: null) (Syntax: 'if (j == 5) ... break;')
              Condition: 
                IBinaryOperation (BinaryOperatorKind.Equals) (OperationKind.BinaryOperator, Type: System.Boolean) (Syntax: 'j == 5')
                  Left: 
                    ILocalReferenceOperation: j (OperationKind.LocalReference, Type: System.Int32) (Syntax: 'j')
                  Right: 
                    ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 5) (Syntax: '5')
              WhenTrue: 
                IBranchOperation (BranchKind.Break) (OperationKind.Branch, Type: null) (Syntax: 'break;')
              WhenFalse: 
                null
";
            VerifyOperationTreeForTest<ForStatementSyntax>(source, expectedOperationTree);
        }

        [CompilerTrait(CompilerFeature.IOperation)]
        [Fact, WorkItem(17602, "https://github.com/dotnet/roslyn/issues/17602")]
        public void IForLoopStatement_ContinueForNestedLoop()
        {
            string source = @"
class C
{
    static void Main(string[] args)
    {
        /*<bind>*/for (int i = 0; i < 5; i = i + 1)
        {
            for (int j = 1; j < 10; j = j + 1)
            {
                if ((j % 2) != 0)
                    continue;
                i = i + 1;
                System.Console.Write(i);
            }
        }/*</bind>*/
    }
}
";
            string expectedOperationTree = @"
IForLoopOperation (LoopKind.For) (OperationKind.Loop, Type: null) (Syntax: 'for (int i  ... }')
  Locals: Local_1: System.Int32 i
  Condition: 
    IBinaryOperation (BinaryOperatorKind.LessThan) (OperationKind.BinaryOperator, Type: System.Boolean) (Syntax: 'i < 5')
      Left: 
        ILocalReferenceOperation: i (OperationKind.LocalReference, Type: System.Int32) (Syntax: 'i')
      Right: 
        ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 5) (Syntax: '5')
  Before:
      IVariableDeclarationGroupOperation (1 declarations) (OperationKind.VariableDeclarationGroup, Type: null, IsImplicit) (Syntax: 'int i = 0')
        IVariableDeclarationOperation (1 declarators) (OperationKind.VariableDeclaration, Type: null) (Syntax: 'int i = 0')
          Declarators:
              IVariableDeclaratorOperation (Symbol: System.Int32 i) (OperationKind.VariableDeclarator, Type: null) (Syntax: 'i = 0')
                Initializer: 
                  IVariableInitializerOperation (OperationKind.VariableInitializer, Type: null) (Syntax: '= 0')
                    ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 0) (Syntax: '0')
          Initializer: 
            null
  AtLoopBottom:
      IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null, IsImplicit) (Syntax: 'i = i + 1')
        Expression: 
          ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Int32) (Syntax: 'i = i + 1')
            Left: 
              ILocalReferenceOperation: i (OperationKind.LocalReference, Type: System.Int32) (Syntax: 'i')
            Right: 
              IBinaryOperation (BinaryOperatorKind.Add) (OperationKind.BinaryOperator, Type: System.Int32) (Syntax: 'i + 1')
                Left: 
                  ILocalReferenceOperation: i (OperationKind.LocalReference, Type: System.Int32) (Syntax: 'i')
                Right: 
                  ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 1) (Syntax: '1')
  Body: 
    IBlockOperation (1 statements) (OperationKind.Block, Type: null) (Syntax: '{ ... }')
      IForLoopOperation (LoopKind.For) (OperationKind.Loop, Type: null) (Syntax: 'for (int j  ... }')
        Locals: Local_1: System.Int32 j
        Condition: 
          IBinaryOperation (BinaryOperatorKind.LessThan) (OperationKind.BinaryOperator, Type: System.Boolean) (Syntax: 'j < 10')
            Left: 
              ILocalReferenceOperation: j (OperationKind.LocalReference, Type: System.Int32) (Syntax: 'j')
            Right: 
              ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 10) (Syntax: '10')
        Before:
            IVariableDeclarationGroupOperation (1 declarations) (OperationKind.VariableDeclarationGroup, Type: null, IsImplicit) (Syntax: 'int j = 1')
              IVariableDeclarationOperation (1 declarators) (OperationKind.VariableDeclaration, Type: null) (Syntax: 'int j = 1')
                Declarators:
                    IVariableDeclaratorOperation (Symbol: System.Int32 j) (OperationKind.VariableDeclarator, Type: null) (Syntax: 'j = 1')
                      Initializer: 
                        IVariableInitializerOperation (OperationKind.VariableInitializer, Type: null) (Syntax: '= 1')
                          ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 1) (Syntax: '1')
                Initializer: 
                  null
        AtLoopBottom:
            IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null, IsImplicit) (Syntax: 'j = j + 1')
              Expression: 
                ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Int32) (Syntax: 'j = j + 1')
                  Left: 
                    ILocalReferenceOperation: j (OperationKind.LocalReference, Type: System.Int32) (Syntax: 'j')
                  Right: 
                    IBinaryOperation (BinaryOperatorKind.Add) (OperationKind.BinaryOperator, Type: System.Int32) (Syntax: 'j + 1')
                      Left: 
                        ILocalReferenceOperation: j (OperationKind.LocalReference, Type: System.Int32) (Syntax: 'j')
                      Right: 
                        ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 1) (Syntax: '1')
        Body: 
          IBlockOperation (3 statements) (OperationKind.Block, Type: null) (Syntax: '{ ... }')
            IConditionalOperation (OperationKind.Conditional, Type: null) (Syntax: 'if ((j % 2) ... continue;')
              Condition: 
                IBinaryOperation (BinaryOperatorKind.NotEquals) (OperationKind.BinaryOperator, Type: System.Boolean) (Syntax: '(j % 2) != 0')
                  Left: 
                    IBinaryOperation (BinaryOperatorKind.Remainder) (OperationKind.BinaryOperator, Type: System.Int32) (Syntax: 'j % 2')
                      Left: 
                        ILocalReferenceOperation: j (OperationKind.LocalReference, Type: System.Int32) (Syntax: 'j')
                      Right: 
                        ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 2) (Syntax: '2')
                  Right: 
                    ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 0) (Syntax: '0')
              WhenTrue: 
                IBranchOperation (BranchKind.Continue) (OperationKind.Branch, Type: null) (Syntax: 'continue;')
              WhenFalse: 
                null
            IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'i = i + 1;')
              Expression: 
                ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Int32) (Syntax: 'i = i + 1')
                  Left: 
                    ILocalReferenceOperation: i (OperationKind.LocalReference, Type: System.Int32) (Syntax: 'i')
                  Right: 
                    IBinaryOperation (BinaryOperatorKind.Add) (OperationKind.BinaryOperator, Type: System.Int32) (Syntax: 'i + 1')
                      Left: 
                        ILocalReferenceOperation: i (OperationKind.LocalReference, Type: System.Int32) (Syntax: 'i')
                      Right: 
                        ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 1) (Syntax: '1')
            IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'System.Console.Write(i);')
              Expression: 
                IInvocationOperation (void System.Console.Write(System.Int32 value)) (OperationKind.Invocation, Type: System.Void) (Syntax: 'System.Console.Write(i)')
                  Instance Receiver: 
                    null
                  Arguments(1):
                      IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: value) (OperationKind.Argument, Type: null) (Syntax: 'i')
                        ILocalReferenceOperation: i (OperationKind.LocalReference, Type: System.Int32) (Syntax: 'i')
                        InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                        OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
";
            VerifyOperationTreeForTest<ForStatementSyntax>(source, expectedOperationTree);
        }

        [CompilerTrait(CompilerFeature.IOperation)]
        [Fact, WorkItem(17602, "https://github.com/dotnet/roslyn/issues/17602")]
        public void IForLoopStatement_GotoForNestedLoop_1()
        {
            string source = @"
class C
{
    static void Main(string[] args)
    {
        /*<bind>*/for (int i = 0; i < 5; i = i + 1)
        {
            for (int j = 0; j < 10; j = j + 1)
            {
                goto stop;
            stop:
                j = j + 1;
            }
        }/*</bind>*/
    }
}

";
            string expectedOperationTree = @"
IForLoopOperation (LoopKind.For) (OperationKind.Loop, Type: null) (Syntax: 'for (int i  ... }')
  Locals: Local_1: System.Int32 i
  Condition: 
    IBinaryOperation (BinaryOperatorKind.LessThan) (OperationKind.BinaryOperator, Type: System.Boolean) (Syntax: 'i < 5')
      Left: 
        ILocalReferenceOperation: i (OperationKind.LocalReference, Type: System.Int32) (Syntax: 'i')
      Right: 
        ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 5) (Syntax: '5')
  Before:
      IVariableDeclarationGroupOperation (1 declarations) (OperationKind.VariableDeclarationGroup, Type: null, IsImplicit) (Syntax: 'int i = 0')
        IVariableDeclarationOperation (1 declarators) (OperationKind.VariableDeclaration, Type: null) (Syntax: 'int i = 0')
          Declarators:
              IVariableDeclaratorOperation (Symbol: System.Int32 i) (OperationKind.VariableDeclarator, Type: null) (Syntax: 'i = 0')
                Initializer: 
                  IVariableInitializerOperation (OperationKind.VariableInitializer, Type: null) (Syntax: '= 0')
                    ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 0) (Syntax: '0')
          Initializer: 
            null
  AtLoopBottom:
      IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null, IsImplicit) (Syntax: 'i = i + 1')
        Expression: 
          ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Int32) (Syntax: 'i = i + 1')
            Left: 
              ILocalReferenceOperation: i (OperationKind.LocalReference, Type: System.Int32) (Syntax: 'i')
            Right: 
              IBinaryOperation (BinaryOperatorKind.Add) (OperationKind.BinaryOperator, Type: System.Int32) (Syntax: 'i + 1')
                Left: 
                  ILocalReferenceOperation: i (OperationKind.LocalReference, Type: System.Int32) (Syntax: 'i')
                Right: 
                  ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 1) (Syntax: '1')
  Body: 
    IBlockOperation (1 statements) (OperationKind.Block, Type: null) (Syntax: '{ ... }')
      IForLoopOperation (LoopKind.For) (OperationKind.Loop, Type: null) (Syntax: 'for (int j  ... }')
        Locals: Local_1: System.Int32 j
        Condition: 
          IBinaryOperation (BinaryOperatorKind.LessThan) (OperationKind.BinaryOperator, Type: System.Boolean) (Syntax: 'j < 10')
            Left: 
              ILocalReferenceOperation: j (OperationKind.LocalReference, Type: System.Int32) (Syntax: 'j')
            Right: 
              ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 10) (Syntax: '10')
        Before:
            IVariableDeclarationGroupOperation (1 declarations) (OperationKind.VariableDeclarationGroup, Type: null, IsImplicit) (Syntax: 'int j = 0')
              IVariableDeclarationOperation (1 declarators) (OperationKind.VariableDeclaration, Type: null) (Syntax: 'int j = 0')
                Declarators:
                    IVariableDeclaratorOperation (Symbol: System.Int32 j) (OperationKind.VariableDeclarator, Type: null) (Syntax: 'j = 0')
                      Initializer: 
                        IVariableInitializerOperation (OperationKind.VariableInitializer, Type: null) (Syntax: '= 0')
                          ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 0) (Syntax: '0')
                Initializer: 
                  null
        AtLoopBottom:
            IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null, IsImplicit) (Syntax: 'j = j + 1')
              Expression: 
                ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Int32) (Syntax: 'j = j + 1')
                  Left: 
                    ILocalReferenceOperation: j (OperationKind.LocalReference, Type: System.Int32) (Syntax: 'j')
                  Right: 
                    IBinaryOperation (BinaryOperatorKind.Add) (OperationKind.BinaryOperator, Type: System.Int32) (Syntax: 'j + 1')
                      Left: 
                        ILocalReferenceOperation: j (OperationKind.LocalReference, Type: System.Int32) (Syntax: 'j')
                      Right: 
                        ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 1) (Syntax: '1')
        Body: 
          IBlockOperation (2 statements) (OperationKind.Block, Type: null) (Syntax: '{ ... }')
            IBranchOperation (BranchKind.GoTo, Label: stop) (OperationKind.Branch, Type: null) (Syntax: 'goto stop;')
            ILabeledOperation (Label: stop) (OperationKind.Labeled, Type: null) (Syntax: 'stop: ... j = j + 1;')
              Statement: 
                IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'j = j + 1;')
                  Expression: 
                    ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Int32) (Syntax: 'j = j + 1')
                      Left: 
                        ILocalReferenceOperation: j (OperationKind.LocalReference, Type: System.Int32) (Syntax: 'j')
                      Right: 
                        IBinaryOperation (BinaryOperatorKind.Add) (OperationKind.BinaryOperator, Type: System.Int32) (Syntax: 'j + 1')
                          Left: 
                            ILocalReferenceOperation: j (OperationKind.LocalReference, Type: System.Int32) (Syntax: 'j')
                          Right: 
                            ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 1) (Syntax: '1')
";
            VerifyOperationTreeForTest<ForStatementSyntax>(source, expectedOperationTree);
        }

        [CompilerTrait(CompilerFeature.IOperation)]
        [Fact, WorkItem(17602, "https://github.com/dotnet/roslyn/issues/17602")]
        public void IForLoopStatement_ThrowException()
        {
            string source = @"
class C
{
    static void Main(string[] args)
    {
        /*<bind>*/for (int i = 0; i < 10; i = i + 1)
        {
            for (int j = 0; j < 10;)
            {
                throw new System.Exception();
            }
        }/*</bind>*/
    }
}

";
            string expectedOperationTree = @"
IForLoopOperation (LoopKind.For) (OperationKind.Loop, Type: null) (Syntax: 'for (int i  ... }')
  Locals: Local_1: System.Int32 i
  Condition: 
    IBinaryOperation (BinaryOperatorKind.LessThan) (OperationKind.BinaryOperator, Type: System.Boolean) (Syntax: 'i < 10')
      Left: 
        ILocalReferenceOperation: i (OperationKind.LocalReference, Type: System.Int32) (Syntax: 'i')
      Right: 
        ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 10) (Syntax: '10')
  Before:
      IVariableDeclarationGroupOperation (1 declarations) (OperationKind.VariableDeclarationGroup, Type: null, IsImplicit) (Syntax: 'int i = 0')
        IVariableDeclarationOperation (1 declarators) (OperationKind.VariableDeclaration, Type: null) (Syntax: 'int i = 0')
          Declarators:
              IVariableDeclaratorOperation (Symbol: System.Int32 i) (OperationKind.VariableDeclarator, Type: null) (Syntax: 'i = 0')
                Initializer: 
                  IVariableInitializerOperation (OperationKind.VariableInitializer, Type: null) (Syntax: '= 0')
                    ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 0) (Syntax: '0')
          Initializer: 
            null
  AtLoopBottom:
      IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null, IsImplicit) (Syntax: 'i = i + 1')
        Expression: 
          ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Int32) (Syntax: 'i = i + 1')
            Left: 
              ILocalReferenceOperation: i (OperationKind.LocalReference, Type: System.Int32) (Syntax: 'i')
            Right: 
              IBinaryOperation (BinaryOperatorKind.Add) (OperationKind.BinaryOperator, Type: System.Int32) (Syntax: 'i + 1')
                Left: 
                  ILocalReferenceOperation: i (OperationKind.LocalReference, Type: System.Int32) (Syntax: 'i')
                Right: 
                  ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 1) (Syntax: '1')
  Body: 
    IBlockOperation (1 statements) (OperationKind.Block, Type: null) (Syntax: '{ ... }')
      IForLoopOperation (LoopKind.For) (OperationKind.Loop, Type: null) (Syntax: 'for (int j  ... }')
        Locals: Local_1: System.Int32 j
        Condition: 
          IBinaryOperation (BinaryOperatorKind.LessThan) (OperationKind.BinaryOperator, Type: System.Boolean) (Syntax: 'j < 10')
            Left: 
              ILocalReferenceOperation: j (OperationKind.LocalReference, Type: System.Int32) (Syntax: 'j')
            Right: 
              ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 10) (Syntax: '10')
        Before:
            IVariableDeclarationGroupOperation (1 declarations) (OperationKind.VariableDeclarationGroup, Type: null, IsImplicit) (Syntax: 'int j = 0')
              IVariableDeclarationOperation (1 declarators) (OperationKind.VariableDeclaration, Type: null) (Syntax: 'int j = 0')
                Declarators:
                    IVariableDeclaratorOperation (Symbol: System.Int32 j) (OperationKind.VariableDeclarator, Type: null) (Syntax: 'j = 0')
                      Initializer: 
                        IVariableInitializerOperation (OperationKind.VariableInitializer, Type: null) (Syntax: '= 0')
                          ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 0) (Syntax: '0')
                Initializer: 
                  null
        AtLoopBottom(0)
        Body: 
          IBlockOperation (1 statements) (OperationKind.Block, Type: null) (Syntax: '{ ... }')
            IThrowOperation (OperationKind.Throw, Type: null) (Syntax: 'throw new S ... xception();')
              IObjectCreationOperation (Constructor: System.Exception..ctor()) (OperationKind.ObjectCreation, Type: System.Exception) (Syntax: 'new System.Exception()')
                Arguments(0)
                Initializer: 
                  null
";
            VerifyOperationTreeForTest<ForStatementSyntax>(source, expectedOperationTree);
        }

        [CompilerTrait(CompilerFeature.IOperation)]
        [Fact, WorkItem(17602, "https://github.com/dotnet/roslyn/issues/17602")]
        public void IForLoopStatement_ReturnInFor()
        {
            string source = @"
class C
{
    static void Main(string[] args)
    {
        /*<bind>*/for (int i = 0; i < 10;)
        {
            for (int j = 0; j < 5;)
            {
                return;
            }
        }/*</bind>*/
    }
}

";
            string expectedOperationTree = @"
IForLoopOperation (LoopKind.For) (OperationKind.Loop, Type: null) (Syntax: 'for (int i  ... }')
  Locals: Local_1: System.Int32 i
  Condition: 
    IBinaryOperation (BinaryOperatorKind.LessThan) (OperationKind.BinaryOperator, Type: System.Boolean) (Syntax: 'i < 10')
      Left: 
        ILocalReferenceOperation: i (OperationKind.LocalReference, Type: System.Int32) (Syntax: 'i')
      Right: 
        ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 10) (Syntax: '10')
  Before:
      IVariableDeclarationGroupOperation (1 declarations) (OperationKind.VariableDeclarationGroup, Type: null, IsImplicit) (Syntax: 'int i = 0')
        IVariableDeclarationOperation (1 declarators) (OperationKind.VariableDeclaration, Type: null) (Syntax: 'int i = 0')
          Declarators:
              IVariableDeclaratorOperation (Symbol: System.Int32 i) (OperationKind.VariableDeclarator, Type: null) (Syntax: 'i = 0')
                Initializer: 
                  IVariableInitializerOperation (OperationKind.VariableInitializer, Type: null) (Syntax: '= 0')
                    ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 0) (Syntax: '0')
          Initializer: 
            null
  AtLoopBottom(0)
  Body: 
    IBlockOperation (1 statements) (OperationKind.Block, Type: null) (Syntax: '{ ... }')
      IForLoopOperation (LoopKind.For) (OperationKind.Loop, Type: null) (Syntax: 'for (int j  ... }')
        Locals: Local_1: System.Int32 j
        Condition: 
          IBinaryOperation (BinaryOperatorKind.LessThan) (OperationKind.BinaryOperator, Type: System.Boolean) (Syntax: 'j < 5')
            Left: 
              ILocalReferenceOperation: j (OperationKind.LocalReference, Type: System.Int32) (Syntax: 'j')
            Right: 
              ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 5) (Syntax: '5')
        Before:
            IVariableDeclarationGroupOperation (1 declarations) (OperationKind.VariableDeclarationGroup, Type: null, IsImplicit) (Syntax: 'int j = 0')
              IVariableDeclarationOperation (1 declarators) (OperationKind.VariableDeclaration, Type: null) (Syntax: 'int j = 0')
                Declarators:
                    IVariableDeclaratorOperation (Symbol: System.Int32 j) (OperationKind.VariableDeclarator, Type: null) (Syntax: 'j = 0')
                      Initializer: 
                        IVariableInitializerOperation (OperationKind.VariableInitializer, Type: null) (Syntax: '= 0')
                          ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 0) (Syntax: '0')
                Initializer: 
                  null
        AtLoopBottom(0)
        Body: 
          IBlockOperation (1 statements) (OperationKind.Block, Type: null) (Syntax: '{ ... }')
            IReturnOperation (OperationKind.Return, Type: null) (Syntax: 'return;')
              ReturnedValue: 
                null
";
            VerifyOperationTreeForTest<ForStatementSyntax>(source, expectedOperationTree);
        }

        [CompilerTrait(CompilerFeature.IOperation)]
        [Fact, WorkItem(17602, "https://github.com/dotnet/roslyn/issues/17602")]
        public void IForLoopStatement_ChangeValueOfInit()
        {
            string source = @"
class C
{
    static void Main(string[] args)
    {
        /*<bind>*/for (int i = 0, j = 1; i < 5; i = i + 1)
        {
            j = 2;
        }/*</bind>*/
    }
}

";
            string expectedOperationTree = @"
IForLoopOperation (LoopKind.For) (OperationKind.Loop, Type: null) (Syntax: 'for (int i  ... }')
  Locals: Local_1: System.Int32 i
    Local_2: System.Int32 j
  Condition: 
    IBinaryOperation (BinaryOperatorKind.LessThan) (OperationKind.BinaryOperator, Type: System.Boolean) (Syntax: 'i < 5')
      Left: 
        ILocalReferenceOperation: i (OperationKind.LocalReference, Type: System.Int32) (Syntax: 'i')
      Right: 
        ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 5) (Syntax: '5')
  Before:
      IVariableDeclarationGroupOperation (1 declarations) (OperationKind.VariableDeclarationGroup, Type: null, IsImplicit) (Syntax: 'int i = 0, j = 1')
        IVariableDeclarationOperation (2 declarators) (OperationKind.VariableDeclaration, Type: null) (Syntax: 'int i = 0, j = 1')
          Declarators:
              IVariableDeclaratorOperation (Symbol: System.Int32 i) (OperationKind.VariableDeclarator, Type: null) (Syntax: 'i = 0')
                Initializer: 
                  IVariableInitializerOperation (OperationKind.VariableInitializer, Type: null) (Syntax: '= 0')
                    ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 0) (Syntax: '0')
              IVariableDeclaratorOperation (Symbol: System.Int32 j) (OperationKind.VariableDeclarator, Type: null) (Syntax: 'j = 1')
                Initializer: 
                  IVariableInitializerOperation (OperationKind.VariableInitializer, Type: null) (Syntax: '= 1')
                    ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 1) (Syntax: '1')
          Initializer: 
            null
  AtLoopBottom:
      IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null, IsImplicit) (Syntax: 'i = i + 1')
        Expression: 
          ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Int32) (Syntax: 'i = i + 1')
            Left: 
              ILocalReferenceOperation: i (OperationKind.LocalReference, Type: System.Int32) (Syntax: 'i')
            Right: 
              IBinaryOperation (BinaryOperatorKind.Add) (OperationKind.BinaryOperator, Type: System.Int32) (Syntax: 'i + 1')
                Left: 
                  ILocalReferenceOperation: i (OperationKind.LocalReference, Type: System.Int32) (Syntax: 'i')
                Right: 
                  ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 1) (Syntax: '1')
  Body: 
    IBlockOperation (1 statements) (OperationKind.Block, Type: null) (Syntax: '{ ... }')
      IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'j = 2;')
        Expression: 
          ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Int32) (Syntax: 'j = 2')
            Left: 
              ILocalReferenceOperation: j (OperationKind.LocalReference, Type: System.Int32) (Syntax: 'j')
            Right: 
              ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 2) (Syntax: '2')
";
            VerifyOperationTreeForTest<ForStatementSyntax>(source, expectedOperationTree);
        }

        [CompilerTrait(CompilerFeature.IOperation)]
        [Fact, WorkItem(17602, "https://github.com/dotnet/roslyn/issues/17602")]
        public void IForLoopStatement_ChangeValueOfCondition()
        {
            string source = @"
class C
{
    static void Main(string[] args)
    {
        int c = 0, x = 0;
        /*<bind>*/for (int i = 0; i < 50 - x; i = i + 1)
        {
            x = x + 1;
            c = c + 1;
        }/*</bind>*/
    }
}

";
            string expectedOperationTree = @"
IForLoopOperation (LoopKind.For) (OperationKind.Loop, Type: null) (Syntax: 'for (int i  ... }')
  Locals: Local_1: System.Int32 i
  Condition: 
    IBinaryOperation (BinaryOperatorKind.LessThan) (OperationKind.BinaryOperator, Type: System.Boolean) (Syntax: 'i < 50 - x')
      Left: 
        ILocalReferenceOperation: i (OperationKind.LocalReference, Type: System.Int32) (Syntax: 'i')
      Right: 
        IBinaryOperation (BinaryOperatorKind.Subtract) (OperationKind.BinaryOperator, Type: System.Int32) (Syntax: '50 - x')
          Left: 
            ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 50) (Syntax: '50')
          Right: 
            ILocalReferenceOperation: x (OperationKind.LocalReference, Type: System.Int32) (Syntax: 'x')
  Before:
      IVariableDeclarationGroupOperation (1 declarations) (OperationKind.VariableDeclarationGroup, Type: null, IsImplicit) (Syntax: 'int i = 0')
        IVariableDeclarationOperation (1 declarators) (OperationKind.VariableDeclaration, Type: null) (Syntax: 'int i = 0')
          Declarators:
              IVariableDeclaratorOperation (Symbol: System.Int32 i) (OperationKind.VariableDeclarator, Type: null) (Syntax: 'i = 0')
                Initializer: 
                  IVariableInitializerOperation (OperationKind.VariableInitializer, Type: null) (Syntax: '= 0')
                    ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 0) (Syntax: '0')
          Initializer: 
            null
  AtLoopBottom:
      IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null, IsImplicit) (Syntax: 'i = i + 1')
        Expression: 
          ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Int32) (Syntax: 'i = i + 1')
            Left: 
              ILocalReferenceOperation: i (OperationKind.LocalReference, Type: System.Int32) (Syntax: 'i')
            Right: 
              IBinaryOperation (BinaryOperatorKind.Add) (OperationKind.BinaryOperator, Type: System.Int32) (Syntax: 'i + 1')
                Left: 
                  ILocalReferenceOperation: i (OperationKind.LocalReference, Type: System.Int32) (Syntax: 'i')
                Right: 
                  ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 1) (Syntax: '1')
  Body: 
    IBlockOperation (2 statements) (OperationKind.Block, Type: null) (Syntax: '{ ... }')
      IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'x = x + 1;')
        Expression: 
          ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Int32) (Syntax: 'x = x + 1')
            Left: 
              ILocalReferenceOperation: x (OperationKind.LocalReference, Type: System.Int32) (Syntax: 'x')
            Right: 
              IBinaryOperation (BinaryOperatorKind.Add) (OperationKind.BinaryOperator, Type: System.Int32) (Syntax: 'x + 1')
                Left: 
                  ILocalReferenceOperation: x (OperationKind.LocalReference, Type: System.Int32) (Syntax: 'x')
                Right: 
                  ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 1) (Syntax: '1')
      IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'c = c + 1;')
        Expression: 
          ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Int32) (Syntax: 'c = c + 1')
            Left: 
              ILocalReferenceOperation: c (OperationKind.LocalReference, Type: System.Int32) (Syntax: 'c')
            Right: 
              IBinaryOperation (BinaryOperatorKind.Add) (OperationKind.BinaryOperator, Type: System.Int32) (Syntax: 'c + 1')
                Left: 
                  ILocalReferenceOperation: c (OperationKind.LocalReference, Type: System.Int32) (Syntax: 'c')
                Right: 
                  ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 1) (Syntax: '1')
";
            VerifyOperationTreeForTest<ForStatementSyntax>(source, expectedOperationTree);
        }

        [CompilerTrait(CompilerFeature.IOperation)]
        [Fact, WorkItem(17602, "https://github.com/dotnet/roslyn/issues/17602")]
        public void IForLoopStatement_UnreachableCode1()
        {
            string source = @"
class C
{
    static void Main(string[] args)
    {
        /*<bind>*/for (; false;)
        {
            System.Console.WriteLine(""hello"");        //unreachable
        }/*</bind>*/
    }
}
";
            string expectedOperationTree = @"
IForLoopOperation (LoopKind.For) (OperationKind.Loop, Type: null) (Syntax: 'for (; fals ... }')
  Condition: 
    ILiteralOperation (OperationKind.Literal, Type: System.Boolean, Constant: False) (Syntax: 'false')
  Before(0)
  AtLoopBottom(0)
  Body: 
    IBlockOperation (1 statements) (OperationKind.Block, Type: null) (Syntax: '{ ... }')
      IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'System.Cons ... e(""hello"");')
        Expression: 
          IInvocationOperation (void System.Console.WriteLine(System.String value)) (OperationKind.Invocation, Type: System.Void) (Syntax: 'System.Cons ... ne(""hello"")')
            Instance Receiver: 
              null
            Arguments(1):
                IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: value) (OperationKind.Argument, Type: null) (Syntax: '""hello""')
                  ILiteralOperation (OperationKind.Literal, Type: System.String, Constant: ""hello"") (Syntax: '""hello""')
                  InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                  OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
";
            VerifyOperationTreeForTest<ForStatementSyntax>(source, expectedOperationTree);
        }

        [CompilerTrait(CompilerFeature.IOperation)]
        [Fact, WorkItem(17602, "https://github.com/dotnet/roslyn/issues/17602")]
        public void IForLoopStatement_ObjectInitAsInitializer()
        {
            string source = @"
class C
{
    static void Main(string[] args)
    {
        /*<bind>*/for (F f = new F { i = 0, s = ""abc"" }; f.i < 5; f.i = f.i + 1)
        {
        }/*</bind>*/
    }
}
public class F
{
    public int i;
    public string s;
}

";
            string expectedOperationTree = @"
IForLoopOperation (LoopKind.For) (OperationKind.Loop, Type: null) (Syntax: 'for (F f =  ... }')
  Locals: Local_1: F f
  Condition: 
    IBinaryOperation (BinaryOperatorKind.LessThan) (OperationKind.BinaryOperator, Type: System.Boolean) (Syntax: 'f.i < 5')
      Left: 
        IFieldReferenceOperation: System.Int32 F.i (OperationKind.FieldReference, Type: System.Int32) (Syntax: 'f.i')
          Instance Receiver: 
            ILocalReferenceOperation: f (OperationKind.LocalReference, Type: F) (Syntax: 'f')
      Right: 
        ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 5) (Syntax: '5')
  Before:
      IVariableDeclarationGroupOperation (1 declarations) (OperationKind.VariableDeclarationGroup, Type: null, IsImplicit) (Syntax: 'F f = new F ... s = ""abc"" }')
        IVariableDeclarationOperation (1 declarators) (OperationKind.VariableDeclaration, Type: null) (Syntax: 'F f = new F ... s = ""abc"" }')
          Declarators:
              IVariableDeclaratorOperation (Symbol: F f) (OperationKind.VariableDeclarator, Type: null) (Syntax: 'f = new F { ... s = ""abc"" }')
                Initializer: 
                  IVariableInitializerOperation (OperationKind.VariableInitializer, Type: null) (Syntax: '= new F { i ... s = ""abc"" }')
                    IObjectCreationOperation (Constructor: F..ctor()) (OperationKind.ObjectCreation, Type: F) (Syntax: 'new F { i = ... s = ""abc"" }')
                      Arguments(0)
                      Initializer: 
                        IObjectOrCollectionInitializerOperation (OperationKind.ObjectOrCollectionInitializer, Type: F) (Syntax: '{ i = 0, s = ""abc"" }')
                          Initializers(2):
                              ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Int32) (Syntax: 'i = 0')
                                Left: 
                                  IFieldReferenceOperation: System.Int32 F.i (OperationKind.FieldReference, Type: System.Int32) (Syntax: 'i')
                                    Instance Receiver: 
                                      IInstanceReferenceOperation (OperationKind.InstanceReference, Type: F, IsImplicit) (Syntax: 'i')
                                Right: 
                                  ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 0) (Syntax: '0')
                              ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.String) (Syntax: 's = ""abc""')
                                Left: 
                                  IFieldReferenceOperation: System.String F.s (OperationKind.FieldReference, Type: System.String) (Syntax: 's')
                                    Instance Receiver: 
                                      IInstanceReferenceOperation (OperationKind.InstanceReference, Type: F, IsImplicit) (Syntax: 's')
                                Right: 
                                  ILiteralOperation (OperationKind.Literal, Type: System.String, Constant: ""abc"") (Syntax: '""abc""')
          Initializer: 
            null
  AtLoopBottom:
      IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null, IsImplicit) (Syntax: 'f.i = f.i + 1')
        Expression: 
          ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Int32) (Syntax: 'f.i = f.i + 1')
            Left: 
              IFieldReferenceOperation: System.Int32 F.i (OperationKind.FieldReference, Type: System.Int32) (Syntax: 'f.i')
                Instance Receiver: 
                  ILocalReferenceOperation: f (OperationKind.LocalReference, Type: F) (Syntax: 'f')
            Right: 
              IBinaryOperation (BinaryOperatorKind.Add) (OperationKind.BinaryOperator, Type: System.Int32) (Syntax: 'f.i + 1')
                Left: 
                  IFieldReferenceOperation: System.Int32 F.i (OperationKind.FieldReference, Type: System.Int32) (Syntax: 'f.i')
                    Instance Receiver: 
                      ILocalReferenceOperation: f (OperationKind.LocalReference, Type: F) (Syntax: 'f')
                Right: 
                  ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 1) (Syntax: '1')
  Body: 
    IBlockOperation (0 statements) (OperationKind.Block, Type: null) (Syntax: '{ ... }')
";
            VerifyOperationTreeForTest<ForStatementSyntax>(source, expectedOperationTree);
        }

        [CompilerTrait(CompilerFeature.IOperation)]
        [Fact, WorkItem(17602, "https://github.com/dotnet/roslyn/issues/17602")]
        public void IForLoopStatement_DynamicInFor()
        {
            string source = @"
class C
{
    static void Main(string[] args)
    {
        dynamic d = new myFor();
        /*<bind>*/for (d.Initialize(5); d.Done; d.Next())
        {
        }/*</bind>*/
    }
}

public class myFor
{
    int index;
    int max;
    public void Initialize(int max)
    {
        index = 0;
        this.max = max;
        System.Console.WriteLine(""Initialize"");
    }
    public bool Done
    {
        get
        {
            System.Console.WriteLine(""Done"");
            return index < max;
        }
    }
    public void Next()
    {
        index = index + 1;
        System.Console.WriteLine(""Next"");
    }
}
";
            string expectedOperationTree = @"
IForLoopOperation (LoopKind.For) (OperationKind.Loop, Type: null) (Syntax: 'for (d.Init ... }')
  Condition: 
    IUnaryOperation (UnaryOperatorKind.True) (OperationKind.UnaryOperator, Type: System.Boolean, IsImplicit) (Syntax: 'd.Done')
      Operand: 
        IDynamicMemberReferenceOperation (Member Name: ""Done"", Containing Type: null) (OperationKind.DynamicMemberReference, Type: dynamic) (Syntax: 'd.Done')
          Type Arguments(0)
          Instance Receiver: 
            ILocalReferenceOperation: d (OperationKind.LocalReference, Type: dynamic) (Syntax: 'd')
  Before:
      IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null, IsImplicit) (Syntax: 'd.Initialize(5)')
        Expression: 
          IDynamicInvocationOperation (OperationKind.DynamicInvocation, Type: dynamic) (Syntax: 'd.Initialize(5)')
            Expression: 
              IDynamicMemberReferenceOperation (Member Name: ""Initialize"", Containing Type: null) (OperationKind.DynamicMemberReference, Type: dynamic) (Syntax: 'd.Initialize')
                Type Arguments(0)
                Instance Receiver: 
                  ILocalReferenceOperation: d (OperationKind.LocalReference, Type: dynamic) (Syntax: 'd')
            Arguments(1):
                ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 5) (Syntax: '5')
            ArgumentNames(0)
            ArgumentRefKinds(0)
  AtLoopBottom:
      IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null, IsImplicit) (Syntax: 'd.Next()')
        Expression: 
          IDynamicInvocationOperation (OperationKind.DynamicInvocation, Type: dynamic) (Syntax: 'd.Next()')
            Expression: 
              IDynamicMemberReferenceOperation (Member Name: ""Next"", Containing Type: null) (OperationKind.DynamicMemberReference, Type: dynamic) (Syntax: 'd.Next')
                Type Arguments(0)
                Instance Receiver: 
                  ILocalReferenceOperation: d (OperationKind.LocalReference, Type: dynamic) (Syntax: 'd')
            Arguments(0)
            ArgumentNames(0)
            ArgumentRefKinds(0)
  Body: 
    IBlockOperation (0 statements) (OperationKind.Block, Type: null) (Syntax: '{ ... }')
";

            VerifyOperationTreeForTest<ForStatementSyntax>(source, expectedOperationTree);
        }

        [CompilerTrait(CompilerFeature.IOperation)]
        [Fact, WorkItem(17602, "https://github.com/dotnet/roslyn/issues/17602")]
        public void IForLoopStatement_VarInFor()
        {
            string source = @"
class C
{
    static void Main(string[] args)
    {
        /*<bind>*/for (var i = 1; i < 5; i = i + 1) ;/*</bind>*/
    }
}

";
            string expectedOperationTree = @"
IForLoopOperation (LoopKind.For) (OperationKind.Loop, Type: null) (Syntax: 'for (var i  ...  = i + 1) ;')
  Locals: Local_1: System.Int32 i
  Condition: 
    IBinaryOperation (BinaryOperatorKind.LessThan) (OperationKind.BinaryOperator, Type: System.Boolean) (Syntax: 'i < 5')
      Left: 
        ILocalReferenceOperation: i (OperationKind.LocalReference, Type: System.Int32) (Syntax: 'i')
      Right: 
        ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 5) (Syntax: '5')
  Before:
      IVariableDeclarationGroupOperation (1 declarations) (OperationKind.VariableDeclarationGroup, Type: null, IsImplicit) (Syntax: 'var i = 1')
        IVariableDeclarationOperation (1 declarators) (OperationKind.VariableDeclaration, Type: null) (Syntax: 'var i = 1')
          Declarators:
              IVariableDeclaratorOperation (Symbol: System.Int32 i) (OperationKind.VariableDeclarator, Type: null) (Syntax: 'i = 1')
                Initializer: 
                  IVariableInitializerOperation (OperationKind.VariableInitializer, Type: null) (Syntax: '= 1')
                    ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 1) (Syntax: '1')
          Initializer: 
            null
  AtLoopBottom:
      IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null, IsImplicit) (Syntax: 'i = i + 1')
        Expression: 
          ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Int32) (Syntax: 'i = i + 1')
            Left: 
              ILocalReferenceOperation: i (OperationKind.LocalReference, Type: System.Int32) (Syntax: 'i')
            Right: 
              IBinaryOperation (BinaryOperatorKind.Add) (OperationKind.BinaryOperator, Type: System.Int32) (Syntax: 'i + 1')
                Left: 
                  ILocalReferenceOperation: i (OperationKind.LocalReference, Type: System.Int32) (Syntax: 'i')
                Right: 
                  ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 1) (Syntax: '1')
  Body: 
    IEmptyOperation (OperationKind.Empty, Type: null) (Syntax: ';')
";
            VerifyOperationTreeForTest<ForStatementSyntax>(source, expectedOperationTree);
        }

        [CompilerTrait(CompilerFeature.IOperation)]
        [Fact, WorkItem(17602, "https://github.com/dotnet/roslyn/issues/17602")]
        public void IForLoopStatement_QueryInInit()
        {
            string source = @"
using System.Linq;
using System.Collections.Generic;
class C
{
    static void Main(string[] args)
    {
        /*<bind>*/for (IEnumerable<string> str = from x in ""123""
                                       let z = x.ToString()
                                       select z into w
                                       select w; ; )
        {
            foreach (var item in str)
            {
                System.Console.WriteLine(item);
            }
            return;
        }/*</bind>*/
    }
}
";
            string expectedOperationTree = @"
IForLoopOperation (LoopKind.For) (OperationKind.Loop, Type: null) (Syntax: 'for (IEnume ... }')
  Locals: Local_1: System.Collections.Generic.IEnumerable<System.String> str
  Condition: 
    null
  Before:
      IVariableDeclarationGroupOperation (1 declarations) (OperationKind.VariableDeclarationGroup, Type: null, IsImplicit) (Syntax: 'IEnumerable ... select w')
        IVariableDeclarationOperation (1 declarators) (OperationKind.VariableDeclaration, Type: null) (Syntax: 'IEnumerable ... select w')
          Declarators:
              IVariableDeclaratorOperation (Symbol: System.Collections.Generic.IEnumerable<System.String> str) (OperationKind.VariableDeclarator, Type: null) (Syntax: 'str = from  ... select w')
                Initializer: 
                  IVariableInitializerOperation (OperationKind.VariableInitializer, Type: null) (Syntax: '= from x in ... select w')
                    ITranslatedQueryOperation (OperationKind.TranslatedQuery, Type: System.Collections.Generic.IEnumerable<System.String>) (Syntax: 'from x in "" ... select w')
                      Expression: 
                        IInvocationOperation (System.Collections.Generic.IEnumerable<System.String> System.Linq.Enumerable.Select<System.String, System.String>(this System.Collections.Generic.IEnumerable<System.String> source, System.Func<System.String, System.String> selector)) (OperationKind.Invocation, Type: System.Collections.Generic.IEnumerable<System.String>, IsImplicit) (Syntax: 'select w')
                          Instance Receiver: 
                            null
                          Arguments(2):
                              IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: source) (OperationKind.Argument, Type: null, IsImplicit) (Syntax: 'select z')
                                IInvocationOperation (System.Collections.Generic.IEnumerable<System.String> System.Linq.Enumerable.Select<<anonymous type: System.Char x, System.String z>, System.String>(this System.Collections.Generic.IEnumerable<<anonymous type: System.Char x, System.String z>> source, System.Func<<anonymous type: System.Char x, System.String z>, System.String> selector)) (OperationKind.Invocation, Type: System.Collections.Generic.IEnumerable<System.String>, IsImplicit) (Syntax: 'select z')
                                  Instance Receiver: 
                                    null
                                  Arguments(2):
                                      IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: source) (OperationKind.Argument, Type: null, IsImplicit) (Syntax: 'let z = x.ToString()')
                                        IInvocationOperation (System.Collections.Generic.IEnumerable<<anonymous type: System.Char x, System.String z>> System.Linq.Enumerable.Select<System.Char, <anonymous type: System.Char x, System.String z>>(this System.Collections.Generic.IEnumerable<System.Char> source, System.Func<System.Char, <anonymous type: System.Char x, System.String z>> selector)) (OperationKind.Invocation, Type: System.Collections.Generic.IEnumerable<<anonymous type: System.Char x, System.String z>>, IsImplicit) (Syntax: 'let z = x.ToString()')
                                          Instance Receiver: 
                                            null
                                          Arguments(2):
                                              IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: source) (OperationKind.Argument, Type: null, IsImplicit) (Syntax: 'from x in ""123""')
                                                IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.Collections.Generic.IEnumerable<System.Char>, IsImplicit) (Syntax: 'from x in ""123""')
                                                  Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: True, IsUserDefined: False) (MethodSymbol: null)
                                                  Operand: 
                                                    ILiteralOperation (OperationKind.Literal, Type: System.String, Constant: ""123"") (Syntax: '""123""')
                                                InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                                                OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                                              IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: selector) (OperationKind.Argument, Type: null, IsImplicit) (Syntax: 'x.ToString()')
                                                IDelegateCreationOperation (OperationKind.DelegateCreation, Type: System.Func<System.Char, <anonymous type: System.Char x, System.String z>>, IsImplicit) (Syntax: 'x.ToString()')
                                                  Target: 
                                                    IAnonymousFunctionOperation (Symbol: lambda expression) (OperationKind.AnonymousFunction, Type: null, IsImplicit) (Syntax: 'x.ToString()')
                                                      IBlockOperation (1 statements) (OperationKind.Block, Type: null, IsImplicit) (Syntax: 'x.ToString()')
                                                        IReturnOperation (OperationKind.Return, Type: null, IsImplicit) (Syntax: 'x.ToString()')
                                                          ReturnedValue: 
                                                            IObjectCreationOperation (Constructor: <anonymous type: System.Char x, System.String z>..ctor(System.Char x, System.String z)) (OperationKind.ObjectCreation, Type: <anonymous type: System.Char x, System.String z>, IsImplicit) (Syntax: 'let z = x.ToString()')
                                                              Arguments(2):
                                                                  IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: x) (OperationKind.Argument, Type: null, IsImplicit) (Syntax: 'let z = x.ToString()')
                                                                    IParameterReferenceOperation: x (OperationKind.ParameterReference, Type: System.Char, IsImplicit) (Syntax: 'let z = x.ToString()')
                                                                    InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                                                                    OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                                                                  IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: z) (OperationKind.Argument, Type: null, IsImplicit) (Syntax: 'x.ToString()')
                                                                    IInvocationOperation (virtual System.String System.Char.ToString()) (OperationKind.Invocation, Type: System.String) (Syntax: 'x.ToString()')
                                                                      Instance Receiver: 
                                                                        IParameterReferenceOperation: x (OperationKind.ParameterReference, Type: System.Char) (Syntax: 'x')
                                                                      Arguments(0)
                                                                    InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                                                                    OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                                                              Initializer: 
                                                                null
                                                InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                                                OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                                        InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                                        OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                                      IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: selector) (OperationKind.Argument, Type: null, IsImplicit) (Syntax: 'z')
                                        IDelegateCreationOperation (OperationKind.DelegateCreation, Type: System.Func<<anonymous type: System.Char x, System.String z>, System.String>, IsImplicit) (Syntax: 'z')
                                          Target: 
                                            IAnonymousFunctionOperation (Symbol: lambda expression) (OperationKind.AnonymousFunction, Type: null, IsImplicit) (Syntax: 'z')
                                              IBlockOperation (1 statements) (OperationKind.Block, Type: null, IsImplicit) (Syntax: 'z')
                                                IReturnOperation (OperationKind.Return, Type: null, IsImplicit) (Syntax: 'z')
                                                  ReturnedValue: 
                                                    IPropertyReferenceOperation: System.String <anonymous type: System.Char x, System.String z>.z { get; } (OperationKind.PropertyReference, Type: System.String, IsImplicit) (Syntax: 'z')
                                                      Instance Receiver: 
                                                        IParameterReferenceOperation: <>h__TransparentIdentifier0 (OperationKind.ParameterReference, Type: <anonymous type: System.Char x, System.String z>) (Syntax: 'z')
                                        InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                                        OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                                InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                                OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                              IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: selector) (OperationKind.Argument, Type: null, IsImplicit) (Syntax: 'w')
                                IDelegateCreationOperation (OperationKind.DelegateCreation, Type: System.Func<System.String, System.String>, IsImplicit) (Syntax: 'w')
                                  Target: 
                                    IAnonymousFunctionOperation (Symbol: lambda expression) (OperationKind.AnonymousFunction, Type: null, IsImplicit) (Syntax: 'w')
                                      IBlockOperation (1 statements) (OperationKind.Block, Type: null, IsImplicit) (Syntax: 'w')
                                        IReturnOperation (OperationKind.Return, Type: null, IsImplicit) (Syntax: 'w')
                                          ReturnedValue: 
                                            IParameterReferenceOperation: w (OperationKind.ParameterReference, Type: System.String) (Syntax: 'w')
                                InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                                OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
          Initializer: 
            null
  AtLoopBottom(0)
  Body: 
    IBlockOperation (2 statements) (OperationKind.Block, Type: null) (Syntax: '{ ... }')
      IForEachLoopOperation (LoopKind.ForEach) (OperationKind.Loop, Type: null) (Syntax: 'foreach (va ... }')
        Locals: Local_1: System.String item
        LoopControlVariable: 
          IVariableDeclaratorOperation (Symbol: System.String item) (OperationKind.VariableDeclarator, Type: null) (Syntax: 'var')
            Initializer: 
              null
        Collection: 
          IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.Collections.Generic.IEnumerable<System.String>, IsImplicit) (Syntax: 'str')
            Conversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
            Operand: 
              ILocalReferenceOperation: str (OperationKind.LocalReference, Type: System.Collections.Generic.IEnumerable<System.String>) (Syntax: 'str')
        Body: 
          IBlockOperation (1 statements) (OperationKind.Block, Type: null) (Syntax: '{ ... }')
            IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'System.Cons ... Line(item);')
              Expression: 
                IInvocationOperation (void System.Console.WriteLine(System.String value)) (OperationKind.Invocation, Type: System.Void) (Syntax: 'System.Cons ... eLine(item)')
                  Instance Receiver: 
                    null
                  Arguments(1):
                      IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: value) (OperationKind.Argument, Type: null) (Syntax: 'item')
                        ILocalReferenceOperation: item (OperationKind.LocalReference, Type: System.String) (Syntax: 'item')
                        InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                        OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
        NextVariables(0)
      IReturnOperation (OperationKind.Return, Type: null) (Syntax: 'return;')
        ReturnedValue: 
          null
";
            VerifyOperationTreeForTest<ForStatementSyntax>(source, expectedOperationTree);
        }

        [CompilerTrait(CompilerFeature.IOperation)]
        [Fact, WorkItem(17602, "https://github.com/dotnet/roslyn/issues/17602")]
        public void IForLoopStatement_QueryInBody()
        {
            string source = @"
using System.Linq;
using System.Collections.Generic;
class C
{
    static void Main(string[] args)
    {
        foreach (var item in fun())
        {
            System.Console.WriteLine(item);
        }
    }

    private static IEnumerable<string> fun()
    {
        /*<bind>*/for (int i = 0; i < 5;)
        {
            return from x in ""123""
                   let z = x.ToString()
                   select z into w
                   select w;
        }/*</bind>*/
        return null;
    }
}

";
            string expectedOperationTree = @"
IForLoopOperation (LoopKind.For) (OperationKind.Loop, Type: null) (Syntax: 'for (int i  ... }')
  Locals: Local_1: System.Int32 i
  Condition: 
    IBinaryOperation (BinaryOperatorKind.LessThan) (OperationKind.BinaryOperator, Type: System.Boolean) (Syntax: 'i < 5')
      Left: 
        ILocalReferenceOperation: i (OperationKind.LocalReference, Type: System.Int32) (Syntax: 'i')
      Right: 
        ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 5) (Syntax: '5')
  Before:
      IVariableDeclarationGroupOperation (1 declarations) (OperationKind.VariableDeclarationGroup, Type: null, IsImplicit) (Syntax: 'int i = 0')
        IVariableDeclarationOperation (1 declarators) (OperationKind.VariableDeclaration, Type: null) (Syntax: 'int i = 0')
          Declarators:
              IVariableDeclaratorOperation (Symbol: System.Int32 i) (OperationKind.VariableDeclarator, Type: null) (Syntax: 'i = 0')
                Initializer: 
                  IVariableInitializerOperation (OperationKind.VariableInitializer, Type: null) (Syntax: '= 0')
                    ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 0) (Syntax: '0')
          Initializer: 
            null
  AtLoopBottom(0)
  Body: 
    IBlockOperation (1 statements) (OperationKind.Block, Type: null) (Syntax: '{ ... }')
      IReturnOperation (OperationKind.Return, Type: null) (Syntax: 'return from ... select w;')
        ReturnedValue: 
          ITranslatedQueryOperation (OperationKind.TranslatedQuery, Type: System.Collections.Generic.IEnumerable<System.String>) (Syntax: 'from x in "" ... select w')
            Expression: 
              IInvocationOperation (System.Collections.Generic.IEnumerable<System.String> System.Linq.Enumerable.Select<System.String, System.String>(this System.Collections.Generic.IEnumerable<System.String> source, System.Func<System.String, System.String> selector)) (OperationKind.Invocation, Type: System.Collections.Generic.IEnumerable<System.String>, IsImplicit) (Syntax: 'select w')
                Instance Receiver: 
                  null
                Arguments(2):
                    IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: source) (OperationKind.Argument, Type: null, IsImplicit) (Syntax: 'select z')
                      IInvocationOperation (System.Collections.Generic.IEnumerable<System.String> System.Linq.Enumerable.Select<<anonymous type: System.Char x, System.String z>, System.String>(this System.Collections.Generic.IEnumerable<<anonymous type: System.Char x, System.String z>> source, System.Func<<anonymous type: System.Char x, System.String z>, System.String> selector)) (OperationKind.Invocation, Type: System.Collections.Generic.IEnumerable<System.String>, IsImplicit) (Syntax: 'select z')
                        Instance Receiver: 
                          null
                        Arguments(2):
                            IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: source) (OperationKind.Argument, Type: null, IsImplicit) (Syntax: 'let z = x.ToString()')
                              IInvocationOperation (System.Collections.Generic.IEnumerable<<anonymous type: System.Char x, System.String z>> System.Linq.Enumerable.Select<System.Char, <anonymous type: System.Char x, System.String z>>(this System.Collections.Generic.IEnumerable<System.Char> source, System.Func<System.Char, <anonymous type: System.Char x, System.String z>> selector)) (OperationKind.Invocation, Type: System.Collections.Generic.IEnumerable<<anonymous type: System.Char x, System.String z>>, IsImplicit) (Syntax: 'let z = x.ToString()')
                                Instance Receiver: 
                                  null
                                Arguments(2):
                                    IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: source) (OperationKind.Argument, Type: null, IsImplicit) (Syntax: 'from x in ""123""')
                                      IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.Collections.Generic.IEnumerable<System.Char>, IsImplicit) (Syntax: 'from x in ""123""')
                                        Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: True, IsUserDefined: False) (MethodSymbol: null)
                                        Operand: 
                                          ILiteralOperation (OperationKind.Literal, Type: System.String, Constant: ""123"") (Syntax: '""123""')
                                      InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                                      OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                                    IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: selector) (OperationKind.Argument, Type: null, IsImplicit) (Syntax: 'x.ToString()')
                                      IDelegateCreationOperation (OperationKind.DelegateCreation, Type: System.Func<System.Char, <anonymous type: System.Char x, System.String z>>, IsImplicit) (Syntax: 'x.ToString()')
                                        Target: 
                                          IAnonymousFunctionOperation (Symbol: lambda expression) (OperationKind.AnonymousFunction, Type: null, IsImplicit) (Syntax: 'x.ToString()')
                                            IBlockOperation (1 statements) (OperationKind.Block, Type: null, IsImplicit) (Syntax: 'x.ToString()')
                                              IReturnOperation (OperationKind.Return, Type: null, IsImplicit) (Syntax: 'x.ToString()')
                                                ReturnedValue: 
                                                  IObjectCreationOperation (Constructor: <anonymous type: System.Char x, System.String z>..ctor(System.Char x, System.String z)) (OperationKind.ObjectCreation, Type: <anonymous type: System.Char x, System.String z>, IsImplicit) (Syntax: 'let z = x.ToString()')
                                                    Arguments(2):
                                                        IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: x) (OperationKind.Argument, Type: null, IsImplicit) (Syntax: 'let z = x.ToString()')
                                                          IParameterReferenceOperation: x (OperationKind.ParameterReference, Type: System.Char, IsImplicit) (Syntax: 'let z = x.ToString()')
                                                          InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                                                          OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                                                        IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: z) (OperationKind.Argument, Type: null, IsImplicit) (Syntax: 'x.ToString()')
                                                          IInvocationOperation (virtual System.String System.Char.ToString()) (OperationKind.Invocation, Type: System.String) (Syntax: 'x.ToString()')
                                                            Instance Receiver: 
                                                              IParameterReferenceOperation: x (OperationKind.ParameterReference, Type: System.Char) (Syntax: 'x')
                                                            Arguments(0)
                                                          InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                                                          OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                                                    Initializer: 
                                                      null
                                      InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                                      OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                              InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                              OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                            IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: selector) (OperationKind.Argument, Type: null, IsImplicit) (Syntax: 'z')
                              IDelegateCreationOperation (OperationKind.DelegateCreation, Type: System.Func<<anonymous type: System.Char x, System.String z>, System.String>, IsImplicit) (Syntax: 'z')
                                Target: 
                                  IAnonymousFunctionOperation (Symbol: lambda expression) (OperationKind.AnonymousFunction, Type: null, IsImplicit) (Syntax: 'z')
                                    IBlockOperation (1 statements) (OperationKind.Block, Type: null, IsImplicit) (Syntax: 'z')
                                      IReturnOperation (OperationKind.Return, Type: null, IsImplicit) (Syntax: 'z')
                                        ReturnedValue: 
                                          IPropertyReferenceOperation: System.String <anonymous type: System.Char x, System.String z>.z { get; } (OperationKind.PropertyReference, Type: System.String, IsImplicit) (Syntax: 'z')
                                            Instance Receiver: 
                                              IParameterReferenceOperation: <>h__TransparentIdentifier0 (OperationKind.ParameterReference, Type: <anonymous type: System.Char x, System.String z>) (Syntax: 'z')
                              InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                              OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                      InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                      OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                    IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: selector) (OperationKind.Argument, Type: null, IsImplicit) (Syntax: 'w')
                      IDelegateCreationOperation (OperationKind.DelegateCreation, Type: System.Func<System.String, System.String>, IsImplicit) (Syntax: 'w')
                        Target: 
                          IAnonymousFunctionOperation (Symbol: lambda expression) (OperationKind.AnonymousFunction, Type: null, IsImplicit) (Syntax: 'w')
                            IBlockOperation (1 statements) (OperationKind.Block, Type: null, IsImplicit) (Syntax: 'w')
                              IReturnOperation (OperationKind.Return, Type: null, IsImplicit) (Syntax: 'w')
                                ReturnedValue: 
                                  IParameterReferenceOperation: w (OperationKind.ParameterReference, Type: System.String) (Syntax: 'w')
                      InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                      OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
";
            VerifyOperationTreeForTest<ForStatementSyntax>(source, expectedOperationTree);
        }

        [CompilerTrait(CompilerFeature.IOperation)]
        [Fact, WorkItem(17602, "https://github.com/dotnet/roslyn/issues/17602")]
        public void IForLoopStatement_ExpressiontreeInInit()
        {
            string source = @"
class C
{
    static void Main(string[] args)
    {
        System.Linq.Expressions.Expression<System.Func<int, int>> e = x => x % 6;
        int i = 1;
        /*<bind>*/for (e = x => x * x; i < 5; i++)
        {
            var lambda = e.Compile();
            System.Console.WriteLine(lambda(i));
        }/*</bind>*/
    }
}

";
            string expectedOperationTree = @"
IForLoopOperation (LoopKind.For) (OperationKind.Loop, Type: null) (Syntax: 'for (e = x  ... }')
  Condition: 
    IBinaryOperation (BinaryOperatorKind.LessThan) (OperationKind.BinaryOperator, Type: System.Boolean) (Syntax: 'i < 5')
      Left: 
        ILocalReferenceOperation: i (OperationKind.LocalReference, Type: System.Int32) (Syntax: 'i')
      Right: 
        ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 5) (Syntax: '5')
  Before:
      IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null, IsImplicit) (Syntax: 'e = x => x * x')
        Expression: 
          ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Linq.Expressions.Expression<System.Func<System.Int32, System.Int32>>) (Syntax: 'e = x => x * x')
            Left: 
              ILocalReferenceOperation: e (OperationKind.LocalReference, Type: System.Linq.Expressions.Expression<System.Func<System.Int32, System.Int32>>) (Syntax: 'e')
            Right: 
              IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.Linq.Expressions.Expression<System.Func<System.Int32, System.Int32>>, IsImplicit) (Syntax: 'x => x * x')
                Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                Operand: 
                  IAnonymousFunctionOperation (Symbol: lambda expression) (OperationKind.AnonymousFunction, Type: null) (Syntax: 'x => x * x')
                    IBlockOperation (1 statements) (OperationKind.Block, Type: null, IsImplicit) (Syntax: 'x * x')
                      IReturnOperation (OperationKind.Return, Type: null, IsImplicit) (Syntax: 'x * x')
                        ReturnedValue: 
                          IBinaryOperation (BinaryOperatorKind.Multiply) (OperationKind.BinaryOperator, Type: System.Int32) (Syntax: 'x * x')
                            Left: 
                              IParameterReferenceOperation: x (OperationKind.ParameterReference, Type: System.Int32) (Syntax: 'x')
                            Right: 
                              IParameterReferenceOperation: x (OperationKind.ParameterReference, Type: System.Int32) (Syntax: 'x')
  AtLoopBottom:
      IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null, IsImplicit) (Syntax: 'i++')
        Expression: 
          IIncrementOrDecrementOperation (Postfix) (OperationKind.Increment, Type: System.Int32) (Syntax: 'i++')
            Target: 
              ILocalReferenceOperation: i (OperationKind.LocalReference, Type: System.Int32) (Syntax: 'i')
  Body: 
    IBlockOperation (2 statements, 1 locals) (OperationKind.Block, Type: null) (Syntax: '{ ... }')
      Locals: Local_1: System.Func<System.Int32, System.Int32> lambda
      IVariableDeclarationGroupOperation (1 declarations) (OperationKind.VariableDeclarationGroup, Type: null) (Syntax: 'var lambda  ... .Compile();')
        IVariableDeclarationOperation (1 declarators) (OperationKind.VariableDeclaration, Type: null) (Syntax: 'var lambda = e.Compile()')
          Declarators:
              IVariableDeclaratorOperation (Symbol: System.Func<System.Int32, System.Int32> lambda) (OperationKind.VariableDeclarator, Type: null) (Syntax: 'lambda = e.Compile()')
                Initializer: 
                  IVariableInitializerOperation (OperationKind.VariableInitializer, Type: null) (Syntax: '= e.Compile()')
                    IInvocationOperation ( System.Func<System.Int32, System.Int32> System.Linq.Expressions.Expression<System.Func<System.Int32, System.Int32>>.Compile()) (OperationKind.Invocation, Type: System.Func<System.Int32, System.Int32>) (Syntax: 'e.Compile()')
                      Instance Receiver: 
                        ILocalReferenceOperation: e (OperationKind.LocalReference, Type: System.Linq.Expressions.Expression<System.Func<System.Int32, System.Int32>>) (Syntax: 'e')
                      Arguments(0)
          Initializer: 
            null
      IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'System.Cons ... lambda(i));')
        Expression: 
          IInvocationOperation (void System.Console.WriteLine(System.Int32 value)) (OperationKind.Invocation, Type: System.Void) (Syntax: 'System.Cons ... (lambda(i))')
            Instance Receiver: 
              null
            Arguments(1):
                IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: value) (OperationKind.Argument, Type: null) (Syntax: 'lambda(i)')
                  IInvocationOperation (virtual System.Int32 System.Func<System.Int32, System.Int32>.Invoke(System.Int32 arg)) (OperationKind.Invocation, Type: System.Int32) (Syntax: 'lambda(i)')
                    Instance Receiver: 
                      ILocalReferenceOperation: lambda (OperationKind.LocalReference, Type: System.Func<System.Int32, System.Int32>) (Syntax: 'lambda')
                    Arguments(1):
                        IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: arg) (OperationKind.Argument, Type: null) (Syntax: 'i')
                          ILocalReferenceOperation: i (OperationKind.LocalReference, Type: System.Int32) (Syntax: 'i')
                          InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                          OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                  InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                  OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
";
            VerifyOperationTreeForTest<ForStatementSyntax>(source, expectedOperationTree);
        }

        [CompilerTrait(CompilerFeature.IOperation)]
        [Fact, WorkItem(17602, "https://github.com/dotnet/roslyn/issues/17602")]
        public void IForLoopStatement_ExpressiontreeInIterator()
        {
            string source = @"
class C
{
    static void Main(string[] args)
    {
        System.Linq.Expressions.Expression<System.Func<int, int>> e = x => x % 6;
        /*<bind>*/for (int i = 1; i < 5; e = x => x * x, i = i + 1)
        {
            var lambda = e.Compile();
            System.Console.WriteLine(lambda(i));
        }/*</bind>*/
    }
}

";
            string expectedOperationTree = @"
IForLoopOperation (LoopKind.For) (OperationKind.Loop, Type: null) (Syntax: 'for (int i  ... }')
  Locals: Local_1: System.Int32 i
  Condition: 
    IBinaryOperation (BinaryOperatorKind.LessThan) (OperationKind.BinaryOperator, Type: System.Boolean) (Syntax: 'i < 5')
      Left: 
        ILocalReferenceOperation: i (OperationKind.LocalReference, Type: System.Int32) (Syntax: 'i')
      Right: 
        ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 5) (Syntax: '5')
  Before:
      IVariableDeclarationGroupOperation (1 declarations) (OperationKind.VariableDeclarationGroup, Type: null, IsImplicit) (Syntax: 'int i = 1')
        IVariableDeclarationOperation (1 declarators) (OperationKind.VariableDeclaration, Type: null) (Syntax: 'int i = 1')
          Declarators:
              IVariableDeclaratorOperation (Symbol: System.Int32 i) (OperationKind.VariableDeclarator, Type: null) (Syntax: 'i = 1')
                Initializer: 
                  IVariableInitializerOperation (OperationKind.VariableInitializer, Type: null) (Syntax: '= 1')
                    ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 1) (Syntax: '1')
          Initializer: 
            null
  AtLoopBottom:
      IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null, IsImplicit) (Syntax: 'e = x => x * x')
        Expression: 
          ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Linq.Expressions.Expression<System.Func<System.Int32, System.Int32>>) (Syntax: 'e = x => x * x')
            Left: 
              ILocalReferenceOperation: e (OperationKind.LocalReference, Type: System.Linq.Expressions.Expression<System.Func<System.Int32, System.Int32>>) (Syntax: 'e')
            Right: 
              IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.Linq.Expressions.Expression<System.Func<System.Int32, System.Int32>>, IsImplicit) (Syntax: 'x => x * x')
                Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                Operand: 
                  IAnonymousFunctionOperation (Symbol: lambda expression) (OperationKind.AnonymousFunction, Type: null) (Syntax: 'x => x * x')
                    IBlockOperation (1 statements) (OperationKind.Block, Type: null, IsImplicit) (Syntax: 'x * x')
                      IReturnOperation (OperationKind.Return, Type: null, IsImplicit) (Syntax: 'x * x')
                        ReturnedValue: 
                          IBinaryOperation (BinaryOperatorKind.Multiply) (OperationKind.BinaryOperator, Type: System.Int32) (Syntax: 'x * x')
                            Left: 
                              IParameterReferenceOperation: x (OperationKind.ParameterReference, Type: System.Int32) (Syntax: 'x')
                            Right: 
                              IParameterReferenceOperation: x (OperationKind.ParameterReference, Type: System.Int32) (Syntax: 'x')
      IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null, IsImplicit) (Syntax: 'i = i + 1')
        Expression: 
          ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Int32) (Syntax: 'i = i + 1')
            Left: 
              ILocalReferenceOperation: i (OperationKind.LocalReference, Type: System.Int32) (Syntax: 'i')
            Right: 
              IBinaryOperation (BinaryOperatorKind.Add) (OperationKind.BinaryOperator, Type: System.Int32) (Syntax: 'i + 1')
                Left: 
                  ILocalReferenceOperation: i (OperationKind.LocalReference, Type: System.Int32) (Syntax: 'i')
                Right: 
                  ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 1) (Syntax: '1')
  Body: 
    IBlockOperation (2 statements, 1 locals) (OperationKind.Block, Type: null) (Syntax: '{ ... }')
      Locals: Local_1: System.Func<System.Int32, System.Int32> lambda
      IVariableDeclarationGroupOperation (1 declarations) (OperationKind.VariableDeclarationGroup, Type: null) (Syntax: 'var lambda  ... .Compile();')
        IVariableDeclarationOperation (1 declarators) (OperationKind.VariableDeclaration, Type: null) (Syntax: 'var lambda = e.Compile()')
          Declarators:
              IVariableDeclaratorOperation (Symbol: System.Func<System.Int32, System.Int32> lambda) (OperationKind.VariableDeclarator, Type: null) (Syntax: 'lambda = e.Compile()')
                Initializer: 
                  IVariableInitializerOperation (OperationKind.VariableInitializer, Type: null) (Syntax: '= e.Compile()')
                    IInvocationOperation ( System.Func<System.Int32, System.Int32> System.Linq.Expressions.Expression<System.Func<System.Int32, System.Int32>>.Compile()) (OperationKind.Invocation, Type: System.Func<System.Int32, System.Int32>) (Syntax: 'e.Compile()')
                      Instance Receiver: 
                        ILocalReferenceOperation: e (OperationKind.LocalReference, Type: System.Linq.Expressions.Expression<System.Func<System.Int32, System.Int32>>) (Syntax: 'e')
                      Arguments(0)
          Initializer: 
            null
      IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'System.Cons ... lambda(i));')
        Expression: 
          IInvocationOperation (void System.Console.WriteLine(System.Int32 value)) (OperationKind.Invocation, Type: System.Void) (Syntax: 'System.Cons ... (lambda(i))')
            Instance Receiver: 
              null
            Arguments(1):
                IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: value) (OperationKind.Argument, Type: null) (Syntax: 'lambda(i)')
                  IInvocationOperation (virtual System.Int32 System.Func<System.Int32, System.Int32>.Invoke(System.Int32 arg)) (OperationKind.Invocation, Type: System.Int32) (Syntax: 'lambda(i)')
                    Instance Receiver: 
                      ILocalReferenceOperation: lambda (OperationKind.LocalReference, Type: System.Func<System.Int32, System.Int32>) (Syntax: 'lambda')
                    Arguments(1):
                        IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: arg) (OperationKind.Argument, Type: null) (Syntax: 'i')
                          ILocalReferenceOperation: i (OperationKind.LocalReference, Type: System.Int32) (Syntax: 'i')
                          InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                          OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                  InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                  OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
";
            VerifyOperationTreeForTest<ForStatementSyntax>(source, expectedOperationTree);
        }

        [CompilerTrait(CompilerFeature.IOperation)]
        [Fact, WorkItem(17602, "https://github.com/dotnet/roslyn/issues/17602")]
        public void IForLoopStatement_CustomerTypeInFor()
        {
            string source = @"
class C
{
    static void Main(string[] args)
    {
        /*<bind>*/for (C1 i = new C1(); i == null; i++) { }/*</bind>*/
    }
}
public class C1
{
    public static C1 operator ++(C1 obj)
    {
        return obj;
    }
}
";
            string expectedOperationTree = @"
IForLoopOperation (LoopKind.For) (OperationKind.Loop, Type: null) (Syntax: 'for (C1 i = ... l; i++) { }')
  Locals: Local_1: C1 i
  Condition: 
    IBinaryOperation (BinaryOperatorKind.Equals) (OperationKind.BinaryOperator, Type: System.Boolean) (Syntax: 'i == null')
      Left: 
        IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.Object, IsImplicit) (Syntax: 'i')
          Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: True, IsUserDefined: False) (MethodSymbol: null)
          Operand: 
            ILocalReferenceOperation: i (OperationKind.LocalReference, Type: C1) (Syntax: 'i')
      Right: 
        IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.Object, Constant: null, IsImplicit) (Syntax: 'null')
          Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: True, IsUserDefined: False) (MethodSymbol: null)
          Operand: 
            ILiteralOperation (OperationKind.Literal, Type: null, Constant: null) (Syntax: 'null')
  Before:
      IVariableDeclarationGroupOperation (1 declarations) (OperationKind.VariableDeclarationGroup, Type: null, IsImplicit) (Syntax: 'C1 i = new C1()')
        IVariableDeclarationOperation (1 declarators) (OperationKind.VariableDeclaration, Type: null) (Syntax: 'C1 i = new C1()')
          Declarators:
              IVariableDeclaratorOperation (Symbol: C1 i) (OperationKind.VariableDeclarator, Type: null) (Syntax: 'i = new C1()')
                Initializer: 
                  IVariableInitializerOperation (OperationKind.VariableInitializer, Type: null) (Syntax: '= new C1()')
                    IObjectCreationOperation (Constructor: C1..ctor()) (OperationKind.ObjectCreation, Type: C1) (Syntax: 'new C1()')
                      Arguments(0)
                      Initializer: 
                        null
          Initializer: 
            null
  AtLoopBottom:
      IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null, IsImplicit) (Syntax: 'i++')
        Expression: 
          IIncrementOrDecrementOperation (Postfix) (OperatorMethod: C1 C1.op_Increment(C1 obj)) (OperationKind.Increment, Type: C1) (Syntax: 'i++')
            Target: 
              ILocalReferenceOperation: i (OperationKind.LocalReference, Type: C1) (Syntax: 'i')
  Body: 
    IBlockOperation (0 statements) (OperationKind.Block, Type: null) (Syntax: '{ }')
";
            VerifyOperationTreeForTest<ForStatementSyntax>(source, expectedOperationTree);
        }

        [CompilerTrait(CompilerFeature.IOperation)]
        [Fact, WorkItem(17602, "https://github.com/dotnet/roslyn/issues/17602")]
        public void IForLoopStatement_PostFixIncrementInFor()
        {
            string source = @"
class Program
{
    static void Main(string[] args)
    {
        int i = 0;
        /*<bind>*/for (int j = i++; j < 5; ++j)
        {
            System.Console.WriteLine(j);
        }/*</bind>*/
    }
}
";
            string expectedOperationTree = @"
IForLoopOperation (LoopKind.For) (OperationKind.Loop, Type: null) (Syntax: 'for (int j  ... }')
  Locals: Local_1: System.Int32 j
  Condition: 
    IBinaryOperation (BinaryOperatorKind.LessThan) (OperationKind.BinaryOperator, Type: System.Boolean) (Syntax: 'j < 5')
      Left: 
        ILocalReferenceOperation: j (OperationKind.LocalReference, Type: System.Int32) (Syntax: 'j')
      Right: 
        ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 5) (Syntax: '5')
  Before:
      IVariableDeclarationGroupOperation (1 declarations) (OperationKind.VariableDeclarationGroup, Type: null, IsImplicit) (Syntax: 'int j = i++')
        IVariableDeclarationOperation (1 declarators) (OperationKind.VariableDeclaration, Type: null) (Syntax: 'int j = i++')
          Declarators:
              IVariableDeclaratorOperation (Symbol: System.Int32 j) (OperationKind.VariableDeclarator, Type: null) (Syntax: 'j = i++')
                Initializer: 
                  IVariableInitializerOperation (OperationKind.VariableInitializer, Type: null) (Syntax: '= i++')
                    IIncrementOrDecrementOperation (Postfix) (OperationKind.Increment, Type: System.Int32) (Syntax: 'i++')
                      Target: 
                        ILocalReferenceOperation: i (OperationKind.LocalReference, Type: System.Int32) (Syntax: 'i')
          Initializer: 
            null
  AtLoopBottom:
      IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null, IsImplicit) (Syntax: '++j')
        Expression: 
          IIncrementOrDecrementOperation (Prefix) (OperationKind.Increment, Type: System.Int32) (Syntax: '++j')
            Target: 
              ILocalReferenceOperation: j (OperationKind.LocalReference, Type: System.Int32) (Syntax: 'j')
  Body: 
    IBlockOperation (1 statements) (OperationKind.Block, Type: null) (Syntax: '{ ... }')
      IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'System.Cons ... iteLine(j);')
        Expression: 
          IInvocationOperation (void System.Console.WriteLine(System.Int32 value)) (OperationKind.Invocation, Type: System.Void) (Syntax: 'System.Cons ... riteLine(j)')
            Instance Receiver: 
              null
            Arguments(1):
                IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: value) (OperationKind.Argument, Type: null) (Syntax: 'j')
                  ILocalReferenceOperation: j (OperationKind.LocalReference, Type: System.Int32) (Syntax: 'j')
                  InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                  OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
";
            VerifyOperationTreeForTest<ForStatementSyntax>(source, expectedOperationTree);
        }

        [CompilerTrait(CompilerFeature.IOperation)]
        [Fact, WorkItem(17602, "https://github.com/dotnet/roslyn/issues/17602")]
        public void IForLoopStatement_PreFixIncrementInFor()
        {
            string source = @"
class Program
{
    static void Main(string[] args)
    {
        int i = 0;
        /*<bind>*/for (int j = ++i; j < 5; ++j)
        {
            System.Console.WriteLine(j);
        }/*</bind>*/
    }
}

";
            string expectedOperationTree = @"
IForLoopOperation (LoopKind.For) (OperationKind.Loop, Type: null) (Syntax: 'for (int j  ... }')
  Locals: Local_1: System.Int32 j
  Condition: 
    IBinaryOperation (BinaryOperatorKind.LessThan) (OperationKind.BinaryOperator, Type: System.Boolean) (Syntax: 'j < 5')
      Left: 
        ILocalReferenceOperation: j (OperationKind.LocalReference, Type: System.Int32) (Syntax: 'j')
      Right: 
        ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 5) (Syntax: '5')
  Before:
      IVariableDeclarationGroupOperation (1 declarations) (OperationKind.VariableDeclarationGroup, Type: null, IsImplicit) (Syntax: 'int j = ++i')
        IVariableDeclarationOperation (1 declarators) (OperationKind.VariableDeclaration, Type: null) (Syntax: 'int j = ++i')
          Declarators:
              IVariableDeclaratorOperation (Symbol: System.Int32 j) (OperationKind.VariableDeclarator, Type: null) (Syntax: 'j = ++i')
                Initializer: 
                  IVariableInitializerOperation (OperationKind.VariableInitializer, Type: null) (Syntax: '= ++i')
                    IIncrementOrDecrementOperation (Prefix) (OperationKind.Increment, Type: System.Int32) (Syntax: '++i')
                      Target: 
                        ILocalReferenceOperation: i (OperationKind.LocalReference, Type: System.Int32) (Syntax: 'i')
          Initializer: 
            null
  AtLoopBottom:
      IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null, IsImplicit) (Syntax: '++j')
        Expression: 
          IIncrementOrDecrementOperation (Prefix) (OperationKind.Increment, Type: System.Int32) (Syntax: '++j')
            Target: 
              ILocalReferenceOperation: j (OperationKind.LocalReference, Type: System.Int32) (Syntax: 'j')
  Body: 
    IBlockOperation (1 statements) (OperationKind.Block, Type: null) (Syntax: '{ ... }')
      IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'System.Cons ... iteLine(j);')
        Expression: 
          IInvocationOperation (void System.Console.WriteLine(System.Int32 value)) (OperationKind.Invocation, Type: System.Void) (Syntax: 'System.Cons ... riteLine(j)')
            Instance Receiver: 
              null
            Arguments(1):
                IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: value) (OperationKind.Argument, Type: null) (Syntax: 'j')
                  ILocalReferenceOperation: j (OperationKind.LocalReference, Type: System.Int32) (Syntax: 'j')
                  InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                  OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
";
            VerifyOperationTreeForTest<ForStatementSyntax>(source, expectedOperationTree);
        }

        [CompilerTrait(CompilerFeature.IOperation)]
        [Fact, WorkItem(17602, "https://github.com/dotnet/roslyn/issues/17602")]
        public void IForLoopStatement_PreFixIncrementInCondition()
        {
            string source = @"
class Program
{
    static void Main(string[] args)
    {
        /*<bind>*/for (int i = 0; ++i < 5;)
        {
            System.Console.WriteLine(i);
        }/*</bind>*/
    }
}
";
            string expectedOperationTree = @"
IForLoopOperation (LoopKind.For) (OperationKind.Loop, Type: null) (Syntax: 'for (int i  ... }')
  Locals: Local_1: System.Int32 i
  Condition: 
    IBinaryOperation (BinaryOperatorKind.LessThan) (OperationKind.BinaryOperator, Type: System.Boolean) (Syntax: '++i < 5')
      Left: 
        IIncrementOrDecrementOperation (Prefix) (OperationKind.Increment, Type: System.Int32) (Syntax: '++i')
          Target: 
            ILocalReferenceOperation: i (OperationKind.LocalReference, Type: System.Int32) (Syntax: 'i')
      Right: 
        ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 5) (Syntax: '5')
  Before:
      IVariableDeclarationGroupOperation (1 declarations) (OperationKind.VariableDeclarationGroup, Type: null, IsImplicit) (Syntax: 'int i = 0')
        IVariableDeclarationOperation (1 declarators) (OperationKind.VariableDeclaration, Type: null) (Syntax: 'int i = 0')
          Declarators:
              IVariableDeclaratorOperation (Symbol: System.Int32 i) (OperationKind.VariableDeclarator, Type: null) (Syntax: 'i = 0')
                Initializer: 
                  IVariableInitializerOperation (OperationKind.VariableInitializer, Type: null) (Syntax: '= 0')
                    ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 0) (Syntax: '0')
          Initializer: 
            null
  AtLoopBottom(0)
  Body: 
    IBlockOperation (1 statements) (OperationKind.Block, Type: null) (Syntax: '{ ... }')
      IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'System.Cons ... iteLine(i);')
        Expression: 
          IInvocationOperation (void System.Console.WriteLine(System.Int32 value)) (OperationKind.Invocation, Type: System.Void) (Syntax: 'System.Cons ... riteLine(i)')
            Instance Receiver: 
              null
            Arguments(1):
                IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: value) (OperationKind.Argument, Type: null) (Syntax: 'i')
                  ILocalReferenceOperation: i (OperationKind.LocalReference, Type: System.Int32) (Syntax: 'i')
                  InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                  OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
";
            VerifyOperationTreeForTest<ForStatementSyntax>(source, expectedOperationTree);
        }

        [CompilerTrait(CompilerFeature.IOperation)]
        [Fact, WorkItem(17602, "https://github.com/dotnet/roslyn/issues/17602")]
        public void IForLoopStatement_PostFixDecrementInCondition()
        {
            string source = @"
class Program
{
    static void Main(string[] args)
    {
        /*<bind>*/for (int i = 0; foo(i--) > -5;)
        {
            System.Console.WriteLine(i);
        }/*</bind>*/
    }
    static int foo(int x)
    {
        return x;
    }
}

";
            string expectedOperationTree = @"
IForLoopOperation (LoopKind.For) (OperationKind.Loop, Type: null) (Syntax: 'for (int i  ... }')
  Locals: Local_1: System.Int32 i
  Condition: 
    IBinaryOperation (BinaryOperatorKind.GreaterThan) (OperationKind.BinaryOperator, Type: System.Boolean) (Syntax: 'foo(i--) > -5')
      Left: 
        IInvocationOperation (System.Int32 Program.foo(System.Int32 x)) (OperationKind.Invocation, Type: System.Int32) (Syntax: 'foo(i--)')
          Instance Receiver: 
            null
          Arguments(1):
              IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: x) (OperationKind.Argument, Type: null) (Syntax: 'i--')
                IIncrementOrDecrementOperation (Postfix) (OperationKind.Decrement, Type: System.Int32) (Syntax: 'i--')
                  Target: 
                    ILocalReferenceOperation: i (OperationKind.LocalReference, Type: System.Int32) (Syntax: 'i')
                InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
      Right: 
        IUnaryOperation (UnaryOperatorKind.Minus) (OperationKind.UnaryOperator, Type: System.Int32, Constant: -5) (Syntax: '-5')
          Operand: 
            ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 5) (Syntax: '5')
  Before:
      IVariableDeclarationGroupOperation (1 declarations) (OperationKind.VariableDeclarationGroup, Type: null, IsImplicit) (Syntax: 'int i = 0')
        IVariableDeclarationOperation (1 declarators) (OperationKind.VariableDeclaration, Type: null) (Syntax: 'int i = 0')
          Declarators:
              IVariableDeclaratorOperation (Symbol: System.Int32 i) (OperationKind.VariableDeclarator, Type: null) (Syntax: 'i = 0')
                Initializer: 
                  IVariableInitializerOperation (OperationKind.VariableInitializer, Type: null) (Syntax: '= 0')
                    ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 0) (Syntax: '0')
          Initializer: 
            null
  AtLoopBottom(0)
  Body: 
    IBlockOperation (1 statements) (OperationKind.Block, Type: null) (Syntax: '{ ... }')
      IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'System.Cons ... iteLine(i);')
        Expression: 
          IInvocationOperation (void System.Console.WriteLine(System.Int32 value)) (OperationKind.Invocation, Type: System.Void) (Syntax: 'System.Cons ... riteLine(i)')
            Instance Receiver: 
              null
            Arguments(1):
                IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: value) (OperationKind.Argument, Type: null) (Syntax: 'i')
                  ILocalReferenceOperation: i (OperationKind.LocalReference, Type: System.Int32) (Syntax: 'i')
                  InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                  OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
";
            VerifyOperationTreeForTest<ForStatementSyntax>(source, expectedOperationTree);
        }

        [CompilerTrait(CompilerFeature.IOperation)]
        [Fact, WorkItem(17602, "https://github.com/dotnet/roslyn/issues/17602")]
        public void IForLoopStatement_InfiniteLoopVerify()
        {
            string source = @"
class Program
{
    static void Main(string[] args)
    {
        /*<bind>*/for (; true;)
        {
            System.Console.WriteLine(""z"");
        }/*</bind>*/
    }
}
";
            string expectedOperationTree = @"
IForLoopOperation (LoopKind.For) (OperationKind.Loop, Type: null) (Syntax: 'for (; true ... }')
  Condition: 
    ILiteralOperation (OperationKind.Literal, Type: System.Boolean, Constant: True) (Syntax: 'true')
  Before(0)
  AtLoopBottom(0)
  Body: 
    IBlockOperation (1 statements) (OperationKind.Block, Type: null) (Syntax: '{ ... }')
      IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'System.Cons ... eLine(""z"");')
        Expression: 
          IInvocationOperation (void System.Console.WriteLine(System.String value)) (OperationKind.Invocation, Type: System.Void) (Syntax: 'System.Cons ... teLine(""z"")')
            Instance Receiver: 
              null
            Arguments(1):
                IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: value) (OperationKind.Argument, Type: null) (Syntax: '""z""')
                  ILiteralOperation (OperationKind.Literal, Type: System.String, Constant: ""z"") (Syntax: '""z""')
                  InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                  OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
";
            VerifyOperationTreeForTest<ForStatementSyntax>(source, expectedOperationTree);
        }

        [CompilerTrait(CompilerFeature.IOperation)]
        [Fact, WorkItem(17602, "https://github.com/dotnet/roslyn/issues/17602")]
        public void IForLoopStatement_InvalidExpression()
        {
            string source = @"
class C
{
    static void Main(string[] args)
    {
        /*<bind>*/for (int k = 0, j = 0; k < 100, j > 5;/*</bind>*/ k++)
        {
        }
    }
}
";
            string expectedOperationTree = @"
IForLoopOperation (LoopKind.For) (OperationKind.Loop, Type: null, IsInvalid) (Syntax: 'for (int k  ... 100, j > 5;')
  Locals: Local_1: System.Int32 k
    Local_2: System.Int32 j
  Condition: 
    IBinaryOperation (BinaryOperatorKind.LessThan) (OperationKind.BinaryOperator, Type: System.Boolean, IsInvalid) (Syntax: 'k < 100')
      Left: 
        ILocalReferenceOperation: k (OperationKind.LocalReference, Type: System.Int32) (Syntax: 'k')
      Right: 
        ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 100, IsInvalid) (Syntax: '100')
  Before:
      IVariableDeclarationGroupOperation (1 declarations) (OperationKind.VariableDeclarationGroup, Type: null, IsImplicit) (Syntax: 'int k = 0, j = 0')
        IVariableDeclarationOperation (2 declarators) (OperationKind.VariableDeclaration, Type: null) (Syntax: 'int k = 0, j = 0')
          Declarators:
              IVariableDeclaratorOperation (Symbol: System.Int32 k) (OperationKind.VariableDeclarator, Type: null) (Syntax: 'k = 0')
                Initializer: 
                  IVariableInitializerOperation (OperationKind.VariableInitializer, Type: null) (Syntax: '= 0')
                    ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 0) (Syntax: '0')
              IVariableDeclaratorOperation (Symbol: System.Int32 j) (OperationKind.VariableDeclarator, Type: null) (Syntax: 'j = 0')
                Initializer: 
                  IVariableInitializerOperation (OperationKind.VariableInitializer, Type: null) (Syntax: '= 0')
                    ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 0) (Syntax: '0')
          Initializer: 
            null
  AtLoopBottom:
      IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null, IsInvalid, IsImplicit) (Syntax: '')
        Expression: 
          IInvalidOperation (OperationKind.Invalid, Type: null, IsInvalid) (Syntax: '')
            Children(0)
      IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null, IsInvalid, IsImplicit) (Syntax: 'j > 5')
        Expression: 
          IBinaryOperation (BinaryOperatorKind.GreaterThan) (OperationKind.BinaryOperator, Type: System.Boolean, IsInvalid) (Syntax: 'j > 5')
            Left: 
              ILocalReferenceOperation: j (OperationKind.LocalReference, Type: System.Int32, IsInvalid) (Syntax: 'j')
            Right: 
              ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 5, IsInvalid) (Syntax: '5')
  Body: 
    IEmptyOperation (OperationKind.Empty, Type: null, IsInvalid) (Syntax: ';')
";
            VerifyOperationTreeForTest<ForStatementSyntax>(source, expectedOperationTree);
        }

        [CompilerTrait(CompilerFeature.IOperation)]
        [Fact, WorkItem(17602, "https://github.com/dotnet/roslyn/issues/17602")]
        public void IForLoopStatement_ConditionOutVar()
        {
            string source = @"
class P
{
    private void M()
    {
        var s = """";
        /*<bind>*/for (var j = int.TryParse(s, out var i) ? i : 0; i < 10; i++)
        {
            System.Console.WriteLine($""i={i}, s={s}"");
        }/*</bind>*/
    }
}
";
            string expectedOperationTree = @"
IForLoopOperation (LoopKind.For) (OperationKind.Loop, Type: null) (Syntax: 'for (var j  ... }')
  Locals: Local_1: System.Int32 j
    Local_2: System.Int32 i
  Condition: 
    IBinaryOperation (BinaryOperatorKind.LessThan) (OperationKind.BinaryOperator, Type: System.Boolean) (Syntax: 'i < 10')
      Left: 
        ILocalReferenceOperation: i (OperationKind.LocalReference, Type: System.Int32) (Syntax: 'i')
      Right: 
        ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 10) (Syntax: '10')
  Before:
      IVariableDeclarationGroupOperation (1 declarations) (OperationKind.VariableDeclarationGroup, Type: null, IsImplicit) (Syntax: 'var j = int ...  i) ? i : 0')
        IVariableDeclarationOperation (1 declarators) (OperationKind.VariableDeclaration, Type: null) (Syntax: 'var j = int ...  i) ? i : 0')
          Declarators:
              IVariableDeclaratorOperation (Symbol: System.Int32 j) (OperationKind.VariableDeclarator, Type: null) (Syntax: 'j = int.Try ...  i) ? i : 0')
                Initializer: 
                  IVariableInitializerOperation (OperationKind.VariableInitializer, Type: null) (Syntax: '= int.TryPa ...  i) ? i : 0')
                    IConditionalOperation (OperationKind.Conditional, Type: System.Int32) (Syntax: 'int.TryPars ...  i) ? i : 0')
                      Condition: 
                        IInvocationOperation (System.Boolean System.Int32.TryParse(System.String s, out System.Int32 result)) (OperationKind.Invocation, Type: System.Boolean) (Syntax: 'int.TryPars ...  out var i)')
                          Instance Receiver: 
                            null
                          Arguments(2):
                              IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: s) (OperationKind.Argument, Type: null) (Syntax: 's')
                                ILocalReferenceOperation: s (OperationKind.LocalReference, Type: System.String) (Syntax: 's')
                                InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                                OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                              IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: result) (OperationKind.Argument, Type: null) (Syntax: 'out var i')
                                IDeclarationExpressionOperation (OperationKind.DeclarationExpression, Type: System.Int32) (Syntax: 'var i')
                                  ILocalReferenceOperation: i (IsDeclaration: True) (OperationKind.LocalReference, Type: System.Int32) (Syntax: 'i')
                                InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                                OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                      WhenTrue: 
                        ILocalReferenceOperation: i (OperationKind.LocalReference, Type: System.Int32) (Syntax: 'i')
                      WhenFalse: 
                        ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 0) (Syntax: '0')
          Initializer: 
            null
  AtLoopBottom:
      IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null, IsImplicit) (Syntax: 'i++')
        Expression: 
          IIncrementOrDecrementOperation (Postfix) (OperationKind.Increment, Type: System.Int32) (Syntax: 'i++')
            Target: 
              ILocalReferenceOperation: i (OperationKind.LocalReference, Type: System.Int32) (Syntax: 'i')
  Body: 
    IBlockOperation (1 statements) (OperationKind.Block, Type: null) (Syntax: '{ ... }')
      IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'System.Cons ... }, s={s}"");')
        Expression: 
          IInvocationOperation (void System.Console.WriteLine(System.String value)) (OperationKind.Invocation, Type: System.Void) (Syntax: 'System.Cons ... i}, s={s}"")')
            Instance Receiver: 
              null
            Arguments(1):
                IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: value) (OperationKind.Argument, Type: null) (Syntax: '$""i={i}, s={s}""')
                  IInterpolatedStringOperation (OperationKind.InterpolatedString, Type: System.String) (Syntax: '$""i={i}, s={s}""')
                    Parts(4):
                        IInterpolatedStringTextOperation (OperationKind.InterpolatedStringText, Type: null) (Syntax: 'i=')
                          Text: 
                            ILiteralOperation (OperationKind.Literal, Type: System.String, Constant: ""i="", IsImplicit) (Syntax: 'i=')
                        IInterpolationOperation (OperationKind.Interpolation, Type: null) (Syntax: '{i}')
                          Expression: 
                            ILocalReferenceOperation: i (OperationKind.LocalReference, Type: System.Int32) (Syntax: 'i')
                          Alignment: 
                            null
                          FormatString: 
                            null
                        IInterpolatedStringTextOperation (OperationKind.InterpolatedStringText, Type: null) (Syntax: ', s=')
                          Text: 
                            ILiteralOperation (OperationKind.Literal, Type: System.String, Constant: "", s="", IsImplicit) (Syntax: ', s=')
                        IInterpolationOperation (OperationKind.Interpolation, Type: null) (Syntax: '{s}')
                          Expression: 
                            ILocalReferenceOperation: s (OperationKind.LocalReference, Type: System.String) (Syntax: 's')
                          Alignment: 
                            null
                          FormatString: 
                            null
                  InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                  OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
";
            VerifyOperationTreeForTest<ForStatementSyntax>(source, expectedOperationTree);
        }

        [CompilerTrait(CompilerFeature.IOperation)]
        [Fact]
        public void IForLoopStatement_InvalidIterationVariableDeclaration()
        {
            string source = @"
class Program
{
    static void Main(string[] args)
    {
        int i = 0;
        /*<bind>*/for (int i = 0; true;)
        {
            System.Console.WriteLine(i);
        }/*</bind>*/
    }
}
";
            string expectedOperationTree = @"
IForLoopOperation (LoopKind.For) (OperationKind.Loop, Type: null, IsInvalid) (Syntax: 'for (int i  ... }')
  Locals: Local_1: System.Int32 i
  Condition: 
    ILiteralOperation (OperationKind.Literal, Type: System.Boolean, Constant: True) (Syntax: 'true')
  Before:
      IVariableDeclarationGroupOperation (1 declarations) (OperationKind.VariableDeclarationGroup, Type: null, IsInvalid, IsImplicit) (Syntax: 'int i = 0')
        IVariableDeclarationOperation (1 declarators) (OperationKind.VariableDeclaration, Type: null, IsInvalid) (Syntax: 'int i = 0')
          Declarators:
              IVariableDeclaratorOperation (Symbol: System.Int32 i) (OperationKind.VariableDeclarator, Type: null, IsInvalid) (Syntax: 'i = 0')
                Initializer: 
                  IVariableInitializerOperation (OperationKind.VariableInitializer, Type: null) (Syntax: '= 0')
                    ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 0) (Syntax: '0')
          Initializer: 
            null
  AtLoopBottom(0)
  Body: 
    IBlockOperation (1 statements) (OperationKind.Block, Type: null) (Syntax: '{ ... }')
      IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'System.Cons ... iteLine(i);')
        Expression: 
          IInvocationOperation (void System.Console.WriteLine(System.Int32 value)) (OperationKind.Invocation, Type: System.Void) (Syntax: 'System.Cons ... riteLine(i)')
            Instance Receiver: 
              null
            Arguments(1):
                IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: value) (OperationKind.Argument, Type: null) (Syntax: 'i')
                  ILocalReferenceOperation: i (OperationKind.LocalReference, Type: System.Int32) (Syntax: 'i')
                  InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                  OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
";
            VerifyOperationTreeForTest<ForStatementSyntax>(source, expectedOperationTree);
        }
    }
}
