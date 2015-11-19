// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Linq;
using System.Text;
using Microsoft.CodeAnalysis.CSharp.Test.Utilities;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.CSharp.UnitTests
{
    public class ParserErrorMessageTests : CSharpTestBase
    {
        #region "Targeted Error Tests - please arrange tests in the order of error code"

        [WorkItem(536666, "DevDiv")]
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

            ParseAndValidate(test, Diagnostic(ErrorCode.ERR_ExplicitEventFieldImpl, "."), Diagnostic(ErrorCode.ERR_MemberNeedsType, "E"));
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

            CreateCompilationWithMscorlib(test).VerifyDiagnostics(
                // (5,25): error CS0073: An add or remove accessor must have a body
                //     event Action E { add; remove; }
                Diagnostic(ErrorCode.ERR_AddRemoveMustHaveBody, ";"),
                // (5,33): error CS0073: An add or remove accessor must have a body
                //     event Action E { add; remove; }
                Diagnostic(ErrorCode.ERR_AddRemoveMustHaveBody, ";"),
                // (9,41): error CS0073: An add or remove accessor must have a body
                //     public abstract event Action E { add; remove; }
                Diagnostic(ErrorCode.ERR_AddRemoveMustHaveBody, ";"),
                // (9,49): error CS0073: An add or remove accessor must have a body
                //     public abstract event Action E { add; remove; }
                Diagnostic(ErrorCode.ERR_AddRemoveMustHaveBody, ";"));
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
            CreateCompilationWithMscorlib(test).VerifyDiagnostics(
                // (3,9): error CS0080: Constraints are not allowed on non-generic declarations
                Diagnostic(ErrorCode.ERR_ConstraintOnlyAllowedOnGenericDecl, "where").WithLocation(3, 9));
        }

        [WorkItem(527827, "DevDiv")]
        [Fact]
        public void CS0080ERR_ConstraintOnlyAllowedOnGenericDecl_2()
        {
            var test = @"
class C 
    where C : I
{
}
";
            CreateCompilationWithMscorlib(test).VerifyDiagnostics(
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
    public static int Main()
        {
        return 1;
        }
    }
";

            ParseAndValidate(test, Diagnostic(ErrorCode.ERR_BadMemberProtection, "internal"));
        }

        [Fact, WorkItem(543622, "DevDiv")]
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
    // (1,1): error CS1022: Type or namespace definition, or end-of-file expected
    // {
    Diagnostic(ErrorCode.ERR_EOFExpected, "{"),
    // (3,5): error CS1022: Type or namespace definition, or end-of-file expected
    //     {
    Diagnostic(ErrorCode.ERR_EOFExpected, "{"),
    // (3,6): error CS1520: Method must have a return type
    //     {
    Diagnostic(ErrorCode.ERR_MemberNeedsType, ""),
    // (2,5): error CS0116: A namespace does not directly contain members such as fields or methods
    //     get
    Diagnostic(ErrorCode.ERR_NamespaceUnexpected, "get"),
    // (5,5): error CS1022: Type or namespace definition, or end-of-file expected
    //     }
    Diagnostic(ErrorCode.ERR_EOFExpected, "}"),
    // (6,1): error CS1022: Type or namespace definition, or end-of-file expected
    // }
    Diagnostic(ErrorCode.ERR_EOFExpected, "}"));
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

        [WorkItem(536667, "DevDiv")]
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
    Diagnostic(ErrorCode.ERR_IdentifierExpectedKW, "namespace").WithArguments("", "namespace"),
    // (2,23): error CS1514: { expected
    // using namespace System;
    Diagnostic(ErrorCode.ERR_LbraceExpected, ";"),
    // (4,42): error CS0150: A constant value is expected
    //     public enum e1 {one=1, two=2, three= };
    Diagnostic(ErrorCode.ERR_ConstantExpected, ""));
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

            ParseAndValidate(test,
    // (6,32): error CS0178: Invalid rank specifier: expected ',' or ']'
    //         int[] arr = new int[5][5;
    Diagnostic(ErrorCode.ERR_InvalidArray, "5"),
    // (6,33): error CS1003: Syntax error, ',' expected
    //         int[] arr = new int[5][5;
    Diagnostic(ErrorCode.ERR_SyntaxError, ";").WithArguments(",", ";"),
    // (6,33): error CS0443: Syntax error; value expected
    //         int[] arr = new int[5][5;
    Diagnostic(ErrorCode.ERR_ValueExpected, ""),
    // (6,33): error CS1003: Syntax error, ']' expected
    //         int[] arr = new int[5][5;
    Diagnostic(ErrorCode.ERR_SyntaxError, ";").WithArguments("]", ";"));
        }

        [WorkItem(862031, "DevDiv/Personal")]
        [Fact]
        public void CS0201ERR_IllegalStatement()
        {
            var test = @"
class A
{
    public static int Main()
    {
        (a) => a;
        (a, b) =>
        {
        };
        int x = 0; int y = 0;
        x + y; x == 1;
    }
}
";

            ParseAndValidate(test);
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
    // (7,22): error CS1001: Identifier expected
    //         foreach (int in myarray)   // CS0230
    Diagnostic(ErrorCode.ERR_IdentifierExpected, "in"),
    // (7,22): error CS0230: Type and identifier are both required in a foreach statement
    //         foreach (int in myarray)   // CS0230
    Diagnostic(ErrorCode.ERR_BadForeachDecl, "in"));
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
    // (7,20): error CS1001: Identifier expected
    //         foreach (x in myarray) { }// Invalid
    Diagnostic(ErrorCode.ERR_IdentifierExpected, "in"),
    // (7,20): error CS0230: Type and identifier are both required in a foreach statement
    //         foreach (x in myarray) { }// Invalid
    Diagnostic(ErrorCode.ERR_BadForeachDecl, "in"));
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
    // (7,23): error CS1001: Identifier expected
    //         foreach (st[] in myarray) { }
    Diagnostic(ErrorCode.ERR_IdentifierExpected, "in"),
    // (7,23): error CS0230: Type and identifier are both required in a foreach statement
    //         foreach (st[] in myarray) { }
    Diagnostic(ErrorCode.ERR_BadForeachDecl, "in"));
        }

        [Fact]
        public void CS0231ERR_ParamsLast()
        {
            var test = @"
using System;
public class MyClass {
    public void MyMeth(params int[] values, int i) {}
    public static int Main() {
        return 1;
    }
}
";

            ParseAndValidate(test, Diagnostic(ErrorCode.ERR_ParamsLast, "params int[] values"));
        }

        [Fact]
        public void CS0257ERR_VarargsLast()
        {
            var test = @"
class Foo
{
  public void Bar(__arglist,  int b)
  {
  }
}
";

            ParseAndValidate(test,
    // (4,19): error CS0257: An __arglist parameter must be the last parameter in a formal parameter list
    //   public void Bar(__arglist,  int b)
    Diagnostic(ErrorCode.ERR_VarargsLast, "__arglist"));
        }

        [WorkItem(536668, "DevDiv")]
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

            ParseAndValidate(test, Diagnostic(ErrorCode.ERR_PartialMisplaced, "partial"));
        }

        [Fact]
        public void CS0267ERR_PartialMisplaced_Enum()
        {
            var test = @"
partial enum E { }
";

            ParseAndValidate(test, Diagnostic(ErrorCode.ERR_PartialMisplaced, "partial"));
        }

        [Fact]
        public void CS0267ERR_PartialMisplaced_Delegate()
        {
            var test = @"
partial delegate E { }
";

            // Extra errors
            ParseAndValidate(test,
    // (2,1): error CS0267: The 'partial' modifier can only appear immediately before 'class', 'struct', 'interface', or 'void'
    // partial delegate E { }
    Diagnostic(ErrorCode.ERR_PartialMisplaced, "partial"),
    // (2,20): error CS1001: Identifier expected
    // partial delegate E { }
    Diagnostic(ErrorCode.ERR_IdentifierExpected, "{"),
    // (2,20): error CS1003: Syntax error, '(' expected
    // partial delegate E { }
    Diagnostic(ErrorCode.ERR_SyntaxError, "{").WithArguments("(", "{"),
    // (2,20): error CS1026: ) expected
    // partial delegate E { }
    Diagnostic(ErrorCode.ERR_CloseParenExpected, "{"),
    // (2,20): error CS1002: ; expected
    // partial delegate E { }
    Diagnostic(ErrorCode.ERR_SemicolonExpected, "{"),
    // (2,20): error CS1022: Type or namespace definition, or end-of-file expected
    // partial delegate E { }
    Diagnostic(ErrorCode.ERR_EOFExpected, "{"),
    // (2,22): error CS1022: Type or namespace definition, or end-of-file expected
    // partial delegate E { }
    Diagnostic(ErrorCode.ERR_EOFExpected, "}"));
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
        int myarray[2]; 
        MyClass m[0];
        byte b[13,5];
        double d[14,5,6];
        E e[,50];
    }
}
";

            ParseAndValidate(test,
    // (7,20): error CS0650: Bad array declarator: To declare a managed array the rank specifier precedes the variable's identifier. To declare a fixed size buffer field, use the fixed keyword before the field type.
    //         int myarray[2]; 
    Diagnostic(ErrorCode.ERR_CStyleArray, "[2]"),
    // (7,21): error CS0270: Array size cannot be specified in a variable declaration (try initializing with a 'new' expression)
    //         int myarray[2]; 
    Diagnostic(ErrorCode.ERR_ArraySizeInDeclaration, "2"),
    // (8,18): error CS0650: Bad array declarator: To declare a managed array the rank specifier precedes the variable's identifier. To declare a fixed size buffer field, use the fixed keyword before the field type.
    //         MyClass m[0];
    Diagnostic(ErrorCode.ERR_CStyleArray, "[0]"),
    // (8,19): error CS0270: Array size cannot be specified in a variable declaration (try initializing with a 'new' expression)
    //         MyClass m[0];
    Diagnostic(ErrorCode.ERR_ArraySizeInDeclaration, "0"),
    // (9,15): error CS0650: Bad array declarator: To declare a managed array the rank specifier precedes the variable's identifier. To declare a fixed size buffer field, use the fixed keyword before the field type.
    //         byte b[13,5];
    Diagnostic(ErrorCode.ERR_CStyleArray, "[13,5]"),
    // (9,16): error CS0270: Array size cannot be specified in a variable declaration (try initializing with a 'new' expression)
    //         byte b[13,5];
    Diagnostic(ErrorCode.ERR_ArraySizeInDeclaration, "13"),
    // (9,19): error CS0270: Array size cannot be specified in a variable declaration (try initializing with a 'new' expression)
    //         byte b[13,5];
    Diagnostic(ErrorCode.ERR_ArraySizeInDeclaration, "5"),
    // (10,17): error CS0650: Bad array declarator: To declare a managed array the rank specifier precedes the variable's identifier. To declare a fixed size buffer field, use the fixed keyword before the field type.
    //         double d[14,5,6];
    Diagnostic(ErrorCode.ERR_CStyleArray, "[14,5,6]"),
    // (10,18): error CS0270: Array size cannot be specified in a variable declaration (try initializing with a 'new' expression)
    //         double d[14,5,6];
    Diagnostic(ErrorCode.ERR_ArraySizeInDeclaration, "14"),
    // (10,21): error CS0270: Array size cannot be specified in a variable declaration (try initializing with a 'new' expression)
    //         double d[14,5,6];
    Diagnostic(ErrorCode.ERR_ArraySizeInDeclaration, "5"),
    // (10,23): error CS0270: Array size cannot be specified in a variable declaration (try initializing with a 'new' expression)
    //         double d[14,5,6];
    Diagnostic(ErrorCode.ERR_ArraySizeInDeclaration, "6"),
    // (11,12): error CS0650: Bad array declarator: To declare a managed array the rank specifier precedes the variable's identifier. To declare a fixed size buffer field, use the fixed keyword before the field type.
    //         E e[,50];
    Diagnostic(ErrorCode.ERR_CStyleArray, "[,50]"),
    // (11,13): error CS0443: Syntax error; value expected
    //         E e[,50];
    Diagnostic(ErrorCode.ERR_ValueExpected, ""),
    // (11,14): error CS0270: Array size cannot be specified in a variable declaration (try initializing with a 'new' expression)
    //         E e[,50];
    Diagnostic(ErrorCode.ERR_ArraySizeInDeclaration, "50"));
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
            CreateCompilationWithMscorlib(test).VerifyDiagnostics(
                // (5,22): error CS0401: The new() constraint must be the last constraint specified
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
        public void CS0449ERR_RefValBoundMustBeFirst()
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
            CreateCompilationWithMscorlib(test).VerifyDiagnostics(
                // (5,41): error CS0449: The 'class' or 'struct' constraint must come before any other constraints
                Diagnostic(ErrorCode.ERR_RefValBoundMustBeFirst, "struct").WithLocation(5, 41),
                // (6,37): error CS0449: The 'class' or 'struct' constraint must come before any other constraints
                Diagnostic(ErrorCode.ERR_RefValBoundMustBeFirst, "struct").WithLocation(6, 37),
                // (7,37): error CS0449: The 'class' or 'struct' constraint must come before any other constraints
                Diagnostic(ErrorCode.ERR_RefValBoundMustBeFirst, "class").WithLocation(7, 37));
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
            CreateCompilationWithMscorlib(test).VerifyDiagnostics(
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
            CreateCompilationWithMscorlib(source).VerifyDiagnostics(
                // (14,20): error CS0460: Constraints for override and explicit interface implementation methods are inherited from the base method, so they cannot be specified directly
                Diagnostic(ErrorCode.ERR_OverrideWithConstraints, "where").WithLocation(14, 20),
                // (15,22): error CS0460: Constraints for override and explicit interface implementation methods are inherited from the base method, so they cannot be specified directly
                Diagnostic(ErrorCode.ERR_OverrideWithConstraints, "where").WithLocation(15, 22),
                // (16,37): error CS0460: Constraints for override and explicit interface implementation methods are inherited from the base method, so they cannot be specified directly
                Diagnostic(ErrorCode.ERR_OverrideWithConstraints, "where").WithLocation(16, 37),
                // (17,36): error CS0460: Constraints for override and explicit interface implementation methods are inherited from the base method, so they cannot be specified directly
                Diagnostic(ErrorCode.ERR_OverrideWithConstraints, "where").WithLocation(17, 36),
                // (18,45): error CS0460: Constraints for override and explicit interface implementation methods are inherited from the base method, so they cannot be specified directly
                Diagnostic(ErrorCode.ERR_OverrideWithConstraints, "where").WithLocation(18, 45),
                // (19,37): error CS0115: 'B.M4<T>()': no suitable method found to override
                Diagnostic(ErrorCode.ERR_OverrideNotExpected, "M4").WithArguments("B.M4<T>()").WithLocation(19, 37),
                // (19,45): error CS0460: Constraints for override and explicit interface implementation methods are inherited from the base method, so they cannot be specified directly
                Diagnostic(ErrorCode.ERR_OverrideWithConstraints, "where").WithLocation(19, 45));
        }

        [WorkItem(862094, "DevDiv/Personal")]
        [Fact]
        public void CS0514ERR_StaticConstructorWithExplicitConstructorCall()
        {
            var test = @"
namespace x
{
    public class clx 
    {
        public clx(int i){}
    }
    public class cly : clx
    {
// static does not have an object, therefore base cannot be called.
// objects must be known at compiler time
        static cly() : base(0){} // sc0514
    }
}
";

            ParseAndValidate(test, Diagnostic(ErrorCode.ERR_StaticConstructorWithExplicitConstructorCall, "base").WithArguments("cly"));
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

            ParseAndValidate(test, Diagnostic(ErrorCode.ERR_StaticConstructorWithExplicitConstructorCall, "this").WithArguments("C"));
        }

        [Fact]
        public void CS0574ERR_BadDestructorName()
        {
            var test = @"
namespace x
{
    public class iii
    {
        ~iiii(){}
        public static void Main()
        {
        }
    }
}
";

            ParseAndValidate(test, Diagnostic(ErrorCode.ERR_BadDestructorName, "iiii"));
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

            ParseAndValidate(test,
    // (6,20): error CS0650: Bad array declarator: To declare a managed array the rank specifier precedes the variable's identifier. To declare a fixed size buffer field, use the fixed keyword before the field type.
    //         int myarray[2]; 
    Diagnostic(ErrorCode.ERR_CStyleArray, "[2]"),
    // (6,21): error CS0270: Array size cannot be specified in a variable declaration (try initializing with a 'new' expression)
    //         int myarray[2]; 
    Diagnostic(ErrorCode.ERR_ArraySizeInDeclaration, "2"),
    // (7,18): error CS0650: Bad array declarator: To declare a managed array the rank specifier precedes the variable's identifier. To declare a fixed size buffer field, use the fixed keyword before the field type.
    //         MyClass m[0];
    Diagnostic(ErrorCode.ERR_CStyleArray, "[0]"),
    // (7,19): error CS0270: Array size cannot be specified in a variable declaration (try initializing with a 'new' expression)
    //         MyClass m[0];
    Diagnostic(ErrorCode.ERR_ArraySizeInDeclaration, "0"),
    // (8,15): error CS0650: Bad array declarator: To declare a managed array the rank specifier precedes the variable's identifier. To declare a fixed size buffer field, use the fixed keyword before the field type.
    //         byte b[13,5];
    Diagnostic(ErrorCode.ERR_CStyleArray, "[13,5]"),
    // (8,16): error CS0270: Array size cannot be specified in a variable declaration (try initializing with a 'new' expression)
    //         byte b[13,5];
    Diagnostic(ErrorCode.ERR_ArraySizeInDeclaration, "13"),
    // (8,19): error CS0270: Array size cannot be specified in a variable declaration (try initializing with a 'new' expression)
    //         byte b[13,5];
    Diagnostic(ErrorCode.ERR_ArraySizeInDeclaration, "5"),
    // (9,17): error CS0650: Bad array declarator: To declare a managed array the rank specifier precedes the variable's identifier. To declare a fixed size buffer field, use the fixed keyword before the field type.
    //         double d[14,5,6];
    Diagnostic(ErrorCode.ERR_CStyleArray, "[14,5,6]"),
    // (9,18): error CS0270: Array size cannot be specified in a variable declaration (try initializing with a 'new' expression)
    //         double d[14,5,6];
    Diagnostic(ErrorCode.ERR_ArraySizeInDeclaration, "14"),
    // (9,21): error CS0270: Array size cannot be specified in a variable declaration (try initializing with a 'new' expression)
    //         double d[14,5,6];
    Diagnostic(ErrorCode.ERR_ArraySizeInDeclaration, "5"),
    // (9,23): error CS0270: Array size cannot be specified in a variable declaration (try initializing with a 'new' expression)
    //         double d[14,5,6];
    Diagnostic(ErrorCode.ERR_ArraySizeInDeclaration, "6"));
        }

        [Fact, WorkItem(535883, "DevDiv")]
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
            CreateCompilationWithMscorlib(test).VerifyDiagnostics(
                Diagnostic(ErrorCode.ERR_GlobalSingleTypeNameNotFound, "MyType").WithArguments("MyType", "<global namespace>")
                );
        }

        [WorkItem(542478, "DevDiv")]
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
            CreateCompilationWithMscorlib(source).VerifyDiagnostics(
                // (2,15): error CS0706: Invalid constraint type. A type used as a constraint must be an interface, a non-sealed class or a type parameter.
                Diagnostic(ErrorCode.ERR_BadConstraintType, "T*").WithLocation(2, 15),
                // (3,15): error CS0706: Invalid constraint type. A type used as a constraint must be an interface, a non-sealed class or a type parameter.
                Diagnostic(ErrorCode.ERR_BadConstraintType, "T[]").WithLocation(3, 15),
                // (9,19): error CS0706: Invalid constraint type. A type used as a constraint must be an interface, a non-sealed class or a type parameter.
                Diagnostic(ErrorCode.ERR_BadConstraintType, "T*").WithLocation(9, 19),
                // (10,19): error CS0706: Invalid constraint type. A type used as a constraint must be an interface, a non-sealed class or a type parameter.
                Diagnostic(ErrorCode.ERR_BadConstraintType, "T[]").WithLocation(10, 19),

                // CONSIDER: Dev10 doesn't report these cascading errors.

                // (2,15): error CS0214: Pointers and fixed size buffers may only be used in an unsafe context
                Diagnostic(ErrorCode.ERR_UnsafeNeeded, "T*"),
                // (2,15): error CS0208: Cannot take the address of, get the size of, or declare a pointer to a managed type ('T')
                Diagnostic(ErrorCode.ERR_ManagedAddr, "T*").WithArguments("T"),
                // (9,19): error CS0214: Pointers and fixed size buffers may only be used in an unsafe context
                Diagnostic(ErrorCode.ERR_UnsafeNeeded, "T*"),
                // (9,19): error CS0208: Cannot take the address of, get the size of, or declare a pointer to a managed type ('T')
                Diagnostic(ErrorCode.ERR_ManagedAddr, "T*").WithArguments("T"));
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
using System;
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

            ParseAndValidate(test, Diagnostic(ErrorCode.ERR_InvalidAnonymousTypeMemberDeclarator, "a.b = 1"));
        }

        [Fact]
        public void CS0746ERR_InvalidAnonymousTypeMemberDeclarator_2()
        {
            var test = @"
using System;
public class C
{
    public static void Main()
    {
        string s = """";
        var t = new { s.Length = 1 };
    }
}
";
            ParseAndValidate(test,
    // (8,23): error CS0746: Invalid anonymous type member declarator. Anonymous type members must be declared with a member assignment, simple name or member access.
    //         var t = new { s.Length = 1 };
    Diagnostic(ErrorCode.ERR_InvalidAnonymousTypeMemberDeclarator, "s.Length = 1"));
        }

        [Fact]
        public void CS0746ERR_InvalidAnonymousTypeMemberDeclarator_3()
        {
            var test = @"
using System;
public class C
{
    public static void Main()
    {
        string s = """";
        var t = new { s.ToString() = 1 };
    }
}
";
            ParseAndValidate(test, Diagnostic(ErrorCode.ERR_InvalidAnonymousTypeMemberDeclarator, "s.ToString() = 1"));
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
    delegate T Func<A0, A1, A2, A3, T>(A0 a0, A1 a1, A2 a2, A3 a3);
    static void X()
    {
        Func<int,int> f1      = (int x, y) => 1;          // err: mixed parameters
        Func<int,int> f2      = (x, int y) => 1;          // err: mixed parameters
        Func<int,int> f3      = (int x, int y, z) => 1;   // err: mixed parameters
        Func<int,int> f4      = (int x, y, int z) => 1;   // err: mixed parameters
        Func<int,int> f5      = (x, int y, int z) => 1;   // err: mixed parameters
        Func<int,int> f6      = (x, y, int z) => 1;       // err: mixed parameters
    }
}
";

            ParseAndValidate(test,
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

        [WorkItem(535915, "DevDiv")]
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

        [Fact, WorkItem(542408, "DevDiv")]
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

        [Fact, WorkItem(542408, "DevDiv")]
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

        [Fact, WorkItem(542416, "DevDiv")]
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
    Diagnostic(ErrorCode.ERR_SyntaxError, "1").WithArguments(",", ""));
        }

        [Fact, WorkItem(542416, "DevDiv")]
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

        [Fact, WorkItem(542416, "DevDiv")]
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
    Diagnostic(ErrorCode.ERR_SyntaxError, ",").WithArguments(":", ","),
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

        [WorkItem(528008, "DevDiv")]
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

        [WorkItem(527944, "DevDiv")]
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
    Diagnostic(ErrorCode.ERR_RbraceExpected, ""),
    // (9,16): error CS1525: Invalid expression term 'case'
    //         label1:
    Diagnostic(ErrorCode.ERR_InvalidExprTerm, "").WithArguments("case"),
    // (9,16): error CS1002: ; expected
    //         label1:
    Diagnostic(ErrorCode.ERR_SemicolonExpected, ""),
    // (9,16): error CS1513: } expected
    //         label1:
    Diagnostic(ErrorCode.ERR_RbraceExpected, ""),
    // (10,18): error CS1002: ; expected
    //         case "t1":
    Diagnostic(ErrorCode.ERR_SemicolonExpected, ":"),
    // (10,18): error CS1513: } expected
    //         case "t1":
    Diagnostic(ErrorCode.ERR_RbraceExpected, ":"),
    // (14,1): error CS1022: Type or namespace definition, or end-of-file expected
    // }
    Diagnostic(ErrorCode.ERR_EOFExpected, "}"));
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
    Diagnostic(ErrorCode.ERR_SyntaxError, ")").WithArguments("]", ")"),
    // (8,15): error CS1002: ; expected
    //             a[);
    Diagnostic(ErrorCode.ERR_SemicolonExpected, ")"),
    // (8,15): error CS1513: } expected
    //             a[);
    Diagnostic(ErrorCode.ERR_RbraceExpected, ")"));
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
            firstDiag.Verify(Diagnostic(ErrorCode.ERR_SyntaxError, "for").WithArguments("foreach", "for"));
        }

        [Fact]
        public void CS1004ERR_DuplicateModifier()
        {
            var test = @"
namespace x {
    abstract public class clx 
    {
        int i;
        public public static int Main()    // CS1004, two public keywords
        {
            return 0;
        }
    }
}
";

            ParseAndValidate(test, Diagnostic(ErrorCode.ERR_DuplicateModifier, "public").WithArguments("public"));
        }

        [Fact]
        public void CS1007ERR_DuplicateAccessor()
        {
            var test = @"using System;

public class Container
{
    public int Prop1{ protected get{return 1;} set {} protected get { return 1;}  }
    public static int Prop2{ get{return 1;} internal set {} internal set{} }
    public int this[int i]{ protected get{return 1;} internal set {} protected get { return 1;} internal set {}  }
}
";

            ParseAndValidate(test,
    // (5,65): error CS1007: Property accessor already defined
    //     public int Prop1{ protected get{return 1;} set {} protected get { return 1;}  }
    Diagnostic(ErrorCode.ERR_DuplicateAccessor, "get"),
    // (6,70): error CS1007: Property accessor already defined
    //     public static int Prop2{ get{return 1;} internal set {} internal set{} }
    Diagnostic(ErrorCode.ERR_DuplicateAccessor, "set"),
    // (7,80): error CS1007: Property accessor already defined
    //     public int this[int i]{ protected get{return 1;} internal set {} protected get { return 1;} internal set {}  }
    Diagnostic(ErrorCode.ERR_DuplicateAccessor, "get"),
    // (7,106): error CS1007: Property accessor already defined
    //     public int this[int i]{ protected get{return 1;} internal set {} protected get { return 1;} internal set {}  }
    Diagnostic(ErrorCode.ERR_DuplicateAccessor, "set"));
        }

        [Fact]
        public void CS1008ERR_IntegralTypeExpected01()
        {
            CreateCompilationWithMscorlib(
@"namespace x
{
    abstract public class clx 
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
            CreateCompilationWithMscorlib(
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

        [Fact, WorkItem(667303)]
        public void CS1008ERR_IntegralTypeExpected03()
        {
            ParseAndValidate(@"enum E : byt { A, B }"); // no *parser* errors. This is a semantic error now.
        }

        [Fact, WorkItem(540117, "DevDiv")]
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

        [Fact, WorkItem(528100, "DevDiv")]
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

        [WorkItem(535921, "DevDiv")]
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

            ParseAndValidate(test,
Diagnostic(ErrorCode.ERR_ClassTypeExpected, "int"),
Diagnostic(ErrorCode.ERR_ClassTypeExpected, "byte"));
        }

        [WorkItem(863382, "DevDiv/Personal")]
        [Fact]
        public void CS1016ERR_NamedArgumentExpected()
        {
            var test = @"
namespace x
{
[foo(a=5, b)]
class foo
    {
    }
public class a
    {
    public static int Main()
        {
        return 1;
        }
    }
}
";

            ParseAndValidate(test, Diagnostic(ErrorCode.ERR_NamedArgumentExpected, "b"));
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
public class mine {
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

            ParseAndValidate(test,
                Diagnostic(ErrorCode.ERR_TooManyCatches, "catch"),
                Diagnostic(ErrorCode.ERR_TooManyCatches, "catch"),
                Diagnostic(ErrorCode.ERR_TooManyCatches, "catch"));
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

        [WorkItem(535924, "DevDiv")]
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

            CreateCompilationWithMscorlib(test).VerifyDiagnostics(
                // (1,2): error CS1022: Type or namespace definition, or end-of-file expected
                Diagnostic(ErrorCode.ERR_EOFExpected, ">").WithLocation(1, 2),
                // (1,21): error CS0116: A namespace does not directly contain members such as fields or methods
                Diagnostic(ErrorCode.ERR_NamespaceUnexpected, "dll").WithLocation(1, 21),
                // (1,24): error CS1022: Type or namespace definition, or end-of-file expected
                Diagnostic(ErrorCode.ERR_EOFExpected, "!").WithLocation(1, 24),
                // (1,27): error CS0116: A namespace does not directly contain members such as fields or methods
                Diagnostic(ErrorCode.ERR_NamespaceUnexpected, "Basic").WithLocation(1, 27));
        }

        [Fact]
        public void CS1023ERR_BadEmbeddedStmt()
        {
            var test = @"
struct S {
}
public class a {
    public static int Main() {
        for (int i=0; i < 3; i++) MyLabel: {}
        return 1;
    }
}
";

            ParseAndValidate(test, Diagnostic(ErrorCode.ERR_BadEmbeddedStmt, "MyLabel: {}"));
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

        [WorkItem(541954, "DevDiv")]
        [Fact]
        public void CS1029ERR_ErrorDirectiveppNonLatin()
        {
            var test = "public class Test\r\n{\r\n# error \u0444\u0430\u0439\u043B\r\n}";
            var parsedTree = ParseWithRoundTripCheck(test);
            var error = parsedTree.GetDiagnostics().Single();
            Assert.Equal((int)ErrorCode.ERR_ErrorDirective, error.Code);
            Assert.Equal("error CS1029: #error: '\u0444\u0430\u0439\u043B'", CSharpDiagnosticFormatter.Instance.Format(error.WithLocation(Location.None), EnsureEnglishUICulture.PreferredOrNull));
        }

        [Fact(), WorkItem(526991, "DevDiv")]
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
                Diagnostic(ErrorCode.ERR_SyntaxError, "operator").WithArguments("explicit", "operator")
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
            e = new ();     // CS1031, not a type
        }
    }
}
";
            // TODO: this appears to be a severe regression from Dev10, which neatly reported 3 errors.
            ParseAndValidate(text,
    // (7,21): error CS1031: Type expected
    //             e = new base;   // CS1031, not a type
    Diagnostic(ErrorCode.ERR_TypeExpected, "base"),
    // (7,21): error CS1526: A new expression requires (), [], or {} after type
    //             e = new base;   // CS1031, not a type
    Diagnostic(ErrorCode.ERR_BadNewExpr, "base"),
    // (7,21): error CS1002: ; expected
    //             e = new base;   // CS1031, not a type
    Diagnostic(ErrorCode.ERR_SemicolonExpected, "base"),
    // (8,21): error CS1031: Type expected
    //             e = new this;   // CS1031, not a type
    Diagnostic(ErrorCode.ERR_TypeExpected, "this"),
    // (8,21): error CS1526: A new expression requires (), [], or {} after type
    //             e = new this;   // CS1031, not a type
    Diagnostic(ErrorCode.ERR_BadNewExpr, "this"),
    // (8,21): error CS1002: ; expected
    //             e = new this;   // CS1031, not a type
    Diagnostic(ErrorCode.ERR_SemicolonExpected, "this"),
    // (9,21): error CS1031: Type expected
    //             e = new ();     // CS1031, not a type
    Diagnostic(ErrorCode.ERR_TypeExpected, "(")
             );
        }

        [WorkItem(541347, "DevDiv")]
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

            ParseAndValidate(test, Diagnostic(ErrorCode.ERR_IdentifierExpectedKW, "this").WithArguments("", "this"));
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
            ParseAndValidate(test,
                // (4,19): error CS1553: Declaration is not valid; use '+ operator <dest-type> (...' instead
                //     public static int explicit operator ()
                Diagnostic(ErrorCode.ERR_BadOperatorSyntax, "int").WithArguments("+"),
                // (4,23): error CS1003: Syntax error, 'operator' expected
                //     public static int explicit operator ()
                Diagnostic(ErrorCode.ERR_SyntaxError, "explicit").WithArguments("operator", "explicit"),
                // (4,23): error CS1037: Overloadable operator expected
                //     public static int explicit operator ()
                Diagnostic(ErrorCode.ERR_OvlOperatorExpected, "explicit"),
                // (4,32): error CS1003: Syntax error, '(' expected
                //     public static int explicit operator ()
                Diagnostic(ErrorCode.ERR_SyntaxError, "operator").WithArguments("(", "operator"),
                // (4,32): error CS1041: Identifier expected; 'operator' is a keyword
                //     public static int explicit operator ()
                Diagnostic(ErrorCode.ERR_IdentifierExpectedKW, "operator").WithArguments("", "operator"),
                // (8,30): error CS1037: Overloadable operator expected
                //     public static A operator ()
                Diagnostic(ErrorCode.ERR_OvlOperatorExpected, "("),
                // (8,31): error CS1003: Syntax error, '(' expected
                //     public static A operator ()
                Diagnostic(ErrorCode.ERR_SyntaxError, ")").WithArguments("(", ")"));
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

        [Fact, WorkItem(535926, "DevDiv")]
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
    Diagnostic(ErrorCode.ERR_SyntaxError, "long").WithArguments(",", "long"),
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

        [Fact, WorkItem(541347, "DevDiv")]
        public void CS1041ERR_IdentifierExpectedKW02()
        {
            var test =
@"class C
{
    C(this object o) { }
}";

            ParseAndValidate(test, Diagnostic(ErrorCode.ERR_IdentifierExpectedKW, "this").WithArguments("", "this"));
        }

        [Fact, WorkItem(541347, "DevDiv")]
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
            ParseAndValidate(test, Diagnostic(ErrorCode.ERR_IdentifierExpectedKW, "this").WithArguments("", "this"));
        }

        [Fact, WorkItem(541347, "DevDiv")]
        public void CS1041ERR_IdentifierExpectedKW04()
        {
            var test = @"delegate void D(this object o);";

            ParseAndValidate(test, Diagnostic(ErrorCode.ERR_IdentifierExpectedKW, "this").WithArguments("", "this"));
        }

        [Fact, WorkItem(541347, "DevDiv")]
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
            ParseAndValidate(test,
                // (6,25): error CS1026: ) expected
                Diagnostic(ErrorCode.ERR_CloseParenExpected, "this").WithLocation(6, 25),
                // (6,25): error CS1514: { expected
                Diagnostic(ErrorCode.ERR_LbraceExpected, "this").WithLocation(6, 25),
                // (6,25): error CS1002: ; expected
                Diagnostic(ErrorCode.ERR_SemicolonExpected, "this").WithLocation(6, 25),
                // (6,30): error CS1002: ; expected
                Diagnostic(ErrorCode.ERR_SemicolonExpected, "object").WithLocation(6, 30),
                // (6,38): error CS1002: ; expected
                Diagnostic(ErrorCode.ERR_SemicolonExpected, ")").WithLocation(6, 38),
                // (6,38): error CS1513: } expected
                Diagnostic(ErrorCode.ERR_RbraceExpected, ")").WithLocation(6, 38));
        }

        [Fact, WorkItem(541347, "DevDiv")]
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
Diagnostic(ErrorCode.ERR_CloseParenExpected, "object"),
Diagnostic(ErrorCode.ERR_SemicolonExpected, "object"),
Diagnostic(ErrorCode.ERR_SemicolonExpected, ")"),
Diagnostic(ErrorCode.ERR_RbraceExpected, ")"));
        }

        // TODO: extra error CS1014
        [Fact]
        public void CS1043ERR_SemiOrLBraceExpected()
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
    // (7,13): error CS1043: { or ; expected
    //         get return 1;
    Diagnostic(ErrorCode.ERR_SemiOrLBraceExpected, "return"),
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
    Diagnostic(ErrorCode.ERR_AddOrRemoveExpected, "value"),
    // (7,21): error CS0073: An add or remove accessor must have a body
    //         return value; 
    Diagnostic(ErrorCode.ERR_AddRemoveMustHaveBody, ";"));
        }

        [WorkItem(536956, "DevDiv")]
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

            ParseAndValidate(test, Diagnostic(ErrorCode.ERR_DefaultValueNotAllowed, "="));
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

            ParseAndValidate(test, Diagnostic(ErrorCode.ERR_DefaultValueNotAllowed, "="));
        }

        [WorkItem(540251, "DevDiv")]
        [Fact]
        public void CS7014ERR_AttributesNotAllowed()
        {
            var test = @"
using System;

class Program
{
    static void Main()
    {
        const string message = ""the parameter is obsolete"";
        Action<int> a = delegate (
            [ObsoleteAttribute(message)] [ObsoleteAttribute(message)] int x,
            [ObsoleteAttribute(message)] int y
        ) { };
    }
}
";

            ParseAndValidate(test,
    // (10,13): error CS7014: Attributes are not valid in this context.
    //             [ObsoleteAttribute(message)] [ObsoleteAttribute(message)] int x,
    Diagnostic(ErrorCode.ERR_AttributesNotAllowed, "[ObsoleteAttribute(message)]"),
    // (10,42): error CS7014: Attributes are not valid in this context.
    //             [ObsoleteAttribute(message)] [ObsoleteAttribute(message)] int x,
    Diagnostic(ErrorCode.ERR_AttributesNotAllowed, "[ObsoleteAttribute(message)]"),
    // (11,13): error CS7014: Attributes are not valid in this context.
    //             [ObsoleteAttribute(message)] int y
    Diagnostic(ErrorCode.ERR_AttributesNotAllowed, "[ObsoleteAttribute(message)]"));
        }

        [WorkItem(863401, "DevDiv/Personal")]
        [Fact]
        public void CS1101ERR_BadRefWithThis()
        {
            // No error
            var test = @"
using System;
public static class Extensions
{
    //No type parameters
    public static void Foo(ref this int i) {}
    //Single type parameter
    public static void Foo<T>(ref this T t) {}
    //Multiple type parameters
    public static void Foo<T,U,V>(ref this U u) {}
}
public static class GenExtensions<X>
{
    //No type parameters
    public static void Foo(ref this int i) {}
    public static void Foo(ref this X x) {}
    //Single type parameter
    public static void Foo<T>(ref this T t) {}
    public static void Foo<T>(ref this X x) {}
    //Multiple type parameters
    public static void Foo<T,U,V>(ref this U u) {}
    public static void Foo<T,U,V>(ref this X x) {}
}
";

            ParseAndValidate(test,
Diagnostic(ErrorCode.ERR_BadRefWithThis, "this"),
Diagnostic(ErrorCode.ERR_BadRefWithThis, "this"),
Diagnostic(ErrorCode.ERR_BadRefWithThis, "this"),
Diagnostic(ErrorCode.ERR_BadRefWithThis, "this"),
Diagnostic(ErrorCode.ERR_BadRefWithThis, "this"),
Diagnostic(ErrorCode.ERR_BadRefWithThis, "this"),
Diagnostic(ErrorCode.ERR_BadRefWithThis, "this"),
Diagnostic(ErrorCode.ERR_BadRefWithThis, "this"),
Diagnostic(ErrorCode.ERR_BadRefWithThis, "this"));
        }

        [WorkItem(906072, "DevDiv/Personal")]
        [Fact]
        public void CS1102ERR_BadOutWithThis()
        {
            // No error
            var test = @"
using System;
public static class Extensions
{
    //No type parameters
    public static void Foo(this out int i) {}
    //Single type parameter
    public static void Foo<T>(this out T t) {}
    //Multiple type parameters
    public static void Foo<T,U,V>(this out U u) {}
}
public static class GenExtensions<X>
{
    //No type parameters
    public static void Foo(this out int i) {}
    public static void Foo(this out X x) {}
    //Single type parameter
    public static void Foo<T>(this out T t) {}
    public static void Foo<T>(this out X x) {}
    //Multiple type parameters
    public static void Foo<T,U,V>(this out U u) {}
    public static void Foo<T,U,V>(this out X x) {}
}
";

            ParseAndValidate(test,
Diagnostic(ErrorCode.ERR_BadOutWithThis, "out"),
Diagnostic(ErrorCode.ERR_BadOutWithThis, "out"),
Diagnostic(ErrorCode.ERR_BadOutWithThis, "out"),
Diagnostic(ErrorCode.ERR_BadOutWithThis, "out"),
Diagnostic(ErrorCode.ERR_BadOutWithThis, "out"),
Diagnostic(ErrorCode.ERR_BadOutWithThis, "out"),
Diagnostic(ErrorCode.ERR_BadOutWithThis, "out"),
Diagnostic(ErrorCode.ERR_BadOutWithThis, "out"),
Diagnostic(ErrorCode.ERR_BadOutWithThis, "out"));
        }

        [WorkItem(863402, "DevDiv/Personal")]
        [Fact]
        public void CS1104ERR_BadParamModThis()
        {
            // NO error
            var test = @"
using System;
public static class Extensions
{
    //No type parameters
    public static void Foo(this params int[] iArr) {}
    //Single type parameter
    public static void Foo<T>(this params T[] tArr) {}
    //Multiple type parameters
    public static void Foo<T,U,V>(this params U[] uArr) {}
}
public static class GenExtensions<X>
{
    //No type parameters
    public static void Foo(this params int[] iArr) {}
    public static void Foo(this params X[] xArr) {}
    //Single type parameter
    public static void Foo<T>(this params T[] tArr) {}
    public static void Foo<T>(this params X[] xArr) {}
    //Multiple type parameters
    public static void Foo<T,U,V>(this params U[] uArr) {}
    public static void Foo<T,U,V>(this params X[] xArr) {}
}
";

            ParseAndValidate(test,
Diagnostic(ErrorCode.ERR_BadParamModThis, "params"),
Diagnostic(ErrorCode.ERR_BadParamModThis, "params"),
Diagnostic(ErrorCode.ERR_BadParamModThis, "params"),
Diagnostic(ErrorCode.ERR_BadParamModThis, "params"),
Diagnostic(ErrorCode.ERR_BadParamModThis, "params"),
Diagnostic(ErrorCode.ERR_BadParamModThis, "params"),
Diagnostic(ErrorCode.ERR_BadParamModThis, "params"),
Diagnostic(ErrorCode.ERR_BadParamModThis, "params"),
Diagnostic(ErrorCode.ERR_BadParamModThis, "params"));
        }

        [Fact, WorkItem(535930, "DevDiv")]
        public void CS1107ERR_DupParamMod()
        {
            // Diff errors
            var test = @"
using System;
public static class Extensions
{
    //Extension methods
    public static void Foo(this this t) {}
    public static void Foo(this int this) {}
    //Non-extension methods
    public static void Foo(this t) {}
    public static void Foo(int this) {}
}
";
            // Extra errors
            ParseAndValidate(test,
    // (6,33): error CS1107: A parameter can only have one 'this' modifier
    //     public static void Foo(this this t) {}
    Diagnostic(ErrorCode.ERR_DupParamMod, "this").WithArguments("this"),
    // (6,39): error CS1001: Identifier expected
    //     public static void Foo(this this t) {}
    Diagnostic(ErrorCode.ERR_IdentifierExpected, ")"),
    // (7,37): error CS1001: Identifier expected
    //     public static void Foo(this int this) {}
    Diagnostic(ErrorCode.ERR_IdentifierExpected, "this"),
    // (7,37): error CS1003: Syntax error, ',' expected
    //     public static void Foo(this int this) {}
    Diagnostic(ErrorCode.ERR_SyntaxError, "this").WithArguments(",", "this"),
    // (7,41): error CS1031: Type expected
    //     public static void Foo(this int this) {}
    Diagnostic(ErrorCode.ERR_TypeExpected, ")"),
    // (7,41): error CS1001: Identifier expected
    //     public static void Foo(this int this) {}
    Diagnostic(ErrorCode.ERR_IdentifierExpected, ")"),
    // (9,34): error CS1001: Identifier expected
    //     public static void Foo(this t) {}
    Diagnostic(ErrorCode.ERR_IdentifierExpected, ")"),
    // (10,32): error CS1001: Identifier expected
    //     public static void Foo(int this) {}
    Diagnostic(ErrorCode.ERR_IdentifierExpected, "this"),
    // (10,32): error CS1003: Syntax error, ',' expected
    //     public static void Foo(int this) {}
    Diagnostic(ErrorCode.ERR_SyntaxError, "this").WithArguments(",", "this"),
    // (10,36): error CS1031: Type expected
    //     public static void Foo(int this) {}
    Diagnostic(ErrorCode.ERR_TypeExpected, ")"),
    // (10,36): error CS1001: Identifier expected
    //     public static void Foo(int this) {}
    Diagnostic(ErrorCode.ERR_IdentifierExpected, ")"));
        }

        [WorkItem(863405, "DevDiv/Personal")]
        [Fact]
        public void CS1108ERR_MultiParamMod()
        {
            // No error
            var test = @"
using System;
public static class Extensions
{
    //No type parameters
    public static void Foo(ref out int i) {}
    //Single type parameter
    public static void Foo<T>(ref out T t) {}
    //Multiple type parameters
    public static void Foo<T,U,V>(ref out U u) {}
}
public static class GenExtensions<X>
{
    //No type parameters
    public static void Foo(ref out int i) {}
    public static void Foo(ref out X x) {}
    //Single type parameter
    public static void Foo<T>(ref out T t) {}
    public static void Foo<T>(ref out X x) {}
    //Multiple type parameters
    public static void Foo<T,U,V>(ref out U u) {}
    public static void Foo<T,U,V>(ref out X x) {}
}
";

            ParseAndValidate(test,
Diagnostic(ErrorCode.ERR_MultiParamMod, "out"),
Diagnostic(ErrorCode.ERR_MultiParamMod, "out"),
Diagnostic(ErrorCode.ERR_MultiParamMod, "out"),
Diagnostic(ErrorCode.ERR_MultiParamMod, "out"),
Diagnostic(ErrorCode.ERR_MultiParamMod, "out"),
Diagnostic(ErrorCode.ERR_MultiParamMod, "out"),
Diagnostic(ErrorCode.ERR_MultiParamMod, "out"),
Diagnostic(ErrorCode.ERR_MultiParamMod, "out"),
Diagnostic(ErrorCode.ERR_MultiParamMod, "out"));
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
   Diagnostic(ErrorCode.ERR_LbraceExpected, "."),
   // (1,15): error CS1513: } expected
   // public class S.D 
   Diagnostic(ErrorCode.ERR_RbraceExpected, "."),
   // (1,15): error CS1022: Type or namespace definition, or end-of-file expected
   // public class S.D 
   Diagnostic(ErrorCode.ERR_EOFExpected, "."),
   // (1,16): error CS0116: A namespace does not directly contain members such as fields or methods
   // public class S.D 
   Diagnostic(ErrorCode.ERR_NamespaceUnexpected, "D"),
   // (2,1): error CS1022: Type or namespace definition, or end-of-file expected
   // {
   Diagnostic(ErrorCode.ERR_EOFExpected, "{"),
   // (4,1): error CS1022: Type or namespace definition, or end-of-file expected
   // }
   Diagnostic(ErrorCode.ERR_EOFExpected, "}"));
        }

        [WorkItem(535932, "DevDiv")]
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
    // (6,18): error CS1031: Type expected
    //         foreach (1)
    Diagnostic(ErrorCode.ERR_TypeExpected, "1"),
    // (6,18): error CS1001: Identifier expected
    //         foreach (1)
    Diagnostic(ErrorCode.ERR_IdentifierExpected, "1"),
    // (6,18): error CS1515: 'in' expected
    //         foreach (1)
    Diagnostic(ErrorCode.ERR_InExpected, "1"));
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
    // (4,5): error CS1519: Invalid token 'goto' in class, struct, or interface member declaration
    //     goto Labl; // Invalid
    Diagnostic(ErrorCode.ERR_InvalidMemberDecl, "goto").WithArguments("goto"),
    // (4,14): error CS1519: Invalid token ';' in class, struct, or interface member declaration
    //     goto Labl; // Invalid
    Diagnostic(ErrorCode.ERR_InvalidMemberDecl, ";").WithArguments(";"),
    // (4,14): error CS1519: Invalid token ';' in class, struct, or interface member declaration
    //     goto Labl; // Invalid
    Diagnostic(ErrorCode.ERR_InvalidMemberDecl, ";").WithArguments(";"),
    // (6,9): error CS1519: Invalid token ':' in class, struct, or interface member declaration
    //     Lab1:
    Diagnostic(ErrorCode.ERR_InvalidMemberDecl, ":").WithArguments(":"),
    // (6,9): error CS1519: Invalid token ':' in class, struct, or interface member declaration
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

            ParseAndValidate(test, Diagnostic(ErrorCode.ERR_MemberNeedsType, "x"));
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

            ParseAndValidate(test, Diagnostic(ErrorCode.ERR_BadBaseType, "Test1[]"), Diagnostic(ErrorCode.ERR_BadBaseType, "Test1*"));
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
    // (12,13): error CS1524: Expected catch or finally
    //             }
    Diagnostic(ErrorCode.ERR_ExpectedEndTry, "}"),
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

        [WorkItem(540245, "DevDiv")]
        [Fact]
        public void CS1525RegressVoidInfiniteLoop()
        {
            var test = @"class C
{
    void M()
    {
        void.Foo();
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
    Diagnostic(ErrorCode.ERR_SyntaxError, ",").WithArguments(":", ","),
    // (7,27): error CS1525: Invalid expression term ','
    //         int s = true ? x++, y++ : y++; // Invalid
    Diagnostic(ErrorCode.ERR_InvalidExprTerm, ",").WithArguments(","),
    // (7,30): error CS1002: ; expected
    //         int s = true ? x++, y++ : y++; // Invalid
    Diagnostic(ErrorCode.ERR_SemicolonExpected, "++"),
    // (7,33): error CS1525: Invalid expression term ':'
    //         int s = true ? x++, y++ : y++; // Invalid
    Diagnostic(ErrorCode.ERR_InvalidExprTerm, ":").WithArguments(":"),
    // (7,33): error CS1002: ; expected
    //         int s = true ? x++, y++ : y++; // Invalid
    Diagnostic(ErrorCode.ERR_SemicolonExpected, ":"),
    // (7,33): error CS1513: } expected
    //         int s = true ? x++, y++ : y++; // Invalid
    Diagnostic(ErrorCode.ERR_RbraceExpected, ":"),
    // (8,29): error CS1002: ; expected
    //         s = true ? x++ : x++, y++; // Invalid
    Diagnostic(ErrorCode.ERR_SemicolonExpected, ","),
    // (8,29): error CS1513: } expected
    //         s = true ? x++ : x++, y++; // Invalid
    Diagnostic(ErrorCode.ERR_RbraceExpected, ","));
        }

        [WorkItem(542229, "DevDiv")]
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
    Diagnostic(ErrorCode.ERR_InvalidExprTerm, "return").WithArguments("return"),
    // (5,23): error CS1003: Syntax error, ':' expected
    //         int s = 1>2 ? return 0: return 1; 	// Invalid
    Diagnostic(ErrorCode.ERR_SyntaxError, "return").WithArguments(":", "return"),
    // (5,23): error CS1525: Invalid expression term 'return'
    //         int s = 1>2 ? return 0: return 1; 	// Invalid
    Diagnostic(ErrorCode.ERR_InvalidExprTerm, "return").WithArguments("return"),
    // (5,23): error CS1002: ; expected
    //         int s = 1>2 ? return 0: return 1; 	// Invalid
    Diagnostic(ErrorCode.ERR_SemicolonExpected, "return"),
    // (5,31): error CS1002: ; expected
    //         int s = 1>2 ? return 0: return 1; 	// Invalid
    Diagnostic(ErrorCode.ERR_SemicolonExpected, ":"),
    // (5,31): error CS1513: } expected
    //         int s = 1>2 ? return 0: return 1; 	// Invalid
    Diagnostic(ErrorCode.ERR_RbraceExpected, ":"));
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
    Diagnostic(ErrorCode.ERR_InvalidExprTerm, "goto").WithArguments("goto"),
    // (5,24): error CS1003: Syntax error, ':' expected
    //         int s = true ? goto lab1: goto lab2; // Invalid
    Diagnostic(ErrorCode.ERR_SyntaxError, "goto").WithArguments(":", "goto"),
    // (5,24): error CS1525: Invalid expression term 'goto'
    //         int s = true ? goto lab1: goto lab2; // Invalid
    Diagnostic(ErrorCode.ERR_InvalidExprTerm, "goto").WithArguments("goto"),
    // (5,24): error CS1002: ; expected
    //         int s = true ? goto lab1: goto lab2; // Invalid
    Diagnostic(ErrorCode.ERR_SemicolonExpected, "goto"),
    // (5,33): error CS1002: ; expected
    //         int s = true ? goto lab1: goto lab2; // Invalid
    Diagnostic(ErrorCode.ERR_SemicolonExpected, ":"),
    // (5,33): error CS1513: } expected
    //         int s = true ? goto lab1: goto lab2; // Invalid
    Diagnostic(ErrorCode.ERR_RbraceExpected, ":"));
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
    // (12,17): error CS1528: Expected ; or = (cannot specify constructor arguments in declaration)
    //         try {B b(3);
    Diagnostic(ErrorCode.ERR_BadVarDecl, "(3)"),
    // (12,17): error CS1003: Syntax error, '[' expected
    //         try {B b(3);
    Diagnostic(ErrorCode.ERR_SyntaxError, "(").WithArguments("[", "("),
    // (12,20): error CS1003: Syntax error, ']' expected
    //         try {B b(3);
    Diagnostic(ErrorCode.ERR_SyntaxError, ";").WithArguments("]", ";"));
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
    Diagnostic(ErrorCode.ERR_BadVarDecl, "()"),
    // (4,26): error CS1003: Syntax error, '[' expected
    //     event System.Action E();
    Diagnostic(ErrorCode.ERR_SyntaxError, "(").WithArguments("[", "("),
    // (4,27): error CS1525: Invalid expression term ')'
    //     event System.Action E();
    Diagnostic(ErrorCode.ERR_InvalidExprTerm, ")").WithArguments(")"),
    // (4,28): error CS1003: Syntax error, ']' expected
    //     event System.Action E();
    Diagnostic(ErrorCode.ERR_SyntaxError, ";").WithArguments("]", ";"));
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
    public void foo(void){}
}
";

            ParseAndValidate(test,
    // (4,21): error CS1536: Invalid parameter type 'void'
    //     public void foo(void){}
    Diagnostic(ErrorCode.ERR_NoVoidParameter, "void"),
    // (4,25): error CS1001: Identifier expected
    //     public void foo(void){}
    Diagnostic(ErrorCode.ERR_IdentifierExpected, ")"));
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

            ParseAndValidate(test, Diagnostic(ErrorCode.ERR_IndexerNeedsParam, "]"));
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

        [Fact, WorkItem(535933, "DevDiv")] // ?
        public void CS1553ERR_BadOperatorSyntax()
        {
            // Extra errors
            var test = @"
class foo {
    public static int implicit operator (foo f) { return 6; }    // Error
}
public class MainClass
    {
    public static int Main ()
        {
        return 1;
        }
    }
";

            ParseAndValidate(test,
    // (3,19): error CS1553: Declaration is not valid; use '+ operator <dest-type> (...' instead
    //     public static int implicit operator (foo f) { return 6; }    // Error
    Diagnostic(ErrorCode.ERR_BadOperatorSyntax, "int").WithArguments("+"),
    // (3,23): error CS1003: Syntax error, 'operator' expected
    //     public static int implicit operator (foo f) { return 6; }    // Error
    Diagnostic(ErrorCode.ERR_SyntaxError, "implicit").WithArguments("operator", "implicit"),
    // (3,23): error CS1019: Overloadable unary operator expected
    //     public static int implicit operator (foo f) { return 6; }    // Error
    Diagnostic(ErrorCode.ERR_OvlUnaryOperatorExpected, "implicit"),
    // (3,32): error CS1003: Syntax error, '(' expected
    //     public static int implicit operator (foo f) { return 6; }    // Error
    Diagnostic(ErrorCode.ERR_SyntaxError, "operator").WithArguments("(", "operator"),
    // (3,32): error CS1041: Identifier expected; 'operator' is a keyword
    //     public static int implicit operator (foo f) { return 6; }    // Error
    Diagnostic(ErrorCode.ERR_IdentifierExpectedKW, "operator").WithArguments("", "operator"));
        }

        [Fact(), WorkItem(526995, "DevDiv")]
        public void CS1554ERR_BadOperatorSyntax2()
        {
            // Diff errors: CS1003, 1031 etc. (8 errors)
            var test = @"
class foo {
    public static operator ++ foo (foo f) { return new foo(); }    // Error
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

        [Fact, WorkItem(536673, "DevDiv")]
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
            ParseAndValidate(test,
                // (6,29): error CS1575: A stackalloc expression requires [] after type
                //         int *p = stackalloc int (30); 
                Diagnostic(ErrorCode.ERR_BadStackAllocExpr, "int"),
                // (6,33): error CS1002: ; expected
                //         int *p = stackalloc int (30); 
                Diagnostic(ErrorCode.ERR_SemicolonExpected, "("),
                // (7,30): error CS1575: A stackalloc expression requires [] after type
                //         int *pp = stackalloc int 30; 
                Diagnostic(ErrorCode.ERR_BadStackAllocExpr, "int"),
                // (7,34): error CS1002: ; expected
                //         int *pp = stackalloc int 30; 
                Diagnostic(ErrorCode.ERR_SemicolonExpected, "30"));
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

        [WorkItem(541952, "DevDiv")]
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

        [WorkItem(536689, "DevDiv")]
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
    Diagnostic(ErrorCode.ERR_BadModifierLocation, "virtual").WithArguments("virtual"),
    // (7,32): error CS1520: Method must have a return type
    //     public static void virtual f() {}
    Diagnostic(ErrorCode.ERR_MemberNeedsType, "f"));
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
        byte[] b = new byte[];
        string[] s = new string[];
        return 1;
    }
}
";

            ParseAndValidate(test,
Diagnostic(ErrorCode.ERR_MissingArraySize, "[]"),
Diagnostic(ErrorCode.ERR_MissingArraySize, "[]"),
Diagnostic(ErrorCode.ERR_MissingArraySize, "[]"));
        }

        [Fact, WorkItem(535935, "DevDiv")]
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

            ParseAndValidate(test, Diagnostic(ErrorCode.ERR_NoModifiersOnAccessor, "private"), Diagnostic(ErrorCode.ERR_NoModifiersOnAccessor, "public"));
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

            ParseAndValidate(test, Diagnostic(ErrorCode.ERR_NoModifiersOnAccessor, "public"), Diagnostic(ErrorCode.ERR_NoModifiersOnAccessor, "private"));
        }

        [WorkItem(863423, "DevDiv/Personal")]
        [Fact]
        public void CS1611ERR_ParamsCantBeRefOut()
        {
            // No error
            var test = @"
public class Test
{
    public static void foo(params ref int[] a) 
    {
    }
    public static void boo(params out int[] a) 
    {
    }
    public static int Main()
    {
        int i = 10;
        foo(ref i);
        boo(out i);
        return 1;
    }
}
";

            ParseAndValidate(test, Diagnostic(ErrorCode.ERR_ParamsCantBeRefOut, "ref"), Diagnostic(ErrorCode.ERR_ParamsCantBeRefOut, "out"));
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

            ParseAndValidate(test, Diagnostic(ErrorCode.ERR_BadModifiersOnNamespace, "public"));
        }

        [Fact]
        public void CS1671ERR_BadModifiersOnNamespace02()
        {
            var test = @"[System.Obsolete]
namespace N { }
";

            ParseAndValidate(test, Diagnostic(ErrorCode.ERR_BadModifiersOnNamespace, "[System.Obsolete]"));
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

        [Fact(), WorkItem(527039, "DevDiv")]
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

        [WorkItem(536674, "DevDiv")]
        [Fact]
        public void CS1733ERR_ExpressionExpected()
        {
            // diff error msg - CS1525
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

            ParseAndValidate(test, Diagnostic(ErrorCode.ERR_ExpressionExpected, "}"));
        }

        [WorkItem(536674, "DevDiv")]
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
                Diagnostic(ErrorCode.ERR_SyntaxError, "").WithArguments(":", "").WithLocation(9, 36),
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

            ParseAndValidate(test,
                // (3,12): error CS1960: Invalid variance modifier. Only interface and delegate type parameters can be specified as variant.
                Diagnostic(ErrorCode.ERR_IllegalVarianceSyntax, "in").WithLocation(3, 12),
                // (4,12): error CS7002: Unexpected use of a generic name
                Diagnostic(ErrorCode.ERR_UnexpectedGenericName, "this").WithLocation(4, 12),
                // (4,17): error CS1960: Invalid variance modifier. Only interface and delegate type parameters can be specified as variant.
                Diagnostic(ErrorCode.ERR_IllegalVarianceSyntax, "out").WithLocation(4, 17),
                // (6,10): error CS1960: Invalid variance modifier. Only interface and delegate type parameters can be specified as variant.
                Diagnostic(ErrorCode.ERR_IllegalVarianceSyntax, "out").WithLocation(6, 10),
                // (8,12): error CS1960: Invalid variance modifier. Only interface and delegate type parameters can be specified as variant.
                Diagnostic(ErrorCode.ERR_IllegalVarianceSyntax, "out").WithLocation(8, 12),
                // (11,9): error CS1960: Invalid variance modifier. Only interface and delegate type parameters can be specified as variant.
                Diagnostic(ErrorCode.ERR_IllegalVarianceSyntax, "out").WithLocation(11, 9),
                // (13, 12): error CS1960: Invalid variance modifier. Only interface and delegate type parameters can be specified as variant.
                Diagnostic(ErrorCode.ERR_IllegalVarianceSyntax, "out").WithLocation(13, 12),
                // (15, 14): error CS1960: Invalid variance modifier. Only interface and delegate type parameters can be specified as variant.
                Diagnostic(ErrorCode.ERR_IllegalVarianceSyntax, "out").WithLocation(15, 14),
                // (17,13): error CS1960: Invalid variance modifier. Only interface and delegate type parameters can be specified as variant.
                Diagnostic(ErrorCode.ERR_IllegalVarianceSyntax, "out").WithLocation(17, 13));
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
            ParseAndValidate(test,
    // (6,15): error CS7000: Unexpected use of an aliased name
    //     namespace N1Alias::N2 {}
    Diagnostic(ErrorCode.ERR_UnexpectedAliasedName, "N1Alias::N2"),
    // (12,22): error CS7000: Unexpected use of an aliased name
    //             N1.global::Test.M1();
    Diagnostic(ErrorCode.ERR_UnexpectedAliasedName, "::"));
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

        [Fact, WorkItem(546212, "DevDiv")]
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
              Diagnostic(ErrorCode.ERR_SyntaxError, "in").WithArguments(",", "in"),
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
            tree.GetCompilationUnitRoot().GetDiagnostics().Verify(
                // (2,1): error CS8022: Feature 'partial types' is not available in C# 1.  Please use language version 2 or greater.
                // partial class C
                Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion1, "partial").WithArguments("partial types", "2"));
        }

        [Fact]
        public void PartialMethodsVersionThree()
        {
            var text = @"
class C
{
    partial int Foo() { }
}
";
            var tree = SyntaxFactory.ParseSyntaxTree(text, options: CSharpParseOptions.Default.WithLanguageVersion(LanguageVersion.CSharp5));
            tree.GetCompilationUnitRoot().GetDiagnostics().Verify();

            tree = SyntaxFactory.ParseSyntaxTree(text, options: CSharpParseOptions.Default.WithLanguageVersion(LanguageVersion.CSharp2));
            tree.GetCompilationUnitRoot().GetDiagnostics().Verify(
                // (4,5): error CS8023: Feature 'partial method' is not available in C# 2.  Please use language version 3 or greater.
                //     partial int Foo() { }
                Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion2, "partial").WithArguments("partial method", "3"));
        }

        [Fact]
        public void QueryBeforeVersionThree()
        {
            var text = @"
class C
{
    void Foo()
    {
        var q = from a in b
                select c;
    }
}
";
            var tree = SyntaxFactory.ParseSyntaxTree(text, options: CSharpParseOptions.Default.WithLanguageVersion(LanguageVersion.CSharp5));
            tree.GetCompilationUnitRoot().GetDiagnostics().Verify();

            tree = SyntaxFactory.ParseSyntaxTree(text, options: CSharpParseOptions.Default.WithLanguageVersion(LanguageVersion.CSharp2));
            tree.GetCompilationUnitRoot().GetDiagnostics().Verify(
                // (6,17): error CS8023: Feature 'query expression' is not available in C# 2.  Please use language version 3 or greater.
                //         var q = from a in b
                Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion2, "from a in b").WithArguments("query expression", "3"),
                // (6,17): error CS8023: Feature 'query expression' is not available in C# 2.  Please use language version 3 or greater.
                //         var q = from a in b
                Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion2, "from").WithArguments("query expression", "3"));
        }

        [Fact]
        public void AnonymousTypeBeforeVersionThree()
        {
            var text = @"
class C
{
    void Foo()
    {
        var q = new { };
    }
}
";
            var tree = SyntaxFactory.ParseSyntaxTree(text, options: CSharpParseOptions.Default.WithLanguageVersion(LanguageVersion.CSharp5));
            tree.GetCompilationUnitRoot().GetDiagnostics().Verify();

            tree = SyntaxFactory.ParseSyntaxTree(text, options: CSharpParseOptions.Default.WithLanguageVersion(LanguageVersion.CSharp2));
            tree.GetCompilationUnitRoot().GetDiagnostics().Verify(
                // (6,17): error CS8023: Feature 'anonymous types' is not available in C# 2.  Please use language version 3 or greater.
                //         var q = new { };
                Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion2, "new").WithArguments("anonymous types", "3"));
        }

        [Fact]
        public void ImplicitArrayBeforeVersionThree()
        {
            var text = @"
class C
{
    void Foo()
    {
        var q = new [] { };
    }
}
";
            var tree = SyntaxFactory.ParseSyntaxTree(text, options: CSharpParseOptions.Default.WithLanguageVersion(LanguageVersion.CSharp5));
            tree.GetCompilationUnitRoot().GetDiagnostics().Verify();

            tree = SyntaxFactory.ParseSyntaxTree(text, options: CSharpParseOptions.Default.WithLanguageVersion(LanguageVersion.CSharp2));
            tree.GetCompilationUnitRoot().GetDiagnostics().Verify(
                // (6,17): error CS8023: Feature 'implicitly typed array' is not available in C# 2.  Please use language version 3 or greater.
                //         var q = new [] { };
                Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion2, "new").WithArguments("implicitly typed array", "3"));
        }

        [Fact]
        public void ObjectInitializerBeforeVersionThree()
        {
            var text = @"
class C
{
    void Foo()
    {
        var q = new Foo { };
    }
}
";
            var tree = SyntaxFactory.ParseSyntaxTree(text, options: CSharpParseOptions.Default.WithLanguageVersion(LanguageVersion.CSharp5));
            tree.GetCompilationUnitRoot().GetDiagnostics().Verify();

            tree = SyntaxFactory.ParseSyntaxTree(text, options: CSharpParseOptions.Default.WithLanguageVersion(LanguageVersion.CSharp2));
            tree.GetCompilationUnitRoot().GetDiagnostics().Verify(
                // (6,25): error CS8023: Feature 'object initializer' is not available in C# 2.  Please use language version 3 or greater.
                //         var q = new Foo { };
                Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion2, "{").WithArguments("object initializer", "3"));
        }

        [Fact]
        public void LambdaBeforeVersionThree()
        {
            var text = @"
class C
{
    void Foo()
    {
        var q = a => b;
    }
}
";
            var tree = SyntaxFactory.ParseSyntaxTree(text, options: CSharpParseOptions.Default.WithLanguageVersion(LanguageVersion.CSharp5));
            tree.GetCompilationUnitRoot().GetDiagnostics().Verify();

            tree = SyntaxFactory.ParseSyntaxTree(text, options: CSharpParseOptions.Default.WithLanguageVersion(LanguageVersion.CSharp2));
            tree.GetCompilationUnitRoot().GetDiagnostics().Verify(
                // (6,19): error CS8023: Feature 'lambda expression' is not available in C# 2.  Please use language version 3 or greater.
                //         var q = a => b;
                Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion2, "=>").WithArguments("lambda expression", "3"));
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
            var tree = SyntaxFactory.ParseSyntaxTree(text, options: CSharpParseOptions.Default.WithLanguageVersion(LanguageVersion.CSharp6));
            tree.GetDiagnostics().Verify();

            tree = Parse(text, options: CSharpParseOptions.Default.WithLanguageVersion(LanguageVersion.CSharp5));
            tree.GetDiagnostics().Verify(
    // (6,23): error CS8026: Feature 'exception filter' is not available in C# 5.  Please use language version 6 or greater.
    //         try { } catch when (true) {}
    Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion5, "when").WithArguments("exception filter", "6").WithLocation(6, 23)
                );
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

            ParseAndValidate(test, Diagnostic(ErrorCode.WRN_GlobalAliasDefn, "global"));
        }

        [Fact]
        public void CS0642WRN_PossibleMistakenNullStatement()
        {
            var test = @"
class MyClass
{
    public static int Main()
    {
        for (int i = 0; i < 10; i += 1);   // CS0642, semicolon intentional?
        if(true);
        while(false);
        return 0;
    }
}
";

            ParseAndValidate(test, Diagnostic(ErrorCode.WRN_PossibleMistakenNullStatement, ";"));
        }

        [Fact, WorkItem(529895, "DevDiv")]
        public void AttributeInMethodBody()
        {
            var test = @"
public class Class1 
{
    int Meth2 (int parm) {[Foo(5)]return 0;}
}
";
            ParseAndValidate(test,
                // (4,27): error CS1513: } expected
                Diagnostic(ErrorCode.ERR_RbraceExpected, "[").WithLocation(4, 27),
                // (4,35): error CS1519: Invalid token 'return' in class, struct, or interface member declaration
                Diagnostic(ErrorCode.ERR_InvalidMemberDecl, "return").WithArguments("return").WithLocation(4, 35),
                // (4,35): error CS1519: Invalid token 'return' in class, struct, or interface member declaration
                Diagnostic(ErrorCode.ERR_InvalidMemberDecl, "return").WithArguments("return").WithLocation(4, 35),
                // (5,1): error CS1022: Type or namespace definition, or end-of-file expected
                Diagnostic(ErrorCode.ERR_EOFExpected, "}").WithLocation(5, 1));
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

            ParseAndValidate(test, Diagnostic(ErrorCode.WRN_EmptySwitch, "{"));
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
            CreateCompilationWithMscorlib(test, parseOptions: CSharpParseOptions.Default.WithLanguageVersion(LanguageVersion.CSharp2)).VerifyDiagnostics(
                // (4,5): error CS8023: Feature 'partial method' is not available in C# 2.  Please use language version 3 or greater.
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

            var tree = SyntaxFactory.ParseSyntaxTree(text, options: CSharpParseOptions.Default.WithLanguageVersion(LanguageVersion.CSharp5));
            tree.GetCompilationUnitRoot().GetDiagnostics().Verify(
                // (6,16): error CS8026: Feature 'interpolated strings' is not available in C# 5.  Please use language version 6 or greater.
                //         return $"hello";
                Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion5, @"$""hello""").WithArguments("interpolated strings", "6").WithLocation(6, 16));
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

            var tree = SyntaxFactory.ParseSyntaxTree(text, options: CSharpParseOptions.Default.WithLanguageVersion(LanguageVersion.CSharp5));
            tree.GetCompilationUnitRoot().GetDiagnostics().Verify(
            // (7,16): error CS8026: Feature 'interpolated strings' is not available in C# 5.  Please use language version 6 or greater.
            //         return $"hello + {other}";
            Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion5, @"$""hello + {other}""").WithArguments("interpolated strings", "6").WithLocation(7, 16));
        }

        [WorkItem(529870, "DevDiv")]
        [Fact]
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

            tree = SyntaxFactory.ParseSyntaxTree(text, options: CSharpParseOptions.Default.WithLanguageVersion(LanguageVersion.CSharp3));
            tree.GetCompilationUnitRoot().GetDiagnostics().Verify(
                // (4,5): error CS8024: Feature 'async function' is not available in C# 3.  Please use language version 5 or greater.
                //     async void M() { }
                Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion3, "async").WithArguments("async function", "5"));
        }

        [WorkItem(529870, "DevDiv")]
        [Fact]
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

            tree = SyntaxFactory.ParseSyntaxTree(text, options: CSharpParseOptions.Default.WithLanguageVersion(LanguageVersion.CSharp3));
            tree.GetCompilationUnitRoot().GetDiagnostics().Verify(
                // (4,5): error CS8024: Feature 'async function' is not available in C# 3.  Please use language version 5 or greater.
                //     async static void M() { }
                Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion3, "async").WithArguments("async function", "5"));
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

            tree = SyntaxFactory.ParseSyntaxTree(text, options: CSharpParseOptions.Default.WithLanguageVersion(LanguageVersion.CSharp4));
            tree.GetCompilationUnitRoot().GetDiagnostics().Verify(
                // (6,34): error CS8025: Feature 'async function' is not available in C# 4.  Please use language version 5 or greater.
                //         Func<int, Task<int>> f = async x => x;
                Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion4, "async").WithArguments("async function", "5"));
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

            tree = SyntaxFactory.ParseSyntaxTree(text, options: CSharpParseOptions.Default.WithLanguageVersion(LanguageVersion.CSharp4));
            tree.GetCompilationUnitRoot().GetDiagnostics().Verify(
                // (6,34): error CS8025: Feature 'async function' is not available in C# 4.  Please use language version 5 or greater.
                //         Func<int, Task<int>> f = async delegate (int x) { return x; };
                Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion4, "async").WithArguments("async function", "5"));
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
            SyntaxFactory.ParseSyntaxTree(text, options: CSharpParseOptions.Default.WithLanguageVersion(LanguageVersion.CSharp4)).GetDiagnostics().Verify();
            SyntaxFactory.ParseSyntaxTree(text, options: CSharpParseOptions.Default.WithLanguageVersion(LanguageVersion.CSharp3)).GetDiagnostics().Verify(
                // (2,7): error CS8024: Feature 'named argument' is not available in C# 3.  Please use language version 4 or greater.
                // [Attr(x:1)]
                Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion3, "x:").WithArguments("named argument", "4"),
                // (7,11): error CS8024: Feature 'named argument' is not available in C# 3.  Please use language version 4 or greater.
                //         M(y:2);
                Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion3, "y:").WithArguments("named argument", "4"));
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
            SyntaxFactory.ParseSyntaxTree(text, options: CSharpParseOptions.Default.WithLanguageVersion(LanguageVersion.CSharp1)).GetDiagnostics().Verify(
                // (2,11): error CS8022: Feature 'namespace alias qualifier' is not available in C# 1.  Please use language version 2 or greater.
                // class C : global::B
                Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion1, "global").WithArguments("namespace alias qualifier", "2"));
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
            SyntaxFactory.ParseSyntaxTree(text, options: CSharpParseOptions.Default.WithLanguageVersion(LanguageVersion.CSharp1)).GetDiagnostics().Verify(
                // (2,11): error CS8022: Feature 'namespace alias qualifier' is not available in C# 1.  Please use language version 2 or greater.
                // class C : A::B
                Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion1, "A").WithArguments("namespace alias qualifier", "2"));
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
            SyntaxFactory.ParseSyntaxTree(text, options: CSharpParseOptions.Default.WithLanguageVersion(LanguageVersion.CSharp4)).GetDiagnostics().Verify();
            SyntaxFactory.ParseSyntaxTree(text, options: CSharpParseOptions.Default.WithLanguageVersion(LanguageVersion.CSharp3)).GetDiagnostics().Verify(
                // (4,18): error CS8024: Feature 'optional parameter' is not available in C# 3.  Please use language version 4 or greater.
                //     void M(int x = 1) { }
                Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion3, "= 1").WithArguments("optional parameter", "4"));
        }

        [Fact]
        public void ObjectInitializerBeforeCSharp3()
        {
            var text = @"
class C
{
    void M() 
    {
        return new C { Foo = 1 }; 
    }
}
";
            SyntaxFactory.ParseSyntaxTree(text, options: CSharpParseOptions.Default.WithLanguageVersion(LanguageVersion.CSharp3)).GetDiagnostics().Verify();
            SyntaxFactory.ParseSyntaxTree(text, options: CSharpParseOptions.Default.WithLanguageVersion(LanguageVersion.CSharp2)).GetDiagnostics().Verify(
                // (6,22): error CS8023: Feature 'object initializer' is not available in C# 2.  Please use language version 3 or greater.
                //         return new C { Foo = 1 }; 
                Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion2, "{").WithArguments("object initializer", "3"));
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
            SyntaxFactory.ParseSyntaxTree(text, options: CSharpParseOptions.Default.WithLanguageVersion(LanguageVersion.CSharp3)).GetDiagnostics().Verify();
            SyntaxFactory.ParseSyntaxTree(text, options: CSharpParseOptions.Default.WithLanguageVersion(LanguageVersion.CSharp2)).GetDiagnostics().Verify(
                // (6,22): error CS8023: Feature 'collection initializer' is not available in C# 2.  Please use language version 3 or greater.
                //         return new C { 1, 2, 3 }; 
                Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion2, "{").WithArguments("collection initializer", "3"));
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
                // (2,17): warning CS1658: Feature 'generics' is not available in C# 1.  Please use language version 2 or greater.. See also error CS8022.
                // /// <see cref='C{T}'/>
                Diagnostic(ErrorCode.WRN_ErrorOverride, "{").WithArguments("Feature 'generics' is not available in C# 1.  Please use language version 2 or greater.", "8022"));
        }

        [Fact]
        public void CrefAliasQualifiedNameBeforeCSharp2()
        {
            var text = @"
/// <see cref='Alias::Foo'/>
/// <see cref='global::Foo'/>
class C { }
";
            // NOTE: This actually causes an internal compiler error in dev12 (probably wasn't expecting an error from cref parsing).
            SyntaxFactory.ParseSyntaxTree(text, options: TestOptions.RegularWithDocumentationComments.WithLanguageVersion(LanguageVersion.CSharp2)).GetDiagnostics().Verify();
            SyntaxFactory.ParseSyntaxTree(text, options: TestOptions.RegularWithDocumentationComments.WithLanguageVersion(LanguageVersion.CSharp1)).GetDiagnostics().Verify(
                // (2,16): warning CS1584: XML comment has syntactically incorrect cref attribute 'Alias::Foo'
                // /// <see cref='Alias::Foo'/>
                Diagnostic(ErrorCode.WRN_BadXMLRefSyntax, "Alias::Foo").WithArguments("Alias::Foo"),
                // (2,16): warning CS1658: Feature 'namespace alias qualifier' is not available in C# 1.  Please use language version 2 or greater.. See also error CS8022.
                // /// <see cref='Alias::Foo'/>
                Diagnostic(ErrorCode.WRN_ErrorOverride, "Alias").WithArguments("Feature 'namespace alias qualifier' is not available in C# 1.  Please use language version 2 or greater.", "8022"),
                // (3,16): warning CS1584: XML comment has syntactically incorrect cref attribute 'global::Foo'
                // /// <see cref='global::Foo'/>
                Diagnostic(ErrorCode.WRN_BadXMLRefSyntax, "global::Foo").WithArguments("global::Foo"),
                // (3,16): warning CS1658: Feature 'namespace alias qualifier' is not available in C# 1.  Please use language version 2 or greater.. See also error CS8022.
                // /// <see cref='global::Foo'/>
                Diagnostic(ErrorCode.WRN_ErrorOverride, "global").WithArguments("Feature 'namespace alias qualifier' is not available in C# 1.  Please use language version 2 or greater.", "8022"));
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
                // (2,2): error CS8022: Feature '#pragma' is not available in C# 1.  Please use language version 2 or greater.
                // #pragma warning disable 1584
                Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion1, "pragma").WithArguments("#pragma", "2"),
                // (3,2): error CS8022: Feature '#pragma' is not available in C# 1.  Please use language version 2 or greater.
                // #pragma checksum "file.txt" "{00000000-0000-0000-0000-000000000000}" "2453"
                Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion1, "pragma").WithArguments("#pragma", "2"));
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
            SyntaxFactory.ParseSyntaxTree(source, options: CSharpParseOptions.Default.WithLanguageVersion(LanguageVersion.CSharp1)).GetDiagnostics().Verify(
                // (1,2): warning CS1645: Feature 'module as an attribute target specifier' is not part of the standardized ISO C# language specification, and may not be accepted by other compilers
                // [module:Obsolete()]
                Diagnostic(ErrorCode.WRN_NonECMAFeature, "module:").WithArguments("module as an attribute target specifier"));
        }

        [Fact]
        public void CSharp6Features()
        {
            var source =
@"class Foo
{
    int L { get; } = 12; // auto property initializer

    int M() => 12; // expression-bodied method

    int N => 12; // expression-bodied property

    int this[int a] => a + 1; // expression-bodied indexer
    
    public static int operator +(Foo a, Foo b) => null; // expression-bodied operator
    
    public static explicit operator bool(Foo a) => false; // expression-bodied conversion operator

    void P(object o)
    {
        try {
        } catch (Exception ex) when (ex.ToString() == null) { // exception filter
        }

        var s = o?.ToString(); // null propagating operator
        var x = $""hello world"";
    }
}";
            SyntaxFactory.ParseSyntaxTree(source, options: CSharpParseOptions.Default.WithLanguageVersion(LanguageVersion.CSharp6)).GetDiagnostics().Verify();

            SyntaxFactory.ParseSyntaxTree(source, options: CSharpParseOptions.Default.WithLanguageVersion(LanguageVersion.CSharp5)).GetDiagnostics().Verify(
    // (3,20): error CS8026: Feature 'auto property initializer' is not available in C# 5.  Please use language version 6 or greater.
    //     int L { get; } = 12; // auto property initializer
    Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion5, "= 12").WithArguments("auto property initializer", "6").WithLocation(3, 20),
    // (5,13): error CS8026: Feature 'expression-bodied method' is not available in C# 5.  Please use language version 6 or greater.
    //     int M() => 12; // expression-bodied method
    Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion5, "=> 12").WithArguments("expression-bodied method", "6").WithLocation(5, 13),
    // (7,11): error CS8026: Feature 'expression-bodied property' is not available in C# 5.  Please use language version 6 or greater.
    //     int N => 12; // expression-bodied property
    Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion5, "=> 12").WithArguments("expression-bodied property", "6").WithLocation(7, 11),
    // (9,21): error CS8026: Feature 'expression-bodied indexer' is not available in C# 5.  Please use language version 6 or greater.
    //     int this[int a] => a + 1; // expression-bodied indexer
    Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion5, "=> a + 1").WithArguments("expression-bodied indexer", "6").WithLocation(9, 21),
    // (11,48): error CS8026: Feature 'expression-bodied method' is not available in C# 5.  Please use language version 6 or greater.
    //     public static int operator +(Foo a, Foo b) => null; // expression-bodied operator
    Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion5, "=> null").WithArguments("expression-bodied method", "6").WithLocation(11, 48),
    // (13,49): error CS8026: Feature 'expression-bodied method' is not available in C# 5.  Please use language version 6 or greater.
    //     public static explicit operator bool(Foo a) => false; // expression-bodied conversion operator
    Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion5, "=> false").WithArguments("expression-bodied method", "6").WithLocation(13, 49),
    // (18,32): error CS8026: Feature 'exception filter' is not available in C# 5.  Please use language version 6 or greater.
    //         } catch (Exception ex) when (ex.ToString() == null) { // exception filter
    Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion5, "when").WithArguments("exception filter", "6").WithLocation(18, 32),
    // (21,17): error CS8026: Feature 'null propagating operator' is not available in C# 5.  Please use language version 6 or greater.
    //         var s = o?.ToString(); // null propagating operator
    Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion5, "o?.ToString()").WithArguments("null propagating operator", "6").WithLocation(21, 17),
    // (22,17): error CS8026: Feature 'interpolated strings' is not available in C# 5.  Please use language version 6 or greater.
    //         var x = $"hello world";
    Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion5, @"$""hello world""").WithArguments("interpolated strings", "6").WithLocation(22, 17)
                );
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
        [WorkItem(1085618, "DevDiv")]
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

            const int depth = 10000;
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

            builder.Append(@"}");

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

            builder.Append(@"}");

            var stmt = SyntaxFactory.ParseStatement(builder.ToString());
            var actualErrors = stmt.GetDiagnostics().ToArray();
            Assert.Equal(1, actualErrors.Length);
            Assert.Equal((int)ErrorCode.ERR_InsufficientStack, actualErrors[0].Code);
        }

        [Fact]
        [WorkItem(1085618, "DevDiv")]
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
                Diagnostic(ErrorCode.ERR_LbraceExpected, "int").WithLocation(7, 14),
                // (7,14): error CS1002: ; expected
                Diagnostic(ErrorCode.ERR_SemicolonExpected, "int").WithLocation(7, 14),
                // (7,20): error CS1528: Expected ; or = (cannot specify constructor arguments in declaration)
                Diagnostic(ErrorCode.ERR_BadVarDecl, "()").WithLocation(7, 20),
                // (7,20): error CS1003: Syntax error, '[' expected
                Diagnostic(ErrorCode.ERR_SyntaxError, "(").WithArguments("[", "(").WithLocation(7, 20),
                // (7,21): error CS1525: Invalid expression term ')'
                Diagnostic(ErrorCode.ERR_InvalidExprTerm, ")").WithArguments(")").WithLocation(7, 21),
                // (7,22): error CS1003: Syntax error, ']' expected
                Diagnostic(ErrorCode.ERR_SyntaxError, ";").WithArguments("]", ";").WithLocation(7, 22),
                // (8,14): error CS1514: { expected
                Diagnostic(ErrorCode.ERR_LbraceExpected, "int").WithLocation(8, 14),
                // (8,14): error CS1002: ; expected
                Diagnostic(ErrorCode.ERR_SemicolonExpected, "int").WithLocation(8, 14),
                // (8,20): error CS1528: Expected ; or = (cannot specify constructor arguments in declaration)
                Diagnostic(ErrorCode.ERR_BadVarDecl, "()").WithLocation(8, 20),
                // (8,20): error CS1003: Syntax error, '[' expected
                Diagnostic(ErrorCode.ERR_SyntaxError, "(").WithArguments("[", "(").WithLocation(8, 20),
                // (8,21): error CS1525: Invalid expression term ')'
                Diagnostic(ErrorCode.ERR_InvalidExprTerm, ")").WithArguments(")").WithLocation(8, 21),
                // (8,22): error CS1003: Syntax error, ']' expected
                Diagnostic(ErrorCode.ERR_SyntaxError, ";").WithArguments("]", ";").WithLocation(8, 22),
                // (9,2): error CS1513: } expected
                Diagnostic(ErrorCode.ERR_RbraceExpected, "").WithLocation(9, 2));
        }

        #endregion

        #region "Helpers"

        public static void ParseAndValidate(string text, params DiagnosticDescription[] expectedErrors)
        {
            var parsedTree = ParseWithRoundTripCheck(text);
            var actualErrors = parsedTree.GetDiagnostics();
            actualErrors.Verify(expectedErrors);
        }

        public static void ParseAndValidate(string text, CSharpParseOptions options, params DiagnosticDescription[] expectedErrors)
        {
            var parsedTree = ParseWithRoundTripCheck(text, options: options);
            var actualErrors = parsedTree.GetDiagnostics();
            actualErrors.Verify(expectedErrors);
        }

        public static void ParseAndValidateFirst(string text, DiagnosticDescription expectedFirstError)
        {
            var parsedTree = ParseWithRoundTripCheck(text);
            var actualErrors = parsedTree.GetDiagnostics();
            actualErrors.Take(1).Verify(expectedFirstError);
        }

        #endregion
    }
}
