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
IForEachLoopOperation (LoopKind.ForEach) (OperationKind.Loop, Type: null) (Syntax: 'foreach (st ... }')
  Locals: Local_1: System.String value
  LoopControlVariable: 
    ILocalReferenceOperation: value (IsDeclaration: True) (OperationKind.LocalReference, Type: System.String, Constant: null, IsImplicit) (Syntax: 'foreach (st ... }')
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
                IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: value) (OperationKind.Argument, Type: System.String) (Syntax: 'value')
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
IForEachLoopOperation (LoopKind.ForEach) (OperationKind.Loop, Type: null) (Syntax: 'foreach (st ... }')
  Locals: Local_1: System.String item
  LoopControlVariable: 
    ILocalReferenceOperation: item (IsDeclaration: True) (OperationKind.LocalReference, Type: System.String, Constant: null, IsImplicit) (Syntax: 'foreach (st ... }')
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
                IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: value) (OperationKind.Argument, Type: System.String) (Syntax: 'item')
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
IForEachLoopOperation (LoopKind.ForEach) (OperationKind.Loop, Type: null) (Syntax: 'foreach (Ke ... }')
  Locals: Local_1: System.Collections.Generic.KeyValuePair<System.Int32, System.Int32> pair
  LoopControlVariable: 
    ILocalReferenceOperation: pair (IsDeclaration: True) (OperationKind.LocalReference, Type: System.Collections.Generic.KeyValuePair<System.Int32, System.Int32>, Constant: null, IsImplicit) (Syntax: 'foreach (Ke ... }')
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
                IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: format) (OperationKind.Argument, Type: System.String) (Syntax: '""{0},{1}""')
                  ILiteralOperation (OperationKind.Literal, Type: System.String, Constant: ""{0},{1}"") (Syntax: '""{0},{1}""')
                  InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                  OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: arg0) (OperationKind.Argument, Type: System.Object) (Syntax: 'pair.Key')
                  IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.Object, IsImplicit) (Syntax: 'pair.Key')
                    Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                    Operand: 
                      IPropertyReferenceOperation: System.Int32 System.Collections.Generic.KeyValuePair<System.Int32, System.Int32>.Key { get; } (OperationKind.PropertyReference, Type: System.Int32) (Syntax: 'pair.Key')
                        Instance Receiver: 
                          ILocalReferenceOperation: pair (OperationKind.LocalReference, Type: System.Collections.Generic.KeyValuePair<System.Int32, System.Int32>) (Syntax: 'pair')
                  InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                  OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: arg1) (OperationKind.Argument, Type: System.Object) (Syntax: 'pair.Value')
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
IForEachLoopOperation (LoopKind.ForEach) (OperationKind.Loop, Type: null) (Syntax: 'foreach (in ... }')
  Locals: Local_1: System.Int32 num
  LoopControlVariable: 
    ILocalReferenceOperation: num (IsDeclaration: True) (OperationKind.LocalReference, Type: System.Int32, Constant: null, IsImplicit) (Syntax: 'foreach (in ... }')
  Collection: 
    IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.Collections.IEnumerable, IsImplicit) (Syntax: 'numbers')
      Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: True, IsUserDefined: False) (MethodSymbol: null)
      Operand: 
        ILocalReferenceOperation: numbers (OperationKind.LocalReference, Type: System.Int32[]) (Syntax: 'numbers')
  Body: 
    IBlockOperation (2 statements) (OperationKind.Block, Type: null) (Syntax: '{ ... }')
      IConditionalOperation (OperationKind.Conditional, Type: null) (Syntax: 'if (num>3) ... }')
        Condition: 
          IBinaryOperation (BinaryOperatorKind.GreaterThan) (OperationKind.BinaryOperator, Type: System.Boolean) (Syntax: 'num>3')
            Left: 
              ILocalReferenceOperation: num (OperationKind.LocalReference, Type: System.Int32) (Syntax: 'num')
            Right: 
              ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 3) (Syntax: '3')
        WhenTrue: 
          IBlockOperation (1 statements) (OperationKind.Block, Type: null) (Syntax: '{ ... }')
            IBranchOperation (BranchKind.Break) (OperationKind.Branch, Type: null) (Syntax: 'break;')
        WhenFalse: 
          null
      IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'System.Cons ... eLine(num);')
        Expression: 
          IInvocationOperation (void System.Console.WriteLine(System.Int32 value)) (OperationKind.Invocation, Type: System.Void) (Syntax: 'System.Cons ... teLine(num)')
            Instance Receiver: 
              null
            Arguments(1):
                IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: value) (OperationKind.Argument, Type: System.Int32) (Syntax: 'num')
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
IForEachLoopOperation (LoopKind.ForEach) (OperationKind.Loop, Type: null) (Syntax: 'foreach (in ... }')
  Locals: Local_1: System.Int32 num
  LoopControlVariable: 
    ILocalReferenceOperation: num (IsDeclaration: True) (OperationKind.LocalReference, Type: System.Int32, Constant: null, IsImplicit) (Syntax: 'foreach (in ... }')
  Collection: 
    IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.Collections.IEnumerable, IsImplicit) (Syntax: 'numbers')
      Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: True, IsUserDefined: False) (MethodSymbol: null)
      Operand: 
        ILocalReferenceOperation: numbers (OperationKind.LocalReference, Type: System.Int32[]) (Syntax: 'numbers')
  Body: 
    IBlockOperation (2 statements) (OperationKind.Block, Type: null) (Syntax: '{ ... }')
      IConditionalOperation (OperationKind.Conditional, Type: null) (Syntax: 'if (num>3) ... }')
        Condition: 
          IBinaryOperation (BinaryOperatorKind.GreaterThan) (OperationKind.BinaryOperator, Type: System.Boolean) (Syntax: 'num>3')
            Left: 
              ILocalReferenceOperation: num (OperationKind.LocalReference, Type: System.Int32) (Syntax: 'num')
            Right: 
              ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 3) (Syntax: '3')
        WhenTrue: 
          IBlockOperation (1 statements) (OperationKind.Block, Type: null) (Syntax: '{ ... }')
            IBranchOperation (BranchKind.Continue) (OperationKind.Branch, Type: null) (Syntax: 'continue;')
        WhenFalse: 
          null
      IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'System.Cons ... eLine(num);')
        Expression: 
          IInvocationOperation (void System.Console.WriteLine(System.Int32 value)) (OperationKind.Invocation, Type: System.Void) (Syntax: 'System.Cons ... teLine(num)')
            Instance Receiver: 
              null
            Arguments(1):
                IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: value) (OperationKind.Argument, Type: System.Int32) (Syntax: 'num')
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
IForEachLoopOperation (LoopKind.ForEach) (OperationKind.Loop, Type: null, IsInvalid) (Syntax: 'foreach (st ... }')
  Locals: Local_1: System.String value
  LoopControlVariable: 
    ILocalReferenceOperation: value (IsDeclaration: True) (OperationKind.LocalReference, Type: System.String, Constant: null, IsInvalid, IsImplicit) (Syntax: 'foreach (st ... }')
  Collection: 
    ILocalReferenceOperation: sorted (OperationKind.LocalReference, Type: ?, IsInvalid) (Syntax: 'sorted')
  Body: 
    IBlockOperation (1 statements) (OperationKind.Block, Type: null) (Syntax: '{ ... }')
      IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null) (Syntax: 'System.Cons ... ine(value);')
        Expression: 
          IInvocationOperation (void System.Console.WriteLine(System.String value)) (OperationKind.Invocation, Type: System.Void) (Syntax: 'System.Cons ... Line(value)')
            Instance Receiver: 
              null
            Arguments(1):
                IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: value) (OperationKind.Argument, Type: System.String) (Syntax: 'value')
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
IForEachLoopOperation (LoopKind.ForEach) (OperationKind.Loop, Type: null) (Syntax: 'foreach (Fi ... }')
  Locals: Local_1: System.Reflection.FieldInfo fi
  LoopControlVariable: 
    ILocalReferenceOperation: fi (IsDeclaration: True) (OperationKind.LocalReference, Type: System.Reflection.FieldInfo, Constant: null, IsImplicit) (Syntax: 'foreach (Fi ... }')
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
                IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: value) (OperationKind.Argument, Type: System.String) (Syntax: 'fi.Name + "" ... (employee))')
                  IBinaryOperation (BinaryOperatorKind.Add) (OperationKind.BinaryOperator, Type: System.String) (Syntax: 'fi.Name + "" ... (employee))')
                    Left: 
                      IBinaryOperation (BinaryOperatorKind.Add) (OperationKind.BinaryOperator, Type: System.String) (Syntax: 'fi.Name + "" = ""')
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
                            IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: value) (OperationKind.Argument, Type: System.Object) (Syntax: 'fi.GetValue(employee)')
                              IInvocationOperation (virtual System.Object System.Reflection.FieldInfo.GetValue(System.Object obj)) (OperationKind.Invocation, Type: System.Object) (Syntax: 'fi.GetValue(employee)')
                                Instance Receiver: 
                                  ILocalReferenceOperation: fi (OperationKind.LocalReference, Type: System.Reflection.FieldInfo) (Syntax: 'fi')
                                Arguments(1):
                                    IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: obj) (OperationKind.Argument, Type: System.Object) (Syntax: 'employee')
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
IForEachLoopOperation (LoopKind.ForEach) (OperationKind.Loop, Type: null) (Syntax: 'foreach (ch ... }')
  Locals: Local_1: System.Char c
  LoopControlVariable: 
    ILocalReferenceOperation: c (IsDeclaration: True) (OperationKind.LocalReference, Type: System.Char, Constant: null, IsImplicit) (Syntax: 'foreach (ch ... }')
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
                IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: value) (OperationKind.Argument, Type: System.Char) (Syntax: 'c')
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
IForEachLoopOperation (LoopKind.ForEach) (OperationKind.Loop, Type: null) (Syntax: 'foreach (va ... }')
  Locals: Local_1: System.Collections.Generic.KeyValuePair<System.Int32, System.Int32> pair
  LoopControlVariable: 
    ILocalReferenceOperation: pair (IsDeclaration: True) (OperationKind.LocalReference, Type: System.Collections.Generic.KeyValuePair<System.Int32, System.Int32>, Constant: null, IsImplicit) (Syntax: 'foreach (va ... }')
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
                IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: format) (OperationKind.Argument, Type: System.String) (Syntax: '""{0},{1}""')
                  ILiteralOperation (OperationKind.Literal, Type: System.String, Constant: ""{0},{1}"") (Syntax: '""{0},{1}""')
                  InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                  OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: arg0) (OperationKind.Argument, Type: System.Object) (Syntax: 'pair.Key')
                  IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.Object, IsImplicit) (Syntax: 'pair.Key')
                    Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                    Operand: 
                      IPropertyReferenceOperation: System.Int32 System.Collections.Generic.KeyValuePair<System.Int32, System.Int32>.Key { get; } (OperationKind.PropertyReference, Type: System.Int32) (Syntax: 'pair.Key')
                        Instance Receiver: 
                          ILocalReferenceOperation: pair (OperationKind.LocalReference, Type: System.Collections.Generic.KeyValuePair<System.Int32, System.Int32>) (Syntax: 'pair')
                  InConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                  OutConversion: CommonConversion (Exists: True, IsIdentity: True, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: arg1) (OperationKind.Argument, Type: System.Object) (Syntax: 'pair.Value')
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
IForEachLoopOperation (LoopKind.ForEach) (OperationKind.Loop, Type: null, IsInvalid) (Syntax: 'foreach (Mi ... }')
  Locals: Local_1: MissingType x
  LoopControlVariable: 
    ILocalReferenceOperation: x (IsDeclaration: True) (OperationKind.LocalReference, Type: MissingType, Constant: null, IsInvalid, IsImplicit) (Syntax: 'foreach (Mi ... }')
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
                      Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: False, IsUserDefined: False) (MethodSymbol: null)
                      Operand: 
                        IUnaryOperation (UnaryOperatorKind.Not) (OperationKind.UnaryOperator, Type: System.Object) (Syntax: '!x.Equals(null)')
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
IForEachLoopOperation (LoopKind.ForEach) (OperationKind.Loop, Type: null, IsInvalid) (Syntax: 'foreach (in ... }')
  Locals: Local_1: System.Int32 x
  LoopControlVariable: 
    ILocalReferenceOperation: x (IsDeclaration: True) (OperationKind.LocalReference, Type: System.Int32, Constant: null, IsInvalid, IsImplicit) (Syntax: 'foreach (in ... }')
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
IForEachLoopOperation (LoopKind.ForEach) (OperationKind.Loop, Type: null, IsInvalid) (Syntax: 'foreach (in ... }')
  Locals: Local_1: System.Int32 x
  LoopControlVariable: 
    ILocalReferenceOperation: x (IsDeclaration: True) (OperationKind.LocalReference, Type: System.Int32, Constant: null, IsInvalid, IsImplicit) (Syntax: 'foreach (in ... }')
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
IForEachLoopOperation (LoopKind.ForEach) (OperationKind.Loop, Type: null, IsInvalid) (Syntax: 'foreach (in ... a) { x++; }')
  Locals: Local_1: System.Int32 x
  LoopControlVariable: 
    ILocalReferenceOperation: x (IsDeclaration: True) (OperationKind.LocalReference, Type: System.Int32, Constant: null, IsInvalid, IsImplicit) (Syntax: 'foreach (in ... a) { x++; }')
  Collection: 
    IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.Collections.IEnumerable, IsImplicit) (Syntax: 'a')
      Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: True, IsUserDefined: False) (MethodSymbol: null)
      Operand: 
        IParameterReferenceOperation: a (OperationKind.ParameterReference, Type: System.Int32[]) (Syntax: 'a')
  Body: 
    IBlockOperation (1 statements) (OperationKind.Block, Type: null, IsInvalid) (Syntax: '{ x++; }')
      IExpressionStatementOperation (OperationKind.ExpressionStatement, Type: null, IsInvalid) (Syntax: 'x++;')
        Expression: 
          IIncrementOrDecrementOperation (Postfix) (OperationKind.Increment, Type: System.Object, IsInvalid) (Syntax: 'x++')
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
IForEachLoopOperation (LoopKind.ForEach) (OperationKind.Loop, Type: null) (Syntax: 'foreach (lo ... x in e) { }')
  Locals: Local_1: System.Int64 x
  LoopControlVariable: 
    ILocalReferenceOperation: x (IsDeclaration: True) (OperationKind.LocalReference, Type: System.Int64, Constant: null, IsImplicit) (Syntax: 'foreach (lo ... x in e) { }')
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
IForEachLoopOperation (LoopKind.ForEach) (OperationKind.Loop, Type: null) (Syntax: 'foreach (var x in s) { }')
  Locals: Local_1: System.Char x
  LoopControlVariable: 
    ILocalReferenceOperation: x (IsDeclaration: True) (OperationKind.LocalReference, Type: System.Char, Constant: null, IsImplicit) (Syntax: 'foreach (var x in s) { }')
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
IForEachLoopOperation (LoopKind.ForEach) (OperationKind.Loop, Type: null) (Syntax: 'foreach (var x in a) { }')
  Locals: Local_1: C.var x
  LoopControlVariable: 
    ILocalReferenceOperation: x (IsDeclaration: True) (OperationKind.LocalReference, Type: C.var, Constant: null, IsImplicit) (Syntax: 'foreach (var x in a) { }')
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
IForEachLoopOperation (LoopKind.ForEach) (OperationKind.Loop, Type: null) (Syntax: 'foreach (int x in d) { }')
  Locals: Local_1: System.Int32 x
  LoopControlVariable: 
    ILocalReferenceOperation: x (IsDeclaration: True) (OperationKind.LocalReference, Type: System.Int32, Constant: null, IsImplicit) (Syntax: 'foreach (int x in d) { }')
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
IForEachLoopOperation (LoopKind.ForEach) (OperationKind.Loop, Type: null) (Syntax: 'foreach (ob ... }')
  Locals: Local_1: System.Object x
  LoopControlVariable: 
    ILocalReferenceOperation: x (IsDeclaration: True) (OperationKind.LocalReference, Type: System.Object, Constant: null, IsImplicit) (Syntax: 'foreach (ob ... }')
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
                IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: value) (OperationKind.Argument, Type: System.Object) (Syntax: 'x')
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
IForEachLoopOperation (LoopKind.ForEach) (OperationKind.Loop, Type: null) (Syntax: 'foreach (st ... e)args) { }')
  Locals: Local_1: System.String x
  LoopControlVariable: 
    ILocalReferenceOperation: x (IsDeclaration: True) (OperationKind.LocalReference, Type: System.String, Constant: null, IsImplicit) (Syntax: 'foreach (st ... e)args) { }')
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
IForEachLoopOperation (LoopKind.ForEach) (OperationKind.Loop, Type: null) (Syntax: 'foreach (in ... }')
  Locals: Local_1: System.Int32 num
  LoopControlVariable: 
    ILocalReferenceOperation: num (IsDeclaration: True) (OperationKind.LocalReference, Type: System.Int32, Constant: null, IsImplicit) (Syntax: 'foreach (in ... }')
  Collection: 
    IConversionOperation (TryCast: False, Unchecked) (OperationKind.Conversion, Type: System.Collections.IEnumerable, IsImplicit) (Syntax: 'numbers')
      Conversion: CommonConversion (Exists: True, IsIdentity: False, IsNumeric: False, IsReference: True, IsUserDefined: False) (MethodSymbol: null)
      Operand: 
        ILocalReferenceOperation: numbers (OperationKind.LocalReference, Type: System.Int32[]) (Syntax: 'numbers')
  Body: 
    IBlockOperation (2 statements) (OperationKind.Block, Type: null) (Syntax: '{ ... }')
      IConditionalOperation (OperationKind.Conditional, Type: null) (Syntax: 'if (num > 3 ... }')
        Condition: 
          IBinaryOperation (BinaryOperatorKind.GreaterThan) (OperationKind.BinaryOperator, Type: System.Boolean) (Syntax: 'num > 3')
            Left: 
              ILocalReferenceOperation: num (OperationKind.LocalReference, Type: System.Int32) (Syntax: 'num')
            Right: 
              ILiteralOperation (OperationKind.Literal, Type: System.Int32, Constant: 3) (Syntax: '3')
        WhenTrue: 
          IBlockOperation (1 statements) (OperationKind.Block, Type: null) (Syntax: '{ ... }')
            IThrowOperation (OperationKind.Throw, Type: null) (Syntax: 'throw new S ... ""testing"");')
              IObjectCreationOperation (Constructor: System.Exception..ctor(System.String message)) (OperationKind.ObjectCreation, Type: System.Exception) (Syntax: 'new System. ... (""testing"")')
                Arguments(1):
                    IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: message) (OperationKind.Argument, Type: System.String) (Syntax: '""testing""')
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
                IArgumentOperation (ArgumentKind.Explicit, Matching Parameter: value) (OperationKind.Argument, Type: System.Int32) (Syntax: 'num')
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
IForEachLoopOperation (LoopKind.ForEach) (OperationKind.Loop, Type: null) (Syntax: 'foreach (va ... }')
  Locals: Local_1: System.Int32 a
    Local_2: System.Int32 b
  LoopControlVariable: 
    IDeclarationExpressionOperation (OperationKind.DeclarationExpression, Type: (System.Int32 a, System.Int32 b)) (Syntax: 'var (a, b)')
      ITupleOperation (OperationKind.Tuple, Type: (System.Int32 a, System.Int32 b)) (Syntax: '(a, b)')
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
IForEachLoopOperation (LoopKind.ForEach) (OperationKind.Loop, Type: null) (Syntax: 'foreach (va ... }')
  Locals: Local_1: System.Int32 a
    Local_2: System.Int32 b
    Local_3: System.Int32 c
  LoopControlVariable: 
    IDeclarationExpressionOperation (OperationKind.DeclarationExpression, Type: (System.Int32 a, (System.Int32 b, System.Int32 c))) (Syntax: 'var (a, (b, c))')
      ITupleOperation (OperationKind.Tuple, Type: (System.Int32 a, (System.Int32 b, System.Int32 c))) (Syntax: '(a, (b, c))')
        Elements(2):
            ILocalReferenceOperation: a (IsDeclaration: True) (OperationKind.LocalReference, Type: System.Int32) (Syntax: 'a')
            ITupleOperation (OperationKind.Tuple, Type: (System.Int32 b, System.Int32 c)) (Syntax: '(b, c)')
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
  IForEachLoopOperation (LoopKind.ForEach) (OperationKind.Loop, Type: null, IsInvalid) (Syntax: 'foreach (i')
    LoopControlVariable: 
      null
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
  IForEachLoopOperation (LoopKind.ForEach) (OperationKind.Loop, Type: null, IsInvalid) (Syntax: 'foreach (x[ ... }')
    LoopControlVariable: 
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
                Diagnostic(ErrorCode.ERR_BadForeachDecl, "in").WithLocation(6, 23)
            };

            VerifyOperationTreeAndDiagnosticsForTest<BlockSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }
    }
}
