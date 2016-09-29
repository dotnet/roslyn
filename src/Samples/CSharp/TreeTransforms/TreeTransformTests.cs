// *********************************************************
//
// Copyright © Microsoft Corporation
//
// Licensed under the Apache License, Version 2.0 (the
// "License"); you may not use this file except in
// compliance with the License. You may obtain a copy of
// the License at
//
// http://www.apache.org/licenses/LICENSE-2.0 
//
// THIS CODE IS PROVIDED ON AN *AS IS* BASIS, WITHOUT WARRANTIES
// OR CONDITIONS OF ANY KIND, EITHER EXPRESS OR IMPLIED,
// INCLUDING WITHOUT LIMITATION ANY IMPLIED WARRANTIES
// OR CONDITIONS OF TITLE, FITNESS FOR A PARTICULAR
// PURPOSE, MERCHANTABILITY OR NON-INFRINGEMENT.
//
// See the Apache 2 License for the specific language
// governing permissions and limitations under the License.
//
// *********************************************************

namespace TreeTransformsCS
{
    public static class TreeTransformTests
    {
        public static bool LambdaToAnonMethodTest()
        {
            string input = @"
public class Test
{
    public static void Main(string[] args)
    {
        Func<int, int, int> f1 = (int x, int y) => { return x + y; };
    }
}";

            string expected_transform = @"
public class Test
{
    public static void Main(string[] args)
    {
        Func<int, int, int> f1 = delegate(int x, int y) { return x + y; };
    }
}";

            string actual_transform = Transforms.Transform(input, TransformKind.LambdaToAnonMethod);

            return expected_transform == actual_transform;
        }

        public static bool AnonMethodToLambdaTest()
        {
            string input = @"
public class Test
{
    public static void Main(string[] args)
    {
        Func<int, int, int> f1 = delegate(int x, int y) { return x + y; };
    }
}";

            string expected_transform = @"
public class Test
{
    public static void Main(string[] args)
    {
        Func<int, int, int> f1 = (int x, int y) =>{ return x + y; };
    }
}";
            string actual_transform = Transforms.Transform(input, TransformKind.AnonMethodToLambda);

            return expected_transform == actual_transform;
        }

        public static bool DoToWhileTest()
        {
            string input = @"
class Program
{
    static void Main()
    {
        int i = 0;
        int sum = 0;

        do
        {
            sum += i;
            i++;
        } while (i < 10);

        System.Console.WriteLine(sum);
    }
}
";

            string expected_transform = @"
class Program
{
    static void Main()
    {
        int i = 0;
        int sum = 0;

        while (i < 10)
        {
            sum += i;
            i++;
        } 

        System.Console.WriteLine(sum);
    }
}
";
            string actual_transform = Transforms.Transform(input, TransformKind.DoToWhile);

            return expected_transform == actual_transform;
        }

        public static bool WhileToDoTest()
        {
            string input = @"
class Program
{
    static void Main()
    {
        int i = 0;
        int sum = 0;

        while (i < 10)
        {
            sum += i;
            i++;
        }

        System.Console.WriteLine(sum);
    }
}
";

            string expected_transform = @"
class Program
{
    static void Main()
    {
        int i = 0;
        int sum = 0;

        do
        {
            sum += i;
            i++;
        }while (i < 10);

        System.Console.WriteLine(sum);
    }
}
";
            string actual_transform = Transforms.Transform(input, TransformKind.WhileToDo);

            return expected_transform == actual_transform;
        }

        public static bool CheckedStmtToUncheckedStmtTest()
        {
            string input = @"
class Program
{
    static void Main()
    {
        checked
        {
            int x = int.MaxValue;
            x = x + 1;
        }
    }
}

";

            string expected_transform = @"
class Program
{
    static void Main()
    {
        unchecked
        {
            int x = int.MaxValue;
            x = x + 1;
        }
    }
}

";
            string actual_transform = Transforms.Transform(input, TransformKind.CheckedStmtToUncheckedStmt);

            return expected_transform == actual_transform;
        }

        public static bool UncheckedStmtToCheckedStmt()
        {
            string input = @"
class Program
{
    static void Main()
    {
        unchecked
        {
            int x = int.MaxValue;
            x = x + 1;
        }
    }
}

";

            string expected_transform = @"
class Program
{
    static void Main()
    {
        checked
        {
            int x = int.MaxValue;
            x = x + 1;
        }
    }
}

";
            string actual_transform = Transforms.Transform(input, TransformKind.UncheckedStmtToCheckedStmt);

            return expected_transform == actual_transform;
        }

        public static bool CheckedExprToUncheckedExprTest()
        {
            string input = @"
class Program
{
    static void Main()
    {
        int x = int.MaxValue;
        x = checked(x + 1);
    }
}
";

            string expected_transform = @"
class Program
{
    static void Main()
    {
        int x = int.MaxValue;
        x = unchecked(x + 1);
    }
}
";
            string actual_transform = Transforms.Transform(input, TransformKind.CheckedExprToUncheckedExpr);

            return expected_transform == actual_transform;
        }

        public static bool UncheckedExprToCheckedExprTest()
        {
            string input = @"
class Program
{
    static void Main()
    {
        int x = int.MaxValue;
        x = unchecked(x + 1);
    }
}
";

            string expected_transform = @"
class Program
{
    static void Main()
    {
        int x = int.MaxValue;
        x = checked(x + 1);
    }
}
";
            string actual_transform = Transforms.Transform(input, TransformKind.UncheckedExprToCheckedExpr);

            return expected_transform == actual_transform;
        }

        public static bool PostfixToPrefixTest()
        {
            string input = @"
class Program
{
    static void Main()
    {
        int x = 10;
        /*START*/ x++ /*END*/;
        x--;
    }
}
";

            string expected_transform = @"
class Program
{
    static void Main()
    {
        int x = 10;
        /*START*/ ++x /*END*/;
        --x;
    }
}
";
            string actual_transform = Transforms.Transform(input, TransformKind.PostfixToPrefix);

            return expected_transform == actual_transform;
        }

        public static bool PrefixToPostfixTest()
        {
            string input = @"
class Program
{
    static void Main()
    {
        int x = 10;
        /*START*/ ++x /*END*/;
        --x;
    }
}
";

            string expected_transform = @"
class Program
{
    static void Main()
    {
        int x = 10;
        /*START*/ x++ /*END*/;
        x--;
    }
}
";
            string actual_transform = Transforms.Transform(input, TransformKind.PrefixToPostfix);

            return expected_transform == actual_transform;
        }

        public static bool TrueToFalseTest()
        {
            string input = @"
class Program
{
    static void Main()
    {
        bool b1 = true;

        if (true)
        {
        }
    }
}
";

            string expected_transform = @"
class Program
{
    static void Main()
    {
        bool b1 = false;

        if (false)
        {
        }
    }
}
";
            string actual_transform = Transforms.Transform(input, TransformKind.TrueToFalse);

            return expected_transform == actual_transform;
        }

        public static bool FalseToTrueTest()
        {
            string input = @"
class Program
{
    static void Main()
    {
        bool b1 = false;

        if (false)
        {
        }
    }
}
";

            string expected_transform = @"
class Program
{
    static void Main()
    {
        bool b1 = true;

        if (true)
        {
        }
    }
}
";
            string actual_transform = Transforms.Transform(input, TransformKind.FalseToTrue);

            return expected_transform == actual_transform;
        }

        public static bool AddAssignToAssignTest()
        {
            string input = @"
class Program
{
    static void Main()
    {
        int x = 10;
        int y = 45;

        x += y;
    }
}
";

            string expected_transform = @"
class Program
{
    static void Main()
    {
        int x = 10;
        int y = 45;

        x = x + y;
    }
}
";
            string actual_transform = Transforms.Transform(input, TransformKind.AddAssignToAssign);

            return expected_transform == actual_transform;
        }

        public static bool RefParamToOutParamTest()
        {
            string input = @"
class Program
{
    static void Method1(ref int i1, out int i2, int i3)
    {
        i2 = 45;
    }

    static void Main()
    {
        int x = 4, y = 5, z = 6;
        Method1(ref x, out y, z);
    }
}

";

            string expected_transform = @"
class Program
{
    static void Method1(out int i1, out int i2, int i3)
    {
        i2 = 45;
    }

    static void Main()
    {
        int x = 4, y = 5, z = 6;
        Method1(ref x, out y, z);
    }
}

";
            string actual_transform = Transforms.Transform(input, TransformKind.RefParamToOutParam);

            return expected_transform == actual_transform;
        }

        public static bool OutParamToRefParamTest()
        {
            string input = @"
class Program
{
    static void Method1(ref int i1, out int i2, int i3)
    {
        i2 = 45;
    }

    static void Main()
    {
        int x = 4, y = 5, z = 6;
        Method1(ref x, out y, z);
    }
}

";

            string expected_transform = @"
class Program
{
    static void Method1(ref int i1, ref int i2, int i3)
    {
        i2 = 45;
    }

    static void Main()
    {
        int x = 4, y = 5, z = 6;
        Method1(ref x, out y, z);
    }
}

";
            string actual_transform = Transforms.Transform(input, TransformKind.OutParamToRefParam);

            return expected_transform == actual_transform;
        }

        public static bool RefArgToOutArgTest()
        {
            string input = @"
class Program
{
    static void Method1(ref int i1, out int i2, int i3)
    {
        i2 = 45;
    }

    static void Main()
    {
        int x = 4, y = 5, z = 6;
        Method1(ref x, out y, z);
    }
}

";

            string expected_transform = @"
class Program
{
    static void Method1(ref int i1, out int i2, int i3)
    {
        i2 = 45;
    }

    static void Main()
    {
        int x = 4, y = 5, z = 6;
        Method1(out x, out y, z);
    }
}

";
            string actual_transform = Transforms.Transform(input, TransformKind.RefArgToOutArg);

            return expected_transform == actual_transform;
        }

        public static bool OutArgToRefArgTest()
        {
            string input = @"
class Program
{
    static void Method1(ref int i1, out int i2, int i3)
    {
        i2 = 45;
    }

    static void Main()
    {
        int x = 4, y = 5, z = 6;
        Method1(ref x, out y, z);
    }
}

";

            string expected_transform = @"
class Program
{
    static void Method1(ref int i1, out int i2, int i3)
    {
        i2 = 45;
    }

    static void Main()
    {
        int x = 4, y = 5, z = 6;
        Method1(ref x, ref y, z);
    }
}

";
            string actual_transform = Transforms.Transform(input, TransformKind.OutArgToRefArg);

            return expected_transform == actual_transform;
        }

        public static bool OrderByAscToOrderByDescTest()
        {
            string input = @"
using System;
using System.Linq;

class Program
{
    static void Main()
    {
        int[] numbers = { 3, 1, 4, 6, 10 };

        var sortedNumbers = from number in numbers orderby number ascending select number;

        foreach (var num in sortedNumbers)
            Console.WriteLine(num);
    }
}
";

            string expected_transform = @"
using System;
using System.Linq;

class Program
{
    static void Main()
    {
        int[] numbers = { 3, 1, 4, 6, 10 };

        var sortedNumbers = from number in numbers orderby number descending select number;

        foreach (var num in sortedNumbers)
            Console.WriteLine(num);
    }
}
";
            string actual_transform = Transforms.Transform(input, TransformKind.OrderByAscToOrderByDesc);

            return expected_transform == actual_transform;
        }

        public static bool OrderByDescToOrderByAscTest()
        {
            string input = @"
using System;
using System.Linq;

class Program
{
    static void Main()
    {
        int[] numbers = { 3, 1, 4, 6, 10 };

        var sortedNumbers = from number in numbers orderby number descending select number;

        foreach (var num in sortedNumbers)
            Console.WriteLine(num);
    }
}
";

            string expected_transform = @"
using System;
using System.Linq;

class Program
{
    static void Main()
    {
        int[] numbers = { 3, 1, 4, 6, 10 };

        var sortedNumbers = from number in numbers orderby number ascending select number;

        foreach (var num in sortedNumbers)
            Console.WriteLine(num);
    }
}
";
            string actual_transform = Transforms.Transform(input, TransformKind.OrderByDescToOrderByAsc);

            return expected_transform == actual_transform;
        }

        public static bool DefaultInitAllVarsTest()
        {
            string input = @"
class Program
{
    static void Main()
    {
        int i, j;
        Program f1;
    }
}
";

            string expected_transform = @"
class Program
{
    static void Main()
    {
        int i = default(int), j = default(int);
        Program f1 = default(Program);
    }
}
";
            string actual_transform = Transforms.Transform(input, TransformKind.DefaultInitAllVars);

            return expected_transform == actual_transform;
        }

        public static bool ClassDeclToStructDeclTest()
        {
            string input = @"
class Program
{
    static void Main()
    {
    }
}
";

            string expected_transform = @"
struct Program
{
    static void Main()
    {
    }
}
";
            string actual_transform = Transforms.Transform(input, TransformKind.ClassDeclToStructDecl);

            return expected_transform == actual_transform;
        }

        public static bool StructDeclToClassDeclTest()
        {
            string input = @"
struct Program
{
    static void Main()
    {
    }
}
";

            string expected_transform = @"
class Program
{
    static void Main()
    {
    }
}
";
            string actual_transform = Transforms.Transform(input, TransformKind.StructDeclToClassDecl);

            return expected_transform == actual_transform;
        }

        public static bool IntTypeToLongTypeTest()
        {
            string input = @"
using System.Collections.Generic;

class Program
{    
    static void Main()
    {
        int i;
        List<int> l1 = new List<int>();
    }
}
";

            string expected_transform = @"
using System.Collections.Generic;

class Program
{    
    static void Main()
    {
        long i;
        List<long> l1 = new List<long>();
    }
}
";
            string actual_transform = Transforms.Transform(input, TransformKind.IntTypeToLongType);

            return expected_transform == actual_transform;
        }
    }
}
