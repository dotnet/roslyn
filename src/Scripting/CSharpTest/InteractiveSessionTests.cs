// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Scripting;
using Microsoft.CodeAnalysis.Scripting.Hosting;
using Microsoft.CodeAnalysis.Scripting.Test;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.CSharp.Scripting.UnitTests
{
    using static TestCompilationFactory;

    public class HostModel
    {
        public readonly int Goo;
    }

    public class InteractiveSessionTests : TestBase
    {
        internal static readonly Assembly HostAssembly = typeof(InteractiveSessionTests).GetTypeInfo().Assembly;

        #region Namespaces, Types

        [Fact(Skip = "https://github.com/dotnet/roslyn/issues/17869")]
        public void CompilationChain_NestedTypesClass()
        {
            var script = CSharpScript.Create(@"
static string outerStr = null;
public static void Goo(string str) { outerStr = str; }
class InnerClass
{
   public string innerStr = null;
   public void Goo() { Goo(""test""); innerStr = outerStr; }       
}
").ContinueWith(@"
InnerClass iC = new InnerClass();
iC.Goo();
").ContinueWith(@"
System.Console.WriteLine(iC.innerStr);
");

            ScriptingTestHelpers.RunScriptWithOutput(script, "test");
        }

        [Fact(Skip = "https://github.com/dotnet/roslyn/issues/17869")]
        public void CompilationChain_NestedTypesStruct()
        {
            var script = CSharpScript.Create(@"
static string outerStr = null;
public static void Goo(string str) { outerStr = str; }
struct InnerStruct
{
   public string innerStr;
   public void Goo() { Goo(""test""); innerStr = outerStr; }            
}
").ContinueWith(@"
InnerStruct iS = new InnerStruct();     
iS.Goo();
").ContinueWith(@"
System.Console.WriteLine(iS.innerStr);
");

            ScriptingTestHelpers.RunScriptWithOutput(script, "test");
        }

        [Fact]
        public async Task CompilationChain_InterfaceTypes()
        {
            var script = CSharpScript.Create(@"
interface I1 { int Goo();}
class InnerClass : I1
{
  public int Goo() { return 1; }
}").ContinueWith(@"
I1 iC = new InnerClass();
").ContinueWith(@"
iC.Goo()
");

            Assert.Equal(1, await script.EvaluateAsync());
        }

        [Fact]
        public void ScriptMemberAccessFromNestedClass()
        {
            var script = CSharpScript.Create(@"
object field;
object Property { get; set; }
void Method() { }
").ContinueWith(@"
class C 
{
    public void Goo() 
    {
        object f = field;
        object p = Property;
        Method();
    }
}
");

            ScriptingTestHelpers.AssertCompilationError(script,
                // (6,20): error CS0120: An object reference is required for the non-static field, method, or property 'field'
                Diagnostic(ErrorCode.ERR_ObjectRequired, "field").WithArguments("field"),
                // (7,20): error CS0120: An object reference is required for the non-static field, method, or property 'Property'
                Diagnostic(ErrorCode.ERR_ObjectRequired, "Property").WithArguments("Property"),
                // (8,9): error CS0120: An object reference is required for the non-static field, method, or property 'Method()'
                Diagnostic(ErrorCode.ERR_ObjectRequired, "Method").WithArguments("Method()"));
        }

        #region Anonymous Types 

        [Fact]
        public void AnonymousTypes_TopLevel_MultipleSubmissions()
        {
            var script = CSharpScript.Create(@"
var a = new { f = 1 };
").ContinueWith(@"
var b = new { g = 1 };
").ContinueWith<Array>(@"
var c = new { f = 1 };
var d = new { g = 1 };
new object[] { new[] { a, c }, new[] { b, d } }
");

            var result = script.EvaluateAsync().Result;

            Assert.Equal(2, result.Length);
            Assert.Equal(2, ((Array)result.GetValue(0)).Length);
            Assert.Equal(2, ((Array)result.GetValue(1)).Length);
        }

        [Fact]
        public void AnonymousTypes_TopLevel_MultipleSubmissions2()
        {
            var script = CSharpScript.Create(@"
var a = new { f = 1 };
").ContinueWith(@"
var b = new { g = 1 };
").ContinueWith(@"
var c = new { f = 1 };
var d = new { g = 1 };

object.ReferenceEquals(a.GetType(), c.GetType()).ToString() + "" "" +
    object.ReferenceEquals(a.GetType(), b.GetType()).ToString() + "" "" + 
    object.ReferenceEquals(b.GetType(), d.GetType()).ToString()
");

            Assert.Equal("True False True", script.EvaluateAsync().Result.ToString());
        }

        [WorkItem(543863, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/543863")]
        [Fact]
        public void AnonymousTypes_Redefinition()
        {
            var script = CSharpScript.Create(@"
var x = new { Goo = ""goo"" };
").ContinueWith(@"
var x = new { Goo = ""goo"" };
").ContinueWith(@"
x.Goo
");

            var result = script.EvaluateAsync().Result;
            Assert.Equal("goo", result);
        }

        [Fact]
        public void AnonymousTypes_TopLevel_Empty()
        {
            var script = CSharpScript.Create(@"
var a = new { };
").ContinueWith(@"
var b = new { };
").ContinueWith<Array>(@"
var c = new { };
var d = new { };
new object[] { new[] { a, c }, new[] { b, d } }
");
            var result = script.EvaluateAsync().Result;

            Assert.Equal(2, result.Length);
            Assert.Equal(2, ((Array)result.GetValue(0)).Length);
            Assert.Equal(2, ((Array)result.GetValue(1)).Length);
        }

        #endregion

        #region Dynamic

        [Fact]
        public void Dynamic_Expando()
        {
            var options = ScriptOptions.Default.
                AddReferences(
                    typeof(Microsoft.CSharp.RuntimeBinder.RuntimeBinderException).GetTypeInfo().Assembly,
                    typeof(System.Dynamic.ExpandoObject).GetTypeInfo().Assembly).
                AddImports(
                    "System.Dynamic");

            var script = CSharpScript.Create(@"
dynamic expando = new ExpandoObject();  
", options).ContinueWith(@"
expando.goo = 1;
").ContinueWith(@"
expando.goo
");

            Assert.Equal(1, script.EvaluateAsync().Result);
        }

        #endregion

        [Fact]
        public void Enums()
        {
            var script = CSharpScript.Create(@"
public enum Enum1
{
    A, B, C
}
Enum1 E = Enum1.C;

E
");
            var e = script.EvaluateAsync().Result;

            Assert.True(e.GetType().GetTypeInfo().IsEnum, "Expected enum");
            Assert.Equal(typeof(int), Enum.GetUnderlyingType(e.GetType()));
        }

        #endregion

        #region Attributes

        [Fact]
        public void PInvoke()
        {
            var source = @"
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

[DllImport(""goo"", 
    EntryPoint = ""bar"", 
    CallingConvention = CallingConvention.Cdecl, 
    CharSet = CharSet.Unicode, 
    ExactSpelling = true, 
    PreserveSig = true,
    SetLastError = true, 
    BestFitMapping = true,
    ThrowOnUnmappableChar = true)]
public static extern void M();

class C { }

typeof(C)
";
            Type c = CSharpScript.EvaluateAsync<Type>(source).Result;
            var m = c.DeclaringType.GetTypeInfo().GetDeclaredMethod("M");
            Assert.Equal(MethodImplAttributes.PreserveSig, m.MethodImplementationFlags);

            // Reflection synthesizes DllImportAttribute
            var dllImport = (DllImportAttribute)m.GetCustomAttributes(typeof(DllImportAttribute), inherit: false).Single();
            Assert.True(dllImport.BestFitMapping);
            Assert.Equal(CallingConvention.Cdecl, dllImport.CallingConvention);
            Assert.Equal(CharSet.Unicode, dllImport.CharSet);
            Assert.True(dllImport.ExactSpelling);
            Assert.True(dllImport.SetLastError);
            Assert.True(dllImport.PreserveSig);
            Assert.True(dllImport.ThrowOnUnmappableChar);
            Assert.Equal("bar", dllImport.EntryPoint);
            Assert.Equal("goo", dllImport.Value);
        }

        #endregion

        // extension methods - must be private, can be top level

        #region Modifiers and Visibility

        [Fact]
        public void PrivateTopLevel()
        {
            var script = CSharpScript.Create<int>(@"
private int goo() { return 1; }
private static int bar() { return 10; }
private static int f = 100;

goo() + bar() + f
");
            Assert.Equal(111, script.EvaluateAsync().Result);

            script = script.ContinueWith<int>(@"
goo() + bar() + f
");

            Assert.Equal(111, script.EvaluateAsync().Result);

            script = script.ContinueWith<int>(@"
class C { public static int baz() { return bar() + f; } }

C.baz()
");

            Assert.Equal(110, script.EvaluateAsync().Result);
        }

        [Fact]
        public void NestedVisibility()
        {
            var script = CSharpScript.Create(@"
private class C 
{ 
    internal class D
    {
        internal static int goo() { return 1; } 
    }

    private class E
    {
        internal static int goo() { return 1; } 
    }

    public class F
    {
        internal protected static int goo() { return 1; } 
    }

    internal protected class G
    {
        internal static int goo() { return 1; } 
    }
}
");
            Assert.Equal(1, script.ContinueWith<int>("C.D.goo()").EvaluateAsync().Result);
            Assert.Equal(1, script.ContinueWith<int>("C.F.goo()").EvaluateAsync().Result);
            Assert.Equal(1, script.ContinueWith<int>("C.G.goo()").EvaluateAsync().Result);

            ScriptingTestHelpers.AssertCompilationError(script.ContinueWith<int>(@"C.E.goo()"),
                // error CS0122: 'C.E' is inaccessible due to its protection level
                Diagnostic(ErrorCode.ERR_BadAccess, "E").WithArguments("C.E"));
        }

        [Fact]
        public void Fields_Visibility()
        {
            var script = CSharpScript.Create(@"
private int i = 2;     // test comment;
public int j = 2;
protected int k = 2;
internal protected int l = 2;
internal int pi = 2;
").ContinueWith(@"
i = i + i;
j = j + j;
k = k + k;
l = l + l;
").ContinueWith(@"
pi = i + j + k + l;
");

            Assert.Equal(4, script.ContinueWith<int>("i").EvaluateAsync().Result);
            Assert.Equal(4, script.ContinueWith<int>("j").EvaluateAsync().Result);
            Assert.Equal(4, script.ContinueWith<int>("k").EvaluateAsync().Result);
            Assert.Equal(4, script.ContinueWith<int>("l").EvaluateAsync().Result);
            Assert.Equal(16, script.ContinueWith<int>("pi").EvaluateAsync().Result);
        }

        [WorkItem(100639, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/100639")]
        [Fact]
        public void ExternDestructor()
        {
            var script = CSharpScript.Create(
@"class C
{
    extern ~C();
}");
            Assert.Null(script.EvaluateAsync().Result);
        }

        #endregion

        #region Chaining

        [Fact]
        public void CompilationChain_BasicFields()
        {
            var script = CSharpScript.Create("var x = 1;").ContinueWith("x");
            Assert.Equal(1, script.EvaluateAsync().Result);
        }

        [Fact]
        public void CompilationChain_GlobalNamespaceAndUsings()
        {
            var result =
                CSharpScript.Create("using InteractiveFixtures.C;", ScriptOptions.Default.AddReferences(HostAssembly)).
                ContinueWith("using InteractiveFixtures.C;").
                ContinueWith("System.Environment.ProcessorCount").
                EvaluateAsync().Result;

            Assert.Equal(Environment.ProcessorCount, result);
        }

        [Fact]
        public void CompilationChain_CurrentSubmissionUsings()
        {
            var s0 = CSharpScript.RunAsync("", ScriptOptions.Default.AddReferences(HostAssembly));

            var state = s0.
                ContinueWith("class X { public int goo() { return 1; } }").
                ContinueWith("class X { public int goo() { return 1; } }").
                ContinueWith("using InteractiveFixtures.A;").
                ContinueWith("new X().goo()");

            Assert.Equal(1, state.Result.ReturnValue);

            state =
                s0.
                ContinueWith("class X { public int goo() { return 1; } }").
                ContinueWith(@"
using InteractiveFixtures.A;
new X().goo()
");

            Assert.Equal(1, state.Result.ReturnValue);
        }

        [Fact]
        public void CompilationChain_UsingDuplicates()
        {
            var script = CSharpScript.Create(@"
using System;
using System;
").ContinueWith(@"
using System;
using System;
").ContinueWith(@"
Environment.ProcessorCount
");

            Assert.Equal(Environment.ProcessorCount, script.EvaluateAsync().Result);
        }

        [Fact]
        public void CompilationChain_GlobalImports()
        {
            var options = ScriptOptions.Default.AddImports("System");

            var state = CSharpScript.RunAsync("Environment.ProcessorCount", options);
            Assert.Equal(Environment.ProcessorCount, state.Result.ReturnValue);

            state = state.ContinueWith("Environment.ProcessorCount");
            Assert.Equal(Environment.ProcessorCount, state.Result.ReturnValue);
        }

        [Fact]
        public void CompilationChain_Accessibility()
        {
            // Submissions have internal and protected access to one another.
            var state1 = CSharpScript.RunAsync("internal class C1 { }   protected int X;   1");
            var compilation1 = state1.Result.Script.GetCompilation();
            compilation1.VerifyDiagnostics(
                // (1,39): warning CS0628: 'X': new protected member declared in sealed class
                // internal class C1 { }   protected int X;   1
                Diagnostic(ErrorCode.WRN_ProtectedInSealed, "X").WithArguments("X").WithLocation(1, 39)
                );
            Assert.Equal(1, state1.Result.ReturnValue);

            var state2 = state1.ContinueWith("internal class C2 : C1 { }   2");
            var compilation2 = state2.Result.Script.GetCompilation();
            compilation2.VerifyDiagnostics();
            Assert.Equal(2, state2.Result.ReturnValue);
            var c2C2 = (INamedTypeSymbol)lookupMember(compilation2, "Submission#1", "C2");
            var c2C1 = c2C2.BaseType;
            var c2X = lookupMember(compilation1, "Submission#0", "X");
            Assert.True(compilation2.IsSymbolAccessibleWithin(c2C1, c2C2));
            Assert.True(compilation2.IsSymbolAccessibleWithin(c2C2, c2C1));
            Assert.True(compilation2.IsSymbolAccessibleWithin(c2X, c2C2));  // access not enforced among submission symbols

            var state3 = state2.ContinueWith("private class C3 : C2 { }   3");
            var compilation3 = state3.Result.Script.GetCompilation();
            compilation3.VerifyDiagnostics();
            Assert.Equal(3, state3.Result.ReturnValue);
            var c3C3 = (INamedTypeSymbol)lookupMember(compilation3, "Submission#2", "C3");
            var c3C1 = c3C3.BaseType;
            Assert.Throws<ArgumentException>(() => compilation2.IsSymbolAccessibleWithin(c3C3, c3C1));
            Assert.True(compilation3.IsSymbolAccessibleWithin(c3C3, c3C1));

            INamedTypeSymbol lookupType(Compilation c, string name)
            {
                return c.GlobalNamespace.GetMembers(name).Single() as INamedTypeSymbol;
            }
            ISymbol lookupMember(Compilation c, string typeName, string memberName)
            {
                return lookupType(c, typeName).GetMembers(memberName).Single();
            }
        }

        [Fact]
        public void CompilationChain_SubmissionSlotResize()
        {
            var state = CSharpScript.RunAsync("");

            for (int i = 0; i < 17; i++)
            {
                state = state.ContinueWith(@"public int i =  1;");
            }

            ScriptingTestHelpers.ContinueRunScriptWithOutput(state, @"System.Console.WriteLine(i);", "1");
        }

        [Fact]
        public void CompilationChain_UsingNotHidingPreviousSubmission()
        {
            int result1 =
                CSharpScript.Create("using System;").
                ContinueWith("int Environment = 1;").
                ContinueWith<int>("Environment").
                EvaluateAsync().Result;

            Assert.Equal(1, result1);

            int result2 =
                CSharpScript.Create("int Environment = 1;").
                ContinueWith("using System;").
                ContinueWith<int>("Environment").
                EvaluateAsync().Result;

            Assert.Equal(1, result2);
        }

        [Fact]
        public void CompilationChain_DefinitionHidesGlobal()
        {
            var result =
                CSharpScript.Create("int System = 1;").
                ContinueWith("System").
                EvaluateAsync().Result;

            Assert.Equal(1, result);
        }

        public class C1
        {
            public readonly int System = 1;
            public readonly int Environment = 2;
        }

        /// <summary>
        /// Symbol declaration in host object model hides global definition.
        /// </summary>
        [Fact]
        public void CompilationChain_HostObjectMembersHidesGlobal()
        {
            var result =
                CSharpScript.RunAsync("System", globals: new C1()).
                Result.ReturnValue;

            Assert.Equal(1, result);
        }

        [Fact]
        public void CompilationChain_UsingNotHidingHostObjectMembers()
        {
            var result =
                CSharpScript.RunAsync("using System;", globals: new C1()).
                ContinueWith("Environment").
                Result.ReturnValue;

            Assert.Equal(2, result);
        }

        [Fact]
        public void CompilationChain_DefinitionHidesHostObjectMembers()
        {
            var result =
                CSharpScript.RunAsync("int System = 2;", globals: new C1()).
                ContinueWith("System").
                Result.ReturnValue;

            Assert.Equal(2, result);
        }

        [Fact]
        public void Submissions_ExecutionOrder1()
        {
            var s0 = CSharpScript.Create("int x = 1;");
            var s1 = s0.ContinueWith("int y = 2;");
            var s2 = s1.ContinueWith<int>("x + y");

            Assert.Equal(3, s2.EvaluateAsync().Result);
            Assert.Null(s1.EvaluateAsync().Result);
            Assert.Null(s0.EvaluateAsync().Result);

            Assert.Equal(3, s2.EvaluateAsync().Result);
            Assert.Null(s1.EvaluateAsync().Result);
            Assert.Null(s0.EvaluateAsync().Result);

            Assert.Equal(3, s2.EvaluateAsync().Result);
            Assert.Equal(3, s2.EvaluateAsync().Result);
        }

        [Fact]
        public async Task Submissions_ExecutionOrder2()
        {
            var s0 = await CSharpScript.RunAsync("int x = 1;");

            Assert.Throws<CompilationErrorException>(() => s0.ContinueWithAsync("invalid$syntax").Result);

            var s1 = await s0.ContinueWithAsync("x = 2; x = 10");

            Assert.Throws<CompilationErrorException>(() => s1.ContinueWithAsync("invalid$syntax").Result);
            Assert.Throws<CompilationErrorException>(() => s1.ContinueWithAsync("x = undefined_symbol").Result);

            var s2 = await s1.ContinueWithAsync("int y = 2;");

            Assert.Null(s2.ReturnValue);

            var s3 = await s2.ContinueWithAsync("x + y");
            Assert.Equal(12, s3.ReturnValue);
        }

        public class HostObjectWithOverrides
        {
            public override bool Equals(object obj) => true;
            public override int GetHashCode() => 1234567;
            public override string ToString() => "HostObjectToString impl";
        }

        [Fact]
        public async Task ObjectOverrides1()
        {
            var state0 = await CSharpScript.RunAsync("", globals: new HostObjectWithOverrides());

            var state1 = await state0.ContinueWithAsync<bool>("Equals(null)");
            Assert.True(state1.ReturnValue);

            var state2 = await state1.ContinueWithAsync<int>("GetHashCode()");
            Assert.Equal(1234567, state2.ReturnValue);

            var state3 = await state2.ContinueWithAsync<string>("ToString()");
            Assert.Equal("HostObjectToString impl", state3.ReturnValue);
        }

        [Fact]
        public async Task ObjectOverrides2()
        {
            var state0 = await CSharpScript.RunAsync("", globals: new object());
            var state1 = await state0.ContinueWithAsync<bool>(@"
object x = 1;
object y = x;
ReferenceEquals(x, y)");

            Assert.True(state1.ReturnValue);

            var state2 = await state1.ContinueWithAsync<string>("ToString()");
            Assert.Equal("System.Object", state2.ReturnValue);

            var state3 = await state2.ContinueWithAsync<bool>("Equals(null)");
            Assert.False(state3.ReturnValue);
        }

        [Fact]
        public void ObjectOverrides3()
        {
            var state0 = CSharpScript.RunAsync("");

            var src1 = @"
Equals(null);
GetHashCode();
ToString();
ReferenceEquals(null, null);";

            ScriptingTestHelpers.AssertCompilationError(state0, src1,
                // (2,1): error CS0103: The name 'Equals' does not exist in the current context
                Diagnostic(ErrorCode.ERR_NameNotInContext, "Equals").WithArguments("Equals"),
                // (3,1): error CS0103: The name 'GetHashCode' does not exist in the current context
                Diagnostic(ErrorCode.ERR_NameNotInContext, "GetHashCode").WithArguments("GetHashCode"),
                // (4,1): error CS0103: The name 'ToString' does not exist in the current context
                Diagnostic(ErrorCode.ERR_NameNotInContext, "ToString").WithArguments("ToString"),
                // (5,1): error CS0103: The name 'ReferenceEquals' does not exist in the current context
                Diagnostic(ErrorCode.ERR_NameNotInContext, "ReferenceEquals").WithArguments("ReferenceEquals"));

            var src2 = @"
public override string ToString() { return null; }
";

            ScriptingTestHelpers.AssertCompilationError(state0, src2,
                // (1,24): error CS0115: 'ToString()': no suitable method found to override
                Diagnostic(ErrorCode.ERR_OverrideNotExpected, "ToString").WithArguments("ToString()"));
        }

        #endregion

        #region Generics

        [Fact, WorkItem(201759, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/201759")]
        public void CompilationChain_GenericTypes()
        {
            var script = CSharpScript.Create(@"
class InnerClass<T> 
{
    public int method(int value) { return value + 1; }            
    public int field = 2;
}").ContinueWith(@"
InnerClass<int> iC = new InnerClass<int>();
").ContinueWith(@"
iC.method(iC.field)
");

            Assert.Equal(3, script.EvaluateAsync().Result);
        }

        [WorkItem(529243, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/529243")]
        [Fact]
        public void RecursiveBaseType()
        {
            CSharpScript.EvaluateAsync(@"
class A<T> { }
class B<T> : A<B<B<T>>> { }
");
        }

        [WorkItem(5378, "DevDiv_Projects/Roslyn")]
        [Fact]
        public void CompilationChain_GenericMethods()
        {
            var s0 = CSharpScript.Create(@"
public int goo<T, R>(T arg) { return 1; }

public static T bar<T>(T i)
{
   return i;
}
");

            Assert.Equal(1, s0.ContinueWith(@"goo<int, int>(1)").EvaluateAsync().Result);
            Assert.Equal(5, s0.ContinueWith(@"bar(5)").EvaluateAsync().Result);
        }

        /// <summary>
        /// Tests that we emit ldftn and ldvirtftn instructions correctly.
        /// </summary>
        [Fact]
        public void CompilationChain_Ldftn()
        {
            var state = CSharpScript.RunAsync(@"
public class C 
{
   public static int f() { return 1; }
   public int g() { return 10; }
   public virtual int h() { return 100; }

   public static int gf<T>() { return 2; }
   public int gg<T>() { return 20; }
   public virtual int gh<T>() { return 200; }
}
");
            state = state.ContinueWith(@"
new System.Func<int>(C.f)() +
new System.Func<int>(new C().g)() +
new System.Func<int>(new C().h)()"
);
            Assert.Equal(111, state.Result.ReturnValue);

            state = state.ContinueWith(@"
new System.Func<int>(C.gf<int>)() +
new System.Func<int>(new C().gg<object>)() +
new System.Func<int>(new C().gh<bool>)()
");
            Assert.Equal(222, state.Result.ReturnValue);
        }

        /// <summary>
        /// Tests that we emit ldftn and ldvirtftn instructions correctly.
        /// </summary>
        [Fact]
        public void CompilationChain_Ldftn_GenericType()
        {
            var state = CSharpScript.RunAsync(@"
public class C<S>
{
   public static int f() { return 1; }
   public int g() { return 10; }
   public virtual int h() { return 100; }

   public static int gf<T>() { return 2; }
   public int gg<T>() { return 20; }
   public virtual int gh<T>() { return 200; }
}
");
            state = state.ContinueWith(@"
new System.Func<int>(C<byte>.f)() +
new System.Func<int>(new C<byte>().g)() +
new System.Func<int>(new C<byte>().h)()
");

            Assert.Equal(111, state.Result.ReturnValue);

            state = state.ContinueWith(@"
new System.Func<int>(C<byte>.gf<int>)() +
new System.Func<int>(new C<byte>().gg<object>)() +
new System.Func<int>(new C<byte>().gh<bool>)()
");
            Assert.Equal(222, state.Result.ReturnValue);
        }

        #endregion

        #region Statements and Expressions

        [Fact]
        public void IfStatement()
        {
            var result = CSharpScript.EvaluateAsync<int>(@"
using static System.Console;
int x;

if (true)
{
    x = 5;
} 
else
{
    x = 6;
}

x
").Result;

            Assert.Equal(5, result);
        }

        [Fact]
        public void ExprStmtParenthesesUsedToOverrideDefaultEval()
        {
            Assert.Equal(18, CSharpScript.EvaluateAsync<int>("(4 + 5) * 2").Result);
            Assert.Equal(1, CSharpScript.EvaluateAsync<long>("6 / (2 * 3)").Result);
        }

        [WorkItem(5397, "DevDiv_Projects/Roslyn")]
        [Fact]
        public void TopLevelLambda()
        {
            var s = CSharpScript.RunAsync(@"
using System;
delegate void TestDelegate(string s);
");

            s = s.ContinueWith(@"
TestDelegate testDelB = delegate (string s) { Console.WriteLine(s); };
");

            ScriptingTestHelpers.ContinueRunScriptWithOutput(s, @"testDelB(""hello"");", "hello");
        }

        [Fact]
        public void Closure()
        {
            var f = CSharpScript.EvaluateAsync<Func<int, int>>(@"
int Goo(int arg) { return arg + 1; }

System.Func<int, int> f = (arg) =>
{
    return Goo(arg);
};

f
").Result;
            Assert.Equal(3, f(2));
        }

        [Fact]
        public void Closure2()
        {
            var result = CSharpScript.EvaluateAsync<List<string>>(@"
#r ""System.Core""
using System;
using System.Linq;
using System.Collections.Generic;

List<string> result = new List<string>();
string s = ""hello"";
Enumerable.ToList(Enumerable.Range(1, 2)).ForEach(x => result.Add(s));
result
").Result;
            AssertEx.Equal(new[] { "hello", "hello" }, result);
        }

        [Fact]
        public void UseDelegateMixStaticAndDynamic()
        {
            var f = CSharpScript.RunAsync("using System;").
                ContinueWith("int Sqr(int x) {return x*x;}").
                ContinueWith<Func<int, int>>("new Func<int,int>(Sqr)").Result.ReturnValue;

            Assert.Equal(4, f(2));
        }

        [WorkItem(9229, "DevDiv_Projects/Roslyn")]
        [Fact]
        public void Arrays()
        {
            var s = CSharpScript.RunAsync(@"
int[] arr_1 = { 1, 2, 3 };
int[] arr_2 = new int[] { 1, 2, 3 };
int[] arr_3 = new int[5];
").ContinueWith(@"
arr_2[0] = 5;
");

            Assert.Equal(3, s.ContinueWith(@"arr_1[2]").Result.ReturnValue);
            Assert.Equal(5, s.ContinueWith(@"arr_2[0]").Result.ReturnValue);
            Assert.Equal(0, s.ContinueWith(@"arr_3[0]").Result.ReturnValue);
        }

        [Fact]
        public void FieldInitializers()
        {
            var result = CSharpScript.EvaluateAsync<List<int>>(@"
using System.Collections.Generic;
static List<int> result = new List<int>();
int b = 2;
int a;
int x = 1, y = b;
static int g = 1;
static int f = g + 1;
a = x + y;
result.Add(a);
int z = 4 + f;
result.Add(z);
result.Add(a * z);
result
").Result;
            Assert.Equal(3, result.Count);
            Assert.Equal(3, result[0]);
            Assert.Equal(6, result[1]);
            Assert.Equal(18, result[2]);
        }

        [Fact]
        public void FieldInitializersWithBlocks()
        {
            var result = CSharpScript.EvaluateAsync<List<int>>(@"
using System.Collections.Generic;
static List<int> result = new List<int>();
const int constant = 1;
{
    int x = constant;
    result.Add(x);
}
int field = 2;
{
    int x = field;
    result.Add(x);
}
result.Add(constant);
result.Add(field);
result
").Result;
            Assert.Equal(4, result.Count);
            Assert.Equal(1, result[0]);
            Assert.Equal(2, result[1]);
            Assert.Equal(1, result[2]);
            Assert.Equal(2, result[3]);
        }

        [Fact]
        public void TestInteractiveClosures()
        {
            var result = CSharpScript.RunAsync(@"
using System.Collections.Generic;
static List<int> result = new List<int>();").
            ContinueWith("int x = 1;").
            ContinueWith("System.Func<int> f = () => x++;").
            ContinueWith("result.Add(f());").
            ContinueWith("result.Add(x);").
            ContinueWith<List<int>>("result").Result.ReturnValue;

            Assert.Equal(2, result.Count);
            Assert.Equal(1, result[0]);
            Assert.Equal(2, result[1]);
        }

        [Fact]
        public void ExtensionMethods()
        {
            var options = ScriptOptions.Default.AddReferences(
                typeof(Enumerable).GetTypeInfo().Assembly);

            var result = CSharpScript.EvaluateAsync<int>(@"
using System.Linq;
string[] fruit = { ""banana"", ""orange"", ""lime"", ""apple"", ""kiwi"" };
fruit.Skip(1).Where(s => s.Length > 4).Count()", options).Result;

            Assert.Equal(2, result);
        }

        [Fact]
        public void ImplicitlyTypedFields()
        {
            var result = CSharpScript.EvaluateAsync<object[]>(@"
var x = 1;
var y = x;
var z = goo(x);

string goo(int a) { return null; } 
int goo(string a) { return 0; }

new object[] { x, y, z }
").Result;
            AssertEx.Equal(new object[] { 1, 1, null }, result);
        }

        /// <summary>
        /// Name of PrivateImplementationDetails type needs to be unique across submissions.
        /// The compiler should suffix it with a MVID of the current submission module so we should be fine.
        /// </summary>
        [WorkItem(949559, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/949559")]
        [WorkItem(540237, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/540237")]
        [WorkItem(9229, "DevDiv_Projects/Roslyn")]
        [WorkItem(2721, "https://github.com/dotnet/roslyn/issues/2721")]
        [Fact]
        public async Task PrivateImplementationDetailsType()
        {
            var result1 = await CSharpScript.EvaluateAsync<int[]>("new int[] { 1,2,3,4 }");
            AssertEx.Equal(new[] { 1, 2, 3, 4 }, result1);

            var result2 = await CSharpScript.EvaluateAsync<int[]>("new int[] { 1,2,3,4,5  }");
            AssertEx.Equal(new[] { 1, 2, 3, 4, 5 }, result2);

            var s1 = await CSharpScript.RunAsync<int[]>("new int[] { 1,2,3,4,5,6  }");
            AssertEx.Equal(new[] { 1, 2, 3, 4, 5, 6 }, s1.ReturnValue);

            var s2 = await s1.ContinueWithAsync<int[]>("new int[] { 1,2,3,4,5,6,7  }");
            AssertEx.Equal(new[] { 1, 2, 3, 4, 5, 6, 7 }, s2.ReturnValue);

            var s3 = await s2.ContinueWithAsync<int[]>("new int[] { 1,2,3,4,5,6,7,8  }");
            AssertEx.Equal(new[] { 1, 2, 3, 4, 5, 6, 7, 8 }, s3.ReturnValue);
        }

        [Fact]
        public void NoAwait()
        {
            // No await. The return value is Task<int> rather than int.
            var result = CSharpScript.EvaluateAsync("System.Threading.Tasks.Task.FromResult(1)").Result;
            Assert.Equal(1, ((Task<int>)result).Result);
        }

        /// <summary>
        /// 'await' expression at top-level.
        /// </summary>
        [Fact]
        public void Await()
        {
            Assert.Equal(2, CSharpScript.EvaluateAsync("await System.Threading.Tasks.Task.FromResult(2)").Result);
        }

        /// <summary>
        /// 'await' in sub-expression.
        /// </summary>
        [Fact]
        public void AwaitSubExpression()
        {
            Assert.Equal(3, CSharpScript.EvaluateAsync<int>("0 + await System.Threading.Tasks.Task.FromResult(3)").Result);
        }

        [Fact]
        public void AwaitVoid()
        {
            var task = CSharpScript.EvaluateAsync<object>("await System.Threading.Tasks.Task.Run(() => { })");
            Assert.Null(task.Result);
            Assert.Equal(TaskStatus.RanToCompletion, task.Status);
        }

        /// <summary>
        /// 'await' in lambda should be ignored.
        /// </summary>
        [Fact]
        public async Task AwaitInLambda()
        {
            var s0 = await CSharpScript.RunAsync(@"
using System;
using System.Threading.Tasks;
static T F<T>(Func<Task<T>> f)
{
    return f().Result;
}
static T G<T>(T t, Func<T, Task<T>> f)
{
    return f(t).Result;
}");

            var s1 = await s0.ContinueWithAsync("F(async () => await Task.FromResult(4))");
            Assert.Equal(4, s1.ReturnValue);

            var s2 = await s1.ContinueWithAsync("G(5, async x => await Task.FromResult(x))");
            Assert.Equal(5, s2.ReturnValue);
        }

        [Fact]
        public void AwaitChain1()
        {
            var options = ScriptOptions.Default.
                AddReferences(typeof(Task).GetTypeInfo().Assembly).
                AddImports("System.Threading.Tasks");

            var state =
                CSharpScript.RunAsync("int i = 0;", options).
                ContinueWith("await Task.Delay(1); i++;").
                ContinueWith("await Task.Delay(1); i++;").
                ContinueWith("await Task.Delay(1); i++;").
                ContinueWith("i").
                Result;

            Assert.Equal(3, state.ReturnValue);
        }

        [Fact]
        public void AwaitChain2()
        {
            var options = ScriptOptions.Default.
                AddReferences(typeof(Task).GetTypeInfo().Assembly).
                AddImports("System.Threading.Tasks");

            var state =
                CSharpScript.Create("int i = 0;", options).
                ContinueWith("await Task.Delay(1); i++;").
                ContinueWith("await Task.Delay(1); i++;").
                RunAsync().
                ContinueWith("await Task.Delay(1); i++;").
                ContinueWith("i").
                Result;

            Assert.Equal(3, state.ReturnValue);
        }

        #endregion

        #region References

        [Fact]
        public void ReferenceDirective_FileWithDependencies()
        {
            var file1 = Temp.CreateFile();
            var file2 = Temp.CreateFile();

            var lib1 = CreateCSharpCompilationWithCorlib(@"
public interface I
{
    int F();
}");

            lib1.Emit(file1.Path);

            var lib2 = CreateCSharpCompilation(@" 
public class C : I
{
    public int F() => 1;
}", new MetadataReference[] { TestReferences.NetStandard13.SystemRuntime, lib1.ToMetadataReference() });

            lib2.Emit(file2.Path);

            object result = CSharpScript.EvaluateAsync($@"
#r ""{file1.Path}""
#r ""{file2.Path}""
new C()
").Result;
            Assert.NotNull(result);
        }

        [ConditionalFact(typeof(WindowsOnly)), WorkItem(15860, "https://github.com/dotnet/roslyn/issues/15860")]
        public void ReferenceDirective_RelativeToBaseParent()
        {
            var file = Temp.CreateFile();
            var lib = CreateCSharpCompilationWithCorlib("public class C {}");
            lib.Emit(file.Path);

            string dir = Path.Combine(Path.GetDirectoryName(file.Path), "subdir");
            string libFileName = Path.GetFileName(file.Path);
            string scriptPath = Path.Combine(dir, "a.csx");

            var script = CSharpScript.Create(
                $@"#r ""{Path.Combine("..", libFileName)}""",
                ScriptOptions.Default.WithFilePath(scriptPath));

            script.GetCompilation().VerifyDiagnostics();
        }

        [ConditionalFact(typeof(WindowsOnly)), WorkItem(15860, "https://github.com/dotnet/roslyn/issues/15860")]
        public void ReferenceDirective_RelativeToBaseRoot()
        {
            var file = Temp.CreateFile();
            var lib = CreateCSharpCompilationWithCorlib("public class C {}");
            lib.Emit(file.Path);

            string root = Path.GetPathRoot(file.Path);
            string unrooted = file.Path.Substring(root.Length);

            string dir = Path.Combine(root, "goo", "bar", "baz");
            string scriptPath = Path.Combine(dir, "a.csx");

            var script = CSharpScript.Create(
                $@"#r ""\{unrooted}""",
                ScriptOptions.Default.WithFilePath(scriptPath));

            script.GetCompilation().VerifyDiagnostics();
        }

        [Fact]
        public void ExtensionPriority1()
        {
            string mainName = "Main_" + Guid.NewGuid();
            string libName = "Lib_" + Guid.NewGuid();

            var libExe = CreateCSharpCompilationWithCorlib(@"public class C { public string F = ""exe""; }", libName);
            var libDll = CreateCSharpCompilationWithCorlib(@"public class C { public string F = ""dll""; }", libName);
            var libWinmd = CreateCSharpCompilationWithCorlib(@"public class C { public string F = ""winmd""; }", libName);

            var main = CreateCSharpCompilation(
                @"public static class M { public static readonly C X = new C(); }",
                new MetadataReference[] { TestReferences.NetStandard13.SystemRuntime, libExe.ToMetadataReference() },
                mainName);

            var exeImage = libExe.EmitToArray();
            var dllImage = libDll.EmitToArray();
            var winmdImage = libWinmd.EmitToArray();
            var mainImage = main.EmitToArray();

            var dir = Temp.CreateDirectory();
            var fileMain = dir.CreateFile(mainName + ".dll").WriteAllBytes(mainImage);

            dir.CreateFile(libName + ".exe").WriteAllBytes(exeImage);
            dir.CreateFile(libName + ".winmd").WriteAllBytes(winmdImage);

            var r2 = CSharpScript.Create($@"#r ""{fileMain.Path}""").ContinueWith($@"M.X.F").RunAsync().Result.ReturnValue;
            Assert.Equal("exe", r2);
        }

        [Fact]
        public void ExtensionPriority2()
        {
            string mainName = "Main_" + Guid.NewGuid();
            string libName = "Lib_" + Guid.NewGuid();

            var libExe = CreateCSharpCompilationWithCorlib(@"public class C { public string F = ""exe""; }", libName);
            var libDll = CreateCSharpCompilationWithCorlib(@"public class C { public string F = ""dll""; }", libName);
            var libWinmd = CreateCSharpCompilationWithCorlib(@"public class C { public string F = ""winmd""; }", libName);

            var main = CreateCSharpCompilation(
                @"public static class M { public static readonly C X = new C(); }",
                new MetadataReference[] { TestReferences.NetStandard13.SystemRuntime, libExe.ToMetadataReference() },
                mainName);

            var exeImage = libExe.EmitToArray();
            var dllImage = libDll.EmitToArray();
            var winmdImage = libWinmd.EmitToArray();
            var mainImage = main.EmitToArray();

            var dir = Temp.CreateDirectory();
            var fileMain = dir.CreateFile(mainName + ".dll").WriteAllBytes(mainImage);

            dir.CreateFile(libName + ".exe").WriteAllBytes(exeImage);
            dir.CreateFile(libName + ".dll").WriteAllBytes(dllImage);
            dir.CreateFile(libName + ".winmd").WriteAllBytes(winmdImage);

            var r2 = CSharpScript.Create($@"#r ""{fileMain.Path}""").ContinueWith($@"M.X.F").RunAsync().Result.ReturnValue;
            Assert.Equal("dll", r2);
        }

        [Fact(Skip = "https://github.com/dotnet/roslyn/issues/6015")]
        public void UsingExternalAliasesForHiding()
        {
            string source = @"
namespace N { public class C { } }
public class D { }
public class E { }
";

            var libRef = CreateCSharpCompilationWithCorlib(source, "lib").EmitToImageReference();

            var script = CSharpScript.Create(@"new C()",
                ScriptOptions.Default.WithReferences(libRef.WithAliases(new[] { "Hidden" })).WithImports("Hidden::N"));

            script.Compile().Verify();
        }

        #endregion

        #region UsingDeclarations

        [Fact]
        public void UsingAlias()
        {
            object result = CSharpScript.EvaluateAsync(@"
using D = System.Collections.Generic.Dictionary<string, int>;
D d = new D();

d
").Result;
            Assert.True(result is Dictionary<string, int>, "Expected Dictionary<string, int>");
        }

        [WorkItem(9229, "DevDiv_Projects/Roslyn")]
        [Fact]
        public void Usings1()
        {
            var options = ScriptOptions.Default.
                AddImports("System", "System.Linq").
                AddReferences(typeof(Enumerable).GetTypeInfo().Assembly);

            object result = CSharpScript.EvaluateAsync("new int[] { 1, 2, 3 }.First()", options).Result;
            Assert.Equal(1, result);
        }

        [WorkItem(9229, "DevDiv_Projects/Roslyn")]
        [Fact]
        public void Usings2()
        {
            var options = ScriptOptions.Default.
                 AddImports("System", "System.Linq").
                 AddReferences(typeof(Enumerable).GetTypeInfo().Assembly);

            var s1 = CSharpScript.RunAsync("new int[] { 1, 2, 3 }.First()", options);
            Assert.Equal(1, s1.Result.ReturnValue);

            var s2 = s1.ContinueWith("new List<int>()", options.AddImports("System.Collections.Generic"));
            Assert.IsType<List<int>>(s2.Result.ReturnValue);
        }

        [Fact]
        public void AddNamespaces_Errors()
        {
            // no immediate error, error is reported if the namespace can't be found when compiling:
            var options = ScriptOptions.Default.AddImports("?1", "?2");

            ScriptingTestHelpers.AssertCompilationError(() => CSharpScript.EvaluateAsync("1", options),
                // error CS0246: The type or namespace name '?1' could not be found (are you missing a using directive or an assembly reference?)
                Diagnostic(ErrorCode.ERR_SingleTypeNameNotFound).WithArguments("?1"),
                // error CS0246: The type or namespace name '?2' could not be found (are you missing a using directive or an assembly reference?)
                Diagnostic(ErrorCode.ERR_SingleTypeNameNotFound).WithArguments("?2"));

            options = ScriptOptions.Default.AddImports("");

            ScriptingTestHelpers.AssertCompilationError(() => CSharpScript.EvaluateAsync("1", options),
                // error CS7088: Invalid 'Usings' value: ''.
                Diagnostic(ErrorCode.ERR_BadCompilationOptionValue).WithArguments("Usings", ""));

            options = ScriptOptions.Default.AddImports(".abc");

            ScriptingTestHelpers.AssertCompilationError(() => CSharpScript.EvaluateAsync("1", options),
                // error CS7088: Invalid 'Usings' value: '.abc'.
                Diagnostic(ErrorCode.ERR_BadCompilationOptionValue).WithArguments("Usings", ".abc"));

            options = ScriptOptions.Default.AddImports("a\0bc");

            ScriptingTestHelpers.AssertCompilationError(() => CSharpScript.EvaluateAsync("1", options),
                // error CS7088: Invalid 'Usings' value: '.abc'.
                Diagnostic(ErrorCode.ERR_BadCompilationOptionValue).WithArguments("Usings", "a\0bc"));
        }

        #endregion

        #region Host Object Binding and Conversions

        public class C<T>
        {
        }

        [Fact]
        public void Submission_HostConversions()
        {
            Assert.Equal(2, CSharpScript.EvaluateAsync<int>("1+1").Result);

            Assert.Null(CSharpScript.EvaluateAsync<string>("null").Result);

            try
            {
                CSharpScript.RunAsync<C<int>>("null");
                Assert.True(false, "Expected an exception");
            }
            catch (CompilationErrorException e)
            {
                // error CS0400: The type or namespace name 'Microsoft.CodeAnalysis.CSharp.UnitTests.Symbols.Source.InteractiveSessionTests+C`1[[System.Int32, mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089]], Roslyn.Compilers.CSharp.Emit.UnitTests, Version=42.42.42.42, Culture=neutral, PublicKeyToken=fc793a00266884fb' could not be found in the global namespace (are you missing an assembly reference?)
                Assert.Equal(ErrorCode.ERR_GlobalSingleTypeNameNotFound, (ErrorCode)e.Diagnostics.Single().Code);
                // Can't use Verify() because the version number of the test dll is different in the build lab.
            }

            var options = ScriptOptions.Default.AddReferences(HostAssembly);

            var cint = CSharpScript.EvaluateAsync<C<int>>("null", options).Result;
            Assert.Null(cint);

            Assert.Null(CSharpScript.EvaluateAsync<int?>("null", options).Result);

            try
            {
                CSharpScript.RunAsync<int>("null");
                Assert.True(false, "Expected an exception");
            }
            catch (CompilationErrorException e)
            {
                e.Diagnostics.Verify(
                    // (1,1): error CS0037: Cannot convert null to 'int' because it is a non-nullable value type
                    // null
                    Diagnostic(ErrorCode.ERR_ValueCantBeNull, "null").WithArguments("int"));
            }

            try
            {
                CSharpScript.RunAsync<string>("1+1");
                Assert.True(false, "Expected an exception");
            }
            catch (CompilationErrorException e)
            {
                e.Diagnostics.Verify(
                    // (1,1): error CS0029: Cannot implicitly convert type 'int' to 'string'
                    // 1+1
                    Diagnostic(ErrorCode.ERR_NoImplicitConv, "1+1").WithArguments("int", "string"));
            }
        }

        [Fact]
        public void Submission_HostVarianceConversions()
        {
            var value = CSharpScript.EvaluateAsync<IEnumerable<Exception>>(@"
using System;
using System.Collections.Generic;
new List<ArgumentException>()
").Result;

            Assert.Null(value.FirstOrDefault());
        }

        public class B
        {
            public int x = 1, w = 4;
        }

        public class C : B, I
        {
            public static readonly int StaticField = 123;
            public int Y => 2;
            public string N { get; set; } = "2";
            public int Z() => 3;
            public override int GetHashCode() => 123;
        }

        public interface I
        {
            string N { get; set; }
            int Z();
        }

        private class PrivateClass : I
        {
            public string N { get; set; } = null;
            public int Z() => 3;
        }

        public class M<T>
        {
            private int F() => 3;
            public T G() => default(T);
        }

        [Fact]
        public void HostObjectBinding_PublicClassMembers()
        {
            var c = new C();

            var s0 = CSharpScript.RunAsync<int>("x + Y + Z()", globals: c);
            Assert.Equal(6, s0.Result.ReturnValue);

            var s1 = s0.ContinueWith<int>("x");
            Assert.Equal(1, s1.Result.ReturnValue);

            var s2 = s1.ContinueWith<int>("int x = 20;");

            var s3 = s2.ContinueWith<int>("x");
            Assert.Equal(20, s3.Result.ReturnValue);
        }

        [Fact]
        public void HostObjectBinding_PublicGenericClassMembers()
        {
            var m = new M<string>();
            var result = CSharpScript.EvaluateAsync<string>("G()", globals: m);
            Assert.Null(result.Result);
        }

        [Fact]
        public async Task HostObjectBinding_Interface()
        {
            var c = new C();

            var s0 = await CSharpScript.RunAsync<int>("Z()", globals: c, globalsType: typeof(I));
            Assert.Equal(3, s0.ReturnValue);

            ScriptingTestHelpers.AssertCompilationError(s0, @"x + Y",
                // The name '{0}' does not exist in the current context
                Diagnostic(ErrorCode.ERR_NameNotInContext, "x").WithArguments("x"),
                Diagnostic(ErrorCode.ERR_NameNotInContext, "Y").WithArguments("Y"));

            var s1 = await s0.ContinueWithAsync<string>("N");
            Assert.Equal("2", s1.ReturnValue);
        }

        [Fact]
        public void HostObjectBinding_PrivateClass()
        {
            var c = new PrivateClass();

            ScriptingTestHelpers.AssertCompilationError(() => CSharpScript.EvaluateAsync("Z()", globals: c),
                // (1,1): error CS0122: '<Fully Qualified Name of PrivateClass>.Z()' is inaccessible due to its protection level
                Diagnostic(ErrorCode.ERR_BadAccess, "Z").WithArguments(typeof(PrivateClass).FullName.Replace("+", ".") + ".Z()"));
        }

        [Fact]
        public void HostObjectBinding_PrivateMembers()
        {
            object c = new M<int>();

            ScriptingTestHelpers.AssertCompilationError(() => CSharpScript.EvaluateAsync("Z()", globals: c),
                // (1,1): error CS0103: The name 'z' does not exist in the current context
                Diagnostic(ErrorCode.ERR_NameNotInContext, "Z").WithArguments("Z"));
        }

        [Fact]
        public void HostObjectBinding_PrivateClassImplementingPublicInterface()
        {
            var c = new PrivateClass();
            var result = CSharpScript.EvaluateAsync<int>("Z()", globals: c, globalsType: typeof(I));
            Assert.Equal(3, result.Result);
        }

        [Fact]
        public void HostObjectBinding_StaticMembers()
        {
            var s0 = CSharpScript.RunAsync("static int goo = StaticField;", globals: new C());
            var s1 = s0.ContinueWith("static int bar { get { return goo; } }");
            var s2 = s1.ContinueWith("class C { public static int baz() { return bar; } }");
            var s3 = s2.ContinueWith("C.baz()");

            Assert.Equal(123, s3.Result.ReturnValue);
        }

        public class D
        {
            public int goo(int a) { return 0; }
        }

        /// <summary>
        /// Host object members don't form a method group with submission members.
        /// </summary>
        [Fact]
        public void HostObjectBinding_Overloads()
        {
            var s0 = CSharpScript.RunAsync("int goo(double a) { return 2; }", globals: new D());
            var s1 = s0.ContinueWith("goo(1)");
            Assert.Equal(2, s1.Result.ReturnValue);

            var s2 = s1.ContinueWith("goo(1.0)");
            Assert.Equal(2, s2.Result.ReturnValue);
        }

        [Fact]
        public void HostObjectInRootNamespace()
        {
            var obj = new InteractiveFixtures_TopLevelHostObject { X = 1, Y = 2, Z = 3 };
            var r0 = CSharpScript.EvaluateAsync<int>("X + Y + Z", globals: obj);
            Assert.Equal(6, r0.Result);

            obj = new InteractiveFixtures_TopLevelHostObject { X = 1, Y = 2, Z = 3 };
            var r1 = CSharpScript.EvaluateAsync<int>("X", globals: obj);
            Assert.Equal(1, r1.Result);
        }

        [ConditionalFact(typeof(ClrOnly), Reason = "https://github.com/dotnet/roslyn/issues/30303")]
        public void HostObjectAssemblyReference1()
        {
            var scriptCompilation = CSharpScript.Create(
                "nameof(Microsoft.CodeAnalysis.Scripting)",
                ScriptOptions.Default.WithMetadataResolver(TestRuntimeMetadataReferenceResolver.Instance),
                globalsType: typeof(CommandLineScriptGlobals)).GetCompilation();

            scriptCompilation.VerifyDiagnostics(
                // (1,8): error CS0234: The type or namespace name 'CodeAnalysis' does not exist in the namespace 'Microsoft' (are you missing an assembly reference?)
                // nameof(Microsoft.CodeAnalysis.Scripting)
                Diagnostic(ErrorCode.ERR_DottedTypeNameNotFoundInNS, "Microsoft.CodeAnalysis").WithArguments("CodeAnalysis", "Microsoft").WithLocation(1, 8));

            string corAssemblyName = typeof(object).GetTypeInfo().Assembly.GetName().Name;
            string hostObjectAssemblyName = scriptCompilation.ScriptCompilationInfo.GlobalsType.GetTypeInfo().Assembly.GetName().Name;

            // The host adds 
            // 1) a reference to typeof(object).Assembly
            // 2) a reference to GlobalsType with alias <host> applied recursively.
            // References returned from ResolveMissingAssembly have <implicit> alias.

            foreach (var (assembly, aliases) in scriptCompilation.GetBoundReferenceManager().GetReferencedAssemblyAliases())
            {
                string name = assembly.Identity.Name;

                switch (name)
                {
                    case "Microsoft.CodeAnalysis.CSharp.Scripting":
                    case "Microsoft.CodeAnalysis.CSharp":
                        // assemblies not referenced
                        Assert.False(true);
                        break;

                    case "Microsoft.CodeAnalysis":
                    case "System.Collections.Immutable":
                        // Microsoft.CodeAnalysis.Scripting contains host object and is thus referenced recursively with <host> alias. 
                        // The script doesn't reference the assemblies explicitly.
                        AssertEx.SetEqual(new[] { "<implicit>", "<host>" }, aliases);
                        break;

                    default:
                        if (name == corAssemblyName)
                        {
                            // Host object depends on System.Object, thus <host> is applied on CorLib.
                            AssertEx.SetEqual(new[] { "<host>", "global" }, aliases);
                        }
                        else if (name == hostObjectAssemblyName)
                        {
                            // Host object is only referenced by the host and thus the assembly is hidden behind <host> alias.
                            AssertEx.SetEqual(new[] { "<host>" }, aliases);
                        }

                        break;
                }
            }
        }

        [ConditionalFact(typeof(ClrOnly), Reason = "https://github.com/dotnet/roslyn/issues/30303")]
        public void HostObjectAssemblyReference2()
        {
            var scriptCompilation = CSharpScript.Create(
                "typeof(Microsoft.CodeAnalysis.Scripting.Script)",
                options: ScriptOptions.Default.
                    WithMetadataResolver(TestRuntimeMetadataReferenceResolver.Instance).
                    WithReferences(typeof(CSharpScript).GetTypeInfo().Assembly),
                globalsType: typeof(CommandLineScriptGlobals)).GetCompilation();

            scriptCompilation.VerifyDiagnostics();

            string corAssemblyName = typeof(object).GetTypeInfo().Assembly.GetName().Name;
            string hostObjectAssemblyName = scriptCompilation.ScriptCompilationInfo.GlobalsType.GetTypeInfo().Assembly.GetName().Name;

            // The host adds 
            // 1) a reference to typeof(object).Assembly
            // 2) a reference to GlobalsType with alias <host> applied recursively.
            // References returned from ResolveMissingAssembly have <implicit> alias.

            foreach (var (assembly, aliases) in scriptCompilation.GetBoundReferenceManager().GetReferencedAssemblyAliases())
            {
                string name = assembly.Identity.Name;

                switch (name)
                {
                    case "Microsoft.CodeAnalysis.CSharp.Scripting":
                        // we have an explicit reference to CSharpScript assembly:
                        AssertEx.SetEqual(Array.Empty<string>(), aliases);
                        break;

                    case "Microsoft.CodeAnalysis.CSharp":
                        // The script has a recursive reference to Microsoft.CodeAnalysis.CSharp.Scripting, which references these assemblies.
                        // The script doesn't reference the assembly explicitly.
                        AssertEx.SetEqual(new[] { "<implicit>", "global" }, aliases);
                        break;

                    case "Microsoft.CodeAnalysis":
                    case "System.Collections.Immutable":
                        // The script has a recursive reference to Microsoft.CodeAnalysis.CSharp.Scripting, which references these assemblies.
                        // Microsoft.CodeAnalysis.Scripting contains host object and is thus referenced recursively with <host> alias. 
                        // The script doesn't reference the assemblies explicitly.
                        AssertEx.SetEqual(new[] { "<implicit>", "<host>", "global" }, aliases);
                        break;

                    default:
                        if (name == corAssemblyName)
                        {
                            // Host object depends on System.Object, thus <host> is applied on CorLib.
                            AssertEx.SetEqual(new[] { "<host>", "global" }, aliases);
                        }
                        else if (name == hostObjectAssemblyName)
                        {
                            // Host object is defined in an assembly that CSharpScript depends on.
                            // CSharpScript assembly was references (recursively) hence host object assembly is 
                            // available to the script (global alias).
                            AssertEx.SetEqual(new[] { "<host>", "global" }, aliases);
                        }

                        break;
                }
            }
        }

        [ConditionalFact(typeof(ClrOnly), Reason = "https://github.com/dotnet/roslyn/issues/30303")]
        public void HostObjectAssemblyReference3()
        {
            string source = $@"
#r ""{typeof(CSharpScript).GetTypeInfo().Assembly.ManifestModule.FullyQualifiedName}""
typeof(Microsoft.CodeAnalysis.Scripting.Script)
";
            var scriptCompilation = CSharpScript.Create(
                source,
                ScriptOptions.Default.WithMetadataResolver(TestRuntimeMetadataReferenceResolver.Instance),
                globalsType: typeof(CommandLineScriptGlobals)).GetCompilation();

            scriptCompilation.VerifyDiagnostics();

            string corAssemblyName = typeof(object).GetTypeInfo().Assembly.GetName().Name;
            string hostObjectAssemblyName = scriptCompilation.ScriptCompilationInfo.GlobalsType.GetTypeInfo().Assembly.GetName().Name;

            // The host adds 
            // 1) a reference to typeof(object).Assembly
            // 2) a reference to GlobalsType with alias <host> applied recursively.
            // References returned from ResolveMissingAssembly have <implicit> alias.

            foreach (var (assembly, aliases) in scriptCompilation.GetBoundReferenceManager().GetReferencedAssemblyAliases())
            {
                string name = assembly.Identity.Name;

                switch (name)
                {
                    case "Microsoft.CodeAnalysis.CSharp.Scripting":
                        // we have an explicit reference to CSharpScript assembly:
                        AssertEx.SetEqual(Array.Empty<string>(), aliases);
                        break;

                    case "Microsoft.CodeAnalysis.CSharp":
                        // The script has a recursive reference to Microsoft.CodeAnalysis.CSharp.Scripting, which references these assemblies.
                        // The script doesn't reference the assembly explicitly.
                        AssertEx.SetEqual(new[] { "<implicit>", "global" }, aliases);
                        break;

                    case "Microsoft.CodeAnalysis":
                    case "System.Collections.Immutable":
                        // The script has a recursive reference to Microsoft.CodeAnalysis.CSharp.Scripting, which references these assemblies.
                        // Microsoft.CodeAnalysis.Scripting contains host object and is thus referenced recursively with <host> alias. 
                        // The script doesn't reference the assemblies explicitly.
                        AssertEx.SetEqual(new[] { "<implicit>", "<host>", "global" }, aliases);
                        break;

                    default:
                        if (name == corAssemblyName)
                        {
                            // Host object depends on System.Object, thus <host> is applied on CorLib.
                            AssertEx.SetEqual(new[] { "<host>", "global" }, aliases);
                        }
                        else if (name == hostObjectAssemblyName)
                        {
                            // Host object is defined in an assembly that CSharpScript depends on.
                            // CSharpScript assembly was references (recursively) hence host object assembly is 
                            // available to the script (global alias).
                            AssertEx.SetEqual(new[] { "<host>", "global" }, aliases);
                        }

                        break;
                }
            }
        }

        public class E
        {
            public bool TryGetValue(out object obj)
            {
                obj = new object();
                return true;
            }
        }

        [Fact]
        [WorkItem(39565, "https://github.com/dotnet/roslyn/issues/39565")]
        public async Task Regression39565()
        {
            var code = @"
            if(TryGetValue(out var result)){
                _ = result;
            }
            return true;
            ";

            var result = await CSharpScript.EvaluateAsync<bool>(code, globalsType: typeof(E), globals: new E());
            Assert.True(result);
        }

        public class F
        {
            public bool Value = true;
        }

        [Fact]
        public void StaticMethodCannotAccessGlobalInstance()
        {
            var code = @"
			static bool M()
			{
				return Value;
			}
			return M();
            ";

            var script = CSharpScript.Create<bool>(code, globalsType: typeof(F));
            ScriptingTestHelpers.AssertCompilationError(() => script.RunAsync(new F()).Wait(),
                    // (4,12): error CS0120: An object reference is required for the non-static field, method, or property 'InteractiveSessionTests.F.Value'
                    // 				return Value;
                    Diagnostic(ErrorCode.ERR_ObjectRequired, "Value").WithArguments("Microsoft.CodeAnalysis.CSharp.Scripting.UnitTests.InteractiveSessionTests.F.Value").WithLocation(4, 12));
        }

        [Fact]
        [WorkItem(39581, "https://github.com/dotnet/roslyn/issues/39581")]
        public void StaticLocalFunctionCannotAccessGlobalInstance()
        {
            var code = @"
			bool M()
			{
				return Inner();
				static bool Inner()
				{
					return Value;
				}
			}
			return M();
            ";

            var script = CSharpScript.Create<bool>(code, globalsType: typeof(F));
            ScriptingTestHelpers.AssertCompilationError(() => script.RunAsync(new F()).Wait(),
                    // (7,13): error CS0120: An object reference is required for the non-static field, method, or property 'InteractiveSessionTests.F.Value'
                    // 					return Value;
                    Diagnostic(ErrorCode.ERR_ObjectRequired, "Value").WithArguments("Microsoft.CodeAnalysis.CSharp.Scripting.UnitTests.InteractiveSessionTests.F.Value").WithLocation(7, 13));
        }

        [Fact]
        public async Task LocalFunctionCanAccessGlobalInstance()
        {
            var code = @"
			bool M()
			{
				return Inner();
				bool Inner()
				{
					return Value;
				}
			}
			return M();
            ";

            var result = await CSharpScript.EvaluateAsync<bool>(code, globalsType: typeof(F), globals: new F());
            Assert.True(result);
        }

        #endregion

        #region Exceptions

        [Fact]
        [WorkItem(6580, "https://github.com/dotnet/roslyn/issues/6580")]
        [WorkItem(10883, "https://github.com/dotnet/roslyn/issues/10883")]
        public async Task PreservingDeclarationsOnException1()
        {
            var s0 = CSharpScript.Create(@"
int i = 10;
throw new System.Exception(""Bang!"");
int j = 2;
");

            var s1 = s0.ContinueWith(@"
int F() => i + j;
");

            var state1 = await s1.RunAsync(catchException: e => true);

            Assert.Equal("Bang!", state1.Exception.Message);

            var state2 = await state1.ContinueWithAsync<int>("F()");

            Assert.Equal(10, state2.ReturnValue);
        }

        [Fact]
        [WorkItem(6580, "https://github.com/dotnet/roslyn/issues/6580")]
        [WorkItem(10883, "https://github.com/dotnet/roslyn/issues/10883")]
        public async Task PreservingDeclarationsOnException2()
        {
            var s0 = CSharpScript.Create(@"
int i = 100;
");

            var s1 = s0.ContinueWith(@"
int j = 20;
throw new System.Exception(""Bang!"");
int k = 3;
");

            var s2 = s1.ContinueWith(@"
int F() => i + j + k;
");

            var state2 = await s2.RunAsync(catchException: e => true);

            Assert.Equal("Bang!", state2.Exception.Message);

            var state3 = await state2.ContinueWithAsync<int>("F()");

            Assert.Equal(120, state3.ReturnValue);
        }

        [Fact]
        [WorkItem(6580, "https://github.com/dotnet/roslyn/issues/6580")]
        [WorkItem(10883, "https://github.com/dotnet/roslyn/issues/10883")]
        public async Task PreservingDeclarationsOnException3()
        {
            var s0 = CSharpScript.Create(@"
int i = 1000;
");
            var s1 = s0.ContinueWith(@"
int j = 200;
throw new System.Exception(""Bang!"");
int k = 30;
");
            var s2 = s1.ContinueWith(@"
int l = 4;
");
            var s3 = s2.ContinueWith(@"
int F() => i + j + k + l;
");

            var state3 = await s3.RunAsync(catchException: e => true);

            Assert.Equal("Bang!", state3.Exception.Message);

            var state4 = await state3.ContinueWithAsync<int>("F()");

            Assert.Equal(1200, state4.ReturnValue);
        }

        [Fact]
        [WorkItem(6580, "https://github.com/dotnet/roslyn/issues/6580")]
        [WorkItem(10883, "https://github.com/dotnet/roslyn/issues/10883")]
        public async Task PreservingDeclarationsOnException4()
        {
            var state0 = await CSharpScript.RunAsync(@"
int i = 1000;
");
            var state1 = await state0.ContinueWithAsync(@"
int j = 200;
throw new System.Exception(""Bang 1!"");
int k = 30;
", catchException: e => true);

            Assert.Equal("Bang 1!", state1.Exception.Message);

            var state2 = await state1.ContinueWithAsync<int>(@"
int l = 4;
throw new System.Exception(""Bang 2!"");
1
", catchException: e => true);

            Assert.Equal("Bang 2!", state2.Exception.Message);

            var state4 = await state2.ContinueWithAsync(@"
i + j + k + l
");
            Assert.Equal(1204, state4.ReturnValue);
        }

        [Fact]
        public async Task PreservingDeclarationsOnCancellation1()
        {
            var cancellationSource = new CancellationTokenSource();

            var globals = new StrongBox<CancellationTokenSource>();
            globals.Value = cancellationSource;

            var s0 = CSharpScript.Create(@"
int i = 1000;
", globalsType: globals.GetType());

            var s1 = s0.ContinueWith(@"
int j = 200;
Value.Cancel();
int k = 30;
");
            // cancellation exception is thrown just before we start evaluating s2:
            var s2 = s1.ContinueWith(@"
int l = 4;
");
            var s3 = s2.ContinueWith(@"
int F() => i + j + k + l;
");

            var state3 = await s3.RunAsync(globals, catchException: e => true, cancellationToken: cancellationSource.Token);

            Assert.IsType<OperationCanceledException>(state3.Exception);

            var state4 = await state3.ContinueWithAsync<int>("F()");

            Assert.Equal(1230, state4.ReturnValue);
        }

        [Fact]
        public async Task PreservingDeclarationsOnCancellation2()
        {
            var cancellationSource = new CancellationTokenSource();

            var globals = new StrongBox<CancellationTokenSource>();
            globals.Value = cancellationSource;

            var s0 = CSharpScript.Create(@"
int i = 1000;
", globalsType: globals.GetType());

            var s1 = s0.ContinueWith(@"
int j = 200;
int k = 30;
");
            var s2 = s1.ContinueWith(@"
int l = 4;
Value.Cancel();
");
            // cancellation exception is thrown just before we start evaluating s3:
            var s3 = s2.ContinueWith(@"
int F() => i + j + k + l;
");

            var state3 = await s3.RunAsync(globals, catchException: e => true, cancellationToken: cancellationSource.Token);

            Assert.IsType<OperationCanceledException>(state3.Exception);

            var state4 = await state3.ContinueWithAsync<int>("F()");

            Assert.Equal(1234, state4.ReturnValue);
        }

        [Fact]
        public async Task PreservingDeclarationsOnCancellation3()
        {
            var cancellationSource = new CancellationTokenSource();

            var globals = new StrongBox<CancellationTokenSource>();
            globals.Value = cancellationSource;

            var s0 = CSharpScript.Create(@"
int i = 1000;
", globalsType: globals.GetType());

            var s1 = s0.ContinueWith(@"
int j = 200;
Value.Cancel();
int k = 30;
");
            // cancellation exception is thrown just before we start evaluating s2:
            var s2 = s1.ContinueWith(@"
int l = 4;
");
            var s3 = s2.ContinueWith(@"
int F() => i + j + k + l;
");

            await Assert.ThrowsAsync<OperationCanceledException>(() =>
                s3.RunAsync(globals, catchException: e => !(e is OperationCanceledException), cancellationToken: cancellationSource.Token));
        }

        #endregion

        #region Local Functions

        [Fact]
        public void LocalFunction_PreviousSubmissionAndGlobal()
        {
            var result =
                CSharpScript.RunAsync(
@"int InInitialSubmission()
{
    return LocalFunction();
    int LocalFunction() => Y;
}", globals: new C()).
                ContinueWith(
@"var lambda = new System.Func<int>(() =>
{
    return LocalFunction();
    int LocalFunction() => Y + InInitialSubmission();
});

lambda.Invoke()").
                Result.ReturnValue;

            Assert.Equal(4, result);
        }

        #endregion
    }
}
