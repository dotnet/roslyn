// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.CSharp.Test.Utilities;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.CSharp.UnitTests
{
    public sealed class NameofTests : CSharpTestBase
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
    public class NestedGeneric<T1> { }
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
        Console.WriteLine(nameof(List<int>.Equals));
        Console.WriteLine(nameof(Dictionary<int,int>.Add));
        Console.WriteLine(nameof(List<int>.Enumerator));

        // (2.3) qualified name . identifier
        Console.WriteLine(nameof(TestGeneric<int>.NestedGeneric<int>.Equals));
        Console.WriteLine(nameof(global::Test.instanceVar));
        Console.WriteLine(nameof(System.IO.FileMode));
        Console.WriteLine(nameof(System.Collections.Generic.List<int>));
        Console.WriteLine(nameof(alias1::Collections.Generic.List<int>.Add));

        // (2.4) accessing instance members of other classes
        Console.WriteLine(nameof(Test.instanceVar));

        // (2.5) members that hide
        Console.WriteLine(nameof(E.D));
        Console.WriteLine(nameof(A.D.C));

        ////// (3) identifier :: identifier
        ////Console.WriteLine(nameof(alias2::List<int>));
        ////Console.WriteLine(nameof(global::Microsoft));

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
    // testing ambiguous
    int Test(string arg = nameof(M), string arg2 = ""N"" /* nameof(N) */);
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
S");
        }

        [Fact]
        public void TestBadNameofInstances()
        {
            var source = @"
using System;
using System.Linq;

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
        s = nameof(global::Program); // not an expression
        s = nameof(Test<>.s); // inaccessible
        s = nameof(b); // cannot use before declaration
        //s = nameof(System.Collections.Generic.List<int>.Select); // extension methods are now candidates for nameof
        s = nameof(System.Linq.Enumerable.Select<int, int>); // type parameters not allowed on method group in nameof
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
            CreateCompilationWithMscorlib40AndSystemCore(source, options: option).VerifyDiagnostics(
                // (12,20): error CS1525: Invalid expression term 'int'
                //         s = nameof(int);
                Diagnostic(ErrorCode.ERR_InvalidExprTerm, "int").WithArguments("int").WithLocation(12, 20),
                // (13,20): error CS1525: Invalid expression term 'void'
                //         s = nameof(void);
                Diagnostic(ErrorCode.ERR_InvalidExprTerm, "void").WithArguments("void").WithLocation(13, 20),
                // (17,66): error CS1031: Type expected
                //         s = nameof(System.Collections.Generic.Dictionary<Program,>.KeyCollection);
                Diagnostic(ErrorCode.ERR_TypeExpected, ">").WithLocation(17, 66),
                // (16,20): error CS0103: The name 'List' does not exist in the current context
                //         s = nameof(List<int>.Enumerator);
                Diagnostic(ErrorCode.ERR_NameNotInContext, "List<int>").WithArguments("List").WithLocation(16, 20),
                // (19,33): error CS0122: 'Test<object>.s' is inaccessible due to its protection level
                //         s = nameof(Test<Object>.s);
                Diagnostic(ErrorCode.ERR_BadAccess, "s").WithArguments("Test<object>.s").WithLocation(19, 33),
                // (22,20): error CS0103: The name 'nameof' does not exist in the current context
                //         s = nameof(nameof);
                Diagnostic(ErrorCode.ERR_NameNotInContext, "nameof").WithArguments("nameof").WithLocation(22, 20),
                // (23,28): error CS0117: 'Program' does not contain a definition for 's2'
                //         s = nameof(Program.s2);
                Diagnostic(ErrorCode.ERR_NoSuchMember, "s2").WithArguments("Program", "s2").WithLocation(23, 28),
                // (24,27): error CS0117: 'object' does not contain a definition for 'Something'
                //         s = nameof(Object.Something);
                Diagnostic(ErrorCode.ERR_NoSuchMember, "Something").WithArguments("object", "Something").WithLocation(24, 27),
                // (25,28): error CS0400: The type or namespace name 'Something' could not be found in the global namespace (are you missing an assembly reference?)
                //         s = nameof(global::Something);
                Diagnostic(ErrorCode.ERR_GlobalSingleTypeNameNotFound, "Something").WithArguments("Something").WithLocation(25, 28),
                // (26,20): error CS0432: Alias 'global2' not found
                //         s = nameof(global2::Something);
                Diagnostic(ErrorCode.ERR_AliasNotFound, "global2").WithArguments("global2").WithLocation(26, 20),
                // (27,20): error CS0234: The type or namespace name 'Collections2' does not exist in the namespace 'System' (are you missing an assembly reference?)
                //         s = nameof(System.Collections2.Generic.List);
                Diagnostic(ErrorCode.ERR_DottedTypeNameNotFoundInNS, "System.Collections2").WithArguments("Collections2", "System").WithLocation(27, 20),
                // (28,20): error CS0103: The name 'List2' does not exist in the current context
                //         s = nameof(List2<>.Add);
                Diagnostic(ErrorCode.ERR_NameNotInContext, "List2<>").WithArguments("List2").WithLocation(28, 20),
                // (31,20): error CS8083: An alias-qualified name is not an expression.
                //         s = nameof(global::Program); // not an expression
                Diagnostic(ErrorCode.ERR_AliasQualifiedNameNotAnExpression, "global::Program").WithLocation(31, 20),
                // (32,27): error CS0122: 'Test<T>.s' is inaccessible due to its protection level
                //         s = nameof(Test<>.s); // inaccessible
                Diagnostic(ErrorCode.ERR_BadAccess, "s").WithArguments("Test<T>.s").WithLocation(32, 27),
                // (33,20): error CS0841: Cannot use local variable 'b' before it is declared
                //         s = nameof(b); // cannot use before declaration
                Diagnostic(ErrorCode.ERR_VariableUsedBeforeDeclaration, "b").WithArguments("b").WithLocation(33, 20),
                // (35,20): error CS8084: Type parameters are not allowed on a method group as an argument to 'nameof'.
                //         s = nameof(System.Linq.Enumerable.Select<int, int>); // type parameters not allowed on method group in nameof
                Diagnostic(ErrorCode.ERR_NameofMethodGroupWithTypeParameters, "System.Linq.Enumerable.Select<int, int>").WithLocation(35, 20),
                // (43,13): error CS0103: The name 'nameof' does not exist in the current context
                //         s = nameof();
                Diagnostic(ErrorCode.ERR_NameNotInContext, "nameof").WithArguments("nameof").WithLocation(43, 13),
                // (44,20): error CS8081: Expression does not have a name.
                //         s = nameof(this);
                Diagnostic(ErrorCode.ERR_ExpressionHasNoName, "this").WithLocation(44, 20),
                // (47,20): error CS8081: Expression does not have a name.
                //         s = nameof(typeof(string));
                Diagnostic(ErrorCode.ERR_ExpressionHasNoName, "typeof(string)").WithLocation(47, 20),
                // (49,20): error CS8082: Sub-expression cannot be used in an argument to nameof.
                //         s = nameof(a[4].Equals);
                Diagnostic(ErrorCode.ERR_SubexpressionNotInNameof, "a[4]").WithLocation(49, 20)
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
            CreateCompilationWithMscorlib461(source, references, options: option).VerifyDiagnostics(
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
    string className = nameof(Program);
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
        switch (nameof(Dictionary<int,int>.Add))
        {
            // case expression
            case nameof(List<int>.Equals):
                // goto case
                goto case nameof(Dictionary<int,int>.Add);
                break;
            case nameof(List<int>.Add):
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
            CompileAndVerify(source, expectedOutput: @"
EntryMethodName
Correct
Correct");
        }

        [Fact]
        public void TestNameofLowerLangVersion()
        {
            var comp = CreateCompilation(@"
class Program
{
    Program(string s = nameof(Program))
    { }
}
", parseOptions: TestOptions.Regular.WithLanguageVersion(LanguageVersion.CSharp5));

            comp.VerifyDiagnostics(
                // (4,24): error CS8026: Feature 'nameof operator' is not available in C# 5. Please use language version 6 or greater.
                //     Program(string s = nameof(Program))
                Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion5, "nameof(Program)").WithArguments("nameof operator", "6").WithLocation(4, 24)
                );
        }

        [Fact]
        [WorkItem(33564, "https://github.com/dotnet/roslyn/issues/33564")]
        public void TestNameofIndexerName()
        {
            var source = @"
using System.Linq;
class C
{
    public static void Main(string[] args)
    {
        var t = typeof(C);
        foreach (var m in t.GetMethods().Where(m => m.DeclaringType == t).OrderBy(m => m.Name))
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
            CompileAndVerify(source, expectedOutput:
@"get__Other
Main
Other");
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

        [Fact, WorkItem(1013334, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1013334")]
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
            var compilation = CreateCompilation(
                source,
                options: TestOptions.DebugExe,
                parseOptions: TestOptions.Regular.WithLanguageVersion(LanguageVersion.CSharp5));
            CompileAndVerify(compilation, expectedOutput: @"12");
        }

        [Fact, WorkItem(1013334, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1013334")]
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
            var compilation = CreateCompilation(
                source,
                options: TestOptions.DebugExe,
                parseOptions: TestOptions.Regular.WithLanguageVersion(LanguageVersion.CSharp6));
            CompileAndVerify(compilation, expectedOutput: @"12");
        }

        [Fact, WorkItem(1013334, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1013334")]
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
            var compilation = CreateCompilation(
                source,
                options: TestOptions.DebugExe,
                parseOptions: TestOptions.Regular.WithLanguageVersion(LanguageVersion.CSharp6)).VerifyDiagnostics(
                    // (7,9): error CS0201: Only assignment, call, increment, decrement, await, and new object expressions can be used as a statement
                    //         nameof(N);
                    Diagnostic(ErrorCode.ERR_IllegalStatement, "nameof(N)").WithLocation(7, 9)
                );
        }

        [Fact, WorkItem(1013334, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1013334")]
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
            var compilation = CreateCompilation(
                source,
                options: TestOptions.DebugExe,
                parseOptions: TestOptions.Regular.WithLanguageVersion(LanguageVersion.CSharp5)).VerifyDiagnostics(
                    // (7,9): error CS8026: Feature 'nameof operator' is not available in C# 5. Please use language version 6 or greater.
                    //         nameof(N);
                    Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion5, "nameof(N)").WithArguments("nameof operator", "6").WithLocation(7, 9),
                    // (7,9): error CS0201: Only assignment, call, increment, decrement, await, and new object expressions can be used as a statement
                    //         nameof(N);
                    Diagnostic(ErrorCode.ERR_IllegalStatement, "nameof(N)").WithLocation(7, 9)
                );
        }

        [Fact]
        [WorkItem(1023539, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1023539")]
        public void SymbolInfoForMethodGroup01()
        {
            var source =
@"public class SomeClass
{
    public const string GooName = nameof(SomeClass.Goo);
    public static int Goo()
    {
        return 1;
    }
}";
            var compilation = CreateCompilation(source);
            var tree = compilation.SyntaxTrees[0];
            var model = compilation.GetSemanticModel(tree);
            var node = tree.GetRoot().DescendantNodes().Where(n => n.ToString() == "SomeClass.Goo").OfType<ExpressionSyntax>().First();
            var symbolInfo = model.GetSymbolInfo(node, default(CancellationToken));
            Assert.Equal(CandidateReason.MemberGroup, symbolInfo.CandidateReason);
            Assert.Equal("Goo", symbolInfo.CandidateSymbols[0].Name);
        }

        [Fact]
        [WorkItem(1023539, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1023539")]
        public void SymbolInfoForMethodGroup02()
        {
            var source =
@"public class SomeClass
{
    public const string GooName = nameof(SomeClass.Goo);
    public static int Goo()
    {
        return 1;
    }
    public static string Goo()
    {
        return string.Empty;
    }
}";
            var compilation = CreateCompilation(source);
            var tree = compilation.SyntaxTrees[0];
            var model = compilation.GetSemanticModel(tree);
            var node = tree.GetRoot().DescendantNodes().Where(n => n.ToString() == "SomeClass.Goo").OfType<ExpressionSyntax>().First();
            var symbolInfo = model.GetSymbolInfo(node, default(CancellationToken));
            Assert.Equal(CandidateReason.MemberGroup, symbolInfo.CandidateReason);
            Assert.Equal(2, symbolInfo.CandidateSymbols.Length);
        }

        [Fact]
        [WorkItem(1077150, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1077150")]
        public void SymbolInfoForMethodGroup03()
        {
            var source =
@"public class A
{
}
public static class X1
{
    public static string Extension(this A a) { return null; }
}
public class Program
{
    public static void Main(string[] args)
    {
        A a = null;
        Use(nameof(a.Extension));
    }
    private static void Use(object o) {}
}";
            var compilation = CreateCompilationWithMscorlib40AndSystemCore(source);
            compilation.VerifyDiagnostics(
                // (13,20): error CS8093: Extension method groups are not allowed as an argument to 'nameof'.
                //         Use(nameof(a.Extension));
                Diagnostic(ErrorCode.ERR_NameofExtensionMethod, "a.Extension").WithLocation(13, 20)
                );
            var tree = compilation.SyntaxTrees[0];
            var model = compilation.GetSemanticModel(tree);
            var node = tree.GetRoot().DescendantNodes().Where(n => n.ToString() == "a.Extension").OfType<ExpressionSyntax>().First();
            var symbolInfo = model.GetSymbolInfo(node, default(CancellationToken));
            Assert.Null(symbolInfo.Symbol);
            Assert.Equal(CandidateReason.MemberGroup, symbolInfo.CandidateReason);
            Assert.Equal(1, symbolInfo.CandidateSymbols.Length);
        }

        [Fact]
        [WorkItem(1077150, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1077150")]
        public void SymbolInfoForMethodGroup04()
        {
            var source =
@"public class A
{
}
namespace N1
{
    public static class X1
    {
        public static string Extension(this A a) { return null; }
    }
    namespace N2
    {
        public static class X2
        {
            public static string Extension(this A a, long x) { return null; }
            public static string Extension(this A a, int x) { return null; }
        }
        public class Program
        {
            public static void Main(string[] args)
            {
                A a = null;
                Use(nameof(a.Extension));
            }
            private static void Use(object o) {}
        }
    }
}";
            var compilation = CreateCompilationWithMscorlib40AndSystemCore(source);
            compilation.VerifyDiagnostics(
                // (22,28): error CS8093: Extension method groups are not allowed as an argument to 'nameof'.
                //                 Use(nameof(a.Extension));
                Diagnostic(ErrorCode.ERR_NameofExtensionMethod, "a.Extension").WithLocation(22, 28)
                );
            var tree = compilation.SyntaxTrees[0];
            var model = compilation.GetSemanticModel(tree);
            var node = tree.GetRoot().DescendantNodes().Where(n => n.ToString() == "a.Extension").OfType<ExpressionSyntax>().First();
            var symbolInfo = model.GetSymbolInfo(node, default(CancellationToken));
            Assert.Equal(CandidateReason.MemberGroup, symbolInfo.CandidateReason);
            Assert.Null(symbolInfo.Symbol);
            Assert.Equal(3, symbolInfo.CandidateSymbols.Length);
        }

        [Fact]
        [WorkItem(1077150, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1077150")]
        public void SymbolInfoForEmptyMethodGroup()
        {
            var source =
@"public class A
{
}
public static class X1
{
    public static string Extension(this string a) { return null; }
    public static string Extension(this int a) { return null; }
}
public class Program
{
    public static void Main(string[] args)
    {
        A a = null;
        Use(nameof(a.Extension));
    }
    private static void Use(object o) {}
}";
            var compilation = CreateCompilationWithMscorlib40AndSystemCore(source);
            compilation.VerifyDiagnostics(
                // (14,22): error CS1061: 'A' does not contain a definition for 'Extension' and no accessible extension method 'Extension' accepting a first argument of type 'A' could be found (are you missing a using directive or an assembly reference?)
                //         Use(nameof(a.Extension));
                Diagnostic(ErrorCode.ERR_NoSuchMemberOrExtension, "Extension").WithArguments("A", "Extension").WithLocation(14, 22)
                );
            var tree = compilation.SyntaxTrees[0];
            var model = compilation.GetSemanticModel(tree);
            var node = tree.GetRoot().DescendantNodes().Where(n => n.ToString() == "a.Extension").OfType<ExpressionSyntax>().First();
            var symbolInfo = model.GetSymbolInfo(node, default(CancellationToken));
            Assert.Equal(CandidateReason.None, symbolInfo.CandidateReason);
            Assert.Null(symbolInfo.Symbol);
            Assert.Equal(0, symbolInfo.CandidateSymbols.Length);
        }

        [Fact]
        [WorkItem(1077150, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1077150")]
        public void SymbolInfoForTypeFromInstance()
        {
            var source =
@"public class A
{
    public class Nested {}
}
public class Program
{
    public static void Main(string[] args)
    {
        A a = null;
        Use(nameof(a.Nested));
    }
    private static void Use(object o) {}
}";
            var compilation = CreateCompilationWithMscorlib40AndSystemCore(source);
            compilation.VerifyDiagnostics(
                // (10,22): error CS0572: 'Nested': cannot reference a type through an expression; try 'A.Nested' instead
                //         Use(nameof(a.Nested));
                Diagnostic(ErrorCode.ERR_BadTypeReference, "Nested").WithArguments("Nested", "A.Nested").WithLocation(10, 22),
                // (9,11): warning CS0219: The variable 'a' is assigned but its value is never used
                //         A a = null;
                Diagnostic(ErrorCode.WRN_UnreferencedVarAssg, "a").WithArguments("a").WithLocation(9, 11)
                );
            var tree = compilation.SyntaxTrees[0];
            var model = compilation.GetSemanticModel(tree);
            var node = tree.GetRoot().DescendantNodes().Where(n => n.ToString() == "a.Nested").OfType<ExpressionSyntax>().First();
            var symbolInfo = model.GetSymbolInfo(node, default(CancellationToken));
            Assert.Equal(CandidateReason.None, symbolInfo.CandidateReason);
            Assert.NotNull(symbolInfo.Symbol);
            Assert.Equal(0, symbolInfo.CandidateSymbols.Length);
        }

        [Fact]
        [WorkItem(1077150, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1077150")]
        public void SymbolInfoForMethodGroup05()
        {
            var source =
@"public class A
{
}
namespace N1
{
    public static class X1
    {
        public static string Extension(this A a) { return null; }
    }
    namespace N2
    {
        public static class X2
        {
            public static string Extension(this A a, long x) { return null; }
            public static string Extension(this A a, int x) { return null; }
        }
        public class Program
        {
            public static void Main(string[] args)
            {
                Use(nameof(A.Extension));
            }
            private static void Use(object o) {}
        }
    }
}";
            var compilation = CreateCompilationWithMscorlib40AndSystemCore(source);
            compilation.VerifyDiagnostics(
                // (21,28): error CS8093: Extension method groups are not allowed as an argument to 'nameof'.
                //                 Use(nameof(A.Extension));
                Diagnostic(ErrorCode.ERR_NameofExtensionMethod, "A.Extension").WithLocation(21, 28)
                );
            var tree = compilation.SyntaxTrees[0];
            var model = compilation.GetSemanticModel(tree);
            var node = tree.GetRoot().DescendantNodes().Where(n => n.ToString() == "A.Extension").OfType<ExpressionSyntax>().First();
            var symbolInfo = model.GetSymbolInfo(node, default(CancellationToken));
            Assert.Equal(CandidateReason.MemberGroup, symbolInfo.CandidateReason);
            Assert.Null(symbolInfo.Symbol);
            Assert.Equal(3, symbolInfo.CandidateSymbols.Length);
        }

        [Fact]
        [WorkItem(1077150, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1077150")]
        public void SymbolInfoForNothingFound()
        {
            var source =
@"public class A
{
}
public class Program
{
    public static void Main(string[] args)
    {
        A a = null;
        Use(nameof(a.Extension));
    }
    private static void Use(object o) {}
}
";
            var compilation = CreateCompilationWithMscorlib40AndSystemCore(source);
            compilation.VerifyDiagnostics(
                // (9,22): error CS1061: 'A' does not contain a definition for 'Extension' and no extension method 'Extension' accepting a first argument of type 'A' could be found (are you missing a using directive or an assembly reference?)
                //         Use(nameof(a.Extension));
                Diagnostic(ErrorCode.ERR_NoSuchMemberOrExtension, "Extension").WithArguments("A", "Extension").WithLocation(9, 22)
                );
            var tree = compilation.SyntaxTrees[0];
            var model = compilation.GetSemanticModel(tree);
            var node = tree.GetRoot().DescendantNodes().Where(n => n.ToString() == "a.Extension").OfType<ExpressionSyntax>().First();
            var symbolInfo = model.GetSymbolInfo(node, default(CancellationToken));
            Assert.Equal(CandidateReason.None, symbolInfo.CandidateReason);
            Assert.Null(symbolInfo.Symbol);
            Assert.Equal(0, symbolInfo.CandidateSymbols.Length);
        }

        [Fact]
        public void SymbolInfo_InstanceMemberFromStatic_Flat()
        {
            var source = """
                public class C
                {
                    public int Property { get; }
                    public int Field;
                    public event System.Action Event;

                    public static string StaticField =
                        nameof(Property) +
                        nameof(Field) +
                        nameof(Event);
                }
                """;
            var comp = CreateCompilation(source).VerifyDiagnostics();

            var cProperty = comp.GetMember("C.Property");
            var cField = comp.GetMember("C.Field");
            var cEvent = comp.GetMember("C.Event");

            var tree = comp.SyntaxTrees.Single();
            var tree2 = SyntaxFactory.ParseSyntaxTree(source + " ");

            var initializer = tree2.GetRoot().DescendantNodes().OfType<EqualsValueClauseSyntax>().Single();

            var nameofCalls = getNameOfCalls(tree);
            Assert.Equal(3, nameofCalls.Length);
            var nameofCalls2 = getNameOfCalls(tree2);
            Assert.Equal(3, nameofCalls2.Length);

            var model = comp.GetSemanticModel(tree);

            verify(0, "Property", cProperty);
            verify(1, "Field", cField);
            verify(2, "Event", cEvent);

            void verify(int index, string expression, Symbol symbol)
            {
                var argument = nameofCalls[index].ArgumentList.Arguments.Single().Expression;
                Assert.Equal(expression, argument.ToString());

                verifySymbolInfo(model.GetSymbolInfo(argument));

                var argument2 = nameofCalls2[index].ArgumentList.Arguments.Single().Expression;
                Assert.Equal(expression, argument2.ToString());

                Assert.True(model.TryGetSpeculativeSemanticModel(initializer.Position, initializer, out var model2));

                verifySymbolInfo(model2.GetSymbolInfo(argument2));

                verifySymbolInfo(model.GetSpeculativeSymbolInfo(argument2.Position, argument2, SpeculativeBindingOption.BindAsExpression));

                Assert.True(model.GetSpeculativeSymbolInfo(argument2.Position, argument2, SpeculativeBindingOption.BindAsTypeOrNamespace).IsEmpty);

                void verifySymbolInfo(SymbolInfo symbolInfo)
                {
                    Assert.NotNull(symbolInfo.Symbol);
                    Assert.Same(symbol.GetPublicSymbol(), symbolInfo.Symbol);
                }
            }

            static ImmutableArray<InvocationExpressionSyntax> getNameOfCalls(SyntaxTree tree)
            {
                return tree.GetRoot().DescendantNodes().OfType<InvocationExpressionSyntax>()
                    .Where(e => e.Expression is IdentifierNameSyntax { Identifier.ValueText: "nameof" })
                    .ToImmutableArray();
            }
        }

        [Fact]
        public void SymbolInfo_InstanceMemberFromStatic_Flat_MethodGroup()
        {
            var source = """
                public class C
                {
                    public void Method1() { }
                    public void Method1(int i) { }
                    public void Method2() { }
                    public static void Method2(int i) { }
                
                    public static string StaticField =
                        nameof(Method1) +
                        nameof(Method2);
                }
                """;
            var comp = CreateCompilation(source).VerifyDiagnostics();

            var cMethods1 = comp.GetMembers("C.Method1");
            Assert.Equal(2, cMethods1.Length);
            var cMethods2 = comp.GetMembers("C.Method2");
            Assert.Equal(2, cMethods2.Length);

            var tree = comp.SyntaxTrees.Single();
            var tree2 = SyntaxFactory.ParseSyntaxTree(source + " ");

            var initializer = tree2.GetRoot().DescendantNodes().OfType<EqualsValueClauseSyntax>().Single();

            var nameofCalls = getNameOfCalls(tree);
            Assert.Equal(2, nameofCalls.Length);
            var nameofCalls2 = getNameOfCalls(tree2);
            Assert.Equal(2, nameofCalls2.Length);

            var model = comp.GetSemanticModel(tree);

            verify(0, "Method1", cMethods1);
            verify(1, "Method2", cMethods2);

            void verify(int index, string expression, ImmutableArray<Symbol> symbols)
            {
                var argument = nameofCalls[index].ArgumentList.Arguments.Single().Expression;
                Assert.Equal(expression, argument.ToString());

                verifySymbolInfo(CandidateReason.MemberGroup, model.GetSymbolInfo(argument));

                var argument2 = nameofCalls2[index].ArgumentList.Arguments.Single().Expression;
                Assert.Equal(expression, argument2.ToString());

                Assert.True(model.TryGetSpeculativeSemanticModel(initializer.Position, initializer, out var model2));

                verifySymbolInfo(CandidateReason.MemberGroup, model2.GetSymbolInfo(argument2));

                verifySymbolInfo(CandidateReason.OverloadResolutionFailure, model.GetSpeculativeSymbolInfo(argument2.Position, argument2, SpeculativeBindingOption.BindAsExpression));

                Assert.True(model.GetSpeculativeSymbolInfo(argument2.Position, argument2, SpeculativeBindingOption.BindAsTypeOrNamespace).IsEmpty);

                void verifySymbolInfo(CandidateReason reason, SymbolInfo symbolInfo)
                {
                    Assert.Equal(reason, symbolInfo.CandidateReason);
                    AssertEx.SetEqual(
                        symbols.Select(s => s.GetPublicSymbol()),
                        symbolInfo.CandidateSymbols,
                        ReferenceEqualityComparer.Instance);
                }
            }

            static ImmutableArray<InvocationExpressionSyntax> getNameOfCalls(SyntaxTree tree)
            {
                return tree.GetRoot().DescendantNodes().OfType<InvocationExpressionSyntax>()
                    .Where(e => e.Expression is IdentifierNameSyntax { Identifier.ValueText: "nameof" })
                    .ToImmutableArray();
            }
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/67565")]
        public void SymbolInfo_InstanceMemberFromStatic_Nested()
        {
            var source = """
                public class C
                {
                    public C1 Property { get; }
                    public C1 Field;

                    public static string StaticField =
                        nameof(Property.Property) +
                        nameof(Property.Field) +
                        nameof(Property.Event) +
                        nameof(Field.Property) +
                        nameof(Field.Field) +
                        nameof(Field.Event);
                }
                
                public class C1
                {
                    public int Property { get; }
                    public int Field;
                    public event System.Action Event;
                }
                """;
            var comp = CreateCompilation(source).VerifyDiagnostics();

            var c1Property = comp.GetMember("C1.Property");
            var c1Field = comp.GetMember("C1.Field");
            var c1Event = comp.GetMember("C1.Event");

            var tree = comp.SyntaxTrees.Single();
            var tree2 = SyntaxFactory.ParseSyntaxTree(source + " ");

            var initializer = tree2.GetRoot().DescendantNodes().OfType<EqualsValueClauseSyntax>().Single();

            var nameofCalls = getNameOfCalls(tree);
            Assert.Equal(6, nameofCalls.Length);
            var nameofCalls2 = getNameOfCalls(tree2);
            Assert.Equal(6, nameofCalls2.Length);

            var model = comp.GetSemanticModel(tree);

            verify(0, "Property.Property", c1Property);
            verify(1, "Property.Field", c1Field);
            verify(2, "Property.Event", c1Event);
            verify(3, "Field.Property", c1Property);
            verify(4, "Field.Field", c1Field);
            verify(5, "Field.Event", c1Event);

            void verify(int index, string expression, Symbol symbol)
            {
                var argument = nameofCalls[index].ArgumentList.Arguments.Single().Expression;
                Assert.Equal(expression, argument.ToString());

                verifySymbolInfo(model.GetSymbolInfo(argument));

                var argument2 = nameofCalls2[index].ArgumentList.Arguments.Single().Expression;
                Assert.Equal(expression, argument2.ToString());

                Assert.True(model.TryGetSpeculativeSemanticModel(initializer.Position, initializer, out var model2));

                verifySymbolInfo(model2.GetSymbolInfo(argument2));

                verifySymbolInfo(model.GetSpeculativeSymbolInfo(argument2.Position, argument2, SpeculativeBindingOption.BindAsExpression));

                Assert.True(model.GetSpeculativeSymbolInfo(argument2.Position, argument2, SpeculativeBindingOption.BindAsTypeOrNamespace).IsEmpty);

                void verifySymbolInfo(SymbolInfo symbolInfo)
                {
                    Assert.NotNull(symbolInfo.Symbol);
                    Assert.Same(symbol.GetPublicSymbol(), symbolInfo.Symbol);
                }
            }

            static ImmutableArray<InvocationExpressionSyntax> getNameOfCalls(SyntaxTree tree)
            {
                return tree.GetRoot().DescendantNodes().OfType<InvocationExpressionSyntax>()
                    .Where(e => e.Expression is IdentifierNameSyntax { Identifier.ValueText: "nameof" })
                    .ToImmutableArray();
            }
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/67565")]
        public void SymbolInfo_InstanceMemberFromStatic_Nested_MethodGroup()
        {
            var source = """
                public class C
                {
                    public C1 Property { get; }
                    public C1 Field;
                    public event System.Action Event;
                
                    public static string StaticField =
                        nameof(Property.Method) +
                        nameof(Field.Method) +
                        nameof(Event.Invoke);
                }
                
                public class C1
                {
                    public void Method() { }
                    public void Method(int i) { }
                }
                """;
            var comp = CreateCompilation(source).VerifyDiagnostics();

            var c1Methods = comp.GetMembers("C1.Method").ToArray();
            Assert.Equal(2, c1Methods.Length);
            var c1Event = comp.GetMember("C1.Event");
            var actionInvoke = comp.GetWellKnownType(WellKnownType.System_Action).GetMember("Invoke");

            var tree = comp.SyntaxTrees.Single();
            var tree2 = SyntaxFactory.ParseSyntaxTree(source + " ");

            var initializer = tree2.GetRoot().DescendantNodes().OfType<EqualsValueClauseSyntax>().Single();

            var nameofCalls = getNameOfCalls(tree);
            Assert.Equal(3, nameofCalls.Length);
            var nameofCalls2 = getNameOfCalls(tree2);
            Assert.Equal(3, nameofCalls2.Length);

            var model = comp.GetSemanticModel(tree);

            verify(0, "Property.Method", c1Methods);
            verify(1, "Field.Method", c1Methods);
            verify(2, "Event.Invoke", actionInvoke);

            void verify(int index, string expression, params Symbol[] symbols)
            {
                var argument = nameofCalls[index].ArgumentList.Arguments.Single().Expression;
                Assert.Equal(expression, argument.ToString());

                verifySymbolInfo(CandidateReason.MemberGroup, model.GetSymbolInfo(argument));

                var argument2 = nameofCalls2[index].ArgumentList.Arguments.Single().Expression;
                Assert.Equal(expression, argument2.ToString());

                Assert.True(model.TryGetSpeculativeSemanticModel(initializer.Position, initializer, out var model2));

                verifySymbolInfo(CandidateReason.MemberGroup, model2.GetSymbolInfo(argument2));

                verifySymbolInfo(CandidateReason.OverloadResolutionFailure, model.GetSpeculativeSymbolInfo(argument2.Position, argument2, SpeculativeBindingOption.BindAsExpression));

                Assert.True(model.GetSpeculativeSymbolInfo(argument2.Position, argument2, SpeculativeBindingOption.BindAsTypeOrNamespace).IsEmpty);

                void verifySymbolInfo(CandidateReason reason, SymbolInfo symbolInfo)
                {
                    Assert.Equal(reason, symbolInfo.CandidateReason);
                    AssertEx.SetEqual(
                        symbols.Select(s => s.GetPublicSymbol()),
                        symbolInfo.CandidateSymbols,
                        ReferenceEqualityComparer.Instance);
                }
            }

            static ImmutableArray<InvocationExpressionSyntax> getNameOfCalls(SyntaxTree tree)
            {
                return tree.GetRoot().DescendantNodes().OfType<InvocationExpressionSyntax>()
                    .Where(e => e.Expression is IdentifierNameSyntax { Identifier.ValueText: "nameof" })
                    .ToImmutableArray();
            }
        }

        [Fact]
        public void ExtensionMethodConstraintFailed()
        {
            var source =
@"public class A
{
}
public interface Interface
{
}
public static class B
{
    public static void Extension<T>(this T t) where T : Interface {}
}
public class Program
{
    public static void Main(string[] args)
    {
        A a = null;
        Use(nameof(a.Extension));
    }
    private static void Use(object o) {}
}
";
            var compilation = CreateCompilationWithMscorlib40AndSystemCore(source);
            compilation.VerifyDiagnostics(
                // (16,22): error CS1061: 'A' does not contain a definition for 'Extension' and no accessible extension method 'Extension' accepting a first argument of type 'A' could be found (are you missing a using directive or an assembly reference?)
                //         Use(nameof(a.Extension));
                Diagnostic(ErrorCode.ERR_NoSuchMemberOrExtension, "Extension").WithArguments("A", "Extension").WithLocation(16, 22)
                );
        }

        [Fact]
        public void StaticMemberFromType()
        {
            var source =
@"public class A
{
    public static int Field;
    public static int Property { get; }
    public static event System.Action Event;
    public class Type {}
}
public class Program
{
    public static void Main(string[] args)
    {
        Use(nameof(A.Field));
        Use(nameof(A.Property));
        Use(nameof(A.Event));
        Use(nameof(A.Type));
    }
    private static void Use(object o) {}
}";
            var compilation = CreateCompilationWithMscorlib40AndSystemCore(source);
            compilation.VerifyDiagnostics();
        }

        [Fact]
        public void AllowImplicitThisInStaticContext()
        {
            var source =
@"
public class MyAttribute : System.Attribute
{
    public MyAttribute(string S) {}
}

[My(nameof(SS))] // attribute argument (NOTE: class members in scope here)
public class Program
{
    string SS;
    static string S1 = nameof(SS); // static initializer
    string S2 = nameof(SS); // instance initializer
    Program(string s) {}
    Program() : this(nameof(SS)) {} // ctor initializer

    static string L(string s = nameof(SS)) // default value
    {
        return nameof(SS); // in static method
    }
    public void M()
    {
        SS = SS + S1 + S2;
    }
}";
            var compilation = CreateCompilationWithMscorlib40AndSystemCore(source);
            compilation.VerifyDiagnostics();
        }

        [Fact]
        public void NameofInaccessibleMethod()
        {
            var source =
@"
public class Class
{
    protected void Method() {}
}
public class Program
{
    public string S = nameof(Class.Method);
}";
            var compilation = CreateCompilationWithMscorlib40AndSystemCore(source);
            compilation.VerifyDiagnostics(
                // (8,36): error CS0122: 'Class.Method()' is inaccessible due to its protection level
                //     public string S = nameof(Class.Method);
                Diagnostic(ErrorCode.ERR_BadAccess, "Method").WithArguments("Class.Method()").WithLocation(8, 36)
                );
        }

        [Fact]
        public void NameofAmbiguousProperty()
        {
            var source =
@"
public interface I1
{
    int Property { get; }
}
public interface I2
{
    int Property { get; }
}
public interface I3 : I1, I2 {}
public class Program
{
    public string S = nameof(I3.Property);
}";
            var compilation = CreateCompilationWithMscorlib40AndSystemCore(source);
            compilation.VerifyDiagnostics(
                // (13,33): error CS0229: Ambiguity between 'I1.Property' and 'I2.Property'
                //     public string S = nameof(I3.Property);
                Diagnostic(ErrorCode.ERR_AmbigMember, "Property").WithArguments("I1.Property", "I2.Property").WithLocation(13, 33)
                );
        }

        [Fact]
        [WorkItem(1077150, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1077150")]
        public void SymbolInfoForMethodGroup06()
        {
            var source =
@"public class A
{
}
public static class X1
{
    public static string Extension(this A a) { return null; }
}
public class Program
{
    public static void Main(string[] args)
    {
        Use(nameof(X1.Extension));
    }
    private static void Use(object o) {}
}";
            var compilation = CreateCompilationWithMscorlib40AndSystemCore(source);
            compilation.VerifyDiagnostics();
            var tree = compilation.SyntaxTrees[0];
            var model = compilation.GetSemanticModel(tree);
            var node = tree.GetRoot().DescendantNodes().Where(n => n.ToString() == "X1.Extension").OfType<ExpressionSyntax>().First();
            var symbolInfo = model.GetSymbolInfo(node, default(CancellationToken));
            Assert.Null(symbolInfo.Symbol);
            Assert.Equal(CandidateReason.MemberGroup, symbolInfo.CandidateReason);
            Assert.Equal(1, symbolInfo.CandidateSymbols.Length);
        }

        [Fact, WorkItem(40, "github.com/dotnet/roslyn")]
        public void ConstInitializerUsingSelf()
        {
            var source =
@"public class X
{
    const string N1 = nameof(N1);
    public static void Main()
    {
        const string N2 = nameof(N2);
        System.Console.WriteLine(N1 + N2);
    }
}";
            var compilation = CreateCompilationWithMscorlib461(source, options: TestOptions.DebugExe);
            var comp = CompileAndVerify(compilation, expectedOutput: @"N1N2");
        }

        [Fact, WorkItem(42, "github.com/dotnet/roslyn")]
        public void NameofTypeParameterInParameterInitializer()
        {
            var source =
@"class Test {
  void M<T>(
    T t = default(T), // ok
    string s = nameof(T) // ok
  ) { }
}";
            var compilation = CreateCompilationWithMscorlib461(source).VerifyDiagnostics();
        }

        [Fact, WorkItem(10467, "https://github.com/dotnet/roslyn/issues/10467")]
        public void NameofFixedBuffer()
        {
            var source =
@"
using System;
unsafe struct Struct1
{
    public fixed char MessageType[50];

    public override string ToString()
    {
        return nameof(MessageType);
    }

    public void DoSomething(out char[] x)
    {
        x = new char[] { };
        Action a = () => { System.Console.Write($"" {nameof(x)} ""); };
        a();
    }

    public static void Main()
    {
        Struct1 myStruct = default(Struct1);
        Console.Write(myStruct.ToString());
        char[] o;
        myStruct.DoSomething(out o);
        Console.Write(Other.GetFromExternal());
    }
}

class Other {
    public static string GetFromExternal() {
        Struct1 myStruct = default(Struct1);
        return nameof(myStruct.MessageType);
    }
}
";
            var compilation = CreateCompilationWithMscorlib461(source, null, TestOptions.UnsafeDebugExe);
            CompileAndVerify(compilation, expectedOutput:
                "MessageType x MessageType").VerifyDiagnostics();
        }

        [Fact, WorkItem(10467, "https://github.com/dotnet/roslyn/issues/10467")]
        public void NameofMethodFixedBuffer()
        {
            var source =
@"
using System;

unsafe struct Struct1
{
  public fixed char MessageType[50];
  public static string nameof(char[] mt)
  {
    return """";
  }

  public override string ToString()
  {
    return nameof(MessageType);
  }

  public void DoSomething(out char[] x)
  {
    x = new char[] {};
    Action a = () => { System.Console.WriteLine(nameof(x)); };
  }

  class Other {
    public static string GetFromExternal() {
        Struct1 myStruct = default(Struct1);
        return nameof(myStruct.MessageType);
    }
  }
}";
            var compilation = CreateCompilationWithMscorlib461(source, null,
                TestOptions.UnsafeDebugDll).VerifyDiagnostics(
                // (14,19): error CS1666: You cannot use fixed size buffers contained in unfixed expressions. Try using the fixed statement.
                //     return nameof(MessageType);
                Diagnostic(ErrorCode.ERR_FixedBufferNotFixed, "MessageType").WithLocation(14, 19),
                // (20,29): error CS1628: Cannot use ref or out parameter 'x' inside an anonymous method, lambda expression, or query expression
                //     Action a = () => nameof(x);
                Diagnostic(ErrorCode.ERR_AnonDelegateCantUse, "x").WithArguments("x").WithLocation(20, 56),
                // (26,23): error CS1503: Argument 1: cannot convert from 'char*' to 'char[]'
                //         return nameof(myStruct.MessageType);
                Diagnostic(ErrorCode.ERR_BadArgType, "myStruct.MessageType").WithArguments("1", "char*", "char[]").WithLocation(26, 23));
        }

        [Fact, WorkItem(12696, "https://github.com/dotnet/roslyn/issues/12696")]
        public void FixedFieldAccessInsideNameOf()
        {
            var source =
@"
using System;

struct MyType
{
  public static string a = nameof(MyType.normalField);
  public static string b = nameof(MyType.fixedField);
  public static string c = nameof(fixedField);

  public int normalField;
  public unsafe fixed short fixedField[6];

  public MyType(int i) {
      this.normalField = i;
  }
}

class EntryPoint
{
    public static void Main(string[] args)
    {
        Console.Write(MyType.a + "" "");
        Console.Write(MyType.b + "" "");
        Console.Write(MyType.c);
    }
}
";
            var compilation = CreateCompilationWithMscorlib461(source, null, TestOptions.UnsafeDebugExe);
            CompileAndVerify(compilation, expectedOutput: "normalField fixedField fixedField").VerifyDiagnostics();
        }

        [Fact, WorkItem(12696, "https://github.com/dotnet/roslyn/issues/12696")]
        public void FixedFieldAccessFromInnerClass()
        {
            var source =
@"
using System;

public struct MyType
{
  public static class Inner
  {
     public static string a = nameof(normalField);
     public static string b = nameof(fixedField);
  }

  public int normalField;
  public unsafe fixed short fixedField[6];
}

class EntryPoint
{
    public static void Main(string[] args)
    {
        Console.Write(MyType.Inner.a + "" "");
        Console.Write(MyType.Inner.b);
    }
}
";
            var compilation = CreateCompilationWithMscorlib461(source, null, TestOptions.UnsafeDebugExe);
            CompileAndVerify(compilation, expectedOutput: "normalField fixedField").VerifyDiagnostics();
        }

        [Fact]
        public void PassingNameOfToInShouldCopy()
        {
            CompileAndVerify(@"
class Program
{
    public static void Main()
    {
        M(nameof(Main));
    }
    private static void M(in string value)
    {
        System.Console.WriteLine(value);
    }
}", expectedOutput: "Main");
        }

        [Fact, WorkItem(20600, "https://github.com/dotnet/roslyn/issues/20600")]
        public void PermitInstanceQualifiedFromType()
        {
            var source = @"
class Program
{
    static void Main()
    {
        new C().M();
        new C<int>().M();
        System.Console.WriteLine(""passed"");
    }
}
class C
{
    public string Instance1 = null;
    public static string Static1 = null;
    public string Instance2 => string.Empty;
    public static string Static2 => string.Empty;
      
    public void M()
    {
        nameof(C.Instance1).Verify(""Instance1"");
        nameof(C.Instance1.Length).Verify(""Length"");
        nameof(C.Static1).Verify(""Static1"");
        nameof(C.Static1.Length).Verify(""Length"");
        nameof(C.Instance2).Verify(""Instance2"");
        nameof(C.Instance2.Length).Verify(""Length"");
        nameof(C.Static2).Verify(""Static2"");
        nameof(C.Static2.Length).Verify(""Length"");
    }
}
class C<T>
{
    public string Instance1 = null;
    public static string Static1 = null;
    public string Instance2 => string.Empty;
    public static string Static2 => string.Empty;

    public void M()
    {
        nameof(C<string>.Instance1).Verify(""Instance1"");
        nameof(C<string>.Instance1.Length).Verify(""Length"");
        nameof(C<string>.Static1).Verify(""Static1"");
        nameof(C<string>.Static1.Length).Verify(""Length"");
        nameof(C<string>.Instance2).Verify(""Instance2"");
        nameof(C<string>.Instance2.Length).Verify(""Length"");
        nameof(C<string>.Static2).Verify(""Static2"");
        nameof(C<string>.Static2.Length).Verify(""Length"");
    }
}
public static class Extensions
{
    public static void Verify(this string actual, string expected)
    {
        if (expected != actual) throw new System.Exception($""expected={expected}; actual={actual}"");
    }
}
";
            var compilation = CreateCompilationWithMscorlib40AndSystemCore(source, options: TestOptions.ReleaseExe);
            compilation.VerifyDiagnostics(
                );
            var comp = CompileAndVerify(compilation, expectedOutput: @"passed");
        }

        [Fact]
        public void TestDynamicWhenNotDefined()
        {
            var source = @"
class Program
{
    static string M() => nameof(dynamic);
}
";
            var option = TestOptions.ReleaseDll;
            CreateCompilation(source, options: option).VerifyDiagnostics(
                // (4,33): error CS0103: The name 'dynamic' does not exist in the current context
                //     static string M() => nameof(dynamic);
                Diagnostic(ErrorCode.ERR_NameNotInContext, "dynamic").WithArguments("dynamic").WithLocation(4, 33)
            );
        }

        [Fact]
        public void TestNintWhenDefined()
        {
            var source = @"
class Program
{
    static string M(object nint) => nameof(nint);
}
";
            var option = TestOptions.ReleaseDll;
            CreateCompilation(source, options: option).VerifyDiagnostics();
        }

        [Fact]
        public void TestDynamicWhenDefined()
        {
            var source = @"
class Program
{
    static string M(object dynamic) => nameof(dynamic);
}
";
            var option = TestOptions.ReleaseDll;
            CreateCompilation(source, options: option).VerifyDiagnostics();
        }

        [Fact]
        public void TestTypeArguments()
        {
            var source = @"
interface I<T> { }
class Program
{
    static string F1() => nameof(I<int>);
    static string F2() => nameof(I<nint>);
    static string F3() => nameof(I<dynamic>);
}";
            var option = TestOptions.ReleaseDll;
            CreateCompilation(source, options: option).VerifyDiagnostics();
        }

        [Fact]
        public void TestNameOfTypeOf()
        {
            var source = @"
class Program
{
    static string F1() => nameof(typeof(int));
    static string F2() => nameof(typeof(nint));
    static string F3() => nameof(typeof(dynamic));
}";
            var option = TestOptions.ReleaseDll;
            CreateCompilation(source, options: option).VerifyDiagnostics(
                // (4,34): error CS8081: Expression does not have a name.
                //     static string F1() => nameof(typeof(int));
                Diagnostic(ErrorCode.ERR_ExpressionHasNoName, "typeof(int)").WithLocation(4, 34),
                // (5,34): error CS8081: Expression does not have a name.
                //     static string F2() => nameof(typeof(nint));
                Diagnostic(ErrorCode.ERR_ExpressionHasNoName, "typeof(nint)").WithLocation(5, 34),
                // (6,34): error CS1962: The typeof operator cannot be used on the dynamic type
                //     static string F3() => nameof(typeof(dynamic));
                Diagnostic(ErrorCode.ERR_BadDynamicTypeof, "typeof(dynamic)").WithLocation(6, 34));
        }

        [Fact]
        public void TestNameOfNintWhenTheyAreIdentifierNames()
        {
            var source = @"
public class C 
{
    public string nint;
    public void nameof(string x)
    {
        nameof(nint);
    }
}";
            var option = TestOptions.ReleaseDll;
            CreateCompilation(source, options: option).VerifyDiagnostics();
        }

        [Fact, WorkItem(40229, "https://github.com/dotnet/roslyn/issues/40229")]
        public void TestCanReferenceInstanceMembersFromStaticMemberInNameof_Flat()
        {
            var source = @"
System.Console.Write(C.M());
public class C
{
    public object Property { get; }
    public object Field;
    public event System.Action Event;
    public void M2() { }
    public static string M() => nameof(Property)
        + "","" + nameof(Field)
        + "","" + nameof(Event)
        + "","" + nameof(M2)
        ;
}";
            var expectedOutput = "Property,Field,Event,M2";

            CompileAndVerify(source, parseOptions: TestOptions.Regular11, expectedOutput: expectedOutput).VerifyDiagnostics();
            CompileAndVerify(source, parseOptions: TestOptions.Regular12, expectedOutput: expectedOutput).VerifyDiagnostics();
            CompileAndVerify(source, parseOptions: TestOptions.RegularPreview, expectedOutput: expectedOutput).VerifyDiagnostics();
        }

        [Fact, WorkItem(40229, "https://github.com/dotnet/roslyn/issues/40229")]
        public void TestCanReferenceInstanceMembersFromStaticMemberInNameof_Nested()
        {
            var source = @"
System.Console.Write(C.M());
public class C
{
    public C1 Property { get; }
    public C1 Field;
    public event System.Action Event;
    public static string M() => nameof(Property.Property) 
        + "","" + nameof(Property.Field)
        + "","" + nameof(Property.Method)
        + "","" + nameof(Property.Event)
        + "","" + nameof(Field.Property) 
        + "","" + nameof(Field.Field)
        + "","" + nameof(Field.Method)
        + "","" + nameof(Field.Event)
        + "","" + nameof(Event.Invoke)
        ;
}

public class C1
{
    public int Property { get; }
    public int Field;
    public void Method(){}
    public event System.Action Event;
}";
            var expectedOutput = "Property,Field,Method,Event,Property,Field,Method,Event,Invoke";
            CompileAndVerify(source, parseOptions: TestOptions.Regular12, expectedOutput: expectedOutput).VerifyDiagnostics();
            CompileAndVerify(source, parseOptions: TestOptions.RegularPreview, expectedOutput: expectedOutput).VerifyDiagnostics();
            CreateCompilation(source, parseOptions: TestOptions.Regular11).VerifyDiagnostics(
                // (8,40): error CS9058: Feature 'instance member in 'nameof'' is not available in C# 11.0. Please use language version 12.0 or greater.
                //     public static string M() => nameof(Property.Property) 
                Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion11, "Property").WithArguments("instance member in 'nameof'", "12.0").WithLocation(8, 40),
                // (9,24): error CS9058: Feature 'instance member in 'nameof'' is not available in C# 11.0. Please use language version 12.0 or greater.
                //         + "," + nameof(Property.Field)
                Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion11, "Property").WithArguments("instance member in 'nameof'", "12.0").WithLocation(9, 24),
                // (10,24): error CS9058: Feature 'instance member in 'nameof'' is not available in C# 11.0. Please use language version 12.0 or greater.
                //         + "," + nameof(Property.Method)
                Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion11, "Property").WithArguments("instance member in 'nameof'", "12.0").WithLocation(10, 24),
                // (11,24): error CS9058: Feature 'instance member in 'nameof'' is not available in C# 11.0. Please use language version 12.0 or greater.
                //         + "," + nameof(Property.Event)
                Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion11, "Property").WithArguments("instance member in 'nameof'", "12.0").WithLocation(11, 24),
                // (12,24): error CS9058: Feature 'instance member in 'nameof'' is not available in C# 11.0. Please use language version 12.0 or greater.
                //         + "," + nameof(Field.Property) 
                Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion11, "Field").WithArguments("instance member in 'nameof'", "12.0").WithLocation(12, 24),
                // (13,24): error CS9058: Feature 'instance member in 'nameof'' is not available in C# 11.0. Please use language version 12.0 or greater.
                //         + "," + nameof(Field.Field)
                Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion11, "Field").WithArguments("instance member in 'nameof'", "12.0").WithLocation(13, 24),
                // (14,24): error CS9058: Feature 'instance member in 'nameof'' is not available in C# 11.0. Please use language version 12.0 or greater.
                //         + "," + nameof(Field.Method)
                Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion11, "Field").WithArguments("instance member in 'nameof'", "12.0").WithLocation(14, 24),
                // (15,24): error CS9058: Feature 'instance member in 'nameof'' is not available in C# 11.0. Please use language version 12.0 or greater.
                //         + "," + nameof(Field.Event)
                Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion11, "Field").WithArguments("instance member in 'nameof'", "12.0").WithLocation(15, 24),
                // (16,24): error CS9058: Feature 'instance member in 'nameof'' is not available in C# 11.0. Please use language version 12.0 or greater.
                //         + "," + nameof(Event.Invoke)
                Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion11, "Event").WithArguments("instance member in 'nameof'", "12.0").WithLocation(16, 24));
        }

        [Fact, WorkItem(40229, "https://github.com/dotnet/roslyn/issues/40229")]
        public void InstanceFromStatic_Lambdas()
        {
            var source = """
                using System;
                Console.Write(C.Names());
                public class C
                {
                    public object Property { get; }
                    public object Field;
                    public event Action Event;
                    public void Method() { }
                    public static string Names()
                    {
                        var lambda1 = static () => nameof(Property);
                        var lambda2 = static (string f = nameof(Field)) => f;
                        var lambda3 = static () => nameof(Event.Invoke);
                        var lambda4 = static (string i = nameof(Event.Invoke)) => i;
                        return lambda1() + "," + lambda2() + "," + lambda3() + "," + lambda4();
                    }
                }
                """;
            var expectedOutput = "Property,Field,Invoke,Invoke";

            CompileAndVerify(source, parseOptions: TestOptions.Regular12, expectedOutput: expectedOutput).VerifyDiagnostics();
            CompileAndVerify(source, parseOptions: TestOptions.RegularPreview, expectedOutput: expectedOutput).VerifyDiagnostics();
            CreateCompilation(source, parseOptions: TestOptions.Regular11).VerifyDiagnostics(
                // (12,40): error CS9058: Feature 'lambda optional parameters' is not available in C# 11.0. Please use language version 12.0 or greater.
                //         var lambda2 = static (string f = nameof(Field)) => f;
                Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion11, "=").WithArguments("lambda optional parameters", "12.0").WithLocation(12, 40),
                // (13,43): error CS9058: Feature 'instance member in 'nameof'' is not available in C# 11.0. Please use language version 12.0 or greater.
                //         var lambda3 = static () => nameof(Event.Invoke);
                Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion11, "Event").WithArguments("instance member in 'nameof'", "12.0").WithLocation(13, 43),
                // (14,40): error CS9058: Feature 'lambda optional parameters' is not available in C# 11.0. Please use language version 12.0 or greater.
                //         var lambda4 = static (string i = nameof(Event.Invoke)) => i;
                Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion11, "=").WithArguments("lambda optional parameters", "12.0").WithLocation(14, 40),
                // (14,49): error CS9058: Feature 'instance member in 'nameof'' is not available in C# 11.0. Please use language version 12.0 or greater.
                //         var lambda4 = static (string i = nameof(Event.Invoke)) => i;
                Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion11, "Event").WithArguments("instance member in 'nameof'", "12.0").WithLocation(14, 49));
        }

        [Fact, WorkItem(40229, "https://github.com/dotnet/roslyn/issues/40229")]
        public void InstanceFromStatic_LocalFunctions()
        {
            var source = """
                using System;
                Console.Write(C.Names());
                public class C
                {
                    public object Property { get; }
                    public object Field;
                    public event Action Event;
                    public void Method() { }
                    public static string Names()
                    {
                        static string local1() => nameof(Property);
                        static string local2(string f = nameof(Field)) => f;
                        static string local3() => nameof(Event.Invoke);
                        static string local4(string i = nameof(Event.Invoke)) => i;
                        return local1() + "," + local2() + "," + local3() + "," + local4();
                    }
                }
                """;
            var expectedOutput = "Property,Field,Invoke,Invoke";

            CompileAndVerify(source, parseOptions: TestOptions.Regular12, expectedOutput: expectedOutput).VerifyDiagnostics();
            CompileAndVerify(source, parseOptions: TestOptions.RegularPreview, expectedOutput: expectedOutput).VerifyDiagnostics();
            CreateCompilation(source, parseOptions: TestOptions.Regular11).VerifyDiagnostics(
                // (13,42): error CS9058: Feature 'instance member in 'nameof'' is not available in C# 11.0. Please use language version 12.0 or greater.
                //         static string local3() => nameof(Event.Invoke);
                Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion11, "Event").WithArguments("instance member in 'nameof'", "12.0").WithLocation(13, 42),
                // (14,48): error CS9058: Feature 'instance member in 'nameof'' is not available in C# 11.0. Please use language version 12.0 or greater.
                //         static string local4(string i = nameof(Event.Invoke)) => i;
                Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion11, "Event").WithArguments("instance member in 'nameof'", "12.0").WithLocation(14, 48));
        }

        [Fact, WorkItem(40229, "https://github.com/dotnet/roslyn/issues/40229")]
        public void TestCanReferenceInstanceMembersFromFieldInitializerInNameof()
        {
            var source = @"
System.Console.Write(new C().S);
public class C
{
    public string S { get; } = nameof(S.Length);
}";
            CompileAndVerify(source, parseOptions: TestOptions.Regular12, expectedOutput: "Length").VerifyDiagnostics();
            CompileAndVerify(source, parseOptions: TestOptions.RegularPreview, expectedOutput: "Length").VerifyDiagnostics();
            CreateCompilation(source, parseOptions: TestOptions.Regular11).VerifyDiagnostics(
                // (5,39): error CS9058: Feature 'instance member in 'nameof'' is not available in C# 11.0. Please use language version 12.0 or greater.
                //     public string S { get; } = nameof(S.Length);
                Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion11, "S").WithArguments("instance member in 'nameof'", "12.0").WithLocation(5, 39));
        }

        [Fact, WorkItem(40229, "https://github.com/dotnet/roslyn/issues/40229")]
        public void TestCanReferenceInstanceMembersFromAttributeInNameof()
        {
            var source = @"
var p = new C().P; // 1
public class C
{
    [System.Obsolete(nameof(S.Length))]
    public int P { get; }
    public string S { get; }
}";
            var expectedDiagnostics = new[]
            {
                // (2,9): warning CS0618: 'C.P' is obsolete: 'Length'
                // var p = new C().P; // 1
                Diagnostic(ErrorCode.WRN_DeprecatedSymbolStr, "new C().P").WithArguments("C.P", "Length").WithLocation(2, 9)
            };
            CreateCompilation(source, parseOptions: TestOptions.Regular12).VerifyDiagnostics(expectedDiagnostics);
            CreateCompilation(source, parseOptions: TestOptions.RegularPreview).VerifyDiagnostics(expectedDiagnostics);
            CreateCompilation(source, parseOptions: TestOptions.Regular11).VerifyDiagnostics(
                // (5,29): error CS9058: Feature 'instance member in 'nameof'' is not available in C# 11.0. Please use language version 12.0 or greater.
                //     [System.Obsolete(nameof(S.Length))]
                Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion11, "S").WithArguments("instance member in 'nameof'", "12.0").WithLocation(5, 29));
        }

        [Fact, WorkItem(40229, "https://github.com/dotnet/roslyn/issues/40229")]
        public void TestCanReferenceInstanceMembersFromConstructorInitializersInNameof()
        {
            var source = @"
System.Console.WriteLine(new C().S);
public class C
{
    public C(string s){ S = s; }
    public C() : this(nameof(S.Length)){}
    public string S { get; }
}";
            CompileAndVerify(source, parseOptions: TestOptions.Regular12, expectedOutput: "Length").VerifyDiagnostics();
            CompileAndVerify(source, parseOptions: TestOptions.RegularPreview, expectedOutput: "Length").VerifyDiagnostics();
            CreateCompilation(source, parseOptions: TestOptions.Regular11).VerifyDiagnostics(
                // (6,30): error CS9058: Feature 'instance member in 'nameof'' is not available in C# 11.0. Please use language version 12.0 or greater.
                //     public C() : this(nameof(S.Length)){}
                Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion11, "S").WithArguments("instance member in 'nameof'", "12.0").WithLocation(6, 30));
        }

        [Fact, WorkItem(40229, "https://github.com/dotnet/roslyn/issues/40229")]
        public void TestCanAccessStructInstancePropertyInLambdaInNameof()
        {
            var source = @"
using System;

string s = ""str"";
new S().M(ref s);

public struct S
{
    public string P { get; }
    public void M(ref string x)
    {
        Func<string> func = () => nameof(P.Length);
        Console.WriteLine(func());
    }
}";
            CompileAndVerify(source, parseOptions: TestOptions.Regular12, expectedOutput: "Length").VerifyDiagnostics();
            CompileAndVerify(source, parseOptions: TestOptions.RegularPreview, expectedOutput: "Length").VerifyDiagnostics();
            CreateCompilation(source, parseOptions: TestOptions.Regular11).VerifyDiagnostics(
                // (12,42): error CS9058: Feature 'instance member in 'nameof'' is not available in C# 11.0. Please use language version 12.0 or greater.
                //         Func<string> func = () => nameof(P.Length);
                Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion11, "P").WithArguments("instance member in 'nameof'", "12.0").WithLocation(12, 42));
        }

        [Fact, WorkItem(40229, "https://github.com/dotnet/roslyn/issues/40229")]
        public void TestCanReferenceStaticMembersFromInstanceMemberInNameof1()
        {
            var source = @"
System.Console.WriteLine(new C().M());
public class C
{
    public C Prop { get; }
    public static int StaticProp { get; }
    public string M() => nameof(Prop.StaticProp);
}";
            CompileAndVerify(source, parseOptions: TestOptions.Regular12, expectedOutput: "StaticProp").VerifyDiagnostics();
            CompileAndVerify(source, parseOptions: TestOptions.RegularPreview, expectedOutput: "StaticProp").VerifyDiagnostics();
            CreateCompilation(source, parseOptions: TestOptions.Regular11).VerifyDiagnostics(
                // (7,33): error CS9058: Feature 'instance member in 'nameof'' is not available in C# 11.0. Please use language version 12.0 or greater.
                //     public string M() => nameof(Prop.StaticProp);
                Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion11, "Prop.StaticProp").WithArguments("instance member in 'nameof'", "12.0").WithLocation(7, 33));
        }

        [Fact, WorkItem(40229, "https://github.com/dotnet/roslyn/issues/40229")]
        public void TestCanReferenceStaticMembersFromInstanceMemberInNameof2()
        {
            var source = @"
System.Console.WriteLine(C.M());
public class C
{
    public C Prop { get; }
    public static int StaticProp { get; }
    public static string M() => nameof(Prop.StaticProp);
}";
            CompileAndVerify(source, parseOptions: TestOptions.Regular12, expectedOutput: "StaticProp").VerifyDiagnostics();
            CompileAndVerify(source, parseOptions: TestOptions.RegularPreview, expectedOutput: "StaticProp").VerifyDiagnostics();
            CreateCompilation(source, parseOptions: TestOptions.Regular11).VerifyDiagnostics(
                // (7,40): error CS9058: Feature 'instance member in 'nameof'' is not available in C# 11.0. Please use language version 12.0 or greater.
                //     public static string M() => nameof(Prop.StaticProp);
                Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion11, "Prop").WithArguments("instance member in 'nameof'", "12.0").WithLocation(7, 40),
                // (7,40): error CS9058: Feature 'instance member in 'nameof'' is not available in C# 11.0. Please use language version 12.0 or greater.
                //     public static string M() => nameof(Prop.StaticProp);
                Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion11, "Prop.StaticProp").WithArguments("instance member in 'nameof'", "12.0").WithLocation(7, 40));
        }

        [Fact, WorkItem(40229, "https://github.com/dotnet/roslyn/issues/40229")]
        public void TestCanReferenceStaticMembersFromInstanceMemberInNameof3()
        {
            var source = @"
System.Console.WriteLine(C.M());
public class C
{
    public C Prop { get; }
    public static string M() => nameof(Prop.M);
}";
            CompileAndVerify(source, parseOptions: TestOptions.Regular12, expectedOutput: "M").VerifyDiagnostics();
            CompileAndVerify(source, parseOptions: TestOptions.RegularPreview, expectedOutput: "M").VerifyDiagnostics();
            CreateCompilation(source, parseOptions: TestOptions.Regular11).VerifyDiagnostics(
                // (6,40): error CS9058: Feature 'instance member in 'nameof'' is not available in C# 11.0. Please use language version 12.0 or greater.
                //     public static string M() => nameof(Prop.M);
                Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion11, "Prop").WithArguments("instance member in 'nameof'", "12.0").WithLocation(6, 40));
        }

        [Fact, WorkItem(40229, "https://github.com/dotnet/roslyn/issues/40229")]
        public void TestCanReferenceStaticMembersFromInstanceMemberInNameof4()
        {
            var source = @"
System.Console.WriteLine(new C().M());
public class C
{
    public C Prop { get; }
    public static void StaticMethod(){}
    public string M() => nameof(Prop.StaticMethod);
}";
            CompileAndVerify(source, parseOptions: TestOptions.Regular12, expectedOutput: "StaticMethod").VerifyDiagnostics();
            CompileAndVerify(source, parseOptions: TestOptions.RegularPreview, expectedOutput: "StaticMethod").VerifyDiagnostics();
            CreateCompilation(source, parseOptions: TestOptions.Regular11).VerifyDiagnostics();
        }

        [Fact, WorkItem(40229, "https://github.com/dotnet/roslyn/issues/40229")]
        public void TestCannotReferenceInstanceMembersFromStaticMemberInNameofInCSharp11()
        {
            var source = @"
public class C
{
    public string S { get; }
    public static string M() => nameof(S.Length);
}";
            CreateCompilation(source, parseOptions: TestOptions.Regular11).VerifyDiagnostics(
                // (5,40): error CS9058: Feature 'instance member in 'nameof'' is not available in C# 11.0. Please use language version 12.0 or greater.
                //     public static string M() => nameof(S.Length);
                Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion11, "S").WithArguments("instance member in 'nameof'", "12.0").WithLocation(5, 40));
        }

        [Fact, WorkItem(40229, "https://github.com/dotnet/roslyn/issues/40229")]
        public void TestCannotReferenceInstanceMembersFromFieldInitializerInNameofInCSharp11()
        {
            var source = @"
public class C
{
    public string S { get; } = nameof(S.Length);
}";
            CreateCompilation(source, parseOptions: TestOptions.Regular11).VerifyDiagnostics(
                // (4,39): error CS9058: Feature 'instance member in 'nameof'' is not available in C# 11.0. Please use language version 12.0 or greater.
                //     public string S { get; } = nameof(S.Length);
                Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion11, "S").WithArguments("instance member in 'nameof'", "12.0").WithLocation(4, 39));
        }

        [Fact, WorkItem(40229, "https://github.com/dotnet/roslyn/issues/40229")]
        public void TestCannotReferenceInstanceMembersFromAttributeInNameofInCSharp11()
        {
            var source = @"
public class C
{
    [System.Obsolete(nameof(S.Length))]
    public int P { get; }
    public string S { get; }
}";
            CreateCompilation(source, parseOptions: TestOptions.Regular11).VerifyDiagnostics(
                // (4,29): error CS9058: Feature 'instance member in 'nameof'' is not available in C# 11.0. Please use language version 12.0 or greater.
                //     [System.Obsolete(nameof(S.Length))]
                Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion11, "S").WithArguments("instance member in 'nameof'", "12.0").WithLocation(4, 29));
        }

        [Fact, WorkItem(40229, "https://github.com/dotnet/roslyn/issues/40229")]
        public void TestCannotReferenceInstanceMembersFromConstructorInitializersInNameofInCSharp11()
        {
            var source = @"
public class C
{
    public C(string s){}
    public C() : this(nameof(S.Length)){}
    public string S { get; }
}";
            CreateCompilation(source, parseOptions: TestOptions.Regular11).VerifyDiagnostics(
                // (5,30): error CS9058: Feature 'instance member in 'nameof'' is not available in C# 11.0. Please use language version 12.0 or greater.
                //     public C() : this(nameof(S.Length)){}
                Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion11, "S").WithArguments("instance member in 'nameof'", "12.0").WithLocation(5, 30));
        }

        [Fact, WorkItem(40229, "https://github.com/dotnet/roslyn/issues/40229")]
        public void TestCannotAccessStructInstancePropertyInLambdaInNameofInCSharp11()
        {
            var source = @"
using System;

public struct S
{
    public string P { get; }
    public void M(ref string x)
    {
        Func<string> func = () => nameof(P.Length);
    }
}";
            CreateCompilation(source, parseOptions: TestOptions.Regular11).VerifyDiagnostics(
                // (9,42): error CS9058: Feature 'instance member in 'nameof'' is not available in C# 11.0. Please use language version 12.0 or greater.
                //         Func<string> func = () => nameof(P.Length);
                Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion11, "P").WithArguments("instance member in 'nameof'", "12.0").WithLocation(9, 42));
        }

        [Fact, WorkItem(40229, "https://github.com/dotnet/roslyn/issues/40229")]
        public void TestCannotReferenceStaticPropertyFromInstanceMemberInNameofInCSharp11()
        {
            var source = @"
public class C
{
    public C Prop { get; }
    public static int StaticProp { get; }
    public string M() => nameof(Prop.StaticProp);
}";
            CreateCompilation(source, parseOptions: TestOptions.Regular11).VerifyDiagnostics(
                // (6,33): error CS9058: Feature 'instance member in 'nameof'' is not available in C# 11.0. Please use language version 12.0 or greater.
                //     public string M() => nameof(Prop.StaticProp);
                Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion11, "Prop.StaticProp").WithArguments("instance member in 'nameof'", "12.0").WithLocation(6, 33));
        }

        [Fact]
        public void TestCanReferenceStaticMethodFromInstanceMemberInNameofInCSharp11()
        {
            var source = @"
System.Console.WriteLine(new C().M());
public class C
{
    public C Prop { get; }
    public static void StaticMethod(){}
    public string M() => nameof(Prop.StaticMethod);
}";
            CompileAndVerify(source, parseOptions: TestOptions.Regular11, expectedOutput: "StaticMethod").VerifyDiagnostics();
            CompileAndVerify(source, parseOptions: TestOptions.RegularPreview, expectedOutput: "StaticMethod").VerifyDiagnostics();
        }

        [Fact]
        public void TestCanAccessRefParameterInLambdaInNameof()
        {
            var source = @"
using System;

string s = ""str"";
new S().M(ref s);

public struct S
{
    public void M(ref string x)
    {
        Func<string> func = () => nameof(x.Length);
        Console.WriteLine(func());
    }
}";
            CompileAndVerify(source, parseOptions: TestOptions.Regular11, expectedOutput: "Length").VerifyDiagnostics();
            CompileAndVerify(source, parseOptions: TestOptions.RegularPreview, expectedOutput: "Length").VerifyDiagnostics();
        }

        [Fact, WorkItem(40229, "https://github.com/dotnet/roslyn/issues/40229")]
        public void TestCanReferenceStaticMembersFromInstanceMemberInNameofUsedRecursivelyInAttributes1()
        {
            var source = @"
using System;
using System.Reflection;
Console.WriteLine(typeof(C).GetProperty(""Prop"").GetCustomAttribute<Attr>().S);
class C
{
    [Attr(nameof(Prop.StaticMethod))]
    public C Prop { get; }
    public static void StaticMethod(){}
}
class Attr : Attribute
{
    public readonly string S;
    public Attr(string s) { S = s; }
}";
            CompileAndVerify(source, parseOptions: TestOptions.Regular12, expectedOutput: "StaticMethod").VerifyDiagnostics();
            CompileAndVerify(source, parseOptions: TestOptions.RegularPreview, expectedOutput: "StaticMethod").VerifyDiagnostics();
            CreateCompilation(source, parseOptions: TestOptions.Regular11).VerifyDiagnostics(
                // (7,18): error CS9058: Feature 'instance member in 'nameof'' is not available in C# 11.0. Please use language version 12.0 or greater.
                //     [Attr(nameof(Prop.StaticMethod))]
                Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion11, "Prop").WithArguments("instance member in 'nameof'", "12.0").WithLocation(7, 18));
        }

        [Fact, WorkItem(40229, "https://github.com/dotnet/roslyn/issues/40229")]
        public void TestCanReferenceStaticMembersFromInstanceMemberInNameofUsedRecursivelyInAttributes2()
        {
            var source = @"
using System;
using System.Reflection;
Console.WriteLine(typeof(C).GetProperty(""Prop"").GetCustomAttribute<Attr>().S);
class C
{
    [Attr(nameof(Prop.Prop))]
    public static C Prop { get; }
}
class Attr : Attribute
{
    public readonly string S;
    public Attr(string s) { S = s; }
}";
            CompileAndVerify(source, parseOptions: TestOptions.Regular12, expectedOutput: "Prop").VerifyDiagnostics();
            CompileAndVerify(source, parseOptions: TestOptions.RegularPreview, expectedOutput: "Prop").VerifyDiagnostics();
            CreateCompilation(source, parseOptions: TestOptions.Regular11).VerifyDiagnostics(
                // (7,18): error CS9058: Feature 'instance member in 'nameof'' is not available in C# 11.0. Please use language version 12.0 or greater.
                //     [Attr(nameof(Prop.Prop))]
                Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion11, "Prop.Prop").WithArguments("instance member in 'nameof'", "12.0").WithLocation(7, 18));
        }

        [Fact, WorkItem(40229, "https://github.com/dotnet/roslyn/issues/40229")]
        public void TestCanReferenceStaticMembersFromInstanceMemberInNameofUsedRecursivelyInAttributes3()
        {
            var source = @"
using System;
using System.Reflection;
Console.WriteLine(typeof(C).GetCustomAttribute<Attr>().S);
[Attr(nameof(C.Prop.Prop))]
class C
{
    public static C Prop { get; }
}
class Attr : Attribute
{
    public readonly string S;
    public Attr(string s) { S = s; }
}";
            CompileAndVerify(source, parseOptions: TestOptions.Regular12, expectedOutput: "Prop").VerifyDiagnostics();
            CompileAndVerify(source, parseOptions: TestOptions.RegularPreview, expectedOutput: "Prop").VerifyDiagnostics();
            CreateCompilation(source, parseOptions: TestOptions.Regular11).VerifyDiagnostics(
                // (5,14): error CS9058: Feature 'instance member in 'nameof'' is not available in C# 11.0. Please use language version 12.0 or greater.
                // [Attr(nameof(C.Prop.Prop))]
                Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion11, "C.Prop.Prop").WithArguments("instance member in 'nameof'", "12.0").WithLocation(5, 14));
        }

        [Fact, WorkItem(40229, "https://github.com/dotnet/roslyn/issues/40229")]
        public void TestInvalidRecursiveUsageOfNameofInAttributesDoesNotCrashCompiler1()
        {
            var source = @"
class C
{
    [Attr(nameof(Method().Method))]
    T Method<T>() where T : C => default;
}
class Attr : System.Attribute { public Attr(string s) {} }";
            var expectedDiagnostics = new[]
            {
                // (4,18): error CS0411: The type arguments for method 'C.Method<T>()' cannot be inferred from the usage. Try specifying the type arguments explicitly.
                //     [Attr(nameof(Method().Method))]
                Diagnostic(ErrorCode.ERR_CantInferMethTypeArgs, "Method").WithArguments("C.Method<T>()").WithLocation(4, 18)
            };
            CreateCompilation(source, parseOptions: TestOptions.Regular12).VerifyDiagnostics(expectedDiagnostics);
            CreateCompilation(source, parseOptions: TestOptions.RegularPreview).VerifyDiagnostics(expectedDiagnostics);
            CreateCompilation(source, parseOptions: TestOptions.Regular11).VerifyDiagnostics(expectedDiagnostics);
        }

        [Fact, WorkItem(40229, "https://github.com/dotnet/roslyn/issues/40229")]
        public void TestInvalidRecursiveUsageOfNameofInAttributesDoesNotCrashCompiler2()
        {
            var source = @"
class C
{
    [Attr(nameof(Method<C>().Method))]
    T Method<T>() where T : C => default;
}
class Attr : System.Attribute { public Attr(string s) {} }";
            var expectedDiagnostics = new[]
            {
                // (4,18): error CS8082: Sub-expression cannot be used in an argument to nameof.
                //     [Attr(nameof(Method<C>().Method))]
                Diagnostic(ErrorCode.ERR_SubexpressionNotInNameof, "Method<C>()").WithLocation(4, 18)
            };
            CreateCompilation(source, parseOptions: TestOptions.Regular12).VerifyDiagnostics(expectedDiagnostics);
            CreateCompilation(source, parseOptions: TestOptions.RegularPreview).VerifyDiagnostics(expectedDiagnostics);
            CreateCompilation(source, parseOptions: TestOptions.Regular11).VerifyDiagnostics(expectedDiagnostics);
        }

        [Fact]
        public void OpenTypeInNameof_Preview()
        {
            CompileAndVerify("""
                using System;
                using System.Collections.Generic;

                var v = nameof(List<>);
                Console.WriteLine(v);
                """, expectedOutput: "List").VerifyDiagnostics();
        }

        [Fact]
        public void OpenTypeInNameof_CSharp13()
        {
            CreateCompilation("""
                using System;
                using System.Collections.Generic;

                var v = nameof(List<>);
                Console.WriteLine(v);
                """, parseOptions: TestOptions.Regular13).VerifyDiagnostics(
                // (4,16): error CS9260: Feature 'unbound generic types in nameof operator' is not available in C# 13.0. Please use language version 14.0 or greater.
                // var v = nameof(List<>);
                Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion13, "List<>").WithArguments("unbound generic types in nameof operator", "14.0").WithLocation(4, 16));
        }

        [Fact]
        public void OpenTypeInNameof_Next()
        {
            CompileAndVerify("""
                using System;
                using System.Collections.Generic;

                var v = nameof(List<>);
                Console.WriteLine(v);
                """, parseOptions: TestOptions.Regular14, expectedOutput: "List").VerifyDiagnostics();
        }

        [Fact]
        public void OpenTypeInNameof_CSharp13_Nested1()
        {
            CreateCompilation("""
                using System;
                
                var v = nameof(A<>.B<int>);
                Console.WriteLine(v);

                class A<X> { public class B<Y>; }
                """, parseOptions: TestOptions.Regular13).VerifyDiagnostics(
                // (3,16): error CS9260: Feature 'unbound generic types in nameof operator' is not available in C# 13.0. Please use language version 14.0 or greater.
                // var v = nameof(A<>.B<int>);
                Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion13, "A<>").WithArguments("unbound generic types in nameof operator", "14.0").WithLocation(3, 16));
        }

        [Fact]
        public void OpenTypeInNameof_CSharp13_Nested2()
        {
            CreateCompilation("""
                using System;
                
                var v = nameof(A<int>.B<>);
                Console.WriteLine(v);

                class A<X> { public class B<Y>; }
                """, parseOptions: TestOptions.Regular13).VerifyDiagnostics(
                // (3,23): error CS9260: Feature 'unbound generic types in nameof operator' is not available in C# 13.0. Please use language version 14.0 or greater.
                // var v = nameof(A<int>.B<>);
                Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion13, "B<>").WithArguments("unbound generic types in nameof operator", "14.0").WithLocation(3, 23));
        }

        [Fact]
        public void OpenTypeInNameof_CSharp13_Nested3()
        {
            CreateCompilation("""
                using System;
                
                var v = nameof(A<>.B<>);
                Console.WriteLine(v);

                class A<X> { public class B<Y>; }
                """, parseOptions: TestOptions.Regular13).VerifyDiagnostics(
                // (3,16): error CS9260: Feature 'unbound generic types in nameof operator' is not available in C# 13.0. Please use language version 14.0 or greater.
                // var v = nameof(A<>.B<>);
                Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion13, "A<>").WithArguments("unbound generic types in nameof operator", "14.0").WithLocation(3, 16),
                // (3,20): error CS9260: Feature 'unbound generic types in nameof operator' is not available in C# 13.0. Please use language version 14.0 or greater.
                // var v = nameof(A<>.B<>);
                Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion13, "B<>").WithArguments("unbound generic types in nameof operator", "14.0").WithLocation(3, 20));
        }

        [Fact]
        public void OpenTypeInNameof_BaseCase()
        {
            CompileAndVerify("""
                using System;
                using System.Collections.Generic;

                var v = nameof(List<>);
                Console.WriteLine(v);
                """, expectedOutput: "List").VerifyDiagnostics();
        }

        [Fact]
        public void OpenTypeInNameof_Nested1()
        {
            CompileAndVerify("""
                using System;
                    
                var v = nameof(A<>.B<int>);
                Console.WriteLine(v);

                class A<X> { public class B<Y>; }
                """, expectedOutput: "B").VerifyDiagnostics();
        }

        [Fact]
        public void OpenTypeInNameof_Nested2()
        {
            CompileAndVerify("""
                using System;
                    
                var v = nameof(A<int>.B<>);
                Console.WriteLine(v);

                class A<X> { public class B<Y>; }
                """, expectedOutput: "B").VerifyDiagnostics();
        }

        [Fact]
        public void OpenTypeInNameof_Nested3()
        {
            CompileAndVerify("""
                using System;
                    
                var v = nameof(A<>.B<>);
                Console.WriteLine(v);

                class A<X> { public class B<Y>; }
                """, expectedOutput: "B").VerifyDiagnostics();
        }

        [Fact]
        public void OpenTypeInNameof_MultipleTypeArguments()
        {
            CompileAndVerify("""
                using System;
                using System.Collections.Generic;

                var v = nameof(Dictionary<,>);
                Console.WriteLine(v);
                """, expectedOutput: "Dictionary").VerifyDiagnostics();
        }

        [Fact]
        public void OpenTypeInNameof_IncorrectTypeArgumentCount1()
        {
            CreateCompilation("""
                using System;
                using System.Collections.Generic;

                var v = nameof(Dictionary<>);
                Console.WriteLine(v);
                """).VerifyDiagnostics(
                    // (2,1): hidden CS8019: Unnecessary using directive.
                    // using System.Collections.Generic;
                    Diagnostic(ErrorCode.HDN_UnusedUsingDirective, "using System.Collections.Generic;").WithLocation(2, 1),
                    // (4,16): error CS0305: Using the generic type 'Dictionary<TKey, TValue>' requires 2 type arguments
                    // var v = nameof(Dictionary<>);
                    Diagnostic(ErrorCode.ERR_BadArity, "Dictionary<>").WithArguments("System.Collections.Generic.Dictionary<TKey, TValue>", "type", "2").WithLocation(4, 16));
        }

        [Fact]
        public void OpenTypeInNameof_IncorrectTypeArgumentCount2()
        {
            CreateCompilation("""
                using System;
                using System.Collections.Generic;

                var v = nameof(List<,>);
                Console.WriteLine(v);
                """).VerifyDiagnostics(
                // (2,1): hidden CS8019: Unnecessary using directive.
                // using System.Collections.Generic;
                Diagnostic(ErrorCode.HDN_UnusedUsingDirective, "using System.Collections.Generic;").WithLocation(2, 1),
                // (4,16): error CS0305: Using the generic type 'List<T>' requires 1 type arguments
                // var v = nameof(List<,>);
                Diagnostic(ErrorCode.ERR_BadArity, "List<,>").WithArguments("System.Collections.Generic.List<T>", "type", "1").WithLocation(4, 16));
        }

        [Fact]
        public void OpenTypeInNameof_NoNestedOpenTypes1()
        {
            CreateCompilation("""
                using System;
                using System.Collections.Generic;

                var v = nameof(List<List<>>);
                Console.WriteLine(v);
                """).VerifyDiagnostics(
                    // (4,21): error CS7003: Unexpected use of an unbound generic name
                    // var v = nameof(List<List<>>);
                    Diagnostic(ErrorCode.ERR_UnexpectedUnboundGenericName, "List<>").WithLocation(4, 21));
        }

        [Fact]
        public void OpenTypeInNameof_NoNestedOpenTypes2()
        {
            CreateCompilation("""
                using System;
                using System.Collections.Generic;

                var v = nameof(List<List<>[]>);
                Console.WriteLine(v);
                """).VerifyDiagnostics(
                    // (4,21): error CS7003: Unexpected use of an unbound generic name
                    // var v = nameof(List<List<>>);
                    Diagnostic(ErrorCode.ERR_UnexpectedUnboundGenericName, "List<>").WithLocation(4, 21));
        }

        [Fact]
        public void OpenTypeInNameof_NoNestedOpenTypes3()
        {
            CreateCompilation("""
                using System;
                using System.Collections.Generic;

                var v = nameof(List<Outer<>.Inner>);
                Console.WriteLine(v);
                
                public class Outer<T> { public class Inner { } }
                """).VerifyDiagnostics(
                    // (4,21): error CS7003: Unexpected use of an unbound generic name
                    // var v = nameof(List<Outer<>.Inner>);
                    Diagnostic(ErrorCode.ERR_UnexpectedUnboundGenericName, "Outer<>").WithLocation(4, 21));
        }

        [Fact]
        public void OpenTypeInNameof_NoNestedOpenTypes4()
        {
            CreateCompilation("""
                using System;
                using System.Collections.Generic;

                var v = nameof(List<Outer.Inner<>>);
                Console.WriteLine(v);
                
                public class Outer { public class Inner<T> { } }
                """).VerifyDiagnostics(
                    // (4,27): error CS7003: Unexpected use of an unbound generic name
                    // var v = nameof(List<Outer.Inner<>>);
                    Diagnostic(ErrorCode.ERR_UnexpectedUnboundGenericName, "Inner<>").WithLocation(4, 27));
        }

        [Fact]
        public void Nameof_NestedClosedType1()
        {
            CompileAndVerify("""
                using System;
                using System.Collections.Generic;

                var v = nameof(List<List<int>>);
                Console.WriteLine(v);
                """, expectedOutput: "List").VerifyDiagnostics();
        }

        [Fact]
        public void Nameof_NestedClosedType2()
        {
            CompileAndVerify("""
                using System;
                using System.Collections.Generic;

                var v = nameof(List<List<int>[]>);
                Console.WriteLine(v);
                """, expectedOutput: "List").VerifyDiagnostics();
        }

        [Fact]
        public void OpenTypeInNameof_NoPartialOpenTypes_1()
        {
            CreateCompilation("""
                using System;
                using System.Collections.Generic;

                var v = nameof(Dictionary<,int>);
                Console.WriteLine(v);
                """).VerifyDiagnostics(
                    // (4,27): error CS1031: Type expected
                    // var v = nameof(Dictionary<,int>);
                    Diagnostic(ErrorCode.ERR_TypeExpected, ",").WithLocation(4, 27));
        }

        [Fact]
        public void OpenTypeInNameof_NoPartialOpenTypes_2()
        {
            CreateCompilation("""
                using System;
                using System.Collections.Generic;

                var v = nameof(Dictionary<int,>);
                Console.WriteLine(v);
                """).VerifyDiagnostics(
                // (4,31): error CS1031: Type expected
                // var v = nameof(Dictionary<int,>);
                Diagnostic(ErrorCode.ERR_TypeExpected, ">").WithLocation(4, 31));
        }

        [Fact]
        public void OpenTypeInNameof_MemberAccessThatDoesNotUseTypeArgument()
        {
            CompileAndVerify("""
                using System;
                using System.Collections.Generic;

                var v = nameof(List<>.Count);
                Console.WriteLine(v);
                """, expectedOutput: "Count").VerifyDiagnostics();
        }

        [Fact]
        public void OpenTypeInNameof_MemberAccessThatDoesUseTypeArgument()
        {
            CompileAndVerify("""
                using System;
                    
                var v = nameof(IGoo<>.Count);
                Console.WriteLine(v);

                interface IGoo<T>
                {
                    T Count { get; }
                }
                """, expectedOutput: "Count").VerifyDiagnostics();
        }

        [Fact]
        public void OpenTypeInNameof_MemberAccessThatDoesUseTypeArgument_ReferenceObjectMember()
        {
            CompileAndVerify("""
                using System;
                    
                var v = nameof(IGoo<>.Count.ToString);
                Console.WriteLine(v);

                interface IGoo<T>
                {
                    T Count { get; }
                }
                """, expectedOutput: "ToString").VerifyDiagnostics();
        }

        [Fact]
        public void OpenTypeInNameof_MemberAccessThatDoesUseTypeArgument_ReferenceConstraintMember_Interface()
        {
            CompileAndVerify("""
                using System;
                    
                var v = nameof(IGoo<>.X.CompareTo);
                Console.WriteLine(v);

                interface IGoo<T> where T : IComparable<T>
                {
                    T X { get; }
                }
                """, expectedOutput: "CompareTo").VerifyDiagnostics();
        }

        [Fact]
        public void OpenTypeInNameof_MemberAccessThatDoesUseTypeArgument_ReferenceConstraintMember_ThroughTypeParameter()
        {
            CompileAndVerify("""
                using System;
                    
                var v = nameof(IGoo<,>.X.CompareTo);
                Console.WriteLine(v);

                interface IGoo<T,U> where T : U where U : IComparable<T>
                {
                    T X { get; }
                }
                """, expectedOutput: "CompareTo").VerifyDiagnostics();
        }

        [Fact]
        public void OpenTypeInNameof_MemberAccessThatDoesUseTypeArgument_ReferenceConstraintMember_Class()
        {
            CompileAndVerify("""
                using System;
                    
                var v = nameof(IGoo<>.X.Z);
                Console.WriteLine(v);

                class Base
                {
                    public int Z { get; }
                }

                interface IGoo<T> where T : Base
                {
                    T X { get; }
                }
                """, expectedOutput: "Z").VerifyDiagnostics();
        }

        [Fact]
        public void OpenTypeInNameof_GenericMethod1()
        {
            CompileAndVerify("""
                using System;
                    
                var v = nameof(IGoo.M);
                Console.WriteLine(v);

                interface IGoo
                {
                    void M<T>();
                }
                """, expectedOutput: "M").VerifyDiagnostics();
        }

        [Fact]
        public void OpenTypeInNameof_GenericMethod2()
        {
            CreateCompilation("""
                using System;
                    
                var v = nameof(IGoo.M<>);
                Console.WriteLine(v);

                interface IGoo
                {
                    void M<T>();
                }
                """).VerifyDiagnostics(
                    // (3,16): error CS0305: Using the generic method group 'M' requires 1 type arguments
                    // var v = nameof(IGoo.M<>);
                    Diagnostic(ErrorCode.ERR_BadArity, "IGoo.M<>").WithArguments("M", "method group", "1").WithLocation(3, 16));
        }

        [Fact]
        public void OpenTypeInNameof_GenericMethod3()
        {
            CreateCompilation("""
                using System;
                    
                var v = nameof(IGoo.M<int>);
                Console.WriteLine(v);

                interface IGoo
                {
                    void M<T>();
                }
                """).VerifyDiagnostics(
                // (3,16): error CS8084: Type parameters are not allowed on a method group as an argument to 'nameof'.
                // var v = nameof(IGoo.M<int>);
                Diagnostic(ErrorCode.ERR_NameofMethodGroupWithTypeParameters, "IGoo.M<int>").WithLocation(3, 16));
        }

        [Fact]
        public void NameofFunctionPointer1()
        {
            CreateCompilation("""
                class C
                {
                    unsafe void M()
                    {
                        var v = nameof(delegate*<int>);
                    }
                }
                """, options: TestOptions.UnsafeReleaseDll).VerifyDiagnostics(
                // (5,32): error CS1514: { expected
                //         var v = nameof(delegate*<int>);
                Diagnostic(ErrorCode.ERR_LbraceExpected, "*").WithLocation(5, 32),
                // (5,32): warning CS8848: Operator '*' cannot be used here due to precedence. Use parentheses to disambiguate.
                //         var v = nameof(delegate*<int>);
                Diagnostic(ErrorCode.WRN_PrecedenceInversion, "*").WithArguments("*").WithLocation(5, 32),
                // (5,33): error CS1525: Invalid expression term '<'
                //         var v = nameof(delegate*<int>);
                Diagnostic(ErrorCode.ERR_InvalidExprTerm, "<").WithArguments("<").WithLocation(5, 33),
                // (5,34): error CS1525: Invalid expression term 'int'
                //         var v = nameof(delegate*<int>);
                Diagnostic(ErrorCode.ERR_InvalidExprTerm, "int").WithArguments("int").WithLocation(5, 34),
                // (5,38): error CS1525: Invalid expression term ')'
                //         var v = nameof(delegate*<int>);
                Diagnostic(ErrorCode.ERR_InvalidExprTerm, ")").WithArguments(")").WithLocation(5, 38));
        }

        [Fact]
        public void NameofFunctionPointer2()
        {
            CreateCompilation("""
                using System.Collections.Generic;
                    
                class C
                {
                    unsafe void M()
                    {
                        var v = nameof(delegate*<List<>>);
                    }
                }
                """, options: TestOptions.UnsafeReleaseDll).VerifyDiagnostics(
                // (7,32): error CS1514: { expected
                //         var v = nameof(delegate*<List<>>);
                Diagnostic(ErrorCode.ERR_LbraceExpected, "*").WithLocation(7, 32),
                // (7,32): warning CS8848: Operator '*' cannot be used here due to precedence. Use parentheses to disambiguate.
                //         var v = nameof(delegate*<List<>>);
                Diagnostic(ErrorCode.WRN_PrecedenceInversion, "*").WithArguments("*").WithLocation(7, 32),
                // (7,33): error CS1525: Invalid expression term '<'
                //         var v = nameof(delegate*<List<>>);
                Diagnostic(ErrorCode.ERR_InvalidExprTerm, "<").WithArguments("<").WithLocation(7, 33),
                // (7,34): error CS0119: 'List<T>' is a type, which is not valid in the given context
                //         var v = nameof(delegate*<List<>>);
                Diagnostic(ErrorCode.ERR_BadSKunknown, "List<>").WithArguments("System.Collections.Generic.List<T>", "type").WithLocation(7, 34),
                // (7,41): error CS1525: Invalid expression term ')'
                //         var v = nameof(delegate*<List<>>);
                Diagnostic(ErrorCode.ERR_InvalidExprTerm, ")").WithArguments(")").WithLocation(7, 41));
        }

        [Fact]
        public void NameofFunctionPointer3()
        {
            CreateCompilation("""
                using System.Collections.Generic;
                    
                class C
                {
                    unsafe void M()
                    {
                        var v = nameof(List<delegate*<int>>);
                    }
                }
                """, options: TestOptions.UnsafeReleaseDll).VerifyDiagnostics(
                // (7,13): warning CS0219: The variable 'v' is assigned but its value is never used
                //         var v = nameof(List<delegate*<int>>);
                Diagnostic(ErrorCode.WRN_UnreferencedVarAssg, "v").WithArguments("v").WithLocation(7, 13),
                // (7,29): error CS0306: The type 'delegate*<int>' may not be used as a type argument
                //         var v = nameof(List<delegate*<int>>);
                Diagnostic(ErrorCode.ERR_BadTypeArgument, "delegate*<int>").WithArguments("delegate*<int>").WithLocation(7, 29));
        }

        [Fact]
        public void NameofFunctionPointer4()
        {
            CreateCompilation("""
                using System.Collections.Generic;
                    
                class C
                {
                    unsafe void M()
                    {
                        var v = nameof(List<delegate*<List<>>>);
                    }
                }
                """, options: TestOptions.UnsafeReleaseDll).VerifyDiagnostics(
                // (7,13): warning CS0219: The variable 'v' is assigned but its value is never used
                //         var v = nameof(List<delegate*<List<>>>);
                Diagnostic(ErrorCode.WRN_UnreferencedVarAssg, "v").WithArguments("v").WithLocation(7, 13),
                // (7,29): error CS0306: The type 'delegate*<List<T>>' may not be used as a type argument
                //         var v = nameof(List<delegate*<List<>>>);
                Diagnostic(ErrorCode.ERR_BadTypeArgument, "delegate*<List<>>").WithArguments("delegate*<System.Collections.Generic.List<T>>").WithLocation(7, 29),
                // (7,39): error CS7003: Unexpected use of an unbound generic name
                //         var v = nameof(List<delegate*<List<>>>);
                Diagnostic(ErrorCode.ERR_UnexpectedUnboundGenericName, "List<>").WithLocation(7, 39));
        }

        [Fact]
        public void NameofFunctionPointer5()
        {
            CreateCompilation("""
                using System.Collections.Generic;
                
                class D<A, B, C>
                {
                    unsafe void M()
                    {
                        var v = nameof(D<, delegate*<int>, List<>>);
                    }
                }
                """, options: TestOptions.UnsafeReleaseDll).VerifyDiagnostics(
                // (7,26): error CS1031: Type expected
                //         var v = nameof(D<, delegate*<int>, List<>>);
                Diagnostic(ErrorCode.ERR_TypeExpected, ",").WithLocation(7, 26),
                // (7,44): error CS7003: Unexpected use of an unbound generic name
                //         var v = nameof(D<, delegate*<int>, List<>>);
                Diagnostic(ErrorCode.ERR_UnexpectedUnboundGenericName, "List<>").WithLocation(7, 44));
        }

        [Fact]
        public void Nameof_NestedOpenType1()
        {
            CompileAndVerify("""
                using System;
                using System.Collections.Generic;
                    
                var v = nameof(List<List<int>[]>);
                Console.WriteLine(v);
                """, expectedOutput: "List").VerifyDiagnostics();
        }

        [Fact]
        public void Nameof_NestedOpenType2()
        {
            CreateCompilation("""
                using System;
                using System.Collections.Generic;
                    
                var v = nameof(List<List<>[]>);
                Console.WriteLine(v);
                """).VerifyDiagnostics(
                // (4,21): error CS7003: Unexpected use of an unbound generic name
                // var v = nameof(List<List<>[]>);
                Diagnostic(ErrorCode.ERR_UnexpectedUnboundGenericName, "List<>").WithLocation(4, 21));
        }

        [Fact]
        public void Nameof_NestedOpenType3()
        {
            CompileAndVerify("""
                #nullable enable
                using System;
                using System.Collections.Generic;
                    
                var v = nameof(List<List<int>?>);
                Console.WriteLine(v);
                """, expectedOutput: "List").VerifyDiagnostics();
        }

        [Fact]
        public void Nameof_NestedOpenType4()
        {
            CreateCompilation("""
                #nullable enable
                using System;
                using System.Collections.Generic;
                    
                var v = nameof(List<List<>?>);
                Console.WriteLine(v);
                """).VerifyDiagnostics(
                // (5,21): error CS7003: Unexpected use of an unbound generic name
                // var v = nameof(List<List<>?>);
                Diagnostic(ErrorCode.ERR_UnexpectedUnboundGenericName, "List<>").WithLocation(5, 21));
        }

        [Fact]
        public void Nameof_AliasQualifiedName()
        {
            CompileAndVerify("""
                using System;
                    
                var v = nameof(global::System.Collections.Generic.List<>);
                Console.WriteLine(v);
                """, expectedOutput: "List").VerifyDiagnostics();
        }

        [Theory]
        [InlineData("IGoo<>")]
        [InlineData("IGoo<>.Count")]
        public void OpenTypeInNameof_SemanticModelTest1(string nameofTypeString)
        {
            var compilation = CreateCompilation($$"""
                using System;
                    
                var v1 = nameof({{nameofTypeString}});
                var v2 = typeof(IGoo<>);
                Console.WriteLine(v1 + v2);

                interface IGoo<T> { public T Count { get; } }
                """).VerifyDiagnostics();
            var tree = compilation.SyntaxTrees.Single();
            var semanticModel = compilation.GetSemanticModel(tree);

            var root = tree.GetRoot();

            var firstGeneric = root.DescendantNodes().OfType<GenericNameSyntax>().First();
            var lastGeneric = root.DescendantNodes().OfType<GenericNameSyntax>().Last();

            Assert.NotSame(firstGeneric, lastGeneric);

            // Ensure the type inside the nameof is the same as the type inside the typeof.
            var nameofType = semanticModel.GetTypeInfo(firstGeneric).Type;
            var typeofType = semanticModel.GetTypeInfo(lastGeneric).Type;

            Assert.NotNull(nameofType);
            Assert.NotNull(typeofType);

            // typeof will produce IGoo<>, while nameof will produce IGoo<T>.  These are distinctly different types (the
            // latter has members for example).
            Assert.NotEqual(nameofType, typeofType);

            Assert.True(nameofType.IsDefinition);
            Assert.False(nameofType.IsUnboundGenericType());

            Assert.False(typeofType.IsDefinition);
            Assert.True(typeofType.IsUnboundGenericType());

            Assert.Empty(typeofType.GetMembers("Count"));
            Assert.Single(nameofType.GetMembers("Count"));

            var igooType = compilation.GetTypeByMetadataName("IGoo`1").GetPublicSymbol();
            Assert.NotNull(igooType);

            Assert.Equal(igooType, nameofType);
        }

        [Fact]
        public void Nameof_Indexer_01()
        {
            string source = """
                using System.Collections.Generic;
                var d = new Dictionary<string, string>();
                _ = nameof(d[""]);
                """;
            var comp = CreateCompilation(source);
            comp.VerifyEmitDiagnostics(
                // (3,12): error CS8081: Expression does not have a name.
                // _ = nameof(d[""]);
                Diagnostic(ErrorCode.ERR_ExpressionHasNoName, @"d[""""]").WithLocation(3, 12));
        }

        [Fact]
        public void Nameof_Indexer_02()
        {
            string source = """
                var a = new object[1];
                _ = nameof(a[0]);
                _ = nameof(a[^1]);
                """;
            var comp = CreateCompilation(source, targetFramework: TargetFramework.Net70);
            comp.VerifyEmitDiagnostics(
                // (2,12): error CS8081: Expression does not have a name.
                // _ = nameof(a[0]);
                Diagnostic(ErrorCode.ERR_ExpressionHasNoName, "a[0]").WithLocation(2, 12),
                // (3,12): error CS8081: Expression does not have a name.
                // _ = nameof(a[^1]);
                Diagnostic(ErrorCode.ERR_ExpressionHasNoName, "a[^1]").WithLocation(3, 12));
        }

        [Fact]
        public void Nameof_Indexer_03()
        {
            string source = """
                using System;
                var s = new Span<int>();
                _ = nameof(s[0]);
                _ = nameof(s[^1]);
                """;
            var comp = CreateCompilation(source, targetFramework: TargetFramework.Net70);
            comp.VerifyEmitDiagnostics(
                // (3,12): error CS8081: Expression does not have a name.
                // _ = nameof(s[0]);
                Diagnostic(ErrorCode.ERR_ExpressionHasNoName, "s[0]").WithLocation(3, 12),
                // (4,12): error CS8081: Expression does not have a name.
                // _ = nameof(s[^1]);
                Diagnostic(ErrorCode.ERR_ExpressionHasNoName, "s[^1]").WithLocation(4, 12));
        }

        [Fact]
        public void Nameof_Indexer_04()
        {
            string source = """
                class C<T>
                {
                    public ref T this[int i] => throw null;
                }
                class Program
                {
                    static void Main()
                    {
                        var x = new C<object>();
                        _ = nameof(x[0]);
                        _ = nameof(x[0] = default);
                        var y = new C<int>();
                        _ = nameof(y[0] += 1);
                    }
                }
                """;
            var comp = CreateCompilation(source);
            comp.VerifyEmitDiagnostics(
                // (10,20): error CS8081: Expression does not have a name.
                //         _ = nameof(x[0]);
                Diagnostic(ErrorCode.ERR_ExpressionHasNoName, "x[0]").WithLocation(10, 20),
                // (11,20): error CS8081: Expression does not have a name.
                //         _ = nameof(x[0] = default);
                Diagnostic(ErrorCode.ERR_ExpressionHasNoName, "x[0] = default").WithLocation(11, 20),
                // (13,20): error CS8081: Expression does not have a name.
                //         _ = nameof(y[0] += 1);
                Diagnostic(ErrorCode.ERR_ExpressionHasNoName, "y[0] += 1").WithLocation(13, 20));
        }

        [Fact]
        public void Nameof_Indexer_05()
        {
            string source = """
                ref struct R<T>
                {
                    public T this[int i] => default;
                }
                class Program
                {
                    static void Main()
                    {
                        var r = new R<object>();
                        _ = nameof(r[0]);
                    }
                }
                """;
            var comp = CreateCompilation(source);
            comp.VerifyEmitDiagnostics(
                // (10,20): error CS8081: Expression does not have a name.
                //         _ = nameof(r[0]);
                Diagnostic(ErrorCode.ERR_ExpressionHasNoName, "r[0]").WithLocation(10, 20));
        }

        [Fact]
        public void Nameof_Indexer_06()
        {
            string source = """
                ref struct R<T>
                {
                    public T this[int i] { set { } }
                }
                class Program
                {
                    static void Main()
                    {
                        var r = new R<object>();
                        _ = nameof(r[0]);
                        _ = nameof(r[0] = default);
                    }
                }
                """;
            var comp = CreateCompilation(source);
            comp.VerifyEmitDiagnostics(
                // (10,20): error CS8081: Expression does not have a name.
                //         _ = nameof(r[0]);
                Diagnostic(ErrorCode.ERR_ExpressionHasNoName, "r[0]").WithLocation(10, 20),
                // (10,20): error CS0154: The property or indexer 'R<object>.this[int]' cannot be used in this context because it lacks the get accessor
                //         _ = nameof(r[0]);
                Diagnostic(ErrorCode.ERR_PropertyLacksGet, "r[0]").WithArguments("R<object>.this[int]").WithLocation(10, 20),
                // (11,20): error CS8081: Expression does not have a name.
                //         _ = nameof(r[0] = default);
                Diagnostic(ErrorCode.ERR_ExpressionHasNoName, "r[0] = default").WithLocation(11, 20));
        }

        [Fact]
        public void Nameof_Indexer_07()
        {
            string source = """
                using System.Diagnostics.CodeAnalysis;
                ref struct R<T>
                {
                    private ref readonly int _i;
                    public T this[[UnscopedRef] in int i] { get { _i = ref i; return default; } }
                }
                class Program
                {
                    static R<string> F(bool b)
                    {
                        var r = new R<string>();
                        _ = b ?
                            r[0] :
                            nameof(r[0]);
                        return r;
                    }
                }
                """;
            var comp = CreateCompilation(source, targetFramework: TargetFramework.Net70);
            comp.VerifyEmitDiagnostics(
                // (13,13): error CS8350: This combination of arguments to 'R<string>.this[in int]' is disallowed because it may expose variables referenced by parameter 'i' outside of their declaration scope
                //             r[0] :
                Diagnostic(ErrorCode.ERR_CallArgMixing, "r[0]").WithArguments("R<string>.this[in int]", "i").WithLocation(13, 13),
                // (13,15): error CS8156: An expression cannot be used in this context because it may not be passed or returned by reference
                //             r[0] :
                Diagnostic(ErrorCode.ERR_RefReturnLvalueExpected, "0").WithLocation(13, 15),
                // (14,20): error CS8081: Expression does not have a name.
                //             nameof(r[0]);
                Diagnostic(ErrorCode.ERR_ExpressionHasNoName, "r[0]").WithLocation(14, 20),
                // (14,20): error CS8350: This combination of arguments to 'R<string>.this[in int]' is disallowed because it may expose variables referenced by parameter 'i' outside of their declaration scope
                //             nameof(r[0]);
                Diagnostic(ErrorCode.ERR_CallArgMixing, "r[0]").WithArguments("R<string>.this[in int]", "i").WithLocation(14, 20),
                // (14,22): error CS8156: An expression cannot be used in this context because it may not be passed or returned by reference
                //             nameof(r[0]);
                Diagnostic(ErrorCode.ERR_RefReturnLvalueExpected, "0").WithLocation(14, 22));
        }
    }
}
