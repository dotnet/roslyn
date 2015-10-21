// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.CSharp.Test.Utilities;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.CSharp.UnitTests
{
    public partial class IOperationTests : SemanticModelTestBase
    {
        [Fact, WorkItem(17601, "https://github.com/dotnet/roslyn/issues/17601")]
        public void IIfstatementSimpleIf()
        {
            string source = @"
class P
{
    private void M()
    {
        bool condition=false;
        /*<bind>*/if (true)
        {
            condition = true;
        }/*</bind>*/
    }
}
";

            string expectedOperationTree = @"
IIfStatement (OperationKind.IfStatement)
  Condition: ILiteralExpression (OperationKind.LiteralExpression, Type: System.Boolean, Constant: True)
  IBlockStatement (1 statements) (OperationKind.BlockStatement)
    IExpressionStatement (OperationKind.ExpressionStatement)
      IAssignmentExpression (OperationKind.AssignmentExpression, Type: System.Boolean)
        Left: ILocalReferenceExpression: condition (OperationKind.LocalReferenceExpression, Type: System.Boolean)
        Right: ILiteralExpression (OperationKind.LiteralExpression, Type: System.Boolean, Constant: True)
";
            VerifyOperationTreeForTest<IfStatementSyntax>(source, expectedOperationTree);
        }

        [Fact, WorkItem(17601, "https://github.com/dotnet/roslyn/issues/17601")]
        public void IIfstatementSimpleIfWithElse()
        {
            string source = @"
class P
{
    private void M()
    {
        bool condition=false;
        /*<bind>*/if (true)
        {
            condition = true;
        }
        else
        {
            condition = false;
        }/*</bind>*/
    }
}
";
            string expectedOperationTree = @"
IIfStatement (OperationKind.IfStatement)
  Condition: ILiteralExpression (OperationKind.LiteralExpression, Type: System.Boolean, Constant: True)
  IBlockStatement (1 statements) (OperationKind.BlockStatement)
    IExpressionStatement (OperationKind.ExpressionStatement)
      IAssignmentExpression (OperationKind.AssignmentExpression, Type: System.Boolean)
        Left: ILocalReferenceExpression: condition (OperationKind.LocalReferenceExpression, Type: System.Boolean)
        Right: ILiteralExpression (OperationKind.LiteralExpression, Type: System.Boolean, Constant: True)
  IBlockStatement (1 statements) (OperationKind.BlockStatement)
    IExpressionStatement (OperationKind.ExpressionStatement)
      IAssignmentExpression (OperationKind.AssignmentExpression, Type: System.Boolean)
        Left: ILocalReferenceExpression: condition (OperationKind.LocalReferenceExpression, Type: System.Boolean)
        Right: ILiteralExpression (OperationKind.LiteralExpression, Type: System.Boolean, Constant: False)
";
            VerifyOperationTreeForTest<IfStatementSyntax>(source, expectedOperationTree);
        }

        [Fact, WorkItem(17601, "https://github.com/dotnet/roslyn/issues/17601")]
        public void IIfstatementSimpleIfWithConditionEvaluationTrue()
        {
            string source = @"
class P
{
    private void M()
    {
          bool condition=false;
        /*<bind>*/if (1==1)
        {
            condition = true;
        }/*</bind>*/
        
    }
}
";
            string expectedOperationTree = @"
IIfStatement (OperationKind.IfStatement)
  Condition: IBinaryOperatorExpression (BinaryOperationKind.IntegerEquals) (OperationKind.BinaryOperatorExpression, Type: System.Boolean, Constant: True)
      Left: ILiteralExpression (Text: 1) (OperationKind.LiteralExpression, Type: System.Int32, Constant: 1)
      Right: ILiteralExpression (Text: 1) (OperationKind.LiteralExpression, Type: System.Int32, Constant: 1)
  IBlockStatement (1 statements) (OperationKind.BlockStatement)
    IExpressionStatement (OperationKind.ExpressionStatement)
      IAssignmentExpression (OperationKind.AssignmentExpression, Type: System.Boolean)
        Left: ILocalReferenceExpression: condition (OperationKind.LocalReferenceExpression, Type: System.Boolean)
        Right: ILiteralExpression (OperationKind.LiteralExpression, Type: System.Boolean, Constant: True)
";
            VerifyOperationTreeForTest<IfStatementSyntax>(source, expectedOperationTree);
        }

        [Fact, WorkItem(17601, "https://github.com/dotnet/roslyn/issues/17601")]
        public void IIfstatementSimpleIfNested1()
        {
            string source = @"
using System;
class P
{
   private void M()
    {
        int m = 12;
        int n = 18;
        /*<bind>*/
        if (m > 10)
        {
            if (n > 20)
                Console.WriteLine(""Result1"");
        }
        else
        {
            Console.WriteLine(""Result2"");
        }/*</bind>*/

    }
}
";
            var compilation = CreateCompilationWithMscorlib(source, options: TestOptions.ReleaseDll, parseOptions: TestOptions.Regular);

            string expectedOperationTree = @"
IIfStatement (OperationKind.IfStatement)
  Condition: IBinaryOperatorExpression (BinaryOperationKind.IntegerGreaterThan) (OperationKind.BinaryOperatorExpression, Type: System.Boolean)
      Left: ILocalReferenceExpression: m (OperationKind.LocalReferenceExpression, Type: System.Int32)
      Right: ILiteralExpression (Text: 10) (OperationKind.LiteralExpression, Type: System.Int32, Constant: 10)
  IBlockStatement (1 statements) (OperationKind.BlockStatement)
    IIfStatement (OperationKind.IfStatement)
      Condition: IBinaryOperatorExpression (BinaryOperationKind.IntegerGreaterThan) (OperationKind.BinaryOperatorExpression, Type: System.Boolean)
          Left: ILocalReferenceExpression: n (OperationKind.LocalReferenceExpression, Type: System.Int32)
          Right: ILiteralExpression (Text: 20) (OperationKind.LiteralExpression, Type: System.Int32, Constant: 20)
      IExpressionStatement (OperationKind.ExpressionStatement)
        IInvocationExpression (static void System.Console.WriteLine(System.String value)) (OperationKind.InvocationExpression, Type: System.Void)
          IArgument (Matching Parameter: value) (OperationKind.Argument)
            ILiteralExpression (OperationKind.LiteralExpression, Type: System.String, Constant: Result1)
  IBlockStatement (1 statements) (OperationKind.BlockStatement)
    IExpressionStatement (OperationKind.ExpressionStatement)
      IInvocationExpression (static void System.Console.WriteLine(System.String value)) (OperationKind.InvocationExpression, Type: System.Void)
        IArgument (Matching Parameter: value) (OperationKind.Argument)
          ILiteralExpression (OperationKind.LiteralExpression, Type: System.String, Constant: Result2)
";
            VerifyOperationTreeForTest<IfStatementSyntax>(source, expectedOperationTree);
        }

        [Fact, WorkItem(17601, "https://github.com/dotnet/roslyn/issues/17601")]
        public void IIfstatementSimpleIfNested2()
        {
            string source = @"
using System;
class P
{
private void M()
    {
        int m = 9;
        int n = 7;
        /*<bind>*/if (m > 10)
            if (n > 20)
            {
                Console.WriteLine(""Result1"");
            }
            else
            {
                Console.WriteLine(""Result2"");
            }/*</bind>*/

    }
}
";
            string expectedOperationTree = @"
IIfStatement (OperationKind.IfStatement)
  Condition: IBinaryOperatorExpression (BinaryOperationKind.IntegerGreaterThan) (OperationKind.BinaryOperatorExpression, Type: System.Boolean)
      Left: ILocalReferenceExpression: m (OperationKind.LocalReferenceExpression, Type: System.Int32)
      Right: ILiteralExpression (Text: 10) (OperationKind.LiteralExpression, Type: System.Int32, Constant: 10)
  IIfStatement (OperationKind.IfStatement)
    Condition: IBinaryOperatorExpression (BinaryOperationKind.IntegerGreaterThan) (OperationKind.BinaryOperatorExpression, Type: System.Boolean)
        Left: ILocalReferenceExpression: n (OperationKind.LocalReferenceExpression, Type: System.Int32)
        Right: ILiteralExpression (Text: 20) (OperationKind.LiteralExpression, Type: System.Int32, Constant: 20)
    IBlockStatement (1 statements) (OperationKind.BlockStatement)
      IExpressionStatement (OperationKind.ExpressionStatement)
        IInvocationExpression (static void System.Console.WriteLine(System.String value)) (OperationKind.InvocationExpression, Type: System.Void)
          IArgument (Matching Parameter: value) (OperationKind.Argument)
            ILiteralExpression (OperationKind.LiteralExpression, Type: System.String, Constant: Result1)
    IBlockStatement (1 statements) (OperationKind.BlockStatement)
      IExpressionStatement (OperationKind.ExpressionStatement)
        IInvocationExpression (static void System.Console.WriteLine(System.String value)) (OperationKind.InvocationExpression, Type: System.Void)
          IArgument (Matching Parameter: value) (OperationKind.Argument)
            ILiteralExpression (OperationKind.LiteralExpression, Type: System.String, Constant: Result2)
";
            VerifyOperationTreeForTest<IfStatementSyntax>(source, expectedOperationTree);
        }

        [Fact, WorkItem(17601, "https://github.com/dotnet/roslyn/issues/17601")]
        public void IIfstatementWithMultipleCondition()
        {
            string source = @"
using System;
class P
{
  private void M()
    {
        int m = 9;
        int n = 7;
        int p = 5;
        /*<bind>*/if (m >= n && m >= p)
        {
            Console.WriteLine(""Nothing is larger than m."");
        }/*</bind>*/

    }
}
";
            string expectedOperationTree = @"
IIfStatement (OperationKind.IfStatement)
  Condition: IBinaryOperatorExpression (BinaryOperationKind.BooleanConditionalAnd) (OperationKind.BinaryOperatorExpression, Type: System.Boolean)
      Left: IBinaryOperatorExpression (BinaryOperationKind.IntegerGreaterThanOrEqual) (OperationKind.BinaryOperatorExpression, Type: System.Boolean)
          Left: ILocalReferenceExpression: m (OperationKind.LocalReferenceExpression, Type: System.Int32)
          Right: ILocalReferenceExpression: n (OperationKind.LocalReferenceExpression, Type: System.Int32)
      Right: IBinaryOperatorExpression (BinaryOperationKind.IntegerGreaterThanOrEqual) (OperationKind.BinaryOperatorExpression, Type: System.Boolean)
          Left: ILocalReferenceExpression: m (OperationKind.LocalReferenceExpression, Type: System.Int32)
          Right: ILocalReferenceExpression: p (OperationKind.LocalReferenceExpression, Type: System.Int32)
  IBlockStatement (1 statements) (OperationKind.BlockStatement)
    IExpressionStatement (OperationKind.ExpressionStatement)
      IInvocationExpression (static void System.Console.WriteLine(System.String value)) (OperationKind.InvocationExpression, Type: System.Void)
        IArgument (Matching Parameter: value) (OperationKind.Argument)
          ILiteralExpression (OperationKind.LiteralExpression, Type: System.String, Constant: Nothing is larger than m.)
";
            VerifyOperationTreeForTest<IfStatementSyntax>(source, expectedOperationTree);
        }

        [Fact, WorkItem(17601, "https://github.com/dotnet/roslyn/issues/17601")]
        public void IIfstatementWithElseIfCondition()
        {
            string source = @"
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

class P
{
    private void M()
    {
        int m = 9;
        int n = 7;
        /*<bind>*/if (n > 20)
        {
            Console.WriteLine(""Result1"");
        }
        else if(m > 10)
        {
            Console.WriteLine(""Result2"");
        }
        else
        {
            Console.WriteLine(""Result3"");
        }/*</bind>*/

    }
}
";
            string expectedOperationTree = @"
IIfStatement (OperationKind.IfStatement)
  Condition: IBinaryOperatorExpression (BinaryOperationKind.IntegerGreaterThan) (OperationKind.BinaryOperatorExpression, Type: System.Boolean)
      Left: ILocalReferenceExpression: n (OperationKind.LocalReferenceExpression, Type: System.Int32)
      Right: ILiteralExpression (Text: 20) (OperationKind.LiteralExpression, Type: System.Int32, Constant: 20)
  IBlockStatement (1 statements) (OperationKind.BlockStatement)
    IExpressionStatement (OperationKind.ExpressionStatement)
      IInvocationExpression (static void System.Console.WriteLine(System.String value)) (OperationKind.InvocationExpression, Type: System.Void)
        IArgument (Matching Parameter: value) (OperationKind.Argument)
          ILiteralExpression (OperationKind.LiteralExpression, Type: System.String, Constant: Result1)
  IIfStatement (OperationKind.IfStatement)
    Condition: IBinaryOperatorExpression (BinaryOperationKind.IntegerGreaterThan) (OperationKind.BinaryOperatorExpression, Type: System.Boolean)
        Left: ILocalReferenceExpression: m (OperationKind.LocalReferenceExpression, Type: System.Int32)
        Right: ILiteralExpression (Text: 10) (OperationKind.LiteralExpression, Type: System.Int32, Constant: 10)
    IBlockStatement (1 statements) (OperationKind.BlockStatement)
      IExpressionStatement (OperationKind.ExpressionStatement)
        IInvocationExpression (static void System.Console.WriteLine(System.String value)) (OperationKind.InvocationExpression, Type: System.Void)
          IArgument (Matching Parameter: value) (OperationKind.Argument)
            ILiteralExpression (OperationKind.LiteralExpression, Type: System.String, Constant: Result2)
    IBlockStatement (1 statements) (OperationKind.BlockStatement)
      IExpressionStatement (OperationKind.ExpressionStatement)
        IInvocationExpression (static void System.Console.WriteLine(System.String value)) (OperationKind.InvocationExpression, Type: System.Void)
          IArgument (Matching Parameter: value) (OperationKind.Argument)
            ILiteralExpression (OperationKind.LiteralExpression, Type: System.String, Constant: Result3)
";
            VerifyOperationTreeForTest<IfStatementSyntax>(source, expectedOperationTree);
        }

        [Fact, WorkItem(17601, "https://github.com/dotnet/roslyn/issues/17601")]
        public void IIfstatementWithElseIfConditionOutVar()
        {
            string source = @"
using System;

class P
{
    private void M()
    {
        var s = """";
        /*<bind>*/if (int.TryParse(s, out var i))
           Console.WriteLine($""i={i}, s={s}"");
        else
          Console.WriteLine($""i={i}, s={s}"");/*</bind>*/ 
        
    }
}
";
            string expectedOperationTree = @"
IIfStatement (OperationKind.IfStatement)
  Condition: IInvocationExpression (static System.Boolean System.Int32.TryParse(System.String s, out System.Int32 result)) (OperationKind.InvocationExpression, Type: System.Boolean)
      IArgument (Matching Parameter: s) (OperationKind.Argument)
        ILocalReferenceExpression: s (OperationKind.LocalReferenceExpression, Type: System.String)
      IArgument (Matching Parameter: result) (OperationKind.Argument)
        ILocalReferenceExpression: i (OperationKind.LocalReferenceExpression, Type: System.Int32)
  IExpressionStatement (OperationKind.ExpressionStatement)
    IInvocationExpression (static void System.Console.WriteLine(System.String value)) (OperationKind.InvocationExpression, Type: System.Void)
      IArgument (Matching Parameter: value) (OperationKind.Argument)
        IOperation:  (OperationKind.None)
  IExpressionStatement (OperationKind.ExpressionStatement)
    IInvocationExpression (static void System.Console.WriteLine(System.String value)) (OperationKind.InvocationExpression, Type: System.Void)
      IArgument (Matching Parameter: value) (OperationKind.Argument)
        IOperation:  (OperationKind.None)
";
            VerifyOperationTreeForTest<IfStatementSyntax>(source, expectedOperationTree);
        }

        [Fact, WorkItem(17601, "https://github.com/dotnet/roslyn/issues/17601")]
        public void IIfstatementWithPattern()
        {
            string source = @"
using System;

class P
{
    private void M()
    {
        object obj = ""pattern"";

        /*<bind>*/if (obj is string str)
        {
            Console.WriteLine(str);
        }/*</bind>*/
    }
}
";
            string expectedOperationTree = @"
IIfStatement (OperationKind.IfStatement)
  Condition: IOperation:  (OperationKind.None)
  IBlockStatement (1 statements) (OperationKind.BlockStatement)
    IExpressionStatement (OperationKind.ExpressionStatement)
      IInvocationExpression (static void System.Console.WriteLine(System.String value)) (OperationKind.InvocationExpression, Type: System.Void)
        IArgument (Matching Parameter: value) (OperationKind.Argument)
          ILocalReferenceExpression: str (OperationKind.LocalReferenceExpression, Type: System.String)
";
            VerifyOperationTreeForTest<IfStatementSyntax>(source, expectedOperationTree);
        }

        [Fact, WorkItem(17601, "https://github.com/dotnet/roslyn/issues/17601")]
        public void IIfstatementWithElseMissing()
        {
            string source = @"
using System;

class P
{
    private void M()
    {
        object obj = ""pattern"";

        /*<bind>*/if (obj is string str)
        {
            Console.WriteLine(str);
        }
        else
/*</bind>*/    }
}
";
            string expectedOperationTree = @"
IIfStatement (OperationKind.IfStatement, IsInvalid)
  Condition: IOperation:  (OperationKind.None)
  IBlockStatement (1 statements) (OperationKind.BlockStatement)
    IExpressionStatement (OperationKind.ExpressionStatement)
      IInvocationExpression (static void System.Console.WriteLine(System.String value)) (OperationKind.InvocationExpression, Type: System.Void)
        IArgument (Matching Parameter: value) (OperationKind.Argument)
          ILocalReferenceExpression: str (OperationKind.LocalReferenceExpression, Type: System.String)
  IExpressionStatement (OperationKind.ExpressionStatement, IsInvalid)
    IInvalidExpression (OperationKind.InvalidExpression, Type: ?, IsInvalid)
";
            VerifyOperationTreeForTest<IfStatementSyntax>(source, expectedOperationTree);
        }

        [Fact, WorkItem(17601, "https://github.com/dotnet/roslyn/issues/17601")]
        public void IIfstatementWithConditionMissing()
        {
            string source = @"
using System;

class P
{
    private void M()
    {
        int a = 1;
        /*<bind>*/if ()
        {
            a = 2;
        }
        else
        {
            a = 3; 
        }/*</bind>*/
    }
}
";
            string expectedOperationTree = @"
IIfStatement (OperationKind.IfStatement, IsInvalid)
  Condition: IConversionExpression (ConversionKind.Invalid, Implicit) (OperationKind.ConversionExpression, Type: System.Boolean, IsInvalid)
      IInvalidExpression (OperationKind.InvalidExpression, Type: ?, IsInvalid)
  IBlockStatement (1 statements) (OperationKind.BlockStatement)
    IExpressionStatement (OperationKind.ExpressionStatement)
      IAssignmentExpression (OperationKind.AssignmentExpression, Type: System.Int32)
        Left: ILocalReferenceExpression: a (OperationKind.LocalReferenceExpression, Type: System.Int32)
        Right: ILiteralExpression (Text: 2) (OperationKind.LiteralExpression, Type: System.Int32, Constant: 2)
  IBlockStatement (1 statements) (OperationKind.BlockStatement)
    IExpressionStatement (OperationKind.ExpressionStatement)
      IAssignmentExpression (OperationKind.AssignmentExpression, Type: System.Int32)
        Left: ILocalReferenceExpression: a (OperationKind.LocalReferenceExpression, Type: System.Int32)
        Right: ILiteralExpression (Text: 3) (OperationKind.LiteralExpression, Type: System.Int32, Constant: 3)
";
            VerifyOperationTreeForTest<IfStatementSyntax>(source, expectedOperationTree);
        }

        [Fact, WorkItem(17601, "https://github.com/dotnet/roslyn/issues/17601")]
        public void IIfstatementWithStatementMissing()
        {
            string source = @"
using System;

class P
{
    private void M()
    {
        int a = 1;
        /*<bind>*/if (a==1)
        else
/*</bind>*/        
    }
}
";
            string expectedOperationTree = @"
IIfStatement (OperationKind.IfStatement, IsInvalid)
  Condition: IBinaryOperatorExpression (BinaryOperationKind.IntegerEquals) (OperationKind.BinaryOperatorExpression, Type: System.Boolean)
      Left: ILocalReferenceExpression: a (OperationKind.LocalReferenceExpression, Type: System.Int32)
      Right: ILiteralExpression (Text: 1) (OperationKind.LiteralExpression, Type: System.Int32, Constant: 1)
  IExpressionStatement (OperationKind.ExpressionStatement, IsInvalid)
    IInvalidExpression (OperationKind.InvalidExpression, Type: ?, IsInvalid)
  IExpressionStatement (OperationKind.ExpressionStatement, IsInvalid)
    IInvalidExpression (OperationKind.InvalidExpression, Type: ?, IsInvalid)
";
            VerifyOperationTreeForTest<IfStatementSyntax>(source, expectedOperationTree);
        }

        [Fact, WorkItem(17601, "https://github.com/dotnet/roslyn/issues/17601")]
        public void IIfstatementWithFuncCall()
        {
            string source = @"
using System;

class P
{
    private void M()
    {
        /*<bind>*/if (true)
            A();
        else
            B();/*</bind>*/
    }
    private void A()
    {
        Console.WriteLine(""A"");
    }
    private void B()
    {
        Console.WriteLine(""B"");
    }
}
";
            string expectedOperationTree = @"
IIfStatement (OperationKind.IfStatement)
  Condition: ILiteralExpression (OperationKind.LiteralExpression, Type: System.Boolean, Constant: True)
  IExpressionStatement (OperationKind.ExpressionStatement)
    IInvocationExpression ( void P.A()) (OperationKind.InvocationExpression, Type: System.Void)
      Instance Receiver: IInstanceReferenceExpression (InstanceReferenceKind.Implicit) (OperationKind.InstanceReferenceExpression, Type: P)
  IExpressionStatement (OperationKind.ExpressionStatement)
    IInvocationExpression ( void P.B()) (OperationKind.InvocationExpression, Type: System.Void)
      Instance Receiver: IInstanceReferenceExpression (InstanceReferenceKind.Implicit) (OperationKind.InstanceReferenceExpression, Type: P)
";
            VerifyOperationTreeForTest<IfStatementSyntax>(source, expectedOperationTree);
        }

        [Fact, WorkItem(17601, "https://github.com/dotnet/roslyn/issues/17601")]
        public void IIfstatementWithDynamic()
        {
            string source = @"
using System;

class C
{
    public static int F<T>(dynamic d, Type t, T x) where T : struct
    {
        /*<bind>*/if (d.GetType() == t && ((T)d).Equals(x))
        {
            return 1;
        }/*</bind>*/

        return 2;
    }
}
";
            string expectedOperationTree = @"
IIfStatement (OperationKind.IfStatement)
  Condition: IUnaryOperatorExpression (UnaryOperationKind.DynamicTrue) (OperationKind.UnaryOperatorExpression, Type: System.Boolean)
      IBinaryOperatorExpression (BinaryOperationKind.DynamicAnd) (OperationKind.BinaryOperatorExpression, Type: dynamic)
        Left: IBinaryOperatorExpression (BinaryOperationKind.Invalid) (OperationKind.BinaryOperatorExpression, Type: dynamic)
            Left: IOperation:  (OperationKind.None)
            Right: IParameterReferenceExpression: t (OperationKind.ParameterReferenceExpression, Type: System.Type)
        Right: IInvocationExpression (virtual System.Boolean System.ValueType.Equals(System.Object obj)) (OperationKind.InvocationExpression, Type: System.Boolean)
            Instance Receiver: IConversionExpression (ConversionKind.CSharp, Explicit) (OperationKind.ConversionExpression, Type: T)
                IParameterReferenceExpression: d (OperationKind.ParameterReferenceExpression, Type: dynamic)
            IArgument (Matching Parameter: obj) (OperationKind.Argument)
              IConversionExpression (ConversionKind.Cast, Implicit) (OperationKind.ConversionExpression, Type: System.Object)
                IParameterReferenceExpression: x (OperationKind.ParameterReferenceExpression, Type: T)
  IBlockStatement (1 statements) (OperationKind.BlockStatement)
    IReturnStatement (OperationKind.ReturnStatement)
      ILiteralExpression (Text: 1) (OperationKind.LiteralExpression, Type: System.Int32, Constant: 1)
";
            VerifyOperationTreeForTest<IfStatementSyntax>(source, expectedOperationTree);
        }

    }
}
