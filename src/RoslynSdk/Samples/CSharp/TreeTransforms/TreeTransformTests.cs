using Xunit;

namespace TreeTransforms
{
    public static class TreeTransformTests
    {
        [Fact]
        public static void LambdaToAnonMethodTest()
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

            Assert.Equal(expected_transform, actual_transform);
        }

        [Fact]
        public static void AnonMethodToLambdaTest()
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

            Assert.Equal(expected_transform, actual_transform);
        }

        [Fact]
        public static void DoToWhileTest()
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

            Assert.Equal(expected_transform, actual_transform);
        }

        [Fact]
        public static void WhileToDoTest()
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

            Assert.Equal(expected_transform, actual_transform);
        }

        [Fact]
        public static void CheckedStmtToUncheckedStmtTest()
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

            Assert.Equal(expected_transform, actual_transform);
        }

        [Fact]
        public static void UncheckedStmtToCheckedStmt()
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

            Assert.Equal(expected_transform, actual_transform);
        }

        [Fact]
        public static void CheckedExprToUncheckedExprTest()
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

            Assert.Equal(expected_transform, actual_transform);
        }

        [Fact]
        public static void UncheckedExprToCheckedExprTest()
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

            Assert.Equal(expected_transform, actual_transform);
        }

        [Fact]
        public static void PostfixToPrefixTest()
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

            Assert.Equal(expected_transform, actual_transform);
        }

        [Fact]
        public static void PrefixToPostfixTest()
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

            Assert.Equal(expected_transform, actual_transform);
        }

        [Fact]
        public static void TrueToFalseTest()
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

            Assert.Equal(expected_transform, actual_transform);
        }

        [Fact]
        public static void FalseToTrueTest()
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

            Assert.Equal(expected_transform, actual_transform);
        }

        [Fact]
        public static void AddAssignToAssignTest()
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

            Assert.Equal(expected_transform, actual_transform);
        }

        [Fact]
        public static void RefParamToOutParamTest()
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

            Assert.Equal(expected_transform, actual_transform);
        }

        [Fact]
        public static void OutParamToRefParamTest()
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

            Assert.Equal(expected_transform, actual_transform);
        }

        [Fact]
        public static void RefArgToOutArgTest()
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

            Assert.Equal(expected_transform, actual_transform);
        }

        [Fact]
        public static void OutArgToRefArgTest()
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

            Assert.Equal(expected_transform, actual_transform);
        }

        [Fact]
        public static void OrderByAscToOrderByDescTest()
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

            Assert.Equal(expected_transform, actual_transform);
        }

        [Fact]
        public static void OrderByDescToOrderByAscTest()
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

            Assert.Equal(expected_transform, actual_transform);
        }

        [Fact]
        public static void DefaultInitAllVarsTest()
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
        int i = default(int ), j = default(int );
        Program f1 = default(Program );
    }
}
";
            string actual_transform = Transforms.Transform(input, TransformKind.DefaultInitAllVars);

            Assert.Equal(expected_transform, actual_transform);
        }

        [Fact]
        public static void ClassDeclToStructDeclTest()
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

            Assert.Equal(expected_transform, actual_transform);
        }

        [Fact]
        public static void StructDeclToClassDeclTest()
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

            Assert.Equal(expected_transform, actual_transform);
        }

        [Fact]
        public static void IntTypeToLongTypeTest()
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

            Assert.Equal(expected_transform, actual_transform);
        }
    }
}
