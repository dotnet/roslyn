// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Scripting.Test;
using Roslyn.Test.Utilities;
using Roslyn.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Scripting.CSharp.UnitTests
{
    public class HostModel
    {
        public readonly int Foo;
    }

    public class InteractiveSessionTests : TestBase
    {
        private static readonly Assembly s_lazySystemRuntimeAssembly;
        private static readonly Assembly SystemRuntimeAssembly = s_lazySystemRuntimeAssembly ?? (s_lazySystemRuntimeAssembly = Assembly.Load(new AssemblyName("System.Runtime, Version=4.0.20.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a")));

        #region Namespaces, Types

        [Fact]
        public void Namespaces()
        {
            var engine = new CSharpScriptEngine();

            ScriptingTestHelpers.AssertCompilationError(engine, @"
namespace N1
{
   class A { public int Foo() { return 2; }}
}
",
                Diagnostic(ErrorCode.ERR_NamespaceNotAllowedInScript, "namespace"));
        }

        [Fact]
        public void CompilationChain_NestedTypesClass()
        {
            var engine = new CSharpScriptEngine();
            var session = engine.CreateSession();

            session.Execute(@"
static string outerStr = null;
public static void Foo(string str) { outerStr = str; }
class InnerClass
{
   public string innerStr = null;
   public void Goo() { Foo(""test""); innerStr = outerStr; }       
}
");

            session.Execute(@"
InnerClass iC = new InnerClass();
iC.Goo();
");

            using (var redirect = new OutputRedirect(CultureInfo.InvariantCulture))
            {
                session.Execute(@"System.Console.WriteLine(iC.innerStr);");
                Assert.Equal("test", redirect.Output.Trim());
            }
        }

        [Fact]
        public void CompilationChain_NestedTypesStruct()
        {
            var engine = new CSharpScriptEngine();
            var session = engine.CreateSession();

            session.Execute(@"
static string outerStr = null;
public static void Foo(string str) { outerStr = str; }
struct InnerStruct
{
   public string innerStr;
   public void Goo() { Foo(""test""); innerStr = outerStr; }            
}
");

            session.Execute(@"
InnerStruct iS = new InnerStruct();     
iS.Goo();
");

            using (var redirect = new OutputRedirect(CultureInfo.InvariantCulture))
            {
                session.Execute(@"System.Console.WriteLine(iS.innerStr);");
                Assert.Equal("test", redirect.Output.Trim());
            }
        }

        [Fact]
        public void CompilationChain_InterfaceTypes()
        {
            var engine = new CSharpScriptEngine();
            var session = engine.CreateSession();

            session.Execute(@"
interface I1 { int Goo();}
class InnerClass : I1
{
  public int Goo() { return 1; }
}");

            session.Execute(@"
I1 iC = new InnerClass();
");

            Assert.Equal(1, session.Execute(@"iC.Goo()"));
        }

        [Fact]
        public void ScriptMemberAccessFromNestedClass()
        {
            var engine = new CSharpScriptEngine();
            var session = engine.CreateSession();

            session.Execute(@"
object field;
object Property { get; set; }
void Method() { }
");

            ScriptingTestHelpers.AssertCompilationError(session, @"
class C 
{
    public void Foo() 
    {
        object f = field;
        object p = Property;
        Method();
    }
}
",
                // (6,20): error CS0120: An object reference is required for the non-static field, method, or property 'field'
                Diagnostic(ErrorCode.ERR_ObjectRequired, "field").WithArguments("field"),
                // (7,20): error CS0120: An object reference is required for the non-static field, method, or property 'Property'
                Diagnostic(ErrorCode.ERR_ObjectRequired, "Property").WithArguments("Property"),
                // (8,9): error CS0120: An object reference is required for the non-static field, method, or property 'Method()'
                Diagnostic(ErrorCode.ERR_ObjectRequired, "Method").WithArguments("Method()")
 );
        }

        #region Anonymous Types 


        [Fact]
        public void AnonymousTypes_TopLevel_MultipleSubmissions()
        {
            var engine = new CSharpScriptEngine();
            var session = engine.CreateSession();

            session.Execute(@"
var a = new { f = 1 };
");

            session.Execute(@"
var b = new { g = 1 };
");

            var result = session.Execute<Array>(@"
var c = new { f = 1 };
var d = new { g = 1 };
new object[] { new[] { a, c }, new[] { b, d } }
");

            Assert.Equal(2, result.Length);
            Assert.Equal(2, ((Array)result.GetValue(0)).Length);
            Assert.Equal(2, ((Array)result.GetValue(1)).Length);
        }

        [Fact]
        public void AnonymousTypes_TopLevel_MultipleSubmissions2()
        {
            var engine = new CSharpScriptEngine();
            var session = engine.CreateSession();

            session.Execute(@"
var a = new { f = 1 };
");

            session.Execute(@"
var b = new { g = 1 };
");

            var result = session.Execute<object>(@"
var c = new { f = 1 };
var d = new { g = 1 };
object.ReferenceEquals(a.GetType(), c.GetType()).ToString() + "" "" +
    object.ReferenceEquals(a.GetType(), b.GetType()).ToString() + "" "" + 
    object.ReferenceEquals(b.GetType(), d.GetType()).ToString()
");

            Assert.Equal("True False True", result.ToString());
        }

        [WorkItem(543863)]
        [Fact]
        public void AnonymousTypes_Redefinition()
        {
            var engine = new CSharpScriptEngine();
            var session = engine.CreateSession();

            session.Execute(@"var x = new { Foo = ""foo"" };");
            session.Execute(@"var x = new { Foo = ""foo"" };");

            var result = session.Execute("x.Foo");
            Assert.Equal("foo", result);
        }

        [Fact]
        public void AnonymousTypes_TopLevel_Empty()
        {
            var engine = new CSharpScriptEngine();
            var session = engine.CreateSession();

            session.Execute(@"
var a = new { };
");

            session.Execute(@"
var b = new { };
");

            var result = session.Execute<Array>(@"
var c = new { };
var d = new { };
new object[] { new[] { a, c }, new[] { b, d } }
");

            Assert.Equal(2, result.Length);
            Assert.Equal(2, ((Array)result.GetValue(0)).Length);
            Assert.Equal(2, ((Array)result.GetValue(1)).Length);
        }

        #endregion

        #region Dynamic

        [Fact]
        public void Dynamic_Expando()
        {
            var engine = new CSharpScriptEngine();
            var session = engine.CreateSession();
            session.AddReference(typeof(Microsoft.CSharp.RuntimeBinder.RuntimeBinderException).GetTypeInfo().Assembly);
            session.AddReference(typeof(System.Dynamic.ExpandoObject).GetTypeInfo().Assembly);
            session.ImportNamespace("System.Dynamic");

            session.Execute(@"
dynamic expando = new ExpandoObject();
");

            session.Execute(@"
expando.foo = 1;
");

            var result = session.Execute<int>(@"
expando.foo
");

            Assert.Equal(1, result);
        }

        #endregion

        [Fact]
        public void Enums()
        {
            var engine = new CSharpScriptEngine();
            var session = engine.CreateSession();
            object e = session.Execute(@"
public enum Enum1
{
    A, B, C
}
Enum1 E = Enum1.C;

E
");
            Assert.True(e.GetType().GetTypeInfo().IsEnum, "Expected enum");
            Assert.Equal(typeof(int), Enum.GetUnderlyingType(e.GetType()));
        }

        #endregion

        #region Attributes

        [WorkItem(949595)]
        [Fact(Skip = "949595")]
        public void GlobalAttributes()
        {
            var engine = new CSharpScriptEngine();

            ScriptingTestHelpers.AssertCompilationError(engine, @"
[assembly: System.Reflection.AssemblyVersion(""4.0.0.0"")]
[module: System.Security.UnverifiableCode]
",
                // (2,2): error CS7026: Assembly and module attributes are not allowed in this context
                Diagnostic(ErrorCode.ERR_GlobalAttributesNotAllowed, "assembly"),
                // (3,2): error CS7026: Assembly and module attributes are not allowed in this context
                Diagnostic(ErrorCode.ERR_GlobalAttributesNotAllowed, "module"));
        }

        [Fact]
        public void PInvoke()
        {
            var engine = new CSharpScriptEngine();
            var session = engine.CreateSession();

            var source = @"
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

[DllImport(""foo"", 
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
            Type c = engine.CreateSession().Execute<Type>(source);
            var m = c.DeclaringType.GetTypeInfo().GetDeclaredMethod("M");
            Assert.Equal(MethodImplAttributes.PreserveSig, m.MethodImplementationFlags);

            // uncollectible Ref.Emit:
            c = session.Execute<Type>(source);
            m = c.DeclaringType.GetTypeInfo().GetDeclaredMethod("M");
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
            Assert.Equal("foo", dllImport.Value);
        }

        #endregion

        // extension methods - must be private, can be top level

        #region Modifiers and Visibility

        [Fact]
        public void SealedOverride()
        {
            var engine = new CSharpScriptEngine();
            var session = engine.CreateSession();
            session.Execute(@"
class M
{
    protected virtual void F() { }
}
class Y : M
{
    sealed protected override void F() {  }
}
");
        }

        [Fact]
        public void PrivateTopLevel()
        {
            var engine = new CSharpScriptEngine();
            var session = engine.CreateSession();

            int result;

            result = session.Execute<int>(@"
private int foo() { return 1; }
private static int bar() { return 10; }
private static int f = 100;

foo() + bar() + f
");
            Assert.Equal(111, result);

            result = session.Execute<int>(@"
foo() + bar() + f
");
            Assert.Equal(111, result);

            result = session.Execute<int>(@"
class C { public static int baz() { return bar() + f; } }

C.baz()
");
            Assert.Equal(110, result);
        }

        [Fact]
        public void PrivateNested()
        {
            var engine = new CSharpScriptEngine();
            var session = engine.CreateSession();

            session.Execute(@"
public class C { private static int foo() { return 1; } }
");

            ScriptingTestHelpers.AssertCompilationError(session, @"C.foo()",
                // error CS0122: '{0}' is inaccessible due to its protection level
                Diagnostic(ErrorCode.ERR_BadAccess, "foo").WithArguments("C.foo()"));
        }

        [Fact]
        public void NestedVisibility()
        {
            var engine = new CSharpScriptEngine();
            var session = engine.CreateSession();
            int result;

            session.Execute(@"
private class C 
{ 
    internal class D
    {
        internal static int foo() { return 1; } 
    }

    private class E
    {
        internal static int foo() { return 1; } 
    }

    public class F
    {
        internal protected static int foo() { return 1; } 
    }

    internal protected class G
    {
        internal static int foo() { return 1; } 
    }
}
");

            result = session.Execute<int>(@"C.D.foo()");
            Assert.Equal(1, result);

            result = session.Execute<int>(@"C.F.foo()");
            Assert.Equal(1, result);

            result = session.Execute<int>(@"C.G.foo()");
            Assert.Equal(1, result);

            ScriptingTestHelpers.AssertCompilationError(session, @"C.E.foo()",
                // error CS0122: 'C.E' is inaccessible due to its protection level
                Diagnostic(ErrorCode.ERR_BadAccess, "E").WithArguments("C.E"));
        }

        [Fact]
        public void InconsistentAccessibilityChecks()
        {
            var engine = new CSharpScriptEngine();
            var session = engine.CreateSession();

            session.Execute(@"
private class A { }
protected class B { }
internal class C { }
internal protected class D { }
public class E { }
");

            session.Execute(@"
private A a0;

private B b0;
protected B b1;

private C c0;
protected C c1;             // allowed since the submission is an internal class
internal C c2;
internal protected C c3;    // allowed since the submission is an internal class
public C c4;                // allowed since the submission is an internal class

private D d0;
protected D d1;             
internal D d2;
internal protected D d3;    
public D d4;                // allowed since the submission is an internal class

private E e0;
protected E e1;
internal E e2;
internal protected E e3;
public E e4;
");

            ScriptingTestHelpers.AssertCompilationError(session, @"protected A x;",
                // (1,10): error CS0052: Inconsistent accessibility: field type 'A' is less accessible than field 'x'
                Diagnostic(ErrorCode.ERR_BadVisFieldType, "x").WithArguments("x", "A"),
                // (1,13): warning CS0628: 'x': new protected member declared in sealed class
                Diagnostic(ErrorCode.WRN_ProtectedInSealed, "x").WithArguments("x"));

            ScriptingTestHelpers.AssertCompilationError(session, @"internal A x;",
                // (1,10): error CS0052: Inconsistent accessibility: field type 'A' is less accessible than field 'x'
                Diagnostic(ErrorCode.ERR_BadVisFieldType, "x").WithArguments("x", "A"));

            ScriptingTestHelpers.AssertCompilationError(session, @"internal protected A x;",
                // (1,10): error CS0052: Inconsistent accessibility: field type 'A' is less accessible than field 'x'
                Diagnostic(ErrorCode.ERR_BadVisFieldType, "x").WithArguments("x", "A"),
                // (1,13): warning CS0628: 'x': new protected member declared in sealed class
                Diagnostic(ErrorCode.WRN_ProtectedInSealed, "x").WithArguments("x"));

            ScriptingTestHelpers.AssertCompilationError(session, @"public A x;",
                // (1,10): error CS0052: Inconsistent accessibility: field type 'A' is less accessible than field 'x'
                Diagnostic(ErrorCode.ERR_BadVisFieldType, "x").WithArguments("x", "A"));


            ScriptingTestHelpers.AssertCompilationError(session, @"internal B x;",
                // (1,10): error CS0052: Inconsistent accessibility: field type 'B' is less accessible than field 'x'
                Diagnostic(ErrorCode.ERR_BadVisFieldType, "x").WithArguments("x", "B"));

            ScriptingTestHelpers.AssertCompilationError(session, @"internal protected B x;",
                // (1,10): error CS0052: Inconsistent accessibility: field type 'B' is less accessible than field 'x'
                Diagnostic(ErrorCode.ERR_BadVisFieldType, "x").WithArguments("x", "B"),
                // (1,13): warning CS0628: 'x': new protected member declared in sealed class
                Diagnostic(ErrorCode.WRN_ProtectedInSealed, "x").WithArguments("x"));

            ScriptingTestHelpers.AssertCompilationError(session, @"public B x;",
                // (1,10): error CS0052: Inconsistent accessibility: field type 'B' is less accessible than field 'x'
                Diagnostic(ErrorCode.ERR_BadVisFieldType, "x").WithArguments("x", "B"));
        }

        [Fact]
        public void Fields_Visibility()
        {
            var engine = new CSharpScriptEngine();
            var session = engine.CreateSession();

            session.Execute(@"
private int i = 2;     // test comment;
public int j = 2;
protected int k = 2;
internal protected int l = 2;
internal int pi = 2;
");

            session.Execute(@"
i = i + i;
j = j + j;
k = k + k;
l = l + l;
");

            session.Execute(@"
pi = i + j + k + l;
");

            int result = session.Execute<int>(@"i");
            Assert.Equal(4, result);

            result = session.Execute<int>(@"j");
            Assert.Equal(4, result);

            result = session.Execute<int>(@"k");
            Assert.Equal(4, result);

            result = session.Execute<int>(@"l");
            Assert.Equal(4, result);

            result = session.Execute<int>(@"pi");
            Assert.Equal(16, result);
        }

        #endregion

        #region Chaining

        [Fact]
        public void CompilationChain_BasicFields()
        {
            var engine = new CSharpScriptEngine();
            var session = engine.CreateSession();
            session.Execute("var x = 1;");
            var result = session.Execute("x");
            Assert.Equal(1, result);
        }

        [Fact]
        public void CompilationChain_GlobalNamespaceAndUsings()
        {
            var engine = new CSharpScriptEngine();
            var session = engine.CreateSession();
            session.AddReference(this.GetType().GetTypeInfo().Assembly);
            session.Execute("using InteractiveFixtures.C;");

            object result = session.Execute(@"System.Environment.ProcessorCount");
            Assert.Equal(Environment.ProcessorCount, result);
        }

        [Fact]
        public void CompilationChain_CurrentSubmissionUsings()
        {
            var engine = new CSharpScriptEngine();
            Session session;
            object result;

            session = engine.CreateSession();
            session.AddReference(this.GetType().GetTypeInfo().Assembly);
            session.Execute("class X { public int foo() { return 1; } }");
            session.Execute("using InteractiveFixtures.A;");
            result = session.Execute("new X().foo()");
            Assert.Equal(1, result);

            session = engine.CreateSession();
            session.AddReference(this.GetType().GetTypeInfo().Assembly);
            session.Execute("class X { public int foo() { return 1; } }");
            result = session.Execute(@"
using InteractiveFixtures.A;
new X().foo()
");

            Assert.Equal(1, result);
        }

        [Fact]
        public void CompilationChain_UsingDuplicates()
        {
            var engine = new CSharpScriptEngine();
            var session = engine.CreateSession();

            session.Execute(@"
using System;
using System;");

            session.Execute(@"
using System;
using System;");

            var result = session.Execute(@"Environment.ProcessorCount");
            Assert.Equal(Environment.ProcessorCount, result);
        }

        [Fact]
        public void CompilationChain_GlobalImports()
        {
            var engine = new CSharpScriptEngine();
            engine.ImportNamespace("System");

            // imported namespaces on engine are captured by the session
            var session1 = engine.CreateSession();

            var result = session1.Execute("Environment.ProcessorCount");
            Assert.Equal(Environment.ProcessorCount, result);

            result = session1.Execute("Environment.ProcessorCount");
            Assert.Equal(Environment.ProcessorCount, result);

            var session2 = engine.CreateSession();
            result = session2.Execute("Environment.ProcessorCount");
            Assert.Equal(Environment.ProcessorCount, result);
        }

        [Fact]
        public void CompilationChain_SubmissionSlotResize()
        {
            var engine = new CSharpScriptEngine();
            var session = engine.CreateSession();

            for (int i = 0; i < 17; i++)
            {
                session.Execute(@"public int i =  1;");
            }
            using (var redirect = new OutputRedirect(CultureInfo.InvariantCulture))
            {
                session.Execute(@"System.Console.WriteLine(i);");
                Assert.Equal(1, int.Parse(redirect.Output));
            }
        }

        [Fact]
        public void CompilationChain_Uncollectible1()
        {
            var engine = new CSharpScriptEngine();
            var session = engine.CreateSession();

            // Ref.Emit
            session.Execute(@"
public class B { }
int x; x = 1;
");

            // Ref.Emit
            session.Execute(@"
public class C { }
int y; y = 1;
");

            // CCI
            session.Execute(@"
public class E
{
  public struct N2
  { 
    public N3 n1;
  }
  public struct N3
  { 
  }
  N2 n2; 
  C c; 
  B b;
}

int z; z = x + y;
");

            using (var redirect = new OutputRedirect(CultureInfo.InvariantCulture))
            {
                // Ref.Emit
                session.Execute(@"
System.Console.Write(new E());
System.Console.Write(x + y);
");
                Assert.True(redirect.Output.Trim().EndsWith("+E2", StringComparison.Ordinal), redirect.Output);
            }
        }

        [Fact]
        public void CrossSubmissionGenericInterfaceImplementation_Generic()
        {
            var engine = new CSharpScriptEngine();
            var session = engine.CreateSession();

            session.Execute(@"
public interface I<T>
{
    void m<TT>(T x, TT y);
}
");

            session.Execute(@"
abstract public class C : I<int>
{
    public void m<TT>(int x, TT y)
    {
    }
}");
        }

        [Fact]
        public void CrossSubmissionGenericInterfaceImplementation_Explicit_GenericMethod()
        {
            var engine = new CSharpScriptEngine();
            var session = engine.CreateSession();

            session.Execute(@"
public interface I<T>
{
    void m<S>(T x, S y);
}
");

            session.Execute(@"
abstract public class C : I<int>
{
    void I<int>.m<S>(int x, S y)
    {
    }
}");
        }

        [Fact]
        public void CrossSubmissionGenericInterfaceImplementation_Explicit()
        {
            var engine = new CSharpScriptEngine();
            var session = engine.CreateSession();
            session.Execute(@"
public interface I<T>
{
    void m(T x);
}
");

            session.Execute(@"
abstract public class C : I<int>
{
    void I<int>.m(int x)
    {
    }
}");
        }

        [Fact]
        public void CrossSubmissionGenericInterfaceImplementation_Explicit_NoGenericParametersInSignature()
        {
            var engine = new CSharpScriptEngine();
            var session = engine.CreateSession();
            session.Execute(@"
public interface I<T>
{
    void m(byte x);
}
");

            session.Execute(@"
abstract public class C : I<int>
{
    void I<int>.m(byte x)
    {
    }
}");
        }

        [Fact]
        public void GenericInterfaceImplementation_Explicit_NoGenericParametersInSignature()
        {
            var engine = new CSharpScriptEngine();
            engine.CreateSession().Execute(@"
public interface I<T>
{
    void m(byte x);
}
abstract public class C : I<int>
{
    void I<int>.m(byte x)
    {
    }
}");
        }

        [Fact]
        public void CrossSubmissionInterfaceImplementation_Explicit_NoGenericParametersInSignature()
        {
            var engine = new CSharpScriptEngine();
            var session = engine.CreateSession();
            session.Execute(@"
public interface I
{
    void m(byte x);
}
");

            session.Execute(@"
abstract public class C : I
{
    void I.m(byte x)
    {
    }
}");
        }

        [Fact]
        public void CrossSubmissionNestedGenericInterfaceImplementation_Explicit()
        {
            var engine = new CSharpScriptEngine();
            var session = engine.CreateSession();
            session.Execute(@"
class C<T>
{
    public interface I
    {
        void m(T x);
    }
}
");

            session.Execute(@"
abstract public class D : C<int>.I
{
    void C<int>.I.m(int x)
    {
    }
}");
        }

        [Fact]
        public void NestedGenericInterfaceImplementation_Explicit()
        {
            var engine = new CSharpScriptEngine();
            engine.CreateSession().Execute(@"
class C<T>
{
    public interface I
    {
        void m(T x);
    }
}
abstract public class D : C<int>.I
{
    void C<int>.I.m(int x)
    {
    }
}");
        }

        [Fact]
        public void ExternalInterfaceImplementation_Explicit()
        {
            var engine = new CSharpScriptEngine();

            engine.CreateSession().Execute(@"
using System.Collections;
using System.Collections.Generic;

abstract public class C : IEnumerable<int>
{
    IEnumerator<int> IEnumerable<int>.GetEnumerator()
    {
        return null;
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return null;
    }
}");
        }

        [Fact]
        public void CompilationChain_Fields()
        {
            var engine = new CSharpScriptEngine();
            var session = engine.CreateSession();

            session.Execute(@"
static int s = 1;
int i = 2;
");

            int result = session.Execute<int>("s + i");

            ScriptingTestHelpers.AssertCompilationError(session, "static int t = i;",
                Diagnostic(ErrorCode.ERR_ObjectRequired, "i").WithArguments("i")
            );
        }

        [Fact]
        public void CompilationChain_UsingNotHidingPreviousSubmission()
        {
            var engine = new CSharpScriptEngine();
            Session session;
            object result;

            session = engine.CreateSession();
            session.Execute("using System;");
            session.Execute("int Environment = 1;");
            result = session.Execute("Environment");
            Assert.Equal(1, result);

            session = engine.CreateSession();
            session.Execute("int Environment = 1;");
            session.Execute("using System;");
            result = session.Execute("Environment");
            Assert.Equal(1, result);
        }

        [Fact]
        public void CompilationChain_DefinitionHidesGlobal()
        {
            var engine = new CSharpScriptEngine();
            var session = engine.CreateSession();
            session.Execute("int System = 1;");
            var result = session.Execute("System");
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
            var engine = new CSharpScriptEngine();
            var session = engine.CreateSession(new C1());
            session.AddReference(typeof(C1).GetTypeInfo().Assembly);
            object result = session.Execute("System");
            Assert.Equal(1, result);
        }

        [Fact]
        public void CompilationChain_UsingNotHidingHostObjectMembers()
        {
            var engine = new CSharpScriptEngine();
            var session = engine.CreateSession(new C1());
            session.AddReference(typeof(C1).GetTypeInfo().Assembly);
            session.Execute("using System;");
            var result = session.Execute("Environment");
            Assert.Equal(2, result);
        }

        [Fact]
        public void CompilationChain_DefinitionHidesHostObjectMembers()
        {
            var engine = new CSharpScriptEngine();
            var session = engine.CreateSession(new C1());
            session.AddReference(typeof(C1).GetTypeInfo().Assembly);
            session.Execute("int System = 2;");
            var result = session.Execute("System");
            Assert.Equal(2, result);
        }

        [Fact]
        public void CompilationChain_InStaticContext()
        {
            var engine = new CSharpScriptEngine();
            var session = engine.CreateSession();

            session.Execute(@"
int x = 1;
int y = 2;
int z() { return 3; }
int w = 4;
");

            ScriptingTestHelpers.AssertCompilationError(session, @"
static int Foo() { return x; }
static int Bar { get { return y; } set { return z(); } }
static int Baz = w;
",
                // error CS0120: An object reference is required for the non-static field, method, or property '{0}'
                Diagnostic(ErrorCode.ERR_ObjectRequired, "w").WithArguments("w"),
                Diagnostic(ErrorCode.ERR_ObjectRequired, "x").WithArguments("x"),
                Diagnostic(ErrorCode.ERR_ObjectRequired, "y").WithArguments("y"),
                Diagnostic(ErrorCode.ERR_ObjectRequired, "z").WithArguments("z()")
            );
        }

#if TODO
        [Fact]
        public void AccessToGlobalMemberFromNestedClass1()
        {
            var engine = new Engine();
            var session = engine.CreateSession();

            ScriptingTestHelpers.AssertCompilationError(session, @"
int foo() { return 1; }

class D 
{
    int bar() { return foo(); }
}
",
                // error CS0038: Cannot access a non-static global member from within type '{0}'
                Diagnostic(ErrorCode.ERR_AccessToGlobalMemberFromNestedType, "i").WithArguments("D"));
        }

        [Fact]
        public void AccessToGlobalMemberFromNestedClass2()
        {
            var engine = new Engine();
            var session = engine.CreateSession();

            engine.Execute(@"
int foo() { return 1; }
", session);

            ScriptingTestHelpers.AssertCompilationError(session, @"
class D 
{
    int bar() { return foo(); }
}
",
                // error CS0038: Cannot access a non-static global member from within type '{0}'
                Diagnostic(ErrorCode.ERR_AccessToGlobalMemberFromNestedType, "foo").WithArguments("D"));
        }
#endif

        [Fact]
        public void Submissions_ExecutionOrder1()
        {
            var engine = new CSharpScriptEngine();
            var session = engine.CreateSession();

            var s0 = session.CompileSubmission<object>("int x = 1;");
            var s1 = session.CompileSubmission<object>("int y = 2;");
            var s2 = session.CompileSubmission<int>("x + y");
            Assert.Equal(s2.Compilation, session.LastSubmission);

            s2.Execute();
            s1.Execute();
            s0.Execute();

            ////Assert.Throws<InvalidOperationException>(() => s2.Execute());
            ////Assert.Throws<InvalidOperationException>(() => s1.Execute());

            s0.Execute();
            s1.Execute();
            int result;

            result = s2.Execute();
            Assert.Equal(3, result);

            result = s2.Execute();
            Assert.Equal(3, result);
        }

        [Fact]
        public void Submissions_ExecutionOrder2()
        {
            var engine = new CSharpScriptEngine();
            var session = engine.CreateSession();

            session.Execute("int x = 1;");
            Assert.Throws<CompilationErrorException>(() => session.Execute("$invalid_syntax"));

            var s1 = session.CompileSubmission<object>("x = 2;");

            ////Assert.Throws<InvalidOperationException>(() => session.Execute("x = 10"));
            session.Execute("x = 10");
            Assert.Throws<CompilationErrorException>(() => session.Execute("$invalid_syntax"));
            Assert.Throws<CompilationErrorException>(() => session.Execute("x = undefined_symbol;"));

            var s2 = session.CompileSubmission<object>("int y = 2;");

            Assert.Equal(s2.Compilation, session.LastSubmission);

            s1.Execute();

            s2.Execute();

            var result = session.Execute("x + y");

            Assert.Equal(12, result);
        }

        [Fact]
        public void Submissions_Errors()
        {
            var session = new CSharpScriptEngine().CreateSession();

            // missing semicolon (script):
            Assert.Throws<CompilationErrorException>(() => session.CompileSubmission<object>("1", isInteractive: false));
        }

        public class HostObjectWithOverrides
        {
            public override bool Equals(object obj)
            {
                return true;
            }

            public override int GetHashCode()
            {
                return 1234567;
            }

            public override string ToString()
            {
                return "HostObjectToString impl";
            }
        }

        [Fact]
        public void ObjectOverrides()
        {
            var engine = new CSharpScriptEngine();
            var session = engine.CreateSession(new HostObjectWithOverrides());

            // TODO: should not be neccessary
            session.AddReference(SystemRuntimeAssembly);

            session.AddReference(typeof(HostObjectWithOverrides).GetTypeInfo().Assembly);

            Assert.Equal(true, session.Execute<bool>(@"Equals(null)"));
            Assert.Equal(1234567, session.Execute<int>(@"GetHashCode()"));
            Assert.Equal("HostObjectToString impl", session.Execute<string>(@"ToString()"));

            Assert.Equal(true, engine.CreateSession(new object()).Execute<bool>(@"
object x = 1;
object y = x;
ReferenceEquals(x, y)"));

            ScriptingTestHelpers.AssertCompilationError(engine, @"
Equals(null);
GetHashCode();
ToString();
ReferenceEquals(null, null);",
                // (2,1): error CS0103: The name 'Equals' does not exist in the current context
                Diagnostic(ErrorCode.ERR_NameNotInContext, "Equals").WithArguments("Equals"),
                // (3,1): error CS0103: The name 'GetHashCode' does not exist in the current context
                Diagnostic(ErrorCode.ERR_NameNotInContext, "GetHashCode").WithArguments("GetHashCode"),
                // (4,1): error CS0103: The name 'ToString' does not exist in the current context
                Diagnostic(ErrorCode.ERR_NameNotInContext, "ToString").WithArguments("ToString"),
                // (5,1): error CS0103: The name 'ReferenceEquals' does not exist in the current context
                Diagnostic(ErrorCode.ERR_NameNotInContext, "ReferenceEquals").WithArguments("ReferenceEquals"));

            ScriptingTestHelpers.AssertCompilationError(engine, @"public override string ToString() { return null; }",
                // (1,24): error CS0115: 'ToString()': no suitable method found to override
                Diagnostic(ErrorCode.ERR_OverrideNotExpected, "ToString").WithArguments("ToString()"));
        }

        #endregion

        #region Generics

        /// <summary>
        /// This test exposes CLR bug #201759 for which we have a workaround in ReflectionEmitter implementation.
        /// </summary>
        [Fact]
        public void CompilationChain_GenericTypes()
        {
            var engine = new CSharpScriptEngine();
            var session = engine.CreateSession();

            session.Execute(@"
class InnerClass<T> 
{
    public int method(int value) { return value + 1; }            
    public int field = 2;
}");

            session.Execute(@"
InnerClass<int> iC = new InnerClass<int>();
");

            Assert.Equal(3, session.Execute(@"iC.method(iC.field)"));
        }

        [WorkItem(529243)]
        [Fact]
        public void RecursiveBaseType()
        {
            var engine = new CSharpScriptEngine();

            // TODO: compiler should report an error (see Metadata spec 9.2 Generics and recursive inheritance graphs)

            engine.CreateSession().Execute(@"
class A<T> { }
class B<T> : A<B<B<T>>> { }
");
        }

        [Fact]
        public void CompilationChain_ExternalGenericTypes()
        {
            var engine = new CSharpScriptEngine();

            // need MethodSpec token
            Assert.Equal(0, engine.CreateSession().Execute(@"System.Activator.CreateInstance<int>()"));
        }

        [WorkItem(5378, "DevDiv_Projects/Roslyn")]
        [Fact]
        public void CompilationChain_GenericMethods()
        {
            var engine = new CSharpScriptEngine();
            var session = engine.CreateSession();

            session.Execute(@"
public int foo<T, R>(T arg) { return 1; }

public static T bar<T>(T i)
{
   return i;
}
");

            Assert.Equal(1, session.Execute(@"foo<int, int>(1)"));
            Assert.Equal(5, session.Execute(@"bar(5)"));
        }

        /// <summary>
        /// Tests that we emit ldftn and ldvirtftn instructions correctly.
        /// </summary>
        [Fact]
        public void CompilationChain_Ldftn()
        {
            var engine = new CSharpScriptEngine();
            var session = engine.CreateSession();

            session.Execute(@"
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

            Assert.Equal(111, session.Execute(@"
new System.Func<int>(C.f)() +
new System.Func<int>(new C().g)() +
new System.Func<int>(new C().h)()
"));

            Assert.Equal(222, session.Execute(@"
new System.Func<int>(C.gf<int>)() +
new System.Func<int>(new C().gg<object>)() +
new System.Func<int>(new C().gh<bool>)()
"));
        }

        /// <summary>
        /// Tests that we emit ldftn and ldvirtftn instructions correctly.
        /// </summary>
        [Fact]
        public void CompilationChain_Ldftn_GenericType()
        {
            var engine = new CSharpScriptEngine();
            var session = engine.CreateSession();

            session.Execute(@"
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

            Assert.Equal(111, session.Execute(@"
new System.Func<int>(C<byte>.f)() +
new System.Func<int>(new C<byte>().g)() +
new System.Func<int>(new C<byte>().h)()
"));

            Assert.Equal(222, session.Execute(@"
new System.Func<int>(C<byte>.gf<int>)() +
new System.Func<int>(new C<byte>().gg<object>)() +
new System.Func<int>(new C<byte>().gh<bool>)()
"));
        }

        #endregion

        #region Members

        [Fact]
        public void AbstractAccessors()
        {
            var engine = new CSharpScriptEngine();
            engine.CreateSession().Execute(@"
public abstract class C
{
    public abstract event System.Action vEv;
    public abstract int prop { get; set; }
}
");
        }

        [WorkItem(541436)]
        [Fact]
        public void InstanceFieldFromStaticMethod()
        {
            Assert.Throws<CompilationErrorException>(() =>
            {
                var engine = new CSharpScriptEngine();
                object result = engine.CreateSession().Execute(@"
delegate bool D();
D del;
public static void Foo(int input)
{
    int j = 0;
    del = () => { j = 10; return j > input; };
}
Foo(2);
del()
");
            });
        }

        #endregion

        #region Statements and Expressions

        [Fact]
        public void IfStatement()
        {
            var engine = new CSharpScriptEngine();
            var session = engine.CreateSession();

            object result = session.Execute(@"
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
");

            Assert.Equal(5, result);
        }

        [Fact]
        public void ExprStmtParenthesesUsedToOverrideDefaultEval()
        {
            var engine = new CSharpScriptEngine();
            int i = engine.CreateSession().Execute<int>(@"(4 + 5) * 2");
            Assert.Equal(18, i);

            long l = engine.CreateSession().Execute<long>(@"6 / (2 * 3)");
            Assert.Equal(1, l);
        }

        [Fact]
        public void ExprStmtWithMethodCall()
        {
            var engine = new CSharpScriptEngine();
            var session = engine.CreateSession();
            session.Execute(@"
int Foo() { return 2;}
");
            object value = session.Execute(@"(4 + 5) * Foo()");
            Assert.Equal(18, value);
        }

        [Fact]
        public void ArithmeticOperators_IdentifierAddition()
        {
            var engine = new CSharpScriptEngine();
            var session = engine.CreateSession();
            object value;

            value = session.Execute(@"int x = 1;");
            Assert.Equal(null, value);

            value = session.Execute(@"int y = 2;");
            Assert.Equal(null, value);

            value = session.Execute(@"x + y");
            Assert.Equal(3, value);
        }

        [WorkItem(527850)]
        [Fact]
        public void ArithmeticOperators_MultiplicationExpression()
        {
            var engine = new CSharpScriptEngine();
            var session = engine.CreateSession();

            session.Execute(@"int i = 5;");

            Assert.Equal(25, session.Execute(@"i* i"));

            ScriptingTestHelpers.AssertCompilationError(session, "i* i;",
                // (1,1): error CS0201: Only assignment, call, increment, decrement, and new object expressions can be used as a statement
                Diagnostic(ErrorCode.ERR_IllegalStatement, "i* i"));
        }

        [WorkItem(527850)]
        [WorkItem(522569)]
        [Fact(Skip = "522569")]
        public void TopLevelLabel()
        {
            //var engine = new ScriptEngine();

            //ScriptingTestHelpers.AssertCompilationError(engine, "Label: ;",
            //    // (1,1): error CS8000: This language feature ('Label in interactive') is not yet implemented in Roslyn.
            //    // Label: ;
            //    Diagnostic(ErrorCode.ERR_NotYetImplementedInRoslyn, "Label: ;").WithArguments("Label in interactive")
            //);
        }

        [WorkItem(541210)]
        [Fact]
        public void TopLevelGoto()
        {
            var engine = new CSharpScriptEngine();

            ScriptingTestHelpers.AssertCompilationError(engine, "goto Object;",
                // (1,1): error CS0159: No such label 'Object' within the scope of the goto statement
                // goto Object;
                Diagnostic(ErrorCode.ERR_LabelNotFound, "Object").WithArguments("Object")
            );
        }

        [WorkItem(5397, "DevDiv_Projects/Roslyn")]
        [Fact]
        public void TopLevelLambda()
        {
            var engine = new CSharpScriptEngine();
            var session = engine.CreateSession();

            session.Execute(@"
using System;
delegate void TestDelegate(string s);
");

            session.Execute(@"
TestDelegate testDelB = delegate (string s) { Console.WriteLine(s); };
");

            using (var redirect = new OutputRedirect(CultureInfo.InvariantCulture))
            {
                session.Execute(@"testDelB(""hello"");");
                Assert.Equal("hello", redirect.Output.Trim());
            }
        }

        [Fact]
        public void Closure()
        {
            var engine = new CSharpScriptEngine();

            var f = engine.CreateSession().Execute<Func<int, int>>(@"
int Foo(int arg) { return arg + 1; }

System.Func<int, int> f = (arg) =>
{
    return Foo(arg);
};

f
");
            Assert.Equal(3, f(2));
        }

        [Fact]
        public void Closure2()
        {
            var engine = new CSharpScriptEngine();

            var result = engine.CreateSession().Execute<List<string>>(@"
#r ""System.Core""
using System;
using System.Linq;
using System.Collections.Generic;

List<string> result = new List<string>();
string s = ""hello"";
Enumerable.ToList(Enumerable.Range(1, 2)).ForEach(x => result.Add(s));
result
");
            AssertEx.Equal(new[] { "hello", "hello" }, result);
        }

        [Fact]
        public void UseDelegateMixStaticAndDynamic()
        {
            var engine = new CSharpScriptEngine();
            var session = engine.CreateSession();

            session.Execute("using System;");
            session.Execute("int Sqr(int x) {return x*x;}");

            var f = (Func<int, int>)session.Execute("new Func<int,int>(Sqr)");

            Assert.Equal(4, f(2));
        }

        [WorkItem(9229, "DevDiv_Projects/Roslyn")]
        [Fact]
        public void Arrays()
        {
            var engine = new CSharpScriptEngine();
            var session = engine.CreateSession();

            session.Execute(@"
int[] arr_1 = { 1, 2, 3 };
int[] arr_2 = new int[] { 1, 2, 3 };
int[] arr_3 = new int[5];
");

            session.Execute(@"
arr_2[0] = 5;
");

            Assert.Equal(3, session.Execute(@"arr_1[2]"));
            Assert.Equal(5, session.Execute(@"arr_2[0]"));
            Assert.Equal(0, session.Execute(@"arr_3[0]"));
        }

        [Fact]
        public void FieldInitializers()
        {
            var engine = new CSharpScriptEngine();
            var result = (List<int>)engine.CreateSession().Execute(@"
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
");
            Assert.Equal(3, result.Count);
            Assert.Equal(3, result[0]);
            Assert.Equal(6, result[1]);
            Assert.Equal(18, result[2]);
        }

        [Fact]
        public void FieldInitializersWithBlocks()
        {
            var engine = new CSharpScriptEngine();
            var result = (List<int>)engine.CreateSession().Execute(@"
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
");
            Assert.Equal(4, result.Count);
            Assert.Equal(1, result[0]);
            Assert.Equal(2, result[1]);
            Assert.Equal(1, result[2]);
            Assert.Equal(2, result[3]);
        }

        [Fact]
        public void TestInteractiveClosures()
        {
            var engine = new CSharpScriptEngine();
            var session = engine.CreateSession();
            session.Execute(@"
using System.Collections.Generic;
static List<int> result = new List<int>();");
            session.Execute("int x = 1;");
            session.Execute("System.Func<int> f = () => x++;");
            session.Execute("result.Add(f());");
            session.Execute("result.Add(x);");
            var result = (List<int>)session.Execute("result");

            Assert.Equal(2, result.Count);
            Assert.Equal(1, result[0]);
            Assert.Equal(2, result[1]);
        }

        [Fact]
        public void ExtensionMethods()
        {
            var engine = new CSharpScriptEngine();
            var session = engine.CreateSession();
            session.AddReference(typeof(Enumerable).GetTypeInfo().Assembly);
            var result = (int)session.Execute(
@"using System.Linq;
string[] fruit = { ""banana"", ""orange"", ""lime"", ""apple"", ""kiwi"" };
fruit.Skip(1).Where(s => s.Length > 4).Count()
");
            Assert.Equal(2, result);
        }

        [WorkItem(541166)]
        [Fact]
        public void DefineExtensionMethods()
        {
            var engine = new CSharpScriptEngine();
            var session = engine.CreateSession();
            session.AddReference(TestReferences.NetFx.v4_0_30319.System_Core);

            // No error for extension method defined in interactive session.
            session.Execute("static void E(this object o) { }");

            ScriptingTestHelpers.AssertCompilationError(session, "void F(this object o) { }",
                // (1,6): error CS1105: Extension method must be static
                // void F(this object o) { }
                Diagnostic(ErrorCode.ERR_BadExtensionMeth, "F").WithLocation(1, 6));

            ScriptingTestHelpers.AssertCompilationError(session, "static void G(this dynamic o) { }",
                // error CS1103: The first parameter of an extension method cannot be of type 'dynamic'
                Diagnostic(ErrorCode.ERR_BadTypeforThis, "dynamic").WithArguments("dynamic"));
        }

        [Fact]
        public void ImplicitlyTypedFields()
        {
            var engine = new CSharpScriptEngine();
            var result = engine.CreateSession().Execute<object[]>(@"
var x = 1;
var y = x;
var z = foo(x);

string foo(int a) { return null; } 
int foo(string a) { return 0; }

new object[] { x, y, z }
");
            AssertEx.Equal(new object[] { 1, 1, null }, result);
        }

        /// <summary>
        /// Name of PrivateImplementationDetails type needs to be unique across submissions.
        /// The compiler should suffix it with a MVID of the current submission module so we should be fine.
        /// </summary>
        [WorkItem(949559)]
        [WorkItem(540237)]
        [WorkItem(9229, "DevDiv_Projects/Roslyn")]
        [WorkItem(2721, "https://github.com/dotnet/roslyn/issues/2721")]
        [Fact]
        public void PrivateImplementationDetailsType()
        {
            var engine = new CSharpScriptEngine();
            object result = engine.CreateSession().Execute(@"new int[] { 1,2,3,4 }");
            Assert.IsType<int[]>(result);

            result = engine.CreateSession().Execute(@"new int[] { 1,2,3,4,5 }");
            Assert.IsType<int[]>(result);
        }

        [Fact]
        public void NoAwait()
        {
            var engine = new CSharpScriptEngine();
            var session = engine.CreateSession();
            // No await. The return value is Task<int> rather than int.
            var result = (Task<int>)session.Execute("System.Threading.Tasks.Task.FromResult(1)");
            Assert.Equal(1, result.Result);
        }

        /// <summary>
        /// 'await' expression at top-level.
        /// </summary>
        [Fact]
        public void Await()
        {
            var engine = new CSharpScriptEngine();
            var session = engine.CreateSession();
            var result = session.Execute("await System.Threading.Tasks.Task.FromResult(2)");
            Assert.Equal(2, result);
        }

        /// <summary>
        /// 'await' in sub-expression.
        /// </summary>
        [Fact]
        public void AwaitSubExpression()
        {
            var engine = new CSharpScriptEngine();
            var session = engine.CreateSession();
            var result = session.Execute("0 + await System.Threading.Tasks.Task.FromResult(3)");
            Assert.Equal(3, result);
        }

        [Fact]
        public void AwaitVoid()
        {
            var engine = new CSharpScriptEngine();
            var session = engine.CreateSession();
            var task = session.ExecuteAsync<object>("await System.Threading.Tasks.Task.Run(() => { })");
            Assert.Equal(null, task.Result);
            Assert.Equal(TaskStatus.RanToCompletion, task.Status);
        }

        /// <summary>
        /// 'await' in lambda should be ignored.
        /// </summary>
        [Fact]
        public void AwaitInLambda()
        {
            var engine = new CSharpScriptEngine();
            var session = engine.CreateSession();
            session.Execute(
@"using System;
using System.Threading.Tasks;
static T F<T>(Func<Task<T>> f)
{
    return f().Result;
}
static T G<T>(T t, Func<T, Task<T>> f)
{
    return f(t).Result;
}");
            var result = session.Execute("F(async () => await Task.FromResult(4))");
            Assert.Equal(4, result);
            result = session.Execute("G(5, async x => await Task.FromResult(x))");
            Assert.Equal(5, result);
        }

        #endregion

        #region References
        
        [Fact]
        public void AddReference_Errors()
        {
            var moduleRef = ModuleMetadata.CreateFromImage(TestResources.MetadataTests.NetModule01.ModuleCS00).GetReference();

            var engine = new CSharpScriptEngine();
            AssertEx.ThrowsArgumentNull("reference", () => engine.AddReference(reference: null));
            AssertEx.ThrowsArgumentNull("assembly", () => engine.AddReference(assembly: null));
            AssertEx.ThrowsArgumentNull("assemblyDisplayNameOrPath", () => engine.AddReference(assemblyDisplayNameOrPath: null));
            AssertEx.ThrowsArgumentException("reference", () => engine.AddReference(moduleRef));

            var session = engine.CreateSession();
            AssertEx.ThrowsArgumentNull("reference", () => session.AddReference(reference: null));
            AssertEx.ThrowsArgumentNull("assembly", () => session.AddReference(assembly: null));
            AssertEx.ThrowsArgumentNull("assemblyDisplayNameOrPath", () => session.AddReference(assemblyDisplayNameOrPath: null));
            AssertEx.ThrowsArgumentException("reference", () => session.AddReference(moduleRef));
        }

        [Fact]
        public void ReferenceDirective_FileWithDependencies()
        {
            var engine = new CSharpScriptEngine();

            string file1 = Temp.CreateFile().WriteAllBytes(TestResources.MetadataTests.InterfaceAndClass.CSClasses01).Path;
            string file2 = Temp.CreateFile().WriteAllBytes(TestResources.MetadataTests.InterfaceAndClass.CSInterfaces01).Path;

            // ICSPropImpl in CSClasses01.dll implements ICSProp in CSInterfces01.dll.
            object result = engine.CreateSession().Execute(@"
#r """ + file1 + @"""
#r """ + file2 + @"""
new Metadata.ICSPropImpl()
");
            Assert.NotNull(result);
        }

        #endregion

        #region UsingDeclarations

        [Fact]
        public void UsingAlias()
        {
            var engine = new CSharpScriptEngine();
            object result = engine.CreateSession().Execute(@"
using D = System.Collections.Generic.Dictionary<string, int>;
D d = new D();

d
");
            Assert.True(result is Dictionary<string, int>, "Expected Dictionary<string, int>");
        }

        [WorkItem(9229, "DevDiv_Projects/Roslyn")]
        [Fact]
        public void Usings1()
        {
            var engine = new CSharpScriptEngine();

            var session = engine.CreateSession();
            session.ImportNamespace("System");
            session.ImportNamespace("System.Linq");
            session.AddReference(typeof(Enumerable).GetTypeInfo().Assembly);

            // TODO: AssertEx.Equal(new[] { "System", "System.Linq" }, session.GetImportedNamespaces());

            object result = session.Execute("new int[] { 1, 2, 3 }.First()");
            Assert.Equal(1, result);
        }

        [WorkItem(9229, "DevDiv_Projects/Roslyn")]
        [Fact]
        public void Usings2()
        {
            var engine = new CSharpScriptEngine();
            engine.ImportNamespace("System");
            engine.ImportNamespace("System.Linq");

            var session = engine.CreateSession();
            session.AddReference(typeof(Enumerable).GetTypeInfo().Assembly);

            object result = session.Execute("new int[] { 1, 2, 3 }.First()");
            Assert.Equal(1, result);

            session.ImportNamespace("System.Collections.Generic");

            result = session.Execute("new List<int>()");
            Assert.True(result is List<int>, "Expected List<int>");
        }

        [Fact]
        public void AddImportedNamespace_Errors()
        {
            var engine = new CSharpScriptEngine();

            // we only check if the specified name is a valid CLR namespace name, it might not be a valid C#/VB namespace name:
            Assert.Throws<ArgumentException>(() => engine.ImportNamespace(""));
            Assert.Throws<ArgumentException>(() => engine.ImportNamespace("blah."));
            Assert.Throws<ArgumentException>(() => engine.ImportNamespace("b\0lah"));
            Assert.Throws<ArgumentException>(() => engine.ImportNamespace(".blah"));

            // no immediate error, error is reported if the namespace can't be found when compiling:
            engine.ImportNamespace("?1");

            var session1 = engine.CreateSession();

            ScriptingTestHelpers.AssertCompilationError(session1, "1",
                // error CS0246: The type or namespace name '?1' could not be found (are you missing a using directive or an assembly reference?)
                Diagnostic(ErrorCode.ERR_SingleTypeNameNotFound).WithArguments("?1"));

            var session2 = engine.CreateSession();
            Assert.Throws<ArgumentException>(() => session2.ImportNamespace(""));
            Assert.Throws<ArgumentException>(() => session2.ImportNamespace("blah."));
            Assert.Throws<ArgumentException>(() => session2.ImportNamespace("b\0lah"));
            Assert.Throws<ArgumentException>(() => session2.ImportNamespace(".blah"));

            session2.ImportNamespace("?2");

            ScriptingTestHelpers.AssertCompilationError(session2, "1",
                // error CS0246: The type or namespace name '?1' could not be found (are you missing a using directive or an assembly reference?)
                Diagnostic(ErrorCode.ERR_SingleTypeNameNotFound).WithArguments("?1"),
                // error CS0246: The type or namespace name '?2' could not be found (are you missing a using directive or an assembly reference?)
                Diagnostic(ErrorCode.ERR_SingleTypeNameNotFound).WithArguments("?2"));
        }

        #endregion UsingDeclarations

        #region Host Object Binding and Conversions

        public class C<T>
        {
        }

        [Fact]
        public void Submission_HostConversions()
        {
            var engine = new CSharpScriptEngine();
            int value = engine.CreateSession().Execute<int>(@"1+1");
            Assert.Equal(2, value);

            string str = engine.CreateSession().Execute<string>(@"null");
            Assert.Equal(null, str);

            try
            {
                engine.CreateSession().Execute<C<int>>(@"null");
                Assert.True(false, "Expected an exception");
            }
            catch (CompilationErrorException e)
            {
                // error CS0400: The type or namespace name 'Microsoft.CodeAnalysis.CSharp.UnitTests.Symbols.Source.InteractiveSessionTests+C`1[[System.Int32, mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089]], Roslyn.Compilers.CSharp.Emit.UnitTests, Version=42.42.42.42, Culture=neutral, PublicKeyToken=fc793a00266884fb' could not be found in the global namespace (are you missing an assembly reference?)
                Assert.Equal(ErrorCode.ERR_GlobalSingleTypeNameNotFound, (ErrorCode)e.Diagnostics.Single().Code);
                // Can't use Verify() because the version number of the test dll is different in the build lab.
            }

            var session = engine.CreateSession();
            session.AddReference(typeof(C<>).GetTypeInfo().Assembly);
            var cint = session.Execute<C<int>>(@"null");
            Assert.Equal(null, cint);

            // TODO (tomat): Nullable types not supported by the compiler
            // int? ni = engine.Execute<int?>(@"null");
            // Assert.Equal(null, ni);

            try
            {
                engine.CreateSession().Execute<int>(@"null");
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
                engine.CreateSession().Execute<string>(@"1+1");
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
            var engine = new CSharpScriptEngine();
            var session = engine.CreateSession();

            var code = @"
using System;
using System.Collections.Generic;
new List<ArgumentException>()
";

            IEnumerable<Exception> value = session.Execute<IEnumerable<Exception>>(code);

            Assert.Equal(null, value.FirstOrDefault());
        }

        public class B
        {
            public int x = 1, w = 4;
        }

        public class C : B, I
        {
            public static readonly int StaticField = 123;

            private string _n = "2";

            public int Y { get { return 2; } }

            public string N
            {
                get
                {
                    return _n;
                }
                set
                {
                    _n = value;
                }
            }

            public int Z()
            {
                return 3;
            }

            public override int GetHashCode()
            {
                return 123;
            }
        }

        public interface I
        {
            string N { get; set; }
            int Z();
        }

        private class PrivateClass : I
        {
            public string N
            {
                get
                {
                    return null;
                }
                set
                {
                }
            }

            public int Z()
            {
                return 3;
            }
        }

        public class M<T>
        {
            private int F()
            {
                return 3;
            }

            public T G()
            {
                return default(T);
            }
        }

        [Fact]
        public void CreateSession_Arguments()
        {
            var engine = new CSharpScriptEngine();
            engine.AddReference(typeof(C).GetTypeInfo().Assembly);

            Assert.Throws<ArgumentNullException>(() => engine.CreateSession(null));
            Assert.Throws<ArgumentNullException>(() => engine.CreateSession(null, typeof(C)));
            Assert.Throws<ArgumentNullException>(() => engine.CreateSession(new C(), null));
            Assert.Throws<ArgumentNullException>(() => engine.CreateSession<C>(null));
            Assert.Throws<ArgumentException>(() => engine.CreateSession(new C(), typeof(D)));

            var s = engine.CreateSession(new C());
            Assert.Equal(typeof(C), s.HostObjectType);

            s = engine.CreateSession(new C(), typeof(B));
            Assert.Equal(typeof(B), s.HostObjectType);

            s = engine.CreateSession<B>(new C());
            Assert.Equal(typeof(B), s.HostObjectType);
        }

        [Fact]
        public void HostObjectBinding_PublicClassMembers()
        {
            var engine = new CSharpScriptEngine();
            var c = new C();
            var session = engine.CreateSession(c);

            // TODO: should not be neccessary
            session.AddReference(SystemRuntimeAssembly);

            session.AddReference(typeof(C).GetTypeInfo().Assembly);

            int result = session.Execute<int>("x + Y + Z()");
            Assert.Equal(6, result);

            result = session.Execute<int>("x");
            Assert.Equal(1, result);

            session.Execute("int x = 20;");

            result = session.Execute<int>("x");
            Assert.Equal(20, result);
        }

        [Fact]
        public void HostObjectBinding_PublicGenericClassMembers()
        {
            var engine = new CSharpScriptEngine();
            var m = new M<string>();
            var session = engine.CreateSession(m);

            // TODO: should not be neccessary
            session.AddReference(SystemRuntimeAssembly);

            session.AddReference(typeof(M<string>).GetTypeInfo().Assembly);

            string result = session.Execute<string>("G()");
            Assert.Equal(null, result);
        }

        [Fact]
        public void HostObjectBinding_Interface()
        {
            var engine = new CSharpScriptEngine();
            var c = new C();
            var session = engine.CreateSession<I>(c);

            session.AddReference(typeof(C).GetTypeInfo().Assembly);

            int result = session.Execute<int>("Z()");
            Assert.Equal(3, result);

            ScriptingTestHelpers.AssertCompilationError(session, @"x + Y",
                // The name '{0}' does not exist in the current context
                Diagnostic(ErrorCode.ERR_NameNotInContext, "x").WithArguments("x"),
                Diagnostic(ErrorCode.ERR_NameNotInContext, "Y").WithArguments("Y")
            );

            string result2 = session.Execute<string>("N");
            Assert.Equal("2", result2);
        }

        [Fact]
        public void HostObjectBinding_PrivateClass()
        {
            var engine = new CSharpScriptEngine();
            var c = new PrivateClass();
            var session = engine.CreateSession(c);

            // TODO: should not be neccessary
            session.AddReference(SystemRuntimeAssembly);

            session.AddReference(typeof(PrivateClass).GetTypeInfo().Assembly);

            ScriptingTestHelpers.AssertCompilationError(session, @"Z()",
                // (1,1): error CS0122: '<Fully Qualified Name of PrivateClass>.Z()' is inaccessible due to its protection level
                // Z()
                Diagnostic(ErrorCode.ERR_BadAccess, "Z").WithArguments(typeof(PrivateClass).FullName.Replace("+", ".") + ".Z()"));
        }

        [Fact]
        public void HostObjectBinding_PrivateMembers()
        {
            var engine = new CSharpScriptEngine();
            object m = new M<int>();
            var session = engine.CreateSession(m);

            // TODO: should not be neccessary
            session.AddReference(SystemRuntimeAssembly);

            session.AddReference(typeof(M<int>).GetTypeInfo().Assembly);

            ScriptingTestHelpers.AssertCompilationError(session, @"Z()",
                // (1,1): error CS0103: The name 'z' does not exist in the current context
                Diagnostic(ErrorCode.ERR_NameNotInContext, "Z").WithArguments("Z")
            );
        }

        [Fact]
        public void HostObjectBinding_PrivateClassImplementingPublicInterface()
        {
            var engine = new CSharpScriptEngine();
            var c = new PrivateClass();
            var session = engine.CreateSession<I>(c);

            session.AddReference(typeof(PrivateClass).GetTypeInfo().Assembly);

            int result = session.Execute<int>("Z()");
            Assert.Equal(3, result);
        }

#if false  // all these tests make assumption that globals type assembly is not referenced except if explicit, but that is new default.
        [Fact]
        public void HostObjectBinding_MissingReference1()
        {
            var engine = new CSharpScriptEngine();
            var c = new C();
            var session = engine.CreateSession(c);

            ScriptingTestHelpers.AssertCompilationError(session, "x",
                // The name '{0}' does not exist in the current context (are you missing a reference to assembly '{1}'?)
                Diagnostic(ErrorCode.ERR_NameNotInContextPossibleMissingReference, "x").WithArguments("x", typeof(C).Assembly.FullName)
            );

            session.AddReference(typeof(C).Assembly);

            int result = session.Execute<int>("x");
            Assert.Equal(1, result);
        }

        [Fact]
        public void HostObjectBinding_MissingReference2()
        {
            var engine = new CSharpScriptEngine();
            var c = new C();
            var session = engine.CreateSession(c);

            ScriptingTestHelpers.AssertCompilationError(session, "x + Y",
                // The name '{0}' does not exist in the current context (are you missing a reference to assembly '{1}'?)
                Diagnostic(ErrorCode.ERR_NameNotInContextPossibleMissingReference, "x").WithArguments("x", typeof(C).Assembly.FullName),
                Diagnostic(ErrorCode.ERR_NameNotInContextPossibleMissingReference, "Y").WithArguments("Y", typeof(C).Assembly.FullName)
            );

            int result = session.Execute<int>(@"
#r """ + typeof(C).Assembly.Location + @"""
x            
");

            Assert.Equal(1, result);
        }

        [Fact]
        public void HostObjectBinding_MissingReference3()
        {
            var engine = new CSharpScriptEngine();
            var c = new C();
            var session = engine.CreateSession(c);

            session.Execute(@"int y = 1;");

            ScriptingTestHelpers.AssertCompilationError(session, @"
int y = 2;
x",
                // The name '{0}' does not exist in the current context (are you missing a reference to assembly '{1}'?)
                Diagnostic(ErrorCode.ERR_NameNotInContextPossibleMissingReference, "x").WithArguments("x", typeof(C).Assembly.FullName)
            );

            int result = session.Execute<int>(@"y");
            Assert.Equal(1, result);
        }
#endif

        [Fact]
        public void HostObjectBinding_MissingHostObjectContext()
        {
            var engine = new CSharpScriptEngine();
            var session = engine.CreateSession();

            session.AddReference(typeof(PrivateClass).GetTypeInfo().Assembly);

            ScriptingTestHelpers.AssertCompilationError(session, @"Z()",
                // (1,1): error CS0103: The name 'z' does not exist in the current context
                Diagnostic(ErrorCode.ERR_NameNotInContext, "Z").WithArguments("Z")
            );
        }

        [Fact]
        public void HostObjectBinding_InStaticContext()
        {
            var engine = new CSharpScriptEngine();
            var c = new C();
            var session = engine.CreateSession(c);

            // TODO: should not be neccessary
            session.AddReference(SystemRuntimeAssembly);

            session.AddReference(typeof(C).GetTypeInfo().Assembly);

            var typeName = typeof(InteractiveSessionTests).FullName;

            ScriptingTestHelpers.AssertCompilationError(session, @"
static int Foo() { return x; }
static int Bar { get { return Y; } set { return Z(); } }
static int Baz = w;
",
                // (4,18): error CS0120: An object reference is required for the non-static field, method, or property 'Roslyn.Compilers.CSharp.UnitTests.Symbols.Source.InteractiveSessionTests.B.w'
                Diagnostic(ErrorCode.ERR_ObjectRequired, "w").WithArguments(typeName + ".B.w"),
                // (2,27): error CS0120: An object reference is required for the non-static field, method, or property 'Roslyn.Compilers.CSharp.UnitTests.Symbols.Source.InteractiveSessionTests.B.x'
                Diagnostic(ErrorCode.ERR_ObjectRequired, "x").WithArguments(typeName + ".B.x"),
                // (3,31): error CS0120: An object reference is required for the non-static field, method, or property 'Roslyn.Compilers.CSharp.UnitTests.Symbols.Source.InteractiveSessionTests.C.Y'
                Diagnostic(ErrorCode.ERR_ObjectRequired, "Y").WithArguments(typeName + ".C.Y"),
                // (3,49): error CS0120: An object reference is required for the non-static field, method, or property 'Roslyn.Compilers.CSharp.UnitTests.Symbols.Source.InteractiveSessionTests.C.Z()'
                Diagnostic(ErrorCode.ERR_ObjectRequired, "Z").WithArguments(typeName + ".C.Z()")
            );
        }

        [Fact]
        public void HostObjectBinding_StaticMembers()
        {
            var engine = new CSharpScriptEngine();
            var c = new C();
            var session = engine.CreateSession(c);
            session.AddReference(typeof(C).GetTypeInfo().Assembly);
            object result;

            session.Execute("static int foo = StaticField;");
            session.Execute("static int bar { get { return foo; } }");
            session.Execute("class C { public static int baz() { return bar; } }");
            result = session.Execute("C.baz()");
            Assert.Equal(123, result);
        }

        public class D
        {
            public int foo(int a) { return 0; }
        }

        /// <summary>
        /// Host object members don't form a method group with submission members.
        /// </summary>
        [Fact]
        public void HostObjectBinding_Overloads()
        {
            var engine = new CSharpScriptEngine();
            var d = new D();
            var session = engine.CreateSession(d);
            session.AddReference(typeof(D).GetTypeInfo().Assembly);
            object result;

            session.Execute("int foo(double a) { return 2; }");

            result = session.Execute("foo(1)");
            Assert.Equal(2, result);

            result = session.Execute("foo(1.0)");
            Assert.Equal(2, result);
        }

        [Fact]
        public void HostObjectInRootNamespace()
        {
            var engine = new CSharpScriptEngine();
            var obj = new InteractiveFixtures_TopLevelHostObject { X = 1, Y = 2, Z = 3 };
            var session = engine.CreateSession(obj);
            session.AddReference(typeof(InteractiveFixtures_TopLevelHostObject).GetTypeInfo().Assembly);
            session.Execute("X + Y + Z");

            obj = new InteractiveFixtures_TopLevelHostObject { X = 1, Y = 2, Z = 3 };
            session = engine.CreateSession(obj);
            session.Execute("X");

#if false // globals type assembly is defaulted as reference
            ScriptingTestHelpers.AssertCompilationError(session, "X",
                // (1,1): error CS7012: The name 'X' does not exist in the current context 
                //        (are you missing a reference to assembly 'Roslyn.Compilers.CSharp.UnitTests, Version=1.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35'?)
                Diagnostic(ErrorCode.ERR_NameNotInContextPossibleMissingReference, "X").
                WithArguments("X", typeof(InteractiveFixtures_TopLevelHostObject).Assembly.FullName));
#endif
        }

        #endregion

        #region Erroneous language features

        [Fact]
        public void WRN_LowercaseEllSuffix()
        {
            var engine = new CSharpScriptEngine();
            ScriptingTestHelpers.AssertCompilationError(engine, "int i = 42l;",
                // (1,11): warning CS0078: The 'l' suffix is easily confused with the digit '1' -- use 'L' for clarity
                Diagnostic(ErrorCode.WRN_LowercaseEllSuffix, "l"),
                // (1,9): error CS0266: Cannot implicitly convert type 'long' to 'int'. An explicit conversion exists (are you missing a cast?)
                Diagnostic(ErrorCode.ERR_NoImplicitConvCast, "42l").WithArguments("long", "int"));
        }

        [Fact]
        public void ERR_RecursivelyTypedVariable()
        {
            var engine = new CSharpScriptEngine();
            ScriptingTestHelpers.AssertCompilationError(engine, "var x = x;",
                // (1,5): error CS7019: Type of 'x' cannot be inferred since its initializer directly or indirectly refers to the definition.
                Diagnostic(ErrorCode.ERR_RecursivelyTypedVariable, "x").WithArguments("x"));
        }

        [Fact]
        public void ERR_VariableUsedBeforeDeclaration()
        {
            var engine = new CSharpScriptEngine();
            ScriptingTestHelpers.AssertCompilationError(engine, "var x = 1; { var x = x;}",
                // (2,11): error CS0841: Cannot use local variable 'x' before it is declared
                Diagnostic(ErrorCode.ERR_VariableUsedBeforeDeclaration, "x").WithArguments("x"));
        }

        [Fact]
        public void ERR_SemicolonExpected()
        {
            var engine = new CSharpScriptEngine();
            ScriptingTestHelpers.AssertCompilationError(engine, "T Echo&lt;T&gt;(T t) { return t; } var y = Echo(y);",
#if false // With Declaration Expressions enabled
                // (1,7): error CS1002: ; expected
                Diagnostic(ErrorCode.ERR_SemicolonExpected, "&"),
                // (1,22): error CS1002: ; expected
                Diagnostic(ErrorCode.ERR_SemicolonExpected, "{"));
#else
                // (1,7): error CS1002: ; expected
                Diagnostic(ErrorCode.ERR_SemicolonExpected, "&"),
                // (1,23): error CS1002: ; expected
                Diagnostic(ErrorCode.ERR_CloseParenExpected, "t"),
                // (1,24): error CS1002: ; expected
                Diagnostic(ErrorCode.ERR_SemicolonExpected, "t"),
                // (1,23): error CS1026: ) expected
                Diagnostic(ErrorCode.ERR_SemicolonExpected, ")"),
                // (1,24): error CS7017: Member definition, statement, or end-of-file expected
                Diagnostic(ErrorCode.ERR_GlobalDefinitionOrStatementExpected, ")"));
#endif
        }

        [Fact]
        public void ERR_ReturnNotAllowedInScript()
        {
            var engine = new CSharpScriptEngine();
            ScriptingTestHelpers.AssertCompilationError(engine, "return;",
                // (1,1): error CS7020: You cannot use 'return' in top-level script code
                Diagnostic(ErrorCode.ERR_ReturnNotAllowedInScript, "return"));

            ScriptingTestHelpers.AssertCompilationError(engine, "return 17;",
                // (1,1): error CS7020: You cannot use 'return' in top-level script code
                Diagnostic(ErrorCode.ERR_ReturnNotAllowedInScript, "return"));
        }

        [Fact]
        public void ERR_FieldCantBeRefAny()
        {
            var engine = new CSharpScriptEngine();
            string sources = @"
System.RuntimeArgumentHandle a;
System.ArgIterator b;
System.TypedReference c;
";
            ScriptingTestHelpers.AssertCompilationError(engine, sources,
                // (2,1): error CS0610: Field or property cannot be of type 'System.RuntimeArgumentHandle'
                Diagnostic(ErrorCode.ERR_FieldCantBeRefAny, "System.RuntimeArgumentHandle").WithArguments("System.RuntimeArgumentHandle"),
                // (3,1): error CS0610: Field or property cannot be of type 'System.ArgIterator'
                Diagnostic(ErrorCode.ERR_FieldCantBeRefAny, "System.ArgIterator").WithArguments("System.ArgIterator"),
                // (4,1): error CS0610: Field or property cannot be of type 'System.TypedReference'
                Diagnostic(ErrorCode.ERR_FieldCantBeRefAny, "System.TypedReference").WithArguments("System.TypedReference"));
        }

        [WorkItem(529387)]
        [Fact]
        public void IsVariable_PreviousSubmission()
        {
            var engine = new CSharpScriptEngine();
            var session = engine.CreateSession();
            session.Execute("var x = 1;");
            ScriptingTestHelpers.AssertCompilationError(session, "&x",
                // (1,1): error CS0212: You can only take the address of an unfixed expression inside of a fixed statement initializer
                // &x
                Diagnostic(ErrorCode.ERR_FixedNeeded, "&x").WithLocation(1, 1));
        }

        [Fact]
        public void IsVariable_HostObject()
        {
            var engine = new CSharpScriptEngine();
            var session = engine.CreateSession(new B());
            session.AddReference(typeof(B).GetTypeInfo().Assembly);
            ScriptingTestHelpers.AssertCompilationError(session, "&x",
                // (1,1): error CS0212: You can only take the address of an unfixed expression inside of a fixed statement initializer
                // &x
                Diagnostic(ErrorCode.ERR_FixedNeeded, "&x").WithLocation(1, 1));
        }

        [WorkItem(530404)]
        [Fact]
        public void DiagnosticsPass()
        {
            var engine = new CSharpScriptEngine();
            var session = engine.CreateSession();
            session.AddReference(typeof(System.Linq.Expressions.Expression).GetTypeInfo().Assembly);
            var source = "(System.Linq.Expressions.Expression<System.Func<object>>)(() => null ?? new object())";
            ScriptingTestHelpers.AssertCompilationError(session, source,
                // (1,65): error CS0845: An expression tree lambda may not contain a coalescing operator with a null literal left-hand side
                Diagnostic(ErrorCode.ERR_ExpressionTreeContainsBadCoalesce, "null").WithLocation(1, 65));
        }

        #endregion
    }
}
