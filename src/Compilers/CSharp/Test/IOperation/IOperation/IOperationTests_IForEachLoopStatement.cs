// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.CSharp.Test.Utilities;
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
IForEachLoopOperation (LoopKind.ForEach, Continue Label Id: 0, Exit Label Id: 1) (OperationKind.Loop, Type: null) (Syntax: 'foreach (st ... }')
  Locals: Local_1: System.String value
  LoopControlVariable: 
    IVariableDeclaratorOperation (Symbol: System.String value) (OperationKind.VariableDeclarator, Type: null) (Syntax: 'string')
      Initializer: 
        null
  Collection: 
    IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.Collections.IEnumerable, IsImplicit) (Syntax: 'pets')
      Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: True, IsUserDefined: False) (MethodSymbol: null)
      Operand: 
        ILocalReferenceOperation: pets (OperationKind.LocalReference, Type: System.String[]) (Syntax: 'pets')
  Body: 
    IBlockOperation (1 statements) (OperationKind.Block, Type: null) (Syntax: '{ ... }')
      IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'System.Cons ... ine(value);')
        Expression: 
          IInvocationOperation (void System.Console.WriteLine(System.String value)) (OperationKind.Invocation, Type: System.Void) (Syntax: 'System.Cons ... Line(value)')
            Instance Receiver: 
              null
            Arguments(1):
                IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: value) (OperationKind.Argument, Type: null) (Syntax: 'value')
                  ILocalReferenceOperation: value (OperationKind.LocalReference, Type: System.String) (Syntax: 'value')
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
IForEachLoopOperation (LoopKind.ForEach, Continue Label Id: 0, Exit Label Id: 1) (OperationKind.Loop, Type: null) (Syntax: 'foreach (st ... }')
  Locals: Local_1: System.String item
  LoopControlVariable: 
    IVariableDeclaratorOperation (Symbol: System.String item) (OperationKind.VariableDeclarator, Type: null) (Syntax: 'string')
      Initializer: 
        null
  Collection: 
    IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.Collections.Generic.List<System.String>, IsImplicit) (Syntax: 'list')
      Conversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
      Operand: 
        ILocalReferenceOperation: list (OperationKind.LocalReference, Type: System.Collections.Generic.List<System.String>) (Syntax: 'list')
  Body: 
    IBlockOperation (1 statements) (OperationKind.Block, Type: null) (Syntax: '{ ... }')
      IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'Console.WriteLine(item);')
        Expression: 
          IInvocationOperation (void System.Console.WriteLine(System.String value)) (OperationKind.Invocation, Type: System.Void) (Syntax: 'Console.WriteLine(item)')
            Instance Receiver: 
              null
            Arguments(1):
                IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: value) (OperationKind.Argument, Type: null) (Syntax: 'item')
                  ILocalReferenceOperation: item (OperationKind.LocalReference, Type: System.String) (Syntax: 'item')
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
IForEachLoopOperation (LoopKind.ForEach, Continue Label Id: 0, Exit Label Id: 1) (OperationKind.Loop, Type: null) (Syntax: 'foreach (Ke ... }')
  Locals: Local_1: System.Collections.Generic.KeyValuePair<System.Int32, System.Int32> pair
  LoopControlVariable: 
    IVariableDeclaratorOperation (Symbol: System.Collections.Generic.KeyValuePair<System.Int32, System.Int32> pair) (OperationKind.VariableDeclarator, Type: null) (Syntax: 'KeyValuePair<int, int>')
      Initializer: 
        null
  Collection: 
    IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.Collections.Generic.Dictionary<System.Int32, System.Int32>, IsImplicit) (Syntax: '_h')
      Conversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
      Operand: 
        IFieldReferenceOperation: System.Collections.Generic.Dictionary<System.Int32, System.Int32> Program._h (Static) (OperationKind.FieldReference, Type: System.Collections.Generic.Dictionary<System.Int32, System.Int32>) (Syntax: '_h')
          Instance Receiver: 
            null
  Body: 
    IBlockOperation (1 statements) (OperationKind.Block, Type: null) (Syntax: '{ ... }')
      IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'Console.Wri ... air.Value);')
        Expression: 
          IInvocationOperation (void System.Console.WriteLine(System.String format, System.Object arg0, System.Object arg1)) (OperationKind.Invocation, Type: System.Void) (Syntax: 'Console.Wri ... pair.Value)')
            Instance Receiver: 
              null
            Arguments(3):
                IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: format) (OperationKind.Argument, Type: null) (Syntax: '""{0},{1}""')
                  ILiteralOperation (OperationKind.Literal, Type: System.String, Constant: ""{0},{1}"") (Syntax: '""{0},{1}""')
                  InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                  OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: arg0) (OperationKind.Argument, Type: null) (Syntax: 'pair.Key')
                  IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.Object, IsImplicit) (Syntax: 'pair.Key')
                    Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                    Operand: 
                      IPropertyReferenceOperation: System.Int32 System.Collections.Generic.KeyValuePair<System.Int32, System.Int32>.Key { get; } (OperationKind.PropertyReference, Type: System.Int32) (Syntax: 'pair.Key')
                        Instance Receiver: 
                          ILocalReferenceOperation: pair (OperationKind.LocalReference, Type: System.Collections.Generic.KeyValuePair<System.Int32, System.Int32>) (Syntax: 'pair')
                  InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                  OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: arg1) (OperationKind.Argument, Type: null) (Syntax: 'pair.Value')
                  IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.Object, IsImplicit) (Syntax: 'pair.Value')
                    Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                    Operand: 
                      IPropertyReferenceOperation: System.Int32 System.Collections.Generic.KeyValuePair<System.Int32, System.Int32>.Value { get; } (OperationKind.PropertyReference, Type: System.Int32) (Syntax: 'pair.Value')
                        Instance Receiver: 
                          ILocalReferenceOperation: pair (OperationKind.LocalReference, Type: System.Collections.Generic.KeyValuePair<System.Int32, System.Int32>) (Syntax: 'pair')
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
IForEachLoopOperation (LoopKind.ForEach, Continue Label Id: 0, Exit Label Id: 1) (OperationKind.Loop, Type: null) (Syntax: 'foreach (in ... }')
  Locals: Local_1: System.Int32 num
  LoopControlVariable: 
    IVariableDeclaratorOperation (Symbol: System.Int32 num) (OperationKind.VariableDeclarator, Type: null) (Syntax: 'int')
      Initializer: 
        null
  Collection: 
    IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.Collections.IEnumerable, IsImplicit) (Syntax: 'numbers')
      Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: True, IsUserDefined: False) (MethodSymbol: null)
      Operand: 
        ILocalReferenceOperation: numbers (OperationKind.LocalReference, Type: System.Int32[]) (Syntax: 'numbers')
  Body: 
    IBlockOperation (2 statements) (OperationKind.Block, Type: null) (Syntax: '{ ... }')
      IConditionalOperation (OperationKind.Conditional, Type: null) (Syntax: 'if (num>3) ... }')
        Condition: 
          IBinaryOperation (BinaryOperatorKind.GreaterThan) (OperationKind.Binary, Type: System.Boolean) (Syntax: 'num>3')
            Left: 
              ILocalReferenceOperation: num (OperationKind.LocalReference, Type: System.Int32) (Syntax: 'num')
            Right: 
              ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 3) (Syntax: '3')
        WhenTrue: 
          IBlockOperation (1 statements) (OperationKind.Block, Type: null) (Syntax: '{ ... }')
            IBranchOperation (BranchKind.Break, Label Id: 1) (OperationKind.Branch, Type: null) (Syntax: 'break;')
        WhenFalse: 
          null
      IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'System.Cons ... eLine(num);')
        Expression: 
          IInvocationOperation (void System.Console.WriteLine(System.Int32 value)) (OperationKind.Invocation, Type: System.Void) (Syntax: 'System.Cons ... teLine(num)')
            Instance Receiver: 
              null
            Arguments(1):
                IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: value) (OperationKind.Argument, Type: null) (Syntax: 'num')
                  ILocalReferenceOperation: num (OperationKind.LocalReference, Type: System.Int32) (Syntax: 'num')
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
IForEachLoopOperation (LoopKind.ForEach, Continue Label Id: 0, Exit Label Id: 1) (OperationKind.Loop, Type: null) (Syntax: 'foreach (in ... }')
  Locals: Local_1: System.Int32 num
  LoopControlVariable: 
    IVariableDeclaratorOperation (Symbol: System.Int32 num) (OperationKind.VariableDeclarator, Type: null) (Syntax: 'int')
      Initializer: 
        null
  Collection: 
    IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.Collections.IEnumerable, IsImplicit) (Syntax: 'numbers')
      Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: True, IsUserDefined: False) (MethodSymbol: null)
      Operand: 
        ILocalReferenceOperation: numbers (OperationKind.LocalReference, Type: System.Int32[]) (Syntax: 'numbers')
  Body: 
    IBlockOperation (2 statements) (OperationKind.Block, Type: null) (Syntax: '{ ... }')
      IConditionalOperation (OperationKind.Conditional, Type: null) (Syntax: 'if (num>3) ... }')
        Condition: 
          IBinaryOperation (BinaryOperatorKind.GreaterThan) (OperationKind.Binary, Type: System.Boolean) (Syntax: 'num>3')
            Left: 
              ILocalReferenceOperation: num (OperationKind.LocalReference, Type: System.Int32) (Syntax: 'num')
            Right: 
              ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 3) (Syntax: '3')
        WhenTrue: 
          IBlockOperation (1 statements) (OperationKind.Block, Type: null) (Syntax: '{ ... }')
            IBranchOperation (BranchKind.Continue, Label Id: 0) (OperationKind.Branch, Type: null) (Syntax: 'continue;')
        WhenFalse: 
          null
      IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'System.Cons ... eLine(num);')
        Expression: 
          IInvocationOperation (void System.Console.WriteLine(System.Int32 value)) (OperationKind.Invocation, Type: System.Void) (Syntax: 'System.Cons ... teLine(num)')
            Instance Receiver: 
              null
            Arguments(1):
                IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: value) (OperationKind.Argument, Type: null) (Syntax: 'num')
                  ILocalReferenceOperation: num (OperationKind.LocalReference, Type: System.Int32) (Syntax: 'num')
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
IForEachLoopOperation (LoopKind.ForEach, Continue Label Id: 0, Exit Label Id: 1) (OperationKind.Loop, Type: null) (Syntax: 'foreach (st ... }')
  Locals: Local_1: System.String value
  LoopControlVariable: 
    IVariableDeclaratorOperation (Symbol: System.String value) (OperationKind.VariableDeclarator, Type: null) (Syntax: 'string')
      Initializer: 
        null
  Collection: 
    ILocalReferenceOperation: sorted (OperationKind.LocalReference, Type: ?) (Syntax: 'sorted')
  Body: 
    IBlockOperation (1 statements) (OperationKind.Block, Type: null) (Syntax: '{ ... }')
      IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'System.Cons ... ine(value);')
        Expression: 
          IInvocationOperation (void System.Console.WriteLine(System.String value)) (OperationKind.Invocation, Type: System.Void) (Syntax: 'System.Cons ... Line(value)')
            Instance Receiver: 
              null
            Arguments(1):
                IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: value) (OperationKind.Argument, Type: null) (Syntax: 'value')
                  ILocalReferenceOperation: value (OperationKind.LocalReference, Type: System.String) (Syntax: 'value')
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
IForEachLoopOperation (LoopKind.ForEach, Continue Label Id: 0, Exit Label Id: 1) (OperationKind.Loop, Type: null) (Syntax: 'foreach (Fi ... }')
  Locals: Local_1: System.Reflection.FieldInfo fi
  LoopControlVariable: 
    IVariableDeclaratorOperation (Symbol: System.Reflection.FieldInfo fi) (OperationKind.VariableDeclarator, Type: null) (Syntax: 'FieldInfo')
      Initializer: 
        null
  Collection: 
    IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.Collections.IEnumerable, IsImplicit) (Syntax: 'employee.Ge ... GetFields()')
      Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: True, IsUserDefined: False) (MethodSymbol: null)
      Operand: 
        IInvocationOperation ( System.Reflection.FieldInfo[] System.Type.GetFields()) (OperationKind.Invocation, Type: System.Reflection.FieldInfo[]) (Syntax: 'employee.Ge ... GetFields()')
          Instance Receiver: 
            IInvocationOperation ( System.Type System.Object.GetType()) (OperationKind.Invocation, Type: System.Type) (Syntax: 'employee.GetType()')
              Instance Receiver: 
                ILocalReferenceOperation: employee (OperationKind.LocalReference, Type: DisplayStructContentsTest.Program.Employee) (Syntax: 'employee')
              Arguments(0)
          Arguments(0)
  Body: 
    IBlockOperation (1 statements) (OperationKind.Block, Type: null) (Syntax: '{ ... }')
      IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'System.Cons ... mployee)));')
        Expression: 
          IInvocationOperation (void System.Console.WriteLine(System.String value)) (OperationKind.Invocation, Type: System.Void) (Syntax: 'System.Cons ... employee)))')
            Instance Receiver: 
              null
            Arguments(1):
                IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: value) (OperationKind.Argument, Type: null) (Syntax: 'fi.Name + "" ... (employee))')
                  IBinaryOperation (BinaryOperatorKind.Add) (OperationKind.Binary, Type: System.String) (Syntax: 'fi.Name + "" ... (employee))')
                    Left: 
                      IBinaryOperation (BinaryOperatorKind.Add) (OperationKind.Binary, Type: System.String) (Syntax: 'fi.Name + "" = ""')
                        Left: 
                          IPropertyReferenceOperation: System.String System.Reflection.MemberInfo.Name { get; } (OperationKind.PropertyReference, Type: System.String) (Syntax: 'fi.Name')
                            Instance Receiver: 
                              ILocalReferenceOperation: fi (OperationKind.LocalReference, Type: System.Reflection.FieldInfo) (Syntax: 'fi')
                        Right: 
                          ILiteralOperation (OperationKind.Literal, Type: System.String, Constant: "" = "") (Syntax: '"" = ""')
                    Right: 
                      IInvocationOperation (System.String System.Convert.ToString(System.Object value)) (OperationKind.Invocation, Type: System.String) (Syntax: 'System.Conv ... (employee))')
                        Instance Receiver: 
                          null
                        Arguments(1):
                            IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: value) (OperationKind.Argument, Type: null) (Syntax: 'fi.GetValue(employee)')
                              IInvocationOperation (virtual System.Object System.Reflection.FieldInfo.GetValue(System.Object obj)) (OperationKind.Invocation, Type: System.Object) (Syntax: 'fi.GetValue(employee)')
                                Instance Receiver: 
                                  ILocalReferenceOperation: fi (OperationKind.LocalReference, Type: System.Reflection.FieldInfo) (Syntax: 'fi')
                                Arguments(1):
                                    IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: obj) (OperationKind.Argument, Type: null) (Syntax: 'employee')
                                      IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.Object, IsImplicit) (Syntax: 'employee')
                                        Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                                        Operand: 
                                          ILocalReferenceOperation: employee (OperationKind.LocalReference, Type: DisplayStructContentsTest.Program.Employee) (Syntax: 'employee')
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
IForEachLoopOperation (LoopKind.ForEach, Continue Label Id: 0, Exit Label Id: 1) (OperationKind.Loop, Type: null) (Syntax: 'foreach (ch ... }')
  Locals: Local_1: System.Char c
  LoopControlVariable: 
    IVariableDeclaratorOperation (Symbol: System.Char c) (OperationKind.VariableDeclarator, Type: null) (Syntax: 'char')
      Initializer: 
        null
  Collection: 
    IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.String, IsImplicit) (Syntax: 's')
      Conversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
      Operand: 
        ILocalReferenceOperation: s (OperationKind.LocalReference, Type: System.String, Constant: """") (Syntax: 's')
  Body: 
    IBlockOperation (1 statements) (OperationKind.Block, Type: null) (Syntax: '{ ... }')
      IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'System.Cons ... iteLine(c);')
        Expression: 
          IInvocationOperation (void System.Console.WriteLine(System.Char value)) (OperationKind.Invocation, Type: System.Void) (Syntax: 'System.Cons ... riteLine(c)')
            Instance Receiver: 
              null
            Arguments(1):
                IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: value) (OperationKind.Argument, Type: null) (Syntax: 'c')
                  ILocalReferenceOperation: c (OperationKind.LocalReference, Type: System.Char) (Syntax: 'c')
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
IForEachLoopOperation (LoopKind.ForEach, Continue Label Id: 0, Exit Label Id: 1) (OperationKind.Loop, Type: null) (Syntax: 'foreach (va ... }')
  Locals: Local_1: System.Collections.Generic.KeyValuePair<System.Int32, System.Int32> pair
  LoopControlVariable: 
    IVariableDeclaratorOperation (Symbol: System.Collections.Generic.KeyValuePair<System.Int32, System.Int32> pair) (OperationKind.VariableDeclarator, Type: null) (Syntax: 'var')
      Initializer: 
        null
  Collection: 
    IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.Collections.Generic.Dictionary<System.Int32, System.Int32>, IsImplicit) (Syntax: '_f')
      Conversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
      Operand: 
        IFieldReferenceOperation: System.Collections.Generic.Dictionary<System.Int32, System.Int32> Program._f (Static) (OperationKind.FieldReference, Type: System.Collections.Generic.Dictionary<System.Int32, System.Int32>) (Syntax: '_f')
          Instance Receiver: 
            null
  Body: 
    IBlockOperation (1 statements) (OperationKind.Block, Type: null) (Syntax: '{ ... }')
      IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'System.Cons ... air.Value);')
        Expression: 
          IInvocationOperation (void System.Console.WriteLine(System.String format, System.Object arg0, System.Object arg1)) (OperationKind.Invocation, Type: System.Void) (Syntax: 'System.Cons ... pair.Value)')
            Instance Receiver: 
              null
            Arguments(3):
                IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: format) (OperationKind.Argument, Type: null) (Syntax: '""{0},{1}""')
                  ILiteralOperation (OperationKind.Literal, Type: System.String, Constant: ""{0},{1}"") (Syntax: '""{0},{1}""')
                  InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                  OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: arg0) (OperationKind.Argument, Type: null) (Syntax: 'pair.Key')
                  IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.Object, IsImplicit) (Syntax: 'pair.Key')
                    Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                    Operand: 
                      IPropertyReferenceOperation: System.Int32 System.Collections.Generic.KeyValuePair<System.Int32, System.Int32>.Key { get; } (OperationKind.PropertyReference, Type: System.Int32) (Syntax: 'pair.Key')
                        Instance Receiver: 
                          ILocalReferenceOperation: pair (OperationKind.LocalReference, Type: System.Collections.Generic.KeyValuePair<System.Int32, System.Int32>) (Syntax: 'pair')
                  InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                  OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: arg1) (OperationKind.Argument, Type: null) (Syntax: 'pair.Value')
                  IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.Object, IsImplicit) (Syntax: 'pair.Value')
                    Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                    Operand: 
                      IPropertyReferenceOperation: System.Int32 System.Collections.Generic.KeyValuePair<System.Int32, System.Int32>.Value { get; } (OperationKind.PropertyReference, Type: System.Int32) (Syntax: 'pair.Value')
                        Instance Receiver: 
                          ILocalReferenceOperation: pair (OperationKind.LocalReference, Type: System.Collections.Generic.KeyValuePair<System.Int32, System.Int32>) (Syntax: 'pair')
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
IForEachLoopOperation (LoopKind.ForEach, Continue Label Id: 0, Exit Label Id: 1) (OperationKind.Loop, Type: null, IsInvalid) (Syntax: 'foreach (Mi ... }')
  Locals: Local_1: MissingType x
  LoopControlVariable: 
    IVariableDeclaratorOperation (Symbol: MissingType x) (OperationKind.VariableDeclarator, Type: null, IsInvalid) (Syntax: 'MissingType')
      Initializer: 
        null
  Collection: 
    ILocalReferenceOperation: sequence (OperationKind.LocalReference, Type: System.Collections.IEnumerable) (Syntax: 'sequence')
  Body: 
    IBlockOperation (1 statements, 1 locals) (OperationKind.Block, Type: null) (Syntax: '{ ... }')
      Locals: Local_1: System.Boolean b
      IVariableDeclarationGroupOperation (1 declarations) (OperationKind.VariableDeclarationGroup, Type: null) (Syntax: 'bool b = !x ... uals(null);')
        IVariableDeclarationOperation (1 declarators) (OperationKind.VariableDeclaration, Type: null) (Syntax: 'bool b = !x.Equals(null)')
          Declarators:
              IVariableDeclaratorOperation (Symbol: System.Boolean b) (OperationKind.VariableDeclarator, Type: null) (Syntax: 'b = !x.Equals(null)')
                Initializer: 
                  IVariableInitializerOperation (OperationKind.VariableInitializer, Type: null) (Syntax: '= !x.Equals(null)')
                    IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.Boolean, IsImplicit) (Syntax: '!x.Equals(null)')
                      Conversion: CommonConversion (Exists: False, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                      Operand: 
                        IUnaryOperation (UnaryOperatorKind.Not) (OperationKind.Unary, Type: ?) (Syntax: '!x.Equals(null)')
                          Operand: 
                            IInvalidOperation (OperationKind.Invalid, Type: ?) (Syntax: 'x.Equals(null)')
                              Children(2):
                                  IOperation:  (OperationKind.None, Type: null) (Syntax: 'x.Equals')
                                    Children(1):
                                        ILocalReferenceOperation: x (OperationKind.LocalReference, Type: MissingType) (Syntax: 'x')
                                  ILiteralOperation (OperationKind.Literal, Type: null, Constant: null) (Syntax: 'null')
          Initializer: 
            null
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
IForEachLoopOperation (LoopKind.ForEach, Continue Label Id: 0, Exit Label Id: 1) (OperationKind.Loop, Type: null, IsInvalid) (Syntax: 'foreach (in ... }')
  Locals: Local_1: System.Int32 x
  LoopControlVariable: 
    IVariableDeclaratorOperation (Symbol: System.Int32 x) (OperationKind.VariableDeclarator, Type: null) (Syntax: 'int')
      Initializer: 
        null
  Collection: 
    ILiteralOperation (OperationKind.Literal, Type: null, Constant: null, IsInvalid) (Syntax: 'null')
  Body: 
    IBlockOperation (0 statements) (OperationKind.Block, Type: null) (Syntax: '{ ... }')
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
IForEachLoopOperation (LoopKind.ForEach, Continue Label Id: 0, Exit Label Id: 1) (OperationKind.Loop, Type: null, IsInvalid) (Syntax: 'foreach (in ... }')
  Locals: Local_1: System.Int32 x
  LoopControlVariable: 
    IVariableDeclaratorOperation (Symbol: System.Int32 x) (OperationKind.VariableDeclarator, Type: null) (Syntax: 'int')
      Initializer: 
        null
  Collection: 
    IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.Collections.IEnumerable, IsImplicit) (Syntax: 'args')
      Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: True, IsUserDefined: False) (MethodSymbol: null)
      Operand: 
        IParameterReferenceOperation: args (OperationKind.ParameterReference, Type: System.String[]) (Syntax: 'args')
  Body: 
    IBlockOperation (0 statements) (OperationKind.Block, Type: null) (Syntax: '{ ... }')
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
IForEachLoopOperation (LoopKind.ForEach, Continue Label Id: 0, Exit Label Id: 1) (OperationKind.Loop, Type: null, IsInvalid) (Syntax: 'foreach (in ... a) { x++; }')
  Locals: Local_1: System.Int32 x
  LoopControlVariable: 
    IVariableDeclaratorOperation (Symbol: System.Int32 x) (OperationKind.VariableDeclarator, Type: null) (Syntax: 'int')
      Initializer: 
        null
  Collection: 
    IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.Collections.IEnumerable, IsImplicit) (Syntax: 'a')
      Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: True, IsUserDefined: False) (MethodSymbol: null)
      Operand: 
        IParameterReferenceOperation: a (OperationKind.ParameterReference, Type: System.Int32[]) (Syntax: 'a')
  Body: 
    IBlockOperation (1 statements) (OperationKind.Block, Type: null, IsInvalid) (Syntax: '{ x++; }')
      IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null, IsInvalid) (Syntax: 'x++;')
        Expression: 
          IIncrementOrDecrementOperation (Postfix) (OperationKind.Increment, Type: ?, IsInvalid) (Syntax: 'x++')
            Target: 
              IInvalidOperation (OperationKind.Invalid, Type: System.Int32, IsInvalid, IsImplicit) (Syntax: 'x')
                Children(1):
                    ILocalReferenceOperation: x (OperationKind.LocalReference, Type: System.Int32, IsInvalid) (Syntax: 'x')
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
IForEachLoopOperation (LoopKind.ForEach, Continue Label Id: 0, Exit Label Id: 1) (OperationKind.Loop, Type: null) (Syntax: 'foreach (lo ... x in e) { }')
  Locals: Local_1: System.Int64 x
  LoopControlVariable: 
    IVariableDeclaratorOperation (Symbol: System.Int64 x) (OperationKind.VariableDeclarator, Type: null) (Syntax: 'long')
      Initializer: 
        null
  Collection: 
    IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: Enumerable, IsImplicit) (Syntax: 'e')
      Conversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
      Operand: 
        IParameterReferenceOperation: e (OperationKind.ParameterReference, Type: Enumerable) (Syntax: 'e')
  Body: 
    IBlockOperation (0 statements) (OperationKind.Block, Type: null) (Syntax: '{ }')
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
IForEachLoopOperation (LoopKind.ForEach, Continue Label Id: 0, Exit Label Id: 1) (OperationKind.Loop, Type: null) (Syntax: 'foreach (var x in s) { }')
  Locals: Local_1: System.Char x
  LoopControlVariable: 
    IVariableDeclaratorOperation (Symbol: System.Char x) (OperationKind.VariableDeclarator, Type: null) (Syntax: 'var')
      Initializer: 
        null
  Collection: 
    IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.String, IsImplicit) (Syntax: 's')
      Conversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
      Operand: 
        IParameterReferenceOperation: s (OperationKind.ParameterReference, Type: System.String) (Syntax: 's')
  Body: 
    IBlockOperation (0 statements) (OperationKind.Block, Type: null) (Syntax: '{ }')
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
IForEachLoopOperation (LoopKind.ForEach, Continue Label Id: 0, Exit Label Id: 1) (OperationKind.Loop, Type: null) (Syntax: 'foreach (var x in a) { }')
  Locals: Local_1: C.var x
  LoopControlVariable: 
    IVariableDeclaratorOperation (Symbol: C.var x) (OperationKind.VariableDeclarator, Type: null) (Syntax: 'var')
      Initializer: 
        null
  Collection: 
    IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.Collections.IEnumerable, IsImplicit) (Syntax: 'a')
      Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: True, IsUserDefined: False) (MethodSymbol: null)
      Operand: 
        IParameterReferenceOperation: a (OperationKind.ParameterReference, Type: C.var[]) (Syntax: 'a')
  Body: 
    IBlockOperation (0 statements) (OperationKind.Block, Type: null) (Syntax: '{ }')
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
IForEachLoopOperation (LoopKind.ForEach, Continue Label Id: 0, Exit Label Id: 1) (OperationKind.Loop, Type: null) (Syntax: 'foreach (int x in d) { }')
  Locals: Local_1: System.Int32 x
  LoopControlVariable: 
    IVariableDeclaratorOperation (Symbol: System.Int32 x) (OperationKind.VariableDeclarator, Type: null) (Syntax: 'int')
      Initializer: 
        null
  Collection: 
    IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.Collections.IEnumerable, IsImplicit) (Syntax: 'd')
      Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
      Operand: 
        IParameterReferenceOperation: d (OperationKind.ParameterReference, Type: dynamic) (Syntax: 'd')
  Body: 
    IBlockOperation (0 statements) (OperationKind.Block, Type: null) (Syntax: '{ }')
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
IForEachLoopOperation (LoopKind.ForEach, Continue Label Id: 0, Exit Label Id: 1) (OperationKind.Loop, Type: null) (Syntax: 'foreach (ob ... }')
  Locals: Local_1: System.Object x
  LoopControlVariable: 
    IVariableDeclaratorOperation (Symbol: System.Object x) (OperationKind.VariableDeclarator, Type: null) (Syntax: 'object')
      Initializer: 
        null
  Collection: 
    IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: Enumerable<T>, IsImplicit) (Syntax: 'new Enumerable<T>()')
      Conversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
      Operand: 
        IObjectCreationOperation (Constructor: Enumerable<T>..ctor()) (OperationKind.ObjectCreation, Type: Enumerable<T>) (Syntax: 'new Enumerable<T>()')
          Arguments(0)
          Initializer: 
            null
  Body: 
    IBlockOperation (1 statements) (OperationKind.Block, Type: null) (Syntax: '{ ... }')
      IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'System.Cons ... iteLine(x);')
        Expression: 
          IInvocationOperation (void System.Console.WriteLine(System.Object value)) (OperationKind.Invocation, Type: System.Void) (Syntax: 'System.Cons ... riteLine(x)')
            Instance Receiver: 
              null
            Arguments(1):
                IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: value) (OperationKind.Argument, Type: null) (Syntax: 'x')
                  ILocalReferenceOperation: x (OperationKind.LocalReference, Type: System.Object) (Syntax: 'x')
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
IForEachLoopOperation (LoopKind.ForEach, Continue Label Id: 0, Exit Label Id: 1) (OperationKind.Loop, Type: null) (Syntax: 'foreach (st ... e)args) { }')
  Locals: Local_1: System.String x
  LoopControlVariable: 
    IVariableDeclaratorOperation (Symbol: System.String x) (OperationKind.VariableDeclarator, Type: null) (Syntax: 'string')
      Initializer: 
        null
  Collection: 
    IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.Collections.IEnumerable, IsImplicit) (Syntax: '(IEnumerable)args')
      Conversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
      Operand: 
        IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.Collections.IEnumerable) (Syntax: '(IEnumerable)args')
          Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: True, IsUserDefined: False) (MethodSymbol: null)
          Operand: 
            IParameterReferenceOperation: args (OperationKind.ParameterReference, Type: System.String[]) (Syntax: 'args')
  Body: 
    IBlockOperation (0 statements) (OperationKind.Block, Type: null) (Syntax: '{ }')
  NextVariables(0)
";

            VerifyOperationTreeForTest<ForEachStatementSyntax>(source, expectedOperationTree);
        }

        [CompilerTrait(CompilerFeature.IOperation)]
        [Fact, WorkItem(17602, "https://github.com/dotnet/roslyn/issues/17602")]
        public void IForEachLoopStatement_CastCollectionToIEnumerable()
        {
            string source = @"
using System.Collections.Generic;

class C
{
    static void Main(List<string> args)
    {
        /*<bind>*/foreach (string x in (IEnumerable<string>)args) { }/*</bind>*/
    }
}
";
            // Affected by https://github.com/dotnet/roslyn/issues/20756
            string expectedOperationTree = @"
IForEachLoopOperation (LoopKind.ForEach, Continue Label Id: 0, Exit Label Id: 1) (OperationKind.Loop, Type: null) (Syntax: 'foreach (st ... >)args) { }')
  Locals: Local_1: System.String x
  LoopControlVariable: 
    IVariableDeclaratorOperation (Symbol: System.String x) (OperationKind.VariableDeclarator, Type: null) (Syntax: 'string')
      Initializer: 
        null
  Collection: 
    IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.Collections.Generic.IEnumerable<System.String>, IsImplicit) (Syntax: '(IEnumerabl ... tring>)args')
      Conversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
      Operand: 
        IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.Collections.Generic.IEnumerable<System.String>) (Syntax: '(IEnumerabl ... tring>)args')
          Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: True, IsUserDefined: False) (MethodSymbol: null)
          Operand: 
            IParameterReferenceOperation: args (OperationKind.ParameterReference, Type: System.Collections.Generic.List<System.String>) (Syntax: 'args')
  Body: 
    IBlockOperation (0 statements) (OperationKind.Block, Type: null) (Syntax: '{ }')
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
IForEachLoopOperation (LoopKind.ForEach, Continue Label Id: 0, Exit Label Id: 1) (OperationKind.Loop, Type: null) (Syntax: 'foreach (in ... }')
  Locals: Local_1: System.Int32 num
  LoopControlVariable: 
    IVariableDeclaratorOperation (Symbol: System.Int32 num) (OperationKind.VariableDeclarator, Type: null) (Syntax: 'int')
      Initializer: 
        null
  Collection: 
    IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.Collections.IEnumerable, IsImplicit) (Syntax: 'numbers')
      Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: True, IsUserDefined: False) (MethodSymbol: null)
      Operand: 
        ILocalReferenceOperation: numbers (OperationKind.LocalReference, Type: System.Int32[]) (Syntax: 'numbers')
  Body: 
    IBlockOperation (2 statements) (OperationKind.Block, Type: null) (Syntax: '{ ... }')
      IConditionalOperation (OperationKind.Conditional, Type: null) (Syntax: 'if (num > 3 ... }')
        Condition: 
          IBinaryOperation (BinaryOperatorKind.GreaterThan) (OperationKind.Binary, Type: System.Boolean) (Syntax: 'num > 3')
            Left: 
              ILocalReferenceOperation: num (OperationKind.LocalReference, Type: System.Int32) (Syntax: 'num')
            Right: 
              ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 3) (Syntax: '3')
        WhenTrue: 
          IBlockOperation (1 statements) (OperationKind.Block, Type: null) (Syntax: '{ ... }')
            IThrowOperation (OperationKind.Throw, Type: null) (Syntax: 'throw new S ... ""testing"");')
              IObjectCreationOperation (Constructor: System.Exception..ctor(System.String message)) (OperationKind.ObjectCreation, Type: System.Exception) (Syntax: 'new System. ... (""testing"")')
                Arguments(1):
                    IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: message) (OperationKind.Argument, Type: null) (Syntax: '""testing""')
                      ILiteralOperation (OperationKind.Literal, Type: System.String, Constant: ""testing"") (Syntax: '""testing""')
                      InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                      OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                Initializer: 
                  null
        WhenFalse: 
          null
      IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'System.Cons ... eLine(num);')
        Expression: 
          IInvocationOperation (void System.Console.WriteLine(System.Int32 value)) (OperationKind.Invocation, Type: System.Void) (Syntax: 'System.Cons ... teLine(num)')
            Instance Receiver: 
              null
            Arguments(1):
                IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: value) (OperationKind.Argument, Type: null) (Syntax: 'num')
                  ILocalReferenceOperation: num (OperationKind.LocalReference, Type: System.Int32) (Syntax: 'num')
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
IForEachLoopOperation (LoopKind.ForEach, Continue Label Id: 0, Exit Label Id: 1) (OperationKind.Loop, Type: null) (Syntax: 'foreach (va ... }')
  Locals: Local_1: System.Int32 a
    Local_2: System.Int32 b
  LoopControlVariable: 
    IDeclarationExpressionOperation (OperationKind.DeclarationExpression, Type: (System.Int32 a, System.Int32 b)) (Syntax: 'var (a, b)')
      ITupleOperation (OperationKind.Tuple, Type: (System.Int32 a, System.Int32 b)) (Syntax: '(a, b)')
        NaturalType: (System.Int32 a, System.Int32 b)
        Elements(2):
            ILocalReferenceOperation: a (IsDeclaration: True) (OperationKind.LocalReference, Type: System.Int32) (Syntax: 'a')
            ILocalReferenceOperation: b (IsDeclaration: True) (OperationKind.LocalReference, Type: System.Int32) (Syntax: 'b')
  Collection: 
    IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.Collections.IEnumerable, IsImplicit) (Syntax: 'x')
      Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: True, IsUserDefined: False) (MethodSymbol: null)
      Operand: 
        IParameterReferenceOperation: x (OperationKind.ParameterReference, Type: (System.Int32, System.Int32)[]) (Syntax: 'x')
  Body: 
    IBlockOperation (0 statements) (OperationKind.Block, Type: null) (Syntax: '{ ... }')
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
IForEachLoopOperation (LoopKind.ForEach, Continue Label Id: 0, Exit Label Id: 1) (OperationKind.Loop, Type: null) (Syntax: 'foreach (va ... }')
  Locals: Local_1: System.Int32 a
    Local_2: System.Int32 b
    Local_3: System.Int32 c
  LoopControlVariable: 
    IDeclarationExpressionOperation (OperationKind.DeclarationExpression, Type: (System.Int32 a, (System.Int32 b, System.Int32 c))) (Syntax: 'var (a, (b, c))')
      ITupleOperation (OperationKind.Tuple, Type: (System.Int32 a, (System.Int32 b, System.Int32 c))) (Syntax: '(a, (b, c))')
        NaturalType: (System.Int32 a, (System.Int32 b, System.Int32 c))
        Elements(2):
            ILocalReferenceOperation: a (IsDeclaration: True) (OperationKind.LocalReference, Type: System.Int32) (Syntax: 'a')
            ITupleOperation (OperationKind.Tuple, Type: (System.Int32 b, System.Int32 c)) (Syntax: '(b, c)')
              NaturalType: (System.Int32 b, System.Int32 c)
              Elements(2):
                  ILocalReferenceOperation: b (IsDeclaration: True) (OperationKind.LocalReference, Type: System.Int32) (Syntax: 'b')
                  ILocalReferenceOperation: c (IsDeclaration: True) (OperationKind.LocalReference, Type: System.Int32) (Syntax: 'c')
  Collection: 
    IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.Collections.IEnumerable, IsImplicit) (Syntax: 'x')
      Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: True, IsUserDefined: False) (MethodSymbol: null)
      Operand: 
        IParameterReferenceOperation: x (OperationKind.ParameterReference, Type: (System.Int32, (System.Int32, System.Int32))[]) (Syntax: 'x')
  Body: 
    IBlockOperation (0 statements) (OperationKind.Block, Type: null) (Syntax: '{ ... }')
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
IBlockOperation (4 statements) (OperationKind.Block, Type: null, IsInvalid) (Syntax: '{ ... }')
  IForEachLoopOperation (LoopKind.ForEach, Continue Label Id: 0, Exit Label Id: 1) (OperationKind.Loop, Type: null, IsInvalid) (Syntax: 'foreach (i')
    LoopControlVariable: 
      IInvalidOperation (OperationKind.Invalid, Type: ?, IsInvalid) (Syntax: 'i')
        Children(0)
    Collection: 
      IInvalidOperation (OperationKind.Invalid, Type: null, IsInvalid) (Syntax: '')
        Children(0)
    Body: 
      IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null, IsInvalid) (Syntax: '')
        Expression: 
          IInvalidOperation (OperationKind.Invalid, Type: null, IsInvalid) (Syntax: '')
            Children(0)
    NextVariables(0)
  IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null, IsInvalid) (Syntax: 'j ')
    Expression: 
      IInvalidOperation (OperationKind.Invalid, Type: ?, IsInvalid) (Syntax: 'j')
        Children(0)
  IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null, IsInvalid) (Syntax: 'x')
    Expression: 
      IParameterReferenceOperation: x (OperationKind.ParameterReference, Type: (System.Int32, System.Int32)[], IsInvalid) (Syntax: 'x')
  IBlockOperation (0 statements) (OperationKind.Block, Type: null) (Syntax: '{ ... }')
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
IBlockOperation (1 statements) (OperationKind.Block, Type: null, IsInvalid) (Syntax: '{ ... }')
  IForEachLoopOperation (LoopKind.ForEach, Continue Label Id: 0, Exit Label Id: 1) (OperationKind.Loop, Type: null, IsInvalid) (Syntax: 'foreach (x[ ... }')
    LoopControlVariable: 
      IArrayElementReferenceOperation (OperationKind.ArrayElementReference, Type: System.Int32) (Syntax: 'x[0]')
        Array reference: 
          IParameterReferenceOperation: x (OperationKind.ParameterReference, Type: System.Int32[]) (Syntax: 'x')
        Indices(1):
            ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 0) (Syntax: '0')
    Collection: 
      IParameterReferenceOperation: x (OperationKind.ParameterReference, Type: System.Int32[]) (Syntax: 'x')
    Body: 
      IBlockOperation (0 statements) (OperationKind.Block, Type: null) (Syntax: '{ ... }')
    NextVariables(0)
";
            var expectedDiagnostics = new DiagnosticDescription[] {
                Diagnostic(ErrorCode.ERR_BadForeachDecl, "in").WithLocation(6, 23)
            };

            VerifyOperationTreeAndDiagnosticsForTest<BlockSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation)]
        [Fact, WorkItem(17602, "https://github.com/dotnet/roslyn/issues/17602")]
        public void IForEachLoopStatement_InvalidLoopControlVariableDeclaration()
        {
            string source = @"
class X
{
    public static void M(int[] x)
    {
        int i = 0;
        /*<bind>*/foreach (int i in x)
        {
        }/*</bind>*/
    }
}
";
            string expectedOperationTree = @"
IForEachLoopOperation (LoopKind.ForEach, Continue Label Id: 0, Exit Label Id: 1) (OperationKind.Loop, Type: null, IsInvalid) (Syntax: 'foreach (in ... }')
  Locals: Local_1: System.Int32 i
  LoopControlVariable: 
    IVariableDeclaratorOperation (Symbol: System.Int32 i) (OperationKind.VariableDeclarator, Type: null) (Syntax: 'int')
      Initializer: 
        null
  Collection: 
    IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.Collections.IEnumerable, IsImplicit) (Syntax: 'x')
      Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: True, IsUserDefined: False) (MethodSymbol: null)
      Operand: 
        IParameterReferenceOperation: x (OperationKind.ParameterReference, Type: System.Int32[]) (Syntax: 'x')
  Body: 
    IBlockOperation (0 statements) (OperationKind.Block, Type: null) (Syntax: '{ ... }')
  NextVariables(0)
";
            var expectedDiagnostics = new DiagnosticDescription[] {
                // file.cs(7,32): error CS0136: A local or parameter named 'i' cannot be declared in this scope because that name is used in an enclosing local scope to define a local or parameter
                //         /*<bind>*/foreach (int i in x)
                Diagnostic(ErrorCode.ERR_LocalIllegallyOverrides, "i").WithArguments("i").WithLocation(7, 32),
                // file.cs(6,13): warning CS0219: The variable 'i' is assigned but its value is never used
                //         int i = 0;
                Diagnostic(ErrorCode.WRN_UnreferencedVarAssg, "i").WithArguments("i").WithLocation(6, 13)
            };

            VerifyOperationTreeAndDiagnosticsForTest<ForEachStatementSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation)]
        [Fact, WorkItem(19996, "https://github.com/dotnet/roslyn/issues/19996")]
        public void IForEachLoopStatement_InvalidLoopControlVariableExpression_01()
        {
            string source = @"
class C
{
    void M(int a, int b)
    {
        int[] arr = new int[10];
        /*<bind>*/foreach (M(1, 2) in arr)
        {
        }/*</bind>*/
    }
}
";
            string expectedOperationTree = @"
IForEachLoopOperation (LoopKind.ForEach, Continue Label Id: 0, Exit Label Id: 1) (OperationKind.Loop, Type: null, IsInvalid) (Syntax: 'foreach (M( ... }')
  LoopControlVariable: 
    IInvocationOperation ( void C.M(System.Int32 a, System.Int32 b)) (OperationKind.Invocation, Type: System.Void) (Syntax: 'M(1, 2)')
      Instance Receiver: 
        IInstanceReferenceOperation (ReferenceKind: ContainingTypeInstance) (OperationKind.InstanceReference, Type: C, IsImplicit) (Syntax: 'M')
      Arguments(2):
          IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: a) (OperationKind.Argument, Type: null) (Syntax: '1')
            ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 1) (Syntax: '1')
            InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
            OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
          IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: b) (OperationKind.Argument, Type: null) (Syntax: '2')
            ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 2) (Syntax: '2')
            InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
            OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
  Collection: 
    ILocalReferenceOperation: arr (OperationKind.LocalReference, Type: System.Int32[]) (Syntax: 'arr')
  Body: 
    IBlockOperation (0 statements) (OperationKind.Block, Type: null) (Syntax: '{ ... }')
  NextVariables(0)
";
            var expectedDiagnostics = new DiagnosticDescription[] {
                // error CS0230: Type and identifier are both required in a foreach statement
                //         /*<bind>*/foreach (M(1, 2) in arr)
                Diagnostic(ErrorCode.ERR_BadForeachDecl, "in").WithLocation(7, 36)
            };

            VerifyOperationTreeAndDiagnosticsForTest<ForEachVariableStatementSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation)]
        [Fact, WorkItem(17602, "https://github.com/dotnet/roslyn/issues/17602")]
        public void IForEachLoopStatement_InvalidLoopControlVariableExpression_02()
        {
            string source = @"
class C
{
    void M(int a, int b)
    {
        int[] arr = new int[10];
        /*<bind>*/foreach (M2(out var x) in arr)
        {
        }/*</bind>*/
    }

    void M2(out int x)
    {
        x = 0;
    }
}
";
            string expectedOperationTree = @"
IForEachLoopOperation (LoopKind.ForEach, Continue Label Id: 0, Exit Label Id: 1) (OperationKind.Loop, Type: null, IsInvalid) (Syntax: 'foreach (M2 ... }')
  Locals: Local_1: System.Int32 x
  LoopControlVariable: 
    IInvocationOperation ( void C.M2(out System.Int32 x)) (OperationKind.Invocation, Type: System.Void) (Syntax: 'M2(out var x)')
      Instance Receiver: 
        IInstanceReferenceOperation (ReferenceKind: ContainingTypeInstance) (OperationKind.InstanceReference, Type: C, IsImplicit) (Syntax: 'M2')
      Arguments(1):
          IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: x) (OperationKind.Argument, Type: null) (Syntax: 'out var x')
            IDeclarationExpressionOperation (OperationKind.DeclarationExpression, Type: System.Int32) (Syntax: 'var x')
              ILocalReferenceOperation: x (IsDeclaration: True) (OperationKind.LocalReference, Type: System.Int32) (Syntax: 'x')
            InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
            OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
  Collection: 
    ILocalReferenceOperation: arr (OperationKind.LocalReference, Type: System.Int32[]) (Syntax: 'arr')
  Body: 
    IBlockOperation (0 statements) (OperationKind.Block, Type: null) (Syntax: '{ ... }')
  NextVariables(0)
";
            var expectedDiagnostics = new DiagnosticDescription[] {
                // file.cs(7,42): error CS0230: Type and identifier are both required in a foreach statement
                //         /*<bind>*/foreach (M2(out var x) in arr)
                Diagnostic(ErrorCode.ERR_BadForeachDecl, "in").WithLocation(7, 42)
            };

            VerifyOperationTreeAndDiagnosticsForTest<ForEachVariableStatementSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }


        [CompilerTrait(CompilerFeature.IOperation)]
        [Fact, WorkItem(17602, "https://github.com/dotnet/roslyn/issues/17602")]
        public void IForEachLoopStatement_InvalidLoopControlVariableExpression_03()
        {
            string source = @"
class C
{
    void M(object o)
    {
        int[] arr = new int[10];
        /*<bind>*/foreach (o is int x in arr)
        {
        }/*</bind>*/
    }
}
";
            string expectedOperationTree = @"
IForEachLoopOperation (LoopKind.ForEach, Continue Label Id: 0, Exit Label Id: 1) (OperationKind.Loop, Type: null, IsInvalid) (Syntax: 'foreach (o  ... }')
  Locals: Local_1: System.Int32 x
  LoopControlVariable: 
    IIsPatternOperation (OperationKind.IsPattern, Type: System.Boolean) (Syntax: 'o is int x')
      Value: 
        IParameterReferenceOperation: o (OperationKind.ParameterReference, Type: System.Object) (Syntax: 'o')
      Pattern: 
        IDeclarationPatternOperation (OperationKind.DeclarationPattern, Type: null) (Syntax: 'int x') (InputType: System.Object, DeclaredSymbol: System.Int32 x, MatchesNull: False)
  Collection: 
    ILocalReferenceOperation: arr (OperationKind.LocalReference, Type: System.Int32[]) (Syntax: 'arr')
  Body: 
    IBlockOperation (0 statements) (OperationKind.Block, Type: null) (Syntax: '{ ... }')
  NextVariables(0)
";
            var expectedDiagnostics = new DiagnosticDescription[] {
                // file.cs(7,39): error CS0230: Type and identifier are both required in a foreach statement
                //         /*<bind>*/foreach (o is int x in arr)
                Diagnostic(ErrorCode.ERR_BadForeachDecl, "in").WithLocation(7, 39)
            };

            VerifyOperationTreeAndDiagnosticsForTest<ForEachVariableStatementSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation, CompilerFeature.Dataflow)]
        [Fact]
        public void ForEachFlow_01()
        {
            string source = @"
public class MyClass
{
    void M(MyClass[] a, MyClass[] b)
    /*<bind>*/{
        foreach (var x in a ?? b) 
        { 
            x?.ToString();
        }
    }/*</bind>*/
}
";
            var expectedDiagnostics = DiagnosticDescription.None;

            string expectedFlowGraph = @"
Block[B0] - Entry
    Statements (0)
    Next (Regular) Block[B1]
        Entering: {R1} {R2} {R3}

.locals {R1}
{
    CaptureIds: [2]
    .locals {R2}
    {
        CaptureIds: [1]
        .locals {R3}
        {
            CaptureIds: [0]
            Block[B1] - Block
                Predecessors: [B0]
                Statements (1)
                    IFlowCaptureOperation: 0 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'a')
                      Value: 
                        IParameterReferenceOperation: a (OperationKind.ParameterReference, Type: MyClass[]) (Syntax: 'a')

                Jump if True (Regular) to Block[B3]
                    IIsNullOperation (OperationKind.IsNull, Type: System.Boolean, IsImplicit) (Syntax: 'a')
                      Operand: 
                        IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: MyClass[], IsImplicit) (Syntax: 'a')
                    Leaving: {R3}

                Next (Regular) Block[B2]
            Block[B2] - Block
                Predecessors: [B1]
                Statements (1)
                    IFlowCaptureOperation: 1 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'a')
                      Value: 
                        IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: MyClass[], IsImplicit) (Syntax: 'a')

                Next (Regular) Block[B4]
                    Leaving: {R3}
        }

        Block[B3] - Block
            Predecessors: [B1]
            Statements (1)
                IFlowCaptureOperation: 1 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'b')
                  Value: 
                    IParameterReferenceOperation: b (OperationKind.ParameterReference, Type: MyClass[]) (Syntax: 'b')

            Next (Regular) Block[B4]
        Block[B4] - Block
            Predecessors: [B2] [B3]
            Statements (1)
                IFlowCaptureOperation: 2 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'a ?? b')
                  Value: 
                    IInvocationOperation (virtual System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()) (OperationKind.Invocation, Type: System.Collections.IEnumerator, IsImplicit) (Syntax: 'a ?? b')
                      Instance Receiver: 
                        IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.Collections.IEnumerable, IsImplicit) (Syntax: 'a ?? b')
                          Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: True, IsUserDefined: False) (MethodSymbol: null)
                            (ImplicitReference)
                          Operand: 
                            IFlowCaptureReferenceOperation: 1 (OperationKind.FlowCaptureReference, Type: MyClass[], IsImplicit) (Syntax: 'a ?? b')
                      Arguments(0)

            Next (Regular) Block[B5]
                Leaving: {R2}
                Entering: {R4} {R5}
    }
    .try {R4, R5}
    {
        Block[B5] - Block
            Predecessors: [B4] [B7] [B8]
            Statements (0)
            Jump if False (Regular) to Block[B12]
                IInvocationOperation (virtual System.Boolean System.Collections.IEnumerator.MoveNext()) (OperationKind.Invocation, Type: System.Boolean, IsImplicit) (Syntax: 'a ?? b')
                  Instance Receiver: 
                    IFlowCaptureReferenceOperation: 2 (OperationKind.FlowCaptureReference, Type: System.Collections.IEnumerator, IsImplicit) (Syntax: 'a ?? b')
                  Arguments(0)
                Finalizing: {R8}
                Leaving: {R5} {R4} {R1}

            Next (Regular) Block[B6]
                Entering: {R6}

        .locals {R6}
        {
            Locals: [MyClass x]
            Block[B6] - Block
                Predecessors: [B5]
                Statements (1)
                    ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: null, IsImplicit) (Syntax: 'var')
                      Left: 
                        ILocalReferenceOperation: x (IsDeclaration: True) (OperationKind.LocalReference, Type: MyClass, IsImplicit) (Syntax: 'var')
                      Right: 
                        IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: MyClass, IsImplicit) (Syntax: 'var')
                          Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: True, IsUserDefined: False) (MethodSymbol: null)
                            (ExplicitReference)
                          Operand: 
                            IPropertyReferenceOperation: System.Object System.Collections.IEnumerator.Current { get; } (OperationKind.PropertyReference, Type: System.Object, IsImplicit) (Syntax: 'var')
                              Instance Receiver: 
                                IFlowCaptureReferenceOperation: 2 (OperationKind.FlowCaptureReference, Type: System.Collections.IEnumerator, IsImplicit) (Syntax: 'a ?? b')

                Next (Regular) Block[B7]
                    Entering: {R7}

            .locals {R7}
            {
                CaptureIds: [3]
                Block[B7] - Block
                    Predecessors: [B6]
                    Statements (1)
                        IFlowCaptureOperation: 3 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'x')
                          Value: 
                            ILocalReferenceOperation: x (OperationKind.LocalReference, Type: MyClass) (Syntax: 'x')

                    Jump if True (Regular) to Block[B5]
                        IIsNullOperation (OperationKind.IsNull, Type: System.Boolean, IsImplicit) (Syntax: 'x')
                          Operand: 
                            IFlowCaptureReferenceOperation: 3 (OperationKind.FlowCaptureReference, Type: MyClass, IsImplicit) (Syntax: 'x')
                        Leaving: {R7} {R6}

                    Next (Regular) Block[B8]
                Block[B8] - Block
                    Predecessors: [B7]
                    Statements (1)
                        IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'x?.ToString();')
                          Expression: 
                            IInvocationOperation (virtual System.String System.Object.ToString()) (OperationKind.Invocation, Type: System.String) (Syntax: '.ToString()')
                              Instance Receiver: 
                                IFlowCaptureReferenceOperation: 3 (OperationKind.FlowCaptureReference, Type: MyClass, IsImplicit) (Syntax: 'x')
                              Arguments(0)

                    Next (Regular) Block[B5]
                        Leaving: {R7} {R6}
            }
        }
    }
    .finally {R8}
    {
        CaptureIds: [4]
        Block[B9] - Block
            Predecessors (0)
            Statements (1)
                IFlowCaptureOperation: 4 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'a ?? b')
                  Value: 
                    IConversionOperation (TryCast: True, Unchecked) (OperationKind.Conversion, Type: System.IDisposable, IsImplicit) (Syntax: 'a ?? b')
                      Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: True, IsUserDefined: False) (MethodSymbol: null)
                        (ExplicitReference)
                      Operand: 
                        IFlowCaptureReferenceOperation: 2 (OperationKind.FlowCaptureReference, Type: System.Collections.IEnumerator, IsImplicit) (Syntax: 'a ?? b')

            Jump if True (Regular) to Block[B11]
                IIsNullOperation (OperationKind.IsNull, Type: System.Boolean, IsImplicit) (Syntax: 'a ?? b')
                  Operand: 
                    IFlowCaptureReferenceOperation: 4 (OperationKind.FlowCaptureReference, Type: System.IDisposable, IsImplicit) (Syntax: 'a ?? b')

            Next (Regular) Block[B10]
        Block[B10] - Block
            Predecessors: [B9]
            Statements (1)
                IInvocationOperation (virtual void System.IDisposable.Dispose()) (OperationKind.Invocation, Type: System.Void, IsImplicit) (Syntax: 'a ?? b')
                  Instance Receiver: 
                    IFlowCaptureReferenceOperation: 4 (OperationKind.FlowCaptureReference, Type: System.IDisposable, IsImplicit) (Syntax: 'a ?? b')
                  Arguments(0)

            Next (Regular) Block[B11]
        Block[B11] - Block
            Predecessors: [B9] [B10]
            Statements (0)
            Next (StructuredExceptionHandling) Block[null]
    }
}

Block[B12] - Exit
    Predecessors: [B5]
    Statements (0)
";
            VerifyFlowGraphAndDiagnosticsForTest<BlockSyntax>(source, expectedFlowGraph, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation, CompilerFeature.Dataflow)]
        [Fact]
        public void ForEachFlow_02()
        {
            string source = @"
public class MyClass
{
    void M(string a, bool result)
    /*<bind>*/{
        foreach (ushort x in a) 
        { 
            result = true;
        }
    }/*</bind>*/
}
";
            var expectedDiagnostics = DiagnosticDescription.None;

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
            IFlowCaptureOperation: 0 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'a')
              Value: 
                IInvocationOperation ( System.CharEnumerator System.String.GetEnumerator()) (OperationKind.Invocation, Type: System.CharEnumerator, IsImplicit) (Syntax: 'a')
                  Instance Receiver: 
                    IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.String, IsImplicit) (Syntax: 'a')
                      Conversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                        (Identity)
                      Operand: 
                        IParameterReferenceOperation: a (OperationKind.ParameterReference, Type: System.String) (Syntax: 'a')
                  Arguments(0)

        Next (Regular) Block[B2]
            Entering: {R2} {R3}

    .try {R2, R3}
    {
        Block[B2] - Block
            Predecessors: [B1] [B3]
            Statements (0)
            Jump if False (Regular) to Block[B7]
                IInvocationOperation ( System.Boolean System.CharEnumerator.MoveNext()) (OperationKind.Invocation, Type: System.Boolean, IsImplicit) (Syntax: 'a')
                  Instance Receiver: 
                    IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: System.CharEnumerator, IsImplicit) (Syntax: 'a')
                  Arguments(0)
                Finalizing: {R5}
                Leaving: {R3} {R2} {R1}

            Next (Regular) Block[B3]
                Entering: {R4}

        .locals {R4}
        {
            Locals: [System.UInt16 x]
            Block[B3] - Block
                Predecessors: [B2]
                Statements (2)
                    ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: null, IsImplicit) (Syntax: 'ushort')
                      Left: 
                        ILocalReferenceOperation: x (IsDeclaration: True) (OperationKind.LocalReference, Type: System.UInt16, IsImplicit) (Syntax: 'ushort')
                      Right: 
                        IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.UInt16, IsImplicit) (Syntax: 'ushort')
                          Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: True, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                            (ImplicitNumeric)
                          Operand: 
                            IPropertyReferenceOperation: System.Char System.CharEnumerator.Current { get; } (OperationKind.PropertyReference, Type: System.Char, IsImplicit) (Syntax: 'ushort')
                              Instance Receiver: 
                                IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: System.CharEnumerator, IsImplicit) (Syntax: 'a')

                    IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'result = true;')
                      Expression: 
                        ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Boolean) (Syntax: 'result = true')
                          Left: 
                            IParameterReferenceOperation: result (OperationKind.ParameterReference, Type: System.Boolean) (Syntax: 'result')
                          Right: 
                            ILiteralOperation (OperationKind.Literal, Type: System.Boolean, Constant: True) (Syntax: 'true')

                Next (Regular) Block[B2]
                    Leaving: {R4}
        }
    }
    .finally {R5}
    {
        Block[B4] - Block
            Predecessors (0)
            Statements (0)
            Jump if True (Regular) to Block[B6]
                IIsNullOperation (OperationKind.IsNull, Type: System.Boolean, IsImplicit) (Syntax: 'a')
                  Operand: 
                    IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: System.CharEnumerator, IsImplicit) (Syntax: 'a')

            Next (Regular) Block[B5]
        Block[B5] - Block
            Predecessors: [B4]
            Statements (1)
                IInvocationOperation (virtual void System.IDisposable.Dispose()) (OperationKind.Invocation, Type: System.Void, IsImplicit) (Syntax: 'a')
                  Instance Receiver: 
                    IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.IDisposable, IsImplicit) (Syntax: 'a')
                      Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: True, IsUserDefined: False) (MethodSymbol: null)
                        (ImplicitReference)
                      Operand: 
                        IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: System.CharEnumerator, IsImplicit) (Syntax: 'a')
                  Arguments(0)

            Next (Regular) Block[B6]
        Block[B6] - Block
            Predecessors: [B4] [B5]
            Statements (0)
            Next (StructuredExceptionHandling) Block[null]
    }
}

Block[B7] - Exit
    Predecessors: [B2]
    Statements (0)
";
            VerifyFlowGraphAndDiagnosticsForTest<BlockSyntax>(source, expectedFlowGraph, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation, CompilerFeature.Dataflow)]
        [Fact]
        public void ForEachFlow_03()
        {
            string source = @"
public class MyClass
{
    void M(int[,] a, long result)
    /*<bind>*/{
        foreach (long x in a) 
        { 
            result = x;
        }
    }/*</bind>*/
}
";
            var expectedDiagnostics = DiagnosticDescription.None;

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
            IFlowCaptureOperation: 0 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'a')
              Value: 
                IInvocationOperation (virtual System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()) (OperationKind.Invocation, Type: System.Collections.IEnumerator, IsImplicit) (Syntax: 'a')
                  Instance Receiver: 
                    IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.Collections.IEnumerable, IsImplicit) (Syntax: 'a')
                      Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: True, IsUserDefined: False) (MethodSymbol: null)
                        (ImplicitReference)
                      Operand: 
                        IParameterReferenceOperation: a (OperationKind.ParameterReference, Type: System.Int32[,]) (Syntax: 'a')
                  Arguments(0)

        Next (Regular) Block[B2]
            Entering: {R2} {R3}

    .try {R2, R3}
    {
        Block[B2] - Block
            Predecessors: [B1] [B3]
            Statements (0)
            Jump if False (Regular) to Block[B7]
                IInvocationOperation (virtual System.Boolean System.Collections.IEnumerator.MoveNext()) (OperationKind.Invocation, Type: System.Boolean, IsImplicit) (Syntax: 'a')
                  Instance Receiver: 
                    IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: System.Collections.IEnumerator, IsImplicit) (Syntax: 'a')
                  Arguments(0)
                Finalizing: {R5}
                Leaving: {R3} {R2} {R1}

            Next (Regular) Block[B3]
                Entering: {R4}

        .locals {R4}
        {
            Locals: [System.Int64 x]
            Block[B3] - Block
                Predecessors: [B2]
                Statements (2)
                    ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: null, IsImplicit) (Syntax: 'long')
                      Left: 
                        ILocalReferenceOperation: x (IsDeclaration: True) (OperationKind.LocalReference, Type: System.Int64, IsImplicit) (Syntax: 'long')
                      Right: 
                        IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.Int64, IsImplicit) (Syntax: 'long')
                          Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: True, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                            (ImplicitNumeric)
                          Operand: 
                            IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.Int32, IsImplicit) (Syntax: 'long')
                              Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                                (Unboxing)
                              Operand: 
                                IPropertyReferenceOperation: System.Object System.Collections.IEnumerator.Current { get; } (OperationKind.PropertyReference, Type: System.Object, IsImplicit) (Syntax: 'long')
                                  Instance Receiver: 
                                    IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: System.Collections.IEnumerator, IsImplicit) (Syntax: 'a')

                    IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'result = x;')
                      Expression: 
                        ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Int64) (Syntax: 'result = x')
                          Left: 
                            IParameterReferenceOperation: result (OperationKind.ParameterReference, Type: System.Int64) (Syntax: 'result')
                          Right: 
                            ILocalReferenceOperation: x (OperationKind.LocalReference, Type: System.Int64) (Syntax: 'x')

                Next (Regular) Block[B2]
                    Leaving: {R4}
        }
    }
    .finally {R5}
    {
        CaptureIds: [1]
        Block[B4] - Block
            Predecessors (0)
            Statements (1)
                IFlowCaptureOperation: 1 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'a')
                  Value: 
                    IConversionOperation (TryCast: True, Unchecked) (OperationKind.Conversion, Type: System.IDisposable, IsImplicit) (Syntax: 'a')
                      Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: True, IsUserDefined: False) (MethodSymbol: null)
                        (ExplicitReference)
                      Operand: 
                        IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: System.Collections.IEnumerator, IsImplicit) (Syntax: 'a')

            Jump if True (Regular) to Block[B6]
                IIsNullOperation (OperationKind.IsNull, Type: System.Boolean, IsImplicit) (Syntax: 'a')
                  Operand: 
                    IFlowCaptureReferenceOperation: 1 (OperationKind.FlowCaptureReference, Type: System.IDisposable, IsImplicit) (Syntax: 'a')

            Next (Regular) Block[B5]
        Block[B5] - Block
            Predecessors: [B4]
            Statements (1)
                IInvocationOperation (virtual void System.IDisposable.Dispose()) (OperationKind.Invocation, Type: System.Void, IsImplicit) (Syntax: 'a')
                  Instance Receiver: 
                    IFlowCaptureReferenceOperation: 1 (OperationKind.FlowCaptureReference, Type: System.IDisposable, IsImplicit) (Syntax: 'a')
                  Arguments(0)

            Next (Regular) Block[B6]
        Block[B6] - Block
            Predecessors: [B4] [B5]
            Statements (0)
            Next (StructuredExceptionHandling) Block[null]
    }
}

Block[B7] - Exit
    Predecessors: [B2]
    Statements (0)
";
            VerifyFlowGraphAndDiagnosticsForTest<BlockSyntax>(source, expectedFlowGraph, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation, CompilerFeature.Dataflow)]
        [Fact]
        public void ForEachFlow_04()
        {
            string source = @"
public class MyClass
{
    void M(Enumerable e, long result)
    /*<bind>*/{
        foreach (long x in e)
        { 
            result = x;
        }
    }/*</bind>*/
}

struct Enumerable
{
    public Enumerator GetEnumerator() { return new Enumerator(); }
}

struct Enumerator
{
    public int Current { get { return 1; } }
    public bool MoveNext() { return false; }
}
";
            var expectedDiagnostics = DiagnosticDescription.None;

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
            IFlowCaptureOperation: 0 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'e')
              Value: 
                IInvocationOperation ( Enumerator Enumerable.GetEnumerator()) (OperationKind.Invocation, Type: Enumerator, IsImplicit) (Syntax: 'e')
                  Instance Receiver: 
                    IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: Enumerable, IsImplicit) (Syntax: 'e')
                      Conversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                        (Identity)
                      Operand: 
                        IParameterReferenceOperation: e (OperationKind.ParameterReference, Type: Enumerable) (Syntax: 'e')
                  Arguments(0)

        Next (Regular) Block[B2]
    Block[B2] - Block
        Predecessors: [B1] [B3]
        Statements (0)
        Jump if False (Regular) to Block[B4]
            IInvocationOperation ( System.Boolean Enumerator.MoveNext()) (OperationKind.Invocation, Type: System.Boolean, IsImplicit) (Syntax: 'e')
              Instance Receiver: 
                IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: Enumerator, IsImplicit) (Syntax: 'e')
              Arguments(0)
            Leaving: {R1}

        Next (Regular) Block[B3]
            Entering: {R2}

    .locals {R2}
    {
        Locals: [System.Int64 x]
        Block[B3] - Block
            Predecessors: [B2]
            Statements (2)
                ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: null, IsImplicit) (Syntax: 'long')
                  Left: 
                    ILocalReferenceOperation: x (IsDeclaration: True) (OperationKind.LocalReference, Type: System.Int64, IsImplicit) (Syntax: 'long')
                  Right: 
                    IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.Int64, IsImplicit) (Syntax: 'long')
                      Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: True, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                        (ImplicitNumeric)
                      Operand: 
                        IPropertyReferenceOperation: System.Int32 Enumerator.Current { get; } (OperationKind.PropertyReference, Type: System.Int32, IsImplicit) (Syntax: 'long')
                          Instance Receiver: 
                            IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: Enumerator, IsImplicit) (Syntax: 'e')

                IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'result = x;')
                  Expression: 
                    ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Int64) (Syntax: 'result = x')
                      Left: 
                        IParameterReferenceOperation: result (OperationKind.ParameterReference, Type: System.Int64) (Syntax: 'result')
                      Right: 
                        ILocalReferenceOperation: x (OperationKind.LocalReference, Type: System.Int64) (Syntax: 'x')

            Next (Regular) Block[B2]
                Leaving: {R2}
    }
}

Block[B4] - Exit
    Predecessors: [B2]
    Statements (0)
";
            VerifyFlowGraphAndDiagnosticsForTest<BlockSyntax>(source, expectedFlowGraph, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation, CompilerFeature.Dataflow)]
        [Fact]
        public void ForEachFlow_05()
        {
            string source = @"
public class MyClass
{
    void M(Enumerable e, long result)
    /*<bind>*/{
        foreach (long x in e)
        { 
            result = x;
        }
    }/*</bind>*/
}

struct Enumerable
{
    public Enumerator GetEnumerator() { return new Enumerator(); }
}

struct Enumerator : System.IDisposable
{
    public int Current { get { return 1; } }
    public bool MoveNext() { return false; }
    public void Dispose() {}
}
";
            var expectedDiagnostics = DiagnosticDescription.None;

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
            IFlowCaptureOperation: 0 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'e')
              Value: 
                IInvocationOperation ( Enumerator Enumerable.GetEnumerator()) (OperationKind.Invocation, Type: Enumerator, IsImplicit) (Syntax: 'e')
                  Instance Receiver: 
                    IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: Enumerable, IsImplicit) (Syntax: 'e')
                      Conversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                        (Identity)
                      Operand: 
                        IParameterReferenceOperation: e (OperationKind.ParameterReference, Type: Enumerable) (Syntax: 'e')
                  Arguments(0)

        Next (Regular) Block[B2]
            Entering: {R2} {R3}

    .try {R2, R3}
    {
        Block[B2] - Block
            Predecessors: [B1] [B3]
            Statements (0)
            Jump if False (Regular) to Block[B5]
                IInvocationOperation ( System.Boolean Enumerator.MoveNext()) (OperationKind.Invocation, Type: System.Boolean, IsImplicit) (Syntax: 'e')
                  Instance Receiver: 
                    IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: Enumerator, IsImplicit) (Syntax: 'e')
                  Arguments(0)
                Finalizing: {R5}
                Leaving: {R3} {R2} {R1}

            Next (Regular) Block[B3]
                Entering: {R4}

        .locals {R4}
        {
            Locals: [System.Int64 x]
            Block[B3] - Block
                Predecessors: [B2]
                Statements (2)
                    ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: null, IsImplicit) (Syntax: 'long')
                      Left: 
                        ILocalReferenceOperation: x (IsDeclaration: True) (OperationKind.LocalReference, Type: System.Int64, IsImplicit) (Syntax: 'long')
                      Right: 
                        IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.Int64, IsImplicit) (Syntax: 'long')
                          Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: True, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                            (ImplicitNumeric)
                          Operand: 
                            IPropertyReferenceOperation: System.Int32 Enumerator.Current { get; } (OperationKind.PropertyReference, Type: System.Int32, IsImplicit) (Syntax: 'long')
                              Instance Receiver: 
                                IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: Enumerator, IsImplicit) (Syntax: 'e')

                    IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'result = x;')
                      Expression: 
                        ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Int64) (Syntax: 'result = x')
                          Left: 
                            IParameterReferenceOperation: result (OperationKind.ParameterReference, Type: System.Int64) (Syntax: 'result')
                          Right: 
                            ILocalReferenceOperation: x (OperationKind.LocalReference, Type: System.Int64) (Syntax: 'x')

                Next (Regular) Block[B2]
                    Leaving: {R4}
        }
    }
    .finally {R5}
    {
        Block[B4] - Block
            Predecessors (0)
            Statements (1)
                IInvocationOperation (virtual void System.IDisposable.Dispose()) (OperationKind.Invocation, Type: System.Void, IsImplicit) (Syntax: 'e')
                  Instance Receiver: 
                    IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.IDisposable, IsImplicit) (Syntax: 'e')
                      Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                        (Boxing)
                      Operand: 
                        IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: Enumerator, IsImplicit) (Syntax: 'e')
                  Arguments(0)

            Next (StructuredExceptionHandling) Block[null]
    }
}

Block[B5] - Exit
    Predecessors: [B2]
    Statements (0)
";
            VerifyFlowGraphAndDiagnosticsForTest<BlockSyntax>(source, expectedFlowGraph, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation, CompilerFeature.Dataflow)]
        [Fact]
        public void ForEachFlow_06()
        {
            string source = @"
public class MyClass
{
    void M(System.Collections.Generic.IEnumerable<int> e, int result)
    /*<bind>*/{
        foreach (var x in e)
        { 
            result = x;
        }
    }/*</bind>*/
}
";
            var expectedDiagnostics = DiagnosticDescription.None;

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
            IFlowCaptureOperation: 0 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'e')
              Value: 
                IInvocationOperation (virtual System.Collections.Generic.IEnumerator<System.Int32> System.Collections.Generic.IEnumerable<System.Int32>.GetEnumerator()) (OperationKind.Invocation, Type: System.Collections.Generic.IEnumerator<System.Int32>, IsImplicit) (Syntax: 'e')
                  Instance Receiver: 
                    IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.Collections.Generic.IEnumerable<System.Int32>, IsImplicit) (Syntax: 'e')
                      Conversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                        (Identity)
                      Operand: 
                        IParameterReferenceOperation: e (OperationKind.ParameterReference, Type: System.Collections.Generic.IEnumerable<System.Int32>) (Syntax: 'e')
                  Arguments(0)

        Next (Regular) Block[B2]
            Entering: {R2} {R3}

    .try {R2, R3}
    {
        Block[B2] - Block
            Predecessors: [B1] [B3]
            Statements (0)
            Jump if False (Regular) to Block[B7]
                IInvocationOperation (virtual System.Boolean System.Collections.IEnumerator.MoveNext()) (OperationKind.Invocation, Type: System.Boolean, IsImplicit) (Syntax: 'e')
                  Instance Receiver: 
                    IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: System.Collections.Generic.IEnumerator<System.Int32>, IsImplicit) (Syntax: 'e')
                  Arguments(0)
                Finalizing: {R5}
                Leaving: {R3} {R2} {R1}

            Next (Regular) Block[B3]
                Entering: {R4}

        .locals {R4}
        {
            Locals: [System.Int32 x]
            Block[B3] - Block
                Predecessors: [B2]
                Statements (2)
                    ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: null, IsImplicit) (Syntax: 'var')
                      Left: 
                        ILocalReferenceOperation: x (IsDeclaration: True) (OperationKind.LocalReference, Type: System.Int32, IsImplicit) (Syntax: 'var')
                      Right: 
                        IPropertyReferenceOperation: System.Int32 System.Collections.Generic.IEnumerator<System.Int32>.Current { get; } (OperationKind.PropertyReference, Type: System.Int32, IsImplicit) (Syntax: 'var')
                          Instance Receiver: 
                            IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: System.Collections.Generic.IEnumerator<System.Int32>, IsImplicit) (Syntax: 'e')

                    IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'result = x;')
                      Expression: 
                        ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Int32) (Syntax: 'result = x')
                          Left: 
                            IParameterReferenceOperation: result (OperationKind.ParameterReference, Type: System.Int32) (Syntax: 'result')
                          Right: 
                            ILocalReferenceOperation: x (OperationKind.LocalReference, Type: System.Int32) (Syntax: 'x')

                Next (Regular) Block[B2]
                    Leaving: {R4}
        }
    }
    .finally {R5}
    {
        Block[B4] - Block
            Predecessors (0)
            Statements (0)
            Jump if True (Regular) to Block[B6]
                IIsNullOperation (OperationKind.IsNull, Type: System.Boolean, IsImplicit) (Syntax: 'e')
                  Operand: 
                    IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: System.Collections.Generic.IEnumerator<System.Int32>, IsImplicit) (Syntax: 'e')

            Next (Regular) Block[B5]
        Block[B5] - Block
            Predecessors: [B4]
            Statements (1)
                IInvocationOperation (virtual void System.IDisposable.Dispose()) (OperationKind.Invocation, Type: System.Void, IsImplicit) (Syntax: 'e')
                  Instance Receiver: 
                    IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.IDisposable, IsImplicit) (Syntax: 'e')
                      Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: True, IsUserDefined: False) (MethodSymbol: null)
                        (ImplicitReference)
                      Operand: 
                        IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: System.Collections.Generic.IEnumerator<System.Int32>, IsImplicit) (Syntax: 'e')
                  Arguments(0)

            Next (Regular) Block[B6]
        Block[B6] - Block
            Predecessors: [B4] [B5]
            Statements (0)
            Next (StructuredExceptionHandling) Block[null]
    }
}

Block[B7] - Exit
    Predecessors: [B2]
    Statements (0)
";
            VerifyFlowGraphAndDiagnosticsForTest<BlockSyntax>(source, expectedFlowGraph, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation, CompilerFeature.Dataflow)]
        [Fact]
        public void ForEachFlow_07()
        {
            string source = @"
public class MyClass
{
    void M(in System.Span<int> e, int result)
    /*<bind>*/{
        foreach (var x in e)
        { 
            result = x;
        }
    }/*</bind>*/
}
";
            var expectedDiagnostics = DiagnosticDescription.None;

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
            IFlowCaptureOperation: 0 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'e')
              Value: 
                IInvocationOperation ( System.Span<System.Int32>.Enumerator System.Span<System.Int32>.GetEnumerator()) (OperationKind.Invocation, Type: System.Span<System.Int32>.Enumerator, IsImplicit) (Syntax: 'e')
                  Instance Receiver: 
                    IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.Span<System.Int32>, IsImplicit) (Syntax: 'e')
                      Conversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                        (Identity)
                      Operand: 
                        IParameterReferenceOperation: e (OperationKind.ParameterReference, Type: System.Span<System.Int32>) (Syntax: 'e')
                  Arguments(0)

        Next (Regular) Block[B2]
    Block[B2] - Block
        Predecessors: [B1] [B3]
        Statements (0)
        Jump if False (Regular) to Block[B4]
            IInvocationOperation ( System.Boolean System.Span<System.Int32>.Enumerator.MoveNext()) (OperationKind.Invocation, Type: System.Boolean, IsImplicit) (Syntax: 'e')
              Instance Receiver: 
                IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: System.Span<System.Int32>.Enumerator, IsImplicit) (Syntax: 'e')
              Arguments(0)
            Leaving: {R1}

        Next (Regular) Block[B3]
            Entering: {R2}

    .locals {R2}
    {
        Locals: [System.Int32 x]
        Block[B3] - Block
            Predecessors: [B2]
            Statements (2)
                ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: null, IsImplicit) (Syntax: 'var')
                  Left: 
                    ILocalReferenceOperation: x (IsDeclaration: True) (OperationKind.LocalReference, Type: System.Int32, IsImplicit) (Syntax: 'var')
                  Right: 
                    IPropertyReferenceOperation: ref System.Int32 System.Span<System.Int32>.Enumerator.Current { get; } (OperationKind.PropertyReference, Type: System.Int32, IsImplicit) (Syntax: 'var')
                      Instance Receiver: 
                        IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: System.Span<System.Int32>.Enumerator, IsImplicit) (Syntax: 'e')

                IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'result = x;')
                  Expression: 
                    ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Int32) (Syntax: 'result = x')
                      Left: 
                        IParameterReferenceOperation: result (OperationKind.ParameterReference, Type: System.Int32) (Syntax: 'result')
                      Right: 
                        ILocalReferenceOperation: x (OperationKind.LocalReference, Type: System.Int32) (Syntax: 'x')

            Next (Regular) Block[B2]
                Leaving: {R2}
    }
}

Block[B4] - Exit
    Predecessors: [B2]
    Statements (0)
";
            VerifyFlowGraphAndDiagnosticsForTest<BlockSyntax>(source + SpanSource, expectedFlowGraph, expectedDiagnostics, TestOptions.ReleaseDll.WithAllowUnsafe(true));
        }

        [CompilerTrait(CompilerFeature.IOperation, CompilerFeature.Dataflow)]
        [Fact]
        public void ForEachFlow_08()
        {
            string source = @"
public class MyClass
{
    void M(in System.Span<int> e, int result)
    /*<bind>*/{
        foreach (ref var x in e)
        { 
            result = x;
        }
    }/*</bind>*/
}
";
            var expectedDiagnostics = DiagnosticDescription.None;

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
            IFlowCaptureOperation: 0 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'e')
              Value: 
                IInvocationOperation ( System.Span<System.Int32>.Enumerator System.Span<System.Int32>.GetEnumerator()) (OperationKind.Invocation, Type: System.Span<System.Int32>.Enumerator, IsImplicit) (Syntax: 'e')
                  Instance Receiver: 
                    IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.Span<System.Int32>, IsImplicit) (Syntax: 'e')
                      Conversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                        (Identity)
                      Operand: 
                        IParameterReferenceOperation: e (OperationKind.ParameterReference, Type: System.Span<System.Int32>) (Syntax: 'e')
                  Arguments(0)

        Next (Regular) Block[B2]
    Block[B2] - Block
        Predecessors: [B1] [B3]
        Statements (0)
        Jump if False (Regular) to Block[B4]
            IInvocationOperation ( System.Boolean System.Span<System.Int32>.Enumerator.MoveNext()) (OperationKind.Invocation, Type: System.Boolean, IsImplicit) (Syntax: 'e')
              Instance Receiver: 
                IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: System.Span<System.Int32>.Enumerator, IsImplicit) (Syntax: 'e')
              Arguments(0)
            Leaving: {R1}

        Next (Regular) Block[B3]
            Entering: {R2}

    .locals {R2}
    {
        Locals: [System.Int32 x]
        Block[B3] - Block
            Predecessors: [B2]
            Statements (2)
                ISimpleAssignmentOperation (IsRef) (OperationKind.SimpleAssignment, Type: null, IsImplicit) (Syntax: 'var')
                  Left: 
                    ILocalReferenceOperation: x (IsDeclaration: True) (OperationKind.LocalReference, Type: System.Int32, IsImplicit) (Syntax: 'var')
                  Right: 
                    IPropertyReferenceOperation: ref System.Int32 System.Span<System.Int32>.Enumerator.Current { get; } (OperationKind.PropertyReference, Type: System.Int32, IsImplicit) (Syntax: 'var')
                      Instance Receiver: 
                        IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: System.Span<System.Int32>.Enumerator, IsImplicit) (Syntax: 'e')

                IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'result = x;')
                  Expression: 
                    ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Int32) (Syntax: 'result = x')
                      Left: 
                        IParameterReferenceOperation: result (OperationKind.ParameterReference, Type: System.Int32) (Syntax: 'result')
                      Right: 
                        ILocalReferenceOperation: x (OperationKind.LocalReference, Type: System.Int32) (Syntax: 'x')

            Next (Regular) Block[B2]
                Leaving: {R2}
    }
}

Block[B4] - Exit
    Predecessors: [B2]
    Statements (0)
";
            VerifyFlowGraphAndDiagnosticsForTest<BlockSyntax>(source + SpanSource, expectedFlowGraph, expectedDiagnostics, TestOptions.ReleaseDll.WithAllowUnsafe(true));
        }

        [CompilerTrait(CompilerFeature.IOperation, CompilerFeature.Dataflow)]
        [Fact]
        public void ForEachFlow_09()
        {
            string source = @"
public class MyClass
{
    void M(in System.ReadOnlySpan<int> e, int result)
    /*<bind>*/{
        foreach (var x in e)
        { 
            result = x;
        }
    }/*</bind>*/
}
";
            var expectedDiagnostics = DiagnosticDescription.None;

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
            IFlowCaptureOperation: 0 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'e')
              Value: 
                IInvocationOperation ( System.ReadOnlySpan<System.Int32>.Enumerator System.ReadOnlySpan<System.Int32>.GetEnumerator()) (OperationKind.Invocation, Type: System.ReadOnlySpan<System.Int32>.Enumerator, IsImplicit) (Syntax: 'e')
                  Instance Receiver: 
                    IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.ReadOnlySpan<System.Int32>, IsImplicit) (Syntax: 'e')
                      Conversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                        (Identity)
                      Operand: 
                        IParameterReferenceOperation: e (OperationKind.ParameterReference, Type: System.ReadOnlySpan<System.Int32>) (Syntax: 'e')
                  Arguments(0)

        Next (Regular) Block[B2]
    Block[B2] - Block
        Predecessors: [B1] [B3]
        Statements (0)
        Jump if False (Regular) to Block[B4]
            IInvocationOperation ( System.Boolean System.ReadOnlySpan<System.Int32>.Enumerator.MoveNext()) (OperationKind.Invocation, Type: System.Boolean, IsImplicit) (Syntax: 'e')
              Instance Receiver: 
                IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: System.ReadOnlySpan<System.Int32>.Enumerator, IsImplicit) (Syntax: 'e')
              Arguments(0)
            Leaving: {R1}

        Next (Regular) Block[B3]
            Entering: {R2}

    .locals {R2}
    {
        Locals: [System.Int32 x]
        Block[B3] - Block
            Predecessors: [B2]
            Statements (2)
                ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: null, IsImplicit) (Syntax: 'var')
                  Left: 
                    ILocalReferenceOperation: x (IsDeclaration: True) (OperationKind.LocalReference, Type: System.Int32, IsImplicit) (Syntax: 'var')
                  Right: 
                    IPropertyReferenceOperation: ref readonly modreq(System.Runtime.InteropServices.InAttribute) System.Int32 System.ReadOnlySpan<System.Int32>.Enumerator.Current { get; } (OperationKind.PropertyReference, Type: System.Int32, IsImplicit) (Syntax: 'var')
                      Instance Receiver: 
                        IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: System.ReadOnlySpan<System.Int32>.Enumerator, IsImplicit) (Syntax: 'e')

                IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'result = x;')
                  Expression: 
                    ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Int32) (Syntax: 'result = x')
                      Left: 
                        IParameterReferenceOperation: result (OperationKind.ParameterReference, Type: System.Int32) (Syntax: 'result')
                      Right: 
                        ILocalReferenceOperation: x (OperationKind.LocalReference, Type: System.Int32) (Syntax: 'x')

            Next (Regular) Block[B2]
                Leaving: {R2}
    }
}

Block[B4] - Exit
    Predecessors: [B2]
    Statements (0)
";
            VerifyFlowGraphAndDiagnosticsForTest<BlockSyntax>(source + SpanSource, expectedFlowGraph, expectedDiagnostics, TestOptions.ReleaseDll.WithAllowUnsafe(true));
        }

        [CompilerTrait(CompilerFeature.IOperation, CompilerFeature.Dataflow)]
        [Fact]
        public void ForEachFlow_10()
        {
            string source = @"
public class MyClass
{
    void M(in System.ReadOnlySpan<int> e, int result)
    /*<bind>*/{
        foreach (ref readonly var x in e)
        { 
            result = x;
        }
    }/*</bind>*/
}
";
            var expectedDiagnostics = DiagnosticDescription.None;

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
            IFlowCaptureOperation: 0 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'e')
              Value: 
                IInvocationOperation ( System.ReadOnlySpan<System.Int32>.Enumerator System.ReadOnlySpan<System.Int32>.GetEnumerator()) (OperationKind.Invocation, Type: System.ReadOnlySpan<System.Int32>.Enumerator, IsImplicit) (Syntax: 'e')
                  Instance Receiver: 
                    IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.ReadOnlySpan<System.Int32>, IsImplicit) (Syntax: 'e')
                      Conversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                        (Identity)
                      Operand: 
                        IParameterReferenceOperation: e (OperationKind.ParameterReference, Type: System.ReadOnlySpan<System.Int32>) (Syntax: 'e')
                  Arguments(0)

        Next (Regular) Block[B2]
    Block[B2] - Block
        Predecessors: [B1] [B3]
        Statements (0)
        Jump if False (Regular) to Block[B4]
            IInvocationOperation ( System.Boolean System.ReadOnlySpan<System.Int32>.Enumerator.MoveNext()) (OperationKind.Invocation, Type: System.Boolean, IsImplicit) (Syntax: 'e')
              Instance Receiver: 
                IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: System.ReadOnlySpan<System.Int32>.Enumerator, IsImplicit) (Syntax: 'e')
              Arguments(0)
            Leaving: {R1}

        Next (Regular) Block[B3]
            Entering: {R2}

    .locals {R2}
    {
        Locals: [System.Int32 x]
        Block[B3] - Block
            Predecessors: [B2]
            Statements (2)
                ISimpleAssignmentOperation (IsRef) (OperationKind.SimpleAssignment, Type: null, IsImplicit) (Syntax: 'var')
                  Left: 
                    ILocalReferenceOperation: x (IsDeclaration: True) (OperationKind.LocalReference, Type: System.Int32, IsImplicit) (Syntax: 'var')
                  Right: 
                    IPropertyReferenceOperation: ref readonly modreq(System.Runtime.InteropServices.InAttribute) System.Int32 System.ReadOnlySpan<System.Int32>.Enumerator.Current { get; } (OperationKind.PropertyReference, Type: System.Int32, IsImplicit) (Syntax: 'var')
                      Instance Receiver: 
                        IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: System.ReadOnlySpan<System.Int32>.Enumerator, IsImplicit) (Syntax: 'e')

                IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'result = x;')
                  Expression: 
                    ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Int32) (Syntax: 'result = x')
                      Left: 
                        IParameterReferenceOperation: result (OperationKind.ParameterReference, Type: System.Int32) (Syntax: 'result')
                      Right: 
                        ILocalReferenceOperation: x (OperationKind.LocalReference, Type: System.Int32) (Syntax: 'x')

            Next (Regular) Block[B2]
                Leaving: {R2}
    }
}

Block[B4] - Exit
    Predecessors: [B2]
    Statements (0)
";
            VerifyFlowGraphAndDiagnosticsForTest<BlockSyntax>(source + SpanSource, expectedFlowGraph, expectedDiagnostics, TestOptions.ReleaseDll.WithAllowUnsafe(true));
        }

        [CompilerTrait(CompilerFeature.IOperation, CompilerFeature.Dataflow)]
        [Fact]
        public void ForEachFlow_11()
        {
            string source = @"
public class MyClass
{
    void M(dynamic e, int result)
    /*<bind>*/{
        foreach (var x in e)
        { 
            result = x;
        }
    }/*</bind>*/
}
";
            var expectedDiagnostics = DiagnosticDescription.None;

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
            IFlowCaptureOperation: 0 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'e')
              Value: 
                IInvocationOperation (virtual System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()) (OperationKind.Invocation, Type: System.Collections.IEnumerator, IsImplicit) (Syntax: 'e')
                  Instance Receiver: 
                    IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.Collections.IEnumerable, IsImplicit) (Syntax: 'e')
                      Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                        (ImplicitDynamic)
                      Operand: 
                        IParameterReferenceOperation: e (OperationKind.ParameterReference, Type: dynamic) (Syntax: 'e')
                  Arguments(0)

        Next (Regular) Block[B2]
            Entering: {R2} {R3}

    .try {R2, R3}
    {
        Block[B2] - Block
            Predecessors: [B1] [B3]
            Statements (0)
            Jump if False (Regular) to Block[B7]
                IInvocationOperation (virtual System.Boolean System.Collections.IEnumerator.MoveNext()) (OperationKind.Invocation, Type: System.Boolean, IsImplicit) (Syntax: 'e')
                  Instance Receiver: 
                    IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: System.Collections.IEnumerator, IsImplicit) (Syntax: 'e')
                  Arguments(0)
                Finalizing: {R5}
                Leaving: {R3} {R2} {R1}

            Next (Regular) Block[B3]
                Entering: {R4}

        .locals {R4}
        {
            Locals: [dynamic x]
            Block[B3] - Block
                Predecessors: [B2]
                Statements (2)
                    ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: null, IsImplicit) (Syntax: 'var')
                      Left: 
                        ILocalReferenceOperation: x (IsDeclaration: True) (OperationKind.LocalReference, Type: dynamic, IsImplicit) (Syntax: 'var')
                      Right: 
                        IPropertyReferenceOperation: System.Object System.Collections.IEnumerator.Current { get; } (OperationKind.PropertyReference, Type: System.Object, IsImplicit) (Syntax: 'var')
                          Instance Receiver: 
                            IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: System.Collections.IEnumerator, IsImplicit) (Syntax: 'e')

                    IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'result = x;')
                      Expression: 
                        ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Int32) (Syntax: 'result = x')
                          Left: 
                            IParameterReferenceOperation: result (OperationKind.ParameterReference, Type: System.Int32) (Syntax: 'result')
                          Right: 
                            IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.Int32, IsImplicit) (Syntax: 'x')
                              Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                                (ImplicitDynamic)
                              Operand: 
                                ILocalReferenceOperation: x (OperationKind.LocalReference, Type: dynamic) (Syntax: 'x')

                Next (Regular) Block[B2]
                    Leaving: {R4}
        }
    }
    .finally {R5}
    {
        CaptureIds: [1]
        Block[B4] - Block
            Predecessors (0)
            Statements (1)
                IFlowCaptureOperation: 1 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'e')
                  Value: 
                    IConversionOperation (TryCast: True, Unchecked) (OperationKind.Conversion, Type: System.IDisposable, IsImplicit) (Syntax: 'e')
                      Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: True, IsUserDefined: False) (MethodSymbol: null)
                        (ExplicitReference)
                      Operand: 
                        IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: System.Collections.IEnumerator, IsImplicit) (Syntax: 'e')

            Jump if True (Regular) to Block[B6]
                IIsNullOperation (OperationKind.IsNull, Type: System.Boolean, IsImplicit) (Syntax: 'e')
                  Operand: 
                    IFlowCaptureReferenceOperation: 1 (OperationKind.FlowCaptureReference, Type: System.IDisposable, IsImplicit) (Syntax: 'e')

            Next (Regular) Block[B5]
        Block[B5] - Block
            Predecessors: [B4]
            Statements (1)
                IInvocationOperation (virtual void System.IDisposable.Dispose()) (OperationKind.Invocation, Type: System.Void, IsImplicit) (Syntax: 'e')
                  Instance Receiver: 
                    IFlowCaptureReferenceOperation: 1 (OperationKind.FlowCaptureReference, Type: System.IDisposable, IsImplicit) (Syntax: 'e')
                  Arguments(0)

            Next (Regular) Block[B6]
        Block[B6] - Block
            Predecessors: [B4] [B5]
            Statements (0)
            Next (StructuredExceptionHandling) Block[null]
    }
}

Block[B7] - Exit
    Predecessors: [B2]
    Statements (0)
";
            VerifyFlowGraphAndDiagnosticsForTest<BlockSyntax>(source, expectedFlowGraph, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation, CompilerFeature.Dataflow)]
        [Fact]
        public void ForEachFlow_12()
        {
            string source = @"
public class MyClass
{
    void M(MyClass e, int result)
    /*<bind>*/{
        foreach (var x in e)
        { 
            result = x;
        }
    }/*</bind>*/
}
";
            var expectedDiagnostics = new[] {
                // file.cs(6,27): error CS1579: foreach statement cannot operate on variables of type 'MyClass' because 'MyClass' does not contain a public instance definition for 'GetEnumerator'
                //         foreach (var x in e)
                Diagnostic(ErrorCode.ERR_ForEachMissingMember, "e").WithArguments("MyClass", "GetEnumerator").WithLocation(6, 27)
            };

            string expectedFlowGraph = @"
Block[B0] - Entry
    Statements (0)
    Next (Regular) Block[B1]
Block[B1] - Block
    Predecessors: [B0]
    Statements (1)
        IInvalidOperation (OperationKind.Invalid, Type: null, IsInvalid, IsImplicit) (Syntax: 'e')
          Children(1):
              IParameterReferenceOperation: e (OperationKind.ParameterReference, Type: MyClass, IsInvalid) (Syntax: 'e')

    Next (Regular) Block[B2]
Block[B2] - Block
    Predecessors: [B1] [B3]
    Statements (0)
    Jump if False (Regular) to Block[B4]
        IInvalidOperation (OperationKind.Invalid, Type: System.Boolean, IsInvalid, IsImplicit) (Syntax: 'e')
          Children(1):
              IInvalidOperation (OperationKind.Invalid, Type: null, IsInvalid, IsImplicit) (Syntax: 'e')
                Children(0)

    Next (Regular) Block[B3]
        Entering: {R1}

.locals {R1}
{
    Locals: [var x]
    Block[B3] - Block
        Predecessors: [B2]
        Statements (2)
            ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: null, IsImplicit) (Syntax: 'var')
              Left: 
                ILocalReferenceOperation: x (IsDeclaration: True) (OperationKind.LocalReference, Type: var, IsImplicit) (Syntax: 'var')
              Right: 
                IInvalidOperation (OperationKind.Invalid, Type: null, IsInvalid, IsImplicit) (Syntax: 'e')
                  Children(1):
                      IInvalidOperation (OperationKind.Invalid, Type: null, IsInvalid, IsImplicit) (Syntax: 'e')
                        Children(0)

            IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'result = x;')
              Expression: 
                ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Int32) (Syntax: 'result = x')
                  Left: 
                    IParameterReferenceOperation: result (OperationKind.ParameterReference, Type: System.Int32) (Syntax: 'result')
                  Right: 
                    IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.Int32, IsImplicit) (Syntax: 'x')
                      Conversion: CommonConversion (Exists: False, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                        (NoConversion)
                      Operand: 
                        ILocalReferenceOperation: x (OperationKind.LocalReference, Type: var) (Syntax: 'x')

        Next (Regular) Block[B2]
            Leaving: {R1}
}

Block[B4] - Exit
    Predecessors: [B2]
    Statements (0)
";
            VerifyFlowGraphAndDiagnosticsForTest<BlockSyntax>(source, expectedFlowGraph, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation, CompilerFeature.Dataflow)]
        [Fact]
        public void ForEachFlow_13()
        {
            string source = @"
public class MyClass
{
    void M(MyClass[] a, int result)
    /*<bind>*/{
        foreach (var (x, y) in a) 
        { 
            result = x + y;
        }
    }/*</bind>*/
    public void Deconstruct(out int a, out int b)
    {
        throw null;
    }
}
";
            var expectedDiagnostics = DiagnosticDescription.None;

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
            IFlowCaptureOperation: 0 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'a')
              Value: 
                IInvocationOperation (virtual System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()) (OperationKind.Invocation, Type: System.Collections.IEnumerator, IsImplicit) (Syntax: 'a')
                  Instance Receiver: 
                    IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.Collections.IEnumerable, IsImplicit) (Syntax: 'a')
                      Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: True, IsUserDefined: False) (MethodSymbol: null)
                        (ImplicitReference)
                      Operand: 
                        IParameterReferenceOperation: a (OperationKind.ParameterReference, Type: MyClass[]) (Syntax: 'a')
                  Arguments(0)

        Next (Regular) Block[B2]
            Entering: {R2} {R3}

    .try {R2, R3}
    {
        Block[B2] - Block
            Predecessors: [B1] [B3]
            Statements (0)
            Jump if False (Regular) to Block[B7]
                IInvocationOperation (virtual System.Boolean System.Collections.IEnumerator.MoveNext()) (OperationKind.Invocation, Type: System.Boolean, IsImplicit) (Syntax: 'a')
                  Instance Receiver: 
                    IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: System.Collections.IEnumerator, IsImplicit) (Syntax: 'a')
                  Arguments(0)
                Finalizing: {R5}
                Leaving: {R3} {R2} {R1}

            Next (Regular) Block[B3]
                Entering: {R4}

        .locals {R4}
        {
            Locals: [System.Int32 x] [System.Int32 y]
            Block[B3] - Block
                Predecessors: [B2]
                Statements (2)
                    IDeconstructionAssignmentOperation (OperationKind.DeconstructionAssignment, Type: (System.Int32 x, System.Int32 y), IsImplicit) (Syntax: 'var (x, y)')
                      Left: 
                        IDeclarationExpressionOperation (OperationKind.DeclarationExpression, Type: (System.Int32 x, System.Int32 y)) (Syntax: 'var (x, y)')
                          ITupleOperation (OperationKind.Tuple, Type: (System.Int32 x, System.Int32 y)) (Syntax: '(x, y)')
                            NaturalType: (System.Int32 x, System.Int32 y)
                            Elements(2):
                                ILocalReferenceOperation: x (IsDeclaration: True) (OperationKind.LocalReference, Type: System.Int32) (Syntax: 'x')
                                ILocalReferenceOperation: y (IsDeclaration: True) (OperationKind.LocalReference, Type: System.Int32) (Syntax: 'y')
                      Right: 
                        IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: MyClass, IsImplicit) (Syntax: 'var (x, y)')
                          Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: True, IsUserDefined: False) (MethodSymbol: null)
                            (ExplicitReference)
                          Operand: 
                            IPropertyReferenceOperation: System.Object System.Collections.IEnumerator.Current { get; } (OperationKind.PropertyReference, Type: System.Object, IsImplicit) (Syntax: 'var (x, y)')
                              Instance Receiver: 
                                IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: System.Collections.IEnumerator, IsImplicit) (Syntax: 'a')

                    IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'result = x + y;')
                      Expression: 
                        ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Int32) (Syntax: 'result = x + y')
                          Left: 
                            IParameterReferenceOperation: result (OperationKind.ParameterReference, Type: System.Int32) (Syntax: 'result')
                          Right: 
                            IBinaryOperation (BinaryOperatorKind.Add) (OperationKind.Binary, Type: System.Int32) (Syntax: 'x + y')
                              Left: 
                                ILocalReferenceOperation: x (OperationKind.LocalReference, Type: System.Int32) (Syntax: 'x')
                              Right: 
                                ILocalReferenceOperation: y (OperationKind.LocalReference, Type: System.Int32) (Syntax: 'y')

                Next (Regular) Block[B2]
                    Leaving: {R4}
        }
    }
    .finally {R5}
    {
        CaptureIds: [1]
        Block[B4] - Block
            Predecessors (0)
            Statements (1)
                IFlowCaptureOperation: 1 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'a')
                  Value: 
                    IConversionOperation (TryCast: True, Unchecked) (OperationKind.Conversion, Type: System.IDisposable, IsImplicit) (Syntax: 'a')
                      Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: True, IsUserDefined: False) (MethodSymbol: null)
                        (ExplicitReference)
                      Operand: 
                        IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: System.Collections.IEnumerator, IsImplicit) (Syntax: 'a')

            Jump if True (Regular) to Block[B6]
                IIsNullOperation (OperationKind.IsNull, Type: System.Boolean, IsImplicit) (Syntax: 'a')
                  Operand: 
                    IFlowCaptureReferenceOperation: 1 (OperationKind.FlowCaptureReference, Type: System.IDisposable, IsImplicit) (Syntax: 'a')

            Next (Regular) Block[B5]
        Block[B5] - Block
            Predecessors: [B4]
            Statements (1)
                IInvocationOperation (virtual void System.IDisposable.Dispose()) (OperationKind.Invocation, Type: System.Void, IsImplicit) (Syntax: 'a')
                  Instance Receiver: 
                    IFlowCaptureReferenceOperation: 1 (OperationKind.FlowCaptureReference, Type: System.IDisposable, IsImplicit) (Syntax: 'a')
                  Arguments(0)

            Next (Regular) Block[B6]
        Block[B6] - Block
            Predecessors: [B4] [B5]
            Statements (0)
            Next (StructuredExceptionHandling) Block[null]
    }
}

Block[B7] - Exit
    Predecessors: [B2]
    Statements (0)
";
            VerifyFlowGraphAndDiagnosticsForTest<BlockSyntax>(source, expectedFlowGraph, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation, CompilerFeature.Dataflow)]
        [Fact]
        public void ForEachFlow_14()
        {
            string source = @"
public sealed class MyClass
{
    void M(int result)
    /*<bind>*/{
        foreach ((var x, var y) in this) 
        { 
            result = x + y;
        }
    }/*</bind>*/
    public void Deconstruct(out int a, out int b)
    {
        throw null;
    }
    public bool MoveNext() => false;
    public MyClass Current => null;
    public MyClass GetEnumerator() => throw null;
}
";
            var expectedDiagnostics = DiagnosticDescription.None;

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
            IFlowCaptureOperation: 0 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'this')
              Value: 
                IInvocationOperation ( MyClass MyClass.GetEnumerator()) (OperationKind.Invocation, Type: MyClass, IsImplicit) (Syntax: 'this')
                  Instance Receiver: 
                    IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: MyClass, IsImplicit) (Syntax: 'this')
                      Conversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                        (Identity)
                      Operand: 
                        IInstanceReferenceOperation (ReferenceKind: ContainingTypeInstance) (OperationKind.InstanceReference, Type: MyClass) (Syntax: 'this')
                  Arguments(0)

        Next (Regular) Block[B2]
    Block[B2] - Block
        Predecessors: [B1] [B3]
        Statements (0)
        Jump if False (Regular) to Block[B4]
            IInvocationOperation ( System.Boolean MyClass.MoveNext()) (OperationKind.Invocation, Type: System.Boolean, IsImplicit) (Syntax: 'this')
              Instance Receiver: 
                IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: MyClass, IsImplicit) (Syntax: 'this')
              Arguments(0)
            Leaving: {R1}

        Next (Regular) Block[B3]
            Entering: {R2}

    .locals {R2}
    {
        Locals: [System.Int32 x] [System.Int32 y]
        Block[B3] - Block
            Predecessors: [B2]
            Statements (2)
                IDeconstructionAssignmentOperation (OperationKind.DeconstructionAssignment, Type: (System.Int32 x, System.Int32 y), IsImplicit) (Syntax: '(var x, var y)')
                  Left: 
                    ITupleOperation (OperationKind.Tuple, Type: (System.Int32 x, System.Int32 y)) (Syntax: '(var x, var y)')
                      NaturalType: (System.Int32 x, System.Int32 y)
                      Elements(2):
                          IDeclarationExpressionOperation (OperationKind.DeclarationExpression, Type: System.Int32) (Syntax: 'var x')
                            ILocalReferenceOperation: x (IsDeclaration: True) (OperationKind.LocalReference, Type: System.Int32) (Syntax: 'x')
                          IDeclarationExpressionOperation (OperationKind.DeclarationExpression, Type: System.Int32) (Syntax: 'var y')
                            ILocalReferenceOperation: y (IsDeclaration: True) (OperationKind.LocalReference, Type: System.Int32) (Syntax: 'y')
                  Right: 
                    IPropertyReferenceOperation: MyClass MyClass.Current { get; } (OperationKind.PropertyReference, Type: MyClass, IsImplicit) (Syntax: '(var x, var y)')
                      Instance Receiver: 
                        IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: MyClass, IsImplicit) (Syntax: 'this')

                IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'result = x + y;')
                  Expression: 
                    ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Int32) (Syntax: 'result = x + y')
                      Left: 
                        IParameterReferenceOperation: result (OperationKind.ParameterReference, Type: System.Int32) (Syntax: 'result')
                      Right: 
                        IBinaryOperation (BinaryOperatorKind.Add) (OperationKind.Binary, Type: System.Int32) (Syntax: 'x + y')
                          Left: 
                            ILocalReferenceOperation: x (OperationKind.LocalReference, Type: System.Int32) (Syntax: 'x')
                          Right: 
                            ILocalReferenceOperation: y (OperationKind.LocalReference, Type: System.Int32) (Syntax: 'y')

            Next (Regular) Block[B2]
                Leaving: {R2}
    }
}

Block[B4] - Exit
    Predecessors: [B2]
    Statements (0)
";
            VerifyFlowGraphAndDiagnosticsForTest<BlockSyntax>(source, expectedFlowGraph, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation, CompilerFeature.Dataflow)]
        [Fact]
        public void ForEachFlow_15()
        {
            string source = @"
public sealed class MyClass
{
    void M(int result)
    /*<bind>*/{
        foreach (M2(out var x) in this) 
        { 
            result = x;
        }
    }/*</bind>*/
    public bool MoveNext() => false;
    public MyClass Current => null;
    public MyClass GetEnumerator() => throw null;
    static void M2(out int x)
    {
        x = 0;
    }
}
";
            var expectedDiagnostics = new[] {
                // file.cs(6,32): error CS0230: Type and identifier are both required in a foreach statement
                //         foreach (M2(out var x) in this) 
                Diagnostic(ErrorCode.ERR_BadForeachDecl, "in").WithLocation(6, 32)
            };

            string expectedFlowGraph = @"
Block[B0] - Entry
    Statements (0)
    Next (Regular) Block[B1]
Block[B1] - Block
    Predecessors: [B0]
    Statements (1)
        IInvalidOperation (OperationKind.Invalid, Type: null, IsImplicit) (Syntax: 'this')
          Children(1):
              IInstanceReferenceOperation (ReferenceKind: ContainingTypeInstance) (OperationKind.InstanceReference, Type: MyClass) (Syntax: 'this')

    Next (Regular) Block[B2]
Block[B2] - Block
    Predecessors: [B1] [B3]
    Statements (0)
    Jump if False (Regular) to Block[B4]
        IInvalidOperation (OperationKind.Invalid, Type: System.Boolean, IsImplicit) (Syntax: 'this')
          Children(1):
              IInvalidOperation (OperationKind.Invalid, Type: null, IsImplicit) (Syntax: 'this')
                Children(0)

    Next (Regular) Block[B3]
        Entering: {R1}

.locals {R1}
{
    Locals: [System.Int32 x]
    Block[B3] - Block
        Predecessors: [B2]
        Statements (2)
            ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Void, IsImplicit) (Syntax: 'M2(out var x)')
              Left: 
                IInvocationOperation (void MyClass.M2(out System.Int32 x)) (OperationKind.Invocation, Type: System.Void) (Syntax: 'M2(out var x)')
                  Instance Receiver: 
                    null
                  Arguments(1):
                      IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: x) (OperationKind.Argument, Type: null) (Syntax: 'out var x')
                        IDeclarationExpressionOperation (OperationKind.DeclarationExpression, Type: System.Int32) (Syntax: 'var x')
                          ILocalReferenceOperation: x (IsDeclaration: True) (OperationKind.LocalReference, Type: System.Int32) (Syntax: 'x')
                        InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                        OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
              Right: 
                IInvalidOperation (OperationKind.Invalid, Type: null, IsImplicit) (Syntax: 'this')
                  Children(1):
                      IInvalidOperation (OperationKind.Invalid, Type: null, IsImplicit) (Syntax: 'this')
                        Children(0)

            IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'result = x;')
              Expression: 
                ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Int32) (Syntax: 'result = x')
                  Left: 
                    IParameterReferenceOperation: result (OperationKind.ParameterReference, Type: System.Int32) (Syntax: 'result')
                  Right: 
                    ILocalReferenceOperation: x (OperationKind.LocalReference, Type: System.Int32) (Syntax: 'x')

        Next (Regular) Block[B2]
            Leaving: {R1}
}

Block[B4] - Exit
    Predecessors: [B2]
    Statements (0)
";
            VerifyFlowGraphAndDiagnosticsForTest<BlockSyntax>(source, expectedFlowGraph, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation, CompilerFeature.Dataflow)]
        [Fact]
        public void ForEachFlow_16()
        {
            string source = @"
public class MyClass
{
    void M(System.Collections.IEnumerable e, object result)
    /*<bind>*/{
        foreach (var x in e)
        { 
            result = x;
        }
    }/*</bind>*/
}
";
            var expectedDiagnostics = DiagnosticDescription.None;

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
            IFlowCaptureOperation: 0 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'e')
              Value: 
                IInvocationOperation (virtual System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()) (OperationKind.Invocation, Type: System.Collections.IEnumerator, IsImplicit) (Syntax: 'e')
                  Instance Receiver: 
                    IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.Collections.IEnumerable, IsImplicit) (Syntax: 'e')
                      Conversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                        (Identity)
                      Operand: 
                        IParameterReferenceOperation: e (OperationKind.ParameterReference, Type: System.Collections.IEnumerable) (Syntax: 'e')
                  Arguments(0)

        Next (Regular) Block[B2]
            Entering: {R2} {R3}

    .try {R2, R3}
    {
        Block[B2] - Block
            Predecessors: [B1] [B3]
            Statements (0)
            Jump if False (Regular) to Block[B7]
                IInvocationOperation (virtual System.Boolean System.Collections.IEnumerator.MoveNext()) (OperationKind.Invocation, Type: System.Boolean, IsImplicit) (Syntax: 'e')
                  Instance Receiver: 
                    IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: System.Collections.IEnumerator, IsImplicit) (Syntax: 'e')
                  Arguments(0)
                Finalizing: {R5}
                Leaving: {R3} {R2} {R1}

            Next (Regular) Block[B3]
                Entering: {R4}

        .locals {R4}
        {
            Locals: [System.Object x]
            Block[B3] - Block
                Predecessors: [B2]
                Statements (2)
                    ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: null, IsImplicit) (Syntax: 'var')
                      Left: 
                        ILocalReferenceOperation: x (IsDeclaration: True) (OperationKind.LocalReference, Type: System.Object, IsImplicit) (Syntax: 'var')
                      Right: 
                        IPropertyReferenceOperation: System.Object System.Collections.IEnumerator.Current { get; } (OperationKind.PropertyReference, Type: System.Object, IsImplicit) (Syntax: 'var')
                          Instance Receiver: 
                            IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: System.Collections.IEnumerator, IsImplicit) (Syntax: 'e')

                    IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'result = x;')
                      Expression: 
                        ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Object) (Syntax: 'result = x')
                          Left: 
                            IParameterReferenceOperation: result (OperationKind.ParameterReference, Type: System.Object) (Syntax: 'result')
                          Right: 
                            ILocalReferenceOperation: x (OperationKind.LocalReference, Type: System.Object) (Syntax: 'x')

                Next (Regular) Block[B2]
                    Leaving: {R4}
        }
    }
    .finally {R5}
    {
        CaptureIds: [1]
        Block[B4] - Block
            Predecessors (0)
            Statements (1)
                IFlowCaptureOperation: 1 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'e')
                  Value: 
                    IConversionOperation (TryCast: True, Unchecked) (OperationKind.Conversion, Type: System.IDisposable, IsImplicit) (Syntax: 'e')
                      Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: True, IsUserDefined: False) (MethodSymbol: null)
                        (ExplicitReference)
                      Operand: 
                        IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: System.Collections.IEnumerator, IsImplicit) (Syntax: 'e')

            Jump if True (Regular) to Block[B6]
                IIsNullOperation (OperationKind.IsNull, Type: System.Boolean, IsImplicit) (Syntax: 'e')
                  Operand: 
                    IFlowCaptureReferenceOperation: 1 (OperationKind.FlowCaptureReference, Type: System.IDisposable, IsImplicit) (Syntax: 'e')

            Next (Regular) Block[B5]
        Block[B5] - Block
            Predecessors: [B4]
            Statements (1)
                IInvocationOperation (virtual void System.IDisposable.Dispose()) (OperationKind.Invocation, Type: System.Void, IsImplicit) (Syntax: 'e')
                  Instance Receiver: 
                    IFlowCaptureReferenceOperation: 1 (OperationKind.FlowCaptureReference, Type: System.IDisposable, IsImplicit) (Syntax: 'e')
                  Arguments(0)

            Next (Regular) Block[B6]
        Block[B6] - Block
            Predecessors: [B4] [B5]
            Statements (0)
            Next (StructuredExceptionHandling) Block[null]
    }
}

Block[B7] - Exit
    Predecessors: [B2]
    Statements (0)
";
            VerifyFlowGraphAndDiagnosticsForTest<BlockSyntax>(source, expectedFlowGraph, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation, CompilerFeature.Dataflow)]
        [Fact]
        public void ForEachFlow_17()
        {
            string source = @"
public class MyClass
{
    void M(int[] a, long result)
    /*<bind>*/{
        foreach (long x in a) 
        { 
            result = x;
        }
    }/*</bind>*/
}
";
            var expectedDiagnostics = DiagnosticDescription.None;

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
            IFlowCaptureOperation: 0 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'a')
              Value: 
                IInvocationOperation (virtual System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()) (OperationKind.Invocation, Type: System.Collections.IEnumerator, IsImplicit) (Syntax: 'a')
                  Instance Receiver: 
                    IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.Collections.IEnumerable, IsImplicit) (Syntax: 'a')
                      Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: True, IsUserDefined: False) (MethodSymbol: null)
                        (ImplicitReference)
                      Operand: 
                        IParameterReferenceOperation: a (OperationKind.ParameterReference, Type: System.Int32[]) (Syntax: 'a')
                  Arguments(0)

        Next (Regular) Block[B2]
            Entering: {R2} {R3}

    .try {R2, R3}
    {
        Block[B2] - Block
            Predecessors: [B1] [B3]
            Statements (0)
            Jump if False (Regular) to Block[B7]
                IInvocationOperation (virtual System.Boolean System.Collections.IEnumerator.MoveNext()) (OperationKind.Invocation, Type: System.Boolean, IsImplicit) (Syntax: 'a')
                  Instance Receiver: 
                    IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: System.Collections.IEnumerator, IsImplicit) (Syntax: 'a')
                  Arguments(0)
                Finalizing: {R5}
                Leaving: {R3} {R2} {R1}

            Next (Regular) Block[B3]
                Entering: {R4}

        .locals {R4}
        {
            Locals: [System.Int64 x]
            Block[B3] - Block
                Predecessors: [B2]
                Statements (2)
                    ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: null, IsImplicit) (Syntax: 'long')
                      Left: 
                        ILocalReferenceOperation: x (IsDeclaration: True) (OperationKind.LocalReference, Type: System.Int64, IsImplicit) (Syntax: 'long')
                      Right: 
                        IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.Int64, IsImplicit) (Syntax: 'long')
                          Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: True, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                            (ImplicitNumeric)
                          Operand: 
                            IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.Int32, IsImplicit) (Syntax: 'long')
                              Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                                (Unboxing)
                              Operand: 
                                IPropertyReferenceOperation: System.Object System.Collections.IEnumerator.Current { get; } (OperationKind.PropertyReference, Type: System.Object, IsImplicit) (Syntax: 'long')
                                  Instance Receiver: 
                                    IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: System.Collections.IEnumerator, IsImplicit) (Syntax: 'a')

                    IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'result = x;')
                      Expression: 
                        ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Int64) (Syntax: 'result = x')
                          Left: 
                            IParameterReferenceOperation: result (OperationKind.ParameterReference, Type: System.Int64) (Syntax: 'result')
                          Right: 
                            ILocalReferenceOperation: x (OperationKind.LocalReference, Type: System.Int64) (Syntax: 'x')

                Next (Regular) Block[B2]
                    Leaving: {R4}
        }
    }
    .finally {R5}
    {
        CaptureIds: [1]
        Block[B4] - Block
            Predecessors (0)
            Statements (1)
                IFlowCaptureOperation: 1 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'a')
                  Value: 
                    IConversionOperation (TryCast: True, Unchecked) (OperationKind.Conversion, Type: System.IDisposable, IsImplicit) (Syntax: 'a')
                      Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: True, IsUserDefined: False) (MethodSymbol: null)
                        (ExplicitReference)
                      Operand: 
                        IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: System.Collections.IEnumerator, IsImplicit) (Syntax: 'a')

            Jump if True (Regular) to Block[B6]
                IIsNullOperation (OperationKind.IsNull, Type: System.Boolean, IsImplicit) (Syntax: 'a')
                  Operand: 
                    IFlowCaptureReferenceOperation: 1 (OperationKind.FlowCaptureReference, Type: System.IDisposable, IsImplicit) (Syntax: 'a')

            Next (Regular) Block[B5]
        Block[B5] - Block
            Predecessors: [B4]
            Statements (1)
                IInvocationOperation (virtual void System.IDisposable.Dispose()) (OperationKind.Invocation, Type: System.Void, IsImplicit) (Syntax: 'a')
                  Instance Receiver: 
                    IFlowCaptureReferenceOperation: 1 (OperationKind.FlowCaptureReference, Type: System.IDisposable, IsImplicit) (Syntax: 'a')
                  Arguments(0)

            Next (Regular) Block[B6]
        Block[B6] - Block
            Predecessors: [B4] [B5]
            Statements (0)
            Next (StructuredExceptionHandling) Block[null]
    }
}

Block[B7] - Exit
    Predecessors: [B2]
    Statements (0)
";
            VerifyFlowGraphAndDiagnosticsForTest<BlockSyntax>(source, expectedFlowGraph, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation, CompilerFeature.Dataflow)]
        [Fact]
        public void ForEachFlow_18()
        {
            string source = @"
public sealed class MyClass
{
    void M(bool result)
    /*<bind>*/{
        foreach (var x in this) 
        { 
            if (x) continue;
            result = x;
        }
    }/*</bind>*/
    public bool MoveNext() => false;
    public bool Current => false;
    public MyClass GetEnumerator() => throw null;
}
";
            var expectedDiagnostics = DiagnosticDescription.None;

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
            IFlowCaptureOperation: 0 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'this')
              Value: 
                IInvocationOperation ( MyClass MyClass.GetEnumerator()) (OperationKind.Invocation, Type: MyClass, IsImplicit) (Syntax: 'this')
                  Instance Receiver: 
                    IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: MyClass, IsImplicit) (Syntax: 'this')
                      Conversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                        (Identity)
                      Operand: 
                        IInstanceReferenceOperation (ReferenceKind: ContainingTypeInstance) (OperationKind.InstanceReference, Type: MyClass) (Syntax: 'this')
                  Arguments(0)

        Next (Regular) Block[B2]
    Block[B2] - Block
        Predecessors: [B1] [B3] [B4]
        Statements (0)
        Jump if False (Regular) to Block[B5]
            IInvocationOperation ( System.Boolean MyClass.MoveNext()) (OperationKind.Invocation, Type: System.Boolean, IsImplicit) (Syntax: 'this')
              Instance Receiver: 
                IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: MyClass, IsImplicit) (Syntax: 'this')
              Arguments(0)
            Leaving: {R1}

        Next (Regular) Block[B3]
            Entering: {R2}

    .locals {R2}
    {
        Locals: [System.Boolean x]
        Block[B3] - Block
            Predecessors: [B2]
            Statements (1)
                ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: null, IsImplicit) (Syntax: 'var')
                  Left: 
                    ILocalReferenceOperation: x (IsDeclaration: True) (OperationKind.LocalReference, Type: System.Boolean, IsImplicit) (Syntax: 'var')
                  Right: 
                    IPropertyReferenceOperation: System.Boolean MyClass.Current { get; } (OperationKind.PropertyReference, Type: System.Boolean, IsImplicit) (Syntax: 'var')
                      Instance Receiver: 
                        IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: MyClass, IsImplicit) (Syntax: 'this')

            Jump if False (Regular) to Block[B4]
                ILocalReferenceOperation: x (OperationKind.LocalReference, Type: System.Boolean) (Syntax: 'x')

            Next (Regular) Block[B2]
                Leaving: {R2}
        Block[B4] - Block
            Predecessors: [B3]
            Statements (1)
                IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'result = x;')
                  Expression: 
                    ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Boolean) (Syntax: 'result = x')
                      Left: 
                        IParameterReferenceOperation: result (OperationKind.ParameterReference, Type: System.Boolean) (Syntax: 'result')
                      Right: 
                        ILocalReferenceOperation: x (OperationKind.LocalReference, Type: System.Boolean) (Syntax: 'x')

            Next (Regular) Block[B2]
                Leaving: {R2}
    }
}

Block[B5] - Exit
    Predecessors: [B2]
    Statements (0)
";
            VerifyFlowGraphAndDiagnosticsForTest<BlockSyntax>(source, expectedFlowGraph, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation, CompilerFeature.Dataflow)]
        [Fact]
        public void ForEachFlow_19()
        {
            string source = @"
public sealed class MyClass
{
    void M(bool result)
    /*<bind>*/{
        foreach (var x in this) 
        { 
            if (x) break;
            result = x;
        }
    }/*</bind>*/
    public bool MoveNext() => false;
    public bool Current => false;
    public MyClass GetEnumerator() => throw null;
}
";
            var expectedDiagnostics = DiagnosticDescription.None;

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
            IFlowCaptureOperation: 0 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'this')
              Value: 
                IInvocationOperation ( MyClass MyClass.GetEnumerator()) (OperationKind.Invocation, Type: MyClass, IsImplicit) (Syntax: 'this')
                  Instance Receiver: 
                    IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: MyClass, IsImplicit) (Syntax: 'this')
                      Conversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                        (Identity)
                      Operand: 
                        IInstanceReferenceOperation (ReferenceKind: ContainingTypeInstance) (OperationKind.InstanceReference, Type: MyClass) (Syntax: 'this')
                  Arguments(0)

        Next (Regular) Block[B2]
    Block[B2] - Block
        Predecessors: [B1] [B4]
        Statements (0)
        Jump if False (Regular) to Block[B5]
            IInvocationOperation ( System.Boolean MyClass.MoveNext()) (OperationKind.Invocation, Type: System.Boolean, IsImplicit) (Syntax: 'this')
              Instance Receiver: 
                IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: MyClass, IsImplicit) (Syntax: 'this')
              Arguments(0)
            Leaving: {R1}

        Next (Regular) Block[B3]
            Entering: {R2}

    .locals {R2}
    {
        Locals: [System.Boolean x]
        Block[B3] - Block
            Predecessors: [B2]
            Statements (1)
                ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: null, IsImplicit) (Syntax: 'var')
                  Left: 
                    ILocalReferenceOperation: x (IsDeclaration: True) (OperationKind.LocalReference, Type: System.Boolean, IsImplicit) (Syntax: 'var')
                  Right: 
                    IPropertyReferenceOperation: System.Boolean MyClass.Current { get; } (OperationKind.PropertyReference, Type: System.Boolean, IsImplicit) (Syntax: 'var')
                      Instance Receiver: 
                        IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: MyClass, IsImplicit) (Syntax: 'this')

            Jump if False (Regular) to Block[B4]
                ILocalReferenceOperation: x (OperationKind.LocalReference, Type: System.Boolean) (Syntax: 'x')

            Next (Regular) Block[B5]
                Leaving: {R2} {R1}
        Block[B4] - Block
            Predecessors: [B3]
            Statements (1)
                IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'result = x;')
                  Expression: 
                    ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Boolean) (Syntax: 'result = x')
                      Left: 
                        IParameterReferenceOperation: result (OperationKind.ParameterReference, Type: System.Boolean) (Syntax: 'result')
                      Right: 
                        ILocalReferenceOperation: x (OperationKind.LocalReference, Type: System.Boolean) (Syntax: 'x')

            Next (Regular) Block[B2]
                Leaving: {R2}
    }
}

Block[B5] - Exit
    Predecessors: [B2] [B3]
    Statements (0)
";
            VerifyFlowGraphAndDiagnosticsForTest<BlockSyntax>(source, expectedFlowGraph, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation, CompilerFeature.Dataflow)]
        [Fact]
        public void ForEachFlow_26()
        {
            string source = @"
public sealed class MyClass
{
    void M(bool result, object a, object b)
    /*<bind>*/{
        foreach ((var x, (var y, var z), a ?? b) in this) 
        { 
        }
    }/*</bind>*/
    public bool MoveNext() => false;
    public (int a, (int, int) b, object c) Current => default;
    public MyClass GetEnumerator() => throw null;
}
";
            var expectedDiagnostics = new[] {
                // file.cs(6,42): error CS0131: The left-hand side of an assignment must be a variable, property or indexer
                //         foreach ((var x, (var y, var z), a ?? b) in this) 
                Diagnostic(ErrorCode.ERR_AssgLvalueExpected, "a ?? b").WithLocation(6, 42),
                // file.cs(6,18): error CS8186: A foreach loop must declare its iteration variables.
                //         foreach ((var x, (var y, var z), a ?? b) in this) 
                Diagnostic(ErrorCode.ERR_MustDeclareForeachIteration, "(var x, (var y, var z), a ?? b)").WithLocation(6, 18)
            };

            string expectedFlowGraph = @"
Block[B0] - Entry
    Statements (0)
    Next (Regular) Block[B1]
Block[B1] - Block
    Predecessors: [B0]
    Statements (1)
        IInvalidOperation (OperationKind.Invalid, Type: null, IsImplicit) (Syntax: 'this')
          Children(1):
              IInstanceReferenceOperation (ReferenceKind: ContainingTypeInstance) (OperationKind.InstanceReference, Type: MyClass) (Syntax: 'this')

    Next (Regular) Block[B2]
Block[B2] - Block
    Predecessors: [B1] [B6]
    Statements (0)
    Jump if False (Regular) to Block[B7]
        IInvalidOperation (OperationKind.Invalid, Type: System.Boolean, IsImplicit) (Syntax: 'this')
          Children(1):
              IInvalidOperation (OperationKind.Invalid, Type: null, IsImplicit) (Syntax: 'this')
                Children(0)

    Next (Regular) Block[B3]
        Entering: {R1} {R2}

.locals {R1}
{
    Locals: [System.Int32 x] [System.Int32 y] [System.Int32 z]
    CaptureIds: [1]
    .locals {R2}
    {
        CaptureIds: [0]
        Block[B3] - Block
            Predecessors: [B2]
            Statements (1)
                IFlowCaptureOperation: 0 (OperationKind.FlowCapture, Type: null, IsInvalid, IsImplicit) (Syntax: 'a')
                  Value: 
                    IParameterReferenceOperation: a (OperationKind.ParameterReference, Type: System.Object, IsInvalid) (Syntax: 'a')

            Jump if True (Regular) to Block[B5]
                IIsNullOperation (OperationKind.IsNull, Type: System.Boolean, IsInvalid, IsImplicit) (Syntax: 'a')
                  Operand: 
                    IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: System.Object, IsInvalid, IsImplicit) (Syntax: 'a')
                Leaving: {R2}

            Next (Regular) Block[B4]
        Block[B4] - Block
            Predecessors: [B3]
            Statements (1)
                IFlowCaptureOperation: 1 (OperationKind.FlowCapture, Type: null, IsInvalid, IsImplicit) (Syntax: 'a')
                  Value: 
                    IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: System.Object, IsInvalid, IsImplicit) (Syntax: 'a')

            Next (Regular) Block[B6]
                Leaving: {R2}
    }

    Block[B5] - Block
        Predecessors: [B3]
        Statements (1)
            IFlowCaptureOperation: 1 (OperationKind.FlowCapture, Type: null, IsInvalid, IsImplicit) (Syntax: 'b')
              Value: 
                IParameterReferenceOperation: b (OperationKind.ParameterReference, Type: System.Object, IsInvalid) (Syntax: 'b')

        Next (Regular) Block[B6]
    Block[B6] - Block
        Predecessors: [B4] [B5]
        Statements (1)
            IDeconstructionAssignmentOperation (OperationKind.DeconstructionAssignment, Type: (System.Int32 x, (System.Int32 y, System.Int32 z), System.Object), IsInvalid, IsImplicit) (Syntax: '(var x, (va ... z), a ?? b)')
              Left: 
                ITupleOperation (OperationKind.Tuple, Type: (System.Int32 x, (System.Int32 y, System.Int32 z), System.Object), IsInvalid) (Syntax: '(var x, (va ... z), a ?? b)')
                  NaturalType: (System.Int32 x, (System.Int32 y, System.Int32 z), System.Object)
                  Elements(3):
                      IDeclarationExpressionOperation (OperationKind.DeclarationExpression, Type: System.Int32, IsInvalid) (Syntax: 'var x')
                        ILocalReferenceOperation: x (IsDeclaration: True) (OperationKind.LocalReference, Type: System.Int32, IsInvalid) (Syntax: 'x')
                      ITupleOperation (OperationKind.Tuple, Type: (System.Int32 y, System.Int32 z), IsInvalid) (Syntax: '(var y, var z)')
                        NaturalType: (System.Int32 y, System.Int32 z)
                        Elements(2):
                            IDeclarationExpressionOperation (OperationKind.DeclarationExpression, Type: System.Int32, IsInvalid) (Syntax: 'var y')
                              ILocalReferenceOperation: y (IsDeclaration: True) (OperationKind.LocalReference, Type: System.Int32, IsInvalid) (Syntax: 'y')
                            IDeclarationExpressionOperation (OperationKind.DeclarationExpression, Type: System.Int32, IsInvalid) (Syntax: 'var z')
                              ILocalReferenceOperation: z (IsDeclaration: True) (OperationKind.LocalReference, Type: System.Int32, IsInvalid) (Syntax: 'z')
                      IInvalidOperation (OperationKind.Invalid, Type: System.Object, IsInvalid, IsImplicit) (Syntax: 'a ?? b')
                        Children(1):
                            IFlowCaptureReferenceOperation: 1 (OperationKind.FlowCaptureReference, Type: System.Object, IsInvalid, IsImplicit) (Syntax: 'a ?? b')
              Right: 
                IInvalidOperation (OperationKind.Invalid, Type: null, IsImplicit) (Syntax: 'this')
                  Children(1):
                      IInvalidOperation (OperationKind.Invalid, Type: null, IsImplicit) (Syntax: 'this')
                        Children(0)

        Next (Regular) Block[B2]
            Leaving: {R1}
}

Block[B7] - Exit
    Predecessors: [B2]
    Statements (0)
";
            VerifyFlowGraphAndDiagnosticsForTest<BlockSyntax>(source, expectedFlowGraph, expectedDiagnostics);
        }

        [Fact, CompilerTrait(CompilerFeature.IOperation, CompilerFeature.AsyncStreams)]
        [WorkItem(30362, "https://github.com/dotnet/roslyn/issues/30362")]
        public void IForEachLoopStatement_SimpleAwaitForEachLoop()
        {
            string source = @"
class Program
{
    static async System.Threading.Tasks.Task Main(System.Collections.Generic.IAsyncEnumerable<string> pets)
    {
        /*<bind>*/await foreach (string value in pets)
        {
            System.Console.WriteLine(value);
        }/*</bind>*/
    }
}
";
            string expectedOperationTree = @"
IForEachLoopOperation (LoopKind.ForEach, IsAsynchronous, Continue Label Id: 0, Exit Label Id: 1) (OperationKind.Loop, Type: null) (Syntax: 'await forea ... }')
  Locals: Local_1: System.String value
  LoopControlVariable: 
    IVariableDeclaratorOperation (Symbol: System.String value) (OperationKind.VariableDeclarator, Type: null) (Syntax: 'string')
      Initializer: 
        null
  Collection: 
    IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.Collections.Generic.IAsyncEnumerable<System.String>, IsImplicit) (Syntax: 'pets')
      Conversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
      Operand: 
        IParameterReferenceOperation: pets (OperationKind.ParameterReference, Type: System.Collections.Generic.IAsyncEnumerable<System.String>) (Syntax: 'pets')
  Body: 
    IBlockOperation (1 statements) (OperationKind.Block, Type: null) (Syntax: '{ ... }')
      IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'System.Cons ... ine(value);')
        Expression: 
          IInvocationOperation (void System.Console.WriteLine(System.String value)) (OperationKind.Invocation, Type: System.Void) (Syntax: 'System.Cons ... Line(value)')
            Instance Receiver: 
              null
            Arguments(1):
                IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: value) (OperationKind.Argument, Type: null) (Syntax: 'value')
                  ILocalReferenceOperation: value (OperationKind.LocalReference, Type: System.String) (Syntax: 'value')
                  InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                  OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
  NextVariables(0)
";

            VerifyOperationTreeForTest<ForEachStatementSyntax>(source + s_IAsyncEnumerable + s_ValueTask, expectedOperationTree);
        }

        [CompilerTrait(CompilerFeature.IOperation, CompilerFeature.Dataflow, CompilerFeature.AsyncStreams)]
        [Fact, WorkItem(30362, "https://github.com/dotnet/roslyn/issues/30362")]
        public void ForEachAwaitFlow_SimpleAwaitForEachLoop()
        {
            string source = @"
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System;
class Program
{
    static async Task Main(System.Collections.Generic.IAsyncEnumerable<string> pets)
    /*<bind>*/{
        await foreach (string value in pets)
        {
            System.Console.WriteLine(value);
        }
    }/*</bind>*/
}";

            var expectedDiagnostics = DiagnosticDescription.None;

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
            IFlowCaptureOperation: 0 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'pets')
              Value: 
                IInvocationOperation (virtual System.Collections.Generic.IAsyncEnumerator<System.String> System.Collections.Generic.IAsyncEnumerable<System.String>.GetAsyncEnumerator([System.Threading.CancellationToken token = default(System.Threading.CancellationToken)])) (OperationKind.Invocation, Type: System.Collections.Generic.IAsyncEnumerator<System.String>, IsImplicit) (Syntax: 'pets')
                  Instance Receiver: 
                    IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.Collections.Generic.IAsyncEnumerable<System.String>, IsImplicit) (Syntax: 'pets')
                      Conversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                        (Identity)
                      Operand: 
                        IParameterReferenceOperation: pets (OperationKind.ParameterReference, Type: System.Collections.Generic.IAsyncEnumerable<System.String>) (Syntax: 'pets')
                  Arguments(0)
        Next (Regular) Block[B2]
            Entering: {R2} {R3}
    .try {R2, R3}
    {
        Block[B2] - Block
            Predecessors: [B1] [B3]
            Statements (0)
            Jump if False (Regular) to Block[B7]
                IAwaitOperation (OperationKind.Await, Type: System.Boolean, IsImplicit) (Syntax: 'await forea ... }')
                  Expression: 
                    IInvocationOperation (virtual System.Threading.Tasks.ValueTask<System.Boolean> System.Collections.Generic.IAsyncEnumerator<System.String>.MoveNextAsync()) (OperationKind.Invocation, Type: System.Threading.Tasks.ValueTask<System.Boolean>, IsImplicit) (Syntax: 'pets')
                      Instance Receiver: 
                        IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: System.Collections.Generic.IAsyncEnumerator<System.String>, IsImplicit) (Syntax: 'pets')
                      Arguments(0)
                Finalizing: {R5}
                Leaving: {R3} {R2} {R1}
            Next (Regular) Block[B3]
                Entering: {R4}
        .locals {R4}
        {
            Locals: [System.String value]
            Block[B3] - Block
                Predecessors: [B2]
                Statements (2)
                    ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: null, IsImplicit) (Syntax: 'string')
                      Left: 
                        ILocalReferenceOperation: value (IsDeclaration: True) (OperationKind.LocalReference, Type: System.String, IsImplicit) (Syntax: 'string')
                      Right: 
                        IPropertyReferenceOperation: System.String System.Collections.Generic.IAsyncEnumerator<System.String>.Current { get; } (OperationKind.PropertyReference, Type: System.String, IsImplicit) (Syntax: 'string')
                          Instance Receiver: 
                            IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: System.Collections.Generic.IAsyncEnumerator<System.String>, IsImplicit) (Syntax: 'pets')
                    IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'System.Cons ... ine(value);')
                      Expression: 
                        IInvocationOperation (void System.Console.WriteLine(System.String value)) (OperationKind.Invocation, Type: System.Void) (Syntax: 'System.Cons ... Line(value)')
                          Instance Receiver: 
                            null
                          Arguments(1):
                              IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: value) (OperationKind.Argument, Type: null) (Syntax: 'value')
                                ILocalReferenceOperation: value (OperationKind.LocalReference, Type: System.String) (Syntax: 'value')
                                InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                                OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                Next (Regular) Block[B2]
                    Leaving: {R4}
        }
    }
    .finally {R5}
    {
        CaptureIds: [1]
        Block[B4] - Block
            Predecessors (0)
            Statements (1)
                IFlowCaptureOperation: 1 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'pets')
                  Value: 
                    IConversionOperation (TryCast: True, Unchecked) (OperationKind.Conversion, Type: System.IAsyncDisposable, IsImplicit) (Syntax: 'pets')
                      Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: True, IsUserDefined: False) (MethodSymbol: null)
                        (ImplicitReference)
                      Operand: 
                        IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: System.Collections.Generic.IAsyncEnumerator<System.String>, IsImplicit) (Syntax: 'pets')
            Jump if True (Regular) to Block[B6]
                IIsNullOperation (OperationKind.IsNull, Type: System.Boolean, IsImplicit) (Syntax: 'pets')
                  Operand: 
                    IFlowCaptureReferenceOperation: 1 (OperationKind.FlowCaptureReference, Type: System.IAsyncDisposable, IsImplicit) (Syntax: 'pets')
            Next (Regular) Block[B5]
        Block[B5] - Block
            Predecessors: [B4]
            Statements (1)
                IAwaitOperation (OperationKind.Await, Type: System.Void, IsImplicit) (Syntax: 'pets')
                  Expression: 
                    IInvocationOperation (virtual System.Threading.Tasks.ValueTask System.IAsyncDisposable.DisposeAsync()) (OperationKind.Invocation, Type: System.Threading.Tasks.ValueTask, IsImplicit) (Syntax: 'pets')
                      Instance Receiver: 
                        IFlowCaptureReferenceOperation: 1 (OperationKind.FlowCaptureReference, Type: System.IAsyncDisposable, IsImplicit) (Syntax: 'pets')
                      Arguments(0)
            Next (Regular) Block[B6]
        Block[B6] - Block
            Predecessors: [B4] [B5]
            Statements (0)
            Next (StructuredExceptionHandling) Block[null]
    }
}
Block[B7] - Exit
    Predecessors: [B2]
    Statements (0)
";
            VerifyFlowGraphAndDiagnosticsForTest<BlockSyntax>(source + s_IAsyncEnumerable + s_ValueTask, expectedFlowGraph, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation, CompilerFeature.Dataflow, CompilerFeature.AsyncStreams)]
        [Fact, WorkItem(30362, "https://github.com/dotnet/roslyn/issues/30362")]
        public void ForEachAwaitFlow_SimpleAwaitForEachLoop_MissingIAsyncEnumerableType()
        {
            string source = @"
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System;
class Program
{
    static async Task Main(System.Collections.Generic.IAsyncEnumerable<string> pets)
    /*<bind>*/{
        await foreach (string value in pets)
        {
            System.Console.WriteLine(value);
        }
    }/*</bind>*/
}";

            var expectedDiagnostics = new DiagnosticDescription[] {
                // file.cs(7,55): error CS0234: The type or namespace name 'IAsyncEnumerable<>' does not exist in the namespace 'System.Collections.Generic' (are you missing an assembly reference?)
                //     static async Task Main(System.Collections.Generic.IAsyncEnumerable<string> pets)
                Diagnostic(ErrorCode.ERR_DottedTypeNameNotFoundInNS, "IAsyncEnumerable<string>").WithArguments("IAsyncEnumerable<>", "System.Collections.Generic").WithLocation(7, 55)
            };

            string expectedFlowGraph = @"
Block[B0] - Entry
    Statements (0)
    Next (Regular) Block[B1]
Block[B1] - Block
    Predecessors: [B0]
    Statements (1)
        IInvalidOperation (OperationKind.Invalid, Type: null, IsImplicit) (Syntax: 'pets')
          Children(1):
              IParameterReferenceOperation: pets (OperationKind.ParameterReference, Type: System.Collections.Generic.IAsyncEnumerable<System.String>) (Syntax: 'pets')
    Next (Regular) Block[B2]
Block[B2] - Block
    Predecessors: [B1] [B3]
    Statements (0)
    Jump if False (Regular) to Block[B4]
        IInvalidOperation (OperationKind.Invalid, Type: System.Boolean, IsImplicit) (Syntax: 'pets')
          Children(1):
              IInvalidOperation (OperationKind.Invalid, Type: null, IsImplicit) (Syntax: 'pets')
                Children(0)
    Next (Regular) Block[B3]
        Entering: {R1}
.locals {R1}
{
    Locals: [System.String value]
    Block[B3] - Block
        Predecessors: [B2]
        Statements (2)
            ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: null, IsImplicit) (Syntax: 'string')
              Left: 
                ILocalReferenceOperation: value (IsDeclaration: True) (OperationKind.LocalReference, Type: System.String, IsImplicit) (Syntax: 'string')
              Right: 
                IInvalidOperation (OperationKind.Invalid, Type: null, IsImplicit) (Syntax: 'pets')
                  Children(1):
                      IInvalidOperation (OperationKind.Invalid, Type: null, IsImplicit) (Syntax: 'pets')
                        Children(0)
            IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'System.Cons ... ine(value);')
              Expression: 
                IInvocationOperation (void System.Console.WriteLine(System.String value)) (OperationKind.Invocation, Type: System.Void) (Syntax: 'System.Cons ... Line(value)')
                  Instance Receiver: 
                    null
                  Arguments(1):
                      IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: value) (OperationKind.Argument, Type: null) (Syntax: 'value')
                        ILocalReferenceOperation: value (OperationKind.LocalReference, Type: System.String) (Syntax: 'value')
                        InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                        OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
        Next (Regular) Block[B2]
            Leaving: {R1}
}
Block[B4] - Exit
    Predecessors: [B2]
    Statements (0)
";
            VerifyFlowGraphAndDiagnosticsForTest<BlockSyntax>(source, expectedFlowGraph, expectedDiagnostics);

            var expectedOperationTree = @"
IBlockOperation (1 statements) (OperationKind.Block, Type: null) (Syntax: '{ ... }')
  IForEachLoopOperation (LoopKind.ForEach, IsAsynchronous, Continue Label Id: 0, Exit Label Id: 1) (OperationKind.Loop, Type: null) (Syntax: 'await forea ... }')
    Locals: Local_1: System.String value
    LoopControlVariable:
      IVariableDeclaratorOperation (Symbol: System.String value) (OperationKind.VariableDeclarator, Type: null) (Syntax: 'string')
        Initializer:
          null
    Collection:
      IParameterReferenceOperation: pets (OperationKind.ParameterReference, Type: System.Collections.Generic.IAsyncEnumerable<System.String>) (Syntax: 'pets')
    Body:
      IBlockOperation (1 statements) (OperationKind.Block, Type: null) (Syntax: '{ ... }')
        IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'System.Cons ... ine(value);')
          Expression:
            IInvocationOperation (void System.Console.WriteLine(System.String value)) (OperationKind.Invocation, Type: System.Void) (Syntax: 'System.Cons ... Line(value)')
              Instance Receiver:
                null
              Arguments(1):
                  IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: value) (OperationKind.Argument, Type: null) (Syntax: 'value')
                    ILocalReferenceOperation: value (OperationKind.LocalReference, Type: System.String) (Syntax: 'value')
                    InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                    OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
    NextVariables(0)";
            VerifyOperationTreeForTest<BlockSyntax>(source, expectedOperationTree);
        }

        private static string s_ValueTask = @"
namespace System.Threading.Tasks
{
    [System.Runtime.CompilerServices.AsyncMethodBuilder(typeof(System.Runtime.CompilerServices.ValueTaskMethodBuilder))]
    public struct ValueTask
    {
        public Awaiter GetAwaiter() => null;
        public class Awaiter : System.Runtime.CompilerServices.INotifyCompletion
        {
            public void OnCompleted(Action a) { }
            public bool IsCompleted => true;
            public void GetResult() { }
        }
    }
    [System.Runtime.CompilerServices.AsyncMethodBuilder(typeof(System.Runtime.CompilerServices.ValueTaskMethodBuilder<>))]
    public struct ValueTask<T>
    {
        public Awaiter GetAwaiter() => null;
        public class Awaiter : System.Runtime.CompilerServices.INotifyCompletion
        {
            public void OnCompleted(Action a) { }
            public bool IsCompleted => true;
            public T GetResult() => default;
        }
    }
}
namespace System.Runtime.CompilerServices
{
    public class AsyncMethodBuilderAttribute : Attribute
    {
       public AsyncMethodBuilderAttribute(Type t) { }
    }
    public class ValueTaskMethodBuilder
    {
        public static ValueTaskMethodBuilder Create() => null;
        internal ValueTaskMethodBuilder(System.Threading.Tasks.ValueTask task) { }
        public void SetStateMachine(IAsyncStateMachine stateMachine) { }
        public void Start<TStateMachine>(ref TStateMachine stateMachine) where TStateMachine : System.Runtime.CompilerServices.IAsyncStateMachine { }
        public void SetException(Exception e) { }
        public void SetResult() { }
        public void AwaitOnCompleted<TAwaiter, TStateMachine>(ref TAwaiter awaiter, ref TStateMachine stateMachine) where TAwaiter : System.Runtime.CompilerServices.INotifyCompletion where TStateMachine : System.Runtime.CompilerServices.IAsyncStateMachine { }
        public void AwaitUnsafeOnCompleted<TAwaiter, TStateMachine>(ref TAwaiter awaiter, ref TStateMachine stateMachine) where TAwaiter : System.Runtime.CompilerServices.ICriticalNotifyCompletion where TStateMachine : System.Runtime.CompilerServices.IAsyncStateMachine { }
        public System.Threading.Tasks.ValueTask Task => default;
    }
    public class ValueTaskMethodBuilder<T>
    {
        public static ValueTaskMethodBuilder<T> Create() => null;
        internal ValueTaskMethodBuilder(System.Threading.Tasks.ValueTask<T> task) { }
        public void SetStateMachine(IAsyncStateMachine stateMachine) { }
        public void Start<TStateMachine>(ref TStateMachine stateMachine) where TStateMachine : System.Runtime.CompilerServices.IAsyncStateMachine { }
        public void SetException(Exception e) { }
        public void SetResult(T t) { }
        public void AwaitOnCompleted<TAwaiter, TStateMachine>(ref TAwaiter awaiter, ref TStateMachine stateMachine) where TAwaiter : System.Runtime.CompilerServices.INotifyCompletion where TStateMachine : System.Runtime.CompilerServices.IAsyncStateMachine { }
        public void AwaitUnsafeOnCompleted<TAwaiter, TStateMachine>(ref TAwaiter awaiter, ref TStateMachine stateMachine) where TAwaiter : System.Runtime.CompilerServices.ICriticalNotifyCompletion where TStateMachine : System.Runtime.CompilerServices.IAsyncStateMachine { }
        public System.Threading.Tasks.ValueTask<T> Task => default;
    }
}";
    }
}
