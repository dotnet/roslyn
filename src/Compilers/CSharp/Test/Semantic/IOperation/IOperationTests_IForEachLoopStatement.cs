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
        public void IForEachLoopStatement_SimpleForEachLoop()
        {
            string source = @"
class Program
{
    static void Main()
    {
        string[] pets = { ""dog"", ""cat"", ""bird"" };

        /*<bind>*/foreach (string value in pets)
        {
            System.Console.WriteLine(value);
        }/*</bind>*/
    }
}
";
            string expectedOperationTree = @"
IForEachLoopStatement (LoopKind.ForEach) (OperationKind.LoopStatement, Language: C#) (Syntax: 'foreach (st ... }')
  Locals: Local_1: System.String value
  LoopControlVariable: 
    ILocalReferenceExpression: value (IsDeclaration: True) (OperationKind.LocalReferenceExpression, Type: System.String, Constant: null, Language: C#) (Syntax: 'foreach (st ... }')
  Collection: 
    IConversionExpression (Implicit, TryCast: False, Unchecked) (OperationKind.ConversionExpression, Type: System.Collections.IEnumerable, IsImplicit, Language: C#) (Syntax: 'pets')
      Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: True, IsUserDefined: False) (MethodSymbol: null)
      Operand: 
        ILocalReferenceExpression: pets (OperationKind.LocalReferenceExpression, Type: System.String[], Language: C#) (Syntax: 'pets')
  Body: 
    IBlockStatement (1 statements) (OperationKind.BlockStatement, Language: C#) (Syntax: '{ ... }')
      IExpressionStatement (OperationKind.ExpressionStatement, Language: C#) (Syntax: 'System.Cons ... ine(value);')
        Expression: 
          IInvocationExpression (void System.Console.WriteLine(System.String value)) (OperationKind.InvocationExpression, Type: System.Void, Language: C#) (Syntax: 'System.Cons ... Line(value)')
            Instance Receiver: 
            null
            Arguments(1):
                IArgument (ArgumentKind.Explicit, Matching Parameter: value) (OperationKind.Argument, Language: C#) (Syntax: 'value')
                  ILocalReferenceExpression: value (OperationKind.LocalReferenceExpression, Type: System.String, Language: C#) (Syntax: 'value')
                  InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                  OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
  NextVariables(0)
";
            VerifyOperationTreeForTest<ForEachStatementSyntax>(source, expectedOperationTree);
        }

        [CompilerTrait(CompilerFeature.IOperation)]
        [Fact, WorkItem(17602, "https://github.com/dotnet/roslyn/issues/17602")]
        public void IForEachLoopStatement_WithList()
        {
            string source = @"
using System;
using System.Collections.Generic;

class Program
{
    static void Main(string[] args)
    {
        List<string> list = new List<string>();
        list.Add(""a"");
        list.Add(""b"");
        list.Add(""c"");
        /*<bind>*/foreach (string item in list)
        {
            Console.WriteLine(item);
        }/*</bind>*/

    }
}
";
            string expectedOperationTree = @"
IForEachLoopStatement (LoopKind.ForEach) (OperationKind.LoopStatement, Language: C#) (Syntax: 'foreach (st ... }')
  Locals: Local_1: System.String item
  LoopControlVariable: 
    ILocalReferenceExpression: item (IsDeclaration: True) (OperationKind.LocalReferenceExpression, Type: System.String, Constant: null, Language: C#) (Syntax: 'foreach (st ... }')
  Collection: 
    IConversionExpression (Implicit, TryCast: False, Unchecked) (OperationKind.ConversionExpression, Type: System.Collections.Generic.List<System.String>, IsImplicit, Language: C#) (Syntax: 'list')
      Conversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
      Operand: 
        ILocalReferenceExpression: list (OperationKind.LocalReferenceExpression, Type: System.Collections.Generic.List<System.String>, Language: C#) (Syntax: 'list')
  Body: 
    IBlockStatement (1 statements) (OperationKind.BlockStatement, Language: C#) (Syntax: '{ ... }')
      IExpressionStatement (OperationKind.ExpressionStatement, Language: C#) (Syntax: 'Console.WriteLine(item);')
        Expression: 
          IInvocationExpression (void System.Console.WriteLine(System.String value)) (OperationKind.InvocationExpression, Type: System.Void, Language: C#) (Syntax: 'Console.WriteLine(item)')
            Instance Receiver: 
            null
            Arguments(1):
                IArgument (ArgumentKind.Explicit, Matching Parameter: value) (OperationKind.Argument, Language: C#) (Syntax: 'item')
                  ILocalReferenceExpression: item (OperationKind.LocalReferenceExpression, Type: System.String, Language: C#) (Syntax: 'item')
                  InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                  OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
  NextVariables(0)
";
            VerifyOperationTreeForTest<ForEachStatementSyntax>(source, expectedOperationTree);
        }

        [CompilerTrait(CompilerFeature.IOperation)]
        [Fact, WorkItem(17602, "https://github.com/dotnet/roslyn/issues/17602")]
        public void IForEachLoopStatement_WithKeyValue()
        {
            string source = @"
using System;
using System.Collections.Generic;

class Program
{
    static Dictionary<int, int> _h = new Dictionary<int, int>();

    static void Main()
    {
        _h.Add(5, 4);
        _h.Add(4, 3);
        _h.Add(2, 1);

        /*<bind>*/foreach (KeyValuePair<int, int> pair in _h)
        {
            Console.WriteLine(""{0},{1}"", pair.Key, pair.Value);
        }/*</bind>*/
    }
}
";
            string expectedOperationTree = @"
IForEachLoopStatement (LoopKind.ForEach) (OperationKind.LoopStatement, Language: C#) (Syntax: 'foreach (Ke ... }')
  Locals: Local_1: System.Collections.Generic.KeyValuePair<System.Int32, System.Int32> pair
  LoopControlVariable: 
    ILocalReferenceExpression: pair (IsDeclaration: True) (OperationKind.LocalReferenceExpression, Type: System.Collections.Generic.KeyValuePair<System.Int32, System.Int32>, Constant: null, Language: C#) (Syntax: 'foreach (Ke ... }')
  Collection: 
    IConversionExpression (Implicit, TryCast: False, Unchecked) (OperationKind.ConversionExpression, Type: System.Collections.Generic.Dictionary<System.Int32, System.Int32>, IsImplicit, Language: C#) (Syntax: '_h')
      Conversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
      Operand: 
        IFieldReferenceExpression: System.Collections.Generic.Dictionary<System.Int32, System.Int32> Program._h (Static) (OperationKind.FieldReferenceExpression, Type: System.Collections.Generic.Dictionary<System.Int32, System.Int32>, Language: C#) (Syntax: '_h')
          Instance Receiver: 
          null
  Body: 
    IBlockStatement (1 statements) (OperationKind.BlockStatement, Language: C#) (Syntax: '{ ... }')
      IExpressionStatement (OperationKind.ExpressionStatement, Language: C#) (Syntax: 'Console.Wri ... air.Value);')
        Expression: 
          IInvocationExpression (void System.Console.WriteLine(System.String format, System.Object arg0, System.Object arg1)) (OperationKind.InvocationExpression, Type: System.Void, Language: C#) (Syntax: 'Console.Wri ... pair.Value)')
            Instance Receiver: 
            null
            Arguments(3):
                IArgument (ArgumentKind.Explicit, Matching Parameter: format) (OperationKind.Argument, Language: C#) (Syntax: '""{0},{1}""')
                  ILiteralExpression (OperationKind.LiteralExpression, Type: System.String, Constant: ""{0},{1}"", Language: C#) (Syntax: '""{0},{1}""')
                  InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                  OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                IArgument (ArgumentKind.Explicit, Matching Parameter: arg0) (OperationKind.Argument, Language: C#) (Syntax: 'pair.Key')
                  IConversionExpression (Implicit, TryCast: False, Unchecked) (OperationKind.ConversionExpression, Type: System.Object, IsImplicit, Language: C#) (Syntax: 'pair.Key')
                    Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                    Operand: 
                      IPropertyReferenceExpression: System.Int32 System.Collections.Generic.KeyValuePair<System.Int32, System.Int32>.Key { get; } (OperationKind.PropertyReferenceExpression, Type: System.Int32, Language: C#) (Syntax: 'pair.Key')
                        Instance Receiver: 
                          ILocalReferenceExpression: pair (OperationKind.LocalReferenceExpression, Type: System.Collections.Generic.KeyValuePair<System.Int32, System.Int32>, Language: C#) (Syntax: 'pair')
                  InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                  OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                IArgument (ArgumentKind.Explicit, Matching Parameter: arg1) (OperationKind.Argument, Language: C#) (Syntax: 'pair.Value')
                  IConversionExpression (Implicit, TryCast: False, Unchecked) (OperationKind.ConversionExpression, Type: System.Object, IsImplicit, Language: C#) (Syntax: 'pair.Value')
                    Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                    Operand: 
                      IPropertyReferenceExpression: System.Int32 System.Collections.Generic.KeyValuePair<System.Int32, System.Int32>.Value { get; } (OperationKind.PropertyReferenceExpression, Type: System.Int32, Language: C#) (Syntax: 'pair.Value')
                        Instance Receiver: 
                          ILocalReferenceExpression: pair (OperationKind.LocalReferenceExpression, Type: System.Collections.Generic.KeyValuePair<System.Int32, System.Int32>, Language: C#) (Syntax: 'pair')
                  InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                  OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
  NextVariables(0)
";
            VerifyOperationTreeForTest<ForEachStatementSyntax>(source, expectedOperationTree);
        }

        [CompilerTrait(CompilerFeature.IOperation)]
        [Fact, WorkItem(17602, "https://github.com/dotnet/roslyn/issues/17602")]
        public void IForEachLoopStatement_WithBreak()
        {
            string source = @"
class Program
{
    static void Main()
    {
        int[] numbers = { 1,2,3,4};

        /*<bind>*/foreach (int num in numbers)
        {
            if (num>3)
            {
                break;
            }
            System.Console.WriteLine(num);
        }/*</bind>*/
    }
}
";
            string expectedOperationTree = @"
IForEachLoopStatement (LoopKind.ForEach) (OperationKind.LoopStatement, Language: C#) (Syntax: 'foreach (in ... }')
  Locals: Local_1: System.Int32 num
  LoopControlVariable: 
    ILocalReferenceExpression: num (IsDeclaration: True) (OperationKind.LocalReferenceExpression, Type: System.Int32, Constant: null, Language: C#) (Syntax: 'foreach (in ... }')
  Collection: 
    IConversionExpression (Implicit, TryCast: False, Unchecked) (OperationKind.ConversionExpression, Type: System.Collections.IEnumerable, IsImplicit, Language: C#) (Syntax: 'numbers')
      Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: True, IsUserDefined: False) (MethodSymbol: null)
      Operand: 
        ILocalReferenceExpression: numbers (OperationKind.LocalReferenceExpression, Type: System.Int32[], Language: C#) (Syntax: 'numbers')
  Body: 
    IBlockStatement (2 statements) (OperationKind.BlockStatement, Language: C#) (Syntax: '{ ... }')
      IIfStatement (OperationKind.IfStatement, Language: C#) (Syntax: 'if (num>3) ... }')
        Condition: 
          IBinaryOperatorExpression (BinaryOperatorKind.GreaterThan) (OperationKind.BinaryOperatorExpression, Type: System.Boolean, Language: C#) (Syntax: 'num>3')
            Left: 
              ILocalReferenceExpression: num (OperationKind.LocalReferenceExpression, Type: System.Int32, Language: C#) (Syntax: 'num')
            Right: 
              ILiteralExpression (OperationKind.LiteralExpression, Type: System.Int32, Constant: 3, Language: C#) (Syntax: '3')
        IfTrue: 
          IBlockStatement (1 statements) (OperationKind.BlockStatement, Language: C#) (Syntax: '{ ... }')
            IBranchStatement (BranchKind.Break) (OperationKind.BranchStatement, Language: C#) (Syntax: 'break;')
        IfFalse: 
        null
      IExpressionStatement (OperationKind.ExpressionStatement, Language: C#) (Syntax: 'System.Cons ... eLine(num);')
        Expression: 
          IInvocationExpression (void System.Console.WriteLine(System.Int32 value)) (OperationKind.InvocationExpression, Type: System.Void, Language: C#) (Syntax: 'System.Cons ... teLine(num)')
            Instance Receiver: 
            null
            Arguments(1):
                IArgument (ArgumentKind.Explicit, Matching Parameter: value) (OperationKind.Argument, Language: C#) (Syntax: 'num')
                  ILocalReferenceExpression: num (OperationKind.LocalReferenceExpression, Type: System.Int32, Language: C#) (Syntax: 'num')
                  InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                  OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
  NextVariables(0)
";
            VerifyOperationTreeForTest<ForEachStatementSyntax>(source, expectedOperationTree);
        }

        [CompilerTrait(CompilerFeature.IOperation)]
        [Fact, WorkItem(17602, "https://github.com/dotnet/roslyn/issues/17602")]
        public void IForEachLoopStatement_WithContinue()
        {
            string source = @"
class Program
{
    static void Main()
    {
        int[] numbers = { 1,2,3,4};

        /*<bind>*/foreach (int num in numbers)
        {
            if (num>3)
            {
                continue;
            }
            System.Console.WriteLine(num);
        }/*</bind>*/
    }
}
";
            string expectedOperationTree = @"
IForEachLoopStatement (LoopKind.ForEach) (OperationKind.LoopStatement, Language: C#) (Syntax: 'foreach (in ... }')
  Locals: Local_1: System.Int32 num
  LoopControlVariable: 
    ILocalReferenceExpression: num (IsDeclaration: True) (OperationKind.LocalReferenceExpression, Type: System.Int32, Constant: null, Language: C#) (Syntax: 'foreach (in ... }')
  Collection: 
    IConversionExpression (Implicit, TryCast: False, Unchecked) (OperationKind.ConversionExpression, Type: System.Collections.IEnumerable, IsImplicit, Language: C#) (Syntax: 'numbers')
      Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: True, IsUserDefined: False) (MethodSymbol: null)
      Operand: 
        ILocalReferenceExpression: numbers (OperationKind.LocalReferenceExpression, Type: System.Int32[], Language: C#) (Syntax: 'numbers')
  Body: 
    IBlockStatement (2 statements) (OperationKind.BlockStatement, Language: C#) (Syntax: '{ ... }')
      IIfStatement (OperationKind.IfStatement, Language: C#) (Syntax: 'if (num>3) ... }')
        Condition: 
          IBinaryOperatorExpression (BinaryOperatorKind.GreaterThan) (OperationKind.BinaryOperatorExpression, Type: System.Boolean, Language: C#) (Syntax: 'num>3')
            Left: 
              ILocalReferenceExpression: num (OperationKind.LocalReferenceExpression, Type: System.Int32, Language: C#) (Syntax: 'num')
            Right: 
              ILiteralExpression (OperationKind.LiteralExpression, Type: System.Int32, Constant: 3, Language: C#) (Syntax: '3')
        IfTrue: 
          IBlockStatement (1 statements) (OperationKind.BlockStatement, Language: C#) (Syntax: '{ ... }')
            IBranchStatement (BranchKind.Continue) (OperationKind.BranchStatement, Language: C#) (Syntax: 'continue;')
        IfFalse: 
        null
      IExpressionStatement (OperationKind.ExpressionStatement, Language: C#) (Syntax: 'System.Cons ... eLine(num);')
        Expression: 
          IInvocationExpression (void System.Console.WriteLine(System.Int32 value)) (OperationKind.InvocationExpression, Type: System.Void, Language: C#) (Syntax: 'System.Cons ... teLine(num)')
            Instance Receiver: 
            null
            Arguments(1):
                IArgument (ArgumentKind.Explicit, Matching Parameter: value) (OperationKind.Argument, Language: C#) (Syntax: 'num')
                  ILocalReferenceExpression: num (OperationKind.LocalReferenceExpression, Type: System.Int32, Language: C#) (Syntax: 'num')
                  InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                  OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
  NextVariables(0)
";
            VerifyOperationTreeForTest<ForEachStatementSyntax>(source, expectedOperationTree);
        }

        [CompilerTrait(CompilerFeature.IOperation)]
        [Fact, WorkItem(17602, "https://github.com/dotnet/roslyn/issues/17602")]
        public void IForEachLoopStatement_QueryExpression()
        {
            string source = @"
class Program
{
    static void Main()
    {
        string[] letters = { ""d"", ""c"", ""a"", ""b"" };
        var sorted = from letter in letters
                     orderby letter
                     select letter;
        /*<bind>*/foreach (string value in sorted)
        {
            System.Console.WriteLine(value);
        }/*</bind>*/
    }
}
";
            string expectedOperationTree = @"
IForEachLoopStatement (LoopKind.ForEach) (OperationKind.LoopStatement, IsInvalid, Language: C#) (Syntax: 'foreach (st ... }')
  Locals: Local_1: System.String value
  LoopControlVariable: 
    ILocalReferenceExpression: value (IsDeclaration: True) (OperationKind.LocalReferenceExpression, Type: System.String, Constant: null, IsInvalid, Language: C#) (Syntax: 'foreach (st ... }')
  Collection: 
    ILocalReferenceExpression: sorted (OperationKind.LocalReferenceExpression, Type: ?, IsInvalid, Language: C#) (Syntax: 'sorted')
  Body: 
    IBlockStatement (1 statements) (OperationKind.BlockStatement, Language: C#) (Syntax: '{ ... }')
      IExpressionStatement (OperationKind.ExpressionStatement, Language: C#) (Syntax: 'System.Cons ... ine(value);')
        Expression: 
          IInvocationExpression (void System.Console.WriteLine(System.String value)) (OperationKind.InvocationExpression, Type: System.Void, Language: C#) (Syntax: 'System.Cons ... Line(value)')
            Instance Receiver: 
            null
            Arguments(1):
                IArgument (ArgumentKind.Explicit, Matching Parameter: value) (OperationKind.Argument, Language: C#) (Syntax: 'value')
                  ILocalReferenceExpression: value (OperationKind.LocalReferenceExpression, Type: System.String, Language: C#) (Syntax: 'value')
                  InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                  OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
  NextVariables(0)
";
            VerifyOperationTreeForTest<ForEachStatementSyntax>(source, expectedOperationTree);
        }

        [CompilerTrait(CompilerFeature.IOperation)]
        [Fact, WorkItem(17602, "https://github.com/dotnet/roslyn/issues/17602")]
        public void IForEachLoopStatement_Struct()
        {
            string source = @"
using System.Reflection;

namespace DisplayStructContentsTest
{
    class Program
    {

        struct Employee
        {
            public string name;
            public int age;
            public string location;
        };

        static void Main(string[] args)
        {
            Employee employee;

            employee.name = ""name1"";
            employee.age = 35;
            employee.location = ""loc"";

            /*<bind>*/foreach (FieldInfo fi in employee.GetType().GetFields())
            {
                System.Console.WriteLine(fi.Name + "" = "" +
                System.Convert.ToString(fi.GetValue(employee)));
            }/*</bind>*/

        }
    }
}
";
            string expectedOperationTree = @"
IForEachLoopStatement (LoopKind.ForEach) (OperationKind.LoopStatement, Language: C#) (Syntax: 'foreach (Fi ... }')
  Locals: Local_1: System.Reflection.FieldInfo fi
  LoopControlVariable: 
    ILocalReferenceExpression: fi (IsDeclaration: True) (OperationKind.LocalReferenceExpression, Type: System.Reflection.FieldInfo, Constant: null, Language: C#) (Syntax: 'foreach (Fi ... }')
  Collection: 
    IConversionExpression (Implicit, TryCast: False, Unchecked) (OperationKind.ConversionExpression, Type: System.Collections.IEnumerable, IsImplicit, Language: C#) (Syntax: 'employee.Ge ... GetFields()')
      Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: True, IsUserDefined: False) (MethodSymbol: null)
      Operand: 
        IInvocationExpression ( System.Reflection.FieldInfo[] System.Type.GetFields()) (OperationKind.InvocationExpression, Type: System.Reflection.FieldInfo[], Language: C#) (Syntax: 'employee.Ge ... GetFields()')
          Instance Receiver: 
            IInvocationExpression ( System.Type System.Object.GetType()) (OperationKind.InvocationExpression, Type: System.Type, Language: C#) (Syntax: 'employee.GetType()')
              Instance Receiver: 
                ILocalReferenceExpression: employee (OperationKind.LocalReferenceExpression, Type: DisplayStructContentsTest.Program.Employee, Language: C#) (Syntax: 'employee')
              Arguments(0)
          Arguments(0)
  Body: 
    IBlockStatement (1 statements) (OperationKind.BlockStatement, Language: C#) (Syntax: '{ ... }')
      IExpressionStatement (OperationKind.ExpressionStatement, Language: C#) (Syntax: 'System.Cons ... mployee)));')
        Expression: 
          IInvocationExpression (void System.Console.WriteLine(System.String value)) (OperationKind.InvocationExpression, Type: System.Void, Language: C#) (Syntax: 'System.Cons ... employee)))')
            Instance Receiver: 
            null
            Arguments(1):
                IArgument (ArgumentKind.Explicit, Matching Parameter: value) (OperationKind.Argument, Language: C#) (Syntax: 'fi.Name + "" ... (employee))')
                  IBinaryOperatorExpression (BinaryOperatorKind.Add) (OperationKind.BinaryOperatorExpression, Type: System.String, Language: C#) (Syntax: 'fi.Name + "" ... (employee))')
                    Left: 
                      IBinaryOperatorExpression (BinaryOperatorKind.Add) (OperationKind.BinaryOperatorExpression, Type: System.String, Language: C#) (Syntax: 'fi.Name + "" = ""')
                        Left: 
                          IPropertyReferenceExpression: System.String System.Reflection.MemberInfo.Name { get; } (OperationKind.PropertyReferenceExpression, Type: System.String, Language: C#) (Syntax: 'fi.Name')
                            Instance Receiver: 
                              ILocalReferenceExpression: fi (OperationKind.LocalReferenceExpression, Type: System.Reflection.FieldInfo, Language: C#) (Syntax: 'fi')
                        Right: 
                          ILiteralExpression (OperationKind.LiteralExpression, Type: System.String, Constant: "" = "", Language: C#) (Syntax: '"" = ""')
                    Right: 
                      IInvocationExpression (System.String System.Convert.ToString(System.Object value)) (OperationKind.InvocationExpression, Type: System.String, Language: C#) (Syntax: 'System.Conv ... (employee))')
                        Instance Receiver: 
                        null
                        Arguments(1):
                            IArgument (ArgumentKind.Explicit, Matching Parameter: value) (OperationKind.Argument, Language: C#) (Syntax: 'fi.GetValue(employee)')
                              IInvocationExpression (virtual System.Object System.Reflection.FieldInfo.GetValue(System.Object obj)) (OperationKind.InvocationExpression, Type: System.Object, Language: C#) (Syntax: 'fi.GetValue(employee)')
                                Instance Receiver: 
                                  ILocalReferenceExpression: fi (OperationKind.LocalReferenceExpression, Type: System.Reflection.FieldInfo, Language: C#) (Syntax: 'fi')
                                Arguments(1):
                                    IArgument (ArgumentKind.Explicit, Matching Parameter: obj) (OperationKind.Argument, Language: C#) (Syntax: 'employee')
                                      IConversionExpression (Implicit, TryCast: False, Unchecked) (OperationKind.ConversionExpression, Type: System.Object, IsImplicit, Language: C#) (Syntax: 'employee')
                                        Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                                        Operand: 
                                          ILocalReferenceExpression: employee (OperationKind.LocalReferenceExpression, Type: DisplayStructContentsTest.Program.Employee, Language: C#) (Syntax: 'employee')
                                      InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                                      OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                              InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                              OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                  InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                  OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
  NextVariables(0)
";
            VerifyOperationTreeForTest<ForEachStatementSyntax>(source, expectedOperationTree);
        }

        [CompilerTrait(CompilerFeature.IOperation)]
        [Fact, WorkItem(17602, "https://github.com/dotnet/roslyn/issues/17602")]
        public void IForEachLoopStatement_String()
        {
            string source = @"
class Class1
{
    public void M()
    {
        const string s = """";
        /*<bind>*/foreach (char c in s)
        {
            System.Console.WriteLine(c);
        }/*</bind>*/

    }
}

";
            string expectedOperationTree = @"
IForEachLoopStatement (LoopKind.ForEach) (OperationKind.LoopStatement, Language: C#) (Syntax: 'foreach (ch ... }')
  Locals: Local_1: System.Char c
  LoopControlVariable: 
    ILocalReferenceExpression: c (IsDeclaration: True) (OperationKind.LocalReferenceExpression, Type: System.Char, Constant: null, Language: C#) (Syntax: 'foreach (ch ... }')
  Collection: 
    IConversionExpression (Implicit, TryCast: False, Unchecked) (OperationKind.ConversionExpression, Type: System.String, IsImplicit, Language: C#) (Syntax: 's')
      Conversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
      Operand: 
        ILocalReferenceExpression: s (OperationKind.LocalReferenceExpression, Type: System.String, Constant: """", Language: C#) (Syntax: 's')
  Body: 
    IBlockStatement (1 statements) (OperationKind.BlockStatement, Language: C#) (Syntax: '{ ... }')
      IExpressionStatement (OperationKind.ExpressionStatement, Language: C#) (Syntax: 'System.Cons ... iteLine(c);')
        Expression: 
          IInvocationExpression (void System.Console.WriteLine(System.Char value)) (OperationKind.InvocationExpression, Type: System.Void, Language: C#) (Syntax: 'System.Cons ... riteLine(c)')
            Instance Receiver: 
            null
            Arguments(1):
                IArgument (ArgumentKind.Explicit, Matching Parameter: value) (OperationKind.Argument, Language: C#) (Syntax: 'c')
                  ILocalReferenceExpression: c (OperationKind.LocalReferenceExpression, Type: System.Char, Language: C#) (Syntax: 'c')
                  InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                  OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
  NextVariables(0)
";
            VerifyOperationTreeForTest<ForEachStatementSyntax>(source, expectedOperationTree);
        }

        [CompilerTrait(CompilerFeature.IOperation)]
        [Fact, WorkItem(17602, "https://github.com/dotnet/roslyn/issues/17602")]
        public void IForEachLoopStatement_WithVar()
        {
            string source = @"
using System.Collections.Generic;
class Program
{
    static Dictionary<int, int> _f = new Dictionary<int, int>();

    static void Main()
    {
        _f.Add(1, 2);
        _f.Add(2, 3);
        _f.Add(3, 4);

        /*<bind>*/foreach (var pair in _f)
        {
            System.Console.WriteLine(""{0},{1}"", pair.Key, pair.Value);
        }/*</bind>*/
    }
}
";
            string expectedOperationTree = @"
IForEachLoopStatement (LoopKind.ForEach) (OperationKind.LoopStatement, Language: C#) (Syntax: 'foreach (va ... }')
  Locals: Local_1: System.Collections.Generic.KeyValuePair<System.Int32, System.Int32> pair
  LoopControlVariable: 
    ILocalReferenceExpression: pair (IsDeclaration: True) (OperationKind.LocalReferenceExpression, Type: System.Collections.Generic.KeyValuePair<System.Int32, System.Int32>, Constant: null, Language: C#) (Syntax: 'foreach (va ... }')
  Collection: 
    IConversionExpression (Implicit, TryCast: False, Unchecked) (OperationKind.ConversionExpression, Type: System.Collections.Generic.Dictionary<System.Int32, System.Int32>, IsImplicit, Language: C#) (Syntax: '_f')
      Conversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
      Operand: 
        IFieldReferenceExpression: System.Collections.Generic.Dictionary<System.Int32, System.Int32> Program._f (Static) (OperationKind.FieldReferenceExpression, Type: System.Collections.Generic.Dictionary<System.Int32, System.Int32>, Language: C#) (Syntax: '_f')
          Instance Receiver: 
          null
  Body: 
    IBlockStatement (1 statements) (OperationKind.BlockStatement, Language: C#) (Syntax: '{ ... }')
      IExpressionStatement (OperationKind.ExpressionStatement, Language: C#) (Syntax: 'System.Cons ... air.Value);')
        Expression: 
          IInvocationExpression (void System.Console.WriteLine(System.String format, System.Object arg0, System.Object arg1)) (OperationKind.InvocationExpression, Type: System.Void, Language: C#) (Syntax: 'System.Cons ... pair.Value)')
            Instance Receiver: 
            null
            Arguments(3):
                IArgument (ArgumentKind.Explicit, Matching Parameter: format) (OperationKind.Argument, Language: C#) (Syntax: '""{0},{1}""')
                  ILiteralExpression (OperationKind.LiteralExpression, Type: System.String, Constant: ""{0},{1}"", Language: C#) (Syntax: '""{0},{1}""')
                  InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                  OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                IArgument (ArgumentKind.Explicit, Matching Parameter: arg0) (OperationKind.Argument, Language: C#) (Syntax: 'pair.Key')
                  IConversionExpression (Implicit, TryCast: False, Unchecked) (OperationKind.ConversionExpression, Type: System.Object, IsImplicit, Language: C#) (Syntax: 'pair.Key')
                    Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                    Operand: 
                      IPropertyReferenceExpression: System.Int32 System.Collections.Generic.KeyValuePair<System.Int32, System.Int32>.Key { get; } (OperationKind.PropertyReferenceExpression, Type: System.Int32, Language: C#) (Syntax: 'pair.Key')
                        Instance Receiver: 
                          ILocalReferenceExpression: pair (OperationKind.LocalReferenceExpression, Type: System.Collections.Generic.KeyValuePair<System.Int32, System.Int32>, Language: C#) (Syntax: 'pair')
                  InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                  OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                IArgument (ArgumentKind.Explicit, Matching Parameter: arg1) (OperationKind.Argument, Language: C#) (Syntax: 'pair.Value')
                  IConversionExpression (Implicit, TryCast: False, Unchecked) (OperationKind.ConversionExpression, Type: System.Object, IsImplicit, Language: C#) (Syntax: 'pair.Value')
                    Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                    Operand: 
                      IPropertyReferenceExpression: System.Int32 System.Collections.Generic.KeyValuePair<System.Int32, System.Int32>.Value { get; } (OperationKind.PropertyReferenceExpression, Type: System.Int32, Language: C#) (Syntax: 'pair.Value')
                        Instance Receiver: 
                          ILocalReferenceExpression: pair (OperationKind.LocalReferenceExpression, Type: System.Collections.Generic.KeyValuePair<System.Int32, System.Int32>, Language: C#) (Syntax: 'pair')
                  InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                  OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
  NextVariables(0)
";
            VerifyOperationTreeForTest<ForEachStatementSyntax>(source, expectedOperationTree);
        }

        [CompilerTrait(CompilerFeature.IOperation)]
        [Fact, WorkItem(17602, "https://github.com/dotnet/roslyn/issues/17602")]
        public void IForEachLoopStatement_BadElementType()
        {
            string source = @"
class C
{
    static void Main()
    {
        System.Collections.IEnumerable sequence = null;
        /*<bind>*/foreach (MissingType x in sequence)
        {
            bool b = !x.Equals(null);
        }/*</bind>*/
    }
}
";
            string expectedOperationTree = @"
IForEachLoopStatement (LoopKind.ForEach) (OperationKind.LoopStatement, IsInvalid, Language: C#) (Syntax: 'foreach (Mi ... }')
  Locals: Local_1: MissingType x
  LoopControlVariable: 
    ILocalReferenceExpression: x (IsDeclaration: True) (OperationKind.LocalReferenceExpression, Type: MissingType, Constant: null, IsInvalid, Language: C#) (Syntax: 'foreach (Mi ... }')
  Collection: 
    ILocalReferenceExpression: sequence (OperationKind.LocalReferenceExpression, Type: System.Collections.IEnumerable, Language: C#) (Syntax: 'sequence')
  Body: 
    IBlockStatement (1 statements, 1 locals) (OperationKind.BlockStatement, Language: C#) (Syntax: '{ ... }')
      Locals: Local_1: System.Boolean b
      IVariableDeclarationStatement (1 declarations) (OperationKind.VariableDeclarationStatement, Language: C#) (Syntax: 'bool b = !x ... uals(null);')
        IVariableDeclaration (1 variables) (OperationKind.VariableDeclaration, Language: C#) (Syntax: 'b = !x.Equals(null)')
          Variables: Local_1: System.Boolean b
          Initializer: 
            IConversionExpression (Implicit, TryCast: False, Unchecked) (OperationKind.ConversionExpression, Type: System.Boolean, IsImplicit, Language: C#) (Syntax: '!x.Equals(null)')
              Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
              Operand: 
                IUnaryOperatorExpression (UnaryOperatorKind.Not) (OperationKind.UnaryOperatorExpression, Type: System.Object, Language: C#) (Syntax: '!x.Equals(null)')
                  Operand: 
                    IInvocationExpression ( ? C.()) (OperationKind.InvocationExpression, Type: ?, Language: C#) (Syntax: 'x.Equals(null)')
                      Instance Receiver: 
                        IOperation:  (OperationKind.None, Language: C#) (Syntax: 'x.Equals')
                          Children(1):
                              ILocalReferenceExpression: x (OperationKind.LocalReferenceExpression, Type: MissingType, Language: C#) (Syntax: 'x')
                      Arguments(1):
                          IArgument (ArgumentKind.Explicit, Matching Parameter: null) (OperationKind.Argument, Language: C#) (Syntax: 'null')
                            ILiteralExpression (OperationKind.LiteralExpression, Type: null, Constant: null, Language: C#) (Syntax: 'null')
                            InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                            OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
  NextVariables(0)
";
            VerifyOperationTreeForTest<ForEachStatementSyntax>(source, expectedOperationTree);
        }

        [CompilerTrait(CompilerFeature.IOperation)]
        [Fact, WorkItem(17602, "https://github.com/dotnet/roslyn/issues/17602")]
        public void IForEachLoopStatement_NullLiteralCollection()
        {
            string source = @"
class C
{
    static void Main()
    {
        /*<bind>*/foreach (int x in null)
        {
        }/*</bind>*/
    }
}
";
            string expectedOperationTree = @"
IForEachLoopStatement (LoopKind.ForEach) (OperationKind.LoopStatement, IsInvalid, Language: C#) (Syntax: 'foreach (in ... }')
  Locals: Local_1: System.Int32 x
  LoopControlVariable: 
    ILocalReferenceExpression: x (IsDeclaration: True) (OperationKind.LocalReferenceExpression, Type: System.Int32, Constant: null, IsInvalid, Language: C#) (Syntax: 'foreach (in ... }')
  Collection: 
    ILiteralExpression (OperationKind.LiteralExpression, Type: null, Constant: null, IsInvalid, Language: C#) (Syntax: 'null')
  Body: 
    IBlockStatement (0 statements) (OperationKind.BlockStatement, Language: C#) (Syntax: '{ ... }')
  NextVariables(0)
";
            VerifyOperationTreeForTest<ForEachStatementSyntax>(source, expectedOperationTree);
        }

        [CompilerTrait(CompilerFeature.IOperation)]
        [Fact, WorkItem(17602, "https://github.com/dotnet/roslyn/issues/17602")]
        public void IForEachLoopStatement_NoElementCollection()
        {
            string source = @"
class C
{
    static void Main(string[] args)
    {
        /*<bind>*/foreach (int x in args)
        {
        }/*</bind>*/
    }
}
";
            string expectedOperationTree = @"
IForEachLoopStatement (LoopKind.ForEach) (OperationKind.LoopStatement, IsInvalid, Language: C#) (Syntax: 'foreach (in ... }')
  Locals: Local_1: System.Int32 x
  LoopControlVariable: 
    ILocalReferenceExpression: x (IsDeclaration: True) (OperationKind.LocalReferenceExpression, Type: System.Int32, Constant: null, IsInvalid, Language: C#) (Syntax: 'foreach (in ... }')
  Collection: 
    IConversionExpression (Implicit, TryCast: False, Unchecked) (OperationKind.ConversionExpression, Type: System.Collections.IEnumerable, IsImplicit, Language: C#) (Syntax: 'args')
      Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: True, IsUserDefined: False) (MethodSymbol: null)
      Operand: 
        IParameterReferenceExpression: args (OperationKind.ParameterReferenceExpression, Type: System.String[], Language: C#) (Syntax: 'args')
  Body: 
    IBlockStatement (0 statements) (OperationKind.BlockStatement, Language: C#) (Syntax: '{ ... }')
  NextVariables(0)
";
            VerifyOperationTreeForTest<ForEachStatementSyntax>(source, expectedOperationTree);
        }

        [CompilerTrait(CompilerFeature.IOperation)]
        [Fact, WorkItem(17602, "https://github.com/dotnet/roslyn/issues/17602")]
        public void IForEachLoopStatement_ModifyIterationVariable()
        {
            string source = @"
class C
{
    void F(int[] a)
    {
        /*<bind>*/foreach (int x in a) { x++; }/*</bind>*/
    }
}
";
            string expectedOperationTree = @"
IForEachLoopStatement (LoopKind.ForEach) (OperationKind.LoopStatement, IsInvalid, Language: C#) (Syntax: 'foreach (in ... a) { x++; }')
  Locals: Local_1: System.Int32 x
  LoopControlVariable: 
    ILocalReferenceExpression: x (IsDeclaration: True) (OperationKind.LocalReferenceExpression, Type: System.Int32, Constant: null, IsInvalid, Language: C#) (Syntax: 'foreach (in ... a) { x++; }')
  Collection: 
    IConversionExpression (Implicit, TryCast: False, Unchecked) (OperationKind.ConversionExpression, Type: System.Collections.IEnumerable, IsImplicit, Language: C#) (Syntax: 'a')
      Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: True, IsUserDefined: False) (MethodSymbol: null)
      Operand: 
        IParameterReferenceExpression: a (OperationKind.ParameterReferenceExpression, Type: System.Int32[], Language: C#) (Syntax: 'a')
  Body: 
    IBlockStatement (1 statements) (OperationKind.BlockStatement, IsInvalid, Language: C#) (Syntax: '{ x++; }')
      IExpressionStatement (OperationKind.ExpressionStatement, IsInvalid, Language: C#) (Syntax: 'x++;')
        Expression: 
          IIncrementOrDecrementExpression (Postfix) (OperationKind.IncrementExpression, Type: System.Object, IsInvalid, Language: C#) (Syntax: 'x++')
            Target: 
              IInvalidExpression (OperationKind.InvalidExpression, Type: System.Int32, IsInvalid, IsImplicit, Language: C#) (Syntax: 'x')
                Children(1):
                    ILocalReferenceExpression: x (OperationKind.LocalReferenceExpression, Type: System.Int32, IsInvalid, Language: C#) (Syntax: 'x')
  NextVariables(0)
";
            VerifyOperationTreeForTest<ForEachStatementSyntax>(source, expectedOperationTree);
        }

        [CompilerTrait(CompilerFeature.IOperation)]
        [Fact, WorkItem(17602, "https://github.com/dotnet/roslyn/issues/17602")]
        public void IForEachLoopStatement_Pattern()
        {
            string source = @"
class C
{
    void F(Enumerable e)
    {
        /*<bind>*/foreach (long x in e) { }/*</bind>*/
    }
}

class Enumerable
{
    public Enumerator GetEnumerator() { return new Enumerator(); }
}

class Enumerator
{
    public int Current { get { return 1; } }
    public bool MoveNext() { return false; }
}

";
            string expectedOperationTree = @"
IForEachLoopStatement (LoopKind.ForEach) (OperationKind.LoopStatement, Language: C#) (Syntax: 'foreach (lo ... x in e) { }')
  Locals: Local_1: System.Int64 x
  LoopControlVariable: 
    ILocalReferenceExpression: x (IsDeclaration: True) (OperationKind.LocalReferenceExpression, Type: System.Int64, Constant: null, Language: C#) (Syntax: 'foreach (lo ... x in e) { }')
  Collection: 
    IConversionExpression (Implicit, TryCast: False, Unchecked) (OperationKind.ConversionExpression, Type: Enumerable, IsImplicit, Language: C#) (Syntax: 'e')
      Conversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
      Operand: 
        IParameterReferenceExpression: e (OperationKind.ParameterReferenceExpression, Type: Enumerable, Language: C#) (Syntax: 'e')
  Body: 
    IBlockStatement (0 statements) (OperationKind.BlockStatement, Language: C#) (Syntax: '{ }')
  NextVariables(0)
";
            VerifyOperationTreeForTest<ForEachStatementSyntax>(source, expectedOperationTree);
        }

        [CompilerTrait(CompilerFeature.IOperation)]
        [Fact, WorkItem(17602, "https://github.com/dotnet/roslyn/issues/17602")]
        public void IForEachLoopStatement_ImplicitlyTypedString()
        {
            string source = @"
class C
{
    void F(string s)
    {
        /*<bind>*/foreach (var x in s) { }/*</bind>*/
    }
}
";
            string expectedOperationTree = @"
IForEachLoopStatement (LoopKind.ForEach) (OperationKind.LoopStatement, Language: C#) (Syntax: 'foreach (var x in s) { }')
  Locals: Local_1: System.Char x
  LoopControlVariable: 
    ILocalReferenceExpression: x (IsDeclaration: True) (OperationKind.LocalReferenceExpression, Type: System.Char, Constant: null, Language: C#) (Syntax: 'foreach (var x in s) { }')
  Collection: 
    IConversionExpression (Implicit, TryCast: False, Unchecked) (OperationKind.ConversionExpression, Type: System.String, IsImplicit, Language: C#) (Syntax: 's')
      Conversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
      Operand: 
        IParameterReferenceExpression: s (OperationKind.ParameterReferenceExpression, Type: System.String, Language: C#) (Syntax: 's')
  Body: 
    IBlockStatement (0 statements) (OperationKind.BlockStatement, Language: C#) (Syntax: '{ }')
  NextVariables(0)
";
            VerifyOperationTreeForTest<ForEachStatementSyntax>(source, expectedOperationTree);
        }

        [CompilerTrait(CompilerFeature.IOperation)]
        [Fact, WorkItem(17602, "https://github.com/dotnet/roslyn/issues/17602")]
        public void IForEachLoopStatement_ExplicitlyTypedVar()
        {
            string source = @"
class C
{
    void F(var[] a)
    {
        /*<bind>*/foreach (var x in a) { }/*</bind>*/
    }

    class var { }
}
";
            string expectedOperationTree = @"
IForEachLoopStatement (LoopKind.ForEach) (OperationKind.LoopStatement, Language: C#) (Syntax: 'foreach (var x in a) { }')
  Locals: Local_1: C.var x
  LoopControlVariable: 
    ILocalReferenceExpression: x (IsDeclaration: True) (OperationKind.LocalReferenceExpression, Type: C.var, Constant: null, Language: C#) (Syntax: 'foreach (var x in a) { }')
  Collection: 
    IConversionExpression (Implicit, TryCast: False, Unchecked) (OperationKind.ConversionExpression, Type: System.Collections.IEnumerable, IsImplicit, Language: C#) (Syntax: 'a')
      Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: True, IsUserDefined: False) (MethodSymbol: null)
      Operand: 
        IParameterReferenceExpression: a (OperationKind.ParameterReferenceExpression, Type: C.var[], Language: C#) (Syntax: 'a')
  Body: 
    IBlockStatement (0 statements) (OperationKind.BlockStatement, Language: C#) (Syntax: '{ }')
  NextVariables(0)
";
            VerifyOperationTreeForTest<ForEachStatementSyntax>(source, expectedOperationTree);
        }

        [CompilerTrait(CompilerFeature.IOperation)]
        [Fact, WorkItem(17602, "https://github.com/dotnet/roslyn/issues/17602")]
        public void IForEachLoopStatement_DynamicEnumerable()
        {
            string source = @"
class C
{
    void F(dynamic d)
    {
        /*<bind>*/foreach (int x in d) { }/*</bind>*/
    }
}
";
            string expectedOperationTree = @"
IForEachLoopStatement (LoopKind.ForEach) (OperationKind.LoopStatement, Language: C#) (Syntax: 'foreach (int x in d) { }')
  Locals: Local_1: System.Int32 x
  LoopControlVariable: 
    ILocalReferenceExpression: x (IsDeclaration: True) (OperationKind.LocalReferenceExpression, Type: System.Int32, Constant: null, Language: C#) (Syntax: 'foreach (int x in d) { }')
  Collection: 
    IConversionExpression (Implicit, TryCast: False, Unchecked) (OperationKind.ConversionExpression, Type: System.Collections.IEnumerable, IsImplicit, Language: C#) (Syntax: 'd')
      Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
      Operand: 
        IParameterReferenceExpression: d (OperationKind.ParameterReferenceExpression, Type: dynamic, Language: C#) (Syntax: 'd')
  Body: 
    IBlockStatement (0 statements) (OperationKind.BlockStatement, Language: C#) (Syntax: '{ }')
  NextVariables(0)
";
            VerifyOperationTreeForTest<ForEachStatementSyntax>(source, expectedOperationTree);
        }

        [CompilerTrait(CompilerFeature.IOperation)]
        [Fact, WorkItem(17602, "https://github.com/dotnet/roslyn/issues/17602")]
        public void IForEachLoopStatement_TypeParameterConstrainedToInterface()
        {
            string source = @"
class C
{
    static void Test<T>() where T : System.Collections.IEnumerator
    {
        /*<bind>*/foreach (object x in new Enumerable<T>())
        {
            System.Console.WriteLine(x);
        }/*</bind>*/
    }
}

public class Enumerable<T>
{
    public T GetEnumerator() { return default(T); }
}
";
            string expectedOperationTree = @"
IForEachLoopStatement (LoopKind.ForEach) (OperationKind.LoopStatement, Language: C#) (Syntax: 'foreach (ob ... }')
  Locals: Local_1: System.Object x
  LoopControlVariable: 
    ILocalReferenceExpression: x (IsDeclaration: True) (OperationKind.LocalReferenceExpression, Type: System.Object, Constant: null, Language: C#) (Syntax: 'foreach (ob ... }')
  Collection: 
    IConversionExpression (Implicit, TryCast: False, Unchecked) (OperationKind.ConversionExpression, Type: Enumerable<T>, IsImplicit, Language: C#) (Syntax: 'new Enumerable<T>()')
      Conversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
      Operand: 
        IObjectCreationExpression (Constructor: Enumerable<T>..ctor()) (OperationKind.ObjectCreationExpression, Type: Enumerable<T>, Language: C#) (Syntax: 'new Enumerable<T>()')
          Arguments(0)
          Initializer: 
          null
  Body: 
    IBlockStatement (1 statements) (OperationKind.BlockStatement, Language: C#) (Syntax: '{ ... }')
      IExpressionStatement (OperationKind.ExpressionStatement, Language: C#) (Syntax: 'System.Cons ... iteLine(x);')
        Expression: 
          IInvocationExpression (void System.Console.WriteLine(System.Object value)) (OperationKind.InvocationExpression, Type: System.Void, Language: C#) (Syntax: 'System.Cons ... riteLine(x)')
            Instance Receiver: 
            null
            Arguments(1):
                IArgument (ArgumentKind.Explicit, Matching Parameter: value) (OperationKind.Argument, Language: C#) (Syntax: 'x')
                  ILocalReferenceExpression: x (OperationKind.LocalReferenceExpression, Type: System.Object, Language: C#) (Syntax: 'x')
                  InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                  OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
  NextVariables(0)
";
            VerifyOperationTreeForTest<ForEachStatementSyntax>(source, expectedOperationTree);
        }

        [CompilerTrait(CompilerFeature.IOperation)]
        [Fact, WorkItem(17602, "https://github.com/dotnet/roslyn/issues/17602")]
        public void IForEachLoopStatement_CastArrayToIEnumerable()
        {
            string source = @"
using System.Collections;

class C
{
    static void Main(string[] args)
    {
        /*<bind>*/foreach (string x in (IEnumerable)args) { }/*</bind>*/
    }
}
";
            string expectedOperationTree = @"
IForEachLoopStatement (LoopKind.ForEach) (OperationKind.LoopStatement, Language: C#) (Syntax: 'foreach (st ... e)args) { }')
  Locals: Local_1: System.String x
  LoopControlVariable: 
    ILocalReferenceExpression: x (IsDeclaration: True) (OperationKind.LocalReferenceExpression, Type: System.String, Constant: null, Language: C#) (Syntax: 'foreach (st ... e)args) { }')
  Collection: 
    IConversionExpression (Implicit, TryCast: False, Unchecked) (OperationKind.ConversionExpression, Type: System.Collections.IEnumerable, IsImplicit, Language: C#) (Syntax: '(IEnumerable)args')
      Conversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
      Operand: 
        IConversionExpression (Explicit, TryCast: False, Unchecked) (OperationKind.ConversionExpression, Type: System.Collections.IEnumerable, Language: C#) (Syntax: '(IEnumerable)args')
          Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: True, IsUserDefined: False) (MethodSymbol: null)
          Operand: 
            IParameterReferenceExpression: args (OperationKind.ParameterReferenceExpression, Type: System.String[], Language: C#) (Syntax: 'args')
  Body: 
    IBlockStatement (0 statements) (OperationKind.BlockStatement, Language: C#) (Syntax: '{ }')
  NextVariables(0)
";

            VerifyOperationTreeForTest<ForEachStatementSyntax>(source, expectedOperationTree);
        }

        [CompilerTrait(CompilerFeature.IOperation)]
        [Fact, WorkItem(17602, "https://github.com/dotnet/roslyn/issues/17602")]
        public void IForEachLoopStatement_WithThrow()
        {
            string source = @"
class Program
{
    static void Main()
    {
        int[] numbers = { 1, 2, 3, 4 };

        /*<bind>*/foreach (int num in numbers)
        {
            if (num > 3)
            {
                throw new System.Exception(""testing"");
            }
            System.Console.WriteLine(num);
        }/*</bind>*/
    }
}
";
            string expectedOperationTree = @"
IForEachLoopStatement (LoopKind.ForEach) (OperationKind.LoopStatement, Language: C#) (Syntax: 'foreach (in ... }')
  Locals: Local_1: System.Int32 num
  LoopControlVariable: 
    ILocalReferenceExpression: num (IsDeclaration: True) (OperationKind.LocalReferenceExpression, Type: System.Int32, Constant: null, Language: C#) (Syntax: 'foreach (in ... }')
  Collection: 
    IConversionExpression (Implicit, TryCast: False, Unchecked) (OperationKind.ConversionExpression, Type: System.Collections.IEnumerable, IsImplicit, Language: C#) (Syntax: 'numbers')
      Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: True, IsUserDefined: False) (MethodSymbol: null)
      Operand: 
        ILocalReferenceExpression: numbers (OperationKind.LocalReferenceExpression, Type: System.Int32[], Language: C#) (Syntax: 'numbers')
  Body: 
    IBlockStatement (2 statements) (OperationKind.BlockStatement, Language: C#) (Syntax: '{ ... }')
      IIfStatement (OperationKind.IfStatement, Language: C#) (Syntax: 'if (num > 3 ... }')
        Condition: 
          IBinaryOperatorExpression (BinaryOperatorKind.GreaterThan) (OperationKind.BinaryOperatorExpression, Type: System.Boolean, Language: C#) (Syntax: 'num > 3')
            Left: 
              ILocalReferenceExpression: num (OperationKind.LocalReferenceExpression, Type: System.Int32, Language: C#) (Syntax: 'num')
            Right: 
              ILiteralExpression (OperationKind.LiteralExpression, Type: System.Int32, Constant: 3, Language: C#) (Syntax: '3')
        IfTrue: 
          IBlockStatement (1 statements) (OperationKind.BlockStatement, Language: C#) (Syntax: '{ ... }')
            IExpressionStatement (OperationKind.ExpressionStatement, Language: C#) (Syntax: 'throw new S ... ""testing"");')
              Expression: 
                IThrowExpression (OperationKind.ThrowExpression, Type: System.Exception, Language: C#) (Syntax: 'throw new S ... ""testing"");')
                  IObjectCreationExpression (Constructor: System.Exception..ctor(System.String message)) (OperationKind.ObjectCreationExpression, Type: System.Exception, Language: C#) (Syntax: 'new System. ... (""testing"")')
                    Arguments(1):
                        IArgument (ArgumentKind.Explicit, Matching Parameter: message) (OperationKind.Argument, Language: C#) (Syntax: '""testing""')
                          ILiteralExpression (OperationKind.LiteralExpression, Type: System.String, Constant: ""testing"", Language: C#) (Syntax: '""testing""')
                          InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                          OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                    Initializer: 
                    null
        IfFalse: 
        null
      IExpressionStatement (OperationKind.ExpressionStatement, Language: C#) (Syntax: 'System.Cons ... eLine(num);')
        Expression: 
          IInvocationExpression (void System.Console.WriteLine(System.Int32 value)) (OperationKind.InvocationExpression, Type: System.Void, Language: C#) (Syntax: 'System.Cons ... teLine(num)')
            Instance Receiver: 
            null
            Arguments(1):
                IArgument (ArgumentKind.Explicit, Matching Parameter: value) (OperationKind.Argument, Language: C#) (Syntax: 'num')
                  ILocalReferenceExpression: num (OperationKind.LocalReferenceExpression, Type: System.Int32, Language: C#) (Syntax: 'num')
                  InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                  OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
  NextVariables(0)
";
            VerifyOperationTreeForTest<ForEachStatementSyntax>(source, expectedOperationTree);
        }

        [CompilerTrait(CompilerFeature.IOperation)]
        [Fact, WorkItem(17602, "https://github.com/dotnet/roslyn/issues/17602")]
        public void IForEachLoopStatement_WithDeconstructDeclaration()
        {
            string source = @"
class X
{
    public static void M((int, int)[] x)
    {
        /*<bind>*/foreach (var (a, b) in x)
        {
        }/*</bind>*/
    }
}
";
            string expectedOperationTree = @"
IForEachLoopStatement (LoopKind.ForEach) (OperationKind.LoopStatement, Language: C#) (Syntax: 'foreach (va ... }')
  Locals: Local_1: System.Int32 a
    Local_2: System.Int32 b
  LoopControlVariable: 
    IOperation:  (OperationKind.None, Language: C#) (Syntax: 'var (a, b)')
  Collection: 
    IConversionExpression (Implicit, TryCast: False, Unchecked) (OperationKind.ConversionExpression, Type: System.Collections.IEnumerable, IsImplicit, Language: C#) (Syntax: 'x')
      Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: True, IsUserDefined: False) (MethodSymbol: null)
      Operand: 
        IParameterReferenceExpression: x (OperationKind.ParameterReferenceExpression, Type: (System.Int32, System.Int32)[], Language: C#) (Syntax: 'x')
  Body: 
    IBlockStatement (0 statements) (OperationKind.BlockStatement, Language: C#) (Syntax: '{ ... }')
  NextVariables(0)
";
            var expectedDiagnostics = DiagnosticDescription.None;

            VerifyOperationTreeAndDiagnosticsForTest<ForEachVariableStatementSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation)]
        [Fact, WorkItem(17602, "https://github.com/dotnet/roslyn/issues/17602")]
        public void IForEachLoopStatement_WithNestedDeconstructDeclaration()
        {
            string source = @"
class X
{
    public static void M((int, (int, int))[] x)
    {
        /*<bind>*/foreach (var (a, (b, c)) in x)
        {
        }/*</bind>*/
    }
}
";
            string expectedOperationTree = @"
IForEachLoopStatement (LoopKind.ForEach) (OperationKind.LoopStatement, Language: C#) (Syntax: 'foreach (va ... }')
  Locals: Local_1: System.Int32 a
    Local_2: System.Int32 b
    Local_3: System.Int32 c
  LoopControlVariable: 
    IOperation:  (OperationKind.None, Language: C#) (Syntax: 'var (a, (b, c))')
  Collection: 
    IConversionExpression (Implicit, TryCast: False, Unchecked) (OperationKind.ConversionExpression, Type: System.Collections.IEnumerable, IsImplicit, Language: C#) (Syntax: 'x')
      Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: True, IsUserDefined: False) (MethodSymbol: null)
      Operand: 
        IParameterReferenceExpression: x (OperationKind.ParameterReferenceExpression, Type: (System.Int32, (System.Int32, System.Int32))[], Language: C#) (Syntax: 'x')
  Body: 
    IBlockStatement (0 statements) (OperationKind.BlockStatement, Language: C#) (Syntax: '{ ... }')
  NextVariables(0)
";
            var expectedDiagnostics = DiagnosticDescription.None;

            VerifyOperationTreeAndDiagnosticsForTest<ForEachVariableStatementSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation)]
        [Fact, WorkItem(17602, "https://github.com/dotnet/roslyn/issues/17602")]
        public void IForEachLoopStatement_WithInvalidLoopControlVariable()
        {
            string source = @"
class X
{
    public static void M((int, int)[] x)
    /*<bind>*/{
        foreach (i, j in x)
        {
        }
    }/*</bind>*/
}
";
            string expectedOperationTree = @"
IBlockStatement (4 statements) (OperationKind.BlockStatement, IsInvalid, Language: C#) (Syntax: '{ ... }')
  IForEachLoopStatement (LoopKind.ForEach) (OperationKind.LoopStatement, IsInvalid, Language: C#) (Syntax: 'foreach (i')
    LoopControlVariable: 
    null
    Collection: 
      IInvalidExpression (OperationKind.InvalidExpression, Type: null, IsInvalid, Language: C#) (Syntax: '')
        Children(0)
    Body: 
      IExpressionStatement (OperationKind.ExpressionStatement, IsInvalid, Language: C#) (Syntax: '')
        Expression: 
          IInvalidExpression (OperationKind.InvalidExpression, Type: null, IsInvalid, Language: C#) (Syntax: '')
            Children(0)
    NextVariables(0)
  IExpressionStatement (OperationKind.ExpressionStatement, IsInvalid, Language: C#) (Syntax: 'j ')
    Expression: 
      IInvalidExpression (OperationKind.InvalidExpression, Type: ?, IsInvalid, Language: C#) (Syntax: 'j')
        Children(0)
  IExpressionStatement (OperationKind.ExpressionStatement, IsInvalid, Language: C#) (Syntax: 'x')
    Expression: 
      IParameterReferenceExpression: x (OperationKind.ParameterReferenceExpression, Type: (System.Int32, System.Int32)[], IsInvalid, Language: C#) (Syntax: 'x')
  IBlockStatement (0 statements) (OperationKind.BlockStatement, Language: C#) (Syntax: '{ ... }')
";
            var expectedDiagnostics = new DiagnosticDescription[] {
                // CS1515: 'in' expected
                //         foreach (i, j in x)
                Diagnostic(ErrorCode.ERR_InExpected, ",").WithLocation(6, 19),
                // CS0230: Type and identifier are both required in a foreach statement
                //         foreach (i, j in x)
                Diagnostic(ErrorCode.ERR_BadForeachDecl, ",").WithLocation(6, 19),
                // CS1525: Invalid expression term ','
                //         foreach (i, j in x)
                Diagnostic(ErrorCode.ERR_InvalidExprTerm, ",").WithArguments(",").WithLocation(6, 19),
                // CS1026: ) expected
                //         foreach (i, j in x)
                Diagnostic(ErrorCode.ERR_CloseParenExpected, ",").WithLocation(6, 19),
                // CS1525: Invalid expression term ','
                //         foreach (i, j in x)
                Diagnostic(ErrorCode.ERR_InvalidExprTerm, ",").WithArguments(",").WithLocation(6, 19),
                // CS1002: ; expected
                //         foreach (i, j in x)
                Diagnostic(ErrorCode.ERR_SemicolonExpected, ",").WithLocation(6, 19),
                // CS1513: } expected
                //         foreach (i, j in x)
                Diagnostic(ErrorCode.ERR_RbraceExpected, ",").WithLocation(6, 19),
                // CS1002: ; expected
                //         foreach (i, j in x)
                Diagnostic(ErrorCode.ERR_SemicolonExpected, "in").WithLocation(6, 23),
                // CS1513: } expected
                //         foreach (i, j in x)
                Diagnostic(ErrorCode.ERR_RbraceExpected, "in").WithLocation(6, 23),
                // CS1002: ; expected
                //         foreach (i, j in x)
                Diagnostic(ErrorCode.ERR_SemicolonExpected, ")").WithLocation(6, 27),
                // CS1513: } expected
                //         foreach (i, j in x)
                Diagnostic(ErrorCode.ERR_RbraceExpected, ")").WithLocation(6, 27),
                // CS0103: The name 'j' does not exist in the current context
                //         foreach (i, j in x)
                Diagnostic(ErrorCode.ERR_NameNotInContext, "j").WithArguments("j").WithLocation(6, 21)
            };

            VerifyOperationTreeAndDiagnosticsForTest<BlockSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation)]
        [Fact, WorkItem(17602, "https://github.com/dotnet/roslyn/issues/17602")]
        public void IForEachLoopStatement_WithInvalidLoopControlVariable_02()
        {
            string source = @"
class X
{
    public static void M(int[] x)
    /*<bind>*/{
        foreach (x[0] in x)
        {
        }
    }/*</bind>*/
}
";
            string expectedOperationTree = @"
IBlockStatement (1 statements) (OperationKind.BlockStatement, IsInvalid, Language: C#) (Syntax: '{ ... }')
  IForEachLoopStatement (LoopKind.ForEach) (OperationKind.LoopStatement, IsInvalid, Language: C#) (Syntax: 'foreach (x[ ... }')
    LoopControlVariable: 
    null
    Collection: 
      IConversionExpression (Implicit, TryCast: False, Unchecked) (OperationKind.ConversionExpression, Type: System.Collections.IEnumerable, IsImplicit, Language: C#) (Syntax: 'x')
        Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: True, IsUserDefined: False) (MethodSymbol: null)
        Operand: 
          IParameterReferenceExpression: x (OperationKind.ParameterReferenceExpression, Type: System.Int32[], Language: C#) (Syntax: 'x')
    Body: 
      IBlockStatement (0 statements) (OperationKind.BlockStatement, Language: C#) (Syntax: '{ ... }')
    NextVariables(0)
";
            var expectedDiagnostics = new DiagnosticDescription[] {
                Diagnostic(ErrorCode.ERR_BadForeachDecl, "in").WithLocation(6, 23)
            };

            VerifyOperationTreeAndDiagnosticsForTest<BlockSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }
    }
}
