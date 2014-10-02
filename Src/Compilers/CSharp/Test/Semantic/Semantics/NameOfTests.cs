// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.CSharp.Test.Utilities;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.CSharp.UnitTests
{
    public class NameofTests : CSharpTestBase
    {
        [Fact]
        public void TestGoodNameofInstances()
        {
            var source = @"
using System;
using System.Collections.Generic;
using alias1 = System;
using alias2 = System.Collections.Generic;
namespace Source
{
    class Class { }
    struct Struct { }
    enum Enum { e }
    interface Interface { }
    class StaticClass { }
}

class Test
{
    public int instanceVar;
    private static int privateVar;
}
class TestGeneric<T>
{
    public class NestedGeneric<T> { }
}

class Program
{
    string var1;
    static byte @static;
    char nameof;
    event EventHandler Event;
    delegate void DelegateExample(object sender, EventArgs e);

    static void Main(string[] args)
    {
        // (1) identifier
        int var2;
        Console.WriteLine(nameof(var2));
        Console.WriteLine(nameof(nameof));
        Console.WriteLine(nameof(var1));
        Console.WriteLine(nameof(@static));
        Console.WriteLine(nameof(args));
        Console.WriteLine(nameof(Program));
        Console.WriteLine(nameof(Event));
        Console.WriteLine(nameof(DelegateExample));
        Console.WriteLine(nameof(Source));

        // (1.1) from metadata
        Console.WriteLine(nameof(IFormattable));
        Console.WriteLine(nameof(Math));


        // (2) unbound-type-name . identifier

        // (2.1) identifier . identifier
        Console.WriteLine(nameof(Source.Class));
        Console.WriteLine(nameof(Source.Struct));
        Console.WriteLine(nameof(Source.Enum));
        Console.WriteLine(nameof(Source.Interface));
        Console.WriteLine(nameof(Source.StaticClass));
        Console.WriteLine(nameof(Program.Main));
        Console.WriteLine(nameof(System.Byte));
        Console.WriteLine(nameof(System.Int64));

        // (2.2) generic name . identifier
        Console.WriteLine(nameof(List<>.Equals));
        Console.WriteLine(nameof(Dictionary<,>.Add));
        Console.WriteLine(nameof(List<>.Enumerator));

        // (2.3) qualified name . identifier
        Console.WriteLine(nameof(TestGeneric<>.NestedGeneric<>.Equals));
        Console.WriteLine(nameof(global::Test.instanceVar));
        Console.WriteLine(nameof(System.IO.FileMode));
        Console.WriteLine(nameof(System.Collections.Generic.List));
        Console.WriteLine(nameof(alias1::Collections.Generic.List<>.Add));

        // (2.4) accessing instance members of other classes
        Console.WriteLine(nameof(Test.instanceVar));

        // (2.5) ambiguous members
        Console.WriteLine(nameof(E.D));
        Console.WriteLine(nameof(E.D.C));


        // (3) identifier :: identifier
        Console.WriteLine(nameof(alias2::List));
        Console.WriteLine(nameof(global::Microsoft));

        // postfix
        Console.WriteLine(nameof(System)[0]);
    }
}

class A
{
    public class B
    {
        public class C { }
    }

    public class D : B
    {
        new public const int C = 0;
    }
}

class E : A
{
    new public const int D = 0;
}

interface I1
{
    int M();
    int N { get; }
}
interface I2
{
    int M<T>();
    int N { get; }
}
interface I3 : I1, I2
{
    // testing ambigous
    int Test(string arg = nameof(M), string arg2 = nameof(N));
}
";
            var comp = CompileAndVerify(source, expectedOutput: @"
var2
nameof
var1
static
args
Program
Event
DelegateExample
Source
IFormattable
Math
Class
Struct
Enum
Interface
StaticClass
Main
Byte
Int64
Equals
Add
Enumerator
Equals
instanceVar
FileMode
List
Add
instanceVar
D
C
List
Microsoft
S");

        }

        [Fact]
        public void TestBadNameofInstances()
        {
            var source = @"
using System;
class Program
{
    static void Main(string[] args)
    {
        string s;
        // identifier expected errors
        s = nameof(System.Action<>);
        s = nameof(int);
        s = nameof(void);

        // unexpected unbound errors
        s = nameof(List<int>.Enumerator);
        s = nameof(System.Collections.Generic.Dictionary<Program,>.KeyCollection);
        s = nameof(global::System.Collections.Generic.List<string>.Enumerator);
        s = nameof(Test<Object>.s);

        // does not exist in current context
        s = nameof(nameof);
        s = nameof(Program.s2);
        s = nameof(Object.Something);
        s = nameof(global::Something);
        s = nameof(global2::Something);
        s = nameof(System.Collections2.Generic.List);
        s = nameof(List2<>.Add);

        // other type of errors
        s = nameof(Test<>.s); // inaccessible
        s = nameof(b); // cannot use before declaration
        int b;

    }
    void ParsedAsInvocation()
    {
        string s;
        // parsed as invocation expression 
        s = nameof();
        s = nameof(this);
        s = nameof(this.ParsedAsInvocation);
        s = nameof(int.ToString);
        s = nameof(typeof(string));
        string[] a = null;
        s = nameof(a[4].Equals);
    }
}
class Test<T>
{
    static string s;
}";
            var option = TestOptions.ReleaseExe.WithWarningLevel(0);
            CreateCompilationWithMscorlib(source, options: option).VerifyDiagnostics(
                // (11,20): error CS1026: ) expected
                //         s = nameof(void);
                Diagnostic(ErrorCode.ERR_CloseParenExpected, "void").WithLocation(11, 20),
                // (11,20): error CS1002: ; expected
                //         s = nameof(void);
                Diagnostic(ErrorCode.ERR_SemicolonExpected, "void").WithLocation(11, 20),
                // (11,20): error CS1547: Keyword 'void' cannot be used in this context
                //         s = nameof(void);
                Diagnostic(ErrorCode.ERR_NoVoidHere, "void").WithLocation(11, 20),
                // (11,24): error CS1001: Identifier expected
                //         s = nameof(void);
                Diagnostic(ErrorCode.ERR_IdentifierExpected, ")").WithLocation(11, 24),
                // (11,24): error CS1002: ; expected
                //         s = nameof(void);
                Diagnostic(ErrorCode.ERR_SemicolonExpected, ")").WithLocation(11, 24),
                // (11,24): error CS1513: } expected
                //         s = nameof(void);
                Diagnostic(ErrorCode.ERR_RbraceExpected, ")").WithLocation(11, 24),
                // (15,66): error CS1031: Type expected
                //         s = nameof(System.Collections.Generic.Dictionary<Program,>.KeyCollection);
                Diagnostic(ErrorCode.ERR_TypeExpected, ">").WithLocation(15, 66),
                // (9,27): error CS1001: Identifier expected
                //         s = nameof(System.Action<>);
                Diagnostic(ErrorCode.ERR_IdentifierExpected, "Action<>").WithLocation(9, 27),
                // (10,20): error CS1001: Identifier expected
                //         s = nameof(int);
                Diagnostic(ErrorCode.ERR_IdentifierExpected, "int").WithLocation(10, 20),
                // (11,13): error CS0103: The name 'nameof' does not exist in the current context
                //         s = nameof(void);
                Diagnostic(ErrorCode.ERR_NameNotInContext, "nameof").WithArguments("nameof").WithLocation(11, 13),
                // (14,25): error CS8071: Type arguments are not allowed in the nameof operator.
                //         s = nameof(List<int>.Enumerator);
                Diagnostic(ErrorCode.ERR_UnexpectedBoundGenericName, "int").WithLocation(14, 25),
                // (15,13): error CS0103: The name 'nameof' does not exist in the current context
                //         s = nameof(System.Collections.Generic.Dictionary<Program,>.KeyCollection);
                Diagnostic(ErrorCode.ERR_NameNotInContext, "nameof").WithArguments("nameof").WithLocation(15, 13),
                // (16,60): error CS8071: Type arguments are not allowed in the nameof operator.
                //         s = nameof(global::System.Collections.Generic.List<string>.Enumerator);
                Diagnostic(ErrorCode.ERR_UnexpectedBoundGenericName, "string").WithLocation(16, 60),
                // (17,25): error CS8071: Type arguments are not allowed in the nameof operator.
                //         s = nameof(Test<Object>.s);
                Diagnostic(ErrorCode.ERR_UnexpectedBoundGenericName, "Object").WithLocation(17, 25),
                // (20,20): error CS0246: The type or namespace name 'nameof' could not be found (are you missing a using directive or an assembly reference?)
                //         s = nameof(nameof);
                Diagnostic(ErrorCode.ERR_SingleTypeNameNotFound, "nameof").WithArguments("nameof").WithLocation(20, 20),
                // (21,28): error CS0426: The type name 's2' does not exist in the type 'Program'
                //         s = nameof(Program.s2);
                Diagnostic(ErrorCode.ERR_DottedTypeNameNotFoundInAgg, "s2").WithArguments("s2", "Program").WithLocation(21, 28),
                // (22,27): error CS0426: The type name 'Something' does not exist in the type 'object'
                //         s = nameof(Object.Something);
                Diagnostic(ErrorCode.ERR_DottedTypeNameNotFoundInAgg, "Something").WithArguments("Something", "object").WithLocation(22, 27),
                // (23,28): error CS0400: The type or namespace name 'Something' could not be found in the global namespace (are you missing an assembly reference?)
                //         s = nameof(global::Something);
                Diagnostic(ErrorCode.ERR_GlobalSingleTypeNameNotFound, "Something").WithArguments("Something", "<global namespace>").WithLocation(23, 28),
                // (24,20): error CS0432: Alias 'global2' not found
                //         s = nameof(global2::Something);
                Diagnostic(ErrorCode.ERR_AliasNotFound, "global2").WithArguments("global2").WithLocation(24, 20),
                // (25,27): error CS0234: The type or namespace name 'Collections2' does not exist in the namespace 'System' (are you missing an assembly reference?)
                //         s = nameof(System.Collections2.Generic.List);
                Diagnostic(ErrorCode.ERR_DottedTypeNameNotFoundInNS, "Collections2").WithArguments("Collections2", "System").WithLocation(25, 27),
                // (26,20): error CS0246: The type or namespace name 'List2<>' could not be found (are you missing a using directive or an assembly reference?)
                //         s = nameof(List2<>.Add);
                Diagnostic(ErrorCode.ERR_SingleTypeNameNotFound, "List2<>").WithArguments("List2<>").WithLocation(26, 20),
                // (29,27): error CS0122: 'Test<T>.s' is inaccessible due to its protection level
                //         s = nameof(Test<>.s); // inaccessible
                Diagnostic(ErrorCode.ERR_BadAccess, "s").WithArguments("Test<T>.s").WithLocation(29, 27),
                // (30,20): error CS0841: Cannot use local variable 'b' before it is declared
                //         s = nameof(b); // cannot use before declaration
                Diagnostic(ErrorCode.ERR_VariableUsedBeforeDeclaration, "b").WithArguments("b").WithLocation(30, 20),
                // (38,13): error CS0103: The name 'nameof' does not exist in the current context
                //         s = nameof();
                Diagnostic(ErrorCode.ERR_NameNotInContext, "nameof").WithArguments("nameof").WithLocation(38, 13),
                // (39,13): error CS0103: The name 'nameof' does not exist in the current context
                //         s = nameof(this);
                Diagnostic(ErrorCode.ERR_NameNotInContext, "nameof").WithArguments("nameof").WithLocation(39, 13),
                // (40,13): error CS0103: The name 'nameof' does not exist in the current context
                //         s = nameof(this.ParsedAsInvocation);
                Diagnostic(ErrorCode.ERR_NameNotInContext, "nameof").WithArguments("nameof").WithLocation(40, 13),
                // (41,13): error CS0103: The name 'nameof' does not exist in the current context
                //         s = nameof(int.ToString);
                Diagnostic(ErrorCode.ERR_NameNotInContext, "nameof").WithArguments("nameof").WithLocation(41, 13),
                // (42,13): error CS0103: The name 'nameof' does not exist in the current context
                //         s = nameof(typeof(string));
                Diagnostic(ErrorCode.ERR_NameNotInContext, "nameof").WithArguments("nameof").WithLocation(42, 13),
                // (44,13): error CS0103: The name 'nameof' does not exist in the current context
                //         s = nameof(a[4].Equals);
                Diagnostic(ErrorCode.ERR_NameNotInContext, "nameof").WithArguments("nameof").WithLocation(44, 13)
            );
        }

        [Fact]
        public void TestWhenNameofOperatorBinds()
        {
            var source = @"
using System;
class Class
{
    public static int var;
}
class NameofField
{
    static string nameof;
    static void Main(string[] args)
    {
        Console.WriteLine(nameof(Class.var));
    }
}
class NameofTypeParameter<nameof>
{
    static void test()
    {
        Console.WriteLine(nameof(Class.var));
    }
}
class NameofLabel
{
    static void test()
    {
    nameof:
        Console.WriteLine(nameof(Class.var));
        goto nameof;
    }
}
class NameofDelegate
{
    public delegate void nameof(object sender, EventArgs e);
    static void test()
    {
        Console.WriteLine(nameof(Class.var));
    }
}
class PrivateNameofMethodInSuperClass : Super1
{
    static void test()
    {
        Console.WriteLine(nameof(Class.var));
    }
}
class Super1
{
    private static void nameof() { }
}
class NameofProperty
{
    public int nameof
    {
        get { return 3; }
    }
    void test()
    {
        Console.WriteLine(nameof(Class.var));
    }
}
class NameofDynamic
{
    dynamic nameof;
    void test()
    {
        Console.WriteLine(nameof(Class.var));
    }
}
class NameofEvent
{
    event Action nameof { add { } remove { } }
    void test()
    {
        nameof(Class.var);
    }
}
class NameofParameter
{
    static void test(string nameof)
    {
        Console.WriteLine(nameof(Class.var));
    }
}
class NameofLocal
{
    static void test()
    {
        string nameof = ""naber"";
        Console.WriteLine(nameof(Class.var));
        }
    }
    class NameofMethod
    {
        static void test()
        {
            Console.WriteLine(nameof(Class.var));
        }
        int nameof() { return 3; }
    }
    class NameofMethodInSuperClass : Super2
    {
        static void test()
        {
            Console.WriteLine(nameof(Class.var));
        }
    }
    public class Super2
    {
        public static int nameof() { return 3; }
    }
";
            MetadataReference[] references = new[] { SystemCoreRef, CSharpRef };
            var option = TestOptions.ReleaseExe.WithWarningLevel(0);
            CreateCompilationWithMscorlib45(source, references, options: option).VerifyDiagnostics(
                // (104,31): error CS1501: No overload for method 'nameof' takes 1 arguments
                //             Console.WriteLine(nameof(Class.var));
                Diagnostic(ErrorCode.ERR_BadArgCount, "nameof").WithArguments("nameof", "1").WithLocation(104, 31),
                // (74,9): error CS0079: The event 'NameofEvent.nameof' can only appear on the left hand side of += or -=
                //         nameof(Class.var);
                Diagnostic(ErrorCode.ERR_BadEventUsageNoField, "nameof").WithArguments("NameofEvent.nameof").WithLocation(74, 9),
                // (74,9): error CS1593: Delegate 'System.Action' does not take 1 arguments
                //         nameof(Class.var);
                Diagnostic(ErrorCode.ERR_BadDelArgCount, "nameof").WithArguments("System.Action", "1").WithLocation(74, 9),
                // (81,27): error CS0149: Method name expected
                //         Console.WriteLine(nameof(Class.var));
                Diagnostic(ErrorCode.ERR_MethodNameExpected, "nameof").WithLocation(81, 27),
                // (96,31): error CS1501: No overload for method 'nameof' takes 1 arguments
                //             Console.WriteLine(nameof(Class.var));
                Diagnostic(ErrorCode.ERR_BadArgCount, "nameof").WithArguments("nameof", "1").WithLocation(96, 31),
                // (89,27): error CS0149: Method name expected
                //         Console.WriteLine(nameof(Class.var));
                Diagnostic(ErrorCode.ERR_MethodNameExpected, "nameof").WithLocation(89, 27)
            );
        }

        [Fact]
        public void TestNameofDifferentContexts()
        {
            var source = @"
using System;
using System.Collections.Generic;
using System.Linq;
namespace Source
{
    class Class { }
    struct Struct { }
    enum Enum { e }
    interface Interface { }
    class StaticClass { }
}
class Program
{
    // field initializer
    string className = nameof(global::Program);
    string temp;

    // in getter and setter
    public string EntryMethodName { get { return nameof(Program.Main); } set { temp = nameof(Program.Main); } }

    static void Main(string[] args)
    {
        // array initializer
        string[] values = { nameof(Source.Enum), nameof(Source.Interface), nameof(Source.Struct), nameof(Source.Class) };
        // as an argument
        TestParameter(nameof(EntryMethodName));

        // switch argument
        switch (nameof(Dictionary<,>.Add))
        {
            // case expression
            case nameof(List<>.Equals):
                // goto case
                goto case nameof(Dictionary<,>.Add);
                break;
            case nameof(List<>.Add):
                Console.WriteLine(""Correct"");
                break;
        }

        // in query expression
        string result = (from value in values where value == nameof(Source.Struct) select value).First();
        // as a range variable
        var s = (from value in values let name = nameof(Source.Enum) select new { value, name });
        // in if condition
        if (values[0] == nameof(Source.Enum) && result == nameof(Source.Struct))
            Console.WriteLine(""Correct"");
    }
    // default parameter value
    static void TestParameter(string arg = nameof(Program.TestParameter))
    {
        Console.WriteLine(arg);
    }

    // in attribute with string concatenation 
    [Obsolete(""Please do not use this method: "" + nameof(Program.Old), true)]
    static void Old() { }
}";
            CompileAndVerify(source, new[] { LinqAssemblyRef }, expectedOutput: @"
EntryMethodName
Correct
Correct");

        }

        [Fact]
        public void TestNameofLowerLangVersion()
        {
            var comp = CreateCompilationWithMscorlib(@"
class Program
{
    Program(string s = nameof(global::Program))
    { }
}
", parseOptions: TestOptions.Regular.WithLanguageVersion(LanguageVersion.CSharp5));

            comp.VerifyDiagnostics(
                // (4,24): error CS8026: Feature 'nameof operator' is not available in C# 5.  Please use language version 6 or greater.
                //     Program(string s = nameof(global::Program))
                Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion5, "nameof(global::Program)").WithArguments("nameof operator", "6").WithLocation(4, 24));
        }

        [Fact]
        public void TestNameofIndexerName()
        {
            var source = @"
class C
{
    public static void Main(string[] args)
    {
        var t = typeof(C);
        foreach (var m in t.GetMethods())
        {
            System.Console.WriteLine(m.Name);
        }
    }
    public int Other(int index) { return 0; }
    [System.Runtime.CompilerServices.IndexerName(""_"" + nameof(Other))]
    public int this[int index]
    {
        get { return 0; }
    }
}";
            CompileAndVerify(source, expectedOutput: @"Main
Other
get__Other
ToString
Equals
GetHashCode
GetType");
        }

        [Fact]
        public void TestNameofAliasMember()
        {
            var source = @"
using System;
using SCGL = System.Collections.Generic.List<int>;
class C
{
    public static void Main(string[] args)
    {
        System.Console.WriteLine(nameof(SCGL.Contains));
    }
}";
            CompileAndVerify(source, expectedOutput: @"Contains");
        }

        [Fact, WorkItem(1013334, "DevDiv")]
        public void TestCompatStatementExpressionInvocation()
        {
            var source = @"
using System;
class Program
{
    static void nameof(object o)
    {
        Console.WriteLine(o);
    }
    static int N = 12;
    static void Main(string[] args)
    {
        nameof(N);
    }
}";
            var compilation = CreateCompilationWithMscorlib(
                source,
                options: TestOptions.DebugExe,
                parseOptions: TestOptions.Regular.WithLanguageVersion(LanguageVersion.CSharp5));
            CompileAndVerify(compilation, expectedOutput: @"12");
        }

        [Fact, WorkItem(1013334, "DevDiv")]
        public void TestCompatStatementExpressionInvocation02()
        {
            var source = @"
using System;
class Program
{
    static void nameof(object o)
    {
        Console.WriteLine(o);
    }
    static int N = 12;
    static void Main(string[] args)
    {
        nameof(N);
    }
}";
            var compilation = CreateCompilationWithMscorlib(
                source,
                options: TestOptions.DebugExe,
                parseOptions: TestOptions.Regular.WithLanguageVersion(LanguageVersion.CSharp6));
            CompileAndVerify(compilation, expectedOutput: @"12");
        }

        [Fact, WorkItem(1013334, "DevDiv")]
        public void TestCompatStatementExpressionInvocation03()
        {
            var source = @"
class Program
{
    const int N = 12;
    static void Main(string[] args)
    {
        nameof(N);
    }
}";
            var compilation = CreateCompilationWithMscorlib(
                source,
                options: TestOptions.DebugExe,
                parseOptions: TestOptions.Regular.WithLanguageVersion(LanguageVersion.CSharp6)).VerifyDiagnostics(
                    // (7,9): error CS0201: Only assignment, call, increment, decrement, and new object expressions can be used as a statement
                    //         nameof(N);
                    Diagnostic(ErrorCode.ERR_IllegalStatement, "nameof(N)").WithLocation(7, 9)
                );
        }

        [Fact, WorkItem(1013334, "DevDiv")]
        public void TestCompatStatementExpressionInvocation04()
        {
            var source = @"
class Program
{
    const int N = 12;
    static void Main(string[] args)
    {
        nameof(N);
    }
}";
            var compilation = CreateCompilationWithMscorlib(
                source,
                options: TestOptions.DebugExe,
                parseOptions: TestOptions.Regular.WithLanguageVersion(LanguageVersion.CSharp5)).VerifyDiagnostics(
                    // (7,9): error CS8026: Feature 'nameof operator' is not available in C# 5.  Please use language version 6 or greater.
                    //         nameof(N);
                    Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion5, "nameof(N)").WithArguments("nameof operator", "6").WithLocation(7, 9),
                    // (7,9): error CS0201: Only assignment, call, increment, decrement, and new object expressions can be used as a statement
                    //         nameof(N);
                    Diagnostic(ErrorCode.ERR_IllegalStatement, "nameof(N)").WithLocation(7, 9)
                );
        }

    }
}
