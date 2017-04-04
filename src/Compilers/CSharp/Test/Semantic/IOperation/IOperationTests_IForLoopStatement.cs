// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis.CSharp.Syntax;
using Roslyn.Test.Utilities;
using Xunit;


namespace Microsoft.CodeAnalysis.CSharp.UnitTests
{
    public partial class IOperationTests : SemanticModelTestBase
    {
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
IForLoopStatement (LoopKind.For) (OperationKind.LoopStatement)
  Condition: IBinaryOperatorExpression (BinaryOperationKind.IntegerLessThan) (OperationKind.BinaryOperatorExpression, Type: System.Boolean)
      Left: ILocalReferenceExpression: i (OperationKind.LocalReferenceExpression, Type: System.Int32)
      Right: ILiteralExpression (Text: 3) (OperationKind.LiteralExpression, Type: System.Int32, Constant: 3)
  Local_1: System.Int32 i
  Before: IVariableDeclarationStatement (1 variables) (OperationKind.VariableDeclarationStatement)
      IVariableDeclaration: System.Int32 i (OperationKind.VariableDeclaration)
        Initializer: ILiteralExpression (Text: 0) (OperationKind.LiteralExpression, Type: System.Int32, Constant: 0)
  AtLoopBottom: IExpressionStatement (OperationKind.ExpressionStatement)
      IAssignmentExpression (OperationKind.AssignmentExpression, Type: System.Int32)
        Left: ILocalReferenceExpression: i (OperationKind.LocalReferenceExpression, Type: System.Int32)
        Right: IBinaryOperatorExpression (BinaryOperationKind.IntegerAdd) (OperationKind.BinaryOperatorExpression, Type: System.Int32)
            Left: ILocalReferenceExpression: i (OperationKind.LocalReferenceExpression, Type: System.Int32)
            Right: ILiteralExpression (Text: 1) (OperationKind.LiteralExpression, Type: System.Int32, Constant: 1)
  IBlockStatement (1 statements) (OperationKind.BlockStatement)
    IExpressionStatement (OperationKind.ExpressionStatement)
      IAssignmentExpression (OperationKind.AssignmentExpression, Type: System.Int32)
        Left: ILocalReferenceExpression: x (OperationKind.LocalReferenceExpression, Type: System.Int32)
        Right: IBinaryOperatorExpression (BinaryOperationKind.IntegerMultiply) (OperationKind.BinaryOperatorExpression, Type: System.Int32)
            Left: ILocalReferenceExpression: x (OperationKind.LocalReferenceExpression, Type: System.Int32)
            Right: ILiteralExpression (Text: 3) (OperationKind.LiteralExpression, Type: System.Int32, Constant: 3)
";
            VerifyOperationTreeForTest<ForStatementSyntax>(source, expectedOperationTree);
        }

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
IForLoopStatement (LoopKind.For) (OperationKind.LoopStatement)
  Condition: ILiteralExpression (OperationKind.LiteralExpression, Type: System.Boolean, Constant: True)
  Before: IExpressionStatement (OperationKind.ExpressionStatement)
      IAssignmentExpression (OperationKind.AssignmentExpression, Type: System.Int32)
        Left: ILocalReferenceExpression: j (OperationKind.LocalReferenceExpression, Type: System.Int32)
        Right: ILiteralExpression (Text: 0) (OperationKind.LiteralExpression, Type: System.Int32, Constant: 0)
  AtLoopBottom: IExpressionStatement (OperationKind.ExpressionStatement)
      IAssignmentExpression (OperationKind.AssignmentExpression, Type: System.Int32)
        Left: ILocalReferenceExpression: j (OperationKind.LocalReferenceExpression, Type: System.Int32)
        Right: IBinaryOperatorExpression (BinaryOperationKind.IntegerAdd) (OperationKind.BinaryOperatorExpression, Type: System.Int32)
            Left: ILocalReferenceExpression: j (OperationKind.LocalReferenceExpression, Type: System.Int32)
            Right: ILiteralExpression (Text: 1) (OperationKind.LiteralExpression, Type: System.Int32, Constant: 1)
  IBlockStatement (2 statements) (OperationKind.BlockStatement)
    IExpressionStatement (OperationKind.ExpressionStatement)
      IAssignmentExpression (OperationKind.AssignmentExpression, Type: System.Int32)
        Left: ILocalReferenceExpression: i (OperationKind.LocalReferenceExpression, Type: System.Int32)
        Right: IBinaryOperatorExpression (BinaryOperationKind.IntegerAdd) (OperationKind.BinaryOperatorExpression, Type: System.Int32)
            Left: ILocalReferenceExpression: i (OperationKind.LocalReferenceExpression, Type: System.Int32)
            Right: ILiteralExpression (Text: 1) (OperationKind.LiteralExpression, Type: System.Int32, Constant: 1)
    IBranchStatement (BranchKind.Break) (OperationKind.BranchStatement)
";
            VerifyOperationTreeForTest<ForStatementSyntax>(source, expectedOperationTree);
        }

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
IForLoopStatement (LoopKind.For) (OperationKind.LoopStatement)
  Condition: ILiteralExpression (OperationKind.LiteralExpression, Type: System.Boolean, Constant: False)
  Before: IExpressionStatement (OperationKind.ExpressionStatement)
      IAssignmentExpression (OperationKind.AssignmentExpression, Type: System.Int32)
        Left: ILocalReferenceExpression: j (OperationKind.LocalReferenceExpression, Type: System.Int32)
        Right: ILiteralExpression (Text: 0) (OperationKind.LiteralExpression, Type: System.Int32, Constant: 0)
  AtLoopBottom: IExpressionStatement (OperationKind.ExpressionStatement)
      IAssignmentExpression (OperationKind.AssignmentExpression, Type: System.Int32)
        Left: ILocalReferenceExpression: j (OperationKind.LocalReferenceExpression, Type: System.Int32)
        Right: IBinaryOperatorExpression (BinaryOperationKind.IntegerAdd) (OperationKind.BinaryOperatorExpression, Type: System.Int32)
            Left: ILocalReferenceExpression: j (OperationKind.LocalReferenceExpression, Type: System.Int32)
            Right: ILiteralExpression (Text: 1) (OperationKind.LiteralExpression, Type: System.Int32, Constant: 1)
  IBlockStatement (2 statements) (OperationKind.BlockStatement)
    IExpressionStatement (OperationKind.ExpressionStatement)
      IAssignmentExpression (OperationKind.AssignmentExpression, Type: System.Int32)
        Left: ILocalReferenceExpression: i (OperationKind.LocalReferenceExpression, Type: System.Int32)
        Right: IBinaryOperatorExpression (BinaryOperationKind.IntegerAdd) (OperationKind.BinaryOperatorExpression, Type: System.Int32)
            Left: ILocalReferenceExpression: i (OperationKind.LocalReferenceExpression, Type: System.Int32)
            Right: ILiteralExpression (Text: 1) (OperationKind.LiteralExpression, Type: System.Int32, Constant: 1)
    IBranchStatement (BranchKind.Break) (OperationKind.BranchStatement)
";
            VerifyOperationTreeForTest<ForStatementSyntax>(source, expectedOperationTree);
        }

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
IForLoopStatement (LoopKind.For) (OperationKind.LoopStatement)
  Condition: IBinaryOperatorExpression (BinaryOperationKind.IntegerLessThan) (OperationKind.BinaryOperatorExpression, Type: System.Boolean)
      Left: ILocalReferenceExpression: i (OperationKind.LocalReferenceExpression, Type: System.Int32)
      Right: ILiteralExpression (Text: 5) (OperationKind.LiteralExpression, Type: System.Int32, Constant: 5)
  Before: IExpressionStatement (OperationKind.ExpressionStatement)
      IAssignmentExpression (OperationKind.AssignmentExpression, Type: System.Int32)
        Left: ILocalReferenceExpression: i (OperationKind.LocalReferenceExpression, Type: System.Int32)
        Right: ILiteralExpression (Text: 0) (OperationKind.LiteralExpression, Type: System.Int32, Constant: 0)
    IExpressionStatement (OperationKind.ExpressionStatement)
      IAssignmentExpression (OperationKind.AssignmentExpression, Type: System.Int32)
        Left: ILocalReferenceExpression: j (OperationKind.LocalReferenceExpression, Type: System.Int32)
        Right: ILiteralExpression (Text: 0) (OperationKind.LiteralExpression, Type: System.Int32, Constant: 0)
  AtLoopBottom: IExpressionStatement (OperationKind.ExpressionStatement)
      IAssignmentExpression (OperationKind.AssignmentExpression, Type: System.Int32)
        Left: ILocalReferenceExpression: i (OperationKind.LocalReferenceExpression, Type: System.Int32)
        Right: IBinaryOperatorExpression (BinaryOperationKind.IntegerAdd) (OperationKind.BinaryOperatorExpression, Type: System.Int32)
            Left: ILocalReferenceExpression: i (OperationKind.LocalReferenceExpression, Type: System.Int32)
            Right: ILiteralExpression (Text: 1) (OperationKind.LiteralExpression, Type: System.Int32, Constant: 1)
  IBlockStatement (2 statements) (OperationKind.BlockStatement)
    IIfStatement (OperationKind.IfStatement)
      Condition: IBinaryOperatorExpression (BinaryOperationKind.IntegerGreaterThan) (OperationKind.BinaryOperatorExpression, Type: System.Boolean)
          Left: ILocalReferenceExpression: i (OperationKind.LocalReferenceExpression, Type: System.Int32)
          Right: ILiteralExpression (Text: 2) (OperationKind.LiteralExpression, Type: System.Int32, Constant: 2)
      IBranchStatement (BranchKind.Continue) (OperationKind.BranchStatement)
    IExpressionStatement (OperationKind.ExpressionStatement)
      IAssignmentExpression (OperationKind.AssignmentExpression, Type: System.Int32)
        Left: ILocalReferenceExpression: j (OperationKind.LocalReferenceExpression, Type: System.Int32)
        Right: IBinaryOperatorExpression (BinaryOperationKind.IntegerAdd) (OperationKind.BinaryOperatorExpression, Type: System.Int32)
            Left: ILocalReferenceExpression: j (OperationKind.LocalReferenceExpression, Type: System.Int32)
            Right: ILiteralExpression (Text: 1) (OperationKind.LiteralExpression, Type: System.Int32, Constant: 1)
";
            VerifyOperationTreeForTest<ForStatementSyntax>(source, expectedOperationTree);
        }

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
IForLoopStatement (LoopKind.For) (OperationKind.LoopStatement)
  Condition: IBinaryOperatorExpression (BinaryOperationKind.IntegerLessThan) (OperationKind.BinaryOperatorExpression, Type: System.Boolean)
      Left: ILocalReferenceExpression: i (OperationKind.LocalReferenceExpression, Type: System.Int32)
      Right: ILiteralExpression (Text: 5) (OperationKind.LiteralExpression, Type: System.Int32, Constant: 5)
  Before: IExpressionStatement (OperationKind.ExpressionStatement)
      IAssignmentExpression (OperationKind.AssignmentExpression, Type: System.Int32)
        Left: ILocalReferenceExpression: i (OperationKind.LocalReferenceExpression, Type: System.Int32)
        Right: ILiteralExpression (Text: 0) (OperationKind.LiteralExpression, Type: System.Int32, Constant: 0)
    IExpressionStatement (OperationKind.ExpressionStatement)
      IAssignmentExpression (OperationKind.AssignmentExpression, Type: System.Int32)
        Left: ILocalReferenceExpression: j (OperationKind.LocalReferenceExpression, Type: System.Int32)
        Right: ILiteralExpression (Text: 0) (OperationKind.LiteralExpression, Type: System.Int32, Constant: 0)
  AtLoopBottom: IExpressionStatement (OperationKind.ExpressionStatement)
      IAssignmentExpression (OperationKind.AssignmentExpression, Type: System.Int32)
        Left: ILocalReferenceExpression: i (OperationKind.LocalReferenceExpression, Type: System.Int32)
        Right: IBinaryOperatorExpression (BinaryOperationKind.IntegerAdd) (OperationKind.BinaryOperatorExpression, Type: System.Int32)
            Left: ILocalReferenceExpression: i (OperationKind.LocalReferenceExpression, Type: System.Int32)
            Right: ILiteralExpression (Text: 1) (OperationKind.LiteralExpression, Type: System.Int32, Constant: 1)
  IBlockStatement (2 statements) (OperationKind.BlockStatement)
    IIfStatement (OperationKind.IfStatement)
      Condition: IBinaryOperatorExpression (BinaryOperationKind.IntegerGreaterThan) (OperationKind.BinaryOperatorExpression, Type: System.Boolean)
          Left: ILocalReferenceExpression: i (OperationKind.LocalReferenceExpression, Type: System.Int32)
          Right: ILiteralExpression (Text: 3) (OperationKind.LiteralExpression, Type: System.Int32, Constant: 3)
      IBranchStatement (BranchKind.Break) (OperationKind.BranchStatement)
    IExpressionStatement (OperationKind.ExpressionStatement)
      IAssignmentExpression (OperationKind.AssignmentExpression, Type: System.Int32)
        Left: ILocalReferenceExpression: j (OperationKind.LocalReferenceExpression, Type: System.Int32)
        Right: IBinaryOperatorExpression (BinaryOperationKind.IntegerAdd) (OperationKind.BinaryOperatorExpression, Type: System.Int32)
            Left: ILocalReferenceExpression: j (OperationKind.LocalReferenceExpression, Type: System.Int32)
            Right: ILiteralExpression (Text: 1) (OperationKind.LiteralExpression, Type: System.Int32, Constant: 1)
";
            VerifyOperationTreeForTest<ForStatementSyntax>(source, expectedOperationTree);
        }

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
IForLoopStatement (LoopKind.For) (OperationKind.LoopStatement)
  IBlockStatement (2 statements) (OperationKind.BlockStatement)
    IIfStatement (OperationKind.IfStatement)
      Condition: IBinaryOperatorExpression (BinaryOperationKind.IntegerGreaterThan) (OperationKind.BinaryOperatorExpression, Type: System.Boolean)
          Left: ILocalReferenceExpression: i (OperationKind.LocalReferenceExpression, Type: System.Int32)
          Right: ILiteralExpression (Text: 4) (OperationKind.LiteralExpression, Type: System.Int32, Constant: 4)
      IBranchStatement (BranchKind.Break) (OperationKind.BranchStatement)
    IExpressionStatement (OperationKind.ExpressionStatement)
      IAssignmentExpression (OperationKind.AssignmentExpression, Type: System.Int32)
        Left: ILocalReferenceExpression: i (OperationKind.LocalReferenceExpression, Type: System.Int32)
        Right: IBinaryOperatorExpression (BinaryOperationKind.IntegerAdd) (OperationKind.BinaryOperatorExpression, Type: System.Int32)
            Left: ILocalReferenceExpression: i (OperationKind.LocalReferenceExpression, Type: System.Int32)
            Right: ILiteralExpression (Text: 2) (OperationKind.LiteralExpression, Type: System.Int32, Constant: 2)
";
            VerifyOperationTreeForTest<ForStatementSyntax>(source, expectedOperationTree);
        }

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
IForLoopStatement (LoopKind.For) (OperationKind.LoopStatement)
  Condition: IBinaryOperatorExpression (BinaryOperationKind.IntegerLessThan) (OperationKind.BinaryOperatorExpression, Type: System.Boolean)
      Left: ILocalReferenceExpression: j (OperationKind.LocalReferenceExpression, Type: System.Int32)
      Right: ILiteralExpression (Text: 2) (OperationKind.LiteralExpression, Type: System.Int32, Constant: 2)
  Before: IExpressionStatement (OperationKind.ExpressionStatement)
      IAssignmentExpression (OperationKind.AssignmentExpression, Type: System.Int32)
        Left: ILocalReferenceExpression: i (OperationKind.LocalReferenceExpression, Type: System.Int32)
        Right: IBinaryOperatorExpression (BinaryOperationKind.IntegerAdd) (OperationKind.BinaryOperatorExpression, Type: System.Int32)
            Left: ILocalReferenceExpression: i (OperationKind.LocalReferenceExpression, Type: System.Int32)
            Right: ILiteralExpression (Text: 1) (OperationKind.LiteralExpression, Type: System.Int32, Constant: 1)
    IExpressionStatement (OperationKind.ExpressionStatement)
      IAssignmentExpression (OperationKind.AssignmentExpression, Type: System.Int32)
        Left: ILocalReferenceExpression: i (OperationKind.LocalReferenceExpression, Type: System.Int32)
        Right: IBinaryOperatorExpression (BinaryOperationKind.IntegerAdd) (OperationKind.BinaryOperatorExpression, Type: System.Int32)
            Left: ILocalReferenceExpression: i (OperationKind.LocalReferenceExpression, Type: System.Int32)
            Right: ILiteralExpression (Text: 1) (OperationKind.LiteralExpression, Type: System.Int32, Constant: 1)
  AtLoopBottom: IExpressionStatement (OperationKind.ExpressionStatement)
      IAssignmentExpression (OperationKind.AssignmentExpression, Type: System.Int32)
        Left: ILocalReferenceExpression: i (OperationKind.LocalReferenceExpression, Type: System.Int32)
        Right: IBinaryOperatorExpression (BinaryOperationKind.IntegerAdd) (OperationKind.BinaryOperatorExpression, Type: System.Int32)
            Left: ILocalReferenceExpression: i (OperationKind.LocalReferenceExpression, Type: System.Int32)
            Right: ILiteralExpression (Text: 2) (OperationKind.LiteralExpression, Type: System.Int32, Constant: 2)
    IExpressionStatement (OperationKind.ExpressionStatement)
      IAssignmentExpression (OperationKind.AssignmentExpression, Type: System.Int32)
        Left: ILocalReferenceExpression: j (OperationKind.LocalReferenceExpression, Type: System.Int32)
        Right: IBinaryOperatorExpression (BinaryOperationKind.IntegerAdd) (OperationKind.BinaryOperatorExpression, Type: System.Int32)
            Left: ILocalReferenceExpression: j (OperationKind.LocalReferenceExpression, Type: System.Int32)
            Right: ILiteralExpression (Text: 1) (OperationKind.LiteralExpression, Type: System.Int32, Constant: 1)
  IBlockStatement (0 statements) (OperationKind.BlockStatement)
";
            VerifyOperationTreeForTest<ForStatementSyntax>(source, expectedOperationTree);
        }

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
IForLoopStatement (LoopKind.For) (OperationKind.LoopStatement)
  Condition: IBinaryOperatorExpression (BinaryOperationKind.IntegerLessThan) (OperationKind.BinaryOperatorExpression, Type: System.Boolean)
      Left: ILocalReferenceExpression: i (OperationKind.LocalReferenceExpression, Type: System.Int32)
      Right: ILiteralExpression (Text: 10) (OperationKind.LiteralExpression, Type: System.Int32, Constant: 10)
  AtLoopBottom: IExpressionStatement (OperationKind.ExpressionStatement)
      IAssignmentExpression (OperationKind.AssignmentExpression, Type: System.Int32)
        Left: ILocalReferenceExpression: i (OperationKind.LocalReferenceExpression, Type: System.Int32)
        Right: IBinaryOperatorExpression (BinaryOperationKind.IntegerAdd) (OperationKind.BinaryOperatorExpression, Type: System.Int32)
            Left: ILocalReferenceExpression: i (OperationKind.LocalReferenceExpression, Type: System.Int32)
            Right: ILiteralExpression (Text: 1) (OperationKind.LiteralExpression, Type: System.Int32, Constant: 1)
  IBlockStatement (0 statements) (OperationKind.BlockStatement)
";
            VerifyOperationTreeForTest<ForStatementSyntax>(source, expectedOperationTree);
        }

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
IForLoopStatement (LoopKind.For) (OperationKind.LoopStatement)
  Condition: IBinaryOperatorExpression (BinaryOperationKind.IntegerGreaterThan) (OperationKind.BinaryOperatorExpression, Type: System.Boolean)
      Left: ILocalReferenceExpression: k (OperationKind.LocalReferenceExpression, Type: System.Int32)
      Right: ILiteralExpression (Text: 100) (OperationKind.LiteralExpression, Type: System.Int32, Constant: 100)
  Local_1: System.Int32 k
  Before: IVariableDeclarationStatement (1 variables) (OperationKind.VariableDeclarationStatement)
      IVariableDeclaration: System.Int32 k (OperationKind.VariableDeclaration)
        Initializer: ILiteralExpression (Text: 200) (OperationKind.LiteralExpression, Type: System.Int32, Constant: 200)
  AtLoopBottom: IExpressionStatement (OperationKind.ExpressionStatement)
      IAssignmentExpression (OperationKind.AssignmentExpression, Type: System.Int32)
        Left: ILocalReferenceExpression: k (OperationKind.LocalReferenceExpression, Type: System.Int32)
        Right: IBinaryOperatorExpression (BinaryOperationKind.IntegerSubtract) (OperationKind.BinaryOperatorExpression, Type: System.Int32)
            Left: ILocalReferenceExpression: k (OperationKind.LocalReferenceExpression, Type: System.Int32)
            Right: ILiteralExpression (Text: 1) (OperationKind.LiteralExpression, Type: System.Int32, Constant: 1)
  IBlockStatement (0 statements) (OperationKind.BlockStatement)
";
            VerifyOperationTreeForTest<ForStatementSyntax>(source, expectedOperationTree);
        }

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
IForLoopStatement (LoopKind.For) (OperationKind.LoopStatement)
  Condition: IInvocationExpression (static System.Boolean C.Conditional()) (OperationKind.InvocationExpression, Type: System.Boolean)
  Before: IExpressionStatement (OperationKind.ExpressionStatement)
      IInvocationExpression (static System.Int32 C.Initializer()) (OperationKind.InvocationExpression, Type: System.Int32)
  AtLoopBottom: IExpressionStatement (OperationKind.ExpressionStatement)
      IInvocationExpression (static System.Int32 C.Iterator()) (OperationKind.InvocationExpression, Type: System.Int32)
  IBlockStatement (0 statements) (OperationKind.BlockStatement)
";
            VerifyOperationTreeForTest<ForStatementSyntax>(source, expectedOperationTree);
        }

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
IForLoopStatement (LoopKind.For) (OperationKind.LoopStatement)
  Condition: IBinaryOperatorExpression (BinaryOperationKind.IntegerLessThan) (OperationKind.BinaryOperatorExpression, Type: System.Boolean)
      Left: ILocalReferenceExpression: i (OperationKind.LocalReferenceExpression, Type: System.Int32)
      Right: ILiteralExpression (Text: 100) (OperationKind.LiteralExpression, Type: System.Int32, Constant: 100)
  Local_1: System.Int32 i
  Before: IVariableDeclarationStatement (1 variables) (OperationKind.VariableDeclarationStatement)
      IVariableDeclaration: System.Int32 i (OperationKind.VariableDeclaration)
        Initializer: ILiteralExpression (Text: 10) (OperationKind.LiteralExpression, Type: System.Int32, Constant: 10)
  AtLoopBottom: IExpressionStatement (OperationKind.ExpressionStatement)
      IAssignmentExpression (OperationKind.AssignmentExpression, Type: System.Int32)
        Left: ILocalReferenceExpression: i (OperationKind.LocalReferenceExpression, Type: System.Int32)
        Right: IBinaryOperatorExpression (BinaryOperationKind.IntegerAdd) (OperationKind.BinaryOperatorExpression, Type: System.Int32)
            Left: ILocalReferenceExpression: i (OperationKind.LocalReferenceExpression, Type: System.Int32)
            Right: ILiteralExpression (Text: 1) (OperationKind.LiteralExpression, Type: System.Int32, Constant: 1)
  IEmptyStatement (OperationKind.EmptyStatement)
";
            VerifyOperationTreeForTest<ForStatementSyntax>(source, expectedOperationTree);
        }

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
IForLoopStatement (LoopKind.For) (OperationKind.LoopStatement)
  Condition: IBinaryOperatorExpression (BinaryOperationKind.IntegerLessThan) (OperationKind.BinaryOperatorExpression, Type: System.Boolean)
      Left: ILocalReferenceExpression: i (OperationKind.LocalReferenceExpression, Type: System.Int32)
      Right: ILiteralExpression (Text: 100) (OperationKind.LiteralExpression, Type: System.Int32, Constant: 100)
  Local_1: System.Int32 i
  Before: IVariableDeclarationStatement (1 variables) (OperationKind.VariableDeclarationStatement)
      IVariableDeclaration: System.Int32 i (OperationKind.VariableDeclaration)
        Initializer: ILiteralExpression (Text: 0) (OperationKind.LiteralExpression, Type: System.Int32, Constant: 0)
  AtLoopBottom: IExpressionStatement (OperationKind.ExpressionStatement)
      IAssignmentExpression (OperationKind.AssignmentExpression, Type: System.Int32)
        Left: ILocalReferenceExpression: i (OperationKind.LocalReferenceExpression, Type: System.Int32)
        Right: IBinaryOperatorExpression (BinaryOperationKind.IntegerAdd) (OperationKind.BinaryOperatorExpression, Type: System.Int32)
            Left: ILocalReferenceExpression: i (OperationKind.LocalReferenceExpression, Type: System.Int32)
            Right: ILiteralExpression (Text: 1) (OperationKind.LiteralExpression, Type: System.Int32, Constant: 1)
  IBlockStatement (1 statements) (OperationKind.BlockStatement)
    IForLoopStatement (LoopKind.For) (OperationKind.LoopStatement)
      Condition: IBinaryOperatorExpression (BinaryOperationKind.IntegerLessThan) (OperationKind.BinaryOperatorExpression, Type: System.Boolean)
          Left: ILocalReferenceExpression: j (OperationKind.LocalReferenceExpression, Type: System.Int32)
          Right: ILiteralExpression (Text: 10) (OperationKind.LiteralExpression, Type: System.Int32, Constant: 10)
      Local_1: System.Int32 j
      Before: IVariableDeclarationStatement (1 variables) (OperationKind.VariableDeclarationStatement)
          IVariableDeclaration: System.Int32 j (OperationKind.VariableDeclaration)
            Initializer: ILiteralExpression (Text: 0) (OperationKind.LiteralExpression, Type: System.Int32, Constant: 0)
      AtLoopBottom: IExpressionStatement (OperationKind.ExpressionStatement)
          IAssignmentExpression (OperationKind.AssignmentExpression, Type: System.Int32)
            Left: ILocalReferenceExpression: j (OperationKind.LocalReferenceExpression, Type: System.Int32)
            Right: IBinaryOperatorExpression (BinaryOperationKind.IntegerAdd) (OperationKind.BinaryOperatorExpression, Type: System.Int32)
                Left: ILocalReferenceExpression: j (OperationKind.LocalReferenceExpression, Type: System.Int32)
                Right: ILiteralExpression (Text: 1) (OperationKind.LiteralExpression, Type: System.Int32, Constant: 1)
      IBlockStatement (0 statements) (OperationKind.BlockStatement)
";
            VerifyOperationTreeForTest<ForStatementSyntax>(source, expectedOperationTree);
        }

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
IForLoopStatement (LoopKind.For) (OperationKind.LoopStatement)
  Condition: IBinaryOperatorExpression (BinaryOperationKind.IntegerLessThan) (OperationKind.BinaryOperatorExpression, Type: System.Boolean)
      Left: ILocalReferenceExpression: i (OperationKind.LocalReferenceExpression, Type: System.Int32)
      Right: ILiteralExpression (Text: 10) (OperationKind.LiteralExpression, Type: System.Int32, Constant: 10)
  Local_1: System.Int32 i
  Before: IVariableDeclarationStatement (1 variables) (OperationKind.VariableDeclarationStatement)
      IVariableDeclaration: System.Int32 i (OperationKind.VariableDeclaration)
        Initializer: ILiteralExpression (Text: 0) (OperationKind.LiteralExpression, Type: System.Int32, Constant: 0)
  AtLoopBottom: IExpressionStatement (OperationKind.ExpressionStatement)
      IAssignmentExpression (OperationKind.AssignmentExpression, Type: System.Int32)
        Left: ILocalReferenceExpression: i (OperationKind.LocalReferenceExpression, Type: System.Int32)
        Right: IBinaryOperatorExpression (BinaryOperationKind.IntegerAdd) (OperationKind.BinaryOperatorExpression, Type: System.Int32)
            Left: ILocalReferenceExpression: i (OperationKind.LocalReferenceExpression, Type: System.Int32)
            Right: ILiteralExpression (Text: 1) (OperationKind.LiteralExpression, Type: System.Int32, Constant: 1)
  IBlockStatement (1 statements) (OperationKind.BlockStatement)
    IForLoopStatement (LoopKind.For) (OperationKind.LoopStatement)
      Condition: IBinaryOperatorExpression (BinaryOperationKind.IntegerLessThan) (OperationKind.BinaryOperatorExpression, Type: System.Boolean)
          Left: ILocalReferenceExpression: j (OperationKind.LocalReferenceExpression, Type: System.Int32)
          Right: ILiteralExpression (Text: 10) (OperationKind.LiteralExpression, Type: System.Int32, Constant: 10)
      Local_1: System.Int32 j
      Before: IVariableDeclarationStatement (1 variables) (OperationKind.VariableDeclarationStatement)
          IVariableDeclaration: System.Int32 j (OperationKind.VariableDeclaration)
            Initializer: ILiteralExpression (Text: 0) (OperationKind.LiteralExpression, Type: System.Int32, Constant: 0)
      AtLoopBottom: IExpressionStatement (OperationKind.ExpressionStatement)
          IAssignmentExpression (OperationKind.AssignmentExpression, Type: System.Int32)
            Left: ILocalReferenceExpression: j (OperationKind.LocalReferenceExpression, Type: System.Int32)
            Right: IBinaryOperatorExpression (BinaryOperationKind.IntegerAdd) (OperationKind.BinaryOperatorExpression, Type: System.Int32)
                Left: ILocalReferenceExpression: j (OperationKind.LocalReferenceExpression, Type: System.Int32)
                Right: ILiteralExpression (Text: 1) (OperationKind.LiteralExpression, Type: System.Int32, Constant: 1)
      IBlockStatement (1 statements) (OperationKind.BlockStatement)
        IExpressionStatement (OperationKind.ExpressionStatement)
          IAssignmentExpression (OperationKind.AssignmentExpression, Type: System.Int32)
            Left: ILocalReferenceExpression: i (OperationKind.LocalReferenceExpression, Type: System.Int32)
            Right: ILiteralExpression (Text: 1) (OperationKind.LiteralExpression, Type: System.Int32, Constant: 1)
";
            VerifyOperationTreeForTest<ForStatementSyntax>(source, expectedOperationTree);
        }

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
IForLoopStatement (LoopKind.For) (OperationKind.LoopStatement)
  Condition: IBinaryOperatorExpression (BinaryOperationKind.IntegerLessThan) (OperationKind.BinaryOperatorExpression, Type: System.Boolean)
      Left: ILocalReferenceExpression: i (OperationKind.LocalReferenceExpression, Type: System.Int32)
      Right: ILiteralExpression (Text: 5) (OperationKind.LiteralExpression, Type: System.Int32, Constant: 5)
  Local_1: System.Int32 i
  Before: IVariableDeclarationStatement (1 variables) (OperationKind.VariableDeclarationStatement)
      IVariableDeclaration: System.Int32 i (OperationKind.VariableDeclaration)
        Initializer: ILiteralExpression (Text: 0) (OperationKind.LiteralExpression, Type: System.Int32, Constant: 0)
  AtLoopBottom: IExpressionStatement (OperationKind.ExpressionStatement)
      IAssignmentExpression (OperationKind.AssignmentExpression, Type: System.Int32)
        Left: ILocalReferenceExpression: i (OperationKind.LocalReferenceExpression, Type: System.Int32)
        Right: IBinaryOperatorExpression (BinaryOperationKind.IntegerAdd) (OperationKind.BinaryOperatorExpression, Type: System.Int32)
            Left: ILocalReferenceExpression: i (OperationKind.LocalReferenceExpression, Type: System.Int32)
            Right: ILiteralExpression (Text: 1) (OperationKind.LiteralExpression, Type: System.Int32, Constant: 1)
  IBlockStatement (1 statements) (OperationKind.BlockStatement)
    IForLoopStatement (LoopKind.For) (OperationKind.LoopStatement)
      Condition: IBinaryOperatorExpression (BinaryOperationKind.IntegerLessThan) (OperationKind.BinaryOperatorExpression, Type: System.Boolean)
          Left: ILocalReferenceExpression: i (OperationKind.LocalReferenceExpression, Type: System.Int32)
          Right: ILocalReferenceExpression: j (OperationKind.LocalReferenceExpression, Type: System.Int32)
      Local_1: System.Int32 j
      Before: IVariableDeclarationStatement (1 variables) (OperationKind.VariableDeclarationStatement)
          IVariableDeclaration: System.Int32 j (OperationKind.VariableDeclaration)
            Initializer: IBinaryOperatorExpression (BinaryOperationKind.IntegerAdd) (OperationKind.BinaryOperatorExpression, Type: System.Int32)
                Left: ILocalReferenceExpression: i (OperationKind.LocalReferenceExpression, Type: System.Int32)
                Right: ILiteralExpression (Text: 1) (OperationKind.LiteralExpression, Type: System.Int32, Constant: 1)
      AtLoopBottom: IExpressionStatement (OperationKind.ExpressionStatement)
          IAssignmentExpression (OperationKind.AssignmentExpression, Type: System.Int32)
            Left: ILocalReferenceExpression: j (OperationKind.LocalReferenceExpression, Type: System.Int32)
            Right: IBinaryOperatorExpression (BinaryOperationKind.IntegerSubtract) (OperationKind.BinaryOperatorExpression, Type: System.Int32)
                Left: ILocalReferenceExpression: j (OperationKind.LocalReferenceExpression, Type: System.Int32)
                Right: ILiteralExpression (Text: 1) (OperationKind.LiteralExpression, Type: System.Int32, Constant: 1)
      IBlockStatement (0 statements) (OperationKind.BlockStatement)
";
            VerifyOperationTreeForTest<ForStatementSyntax>(source, expectedOperationTree);
        }

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
IForLoopStatement (LoopKind.For) (OperationKind.LoopStatement)
  Condition: IBinaryOperatorExpression (BinaryOperationKind.IntegerLessThan) (OperationKind.BinaryOperatorExpression, Type: System.Boolean)
      Left: ILocalReferenceExpression: i (OperationKind.LocalReferenceExpression, Type: System.Int32)
      Right: ILiteralExpression (Text: 5) (OperationKind.LiteralExpression, Type: System.Int32, Constant: 5)
  Local_1: System.Int32 i
  Before: IVariableDeclarationStatement (1 variables) (OperationKind.VariableDeclarationStatement)
      IVariableDeclaration: System.Int32 i (OperationKind.VariableDeclaration)
        Initializer: ILiteralExpression (Text: 0) (OperationKind.LiteralExpression, Type: System.Int32, Constant: 0)
  AtLoopBottom: IExpressionStatement (OperationKind.ExpressionStatement)
      IAssignmentExpression (OperationKind.AssignmentExpression, Type: System.Int32)
        Left: ILocalReferenceExpression: i (OperationKind.LocalReferenceExpression, Type: System.Int32)
        Right: IBinaryOperatorExpression (BinaryOperationKind.IntegerAdd) (OperationKind.BinaryOperatorExpression, Type: System.Int32)
            Left: ILocalReferenceExpression: i (OperationKind.LocalReferenceExpression, Type: System.Int32)
            Right: ILiteralExpression (Text: 1) (OperationKind.LiteralExpression, Type: System.Int32, Constant: 1)
  IBlockStatement (1 statements) (OperationKind.BlockStatement)
    IForLoopStatement (LoopKind.For) (OperationKind.LoopStatement)
      Condition: IBinaryOperatorExpression (BinaryOperationKind.IntegerLessThan) (OperationKind.BinaryOperatorExpression, Type: System.Boolean)
          Left: ILocalReferenceExpression: j (OperationKind.LocalReferenceExpression, Type: System.Int32)
          Right: ILiteralExpression (Text: 10) (OperationKind.LiteralExpression, Type: System.Int32, Constant: 10)
      Local_1: System.Int32 j
      Before: IVariableDeclarationStatement (1 variables) (OperationKind.VariableDeclarationStatement)
          IVariableDeclaration: System.Int32 j (OperationKind.VariableDeclaration)
            Initializer: ILiteralExpression (Text: 0) (OperationKind.LiteralExpression, Type: System.Int32, Constant: 0)
      AtLoopBottom: IExpressionStatement (OperationKind.ExpressionStatement)
          IAssignmentExpression (OperationKind.AssignmentExpression, Type: System.Int32)
            Left: ILocalReferenceExpression: j (OperationKind.LocalReferenceExpression, Type: System.Int32)
            Right: IBinaryOperatorExpression (BinaryOperationKind.IntegerAdd) (OperationKind.BinaryOperatorExpression, Type: System.Int32)
                Left: ILocalReferenceExpression: j (OperationKind.LocalReferenceExpression, Type: System.Int32)
                Right: ILiteralExpression (Text: 1) (OperationKind.LiteralExpression, Type: System.Int32, Constant: 1)
      IBlockStatement (1 statements) (OperationKind.BlockStatement)
        IIfStatement (OperationKind.IfStatement)
          Condition: IBinaryOperatorExpression (BinaryOperationKind.IntegerEquals) (OperationKind.BinaryOperatorExpression, Type: System.Boolean)
              Left: ILocalReferenceExpression: j (OperationKind.LocalReferenceExpression, Type: System.Int32)
              Right: ILiteralExpression (Text: 5) (OperationKind.LiteralExpression, Type: System.Int32, Constant: 5)
          IBranchStatement (BranchKind.Break) (OperationKind.BranchStatement)
";
            VerifyOperationTreeForTest<ForStatementSyntax>(source, expectedOperationTree);
        }

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
IForLoopStatement (LoopKind.For) (OperationKind.LoopStatement)
  Condition: IBinaryOperatorExpression (BinaryOperationKind.IntegerLessThan) (OperationKind.BinaryOperatorExpression, Type: System.Boolean)
      Left: ILocalReferenceExpression: i (OperationKind.LocalReferenceExpression, Type: System.Int32)
      Right: ILiteralExpression (Text: 5) (OperationKind.LiteralExpression, Type: System.Int32, Constant: 5)
  Local_1: System.Int32 i
  Before: IVariableDeclarationStatement (1 variables) (OperationKind.VariableDeclarationStatement)
      IVariableDeclaration: System.Int32 i (OperationKind.VariableDeclaration)
        Initializer: ILiteralExpression (Text: 0) (OperationKind.LiteralExpression, Type: System.Int32, Constant: 0)
  AtLoopBottom: IExpressionStatement (OperationKind.ExpressionStatement)
      IAssignmentExpression (OperationKind.AssignmentExpression, Type: System.Int32)
        Left: ILocalReferenceExpression: i (OperationKind.LocalReferenceExpression, Type: System.Int32)
        Right: IBinaryOperatorExpression (BinaryOperationKind.IntegerAdd) (OperationKind.BinaryOperatorExpression, Type: System.Int32)
            Left: ILocalReferenceExpression: i (OperationKind.LocalReferenceExpression, Type: System.Int32)
            Right: ILiteralExpression (Text: 1) (OperationKind.LiteralExpression, Type: System.Int32, Constant: 1)
  IBlockStatement (1 statements) (OperationKind.BlockStatement)
    IForLoopStatement (LoopKind.For) (OperationKind.LoopStatement)
      Condition: IBinaryOperatorExpression (BinaryOperationKind.IntegerLessThan) (OperationKind.BinaryOperatorExpression, Type: System.Boolean)
          Left: ILocalReferenceExpression: j (OperationKind.LocalReferenceExpression, Type: System.Int32)
          Right: ILiteralExpression (Text: 10) (OperationKind.LiteralExpression, Type: System.Int32, Constant: 10)
      Local_1: System.Int32 j
      Before: IVariableDeclarationStatement (1 variables) (OperationKind.VariableDeclarationStatement)
          IVariableDeclaration: System.Int32 j (OperationKind.VariableDeclaration)
            Initializer: ILiteralExpression (Text: 1) (OperationKind.LiteralExpression, Type: System.Int32, Constant: 1)
      AtLoopBottom: IExpressionStatement (OperationKind.ExpressionStatement)
          IAssignmentExpression (OperationKind.AssignmentExpression, Type: System.Int32)
            Left: ILocalReferenceExpression: j (OperationKind.LocalReferenceExpression, Type: System.Int32)
            Right: IBinaryOperatorExpression (BinaryOperationKind.IntegerAdd) (OperationKind.BinaryOperatorExpression, Type: System.Int32)
                Left: ILocalReferenceExpression: j (OperationKind.LocalReferenceExpression, Type: System.Int32)
                Right: ILiteralExpression (Text: 1) (OperationKind.LiteralExpression, Type: System.Int32, Constant: 1)
      IBlockStatement (3 statements) (OperationKind.BlockStatement)
        IIfStatement (OperationKind.IfStatement)
          Condition: IBinaryOperatorExpression (BinaryOperationKind.IntegerNotEquals) (OperationKind.BinaryOperatorExpression, Type: System.Boolean)
              Left: IBinaryOperatorExpression (BinaryOperationKind.IntegerRemainder) (OperationKind.BinaryOperatorExpression, Type: System.Int32)
                  Left: ILocalReferenceExpression: j (OperationKind.LocalReferenceExpression, Type: System.Int32)
                  Right: ILiteralExpression (Text: 2) (OperationKind.LiteralExpression, Type: System.Int32, Constant: 2)
              Right: ILiteralExpression (Text: 0) (OperationKind.LiteralExpression, Type: System.Int32, Constant: 0)
          IBranchStatement (BranchKind.Continue) (OperationKind.BranchStatement)
        IExpressionStatement (OperationKind.ExpressionStatement)
          IAssignmentExpression (OperationKind.AssignmentExpression, Type: System.Int32)
            Left: ILocalReferenceExpression: i (OperationKind.LocalReferenceExpression, Type: System.Int32)
            Right: IBinaryOperatorExpression (BinaryOperationKind.IntegerAdd) (OperationKind.BinaryOperatorExpression, Type: System.Int32)
                Left: ILocalReferenceExpression: i (OperationKind.LocalReferenceExpression, Type: System.Int32)
                Right: ILiteralExpression (Text: 1) (OperationKind.LiteralExpression, Type: System.Int32, Constant: 1)
        IExpressionStatement (OperationKind.ExpressionStatement)
          IInvocationExpression (static void System.Console.Write(System.Int32 value)) (OperationKind.InvocationExpression, Type: System.Void)
            IArgument (Matching Parameter: value) (OperationKind.Argument)
              ILocalReferenceExpression: i (OperationKind.LocalReferenceExpression, Type: System.Int32)
";
            VerifyOperationTreeForTest<ForStatementSyntax>(source, expectedOperationTree);
        }

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
IForLoopStatement (LoopKind.For) (OperationKind.LoopStatement)
  Condition: IBinaryOperatorExpression (BinaryOperationKind.IntegerLessThan) (OperationKind.BinaryOperatorExpression, Type: System.Boolean)
      Left: ILocalReferenceExpression: i (OperationKind.LocalReferenceExpression, Type: System.Int32)
      Right: ILiteralExpression (Text: 5) (OperationKind.LiteralExpression, Type: System.Int32, Constant: 5)
  Local_1: System.Int32 i
  Before: IVariableDeclarationStatement (1 variables) (OperationKind.VariableDeclarationStatement)
      IVariableDeclaration: System.Int32 i (OperationKind.VariableDeclaration)
        Initializer: ILiteralExpression (Text: 0) (OperationKind.LiteralExpression, Type: System.Int32, Constant: 0)
  AtLoopBottom: IExpressionStatement (OperationKind.ExpressionStatement)
      IAssignmentExpression (OperationKind.AssignmentExpression, Type: System.Int32)
        Left: ILocalReferenceExpression: i (OperationKind.LocalReferenceExpression, Type: System.Int32)
        Right: IBinaryOperatorExpression (BinaryOperationKind.IntegerAdd) (OperationKind.BinaryOperatorExpression, Type: System.Int32)
            Left: ILocalReferenceExpression: i (OperationKind.LocalReferenceExpression, Type: System.Int32)
            Right: ILiteralExpression (Text: 1) (OperationKind.LiteralExpression, Type: System.Int32, Constant: 1)
  IBlockStatement (1 statements) (OperationKind.BlockStatement)
    IForLoopStatement (LoopKind.For) (OperationKind.LoopStatement)
      Condition: IBinaryOperatorExpression (BinaryOperationKind.IntegerLessThan) (OperationKind.BinaryOperatorExpression, Type: System.Boolean)
          Left: ILocalReferenceExpression: j (OperationKind.LocalReferenceExpression, Type: System.Int32)
          Right: ILiteralExpression (Text: 10) (OperationKind.LiteralExpression, Type: System.Int32, Constant: 10)
      Local_1: System.Int32 j
      Before: IVariableDeclarationStatement (1 variables) (OperationKind.VariableDeclarationStatement)
          IVariableDeclaration: System.Int32 j (OperationKind.VariableDeclaration)
            Initializer: ILiteralExpression (Text: 0) (OperationKind.LiteralExpression, Type: System.Int32, Constant: 0)
      AtLoopBottom: IExpressionStatement (OperationKind.ExpressionStatement)
          IAssignmentExpression (OperationKind.AssignmentExpression, Type: System.Int32)
            Left: ILocalReferenceExpression: j (OperationKind.LocalReferenceExpression, Type: System.Int32)
            Right: IBinaryOperatorExpression (BinaryOperationKind.IntegerAdd) (OperationKind.BinaryOperatorExpression, Type: System.Int32)
                Left: ILocalReferenceExpression: j (OperationKind.LocalReferenceExpression, Type: System.Int32)
                Right: ILiteralExpression (Text: 1) (OperationKind.LiteralExpression, Type: System.Int32, Constant: 1)
      IBlockStatement (2 statements) (OperationKind.BlockStatement)
        IBranchStatement (BranchKind.GoTo, Label: stop) (OperationKind.BranchStatement)
        ILabelStatement (Label: stop) (OperationKind.LabelStatement)
          IExpressionStatement (OperationKind.ExpressionStatement)
            IAssignmentExpression (OperationKind.AssignmentExpression, Type: System.Int32)
              Left: ILocalReferenceExpression: j (OperationKind.LocalReferenceExpression, Type: System.Int32)
              Right: IBinaryOperatorExpression (BinaryOperationKind.IntegerAdd) (OperationKind.BinaryOperatorExpression, Type: System.Int32)
                  Left: ILocalReferenceExpression: j (OperationKind.LocalReferenceExpression, Type: System.Int32)
                  Right: ILiteralExpression (Text: 1) (OperationKind.LiteralExpression, Type: System.Int32, Constant: 1)
";
            VerifyOperationTreeForTest<ForStatementSyntax>(source, expectedOperationTree);
        }

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
IForLoopStatement (LoopKind.For) (OperationKind.LoopStatement)
  Condition: IBinaryOperatorExpression (BinaryOperationKind.IntegerLessThan) (OperationKind.BinaryOperatorExpression, Type: System.Boolean)
      Left: ILocalReferenceExpression: i (OperationKind.LocalReferenceExpression, Type: System.Int32)
      Right: ILiteralExpression (Text: 10) (OperationKind.LiteralExpression, Type: System.Int32, Constant: 10)
  Local_1: System.Int32 i
  Before: IVariableDeclarationStatement (1 variables) (OperationKind.VariableDeclarationStatement)
      IVariableDeclaration: System.Int32 i (OperationKind.VariableDeclaration)
        Initializer: ILiteralExpression (Text: 0) (OperationKind.LiteralExpression, Type: System.Int32, Constant: 0)
  AtLoopBottom: IExpressionStatement (OperationKind.ExpressionStatement)
      IAssignmentExpression (OperationKind.AssignmentExpression, Type: System.Int32)
        Left: ILocalReferenceExpression: i (OperationKind.LocalReferenceExpression, Type: System.Int32)
        Right: IBinaryOperatorExpression (BinaryOperationKind.IntegerAdd) (OperationKind.BinaryOperatorExpression, Type: System.Int32)
            Left: ILocalReferenceExpression: i (OperationKind.LocalReferenceExpression, Type: System.Int32)
            Right: ILiteralExpression (Text: 1) (OperationKind.LiteralExpression, Type: System.Int32, Constant: 1)
  IBlockStatement (1 statements) (OperationKind.BlockStatement)
    IForLoopStatement (LoopKind.For) (OperationKind.LoopStatement)
      Condition: IBinaryOperatorExpression (BinaryOperationKind.IntegerLessThan) (OperationKind.BinaryOperatorExpression, Type: System.Boolean)
          Left: ILocalReferenceExpression: j (OperationKind.LocalReferenceExpression, Type: System.Int32)
          Right: ILiteralExpression (Text: 10) (OperationKind.LiteralExpression, Type: System.Int32, Constant: 10)
      Local_1: System.Int32 j
      Before: IVariableDeclarationStatement (1 variables) (OperationKind.VariableDeclarationStatement)
          IVariableDeclaration: System.Int32 j (OperationKind.VariableDeclaration)
            Initializer: ILiteralExpression (Text: 0) (OperationKind.LiteralExpression, Type: System.Int32, Constant: 0)
      IBlockStatement (1 statements) (OperationKind.BlockStatement)
        IThrowStatement (OperationKind.ThrowStatement)
          IObjectCreationExpression (Constructor: System.Exception..ctor()) (OperationKind.ObjectCreationExpression, Type: System.Exception)
";
            VerifyOperationTreeForTest<ForStatementSyntax>(source, expectedOperationTree);
        }

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
IForLoopStatement (LoopKind.For) (OperationKind.LoopStatement)
  Condition: IBinaryOperatorExpression (BinaryOperationKind.IntegerLessThan) (OperationKind.BinaryOperatorExpression, Type: System.Boolean)
      Left: ILocalReferenceExpression: i (OperationKind.LocalReferenceExpression, Type: System.Int32)
      Right: ILiteralExpression (Text: 10) (OperationKind.LiteralExpression, Type: System.Int32, Constant: 10)
  Local_1: System.Int32 i
  Before: IVariableDeclarationStatement (1 variables) (OperationKind.VariableDeclarationStatement)
      IVariableDeclaration: System.Int32 i (OperationKind.VariableDeclaration)
        Initializer: ILiteralExpression (Text: 0) (OperationKind.LiteralExpression, Type: System.Int32, Constant: 0)
  IBlockStatement (1 statements) (OperationKind.BlockStatement)
    IForLoopStatement (LoopKind.For) (OperationKind.LoopStatement)
      Condition: IBinaryOperatorExpression (BinaryOperationKind.IntegerLessThan) (OperationKind.BinaryOperatorExpression, Type: System.Boolean)
          Left: ILocalReferenceExpression: j (OperationKind.LocalReferenceExpression, Type: System.Int32)
          Right: ILiteralExpression (Text: 5) (OperationKind.LiteralExpression, Type: System.Int32, Constant: 5)
      Local_1: System.Int32 j
      Before: IVariableDeclarationStatement (1 variables) (OperationKind.VariableDeclarationStatement)
          IVariableDeclaration: System.Int32 j (OperationKind.VariableDeclaration)
            Initializer: ILiteralExpression (Text: 0) (OperationKind.LiteralExpression, Type: System.Int32, Constant: 0)
      IBlockStatement (1 statements) (OperationKind.BlockStatement)
        IReturnStatement (OperationKind.ReturnStatement)
";
            VerifyOperationTreeForTest<ForStatementSyntax>(source, expectedOperationTree);
        }

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
IForLoopStatement (LoopKind.For) (OperationKind.LoopStatement)
  Condition: IBinaryOperatorExpression (BinaryOperationKind.IntegerLessThan) (OperationKind.BinaryOperatorExpression, Type: System.Boolean)
      Left: ILocalReferenceExpression: i (OperationKind.LocalReferenceExpression, Type: System.Int32)
      Right: ILiteralExpression (Text: 5) (OperationKind.LiteralExpression, Type: System.Int32, Constant: 5)
  Local_1: System.Int32 i
  Local_2: System.Int32 j
  Before: IVariableDeclarationStatement (2 variables) (OperationKind.VariableDeclarationStatement)
      IVariableDeclaration: System.Int32 i (OperationKind.VariableDeclaration)
        Initializer: ILiteralExpression (Text: 0) (OperationKind.LiteralExpression, Type: System.Int32, Constant: 0)
      IVariableDeclaration: System.Int32 j (OperationKind.VariableDeclaration)
        Initializer: ILiteralExpression (Text: 1) (OperationKind.LiteralExpression, Type: System.Int32, Constant: 1)
  AtLoopBottom: IExpressionStatement (OperationKind.ExpressionStatement)
      IAssignmentExpression (OperationKind.AssignmentExpression, Type: System.Int32)
        Left: ILocalReferenceExpression: i (OperationKind.LocalReferenceExpression, Type: System.Int32)
        Right: IBinaryOperatorExpression (BinaryOperationKind.IntegerAdd) (OperationKind.BinaryOperatorExpression, Type: System.Int32)
            Left: ILocalReferenceExpression: i (OperationKind.LocalReferenceExpression, Type: System.Int32)
            Right: ILiteralExpression (Text: 1) (OperationKind.LiteralExpression, Type: System.Int32, Constant: 1)
  IBlockStatement (1 statements) (OperationKind.BlockStatement)
    IExpressionStatement (OperationKind.ExpressionStatement)
      IAssignmentExpression (OperationKind.AssignmentExpression, Type: System.Int32)
        Left: ILocalReferenceExpression: j (OperationKind.LocalReferenceExpression, Type: System.Int32)
        Right: ILiteralExpression (Text: 2) (OperationKind.LiteralExpression, Type: System.Int32, Constant: 2)
";
            VerifyOperationTreeForTest<ForStatementSyntax>(source, expectedOperationTree);
        }

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
IForLoopStatement (LoopKind.For) (OperationKind.LoopStatement)
  Condition: IBinaryOperatorExpression (BinaryOperationKind.IntegerLessThan) (OperationKind.BinaryOperatorExpression, Type: System.Boolean)
      Left: ILocalReferenceExpression: i (OperationKind.LocalReferenceExpression, Type: System.Int32)
      Right: IBinaryOperatorExpression (BinaryOperationKind.IntegerSubtract) (OperationKind.BinaryOperatorExpression, Type: System.Int32)
          Left: ILiteralExpression (Text: 50) (OperationKind.LiteralExpression, Type: System.Int32, Constant: 50)
          Right: ILocalReferenceExpression: x (OperationKind.LocalReferenceExpression, Type: System.Int32)
  Local_1: System.Int32 i
  Before: IVariableDeclarationStatement (1 variables) (OperationKind.VariableDeclarationStatement)
      IVariableDeclaration: System.Int32 i (OperationKind.VariableDeclaration)
        Initializer: ILiteralExpression (Text: 0) (OperationKind.LiteralExpression, Type: System.Int32, Constant: 0)
  AtLoopBottom: IExpressionStatement (OperationKind.ExpressionStatement)
      IAssignmentExpression (OperationKind.AssignmentExpression, Type: System.Int32)
        Left: ILocalReferenceExpression: i (OperationKind.LocalReferenceExpression, Type: System.Int32)
        Right: IBinaryOperatorExpression (BinaryOperationKind.IntegerAdd) (OperationKind.BinaryOperatorExpression, Type: System.Int32)
            Left: ILocalReferenceExpression: i (OperationKind.LocalReferenceExpression, Type: System.Int32)
            Right: ILiteralExpression (Text: 1) (OperationKind.LiteralExpression, Type: System.Int32, Constant: 1)
  IBlockStatement (2 statements) (OperationKind.BlockStatement)
    IExpressionStatement (OperationKind.ExpressionStatement)
      IAssignmentExpression (OperationKind.AssignmentExpression, Type: System.Int32)
        Left: ILocalReferenceExpression: x (OperationKind.LocalReferenceExpression, Type: System.Int32)
        Right: IBinaryOperatorExpression (BinaryOperationKind.IntegerAdd) (OperationKind.BinaryOperatorExpression, Type: System.Int32)
            Left: ILocalReferenceExpression: x (OperationKind.LocalReferenceExpression, Type: System.Int32)
            Right: ILiteralExpression (Text: 1) (OperationKind.LiteralExpression, Type: System.Int32, Constant: 1)
    IExpressionStatement (OperationKind.ExpressionStatement)
      IAssignmentExpression (OperationKind.AssignmentExpression, Type: System.Int32)
        Left: ILocalReferenceExpression: c (OperationKind.LocalReferenceExpression, Type: System.Int32)
        Right: IBinaryOperatorExpression (BinaryOperationKind.IntegerAdd) (OperationKind.BinaryOperatorExpression, Type: System.Int32)
            Left: ILocalReferenceExpression: c (OperationKind.LocalReferenceExpression, Type: System.Int32)
            Right: ILiteralExpression (Text: 1) (OperationKind.LiteralExpression, Type: System.Int32, Constant: 1)
";
            VerifyOperationTreeForTest<ForStatementSyntax>(source, expectedOperationTree);
        }

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
IForLoopStatement (LoopKind.For) (OperationKind.LoopStatement)
  Condition: ILiteralExpression (OperationKind.LiteralExpression, Type: System.Boolean, Constant: False)
  IBlockStatement (1 statements) (OperationKind.BlockStatement)
    IExpressionStatement (OperationKind.ExpressionStatement)
      IInvocationExpression (static void System.Console.WriteLine(System.String value)) (OperationKind.InvocationExpression, Type: System.Void)
        IArgument (Matching Parameter: value) (OperationKind.Argument)
          ILiteralExpression (OperationKind.LiteralExpression, Type: System.String, Constant: hello)
";
            VerifyOperationTreeForTest<ForStatementSyntax>(source, expectedOperationTree);
        }

        [Fact, WorkItem(17602, "https://github.com/dotnet/roslyn/issues/17602")]
        public void IForLoopStatement_ObjectInitAsInitializer()
        {
            string source = @"
class C
{
    static void Main(string[] args)
    {
        /*<bind>*/for (Foo f = new Foo { i = 0, s = ""abc"" }; f.i < 5; f.i = f.i + 1)
        {
        }/*</bind>*/
    }
}
public class Foo
{
    public int i;
    public string s;
}

";
            string expectedOperationTree = @"
IForLoopStatement (LoopKind.For) (OperationKind.LoopStatement)
  Condition: IBinaryOperatorExpression (BinaryOperationKind.IntegerLessThan) (OperationKind.BinaryOperatorExpression, Type: System.Boolean)
      Left: IFieldReferenceExpression: System.Int32 Foo.i (OperationKind.FieldReferenceExpression, Type: System.Int32)
          Instance Receiver: ILocalReferenceExpression: f (OperationKind.LocalReferenceExpression, Type: Foo)
      Right: ILiteralExpression (Text: 5) (OperationKind.LiteralExpression, Type: System.Int32, Constant: 5)
  Local_1: Foo f
  Before: IVariableDeclarationStatement (1 variables) (OperationKind.VariableDeclarationStatement)
      IVariableDeclaration: Foo f (OperationKind.VariableDeclaration)
        Initializer: IObjectCreationExpression (Constructor: Foo..ctor()) (OperationKind.ObjectCreationExpression, Type: Foo)
            Member Initializers: IFieldInitializer (Field: System.Int32 Foo.i) (OperationKind.FieldInitializerInCreation)
                ILiteralExpression (Text: 0) (OperationKind.LiteralExpression, Type: System.Int32, Constant: 0)
              IFieldInitializer (Field: System.String Foo.s) (OperationKind.FieldInitializerInCreation)
                ILiteralExpression (OperationKind.LiteralExpression, Type: System.String, Constant: abc)
  AtLoopBottom: IExpressionStatement (OperationKind.ExpressionStatement)
      IAssignmentExpression (OperationKind.AssignmentExpression, Type: System.Int32)
        Left: IFieldReferenceExpression: System.Int32 Foo.i (OperationKind.FieldReferenceExpression, Type: System.Int32)
            Instance Receiver: ILocalReferenceExpression: f (OperationKind.LocalReferenceExpression, Type: Foo)
        Right: IBinaryOperatorExpression (BinaryOperationKind.IntegerAdd) (OperationKind.BinaryOperatorExpression, Type: System.Int32)
            Left: IFieldReferenceExpression: System.Int32 Foo.i (OperationKind.FieldReferenceExpression, Type: System.Int32)
                Instance Receiver: ILocalReferenceExpression: f (OperationKind.LocalReferenceExpression, Type: Foo)
            Right: ILiteralExpression (Text: 1) (OperationKind.LiteralExpression, Type: System.Int32, Constant: 1)
  IBlockStatement (0 statements) (OperationKind.BlockStatement)
";
            VerifyOperationTreeForTest<ForStatementSyntax>(source, expectedOperationTree);
        }

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
IForLoopStatement (LoopKind.For) (OperationKind.LoopStatement)
  Condition: IUnaryOperatorExpression (UnaryOperationKind.DynamicTrue) (OperationKind.UnaryOperatorExpression, Type: System.Boolean)
      IOperation:  (OperationKind.None)
  Before: IExpressionStatement (OperationKind.ExpressionStatement)
      IOperation:  (OperationKind.None)
  AtLoopBottom: IExpressionStatement (OperationKind.ExpressionStatement)
      IOperation:  (OperationKind.None)
  IBlockStatement (0 statements) (OperationKind.BlockStatement)
";
            VerifyOperationTreeForTest<ForStatementSyntax>(source, expectedOperationTree);
        }

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
IForLoopStatement (LoopKind.For) (OperationKind.LoopStatement)
  Condition: IBinaryOperatorExpression (BinaryOperationKind.IntegerLessThan) (OperationKind.BinaryOperatorExpression, Type: System.Boolean)
      Left: ILocalReferenceExpression: i (OperationKind.LocalReferenceExpression, Type: System.Int32)
      Right: ILiteralExpression (Text: 5) (OperationKind.LiteralExpression, Type: System.Int32, Constant: 5)
  Local_1: System.Int32 i
  Before: IVariableDeclarationStatement (1 variables) (OperationKind.VariableDeclarationStatement)
      IVariableDeclaration: System.Int32 i (OperationKind.VariableDeclaration)
        Initializer: ILiteralExpression (Text: 1) (OperationKind.LiteralExpression, Type: System.Int32, Constant: 1)
  AtLoopBottom: IExpressionStatement (OperationKind.ExpressionStatement)
      IAssignmentExpression (OperationKind.AssignmentExpression, Type: System.Int32)
        Left: ILocalReferenceExpression: i (OperationKind.LocalReferenceExpression, Type: System.Int32)
        Right: IBinaryOperatorExpression (BinaryOperationKind.IntegerAdd) (OperationKind.BinaryOperatorExpression, Type: System.Int32)
            Left: ILocalReferenceExpression: i (OperationKind.LocalReferenceExpression, Type: System.Int32)
            Right: ILiteralExpression (Text: 1) (OperationKind.LiteralExpression, Type: System.Int32, Constant: 1)
  IEmptyStatement (OperationKind.EmptyStatement)
";
            VerifyOperationTreeForTest<ForStatementSyntax>(source, expectedOperationTree);
        }

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
IForLoopStatement (LoopKind.For) (OperationKind.LoopStatement)
  Local_1: System.Collections.Generic.IEnumerable<System.String> str
  Before: IVariableDeclarationStatement (1 variables) (OperationKind.VariableDeclarationStatement)
      IVariableDeclaration: System.Collections.Generic.IEnumerable<System.String> str (OperationKind.VariableDeclaration)
        Initializer: IOperation:  (OperationKind.None)
  IBlockStatement (2 statements) (OperationKind.BlockStatement)
    IForEachLoopStatement (Iteration variable: System.String item) (LoopKind.ForEach) (OperationKind.LoopStatement)
      Collection: IConversionExpression (ConversionKind.Cast, Implicit) (OperationKind.ConversionExpression, Type: System.Collections.Generic.IEnumerable<System.String>)
          ILocalReferenceExpression: str (OperationKind.LocalReferenceExpression, Type: System.Collections.Generic.IEnumerable<System.String>)
      IBlockStatement (1 statements) (OperationKind.BlockStatement)
        IExpressionStatement (OperationKind.ExpressionStatement)
          IInvocationExpression (static void System.Console.WriteLine(System.String value)) (OperationKind.InvocationExpression, Type: System.Void)
            IArgument (Matching Parameter: value) (OperationKind.Argument)
              ILocalReferenceExpression: item (OperationKind.LocalReferenceExpression, Type: System.String)
    IReturnStatement (OperationKind.ReturnStatement)
";
            VerifyOperationTreeForTest<ForStatementSyntax>(source, expectedOperationTree);
        }

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
IForLoopStatement (LoopKind.For) (OperationKind.LoopStatement)
  Condition: IBinaryOperatorExpression (BinaryOperationKind.IntegerLessThan) (OperationKind.BinaryOperatorExpression, Type: System.Boolean)
      Left: ILocalReferenceExpression: i (OperationKind.LocalReferenceExpression, Type: System.Int32)
      Right: ILiteralExpression (Text: 5) (OperationKind.LiteralExpression, Type: System.Int32, Constant: 5)
  Local_1: System.Int32 i
  Before: IVariableDeclarationStatement (1 variables) (OperationKind.VariableDeclarationStatement)
      IVariableDeclaration: System.Int32 i (OperationKind.VariableDeclaration)
        Initializer: ILiteralExpression (Text: 0) (OperationKind.LiteralExpression, Type: System.Int32, Constant: 0)
  IBlockStatement (1 statements) (OperationKind.BlockStatement)
    IReturnStatement (OperationKind.ReturnStatement)
      IOperation:  (OperationKind.None)
";
            VerifyOperationTreeForTest<ForStatementSyntax>(source, expectedOperationTree);
        }

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
IForLoopStatement (LoopKind.For) (OperationKind.LoopStatement)
  Condition: IBinaryOperatorExpression (BinaryOperationKind.IntegerLessThan) (OperationKind.BinaryOperatorExpression, Type: System.Boolean)
      Left: ILocalReferenceExpression: i (OperationKind.LocalReferenceExpression, Type: System.Int32)
      Right: ILiteralExpression (Text: 5) (OperationKind.LiteralExpression, Type: System.Int32, Constant: 5)
  Before: IExpressionStatement (OperationKind.ExpressionStatement)
      IAssignmentExpression (OperationKind.AssignmentExpression, Type: System.Linq.Expressions.Expression<System.Func<System.Int32, System.Int32>>)
        Left: ILocalReferenceExpression: e (OperationKind.LocalReferenceExpression, Type: System.Linq.Expressions.Expression<System.Func<System.Int32, System.Int32>>)
        Right: IConversionExpression (ConversionKind.CSharp, Implicit) (OperationKind.ConversionExpression, Type: System.Linq.Expressions.Expression<System.Func<System.Int32, System.Int32>>)
            ILambdaExpression (Signature: lambda expression) (OperationKind.LambdaExpression, Type: System.Linq.Expressions.Expression<System.Func<System.Int32, System.Int32>>)
              IBlockStatement (1 statements) (OperationKind.BlockStatement)
                IReturnStatement (OperationKind.ReturnStatement)
                  IBinaryOperatorExpression (BinaryOperationKind.IntegerMultiply) (OperationKind.BinaryOperatorExpression, Type: System.Int32)
                    Left: IParameterReferenceExpression: x (OperationKind.ParameterReferenceExpression, Type: System.Int32)
                    Right: IParameterReferenceExpression: x (OperationKind.ParameterReferenceExpression, Type: System.Int32)
  AtLoopBottom: IExpressionStatement (OperationKind.ExpressionStatement)
      IIncrementExpression (UnaryOperandKind.IntegerPostfixIncrement) (BinaryOperationKind.IntegerAdd) (OperationKind.IncrementExpression, Type: System.Int32)
        Left: ILocalReferenceExpression: i (OperationKind.LocalReferenceExpression, Type: System.Int32)
        Right: ILiteralExpression (OperationKind.LiteralExpression, Type: System.Int32, Constant: 1)
  IBlockStatement (2 statements, 1 locals) (OperationKind.BlockStatement)
    Local_1: System.Func<System.Int32, System.Int32> lambda
    IVariableDeclarationStatement (1 variables) (OperationKind.VariableDeclarationStatement)
      IVariableDeclaration: System.Func<System.Int32, System.Int32> lambda (OperationKind.VariableDeclaration)
        Initializer: IInvocationExpression ( System.Func<System.Int32, System.Int32> System.Linq.Expressions.Expression<System.Func<System.Int32, System.Int32>>.Compile()) (OperationKind.InvocationExpression, Type: System.Func<System.Int32, System.Int32>)
            Instance Receiver: ILocalReferenceExpression: e (OperationKind.LocalReferenceExpression, Type: System.Linq.Expressions.Expression<System.Func<System.Int32, System.Int32>>)
    IExpressionStatement (OperationKind.ExpressionStatement)
      IInvocationExpression (static void System.Console.WriteLine(System.Int32 value)) (OperationKind.InvocationExpression, Type: System.Void)
        IArgument (Matching Parameter: value) (OperationKind.Argument)
          IInvocationExpression (virtual System.Int32 System.Func<System.Int32, System.Int32>.Invoke(System.Int32 arg)) (OperationKind.InvocationExpression, Type: System.Int32)
            Instance Receiver: ILocalReferenceExpression: lambda (OperationKind.LocalReferenceExpression, Type: System.Func<System.Int32, System.Int32>)
            IArgument (Matching Parameter: arg) (OperationKind.Argument)
              ILocalReferenceExpression: i (OperationKind.LocalReferenceExpression, Type: System.Int32)
";
            VerifyOperationTreeForTest<ForStatementSyntax>(source, expectedOperationTree);
        }

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
IForLoopStatement (LoopKind.For) (OperationKind.LoopStatement)
  Condition: IBinaryOperatorExpression (BinaryOperationKind.IntegerLessThan) (OperationKind.BinaryOperatorExpression, Type: System.Boolean)
      Left: ILocalReferenceExpression: i (OperationKind.LocalReferenceExpression, Type: System.Int32)
      Right: ILiteralExpression (Text: 5) (OperationKind.LiteralExpression, Type: System.Int32, Constant: 5)
  Local_1: System.Int32 i
  Before: IVariableDeclarationStatement (1 variables) (OperationKind.VariableDeclarationStatement)
      IVariableDeclaration: System.Int32 i (OperationKind.VariableDeclaration)
        Initializer: ILiteralExpression (Text: 1) (OperationKind.LiteralExpression, Type: System.Int32, Constant: 1)
  AtLoopBottom: IExpressionStatement (OperationKind.ExpressionStatement)
      IAssignmentExpression (OperationKind.AssignmentExpression, Type: System.Linq.Expressions.Expression<System.Func<System.Int32, System.Int32>>)
        Left: ILocalReferenceExpression: e (OperationKind.LocalReferenceExpression, Type: System.Linq.Expressions.Expression<System.Func<System.Int32, System.Int32>>)
        Right: IConversionExpression (ConversionKind.CSharp, Implicit) (OperationKind.ConversionExpression, Type: System.Linq.Expressions.Expression<System.Func<System.Int32, System.Int32>>)
            ILambdaExpression (Signature: lambda expression) (OperationKind.LambdaExpression, Type: System.Linq.Expressions.Expression<System.Func<System.Int32, System.Int32>>)
              IBlockStatement (1 statements) (OperationKind.BlockStatement)
                IReturnStatement (OperationKind.ReturnStatement)
                  IBinaryOperatorExpression (BinaryOperationKind.IntegerMultiply) (OperationKind.BinaryOperatorExpression, Type: System.Int32)
                    Left: IParameterReferenceExpression: x (OperationKind.ParameterReferenceExpression, Type: System.Int32)
                    Right: IParameterReferenceExpression: x (OperationKind.ParameterReferenceExpression, Type: System.Int32)
    IExpressionStatement (OperationKind.ExpressionStatement)
      IAssignmentExpression (OperationKind.AssignmentExpression, Type: System.Int32)
        Left: ILocalReferenceExpression: i (OperationKind.LocalReferenceExpression, Type: System.Int32)
        Right: IBinaryOperatorExpression (BinaryOperationKind.IntegerAdd) (OperationKind.BinaryOperatorExpression, Type: System.Int32)
            Left: ILocalReferenceExpression: i (OperationKind.LocalReferenceExpression, Type: System.Int32)
            Right: ILiteralExpression (Text: 1) (OperationKind.LiteralExpression, Type: System.Int32, Constant: 1)
  IBlockStatement (2 statements, 1 locals) (OperationKind.BlockStatement)
    Local_1: System.Func<System.Int32, System.Int32> lambda
    IVariableDeclarationStatement (1 variables) (OperationKind.VariableDeclarationStatement)
      IVariableDeclaration: System.Func<System.Int32, System.Int32> lambda (OperationKind.VariableDeclaration)
        Initializer: IInvocationExpression ( System.Func<System.Int32, System.Int32> System.Linq.Expressions.Expression<System.Func<System.Int32, System.Int32>>.Compile()) (OperationKind.InvocationExpression, Type: System.Func<System.Int32, System.Int32>)
            Instance Receiver: ILocalReferenceExpression: e (OperationKind.LocalReferenceExpression, Type: System.Linq.Expressions.Expression<System.Func<System.Int32, System.Int32>>)
    IExpressionStatement (OperationKind.ExpressionStatement)
      IInvocationExpression (static void System.Console.WriteLine(System.Int32 value)) (OperationKind.InvocationExpression, Type: System.Void)
        IArgument (Matching Parameter: value) (OperationKind.Argument)
          IInvocationExpression (virtual System.Int32 System.Func<System.Int32, System.Int32>.Invoke(System.Int32 arg)) (OperationKind.InvocationExpression, Type: System.Int32)
            Instance Receiver: ILocalReferenceExpression: lambda (OperationKind.LocalReferenceExpression, Type: System.Func<System.Int32, System.Int32>)
            IArgument (Matching Parameter: arg) (OperationKind.Argument)
              ILocalReferenceExpression: i (OperationKind.LocalReferenceExpression, Type: System.Int32)
";
            VerifyOperationTreeForTest<ForStatementSyntax>(source, expectedOperationTree);
        }

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
IForLoopStatement (LoopKind.For) (OperationKind.LoopStatement)
  Condition: IBinaryOperatorExpression (BinaryOperationKind.ObjectEquals) (OperationKind.BinaryOperatorExpression, Type: System.Boolean)
      Left: IConversionExpression (ConversionKind.Cast, Implicit) (OperationKind.ConversionExpression, Type: System.Object)
          ILocalReferenceExpression: i (OperationKind.LocalReferenceExpression, Type: C1)
      Right: IConversionExpression (ConversionKind.Cast, Implicit) (OperationKind.ConversionExpression, Type: System.Object, Constant: null)
          ILiteralExpression (Text: null) (OperationKind.LiteralExpression, Type: null, Constant: null)
  Local_1: C1 i
  Before: IVariableDeclarationStatement (1 variables) (OperationKind.VariableDeclarationStatement)
      IVariableDeclaration: C1 i (OperationKind.VariableDeclaration)
        Initializer: IObjectCreationExpression (Constructor: C1..ctor()) (OperationKind.ObjectCreationExpression, Type: C1)
  AtLoopBottom: IExpressionStatement (OperationKind.ExpressionStatement)
      IIncrementExpression (UnaryOperandKind.OperatorMethodPostfixIncrement) (BinaryOperationKind.OperatorMethodAdd) (OperatorMethod: C1 C1.op_Increment(C1 obj)) (OperationKind.IncrementExpression, Type: C1)
        Left: ILocalReferenceExpression: i (OperationKind.LocalReferenceExpression, Type: C1)
        Right: ILiteralExpression (OperationKind.LiteralExpression, Type: C1, Constant: null)
  IBlockStatement (0 statements) (OperationKind.BlockStatement)
";
            VerifyOperationTreeForTest<ForStatementSyntax>(source, expectedOperationTree);
        }

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
IForLoopStatement (LoopKind.For) (OperationKind.LoopStatement)
  Condition: IBinaryOperatorExpression (BinaryOperationKind.IntegerLessThan) (OperationKind.BinaryOperatorExpression, Type: System.Boolean)
      Left: ILocalReferenceExpression: j (OperationKind.LocalReferenceExpression, Type: System.Int32)
      Right: ILiteralExpression (Text: 5) (OperationKind.LiteralExpression, Type: System.Int32, Constant: 5)
  Local_1: System.Int32 j
  Before: IVariableDeclarationStatement (1 variables) (OperationKind.VariableDeclarationStatement)
      IVariableDeclaration: System.Int32 j (OperationKind.VariableDeclaration)
        Initializer: IIncrementExpression (UnaryOperandKind.IntegerPostfixIncrement) (BinaryOperationKind.IntegerAdd) (OperationKind.IncrementExpression, Type: System.Int32)
            Left: ILocalReferenceExpression: i (OperationKind.LocalReferenceExpression, Type: System.Int32)
            Right: ILiteralExpression (OperationKind.LiteralExpression, Type: System.Int32, Constant: 1)
  AtLoopBottom: IExpressionStatement (OperationKind.ExpressionStatement)
      IIncrementExpression (UnaryOperandKind.IntegerPrefixIncrement) (BinaryOperationKind.IntegerAdd) (OperationKind.IncrementExpression, Type: System.Int32)
        Left: ILocalReferenceExpression: j (OperationKind.LocalReferenceExpression, Type: System.Int32)
        Right: ILiteralExpression (OperationKind.LiteralExpression, Type: System.Int32, Constant: 1)
  IBlockStatement (1 statements) (OperationKind.BlockStatement)
    IExpressionStatement (OperationKind.ExpressionStatement)
      IInvocationExpression (static void System.Console.WriteLine(System.Int32 value)) (OperationKind.InvocationExpression, Type: System.Void)
        IArgument (Matching Parameter: value) (OperationKind.Argument)
          ILocalReferenceExpression: j (OperationKind.LocalReferenceExpression, Type: System.Int32)
";
            VerifyOperationTreeForTest<ForStatementSyntax>(source, expectedOperationTree);
        }

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
IForLoopStatement (LoopKind.For) (OperationKind.LoopStatement)
  Condition: IBinaryOperatorExpression (BinaryOperationKind.IntegerLessThan) (OperationKind.BinaryOperatorExpression, Type: System.Boolean)
      Left: ILocalReferenceExpression: j (OperationKind.LocalReferenceExpression, Type: System.Int32)
      Right: ILiteralExpression (Text: 5) (OperationKind.LiteralExpression, Type: System.Int32, Constant: 5)
  Local_1: System.Int32 j
  Before: IVariableDeclarationStatement (1 variables) (OperationKind.VariableDeclarationStatement)
      IVariableDeclaration: System.Int32 j (OperationKind.VariableDeclaration)
        Initializer: IIncrementExpression (UnaryOperandKind.IntegerPrefixIncrement) (BinaryOperationKind.IntegerAdd) (OperationKind.IncrementExpression, Type: System.Int32)
            Left: ILocalReferenceExpression: i (OperationKind.LocalReferenceExpression, Type: System.Int32)
            Right: ILiteralExpression (OperationKind.LiteralExpression, Type: System.Int32, Constant: 1)
  AtLoopBottom: IExpressionStatement (OperationKind.ExpressionStatement)
      IIncrementExpression (UnaryOperandKind.IntegerPrefixIncrement) (BinaryOperationKind.IntegerAdd) (OperationKind.IncrementExpression, Type: System.Int32)
        Left: ILocalReferenceExpression: j (OperationKind.LocalReferenceExpression, Type: System.Int32)
        Right: ILiteralExpression (OperationKind.LiteralExpression, Type: System.Int32, Constant: 1)
  IBlockStatement (1 statements) (OperationKind.BlockStatement)
    IExpressionStatement (OperationKind.ExpressionStatement)
      IInvocationExpression (static void System.Console.WriteLine(System.Int32 value)) (OperationKind.InvocationExpression, Type: System.Void)
        IArgument (Matching Parameter: value) (OperationKind.Argument)
          ILocalReferenceExpression: j (OperationKind.LocalReferenceExpression, Type: System.Int32)
";
            VerifyOperationTreeForTest<ForStatementSyntax>(source, expectedOperationTree);
        }

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
IForLoopStatement (LoopKind.For) (OperationKind.LoopStatement)
  Condition: IBinaryOperatorExpression (BinaryOperationKind.IntegerLessThan) (OperationKind.BinaryOperatorExpression, Type: System.Boolean)
      Left: IIncrementExpression (UnaryOperandKind.IntegerPrefixIncrement) (BinaryOperationKind.IntegerAdd) (OperationKind.IncrementExpression, Type: System.Int32)
          Left: ILocalReferenceExpression: i (OperationKind.LocalReferenceExpression, Type: System.Int32)
          Right: ILiteralExpression (OperationKind.LiteralExpression, Type: System.Int32, Constant: 1)
      Right: ILiteralExpression (Text: 5) (OperationKind.LiteralExpression, Type: System.Int32, Constant: 5)
  Local_1: System.Int32 i
  Before: IVariableDeclarationStatement (1 variables) (OperationKind.VariableDeclarationStatement)
      IVariableDeclaration: System.Int32 i (OperationKind.VariableDeclaration)
        Initializer: ILiteralExpression (Text: 0) (OperationKind.LiteralExpression, Type: System.Int32, Constant: 0)
  IBlockStatement (1 statements) (OperationKind.BlockStatement)
    IExpressionStatement (OperationKind.ExpressionStatement)
      IInvocationExpression (static void System.Console.WriteLine(System.Int32 value)) (OperationKind.InvocationExpression, Type: System.Void)
        IArgument (Matching Parameter: value) (OperationKind.Argument)
          ILocalReferenceExpression: i (OperationKind.LocalReferenceExpression, Type: System.Int32)
";
            VerifyOperationTreeForTest<ForStatementSyntax>(source, expectedOperationTree);
        }

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
IForLoopStatement (LoopKind.For) (OperationKind.LoopStatement)
  Condition: IBinaryOperatorExpression (BinaryOperationKind.IntegerGreaterThan) (OperationKind.BinaryOperatorExpression, Type: System.Boolean)
      Left: IInvocationExpression (static System.Int32 Program.foo(System.Int32 x)) (OperationKind.InvocationExpression, Type: System.Int32)
          IArgument (Matching Parameter: x) (OperationKind.Argument)
            IIncrementExpression (UnaryOperandKind.IntegerPostfixDecrement) (BinaryOperationKind.IntegerSubtract) (OperationKind.IncrementExpression, Type: System.Int32)
              Left: ILocalReferenceExpression: i (OperationKind.LocalReferenceExpression, Type: System.Int32)
              Right: ILiteralExpression (OperationKind.LiteralExpression, Type: System.Int32, Constant: 1)
      Right: IUnaryOperatorExpression (UnaryOperationKind.IntegerMinus) (OperationKind.UnaryOperatorExpression, Type: System.Int32, Constant: -5)
          ILiteralExpression (Text: 5) (OperationKind.LiteralExpression, Type: System.Int32, Constant: 5)
  Local_1: System.Int32 i
  Before: IVariableDeclarationStatement (1 variables) (OperationKind.VariableDeclarationStatement)
      IVariableDeclaration: System.Int32 i (OperationKind.VariableDeclaration)
        Initializer: ILiteralExpression (Text: 0) (OperationKind.LiteralExpression, Type: System.Int32, Constant: 0)
  IBlockStatement (1 statements) (OperationKind.BlockStatement)
    IExpressionStatement (OperationKind.ExpressionStatement)
      IInvocationExpression (static void System.Console.WriteLine(System.Int32 value)) (OperationKind.InvocationExpression, Type: System.Void)
        IArgument (Matching Parameter: value) (OperationKind.Argument)
          ILocalReferenceExpression: i (OperationKind.LocalReferenceExpression, Type: System.Int32)
";
            VerifyOperationTreeForTest<ForStatementSyntax>(source, expectedOperationTree);
        }

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
IForLoopStatement (LoopKind.For) (OperationKind.LoopStatement)
  Condition: ILiteralExpression (OperationKind.LiteralExpression, Type: System.Boolean, Constant: True)
  IBlockStatement (1 statements) (OperationKind.BlockStatement)
    IExpressionStatement (OperationKind.ExpressionStatement)
      IInvocationExpression (static void System.Console.WriteLine(System.String value)) (OperationKind.InvocationExpression, Type: System.Void)
        IArgument (Matching Parameter: value) (OperationKind.Argument)
          ILiteralExpression (OperationKind.LiteralExpression, Type: System.String, Constant: z)
";
            VerifyOperationTreeForTest<ForStatementSyntax>(source, expectedOperationTree);
        }

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
IForLoopStatement (LoopKind.For) (OperationKind.LoopStatement, IsInvalid)
  Condition: IBinaryOperatorExpression (BinaryOperationKind.IntegerLessThan) (OperationKind.BinaryOperatorExpression, Type: System.Boolean)
      Left: ILocalReferenceExpression: k (OperationKind.LocalReferenceExpression, Type: System.Int32)
      Right: ILiteralExpression (Text: 100) (OperationKind.LiteralExpression, Type: System.Int32, Constant: 100)
  Local_1: System.Int32 k
  Local_2: System.Int32 j
  Before: IVariableDeclarationStatement (2 variables) (OperationKind.VariableDeclarationStatement)
      IVariableDeclaration: System.Int32 k (OperationKind.VariableDeclaration)
        Initializer: ILiteralExpression (Text: 0) (OperationKind.LiteralExpression, Type: System.Int32, Constant: 0)
      IVariableDeclaration: System.Int32 j (OperationKind.VariableDeclaration)
        Initializer: ILiteralExpression (Text: 0) (OperationKind.LiteralExpression, Type: System.Int32, Constant: 0)
  AtLoopBottom: IExpressionStatement (OperationKind.ExpressionStatement, IsInvalid)
      IInvalidExpression (OperationKind.InvalidExpression, Type: ?, IsInvalid)
    IExpressionStatement (OperationKind.ExpressionStatement, IsInvalid)
      IBinaryOperatorExpression (BinaryOperationKind.IntegerGreaterThan) (OperationKind.BinaryOperatorExpression, Type: System.Boolean)
        Left: ILocalReferenceExpression: j (OperationKind.LocalReferenceExpression, Type: System.Int32)
        Right: ILiteralExpression (Text: 5) (OperationKind.LiteralExpression, Type: System.Int32, Constant: 5)
  IEmptyStatement (OperationKind.EmptyStatement)
";
            VerifyOperationTreeForTest<ForStatementSyntax>(source, expectedOperationTree);
        }

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
IForLoopStatement (LoopKind.For) (OperationKind.LoopStatement)
  Condition: IBinaryOperatorExpression (BinaryOperationKind.IntegerLessThan) (OperationKind.BinaryOperatorExpression, Type: System.Boolean)
      Left: ILocalReferenceExpression: i (OperationKind.LocalReferenceExpression, Type: System.Int32)
      Right: ILiteralExpression (Text: 10) (OperationKind.LiteralExpression, Type: System.Int32, Constant: 10)
  Local_1: System.Int32 j
  Local_2: System.Int32 i
  Before: IVariableDeclarationStatement (1 variables) (OperationKind.VariableDeclarationStatement)
      IVariableDeclaration: System.Int32 j (OperationKind.VariableDeclaration)
        Initializer: IConditionalChoiceExpression (OperationKind.ConditionalChoiceExpression, Type: System.Int32)
            Condition: IInvocationExpression (static System.Boolean System.Int32.TryParse(System.String s, out System.Int32 result)) (OperationKind.InvocationExpression, Type: System.Boolean)
                IArgument (Matching Parameter: s) (OperationKind.Argument)
                  ILocalReferenceExpression: s (OperationKind.LocalReferenceExpression, Type: System.String)
                IArgument (Matching Parameter: result) (OperationKind.Argument)
                  ILocalReferenceExpression: i (OperationKind.LocalReferenceExpression, Type: System.Int32)
            IfTrue: ILocalReferenceExpression: i (OperationKind.LocalReferenceExpression, Type: System.Int32)
            IfFalse: ILiteralExpression (Text: 0) (OperationKind.LiteralExpression, Type: System.Int32, Constant: 0)
  AtLoopBottom: IExpressionStatement (OperationKind.ExpressionStatement)
      IIncrementExpression (UnaryOperandKind.IntegerPostfixIncrement) (BinaryOperationKind.IntegerAdd) (OperationKind.IncrementExpression, Type: System.Int32)
        Left: ILocalReferenceExpression: i (OperationKind.LocalReferenceExpression, Type: System.Int32)
        Right: ILiteralExpression (OperationKind.LiteralExpression, Type: System.Int32, Constant: 1)
  IBlockStatement (1 statements) (OperationKind.BlockStatement)
    IExpressionStatement (OperationKind.ExpressionStatement)
      IInvocationExpression (static void System.Console.WriteLine(System.String value)) (OperationKind.InvocationExpression, Type: System.Void)
        IArgument (Matching Parameter: value) (OperationKind.Argument)
          IOperation:  (OperationKind.None)
";
            VerifyOperationTreeForTest<ForStatementSyntax>(source, expectedOperationTree);
        }

    }
}

