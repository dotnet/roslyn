// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Test.Utilities;
using Xunit;


namespace Microsoft.CodeAnalysis.CSharp.UnitTests
{
    public partial class IOperationTests : SemanticModelTestBase
    {
        [Fact, WorkItem(17602, "https://github.com/dotnet/roslyn/issues/17602")]
        public void IForEachLoopStatement_Array()
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
      ILocalReferenceExpression: pets (OperationKind.LocalReferenceExpression, Type: System.String[]) (Syntax: 'pets')
  Body: IBlockStatement (1 statements) (OperationKind.BlockStatement) (Syntax: '{ ... }')
      IExpressionStatement (OperationKind.ExpressionStatement) (Syntax: 'System.Cons ... ine(value);')
        IInvocationExpression (static void System.Console.WriteLine(System.String value)) (OperationKind.InvocationExpression, Type: System.Void) (Syntax: 'System.Cons ... Line(value)')
          Arguments(1): IArgument (ArgumentKind.Explicit, Matching Parameter: value) (OperationKind.Argument) (Syntax: 'value')
              ILocalReferenceExpression: value (OperationKind.LocalReferenceExpression, Type: System.String) (Syntax: 'value')
";
            var expectedDiagnostics = DiagnosticDescription.None;

            VerifyOperationTreeAndDiagnosticsForTest<ForEachStatementSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }


        [Fact, WorkItem(17602, "https://github.com/dotnet/roslyn/issues/17602")]
        public void IForEachLoopStatement_List()
        {
            string source = @"
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
            System.Console.WriteLine(item);
        }/*</bind>*/
    }
}
";
            string expectedOperationTree = @"
IForEachLoopStatement (Iteration variable: System.String item) (LoopKind.ForEach) (OperationKind.LoopStatement) (Syntax: 'foreach (st ... }')
  Collection: IConversionExpression (ConversionKind.Cast, Implicit) (OperationKind.ConversionExpression, Type: System.Collections.Generic.List<System.String>) (Syntax: 'list')
      ILocalReferenceExpression: list (OperationKind.LocalReferenceExpression, Type: System.Collections.Generic.List<System.String>) (Syntax: 'list')
  Body: IBlockStatement (1 statements) (OperationKind.BlockStatement) (Syntax: '{ ... }')
      IExpressionStatement (OperationKind.ExpressionStatement) (Syntax: 'System.Cons ... Line(item);')
        IInvocationExpression (static void System.Console.WriteLine(System.String value)) (OperationKind.InvocationExpression, Type: System.Void) (Syntax: 'System.Cons ... eLine(item)')
          Arguments(1): IArgument (ArgumentKind.Explicit, Matching Parameter: value) (OperationKind.Argument) (Syntax: 'item')
              ILocalReferenceExpression: item (OperationKind.LocalReferenceExpression, Type: System.String) (Syntax: 'item')
";
            var expectedDiagnostics = DiagnosticDescription.None;

            VerifyOperationTreeAndDiagnosticsForTest<ForEachStatementSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }

        [Fact, WorkItem(17602, "https://github.com/dotnet/roslyn/issues/17602")]
        public void IForEachLoopStatement_Dictionary()
        {
            string source = @"
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
            System.Console.WriteLine(""{0},{1}"", pair.Key, pair.Value);
        }/*</bind>*/
    }
}
";
            string expectedOperationTree = @"
IForEachLoopStatement (Iteration variable: System.Collections.Generic.KeyValuePair<System.Int32, System.Int32> pair) (LoopKind.ForEach) (OperationKind.LoopStatement) (Syntax: 'foreach (Ke ... }')
  Collection: IConversionExpression (ConversionKind.Cast, Implicit) (OperationKind.ConversionExpression, Type: System.Collections.Generic.Dictionary<System.Int32, System.Int32>) (Syntax: '_h')
      IFieldReferenceExpression: System.Collections.Generic.Dictionary<System.Int32, System.Int32> Program._h (Static) (OperationKind.FieldReferenceExpression, Type: System.Collections.Generic.Dictionary<System.Int32, System.Int32>) (Syntax: '_h')
  Body: IBlockStatement (1 statements) (OperationKind.BlockStatement) (Syntax: '{ ... }')
      IExpressionStatement (OperationKind.ExpressionStatement) (Syntax: 'System.Cons ... air.Value);')
        IInvocationExpression (static void System.Console.WriteLine(System.String format, System.Object arg0, System.Object arg1)) (OperationKind.InvocationExpression, Type: System.Void) (Syntax: 'System.Cons ... pair.Value)')
          Arguments(3): IArgument (ArgumentKind.Explicit, Matching Parameter: format) (OperationKind.Argument) (Syntax: '""{0},{1}""')
              ILiteralExpression (OperationKind.LiteralExpression, Type: System.String, Constant: ""{0},{1}"") (Syntax: '""{0},{1}""')
            IArgument (ArgumentKind.Explicit, Matching Parameter: arg0) (OperationKind.Argument) (Syntax: 'pair.Key')
              IConversionExpression (ConversionKind.Cast, Implicit) (OperationKind.ConversionExpression, Type: System.Object) (Syntax: 'pair.Key')
                IPropertyReferenceExpression: System.Int32 System.Collections.Generic.KeyValuePair<System.Int32, System.Int32>.Key { get; } (OperationKind.PropertyReferenceExpression, Type: System.Int32) (Syntax: 'pair.Key')
                  Instance Receiver: ILocalReferenceExpression: pair (OperationKind.LocalReferenceExpression, Type: System.Collections.Generic.KeyValuePair<System.Int32, System.Int32>) (Syntax: 'pair')
            IArgument (ArgumentKind.Explicit, Matching Parameter: arg1) (OperationKind.Argument) (Syntax: 'pair.Value')
              IConversionExpression (ConversionKind.Cast, Implicit) (OperationKind.ConversionExpression, Type: System.Object) (Syntax: 'pair.Value')
                IPropertyReferenceExpression: System.Int32 System.Collections.Generic.KeyValuePair<System.Int32, System.Int32>.Value { get; } (OperationKind.PropertyReferenceExpression, Type: System.Int32) (Syntax: 'pair.Value')
                  Instance Receiver: ILocalReferenceExpression: pair (OperationKind.LocalReferenceExpression, Type: System.Collections.Generic.KeyValuePair<System.Int32, System.Int32>) (Syntax: 'pair')
";
            var expectedDiagnostics = DiagnosticDescription.None;

            VerifyOperationTreeAndDiagnosticsForTest<ForEachStatementSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }

        [Fact, WorkItem(17602, "https://github.com/dotnet/roslyn/issues/17602")]
        public void IForEachLoopStatement_Yield()
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
                yield return new School { Name = ""t"", Years = 400 };
                yield return new School { Name = ""p"", Years = 25 };
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
      IPropertyReferenceExpression: System.Collections.Generic.IEnumerable<Class1.School> Class1.Schools.NextGalaxy { get; } (OperationKind.PropertyReferenceExpression, Type: System.Collections.Generic.IEnumerable<Class1.School>) (Syntax: 'theSchools.NextGalaxy')
        Instance Receiver: ILocalReferenceExpression: theSchools (OperationKind.LocalReferenceExpression, Type: Class1.Schools) (Syntax: 'theSchools')
  Body: IBlockStatement (1 statements) (OperationKind.BlockStatement) (Syntax: '{ ... }')
      IExpressionStatement (OperationKind.ExpressionStatement) (Syntax: 'System.Cons ... hool.Name);')
        IInvocationExpression (static void System.Console.WriteLine(System.String value)) (OperationKind.InvocationExpression, Type: System.Void) (Syntax: 'System.Cons ... chool.Name)')
          Arguments(1): IArgument (ArgumentKind.Explicit, Matching Parameter: value) (OperationKind.Argument) (Syntax: 'school.Name')
              IPropertyReferenceExpression: System.String Class1.School.Name { get; set; } (OperationKind.PropertyReferenceExpression, Type: System.String) (Syntax: 'school.Name')
                Instance Receiver: ILocalReferenceExpression: school (OperationKind.LocalReferenceExpression, Type: Class1.School) (Syntax: 'school')
";
            var expectedDiagnostics = DiagnosticDescription.None;

            VerifyOperationTreeAndDiagnosticsForTest<ForEachStatementSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }

        [Fact, WorkItem(17602, "https://github.com/dotnet/roslyn/issues/17602")]
        public void IForEachLoopStatement_WithBreak()
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
      ILocalReferenceExpression: numbers (OperationKind.LocalReferenceExpression, Type: System.Int32[]) (Syntax: 'numbers')
  Body: IBlockStatement (2 statements) (OperationKind.BlockStatement) (Syntax: '{ ... }')
      IIfStatement (OperationKind.IfStatement) (Syntax: 'if (num > 3 ... }')
        Condition: IBinaryOperatorExpression (BinaryOperationKind.IntegerGreaterThan) (OperationKind.BinaryOperatorExpression, Type: System.Boolean) (Syntax: 'num > 3')
            Left: ILocalReferenceExpression: num (OperationKind.LocalReferenceExpression, Type: System.Int32) (Syntax: 'num')
            Right: ILiteralExpression (Text: 3) (OperationKind.LiteralExpression, Type: System.Int32, Constant: 3) (Syntax: '3')
        IfTrue: IBlockStatement (1 statements) (OperationKind.BlockStatement) (Syntax: '{ ... }')
            IBranchStatement (BranchKind.Break) (OperationKind.BranchStatement) (Syntax: 'break;')
      IExpressionStatement (OperationKind.ExpressionStatement) (Syntax: 'System.Cons ... eLine(num);')
        IInvocationExpression (static void System.Console.WriteLine(System.Int32 value)) (OperationKind.InvocationExpression, Type: System.Void) (Syntax: 'System.Cons ... teLine(num)')
          Arguments(1): IArgument (ArgumentKind.Explicit, Matching Parameter: value) (OperationKind.Argument) (Syntax: 'num')
              ILocalReferenceExpression: num (OperationKind.LocalReferenceExpression, Type: System.Int32) (Syntax: 'num')
";
            var expectedDiagnostics = DiagnosticDescription.None;

            VerifyOperationTreeAndDiagnosticsForTest<ForEachStatementSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }

        [Fact, WorkItem(17602, "https://github.com/dotnet/roslyn/issues/17602")]
        public void IForEachLoopStatement_Continue()
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
      ILocalReferenceExpression: numbers (OperationKind.LocalReferenceExpression, Type: System.Int32[]) (Syntax: 'numbers')
  Body: IBlockStatement (2 statements) (OperationKind.BlockStatement) (Syntax: '{ ... }')
      IIfStatement (OperationKind.IfStatement) (Syntax: 'if (num > 3 ... }')
        Condition: IBinaryOperatorExpression (BinaryOperationKind.IntegerGreaterThan) (OperationKind.BinaryOperatorExpression, Type: System.Boolean) (Syntax: 'num > 3')
            Left: ILocalReferenceExpression: num (OperationKind.LocalReferenceExpression, Type: System.Int32) (Syntax: 'num')
            Right: ILiteralExpression (Text: 3) (OperationKind.LiteralExpression, Type: System.Int32, Constant: 3) (Syntax: '3')
        IfTrue: IBlockStatement (1 statements) (OperationKind.BlockStatement) (Syntax: '{ ... }')
            IBranchStatement (BranchKind.Continue) (OperationKind.BranchStatement) (Syntax: 'continue;')
      IExpressionStatement (OperationKind.ExpressionStatement) (Syntax: 'System.Cons ... eLine(num);')
        IInvocationExpression (static void System.Console.WriteLine(System.Int32 value)) (OperationKind.InvocationExpression, Type: System.Void) (Syntax: 'System.Cons ... teLine(num)')
          Arguments(1): IArgument (ArgumentKind.Explicit, Matching Parameter: value) (OperationKind.Argument) (Syntax: 'num')
              ILocalReferenceExpression: num (OperationKind.LocalReferenceExpression, Type: System.Int32) (Syntax: 'num')
";
            var expectedDiagnostics = DiagnosticDescription.None;

            VerifyOperationTreeAndDiagnosticsForTest<ForEachStatementSyntax>(source, expectedOperationTree, expectedDiagnostics);
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
      ILocalReferenceExpression: array (OperationKind.LocalReferenceExpression, Type: System.Int32[][]) (Syntax: 'array')
  Body: IBlockStatement (1 statements) (OperationKind.BlockStatement) (Syntax: '{ ... }')
      IForEachLoopStatement (Iteration variable: System.Int32 i) (LoopKind.ForEach) (OperationKind.LoopStatement) (Syntax: 'foreach (in ... }')
        Collection: IConversionExpression (ConversionKind.Cast, Implicit) (OperationKind.ConversionExpression, Type: System.Collections.IEnumerable) (Syntax: 'subArray')
            ILocalReferenceExpression: subArray (OperationKind.LocalReferenceExpression, Type: System.Int32[]) (Syntax: 'subArray')
        Body: IBlockStatement (1 statements) (OperationKind.BlockStatement) (Syntax: '{ ... }')
            IExpressionStatement (OperationKind.ExpressionStatement) (Syntax: 'System.Console.Write(i);')
              IInvocationExpression (static void System.Console.Write(System.Int32 value)) (OperationKind.InvocationExpression, Type: System.Void) (Syntax: 'System.Console.Write(i)')
                Arguments(1): IArgument (ArgumentKind.Explicit, Matching Parameter: value) (OperationKind.Argument) (Syntax: 'i')
                    ILocalReferenceExpression: i (OperationKind.LocalReferenceExpression, Type: System.Int32) (Syntax: 'i')
";
            var expectedDiagnostics = DiagnosticDescription.None;

            VerifyOperationTreeAndDiagnosticsForTest<ForEachStatementSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }

        [Fact, WorkItem(17602, "https://github.com/dotnet/roslyn/issues/17602")]
        public void IForEachLoopStatement_NestedQuery()
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
      IInvocationExpression (static System.Collections.Generic.IEnumerable<System.Int32> System.Linq.Enumerable.Range(System.Int32 start, System.Int32 count)) (OperationKind.InvocationExpression, Type: System.Collections.Generic.IEnumerable<System.Int32>) (Syntax: 'System.Linq ... tLength(0))')
        Arguments(2): IArgument (ArgumentKind.Explicit, Matching Parameter: start) (OperationKind.Argument) (Syntax: '0')
            ILiteralExpression (Text: 0) (OperationKind.LiteralExpression, Type: System.Int32, Constant: 0) (Syntax: '0')
          IArgument (ArgumentKind.Explicit, Matching Parameter: count) (OperationKind.Argument) (Syntax: 'array.GetLength(0)')
            IInvocationExpression ( System.Int32 System.Array.GetLength(System.Int32 dimension)) (OperationKind.InvocationExpression, Type: System.Int32) (Syntax: 'array.GetLength(0)')
              Instance Receiver: ILocalReferenceExpression: array (OperationKind.LocalReferenceExpression, Type: System.Int32[][]) (Syntax: 'array')
              Arguments(1): IArgument (ArgumentKind.Explicit, Matching Parameter: dimension) (OperationKind.Argument) (Syntax: '0')
                  ILiteralExpression (Text: 0) (OperationKind.LiteralExpression, Type: System.Int32, Constant: 0) (Syntax: '0')
  Body: IForEachLoopStatement (Iteration variable: System.Int32 j) (LoopKind.ForEach) (OperationKind.LoopStatement) (Syntax: 'foreach (in ... ray[i][j]);')
      Collection: IConversionExpression (ConversionKind.Cast, Implicit) (OperationKind.ConversionExpression, Type: System.Collections.Generic.IEnumerable<System.Int32>) (Syntax: 'System.Linq ... tLength(1))')
          IInvocationExpression (static System.Collections.Generic.IEnumerable<System.Int32> System.Linq.Enumerable.Range(System.Int32 start, System.Int32 count)) (OperationKind.InvocationExpression, Type: System.Collections.Generic.IEnumerable<System.Int32>) (Syntax: 'System.Linq ... tLength(1))')
            Arguments(2): IArgument (ArgumentKind.Explicit, Matching Parameter: start) (OperationKind.Argument) (Syntax: '0')
                ILiteralExpression (Text: 0) (OperationKind.LiteralExpression, Type: System.Int32, Constant: 0) (Syntax: '0')
              IArgument (ArgumentKind.Explicit, Matching Parameter: count) (OperationKind.Argument) (Syntax: 'array.GetLength(1)')
                IInvocationExpression ( System.Int32 System.Array.GetLength(System.Int32 dimension)) (OperationKind.InvocationExpression, Type: System.Int32) (Syntax: 'array.GetLength(1)')
                  Instance Receiver: ILocalReferenceExpression: array (OperationKind.LocalReferenceExpression, Type: System.Int32[][]) (Syntax: 'array')
                  Arguments(1): IArgument (ArgumentKind.Explicit, Matching Parameter: dimension) (OperationKind.Argument) (Syntax: '1')
                      ILiteralExpression (Text: 1) (OperationKind.LiteralExpression, Type: System.Int32, Constant: 1) (Syntax: '1')
      Body: IExpressionStatement (OperationKind.ExpressionStatement) (Syntax: 'System.Cons ... ray[i][j]);')
          IInvocationExpression (static void System.Console.WriteLine(System.Int32 value)) (OperationKind.InvocationExpression, Type: System.Void) (Syntax: 'System.Cons ... rray[i][j])')
            Arguments(1): IArgument (ArgumentKind.Explicit, Matching Parameter: value) (OperationKind.Argument) (Syntax: 'array[i][j]')
                IArrayElementReferenceExpression (OperationKind.ArrayElementReferenceExpression, Type: System.Int32) (Syntax: 'array[i][j]')
                  Array reference: IArrayElementReferenceExpression (OperationKind.ArrayElementReferenceExpression, Type: System.Int32[]) (Syntax: 'array[i]')
                      Array reference: ILocalReferenceExpression: array (OperationKind.LocalReferenceExpression, Type: System.Int32[][]) (Syntax: 'array')
                      Indices(1): ILocalReferenceExpression: i (OperationKind.LocalReferenceExpression, Type: System.Int32) (Syntax: 'i')
                  Indices(1): ILocalReferenceExpression: j (OperationKind.LocalReferenceExpression, Type: System.Int32) (Syntax: 'j')
";
            var expectedDiagnostics = DiagnosticDescription.None;

            VerifyOperationTreeAndDiagnosticsForTest<ForEachStatementSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }

        [Fact, WorkItem(17602, "https://github.com/dotnet/roslyn/issues/17602")]
        public void IForEachLoopStatement_Evaluator()
        {
            string source = @"
using System.Linq;
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
IForEachLoopStatement (Iteration variable: System.String value) (LoopKind.ForEach) (OperationKind.LoopStatement) (Syntax: 'foreach (st ... }')
  Collection: IConversionExpression (ConversionKind.Cast, Implicit) (OperationKind.ConversionExpression, Type: System.Linq.IOrderedEnumerable<System.String>) (Syntax: 'sorted')
      ILocalReferenceExpression: sorted (OperationKind.LocalReferenceExpression, Type: System.Linq.IOrderedEnumerable<System.String>) (Syntax: 'sorted')
  Body: IBlockStatement (1 statements) (OperationKind.BlockStatement) (Syntax: '{ ... }')
      IExpressionStatement (OperationKind.ExpressionStatement) (Syntax: 'System.Cons ... ine(value);')
        IInvocationExpression (static void System.Console.WriteLine(System.String value)) (OperationKind.InvocationExpression, Type: System.Void) (Syntax: 'System.Cons ... Line(value)')
          Arguments(1): IArgument (ArgumentKind.Explicit, Matching Parameter: value) (OperationKind.Argument) (Syntax: 'value')
              ILocalReferenceExpression: value (OperationKind.LocalReferenceExpression, Type: System.String) (Syntax: 'value')
";
            var expectedDiagnostics = DiagnosticDescription.None;

            VerifyOperationTreeAndDiagnosticsForTest<ForEachStatementSyntax>(source, expectedOperationTree, expectedDiagnostics);
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
      IInvocationExpression ( System.Reflection.FieldInfo[] System.Type.GetFields()) (OperationKind.InvocationExpression, Type: System.Reflection.FieldInfo[]) (Syntax: 'employee.Ge ... GetFields()')
        Instance Receiver: IInvocationExpression ( System.Type System.Object.GetType()) (OperationKind.InvocationExpression, Type: System.Type) (Syntax: 'employee.GetType()')
            Instance Receiver: ILocalReferenceExpression: employee (OperationKind.LocalReferenceExpression, Type: DisplayStructContentsTest.Program.Employee) (Syntax: 'employee')
  Body: IBlockStatement (1 statements) (OperationKind.BlockStatement) (Syntax: '{ ... }')
      IExpressionStatement (OperationKind.ExpressionStatement) (Syntax: 'System.Cons ... mployee)));')
        IInvocationExpression (static void System.Console.WriteLine(System.String value)) (OperationKind.InvocationExpression, Type: System.Void) (Syntax: 'System.Cons ... employee)))')
          Arguments(1): IArgument (ArgumentKind.Explicit, Matching Parameter: value) (OperationKind.Argument) (Syntax: 'fi.Name + "" ... (employee))')
              IBinaryOperatorExpression (BinaryOperationKind.StringConcatenate) (OperationKind.BinaryOperatorExpression, Type: System.String) (Syntax: 'fi.Name + "" ... (employee))')
                Left: IBinaryOperatorExpression (BinaryOperationKind.StringConcatenate) (OperationKind.BinaryOperatorExpression, Type: System.String) (Syntax: 'fi.Name + "" = ""')
                    Left: IPropertyReferenceExpression: System.String System.Reflection.MemberInfo.Name { get; } (OperationKind.PropertyReferenceExpression, Type: System.String) (Syntax: 'fi.Name')
                        Instance Receiver: ILocalReferenceExpression: fi (OperationKind.LocalReferenceExpression, Type: System.Reflection.FieldInfo) (Syntax: 'fi')
                    Right: ILiteralExpression (OperationKind.LiteralExpression, Type: System.String, Constant: "" = "") (Syntax: '"" = ""')
                Right: IInvocationExpression (static System.String System.Convert.ToString(System.Object value)) (OperationKind.InvocationExpression, Type: System.String) (Syntax: 'System.Conv ... (employee))')
                    Arguments(1): IArgument (ArgumentKind.Explicit, Matching Parameter: value) (OperationKind.Argument) (Syntax: 'fi.GetValue(employee)')
                        IInvocationExpression (virtual System.Object System.Reflection.FieldInfo.GetValue(System.Object obj)) (OperationKind.InvocationExpression, Type: System.Object) (Syntax: 'fi.GetValue(employee)')
                          Instance Receiver: ILocalReferenceExpression: fi (OperationKind.LocalReferenceExpression, Type: System.Reflection.FieldInfo) (Syntax: 'fi')
                          Arguments(1): IArgument (ArgumentKind.Explicit, Matching Parameter: obj) (OperationKind.Argument) (Syntax: 'employee')
                              IConversionExpression (ConversionKind.Cast, Implicit) (OperationKind.ConversionExpression, Type: System.Object) (Syntax: 'employee')
                                ILocalReferenceExpression: employee (OperationKind.LocalReferenceExpression, Type: DisplayStructContentsTest.Program.Employee) (Syntax: 'employee')
";
            var expectedDiagnostics = DiagnosticDescription.None;

            VerifyOperationTreeAndDiagnosticsForTest<ForEachStatementSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }

        [Fact, WorkItem(17602, "https://github.com/dotnet/roslyn/issues/17602")]
        public void IForEachLoopStatement_ConstantEmpty()
        {
            string source = @"
   class Program
   {
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
    }

";
            string expectedOperationTree = @"
IForEachLoopStatement (Iteration variable: System.Char c) (LoopKind.ForEach) (OperationKind.LoopStatement) (Syntax: 'foreach (ch ... }')
  Collection: IConversionExpression (ConversionKind.Cast, Implicit) (OperationKind.ConversionExpression, Type: System.String) (Syntax: 's')
      ILocalReferenceExpression: s (OperationKind.LocalReferenceExpression, Type: System.String, Constant: """") (Syntax: 's')
  Body: IBlockStatement (1 statements) (OperationKind.BlockStatement) (Syntax: '{ ... }')
      IExpressionStatement (OperationKind.ExpressionStatement) (Syntax: 'System.Cons ... iteLine(c);')
        IInvocationExpression (static void System.Console.WriteLine(System.Char value)) (OperationKind.InvocationExpression, Type: System.Void) (Syntax: 'System.Cons ... riteLine(c)')
          Arguments(1): IArgument (ArgumentKind.Explicit, Matching Parameter: value) (OperationKind.Argument) (Syntax: 'c')
              ILocalReferenceExpression: c (OperationKind.LocalReferenceExpression, Type: System.Char) (Syntax: 'c')
";
            var expectedDiagnostics = DiagnosticDescription.None;

            VerifyOperationTreeAndDiagnosticsForTest<ForEachStatementSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }

        [Fact, WorkItem(17602, "https://github.com/dotnet/roslyn/issues/17602")]
        public void IForEachLoopStatement_Var()
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
            System.Console.WriteLine(""{ 0},{ 1}"", pair.Key, pair.Value);
        }/*</bind>*/
    }
}
";
            string expectedOperationTree = @"
IForEachLoopStatement (Iteration variable: System.Collections.Generic.KeyValuePair<System.Int32, System.Int32> pair) (LoopKind.ForEach) (OperationKind.LoopStatement) (Syntax: 'foreach (va ... }')
  Collection: IConversionExpression (ConversionKind.Cast, Implicit) (OperationKind.ConversionExpression, Type: System.Collections.Generic.Dictionary<System.Int32, System.Int32>) (Syntax: '_f')
      IFieldReferenceExpression: System.Collections.Generic.Dictionary<System.Int32, System.Int32> Program._f (Static) (OperationKind.FieldReferenceExpression, Type: System.Collections.Generic.Dictionary<System.Int32, System.Int32>) (Syntax: '_f')
  Body: IBlockStatement (1 statements) (OperationKind.BlockStatement) (Syntax: '{ ... }')
      IExpressionStatement (OperationKind.ExpressionStatement) (Syntax: 'System.Cons ... air.Value);')
        IInvocationExpression (static void System.Console.WriteLine(System.String format, System.Object arg0, System.Object arg1)) (OperationKind.InvocationExpression, Type: System.Void) (Syntax: 'System.Cons ... pair.Value)')
          Arguments(3): IArgument (ArgumentKind.Explicit, Matching Parameter: format) (OperationKind.Argument) (Syntax: '""{ 0},{ 1}""')
              ILiteralExpression (OperationKind.LiteralExpression, Type: System.String, Constant: ""{ 0},{ 1}"") (Syntax: '""{ 0},{ 1}""')
            IArgument (ArgumentKind.Explicit, Matching Parameter: arg0) (OperationKind.Argument) (Syntax: 'pair.Key')
              IConversionExpression (ConversionKind.Cast, Implicit) (OperationKind.ConversionExpression, Type: System.Object) (Syntax: 'pair.Key')
                IPropertyReferenceExpression: System.Int32 System.Collections.Generic.KeyValuePair<System.Int32, System.Int32>.Key { get; } (OperationKind.PropertyReferenceExpression, Type: System.Int32) (Syntax: 'pair.Key')
                  Instance Receiver: ILocalReferenceExpression: pair (OperationKind.LocalReferenceExpression, Type: System.Collections.Generic.KeyValuePair<System.Int32, System.Int32>) (Syntax: 'pair')
            IArgument (ArgumentKind.Explicit, Matching Parameter: arg1) (OperationKind.Argument) (Syntax: 'pair.Value')
              IConversionExpression (ConversionKind.Cast, Implicit) (OperationKind.ConversionExpression, Type: System.Object) (Syntax: 'pair.Value')
                IPropertyReferenceExpression: System.Int32 System.Collections.Generic.KeyValuePair<System.Int32, System.Int32>.Value { get; } (OperationKind.PropertyReferenceExpression, Type: System.Int32) (Syntax: 'pair.Value')
                  Instance Receiver: ILocalReferenceExpression: pair (OperationKind.LocalReferenceExpression, Type: System.Collections.Generic.KeyValuePair<System.Int32, System.Int32>) (Syntax: 'pair')
";
            var expectedDiagnostics = DiagnosticDescription.None;

            VerifyOperationTreeAndDiagnosticsForTest<ForEachStatementSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }

        [Fact, WorkItem(17602, "https://github.com/dotnet/roslyn/issues/17602")]
        public void IForEachLoopStatement_OutVar()
        {
            string source = @"
   class P
{
    private void M()
    {
        var s = """";
        /*<bind>*/foreach (var j in new[] { int.TryParse(s, out var i) ? i : 0 })
        {
            System.Console.WriteLine($""i ={ i}, s ={s}"");
        }/*</bind>*/
    }
}
";
            string expectedOperationTree = @"
IForEachLoopStatement (Iteration variable: System.Int32 j) (LoopKind.ForEach) (OperationKind.LoopStatement) (Syntax: 'foreach (va ... }')
  Collection: IConversionExpression (ConversionKind.Cast, Implicit) (OperationKind.ConversionExpression, Type: System.Collections.IEnumerable) (Syntax: 'new[] { int ... ) ? i : 0 }')
      IArrayCreationExpression (Element Type: System.Int32) (OperationKind.ArrayCreationExpression, Type: System.Int32[]) (Syntax: 'new[] { int ... ) ? i : 0 }')
        Dimension Sizes(1): ILiteralExpression (OperationKind.LiteralExpression, Type: System.Int32, Constant: 1) (Syntax: 'new[] { int ... ) ? i : 0 }')
        Initializer: IArrayInitializer (1 elements) (OperationKind.ArrayInitializer) (Syntax: '{ int.TryPa ... ) ? i : 0 }')
            Element Values(1): IConditionalChoiceExpression (OperationKind.ConditionalChoiceExpression, Type: System.Int32) (Syntax: 'int.TryPars ...  i) ? i : 0')
                Condition: IInvocationExpression (static System.Boolean System.Int32.TryParse(System.String s, out System.Int32 result)) (OperationKind.InvocationExpression, Type: System.Boolean) (Syntax: 'int.TryPars ...  out var i)')
                    Arguments(2): IArgument (ArgumentKind.Explicit, Matching Parameter: s) (OperationKind.Argument) (Syntax: 's')
                        ILocalReferenceExpression: s (OperationKind.LocalReferenceExpression, Type: System.String) (Syntax: 's')
                      IArgument (ArgumentKind.Explicit, Matching Parameter: result) (OperationKind.Argument) (Syntax: 'var i')
                        ILocalReferenceExpression: i (OperationKind.LocalReferenceExpression, Type: System.Int32) (Syntax: 'var i')
                IfTrue: ILocalReferenceExpression: i (OperationKind.LocalReferenceExpression, Type: System.Int32) (Syntax: 'i')
                IfFalse: ILiteralExpression (Text: 0) (OperationKind.LiteralExpression, Type: System.Int32, Constant: 0) (Syntax: '0')
  Body: IBlockStatement (1 statements) (OperationKind.BlockStatement) (Syntax: '{ ... }')
      IExpressionStatement (OperationKind.ExpressionStatement) (Syntax: 'System.Cons ... , s ={s}"");')
        IInvocationExpression (static void System.Console.WriteLine(System.String value)) (OperationKind.InvocationExpression, Type: System.Void) (Syntax: 'System.Cons ... }, s ={s}"")')
          Arguments(1): IArgument (ArgumentKind.Explicit, Matching Parameter: value) (OperationKind.Argument) (Syntax: '$""i ={ i}, s ={s}""')
              IOperation:  (OperationKind.None) (Syntax: '$""i ={ i}, s ={s}""')
";
            var expectedDiagnostics = DiagnosticDescription.None;

            VerifyOperationTreeAndDiagnosticsForTest<ForEachStatementSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }

        [Fact, WorkItem(17602, "https://github.com/dotnet/roslyn/issues/17602")]
        public void IForEachLoopStatement_TypeNameNotFound()
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
  Body: IBlockStatement (1 statements, 1 locals) (OperationKind.BlockStatement, IsInvalid) (Syntax: '{ ... }')
      Locals: Local_1: System.Boolean b
      IVariableDeclarationStatement (1 declarations) (OperationKind.VariableDeclarationStatement, IsInvalid) (Syntax: 'bool b = !x ... uals(null);')
        IVariableDeclaration (1 variables) (OperationKind.VariableDeclaration, IsInvalid) (Syntax: 'bool b = !x ... uals(null);')
          Variables: Local_1: System.Boolean b
          Initializer: IConversionExpression (ConversionKind.Cast, Implicit) (OperationKind.ConversionExpression, Type: System.Boolean, IsInvalid) (Syntax: '!x.Equals(null)')
              IUnaryOperatorExpression (UnaryOperationKind.Invalid) (OperationKind.UnaryOperatorExpression, Type: System.Object, IsInvalid) (Syntax: '!x.Equals(null)')
                IInvocationExpression ( ? C.()) (OperationKind.InvocationExpression, Type: ?, IsInvalid) (Syntax: 'x.Equals(null)')
                  Instance Receiver: IOperation:  (OperationKind.None) (Syntax: 'x.Equals')
";
            var expectedDiagnostics = new DiagnosticDescription[] {
                // CS0246: The type or namespace name 'MissingType' could not be found (are you missing a using directive or an assembly reference?)
                //         /*<bind>*/foreach (MissingType x in sequence)
                Diagnostic(ErrorCode.ERR_SingleTypeNameNotFound, "MissingType").WithArguments("MissingType").WithLocation(7, 28)
            };

            VerifyOperationTreeAndDiagnosticsForTest<ForEachStatementSyntax>(source, expectedOperationTree, expectedDiagnostics);
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
  Collection: ILiteralExpression (Text: null) (OperationKind.LiteralExpression, Type: null, Constant: null) (Syntax: 'null')
  Body: IBlockStatement (0 statements) (OperationKind.BlockStatement) (Syntax: '{ ... }')
";
            var expectedDiagnostics = new DiagnosticDescription[] {
                // CS0186: Use of null is not valid in this context
                //         /*<bind>*/foreach (int x in null)
                Diagnostic(ErrorCode.ERR_NullNotValid, "null").WithLocation(6, 37)
            };

            VerifyOperationTreeAndDiagnosticsForTest<ForEachStatementSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }

        [Fact, WorkItem(17602, "https://github.com/dotnet/roslyn/issues/17602")]
        public void IForEachLoopStatement_NoExplicitConvertion()
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
      IParameterReferenceExpression: args (OperationKind.ParameterReferenceExpression, Type: System.String[]) (Syntax: 'args')
  Body: IBlockStatement (0 statements) (OperationKind.BlockStatement) (Syntax: '{ ... }')
";
            var expectedDiagnostics = new DiagnosticDescription[] {
                // CS0030: Cannot convert type 'string' to 'int'
                //         /*<bind>*/foreach (int x in args)
                Diagnostic(ErrorCode.ERR_NoExplicitConv, "foreach").WithArguments("string", "int").WithLocation(6, 19)
            };

            VerifyOperationTreeAndDiagnosticsForTest<ForEachStatementSyntax>(source, expectedOperationTree, expectedDiagnostics);
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
      IParameterReferenceExpression: a (OperationKind.ParameterReferenceExpression, Type: System.Int32[]) (Syntax: 'a')
  Body: IBlockStatement (1 statements) (OperationKind.BlockStatement, IsInvalid) (Syntax: '{ x++; }')
      IExpressionStatement (OperationKind.ExpressionStatement, IsInvalid) (Syntax: 'x++;')
        IIncrementExpression (UnaryOperandKind.Invalid) (BinaryOperationKind.Invalid) (OperationKind.IncrementExpression, Type: System.Object, IsInvalid) (Syntax: 'x++')
          Left: IInvalidExpression (OperationKind.InvalidExpression, Type: System.Int32, IsInvalid) (Syntax: 'x')
              Children(1): ILocalReferenceExpression: x (OperationKind.LocalReferenceExpression, Type: System.Int32) (Syntax: 'x')
          Right: ILiteralExpression (OperationKind.LiteralExpression, Type: System.Object, Constant: 1) (Syntax: 'x++')
";
            var expectedDiagnostics = new DiagnosticDescription[] {
                // CS1656: Cannot assign to 'x' because it is a 'foreach iteration variable'
                //        /*<bind>*/foreach (int x in a) { x++; }/*</bind>*/
                Diagnostic(ErrorCode.ERR_AssgReadonlyLocalCause, "x").WithArguments("x", "foreach iteration variable").WithLocation(6, 41)
            };

            VerifyOperationTreeAndDiagnosticsForTest<ForEachStatementSyntax>(source, expectedOperationTree, expectedDiagnostics);
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
      IParameterReferenceExpression: e (OperationKind.ParameterReferenceExpression, Type: Enumerable) (Syntax: 'e')
  Body: IBlockStatement (0 statements) (OperationKind.BlockStatement) (Syntax: '{ }')
";
            var expectedDiagnostics = DiagnosticDescription.None;

            VerifyOperationTreeAndDiagnosticsForTest<ForEachStatementSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }

        [Fact, WorkItem(17602, "https://github.com/dotnet/roslyn/issues/17602")]
        public void IForEachLoopStatement_Dynamic()
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
      IParameterReferenceExpression: d (OperationKind.ParameterReferenceExpression, Type: dynamic) (Syntax: 'd')
  Body: IBlockStatement (0 statements) (OperationKind.BlockStatement) (Syntax: '{ }')
";
            var expectedDiagnostics = DiagnosticDescription.None;

            VerifyOperationTreeAndDiagnosticsForTest<ForEachStatementSyntax>(source, expectedOperationTree, expectedDiagnostics);
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

class Enumerable<T>
{
    public T GetEnumerator() { return default(T); }
}
";
            string expectedOperationTree = @"
IForEachLoopStatement (Iteration variable: System.Object x) (LoopKind.ForEach) (OperationKind.LoopStatement) (Syntax: 'foreach (ob ... }')
  Collection: IConversionExpression (ConversionKind.Cast, Implicit) (OperationKind.ConversionExpression, Type: Enumerable<T>) (Syntax: 'new Enumerable<T>()')
      IObjectCreationExpression (Constructor: Enumerable<T>..ctor()) (OperationKind.ObjectCreationExpression, Type: Enumerable<T>) (Syntax: 'new Enumerable<T>()')
  Body: IBlockStatement (1 statements) (OperationKind.BlockStatement) (Syntax: '{ ... }')
      IExpressionStatement (OperationKind.ExpressionStatement) (Syntax: 'System.Cons ... iteLine(x);')
        IInvocationExpression (static void System.Console.WriteLine(System.Object value)) (OperationKind.InvocationExpression, Type: System.Void) (Syntax: 'System.Cons ... riteLine(x)')
          Arguments(1): IArgument (ArgumentKind.Explicit, Matching Parameter: value) (OperationKind.Argument) (Syntax: 'x')
              ILocalReferenceExpression: x (OperationKind.LocalReferenceExpression, Type: System.Object) (Syntax: 'x')
";
            var expectedDiagnostics = DiagnosticDescription.None;

            VerifyOperationTreeAndDiagnosticsForTest<ForEachStatementSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }

        [Fact, WorkItem(17602, "https://github.com/dotnet/roslyn/issues/17602")]
        public void IForEachLoopStatement_Throw()
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
      ILocalReferenceExpression: numbers (OperationKind.LocalReferenceExpression, Type: System.Int32[]) (Syntax: 'numbers')
  Body: IBlockStatement (2 statements) (OperationKind.BlockStatement) (Syntax: '{ ... }')
      IIfStatement (OperationKind.IfStatement) (Syntax: 'if (num > 3 ... }')
        Condition: IBinaryOperatorExpression (BinaryOperationKind.IntegerGreaterThan) (OperationKind.BinaryOperatorExpression, Type: System.Boolean) (Syntax: 'num > 3')
            Left: ILocalReferenceExpression: num (OperationKind.LocalReferenceExpression, Type: System.Int32) (Syntax: 'num')
            Right: ILiteralExpression (Text: 3) (OperationKind.LiteralExpression, Type: System.Int32, Constant: 3) (Syntax: '3')
        IfTrue: IBlockStatement (1 statements) (OperationKind.BlockStatement) (Syntax: '{ ... }')
            IThrowStatement (OperationKind.ThrowStatement) (Syntax: 'throw new S ... ""testing"");')
              IObjectCreationExpression (Constructor: System.Exception..ctor(System.String message)) (OperationKind.ObjectCreationExpression, Type: System.Exception) (Syntax: 'new System. ... (""testing"")')
                Arguments(1): IArgument (ArgumentKind.Explicit, Matching Parameter: message) (OperationKind.Argument) (Syntax: '""testing""')
                    ILiteralExpression (OperationKind.LiteralExpression, Type: System.String, Constant: ""testing"") (Syntax: '""testing""')
      IExpressionStatement (OperationKind.ExpressionStatement) (Syntax: 'System.Cons ... eLine(num);')
        IInvocationExpression (static void System.Console.WriteLine(System.Int32 value)) (OperationKind.InvocationExpression, Type: System.Void) (Syntax: 'System.Cons ... teLine(num)')
          Arguments(1): IArgument (ArgumentKind.Explicit, Matching Parameter: value) (OperationKind.Argument) (Syntax: 'num')
              ILocalReferenceExpression: num (OperationKind.LocalReferenceExpression, Type: System.Int32) (Syntax: 'num')
";
            var expectedDiagnostics = DiagnosticDescription.None;

            VerifyOperationTreeAndDiagnosticsForTest<ForEachStatementSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }

        [Fact, WorkItem(17602, "https://github.com/dotnet/roslyn/issues/17602")]
        public void IForEachLoopStatement_EmptyStatement()
        {
            string source = @"
class Program
{
    static void Main()
    {
        string[] pets = { ""dog"", ""cat"", ""bird"" };
        /*<bind>*/foreach (string value in pets)
        {
            
        }/*</bind>*/
    }
}
";
            string expectedOperationTree = @"
IForEachLoopStatement (Iteration variable: System.String value) (LoopKind.ForEach) (OperationKind.LoopStatement) (Syntax: 'foreach (st ... }')
  Collection: IConversionExpression (ConversionKind.Cast, Implicit) (OperationKind.ConversionExpression, Type: System.Collections.IEnumerable) (Syntax: 'pets')
      ILocalReferenceExpression: pets (OperationKind.LocalReferenceExpression, Type: System.String[]) (Syntax: 'pets')
  Body: IBlockStatement (0 statements) (OperationKind.BlockStatement) (Syntax: '{ ... }')
";
            var expectedDiagnostics = DiagnosticDescription.None;

            VerifyOperationTreeAndDiagnosticsForTest<ForEachStatementSyntax>(source, expectedOperationTree, expectedDiagnostics);
        }

    }
}
