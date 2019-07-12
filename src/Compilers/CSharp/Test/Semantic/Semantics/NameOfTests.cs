// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.CSharp.Test.Utilities;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Test.Utilities;
using Xunit;
using System.Threading;
using System.Linq;

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
                // (13,20): error CS1026: ) expected
                //         s = nameof(void);
                Diagnostic(ErrorCode.ERR_CloseParenExpected, "void").WithLocation(13, 20),
                // (13,20): error CS1002: ; expected
                //         s = nameof(void);
                Diagnostic(ErrorCode.ERR_SemicolonExpected, "void").WithLocation(13, 20),
                // (13,20): error CS1547: Keyword 'void' cannot be used in this context
                //         s = nameof(void);
                Diagnostic(ErrorCode.ERR_NoVoidHere, "void").WithLocation(13, 20),
                // (13,24): error CS1001: Identifier expected
                //         s = nameof(void);
                Diagnostic(ErrorCode.ERR_IdentifierExpected, ")").WithLocation(13, 24),
                // (13,24): error CS1002: ; expected
                //         s = nameof(void);
                Diagnostic(ErrorCode.ERR_SemicolonExpected, ")").WithLocation(13, 24),
                // (13,24): error CS1513: } expected
                //         s = nameof(void);
                Diagnostic(ErrorCode.ERR_RbraceExpected, ")").WithLocation(13, 24),
                // (17,66): error CS1031: Type expected
                //         s = nameof(System.Collections.Generic.Dictionary<Program,>.KeyCollection);
                Diagnostic(ErrorCode.ERR_TypeExpected, ">").WithLocation(17, 66),
                // (11,27): error CS0305: Using the generic type 'Action<T>' requires 1 type arguments
                //         s = nameof(System.Action<>);
                Diagnostic(ErrorCode.ERR_BadArity, "Action<>").WithArguments("System.Action<T>", "type", "1").WithLocation(11, 27),
                // (13,13): error CS0103: The name 'nameof' does not exist in the current context
                //         s = nameof(void);
                Diagnostic(ErrorCode.ERR_NameNotInContext, "nameof").WithArguments("nameof").WithLocation(13, 13),
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
                Diagnostic(ErrorCode.ERR_GlobalSingleTypeNameNotFound, "Something").WithArguments("Something", "<global namespace>").WithLocation(25, 28),
                // (26,20): error CS0432: Alias 'global2' not found
                //         s = nameof(global2::Something);
                Diagnostic(ErrorCode.ERR_AliasNotFound, "global2").WithArguments("global2").WithLocation(26, 20),
                // (27,20): error CS0234: The type or namespace name 'Collections2' does not exist in the namespace 'System' (are you missing an assembly reference?)
                //         s = nameof(System.Collections2.Generic.List);
                Diagnostic(ErrorCode.ERR_DottedTypeNameNotFoundInNS, "System.Collections2").WithArguments("Collections2", "System").WithLocation(27, 20),
                // (28,20): error CS0103: The name 'List2' does not exist in the current context
                //         s = nameof(List2<>.Add);
                Diagnostic(ErrorCode.ERR_NameNotInContext, "List2<>").WithArguments("List2").WithLocation(28, 20),
                // (31,20): error CS8149: An alias-qualified name is not an expression.
                //         s = nameof(global::Program); // not an expression
                Diagnostic(ErrorCode.ERR_AliasQualifiedNameNotAnExpression, "global::Program").WithLocation(31, 20),
                // (32,20): error CS0305: Using the generic type 'Test<T>' requires 1 type arguments
                //         s = nameof(Test<>.s); // inaccessible
                Diagnostic(ErrorCode.ERR_BadArity, "Test<>").WithArguments("Test<T>", "type", "1").WithLocation(32, 20),
                // (32,27): error CS0122: 'Test<T>.s' is inaccessible due to its protection level
                //         s = nameof(Test<>.s); // inaccessible
                Diagnostic(ErrorCode.ERR_BadAccess, "s").WithArguments("Test<T>.s").WithLocation(32, 27),
                // (33,20): error CS0841: Cannot use local variable 'b' before it is declared
                //         s = nameof(b); // cannot use before declaration
                Diagnostic(ErrorCode.ERR_VariableUsedBeforeDeclaration, "b").WithArguments("b").WithLocation(33, 20),
                // (35,20): error CS8150: Type parameters are not allowed on a method group as an argument to 'nameof'.
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
                // (14,20): error CS8093: Extension method groups are not allowed as an argument to 'nameof'.
                //         Use(nameof(a.Extension));
                Diagnostic(ErrorCode.ERR_NameofExtensionMethod, "a.Extension").WithLocation(14, 20)
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
                // (16,20): error CS8093: Extension method groups are not allowed as an argument to 'nameof'.
                //         Use(nameof(a.Extension));
                Diagnostic(ErrorCode.ERR_NameofExtensionMethod, "a.Extension").WithLocation(16, 20)
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
            var compilation = CreateCompilationWithMscorlib45(source, options: TestOptions.DebugExe);
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
            var compilation = CreateCompilationWithMscorlib45(source).VerifyDiagnostics();
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
            var compilation = CreateCompilationWithMscorlib45(source, null, new CSharpCompilationOptions(OutputKind.ConsoleApplication).WithAllowUnsafe(true));
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
            var compilation = CreateCompilationWithMscorlib45(source, null,
                new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary).WithAllowUnsafe(true)).VerifyDiagnostics(
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
            var compilation = CreateCompilationWithMscorlib45(source, null, new CSharpCompilationOptions(OutputKind.ConsoleApplication).WithAllowUnsafe(true));
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
            var compilation = CreateCompilationWithMscorlib45(source, null, new CSharpCompilationOptions(OutputKind.ConsoleApplication).WithAllowUnsafe(true));
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
    }
}
