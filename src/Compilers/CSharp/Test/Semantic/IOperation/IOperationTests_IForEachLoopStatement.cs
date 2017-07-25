// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis.CSharp.Syntax;
using Roslyn.Test.Utilities;
using Xunit;


namespace Microsoft.CodeAnalysis.CSharp.UnitTests
{
    public partial class IOperationTests : SemanticModelTestBase
    {
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
IForEachLoopStatement (Iteration variable: System.String value) (LoopKind.ForEach) (OperationKind.LoopStatement) (Syntax: 'foreach (st ... }')
  Collection: IConversionExpression (ConversionKind.Cast, Implicit) (OperationKind.ConversionExpression, Type: System.Collections.IEnumerable) (Syntax: 'pets')
      Operand: ILocalReferenceExpression: pets (OperationKind.LocalReferenceExpression, Type: System.String[]) (Syntax: 'pets')
  Body: IBlockStatement (1 statements) (OperationKind.BlockStatement) (Syntax: '{ ... }')
      IExpressionStatement (OperationKind.ExpressionStatement) (Syntax: 'System.Cons ... ine(value);')
        Expression: IInvocationExpression (void System.Console.WriteLine(System.String value)) (OperationKind.InvocationExpression, Type: System.Void) (Syntax: 'System.Cons ... Line(value)')
            Instance Receiver: null
            Arguments(1):
                IArgument (ArgumentKind.Explicit, Matching Parameter: value) (OperationKind.Argument) (Syntax: 'value')
                  ILocalReferenceExpression: value (OperationKind.LocalReferenceExpression, Type: System.String) (Syntax: 'value')
                  InConversion: null
                  OutConversion: null
";
            VerifyOperationTreeForTest<ForEachStatementSyntax>(source, expectedOperationTree);
        }

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
IForEachLoopStatement (Iteration variable: System.String item) (LoopKind.ForEach) (OperationKind.LoopStatement) (Syntax: 'foreach (st ... }')
  Collection: IConversionExpression (ConversionKind.Cast, Implicit) (OperationKind.ConversionExpression, Type: System.Collections.Generic.List<System.String>) (Syntax: 'list')
      Operand: ILocalReferenceExpression: list (OperationKind.LocalReferenceExpression, Type: System.Collections.Generic.List<System.String>) (Syntax: 'list')
  Body: IBlockStatement (1 statements) (OperationKind.BlockStatement) (Syntax: '{ ... }')
      IExpressionStatement (OperationKind.ExpressionStatement) (Syntax: 'Console.WriteLine(item);')
        Expression: IInvocationExpression (void System.Console.WriteLine(System.String value)) (OperationKind.InvocationExpression, Type: System.Void) (Syntax: 'Console.WriteLine(item)')
            Instance Receiver: null
            Arguments(1):
                IArgument (ArgumentKind.Explicit, Matching Parameter: value) (OperationKind.Argument) (Syntax: 'item')
                  ILocalReferenceExpression: item (OperationKind.LocalReferenceExpression, Type: System.String) (Syntax: 'item')
                  InConversion: null
                  OutConversion: null
";
            VerifyOperationTreeForTest<ForEachStatementSyntax>(source, expectedOperationTree);
        }

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
IForEachLoopStatement (Iteration variable: System.Collections.Generic.KeyValuePair<System.Int32, System.Int32> pair) (LoopKind.ForEach) (OperationKind.LoopStatement) (Syntax: 'foreach (Ke ... }')
  Collection: IConversionExpression (ConversionKind.Cast, Implicit) (OperationKind.ConversionExpression, Type: System.Collections.Generic.Dictionary<System.Int32, System.Int32>) (Syntax: '_h')
      Operand: IFieldReferenceExpression: System.Collections.Generic.Dictionary<System.Int32, System.Int32> Program._h (Static) (OperationKind.FieldReferenceExpression, Type: System.Collections.Generic.Dictionary<System.Int32, System.Int32>) (Syntax: '_h')
          Instance Receiver: null
  Body: IBlockStatement (1 statements) (OperationKind.BlockStatement) (Syntax: '{ ... }')
      IExpressionStatement (OperationKind.ExpressionStatement) (Syntax: 'Console.Wri ... air.Value);')
        Expression: IInvocationExpression (void System.Console.WriteLine(System.String format, System.Object arg0, System.Object arg1)) (OperationKind.InvocationExpression, Type: System.Void) (Syntax: 'Console.Wri ... pair.Value)')
            Instance Receiver: null
            Arguments(3):
                IArgument (ArgumentKind.Explicit, Matching Parameter: format) (OperationKind.Argument) (Syntax: '""{0},{1}""')
                  ILiteralExpression (OperationKind.LiteralExpression, Type: System.String, Constant: ""{0},{1}"") (Syntax: '""{0},{1}""')
                  InConversion: null
                  OutConversion: null
                IArgument (ArgumentKind.Explicit, Matching Parameter: arg0) (OperationKind.Argument) (Syntax: 'pair.Key')
                  IConversionExpression (ConversionKind.Cast, Implicit) (OperationKind.ConversionExpression, Type: System.Object) (Syntax: 'pair.Key')
                    Operand: IPropertyReferenceExpression: System.Int32 System.Collections.Generic.KeyValuePair<System.Int32, System.Int32>.Key { get; } (OperationKind.PropertyReferenceExpression, Type: System.Int32) (Syntax: 'pair.Key')
                        Instance Receiver: ILocalReferenceExpression: pair (OperationKind.LocalReferenceExpression, Type: System.Collections.Generic.KeyValuePair<System.Int32, System.Int32>) (Syntax: 'pair')
                  InConversion: null
                  OutConversion: null
                IArgument (ArgumentKind.Explicit, Matching Parameter: arg1) (OperationKind.Argument) (Syntax: 'pair.Value')
                  IConversionExpression (ConversionKind.Cast, Implicit) (OperationKind.ConversionExpression, Type: System.Object) (Syntax: 'pair.Value')
                    Operand: IPropertyReferenceExpression: System.Int32 System.Collections.Generic.KeyValuePair<System.Int32, System.Int32>.Value { get; } (OperationKind.PropertyReferenceExpression, Type: System.Int32) (Syntax: 'pair.Value')
                        Instance Receiver: ILocalReferenceExpression: pair (OperationKind.LocalReferenceExpression, Type: System.Collections.Generic.KeyValuePair<System.Int32, System.Int32>) (Syntax: 'pair')
                  InConversion: null
                  OutConversion: null
";
            VerifyOperationTreeForTest<ForEachStatementSyntax>(source, expectedOperationTree);
        }

        [Fact, WorkItem(17602, "https://github.com/dotnet/roslyn/issues/17602")]
        public void IForEachLoopStatement_WithYield()
        {
            string source = @"
class Class1
{
    public static void ShowSchools()
    {
        var theSchools = new Schools();
        /*<bind>*/foreach (School school in theSchools.NextGalaxy)
        {
            System.Console.WriteLine(school.Name);
        }/*</bind>*/
    }

    public class Schools
    {
        public System.Collections.Generic.IEnumerable<School> NextGalaxy
        {
            get
            {
                yield return new School { Name = ""Tadpole"", Years = 400 };
                yield return new School { Name = ""Pinwheel"", Years = 25 };
            }
        }
    }

    public class School
    {
        public string Name { get; set; }
        public int Years { get; set; }
    }
}

";
string expectedOperationTree = @"
IForEachLoopStatement (Iteration variable: Class1.School school) (LoopKind.ForEach) (OperationKind.LoopStatement) (Syntax: 'foreach (Sc ... }')
  Collection: IConversionExpression (ConversionKind.Cast, Implicit) (OperationKind.ConversionExpression, Type: System.Collections.Generic.IEnumerable<Class1.School>) (Syntax: 'theSchools.NextGalaxy')
      Operand: IPropertyReferenceExpression: System.Collections.Generic.IEnumerable<Class1.School> Class1.Schools.NextGalaxy { get; } (OperationKind.PropertyReferenceExpression, Type: System.Collections.Generic.IEnumerable<Class1.School>) (Syntax: 'theSchools.NextGalaxy')
          Instance Receiver: ILocalReferenceExpression: theSchools (OperationKind.LocalReferenceExpression, Type: Class1.Schools) (Syntax: 'theSchools')
  Body: IBlockStatement (1 statements) (OperationKind.BlockStatement) (Syntax: '{ ... }')
      IExpressionStatement (OperationKind.ExpressionStatement) (Syntax: 'System.Cons ... hool.Name);')
        Expression: IInvocationExpression (void System.Console.WriteLine(System.String value)) (OperationKind.InvocationExpression, Type: System.Void) (Syntax: 'System.Cons ... chool.Name)')
            Instance Receiver: null
            Arguments(1):
                IArgument (ArgumentKind.Explicit, Matching Parameter: value) (OperationKind.Argument) (Syntax: 'school.Name')
                  IPropertyReferenceExpression: System.String Class1.School.Name { get; set; } (OperationKind.PropertyReferenceExpression, Type: System.String) (Syntax: 'school.Name')
                    Instance Receiver: ILocalReferenceExpression: school (OperationKind.LocalReferenceExpression, Type: Class1.School) (Syntax: 'school')
                  InConversion: null
                  OutConversion: null
";
            VerifyOperationTreeForTest<ForEachStatementSyntax>(source, expectedOperationTree);
        }

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
IForEachLoopStatement (Iteration variable: System.Int32 num) (LoopKind.ForEach) (OperationKind.LoopStatement) (Syntax: 'foreach (in ... }')
  Collection: IConversionExpression (ConversionKind.Cast, Implicit) (OperationKind.ConversionExpression, Type: System.Collections.IEnumerable) (Syntax: 'numbers')
      Operand: ILocalReferenceExpression: numbers (OperationKind.LocalReferenceExpression, Type: System.Int32[]) (Syntax: 'numbers')
  Body: IBlockStatement (2 statements) (OperationKind.BlockStatement) (Syntax: '{ ... }')
      IIfStatement (OperationKind.IfStatement) (Syntax: 'if (num>3) ... }')
        Condition: IBinaryOperatorExpression (BinaryOperationKind.IntegerGreaterThan) (OperationKind.BinaryOperatorExpression, Type: System.Boolean) (Syntax: 'num>3')
            Left: ILocalReferenceExpression: num (OperationKind.LocalReferenceExpression, Type: System.Int32) (Syntax: 'num')
            Right: ILiteralExpression (Text: 3) (OperationKind.LiteralExpression, Type: System.Int32, Constant: 3) (Syntax: '3')
        IfTrue: IBlockStatement (1 statements) (OperationKind.BlockStatement) (Syntax: '{ ... }')
            IBranchStatement (BranchKind.Break) (OperationKind.BranchStatement) (Syntax: 'break;')
        IfFalse: null
      IExpressionStatement (OperationKind.ExpressionStatement) (Syntax: 'System.Cons ... eLine(num);')
        Expression: IInvocationExpression (void System.Console.WriteLine(System.Int32 value)) (OperationKind.InvocationExpression, Type: System.Void) (Syntax: 'System.Cons ... teLine(num)')
            Instance Receiver: null
            Arguments(1):
                IArgument (ArgumentKind.Explicit, Matching Parameter: value) (OperationKind.Argument) (Syntax: 'num')
                  ILocalReferenceExpression: num (OperationKind.LocalReferenceExpression, Type: System.Int32) (Syntax: 'num')
                  InConversion: null
                  OutConversion: null
";
            VerifyOperationTreeForTest<ForEachStatementSyntax>(source, expectedOperationTree);
        }

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
IForEachLoopStatement (Iteration variable: System.Int32 num) (LoopKind.ForEach) (OperationKind.LoopStatement) (Syntax: 'foreach (in ... }')
  Collection: IConversionExpression (ConversionKind.Cast, Implicit) (OperationKind.ConversionExpression, Type: System.Collections.IEnumerable) (Syntax: 'numbers')
      Operand: ILocalReferenceExpression: numbers (OperationKind.LocalReferenceExpression, Type: System.Int32[]) (Syntax: 'numbers')
  Body: IBlockStatement (2 statements) (OperationKind.BlockStatement) (Syntax: '{ ... }')
      IIfStatement (OperationKind.IfStatement) (Syntax: 'if (num>3) ... }')
        Condition: IBinaryOperatorExpression (BinaryOperationKind.IntegerGreaterThan) (OperationKind.BinaryOperatorExpression, Type: System.Boolean) (Syntax: 'num>3')
            Left: ILocalReferenceExpression: num (OperationKind.LocalReferenceExpression, Type: System.Int32) (Syntax: 'num')
            Right: ILiteralExpression (Text: 3) (OperationKind.LiteralExpression, Type: System.Int32, Constant: 3) (Syntax: '3')
        IfTrue: IBlockStatement (1 statements) (OperationKind.BlockStatement) (Syntax: '{ ... }')
            IBranchStatement (BranchKind.Continue) (OperationKind.BranchStatement) (Syntax: 'continue;')
        IfFalse: null
      IExpressionStatement (OperationKind.ExpressionStatement) (Syntax: 'System.Cons ... eLine(num);')
        Expression: IInvocationExpression (void System.Console.WriteLine(System.Int32 value)) (OperationKind.InvocationExpression, Type: System.Void) (Syntax: 'System.Cons ... teLine(num)')
            Instance Receiver: null
            Arguments(1):
                IArgument (ArgumentKind.Explicit, Matching Parameter: value) (OperationKind.Argument) (Syntax: 'num')
                  ILocalReferenceExpression: num (OperationKind.LocalReferenceExpression, Type: System.Int32) (Syntax: 'num')
                  InConversion: null
                  OutConversion: null
";
            VerifyOperationTreeForTest<ForEachStatementSyntax>(source, expectedOperationTree);
        }

        [Fact, WorkItem(17602, "https://github.com/dotnet/roslyn/issues/17602")]
        public void IForEachLoopStatement_Nested()
        {
            string source = @"
class Program
{
    static void Main()
    {
        int[][] array = new int[2][] { new int[3] { 1, 2, 3 }, new int[3] { 4, 5, 6 } };
        /*<bind>*/foreach (int[] subArray in array)
        {
            foreach (int i in subArray)
            {
                System.Console.Write(i);
            }
        }/*</bind>*/
    }
}
";
string expectedOperationTree = @"
IForEachLoopStatement (Iteration variable: System.Int32[] subArray) (LoopKind.ForEach) (OperationKind.LoopStatement) (Syntax: 'foreach (in ... }')
  Collection: IConversionExpression (ConversionKind.Cast, Implicit) (OperationKind.ConversionExpression, Type: System.Collections.IEnumerable) (Syntax: 'array')
      Operand: ILocalReferenceExpression: array (OperationKind.LocalReferenceExpression, Type: System.Int32[][]) (Syntax: 'array')
  Body: IBlockStatement (1 statements) (OperationKind.BlockStatement) (Syntax: '{ ... }')
      IForEachLoopStatement (Iteration variable: System.Int32 i) (LoopKind.ForEach) (OperationKind.LoopStatement) (Syntax: 'foreach (in ... }')
        Collection: IConversionExpression (ConversionKind.Cast, Implicit) (OperationKind.ConversionExpression, Type: System.Collections.IEnumerable) (Syntax: 'subArray')
            Operand: ILocalReferenceExpression: subArray (OperationKind.LocalReferenceExpression, Type: System.Int32[]) (Syntax: 'subArray')
        Body: IBlockStatement (1 statements) (OperationKind.BlockStatement) (Syntax: '{ ... }')
            IExpressionStatement (OperationKind.ExpressionStatement) (Syntax: 'System.Console.Write(i);')
              Expression: IInvocationExpression (void System.Console.Write(System.Int32 value)) (OperationKind.InvocationExpression, Type: System.Void) (Syntax: 'System.Console.Write(i)')
                  Instance Receiver: null
                  Arguments(1):
                      IArgument (ArgumentKind.Explicit, Matching Parameter: value) (OperationKind.Argument) (Syntax: 'i')
                        ILocalReferenceExpression: i (OperationKind.LocalReferenceExpression, Type: System.Int32) (Syntax: 'i')
                        InConversion: null
                        OutConversion: null
";
            VerifyOperationTreeForTest<ForEachStatementSyntax>(source, expectedOperationTree);
        }

        [Fact, WorkItem(17602, "https://github.com/dotnet/roslyn/issues/17602")]
        public void IForEachLoopStatement_Nested1()
        {
            string source = @"
class Program
{
    static void Main()
    {
        int[][] array = new int[2][] { new int[3] { 1, 2, 3 }, new int[3] { 4, 5, 6 } };
        /*<bind>*/foreach (int i in System.Linq.Enumerable.Range(0, array.GetLength(0)))
            foreach (int j in System.Linq.Enumerable.Range(0, array.GetLength(1)))
                System.Console.WriteLine(array[i][j]);/*</bind>*/
    }
}
";
string expectedOperationTree = @"
IForEachLoopStatement (Iteration variable: System.Int32 i) (LoopKind.ForEach) (OperationKind.LoopStatement) (Syntax: 'foreach (in ... ray[i][j]);')
  Collection: IConversionExpression (ConversionKind.Cast, Implicit) (OperationKind.ConversionExpression, Type: System.Collections.Generic.IEnumerable<System.Int32>) (Syntax: 'System.Linq ... tLength(0))')
      Operand: IInvocationExpression (System.Collections.Generic.IEnumerable<System.Int32> System.Linq.Enumerable.Range(System.Int32 start, System.Int32 count)) (OperationKind.InvocationExpression, Type: System.Collections.Generic.IEnumerable<System.Int32>) (Syntax: 'System.Linq ... tLength(0))')
          Instance Receiver: null
          Arguments(2):
              IArgument (ArgumentKind.Explicit, Matching Parameter: start) (OperationKind.Argument) (Syntax: '0')
                ILiteralExpression (Text: 0) (OperationKind.LiteralExpression, Type: System.Int32, Constant: 0) (Syntax: '0')
                InConversion: null
                OutConversion: null
              IArgument (ArgumentKind.Explicit, Matching Parameter: count) (OperationKind.Argument) (Syntax: 'array.GetLength(0)')
                IInvocationExpression ( System.Int32 System.Array.GetLength(System.Int32 dimension)) (OperationKind.InvocationExpression, Type: System.Int32) (Syntax: 'array.GetLength(0)')
                  Instance Receiver: ILocalReferenceExpression: array (OperationKind.LocalReferenceExpression, Type: System.Int32[][]) (Syntax: 'array')
                  Arguments(1):
                      IArgument (ArgumentKind.Explicit, Matching Parameter: dimension) (OperationKind.Argument) (Syntax: '0')
                        ILiteralExpression (Text: 0) (OperationKind.LiteralExpression, Type: System.Int32, Constant: 0) (Syntax: '0')
                        InConversion: null
                        OutConversion: null
                InConversion: null
                OutConversion: null
  Body: IForEachLoopStatement (Iteration variable: System.Int32 j) (LoopKind.ForEach) (OperationKind.LoopStatement) (Syntax: 'foreach (in ... ray[i][j]);')
      Collection: IConversionExpression (ConversionKind.Cast, Implicit) (OperationKind.ConversionExpression, Type: System.Collections.Generic.IEnumerable<System.Int32>) (Syntax: 'System.Linq ... tLength(1))')
          Operand: IInvocationExpression (System.Collections.Generic.IEnumerable<System.Int32> System.Linq.Enumerable.Range(System.Int32 start, System.Int32 count)) (OperationKind.InvocationExpression, Type: System.Collections.Generic.IEnumerable<System.Int32>) (Syntax: 'System.Linq ... tLength(1))')
              Instance Receiver: null
              Arguments(2):
                  IArgument (ArgumentKind.Explicit, Matching Parameter: start) (OperationKind.Argument) (Syntax: '0')
                    ILiteralExpression (Text: 0) (OperationKind.LiteralExpression, Type: System.Int32, Constant: 0) (Syntax: '0')
                    InConversion: null
                    OutConversion: null
                  IArgument (ArgumentKind.Explicit, Matching Parameter: count) (OperationKind.Argument) (Syntax: 'array.GetLength(1)')
                    IInvocationExpression ( System.Int32 System.Array.GetLength(System.Int32 dimension)) (OperationKind.InvocationExpression, Type: System.Int32) (Syntax: 'array.GetLength(1)')
                      Instance Receiver: ILocalReferenceExpression: array (OperationKind.LocalReferenceExpression, Type: System.Int32[][]) (Syntax: 'array')
                      Arguments(1):
                          IArgument (ArgumentKind.Explicit, Matching Parameter: dimension) (OperationKind.Argument) (Syntax: '1')
                            ILiteralExpression (Text: 1) (OperationKind.LiteralExpression, Type: System.Int32, Constant: 1) (Syntax: '1')
                            InConversion: null
                            OutConversion: null
                    InConversion: null
                    OutConversion: null
      Body: IExpressionStatement (OperationKind.ExpressionStatement) (Syntax: 'System.Cons ... ray[i][j]);')
          Expression: IInvocationExpression (void System.Console.WriteLine(System.Int32 value)) (OperationKind.InvocationExpression, Type: System.Void) (Syntax: 'System.Cons ... rray[i][j])')
              Instance Receiver: null
              Arguments(1):
                  IArgument (ArgumentKind.Explicit, Matching Parameter: value) (OperationKind.Argument) (Syntax: 'array[i][j]')
                    IArrayElementReferenceExpression (OperationKind.ArrayElementReferenceExpression, Type: System.Int32) (Syntax: 'array[i][j]')
                      Array reference: IArrayElementReferenceExpression (OperationKind.ArrayElementReferenceExpression, Type: System.Int32[]) (Syntax: 'array[i]')
                          Array reference: ILocalReferenceExpression: array (OperationKind.LocalReferenceExpression, Type: System.Int32[][]) (Syntax: 'array')
                          Indices(1):
                              ILocalReferenceExpression: i (OperationKind.LocalReferenceExpression, Type: System.Int32) (Syntax: 'i')
                      Indices(1):
                          ILocalReferenceExpression: j (OperationKind.LocalReferenceExpression, Type: System.Int32) (Syntax: 'j')
                    InConversion: null
                    OutConversion: null
";
            VerifyOperationTreeForTest<ForEachStatementSyntax>(source, expectedOperationTree);
        }

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
IForEachLoopStatement (Iteration variable: System.String value) (LoopKind.ForEach) (OperationKind.LoopStatement, IsInvalid) (Syntax: 'foreach (st ... }')
  Collection: ILocalReferenceExpression: sorted (OperationKind.LocalReferenceExpression, Type: ?, IsInvalid) (Syntax: 'sorted')
  Body: IBlockStatement (1 statements) (OperationKind.BlockStatement) (Syntax: '{ ... }')
      IExpressionStatement (OperationKind.ExpressionStatement) (Syntax: 'System.Cons ... ine(value);')
        Expression: IInvocationExpression (void System.Console.WriteLine(System.String value)) (OperationKind.InvocationExpression, Type: System.Void) (Syntax: 'System.Cons ... Line(value)')
            Instance Receiver: null
            Arguments(1):
                IArgument (ArgumentKind.Explicit, Matching Parameter: value) (OperationKind.Argument) (Syntax: 'value')
                  ILocalReferenceExpression: value (OperationKind.LocalReferenceExpression, Type: System.String) (Syntax: 'value')
                  InConversion: null
                  OutConversion: null
";
            VerifyOperationTreeForTest<ForEachStatementSyntax>(source, expectedOperationTree);
        }

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
IForEachLoopStatement (Iteration variable: System.Reflection.FieldInfo fi) (LoopKind.ForEach) (OperationKind.LoopStatement) (Syntax: 'foreach (Fi ... }')
  Collection: IConversionExpression (ConversionKind.Cast, Implicit) (OperationKind.ConversionExpression, Type: System.Collections.IEnumerable) (Syntax: 'employee.Ge ... GetFields()')
      Operand: IInvocationExpression ( System.Reflection.FieldInfo[] System.Type.GetFields()) (OperationKind.InvocationExpression, Type: System.Reflection.FieldInfo[]) (Syntax: 'employee.Ge ... GetFields()')
          Instance Receiver: IInvocationExpression ( System.Type System.Object.GetType()) (OperationKind.InvocationExpression, Type: System.Type) (Syntax: 'employee.GetType()')
              Instance Receiver: ILocalReferenceExpression: employee (OperationKind.LocalReferenceExpression, Type: DisplayStructContentsTest.Program.Employee) (Syntax: 'employee')
              Arguments(0)
          Arguments(0)
  Body: IBlockStatement (1 statements) (OperationKind.BlockStatement) (Syntax: '{ ... }')
      IExpressionStatement (OperationKind.ExpressionStatement) (Syntax: 'System.Cons ... mployee)));')
        Expression: IInvocationExpression (void System.Console.WriteLine(System.String value)) (OperationKind.InvocationExpression, Type: System.Void) (Syntax: 'System.Cons ... employee)))')
            Instance Receiver: null
            Arguments(1):
                IArgument (ArgumentKind.Explicit, Matching Parameter: value) (OperationKind.Argument) (Syntax: 'fi.Name + "" ... (employee))')
                  IBinaryOperatorExpression (BinaryOperationKind.StringConcatenate) (OperationKind.BinaryOperatorExpression, Type: System.String) (Syntax: 'fi.Name + "" ... (employee))')
                    Left: IBinaryOperatorExpression (BinaryOperationKind.StringConcatenate) (OperationKind.BinaryOperatorExpression, Type: System.String) (Syntax: 'fi.Name + "" = ""')
                        Left: IPropertyReferenceExpression: System.String System.Reflection.MemberInfo.Name { get; } (OperationKind.PropertyReferenceExpression, Type: System.String) (Syntax: 'fi.Name')
                            Instance Receiver: ILocalReferenceExpression: fi (OperationKind.LocalReferenceExpression, Type: System.Reflection.FieldInfo) (Syntax: 'fi')
                        Right: ILiteralExpression (OperationKind.LiteralExpression, Type: System.String, Constant: "" = "") (Syntax: '"" = ""')
                    Right: IInvocationExpression (System.String System.Convert.ToString(System.Object value)) (OperationKind.InvocationExpression, Type: System.String) (Syntax: 'System.Conv ... (employee))')
                        Instance Receiver: null
                        Arguments(1):
                            IArgument (ArgumentKind.Explicit, Matching Parameter: value) (OperationKind.Argument) (Syntax: 'fi.GetValue(employee)')
                              IInvocationExpression (virtual System.Object System.Reflection.FieldInfo.GetValue(System.Object obj)) (OperationKind.InvocationExpression, Type: System.Object) (Syntax: 'fi.GetValue(employee)')
                                Instance Receiver: ILocalReferenceExpression: fi (OperationKind.LocalReferenceExpression, Type: System.Reflection.FieldInfo) (Syntax: 'fi')
                                Arguments(1):
                                    IArgument (ArgumentKind.Explicit, Matching Parameter: obj) (OperationKind.Argument) (Syntax: 'employee')
                                      IConversionExpression (ConversionKind.Cast, Implicit) (OperationKind.ConversionExpression, Type: System.Object) (Syntax: 'employee')
                                        Operand: ILocalReferenceExpression: employee (OperationKind.LocalReferenceExpression, Type: DisplayStructContentsTest.Program.Employee) (Syntax: 'employee')
                                      InConversion: null
                                      OutConversion: null
                              InConversion: null
                              OutConversion: null
                  InConversion: null
                  OutConversion: null
";
            VerifyOperationTreeForTest<ForEachStatementSyntax>(source, expectedOperationTree);
        }

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
IForEachLoopStatement (Iteration variable: System.Char c) (LoopKind.ForEach) (OperationKind.LoopStatement) (Syntax: 'foreach (ch ... }')
  Collection: IConversionExpression (ConversionKind.Cast, Implicit) (OperationKind.ConversionExpression, Type: System.String) (Syntax: 's')
      Operand: ILocalReferenceExpression: s (OperationKind.LocalReferenceExpression, Type: System.String, Constant: """") (Syntax: 's')
  Body: IBlockStatement (1 statements) (OperationKind.BlockStatement) (Syntax: '{ ... }')
      IExpressionStatement (OperationKind.ExpressionStatement) (Syntax: 'System.Cons ... iteLine(c);')
        Expression: IInvocationExpression (void System.Console.WriteLine(System.Char value)) (OperationKind.InvocationExpression, Type: System.Void) (Syntax: 'System.Cons ... riteLine(c)')
            Instance Receiver: null
            Arguments(1):
                IArgument (ArgumentKind.Explicit, Matching Parameter: value) (OperationKind.Argument) (Syntax: 'c')
                  ILocalReferenceExpression: c (OperationKind.LocalReferenceExpression, Type: System.Char) (Syntax: 'c')
                  InConversion: null
                  OutConversion: null
";
            VerifyOperationTreeForTest<ForEachStatementSyntax>(source, expectedOperationTree);
        }

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
IForEachLoopStatement (Iteration variable: System.Collections.Generic.KeyValuePair<System.Int32, System.Int32> pair) (LoopKind.ForEach) (OperationKind.LoopStatement) (Syntax: 'foreach (va ... }')
  Collection: IConversionExpression (ConversionKind.Cast, Implicit) (OperationKind.ConversionExpression, Type: System.Collections.Generic.Dictionary<System.Int32, System.Int32>) (Syntax: '_f')
      Operand: IFieldReferenceExpression: System.Collections.Generic.Dictionary<System.Int32, System.Int32> Program._f (Static) (OperationKind.FieldReferenceExpression, Type: System.Collections.Generic.Dictionary<System.Int32, System.Int32>) (Syntax: '_f')
          Instance Receiver: null
  Body: IBlockStatement (1 statements) (OperationKind.BlockStatement) (Syntax: '{ ... }')
      IExpressionStatement (OperationKind.ExpressionStatement) (Syntax: 'System.Cons ... air.Value);')
        Expression: IInvocationExpression (void System.Console.WriteLine(System.String format, System.Object arg0, System.Object arg1)) (OperationKind.InvocationExpression, Type: System.Void) (Syntax: 'System.Cons ... pair.Value)')
            Instance Receiver: null
            Arguments(3):
                IArgument (ArgumentKind.Explicit, Matching Parameter: format) (OperationKind.Argument) (Syntax: '""{0},{1}""')
                  ILiteralExpression (OperationKind.LiteralExpression, Type: System.String, Constant: ""{0},{1}"") (Syntax: '""{0},{1}""')
                  InConversion: null
                  OutConversion: null
                IArgument (ArgumentKind.Explicit, Matching Parameter: arg0) (OperationKind.Argument) (Syntax: 'pair.Key')
                  IConversionExpression (ConversionKind.Cast, Implicit) (OperationKind.ConversionExpression, Type: System.Object) (Syntax: 'pair.Key')
                    Operand: IPropertyReferenceExpression: System.Int32 System.Collections.Generic.KeyValuePair<System.Int32, System.Int32>.Key { get; } (OperationKind.PropertyReferenceExpression, Type: System.Int32) (Syntax: 'pair.Key')
                        Instance Receiver: ILocalReferenceExpression: pair (OperationKind.LocalReferenceExpression, Type: System.Collections.Generic.KeyValuePair<System.Int32, System.Int32>) (Syntax: 'pair')
                  InConversion: null
                  OutConversion: null
                IArgument (ArgumentKind.Explicit, Matching Parameter: arg1) (OperationKind.Argument) (Syntax: 'pair.Value')
                  IConversionExpression (ConversionKind.Cast, Implicit) (OperationKind.ConversionExpression, Type: System.Object) (Syntax: 'pair.Value')
                    Operand: IPropertyReferenceExpression: System.Int32 System.Collections.Generic.KeyValuePair<System.Int32, System.Int32>.Value { get; } (OperationKind.PropertyReferenceExpression, Type: System.Int32) (Syntax: 'pair.Value')
                        Instance Receiver: ILocalReferenceExpression: pair (OperationKind.LocalReferenceExpression, Type: System.Collections.Generic.KeyValuePair<System.Int32, System.Int32>) (Syntax: 'pair')
                  InConversion: null
                  OutConversion: null
";
            VerifyOperationTreeForTest<ForEachStatementSyntax>(source, expectedOperationTree);
        }

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
IForEachLoopStatement (Iteration variable: MissingType x) (LoopKind.ForEach) (OperationKind.LoopStatement, IsInvalid) (Syntax: 'foreach (Mi ... }')
  Collection: ILocalReferenceExpression: sequence (OperationKind.LocalReferenceExpression, Type: System.Collections.IEnumerable) (Syntax: 'sequence')
  Body: IBlockStatement (1 statements, 1 locals) (OperationKind.BlockStatement) (Syntax: '{ ... }')
      Locals: Local_1: System.Boolean b
      IVariableDeclarationStatement (1 declarations) (OperationKind.VariableDeclarationStatement) (Syntax: 'bool b = !x ... uals(null);')
        IVariableDeclaration (1 variables) (OperationKind.VariableDeclaration) (Syntax: 'bool b = !x ... uals(null);')
          Variables: Local_1: System.Boolean b
          Initializer: IConversionExpression (ConversionKind.Cast, Implicit) (OperationKind.ConversionExpression, Type: System.Boolean) (Syntax: '!x.Equals(null)')
              Operand: IUnaryOperatorExpression (UnaryOperationKind.Invalid) (OperationKind.UnaryOperatorExpression, Type: System.Object) (Syntax: '!x.Equals(null)')
                  Operand: IInvocationExpression ( ? C.()) (OperationKind.InvocationExpression, Type: ?) (Syntax: 'x.Equals(null)')
                      Instance Receiver: IOperation:  (OperationKind.None) (Syntax: 'x.Equals')
                      Arguments(1):
                          IArgument (ArgumentKind.Explicit, Matching Parameter: null) (OperationKind.Argument) (Syntax: 'null')
                            ILiteralExpression (Text: null) (OperationKind.LiteralExpression, Type: null, Constant: null) (Syntax: 'null')
                            InConversion: null
                            OutConversion: null
";
            VerifyOperationTreeForTest<ForEachStatementSyntax>(source, expectedOperationTree);
        }

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
IForEachLoopStatement (Iteration variable: System.Int32 x) (LoopKind.ForEach) (OperationKind.LoopStatement, IsInvalid) (Syntax: 'foreach (in ... }')
  Collection: ILiteralExpression (Text: null) (OperationKind.LiteralExpression, Type: null, Constant: null, IsInvalid) (Syntax: 'null')
  Body: IBlockStatement (0 statements) (OperationKind.BlockStatement) (Syntax: '{ ... }')
";
            VerifyOperationTreeForTest<ForEachStatementSyntax>(source, expectedOperationTree);
        }

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
IForEachLoopStatement (Iteration variable: System.Int32 x) (LoopKind.ForEach) (OperationKind.LoopStatement, IsInvalid) (Syntax: 'foreach (in ... }')
  Collection: IConversionExpression (ConversionKind.Cast, Implicit) (OperationKind.ConversionExpression, Type: System.Collections.IEnumerable) (Syntax: 'args')
      Operand: IParameterReferenceExpression: args (OperationKind.ParameterReferenceExpression, Type: System.String[]) (Syntax: 'args')
  Body: IBlockStatement (0 statements) (OperationKind.BlockStatement) (Syntax: '{ ... }')
";
            VerifyOperationTreeForTest<ForEachStatementSyntax>(source, expectedOperationTree);
        }

        [Fact, WorkItem(17602, "https://github.com/dotnet/roslyn/issues/17602")]
        public void IForEachLoopStatement_ModifyIterationVariable()
        {
            string source = @"
class C
{
    void Foo(int[] a)
    {
        /*<bind>*/foreach (int x in a) { x++; }/*</bind>*/
    }
}
";
string expectedOperationTree = @"
IForEachLoopStatement (Iteration variable: System.Int32 x) (LoopKind.ForEach) (OperationKind.LoopStatement, IsInvalid) (Syntax: 'foreach (in ... a) { x++; }')
  Collection: IConversionExpression (ConversionKind.Cast, Implicit) (OperationKind.ConversionExpression, Type: System.Collections.IEnumerable) (Syntax: 'a')
      Operand: IParameterReferenceExpression: a (OperationKind.ParameterReferenceExpression, Type: System.Int32[]) (Syntax: 'a')
  Body: IBlockStatement (1 statements) (OperationKind.BlockStatement, IsInvalid) (Syntax: '{ x++; }')
      IExpressionStatement (OperationKind.ExpressionStatement, IsInvalid) (Syntax: 'x++;')
        Expression: IIncrementExpression (UnaryOperandKind.Invalid) (OperationKind.IncrementExpression, Type: System.Object, IsInvalid) (Syntax: 'x++')
            Left: IInvalidExpression (OperationKind.InvalidExpression, Type: System.Int32, IsInvalid) (Syntax: 'x')
                Children(1):
                    ILocalReferenceExpression: x (OperationKind.LocalReferenceExpression, Type: System.Int32, IsInvalid) (Syntax: 'x')
";
            VerifyOperationTreeForTest<ForEachStatementSyntax>(source, expectedOperationTree);
        }

        [Fact, WorkItem(17602, "https://github.com/dotnet/roslyn/issues/17602")]
        public void IForEachLoopStatement_Pattern()
        {
            string source = @"
class C
{
    void Foo(Enumerable e)
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
IForEachLoopStatement (Iteration variable: System.Int64 x) (LoopKind.ForEach) (OperationKind.LoopStatement) (Syntax: 'foreach (lo ... x in e) { }')
  Collection: IConversionExpression (ConversionKind.Cast, Implicit) (OperationKind.ConversionExpression, Type: Enumerable) (Syntax: 'e')
      Operand: IParameterReferenceExpression: e (OperationKind.ParameterReferenceExpression, Type: Enumerable) (Syntax: 'e')
  Body: IBlockStatement (0 statements) (OperationKind.BlockStatement) (Syntax: '{ }')
";
            VerifyOperationTreeForTest<ForEachStatementSyntax>(source, expectedOperationTree);
        }

        [Fact, WorkItem(17602, "https://github.com/dotnet/roslyn/issues/17602")]
        public void IForEachLoopStatement_ImplicitlyTypedString()
        {
            string source = @"
class C
{
    void Foo(string s)
    {
        /*<bind>*/foreach (var x in s) { }/*</bind>*/
    }
}
";
string expectedOperationTree = @"
IForEachLoopStatement (Iteration variable: System.Char x) (LoopKind.ForEach) (OperationKind.LoopStatement) (Syntax: 'foreach (var x in s) { }')
  Collection: IConversionExpression (ConversionKind.Cast, Implicit) (OperationKind.ConversionExpression, Type: System.String) (Syntax: 's')
      Operand: IParameterReferenceExpression: s (OperationKind.ParameterReferenceExpression, Type: System.String) (Syntax: 's')
  Body: IBlockStatement (0 statements) (OperationKind.BlockStatement) (Syntax: '{ }')
";
            VerifyOperationTreeForTest<ForEachStatementSyntax>(source, expectedOperationTree);
        }

        [Fact, WorkItem(17602, "https://github.com/dotnet/roslyn/issues/17602")]
        public void IForEachLoopStatement_ExplicitlyTypedVar()
        {
            string source = @"
class C
{
    void Foo(var[] a)
    {
        /*<bind>*/foreach (var x in a) { }/*</bind>*/
    }

    class var { }
}
";
string expectedOperationTree = @"
IForEachLoopStatement (Iteration variable: C.var x) (LoopKind.ForEach) (OperationKind.LoopStatement) (Syntax: 'foreach (var x in a) { }')
  Collection: IConversionExpression (ConversionKind.Cast, Implicit) (OperationKind.ConversionExpression, Type: System.Collections.IEnumerable) (Syntax: 'a')
      Operand: IParameterReferenceExpression: a (OperationKind.ParameterReferenceExpression, Type: C.var[]) (Syntax: 'a')
  Body: IBlockStatement (0 statements) (OperationKind.BlockStatement) (Syntax: '{ }')
";
            VerifyOperationTreeForTest<ForEachStatementSyntax>(source, expectedOperationTree);
        }

        [Fact, WorkItem(17602, "https://github.com/dotnet/roslyn/issues/17602")]
        public void IForEachLoopStatement_DynamicEnumerable()
        {
            string source = @"
class C
{
    void Foo(dynamic d)
    {
        /*<bind>*/foreach (int x in d) { }/*</bind>*/
    }
}
";
string expectedOperationTree = @"
IForEachLoopStatement (Iteration variable: System.Int32 x) (LoopKind.ForEach) (OperationKind.LoopStatement) (Syntax: 'foreach (int x in d) { }')
  Collection: IConversionExpression (ConversionKind.CSharp, Implicit) (OperationKind.ConversionExpression, Type: System.Collections.IEnumerable) (Syntax: 'd')
      Operand: IParameterReferenceExpression: d (OperationKind.ParameterReferenceExpression, Type: dynamic) (Syntax: 'd')
  Body: IBlockStatement (0 statements) (OperationKind.BlockStatement) (Syntax: '{ }')
";
            VerifyOperationTreeForTest<ForEachStatementSyntax>(source, expectedOperationTree);
        }

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
IForEachLoopStatement (Iteration variable: System.Object x) (LoopKind.ForEach) (OperationKind.LoopStatement) (Syntax: 'foreach (ob ... }')
  Collection: IConversionExpression (ConversionKind.Cast, Implicit) (OperationKind.ConversionExpression, Type: Enumerable<T>) (Syntax: 'new Enumerable<T>()')
      Operand: IObjectCreationExpression (Constructor: Enumerable<T>..ctor()) (OperationKind.ObjectCreationExpression, Type: Enumerable<T>) (Syntax: 'new Enumerable<T>()')
          Arguments(0)
          Initializer: null
  Body: IBlockStatement (1 statements) (OperationKind.BlockStatement) (Syntax: '{ ... }')
      IExpressionStatement (OperationKind.ExpressionStatement) (Syntax: 'System.Cons ... iteLine(x);')
        Expression: IInvocationExpression (void System.Console.WriteLine(System.Object value)) (OperationKind.InvocationExpression, Type: System.Void) (Syntax: 'System.Cons ... riteLine(x)')
            Instance Receiver: null
            Arguments(1):
                IArgument (ArgumentKind.Explicit, Matching Parameter: value) (OperationKind.Argument) (Syntax: 'x')
                  ILocalReferenceExpression: x (OperationKind.LocalReferenceExpression, Type: System.Object) (Syntax: 'x')
                  InConversion: null
                  OutConversion: null
";
            VerifyOperationTreeForTest<ForEachStatementSyntax>(source, expectedOperationTree);
        }

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
IForEachLoopStatement (Iteration variable: System.String x) (LoopKind.ForEach) (OperationKind.LoopStatement) (Syntax: 'foreach (st ... e)args) { }')
  Collection: IConversionExpression (ConversionKind.Cast, Implicit) (OperationKind.ConversionExpression, Type: System.Collections.IEnumerable) (Syntax: '(IEnumerable)args')
      Operand: IConversionExpression (ConversionKind.Cast, Explicit) (OperationKind.ConversionExpression, Type: System.Collections.IEnumerable) (Syntax: '(IEnumerable)args')
          Operand: IParameterReferenceExpression: args (OperationKind.ParameterReferenceExpression, Type: System.String[]) (Syntax: 'args')
  Body: IBlockStatement (0 statements) (OperationKind.BlockStatement) (Syntax: '{ }')
";

            VerifyOperationTreeForTest<ForEachStatementSyntax>(source, expectedOperationTree);
        }

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
IForEachLoopStatement (Iteration variable: System.Int32 num) (LoopKind.ForEach) (OperationKind.LoopStatement) (Syntax: 'foreach (in ... }')
  Collection: IConversionExpression (ConversionKind.Cast, Implicit) (OperationKind.ConversionExpression, Type: System.Collections.IEnumerable) (Syntax: 'numbers')
      Operand: ILocalReferenceExpression: numbers (OperationKind.LocalReferenceExpression, Type: System.Int32[]) (Syntax: 'numbers')
  Body: IBlockStatement (2 statements) (OperationKind.BlockStatement) (Syntax: '{ ... }')
      IIfStatement (OperationKind.IfStatement) (Syntax: 'if (num > 3 ... }')
        Condition: IBinaryOperatorExpression (BinaryOperationKind.IntegerGreaterThan) (OperationKind.BinaryOperatorExpression, Type: System.Boolean) (Syntax: 'num > 3')
            Left: ILocalReferenceExpression: num (OperationKind.LocalReferenceExpression, Type: System.Int32) (Syntax: 'num')
            Right: ILiteralExpression (Text: 3) (OperationKind.LiteralExpression, Type: System.Int32, Constant: 3) (Syntax: '3')
        IfTrue: IBlockStatement (1 statements) (OperationKind.BlockStatement) (Syntax: '{ ... }')
            IThrowStatement (OperationKind.ThrowStatement) (Syntax: 'throw new S ... ""testing"");')
              ThrownObject: IObjectCreationExpression (Constructor: System.Exception..ctor(System.String message)) (OperationKind.ObjectCreationExpression, Type: System.Exception) (Syntax: 'new System. ... (""testing"")')
                  Arguments(1):
                      IArgument (ArgumentKind.Explicit, Matching Parameter: message) (OperationKind.Argument) (Syntax: '""testing""')
                        ILiteralExpression (OperationKind.LiteralExpression, Type: System.String, Constant: ""testing"") (Syntax: '""testing""')
                        InConversion: null
                        OutConversion: null
                  Initializer: null
        IfFalse: null
      IExpressionStatement (OperationKind.ExpressionStatement) (Syntax: 'System.Cons ... eLine(num);')
        Expression: IInvocationExpression (void System.Console.WriteLine(System.Int32 value)) (OperationKind.InvocationExpression, Type: System.Void) (Syntax: 'System.Cons ... teLine(num)')
            Instance Receiver: null
            Arguments(1):
                IArgument (ArgumentKind.Explicit, Matching Parameter: value) (OperationKind.Argument) (Syntax: 'num')
                  ILocalReferenceExpression: num (OperationKind.LocalReferenceExpression, Type: System.Int32) (Syntax: 'num')
                  InConversion: null
                  OutConversion: null
";
            VerifyOperationTreeForTest<ForEachStatementSyntax>(source, expectedOperationTree);
        }
    }
}
