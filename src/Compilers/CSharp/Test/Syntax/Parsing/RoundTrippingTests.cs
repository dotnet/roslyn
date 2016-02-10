// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Linq;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.CSharp.Test.Utilities;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.CSharp.UnitTests
{
    public class RoundTrippingTests
    {
        #region Helper

        internal static void ParseAndRoundTripping(string text, int errorCount = 0, int memberCount = 0)
        {
            ParseAndRoundTripping(text, TestOptions.RegularWithDocumentationComments, errorCount, memberCount);
        }

        internal static void ParseAndRoundTripping(string text, CSharpParseOptions options, int errorCount = 0, int memberCount = 0)
        {
            var tree = SyntaxFactory.ParseSyntaxTree(SourceText.From(text), options);
            var toText = tree.GetCompilationUnitRoot().ToFullString();

            Assert.Equal(text, toText);

            // -1 mean there are errors but actual number of errors is not important.
            // it makes the test more robust in case error count changes
            if (errorCount == -1)
            {
                Assert.NotEqual(0, tree.GetCompilationUnitRoot().ErrorsAndWarnings().Length);
            }
            else
            {
                Assert.Equal(errorCount, tree.GetCompilationUnitRoot().ErrorsAndWarnings().Length);
            }

            // check member count only if > 0
            if (memberCount > 0)
            {
                Assert.Equal(memberCount, tree.GetCompilationUnitRoot().Members.Count);
            }

            ParentChecker.CheckParents(tree.GetCompilationUnitRoot(), tree);
        }

        public static void ParseAndCheckTerminalSpans(string text)
        {
            var tree = SyntaxFactory.ParseSyntaxTree(text);
            var toText = tree.GetCompilationUnitRoot().ToFullString();
            Assert.Equal(text, toText);

            var nodes = tree.GetCompilationUnitRoot().DescendantTokens(tk => tk.FullWidth > 0).ToList();
            if (nodes.Count > 0)
            {
                var prevSpan = nodes[0].FullSpan;
                for (int i = 1; i < nodes.Count; i++)
                {
                    var span = nodes[i].FullSpan;
                    Assert.Equal(prevSpan.End, span.Start);
                    prevSpan = span;
                }
            }
        }

        #endregion

        [Fact]
        public void AutoPropInitializers()
        {
            var experimental = TestOptions.ExperimentalParseOptions;
            ParseAndRoundTripping("class C { int GetInt { get; } = 0; }", experimental, memberCount: 1);
            ParseAndRoundTripping("class C { int GetInt { get; } = 0 }", experimental, 1, 1);
            ParseAndRoundTripping("class C { public int GetInt { get; } = 0; }", experimental, memberCount: 1);
            ParseAndRoundTripping("class C { int GetInt { get; } = 0;; }", experimental, 1, 1);
            ParseAndRoundTripping("class C { int GetInt { get;; } = 0;; }", experimental, 2, 1);
            ParseAndRoundTripping("interface I { int GetInt { get; } = 0; }", experimental, memberCount: 1);
            ParseAndRoundTripping("interface I { int GetInt { get; } = 0 }", experimental, 1, 1);
            ParseAndRoundTripping("interface I { public int GetInt { get; } = 0; }", experimental, memberCount: 1);
        }

        [Fact()]
        [WorkItem(530410, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/530410")]
        public void NullChar()
        {
            ParseAndRoundTripping("\0", 1);
            ParseAndRoundTripping("abc\0def", 3);
            ParseAndRoundTripping("\0abc", 2);
            ParseAndRoundTripping("class C { string s = \"\0\"; }", 0);
        }

        [Fact()]
        [WorkItem(530410, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/530410")]
        public void CharMaxValue()
        {
            string text = "abc" + char.MaxValue + "def";
            var tree = SyntaxFactory.ParseSyntaxTree(SourceText.From(text), path: "");
            var toText = tree.GetCompilationUnitRoot().ToFullString();
            Assert.Equal(text, toText);
        }

        [Fact]
        public void TestOptionalFloat()
        {
            ParseAndRoundTripping(Resources.OptionalFloat);
        }

        [Fact]
        public void TestOptionalParamsArray()
        {
            ParseAndRoundTripping(Resources.OptionalParamsArray);
        }

        [WorkItem(862632, "DevDiv/Personal")]
        [Fact]
        public void TestNegInvalidExternAlias01()
        {
            ParseAndRoundTripping(Resources.InvalidExternAlias01, -1);
        }

        [WorkItem(901348, "DevDiv/Personal")]
        [Fact]
        public void TestNegPartialAliasedName()
        {
            ParseAndRoundTripping(Resources.PartialAliasedName, -1);
        }

        [WorkItem(894884, "DevDiv/Personal")]
        [Fact]
        public void TestNegPartialInKeyword()
        {
            ParseAndRoundTripping(Resources.PartialInKeyword, -1);
        }

        [WorkItem(901493, "DevDiv/Personal")]
        [Fact]
        public void TestNegPartialAttribute()
        {
            // although this code snippet has multiple statements on top level we report that as semantic errors, not parse errors:
            ParseAndRoundTripping(Resources.PartialNewAttributeArray, 0);
        }

        [WorkItem(901498, "DevDiv/Personal")]
        [Fact]
        public void TestNegPartialPreProcessorExpression()
        {
            ParseAndRoundTripping(Resources.PartialPreprocessorExpression, -1);
        }

        [WorkItem(901508, "DevDiv/Personal")]
        [Fact]
        public void TestNegPartialUnicodeIdentifier()
        {
            ParseAndRoundTripping(Resources.PartialUnicodeIdentifier, -1);
        }

        [WorkItem(901516, "DevDiv/Personal")]
        [Fact]
        public void TestNegIncompleteSwitchBlock()
        {
            ParseAndRoundTripping(Resources.PartialSwitchBlock, -1);
        }

        [Fact]
        public void TestNegBug862116()
        {
            var text = @"
namespace x
{
    public class a
    {
        public int hiddenMember2;
    }
    public class b : a
    {
        public override int hiddenMember2
        {
        public static void Main()
        {
        }
    }
}
";
            ParseAndRoundTripping(text, -1);
        }

        [Fact]
        public void TestNegBug862635()
        {
            var text = @"
class Test
{
    static void Main() 
    {
        ::Console.WriteLine(""Missing identifier before :: "");
    }
}
";
            ParseAndRoundTripping(text, -1);
        }

        [Fact]
        public void Bug862637()
        {
            var text = @"
using System;
public class Test
{
    static void Main()
    {
         Console.WriteLine(dDep.GetType().Assembly.FullName);
    }
}
";
            ParseAndRoundTripping(text);
        }

        [Fact]
        public void Bug862640()
        {
            var text = @"
public class Production
{
    public Production()
    {
        ((VoidDelegate)delegate
        {
            this.someType.Iterate(delegate(object o)
            {
                System.Console.WriteLine(((BoolDelegate)delegate { return object.Equals(o, this.epsilon); })());
            });
        })();
    }
}
";
            ParseAndRoundTripping(text);
        }

        [Fact]
        public void TestNegBug862642()
        {
            var text = @"
alias myAlias;
class myClass
{
}
";
            // top-level field declaration is a semantic error
            ParseAndRoundTripping(text);
        }

        [Fact]
        public void TestNegBug862643()
        {
            var text = @"
using System;
[AttributeUsage(AttributeTargets.All)]
public class Foo : Attribute
    {
    public int Name;
    public Foo (int sName) {Name = sName;}
    }
public class Class1 {
 int Meth2 ([event:Foo(5)]int parm) {return 0;}
 public int IP { get {return 0;} set {}}
}
";
            ParseAndRoundTripping(text);
        }

        [Fact]
        public void Bug862644()
        {
            var text = @"
using System;
public class Test
{
 [method:MyAttribute(TypeObject = new int[1].GetType())]
 public void foo()
 {
 }
}
";
            ParseAndRoundTripping(text);
        }

        [Fact]
        public void Bug862646()
        {
            var text = @"
// C# compiler emits Void& type
";
            ParseAndRoundTripping(text);
        }

        [Fact]
        public void Bug862648()
        {
            var text = @"
class TestClass
{
    static void Main()
    {
       int partial;
    }
}
";
            ParseAndRoundTripping(text);
        }

        [Fact]
        public void Bug870754()
        {
            var text = @"class C{
C(){
int y = 3;
(y).ToString();
}
}
";
            ParseAndRoundTripping(text);
        }

        [Fact]
        public void TestNegBug875711()
        {
            var text = @"
using System;
public class A
{
 public static operator ++ A(int i)
 {
 }
}
";
            ParseAndRoundTripping(text, -1);
        }

        [Fact]
        public void TestNegBug876359()
        {
            var text = @"
class C {
  fixed x;
}
";
            ParseAndRoundTripping(text, -1);
        }

        [Fact]
        public void TestNegBug876363()
        {
            var text = @"
class X { void f() {
int a = 1; \u000a int b = (int)2.3;
}
}
";
            ParseAndRoundTripping(text, -1);
        }

        [Fact]
        public void Bug876565()
        {
            var text = @"
public class C
{
    public static int Main()
    {
        int result = 0;
        Func<int?, IStr0?> f1 = (int? x) => x;
    }
}
";
            ParseAndRoundTripping(text);
        }

        [Fact]
        public void Bug876573()
        {
            var text = @"
[partial]
partial class partial { }
partial class partial
{
    public partial()
    {
        fld1 = fld2 = fld3 = fld4 = -1;
    }
}
";
            ParseAndRoundTripping(text);
        }

        [Fact]
        public void TestNegBug876575()
        {
            var text = @"partial enum E{}";
            ParseAndRoundTripping(text, 1);
        }

        [Fact]
        public void Bug877232()
        {
            var text = @"
class MyClass : MyBase {
 MyClass() : base() {
 }
}
";
            ParseAndRoundTripping(text);
        }

        [Fact]
        public void TestNegBug877242()
        {
            var text = @"
private namespace test
{
}
";
            ParseAndRoundTripping(text, -1);
        }

        [Fact]
        public void Bug877246()
        {
            var text = @"
public struct Test
{
    static int Main()
    {
        test.emptyStructGenNullableArr = new EmptyStructGen<string, EmptyClass>?[++i];
    }
}
";
            ParseAndRoundTripping(text);
        }

        [Fact]
        public void Bug877251()
        {
            var text = @"
static class Test
{
    static void Main()
    {
        A a = new A { 5, { 1, 2, {1, 2} }, 3 };
    }
}
";
            ParseAndRoundTripping(text, -1);
        }

        [Fact]
        public void TestNegBug877256()
        {
            var text = @"
using System;
public static class Extensions
{
 static ~Extensions(this Struct s) {}
 static ~Extensions(this Struct? s) {}
}
";
            ParseAndRoundTripping(text, -1);
        }

        [Fact]
        public void Bug877313()
        {
            var text = @"
partial class ConstraintsDef
{
    partial void PM<T1, T2>(T1 v1, ref T2 v2, params T2[] v3)
        where T2 : new()
        where T1 : System.IComparable<T1>, System.Collections.IEnumerable;
    partial void PM<T1, T2>(T1 v1, ref T2 v2, params T2[] v3)
    {
        v2 = v3[0];
    }
}
";
            ParseAndRoundTripping(text);
        }

        [Fact]
        public void TestNegBug877318()
        {
            var text = @"
partial class PartialPartial
{
    int i = 1;
    partial partial void PM();
    partial partial void PM()
    {
        i = 0;
    }
    static int Main()
    {
        PartialPartial t = new PartialPartial();
        t.PM();
        return t.i;
    }
}
";
            ParseAndRoundTripping(text, -1);
        }

        [Fact]
        public void TestNegBug879395()
        {
            var text = @"
module m1
end module
";
            ParseAndRoundTripping(text, 2);
        }

        [Fact]
        public void Bug880479()
        {
            var text = @"
class c1
{
void foo(int a, int b, int c)
{
}
}
";
            ParseAndCheckTerminalSpans(text);
        }

        [Fact]
        public void TestNegBug881436()
        {
            var text = @"
public class Test
{
    public static void Main()
    {
        var v1 = new { X = x; Y = y, Z = z };
    }
}
";
            ParseAndRoundTripping(text, -1);
        }

        [Fact]
        public void Bug881480()
        {
            var text = @"
// <Code> 
 
public static class SubGenericClass<T> : GenericClass<T>
{
    public SubGenericClass() : base() { }
}
";
            ParseAndRoundTripping(text);
        }

        [Fact]
        public void Bug881485()
        {
            var text = @"
class Test
{
    public static int Test2()
    {
        var testResult = testFunc((long?)-1);
   }
}
";
            ParseAndRoundTripping(text);
        }

        [Fact]
        public void TestNegBug881488()
        {
            var text = @"partial class A
{
       partial void C<T>(T )=>{ t) { }
}
";
            ParseAndRoundTripping(text, -1);
        }

        [Fact]
        public void TestNegBug882388()
        {
            var text = @"
public class Class1
{
    public int Meth2(int i) {
        [return:Foo(5)]
        return 0;
    }
}
";
            ParseAndRoundTripping(text, -1);
        }

        [Fact]
        public void Bug882417()
        {
            var text = @"
[AttributeUsage(AttributeTargets.Class)]
public class HelpAttribute : Attribute
{
 public HelpAttribute(byte b1) {
     b = b1;
 }
 byte b = 0;
 public byte Verify {get {return b;} }
}
";
            ParseAndRoundTripping(text);
        }

        [Fact]
        public void TestNegBug882424()
        {
            var text = @"
public class MyClass {
 //invalid simple name
 int -foo(){
  return 1;
 }
}
";
            ParseAndRoundTripping(text, -1);
        }

        [Fact]
        public void TestNegBug882432()
        {
            var text = @"
public class Base1 {
    public static E1 {a, b, c, };
}
";
            ParseAndRoundTripping(text, -1);
        }

        [Fact]
        public void TestNegBug882444()
        {
            var text = @"
namespace nms {
public class MyException : ApplicationException {
    public MyException(String str) : base ApplicationException (str)
    {}
};
";
            ParseAndRoundTripping(text, -1, 1);
        }

        [Fact]
        public void Bug882459()
        {
            var text = @"
public partial class Base
{
    ViolateClassConstraint Fld01 = new Base().Meth<ViolateClassConstraint>(new ViolateClassConstraint()); //E:CS0315
}
";
            ParseAndRoundTripping(text);
        }

        [Fact]
        public void TestNegBug882463()
        {
            var text = @"
public class Test
{
 yield break;
 static int Main()
 { 
  return 1;
 }
}
";
            ParseAndRoundTripping(text, -1);
        }

        [Fact]
        public void TestNegBug882465()
        {
            var text = @"
public class Comments
{
 // /* This is a comment 
    This is a comment 
 */
}
";
            ParseAndRoundTripping(text, -1);
        }

        [Fact]
        public void Bug882471()
        {
            var text = @"
class Test
{
    static int Main()
    {
        string \u0069f;
    }
}
";
            ParseAndRoundTripping(text, 0, 1);
        }

        [Fact]
        public void Bug882481()
        {
            var text = @"
#define \u0066oxbar
#if foxbar
class Foo { }
#endif
";
            ParseAndRoundTripping(text, 0, 1);
        }

        [Fact]
        public void TestNegBug882482()
        {
            var text = @"
using System;
class main
{
 public static void Main()
 {
  i\u0066 (true)
   Console.WriteLine(""This should not have worked!"");
 }
}
";
            ParseAndRoundTripping(text, -1);
        }

        [Fact]
        public void Bug882957()
        {
            var text = @"
partial class CNExp
{
    public static long? operator &(CNExp v1, long? v2)
    {
        return null;
    }
}
";
            ParseAndRoundTripping(text);
        }

        [Fact]
        public void Bug882984()
        {
            var text = @"
unsafe partial class C 
{
    byte* buf;
    public byte* ubuf
    {
        set { buf = value;}
    }
}
";
            ParseAndRoundTripping(text);
        }

        [Fact]
        public void Bug883177()
        {
            var text = @"
unsafe
struct Test
{
    public fixed int i[1];
    byte* myIntBuf;
    static int Main()
    {
        int retval = 0;
        Test t = new Test();
        t.i[0] = 0;
        t.myIntBuf = (byte*) t.i;
        if (*t.myIntBuf !=0)
            retval = 1;
        if (retval != 0)
            System.Console.WriteLine(""Failed."");
        return retval;
    }
}
";
            ParseAndRoundTripping(text);
        }

        [Fact]
        [Trait("Regression", "Spans")]
        public void Bug884246()
        {
            var text = @"
using System.Reflection;
#if VER1
[assembly:AssemblyVersionAttribute(""1.0.0.0"")]
#elif VER2
[assembly:AssemblyVersionAttribute(""2.0.0.0"")]
#endif
public class otherClass{}";

            ParseAndCheckTerminalSpans(text);
        }

        [Fact]
        public void Bug890389()
        {
            var text = @"using System;
class C
{
void Foo()
{
Func<string> i = 3.ToString;
}
}
";
            ParseAndRoundTripping(text);
        }

        [Fact]
        public void TestNegBug892249()
        {
            var text = @"using System;
class AAttribute : Attribute {
  public AAttribute(object o) { }
}
[A(new string[] is { ""hello"" })]
class C {
}
";
            ParseAndRoundTripping(text, -1);
        }

        [Fact]
        public void TestNegBug892255()
        {
            var text = @"using System;
public class Test
{
    public static int Main(string [] args)
    {
  int ret = 1;
  switch (false) {
  case true:
   ret = 1;
   break;
  default false:
   ret = 1;
  }
        return(ret);
    }
}
";
            ParseAndRoundTripping(text, -1);
        }

        [WorkItem(894494, "DevDiv/Personal")]
        [Fact]
        public void TestRegressNegExtraPublicKeyword()
        {
            var text = @"
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
namespace ConsoleApplication1
{
 class Program
 {
  static void Main(string[] args)
  {
  }
 }
}
　
public class Class_1_L0
{
/";
            ParseAndRoundTripping(text, -1);
        }

        [Fact]
        public void TestNegBug894884()
        {
            var text = @"
class C
{
public static int Main()
{
switch(str)
{ 
default:
List<in
";
            ParseAndRoundTripping(text, -1);
        }

        [WorkItem(895762, "DevDiv/Personal")]
        [Fact]
        public void TestRegressInfiniteLoopXmlDoc()
        {
            var text = @"
public struct MemberClass<T>
{
/// <summary>
/// We use this to get the values we cannot get directly
/// </summary>
/// <param n";

            ParseAndRoundTripping(text, -1);
        }

        [Fact]
        public void Bug909041()
        {
            var text = @"
interface A
{
void M<T>(T t) where T : class;
}
";
            ParseAndRoundTripping(text);
        }

        [Fact]
        public void Bug909041b()
        {
            var text = @"
public delegate void Del<T>(T t) where T : IEnumerable;
public class A {}
";
            ParseAndRoundTripping(text);
        }

        [Fact]
        public void Bug909041c()
        {
            var text = @"
class A
{
static extern bool Bar<U>() where U : class;
}
";
            ParseAndRoundTripping(text);
        }

        [Fact]
        public void Bug909045()
        {
            var text = @"
public class A
{
public void M<T, V>(T t, V v)
where T : new()
where V : class { }
}
";
            ParseAndRoundTripping(text);
        }

        [WorkItem(909049, "DevDiv/Personal")]
        [WorkItem(911392, "DevDiv/Personal")]
        [Fact]
        public void RegressError4AttributeWithTarget()
        {
            var text = @"using System;
using System.Runtime.InteropServices;

interface IFoo
{
    [return: MarshalAs(UnmanagedType.I2)]
    short M();

    int Prop
    {
        [return: MarshalAs(UnmanagedType.I4)]
        get;
        [param: MarshalAs(UnmanagedType.I4)]
        set;
    }

    long this[[MarshalAs(UnmanagedType.BStr)] string s]
    {
        [return: MarshalAs(UnmanagedType.I8)]
        get;
        set;
    }
}

public class Foo
{
    public delegate void MyDelegate();
    public event MyDelegate eventMethod
    {
        [method: ComVisible(true)]
        add { }
        remove { }
    }
}";
            ParseAndRoundTripping(text);
        }

        [Fact]
        public void Bug909063()
        {
            var text = @"
class A
{
public int i, j;
public static void Main()
{
var v = new A() { i=0, j=1, };
var vv = new[] { 1, 2, };
}
}
";
            ParseAndRoundTripping(text);
        }

        [Fact]
        public void Bug909333()
        {
            var text = @"
extern alias libAlias;
class myClass
{
static int Main()
{
libAlias::otherClass oc = new libAlias.otherClass(); // '::' and '.'
return 0;
}
}
";
            ParseAndRoundTripping(text);
        }

        [Fact]
        public void Bug909334()
        {
            var text = @"
class Test
{
unsafe static int Main()
{
global::System.Int32* p = stackalloc global::System.Int32[5];
return 0;
}
}
";
            ParseAndRoundTripping(text);
        }

        [Fact]
        public void Bug909337()
        {
            var text = @"
using System.Collections;
public class Test<I> 
{
//ctor
public Test(I i)
{ 
}
// dtor
~Test() {}
}
";
            ParseAndRoundTripping(text);
        }

        [Fact]
        public void Bug909338()
        {
            var text = @"
class Test
{
static void Main()
{
var v = typeof(void);
}
}
";
            ParseAndRoundTripping(text);
        }

        [Fact]
        public void Bug909350()
        {
            var text = @"
[Author(""Brian Kernighan""), Author(""Dennis Ritchie""),] 
class Class1
{
}
enum E
{
    One, Two,
}
";
            ParseAndRoundTripping(text);
        }

        [Fact]
        public void Bug909371()
        {
            var text = @"
class A
{
byte M(byte b) 
{ 
return b; 
}
}
";
            ParseAndRoundTripping(text);
        }

        [Fact]
        public void Bug909371b()
        {
            var text = @"
[AttributeUsage(AttributeTargets.Class)]
public class HelpAttribute : Attribute
{
byte b = 0;
public HelpAttribute(byte b1) {
b = b1;
}
}
";
            ParseAndRoundTripping(text);
        }

        [WorkItem(909372, "DevDiv/Personal")]
        [Fact]
        public void RegressError4ValidOperatorOverloading()
        {
            var text = @"using System;

public class A
{
    // unary
    public static bool operator true(A a)
    {
        return false;
    }

    public static bool operator false(A a)
    {
        return true;
    }

    public static A operator ++(A a)
    {
        return a;
    }

    public static A operator --(A a)
    {
        return a;
    }

    // binary
    public static bool operator <(A a, A b)
    {
        return true;
    }
    public static bool operator >(A a, A b)
    {
        return false;
    }
    public static bool operator <=(A a, A b)
    {
        return true;
    }
    public static bool operator >=(A a, A b)
    {
        return false;
    }
}";
            ParseAndRoundTripping(text);
        }

        [Fact]
        public void Bug909419()
        {
            var text = @"
class Test
{
static int Main()
{
  int n1 = test is Test ? 0 : 1;
  int n2 = null == test as Test ? 0 : 1;
  return n1 + n2;
}
}
";
            ParseAndRoundTripping(text);
        }

        [Fact]
        public void Bug909425()
        {
            var text = @"
public class MyClass
{
public static int Main() 
{
  float f1 = 0.7e-44f;
  double d1 = 5.0e-324;
  return M(0.0e+999);
}
static int M(double d) { return 0; }
}
";
            ParseAndRoundTripping(text);
        }

        [WorkItem(909449, "DevDiv/Personal")]
        [Fact]
        public void RegressOverAggressiveWarningForUlongSuffix()
        {
            var text = @"class Program
{
static void Main()
{
ulong x1 = 7L;
// ulong x2 = 7l; // should Warn
ulong x4 = 7Ul; // should NOT warn
ulong x6 = 7ul; // should NOT warn
}
}";
            ParseAndRoundTripping(text);
        }

        [Fact]
        public void Bug909451()
        {
            var text = @"
public class AnonymousTypeTest : ParentClass
{
public void Run()
{
var p1 = new { base.Number };
}
}
";
            ParseAndRoundTripping(text);
        }

        [WorkItem(909828, "DevDiv/Personal")]
        [Fact]
        public void RegressError4ValidUlongLiteral()
        {
            var text = @"public class Test
{
public static int Main()
{
ulong n = 9223372036854775808U; //this should fit ulong
ulong n1 = 9223372036854775808Ul;
ulong n2 = 9223372036854775808uL;
ulong n3 = 9223372036854775808u;
return 0;
}
}
";
            ParseAndRoundTripping(text);
        }

        [WorkItem(922886, "DevDiv/Personal")]
        [Fact]
        public void RegressError4ValidNumericLiteral()
        {
            var text = @"public class Test
{
public static void Main()
{
sbyte min1 = -128, max1 = 127;
short min2 = -32768, max2 = 32767;
int   min3 = -2147483648, max3 = 2147483647;
long  min4 = -9223372036854775808L, max4 = 9223372036854775807L;
byte max5 = 255;
ushort max6 = 65535;
uint max7 = 4294967295;
ulong max8 = 18446744073709551615;
}
}
";
            ParseAndRoundTripping(text);
        }

        [WorkItem(911058, "DevDiv/Personal")]
        [Fact]
        public void RegressError4IdentifierStartWithAt1()
        {
            var text = @"using System;

[AttributeUsage(AttributeTargets.All)]
public class X : Attribute { }
[@X]
class A { }

namespace @namespace
{
    class C1 { }
    class @class : C1 { }
}

namespace N2
{
    class Test
    {
        static int Main()
        {
            global::@namespace.@class c1 = new global::@namespace.@class();
            global::@namespace.C1 c2 = new global::@namespace.@C1();

            return 0;
        }
    }
}";
            ParseAndRoundTripping(text);
        }

        [WorkItem(911059, "DevDiv/Personal")]
        [Fact]
        public void RegressError4IdentifierStartWithAt2()
        {
            var text = @"public class A
        {
            public int @__namespace = 0;

            class @public // ok
            {
                private void M(int @int) { } // error
            }
            internal class @private // error
            {
            }
        }";

            ParseAndRoundTripping(text);
        }

        [WorkItem(911418, "DevDiv/Personal")]
        [Fact]
        public void RegressNegNoError4InvalidAttributeTarget()
        {
            var text = @"using System;
using System.Reflection;

public class foo 
{
    [method: method:A]
    public static void Main() 
    {
    }
}
[AttributeUsage(AttributeTargets.All,AllowMultiple=true)]
public class A : Attribute
{
}";
            ParseAndRoundTripping(text, -1);

            // Assert.Equal((int)ErrorCode.ERR_SyntaxError, tree.Errors()[0].Code); // CS1003
            // Assert.Equal((int)ErrorCode.ERR_InvalidMemberDecl, tree.Errors()[1].Code); // CS1519
        }

        [WorkItem(911477, "DevDiv/Personal")]
        [Fact]
        public void RegressWrongError4WarningExternOnCtor()
        {
            var text = @"using System;
public class C
{
    extern C();
    public static int Main()
    {
        return 1;
    }
}";

            // The warning WRN_ExternCtorNoImplementation is given in semantic analysis.
            ParseAndRoundTripping(text); // , 1);

            // Assert.Equal((int)ErrorCode.WRN_ExternCtorNoImplementation, tree.Errors()[0].Code); // W CS0824
        }

        [WorkItem(911488, "DevDiv/Personal")]
        [Fact]
        public void RegressError4MemberOnSimpleTypeAsKeyword()
        {
            var text = @"using System;
public class Test
{
    public void M()
    {
               var v = int.MaxValue; // error
        bool b = false;
        string s = ""true"";
        Boolean.TryParse(s, out b); // ok
        bool.TryParse(s, out b);  // error
    }
}";
            ParseAndRoundTripping(text);
        }

        [WorkItem(911505, "DevDiv/Personal")]
        [Fact]
        public void RegressWarning4EscapeCharInXmlDocAsText()
        {
            var text = @"using System;
/// <summary>
/// << A '&' B >>
/// </summary>
public class Test
{
    bool Find()
    {
        return false;
    }
}";
            ParseAndRoundTripping(text, -1);
        }

        [WorkItem(911518, "DevDiv/Personal")]
        [Fact]
        public void RegressError4AnonymousTypeWithTailingComma()
        {
            var text = @"using System;
public class Test
{
    public static void Main()
    {
        int x = 0;
        var v1 = new { X = x, };
    }
}";
            ParseAndRoundTripping(text);
        }

        [WorkItem(911521, "DevDiv/Personal")]
        [Fact]
        public void RegressError4QueryWithVarInLet()
        {
            var text = @"class Q
{
    static void Main()
    {
        var expr1 = new[] { 1, 2, 3, };
        var expr2 = new[] { 3, 4, 5, };
        var q = from i in (expr1) let j = expr2 select i;

        var qq = from x1 in new[] { 3, 9, 5, 5, 0, 8, 6, 8, 9, }
                  orderby x1 descending
                  let x47 = x1
                  select (x47) - (x1);

    }
}";
            ParseAndRoundTripping(text);
        }

        [WorkItem(911525, "DevDiv/Personal")]
        [Fact]
        public void RegressError4AttributeWithNamedParam()
        {
            var text = @"using System;
public class TestAttribute : Attribute
{
  public TestAttribute(int i = 0, int j = 1) { }
  public int i { get; set; }
}
[Test(123, j:-1)]
public class A 
{
  public static int Main()
  {
    return 0;
  }
}
";
            ParseAndRoundTripping(text);
        }

        [WorkItem(917285, "DevDiv/Personal")]
        [Fact]
        public void RegressAssertForLFCRSequence()
        {
            var text = "class Test\r\n{\n\r}\r\n";

            ParseAndRoundTripping(text);
        }

        [WorkItem(917771, "DevDiv/Personal")]
        [WorkItem(918947, "DevDiv/Personal")]
        [Fact]
        public void RegressNotCheckNullRef()
        {
            var text = @"public struct MyStruct
{
public delegate void TypeName<T>(ref T t, dynamic d);
public delegate Y @dynamic<X, Y>(X u, params dynamic[] ary);
public enum EM { };
}
";
            ParseAndRoundTripping(text);
        }

        [WorkItem(917771, "DevDiv/Personal")]
        [Fact]
        public void RegressNegNotCheckNullRef()
        {
            var text = @"class A
{
A a { 0, 1 };
}
";
            ParseAndRoundTripping(text, 2);
        }

        [WorkItem(922887, "DevDiv/Personal")]
        [Fact]
        public void RegressError4ExternOperator()
        {
            var text = @"public class A
{
    public static extern int operator !(A a);
    public static extern int operator +(A a, int n);
}
";
            ParseAndRoundTripping(text);
        }

        [Fact, WorkItem(536922, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/536922")]
        public void RegressError4QueryWithNullable()
        {
            var text = @"using System.Linq;
class A
{
    static void Main()
    {
        object[] p = { 1, 2, 3 };
        var q = from x in p
                where x is int?
                select x;
    }
}";
            ParseAndRoundTripping(text);
        }

        [Fact, WorkItem(537265, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/537265")]
        public void PartialMethodWithLanguageVersion2()
        {
            var text = @"partial class P
{
    partial void M();
}
";
            CSharpParseOptions options = new CSharpParseOptions(languageVersion: LanguageVersion.CSharp2);

            var itext = SourceText.From(text);
            var tree = SyntaxFactory.ParseSyntaxTree(itext, options, "");
            var newTest = tree.GetCompilationUnitRoot().ToFullString();
            Assert.Equal(text, newTest);
        }

        [WorkItem(527490, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/527490")]
        [Fact]
        public void VariableDeclarationAsTypeOfArgument()
        {
            string text = "typeof(System.String value)";
            var typeOfExpression = SyntaxFactory.ParseExpression(text, consumeFullText: true);
            Assert.Equal(text, typeOfExpression.ToFullString());
            Assert.NotEmpty(typeOfExpression.GetDiagnostics());

            typeOfExpression = SyntaxFactory.ParseExpression(text, consumeFullText: false);
            Assert.Equal("typeof(System.String ", typeOfExpression.ToFullString());
            Assert.NotEmpty(typeOfExpression.GetDiagnostics());
        }

        [WorkItem(540809, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/540809")]
        [Fact]
        public void IncompleteGlobalAlias()
        {
            var text = @"namespace N2
{
    [global:";

            ParseAndRoundTripping(text, errorCount: 3);
        }

        [WorkItem(542229, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/542229")]
        [Fact]
        public void MethodCallWithQueryArgInsideQueryExpr()
        {
            var text = @"
using System;
using System.Collections.Generic;
using System.Linq;

class Program
{
    public static bool Method1(IEnumerable<int> f1)
    {
        return true;
    }

    static void Main(string[] args)
    {
        var numbers = new int[] { 4, 5 };
        var f1 = from num1 in numbers where Method1(from num2 in numbers select num2) select num1;
    }
}";

            ParseAndRoundTripping(text, 0);
        }

        [WorkItem(542229, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/542229")]
        [Fact]
        public void MethodCallWithFromArgInsideQueryExpr()
        {
            var text = @"
using System;
using System.Collections.Generic;
using System.Linq;

class Program
{
    public static bool Method1(IEnumerable<int> f1)
    {
        return true;
    }

    static void Main(string[] args)
    {
        var numbers = new int[] { 4, 5 };
        var f1 = from num1 in numbers where Method1(from) select num1;
    }
}";

            ParseAndRoundTripping(text, -1);
        }

        [WorkItem(542229, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/542229")]
        [Fact]
        public void ArrayCreationWithQueryArgInsideQueryExpr()
        {
            var text = @"
using System;
using System.Collections.Generic;
using System.Linq;

class Program
{
    public static bool Method1(IEnumerable<int> f1)
    {
        return true;
    }

    static void Main(string[] args)
    {
        var numbers = new int[] { 4, 5 };
        var f1 = from num1 in new int[from num2 in numbers select num2] select num1; //not valid but this is only a parser test
    }
}";

            ParseAndRoundTripping(text, 0);
        }
    }
}
