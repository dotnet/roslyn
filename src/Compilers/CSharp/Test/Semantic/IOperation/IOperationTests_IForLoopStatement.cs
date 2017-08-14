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
IForLoopStatement (LoopKind.For) (OperationKind.LoopStatement) (Syntax: 'for (int i  ... }')
  Condition: IBinaryOperatorExpression (BinaryOperationKind.IntegerLessThan) (OperationKind.BinaryOperatorExpression, Type: System.Boolean) (Syntax: 'i < 3')
      Left: ILocalReferenceExpression: i (OperationKind.LocalReferenceExpression, Type: System.Int32) (Syntax: 'i')
      Right: ILiteralExpression (Text: 3) (OperationKind.LiteralExpression, Type: System.Int32, Constant: 3) (Syntax: '3')
  Locals: Local_1: System.Int32 i
  Before:
      IVariableDeclarationStatement (1 declarations) (OperationKind.VariableDeclarationStatement) (Syntax: 'i = 0')
        IVariableDeclaration (1 variables) (OperationKind.VariableDeclaration) (Syntax: 'i = 0')
          Variables: Local_1: System.Int32 i
          Initializer: ILiteralExpression (Text: 0) (OperationKind.LiteralExpression, Type: System.Int32, Constant: 0) (Syntax: '0')
  AtLoopBottom:
      IExpressionStatement (OperationKind.ExpressionStatement) (Syntax: 'i = i + 1')
        Expression: ISimpleAssignmentExpression (OperationKind.SimpleAssignmentExpression, Type: System.Int32) (Syntax: 'i = i + 1')
            Left: ILocalReferenceExpression: i (OperationKind.LocalReferenceExpression, Type: System.Int32) (Syntax: 'i')
            Right: IBinaryOperatorExpression (BinaryOperationKind.IntegerAdd) (OperationKind.BinaryOperatorExpression, Type: System.Int32) (Syntax: 'i + 1')
                Left: ILocalReferenceExpression: i (OperationKind.LocalReferenceExpression, Type: System.Int32) (Syntax: 'i')
                Right: ILiteralExpression (Text: 1) (OperationKind.LiteralExpression, Type: System.Int32, Constant: 1) (Syntax: '1')
  Body: IBlockStatement (1 statements) (OperationKind.BlockStatement) (Syntax: '{ ... }')
      IExpressionStatement (OperationKind.ExpressionStatement) (Syntax: 'x = x * 3;')
        Expression: ISimpleAssignmentExpression (OperationKind.SimpleAssignmentExpression, Type: System.Int32) (Syntax: 'x = x * 3')
            Left: ILocalReferenceExpression: x (OperationKind.LocalReferenceExpression, Type: System.Int32) (Syntax: 'x')
            Right: IBinaryOperatorExpression (BinaryOperationKind.IntegerMultiply) (OperationKind.BinaryOperatorExpression, Type: System.Int32) (Syntax: 'x * 3')
                Left: ILocalReferenceExpression: x (OperationKind.LocalReferenceExpression, Type: System.Int32) (Syntax: 'x')
                Right: ILiteralExpression (Text: 3) (OperationKind.LiteralExpression, Type: System.Int32, Constant: 3) (Syntax: '3')
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
IForLoopStatement (LoopKind.For) (OperationKind.LoopStatement) (Syntax: 'for (j = 0; ... }')
  Condition: ILiteralExpression (OperationKind.LiteralExpression, Type: System.Boolean, Constant: True) (Syntax: 'true')
  Before:
      IExpressionStatement (OperationKind.ExpressionStatement) (Syntax: 'j = 0')
        Expression: ISimpleAssignmentExpression (OperationKind.SimpleAssignmentExpression, Type: System.Int32) (Syntax: 'j = 0')
            Left: ILocalReferenceExpression: j (OperationKind.LocalReferenceExpression, Type: System.Int32) (Syntax: 'j')
            Right: ILiteralExpression (Text: 0) (OperationKind.LiteralExpression, Type: System.Int32, Constant: 0) (Syntax: '0')
  AtLoopBottom:
      IExpressionStatement (OperationKind.ExpressionStatement) (Syntax: 'j = j + 1')
        Expression: ISimpleAssignmentExpression (OperationKind.SimpleAssignmentExpression, Type: System.Int32) (Syntax: 'j = j + 1')
            Left: ILocalReferenceExpression: j (OperationKind.LocalReferenceExpression, Type: System.Int32) (Syntax: 'j')
            Right: IBinaryOperatorExpression (BinaryOperationKind.IntegerAdd) (OperationKind.BinaryOperatorExpression, Type: System.Int32) (Syntax: 'j + 1')
                Left: ILocalReferenceExpression: j (OperationKind.LocalReferenceExpression, Type: System.Int32) (Syntax: 'j')
                Right: ILiteralExpression (Text: 1) (OperationKind.LiteralExpression, Type: System.Int32, Constant: 1) (Syntax: '1')
  Body: IBlockStatement (2 statements) (OperationKind.BlockStatement) (Syntax: '{ ... }')
      IExpressionStatement (OperationKind.ExpressionStatement) (Syntax: 'i = i + 1;')
        Expression: ISimpleAssignmentExpression (OperationKind.SimpleAssignmentExpression, Type: System.Int32) (Syntax: 'i = i + 1')
            Left: ILocalReferenceExpression: i (OperationKind.LocalReferenceExpression, Type: System.Int32) (Syntax: 'i')
            Right: IBinaryOperatorExpression (BinaryOperationKind.IntegerAdd) (OperationKind.BinaryOperatorExpression, Type: System.Int32) (Syntax: 'i + 1')
                Left: ILocalReferenceExpression: i (OperationKind.LocalReferenceExpression, Type: System.Int32) (Syntax: 'i')
                Right: ILiteralExpression (Text: 1) (OperationKind.LiteralExpression, Type: System.Int32, Constant: 1) (Syntax: '1')
      IBranchStatement (BranchKind.Break) (OperationKind.BranchStatement) (Syntax: 'break;')
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
IForLoopStatement (LoopKind.For) (OperationKind.LoopStatement) (Syntax: 'for (j = 0; ... }')
  Condition: ILiteralExpression (OperationKind.LiteralExpression, Type: System.Boolean, Constant: False) (Syntax: 'false')
  Before:
      IExpressionStatement (OperationKind.ExpressionStatement) (Syntax: 'j = 0')
        Expression: ISimpleAssignmentExpression (OperationKind.SimpleAssignmentExpression, Type: System.Int32) (Syntax: 'j = 0')
            Left: ILocalReferenceExpression: j (OperationKind.LocalReferenceExpression, Type: System.Int32) (Syntax: 'j')
            Right: ILiteralExpression (Text: 0) (OperationKind.LiteralExpression, Type: System.Int32, Constant: 0) (Syntax: '0')
  AtLoopBottom:
      IExpressionStatement (OperationKind.ExpressionStatement) (Syntax: 'j = j + 1')
        Expression: ISimpleAssignmentExpression (OperationKind.SimpleAssignmentExpression, Type: System.Int32) (Syntax: 'j = j + 1')
            Left: ILocalReferenceExpression: j (OperationKind.LocalReferenceExpression, Type: System.Int32) (Syntax: 'j')
            Right: IBinaryOperatorExpression (BinaryOperationKind.IntegerAdd) (OperationKind.BinaryOperatorExpression, Type: System.Int32) (Syntax: 'j + 1')
                Left: ILocalReferenceExpression: j (OperationKind.LocalReferenceExpression, Type: System.Int32) (Syntax: 'j')
                Right: ILiteralExpression (Text: 1) (OperationKind.LiteralExpression, Type: System.Int32, Constant: 1) (Syntax: '1')
  Body: IBlockStatement (2 statements) (OperationKind.BlockStatement) (Syntax: '{ ... }')
      IExpressionStatement (OperationKind.ExpressionStatement) (Syntax: 'i = i + 1;')
        Expression: ISimpleAssignmentExpression (OperationKind.SimpleAssignmentExpression, Type: System.Int32) (Syntax: 'i = i + 1')
            Left: ILocalReferenceExpression: i (OperationKind.LocalReferenceExpression, Type: System.Int32) (Syntax: 'i')
            Right: IBinaryOperatorExpression (BinaryOperationKind.IntegerAdd) (OperationKind.BinaryOperatorExpression, Type: System.Int32) (Syntax: 'i + 1')
                Left: ILocalReferenceExpression: i (OperationKind.LocalReferenceExpression, Type: System.Int32) (Syntax: 'i')
                Right: ILiteralExpression (Text: 1) (OperationKind.LiteralExpression, Type: System.Int32, Constant: 1) (Syntax: '1')
      IBranchStatement (BranchKind.Break) (OperationKind.BranchStatement) (Syntax: 'break;')
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
IForLoopStatement (LoopKind.For) (OperationKind.LoopStatement) (Syntax: 'for (i = 0, ... }')
  Condition: IBinaryOperatorExpression (BinaryOperationKind.IntegerLessThan) (OperationKind.BinaryOperatorExpression, Type: System.Boolean) (Syntax: 'i < 5')
      Left: ILocalReferenceExpression: i (OperationKind.LocalReferenceExpression, Type: System.Int32) (Syntax: 'i')
      Right: ILiteralExpression (Text: 5) (OperationKind.LiteralExpression, Type: System.Int32, Constant: 5) (Syntax: '5')
  Before:
      IExpressionStatement (OperationKind.ExpressionStatement) (Syntax: 'i = 0')
        Expression: ISimpleAssignmentExpression (OperationKind.SimpleAssignmentExpression, Type: System.Int32) (Syntax: 'i = 0')
            Left: ILocalReferenceExpression: i (OperationKind.LocalReferenceExpression, Type: System.Int32) (Syntax: 'i')
            Right: ILiteralExpression (Text: 0) (OperationKind.LiteralExpression, Type: System.Int32, Constant: 0) (Syntax: '0')
      IExpressionStatement (OperationKind.ExpressionStatement) (Syntax: 'j = 0')
        Expression: ISimpleAssignmentExpression (OperationKind.SimpleAssignmentExpression, Type: System.Int32) (Syntax: 'j = 0')
            Left: ILocalReferenceExpression: j (OperationKind.LocalReferenceExpression, Type: System.Int32) (Syntax: 'j')
            Right: ILiteralExpression (Text: 0) (OperationKind.LiteralExpression, Type: System.Int32, Constant: 0) (Syntax: '0')
  AtLoopBottom:
      IExpressionStatement (OperationKind.ExpressionStatement) (Syntax: 'i = i + 1')
        Expression: ISimpleAssignmentExpression (OperationKind.SimpleAssignmentExpression, Type: System.Int32) (Syntax: 'i = i + 1')
            Left: ILocalReferenceExpression: i (OperationKind.LocalReferenceExpression, Type: System.Int32) (Syntax: 'i')
            Right: IBinaryOperatorExpression (BinaryOperationKind.IntegerAdd) (OperationKind.BinaryOperatorExpression, Type: System.Int32) (Syntax: 'i + 1')
                Left: ILocalReferenceExpression: i (OperationKind.LocalReferenceExpression, Type: System.Int32) (Syntax: 'i')
                Right: ILiteralExpression (Text: 1) (OperationKind.LiteralExpression, Type: System.Int32, Constant: 1) (Syntax: '1')
  Body: IBlockStatement (2 statements) (OperationKind.BlockStatement) (Syntax: '{ ... }')
      IIfStatement (OperationKind.IfStatement) (Syntax: 'if (i > 2) continue;')
        Condition: IBinaryOperatorExpression (BinaryOperationKind.IntegerGreaterThan) (OperationKind.BinaryOperatorExpression, Type: System.Boolean) (Syntax: 'i > 2')
            Left: ILocalReferenceExpression: i (OperationKind.LocalReferenceExpression, Type: System.Int32) (Syntax: 'i')
            Right: ILiteralExpression (Text: 2) (OperationKind.LiteralExpression, Type: System.Int32, Constant: 2) (Syntax: '2')
        IfTrue: IBranchStatement (BranchKind.Continue) (OperationKind.BranchStatement) (Syntax: 'continue;')
        IfFalse: null
      IExpressionStatement (OperationKind.ExpressionStatement) (Syntax: 'j = j + 1;')
        Expression: ISimpleAssignmentExpression (OperationKind.SimpleAssignmentExpression, Type: System.Int32) (Syntax: 'j = j + 1')
            Left: ILocalReferenceExpression: j (OperationKind.LocalReferenceExpression, Type: System.Int32) (Syntax: 'j')
            Right: IBinaryOperatorExpression (BinaryOperationKind.IntegerAdd) (OperationKind.BinaryOperatorExpression, Type: System.Int32) (Syntax: 'j + 1')
                Left: ILocalReferenceExpression: j (OperationKind.LocalReferenceExpression, Type: System.Int32) (Syntax: 'j')
                Right: ILiteralExpression (Text: 1) (OperationKind.LiteralExpression, Type: System.Int32, Constant: 1) (Syntax: '1')
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
IForLoopStatement (LoopKind.For) (OperationKind.LoopStatement) (Syntax: 'for (i = 0, ... }')
  Condition: IBinaryOperatorExpression (BinaryOperationKind.IntegerLessThan) (OperationKind.BinaryOperatorExpression, Type: System.Boolean) (Syntax: 'i < 5')
      Left: ILocalReferenceExpression: i (OperationKind.LocalReferenceExpression, Type: System.Int32) (Syntax: 'i')
      Right: ILiteralExpression (Text: 5) (OperationKind.LiteralExpression, Type: System.Int32, Constant: 5) (Syntax: '5')
  Before:
      IExpressionStatement (OperationKind.ExpressionStatement) (Syntax: 'i = 0')
        Expression: ISimpleAssignmentExpression (OperationKind.SimpleAssignmentExpression, Type: System.Int32) (Syntax: 'i = 0')
            Left: ILocalReferenceExpression: i (OperationKind.LocalReferenceExpression, Type: System.Int32) (Syntax: 'i')
            Right: ILiteralExpression (Text: 0) (OperationKind.LiteralExpression, Type: System.Int32, Constant: 0) (Syntax: '0')
      IExpressionStatement (OperationKind.ExpressionStatement) (Syntax: 'j = 0')
        Expression: ISimpleAssignmentExpression (OperationKind.SimpleAssignmentExpression, Type: System.Int32) (Syntax: 'j = 0')
            Left: ILocalReferenceExpression: j (OperationKind.LocalReferenceExpression, Type: System.Int32) (Syntax: 'j')
            Right: ILiteralExpression (Text: 0) (OperationKind.LiteralExpression, Type: System.Int32, Constant: 0) (Syntax: '0')
  AtLoopBottom:
      IExpressionStatement (OperationKind.ExpressionStatement) (Syntax: 'i = i + 1')
        Expression: ISimpleAssignmentExpression (OperationKind.SimpleAssignmentExpression, Type: System.Int32) (Syntax: 'i = i + 1')
            Left: ILocalReferenceExpression: i (OperationKind.LocalReferenceExpression, Type: System.Int32) (Syntax: 'i')
            Right: IBinaryOperatorExpression (BinaryOperationKind.IntegerAdd) (OperationKind.BinaryOperatorExpression, Type: System.Int32) (Syntax: 'i + 1')
                Left: ILocalReferenceExpression: i (OperationKind.LocalReferenceExpression, Type: System.Int32) (Syntax: 'i')
                Right: ILiteralExpression (Text: 1) (OperationKind.LiteralExpression, Type: System.Int32, Constant: 1) (Syntax: '1')
  Body: IBlockStatement (2 statements) (OperationKind.BlockStatement) (Syntax: '{ ... }')
      IIfStatement (OperationKind.IfStatement) (Syntax: 'if (i > 3) break;')
        Condition: IBinaryOperatorExpression (BinaryOperationKind.IntegerGreaterThan) (OperationKind.BinaryOperatorExpression, Type: System.Boolean) (Syntax: 'i > 3')
            Left: ILocalReferenceExpression: i (OperationKind.LocalReferenceExpression, Type: System.Int32) (Syntax: 'i')
            Right: ILiteralExpression (Text: 3) (OperationKind.LiteralExpression, Type: System.Int32, Constant: 3) (Syntax: '3')
        IfTrue: IBranchStatement (BranchKind.Break) (OperationKind.BranchStatement) (Syntax: 'break;')
        IfFalse: null
      IExpressionStatement (OperationKind.ExpressionStatement) (Syntax: 'j = j + 1;')
        Expression: ISimpleAssignmentExpression (OperationKind.SimpleAssignmentExpression, Type: System.Int32) (Syntax: 'j = j + 1')
            Left: ILocalReferenceExpression: j (OperationKind.LocalReferenceExpression, Type: System.Int32) (Syntax: 'j')
            Right: IBinaryOperatorExpression (BinaryOperationKind.IntegerAdd) (OperationKind.BinaryOperatorExpression, Type: System.Int32) (Syntax: 'j + 1')
                Left: ILocalReferenceExpression: j (OperationKind.LocalReferenceExpression, Type: System.Int32) (Syntax: 'j')
                Right: ILiteralExpression (Text: 1) (OperationKind.LiteralExpression, Type: System.Int32, Constant: 1) (Syntax: '1')
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
IForLoopStatement (LoopKind.For) (OperationKind.LoopStatement) (Syntax: 'for (;;) ... }')
  Condition: null
  Before(0)
  AtLoopBottom(0)
  Body: IBlockStatement (2 statements) (OperationKind.BlockStatement) (Syntax: '{ ... }')
      IIfStatement (OperationKind.IfStatement) (Syntax: 'if (i > 4) break;')
        Condition: IBinaryOperatorExpression (BinaryOperationKind.IntegerGreaterThan) (OperationKind.BinaryOperatorExpression, Type: System.Boolean) (Syntax: 'i > 4')
            Left: ILocalReferenceExpression: i (OperationKind.LocalReferenceExpression, Type: System.Int32) (Syntax: 'i')
            Right: ILiteralExpression (Text: 4) (OperationKind.LiteralExpression, Type: System.Int32, Constant: 4) (Syntax: '4')
        IfTrue: IBranchStatement (BranchKind.Break) (OperationKind.BranchStatement) (Syntax: 'break;')
        IfFalse: null
      IExpressionStatement (OperationKind.ExpressionStatement) (Syntax: 'i = i + 2;')
        Expression: ISimpleAssignmentExpression (OperationKind.SimpleAssignmentExpression, Type: System.Int32) (Syntax: 'i = i + 2')
            Left: ILocalReferenceExpression: i (OperationKind.LocalReferenceExpression, Type: System.Int32) (Syntax: 'i')
            Right: IBinaryOperatorExpression (BinaryOperationKind.IntegerAdd) (OperationKind.BinaryOperatorExpression, Type: System.Int32) (Syntax: 'i + 2')
                Left: ILocalReferenceExpression: i (OperationKind.LocalReferenceExpression, Type: System.Int32) (Syntax: 'i')
                Right: ILiteralExpression (Text: 2) (OperationKind.LiteralExpression, Type: System.Int32, Constant: 2) (Syntax: '2')
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
IForLoopStatement (LoopKind.For) (OperationKind.LoopStatement) (Syntax: 'for (i = i  ... }')
  Condition: IBinaryOperatorExpression (BinaryOperationKind.IntegerLessThan) (OperationKind.BinaryOperatorExpression, Type: System.Boolean) (Syntax: 'j < 2')
      Left: ILocalReferenceExpression: j (OperationKind.LocalReferenceExpression, Type: System.Int32) (Syntax: 'j')
      Right: ILiteralExpression (Text: 2) (OperationKind.LiteralExpression, Type: System.Int32, Constant: 2) (Syntax: '2')
  Before:
      IExpressionStatement (OperationKind.ExpressionStatement) (Syntax: 'i = i + 1')
        Expression: ISimpleAssignmentExpression (OperationKind.SimpleAssignmentExpression, Type: System.Int32) (Syntax: 'i = i + 1')
            Left: ILocalReferenceExpression: i (OperationKind.LocalReferenceExpression, Type: System.Int32) (Syntax: 'i')
            Right: IBinaryOperatorExpression (BinaryOperationKind.IntegerAdd) (OperationKind.BinaryOperatorExpression, Type: System.Int32) (Syntax: 'i + 1')
                Left: ILocalReferenceExpression: i (OperationKind.LocalReferenceExpression, Type: System.Int32) (Syntax: 'i')
                Right: ILiteralExpression (Text: 1) (OperationKind.LiteralExpression, Type: System.Int32, Constant: 1) (Syntax: '1')
      IExpressionStatement (OperationKind.ExpressionStatement) (Syntax: 'i = i + 1')
        Expression: ISimpleAssignmentExpression (OperationKind.SimpleAssignmentExpression, Type: System.Int32) (Syntax: 'i = i + 1')
            Left: ILocalReferenceExpression: i (OperationKind.LocalReferenceExpression, Type: System.Int32) (Syntax: 'i')
            Right: IBinaryOperatorExpression (BinaryOperationKind.IntegerAdd) (OperationKind.BinaryOperatorExpression, Type: System.Int32) (Syntax: 'i + 1')
                Left: ILocalReferenceExpression: i (OperationKind.LocalReferenceExpression, Type: System.Int32) (Syntax: 'i')
                Right: ILiteralExpression (Text: 1) (OperationKind.LiteralExpression, Type: System.Int32, Constant: 1) (Syntax: '1')
  AtLoopBottom:
      IExpressionStatement (OperationKind.ExpressionStatement) (Syntax: 'i = i + 2')
        Expression: ISimpleAssignmentExpression (OperationKind.SimpleAssignmentExpression, Type: System.Int32) (Syntax: 'i = i + 2')
            Left: ILocalReferenceExpression: i (OperationKind.LocalReferenceExpression, Type: System.Int32) (Syntax: 'i')
            Right: IBinaryOperatorExpression (BinaryOperationKind.IntegerAdd) (OperationKind.BinaryOperatorExpression, Type: System.Int32) (Syntax: 'i + 2')
                Left: ILocalReferenceExpression: i (OperationKind.LocalReferenceExpression, Type: System.Int32) (Syntax: 'i')
                Right: ILiteralExpression (Text: 2) (OperationKind.LiteralExpression, Type: System.Int32, Constant: 2) (Syntax: '2')
      IExpressionStatement (OperationKind.ExpressionStatement) (Syntax: 'j = j + 1')
        Expression: ISimpleAssignmentExpression (OperationKind.SimpleAssignmentExpression, Type: System.Int32) (Syntax: 'j = j + 1')
            Left: ILocalReferenceExpression: j (OperationKind.LocalReferenceExpression, Type: System.Int32) (Syntax: 'j')
            Right: IBinaryOperatorExpression (BinaryOperationKind.IntegerAdd) (OperationKind.BinaryOperatorExpression, Type: System.Int32) (Syntax: 'j + 1')
                Left: ILocalReferenceExpression: j (OperationKind.LocalReferenceExpression, Type: System.Int32) (Syntax: 'j')
                Right: ILiteralExpression (Text: 1) (OperationKind.LiteralExpression, Type: System.Int32, Constant: 1) (Syntax: '1')
  Body: IBlockStatement (0 statements) (OperationKind.BlockStatement) (Syntax: '{ ... }')
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
IForLoopStatement (LoopKind.For) (OperationKind.LoopStatement) (Syntax: 'for (; i <  ... }')
  Condition: IBinaryOperatorExpression (BinaryOperationKind.IntegerLessThan) (OperationKind.BinaryOperatorExpression, Type: System.Boolean) (Syntax: 'i < 10')
      Left: ILocalReferenceExpression: i (OperationKind.LocalReferenceExpression, Type: System.Int32) (Syntax: 'i')
      Right: ILiteralExpression (Text: 10) (OperationKind.LiteralExpression, Type: System.Int32, Constant: 10) (Syntax: '10')
  Before(0)
  AtLoopBottom:
      IExpressionStatement (OperationKind.ExpressionStatement) (Syntax: 'i = i + 1')
        Expression: ISimpleAssignmentExpression (OperationKind.SimpleAssignmentExpression, Type: System.Int32) (Syntax: 'i = i + 1')
            Left: ILocalReferenceExpression: i (OperationKind.LocalReferenceExpression, Type: System.Int32) (Syntax: 'i')
            Right: IBinaryOperatorExpression (BinaryOperationKind.IntegerAdd) (OperationKind.BinaryOperatorExpression, Type: System.Int32) (Syntax: 'i + 1')
                Left: ILocalReferenceExpression: i (OperationKind.LocalReferenceExpression, Type: System.Int32) (Syntax: 'i')
                Right: ILiteralExpression (Text: 1) (OperationKind.LiteralExpression, Type: System.Int32, Constant: 1) (Syntax: '1')
  Body: IBlockStatement (0 statements) (OperationKind.BlockStatement) (Syntax: '{ ... }')
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
IForLoopStatement (LoopKind.For) (OperationKind.LoopStatement) (Syntax: 'for (int k  ... }')
  Condition: IBinaryOperatorExpression (BinaryOperationKind.IntegerGreaterThan) (OperationKind.BinaryOperatorExpression, Type: System.Boolean) (Syntax: 'k > 100')
      Left: ILocalReferenceExpression: k (OperationKind.LocalReferenceExpression, Type: System.Int32) (Syntax: 'k')
      Right: ILiteralExpression (Text: 100) (OperationKind.LiteralExpression, Type: System.Int32, Constant: 100) (Syntax: '100')
  Locals: Local_1: System.Int32 k
  Before:
      IVariableDeclarationStatement (1 declarations) (OperationKind.VariableDeclarationStatement) (Syntax: 'k = 200')
        IVariableDeclaration (1 variables) (OperationKind.VariableDeclaration) (Syntax: 'k = 200')
          Variables: Local_1: System.Int32 k
          Initializer: ILiteralExpression (Text: 200) (OperationKind.LiteralExpression, Type: System.Int32, Constant: 200) (Syntax: '200')
  AtLoopBottom:
      IExpressionStatement (OperationKind.ExpressionStatement) (Syntax: 'k = k - 1')
        Expression: ISimpleAssignmentExpression (OperationKind.SimpleAssignmentExpression, Type: System.Int32) (Syntax: 'k = k - 1')
            Left: ILocalReferenceExpression: k (OperationKind.LocalReferenceExpression, Type: System.Int32) (Syntax: 'k')
            Right: IBinaryOperatorExpression (BinaryOperationKind.IntegerSubtract) (OperationKind.BinaryOperatorExpression, Type: System.Int32) (Syntax: 'k - 1')
                Left: ILocalReferenceExpression: k (OperationKind.LocalReferenceExpression, Type: System.Int32) (Syntax: 'k')
                Right: ILiteralExpression (Text: 1) (OperationKind.LiteralExpression, Type: System.Int32, Constant: 1) (Syntax: '1')
  Body: IBlockStatement (0 statements) (OperationKind.BlockStatement) (Syntax: '{ ... }')
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
IForLoopStatement (LoopKind.For) (OperationKind.LoopStatement) (Syntax: 'for (Initia ... }')
  Condition: IInvocationExpression (System.Boolean C.Conditional()) (OperationKind.InvocationExpression, Type: System.Boolean) (Syntax: 'Conditional()')
      Instance Receiver: null
      Arguments(0)
  Before:
      IExpressionStatement (OperationKind.ExpressionStatement) (Syntax: 'Initializer()')
        Expression: IInvocationExpression (System.Int32 C.Initializer()) (OperationKind.InvocationExpression, Type: System.Int32) (Syntax: 'Initializer()')
            Instance Receiver: null
            Arguments(0)
  AtLoopBottom:
      IExpressionStatement (OperationKind.ExpressionStatement) (Syntax: 'Iterator()')
        Expression: IInvocationExpression (System.Int32 C.Iterator()) (OperationKind.InvocationExpression, Type: System.Int32) (Syntax: 'Iterator()')
            Instance Receiver: null
            Arguments(0)
  Body: IBlockStatement (0 statements) (OperationKind.BlockStatement) (Syntax: '{ ... }')
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
IForLoopStatement (LoopKind.For) (OperationKind.LoopStatement) (Syntax: 'for (int i  ...  = i + 1) ;')
  Condition: IBinaryOperatorExpression (BinaryOperationKind.IntegerLessThan) (OperationKind.BinaryOperatorExpression, Type: System.Boolean) (Syntax: 'i < 100')
      Left: ILocalReferenceExpression: i (OperationKind.LocalReferenceExpression, Type: System.Int32) (Syntax: 'i')
      Right: ILiteralExpression (Text: 100) (OperationKind.LiteralExpression, Type: System.Int32, Constant: 100) (Syntax: '100')
  Locals: Local_1: System.Int32 i
  Before:
      IVariableDeclarationStatement (1 declarations) (OperationKind.VariableDeclarationStatement) (Syntax: 'i = 10')
        IVariableDeclaration (1 variables) (OperationKind.VariableDeclaration) (Syntax: 'i = 10')
          Variables: Local_1: System.Int32 i
          Initializer: ILiteralExpression (Text: 10) (OperationKind.LiteralExpression, Type: System.Int32, Constant: 10) (Syntax: '10')
  AtLoopBottom:
      IExpressionStatement (OperationKind.ExpressionStatement) (Syntax: 'i = i + 1')
        Expression: ISimpleAssignmentExpression (OperationKind.SimpleAssignmentExpression, Type: System.Int32) (Syntax: 'i = i + 1')
            Left: ILocalReferenceExpression: i (OperationKind.LocalReferenceExpression, Type: System.Int32) (Syntax: 'i')
            Right: IBinaryOperatorExpression (BinaryOperationKind.IntegerAdd) (OperationKind.BinaryOperatorExpression, Type: System.Int32) (Syntax: 'i + 1')
                Left: ILocalReferenceExpression: i (OperationKind.LocalReferenceExpression, Type: System.Int32) (Syntax: 'i')
                Right: ILiteralExpression (Text: 1) (OperationKind.LiteralExpression, Type: System.Int32, Constant: 1) (Syntax: '1')
  Body: IEmptyStatement (OperationKind.EmptyStatement) (Syntax: ';')
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
IForLoopStatement (LoopKind.For) (OperationKind.LoopStatement) (Syntax: 'for (int i  ... }')
  Condition: IBinaryOperatorExpression (BinaryOperationKind.IntegerLessThan) (OperationKind.BinaryOperatorExpression, Type: System.Boolean) (Syntax: 'i < 100')
      Left: ILocalReferenceExpression: i (OperationKind.LocalReferenceExpression, Type: System.Int32) (Syntax: 'i')
      Right: ILiteralExpression (Text: 100) (OperationKind.LiteralExpression, Type: System.Int32, Constant: 100) (Syntax: '100')
  Locals: Local_1: System.Int32 i
  Before:
      IVariableDeclarationStatement (1 declarations) (OperationKind.VariableDeclarationStatement) (Syntax: 'i = 0')
        IVariableDeclaration (1 variables) (OperationKind.VariableDeclaration) (Syntax: 'i = 0')
          Variables: Local_1: System.Int32 i
          Initializer: ILiteralExpression (Text: 0) (OperationKind.LiteralExpression, Type: System.Int32, Constant: 0) (Syntax: '0')
  AtLoopBottom:
      IExpressionStatement (OperationKind.ExpressionStatement) (Syntax: 'i = i + 1')
        Expression: ISimpleAssignmentExpression (OperationKind.SimpleAssignmentExpression, Type: System.Int32) (Syntax: 'i = i + 1')
            Left: ILocalReferenceExpression: i (OperationKind.LocalReferenceExpression, Type: System.Int32) (Syntax: 'i')
            Right: IBinaryOperatorExpression (BinaryOperationKind.IntegerAdd) (OperationKind.BinaryOperatorExpression, Type: System.Int32) (Syntax: 'i + 1')
                Left: ILocalReferenceExpression: i (OperationKind.LocalReferenceExpression, Type: System.Int32) (Syntax: 'i')
                Right: ILiteralExpression (Text: 1) (OperationKind.LiteralExpression, Type: System.Int32, Constant: 1) (Syntax: '1')
  Body: IBlockStatement (1 statements) (OperationKind.BlockStatement) (Syntax: '{ ... }')
      IForLoopStatement (LoopKind.For) (OperationKind.LoopStatement) (Syntax: 'for (int j  ... }')
        Condition: IBinaryOperatorExpression (BinaryOperationKind.IntegerLessThan) (OperationKind.BinaryOperatorExpression, Type: System.Boolean) (Syntax: 'j < 10')
            Left: ILocalReferenceExpression: j (OperationKind.LocalReferenceExpression, Type: System.Int32) (Syntax: 'j')
            Right: ILiteralExpression (Text: 10) (OperationKind.LiteralExpression, Type: System.Int32, Constant: 10) (Syntax: '10')
        Locals: Local_1: System.Int32 j
        Before:
            IVariableDeclarationStatement (1 declarations) (OperationKind.VariableDeclarationStatement) (Syntax: 'j = 0')
              IVariableDeclaration (1 variables) (OperationKind.VariableDeclaration) (Syntax: 'j = 0')
                Variables: Local_1: System.Int32 j
                Initializer: ILiteralExpression (Text: 0) (OperationKind.LiteralExpression, Type: System.Int32, Constant: 0) (Syntax: '0')
        AtLoopBottom:
            IExpressionStatement (OperationKind.ExpressionStatement) (Syntax: 'j = j + 1')
              Expression: ISimpleAssignmentExpression (OperationKind.SimpleAssignmentExpression, Type: System.Int32) (Syntax: 'j = j + 1')
                  Left: ILocalReferenceExpression: j (OperationKind.LocalReferenceExpression, Type: System.Int32) (Syntax: 'j')
                  Right: IBinaryOperatorExpression (BinaryOperationKind.IntegerAdd) (OperationKind.BinaryOperatorExpression, Type: System.Int32) (Syntax: 'j + 1')
                      Left: ILocalReferenceExpression: j (OperationKind.LocalReferenceExpression, Type: System.Int32) (Syntax: 'j')
                      Right: ILiteralExpression (Text: 1) (OperationKind.LiteralExpression, Type: System.Int32, Constant: 1) (Syntax: '1')
        Body: IBlockStatement (0 statements) (OperationKind.BlockStatement) (Syntax: '{ ... }')
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
IForLoopStatement (LoopKind.For) (OperationKind.LoopStatement) (Syntax: 'for (int i  ... }')
  Condition: IBinaryOperatorExpression (BinaryOperationKind.IntegerLessThan) (OperationKind.BinaryOperatorExpression, Type: System.Boolean) (Syntax: 'i < 10')
      Left: ILocalReferenceExpression: i (OperationKind.LocalReferenceExpression, Type: System.Int32) (Syntax: 'i')
      Right: ILiteralExpression (Text: 10) (OperationKind.LiteralExpression, Type: System.Int32, Constant: 10) (Syntax: '10')
  Locals: Local_1: System.Int32 i
  Before:
      IVariableDeclarationStatement (1 declarations) (OperationKind.VariableDeclarationStatement) (Syntax: 'i = 0')
        IVariableDeclaration (1 variables) (OperationKind.VariableDeclaration) (Syntax: 'i = 0')
          Variables: Local_1: System.Int32 i
          Initializer: ILiteralExpression (Text: 0) (OperationKind.LiteralExpression, Type: System.Int32, Constant: 0) (Syntax: '0')
  AtLoopBottom:
      IExpressionStatement (OperationKind.ExpressionStatement) (Syntax: 'i = i + 1')
        Expression: ISimpleAssignmentExpression (OperationKind.SimpleAssignmentExpression, Type: System.Int32) (Syntax: 'i = i + 1')
            Left: ILocalReferenceExpression: i (OperationKind.LocalReferenceExpression, Type: System.Int32) (Syntax: 'i')
            Right: IBinaryOperatorExpression (BinaryOperationKind.IntegerAdd) (OperationKind.BinaryOperatorExpression, Type: System.Int32) (Syntax: 'i + 1')
                Left: ILocalReferenceExpression: i (OperationKind.LocalReferenceExpression, Type: System.Int32) (Syntax: 'i')
                Right: ILiteralExpression (Text: 1) (OperationKind.LiteralExpression, Type: System.Int32, Constant: 1) (Syntax: '1')
  Body: IBlockStatement (1 statements) (OperationKind.BlockStatement) (Syntax: '{ ... }')
      IForLoopStatement (LoopKind.For) (OperationKind.LoopStatement) (Syntax: 'for (int j  ... }')
        Condition: IBinaryOperatorExpression (BinaryOperationKind.IntegerLessThan) (OperationKind.BinaryOperatorExpression, Type: System.Boolean) (Syntax: 'j < 10')
            Left: ILocalReferenceExpression: j (OperationKind.LocalReferenceExpression, Type: System.Int32) (Syntax: 'j')
            Right: ILiteralExpression (Text: 10) (OperationKind.LiteralExpression, Type: System.Int32, Constant: 10) (Syntax: '10')
        Locals: Local_1: System.Int32 j
        Before:
            IVariableDeclarationStatement (1 declarations) (OperationKind.VariableDeclarationStatement) (Syntax: 'j = 0')
              IVariableDeclaration (1 variables) (OperationKind.VariableDeclaration) (Syntax: 'j = 0')
                Variables: Local_1: System.Int32 j
                Initializer: ILiteralExpression (Text: 0) (OperationKind.LiteralExpression, Type: System.Int32, Constant: 0) (Syntax: '0')
        AtLoopBottom:
            IExpressionStatement (OperationKind.ExpressionStatement) (Syntax: 'j = j + 1')
              Expression: ISimpleAssignmentExpression (OperationKind.SimpleAssignmentExpression, Type: System.Int32) (Syntax: 'j = j + 1')
                  Left: ILocalReferenceExpression: j (OperationKind.LocalReferenceExpression, Type: System.Int32) (Syntax: 'j')
                  Right: IBinaryOperatorExpression (BinaryOperationKind.IntegerAdd) (OperationKind.BinaryOperatorExpression, Type: System.Int32) (Syntax: 'j + 1')
                      Left: ILocalReferenceExpression: j (OperationKind.LocalReferenceExpression, Type: System.Int32) (Syntax: 'j')
                      Right: ILiteralExpression (Text: 1) (OperationKind.LiteralExpression, Type: System.Int32, Constant: 1) (Syntax: '1')
        Body: IBlockStatement (1 statements) (OperationKind.BlockStatement) (Syntax: '{ ... }')
            IExpressionStatement (OperationKind.ExpressionStatement) (Syntax: 'i = 1;')
              Expression: ISimpleAssignmentExpression (OperationKind.SimpleAssignmentExpression, Type: System.Int32) (Syntax: 'i = 1')
                  Left: ILocalReferenceExpression: i (OperationKind.LocalReferenceExpression, Type: System.Int32) (Syntax: 'i')
                  Right: ILiteralExpression (Text: 1) (OperationKind.LiteralExpression, Type: System.Int32, Constant: 1) (Syntax: '1')
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
IForLoopStatement (LoopKind.For) (OperationKind.LoopStatement) (Syntax: 'for (int i  ... }')
  Condition: IBinaryOperatorExpression (BinaryOperationKind.IntegerLessThan) (OperationKind.BinaryOperatorExpression, Type: System.Boolean) (Syntax: 'i < 5')
      Left: ILocalReferenceExpression: i (OperationKind.LocalReferenceExpression, Type: System.Int32) (Syntax: 'i')
      Right: ILiteralExpression (Text: 5) (OperationKind.LiteralExpression, Type: System.Int32, Constant: 5) (Syntax: '5')
  Locals: Local_1: System.Int32 i
  Before:
      IVariableDeclarationStatement (1 declarations) (OperationKind.VariableDeclarationStatement) (Syntax: 'i = 0')
        IVariableDeclaration (1 variables) (OperationKind.VariableDeclaration) (Syntax: 'i = 0')
          Variables: Local_1: System.Int32 i
          Initializer: ILiteralExpression (Text: 0) (OperationKind.LiteralExpression, Type: System.Int32, Constant: 0) (Syntax: '0')
  AtLoopBottom:
      IExpressionStatement (OperationKind.ExpressionStatement) (Syntax: 'i = i + 1')
        Expression: ISimpleAssignmentExpression (OperationKind.SimpleAssignmentExpression, Type: System.Int32) (Syntax: 'i = i + 1')
            Left: ILocalReferenceExpression: i (OperationKind.LocalReferenceExpression, Type: System.Int32) (Syntax: 'i')
            Right: IBinaryOperatorExpression (BinaryOperationKind.IntegerAdd) (OperationKind.BinaryOperatorExpression, Type: System.Int32) (Syntax: 'i + 1')
                Left: ILocalReferenceExpression: i (OperationKind.LocalReferenceExpression, Type: System.Int32) (Syntax: 'i')
                Right: ILiteralExpression (Text: 1) (OperationKind.LiteralExpression, Type: System.Int32, Constant: 1) (Syntax: '1')
  Body: IBlockStatement (1 statements) (OperationKind.BlockStatement) (Syntax: '{ ... }')
      IForLoopStatement (LoopKind.For) (OperationKind.LoopStatement) (Syntax: 'for (int j  ... }')
        Condition: IBinaryOperatorExpression (BinaryOperationKind.IntegerLessThan) (OperationKind.BinaryOperatorExpression, Type: System.Boolean) (Syntax: 'i < j')
            Left: ILocalReferenceExpression: i (OperationKind.LocalReferenceExpression, Type: System.Int32) (Syntax: 'i')
            Right: ILocalReferenceExpression: j (OperationKind.LocalReferenceExpression, Type: System.Int32) (Syntax: 'j')
        Locals: Local_1: System.Int32 j
        Before:
            IVariableDeclarationStatement (1 declarations) (OperationKind.VariableDeclarationStatement) (Syntax: 'j = i + 1')
              IVariableDeclaration (1 variables) (OperationKind.VariableDeclaration) (Syntax: 'j = i + 1')
                Variables: Local_1: System.Int32 j
                Initializer: IBinaryOperatorExpression (BinaryOperationKind.IntegerAdd) (OperationKind.BinaryOperatorExpression, Type: System.Int32) (Syntax: 'i + 1')
                    Left: ILocalReferenceExpression: i (OperationKind.LocalReferenceExpression, Type: System.Int32) (Syntax: 'i')
                    Right: ILiteralExpression (Text: 1) (OperationKind.LiteralExpression, Type: System.Int32, Constant: 1) (Syntax: '1')
        AtLoopBottom:
            IExpressionStatement (OperationKind.ExpressionStatement) (Syntax: 'j = j - 1')
              Expression: ISimpleAssignmentExpression (OperationKind.SimpleAssignmentExpression, Type: System.Int32) (Syntax: 'j = j - 1')
                  Left: ILocalReferenceExpression: j (OperationKind.LocalReferenceExpression, Type: System.Int32) (Syntax: 'j')
                  Right: IBinaryOperatorExpression (BinaryOperationKind.IntegerSubtract) (OperationKind.BinaryOperatorExpression, Type: System.Int32) (Syntax: 'j - 1')
                      Left: ILocalReferenceExpression: j (OperationKind.LocalReferenceExpression, Type: System.Int32) (Syntax: 'j')
                      Right: ILiteralExpression (Text: 1) (OperationKind.LiteralExpression, Type: System.Int32, Constant: 1) (Syntax: '1')
        Body: IBlockStatement (0 statements) (OperationKind.BlockStatement) (Syntax: '{ ... }')
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
IForLoopStatement (LoopKind.For) (OperationKind.LoopStatement) (Syntax: 'for (int i  ... }')
  Condition: IBinaryOperatorExpression (BinaryOperationKind.IntegerLessThan) (OperationKind.BinaryOperatorExpression, Type: System.Boolean) (Syntax: 'i < 5')
      Left: ILocalReferenceExpression: i (OperationKind.LocalReferenceExpression, Type: System.Int32) (Syntax: 'i')
      Right: ILiteralExpression (Text: 5) (OperationKind.LiteralExpression, Type: System.Int32, Constant: 5) (Syntax: '5')
  Locals: Local_1: System.Int32 i
  Before:
      IVariableDeclarationStatement (1 declarations) (OperationKind.VariableDeclarationStatement) (Syntax: 'i = 0')
        IVariableDeclaration (1 variables) (OperationKind.VariableDeclaration) (Syntax: 'i = 0')
          Variables: Local_1: System.Int32 i
          Initializer: ILiteralExpression (Text: 0) (OperationKind.LiteralExpression, Type: System.Int32, Constant: 0) (Syntax: '0')
  AtLoopBottom:
      IExpressionStatement (OperationKind.ExpressionStatement) (Syntax: 'i = i + 1')
        Expression: ISimpleAssignmentExpression (OperationKind.SimpleAssignmentExpression, Type: System.Int32) (Syntax: 'i = i + 1')
            Left: ILocalReferenceExpression: i (OperationKind.LocalReferenceExpression, Type: System.Int32) (Syntax: 'i')
            Right: IBinaryOperatorExpression (BinaryOperationKind.IntegerAdd) (OperationKind.BinaryOperatorExpression, Type: System.Int32) (Syntax: 'i + 1')
                Left: ILocalReferenceExpression: i (OperationKind.LocalReferenceExpression, Type: System.Int32) (Syntax: 'i')
                Right: ILiteralExpression (Text: 1) (OperationKind.LiteralExpression, Type: System.Int32, Constant: 1) (Syntax: '1')
  Body: IBlockStatement (1 statements) (OperationKind.BlockStatement) (Syntax: '{ ... }')
      IForLoopStatement (LoopKind.For) (OperationKind.LoopStatement) (Syntax: 'for (int j  ... }')
        Condition: IBinaryOperatorExpression (BinaryOperationKind.IntegerLessThan) (OperationKind.BinaryOperatorExpression, Type: System.Boolean) (Syntax: 'j < 10')
            Left: ILocalReferenceExpression: j (OperationKind.LocalReferenceExpression, Type: System.Int32) (Syntax: 'j')
            Right: ILiteralExpression (Text: 10) (OperationKind.LiteralExpression, Type: System.Int32, Constant: 10) (Syntax: '10')
        Locals: Local_1: System.Int32 j
        Before:
            IVariableDeclarationStatement (1 declarations) (OperationKind.VariableDeclarationStatement) (Syntax: 'j = 0')
              IVariableDeclaration (1 variables) (OperationKind.VariableDeclaration) (Syntax: 'j = 0')
                Variables: Local_1: System.Int32 j
                Initializer: ILiteralExpression (Text: 0) (OperationKind.LiteralExpression, Type: System.Int32, Constant: 0) (Syntax: '0')
        AtLoopBottom:
            IExpressionStatement (OperationKind.ExpressionStatement) (Syntax: 'j = j + 1')
              Expression: ISimpleAssignmentExpression (OperationKind.SimpleAssignmentExpression, Type: System.Int32) (Syntax: 'j = j + 1')
                  Left: ILocalReferenceExpression: j (OperationKind.LocalReferenceExpression, Type: System.Int32) (Syntax: 'j')
                  Right: IBinaryOperatorExpression (BinaryOperationKind.IntegerAdd) (OperationKind.BinaryOperatorExpression, Type: System.Int32) (Syntax: 'j + 1')
                      Left: ILocalReferenceExpression: j (OperationKind.LocalReferenceExpression, Type: System.Int32) (Syntax: 'j')
                      Right: ILiteralExpression (Text: 1) (OperationKind.LiteralExpression, Type: System.Int32, Constant: 1) (Syntax: '1')
        Body: IBlockStatement (1 statements) (OperationKind.BlockStatement) (Syntax: '{ ... }')
            IIfStatement (OperationKind.IfStatement) (Syntax: 'if (j == 5) ... break;')
              Condition: IBinaryOperatorExpression (BinaryOperationKind.IntegerEquals) (OperationKind.BinaryOperatorExpression, Type: System.Boolean) (Syntax: 'j == 5')
                  Left: ILocalReferenceExpression: j (OperationKind.LocalReferenceExpression, Type: System.Int32) (Syntax: 'j')
                  Right: ILiteralExpression (Text: 5) (OperationKind.LiteralExpression, Type: System.Int32, Constant: 5) (Syntax: '5')
              IfTrue: IBranchStatement (BranchKind.Break) (OperationKind.BranchStatement) (Syntax: 'break;')
              IfFalse: null
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
IForLoopStatement (LoopKind.For) (OperationKind.LoopStatement) (Syntax: 'for (int i  ... }')
  Condition: IBinaryOperatorExpression (BinaryOperationKind.IntegerLessThan) (OperationKind.BinaryOperatorExpression, Type: System.Boolean) (Syntax: 'i < 5')
      Left: ILocalReferenceExpression: i (OperationKind.LocalReferenceExpression, Type: System.Int32) (Syntax: 'i')
      Right: ILiteralExpression (Text: 5) (OperationKind.LiteralExpression, Type: System.Int32, Constant: 5) (Syntax: '5')
  Locals: Local_1: System.Int32 i
  Before:
      IVariableDeclarationStatement (1 declarations) (OperationKind.VariableDeclarationStatement) (Syntax: 'i = 0')
        IVariableDeclaration (1 variables) (OperationKind.VariableDeclaration) (Syntax: 'i = 0')
          Variables: Local_1: System.Int32 i
          Initializer: ILiteralExpression (Text: 0) (OperationKind.LiteralExpression, Type: System.Int32, Constant: 0) (Syntax: '0')
  AtLoopBottom:
      IExpressionStatement (OperationKind.ExpressionStatement) (Syntax: 'i = i + 1')
        Expression: ISimpleAssignmentExpression (OperationKind.SimpleAssignmentExpression, Type: System.Int32) (Syntax: 'i = i + 1')
            Left: ILocalReferenceExpression: i (OperationKind.LocalReferenceExpression, Type: System.Int32) (Syntax: 'i')
            Right: IBinaryOperatorExpression (BinaryOperationKind.IntegerAdd) (OperationKind.BinaryOperatorExpression, Type: System.Int32) (Syntax: 'i + 1')
                Left: ILocalReferenceExpression: i (OperationKind.LocalReferenceExpression, Type: System.Int32) (Syntax: 'i')
                Right: ILiteralExpression (Text: 1) (OperationKind.LiteralExpression, Type: System.Int32, Constant: 1) (Syntax: '1')
  Body: IBlockStatement (1 statements) (OperationKind.BlockStatement) (Syntax: '{ ... }')
      IForLoopStatement (LoopKind.For) (OperationKind.LoopStatement) (Syntax: 'for (int j  ... }')
        Condition: IBinaryOperatorExpression (BinaryOperationKind.IntegerLessThan) (OperationKind.BinaryOperatorExpression, Type: System.Boolean) (Syntax: 'j < 10')
            Left: ILocalReferenceExpression: j (OperationKind.LocalReferenceExpression, Type: System.Int32) (Syntax: 'j')
            Right: ILiteralExpression (Text: 10) (OperationKind.LiteralExpression, Type: System.Int32, Constant: 10) (Syntax: '10')
        Locals: Local_1: System.Int32 j
        Before:
            IVariableDeclarationStatement (1 declarations) (OperationKind.VariableDeclarationStatement) (Syntax: 'j = 1')
              IVariableDeclaration (1 variables) (OperationKind.VariableDeclaration) (Syntax: 'j = 1')
                Variables: Local_1: System.Int32 j
                Initializer: ILiteralExpression (Text: 1) (OperationKind.LiteralExpression, Type: System.Int32, Constant: 1) (Syntax: '1')
        AtLoopBottom:
            IExpressionStatement (OperationKind.ExpressionStatement) (Syntax: 'j = j + 1')
              Expression: ISimpleAssignmentExpression (OperationKind.SimpleAssignmentExpression, Type: System.Int32) (Syntax: 'j = j + 1')
                  Left: ILocalReferenceExpression: j (OperationKind.LocalReferenceExpression, Type: System.Int32) (Syntax: 'j')
                  Right: IBinaryOperatorExpression (BinaryOperationKind.IntegerAdd) (OperationKind.BinaryOperatorExpression, Type: System.Int32) (Syntax: 'j + 1')
                      Left: ILocalReferenceExpression: j (OperationKind.LocalReferenceExpression, Type: System.Int32) (Syntax: 'j')
                      Right: ILiteralExpression (Text: 1) (OperationKind.LiteralExpression, Type: System.Int32, Constant: 1) (Syntax: '1')
        Body: IBlockStatement (3 statements) (OperationKind.BlockStatement) (Syntax: '{ ... }')
            IIfStatement (OperationKind.IfStatement) (Syntax: 'if ((j % 2) ... continue;')
              Condition: IBinaryOperatorExpression (BinaryOperationKind.IntegerNotEquals) (OperationKind.BinaryOperatorExpression, Type: System.Boolean) (Syntax: '(j % 2) != 0')
                  Left: IBinaryOperatorExpression (BinaryOperationKind.IntegerRemainder) (OperationKind.BinaryOperatorExpression, Type: System.Int32) (Syntax: 'j % 2')
                      Left: ILocalReferenceExpression: j (OperationKind.LocalReferenceExpression, Type: System.Int32) (Syntax: 'j')
                      Right: ILiteralExpression (Text: 2) (OperationKind.LiteralExpression, Type: System.Int32, Constant: 2) (Syntax: '2')
                  Right: ILiteralExpression (Text: 0) (OperationKind.LiteralExpression, Type: System.Int32, Constant: 0) (Syntax: '0')
              IfTrue: IBranchStatement (BranchKind.Continue) (OperationKind.BranchStatement) (Syntax: 'continue;')
              IfFalse: null
            IExpressionStatement (OperationKind.ExpressionStatement) (Syntax: 'i = i + 1;')
              Expression: ISimpleAssignmentExpression (OperationKind.SimpleAssignmentExpression, Type: System.Int32) (Syntax: 'i = i + 1')
                  Left: ILocalReferenceExpression: i (OperationKind.LocalReferenceExpression, Type: System.Int32) (Syntax: 'i')
                  Right: IBinaryOperatorExpression (BinaryOperationKind.IntegerAdd) (OperationKind.BinaryOperatorExpression, Type: System.Int32) (Syntax: 'i + 1')
                      Left: ILocalReferenceExpression: i (OperationKind.LocalReferenceExpression, Type: System.Int32) (Syntax: 'i')
                      Right: ILiteralExpression (Text: 1) (OperationKind.LiteralExpression, Type: System.Int32, Constant: 1) (Syntax: '1')
            IExpressionStatement (OperationKind.ExpressionStatement) (Syntax: 'System.Console.Write(i);')
              Expression: IInvocationExpression (void System.Console.Write(System.Int32 value)) (OperationKind.InvocationExpression, Type: System.Void) (Syntax: 'System.Console.Write(i)')
                  Instance Receiver: null
                  Arguments(1):
                      IArgument (ArgumentKind.Explicit, Matching Parameter: value) (OperationKind.Argument) (Syntax: 'i')
                        ILocalReferenceExpression: i (OperationKind.LocalReferenceExpression, Type: System.Int32) (Syntax: 'i')
                        InConversion: null
                        OutConversion: null
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
IForLoopStatement (LoopKind.For) (OperationKind.LoopStatement) (Syntax: 'for (int i  ... }')
  Condition: IBinaryOperatorExpression (BinaryOperationKind.IntegerLessThan) (OperationKind.BinaryOperatorExpression, Type: System.Boolean) (Syntax: 'i < 5')
      Left: ILocalReferenceExpression: i (OperationKind.LocalReferenceExpression, Type: System.Int32) (Syntax: 'i')
      Right: ILiteralExpression (Text: 5) (OperationKind.LiteralExpression, Type: System.Int32, Constant: 5) (Syntax: '5')
  Locals: Local_1: System.Int32 i
  Before:
      IVariableDeclarationStatement (1 declarations) (OperationKind.VariableDeclarationStatement) (Syntax: 'i = 0')
        IVariableDeclaration (1 variables) (OperationKind.VariableDeclaration) (Syntax: 'i = 0')
          Variables: Local_1: System.Int32 i
          Initializer: ILiteralExpression (Text: 0) (OperationKind.LiteralExpression, Type: System.Int32, Constant: 0) (Syntax: '0')
  AtLoopBottom:
      IExpressionStatement (OperationKind.ExpressionStatement) (Syntax: 'i = i + 1')
        Expression: ISimpleAssignmentExpression (OperationKind.SimpleAssignmentExpression, Type: System.Int32) (Syntax: 'i = i + 1')
            Left: ILocalReferenceExpression: i (OperationKind.LocalReferenceExpression, Type: System.Int32) (Syntax: 'i')
            Right: IBinaryOperatorExpression (BinaryOperationKind.IntegerAdd) (OperationKind.BinaryOperatorExpression, Type: System.Int32) (Syntax: 'i + 1')
                Left: ILocalReferenceExpression: i (OperationKind.LocalReferenceExpression, Type: System.Int32) (Syntax: 'i')
                Right: ILiteralExpression (Text: 1) (OperationKind.LiteralExpression, Type: System.Int32, Constant: 1) (Syntax: '1')
  Body: IBlockStatement (1 statements) (OperationKind.BlockStatement) (Syntax: '{ ... }')
      IForLoopStatement (LoopKind.For) (OperationKind.LoopStatement) (Syntax: 'for (int j  ... }')
        Condition: IBinaryOperatorExpression (BinaryOperationKind.IntegerLessThan) (OperationKind.BinaryOperatorExpression, Type: System.Boolean) (Syntax: 'j < 10')
            Left: ILocalReferenceExpression: j (OperationKind.LocalReferenceExpression, Type: System.Int32) (Syntax: 'j')
            Right: ILiteralExpression (Text: 10) (OperationKind.LiteralExpression, Type: System.Int32, Constant: 10) (Syntax: '10')
        Locals: Local_1: System.Int32 j
        Before:
            IVariableDeclarationStatement (1 declarations) (OperationKind.VariableDeclarationStatement) (Syntax: 'j = 0')
              IVariableDeclaration (1 variables) (OperationKind.VariableDeclaration) (Syntax: 'j = 0')
                Variables: Local_1: System.Int32 j
                Initializer: ILiteralExpression (Text: 0) (OperationKind.LiteralExpression, Type: System.Int32, Constant: 0) (Syntax: '0')
        AtLoopBottom:
            IExpressionStatement (OperationKind.ExpressionStatement) (Syntax: 'j = j + 1')
              Expression: ISimpleAssignmentExpression (OperationKind.SimpleAssignmentExpression, Type: System.Int32) (Syntax: 'j = j + 1')
                  Left: ILocalReferenceExpression: j (OperationKind.LocalReferenceExpression, Type: System.Int32) (Syntax: 'j')
                  Right: IBinaryOperatorExpression (BinaryOperationKind.IntegerAdd) (OperationKind.BinaryOperatorExpression, Type: System.Int32) (Syntax: 'j + 1')
                      Left: ILocalReferenceExpression: j (OperationKind.LocalReferenceExpression, Type: System.Int32) (Syntax: 'j')
                      Right: ILiteralExpression (Text: 1) (OperationKind.LiteralExpression, Type: System.Int32, Constant: 1) (Syntax: '1')
        Body: IBlockStatement (2 statements) (OperationKind.BlockStatement) (Syntax: '{ ... }')
            IBranchStatement (BranchKind.GoTo, Label: stop) (OperationKind.BranchStatement) (Syntax: 'goto stop;')
            ILabeledStatement (Label: stop) (OperationKind.LabeledStatement) (Syntax: 'stop: ... j = j + 1;')
              Statement: IExpressionStatement (OperationKind.ExpressionStatement) (Syntax: 'j = j + 1;')
                  Expression: ISimpleAssignmentExpression (OperationKind.SimpleAssignmentExpression, Type: System.Int32) (Syntax: 'j = j + 1')
                      Left: ILocalReferenceExpression: j (OperationKind.LocalReferenceExpression, Type: System.Int32) (Syntax: 'j')
                      Right: IBinaryOperatorExpression (BinaryOperationKind.IntegerAdd) (OperationKind.BinaryOperatorExpression, Type: System.Int32) (Syntax: 'j + 1')
                          Left: ILocalReferenceExpression: j (OperationKind.LocalReferenceExpression, Type: System.Int32) (Syntax: 'j')
                          Right: ILiteralExpression (Text: 1) (OperationKind.LiteralExpression, Type: System.Int32, Constant: 1) (Syntax: '1')
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
IForLoopStatement (LoopKind.For) (OperationKind.LoopStatement) (Syntax: 'for (int i  ... }')
  Condition: IBinaryOperatorExpression (BinaryOperationKind.IntegerLessThan) (OperationKind.BinaryOperatorExpression, Type: System.Boolean) (Syntax: 'i < 10')
      Left: ILocalReferenceExpression: i (OperationKind.LocalReferenceExpression, Type: System.Int32) (Syntax: 'i')
      Right: ILiteralExpression (Text: 10) (OperationKind.LiteralExpression, Type: System.Int32, Constant: 10) (Syntax: '10')
  Locals: Local_1: System.Int32 i
  Before:
      IVariableDeclarationStatement (1 declarations) (OperationKind.VariableDeclarationStatement) (Syntax: 'i = 0')
        IVariableDeclaration (1 variables) (OperationKind.VariableDeclaration) (Syntax: 'i = 0')
          Variables: Local_1: System.Int32 i
          Initializer: ILiteralExpression (Text: 0) (OperationKind.LiteralExpression, Type: System.Int32, Constant: 0) (Syntax: '0')
  AtLoopBottom:
      IExpressionStatement (OperationKind.ExpressionStatement) (Syntax: 'i = i + 1')
        Expression: ISimpleAssignmentExpression (OperationKind.SimpleAssignmentExpression, Type: System.Int32) (Syntax: 'i = i + 1')
            Left: ILocalReferenceExpression: i (OperationKind.LocalReferenceExpression, Type: System.Int32) (Syntax: 'i')
            Right: IBinaryOperatorExpression (BinaryOperationKind.IntegerAdd) (OperationKind.BinaryOperatorExpression, Type: System.Int32) (Syntax: 'i + 1')
                Left: ILocalReferenceExpression: i (OperationKind.LocalReferenceExpression, Type: System.Int32) (Syntax: 'i')
                Right: ILiteralExpression (Text: 1) (OperationKind.LiteralExpression, Type: System.Int32, Constant: 1) (Syntax: '1')
  Body: IBlockStatement (1 statements) (OperationKind.BlockStatement) (Syntax: '{ ... }')
      IForLoopStatement (LoopKind.For) (OperationKind.LoopStatement) (Syntax: 'for (int j  ... }')
        Condition: IBinaryOperatorExpression (BinaryOperationKind.IntegerLessThan) (OperationKind.BinaryOperatorExpression, Type: System.Boolean) (Syntax: 'j < 10')
            Left: ILocalReferenceExpression: j (OperationKind.LocalReferenceExpression, Type: System.Int32) (Syntax: 'j')
            Right: ILiteralExpression (Text: 10) (OperationKind.LiteralExpression, Type: System.Int32, Constant: 10) (Syntax: '10')
        Locals: Local_1: System.Int32 j
        Before:
            IVariableDeclarationStatement (1 declarations) (OperationKind.VariableDeclarationStatement) (Syntax: 'j = 0')
              IVariableDeclaration (1 variables) (OperationKind.VariableDeclaration) (Syntax: 'j = 0')
                Variables: Local_1: System.Int32 j
                Initializer: ILiteralExpression (Text: 0) (OperationKind.LiteralExpression, Type: System.Int32, Constant: 0) (Syntax: '0')
        AtLoopBottom(0)
        Body: IBlockStatement (1 statements) (OperationKind.BlockStatement) (Syntax: '{ ... }')
            IThrowStatement (OperationKind.ThrowStatement) (Syntax: 'throw new S ... xception();')
              ThrownObject: IObjectCreationExpression (Constructor: System.Exception..ctor()) (OperationKind.ObjectCreationExpression, Type: System.Exception) (Syntax: 'new System.Exception()')
                  Arguments(0)
                  Initializer: null
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
IForLoopStatement (LoopKind.For) (OperationKind.LoopStatement) (Syntax: 'for (int i  ... }')
  Condition: IBinaryOperatorExpression (BinaryOperationKind.IntegerLessThan) (OperationKind.BinaryOperatorExpression, Type: System.Boolean) (Syntax: 'i < 10')
      Left: ILocalReferenceExpression: i (OperationKind.LocalReferenceExpression, Type: System.Int32) (Syntax: 'i')
      Right: ILiteralExpression (Text: 10) (OperationKind.LiteralExpression, Type: System.Int32, Constant: 10) (Syntax: '10')
  Locals: Local_1: System.Int32 i
  Before:
      IVariableDeclarationStatement (1 declarations) (OperationKind.VariableDeclarationStatement) (Syntax: 'i = 0')
        IVariableDeclaration (1 variables) (OperationKind.VariableDeclaration) (Syntax: 'i = 0')
          Variables: Local_1: System.Int32 i
          Initializer: ILiteralExpression (Text: 0) (OperationKind.LiteralExpression, Type: System.Int32, Constant: 0) (Syntax: '0')
  AtLoopBottom(0)
  Body: IBlockStatement (1 statements) (OperationKind.BlockStatement) (Syntax: '{ ... }')
      IForLoopStatement (LoopKind.For) (OperationKind.LoopStatement) (Syntax: 'for (int j  ... }')
        Condition: IBinaryOperatorExpression (BinaryOperationKind.IntegerLessThan) (OperationKind.BinaryOperatorExpression, Type: System.Boolean) (Syntax: 'j < 5')
            Left: ILocalReferenceExpression: j (OperationKind.LocalReferenceExpression, Type: System.Int32) (Syntax: 'j')
            Right: ILiteralExpression (Text: 5) (OperationKind.LiteralExpression, Type: System.Int32, Constant: 5) (Syntax: '5')
        Locals: Local_1: System.Int32 j
        Before:
            IVariableDeclarationStatement (1 declarations) (OperationKind.VariableDeclarationStatement) (Syntax: 'j = 0')
              IVariableDeclaration (1 variables) (OperationKind.VariableDeclaration) (Syntax: 'j = 0')
                Variables: Local_1: System.Int32 j
                Initializer: ILiteralExpression (Text: 0) (OperationKind.LiteralExpression, Type: System.Int32, Constant: 0) (Syntax: '0')
        AtLoopBottom(0)
        Body: IBlockStatement (1 statements) (OperationKind.BlockStatement) (Syntax: '{ ... }')
            IReturnStatement (OperationKind.ReturnStatement) (Syntax: 'return;')
              ReturnedValue: null
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
IForLoopStatement (LoopKind.For) (OperationKind.LoopStatement) (Syntax: 'for (int i  ... }')
  Condition: IBinaryOperatorExpression (BinaryOperationKind.IntegerLessThan) (OperationKind.BinaryOperatorExpression, Type: System.Boolean) (Syntax: 'i < 5')
      Left: ILocalReferenceExpression: i (OperationKind.LocalReferenceExpression, Type: System.Int32) (Syntax: 'i')
      Right: ILiteralExpression (Text: 5) (OperationKind.LiteralExpression, Type: System.Int32, Constant: 5) (Syntax: '5')
  Locals: Local_1: System.Int32 i
    Local_2: System.Int32 j
  Before:
      IVariableDeclarationStatement (2 declarations) (OperationKind.VariableDeclarationStatement) (Syntax: 'int i = 0, j = 1')
        IVariableDeclaration (1 variables) (OperationKind.VariableDeclaration) (Syntax: 'i = 0')
          Variables: Local_1: System.Int32 i
          Initializer: ILiteralExpression (Text: 0) (OperationKind.LiteralExpression, Type: System.Int32, Constant: 0) (Syntax: '0')
        IVariableDeclaration (1 variables) (OperationKind.VariableDeclaration) (Syntax: 'j = 1')
          Variables: Local_1: System.Int32 j
          Initializer: ILiteralExpression (Text: 1) (OperationKind.LiteralExpression, Type: System.Int32, Constant: 1) (Syntax: '1')
  AtLoopBottom:
      IExpressionStatement (OperationKind.ExpressionStatement) (Syntax: 'i = i + 1')
        Expression: ISimpleAssignmentExpression (OperationKind.SimpleAssignmentExpression, Type: System.Int32) (Syntax: 'i = i + 1')
            Left: ILocalReferenceExpression: i (OperationKind.LocalReferenceExpression, Type: System.Int32) (Syntax: 'i')
            Right: IBinaryOperatorExpression (BinaryOperationKind.IntegerAdd) (OperationKind.BinaryOperatorExpression, Type: System.Int32) (Syntax: 'i + 1')
                Left: ILocalReferenceExpression: i (OperationKind.LocalReferenceExpression, Type: System.Int32) (Syntax: 'i')
                Right: ILiteralExpression (Text: 1) (OperationKind.LiteralExpression, Type: System.Int32, Constant: 1) (Syntax: '1')
  Body: IBlockStatement (1 statements) (OperationKind.BlockStatement) (Syntax: '{ ... }')
      IExpressionStatement (OperationKind.ExpressionStatement) (Syntax: 'j = 2;')
        Expression: ISimpleAssignmentExpression (OperationKind.SimpleAssignmentExpression, Type: System.Int32) (Syntax: 'j = 2')
            Left: ILocalReferenceExpression: j (OperationKind.LocalReferenceExpression, Type: System.Int32) (Syntax: 'j')
            Right: ILiteralExpression (Text: 2) (OperationKind.LiteralExpression, Type: System.Int32, Constant: 2) (Syntax: '2')
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
IForLoopStatement (LoopKind.For) (OperationKind.LoopStatement) (Syntax: 'for (int i  ... }')
  Condition: IBinaryOperatorExpression (BinaryOperationKind.IntegerLessThan) (OperationKind.BinaryOperatorExpression, Type: System.Boolean) (Syntax: 'i < 50 - x')
      Left: ILocalReferenceExpression: i (OperationKind.LocalReferenceExpression, Type: System.Int32) (Syntax: 'i')
      Right: IBinaryOperatorExpression (BinaryOperationKind.IntegerSubtract) (OperationKind.BinaryOperatorExpression, Type: System.Int32) (Syntax: '50 - x')
          Left: ILiteralExpression (Text: 50) (OperationKind.LiteralExpression, Type: System.Int32, Constant: 50) (Syntax: '50')
          Right: ILocalReferenceExpression: x (OperationKind.LocalReferenceExpression, Type: System.Int32) (Syntax: 'x')
  Locals: Local_1: System.Int32 i
  Before:
      IVariableDeclarationStatement (1 declarations) (OperationKind.VariableDeclarationStatement) (Syntax: 'i = 0')
        IVariableDeclaration (1 variables) (OperationKind.VariableDeclaration) (Syntax: 'i = 0')
          Variables: Local_1: System.Int32 i
          Initializer: ILiteralExpression (Text: 0) (OperationKind.LiteralExpression, Type: System.Int32, Constant: 0) (Syntax: '0')
  AtLoopBottom:
      IExpressionStatement (OperationKind.ExpressionStatement) (Syntax: 'i = i + 1')
        Expression: ISimpleAssignmentExpression (OperationKind.SimpleAssignmentExpression, Type: System.Int32) (Syntax: 'i = i + 1')
            Left: ILocalReferenceExpression: i (OperationKind.LocalReferenceExpression, Type: System.Int32) (Syntax: 'i')
            Right: IBinaryOperatorExpression (BinaryOperationKind.IntegerAdd) (OperationKind.BinaryOperatorExpression, Type: System.Int32) (Syntax: 'i + 1')
                Left: ILocalReferenceExpression: i (OperationKind.LocalReferenceExpression, Type: System.Int32) (Syntax: 'i')
                Right: ILiteralExpression (Text: 1) (OperationKind.LiteralExpression, Type: System.Int32, Constant: 1) (Syntax: '1')
  Body: IBlockStatement (2 statements) (OperationKind.BlockStatement) (Syntax: '{ ... }')
      IExpressionStatement (OperationKind.ExpressionStatement) (Syntax: 'x = x + 1;')
        Expression: ISimpleAssignmentExpression (OperationKind.SimpleAssignmentExpression, Type: System.Int32) (Syntax: 'x = x + 1')
            Left: ILocalReferenceExpression: x (OperationKind.LocalReferenceExpression, Type: System.Int32) (Syntax: 'x')
            Right: IBinaryOperatorExpression (BinaryOperationKind.IntegerAdd) (OperationKind.BinaryOperatorExpression, Type: System.Int32) (Syntax: 'x + 1')
                Left: ILocalReferenceExpression: x (OperationKind.LocalReferenceExpression, Type: System.Int32) (Syntax: 'x')
                Right: ILiteralExpression (Text: 1) (OperationKind.LiteralExpression, Type: System.Int32, Constant: 1) (Syntax: '1')
      IExpressionStatement (OperationKind.ExpressionStatement) (Syntax: 'c = c + 1;')
        Expression: ISimpleAssignmentExpression (OperationKind.SimpleAssignmentExpression, Type: System.Int32) (Syntax: 'c = c + 1')
            Left: ILocalReferenceExpression: c (OperationKind.LocalReferenceExpression, Type: System.Int32) (Syntax: 'c')
            Right: IBinaryOperatorExpression (BinaryOperationKind.IntegerAdd) (OperationKind.BinaryOperatorExpression, Type: System.Int32) (Syntax: 'c + 1')
                Left: ILocalReferenceExpression: c (OperationKind.LocalReferenceExpression, Type: System.Int32) (Syntax: 'c')
                Right: ILiteralExpression (Text: 1) (OperationKind.LiteralExpression, Type: System.Int32, Constant: 1) (Syntax: '1')
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
IForLoopStatement (LoopKind.For) (OperationKind.LoopStatement) (Syntax: 'for (; fals ... }')
  Condition: ILiteralExpression (OperationKind.LiteralExpression, Type: System.Boolean, Constant: False) (Syntax: 'false')
  Before(0)
  AtLoopBottom(0)
  Body: IBlockStatement (1 statements) (OperationKind.BlockStatement) (Syntax: '{ ... }')
      IExpressionStatement (OperationKind.ExpressionStatement) (Syntax: 'System.Cons ... e(""hello"");')
        Expression: IInvocationExpression (void System.Console.WriteLine(System.String value)) (OperationKind.InvocationExpression, Type: System.Void) (Syntax: 'System.Cons ... ne(""hello"")')
            Instance Receiver: null
            Arguments(1):
                IArgument (ArgumentKind.Explicit, Matching Parameter: value) (OperationKind.Argument) (Syntax: '""hello""')
                  ILiteralExpression (OperationKind.LiteralExpression, Type: System.String, Constant: ""hello"") (Syntax: '""hello""')
                  InConversion: null
                  OutConversion: null
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
IForLoopStatement (LoopKind.For) (OperationKind.LoopStatement) (Syntax: 'for (F f =  ... }')
  Condition: IBinaryOperatorExpression (BinaryOperationKind.IntegerLessThan) (OperationKind.BinaryOperatorExpression, Type: System.Boolean) (Syntax: 'f.i < 5')
      Left: IFieldReferenceExpression: System.Int32 F.i (OperationKind.FieldReferenceExpression, Type: System.Int32) (Syntax: 'f.i')
          Instance Receiver: ILocalReferenceExpression: f (OperationKind.LocalReferenceExpression, Type: F) (Syntax: 'f')
      Right: ILiteralExpression (Text: 5) (OperationKind.LiteralExpression, Type: System.Int32, Constant: 5) (Syntax: '5')
  Locals: Local_1: F f
  Before:
      IVariableDeclarationStatement (1 declarations) (OperationKind.VariableDeclarationStatement) (Syntax: 'f = new F { ... s = ""abc"" }')
        IVariableDeclaration (1 variables) (OperationKind.VariableDeclaration) (Syntax: 'f = new F { ... s = ""abc"" }')
          Variables: Local_1: F f
          Initializer: IObjectCreationExpression (Constructor: F..ctor()) (OperationKind.ObjectCreationExpression, Type: F) (Syntax: 'new F { i = ... s = ""abc"" }')
              Arguments(0)
              Initializer: IObjectOrCollectionInitializerExpression (OperationKind.ObjectOrCollectionInitializerExpression, Type: F) (Syntax: '{ i = 0, s = ""abc"" }')
                  Initializers(2):
                      ISimpleAssignmentExpression (OperationKind.SimpleAssignmentExpression, Type: System.Int32) (Syntax: 'i = 0')
                        Left: IFieldReferenceExpression: System.Int32 F.i (OperationKind.FieldReferenceExpression, Type: System.Int32) (Syntax: 'i')
                            Instance Receiver: IInstanceReferenceExpression (InstanceReferenceKind.Implicit) (OperationKind.InstanceReferenceExpression, Type: F) (Syntax: 'i')
                        Right: ILiteralExpression (Text: 0) (OperationKind.LiteralExpression, Type: System.Int32, Constant: 0) (Syntax: '0')
                      ISimpleAssignmentExpression (OperationKind.SimpleAssignmentExpression, Type: System.String) (Syntax: 's = ""abc""')
                        Left: IFieldReferenceExpression: System.String F.s (OperationKind.FieldReferenceExpression, Type: System.String) (Syntax: 's')
                            Instance Receiver: IInstanceReferenceExpression (InstanceReferenceKind.Implicit) (OperationKind.InstanceReferenceExpression, Type: F) (Syntax: 's')
                        Right: ILiteralExpression (OperationKind.LiteralExpression, Type: System.String, Constant: ""abc"") (Syntax: '""abc""')
  AtLoopBottom:
      IExpressionStatement (OperationKind.ExpressionStatement) (Syntax: 'f.i = f.i + 1')
        Expression: ISimpleAssignmentExpression (OperationKind.SimpleAssignmentExpression, Type: System.Int32) (Syntax: 'f.i = f.i + 1')
            Left: IFieldReferenceExpression: System.Int32 F.i (OperationKind.FieldReferenceExpression, Type: System.Int32) (Syntax: 'f.i')
                Instance Receiver: ILocalReferenceExpression: f (OperationKind.LocalReferenceExpression, Type: F) (Syntax: 'f')
            Right: IBinaryOperatorExpression (BinaryOperationKind.IntegerAdd) (OperationKind.BinaryOperatorExpression, Type: System.Int32) (Syntax: 'f.i + 1')
                Left: IFieldReferenceExpression: System.Int32 F.i (OperationKind.FieldReferenceExpression, Type: System.Int32) (Syntax: 'f.i')
                    Instance Receiver: ILocalReferenceExpression: f (OperationKind.LocalReferenceExpression, Type: F) (Syntax: 'f')
                Right: ILiteralExpression (Text: 1) (OperationKind.LiteralExpression, Type: System.Int32, Constant: 1) (Syntax: '1')
  Body: IBlockStatement (0 statements) (OperationKind.BlockStatement) (Syntax: '{ ... }')";
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
IForLoopStatement (LoopKind.For) (OperationKind.LoopStatement) (Syntax: 'for (d.Init ... }')
  Condition: IUnaryOperatorExpression (UnaryOperationKind.DynamicTrue) (OperationKind.UnaryOperatorExpression, Type: System.Boolean) (Syntax: 'd.Done')
      Operand: IDynamicMemberReferenceExpression (Member Name: ""Done"", Containing Type: null) (OperationKind.DynamicMemberReferenceExpression, Type: dynamic) (Syntax: 'd.Done')
          Type Arguments(0)
          Instance Receiver: ILocalReferenceExpression: d (OperationKind.LocalReferenceExpression, Type: dynamic) (Syntax: 'd')
  Before:
      IExpressionStatement (OperationKind.ExpressionStatement) (Syntax: 'd.Initialize(5)')
        Expression: IOperation:  (OperationKind.None) (Syntax: 'd.Initialize(5)')
            Children(2):
                IDynamicMemberReferenceExpression (Member Name: ""Initialize"", Containing Type: null) (OperationKind.DynamicMemberReferenceExpression, Type: dynamic) (Syntax: 'd.Initialize')
                  Type Arguments(0)
                  Instance Receiver: ILocalReferenceExpression: d (OperationKind.LocalReferenceExpression, Type: dynamic) (Syntax: 'd')
                ILiteralExpression (Text: 5) (OperationKind.LiteralExpression, Type: System.Int32, Constant: 5) (Syntax: '5')
  AtLoopBottom:
      IExpressionStatement (OperationKind.ExpressionStatement) (Syntax: 'd.Next()')
        Expression: IOperation:  (OperationKind.None) (Syntax: 'd.Next()')
            Children(1):
                IDynamicMemberReferenceExpression (Member Name: ""Next"", Containing Type: null) (OperationKind.DynamicMemberReferenceExpression, Type: dynamic) (Syntax: 'd.Next')
                  Type Arguments(0)
                  Instance Receiver: ILocalReferenceExpression: d (OperationKind.LocalReferenceExpression, Type: dynamic) (Syntax: 'd')
  Body: IBlockStatement (0 statements) (OperationKind.BlockStatement) (Syntax: '{ ... }')";

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
IForLoopStatement (LoopKind.For) (OperationKind.LoopStatement) (Syntax: 'for (var i  ...  = i + 1) ;')
  Condition: IBinaryOperatorExpression (BinaryOperationKind.IntegerLessThan) (OperationKind.BinaryOperatorExpression, Type: System.Boolean) (Syntax: 'i < 5')
      Left: ILocalReferenceExpression: i (OperationKind.LocalReferenceExpression, Type: System.Int32) (Syntax: 'i')
      Right: ILiteralExpression (Text: 5) (OperationKind.LiteralExpression, Type: System.Int32, Constant: 5) (Syntax: '5')
  Locals: Local_1: System.Int32 i
  Before:
      IVariableDeclarationStatement (1 declarations) (OperationKind.VariableDeclarationStatement) (Syntax: 'i = 1')
        IVariableDeclaration (1 variables) (OperationKind.VariableDeclaration) (Syntax: 'i = 1')
          Variables: Local_1: System.Int32 i
          Initializer: ILiteralExpression (Text: 1) (OperationKind.LiteralExpression, Type: System.Int32, Constant: 1) (Syntax: '1')
  AtLoopBottom:
      IExpressionStatement (OperationKind.ExpressionStatement) (Syntax: 'i = i + 1')
        Expression: ISimpleAssignmentExpression (OperationKind.SimpleAssignmentExpression, Type: System.Int32) (Syntax: 'i = i + 1')
            Left: ILocalReferenceExpression: i (OperationKind.LocalReferenceExpression, Type: System.Int32) (Syntax: 'i')
            Right: IBinaryOperatorExpression (BinaryOperationKind.IntegerAdd) (OperationKind.BinaryOperatorExpression, Type: System.Int32) (Syntax: 'i + 1')
                Left: ILocalReferenceExpression: i (OperationKind.LocalReferenceExpression, Type: System.Int32) (Syntax: 'i')
                Right: ILiteralExpression (Text: 1) (OperationKind.LiteralExpression, Type: System.Int32, Constant: 1) (Syntax: '1')
  Body: IEmptyStatement (OperationKind.EmptyStatement) (Syntax: ';')
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
IForLoopStatement (LoopKind.For) (OperationKind.LoopStatement) (Syntax: 'for (IEnume ... }')
  Condition: null
  Locals: Local_1: System.Collections.Generic.IEnumerable<System.String> str
  Before:
      IVariableDeclarationStatement (1 declarations) (OperationKind.VariableDeclarationStatement) (Syntax: 'str = from  ... select w')
        IVariableDeclaration (1 variables) (OperationKind.VariableDeclaration) (Syntax: 'str = from  ... select w')
          Variables: Local_1: System.Collections.Generic.IEnumerable<System.String> str
          Initializer: IOperation:  (OperationKind.None) (Syntax: 'from x in "" ... select w')
              Children(1):
                  IOperation:  (OperationKind.None) (Syntax: 'into w ... select w')
                    Children(1):
                        IOperation:  (OperationKind.None) (Syntax: 'select w')
                          Children(1):
                              IOperation:  (OperationKind.None) (Syntax: 'select w')
                                Children(1):
                                    IInvocationExpression (System.Collections.Generic.IEnumerable<System.String> System.Linq.Enumerable.Select<System.String, System.String>(this System.Collections.Generic.IEnumerable<System.String> source, System.Func<System.String, System.String> selector)) (OperationKind.InvocationExpression, Type: System.Collections.Generic.IEnumerable<System.String>) (Syntax: 'select w')
                                      Instance Receiver: null
                                      Arguments(2):
                                          IArgument (ArgumentKind.Explicit, Matching Parameter: source) (OperationKind.Argument) (Syntax: 'select z')
                                            IOperation:  (OperationKind.None) (Syntax: 'select z')
                                              Children(1):
                                                  IInvocationExpression (System.Collections.Generic.IEnumerable<System.String> System.Linq.Enumerable.Select<<anonymous type: System.Char x, System.String z>, System.String>(this System.Collections.Generic.IEnumerable<<anonymous type: System.Char x, System.String z>> source, System.Func<<anonymous type: System.Char x, System.String z>, System.String> selector)) (OperationKind.InvocationExpression, Type: System.Collections.Generic.IEnumerable<System.String>) (Syntax: 'select z')
                                                    Instance Receiver: null
                                                    Arguments(2):
                                                        IArgument (ArgumentKind.Explicit, Matching Parameter: source) (OperationKind.Argument) (Syntax: 'let z = x.ToString()')
                                                          IOperation:  (OperationKind.None) (Syntax: 'let z = x.ToString()')
                                                            Children(1):
                                                                IInvocationExpression (System.Collections.Generic.IEnumerable<<anonymous type: System.Char x, System.String z>> System.Linq.Enumerable.Select<System.Char, <anonymous type: System.Char x, System.String z>>(this System.Collections.Generic.IEnumerable<System.Char> source, System.Func<System.Char, <anonymous type: System.Char x, System.String z>> selector)) (OperationKind.InvocationExpression, Type: System.Collections.Generic.IEnumerable<<anonymous type: System.Char x, System.String z>>) (Syntax: 'let z = x.ToString()')
                                                                  Instance Receiver: null
                                                                  Arguments(2):
                                                                      IArgument (ArgumentKind.Explicit, Matching Parameter: source) (OperationKind.Argument) (Syntax: 'from x in ""123""')
                                                                        IConversionExpression (Implicit, TryCast: False, Unchecked) (OperationKind.ConversionExpression, Type: System.Collections.Generic.IEnumerable<System.Char>) (Syntax: 'from x in ""123""')
                                                                          Conversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                                                                          Operand: IOperation:  (OperationKind.None) (Syntax: 'from x in ""123""')
                                                                              Children(1):
                                                                                  ILiteralExpression (OperationKind.LiteralExpression, Type: System.String, Constant: ""123"") (Syntax: '""123""')
                                                                        InConversion: null
                                                                        OutConversion: null
                                                                      IArgument (ArgumentKind.Explicit, Matching Parameter: selector) (OperationKind.Argument) (Syntax: 'x.ToString()')
                                                                        IConversionExpression (Implicit, TryCast: False, Unchecked) (OperationKind.ConversionExpression, Type: System.Func<System.Char, <anonymous type: System.Char x, System.String z>>) (Syntax: 'x.ToString()')
                                                                          Conversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                                                                          Operand: ILambdaExpression (Signature: lambda expression) (OperationKind.LambdaExpression, Type: null) (Syntax: 'x.ToString()')
                                                                              IBlockStatement (1 statements) (OperationKind.BlockStatement) (Syntax: 'x.ToString()')
                                                                                IReturnStatement (OperationKind.ReturnStatement) (Syntax: 'x.ToString()')
                                                                                  ReturnedValue: IObjectCreationExpression (Constructor: <anonymous type: System.Char x, System.String z>..ctor(System.Char x, System.String z)) (OperationKind.ObjectCreationExpression, Type: <anonymous type: System.Char x, System.String z>) (Syntax: 'let z = x.ToString()')
                                                                                      Arguments(2):
                                                                                          IArgument (ArgumentKind.Explicit, Matching Parameter: x) (OperationKind.Argument) (Syntax: 'let z = x.ToString()')
                                                                                            IParameterReferenceExpression: x (OperationKind.ParameterReferenceExpression, Type: System.Char) (Syntax: 'let z = x.ToString()')
                                                                                            InConversion: null
                                                                                            OutConversion: null
                                                                                          IArgument (ArgumentKind.Explicit, Matching Parameter: z) (OperationKind.Argument) (Syntax: 'x.ToString()')
                                                                                            IInvocationExpression (virtual System.String System.Char.ToString()) (OperationKind.InvocationExpression, Type: System.String) (Syntax: 'x.ToString()')
                                                                                              Instance Receiver: IOperation:  (OperationKind.None) (Syntax: 'x')
                                                                                              Arguments(0)
                                                                                            InConversion: null
                                                                                            OutConversion: null
                                                                                      Initializer: null
                                                                        InConversion: null
                                                                        OutConversion: null
                                                          InConversion: null
                                                          OutConversion: null
                                                        IArgument (ArgumentKind.Explicit, Matching Parameter: selector) (OperationKind.Argument) (Syntax: 'z')
                                                          IConversionExpression (Implicit, TryCast: False, Unchecked) (OperationKind.ConversionExpression, Type: System.Func<<anonymous type: System.Char x, System.String z>, System.String>) (Syntax: 'z')
                                                            Conversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                                                            Operand: ILambdaExpression (Signature: lambda expression) (OperationKind.LambdaExpression, Type: null) (Syntax: 'z')
                                                                IBlockStatement (1 statements) (OperationKind.BlockStatement) (Syntax: 'z')
                                                                  IReturnStatement (OperationKind.ReturnStatement) (Syntax: 'z')
                                                                    ReturnedValue: IOperation:  (OperationKind.None) (Syntax: 'z')
                                                          InConversion: null
                                                          OutConversion: null
                                            InConversion: null
                                            OutConversion: null
                                          IArgument (ArgumentKind.Explicit, Matching Parameter: selector) (OperationKind.Argument) (Syntax: 'w')
                                            IConversionExpression (Implicit, TryCast: False, Unchecked) (OperationKind.ConversionExpression, Type: System.Func<System.String, System.String>) (Syntax: 'w')
                                              Conversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                                              Operand: ILambdaExpression (Signature: lambda expression) (OperationKind.LambdaExpression, Type: null) (Syntax: 'w')
                                                  IBlockStatement (1 statements) (OperationKind.BlockStatement) (Syntax: 'w')
                                                    IReturnStatement (OperationKind.ReturnStatement) (Syntax: 'w')
                                                      ReturnedValue: IOperation:  (OperationKind.None) (Syntax: 'w')
                                            InConversion: null
                                            OutConversion: null
  AtLoopBottom(0)
  Body: IBlockStatement (2 statements) (OperationKind.BlockStatement) (Syntax: '{ ... }')
      IForEachLoopStatement (Iteration variable: System.String item) (LoopKind.ForEach) (OperationKind.LoopStatement) (Syntax: 'foreach (va ... }')
        Collection: IConversionExpression (Implicit, TryCast: False, Unchecked) (OperationKind.ConversionExpression, Type: System.Collections.Generic.IEnumerable<System.String>) (Syntax: 'str')
            Conversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
            Operand: ILocalReferenceExpression: str (OperationKind.LocalReferenceExpression, Type: System.Collections.Generic.IEnumerable<System.String>) (Syntax: 'str')
        Body: IBlockStatement (1 statements) (OperationKind.BlockStatement) (Syntax: '{ ... }')
            IExpressionStatement (OperationKind.ExpressionStatement) (Syntax: 'System.Cons ... Line(item);')
              Expression: IInvocationExpression (void System.Console.WriteLine(System.String value)) (OperationKind.InvocationExpression, Type: System.Void) (Syntax: 'System.Cons ... eLine(item)')
                  Instance Receiver: null
                  Arguments(1):
                      IArgument (ArgumentKind.Explicit, Matching Parameter: value) (OperationKind.Argument) (Syntax: 'item')
                        ILocalReferenceExpression: item (OperationKind.LocalReferenceExpression, Type: System.String) (Syntax: 'item')
                        InConversion: null
                        OutConversion: null
      IReturnStatement (OperationKind.ReturnStatement) (Syntax: 'return;')
        ReturnedValue: null
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
IForLoopStatement (LoopKind.For) (OperationKind.LoopStatement) (Syntax: 'for (int i  ... }')
  Condition: IBinaryOperatorExpression (BinaryOperationKind.IntegerLessThan) (OperationKind.BinaryOperatorExpression, Type: System.Boolean) (Syntax: 'i < 5')
      Left: ILocalReferenceExpression: i (OperationKind.LocalReferenceExpression, Type: System.Int32) (Syntax: 'i')
      Right: ILiteralExpression (Text: 5) (OperationKind.LiteralExpression, Type: System.Int32, Constant: 5) (Syntax: '5')
  Locals: Local_1: System.Int32 i
  Before:
      IVariableDeclarationStatement (1 declarations) (OperationKind.VariableDeclarationStatement) (Syntax: 'i = 0')
        IVariableDeclaration (1 variables) (OperationKind.VariableDeclaration) (Syntax: 'i = 0')
          Variables: Local_1: System.Int32 i
          Initializer: ILiteralExpression (Text: 0) (OperationKind.LiteralExpression, Type: System.Int32, Constant: 0) (Syntax: '0')
  AtLoopBottom(0)
  Body: IBlockStatement (1 statements) (OperationKind.BlockStatement) (Syntax: '{ ... }')
      IReturnStatement (OperationKind.ReturnStatement) (Syntax: 'return from ... select w;')
        ReturnedValue: IOperation:  (OperationKind.None) (Syntax: 'from x in "" ... select w')
            Children(1):
                IOperation:  (OperationKind.None) (Syntax: 'into w ... select w')
                  Children(1):
                      IOperation:  (OperationKind.None) (Syntax: 'select w')
                        Children(1):
                            IOperation:  (OperationKind.None) (Syntax: 'select w')
                              Children(1):
                                  IInvocationExpression (System.Collections.Generic.IEnumerable<System.String> System.Linq.Enumerable.Select<System.String, System.String>(this System.Collections.Generic.IEnumerable<System.String> source, System.Func<System.String, System.String> selector)) (OperationKind.InvocationExpression, Type: System.Collections.Generic.IEnumerable<System.String>) (Syntax: 'select w')
                                    Instance Receiver: null
                                    Arguments(2):
                                        IArgument (ArgumentKind.Explicit, Matching Parameter: source) (OperationKind.Argument) (Syntax: 'select z')
                                          IOperation:  (OperationKind.None) (Syntax: 'select z')
                                            Children(1):
                                                IInvocationExpression (System.Collections.Generic.IEnumerable<System.String> System.Linq.Enumerable.Select<<anonymous type: System.Char x, System.String z>, System.String>(this System.Collections.Generic.IEnumerable<<anonymous type: System.Char x, System.String z>> source, System.Func<<anonymous type: System.Char x, System.String z>, System.String> selector)) (OperationKind.InvocationExpression, Type: System.Collections.Generic.IEnumerable<System.String>) (Syntax: 'select z')
                                                  Instance Receiver: null
                                                  Arguments(2):
                                                      IArgument (ArgumentKind.Explicit, Matching Parameter: source) (OperationKind.Argument) (Syntax: 'let z = x.ToString()')
                                                        IOperation:  (OperationKind.None) (Syntax: 'let z = x.ToString()')
                                                          Children(1):
                                                              IInvocationExpression (System.Collections.Generic.IEnumerable<<anonymous type: System.Char x, System.String z>> System.Linq.Enumerable.Select<System.Char, <anonymous type: System.Char x, System.String z>>(this System.Collections.Generic.IEnumerable<System.Char> source, System.Func<System.Char, <anonymous type: System.Char x, System.String z>> selector)) (OperationKind.InvocationExpression, Type: System.Collections.Generic.IEnumerable<<anonymous type: System.Char x, System.String z>>) (Syntax: 'let z = x.ToString()')
                                                                Instance Receiver: null
                                                                Arguments(2):
                                                                    IArgument (ArgumentKind.Explicit, Matching Parameter: source) (OperationKind.Argument) (Syntax: 'from x in ""123""')
                                                                      IConversionExpression (Implicit, TryCast: False, Unchecked) (OperationKind.ConversionExpression, Type: System.Collections.Generic.IEnumerable<System.Char>) (Syntax: 'from x in ""123""')
                                                                        Conversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                                                                        Operand: IOperation:  (OperationKind.None) (Syntax: 'from x in ""123""')
                                                                            Children(1):
                                                                                ILiteralExpression (OperationKind.LiteralExpression, Type: System.String, Constant: ""123"") (Syntax: '""123""')
                                                                      InConversion: null
                                                                      OutConversion: null
                                                                    IArgument (ArgumentKind.Explicit, Matching Parameter: selector) (OperationKind.Argument) (Syntax: 'x.ToString()')
                                                                      IConversionExpression (Implicit, TryCast: False, Unchecked) (OperationKind.ConversionExpression, Type: System.Func<System.Char, <anonymous type: System.Char x, System.String z>>) (Syntax: 'x.ToString()')
                                                                        Conversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                                                                        Operand: ILambdaExpression (Signature: lambda expression) (OperationKind.LambdaExpression, Type: null) (Syntax: 'x.ToString()')
                                                                            IBlockStatement (1 statements) (OperationKind.BlockStatement) (Syntax: 'x.ToString()')
                                                                              IReturnStatement (OperationKind.ReturnStatement) (Syntax: 'x.ToString()')
                                                                                ReturnedValue: IObjectCreationExpression (Constructor: <anonymous type: System.Char x, System.String z>..ctor(System.Char x, System.String z)) (OperationKind.ObjectCreationExpression, Type: <anonymous type: System.Char x, System.String z>) (Syntax: 'let z = x.ToString()')
                                                                                    Arguments(2):
                                                                                        IArgument (ArgumentKind.Explicit, Matching Parameter: x) (OperationKind.Argument) (Syntax: 'let z = x.ToString()')
                                                                                          IParameterReferenceExpression: x (OperationKind.ParameterReferenceExpression, Type: System.Char) (Syntax: 'let z = x.ToString()')
                                                                                          InConversion: null
                                                                                          OutConversion: null
                                                                                        IArgument (ArgumentKind.Explicit, Matching Parameter: z) (OperationKind.Argument) (Syntax: 'x.ToString()')
                                                                                          IInvocationExpression (virtual System.String System.Char.ToString()) (OperationKind.InvocationExpression, Type: System.String) (Syntax: 'x.ToString()')
                                                                                            Instance Receiver: IOperation:  (OperationKind.None) (Syntax: 'x')
                                                                                            Arguments(0)
                                                                                          InConversion: null
                                                                                          OutConversion: null
                                                                                    Initializer: null
                                                                      InConversion: null
                                                                      OutConversion: null
                                                        InConversion: null
                                                        OutConversion: null
                                                      IArgument (ArgumentKind.Explicit, Matching Parameter: selector) (OperationKind.Argument) (Syntax: 'z')
                                                        IConversionExpression (Implicit, TryCast: False, Unchecked) (OperationKind.ConversionExpression, Type: System.Func<<anonymous type: System.Char x, System.String z>, System.String>) (Syntax: 'z')
                                                          Conversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                                                          Operand: ILambdaExpression (Signature: lambda expression) (OperationKind.LambdaExpression, Type: null) (Syntax: 'z')
                                                              IBlockStatement (1 statements) (OperationKind.BlockStatement) (Syntax: 'z')
                                                                IReturnStatement (OperationKind.ReturnStatement) (Syntax: 'z')
                                                                  ReturnedValue: IOperation:  (OperationKind.None) (Syntax: 'z')
                                                        InConversion: null
                                                        OutConversion: null
                                          InConversion: null
                                          OutConversion: null
                                        IArgument (ArgumentKind.Explicit, Matching Parameter: selector) (OperationKind.Argument) (Syntax: 'w')
                                          IConversionExpression (Implicit, TryCast: False, Unchecked) (OperationKind.ConversionExpression, Type: System.Func<System.String, System.String>) (Syntax: 'w')
                                            Conversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                                            Operand: ILambdaExpression (Signature: lambda expression) (OperationKind.LambdaExpression, Type: null) (Syntax: 'w')
                                                IBlockStatement (1 statements) (OperationKind.BlockStatement) (Syntax: 'w')
                                                  IReturnStatement (OperationKind.ReturnStatement) (Syntax: 'w')
                                                    ReturnedValue: IOperation:  (OperationKind.None) (Syntax: 'w')
                                          InConversion: null
                                          OutConversion: null
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
IForLoopStatement (LoopKind.For) (OperationKind.LoopStatement) (Syntax: 'for (e = x  ... }')
  Condition: IBinaryOperatorExpression (BinaryOperationKind.IntegerLessThan) (OperationKind.BinaryOperatorExpression, Type: System.Boolean) (Syntax: 'i < 5')
      Left: ILocalReferenceExpression: i (OperationKind.LocalReferenceExpression, Type: System.Int32) (Syntax: 'i')
      Right: ILiteralExpression (Text: 5) (OperationKind.LiteralExpression, Type: System.Int32, Constant: 5) (Syntax: '5')
  Before:
      IExpressionStatement (OperationKind.ExpressionStatement) (Syntax: 'e = x => x * x')
        Expression: ISimpleAssignmentExpression (OperationKind.SimpleAssignmentExpression, Type: System.Linq.Expressions.Expression<System.Func<System.Int32, System.Int32>>) (Syntax: 'e = x => x * x')
            Left: ILocalReferenceExpression: e (OperationKind.LocalReferenceExpression, Type: System.Linq.Expressions.Expression<System.Func<System.Int32, System.Int32>>) (Syntax: 'e')
            Right: IConversionExpression (Implicit, TryCast: False, Unchecked) (OperationKind.ConversionExpression, Type: System.Linq.Expressions.Expression<System.Func<System.Int32, System.Int32>>) (Syntax: 'x => x * x')
                Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                Operand: ILambdaExpression (Signature: lambda expression) (OperationKind.LambdaExpression, Type: null) (Syntax: 'x => x * x')
                    IBlockStatement (1 statements) (OperationKind.BlockStatement) (Syntax: 'x * x')
                      IReturnStatement (OperationKind.ReturnStatement) (Syntax: 'x * x')
                        ReturnedValue: IBinaryOperatorExpression (BinaryOperationKind.IntegerMultiply) (OperationKind.BinaryOperatorExpression, Type: System.Int32) (Syntax: 'x * x')
                            Left: IParameterReferenceExpression: x (OperationKind.ParameterReferenceExpression, Type: System.Int32) (Syntax: 'x')
                            Right: IParameterReferenceExpression: x (OperationKind.ParameterReferenceExpression, Type: System.Int32) (Syntax: 'x')
  AtLoopBottom:
      IExpressionStatement (OperationKind.ExpressionStatement) (Syntax: 'i++')
        Expression: IIncrementExpression (UnaryOperandKind.IntegerPostfixIncrement) (OperationKind.IncrementExpression, Type: System.Int32) (Syntax: 'i++')
            Left: ILocalReferenceExpression: i (OperationKind.LocalReferenceExpression, Type: System.Int32) (Syntax: 'i')
  Body: IBlockStatement (2 statements, 1 locals) (OperationKind.BlockStatement) (Syntax: '{ ... }')
      Locals: Local_1: System.Func<System.Int32, System.Int32> lambda
      IVariableDeclarationStatement (1 declarations) (OperationKind.VariableDeclarationStatement) (Syntax: 'var lambda  ... .Compile();')
        IVariableDeclaration (1 variables) (OperationKind.VariableDeclaration) (Syntax: 'var lambda  ... .Compile();')
          Variables: Local_1: System.Func<System.Int32, System.Int32> lambda
          Initializer: IInvocationExpression ( System.Func<System.Int32, System.Int32> System.Linq.Expressions.Expression<System.Func<System.Int32, System.Int32>>.Compile()) (OperationKind.InvocationExpression, Type: System.Func<System.Int32, System.Int32>) (Syntax: 'e.Compile()')
              Instance Receiver: ILocalReferenceExpression: e (OperationKind.LocalReferenceExpression, Type: System.Linq.Expressions.Expression<System.Func<System.Int32, System.Int32>>) (Syntax: 'e')
              Arguments(0)
      IExpressionStatement (OperationKind.ExpressionStatement) (Syntax: 'System.Cons ... lambda(i));')
        Expression: IInvocationExpression (void System.Console.WriteLine(System.Int32 value)) (OperationKind.InvocationExpression, Type: System.Void) (Syntax: 'System.Cons ... (lambda(i))')
            Instance Receiver: null
            Arguments(1):
                IArgument (ArgumentKind.Explicit, Matching Parameter: value) (OperationKind.Argument) (Syntax: 'lambda(i)')
                  IInvocationExpression (virtual System.Int32 System.Func<System.Int32, System.Int32>.Invoke(System.Int32 arg)) (OperationKind.InvocationExpression, Type: System.Int32) (Syntax: 'lambda(i)')
                    Instance Receiver: ILocalReferenceExpression: lambda (OperationKind.LocalReferenceExpression, Type: System.Func<System.Int32, System.Int32>) (Syntax: 'lambda')
                    Arguments(1):
                        IArgument (ArgumentKind.Explicit, Matching Parameter: arg) (OperationKind.Argument) (Syntax: 'i')
                          ILocalReferenceExpression: i (OperationKind.LocalReferenceExpression, Type: System.Int32) (Syntax: 'i')
                          InConversion: null
                          OutConversion: null
                  InConversion: null
                  OutConversion: null
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
IForLoopStatement (LoopKind.For) (OperationKind.LoopStatement) (Syntax: 'for (int i  ... }')
  Condition: IBinaryOperatorExpression (BinaryOperationKind.IntegerLessThan) (OperationKind.BinaryOperatorExpression, Type: System.Boolean) (Syntax: 'i < 5')
      Left: ILocalReferenceExpression: i (OperationKind.LocalReferenceExpression, Type: System.Int32) (Syntax: 'i')
      Right: ILiteralExpression (Text: 5) (OperationKind.LiteralExpression, Type: System.Int32, Constant: 5) (Syntax: '5')
  Locals: Local_1: System.Int32 i
  Before:
      IVariableDeclarationStatement (1 declarations) (OperationKind.VariableDeclarationStatement) (Syntax: 'i = 1')
        IVariableDeclaration (1 variables) (OperationKind.VariableDeclaration) (Syntax: 'i = 1')
          Variables: Local_1: System.Int32 i
          Initializer: ILiteralExpression (Text: 1) (OperationKind.LiteralExpression, Type: System.Int32, Constant: 1) (Syntax: '1')
  AtLoopBottom:
      IExpressionStatement (OperationKind.ExpressionStatement) (Syntax: 'e = x => x * x')
        Expression: ISimpleAssignmentExpression (OperationKind.SimpleAssignmentExpression, Type: System.Linq.Expressions.Expression<System.Func<System.Int32, System.Int32>>) (Syntax: 'e = x => x * x')
            Left: ILocalReferenceExpression: e (OperationKind.LocalReferenceExpression, Type: System.Linq.Expressions.Expression<System.Func<System.Int32, System.Int32>>) (Syntax: 'e')
            Right: IConversionExpression (Implicit, TryCast: False, Unchecked) (OperationKind.ConversionExpression, Type: System.Linq.Expressions.Expression<System.Func<System.Int32, System.Int32>>) (Syntax: 'x => x * x')
                Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                Operand: ILambdaExpression (Signature: lambda expression) (OperationKind.LambdaExpression, Type: null) (Syntax: 'x => x * x')
                    IBlockStatement (1 statements) (OperationKind.BlockStatement) (Syntax: 'x * x')
                      IReturnStatement (OperationKind.ReturnStatement) (Syntax: 'x * x')
                        ReturnedValue: IBinaryOperatorExpression (BinaryOperationKind.IntegerMultiply) (OperationKind.BinaryOperatorExpression, Type: System.Int32) (Syntax: 'x * x')
                            Left: IParameterReferenceExpression: x (OperationKind.ParameterReferenceExpression, Type: System.Int32) (Syntax: 'x')
                            Right: IParameterReferenceExpression: x (OperationKind.ParameterReferenceExpression, Type: System.Int32) (Syntax: 'x')
      IExpressionStatement (OperationKind.ExpressionStatement) (Syntax: 'i = i + 1')
        Expression: ISimpleAssignmentExpression (OperationKind.SimpleAssignmentExpression, Type: System.Int32) (Syntax: 'i = i + 1')
            Left: ILocalReferenceExpression: i (OperationKind.LocalReferenceExpression, Type: System.Int32) (Syntax: 'i')
            Right: IBinaryOperatorExpression (BinaryOperationKind.IntegerAdd) (OperationKind.BinaryOperatorExpression, Type: System.Int32) (Syntax: 'i + 1')
                Left: ILocalReferenceExpression: i (OperationKind.LocalReferenceExpression, Type: System.Int32) (Syntax: 'i')
                Right: ILiteralExpression (Text: 1) (OperationKind.LiteralExpression, Type: System.Int32, Constant: 1) (Syntax: '1')
  Body: IBlockStatement (2 statements, 1 locals) (OperationKind.BlockStatement) (Syntax: '{ ... }')
      Locals: Local_1: System.Func<System.Int32, System.Int32> lambda
      IVariableDeclarationStatement (1 declarations) (OperationKind.VariableDeclarationStatement) (Syntax: 'var lambda  ... .Compile();')
        IVariableDeclaration (1 variables) (OperationKind.VariableDeclaration) (Syntax: 'var lambda  ... .Compile();')
          Variables: Local_1: System.Func<System.Int32, System.Int32> lambda
          Initializer: IInvocationExpression ( System.Func<System.Int32, System.Int32> System.Linq.Expressions.Expression<System.Func<System.Int32, System.Int32>>.Compile()) (OperationKind.InvocationExpression, Type: System.Func<System.Int32, System.Int32>) (Syntax: 'e.Compile()')
              Instance Receiver: ILocalReferenceExpression: e (OperationKind.LocalReferenceExpression, Type: System.Linq.Expressions.Expression<System.Func<System.Int32, System.Int32>>) (Syntax: 'e')
              Arguments(0)
      IExpressionStatement (OperationKind.ExpressionStatement) (Syntax: 'System.Cons ... lambda(i));')
        Expression: IInvocationExpression (void System.Console.WriteLine(System.Int32 value)) (OperationKind.InvocationExpression, Type: System.Void) (Syntax: 'System.Cons ... (lambda(i))')
            Instance Receiver: null
            Arguments(1):
                IArgument (ArgumentKind.Explicit, Matching Parameter: value) (OperationKind.Argument) (Syntax: 'lambda(i)')
                  IInvocationExpression (virtual System.Int32 System.Func<System.Int32, System.Int32>.Invoke(System.Int32 arg)) (OperationKind.InvocationExpression, Type: System.Int32) (Syntax: 'lambda(i)')
                    Instance Receiver: ILocalReferenceExpression: lambda (OperationKind.LocalReferenceExpression, Type: System.Func<System.Int32, System.Int32>) (Syntax: 'lambda')
                    Arguments(1):
                        IArgument (ArgumentKind.Explicit, Matching Parameter: arg) (OperationKind.Argument) (Syntax: 'i')
                          ILocalReferenceExpression: i (OperationKind.LocalReferenceExpression, Type: System.Int32) (Syntax: 'i')
                          InConversion: null
                          OutConversion: null
                  InConversion: null
                  OutConversion: null
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
IForLoopStatement (LoopKind.For) (OperationKind.LoopStatement) (Syntax: 'for (C1 i = ... l; i++) { }')
  Condition: IBinaryOperatorExpression (BinaryOperationKind.ObjectEquals) (OperationKind.BinaryOperatorExpression, Type: System.Boolean) (Syntax: 'i == null')
      Left: IConversionExpression (Implicit, TryCast: False, Unchecked) (OperationKind.ConversionExpression, Type: System.Object) (Syntax: 'i')
          Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: True, IsUserDefined: False) (MethodSymbol: null)
          Operand: ILocalReferenceExpression: i (OperationKind.LocalReferenceExpression, Type: C1) (Syntax: 'i')
      Right: IConversionExpression (Implicit, TryCast: False, Unchecked) (OperationKind.ConversionExpression, Type: System.Object, Constant: null) (Syntax: 'null')
          Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: True, IsUserDefined: False) (MethodSymbol: null)
          Operand: ILiteralExpression (Text: null) (OperationKind.LiteralExpression, Type: null, Constant: null) (Syntax: 'null')
  Locals: Local_1: C1 i
  Before:
      IVariableDeclarationStatement (1 declarations) (OperationKind.VariableDeclarationStatement) (Syntax: 'i = new C1()')
        IVariableDeclaration (1 variables) (OperationKind.VariableDeclaration) (Syntax: 'i = new C1()')
          Variables: Local_1: C1 i
          Initializer: IObjectCreationExpression (Constructor: C1..ctor()) (OperationKind.ObjectCreationExpression, Type: C1) (Syntax: 'new C1()')
              Arguments(0)
              Initializer: null
  AtLoopBottom:
      IExpressionStatement (OperationKind.ExpressionStatement) (Syntax: 'i++')
        Expression: IIncrementExpression (UnaryOperandKind.OperatorMethodPostfixIncrement) (OperatorMethod: C1 C1.op_Increment(C1 obj)) (OperationKind.IncrementExpression, Type: C1) (Syntax: 'i++')
            Left: ILocalReferenceExpression: i (OperationKind.LocalReferenceExpression, Type: C1) (Syntax: 'i')
  Body: IBlockStatement (0 statements) (OperationKind.BlockStatement) (Syntax: '{ }')
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
IForLoopStatement (LoopKind.For) (OperationKind.LoopStatement) (Syntax: 'for (int j  ... }')
  Condition: IBinaryOperatorExpression (BinaryOperationKind.IntegerLessThan) (OperationKind.BinaryOperatorExpression, Type: System.Boolean) (Syntax: 'j < 5')
      Left: ILocalReferenceExpression: j (OperationKind.LocalReferenceExpression, Type: System.Int32) (Syntax: 'j')
      Right: ILiteralExpression (Text: 5) (OperationKind.LiteralExpression, Type: System.Int32, Constant: 5) (Syntax: '5')
  Locals: Local_1: System.Int32 j
  Before:
      IVariableDeclarationStatement (1 declarations) (OperationKind.VariableDeclarationStatement) (Syntax: 'j = i++')
        IVariableDeclaration (1 variables) (OperationKind.VariableDeclaration) (Syntax: 'j = i++')
          Variables: Local_1: System.Int32 j
          Initializer: IIncrementExpression (UnaryOperandKind.IntegerPostfixIncrement) (OperationKind.IncrementExpression, Type: System.Int32) (Syntax: 'i++')
              Left: ILocalReferenceExpression: i (OperationKind.LocalReferenceExpression, Type: System.Int32) (Syntax: 'i')
  AtLoopBottom:
      IExpressionStatement (OperationKind.ExpressionStatement) (Syntax: '++j')
        Expression: IIncrementExpression (UnaryOperandKind.IntegerPrefixIncrement) (OperationKind.IncrementExpression, Type: System.Int32) (Syntax: '++j')
            Left: ILocalReferenceExpression: j (OperationKind.LocalReferenceExpression, Type: System.Int32) (Syntax: 'j')
  Body: IBlockStatement (1 statements) (OperationKind.BlockStatement) (Syntax: '{ ... }')
      IExpressionStatement (OperationKind.ExpressionStatement) (Syntax: 'System.Cons ... iteLine(j);')
        Expression: IInvocationExpression (void System.Console.WriteLine(System.Int32 value)) (OperationKind.InvocationExpression, Type: System.Void) (Syntax: 'System.Cons ... riteLine(j)')
            Instance Receiver: null
            Arguments(1):
                IArgument (ArgumentKind.Explicit, Matching Parameter: value) (OperationKind.Argument) (Syntax: 'j')
                  ILocalReferenceExpression: j (OperationKind.LocalReferenceExpression, Type: System.Int32) (Syntax: 'j')
                  InConversion: null
                  OutConversion: null
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
IForLoopStatement (LoopKind.For) (OperationKind.LoopStatement) (Syntax: 'for (int j  ... }')
  Condition: IBinaryOperatorExpression (BinaryOperationKind.IntegerLessThan) (OperationKind.BinaryOperatorExpression, Type: System.Boolean) (Syntax: 'j < 5')
      Left: ILocalReferenceExpression: j (OperationKind.LocalReferenceExpression, Type: System.Int32) (Syntax: 'j')
      Right: ILiteralExpression (Text: 5) (OperationKind.LiteralExpression, Type: System.Int32, Constant: 5) (Syntax: '5')
  Locals: Local_1: System.Int32 j
  Before:
      IVariableDeclarationStatement (1 declarations) (OperationKind.VariableDeclarationStatement) (Syntax: 'j = ++i')
        IVariableDeclaration (1 variables) (OperationKind.VariableDeclaration) (Syntax: 'j = ++i')
          Variables: Local_1: System.Int32 j
          Initializer: IIncrementExpression (UnaryOperandKind.IntegerPrefixIncrement) (OperationKind.IncrementExpression, Type: System.Int32) (Syntax: '++i')
              Left: ILocalReferenceExpression: i (OperationKind.LocalReferenceExpression, Type: System.Int32) (Syntax: 'i')
  AtLoopBottom:
      IExpressionStatement (OperationKind.ExpressionStatement) (Syntax: '++j')
        Expression: IIncrementExpression (UnaryOperandKind.IntegerPrefixIncrement) (OperationKind.IncrementExpression, Type: System.Int32) (Syntax: '++j')
            Left: ILocalReferenceExpression: j (OperationKind.LocalReferenceExpression, Type: System.Int32) (Syntax: 'j')
  Body: IBlockStatement (1 statements) (OperationKind.BlockStatement) (Syntax: '{ ... }')
      IExpressionStatement (OperationKind.ExpressionStatement) (Syntax: 'System.Cons ... iteLine(j);')
        Expression: IInvocationExpression (void System.Console.WriteLine(System.Int32 value)) (OperationKind.InvocationExpression, Type: System.Void) (Syntax: 'System.Cons ... riteLine(j)')
            Instance Receiver: null
            Arguments(1):
                IArgument (ArgumentKind.Explicit, Matching Parameter: value) (OperationKind.Argument) (Syntax: 'j')
                  ILocalReferenceExpression: j (OperationKind.LocalReferenceExpression, Type: System.Int32) (Syntax: 'j')
                  InConversion: null
                  OutConversion: null
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
IForLoopStatement (LoopKind.For) (OperationKind.LoopStatement) (Syntax: 'for (int i  ... }')
  Condition: IBinaryOperatorExpression (BinaryOperationKind.IntegerLessThan) (OperationKind.BinaryOperatorExpression, Type: System.Boolean) (Syntax: '++i < 5')
      Left: IIncrementExpression (UnaryOperandKind.IntegerPrefixIncrement) (OperationKind.IncrementExpression, Type: System.Int32) (Syntax: '++i')
          Left: ILocalReferenceExpression: i (OperationKind.LocalReferenceExpression, Type: System.Int32) (Syntax: 'i')
      Right: ILiteralExpression (Text: 5) (OperationKind.LiteralExpression, Type: System.Int32, Constant: 5) (Syntax: '5')
  Locals: Local_1: System.Int32 i
  Before:
      IVariableDeclarationStatement (1 declarations) (OperationKind.VariableDeclarationStatement) (Syntax: 'i = 0')
        IVariableDeclaration (1 variables) (OperationKind.VariableDeclaration) (Syntax: 'i = 0')
          Variables: Local_1: System.Int32 i
          Initializer: ILiteralExpression (Text: 0) (OperationKind.LiteralExpression, Type: System.Int32, Constant: 0) (Syntax: '0')
  AtLoopBottom(0)
  Body: IBlockStatement (1 statements) (OperationKind.BlockStatement) (Syntax: '{ ... }')
      IExpressionStatement (OperationKind.ExpressionStatement) (Syntax: 'System.Cons ... iteLine(i);')
        Expression: IInvocationExpression (void System.Console.WriteLine(System.Int32 value)) (OperationKind.InvocationExpression, Type: System.Void) (Syntax: 'System.Cons ... riteLine(i)')
            Instance Receiver: null
            Arguments(1):
                IArgument (ArgumentKind.Explicit, Matching Parameter: value) (OperationKind.Argument) (Syntax: 'i')
                  ILocalReferenceExpression: i (OperationKind.LocalReferenceExpression, Type: System.Int32) (Syntax: 'i')
                  InConversion: null
                  OutConversion: null
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
IForLoopStatement (LoopKind.For) (OperationKind.LoopStatement) (Syntax: 'for (int i  ... }')
  Condition: IBinaryOperatorExpression (BinaryOperationKind.IntegerGreaterThan) (OperationKind.BinaryOperatorExpression, Type: System.Boolean) (Syntax: 'foo(i--) > -5')
      Left: IInvocationExpression (System.Int32 Program.foo(System.Int32 x)) (OperationKind.InvocationExpression, Type: System.Int32) (Syntax: 'foo(i--)')
          Instance Receiver: null
          Arguments(1):
              IArgument (ArgumentKind.Explicit, Matching Parameter: x) (OperationKind.Argument) (Syntax: 'i--')
                IIncrementExpression (UnaryOperandKind.IntegerPostfixDecrement) (OperationKind.IncrementExpression, Type: System.Int32) (Syntax: 'i--')
                  Left: ILocalReferenceExpression: i (OperationKind.LocalReferenceExpression, Type: System.Int32) (Syntax: 'i')
                InConversion: null
                OutConversion: null
      Right: IUnaryOperatorExpression (UnaryOperationKind.IntegerMinus) (OperationKind.UnaryOperatorExpression, Type: System.Int32, Constant: -5) (Syntax: '-5')
          Operand: ILiteralExpression (Text: 5) (OperationKind.LiteralExpression, Type: System.Int32, Constant: 5) (Syntax: '5')
  Locals: Local_1: System.Int32 i
  Before:
      IVariableDeclarationStatement (1 declarations) (OperationKind.VariableDeclarationStatement) (Syntax: 'i = 0')
        IVariableDeclaration (1 variables) (OperationKind.VariableDeclaration) (Syntax: 'i = 0')
          Variables: Local_1: System.Int32 i
          Initializer: ILiteralExpression (Text: 0) (OperationKind.LiteralExpression, Type: System.Int32, Constant: 0) (Syntax: '0')
  AtLoopBottom(0)
  Body: IBlockStatement (1 statements) (OperationKind.BlockStatement) (Syntax: '{ ... }')
      IExpressionStatement (OperationKind.ExpressionStatement) (Syntax: 'System.Cons ... iteLine(i);')
        Expression: IInvocationExpression (void System.Console.WriteLine(System.Int32 value)) (OperationKind.InvocationExpression, Type: System.Void) (Syntax: 'System.Cons ... riteLine(i)')
            Instance Receiver: null
            Arguments(1):
                IArgument (ArgumentKind.Explicit, Matching Parameter: value) (OperationKind.Argument) (Syntax: 'i')
                  ILocalReferenceExpression: i (OperationKind.LocalReferenceExpression, Type: System.Int32) (Syntax: 'i')
                  InConversion: null
                  OutConversion: null
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
IForLoopStatement (LoopKind.For) (OperationKind.LoopStatement) (Syntax: 'for (; true ... }')
  Condition: ILiteralExpression (OperationKind.LiteralExpression, Type: System.Boolean, Constant: True) (Syntax: 'true')
  Before(0)
  AtLoopBottom(0)
  Body: IBlockStatement (1 statements) (OperationKind.BlockStatement) (Syntax: '{ ... }')
      IExpressionStatement (OperationKind.ExpressionStatement) (Syntax: 'System.Cons ... eLine(""z"");')
        Expression: IInvocationExpression (void System.Console.WriteLine(System.String value)) (OperationKind.InvocationExpression, Type: System.Void) (Syntax: 'System.Cons ... teLine(""z"")')
            Instance Receiver: null
            Arguments(1):
                IArgument (ArgumentKind.Explicit, Matching Parameter: value) (OperationKind.Argument) (Syntax: '""z""')
                  ILiteralExpression (OperationKind.LiteralExpression, Type: System.String, Constant: ""z"") (Syntax: '""z""')
                  InConversion: null
                  OutConversion: null
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
IForLoopStatement (LoopKind.For) (OperationKind.LoopStatement, IsInvalid) (Syntax: 'for (int k  ... 100, j > 5;')
  Condition: IBinaryOperatorExpression (BinaryOperationKind.IntegerLessThan) (OperationKind.BinaryOperatorExpression, Type: System.Boolean, IsInvalid) (Syntax: 'k < 100')
      Left: ILocalReferenceExpression: k (OperationKind.LocalReferenceExpression, Type: System.Int32) (Syntax: 'k')
      Right: ILiteralExpression (Text: 100) (OperationKind.LiteralExpression, Type: System.Int32, Constant: 100, IsInvalid) (Syntax: '100')
  Locals: Local_1: System.Int32 k
    Local_2: System.Int32 j
  Before:
      IVariableDeclarationStatement (2 declarations) (OperationKind.VariableDeclarationStatement) (Syntax: 'int k = 0, j = 0')
        IVariableDeclaration (1 variables) (OperationKind.VariableDeclaration) (Syntax: 'k = 0')
          Variables: Local_1: System.Int32 k
          Initializer: ILiteralExpression (Text: 0) (OperationKind.LiteralExpression, Type: System.Int32, Constant: 0) (Syntax: '0')
        IVariableDeclaration (1 variables) (OperationKind.VariableDeclaration) (Syntax: 'j = 0')
          Variables: Local_1: System.Int32 j
          Initializer: ILiteralExpression (Text: 0) (OperationKind.LiteralExpression, Type: System.Int32, Constant: 0) (Syntax: '0')
  AtLoopBottom:
      IExpressionStatement (OperationKind.ExpressionStatement, IsInvalid) (Syntax: '')
        Expression: IInvalidExpression (OperationKind.InvalidExpression, Type: ?, IsInvalid) (Syntax: '')
            Children(0)
      IExpressionStatement (OperationKind.ExpressionStatement, IsInvalid) (Syntax: 'j > 5')
        Expression: IBinaryOperatorExpression (BinaryOperationKind.IntegerGreaterThan) (OperationKind.BinaryOperatorExpression, Type: System.Boolean, IsInvalid) (Syntax: 'j > 5')
            Left: ILocalReferenceExpression: j (OperationKind.LocalReferenceExpression, Type: System.Int32, IsInvalid) (Syntax: 'j')
            Right: ILiteralExpression (Text: 5) (OperationKind.LiteralExpression, Type: System.Int32, Constant: 5, IsInvalid) (Syntax: '5')
  Body: IEmptyStatement (OperationKind.EmptyStatement, IsInvalid) (Syntax: ';')
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
IForLoopStatement (LoopKind.For) (OperationKind.LoopStatement) (Syntax: 'for (var j  ... }')
  Condition: IBinaryOperatorExpression (BinaryOperationKind.IntegerLessThan) (OperationKind.BinaryOperatorExpression, Type: System.Boolean) (Syntax: 'i < 10')
      Left: ILocalReferenceExpression: i (OperationKind.LocalReferenceExpression, Type: System.Int32) (Syntax: 'i')
      Right: ILiteralExpression (Text: 10) (OperationKind.LiteralExpression, Type: System.Int32, Constant: 10) (Syntax: '10')
  Locals: Local_1: System.Int32 j
    Local_2: System.Int32 i
  Before:
      IVariableDeclarationStatement (1 declarations) (OperationKind.VariableDeclarationStatement) (Syntax: 'j = int.Try ...  i) ? i : 0')
        IVariableDeclaration (1 variables) (OperationKind.VariableDeclaration) (Syntax: 'j = int.Try ...  i) ? i : 0')
          Variables: Local_1: System.Int32 j
          Initializer: IConditionalChoiceExpression (OperationKind.ConditionalChoiceExpression, Type: System.Int32) (Syntax: 'int.TryPars ...  i) ? i : 0')
              Condition: IInvocationExpression (System.Boolean System.Int32.TryParse(System.String s, out System.Int32 result)) (OperationKind.InvocationExpression, Type: System.Boolean) (Syntax: 'int.TryPars ...  out var i)')
                  Instance Receiver: null
                  Arguments(2):
                      IArgument (ArgumentKind.Explicit, Matching Parameter: s) (OperationKind.Argument) (Syntax: 's')
                        ILocalReferenceExpression: s (OperationKind.LocalReferenceExpression, Type: System.String) (Syntax: 's')
                        InConversion: null
                        OutConversion: null
                      IArgument (ArgumentKind.Explicit, Matching Parameter: result) (OperationKind.Argument) (Syntax: 'var i')
                        ILocalReferenceExpression: i (OperationKind.LocalReferenceExpression, Type: System.Int32) (Syntax: 'var i')
                        InConversion: null
                        OutConversion: null
              IfTrue: ILocalReferenceExpression: i (OperationKind.LocalReferenceExpression, Type: System.Int32) (Syntax: 'i')
              IfFalse: ILiteralExpression (Text: 0) (OperationKind.LiteralExpression, Type: System.Int32, Constant: 0) (Syntax: '0')
  AtLoopBottom:
      IExpressionStatement (OperationKind.ExpressionStatement) (Syntax: 'i++')
        Expression: IIncrementExpression (UnaryOperandKind.IntegerPostfixIncrement) (OperationKind.IncrementExpression, Type: System.Int32) (Syntax: 'i++')
            Left: ILocalReferenceExpression: i (OperationKind.LocalReferenceExpression, Type: System.Int32) (Syntax: 'i')
  Body: IBlockStatement (1 statements) (OperationKind.BlockStatement) (Syntax: '{ ... }')
      IExpressionStatement (OperationKind.ExpressionStatement) (Syntax: 'System.Cons ... }, s={s}"");')
        Expression: IInvocationExpression (void System.Console.WriteLine(System.String value)) (OperationKind.InvocationExpression, Type: System.Void) (Syntax: 'System.Cons ... i}, s={s}"")')
            Instance Receiver: null
            Arguments(1):
                IArgument (ArgumentKind.Explicit, Matching Parameter: value) (OperationKind.Argument) (Syntax: '$""i={i}, s={s}""')
                  IInterpolatedStringExpression (OperationKind.InterpolatedStringExpression, Type: System.String) (Syntax: '$""i={i}, s={s}""')
                    Parts(4):
                        IInterpolatedStringText (OperationKind.InterpolatedStringText) (Syntax: 'i=')
                          Text: ILiteralExpression (Text: i=) (OperationKind.LiteralExpression, Type: System.String, Constant: ""i="") (Syntax: 'i=')
                        IInterpolation (OperationKind.Interpolation) (Syntax: '{i}')
                          Expression: ILocalReferenceExpression: i (OperationKind.LocalReferenceExpression, Type: System.Int32) (Syntax: 'i')
                          Alignment: null
                          FormatString: null
                        IInterpolatedStringText (OperationKind.InterpolatedStringText) (Syntax: ', s=')
                          Text: ILiteralExpression (Text: , s=) (OperationKind.LiteralExpression, Type: System.String, Constant: "", s="") (Syntax: ', s=')
                        IInterpolation (OperationKind.Interpolation) (Syntax: '{s}')
                          Expression: ILocalReferenceExpression: s (OperationKind.LocalReferenceExpression, Type: System.String) (Syntax: 's')
                          Alignment: null
                          FormatString: null
                  InConversion: null
                  OutConversion: null
";
            VerifyOperationTreeForTest<ForStatementSyntax>(source, expectedOperationTree);
        }

    }
}
