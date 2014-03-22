using System.Collections.Generic;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.CSharp.Test.Utilities;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.CSharp.UnitTests.Semantics
{
    public class DeclarationExpressionsTests : CompilingTestBase
    {
        [Fact]
        public void Simple_01()
        {
            var text = @"
public class Cls
{
    public static void Main()
    {
        Test(int y = 123);
        System.Console.WriteLine(y);
    }

    static void Test(int x)
    {
        System.Console.WriteLine(x);
    }
}";
            var compilation = CreateCompilationWithMscorlib(text, compOptions: TestOptions.Exe);
            
            CompileAndVerify(compilation, expectedOutput: @"123
123").VerifyDiagnostics();
        }

        [Fact]
        public void Simple_02()
        {
            var text = @"
public class Cls
{
    public static void Main()
    {
        i = 1;
        System.Console.WriteLine(int i = 3);

        System.Console.WriteLine(int j = 3);
        System.Console.WriteLine(int j = 4);

        System.Console.WriteLine(int k = 3);
        int k = 4;

        int l = 4;
        System.Console.WriteLine(int l = 3);

        int m = 5;
        {
            System.Console.WriteLine(int m = 4);
        }

        System.Console.WriteLine(m);
    }
}";
            var compilation = CreateCompilationWithMscorlib(text, compOptions: TestOptions.Exe);

            compilation.VerifyDiagnostics(
    // (6,9): error CS0841: Cannot use local variable 'i' before it is declared
    //         i = 1;
    Diagnostic(ErrorCode.ERR_VariableUsedBeforeDeclaration, "i").WithArguments("i").WithLocation(6, 9),
    // (10,38): error CS0128: A local variable named 'j' is already defined in this scope
    //         System.Console.WriteLine(int j = 4);
    Diagnostic(ErrorCode.ERR_LocalDuplicate, "j").WithArguments("j").WithLocation(10, 38),
    // (13,13): error CS0128: A local variable named 'k' is already defined in this scope
    //         int k = 4;
    Diagnostic(ErrorCode.ERR_LocalDuplicate, "k").WithArguments("k").WithLocation(13, 13),
    // (16,38): error CS0128: A local variable named 'l' is already defined in this scope
    //         System.Console.WriteLine(int l = 3);
    Diagnostic(ErrorCode.ERR_LocalDuplicate, "l").WithArguments("l").WithLocation(16, 38),
    // (20,42): error CS0136: A local or parameter named 'm' cannot be declared in this scope because that name is used in an enclosing local scope to define a local or parameter
    //             System.Console.WriteLine(int m = 4);
    Diagnostic(ErrorCode.ERR_LocalIllegallyOverrides, "m").WithArguments("m").WithLocation(20, 42),
    // (13,13): warning CS0219: The variable 'k' is assigned but its value is never used
    //         int k = 4;
    Diagnostic(ErrorCode.WRN_UnreferencedVarAssg, "k").WithArguments("k").WithLocation(13, 13),
    // (15,13): warning CS0219: The variable 'l' is assigned but its value is never used
    //         int l = 4;
    Diagnostic(ErrorCode.WRN_UnreferencedVarAssg, "l").WithArguments("l").WithLocation(15, 13)
                );
        }

        [Fact]
        public void Simple_03()
        {
            var text = @"
public class Cls
{
    public static void Main()
    {
        int x = (int y = 123);
        System.Console.WriteLine(x);
        System.Console.WriteLine(y);
    }
}";
            var compilation = CreateCompilationWithMscorlib(text, compOptions: TestOptions.Exe);

            CompileAndVerify(compilation, expectedOutput: @"123
123").VerifyDiagnostics();
        }

        [Fact]
        public void Simple_04()
        {
            var text = @"
public class Cls
{
    public static void Main()
    {
        int x = (int y) = 123;
        System.Console.WriteLine(x);
        System.Console.WriteLine(y);
    }
}";
            var compilation = CreateCompilationWithMscorlib(text, compOptions: TestOptions.Exe);

            CompileAndVerify(compilation, expectedOutput: @"123
123").VerifyDiagnostics();
        }

        [Fact]
        public void ERR_DeclarationExpressionOutsideOfAMethodBody_01()
        {
            var text = @"
public class Cls
{

    int x = int y = 3;

    public static void Main()
    {
    }

    static void Test(int p = int y = 3)
    {
    }
}";
            var compilation = CreateCompilationWithMscorlib(text, compOptions: TestOptions.Exe);

            compilation.VerifyDiagnostics(
    // (11,30): error CS1736: Default parameter value for 'p' must be a compile-time constant
    //     static void Test(int p = int y = 3)
    Diagnostic(ErrorCode.ERR_DefaultValueMustBeConstant, "int y = 3").WithArguments("p").WithLocation(11, 30),
    // (5,13): error CS8028: A declaration expression is not permitted in a variable-initializer of a field declaration, or in a class-base specification.
    //     int x = int y = 3;
    Diagnostic(ErrorCode.ERR_DeclarationExpressionOutsideOfAMethodBody, "int y = 3").WithLocation(5, 13)
                );
        }

        [Fact]
        public void SimpleVar_01()
        {
            var text = @"
public class Cls
{
    public static void Main()
    {
        Test(var y = 123);
        System.Console.WriteLine(y);
        PrintType(y);
    }

    static void Test(int x)
    {
        System.Console.WriteLine(x);
    }

    static void PrintType<T>(T x)
    {
        System.Console.WriteLine(typeof(T));
    }
}";
            var compilation = CreateCompilationWithMscorlib(text, compOptions: TestOptions.Exe);

            CompileAndVerify(compilation, expectedOutput: @"123
123
System.Int32").VerifyDiagnostics();
        }

        [Fact]
        public void SimpleVar_02()
        {
            var text = @"
public class Cls
{
    public static void Main()
    {
        Test(var y);
    }

    static void Test(int x)
    {
    }
}";
            var compilation = CreateCompilationWithMscorlib(text, compOptions: TestOptions.Exe);

            compilation.VerifyDiagnostics(
    // (6,18): error CS0818: Implicitly-typed variables must be initialized
    //         Test(var y);
    Diagnostic(ErrorCode.ERR_ImplicitlyTypedVariableWithNoInitializer, "y").WithLocation(6, 18),
    // (6,14): error CS0165: Use of unassigned local variable 'y'
    //         Test(var y);
    Diagnostic(ErrorCode.ERR_UseDefViolation, "var y").WithArguments("y").WithLocation(6, 14)
                );
        }

        [Fact]
        public void SimpleVar_03()
        {
            var text = @"
public class Cls
{
    public static void Main()
    {
        var x = 1 + x;
        Test(var y = 1 + y);
    }

    static void Test(int x)
    {
    }
}";
            var compilation = CreateCompilationWithMscorlib(text, compOptions: TestOptions.Exe);

            compilation.VerifyDiagnostics(
    // (6,21): error CS0841: Cannot use local variable 'x' before it is declared
    //         var x = 1 + x;
    Diagnostic(ErrorCode.ERR_VariableUsedBeforeDeclaration, "x").WithArguments("x").WithLocation(6, 21),
    // (7,26): error CS0841: Cannot use local variable 'y' before it is declared
    //         Test(var y = 1 + y);
    Diagnostic(ErrorCode.ERR_VariableUsedBeforeDeclaration, "y").WithArguments("y").WithLocation(7, 26),
    // (6,21): error CS0165: Use of unassigned local variable 'x'
    //         var x = 1 + x;
    Diagnostic(ErrorCode.ERR_UseDefViolation, "x").WithArguments("x").WithLocation(6, 21),
    // (7,26): error CS0165: Use of unassigned local variable 'y'
    //         Test(var y = 1 + y);
    Diagnostic(ErrorCode.ERR_UseDefViolation, "y").WithArguments("y").WithLocation(7, 26)
                );
        }

        [Fact]
        public void For_01()
        {
            var text = @"
public class Cls
{
    public static void Main()
    {
        for (int i = (int)double j = 0; (j = i) < 2; i=(int)j+1)
        {
            System.Console.WriteLine(j);
        }

        for (int i = (int)double j = 10; (j = i) < 12; i=(int)j+1)
            System.Console.WriteLine(j + (int k = 5 + i) + k);

        int ii;
        for (ii = (int)double j = 10; (j = ii) < 12; ii=(int)j+1)
            System.Console.WriteLine(j + (int k = 5 + ii) + k);
    }
}";
            var compilation = CreateCompilationWithMscorlib(text, compOptions: TestOptions.Exe);

            CompileAndVerify(compilation, expectedOutput: @"0
1
40
43
40
43").VerifyDiagnostics();
        }

        [Fact]
        public void For_02()
        {
            var text = @"
public class Cls
{
    public static void Main()
    {
        for (int i = 0; i < 2; i = (int j = i + 1) + j)
        {
            System.Console.WriteLine(j);
        }

        for (int i = (int)((double j = 10) + j); (j = i) < 12; i++)
            System.Console.WriteLine(j + (int k = 5 + i));

        j = 3;
        k = 4;

        for (int i = l; i < int l = 12; i++) {}

        for (int i = 0; i < m; i = (int m = 12) + m) {}

        for (int i = n; i < 1; i++) 
            System.Console.WriteLine((int n = 5) + n);

        for (int i = 0; i < o; i++) 
            System.Console.WriteLine(int o = 5);

        for (int i = 0; i < 1; i = p) 
            System.Console.WriteLine(int p = 5);

        for (int i = 0; i < q; i++)
        { 
            System.Console.WriteLine((int q = 5) + q);
        }

        for (int i = 0; i < 1; i = r) 
        {
            System.Console.WriteLine(int r = 5);
        }


        int a1 = 1;
        System.Console.WriteLine((int b1 = 5) + a1);
            
        for (int i = (int a1 = 0); i < 0 ;);
        for (int i = (int b1 = 0); i < 0 ;);

        int a2 = 1;
        System.Console.WriteLine(a2);
        for (int i = 0; i < (int a2 = 0) ;);

        int a3 = 1;
        System.Console.WriteLine(a3);
        for (int i = 0; i < 0; i +=(int a3 = 0));

        int a4 = 1;
        System.Console.WriteLine(a4);
        for (int i = 0; i < 0; i ++) 
            System.Console.WriteLine(int a4 = 0);

        int a5 = 1;
        System.Console.WriteLine(a5);
        for (int i = 0; i < 0; i ++) 
        {
            System.Console.WriteLine(int a5 = 0);
        }


        for (int i = (int c1 = 0) + (int c1 = 1); i < 0 ;);

        for (int i = (int c2 = 0); i < (int c2 = 0) ;);

        for (int i = (int c3 = 0); i < 0; i +=(int c3 = 0));

        for (int i = (int c4 = 0); i < 0; i ++) 
            System.Console.WriteLine(int c4 = 0);

        for (int i = (int c5 = 0); i < 0; i ++) 
        {
            System.Console.WriteLine(int c5 = 0);
        }

        for (int i = (int c6 = 0); i < 0; i ++) 
        {
            int c6 = 0;
            System.Console.WriteLine(c6);
        }


        for (int i = 0; (int d1 = 0) < (int d1 = 1); i++);

        for (int i = 0; (int d2 = 0) < 0; i += (int d2 = 0));

        for (int i = 0; (int d3 = 0) < 0; i ++) 
            System.Console.WriteLine(int d3 = 0);

        for (int i = 0; (int d4 = 0) < 0; i ++) 
        {
            System.Console.WriteLine(int d4 = 0);
        }

        for (int i = 0; (int d5 = 0) < 0; i ++) 
        {
            int d5 = 0;
            System.Console.WriteLine(d5);
        }


        for (int i = 0; i < 0; i += (int e1 = 0) + (int e1 = 1));

        for (int i = 0; i < 0; i += (int e2 = 0)) 
            System.Console.WriteLine(int e2 = 0);

        for (int i = 0; i < 0; i += (int e3 = 0)) 
        {
            System.Console.WriteLine(int e3 = 0);
        }

        for (int i = 0; i < 0; i += (int e4 = 0)) 
        {
            int e4 = 0;
            System.Console.WriteLine(e4);
        }


        for (int i = 0; i < 0; i ++) 
            System.Console.WriteLine((int f1 = 0)+(int f1 = 1));
    }
}";
            var compilation = CreateCompilationWithMscorlib(text, compOptions: TestOptions.Exe);

            compilation.VerifyDiagnostics(
    // (14,9): error CS0103: The name 'j' does not exist in the current context
    //         j = 3;
    Diagnostic(ErrorCode.ERR_NameNotInContext, "j").WithArguments("j").WithLocation(14, 9),
    // (15,9): error CS0103: The name 'k' does not exist in the current context
    //         k = 4;
    Diagnostic(ErrorCode.ERR_NameNotInContext, "k").WithArguments("k").WithLocation(15, 9),
    // (17,22): error CS0103: The name 'l' does not exist in the current context
    //         for (int i = l; i < int l = 12; i++) {}
    Diagnostic(ErrorCode.ERR_NameNotInContext, "l").WithArguments("l").WithLocation(17, 22),
    // (19,29): error CS0841: Cannot use local variable 'm' before it is declared
    //         for (int i = 0; i < m; i = (int m = 12)) {}
    Diagnostic(ErrorCode.ERR_VariableUsedBeforeDeclaration, "m").WithArguments("m").WithLocation(19, 29),
    // (21,22): error CS0103: The name 'n' does not exist in the current context
    //         for (int i = n; i < 1; i++) 
    Diagnostic(ErrorCode.ERR_NameNotInContext, "n").WithArguments("n").WithLocation(21, 22),
    // (24,29): error CS0103: The name 'o' does not exist in the current context
    //         for (int i = 0; i < o; i++) 
    Diagnostic(ErrorCode.ERR_NameNotInContext, "o").WithArguments("o").WithLocation(24, 29),
    // (27,36): error CS0103: The name 'p' does not exist in the current context
    //         for (int i = 0; i < 1; i = p) 
    Diagnostic(ErrorCode.ERR_NameNotInContext, "p").WithArguments("p").WithLocation(27, 36),
    // (30,29): error CS0103: The name 'q' does not exist in the current context
    //         for (int i = 0; i < q; i++)
    Diagnostic(ErrorCode.ERR_NameNotInContext, "q").WithArguments("q").WithLocation(30, 29),
    // (35,36): error CS0103: The name 'r' does not exist in the current context
    //         for (int i = 0; i < 1; i = r) 
    Diagnostic(ErrorCode.ERR_NameNotInContext, "r").WithArguments("r").WithLocation(35, 36),
    // (44,27): error CS0136: A local or parameter named 'a1' cannot be declared in this scope because that name is used in an enclosing local scope to define a local or parameter
    //         for (int i = (int a1 = 0); i < 0 ;);
    Diagnostic(ErrorCode.ERR_LocalIllegallyOverrides, "a1").WithArguments("a1").WithLocation(44, 27),
    // (45,27): error CS0136: A local or parameter named 'b1' cannot be declared in this scope because that name is used in an enclosing local scope to define a local or parameter
    //         for (int i = (int b1 = 0); i < 0 ;);
    Diagnostic(ErrorCode.ERR_LocalIllegallyOverrides, "b1").WithArguments("b1").WithLocation(45, 27),
    // (49,34): error CS0136: A local or parameter named 'a2' cannot be declared in this scope because that name is used in an enclosing local scope to define a local or parameter
    //         for (int i = 0; i < (int a2 = 0) ;);
    Diagnostic(ErrorCode.ERR_LocalIllegallyOverrides, "a2").WithArguments("a2").WithLocation(49, 34),
    // (53,41): error CS0136: A local or parameter named 'a3' cannot be declared in this scope because that name is used in an enclosing local scope to define a local or parameter
    //         for (int i = 0; i < 0; i +=(int a3 = 0));
    Diagnostic(ErrorCode.ERR_LocalIllegallyOverrides, "a3").WithArguments("a3").WithLocation(53, 41),
    // (58,42): error CS0136: A local or parameter named 'a4' cannot be declared in this scope because that name is used in an enclosing local scope to define a local or parameter
    //             System.Console.WriteLine(int a4 = 0);
    Diagnostic(ErrorCode.ERR_LocalIllegallyOverrides, "a4").WithArguments("a4").WithLocation(58, 42),
    // (64,42): error CS0136: A local or parameter named 'a5' cannot be declared in this scope because that name is used in an enclosing local scope to define a local or parameter
    //             System.Console.WriteLine(int a5 = 0);
    Diagnostic(ErrorCode.ERR_LocalIllegallyOverrides, "a5").WithArguments("a5").WithLocation(64, 42),
    // (68,42): error CS0128: A local variable named 'c1' is already defined in this scope
    //         for (int i = (int c1 = 0) + (int c1 = 1); i < 0 ;);
    Diagnostic(ErrorCode.ERR_LocalDuplicate, "c1").WithArguments("c1").WithLocation(68, 42),
    // (70,45): error CS0136: A local or parameter named 'c2' cannot be declared in this scope because that name is used in an enclosing local scope to define a local or parameter
    //         for (int i = (int c2 = 0); i < (int c2 = 0) ;);
    Diagnostic(ErrorCode.ERR_LocalIllegallyOverrides, "c2").WithArguments("c2").WithLocation(70, 45),
    // (72,52): error CS0136: A local or parameter named 'c3' cannot be declared in this scope because that name is used in an enclosing local scope to define a local or parameter
    //         for (int i = (int c3 = 0); i < 0; i +=(int c3 = 0));
    Diagnostic(ErrorCode.ERR_LocalIllegallyOverrides, "c3").WithArguments("c3").WithLocation(72, 52),
    // (75,42): error CS0136: A local or parameter named 'c4' cannot be declared in this scope because that name is used in an enclosing local scope to define a local or parameter
    //             System.Console.WriteLine(int c4 = 0);
    Diagnostic(ErrorCode.ERR_LocalIllegallyOverrides, "c4").WithArguments("c4").WithLocation(75, 42),
    // (79,42): error CS0136: A local or parameter named 'c5' cannot be declared in this scope because that name is used in an enclosing local scope to define a local or parameter
    //             System.Console.WriteLine(int c5 = 0);
    Diagnostic(ErrorCode.ERR_LocalIllegallyOverrides, "c5").WithArguments("c5").WithLocation(79, 42),
    // (84,17): error CS0136: A local or parameter named 'c6' cannot be declared in this scope because that name is used in an enclosing local scope to define a local or parameter
    //             int c6 = 0;
    Diagnostic(ErrorCode.ERR_LocalIllegallyOverrides, "c6").WithArguments("c6").WithLocation(84, 17),
    // (89,45): error CS0128: A local variable named 'd1' is already defined in this scope
    //         for (int i = 0; (int d1 = 0) < (int d1 = 1); );
    Diagnostic(ErrorCode.ERR_LocalDuplicate, "d1").WithArguments("d1").WithLocation(89, 45),
    // (91,53): error CS0128: A local variable named 'd2' is already defined in this scope
    //         for (int i = 0; (int d2 = 0) < 0; i += (int d2 = 0));
    Diagnostic(ErrorCode.ERR_LocalDuplicate, "d2").WithArguments("d2").WithLocation(91, 53),
    // (94,42): error CS0136: A local or parameter named 'd3' cannot be declared in this scope because that name is used in an enclosing local scope to define a local or parameter
    //             System.Console.WriteLine(int d3 = 0);
    Diagnostic(ErrorCode.ERR_LocalIllegallyOverrides, "d3").WithArguments("d3").WithLocation(94, 42),
    // (98,42): error CS0136: A local or parameter named 'd4' cannot be declared in this scope because that name is used in an enclosing local scope to define a local or parameter
    //             System.Console.WriteLine(int d4 = 0);
    Diagnostic(ErrorCode.ERR_LocalIllegallyOverrides, "d4").WithArguments("d4").WithLocation(98, 42),
    // (103,17): error CS0136: A local or parameter named 'd5' cannot be declared in this scope because that name is used in an enclosing local scope to define a local or parameter
    //             int d5 = 0;
    Diagnostic(ErrorCode.ERR_LocalIllegallyOverrides, "d5").WithArguments("d5").WithLocation(103, 17),
    // (108,57): error CS0128: A local variable named 'e1' is already defined in this scope
    //         for (int i = 0; i < 0; i += (int e1 = 0) + (int e1 = 1));
    Diagnostic(ErrorCode.ERR_LocalDuplicate, "e1").WithArguments("e1").WithLocation(108, 57),
    // (111,42): error CS0136: A local or parameter named 'e2' cannot be declared in this scope because that name is used in an enclosing local scope to define a local or parameter
    //             System.Console.WriteLine(int e2 = 0);
    Diagnostic(ErrorCode.ERR_LocalIllegallyOverrides, "e2").WithArguments("e2").WithLocation(111, 42),
    // (115,42): error CS0136: A local or parameter named 'e3' cannot be declared in this scope because that name is used in an enclosing local scope to define a local or parameter
    //             System.Console.WriteLine(int e3 = 0);
    Diagnostic(ErrorCode.ERR_LocalIllegallyOverrides, "e3").WithArguments("e3").WithLocation(115, 42),
    // (120,17): error CS0136: A local or parameter named 'e4' cannot be declared in this scope because that name is used in an enclosing local scope to define a local or parameter
    //             int e4 = 0;
    Diagnostic(ErrorCode.ERR_LocalIllegallyOverrides, "e4").WithArguments("e4").WithLocation(120, 17),
    // (126,56): error CS0128: A local variable named 'f1' is already defined in this scope
    //             System.Console.WriteLine((int f1 = 0)+(int f1 = 1));
    Diagnostic(ErrorCode.ERR_LocalDuplicate, "f1").WithArguments("f1").WithLocation(126, 56),
    // (8,38): error CS0165: Use of unassigned local variable 'j'
    //             System.Console.WriteLine(j);
    Diagnostic(ErrorCode.ERR_UseDefViolation, "j").WithArguments("j").WithLocation(8, 38)
                );
        }

        [Fact]
        public void For_03()
        {
            var text = @"
public class Cls
{
    public static void Main()
    {
        var lambdasJ = new System.Func<int>[3];
        var lambdasK = new System.Func<int>[2];
        var lambdasL = new System.Func<int>[2];

        for (int i = 0; 
                i < Dummy(2, int j = (i+1)*10, lambdasJ[i] = () => j);
                i = Dummy(i+1, int k = (i+1)*100, lambdasK[i] = () => k))
            Dummy(i, int l = (i+1)*1000, lambdasL[i] = () => l);

        foreach (var i in lambdasJ)
            System.Console.WriteLine(i());

        foreach (var i in lambdasK)
            System.Console.WriteLine(i());

        foreach (var i in lambdasL)
            System.Console.WriteLine(i());
    }

    static int Dummy(int val, object p1, object p2)
    {
        return val;
    }
}";
            var compilation = CreateCompilationWithMscorlib(text, compOptions: TestOptions.Exe);

            CompileAndVerify(compilation, expectedOutput: @"10
20
30
100
200
1000
2000").VerifyDiagnostics();
        }

        [Fact]
        public void For_04()
        {
            var text = @"
public class Cls
{
    public static void Main()
    {
        var lambdasJ = new System.Func<int>[3];

        for (int i = 0; 
                i < Dummy(2, int j = (i+1)*10, lambdasJ[i] = () => j);
                )
            i++;

        foreach (var i in lambdasJ)
            System.Console.WriteLine(i());
    }

    static int Dummy(int val, object p1, object p2)
    {
        return val;
    }
}";
            var compilation = CreateCompilationWithMscorlib(text, compOptions: TestOptions.Exe);

            CompileAndVerify(compilation, expectedOutput: @"10
20
30").VerifyDiagnostics();
        }

        [Fact]
        public void For_05()
        {
            var text = @"
public class Cls
{
    public static void Main()
    {
        var lambdasK = new System.Func<int>[2];

        for (int i = 0; 
                ;
                i = Dummy(i+1, int k = (i+1)*100, lambdasK[i] = () => k))
            if (i >= 2) break;

        foreach (var i in lambdasK)
            System.Console.WriteLine(i());
    }

    static int Dummy(int val, object p1, object p2)
    {
        return val;
    }
}";
            var compilation = CreateCompilationWithMscorlib(text, compOptions: TestOptions.Exe);

            CompileAndVerify(compilation, expectedOutput: @"100
200").VerifyDiagnostics();
        }

        [Fact]
        public void For_06()
        {
            var text = @"
public class Cls
{
    public static void Main()
    {
        var lambdasJ = new System.Func<int>[3];

        for (int i = 0; 
                i < Dummy(2, int j = (i+1)*10, lambdasJ[i] = () => j);
                i++)
            ;

        foreach (var i in lambdasJ)
            System.Console.WriteLine(i());
    }

    static int Dummy(int val, object p1, object p2)
    {
        return val;
    }
}";
            var compilation = CreateCompilationWithMscorlib(text, compOptions: TestOptions.Exe);

            CompileAndVerify(compilation, expectedOutput: @"10
20
30").VerifyDiagnostics();
        }

        [Fact]
        public void For_07()
        {
            var text = @"
public class Cls
{
    public static void Main()
    {
        var lambdasL = new System.Func<int>[2];

        for (int i = 0; 
                i < 2;
                i++)
            Dummy(i, int l = (i+1)*1000, lambdasL[i] = () => l);

        foreach (var i in lambdasL)
            System.Console.WriteLine(i());
    }

    static int Dummy(int val, object p1, object p2)
    {
        return val;
    }
}";
            var compilation = CreateCompilationWithMscorlib(text, compOptions: TestOptions.Exe);

            CompileAndVerify(compilation, expectedOutput: @"1000
2000").VerifyDiagnostics();
        }

        [Fact]
        public void Using_01()
        {
            var text = @"
using System.Collections.Generic;

public class Cls
{
    public static void Main()
    {
        using (var e = ((IEnumerable<int>)(new [] { int j = 0, 1})).GetEnumerator())
        {
            while(e.MoveNext())
            {
                System.Console.WriteLine(j);
                j++;
            }
        }

        using (var e = ((IEnumerable<int>)(new [] { int j = 3, 1})).GetEnumerator())
            System.Console.WriteLine(j + (int k = 5) + k);

        using (((IEnumerable<int>)(new [] { int j = 5, 1})).GetEnumerator())
            System.Console.WriteLine(j + (int k = 10) + k);
    }
}";
            var compilation = CreateCompilationWithMscorlib(text, compOptions: TestOptions.Exe);

            CompileAndVerify(compilation, expectedOutput: @"0
1
13
25").VerifyDiagnostics();
        }

        [Fact]
        public void Using_02()
        {
            var text = @"
using System.Collections.Generic;

public class Cls
{
    public static void Main()
    {
        using (var e = ((IEnumerable<int>)(new [] { (int j = 0) + j, 1})).GetEnumerator())
        {
            while(e.MoveNext())
            {
                System.Console.WriteLine(j);
                j++;
            }
        }

        using (var e = ((IEnumerable<int>)(new [] { int j = 3, 1})).GetEnumerator())
            System.Console.WriteLine(j + (int k = 5) + k);

        j = 3;
        k = 4;

        using (var e = l)
        {
            System.Console.WriteLine(IEnumerator<int> l = null);
        }

        using (var e = m)
            System.Console.WriteLine(IEnumerator<int> m = null);

        int a1 = 0;
        System.Console.WriteLine(a1 + (int b1 = 1));

        using (var e = ((IEnumerable<int>)(new [] { int a1 = 3, 1})).GetEnumerator()) System.Console.WriteLine();
        using (var e = ((IEnumerable<int>)(new [] { int b1 = 3, 1})).GetEnumerator()) System.Console.WriteLine();

        int a2 = 0;
        System.Console.WriteLine(a2);
        using (var e = ((IEnumerable<int>)(new [] { 0, 1})).GetEnumerator()) 
            System.Console.WriteLine(int a2 = 1);

        int a3 = 0;
        System.Console.WriteLine(a3);
        using (var e = ((IEnumerable<int>)(new [] { 0, 1})).GetEnumerator()) 
        {
            System.Console.WriteLine(int a3 = 1);
        }

        using (var c1 = ((IEnumerable<int>)(new [] { int c1 = 3, 1})).GetEnumerator()) System.Console.WriteLine();

        using (var c2 = ((IEnumerable<int>)(new [] { 0, 1})).GetEnumerator()) 
        {
            System.Console.WriteLine(int c2 = 1);
        }

        using (var e = ((IEnumerable<int>)(new [] { int d1 = 3, int d1 = 4})).GetEnumerator()) System.Console.WriteLine();

        using (var e = ((IEnumerable<int>)(new [] { int d2 = 3, 1})).GetEnumerator()) 
            System.Console.WriteLine(int d2 = 1);

        using (var e = ((IEnumerable<int>)(new [] { int d3 = 3, 1})).GetEnumerator()) 
        {
            System.Console.WriteLine(int d3 = 1);
        }

        using (var e = ((IEnumerable<int>)(new [] { int d4 = 3, 1})).GetEnumerator()) 
        {
            int d4 = 0;
            System.Console.WriteLine(d4);
        }

        using (var c3 = ((IEnumerable<int>)(new [] { 0, 1})).GetEnumerator()) 
            System.Console.WriteLine(int c3 = 1);

        using (var e = ((IEnumerable<int>)(new [] { 0, 1})).GetEnumerator()) 
            System.Console.WriteLine((int e1 = 1) + (int e1 = 1));
    }
}";
            var compilation = CreateCompilationWithMscorlib(text, compOptions: TestOptions.Exe);

            compilation.VerifyDiagnostics(
    // (20,9): error CS0103: The name 'j' does not exist in the current context
    //         j = 3;
    Diagnostic(ErrorCode.ERR_NameNotInContext, "j").WithArguments("j").WithLocation(20, 9),
    // (21,9): error CS0103: The name 'k' does not exist in the current context
    //         k = 4;
    Diagnostic(ErrorCode.ERR_NameNotInContext, "k").WithArguments("k").WithLocation(21, 9),
    // (23,24): error CS0103: The name 'l' does not exist in the current context
    //         using (var e = l)
    Diagnostic(ErrorCode.ERR_NameNotInContext, "l").WithArguments("l").WithLocation(23, 24),
    // (28,24): error CS0103: The name 'm' does not exist in the current context
    //         using (var e = m)
    Diagnostic(ErrorCode.ERR_NameNotInContext, "m").WithArguments("m").WithLocation(28, 24),
    // (34,57): error CS0136: A local or parameter named 'a1' cannot be declared in this scope because that name is used in an enclosing local scope to define a local or parameter
    //         using (var e = ((IEnumerable<int>)(new [] { int a1 = 3, 1})).GetEnumerator()) ;
    Diagnostic(ErrorCode.ERR_LocalIllegallyOverrides, "a1").WithArguments("a1").WithLocation(34, 57),
    // (35,57): error CS0136: A local or parameter named 'b1' cannot be declared in this scope because that name is used in an enclosing local scope to define a local or parameter
    //         using (var e = ((IEnumerable<int>)(new [] { int b1 = 3, 1})).GetEnumerator()) ;
    Diagnostic(ErrorCode.ERR_LocalIllegallyOverrides, "b1").WithArguments("b1").WithLocation(35, 57),
    // (40,42): error CS0136: A local or parameter named 'a2' cannot be declared in this scope because that name is used in an enclosing local scope to define a local or parameter
    //             System.Console.WriteLine(int a2 = 1);
    Diagnostic(ErrorCode.ERR_LocalIllegallyOverrides, "a2").WithArguments("a2").WithLocation(40, 42),
    // (46,42): error CS0136: A local or parameter named 'a3' cannot be declared in this scope because that name is used in an enclosing local scope to define a local or parameter
    //             System.Console.WriteLine(int a3 = 1);
    Diagnostic(ErrorCode.ERR_LocalIllegallyOverrides, "a3").WithArguments("a3").WithLocation(46, 42),
    // (49,58): error CS0128: A local variable named 'c1' is already defined in this scope
    //         using (var c1 = ((IEnumerable<int>)(new [] { int c1 = 3, 1})).GetEnumerator()) ;
    Diagnostic(ErrorCode.ERR_LocalDuplicate, "c1").WithArguments("c1").WithLocation(49, 58),
    // (53,42): error CS0136: A local or parameter named 'c2' cannot be declared in this scope because that name is used in an enclosing local scope to define a local or parameter
    //             System.Console.WriteLine(int c2 = 1);
    Diagnostic(ErrorCode.ERR_LocalIllegallyOverrides, "c2").WithArguments("c2").WithLocation(53, 42),
    // (56,69): error CS0128: A local variable named 'd1' is already defined in this scope
    //         using (var e = ((IEnumerable<int>)(new [] { int d1 = 3, int d1 = 4})).GetEnumerator()) ;
    Diagnostic(ErrorCode.ERR_LocalDuplicate, "d1").WithArguments("d1").WithLocation(56, 69),
    // (59,42): error CS0136: A local or parameter named 'd2' cannot be declared in this scope because that name is used in an enclosing local scope to define a local or parameter
    //             System.Console.WriteLine(int d2 = 1);
    Diagnostic(ErrorCode.ERR_LocalIllegallyOverrides, "d2").WithArguments("d2").WithLocation(59, 42),
    // (63,42): error CS0136: A local or parameter named 'd3' cannot be declared in this scope because that name is used in an enclosing local scope to define a local or parameter
    //             System.Console.WriteLine(int d3 = 1);
    Diagnostic(ErrorCode.ERR_LocalIllegallyOverrides, "d3").WithArguments("d3").WithLocation(63, 42),
    // (68,17): error CS0136: A local or parameter named 'd4' cannot be declared in this scope because that name is used in an enclosing local scope to define a local or parameter
    //             int d4 = 0;
    Diagnostic(ErrorCode.ERR_LocalIllegallyOverrides, "d4").WithArguments("d4").WithLocation(68, 17),
    // (73,42): error CS0136: A local or parameter named 'c3' cannot be declared in this scope because that name is used in an enclosing local scope to define a local or parameter
    //             System.Console.WriteLine(int c3 = 1);
    Diagnostic(ErrorCode.ERR_LocalIllegallyOverrides, "c3").WithArguments("c3").WithLocation(73, 42),
    // (76,58): error CS0128: A local variable named 'e1' is already defined in this scope
    //             System.Console.WriteLine((int e1 = 1) + (int e1 = 1));
    Diagnostic(ErrorCode.ERR_LocalDuplicate, "e1").WithArguments("e1").WithLocation(76, 58)
                );
        }

        [Fact]
        public void Fixed_01()
        {
            var text = @"
public class Cls
{
    public static void Main()
    {
        unsafe
        {
            fixed (int* p = new[] { 1, int j = 2 })
            {
                System.Console.WriteLine(j);
            }

            fixed (int* p = new[] { 1, int j = -20 })
                System.Console.WriteLine(j + (int k = 5) + k);
        }
    }
}";
            var compilation = CreateCompilationWithMscorlib(text, compOptions: TestOptions.Exe.WithAllowUnsafe(true));

            CompileAndVerify(compilation, expectedOutput: @"2
-10").VerifyDiagnostics();
        }

        [Fact]
        public void Fixed_02()
        {
            var text = @"
public class Cls
{
    public static void Main()
    {
        unsafe
        {
            fixed (int* p = new[] { 1, (int j = 2) + j })
            {
                System.Console.WriteLine(j);
            }

            fixed (int* p = new[] { 1, int j = -20 })
                System.Console.WriteLine(j + (int k = 5) + k);

            j = 3;
            k = 4;

            fixed (int* p = l)
            {
                System.Console.WriteLine(int[] l = null);
            }

            fixed (int* p = m)
                System.Console.WriteLine(int[] m = null);

            int a1 = 1;
            System.Console.WriteLine(a1);
            fixed (int* p = new[] { int a1 = 1, 2 })
                System.Console.WriteLine();

            int a2 = 1;
            System.Console.WriteLine(a2);
            fixed (int* p = new[] { 1, 2 })
                System.Console.WriteLine(int a2 = 2);

            int a3 = 1;
            System.Console.WriteLine(a3);
            fixed (int* p = new[] { 1, 2 })
            {
                System.Console.WriteLine(int a3 = 3);
            }

            fixed (int* c1 = new[] { int c1 = 1, 2 })
                System.Console.WriteLine();

            fixed (int* c2 = new[] { 1, 2 })
                System.Console.WriteLine(int c2 = 2);

            fixed (int* c3 = new[] { 1, 2 })
            {
                System.Console.WriteLine(int c3 = 3);
            }

            fixed (int* p = new[] { int d1 = 1, int d1 = 2 })
                System.Console.WriteLine();

            fixed (int* p = new[] { int d2 = 1, 2 })
                System.Console.WriteLine(int d2 = 2);

            fixed (int* p = new[] { int d3 = 1, 2 })
            {
                System.Console.WriteLine(int d3 = 3);
            }

            fixed (int* p = new[] { 1, 2 })
                System.Console.WriteLine((int e1 = 2) + (int e1 = 2));
        }
    }
}";
            var compilation = CreateCompilationWithMscorlib(text, compOptions: TestOptions.Exe.WithAllowUnsafe(true));

            compilation.VerifyDiagnostics(
    // (20,9): error CS0103: The name 'j' does not exist in the current context
    //         j = 3;
    Diagnostic(ErrorCode.ERR_NameNotInContext, "j").WithArguments("j"),
    // (21,9): error CS0103: The name 'k' does not exist in the current context
    //         k = 4;
    Diagnostic(ErrorCode.ERR_NameNotInContext, "k").WithArguments("k"),
    // (19,29): error CS0103: The name 'l' does not exist in the current context
    //             fixed (int* p = l)
    Diagnostic(ErrorCode.ERR_NameNotInContext, "l").WithArguments("l"),
    // (24,29): error CS0103: The name 'm' does not exist in the current context
    //             fixed (int* p = m)
    Diagnostic(ErrorCode.ERR_NameNotInContext, "m").WithArguments("m"),
    // (29,41): error CS0136: A local or parameter named 'a1' cannot be declared in this scope because that name is used in an enclosing local scope to define a local or parameter
    //             fixed (int* p = new[] { int a1 = 1, 2 })
    Diagnostic(ErrorCode.ERR_LocalIllegallyOverrides, "a1").WithArguments("a1").WithLocation(29, 41),
    // (35,46): error CS0136: A local or parameter named 'a2' cannot be declared in this scope because that name is used in an enclosing local scope to define a local or parameter
    //                 System.Console.WriteLine(int a2 = 2);
    Diagnostic(ErrorCode.ERR_LocalIllegallyOverrides, "a2").WithArguments("a2").WithLocation(35, 46),
    // (41,46): error CS0136: A local or parameter named 'a3' cannot be declared in this scope because that name is used in an enclosing local scope to define a local or parameter
    //                 System.Console.WriteLine(int a3 = 3);
    Diagnostic(ErrorCode.ERR_LocalIllegallyOverrides, "a3").WithArguments("a3").WithLocation(41, 46),
    // (44,42): error CS0128: A local variable named 'c1' is already defined in this scope
    //             fixed (int* c1 = new[] { int c1 = 1, 2 })
    Diagnostic(ErrorCode.ERR_LocalDuplicate, "c1").WithArguments("c1").WithLocation(44, 42),
    // (48,46): error CS0136: A local or parameter named 'c2' cannot be declared in this scope because that name is used in an enclosing local scope to define a local or parameter
    //                 System.Console.WriteLine(int c2 = 2);
    Diagnostic(ErrorCode.ERR_LocalIllegallyOverrides, "c2").WithArguments("c2").WithLocation(48, 46),
    // (52,46): error CS0136: A local or parameter named 'c3' cannot be declared in this scope because that name is used in an enclosing local scope to define a local or parameter
    //                 System.Console.WriteLine(int c3 = 3);
    Diagnostic(ErrorCode.ERR_LocalIllegallyOverrides, "c3").WithArguments("c3").WithLocation(52, 46),
    // (55,53): error CS0128: A local variable named 'd1' is already defined in this scope
    //             fixed (int* p = new[] { int d1 = 1, int d1 = 2 })
    Diagnostic(ErrorCode.ERR_LocalDuplicate, "d1").WithArguments("d1").WithLocation(55, 53),
    // (59,46): error CS0136: A local or parameter named 'd2' cannot be declared in this scope because that name is used in an enclosing local scope to define a local or parameter
    //                 System.Console.WriteLine(int d2 = 2);
    Diagnostic(ErrorCode.ERR_LocalIllegallyOverrides, "d2").WithArguments("d2").WithLocation(59, 46),
    // (63,46): error CS0136: A local or parameter named 'd3' cannot be declared in this scope because that name is used in an enclosing local scope to define a local or parameter
    //                 System.Console.WriteLine(int d3 = 3);
    Diagnostic(ErrorCode.ERR_LocalIllegallyOverrides, "d3").WithArguments("d3").WithLocation(63, 46),
    // (67,62): error CS0128: A local variable named 'e1' is already defined in this scope
    //                 System.Console.WriteLine((int e1 = 2) + (int e1 = 2));
    Diagnostic(ErrorCode.ERR_LocalDuplicate, "e1").WithArguments("e1").WithLocation(67, 62)
                );
        }

        [Fact]
        public void switch_01()
        {
            var text = @"
public class Cls
{
    public static void Main()
    {
        switch ((int j = 2) - 1)
        {
            default:
                System.Console.WriteLine(j);
                break;
        }

        switch ((int j = 3) - 1)
        {
            default:
                System.Console.WriteLine(j + (int k = 5) + k);
                break;
            case 298980:
                k = 3;
                System.Console.WriteLine(k);
                break;
        }
    }
}";
            var compilation = CreateCompilationWithMscorlib(text, compOptions: TestOptions.Exe);

            CompileAndVerify(compilation, expectedOutput: @"2
13").VerifyDiagnostics();
        }

        [Fact]
        public void switch_02()
        {
            var text = @"
public class Cls
{
    public static void Main()
    {
        switch ((int j = 2) - j)
        {
            default:
                System.Console.WriteLine(j);
                break;
        }

        switch ((int j = 3) - 1)
        {
            default:
                System.Console.WriteLine(j + (int k = 5) + k);
                break;
        }

        j = 3;
        k = 4;

        switch (l)
        {
            default:
                System.Console.WriteLine(int l = 5);
                break;
        }

        switch ((int j = 3) - 1)
        {
            case 0:
                m=2;
                break;
            default:
                System.Console.WriteLine(int m = 5);
                break;
        }

        int a1 = 0;
        System.Console.WriteLine(a1);
        switch (int a1 = 0)
        {
            default:
                break;
        }
        
        int a2 = 0;
        int a3 = 0;
        System.Console.WriteLine(a2);
        switch (a3)
        {
            default:
                System.Console.WriteLine(int a2 = 5);
                break;
        }
        
        switch ((int c1 = 0) + (int c1 = 1))
        {
            default:
                break;
        }

        switch (int c2 = 0)
        {
            default:
                System.Console.WriteLine(int c2 = 5);
                break;
        }

        switch (int c3 = 0)
        {
            default:
                int c3 = 0;
                System.Console.WriteLine(c3);
                break;
        }
    }
}";
            var compilation = CreateCompilationWithMscorlib(text, compOptions: TestOptions.Exe);

            compilation.VerifyDiagnostics(
    // (14,9): error CS0103: The name 'j' does not exist in the current context
    //         j = 3;
    Diagnostic(ErrorCode.ERR_NameNotInContext, "j").WithArguments("j"),
    // (15,9): error CS0103: The name 'k' does not exist in the current context
    //         k = 4;
    Diagnostic(ErrorCode.ERR_NameNotInContext, "k").WithArguments("k"),
    // (23,17): error CS0103: The name 'l' does not exist in the current context
    //         switch (l)
    Diagnostic(ErrorCode.ERR_NameNotInContext, "l").WithArguments("l"),
    // (33,17): error CS0841: Cannot use local variable 'm' before it is declared
    //                 m=2;
    Diagnostic(ErrorCode.ERR_VariableUsedBeforeDeclaration, "m").WithArguments("m"),
    // (42,21): error CS0136: A local or parameter named 'a1' cannot be declared in this scope because that name is used in an enclosing local scope to define a local or parameter
    //         switch (int a1 = 0)
    Diagnostic(ErrorCode.ERR_LocalIllegallyOverrides, "a1").WithArguments("a1").WithLocation(42, 21),
    // (54,46): error CS0136: A local or parameter named 'a2' cannot be declared in this scope because that name is used in an enclosing local scope to define a local or parameter
    //                 System.Console.WriteLine(int a2 = 5);
    Diagnostic(ErrorCode.ERR_LocalIllegallyOverrides, "a2").WithArguments("a2").WithLocation(54, 46),
    // (58,37): error CS0128: A local variable named 'c1' is already defined in this scope
    //         switch ((int c1 = 0) + (int c1 = 1))
    Diagnostic(ErrorCode.ERR_LocalDuplicate, "c1").WithArguments("c1").WithLocation(58, 37),
    // (67,46): error CS0136: A local or parameter named 'c2' cannot be declared in this scope because that name is used in an enclosing local scope to define a local or parameter
    //                 System.Console.WriteLine(int c2 = 5);
    Diagnostic(ErrorCode.ERR_LocalIllegallyOverrides, "c2").WithArguments("c2").WithLocation(67, 46),
    // (74,21): error CS0136: A local or parameter named 'c3' cannot be declared in this scope because that name is used in an enclosing local scope to define a local or parameter
    //                 int c3 = 0;
    Diagnostic(ErrorCode.ERR_LocalIllegallyOverrides, "c3").WithArguments("c3").WithLocation(74, 21)
                );
        }

        [Fact]
        public void ForEach_01()
        {
            var text = @"
public class Cls
{
    public static void Main()
    {
        foreach (var i in new [] { int j = 0, 1})
        {
            System.Console.WriteLine(j);
            j+=2;
        }

        foreach (var i in new [] { int j = 0, 1})
            System.Console.WriteLine(j = j + (int k = 5) + k);

        var lambdas = new System.Func<int>[2];
        foreach (var i in new [] { 0, 1})
            Dummy(int k = i+30, lambdas[i] = () => k);

        foreach (var i in lambdas)
            System.Console.WriteLine(i());

        foreach (var i in (System.Collections.Generic.IEnumerable<int>)(new [] { int j = 10, 1}))
            System.Console.WriteLine(j = j + i);
    }

    static void Dummy(object p1, object p2){}
}";
            var compilation = CreateCompilationWithMscorlib(text, compOptions: TestOptions.Exe);

            CompileAndVerify(compilation, expectedOutput: @"0
2
10
20
30
31
20
21").VerifyDiagnostics();
        }

        [Fact]
        public void ForEach_02()
        {
            var text = @"
public class Cls
{
    public static void Main()
    {
        foreach (var i in new [] { (int j = 0) + j, 1})
        {
            System.Console.WriteLine(j);
            j+=2;
        }

        foreach (var i in new [] { int j = 0, 1})
            System.Console.WriteLine(j = j + (int k = 5) + k);

        j = 3;
        k = 4;

        foreach (var i in new [] { (int)l})
            System.Console.WriteLine(int l = 5 + i);

        int a1 = 0;
        System.Console.WriteLine(a1);
        foreach (var i in new [] { int a1 = 2 })
            ;

        int a2 = 0;
        System.Console.WriteLine(a2);
        foreach (var i in new [] { 2 })
            System.Console.WriteLine(int a2 = 3);

        int a3 = 0;
        System.Console.WriteLine(a3);
        foreach (var i in new [] { 2 })
        {
            System.Console.WriteLine(int a3 = 3);
        }

        foreach (var i in new [] { int c1 = 2, int c1 = 2 })
            ;

        foreach (var c2 in new [] { int c2 = 2 })
            ;

        foreach (var i in new [] { int c3 = 2 })
            System.Console.WriteLine(int c3 = 3);

        foreach (var i in new [] { int c4 = 2 })
        {
            System.Console.WriteLine(int c4 = 3);
        }

        foreach (var i in new [] { int c5 = 2 })
        {
            int c5 = 0;
            System.Console.WriteLine(c5);
        }

        foreach (var d1 in new [] { 2 })
            System.Console.WriteLine(int d1 = 3);

        foreach (var d2 in new [] { 2 })
        {
            System.Console.WriteLine(int d2 = 3);
        }

        foreach (var i in new [] { 2 })
            System.Console.WriteLine((int e1 = 3) + (int e1 = 3));
    }
}";
            var compilation = CreateCompilationWithMscorlib(text, compOptions: TestOptions.Exe);

            compilation.VerifyDiagnostics(
    // (20,9): error CS0103: The name 'j' does not exist in the current context
    //         j = 3;
    Diagnostic(ErrorCode.ERR_NameNotInContext, "j").WithArguments("j"),
    // (21,9): error CS0103: The name 'k' does not exist in the current context
    //         k = 4;
    Diagnostic(ErrorCode.ERR_NameNotInContext, "k").WithArguments("k"),
    // (18,41): error CS0103: The name 'l' does not exist in the current context
    //         foreach (var i in new [] { (int)l})
    Diagnostic(ErrorCode.ERR_NameNotInContext, "l").WithArguments("l"),
    // (23,40): error CS0136: A local or parameter named 'a1' cannot be declared in this scope because that name is used in an enclosing local scope to define a local or parameter
    //         foreach (var i in new [] { int a1 = 2 })
    Diagnostic(ErrorCode.ERR_LocalIllegallyOverrides, "a1").WithArguments("a1").WithLocation(23, 40),
    // (29,42): error CS0136: A local or parameter named 'a2' cannot be declared in this scope because that name is used in an enclosing local scope to define a local or parameter
    //             System.Console.WriteLine(int a2 = 3);
    Diagnostic(ErrorCode.ERR_LocalIllegallyOverrides, "a2").WithArguments("a2").WithLocation(29, 42),
    // (35,42): error CS0136: A local or parameter named 'a3' cannot be declared in this scope because that name is used in an enclosing local scope to define a local or parameter
    //             System.Console.WriteLine(int a3 = 3);
    Diagnostic(ErrorCode.ERR_LocalIllegallyOverrides, "a3").WithArguments("a3").WithLocation(35, 42),
    // (38,52): error CS0128: A local variable named 'c1' is already defined in this scope
    //         foreach (var i in new [] { int c1 = 2, int c1 = 2 })
    Diagnostic(ErrorCode.ERR_LocalDuplicate, "c1").WithArguments("c1").WithLocation(38, 52),
    // (41,22): error CS0136: A local or parameter named 'c2' cannot be declared in this scope because that name is used in an enclosing local scope to define a local or parameter
    //         foreach (var c2 in new [] { int c2 = 2 })
    Diagnostic(ErrorCode.ERR_LocalIllegallyOverrides, "c2").WithArguments("c2").WithLocation(41, 22),
    // (45,42): error CS0136: A local or parameter named 'c3' cannot be declared in this scope because that name is used in an enclosing local scope to define a local or parameter
    //             System.Console.WriteLine(int c3 = 3);
    Diagnostic(ErrorCode.ERR_LocalIllegallyOverrides, "c3").WithArguments("c3").WithLocation(45, 42),
    // (49,42): error CS0136: A local or parameter named 'c4' cannot be declared in this scope because that name is used in an enclosing local scope to define a local or parameter
    //             System.Console.WriteLine(int c4 = 3);
    Diagnostic(ErrorCode.ERR_LocalIllegallyOverrides, "c4").WithArguments("c4").WithLocation(49, 42),
    // (54,17): error CS0136: A local or parameter named 'c5' cannot be declared in this scope because that name is used in an enclosing local scope to define a local or parameter
    //             int c5 = 0;
    Diagnostic(ErrorCode.ERR_LocalIllegallyOverrides, "c5").WithArguments("c5").WithLocation(54, 17),
    // (59,42): error CS0136: A local or parameter named 'd1' cannot be declared in this scope because that name is used in an enclosing local scope to define a local or parameter
    //             System.Console.WriteLine(int d1 = 3);
    Diagnostic(ErrorCode.ERR_LocalIllegallyOverrides, "d1").WithArguments("d1").WithLocation(59, 42),
    // (63,42): error CS0136: A local or parameter named 'd2' cannot be declared in this scope because that name is used in an enclosing local scope to define a local or parameter
    //             System.Console.WriteLine(int d2 = 3);
    Diagnostic(ErrorCode.ERR_LocalIllegallyOverrides, "d2").WithArguments("d2").WithLocation(63, 42),
    // (67,58): error CS0128: A local variable named 'e1' is already defined in this scope
    //             System.Console.WriteLine((int e1 = 3) + (int e1 = 3));
    Diagnostic(ErrorCode.ERR_LocalDuplicate, "e1").WithArguments("e1").WithLocation(67, 58)
                );
        }

        [Fact]
        public void While_01()
        {
            var text = @"
public class Cls
{
    public static void Main()
    {
        int i;

        i = 0;
        while((int j = i + 1) < 3)
        {
            System.Console.WriteLine(j + i++);
        }

        i = 10;
        while ((int j = i) < 12)
            System.Console.WriteLine(j + (int k = 5 + i) + k + i++);
    }
}";
            var compilation = CreateCompilationWithMscorlib(text, compOptions: TestOptions.Exe);

            CompileAndVerify(compilation, expectedOutput: @"1
3
50
54").VerifyDiagnostics();
        }

        [Fact]
        public void While_02()
        {
            var text = @"
public class Cls
{
    public static void Main()
    {
        while ((int j = 10) < j)
            System.Console.WriteLine(j + (int k = 5) + k);

        j = 3;
        k = 4;

        while (n < 1) 
            System.Console.WriteLine(int n = 5);

        while (0 < q)
        { 
            System.Console.WriteLine(int q = 5);
        }

        int a1 = 0;
        System.Console.WriteLine(a1);
        while (bool a1 = true)
            ;

        int a2 = 0;
        System.Console.WriteLine(a2);
        while (a2 > 0)
            System.Console.WriteLine(int a2 = 1);

        int a3 = 0;
        System.Console.WriteLine(a3);
        while (a2 > 0)
        {
            System.Console.WriteLine(int a3 = 1);
        }

        while ((bool c1 = true) && (bool c1 = true))
            ;

        while (bool c2 = true)
            System.Console.WriteLine(int c2 = 1);

        while (bool c3 = true)
        {
            System.Console.WriteLine(int c3 = 1);
        }

        while (bool c4 = true)
        {
            int c4 = 0;
            System.Console.WriteLine(c4);
        }

        while (a2 > 0)
            System.Console.WriteLine((int d1 = 1) + (int d1 = 1));
    }
}";
            var compilation = CreateCompilationWithMscorlib(text, compOptions: TestOptions.Exe);

            compilation.VerifyDiagnostics(
    // (9,9): error CS0103: The name 'j' does not exist in the current context
    //         j = 3;
    Diagnostic(ErrorCode.ERR_NameNotInContext, "j").WithArguments("j").WithLocation(9, 9),
    // (10,9): error CS0103: The name 'k' does not exist in the current context
    //         k = 4;
    Diagnostic(ErrorCode.ERR_NameNotInContext, "k").WithArguments("k").WithLocation(10, 9),
    // (12,16): error CS0103: The name 'n' does not exist in the current context
    //         while (n < 1) 
    Diagnostic(ErrorCode.ERR_NameNotInContext, "n").WithArguments("n").WithLocation(12, 16),
    // (15,20): error CS0103: The name 'q' does not exist in the current context
    //         while (0 < q)
    Diagnostic(ErrorCode.ERR_NameNotInContext, "q").WithArguments("q").WithLocation(15, 20),
    // (22,21): error CS0136: A local or parameter named 'a1' cannot be declared in this scope because that name is used in an enclosing local scope to define a local or parameter
    //         while (bool a1 = true)
    Diagnostic(ErrorCode.ERR_LocalIllegallyOverrides, "a1").WithArguments("a1").WithLocation(22, 21),
    // (28,42): error CS0136: A local or parameter named 'a2' cannot be declared in this scope because that name is used in an enclosing local scope to define a local or parameter
    //             System.Console.WriteLine(int a2 = 1);
    Diagnostic(ErrorCode.ERR_LocalIllegallyOverrides, "a2").WithArguments("a2").WithLocation(28, 42),
    // (34,42): error CS0136: A local or parameter named 'a3' cannot be declared in this scope because that name is used in an enclosing local scope to define a local or parameter
    //             System.Console.WriteLine(int a3 = 1);
    Diagnostic(ErrorCode.ERR_LocalIllegallyOverrides, "a3").WithArguments("a3").WithLocation(34, 42),
    // (37,42): error CS0128: A local variable named 'c1' is already defined in this scope
    //         while ((bool c1 = true) && (bool c1 = true))
    Diagnostic(ErrorCode.ERR_LocalDuplicate, "c1").WithArguments("c1").WithLocation(37, 42),
    // (41,42): error CS0136: A local or parameter named 'c2' cannot be declared in this scope because that name is used in an enclosing local scope to define a local or parameter
    //             System.Console.WriteLine(int c2 = 1);
    Diagnostic(ErrorCode.ERR_LocalIllegallyOverrides, "c2").WithArguments("c2").WithLocation(41, 42),
    // (45,42): error CS0136: A local or parameter named 'c3' cannot be declared in this scope because that name is used in an enclosing local scope to define a local or parameter
    //             System.Console.WriteLine(int c3 = 1);
    Diagnostic(ErrorCode.ERR_LocalIllegallyOverrides, "c3").WithArguments("c3").WithLocation(45, 42),
    // (50,17): error CS0136: A local or parameter named 'c4' cannot be declared in this scope because that name is used in an enclosing local scope to define a local or parameter
    //             int c4 = 0;
    Diagnostic(ErrorCode.ERR_LocalIllegallyOverrides, "c4").WithArguments("c4").WithLocation(50, 17),
    // (55,58): error CS0128: A local variable named 'd1' is already defined in this scope
    //             System.Console.WriteLine((int d1 = 1) + (int d1 = 1));
    Diagnostic(ErrorCode.ERR_LocalDuplicate, "d1").WithArguments("d1").WithLocation(55, 58)

                );
        }

        [Fact]
        public void While_03()
        {
            var text = @"
public class Cls
{
    public static void Main()
    {
        var lambdasJ = new System.Func<int>[3];
        var lambdasK = new System.Func<int>[2];

        int i = 0; 

        while (i < Dummy(2, int j = (i+1)*10, lambdasJ[i] = () => j))
            i = Dummy(i+1, int k = (i+1)*100, lambdasK[i] = () => k);

        foreach (var l in lambdasJ)
            System.Console.WriteLine(l());

        foreach (var l in lambdasK)
            System.Console.WriteLine(l());
    }

    static int Dummy(int val, object p1, object p2)
    {
        return val;
    }
}";
            var compilation = CreateCompilationWithMscorlib(text, compOptions: TestOptions.Exe);

            CompileAndVerify(compilation, expectedOutput: @"10
20
30
100
200").VerifyDiagnostics();
        }

        [Fact]
        public void While_04()
        {
            var text = @"
public class Cls
{
    public static void Main()
    {
        var lambdasJ = new System.Func<int>[3];

        int i = 0; 

        while (i < Dummy(2, int j = (i+1)*10, lambdasJ[i] = () => j))
            i++;

        foreach (var l in lambdasJ)
            System.Console.WriteLine(l());
    }

    static int Dummy(int val, object p1, object p2)
    {
        return val;
    }
}";
            var compilation = CreateCompilationWithMscorlib(text, compOptions: TestOptions.Exe);

            CompileAndVerify(compilation, expectedOutput: @"10
20
30").VerifyDiagnostics();
        }

        [Fact]
        public void While_05()
        {
            var text = @"
public class Cls
{
    public static void Main()
    {
        var lambdasK = new System.Func<int>[2];

        int i = 0; 

        while (i < 2)
            i = Dummy(i+1, int k = (i+1)*100, lambdasK[i] = () => k);

        foreach (var l in lambdasK)
            System.Console.WriteLine(l());
    }

    static int Dummy(int val, object p1, object p2)
    {
        return val;
    }
}";
            var compilation = CreateCompilationWithMscorlib(text, compOptions: TestOptions.Exe);

            CompileAndVerify(compilation, expectedOutput: @"100
200").VerifyDiagnostics();
        }

        [Fact]
        public void Do_01()
        {
            var text = @"
public class Cls
{
    public static void Main()
    {
        int i;

        i = 0;

        do
        {
            System.Console.WriteLine((int j = i + 1) + j * 2);
        }
        while((int k = Dummy(2, i++)) + k > i * 2);
 
        do
            System.Console.WriteLine((int j = 1) + j * 3);
        while(i < 0);
    }

    private static int Dummy(int val, int p1)
    {
        return val;
    }
}";
            var compilation = CreateCompilationWithMscorlib(text, compOptions: TestOptions.Exe);

            CompileAndVerify(compilation, expectedOutput: @"3
6
4").VerifyDiagnostics();
        }

        [Fact]
        public void Do_02()
        {
            var text = @"
public class Cls
{
    public static void Main()
    {
        do
            System.Console.WriteLine((int k = 5) + k);
        while ((int j = 10) < j);

        j = 3;
        k = 4;

        do
            System.Console.WriteLine(n);
        while ((int n = 5) < 1);

        do
        {
            System.Console.WriteLine(q);
        }
        while ((int q = 5) < 1);

        do
        {
            System.Console.WriteLine(int r = 2);
        }
        while (r < 1);

        do
            System.Console.WriteLine(int s = 1);
        while(s < 3);

        int a1 = 0;
        System.Console.WriteLine(a1);
        do
            System.Console.WriteLine();
        while (bool a1 = true);

        int a2 = 0;
        System.Console.WriteLine(a2);
        do
            System.Console.WriteLine(int a2 = 1);
        while (a2 > 0);

        int a3 = 0;
        System.Console.WriteLine(a3);
        do
        {
            System.Console.WriteLine(int a3 = 1);
        }
        while (a2 > 0);

        do
            System.Console.WriteLine();
        while ((bool c1 = true) && (bool c1 = true));

        do
            System.Console.WriteLine(int c2 = 1);
        while (bool c2 = true);

        do
        {
            System.Console.WriteLine(int c3 = 1);
        }
        while (bool c3 = true);

        do
        {
            int c4 = 0;
            System.Console.WriteLine(c4);
        }
        while (bool c4 = true);

        do
            System.Console.WriteLine((int d1 = 1) + (int d1 = 1));
        while (a2 > 0);

    }
}";
            var compilation = CreateCompilationWithMscorlib(text, compOptions: TestOptions.Exe);

            compilation.VerifyDiagnostics(
    // (10,9): error CS0103: The name 'j' does not exist in the current context
    //         j = 3;
    Diagnostic(ErrorCode.ERR_NameNotInContext, "j").WithArguments("j").WithLocation(10, 9),
    // (11,9): error CS0103: The name 'k' does not exist in the current context
    //         k = 4;
    Diagnostic(ErrorCode.ERR_NameNotInContext, "k").WithArguments("k").WithLocation(11, 9),
    // (14,38): error CS0841: Cannot use local variable 'n' before it is declared
    //             System.Console.WriteLine(n);
    Diagnostic(ErrorCode.ERR_VariableUsedBeforeDeclaration, "n").WithArguments("n").WithLocation(14, 38),
    // (19,38): error CS0841: Cannot use local variable 'q' before it is declared
    //             System.Console.WriteLine(q);
    Diagnostic(ErrorCode.ERR_VariableUsedBeforeDeclaration, "q").WithArguments("q").WithLocation(19, 38),
    // (27,16): error CS0103: The name 'r' does not exist in the current context
    //         while (r < 1);
    Diagnostic(ErrorCode.ERR_NameNotInContext, "r").WithArguments("r").WithLocation(27, 16),
    // (31,15): error CS0103: The name 's' does not exist in the current context
    //         while(s < 3);
    Diagnostic(ErrorCode.ERR_NameNotInContext, "s").WithArguments("s").WithLocation(31, 15),
    // (37,21): error CS0136: A local or parameter named 'a1' cannot be declared in this scope because that name is used in an enclosing local scope to define a local or parameter
    //         while (bool a1 = true);
    Diagnostic(ErrorCode.ERR_LocalIllegallyOverrides, "a1").WithArguments("a1").WithLocation(37, 21),
    // (42,42): error CS0136: A local or parameter named 'a2' cannot be declared in this scope because that name is used in an enclosing local scope to define a local or parameter
    //             System.Console.WriteLine(int a2 = 1);
    Diagnostic(ErrorCode.ERR_LocalIllegallyOverrides, "a2").WithArguments("a2").WithLocation(42, 42),
    // (49,42): error CS0136: A local or parameter named 'a3' cannot be declared in this scope because that name is used in an enclosing local scope to define a local or parameter
    //             System.Console.WriteLine(int a3 = 1);
    Diagnostic(ErrorCode.ERR_LocalIllegallyOverrides, "a3").WithArguments("a3").WithLocation(49, 42),
    // (55,42): error CS0128: A local variable named 'c1' is already defined in this scope
    //         while ((bool c1 = true) && (bool c1 = true));
    Diagnostic(ErrorCode.ERR_LocalDuplicate, "c1").WithArguments("c1").WithLocation(55, 42),
    // (58,42): error CS0136: A local or parameter named 'c2' cannot be declared in this scope because that name is used in an enclosing local scope to define a local or parameter
    //             System.Console.WriteLine(int c2 = 1);
    Diagnostic(ErrorCode.ERR_LocalIllegallyOverrides, "c2").WithArguments("c2").WithLocation(58, 42),
    // (63,42): error CS0136: A local or parameter named 'c3' cannot be declared in this scope because that name is used in an enclosing local scope to define a local or parameter
    //             System.Console.WriteLine(int c3 = 1);
    Diagnostic(ErrorCode.ERR_LocalIllegallyOverrides, "c3").WithArguments("c3").WithLocation(63, 42),
    // (69,17): error CS0136: A local or parameter named 'c4' cannot be declared in this scope because that name is used in an enclosing local scope to define a local or parameter
    //             int c4 = 0;
    Diagnostic(ErrorCode.ERR_LocalIllegallyOverrides, "c4").WithArguments("c4").WithLocation(69, 17),
    // (75,58): error CS0128: A local variable named 'd1' is already defined in this scope
    //             System.Console.WriteLine((int d1 = 1) + (int d1 = 1));
    Diagnostic(ErrorCode.ERR_LocalDuplicate, "d1").WithArguments("d1").WithLocation(75, 58)
                );
        }

        [Fact]
        public void Do_03()
        {
            var text = @"
public class Cls
{
    public static void Main()
    {
        var lambdasJ = new System.Func<int>[2];
        var lambdasK = new System.Func<int>[2];

        int i = 0; 

        do
            i = Dummy(i+1, int k = (i+1)*100, lambdasK[i] = () => k);
        while (i < Dummy(2, int j = (i+1)*10, lambdasJ[i - 1] = () => j));

        foreach (var l in lambdasK)
            System.Console.WriteLine(l());

        foreach (var l in lambdasJ)
            System.Console.WriteLine(l());
    }

    static int Dummy(int val, object p1, object p2)
    {
        return val;
    }
}";
            var compilation = CreateCompilationWithMscorlib(text, compOptions: TestOptions.Exe);

            CompileAndVerify(compilation, expectedOutput: @"100
200
20
30").VerifyDiagnostics();
        }

        [Fact]
        public void if_01()
        {
            var text = @"
public class Cls
{
    public static void Main()
    {
        if ((int j = 2) - 1 > int.MinValue)
        {
            System.Console.WriteLine(j);
        }

        if ((int j = 3) - 1 > int.MinValue)
            System.Console.WriteLine(j);

        if ((int j = 3) - 1 < int.MinValue)
            System.Console.WriteLine(int k = 5);
        else
        {
            System.Console.WriteLine(j + (int k = 100));
        }

        if ((int j = 3) - 1 < int.MinValue)
            System.Console.WriteLine(int k = 5);
        else
            System.Console.WriteLine(j + (int k = 1000));
    }
}";
            var compilation = CreateCompilationWithMscorlib(text, compOptions: TestOptions.Exe);

            CompileAndVerify(compilation, expectedOutput: @"2
3
103
1003").VerifyDiagnostics();
        }

        [Fact]
        public void if_02()
        {
            var text = @"
public class Cls
{
    public static void Main()
    {
        if ((int j = 2) > j)
            System.Console.WriteLine(j + (int k = 5) + k);
        else 
            System.Console.WriteLine(j + (int l = 5) + l);

        if ((int j = 2) > j)
        {
            System.Console.WriteLine(j + (int m = 5) + m);
        }
        else 
            System.Console.WriteLine(j + (int n = 5) + n + m);

        if ((int j = 2) > j)
            System.Console.WriteLine(j + (int k = 5) + k);
        else 
        {
            System.Console.WriteLine(j + (int o = 5) + o);
        }

        j = 3;
        k = 4;
        l = 4;
        m = 4;
        n = 4;
        o = 4;

        if (p < q)
        {
            System.Console.WriteLine(int p = 5);
        }
        else
        {
            System.Console.WriteLine(int q = 5);
        }

        if (r < s)
            System.Console.WriteLine(int r = 5);
        else
            System.Console.WriteLine(int s = 5);

        if ((int x = 3) > 0)
            System.Console.WriteLine((int t = 5) + u);
        else
            System.Console.WriteLine(int u = 5);

        if ((int x = 3) > 0)
        {
            System.Console.WriteLine((int v = 5) + w);
        }
        else
        {
            System.Console.WriteLine((int w = 5) + v);
        }

        if ((int a = 2) > b)
            System.Console.WriteLine(a + c + e);
        else if ((int b = 2) + (int c = 3) > a + d)
            System.Console.WriteLine(int d = 4);
        else 
            System.Console.WriteLine(int e = 5);

        if ((int x = 3) > 0)
            System.Console.WriteLine(int f = 1);
        else
            System.Console.WriteLine(f);

        if ((int x = 3) > g)
            System.Console.WriteLine();
        else 
            System.Console.WriteLine(int g = 5);

        int a1 = 0;
        System.Console.WriteLine(a1);
        if (bool a1 = true)
            System.Console.WriteLine();

        int a2 = 0;
        System.Console.WriteLine(a2);
        int a3 = 0;
        System.Console.WriteLine(a3);

        if (a1 > 0)
            System.Console.WriteLine(int a2 = 1);
        else
            System.Console.WriteLine(int a3 = 1);

        int a4 = 0;
        System.Console.WriteLine(a4);
        int a5 = 0;
        System.Console.WriteLine(a5);

        if (a1 > 0)
        {
            System.Console.WriteLine(int a4 = 1);
        }       
        else
        {
            System.Console.WriteLine(int a5 = 1);
        }

        if ((bool b1 = true) && (bool b1 = true))
            System.Console.WriteLine();

        if ((bool b2 = true) && (bool b3 = true))
            System.Console.WriteLine(int b2 = 1);
        else
            System.Console.WriteLine(int b3 = 1);

        if ((bool b4 = true) && (bool b5 = true))
        {
            System.Console.WriteLine(int b4 = 1);
        }
        else
        {
            System.Console.WriteLine(int b5 = 1);
        }

        if ((bool b6 = true) && (bool b7 = true))
        {
            int b6 = 1;
            System.Console.WriteLine(b6);
        }
        else
        {
            int b7 = 1;
            System.Console.WriteLine(b7);
        }

        if (a2 > 0)
            System.Console.WriteLine((int c1 = 1) + (int c1 = 1));
        else
            System.Console.WriteLine((int c2 = 1) + (int c2 = 1));
    }
}";
            var compilation = CreateCompilationWithMscorlib(text, compOptions: TestOptions.Exe);

            compilation.VerifyDiagnostics(
    // (16,60): error CS0103: The name 'm' does not exist in the current context
    //             System.Console.WriteLine(j + (int n = 5) + n + m);
    Diagnostic(ErrorCode.ERR_NameNotInContext, "m").WithArguments("m").WithLocation(16, 60),
    // (25,9): error CS0103: The name 'j' does not exist in the current context
    //         j = 3;
    Diagnostic(ErrorCode.ERR_NameNotInContext, "j").WithArguments("j").WithLocation(25, 9),
    // (26,9): error CS0103: The name 'k' does not exist in the current context
    //         k = 4;
    Diagnostic(ErrorCode.ERR_NameNotInContext, "k").WithArguments("k").WithLocation(26, 9),
    // (27,9): error CS0103: The name 'l' does not exist in the current context
    //         l = 4;
    Diagnostic(ErrorCode.ERR_NameNotInContext, "l").WithArguments("l").WithLocation(27, 9),
    // (28,9): error CS0103: The name 'm' does not exist in the current context
    //         m = 4;
    Diagnostic(ErrorCode.ERR_NameNotInContext, "m").WithArguments("m").WithLocation(28, 9),
    // (29,9): error CS0103: The name 'n' does not exist in the current context
    //         n = 4;
    Diagnostic(ErrorCode.ERR_NameNotInContext, "n").WithArguments("n").WithLocation(29, 9),
    // (30,9): error CS0103: The name 'o' does not exist in the current context
    //         o = 4;
    Diagnostic(ErrorCode.ERR_NameNotInContext, "o").WithArguments("o").WithLocation(30, 9),
    // (32,17): error CS0103: The name 'q' does not exist in the current context
    //         if (p < q)
    Diagnostic(ErrorCode.ERR_NameNotInContext, "q").WithArguments("q").WithLocation(32, 17),
    // (32,13): error CS0103: The name 'p' does not exist in the current context
    //         if (p < q)
    Diagnostic(ErrorCode.ERR_NameNotInContext, "p").WithArguments("p").WithLocation(32, 13),
    // (41,17): error CS0103: The name 's' does not exist in the current context
    //         if (r < s)
    Diagnostic(ErrorCode.ERR_NameNotInContext, "s").WithArguments("s").WithLocation(41, 17),
    // (41,13): error CS0103: The name 'r' does not exist in the current context
    //         if (r < s)
    Diagnostic(ErrorCode.ERR_NameNotInContext, "r").WithArguments("r").WithLocation(41, 13),
    // (47,52): error CS0103: The name 'u' does not exist in the current context
    //             System.Console.WriteLine((int t = 5) + u);
    Diagnostic(ErrorCode.ERR_NameNotInContext, "u").WithArguments("u").WithLocation(47, 52),
    // (53,52): error CS0103: The name 'w' does not exist in the current context
    //             System.Console.WriteLine((int v = 5) + w);
    Diagnostic(ErrorCode.ERR_NameNotInContext, "w").WithArguments("w").WithLocation(53, 52),
    // (57,52): error CS0103: The name 'v' does not exist in the current context
    //             System.Console.WriteLine((int w = 5) + v);
    Diagnostic(ErrorCode.ERR_NameNotInContext, "v").WithArguments("v").WithLocation(57, 52),
    // (60,27): error CS0103: The name 'b' does not exist in the current context
    //         if ((int a = 2) > b)
    Diagnostic(ErrorCode.ERR_NameNotInContext, "b").WithArguments("b").WithLocation(60, 27),
    // (61,46): error CS0103: The name 'e' does not exist in the current context
    //             System.Console.WriteLine(a + c + e);
    Diagnostic(ErrorCode.ERR_NameNotInContext, "e").WithArguments("e").WithLocation(61, 46),
    // (61,42): error CS0103: The name 'c' does not exist in the current context
    //             System.Console.WriteLine(a + c + e);
    Diagnostic(ErrorCode.ERR_NameNotInContext, "c").WithArguments("c").WithLocation(61, 42),
    // (62,50): error CS0103: The name 'd' does not exist in the current context
    //         else if ((int b = 2) + (int c = 3) > a + d)
    Diagnostic(ErrorCode.ERR_NameNotInContext, "d").WithArguments("d").WithLocation(62, 50),
    // (70,38): error CS0103: The name 'f' does not exist in the current context
    //             System.Console.WriteLine(f);
    Diagnostic(ErrorCode.ERR_NameNotInContext, "f").WithArguments("f").WithLocation(70, 38),
    // (72,27): error CS0103: The name 'g' does not exist in the current context
    //         if ((int x = 3) > g)
    Diagnostic(ErrorCode.ERR_NameNotInContext, "g").WithArguments("g").WithLocation(72, 27),
    // (79,18): error CS0136: A local or parameter named 'a1' cannot be declared in this scope because that name is used in an enclosing local scope to define a local or parameter
    //         if (bool a1 = true)
    Diagnostic(ErrorCode.ERR_LocalIllegallyOverrides, "a1").WithArguments("a1").WithLocation(79, 18),
    // (88,42): error CS0136: A local or parameter named 'a2' cannot be declared in this scope because that name is used in an enclosing local scope to define a local or parameter
    //             System.Console.WriteLine(int a2 = 1);
    Diagnostic(ErrorCode.ERR_LocalIllegallyOverrides, "a2").WithArguments("a2").WithLocation(88, 42),
    // (90,42): error CS0136: A local or parameter named 'a3' cannot be declared in this scope because that name is used in an enclosing local scope to define a local or parameter
    //             System.Console.WriteLine(int a3 = 1);
    Diagnostic(ErrorCode.ERR_LocalIllegallyOverrides, "a3").WithArguments("a3").WithLocation(90, 42),
    // (99,42): error CS0136: A local or parameter named 'a4' cannot be declared in this scope because that name is used in an enclosing local scope to define a local or parameter
    //             System.Console.WriteLine(int a4 = 1);
    Diagnostic(ErrorCode.ERR_LocalIllegallyOverrides, "a4").WithArguments("a4").WithLocation(99, 42),
    // (103,42): error CS0136: A local or parameter named 'a5' cannot be declared in this scope because that name is used in an enclosing local scope to define a local or parameter
    //             System.Console.WriteLine(int a5 = 1);
    Diagnostic(ErrorCode.ERR_LocalIllegallyOverrides, "a5").WithArguments("a5").WithLocation(103, 42),
    // (106,39): error CS0128: A local variable named 'b1' is already defined in this scope
    //         if ((bool b1 = true) && (bool b1 = true))
    Diagnostic(ErrorCode.ERR_LocalDuplicate, "b1").WithArguments("b1").WithLocation(106, 39),
    // (110,42): error CS0136: A local or parameter named 'b2' cannot be declared in this scope because that name is used in an enclosing local scope to define a local or parameter
    //             System.Console.WriteLine(int b2 = 1);
    Diagnostic(ErrorCode.ERR_LocalIllegallyOverrides, "b2").WithArguments("b2").WithLocation(110, 42),
    // (112,42): error CS0136: A local or parameter named 'b3' cannot be declared in this scope because that name is used in an enclosing local scope to define a local or parameter
    //             System.Console.WriteLine(int b3 = 1);
    Diagnostic(ErrorCode.ERR_LocalIllegallyOverrides, "b3").WithArguments("b3").WithLocation(112, 42),
    // (116,42): error CS0136: A local or parameter named 'b4' cannot be declared in this scope because that name is used in an enclosing local scope to define a local or parameter
    //             System.Console.WriteLine(int b4 = 1);
    Diagnostic(ErrorCode.ERR_LocalIllegallyOverrides, "b4").WithArguments("b4").WithLocation(116, 42),
    // (120,42): error CS0136: A local or parameter named 'b5' cannot be declared in this scope because that name is used in an enclosing local scope to define a local or parameter
    //             System.Console.WriteLine(int b5 = 1);
    Diagnostic(ErrorCode.ERR_LocalIllegallyOverrides, "b5").WithArguments("b5").WithLocation(120, 42),
    // (125,17): error CS0136: A local or parameter named 'b6' cannot be declared in this scope because that name is used in an enclosing local scope to define a local or parameter
    //             int b6 = 1;
    Diagnostic(ErrorCode.ERR_LocalIllegallyOverrides, "b6").WithArguments("b6").WithLocation(125, 17),
    // (130,17): error CS0136: A local or parameter named 'b7' cannot be declared in this scope because that name is used in an enclosing local scope to define a local or parameter
    //             int b7 = 1;
    Diagnostic(ErrorCode.ERR_LocalIllegallyOverrides, "b7").WithArguments("b7").WithLocation(130, 17),
    // (135,58): error CS0128: A local variable named 'c1' is already defined in this scope
    //             System.Console.WriteLine((int c1 = 1) + (int c1 = 1));
    Diagnostic(ErrorCode.ERR_LocalDuplicate, "c1").WithArguments("c1").WithLocation(135, 58),
    // (137,58): error CS0128: A local variable named 'c2' is already defined in this scope
    //             System.Console.WriteLine((int c2 = 1) + (int c2 = 1));
    Diagnostic(ErrorCode.ERR_LocalDuplicate, "c2").WithArguments("c2").WithLocation(137, 58)
            );
        }

        [Fact]
        public void DataFlow_01()
        {
            var text = @"
public class Cls
{
    public static void Main()
    {
        Test(out int y);
        System.Console.WriteLine(y);
    }

    static void Test(out int x)
    {
        x = 123;
    }
}";
            var compilation = CreateCompilationWithMscorlib(text, compOptions: TestOptions.Exe);

            CompileAndVerify(compilation, expectedOutput: @"123").VerifyDiagnostics();
        }

        [Fact]
        public void DataFlow_02()
        {
            var text = @"
public class Cls
{
    public static void Main()
    {
        Test((int y) = 123);
        System.Console.WriteLine(y);
    }

    static void Test(int x)
    {
        System.Console.WriteLine(x);
    }
}";
            var compilation = CreateCompilationWithMscorlib(text, compOptions: TestOptions.Exe);

            CompileAndVerify(compilation, expectedOutput: @"123
123").VerifyDiagnostics();
        }

        [Fact]
        public void DataFlow_03()
        {
            var text = @"
public class Cls
{
    public static void Main()
    {
        Test1(ref int x = 1);
        Test1(ref int y);
        Test1(ref (int z) = 2);
        Test1(ref (int u));
        Test2(int v);
        Test2((int w));
    }

    static void Test1(ref int x)
    {
    }
    static void Test2(int x)
    {
    }
}";
            var compilation = CreateCompilationWithMscorlib(text, compOptions: TestOptions.Exe);

            compilation.VerifyDiagnostics(
    // (8,19): error CS1510: A ref or out argument must be an assignable variable
    //         Test1(ref (int z) = 2);
    Diagnostic(ErrorCode.ERR_RefLvalueExpected, "(int z) = 2").WithLocation(8, 19),
    // (7,19): error CS0165: Use of unassigned local variable 'y'
    //         Test1(ref int y);
    Diagnostic(ErrorCode.ERR_UseDefViolation, "int y").WithArguments("y").WithLocation(7, 19),
    // (9,20): error CS0165: Use of unassigned local variable 'u'
    //         Test1(ref (int u));
    Diagnostic(ErrorCode.ERR_UseDefViolation, "int u").WithArguments("u").WithLocation(9, 20),
    // (10,15): error CS0165: Use of unassigned local variable 'v'
    //         Test2(int v);
    Diagnostic(ErrorCode.ERR_UseDefViolation, "int v").WithArguments("v").WithLocation(10, 15),
    // (11,16): error CS0165: Use of unassigned local variable 'w'
    //         Test2((int w));
    Diagnostic(ErrorCode.ERR_UseDefViolation, "int w").WithArguments("w").WithLocation(11, 16),
    // (8,24): warning CS0219: The variable 'z' is assigned but its value is never used
    //         Test1(ref (int z) = 2);
    Diagnostic(ErrorCode.WRN_UnreferencedVarAssg, "z").WithArguments("z").WithLocation(8, 24)
                );
        }

        [Fact]
        public void OutVar_01()
        {
            var text = @"
public class Cls
{
    public static void Main()
    {
        Test(out var y);
        Print(y);
    }

    static void Test(out int x)
    {
        x = 123;
    }

    static void Print<T>(T val)
    {
        System.Console.WriteLine(val);
        System.Console.WriteLine(typeof(T));
    }
}";
            var compilation = CreateCompilationWithMscorlib(text, compOptions: TestOptions.Exe);

            CompileAndVerify(compilation, expectedOutput: @"123
System.Int32").VerifyDiagnostics();
        }

        [Fact]
        public void OutVar_02()
        {
            var text = @"
public class Cls
{
    public static void Main()
    {
        int x = (var y);
    }
}";
            var compilation = CreateCompilationWithMscorlib(text, compOptions: TestOptions.Exe);

            compilation.VerifyDiagnostics(
    // (6,22): error CS0818: Implicitly-typed variables must be initialized
    //         int x = (var y);
    Diagnostic(ErrorCode.ERR_ImplicitlyTypedVariableWithNoInitializer, "y").WithLocation(6, 22),
    // (6,18): error CS0165: Use of unassigned local variable 'y'
    //         int x = (var y);
    Diagnostic(ErrorCode.ERR_UseDefViolation, "var y").WithArguments("y").WithLocation(6, 18)
                );
        }

        [Fact]
        public void OutVar_03()
        {
            var text = @"
public class Cls
{
    public static void Main()
    {
        int x = (var y) = 1;
    }
}";
            var compilation = CreateCompilationWithMscorlib(text, compOptions: TestOptions.Exe);

            compilation.VerifyDiagnostics(
    // (6,22): error CS0818: Implicitly-typed variables must be initialized
    //         int x = (var y) = 1;
    Diagnostic(ErrorCode.ERR_ImplicitlyTypedVariableWithNoInitializer, "y").WithLocation(6, 22)
                );
        }

        [Fact]
        public void OutVar_04()
        {
            var text = @"
public class Cls
{
    public static void Main()
    {
        Test(var y);
        byte z = y;
    }

    static void Test(out int x)
    {
        x = 123;
    }
}";
            var compilation = CreateCompilationWithMscorlib(text, compOptions: TestOptions.Exe);

            compilation.VerifyDiagnostics(
    // (6,18): error CS0818: Implicitly-typed variables must be initialized
    //         Test(var y);
    Diagnostic(ErrorCode.ERR_ImplicitlyTypedVariableWithNoInitializer, "y").WithLocation(6, 18),
    // (6,14): error CS0165: Use of unassigned local variable 'y'
    //         Test(var y);
    Diagnostic(ErrorCode.ERR_UseDefViolation, "var y").WithArguments("y").WithLocation(6, 14)
                );
        }


        [Fact]
        public void OutVar_05()
        {
            var text = @"
public class Cls
{
    public static void Main()
    {
        Test(ref var y);
        byte z = y;
    }

    static void Test(out int x)
    {
        x = 123;
    }
}";
            var compilation = CreateCompilationWithMscorlib(text, compOptions: TestOptions.Exe);

            compilation.VerifyDiagnostics(
    // (6,18): error CS1620: Argument 1 must be passed with the 'out' keyword
    //         Test(ref var y);
    Diagnostic(ErrorCode.ERR_BadArgRef, "var y").WithArguments("1", "out").WithLocation(6, 18),
    // (7,18): error CS0266: Cannot implicitly convert type 'int' to 'byte'. An explicit conversion exists (are you missing a cast?)
    //         byte z = y;
    Diagnostic(ErrorCode.ERR_NoImplicitConvCast, "y").WithArguments("int", "byte").WithLocation(7, 18),
    // (6,18): error CS0165: Use of unassigned local variable 'y'
    //         Test(ref var y);
    Diagnostic(ErrorCode.ERR_UseDefViolation, "var y").WithArguments("y").WithLocation(6, 18)
                );
        }

        [Fact]
        public void OutVar_06()
        {
            var text = @"
public class Cls
{
    public static void Main()
    {
        Test(out var y);
        byte z = y;
    }

    static void Test(int x)
    {
        x = 123;
    }
}";
            var compilation = CreateCompilationWithMscorlib(text, compOptions: TestOptions.Exe);

            compilation.VerifyDiagnostics(
    // (6,18): error CS1615: Argument 1 should not be passed with the 'out' keyword
    //         Test(out var y);
    Diagnostic(ErrorCode.ERR_BadArgExtraRef, "var y").WithArguments("1", "out").WithLocation(6, 18),
    // (7,18): error CS0266: Cannot implicitly convert type 'int' to 'byte'. An explicit conversion exists (are you missing a cast?)
    //         byte z = y;
    Diagnostic(ErrorCode.ERR_NoImplicitConvCast, "y").WithArguments("int", "byte").WithLocation(7, 18)
                );
        }

        [Fact]
        public void OutVar_07()
        {
            var text = @"
public class Cls
{
    public static void Main()
    {
        Test(out var y);
        byte z = y;
    }

    static void Test(ref int x)
    {
        x = 123;
    }
}";
            var compilation = CreateCompilationWithMscorlib(text, compOptions: TestOptions.Exe);

            compilation.VerifyDiagnostics(
    // (6,18): error CS1620: Argument 1 must be passed with the 'ref' keyword
    //         Test(out var y);
    Diagnostic(ErrorCode.ERR_BadArgRef, "var y").WithArguments("1", "ref").WithLocation(6, 18),
    // (7,18): error CS0266: Cannot implicitly convert type 'int' to 'byte'. An explicit conversion exists (are you missing a cast?)
    //         byte z = y;
    Diagnostic(ErrorCode.ERR_NoImplicitConvCast, "y").WithArguments("int", "byte").WithLocation(7, 18)
                );
        }

        [Fact]
        public void OutVar_08()
        {
            var text = @"
public class C
{
    static void Main()
    {
        M(1, __arglist(var x));
        M(1, __arglist(out var y));
        M(1, __arglist(ref var z));
    }
    
    static void M(int x, __arglist)
    {    
    }
}";

            var compilation = CreateCompilationWithMscorlib(text, compOptions: TestOptions.Exe);

            compilation.VerifyDiagnostics(
    // (6,28): error CS0818: Implicitly-typed variables must be initialized
    //         M(1, __arglist(var x));
    Diagnostic(ErrorCode.ERR_ImplicitlyTypedVariableWithNoInitializer, "x").WithLocation(6, 28),
    // (7,32): error CS0818: Implicitly-typed variables must be initialized
    //         M(1, __arglist(out var y));
    Diagnostic(ErrorCode.ERR_ImplicitlyTypedVariableWithNoInitializer, "y").WithLocation(7, 32),
    // (8,32): error CS0818: Implicitly-typed variables must be initialized
    //         M(1, __arglist(ref var z));
    Diagnostic(ErrorCode.ERR_ImplicitlyTypedVariableWithNoInitializer, "z").WithLocation(8, 32),
    // (6,24): error CS0165: Use of unassigned local variable 'x'
    //         M(1, __arglist(var x));
    Diagnostic(ErrorCode.ERR_UseDefViolation, "var x").WithArguments("x").WithLocation(6, 24),
    // (8,28): error CS0165: Use of unassigned local variable 'z'
    //         M(1, __arglist(ref var z));
    Diagnostic(ErrorCode.ERR_UseDefViolation, "var z").WithArguments("z").WithLocation(8, 28));
        }

        [Fact]
        public void OutVar_09()
        {
            var text = @"
using System;
public struct C
{
    static void Main()
    {
        M(__makeref(var i));
        M(__makeref(out var j));
        M(__makeref(ref var k));
    }
    static Type M(TypedReference tr)
    {
        return __reftype(tr);
    }
}";

            var compilation = CreateCompilationWithMscorlib(text, compOptions: TestOptions.Exe);

            compilation.VerifyDiagnostics(
    // (8,21): error CS1525: Invalid expression term 'out'
    //         M(__makeref(out var j));
    Diagnostic(ErrorCode.ERR_InvalidExprTerm, "out").WithArguments("out").WithLocation(8, 21),
    // (8,21): error CS1026: ) expected
    //         M(__makeref(out var j));
    Diagnostic(ErrorCode.ERR_CloseParenExpected, "out").WithLocation(8, 21),
    // (8,21): error CS1003: Syntax error, ',' expected
    //         M(__makeref(out var j));
    Diagnostic(ErrorCode.ERR_SyntaxError, "out").WithArguments(",", "out").WithLocation(8, 21),
    // (8,31): error CS1002: ; expected
    //         M(__makeref(out var j));
    Diagnostic(ErrorCode.ERR_SemicolonExpected, ")").WithLocation(8, 31),
    // (8,31): error CS1513: } expected
    //         M(__makeref(out var j));
    Diagnostic(ErrorCode.ERR_RbraceExpected, ")").WithLocation(8, 31),
    // (9,21): error CS1525: Invalid expression term 'ref'
    //         M(__makeref(ref var k));
    Diagnostic(ErrorCode.ERR_InvalidExprTerm, "ref").WithArguments("ref").WithLocation(9, 21),
    // (9,21): error CS1026: ) expected
    //         M(__makeref(ref var k));
    Diagnostic(ErrorCode.ERR_CloseParenExpected, "ref").WithLocation(9, 21),
    // (9,21): error CS1003: Syntax error, ',' expected
    //         M(__makeref(ref var k));
    Diagnostic(ErrorCode.ERR_SyntaxError, "ref").WithArguments(",", "ref").WithLocation(9, 21),
    // (9,31): error CS1002: ; expected
    //         M(__makeref(ref var k));
    Diagnostic(ErrorCode.ERR_SemicolonExpected, ")").WithLocation(9, 31),
    // (9,31): error CS1513: } expected
    //         M(__makeref(ref var k));
    Diagnostic(ErrorCode.ERR_RbraceExpected, ")").WithLocation(9, 31),
    // (7,25): error CS0818: Implicitly-typed variables must be initialized
    //         M(__makeref(var i));
    Diagnostic(ErrorCode.ERR_ImplicitlyTypedVariableWithNoInitializer, "i").WithLocation(7, 25),
    // (7,21): error CS0165: Use of unassigned local variable 'i'
    //         M(__makeref(var i));
    Diagnostic(ErrorCode.ERR_UseDefViolation, "var i").WithArguments("i").WithLocation(7, 21),
    // (9,25): error CS0165: Use of unassigned local variable 'k'
    //         M(__makeref(ref var k));
    Diagnostic(ErrorCode.ERR_UseDefViolation, "var k").WithArguments("k").WithLocation(9, 25)
                );
        }

        [Fact]
        public void OutVar_10()
        {
            var text = @"
using System;
public struct C
{
    static void Main()
    {
    }
}

[MyAttribute(out var a)] class Test1
{}

[MyAttribute(ref var b)] class Test2
{}

[MyAttribute(var c)] class Test3
{}

public class MyAttribute : Attribute
{
    public MyAttribute(out int x)
    {
        x = 0;
    }
}
";

            var compilation = CreateCompilationWithMscorlib(text, compOptions: TestOptions.Dll);

            compilation.VerifyDiagnostics(
    // (10,14): error CS1041: Identifier expected; 'out' is a keyword
    // [MyAttribute(out var a)] class Test1
    Diagnostic(ErrorCode.ERR_IdentifierExpectedKW, "out").WithArguments("", "out").WithLocation(10, 14),
    // (13,14): error CS1041: Identifier expected; 'ref' is a keyword
    // [MyAttribute(ref var b)] class Test2
    Diagnostic(ErrorCode.ERR_IdentifierExpectedKW, "ref").WithArguments("", "ref").WithLocation(13, 14),
    // (13,18): error CS8028: A declaration expression is not permitted in a variable-initializer of a field declaration, or in a class-base specification.
    // [MyAttribute(ref var b)] class Test2
    Diagnostic(ErrorCode.ERR_DeclarationExpressionOutsideOfAMethodBody, "var b").WithLocation(13, 18),
    // (13,22): error CS0818: Implicitly-typed variables must be initialized
    // [MyAttribute(ref var b)] class Test2
    Diagnostic(ErrorCode.ERR_ImplicitlyTypedVariableWithNoInitializer, "b").WithLocation(13, 22),
    // (10,18): error CS8028: A declaration expression is not permitted in a variable-initializer of a field declaration, or in a class-base specification.
    // [MyAttribute(out var a)] class Test1
    Diagnostic(ErrorCode.ERR_DeclarationExpressionOutsideOfAMethodBody, "var a").WithLocation(10, 18),
    // (10,22): error CS0818: Implicitly-typed variables must be initialized
    // [MyAttribute(out var a)] class Test1
    Diagnostic(ErrorCode.ERR_ImplicitlyTypedVariableWithNoInitializer, "a").WithLocation(10, 22),
    // (16,14): error CS8028: A declaration expression is not permitted in a variable-initializer of a field declaration, or in a class-base specification.
    // [MyAttribute(var c)] class Test3
    Diagnostic(ErrorCode.ERR_DeclarationExpressionOutsideOfAMethodBody, "var c").WithLocation(16, 14),
    // (16,18): error CS0818: Implicitly-typed variables must be initialized
    // [MyAttribute(var c)] class Test3
    Diagnostic(ErrorCode.ERR_ImplicitlyTypedVariableWithNoInitializer, "c").WithLocation(16, 18)
            );
        }

        [Fact]
        public void OutVar_11()
        {
            var text = @"
using System;
public struct C
{
    static void Main()
    {
    }
}

[MyAttribute(out var a)] class Test1
{}

[MyAttribute(ref var b)] class Test2
{}

[MyAttribute(var c)] class Test3
{}

public class MyAttribute : Attribute
{
    public MyAttribute(ref int x)
    {}
}
";

            var compilation = CreateCompilationWithMscorlib(text, compOptions: TestOptions.Dll);

            compilation.VerifyDiagnostics(
    // (10,14): error CS1041: Identifier expected; 'out' is a keyword
    // [MyAttribute(out var a)] class Test1
    Diagnostic(ErrorCode.ERR_IdentifierExpectedKW, "out").WithArguments("", "out").WithLocation(10, 14),
    // (13,14): error CS1041: Identifier expected; 'ref' is a keyword
    // [MyAttribute(ref var b)] class Test2
    Diagnostic(ErrorCode.ERR_IdentifierExpectedKW, "ref").WithArguments("", "ref").WithLocation(13, 14),
    // (13,18): error CS8028: A declaration expression is not permitted in a variable-initializer of a field declaration, or in a class-base specification.
    // [MyAttribute(ref var b)] class Test2
    Diagnostic(ErrorCode.ERR_DeclarationExpressionOutsideOfAMethodBody, "var b").WithLocation(13, 18),
    // (13,22): error CS0818: Implicitly-typed variables must be initialized
    // [MyAttribute(ref var b)] class Test2
    Diagnostic(ErrorCode.ERR_ImplicitlyTypedVariableWithNoInitializer, "b").WithLocation(13, 22),
    // (10,18): error CS8028: A declaration expression is not permitted in a variable-initializer of a field declaration, or in a class-base specification.
    // [MyAttribute(out var a)] class Test1
    Diagnostic(ErrorCode.ERR_DeclarationExpressionOutsideOfAMethodBody, "var a").WithLocation(10, 18),
    // (10,22): error CS0818: Implicitly-typed variables must be initialized
    // [MyAttribute(out var a)] class Test1
    Diagnostic(ErrorCode.ERR_ImplicitlyTypedVariableWithNoInitializer, "a").WithLocation(10, 22),
    // (16,14): error CS8028: A declaration expression is not permitted in a variable-initializer of a field declaration, or in a class-base specification.
    // [MyAttribute(var c)] class Test3
    Diagnostic(ErrorCode.ERR_DeclarationExpressionOutsideOfAMethodBody, "var c").WithLocation(16, 14),
    // (16,18): error CS0818: Implicitly-typed variables must be initialized
    // [MyAttribute(var c)] class Test3
    Diagnostic(ErrorCode.ERR_ImplicitlyTypedVariableWithNoInitializer, "c").WithLocation(16, 18)
                );
        }

        [Fact]
        public void OutVar_12()
        {
            var text = @"
using System;
public struct C
{
    static void Main()
    {
    }
}

[MyAttribute(out var a)] class Test1
{}

[MyAttribute(ref var b)] class Test2
{}

[MyAttribute(var c)] class Test3
{}

public class MyAttribute : Attribute
{
    public MyAttribute(int x)
    {}
}
";

            var compilation = CreateCompilationWithMscorlib(text, compOptions: TestOptions.Dll);

            compilation.VerifyDiagnostics(
    // (10,14): error CS1041: Identifier expected; 'out' is a keyword
    // [MyAttribute(out var a)] class Test1
    Diagnostic(ErrorCode.ERR_IdentifierExpectedKW, "out").WithArguments("", "out").WithLocation(10, 14),
    // (13,14): error CS1041: Identifier expected; 'ref' is a keyword
    // [MyAttribute(ref var b)] class Test2
    Diagnostic(ErrorCode.ERR_IdentifierExpectedKW, "ref").WithArguments("", "ref").WithLocation(13, 14),
    // (13,18): error CS8028: A declaration expression is not permitted in a variable-initializer of a field declaration, or in a class-base specification.
    // [MyAttribute(ref var b)] class Test2
    Diagnostic(ErrorCode.ERR_DeclarationExpressionOutsideOfAMethodBody, "var b").WithLocation(13, 18),
    // (13,22): error CS0818: Implicitly-typed variables must be initialized
    // [MyAttribute(ref var b)] class Test2
    Diagnostic(ErrorCode.ERR_ImplicitlyTypedVariableWithNoInitializer, "b").WithLocation(13, 22),
    // (10,18): error CS8028: A declaration expression is not permitted in a variable-initializer of a field declaration, or in a class-base specification.
    // [MyAttribute(out var a)] class Test1
    Diagnostic(ErrorCode.ERR_DeclarationExpressionOutsideOfAMethodBody, "var a").WithLocation(10, 18),
    // (10,22): error CS0818: Implicitly-typed variables must be initialized
    // [MyAttribute(out var a)] class Test1
    Diagnostic(ErrorCode.ERR_ImplicitlyTypedVariableWithNoInitializer, "a").WithLocation(10, 22),
    // (16,14): error CS8028: A declaration expression is not permitted in a variable-initializer of a field declaration, or in a class-base specification.
    // [MyAttribute(var c)] class Test3
    Diagnostic(ErrorCode.ERR_DeclarationExpressionOutsideOfAMethodBody, "var c").WithLocation(16, 14),
    // (16,18): error CS0818: Implicitly-typed variables must be initialized
    // [MyAttribute(var c)] class Test3
    Diagnostic(ErrorCode.ERR_ImplicitlyTypedVariableWithNoInitializer, "c").WithLocation(16, 18)
                );
        }

        [Fact]
        public void OutVar_13()
        {
            var text = @"
public class Cls
{
    public static void Main(dynamic target)
    {
        target.Test(out var y);
    }
}";
            var compilation = CreateCompilationWithMscorlib(text, references: new[] { CSharpRef, SystemCoreRef }, compOptions: TestOptions.Dll);

            compilation.VerifyDiagnostics(
    // (6,29): error CS0818: Implicitly-typed variables must be initialized
    //         target.Test(out var y);
    Diagnostic(ErrorCode.ERR_ImplicitlyTypedVariableWithNoInitializer, "y").WithLocation(6, 29)
                );
        }

        [Fact]
        public void OutVar_14()
        {
            var text = @"
public class Cls
{
    public static void Main(dynamic target)
    {
        target.Test(ref var y);
    }
}";
            var compilation = CreateCompilationWithMscorlib(text, references: new[] { CSharpRef, SystemCoreRef }, compOptions: TestOptions.Dll);

            compilation.VerifyDiagnostics(
    // (6,29): error CS0818: Implicitly-typed variables must be initialized
    //         target.Test(ref var y);
    Diagnostic(ErrorCode.ERR_ImplicitlyTypedVariableWithNoInitializer, "y").WithLocation(6, 29),
    // (6,25): error CS0165: Use of unassigned local variable 'y'
    //         target.Test(ref var y);
    Diagnostic(ErrorCode.ERR_UseDefViolation, "var y").WithArguments("y").WithLocation(6, 25)
                );
        }

        [Fact]
        public void OutVar_15()
        {
            var text = @"
public class Cls
{
    public static void Main(dynamic target)
    {
        target.Test(var y);
    }
}";
            var compilation = CreateCompilationWithMscorlib(text, references: new[] { CSharpRef, SystemCoreRef }, compOptions: TestOptions.Dll);

            compilation.VerifyDiagnostics(
    // (6,25): error CS0818: Implicitly-typed variables must be initialized
    //         target.Test(var y);
    Diagnostic(ErrorCode.ERR_ImplicitlyTypedVariableWithNoInitializer, "y").WithLocation(6, 25),
    // (6,21): error CS0165: Use of unassigned local variable 'y'
    //         target.Test(var y);
    Diagnostic(ErrorCode.ERR_UseDefViolation, "var y").WithArguments("y").WithLocation(6, 21)
                );
        }

        [Fact]
        public void OutVar_16()
        {
            var text = @"
public class Cls
{
    public static void Main()
    {
        Test(ref var y);
        byte z = y;
    }

    static void Test(ref int x)
    {
        x = 123;
    }
}";
            var compilation = CreateCompilationWithMscorlib(text, compOptions: TestOptions.Exe);

            compilation.VerifyDiagnostics(
    // (7,18): error CS0266: Cannot implicitly convert type 'int' to 'byte'. An explicit conversion exists (are you missing a cast?)
    //         byte z = y;
    Diagnostic(ErrorCode.ERR_NoImplicitConvCast, "y").WithArguments("int", "byte").WithLocation(7, 18),
    // (6,18): error CS0165: Use of unassigned local variable 'y'
    //         Test(ref var y);
    Diagnostic(ErrorCode.ERR_UseDefViolation, "var y").WithArguments("y").WithLocation(6, 18)
                );
        }

        [Fact]
        public void OutVar_17()
        {
            var text = @"
public class Cls
{
    public static void Main()
    {
        Test(ref var y);
        byte z = y;
    }

    static void Test(int x)
    {
        x = 123;
    }
}";
            var compilation = CreateCompilationWithMscorlib(text, compOptions: TestOptions.Exe);

            compilation.VerifyDiagnostics(
    // (6,18): error CS1615: Argument 1 should not be passed with the 'ref' keyword
    //         Test(ref var y);
    Diagnostic(ErrorCode.ERR_BadArgExtraRef, "var y").WithArguments("1", "ref").WithLocation(6, 18),
    // (7,18): error CS0266: Cannot implicitly convert type 'int' to 'byte'. An explicit conversion exists (are you missing a cast?)
    //         byte z = y;
    Diagnostic(ErrorCode.ERR_NoImplicitConvCast, "y").WithArguments("int", "byte").WithLocation(7, 18),
    // (6,18): error CS0165: Use of unassigned local variable 'y'
    //         Test(ref var y);
    Diagnostic(ErrorCode.ERR_UseDefViolation, "var y").WithArguments("y").WithLocation(6, 18)
                );
        }

        [Fact]
        public void OutVar_18()
        {
            var text = @"
public class Cls
{
    public static void Main()
    {
        Test(var y);
        byte z = y;
    }

    static void Test(ref int x)
    {
        x = 123;
    }
}";
            var compilation = CreateCompilationWithMscorlib(text, compOptions: TestOptions.Exe);

            compilation.VerifyDiagnostics(
    // (6,18): error CS0818: Implicitly-typed variables must be initialized
    //         Test(var y);
    Diagnostic(ErrorCode.ERR_ImplicitlyTypedVariableWithNoInitializer, "y").WithLocation(6, 18),
    // (6,14): error CS0165: Use of unassigned local variable 'y'
    //         Test(var y);
    Diagnostic(ErrorCode.ERR_UseDefViolation, "var y").WithArguments("y").WithLocation(6, 14)
                );
        }

        [Fact]
        public void OutVar_19()
        {
            var text = @"
public class Cls
{
    public static void Main()
    {
        Test(var y);
        byte z = y;
    }

    static void Test(int x)
    {
        x = 123;
    }
}";
            var compilation = CreateCompilationWithMscorlib(text, compOptions: TestOptions.Exe);

            compilation.VerifyDiagnostics(
    // (6,18): error CS0818: Implicitly-typed variables must be initialized
    //         Test(var y);
    Diagnostic(ErrorCode.ERR_ImplicitlyTypedVariableWithNoInitializer, "y").WithLocation(6, 18),
    // (6,14): error CS0165: Use of unassigned local variable 'y'
    //         Test(var y);
    Diagnostic(ErrorCode.ERR_UseDefViolation, "var y").WithArguments("y").WithLocation(6, 14)
                );
        }

        [Fact]
        public void OutVar_20()
        {
            var text = @"
public class Cls
{
    public static void Main()
    {
        Test(out var y);
        byte z = y;
    }

    static void Test(int x)
    {}

    static void Test(string x)
    {}
}";
            var compilation = CreateCompilationWithMscorlib(text, compOptions: TestOptions.Exe);

            compilation.VerifyDiagnostics(
    // (6,18): error CS1615: Argument 1 should not be passed with the 'out' keyword
    //         Test(out var y);
    Diagnostic(ErrorCode.ERR_BadArgExtraRef, "var y").WithArguments("1", "out").WithLocation(6, 18)
                );
        }

        [Fact]
        public void OutVar_21()
        {
            var text = @"
public class Cls
{
    public static void Main()
    {
        Test(out var y);
        byte z = y;
    }

    static void Test(out int x)
    {
        x = 0;
    }

    static void Test(out string x)
    {
        x = null;
    }
}";
            var compilation = CreateCompilationWithMscorlib(text, compOptions: TestOptions.Exe);

            compilation.VerifyDiagnostics(
    // (6,9): error CS0121: The call is ambiguous between the following methods or properties: 'Cls.Test(out int)' and 'Cls.Test(out string)'
    //         Test(out var y);
    Diagnostic(ErrorCode.ERR_AmbigCall, "Test").WithArguments("Cls.Test(out int)", "Cls.Test(out string)").WithLocation(6, 9)
                );
        }

        [Fact]
        public void OutVar_22()
        {
            var text = @"
using System;

public class Cls
{
    public static void Main()
    {
        var x = new Action(out var y);
    }
}";
            var compilation = CreateCompilationWithMscorlib(text, compOptions: TestOptions.Exe);

            compilation.VerifyDiagnostics(
    // (8,32): error CS0149: Method name expected
    //         var x = new Action(out var y);
    Diagnostic(ErrorCode.ERR_MethodNameExpected, "var y").WithLocation(8, 32),
    // (8,32): error CS0165: Use of unassigned local variable 'y'
    //         var x = new Action(out var y);
    Diagnostic(ErrorCode.ERR_UseDefViolation, "var y").WithArguments("y").WithLocation(8, 32)
                );
        }

        [Fact]
        public void OutVar_23()
        {
            var text = @"
using System;

public class Cls
{
    public static void Main()
    {
        var x = new Action(Main, out var y);
    }
}";
            var compilation = CreateCompilationWithMscorlib(text, compOptions: TestOptions.Exe);

            compilation.VerifyDiagnostics(
    // (8,28): error CS0149: Method name expected
    //         var x = new Action(Main, out var y);
    Diagnostic(ErrorCode.ERR_MethodNameExpected, "Main, out var y").WithLocation(8, 28),
    // (8,38): error CS0165: Use of unassigned local variable 'y'
    //         var x = new Action(Main, out var y);
    Diagnostic(ErrorCode.ERR_UseDefViolation, "var y").WithArguments("y").WithLocation(8, 38)
                );
        }

        [Fact]
        public void OutVar_24()
        {
            var text = @"
public class Cls
{
    public static void Main(dynamic target)
    {
        var x = new Test(target, out var y);
    }
}

class Test
{
    public Test(int x, out int y)
    {
        y = 0;
    }

    public Test(uint x, out int y)
    {
        y = 1;
    }
}
";
            var compilation = CreateCompilationWithMscorlib(text, references: new[] { CSharpRef, SystemCoreRef }, compOptions: TestOptions.Dll);

            compilation.VerifyDiagnostics(
    // (6,42): error CS0818: Implicitly-typed variables must be initialized
    //         var x = new Test(target, out var y);
    Diagnostic(ErrorCode.ERR_ImplicitlyTypedVariableWithNoInitializer, "y").WithLocation(6, 42)
                );
        }

        [Fact]
        public void OutVar_25()
        {
            var text = @"
public class Cls
{
    public static void Main(int [,] target)
    {
        target[var y] = 0;
    }
}";
            var compilation = CreateCompilationWithMscorlib(text, compOptions: TestOptions.Dll);

            compilation.VerifyDiagnostics(
    // (6,20): error CS0818: Implicitly-typed variables must be initialized
    //         target[var y] = 0;
    Diagnostic(ErrorCode.ERR_ImplicitlyTypedVariableWithNoInitializer, "y").WithLocation(6, 20),
    // (6,16): error CS0165: Use of unassigned local variable 'y'
    //         target[var y] = 0;
    Diagnostic(ErrorCode.ERR_UseDefViolation, "var y").WithArguments("y").WithLocation(6, 16)
                );
        }

        [Fact]
        public void OutVar_26()
        {
            var text = @"
public class Cls
{
    public static void Main(int [] target)
    {
        target[var y] = 0;
    }
}";
            var compilation = CreateCompilationWithMscorlib(text, compOptions: TestOptions.Dll);

            compilation.VerifyDiagnostics(
    // (6,20): error CS0818: Implicitly-typed variables must be initialized
    //         target[var y] = 0;
    Diagnostic(ErrorCode.ERR_ImplicitlyTypedVariableWithNoInitializer, "y").WithLocation(6, 20),
    // (6,16): error CS0165: Use of unassigned local variable 'y'
    //         target[var y] = 0;
    Diagnostic(ErrorCode.ERR_UseDefViolation, "var y").WithArguments("y").WithLocation(6, 16)
                );
        }

        [Fact]
        public void OutVar_27()
        {
            var text = @"
public class Cls
{
    public static void Main(int [] target)
    {
        target[out var y] = 0;
    }
}";
            var compilation = CreateCompilationWithMscorlib(text, compOptions: TestOptions.Dll);

            compilation.VerifyDiagnostics(
    // (6,24): error CS0818: Implicitly-typed variables must be initialized
    //         target[out var y] = 0;
    Diagnostic(ErrorCode.ERR_ImplicitlyTypedVariableWithNoInitializer, "y").WithLocation(6, 24),
    // (6,20): error CS0165: Use of unassigned local variable 'y'
    //         target[out var y] = 0;
    Diagnostic(ErrorCode.ERR_UseDefViolation, "var y").WithArguments("y").WithLocation(6, 20)
                );
        }

        [Fact]
        public void OutVar_28()
        {
            var text = @"
public class Cls
{
    public static void Main(int [,] target)
    {
        target[out var y] = 0;
    }
}";
            var compilation = CreateCompilationWithMscorlib(text, compOptions: TestOptions.Dll);

            compilation.VerifyDiagnostics(
    // (6,9): error CS0022: Wrong number of indices inside []; expected 2
    //         target[out var y] = 0;
    Diagnostic(ErrorCode.ERR_BadIndexCount, "target[out var y]").WithArguments("2").WithLocation(6, 9),
    // (6,20): error CS0165: Use of unassigned local variable 'y'
    //         target[out var y] = 0;
    Diagnostic(ErrorCode.ERR_UseDefViolation, "var y").WithArguments("y").WithLocation(6, 20)
                );
        }


        [Fact]
        public void OutVar_29()
        {
            var text = @"
public class Cls
{
    public unsafe static void Main(int* target)
    {
        target[var y, 3] = 0;
    }
}";
            var compilation = CreateCompilationWithMscorlib(text, compOptions: TestOptions.Dll.WithAllowUnsafe(true));

            compilation.VerifyDiagnostics(
    // (6,20): error CS0818: Implicitly-typed variables must be initialized
    //         target[var y, 3] = 0;
    Diagnostic(ErrorCode.ERR_ImplicitlyTypedVariableWithNoInitializer, "y").WithLocation(6, 20),
    // (6,16): error CS0165: Use of unassigned local variable 'y'
    //         target[var y, 3] = 0;
    Diagnostic(ErrorCode.ERR_UseDefViolation, "var y").WithArguments("y").WithLocation(6, 16)
                );
        }

        [Fact]
        public void OutVar_30()
        {
            var text = @"
public class Cls
{
    public unsafe static void Main(int* target)
    {
        target[var y] = 0;
    }
}";
            var compilation = CreateCompilationWithMscorlib(text, compOptions: TestOptions.Dll.WithAllowUnsafe(true));

            compilation.VerifyDiagnostics(
    // (6,20): error CS0818: Implicitly-typed variables must be initialized
    //         target[var y] = 0;
    Diagnostic(ErrorCode.ERR_ImplicitlyTypedVariableWithNoInitializer, "y").WithLocation(6, 20),
    // (6,16): error CS0165: Use of unassigned local variable 'y'
    //         target[var y] = 0;
    Diagnostic(ErrorCode.ERR_UseDefViolation, "var y").WithArguments("y").WithLocation(6, 16)
                );
        }

        [Fact]
        public void OutVar_31()
        {
            var text = @"
public class Cls
{
    public unsafe static void Main(int * target)
    {
        target[out var y] = 0;
    }
}";
            var compilation = CreateCompilationWithMscorlib(text, compOptions: TestOptions.Dll.WithAllowUnsafe(true));

            compilation.VerifyDiagnostics(
    // (6,20): error CS1615: Argument 1 should not be passed with the 'out' keyword
    //         target[out var y] = 0;
    Diagnostic(ErrorCode.ERR_BadArgExtraRef, "var y").WithArguments("1", "out").WithLocation(6, 20),
    // (6,24): error CS0818: Implicitly-typed variables must be initialized
    //         target[out var y] = 0;
    Diagnostic(ErrorCode.ERR_ImplicitlyTypedVariableWithNoInitializer, "y").WithLocation(6, 24),
    // (6,20): error CS0165: Use of unassigned local variable 'y'
    //         target[out var y] = 0;
    Diagnostic(ErrorCode.ERR_UseDefViolation, "var y").WithArguments("y").WithLocation(6, 20)
                );
        }

        [Fact]
        public void OutVar_32()
        {
            var text = @"
public class Cls
{
    public unsafe static void Main(int * target)
    {
        target[out var y, 1] = 0;
    }
}";
            var compilation = CreateCompilationWithMscorlib(text, compOptions: TestOptions.Dll.WithAllowUnsafe(true));

            compilation.VerifyDiagnostics(
    // (6,20): error CS1615: Argument 1 should not be passed with the 'out' keyword
    //         target[out var y, 1] = 0;
    Diagnostic(ErrorCode.ERR_BadArgExtraRef, "var y").WithArguments("1", "out").WithLocation(6, 20),
    // (6,20): error CS0165: Use of unassigned local variable 'y'
    //         target[out var y, 1] = 0;
    Diagnostic(ErrorCode.ERR_UseDefViolation, "var y").WithArguments("y").WithLocation(6, 20)
                );
        }

        [Fact]
        public void OutVar_33()
        {
            var text = @"
public class Cls
{
    public unsafe static void Main(int* target)
    {
        target[4, var y] = 0;
    }
}";
            var compilation = CreateCompilationWithMscorlib(text, compOptions: TestOptions.Dll.WithAllowUnsafe(true));

            compilation.VerifyDiagnostics(
    // (6,23): error CS0818: Implicitly-typed variables must be initialized
    //         target[4, var y] = 0;
    Diagnostic(ErrorCode.ERR_ImplicitlyTypedVariableWithNoInitializer, "y").WithLocation(6, 23),
    // (6,19): error CS0165: Use of unassigned local variable 'y'
    //         target[4, var y] = 0;
    Diagnostic(ErrorCode.ERR_UseDefViolation, "var y").WithArguments("y").WithLocation(6, 19)
                );
        }

        [Fact]
        public void OutVar_34()
        {
            var text = @"
public class Cls
{
    public unsafe static void Main(int * target)
    {
        target[5, out var y] = 0;
    }
}";
            var compilation = CreateCompilationWithMscorlib(text, compOptions: TestOptions.Dll.WithAllowUnsafe(true));

            compilation.VerifyDiagnostics(
    // (6,23): error CS1615: Argument 2 should not be passed with the 'out' keyword
    //         target[5, out var y] = 0;
    Diagnostic(ErrorCode.ERR_BadArgExtraRef, "var y").WithArguments("2", "out").WithLocation(6, 23),
    // (6,23): error CS0165: Use of unassigned local variable 'y'
    //         target[5, out var y] = 0;
    Diagnostic(ErrorCode.ERR_UseDefViolation, "var y").WithArguments("y").WithLocation(6, 23)
                );
        }

        [Fact]
        public void OutVar_35()
        {
            var text = @"
public class Cls
{
    public static void Main(dynamic target)
    {
        var x = target[out var y];
    }
}";
            var compilation = CreateCompilationWithMscorlib(text, references: new[] { CSharpRef, SystemCoreRef }, compOptions: TestOptions.Dll);

            compilation.VerifyDiagnostics(
    // (6,32): error CS0818: Implicitly-typed variables must be initialized
    //         var x = target[out var y];
    Diagnostic(ErrorCode.ERR_ImplicitlyTypedVariableWithNoInitializer, "y").WithLocation(6, 32)
                );
        }

        [Fact]
        public void OutVar_36()
        {
            var text = @"
public class Cls
{
    public static void Main(dynamic target)
    {
        var x = target[var y];
    }
}";
            var compilation = CreateCompilationWithMscorlib(text, references: new[] { CSharpRef, SystemCoreRef }, compOptions: TestOptions.Dll);

            compilation.VerifyDiagnostics(
    // (6,28): error CS0818: Implicitly-typed variables must be initialized
    //         var x = target[var y];
    Diagnostic(ErrorCode.ERR_ImplicitlyTypedVariableWithNoInitializer, "y").WithLocation(6, 28),
    // (6,24): error CS0165: Use of unassigned local variable 'y'
    //         var x = target[var y];
    Diagnostic(ErrorCode.ERR_UseDefViolation, "var y").WithArguments("y").WithLocation(6, 24)
                );
        }

        [Fact]
        public void OutVar_37()
        {
            var text = @"
public class Cls
{
    public static void Main()
    {
        Test(out var y);
    }

    static void Test(out int x)
    {
        x = 123;
    }

    static void Test(out uint x)
    {
        x = 456;
    }
}";
            var compilation = CreateCompilationWithMscorlib(text, compOptions: TestOptions.Exe);

            compilation.VerifyDiagnostics(
    // (6,9): error CS0121: The call is ambiguous between the following methods or properties: 'Cls.Test(out int)' and 'Cls.Test(out uint)'
    //         Test(out var y);
    Diagnostic(ErrorCode.ERR_AmbigCall, "Test").WithArguments("Cls.Test(out int)", "Cls.Test(out uint)").WithLocation(6, 9)
                );
        }

        [Fact]
        public void OutVar_38()
        {
            var text = @"
public class Cls
{
    public static void Main()
    {
        Test(out var y);
    }

    static void Test<T>(out T x)
    {
        x = default(T);
    }
}";
            var compilation = CreateCompilationWithMscorlib(text, compOptions: TestOptions.Exe);

            compilation.VerifyDiagnostics(
    // (6,9): error CS0411: The type arguments for method 'Cls.Test<T>(out T)' cannot be inferred from the usage. Try specifying the type arguments explicitly.
    //         Test(out var y);
    Diagnostic(ErrorCode.ERR_CantInferMethTypeArgs, "Test").WithArguments("Cls.Test<T>(out T)").WithLocation(6, 9)
                );
        }

        [Fact]
        public void OutVar_39()
        {
            var text = @"
public class Cls
{
    public static void Main()
    {
        Test(out var y, 1);
        Print(y);
    }

    static void Test<T>(out T x, T y)
    {
        x = default(T);
    }

    static void Print<T>(T val)
    {
        System.Console.WriteLine(typeof(T));
    }
}";
            var compilation = CreateCompilationWithMscorlib(text, compOptions: TestOptions.Exe);

            CompileAndVerify(compilation, expectedOutput: @"System.Int32").VerifyDiagnostics();
        }

        [Fact]
        public void OutVar_40()
        {
            var text = @"
public class Cls
{
    public static void Main()
    {
        Test(null, out var y);
    }

    static void Test(A a, out int x)
    {
        x = 123;
    }

    static void Test(B b, out int x)
    {
        x = 456;
    }
}

class A{}
class B{}
";
            var compilation = CreateCompilationWithMscorlib(text, compOptions: TestOptions.Exe);

            compilation.VerifyDiagnostics(
    // (6,9): error CS0121: The call is ambiguous between the following methods or properties: 'Cls.Test(A, out int)' and 'Cls.Test(B, out int)'
    //         Test(null, out var y);
    Diagnostic(ErrorCode.ERR_AmbigCall, "Test").WithArguments("Cls.Test(A, out int)", "Cls.Test(B, out int)").WithLocation(6, 9)
                );
        }

        [Fact]
        public void OutVar_41()
        {
            var text = @"
public class Cls
{
    public static void Main()
    {
        Test(out var y, y + 1);
    }

    static void Test(out int x, int y)
    {
        x = 123;
    }
}";
            var compilation = CreateCompilationWithMscorlib(text, compOptions: TestOptions.Exe);

            compilation.VerifyDiagnostics(
    // (6,25): error CS8029: Reference to variable 'y' is not permitted in this context.
    //         Test(out var y, y + 1);
    Diagnostic(ErrorCode.ERR_VariableUsedInTheSameArgumentList, "y").WithArguments("y").WithLocation(6, 25),
    // (6,25): error CS0165: Use of unassigned local variable 'y'
    //         Test(out var y, y + 1);
    Diagnostic(ErrorCode.ERR_UseDefViolation, "y").WithArguments("y").WithLocation(6, 25)
                );
        }

        [Fact]
        public void OutVar_42()
        {
            var text = @"
public class Cls
{
    public static void Main()
    {
        Test(y + 1, out var y);
    }

    static void Test(int y, out int x)
    {
        x = 123;
    }
}";
            var compilation = CreateCompilationWithMscorlib(text, compOptions: TestOptions.Exe);

            compilation.VerifyDiagnostics(
    // (6,14): error CS0841: Cannot use local variable 'y' before it is declared
    //         Test(y + 1, out var y);
    Diagnostic(ErrorCode.ERR_VariableUsedBeforeDeclaration, "y").WithArguments("y").WithLocation(6, 14)
                );
        }

        [Fact]
        public void OutVar_43()
        {
            var text = @"
public class Cls
{
    public static void Main()
    {
        Test2(Test1(out var y), y);
    }

    static int Test1(out int x)
    {
        x = 123;
        return x + 1;
    }

    static void Test2(int x, int y)
    {
        System.Console.WriteLine(x);
        System.Console.WriteLine(y);
    }
}";
            var compilation = CreateCompilationWithMscorlib(text, compOptions: TestOptions.Exe);

            CompileAndVerify(compilation, expectedOutput: @"124
123").VerifyDiagnostics();
        }

        [Fact]
        public void OutVar_44()
        {
            var text = @"
public class Cls
{
    public static void Main()
    {
        Test2(y, Test1(out var y));
    }

    static int Test1(out int x)
    {
        x = 123;
        return x + 1;
    }

    static void Test2(int x, int y)
    {
        System.Console.WriteLine(x);
        System.Console.WriteLine(y);
    }
}";
            var compilation = CreateCompilationWithMscorlib(text, compOptions: TestOptions.Exe);

            compilation.VerifyDiagnostics(
    // (6,15): error CS0841: Cannot use local variable 'y' before it is declared
    //         Test2(y, Test1(out var y));
    Diagnostic(ErrorCode.ERR_VariableUsedBeforeDeclaration, "y").WithArguments("y").WithLocation(6, 15)
                );
        }

        [Fact]
        public void CatchFilter_01()
        {
            var text = @"
public class Cls
{
    public static void Main()
    {
        try
        {
            throw new System.NullReferenceException();
        }
        catch (System.Exception e) if ((int j = e is System.NullReferenceException ? 1 : 2) + j == 2)
        {
            System.Console.WriteLine(j);
        }

        try
        {
            throw new System.NullReferenceException();
        }
        catch (System.Exception e) if ((int j = e is System.NullReferenceException ? 3 : 2) + j == 6)
        {
            System.Func<object> l = () => j;
            System.Console.WriteLine(l());
        }

        try
        {
            throw new System.NullReferenceException();
        }
        catch (System.Exception e) if ((int j = e is System.NullReferenceException ? 5 : 2) + j == 10)
        {
            System.Func<object> l = () => e.GetType();
            System.Console.WriteLine(l());
        }

        try
        {
            throw new System.NullReferenceException();
        }
        catch (System.Exception e) if ((int j = e is System.NullReferenceException ? 7 : 2) + j == 14)
        {
            System.Func<object> l1 = () => j;
            System.Func<object> l2 = () => e.GetType();
            System.Console.WriteLine(l1());
            System.Console.WriteLine(l2());
        }

        try
        {
            throw new System.NullReferenceException();
        }
        catch (System.NullReferenceException e) if ((int j = 9) == 9)
        {
            System.Console.WriteLine(j);
        }

        try
        {
            throw new System.NullReferenceException();
        }
        catch (System.NullReferenceException) if ((int j = 11) == 11)
        {
            System.Console.WriteLine(j);
        }

        try
        {
            throw new System.NullReferenceException();
        }
        catch (System.NullReferenceException) if ((int j = 13) == 13)
        {
            System.Func<object> l = () => j;
            System.Console.WriteLine(l());
        }
    }
}";
            var compilation = CreateCompilationWithMscorlib(text, compOptions: TestOptions.Exe);

            CompileAndVerify(compilation, expectedOutput: @"1
3
System.NullReferenceException
7
System.NullReferenceException
9
11
13").VerifyDiagnostics(
    // (51,46): warning CS0168: The variable 'e' is declared but never used
    //         catch (System.NullReferenceException e) if ((int j = 9) == 9)
    Diagnostic(ErrorCode.WRN_UnreferencedVar, "e").WithArguments("e").WithLocation(51, 46)
                );
        }

        [Fact]
        public void CatchFilter_02()
        {
            var text = @"
public class Cls
{
    public static void Main()
    {
        try
        {
            throw new System.NullReferenceException();
        }
        catch (System.NullReferenceException e) if (bool a = false)
        {
            System.Console.WriteLine(e);
        }

        System.Console.WriteLine(a);

        try
        {
            throw new System.NullReferenceException();
        }
        catch (System.NullReferenceException e) if (bool b = false)
        {
            System.Console.WriteLine(e);
        }
        catch (System.Exception e) if (b)
        {
            System.Console.WriteLine(e);
        }

        try
        {
            throw new System.NullReferenceException();
        }
        catch (System.NullReferenceException e) if (bool c = false)
        {
            System.Console.WriteLine(e);
        }
        catch (System.Exception e) 
        {
            System.Console.WriteLine(e);
            System.Console.WriteLine(c);
        }

        try
        {
            throw new System.NullReferenceException();
        }
        catch (System.NullReferenceException e) if (bool d = false)
        {
            System.Console.WriteLine(e);
        }
        catch
        {
            System.Console.WriteLine(d);
        }

        try
        {
            throw new System.NullReferenceException();
        }
        catch (System.NullReferenceException e)
        {
            System.Console.WriteLine(e);
            System.Console.WriteLine(f);
        }
        catch (System.Exception e) if (bool f = false)
        {
            System.Console.WriteLine(e);
        }

        try
        {
            throw new System.NullReferenceException();
        }
        catch (System.NullReferenceException e) if (bool g = false)
        {
            System.Console.WriteLine(e);
        }
        finally
        {
            System.Console.WriteLine(g);
        }
    }
}";
            var compilation = CreateCompilationWithMscorlib(text, compOptions: TestOptions.Exe);

            compilation.VerifyDiagnostics(
    // (15,34): error CS0103: The name 'a' does not exist in the current context
    //         System.Console.WriteLine(a);
    Diagnostic(ErrorCode.ERR_NameNotInContext, "a").WithArguments("a").WithLocation(15, 34),
    // (25,40): error CS0103: The name 'b' does not exist in the current context
    //         catch (System.Exception e) if (b)
    Diagnostic(ErrorCode.ERR_NameNotInContext, "b").WithArguments("b").WithLocation(25, 40),
    // (41,38): error CS0103: The name 'c' does not exist in the current context
    //             System.Console.WriteLine(c);
    Diagnostic(ErrorCode.ERR_NameNotInContext, "c").WithArguments("c").WithLocation(41, 38),
    // (54,38): error CS0103: The name 'd' does not exist in the current context
    //             System.Console.WriteLine(d);
    Diagnostic(ErrorCode.ERR_NameNotInContext, "d").WithArguments("d").WithLocation(54, 38),
    // (64,38): error CS0103: The name 'f' does not exist in the current context
    //             System.Console.WriteLine(f);
    Diagnostic(ErrorCode.ERR_NameNotInContext, "f").WithArguments("f").WithLocation(64, 38),
    // (81,38): error CS0103: The name 'g' does not exist in the current context
    //             System.Console.WriteLine(g);
    Diagnostic(ErrorCode.ERR_NameNotInContext, "g").WithArguments("g").WithLocation(81, 38)
            );
        }

        [Fact]
        public void CatchFilter_03()
        {
            var text = @"
public class Cls
{
    public static void Main()
    {
        try
        {
            throw new System.NullReferenceException();
        }
        catch (System.NullReferenceException) if (bool a = false)
        {
        }

        System.Console.WriteLine(a);

        try
        {
            throw new System.NullReferenceException();
        }
        catch (System.NullReferenceException) if (bool b = false)
        {
        }
        catch (System.Exception) if (b)
        {
        }

        try
        {
            throw new System.NullReferenceException();
        }
        catch (System.NullReferenceException) if (bool c = false)
        {
        }
        catch (System.Exception) 
        {
            System.Console.WriteLine(c);
        }

        try
        {
            throw new System.NullReferenceException();
        }
        catch (System.NullReferenceException) if (bool d = false)
        {
        }
        catch
        {
            System.Console.WriteLine(d);
        }

        try
        {
            throw new System.NullReferenceException();
        }
        catch (System.NullReferenceException)
        {
            System.Console.WriteLine(f);
        }
        catch (System.Exception) if (bool f = false)
        {
        }

        try
        {
            throw new System.NullReferenceException();
        }
        catch (System.NullReferenceException) if (bool g = false)
        {
        }
        finally
        {
            System.Console.WriteLine(g);
        }
    }
}";
            var compilation = CreateCompilationWithMscorlib(text, compOptions: TestOptions.Exe);

            compilation.VerifyDiagnostics(
    // (14,34): error CS0103: The name 'a' does not exist in the current context
    //         System.Console.WriteLine(a);
    Diagnostic(ErrorCode.ERR_NameNotInContext, "a").WithArguments("a").WithLocation(14, 34),
    // (23,38): error CS0103: The name 'b' does not exist in the current context
    //         catch (System.Exception) if (b)
    Diagnostic(ErrorCode.ERR_NameNotInContext, "b").WithArguments("b").WithLocation(23, 38),
    // (36,38): error CS0103: The name 'c' does not exist in the current context
    //             System.Console.WriteLine(c);
    Diagnostic(ErrorCode.ERR_NameNotInContext, "c").WithArguments("c").WithLocation(36, 38),
    // (48,38): error CS0103: The name 'd' does not exist in the current context
    //             System.Console.WriteLine(d);
    Diagnostic(ErrorCode.ERR_NameNotInContext, "d").WithArguments("d").WithLocation(48, 38),
    // (57,38): error CS0103: The name 'f' does not exist in the current context
    //             System.Console.WriteLine(f);
    Diagnostic(ErrorCode.ERR_NameNotInContext, "f").WithArguments("f").WithLocation(57, 38),
    // (72,38): error CS0103: The name 'g' does not exist in the current context
    //             System.Console.WriteLine(g);
    Diagnostic(ErrorCode.ERR_NameNotInContext, "g").WithArguments("g").WithLocation(72, 38)
            );
        }

        [Fact]
        public void CatchFilter_04()
        {
            var text = @"
public class Cls
{
    public static void Main()
    {
        int a1 = 0;
        System.Console.WriteLine(a1);
        try
        {
            throw new System.NullReferenceException();
        }
        catch (System.NullReferenceException e) if (bool a1 = false)
        {
            System.Console.WriteLine(e);
        }

        int a2 = 0;
        System.Console.WriteLine(a2);
        try
        {
            throw new System.NullReferenceException();
        }
        catch (System.NullReferenceException)
        {
            System.Console.WriteLine(int a2 = 1);
        }

        try
        {
            throw new System.NullReferenceException();
        }
        catch (System.NullReferenceException b1) if (bool b1 = false)
        {
        }

        try
        {
            throw new System.NullReferenceException();
        }
        catch (System.NullReferenceException b2)
        {
            System.Console.WriteLine(int b2 = 0);
        }

        try
        {
            throw new System.NullReferenceException();
        }
        catch (System.NullReferenceException) if ((bool c1 = false) && (bool c1 = false))
        {
        }

        try
        {
            throw new System.NullReferenceException();
        }
        catch (System.NullReferenceException) if (bool c2 = false) 
        {
            System.Console.WriteLine(int c2 = 1);
        }

        try
        {
            throw new System.NullReferenceException();
        }
        catch (System.NullReferenceException) if (bool c3 = false) 
        {
            int c3 = 1;
            System.Console.WriteLine(c3);
        }

        try
        {
            throw new System.NullReferenceException();
        }
        catch (System.NullReferenceException)
        {
            System.Console.WriteLine((bool d1 = false) && (bool d1 = false));
        }
    }
}";
            var compilation = CreateCompilationWithMscorlib(text, compOptions: TestOptions.Exe);

            compilation.VerifyDiagnostics(
    // (12,58): error CS0136: A local or parameter named 'a1' cannot be declared in this scope because that name is used in an enclosing local scope to define a local or parameter
    //         catch (System.NullReferenceException e) if (bool a1 = false)
    Diagnostic(ErrorCode.ERR_LocalIllegallyOverrides, "a1").WithArguments("a1").WithLocation(12, 58),
    // (25,42): error CS0136: A local or parameter named 'a2' cannot be declared in this scope because that name is used in an enclosing local scope to define a local or parameter
    //             System.Console.WriteLine(int a2 = 1);
    Diagnostic(ErrorCode.ERR_LocalIllegallyOverrides, "a2").WithArguments("a2").WithLocation(25, 42),
    // (32,59): error CS0128: A local variable named 'b1' is already defined in this scope
    //         catch (System.NullReferenceException b1) if (bool b1 = false)
    Diagnostic(ErrorCode.ERR_LocalDuplicate, "b1").WithArguments("b1").WithLocation(32, 59),
    // (42,42): error CS0136: A local or parameter named 'b2' cannot be declared in this scope because that name is used in an enclosing local scope to define a local or parameter
    //             System.Console.WriteLine(int b2 = 0);
    Diagnostic(ErrorCode.ERR_LocalIllegallyOverrides, "b2").WithArguments("b2").WithLocation(42, 42),
    // (49,78): error CS0128: A local variable named 'c1' is already defined in this scope
    //         catch (System.NullReferenceException) if ((bool c1 = false) && (bool c1 = false))
    Diagnostic(ErrorCode.ERR_LocalDuplicate, "c1").WithArguments("c1").WithLocation(49, 78),
    // (59,42): error CS0136: A local or parameter named 'c2' cannot be declared in this scope because that name is used in an enclosing local scope to define a local or parameter
    //             System.Console.WriteLine(int c2 = 1);
    Diagnostic(ErrorCode.ERR_LocalIllegallyOverrides, "c2").WithArguments("c2").WithLocation(59, 42),
    // (68,17): error CS0136: A local or parameter named 'c3' cannot be declared in this scope because that name is used in an enclosing local scope to define a local or parameter
    //             int c3 = 1;
    Diagnostic(ErrorCode.ERR_LocalIllegallyOverrides, "c3").WithArguments("c3").WithLocation(68, 17),
    // (78,65): error CS0128: A local variable named 'd1' is already defined in this scope
    //             System.Console.WriteLine((bool d1 = false) && (bool d1 = false));
    Diagnostic(ErrorCode.ERR_LocalDuplicate, "d1").WithArguments("d1").WithLocation(78, 65),
    // (32,46): warning CS0168: The variable 'b1' is declared but never used
    //         catch (System.NullReferenceException b1) if (bool b1 = false)
    Diagnostic(ErrorCode.WRN_UnreferencedVar, "b1").WithArguments("b1").WithLocation(32, 46),
    // (40,46): warning CS0168: The variable 'b2' is declared but never used
    //         catch (System.NullReferenceException b2)
    Diagnostic(ErrorCode.WRN_UnreferencedVar, "b2").WithArguments("b2").WithLocation(40, 46));
        }

        [Fact(Skip = "867929")]
        public void CatchFilter_05()
        {
            var text = @"
public class Cls
{
    public static void Main()
    {
        try
        {
            throw new System.NullReferenceException();
        }
        catch (System.NullReferenceException b1) if (bool b1 = false)
        {
            System.Console.WriteLine(b1);
        }

        try
        {
            throw new System.NullReferenceException();
        }
        catch (System.NullReferenceException) if ((bool c1 = false) && (bool c1 = false))
        {
            System.Console.WriteLine(c1);
        }
    }
}";
            var compilation = CreateCompilationWithMscorlib(text, compOptions: TestOptions.Exe);

            compilation.VerifyDiagnostics(
    // (10,59): error CS0128: A local variable named 'b1' is already defined in this scope
    //         catch (System.NullReferenceException b1) if (bool b1 = false)
    Diagnostic(ErrorCode.ERR_LocalDuplicate, "b1").WithArguments("b1").WithLocation(10, 59),
    // (19,78): error CS0128: A local variable named 'c1' is already defined in this scope
    //         catch (System.NullReferenceException) if ((bool c1 = false) && (bool c1 = false))
    Diagnostic(ErrorCode.ERR_LocalDuplicate, "c1").WithArguments("c1").WithLocation(19, 78)
                );
        }

    }
}