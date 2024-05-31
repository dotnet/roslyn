// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using System.Linq;
using System.Text;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.CSharp.Test.Utilities;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Test.Utilities;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.CodeAnalysis.CSharp.UnitTests
{
    public class ParserErrorMessageTests : ParsingTests
    {
        public ParserErrorMessageTests(ITestOutputHelper output) : base(output) { }

        #region "Targeted Error Tests - please arrange tests in the order of error code"

        [WorkItem(536666, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/536666")]
        [Fact]
        public void CS0071ERR_ExplicitEventFieldImpl()
        {
            // Diff errors
            var test = @"
public delegate void D();
interface Itest
{
   event D E;
}
class Test : Itest
{
   event D ITest.E()   // CS0071
   {
   }
   public static int Main()
   {
       return 1;
   }
}
";

            ParseAndValidate(test,
                // (9,17): error CS0071: An explicit interface implementation of an event must use event accessor syntax
                //    event D ITest.E()   // CS0071
                Diagnostic(ErrorCode.ERR_ExplicitEventFieldImpl, ".").WithLocation(9, 17),
                // (9,20): error CS8124: Tuple must contain at least two elements.
                //    event D ITest.E()   // CS0071
                Diagnostic(ErrorCode.ERR_TupleTooFewElements, ")").WithLocation(9, 20),
                // (10,4): error CS1519: Invalid token '{' in class, record, struct, or interface member declaration
                //    {
                Diagnostic(ErrorCode.ERR_InvalidMemberDecl, "{").WithArguments("{").WithLocation(10, 4),
                // (12,4): error CS8803: Top-level statements must precede namespace and type declarations.
                //    public static int Main()
                Diagnostic(ErrorCode.ERR_TopLevelStatementAfterNamespaceOrType, @"public static int Main()
   {
       return 1;
   }").WithLocation(12, 4),
                // (12,4): error CS0106: The modifier 'public' is not valid for this item
                //    public static int Main()
                Diagnostic(ErrorCode.ERR_BadMemberFlag, "public").WithArguments("public").WithLocation(12, 4),
                // (16,1): error CS1022: Type or namespace definition, or end-of-file expected
                // }
                Diagnostic(ErrorCode.ERR_EOFExpected, "}").WithLocation(16, 1)
                );
        }

        // Infinite loop 
        [Fact]
        public void CS0073ERR_AddRemoveMustHaveBody()
        {
            var test = @"
using System;
class C 
{
    event Action E { add; remove; }
}
abstract class A
{
    public abstract event Action E { add; remove; }
}
";

            CreateCompilation(test).VerifyDiagnostics(
                // (5,25): error CS0073: An add or remove accessor must have a body
                //     event Action E { add; remove; }
                Diagnostic(ErrorCode.ERR_AddRemoveMustHaveBody, ";"),
                // (5,33): error CS0073: An add or remove accessor must have a body
                //     event Action E { add; remove; }
                Diagnostic(ErrorCode.ERR_AddRemoveMustHaveBody, ";"),
                // (9,36): error CS8712: 'A.E': abstract event cannot use event accessor syntax
                //     public abstract event Action E { add; remove; }
                Diagnostic(ErrorCode.ERR_AbstractEventHasAccessors, "{").WithArguments("A.E").WithLocation(9, 36));
        }

        [Fact]
        public void CS0080ERR_ConstraintOnlyAllowedOnGenericDecl()
        {
            var test = @"
interface I {}
class C where C : I  // CS0080 - C is not generic class
{
}
public class Test
{
    public static int Main ()
    {
        return 1;
    }
}
";
            CreateCompilation(test).VerifyDiagnostics(
                // (3,9): error CS0080: Constraints are not allowed on non-generic declarations
                Diagnostic(ErrorCode.ERR_ConstraintOnlyAllowedOnGenericDecl, "where").WithLocation(3, 9));
        }

        [WorkItem(527827, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/527827")]
        [Fact]
        public void CS0080ERR_ConstraintOnlyAllowedOnGenericDecl_2()
        {
            var test = @"
class C 
    where C : I
{
}
";
            CreateCompilation(test).VerifyDiagnostics(
                // (3,5): error CS0080: Constraints are not allowed on non-generic declarations
                //     where C : I
                Diagnostic(ErrorCode.ERR_ConstraintOnlyAllowedOnGenericDecl, "where"));
        }

        [Fact]
        public void CS0107ERR_BadMemberProtection()
        {
            var test = @"
public class C
{
    private internal void f() {}
    public private int F = 1;
    public private int P { get => 1; }
    public int Q { get => 1; private public set {} }
    public private C() {}
    public private static int Main()
    {
        return 1;
    }
}
";

            ParseAndValidate(test);
            CreateCompilation(test).VerifyDiagnostics(
                // (4,27): error CS0107: More than one protection modifier
                //     private internal void f() {}
                Diagnostic(ErrorCode.ERR_BadMemberProtection, "f").WithLocation(4, 27),
                // (5,24): error CS0107: More than one protection modifier
                //     public private int F = 1;
                Diagnostic(ErrorCode.ERR_BadMemberProtection, "F").WithLocation(5, 24),
                // (6,24): error CS0107: More than one protection modifier
                //     public private int P { get => 1; }
                Diagnostic(ErrorCode.ERR_BadMemberProtection, "P").WithLocation(6, 24),
                // (7,45): error CS0107: More than one protection modifier
                //     public int Q { get => 1; private public set {} }
                Diagnostic(ErrorCode.ERR_BadMemberProtection, "set").WithLocation(7, 45),
                // (7,45): error CS0273: The accessibility modifier of the 'C.Q.set' accessor must be more restrictive than the property or indexer 'C.Q'
                //     public int Q { get => 1; private public set {} }
                Diagnostic(ErrorCode.ERR_InvalidPropertyAccessMod, "set").WithArguments("C.Q.set", "C.Q").WithLocation(7, 45),
                // (8,20): error CS0107: More than one protection modifier
                //     public private C() {}
                Diagnostic(ErrorCode.ERR_BadMemberProtection, "C").WithLocation(8, 20),
                // (9,31): error CS0107: More than one protection modifier
                //     public private static int Main()
                Diagnostic(ErrorCode.ERR_BadMemberProtection, "Main").WithLocation(9, 31)
                );
        }

        [Fact, WorkItem(543622, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/543622")]
        public void CS0116ERR__NamespaceUnexpected()
        {
            var test = @"{
    get
    {
        ParseDefaultDir();
    }
}";
            // Extra errors
            ParseAndValidate(test,
                // (2,8): error CS1002: ; expected
                //     get
                Diagnostic(ErrorCode.ERR_SemicolonExpected, "").WithLocation(2, 8)
                );
        }

        [Fact]
        public void CS0145ERR_ConstValueRequired()
        {
            var test = @"
namespace x
{
    public class a
    {
        public static int Main()
        {
            return 1;
        }
    }
    public class b : a
    {
        public const int i;
    }
}
";

            ParseAndValidate(test, Diagnostic(ErrorCode.ERR_ConstValueRequired, "i"));
        }

        [WorkItem(536667, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/536667")]
        [Fact]
        public void CS0150ERR_ConstantExpected()
        {
            var test = @"
using namespace System;
public class mine {
    public enum e1 {one=1, two=2, three= };
    public static int Main()
        {
        return 1;
        }
    };
}
";

            ParseAndValidate(test,
                // (2,7): error CS1041: Identifier expected; 'namespace' is a keyword
                // using namespace System;
                Diagnostic(ErrorCode.ERR_IdentifierExpectedKW, "namespace").WithArguments("", "namespace").WithLocation(2, 7),
                // (4,42): error CS0150: A constant value is expected
                //     public enum e1 {one=1, two=2, three= };
                Diagnostic(ErrorCode.ERR_ConstantExpected, "}").WithLocation(4, 42),
                // (10,1): error CS1022: Type or namespace definition, or end-of-file expected
                // }
                Diagnostic(ErrorCode.ERR_EOFExpected, "}").WithLocation(10, 1));
        }

        [WorkItem(862028, "DevDiv/Personal")]
        [Fact]
        public void CS0178ERR_InvalidArray()
        {
            // Diff errors
            var test = @"
class A
{
    public static int Main()
    {
        int[] arr = new int[5][5;
        return 1;
    }
}
";

            CreateCompilation(test).VerifyDiagnostics(
                // (6,32): error CS0178: Invalid rank specifier: expected ',' or ']'
                //         int[] arr = new int[5][5;
                Diagnostic(ErrorCode.ERR_InvalidArray, "5").WithLocation(6, 32),
                // (6,33): error CS1003: Syntax error, ',' expected
                //         int[] arr = new int[5][5;
                Diagnostic(ErrorCode.ERR_SyntaxError, ";").WithArguments(",").WithLocation(6, 33),
                // (6,33): error CS0443: Syntax error; value expected
                //         int[] arr = new int[5][5;
                Diagnostic(ErrorCode.ERR_ValueExpected, "").WithLocation(6, 33),
                // (6,33): error CS1003: Syntax error, ']' expected
                //         int[] arr = new int[5][5;
                Diagnostic(ErrorCode.ERR_SyntaxError, ";").WithArguments("]").WithLocation(6, 33),
                // (6,33): error CS0178: Invalid rank specifier: expected ',' or ']'
                //         int[] arr = new int[5][5;
                Diagnostic(ErrorCode.ERR_InvalidArray, "").WithLocation(6, 33)
                );
        }

        [Fact, WorkItem(24701, "https://github.com/dotnet/roslyn/issues/24701")]
        public void CS0178ERR_InvalidArray_ImplicitArray1()
        {
            var test = @"
class C {
    void Goo() {
        var x = new[3] { 1, 2, 3 };
    }
}
";

            ParseAndValidate(test,
                // (4,21): error CS0178: Invalid rank specifier: expected ',' or ']'
                //         var x = new[3] { 1, 2, 3 };
                Diagnostic(ErrorCode.ERR_InvalidArray, "3").WithLocation(4, 21));
        }

        [Fact, WorkItem(24701, "https://github.com/dotnet/roslyn/issues/24701")]
        public void CS0178ERR_InvalidArray_ImplicitArray2()
        {
            var test = @"
class C {
    void Goo() {
        var x = new[3,] { 1, 2, 3 };
    }
}
";

            ParseAndValidate(test,
                // (4,21): error CS0178: Invalid rank specifier: expected ',' or ']'
                //         var x = new[3,] { 1, 2, 3 };
                Diagnostic(ErrorCode.ERR_InvalidArray, "3").WithLocation(4, 21));
        }

        [Fact, WorkItem(24701, "https://github.com/dotnet/roslyn/issues/24701")]
        public void CS0178ERR_InvalidArray_ImplicitArray3()
        {
            var test = @"
class C {
    void Goo() {
        var x = new[,3] { 1, 2, 3 };
    }
}
";

            ParseAndValidate(test,
                // (4,22): error CS0178: Invalid rank specifier: expected ',' or ']'
                //         var x = new[,3] { 1, 2, 3 };
                Diagnostic(ErrorCode.ERR_InvalidArray, "3").WithLocation(4, 22));
        }

        [Fact, WorkItem(24701, "https://github.com/dotnet/roslyn/issues/24701")]
        public void CS0178ERR_InvalidArray_ImplicitArray4()
        {
            var test = @"
class C {
    void Goo() {
        var x = new[,3 { 1, 2, 3 };
    }
}
";

            ParseAndValidate(test,
                // (4,22): error CS0178: Invalid rank specifier: expected ',' or ']'
                //         var x = new[,3 { 1, 2, 3 };
                Diagnostic(ErrorCode.ERR_InvalidArray, "3").WithLocation(4, 22),
                // (4,24): error CS1003: Syntax error, ']' expected
                //         var x = new[,3 { 1, 2, 3 };
                Diagnostic(ErrorCode.ERR_SyntaxError, "{").WithArguments("]").WithLocation(4, 24));
        }

        [Fact, WorkItem(24701, "https://github.com/dotnet/roslyn/issues/24701")]
        public void CS0178ERR_InvalidArray_ImplicitArray5()
        {
            var test = @"
class C {
    void Goo() {
        var x = new[3 { 1, 2, 3 };
    }
}
";

            ParseAndValidate(test,
                // (4,21): error CS0178: Invalid rank specifier: expected ',' or ']'
                //         var x = new[3 { 1, 2, 3 };
                Diagnostic(ErrorCode.ERR_InvalidArray, "3").WithLocation(4, 21),
                // (4,23): error CS1003: Syntax error, ']' expected
                //         var x = new[3 { 1, 2, 3 };
                Diagnostic(ErrorCode.ERR_SyntaxError, "{").WithArguments("]").WithLocation(4, 23));
        }

        [Fact, WorkItem(24701, "https://github.com/dotnet/roslyn/issues/24701")]
        public void CS0178ERR_InvalidArray_ImplicitArray6()
        {
            var test = @"
class C {
    void Goo() {
        var x = new[3, { 1, 2, 3 };
    }
}
";

            ParseAndValidate(test,
                // (4,21): error CS0178: Invalid rank specifier: expected ',' or ']'
                //         var x = new[3, { 1, 2, 3 };
                Diagnostic(ErrorCode.ERR_InvalidArray, "3").WithLocation(4, 21),
                // (4,24): error CS1003: Syntax error, ']' expected
                //         var x = new[3, { 1, 2, 3 };
                Diagnostic(ErrorCode.ERR_SyntaxError, "{").WithArguments("]").WithLocation(4, 24));
        }

        [Fact, WorkItem(24701, "https://github.com/dotnet/roslyn/issues/24701")]
        public void CS0178ERR_InvalidArray_ImplicitArray7()
        {
            var test = @"
class C {
    void Goo() {
        var x = new[3,,] { 1, 2, 3 };
    }
}
";

            ParseAndValidate(test,
                // (4,21): error CS0178: Invalid rank specifier: expected ',' or ']'
                //         var x = new[3,,] { 1, 2, 3 };
                Diagnostic(ErrorCode.ERR_InvalidArray, "3").WithLocation(4, 21));
        }

        [Fact, WorkItem(24701, "https://github.com/dotnet/roslyn/issues/24701")]
        public void CS0178ERR_InvalidArray_ImplicitArray8()
        {
            var test = @"
class C {
    void Goo() {
        var x = new[,3,] { 1, 2, 3 };
    }
}
";

            ParseAndValidate(test,
                // (4,22): error CS0178: Invalid rank specifier: expected ',' or ']'
                //         var x = new[,3,] { 1, 2, 3 };
                Diagnostic(ErrorCode.ERR_InvalidArray, "3").WithLocation(4, 22));
        }

        [Fact, WorkItem(24701, "https://github.com/dotnet/roslyn/issues/24701")]
        public void CS0178ERR_InvalidArray_ImplicitArray9()
        {
            var test = @"
class C {
    void Goo() {
        var x = new[,,3] { 1, 2, 3 };
    }
}
";

            ParseAndValidate(test,
                // (4,23): error CS0178: Invalid rank specifier: expected ',' or ']'
                //         var x = new[,,3] { 1, 2, 3 };
                Diagnostic(ErrorCode.ERR_InvalidArray, "3").WithLocation(4, 23));
        }

        [Fact, WorkItem(24701, "https://github.com/dotnet/roslyn/issues/24701")]
        public void CS0178ERR_InvalidArray_ImplicitArray10()
        {
            var test = @"
class C {
    void Goo() {
        var x = new[3,,3] { 1, 2, 3 };
    }
}
";

            ParseAndValidate(test,
                // (4,21): error CS0178: Invalid rank specifier: expected ',' or ']'
                //         var x = new[3,,3] { 1, 2, 3 };
                Diagnostic(ErrorCode.ERR_InvalidArray, "3").WithLocation(4, 21),
                // (4,24): error CS0178: Invalid rank specifier: expected ',' or ']'
                //         var x = new[3,,3] { 1, 2, 3 };
                Diagnostic(ErrorCode.ERR_InvalidArray, "3").WithLocation(4, 24));
        }

        [Fact, WorkItem(24701, "https://github.com/dotnet/roslyn/issues/24701")]
        public void CS0178ERR_InvalidArray_ImplicitArray11()
        {
            var test = @"
class C {
    void Goo() {
        var x = new[ { 1, 2, 3 };
    }
}
";

            ParseAndValidate(test,
                // (4,22): error CS1003: Syntax error, ']' expected
                //         var x = new[ { 1, 2, 3 };
                Diagnostic(ErrorCode.ERR_SyntaxError, "{").WithArguments("]").WithLocation(4, 22));
        }

        [Fact]
        public void CS0230ERR_BadForeachDecl()
        {
            var test = @"
class MyClass
{
    public static int Main()
    {
        int[] myarray = new int[3] {10,2,3};
        foreach (int in myarray)   // CS0230
        {
        }
        return 1;
    }
}
";
            ParseAndValidate(test,
                // (7,18): error CS1525: Invalid expression term 'int'
                //         foreach (int in myarray)   // CS0230
                Diagnostic(ErrorCode.ERR_InvalidExprTerm, "int").WithArguments("int").WithLocation(7, 18),
                // (7,22): error CS0230: Type and identifier are both required in a foreach statement
                //         foreach (int in myarray)   // CS0230
                Diagnostic(ErrorCode.ERR_BadForeachDecl, "in").WithLocation(7, 22)
                );
        }

        [Fact]
        public void CS0230ERR_BadForeachDecl02()
        {
            // TODO: Extra error
            var test = @"
public class Test
{
    static void Main(string[] args)
    {
        int[] myarray = new int[3] { 1, 2, 3 };
        foreach (x in myarray) { }// Invalid
    }
}
";
            ParseAndValidate(test,
                // (7,20): error CS0230: Type and identifier are both required in a foreach statement
                //         foreach (x in myarray) { }// Invalid
                Diagnostic(ErrorCode.ERR_BadForeachDecl, "in")
                );
        }

        [Fact]
        public void CS0230ERR_BadForeachDecl03()
        {
            // TODO: Extra error
            var test = @"
public class Test
{
    static void Main(string[] args)
    {
        st[][] myarray = new st[1000][];
        foreach (st[] in myarray) { }
    }
}
public struct st { }
";

            ParseAndValidate(test,
                // (7,21): error CS0443: Syntax error; value expected
                //         foreach (st[] in myarray) { }
                Diagnostic(ErrorCode.ERR_ValueExpected, "]").WithLocation(7, 21),
                // (7,23): error CS0230: Type and identifier are both required in a foreach statement
                //         foreach (st[] in myarray) { }
                Diagnostic(ErrorCode.ERR_BadForeachDecl, "in").WithLocation(7, 23)
                );
        }

        [Fact]
        public void CS0231ERR_ParamsLast()
        {
            var test = @"
public class MyClass {
    public void MyMeth(params int[] values, int i) {}
    public static int Main() {
        return 1;
    }
}
";

            CreateCompilationWithMscorlib45(test).VerifyDiagnostics(
                // (3,24): error CS0231: A params parameter must be the last parameter in a parameter list
                //     public void MyMeth(params int[] values, int i) {}
                Diagnostic(ErrorCode.ERR_ParamsLast, "params int[] values").WithLocation(3, 24));
        }

        [Fact]
        public void CS0257ERR_VarargsLast()
        {
            var test = @"
class Goo
{
  public void Bar(__arglist,  int b)
  {
  }
}
";

            CreateCompilation(test).VerifyDiagnostics(
                // (4,19): error CS0257: An __arglist parameter must be the last parameter in a parameter list
                //   public void Bar(__arglist,  int b)
                Diagnostic(ErrorCode.ERR_VarargsLast, "__arglist"));
        }

        [WorkItem(536668, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/536668")]
        [Fact]
        public void CS0267ERR_PartialMisplaced()
        {
            // Diff error
            var test = @"
partial public class C  // CS0267
{
}
public class Test
{
    public static int Main ()
    {
        return 1;
    }
}
";

            CreateCompilation(test).VerifyDiagnostics(
                // (2,1): error CS0267: The 'partial' modifier can only appear immediately before 'class', 'record', 'struct', 'interface', or a method return type.
                // partial public class C  // CS0267
                Diagnostic(ErrorCode.ERR_PartialMisplaced, "partial").WithLocation(2, 1));
        }

        [Fact]
        public void CS0267ERR_PartialMisplaced_Enum()
        {
            var test = @"
partial enum E { }
";

            CreateCompilation(test).VerifyDiagnostics(
                // (2,14): error CS0267: The 'partial' modifier can only appear immediately before 'class', 'record', 'struct', 'interface', or a method return type.
                // partial enum E { }
                Diagnostic(ErrorCode.ERR_PartialMisplaced, "E").WithLocation(2, 14));
        }

        [Fact]
        public void CS0267ERR_PartialMisplaced_Delegate1()
        {
            var test = @"
partial delegate E { }
";

            // Extra errors
            CreateCompilation(test, options: TestOptions.DebugExe, parseOptions: TestOptions.Regular9).VerifyDiagnostics(
                // (2,20): error CS1001: Identifier expected
                // partial delegate E { }
                Diagnostic(ErrorCode.ERR_IdentifierExpected, "{").WithLocation(2, 20),
                // (2,20): error CS1003: Syntax error, '(' expected
                // partial delegate E { }
                Diagnostic(ErrorCode.ERR_SyntaxError, "{").WithArguments("(").WithLocation(2, 20),
                // (2,20): error CS1026: ) expected
                // partial delegate E { }
                Diagnostic(ErrorCode.ERR_CloseParenExpected, "{").WithLocation(2, 20),
                // (2,20): error CS1002: ; expected
                // partial delegate E { }
                Diagnostic(ErrorCode.ERR_SemicolonExpected, "{").WithLocation(2, 20),
                // (2,20): error CS8803: Top-level statements must precede namespace and type declarations.
                // partial delegate E { }
                Diagnostic(ErrorCode.ERR_TopLevelStatementAfterNamespaceOrType, "{ }").WithLocation(2, 20),
                // (2,20): error CS0267: The 'partial' modifier can only appear immediately before 'class', 'record', 'struct', 'interface', or a method return type.
                // partial delegate E { }
                Diagnostic(ErrorCode.ERR_PartialMisplaced, "").WithLocation(2, 20),
                // (2,18): error CS0246: The type or namespace name 'E' could not be found (are you missing a using directive or an assembly reference?)
                // partial delegate E { }
                Diagnostic(ErrorCode.ERR_SingleTypeNameNotFound, "E").WithArguments("E").WithLocation(2, 18));
        }

        [Fact]
        public void CS0267ERR_PartialMisplaced_Delegate2()
        {
            var test = @"
partial delegate void E();
";

            CreateCompilation(test).VerifyDiagnostics(
                // (2,23): error CS0267: The 'partial' modifier can only appear immediately before 'class', 'record', 'struct', 'interface', or a method return type.
                // partial delegate void E();
                Diagnostic(ErrorCode.ERR_PartialMisplaced, "E").WithLocation(2, 23));
        }

        // TODO: Extra errors
        [Fact]
        public void CS0270ERR_ArraySizeInDeclaration()
        {
            var test = @"
public class MyClass
{
    enum E { }
    public static void Main()
    {
        int[2] myarray;
        MyClass[0] m;
        byte[13,5] b;
        double[14,5,6] d;
        E[,50] e;
    }

    static int[2] myarray;
    static MyClass[0] m;
    static byte[13,5] b;
    static double[14,5,6] d;
    static E[,50] e;
}
";
            CreateCompilation(test).VerifyDiagnostics(
                // (7,12): error CS0270: Array size cannot be specified in a variable declaration (try initializing with a 'new' expression)
                //         int[2] myarray;
                Diagnostic(ErrorCode.ERR_ArraySizeInDeclaration, "[2]").WithLocation(7, 12),
                // (7,16): warning CS0168: The variable 'myarray' is declared but never used
                //         int[2] myarray;
                Diagnostic(ErrorCode.WRN_UnreferencedVar, "myarray").WithArguments("myarray").WithLocation(7, 16),
                // (8,9): error CS0119: 'MyClass' is a type, which is not valid in the given context
                //         MyClass[0] m;
                Diagnostic(ErrorCode.ERR_BadSKunknown, "MyClass").WithArguments("MyClass", "type").WithLocation(8, 9),
                // (8,20): error CS1002: ; expected
                //         MyClass[0] m;
                Diagnostic(ErrorCode.ERR_SemicolonExpected, "m").WithLocation(8, 20),
                // (8,20): error CS0201: Only assignment, call, increment, decrement, await, and new object expressions can be used as a statement
                //         MyClass[0] m;
                Diagnostic(ErrorCode.ERR_IllegalStatement, "m").WithLocation(8, 20),
                // (9,13): error CS0270: Array size cannot be specified in a variable declaration (try initializing with a 'new' expression)
                //         byte[13,5] b;
                Diagnostic(ErrorCode.ERR_ArraySizeInDeclaration, "[13,5]").WithLocation(9, 13),
                // (9,20): warning CS0168: The variable 'b' is declared but never used
                //         byte[13,5] b;
                Diagnostic(ErrorCode.WRN_UnreferencedVar, "b").WithArguments("b").WithLocation(9, 20),
                // (10,15): error CS0270: Array size cannot be specified in a variable declaration (try initializing with a 'new' expression)
                //         double[14,5,6] d;
                Diagnostic(ErrorCode.ERR_ArraySizeInDeclaration, "[14,5,6]").WithLocation(10, 15),
                // (10,24): warning CS0168: The variable 'd' is declared but never used
                //         double[14,5,6] d;
                Diagnostic(ErrorCode.WRN_UnreferencedVar, "d").WithArguments("d").WithLocation(10, 24),
                // (11,9): error CS0119: 'MyClass.E' is a type, which is not valid in the given context
                //         E[,50] e;
                Diagnostic(ErrorCode.ERR_BadSKunknown, "E").WithArguments("MyClass.E", "type").WithLocation(11, 9),
                // (11,11): error CS0443: Syntax error; value expected
                //         E[,50] e;
                Diagnostic(ErrorCode.ERR_ValueExpected, ",").WithLocation(11, 11),
                // (11,16): error CS1002: ; expected
                //         E[,50] e;
                Diagnostic(ErrorCode.ERR_SemicolonExpected, "e").WithLocation(11, 16),
                // (11,16): error CS0201: Only assignment, call, increment, decrement, await, and new object expressions can be used as a statement
                //         E[,50] e;
                Diagnostic(ErrorCode.ERR_IllegalStatement, "e").WithLocation(11, 16),
                // (14,15): error CS0270: Array size cannot be specified in a variable declaration (try initializing with a 'new' expression)
                //     static int[2] myarray;
                Diagnostic(ErrorCode.ERR_ArraySizeInDeclaration, "[2]").WithLocation(14, 15),
                // (14,19): warning CS0169: The field 'MyClass.myarray' is never used
                //     static int[2] myarray;
                Diagnostic(ErrorCode.WRN_UnreferencedField, "myarray").WithArguments("MyClass.myarray").WithLocation(14, 19),
                // (15,19): error CS0270: Array size cannot be specified in a variable declaration (try initializing with a 'new' expression)
                //     static MyClass[0] m;
                Diagnostic(ErrorCode.ERR_ArraySizeInDeclaration, "[0]").WithLocation(15, 19),
                // (15,23): warning CS0649: Field 'MyClass.m' is never assigned to, and will always have its default value null
                //     static MyClass[0] m;
                Diagnostic(ErrorCode.WRN_UnassignedInternalField, "m").WithArguments("MyClass.m", "null").WithLocation(15, 23),
                // (16,16): error CS0270: Array size cannot be specified in a variable declaration (try initializing with a 'new' expression)
                //     static byte[13,5] b;
                Diagnostic(ErrorCode.ERR_ArraySizeInDeclaration, "[13,5]").WithLocation(16, 16),
                // (16,23): warning CS0169: The field 'MyClass.b' is never used
                //     static byte[13,5] b;
                Diagnostic(ErrorCode.WRN_UnreferencedField, "b").WithArguments("MyClass.b").WithLocation(16, 23),
                // (17,18): error CS0270: Array size cannot be specified in a variable declaration (try initializing with a 'new' expression)
                //     static double[14,5,6] d;
                Diagnostic(ErrorCode.ERR_ArraySizeInDeclaration, "[14,5,6]").WithLocation(17, 18),
                // (17,27): warning CS0169: The field 'MyClass.d' is never used
                //     static double[14,5,6] d;
                Diagnostic(ErrorCode.WRN_UnreferencedField, "d").WithArguments("MyClass.d").WithLocation(17, 27),
                // (18,13): error CS0270: Array size cannot be specified in a variable declaration (try initializing with a 'new' expression)
                //     static E[,50] e;
                Diagnostic(ErrorCode.ERR_ArraySizeInDeclaration, "[,50]").WithLocation(18, 13),
                // (18,14): error CS0443: Syntax error; value expected
                //     static E[,50] e;
                Diagnostic(ErrorCode.ERR_ValueExpected, "").WithLocation(18, 14),
                // (18,19): warning CS0649: Field 'MyClass.e' is never assigned to, and will always have its default value null
                //     static E[,50] e;
                Diagnostic(ErrorCode.WRN_UnassignedInternalField, "e").WithArguments("MyClass.e", "null").WithLocation(18, 19)
                );
        }

        [Fact]
        public void CS0401ERR_NewBoundMustBeLast()
        {
            var test = @"
interface IA
{
}
class C<T> where T : new(), IA // CS0401 - should be T : IA, new()
{
}
public class Test
{
    public static int Main ()
    {
        return 1;
    }
}
";
            CreateCompilation(test).VerifyDiagnostics(
                // (5,22): error CS0401: The new() constraint must be the last restrictive constraint specified
                // class C<T> where T : new(), IA // CS0401 - should be T : IA, new()
                Diagnostic(ErrorCode.ERR_NewBoundMustBeLast, "new").WithLocation(5, 22));
        }

        [Fact]
        public void CS0439ERR_ExternAfterElements()
        {
            var test = @"
using System;
extern alias MyType;   // CS0439
// To resolve the error, make the extern alias the first line in the file.
public class Test 
{
    public static void Main() 
    {
    }
}
";

            ParseAndValidate(test, Diagnostic(ErrorCode.ERR_ExternAfterElements, "extern"));
        }

        [WorkItem(862086, "DevDiv/Personal")]
        [Fact]
        public void CS0443ERR_ValueExpected()
        {
            var test = @"
using System;
class MyClass
{
    public static void Main()    
    {
        int[,] x = new int[1,5];
        if (x[] == 5) {} // CS0443
        // if (x[0, 0] == 5) {} 
    }
}
";

            ParseAndValidate(test, Diagnostic(ErrorCode.ERR_ValueExpected, "]"));
        }

        [Fact]
        public void CS0443ERR_ValueExpected_MultiDimensional()
        {
            var test = @"
using System;
class MyClass
{
    public static void Main()    
    {
        int[,] x = new int[1,5];
        if (x[,] == 5) {} // CS0443
        // if (x[0, 0] == 5) {} 
    }
}
";

            ParseAndValidate(test,
    // (8,15): error CS0443: Syntax error; value expected
    //         if (x[,] == 5) {} // CS0443
    Diagnostic(ErrorCode.ERR_ValueExpected, ","),
    // (8,16): error CS0443: Syntax error; value expected
    //         if (x[,] == 5) {} // CS0443
    Diagnostic(ErrorCode.ERR_ValueExpected, "]"));
        }

        [Fact]
        public void CS0449ERR_TypeConstraintsMustBeUniqueAndFirst()
        {
            var test = @"
interface I {}
class C4 
{
   public void F1<T>() where T : class, struct, I {}   // CS0449
   public void F2<T>() where T : I, struct {}   // CS0449
   public void F3<T>() where T : I, class {}   // CS0449
   // OK
   public void F4<T>() where T : class {}
   public void F5<T>() where T : struct {}
   public void F6<T>() where T : I {}
}
";
            CreateCompilation(test).VerifyDiagnostics(
                // (5,41): error CS0449: The 'class', 'struct', 'unmanaged', 'notnull', and 'default' constraints cannot be combined or duplicated, and must be specified first in the constraints list.
                //    public void F1<T>() where T : class, struct, I {}   // CS0449
                Diagnostic(ErrorCode.ERR_TypeConstraintsMustBeUniqueAndFirst, "struct").WithLocation(5, 41),
                // (6,37): error CS0449: The 'class', 'struct', 'unmanaged', 'notnull', and 'default' constraints cannot be combined or duplicated, and must be specified first in the constraints list.
                //    public void F2<T>() where T : I, struct {}   // CS0449
                Diagnostic(ErrorCode.ERR_TypeConstraintsMustBeUniqueAndFirst, "struct").WithLocation(6, 37),
                // (7,37): error CS0449: The 'class', 'struct', 'unmanaged', 'notnull', and 'default' constraints cannot be combined or duplicated, and must be specified first in the constraints list.
                //    public void F3<T>() where T : I, class {}   // CS0449
                Diagnostic(ErrorCode.ERR_TypeConstraintsMustBeUniqueAndFirst, "class").WithLocation(7, 37));
        }

        [Fact]
        public void CS0451ERR_NewBoundWithVal()
        {
            var test = @"
public class C4 
{
   public void F4<T>() where T : struct, new() {}   // CS0451
}
// OK
public class C5
{
   public void F5<T>() where T : struct {}
}
public class C6
{
   public void F6<T>() where T : new() {}
}
";
            CreateCompilation(test).VerifyDiagnostics(
                // (4,42): error CS0451: The 'new()' constraint cannot be used with the 'struct' constraint
                Diagnostic(ErrorCode.ERR_NewBoundWithVal, "new").WithLocation(4, 42));
        }

        [WorkItem(862089, "DevDiv/Personal")]
        [Fact]
        public void CS0460ERR_OverrideWithConstraints()
        {
            var source =
@"interface I
{
    void M1<T>() where T : I;
    void M2<T, U>();
}
abstract class A
{
    internal virtual void M1<T>() where T : class { }
    internal abstract void M2<T>() where T : struct;
    internal abstract void M3<T>();
}
abstract class B : A, I
{
    void I.M1<T>() where T : I { }
    void I.M2<T,U>() where U : T { }
    internal override void  M1<T>() where T : class { }
    internal override void M2<T>() where T : new() { }
    internal override abstract void M3<U>() where U : A;
    internal override abstract void M4<T>() where T : struct;
}";
            CreateCompilation(source).VerifyDiagnostics(
                // (14,30): error CS0460: Constraints for override and explicit interface implementation methods are inherited from the base method, so they cannot be specified directly, except for either a 'class', or a 'struct' constraint.
                //     void I.M1<T>() where T : I { }
                Diagnostic(ErrorCode.ERR_OverrideWithConstraints, "I").WithLocation(14, 30),
                // (15,32): error CS0460: Constraints for override and explicit interface implementation methods are inherited from the base method, so they cannot be specified directly, except for either a 'class', or a 'struct' constraint.
                //     void I.M2<T,U>() where U : T { }
                Diagnostic(ErrorCode.ERR_OverrideWithConstraints, "T").WithLocation(15, 32),
                // (17,46): error CS0460: Constraints for override and explicit interface implementation methods are inherited from the base method, so they cannot be specified directly, except for either a 'class', or a 'struct' constraint.
                //     internal override void M2<T>() where T : new() { }
                Diagnostic(ErrorCode.ERR_OverrideWithConstraints, "new()").WithLocation(17, 46),
                // (18,55): error CS0460: Constraints for override and explicit interface implementation methods are inherited from the base method, so they cannot be specified directly, except for either a 'class', or a 'struct' constraint.
                //     internal override abstract void M3<U>() where U : A;
                Diagnostic(ErrorCode.ERR_OverrideWithConstraints, "A").WithLocation(18, 55),
                // (19,37): error CS0115: 'B.M4<T>()': no suitable method found to override
                //     internal override abstract void M4<T>() where T : struct;
                Diagnostic(ErrorCode.ERR_OverrideNotExpected, "M4").WithArguments("B.M4<T>()").WithLocation(19, 37));
        }

        [WorkItem(862094, "DevDiv/Personal")]
        [Fact]
        public void CS0514ERR_StaticConstructorWithExplicitConstructorCall()
        {
            var test = @"
namespace x
{
    public class @clx 
    {
        public clx(int i){}
    }
    public class @cly : clx
    {
// static does not have an object, therefore base cannot be called.
// objects must be known at compiler time
        static cly() : base(0){} // sc0514
    }
}
";

            CreateCompilation(test).VerifyDiagnostics(
                // (12,24): error CS0514: 'cly': static constructor cannot have an explicit 'this' or 'base' constructor call
                //         static cly() : base(0){} // sc0514
                Diagnostic(ErrorCode.ERR_StaticConstructorWithExplicitConstructorCall, "base").WithArguments("cly").WithLocation(12, 24),
                // (8,18): error CS7036: There is no argument given that corresponds to the required parameter 'i' of 'clx.clx(int)'
                //     public class @cly : clx
                Diagnostic(ErrorCode.ERR_NoCorrespondingArgument, "@cly").WithArguments("i", "x.clx.clx(int)").WithLocation(8, 18));
        }

        [Fact]
        public void CS0514ERR_StaticConstructorWithExplicitConstructorCall2()
        {
            var test = @"
class C
{
    C() { }
    static C() : this() { } //CS0514
}
";

            CreateCompilation(test).VerifyDiagnostics(
                // (5,18): error CS0514: 'C': static constructor cannot have an explicit 'this' or 'base' constructor call
                //     static C() : this() { } //CS0514
                Diagnostic(ErrorCode.ERR_StaticConstructorWithExplicitConstructorCall, "this").WithArguments("C").WithLocation(5, 18));
        }

        // Extra same errors
        [Fact]
        public void CS0650ERR_CStyleArray()
        {
            var test = @"
public class MyClass
{
    public static void Main()
    {
        int myarray[2]; 
        MyClass m[0];
        byte b[13,5];
        double d[14,5,6];
    }
}
";
            CreateCompilation(test).VerifyDiagnostics(
                // (6,20): error CS0650: Bad array declarator: To declare a managed array the rank specifier precedes the variable's identifier. To declare a fixed size buffer field, use the fixed keyword before the field type.
                //         int myarray[2]; 
                Diagnostic(ErrorCode.ERR_CStyleArray, "[2]").WithLocation(6, 20),
                // (6,21): error CS0270: Array size cannot be specified in a variable declaration (try initializing with a 'new' expression)
                //         int myarray[2]; 
                Diagnostic(ErrorCode.ERR_ArraySizeInDeclaration, "2").WithLocation(6, 21),
                // (7,18): error CS0650: Bad array declarator: To declare a managed array the rank specifier precedes the variable's identifier. To declare a fixed size buffer field, use the fixed keyword before the field type.
                //         MyClass m[0];
                Diagnostic(ErrorCode.ERR_CStyleArray, "[0]").WithLocation(7, 18),
                // (7,19): error CS0270: Array size cannot be specified in a variable declaration (try initializing with a 'new' expression)
                //         MyClass m[0];
                Diagnostic(ErrorCode.ERR_ArraySizeInDeclaration, "0").WithLocation(7, 19),
                // (8,15): error CS0650: Bad array declarator: To declare a managed array the rank specifier precedes the variable's identifier. To declare a fixed size buffer field, use the fixed keyword before the field type.
                //         byte b[13,5];
                Diagnostic(ErrorCode.ERR_CStyleArray, "[13,5]").WithLocation(8, 15),
                // (8,16): error CS0270: Array size cannot be specified in a variable declaration (try initializing with a 'new' expression)
                //         byte b[13,5];
                Diagnostic(ErrorCode.ERR_ArraySizeInDeclaration, "13").WithLocation(8, 16),
                // (8,19): error CS0270: Array size cannot be specified in a variable declaration (try initializing with a 'new' expression)
                //         byte b[13,5];
                Diagnostic(ErrorCode.ERR_ArraySizeInDeclaration, "5").WithLocation(8, 19),
                // (9,17): error CS0650: Bad array declarator: To declare a managed array the rank specifier precedes the variable's identifier. To declare a fixed size buffer field, use the fixed keyword before the field type.
                //         double d[14,5,6];
                Diagnostic(ErrorCode.ERR_CStyleArray, "[14,5,6]").WithLocation(9, 17),
                // (9,18): error CS0270: Array size cannot be specified in a variable declaration (try initializing with a 'new' expression)
                //         double d[14,5,6];
                Diagnostic(ErrorCode.ERR_ArraySizeInDeclaration, "14").WithLocation(9, 18),
                // (9,21): error CS0270: Array size cannot be specified in a variable declaration (try initializing with a 'new' expression)
                //         double d[14,5,6];
                Diagnostic(ErrorCode.ERR_ArraySizeInDeclaration, "5").WithLocation(9, 21),
                // (9,23): error CS0270: Array size cannot be specified in a variable declaration (try initializing with a 'new' expression)
                //         double d[14,5,6];
                Diagnostic(ErrorCode.ERR_ArraySizeInDeclaration, "6").WithLocation(9, 23),
                // (6,13): warning CS0168: The variable 'myarray' is declared but never used
                //         int myarray[2]; 
                Diagnostic(ErrorCode.WRN_UnreferencedVar, "myarray").WithArguments("myarray").WithLocation(6, 13),
                // (7,17): warning CS0168: The variable 'm' is declared but never used
                //         MyClass m[0];
                Diagnostic(ErrorCode.WRN_UnreferencedVar, "m").WithArguments("m").WithLocation(7, 17),
                // (8,14): warning CS0168: The variable 'b' is declared but never used
                //         byte b[13,5];
                Diagnostic(ErrorCode.WRN_UnreferencedVar, "b").WithArguments("b").WithLocation(8, 14),
                // (9,16): warning CS0168: The variable 'd' is declared but never used
                //         double d[14,5,6];
                Diagnostic(ErrorCode.WRN_UnreferencedVar, "d").WithArguments("d").WithLocation(9, 16)
                );
        }

        [Fact, WorkItem(535883, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/535883")]
        public void CS0687ERR_AliasQualAsExpression()
        {
            var test = @"
class Test
{
    public static int Main()
    {
        int i = global::MyType();  // CS0687
        return 1;
    }
}
";
            // Semantic error
            // (6,25): error CS0400: The type or namespace name 'MyType' could not be found in the global namespace (are you missing an assembly reference?)
            CreateCompilation(test).VerifyDiagnostics(
                Diagnostic(ErrorCode.ERR_GlobalSingleTypeNameNotFound, "MyType").WithArguments("MyType")
                );
        }

        [WorkItem(542478, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/542478")]
        [Fact]
        public void CS0706ERR_BadConstraintType()
        {
            var source =
@"interface IA<T, U, V>
    where U : T*
    where V : T[]
{
}
interface IB<T>
{
    void M<U, V>()
        where U : T*
        where V : T[];
}";
            CreateCompilation(source).VerifyDiagnostics(
                // (2,15): error CS0706: Invalid constraint type. A type used as a constraint must be an interface, a non-sealed class or a type parameter.
                //     where U : T*
                Diagnostic(ErrorCode.ERR_BadConstraintType, "T*").WithLocation(2, 15),
                // (3,15): error CS0706: Invalid constraint type. A type used as a constraint must be an interface, a non-sealed class or a type parameter.
                //     where V : T[]
                Diagnostic(ErrorCode.ERR_BadConstraintType, "T[]").WithLocation(3, 15),
                // (9,19): error CS0706: Invalid constraint type. A type used as a constraint must be an interface, a non-sealed class or a type parameter.
                //         where U : T*
                Diagnostic(ErrorCode.ERR_BadConstraintType, "T*").WithLocation(9, 19),
                // (10,19): error CS0706: Invalid constraint type. A type used as a constraint must be an interface, a non-sealed class or a type parameter.
                //         where V : T[];
                Diagnostic(ErrorCode.ERR_BadConstraintType, "T[]").WithLocation(10, 19),

                // CONSIDER: Dev10 doesn't report these cascading errors.

                // (2,15): error CS0214: Pointers and fixed size buffers may only be used in an unsafe context
                //     where U : T*
                Diagnostic(ErrorCode.ERR_UnsafeNeeded, "T*").WithLocation(2, 15),
                // (9,19): error CS0214: Pointers and fixed size buffers may only be used in an unsafe context
                //         where U : T*
                Diagnostic(ErrorCode.ERR_UnsafeNeeded, "T*").WithLocation(9, 19));
        }

        [Fact]
        public void CS0742ERR_ExpectedSelectOrGroup()
        {
            var test = @"
using System;
using System.Linq;
public class C
{
    public static int Main()
    {
        int[] array = { 1, 2, 3 };
        var c = from num in array;
        return 1;
    }
}
";

            ParseAndValidate(test, Diagnostic(ErrorCode.ERR_ExpectedSelectOrGroup, ";"));
        }

        [Fact]
        public void CS0743ERR_ExpectedContextualKeywordOn()
        {
            var test = @"
using System;
using System.Linq;
public class C
{
    public static int Main()
    {
        int[] array1 = { 1, 2, 3 ,4, 5, 6,};
        int[] array2 = { 5, 6, 7, 8, 9 };
        var c = from x in array1
                join y in array2 x equals y
                select x;
        return 1;
    }
}
";

            ParseAndValidate(test, Diagnostic(ErrorCode.ERR_ExpectedContextualKeywordOn, "x"));
        }

        [Fact]
        public void CS0744ERR_ExpectedContextualKeywordEquals()
        {
            var test = @"
using System;
using System.Linq;
public class C
{
    public static int Main()
    {
        int[] array1 = { 1, 2, 3 ,4, 5, 6,};
        int[] array2 = { 5, 6, 7, 8, 9 };
        var c = from x in array1
                join y in array2 on x y
                select x;
        return 1;
    }
}
";

            ParseAndValidate(test, Diagnostic(ErrorCode.ERR_ExpectedContextualKeywordEquals, "y"));
        }

        [WorkItem(862121, "DevDiv/Personal")]
        [Fact]
        public void CS0745ERR_ExpectedContextualKeywordBy()
        {
            var test = @"
using System;
using System.Linq;
public class C
{
    public static int Main()
    {
        int[] array1 = { 1, 2, 3 ,4, 5, 6,};
        int[] array2 = { 5, 6, 7, 8, 9 };
        var c = from x in array1
                join y in array2 on x equals y
                group x y;
        return 1;
    }
}
";

            ParseAndValidate(test, Diagnostic(ErrorCode.ERR_ExpectedContextualKeywordBy, "y"));
        }

        [Fact]
        public void CS0746ERR_InvalidAnonymousTypeMemberDeclarator()
        {
            var test = @"
public class C
{
    public static int Main()
    {
        int i = 1;
        var t = new { a.b = 1 };
        return 1;
    }
}
";

            CreateCompilation(test).VerifyDiagnostics(
                // (7,23): error CS0746: Invalid anonymous type member declarator. Anonymous type members must be declared with a member assignment, simple name or member access.
                //         var t = new { a.b = 1 };
                Diagnostic(ErrorCode.ERR_InvalidAnonymousTypeMemberDeclarator, "a.b = 1").WithLocation(7, 23),
                // (7,23): error CS0103: The name 'a' does not exist in the current context
                //         var t = new { a.b = 1 };
                Diagnostic(ErrorCode.ERR_NameNotInContext, "a").WithArguments("a").WithLocation(7, 23),
                // (6,13): warning CS0219: The variable 'i' is assigned but its value is never used
                //         int i = 1;
                Diagnostic(ErrorCode.WRN_UnreferencedVarAssg, "i").WithArguments("i").WithLocation(6, 13));
        }

        [Fact]
        public void CS0746ERR_InvalidAnonymousTypeMemberDeclarator_2()
        {
            var test = @"
public class C
{
    public static void Main()
    {
        string s = """";
        var t = new { s.Length = 1 };
    }
}
";
            CreateCompilation(test).VerifyDiagnostics(
                // (7,23): error CS0746: Invalid anonymous type member declarator. Anonymous type members must be declared with a member assignment, simple name or member access.
                //         var t = new { s.Length = 1 };
                Diagnostic(ErrorCode.ERR_InvalidAnonymousTypeMemberDeclarator, "s.Length = 1").WithLocation(7, 23),
                // (7,23): error CS0200: Property or indexer 'string.Length' cannot be assigned to -- it is read only
                //         var t = new { s.Length = 1 };
                Diagnostic(ErrorCode.ERR_AssgReadonlyProp, "s.Length").WithArguments("string.Length").WithLocation(7, 23));
        }

        [Fact]
        public void CS0746ERR_InvalidAnonymousTypeMemberDeclarator_3()
        {
            var test = @"
public class C
{
    public static void Main()
    {
        string s = """";
        var t = new { s.ToString() = 1 };
    }
}
";
            CreateCompilation(test).VerifyDiagnostics(
                // (7,23): error CS0746: Invalid anonymous type member declarator. Anonymous type members must be declared with a member assignment, simple name or member access.
                //         var t = new { s.ToString() = 1 };
                Diagnostic(ErrorCode.ERR_InvalidAnonymousTypeMemberDeclarator, "s.ToString() = 1").WithLocation(7, 23),
                // (7,23): error CS0131: The left-hand side of an assignment must be a variable, property or indexer
                //         var t = new { s.ToString() = 1 };
                Diagnostic(ErrorCode.ERR_AssgLvalueExpected, "s.ToString()").WithLocation(7, 23));
        }

        [Fact]
        public void CS0748ERR_InconsistentLambdaParameterUsage()
        {
            var test = @"
class C
{
    delegate T Func<T>();
    delegate T Func<A0, T>(A0 a0);
    delegate T Func<A0, A1, T>(A0 a0, A1 a1);
    delegate T Func<A0, A1, A2, T>(A0 a0, A1 a1, A2 a2);
    delegate T Func<A0, A1, A2, A3, T>(A0 a0, A1 a1, A2 a2, A3 a3);
    static void X()
    {
        Func<int,int,int> f1     = (int x, y) => 1;          // err: mixed parameters
        Func<int,int,int> f2     = (x, int y) => 1;          // err: mixed parameters
        Func<int,int,int,int> f3 = (int x, int y, z) => 1;   // err: mixed parameters
        Func<int,int,int,int> f4 = (int x, y, int z) => 1;   // err: mixed parameters
        Func<int,int,int,int> f5 = (x, int y, int z) => 1;   // err: mixed parameters
        Func<int,int,int,int> f6 = (x, y, int z) => 1;       // err: mixed parameters
    }
}
";

            CreateCompilation(test).VerifyDiagnostics(
                // (10,41): error CS0748: Inconsistent lambda parameter usage; parameter types must be all explicit or all implicit
                //         Func<int,int> f1      = (int x, y) => 1;          // err: mixed parameters
                Diagnostic(ErrorCode.ERR_InconsistentLambdaParameterUsage, "y"),
                // (11,37): error CS0748: Inconsistent lambda parameter usage; parameter types must be all explicit or all implicit
                //         Func<int,int> f2      = (x, int y) => 1;          // err: mixed parameters
                Diagnostic(ErrorCode.ERR_InconsistentLambdaParameterUsage, "int"),
                // (12,48): error CS0748: Inconsistent lambda parameter usage; parameter types must be all explicit or all implicit
                //         Func<int,int> f3      = (int x, int y, z) => 1;   // err: mixed parameters
                Diagnostic(ErrorCode.ERR_InconsistentLambdaParameterUsage, "z"),
                // (13,41): error CS0748: Inconsistent lambda parameter usage; parameter types must be all explicit or all implicit
                //         Func<int,int> f4      = (int x, y, int z) => 1;   // err: mixed parameters
                Diagnostic(ErrorCode.ERR_InconsistentLambdaParameterUsage, "y"),
                // (14,37): error CS0748: Inconsistent lambda parameter usage; parameter types must be all explicit or all implicit
                //         Func<int,int> f5      = (x, int y, int z) => 1;   // err: mixed parameters
                Diagnostic(ErrorCode.ERR_InconsistentLambdaParameterUsage, "int"),
                // (14,44): error CS0748: Inconsistent lambda parameter usage; parameter types must be all explicit or all implicit
                //         Func<int,int> f5      = (x, int y, int z) => 1;   // err: mixed parameters
                Diagnostic(ErrorCode.ERR_InconsistentLambdaParameterUsage, "int"),
                // (15,40): error CS0748: Inconsistent lambda parameter usage; parameter types must be all explicit or all implicit
                //         Func<int,int> f6      = (x, y, int z) => 1;       // err: mixed parameters
                Diagnostic(ErrorCode.ERR_InconsistentLambdaParameterUsage, "int"));
        }

        [WorkItem(535915, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/535915")]
        [Fact]
        public void CS0839ERR_MissingArgument()
        {
            // Diff error
            var test = @"
using System;
namespace TestNamespace
{
    class Test
    {
        static int Add(int i, int j)
        {
            return i + j;
        }
        static int Main() 
        {
            int i = Test.Add(
                              ,
                             5);
            return 1;
        }
    }
}
";

            ParseAndValidate(test, Diagnostic(ErrorCode.ERR_MissingArgument, ""));
        }

        [WorkItem(863064, "DevDiv/Personal")]
        [Fact]
        public void CS1001ERR_IdentifierExpected()
        {
            var test = @"
public class clx
{
        enum splitch
        {
            'a'
        }
}
";

            ParseAndValidate(test, Diagnostic(ErrorCode.ERR_IdentifierExpected, ""));
        }

        [Fact, WorkItem(542408, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/542408")]
        public void CS1001ERR_IdentifierExpected_2()
        {
            var test = @"
enum 
";

            ParseAndValidate(test,
Diagnostic(ErrorCode.ERR_IdentifierExpected, ""),
Diagnostic(ErrorCode.ERR_LbraceExpected, ""),
Diagnostic(ErrorCode.ERR_RbraceExpected, ""));
        }

        [Fact, WorkItem(542408, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/542408")]
        public void CS1001ERR_IdentifierExpected_5()
        {
            var test = @"
using System;
struct 

";

            ParseAndValidate(test, Diagnostic(ErrorCode.ERR_IdentifierExpected, ""),
Diagnostic(ErrorCode.ERR_LbraceExpected, ""),
Diagnostic(ErrorCode.ERR_RbraceExpected, ""));
        }

        [Fact, WorkItem(542416, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/542416")]
        public void CS1001ERR_IdentifierExpected_3()
        {
            var test = @"
using System;
class NamedExample
{
    static void Main(string[] args)
    {
        ExampleMethod(3, optionalint:4);
    }
    static void ExampleMethod(int required, string 1 = ""default string"",int optionalint = 10)
    { }
}
";
            ParseAndValidate(test,
    // (9,52): error CS1001: Identifier expected
    //     static void ExampleMethod(int required, string 1 = "default string",int optionalint = 10)
    Diagnostic(ErrorCode.ERR_IdentifierExpected, "1"),
    // (9,52): error CS1003: Syntax error, ',' expected
    //     static void ExampleMethod(int required, string 1 = "default string",int optionalint = 10)
    Diagnostic(ErrorCode.ERR_SyntaxError, "1").WithArguments(","));
        }

        [Fact, WorkItem(542416, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/542416")]
        public void CS1001ERR_IdentifierExpected_4()
        {
            var test = @"
using System;
class NamedExample
{
    static void Main(string[] args)
    {
        ExampleMethod(3, optionalint:4);
    }
    static void ExampleMethod(int required, ,int optionalint = 10)
    { }
}
";
            // Extra errors
            ParseAndValidate(test,
    // (9,45): error CS1031: Type expected
    //     static void ExampleMethod(int required, ,int optionalint = 10)
    Diagnostic(ErrorCode.ERR_TypeExpected, ","),
    // (9,45): error CS1001: Identifier expected
    //     static void ExampleMethod(int required, ,int optionalint = 10)
    Diagnostic(ErrorCode.ERR_IdentifierExpected, ","));
        }

        [Fact, WorkItem(542416, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/542416")]
        public void CS1001ERR_IdentifierExpected_6()
        {
            var test = @"
class Program
{
    const int max = 10;
    static void M(int p2 = max is int?1,)
    {
    }

    static void Main()
    {
        M(1);
    }
}
";
            // Extra errors
            ParseAndValidate(test,
    // (5,40): error CS1003: Syntax error, ':' expected
    //     static void M(int p2 = max is int?1,)
    Diagnostic(ErrorCode.ERR_SyntaxError, ",").WithArguments(":"),
    // (5,40): error CS1525: Invalid expression term ','
    //     static void M(int p2 = max is int?1,)
    Diagnostic(ErrorCode.ERR_InvalidExprTerm, ",").WithArguments(","),
    // (5,41): error CS1031: Type expected
    //     static void M(int p2 = max is int?1,)
    Diagnostic(ErrorCode.ERR_TypeExpected, ")"),
    // (5,41): error CS1001: Identifier expected
    //     static void M(int p2 = max is int?1,)
    Diagnostic(ErrorCode.ERR_IdentifierExpected, ")"));
        }

        [Fact]
        public void CS1001ERR_IdentifierExpected_7()
        {
            var test = @"
using System;
class C
{
  void M()
  {
    DateTime
    M();
  }
}
";
            CreateCompilation(test).VerifyDiagnostics(
                // (7,13): error CS1001: Identifier expected
                //     DateTime
                Diagnostic(ErrorCode.ERR_IdentifierExpected, "").WithLocation(7, 13),
                // (7,13): error CS1002: ; expected
                //     DateTime
                Diagnostic(ErrorCode.ERR_SemicolonExpected, "").WithLocation(7, 13));
        }

        [Fact]
        public void CS1002ERR_SemicolonExpected()
        {
            var test = @"
namespace x {
    abstract public class clx 
    {
        int i    // CS1002, missing semicolon
        public static int Main()
        {
            return 0;
        }
    }
}
";

            ParseAndValidate(test, Diagnostic(ErrorCode.ERR_SemicolonExpected, ""));
        }

        [WorkItem(528008, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/528008")]
        [Fact]
        public void CS1002ERR_SemicolonExpected_2()
        {
            var test = @"
class Program
{
    static void Main(string[] args)
    {
        goto Lab2,Lab1;
    Lab1:
        System.Console.WriteLine(""1"");
    Lab2:
        System.Console.WriteLine(""2"");
    }
}
";
            ParseAndValidate(test,
    // (6,18): error CS1002: ; expected
    //         goto Lab2,Lab1;
    Diagnostic(ErrorCode.ERR_SemicolonExpected, ","),
    // (6,18): error CS1513: } expected
    //         goto Lab2,Lab1;
    Diagnostic(ErrorCode.ERR_RbraceExpected, ","));
        }

        [WorkItem(527944, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/527944")]
        [Fact]
        public void CS1002ERR_SemicolonExpected_3()
        {
            var test = @"
class Program
{
    static void Main(string[] args)
    {
        goto L1;
        return;
    L1: //invalid
    }
}
";
            ParseAndValidate(test,
    // (8,8): error CS1525: Invalid expression term '}'
    //     L1: //invalid
    Diagnostic(ErrorCode.ERR_InvalidExprTerm, "").WithArguments("}"),
    // (8,8): error CS1002: ; expected
    //     L1: //invalid
    Diagnostic(ErrorCode.ERR_SemicolonExpected, ""));
        }

        [Fact()]
        public void CS1002ERR_SemicolonExpected_4()
        {
            // This used to emit CS1002.  However, improved error recovery now just treats this as as a switch that
            // terminates early, and a case-statement outside of a switch.
            var test = @"
class Program
{
    static void Main(string[] args)
    {
        string target = ""t1"";
        switch (target)
        {
        label1:
        case ""t1"":
            goto label1;
        }
    }
}
";
            // Extra errors
            ParseAndValidate(test,
                // (8,10): error CS1513: } expected
                //         {
                Diagnostic(ErrorCode.ERR_RbraceExpected, "").WithLocation(8, 10),
                // (9,16): error CS1003: Syntax error, 'switch' expected
                //         label1:
                Diagnostic(ErrorCode.ERR_SyntaxError, "").WithArguments("switch").WithLocation(9, 16));
        }

        // TODO: diff error CS1525 vs. CS1513
        [Fact]
        public void CS1003ERR_SyntaxError()
        {
            var test = @"
namespace x
{
    public class b
    {
        public static void Main()        {
            int[] a;
            a[);
        }
    }
}
";

            ParseAndValidate(test,
                // (8,15): error CS1003: Syntax error, ']' expected
                //             a[);
                Diagnostic(ErrorCode.ERR_SyntaxError, ")").WithArguments("]").WithLocation(8, 15)
                );
        }

        [Fact]
        public void CS1003ERR_SyntaxError_ForeachExpected1()
        {
            var test = @"
public class b
{
    public void Main()
    {
        for (var v in 
    }
}
";
            //the first error should be
            // (6,9): error CS1003: Syntax error, 'foreach' expected
            // don't care about any others.

            var parsedTree = ParseWithRoundTripCheck(test);
            var firstDiag = parsedTree.GetDiagnostics().Take(1);
            firstDiag.Verify(Diagnostic(ErrorCode.ERR_SyntaxError, "for").WithArguments("foreach"));
        }

        [Fact]
        public void CS1004ERR_DuplicateModifier()
        {
            var test = @"
namespace x {
    abstract public class @clx 
    {
        int i;
        public public static int Main()    // CS1004, two public keywords
        {
            return 0;
        }
    }
}
";

            CreateCompilation(test).VerifyDiagnostics(
                // (6,16): error CS1004: Duplicate 'public' modifier
                //         public public static int Main()    // CS1004, two public keywords
                Diagnostic(ErrorCode.ERR_DuplicateModifier, "public").WithArguments("public").WithLocation(6, 16),
                // (5,13): warning CS0169: The field 'clx.i' is never used
                //         int i;
                Diagnostic(ErrorCode.WRN_UnreferencedField, "i").WithArguments("x.clx.i").WithLocation(5, 13));
        }

        [Fact]
        public void CS1004ERR_DuplicateModifier1()
        {
            var test = @"
class C 
{
    public public C()
    {
    }
}";

            CreateCompilation(test).VerifyDiagnostics(
                // (4,12): error CS1004: Duplicate 'public' modifier
                //     public public C()
                Diagnostic(ErrorCode.ERR_DuplicateModifier, "public").WithArguments("public").WithLocation(4, 12));
        }

        [Fact]
        public void CS1004ERR_DuplicateModifier2()
        {
            var test = @"
class C 
{
    public public ~C()
    {
    }
}";

            CreateCompilation(test).VerifyDiagnostics(
                // (4,12): error CS1004: Duplicate 'public' modifier
                //     public public ~C()
                Diagnostic(ErrorCode.ERR_DuplicateModifier, "public").WithArguments("public").WithLocation(4, 12),
                // (4,20): error CS0106: The modifier 'public' is not valid for this item
                //     public public ~C()
                Diagnostic(ErrorCode.ERR_BadMemberFlag, "C").WithArguments("public").WithLocation(4, 20));
        }

        [Fact]
        public void CS1004ERR_DuplicateModifier3()
        {
            var test = @"
class C 
{
    public public int x;
}";

            CreateCompilation(test).VerifyDiagnostics(
                // (4,12): error CS1004: Duplicate 'public' modifier
                //     public public int x;
                Diagnostic(ErrorCode.ERR_DuplicateModifier, "public").WithArguments("public").WithLocation(4, 12),
                // (4,23): warning CS0649: Field 'C.x' is never assigned to, and will always have its default value 0
                //     public public int x;
                Diagnostic(ErrorCode.WRN_UnassignedInternalField, "x").WithArguments("C.x", "0").WithLocation(4, 23));
        }

        [Fact]
        public void CS1004ERR_DuplicateModifier4()
        {
            var test = @"
class C 
{
    public public int P { get; }
}";

            CreateCompilation(test).VerifyDiagnostics(
                // (4,12): error CS1004: Duplicate 'public' modifier
                //     public public int P { get; }
                Diagnostic(ErrorCode.ERR_DuplicateModifier, "public").WithArguments("public").WithLocation(4, 12));
        }

        [Fact]
        public void CS1004ERR_DuplicateModifier5()
        {
            var test = @"
class C 
{
    public public static implicit operator int(C c) => 0;
}";

            CreateCompilation(test).VerifyDiagnostics(
                // (4,12): error CS1004: Duplicate 'public' modifier
                //     public public static implicit operator int(C c) => 0;
                Diagnostic(ErrorCode.ERR_DuplicateModifier, "public").WithArguments("public").WithLocation(4, 12));
        }

        [Fact]
        public void CS1004ERR_DuplicateModifier6()
        {
            var test = @"
class C 
{
    public public static int operator +(C c1, C c2) => 0;
}";

            CreateCompilation(test).VerifyDiagnostics(
                // (4,12): error CS1004: Duplicate 'public' modifier
                //     public public static int operator +(C c1, C c2) => 0;
                Diagnostic(ErrorCode.ERR_DuplicateModifier, "public").WithArguments("public").WithLocation(4, 12));
        }

        [Fact]
        public void CS1004ERR_DuplicateModifier7()
        {
            var test = @"
class C 
{
    public int P { get; private private set; }
}";

            CreateCompilation(test).VerifyDiagnostics(
                // (4,33): error CS1004: Duplicate 'private' modifier
                //     public int P { get; private private set; }
                Diagnostic(ErrorCode.ERR_DuplicateModifier, "private").WithArguments("private").WithLocation(4, 33));
        }

        [Fact]
        public void CS1004ERR_DuplicateModifier8()
        {
            var test = @"
class C 
{
    public public int this[int i] => 0;
}";

            CreateCompilation(test).VerifyDiagnostics(
                // (4,12): error CS1004: Duplicate 'public' modifier
                //     public public int this[int i] => 0;
                Diagnostic(ErrorCode.ERR_DuplicateModifier, "public").WithArguments("public").WithLocation(4, 12));
        }

        [Fact]
        public void CS1004ERR_DuplicateModifier9()
        {
            var test = @"
public public class C 
{
}";

            CreateCompilation(test).VerifyDiagnostics(
                // (2,8): error CS1004: Duplicate 'public' modifier
                // public public class C 
                Diagnostic(ErrorCode.ERR_DuplicateModifier, "public").WithArguments("public").WithLocation(2, 8));
        }

        [Fact]
        public void CS1004ERR_DuplicateModifier10()
        {
            var test = @"
public public interface I
{
}";

            CreateCompilation(test).VerifyDiagnostics(
                // (2,8): error CS1004: Duplicate 'public' modifier
                // public public interface I
                Diagnostic(ErrorCode.ERR_DuplicateModifier, "public").WithArguments("public").WithLocation(2, 8));
        }

        [Fact]
        public void CS1004ERR_DuplicateModifier11()
        {
            var test = @"
public public enum E
{
}";

            CreateCompilation(test).VerifyDiagnostics(
                // (2,8): error CS1004: Duplicate 'public' modifier
                // public public enum E
                Diagnostic(ErrorCode.ERR_DuplicateModifier, "public").WithArguments("public").WithLocation(2, 8));
        }

        [Fact]
        public void CS1004ERR_DuplicateModifier12()
        {
            var test = @"
public public struct S
{
}";

            CreateCompilation(test).VerifyDiagnostics(
                // (2,8): error CS1004: Duplicate 'public' modifier
                // public public struct S
                Diagnostic(ErrorCode.ERR_DuplicateModifier, "public").WithArguments("public").WithLocation(2, 8));
        }

        [Fact]
        public void CS1004ERR_DuplicateModifier13()
        {
            var test = @"
public public delegate void D();";

            CreateCompilation(test).VerifyDiagnostics(
                // (2,8): error CS1004: Duplicate 'public' modifier
                // public public delegate void D();
                Diagnostic(ErrorCode.ERR_DuplicateModifier, "public").WithArguments("public").WithLocation(2, 8));
        }

        [Fact]
        public void CS1007ERR_DuplicateAccessor()
        {
            var test = @"using System;

public class Container
{
    public int Prop1 {
        protected get { return 1; }
        set {}
        protected get { return 1; }
    }
    public static int Prop2 {
        get { return 1; }
        internal set {}
        internal set {}
    }
    public int this[int i] {
        protected get { return 1; }
        internal set {}
        protected get { return 1; }
        internal set {} 
    }
}
";

            CreateCompilation(test).VerifyDiagnostics(
                // (8,19): error CS1007: Property accessor already defined
                //         protected get { return 1; }
                Diagnostic(ErrorCode.ERR_DuplicateAccessor, "get").WithLocation(8, 19),
                // (13,18): error CS1007: Property accessor already defined
                //         internal set {}
                Diagnostic(ErrorCode.ERR_DuplicateAccessor, "set").WithLocation(13, 18),
                // (18,19): error CS1007: Property accessor already defined
                //         protected get { return 1; }
                Diagnostic(ErrorCode.ERR_DuplicateAccessor, "get").WithLocation(18, 19),
                // (19,18): error CS1007: Property accessor already defined
                //         internal set {} 
                Diagnostic(ErrorCode.ERR_DuplicateAccessor, "set").WithLocation(19, 18),
                // (15,16): error CS0274: Cannot specify accessibility modifiers for both accessors of the property or indexer 'Container.this[int]'
                //     public int this[int i] {
                Diagnostic(ErrorCode.ERR_DuplicatePropertyAccessMods, "this").WithArguments("Container.this[int]").WithLocation(15, 16),
                // (1,1): hidden CS8019: Unnecessary using directive.
                // using System;
                Diagnostic(ErrorCode.HDN_UnusedUsingDirective, "using System;").WithLocation(1, 1));
        }

        [Fact]
        public void CS1008ERR_IntegralTypeExpected01()
        {
            CreateCompilation(
@"namespace x
{
    abstract public class @clx 
    {
        enum E : sbyte { x, y, z } // no error
        enum F : char { x, y, z } // CS1008, char not valid type for enums
        enum G : short { A, B, C } // no error
        enum H : System.Int16 { A, B, C } // CS1008, short not System.Int16
    }
}
")
            .VerifyDiagnostics(
                // (6,18): error CS1008: Type byte, sbyte, short, ushort, int, uint, long, or ulong expected
                //         enum F : char { x, y, z } // CS1008, char not valid type for enums
                Diagnostic(ErrorCode.ERR_IntegralTypeExpected, "char").WithLocation(6, 18)
                );
        }

        [Fact]
        public void CS1008ERR_IntegralTypeExpected02()
        {
            CreateCompilation(
@"interface I { }
class C { }
enum E { }
enum F : I { A }
enum G : C { A }
enum H : E { A }
enum K : System.Enum { A }
enum L : string { A }
enum M : float { A }
enum N : decimal { A }
")
                .VerifyDiagnostics(
                    Diagnostic(ErrorCode.ERR_IntegralTypeExpected, "I").WithLocation(4, 10),
                    Diagnostic(ErrorCode.ERR_IntegralTypeExpected, "C").WithLocation(5, 10),
                    Diagnostic(ErrorCode.ERR_IntegralTypeExpected, "E").WithLocation(6, 10),
                    Diagnostic(ErrorCode.ERR_IntegralTypeExpected, "System.Enum").WithLocation(7, 10),
                    Diagnostic(ErrorCode.ERR_IntegralTypeExpected, "string").WithLocation(8, 10),
                    Diagnostic(ErrorCode.ERR_IntegralTypeExpected, "float").WithLocation(9, 10),
                    Diagnostic(ErrorCode.ERR_IntegralTypeExpected, "decimal").WithLocation(10, 10));
        }

        [Fact, WorkItem(667303, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/667303")]
        public void CS1008ERR_IntegralTypeExpected03()
        {
            ParseAndValidate(@"enum E : byt { A, B }"); // no *parser* errors. This is a semantic error now.
        }

        [Fact, WorkItem(540117, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/540117")]
        public void CS1009ERR_IllegalEscape_Strings()
        {
            var text = @"
class Program
{
    static void Main()
    {
        string s;
        s = ""\u"";
        s = ""\u0"";
        s = ""\u00"";
        s = ""\u000"";
        
        s = ""a\uz"";
        s = ""a\u0z"";
        s = ""a\u00z"";
        s = ""a\u000z"";
    }
}
";
            ParseAndValidate(text,
                // (7,14): error CS1009: Unrecognized escape sequence
                Diagnostic(ErrorCode.ERR_IllegalEscape, @"\u"),
                // (8,14): error CS1009: Unrecognized escape sequence
                Diagnostic(ErrorCode.ERR_IllegalEscape, @"\u0"),
                // (9,14): error CS1009: Unrecognized escape sequence
                Diagnostic(ErrorCode.ERR_IllegalEscape, @"\u00"),
                // (10,14): error CS1009: Unrecognized escape sequence
                Diagnostic(ErrorCode.ERR_IllegalEscape, @"\u000"),
                // (12,15): error CS1009: Unrecognized escape sequence
                Diagnostic(ErrorCode.ERR_IllegalEscape, @"\u"),
                // (13,15): error CS1009: Unrecognized escape sequence
                Diagnostic(ErrorCode.ERR_IllegalEscape, @"\u0"),
                // (14,15): error CS1009: Unrecognized escape sequence
                Diagnostic(ErrorCode.ERR_IllegalEscape, @"\u00"),
                // (15,15): error CS1009: Unrecognized escape sequence
                Diagnostic(ErrorCode.ERR_IllegalEscape, @"\u000")
            );
        }

        [Fact, WorkItem(528100, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/528100")]
        public void CS1009ERR_IllegalEscape_Identifiers()
        {
            var text = @"using System;
class Program
{
    static void Main()
    {
        int \u;
        int \u0;
        int \u00;
        int \u000;

        int a\uz;
        int a\u0z;
        int a\u00z;
        int a\u000z;
    }
}
";
            ParseAndValidate(text,
        // (6,13): error CS1009: Unrecognized escape sequence
        //         int \u;
        Diagnostic(ErrorCode.ERR_IllegalEscape, @"\u"),
        // (7,13): error CS1009: Unrecognized escape sequence
        //         int \u0;
        Diagnostic(ErrorCode.ERR_IllegalEscape, @"\u0"),
        // (7,13): error CS1056: Unexpected character '\u0'
        //         int \u0;
        Diagnostic(ErrorCode.ERR_UnexpectedCharacter, "").WithArguments(@"\u0"),
        // (8,13): error CS1009: Unrecognized escape sequence
        //         int \u00;
        Diagnostic(ErrorCode.ERR_IllegalEscape, @"\u00"),
        // (8,13): error CS1056: Unexpected character '\u00'
        //         int \u00;
        Diagnostic(ErrorCode.ERR_UnexpectedCharacter, "").WithArguments(@"\u00"),
        // (9,13): error CS1009: Unrecognized escape sequence
        //         int \u000;
        Diagnostic(ErrorCode.ERR_IllegalEscape, @"\u000"),
        // (9,13): error CS1056: Unexpected character '\u000'
        //         int \u000;
        Diagnostic(ErrorCode.ERR_UnexpectedCharacter, "").WithArguments(@"\u000"),
        // (11,14): error CS1009: Unrecognized escape sequence
        //         int a\uz;
        Diagnostic(ErrorCode.ERR_IllegalEscape, @"\u"),
        // (12,14): error CS1009: Unrecognized escape sequence
        //         int a\u0z;
        Diagnostic(ErrorCode.ERR_IllegalEscape, @"\u0"),
        // (12,14): error CS1056: Unexpected character '\u0'
        //         int a\u0z;
        Diagnostic(ErrorCode.ERR_UnexpectedCharacter, "").WithArguments(@"\u0"),
        // (13,14): error CS1009: Unrecognized escape sequence
        //         int a\u00z;
        Diagnostic(ErrorCode.ERR_IllegalEscape, @"\u00"),
        // (13,14): error CS1056: Unexpected character '\u00'
        //         int a\u00z;
        Diagnostic(ErrorCode.ERR_UnexpectedCharacter, "").WithArguments(@"\u00"),
        // (14,14): error CS1009: Unrecognized escape sequence
        //         int a\u000z;
        Diagnostic(ErrorCode.ERR_IllegalEscape, @"\u000"),
                // (14,14): error CS1056: Unexpected character '\u000'
                //         int a\u000z;
                Diagnostic(ErrorCode.ERR_UnexpectedCharacter, "").WithArguments(@"\u000"),

                // NOTE: Dev11 doesn't report these cascading diagnostics.

                // (7,13): error CS1001: Identifier expected
                //         int \u0;
                Diagnostic(ErrorCode.ERR_IdentifierExpected, @"\u0"),
                // (8,13): error CS1001: Identifier expected
                //         int \u00;
                Diagnostic(ErrorCode.ERR_IdentifierExpected, @"\u00"),
                // (9,13): error CS1001: Identifier expected
                //         int \u000;
                Diagnostic(ErrorCode.ERR_IdentifierExpected, @"\u000"),
                // (12,17): error CS1002: ; expected
                //         int a\u0z;
                Diagnostic(ErrorCode.ERR_SemicolonExpected, "z"),
                // (13,18): error CS1002: ; expected
                //         int a\u00z;
                Diagnostic(ErrorCode.ERR_SemicolonExpected, "z"),
                // (14,19): error CS1002: ; expected
                //         int a\u000z;
                Diagnostic(ErrorCode.ERR_SemicolonExpected, "z")
            );
        }

        [WorkItem(535921, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/535921")]
        [Fact]
        public void CS1013ERR_InvalidNumber()
        {
            // Diff error
            var test = @"
namespace x
{
    public class a
    {
        public static int Main()        
        {
        return 1;
        }
    }
    public class b
    {
        public int d = 0x;
    }
}
";

            ParseAndValidate(test, Diagnostic(ErrorCode.ERR_InvalidNumber, ""));
        }

        [WorkItem(862116, "DevDiv/Personal")]
        [Fact]
        public void CS1014ERR_GetOrSetExpected()
        {
            var test = @"using System;

public sealed class Container
{
    public string Prop1 { protected }
    public string Prop2 { get { return null; } protected }
    public string Prop3 { get { return null; } protected set { } protected }
}
";
            ParseAndValidate(test,
Diagnostic(ErrorCode.ERR_GetOrSetExpected, "}"),
Diagnostic(ErrorCode.ERR_GetOrSetExpected, "}"),
Diagnostic(ErrorCode.ERR_GetOrSetExpected, "}"));
        }

        [Fact]
        public void CS1015ERR_ClassTypeExpected()
        {
            var test = @"
using System;
public class Test
{
    public static void Main()
    {
        try
        {
        }
        catch(int)
        {
        }
        catch(byte)
        {
        }
    }
}
";

            CreateCompilation(test).VerifyDiagnostics(
                // (10,15): error CS0155: The type caught or thrown must be derived from System.Exception
                //         catch(int)
                Diagnostic(ErrorCode.ERR_BadExceptionType, "int").WithLocation(10, 15),
                // (13,15): error CS0155: The type caught or thrown must be derived from System.Exception
                //         catch(byte)
                Diagnostic(ErrorCode.ERR_BadExceptionType, "byte").WithLocation(13, 15),
                // (2,1): hidden CS8019: Unnecessary using directive.
                // using System;
                Diagnostic(ErrorCode.HDN_UnusedUsingDirective, "using System;").WithLocation(2, 1));
        }

        [WorkItem(863382, "DevDiv/Personal")]
        [Fact]
        public void CS1016ERR_NamedArgumentExpected()
        {
            var test = @"
namespace x
{
    class GooAttribute : System.Attribute
    {
        public int a;
    }

    [Goo(a=5, b)]
    class Bar
    {
    }
    public class @a
    {
        public static int Main()
        {
            return 1;
        }
    }
}";
            CreateCompilation(test).VerifyDiagnostics(
                // (9,15): error CS1016: Named attribute argument expected
                //     [Goo(a=5, b)]
                Diagnostic(ErrorCode.ERR_NamedArgumentExpected, "b").WithLocation(9, 15),
                // (9,15): error CS0103: The name 'b' does not exist in the current context
                //     [Goo(a=5, b)]
                Diagnostic(ErrorCode.ERR_NameNotInContext, "b").WithArguments("b").WithLocation(9, 15),
                //(9,6): error CS1729: 'GooAttribute' does not contain a constructor that takes 1 arguments
                //     [Goo(a=5, b)]
                Diagnostic(ErrorCode.ERR_BadCtorArgCount, "Goo(a=5, b)").WithArguments("x.GooAttribute", "1").WithLocation(9, 6));
        }

        [Fact]
        public void CS1017ERR_TooManyCatches()
        {
            var test = @"
using System;
namespace nms {
public class S : Exception {
};
public class S1 : Exception {
};
public class @mine {
    private static int retval = 2;
    public static int Main()
        {
        try {
                throw new S();
        }
        catch {}
        catch (S1) {}
        catch (S) {}
        catch when (false) {}
        if (retval == 0) Console.WriteLine(""PASS"");
        else Console.WriteLine(""FAIL"");
           return retval;
        }
    };
}
";

            CreateCompilation(test).VerifyDiagnostics(
                // (16,9): error CS1017: Catch clauses cannot follow the general catch clause of a try statement
                //         catch (S1) {}
                Diagnostic(ErrorCode.ERR_TooManyCatches, "catch").WithLocation(16, 9),
                // (17,9): error CS1017: Catch clauses cannot follow the general catch clause of a try statement
                //         catch (S) {}
                Diagnostic(ErrorCode.ERR_TooManyCatches, "catch").WithLocation(17, 9),
                // (18,9): error CS1017: Catch clauses cannot follow the general catch clause of a try statement
                //         catch when (false) {}
                Diagnostic(ErrorCode.ERR_TooManyCatches, "catch").WithLocation(18, 9),
                // (18,21): warning CS8359: Filter expression is a constant 'false', consider removing the catch clause
                //         catch when (false) {}
                Diagnostic(ErrorCode.WRN_FilterIsConstantFalse, "false").WithLocation(18, 21));
        }

        [Fact]
        public void CS1017ERR_TooManyCatches_NoError()
        {
            var test = @"
using System;
namespace nms {
public class S : Exception {
};
public class S1 : Exception {
};
public class mine {
    private static int retval = 2;
    public static int Main()
        {
        try {
                throw new S();
        }
        catch when (true) {}
        catch (S1) {}
        catch (S) {}
        if (retval == 0) Console.WriteLine(""PASS"");
        else Console.WriteLine(""FAIL"");
           return retval;
        }
    };
}
";
            ParseAndValidate(test);
        }

        [Fact]
        public void CS1018ERR_ThisOrBaseExpected()
        {
            var test = @"
namespace x
{
    public class C
    {
    }
    public class a : C
    {
        public a () : {}
        public static int Main()
        {
            return 1;
        }
    }
}
";

            ParseAndValidate(test, Diagnostic(ErrorCode.ERR_ThisOrBaseExpected, "{"));
        }

        [WorkItem(535924, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/535924")]
        [Fact]
        public void CS1019ERR_OvlUnaryOperatorExpected()
        {
            // Diff errors
            var test = @"
namespace x
{
    public class ii
    {
        int i
        {
            get
            {
                return 0;
            }
        }
    }
    public class a 
    {
        public static ii operator ii(a aa) // replace ii with explicit or implicit
        {
            return new ii();
        }
        public static void Main()
        {
        }
    }
}
";

            ParseAndValidate(test, Diagnostic(ErrorCode.ERR_OvlUnaryOperatorExpected, "ii"));
        }

        [Fact]
        public void CS1019ERR_OvlUnaryOperatorExpected2()
        {
            var test = @"
class C
{
    public static implicit operator int(C c1, C c2) => 0;
}
";

            CreateCompilation(test).VerifyDiagnostics(
                // (4,40): error CS1019: Overloadable unary operator expected
                //     public static implicit operator int(C c1, C c2) => 0;
                Diagnostic(ErrorCode.ERR_OvlUnaryOperatorExpected, "(C c1, C c2)").WithLocation(4, 40));
        }

        [WorkItem(906502, "DevDiv/Personal")]
        [Fact]
        public void CS1020ERR_OvlBinaryOperatorExpected()
        {
            // Diff error
            var test = @"
namespace x
{
    public class iii
    {
        public static implicit operator int(iii x)
        {
            return 0;
        }
        public static implicit operator iii(int x)
        {
            return null;
        }
        public static int operator ++(iii aa, int bb)    // change ++ to +
        {
            return 0;
        }
        public static void Main()
        {
        }
    }
}
";

            ParseAndValidate(test, Diagnostic(ErrorCode.ERR_OvlBinaryOperatorExpected, "++"));
        }

        [Fact]
        public void CS1022ERR_EOFExpected()
        {
            var test = @"
 }}}}}
";

            ParseAndValidate(test,
Diagnostic(ErrorCode.ERR_EOFExpected, "}"),
Diagnostic(ErrorCode.ERR_EOFExpected, "}"),
Diagnostic(ErrorCode.ERR_EOFExpected, "}"),
Diagnostic(ErrorCode.ERR_EOFExpected, "}"),
Diagnostic(ErrorCode.ERR_EOFExpected, "}"));
        }

        [Fact]
        public void CS1022ERR_EOFExpected02()
        {
            var test = @" > Roslyn.Utilities.dll!  Basic";

            CreateCompilation(test, options: TestOptions.DebugExe, parseOptions: TestOptions.Regular9).VerifyDiagnostics(
                // (1,2): error CS1525: Invalid expression term '>'
                //  > Roslyn.Utilities.dll!  Basic
                Diagnostic(ErrorCode.ERR_InvalidExprTerm, ">").WithArguments(">").WithLocation(1, 2),
                // (1,4): error CS0103: The name 'Roslyn' does not exist in the current context
                //  > Roslyn.Utilities.dll!  Basic
                Diagnostic(ErrorCode.ERR_NameNotInContext, "Roslyn").WithArguments("Roslyn").WithLocation(1, 4),
                // (1,27): error CS1002: ; expected
                //  > Roslyn.Utilities.dll!  Basic
                Diagnostic(ErrorCode.ERR_SemicolonExpected, "Basic").WithLocation(1, 27),
                // (1,27): error CS0246: The type or namespace name 'Basic' could not be found (are you missing a using directive or an assembly reference?)
                //  > Roslyn.Utilities.dll!  Basic
                Diagnostic(ErrorCode.ERR_SingleTypeNameNotFound, "Basic").WithArguments("Basic").WithLocation(1, 27),
                // (1,32): error CS1001: Identifier expected
                //  > Roslyn.Utilities.dll!  Basic
                Diagnostic(ErrorCode.ERR_IdentifierExpected, "").WithLocation(1, 32),
                // (1,32): error CS1002: ; expected
                //  > Roslyn.Utilities.dll!  Basic
                Diagnostic(ErrorCode.ERR_SemicolonExpected, "").WithLocation(1, 32)
                );
        }

        [Fact]
        public void CS1023ERR_BadEmbeddedStmt()
        {
            var test = @"
struct S {
}
public class @a {
    public static int Main() {
        for (int i=0; i < 3; i++) MyLabel: {}
        return 1;
    }
}
";
            CreateCompilation(test).VerifyDiagnostics(
                // (6,35): error CS1023: Embedded statement cannot be a declaration or labeled statement
                //         for (int i=0; i < 3; i++) MyLabel: {}
                Diagnostic(ErrorCode.ERR_BadEmbeddedStmt, "MyLabel: {}").WithLocation(6, 35),
                // (6,35): warning CS0164: This label has not been referenced
                //         for (int i=0; i < 3; i++) MyLabel: {}
                Diagnostic(ErrorCode.WRN_UnreferencedLabel, "MyLabel").WithLocation(6, 35));
        }

        [Fact]
        public void CS1023ERR_BadEmbeddedStmt2()
        {
            var test = @"
struct S {
}
public class @a {
    public static int Main() {
        for (int i=0; i < 3; i++) int j;
        return 1;
    }
}
";
            CreateCompilation(test).VerifyDiagnostics(
                // (6,35): error CS1023: Embedded statement cannot be a declaration or labeled statement
                //         for (int i=0; i < 3; i++) int j;
                Diagnostic(ErrorCode.ERR_BadEmbeddedStmt, "int j;").WithLocation(6, 35),
                // (6,39): warning CS0168: The variable 'j' is declared but never used
                //         for (int i=0; i < 3; i++) int j;
                Diagnostic(ErrorCode.WRN_UnreferencedVar, "j").WithArguments("j").WithLocation(6, 39));
        }

        [Fact]
        public void CS1023ERR_BadEmbeddedStmt3()
        {
            var test = @"
struct S {
}
public class @a {
    public static int Main() {
        for (int i=0; i < 3; i++) void j() { }
        return 1;
    }
}
";
            CreateCompilation(test).VerifyDiagnostics(
                // (6,35): error CS1023: Embedded statement cannot be a declaration or labeled statement
                //         for (int i=0; i < 3; i++) void j() { }
                Diagnostic(ErrorCode.ERR_BadEmbeddedStmt, "void j() { }").WithLocation(6, 35),
                // (6,40): warning CS8321: The local function 'j' is declared but never used
                //         for (int i=0; i < 3; i++) void j() { }
                Diagnostic(ErrorCode.WRN_UnreferencedLocalFunction, "j").WithArguments("j").WithLocation(6, 40));
        }

        // Preprocessor:
        [Fact]
        public void CS1024ERR_PPDirectiveExpectedpp()
        {
            var test = @"#import System;";

            ParseAndValidate(test, // (1,2): error CS1024: Preprocessor directive expected
                                   // #import System;
    Diagnostic(ErrorCode.ERR_PPDirectiveExpected, "import"));
        }

        // Preprocessor:
        [Fact]
        public void CS1025ERR_EndOfPPLineExpectedpp()
        {
            var test = @"
public class Test
{
# line hidden 123
    public static void MyHiddenMethod()
    {
    }

    #undef x y
    public static void Main() 
    {
    }
}
";
            // Extra Errors
            ParseAndValidate(test,
    // (4,15): error CS1025: Single-line comment or end-of-line expected
    // # line hidden 123
    Diagnostic(ErrorCode.ERR_EndOfPPLineExpected, "123"),
    // (9,6): error CS1032: Cannot define/undefine preprocessor symbols after first token in file
    //     #undef x y
    Diagnostic(ErrorCode.ERR_PPDefFollowsToken, "undef"),
    // (9,14): error CS1025: Single-line comment or end-of-line expected
    //     #undef x y
    Diagnostic(ErrorCode.ERR_EndOfPPLineExpected, "y"));
        }

        [WorkItem(863388, "DevDiv/Personal")]
        [Fact]
        public void CS1026ERR_CloseParenExpected()
        {
            var test = @"
#if (fred == barney
#endif
namespace x
{
    public class a
    {
        public static int Main()
        {
            return 1;
        }
    }
}
";

            ParseAndValidate(test, Diagnostic(ErrorCode.ERR_CloseParenExpected, ""));
        }

        // Preprocessor:
        [Fact]
        public void CS1027ERR_EndifDirectiveExpectedpp()
        {
            var test = @"
public class Test
{
# if true
}
";

            ParseAndValidate(test, Diagnostic(ErrorCode.ERR_EndifDirectiveExpected, ""));
        }

        // Preprocessor:
        [Fact]
        public void CS1028ERR_UnexpectedDirectivepp()
        {
            var test = @"
class Test
{
  #endregion
    public static int Main()
    {
        return 0;
    }
#  endif
}
";

            ParseAndValidate(test,
    // (4,3): error CS1028: Unexpected preprocessor directive
    //   #endregion
    Diagnostic(ErrorCode.ERR_UnexpectedDirective, "#endregion"),
    // (9,1): error CS1028: Unexpected preprocessor directive
    // #  endif
    Diagnostic(ErrorCode.ERR_UnexpectedDirective, "#  endif"));
        }

        // Preprocessor:
        [Fact]
        public void CS1029ERR_ErrorDirectivepp()
        {
            var test = @"
public class Test
{
# error  (12345)
}
";

            ParseAndValidate(test, Diagnostic(ErrorCode.ERR_ErrorDirective, "(12345)").WithArguments("(12345)"));
        }

        [WorkItem(541954, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/541954")]
        [Fact]
        public void CS1029ERR_ErrorDirectiveppNonLatin()
        {
            var test = "public class Test\r\n{\r\n# error \u0444\u0430\u0439\u043B\r\n}";
            var parsedTree = ParseWithRoundTripCheck(test);
            var error = parsedTree.GetDiagnostics().Single();
            Assert.Equal((int)ErrorCode.ERR_ErrorDirective, error.Code);
            Assert.Equal("error CS1029: #error: '\u0444\u0430\u0439\u043B'", CSharpDiagnosticFormatter.Instance.Format(error.WithLocation(Location.None), EnsureEnglishUICulture.PreferredOrNull));
        }

        [Fact(), WorkItem(526991, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/526991")]
        public void CS1031ERR_TypeExpected01()
        {
            // Diff error - CS1003
            var test = @"
namespace x
{
    interface ii
    {
        int i
        {
            get;
        }
    }
    public class a 
    {
        public operator ii(a aa)
        {
            return new ii();
        }
    }
}
";

            ParseAndValidate(test,
                // (13,16): error CS1003: Syntax error, 'explicit' expected
                //         public operator ii(a aa)
                Diagnostic(ErrorCode.ERR_SyntaxError, "operator").WithArguments("explicit")
                );
        }

        [Fact]
        public void CS1031ERR_TypeExpected02()
        {
            var text = @"namespace x
{
    public class a
    {
        public static void Main()
        {
            e = new base;   // CS1031, not a type
            e = new this;   // CS1031, not a type
        }
    }
}
";

            ParseAndValidate(text, TestOptions.Regular,
                // (7,21): error CS1526: A new expression requires an argument list or (), [], or {} after type
                //             e = new base;   // CS1031, not a type
                Diagnostic(ErrorCode.ERR_BadNewExpr, "base").WithLocation(7, 21),
                // (7,21): error CS1002: ; expected
                //             e = new base;   // CS1031, not a type
                Diagnostic(ErrorCode.ERR_SemicolonExpected, "base").WithLocation(7, 21),
                // (8,21): error CS1526: A new expression requires an argument list or (), [], or {} after type
                //             e = new this;   // CS1031, not a type
                Diagnostic(ErrorCode.ERR_BadNewExpr, "this").WithLocation(8, 21),
                // (8,21): error CS1002: ; expected
                //             e = new this;   // CS1031, not a type
                Diagnostic(ErrorCode.ERR_SemicolonExpected, "this").WithLocation(8, 21));
        }

        [Fact]
        public void CS1031ERR_TypeExpected02_Tuple()
        {
            var text = @"namespace x
{
    public class @a
    {
        public static void Main()
        {
            var e = new ();
        }
    }
}
";

            CreateCompilationWithMscorlib46(text, parseOptions: TestOptions.Regular8).VerifyDiagnostics(
                // (7,21): error CS8400: Feature 'target-typed object creation' is not available in C# 8.0. Please use language version 9.0 or greater.
                //             var e = new ();
                Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion8, "new").WithArguments("target-typed object creation", "9.0").WithLocation(7, 21),
                // (7,21): error CS8754: There is no target type for 'new()'
                //             var e = new ();
                Diagnostic(ErrorCode.ERR_ImplicitObjectCreationNoTargetType, "new ()").WithArguments("new()").WithLocation(7, 21));
        }

        [Fact]
        public void CS1031ERR_TypeExpected02WithCSharp6()
        {
            var text = @"namespace x
{
    public class a
    {
        public static void Main()
        {
            e = new base;   // CS1031, not a type
            e = new this;   // CS1031, not a type
        }
    }
}
";
            // TODO: this appears to be a severe regression from Dev10, which neatly reported 3 errors.
            ParseAndValidate(text, TestOptions.Regular.WithLanguageVersion(LanguageVersion.CSharp6),
                // (7,21): error CS1526: A new expression requires an argument list or (), [], or {} after type
                //             e = new base;   // CS1031, not a type
                Diagnostic(ErrorCode.ERR_BadNewExpr, "base").WithLocation(7, 21),
                // (7,21): error CS1002: ; expected
                //             e = new base;   // CS1031, not a type
                Diagnostic(ErrorCode.ERR_SemicolonExpected, "base").WithLocation(7, 21),
                // (8,21): error CS1526: A new expression requires an argument list or (), [], or {} after type
                //             e = new this;   // CS1031, not a type
                Diagnostic(ErrorCode.ERR_BadNewExpr, "this").WithLocation(8, 21),
                // (8,21): error CS1002: ; expected
                //             e = new this;   // CS1031, not a type
                Diagnostic(ErrorCode.ERR_SemicolonExpected, "this").WithLocation(8, 21));
        }

        [Fact]
        public void CS1031ERR_TypeExpected02WithCSharp6_Tuple()
        {
            var text = @"namespace x
{
    public class @a
    {
        public static void Main()
        {
            var e = new ();
        }
    }
}
";
            CreateCompilationWithMscorlib46(text, parseOptions: TestOptions.Regular.WithLanguageVersion(LanguageVersion.CSharp6)).VerifyDiagnostics(
                // (7,21): error CS8059: Feature 'target-typed object creation' is not available in C# 6. Please use language version 9.0 or greater.
                //             var e = new ();
                Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion6, "new").WithArguments("target-typed object creation", "9.0").WithLocation(7, 21),
                // (7,21): error CS8754: There is no target type for 'new()'
                //             var e = new ();
                Diagnostic(ErrorCode.ERR_ImplicitObjectCreationNoTargetType, "new ()").WithArguments("new()").WithLocation(7, 21));
        }

        [Fact]
        public void CS1031ERR_TypeExpected02WithCSharp7_Tuple()
        {
            var text = @"namespace x
{
    public class @a
    {
        public static void Main()
        {
            var e = new ();
        }
    }
}
";
            CreateCompilationWithMscorlib46(text, parseOptions: TestOptions.Regular.WithLanguageVersion(LanguageVersion.CSharp7)).VerifyDiagnostics(
                // (7,21): error CS8107: Feature 'target-typed object creation' is not available in C# 7.0. Please use language version 9.0 or greater.
                //             var e = new ();
                Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion7, "new").WithArguments("target-typed object creation", "9.0").WithLocation(7, 21),
                // (7,21): error CS8754: There is no target type for 'new()'
                //             var e = new ();
                Diagnostic(ErrorCode.ERR_ImplicitObjectCreationNoTargetType, "new ()").WithArguments("new()").WithLocation(7, 21));
        }

        [WorkItem(541347, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/541347")]
        [Fact]
        public void CS1031ERR_TypeExpected03()
        {
            var test = @"
using System;
public class Extensions 
{ 
   //Extension method must be static 
   public Extensions(this int i) {} 
   public static void Main(){} 
} 
";
            CreateCompilation(test).VerifyDiagnostics(
                // (6,22): error CS0027: Keyword 'this' is not available in the current context
                //    public Extensions(this int i) {} 
                Diagnostic(ErrorCode.ERR_ThisInBadContext, "this").WithLocation(6, 22),
                // (2,1): hidden CS8019: Unnecessary using directive.
                // using System;
                Diagnostic(ErrorCode.HDN_UnusedUsingDirective, "using System;").WithLocation(2, 1));
        }

        [Fact]
        public void CS1031ERR_TypeExpected04_RoslynCS1001()
        {
            var test = @"public struct S<> 
{
    public void M<>() {}
}
";

            ParseAndValidate(test,
                // (1,17): error CS1001: Identifier expected
                // public struct S<> 
                Diagnostic(ErrorCode.ERR_IdentifierExpected, ">"),
                // (3,19): error CS1001: Identifier expected
                //     public void M<>() {}
                Diagnostic(ErrorCode.ERR_IdentifierExpected, ">"));
        }

        [Fact]
        public void CS1037ERR_OvlOperatorExpected()
        {
            var test = @"
class A
{
    public static int explicit operator ()
    {
        return 0;
    }
    public static A operator ()
    {
        return null;
    }
}";
            ParseAndValidate(test, TestOptions.Regular,
                // (4,19): error CS1553: Declaration is not valid; use '+ operator <dest-type> (...' instead
                //     public static int explicit operator ()
                Diagnostic(ErrorCode.ERR_BadOperatorSyntax, "int").WithArguments("+").WithLocation(4, 19),
                // (4,23): error CS1003: Syntax error, 'operator' expected
                //     public static int explicit operator ()
                Diagnostic(ErrorCode.ERR_SyntaxError, "explicit").WithArguments("operator").WithLocation(4, 23),
                // (4,23): error CS1019: Overloadable unary operator expected
                //     public static int explicit operator ()
                Diagnostic(ErrorCode.ERR_OvlUnaryOperatorExpected, "explicit").WithLocation(4, 23),
                // (4,32): error CS1003: Syntax error, '(' expected
                //     public static int explicit operator ()
                Diagnostic(ErrorCode.ERR_SyntaxError, "operator").WithArguments("(").WithLocation(4, 32),
                // (4,32): error CS1041: Identifier expected; 'operator' is a keyword
                //     public static int explicit operator ()
                Diagnostic(ErrorCode.ERR_IdentifierExpectedKW, "operator").WithArguments("", "operator").WithLocation(4, 32),
                // (4,42): error CS8124: Tuple must contain at least two elements.
                //     public static int explicit operator ()
                Diagnostic(ErrorCode.ERR_TupleTooFewElements, ")").WithLocation(4, 42),
                // (4,43): error CS1001: Identifier expected
                //     public static int explicit operator ()
                Diagnostic(ErrorCode.ERR_IdentifierExpected, "").WithLocation(4, 43),
                // (4,43): error CS1003: Syntax error, ',' expected
                //     public static int explicit operator ()
                Diagnostic(ErrorCode.ERR_SyntaxError, "").WithArguments(",").WithLocation(4, 43),
                // (6,17): error CS1026: ) expected
                //         return 0;
                Diagnostic(ErrorCode.ERR_CloseParenExpected, ";").WithLocation(6, 17),
                // (8,30): error CS1037: Overloadable operator expected
                //     public static A operator ()
                Diagnostic(ErrorCode.ERR_OvlOperatorExpected, "(").WithLocation(8, 30),
                // (8,31): error CS1003: Syntax error, '(' expected
                //     public static A operator ()
                Diagnostic(ErrorCode.ERR_SyntaxError, ")").WithArguments("(").WithLocation(8, 31),
                // (12,1): error CS1022: Type or namespace definition, or end-of-file expected
                // }
                Diagnostic(ErrorCode.ERR_EOFExpected, "}").WithLocation(12, 1)
                );
        }

        [Fact]
        public void CS1037ERR_OvlOperatorExpectedWithCSharp6()
        {
            var test = @"
class A
{
    public static int explicit operator ()
    {
        return 0;
    }
    public static A operator ()
    {
        return null;
    }
}";
            CreateCompilation(test, parseOptions: TestOptions.Regular6).VerifyDiagnostics(
                // (4,19): error CS1553: Declaration is not valid; use '+ operator <dest-type> (...' instead
                //     public static int explicit operator ()
                Diagnostic(ErrorCode.ERR_BadOperatorSyntax, "int").WithArguments("+").WithLocation(4, 19),
                // (4,23): error CS1003: Syntax error, 'operator' expected
                //     public static int explicit operator ()
                Diagnostic(ErrorCode.ERR_SyntaxError, "explicit").WithArguments("operator").WithLocation(4, 23),
                // (4,23): error CS1019: Overloadable unary operator expected
                //     public static int explicit operator ()
                Diagnostic(ErrorCode.ERR_OvlUnaryOperatorExpected, "explicit").WithLocation(4, 23),
                // (4,23): error CS0501: 'A.operator +((?, ?))' must declare a body because it is not marked abstract, extern, or partial
                //     public static int explicit operator ()
                Diagnostic(ErrorCode.ERR_ConcreteMissingBody, "").WithArguments("A.operator +((?, ?))").WithLocation(4, 23),
                // (4,23): error CS0562: The parameter of a unary operator must be the containing type
                //     public static int explicit operator ()
                Diagnostic(ErrorCode.ERR_BadUnaryOperatorSignature, "").WithLocation(4, 23),
                // (4,32): error CS1003: Syntax error, '(' expected
                //     public static int explicit operator ()
                Diagnostic(ErrorCode.ERR_SyntaxError, "operator").WithArguments("(").WithLocation(4, 32),
                // (4,32): error CS1041: Identifier expected; 'operator' is a keyword
                //     public static int explicit operator ()
                Diagnostic(ErrorCode.ERR_IdentifierExpectedKW, "operator").WithArguments("", "operator").WithLocation(4, 32),
                // (4,41): error CS8059: Feature 'tuples' is not available in C# 6. Please use language version 7.0 or greater.
                //     public static int explicit operator ()
                Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion6, "()").WithArguments("tuples", "7.0").WithLocation(4, 41),
                // (4,42): error CS8124: Tuple must contain at least two elements.
                //     public static int explicit operator ()
                Diagnostic(ErrorCode.ERR_TupleTooFewElements, ")").WithLocation(4, 42),
                // (4,43): error CS1001: Identifier expected
                //     public static int explicit operator ()
                Diagnostic(ErrorCode.ERR_IdentifierExpected, "").WithLocation(4, 43),
                // (4,43): error CS1003: Syntax error, ',' expected
                //     public static int explicit operator ()
                Diagnostic(ErrorCode.ERR_SyntaxError, "").WithArguments(",").WithLocation(4, 43),
                // (6,17): error CS1026: ) expected
                //         return 0;
                Diagnostic(ErrorCode.ERR_CloseParenExpected, ";").WithLocation(6, 17),
                // (8,30): error CS1037: Overloadable operator expected
                //     public static A operator ()
                Diagnostic(ErrorCode.ERR_OvlOperatorExpected, "(").WithLocation(8, 30),
                // (8,31): error CS1003: Syntax error, '(' expected
                //     public static A operator ()
                Diagnostic(ErrorCode.ERR_SyntaxError, ")").WithArguments("(").WithLocation(8, 31),
                // (12,1): error CS1022: Type or namespace definition, or end-of-file expected
                // }
                Diagnostic(ErrorCode.ERR_EOFExpected, "}").WithLocation(12, 1));

            ParseAndValidate(test, TestOptions.Regular.WithLanguageVersion(LanguageVersion.CSharp6),
                // (4,19): error CS1553: Declaration is not valid; use '+ operator <dest-type> (...' instead
                //     public static int explicit operator ()
                Diagnostic(ErrorCode.ERR_BadOperatorSyntax, "int").WithArguments("+").WithLocation(4, 19),
                // (4,23): error CS1003: Syntax error, 'operator' expected
                //     public static int explicit operator ()
                Diagnostic(ErrorCode.ERR_SyntaxError, "explicit").WithArguments("operator").WithLocation(4, 23),
                // (4,23): error CS1019: Overloadable unary operator expected
                //     public static int explicit operator ()
                Diagnostic(ErrorCode.ERR_OvlUnaryOperatorExpected, "explicit").WithLocation(4, 23),
                // (4,32): error CS1003: Syntax error, '(' expected
                //     public static int explicit operator ()
                Diagnostic(ErrorCode.ERR_SyntaxError, "operator").WithArguments("(").WithLocation(4, 32),
                // (4,32): error CS1041: Identifier expected; 'operator' is a keyword
                //     public static int explicit operator ()
                Diagnostic(ErrorCode.ERR_IdentifierExpectedKW, "operator").WithArguments("", "operator").WithLocation(4, 32),
                // (4,41): error CS8059: Feature 'tuples' is not available in C# 6. Please use language version 7.0 or greater.
                //     public static int explicit operator ()
                Diagnostic(ErrorCode.ERR_TupleTooFewElements, ")").WithLocation(4, 42),
                // (4,43): error CS1001: Identifier expected
                //     public static int explicit operator ()
                Diagnostic(ErrorCode.ERR_IdentifierExpected, "").WithLocation(4, 43),
                // (4,43): error CS1003: Syntax error, ',' expected
                //     public static int explicit operator ()
                Diagnostic(ErrorCode.ERR_SyntaxError, "").WithArguments(",").WithLocation(4, 43),
                // (6,17): error CS1026: ) expected
                //         return 0;
                Diagnostic(ErrorCode.ERR_CloseParenExpected, ";").WithLocation(6, 17),
                // (8,30): error CS1037: Overloadable operator expected
                //     public static A operator ()
                Diagnostic(ErrorCode.ERR_OvlOperatorExpected, "(").WithLocation(8, 30),
                // (8,31): error CS1003: Syntax error, '(' expected
                //     public static A operator ()
                Diagnostic(ErrorCode.ERR_SyntaxError, ")").WithArguments("(").WithLocation(8, 31),
                // (12,1): error CS1022: Type or namespace definition, or end-of-file expected
                // }
                Diagnostic(ErrorCode.ERR_EOFExpected, "}").WithLocation(12, 1));
        }

        // Preprocessor:
        [Fact]
        public void CS1038ERR_EndRegionDirectiveExpectedpp()
        {
            var test = @"
class Test
{
# region
}
";

            ParseAndValidate(test, Diagnostic(ErrorCode.ERR_EndRegionDirectiveExpected, ""));
        }

        [Fact, WorkItem(535926, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/535926")]
        public void CS1041ERR_IdentifierExpectedKW()
        {
            // Diff errors
            var test = @"
class MyClass {
    public void f(int long) {    // CS1041
    }
    public static int Main() {
        return  1;
    }
}
";

            ParseAndValidate(test,
    // (3,23): error CS1001: Identifier expected
    //     public void f(int long) {    // CS1041
    Diagnostic(ErrorCode.ERR_IdentifierExpected, "long"),
    // (3,23): error CS1003: Syntax error, ',' expected
    //     public void f(int long) {    // CS1041
    Diagnostic(ErrorCode.ERR_SyntaxError, "long").WithArguments(","),
    // (3,27): error CS1001: Identifier expected
    //     public void f(int long) {    // CS1041
    Diagnostic(ErrorCode.ERR_IdentifierExpected, ")"));
        }

        [WorkItem(919476, "DevDiv/Personal")]
        [Fact]
        public void CS1041RegressKeywordInEnumField()
        {
            var test = @"enum ColorA 
{
    const Red,
    Green = 10,
    readonly Blue,
}";

            ParseAndValidate(test,
Diagnostic(ErrorCode.ERR_IdentifierExpectedKW, "").WithArguments("", "const"),
Diagnostic(ErrorCode.ERR_IdentifierExpectedKW, "").WithArguments("", "readonly"));
        }

        [Fact, WorkItem(541347, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/541347")]
        public void CS1041ERR_IdentifierExpectedKW02()
        {
            var test =
@"class C
{
    C(this object o) { }
}";

            CreateCompilation(test).VerifyDiagnostics(
                // (3,7): error CS0027: Keyword 'this' is not available in the current context
                //     C(this object o) { }
                Diagnostic(ErrorCode.ERR_ThisInBadContext, "this").WithLocation(3, 7));
        }

        [Fact, WorkItem(541347, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/541347")]
        public void CS1041ERR_IdentifierExpectedKW03()
        {
            var test =
@"class C
{
    object this[this object o]
    {
        get { return null; }
    }
}";
            CreateCompilation(test).VerifyDiagnostics(
                // (3,17): error CS0027: Keyword 'this' is not available in the current context
                //     object this[this object o]
                Diagnostic(ErrorCode.ERR_ThisInBadContext, "this").WithLocation(3, 17));
        }

        [Fact, WorkItem(541347, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/541347")]
        public void CS1041ERR_IdentifierExpectedKW04()
        {
            var test = @"delegate void D(this object o);";

            CreateCompilation(test).VerifyDiagnostics(
                // (1,17): error CS0027: Keyword 'this' is not available in the current context
                // delegate void D(this object o);
                Diagnostic(ErrorCode.ERR_ThisInBadContext, "this").WithLocation(1, 17));
        }

        [Fact, WorkItem(541347, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/541347")]
        public void CS1041ERR_IdentifierExpectedKW05()
        {
            var test =
@"delegate void D(object o);
class C
{
    static void M()
    {
        D d = delegate (this object o) { };
    }
}";
            CreateCompilation(test).VerifyDiagnostics(
                // (6,25): error CS0027: Keyword 'this' is not available in the current context
                //         D d = delegate (this object o) { };
                Diagnostic(ErrorCode.ERR_ThisInBadContext, "this").WithLocation(6, 25));
        }

        [Fact]
        public void ERR_ThisInBadContext01()
        {
            var test =
@"class C
{
    public static implicit operator int(this C c) { return 0; }
    public static C operator +(this C c1, C c2) { return null; }
}";
            CreateCompilation(test).VerifyDiagnostics(
                // (4,32): error CS0027: Keyword 'this' is not available in the current context
                //     public static C operator +(this C c1, C c2) { return null; }
                Diagnostic(ErrorCode.ERR_ThisInBadContext, "this").WithLocation(4, 32),
                // (3,41): error CS0027: Keyword 'this' is not available in the current context
                //     public static implicit operator int(this C c) { return 0; }
                Diagnostic(ErrorCode.ERR_ThisInBadContext, "this").WithLocation(3, 41));
        }

        [Fact, WorkItem(541347, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/541347")]
        public void CS1041ERR_IdentifierExpectedKW06()
        {
            var test =
@"delegate object D(object o);
class C
{
    static void M()
    {
        D d = (this object o) => null;
    }
}";
            ParseAndValidate(test,
                // (6,16): error CS1026: ) expected
                //         D d = (this object o) => null;
                Diagnostic(ErrorCode.ERR_CloseParenExpected, "this").WithLocation(6, 16),
                // (6,16): error CS1003: Syntax error, '=>' expected
                //         D d = (this object o) => null;
                Diagnostic(ErrorCode.ERR_SyntaxError, "this").WithArguments("=>").WithLocation(6, 16),
                // (6,21): error CS1002: ; expected
                //         D d = (this object o) => null;
                Diagnostic(ErrorCode.ERR_SemicolonExpected, "object").WithLocation(6, 21),
                // (6,29): error CS1003: Syntax error, ',' expected
                //         D d = (this object o) => null;
                Diagnostic(ErrorCode.ERR_SyntaxError, ")").WithArguments(",").WithLocation(6, 29),
                // (6,34): error CS1002: ; expected
                //         D d = (this object o) => null;
                Diagnostic(ErrorCode.ERR_SemicolonExpected, "null").WithLocation(6, 34));
        }

        // TODO: extra error CS1014
        [Fact]
        public void CS7887ERR_SemiOrLBraceOrArrowExpected()
        {
            var test = @"
using System;
public class Test
{
    public    int Prop 
    {
        get return 1;
}
public static int Main()
{
return 1;
}
}
";

            ParseAndValidate(test,
    // (7,13): error CS7887: { or ; or => expected
    //         get return 1;
    Diagnostic(ErrorCode.ERR_SemiOrLBraceOrArrowExpected, "return"),
    // (8,2): error CS1513: } expected
    Diagnostic(ErrorCode.ERR_RbraceExpected, ""));
        }

        [Fact]
        public void CS1044ERR_MultiTypeInDeclaration()
        {
            var test = @"
using System;

// two normal classes...
public class Res1 : IDisposable
{
    public void Dispose()
    {
    }
    public void Func()
    {
    }
    public void Throw()
    {
    }
}

public class Res2 : IDisposable
{
    public void Dispose()
    {
    }
    public void Func()
    {
    }
    public void Throw()
    {
    }
}

public class Test
{
    public static int Main()
    {
    using (    Res1 res1 = new Res1(), 
        Res2 res2 = new Res2())
        {
            res1.Func();
            res2.Func();
        }
    return 1;
    }
}
";
            // Extra Errors
            ParseAndValidate(test,
    // (36,9): error CS1044: Cannot use more than one type in a for, using, fixed, or declaration statement
    //         Res2 res2 = new Res2())
    Diagnostic(ErrorCode.ERR_MultiTypeInDeclaration, "Res2"),
    // (36,14): error CS1026: ) expected
    //         Res2 res2 = new Res2())
    Diagnostic(ErrorCode.ERR_CloseParenExpected, "res2"),
    // (36,31): error CS1002: ; expected
    //         Res2 res2 = new Res2())
    Diagnostic(ErrorCode.ERR_SemicolonExpected, ")"),
    // (36,31): error CS1513: } expected
    //         Res2 res2 = new Res2())
    Diagnostic(ErrorCode.ERR_RbraceExpected, ")"));
        }

        [WorkItem(863395, "DevDiv/Personal")]
        [Fact]
        public void CS1055ERR_AddOrRemoveExpected()
        {
            // TODO: extra errors
            var test = @"
delegate void del();
class Test
{
    public event del MyEvent
    {
        return value; 
}
public static int Main()
{
return 1;
}
}
";

            ParseAndValidate(test,
                // (7,9): error CS1055: An add or remove accessor expected
                //         return value; 
                Diagnostic(ErrorCode.ERR_AddOrRemoveExpected, "return"),
                // (7,16): error CS1055: An add or remove accessor expected
                //         return value; 
                Diagnostic(ErrorCode.ERR_AddOrRemoveExpected, "value"));
        }

        [WorkItem(536956, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/536956")]
        [Fact]
        public void CS1065ERR_DefaultValueNotAllowed()
        {
            var test = @"
class A
{
    delegate void D(int x);    
    D d1 = delegate(int x = 42) { };
}
";

            CreateCompilation(test).VerifyDiagnostics(
                    // (5,27): error CS1065: Default values are not valid in this context.
                    //     D d1 = delegate(int x = 42) { };
                    Diagnostic(ErrorCode.ERR_DefaultValueNotAllowed, "=").WithLocation(5, 27));
        }

        [Fact]
        public void CS1065ERR_DefaultValueNotAllowed_2()
        {
            var test = @"
class A
{
    delegate void D(int x, int y);    
    D d1 = delegate(int x, int y = 42) { };
}
";

            CreateCompilation(test).VerifyDiagnostics(
                    // (5,34): error CS1065: Default values are not valid in this context.
                    //     D d1 = delegate(int x, int y = 42) { };
                    Diagnostic(ErrorCode.ERR_DefaultValueNotAllowed, "=").WithLocation(5, 34));
        }

        [Fact, WorkItem(540251, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/540251")]
        public void CS7014ERR_AttributesNotAllowed()
        {
            var test = @"
using System;

class Program
{
    static void Main()
    {
        const string message = ""the parameter is obsolete"";
        Action<int, int> a = delegate (
            [ObsoleteAttribute(message)] [ObsoleteAttribute(message)] int x,
            [ObsoleteAttribute(message)] int y
        ) { };
    }
}
";

            CreateCompilation(test).VerifyDiagnostics(
                // (8,22): warning CS0219: The variable 'message' is assigned but its value is never used
                //         const string message = "the parameter is obsolete";
                Diagnostic(ErrorCode.WRN_UnreferencedVarAssg, "message").WithArguments("message").WithLocation(8, 22),
                // (10,13): error CS7014: Attributes are not valid in this context.
                //             [ObsoleteAttribute(message)] [ObsoleteAttribute(message)] int x,
                Diagnostic(ErrorCode.ERR_AttributesNotAllowed, "[ObsoleteAttribute(message)]").WithLocation(10, 13),
                // (10,42): error CS7014: Attributes are not valid in this context.
                //             [ObsoleteAttribute(message)] [ObsoleteAttribute(message)] int x,
                Diagnostic(ErrorCode.ERR_AttributesNotAllowed, "[ObsoleteAttribute(message)]").WithLocation(10, 42),
                // (11,13): error CS7014: Attributes are not valid in this context.
                //             [ObsoleteAttribute(message)] int y
                Diagnostic(ErrorCode.ERR_AttributesNotAllowed, "[ObsoleteAttribute(message)]").WithLocation(11, 13));
        }

        [Fact]
        public void BadRefOrInWithThisParameterModifiers()
        {
            var test = @"
public static class Extensions
{
    public static void M1(ref this ref int i) {}
    public static void M2(ref this in int i) {}
    public static void M3(in this ref int i) {}
    public static void M4(in this in int i) {}
}
";

            CreateCompilationWithMscorlib40AndSystemCore(test).GetDeclarationDiagnostics().Verify(
                // (7,35): error CS1107: A parameter can only have one 'in' modifier
                //     public static void M4(in this in int i) {}
                Diagnostic(ErrorCode.ERR_DupParamMod, "in").WithArguments("in").WithLocation(7, 35),
                // (5,36): error CS8328:  The parameter modifier 'in' cannot be used with 'ref'
                //     public static void M2(ref this in int i) {}
                Diagnostic(ErrorCode.ERR_BadParameterModifiers, "in").WithArguments("in", "ref").WithLocation(5, 36),
                // (6,35): error CS8328:  The parameter modifier 'ref' cannot be used with 'in'
                //     public static void M3(in this ref int i) {}
                Diagnostic(ErrorCode.ERR_BadParameterModifiers, "ref").WithArguments("ref", "in").WithLocation(6, 35),
                // (4,36): error CS1107: A parameter can only have one 'ref' modifier
                //     public static void M1(ref this ref int i) {}
                Diagnostic(ErrorCode.ERR_DupParamMod, "ref").WithArguments("ref").WithLocation(4, 36)
                );
        }

        [WorkItem(906072, "DevDiv/Personal")]
        [Fact]
        public void CS1102ERR_BadOutWithThis()
        {
            var test = @"
using System;
public static class Extensions
{
    //No type parameters
    public static void Goo(this out int i) {}
    //Single type parameter
    public static void Goo<T>(this out T t) {}
    //Multiple type parameters
    public static void Goo<T,U,V>(this out U u) {}
}
public static class GenExtensions<X>
{
    //No type parameters
    public static void Goo(this out int i) {}
    public static void Goo(this out X x) {}
    //Single type parameter
    public static void Goo<T>(this out T t) {}
    public static void Goo<T>(this out X x) {}
    //Multiple type parameters
    public static void Goo<T,U,V>(this out U u) {}
    public static void Goo<T,U,V>(this out X x) {}
}
";

            CreateCompilationWithMscorlib40AndSystemCore(test).GetDeclarationDiagnostics().Verify(
                // (10,40): error CS8328:  The parameter modifier 'out' cannot be used with 'this' 
                //     public static void Foo<T,U,V>(this out U u) {}
                Diagnostic(ErrorCode.ERR_BadParameterModifiers, "out").WithArguments("out", "this"),
                // (8,36): error CS8328:  The parameter modifier 'out' cannot be used with 'this' 
                //     public static void Foo<T>(this out T t) {}
                Diagnostic(ErrorCode.ERR_BadParameterModifiers, "out").WithArguments("out", "this"),
                // (6,33): error CS8328:  The parameter modifier 'out' cannot be used with 'this' 
                //     public static void Foo(this out int i) {}
                Diagnostic(ErrorCode.ERR_BadParameterModifiers, "out").WithArguments("out", "this"),
                // (22,40): error CS8328:  The parameter modifier 'out' cannot be used with 'this' 
                //     public static void Foo<T,U,V>(this out X x) {}
                Diagnostic(ErrorCode.ERR_BadParameterModifiers, "out").WithArguments("out", "this"),
                // (12,21): error CS1106: Extension method must be defined in a non-generic static class
                // public static class GenExtensions<X>
                Diagnostic(ErrorCode.ERR_BadExtensionAgg, "GenExtensions").WithLocation(12, 21),
                // (16,33): error CS8328:  The parameter modifier 'out' cannot be used with 'this' 
                //     public static void Foo(this out X x) {}
                Diagnostic(ErrorCode.ERR_BadParameterModifiers, "out").WithArguments("out", "this"),
                // (12,21): error CS1106: Extension method must be defined in a non-generic static class
                // public static class GenExtensions<X>
                Diagnostic(ErrorCode.ERR_BadExtensionAgg, "GenExtensions").WithLocation(12, 21),
                // (18,36): error CS8328:  The parameter modifier 'out' cannot be used with 'this' 
                //     public static void Foo<T>(this out T t) {}
                Diagnostic(ErrorCode.ERR_BadParameterModifiers, "out").WithArguments("out", "this"),
                // (12,21): error CS1106: Extension method must be defined in a non-generic static class
                // public static class GenExtensions<X>
                Diagnostic(ErrorCode.ERR_BadExtensionAgg, "GenExtensions").WithLocation(12, 21),
                // (19,36): error CS8328:  The parameter modifier 'out' cannot be used with 'this' 
                //     public static void Foo<T>(this out X x) {}
                Diagnostic(ErrorCode.ERR_BadParameterModifiers, "out").WithArguments("out", "this"),
                // (12,21): error CS1106: Extension method must be defined in a non-generic static class
                // public static class GenExtensions<X>
                Diagnostic(ErrorCode.ERR_BadExtensionAgg, "GenExtensions").WithLocation(12, 21),
                // (21,40): error CS8328:  The parameter modifier 'out' cannot be used with 'this' 
                //     public static void Foo<T,U,V>(this out U u) {}
                Diagnostic(ErrorCode.ERR_BadParameterModifiers, "out").WithArguments("out", "this"),
                // (12,21): error CS1106: Extension method must be defined in a non-generic static class
                // public static class GenExtensions<X>
                Diagnostic(ErrorCode.ERR_BadExtensionAgg, "GenExtensions").WithLocation(12, 21),
                // (15,33): error CS8328:  The parameter modifier 'out' cannot be used with 'this' 
                //     public static void Foo(this out int i) {}
                Diagnostic(ErrorCode.ERR_BadParameterModifiers, "out").WithArguments("out", "this"),
                // (12,21): error CS1106: Extension method must be defined in a non-generic static class
                // public static class GenExtensions<X>
                Diagnostic(ErrorCode.ERR_BadExtensionAgg, "GenExtensions").WithLocation(12, 21));
        }

        [WorkItem(863402, "DevDiv/Personal")]
        [Fact]
        public void CS1104ERR_BadParamModThis()
        {
            var test = @"
using System;
public static class Extensions
{
    //No type parameters
    public static void Goo(this params int[] iArr) {}
    //Single type parameter
    public static void Goo<T>(this params T[] tArr) {}
    //Multiple type parameters
    public static void Goo<T,U,V>(this params U[] uArr) {}
}
public static class GenExtensions<X>
{
    //No type parameters
    public static void Goo(this params int[] iArr) {}
    public static void Goo(this params X[] xArr) {}
    //Single type parameter
    public static void Goo<T>(this params T[] tArr) {}
    public static void Goo<T>(this params X[] xArr) {}
    //Multiple type parameters
    public static void Goo<T,U,V>(this params U[] uArr) {}
    public static void Goo<T,U,V>(this params X[] xArr) {}
}
";

            CreateCompilationWithMscorlib40AndSystemCore(test).GetDeclarationDiagnostics().Verify(
                // (22,40): error CS1104: A parameter array cannot be used with 'this' modifier on an extension method
                //     public static void Goo<T,U,V>(this params X[] xArr) {}
                Diagnostic(ErrorCode.ERR_BadParamModThis, "params").WithLocation(22, 40),
                // (12,21): error CS1106: Extension method must be defined in a non-generic static class
                // public static class GenExtensions<X>
                Diagnostic(ErrorCode.ERR_BadExtensionAgg, "GenExtensions").WithLocation(12, 21),
                // (16,33): error CS1104: A parameter array cannot be used with 'this' modifier on an extension method
                //     public static void Goo(this params X[] xArr) {}
                Diagnostic(ErrorCode.ERR_BadParamModThis, "params").WithLocation(16, 33),
                // (12,21): error CS1106: Extension method must be defined in a non-generic static class
                // public static class GenExtensions<X>
                Diagnostic(ErrorCode.ERR_BadExtensionAgg, "GenExtensions").WithLocation(12, 21),
                // (18,36): error CS1104: A parameter array cannot be used with 'this' modifier on an extension method
                //     public static void Goo<T>(this params T[] tArr) {}
                Diagnostic(ErrorCode.ERR_BadParamModThis, "params").WithLocation(18, 36),
                // (12,21): error CS1106: Extension method must be defined in a non-generic static class
                // public static class GenExtensions<X>
                Diagnostic(ErrorCode.ERR_BadExtensionAgg, "GenExtensions").WithLocation(12, 21),
                // (19,36): error CS1104: A parameter array cannot be used with 'this' modifier on an extension method
                //     public static void Goo<T>(this params X[] xArr) {}
                Diagnostic(ErrorCode.ERR_BadParamModThis, "params").WithLocation(19, 36),
                // (12,21): error CS1106: Extension method must be defined in a non-generic static class
                // public static class GenExtensions<X>
                Diagnostic(ErrorCode.ERR_BadExtensionAgg, "GenExtensions").WithLocation(12, 21),
                // (21,40): error CS1104: A parameter array cannot be used with 'this' modifier on an extension method
                //     public static void Goo<T,U,V>(this params U[] uArr) {}
                Diagnostic(ErrorCode.ERR_BadParamModThis, "params").WithLocation(21, 40),
                // (12,21): error CS1106: Extension method must be defined in a non-generic static class
                // public static class GenExtensions<X>
                Diagnostic(ErrorCode.ERR_BadExtensionAgg, "GenExtensions").WithLocation(12, 21),
                // (15,33): error CS1104: A parameter array cannot be used with 'this' modifier on an extension method
                //     public static void Goo(this params int[] iArr) {}
                Diagnostic(ErrorCode.ERR_BadParamModThis, "params").WithLocation(15, 33),
                // (12,21): error CS1106: Extension method must be defined in a non-generic static class
                // public static class GenExtensions<X>
                Diagnostic(ErrorCode.ERR_BadExtensionAgg, "GenExtensions").WithLocation(12, 21),
                // (10,40): error CS1104: A parameter array cannot be used with 'this' modifier on an extension method
                //     public static void Goo<T,U,V>(this params U[] uArr) {}
                Diagnostic(ErrorCode.ERR_BadParamModThis, "params").WithLocation(10, 40),
                // (8,36): error CS1104: A parameter array cannot be used with 'this' modifier on an extension method
                //     public static void Goo<T>(this params T[] tArr) {}
                Diagnostic(ErrorCode.ERR_BadParamModThis, "params").WithLocation(8, 36),
                // (6,33): error CS1104: A parameter array cannot be used with 'this' modifier on an extension method
                //     public static void Goo(this params int[] iArr) {}
                Diagnostic(ErrorCode.ERR_BadParamModThis, "params").WithLocation(6, 33));
        }

        [Fact, WorkItem(535930, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/535930")]
        public void CS1107ERR_DupParamMod()
        {
            var test = @"
using System;
public static class Extensions
{
    //Extension methods
    public static void Goo(this this t) {}
    public static void Goo(this int this) {}
    //Non-extension methods
    public static void Goo(this t) {}
    public static void Goo(int this) {}
}
";
            CreateCompilationWithMscorlib40AndSystemCore(test).GetDeclarationDiagnostics().Verify(
                // (10,32): error CS1100: Method 'Goo' has a parameter modifier 'this' which is not on the first parameter
                //     public static void Goo(int this) {}
                Diagnostic(ErrorCode.ERR_BadThisParam, "this").WithArguments("Goo").WithLocation(10, 32),
                // (7,37): error CS1100: Method 'Goo' has a parameter modifier 'this' which is not on the first parameter
                //     public static void Goo(this int this) {}
                Diagnostic(ErrorCode.ERR_BadThisParam, "this").WithArguments("Goo").WithLocation(7, 37),
                // (9,33): error CS0246: The type or namespace name 't' could not be found (are you missing a using directive or an assembly reference?)
                //     public static void Goo(this t) {}
                Diagnostic(ErrorCode.ERR_SingleTypeNameNotFound, "t").WithArguments("t").WithLocation(9, 33),
                // (6,33): error CS1107: A parameter can only have one 'this' modifier
                //     public static void Goo(this this t) {}
                Diagnostic(ErrorCode.ERR_DupParamMod, "this").WithArguments("this").WithLocation(6, 33),
                // (6,38): error CS0246: The type or namespace name 't' could not be found (are you missing a using directive or an assembly reference?)
                //     public static void Goo(this this t) {}
                Diagnostic(ErrorCode.ERR_SingleTypeNameNotFound, "t").WithArguments("t").WithLocation(6, 38),
                // (9,24): error CS0111: Type 'Extensions' already defines a member called 'Goo' with the same parameter types
                //     public static void Goo(this t) {}
                Diagnostic(ErrorCode.ERR_MemberAlreadyExists, "Goo").WithArguments("Goo", "Extensions").WithLocation(9, 24),
                // (10,24): error CS0111: Type 'Extensions' already defines a member called 'Goo' with the same parameter types
                //     public static void Goo(int this) {}
                Diagnostic(ErrorCode.ERR_MemberAlreadyExists, "Goo").WithArguments("Goo", "Extensions").WithLocation(10, 24));
        }

        [WorkItem(863405, "DevDiv/Personal")]
        [Fact]
        public void CS1108ERR_MultiParamMod()
        {
            var test = @"
using System;
public static class Extensions
{
    //No type parameters
    public static void Goo(ref out int i) {}
    //Single type parameter
    public static void Goo<T>(ref out T t) {}
    //Multiple type parameters
    public static void Goo<T,U,V>(ref out U u) {}
}
public static class GenExtensions<X>
{
    //No type parameters
    public static void Goo(ref out int i) {}
    public static void Goo(ref out X x) {}
    //Single type parameter
    public static void Goo<T>(ref out T t) {}
    public static void Goo<T>(ref out X x) {}
    //Multiple type parameters
    public static void Goo<T,U,V>(ref out U u) {}
    public static void Goo<T,U,V>(ref out X x) {}
}
";

            CreateCompilationWithMscorlib40AndSystemCore(test).GetDeclarationDiagnostics().Verify(
                // (6,32): error CS81250:  The parameter modifier 'out' cannot be used with 'ref' 
                //     public static void Foo(ref out int i) {}
                Diagnostic(ErrorCode.ERR_BadParameterModifiers, "out").WithArguments("out", "ref").WithLocation(6, 32),
                // (8,35): error CS81250:  The parameter modifier 'out' cannot be used with 'ref' 
                //     public static void Foo<T>(ref out T t) {}
                Diagnostic(ErrorCode.ERR_BadParameterModifiers, "out").WithArguments("out", "ref").WithLocation(8, 35),
                // (10,39): error CS81250:  The parameter modifier 'out' cannot be used with 'ref' 
                //     public static void Foo<T,U,V>(ref out U u) {}
                Diagnostic(ErrorCode.ERR_BadParameterModifiers, "out").WithArguments("out", "ref").WithLocation(10, 39),
                // (15,32): error CS81250:  The parameter modifier 'out' cannot be used with 'ref' 
                //     public static void Foo(ref out int i) {}
                Diagnostic(ErrorCode.ERR_BadParameterModifiers, "out").WithArguments("out", "ref").WithLocation(15, 32),
                // (16,32): error CS81250:  The parameter modifier 'out' cannot be used with 'ref' 
                //     public static void Foo(ref out X x) {}
                Diagnostic(ErrorCode.ERR_BadParameterModifiers, "out").WithArguments("out", "ref").WithLocation(16, 32),
                // (18,35): error CS81250:  The parameter modifier 'out' cannot be used with 'ref' 
                //     public static void Foo<T>(ref out T t) {}
                Diagnostic(ErrorCode.ERR_BadParameterModifiers, "out").WithArguments("out", "ref").WithLocation(18, 35),
                // (19,35): error CS81250:  The parameter modifier 'out' cannot be used with 'ref' 
                //     public static void Foo<T>(ref out X x) {}
                Diagnostic(ErrorCode.ERR_BadParameterModifiers, "out").WithArguments("out", "ref").WithLocation(19, 35),
                // (21,39): error CS81250:  The parameter modifier 'out' cannot be used with 'ref' 
                //     public static void Foo<T,U,V>(ref out U u) {}
                Diagnostic(ErrorCode.ERR_BadParameterModifiers, "out").WithArguments("out", "ref").WithLocation(21, 39),
                // (22,39): error CS81250:  The parameter modifier 'out' cannot be used with 'ref' 
                //     public static void Foo<T,U,V>(ref out X x) {}
                Diagnostic(ErrorCode.ERR_BadParameterModifiers, "out").WithArguments("out", "ref").WithLocation(22, 39));
        }

        [Fact]
        [CompilerTrait(CompilerFeature.ReadOnlyReferences)]
        public void DuplicateParameterModifiersWillErrorOut()
        {
            var test = @"
public static class TestType
{
    public static void Test1(ref ref int i) {}
    public static void Test2(out out int i) {}
    public static void Test3(this this int i) {}
    public static void Test4(params params int[] i) {}
    public static void Test5(in in int[] i) {}
}
";

            CreateCompilationWithMscorlib40AndSystemCore(test).GetDeclarationDiagnostics().Verify(
                // (8,33): error CS1107: A parameter can only have one 'in' modifier
                //     public static void Test5(in in int[] i) {}
                Diagnostic(ErrorCode.ERR_DupParamMod, "in").WithArguments("in").WithLocation(8, 33),
                // (5,34): error CS1107: A parameter can only have one 'out' modifier
                //     public static void Test2(out out int i) {}
                Diagnostic(ErrorCode.ERR_DupParamMod, "out").WithArguments("out").WithLocation(5, 34),
                // (6,35): error CS1107: A parameter can only have one 'this' modifier
                //     public static void Test3(this this int i) {}
                Diagnostic(ErrorCode.ERR_DupParamMod, "this").WithArguments("this").WithLocation(6, 35),
                // (7,37): error CS1107: A parameter can only have one 'params' modifier
                //     public static void Test4(params params int[] i) {}
                Diagnostic(ErrorCode.ERR_DupParamMod, "params").WithArguments("params").WithLocation(7, 37),
                // (4,34): error CS1107: A parameter can only have one 'ref' modifier
                //     public static void Test1(ref ref int i) {}
                Diagnostic(ErrorCode.ERR_DupParamMod, "ref").WithArguments("ref").WithLocation(4, 34));
        }

        [Fact]
        [CompilerTrait(CompilerFeature.ReadOnlyReferences)]
        public void BadInWithRefParameterModifiers()
        {
            var test = @"
public class TestType
{
// No type parameters
public static void Method1(in ref int i) { }
public static void Method2(ref in int i) { }

// Single type parameters
public static void Method3<T>(in ref int i) { }
public static void Method4<T>(ref in int i) { }

// Multiple type parameters
public static void Method5<T, U, V>(in ref int i) { }
public static void Method6<T, U, V>(ref in int i) { }
}
";

            CreateCompilationWithMscorlib40AndSystemCore(test).GetDeclarationDiagnostics().Verify(
                // (6,32): error CS8328:  The parameter modifier 'in' cannot be used with 'ref'
                // public static void Method2(ref in int i) { }
                Diagnostic(ErrorCode.ERR_BadParameterModifiers, "in").WithArguments("in", "ref").WithLocation(6, 32),
                // (9,34): error CS8328:  The parameter modifier 'ref' cannot be used with 'in'
                // public static void Method3<T>(in ref int i) { }
                Diagnostic(ErrorCode.ERR_BadParameterModifiers, "ref").WithArguments("ref", "in").WithLocation(9, 34),
                // (10,35): error CS8328:  The parameter modifier 'in' cannot be used with 'ref'
                // public static void Method4<T>(ref in int i) { }
                Diagnostic(ErrorCode.ERR_BadParameterModifiers, "in").WithArguments("in", "ref").WithLocation(10, 35),
                // (13,40): error CS8328:  The parameter modifier 'ref' cannot be used with 'in'
                // public static void Method5<T, U, V>(in ref int i) { }
                Diagnostic(ErrorCode.ERR_BadParameterModifiers, "ref").WithArguments("ref", "in").WithLocation(13, 40),
                // (14,41): error CS8328:  The parameter modifier 'in' cannot be used with 'ref'
                // public static void Method6<T, U, V>(ref in int i) { }
                Diagnostic(ErrorCode.ERR_BadParameterModifiers, "in").WithArguments("in", "ref").WithLocation(14, 41),
                // (5,31): error CS8328:  The parameter modifier 'ref' cannot be used with 'in'
                // public static void Method1(in ref int i) { }
                Diagnostic(ErrorCode.ERR_BadParameterModifiers, "ref").WithArguments("ref", "in").WithLocation(5, 31));
        }

        [Fact]
        [CompilerTrait(CompilerFeature.ReadOnlyReferences)]
        public void InWithThis_ParameterModifiers()
        {
            var test = @"
public static class TestType
{
// No type parameters
public static void Method1(in this int i) { }
public static void Method2(this in int i) { }

// Single type parameters
public static void Method3<T>(in this int i) { }
public static void Method4<T>(this in int i) { }

// Multiple type parameters
public static void Method5<T, U, V>(in this int i) { }
public static void Method6<T, U, V>(this in int i) { }
}
";

            CreateCompilationWithMscorlib40AndSystemCore(test).VerifyDiagnostics();
        }

        [Fact]
        [CompilerTrait(CompilerFeature.ReadOnlyReferences)]
        public void BadInWithParamsParameterModifiers()
        {
            var test = @"
public class TestType
{
// No type parameters
public static void Method1(in params int[] i) { }
public static void Method2(params in int[] i) { }

// Single type parameters
public static void Method3<T>(in params int[] i) { }
public static void Method4<T>(params in int[] i) { }

// Multiple type parameters
public static void Method5<T, U, V>(in params int[] i) { }
public static void Method6<T, U, V>(params in int[] i) { }
}
";

            CreateCompilationWithMscorlib40AndSystemCore(test).GetDeclarationDiagnostics().Verify(
                // (6,35): error CS1611: The params parameter cannot be declared as in
                // public static void Method2(params in int[] i) { }
                Diagnostic(ErrorCode.ERR_ParamsCantBeWithModifier, "in").WithArguments("in").WithLocation(6, 35),
                // (9,34): error CS8328:  The parameter modifier 'params' cannot be used with 'in'
                // public static void Method3<T>(in params int[] i) { }
                Diagnostic(ErrorCode.ERR_BadParameterModifiers, "params").WithArguments("params", "in").WithLocation(9, 34),
                // (10,38): error CS1611: The params parameter cannot be declared as in
                // public static void Method4<T>(params in int[] i) { }
                Diagnostic(ErrorCode.ERR_ParamsCantBeWithModifier, "in").WithArguments("in").WithLocation(10, 38),
                // (13,40): error CS8328:  The parameter modifier 'params' cannot be used with 'in'
                // public static void Method5<T, U, V>(in params int[] i) { }
                Diagnostic(ErrorCode.ERR_BadParameterModifiers, "params").WithArguments("params", "in").WithLocation(13, 40),
                // (14,44): error CS1611: The params parameter cannot be declared as in
                // public static void Method6<T, U, V>(params in int[] i) { }
                Diagnostic(ErrorCode.ERR_ParamsCantBeWithModifier, "in").WithArguments("in").WithLocation(14, 44),
                // (5,31): error CS8328:  The parameter modifier 'params' cannot be used with 'in'
                // public static void Method1(in params int[] i) { }
                Diagnostic(ErrorCode.ERR_BadParameterModifiers, "params").WithArguments("params", "in").WithLocation(5, 31));
        }

        [Fact]
        [CompilerTrait(CompilerFeature.ReadOnlyReferences)]
        public void BadInWithOutParameterModifiers()
        {
            var test = @"
public class TestType
{
// No type parameters
public static void Method1(in out int i) { }
public static void Method2(out in int i) { }

// Single type parameters
public static void Method3<T>(in out int i) { }
public static void Method4<T>(out in int i) { }

// Multiple type parameters
public static void Method5<T, U, V>(in out int i) { }
public static void Method6<T, U, V>(out in int i) { }
}
";

            CreateCompilationWithMscorlib40AndSystemCore(test).GetDeclarationDiagnostics().Verify(
                // (6,32): error CS8328:  The parameter modifier 'in' cannot be used with 'out'
                // public static void Method2(out in int i) { }
                Diagnostic(ErrorCode.ERR_BadParameterModifiers, "in").WithArguments("in", "out").WithLocation(6, 32),
                // (9,34): error CS8328:  The parameter modifier 'out' cannot be used with 'in'
                // public static void Method3<T>(in out int i) { }
                Diagnostic(ErrorCode.ERR_BadParameterModifiers, "out").WithArguments("out", "in").WithLocation(9, 34),
                // (10,35): error CS8328:  The parameter modifier 'in' cannot be used with 'out'
                // public static void Method4<T>(out in int i) { }
                Diagnostic(ErrorCode.ERR_BadParameterModifiers, "in").WithArguments("in", "out").WithLocation(10, 35),
                // (13,40): error CS8328:  The parameter modifier 'out' cannot be used with 'in'
                // public static void Method5<T, U, V>(in out int i) { }
                Diagnostic(ErrorCode.ERR_BadParameterModifiers, "out").WithArguments("out", "in").WithLocation(13, 40),
                // (14,41): error CS8328:  The parameter modifier 'in' cannot be used with 'out'
                // public static void Method6<T, U, V>(out in int i) { }
                Diagnostic(ErrorCode.ERR_BadParameterModifiers, "in").WithArguments("in", "out").WithLocation(14, 41),
                // (5,31): error CS8328:  The parameter modifier 'out' cannot be used with 'in'
                // public static void Method1(in out int i) { }
                Diagnostic(ErrorCode.ERR_BadParameterModifiers, "out").WithArguments("out", "in").WithLocation(5, 31));
        }

        [Fact]
        [CompilerTrait(CompilerFeature.ReadOnlyReferences)]
        public void InParametersAreParsedCorrectly()
        {
            var test = @"
public class Test
{
    public delegate int Delegate(in int a);

    public void Method(in int b)
    {
        void localFunc(in int c) { }

        Delegate lambda = (in int d) => d;

        Delegate anonymousDelegate = delegate (in int e) { return e; };
    }

    public int this [in int f]
    {
        get { return f; }
    }

    public static bool operator ! (in Test g)
    {
        return false;
    }
}
";

            var tree = ParseTree(test, TestOptions.Regular);
            tree.GetDiagnostics().Verify();

            var methodDeclaration = (MethodDeclarationSyntax)tree.GetRoot().DescendantNodes().Single(node => node is MethodDeclarationSyntax);
            Assert.Equal(SyntaxKind.InKeyword, methodDeclaration.ParameterList.Parameters.Single().Modifiers.Single().Kind());

            var delegateDeclaration = (DelegateDeclarationSyntax)tree.GetRoot().DescendantNodes().Single(node => node is DelegateDeclarationSyntax);
            Assert.Equal(SyntaxKind.InKeyword, delegateDeclaration.ParameterList.Parameters.Single().Modifiers.Single().Kind());

            var localFunctionStatement = (LocalFunctionStatementSyntax)tree.GetRoot().DescendantNodes().Single(node => node is LocalFunctionStatementSyntax);
            Assert.Equal(SyntaxKind.InKeyword, localFunctionStatement.ParameterList.Parameters.Single().Modifiers.Single().Kind());

            var lambdaExpression = (ParenthesizedLambdaExpressionSyntax)tree.GetRoot().DescendantNodes().Single(node => node is ParenthesizedLambdaExpressionSyntax);
            Assert.Equal(SyntaxKind.InKeyword, lambdaExpression.ParameterList.Parameters.Single().Modifiers.Single().Kind());

            var anonymousMethodExpression = (AnonymousMethodExpressionSyntax)tree.GetRoot().DescendantNodes().Single(node => node is AnonymousMethodExpressionSyntax);
            Assert.Equal(SyntaxKind.InKeyword, anonymousMethodExpression.ParameterList.Parameters.Single().Modifiers.Single().Kind());

            var indexerDeclaration = (IndexerDeclarationSyntax)tree.GetRoot().DescendantNodes().Single(node => node is IndexerDeclarationSyntax);
            Assert.Equal(SyntaxKind.InKeyword, indexerDeclaration.ParameterList.Parameters.Single().Modifiers.Single().Kind());

            var operatorDeclaration = (OperatorDeclarationSyntax)tree.GetRoot().DescendantNodes().Single(node => node is OperatorDeclarationSyntax);
            Assert.Equal(SyntaxKind.InKeyword, operatorDeclaration.ParameterList.Parameters.Single().Modifiers.Single().Kind());
        }

        [Fact]
        public void CS1513ERR_RbraceExpected()
        {
            var test = @"
struct S {
}
public class a {
    public static int Main() {
        return 1;
}
";

            ParseAndValidate(test, Diagnostic(ErrorCode.ERR_RbraceExpected, ""));
        }

        // Infinite loop 
        [Fact]
        public void CS1514ERR_LbraceExpected()
        {
            var test = @"
namespace x
";

            ParseAndValidate(test, Diagnostic(ErrorCode.ERR_LbraceExpected, ""), Diagnostic(ErrorCode.ERR_RbraceExpected, ""));
        }

        [Fact]
        public void CS1514ERR_LbraceExpected02()
        {
            var test = @"public class S.D 
{
    public string P.P { get; set; }
}
";

            ParseAndValidate(test,
                // (1,15): error CS1514: { expected
                // public class S.D 
                Diagnostic(ErrorCode.ERR_LbraceExpected, ".").WithLocation(1, 15),
                // (1,15): error CS1513: } expected
                // public class S.D 
                Diagnostic(ErrorCode.ERR_RbraceExpected, ".").WithLocation(1, 15),
                // (1,15): error CS1022: Type or namespace definition, or end-of-file expected
                // public class S.D 
                Diagnostic(ErrorCode.ERR_EOFExpected, ".").WithLocation(1, 15),
                // (1,16): error CS8803: Top-level statements must precede namespace and type declarations.
                // public class S.D 
                Diagnostic(ErrorCode.ERR_TopLevelStatementAfterNamespaceOrType, @"D 
{
").WithLocation(1, 16),
                // (1,17): error CS1001: Identifier expected
                // public class S.D 
                Diagnostic(ErrorCode.ERR_IdentifierExpected, "").WithLocation(1, 17),
                // (1,17): error CS1003: Syntax error, ',' expected
                // public class S.D 
                Diagnostic(ErrorCode.ERR_SyntaxError, "").WithArguments(",").WithLocation(1, 17),
                // (2,2): error CS1002: ; expected
                // {
                Diagnostic(ErrorCode.ERR_SemicolonExpected, "").WithLocation(2, 2),
                // (4,1): error CS1022: Type or namespace definition, or end-of-file expected
                // }
                Diagnostic(ErrorCode.ERR_EOFExpected, "}").WithLocation(4, 1));
        }

        [WorkItem(535932, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/535932")]
        [Fact]
        public void CS1515ERR_InExpected()
        {
            // Diff error - CS1003
            var test = @"
using System;
class Test
{
    public static int Main()
    {
        int[] arr = new int[] {1, 2, 3};
        foreach (int x arr)     // CS1515
        {
            Console.WriteLine(x);
        }
        return 1;
    }
}
";

            ParseAndValidate(test, Diagnostic(ErrorCode.ERR_InExpected, "arr"));
        }

        [Fact]
        public void CS1515ERR_InExpected02()
        {
            var test = @"
class C
{
    static void Main()
    {
        foreach (1)
            System.Console.WriteLine(1);
    }
}
";

            ParseAndValidate(test,
                // (6,19): error CS1515: 'in' expected
                //         foreach (1)
                Diagnostic(ErrorCode.ERR_InExpected, ")").WithLocation(6, 19),
                // (6,19): error CS0230: Type and identifier are both required in a foreach statement
                //         foreach (1)
                Diagnostic(ErrorCode.ERR_BadForeachDecl, ")").WithLocation(6, 19),
                // (6,19): error CS1525: Invalid expression term ')'
                //         foreach (1)
                Diagnostic(ErrorCode.ERR_InvalidExprTerm, ")").WithArguments(")").WithLocation(6, 19)
                );
        }

        [WorkItem(906503, "DevDiv/Personal")]
        [Fact]
        public void CS1517ERR_InvalidPreprocExprpp()
        {
            var test = @"
class Test
{
#if 1=2
#endif
    public static int Main()
    {
#if 0
        return 0;
#endif
    }
}
";
            // TODO: Extra errors
            ParseAndValidate(test,
    // (4,5): error CS1517: Invalid preprocessor expression
    // #if 1=2
    Diagnostic(ErrorCode.ERR_InvalidPreprocExpr, "1"),
    // (4,5): error CS1025: Single-line comment or end-of-line expected
    // #if 1=2
    Diagnostic(ErrorCode.ERR_EndOfPPLineExpected, "1"),
    // (8,5): error CS1517: Invalid preprocessor expression
    // #if 0
    Diagnostic(ErrorCode.ERR_InvalidPreprocExpr, "0"),
    // (8,5): error CS1025: Single-line comment or end-of-line expected
    // #if 0
    Diagnostic(ErrorCode.ERR_EndOfPPLineExpected, "0"));
        }

        // TODO: Extra errors
        [Fact]
        public void CS1519ERR_InvalidMemberDecl_1()
        {
            var test = @"
namespace x
    {
    public void f() {}
    public class C
        {
        return 1;
        }
    }
";
            // member declarations in namespace trigger semantic error, not parse error:
            ParseAndValidate(test, Diagnostic(ErrorCode.ERR_InvalidMemberDecl, "return").WithArguments("return"));
        }

        [Fact]
        public void CS1519ERR_InvalidMemberDecl_2()
        {
            var test = @"
public class C
{
    int[] i = new int[5];;
}
public class D
{
    public static int Main ()
        {
        return 1;
        }
}
";

            ParseAndValidate(test, Diagnostic(ErrorCode.ERR_InvalidMemberDecl, ";").WithArguments(";"));
        }

        [Fact]
        public void CS1519ERR_InvalidMemberDecl_3()
        {
            var test = @"
struct s1
{
    goto Labl; // Invalid
    const int x = 1;
    Lab1:
    const int y = 2;
}
";
            // Extra errors
            ParseAndValidate(test,
    // (4,5): error CS1519: Invalid token 'goto' in class, record, struct, or interface member declaration
    //     goto Labl; // Invalid
    Diagnostic(ErrorCode.ERR_InvalidMemberDecl, "goto").WithArguments("goto"),
    // (4,14): error CS1519: Invalid token ';' in class, record, struct, or interface member declaration
    //     goto Labl; // Invalid
    Diagnostic(ErrorCode.ERR_InvalidMemberDecl, ";").WithArguments(";"),
    // (4,14): error CS1519: Invalid token ';' in class, record, struct, or interface member declaration
    //     goto Labl; // Invalid
    Diagnostic(ErrorCode.ERR_InvalidMemberDecl, ";").WithArguments(";"),
    // (6,9): error CS1519: Invalid token ':' in class, record, struct, or interface member declaration
    //     Lab1:
    Diagnostic(ErrorCode.ERR_InvalidMemberDecl, ":").WithArguments(":"),
    // (6,9): error CS1519: Invalid token ':' in class, record, struct, or interface member declaration
    //     Lab1:
    Diagnostic(ErrorCode.ERR_InvalidMemberDecl, ":").WithArguments(":"));
        }

        [Fact]
        public void CS1520ERR_MemberNeedsType()
        {
            var test = @"
namespace x {
    public class clx {
        public int i;
        public static int Main(){return 0;}
    }
    public class clz 
    {
        public x(){}
    }
}
";

            ParseAndValidate(test);
        }

        [Fact]
        public void CS1521ERR_BadBaseType()
        {
            var test = @"
class Test1{}
class Test2 : Test1[]   // CS1521
{
}
class Test3 : Test1*    // CS1521
{
}
class Program
{
    static int Main()
    {
        return -1;
    }
}
";
            // note: ErrorCode.ManagedAddr not given for Test1* because the base type after binding is considered to be System.Object
            CreateCompilation(test).GetDeclarationDiagnostics().Verify(
                // (6,15): error CS1521: Invalid base type
                // class Test3 : Test1*    // CS1521
                Diagnostic(ErrorCode.ERR_BadBaseType, "Test1*").WithLocation(6, 15),
                // (6,15): error CS0214: Pointers and fixed size buffers may only be used in an unsafe context
                // class Test3 : Test1*    // CS1521
                Diagnostic(ErrorCode.ERR_UnsafeNeeded, "Test1*").WithLocation(6, 15),
                // (6,15): error CS0527: Type 'Test1*' in interface list is not an interface
                // class Test3 : Test1*    // CS1521
                Diagnostic(ErrorCode.ERR_NonInterfaceInInterfaceList, "Test1*").WithArguments("Test1*").WithLocation(6, 15),
                // (3,15): error CS1521: Invalid base type
                // class Test2 : Test1[]   // CS1521
                Diagnostic(ErrorCode.ERR_BadBaseType, "Test1[]").WithLocation(3, 15),
                // (3,15): error CS0527: Type 'Test1[]' in interface list is not an interface
                // class Test2 : Test1[]   // CS1521
                Diagnostic(ErrorCode.ERR_NonInterfaceInInterfaceList, "Test1[]").WithArguments("Test1[]").WithLocation(3, 15));
        }

        [WorkItem(906299, "DevDiv/Personal")]
        [Fact]
        public void CS1524ERR_ExpectedEndTry()
        {
            var test = @"using System;
namespace nms
{
    public class mine
    {
        private static int retval = 5;
        public static int Main()
        {
        try {
            Console.WriteLine(""In try block, ready to throw."");
            sizeof (throw new RecoverableException(""An exception has occurred""));
            }
        return retval;
        }
    };
}
";
            // Extra Errors
            ParseAndValidate(test,
                // (11,21): error CS1031: Type expected
                //             sizeof (throw new RecoverableException("An exception has occurred"));
                Diagnostic(ErrorCode.ERR_TypeExpected, "throw"),
                // (11,21): error CS1026: ) expected
                //             sizeof (throw new RecoverableException("An exception has occurred"));
                Diagnostic(ErrorCode.ERR_CloseParenExpected, "throw"),
                // (11,21): error CS1002: ; expected
                //             sizeof (throw new RecoverableException("An exception has occurred"));
                Diagnostic(ErrorCode.ERR_SemicolonExpected, "throw"),
                // (11,80): error CS1002: ; expected
                //             sizeof (throw new RecoverableException("An exception has occurred"));
                Diagnostic(ErrorCode.ERR_SemicolonExpected, ")"),
                // (11,80): error CS1513: } expected
                //             sizeof (throw new RecoverableException("An exception has occurred"));
                Diagnostic(ErrorCode.ERR_RbraceExpected, ")"));
        }

        [Fact]
        public void ParseTryWithoutCatchesOrFinally()
        {
            var test = @"
public class mine
{
    void M()
    {
        try { }
    }
}
";
            ParseAndValidate(test,
                // (6,15): error CS1524: Expected catch or finally
                //         try { }
                Diagnostic(ErrorCode.ERR_ExpectedEndTry, "}").WithLocation(6, 15));
        }

        [WorkItem(906299, "DevDiv/Personal")]
        [Fact]
        public void CS1525ERR_InvalidExprTerm()
        {
            var test = @"public class mine
    {
        public static int Main()
        {
            throw
        }
    };
";

            ParseAndValidate(test,
    // (5,18): error CS1525: Invalid expression term '}'
    //             throw
    Diagnostic(ErrorCode.ERR_InvalidExprTerm, "").WithArguments("}"),
    // (5,18): error CS1002: ; expected
    //             throw
    Diagnostic(ErrorCode.ERR_SemicolonExpected, ""));
        }

        [WorkItem(919539, "DevDiv/Personal")]
        [Fact]
        public void CS1525RegressBadStatement()
        {
            // Dev10 CS1525 vs. new parser CS1513
            var test = @"class C
{
    static void X()
    {
        => // error
    }
}";

            ParseAndValidate(test,
    // (4,6): error CS1513: } expected
    //     {
    Diagnostic(ErrorCode.ERR_RbraceExpected, ""));
        }

        [WorkItem(540245, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/540245")]
        [Fact]
        public void CS1525RegressVoidInfiniteLoop()
        {
            var test = @"class C
{
    void M()
    {
        void.Goo();
    }
}";

            ParseAndValidate(test, Diagnostic(ErrorCode.ERR_InvalidExprTerm, "void").WithArguments("void"));
        }

        [Fact]
        public void CS1525ERR_InvalidExprTerm_TernaryOperator()
        {
            var test = @"class Program
{
    static void Main(string[] args)
    {
        int x = 1;
        int y = 1;
        int s = true ?  : y++; // Invalid
        s = true ? x++ : ; // Invalid
        s =   ? x++ : y++; // Invalid
    }
}
";
            ParseAndValidate(test,
Diagnostic(ErrorCode.ERR_InvalidExprTerm, ":").WithArguments(":"),
Diagnostic(ErrorCode.ERR_InvalidExprTerm, ";").WithArguments(";"),
Diagnostic(ErrorCode.ERR_InvalidExprTerm, "?").WithArguments("?"));
        }

        [Fact]
        public void CS1525ERR_InvalidExprTerm_MultiExpression()
        {
            var test = @"class Program
{
    static void Main(string[] args)
    {
        int x = 1;
        int y = 1;
        int s = true ? x++, y++ : y++; // Invalid
        s = true ? x++ : x++, y++; // Invalid
    }
}
";

            ParseAndValidate(test,
                // (7,27): error CS1003: Syntax error, ':' expected
                //         int s = true ? x++, y++ : y++; // Invalid
                Diagnostic(ErrorCode.ERR_SyntaxError, ",").WithArguments(":").WithLocation(7, 27),
                // (7,27): error CS1525: Invalid expression term ','
                //         int s = true ? x++, y++ : y++; // Invalid
                Diagnostic(ErrorCode.ERR_InvalidExprTerm, ",").WithArguments(",").WithLocation(7, 27),
                // (7,30): error CS1002: ; expected
                //         int s = true ? x++, y++ : y++; // Invalid
                Diagnostic(ErrorCode.ERR_SemicolonExpected, "++").WithLocation(7, 30),
                // (7,33): error CS1525: Invalid expression term ':'
                //         int s = true ? x++, y++ : y++; // Invalid
                Diagnostic(ErrorCode.ERR_InvalidExprTerm, ":").WithArguments(":").WithLocation(7, 33),
                // (7,33): error CS1002: ; expected
                //         int s = true ? x++, y++ : y++; // Invalid
                Diagnostic(ErrorCode.ERR_SemicolonExpected, ":").WithLocation(7, 33),
                // (7,33): error CS1513: } expected
                //         int s = true ? x++, y++ : y++; // Invalid
                Diagnostic(ErrorCode.ERR_RbraceExpected, ":").WithLocation(7, 33),
                // (8,29): error CS1002: ; expected
                //         s = true ? x++ : x++, y++; // Invalid
                Diagnostic(ErrorCode.ERR_SemicolonExpected, ",").WithLocation(8, 29),
                // (8,29): error CS1513: } expected
                //         s = true ? x++ : x++, y++; // Invalid
                Diagnostic(ErrorCode.ERR_RbraceExpected, ",").WithLocation(8, 29));
        }

        [WorkItem(542229, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/542229")]
        [Fact]
        public void CS1525ERR_InvalidExprTerm_FromInExprInQuery()
        {
            var test = @"
class Program
{
    static void Main(string[] args)
    {
        var f1 = from num1 in new int[from] select num1;
    }
}
";
            ParseAndValidate(test, Diagnostic(ErrorCode.ERR_InvalidExprTerm, "from").WithArguments("]"));
        }

        [Fact]
        public void CS1525ERR_InvalidExprTerm_ReturnInCondition()
        {
            var test = @"class Program
{
    static void Main(string[] args)
    {
        int s = 1>2 ? return 0: return 1; 	// Invalid
    }
}
";
            ParseAndValidate(test,
                // (5,23): error CS1525: Invalid expression term 'return'
                //         int s = 1>2 ? return 0: return 1; 	// Invalid
                Diagnostic(ErrorCode.ERR_InvalidExprTerm, "return").WithArguments("return").WithLocation(5, 23),
                // (5,23): error CS1003: Syntax error, ':' expected
                //         int s = 1>2 ? return 0: return 1; 	// Invalid
                Diagnostic(ErrorCode.ERR_SyntaxError, "return").WithArguments(":").WithLocation(5, 23),
                // (5,23): error CS1525: Invalid expression term 'return'
                //         int s = 1>2 ? return 0: return 1; 	// Invalid
                Diagnostic(ErrorCode.ERR_InvalidExprTerm, "return").WithArguments("return").WithLocation(5, 23),
                // (5,23): error CS1002: ; expected
                //         int s = 1>2 ? return 0: return 1; 	// Invalid
                Diagnostic(ErrorCode.ERR_SemicolonExpected, "return").WithLocation(5, 23),
                // (5,31): error CS1002: ; expected
                //         int s = 1>2 ? return 0: return 1; 	// Invalid
                Diagnostic(ErrorCode.ERR_SemicolonExpected, ":").WithLocation(5, 31),
                // (5,31): error CS1513: } expected
                //         int s = 1>2 ? return 0: return 1; 	// Invalid
                Diagnostic(ErrorCode.ERR_RbraceExpected, ":").WithLocation(5, 31));
        }

        [Fact]
        public void CS1525ERR_InvalidExprTerm_GotoInCondition()
        {
            var test = @"class Program
{
    static int Main(string[] args)
    {
        int s = true ? goto lab1: goto lab2; // Invalid
    lab1:
        return 0;
    lab2:
        return 1;
    }
}
";
            ParseAndValidate(test,
                // (5,24): error CS1525: Invalid expression term 'goto'
                //         int s = true ? goto lab1: goto lab2; // Invalid
                Diagnostic(ErrorCode.ERR_InvalidExprTerm, "goto").WithArguments("goto").WithLocation(5, 24),
                // (5,24): error CS1003: Syntax error, ':' expected
                //         int s = true ? goto lab1: goto lab2; // Invalid
                Diagnostic(ErrorCode.ERR_SyntaxError, "goto").WithArguments(":").WithLocation(5, 24),
                // (5,24): error CS1525: Invalid expression term 'goto'
                //         int s = true ? goto lab1: goto lab2; // Invalid
                Diagnostic(ErrorCode.ERR_InvalidExprTerm, "goto").WithArguments("goto").WithLocation(5, 24),
                // (5,24): error CS1002: ; expected
                //         int s = true ? goto lab1: goto lab2; // Invalid
                Diagnostic(ErrorCode.ERR_SemicolonExpected, "goto").WithLocation(5, 24),
                // (5,33): error CS1002: ; expected
                //         int s = true ? goto lab1: goto lab2; // Invalid
                Diagnostic(ErrorCode.ERR_SemicolonExpected, ":").WithLocation(5, 33),
                // (5,33): error CS1513: } expected
                //         int s = true ? goto lab1: goto lab2; // Invalid
                Diagnostic(ErrorCode.ERR_RbraceExpected, ":").WithLocation(5, 33));
        }

        [Fact]
        public void CS1526ERR_BadNewExpr()
        {
            var test = @"
public class MainClass
    {
    public static int Main ()
        {
        int []pi = new int;
        return 1;
        }
    }
";

            ParseAndValidate(test, Diagnostic(ErrorCode.ERR_BadNewExpr, ";"));
        }

        [Fact]
        public void CS1528ERR_BadVarDecl()
        {
            var test = @"
using System;
namespace nms {
public class B {
    public B(int i) {}
    public void toss () { throw new Exception(""Exception thrown in function toss()."");}
};
public class mine {
    private static int retval = 5;
    public static int Main()
        {
        try {B b(3);
            b.toss();
            }
        catch ( Exception e ) {retval -= 5; Console.WriteLine (e.GetMessage()); }
        return retval;
        }
    };
}
";
            // Extra errors
            ParseAndValidate(test,
                // (12,18): error CS1026: ) expected
                //         try {B b(3);
                Diagnostic(ErrorCode.ERR_CloseParenExpected, "3").WithLocation(12, 18),
                // (12,18): error CS1002: ; expected
                //         try {B b(3);
                Diagnostic(ErrorCode.ERR_SemicolonExpected, "3").WithLocation(12, 18),
                // (12,19): error CS1002: ; expected
                //         try {B b(3);
                Diagnostic(ErrorCode.ERR_SemicolonExpected, ")").WithLocation(12, 19),
                // (12,19): error CS1513: } expected
                //         try {B b(3);
                Diagnostic(ErrorCode.ERR_RbraceExpected, ")").WithLocation(12, 19));
        }

        [Fact]
        public void CS1528RegressEventVersion()
        {
            var test = @"
class C
{
    event System.Action E();
}
";
            // Extra errors
            ParseAndValidate(test,
                // (4,26): error CS1528: Expected ; or = (cannot specify constructor arguments in declaration)
                //     event System.Action E();
                Diagnostic(ErrorCode.ERR_BadVarDecl, "(").WithLocation(4, 26),
                // (4,26): error CS1003: Syntax error, '[' expected
                //     event System.Action E();
                Diagnostic(ErrorCode.ERR_SyntaxError, "(").WithArguments("[").WithLocation(4, 26),
                // (4,27): error CS1003: Syntax error, ']' expected
                //     event System.Action E();
                Diagnostic(ErrorCode.ERR_SyntaxError, ")").WithArguments("]").WithLocation(4, 27)
                );
        }

        [Fact]
        public void CS1529ERR_UsingAfterElements()
        {
            var test = @"
namespace NS 
{
    class SomeClass
    {}
    using System;
}
using Microsoft;
";

            ParseAndValidate(test,
Diagnostic(ErrorCode.ERR_UsingAfterElements, "using System;"),
Diagnostic(ErrorCode.ERR_UsingAfterElements, "using Microsoft;"));
        }

        [Fact]
        public void CS1534ERR_BadBinOpArgs()
        {
            var test = @"
class MyClass {
    public int intI = 2;
    public static MyClass operator + (MyClass MC1, MyClass MC2, MyClass MC3) {
        return new MyClass();
    }
    public static int Main() {
        return 1;
    }
}
";

            ParseAndValidate(test,
    // (4,36): error CS1534: Overloaded binary operator '+' takes two parameters
    //     public static MyClass operator + (MyClass MC1, MyClass MC2, MyClass MC3) {
    Diagnostic(ErrorCode.ERR_BadBinOpArgs, "+").WithArguments("+"));
        }

        [WorkItem(863409, "DevDiv/Personal")]
        [WorkItem(906305, "DevDiv/Personal")]
        [Fact]
        public void CS1535ERR_BadUnOpArgs()
        {
            var test = @"
class MyClass {
    public int intI = 2;
    public static MyClass operator ++ () {
        return new MyClass();
    }
    public static int Main() {
        return 1;
    }
}
";

            ParseAndValidate(test, Diagnostic(ErrorCode.ERR_BadUnOpArgs, "++").WithArguments("++"));
        }

        // TODO: extra error CS1001

        [Fact]
        public void CS1536ERR_NoVoidParameter()
        {
            var test = @"
class Test
{    
    public void goo(void){}
}
";

            ParseAndValidate(test,
    // (4,21): error CS1536: Invalid parameter type 'void'
    //     public void goo(void){}
    Diagnostic(ErrorCode.ERR_NoVoidParameter, "void"),
    // (4,25): error CS1001: Identifier expected
    //     public void goo(void){}
    Diagnostic(ErrorCode.ERR_IdentifierExpected, ")"));
        }

        [Fact]
        public void CS1536ERR_NoVoidParameter_02()
        {
            var test = @"
class Test
{
    object o = (ref void x) => {};
}
";

            ParseAndValidate(test,
                // (4,21): error CS1536: Invalid parameter type 'void'
                //     object o = (ref void x) => {};
                Diagnostic(ErrorCode.ERR_NoVoidParameter, "void").WithLocation(4, 21)
                );
        }

        [Fact]
        public void CS1547ERR_NoVoidHere()
        {
            var test = @"
using System;
public class MainClass
    {
    public static int Main ()
        {
        void v;
        Console.WriteLine (5);
        return 1;
        }
    }
";

            ParseAndValidate(test, Diagnostic(ErrorCode.ERR_NoVoidHere, "void"));
        }

        [WorkItem(919490, "DevDiv/Personal")]
        [Fact]
        public void CS1547ERR_NoVoidHereInDefaultAndSizeof()
        {
            var test = @"class C {
void M()
{
 var x = sizeof(void);
 return default(void);
} }
";

            ParseAndValidate(test, Diagnostic(ErrorCode.ERR_NoVoidHere, "void"), Diagnostic(ErrorCode.ERR_NoVoidHere, "void"));
        }

        [Fact]
        public void CS1551ERR_IndexerNeedsParam()
        {
            var test = @"
public class MyClass {
    int intI;
    int this[] {
        get {
            return intI;
        }
        set {
            intI = value;
        }
    }
    public static int Main() {
        return 1;
    }
}
";

            CreateCompilation(test).VerifyDiagnostics(
                // (4,14): error CS1551: Indexers must have at least one parameter
                //     int this[] {
                Diagnostic(ErrorCode.ERR_IndexerNeedsParam, "]").WithLocation(4, 14));
        }

        [Fact]
        public void CS1552ERR_BadArraySyntax()
        {
            var test = @"
    public class C { 
        public static void Main(string args[]) { 
        }
    }
";

            ParseAndValidate(test, Diagnostic(ErrorCode.ERR_BadArraySyntax, "["));
        }

        [Fact, WorkItem(535933, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/535933")] // ?
        public void CS1553ERR_BadOperatorSyntax()
        {
            // Extra errors
            var test = @"
class goo {
    public static int implicit operator (goo f) { return 6; }    // Error
}
public class MainClass
    {
    public static int Main ()
        {
        return 1;
        }
    }
";

            ParseAndValidate(test, TestOptions.Regular,
                // (3,19): error CS1553: Declaration is not valid; use '+ operator <dest-type> (...' instead
                //     public static int implicit operator (goo f) { return 6; }    // Error
                Diagnostic(ErrorCode.ERR_BadOperatorSyntax, "int").WithArguments("+").WithLocation(3, 19),
                // (3,23): error CS1003: Syntax error, 'operator' expected
                //     public static int implicit operator (goo f) { return 6; }    // Error
                Diagnostic(ErrorCode.ERR_SyntaxError, "implicit").WithArguments("operator").WithLocation(3, 23),
                // (3,23): error CS1019: Overloadable unary operator expected
                //     public static int implicit operator (goo f) { return 6; }    // Error
                Diagnostic(ErrorCode.ERR_OvlUnaryOperatorExpected, "implicit").WithLocation(3, 23),
                // (3,32): error CS1003: Syntax error, '(' expected
                //     public static int implicit operator (goo f) { return 6; }    // Error
                Diagnostic(ErrorCode.ERR_SyntaxError, "operator").WithArguments("(").WithLocation(3, 32),
                // (3,32): error CS1041: Identifier expected; 'operator' is a keyword
                //     public static int implicit operator (goo f) { return 6; }    // Error
                Diagnostic(ErrorCode.ERR_IdentifierExpectedKW, "operator").WithArguments("", "operator").WithLocation(3, 32),
                // (3,47): error CS8124: Tuple must contain at least two elements.
                //     public static int implicit operator (goo f) { return 6; }    // Error
                Diagnostic(ErrorCode.ERR_TupleTooFewElements, ")").WithLocation(3, 47),
                // (3,49): error CS1001: Identifier expected
                //     public static int implicit operator (goo f) { return 6; }    // Error
                Diagnostic(ErrorCode.ERR_IdentifierExpected, "{").WithLocation(3, 49),
                // (3,49): error CS1003: Syntax error, ',' expected
                //     public static int implicit operator (goo f) { return 6; }    // Error
                Diagnostic(ErrorCode.ERR_SyntaxError, "{").WithArguments(",").WithLocation(3, 49),
                // (3,59): error CS1026: ) expected
                //     public static int implicit operator (goo f) { return 6; }    // Error
                Diagnostic(ErrorCode.ERR_CloseParenExpected, ";").WithLocation(3, 59),
                // (4,1): error CS1022: Type or namespace definition, or end-of-file expected
                // }
                Diagnostic(ErrorCode.ERR_EOFExpected, "}").WithLocation(4, 1)
                );
        }

        [Fact, WorkItem(535933, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/535933")] // ?
        public void CS1553ERR_BadOperatorSyntaxWithCSharp6()
        {
            // Extra errors
            var test = @"
class goo {
    public static int implicit operator (goo f) { return 6; }    // Error
}
public class MainClass
    {
    public static int Main ()
        {
        return 1;
        }
    }
";
            CreateCompilation(test, parseOptions: TestOptions.Regular6).VerifyDiagnostics(
                // (2,7): warning CS8981: The type name 'goo' only contains lower-cased ascii characters. Such names may become reserved for the language.
                // class goo {
                Diagnostic(ErrorCode.WRN_LowerCaseTypeName, "goo").WithArguments("goo").WithLocation(2, 7),
                // (3,19): error CS1553: Declaration is not valid; use '+ operator <dest-type> (...' instead
                //     public static int implicit operator (goo f) { return 6; }    // Error
                Diagnostic(ErrorCode.ERR_BadOperatorSyntax, "int").WithArguments("+").WithLocation(3, 19),
                // (3,23): error CS1003: Syntax error, 'operator' expected
                //     public static int implicit operator (goo f) { return 6; }    // Error
                Diagnostic(ErrorCode.ERR_SyntaxError, "implicit").WithArguments("operator").WithLocation(3, 23),
                // (3,23): error CS1019: Overloadable unary operator expected
                //     public static int implicit operator (goo f) { return 6; }    // Error
                Diagnostic(ErrorCode.ERR_OvlUnaryOperatorExpected, "implicit").WithLocation(3, 23),
                // (3,23): error CS0501: 'goo.operator +((goo f, ?))' must declare a body because it is not marked abstract, extern, or partial
                //     public static int implicit operator (goo f) { return 6; }    // Error
                Diagnostic(ErrorCode.ERR_ConcreteMissingBody, "").WithArguments("goo.operator +((goo f, ?))").WithLocation(3, 23),
                // (3,23): error CS0562: The parameter of a unary operator must be the containing type
                //     public static int implicit operator (goo f) { return 6; }    // Error
                Diagnostic(ErrorCode.ERR_BadUnaryOperatorSignature, "").WithLocation(3, 23),
                // (3,32): error CS1003: Syntax error, '(' expected
                //     public static int implicit operator (goo f) { return 6; }    // Error
                Diagnostic(ErrorCode.ERR_SyntaxError, "operator").WithArguments("(").WithLocation(3, 32),
                // (3,32): error CS1041: Identifier expected; 'operator' is a keyword
                //     public static int implicit operator (goo f) { return 6; }    // Error
                Diagnostic(ErrorCode.ERR_IdentifierExpectedKW, "operator").WithArguments("", "operator").WithLocation(3, 32),
                // (3,41): error CS8059: Feature 'tuples' is not available in C# 6. Please use language version 7.0 or greater.
                //     public static int implicit operator (goo f) { return 6; }    // Error
                Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion6, "(goo f)").WithArguments("tuples", "7.0").WithLocation(3, 41),
                // (3,47): error CS8124: Tuple must contain at least two elements.
                //     public static int implicit operator (goo f) { return 6; }    // Error
                Diagnostic(ErrorCode.ERR_TupleTooFewElements, ")").WithLocation(3, 47),
                // (3,49): error CS1001: Identifier expected
                //     public static int implicit operator (goo f) { return 6; }    // Error
                Diagnostic(ErrorCode.ERR_IdentifierExpected, "{").WithLocation(3, 49),
                // (3,49): error CS1003: Syntax error, ',' expected
                //     public static int implicit operator (goo f) { return 6; }    // Error
                Diagnostic(ErrorCode.ERR_SyntaxError, "{").WithArguments(",").WithLocation(3, 49),
                // (3,59): error CS1026: ) expected
                //     public static int implicit operator (goo f) { return 6; }    // Error
                Diagnostic(ErrorCode.ERR_CloseParenExpected, ";").WithLocation(3, 59),
                // (4,1): error CS1022: Type or namespace definition, or end-of-file expected
                // }
                Diagnostic(ErrorCode.ERR_EOFExpected, "}").WithLocation(4, 1));

            ParseAndValidate(test, TestOptions.Regular.WithLanguageVersion(LanguageVersion.CSharp6),
                // (3,19): error CS1553: Declaration is not valid; use '+ operator <dest-type> (...' instead
                //     public static int implicit operator (goo f) { return 6; }    // Error
                Diagnostic(ErrorCode.ERR_BadOperatorSyntax, "int").WithArguments("+").WithLocation(3, 19),
                // (3,23): error CS1003: Syntax error, 'operator' expected
                //     public static int implicit operator (goo f) { return 6; }    // Error
                Diagnostic(ErrorCode.ERR_SyntaxError, "implicit").WithArguments("operator").WithLocation(3, 23),
                // (3,23): error CS1019: Overloadable unary operator expected
                //     public static int implicit operator (goo f) { return 6; }    // Error
                Diagnostic(ErrorCode.ERR_OvlUnaryOperatorExpected, "implicit").WithLocation(3, 23),
                // (3,32): error CS1003: Syntax error, '(' expected
                //     public static int implicit operator (goo f) { return 6; }    // Error
                Diagnostic(ErrorCode.ERR_SyntaxError, "operator").WithArguments("(").WithLocation(3, 32),
                // (3,32): error CS1041: Identifier expected; 'operator' is a keyword
                //     public static int implicit operator (goo f) { return 6; }    // Error
                Diagnostic(ErrorCode.ERR_IdentifierExpectedKW, "operator").WithArguments("", "operator").WithLocation(3, 32),
                // (3,47): error CS8124: Tuple must contain at least two elements.
                //     public static int implicit operator (goo f) { return 6; }    // Error
                Diagnostic(ErrorCode.ERR_TupleTooFewElements, ")").WithLocation(3, 47),
                // (3,49): error CS1001: Identifier expected
                //     public static int implicit operator (goo f) { return 6; }    // Error
                Diagnostic(ErrorCode.ERR_IdentifierExpected, "{").WithLocation(3, 49),
                // (3,49): error CS1003: Syntax error, ',' expected
                //     public static int implicit operator (goo f) { return 6; }    // Error
                Diagnostic(ErrorCode.ERR_SyntaxError, "{").WithArguments(",").WithLocation(3, 49),
                // (3,59): error CS1026: ) expected
                //     public static int implicit operator (goo f) { return 6; }    // Error
                Diagnostic(ErrorCode.ERR_CloseParenExpected, ";").WithLocation(3, 59),
                // (4,1): error CS1022: Type or namespace definition, or end-of-file expected
                // }
                Diagnostic(ErrorCode.ERR_EOFExpected, "}").WithLocation(4, 1));
        }

        [Fact(), WorkItem(526995, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/526995")]
        public void CS1554ERR_BadOperatorSyntax2()
        {
            // Diff errors: CS1003, 1031 etc. (8 errors)
            var test = @"
class goo {
    public static operator ++ goo (goo f) { return new goo(); }    // Error
}
public class MainClass
    {
    public static int Main ()
        {
        return 1;
        }
    }
";

            ParseAndValidateFirst(test, Diagnostic(ErrorCode.ERR_TypeExpected, "operator"));
        }

        [Fact, WorkItem(536673, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/536673")]
        public void CS1575ERR_BadStackAllocExpr()
        {
            // Diff errors
            var test = @"
public class Test
{
    unsafe public static int Main()
    {
        int *p = stackalloc int (30); 
        int *pp = stackalloc int 30; 
        return 1;
    }
}
";
            // Extra errors
            CreateCompilation(test, options: TestOptions.UnsafeDebugDll).VerifyDiagnostics(
                // (7,34): error CS1002: ; expected
                //         int *pp = stackalloc int 30;
                Diagnostic(ErrorCode.ERR_SemicolonExpected, "30").WithLocation(7, 34),
                // (6,29): error CS1575: A stackalloc expression requires [] after type
                //         int *p = stackalloc int (30);
                Diagnostic(ErrorCode.ERR_BadStackAllocExpr, "int").WithLocation(6, 29),
                // (7,30): error CS1575: A stackalloc expression requires [] after type
                //         int *pp = stackalloc int 30;
                Diagnostic(ErrorCode.ERR_BadStackAllocExpr, "int").WithLocation(7, 30),
                // (7,34): error CS0201: Only assignment, call, increment, decrement, await, and new object expressions can be used as a statement
                //         int *pp = stackalloc int 30;
                Diagnostic(ErrorCode.ERR_IllegalStatement, "30").WithLocation(7, 34)
                );
        }

        [Fact]
        public void CS1575ERR_BadStackAllocExpr1()
        {
            // Diff errors
            var test = @"
unsafe public class Test
{
    int* p = stackalloc int[1];
}
";
            CreateCompilation(test, options: TestOptions.UnsafeDebugDll).VerifyDiagnostics(
                // (4,14): error CS0518: Predefined type 'System.Span`1' is not defined or imported
                //     int* p = stackalloc int[1];
                Diagnostic(ErrorCode.ERR_PredefinedTypeNotFound, "stackalloc int[1]").WithArguments("System.Span`1").WithLocation(4, 14)
                );
        }

        [Fact]
        public void CS1575ERR_BadStackAllocExpr2()
        {
            // Diff errors
            var test = @"
unsafe public class Test
{
    void M()
    {
        int*[] p = new int*[] { stackalloc int[1] };
    }
}
";
            CreateCompilation(test, options: TestOptions.UnsafeDebugDll).VerifyDiagnostics(
                // (6,33): error CS0518: Predefined type 'System.Span`1' is not defined or imported
                //         int*[] p = new int*[] { stackalloc int[1] };
                Diagnostic(ErrorCode.ERR_PredefinedTypeNotFound, "stackalloc int[1]").WithArguments("System.Span`1").WithLocation(6, 33)
                );
        }

        [Fact]
        public void CS1575ERR_BadStackAllocExpr3()
        {
            // Diff errors
            var test = @"
unsafe public class Test
{
    void M()
    {
        const int* p = stackalloc int[1];
    }
}
";
            CreateCompilation(test, options: TestOptions.UnsafeDebugDll).VerifyDiagnostics(
                // (6,15): error CS0283: The type 'int*' cannot be declared const
                //         const int* p = stackalloc int[1];
                Diagnostic(ErrorCode.ERR_BadConstType, "int*").WithArguments("int*").WithLocation(6, 15)
            );
        }

        [Fact]
        public void CS1674ERR_StackAllocInUsing1()
        {
            // Diff errors
            var test = @"
public class Test
{
    unsafe public static void Main()
    {
        using (var v = stackalloc int[1])
        {
        }
    }
}
";
            CreateCompilationWithMscorlibAndSpan(test, options: TestOptions.ReleaseDll.WithAllowUnsafe(true)).VerifyDiagnostics(
                // (6,16): error CS1674: 'Span<int>': type used in a using statement must be implicitly convertible to 'System.IDisposable'
                //         using (var v = stackalloc int[1])
                Diagnostic(ErrorCode.ERR_NoConvToIDisp, "var v = stackalloc int[1]").WithArguments("System.Span<int>").WithLocation(6, 16));
        }

        [Fact]
        public void CS0029ERR_StackAllocInUsing2()
        {
            // Diff errors
            var test = @"
public class Test
{
    unsafe public static void Main()
    {
        using (System.IDisposable v = stackalloc int[1])
        {
        }
    }
}
";
            CreateCompilation(test, options: TestOptions.ReleaseDll.WithAllowUnsafe(true)).VerifyDiagnostics(
                // (6,39): error CS0518: Predefined type 'System.Span`1' is not defined or imported
                //         using (System.IDisposable v = stackalloc int[1])
                Diagnostic(ErrorCode.ERR_PredefinedTypeNotFound, "stackalloc int[1]").WithArguments("System.Span`1").WithLocation(6, 39)
             );
        }

        [WorkItem(906993, "DevDiv/Personal")]
        [Fact]
        public void CS1576ERR_InvalidLineNumberpp()
        {
            var test = @"
public class Test
{
    # line abc hidden
    public static void MyHiddenMethod()
    {
#line 0
    }
}
";

            ParseAndValidate(test,
    // (4,12): error CS1576: The line number specified for #line directive is missing or invalid
    //     # line abc hidden
    Diagnostic(ErrorCode.ERR_InvalidLineNumber, "abc"),
    // (7,7): error CS1576: The line number specified for #line directive is missing or invalid
    // #line 0
    Diagnostic(ErrorCode.ERR_InvalidLineNumber, "0"));
        }

        [WorkItem(541952, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/541952")]
        [Fact]
        public void CS1576ERR_InvalidLineNumber02()
        {
            var test = @"
#line 0
#error
";

            ParseAndValidate(test,
    // (2,7): error CS1576: The line number specified for #line directive is missing or invalid
    // #line 0
    Diagnostic(ErrorCode.ERR_InvalidLineNumber, "0"),
    // (3,7): error CS1029: #error: ''
    // #error
    Diagnostic(ErrorCode.ERR_ErrorDirective, "").WithArguments(""));
        }

        [WorkItem(536689, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/536689")]
        [Fact]
        public void CS1578ERR_MissingPPFile()
        {
            var test = @"
public class Test
{
    #line 5 hidden
    public static void MyHiddenMethod()
    {
    }
}
";

            ParseAndValidate(test, Diagnostic(ErrorCode.ERR_MissingPPFile, "hidden"));
        }

        [WorkItem(863414, "DevDiv/Personal")]
        [Fact]
        public void CS1585ERR_BadModifierLocation()
        {
            // Diff error: CS1519 v.s. CS1585
            var test = @"
namespace oo {
public class clx {
public void f(){}
} // class clx
public class clxx : clx {
    public static void virtual f() {}
}
public class cly {
public static int Main(){return 0;}
} // class cly
} // namespace
";

            ParseAndValidate(test,
    // (7,24): error CS1585: Member modifier 'virtual' must precede the member type and name
    //     public static void virtual f() {}
    Diagnostic(ErrorCode.ERR_BadModifierLocation, "virtual").WithArguments("virtual"));
        }

        [Fact]
        public void CS1586ERR_MissingArraySize()
        {
            var test = @"
class Test
{
    public static int Main()
    {
        int[] a = new int[];
        int[,] t = new int[,];
        byte[] b = new byte[];
        string[] s = new string[];
        return 1;
    }
}
";

            CreateCompilation(test, options: TestOptions.UnsafeDebugDll).VerifyDiagnostics(
                // (6,26): error CS1586: Array creation must have array size or array initializer
                //         int[] a = new int[];
                Diagnostic(ErrorCode.ERR_MissingArraySize, "[]").WithLocation(6, 26),
                // (7,27): error CS1586: Array creation must have array size or array initializer
                //         int[,] t = new int[,];
                Diagnostic(ErrorCode.ERR_MissingArraySize, "[,]").WithLocation(7, 27),
                // (8,28): error CS1586: Array creation must have array size or array initializer
                //         byte[] b = new byte[];
                Diagnostic(ErrorCode.ERR_MissingArraySize, "[]").WithLocation(8, 28),
                // (9,32): error CS1586: Array creation must have array size or array initializer
                //         string[] s = new string[];
                Diagnostic(ErrorCode.ERR_MissingArraySize, "[]").WithLocation(9, 32)
                );
        }

        [Fact, WorkItem(535935, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/535935")]
        public void CS1597ERR_UnexpectedSemicolon()
        {
            // Diff error: CS1519
            var test = @"
public class Test
{
   public static int Main()
   {
       return 1;
   };   
}
";

            ParseAndValidate(test, Diagnostic(ErrorCode.ERR_UnexpectedSemicolon, ";"));
        }

        [Fact]
        public void CS1609ERR_NoModifiersOnAccessor()
        {
            var test = @"
public delegate void Del();
public class Test
{
    public int Prop   
    {
        get
        {
            return 0;
        }
        private set
        {
        }
    }
    public event Del E
    {
        private add{}
        public remove{}
    }
    public static int Main()
    {
        return 1;
    }
}
";
            CreateCompilation(test).GetDeclarationDiagnostics().Verify(
                // (17,9): error CS1609: Modifiers cannot be placed on event accessor declarations
                //         private add{}
                Diagnostic(ErrorCode.ERR_NoModifiersOnAccessor, "private").WithLocation(17, 9),
                // (18,9): error CS1609: Modifiers cannot be placed on event accessor declarations
                //         public remove{}
                Diagnostic(ErrorCode.ERR_NoModifiersOnAccessor, "public").WithLocation(18, 9));
        }

        [Fact]
        public void CS1609ERR_NoModifiersOnAccessor_Event()
        {
            var test = @"
public delegate void Del();
public class Test
{
    event Del E
    {
        public add { }
        private remove { }
    }
}
";

            CreateCompilation(test).GetDeclarationDiagnostics().Verify(
                // (7,9): error CS1609: Modifiers cannot be placed on event accessor declarations
                //         public add { }
                Diagnostic(ErrorCode.ERR_NoModifiersOnAccessor, "public").WithLocation(7, 9),
                // (8,9): error CS1609: Modifiers cannot be placed on event accessor declarations
                //         private remove { }
                Diagnostic(ErrorCode.ERR_NoModifiersOnAccessor, "private").WithLocation(8, 9));
        }

        [Fact]
        public void ParamsCantBeUsedWithModifiers()
        {
            var test = @"
public class Test
{
    public static void ParamsWithRef(params ref int[] a) 
    {
    }
    public static void ParamsWithOut(params out int[] a) 
    {
    }
    public static int Main()
    {
        int i = 10;
        ParamsWithRef(ref i);
        ParamsWithOut(out i);
        return 1;
    }
}
";

            CreateCompilationWithMscorlib40AndSystemCore(test).GetDeclarationDiagnostics().Verify(
                // (4,45): error CS1611: The params parameter cannot be declared as ref
                //     public static void ParamsWithRef(params ref int[] a) 
                Diagnostic(ErrorCode.ERR_ParamsCantBeWithModifier, "ref").WithArguments("ref").WithLocation(4, 45),
                // (7,45): error CS1611: The params parameter cannot be declared as out
                //     public static void ParamsWithOut(params out int[] a) 
                Diagnostic(ErrorCode.ERR_ParamsCantBeWithModifier, "out").WithArguments("out").WithLocation(7, 45));
        }

        [Fact]
        public void CS1627ERR_EmptyYield()
        {
            var test = @"
using System.Collections;
class C : IEnumerable
{
   public IEnumerator GetEnumerator()
   {
      yield return;  // CS1627
   }
}
class Test
{
    public static int Main()
    {
        return 1;
    }
}
";

            ParseAndValidate(test, Diagnostic(ErrorCode.ERR_EmptyYield, "return"));
        }

        [Fact]
        public void CS1641ERR_FixedDimsRequired()
        {
            var test = @"
unsafe struct S 
{
    fixed int [] ia;  // CS1641
    fixed int [] ib[];  // CS0443
};
class Test
{
    public static int Main()
    {
        return 1;
    }
}
";

            ParseAndValidate(test, Diagnostic(ErrorCode.ERR_FixedDimsRequired, "ia"), Diagnostic(ErrorCode.ERR_ValueExpected, "]"));
        }

        [WorkItem(863435, "DevDiv/Personal")]
        [Fact]
        public void CS1671ERR_BadModifiersOnNamespace01()
        {
            var test = @"
public namespace NS // CS1671
{
    class Test
    {
        public static int Main()
        {
            return 1;
        }
    }
}
";

            ParseAndValidate(test);
        }

        [Fact]
        public void CS1671ERR_BadModifiersOnNamespace02()
        {
            var test = @"[System.Obsolete]
namespace N { }
";

            ParseAndValidate(test);
        }

        [WorkItem(863437, "DevDiv/Personal")]
        [Fact]
        public void CS1675ERR_InvalidGenericEnumNowCS7002()
        {
            var test = @"
enum E<T> // CS1675
{
}
class Test
{
    public static int Main()
    {
        return 1;
    }
}
";

            ParseAndValidate(test, Diagnostic(ErrorCode.ERR_UnexpectedGenericName, "E"));
        }

        [WorkItem(863438, "DevDiv/Personal")]
        [Fact]
        public void CS1730ERR_GlobalAttributesNotFirst()
        {
            var test = @"
class Test
{
}
[assembly:System.Attribute]
";

            ParseAndValidate(test, Diagnostic(ErrorCode.ERR_GlobalAttributesNotFirst, "assembly"));
        }

        [Fact(), WorkItem(527039, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/527039")]
        public void CS1732ERR_ParameterExpected()
        {
            var test = @"
using System;
static class Test
{
    static int Main()
    {
        Func<int,int> f1 = (x,) => 1;
        Func<int,int, int> f2 = (y,) => 2;
        return 1;
    }
}
";

            ParseAndValidate(test,
                // (7,31): error CS1001: Identifier expected
                //         Func<int,int> f1 = (x,) => 1;
                Diagnostic(ErrorCode.ERR_IdentifierExpected, ")"),
                // (8,36): error CS1001: Identifier expected
                //         Func<int,int, int> f2 = (y,) => 2;
                Diagnostic(ErrorCode.ERR_IdentifierExpected, ")"));
        }

        [Fact, WorkItem(536674, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/536674")]
        public void CS1525ERR_InvalidExprTerm_02()
        {
            var test = @"
using System.Collections.Generic;
using System.Collections;
static class Test
{
    static void Main()
    {
        A a = new A { 5, {9, 5, }, 3 };
    }
}
";

            ParseAndValidate(test,
                // (8,33): error CS1525: Invalid expression term '}'
                //         A a = new A { 5, {9, 5, }, 3 };
                Diagnostic(ErrorCode.ERR_InvalidExprTerm, "}").WithArguments("}").WithLocation(8, 33));
        }

        [WorkItem(536674, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/536674")]
        [Fact]
        public void CS1733ERR_ExpressionExpected_02()
        {
            var test = @"using System;
using System.Collections.Generic;
using System.Linq;
 
class Program
{
    static void Main(string[] args)
    {
        Console.WriteLine(""Hello"")?";

            ParseAndValidate(test,
                // (9,36): error CS1733: Expected expression
                //         Console.WriteLine("Hello")?
                Diagnostic(ErrorCode.ERR_ExpressionExpected, "").WithLocation(9, 36),
                // (9,36): error CS1003: Syntax error, ':' expected
                //         Console.WriteLine("Hello")?
                Diagnostic(ErrorCode.ERR_SyntaxError, "").WithArguments(":").WithLocation(9, 36),
                // (9,36): error CS1733: Expected expression
                //         Console.WriteLine("Hello")?
                Diagnostic(ErrorCode.ERR_ExpressionExpected, "").WithLocation(9, 36),
                // (9,36): error CS1002: ; expected
                //         Console.WriteLine("Hello")?
                Diagnostic(ErrorCode.ERR_SemicolonExpected, "").WithLocation(9, 36),
                // (9,36): error CS1513: } expected
                //         Console.WriteLine("Hello")?
                Diagnostic(ErrorCode.ERR_RbraceExpected, "").WithLocation(9, 36),
                // (9,36): error CS1513: } expected
                //         Console.WriteLine("Hello")?
                Diagnostic(ErrorCode.ERR_RbraceExpected, "").WithLocation(9, 36));
        }

        [Fact]
        public void CS1960ERR_IllegalVarianceSyntax()
        {
            var test =
@"interface I<in T>
{
    void M<in U>();
    object this<out U>[int i] { get; set; }
}
struct S<out T>
{
    void M<out U>();
}
delegate void D<in T>();
class A<out T>
{
    void M<out U>();
    interface I<in U> { }
    struct S<out U> { }
    delegate void D<in U>();
    class B<out U> { }
}";
            CreateCompilation(test).VerifyDiagnostics(
                // (4,12): error CS7002: Unexpected use of a generic name
                //     object this<out U>[int i] { get; set; }
                Diagnostic(ErrorCode.ERR_UnexpectedGenericName, "this").WithLocation(4, 12),
                // (6,10): error CS1960: Invalid variance modifier. Only interface and delegate type parameters can be specified as variant.
                // struct S<out T>
                Diagnostic(ErrorCode.ERR_IllegalVarianceSyntax, "out").WithLocation(6, 10),
                // (11,9): error CS1960: Invalid variance modifier. Only interface and delegate type parameters can be specified as variant.
                // class A<out T>
                Diagnostic(ErrorCode.ERR_IllegalVarianceSyntax, "out").WithLocation(11, 9),
                // (3,12): error CS1960: Invalid variance modifier. Only interface and delegate type parameters can be specified as variant.
                //     void M<in U>();
                Diagnostic(ErrorCode.ERR_IllegalVarianceSyntax, "in").WithLocation(3, 12),
                // (13,12): error CS1960: Invalid variance modifier. Only interface and delegate type parameters can be specified as variant.
                //     void M<out U>();
                Diagnostic(ErrorCode.ERR_IllegalVarianceSyntax, "out").WithLocation(13, 12),
                // (8,12): error CS1960: Invalid variance modifier. Only interface and delegate type parameters can be specified as variant.
                //     void M<out U>();
                Diagnostic(ErrorCode.ERR_IllegalVarianceSyntax, "out").WithLocation(8, 12),
                // (8,10): error CS0501: 'S<T>.M<U>()' must declare a body because it is not marked abstract, extern, or partial
                //     void M<out U>();
                Diagnostic(ErrorCode.ERR_ConcreteMissingBody, "M").WithArguments("S<T>.M<U>()").WithLocation(8, 10),
                // (13,10): error CS0501: 'A<T>.M<U>()' must declare a body because it is not marked abstract, extern, or partial
                //     void M<out U>();
                Diagnostic(ErrorCode.ERR_ConcreteMissingBody, "M").WithArguments("A<T>.M<U>()").WithLocation(13, 10),
                // (17,13): error CS1960: Invalid variance modifier. Only interface and delegate type parameters can be specified as variant.
                //     class B<out U> { }
                Diagnostic(ErrorCode.ERR_IllegalVarianceSyntax, "out").WithLocation(17, 13),
                // (15,14): error CS1960: Invalid variance modifier. Only interface and delegate type parameters can be specified as variant.
                //     struct S<out U> { }
                Diagnostic(ErrorCode.ERR_IllegalVarianceSyntax, "out").WithLocation(15, 14));
        }

        [Fact]
        public void CS1960ERR_IllegalVarianceSyntax_LocalFunction()
        {
            var test =
@"class C
{
    void M()
    {
        void Local<in T>() { }
    }
}";
            CreateCompilation(test).VerifyDiagnostics(
                // (5,20): error CS1960: Invalid variance modifier. Only interface and delegate type parameters can be specified as variant.
                //         void Local<in T>() { }
                Diagnostic(ErrorCode.ERR_IllegalVarianceSyntax, "in").WithLocation(5, 20),
                // (5,14): warning CS8321: The local function 'Local' is declared but never used
                //         void Local<in T>() { }
                Diagnostic(ErrorCode.WRN_UnreferencedLocalFunction, "Local").WithArguments("Local").WithLocation(5, 14));
        }

        [Fact]
        public void CS7000ERR_UnexpectedAliasedName()
        {
            var test = @"using System;
using N1Alias = N1;

namespace N1 
{
    namespace N1Alias::N2 {}

    class Test
    {      
        static int Main()
        {
            N1.global::Test.M1();
            return 1;
        }
    }
}
";

            // Native compiler : CS1003
            CreateCompilation(test).VerifyDiagnostics(
                // (12,22): error CS7000: Unexpected use of an aliased name
                //             N1.global::Test.M1();
                Diagnostic(ErrorCode.ERR_UnexpectedAliasedName, "::").WithLocation(12, 22),
                // (6,15): error CS7000: Unexpected use of an aliased name
                //     namespace N1Alias::N2 {}
                Diagnostic(ErrorCode.ERR_UnexpectedAliasedName, "N1Alias::N2").WithLocation(6, 15),
                // (12,13): error CS0234: The type or namespace name 'global' does not exist in the namespace 'N1' (are you missing an assembly reference?)
                //             N1.global::Test.M1();
                Diagnostic(ErrorCode.ERR_DottedTypeNameNotFoundInNS, "N1.global").WithArguments("global", "N1").WithLocation(12, 13),
                // (2,1): hidden CS8019: Unnecessary using directive.
                // using N1Alias = N1;
                Diagnostic(ErrorCode.HDN_UnusedUsingDirective, "using N1Alias = N1;").WithLocation(2, 1),
                // (1,1): hidden CS8019: Unnecessary using directive.
                // using System;
                Diagnostic(ErrorCode.HDN_UnusedUsingDirective, "using System;").WithLocation(1, 1));
        }

        [Fact]
        public void CS7002ERR_UnexpectedGenericName()
        {
            var test = @"enum E<T> {    One, Two, Three   }

public class Test
{
    public int this<V>[V v]  {    get { return 0; }   } 
}
";

            // Native Compiler : CS1675 etc.
            ParseAndValidate(test,
    // (1,6): error CS7002: Unexpected use of a generic name
    // enum E<T> {    One, Two, Three   }
    Diagnostic(ErrorCode.ERR_UnexpectedGenericName, "E"),
    // (5,16): error CS7002: Unexpected use of a generic name
    //     public int this<V>[V v]  {    get { return 0; }   } 
    Diagnostic(ErrorCode.ERR_UnexpectedGenericName, "this"));
        }

        [Fact, WorkItem(546212, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/546212")]
        public void InvalidQueryExpression()
        {
            var text = @"
using System;
using System.Linq;

public class QueryExpressionTest
{
    public static void Main()
    {
        var expr1 = new[] { 1, 2, 3 };
        var expr2 = new[] { 1, 2, 3 };

        var query13 = from  const in expr1 join  i in expr2 on const equals i select new { const, i };
    }
}
";

            // error CS1002: ; expected
            // error CS1031: Type expected
            // error CS1525: Invalid expression term 'in' ... ...
            ParseAndValidate(text,
              // (12,29): error CS1002: ; expected
              //         var query13 = from  const in expr1 join  i in expr2 on const equals i select new { const, i };
              Diagnostic(ErrorCode.ERR_SemicolonExpected, "const"),
              // (12,35): error CS1031: Type expected
              //         var query13 = from  const in expr1 join  i in expr2 on const equals i select new { const, i };
              Diagnostic(ErrorCode.ERR_TypeExpected, "in"),
              // (12,35): error CS1001: Identifier expected
              //         var query13 = from  const in expr1 join  i in expr2 on const equals i select new { const, i };
              Diagnostic(ErrorCode.ERR_IdentifierExpected, "in"),
              // (12,35): error CS0145: A const field requires a value to be provided
              //         var query13 = from  const in expr1 join  i in expr2 on const equals i select new { const, i };
              Diagnostic(ErrorCode.ERR_ConstValueRequired, "in"),
              // (12,35): error CS1003: Syntax error, ',' expected
              //         var query13 = from  const in expr1 join  i in expr2 on const equals i select new { const, i };
              Diagnostic(ErrorCode.ERR_SyntaxError, "in").WithArguments(","),
              // (12,38): error CS1002: ; expected
              //         var query13 = from  const in expr1 join  i in expr2 on const equals i select new { const, i };
              Diagnostic(ErrorCode.ERR_SemicolonExpected, "expr1"),
              // (12,50): error CS1002: ; expected
              //         var query13 = from  const in expr1 join  i in expr2 on const equals i select new { const, i };
              Diagnostic(ErrorCode.ERR_SemicolonExpected, "i"),
              // (12,52): error CS1002: ; expected
              //         var query13 = from  const in expr1 join  i in expr2 on const equals i select new { const, i };
              Diagnostic(ErrorCode.ERR_SemicolonExpected, "in"),
              // (12,52): error CS1513: } expected
              //         var query13 = from  const in expr1 join  i in expr2 on const equals i select new { const, i };
              Diagnostic(ErrorCode.ERR_RbraceExpected, "in"),
              // (12,64): error CS1002: ; expected
              //         var query13 = from  const in expr1 join  i in expr2 on const equals i select new { const, i };
              Diagnostic(ErrorCode.ERR_SemicolonExpected, "const"),
              // (12,77): error CS0145: A const field requires a value to be provided
              //         var query13 = from  const in expr1 join  i in expr2 on const equals i select new { const, i };
              Diagnostic(ErrorCode.ERR_ConstValueRequired, "i"),
              // (12,79): error CS1002: ; expected
              //         var query13 = from  const in expr1 join  i in expr2 on const equals i select new { const, i };
              Diagnostic(ErrorCode.ERR_SemicolonExpected, "select"),
              // (12,86): error CS1002: ; expected
              //         var query13 = from  const in expr1 join  i in expr2 on const equals i select new { const, i };
              Diagnostic(ErrorCode.ERR_SemicolonExpected, "new"),
              // (12,92): error CS1513: } expected
              //         var query13 = from  const in expr1 join  i in expr2 on const equals i select new { const, i };
              Diagnostic(ErrorCode.ERR_RbraceExpected, "const"),
              // (12,92): error CS1002: ; expected
              //         var query13 = from  const in expr1 join  i in expr2 on const equals i select new { const, i };
              Diagnostic(ErrorCode.ERR_SemicolonExpected, "const"),
              // (12,97): error CS1031: Type expected
              //         var query13 = from  const in expr1 join  i in expr2 on const equals i select new { const, i };
              Diagnostic(ErrorCode.ERR_TypeExpected, ","),
              // (12,97): error CS1001: Identifier expected
              //         var query13 = from  const in expr1 join  i in expr2 on const equals i select new { const, i };
              Diagnostic(ErrorCode.ERR_IdentifierExpected, ","),
              // (12,97): error CS0145: A const field requires a value to be provided
              //         var query13 = from  const in expr1 join  i in expr2 on const equals i select new { const, i };
              Diagnostic(ErrorCode.ERR_ConstValueRequired, ","),
              // (12,99): error CS0145: A const field requires a value to be provided
              //         var query13 = from  const in expr1 join  i in expr2 on const equals i select new { const, i };
              Diagnostic(ErrorCode.ERR_ConstValueRequired, "i"),
              // (12,101): error CS1002: ; expected
              //         var query13 = from  const in expr1 join  i in expr2 on const equals i select new { const, i };
              Diagnostic(ErrorCode.ERR_SemicolonExpected, "}"),
              // (12,102): error CS1597: Semicolon after method or accessor block is not valid
              //         var query13 = from  const in expr1 join  i in expr2 on const equals i select new { const, i };
              Diagnostic(ErrorCode.ERR_UnexpectedSemicolon, ";"),
              // (14,1): error CS1022: Type or namespace definition, or end-of-file expected
              // }
              Diagnostic(ErrorCode.ERR_EOFExpected, "}")
                );
        }

        [Fact]
        public void PartialTypesBeforeVersionTwo()
        {
            var text = @"
partial class C
{
}
";
            var tree = SyntaxFactory.ParseSyntaxTree(text, options: CSharpParseOptions.Default.WithLanguageVersion(LanguageVersion.CSharp5));
            tree.GetCompilationUnitRoot().GetDiagnostics().Verify();

            tree = SyntaxFactory.ParseSyntaxTree(text, options: CSharpParseOptions.Default.WithLanguageVersion(LanguageVersion.CSharp1));
            tree.GetCompilationUnitRoot().GetDiagnostics().Verify();

            CreateCompilation(text, parseOptions: TestOptions.Regular5).VerifyDiagnostics();
            CreateCompilation(text, parseOptions: TestOptions.Regular1).VerifyDiagnostics(
                // (2,1): error CS8022: Feature 'partial types' is not available in C# 1. Please use language version 2 or greater.
                // partial class C
                Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion1, "partial").WithArguments("partial types", "2").WithLocation(2, 1));
        }

        [Fact]
        public void PartialMethodsVersionThree()
        {
            var text = @"
class C
{
    partial int Goo() { }
}
";
            var tree = SyntaxFactory.ParseSyntaxTree(text, options: CSharpParseOptions.Default.WithLanguageVersion(LanguageVersion.CSharp5));
            tree.GetCompilationUnitRoot().GetDiagnostics().Verify();

            tree = SyntaxFactory.ParseSyntaxTree(text, options: CSharpParseOptions.Default.WithLanguageVersion(LanguageVersion.CSharp2));
            tree.GetCompilationUnitRoot().GetDiagnostics().Verify();

            CreateCompilation(text, parseOptions: TestOptions.Regular5).VerifyDiagnostics(
                // (4,17): error CS0759: No defining declaration found for implementing declaration of partial method 'C.Goo()'
                //     partial int Goo() { }
                Diagnostic(ErrorCode.ERR_PartialMethodMustHaveLatent, "Goo").WithArguments("C.Goo()").WithLocation(4, 17),
                // (4,17): error CS0751: A partial member must be declared within a partial type
                //     partial int Goo() { }
                Diagnostic(ErrorCode.ERR_PartialMemberOnlyInPartialClass, "Goo").WithLocation(4, 17),
                // (4,17): error CS8796: Partial method 'C.Goo()' must have accessibility modifiers because it has a non-void return type.
                //     partial int Goo() { }
                Diagnostic(ErrorCode.ERR_PartialMethodWithNonVoidReturnMustHaveAccessMods, "Goo").WithArguments("C.Goo()").WithLocation(4, 17),
                // (4,17): error CS0161: 'C.Goo()': not all code paths return a value
                //     partial int Goo() { }
                Diagnostic(ErrorCode.ERR_ReturnExpected, "Goo").WithArguments("C.Goo()").WithLocation(4, 17));
            CreateCompilation(text, parseOptions: TestOptions.Regular2).VerifyDiagnostics(
                // (4,5): error CS8023: Feature 'partial method' is not available in C# 2. Please use language version 3 or greater.
                //     partial int Goo() { }
                Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion2, "partial").WithArguments("partial method", "3").WithLocation(4, 5),
                // (4,17): error CS0759: No defining declaration found for implementing declaration of partial method 'C.Goo()'
                //     partial int Goo() { }
                Diagnostic(ErrorCode.ERR_PartialMethodMustHaveLatent, "Goo").WithArguments("C.Goo()").WithLocation(4, 17),
                // (4,17): error CS0751: A partial member must be declared within a partial type
                //     partial int Goo() { }
                Diagnostic(ErrorCode.ERR_PartialMemberOnlyInPartialClass, "Goo").WithLocation(4, 17),
                // (4,17): error CS8796: Partial method 'C.Goo()' must have accessibility modifiers because it has a non-void return type.
                //     partial int Goo() { }
                Diagnostic(ErrorCode.ERR_PartialMethodWithNonVoidReturnMustHaveAccessMods, "Goo").WithArguments("C.Goo()").WithLocation(4, 17),
                // (4,17): error CS0161: 'C.Goo()': not all code paths return a value
                //     partial int Goo() { }
                Diagnostic(ErrorCode.ERR_ReturnExpected, "Goo").WithArguments("C.Goo()").WithLocation(4, 17));
        }

        [Fact]
        public void QueryBeforeVersionThree()
        {
            var text = @"
class C
{
    void Goo()
    {
        var q = from a in b
                select c;
    }
}
";
            CreateCompilation(text, parseOptions: TestOptions.Regular5).VerifyDiagnostics(
                // (6,27): error CS0103: The name 'b' does not exist in the current context
                //         var q = from a in b
                Diagnostic(ErrorCode.ERR_NameNotInContext, "b").WithArguments("b"));
            CreateCompilation(text, parseOptions: TestOptions.Regular2).VerifyDiagnostics(
                // (6,9): error CS8023: Feature 'implicitly typed local variable' is not available in C# 2. Please use language version 3 or greater.
                //         var q = from a in b
                Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion2, "var").WithArguments("implicitly typed local variable", "3").WithLocation(6, 9),
                // (6,17): error CS8023: Feature 'query expression' is not available in C# 2. Please use language version 3 or greater.
                //         var q = from a in b
                Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion2, "from").WithArguments("query expression", "3").WithLocation(6, 17),
                // (6,27): error CS0103: The name 'b' does not exist in the current context
                //         var q = from a in b
                Diagnostic(ErrorCode.ERR_NameNotInContext, "b").WithArguments("b").WithLocation(6, 27));

            var tree = SyntaxFactory.ParseSyntaxTree(text, options: CSharpParseOptions.Default.WithLanguageVersion(LanguageVersion.CSharp5));
            tree.GetCompilationUnitRoot().GetDiagnostics().Verify();

            tree = SyntaxFactory.ParseSyntaxTree(text, options: CSharpParseOptions.Default.WithLanguageVersion(LanguageVersion.CSharp2));
            tree.GetCompilationUnitRoot().GetDiagnostics().Verify();
        }

        [Fact]
        public void AnonymousTypeBeforeVersionThree()
        {
            var text = @"
class C
{
    void Goo()
    {
        var q = new { };
    }
}
";
            var tree = SyntaxFactory.ParseSyntaxTree(text, options: CSharpParseOptions.Default.WithLanguageVersion(LanguageVersion.CSharp5));
            tree.GetCompilationUnitRoot().GetDiagnostics().Verify();

            tree = SyntaxFactory.ParseSyntaxTree(text, options: CSharpParseOptions.Default.WithLanguageVersion(LanguageVersion.CSharp2));
            tree.GetCompilationUnitRoot().GetDiagnostics().Verify();

            CreateCompilation(text, parseOptions: TestOptions.Regular5).VerifyDiagnostics();
            CreateCompilation(text, parseOptions: TestOptions.Regular2).VerifyDiagnostics(
                // (6,9): error CS8023: Feature 'implicitly typed local variable' is not available in C# 2. Please use language version 3 or greater.
                //         var q = new { };
                Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion2, "var").WithArguments("implicitly typed local variable", "3").WithLocation(6, 9),
                // (6,17): error CS8023: Feature 'anonymous types' is not available in C# 2. Please use language version 3 or greater.
                //         var q = new { };
                Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion2, "new").WithArguments("anonymous types", "3").WithLocation(6, 17));
        }

        [Fact]
        public void ImplicitArrayBeforeVersionThree()
        {
            var text = @"
class C
{
    void Goo()
    {
        var q = new [] { };
    }
}
";
            CreateCompilation(text, parseOptions: TestOptions.Regular5).VerifyDiagnostics(
                // (6,17): error CS0826: No best type found for implicitly-typed array
                //         var q = new [] { };
                Diagnostic(ErrorCode.ERR_ImplicitlyTypedArrayNoBestType, "new [] { }").WithLocation(6, 17));
            CreateCompilation(text, parseOptions: TestOptions.Regular2).VerifyDiagnostics(
                // (6,9): error CS8023: Feature 'implicitly typed local variable' is not available in C# 2. Please use language version 3 or greater.
                //         var q = new [] { };
                Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion2, "var").WithArguments("implicitly typed local variable", "3").WithLocation(6, 9),
                // (6,17): error CS8023: Feature 'implicitly typed array' is not available in C# 2. Please use language version 3 or greater.
                //         var q = new [] { };
                Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion2, "new").WithArguments("implicitly typed array", "3").WithLocation(6, 17),
                // (6,17): error CS0826: No best type found for implicitly-typed array
                //         var q = new [] { };
                Diagnostic(ErrorCode.ERR_ImplicitlyTypedArrayNoBestType, "new [] { }").WithLocation(6, 17));

            var tree = SyntaxFactory.ParseSyntaxTree(text, options: CSharpParseOptions.Default.WithLanguageVersion(LanguageVersion.CSharp5));
            tree.GetCompilationUnitRoot().GetDiagnostics().Verify();

            tree = SyntaxFactory.ParseSyntaxTree(text, options: CSharpParseOptions.Default.WithLanguageVersion(LanguageVersion.CSharp2));
            tree.GetCompilationUnitRoot().GetDiagnostics().Verify();
        }

        [Fact]
        public void ObjectInitializerBeforeVersionThree()
        {
            var text = @"
class C
{
    void Goo()
    {
        var q = new Goo { };
    }
}
";

            CreateCompilation(text, parseOptions: TestOptions.Regular5).VerifyEmitDiagnostics(
                // (6,21): error CS0246: The type or namespace name 'Goo' could not be found (are you missing a using directive or an assembly reference?)
                //         var q = new Goo { };
                Diagnostic(ErrorCode.ERR_SingleTypeNameNotFound, "Goo").WithArguments("Goo").WithLocation(6, 21));
            CreateCompilation(text, parseOptions: TestOptions.Regular2).VerifyEmitDiagnostics(
                // (6,9): error CS8023: Feature 'implicitly typed local variable' is not available in C# 2. Please use language version 3 or greater.
                //         var q = new Goo { };
                Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion2, "var").WithArguments("implicitly typed local variable", "3").WithLocation(6, 9),
                // (6,21): error CS0246: The type or namespace name 'Goo' could not be found (are you missing a using directive or an assembly reference?)
                //         var q = new Goo { };
                Diagnostic(ErrorCode.ERR_SingleTypeNameNotFound, "Goo").WithArguments("Goo").WithLocation(6, 21),
                // (6,25): error CS8023: Feature 'object initializer' is not available in C# 2. Please use language version 3 or greater.
                //         var q = new Goo { };
                Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion2, "{").WithArguments("object initializer", "3").WithLocation(6, 25));

            var tree = SyntaxFactory.ParseSyntaxTree(text, options: CSharpParseOptions.Default.WithLanguageVersion(LanguageVersion.CSharp5));
            tree.GetCompilationUnitRoot().GetDiagnostics().Verify();

            tree = SyntaxFactory.ParseSyntaxTree(text, options: CSharpParseOptions.Default.WithLanguageVersion(LanguageVersion.CSharp2));
            tree.GetCompilationUnitRoot().GetDiagnostics().Verify();
        }

        [Fact]
        public void LambdaBeforeVersionThree()
        {
            var text = @"
class C
{
    void Goo()
    {
        var q = a => b;
    }
}
";
            CreateCompilation(text, parseOptions: TestOptions.Regular5).VerifyDiagnostics(
                // (6,17): error CS8026: Feature 'inferred delegate type' is not available in C# 5. Please use language version 10.0 or greater.
                //         var q = a => b;
                Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion5, "a => b").WithArguments("inferred delegate type", "10.0").WithLocation(6, 17));
            CreateCompilation(text, parseOptions: TestOptions.Regular2).VerifyDiagnostics(
                // (6,9): error CS8023: Feature 'implicitly typed local variable' is not available in C# 2. Please use language version 3 or greater.
                //         var q = a => b;
                Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion2, "var").WithArguments("implicitly typed local variable", "3").WithLocation(6, 9),
                // (6,17): error CS8023: Feature 'inferred delegate type' is not available in C# 2. Please use language version 10.0 or greater.
                //         var q = a => b;
                Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion2, "a => b").WithArguments("inferred delegate type", "10.0").WithLocation(6, 17),
                // (6,19): error CS8023: Feature 'lambda expression' is not available in C# 2. Please use language version 3 or greater.
                //         var q = a => b;
                Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion2, "=>").WithArguments("lambda expression", "3").WithLocation(6, 19));

            var tree = SyntaxFactory.ParseSyntaxTree(text, options: CSharpParseOptions.Default.WithLanguageVersion(LanguageVersion.CSharp5));
            tree.GetCompilationUnitRoot().GetDiagnostics().Verify();

            tree = SyntaxFactory.ParseSyntaxTree(text, options: CSharpParseOptions.Default.WithLanguageVersion(LanguageVersion.CSharp2));
            tree.GetCompilationUnitRoot().GetDiagnostics().Verify();
        }

        [Fact]
        public void ExceptionFilterBeforeVersionSix()
        {
            var text = @"
public class C 
{
    public static int Main()
    {
        try { } catch when (true) {}
    }
}
";
            CreateCompilation(text, parseOptions: TestOptions.Regular6).VerifyDiagnostics(
                // (4,23): error CS0161: 'C.Main()': not all code paths return a value
                //     public static int Main()
                Diagnostic(ErrorCode.ERR_ReturnExpected, "Main").WithArguments("C.Main()").WithLocation(4, 23),
                // (6,29): warning CS7095: Filter expression is a constant 'true', consider removing the filter
                //         try { } catch when (true) {}
                Diagnostic(ErrorCode.WRN_FilterIsConstantTrue, "true").WithLocation(6, 29));
            CreateCompilation(text, parseOptions: TestOptions.Regular5).VerifyDiagnostics(
                // (4,23): error CS0161: 'C.Main()': not all code paths return a value
                //     public static int Main()
                Diagnostic(ErrorCode.ERR_ReturnExpected, "Main").WithArguments("C.Main()").WithLocation(4, 23),
                // (6,23): error CS8026: Feature 'exception filter' is not available in C# 5. Please use language version 6 or greater.
                //         try { } catch when (true) {}
                Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion5, "when").WithArguments("exception filter", "6").WithLocation(6, 23),
                // (6,29): warning CS7095: Filter expression is a constant 'true', consider removing the filter
                //         try { } catch when (true) {}
                Diagnostic(ErrorCode.WRN_FilterIsConstantTrue, "true").WithLocation(6, 29));

            var tree = SyntaxFactory.ParseSyntaxTree(text, options: CSharpParseOptions.Default.WithLanguageVersion(LanguageVersion.CSharp6));
            tree.GetDiagnostics().Verify();

            tree = Parse(text, options: CSharpParseOptions.Default.WithLanguageVersion(LanguageVersion.CSharp5));
            tree.GetDiagnostics().Verify();
        }

        [Fact]
        public void MissingCommaInAttribute()
        {
            var text =
@"[One Two] // error: missing comma
class TestClass { }";
            var tree = UsingTree(text,
                // (1,6): error CS1003: Syntax error, ',' expected
                // [One Two] // error: missing comma
                Diagnostic(ErrorCode.ERR_SyntaxError, "Two").WithArguments(",").WithLocation(1, 6)
                );
            N(SyntaxKind.CompilationUnit);
            {
                N(SyntaxKind.ClassDeclaration);
                {
                    N(SyntaxKind.AttributeList);
                    {
                        N(SyntaxKind.OpenBracketToken);
                        N(SyntaxKind.Attribute);
                        {
                            N(SyntaxKind.IdentifierName);
                            {
                                N(SyntaxKind.IdentifierToken, "One");
                            }
                        }
                        M(SyntaxKind.CommaToken);
                        N(SyntaxKind.Attribute);
                        {
                            N(SyntaxKind.IdentifierName);
                            {
                                N(SyntaxKind.IdentifierToken, "Two");
                            }
                        }
                        N(SyntaxKind.CloseBracketToken);
                    }
                }
                N(SyntaxKind.ClassKeyword);
                N(SyntaxKind.IdentifierToken, "TestClass");
                N(SyntaxKind.OpenBraceToken);
                N(SyntaxKind.CloseBraceToken);
            }
            N(SyntaxKind.EndOfFileToken);
        }

        #endregion

        #region "Targeted Warning Tests - please arrange tests in the order of error code"

        [Fact]
        public void CS0440WRN_GlobalAliasDefn()
        {
            var test = @"
using global = MyClass;   // CS0440
class MyClass
{
    static void Main()
    {
        // Note how global refers to the global namespace
        // even though it is redefined above.
        global::System.Console.WriteLine();
    }
}
";

            CreateCompilation(test).VerifyDiagnostics(
                // (2,7): warning CS0440: Defining an alias named 'global' is ill-advised since 'global::' always references the global namespace and not an alias
                // using global = MyClass;   // CS0440
                Diagnostic(ErrorCode.WRN_GlobalAliasDefn, "global").WithLocation(2, 7),
                // (2,7): warning CS8981: The type name 'global' only contains lower-cased ascii characters. Such names may become reserved for the language.
                // using global = MyClass;   // CS0440
                Diagnostic(ErrorCode.WRN_LowerCaseTypeName, "global").WithArguments("global").WithLocation(2, 7),
                // (2,1): hidden CS8019: Unnecessary using directive.
                // using global = MyClass;   // CS0440
                Diagnostic(ErrorCode.HDN_UnusedUsingDirective, "using global = MyClass;").WithLocation(2, 1));
        }

        [Fact]
        public void CS0642WRN_PossibleMistakenNullStatement()
        {
            var test = @"
class MyClass
{
    public static int Main(System.Collections.IEnumerable e)
    {
        for (int i = 0; i < 10; i += 1);
        foreach (var v in e);
        while(false);

        if(true);else;
        using(null);
        lock(null);
        do;while(false);

        for (int i = 0; i < 10; i += 1);{}   // CS0642, semicolon intentional?
        foreach (var v in e);{}
        while(false);{}

        return 0;
    }
}
";

            CreateCompilation(test).VerifyDiagnostics(
                // (10,17): warning CS0642: Possible mistaken empty statement
                //         if(true);else;
                Diagnostic(ErrorCode.WRN_PossibleMistakenNullStatement, ";").WithLocation(10, 17),
                // (10,22): warning CS0642: Possible mistaken empty statement
                //         if(true);else;
                Diagnostic(ErrorCode.WRN_PossibleMistakenNullStatement, ";").WithLocation(10, 22),
                // (11,20): warning CS0642: Possible mistaken empty statement
                //         using(null);
                Diagnostic(ErrorCode.WRN_PossibleMistakenNullStatement, ";").WithLocation(11, 20),
                // (12,19): warning CS0642: Possible mistaken empty statement
                //         lock(null);
                Diagnostic(ErrorCode.WRN_PossibleMistakenNullStatement, ";").WithLocation(12, 19),
                // (13,11): warning CS0642: Possible mistaken empty statement
                //         do;while(false);
                Diagnostic(ErrorCode.WRN_PossibleMistakenNullStatement, ";").WithLocation(13, 11),
                // (15,40): warning CS0642: Possible mistaken empty statement
                //         for (int i = 0; i < 10; i += 1);{}   // CS0642, semicolon intentional?
                Diagnostic(ErrorCode.WRN_PossibleMistakenNullStatement, ";").WithLocation(15, 40),
                // (16,29): warning CS0642: Possible mistaken empty statement
                //         foreach (var v in e);{}
                Diagnostic(ErrorCode.WRN_PossibleMistakenNullStatement, ";").WithLocation(16, 29),
                // (17,21): warning CS0642: Possible mistaken empty statement
                //         while(false);{}
                Diagnostic(ErrorCode.WRN_PossibleMistakenNullStatement, ";").WithLocation(17, 21));
        }

        [Fact]
        public void CS0642_DoNotWarnForMissingEmptyStatement()
        {
            var test = @"
class MyClass
{
    public static int Main(bool b)
    {
        if (b)
    
    public
";

            CreateCompilation(test).VerifyDiagnostics(
                // (6,15): error CS1002: ; expected
                //         if (b)
                Diagnostic(ErrorCode.ERR_SemicolonExpected, "").WithLocation(6, 15),
                // (6,15): error CS1513: } expected
                //         if (b)
                Diagnostic(ErrorCode.ERR_RbraceExpected, "").WithLocation(6, 15),
                // (9,1): error CS1519: Invalid token '' in class, record, struct, or interface member declaration
                // 
                Diagnostic(ErrorCode.ERR_InvalidMemberDecl, "").WithArguments("").WithLocation(9, 1),
                // (8,11): error CS1513: } expected
                //     public
                Diagnostic(ErrorCode.ERR_RbraceExpected, "").WithLocation(8, 11),
                // (4,23): error CS0161: 'MyClass.Main(bool)': not all code paths return a value
                //     public static int Main(bool b)
                Diagnostic(ErrorCode.ERR_ReturnExpected, "Main").WithArguments("MyClass.Main(bool)").WithLocation(4, 23));
        }

        [Fact, WorkItem(529895, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/529895")]
        public void AttributeInMethodBody()
        {
            var test = @"
public class Class1 
{
    int Meth2 (int parm) {[Goo(5)]return 0;}
}
";
            CreateCompilation(test).GetDiagnostics().Verify(
                // (4,27): error CS7014: Attributes are not valid in this context.
                //     int Meth2 (int parm) {[Goo(5)]return 0;}
                Diagnostic(ErrorCode.ERR_AttributesNotAllowed, "[Goo(5)]").WithLocation(4, 27)
            );
        }

        // Preprocessor:
        [Fact]
        public void CS1030WRN_WarningDirectivepp()
        {
            //the single-line comment is handled differently from other trivia in the directive
            var test = @"
class Test
{
    static void Main()
    {
#warning //This is a WARNING!
    }
}
";

            ParseAndValidate(test, Diagnostic(ErrorCode.WRN_WarningDirective, "//This is a WARNING!").WithArguments("//This is a WARNING!"));
        }

        [Fact]
        public void CS1522WRN_EmptySwitch()
        {
            var test = @"
class Test
{
    public static int Main()
    {
        int i = 6;
        switch(i)   // CS1522
        {}
        return 0;
    }
}
";
            ParseAndValidate(test);
            CreateCompilation(test).VerifyDiagnostics(
                // (8,9): warning CS1522: Empty switch block
                //         {}
                Diagnostic(ErrorCode.WRN_EmptySwitch, "{").WithLocation(8, 9)
                );
        }

        [Fact]
        public void PartialMethodInCSharp2()
        {
            var test = @"
partial class X
{
    partial void M();
}
";
            CreateCompilation(test, parseOptions: CSharpParseOptions.Default.WithLanguageVersion(LanguageVersion.CSharp2)).VerifyDiagnostics(
                // (4,5): error CS8023: Feature 'partial method' is not available in C# 2. Please use language version 3 or greater.
                //     partial void M();
                Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion2, "partial").WithArguments("partial method", "3"));
        }

        [Fact]
        public void InterpolatedStringBeforeCSharp6()
        {
            var text = @"
class C
{
    string M()
    {
        return $""hello"";
    }
}";

            // Moved to be a semantic diagnostic.
            var tree = SyntaxFactory.ParseSyntaxTree(text, options: CSharpParseOptions.Default.WithLanguageVersion(LanguageVersion.CSharp5));
            tree.GetCompilationUnitRoot().GetDiagnostics().Verify();
        }

        [Fact]
        public void InterpolatedStringWithReplacementBeforeCSharp6()
        {
            var text = @"
class C
{
    string M()
    {
        string other = ""world"";
        return $""hello + {other}"";
    }
}";

            // Moved to be a semantic diagnostic.
            var tree = SyntaxFactory.ParseSyntaxTree(text, options: CSharpParseOptions.Default.WithLanguageVersion(LanguageVersion.CSharp5));
            tree.GetCompilationUnitRoot().GetDiagnostics().Verify();
        }

        [Fact, WorkItem(529870, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/529870")]
        public void AsyncBeforeCSharp5()
        {
            var text = @"
class C
{
    async void M() { }
}
";
            var tree = SyntaxFactory.ParseSyntaxTree(text, options: CSharpParseOptions.Default.WithLanguageVersion(LanguageVersion.CSharp5));
            tree.GetCompilationUnitRoot().GetDiagnostics().Verify();

            CreateCompilation(text, parseOptions: TestOptions.Regular5).VerifyDiagnostics(
                // (4,16): warning CS1998: This async method lacks 'await' operators and will run synchronously. Consider using the 'await' operator to await non-blocking API calls, or 'await Task.Run(...)' to do CPU-bound work on a background thread.
                //     async void M() { }
                Diagnostic(ErrorCode.WRN_AsyncLacksAwaits, "M").WithLocation(4, 16));

            tree = SyntaxFactory.ParseSyntaxTree(text, options: CSharpParseOptions.Default.WithLanguageVersion(LanguageVersion.CSharp3));
            tree.GetCompilationUnitRoot().GetDiagnostics().Verify();

            CreateCompilation(text, parseOptions: TestOptions.Regular3).VerifyDiagnostics(
                // (4,16): error CS8024: Feature 'async function' is not available in C# 3. Please use language version 5 or greater.
                //     async void M() { }
                Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion3, "M").WithArguments("async function", "5").WithLocation(4, 16),
                // (4,16): warning CS1998: This async method lacks 'await' operators and will run synchronously. Consider using the 'await' operator to await non-blocking API calls, or 'await Task.Run(...)' to do CPU-bound work on a background thread.
                //     async void M() { }
                Diagnostic(ErrorCode.WRN_AsyncLacksAwaits, "M").WithLocation(4, 16));
        }

        [Fact, WorkItem(529870, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/529870")]
        public void AsyncWithOtherModifiersBeforeCSharp5()
        {
            var text = @"
class C
{
    async static void M() { }
}
";
            var tree = SyntaxFactory.ParseSyntaxTree(text, options: CSharpParseOptions.Default.WithLanguageVersion(LanguageVersion.CSharp5));
            tree.GetCompilationUnitRoot().GetDiagnostics().Verify();
            CreateCompilation(text, parseOptions: TestOptions.Regular5).VerifyDiagnostics(
                // (4,23): warning CS1998: This async method lacks 'await' operators and will run synchronously. Consider using the 'await' operator to await non-blocking API calls, or 'await Task.Run(...)' to do CPU-bound work on a background thread.
                //     async static void M() { }
                Diagnostic(ErrorCode.WRN_AsyncLacksAwaits, "M").WithLocation(4, 23));

            tree = SyntaxFactory.ParseSyntaxTree(text, options: CSharpParseOptions.Default.WithLanguageVersion(LanguageVersion.CSharp3));
            tree.GetCompilationUnitRoot().GetDiagnostics().Verify();
            CreateCompilation(text, parseOptions: TestOptions.Regular3).VerifyDiagnostics(
                // (4,23): error CS8024: Feature 'async function' is not available in C# 3. Please use language version 5 or greater.
                //     async static void M() { }
                Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion3, "M").WithArguments("async function", "5").WithLocation(4, 23),
                // (4,23): warning CS1998: This async method lacks 'await' operators and will run synchronously. Consider using the 'await' operator to await non-blocking API calls, or 'await Task.Run(...)' to do CPU-bound work on a background thread.
                //     async static void M() { }
                Diagnostic(ErrorCode.WRN_AsyncLacksAwaits, "M").WithLocation(4, 23));
        }

        [Fact]
        public void AsyncLambdaBeforeCSharp5()
        {
            var text = @"
class C
{
    static void Main()
    {
        Func<int, Task<int>> f = async x => x;
    }
}
";
            var tree = SyntaxFactory.ParseSyntaxTree(text, options: CSharpParseOptions.Default.WithLanguageVersion(LanguageVersion.CSharp5));
            tree.GetCompilationUnitRoot().GetDiagnostics().Verify();
            CreateCompilation(text, parseOptions: TestOptions.Regular5).VerifyDiagnostics(
                // (6,9): error CS0246: The type or namespace name 'Func<,>' could not be found (are you missing a using directive or an assembly reference?)
                //         Func<int, Task<int>> f = async x => x;
                Diagnostic(ErrorCode.ERR_SingleTypeNameNotFound, "Func<int, Task<int>>").WithArguments("Func<,>").WithLocation(6, 9),
                // (6,19): error CS0246: The type or namespace name 'Task<>' could not be found (are you missing a using directive or an assembly reference?)
                //         Func<int, Task<int>> f = async x => x;
                Diagnostic(ErrorCode.ERR_SingleTypeNameNotFound, "Task<int>").WithArguments("Task<>").WithLocation(6, 19),
                // (6,42): warning CS1998: This async method lacks 'await' operators and will run synchronously. Consider using the 'await' operator to await non-blocking API calls, or 'await Task.Run(...)' to do CPU-bound work on a background thread.
                //         Func<int, Task<int>> f = async x => x;
                Diagnostic(ErrorCode.WRN_AsyncLacksAwaits, "=>").WithLocation(6, 42));

            tree = SyntaxFactory.ParseSyntaxTree(text, options: CSharpParseOptions.Default.WithLanguageVersion(LanguageVersion.CSharp4));
            tree.GetCompilationUnitRoot().GetDiagnostics().Verify();
            CreateCompilation(text, parseOptions: TestOptions.Regular4).VerifyDiagnostics(
                // (6,9): error CS0246: The type or namespace name 'Func<,>' could not be found (are you missing a using directive or an assembly reference?)
                //         Func<int, Task<int>> f = async x => x;
                Diagnostic(ErrorCode.ERR_SingleTypeNameNotFound, "Func<int, Task<int>>").WithArguments("Func<,>").WithLocation(6, 9),
                // (6,19): error CS0246: The type or namespace name 'Task<>' could not be found (are you missing a using directive or an assembly reference?)
                //         Func<int, Task<int>> f = async x => x;
                Diagnostic(ErrorCode.ERR_SingleTypeNameNotFound, "Task<int>").WithArguments("Task<>").WithLocation(6, 19),
                // (6,34): error CS8025: Feature 'async function' is not available in C# 4. Please use language version 5 or greater.
                //         Func<int, Task<int>> f = async x => x;
                Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion4, "async").WithArguments("async function", "5").WithLocation(6, 34),
                // (6,42): warning CS1998: This async method lacks 'await' operators and will run synchronously. Consider using the 'await' operator to await non-blocking API calls, or 'await Task.Run(...)' to do CPU-bound work on a background thread.
                //         Func<int, Task<int>> f = async x => x;
                Diagnostic(ErrorCode.WRN_AsyncLacksAwaits, "=>").WithLocation(6, 42));
        }

        [Fact]
        public void AsyncDelegateBeforeCSharp5()
        {
            var text = @"
class C
{
    static void Main()
    {
        Func<int, Task<int>> f = async delegate (int x) { return x; };
    }
}
";
            var tree = SyntaxFactory.ParseSyntaxTree(text, options: CSharpParseOptions.Default.WithLanguageVersion(LanguageVersion.CSharp5));
            tree.GetCompilationUnitRoot().GetDiagnostics().Verify();
            CreateCompilation(text, parseOptions: TestOptions.Regular5).VerifyDiagnostics(
                // (6,9): error CS0246: The type or namespace name 'Func<,>' could not be found (are you missing a using directive or an assembly reference?)
                //         Func<int, Task<int>> f = async delegate (int x) { return x; };
                Diagnostic(ErrorCode.ERR_SingleTypeNameNotFound, "Func<int, Task<int>>").WithArguments("Func<,>").WithLocation(6, 9),
                // (6,19): error CS0246: The type or namespace name 'Task<>' could not be found (are you missing a using directive or an assembly reference?)
                //         Func<int, Task<int>> f = async delegate (int x) { return x; };
                Diagnostic(ErrorCode.ERR_SingleTypeNameNotFound, "Task<int>").WithArguments("Task<>").WithLocation(6, 19),
                // (6,40): warning CS1998: This async method lacks 'await' operators and will run synchronously. Consider using the 'await' operator to await non-blocking API calls, or 'await Task.Run(...)' to do CPU-bound work on a background thread.
                //         Func<int, Task<int>> f = async delegate (int x) { return x; };
                Diagnostic(ErrorCode.WRN_AsyncLacksAwaits, "delegate").WithLocation(6, 40));

            tree = SyntaxFactory.ParseSyntaxTree(text, options: CSharpParseOptions.Default.WithLanguageVersion(LanguageVersion.CSharp4));
            tree.GetCompilationUnitRoot().GetDiagnostics().Verify();
            CreateCompilation(text, parseOptions: TestOptions.Regular4).VerifyDiagnostics(
                // (6,9): error CS0246: The type or namespace name 'Func<,>' could not be found (are you missing a using directive or an assembly reference?)
                //         Func<int, Task<int>> f = async delegate (int x) { return x; };
                Diagnostic(ErrorCode.ERR_SingleTypeNameNotFound, "Func<int, Task<int>>").WithArguments("Func<,>").WithLocation(6, 9),
                // (6,19): error CS0246: The type or namespace name 'Task<>' could not be found (are you missing a using directive or an assembly reference?)
                //         Func<int, Task<int>> f = async delegate (int x) { return x; };
                Diagnostic(ErrorCode.ERR_SingleTypeNameNotFound, "Task<int>").WithArguments("Task<>").WithLocation(6, 19),
                // (6,34): error CS8025: Feature 'async function' is not available in C# 4. Please use language version 5 or greater.
                //         Func<int, Task<int>> f = async delegate (int x) { return x; };
                Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion4, "async").WithArguments("async function", "5").WithLocation(6, 34),
                // (6,40): warning CS1998: This async method lacks 'await' operators and will run synchronously. Consider using the 'await' operator to await non-blocking API calls, or 'await Task.Run(...)' to do CPU-bound work on a background thread.
                //         Func<int, Task<int>> f = async delegate (int x) { return x; };
                Diagnostic(ErrorCode.WRN_AsyncLacksAwaits, "delegate").WithLocation(6, 40));
        }

        [Fact]
        public void NamedArgumentBeforeCSharp4()
        {
            var text = @"
[Attr(x:1)]
class C
{
    C()
    {
        M(y:2);
    }
}
";
            CreateCompilation(text, parseOptions: TestOptions.Regular4).VerifyDiagnostics(
                // (2,2): error CS0246: The type or namespace name 'AttrAttribute' could not be found (are you missing a using directive or an assembly reference?)
                // [Attr(x:1)]
                Diagnostic(ErrorCode.ERR_SingleTypeNameNotFound, "Attr").WithArguments("AttrAttribute").WithLocation(2, 2),
                // (2,2): error CS0246: The type or namespace name 'Attr' could not be found (are you missing a using directive or an assembly reference?)
                // [Attr(x:1)]
                Diagnostic(ErrorCode.ERR_SingleTypeNameNotFound, "Attr").WithArguments("Attr").WithLocation(2, 2),
                // (7,9): error CS0103: The name 'M' does not exist in the current context
                //         M(y:2);
                Diagnostic(ErrorCode.ERR_NameNotInContext, "M").WithArguments("M").WithLocation(7, 9));
            CreateCompilation(text, parseOptions: TestOptions.Regular3).VerifyDiagnostics(
                // (2,2): error CS0246: The type or namespace name 'AttrAttribute' could not be found (are you missing a using directive or an assembly reference?)
                // [Attr(x:1)]
                Diagnostic(ErrorCode.ERR_SingleTypeNameNotFound, "Attr").WithArguments("AttrAttribute").WithLocation(2, 2),
                // (2,2): error CS0246: The type or namespace name 'Attr' could not be found (are you missing a using directive or an assembly reference?)
                // [Attr(x:1)]
                Diagnostic(ErrorCode.ERR_SingleTypeNameNotFound, "Attr").WithArguments("Attr").WithLocation(2, 2),
                // (2,7): error CS8024: Feature 'named argument' is not available in C# 3. Please use language version 4 or greater.
                // [Attr(x:1)]
                Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion3, "x:").WithArguments("named argument", "4").WithLocation(2, 7),
                // (7,9): error CS0103: The name 'M' does not exist in the current context
                //         M(y:2);
                Diagnostic(ErrorCode.ERR_NameNotInContext, "M").WithArguments("M").WithLocation(7, 9),
                // (7,11): error CS8024: Feature 'named argument' is not available in C# 3. Please use language version 4 or greater.
                //         M(y:2);
                Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion3, "y:").WithArguments("named argument", "4").WithLocation(7, 11));

            SyntaxFactory.ParseSyntaxTree(text, options: CSharpParseOptions.Default.WithLanguageVersion(LanguageVersion.CSharp4)).GetDiagnostics().Verify();
            SyntaxFactory.ParseSyntaxTree(text, options: CSharpParseOptions.Default.WithLanguageVersion(LanguageVersion.CSharp3)).GetDiagnostics().Verify();
        }

        [Fact]
        public void GlobalKeywordBeforeCSharp2()
        {
            var text = @"
class C : global::B
{
}
";
            SyntaxFactory.ParseSyntaxTree(text, options: CSharpParseOptions.Default.WithLanguageVersion(LanguageVersion.CSharp2)).GetDiagnostics().Verify();
            SyntaxFactory.ParseSyntaxTree(text, options: CSharpParseOptions.Default.WithLanguageVersion(LanguageVersion.CSharp1)).GetDiagnostics().Verify();

            CreateCompilation(text, parseOptions: TestOptions.Regular2).VerifyDiagnostics(
                // (2,19): error CS0400: The type or namespace name 'B' could not be found in the global namespace (are you missing an assembly reference?)
                // class C : global::B
                Diagnostic(ErrorCode.ERR_GlobalSingleTypeNameNotFound, "B").WithArguments("B").WithLocation(2, 19));
            CreateCompilation(text, parseOptions: TestOptions.Regular1).VerifyDiagnostics(
                // (2,11): error CS8022: Feature 'namespace alias qualifier' is not available in C# 1. Please use language version 2 or greater.
                // class C : global::B
                Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion1, "global").WithArguments("namespace alias qualifier", "2").WithLocation(2, 11),
                // (2,19): error CS0400: The type or namespace name 'B' could not be found in the global namespace (are you missing an assembly reference?)
                // class C : global::B
                Diagnostic(ErrorCode.ERR_GlobalSingleTypeNameNotFound, "B").WithArguments("B").WithLocation(2, 19));
        }

        [Fact]
        public void AliasQualifiedNameBeforeCSharp2()
        {
            var text = @"
class C : A::B
{
}
";
            SyntaxFactory.ParseSyntaxTree(text, options: CSharpParseOptions.Default.WithLanguageVersion(LanguageVersion.CSharp2)).GetDiagnostics().Verify();
            SyntaxFactory.ParseSyntaxTree(text, options: CSharpParseOptions.Default.WithLanguageVersion(LanguageVersion.CSharp1)).GetDiagnostics().Verify();

            CreateCompilation(text, parseOptions: TestOptions.Regular2).VerifyDiagnostics(
                // (2,11): error CS0432: Alias 'A' not found
                // class C : A::B
                Diagnostic(ErrorCode.ERR_AliasNotFound, "A").WithArguments("A").WithLocation(2, 11));
            CreateCompilation(text, parseOptions: TestOptions.Regular1).VerifyDiagnostics(
                // (2,11): error CS8022: Feature 'namespace alias qualifier' is not available in C# 1. Please use language version 2 or greater.
                // class C : A::B
                Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion1, "A").WithArguments("namespace alias qualifier", "2").WithLocation(2, 11),
                // (2,11): error CS0432: Alias 'A' not found
                // class C : A::B
                Diagnostic(ErrorCode.ERR_AliasNotFound, "A").WithArguments("A").WithLocation(2, 11));
        }

        [Fact]
        public void OptionalParameterBeforeCSharp4()
        {
            var text = @"
class C
{
    void M(int x = 1) { }
}
";
            CreateCompilation(text, parseOptions: TestOptions.Regular4).VerifyDiagnostics();
            CreateCompilation(text, parseOptions: TestOptions.Regular3).VerifyDiagnostics(
                // (4,18): error CS8024: Feature 'optional parameter' is not available in C# 3. Please use language version 4 or greater.
                //     void M(int x = 1) { }
                Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion3, "=").WithArguments("optional parameter", "4").WithLocation(4, 18));

            SyntaxFactory.ParseSyntaxTree(text, options: CSharpParseOptions.Default.WithLanguageVersion(LanguageVersion.CSharp4)).GetDiagnostics().Verify();
            SyntaxFactory.ParseSyntaxTree(text, options: CSharpParseOptions.Default.WithLanguageVersion(LanguageVersion.CSharp3)).GetDiagnostics().Verify();
        }

        [Fact]
        public void ObjectInitializerBeforeCSharp3()
        {
            var text = @"
class C
{
    void M() 
    {
        return new C { Goo = 1 }; 
    }
}
";
            CreateCompilation(text, parseOptions: TestOptions.Regular3).VerifyEmitDiagnostics(
                // (6,24): error CS0117: 'C' does not contain a definition for 'Goo'
                //         return new C { Goo = 1 }; 
                Diagnostic(ErrorCode.ERR_NoSuchMember, "Goo").WithArguments("C", "Goo").WithLocation(6, 24));
            CreateCompilation(text, parseOptions: TestOptions.Regular2).VerifyEmitDiagnostics(
                // (6,22): error CS8023: Feature 'object initializer' is not available in C# 2. Please use language version 3 or greater.
                //         return new C { Goo = 1 }; 
                Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion2, "{").WithArguments("object initializer", "3").WithLocation(6, 22),
                // (6,24): error CS0117: 'C' does not contain a definition for 'Goo'
                //         return new C { Goo = 1 }; 
                Diagnostic(ErrorCode.ERR_NoSuchMember, "Goo").WithArguments("C", "Goo").WithLocation(6, 24));

            SyntaxFactory.ParseSyntaxTree(text, options: CSharpParseOptions.Default.WithLanguageVersion(LanguageVersion.CSharp3)).GetDiagnostics().Verify();
            SyntaxFactory.ParseSyntaxTree(text, options: CSharpParseOptions.Default.WithLanguageVersion(LanguageVersion.CSharp2)).GetDiagnostics().Verify();
        }

        [Fact]
        public void CollectionInitializerBeforeCSharp3()
        {
            var text = @"
class C
{
    void M() 
    {
        return new C { 1, 2, 3 }; 
    }
}
";
            CreateCompilation(text, parseOptions: TestOptions.Regular3).VerifyEmitDiagnostics(
                // (6,22): error CS1922: Cannot initialize type 'C' with a collection initializer because it does not implement 'System.Collections.IEnumerable'
                //         return new C { 1, 2, 3 }; 
                Diagnostic(ErrorCode.ERR_CollectionInitRequiresIEnumerable, "{ 1, 2, 3 }").WithArguments("C").WithLocation(6, 22));
            CreateCompilation(text, parseOptions: TestOptions.Regular2).VerifyEmitDiagnostics(
                // (6,22): error CS8023: Feature 'collection initializer' is not available in C# 2. Please use language version 3 or greater.
                //         return new C { 1, 2, 3 }; 
                Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion2, "{").WithArguments("collection initializer", "3").WithLocation(6, 22),
                // (6,22): error CS1922: Cannot initialize type 'C' with a collection initializer because it does not implement 'System.Collections.IEnumerable'
                //         return new C { 1, 2, 3 }; 
                Diagnostic(ErrorCode.ERR_CollectionInitRequiresIEnumerable, "{ 1, 2, 3 }").WithArguments("C").WithLocation(6, 22));

            SyntaxFactory.ParseSyntaxTree(text, options: CSharpParseOptions.Default.WithLanguageVersion(LanguageVersion.CSharp3)).GetDiagnostics().Verify();
            SyntaxFactory.ParseSyntaxTree(text, options: CSharpParseOptions.Default.WithLanguageVersion(LanguageVersion.CSharp2)).GetDiagnostics().Verify();
        }

        [Fact]
        public void CrefGenericBeforeCSharp2()
        {
            var text = @"
/// <see cref='C{T}'/>
class C
{
}
";
            // NOTE: This actually causes an internal compiler error in dev12 (probably wasn't expecting an error from cref parsing).
            SyntaxFactory.ParseSyntaxTree(text, options: TestOptions.RegularWithDocumentationComments.WithLanguageVersion(LanguageVersion.CSharp2)).GetDiagnostics().Verify();
            SyntaxFactory.ParseSyntaxTree(text, options: TestOptions.RegularWithDocumentationComments.WithLanguageVersion(LanguageVersion.CSharp1)).GetDiagnostics().Verify(
                // (2,16): warning CS1584: XML comment has syntactically incorrect cref attribute 'C{T}'
                // /// <see cref='C{T}'/>
                Diagnostic(ErrorCode.WRN_BadXMLRefSyntax, "C{T}").WithArguments("C{T}"),
                // (2,17): warning CS1658: Feature 'generics' is not available in C# 1. Please use language version 2 or greater.. See also error CS8022.
                // /// <see cref='C{T}'/>
                Diagnostic(ErrorCode.WRN_ErrorOverride, "{").WithArguments("Feature 'generics' is not available in C# 1. Please use language version 2 or greater.", "8022"));
        }

        [Fact]
        public void CrefAliasQualifiedNameBeforeCSharp2()
        {
            var text = @"
/// <see cref='Alias::Goo'/>
/// <see cref='global::Goo'/>
class C { }
";
            // NOTE: This actually causes an internal compiler error in dev12 (probably wasn't expecting an error from cref parsing).
            SyntaxFactory.ParseSyntaxTree(text, options: TestOptions.RegularWithDocumentationComments.WithLanguageVersion(LanguageVersion.CSharp2)).GetDiagnostics().Verify();
            SyntaxFactory.ParseSyntaxTree(text, options: TestOptions.RegularWithDocumentationComments.WithLanguageVersion(LanguageVersion.CSharp1)).GetDiagnostics().Verify(
                // (2,16): warning CS1584: XML comment has syntactically incorrect cref attribute 'Alias::Goo'
                // /// <see cref='Alias::Goo'/>
                Diagnostic(ErrorCode.WRN_BadXMLRefSyntax, "Alias::Goo").WithArguments("Alias::Goo"),
                // (2,16): warning CS1658: Feature 'namespace alias qualifier' is not available in C# 1. Please use language version 2 or greater.. See also error CS8022.
                // /// <see cref='Alias::Goo'/>
                Diagnostic(ErrorCode.WRN_ErrorOverride, "Alias").WithArguments("Feature 'namespace alias qualifier' is not available in C# 1. Please use language version 2 or greater.", "8022"),
                // (3,16): warning CS1584: XML comment has syntactically incorrect cref attribute 'global::Goo'
                // /// <see cref='global::Goo'/>
                Diagnostic(ErrorCode.WRN_BadXMLRefSyntax, "global::Goo").WithArguments("global::Goo"),
                // (3,16): warning CS1658: Feature 'namespace alias qualifier' is not available in C# 1. Please use language version 2 or greater.. See also error CS8022.
                // /// <see cref='global::Goo'/>
                Diagnostic(ErrorCode.WRN_ErrorOverride, "global").WithArguments("Feature 'namespace alias qualifier' is not available in C# 1. Please use language version 2 or greater.", "8022"));
        }

        [Fact]
        public void PragmaBeforeCSharp2()
        {
            var text = @"
#pragma warning disable 1584
#pragma checksum ""file.txt"" ""{00000000-0000-0000-0000-000000000000}"" ""2453""
class C { }
";
            SyntaxFactory.ParseSyntaxTree(text, options: TestOptions.RegularWithDocumentationComments.WithLanguageVersion(LanguageVersion.CSharp2)).GetDiagnostics().Verify();
            SyntaxFactory.ParseSyntaxTree(text, options: TestOptions.RegularWithDocumentationComments.WithLanguageVersion(LanguageVersion.CSharp1)).GetDiagnostics().Verify(
                // (2,2): error CS8022: Feature '#pragma' is not available in C# 1. Please use language version 2 or greater.
                // #pragma warning disable 1584
                Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion1, "pragma").WithArguments("#pragma", "2"),
                // (3,2): error CS8022: Feature '#pragma' is not available in C# 1. Please use language version 2 or greater.
                // #pragma checksum "file.txt" "{00000000-0000-0000-0000-000000000000}" "2453"
                Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion1, "pragma").WithArguments("#pragma", "2"));
        }

        [Fact]
        public void PragmaBeforeCSharp2_InDisabledCode()
        {
            var text = @"
#if UNDEF
#pragma warning disable 1584
#pragma checksum ""file.txt"" ""{00000000-0000-0000-0000-000000000000}"" ""2453""
#endif
class C { }
";
            SyntaxFactory.ParseSyntaxTree(text, options: TestOptions.RegularWithDocumentationComments.WithLanguageVersion(LanguageVersion.CSharp2)).GetDiagnostics().Verify();
            SyntaxFactory.ParseSyntaxTree(text, options: TestOptions.RegularWithDocumentationComments.WithLanguageVersion(LanguageVersion.CSharp1)).GetDiagnostics().Verify();
        }

        [Fact]
        public void AwaitAsIdentifierInAsyncContext()
        {
            var text = @"
class C
{
    async void f()
    {
        int await;
    }
}
";

            var tree = SyntaxFactory.ParseSyntaxTree(text, options: CSharpParseOptions.Default.WithLanguageVersion(LanguageVersion.CSharp5));
            tree.GetCompilationUnitRoot().GetDiagnostics().Verify(
                // (6,13): error CS4003: 'await' cannot be used as an identifier within an async method or lambda expression
                //         int await;
                Diagnostic(ErrorCode.ERR_BadAwaitAsIdentifier, "await"));
        }

        // Note: Warnings covered in other test suite:
        //  1) PreprocessorTests: CS1633WRN_IllegalPragma, CS1634WRN_IllegalPPWarning, CS1691WRN_BadWarningNumber, CS1692WRN_InvalidNumber, 
        //                        CS1695WRN_IllegalPPChecksum, CS1696WRN_EndOfPPLineExpected 

        [Fact]
        public void WRN_NonECMAFeature()
        {
            var source = @"[module:Obsolete()]";
            SyntaxFactory.ParseSyntaxTree(source, options: CSharpParseOptions.Default.WithLanguageVersion(LanguageVersion.CSharp2)).GetDiagnostics().Verify();
            SyntaxFactory.ParseSyntaxTree(source, options: CSharpParseOptions.Default.WithLanguageVersion(LanguageVersion.CSharp1)).GetDiagnostics().Verify();

            CreateCompilation(source, parseOptions: TestOptions.Regular2).VerifyDiagnostics(
                // (1,9): error CS0246: The type or namespace name 'ObsoleteAttribute' could not be found (are you missing a using directive or an assembly reference?)
                // [module:Obsolete()]
                Diagnostic(ErrorCode.ERR_SingleTypeNameNotFound, "Obsolete").WithArguments("ObsoleteAttribute").WithLocation(1, 9),
                // (1,9): error CS0246: The type or namespace name 'Obsolete' could not be found (are you missing a using directive or an assembly reference?)
                // [module:Obsolete()]
                Diagnostic(ErrorCode.ERR_SingleTypeNameNotFound, "Obsolete").WithArguments("Obsolete").WithLocation(1, 9));
            CreateCompilation(source, parseOptions: TestOptions.Regular1).VerifyDiagnostics(
                // (1,2): warning CS1645: Feature 'IDS_FeatureModuleAttrLoc' is not part of the standardized ISO C# language specification, and may not be accepted by other compilers
                // [module:Obsolete()]
                Diagnostic(ErrorCode.WRN_NonECMAFeature, "module:").WithArguments("IDS_FeatureModuleAttrLoc").WithLocation(1, 2),
                // (1,9): error CS0246: The type or namespace name 'ObsoleteAttribute' could not be found (are you missing a using directive or an assembly reference?)
                // [module:Obsolete()]
                Diagnostic(ErrorCode.ERR_SingleTypeNameNotFound, "Obsolete").WithArguments("ObsoleteAttribute").WithLocation(1, 9),
                // (1,9): error CS0246: The type or namespace name 'Obsolete' could not be found (are you missing a using directive or an assembly reference?)
                // [module:Obsolete()]
                Diagnostic(ErrorCode.ERR_SingleTypeNameNotFound, "Obsolete").WithArguments("Obsolete").WithLocation(1, 9));
        }

        [Fact]
        public void CSharp6Features()
        {
            var source =
@"class Goo
{
    int L { get; } = 12; // auto property initializer

    int M() => 12; // expression-bodied method

    int N => 12; // expression-bodied property

    int this[int a] => a + 1; // expression-bodied indexer
    
    public static int operator +(Goo a, Goo b) => null; // expression-bodied operator
    
    public static explicit operator bool(Goo a) => false; // expression-bodied conversion operator

    void P(object o)
    {
        try {
        } catch (Exception ex) when (ex.ToString() == null) { // exception filter
        }

        var s = o?.ToString(); // null propagating operator
        var x = $""hello world"";
    }
}";
            CreateCompilation(source, parseOptions: TestOptions.Regular6).VerifyDiagnostics(
                // (11,51): error CS0037: Cannot convert null to 'int' because it is a non-nullable value type
                //     public static int operator +(Goo a, Goo b) => null; // expression-bodied operator
                Diagnostic(ErrorCode.ERR_ValueCantBeNull, "null").WithArguments("int").WithLocation(11, 51),
                // (18,18): error CS0246: The type or namespace name 'Exception' could not be found (are you missing a using directive or an assembly reference?)
                //         } catch (Exception ex) when (ex.ToString() == null) { // exception filter
                Diagnostic(ErrorCode.ERR_SingleTypeNameNotFound, "Exception").WithArguments("Exception").WithLocation(18, 18));
            CreateCompilation(source, parseOptions: TestOptions.Regular5).VerifyDiagnostics(
                // (3,9): error CS8026: Feature 'readonly automatically implemented properties' is not available in C# 5. Please use language version 6 or greater.
                //     int L { get; } = 12; // auto property initializer
                Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion5, "L").WithArguments("readonly automatically implemented properties", "6").WithLocation(3, 9),
                // (3,20): error CS8026: Feature 'auto property initializer' is not available in C# 5. Please use language version 6 or greater.
                //     int L { get; } = 12; // auto property initializer
                Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion5, "=").WithArguments("auto property initializer", "6").WithLocation(3, 20),
                // (5,13): error CS8026: Feature 'expression-bodied method' is not available in C# 5. Please use language version 6 or greater.
                //     int M() => 12; // expression-bodied method
                Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion5, "=>").WithArguments("expression-bodied method", "6").WithLocation(5, 13),
                // (7,11): error CS8026: Feature 'expression-bodied property' is not available in C# 5. Please use language version 6 or greater.
                //     int N => 12; // expression-bodied property
                Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion5, "=>").WithArguments("expression-bodied property", "6").WithLocation(7, 11),
                // (9,21): error CS8026: Feature 'expression-bodied indexer' is not available in C# 5. Please use language version 6 or greater.
                //     int this[int a] => a + 1; // expression-bodied indexer
                Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion5, "=>").WithArguments("expression-bodied indexer", "6").WithLocation(9, 21),
                // (11,48): error CS8026: Feature 'expression-bodied method' is not available in C# 5. Please use language version 6 or greater.
                //     public static int operator +(Goo a, Goo b) => null; // expression-bodied operator
                Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion5, "=>").WithArguments("expression-bodied method", "6").WithLocation(11, 48),
                // (11,51): error CS0037: Cannot convert null to 'int' because it is a non-nullable value type
                //     public static int operator +(Goo a, Goo b) => null; // expression-bodied operator
                Diagnostic(ErrorCode.ERR_ValueCantBeNull, "null").WithArguments("int").WithLocation(11, 51),
                // (13,49): error CS8026: Feature 'expression-bodied method' is not available in C# 5. Please use language version 6 or greater.
                //     public static explicit operator bool(Goo a) => false; // expression-bodied conversion operator
                Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion5, "=>").WithArguments("expression-bodied method", "6").WithLocation(13, 49),
                // (18,18): error CS0246: The type or namespace name 'Exception' could not be found (are you missing a using directive or an assembly reference?)
                //         } catch (Exception ex) when (ex.ToString() == null) { // exception filter
                Diagnostic(ErrorCode.ERR_SingleTypeNameNotFound, "Exception").WithArguments("Exception").WithLocation(18, 18),
                // (18,32): error CS8026: Feature 'exception filter' is not available in C# 5. Please use language version 6 or greater.
                //         } catch (Exception ex) when (ex.ToString() == null) { // exception filter
                Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion5, "when").WithArguments("exception filter", "6").WithLocation(18, 32),
                // (21,18): error CS8026: Feature 'null propagating operator' is not available in C# 5. Please use language version 6 or greater.
                //         var s = o?.ToString(); // null propagating operator
                Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion5, "?").WithArguments("null propagating operator", "6").WithLocation(21, 18),
                // (22,17): error CS8026: Feature 'interpolated strings' is not available in C# 5. Please use language version 6 or greater.
                //         var x = $"hello world";
                Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion5, @"$""hello world""").WithArguments("interpolated strings", "6").WithLocation(22, 17));

            SyntaxFactory.ParseSyntaxTree(source, options: CSharpParseOptions.Default.WithLanguageVersion(LanguageVersion.CSharp6)).GetDiagnostics().Verify();

            SyntaxFactory.ParseSyntaxTree(source, options: CSharpParseOptions.Default.WithLanguageVersion(LanguageVersion.CSharp5)).GetDiagnostics().Verify();
        }

        [ClrOnlyFact]
        public void TooDeepObjectInitializer()
        {
            var builder = new StringBuilder();
            const int depth = 5000;
            builder.Append(
@"
class C 
{ 
    public C c;
    public ulong u;
}

class Test
{
    void M()
    {
        C ");

            for (int i = 0; i < depth; i++)
            {
                builder.AppendLine("c = new C {");
            }

            builder.Append("c = new C(), u = 0");

            for (int i = 0; i < depth - 1; i++)
            {
                builder.AppendLine("}, u = 0");
            }

            builder.Append(
@"
        };
    }
}

");

            var parsedTree = Parse(builder.ToString());
            var actualErrors = parsedTree.GetDiagnostics().ToArray();
            Assert.Equal(1, actualErrors.Length);
            Assert.Equal((int)ErrorCode.ERR_InsufficientStack, actualErrors[0].Code);
        }

        [ClrOnlyFact]
        [WorkItem(1085618, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1085618")]
        public void TooDeepDelegateDeclaration()
        {
            var builder = new StringBuilder();
            builder.AppendLine(
@"
class Program
{
    static void Main(string[] args)
    {
");

            const int depth = 100000;
            for (int i = 0; i < depth; i++)
            {
                var line = string.Format("Action a{0} = delegate d{0} {{", i);
                builder.AppendLine(line);
            }

            for (int i = 0; i < depth; i++)
            {
                builder.Append("};");
            }

            builder.Append(@"} }");

            var parsedTree = Parse(builder.ToString());
            var actualErrors = parsedTree.GetDiagnostics().ToArray();
            Assert.Equal(1, actualErrors.Length);
            Assert.Equal((int)ErrorCode.ERR_InsufficientStack, actualErrors[0].Code);
        }

        [ClrOnlyFact]
        public void TooDeepObjectInitializerAsExpression()
        {
            var builder = new StringBuilder();
            const int depth = 5000;
            builder.Append(@"new C {");

            for (int i = 0; i < depth; i++)
            {
                builder.AppendLine("c = new C {");
            }

            builder.Append("c = new C(), u = 0");

            for (int i = 0; i < depth - 1; i++)
            {
                builder.AppendLine("}, u = 0");
            }

            builder.Append('}');

            var expr = SyntaxFactory.ParseExpression(builder.ToString());
            var actualErrors = expr.GetDiagnostics().ToArray();
            Assert.Equal(1, actualErrors.Length);
            Assert.Equal((int)ErrorCode.ERR_InsufficientStack, actualErrors[0].Code);
        }

        [ClrOnlyFact]
        public void TooDeepObjectInitializerAsStatement()
        {
            var builder = new StringBuilder();
            const int depth = 5000;
            builder.Append(@"C c = new C {");

            for (int i = 0; i < depth; i++)
            {
                builder.AppendLine("c = new C {");
            }

            builder.Append("c = new C(), u = 0");

            for (int i = 0; i < depth - 1; i++)
            {
                builder.AppendLine("}, u = 0");
            }

            builder.Append('}');

            var stmt = SyntaxFactory.ParseStatement(builder.ToString());
            var actualErrors = stmt.GetDiagnostics().ToArray();
            Assert.Equal(1, actualErrors.Length);
            Assert.Equal((int)ErrorCode.ERR_InsufficientStack, actualErrors[0].Code);
        }

        [Fact]
        [WorkItem(1085618, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1085618")]
        public void MismatchedBracesAndDelegateDeclaration()
        {
            var source = @"
class Program
{
    public static void Main(string[] args)
    {

    delegate int F1(); 
    delegate int F2();
}
";

            SyntaxFactory.ParseSyntaxTree(source).GetDiagnostics().Verify(
                // (7,14): error CS1514: { expected
                //     delegate int F1(); 
                Diagnostic(ErrorCode.ERR_LbraceExpected, "int").WithLocation(7, 14),
                // (7,14): error CS1002: ; expected
                //     delegate int F1(); 
                Diagnostic(ErrorCode.ERR_SemicolonExpected, "int").WithLocation(7, 14),
                // (8,14): error CS1514: { expected
                //     delegate int F2();
                Diagnostic(ErrorCode.ERR_LbraceExpected, "int").WithLocation(8, 14),
                // (8,14): error CS1002: ; expected
                //     delegate int F2();
                Diagnostic(ErrorCode.ERR_SemicolonExpected, "int").WithLocation(8, 14),
                // (9,2): error CS1513: } expected
                // }
                Diagnostic(ErrorCode.ERR_RbraceExpected, "").WithLocation(9, 2));
        }

        #endregion
    }
}
