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
IDoLoopOperation (DoLoopKind: DoWhileBottomLoop) (LoopKind.Do) (OperationKind.Loop, IsStatement, Type: null) (Syntax: 'do ... le (i < 4);')
  Condition: 
    IBinaryOperation (BinaryOperatorKind.LessThan) (OperationKind.BinaryOperator, IsExpression, Type: System.Boolean) (Syntax: 'i < 4')
      Left: 
        ILocalReferenceOperation: i (OperationKind.LocalReference, IsExpression, Type: System.Int32) (Syntax: 'i')
      Right: 
        ILiteralOperation (OperationKind.Literal, IsExpression, Type: System.Int32, Constant: 4) (Syntax: '4')
  IgnoredCondition: 
    null
  Body: 
    IBlockOperation (2 statements) (OperationKind.Block, IsStatement, Type: null) (Syntax: '{ ... }')
      IExpressionStatementOperation (OperationKind.ExpressionStatement, IsStatement, Type: null) (Syntax: 'sum += ids[i];')
        Expression: 
          ICompoundAssignmentOperation (BinaryOperatorKind.Add) (OperationKind.CompoundAssignment, IsExpression, Type: System.Int32) (Syntax: 'sum += ids[i]')
            Left: 
              ILocalReferenceOperation: sum (OperationKind.LocalReference, IsExpression, Type: System.Int32) (Syntax: 'sum')
            Right: 
              IArrayElementReferenceOperation (OperationKind.ArrayElementReference, IsExpression, Type: System.Int32) (Syntax: 'ids[i]')
                Array reference: 
                  ILocalReferenceOperation: ids (OperationKind.LocalReference, IsExpression, Type: System.Int32[]) (Syntax: 'ids')
                Indices(1):
                    ILocalReferenceOperation: i (OperationKind.LocalReference, IsExpression, Type: System.Int32) (Syntax: 'i')
      IExpressionStatementOperation (OperationKind.ExpressionStatement, IsStatement, Type: null) (Syntax: 'i++;')
        Expression: 
          IIncrementOrDecrementOperation (Postfix) (OperationKind.Increment, IsExpression, Type: System.Int32) (Syntax: 'i++')
            Target: 
              ILocalReferenceOperation: i (OperationKind.LocalReference, IsExpression, Type: System.Int32) (Syntax: 'i')
";
            VerifyOperationTreeForTest<DoStatementSyntax>(source, expectedOperationTree);
        }

        [CompilerTrait(CompilerFeature.IOperation)]
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
IWhileLoopOperation (LoopKind.While) (OperationKind.Loop, IsStatement, Type: null) (Syntax: 'while (i <  ... }')
  Condition: 
    IBinaryOperation (BinaryOperatorKind.LessThan) (OperationKind.BinaryOperator, IsExpression, Type: System.Boolean) (Syntax: 'i < 5')
      Left: 
        ILocalReferenceOperation: i (OperationKind.LocalReference, IsExpression, Type: System.Int32) (Syntax: 'i')
      Right: 
        ILiteralOperation (OperationKind.Literal, IsExpression, Type: System.Int32, Constant: 5) (Syntax: '5')
  Body: 
    IBlockOperation (2 statements) (OperationKind.Block, IsStatement, Type: null) (Syntax: '{ ... }')
      IExpressionStatementOperation (OperationKind.ExpressionStatement, IsStatement, Type: null) (Syntax: 'sum += i;')
        Expression: 
          ICompoundAssignmentOperation (BinaryOperatorKind.Add) (OperationKind.CompoundAssignment, IsExpression, Type: System.Int32) (Syntax: 'sum += i')
            Left: 
              ILocalReferenceOperation: sum (OperationKind.LocalReference, IsExpression, Type: System.Int32) (Syntax: 'sum')
            Right: 
              ILocalReferenceOperation: i (OperationKind.LocalReference, IsExpression, Type: System.Int32) (Syntax: 'i')
      IExpressionStatementOperation (OperationKind.ExpressionStatement, IsStatement, Type: null) (Syntax: 'i++;')
        Expression: 
          IIncrementOrDecrementOperation (Postfix) (OperationKind.Increment, IsExpression, Type: System.Int32) (Syntax: 'i++')
            Target: 
              ILocalReferenceOperation: i (OperationKind.LocalReference, IsExpression, Type: System.Int32) (Syntax: 'i')
";
            VerifyOperationTreeForTest<WhileStatementSyntax>(source, expectedOperationTree);
        }

        [CompilerTrait(CompilerFeature.IOperation)]
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
IWhileLoopOperation (LoopKind.While) (OperationKind.Loop, IsStatement, Type: null) (Syntax: 'while (cond ... }')
  Condition: 
    ILocalReferenceOperation: condition (OperationKind.LocalReference, IsExpression, Type: System.Boolean) (Syntax: 'condition')
  Body: 
    IBlockOperation (2 statements, 1 locals) (OperationKind.Block, IsStatement, Type: null) (Syntax: '{ ... }')
      Locals: Local_1: System.Int32 value
      IVariableDeclarationsOperation (1 declarations) (OperationKind.VariableDeclarations, IsStatement, Type: null) (Syntax: 'int value = ++index;')
        IVariableDeclarationOperation (1 variables) (OperationKind.VariableDeclaration, Type: null) (Syntax: 'value = ++index')
          Variables: Local_1: System.Int32 value
          Initializer: 
            IVariableInitializerOperation (OperationKind.VariableInitializer, Type: null) (Syntax: '= ++index')
              IIncrementOrDecrementOperation (Prefix) (OperationKind.Increment, IsExpression, Type: System.Int32) (Syntax: '++index')
                Target: 
                  ILocalReferenceOperation: index (OperationKind.LocalReference, IsExpression, Type: System.Int32) (Syntax: 'index')
      IConditionalOperation (OperationKind.Conditional, IsStatement, Type: null) (Syntax: 'if (value > ... }')
        Condition: 
          IBinaryOperation (BinaryOperatorKind.GreaterThan) (OperationKind.BinaryOperator, IsExpression, Type: System.Boolean) (Syntax: 'value > 10')
            Left: 
              ILocalReferenceOperation: value (OperationKind.LocalReference, IsExpression, Type: System.Int32) (Syntax: 'value')
            Right: 
              ILiteralOperation (OperationKind.Literal, IsExpression, Type: System.Int32, Constant: 10) (Syntax: '10')
        WhenTrue: 
          IBlockOperation (1 statements) (OperationKind.Block, IsStatement, Type: null) (Syntax: '{ ... }')
            IExpressionStatementOperation (OperationKind.ExpressionStatement, IsStatement, Type: null) (Syntax: 'condition = false;')
              Expression: 
                ISimpleAssignmentOperation (OperationKind.SimpleAssignment, IsExpression, Type: System.Boolean) (Syntax: 'condition = false')
                  Left: 
                    ILocalReferenceOperation: condition (OperationKind.LocalReference, IsExpression, Type: System.Boolean) (Syntax: 'condition')
                  Right: 
                    ILiteralOperation (OperationKind.Literal, IsExpression, Type: System.Boolean, Constant: False) (Syntax: 'false')
        WhenFalse: 
          null
";
            VerifyOperationTreeForTest<WhileStatementSyntax>(source, expectedOperationTree);
        }

        [CompilerTrait(CompilerFeature.IOperation)]
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
IWhileLoopOperation (LoopKind.While) (OperationKind.Loop, IsStatement, Type: null) (Syntax: 'while (true ... }')
  Condition: 
    ILiteralOperation (OperationKind.Literal, IsExpression, Type: System.Boolean, Constant: True) (Syntax: 'true')
  Body: 
    IBlockOperation (3 statements, 1 locals) (OperationKind.Block, IsStatement, Type: null) (Syntax: '{ ... }')
      Locals: Local_1: System.Int32 value
      IVariableDeclarationsOperation (1 declarations) (OperationKind.VariableDeclarations, IsStatement, Type: null) (Syntax: 'int value = ++index;')
        IVariableDeclarationOperation (1 variables) (OperationKind.VariableDeclaration, Type: null) (Syntax: 'value = ++index')
          Variables: Local_1: System.Int32 value
          Initializer: 
            IVariableInitializerOperation (OperationKind.VariableInitializer, Type: null) (Syntax: '= ++index')
              IIncrementOrDecrementOperation (Prefix) (OperationKind.Increment, IsExpression, Type: System.Int32) (Syntax: '++index')
                Target: 
                  ILocalReferenceOperation: index (OperationKind.LocalReference, IsExpression, Type: System.Int32) (Syntax: 'index')
      IConditionalOperation (OperationKind.Conditional, IsStatement, Type: null) (Syntax: 'if (value > ... }')
        Condition: 
          IBinaryOperation (BinaryOperatorKind.GreaterThan) (OperationKind.BinaryOperator, IsExpression, Type: System.Boolean) (Syntax: 'value > 5')
            Left: 
              ILocalReferenceOperation: value (OperationKind.LocalReference, IsExpression, Type: System.Int32) (Syntax: 'value')
            Right: 
              ILiteralOperation (OperationKind.Literal, IsExpression, Type: System.Int32, Constant: 5) (Syntax: '5')
        WhenTrue: 
          IBlockOperation (2 statements) (OperationKind.Block, IsStatement, Type: null) (Syntax: '{ ... }')
            IExpressionStatementOperation (OperationKind.ExpressionStatement, IsStatement, Type: null) (Syntax: 'Console.Wri ... op break"");')
              Expression: 
                IInvocationOperation (void System.Console.WriteLine(System.String value)) (OperationKind.Invocation, IsExpression, Type: System.Void) (Syntax: 'Console.Wri ... oop break"")')
                  Instance Receiver: 
                    null
                  Arguments(1):
                      IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: value) (OperationKind.Argument, Type: System.String) (Syntax: '""While-loop break""')
                        ILiteralOperation (OperationKind.Literal, IsExpression, Type: System.String, Constant: ""While-loop break"") (Syntax: '""While-loop break""')
                        InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                        OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
            IBranchOperation (BranchKind.Break) (OperationKind.Branch, IsStatement, Type: null) (Syntax: 'break;')
        WhenFalse: 
          null
      IExpressionStatementOperation (OperationKind.ExpressionStatement, IsStatement, Type: null) (Syntax: 'Console.Wri ... tatement"");')
        Expression: 
          IInvocationOperation (void System.Console.WriteLine(System.String value)) (OperationKind.Invocation, IsExpression, Type: System.Void) (Syntax: 'Console.Wri ... statement"")')
            Instance Receiver: 
              null
            Arguments(1):
                IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: value) (OperationKind.Argument, Type: System.String) (Syntax: '""While-loop statement""')
                  ILiteralOperation (OperationKind.Literal, IsExpression, Type: System.String, Constant: ""While-loop statement"") (Syntax: '""While-loop statement""')
                  InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                  OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
";
            VerifyOperationTreeForTest<WhileStatementSyntax>(source, expectedOperationTree);
        }

        [CompilerTrait(CompilerFeature.IOperation)]
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
IWhileLoopOperation (LoopKind.While) (OperationKind.Loop, IsStatement, Type: null) (Syntax: 'while (true ... }')
  Condition: 
    ILiteralOperation (OperationKind.Literal, IsExpression, Type: System.Boolean, Constant: True) (Syntax: 'true')
  Body: 
    IBlockOperation (3 statements, 1 locals) (OperationKind.Block, IsStatement, Type: null) (Syntax: '{ ... }')
      Locals: Local_1: System.Int32 value
      IVariableDeclarationsOperation (1 declarations) (OperationKind.VariableDeclarations, IsStatement, Type: null) (Syntax: 'int value = ++index;')
        IVariableDeclarationOperation (1 variables) (OperationKind.VariableDeclaration, Type: null) (Syntax: 'value = ++index')
          Variables: Local_1: System.Int32 value
          Initializer: 
            IVariableInitializerOperation (OperationKind.VariableInitializer, Type: null) (Syntax: '= ++index')
              IIncrementOrDecrementOperation (Prefix) (OperationKind.Increment, IsExpression, Type: System.Int32) (Syntax: '++index')
                Target: 
                  ILocalReferenceOperation: index (OperationKind.LocalReference, IsExpression, Type: System.Int32) (Syntax: 'index')
      IConditionalOperation (OperationKind.Conditional, IsStatement, Type: null) (Syntax: 'if (value > ... }')
        Condition: 
          IBinaryOperation (BinaryOperatorKind.GreaterThan) (OperationKind.BinaryOperator, IsExpression, Type: System.Boolean) (Syntax: 'value > 100')
            Left: 
              ILocalReferenceOperation: value (OperationKind.LocalReference, IsExpression, Type: System.Int32) (Syntax: 'value')
            Right: 
              ILiteralOperation (OperationKind.Literal, IsExpression, Type: System.Int32, Constant: 100) (Syntax: '100')
        WhenTrue: 
          IBlockOperation (1 statements) (OperationKind.Block, IsStatement, Type: null) (Syntax: '{ ... }')
            IExpressionStatementOperation (OperationKind.ExpressionStatement, IsStatement, Type: null) (Syntax: 'throw new E ... ever hit"");')
              Expression: 
                IThrowOperation (OperationKind.Throw, IsExpression, Type: System.Exception) (Syntax: 'throw new E ... ever hit"");')
                  IObjectCreationOperation (Constructor: System.Exception..ctor(System.String message)) (OperationKind.ObjectCreation, IsExpression, Type: System.Exception) (Syntax: 'new Excepti ... Never hit"")')
                    Arguments(1):
                        IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: message) (OperationKind.Argument, Type: System.String) (Syntax: '""Never hit""')
                          ILiteralOperation (OperationKind.Literal, IsExpression, Type: System.String, Constant: ""Never hit"") (Syntax: '""Never hit""')
                          InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                          OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                    Initializer: 
                      null
        WhenFalse: 
          null
      IExpressionStatementOperation (OperationKind.ExpressionStatement, IsStatement, Type: null) (Syntax: 'Console.Wri ... tatement"");')
        Expression: 
          IInvocationOperation (void System.Console.WriteLine(System.String value)) (OperationKind.Invocation, IsExpression, Type: System.Void) (Syntax: 'Console.Wri ... statement"")')
            Instance Receiver: 
              null
            Arguments(1):
                IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: value) (OperationKind.Argument, Type: System.String) (Syntax: '""While-loop statement""')
                  ILiteralOperation (OperationKind.Literal, IsExpression, Type: System.String, Constant: ""While-loop statement"") (Syntax: '""While-loop statement""')
                  InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                  OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
";
            VerifyOperationTreeForTest<WhileStatementSyntax>(source, expectedOperationTree);
        }

        [CompilerTrait(CompilerFeature.IOperation)]
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
IWhileLoopOperation (LoopKind.While) (OperationKind.Loop, IsStatement, Type: null) (Syntax: 'while ((i = ... }')
  Condition: 
    IBinaryOperation (BinaryOperatorKind.GreaterThanOrEqual) (OperationKind.BinaryOperator, IsExpression, Type: System.Boolean) (Syntax: '(i = value) >= 0')
      Left: 
        ISimpleAssignmentOperation (OperationKind.SimpleAssignment, IsExpression, Type: System.Int32) (Syntax: 'i = value')
          Left: 
            ILocalReferenceOperation: i (OperationKind.LocalReference, IsExpression, Type: System.Int32) (Syntax: 'i')
          Right: 
            ILocalReferenceOperation: value (OperationKind.LocalReference, IsExpression, Type: System.Int32) (Syntax: 'value')
      Right: 
        ILiteralOperation (OperationKind.Literal, IsExpression, Type: System.Int32, Constant: 0) (Syntax: '0')
  Body: 
    IBlockOperation (2 statements) (OperationKind.Block, IsStatement, Type: null) (Syntax: '{ ... }')
      IExpressionStatementOperation (OperationKind.ExpressionStatement, IsStatement, Type: null) (Syntax: 'Console.Wri ...  i, value);')
        Expression: 
          IInvocationOperation (void System.Console.WriteLine(System.String format, System.Object arg0, System.Object arg1)) (OperationKind.Invocation, IsExpression, Type: System.Void) (Syntax: 'Console.Wri ... , i, value)')
            Instance Receiver: 
              null
            Arguments(3):
                IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: format) (OperationKind.Argument, Type: System.String) (Syntax: '""While {0} {1}""')
                  ILiteralOperation (OperationKind.Literal, IsExpression, Type: System.String, Constant: ""While {0} {1}"") (Syntax: '""While {0} {1}""')
                  InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                  OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: arg0) (OperationKind.Argument, Type: System.Object) (Syntax: 'i')
                  IConversionOperation (Implicit, TryCast: False, Unchecked) (OperationKind.Conversion, IsExpression, Type: System.Object, IsImplicit) (Syntax: 'i')
                    Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                    Operand: 
                      ILocalReferenceOperation: i (OperationKind.LocalReference, IsExpression, Type: System.Int32) (Syntax: 'i')
                  InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                  OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: arg1) (OperationKind.Argument, Type: System.Object) (Syntax: 'value')
                  IConversionOperation (Implicit, TryCast: False, Unchecked) (OperationKind.Conversion, IsExpression, Type: System.Object, IsImplicit) (Syntax: 'value')
                    Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                    Operand: 
                      ILocalReferenceOperation: value (OperationKind.LocalReference, IsExpression, Type: System.Int32) (Syntax: 'value')
                  InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                  OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
      IExpressionStatementOperation (OperationKind.ExpressionStatement, IsStatement, Type: null) (Syntax: 'value--;')
        Expression: 
          IIncrementOrDecrementOperation (Postfix) (OperationKind.Decrement, IsExpression, Type: System.Int32) (Syntax: 'value--')
            Target: 
              ILocalReferenceOperation: value (OperationKind.LocalReference, IsExpression, Type: System.Int32) (Syntax: 'value')
";
            VerifyOperationTreeForTest<WhileStatementSyntax>(source, expectedOperationTree);
        }

        [CompilerTrait(CompilerFeature.IOperation)]
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
IWhileLoopOperation (LoopKind.While) (OperationKind.Loop, IsStatement, Type: null, IsInvalid) (Syntax: 'while (numb ... }')
  Condition: 
    IConversionOperation (Implicit, TryCast: False, Unchecked) (OperationKind.Conversion, IsExpression, Type: System.Boolean, IsInvalid, IsImplicit) (Syntax: 'number')
      Conversion: CommonConversion (Exists: False, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
      Operand: 
        ILocalReferenceOperation: number (OperationKind.LocalReference, IsExpression, Type: System.Int32, IsInvalid) (Syntax: 'number')
  Body: 
    IBlockOperation (0 statements) (OperationKind.Block, IsStatement, Type: null) (Syntax: '{ ... }')
";
            VerifyOperationTreeForTest<WhileStatementSyntax>(source, expectedOperationTree);
        }

        [CompilerTrait(CompilerFeature.IOperation)]
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
IWhileLoopOperation (LoopKind.While) (OperationKind.Loop, IsStatement, Type: null) (Syntax: 'while (true ... }')
  Condition: 
    ILiteralOperation (OperationKind.Literal, IsExpression, Type: System.Boolean, Constant: True) (Syntax: 'true')
  Body: 
    IBlockOperation (2 statements) (OperationKind.Block, IsStatement, Type: null) (Syntax: '{ ... }')
      IConditionalOperation (OperationKind.Conditional, IsStatement, Type: null) (Syntax: 'if ((number ... }')
        Condition: 
          IBinaryOperation (BinaryOperatorKind.Equals) (OperationKind.BinaryOperator, IsExpression, Type: System.Boolean) (Syntax: '(number % 2) == 0')
            Left: 
              IBinaryOperation (BinaryOperatorKind.Remainder) (OperationKind.BinaryOperator, IsExpression, Type: System.Int32) (Syntax: 'number % 2')
                Left: 
                  IParameterReferenceOperation: number (OperationKind.ParameterReference, IsExpression, Type: System.Int32) (Syntax: 'number')
                Right: 
                  ILiteralOperation (OperationKind.Literal, IsExpression, Type: System.Int32, Constant: 2) (Syntax: '2')
            Right: 
              ILiteralOperation (OperationKind.Literal, IsExpression, Type: System.Int32, Constant: 0) (Syntax: '0')
        WhenTrue: 
          IBlockOperation (1 statements) (OperationKind.Block, IsStatement, Type: null) (Syntax: '{ ... }')
            IReturnOperation (OperationKind.Return, IsStatement, Type: null) (Syntax: 'return number;')
              ReturnedValue: 
                IParameterReferenceOperation: number (OperationKind.ParameterReference, IsExpression, Type: System.Int32) (Syntax: 'number')
        WhenFalse: 
          null
      IExpressionStatementOperation (OperationKind.ExpressionStatement, IsStatement, Type: null) (Syntax: 'number++;')
        Expression: 
          IIncrementOrDecrementOperation (Postfix) (OperationKind.Increment, IsExpression, Type: System.Int32) (Syntax: 'number++')
            Target: 
              IParameterReferenceOperation: number (OperationKind.ParameterReference, IsExpression, Type: System.Int32) (Syntax: 'number')
";
            VerifyOperationTreeForTest<WhileStatementSyntax>(source, expectedOperationTree);
        }

        [CompilerTrait(CompilerFeature.IOperation)]
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
IWhileLoopOperation (LoopKind.While) (OperationKind.Loop, IsStatement, Type: null) (Syntax: 'while (true ... }')
  Condition: 
    ILiteralOperation (OperationKind.Literal, IsExpression, Type: System.Boolean, Constant: True) (Syntax: 'true')
  Body: 
    IBlockOperation (3 statements) (OperationKind.Block, IsStatement, Type: null) (Syntax: '{ ... }')
      IConditionalOperation (OperationKind.Conditional, IsStatement, Type: null) (Syntax: 'if ((number ... }')
        Condition: 
          IBinaryOperation (BinaryOperatorKind.Equals) (OperationKind.BinaryOperator, IsExpression, Type: System.Boolean) (Syntax: '(number % 2) == 0')
            Left: 
              IBinaryOperation (BinaryOperatorKind.Remainder) (OperationKind.BinaryOperator, IsExpression, Type: System.Int32) (Syntax: 'number % 2')
                Left: 
                  IParameterReferenceOperation: number (OperationKind.ParameterReference, IsExpression, Type: System.Int32) (Syntax: 'number')
                Right: 
                  ILiteralOperation (OperationKind.Literal, IsExpression, Type: System.Int32, Constant: 2) (Syntax: '2')
            Right: 
              ILiteralOperation (OperationKind.Literal, IsExpression, Type: System.Int32, Constant: 0) (Syntax: '0')
        WhenTrue: 
          IBlockOperation (1 statements) (OperationKind.Block, IsStatement, Type: null) (Syntax: '{ ... }')
            IBranchOperation (BranchKind.GoTo, Label: Even) (OperationKind.Branch, IsStatement, Type: null) (Syntax: 'goto Even;')
        WhenFalse: 
          null
      IExpressionStatementOperation (OperationKind.ExpressionStatement, IsStatement, Type: null) (Syntax: 'number++;')
        Expression: 
          IIncrementOrDecrementOperation (Postfix) (OperationKind.Increment, IsExpression, Type: System.Int32) (Syntax: 'number++')
            Target: 
              IParameterReferenceOperation: number (OperationKind.ParameterReference, IsExpression, Type: System.Int32) (Syntax: 'number')
      ILabeledOperation (Label: Even) (OperationKind.Labeled, IsStatement, Type: null) (Syntax: 'Even: ... urn number;')
        Statement: 
          IReturnOperation (OperationKind.Return, IsStatement, Type: null) (Syntax: 'return number;')
            ReturnedValue: 
              IParameterReferenceOperation: number (OperationKind.ParameterReference, IsExpression, Type: System.Int32) (Syntax: 'number')
";
            VerifyOperationTreeForTest<WhileStatementSyntax>(source, expectedOperationTree);
        }

        [CompilerTrait(CompilerFeature.IOperation)]
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
IWhileLoopOperation (LoopKind.While) (OperationKind.Loop, IsStatement, Type: null, IsInvalid) (Syntax: 'while () ... }')
  Condition: 
    IInvalidOperation (OperationKind.Invalid, IsExpression, Type: null, IsInvalid) (Syntax: '')
      Children(0)
  Body: 
    IBlockOperation (2 statements, 1 locals) (OperationKind.Block, IsStatement, Type: null) (Syntax: '{ ... }')
      Locals: Local_1: System.Int32 value
      IVariableDeclarationsOperation (1 declarations) (OperationKind.VariableDeclarations, IsStatement, Type: null) (Syntax: 'int value = ++index;')
        IVariableDeclarationOperation (1 variables) (OperationKind.VariableDeclaration, Type: null) (Syntax: 'value = ++index')
          Variables: Local_1: System.Int32 value
          Initializer: 
            IVariableInitializerOperation (OperationKind.VariableInitializer, Type: null) (Syntax: '= ++index')
              IIncrementOrDecrementOperation (Prefix) (OperationKind.Increment, IsExpression, Type: System.Int32) (Syntax: '++index')
                Target: 
                  ILocalReferenceOperation: index (OperationKind.LocalReference, IsExpression, Type: System.Int32) (Syntax: 'index')
      IConditionalOperation (OperationKind.Conditional, IsStatement, Type: null) (Syntax: 'if (value > ... }')
        Condition: 
          IBinaryOperation (BinaryOperatorKind.GreaterThan) (OperationKind.BinaryOperator, IsExpression, Type: System.Boolean) (Syntax: 'value > 100')
            Left: 
              ILocalReferenceOperation: value (OperationKind.LocalReference, IsExpression, Type: System.Int32) (Syntax: 'value')
            Right: 
              ILiteralOperation (OperationKind.Literal, IsExpression, Type: System.Int32, Constant: 100) (Syntax: '100')
        WhenTrue: 
          IBlockOperation (1 statements) (OperationKind.Block, IsStatement, Type: null) (Syntax: '{ ... }')
            IExpressionStatementOperation (OperationKind.ExpressionStatement, IsStatement, Type: null) (Syntax: 'condition = false;')
              Expression: 
                ISimpleAssignmentOperation (OperationKind.SimpleAssignment, IsExpression, Type: System.Boolean) (Syntax: 'condition = false')
                  Left: 
                    ILocalReferenceOperation: condition (OperationKind.LocalReference, IsExpression, Type: System.Boolean) (Syntax: 'condition')
                  Right: 
                    ILiteralOperation (OperationKind.Literal, IsExpression, Type: System.Boolean, Constant: False) (Syntax: 'false')
        WhenFalse: 
          null
";
            VerifyOperationTreeForTest<WhileStatementSyntax>(source, expectedOperationTree);
        }

        [CompilerTrait(CompilerFeature.IOperation)]
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
IWhileLoopOperation (LoopKind.While) (OperationKind.Loop, IsStatement, Type: null) (Syntax: 'while(i <=  ... }')
  Condition: 
    IBinaryOperation (BinaryOperatorKind.LessThanOrEqual) (OperationKind.BinaryOperator, IsExpression, Type: System.Boolean) (Syntax: 'i <= 10')
      Left: 
        ILocalReferenceOperation: i (OperationKind.LocalReference, IsExpression, Type: System.Int32) (Syntax: 'i')
      Right: 
        ILiteralOperation (OperationKind.Literal, IsExpression, Type: System.Int32, Constant: 10) (Syntax: '10')
  Body: 
    IBlockOperation (0 statements) (OperationKind.Block, IsStatement, Type: null) (Syntax: '{ ... }')
";
            VerifyOperationTreeForTest<WhileStatementSyntax>(source, expectedOperationTree);
        }

        [CompilerTrait(CompilerFeature.IOperation)]
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
IWhileLoopOperation (LoopKind.While) (OperationKind.Loop, IsStatement, Type: null) (Syntax: 'while(i <=  ... }')
  Condition: 
    IBinaryOperation (BinaryOperatorKind.LessThanOrEqual) (OperationKind.BinaryOperator, IsExpression, Type: System.Boolean) (Syntax: 'i <= 10')
      Left: 
        ILocalReferenceOperation: i (OperationKind.LocalReference, IsExpression, Type: System.Int32) (Syntax: 'i')
      Right: 
        ILiteralOperation (OperationKind.Literal, IsExpression, Type: System.Int32, Constant: 10) (Syntax: '10')
  Body: 
    IBlockOperation (3 statements) (OperationKind.Block, IsStatement, Type: null) (Syntax: '{ ... }')
      IExpressionStatementOperation (OperationKind.ExpressionStatement, IsStatement, Type: null) (Syntax: 'i++;')
        Expression: 
          IIncrementOrDecrementOperation (Postfix) (OperationKind.Increment, IsExpression, Type: System.Int32) (Syntax: 'i++')
            Target: 
              ILocalReferenceOperation: i (OperationKind.LocalReference, IsExpression, Type: System.Int32) (Syntax: 'i')
      IConditionalOperation (OperationKind.Conditional, IsStatement, Type: null) (Syntax: 'if (i < 9) ... }')
        Condition: 
          IBinaryOperation (BinaryOperatorKind.LessThan) (OperationKind.BinaryOperator, IsExpression, Type: System.Boolean) (Syntax: 'i < 9')
            Left: 
              ILocalReferenceOperation: i (OperationKind.LocalReference, IsExpression, Type: System.Int32) (Syntax: 'i')
            Right: 
              ILiteralOperation (OperationKind.Literal, IsExpression, Type: System.Int32, Constant: 9) (Syntax: '9')
        WhenTrue: 
          IBlockOperation (1 statements) (OperationKind.Block, IsStatement, Type: null) (Syntax: '{ ... }')
            IBranchOperation (BranchKind.Continue) (OperationKind.Branch, IsStatement, Type: null) (Syntax: 'continue;')
        WhenFalse: 
          null
      IExpressionStatementOperation (OperationKind.ExpressionStatement, IsStatement, Type: null) (Syntax: 'System.Cons ... iteLine(i);')
        Expression: 
          IInvocationOperation (void System.Console.WriteLine(System.Int32 value)) (OperationKind.Invocation, IsExpression, Type: System.Void) (Syntax: 'System.Cons ... riteLine(i)')
            Instance Receiver: 
              null
            Arguments(1):
                IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: value) (OperationKind.Argument, Type: System.Int32) (Syntax: 'i')
                  ILocalReferenceOperation: i (OperationKind.LocalReference, IsExpression, Type: System.Int32) (Syntax: 'i')
                  InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                  OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
";
            VerifyOperationTreeForTest<WhileStatementSyntax>(source, expectedOperationTree);
        }

        [CompilerTrait(CompilerFeature.IOperation)]
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
IWhileLoopOperation (LoopKind.While) (OperationKind.Loop, IsStatement, Type: null) (Syntax: 'while(i<10) ... }')
  Condition: 
    IBinaryOperation (BinaryOperatorKind.LessThan) (OperationKind.BinaryOperator, IsExpression, Type: System.Boolean) (Syntax: 'i<10')
      Left: 
        ILocalReferenceOperation: i (OperationKind.LocalReference, IsExpression, Type: System.Int32) (Syntax: 'i')
      Right: 
        ILiteralOperation (OperationKind.Literal, IsExpression, Type: System.Int32, Constant: 10) (Syntax: '10')
  Body: 
    IBlockOperation (4 statements, 1 locals) (OperationKind.Block, IsStatement, Type: null) (Syntax: '{ ... }')
      Locals: Local_1: System.Int32 j
      IExpressionStatementOperation (OperationKind.ExpressionStatement, IsStatement, Type: null) (Syntax: 'i++;')
        Expression: 
          IIncrementOrDecrementOperation (Postfix) (OperationKind.Increment, IsExpression, Type: System.Int32) (Syntax: 'i++')
            Target: 
              ILocalReferenceOperation: i (OperationKind.LocalReference, IsExpression, Type: System.Int32) (Syntax: 'i')
      IVariableDeclarationsOperation (1 declarations) (OperationKind.VariableDeclarations, IsStatement, Type: null) (Syntax: 'int j = 0;')
        IVariableDeclarationOperation (1 variables) (OperationKind.VariableDeclaration, Type: null) (Syntax: 'j = 0')
          Variables: Local_1: System.Int32 j
          Initializer: 
            IVariableInitializerOperation (OperationKind.VariableInitializer, Type: null) (Syntax: '= 0')
              ILiteralOperation (OperationKind.Literal, IsExpression, Type: System.Int32, Constant: 0) (Syntax: '0')
      IWhileLoopOperation (LoopKind.While) (OperationKind.Loop, IsStatement, Type: null) (Syntax: 'while (j <  ... }')
        Condition: 
          IBinaryOperation (BinaryOperatorKind.LessThan) (OperationKind.BinaryOperator, IsExpression, Type: System.Boolean) (Syntax: 'j < 10')
            Left: 
              ILocalReferenceOperation: j (OperationKind.LocalReference, IsExpression, Type: System.Int32) (Syntax: 'j')
            Right: 
              ILiteralOperation (OperationKind.Literal, IsExpression, Type: System.Int32, Constant: 10) (Syntax: '10')
        Body: 
          IBlockOperation (2 statements) (OperationKind.Block, IsStatement, Type: null) (Syntax: '{ ... }')
            IExpressionStatementOperation (OperationKind.ExpressionStatement, IsStatement, Type: null) (Syntax: 'j++;')
              Expression: 
                IIncrementOrDecrementOperation (Postfix) (OperationKind.Increment, IsExpression, Type: System.Int32) (Syntax: 'j++')
                  Target: 
                    ILocalReferenceOperation: j (OperationKind.LocalReference, IsExpression, Type: System.Int32) (Syntax: 'j')
            IExpressionStatementOperation (OperationKind.ExpressionStatement, IsStatement, Type: null) (Syntax: 'System.Cons ... iteLine(j);')
              Expression: 
                IInvocationOperation (void System.Console.WriteLine(System.Int32 value)) (OperationKind.Invocation, IsExpression, Type: System.Void) (Syntax: 'System.Cons ... riteLine(j)')
                  Instance Receiver: 
                    null
                  Arguments(1):
                      IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: value) (OperationKind.Argument, Type: System.Int32) (Syntax: 'j')
                        ILocalReferenceOperation: j (OperationKind.LocalReference, IsExpression, Type: System.Int32) (Syntax: 'j')
                        InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                        OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
      IExpressionStatementOperation (OperationKind.ExpressionStatement, IsStatement, Type: null) (Syntax: 'System.Cons ... iteLine(i);')
        Expression: 
          IInvocationOperation (void System.Console.WriteLine(System.Int32 value)) (OperationKind.Invocation, IsExpression, Type: System.Void) (Syntax: 'System.Cons ... riteLine(i)')
            Instance Receiver: 
              null
            Arguments(1):
                IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: value) (OperationKind.Argument, Type: System.Int32) (Syntax: 'i')
                  ILocalReferenceOperation: i (OperationKind.LocalReference, IsExpression, Type: System.Int32) (Syntax: 'i')
                  InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                  OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
";
            VerifyOperationTreeForTest<WhileStatementSyntax>(source, expectedOperationTree);
        }

        [CompilerTrait(CompilerFeature.IOperation)]
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
IWhileLoopOperation (LoopKind.While) (OperationKind.Loop, IsStatement, Type: null) (Syntax: 'while(i<10) ... }')
  Condition: 
    IBinaryOperation (BinaryOperatorKind.LessThan) (OperationKind.BinaryOperator, IsExpression, Type: System.Boolean) (Syntax: 'i<10')
      Left: 
        ILocalReferenceOperation: i (OperationKind.LocalReference, IsExpression, Type: System.Int32) (Syntax: 'i')
      Right: 
        ILiteralOperation (OperationKind.Literal, IsExpression, Type: System.Int32, Constant: 10) (Syntax: '10')
  Body: 
    IBlockOperation (4 statements, 1 locals) (OperationKind.Block, IsStatement, Type: null) (Syntax: '{ ... }')
      Locals: Local_1: System.Int32 j
      IExpressionStatementOperation (OperationKind.ExpressionStatement, IsStatement, Type: null) (Syntax: 'i++;')
        Expression: 
          IIncrementOrDecrementOperation (Postfix) (OperationKind.Increment, IsExpression, Type: System.Int32) (Syntax: 'i++')
            Target: 
              ILocalReferenceOperation: i (OperationKind.LocalReference, IsExpression, Type: System.Int32) (Syntax: 'i')
      IVariableDeclarationsOperation (1 declarations) (OperationKind.VariableDeclarations, IsStatement, Type: null) (Syntax: 'int j = 0;')
        IVariableDeclarationOperation (1 variables) (OperationKind.VariableDeclaration, Type: null) (Syntax: 'j = 0')
          Variables: Local_1: System.Int32 j
          Initializer: 
            IVariableInitializerOperation (OperationKind.VariableInitializer, Type: null) (Syntax: '= 0')
              ILiteralOperation (OperationKind.Literal, IsExpression, Type: System.Int32, Constant: 0) (Syntax: '0')
      IWhileLoopOperation (LoopKind.While) (OperationKind.Loop, IsStatement, Type: null) (Syntax: 'while (j <  ... }')
        Condition: 
          IBinaryOperation (BinaryOperatorKind.LessThan) (OperationKind.BinaryOperator, IsExpression, Type: System.Boolean) (Syntax: 'j < 10')
            Left: 
              ILocalReferenceOperation: j (OperationKind.LocalReference, IsExpression, Type: System.Int32) (Syntax: 'j')
            Right: 
              ILiteralOperation (OperationKind.Literal, IsExpression, Type: System.Int32, Constant: 10) (Syntax: '10')
        Body: 
          IBlockOperation (3 statements) (OperationKind.Block, IsStatement, Type: null) (Syntax: '{ ... }')
            IExpressionStatementOperation (OperationKind.ExpressionStatement, IsStatement, Type: null) (Syntax: 'j++;')
              Expression: 
                IIncrementOrDecrementOperation (Postfix) (OperationKind.Increment, IsExpression, Type: System.Int32) (Syntax: 'j++')
                  Target: 
                    ILocalReferenceOperation: j (OperationKind.LocalReference, IsExpression, Type: System.Int32) (Syntax: 'j')
            IExpressionStatementOperation (OperationKind.ExpressionStatement, IsStatement, Type: null) (Syntax: 'i = i + j;')
              Expression: 
                ISimpleAssignmentOperation (OperationKind.SimpleAssignment, IsExpression, Type: System.Int32) (Syntax: 'i = i + j')
                  Left: 
                    ILocalReferenceOperation: i (OperationKind.LocalReference, IsExpression, Type: System.Int32) (Syntax: 'i')
                  Right: 
                    IBinaryOperation (BinaryOperatorKind.Add) (OperationKind.BinaryOperator, IsExpression, Type: System.Int32) (Syntax: 'i + j')
                      Left: 
                        ILocalReferenceOperation: i (OperationKind.LocalReference, IsExpression, Type: System.Int32) (Syntax: 'i')
                      Right: 
                        ILocalReferenceOperation: j (OperationKind.LocalReference, IsExpression, Type: System.Int32) (Syntax: 'j')
            IExpressionStatementOperation (OperationKind.ExpressionStatement, IsStatement, Type: null) (Syntax: 'System.Cons ... iteLine(j);')
              Expression: 
                IInvocationOperation (void System.Console.WriteLine(System.Int32 value)) (OperationKind.Invocation, IsExpression, Type: System.Void) (Syntax: 'System.Cons ... riteLine(j)')
                  Instance Receiver: 
                    null
                  Arguments(1):
                      IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: value) (OperationKind.Argument, Type: System.Int32) (Syntax: 'j')
                        ILocalReferenceOperation: j (OperationKind.LocalReference, IsExpression, Type: System.Int32) (Syntax: 'j')
                        InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                        OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
      IExpressionStatementOperation (OperationKind.ExpressionStatement, IsStatement, Type: null) (Syntax: 'System.Cons ... iteLine(i);')
        Expression: 
          IInvocationOperation (void System.Console.WriteLine(System.Int32 value)) (OperationKind.Invocation, IsExpression, Type: System.Void) (Syntax: 'System.Cons ... riteLine(i)')
            Instance Receiver: 
              null
            Arguments(1):
                IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: value) (OperationKind.Argument, Type: System.Int32) (Syntax: 'i')
                  ILocalReferenceOperation: i (OperationKind.LocalReference, IsExpression, Type: System.Int32) (Syntax: 'i')
                  InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                  OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
";
            VerifyOperationTreeForTest<WhileStatementSyntax>(source, expectedOperationTree);
        }

        [CompilerTrait(CompilerFeature.IOperation)]
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
IWhileLoopOperation (LoopKind.While) (OperationKind.Loop, IsStatement, Type: null) (Syntax: 'while (d.Do ... }')
  Condition: 
    IUnaryOperation (UnaryOperatorKind.True) (OperationKind.UnaryOperator, IsExpression, Type: System.Boolean, IsImplicit) (Syntax: 'd.Done')
      Operand: 
        IDynamicMemberReferenceOperation (Member Name: ""Done"", Containing Type: null) (OperationKind.DynamicMemberReference, IsExpression, Type: dynamic) (Syntax: 'd.Done')
          Type Arguments(0)
          Instance Receiver: 
            ILocalReferenceOperation: d (OperationKind.LocalReference, IsExpression, Type: dynamic) (Syntax: 'd')
  Body: 
    IBlockOperation (1 statements) (OperationKind.Block, IsStatement, Type: null) (Syntax: '{ ... }')
      IExpressionStatementOperation (OperationKind.ExpressionStatement, IsStatement, Type: null) (Syntax: 'd.Next();')
        Expression: 
          IDynamicInvocationOperation (OperationKind.DynamicInvocation, IsExpression, Type: dynamic) (Syntax: 'd.Next()')
            Expression: 
              IDynamicMemberReferenceOperation (Member Name: ""Next"", Containing Type: null) (OperationKind.DynamicMemberReference, IsExpression, Type: dynamic) (Syntax: 'd.Next')
                Type Arguments(0)
                Instance Receiver: 
                  ILocalReferenceOperation: d (OperationKind.LocalReference, IsExpression, Type: dynamic) (Syntax: 'd')
            Arguments(0)
            ArgumentNames(0)
            ArgumentRefKinds(0)
";
            VerifyOperationTreeForTest<WhileStatementSyntax>(source, expectedOperationTree);
        }

        [CompilerTrait(CompilerFeature.IOperation)]
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
IWhileLoopOperation (LoopKind.While) (OperationKind.Loop, IsStatement, Type: null) (Syntax: 'while ( ++i ... }')
  Condition: 
    IBinaryOperation (BinaryOperatorKind.LessThan) (OperationKind.BinaryOperator, IsExpression, Type: System.Boolean) (Syntax: '++i < 5')
      Left: 
        IIncrementOrDecrementOperation (Prefix) (OperationKind.Increment, IsExpression, Type: System.Int32) (Syntax: '++i')
          Target: 
            ILocalReferenceOperation: i (OperationKind.LocalReference, IsExpression, Type: System.Int32) (Syntax: 'i')
      Right: 
        ILiteralOperation (OperationKind.Literal, IsExpression, Type: System.Int32, Constant: 5) (Syntax: '5')
  Body: 
    IBlockOperation (1 statements) (OperationKind.Block, IsStatement, Type: null) (Syntax: '{ ... }')
      IExpressionStatementOperation (OperationKind.ExpressionStatement, IsStatement, Type: null) (Syntax: 'System.Cons ... iteLine(i);')
        Expression: 
          IInvocationOperation (void System.Console.WriteLine(System.Int32 value)) (OperationKind.Invocation, IsExpression, Type: System.Void) (Syntax: 'System.Cons ... riteLine(i)')
            Instance Receiver: 
              null
            Arguments(1):
                IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: value) (OperationKind.Argument, Type: System.Int32) (Syntax: 'i')
                  ILocalReferenceOperation: i (OperationKind.LocalReference, IsExpression, Type: System.Int32) (Syntax: 'i')
                  InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                  OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
";
            VerifyOperationTreeForTest<WhileStatementSyntax>(source, expectedOperationTree);
        }

        [CompilerTrait(CompilerFeature.IOperation)]
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
}";
            string expectedOperationTree = @"
IWhileLoopOperation (LoopKind.While) (OperationKind.Loop, IsStatement, Type: null) (Syntax: 'while (i >  ... }')
  Condition: 
    IBinaryOperation (BinaryOperatorKind.GreaterThan) (OperationKind.BinaryOperator, IsExpression, Type: System.Boolean) (Syntax: 'i > 0')
      Left: 
        ILocalReferenceOperation: i (OperationKind.LocalReference, IsExpression, Type: System.Int32) (Syntax: 'i')
      Right: 
        ILiteralOperation (OperationKind.Literal, IsExpression, Type: System.Int32, Constant: 0) (Syntax: '0')
  Body: 
    IBlockOperation (1 statements) (OperationKind.Block, IsStatement, Type: null) (Syntax: '{ ... }')
      IExpressionStatementOperation (OperationKind.ExpressionStatement, IsStatement, Type: null) (Syntax: 'i++;')
        Expression: 
          IIncrementOrDecrementOperation (Postfix) (OperationKind.Increment, IsExpression, Type: System.Int32) (Syntax: 'i++')
            Target: 
              ILocalReferenceOperation: i (OperationKind.LocalReference, IsExpression, Type: System.Int32) (Syntax: 'i')
";
            VerifyOperationTreeForTest<WhileStatementSyntax>(source, expectedOperationTree);
        }

        [CompilerTrait(CompilerFeature.IOperation)]
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
IWhileLoopOperation (LoopKind.While) (OperationKind.Loop, IsStatement, Type: null) (Syntax: 'while (b == ... }')
  Condition: 
    IBinaryOperation (BinaryOperatorKind.Equals) (OperationKind.BinaryOperator, IsExpression, Type: System.Boolean, Constant: True) (Syntax: 'b == b')
      Left: 
        ILocalReferenceOperation: b (OperationKind.LocalReference, IsExpression, Type: System.Boolean, Constant: True) (Syntax: 'b')
      Right: 
        ILocalReferenceOperation: b (OperationKind.LocalReference, IsExpression, Type: System.Boolean, Constant: True) (Syntax: 'b')
  Body: 
    IBlockOperation (1 statements) (OperationKind.Block, IsStatement, Type: null) (Syntax: '{ ... }')
      IReturnOperation (OperationKind.Return, IsStatement, Type: null) (Syntax: 'return b;')
        ReturnedValue: 
          ILocalReferenceOperation: b (OperationKind.LocalReference, IsExpression, Type: System.Boolean, Constant: True) (Syntax: 'b')
";
            VerifyOperationTreeForTest<WhileStatementSyntax>(source, expectedOperationTree);
        }

        [CompilerTrait(CompilerFeature.IOperation)]
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
IWhileLoopOperation (LoopKind.While) (OperationKind.Loop, IsStatement, Type: null) (Syntax: 'while (x--  ... }')
  Condition: 
    IBinaryOperation (BinaryOperatorKind.GreaterThan) (OperationKind.BinaryOperator, IsExpression, Type: System.Boolean) (Syntax: 'x-- > 0')
      Left: 
        IConversionOperation (Implicit, TryCast: False, Unchecked) (OperationKind.Conversion, IsExpression, Type: System.Int32, IsImplicit) (Syntax: 'x--')
          Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: True, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
          Operand: 
            IIncrementOrDecrementOperation (Postfix) (OperationKind.Decrement, IsExpression, Type: System.SByte) (Syntax: 'x--')
              Target: 
                ILocalReferenceOperation: x (OperationKind.LocalReference, IsExpression, Type: System.SByte) (Syntax: 'x')
      Right: 
        ILiteralOperation (OperationKind.Literal, IsExpression, Type: System.Int32, Constant: 0) (Syntax: '0')
  Body: 
    IBlockOperation (1 statements) (OperationKind.Block, IsStatement, Type: null) (Syntax: '{ ... }')
      ITryOperation (OperationKind.Try, IsStatement, Type: null) (Syntax: 'try ... }')
        Body: 
          IBlockOperation (1 statements) (OperationKind.Block, IsStatement, Type: null) (Syntax: '{ ... }')
            IExpressionStatementOperation (OperationKind.ExpressionStatement, IsStatement, Type: null) (Syntax: 'y = (sbyte)(x / 2);')
              Expression: 
                ISimpleAssignmentOperation (OperationKind.SimpleAssignment, IsExpression, Type: System.SByte) (Syntax: 'y = (sbyte)(x / 2)')
                  Left: 
                    ILocalReferenceOperation: y (OperationKind.LocalReference, IsExpression, Type: System.SByte) (Syntax: 'y')
                  Right: 
                    IConversionOperation (Explicit, TryCast: False, Unchecked) (OperationKind.Conversion, IsExpression, Type: System.SByte) (Syntax: '(sbyte)(x / 2)')
                      Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: True, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                      Operand: 
                        IBinaryOperation (BinaryOperatorKind.Divide) (OperationKind.BinaryOperator, IsExpression, Type: System.Int32) (Syntax: 'x / 2')
                          Left: 
                            IConversionOperation (Implicit, TryCast: False, Unchecked) (OperationKind.Conversion, IsExpression, Type: System.Int32, IsImplicit) (Syntax: 'x')
                              Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: True, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                              Operand: 
                                ILocalReferenceOperation: x (OperationKind.LocalReference, IsExpression, Type: System.SByte) (Syntax: 'x')
                          Right: 
                            ILiteralOperation (OperationKind.Literal, IsExpression, Type: System.Int32, Constant: 2) (Syntax: '2')
        Catch clauses(0)
        Finally: 
          IBlockOperation (1 statements) (OperationKind.Block, IsStatement, Type: null) (Syntax: '{ ... }')
            IExpressionStatementOperation (OperationKind.ExpressionStatement, IsStatement, Type: null) (Syntax: 'throw new S ... xception();')
              Expression: 
                IThrowOperation (OperationKind.Throw, IsExpression, Type: System.Exception) (Syntax: 'throw new S ... xception();')
                  IObjectCreationOperation (Constructor: System.Exception..ctor()) (OperationKind.ObjectCreation, IsExpression, Type: System.Exception) (Syntax: 'new System.Exception()')
                    Arguments(0)
                    Initializer: 
                      null
";
            VerifyOperationTreeForTest<WhileStatementSyntax>(source, expectedOperationTree);
        }

        [CompilerTrait(CompilerFeature.IOperation)]
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
IWhileLoopOperation (LoopKind.While) (OperationKind.Loop, IsStatement, Type: null) (Syntax: 'while (Dumm ... }')
  Locals: Local_1: System.Int32 x1
  Condition: 
    IInvocationOperation (System.Boolean X.Dummy(System.Boolean x, System.Object y, System.Object z)) (OperationKind.Invocation, IsExpression, Type: System.Boolean) (Syntax: 'Dummy(f, Ta ... ar x1), x1)')
      Instance Receiver: 
        null
      Arguments(3):
          IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: x) (OperationKind.Argument, Type: System.Boolean) (Syntax: 'f')
            ILocalReferenceOperation: f (OperationKind.LocalReference, IsExpression, Type: System.Boolean) (Syntax: 'f')
            InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
            OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
          IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: y) (OperationKind.Argument, Type: System.Object) (Syntax: 'TakeOutPara ... out var x1)')
            IConversionOperation (Implicit, TryCast: False, Unchecked) (OperationKind.Conversion, IsExpression, Type: System.Object, IsImplicit) (Syntax: 'TakeOutPara ... out var x1)')
              Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
              Operand: 
                IInvocationOperation (System.Boolean X.TakeOutParam<System.Int32>(System.Int32 y, out System.Int32 x)) (OperationKind.Invocation, IsExpression, Type: System.Boolean) (Syntax: 'TakeOutPara ... out var x1)')
                  Instance Receiver: 
                    null
                  Arguments(2):
                      IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: y) (OperationKind.Argument, Type: System.Int32, IsImplicit) (Syntax: 'f ? 1 : 2')
                        IConditionalOperation (OperationKind.Conditional, IsExpression, Type: System.Int32) (Syntax: 'f ? 1 : 2')
                          Condition: 
                            ILocalReferenceOperation: f (OperationKind.LocalReference, IsExpression, Type: System.Boolean) (Syntax: 'f')
                          WhenTrue: 
                            ILiteralOperation (OperationKind.Literal, IsExpression, Type: System.Int32, Constant: 1) (Syntax: '1')
                          WhenFalse: 
                            ILiteralOperation (OperationKind.Literal, IsExpression, Type: System.Int32, Constant: 2) (Syntax: '2')
                        InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                        OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                      IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: x) (OperationKind.Argument, Type: System.Int32) (Syntax: 'out var x1')
                        IDeclarationExpressionOperation (OperationKind.DeclarationExpression, IsExpression, Type: System.Int32) (Syntax: 'var x1')
                          ILocalReferenceOperation: x1 (IsDeclaration: True) (OperationKind.LocalReference, IsExpression, Type: System.Int32) (Syntax: 'x1')
                        InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                        OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
            InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
            OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
          IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: z) (OperationKind.Argument, Type: System.Object) (Syntax: 'x1')
            IConversionOperation (Implicit, TryCast: False, Unchecked) (OperationKind.Conversion, IsExpression, Type: System.Object, IsImplicit) (Syntax: 'x1')
              Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
              Operand: 
                ILocalReferenceOperation: x1 (OperationKind.LocalReference, IsExpression, Type: System.Int32) (Syntax: 'x1')
            InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
            OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
  Body: 
    IBlockOperation (2 statements) (OperationKind.Block, IsStatement, Type: null) (Syntax: '{ ... }')
      IExpressionStatementOperation (OperationKind.ExpressionStatement, IsStatement, Type: null) (Syntax: 'System.Cons ... teLine(x1);')
        Expression: 
          IInvocationOperation (void System.Console.WriteLine(System.Int32 value)) (OperationKind.Invocation, IsExpression, Type: System.Void) (Syntax: 'System.Cons ... iteLine(x1)')
            Instance Receiver: 
              null
            Arguments(1):
                IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: value) (OperationKind.Argument, Type: System.Int32) (Syntax: 'x1')
                  ILocalReferenceOperation: x1 (OperationKind.LocalReference, IsExpression, Type: System.Int32) (Syntax: 'x1')
                  InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                  OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
      IExpressionStatementOperation (OperationKind.ExpressionStatement, IsStatement, Type: null) (Syntax: 'f = false;')
        Expression: 
          ISimpleAssignmentOperation (OperationKind.SimpleAssignment, IsExpression, Type: System.Boolean) (Syntax: 'f = false')
            Left: 
              ILocalReferenceOperation: f (OperationKind.LocalReference, IsExpression, Type: System.Boolean) (Syntax: 'f')
            Right: 
              ILiteralOperation (OperationKind.Literal, IsExpression, Type: System.Boolean, Constant: False) (Syntax: 'false')
";
            VerifyOperationTreeForTest<WhileStatementSyntax>(source, expectedOperationTree);
        }

        [CompilerTrait(CompilerFeature.IOperation)]
        [Fact, WorkItem(17602, "https://github.com/dotnet/roslyn/issues/17602")]
        public void IWhileUntilLoopStatement_DoWithOutVar()
        {
            string source = @"
class X
{
    public static void Main()
    {
        bool f = true;

        /*<bind>*/do
        {
            f = false;
        } while (Dummy(f, TakeOutParam((f ? 1 : 2), out var x1), x1));/*</bind>*/
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
IDoLoopOperation (DoLoopKind: DoWhileBottomLoop) (LoopKind.Do) (OperationKind.Loop, IsStatement, Type: null) (Syntax: 'do ...  x1), x1));')
  Locals: Local_1: System.Int32 x1
  Condition: 
    IInvocationOperation (System.Boolean X.Dummy(System.Boolean x, System.Object y, System.Object z)) (OperationKind.Invocation, IsExpression, Type: System.Boolean) (Syntax: 'Dummy(f, Ta ... ar x1), x1)')
      Instance Receiver: 
        null
      Arguments(3):
          IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: x) (OperationKind.Argument, Type: System.Boolean) (Syntax: 'f')
            ILocalReferenceOperation: f (OperationKind.LocalReference, IsExpression, Type: System.Boolean) (Syntax: 'f')
            InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
            OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
          IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: y) (OperationKind.Argument, Type: System.Object) (Syntax: 'TakeOutPara ... out var x1)')
            IConversionOperation (Implicit, TryCast: False, Unchecked) (OperationKind.Conversion, IsExpression, Type: System.Object, IsImplicit) (Syntax: 'TakeOutPara ... out var x1)')
              Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
              Operand: 
                IInvocationOperation (System.Boolean X.TakeOutParam<System.Int32>(System.Int32 y, out System.Int32 x)) (OperationKind.Invocation, IsExpression, Type: System.Boolean) (Syntax: 'TakeOutPara ... out var x1)')
                  Instance Receiver: 
                    null
                  Arguments(2):
                      IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: y) (OperationKind.Argument, Type: System.Int32, IsImplicit) (Syntax: 'f ? 1 : 2')
                        IConditionalOperation (OperationKind.Conditional, IsExpression, Type: System.Int32) (Syntax: 'f ? 1 : 2')
                          Condition: 
                            ILocalReferenceOperation: f (OperationKind.LocalReference, IsExpression, Type: System.Boolean) (Syntax: 'f')
                          WhenTrue: 
                            ILiteralOperation (OperationKind.Literal, IsExpression, Type: System.Int32, Constant: 1) (Syntax: '1')
                          WhenFalse: 
                            ILiteralOperation (OperationKind.Literal, IsExpression, Type: System.Int32, Constant: 2) (Syntax: '2')
                        InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                        OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                      IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: x) (OperationKind.Argument, Type: System.Int32) (Syntax: 'out var x1')
                        IDeclarationExpressionOperation (OperationKind.DeclarationExpression, IsExpression, Type: System.Int32) (Syntax: 'var x1')
                          ILocalReferenceOperation: x1 (IsDeclaration: True) (OperationKind.LocalReference, IsExpression, Type: System.Int32) (Syntax: 'x1')
                        InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                        OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
            InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
            OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
          IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: z) (OperationKind.Argument, Type: System.Object) (Syntax: 'x1')
            IConversionOperation (Implicit, TryCast: False, Unchecked) (OperationKind.Conversion, IsExpression, Type: System.Object, IsImplicit) (Syntax: 'x1')
              Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
              Operand: 
                ILocalReferenceOperation: x1 (OperationKind.LocalReference, IsExpression, Type: System.Int32) (Syntax: 'x1')
            InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
            OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
  IgnoredCondition: 
    null
  Body: 
    IBlockOperation (1 statements) (OperationKind.Block, IsStatement, Type: null) (Syntax: '{ ... }')
      IExpressionStatementOperation (OperationKind.ExpressionStatement, IsStatement, Type: null) (Syntax: 'f = false;')
        Expression: 
          ISimpleAssignmentOperation (OperationKind.SimpleAssignment, IsExpression, Type: System.Boolean) (Syntax: 'f = false')
            Left: 
              ILocalReferenceOperation: f (OperationKind.LocalReference, IsExpression, Type: System.Boolean) (Syntax: 'f')
            Right: 
              ILiteralOperation (OperationKind.Literal, IsExpression, Type: System.Boolean, Constant: False) (Syntax: 'false')
";
            var expectedDiagnostics = DiagnosticDescription.None;

            VerifyOperationTreeAndDiagnosticsForTest<DoStatementSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }
    }
}
