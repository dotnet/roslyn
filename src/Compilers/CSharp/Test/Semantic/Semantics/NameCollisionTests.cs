// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Roslyn.Test.Utilities;
using Xunit;

// The way the specification describes, and the way the native compiler reports name
// collision errors is inconsistent and confusing. In Roslyn we will implement
// the following more rational behaviors:
//
// ------------------
//
// These two error messages are to be reworded:
//
// CS0135: (ERR_NameIllegallyOverrides) 
//
// Original:  'X' conflicts with the declaration 'C.X'
//
// New:       A local, parameter or range variable named 'X' cannot be declared in this scope 
//            because that name is used in an enclosing local scope to refer to 'C.X'.
//
// CS0136: (ERR_LocalIllegallyOverrides) 
//
// Original:  A local variable named 'X' cannot be declared in this scope
//            because it would give a different meaning to 'X', which is 
//            already used in a 'parent or current'  / 'child' 
//            scope to denote something else
//
// New:       A local or parameter named 'X' cannot be declared in this scope
//            because that name is used in an enclosing local scope to define 
//            a local or parameter.
//
// Note now the error messages are now nicely parallel, and much more clear about
// precisely which rule has been violated.
//
// The rules for what error to report in each name collision scenario are as follows:
//
// ---------------------------
//
// Errors for simple names being used to refer to a member in one place and a declared
// entity in another:
//
// CS0135: (ERR_NameIllegallyOverrides) 
// A local, parameter or range variable cannot be named 'X' because
// that name is used in an enclosing local scope to refer to 'C.X'.
//
// Reported *only* when there is a local variable, local constant, lambda parameter or range variable
// that would change the meaning of a *simple name* in an *expression* in an enclosing declaration 
// space to refer to a member, namespace, type, type parameter etc. Report it on the *inner* usage, 
// never the "outer" usage.  eg:
//
// class C { int x; void M() { int y = x; { int x = y; } } }
//
// ---------------------------
//
// Errors for a local being used before it is defined:
//
// CS0841: (ERR_VariableUsedBeforeDeclaration)
// Cannot use local variable 'X' before it is declared
//
// Reported when a local variable is used before it is declared, and the offending
// usage was probably not intended to refer to a field. eg:
//
// class C { void M() { int y = x; int x; } }
// 
// CS0844: (ERR_VariableUsedBeforeDeclarationAndHidesField)
// Cannot use local variable 'X' before it is declared. The 
// declaration of the local variable hides the field 'C.X'.
//
// Reported if the offending usage might have been intended to refer to a field, eg:
//
// class C { int x; void M() { int y = x; int x; } }
//
// ---------------------------
//
// Errors for two of the same identifier being used to declare two different
// things in overlapping or identical declaration spaces:
//
// CS0100: (ERR_DuplicateParamName) 
// The parameter name 'x' is a duplicate
//
// Reported when one parameter list contains two identically-named parameters. Eg:
//
// void M(int x, int x) {} or  (x, x)=>{}
//
// CS0128: (ERR_LocalDuplicate)
// A local variable named 'x' is already defined in this scope
//
// Reported *only* when there are two local variables or constants defined in the
// *exact* same declaration space with the same name. eg:
//
// void M() { int x; int x; } 
//
// CS0136: (ERR_LocalIllegallyOverrides) 
// New:       A local or parameter named 'X' cannot be declared in this scope
//            because that name is used in an enclosing local scope to define 
//            a local or parameter.
//
// Reported *only* when there is a local variable, local constant or lambda parameter
// but NOT range variable that shadows a local variable, local constant, formal parameter,
// range variable, or lambda parameter that was declared in an enclosing local declaration space. Again,
// report it on the inner usage. eg:
//
// void M() { int y; { int y; } }
//
// CS0412: (ERR_LocalSameNameAsTypeParam) 
// 'X': a parameter or local variable cannot have the same name as a method type parameter
//
// Reported *only* when a local variable, local constant, formal parameter or lambda parameter
// has the same name as a method type parameter. eg:
//
// void M<X>(){ int X; }
//
// CS1948: (ERR_QueryRangeVariableSameAsTypeParam) 
// The range variable 'X' cannot have the same name as a method type parameter
//
// Reported *only* when a range variable has the same name as a method type parameter. eg:
//
// void M<X>(){ var q = from X in z select X; }
//
// CS1930: (ERR_QueryDuplicateRangeVariable)
// The range variable 'x' has already been declared
//
// Reported *only* if a range variable shadows another range variable that is in scope. eg:
//
// from x in y from x in z select q
//
// CS1931: (ERR_QueryRangeVariableOverrides)
// The range variable 'x' conflicts with a previous declaration of 'x'
//
// Reported when there is a range variable that shadows a non-range variable from an
// enclosing scope. eg:
//
// int x; var y = from x in q select m;
//

namespace Microsoft.CodeAnalysis.CSharp.UnitTests.Semantics
{
    public class NameCollisionTests : CompilingTestBase
    {
        [Fact]
        public void TestNamesFromTypeAndExpressionContextsDontCollide()
        {
            var source = @"
using name1 = System.Exception;
namespace Namespace
{
    using name3 = System.Type;
    class Class
    {
        Class(name2 other1, name1 other2)
        {
            name3 other3 = typeof(name1);
            if (typeof(name1) != typeof(name2) ||
                typeof(name2) is name3 ||
                typeof(name1) is name3)
            {
                foreach (var name1 in ""string"")
                {
                    for (var name2 = 1; name2 > --name2; name2++)
                    { int name3 = name2; }
                }
            }
            else
            {
                name2 name1 = null, name2 = name1;
                name3 name3 = typeof(name2);
            }
        }
    }
}
class name2
{
}";
            CompileAndVerify(source).VerifyDiagnostics();
        }

        [Fact]
        public void TestLocalAndLabelDontCollide()
        {
            var source = @"
using System;
namespace Namespace
{
    using name1 = System.Type;
    class Class
    {
        Class(name1 name1)
        {
            goto name2;
        name2: Console.WriteLine();
            var name2 = new name2();
            goto name1;
        name1: Console.WriteLine();
        }
    }
}
class name2
{
}";
            CompileAndVerify(source).VerifyDiagnostics();
        }

        [Fact]
        public void TestCollisionOfLabelWithLabel()
        {
            var source = @"
using System;
namespace Namespace
{
    using name1 = System.Type;
    class Class
    {
        Class(name1 name1)
        {
            goto name1;
        name1: Console.WriteLine();
            {
                goto name1;
            name1: Console.WriteLine();
                var name2 = new name2();
                goto name2;
            name2: Console.WriteLine();
            }
            goto name2;
        name2: Console.WriteLine();
        }

        internal int Property
        {
            set
            {
                goto name1;
            name1: Console.WriteLine();
                Action lambda1 = () =>
                {
                    Action lambda2 = () =>
                    {
                        goto name1;
                    name1: Console.WriteLine();
                        var name2 = new name2();
                        goto name2;
                    name2: Console.WriteLine();
                    };
                    goto name2;
                name2: Console.WriteLine();
                };
            }
        }
    }
}
class name2
{
}";
            CreateCompilationWithMscorlib(source).VerifyDiagnostics(
                // (14,13): error CS0158: The label 'name1' shadows another label by the same name in a contained scope
                // name1: Console.WriteLine();
                Diagnostic(ErrorCode.ERR_LabelShadow, "name1").WithArguments("name1"),
                // (17,13): error CS0158: The label 'name2' shadows another label by the same name in a contained scope
                // name2: Console.WriteLine();
                Diagnostic(ErrorCode.ERR_LabelShadow, "name2").WithArguments("name2"),
                // (34,21): error CS0158: The label 'name1' shadows another label by the same name in a contained scope
                //     name1: Console.WriteLine();
                Diagnostic(ErrorCode.ERR_LabelShadow, "name1").WithArguments("name1"),
                // (37,21): error CS0158: The label 'name2' shadows another label by the same name in a contained scope
                //     name2: Console.WriteLine();
                Diagnostic(ErrorCode.ERR_LabelShadow, "name2").WithArguments("name2"));
        }

        [Fact]
        public void TestCollisionOfLocalWithTypeOrMethodOrProperty_LegalCases()
        {
            var source = @"
using System;
namespace name1
{
    class Class
    {
        void name1()
        {
            {
                name1();
            }
            {
                int name1 = name1 = 1;
            }
            foreach(var name1 in ""string"") ;
        }
    }
}
class name2
{
    Action lambda = () =>
    {
        {
            int name2 = name2 = 2;
            Console.WriteLine(name3);
        }
        {
            int name3 = name3 = 3;
        }
    };
    static int name3
    {
        get
        {
            return 4;
        }
    }
}";
            CompileAndVerify(source).VerifyDiagnostics();
        }

        [Fact]
        public void TestCollisionOfLocalWithType()
        {
            var source = @"
using name1 = System.Console;
class Class
{
    Class()
    {
        {
            name1.WriteLine();                             // Legal
            name2.Equals(null, null);                      // Legal
        }
        {
            int name1 = (name1 = 1), name2 = name2 = 2;    // Legal -- strange, but legal
        }
        {
            name1.WriteLine();
            name2.Equals(null, null);
            {
                int name1 = 3, name2 = name1;             // 0135 on name1, name2
                // Native compiler reports 0136 here; Roslyn reports 0135.
            }
        }
    }
}
class name2
{
}";
            CreateCompilationWithMscorlib(source).VerifyDiagnostics();
        }

        [Fact]
        public void TestCollisionOfLocalWithMethodOrProperty()
        {
            var source = @"
using System;
namespace name1
{
    class Class
    {
        void name1()
        {
            name1();
            {
                name1();
            }
            {
                int name1 = name1 = 1;           // 0135: Roslyn reports 0135, native reports 0136.
            }
            foreach (var name1 in ""string"") ;  // 0135: Roslyn reports 0135, native reports 0136.
        }

        Action lambda = () =>
        {
            {
                int name2 = name2 = 2;        // 0135: conflicts with usage of name2 as the static property below.
                                              // Roslyn reports this here; native compiler reports it below.
                Console.WriteLine(name2);
            } 
            Console.WriteLine(name2);         // Native compiler reports 0135 here; Roslyn reports it above.
        };
        static int name2
        {
            get
            {
                return 3;
            }
        }
    }
}";
            CreateCompilationWithMscorlib(source).VerifyDiagnostics();
        }

        [WorkItem(542039, "DevDiv")]
        [Fact]
        public void TestCollisionOfDelegateWithConst()
        {
            var source = @"class A
{
    delegate void D();
    static void Foo() { }
    class B
    {
        const int Foo = 123;
        static void Main()
        {
            Foo();
            Bar(Foo);
        }
        static void Main2()
        {
            Bar(Foo);
            Foo();
        }
        static void Bar(int x) { }
        static void Bar(D x) { }
    }
}";
            CreateCompilationWithMscorlib(source).VerifyDiagnostics();
        }

        [Fact]
        public void TestCollisionOfLocalWithTypeParameter()
        {
            var source = @"
class Class<name1, name2>
{
    void Method<name3, name4>(name1 other1, name4 name4)               // 0412 on name4
    {
        {
            int name3 = 10;                                            // 0412 on name3
            System.Console.WriteLine(name3);                           // Eliminate warning
            foreach (var name2 in ""string"")
            {
                for (var name1 = 1; name1 <= name1++; name1++)         // legal; name1 conflicts with a class type parameter which is not in the local variable decl space
                    name1 = name2.GetHashCode();
            }
        }
        {
            name1 other2 = typeof(name1) is name1 ? other1 : other1;   // no error; all the name1's refer to the type, not the local.
            int name1 = (name1 = 2), name2 = name2 = 3;                // legal; name1 conflicts with a class type parameter which is not in the local variable decl space
            foreach (var name3 in ""string"")                          // 0412 on name3 
            {
                System.Console.WriteLine(name3);                       // Eliminate warning
                for (var name4 = 4; ; )                                // 0412 on name4
                {
                    name1 = name2.GetHashCode();
                    System.Console.WriteLine(name4);                   // Eliminate warning
                }
            }
            try {} 
            catch(System.Exception name3)                              // 0412 on name3
            { System.Console.WriteLine(name3); }
        }
    }
}";
            CreateCompilationWithMscorlib(source).VerifyDiagnostics(
                // (4,51): error CS0412: 'name4': a parameter or local variable cannot have the same name as a method type parameter
                // void Method<name3, name4>(name1 other1, name4 name4) // 0412 on name4
                Diagnostic(ErrorCode.ERR_LocalSameNameAsTypeParam, "name4").WithArguments("name4").WithLocation(4, 51),
                // (7,17): error CS0412: 'name3': a parameter or local variable cannot have the same name as a method type parameter
                // int name3 = 10; // 0412 on name3
                Diagnostic(ErrorCode.ERR_LocalSameNameAsTypeParam, "name3").WithArguments("name3").WithLocation(7, 17),
                // (18,26): error CS0412: 'name3': a parameter or local variable cannot have the same name as a method type parameter
                // foreach (var name3 in "string") // 0412 on name3 
                Diagnostic(ErrorCode.ERR_LocalSameNameAsTypeParam, "name3").WithArguments("name3").WithLocation(18, 26),
                // (21,26): error CS0136: A local or parameter named 'name4' cannot be declared in this scope because that name is used in an enclosing local scope to define a local or parameter
                //                 for (var name4 = 4; ; )                                // 0412 on name4
                Diagnostic(ErrorCode.ERR_LocalIllegallyOverrides, "name4").WithArguments("name4"),
                // (28,36): error CS0412: 'name3': a parameter or local variable cannot have the same name as a method type parameter
                // catch(System.Exception name3) // 0412 on name3
                Diagnostic(ErrorCode.ERR_LocalSameNameAsTypeParam, "name3").WithArguments("name3"));
        }

        [Fact]
        public void TestCollisionOfLocalWithField_LegalCases()
        {
            var source = @"
partial class Derived : Base
{
    private Derived()
    {
        this.name1 = 1;
        long name1 = this.name1;
        if (true)
        {
            name1 = this.name1 = name1;
            name2 = this.name2 = name2 + name1;
        }
        {
            while (name1 == 1)
            {
                long name2 = 2; name1 = name2;
            }
            do
            {
                long name2 = 3; name1 = name2;
                name1 = this.name1;
            }
            while (name1 != 1);
        }
    }
}
class Base
{
    public long name2 = name1;
    private static int name1 = 4;
}
partial class Derived
{
    internal long name1 = 5;
}";
            CompileAndVerify(source).VerifyDiagnostics();
        }

        [Fact]
        public void TestCollisionOfLocalWithField1()
        {
            var source = @"
class Derived : Base
{
    static long name1 = 1;
    static Derived()
    {
        while(name1 == 2)
        {
            int name1 = 3, other = name1, name2 = other;  // 0135 on name1 and name2
                                                          // Native reports 0136 on name1 here and 0135 on name2 below.
        }
        do
        {
        }
        while (name2 == 4);  // Native reports 0135 on name2 here; Roslyn reports it above.
    }
}
class Base
{
    protected static long name2 = 5;
}";
            CreateCompilationWithMscorlib(source).VerifyDiagnostics();
        }

        [Fact]
        public void TestCollisionOfLocalWithField2()
        {
            var source = @"
class Class
{
    public static int M() { return 1; }
    internal int Property
    {
        set
        {
            for (int i = 0; i < int.MaxValue; ++i)
            {
                if (i == 0)
                {
                    int other = M(), name = M(), name = other; // 0128, 0135
                }
                else
                {
                    {
                        int name = M(); name = M(); // 0135
                    }
                }
            }
            for (int i = 0; i > int.MinValue; ++i)
            {
                {   i += 1; }
            }
            name = M();
        }
    }

    private const int x = 123;
    private void M1(int x = x) {} // UNDONE: Native and Roslyn compilers both allow this; should they?
    private void M2(int y = x)
    {
        int x = M(); // UNDONE: Native and Roslyn compilers both allow this; should they?
    }

    private long other = 0, name = 6;
}";
            CreateCompilationWithMscorlib(source).VerifyDiagnostics(
                // (13,50): error CS0128: A local variable named 'name' is already defined in this scope
                //                     int other = M(), name = M(), name = other; // 0128, 0135
                Diagnostic(ErrorCode.ERR_LocalDuplicate, "name").WithArguments("name"),
                // (37,18): warning CS0414: The field 'Class.other' is assigned but its value is never used
                //     private long other = 0, name = 6;
                Diagnostic(ErrorCode.WRN_UnreferencedFieldAssg, "other").WithArguments("Class.other")
                );
        }

        [Fact]
        public void TestCollisionInsideFieldDeclaration()
        {
            // A close reading of the spec would indicate that this is not an error because the
            // offending simple name 'x' does not appear in any local variable declaration space.
            // A field initializer is not a declaration space. However, it seems plausible
            // that we want to report the error here. The native compiler does so as well.

            var source = @"
class Class
{
    private static int M() { return 1; }
    private static int x = 123;
    private static int z = x + ((System.Func<int>)( ()=>{ int x = M(); return x; } ))();
}";
            CreateCompilationWithMscorlib(source).VerifyDiagnostics();
        }

        [Fact]
        public void TestCollisionOfLocalWithField_PartialType()
        {
            var source = @"
partial struct PartialStruct
{
    private void Method()
    {
        if (true)
        {
            {
                int name = 1, other = name; 
            }
        }
        name = 2; // Native compiler reports 0135 here; Roslyn no longer reports 0135.
    }
}

partial struct PartialStruct
{
    internal long name;
}";
            CreateCompilationWithMscorlib(source).VerifyDiagnostics();
        }

        [Fact]
        public void TestCollisionOfLocalWithLocal_LegalCases1()
        {
            var source = @"
partial class Derived : Base
{
    private string Property
    {
        get
        {
            if (true)
            {
                int name = (name = 1); name += name;
            }
            {
                {
                    int name = 2; name -= name;
                }
            }
            for(long name = 3; name <= 4; ++name)
                name += 5;
            foreach(var name in ""string"")
            {
                name.ToString();
            }
            return this.name;
        }
    }
}
class Base
{
    public string name = null;
}";
            CompileAndVerify(source).VerifyDiagnostics();
        }

        [Fact]
        public void TestCollisionOfLocalWithLocal_LegalCases2()
        {
            var source = @"
using System;
using System.Linq;
partial class Derived : Base
{
    private string Property
    {
        get
        {
            // http://blogs.msdn.com/b/ericlippert/archive/2009/11/02/simple-names-are-not-so-simple.aspx
            foreach (var name in from name in ""string"" orderby name select name)
                Console.WriteLine(name);
            return this.name;
        }
    }
}
class Base
{
    public string name = null;
}";
            CompileAndVerify(source, new[] { LinqAssemblyRef }).VerifyDiagnostics();
        }

        [Fact]
        public void TestCollisionOfLocalWithLocal_LegalCases3()
        {
            var source = @"
using System;
using System.Linq;
partial class Derived : Base
{
    private string Property
    {
        get
        {
            // http://blogs.msdn.com/b/ericlippert/archive/2009/11/02/simple-names-are-not-so-simple.aspx
            foreach(var name in ""string"".OrderBy(name => name).Select(name => name))
            {
                Console.WriteLine(name);
            }
            return this.name;
        }
    }
}
class Base
{
    public string name = null;
}";
            CompileAndVerify(source, new[] { LinqAssemblyRef }).VerifyDiagnostics();
        }

        [Fact]
        public void TestCollisionOfLocalWithLocal()
        {
            var source = @"
class Class
{
    public Class()
    {
        long name1 = 1; System.Console.WriteLine(name1); // Eliminate unused warning.
        name4 = name6;             // 0841 on name4; used before declared. 0103 on name6; not defined in this context
        if (true)
        {
            {
                int other1 = 2, name1 = other1, name2 = name1; // 0136 on name1; already used in parent scope to mean something else.
            } // Native compiler reports 0136 on 'long name2' below; Roslyn reports it on 'int ... name2' here and 'var name2' below
            {
                if (true)
                {
                    for (long name1 = this.name2; name1 >= --name1; name1++) // 0136 on name1; 
                    {
                        name1.ToString(); name5.ToString(); // 0841: name5 is used before the declaration
                        string name6 = ""string"";
                    }
                }
                foreach (var name2 in ""string"") name2.ToString(); // 0136: Native reports this on 'long name2' below; Roslyn reports it here, and above.
            }
            string @name3 = ""string"", other2 = name3, name3 = other2; // 0128: name3 is defined twice.
            long @name2 = 3; System.Console.WriteLine(@name2); // eliminated unused warning.
            // Native compiler reports 0136 on 'long name2' here; Roslyn reports it on 'int ... name2' above.
        }
        string name4 = ""string"", name5 = name4;
        name6 = name3; // 0103 on both name6 and name3; not defined in this context.
    }
    public long name2 = 4;
}";
            CreateCompilationWithMscorlib(source).VerifyDiagnostics(
    // (7,9): error CS0841: Cannot use local variable 'name4' before it is declared
    //         name4 = name6;             // 0841 on name4; used before declared. 0103 on name6; not defined in this context
    Diagnostic(ErrorCode.ERR_VariableUsedBeforeDeclaration, "name4").WithArguments("name4").WithLocation(7, 9),
    // (7,17): error CS0103: The name 'name6' does not exist in the current context
    //         name4 = name6;             // 0841 on name4; used before declared. 0103 on name6; not defined in this context
    Diagnostic(ErrorCode.ERR_NameNotInContext, "name6").WithArguments("name6").WithLocation(7, 17),
    // (11,33): error CS0136: A local or parameter named 'name1' cannot be declared in this scope because that name is used in an enclosing local scope to define a local or parameter
    //                 int other1 = 2, name1 = other1, name2 = name1; // 0136 on name1; already used in parent scope to mean something else.
    Diagnostic(ErrorCode.ERR_LocalIllegallyOverrides, "name1").WithArguments("name1").WithLocation(11, 33),
    // (11,49): error CS0136: A local or parameter named 'name2' cannot be declared in this scope because that name is used in an enclosing local scope to define a local or parameter
    //                 int other1 = 2, name1 = other1, name2 = name1; // 0136 on name1; already used in parent scope to mean something else.
    Diagnostic(ErrorCode.ERR_LocalIllegallyOverrides, "name2").WithArguments("name2").WithLocation(11, 49),
    // (16,31): error CS0136: A local or parameter named 'name1' cannot be declared in this scope because that name is used in an enclosing local scope to define a local or parameter
    //                     for (long name1 = this.name2; name1 >= --name1; name1++) // 0136 on name1; 
    Diagnostic(ErrorCode.ERR_LocalIllegallyOverrides, "name1").WithArguments("name1").WithLocation(16, 31),
    // (18,43): error CS0841: Cannot use local variable 'name5' before it is declared
    //                         name1.ToString(); name5.ToString(); // 0841: name5 is used before the declaration
    Diagnostic(ErrorCode.ERR_VariableUsedBeforeDeclaration, "name5").WithArguments("name5").WithLocation(18, 43),
    // (22,30): error CS0136: A local or parameter named 'name2' cannot be declared in this scope because that name is used in an enclosing local scope to define a local or parameter
    //                 foreach (var name2 in "string") name2.ToString(); // 0136: Native reports this on 'long name2' below; Roslyn reports it here, and above.
    Diagnostic(ErrorCode.ERR_LocalIllegallyOverrides, "name2").WithArguments("name2").WithLocation(22, 30),
    // (24,55): error CS0128: A local variable named 'name3' is already defined in this scope
    //             string @name3 = "string", other2 = name3, name3 = other2; // 0128: name3 is defined twice.
    Diagnostic(ErrorCode.ERR_LocalDuplicate, "name3").WithArguments("name3").WithLocation(24, 55),
    // (29,9): error CS0103: The name 'name6' does not exist in the current context
    //         name6 = name3; // 0103 on both name6 and name3; not defined in this context.
    Diagnostic(ErrorCode.ERR_NameNotInContext, "name6").WithArguments("name6").WithLocation(29, 9),
    // (29,17): error CS0103: The name 'name3' does not exist in the current context
    //         name6 = name3; // 0103 on both name6 and name3; not defined in this context.
    Diagnostic(ErrorCode.ERR_NameNotInContext, "name3").WithArguments("name3").WithLocation(29, 17),
    // (19,32): warning CS0219: The variable 'name6' is assigned but its value is never used
    //                         string name6 = "string";
    Diagnostic(ErrorCode.WRN_UnreferencedVarAssg, "name6").WithArguments("name6").WithLocation(19, 32)

                );
        }

        [Fact]
        public void TestCollisionOfLocalWithParam()
        {
            var source = @"
using System;
class Class
{
    public Func<int, int, int> Method(int name1, int name2)
    {
        foreach (var name1 in ""string"")            // 0136
        {
            foreach (var name2 in ""string"")        // 0136
            {
                int name1 = name2.GetHashCode();     // 0136
            }
        }

        Action<int> lambda = (name3) =>
        {
            int name1 = 1;                           // 0136
            if(name1 == 2)
            {
                name2 = name3 = name1;
            }
            else
            {
                int name2 = 3;                       // 0136
                System.Console.WriteLine(name2);
                {
                    int name3 = 2;                   // 0136
                    System.Console.WriteLine(name3);
                }
            }
        };
        return (name1, name2) => name1;              // 0136 on both name1 and name2
    }
}";
            CreateCompilationWithMscorlib(source).VerifyDiagnostics(
    // (7,22): error CS0136: A local or parameter named 'name1' cannot be declared in this scope because that name is used in an enclosing local scope to define a local or parameter
    //         foreach (var name1 in "string")            // 0136
    Diagnostic(ErrorCode.ERR_LocalIllegallyOverrides, "name1").WithArguments("name1").WithLocation(7, 22),
    // (9,26): error CS0136: A local or parameter named 'name2' cannot be declared in this scope because that name is used in an enclosing local scope to define a local or parameter
    //             foreach (var name2 in "string")        // 0136
    Diagnostic(ErrorCode.ERR_LocalIllegallyOverrides, "name2").WithArguments("name2").WithLocation(9, 26),
    // (11,21): error CS0136: A local or parameter named 'name1' cannot be declared in this scope because that name is used in an enclosing local scope to define a local or parameter
    //                 int name1 = name2.GetHashCode();     // 0136
    Diagnostic(ErrorCode.ERR_LocalIllegallyOverrides, "name1").WithArguments("name1").WithLocation(11, 21),
    // (17,17): error CS0136: A local or parameter named 'name1' cannot be declared in this scope because that name is used in an enclosing local scope to define a local or parameter
    //             int name1 = 1;                           // 0136
    Diagnostic(ErrorCode.ERR_LocalIllegallyOverrides, "name1").WithArguments("name1").WithLocation(17, 17),
    // (24,21): error CS0136: A local or parameter named 'name2' cannot be declared in this scope because that name is used in an enclosing local scope to define a local or parameter
    //                 int name2 = 3;                       // 0136
    Diagnostic(ErrorCode.ERR_LocalIllegallyOverrides, "name2").WithArguments("name2").WithLocation(24, 21),
    // (27,25): error CS0412: 'name3': a parameter or local variable cannot have the same name as a method type parameter
    //                     int name3 = 2;                   // 0136
    Diagnostic(ErrorCode.ERR_LocalSameNameAsTypeParam, "name3").WithArguments("name3").WithLocation(27, 25),
    // (32,17): error CS0136: A local or parameter named 'name1' cannot be declared in this scope because that name is used in an enclosing local scope to define a local or parameter
    //         return (name1, name2) => name1;              // 0136 on both name1 and name2
    Diagnostic(ErrorCode.ERR_LocalIllegallyOverrides, "name1").WithArguments("name1").WithLocation(32, 17),
    // (32,24): error CS0136: A local or parameter named 'name2' cannot be declared in this scope because that name is used in an enclosing local scope to define a local or parameter
    //         return (name1, name2) => name1;              // 0136 on both name1 and name2
    Diagnostic(ErrorCode.ERR_LocalIllegallyOverrides, "name2").WithArguments("name2").WithLocation(32, 24)
);
        }

        [Fact]
        public void TestCollisionOfParamWithParam()
        {
            var source = @"
using System;
class Class
{
    public static void Method(int name1, int name2, int name2) // 0100 on name2
    {
        Action<int, int> lambda = (other, name3) => 
        {
            Action<int, int, int, int> nestedLambda = (name1, name4, name4, name3) => // 0100 on name4, 0136 on name1 and name3
            {
            };
        };
    }
}";
            CreateCompilationWithMscorlib(source).VerifyDiagnostics(
    // (5,57): error CS0100: The parameter name 'name2' is a duplicate
    //     public static void Method(int name1, int name2, int name2) // 0100 on name2
    Diagnostic(ErrorCode.ERR_DuplicateParamName, "name2").WithArguments("name2").WithLocation(5, 57),
    // (9,56): error CS0136: A local or parameter named 'name1' cannot be declared in this scope because that name is used in an enclosing local scope to define a local or parameter
    //             Action<int, int, int, int> nestedLambda = (name1, name4, name4, name3) => // 0100 on name4, 0136 on name1 and name3
    Diagnostic(ErrorCode.ERR_LocalIllegallyOverrides, "name1").WithArguments("name1").WithLocation(9, 56),
    // (9,70): error CS0100: The parameter name 'name4' is a duplicate
    //             Action<int, int, int, int> nestedLambda = (name1, name4, name4, name3) => // 0100 on name4, 0136 on name1 and name3
    Diagnostic(ErrorCode.ERR_DuplicateParamName, "name4").WithArguments("name4").WithLocation(9, 70),
    // (9,77): error CS0412: 'name3': a parameter or local variable cannot have the same name as a method type parameter
    //             Action<int, int, int, int> nestedLambda = (name1, name4, name4, name3) => // 0100 on name4, 0136 on name1 and name3
    Diagnostic(ErrorCode.ERR_LocalSameNameAsTypeParam, "name3").WithArguments("name3").WithLocation(9, 77)
    );
        }

        [WorkItem(930252)]
        [Fact]
        public void TestCollisionOfParamWithParam1()
        {
            var source = @"
class Program
{
    delegate int D(int x, int y);
    static void X()
    {
        D d1 = (int x, int x) => { return 1; };
        D d2 = (x, x) => { return 1; };
    }
}";

            CreateCompilationWithMscorlib(source).VerifyDiagnostics(
            // (7,28): error CS0100: The parameter name 'x' is a duplicate
            //         D d1 = (int x, int x) => { return 1; };
            Diagnostic(ErrorCode.ERR_DuplicateParamName, "x").WithArguments("x").WithLocation(7, 28),
            // (8,20): error CS0100: The parameter name 'x' is a duplicate
            //         D d2 = (x, x) => { return 1; };
            Diagnostic(ErrorCode.ERR_DuplicateParamName, "x").WithArguments("x").WithLocation(8, 20)
            );
        }

        [Fact]
        public void TestCollisionInsideLambda_LegalCases()
        {
            var source = @"
using System;
partial class Class
{
    private string Property
    {
        set
        {
            this.
                Method((name1) =>
                       {
                           name1 = string.Empty;
                           for (int name2 = name2 = 1; ; ) ;
                       }).
                Method((name1) => name1.ToString()).
                Method((name1) => 
                       {
                           foreach (var name2 in string.Empty) ;
                           return name1; 
                       });
        }
    }
    Class Method(Action<string> name1)
    {
        return null;
    }
    Class Method(Func<string, string> name1)
    {
        return null;
    }
}";
            CompileAndVerify(source).VerifyDiagnostics();
        }

        [Fact]
        public void TestCollisionInsideLambda1()
        {
            var source = @"
using System;
class Derived : Base
{
    static int M() { return 1; }
    static long name1 = 1;
    Action lambda = () =>
    {
        name1 = 2;
        {
            int name1 = 3, other = name1, name2 = other; // 0135: on name1 and name2.
            // Native compiler reports 0136 here on name1 and 0135 on name2 below. 
            // Roslyn reports them both as 0135 here.
        }
        name2 = 4; // Native compiler reports 0135 here; Roslyn reports above.

        int name3 = M();
        {
            {
                name3 = 6; 
            }
            if (true)
            {
                int name3 = M(); // 0136: Native compiler says 0135, Roslyn says 0136. The conflict is with the other local.
            }
        }
    };

    Action anonMethod = delegate()
    {
        name1 = 8;
        if (true)
        {
            int name1 = 9, other = name1, name2 = other;  // 0135: on name1, name2
            // Native compiler reports 0136 on name1, Roslyn reports 0135.
            // Native compiler reports 0135 on name2 below, Roslyn reports it here.
        }
        {
            foreach (var name3 in ""string"") name3.ToString(); // 0136: Native compiler reports 0136 below, Roslyn reports it here.
        }
        name2 = 10; // Native compiler reports 0135 here; Roslyn reports it above.
        int name3 = M(); 
    };
}
class Base
{
    protected static long name2 = 12;
}";
            CreateCompilationWithMscorlib(source).VerifyDiagnostics(
    // (24,21): error CS0136: A local or parameter named 'name3' cannot be declared in this scope because that name is used in an enclosing local scope to define a local or parameter
    //                 int name3 = M(); // 0136: Native compiler says 0135, Roslyn says 0136. The conflict is with the other local.
    Diagnostic(ErrorCode.ERR_LocalIllegallyOverrides, "name3").WithArguments("name3").WithLocation(24, 21),
    // (39,26): error CS0136: A local or parameter named 'name3' cannot be declared in this scope because that name is used in an enclosing local scope to define a local or parameter
    //             foreach (var name3 in "string") name3.ToString(); // 0136: Native compiler reports 0136 below, Roslyn reports it here.
    Diagnostic(ErrorCode.ERR_LocalIllegallyOverrides, "name3").WithArguments("name3").WithLocation(39, 26),
    // (6,17): warning CS0414: The field 'Derived.name1' is assigned but its value is never used
    //     static long name1 = 1;
    Diagnostic(ErrorCode.WRN_UnreferencedFieldAssg, "name1").WithArguments("Derived.name1").WithLocation(6, 17)
);
        }

        [Fact]
        public void TestCollisionInsideLambda2()
        {
            var source = @"
using System;
class Class
{
    void Method(Action lambda)
    {
    }
    void Method()
    {
        const long name1 = 1; System.Console.WriteLine(name1); // Eliminate warning.
        Method(() =>
        {
            Console.WriteLine(name1);
            {
                const int name1 = 2;              // 0136 
                int other = name1, name2 = other; // 0136: Native compiler reports this on 'const long name' below; Roslyn reports it here.
            }
            name2 = 3;                            // 0841: local used before declared

            const int name3 = 4;
            {
                {
                    Console.WriteLine(name3);
                }
                if (true)
                {
                    const int name3 = 5;  // 0136 
                    Console.WriteLine(name3);
                }
            }
        });

        Method(delegate()
        {
            Console.WriteLine(name1);
            if (true)
            {
                const int name1 = 6, other = name1, name2 = other;   // 0136 on name1 and name2
                Console.WriteLine(name1 + other + name2); 
            } // Roslyn reports 0136 on name2 above; native compiler reports it on 'const long name2' below.
            {
                foreach (var name3 in ""string"") name3.ToString(); // 0136: Roslyn reports this here, native reports it below.
            }
            Console.WriteLine(name2);   // 0814: local used before declared
            const int name3 = 7;        // Native compiler reports 0136 here, Roslyn reports it on 'var name3' above.
            Console.WriteLine(name3);   // eliminate warning
        });
        const long name2 = 8; // Native compiler reports 0136 here; Roslyn reports it on both offending nested decls above.
        Console.WriteLine(name2); 
    }
}";
            CreateCompilationWithMscorlib(source).VerifyDiagnostics(
                // (15,27): error CS0136: A local or parameter named 'name1' cannot be declared in this scope because that name is used in an enclosing local scope to define a local or parameter
                // const int name1 = 2;              // 0136 
                Diagnostic(ErrorCode.ERR_LocalIllegallyOverrides, "name1").WithArguments("name1").WithLocation(15, 27),
                // (16,36): error CS0136: A local or parameter named 'name2' cannot be declared in this scope because that name is used in an enclosing local scope to define a local or parameter
                // int other = name1, name2 = other;
                Diagnostic(ErrorCode.ERR_LocalIllegallyOverrides, "name2").WithArguments("name2").WithLocation(16, 36),
                // (18,13): error CS0841: Cannot use local variable 'name2' before it is declared
                // name2 = 3;                            // 0841: local used before declared
                Diagnostic(ErrorCode.ERR_VariableUsedBeforeDeclaration, "name2").WithArguments("name2").WithLocation(18, 13),
                // (27,31): error CS0136: A local or parameter named 'name3' cannot be declared in this scope because that name is used in an enclosing local scope to define a local or parameter
                // const int name3 = 5;  // 0136 
                Diagnostic(ErrorCode.ERR_LocalIllegallyOverrides, "name3").WithArguments("name3").WithLocation(27, 31),
                // (38,27): error CS0136: A local or parameter named 'name1' cannot be declared in this scope because that name is used in an enclosing local scope to define a local or parameter
                // const int name1 = 6, other = name1, name2 = other;   // 0136 on name1 and name2
                Diagnostic(ErrorCode.ERR_LocalIllegallyOverrides, "name1").WithArguments("name1").WithLocation(38, 27),
                // (38,53): error CS0136: A local or parameter named 'name2' cannot be declared in this scope because that name is used in an enclosing local scope to define a local or parameter
                // const int name1 = 6, other = name1, name2 = other;   // 0136 on name1 and name2
                Diagnostic(ErrorCode.ERR_LocalIllegallyOverrides, "name2").WithArguments("name2").WithLocation(38, 53),
                // (42,30): error CS0841: Cannot use local variable 'name3' before it is declared
                // foreach (var name3 in ""string"") name3.ToString(); // 0136
                Diagnostic(ErrorCode.ERR_LocalIllegallyOverrides, "name3").WithArguments("name3").WithLocation(42, 30),
                // (44,31): error CS0841: Cannot use local variable 'name2' before it is declared
                // Console.WriteLine(name2);   // 0814: local used before declared
                Diagnostic(ErrorCode.ERR_VariableUsedBeforeDeclaration, "name2").WithArguments("name2").WithLocation(44, 31));
        }

        [Fact]
        public void TestCollisionInsideOperator()
        {
            var source = @"
using System;
class Class
{
    static long name1 = 1;
    public static Class operator +(Class name1, Class other)
    {
        var lambda = (Action)(() =>
        {
            const int name1 = @name2; // 0136 on name1 because it conflicts with parameter
            if (true)
            {
                int name2 = name1; // 0135 because name2 conflicts with usage of name2 as Class.name2 above
                Console.WriteLine(name2);
            }
        });
        
        return other;
    }
    const int name2 = 2;
    public static void Other()
    {
        Console.WriteLine(name1);
    }
}";
            CreateCompilationWithMscorlib(source).VerifyDiagnostics(
    // (10,23): error CS0136: A local or parameter named 'name1' cannot be declared in this scope because that name is used in an enclosing local scope to define a local or parameter
    //             const int name1 = @name2; // 0136 on name1 because it conflicts with parameter
    Diagnostic(ErrorCode.ERR_LocalIllegallyOverrides, "name1").WithArguments("name1").WithLocation(10, 23)
);
        }

        [Fact]
        public void TestCollisionInsideIndexer()
        {
            var source = @"
class Class
{
    static long name1 = 1;
    public int this[int name1]
    {
        get
        {
            foreach (var name2 in ""string"")
            {
                foreach (var name2 in ""string"")    // 0136 on name2
                {
                    int name1 = name2.GetHashCode(); // 0136 on name1
                }
            }
            return name1;
        }
    }
    static long name2 = name1 + name2;
}";
            CreateCompilationWithMscorlibAndSystemCore(source).VerifyDiagnostics(
                // (11,30): error CS0136: A local or parameter named 'name2' cannot be declared in this scope because that name is used in an enclosing local scope to define a local or parameter
                // foreach (var name2 in "string")    // 0136 on name2
                Diagnostic(ErrorCode.ERR_LocalIllegallyOverrides, "name2").WithArguments("name2"),
                // (13,25): error CS0136: A local or parameter named 'name1' cannot be declared in this scope because that name is used in an enclosing local scope to define a local or parameter
                // int name1 = name2.GetHashCode(); // 0136 on name1
                Diagnostic(ErrorCode.ERR_LocalIllegallyOverrides, "name1").WithArguments("name1"));
        }

        [Fact]
        public void TestCollisionInsideFor1()
        {
            var source = @"
class Class
{
    void Method1(int name4 = 1, params int[] name5)
    {
        for (int name1 = 2; name1 <= name1++; ++name1)
        {
            foreach (var name2 in ""string"")
            {
                for (name1 = 3; ; ) { break; }
                for (int name2 = name1; name2 <= name2++; ++name2)   // 0136 on name2
                {
                    int name3 = 4, name4 = 5, name5 = 6; // 0136 on name3, name4 and name5
                    // Native compiler reports 0136 on name3 below, Roslyn reports it above.
                    System.Console.WriteLine(name3 + name4 + name5); // Eliminate warning
                }
            }
            foreach (var name1 in ""string"") ; // 0136 on name1
        }
        int name3 = 7; // Native compiler reports 0136 on name3 here; Roslyn reports it above.
        System.Console.WriteLine(name3); // Eliminate warning
    }
}";
            CreateCompilationWithMscorlib(source).VerifyDiagnostics(
                // (11,26): error CS0136: A local or parameter named 'name2' cannot be declared in this scope because that name is used in an enclosing local scope to define a local or parameter
                // for (int name2 = name1; name2 <= name2++; ++name2)   // 0136 on name2
                Diagnostic(ErrorCode.ERR_LocalIllegallyOverrides, "name2").WithArguments("name2").WithLocation(11, 26),
                // (13,25): error CS0136: A local or parameter named 'name3' cannot be declared in this scope because that name is used in an enclosing local scope to define a local or parameter
                // int name3 = 4, name4 = 5, name5 = 6; // 0136
                Diagnostic(ErrorCode.ERR_LocalIllegallyOverrides, "name3").WithArguments("name3").WithLocation(13, 25),
                // (13,36): error CS0136: A local or parameter named 'name4' cannot be declared in this scope because that name is used in an enclosing local scope to define a local or parameter
                // int name3 = 4, name4 = 5, name5 = 6; // 0136
                Diagnostic(ErrorCode.ERR_LocalIllegallyOverrides, "name4").WithArguments("name4").WithLocation(13, 36),
                // (13,47): error CS0136: A local or parameter named 'name5' cannot be declared in this scope because that name is used in an enclosing local scope to define a local or parameter
                // int name3 = 4, name4 = 5, name5 = 6; // 0136
                Diagnostic(ErrorCode.ERR_LocalIllegallyOverrides, "name5").WithArguments("name5").WithLocation(13, 47),
                // (13,47): error CS0136: A local or parameter named 'name1' cannot be declared in this scope because that name is used in an enclosing local scope to define a local or parameter
                // foreach (var name1 in ""string"") ; // 0136 on name1
                Diagnostic(ErrorCode.ERR_LocalIllegallyOverrides, "name1").WithArguments("name1").WithLocation(18, 26));
        }

        [Fact]
        public void TestCollisionInsideFor2()
        {
            var source = @"
using System.Linq;
using System.Collections;
partial class Class
{
    private string Property
    {
        get
        {
            for (var name = from name in ""string"" orderby name select name; name != null; ) ;                     // 1931
            for (IEnumerable name = null; name == from name in ""string"" orderby name select name; ) ;             // 1931
            for (IEnumerable name = null; name == null; name = from name in ""string"" orderby name select name ) ; // 1931
                return string.Empty;
        }
    }
}";
            CreateCompilationWithMscorlibAndSystemCore(source).VerifyDiagnostics(
                // (10,34): error CS1931: The range variable 'name' conflicts with a previous declaration of 'name'
                // for (var name = from name in "string" orderby name select name; name != null; ) ;                     // 1931
                Diagnostic(ErrorCode.ERR_QueryRangeVariableOverrides, "name").WithArguments("name"),
                // (11,56): error CS1931: The range variable 'name' conflicts with a previous declaration of 'name'
                // for (IEnumerable name = null; name == from name in "string" orderby name select name; ) ;             // 1931
                Diagnostic(ErrorCode.ERR_QueryRangeVariableOverrides, "name").WithArguments("name"),
                // (12,69): error CS1931: The range variable 'name' conflicts with a previous declaration of 'name'
                // for (IEnumerable name = null; name == null; name = from name in "string" orderby name select name ) ; // 1931
                Diagnostic(ErrorCode.ERR_QueryRangeVariableOverrides, "name").WithArguments("name"));
        }

        [WorkItem(792744, "DevDiv")]
        [Fact]
        public void TestCollisionInsideForeach()
        {
            var source = @"
class Class
{
    static int y = 1;
    static void Main(string[] args)
    {
        foreach (var y in new[] {new { y = y }}){ }
        //End
    }
}";
            CreateCompilationWithMscorlibAndSystemCore(source).VerifyDiagnostics();
        }


        [Fact]
        public void TestCollisionInsideUsing()
        {
            var source = @"
class Class : System.IDisposable
{
    public void Dispose() {}
    int[] name3 = {};
    void Method1(Class name2 = null)
    {
        using (var name1 = new Class())
        {
            int other = (name3[0]);
        }
        using (var name1 = new Class())
        {
            var other = name3[0].ToString();
            using (var name3 = new Class()) // 0135 because name3 above refers to this.name3
            {
                int name1 = 2;  // 0136 on name1 
            }
        }
        using (var name2 = new Class()) // 0136 on name2. 
        {
            int name1 = 2;
            int other = (name3[0]);
        }
        using (name2 = new Class())
        {
            int name1 = 2;
            int other = (name3[0]);
        }
    }
}";
            CreateCompilationWithMscorlib(source).VerifyDiagnostics(
    // (17,21): error CS0136: A local or parameter named 'name1' cannot be declared in this scope because that name is used in an enclosing local scope to define a local or parameter
    //                 int name1 = 2;  // 0136 on name1 
    Diagnostic(ErrorCode.ERR_LocalIllegallyOverrides, "name1").WithArguments("name1").WithLocation(17, 21),
    // (20,20): error CS0136: A local or parameter named 'name2' cannot be declared in this scope because that name is used in an enclosing local scope to define a local or parameter
    //         using (var name2 = new Class()) // 0136 on name2. 
    Diagnostic(ErrorCode.ERR_LocalIllegallyOverrides, "name2").WithArguments("name2").WithLocation(20, 20),
    // (17,21): warning CS0219: The variable 'name1' is assigned but its value is never used
    //                 int name1 = 2;  // 0136 on name1 
    Diagnostic(ErrorCode.WRN_UnreferencedVarAssg, "name1").WithArguments("name1").WithLocation(17, 21),
    // (22,17): warning CS0219: The variable 'name1' is assigned but its value is never used
    //             int name1 = 2;
    Diagnostic(ErrorCode.WRN_UnreferencedVarAssg, "name1").WithArguments("name1").WithLocation(22, 17),
    // (27,17): warning CS0219: The variable 'name1' is assigned but its value is never used
    //             int name1 = 2;
    Diagnostic(ErrorCode.WRN_UnreferencedVarAssg, "name1").WithArguments("name1").WithLocation(27, 17)
);
        }

        [Fact]
        public void TestCollisionInsideLock()
        {
            var source = @"
using System;
class Class
{
    const int[] name = null;
    void Method1()
    {
        lock (name)
        {
        }
        {
            lock (string.Empty)
            {
                const int name = 0; // 0135 because name above means 'this.name'.
                Console.WriteLine(name);
            }
        }
    }
}";
            CreateCompilationWithMscorlib(source).VerifyDiagnostics();
        }

        [Fact]
        public void TestCollisionInsideSwitch()
        {
            var source = @"
class Class
{
    int M() { return 1; }
    int name1 = 1;
    void Method1()
    {
        switch (name1)
        {
            case name1: break;                      // 0844: use of 'int name1' below before it is declared -- surprising error, but correct. Also notes that local name1 hides field.
            case 2:
                int name1 = 2;                      // 0135: because 'name1' above means 'this.name1'. Native compiler reports 0136, Roslyn reports 0135.
                var name2 = 3;
                System.Console.WriteLine(name1 + name2);
                break;
            case 3:
                name2 = M();                          // Not a use-before-declaration error; name2 is defined above
                var name2 = M();                      // 0128 on name2; switch sections share the same declaration space
                for (int name3 = 5; ; ) 
                { System.Console.WriteLine(name2 + name3); break; }
                int name4 = 6;
                System.Console.WriteLine(name4);
                break;
            case 4:
                name1 = 2;
                for (int name3 = 7; ; ) 
                { System.Console.WriteLine(name3); break; }
                switch (name1)
                {
                    case 1:
                        int name4 = 8, name5 = 9;   // 0136 on name4, name5
                        // Native compiler reports error 0136 on `name5 = 11` below; Roslyn reports it here.
                        System.Console.WriteLine(name4 + name5);
                        break;
                }
                for (int name6 = 10; ; ) // 0136 on name6; Native compiler reports 0136 on name6 below.
                { System.Console.WriteLine(name6);} 
            default:
                int name5 = 11, name6 = 12;   // Native compiler reports 0136 on name5 and name6. Roslyn reports them above.
                System.Console.WriteLine(name5 + name6);
                break;
        }
    }
}";
            CreateCompilationWithMscorlib(source).VerifyDiagnostics(
    // (10,18): error CS0844: Cannot use local variable 'name1' before it is declared. The declaration of the local variable hides the field 'Class.name1'.
    //             case name1: break;                      // 0844: use of 'int name1' below before it is declared -- surprising error, but correct. Also notes that local name1 hides field.
    Diagnostic(ErrorCode.ERR_VariableUsedBeforeDeclarationAndHidesField, "name1").WithArguments("name1", "Class.name1").WithLocation(10, 18),
    // (18,21): error CS0128: A local variable named 'name2' is already defined in this scope
    //                 var name2 = M();                      // 0128 on name2; switch sections share the same declaration space
    Diagnostic(ErrorCode.ERR_LocalDuplicate, "name2").WithArguments("name2").WithLocation(18, 21),
    // (31,29): error CS0136: A local or parameter named 'name4' cannot be declared in this scope because that name is used in an enclosing local scope to define a local or parameter
    //                         int name4 = 8, name5 = 9;   // 0136 on name4, name5
    Diagnostic(ErrorCode.ERR_LocalIllegallyOverrides, "name4").WithArguments("name4").WithLocation(31, 29),
    // (31,40): error CS0136: A local or parameter named 'name5' cannot be declared in this scope because that name is used in an enclosing local scope to define a local or parameter
    //                         int name4 = 8, name5 = 9;   // 0136 on name4, name5
    Diagnostic(ErrorCode.ERR_LocalIllegallyOverrides, "name5").WithArguments("name5").WithLocation(31, 40),
    // (36,26): error CS0136: A local or parameter named 'name6' cannot be declared in this scope because that name is used in an enclosing local scope to define a local or parameter
    //                 for (int name6 = 10; ; ) // 0136 on name6; Native compiler reports 0136 on name6 below.
    Diagnostic(ErrorCode.ERR_LocalIllegallyOverrides, "name6").WithArguments("name6").WithLocation(36, 26)
);
        }

        [Fact]
        public void TestCollisionInsideTryCatch_LegalCases()
        {
            var source = @"
using System;
class Derived : Base
{
    static long name1 = 1;
    static Derived()
    {
        {
            try
            {
                Console.WriteLine(name1);
            }
            catch (ArgumentException name1)
            {
                Console.WriteLine(name1.Message);
            }
            catch (Exception name1)
            {
                Console.WriteLine(name1.Message);
            }
        }
        {
            Console.WriteLine(name1);
            try
            {
                var name4 = string.Empty;
                try
                {
                    name2 = 3;
                    string name5 = string.Empty;
                    name5.ToString(); name4.ToString();
                }
                catch (Exception name2)
                {
                    Console.WriteLine(name2.Message);
                    string name5 = string.Empty;
                    name5.ToString(); name4.ToString();
                }
            }
            catch (Exception other)
            {
                var name4 = string.Empty;
                Console.WriteLine(name4.ToString());
                name2 = 4;
                Console.WriteLine(other.Message);
            }
        }
    }
}
class Base
{
    protected static long name2 = 5;
}";
            CompileAndVerify(source).VerifyDiagnostics();
        }

        [Fact]
        public void TestCollisionInsideTryCatch()
        {
            var source = @"
using System;
class Derived : Base
{
    static long name1 = 1;
    static Derived()
    {
        {
            Console.WriteLine(name1);
            try
            {
                Console.WriteLine(name1);
            }
            catch (ArgumentException name1)             
            {
                Console.WriteLine(name1.Message);
            }
            catch (Exception name2)                     
            {
                Console.WriteLine(name2.Message);
            }
            Console.WriteLine(name2);                   
        }
        {
            try
            {
                var name4 = string.Empty;             
                Console.WriteLine(name1);

            }
            catch (Exception name1)
            {
                System.Console.WriteLine(name1.Message);
                var name5 = string.Empty;
                try
                {
                }
                catch (Exception name1)         // 0136 on name1
                {
                    var name5 = string.Empty;   // 0136 on name5
                    System.Console.WriteLine(name1.Message);
                }
            }
            var name4 = string.Empty;  // Native reports 0136 here; 
        }
    }
}
class Base
{
    protected static long name2 = 2;
}";
            CreateCompilationWithMscorlib(source).VerifyDiagnostics(
    // (27,21): error CS0136: A local or parameter named 'name4' cannot be declared in this scope because that name is used in an enclosing local scope to define a local or parameter
    //                 var name4 = string.Empty;             // 0136: Roslyn reports this here; native reports it below.
    Diagnostic(ErrorCode.ERR_LocalIllegallyOverrides, "name4").WithArguments("name4").WithLocation(27, 21),
    // (38,34): error CS0136: A local or parameter named 'name1' cannot be declared in this scope because that name is used in an enclosing local scope to define a local or parameter
    //                 catch (Exception name1)         // 0136 on name1
    Diagnostic(ErrorCode.ERR_LocalIllegallyOverrides, "name1").WithArguments("name1").WithLocation(38, 34),
    // (40,25): error CS0136: A local or parameter named 'name5' cannot be declared in this scope because that name is used in an enclosing local scope to define a local or parameter
    //                     var name5 = string.Empty;   // 0136 on name5
    Diagnostic(ErrorCode.ERR_LocalIllegallyOverrides, "name5").WithArguments("name5").WithLocation(40, 25)
    );
        }

        [Fact]
        public void DifferentArities()
        {
            var source = @"
public class C<T>
{
    public static U G<U>(U x)
    {
        return x;
    }
    void M()
    {
        int G = 10;
        G<int>(G);
        int C = 5;
        C<string>.G(C);
    }
}";
            CompileAndVerify(source).VerifyDiagnostics();
        }

        [WorkItem(10556, "DevDiv_Projects/Roslyn")]
        [Fact]
        public void TestCollisionInsideQuery_LegalCases()
        {
            var source = @"
using System.Linq;
using System.Collections.Generic;
partial class Class
{
    private string Property
    {
        set
        {
            var other1 = from int name1 in ""string"" select name1;
            {
                var query1 = from name1 in ""string""
                             select name1;
                var query2 = from name1 in ""string""
                             select name1;
                this.Method(from name1 in ""string""
                            select name1).Method(from name1 in ""string""
                                                 select name1);
            }
            other1 = from int name1 in ""string"" select name2;
            {
                var query1 = from name1 in ""string""
                             let name2 = 1
                             select name1;
                var query2 = from name2 in ""string""
                             select name2;
                this.Method(from other2 in ""string""
                            from name2 in ""string""
                            select other2).Method(from name1 in ""string""
                                                  group name1 by name1 into name2
                                                  select name2);
                Method(from name1 in ""string""
                       group name1 by name1.ToString() into name1
                       select name1);
            }
        }
    }
    int name2 = 2;
    Class Method(IEnumerable<char> name1)
    {
        return null;
    }
    Class Method(object name1)
    {
        return null;
    }
}";
            CompileAndVerify(source, new[] { LinqAssemblyRef }).VerifyDiagnostics();
        }

        [WorkItem(543045, "DevDiv")]
        [Fact]
        public void TestCollisionInsideQuery()
        {
            var source = @"
using System;
using System.Linq;
using System.Collections.Generic;
partial class Class
{
    private string Property
    {
        set
        {
            Console.WriteLine(name1);
            {
                var query1 = from name1 in string.Empty   // 1931 -- UNDONE change to 0135 because name1 above refers to Class.name1
                             select name1;
                var query2 = from other in string.Empty
                             let name1 = 1              // 1931 -- UNDONE change to 0135 because name1 above refers to Class.name1
                             select other;
                this.Method(from other in string.Empty
                            from name1 in string.Empty    // 1931 -- UNDONE change to 0135 because name1 above refers to Class.name1   
                            select other).Method(from other in string.Empty
                                                 group other by other.ToString() into name1  // 1931 -- UNDONE change to 0135 because name1 above refers to Class.name1
                                                 select name1);
            }
            {
                var query1 = from name2 in string.Empty
                             let name2 = 2               // 1930
                             select name2;
                this.Method(from name2 in string.Empty
                            from name2 in name1.ToString()  // 1930
                            select name2);
                
            }
        }
    }
    int name1 = 3;
    Class Method(IEnumerable<char> name1)
    {
        return null;
    }
    Class Method(object name1)
    {
        return null;
    }
}";
            CreateCompilationWithMscorlibAndSystemCore(source).VerifyDiagnostics(
    // (26,34): error CS1930: The range variable 'name2' has already been declared
    //                              let name2 = 2               // 1930
    Diagnostic(ErrorCode.ERR_QueryDuplicateRangeVariable, "name2").WithArguments("name2").WithLocation(26, 34),
    // (29,34): error CS1930: The range variable 'name2' has already been declared
    //                             from name2 in name1.ToString()  // 1930
    Diagnostic(ErrorCode.ERR_QueryDuplicateRangeVariable, "name2").WithArguments("name2").WithLocation(29, 34)
                );
        }

        [Fact]
        public void TestCollisionInsideQuery_TypeParameter()
        {
            var source = @"
using System.Linq;
public class Class
{
    void Method<T, U>() 
    {
        {
            var q = from T in """" 
                    let U = T
                    select T;
        }
    }
}";
            CreateCompilationWithMscorlibAndSystemCore(source).VerifyDiagnostics(
                // (7,26): error CS1948: The range variable 'T' cannot have the same name as a method type parameter
                // var q = from T in "" 
                Diagnostic(ErrorCode.ERR_QueryRangeVariableSameAsTypeParam, "T").WithArguments("T"),
                // (8,25): error CS1948: The range variable 'U' cannot have the same name as a method type parameter
                // let U = T
                Diagnostic(ErrorCode.ERR_QueryRangeVariableSameAsTypeParam, "U").WithArguments("U"));
        }

        [WorkItem(542088, "DevDiv")]
        [Fact]
        public void LocalCollidesWithGenericType()
        {
            var source = @"
public class C
{
    public static int G<T>(int x)
    {
        return x;
    }
    public static void Main()
    {
        int G = 10;
        G<int>(G);
    }
}";
            CompileAndVerify(source).VerifyDiagnostics();
        }

        [WorkItem(542039, "DevDiv")]
        [Fact]
        public void BindingOrderCollisions01()
        {
            var source =
@"using System;

class A
{
    static long M(Action<long> act) { return 0; }
    static void What1()
    {
        int x1;
        {
            int y1 = M(x1 => { });
        }
    }
    static void What2()
    {
        {
            int y2 = M(x2 => { });
        }
        int x2;
    }
}";
            CreateCompilationWithMscorlib(source).VerifyDiagnostics(
                // (10,24): error CS0136: A local or parameter named 'x1' cannot be declared in this scope because
                // that name is used in an enclosing local scope to define a local or parameter int y1 = M(x1 => {
                //             });
                Diagnostic(ErrorCode.ERR_LocalIllegallyOverrides, "x1").WithArguments("x1"),
                // (10,22): error CS0266: Cannot implicitly convert type 'long' to 'int'. An explicit conversion exists (are you missing a cast?)
                //             int y1 = M(x1 => { });
                Diagnostic(ErrorCode.ERR_NoImplicitConvCast, "M(x1 => { })").WithArguments("long", "int"),
                // (8,13): warning CS0168: The variable 'x1' is declared but never used
                //         int x1;
                Diagnostic(ErrorCode.WRN_UnreferencedVar, "x1").WithArguments("x1"),
                // (16,22): error CS0266: Cannot implicitly convert type 'long' to 'int'. An explicit conversion exists (are you missing a cast?)
                //             int y2 = M(x2 => { });
                Diagnostic(ErrorCode.ERR_NoImplicitConvCast, "M(x2 => { })").WithArguments("long", "int"),
                // (16,24): error CS0136: A local or parameter named 'x2' cannot be declared in this scope because that name is used in an enclosing local scope to define a local or parameter
                //             int y2 = M(x2 => { });
                Diagnostic(ErrorCode.ERR_LocalIllegallyOverrides, "x2").WithArguments("x2"),
                // (18,13): warning CS0168: The variable 'x2' is declared but never used
                //         int x2;
                Diagnostic(ErrorCode.WRN_UnreferencedVar, "x2").WithArguments("x2")
                );
        }

        [WorkItem(542039, "DevDiv")]
        [Fact]
        public void BindingOrderCollisions02()
        {
            var source =
@"using System;

class A
{
    static double M(Action<double> act) { return 0; }
    static long M(Action<long> act) { return 0; }
    static void What1()
    {
        int x1;
        {
            int y1 = M(x1 => { });
        }
    }
    static void What2()
    {
        {
            int y2 = M(x2 => { });
        }
        int x2;
    }
}";
            CreateCompilationWithMscorlib(source).VerifyDiagnostics(
                // (11,24): error CS0136: A local or parameter named 'x1' cannot be declared in this scope because that name is used
                // in an enclosing local scope to define a local or parameter int y1 = M(x1 => { });
                Diagnostic(ErrorCode.ERR_LocalIllegallyOverrides, "x1").WithArguments("x1"),
                // (11,22): error CS0121: The call is ambiguous between the following methods or properties: 'A.M(System.Action<double>)' and 'A.M(System.Action<long>)'
                //             int y1 = M(x1 => { });
                Diagnostic(ErrorCode.ERR_AmbigCall, "M").WithArguments("A.M(System.Action<double>)", "A.M(System.Action<long>)"),
                // (9,13): warning CS0168: The variable 'x1' is declared but never used
                //         int x1;
                Diagnostic(ErrorCode.WRN_UnreferencedVar, "x1").WithArguments("x1"),
                // (17,22): error CS0121: The call is ambiguous between the following methods or properties: 'A.M(System.Action<double>)' and 'A.M(System.Action<long>)'
                //             int y2 = M(x2 => { });
                Diagnostic(ErrorCode.ERR_AmbigCall, "M").WithArguments("A.M(System.Action<double>)", "A.M(System.Action<long>)"),
                // (17,24): error CS0136: A local or parameter named 'x2' cannot be declared in this scope because that name is used in an enclosing local scope to define a local or parameter
                //             int y2 = M(x2 => { });
                Diagnostic(ErrorCode.ERR_LocalIllegallyOverrides, "x2").WithArguments("x2"),
                // (19,13): warning CS0168: The variable 'x2' is declared but never used
                //         int x2;
                Diagnostic(ErrorCode.WRN_UnreferencedVar, "x2").WithArguments("x2")
                );
        }

        [WorkItem(542039, "DevDiv")]
        [Fact]
        public void BindingOrderCollisions03()
        {
            var source =
@"using System;

class Outer
{
    static void Main(string[] args)
    {
    }

    public static int M() { return 1; }
    class Inner
    {
        public static int M = 2;

        void F1()
        {
            int x1 = M;
            Action a = () => { int x2 = M(); };
        }

        void F2()
        {
            Action a = () => { int x2 = M(); };
            int x1 = M;
        }

        void F3()
        {
            int x1 = M();
            Action a = () => { int x2 = M; };
        }

        void F4()
        {
            Action a = () => { int x2 = M; };
            int x1 = M();
        }
    }
}";
            CreateCompilationWithMscorlib(source).VerifyDiagnostics();
        }

        [WorkItem(835569, "DevDiv")]
        [Fact]
        public void CollisionWithSameWhenError()
        {
            var source = @"
using System;
using System.Reflection;

class Program
{
    static void Main()
    {
        Console.WriteLine(string.Join < Assembly(Environment.NewLine, Assembly.GetEntryAssembly().GetReferencedAssemblies()));
    }
}
";
            CreateCompilationWithMscorlib(source).VerifyDiagnostics(
                Diagnostic(ErrorCode.ERR_NonInvocableMemberCalled, "Assembly").WithArguments("System.Reflection.Assembly").WithLocation(9, 41)
                );
        }

        [Fact(Skip = "https://roslyn.codeplex.com/workitem/450")]
        [WorkItem(879811, "DevDiv")]
        public void Bug879811_1()
        {
            const string source = @"
using static Static<string>;
using static Static<int>;
 
public static class Static<T>
{
    public class Nested
    {
        public void M() { }
    }
}
 
class D
{
    static void Main(string[] args)
    {
        var c = new Nested();
        c.M();
    }
}";
            CreateCompilationWithMscorlib(source).VerifyDiagnostics(
                // (17,21): error CS0104: 'Nested' is an ambiguous reference between 'Static<int>.Nested' and 'Static<string>.Nested'
                //         var c = new Nested();
                Diagnostic(ErrorCode.ERR_AmbigContext, "Nested").WithArguments("Nested", "Static<int>.Nested", "Static<string>.Nested").WithLocation(17, 21));
        }

        [Fact]
        [WorkItem(879811, "DevDiv")]
        public void Bug879811_2()
        {
            const string source = @"
using static Static<string>;
using static Static<System.String>;
 
public static class Static<T>
{
    public class Nested
    {
        public void M() { }
    }
}
 
class D
{
    static void Main()
    {
        var c = new Nested();
    }
}";
            CreateCompilationWithMscorlib(source).VerifyDiagnostics(
                // (3,7): warning CS0105: The using directive for 'Static<string>' appeared previously in this namespace
                // using Static<System.String>;
                Diagnostic(ErrorCode.WRN_DuplicateUsing, "Static<System.String>").WithArguments("Static<string>").WithLocation(3, 14),
                // (3,1): hidden CS8019: Unnecessary using directive.
                // using Static<System.String>;
                Diagnostic(ErrorCode.HDN_UnusedUsingDirective, "using static Static<System.String>;").WithLocation(3, 1));
        }

        [Fact]
        [WorkItem(879811, "DevDiv")]
        public void Bug879811_3()
        {
            const string source = @"
using static Static<string>;

public static class Static<T>
{
    public class Nested
    {
        public void M() { }
    }
}

namespace N
{
    using static Static<int>;
    class D
    {
        static void Main()
        {
            Static<int>.Nested c = new Nested();
        }
    }
}";
            CreateCompilationWithMscorlib(source).VerifyDiagnostics(
                // (2,1): hidden CS8019: Unnecessary using directive.
                // using Static<string>;
                Diagnostic(ErrorCode.HDN_UnusedUsingDirective, "using static Static<string>;").WithLocation(2, 1));
        }
    }
}
