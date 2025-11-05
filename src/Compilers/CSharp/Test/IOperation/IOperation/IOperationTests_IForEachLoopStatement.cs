// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.CSharp.Test.Utilities;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.CSharp.UnitTests
{
    public class IOperationTests_IForEachLoopStatement : SemanticModelTestBase
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
        IDeclarationPatternOperation (OperationKind.DeclarationPattern, Type: null) (Syntax: 'int x') (InputType: System.Object, NarrowedType: System.Int32, DeclaredSymbol: System.Int32 x, MatchesNull: False)
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

        [CompilerTrait(CompilerFeature.IOperation)]
        [Fact, WorkItem(17602, "https://github.com/dotnet/roslyn/issues/17602")]
        public void IForEachLoopStatement_ViaExtensionMethod()
        {
            var source = @"
using System.Collections.Generic;
class Program
{
    static void Main()
    {
        /*<bind>*/foreach (var value in new Program())
        {
            System.Console.WriteLine(value);
        }/*</bind>*/
    }
}

static class Extensions
{
    public static IEnumerator<string> GetEnumerator(this Program p) => throw null;
}
";
            var expectedOperationTree = @"
IForEachLoopOperation (LoopKind.ForEach, Continue Label Id: 0, Exit Label Id: 1) (OperationKind.Loop, Type: null) (Syntax: 'foreach (va ... }')
  Locals: Local_1: System.String value
  LoopControlVariable: 
    IVariableDeclaratorOperation (Symbol: System.String value) (OperationKind.VariableDeclarator, Type: null) (Syntax: 'var')
      Initializer: 
        null
  Collection: 
    IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: Program, IsImplicit) (Syntax: 'new Program()')
      Conversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
      Operand: 
        IObjectCreationOperation (Constructor: Program..ctor()) (OperationKind.ObjectCreation, Type: Program) (Syntax: 'new Program()')
          Arguments(0)
          Initializer: 
            null
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
            VerifyOperationTreeForTest<ForEachStatementSyntax>(source, expectedOperationTree, parseOptions: TestOptions.Regular9);
        }

        [CompilerTrait(CompilerFeature.IOperation)]
        [Fact, WorkItem(17602, "https://github.com/dotnet/roslyn/issues/17602")]
        public void IForEachLoopStatement_ViaExtensionMethodWithConversion()
        {
            var source = @"
using System.Collections.Generic;
class Program
{
    static void Main()
    {
        /*<bind>*/foreach (var value in new Program())
        {
            System.Console.WriteLine(value);
        }/*</bind>*/
    }
}

static class Extensions
{
    public static IEnumerator<string> GetEnumerator(this object p) => throw null;
}
";
            var expectedOperationTree = @"
IForEachLoopOperation (LoopKind.ForEach, Continue Label Id: 0, Exit Label Id: 1) (OperationKind.Loop, Type: null) (Syntax: 'foreach (va ... }')
  Locals: Local_1: System.String value
  LoopControlVariable: 
    IVariableDeclaratorOperation (Symbol: System.String value) (OperationKind.VariableDeclarator, Type: null) (Syntax: 'var')
      Initializer: 
        null
  Collection: 
    IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.Object, IsImplicit) (Syntax: 'new Program()')
      Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: True, IsUserDefined: False) (MethodSymbol: null)
      Operand: 
        IObjectCreationOperation (Constructor: Program..ctor()) (OperationKind.ObjectCreation, Type: Program) (Syntax: 'new Program()')
          Arguments(0)
          Initializer: 
            null
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
            VerifyOperationTreeForTest<ForEachStatementSyntax>(source, expectedOperationTree, parseOptions: TestOptions.Regular9);
        }

        [CompilerTrait(CompilerFeature.IOperation)]
        [Fact, WorkItem(17602, "https://github.com/dotnet/roslyn/issues/17602")]
        public void IForEachLoopStatement_ViaExtensionMethod_WithGetEnumeratorReturningWrongType()
        {
            var source = @"
using System.Collections.Generic;
class Program
{
    static void Main()
    {
        /*<bind>*/foreach (var value in new Program())
        {
            System.Console.WriteLine(value);
        }/*</bind>*/
    }
}

static class Extensions
{
    public static bool GetEnumerator(this Program p) => throw null;
}
";
            var expectedOperationTree = @"
IForEachLoopOperation (LoopKind.ForEach, Continue Label Id: 0, Exit Label Id: 1) (OperationKind.Loop, Type: null, IsInvalid) (Syntax: 'foreach (va ... }')
  Locals: Local_1: var value
  LoopControlVariable: 
    IVariableDeclaratorOperation (Symbol: var value) (OperationKind.VariableDeclarator, Type: null) (Syntax: 'var')
      Initializer: 
        null
  Collection: 
    IObjectCreationOperation (Constructor: Program..ctor()) (OperationKind.ObjectCreation, Type: Program, IsInvalid) (Syntax: 'new Program()')
      Arguments(0)
      Initializer: 
        null
  Body: 
    IBlockOperation (1 statements) (OperationKind.Block, Type: null) (Syntax: '{ ... }')
      IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'System.Cons ... ine(value);')
        Expression: 
          IInvalidOperation (OperationKind.Invalid, Type: System.Void) (Syntax: 'System.Cons ... Line(value)')
            Children(2):
                IOperation:  (OperationKind.None, Type: System.Console) (Syntax: 'System.Console')
                ILocalReferenceOperation: value (OperationKind.LocalReference, Type: var) (Syntax: 'value')
  NextVariables(0)";
            VerifyOperationTreeForTest<ForEachStatementSyntax>(source, expectedOperationTree, parseOptions: TestOptions.Regular9);
        }

        [CompilerTrait(CompilerFeature.IOperation)]
        [Fact, WorkItem(17602, "https://github.com/dotnet/roslyn/issues/17602")]
        public void IForEachLoopStatement_ViaExtensionMethod_WithSpillInExpression()
        {
            var source = @"
using System.Collections.Generic;
class Program
{
    static void Main()
    {
        /*<bind>*/foreach (var value in null ?? new Program())
        {
            System.Console.WriteLine(value);
        }/*</bind>*/
    }
}

static class Extensions
{
    public static IEnumerator<string> GetEnumerator(this Program p) => throw null;
}
";
            var expectedOperationTree = @"
IForEachLoopOperation (LoopKind.ForEach, Continue Label Id: 0, Exit Label Id: 1) (OperationKind.Loop, Type: null) (Syntax: 'foreach (va ... }')
  Locals: Local_1: System.String value
  LoopControlVariable: 
    IVariableDeclaratorOperation (Symbol: System.String value) (OperationKind.VariableDeclarator, Type: null) (Syntax: 'var')
      Initializer: 
        null
  Collection: 
    IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: Program, IsImplicit) (Syntax: 'null ?? new Program()')
      Conversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
      Operand: 
        ICoalesceOperation (OperationKind.Coalesce, Type: Program) (Syntax: 'null ?? new Program()')
          Expression: 
            ILiteralOperation (OperationKind.Literal, Type: null, Constant: null) (Syntax: 'null')
          ValueConversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: True, IsUserDefined: False) (MethodSymbol: null)
            (ImplicitReference)
          WhenNull: 
            IObjectCreationOperation (Constructor: Program..ctor()) (OperationKind.ObjectCreation, Type: Program) (Syntax: 'new Program()')
              Arguments(0)
              Initializer: 
                null
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
            VerifyOperationTreeForTest<ForEachStatementSyntax>(source, expectedOperationTree, parseOptions: TestOptions.Regular9);
        }

        [CompilerTrait(CompilerFeature.IOperation)]
        [Fact, WorkItem(17602, "https://github.com/dotnet/roslyn/issues/17602")]
        public void IAwaitForEachLoopStatement_ViaExtensionMethod()
        {
            var source = @"
using System.Collections.Generic;
class Program
{
    static async void Main()
    {
        /*<bind>*/await foreach (var value in new Program())
        {
            System.Console.WriteLine(value);
        }/*</bind>*/
    }
}

static class Extensions
{
    public static IAsyncEnumerator<string> GetAsyncEnumerator(this Program p) => throw null;
}
";
            var expectedOperationTree = @"
IForEachLoopOperation (LoopKind.ForEach, IsAsynchronous, Continue Label Id: 0, Exit Label Id: 1) (OperationKind.Loop, Type: null) (Syntax: 'await forea ... }')
  Locals: Local_1: System.String value
  LoopControlVariable: 
    IVariableDeclaratorOperation (Symbol: System.String value) (OperationKind.VariableDeclarator, Type: null) (Syntax: 'var')
      Initializer: 
        null
  Collection: 
    IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: Program, IsImplicit) (Syntax: 'new Program()')
      Conversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
      Operand: 
        IObjectCreationOperation (Constructor: Program..ctor()) (OperationKind.ObjectCreation, Type: Program) (Syntax: 'new Program()')
          Arguments(0)
          Initializer: 
            null
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
            var comp = CreateCompilationWithTasksExtensions(new[] { source, s_IAsyncEnumerable }, parseOptions: TestOptions.Regular9);
            comp.VerifyDiagnostics();
            VerifyOperationTreeForTest<ForEachStatementSyntax>(comp, expectedOperationTree);
        }

        [CompilerTrait(CompilerFeature.IOperation)]
        [Fact, WorkItem(17602, "https://github.com/dotnet/roslyn/issues/17602")]
        public void IAwaitForEachLoopStatement_ViaExtensionMethodWithConversion()
        {
            var source = @"
using System.Collections.Generic;
class Program
{
    static async void Main()
    {
        /*<bind>*/await foreach (var value in new Program())
        {
            System.Console.WriteLine(value);
        }/*</bind>*/
    }
}

static class Extensions
{
    public static IAsyncEnumerator<string> GetAsyncEnumerator(this object p) => throw null;
}
";
            var expectedOperationTree = @"
IForEachLoopOperation (LoopKind.ForEach, IsAsynchronous, Continue Label Id: 0, Exit Label Id: 1) (OperationKind.Loop, Type: null) (Syntax: 'await forea ... }')
  Locals: Local_1: System.String value
  LoopControlVariable: 
    IVariableDeclaratorOperation (Symbol: System.String value) (OperationKind.VariableDeclarator, Type: null) (Syntax: 'var')
      Initializer: 
        null
  Collection: 
    IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.Object, IsImplicit) (Syntax: 'new Program()')
      Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: True, IsUserDefined: False) (MethodSymbol: null)
      Operand: 
        IObjectCreationOperation (Constructor: Program..ctor()) (OperationKind.ObjectCreation, Type: Program) (Syntax: 'new Program()')
          Arguments(0)
          Initializer: 
            null
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
            var comp = CreateCompilationWithTasksExtensions(new[] { source, s_IAsyncEnumerable }, parseOptions: TestOptions.Regular9);
            comp.VerifyDiagnostics();
            VerifyOperationTreeForTest<ForEachStatementSyntax>(comp, expectedOperationTree);
        }

        [CompilerTrait(CompilerFeature.IOperation)]
        [Fact, WorkItem(17602, "https://github.com/dotnet/roslyn/issues/17602")]
        public void IAwaitForEachLoopStatement_ViaExtensionMethod_WithGetAsyncEnumeratorReturningWrongType()
        {
            var source = @"
class Program
{
    static async void Main()
    {
        /*<bind>*/await foreach (var value in new Program())
        {
            System.Console.WriteLine(value);
        }/*</bind>*/
    }
}

static class Extensions
{
    public static bool GetAsyncEnumerator(this Program p) => throw null;
}
";
            var expectedOperationTree = @"
IForEachLoopOperation (LoopKind.ForEach, Continue Label Id: 0, Exit Label Id: 1) (OperationKind.Loop, Type: null, IsInvalid) (Syntax: 'await forea ... }')
  Locals: Local_1: var value
  LoopControlVariable: 
    IVariableDeclaratorOperation (Symbol: var value) (OperationKind.VariableDeclarator, Type: null) (Syntax: 'var')
      Initializer: 
        null
  Collection: 
    IObjectCreationOperation (Constructor: Program..ctor()) (OperationKind.ObjectCreation, Type: Program, IsInvalid) (Syntax: 'new Program()')
      Arguments(0)
      Initializer: 
        null
  Body: 
    IBlockOperation (1 statements) (OperationKind.Block, Type: null) (Syntax: '{ ... }')
      IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'System.Cons ... ine(value);')
        Expression: 
          IInvalidOperation (OperationKind.Invalid, Type: System.Void) (Syntax: 'System.Cons ... Line(value)')
            Children(2):
                IOperation:  (OperationKind.None, Type: System.Console) (Syntax: 'System.Console')
                ILocalReferenceOperation: value (OperationKind.LocalReference, Type: var) (Syntax: 'value')
  NextVariables(0)";
            var comp = CreateCompilationWithTasksExtensions(new[] { source, s_IAsyncEnumerable }, parseOptions: TestOptions.Regular9);
            comp.VerifyDiagnostics(
                // (6,47): error CS0117: 'bool' does not contain a definition for 'Current'
                //         /*<bind>*/await foreach (var value in new Program())
                Diagnostic(ErrorCode.ERR_NoSuchMember, "new Program()").WithArguments("bool", "Current").WithLocation(6, 47),
                // (6,47): error CS8412: Asynchronous foreach requires that the return type 'bool' of 'Extensions.GetAsyncEnumerator(Program)' must have a suitable public 'MoveNextAsync' method and public 'Current' property
                //         /*<bind>*/await foreach (var value in new Program())
                Diagnostic(ErrorCode.ERR_BadGetAsyncEnumerator, "new Program()").WithArguments("bool", "Extensions.GetAsyncEnumerator(Program)").WithLocation(6, 47));
            VerifyOperationTreeForTest<ForEachStatementSyntax>(comp, expectedOperationTree);
        }

        [CompilerTrait(CompilerFeature.IOperation)]
        [Fact, WorkItem(17602, "https://github.com/dotnet/roslyn/issues/17602")]
        public void IAwaitForEachLoopStatement_ViaExtensionMethod_WithSpillInExpression()
        {
            var source = @"
using System.Collections.Generic;
class Program
{
    static async void Main()
    {
        /*<bind>*/await foreach (var value in null ?? new Program())
        {
            System.Console.WriteLine(value);
        }/*</bind>*/
    }
}

static class Extensions
{
    public static IAsyncEnumerator<string> GetAsyncEnumerator(this Program p) => throw null;
}
";
            var expectedOperationTree = @"
IForEachLoopOperation (LoopKind.ForEach, IsAsynchronous, Continue Label Id: 0, Exit Label Id: 1) (OperationKind.Loop, Type: null) (Syntax: 'await forea ... }')
  Locals: Local_1: System.String value
  LoopControlVariable: 
    IVariableDeclaratorOperation (Symbol: System.String value) (OperationKind.VariableDeclarator, Type: null) (Syntax: 'var')
      Initializer: 
        null
  Collection: 
    IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: Program, IsImplicit) (Syntax: 'null ?? new Program()')
      Conversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
      Operand: 
        ICoalesceOperation (OperationKind.Coalesce, Type: Program) (Syntax: 'null ?? new Program()')
          Expression: 
            ILiteralOperation (OperationKind.Literal, Type: null, Constant: null) (Syntax: 'null')
          ValueConversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: True, IsUserDefined: False) (MethodSymbol: null)
            (ImplicitReference)
          WhenNull: 
            IObjectCreationOperation (Constructor: Program..ctor()) (OperationKind.ObjectCreation, Type: Program) (Syntax: 'new Program()')
              Arguments(0)
              Initializer: 
                null
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
            var comp = CreateCompilationWithTasksExtensions(new[] { source, s_IAsyncEnumerable }, parseOptions: TestOptions.Regular9);
            comp.VerifyDiagnostics();
            VerifyOperationTreeForTest<ForEachStatementSyntax>(comp, expectedOperationTree);
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

        [CompilerTrait(CompilerFeature.IOperation, CompilerFeature.IOperation)]
        [Fact]
        public void CheckForEachLoopOperationInfoArguments()
        {
            var src = @"using System;
using System.Threading.Tasks;
using System.Threading;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
/*<bind>*/
await foreach (var x in new CustomAsyncEnumerable())
{
	Console.WriteLine(x);
}
/*</bind>*/

struct CustomAsyncEnumerable : IAsyncEnumerable<int>
{
	public AsyncEnumerator GetAsyncEnumerator([CallerMemberName] string s = default,
	   [CallerLineNumber] int line = default)
	{
		Console.WriteLine($""line: {line}"");
		Console.WriteLine($""member: {s}"");
		Console.WriteLine(""GetAsyncEnumerator"");
		return new();
	}

	IAsyncEnumerator<int> IAsyncEnumerable<int>.GetAsyncEnumerator(CancellationToken token)
	{
		Console.WriteLine(""IAsyncEnumerable<int>.GetAsyncEnumerator(CancellationToken token)"");
		return GetAsyncEnumerator();
	}
}

struct AsyncEnumerator : IAsyncEnumerator<int>
{
	private int x;
	public ValueTask<bool> MoveNextAsync([CallerMemberName] string s = default,
		[CallerLineNumber] int line = default, int r = 12)
	{
		Console.WriteLine($""line: {line}"");
		Console.WriteLine($""member: {s}"");
		return ValueTask.FromResult(x++ < 5);
	}

	ValueTask<bool> IAsyncEnumerator<int>.MoveNextAsync()
	{
		Console.WriteLine(""IAsyncEnumerator<int>.MoveNextAsync()"");
		return MoveNextAsync();
	}
	ValueTask IAsyncDisposable.DisposeAsync()
	{
		Console.WriteLine(""IAsyncDisposable.DisposeAsync()"");
		return DisposeAsync();
	}

	public int Current => x;
	
	public ValueTask DisposeAsync([CallerMemberName] string s = default,
						[CallerLineNumber] int line = default, int xxx=12, string f = """")
	{
		Console.WriteLine($""line: {line}"");
		Console.WriteLine($""member: {s}"");
		return ValueTask.CompletedTask;
	}
}
";

            var comp = CreateCompilation(src, targetFramework: TargetFramework.Net60);
            comp.VerifyDiagnostics();
            var op = (Operations.ForEachLoopOperation)VerifyOperationTreeForTest<ForEachStatementSyntax>(comp, @"IForEachLoopOperation (LoopKind.ForEach, IsAsynchronous, Continue Label Id: 0, Exit Label Id: 1) (OperationKind.Loop, Type: null) (Syntax: 'await forea ... }')
  Locals: Local_1: System.Int32 x
  LoopControlVariable:
    IVariableDeclaratorOperation (Symbol: System.Int32 x) (OperationKind.VariableDeclarator, Type: null) (Syntax: 'var')
      Initializer:
        null
  Collection:
    IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: CustomAsyncEnumerable, IsImplicit) (Syntax: 'new CustomA ... numerable()')
      Conversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
      Operand:
        IObjectCreationOperation (Constructor: CustomAsyncEnumerable..ctor()) (OperationKind.ObjectCreation, Type: CustomAsyncEnumerable) (Syntax: 'new CustomA ... numerable()')
          Arguments(0)
          Initializer:
            null
  Body:
    IBlockOperation (1 statements) (OperationKind.Block, Type: null) (Syntax: '{ ... }')
      IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'Console.WriteLine(x);')
        Expression:
          IInvocationOperation (void System.Console.WriteLine(System.Int32 value)) (OperationKind.Invocation, Type: System.Void) (Syntax: 'Console.WriteLine(x)')
            Instance Receiver:
              null
            Arguments(1):
                IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: value) (OperationKind.Argument, Type: null) (Syntax: 'x')
                  ILocalReferenceOperation: x (OperationKind.LocalReference, Type: System.Int32) (Syntax: 'x')
                  InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                  OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
  NextVariables(0)");

            Assert.Equal(2, op.Info.GetEnumeratorArguments.Length);
            Assert.Equal(3, op.Info.MoveNextArguments.Length);
            Assert.Equal(4, op.Info.DisposeArguments.Length);
            Assert.Equal(@"System.Threading.Tasks.ValueTask AsyncEnumerator.DisposeAsync([System.String s = null], [System.Int32 line = 0], [System.Int32 xxx = 12], [System.String f = """"])",
                op.Info.PatternDisposeMethod.ToTestDisplayString());

            VerifyOperationTree(comp, op.Info.GetEnumeratorArguments[0], @"IArgumentOperation (ArgumentKind.DefaultValue, Matching Parameter: s) (OperationKind.Argument, Type: null, IsImplicit) (Syntax: 'await forea ... }')
  ILiteralOperation (OperationKind.Literal, Type: System.String, Constant: ""<Main>$"", IsImplicit) (Syntax: 'await forea ... }')
  InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
  OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)");

            VerifyOperationTree(comp, op.Info.GetEnumeratorArguments[1], @"IArgumentOperation (ArgumentKind.DefaultValue, Matching Parameter: line) (OperationKind.Argument, Type: null, IsImplicit) (Syntax: 'await forea ... }')
  ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 7, IsImplicit) (Syntax: 'await forea ... }')
  InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
  OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)");

            VerifyOperationTree(comp, op.Info.MoveNextArguments[0], @"IArgumentOperation (ArgumentKind.DefaultValue, Matching Parameter: s) (OperationKind.Argument, Type: null, IsImplicit) (Syntax: 'await forea ... }')
  ILiteralOperation (OperationKind.Literal, Type: System.String, Constant: ""<Main>$"", IsImplicit) (Syntax: 'await forea ... }')
  InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
  OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)");

            VerifyOperationTree(comp, op.Info.MoveNextArguments[1], @"IArgumentOperation (ArgumentKind.DefaultValue, Matching Parameter: line) (OperationKind.Argument, Type: null, IsImplicit) (Syntax: 'await forea ... }')
  ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 7, IsImplicit) (Syntax: 'await forea ... }')
  InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
  OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)");

            VerifyOperationTree(comp, op.Info.MoveNextArguments[2], @"IArgumentOperation (ArgumentKind.DefaultValue, Matching Parameter: r) (OperationKind.Argument, Type: null, IsImplicit) (Syntax: 'await forea ... }')
  ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 12, IsImplicit) (Syntax: 'await forea ... }')
  InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
  OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)");

            VerifyOperationTree(comp, op.Info.DisposeArguments[0], @"IArgumentOperation (ArgumentKind.DefaultValue, Matching Parameter: s) (OperationKind.Argument, Type: null, IsImplicit) (Syntax: 'await forea ... }')
  ILiteralOperation (OperationKind.Literal, Type: System.String, Constant: ""<Main>$"", IsImplicit) (Syntax: 'await forea ... }')
  InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
  OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)");

            VerifyOperationTree(comp, op.Info.DisposeArguments[1], @"IArgumentOperation (ArgumentKind.DefaultValue, Matching Parameter: line) (OperationKind.Argument, Type: null, IsImplicit) (Syntax: 'await forea ... }')
  ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 7, IsImplicit) (Syntax: 'await forea ... }')
  InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
  OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)");

            VerifyOperationTree(comp, op.Info.DisposeArguments[2], @"IArgumentOperation (ArgumentKind.DefaultValue, Matching Parameter: xxx) (OperationKind.Argument, Type: null, IsImplicit) (Syntax: 'await forea ... }')
  ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 12, IsImplicit) (Syntax: 'await forea ... }')
  InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
  OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)");

            VerifyOperationTree(comp, op.Info.DisposeArguments[3], @"IArgumentOperation (ArgumentKind.DefaultValue, Matching Parameter: f) (OperationKind.Argument, Type: null, IsImplicit) (Syntax: 'await forea ... }')
  ILiteralOperation (OperationKind.Literal, Type: System.String, Constant: """", IsImplicit) (Syntax: 'await forea ... }')
  InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
  OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)");
        }

        [CompilerTrait(CompilerFeature.IOperation, CompilerFeature.IOperation)]
        [Fact]
        public void CheckForEachLoopOperationInfoArguments2()
        {
            var src = @"using System;
using System.Threading.Tasks;
using System.Runtime.CompilerServices;

/*<bind>*/
await foreach (var x in new CustomAsyncEnumerable())
{
    Console.WriteLine(x);
}
/*</bind>*/

ref struct CustomAsyncEnumerable
{
    public AsyncEnumerator GetAsyncEnumerator([CallerMemberName] string s = default,
        [CallerLineNumber] int line = default)
    {
        Console.WriteLine($""line: {line}"");
        Console.WriteLine($""member: {s}"");
        Console.WriteLine(""GetAsyncEnumerator"");
        return new();
    }
}

struct AsyncEnumerator
{
    private int x;
    public ValueTask<bool> MoveNextAsync([CallerMemberName] string s = default,
                        [CallerLineNumber] int line = default)
    {
        Console.WriteLine($""line: {line}"");
        Console.WriteLine($""member: {s}"");
        return ValueTask.FromResult(x++ < 5);
    }

    public int Current => x;

    public ValueTask DisposeAsync([CallerMemberName] string s = default,
                        [CallerLineNumber] int line = default)
    {
        Console.WriteLine($""line: {line}"");
        Console.WriteLine($""member: {s}"");
        return ValueTask.CompletedTask;
    }
}
";

            var comp = CreateCompilation(src, targetFramework: TargetFramework.Net60);
            comp.VerifyDiagnostics();
            var op = (Operations.ForEachLoopOperation)VerifyOperationTreeForTest<ForEachStatementSyntax>(comp, @"IForEachLoopOperation (LoopKind.ForEach, IsAsynchronous, Continue Label Id: 0, Exit Label Id: 1) (OperationKind.Loop, Type: null) (Syntax: 'await forea ... }')
  Locals: Local_1: System.Int32 x
  LoopControlVariable:
    IVariableDeclaratorOperation (Symbol: System.Int32 x) (OperationKind.VariableDeclarator, Type: null) (Syntax: 'var')
      Initializer:
        null
  Collection:
    IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: CustomAsyncEnumerable, IsImplicit) (Syntax: 'new CustomA ... numerable()')
      Conversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
      Operand:
        IObjectCreationOperation (Constructor: CustomAsyncEnumerable..ctor()) (OperationKind.ObjectCreation, Type: CustomAsyncEnumerable) (Syntax: 'new CustomA ... numerable()')
          Arguments(0)
          Initializer:
            null
  Body:
    IBlockOperation (1 statements) (OperationKind.Block, Type: null) (Syntax: '{ ... }')
      IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'Console.WriteLine(x);')
        Expression:
          IInvocationOperation (void System.Console.WriteLine(System.Int32 value)) (OperationKind.Invocation, Type: System.Void) (Syntax: 'Console.WriteLine(x)')
            Instance Receiver:
              null
            Arguments(1):
                IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: value) (OperationKind.Argument, Type: null) (Syntax: 'x')
                  ILocalReferenceOperation: x (OperationKind.LocalReference, Type: System.Int32) (Syntax: 'x')
                  InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                  OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
  NextVariables(0)");

            Assert.Equal(2, op.Info.GetEnumeratorArguments.Length);
            Assert.Equal(2, op.Info.MoveNextArguments.Length);
            Assert.Equal(2, op.Info.DisposeArguments.Length);

            Assert.Equal("System.Threading.Tasks.ValueTask AsyncEnumerator.DisposeAsync([System.String s = null], [System.Int32 line = 0])", op.Info.PatternDisposeMethod.ToTestDisplayString());

            VerifyOperationTree(comp, op.Info.GetEnumeratorArguments[0], @"IArgumentOperation (ArgumentKind.DefaultValue, Matching Parameter: s) (OperationKind.Argument, Type: null, IsImplicit) (Syntax: 'await forea ... }')
  ILiteralOperation (OperationKind.Literal, Type: System.String, Constant: ""<Main>$"", IsImplicit) (Syntax: 'await forea ... }')
  InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
  OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)");

            VerifyOperationTree(comp, op.Info.GetEnumeratorArguments[1], @"IArgumentOperation (ArgumentKind.DefaultValue, Matching Parameter: line) (OperationKind.Argument, Type: null, IsImplicit) (Syntax: 'await forea ... }')
  ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 6, IsImplicit) (Syntax: 'await forea ... }')
  InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
  OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)");

            VerifyOperationTree(comp, op.Info.MoveNextArguments[0], @"IArgumentOperation (ArgumentKind.DefaultValue, Matching Parameter: s) (OperationKind.Argument, Type: null, IsImplicit) (Syntax: 'await forea ... }')
  ILiteralOperation (OperationKind.Literal, Type: System.String, Constant: ""<Main>$"", IsImplicit) (Syntax: 'await forea ... }')
  InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
  OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)");

            VerifyOperationTree(comp, op.Info.MoveNextArguments[1], @"IArgumentOperation (ArgumentKind.DefaultValue, Matching Parameter: line) (OperationKind.Argument, Type: null, IsImplicit) (Syntax: 'await forea ... }')
  ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 6, IsImplicit) (Syntax: 'await forea ... }')
  InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
  OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)");

            VerifyOperationTree(comp, op.Info.DisposeArguments[0], @"IArgumentOperation (ArgumentKind.DefaultValue, Matching Parameter: s) (OperationKind.Argument, Type: null, IsImplicit) (Syntax: 'await forea ... }')
  ILiteralOperation (OperationKind.Literal, Type: System.String, Constant: ""<Main>$"", IsImplicit) (Syntax: 'await forea ... }')
  InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
  OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)");

            VerifyOperationTree(comp, op.Info.DisposeArguments[1], @"IArgumentOperation (ArgumentKind.DefaultValue, Matching Parameter: line) (OperationKind.Argument, Type: null, IsImplicit) (Syntax: 'await forea ... }')
  ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 6, IsImplicit) (Syntax: 'await forea ... }')
  InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
  OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)");
        }

        [CompilerTrait(CompilerFeature.IOperation, CompilerFeature.IOperation)]
        [Fact]
        public void NullPatternDisposeMethod()
        {
            var src = @"using System;
using System.Threading.Tasks;
/*<bind>*/
await foreach (var x in new CustomAsyncEnumerable())
{
	Console.WriteLine(x);
}
/*</bind>*/

struct CustomAsyncEnumerable
{
	public CustomAsyncEnumerator GetAsyncEnumerator() => new();
}

struct CustomAsyncEnumerator
{
	public ValueTask<bool> MoveNextAsync() => ValueTask.FromResult(false);
	public int Current => 0;
}
";

            var comp = CreateCompilation(src, targetFramework: TargetFramework.Net60);
            comp.VerifyDiagnostics();
            var op = (Operations.ForEachLoopOperation)VerifyOperationTreeForTest<ForEachStatementSyntax>(comp, @"
IForEachLoopOperation (LoopKind.ForEach, IsAsynchronous, Continue Label Id: 0, Exit Label Id: 1) (OperationKind.Loop, Type: null) (Syntax: 'await forea ... }')
  Locals: Local_1: System.Int32 x
  LoopControlVariable:
    IVariableDeclaratorOperation (Symbol: System.Int32 x) (OperationKind.VariableDeclarator, Type: null) (Syntax: 'var')
      Initializer:
        null
  Collection:
    IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: CustomAsyncEnumerable, IsImplicit) (Syntax: 'new CustomA ... numerable()')
      Conversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
      Operand:
        IObjectCreationOperation (Constructor: CustomAsyncEnumerable..ctor()) (OperationKind.ObjectCreation, Type: CustomAsyncEnumerable) (Syntax: 'new CustomA ... numerable()')
          Arguments(0)
          Initializer:
            null
  Body:
    IBlockOperation (1 statements) (OperationKind.Block, Type: null) (Syntax: '{ ... }')
      IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'Console.WriteLine(x);')
        Expression:
          IInvocationOperation (void System.Console.WriteLine(System.Int32 value)) (OperationKind.Invocation, Type: System.Void) (Syntax: 'Console.WriteLine(x)')
            Instance Receiver:
              null
            Arguments(1):
                IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: value) (OperationKind.Argument, Type: null) (Syntax: 'x')
                  ILocalReferenceOperation: x (OperationKind.LocalReference, Type: System.Int32) (Syntax: 'x')
                  InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                  OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
  NextVariables(0)");

            Assert.Null(op.Info.PatternDisposeMethod);
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
            VerifyFlowGraphAndDiagnosticsForTest<BlockSyntax>(source + TestSources.Span, expectedFlowGraph, expectedDiagnostics, TestOptions.ReleaseDll.WithAllowUnsafe(true));
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
            VerifyFlowGraphAndDiagnosticsForTest<BlockSyntax>(source + TestSources.Span, expectedFlowGraph, expectedDiagnostics, TestOptions.ReleaseDll.WithAllowUnsafe(true));
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
            VerifyFlowGraphAndDiagnosticsForTest<BlockSyntax>(source + TestSources.Span, expectedFlowGraph, expectedDiagnostics, TestOptions.ReleaseDll.WithAllowUnsafe(true));
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
            VerifyFlowGraphAndDiagnosticsForTest<BlockSyntax>(source + TestSources.Span, expectedFlowGraph, expectedDiagnostics, TestOptions.ReleaseDll.WithAllowUnsafe(true));
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
                // file.cs(6,27): error CS1579: foreach statement cannot operate on variables of type 'MyClass' because 'MyClass' does not contain a public instance or extension definition for 'GetEnumerator'
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

        [CompilerTrait(CompilerFeature.IOperation, CompilerFeature.Dataflow)]
        [Fact, WorkItem(17602, "https://github.com/dotnet/roslyn/issues/17602")]
        public void ForEachFlow_ViaExtensionMethod()
        {
            var source = @"
using System.Collections.Generic;
class Program
{
    static void Main()
    /*<bind>*/{
        foreach (var value in new Program())
        {
            System.Console.WriteLine(value);
        }
    }/*</bind>*/
}

static class Extensions
{
    public static IEnumerator<string> GetEnumerator(this Program p) => throw null;
}
";
            var expectedDiagnostics = DiagnosticDescription.None;

            var expectedFlowGraph = @"
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
            IFlowCaptureOperation: 0 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'new Program()')
              Value: 
                IInvocationOperation (System.Collections.Generic.IEnumerator<System.String> Extensions.GetEnumerator(this Program p)) (OperationKind.Invocation, Type: System.Collections.Generic.IEnumerator<System.String>, IsImplicit) (Syntax: 'new Program()')
                  Instance Receiver: 
                    null
                  Arguments(1):
                      IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: p) (OperationKind.Argument, Type: null, IsImplicit) (Syntax: 'new Program()')
                        IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: Program, IsImplicit) (Syntax: 'new Program()')
                          Conversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                            (Identity)
                          Operand: 
                            IObjectCreationOperation (Constructor: Program..ctor()) (OperationKind.ObjectCreation, Type: Program) (Syntax: 'new Program()')
                              Arguments(0)
                              Initializer: 
                                null
                        InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                        OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
        Next (Regular) Block[B2]
            Entering: {R2} {R3}
    .try {R2, R3}
    {
        Block[B2] - Block
            Predecessors: [B1] [B3]
            Statements (0)
            Jump if False (Regular) to Block[B7]
                IInvocationOperation (virtual System.Boolean System.Collections.IEnumerator.MoveNext()) (OperationKind.Invocation, Type: System.Boolean, IsImplicit) (Syntax: 'new Program()')
                  Instance Receiver: 
                    IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: System.Collections.Generic.IEnumerator<System.String>, IsImplicit) (Syntax: 'new Program()')
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
                    ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: null, IsImplicit) (Syntax: 'var')
                      Left: 
                        ILocalReferenceOperation: value (IsDeclaration: True) (OperationKind.LocalReference, Type: System.String, IsImplicit) (Syntax: 'var')
                      Right: 
                        IPropertyReferenceOperation: System.String System.Collections.Generic.IEnumerator<System.String>.Current { get; } (OperationKind.PropertyReference, Type: System.String, IsImplicit) (Syntax: 'var')
                          Instance Receiver: 
                            IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: System.Collections.Generic.IEnumerator<System.String>, IsImplicit) (Syntax: 'new Program()')
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
        Block[B4] - Block
            Predecessors (0)
            Statements (0)
            Jump if True (Regular) to Block[B6]
                IIsNullOperation (OperationKind.IsNull, Type: System.Boolean, IsImplicit) (Syntax: 'new Program()')
                  Operand: 
                    IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: System.Collections.Generic.IEnumerator<System.String>, IsImplicit) (Syntax: 'new Program()')
            Next (Regular) Block[B5]
        Block[B5] - Block
            Predecessors: [B4]
            Statements (1)
                IInvocationOperation (virtual void System.IDisposable.Dispose()) (OperationKind.Invocation, Type: System.Void, IsImplicit) (Syntax: 'new Program()')
                  Instance Receiver: 
                    IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.IDisposable, IsImplicit) (Syntax: 'new Program()')
                      Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: True, IsUserDefined: False) (MethodSymbol: null)
                        (ImplicitReference)
                      Operand: 
                        IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: System.Collections.Generic.IEnumerator<System.String>, IsImplicit) (Syntax: 'new Program()')
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
    Statements (0)";

            VerifyFlowGraphAndDiagnosticsForTest<BlockSyntax>(source, expectedFlowGraph, expectedDiagnostics, parseOptions: TestOptions.Regular9);
        }

        [CompilerTrait(CompilerFeature.IOperation, CompilerFeature.Dataflow)]
        [Fact, WorkItem(17602, "https://github.com/dotnet/roslyn/issues/17602")]
        public void ForEachFlow_ViaExtensionMethodWithConversion()
        {
            var source = @"
using System.Collections.Generic;
class Program
{
    static void Main()
    /*<bind>*/{
        foreach (var value in new Program())
        {
            System.Console.WriteLine(value);
        }
    }/*</bind>*/
}


static class Extensions
{
    public static IEnumerator<string> GetEnumerator(this object p) => throw null;
}
";
            var expectedDiagnostics = DiagnosticDescription.None;

            var expectedFlowGraph = @"
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
            IFlowCaptureOperation: 0 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'new Program()')
              Value: 
                IInvocationOperation (System.Collections.Generic.IEnumerator<System.String> Extensions.GetEnumerator(this System.Object p)) (OperationKind.Invocation, Type: System.Collections.Generic.IEnumerator<System.String>, IsImplicit) (Syntax: 'new Program()')
                  Instance Receiver: 
                    null
                  Arguments(1):
                      IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: p) (OperationKind.Argument, Type: null, IsImplicit) (Syntax: 'new Program()')
                        IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.Object, IsImplicit) (Syntax: 'new Program()')
                          Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: True, IsUserDefined: False) (MethodSymbol: null)
                            (ImplicitReference)
                          Operand: 
                            IObjectCreationOperation (Constructor: Program..ctor()) (OperationKind.ObjectCreation, Type: Program) (Syntax: 'new Program()')
                              Arguments(0)
                              Initializer: 
                                null
                        InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                        OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
        Next (Regular) Block[B2]
            Entering: {R2} {R3}
    .try {R2, R3}
    {
        Block[B2] - Block
            Predecessors: [B1] [B3]
            Statements (0)
            Jump if False (Regular) to Block[B7]
                IInvocationOperation (virtual System.Boolean System.Collections.IEnumerator.MoveNext()) (OperationKind.Invocation, Type: System.Boolean, IsImplicit) (Syntax: 'new Program()')
                  Instance Receiver: 
                    IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: System.Collections.Generic.IEnumerator<System.String>, IsImplicit) (Syntax: 'new Program()')
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
                    ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: null, IsImplicit) (Syntax: 'var')
                      Left: 
                        ILocalReferenceOperation: value (IsDeclaration: True) (OperationKind.LocalReference, Type: System.String, IsImplicit) (Syntax: 'var')
                      Right: 
                        IPropertyReferenceOperation: System.String System.Collections.Generic.IEnumerator<System.String>.Current { get; } (OperationKind.PropertyReference, Type: System.String, IsImplicit) (Syntax: 'var')
                          Instance Receiver: 
                            IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: System.Collections.Generic.IEnumerator<System.String>, IsImplicit) (Syntax: 'new Program()')
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
        Block[B4] - Block
            Predecessors (0)
            Statements (0)
            Jump if True (Regular) to Block[B6]
                IIsNullOperation (OperationKind.IsNull, Type: System.Boolean, IsImplicit) (Syntax: 'new Program()')
                  Operand: 
                    IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: System.Collections.Generic.IEnumerator<System.String>, IsImplicit) (Syntax: 'new Program()')
            Next (Regular) Block[B5]
        Block[B5] - Block
            Predecessors: [B4]
            Statements (1)
                IInvocationOperation (virtual void System.IDisposable.Dispose()) (OperationKind.Invocation, Type: System.Void, IsImplicit) (Syntax: 'new Program()')
                  Instance Receiver: 
                    IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.IDisposable, IsImplicit) (Syntax: 'new Program()')
                      Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: True, IsUserDefined: False) (MethodSymbol: null)
                        (ImplicitReference)
                      Operand: 
                        IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: System.Collections.Generic.IEnumerator<System.String>, IsImplicit) (Syntax: 'new Program()')
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
    Statements (0)";

            VerifyFlowGraphAndDiagnosticsForTest<BlockSyntax>(source, expectedFlowGraph, expectedDiagnostics, parseOptions: TestOptions.Regular9);
        }

        [CompilerTrait(CompilerFeature.IOperation, CompilerFeature.Dataflow)]
        [Fact, WorkItem(17602, "https://github.com/dotnet/roslyn/issues/17602")]
        public void ForEachFlow_ViaExtensionMethod_WithGetEnumeratorReturningWrongType()
        {
            var source = @"
using System.Collections.Generic;
class Program
{
    static void Main()
    /*<bind>*/{
        foreach (var value in new Program())
        {
            System.Console.WriteLine(value);
        }
    }/*</bind>*/
}

static class Extensions
{
    public static bool GetEnumerator(this Program p) => throw null;
}
";
            var expectedDiagnostics = new[] {
                // file.cs(7,31): error CS0117: 'bool' does not contain a definition for 'Current'
                //         foreach (var value in new Program())
                Diagnostic(ErrorCode.ERR_NoSuchMember, "new Program()").WithArguments("bool", "Current").WithLocation(7, 31),
                // file.cs(7,31): error CS0202: foreach requires that the return type 'bool' of 'Extensions.GetEnumerator(Program)' must have a suitable public 'MoveNext' method and public 'Current' property
                //         foreach (var value in new Program())
                Diagnostic(ErrorCode.ERR_BadGetEnumerator, "new Program()").WithArguments("bool", "Extensions.GetEnumerator(Program)").WithLocation(7, 31)
            };

            var expectedFlowGraph = @"
Block[B0] - Entry
    Statements (0)
    Next (Regular) Block[B1]
Block[B1] - Block
    Predecessors: [B0]
    Statements (1)
        IInvalidOperation (OperationKind.Invalid, Type: null, IsInvalid, IsImplicit) (Syntax: 'new Program()')
          Children(1):
              IObjectCreationOperation (Constructor: Program..ctor()) (OperationKind.ObjectCreation, Type: Program, IsInvalid) (Syntax: 'new Program()')
                Arguments(0)
                Initializer: 
                  null
    Next (Regular) Block[B2]
Block[B2] - Block
    Predecessors: [B1] [B3]
    Statements (0)
    Jump if False (Regular) to Block[B4]
        IInvalidOperation (OperationKind.Invalid, Type: System.Boolean, IsInvalid, IsImplicit) (Syntax: 'new Program()')
          Children(1):
              IInvalidOperation (OperationKind.Invalid, Type: null, IsInvalid, IsImplicit) (Syntax: 'new Program()')
                Children(0)
    Next (Regular) Block[B3]
        Entering: {R1}
.locals {R1}
{
    Locals: [var value]
    Block[B3] - Block
        Predecessors: [B2]
        Statements (2)
            ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: null, IsImplicit) (Syntax: 'var')
              Left: 
                ILocalReferenceOperation: value (IsDeclaration: True) (OperationKind.LocalReference, Type: var, IsImplicit) (Syntax: 'var')
              Right: 
                IInvalidOperation (OperationKind.Invalid, Type: null, IsInvalid, IsImplicit) (Syntax: 'new Program()')
                  Children(1):
                      IInvalidOperation (OperationKind.Invalid, Type: null, IsInvalid, IsImplicit) (Syntax: 'new Program()')
                        Children(0)
            IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'System.Cons ... ine(value);')
              Expression: 
                IInvalidOperation (OperationKind.Invalid, Type: System.Void) (Syntax: 'System.Cons ... Line(value)')
                  Children(2):
                      IOperation:  (OperationKind.None, Type: System.Console) (Syntax: 'System.Console')
                      ILocalReferenceOperation: value (OperationKind.LocalReference, Type: var) (Syntax: 'value')
        Next (Regular) Block[B2]
            Leaving: {R1}
}
Block[B4] - Exit
    Predecessors: [B2]
    Statements (0)";

            VerifyFlowGraphAndDiagnosticsForTest<BlockSyntax>(source, expectedFlowGraph, expectedDiagnostics, parseOptions: TestOptions.Regular9);
        }

        [CompilerTrait(CompilerFeature.IOperation, CompilerFeature.Dataflow)]
        [Fact, WorkItem(17602, "https://github.com/dotnet/roslyn/issues/17602")]
        public void ForEachFlow_ViaExtensionMethod_WithSpillInExpression()
        {
            var source = @"
using System.Collections.Generic;
class Program
{
    static void Main()
    /*<bind>*/{
        foreach (var value in null ?? new Program())
        {
            System.Console.WriteLine(value);
        }
    }/*</bind>*/
}

static class Extensions
{
    public static IEnumerator<string> GetEnumerator(this Program p) => throw null;
}
";
            var expectedDiagnostics = DiagnosticDescription.None;

            var expectedFlowGraph = @"
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
                    IFlowCaptureOperation: 0 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'null')
                      Value: 
                        ILiteralOperation (OperationKind.Literal, Type: null, Constant: null) (Syntax: 'null')
                Jump if True (Regular) to Block[B3]
                    IIsNullOperation (OperationKind.IsNull, Type: System.Boolean, Constant: True, IsImplicit) (Syntax: 'null')
                      Operand: 
                        IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: null, Constant: null, IsImplicit) (Syntax: 'null')
                    Leaving: {R3}
                Next (Regular) Block[B2]
            Block[B2] - Block [UnReachable]
                Predecessors: [B1]
                Statements (1)
                    IFlowCaptureOperation: 1 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'null')
                      Value: 
                        IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: Program, IsImplicit) (Syntax: 'null')
                          Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: True, IsUserDefined: False) (MethodSymbol: null)
                            (ImplicitReference)
                          Operand: 
                            IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: null, Constant: null, IsImplicit) (Syntax: 'null')
                Next (Regular) Block[B4]
                    Leaving: {R3}
        }
        Block[B3] - Block
            Predecessors: [B1]
            Statements (1)
                IFlowCaptureOperation: 1 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'new Program()')
                  Value: 
                    IObjectCreationOperation (Constructor: Program..ctor()) (OperationKind.ObjectCreation, Type: Program) (Syntax: 'new Program()')
                      Arguments(0)
                      Initializer: 
                        null
            Next (Regular) Block[B4]
        Block[B4] - Block
            Predecessors: [B2] [B3]
            Statements (1)
                IFlowCaptureOperation: 2 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'null ?? new Program()')
                  Value: 
                    IInvocationOperation (System.Collections.Generic.IEnumerator<System.String> Extensions.GetEnumerator(this Program p)) (OperationKind.Invocation, Type: System.Collections.Generic.IEnumerator<System.String>, IsImplicit) (Syntax: 'null ?? new Program()')
                      Instance Receiver: 
                        null
                      Arguments(1):
                          IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: p) (OperationKind.Argument, Type: null, IsImplicit) (Syntax: 'null ?? new Program()')
                            IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: Program, IsImplicit) (Syntax: 'null ?? new Program()')
                              Conversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                                (Identity)
                              Operand: 
                                IFlowCaptureReferenceOperation: 1 (OperationKind.FlowCaptureReference, Type: Program, IsImplicit) (Syntax: 'null ?? new Program()')
                            InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                            OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
            Next (Regular) Block[B5]
                Leaving: {R2}
                Entering: {R4} {R5}
    }
    .try {R4, R5}
    {
        Block[B5] - Block
            Predecessors: [B4] [B6]
            Statements (0)
            Jump if False (Regular) to Block[B10]
                IInvocationOperation (virtual System.Boolean System.Collections.IEnumerator.MoveNext()) (OperationKind.Invocation, Type: System.Boolean, IsImplicit) (Syntax: 'null ?? new Program()')
                  Instance Receiver: 
                    IFlowCaptureReferenceOperation: 2 (OperationKind.FlowCaptureReference, Type: System.Collections.Generic.IEnumerator<System.String>, IsImplicit) (Syntax: 'null ?? new Program()')
                  Arguments(0)
                Finalizing: {R7}
                Leaving: {R5} {R4} {R1}
            Next (Regular) Block[B6]
                Entering: {R6}
        .locals {R6}
        {
            Locals: [System.String value]
            Block[B6] - Block
                Predecessors: [B5]
                Statements (2)
                    ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: null, IsImplicit) (Syntax: 'var')
                      Left: 
                        ILocalReferenceOperation: value (IsDeclaration: True) (OperationKind.LocalReference, Type: System.String, IsImplicit) (Syntax: 'var')
                      Right: 
                        IPropertyReferenceOperation: System.String System.Collections.Generic.IEnumerator<System.String>.Current { get; } (OperationKind.PropertyReference, Type: System.String, IsImplicit) (Syntax: 'var')
                          Instance Receiver: 
                            IFlowCaptureReferenceOperation: 2 (OperationKind.FlowCaptureReference, Type: System.Collections.Generic.IEnumerator<System.String>, IsImplicit) (Syntax: 'null ?? new Program()')
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
                Next (Regular) Block[B5]
                    Leaving: {R6}
        }
    }
    .finally {R7}
    {
        Block[B7] - Block
            Predecessors (0)
            Statements (0)
            Jump if True (Regular) to Block[B9]
                IIsNullOperation (OperationKind.IsNull, Type: System.Boolean, IsImplicit) (Syntax: 'null ?? new Program()')
                  Operand: 
                    IFlowCaptureReferenceOperation: 2 (OperationKind.FlowCaptureReference, Type: System.Collections.Generic.IEnumerator<System.String>, IsImplicit) (Syntax: 'null ?? new Program()')
            Next (Regular) Block[B8]
        Block[B8] - Block
            Predecessors: [B7]
            Statements (1)
                IInvocationOperation (virtual void System.IDisposable.Dispose()) (OperationKind.Invocation, Type: System.Void, IsImplicit) (Syntax: 'null ?? new Program()')
                  Instance Receiver: 
                    IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.IDisposable, IsImplicit) (Syntax: 'null ?? new Program()')
                      Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: True, IsUserDefined: False) (MethodSymbol: null)
                        (ImplicitReference)
                      Operand: 
                        IFlowCaptureReferenceOperation: 2 (OperationKind.FlowCaptureReference, Type: System.Collections.Generic.IEnumerator<System.String>, IsImplicit) (Syntax: 'null ?? new Program()')
                  Arguments(0)
            Next (Regular) Block[B9]
        Block[B9] - Block
            Predecessors: [B7] [B8]
            Statements (0)
            Next (StructuredExceptionHandling) Block[null]
    }
}
Block[B10] - Exit
    Predecessors: [B5]
    Statements (0)";

            VerifyFlowGraphAndDiagnosticsForTest<BlockSyntax>(source, expectedFlowGraph, expectedDiagnostics, parseOptions: TestOptions.Regular9);
        }

        [CompilerTrait(CompilerFeature.IOperation, CompilerFeature.Dataflow)]
        [Fact, WorkItem(17602, "https://github.com/dotnet/roslyn/issues/17602")]
        public void AwaitForeachFlow_ViaExtensionMethod()
        {
            var source = @"
using System.Collections.Generic;
class Program
{
    static async void M()
    /*<bind>*/{
        await foreach (var value in new Program())
        {
            System.Console.WriteLine(value);
        }
    }/*</bind>*/
}

static class Extensions
{
    public static IAsyncEnumerator<string> GetAsyncEnumerator(this Program p) => throw null;
}
";
            var expectedDiagnostics = DiagnosticDescription.None;

            var expectedFlowGraph = @"
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
            IFlowCaptureOperation: 0 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'new Program()')
              Value:
                IInvocationOperation (System.Collections.Generic.IAsyncEnumerator<System.String> Extensions.GetAsyncEnumerator(this Program p)) (OperationKind.Invocation, Type: System.Collections.Generic.IAsyncEnumerator<System.String>, IsImplicit) (Syntax: 'new Program()')
                  Instance Receiver:
                    null
                  Arguments(1):
                      IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: p) (OperationKind.Argument, Type: null, IsImplicit) (Syntax: 'new Program()')
                        IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: Program, IsImplicit) (Syntax: 'new Program()')
                          Conversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                            (Identity)
                          Operand:
                            IObjectCreationOperation (Constructor: Program..ctor()) (OperationKind.ObjectCreation, Type: Program) (Syntax: 'new Program()')
                              Arguments(0)
                              Initializer:
                                null
                        InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                        OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
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
                    IInvocationOperation (virtual System.Threading.Tasks.ValueTask<System.Boolean> System.Collections.Generic.IAsyncEnumerator<System.String>.MoveNextAsync()) (OperationKind.Invocation, Type: System.Threading.Tasks.ValueTask<System.Boolean>, IsImplicit) (Syntax: 'new Program()')
                      Instance Receiver:
                        IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: System.Collections.Generic.IAsyncEnumerator<System.String>, IsImplicit) (Syntax: 'new Program()')
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
                    ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: null, IsImplicit) (Syntax: 'var')
                      Left:
                        ILocalReferenceOperation: value (IsDeclaration: True) (OperationKind.LocalReference, Type: System.String, IsImplicit) (Syntax: 'var')
                      Right:
                        IPropertyReferenceOperation: System.String System.Collections.Generic.IAsyncEnumerator<System.String>.Current { get; } (OperationKind.PropertyReference, Type: System.String, IsImplicit) (Syntax: 'var')
                          Instance Receiver:
                            IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: System.Collections.Generic.IAsyncEnumerator<System.String>, IsImplicit) (Syntax: 'new Program()')
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
        Block[B4] - Block
            Predecessors (0)
            Statements (0)
            Jump if True (Regular) to Block[B6]
                IIsNullOperation (OperationKind.IsNull, Type: System.Boolean, IsImplicit) (Syntax: 'new Program()')
                  Operand:
                    IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: System.Collections.Generic.IAsyncEnumerator<System.String>, IsImplicit) (Syntax: 'new Program()')
            Next (Regular) Block[B5]
        Block[B5] - Block
            Predecessors: [B4]
            Statements (1)
                IAwaitOperation (OperationKind.Await, Type: System.Void, IsImplicit) (Syntax: 'new Program()')
                  Expression:
                    IInvocationOperation (virtual System.Threading.Tasks.ValueTask System.IAsyncDisposable.DisposeAsync()) (OperationKind.Invocation, Type: System.Threading.Tasks.ValueTask, IsImplicit) (Syntax: 'new Program()')
                      Instance Receiver:
                        IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: System.Collections.Generic.IAsyncEnumerator<System.String>, IsImplicit) (Syntax: 'new Program()')
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
    Statements (0)";

            var comp = CreateCompilationWithTasksExtensions(new[] { source, s_IAsyncEnumerable }, parseOptions: TestOptions.Regular9);
            VerifyFlowGraphAndDiagnosticsForTest<BlockSyntax>(comp, expectedFlowGraph, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation, CompilerFeature.Dataflow)]
        [Fact, WorkItem(17602, "https://github.com/dotnet/roslyn/issues/17602")]
        public void AwaitForeachFlow_ViaExtensionMethodWithConversion()
        {
            var source = @"
using System.Collections.Generic;
class Program
{
    static async void M()
    /*<bind>*/{
        await foreach (var value in new Program())
        {
            System.Console.WriteLine(value);
        }
    }/*</bind>*/
}


static class Extensions
{
    public static IAsyncEnumerator<string> GetAsyncEnumerator(this object p) => throw null;
}
";
            var expectedDiagnostics = DiagnosticDescription.None;

            var expectedFlowGraph = @"
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
            IFlowCaptureOperation: 0 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'new Program()')
              Value:
                IInvocationOperation (System.Collections.Generic.IAsyncEnumerator<System.String> Extensions.GetAsyncEnumerator(this System.Object p)) (OperationKind.Invocation, Type: System.Collections.Generic.IAsyncEnumerator<System.String>, IsImplicit) (Syntax: 'new Program()')
                  Instance Receiver:
                    null
                  Arguments(1):
                      IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: p) (OperationKind.Argument, Type: null, IsImplicit) (Syntax: 'new Program()')
                        IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.Object, IsImplicit) (Syntax: 'new Program()')
                          Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: True, IsUserDefined: False) (MethodSymbol: null)
                            (ImplicitReference)
                          Operand:
                            IObjectCreationOperation (Constructor: Program..ctor()) (OperationKind.ObjectCreation, Type: Program) (Syntax: 'new Program()')
                              Arguments(0)
                              Initializer:
                                null
                        InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                        OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
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
                    IInvocationOperation (virtual System.Threading.Tasks.ValueTask<System.Boolean> System.Collections.Generic.IAsyncEnumerator<System.String>.MoveNextAsync()) (OperationKind.Invocation, Type: System.Threading.Tasks.ValueTask<System.Boolean>, IsImplicit) (Syntax: 'new Program()')
                      Instance Receiver:
                        IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: System.Collections.Generic.IAsyncEnumerator<System.String>, IsImplicit) (Syntax: 'new Program()')
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
                    ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: null, IsImplicit) (Syntax: 'var')
                      Left:
                        ILocalReferenceOperation: value (IsDeclaration: True) (OperationKind.LocalReference, Type: System.String, IsImplicit) (Syntax: 'var')
                      Right:
                        IPropertyReferenceOperation: System.String System.Collections.Generic.IAsyncEnumerator<System.String>.Current { get; } (OperationKind.PropertyReference, Type: System.String, IsImplicit) (Syntax: 'var')
                          Instance Receiver:
                            IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: System.Collections.Generic.IAsyncEnumerator<System.String>, IsImplicit) (Syntax: 'new Program()')
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
        Block[B4] - Block
            Predecessors (0)
            Statements (0)
            Jump if True (Regular) to Block[B6]
                IIsNullOperation (OperationKind.IsNull, Type: System.Boolean, IsImplicit) (Syntax: 'new Program()')
                  Operand:
                    IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: System.Collections.Generic.IAsyncEnumerator<System.String>, IsImplicit) (Syntax: 'new Program()')
            Next (Regular) Block[B5]
        Block[B5] - Block
            Predecessors: [B4]
            Statements (1)
                IAwaitOperation (OperationKind.Await, Type: System.Void, IsImplicit) (Syntax: 'new Program()')
                  Expression:
                    IInvocationOperation (virtual System.Threading.Tasks.ValueTask System.IAsyncDisposable.DisposeAsync()) (OperationKind.Invocation, Type: System.Threading.Tasks.ValueTask, IsImplicit) (Syntax: 'new Program()')
                      Instance Receiver:
                        IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: System.Collections.Generic.IAsyncEnumerator<System.String>, IsImplicit) (Syntax: 'new Program()')
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
    Statements (0)";

            var comp = CreateCompilationWithTasksExtensions(new[] { source, s_IAsyncEnumerable }, parseOptions: TestOptions.Regular9);
            VerifyFlowGraphAndDiagnosticsForTest<BlockSyntax>(comp, expectedFlowGraph, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation, CompilerFeature.Dataflow)]
        [Fact, WorkItem(17602, "https://github.com/dotnet/roslyn/issues/17602")]
        public void AwaitForeachFlow_ViaExtensionMethod_WithGetAsyncEnumeratorReturningWrongType()
        {
            var source = @"
using System.Collections.Generic;
class Program
{
    static async void M()
    /*<bind>*/{
        await foreach (var value in new Program())
        {
            System.Console.WriteLine(value);
        }
    }/*</bind>*/
}

static class Extensions
{
    public static bool GetAsyncEnumerator(this Program p) => throw null;
}
";
            var expectedDiagnostics = new[] {
                // (7,37): error CS0117: 'bool' does not contain a definition for 'Current'
                //         await foreach (var value in new Program())
                Diagnostic(ErrorCode.ERR_NoSuchMember, "new Program()").WithArguments("bool", "Current").WithLocation(7, 37),
                // (7,37): error CS8412: Asynchronous foreach requires that the return type 'bool' of 'Extensions.GetAsyncEnumerator(Program)' must have a suitable public 'MoveNextAsync' method and public 'Current' property
                //         await foreach (var value in new Program())
                Diagnostic(ErrorCode.ERR_BadGetAsyncEnumerator, "new Program()").WithArguments("bool", "Extensions.GetAsyncEnumerator(Program)").WithLocation(7, 37)
            };

            var expectedFlowGraph = @"
Block[B0] - Entry
    Statements (0)
    Next (Regular) Block[B1]
Block[B1] - Block
    Predecessors: [B0]
    Statements (1)
        IInvalidOperation (OperationKind.Invalid, Type: null, IsInvalid, IsImplicit) (Syntax: 'new Program()')
          Children(1):
              IObjectCreationOperation (Constructor: Program..ctor()) (OperationKind.ObjectCreation, Type: Program, IsInvalid) (Syntax: 'new Program()')
                Arguments(0)
                Initializer: 
                  null
    Next (Regular) Block[B2]
Block[B2] - Block
    Predecessors: [B1] [B3]
    Statements (0)
    Jump if False (Regular) to Block[B4]
        IInvalidOperation (OperationKind.Invalid, Type: System.Boolean, IsInvalid, IsImplicit) (Syntax: 'new Program()')
          Children(1):
              IInvalidOperation (OperationKind.Invalid, Type: null, IsInvalid, IsImplicit) (Syntax: 'new Program()')
                Children(0)
    Next (Regular) Block[B3]
        Entering: {R1}
.locals {R1}
{
    Locals: [var value]
    Block[B3] - Block
        Predecessors: [B2]
        Statements (2)
            ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: null, IsImplicit) (Syntax: 'var')
              Left: 
                ILocalReferenceOperation: value (IsDeclaration: True) (OperationKind.LocalReference, Type: var, IsImplicit) (Syntax: 'var')
              Right: 
                IInvalidOperation (OperationKind.Invalid, Type: null, IsInvalid, IsImplicit) (Syntax: 'new Program()')
                  Children(1):
                      IInvalidOperation (OperationKind.Invalid, Type: null, IsInvalid, IsImplicit) (Syntax: 'new Program()')
                        Children(0)
            IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'System.Cons ... ine(value);')
              Expression: 
                IInvalidOperation (OperationKind.Invalid, Type: System.Void) (Syntax: 'System.Cons ... Line(value)')
                  Children(2):
                      IOperation:  (OperationKind.None, Type: System.Console) (Syntax: 'System.Console')
                      ILocalReferenceOperation: value (OperationKind.LocalReference, Type: var) (Syntax: 'value')
        Next (Regular) Block[B2]
            Leaving: {R1}
}
Block[B4] - Exit
    Predecessors: [B2]
    Statements (0)";

            var comp = CreateCompilationWithTasksExtensions(new[] { source, s_IAsyncEnumerable }, parseOptions: TestOptions.Regular9);
            VerifyFlowGraphAndDiagnosticsForTest<BlockSyntax>(comp, expectedFlowGraph, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation, CompilerFeature.Dataflow)]
        [Fact, WorkItem(17602, "https://github.com/dotnet/roslyn/issues/17602")]
        public void AwaitForeachFlow_ViaExtensionMethod_WithSpillInExpression()
        {
            var source = @"
using System.Collections.Generic;
class Program
{
    static async void M()
    /*<bind>*/{
        await foreach (var value in null ?? new Program())
        {
            System.Console.WriteLine(value);
        }
    }/*</bind>*/
}

static class Extensions
{
    public static IAsyncEnumerator<string> GetAsyncEnumerator(this Program p) => throw null;
}
";
            var expectedDiagnostics = DiagnosticDescription.None;

            var expectedFlowGraph = @"
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
                    IFlowCaptureOperation: 0 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'null')
                      Value:
                        ILiteralOperation (OperationKind.Literal, Type: null, Constant: null) (Syntax: 'null')
                Jump if True (Regular) to Block[B3]
                    IIsNullOperation (OperationKind.IsNull, Type: System.Boolean, Constant: True, IsImplicit) (Syntax: 'null')
                      Operand:
                        IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: null, Constant: null, IsImplicit) (Syntax: 'null')
                    Leaving: {R3}
                Next (Regular) Block[B2]
            Block[B2] - Block [UnReachable]
                Predecessors: [B1]
                Statements (1)
                    IFlowCaptureOperation: 1 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'null')
                      Value:
                        IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: Program, IsImplicit) (Syntax: 'null')
                          Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: True, IsUserDefined: False) (MethodSymbol: null)
                            (ImplicitReference)
                          Operand:
                            IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: null, Constant: null, IsImplicit) (Syntax: 'null')
                Next (Regular) Block[B4]
                    Leaving: {R3}
        }
        Block[B3] - Block
            Predecessors: [B1]
            Statements (1)
                IFlowCaptureOperation: 1 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'new Program()')
                  Value:
                    IObjectCreationOperation (Constructor: Program..ctor()) (OperationKind.ObjectCreation, Type: Program) (Syntax: 'new Program()')
                      Arguments(0)
                      Initializer:
                        null
            Next (Regular) Block[B4]
        Block[B4] - Block
            Predecessors: [B2] [B3]
            Statements (1)
                IFlowCaptureOperation: 2 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'null ?? new Program()')
                  Value:
                    IInvocationOperation (System.Collections.Generic.IAsyncEnumerator<System.String> Extensions.GetAsyncEnumerator(this Program p)) (OperationKind.Invocation, Type: System.Collections.Generic.IAsyncEnumerator<System.String>, IsImplicit) (Syntax: 'null ?? new Program()')
                      Instance Receiver:
                        null
                      Arguments(1):
                          IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: p) (OperationKind.Argument, Type: null, IsImplicit) (Syntax: 'null ?? new Program()')
                            IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: Program, IsImplicit) (Syntax: 'null ?? new Program()')
                              Conversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                                (Identity)
                              Operand:
                                IFlowCaptureReferenceOperation: 1 (OperationKind.FlowCaptureReference, Type: Program, IsImplicit) (Syntax: 'null ?? new Program()')
                            InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                            OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
            Next (Regular) Block[B5]
                Leaving: {R2}
                Entering: {R4} {R5}
    }
    .try {R4, R5}
    {
        Block[B5] - Block
            Predecessors: [B4] [B6]
            Statements (0)
            Jump if False (Regular) to Block[B10]
                IAwaitOperation (OperationKind.Await, Type: System.Boolean, IsImplicit) (Syntax: 'await forea ... }')
                  Expression:
                    IInvocationOperation (virtual System.Threading.Tasks.ValueTask<System.Boolean> System.Collections.Generic.IAsyncEnumerator<System.String>.MoveNextAsync()) (OperationKind.Invocation, Type: System.Threading.Tasks.ValueTask<System.Boolean>, IsImplicit) (Syntax: 'null ?? new Program()')
                      Instance Receiver:
                        IFlowCaptureReferenceOperation: 2 (OperationKind.FlowCaptureReference, Type: System.Collections.Generic.IAsyncEnumerator<System.String>, IsImplicit) (Syntax: 'null ?? new Program()')
                      Arguments(0)
                Finalizing: {R7}
                Leaving: {R5} {R4} {R1}
            Next (Regular) Block[B6]
                Entering: {R6}
        .locals {R6}
        {
            Locals: [System.String value]
            Block[B6] - Block
                Predecessors: [B5]
                Statements (2)
                    ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: null, IsImplicit) (Syntax: 'var')
                      Left:
                        ILocalReferenceOperation: value (IsDeclaration: True) (OperationKind.LocalReference, Type: System.String, IsImplicit) (Syntax: 'var')
                      Right:
                        IPropertyReferenceOperation: System.String System.Collections.Generic.IAsyncEnumerator<System.String>.Current { get; } (OperationKind.PropertyReference, Type: System.String, IsImplicit) (Syntax: 'var')
                          Instance Receiver:
                            IFlowCaptureReferenceOperation: 2 (OperationKind.FlowCaptureReference, Type: System.Collections.Generic.IAsyncEnumerator<System.String>, IsImplicit) (Syntax: 'null ?? new Program()')
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
                Next (Regular) Block[B5]
                    Leaving: {R6}
        }
    }
    .finally {R7}
    {
        Block[B7] - Block
            Predecessors (0)
            Statements (0)
            Jump if True (Regular) to Block[B9]
                IIsNullOperation (OperationKind.IsNull, Type: System.Boolean, IsImplicit) (Syntax: 'null ?? new Program()')
                  Operand:
                    IFlowCaptureReferenceOperation: 2 (OperationKind.FlowCaptureReference, Type: System.Collections.Generic.IAsyncEnumerator<System.String>, IsImplicit) (Syntax: 'null ?? new Program()')
            Next (Regular) Block[B8]
        Block[B8] - Block
            Predecessors: [B7]
            Statements (1)
                IAwaitOperation (OperationKind.Await, Type: System.Void, IsImplicit) (Syntax: 'null ?? new Program()')
                  Expression:
                    IInvocationOperation (virtual System.Threading.Tasks.ValueTask System.IAsyncDisposable.DisposeAsync()) (OperationKind.Invocation, Type: System.Threading.Tasks.ValueTask, IsImplicit) (Syntax: 'null ?? new Program()')
                      Instance Receiver:
                        IFlowCaptureReferenceOperation: 2 (OperationKind.FlowCaptureReference, Type: System.Collections.Generic.IAsyncEnumerator<System.String>, IsImplicit) (Syntax: 'null ?? new Program()')
                      Arguments(0)
            Next (Regular) Block[B9]
        Block[B9] - Block
            Predecessors: [B7] [B8]
            Statements (0)
            Next (StructuredExceptionHandling) Block[null]
    }
}
Block[B10] - Exit
    Predecessors: [B5]
    Statements (0)";

            var comp = CreateCompilationWithTasksExtensions(new[] { source, s_IAsyncEnumerable }, parseOptions: TestOptions.Regular9);
            VerifyFlowGraphAndDiagnosticsForTest<BlockSyntax>(comp, expectedFlowGraph, expectedDiagnostics);
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
                  Arguments(1):
                      IArgumentOperation (ArgumentKind.DefaultValue, Matching Parameter: token) (OperationKind.Argument, Type: null, IsImplicit) (Syntax: 'await forea ... }')
                        IDefaultValueOperation (OperationKind.DefaultValue, Type: System.Threading.CancellationToken, IsImplicit) (Syntax: 'await forea ... }')
                        InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                        OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
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
        Block[B4] - Block
            Predecessors (0)
            Statements (0)
            Jump if True (Regular) to Block[B6]
                IIsNullOperation (OperationKind.IsNull, Type: System.Boolean, IsImplicit) (Syntax: 'pets')
                  Operand:
                    IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: System.Collections.Generic.IAsyncEnumerator<System.String>, IsImplicit) (Syntax: 'pets')
            Next (Regular) Block[B5]
        Block[B5] - Block
            Predecessors: [B4]
            Statements (1)
                IAwaitOperation (OperationKind.Await, Type: System.Void, IsImplicit) (Syntax: 'pets')
                  Expression:
                    IInvocationOperation (virtual System.Threading.Tasks.ValueTask System.IAsyncDisposable.DisposeAsync()) (OperationKind.Invocation, Type: System.Threading.Tasks.ValueTask, IsImplicit) (Syntax: 'pets')
                      Instance Receiver:
                        IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: System.Collections.Generic.IAsyncEnumerator<System.String>, IsImplicit) (Syntax: 'pets')
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
  IForEachLoopOperation (LoopKind.ForEach, Continue Label Id: 0, Exit Label Id: 1) (OperationKind.Loop, Type: null) (Syntax: 'await forea ... }')
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

        [CompilerTrait(CompilerFeature.IOperation, CompilerFeature.Dataflow, CompilerFeature.AsyncStreams)]
        [Fact, WorkItem(49267, "https://github.com/dotnet/roslyn/issues/49267")]
        public void AsyncForeach_StructEnumerator()
        {
            var compilation = CreateCompilation(@"
using System.Threading.Tasks;
class C
{
    static async Task Main()
    /*<bind>*/{
        await foreach (var i in new C())
        {
        }
    }/*</bind>*/
    public AsyncEnumerator GetAsyncEnumerator() => throw null;
    public struct AsyncEnumerator
    {
        public int Current => throw null;
        public async Task<bool> MoveNextAsync() => throw null;
        public async ValueTask DisposeAsync() => throw null;
    }
}", targetFramework: TargetFramework.NetCoreApp);

            VerifyOperationTreeAndDiagnosticsForTest<BlockSyntax>(compilation, @"
IBlockOperation (1 statements) (OperationKind.Block, Type: null) (Syntax: '{ ... }')
  IForEachLoopOperation (LoopKind.ForEach, IsAsynchronous, Continue Label Id: 0, Exit Label Id: 1) (OperationKind.Loop, Type: null) (Syntax: 'await forea ... }')
    Locals: Local_1: System.Int32 i
    LoopControlVariable: 
      IVariableDeclaratorOperation (Symbol: System.Int32 i) (OperationKind.VariableDeclarator, Type: null) (Syntax: 'var')
        Initializer: 
          null
    Collection: 
      IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: C, IsImplicit) (Syntax: 'new C()')
        Conversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
        Operand: 
          IObjectCreationOperation (Constructor: C..ctor()) (OperationKind.ObjectCreation, Type: C) (Syntax: 'new C()')
            Arguments(0)
            Initializer: 
              null
    Body: 
      IBlockOperation (0 statements) (OperationKind.Block, Type: null) (Syntax: '{ ... }')
    NextVariables(0)
            ", DiagnosticDescription.None);

            VerifyFlowGraphForTest<BlockSyntax>(compilation, @"
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
            IFlowCaptureOperation: 0 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'new C()')
              Value: 
                IInvocationOperation ( C.AsyncEnumerator C.GetAsyncEnumerator()) (OperationKind.Invocation, Type: C.AsyncEnumerator, IsImplicit) (Syntax: 'new C()')
                  Instance Receiver: 
                    IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: C, IsImplicit) (Syntax: 'new C()')
                      Conversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                        (Identity)
                      Operand: 
                        IObjectCreationOperation (Constructor: C..ctor()) (OperationKind.ObjectCreation, Type: C) (Syntax: 'new C()')
                          Arguments(0)
                          Initializer: 
                            null
                  Arguments(0)
        Next (Regular) Block[B2]
            Entering: {R2} {R3}
    .try {R2, R3}
    {
        Block[B2] - Block
            Predecessors: [B1] [B3]
            Statements (0)
            Jump if False (Regular) to Block[B5]
                IAwaitOperation (OperationKind.Await, Type: System.Boolean, IsImplicit) (Syntax: 'await forea ... }')
                  Expression: 
                    IInvocationOperation ( System.Threading.Tasks.Task<System.Boolean> C.AsyncEnumerator.MoveNextAsync()) (OperationKind.Invocation, Type: System.Threading.Tasks.Task<System.Boolean>, IsImplicit) (Syntax: 'new C()')
                      Instance Receiver: 
                        IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: C.AsyncEnumerator, IsImplicit) (Syntax: 'new C()')
                      Arguments(0)
                Finalizing: {R5}
                Leaving: {R3} {R2} {R1}
            Next (Regular) Block[B3]
                Entering: {R4}
        .locals {R4}
        {
            Locals: [System.Int32 i]
            Block[B3] - Block
                Predecessors: [B2]
                Statements (1)
                    ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: null, IsImplicit) (Syntax: 'var')
                      Left: 
                        ILocalReferenceOperation: i (IsDeclaration: True) (OperationKind.LocalReference, Type: System.Int32, IsImplicit) (Syntax: 'var')
                      Right: 
                        IPropertyReferenceOperation: System.Int32 C.AsyncEnumerator.Current { get; } (OperationKind.PropertyReference, Type: System.Int32, IsImplicit) (Syntax: 'var')
                          Instance Receiver: 
                            IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: C.AsyncEnumerator, IsImplicit) (Syntax: 'new C()')
                Next (Regular) Block[B2]
                    Leaving: {R4}
        }
    }
    .finally {R5}
    {
        Block[B4] - Block
            Predecessors (0)
            Statements (1)
                IAwaitOperation (OperationKind.Await, Type: System.Void, IsImplicit) (Syntax: 'new C()')
                  Expression: 
                    IInvocationOperation ( System.Threading.Tasks.ValueTask C.AsyncEnumerator.DisposeAsync()) (OperationKind.Invocation, Type: System.Threading.Tasks.ValueTask, IsImplicit) (Syntax: 'new C()')
                      Instance Receiver: 
                        IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: C.AsyncEnumerator, IsImplicit) (Syntax: 'new C()')
                      Arguments(0)
            Next (StructuredExceptionHandling) Block[null]
    }
}
Block[B5] - Exit
    Predecessors: [B2]
    Statements (0)
");
        }

        [CompilerTrait(CompilerFeature.IOperation, CompilerFeature.Dataflow, CompilerFeature.AsyncStreams)]
        [Fact, WorkItem(49267, "https://github.com/dotnet/roslyn/issues/49267")]
        public void AsyncForeach_StructEnumerator_ExplicitAsyncDisposeInterface()
        {
            var compilation = CreateCompilation(@"
using System.Threading.Tasks;
class C
{
    static async Task Main()
    /*<bind>*/{
        await foreach (var i in new C())
        {
        }
    }/*</bind>*/
    public AsyncEnumerator GetAsyncEnumerator() => throw null;
    public struct AsyncEnumerator : System.IAsyncDisposable
    {
        public int Current => throw null;
        public async Task<bool> MoveNextAsync() => throw null;
        public async ValueTask DisposeAsync() => throw null;
    }
}", targetFramework: TargetFramework.NetCoreApp);

            VerifyOperationTreeAndDiagnosticsForTest<BlockSyntax>(compilation, @"
IBlockOperation (1 statements) (OperationKind.Block, Type: null) (Syntax: '{ ... }')
  IForEachLoopOperation (LoopKind.ForEach, IsAsynchronous, Continue Label Id: 0, Exit Label Id: 1) (OperationKind.Loop, Type: null) (Syntax: 'await forea ... }')
    Locals: Local_1: System.Int32 i
    LoopControlVariable: 
      IVariableDeclaratorOperation (Symbol: System.Int32 i) (OperationKind.VariableDeclarator, Type: null) (Syntax: 'var')
        Initializer: 
          null
    Collection: 
      IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: C, IsImplicit) (Syntax: 'new C()')
        Conversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
        Operand: 
          IObjectCreationOperation (Constructor: C..ctor()) (OperationKind.ObjectCreation, Type: C) (Syntax: 'new C()')
            Arguments(0)
            Initializer: 
              null
    Body: 
      IBlockOperation (0 statements) (OperationKind.Block, Type: null) (Syntax: '{ ... }')
    NextVariables(0)
            ", DiagnosticDescription.None);

            VerifyFlowGraphForTest<BlockSyntax>(compilation, @"
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
            IFlowCaptureOperation: 0 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'new C()')
              Value:
                IInvocationOperation ( C.AsyncEnumerator C.GetAsyncEnumerator()) (OperationKind.Invocation, Type: C.AsyncEnumerator, IsImplicit) (Syntax: 'new C()')
                  Instance Receiver:
                    IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: C, IsImplicit) (Syntax: 'new C()')
                      Conversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                        (Identity)
                      Operand:
                        IObjectCreationOperation (Constructor: C..ctor()) (OperationKind.ObjectCreation, Type: C) (Syntax: 'new C()')
                          Arguments(0)
                          Initializer:
                            null
                  Arguments(0)
        Next (Regular) Block[B2]
            Entering: {R2} {R3}
    .try {R2, R3}
    {
        Block[B2] - Block
            Predecessors: [B1] [B3]
            Statements (0)
            Jump if False (Regular) to Block[B5]
                IAwaitOperation (OperationKind.Await, Type: System.Boolean, IsImplicit) (Syntax: 'await forea ... }')
                  Expression:
                    IInvocationOperation ( System.Threading.Tasks.Task<System.Boolean> C.AsyncEnumerator.MoveNextAsync()) (OperationKind.Invocation, Type: System.Threading.Tasks.Task<System.Boolean>, IsImplicit) (Syntax: 'new C()')
                      Instance Receiver:
                        IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: C.AsyncEnumerator, IsImplicit) (Syntax: 'new C()')
                      Arguments(0)
                Finalizing: {R5}
                Leaving: {R3} {R2} {R1}
            Next (Regular) Block[B3]
                Entering: {R4}
        .locals {R4}
        {
            Locals: [System.Int32 i]
            Block[B3] - Block
                Predecessors: [B2]
                Statements (1)
                    ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: null, IsImplicit) (Syntax: 'var')
                      Left:
                        ILocalReferenceOperation: i (IsDeclaration: True) (OperationKind.LocalReference, Type: System.Int32, IsImplicit) (Syntax: 'var')
                      Right:
                        IPropertyReferenceOperation: System.Int32 C.AsyncEnumerator.Current { get; } (OperationKind.PropertyReference, Type: System.Int32, IsImplicit) (Syntax: 'var')
                          Instance Receiver:
                            IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: C.AsyncEnumerator, IsImplicit) (Syntax: 'new C()')
                Next (Regular) Block[B2]
                    Leaving: {R4}
        }
    }
    .finally {R5}
    {
        Block[B4] - Block
            Predecessors (0)
            Statements (1)
                IAwaitOperation (OperationKind.Await, Type: System.Void, IsImplicit) (Syntax: 'new C()')
                  Expression:
                    IInvocationOperation ( System.Threading.Tasks.ValueTask C.AsyncEnumerator.DisposeAsync()) (OperationKind.Invocation, Type: System.Threading.Tasks.ValueTask, IsImplicit) (Syntax: 'new C()')
                      Instance Receiver:
                        IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: C.AsyncEnumerator, IsImplicit) (Syntax: 'new C()')
                      Arguments(0)
            Next (StructuredExceptionHandling) Block[null]
    }
}
Block[B5] - Exit
    Predecessors: [B2]
    Statements (0)
");
        }

        [CompilerTrait(CompilerFeature.IOperation, CompilerFeature.Dataflow, CompilerFeature.AsyncStreams)]
        [Fact, WorkItem(49267, "https://github.com/dotnet/roslyn/issues/49267")]
        public void Foreach_StructEnumerator()
        {
            var compilation = CreateCompilation(@"
class C
{
    static void Main()
    /*<bind>*/{
        foreach (var i in new C())
        {
        } 
    }/*</bind>*/

    public Enumerator GetEnumerator() => throw null;
    public struct Enumerator : System.IDisposable
    {
        public int Current => throw null;
        public bool MoveNext() => throw null;
        public void Dispose() => throw null;
    }
}", targetFramework: TargetFramework.NetCoreApp);

            VerifyOperationTreeAndDiagnosticsForTest<BlockSyntax>(compilation, @"
IBlockOperation (1 statements) (OperationKind.Block, Type: null) (Syntax: '{ ... }')
  IForEachLoopOperation (LoopKind.ForEach, Continue Label Id: 0, Exit Label Id: 1) (OperationKind.Loop, Type: null) (Syntax: 'foreach (va ... }')
    Locals: Local_1: System.Int32 i
    LoopControlVariable: 
      IVariableDeclaratorOperation (Symbol: System.Int32 i) (OperationKind.VariableDeclarator, Type: null) (Syntax: 'var')
        Initializer: 
          null
    Collection: 
      IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: C, IsImplicit) (Syntax: 'new C()')
        Conversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
        Operand: 
          IObjectCreationOperation (Constructor: C..ctor()) (OperationKind.ObjectCreation, Type: C) (Syntax: 'new C()')
            Arguments(0)
            Initializer: 
              null
    Body: 
      IBlockOperation (0 statements) (OperationKind.Block, Type: null) (Syntax: '{ ... }')
    NextVariables(0)", DiagnosticDescription.None);

            VerifyFlowGraphForTest<BlockSyntax>(compilation, @"
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
            IFlowCaptureOperation: 0 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'new C()')
              Value: 
                IInvocationOperation ( C.Enumerator C.GetEnumerator()) (OperationKind.Invocation, Type: C.Enumerator, IsImplicit) (Syntax: 'new C()')
                  Instance Receiver: 
                    IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: C, IsImplicit) (Syntax: 'new C()')
                      Conversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                        (Identity)
                      Operand: 
                        IObjectCreationOperation (Constructor: C..ctor()) (OperationKind.ObjectCreation, Type: C) (Syntax: 'new C()')
                          Arguments(0)
                          Initializer: 
                            null
                  Arguments(0)
        Next (Regular) Block[B2]
            Entering: {R2} {R3}
    .try {R2, R3}
    {
        Block[B2] - Block
            Predecessors: [B1] [B3]
            Statements (0)
            Jump if False (Regular) to Block[B5]
                IInvocationOperation ( System.Boolean C.Enumerator.MoveNext()) (OperationKind.Invocation, Type: System.Boolean, IsImplicit) (Syntax: 'new C()')
                  Instance Receiver: 
                    IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: C.Enumerator, IsImplicit) (Syntax: 'new C()')
                  Arguments(0)
                Finalizing: {R5}
                Leaving: {R3} {R2} {R1}
            Next (Regular) Block[B3]
                Entering: {R4}
        .locals {R4}
        {
            Locals: [System.Int32 i]
            Block[B3] - Block
                Predecessors: [B2]
                Statements (1)
                    ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: null, IsImplicit) (Syntax: 'var')
                      Left: 
                        ILocalReferenceOperation: i (IsDeclaration: True) (OperationKind.LocalReference, Type: System.Int32, IsImplicit) (Syntax: 'var')
                      Right: 
                        IPropertyReferenceOperation: System.Int32 C.Enumerator.Current { get; } (OperationKind.PropertyReference, Type: System.Int32, IsImplicit) (Syntax: 'var')
                          Instance Receiver: 
                            IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: C.Enumerator, IsImplicit) (Syntax: 'new C()')
                Next (Regular) Block[B2]
                    Leaving: {R4}
        }
    }
    .finally {R5}
    {
        Block[B4] - Block
            Predecessors (0)
            Statements (1)
                IInvocationOperation (virtual void System.IDisposable.Dispose()) (OperationKind.Invocation, Type: System.Void, IsImplicit) (Syntax: 'new C()')
                  Instance Receiver: 
                    IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: C.Enumerator, IsImplicit) (Syntax: 'new C()')
                  Arguments(0)
            Next (StructuredExceptionHandling) Block[null]
    }
}
Block[B5] - Exit
    Predecessors: [B2]
    Statements (0)
");
        }

        [CompilerTrait(CompilerFeature.IOperation, CompilerFeature.Dataflow, CompilerFeature.AsyncStreams)]
        [Fact, WorkItem(49267, "https://github.com/dotnet/roslyn/issues/49267")]
        public void Foreach_RefStructEnumerator()
        {
            var compilation = CreateCompilation(@"
class C
{
    static void Main()
    /*<bind>*/{
        foreach (var i in new C())
        {
        } 
    }/*</bind>*/

    public Enumerator GetEnumerator() => throw null;
    public ref struct Enumerator
    {
        public int Current => throw null;
        public bool MoveNext() => throw null;
        public void Dispose() => throw null;
    }
}", targetFramework: TargetFramework.NetCoreApp);

            VerifyOperationTreeAndDiagnosticsForTest<BlockSyntax>(compilation, @"
IBlockOperation (1 statements) (OperationKind.Block, Type: null) (Syntax: '{ ... }')
  IForEachLoopOperation (LoopKind.ForEach, Continue Label Id: 0, Exit Label Id: 1) (OperationKind.Loop, Type: null) (Syntax: 'foreach (va ... }')
    Locals: Local_1: System.Int32 i
    LoopControlVariable: 
      IVariableDeclaratorOperation (Symbol: System.Int32 i) (OperationKind.VariableDeclarator, Type: null) (Syntax: 'var')
        Initializer: 
          null
    Collection: 
      IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: C, IsImplicit) (Syntax: 'new C()')
        Conversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
        Operand: 
          IObjectCreationOperation (Constructor: C..ctor()) (OperationKind.ObjectCreation, Type: C) (Syntax: 'new C()')
            Arguments(0)
            Initializer: 
              null
    Body: 
      IBlockOperation (0 statements) (OperationKind.Block, Type: null) (Syntax: '{ ... }')
    NextVariables(0)", DiagnosticDescription.None);

            VerifyFlowGraphForTest<BlockSyntax>(compilation, @"
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
            IFlowCaptureOperation: 0 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'new C()')
              Value: 
                IInvocationOperation ( C.Enumerator C.GetEnumerator()) (OperationKind.Invocation, Type: C.Enumerator, IsImplicit) (Syntax: 'new C()')
                  Instance Receiver: 
                    IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: C, IsImplicit) (Syntax: 'new C()')
                      Conversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                        (Identity)
                      Operand: 
                        IObjectCreationOperation (Constructor: C..ctor()) (OperationKind.ObjectCreation, Type: C) (Syntax: 'new C()')
                          Arguments(0)
                          Initializer: 
                            null
                  Arguments(0)
        Next (Regular) Block[B2]
            Entering: {R2} {R3}
    .try {R2, R3}
    {
        Block[B2] - Block
            Predecessors: [B1] [B3]
            Statements (0)
            Jump if False (Regular) to Block[B5]
                IInvocationOperation ( System.Boolean C.Enumerator.MoveNext()) (OperationKind.Invocation, Type: System.Boolean, IsImplicit) (Syntax: 'new C()')
                  Instance Receiver: 
                    IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: C.Enumerator, IsImplicit) (Syntax: 'new C()')
                  Arguments(0)
                Finalizing: {R5}
                Leaving: {R3} {R2} {R1}
            Next (Regular) Block[B3]
                Entering: {R4}
        .locals {R4}
        {
            Locals: [System.Int32 i]
            Block[B3] - Block
                Predecessors: [B2]
                Statements (1)
                    ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: null, IsImplicit) (Syntax: 'var')
                      Left: 
                        ILocalReferenceOperation: i (IsDeclaration: True) (OperationKind.LocalReference, Type: System.Int32, IsImplicit) (Syntax: 'var')
                      Right: 
                        IPropertyReferenceOperation: System.Int32 C.Enumerator.Current { get; } (OperationKind.PropertyReference, Type: System.Int32, IsImplicit) (Syntax: 'var')
                          Instance Receiver: 
                            IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: C.Enumerator, IsImplicit) (Syntax: 'new C()')
                Next (Regular) Block[B2]
                    Leaving: {R4}
        }
    }
    .finally {R5}
    {
        Block[B4] - Block
            Predecessors (0)
            Statements (1)
                IInvocationOperation ( void C.Enumerator.Dispose()) (OperationKind.Invocation, Type: System.Void, IsImplicit) (Syntax: 'new C()')
                  Instance Receiver: 
                    IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: C.Enumerator, IsImplicit) (Syntax: 'new C()')
                  Arguments(0)
            Next (StructuredExceptionHandling) Block[null]
    }
}
Block[B5] - Exit
    Predecessors: [B2]
    Statements (0)
");
        }

        [Fact]
        public void AsyncForEach_TestConstantNullableImplementingIEnumerable()
        {
            var source = @"
using System;
using System.Threading.Tasks;
using System.Collections.Generic;
public struct C : IAsyncEnumerable<int>
{
    public static async Task Main()
    /*<bind>*/{
        await foreach (var i in (C?)null)
        {
        }
    }/*</bind>*/
    IAsyncEnumerator<int> IAsyncEnumerable<int>.GetAsyncEnumerator(System.Threading.CancellationToken cancellationToken) => throw null;
}";
            var comp = CreateCompilationWithTasksExtensions(new[] { source, AsyncStreamsTypes }, options: TestOptions.DebugExe);
            VerifyOperationTreeAndDiagnosticsForTest<BlockSyntax>(comp, expectedDiagnostics: DiagnosticDescription.None, expectedOperationTree: @"
IBlockOperation (1 statements) (OperationKind.Block, Type: null) (Syntax: '{ ... }')
  IForEachLoopOperation (LoopKind.ForEach, IsAsynchronous, Continue Label Id: 0, Exit Label Id: 1) (OperationKind.Loop, Type: null) (Syntax: 'await forea ... }')
    Locals: Local_1: System.Int32 i
    LoopControlVariable: 
      IVariableDeclaratorOperation (Symbol: System.Int32 i) (OperationKind.VariableDeclarator, Type: null) (Syntax: 'var')
        Initializer: 
          null
    Collection: 
      IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.Collections.Generic.IAsyncEnumerable<System.Int32>, IsImplicit) (Syntax: '(C?)null')
        Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
        Operand: 
          IInvocationOperation ( C C?.Value.get) (OperationKind.Invocation, Type: C, IsImplicit) (Syntax: '(C?)null')
            Instance Receiver: 
              IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: C?, Constant: null) (Syntax: '(C?)null')
                Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                Operand: 
                  ILiteralOperation (OperationKind.Literal, Type: null, Constant: null) (Syntax: 'null')
            Arguments(0)
    Body: 
      IBlockOperation (0 statements) (OperationKind.Block, Type: null) (Syntax: '{ ... }')
    NextVariables(0)
");

            VerifyFlowGraphForTest<BlockSyntax>(comp, @"
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
            IFlowCaptureOperation: 0 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: '(C?)null')
              Value: 
                IInvocationOperation (virtual System.Collections.Generic.IAsyncEnumerator<System.Int32> System.Collections.Generic.IAsyncEnumerable<System.Int32>.GetAsyncEnumerator([System.Threading.CancellationToken token = default(System.Threading.CancellationToken)])) (OperationKind.Invocation, Type: System.Collections.Generic.IAsyncEnumerator<System.Int32>, IsImplicit) (Syntax: '(C?)null')
                  Instance Receiver: 
                    IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.Collections.Generic.IAsyncEnumerable<System.Int32>, IsImplicit) (Syntax: '(C?)null')
                      Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                        (Boxing)
                      Operand: 
                        IInvocationOperation ( C C?.Value.get) (OperationKind.Invocation, Type: C, IsImplicit) (Syntax: '(C?)null')
                          Instance Receiver: 
                            IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: C?, Constant: null) (Syntax: '(C?)null')
                              Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                                (NullLiteral)
                              Operand: 
                                ILiteralOperation (OperationKind.Literal, Type: null, Constant: null) (Syntax: 'null')
                          Arguments(0)
                  Arguments(1):
                      IArgumentOperation (ArgumentKind.DefaultValue, Matching Parameter: token) (OperationKind.Argument, Type: null, IsImplicit) (Syntax: '(C?)null')
                        IDefaultValueOperation (OperationKind.DefaultValue, Type: System.Threading.CancellationToken, IsImplicit) (Syntax: '(C?)null')
                        InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                        OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
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
                    IInvocationOperation (virtual System.Threading.Tasks.ValueTask<System.Boolean> System.Collections.Generic.IAsyncEnumerator<System.Int32>.MoveNextAsync()) (OperationKind.Invocation, Type: System.Threading.Tasks.ValueTask<System.Boolean>, IsImplicit) (Syntax: '(C?)null')
                      Instance Receiver: 
                        IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: System.Collections.Generic.IAsyncEnumerator<System.Int32>, IsImplicit) (Syntax: '(C?)null')
                      Arguments(0)
                Finalizing: {R5}
                Leaving: {R3} {R2} {R1}
            Next (Regular) Block[B3]
                Entering: {R4}
        .locals {R4}
        {
            Locals: [System.Int32 i]
            Block[B3] - Block
                Predecessors: [B2]
                Statements (1)
                    ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: null, IsImplicit) (Syntax: 'var')
                      Left: 
                        ILocalReferenceOperation: i (IsDeclaration: True) (OperationKind.LocalReference, Type: System.Int32, IsImplicit) (Syntax: 'var')
                      Right: 
                        IPropertyReferenceOperation: System.Int32 System.Collections.Generic.IAsyncEnumerator<System.Int32>.Current { get; } (OperationKind.PropertyReference, Type: System.Int32, IsImplicit) (Syntax: 'var')
                          Instance Receiver: 
                            IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: System.Collections.Generic.IAsyncEnumerator<System.Int32>, IsImplicit) (Syntax: '(C?)null')
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
                IIsNullOperation (OperationKind.IsNull, Type: System.Boolean, IsImplicit) (Syntax: '(C?)null')
                  Operand: 
                    IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: System.Collections.Generic.IAsyncEnumerator<System.Int32>, IsImplicit) (Syntax: '(C?)null')
            Next (Regular) Block[B5]
        Block[B5] - Block
            Predecessors: [B4]
            Statements (1)
                IAwaitOperation (OperationKind.Await, Type: System.Void, IsImplicit) (Syntax: '(C?)null')
                  Expression: 
                    IInvocationOperation (virtual System.Threading.Tasks.ValueTask System.IAsyncDisposable.DisposeAsync()) (OperationKind.Invocation, Type: System.Threading.Tasks.ValueTask, IsImplicit) (Syntax: '(C?)null')
                      Instance Receiver: 
                        IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.IAsyncDisposable, IsImplicit) (Syntax: '(C?)null')
                          Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: True, IsUserDefined: False) (MethodSymbol: null)
                            (ImplicitReference)
                          Operand: 
                            IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: System.Collections.Generic.IAsyncEnumerator<System.Int32>, IsImplicit) (Syntax: '(C?)null')
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
");
        }

        [CompilerTrait(CompilerFeature.IOperation, CompilerFeature.Dataflow)]
        [Fact]
        public void Foreach_RefStructEnumerator_DefaultDisposeArguments()
        {
            var compilation = CreateCompilation(@"
class C
{
    static void Main()
    /*<bind>*/{
        foreach (var i in new C())
        {
        } 
    }/*</bind>*/

    public Enumerator GetEnumerator() => throw null;
    public ref struct Enumerator
    {
        public int Current => throw null;
        public bool MoveNext() => throw null;
        public void Dispose(int a = 1, bool b = true, params object[] extras) => throw null;
    }
}", targetFramework: TargetFramework.NetCoreApp);

            VerifyOperationTreeAndDiagnosticsForTest<BlockSyntax>(compilation, @"
IBlockOperation (1 statements) (OperationKind.Block, Type: null) (Syntax: '{ ... }')
  IForEachLoopOperation (LoopKind.ForEach, Continue Label Id: 0, Exit Label Id: 1) (OperationKind.Loop, Type: null) (Syntax: 'foreach (va ... }')
    Locals: Local_1: System.Int32 i
    LoopControlVariable: 
      IVariableDeclaratorOperation (Symbol: System.Int32 i) (OperationKind.VariableDeclarator, Type: null) (Syntax: 'var')
        Initializer: 
          null
    Collection: 
      IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: C, IsImplicit) (Syntax: 'new C()')
        Conversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
        Operand: 
          IObjectCreationOperation (Constructor: C..ctor()) (OperationKind.ObjectCreation, Type: C) (Syntax: 'new C()')
            Arguments(0)
            Initializer: 
              null
    Body: 
      IBlockOperation (0 statements) (OperationKind.Block, Type: null) (Syntax: '{ ... }')
    NextVariables(0)", DiagnosticDescription.None);

            VerifyFlowGraphForTest<BlockSyntax>(compilation, @"
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
            IFlowCaptureOperation: 0 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'new C()')
              Value: 
                IInvocationOperation ( C.Enumerator C.GetEnumerator()) (OperationKind.Invocation, Type: C.Enumerator, IsImplicit) (Syntax: 'new C()')
                  Instance Receiver: 
                    IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: C, IsImplicit) (Syntax: 'new C()')
                      Conversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                        (Identity)
                      Operand: 
                        IObjectCreationOperation (Constructor: C..ctor()) (OperationKind.ObjectCreation, Type: C) (Syntax: 'new C()')
                          Arguments(0)
                          Initializer: 
                            null
                  Arguments(0)
        Next (Regular) Block[B2]
            Entering: {R2} {R3}
    .try {R2, R3}
    {
        Block[B2] - Block
            Predecessors: [B1] [B3]
            Statements (0)
            Jump if False (Regular) to Block[B5]
                IInvocationOperation ( System.Boolean C.Enumerator.MoveNext()) (OperationKind.Invocation, Type: System.Boolean, IsImplicit) (Syntax: 'new C()')
                  Instance Receiver: 
                    IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: C.Enumerator, IsImplicit) (Syntax: 'new C()')
                  Arguments(0)
                Finalizing: {R5}
                Leaving: {R3} {R2} {R1}
            Next (Regular) Block[B3]
                Entering: {R4}
        .locals {R4}
        {
            Locals: [System.Int32 i]
            Block[B3] - Block
                Predecessors: [B2]
                Statements (1)
                    ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: null, IsImplicit) (Syntax: 'var')
                      Left: 
                        ILocalReferenceOperation: i (IsDeclaration: True) (OperationKind.LocalReference, Type: System.Int32, IsImplicit) (Syntax: 'var')
                      Right: 
                        IPropertyReferenceOperation: System.Int32 C.Enumerator.Current { get; } (OperationKind.PropertyReference, Type: System.Int32, IsImplicit) (Syntax: 'var')
                          Instance Receiver: 
                            IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: C.Enumerator, IsImplicit) (Syntax: 'new C()')
                Next (Regular) Block[B2]
                    Leaving: {R4}
        }
    }
    .finally {R5}
    {
        Block[B4] - Block
            Predecessors (0)
            Statements (1)
                IInvocationOperation ( void C.Enumerator.Dispose([System.Int32 a = 1], [System.Boolean b = true], params System.Object[] extras)) (OperationKind.Invocation, Type: System.Void, IsImplicit) (Syntax: 'new C()')
                  Instance Receiver: 
                    IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: C.Enumerator, IsImplicit) (Syntax: 'new C()')
                  Arguments(3):
                      IArgumentOperation (ArgumentKind.DefaultValue, Matching Parameter: a) (OperationKind.Argument, Type: null, IsImplicit) (Syntax: 'foreach (va ... }')
                        ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 1, IsImplicit) (Syntax: 'foreach (va ... }')
                        InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                        OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                      IArgumentOperation (ArgumentKind.DefaultValue, Matching Parameter: b) (OperationKind.Argument, Type: null, IsImplicit) (Syntax: 'foreach (va ... }')
                        ILiteralOperation (OperationKind.Literal, Type: System.Boolean, Constant: True, IsImplicit) (Syntax: 'foreach (va ... }')
                        InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                        OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                      IArgumentOperation (ArgumentKind.ParamArray, Matching Parameter: extras) (OperationKind.Argument, Type: null, IsImplicit) (Syntax: 'foreach (va ... }')
                        IArrayCreationOperation (OperationKind.ArrayCreation, Type: System.Object[], IsImplicit) (Syntax: 'foreach (va ... }')
                          Dimension Sizes(1):
                              ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 0, IsImplicit) (Syntax: 'foreach (va ... }')
                          Initializer: 
                            IArrayInitializerOperation (0 elements) (OperationKind.ArrayInitializer, Type: null, IsImplicit) (Syntax: 'foreach (va ... }')
                              Element Values(0)
                        InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                        OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
            Next (StructuredExceptionHandling) Block[null]
    }
}
Block[B5] - Exit
    Predecessors: [B2]
    Statements (0)
");
        }

        [Fact]
        public void AsyncForeach_ExtensionGetEnumeratorWithParams()
        {
            string source = @"
using System;
using System.Threading.Tasks;
public class C
{
    public static async Task Main()
    /*<bind>*/{
        await foreach (var i in new C())
        {
            Console.Write(i);
        }
    }/*</bind>*/
    public sealed class Enumerator
    {
        public Enumerator() => throw null;
        public int Current { get; private set; }
        public Task<bool> MoveNextAsync() => throw null;
    }
}
public static class Extensions
{
    public static C.Enumerator GetAsyncEnumerator(this C self, params int[] x) => throw null;
}";
            var comp = CreateCompilationWithMscorlib46(source, options: TestOptions.DebugExe, parseOptions: TestOptions.Regular9);
            VerifyOperationTreeAndDiagnosticsForTest<BlockSyntax>(comp, @"
IBlockOperation (1 statements) (OperationKind.Block, Type: null) (Syntax: '{ ... }')
  IForEachLoopOperation (LoopKind.ForEach, IsAsynchronous, Continue Label Id: 0, Exit Label Id: 1) (OperationKind.Loop, Type: null) (Syntax: 'await forea ... }')
    Locals: Local_1: System.Int32 i
    LoopControlVariable: 
      IVariableDeclaratorOperation (Symbol: System.Int32 i) (OperationKind.VariableDeclarator, Type: null) (Syntax: 'var')
        Initializer: 
          null
    Collection: 
      IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: C, IsImplicit) (Syntax: 'new C()')
        Conversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
        Operand: 
          IObjectCreationOperation (Constructor: C..ctor()) (OperationKind.ObjectCreation, Type: C) (Syntax: 'new C()')
            Arguments(0)
            Initializer: 
              null
    Body: 
      IBlockOperation (1 statements) (OperationKind.Block, Type: null) (Syntax: '{ ... }')
        IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'Console.Write(i);')
          Expression: 
            IInvocationOperation (void System.Console.Write(System.Int32 value)) (OperationKind.Invocation, Type: System.Void) (Syntax: 'Console.Write(i)')
              Instance Receiver: 
                null
              Arguments(1):
                  IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: value) (OperationKind.Argument, Type: null) (Syntax: 'i')
                    ILocalReferenceOperation: i (OperationKind.LocalReference, Type: System.Int32) (Syntax: 'i')
                    InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                    OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
    NextVariables(0)
", DiagnosticDescription.None);

            VerifyFlowGraphForTest<BlockSyntax>(comp, @"
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
            IFlowCaptureOperation: 0 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'new C()')
              Value: 
                IInvocationOperation (C.Enumerator Extensions.GetAsyncEnumerator(this C self, params System.Int32[] x)) (OperationKind.Invocation, Type: C.Enumerator, IsImplicit) (Syntax: 'new C()')
                  Instance Receiver: 
                    null
                  Arguments(2):
                      IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: self) (OperationKind.Argument, Type: null, IsImplicit) (Syntax: 'new C()')
                        IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: C, IsImplicit) (Syntax: 'new C()')
                          Conversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                            (Identity)
                          Operand: 
                            IObjectCreationOperation (Constructor: C..ctor()) (OperationKind.ObjectCreation, Type: C) (Syntax: 'new C()')
                              Arguments(0)
                              Initializer: 
                                null
                        InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                        OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                      IArgumentOperation (ArgumentKind.ParamArray, Matching Parameter: x) (OperationKind.Argument, Type: null, IsImplicit) (Syntax: 'new C()')
                        IArrayCreationOperation (OperationKind.ArrayCreation, Type: System.Int32[], IsImplicit) (Syntax: 'new C()')
                          Dimension Sizes(1):
                              ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 0, IsImplicit) (Syntax: 'new C()')
                          Initializer: 
                            IArrayInitializerOperation (0 elements) (OperationKind.ArrayInitializer, Type: null, IsImplicit) (Syntax: 'new C()')
                              Element Values(0)
                        InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                        OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
        Next (Regular) Block[B2]
    Block[B2] - Block
        Predecessors: [B1] [B3]
        Statements (0)
        Jump if False (Regular) to Block[B4]
            IAwaitOperation (OperationKind.Await, Type: System.Boolean, IsImplicit) (Syntax: 'await forea ... }')
              Expression: 
                IInvocationOperation ( System.Threading.Tasks.Task<System.Boolean> C.Enumerator.MoveNextAsync()) (OperationKind.Invocation, Type: System.Threading.Tasks.Task<System.Boolean>, IsImplicit) (Syntax: 'new C()')
                  Instance Receiver: 
                    IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: C.Enumerator, IsImplicit) (Syntax: 'new C()')
                  Arguments(0)
            Leaving: {R1}
        Next (Regular) Block[B3]
            Entering: {R2}
    .locals {R2}
    {
        Locals: [System.Int32 i]
        Block[B3] - Block
            Predecessors: [B2]
            Statements (2)
                ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: null, IsImplicit) (Syntax: 'var')
                  Left: 
                    ILocalReferenceOperation: i (IsDeclaration: True) (OperationKind.LocalReference, Type: System.Int32, IsImplicit) (Syntax: 'var')
                  Right: 
                    IPropertyReferenceOperation: System.Int32 C.Enumerator.Current { get; private set; } (OperationKind.PropertyReference, Type: System.Int32, IsImplicit) (Syntax: 'var')
                      Instance Receiver: 
                        IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: C.Enumerator, IsImplicit) (Syntax: 'new C()')
                IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'Console.Write(i);')
                  Expression: 
                    IInvocationOperation (void System.Console.Write(System.Int32 value)) (OperationKind.Invocation, Type: System.Void) (Syntax: 'Console.Write(i)')
                      Instance Receiver: 
                        null
                      Arguments(1):
                          IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: value) (OperationKind.Argument, Type: null) (Syntax: 'i')
                            ILocalReferenceOperation: i (OperationKind.LocalReference, Type: System.Int32) (Syntax: 'i')
                            InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                            OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
            Next (Regular) Block[B2]
                Leaving: {R2}
    }
}
Block[B4] - Exit
    Predecessors: [B2]
    Statements (0)
");
        }

        [Fact]
        public void ForEach_ExtensionGetEnumeratorDefaultParam()
        {
            var comp = CreateCompilation(@"
public class C
{
    static void M(C c)
    /*<bind>*/{
        foreach (var i in c)
        {
        }
    }/*</bind>*/
}
public static class CExt
{
    public class Enumerator
    {
        public int Current => 1;
        public bool MoveNext() => false;
    }
    public static Enumerator GetEnumerator(this C c, int i = 1) => null;
}
");

            VerifyOperationTreeAndDiagnosticsForTest<BlockSyntax>(comp, @"
IBlockOperation (1 statements) (OperationKind.Block, Type: null) (Syntax: '{ ... }')
  IForEachLoopOperation (LoopKind.ForEach, Continue Label Id: 0, Exit Label Id: 1) (OperationKind.Loop, Type: null) (Syntax: 'foreach (va ... }')
    Locals: Local_1: System.Int32 i
    LoopControlVariable: 
      IVariableDeclaratorOperation (Symbol: System.Int32 i) (OperationKind.VariableDeclarator, Type: null) (Syntax: 'var')
        Initializer: 
          null
    Collection: 
      IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: C, IsImplicit) (Syntax: 'c')
        Conversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
        Operand: 
          IParameterReferenceOperation: c (OperationKind.ParameterReference, Type: C) (Syntax: 'c')
    Body: 
      IBlockOperation (0 statements) (OperationKind.Block, Type: null) (Syntax: '{ ... }')
    NextVariables(0)
", DiagnosticDescription.None);

            VerifyFlowGraphForTest<BlockSyntax>(comp, @"
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
            IFlowCaptureOperation: 0 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'c')
              Value: 
                IInvocationOperation (CExt.Enumerator CExt.GetEnumerator(this C c, [System.Int32 i = 1])) (OperationKind.Invocation, Type: CExt.Enumerator, IsImplicit) (Syntax: 'c')
                  Instance Receiver: 
                    null
                  Arguments(2):
                      IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: c) (OperationKind.Argument, Type: null, IsImplicit) (Syntax: 'c')
                        IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: C, IsImplicit) (Syntax: 'c')
                          Conversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                            (Identity)
                          Operand: 
                            IParameterReferenceOperation: c (OperationKind.ParameterReference, Type: C) (Syntax: 'c')
                        InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                        OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                      IArgumentOperation (ArgumentKind.DefaultValue, Matching Parameter: i) (OperationKind.Argument, Type: null, IsImplicit) (Syntax: 'c')
                        ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 1, IsImplicit) (Syntax: 'c')
                        InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                        OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
        Next (Regular) Block[B2]
            Entering: {R2} {R3}
    .try {R2, R3}
    {
        Block[B2] - Block
            Predecessors: [B1] [B3]
            Statements (0)
            Jump if False (Regular) to Block[B7]
                IInvocationOperation ( System.Boolean CExt.Enumerator.MoveNext()) (OperationKind.Invocation, Type: System.Boolean, IsImplicit) (Syntax: 'c')
                  Instance Receiver: 
                    IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: CExt.Enumerator, IsImplicit) (Syntax: 'c')
                  Arguments(0)
                Finalizing: {R5}
                Leaving: {R3} {R2} {R1}
            Next (Regular) Block[B3]
                Entering: {R4}
        .locals {R4}
        {
            Locals: [System.Int32 i]
            Block[B3] - Block
                Predecessors: [B2]
                Statements (1)
                    ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: null, IsImplicit) (Syntax: 'var')
                      Left: 
                        ILocalReferenceOperation: i (IsDeclaration: True) (OperationKind.LocalReference, Type: System.Int32, IsImplicit) (Syntax: 'var')
                      Right: 
                        IPropertyReferenceOperation: System.Int32 CExt.Enumerator.Current { get; } (OperationKind.PropertyReference, Type: System.Int32, IsImplicit) (Syntax: 'var')
                          Instance Receiver: 
                            IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: CExt.Enumerator, IsImplicit) (Syntax: 'c')
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
                IFlowCaptureOperation: 1 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'c')
                  Value: 
                    IConversionOperation (TryCast: True, Unchecked) (OperationKind.Conversion, Type: System.IDisposable, IsImplicit) (Syntax: 'c')
                      Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: True, IsUserDefined: False) (MethodSymbol: null)
                        (ExplicitReference)
                      Operand: 
                        IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: CExt.Enumerator, IsImplicit) (Syntax: 'c')
            Jump if True (Regular) to Block[B6]
                IIsNullOperation (OperationKind.IsNull, Type: System.Boolean, IsImplicit) (Syntax: 'c')
                  Operand: 
                    IFlowCaptureReferenceOperation: 1 (OperationKind.FlowCaptureReference, Type: System.IDisposable, IsImplicit) (Syntax: 'c')
            Next (Regular) Block[B5]
        Block[B5] - Block
            Predecessors: [B4]
            Statements (1)
                IInvocationOperation (virtual void System.IDisposable.Dispose()) (OperationKind.Invocation, Type: System.Void, IsImplicit) (Syntax: 'c')
                  Instance Receiver: 
                    IFlowCaptureReferenceOperation: 1 (OperationKind.FlowCaptureReference, Type: System.IDisposable, IsImplicit) (Syntax: 'c')
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
");
        }

        [Fact]
        public void ForEach_ExtensionGetEnumeratorParamArrayNotLast()
        {
            var comp = CreateCompilation(@"
public class C
{
    static void M(C c)
    /*<bind>*/{
        foreach (var i in c)
        {
        }
    }/*</bind>*/
}
public static class CExt
{
    public class Enumerator
    {
        public int Current => 1;
        public bool MoveNext() => false;
    }
    public static Enumerator GetEnumerator(this C c, params int[] arr, int i = 0) => null;
}
");

            var diagnostics = new DiagnosticDescription[] {
                // (6,27): error CS7036: There is no argument given that corresponds to the required parameter 'arr' of 'CExt.GetEnumerator(C, params int[], int)'
                //         foreach (var i in c)
                Diagnostic(ErrorCode.ERR_NoCorrespondingArgument, "c").WithArguments("arr", "CExt.GetEnumerator(C, params int[], int)").WithLocation(6, 27),
                // (6,27): error CS1579: foreach statement cannot operate on variables of type 'C' because 'C' does not contain a public instance or extension definition for 'GetEnumerator'
                //         foreach (var i in c)
                Diagnostic(ErrorCode.ERR_ForEachMissingMember, "c").WithArguments("C", "GetEnumerator").WithLocation(6, 27),
                // (18,54): error CS0231: A params parameter must be the last parameter in a parameter list
                //     public static Enumerator GetEnumerator(this C c, params int[] arr, int i = 0) => null;
                Diagnostic(ErrorCode.ERR_ParamsLast, "params int[] arr").WithLocation(18, 54)
            };

            VerifyOperationTreeAndDiagnosticsForTest<BlockSyntax>(comp, @"
IBlockOperation (1 statements) (OperationKind.Block, Type: null, IsInvalid) (Syntax: '{ ... }')
  IForEachLoopOperation (LoopKind.ForEach, Continue Label Id: 0, Exit Label Id: 1) (OperationKind.Loop, Type: null, IsInvalid) (Syntax: 'foreach (va ... }')
    Locals: Local_1: var i
    LoopControlVariable: 
      IVariableDeclaratorOperation (Symbol: var i) (OperationKind.VariableDeclarator, Type: null) (Syntax: 'var')
        Initializer: 
          null
    Collection: 
      IParameterReferenceOperation: c (OperationKind.ParameterReference, Type: C, IsInvalid) (Syntax: 'c')
    Body: 
      IBlockOperation (0 statements) (OperationKind.Block, Type: null) (Syntax: '{ ... }')
    NextVariables(0)", diagnostics);

            VerifyFlowGraphForTest<BlockSyntax>(comp, @"
Block[B0] - Entry
    Statements (0)
    Next (Regular) Block[B1]
Block[B1] - Block
    Predecessors: [B0]
    Statements (1)
        IInvalidOperation (OperationKind.Invalid, Type: null, IsInvalid, IsImplicit) (Syntax: 'c')
          Children(1):
              IParameterReferenceOperation: c (OperationKind.ParameterReference, Type: C, IsInvalid) (Syntax: 'c')
    Next (Regular) Block[B2]
Block[B2] - Block
    Predecessors: [B1] [B3]
    Statements (0)
    Jump if False (Regular) to Block[B4]
        IInvalidOperation (OperationKind.Invalid, Type: System.Boolean, IsInvalid, IsImplicit) (Syntax: 'c')
          Children(1):
              IInvalidOperation (OperationKind.Invalid, Type: null, IsInvalid, IsImplicit) (Syntax: 'c')
                Children(0)
    Next (Regular) Block[B3]
        Entering: {R1}
.locals {R1}
{
    Locals: [var i]
    Block[B3] - Block
        Predecessors: [B2]
        Statements (1)
            ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: null, IsImplicit) (Syntax: 'var')
              Left: 
                ILocalReferenceOperation: i (IsDeclaration: True) (OperationKind.LocalReference, Type: var, IsImplicit) (Syntax: 'var')
              Right: 
                IInvalidOperation (OperationKind.Invalid, Type: null, IsInvalid, IsImplicit) (Syntax: 'c')
                  Children(1):
                      IInvalidOperation (OperationKind.Invalid, Type: null, IsInvalid, IsImplicit) (Syntax: 'c')
                        Children(0)
        Next (Regular) Block[B2]
            Leaving: {R1}
}
Block[B4] - Exit
    Predecessors: [B2]
    Statements (0)
");
        }

        [Fact]
        public void ForEach_ExtensionGetEnumeratorParamArrayWrongType()
        {
            var comp = CreateCompilation(@"
public class C
{
    static void M(C c)
    /*<bind>*/{
        foreach (var i in c)
        {
        }
    }/*</bind>*/
}
public static class CExt
{
    public class Enumerator
    {
        public int Current => 1;
        public bool MoveNext() => false;
    }
    public static Enumerator GetEnumerator(this C c, params int i = 0) => null;
}
");

            var diagnostics = new DiagnosticDescription[] {
                // (6,27): error CS7036: There is no argument given that corresponds to the required parameter 'i' of 'CExt.GetEnumerator(C, params int)'
                //         foreach (var i in c)
                Diagnostic(ErrorCode.ERR_NoCorrespondingArgument, "c").WithArguments("i", "CExt.GetEnumerator(C, params int)").WithLocation(6, 27),
                // (6,27): error CS1579: foreach statement cannot operate on variables of type 'C' because 'C' does not contain a public instance or extension definition for 'GetEnumerator'
                //         foreach (var i in c)
                Diagnostic(ErrorCode.ERR_ForEachMissingMember, "c").WithArguments("C", "GetEnumerator").WithLocation(6, 27),
                // (18,54): error CS0225: The params parameter must have a valid collection type
                //     public static Enumerator GetEnumerator(this C c, params int i = 0) => null;
                Diagnostic(ErrorCode.ERR_ParamsMustBeCollection, "params").WithLocation(18, 54),
                // (18,54): error CS1751: Cannot specify a default value for a parameter collection
                //     public static Enumerator GetEnumerator(this C c, params int i = 0) => null;
                Diagnostic(ErrorCode.ERR_DefaultValueForParamsParameter, "params").WithLocation(18, 54)
            };

            VerifyOperationTreeAndDiagnosticsForTest<BlockSyntax>(comp, @"
IBlockOperation (1 statements) (OperationKind.Block, Type: null, IsInvalid) (Syntax: '{ ... }')
  IForEachLoopOperation (LoopKind.ForEach, Continue Label Id: 0, Exit Label Id: 1) (OperationKind.Loop, Type: null, IsInvalid) (Syntax: 'foreach (va ... }')
    Locals: Local_1: var i
    LoopControlVariable: 
      IVariableDeclaratorOperation (Symbol: var i) (OperationKind.VariableDeclarator, Type: null) (Syntax: 'var')
        Initializer: 
          null
    Collection: 
      IParameterReferenceOperation: c (OperationKind.ParameterReference, Type: C, IsInvalid) (Syntax: 'c')
    Body: 
      IBlockOperation (0 statements) (OperationKind.Block, Type: null) (Syntax: '{ ... }')
    NextVariables(0)", diagnostics);

            VerifyFlowGraphForTest<BlockSyntax>(comp, @"
Block[B0] - Entry
    Statements (0)
    Next (Regular) Block[B1]
Block[B1] - Block
    Predecessors: [B0]
    Statements (1)
        IInvalidOperation (OperationKind.Invalid, Type: null, IsInvalid, IsImplicit) (Syntax: 'c')
          Children(1):
              IParameterReferenceOperation: c (OperationKind.ParameterReference, Type: C, IsInvalid) (Syntax: 'c')
    Next (Regular) Block[B2]
Block[B2] - Block
    Predecessors: [B1] [B3]
    Statements (0)
    Jump if False (Regular) to Block[B4]
        IInvalidOperation (OperationKind.Invalid, Type: System.Boolean, IsInvalid, IsImplicit) (Syntax: 'c')
          Children(1):
              IInvalidOperation (OperationKind.Invalid, Type: null, IsInvalid, IsImplicit) (Syntax: 'c')
                Children(0)
    Next (Regular) Block[B3]
        Entering: {R1}
.locals {R1}
{
    Locals: [var i]
    Block[B3] - Block
        Predecessors: [B2]
        Statements (1)
            ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: null, IsImplicit) (Syntax: 'var')
              Left: 
                ILocalReferenceOperation: i (IsDeclaration: True) (OperationKind.LocalReference, Type: var, IsImplicit) (Syntax: 'var')
              Right: 
                IInvalidOperation (OperationKind.Invalid, Type: null, IsInvalid, IsImplicit) (Syntax: 'c')
                  Children(1):
                      IInvalidOperation (OperationKind.Invalid, Type: null, IsInvalid, IsImplicit) (Syntax: 'c')
                        Children(0)
        Next (Regular) Block[B2]
            Leaving: {R1}
}
Block[B4] - Exit
    Predecessors: [B2]
    Statements (0)
");
        }

        [Fact]
        public static void ForEach_ExtensionGetEnumeratorParamsOnWrongType_IL()
        {
            string il = @"
.class public auto ansi beforefieldinit C extends [mscorlib]System.Object
{
    .method public hidebysig specialname rtspecialname instance void .ctor () cil managed 
    {
        ldarg.0
        call instance void [mscorlib]System.Object::.ctor()
        nop
        ret
    }
}

.class public auto ansi abstract sealed beforefieldinit CExt extends [mscorlib]System.Object
{
    .custom instance void [mscorlib]System.Runtime.CompilerServices.ExtensionAttribute::.ctor() = (
        01 00 00 00
    )
    .method public hidebysig static 
        class [mscorlib]System.Collections.IEnumerator GetEnumerator (
            class C c,
            int32 i
        ) cil managed 
    {
        .custom instance void [mscorlib]System.Runtime.CompilerServices.ExtensionAttribute::.ctor() = (
            01 00 00 00
        )
        .param [2]
            .custom instance void [mscorlib]System.ParamArrayAttribute::.ctor() = (
                01 00 00 00
            )
        ldnull
        ret
    }
}
";

            var comp = CreateCompilationWithIL(@"
public class D
{
    static void M(C c)
    /*<bind>*/{
        foreach (var i in c)
        {
        }
    }/*</bind>*/
}
", il);

            var diagnostics = new DiagnosticDescription[] {
                // (6,27): error CS7036: There is no argument given that corresponds to the required parameter 'i' of 'CExt.GetEnumerator(C, params int)'
                //         foreach (var i in c)
                Diagnostic(ErrorCode.ERR_NoCorrespondingArgument, "c").WithArguments("i", "CExt.GetEnumerator(C, params int)").WithLocation(6, 27),
                // (6,27): error CS1579: foreach statement cannot operate on variables of type 'C' because 'C' does not contain a public instance or extension definition for 'GetEnumerator'
                //         foreach (var i in c)
                Diagnostic(ErrorCode.ERR_ForEachMissingMember, "c").WithArguments("C", "GetEnumerator").WithLocation(6, 27)
            };

            VerifyOperationTreeAndDiagnosticsForTest<BlockSyntax>(comp, @"
IBlockOperation (1 statements) (OperationKind.Block, Type: null, IsInvalid) (Syntax: '{ ... }')
  IForEachLoopOperation (LoopKind.ForEach, Continue Label Id: 0, Exit Label Id: 1) (OperationKind.Loop, Type: null, IsInvalid) (Syntax: 'foreach (va ... }')
    Locals: Local_1: var i
    LoopControlVariable: 
      IVariableDeclaratorOperation (Symbol: var i) (OperationKind.VariableDeclarator, Type: null) (Syntax: 'var')
        Initializer: 
          null
    Collection: 
      IParameterReferenceOperation: c (OperationKind.ParameterReference, Type: C, IsInvalid) (Syntax: 'c')
    Body: 
      IBlockOperation (0 statements) (OperationKind.Block, Type: null) (Syntax: '{ ... }')
    NextVariables(0)", diagnostics);

            VerifyFlowGraphForTest<BlockSyntax>(comp, @"
Block[B0] - Entry
    Statements (0)
    Next (Regular) Block[B1]
Block[B1] - Block
    Predecessors: [B0]
    Statements (1)
        IInvalidOperation (OperationKind.Invalid, Type: null, IsInvalid, IsImplicit) (Syntax: 'c')
          Children(1):
              IParameterReferenceOperation: c (OperationKind.ParameterReference, Type: C, IsInvalid) (Syntax: 'c')
    Next (Regular) Block[B2]
Block[B2] - Block
    Predecessors: [B1] [B3]
    Statements (0)
    Jump if False (Regular) to Block[B4]
        IInvalidOperation (OperationKind.Invalid, Type: System.Boolean, IsInvalid, IsImplicit) (Syntax: 'c')
          Children(1):
              IInvalidOperation (OperationKind.Invalid, Type: null, IsInvalid, IsImplicit) (Syntax: 'c')
                Children(0)
    Next (Regular) Block[B3]
        Entering: {R1}
.locals {R1}
{
    Locals: [var i]
    Block[B3] - Block
        Predecessors: [B2]
        Statements (1)
            ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: null, IsImplicit) (Syntax: 'var')
              Left: 
                ILocalReferenceOperation: i (IsDeclaration: True) (OperationKind.LocalReference, Type: var, IsImplicit) (Syntax: 'var')
              Right: 
                IInvalidOperation (OperationKind.Invalid, Type: null, IsInvalid, IsImplicit) (Syntax: 'c')
                  Children(1):
                      IInvalidOperation (OperationKind.Invalid, Type: null, IsInvalid, IsImplicit) (Syntax: 'c')
                        Children(0)
        Next (Regular) Block[B2]
            Leaving: {R1}
}
Block[B4] - Exit
    Predecessors: [B2]
    Statements (0)
");
        }

        [Fact]
        public static void ForEach_ExtensionGetEnumeratorNonTrailingDefaultValue_IL()
        {
            string il = @"
.class public auto ansi beforefieldinit C extends [mscorlib]System.Object
{
    .method public hidebysig specialname rtspecialname instance void .ctor () cil managed 
    {
        ldarg.0
        call instance void [mscorlib]System.Object::.ctor()
        nop
        ret
    }
}

.class public auto ansi abstract sealed beforefieldinit CExt extends [mscorlib]System.Object
{
    .custom instance void [mscorlib]System.Runtime.CompilerServices.ExtensionAttribute::.ctor() = (
        01 00 00 00
    )
    .method public hidebysig static 
        class [mscorlib]System.Collections.IEnumerator GetEnumerator (
            class C c,
            [opt] int32 i1,
            int32 i2
        ) cil managed 
    {
        .custom instance void [mscorlib]System.Runtime.CompilerServices.ExtensionAttribute::.ctor() = (
            01 00 00 00
        )
        .param [2] = int32(0)

        ldnull
        ret
    } // end of method CExt::GetEnumerator
}
";

            var comp = CreateCompilationWithIL(@"
public class D
{
    static void M(C c)
    /*<bind>*/{
        foreach (var i in c)
        {
        }
    }/*</bind>*/
}
", il);

            var diagnostics = new DiagnosticDescription[] {
                // (6,27): error CS7036: There is no argument given that corresponds to the required parameter 'i2' of 'CExt.GetEnumerator(C, int, int)'
                //         foreach (var i in c)
                Diagnostic(ErrorCode.ERR_NoCorrespondingArgument, "c").WithArguments("i2", "CExt.GetEnumerator(C, int, int)").WithLocation(6, 27),
                // (6,27): error CS1579: foreach statement cannot operate on variables of type 'C' because 'C' does not contain a public instance or extension definition for 'GetEnumerator'
                //         foreach (var i in c)
                Diagnostic(ErrorCode.ERR_ForEachMissingMember, "c").WithArguments("C", "GetEnumerator").WithLocation(6, 27)
            };

            VerifyOperationTreeAndDiagnosticsForTest<BlockSyntax>(comp, @"
IBlockOperation (1 statements) (OperationKind.Block, Type: null, IsInvalid) (Syntax: '{ ... }')
  IForEachLoopOperation (LoopKind.ForEach, Continue Label Id: 0, Exit Label Id: 1) (OperationKind.Loop, Type: null, IsInvalid) (Syntax: 'foreach (va ... }')
    Locals: Local_1: var i
    LoopControlVariable: 
      IVariableDeclaratorOperation (Symbol: var i) (OperationKind.VariableDeclarator, Type: null) (Syntax: 'var')
        Initializer: 
          null
    Collection: 
      IParameterReferenceOperation: c (OperationKind.ParameterReference, Type: C, IsInvalid) (Syntax: 'c')
    Body: 
      IBlockOperation (0 statements) (OperationKind.Block, Type: null) (Syntax: '{ ... }')
    NextVariables(0)", diagnostics);

            VerifyFlowGraphForTest<BlockSyntax>(comp, @"
Block[B0] - Entry
    Statements (0)
    Next (Regular) Block[B1]
Block[B1] - Block
    Predecessors: [B0]
    Statements (1)
        IInvalidOperation (OperationKind.Invalid, Type: null, IsInvalid, IsImplicit) (Syntax: 'c')
          Children(1):
              IParameterReferenceOperation: c (OperationKind.ParameterReference, Type: C, IsInvalid) (Syntax: 'c')
    Next (Regular) Block[B2]
Block[B2] - Block
    Predecessors: [B1] [B3]
    Statements (0)
    Jump if False (Regular) to Block[B4]
        IInvalidOperation (OperationKind.Invalid, Type: System.Boolean, IsInvalid, IsImplicit) (Syntax: 'c')
          Children(1):
              IInvalidOperation (OperationKind.Invalid, Type: null, IsInvalid, IsImplicit) (Syntax: 'c')
                Children(0)
    Next (Regular) Block[B3]
        Entering: {R1}
.locals {R1}
{
    Locals: [var i]
    Block[B3] - Block
        Predecessors: [B2]
        Statements (1)
            ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: null, IsImplicit) (Syntax: 'var')
              Left: 
                ILocalReferenceOperation: i (IsDeclaration: True) (OperationKind.LocalReference, Type: var, IsImplicit) (Syntax: 'var')
              Right: 
                IInvalidOperation (OperationKind.Invalid, Type: null, IsInvalid, IsImplicit) (Syntax: 'c')
                  Children(1):
                      IInvalidOperation (OperationKind.Invalid, Type: null, IsInvalid, IsImplicit) (Syntax: 'c')
                        Children(0)
        Next (Regular) Block[B2]
            Leaving: {R1}
}
Block[B4] - Exit
    Predecessors: [B2]
    Statements (0)
");
        }

        [Fact]
        public void FlowGraph_NullableSuppressionOnForeachVariable()
        {
            var comp = CreateCompilation(@"
using System.Collections.Generic;
class A
{
    public static void M()
    /*<bind>*/{
        foreach(var s in new A()!)
        {
            _ = s.ToString();
        }
    }/*</bind>*/
}
static class Extensions
{
    public static IEnumerator<string>? GetEnumerator(this A a) => throw null!;
}", options: WithNullableEnable());

            VerifyOperationTreeAndDiagnosticsForTest<BlockSyntax>(comp, @"
IBlockOperation (1 statements) (OperationKind.Block, Type: null) (Syntax: '{ ... }')
  IForEachLoopOperation (LoopKind.ForEach, Continue Label Id: 0, Exit Label Id: 1) (OperationKind.Loop, Type: null) (Syntax: 'foreach(var ... }')
    Locals: Local_1: System.String? s
    LoopControlVariable: 
      IVariableDeclaratorOperation (Symbol: System.String? s) (OperationKind.VariableDeclarator, Type: null) (Syntax: 'var')
        Initializer: 
          null
    Collection: 
      IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: A, IsImplicit) (Syntax: 'new A()')
        Conversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
        Operand: 
          IObjectCreationOperation (Constructor: A..ctor()) (OperationKind.ObjectCreation, Type: A) (Syntax: 'new A()')
            Arguments(0)
            Initializer: 
              null
    Body: 
      IBlockOperation (1 statements) (OperationKind.Block, Type: null) (Syntax: '{ ... }')
        IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: '_ = s.ToString();')
          Expression: 
            ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.String) (Syntax: '_ = s.ToString()')
              Left: 
                IDiscardOperation (Symbol: System.String _) (OperationKind.Discard, Type: System.String) (Syntax: '_')
              Right: 
                IInvocationOperation (virtual System.String System.String.ToString()) (OperationKind.Invocation, Type: System.String) (Syntax: 's.ToString()')
                  Instance Receiver: 
                    ILocalReferenceOperation: s (OperationKind.LocalReference, Type: System.String) (Syntax: 's')
                  Arguments(0)
    NextVariables(0)
", DiagnosticDescription.None);

            VerifyFlowGraphForTest<BlockSyntax>(comp, @"
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
            IFlowCaptureOperation: 0 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'new A()')
              Value: 
                IInvocationOperation (System.Collections.Generic.IEnumerator<System.String>? Extensions.GetEnumerator(this A a)) (OperationKind.Invocation, Type: System.Collections.Generic.IEnumerator<System.String>?, IsImplicit) (Syntax: 'new A()')
                  Instance Receiver: 
                    null
                  Arguments(1):
                      IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: a) (OperationKind.Argument, Type: null, IsImplicit) (Syntax: 'new A()')
                        IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: A, IsImplicit) (Syntax: 'new A()')
                          Conversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                            (Identity)
                          Operand: 
                            IObjectCreationOperation (Constructor: A..ctor()) (OperationKind.ObjectCreation, Type: A) (Syntax: 'new A()')
                              Arguments(0)
                              Initializer: 
                                null
                        InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                        OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
        Next (Regular) Block[B2]
            Entering: {R2} {R3}
    .try {R2, R3}
    {
        Block[B2] - Block
            Predecessors: [B1] [B3]
            Statements (0)
            Jump if False (Regular) to Block[B7]
                IInvocationOperation (virtual System.Boolean System.Collections.IEnumerator.MoveNext()) (OperationKind.Invocation, Type: System.Boolean, IsImplicit) (Syntax: 'new A()')
                  Instance Receiver: 
                    IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: System.Collections.Generic.IEnumerator<System.String>?, IsImplicit) (Syntax: 'new A()')
                  Arguments(0)
                Finalizing: {R5}
                Leaving: {R3} {R2} {R1}
            Next (Regular) Block[B3]
                Entering: {R4}
        .locals {R4}
        {
            Locals: [System.String? s]
            Block[B3] - Block
                Predecessors: [B2]
                Statements (2)
                    ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: null, IsImplicit) (Syntax: 'var')
                      Left: 
                        ILocalReferenceOperation: s (IsDeclaration: True) (OperationKind.LocalReference, Type: System.String?, IsImplicit) (Syntax: 'var')
                      Right: 
                        IPropertyReferenceOperation: System.String System.Collections.Generic.IEnumerator<System.String>.Current { get; } (OperationKind.PropertyReference, Type: System.String, IsImplicit) (Syntax: 'var')
                          Instance Receiver: 
                            IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: System.Collections.Generic.IEnumerator<System.String>?, IsImplicit) (Syntax: 'new A()')
                    IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: '_ = s.ToString();')
                      Expression: 
                        ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.String) (Syntax: '_ = s.ToString()')
                          Left: 
                            IDiscardOperation (Symbol: System.String _) (OperationKind.Discard, Type: System.String) (Syntax: '_')
                          Right: 
                            IInvocationOperation (virtual System.String System.String.ToString()) (OperationKind.Invocation, Type: System.String) (Syntax: 's.ToString()')
                              Instance Receiver: 
                                ILocalReferenceOperation: s (OperationKind.LocalReference, Type: System.String) (Syntax: 's')
                              Arguments(0)
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
                IIsNullOperation (OperationKind.IsNull, Type: System.Boolean, IsImplicit) (Syntax: 'new A()')
                  Operand: 
                    IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: System.Collections.Generic.IEnumerator<System.String>?, IsImplicit) (Syntax: 'new A()')
            Next (Regular) Block[B5]
        Block[B5] - Block
            Predecessors: [B4]
            Statements (1)
                IInvocationOperation (virtual void System.IDisposable.Dispose()) (OperationKind.Invocation, Type: System.Void, IsImplicit) (Syntax: 'new A()')
                  Instance Receiver: 
                    IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.IDisposable, IsImplicit) (Syntax: 'new A()')
                      Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: True, IsUserDefined: False) (MethodSymbol: null)
                        (ImplicitReference)
                      Operand: 
                        IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: System.Collections.Generic.IEnumerator<System.String>?, IsImplicit) (Syntax: 'new A()')
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
");
        }

        internal static readonly string s_ValueTask = @"
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

        [CompilerTrait(CompilerFeature.IOperation)]
        [Fact]
        public void ForEach_InlineArray_01()
        {
            string source = @"
class C
{
    public void F(Buffer10 arg)
    {
        /*<bind>*/foreach (ref char item in arg)
        {
            item = '0';
        }/*</bind>*/
    }
}
";

            string expectedOperationTree = @"
IForEachLoopOperation (LoopKind.ForEach, Continue Label Id: 0, Exit Label Id: 1) (OperationKind.Loop, Type: null) (Syntax: 'foreach (re ... }')
  Locals: Local_1: System.Char item
  LoopControlVariable:
    IVariableDeclaratorOperation (Symbol: System.Char item) (OperationKind.VariableDeclarator, Type: null) (Syntax: 'char')
      Initializer:
        null
  Collection:
    IParameterReferenceOperation: arg (OperationKind.ParameterReference, Type: Buffer10) (Syntax: 'arg')
  Body:
    IBlockOperation (1 statements) (OperationKind.Block, Type: null) (Syntax: '{ ... }')
      IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'item = '0';')
        Expression:
          ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Char) (Syntax: 'item = '0'')
            Left:
              ILocalReferenceOperation: item (OperationKind.LocalReference, Type: System.Char) (Syntax: 'item')
            Right:
              ILiteralOperation (OperationKind.Literal, Type: System.Char, Constant: 0) (Syntax: ''0'')
  NextVariables(0)
";
            var expectedDiagnostics = DiagnosticDescription.None;

            var comp = CreateCompilation(source + IOperationTests_IInlineArrayAccessOperation.Buffer10Definition, targetFramework: TargetFramework.Net80);
            VerifyOperationTreeAndDiagnosticsForTest<ForEachStatementSyntax>(comp, expectedOperationTree, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation, CompilerFeature.Dataflow)]
        [Fact]
        public void ForEach_InlineArray_01_Flow()
        {
            string source = @"
class C
{
    public void F(Buffer10 arg)
    /*<bind>*/{
        foreach (ref char item in arg)
        {
            item = '0';
        }
    }/*</bind>*/
}
";

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
            IFlowCaptureOperation: 0 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'arg')
              Value:
                IInvocationOperation ( System.Span<System.Char>.Enumerator System.Span<System.Char>.GetEnumerator()) (OperationKind.Invocation, Type: System.Span<System.Char>.Enumerator, IsImplicit) (Syntax: 'arg')
                  Instance Receiver:
                    IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.Span<System.Char>, IsImplicit) (Syntax: 'arg')
                      Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                        (InlineArray)
                      Operand:
                        IParameterReferenceOperation: arg (OperationKind.ParameterReference, Type: Buffer10) (Syntax: 'arg')
                  Arguments(0)
        Next (Regular) Block[B2]
    Block[B2] - Block
        Predecessors: [B1] [B3]
        Statements (0)
        Jump if False (Regular) to Block[B4]
            IInvocationOperation ( System.Boolean System.Span<System.Char>.Enumerator.MoveNext()) (OperationKind.Invocation, Type: System.Boolean, IsImplicit) (Syntax: 'arg')
              Instance Receiver:
                IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: System.Span<System.Char>.Enumerator, IsImplicit) (Syntax: 'arg')
              Arguments(0)
            Leaving: {R1}
        Next (Regular) Block[B3]
            Entering: {R2}
    .locals {R2}
    {
        Locals: [System.Char item]
        Block[B3] - Block
            Predecessors: [B2]
            Statements (2)
                ISimpleAssignmentOperation (IsRef) (OperationKind.SimpleAssignment, Type: null, IsImplicit) (Syntax: 'char')
                  Left:
                    ILocalReferenceOperation: item (IsDeclaration: True) (OperationKind.LocalReference, Type: System.Char, IsImplicit) (Syntax: 'char')
                  Right:
                    IPropertyReferenceOperation: ref System.Char System.Span<System.Char>.Enumerator.Current { get; } (OperationKind.PropertyReference, Type: System.Char, IsImplicit) (Syntax: 'char')
                      Instance Receiver:
                        IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: System.Span<System.Char>.Enumerator, IsImplicit) (Syntax: 'arg')
                IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'item = '0';')
                  Expression:
                    ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: System.Char) (Syntax: 'item = '0'')
                      Left:
                        ILocalReferenceOperation: item (OperationKind.LocalReference, Type: System.Char) (Syntax: 'item')
                      Right:
                        ILiteralOperation (OperationKind.Literal, Type: System.Char, Constant: 0) (Syntax: ''0'')
            Next (Regular) Block[B2]
                Leaving: {R2}
    }
}
Block[B4] - Exit
    Predecessors: [B2]
    Statements (0)
";
            var expectedDiagnostics = DiagnosticDescription.None;

            var comp = CreateCompilation(source + IOperationTests_IInlineArrayAccessOperation.Buffer10Definition, targetFramework: TargetFramework.Net80);
            VerifyFlowGraphAndDiagnosticsForTest<BlockSyntax>(comp, expectedFlowGraph, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation)]
        [Fact]
        public void ForEach_InlineArray_02()
        {
            string source = @"
class C
{
    public void F(in Buffer10 arg)
    {
        /*<bind>*/foreach (ref readonly char item in arg)
        {
            System.Console.WriteLine(item);
        }/*</bind>*/
    }
}
";

            string expectedOperationTree = @"
IForEachLoopOperation (LoopKind.ForEach, Continue Label Id: 0, Exit Label Id: 1) (OperationKind.Loop, Type: null) (Syntax: 'foreach (re ... }')
  Locals: Local_1: System.Char item
  LoopControlVariable:
    IVariableDeclaratorOperation (Symbol: System.Char item) (OperationKind.VariableDeclarator, Type: null) (Syntax: 'char')
      Initializer:
        null
  Collection:
    IParameterReferenceOperation: arg (OperationKind.ParameterReference, Type: Buffer10) (Syntax: 'arg')
  Body:
    IBlockOperation (1 statements) (OperationKind.Block, Type: null) (Syntax: '{ ... }')
      IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'System.Cons ... Line(item);')
        Expression:
          IInvocationOperation (void System.Console.WriteLine(System.Char value)) (OperationKind.Invocation, Type: System.Void) (Syntax: 'System.Cons ... eLine(item)')
            Instance Receiver:
              null
            Arguments(1):
                IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: value) (OperationKind.Argument, Type: null) (Syntax: 'item')
                  ILocalReferenceOperation: item (OperationKind.LocalReference, Type: System.Char) (Syntax: 'item')
                  InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                  OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
  NextVariables(0)
";
            var expectedDiagnostics = DiagnosticDescription.None;

            var comp = CreateCompilation(source + IOperationTests_IInlineArrayAccessOperation.Buffer10Definition, targetFramework: TargetFramework.Net80);
            VerifyOperationTreeAndDiagnosticsForTest<ForEachStatementSyntax>(comp, expectedOperationTree, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation, CompilerFeature.Dataflow)]
        [Fact]
        public void ForEach_InlineArray_02_Flow()
        {
            string source = @"
class C
{
    public void F(in Buffer10 arg)
    /*<bind>*/{
        foreach (ref readonly char item in arg)
        {
            System.Console.WriteLine(item);
        }
    }/*</bind>*/
}
";

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
            IFlowCaptureOperation: 0 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'arg')
              Value:
                IInvocationOperation ( System.ReadOnlySpan<System.Char>.Enumerator System.ReadOnlySpan<System.Char>.GetEnumerator()) (OperationKind.Invocation, Type: System.ReadOnlySpan<System.Char>.Enumerator, IsImplicit) (Syntax: 'arg')
                  Instance Receiver:
                    IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.ReadOnlySpan<System.Char>, IsImplicit) (Syntax: 'arg')
                      Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                        (InlineArray)
                      Operand:
                        IParameterReferenceOperation: arg (OperationKind.ParameterReference, Type: Buffer10) (Syntax: 'arg')
                  Arguments(0)
        Next (Regular) Block[B2]
    Block[B2] - Block
        Predecessors: [B1] [B3]
        Statements (0)
        Jump if False (Regular) to Block[B4]
            IInvocationOperation ( System.Boolean System.ReadOnlySpan<System.Char>.Enumerator.MoveNext()) (OperationKind.Invocation, Type: System.Boolean, IsImplicit) (Syntax: 'arg')
              Instance Receiver:
                IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: System.ReadOnlySpan<System.Char>.Enumerator, IsImplicit) (Syntax: 'arg')
              Arguments(0)
            Leaving: {R1}
        Next (Regular) Block[B3]
            Entering: {R2}
    .locals {R2}
    {
        Locals: [System.Char item]
        Block[B3] - Block
            Predecessors: [B2]
            Statements (2)
                ISimpleAssignmentOperation (IsRef) (OperationKind.SimpleAssignment, Type: null, IsImplicit) (Syntax: 'char')
                  Left:
                    ILocalReferenceOperation: item (IsDeclaration: True) (OperationKind.LocalReference, Type: System.Char, IsImplicit) (Syntax: 'char')
                  Right:
                    IPropertyReferenceOperation: ref readonly modreq(System.Runtime.InteropServices.InAttribute) System.Char System.ReadOnlySpan<System.Char>.Enumerator.Current { get; } (OperationKind.PropertyReference, Type: System.Char, IsImplicit) (Syntax: 'char')
                      Instance Receiver:
                        IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: System.ReadOnlySpan<System.Char>.Enumerator, IsImplicit) (Syntax: 'arg')
                IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'System.Cons ... Line(item);')
                  Expression:
                    IInvocationOperation (void System.Console.WriteLine(System.Char value)) (OperationKind.Invocation, Type: System.Void) (Syntax: 'System.Cons ... eLine(item)')
                      Instance Receiver:
                        null
                      Arguments(1):
                          IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: value) (OperationKind.Argument, Type: null) (Syntax: 'item')
                            ILocalReferenceOperation: item (OperationKind.LocalReference, Type: System.Char) (Syntax: 'item')
                            InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                            OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
            Next (Regular) Block[B2]
                Leaving: {R2}
    }
}
Block[B4] - Exit
    Predecessors: [B2]
    Statements (0)
";
            var expectedDiagnostics = DiagnosticDescription.None;

            var comp = CreateCompilation(source + IOperationTests_IInlineArrayAccessOperation.Buffer10Definition, targetFramework: TargetFramework.Net80);
            VerifyFlowGraphAndDiagnosticsForTest<BlockSyntax>(comp, expectedFlowGraph, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation)]
        [Fact]
        public void ForEach_InlineArray_03()
        {
            string source = @"
class C
{
    public void F()
    {
        /*<bind>*/foreach (char item in GetBuffer())
        {
            System.Console.WriteLine(item);
        }/*</bind>*/
    }

    static Buffer10 GetBuffer() => default;
}
";

            string expectedOperationTree = @"
IForEachLoopOperation (LoopKind.ForEach, Continue Label Id: 0, Exit Label Id: 1) (OperationKind.Loop, Type: null) (Syntax: 'foreach (ch ... }')
  Locals: Local_1: System.Char item
  LoopControlVariable:
    IVariableDeclaratorOperation (Symbol: System.Char item) (OperationKind.VariableDeclarator, Type: null) (Syntax: 'char')
      Initializer:
        null
  Collection:
    IInvocationOperation (Buffer10 C.GetBuffer()) (OperationKind.Invocation, Type: Buffer10) (Syntax: 'GetBuffer()')
      Instance Receiver:
        null
      Arguments(0)
  Body:
    IBlockOperation (1 statements) (OperationKind.Block, Type: null) (Syntax: '{ ... }')
      IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'System.Cons ... Line(item);')
        Expression:
          IInvocationOperation (void System.Console.WriteLine(System.Char value)) (OperationKind.Invocation, Type: System.Void) (Syntax: 'System.Cons ... eLine(item)')
            Instance Receiver:
              null
            Arguments(1):
                IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: value) (OperationKind.Argument, Type: null) (Syntax: 'item')
                  ILocalReferenceOperation: item (OperationKind.LocalReference, Type: System.Char) (Syntax: 'item')
                  InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                  OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
  NextVariables(0)
";
            var expectedDiagnostics = DiagnosticDescription.None;

            var comp = CreateCompilation(source + IOperationTests_IInlineArrayAccessOperation.Buffer10Definition, targetFramework: TargetFramework.Net80);
            VerifyOperationTreeAndDiagnosticsForTest<ForEachStatementSyntax>(comp, expectedOperationTree, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation, CompilerFeature.Dataflow)]
        [Fact]
        public void ForEach_InlineArray_03_Flow()
        {
            string source = @"
class C
{
    public void F(in Buffer10 arg)
    /*<bind>*/{
        foreach (char item in GetBuffer())
        {
            System.Console.WriteLine(item);
        }
    }/*</bind>*/

    static Buffer10 GetBuffer() => default;
}
";

            string expectedFlowGraph = @"
Block[B0] - Entry
    Statements (0)
    Next (Regular) Block[B1]
        Entering: {R1}
.locals {R1}
{
    CaptureIds: [0] [1]
    Block[B1] - Block
        Predecessors: [B0]
        Statements (2)
            IFlowCaptureOperation: 0 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'GetBuffer()')
              Value:
                IInvocationOperation (Buffer10 C.GetBuffer()) (OperationKind.Invocation, Type: Buffer10) (Syntax: 'GetBuffer()')
                  Instance Receiver:
                    null
                  Arguments(0)
            IFlowCaptureOperation: 1 (OperationKind.FlowCapture, Type: null, IsImplicit) (Syntax: 'GetBuffer()')
              Value:
                IInvocationOperation ( System.ReadOnlySpan<System.Char>.Enumerator System.ReadOnlySpan<System.Char>.GetEnumerator()) (OperationKind.Invocation, Type: System.ReadOnlySpan<System.Char>.Enumerator, IsImplicit) (Syntax: 'GetBuffer()')
                  Instance Receiver:
                    IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.ReadOnlySpan<System.Char>, IsImplicit) (Syntax: 'GetBuffer()')
                      Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                        (InlineArray)
                      Operand:
                        IFlowCaptureReferenceOperation: 0 (OperationKind.FlowCaptureReference, Type: Buffer10, IsImplicit) (Syntax: 'GetBuffer()')
                  Arguments(0)
        Next (Regular) Block[B2]
    Block[B2] - Block
        Predecessors: [B1] [B3]
        Statements (0)
        Jump if False (Regular) to Block[B4]
            IInvocationOperation ( System.Boolean System.ReadOnlySpan<System.Char>.Enumerator.MoveNext()) (OperationKind.Invocation, Type: System.Boolean, IsImplicit) (Syntax: 'GetBuffer()')
              Instance Receiver:
                IFlowCaptureReferenceOperation: 1 (OperationKind.FlowCaptureReference, Type: System.ReadOnlySpan<System.Char>.Enumerator, IsImplicit) (Syntax: 'GetBuffer()')
              Arguments(0)
            Leaving: {R1}
        Next (Regular) Block[B3]
            Entering: {R2}
    .locals {R2}
    {
        Locals: [System.Char item]
        Block[B3] - Block
            Predecessors: [B2]
            Statements (2)
                ISimpleAssignmentOperation (OperationKind.SimpleAssignment, Type: null, IsImplicit) (Syntax: 'char')
                  Left:
                    ILocalReferenceOperation: item (IsDeclaration: True) (OperationKind.LocalReference, Type: System.Char, IsImplicit) (Syntax: 'char')
                  Right:
                    IPropertyReferenceOperation: ref readonly modreq(System.Runtime.InteropServices.InAttribute) System.Char System.ReadOnlySpan<System.Char>.Enumerator.Current { get; } (OperationKind.PropertyReference, Type: System.Char, IsImplicit) (Syntax: 'char')
                      Instance Receiver:
                        IFlowCaptureReferenceOperation: 1 (OperationKind.FlowCaptureReference, Type: System.ReadOnlySpan<System.Char>.Enumerator, IsImplicit) (Syntax: 'GetBuffer()')
                IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'System.Cons ... Line(item);')
                  Expression:
                    IInvocationOperation (void System.Console.WriteLine(System.Char value)) (OperationKind.Invocation, Type: System.Void) (Syntax: 'System.Cons ... eLine(item)')
                      Instance Receiver:
                        null
                      Arguments(1):
                          IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: value) (OperationKind.Argument, Type: null) (Syntax: 'item')
                            ILocalReferenceOperation: item (OperationKind.LocalReference, Type: System.Char) (Syntax: 'item')
                            InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                            OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
            Next (Regular) Block[B2]
                Leaving: {R2}
    }
}
Block[B4] - Exit
    Predecessors: [B2]
    Statements (0)
";
            var expectedDiagnostics = DiagnosticDescription.None;

            var comp = CreateCompilation(source + IOperationTests_IInlineArrayAccessOperation.Buffer10Definition, targetFramework: TargetFramework.Net80);
            VerifyFlowGraphAndDiagnosticsForTest<BlockSyntax>(comp, expectedFlowGraph, expectedDiagnostics);
        }

        [CompilerTrait(CompilerFeature.IOperation)]
        [Fact]
        public void ForEach_InlineArray_04()
        {
            string source = @"
class C
{
    public void F(Buffer10 arg)
    {
        /*<bind>*/foreach (char item in (Buffer10)arg)
        {
            System.Console.WriteLine(item);
        }/*</bind>*/
    }
}
";

            string expectedOperationTree = @"
IForEachLoopOperation (LoopKind.ForEach, Continue Label Id: 0, Exit Label Id: 1) (OperationKind.Loop, Type: null) (Syntax: 'foreach (ch ... }')
  Locals: Local_1: System.Char item
  LoopControlVariable:
    IVariableDeclaratorOperation (Symbol: System.Char item) (OperationKind.VariableDeclarator, Type: null) (Syntax: 'char')
      Initializer:
        null
  Collection:
    IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: Buffer10) (Syntax: '(Buffer10)arg')
      Conversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
      Operand:
        IParameterReferenceOperation: arg (OperationKind.ParameterReference, Type: Buffer10) (Syntax: 'arg')
  Body:
    IBlockOperation (1 statements) (OperationKind.Block, Type: null) (Syntax: '{ ... }')
      IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'System.Cons ... Line(item);')
        Expression:
          IInvocationOperation (void System.Console.WriteLine(System.Char value)) (OperationKind.Invocation, Type: System.Void) (Syntax: 'System.Cons ... eLine(item)')
            Instance Receiver:
              null
            Arguments(1):
                IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: value) (OperationKind.Argument, Type: null) (Syntax: 'item')
                  ILocalReferenceOperation: item (OperationKind.LocalReference, Type: System.Char) (Syntax: 'item')
                  InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                  OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
  NextVariables(0)
";
            var expectedDiagnostics = DiagnosticDescription.None;

            var comp = CreateCompilation(source + IOperationTests_IInlineArrayAccessOperation.Buffer10Definition, targetFramework: TargetFramework.Net80);
            VerifyOperationTreeAndDiagnosticsForTest<ForEachStatementSyntax>(comp, expectedOperationTree, expectedDiagnostics);
        }
    }
}
