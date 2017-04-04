// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis.CSharp.Syntax;
using Roslyn.Test.Utilities;
using Xunit;


namespace Microsoft.CodeAnalysis.CSharp.UnitTests
{
    public partial class IOperationTests : SemanticModelTestBase
    {
        [Fact, WorkItem(17602, "https://github.com/dotnet/roslyn/issues/17602")]
        public void IWhileUntilLoopStatement_DoWhileLoopsTest()
        {
            string source = @"
class Program
{
    static void Main()
    {
        int[] ids = new int[] { 6, 7, 8, 10 };
        int sum = 0;
        int i = 0;
        /*<bind>*/do
        {
            sum += ids[i];
            i++;
        } while (i < 4);/*</bind>*/

        System.Console.WriteLine(sum);
    }
}
";
            string expectedOperationTree = @"
IWhileUntilLoopStatement (IsTopTest: False, IsWhile: True) (LoopKind.WhileUntil) (OperationKind.LoopStatement)
  Condition: IBinaryOperatorExpression (BinaryOperationKind.IntegerLessThan) (OperationKind.BinaryOperatorExpression, Type: System.Boolean)
      Left: ILocalReferenceExpression: i (OperationKind.LocalReferenceExpression, Type: System.Int32)
      Right: ILiteralExpression (Text: 4) (OperationKind.LiteralExpression, Type: System.Int32, Constant: 4)
  IBlockStatement (2 statements) (OperationKind.BlockStatement)
    IExpressionStatement (OperationKind.ExpressionStatement)
      ICompoundAssignmentExpression (BinaryOperationKind.IntegerAdd) (OperationKind.CompoundAssignmentExpression, Type: System.Int32)
        Left: ILocalReferenceExpression: sum (OperationKind.LocalReferenceExpression, Type: System.Int32)
        Right: IArrayElementReferenceExpression (OperationKind.ArrayElementReferenceExpression, Type: System.Int32)
            ILocalReferenceExpression: ids (OperationKind.LocalReferenceExpression, Type: System.Int32[])
            Indices: ILocalReferenceExpression: i (OperationKind.LocalReferenceExpression, Type: System.Int32)
    IExpressionStatement (OperationKind.ExpressionStatement)
      IIncrementExpression (UnaryOperandKind.IntegerPostfixIncrement) (BinaryOperationKind.IntegerAdd) (OperationKind.IncrementExpression, Type: System.Int32)
        Left: ILocalReferenceExpression: i (OperationKind.LocalReferenceExpression, Type: System.Int32)
        Right: ILiteralExpression (OperationKind.LiteralExpression, Type: System.Int32, Constant: 1)
";
            VerifyOperationTreeForTest<DoStatementSyntax>(source, expectedOperationTree);
        }

        [Fact, WorkItem(17602, "https://github.com/dotnet/roslyn/issues/17602")]
        public void IWhileUntilLoopStatement_WhileLoopsTest()
        {
            string source = @"
class Program
{
    static int SumWhile()
    {
        //
        // Sum numbers 0 .. 4
        //
        int sum = 0;
        int i = 0;
        /*<bind>*/while (i < 5)
        {
            sum += i;
            i++;
        }/*</bind>*/
        return sum;
    }
}
";
            string expectedOperationTree = @"
IWhileUntilLoopStatement (IsTopTest: True, IsWhile: True) (LoopKind.WhileUntil) (OperationKind.LoopStatement)
  Condition: IBinaryOperatorExpression (BinaryOperationKind.IntegerLessThan) (OperationKind.BinaryOperatorExpression, Type: System.Boolean)
      Left: ILocalReferenceExpression: i (OperationKind.LocalReferenceExpression, Type: System.Int32)
      Right: ILiteralExpression (Text: 5) (OperationKind.LiteralExpression, Type: System.Int32, Constant: 5)
  IBlockStatement (2 statements) (OperationKind.BlockStatement)
    IExpressionStatement (OperationKind.ExpressionStatement)
      ICompoundAssignmentExpression (BinaryOperationKind.IntegerAdd) (OperationKind.CompoundAssignmentExpression, Type: System.Int32)
        Left: ILocalReferenceExpression: sum (OperationKind.LocalReferenceExpression, Type: System.Int32)
        Right: ILocalReferenceExpression: i (OperationKind.LocalReferenceExpression, Type: System.Int32)
    IExpressionStatement (OperationKind.ExpressionStatement)
      IIncrementExpression (UnaryOperandKind.IntegerPostfixIncrement) (BinaryOperationKind.IntegerAdd) (OperationKind.IncrementExpression, Type: System.Int32)
        Left: ILocalReferenceExpression: i (OperationKind.LocalReferenceExpression, Type: System.Int32)
        Right: ILiteralExpression (OperationKind.LiteralExpression, Type: System.Int32, Constant: 1)
";
            VerifyOperationTreeForTest<WhileStatementSyntax>(source, expectedOperationTree);
        }

        [Fact, WorkItem(17602, "https://github.com/dotnet/roslyn/issues/17602")]
        public void IWhileUntilLoopStatement_WhileConditionTrue()
        {
            string source = @"
using System;

class Program
{
    static void Main()
    {
        int index = 0;
        bool condition = true;
        /*<bind>*/while (condition)
        {
            int value = ++index;
            if (value > 10)
            {
                condition = false;
            }
        }/*</bind>*/
    }
}
";
            string expectedOperationTree = @"
IWhileUntilLoopStatement (IsTopTest: True, IsWhile: True) (LoopKind.WhileUntil) (OperationKind.LoopStatement)
  Condition: ILocalReferenceExpression: condition (OperationKind.LocalReferenceExpression, Type: System.Boolean)
  IBlockStatement (2 statements, 1 locals) (OperationKind.BlockStatement)
    Local_1: System.Int32 value
    IVariableDeclarationStatement (1 variables) (OperationKind.VariableDeclarationStatement)
      IVariableDeclaration: System.Int32 value (OperationKind.VariableDeclaration)
        Initializer: IIncrementExpression (UnaryOperandKind.IntegerPrefixIncrement) (BinaryOperationKind.IntegerAdd) (OperationKind.IncrementExpression, Type: System.Int32)
            Left: ILocalReferenceExpression: index (OperationKind.LocalReferenceExpression, Type: System.Int32)
            Right: ILiteralExpression (OperationKind.LiteralExpression, Type: System.Int32, Constant: 1)
    IIfStatement (OperationKind.IfStatement)
      Condition: IBinaryOperatorExpression (BinaryOperationKind.IntegerGreaterThan) (OperationKind.BinaryOperatorExpression, Type: System.Boolean)
          Left: ILocalReferenceExpression: value (OperationKind.LocalReferenceExpression, Type: System.Int32)
          Right: ILiteralExpression (Text: 10) (OperationKind.LiteralExpression, Type: System.Int32, Constant: 10)
      IBlockStatement (1 statements) (OperationKind.BlockStatement)
        IExpressionStatement (OperationKind.ExpressionStatement)
          IAssignmentExpression (OperationKind.AssignmentExpression, Type: System.Boolean)
            Left: ILocalReferenceExpression: condition (OperationKind.LocalReferenceExpression, Type: System.Boolean)
            Right: ILiteralExpression (OperationKind.LiteralExpression, Type: System.Boolean, Constant: False)
";
            VerifyOperationTreeForTest<WhileStatementSyntax>(source, expectedOperationTree);
        }

        [Fact, WorkItem(17602, "https://github.com/dotnet/roslyn/issues/17602")]
        public void IWhileUntilLoopStatement_WhileWithBreak()
        {
            string source = @"
using System;

class Program
{
    static void Main()
    {
        int index = 0;
        /*<bind>*/while (true)
        {
            int value = ++index;
            if (value > 5)
            {
                Console.WriteLine(""While-loop break"");
                break;
            }
            Console.WriteLine(""While-loop statement"");
        }/*</bind>*/
    }
}
";
            string expectedOperationTree = @"
IWhileUntilLoopStatement (IsTopTest: True, IsWhile: True) (LoopKind.WhileUntil) (OperationKind.LoopStatement)
  Condition: ILiteralExpression (OperationKind.LiteralExpression, Type: System.Boolean, Constant: True)
  IBlockStatement (3 statements, 1 locals) (OperationKind.BlockStatement)
    Local_1: System.Int32 value
    IVariableDeclarationStatement (1 variables) (OperationKind.VariableDeclarationStatement)
      IVariableDeclaration: System.Int32 value (OperationKind.VariableDeclaration)
        Initializer: IIncrementExpression (UnaryOperandKind.IntegerPrefixIncrement) (BinaryOperationKind.IntegerAdd) (OperationKind.IncrementExpression, Type: System.Int32)
            Left: ILocalReferenceExpression: index (OperationKind.LocalReferenceExpression, Type: System.Int32)
            Right: ILiteralExpression (OperationKind.LiteralExpression, Type: System.Int32, Constant: 1)
    IIfStatement (OperationKind.IfStatement)
      Condition: IBinaryOperatorExpression (BinaryOperationKind.IntegerGreaterThan) (OperationKind.BinaryOperatorExpression, Type: System.Boolean)
          Left: ILocalReferenceExpression: value (OperationKind.LocalReferenceExpression, Type: System.Int32)
          Right: ILiteralExpression (Text: 5) (OperationKind.LiteralExpression, Type: System.Int32, Constant: 5)
      IBlockStatement (2 statements) (OperationKind.BlockStatement)
        IExpressionStatement (OperationKind.ExpressionStatement)
          IInvocationExpression (static void System.Console.WriteLine(System.String value)) (OperationKind.InvocationExpression, Type: System.Void)
            IArgument (Matching Parameter: value) (OperationKind.Argument)
              ILiteralExpression (OperationKind.LiteralExpression, Type: System.String, Constant: While-loop break)
        IBranchStatement (BranchKind.Break) (OperationKind.BranchStatement)
    IExpressionStatement (OperationKind.ExpressionStatement)
      IInvocationExpression (static void System.Console.WriteLine(System.String value)) (OperationKind.InvocationExpression, Type: System.Void)
        IArgument (Matching Parameter: value) (OperationKind.Argument)
          ILiteralExpression (OperationKind.LiteralExpression, Type: System.String, Constant: While-loop statement)
";
            VerifyOperationTreeForTest<WhileStatementSyntax>(source, expectedOperationTree);
        }

        [Fact, WorkItem(17602, "https://github.com/dotnet/roslyn/issues/17602")]
        public void IWhileUntilLoopStatement_WhileWithThrow()
        {
            string source = @"
using System;

class Program
{
    static void Main()
    {
        int index = 0;
        /*<bind>*/while (true)
        {
            int value = ++index;
            if (value > 100)
            {
                throw new Exception(""Never hit"");
            }
            Console.WriteLine(""While-loop statement"");
        }/*</bind>*/
    }
}
";
            string expectedOperationTree = @"
IWhileUntilLoopStatement (IsTopTest: True, IsWhile: True) (LoopKind.WhileUntil) (OperationKind.LoopStatement)
  Condition: ILiteralExpression (OperationKind.LiteralExpression, Type: System.Boolean, Constant: True)
  IBlockStatement (3 statements, 1 locals) (OperationKind.BlockStatement)
    Local_1: System.Int32 value
    IVariableDeclarationStatement (1 variables) (OperationKind.VariableDeclarationStatement)
      IVariableDeclaration: System.Int32 value (OperationKind.VariableDeclaration)
        Initializer: IIncrementExpression (UnaryOperandKind.IntegerPrefixIncrement) (BinaryOperationKind.IntegerAdd) (OperationKind.IncrementExpression, Type: System.Int32)
            Left: ILocalReferenceExpression: index (OperationKind.LocalReferenceExpression, Type: System.Int32)
            Right: ILiteralExpression (OperationKind.LiteralExpression, Type: System.Int32, Constant: 1)
    IIfStatement (OperationKind.IfStatement)
      Condition: IBinaryOperatorExpression (BinaryOperationKind.IntegerGreaterThan) (OperationKind.BinaryOperatorExpression, Type: System.Boolean)
          Left: ILocalReferenceExpression: value (OperationKind.LocalReferenceExpression, Type: System.Int32)
          Right: ILiteralExpression (Text: 100) (OperationKind.LiteralExpression, Type: System.Int32, Constant: 100)
      IBlockStatement (1 statements) (OperationKind.BlockStatement)
        IThrowStatement (OperationKind.ThrowStatement)
          IObjectCreationExpression (Constructor: System.Exception..ctor(System.String message)) (OperationKind.ObjectCreationExpression, Type: System.Exception)
            Arguments: IArgument (Matching Parameter: message) (OperationKind.Argument)
                ILiteralExpression (OperationKind.LiteralExpression, Type: System.String, Constant: Never hit)
    IExpressionStatement (OperationKind.ExpressionStatement)
      IInvocationExpression (static void System.Console.WriteLine(System.String value)) (OperationKind.InvocationExpression, Type: System.Void)
        IArgument (Matching Parameter: value) (OperationKind.Argument)
          ILiteralExpression (OperationKind.LiteralExpression, Type: System.String, Constant: While-loop statement)
";
            VerifyOperationTreeForTest<WhileStatementSyntax>(source, expectedOperationTree);
        }

        [Fact, WorkItem(17602, "https://github.com/dotnet/roslyn/issues/17602")]
        public void IWhileUntilLoopStatement_WhileWithAssignment()
        {
            string source = @"
using System;

class Program
{
    static void Main()
    {
        int value = 4;
        int i;
        /*<bind>*/while ((i = value) >= 0)
        {
             Console.WriteLine(""While {0} {1}"", i, value);
            value--;
        }/*</bind>*/
    }
}
";
            string expectedOperationTree = @"
IWhileUntilLoopStatement (IsTopTest: True, IsWhile: True) (LoopKind.WhileUntil) (OperationKind.LoopStatement)
  Condition: IBinaryOperatorExpression (BinaryOperationKind.IntegerGreaterThanOrEqual) (OperationKind.BinaryOperatorExpression, Type: System.Boolean)
      Left: IAssignmentExpression (OperationKind.AssignmentExpression, Type: System.Int32)
          Left: ILocalReferenceExpression: i (OperationKind.LocalReferenceExpression, Type: System.Int32)
          Right: ILocalReferenceExpression: value (OperationKind.LocalReferenceExpression, Type: System.Int32)
      Right: ILiteralExpression (Text: 0) (OperationKind.LiteralExpression, Type: System.Int32, Constant: 0)
  IBlockStatement (2 statements) (OperationKind.BlockStatement)
    IExpressionStatement (OperationKind.ExpressionStatement)
      IInvocationExpression (static void System.Console.WriteLine(System.String format, System.Object arg0, System.Object arg1)) (OperationKind.InvocationExpression, Type: System.Void)
        IArgument (Matching Parameter: format) (OperationKind.Argument)
          ILiteralExpression (OperationKind.LiteralExpression, Type: System.String, Constant: While {0} {1})
        IArgument (Matching Parameter: arg0) (OperationKind.Argument)
          IConversionExpression (ConversionKind.Cast, Implicit) (OperationKind.ConversionExpression, Type: System.Object)
            ILocalReferenceExpression: i (OperationKind.LocalReferenceExpression, Type: System.Int32)
        IArgument (Matching Parameter: arg1) (OperationKind.Argument)
          IConversionExpression (ConversionKind.Cast, Implicit) (OperationKind.ConversionExpression, Type: System.Object)
            ILocalReferenceExpression: value (OperationKind.LocalReferenceExpression, Type: System.Int32)
    IExpressionStatement (OperationKind.ExpressionStatement)
      IIncrementExpression (UnaryOperandKind.IntegerPostfixDecrement) (BinaryOperationKind.IntegerSubtract) (OperationKind.IncrementExpression, Type: System.Int32)
        Left: ILocalReferenceExpression: value (OperationKind.LocalReferenceExpression, Type: System.Int32)
        Right: ILiteralExpression (OperationKind.LiteralExpression, Type: System.Int32, Constant: 1)
";
            VerifyOperationTreeForTest<WhileStatementSyntax>(source, expectedOperationTree);
        }

        [Fact, WorkItem(17602, "https://github.com/dotnet/roslyn/issues/17602")]
        public void IWhileUntilLoopStatement_WhileInvalidCondition()
        {
            string source = @"
class Program
{
    static void Main()
    {
        int number = 10;
        /*<bind>*/while (number)
        {
        }/*</bind>*/
    }
}
";
            string expectedOperationTree = @"
IWhileUntilLoopStatement (IsTopTest: True, IsWhile: True) (LoopKind.WhileUntil) (OperationKind.LoopStatement, IsInvalid)
  Condition: IConversionExpression (ConversionKind.Invalid, Implicit) (OperationKind.ConversionExpression, Type: System.Boolean, IsInvalid)
      ILocalReferenceExpression: number (OperationKind.LocalReferenceExpression, Type: System.Int32)
  IBlockStatement (0 statements) (OperationKind.BlockStatement)
";
            VerifyOperationTreeForTest<WhileStatementSyntax>(source, expectedOperationTree);
        }

        [Fact, WorkItem(17602, "https://github.com/dotnet/roslyn/issues/17602")]
        public void IWhileUntilLoopStatement_WhileWithReturn()
        {
            string source = @"
class Program
{
    static void Main()
    {
        System.Console.WriteLine(GetFirstEvenNumber(33));
    }
    public static int GetFirstEvenNumber(int number)
    {
        /*<bind>*/while (true)
        {
            if ((number % 2) == 0)
            {
                return number;
            }
            number++;

        }/*</bind>*/
    }
}
";
            string expectedOperationTree = @"
IWhileUntilLoopStatement (IsTopTest: True, IsWhile: True) (LoopKind.WhileUntil) (OperationKind.LoopStatement)
  Condition: ILiteralExpression (OperationKind.LiteralExpression, Type: System.Boolean, Constant: True)
  IBlockStatement (2 statements) (OperationKind.BlockStatement)
    IIfStatement (OperationKind.IfStatement)
      Condition: IBinaryOperatorExpression (BinaryOperationKind.IntegerEquals) (OperationKind.BinaryOperatorExpression, Type: System.Boolean)
          Left: IBinaryOperatorExpression (BinaryOperationKind.IntegerRemainder) (OperationKind.BinaryOperatorExpression, Type: System.Int32)
              Left: IParameterReferenceExpression: number (OperationKind.ParameterReferenceExpression, Type: System.Int32)
              Right: ILiteralExpression (Text: 2) (OperationKind.LiteralExpression, Type: System.Int32, Constant: 2)
          Right: ILiteralExpression (Text: 0) (OperationKind.LiteralExpression, Type: System.Int32, Constant: 0)
      IBlockStatement (1 statements) (OperationKind.BlockStatement)
        IReturnStatement (OperationKind.ReturnStatement)
          IParameterReferenceExpression: number (OperationKind.ParameterReferenceExpression, Type: System.Int32)
    IExpressionStatement (OperationKind.ExpressionStatement)
      IIncrementExpression (UnaryOperandKind.IntegerPostfixIncrement) (BinaryOperationKind.IntegerAdd) (OperationKind.IncrementExpression, Type: System.Int32)
        Left: IParameterReferenceExpression: number (OperationKind.ParameterReferenceExpression, Type: System.Int32)
        Right: ILiteralExpression (OperationKind.LiteralExpression, Type: System.Int32, Constant: 1)
";
            VerifyOperationTreeForTest<WhileStatementSyntax>(source, expectedOperationTree);
        }

        [Fact, WorkItem(17602, "https://github.com/dotnet/roslyn/issues/17602")]
        public void IWhileUntilLoopStatement_WhileWithGoto()
        {
            string source = @"
class Program
{
    static void Main()
    {
        System.Console.WriteLine(GetFirstEvenNumber(33));
    }
    public static int GetFirstEvenNumber(int number)
    {
        /*<bind>*/while (true)
        {
            if ((number % 2) == 0)
            {
                goto Even;
            }
            number++;
        Even:
            return number;
        }/*</bind>*/
    }
}
";
            string expectedOperationTree = @"
IWhileUntilLoopStatement (IsTopTest: True, IsWhile: True) (LoopKind.WhileUntil) (OperationKind.LoopStatement)
  Condition: ILiteralExpression (OperationKind.LiteralExpression, Type: System.Boolean, Constant: True)
  IBlockStatement (3 statements) (OperationKind.BlockStatement)
    IIfStatement (OperationKind.IfStatement)
      Condition: IBinaryOperatorExpression (BinaryOperationKind.IntegerEquals) (OperationKind.BinaryOperatorExpression, Type: System.Boolean)
          Left: IBinaryOperatorExpression (BinaryOperationKind.IntegerRemainder) (OperationKind.BinaryOperatorExpression, Type: System.Int32)
              Left: IParameterReferenceExpression: number (OperationKind.ParameterReferenceExpression, Type: System.Int32)
              Right: ILiteralExpression (Text: 2) (OperationKind.LiteralExpression, Type: System.Int32, Constant: 2)
          Right: ILiteralExpression (Text: 0) (OperationKind.LiteralExpression, Type: System.Int32, Constant: 0)
      IBlockStatement (1 statements) (OperationKind.BlockStatement)
        IBranchStatement (BranchKind.GoTo, Label: Even) (OperationKind.BranchStatement)
    IExpressionStatement (OperationKind.ExpressionStatement)
      IIncrementExpression (UnaryOperandKind.IntegerPostfixIncrement) (BinaryOperationKind.IntegerAdd) (OperationKind.IncrementExpression, Type: System.Int32)
        Left: IParameterReferenceExpression: number (OperationKind.ParameterReferenceExpression, Type: System.Int32)
        Right: ILiteralExpression (OperationKind.LiteralExpression, Type: System.Int32, Constant: 1)
    ILabelStatement (Label: Even) (OperationKind.LabelStatement)
      IReturnStatement (OperationKind.ReturnStatement)
        IParameterReferenceExpression: number (OperationKind.ParameterReferenceExpression, Type: System.Int32)
";
            VerifyOperationTreeForTest<WhileStatementSyntax>(source, expectedOperationTree);
        }

        [Fact, WorkItem(17602, "https://github.com/dotnet/roslyn/issues/17602")]
        public void IWhileUntilLoopStatement_WhileMissingCondition()
        {
            string source = @"
class Program
{
    static void Main()
    {
        int index = 0;
        bool condition = true;
        /*<bind>*/while ()
        {
            int value = ++index;
            if (value > 100)
            {
                condition = false;
            }
        }/*</bind>*/
    }
}
";
            string expectedOperationTree = @"
IWhileUntilLoopStatement (IsTopTest: True, IsWhile: True) (LoopKind.WhileUntil) (OperationKind.LoopStatement, IsInvalid)
  Condition: IConversionExpression (ConversionKind.Invalid, Implicit) (OperationKind.ConversionExpression, Type: System.Boolean, IsInvalid)
      IInvalidExpression (OperationKind.InvalidExpression, Type: ?, IsInvalid)
  IBlockStatement (2 statements, 1 locals) (OperationKind.BlockStatement)
    Local_1: System.Int32 value
    IVariableDeclarationStatement (1 variables) (OperationKind.VariableDeclarationStatement)
      IVariableDeclaration: System.Int32 value (OperationKind.VariableDeclaration)
        Initializer: IIncrementExpression (UnaryOperandKind.IntegerPrefixIncrement) (BinaryOperationKind.IntegerAdd) (OperationKind.IncrementExpression, Type: System.Int32)
            Left: ILocalReferenceExpression: index (OperationKind.LocalReferenceExpression, Type: System.Int32)
            Right: ILiteralExpression (OperationKind.LiteralExpression, Type: System.Int32, Constant: 1)
    IIfStatement (OperationKind.IfStatement)
      Condition: IBinaryOperatorExpression (BinaryOperationKind.IntegerGreaterThan) (OperationKind.BinaryOperatorExpression, Type: System.Boolean)
          Left: ILocalReferenceExpression: value (OperationKind.LocalReferenceExpression, Type: System.Int32)
          Right: ILiteralExpression (Text: 100) (OperationKind.LiteralExpression, Type: System.Int32, Constant: 100)
      IBlockStatement (1 statements) (OperationKind.BlockStatement)
        IExpressionStatement (OperationKind.ExpressionStatement)
          IAssignmentExpression (OperationKind.AssignmentExpression, Type: System.Boolean)
            Left: ILocalReferenceExpression: condition (OperationKind.LocalReferenceExpression, Type: System.Boolean)
            Right: ILiteralExpression (OperationKind.LiteralExpression, Type: System.Boolean, Constant: False)
";
            VerifyOperationTreeForTest<WhileStatementSyntax>(source, expectedOperationTree);
        }

        [Fact, WorkItem(17602, "https://github.com/dotnet/roslyn/issues/17602")]
        public void IWhileUntilLoopStatement_WhileMissingStatement()
        {
            string source = @"
class ContinueTest
{
    static void Main()
    {
        int i = 0;
        /*<bind>*/while(i <= 10)
        {

        }/*</bind>*/
    }
}
";
            string expectedOperationTree = @"
IWhileUntilLoopStatement (IsTopTest: True, IsWhile: True) (LoopKind.WhileUntil) (OperationKind.LoopStatement)
  Condition: IBinaryOperatorExpression (BinaryOperationKind.IntegerLessThanOrEqual) (OperationKind.BinaryOperatorExpression, Type: System.Boolean)
      Left: ILocalReferenceExpression: i (OperationKind.LocalReferenceExpression, Type: System.Int32)
      Right: ILiteralExpression (Text: 10) (OperationKind.LiteralExpression, Type: System.Int32, Constant: 10)
  IBlockStatement (0 statements) (OperationKind.BlockStatement)
";
            VerifyOperationTreeForTest<WhileStatementSyntax>(source, expectedOperationTree);
        }

        [Fact, WorkItem(17602, "https://github.com/dotnet/roslyn/issues/17602")]
        public void IWhileUntilLoopStatement_WhileMultipleCondition()
        {
            string source = @"
class ContinueTest
{
    static void Main()
    {
        int i = 0, j = 0 ;
        /*<bind>*/while((i <= 10) && j<=20)
        {
            i++;
            j = j * i;
        }/*</bind>*/
    }
}
";
            string expectedOperationTree = @"
IWhileUntilLoopStatement (IsTopTest: True, IsWhile: True) (LoopKind.WhileUntil) (OperationKind.LoopStatement)
  Condition: IBinaryOperatorExpression (BinaryOperationKind.BooleanConditionalAnd) (OperationKind.BinaryOperatorExpression, Type: System.Boolean)
      Left: IBinaryOperatorExpression (BinaryOperationKind.IntegerLessThanOrEqual) (OperationKind.BinaryOperatorExpression, Type: System.Boolean)
          Left: ILocalReferenceExpression: i (OperationKind.LocalReferenceExpression, Type: System.Int32)
          Right: ILiteralExpression (Text: 10) (OperationKind.LiteralExpression, Type: System.Int32, Constant: 10)
      Right: IBinaryOperatorExpression (BinaryOperationKind.IntegerLessThanOrEqual) (OperationKind.BinaryOperatorExpression, Type: System.Boolean)
          Left: ILocalReferenceExpression: j (OperationKind.LocalReferenceExpression, Type: System.Int32)
          Right: ILiteralExpression (Text: 20) (OperationKind.LiteralExpression, Type: System.Int32, Constant: 20)
  IBlockStatement (2 statements) (OperationKind.BlockStatement)
    IExpressionStatement (OperationKind.ExpressionStatement)
      IIncrementExpression (UnaryOperandKind.IntegerPostfixIncrement) (BinaryOperationKind.IntegerAdd) (OperationKind.IncrementExpression, Type: System.Int32)
        Left: ILocalReferenceExpression: i (OperationKind.LocalReferenceExpression, Type: System.Int32)
        Right: ILiteralExpression (OperationKind.LiteralExpression, Type: System.Int32, Constant: 1)
    IExpressionStatement (OperationKind.ExpressionStatement)
      IAssignmentExpression (OperationKind.AssignmentExpression, Type: System.Int32)
        Left: ILocalReferenceExpression: j (OperationKind.LocalReferenceExpression, Type: System.Int32)
        Right: IBinaryOperatorExpression (BinaryOperationKind.IntegerMultiply) (OperationKind.BinaryOperatorExpression, Type: System.Int32)
            Left: ILocalReferenceExpression: j (OperationKind.LocalReferenceExpression, Type: System.Int32)
            Right: ILocalReferenceExpression: i (OperationKind.LocalReferenceExpression, Type: System.Int32)
";
            VerifyOperationTreeForTest<WhileStatementSyntax>(source, expectedOperationTree);
        }

        [Fact, WorkItem(17602, "https://github.com/dotnet/roslyn/issues/17602")]
        public void IWhileUntilLoopStatement_WhileWithContinue()
        {
            string source = @"
class ContinueTest
{
    static void Main()
    {
        int i = 0;
        /*<bind>*/while(i <= 10)
        {
            i++;
            if (i < 9)
            {
                continue;
            }
            System.Console.WriteLine(i);
        }/*</bind>*/
    }
}
";
            string expectedOperationTree = @"
IWhileUntilLoopStatement (IsTopTest: True, IsWhile: True) (LoopKind.WhileUntil) (OperationKind.LoopStatement)
  Condition: IBinaryOperatorExpression (BinaryOperationKind.IntegerLessThanOrEqual) (OperationKind.BinaryOperatorExpression, Type: System.Boolean)
      Left: ILocalReferenceExpression: i (OperationKind.LocalReferenceExpression, Type: System.Int32)
      Right: ILiteralExpression (Text: 10) (OperationKind.LiteralExpression, Type: System.Int32, Constant: 10)
  IBlockStatement (3 statements) (OperationKind.BlockStatement)
    IExpressionStatement (OperationKind.ExpressionStatement)
      IIncrementExpression (UnaryOperandKind.IntegerPostfixIncrement) (BinaryOperationKind.IntegerAdd) (OperationKind.IncrementExpression, Type: System.Int32)
        Left: ILocalReferenceExpression: i (OperationKind.LocalReferenceExpression, Type: System.Int32)
        Right: ILiteralExpression (OperationKind.LiteralExpression, Type: System.Int32, Constant: 1)
    IIfStatement (OperationKind.IfStatement)
      Condition: IBinaryOperatorExpression (BinaryOperationKind.IntegerLessThan) (OperationKind.BinaryOperatorExpression, Type: System.Boolean)
          Left: ILocalReferenceExpression: i (OperationKind.LocalReferenceExpression, Type: System.Int32)
          Right: ILiteralExpression (Text: 9) (OperationKind.LiteralExpression, Type: System.Int32, Constant: 9)
      IBlockStatement (1 statements) (OperationKind.BlockStatement)
        IBranchStatement (BranchKind.Continue) (OperationKind.BranchStatement)
    IExpressionStatement (OperationKind.ExpressionStatement)
      IInvocationExpression (static void System.Console.WriteLine(System.Int32 value)) (OperationKind.InvocationExpression, Type: System.Void)
        IArgument (Matching Parameter: value) (OperationKind.Argument)
          ILocalReferenceExpression: i (OperationKind.LocalReferenceExpression, Type: System.Int32)
";
            VerifyOperationTreeForTest<WhileStatementSyntax>(source, expectedOperationTree);
        }

        [Fact, WorkItem(17602, "https://github.com/dotnet/roslyn/issues/17602")]
        public void IWhileUntilLoopStatement_WhileContinueInvocationExpression()
        {
            string source = @"
class ContinueTest
{
    static void Main()
    {
        int i = 0;
        /*<bind>*/while(IsTrue(i))
        {
            i++;
            System.Console.WriteLine(i);
        }/*</bind>*/
    }
    private static bool IsTrue(int i)
    {
        if (i<9)
        {
            return true;
        }
        else
        {
            return false;
        }
    }
}
";
            string expectedOperationTree = @"
IWhileUntilLoopStatement (IsTopTest: True, IsWhile: True) (LoopKind.WhileUntil) (OperationKind.LoopStatement)
  Condition: IInvocationExpression (static System.Boolean ContinueTest.IsTrue(System.Int32 i)) (OperationKind.InvocationExpression, Type: System.Boolean)
      IArgument (Matching Parameter: i) (OperationKind.Argument)
        ILocalReferenceExpression: i (OperationKind.LocalReferenceExpression, Type: System.Int32)
  IBlockStatement (2 statements) (OperationKind.BlockStatement)
    IExpressionStatement (OperationKind.ExpressionStatement)
      IIncrementExpression (UnaryOperandKind.IntegerPostfixIncrement) (BinaryOperationKind.IntegerAdd) (OperationKind.IncrementExpression, Type: System.Int32)
        Left: ILocalReferenceExpression: i (OperationKind.LocalReferenceExpression, Type: System.Int32)
        Right: ILiteralExpression (OperationKind.LiteralExpression, Type: System.Int32, Constant: 1)
    IExpressionStatement (OperationKind.ExpressionStatement)
      IInvocationExpression (static void System.Console.WriteLine(System.Int32 value)) (OperationKind.InvocationExpression, Type: System.Void)
        IArgument (Matching Parameter: value) (OperationKind.Argument)
          ILocalReferenceExpression: i (OperationKind.LocalReferenceExpression, Type: System.Int32)
";
            VerifyOperationTreeForTest<WhileStatementSyntax>(source, expectedOperationTree);
        }

        [Fact, WorkItem(17602, "https://github.com/dotnet/roslyn/issues/17602")]
        public void IWhileUntilLoopStatement_WhileNested()
        {
            string source = @"
class Test
{
    static void Main()
    {
        int i = 0;
        /*<bind>*/while(i<10)
        {
            i++;
            int j = 0;
            while (j < 10)
            {
                j++;
                System.Console.WriteLine(j);
            }
            System.Console.WriteLine(i);
        }/*</bind>*/
    }
}
";
            string expectedOperationTree = @"
IWhileUntilLoopStatement (IsTopTest: True, IsWhile: True) (LoopKind.WhileUntil) (OperationKind.LoopStatement)
  Condition: IBinaryOperatorExpression (BinaryOperationKind.IntegerLessThan) (OperationKind.BinaryOperatorExpression, Type: System.Boolean)
      Left: ILocalReferenceExpression: i (OperationKind.LocalReferenceExpression, Type: System.Int32)
      Right: ILiteralExpression (Text: 10) (OperationKind.LiteralExpression, Type: System.Int32, Constant: 10)
  IBlockStatement (4 statements, 1 locals) (OperationKind.BlockStatement)
    Local_1: System.Int32 j
    IExpressionStatement (OperationKind.ExpressionStatement)
      IIncrementExpression (UnaryOperandKind.IntegerPostfixIncrement) (BinaryOperationKind.IntegerAdd) (OperationKind.IncrementExpression, Type: System.Int32)
        Left: ILocalReferenceExpression: i (OperationKind.LocalReferenceExpression, Type: System.Int32)
        Right: ILiteralExpression (OperationKind.LiteralExpression, Type: System.Int32, Constant: 1)
    IVariableDeclarationStatement (1 variables) (OperationKind.VariableDeclarationStatement)
      IVariableDeclaration: System.Int32 j (OperationKind.VariableDeclaration)
        Initializer: ILiteralExpression (Text: 0) (OperationKind.LiteralExpression, Type: System.Int32, Constant: 0)
    IWhileUntilLoopStatement (IsTopTest: True, IsWhile: True) (LoopKind.WhileUntil) (OperationKind.LoopStatement)
      Condition: IBinaryOperatorExpression (BinaryOperationKind.IntegerLessThan) (OperationKind.BinaryOperatorExpression, Type: System.Boolean)
          Left: ILocalReferenceExpression: j (OperationKind.LocalReferenceExpression, Type: System.Int32)
          Right: ILiteralExpression (Text: 10) (OperationKind.LiteralExpression, Type: System.Int32, Constant: 10)
      IBlockStatement (2 statements) (OperationKind.BlockStatement)
        IExpressionStatement (OperationKind.ExpressionStatement)
          IIncrementExpression (UnaryOperandKind.IntegerPostfixIncrement) (BinaryOperationKind.IntegerAdd) (OperationKind.IncrementExpression, Type: System.Int32)
            Left: ILocalReferenceExpression: j (OperationKind.LocalReferenceExpression, Type: System.Int32)
            Right: ILiteralExpression (OperationKind.LiteralExpression, Type: System.Int32, Constant: 1)
        IExpressionStatement (OperationKind.ExpressionStatement)
          IInvocationExpression (static void System.Console.WriteLine(System.Int32 value)) (OperationKind.InvocationExpression, Type: System.Void)
            IArgument (Matching Parameter: value) (OperationKind.Argument)
              ILocalReferenceExpression: j (OperationKind.LocalReferenceExpression, Type: System.Int32)
    IExpressionStatement (OperationKind.ExpressionStatement)
      IInvocationExpression (static void System.Console.WriteLine(System.Int32 value)) (OperationKind.InvocationExpression, Type: System.Void)
        IArgument (Matching Parameter: value) (OperationKind.Argument)
          ILocalReferenceExpression: i (OperationKind.LocalReferenceExpression, Type: System.Int32)
";
            VerifyOperationTreeForTest<WhileStatementSyntax>(source, expectedOperationTree);
        }

        [Fact, WorkItem(17602, "https://github.com/dotnet/roslyn/issues/17602")]
        public void IWhileUntilLoopStatement_WhileChangeOuterInnerValue()
        {
            string source = @"
class Test
{
    static void Main()
    {
        int i = 0;
        /*<bind>*/while(i<10)
        {
            i++;
            int j = 0;
            while (j < 10)
            {
                j++;
                i = i + j;
                System.Console.WriteLine(j);
            }
            System.Console.WriteLine(i);
        }/*</bind>*/
    }
}
";
            string expectedOperationTree = @"
IWhileUntilLoopStatement (IsTopTest: True, IsWhile: True) (LoopKind.WhileUntil) (OperationKind.LoopStatement)
  Condition: IBinaryOperatorExpression (BinaryOperationKind.IntegerLessThan) (OperationKind.BinaryOperatorExpression, Type: System.Boolean)
      Left: ILocalReferenceExpression: i (OperationKind.LocalReferenceExpression, Type: System.Int32)
      Right: ILiteralExpression (Text: 10) (OperationKind.LiteralExpression, Type: System.Int32, Constant: 10)
  IBlockStatement (4 statements, 1 locals) (OperationKind.BlockStatement)
    Local_1: System.Int32 j
    IExpressionStatement (OperationKind.ExpressionStatement)
      IIncrementExpression (UnaryOperandKind.IntegerPostfixIncrement) (BinaryOperationKind.IntegerAdd) (OperationKind.IncrementExpression, Type: System.Int32)
        Left: ILocalReferenceExpression: i (OperationKind.LocalReferenceExpression, Type: System.Int32)
        Right: ILiteralExpression (OperationKind.LiteralExpression, Type: System.Int32, Constant: 1)
    IVariableDeclarationStatement (1 variables) (OperationKind.VariableDeclarationStatement)
      IVariableDeclaration: System.Int32 j (OperationKind.VariableDeclaration)
        Initializer: ILiteralExpression (Text: 0) (OperationKind.LiteralExpression, Type: System.Int32, Constant: 0)
    IWhileUntilLoopStatement (IsTopTest: True, IsWhile: True) (LoopKind.WhileUntil) (OperationKind.LoopStatement)
      Condition: IBinaryOperatorExpression (BinaryOperationKind.IntegerLessThan) (OperationKind.BinaryOperatorExpression, Type: System.Boolean)
          Left: ILocalReferenceExpression: j (OperationKind.LocalReferenceExpression, Type: System.Int32)
          Right: ILiteralExpression (Text: 10) (OperationKind.LiteralExpression, Type: System.Int32, Constant: 10)
      IBlockStatement (3 statements) (OperationKind.BlockStatement)
        IExpressionStatement (OperationKind.ExpressionStatement)
          IIncrementExpression (UnaryOperandKind.IntegerPostfixIncrement) (BinaryOperationKind.IntegerAdd) (OperationKind.IncrementExpression, Type: System.Int32)
            Left: ILocalReferenceExpression: j (OperationKind.LocalReferenceExpression, Type: System.Int32)
            Right: ILiteralExpression (OperationKind.LiteralExpression, Type: System.Int32, Constant: 1)
        IExpressionStatement (OperationKind.ExpressionStatement)
          IAssignmentExpression (OperationKind.AssignmentExpression, Type: System.Int32)
            Left: ILocalReferenceExpression: i (OperationKind.LocalReferenceExpression, Type: System.Int32)
            Right: IBinaryOperatorExpression (BinaryOperationKind.IntegerAdd) (OperationKind.BinaryOperatorExpression, Type: System.Int32)
                Left: ILocalReferenceExpression: i (OperationKind.LocalReferenceExpression, Type: System.Int32)
                Right: ILocalReferenceExpression: j (OperationKind.LocalReferenceExpression, Type: System.Int32)
        IExpressionStatement (OperationKind.ExpressionStatement)
          IInvocationExpression (static void System.Console.WriteLine(System.Int32 value)) (OperationKind.InvocationExpression, Type: System.Void)
            IArgument (Matching Parameter: value) (OperationKind.Argument)
              ILocalReferenceExpression: j (OperationKind.LocalReferenceExpression, Type: System.Int32)
    IExpressionStatement (OperationKind.ExpressionStatement)
      IInvocationExpression (static void System.Console.WriteLine(System.Int32 value)) (OperationKind.InvocationExpression, Type: System.Void)
        IArgument (Matching Parameter: value) (OperationKind.Argument)
          ILocalReferenceExpression: i (OperationKind.LocalReferenceExpression, Type: System.Int32)
";
            VerifyOperationTreeForTest<WhileStatementSyntax>(source, expectedOperationTree);
        }

        [Fact, WorkItem(17602, "https://github.com/dotnet/roslyn/issues/17602")]
        public void IWhileUntilLoopStatement_WhileBreakFromNested()
        {
            string source = @"
class Test
{
    static void Main()
    {
        int i = 0;
        /*<bind>*/while (i < 10)
        {
            i++;
            int j = 0;
            while (j < 10)
            {
                j++;
                if (j > i)
                {
                    break;
                }
                System.Console.WriteLine(j);
            }
            System.Console.WriteLine(i);
        }/*</bind>*/
    }
}
";
            string expectedOperationTree = @"
IWhileUntilLoopStatement (IsTopTest: True, IsWhile: True) (LoopKind.WhileUntil) (OperationKind.LoopStatement)
  Condition: IBinaryOperatorExpression (BinaryOperationKind.IntegerLessThan) (OperationKind.BinaryOperatorExpression, Type: System.Boolean)
      Left: ILocalReferenceExpression: i (OperationKind.LocalReferenceExpression, Type: System.Int32)
      Right: ILiteralExpression (Text: 10) (OperationKind.LiteralExpression, Type: System.Int32, Constant: 10)
  IBlockStatement (4 statements, 1 locals) (OperationKind.BlockStatement)
    Local_1: System.Int32 j
    IExpressionStatement (OperationKind.ExpressionStatement)
      IIncrementExpression (UnaryOperandKind.IntegerPostfixIncrement) (BinaryOperationKind.IntegerAdd) (OperationKind.IncrementExpression, Type: System.Int32)
        Left: ILocalReferenceExpression: i (OperationKind.LocalReferenceExpression, Type: System.Int32)
        Right: ILiteralExpression (OperationKind.LiteralExpression, Type: System.Int32, Constant: 1)
    IVariableDeclarationStatement (1 variables) (OperationKind.VariableDeclarationStatement)
      IVariableDeclaration: System.Int32 j (OperationKind.VariableDeclaration)
        Initializer: ILiteralExpression (Text: 0) (OperationKind.LiteralExpression, Type: System.Int32, Constant: 0)
    IWhileUntilLoopStatement (IsTopTest: True, IsWhile: True) (LoopKind.WhileUntil) (OperationKind.LoopStatement)
      Condition: IBinaryOperatorExpression (BinaryOperationKind.IntegerLessThan) (OperationKind.BinaryOperatorExpression, Type: System.Boolean)
          Left: ILocalReferenceExpression: j (OperationKind.LocalReferenceExpression, Type: System.Int32)
          Right: ILiteralExpression (Text: 10) (OperationKind.LiteralExpression, Type: System.Int32, Constant: 10)
      IBlockStatement (3 statements) (OperationKind.BlockStatement)
        IExpressionStatement (OperationKind.ExpressionStatement)
          IIncrementExpression (UnaryOperandKind.IntegerPostfixIncrement) (BinaryOperationKind.IntegerAdd) (OperationKind.IncrementExpression, Type: System.Int32)
            Left: ILocalReferenceExpression: j (OperationKind.LocalReferenceExpression, Type: System.Int32)
            Right: ILiteralExpression (OperationKind.LiteralExpression, Type: System.Int32, Constant: 1)
        IIfStatement (OperationKind.IfStatement)
          Condition: IBinaryOperatorExpression (BinaryOperationKind.IntegerGreaterThan) (OperationKind.BinaryOperatorExpression, Type: System.Boolean)
              Left: ILocalReferenceExpression: j (OperationKind.LocalReferenceExpression, Type: System.Int32)
              Right: ILocalReferenceExpression: i (OperationKind.LocalReferenceExpression, Type: System.Int32)
          IBlockStatement (1 statements) (OperationKind.BlockStatement)
            IBranchStatement (BranchKind.Break) (OperationKind.BranchStatement)
        IExpressionStatement (OperationKind.ExpressionStatement)
          IInvocationExpression (static void System.Console.WriteLine(System.Int32 value)) (OperationKind.InvocationExpression, Type: System.Void)
            IArgument (Matching Parameter: value) (OperationKind.Argument)
              ILocalReferenceExpression: j (OperationKind.LocalReferenceExpression, Type: System.Int32)
    IExpressionStatement (OperationKind.ExpressionStatement)
      IInvocationExpression (static void System.Console.WriteLine(System.Int32 value)) (OperationKind.InvocationExpression, Type: System.Void)
        IArgument (Matching Parameter: value) (OperationKind.Argument)
          ILocalReferenceExpression: i (OperationKind.LocalReferenceExpression, Type: System.Int32)
";
            VerifyOperationTreeForTest<WhileStatementSyntax>(source, expectedOperationTree);
        }

        [Fact, WorkItem(17602, "https://github.com/dotnet/roslyn/issues/17602")]
        public void IWhileUntilLoopStatement_WhileContinueFromNested()
        {
            string source = @"
class Test
{
    static void Main()
    {
        int i = 0;
        /*<bind>*/while (i < 10)
        {
            i++;
            int j = 0;
            while (j < 10)
            {
                j++;
                if (j < i)
                {
                    continue;
                }
                System.Console.WriteLine(j);
            }
            System.Console.WriteLine(i);
        }/*</bind>*/
    }
}
";
            string expectedOperationTree = @"
IWhileUntilLoopStatement (IsTopTest: True, IsWhile: True) (LoopKind.WhileUntil) (OperationKind.LoopStatement)
  Condition: IBinaryOperatorExpression (BinaryOperationKind.IntegerLessThan) (OperationKind.BinaryOperatorExpression, Type: System.Boolean)
      Left: ILocalReferenceExpression: i (OperationKind.LocalReferenceExpression, Type: System.Int32)
      Right: ILiteralExpression (Text: 10) (OperationKind.LiteralExpression, Type: System.Int32, Constant: 10)
  IBlockStatement (4 statements, 1 locals) (OperationKind.BlockStatement)
    Local_1: System.Int32 j
    IExpressionStatement (OperationKind.ExpressionStatement)
      IIncrementExpression (UnaryOperandKind.IntegerPostfixIncrement) (BinaryOperationKind.IntegerAdd) (OperationKind.IncrementExpression, Type: System.Int32)
        Left: ILocalReferenceExpression: i (OperationKind.LocalReferenceExpression, Type: System.Int32)
        Right: ILiteralExpression (OperationKind.LiteralExpression, Type: System.Int32, Constant: 1)
    IVariableDeclarationStatement (1 variables) (OperationKind.VariableDeclarationStatement)
      IVariableDeclaration: System.Int32 j (OperationKind.VariableDeclaration)
        Initializer: ILiteralExpression (Text: 0) (OperationKind.LiteralExpression, Type: System.Int32, Constant: 0)
    IWhileUntilLoopStatement (IsTopTest: True, IsWhile: True) (LoopKind.WhileUntil) (OperationKind.LoopStatement)
      Condition: IBinaryOperatorExpression (BinaryOperationKind.IntegerLessThan) (OperationKind.BinaryOperatorExpression, Type: System.Boolean)
          Left: ILocalReferenceExpression: j (OperationKind.LocalReferenceExpression, Type: System.Int32)
          Right: ILiteralExpression (Text: 10) (OperationKind.LiteralExpression, Type: System.Int32, Constant: 10)
      IBlockStatement (3 statements) (OperationKind.BlockStatement)
        IExpressionStatement (OperationKind.ExpressionStatement)
          IIncrementExpression (UnaryOperandKind.IntegerPostfixIncrement) (BinaryOperationKind.IntegerAdd) (OperationKind.IncrementExpression, Type: System.Int32)
            Left: ILocalReferenceExpression: j (OperationKind.LocalReferenceExpression, Type: System.Int32)
            Right: ILiteralExpression (OperationKind.LiteralExpression, Type: System.Int32, Constant: 1)
        IIfStatement (OperationKind.IfStatement)
          Condition: IBinaryOperatorExpression (BinaryOperationKind.IntegerLessThan) (OperationKind.BinaryOperatorExpression, Type: System.Boolean)
              Left: ILocalReferenceExpression: j (OperationKind.LocalReferenceExpression, Type: System.Int32)
              Right: ILocalReferenceExpression: i (OperationKind.LocalReferenceExpression, Type: System.Int32)
          IBlockStatement (1 statements) (OperationKind.BlockStatement)
            IBranchStatement (BranchKind.Continue) (OperationKind.BranchStatement)
        IExpressionStatement (OperationKind.ExpressionStatement)
          IInvocationExpression (static void System.Console.WriteLine(System.Int32 value)) (OperationKind.InvocationExpression, Type: System.Void)
            IArgument (Matching Parameter: value) (OperationKind.Argument)
              ILocalReferenceExpression: j (OperationKind.LocalReferenceExpression, Type: System.Int32)
    IExpressionStatement (OperationKind.ExpressionStatement)
      IInvocationExpression (static void System.Console.WriteLine(System.Int32 value)) (OperationKind.InvocationExpression, Type: System.Void)
        IArgument (Matching Parameter: value) (OperationKind.Argument)
          ILocalReferenceExpression: i (OperationKind.LocalReferenceExpression, Type: System.Int32)
";
            VerifyOperationTreeForTest<WhileStatementSyntax>(source, expectedOperationTree);
        }

        [Fact, WorkItem(17602, "https://github.com/dotnet/roslyn/issues/17602")]
        public void IWhileUntilLoopStatement_WhileGotoFromNested()
        {
            string source = @"
class Test
{
    static void Main()
    {
        int i = 0;
        /*<bind>*/while (i < 10)
        {
            i++;
            int j = 0;
            while (j < 10)
            {
                j++;
                if (j > i)
                {
                    goto FirstLoop;
                }
                System.Console.WriteLine(j);

            }
        FirstLoop:
            continue;
            System.Console.WriteLine(i);
        }/*</bind>*/
    }
}
";
            string expectedOperationTree = @"
IWhileUntilLoopStatement (IsTopTest: True, IsWhile: True) (LoopKind.WhileUntil) (OperationKind.LoopStatement)
  Condition: IBinaryOperatorExpression (BinaryOperationKind.IntegerLessThan) (OperationKind.BinaryOperatorExpression, Type: System.Boolean)
      Left: ILocalReferenceExpression: i (OperationKind.LocalReferenceExpression, Type: System.Int32)
      Right: ILiteralExpression (Text: 10) (OperationKind.LiteralExpression, Type: System.Int32, Constant: 10)
  IBlockStatement (5 statements, 1 locals) (OperationKind.BlockStatement)
    Local_1: System.Int32 j
    IExpressionStatement (OperationKind.ExpressionStatement)
      IIncrementExpression (UnaryOperandKind.IntegerPostfixIncrement) (BinaryOperationKind.IntegerAdd) (OperationKind.IncrementExpression, Type: System.Int32)
        Left: ILocalReferenceExpression: i (OperationKind.LocalReferenceExpression, Type: System.Int32)
        Right: ILiteralExpression (OperationKind.LiteralExpression, Type: System.Int32, Constant: 1)
    IVariableDeclarationStatement (1 variables) (OperationKind.VariableDeclarationStatement)
      IVariableDeclaration: System.Int32 j (OperationKind.VariableDeclaration)
        Initializer: ILiteralExpression (Text: 0) (OperationKind.LiteralExpression, Type: System.Int32, Constant: 0)
    IWhileUntilLoopStatement (IsTopTest: True, IsWhile: True) (LoopKind.WhileUntil) (OperationKind.LoopStatement)
      Condition: IBinaryOperatorExpression (BinaryOperationKind.IntegerLessThan) (OperationKind.BinaryOperatorExpression, Type: System.Boolean)
          Left: ILocalReferenceExpression: j (OperationKind.LocalReferenceExpression, Type: System.Int32)
          Right: ILiteralExpression (Text: 10) (OperationKind.LiteralExpression, Type: System.Int32, Constant: 10)
      IBlockStatement (3 statements) (OperationKind.BlockStatement)
        IExpressionStatement (OperationKind.ExpressionStatement)
          IIncrementExpression (UnaryOperandKind.IntegerPostfixIncrement) (BinaryOperationKind.IntegerAdd) (OperationKind.IncrementExpression, Type: System.Int32)
            Left: ILocalReferenceExpression: j (OperationKind.LocalReferenceExpression, Type: System.Int32)
            Right: ILiteralExpression (OperationKind.LiteralExpression, Type: System.Int32, Constant: 1)
        IIfStatement (OperationKind.IfStatement)
          Condition: IBinaryOperatorExpression (BinaryOperationKind.IntegerGreaterThan) (OperationKind.BinaryOperatorExpression, Type: System.Boolean)
              Left: ILocalReferenceExpression: j (OperationKind.LocalReferenceExpression, Type: System.Int32)
              Right: ILocalReferenceExpression: i (OperationKind.LocalReferenceExpression, Type: System.Int32)
          IBlockStatement (1 statements) (OperationKind.BlockStatement)
            IBranchStatement (BranchKind.GoTo, Label: FirstLoop) (OperationKind.BranchStatement)
        IExpressionStatement (OperationKind.ExpressionStatement)
          IInvocationExpression (static void System.Console.WriteLine(System.Int32 value)) (OperationKind.InvocationExpression, Type: System.Void)
            IArgument (Matching Parameter: value) (OperationKind.Argument)
              ILocalReferenceExpression: j (OperationKind.LocalReferenceExpression, Type: System.Int32)
    ILabelStatement (Label: FirstLoop) (OperationKind.LabelStatement)
      IBranchStatement (BranchKind.Continue) (OperationKind.BranchStatement)
    IExpressionStatement (OperationKind.ExpressionStatement)
      IInvocationExpression (static void System.Console.WriteLine(System.Int32 value)) (OperationKind.InvocationExpression, Type: System.Void)
        IArgument (Matching Parameter: value) (OperationKind.Argument)
          ILocalReferenceExpression: i (OperationKind.LocalReferenceExpression, Type: System.Int32)
";
            VerifyOperationTreeForTest<WhileStatementSyntax>(source, expectedOperationTree);
        }

        [Fact, WorkItem(17602, "https://github.com/dotnet/roslyn/issues/17602")]
        public void IWhileUntilLoopStatement_WhileThrowFromNested()
        {
            string source = @"
class Test
{
    static void Main()
    {
        int i = 0;
        /*<bind>*/while (i < 10)
        {
            i++;
            int j = 0;
            while (j < 10)
            {
                j++;
                if (j > i)
                {
                    throw new System.Exception(""Exception Hit"");
                }
                System.Console.WriteLine(j);

            }
            System.Console.WriteLine(i);
        }/*</bind>*/
    }
}
";
            string expectedOperationTree = @"
IWhileUntilLoopStatement (IsTopTest: True, IsWhile: True) (LoopKind.WhileUntil) (OperationKind.LoopStatement)
  Condition: IBinaryOperatorExpression (BinaryOperationKind.IntegerLessThan) (OperationKind.BinaryOperatorExpression, Type: System.Boolean)
      Left: ILocalReferenceExpression: i (OperationKind.LocalReferenceExpression, Type: System.Int32)
      Right: ILiteralExpression (Text: 10) (OperationKind.LiteralExpression, Type: System.Int32, Constant: 10)
  IBlockStatement (4 statements, 1 locals) (OperationKind.BlockStatement)
    Local_1: System.Int32 j
    IExpressionStatement (OperationKind.ExpressionStatement)
      IIncrementExpression (UnaryOperandKind.IntegerPostfixIncrement) (BinaryOperationKind.IntegerAdd) (OperationKind.IncrementExpression, Type: System.Int32)
        Left: ILocalReferenceExpression: i (OperationKind.LocalReferenceExpression, Type: System.Int32)
        Right: ILiteralExpression (OperationKind.LiteralExpression, Type: System.Int32, Constant: 1)
    IVariableDeclarationStatement (1 variables) (OperationKind.VariableDeclarationStatement)
      IVariableDeclaration: System.Int32 j (OperationKind.VariableDeclaration)
        Initializer: ILiteralExpression (Text: 0) (OperationKind.LiteralExpression, Type: System.Int32, Constant: 0)
    IWhileUntilLoopStatement (IsTopTest: True, IsWhile: True) (LoopKind.WhileUntil) (OperationKind.LoopStatement)
      Condition: IBinaryOperatorExpression (BinaryOperationKind.IntegerLessThan) (OperationKind.BinaryOperatorExpression, Type: System.Boolean)
          Left: ILocalReferenceExpression: j (OperationKind.LocalReferenceExpression, Type: System.Int32)
          Right: ILiteralExpression (Text: 10) (OperationKind.LiteralExpression, Type: System.Int32, Constant: 10)
      IBlockStatement (3 statements) (OperationKind.BlockStatement)
        IExpressionStatement (OperationKind.ExpressionStatement)
          IIncrementExpression (UnaryOperandKind.IntegerPostfixIncrement) (BinaryOperationKind.IntegerAdd) (OperationKind.IncrementExpression, Type: System.Int32)
            Left: ILocalReferenceExpression: j (OperationKind.LocalReferenceExpression, Type: System.Int32)
            Right: ILiteralExpression (OperationKind.LiteralExpression, Type: System.Int32, Constant: 1)
        IIfStatement (OperationKind.IfStatement)
          Condition: IBinaryOperatorExpression (BinaryOperationKind.IntegerGreaterThan) (OperationKind.BinaryOperatorExpression, Type: System.Boolean)
              Left: ILocalReferenceExpression: j (OperationKind.LocalReferenceExpression, Type: System.Int32)
              Right: ILocalReferenceExpression: i (OperationKind.LocalReferenceExpression, Type: System.Int32)
          IBlockStatement (1 statements) (OperationKind.BlockStatement)
            IThrowStatement (OperationKind.ThrowStatement)
              IObjectCreationExpression (Constructor: System.Exception..ctor(System.String message)) (OperationKind.ObjectCreationExpression, Type: System.Exception)
                Arguments: IArgument (Matching Parameter: message) (OperationKind.Argument)
                    ILiteralExpression (OperationKind.LiteralExpression, Type: System.String, Constant: Exception Hit)
        IExpressionStatement (OperationKind.ExpressionStatement)
          IInvocationExpression (static void System.Console.WriteLine(System.Int32 value)) (OperationKind.InvocationExpression, Type: System.Void)
            IArgument (Matching Parameter: value) (OperationKind.Argument)
              ILocalReferenceExpression: j (OperationKind.LocalReferenceExpression, Type: System.Int32)
    IExpressionStatement (OperationKind.ExpressionStatement)
      IInvocationExpression (static void System.Console.WriteLine(System.Int32 value)) (OperationKind.InvocationExpression, Type: System.Void)
        IArgument (Matching Parameter: value) (OperationKind.Argument)
          ILocalReferenceExpression: i (OperationKind.LocalReferenceExpression, Type: System.Int32)
";
            VerifyOperationTreeForTest<WhileStatementSyntax>(source, expectedOperationTree);
        }

        [Fact, WorkItem(17602, "https://github.com/dotnet/roslyn/issues/17602")]
        public void IWhileUntilLoopStatement_WhileReturnFromNested()
        {
            string source = @"
class Test
{
    static void Main()
    {
        int i = 0;
        /*<bind>*/while (i < 10)
        {
            i++;
            int j = 0;
            while (j < 10)
            {
                j++;
                if (j > i)
                {
                    return;
                }
                System.Console.WriteLine(j);

            }
            System.Console.WriteLine(i);
        }/*</bind>*/
    }
}
";
            string expectedOperationTree = @"
IWhileUntilLoopStatement (IsTopTest: True, IsWhile: True) (LoopKind.WhileUntil) (OperationKind.LoopStatement)
  Condition: IBinaryOperatorExpression (BinaryOperationKind.IntegerLessThan) (OperationKind.BinaryOperatorExpression, Type: System.Boolean)
      Left: ILocalReferenceExpression: i (OperationKind.LocalReferenceExpression, Type: System.Int32)
      Right: ILiteralExpression (Text: 10) (OperationKind.LiteralExpression, Type: System.Int32, Constant: 10)
  IBlockStatement (4 statements, 1 locals) (OperationKind.BlockStatement)
    Local_1: System.Int32 j
    IExpressionStatement (OperationKind.ExpressionStatement)
      IIncrementExpression (UnaryOperandKind.IntegerPostfixIncrement) (BinaryOperationKind.IntegerAdd) (OperationKind.IncrementExpression, Type: System.Int32)
        Left: ILocalReferenceExpression: i (OperationKind.LocalReferenceExpression, Type: System.Int32)
        Right: ILiteralExpression (OperationKind.LiteralExpression, Type: System.Int32, Constant: 1)
    IVariableDeclarationStatement (1 variables) (OperationKind.VariableDeclarationStatement)
      IVariableDeclaration: System.Int32 j (OperationKind.VariableDeclaration)
        Initializer: ILiteralExpression (Text: 0) (OperationKind.LiteralExpression, Type: System.Int32, Constant: 0)
    IWhileUntilLoopStatement (IsTopTest: True, IsWhile: True) (LoopKind.WhileUntil) (OperationKind.LoopStatement)
      Condition: IBinaryOperatorExpression (BinaryOperationKind.IntegerLessThan) (OperationKind.BinaryOperatorExpression, Type: System.Boolean)
          Left: ILocalReferenceExpression: j (OperationKind.LocalReferenceExpression, Type: System.Int32)
          Right: ILiteralExpression (Text: 10) (OperationKind.LiteralExpression, Type: System.Int32, Constant: 10)
      IBlockStatement (3 statements) (OperationKind.BlockStatement)
        IExpressionStatement (OperationKind.ExpressionStatement)
          IIncrementExpression (UnaryOperandKind.IntegerPostfixIncrement) (BinaryOperationKind.IntegerAdd) (OperationKind.IncrementExpression, Type: System.Int32)
            Left: ILocalReferenceExpression: j (OperationKind.LocalReferenceExpression, Type: System.Int32)
            Right: ILiteralExpression (OperationKind.LiteralExpression, Type: System.Int32, Constant: 1)
        IIfStatement (OperationKind.IfStatement)
          Condition: IBinaryOperatorExpression (BinaryOperationKind.IntegerGreaterThan) (OperationKind.BinaryOperatorExpression, Type: System.Boolean)
              Left: ILocalReferenceExpression: j (OperationKind.LocalReferenceExpression, Type: System.Int32)
              Right: ILocalReferenceExpression: i (OperationKind.LocalReferenceExpression, Type: System.Int32)
          IBlockStatement (1 statements) (OperationKind.BlockStatement)
            IReturnStatement (OperationKind.ReturnStatement)
        IExpressionStatement (OperationKind.ExpressionStatement)
          IInvocationExpression (static void System.Console.WriteLine(System.Int32 value)) (OperationKind.InvocationExpression, Type: System.Void)
            IArgument (Matching Parameter: value) (OperationKind.Argument)
              ILocalReferenceExpression: j (OperationKind.LocalReferenceExpression, Type: System.Int32)
    IExpressionStatement (OperationKind.ExpressionStatement)
      IInvocationExpression (static void System.Console.WriteLine(System.Int32 value)) (OperationKind.InvocationExpression, Type: System.Void)
        IArgument (Matching Parameter: value) (OperationKind.Argument)
          ILocalReferenceExpression: i (OperationKind.LocalReferenceExpression, Type: System.Int32)
";
            VerifyOperationTreeForTest<WhileStatementSyntax>(source, expectedOperationTree);
        }

        [Fact, WorkItem(17602, "https://github.com/dotnet/roslyn/issues/17602")]
        public void IWhileUntilLoopStatement_WhileWithDynamic()
        {
            string source = @"
class C
{
    static void Main(string[] args)
    {
        dynamic d = new MyWhile();
        d.Initialize(5);
        /*<bind>*/while (d.Done)
        {
            d.Next();
        }/*</bind>*/
    }
}

public class MyWhile
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
IWhileUntilLoopStatement (IsTopTest: True, IsWhile: True) (LoopKind.WhileUntil) (OperationKind.LoopStatement)
  Condition: IUnaryOperatorExpression (UnaryOperationKind.DynamicTrue) (OperationKind.UnaryOperatorExpression, Type: System.Boolean)
      IOperation:  (OperationKind.None)
  IBlockStatement (1 statements) (OperationKind.BlockStatement)
    IExpressionStatement (OperationKind.ExpressionStatement)
      IOperation:  (OperationKind.None)
";
            VerifyOperationTreeForTest<WhileStatementSyntax>(source, expectedOperationTree);
        }

        [Fact, WorkItem(17602, "https://github.com/dotnet/roslyn/issues/17602")]
        public void IWhileUntilLoopStatement_WhileIncrementInCondition()
        {
            string source = @"
class Program
{
    static void Main(string[] args)
    {
        int i = 0;
        /*<bind>*/while ( ++i < 5)
        {
            System.Console.WriteLine(i);
        }/*</bind>*/
    }
}

";
            string expectedOperationTree = @"
IWhileUntilLoopStatement (IsTopTest: True, IsWhile: True) (LoopKind.WhileUntil) (OperationKind.LoopStatement)
  Condition: IBinaryOperatorExpression (BinaryOperationKind.IntegerLessThan) (OperationKind.BinaryOperatorExpression, Type: System.Boolean)
      Left: IIncrementExpression (UnaryOperandKind.IntegerPrefixIncrement) (BinaryOperationKind.IntegerAdd) (OperationKind.IncrementExpression, Type: System.Int32)
          Left: ILocalReferenceExpression: i (OperationKind.LocalReferenceExpression, Type: System.Int32)
          Right: ILiteralExpression (OperationKind.LiteralExpression, Type: System.Int32, Constant: 1)
      Right: ILiteralExpression (Text: 5) (OperationKind.LiteralExpression, Type: System.Int32, Constant: 5)
  IBlockStatement (1 statements) (OperationKind.BlockStatement)
    IExpressionStatement (OperationKind.ExpressionStatement)
      IInvocationExpression (static void System.Console.WriteLine(System.Int32 value)) (OperationKind.InvocationExpression, Type: System.Void)
        IArgument (Matching Parameter: value) (OperationKind.Argument)
          ILocalReferenceExpression: i (OperationKind.LocalReferenceExpression, Type: System.Int32)
";
            VerifyOperationTreeForTest<WhileStatementSyntax>(source, expectedOperationTree);
        }

        [Fact, WorkItem(17602, "https://github.com/dotnet/roslyn/issues/17602")]
        public void IWhileUntilLoopStatement_WhileInfiniteLoop()
        {
            string source = @"
class C
{
    static void Main(string[] args)
    {
        int i = 1;
        /*<bind>*/while (i > 0)
        {
            i++;
        }/*</bind>*/
    }
}



";
            string expectedOperationTree = @"
IWhileUntilLoopStatement (IsTopTest: True, IsWhile: True) (LoopKind.WhileUntil) (OperationKind.LoopStatement)
  Condition: IBinaryOperatorExpression (BinaryOperationKind.IntegerGreaterThan) (OperationKind.BinaryOperatorExpression, Type: System.Boolean)
      Left: ILocalReferenceExpression: i (OperationKind.LocalReferenceExpression, Type: System.Int32)
      Right: ILiteralExpression (Text: 0) (OperationKind.LiteralExpression, Type: System.Int32, Constant: 0)
  IBlockStatement (1 statements) (OperationKind.BlockStatement)
    IExpressionStatement (OperationKind.ExpressionStatement)
      IIncrementExpression (UnaryOperandKind.IntegerPostfixIncrement) (BinaryOperationKind.IntegerAdd) (OperationKind.IncrementExpression, Type: System.Int32)
        Left: ILocalReferenceExpression: i (OperationKind.LocalReferenceExpression, Type: System.Int32)
        Right: ILiteralExpression (OperationKind.LiteralExpression, Type: System.Int32, Constant: 1)
";
            VerifyOperationTreeForTest<WhileStatementSyntax>(source, expectedOperationTree);
        }

        [Fact, WorkItem(17602, "https://github.com/dotnet/roslyn/issues/17602")]
        public void IWhileUntilLoopStatement_WhileConstantCheck()
        {
            string source = @"
class Program
{
    bool foo()
    {
        const bool b = true;
        /*<bind>*/while (b == b)
        {
            return b;
        }/*</bind>*/
    }
    
}



";
            string expectedOperationTree = @"
IWhileUntilLoopStatement (IsTopTest: True, IsWhile: True) (LoopKind.WhileUntil) (OperationKind.LoopStatement)
  Condition: IBinaryOperatorExpression (BinaryOperationKind.BooleanEquals) (OperationKind.BinaryOperatorExpression, Type: System.Boolean, Constant: True)
      Left: ILocalReferenceExpression: b (OperationKind.LocalReferenceExpression, Type: System.Boolean, Constant: True)
      Right: ILocalReferenceExpression: b (OperationKind.LocalReferenceExpression, Type: System.Boolean, Constant: True)
  IBlockStatement (1 statements) (OperationKind.BlockStatement)
    IReturnStatement (OperationKind.ReturnStatement)
      ILocalReferenceExpression: b (OperationKind.LocalReferenceExpression, Type: System.Boolean, Constant: True)
";
            VerifyOperationTreeForTest<WhileStatementSyntax>(source, expectedOperationTree);
        }

        [Fact, WorkItem(17602, "https://github.com/dotnet/roslyn/issues/17602")]
        public void IWhileUntilLoopStatement_WhileWithTryCatch()
        {
            string source = @"
public class TryCatchFinally
{
    public void TryMethod()
    {
        sbyte x = 111, y;
        /*<bind>*/while (x-- > 0)
        {
            try
            {
                y = (sbyte)(x / 2);
            }
            finally
            {
                throw new System.Exception(); 
            }
        }/*</bind>*/
       
    }
}
";
            string expectedOperationTree = @"
IWhileUntilLoopStatement (IsTopTest: True, IsWhile: True) (LoopKind.WhileUntil) (OperationKind.LoopStatement)
  Condition: IBinaryOperatorExpression (BinaryOperationKind.IntegerGreaterThan) (OperationKind.BinaryOperatorExpression, Type: System.Boolean)
      Left: IConversionExpression (ConversionKind.CSharp, Implicit) (OperationKind.ConversionExpression, Type: System.Int32)
          IIncrementExpression (UnaryOperandKind.IntegerPostfixDecrement) (BinaryOperationKind.IntegerSubtract) (OperationKind.IncrementExpression, Type: System.SByte)
            Left: ILocalReferenceExpression: x (OperationKind.LocalReferenceExpression, Type: System.SByte)
            Right: ILiteralExpression (OperationKind.LiteralExpression, Type: System.SByte, Constant: 1)
      Right: ILiteralExpression (Text: 0) (OperationKind.LiteralExpression, Type: System.Int32, Constant: 0)
  IBlockStatement (1 statements) (OperationKind.BlockStatement)
    ITryStatement (OperationKind.TryStatement)
      IBlockStatement (1 statements) (OperationKind.BlockStatement)
        IExpressionStatement (OperationKind.ExpressionStatement)
          IAssignmentExpression (OperationKind.AssignmentExpression, Type: System.SByte)
            Left: ILocalReferenceExpression: y (OperationKind.LocalReferenceExpression, Type: System.SByte)
            Right: IConversionExpression (ConversionKind.CSharp, Explicit) (OperationKind.ConversionExpression, Type: System.SByte)
                IBinaryOperatorExpression (BinaryOperationKind.IntegerDivide) (OperationKind.BinaryOperatorExpression, Type: System.Int32)
                  Left: IConversionExpression (ConversionKind.CSharp, Implicit) (OperationKind.ConversionExpression, Type: System.Int32)
                      ILocalReferenceExpression: x (OperationKind.LocalReferenceExpression, Type: System.SByte)
                  Right: ILiteralExpression (Text: 2) (OperationKind.LiteralExpression, Type: System.Int32, Constant: 2)
      IBlockStatement (1 statements) (OperationKind.BlockStatement)
        IThrowStatement (OperationKind.ThrowStatement)
          IObjectCreationExpression (Constructor: System.Exception..ctor()) (OperationKind.ObjectCreationExpression, Type: System.Exception)
";
            VerifyOperationTreeForTest<WhileStatementSyntax>(source, expectedOperationTree);
        }

        [Fact, WorkItem(17602, "https://github.com/dotnet/roslyn/issues/17602")]
        public void IWhileUntilLoopStatement_WhileWithOutVar()
        {
            string source = @"
public class X
{
    public static void Main()
    {
        bool f = true;

        /*<bind>*/while (Dummy(f, TakeOutParam((f ? 1 : 2), out var x1), x1))
        {
            System.Console.WriteLine(x1);
            f = false;
        }/*</bind>*/
    }

    static bool Dummy(bool x, object y, object z)
    {
        System.Console.WriteLine(z);
        return x;
    }

    static bool TakeOutParam<T>(T y, out T x)
    {
        x = y;
        return true;
    }
}

";
            string expectedOperationTree = @"
IWhileUntilLoopStatement (IsTopTest: True, IsWhile: True) (LoopKind.WhileUntil) (OperationKind.LoopStatement)
  Condition: IInvocationExpression (static System.Boolean X.Dummy(System.Boolean x, System.Object y, System.Object z)) (OperationKind.InvocationExpression, Type: System.Boolean)
      IArgument (Matching Parameter: x) (OperationKind.Argument)
        ILocalReferenceExpression: f (OperationKind.LocalReferenceExpression, Type: System.Boolean)
      IArgument (Matching Parameter: y) (OperationKind.Argument)
        IConversionExpression (ConversionKind.Cast, Implicit) (OperationKind.ConversionExpression, Type: System.Object)
          IInvocationExpression (static System.Boolean X.TakeOutParam<System.Int32>(System.Int32 y, out System.Int32 x)) (OperationKind.InvocationExpression, Type: System.Boolean)
            IArgument (Matching Parameter: y) (OperationKind.Argument)
              IConditionalChoiceExpression (OperationKind.ConditionalChoiceExpression, Type: System.Int32)
                Condition: ILocalReferenceExpression: f (OperationKind.LocalReferenceExpression, Type: System.Boolean)
                IfTrue: ILiteralExpression (Text: 1) (OperationKind.LiteralExpression, Type: System.Int32, Constant: 1)
                IfFalse: ILiteralExpression (Text: 2) (OperationKind.LiteralExpression, Type: System.Int32, Constant: 2)
            IArgument (Matching Parameter: x) (OperationKind.Argument)
              ILocalReferenceExpression: x1 (OperationKind.LocalReferenceExpression, Type: System.Int32)
      IArgument (Matching Parameter: z) (OperationKind.Argument)
        IConversionExpression (ConversionKind.Cast, Implicit) (OperationKind.ConversionExpression, Type: System.Object)
          ILocalReferenceExpression: x1 (OperationKind.LocalReferenceExpression, Type: System.Int32)
  IBlockStatement (2 statements) (OperationKind.BlockStatement)
    IExpressionStatement (OperationKind.ExpressionStatement)
      IInvocationExpression (static void System.Console.WriteLine(System.Int32 value)) (OperationKind.InvocationExpression, Type: System.Void)
        IArgument (Matching Parameter: value) (OperationKind.Argument)
          ILocalReferenceExpression: x1 (OperationKind.LocalReferenceExpression, Type: System.Int32)
    IExpressionStatement (OperationKind.ExpressionStatement)
      IAssignmentExpression (OperationKind.AssignmentExpression, Type: System.Boolean)
        Left: ILocalReferenceExpression: f (OperationKind.LocalReferenceExpression, Type: System.Boolean)
        Right: ILiteralExpression (OperationKind.LiteralExpression, Type: System.Boolean, Constant: False)
";
            VerifyOperationTreeForTest<WhileStatementSyntax>(source, expectedOperationTree);
        }

    }
}
