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
IForEachLoopStatement (Iteration variable: System.String value) (LoopKind.ForEach) (OperationKind.LoopStatement)
  Collection: IConversionExpression (ConversionKind.Cast, Implicit) (OperationKind.ConversionExpression, Type: System.Collections.IEnumerable)
      ILocalReferenceExpression: pets (OperationKind.LocalReferenceExpression, Type: System.String[])
  IBlockStatement (1 statements) (OperationKind.BlockStatement)
    IExpressionStatement (OperationKind.ExpressionStatement)
      IInvocationExpression (static void System.Console.WriteLine(System.String value)) (OperationKind.InvocationExpression, Type: System.Void)
        IArgument (Matching Parameter: value) (OperationKind.Argument)
          ILocalReferenceExpression: value (OperationKind.LocalReferenceExpression, Type: System.String)
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
IForEachLoopStatement (Iteration variable: System.String item) (LoopKind.ForEach) (OperationKind.LoopStatement)
  Collection: IConversionExpression (ConversionKind.Cast, Implicit) (OperationKind.ConversionExpression, Type: System.Collections.Generic.List<System.String>)
      ILocalReferenceExpression: list (OperationKind.LocalReferenceExpression, Type: System.Collections.Generic.List<System.String>)
  IBlockStatement (1 statements) (OperationKind.BlockStatement)
    IExpressionStatement (OperationKind.ExpressionStatement)
      IInvocationExpression (static void System.Console.WriteLine(System.String value)) (OperationKind.InvocationExpression, Type: System.Void)
        IArgument (Matching Parameter: value) (OperationKind.Argument)
          ILocalReferenceExpression: item (OperationKind.LocalReferenceExpression, Type: System.String)
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
IForEachLoopStatement (Iteration variable: System.Collections.Generic.KeyValuePair<System.Int32, System.Int32> pair) (LoopKind.ForEach) (OperationKind.LoopStatement)
  Collection: IConversionExpression (ConversionKind.Cast, Implicit) (OperationKind.ConversionExpression, Type: System.Collections.Generic.Dictionary<System.Int32, System.Int32>)
      IFieldReferenceExpression: System.Collections.Generic.Dictionary<System.Int32, System.Int32> Program._h (Static) (OperationKind.FieldReferenceExpression, Type: System.Collections.Generic.Dictionary<System.Int32, System.Int32>)
  IBlockStatement (1 statements) (OperationKind.BlockStatement)
    IExpressionStatement (OperationKind.ExpressionStatement)
      IInvocationExpression (static void System.Console.WriteLine(System.String format, System.Object arg0, System.Object arg1)) (OperationKind.InvocationExpression, Type: System.Void)
        IArgument (Matching Parameter: format) (OperationKind.Argument)
          ILiteralExpression (OperationKind.LiteralExpression, Type: System.String, Constant: {0},{1})
        IArgument (Matching Parameter: arg0) (OperationKind.Argument)
          IConversionExpression (ConversionKind.Cast, Implicit) (OperationKind.ConversionExpression, Type: System.Object)
            IPropertyReferenceExpression: System.Int32 System.Collections.Generic.KeyValuePair<System.Int32, System.Int32>.Key { get; } (OperationKind.PropertyReferenceExpression, Type: System.Int32)
              Instance Receiver: ILocalReferenceExpression: pair (OperationKind.LocalReferenceExpression, Type: System.Collections.Generic.KeyValuePair<System.Int32, System.Int32>)
        IArgument (Matching Parameter: arg1) (OperationKind.Argument)
          IConversionExpression (ConversionKind.Cast, Implicit) (OperationKind.ConversionExpression, Type: System.Object)
            IPropertyReferenceExpression: System.Int32 System.Collections.Generic.KeyValuePair<System.Int32, System.Int32>.Value { get; } (OperationKind.PropertyReferenceExpression, Type: System.Int32)
              Instance Receiver: ILocalReferenceExpression: pair (OperationKind.LocalReferenceExpression, Type: System.Collections.Generic.KeyValuePair<System.Int32, System.Int32>)
";
            VerifyOperationTreeForTest<ForEachStatementSyntax>(source, expectedOperationTree);
        }

        [Fact, WorkItem(17602, "https://github.com/dotnet/roslyn/issues/17602")]
        public void IForEachLoopStatement_WithYeild()
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
IForEachLoopStatement (Iteration variable: Class1.School school) (LoopKind.ForEach) (OperationKind.LoopStatement)
  Collection: IConversionExpression (ConversionKind.Cast, Implicit) (OperationKind.ConversionExpression, Type: System.Collections.Generic.IEnumerable<Class1.School>)
      IPropertyReferenceExpression: System.Collections.Generic.IEnumerable<Class1.School> Class1.Schools.NextGalaxy { get; } (OperationKind.PropertyReferenceExpression, Type: System.Collections.Generic.IEnumerable<Class1.School>)
        Instance Receiver: ILocalReferenceExpression: theSchools (OperationKind.LocalReferenceExpression, Type: Class1.Schools)
  IBlockStatement (1 statements) (OperationKind.BlockStatement)
    IExpressionStatement (OperationKind.ExpressionStatement)
      IInvocationExpression (static void System.Console.WriteLine(System.String value)) (OperationKind.InvocationExpression, Type: System.Void)
        IArgument (Matching Parameter: value) (OperationKind.Argument)
          IPropertyReferenceExpression: System.String Class1.School.Name { get; set; } (OperationKind.PropertyReferenceExpression, Type: System.String)
            Instance Receiver: ILocalReferenceExpression: school (OperationKind.LocalReferenceExpression, Type: Class1.School)
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
IForEachLoopStatement (Iteration variable: System.Int32 num) (LoopKind.ForEach) (OperationKind.LoopStatement)
  Collection: IConversionExpression (ConversionKind.Cast, Implicit) (OperationKind.ConversionExpression, Type: System.Collections.IEnumerable)
      ILocalReferenceExpression: numbers (OperationKind.LocalReferenceExpression, Type: System.Int32[])
  IBlockStatement (2 statements) (OperationKind.BlockStatement)
    IIfStatement (OperationKind.IfStatement)
      Condition: IBinaryOperatorExpression (BinaryOperationKind.IntegerGreaterThan) (OperationKind.BinaryOperatorExpression, Type: System.Boolean)
          Left: ILocalReferenceExpression: num (OperationKind.LocalReferenceExpression, Type: System.Int32)
          Right: ILiteralExpression (Text: 3) (OperationKind.LiteralExpression, Type: System.Int32, Constant: 3)
      IBlockStatement (1 statements) (OperationKind.BlockStatement)
        IBranchStatement (BranchKind.Break) (OperationKind.BranchStatement)
    IExpressionStatement (OperationKind.ExpressionStatement)
      IInvocationExpression (static void System.Console.WriteLine(System.Int32 value)) (OperationKind.InvocationExpression, Type: System.Void)
        IArgument (Matching Parameter: value) (OperationKind.Argument)
          ILocalReferenceExpression: num (OperationKind.LocalReferenceExpression, Type: System.Int32)
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
IForEachLoopStatement (Iteration variable: System.Int32 num) (LoopKind.ForEach) (OperationKind.LoopStatement)
  Collection: IConversionExpression (ConversionKind.Cast, Implicit) (OperationKind.ConversionExpression, Type: System.Collections.IEnumerable)
      ILocalReferenceExpression: numbers (OperationKind.LocalReferenceExpression, Type: System.Int32[])
  IBlockStatement (2 statements) (OperationKind.BlockStatement)
    IIfStatement (OperationKind.IfStatement)
      Condition: IBinaryOperatorExpression (BinaryOperationKind.IntegerGreaterThan) (OperationKind.BinaryOperatorExpression, Type: System.Boolean)
          Left: ILocalReferenceExpression: num (OperationKind.LocalReferenceExpression, Type: System.Int32)
          Right: ILiteralExpression (Text: 3) (OperationKind.LiteralExpression, Type: System.Int32, Constant: 3)
      IBlockStatement (1 statements) (OperationKind.BlockStatement)
        IBranchStatement (BranchKind.Continue) (OperationKind.BranchStatement)
    IExpressionStatement (OperationKind.ExpressionStatement)
      IInvocationExpression (static void System.Console.WriteLine(System.Int32 value)) (OperationKind.InvocationExpression, Type: System.Void)
        IArgument (Matching Parameter: value) (OperationKind.Argument)
          ILocalReferenceExpression: num (OperationKind.LocalReferenceExpression, Type: System.Int32)
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
IForEachLoopStatement (Iteration variable: System.Int32[] subArray) (LoopKind.ForEach) (OperationKind.LoopStatement)
  Collection: IConversionExpression (ConversionKind.Cast, Implicit) (OperationKind.ConversionExpression, Type: System.Collections.IEnumerable)
      ILocalReferenceExpression: array (OperationKind.LocalReferenceExpression, Type: System.Int32[][])
  IBlockStatement (1 statements) (OperationKind.BlockStatement)
    IForEachLoopStatement (Iteration variable: System.Int32 i) (LoopKind.ForEach) (OperationKind.LoopStatement)
      Collection: IConversionExpression (ConversionKind.Cast, Implicit) (OperationKind.ConversionExpression, Type: System.Collections.IEnumerable)
          ILocalReferenceExpression: subArray (OperationKind.LocalReferenceExpression, Type: System.Int32[])
      IBlockStatement (1 statements) (OperationKind.BlockStatement)
        IExpressionStatement (OperationKind.ExpressionStatement)
          IInvocationExpression (static void System.Console.Write(System.Int32 value)) (OperationKind.InvocationExpression, Type: System.Void)
            IArgument (Matching Parameter: value) (OperationKind.Argument)
              ILocalReferenceExpression: i (OperationKind.LocalReferenceExpression, Type: System.Int32)
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
IForEachLoopStatement (Iteration variable: System.Int32 i) (LoopKind.ForEach) (OperationKind.LoopStatement)
  Collection: IConversionExpression (ConversionKind.Cast, Implicit) (OperationKind.ConversionExpression, Type: System.Collections.Generic.IEnumerable<System.Int32>)
      IInvocationExpression (static System.Collections.Generic.IEnumerable<System.Int32> System.Linq.Enumerable.Range(System.Int32 start, System.Int32 count)) (OperationKind.InvocationExpression, Type: System.Collections.Generic.IEnumerable<System.Int32>)
        IArgument (Matching Parameter: start) (OperationKind.Argument)
          ILiteralExpression (Text: 0) (OperationKind.LiteralExpression, Type: System.Int32, Constant: 0)
        IArgument (Matching Parameter: count) (OperationKind.Argument)
          IInvocationExpression ( System.Int32 System.Array.GetLength(System.Int32 dimension)) (OperationKind.InvocationExpression, Type: System.Int32)
            Instance Receiver: ILocalReferenceExpression: array (OperationKind.LocalReferenceExpression, Type: System.Int32[][])
            IArgument (Matching Parameter: dimension) (OperationKind.Argument)
              ILiteralExpression (Text: 0) (OperationKind.LiteralExpression, Type: System.Int32, Constant: 0)
  IForEachLoopStatement (Iteration variable: System.Int32 j) (LoopKind.ForEach) (OperationKind.LoopStatement)
    Collection: IConversionExpression (ConversionKind.Cast, Implicit) (OperationKind.ConversionExpression, Type: System.Collections.Generic.IEnumerable<System.Int32>)
        IInvocationExpression (static System.Collections.Generic.IEnumerable<System.Int32> System.Linq.Enumerable.Range(System.Int32 start, System.Int32 count)) (OperationKind.InvocationExpression, Type: System.Collections.Generic.IEnumerable<System.Int32>)
          IArgument (Matching Parameter: start) (OperationKind.Argument)
            ILiteralExpression (Text: 0) (OperationKind.LiteralExpression, Type: System.Int32, Constant: 0)
          IArgument (Matching Parameter: count) (OperationKind.Argument)
            IInvocationExpression ( System.Int32 System.Array.GetLength(System.Int32 dimension)) (OperationKind.InvocationExpression, Type: System.Int32)
              Instance Receiver: ILocalReferenceExpression: array (OperationKind.LocalReferenceExpression, Type: System.Int32[][])
              IArgument (Matching Parameter: dimension) (OperationKind.Argument)
                ILiteralExpression (Text: 1) (OperationKind.LiteralExpression, Type: System.Int32, Constant: 1)
    IExpressionStatement (OperationKind.ExpressionStatement)
      IInvocationExpression (static void System.Console.WriteLine(System.Int32 value)) (OperationKind.InvocationExpression, Type: System.Void)
        IArgument (Matching Parameter: value) (OperationKind.Argument)
          IArrayElementReferenceExpression (OperationKind.ArrayElementReferenceExpression, Type: System.Int32)
            IArrayElementReferenceExpression (OperationKind.ArrayElementReferenceExpression, Type: System.Int32[])
              ILocalReferenceExpression: array (OperationKind.LocalReferenceExpression, Type: System.Int32[][])
              Indices: ILocalReferenceExpression: i (OperationKind.LocalReferenceExpression, Type: System.Int32)
            Indices: ILocalReferenceExpression: j (OperationKind.LocalReferenceExpression, Type: System.Int32)
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
IForEachLoopStatement (Iteration variable: System.String value) (LoopKind.ForEach) (OperationKind.LoopStatement, IsInvalid)
  Collection: ILocalReferenceExpression: sorted (OperationKind.LocalReferenceExpression, Type: ?)
  IBlockStatement (1 statements) (OperationKind.BlockStatement)
    IExpressionStatement (OperationKind.ExpressionStatement)
      IInvocationExpression (static void System.Console.WriteLine(System.String value)) (OperationKind.InvocationExpression, Type: System.Void)
        IArgument (Matching Parameter: value) (OperationKind.Argument)
          ILocalReferenceExpression: value (OperationKind.LocalReferenceExpression, Type: System.String)
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
IForEachLoopStatement (Iteration variable: System.Reflection.FieldInfo fi) (LoopKind.ForEach) (OperationKind.LoopStatement)
  Collection: IConversionExpression (ConversionKind.Cast, Implicit) (OperationKind.ConversionExpression, Type: System.Collections.IEnumerable)
      IInvocationExpression ( System.Reflection.FieldInfo[] System.Type.GetFields()) (OperationKind.InvocationExpression, Type: System.Reflection.FieldInfo[])
        Instance Receiver: IInvocationExpression ( System.Type System.Object.GetType()) (OperationKind.InvocationExpression, Type: System.Type)
            Instance Receiver: ILocalReferenceExpression: employee (OperationKind.LocalReferenceExpression, Type: DisplayStructContentsTest.Program.Employee)
  IBlockStatement (1 statements) (OperationKind.BlockStatement)
    IExpressionStatement (OperationKind.ExpressionStatement)
      IInvocationExpression (static void System.Console.WriteLine(System.String value)) (OperationKind.InvocationExpression, Type: System.Void)
        IArgument (Matching Parameter: value) (OperationKind.Argument)
          IBinaryOperatorExpression (BinaryOperationKind.StringConcatenate) (OperationKind.BinaryOperatorExpression, Type: System.String)
            Left: IBinaryOperatorExpression (BinaryOperationKind.StringConcatenate) (OperationKind.BinaryOperatorExpression, Type: System.String)
                Left: IPropertyReferenceExpression: System.String System.Reflection.MemberInfo.Name { get; } (OperationKind.PropertyReferenceExpression, Type: System.String)
                    Instance Receiver: ILocalReferenceExpression: fi (OperationKind.LocalReferenceExpression, Type: System.Reflection.FieldInfo)
                Right: ILiteralExpression (OperationKind.LiteralExpression, Type: System.String, Constant:  = )
            Right: IInvocationExpression (static System.String System.Convert.ToString(System.Object value)) (OperationKind.InvocationExpression, Type: System.String)
                IArgument (Matching Parameter: value) (OperationKind.Argument)
                  IInvocationExpression (virtual System.Object System.Reflection.FieldInfo.GetValue(System.Object obj)) (OperationKind.InvocationExpression, Type: System.Object)
                    Instance Receiver: ILocalReferenceExpression: fi (OperationKind.LocalReferenceExpression, Type: System.Reflection.FieldInfo)
                    IArgument (Matching Parameter: obj) (OperationKind.Argument)
                      IConversionExpression (ConversionKind.Cast, Implicit) (OperationKind.ConversionExpression, Type: System.Object)
                        ILocalReferenceExpression: employee (OperationKind.LocalReferenceExpression, Type: DisplayStructContentsTest.Program.Employee)
";
            VerifyOperationTreeForTest<ForEachStatementSyntax>(source, expectedOperationTree);
        }

        [Fact, WorkItem(17602, "https://github.com/dotnet/roslyn/issues/17602")]
        public void IForEachLoopStatement_ConstantNull()
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
IForEachLoopStatement (Iteration variable: System.Char c) (LoopKind.ForEach) (OperationKind.LoopStatement)
  Collection: IConversionExpression (ConversionKind.Cast, Implicit) (OperationKind.ConversionExpression, Type: System.String)
      ILocalReferenceExpression: s (OperationKind.LocalReferenceExpression, Type: System.String, Constant: )
  IBlockStatement (1 statements) (OperationKind.BlockStatement)
    IExpressionStatement (OperationKind.ExpressionStatement)
      IInvocationExpression (static void System.Console.WriteLine(System.Char value)) (OperationKind.InvocationExpression, Type: System.Void)
        IArgument (Matching Parameter: value) (OperationKind.Argument)
          ILocalReferenceExpression: c (OperationKind.LocalReferenceExpression, Type: System.Char)
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
IForEachLoopStatement (Iteration variable: System.Collections.Generic.KeyValuePair<System.Int32, System.Int32> pair) (LoopKind.ForEach) (OperationKind.LoopStatement)
  Collection: IConversionExpression (ConversionKind.Cast, Implicit) (OperationKind.ConversionExpression, Type: System.Collections.Generic.Dictionary<System.Int32, System.Int32>)
      IFieldReferenceExpression: System.Collections.Generic.Dictionary<System.Int32, System.Int32> Program._f (Static) (OperationKind.FieldReferenceExpression, Type: System.Collections.Generic.Dictionary<System.Int32, System.Int32>)
  IBlockStatement (1 statements) (OperationKind.BlockStatement)
    IExpressionStatement (OperationKind.ExpressionStatement)
      IInvocationExpression (static void System.Console.WriteLine(System.String format, System.Object arg0, System.Object arg1)) (OperationKind.InvocationExpression, Type: System.Void)
        IArgument (Matching Parameter: format) (OperationKind.Argument)
          ILiteralExpression (OperationKind.LiteralExpression, Type: System.String, Constant: {0},{1})
        IArgument (Matching Parameter: arg0) (OperationKind.Argument)
          IConversionExpression (ConversionKind.Cast, Implicit) (OperationKind.ConversionExpression, Type: System.Object)
            IPropertyReferenceExpression: System.Int32 System.Collections.Generic.KeyValuePair<System.Int32, System.Int32>.Key { get; } (OperationKind.PropertyReferenceExpression, Type: System.Int32)
              Instance Receiver: ILocalReferenceExpression: pair (OperationKind.LocalReferenceExpression, Type: System.Collections.Generic.KeyValuePair<System.Int32, System.Int32>)
        IArgument (Matching Parameter: arg1) (OperationKind.Argument)
          IConversionExpression (ConversionKind.Cast, Implicit) (OperationKind.ConversionExpression, Type: System.Object)
            IPropertyReferenceExpression: System.Int32 System.Collections.Generic.KeyValuePair<System.Int32, System.Int32>.Value { get; } (OperationKind.PropertyReferenceExpression, Type: System.Int32)
              Instance Receiver: ILocalReferenceExpression: pair (OperationKind.LocalReferenceExpression, Type: System.Collections.Generic.KeyValuePair<System.Int32, System.Int32>)
";
            VerifyOperationTreeForTest<ForEachStatementSyntax>(source, expectedOperationTree);
        }

        [Fact, WorkItem(17602, "https://github.com/dotnet/roslyn/issues/17602")]
        public void IForEachLoopStatement_ForEachOutVar()
        {
            string source = @"
class P
{
    private void M()
    {
        var s = """";
        /*<bind>*/foreach (var j in new[] { int.TryParse(s, out var i) ? i : 0 })
        {
            System.Console.WriteLine($""i={i}, s={s}""); 
        }/*</bind>*/
    }
}
";
            string expectedOperationTree = @"
IForEachLoopStatement (Iteration variable: System.Int32 j) (LoopKind.ForEach) (OperationKind.LoopStatement)
  Collection: IConversionExpression (ConversionKind.Cast, Implicit) (OperationKind.ConversionExpression, Type: System.Collections.IEnumerable)
      IArrayCreationExpression (Dimension sizes: 1, Element Type: System.Int32) (OperationKind.ArrayCreationExpression, Type: System.Int32[])
        ILiteralExpression (OperationKind.LiteralExpression, Type: System.Int32, Constant: 1)
        IArrayInitializer (OperationKind.ArrayInitializer)
          IConditionalChoiceExpression (OperationKind.ConditionalChoiceExpression, Type: System.Int32)
            Condition: IInvocationExpression (static System.Boolean System.Int32.TryParse(System.String s, out System.Int32 result)) (OperationKind.InvocationExpression, Type: System.Boolean)
                IArgument (Matching Parameter: s) (OperationKind.Argument)
                  ILocalReferenceExpression: s (OperationKind.LocalReferenceExpression, Type: System.String)
                IArgument (Matching Parameter: result) (OperationKind.Argument)
                  ILocalReferenceExpression: i (OperationKind.LocalReferenceExpression, Type: System.Int32)
            IfTrue: ILocalReferenceExpression: i (OperationKind.LocalReferenceExpression, Type: System.Int32)
            IfFalse: ILiteralExpression (Text: 0) (OperationKind.LiteralExpression, Type: System.Int32, Constant: 0)
  IBlockStatement (1 statements) (OperationKind.BlockStatement)
    IExpressionStatement (OperationKind.ExpressionStatement)
      IInvocationExpression (static void System.Console.WriteLine(System.String value)) (OperationKind.InvocationExpression, Type: System.Void)
        IArgument (Matching Parameter: value) (OperationKind.Argument)
          IOperation:  (OperationKind.None)
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
IForEachLoopStatement (Iteration variable: MissingType x) (LoopKind.ForEach) (OperationKind.LoopStatement, IsInvalid)
  Collection: ILocalReferenceExpression: sequence (OperationKind.LocalReferenceExpression, Type: System.Collections.IEnumerable)
  IBlockStatement (1 statements, 1 locals) (OperationKind.BlockStatement, IsInvalid)
    Local_1: System.Boolean b
    IVariableDeclarationStatement (1 variables) (OperationKind.VariableDeclarationStatement, IsInvalid)
      IVariableDeclaration: System.Boolean b (OperationKind.VariableDeclaration, IsInvalid)
        Initializer: IConversionExpression (ConversionKind.Cast, Implicit) (OperationKind.ConversionExpression, Type: System.Boolean, IsInvalid)
            IUnaryOperatorExpression (UnaryOperationKind.Invalid) (OperationKind.UnaryOperatorExpression, Type: System.Object, IsInvalid)
              IInvocationExpression ( ? C.()) (OperationKind.InvocationExpression, Type: ?, IsInvalid)
                Instance Receiver: IOperation:  (OperationKind.None)
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
IForEachLoopStatement (Iteration variable: System.Int32 x) (LoopKind.ForEach) (OperationKind.LoopStatement, IsInvalid)
  Collection: ILiteralExpression (Text: null) (OperationKind.LiteralExpression, Type: null, Constant: null)
  IBlockStatement (0 statements) (OperationKind.BlockStatement)
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
IForEachLoopStatement (Iteration variable: System.Int32 x) (LoopKind.ForEach) (OperationKind.LoopStatement, IsInvalid)
  Collection: IConversionExpression (ConversionKind.Cast, Implicit) (OperationKind.ConversionExpression, Type: System.Collections.IEnumerable)
      IParameterReferenceExpression: args (OperationKind.ParameterReferenceExpression, Type: System.String[])
  IBlockStatement (0 statements) (OperationKind.BlockStatement)
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
IForEachLoopStatement (Iteration variable: System.Int32 x) (LoopKind.ForEach) (OperationKind.LoopStatement, IsInvalid)
  Collection: IConversionExpression (ConversionKind.Cast, Implicit) (OperationKind.ConversionExpression, Type: System.Collections.IEnumerable)
      IParameterReferenceExpression: a (OperationKind.ParameterReferenceExpression, Type: System.Int32[])
  IBlockStatement (1 statements) (OperationKind.BlockStatement, IsInvalid)
    IExpressionStatement (OperationKind.ExpressionStatement, IsInvalid)
      IIncrementExpression (UnaryOperandKind.Invalid) (BinaryOperationKind.Invalid) (OperationKind.IncrementExpression, Type: System.Object, IsInvalid)
        Left: IInvalidExpression (OperationKind.InvalidExpression, Type: System.Int32, IsInvalid)
        Right: ILiteralExpression (OperationKind.LiteralExpression, Type: System.Object, Constant: 1)
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
IForEachLoopStatement (Iteration variable: System.Int64 x) (LoopKind.ForEach) (OperationKind.LoopStatement)
  Collection: IConversionExpression (ConversionKind.Cast, Implicit) (OperationKind.ConversionExpression, Type: Enumerable)
      IParameterReferenceExpression: e (OperationKind.ParameterReferenceExpression, Type: Enumerable)
  IBlockStatement (0 statements) (OperationKind.BlockStatement)
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
IForEachLoopStatement (Iteration variable: System.Char x) (LoopKind.ForEach) (OperationKind.LoopStatement)
  Collection: IConversionExpression (ConversionKind.Cast, Implicit) (OperationKind.ConversionExpression, Type: System.String)
      IParameterReferenceExpression: s (OperationKind.ParameterReferenceExpression, Type: System.String)
  IBlockStatement (0 statements) (OperationKind.BlockStatement)
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
IForEachLoopStatement (Iteration variable: C.var x) (LoopKind.ForEach) (OperationKind.LoopStatement)
  Collection: IConversionExpression (ConversionKind.Cast, Implicit) (OperationKind.ConversionExpression, Type: System.Collections.IEnumerable)
      IParameterReferenceExpression: a (OperationKind.ParameterReferenceExpression, Type: C.var[])
  IBlockStatement (0 statements) (OperationKind.BlockStatement)
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
IForEachLoopStatement (Iteration variable: System.Int32 x) (LoopKind.ForEach) (OperationKind.LoopStatement)
  Collection: IConversionExpression (ConversionKind.CSharp, Implicit) (OperationKind.ConversionExpression, Type: System.Collections.IEnumerable)
      IParameterReferenceExpression: d (OperationKind.ParameterReferenceExpression, Type: dynamic)
  IBlockStatement (0 statements) (OperationKind.BlockStatement)
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
IForEachLoopStatement (Iteration variable: System.Object x) (LoopKind.ForEach) (OperationKind.LoopStatement)
  Collection: IConversionExpression (ConversionKind.Cast, Implicit) (OperationKind.ConversionExpression, Type: Enumerable<T>)
      IObjectCreationExpression (Constructor: Enumerable<T>..ctor()) (OperationKind.ObjectCreationExpression, Type: Enumerable<T>)
  IBlockStatement (1 statements) (OperationKind.BlockStatement)
    IExpressionStatement (OperationKind.ExpressionStatement)
      IInvocationExpression (static void System.Console.WriteLine(System.Object value)) (OperationKind.InvocationExpression, Type: System.Void)
        IArgument (Matching Parameter: value) (OperationKind.Argument)
          ILocalReferenceExpression: x (OperationKind.LocalReferenceExpression, Type: System.Object)
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
        /*<bind>*/foreach (C x in (IEnumerable)args) { }/*</bind>*/
    }

    public static implicit operator C(string s)
    {
        return new C();
    }
}
";
            string expectedOperationTree = @"
IForEachLoopStatement (Iteration variable: C x) (LoopKind.ForEach) (OperationKind.LoopStatement)
  Collection: IConversionExpression (ConversionKind.Cast, Implicit) (OperationKind.ConversionExpression, Type: System.Collections.IEnumerable)
      IConversionExpression (ConversionKind.Cast, Explicit) (OperationKind.ConversionExpression, Type: System.Collections.IEnumerable)
        IParameterReferenceExpression: args (OperationKind.ParameterReferenceExpression, Type: System.String[])
  IBlockStatement (0 statements) (OperationKind.BlockStatement)
";
            VerifyOperationTreeForTest<ForEachStatementSyntax>(source, expectedOperationTree);
        }

        [Fact, WorkItem(17602, "https://github.com/dotnet/roslyn/issues/17602")]
        public void IForEachLoopStatement_Withhrow()
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
IForEachLoopStatement (Iteration variable: System.Int32 num) (LoopKind.ForEach) (OperationKind.LoopStatement)
  Collection: IConversionExpression (ConversionKind.Cast, Implicit) (OperationKind.ConversionExpression, Type: System.Collections.IEnumerable)
      ILocalReferenceExpression: numbers (OperationKind.LocalReferenceExpression, Type: System.Int32[])
  IBlockStatement (2 statements) (OperationKind.BlockStatement)
    IIfStatement (OperationKind.IfStatement)
      Condition: IBinaryOperatorExpression (BinaryOperationKind.IntegerGreaterThan) (OperationKind.BinaryOperatorExpression, Type: System.Boolean)
          Left: ILocalReferenceExpression: num (OperationKind.LocalReferenceExpression, Type: System.Int32)
          Right: ILiteralExpression (Text: 3) (OperationKind.LiteralExpression, Type: System.Int32, Constant: 3)
      IBlockStatement (1 statements) (OperationKind.BlockStatement)
        IThrowStatement (OperationKind.ThrowStatement)
          IObjectCreationExpression (Constructor: System.Exception..ctor(System.String message)) (OperationKind.ObjectCreationExpression, Type: System.Exception)
            Arguments: IArgument (Matching Parameter: message) (OperationKind.Argument)
                ILiteralExpression (OperationKind.LiteralExpression, Type: System.String, Constant: testing)
    IExpressionStatement (OperationKind.ExpressionStatement)
      IInvocationExpression (static void System.Console.WriteLine(System.Int32 value)) (OperationKind.InvocationExpression, Type: System.Void)
        IArgument (Matching Parameter: value) (OperationKind.Argument)
          ILocalReferenceExpression: num (OperationKind.LocalReferenceExpression, Type: System.Int32)
";
            VerifyOperationTreeForTest<ForEachStatementSyntax>(source, expectedOperationTree);
        }
    }
}
