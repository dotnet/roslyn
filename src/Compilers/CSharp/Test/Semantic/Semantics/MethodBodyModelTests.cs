// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.CSharp.Test.Utilities;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.CSharp.UnitTests
{
    public class MethodBodyModelTests : CSharpTestBase
    {
        [WorkItem(537881, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/537881")]
        [Fact]
        public void BindAliasWithSameNameClass()
        {
            var text = @"
using NSA = A;

namespace A
{
    class Foo { }
}

namespace B
{
    class Test
    {
        class NSA
        {
            public NSA(int Foo) { this.Foo = Foo; }
            int Foo;
        }

        static int Main()
        {
             NSA::Foo foo = new NSA::Foo(); // shouldn't error here
             if (foo == null) {} 
             return 0;
        }
    }
}
";
            var tree = Parse(text);
            var comp = CreateCompilationWithMscorlib(tree);
            Assert.Equal(0, comp.GetDeclarationDiagnostics().Count());

            comp.GetMethodBodyDiagnostics().Verify();
        }

        [Fact, WorkItem(537919, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/537919")]
        public void NullRefForNameAndOptionalMethod()
        {
            var text = @"
public class MyClass
{
    public object Method01(int x, int y = 0, int z = 1) { return null; }

    public static void Main()
    {
        MyClass c = new MyClass();
        c.Method01(999, z: 888);
    }
}
";
            var tree = Parse(text);
            var comp = CreateCompilationWithMscorlib(tree);
            Assert.Equal(0, comp.GetDeclarationDiagnostics().Count());

            comp.GetMethodBodyDiagnostics().Verify();
        }

        [WorkItem(538099, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/538099")]
        [Fact]
        public void ConversionsForLiterals()
        {
            var text = @"
class Program
{
    static void Main()
    {
        uint ui = 2;
        foo(ui + 2);
    }
    static void foo(uint x)
    {
    }
}";
            var tree = Parse(text);
            var comp = CreateCompilationWithMscorlib(tree);
            comp.VerifyDiagnostics();
        }

        [WorkItem(538100, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/538100")]
        [Fact]
        public void ConversionsFromVoid()
        {
            var text = @"
using System;
class Program
{
    static void Main()
    {
        object x = foo(); 
        if (x == null) {}
        Console.WriteLine(foo());
    }
    static void foo()
    {
    }
}";
            var tree = Parse(text);
            var comp = CreateCompilationWithMscorlib(tree);

            int[] count = new int[4];
            Dictionary<int, int> errors = new Dictionary<int, int>();
            foreach (var e in comp.GetDiagnostics())
            {
                count[(int)e.Severity]++;
                if (!errors.ContainsKey(e.Code)) errors[e.Code] = 0;
                errors[e.Code] += 1;
            }

            Assert.Equal(2, count[(int)DiagnosticSeverity.Error]);
            Assert.Equal(0, count[(int)DiagnosticSeverity.Warning]);
            Assert.Equal(0, count[(int)DiagnosticSeverity.Info]);
            Assert.Equal(1, errors[29]);
            // Assert.Equal(1, errors[1502]);
            Assert.Equal(1, errors[1503]);
        }

        [WorkItem(538110, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/538110")]
        [Fact]
        public void NullComparisons()
        {
            var text = @"
using System;
class Program
{
    static void Main()
    {
        bool x = (true == true); 
        object f = new object();
        x = (f == null); 
        Console.WriteLine(x);
        x = (null == f); 
        Console.WriteLine(x);
        x = (null == null); 
        Console.WriteLine(x);
    } 
}";
            var tree = Parse(text);
            var comp = CreateCompilationWithMscorlib(tree);
            comp.VerifyDiagnostics();
        }

        [WorkItem(538114, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/538114")]
        [Fact]
        public void OvldRslnWithExplicitIfaceImpl()
        {
            var text = @"
using System;
interface i1
{
    int bar(int x);
}
class c : i1
{
    public int bar(int x)
    {
        Console.WriteLine(""1"");
        return 0;
    }
    int i1.bar(int x)
    {
        Console.WriteLine(""2"");
        return 1;
    }
    public void test()
    {
        this.bar(1);
    }
}
class Program
{  
    static void Main()
    {
       c x = new c();
       x.test();
    }
}";
            var tree = Parse(text);
            var comp = CreateCompilationWithMscorlib(tree);
            comp.VerifyDiagnostics();
        }

        [WorkItem(3613, "DevDiv_Projects/Roslyn")]
        [Fact]
        public void OverloadResolutionCallThroughInterface()
        {
            var text = @"
using System;
public interface i1
{
    float bar(string x);
    int bar(int x);
}
class c : i1
{
    public int bar(int x)
    {
        Console.WriteLine(""1"");
        return 0;
    }
    public float bar(string x)
    {
        Console.WriteLine(""2"");
        return 0;
    }
}
class Program
{  
    static void Main()
    {
       c x = new c();
       i1 y = x;
       int i = 1;
       x.bar(i); // Works
       y.bar(i); // Fails to resolve
    }
}";
            var tree = Parse(text);
            var comp = CreateCompilationWithMscorlib(tree);
            comp.VerifyDiagnostics();
        }

        [WorkItem(538194, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/538194")]
        [Fact]
        public void ComparisonOperatorForRefTypes()
        {
            var text = @"
class Program
{
    static void Main()
    {
        object o = null; object o1 = null;
        if(o == o1)
        {
        }
        if(o != o1)
        {
        }
    }
}";
            var tree = Parse(text);
            var comp = CreateCompilationWithMscorlib(tree);
            comp.VerifyDiagnostics();
        }

        [WorkItem(538211, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/538211")]
        [Fact]
        public void BindCastConversionOnArithmeticOp()
        {
            var text = @"
public class MyClass
{
    public static int Main()
    {
        int i1 = (int)(0x80000000 % -1);

        if (i1 == 0)
        {
            return 0;
        }
        else
        {
            return 1;
        }
    }
}
";

            var tree = Parse(text);
            var comp = CreateCompilationWithMscorlib(tree);
            comp.VerifyDiagnostics();
        }

        [WorkItem(538212, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/538212")]
        [Fact]
        public void NegBindLHSCStyleArray()
        {
            // Expect CS0650 etc.
            var text = @"
using System;

class Test
{
    public static int Main()
    {
        int arr[] = new int[10];
        return 0;
    }
}
";

            // The native compiler produces four errors for this: that the [] is in the wrong place
            // is the correct error. It also produces three incorrect errors due to a faulty
            // error recovery heuristic; it treats the '=' as a statement and the "new int[10];" 
            // as a statement, and therefore gives three additional errors: that there is 
            // a missing semicolon before and after the '=', and that '=' is not a valid statement.
            // In Roslyn we now do error recovery better and treat the initialization clause
            // as an initializer. We therefore expect one parse error, not four.

            var tree = Parse(text);
            Assert.Equal(1, tree.GetDiagnostics().Count());
            Assert.Equal(650, tree.GetDiagnostics().First().Code);
        }

        // ImmutableArray NullRef exception by BoundTreeRewriter (No bug for now)
        [Fact]
        public void NegBindMultiDimArrayInit()
        {
            var text = @"
class A
{
    public static int Main()
    {
        int[,] arr = new int[3,2] {{1,2},,{4,5}};
        return 0;
    }
}
";

            CreateCompilationWithMscorlib(text).VerifyDiagnostics(
    // (6,42): error CS1525: Invalid expression term ','
    //         int[,] arr = new int[3,2] {{1,2},,{4,5}};
    Diagnostic(ErrorCode.ERR_InvalidExprTerm, ",").WithArguments(",")
                );
        }

        [Fact]
        public void MethodGroupToDelegate01()
        {
            var text = @"
delegate bool IntFunc(int x);
delegate bool LongFunc(long x);

public class Program
{
    static bool F(long l) { return false; }
    static bool F(int i) { return true; }
    public static void Main(string[] args)
    {
        IntFunc intFunc = F;
        LongFunc longFunc = F;
    }
}
";
            var tree = Parse(text);
            var comp = CreateCompilationWithMscorlib(tree);
            comp.VerifyDiagnostics();
        }

        [Fact]
        public void MethodGroupToDelegate02()
        {
            var text = @"
public class Program2
{
    delegate void MyAction<T>(T x);

    void Y(int x) { }

    void D(MyAction<int> o) { }
    void D(MyAction<long> o) { }

    void T()
    {
        D(Y);
    }
}
";
            var tree = Parse(text);
            var comp = CreateCompilationWithMscorlib(tree);
            comp.VerifyDiagnostics();
        }

        [Fact]
        public void MethodGroupToDelegate03()
        {
            var text = @"
public class Program1
{
    delegate void MyAction<T>(T x);

    void Y(long x) { }

    void D(MyAction<int> o) { }

    void T()
    {
        D(Y); // wrong parameter type
    }
}
";
            var tree = Parse(text);
            var comp = CreateCompilationWithMscorlib(tree);
            Assert.Equal(1, comp.GetDiagnostics().Count());
        }

        [Fact]
        public void MethodGroupToDelegate04()
        {
            var text = @"
public class Program1
{
    delegate void MyAction<T>(T x);

    void Y(long x) { }

    void D(MyAction<int> o) { }
    void D(MyAction<long> o) { }

    void T()
    {
        D(Y); // wrong parameter type
    }
}
";
            var tree = Parse(text);
            var comp = CreateCompilationWithMscorlib(tree);
            Assert.Equal(1, comp.GetDiagnostics().Count());
        }

        [Fact]
        public void MethodGroupToDelegate05()
        {
            var text = @"
public class Program1
{
    delegate void MyAction<T>(T x);

    void Y(long x) { }

    static void D(MyAction<long> o) { }

    static void T()
    {
        D(Y); // no 'this' in scope
    }
}
";
            var tree = Parse(text);
            var comp = CreateCompilationWithMscorlib(tree);
            Assert.Equal(1, comp.GetDiagnostics().Count());
        }

        [Fact]
        public void MethodGroupToDelegate06()
        {
            var text = @"
public class Program1
{
    delegate void MyAction<T>(T x);

    void Y(long x) { }

    static void D(MyAction<long> o) { }

    static void F()
    {
        D(Y); // no 'this' in scope
    }
}
";
            var tree = Parse(text);
            var comp = CreateCompilationWithMscorlib(tree);
            Assert.Equal(1, comp.GetDiagnostics().Count());
        }

        [Fact]
        public void MethodGroupToDelegate07()
        {
            var text = @"
public class Program1
{
    delegate void MyAction<T>(T x);

    static void Y(long x) { }

    static void D(MyAction<long> o) { }

    static void F(Program1 p)
    {
        D(p.Y); // cannot be accessed with an instance
    }
}
";
            var tree = Parse(text);
            var comp = CreateCompilationWithMscorlib(tree);
            Assert.Equal(1, comp.GetDiagnostics().Count());
        }

        [Fact]
        public void InvokeDelegate01()
        {
            var text = @"
public class Program
{
    delegate void MyAction<T>(T x);

    static void Y(long l) { }

    public static void Main(string[] args)
    {
        MyAction<long> o = Y;
        long l = 12;
        o(l);
    }
}
";
            var tree = Parse(text);
            var comp = CreateCompilationWithMscorlib(tree);
            comp.VerifyDiagnostics();
        }

        [WorkItem(538650, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/538650")]
        [Fact]
        public void PropertyAmbiguity()
        {
            var text = @"
interface IA
{
    int Foo { get; }
}

interface IB
{
    int Foo { get; }
}

interface IC : IA, IB { }

class C
{
    static void Main()
    {
        IC x = null;
        int y = x.Foo;
    }
}
";
            var tree = Parse(text);
            var comp = CreateCompilationWithMscorlib(tree);
            comp.VerifyDiagnostics(
                // (19,19): error CS0229: Ambiguity between 'IA.Foo' and 'IB.Foo'
                //         int y = x.Foo;
                Diagnostic(ErrorCode.ERR_AmbigMember, "Foo").WithArguments("IA.Foo", "IB.Foo"),
                // (18,12): warning CS0219: The variable 'x' is assigned but its value is never used
                //         IC x = null;
                Diagnostic(ErrorCode.WRN_UnreferencedVarAssg, "x").WithArguments("x")
            );
        }

        [WorkItem(538770, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/538770")]
        [Fact]
        public void DelegateMethodAmbiguity()
        {
            var text = @"
delegate void MyAction<T>(T x);

interface I1
{
    object Y { get; }
}

interface I2
{
    void Y(long l);
}

interface I3 : I1, I2 { }

public class Program : I3
{
    object I1.Y
    {
        get
        {
            return null;
        }
    }

    void I2.Y(long l) { }

    public static void Main(string[] args)
    {
        I3 p = new Program();
        MyAction<long> o = p.Y;
        long l = 12;
        o(l);
    }
}
";
            var tree = Parse(text);
            var comp = CreateCompilationWithMscorlib(tree);
            var diags = comp.GetDiagnostics();
            Assert.Equal(0, diags.Count(d => d.Severity == DiagnosticSeverity.Error));
            Assert.Equal(0, diags.Count(d => d.Severity == DiagnosticSeverity.Warning));
        }

        [WorkItem(538835, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/538835")]
        [Fact]
        public void LocalReferenceTypeConsts()
        {
            var text = @"
public class c1
{
}

public class Program
{
    static void Main()
    {
        const c1 C = null;
    }
}
";
            var tree = Parse(text);
            var comp = CreateCompilationWithMscorlib(tree);
            comp.VerifyDiagnostics(
                // (10,18): warning CS0219: The variable 'C' is assigned but its value is never used
                //         const c1 C = null;
                Diagnostic(ErrorCode.WRN_UnreferencedVarAssg, "C").WithArguments("C")
                );
        }

        [WorkItem(538617, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/538617")]
        [Fact]
        public void TypeParameterNotInvocable()
        {
            var text = @"
class B
{
    public static void T() { }
}

class A<T> : B
{
    static void Foo()
    {
        T();
    }
}
";
            var tree = Parse(text);
            var comp = CreateCompilationWithMscorlib(tree);
            comp.VerifyDiagnostics();
        }

        [WorkItem(539591, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/539591")]
        [Fact]
        public void ParenthesizedSetOnlyProperty()
        {
            var text = @"
namespace ParenthesizedExpression
{
    class A
    {
        int p;
        public int P
        {
            set
            {
                p = value;
            }
        }

        static void Main()
        {
            A a = new A();
            a.P = 10;
            (a.P) = 11; //devdiv bug 168519.
        }
    }
}
";
            var tree = Parse(text);
            var comp = CreateCompilationWithMscorlib(tree);
            comp.VerifyDiagnostics();
        }

        [WorkItem(5608, "DevDiv_Projects/Roslyn")]
        [Fact]
        public void TypeAliasNameIsSameAsProp()
        {
            var text = @"using System;
using Kind = MyKind;

enum MyKind
{
    Value,
}

class C
{
    public Kind Kind { get { return 0; } }

    public static void Main()
    {
        Console.WriteLine(Kind.Value);
    }
}
";
            var tree = Parse(text);
            var comp = CreateCompilationWithMscorlib(tree);
            comp.VerifyDiagnostics();
        }

        [WorkItem(539929, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/539929")]
        [Fact]
        public void TypeWithSameNameAsProp()
        {
            var text = @"using System;
enum Color
{
    Chartreuse,
}

class C
{
    public static Color Color { get { return 0; } }

    public static void Main()
    {
        Console.WriteLine(Color.Chartreuse);
    }
}
";
            var tree = Parse(text);
            var comp = CreateCompilationWithMscorlib(tree);
            comp.VerifyDiagnostics();
        }

        [WorkItem(541504, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/541504")]
        [Fact]
        public void TypeWithSameNameAsProp2()
        {
            var text = @"
enum ProtectionLevel
{
  Privacy = 0
}
class F
{
  ProtectionLevel p = ProtectionLevel.Privacy;
  ProtectionLevel ProtectionLevel { get { return 0; } }
}
";
            var tree = Parse(text);
            var comp = CreateCompilationWithMscorlib(tree);
            comp.VerifyDiagnostics(
                // (8,19): warning CS0414: The field 'F.p' is assigned but its value is never used
                //   ProtectionLevel p = ProtectionLevel.Privacy;
                Diagnostic(ErrorCode.WRN_UnreferencedFieldAssg, "p").WithArguments("F.p")
            );
        }

        [WorkItem(541505, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/541505")]
        [Fact]
        public void TypeWithSameNameAsProp3()
        {
            var text = @"
using System.ComponentModel;
enum ProtectionLevel
{
  Privacy = 0
}
class F
{
  [DefaultValue(ProtectionLevel.Privacy)]
  ProtectionLevel ProtectionLevel { get { return 0; } }
}
";
            var tree = Parse(text);
            var comp = CreateCompilationWithMscorlib(tree,
                references: new[] { TestReferences.NetFx.v4_0_30319.System });
            Assert.Equal(string.Empty, string.Join(Environment.NewLine, comp.GetDiagnostics()));
        }

        [WorkItem(539622, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/539622")]
        [Fact]
        public void AccessTypeThroughAliasNamespace()
        {
            var text = @"
namespace Conformance.Expressions
{
    using LevelInner = LevelOne.LevelTwo.LevelThree;
    public class LevelInner<A>
    {
        public class LevelThreeClass
        {
            public static int F1 = 1;
        }
    }

    public class Test
    {
        public static int Main()
        {
            return LevelInner.LevelThreeClass.F1; // should not error (Roslyn CS0117)
        }
    }
}
namespace LevelOne.LevelTwo.LevelThree
{
    public class LevelThreeClass
    {
        public static int F1 = 0;
    }
}
";
            var tree = Parse(text);
            var comp = CreateCompilationWithMscorlib(tree);
            comp.VerifyDiagnostics();
        }

        [WorkItem(540105, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/540105")]
        [Fact]
        public void WarningPassLocalAsRefParameter()
        {
            var text = @"
public class MyClass 
{
  public int MyMeth(ref int mbc)  {  return 1;  }
}
public class TestClass
{
public static int Main()
{
  int retval = 3;
  MyClass mc = new MyClass();

  int i = 1;
  retval = mc.MyMeth(ref i);
  return retval -1;
}
}
";
            var tree = Parse(text);
            var comp = CreateCompilationWithMscorlib(tree);
            comp.VerifyDiagnostics();
        }

        [WorkItem(540105, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/540105")]
        [Fact]
        public void WarningPassLocalAsOutParameter()
        {
            var text = @"
public class MyClass
{
    public int MyMeth(out int mbc) { mbc = 1; return mbc; }
}
public class TestClass
{
    public static int Main()
    {
        MyClass mc = new MyClass();
        int i;
        int retval = mc.MyMeth(out i);
        return retval - 1;
    }
}
";
            var tree = Parse(text);
            var comp = CreateCompilationWithMscorlib(tree);
            comp.VerifyDiagnostics();
        }

        [WorkItem(540270, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/540270")]
        [Fact]
        public void SimpleNameThroughUsingAlias()
        {
            var text = @"using System;
using Kind = MyKind;

enum MyKind
{
    Value,
}

class C
{
    public Kind Kind { get { return 0; } }

    public static void Main()
    {
        Console.WriteLine(Kind.Value);
    }
}
";
            var tree = Parse(text);
            var comp = CreateCompilationWithMscorlib(tree);
            comp.VerifyDiagnostics();
        }

        [WorkItem(544434, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/544434")]
        [Fact]
        public void MethodInvocationWithMultipleArgsToParams()
        {
            var text = @"
using System;

class Test
{
    static void Main()
    {
        int i = 9;
        Console.Write(""[{0}, {1}, {2}] = {3,2} "", i, i, i, i);
    }
}
";
            var tree = Parse(text);
            var comp = CreateCompilationWithMscorlib(tree);
            comp.VerifyDiagnostics();
        }

        [Fact, WorkItem(1118749, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1118749")]
        public void InstanceFieldOfEnclosingStruct()
        {
            var text = @"
struct Outer
{
    private int f1;
    void M() { f1 = f1 + 1; }
    public struct Inner
    {
        public Inner(int xyzzy)
        {
            var x = f1 - 1;
        }
    }
}";
            var tree = Parse(text);
            var comp = CreateCompilationWithMscorlib(tree);
            Assert.Equal(0, comp.GetDeclarationDiagnostics().Count());
            comp.GetMethodBodyDiagnostics().Verify(
                // (10,21): error CS0120: An object reference is required for the non-static field, method, or property 'Outer.f1'
                //             var x = f1 - 1;
                Diagnostic(ErrorCode.ERR_ObjectRequired, "f1").WithArguments("Outer.f1").WithLocation(10, 21)
                );
        }
    }
}
