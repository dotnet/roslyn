// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.CSharp.UnitTests.Emit;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.CSharp.UnitTests.CodeGen
{
    public class CompoundAssignmentForDelegate : EmitMetadataTestBase
    {
        // The method to removal or concatenation with 'optional' parameter
        [Fact]
        public void OptionalParaInCompAssignOperator()
        {
            var text =
@"
delegate void MyDelegate1(int x, float y);
class C
{
    public void DelegatedMethod(int x, float y = 3.0f) { System.Console.WriteLine(y); }
    static void Main(string[] args)
    {
        C mc = new C();
        MyDelegate1 md1 = null;
        md1 += mc.DelegatedMethod;
        md1(1, 5);
        md1 -= mc.DelegatedMethod;
    }
}
";
            string expectedIL = @"{
  // Code size       65 (0x41)
  .maxstack  4
  .locals init (C V_0) //mc
  IL_0000:  newobj     ""C..ctor()""
  IL_0005:  stloc.0
  IL_0006:  ldnull
  IL_0007:  ldloc.0
  IL_0008:  ldftn      ""void C.DelegatedMethod(int, float)""
  IL_000e:  newobj     ""MyDelegate1..ctor(object, System.IntPtr)""
  IL_0013:  call       ""System.Delegate System.Delegate.Combine(System.Delegate, System.Delegate)""
  IL_0018:  castclass  ""MyDelegate1""
  IL_001d:  dup
  IL_001e:  ldc.i4.1
  IL_001f:  ldc.r4     5
  IL_0024:  callvirt   ""void MyDelegate1.Invoke(int, float)""
  IL_0029:  ldloc.0
  IL_002a:  ldftn      ""void C.DelegatedMethod(int, float)""
  IL_0030:  newobj     ""MyDelegate1..ctor(object, System.IntPtr)""
  IL_0035:  call       ""System.Delegate System.Delegate.Remove(System.Delegate, System.Delegate)""
  IL_003a:  castclass  ""MyDelegate1""
  IL_003f:  pop
  IL_0040:  ret
}
";
            //var tree = SyntaxTree.ParseCompilationUnit(text);
            //var type = from item in ((CompilationUnitSyntax)tree.Root).Members where item as TypeDeclarationSyntax != null select item as TypeDeclarationSyntax;
            //var cla = type.First() as TypeDeclarationSyntax;
            //var method = from item in cla.Members where (MethodDeclarationSyntax)item != null select item as MethodDeclarationSyntax ;
            //var block = method.Last().Body;
            //var statement = block.Statements;
            CompileAndVerify(text, expectedOutput: "5").VerifyIL("C.Main", expectedIL);
        }

        // The object to removal or concatenation could be create a new instance of a method or a method name
        [Fact]
        public void ObjectOfCompAssignOperator()
        {
            var text =
@"
delegate void boo();
public class abc
{
    public void bar() { System.Console.WriteLine(""bar""); }
}

class C
{
    static void Main(string[] args)
    {
        abc p = new abc();
        boo goo = null;
        goo += p.bar;
        goo += new boo(p.bar);
        goo();
        goo -= p.bar;
        goo -= new boo(p.bar);
    }
}
";
            var expectedOutput = @"bar
bar";
            CompileAndVerify(text, expectedOutput: expectedOutput);
        }

        // The object to removal or concatenation could be null
        [Fact]
        public void ObjectOfCompAssignOperatorIsNull()
        {
            var text =
@"
delegate void boo();
class C
{
    static void Main(string[] args)
    {
        boo goo = null;
        goo += (boo)null;
        goo -= (boo)null;
        goo += null;
        goo -= null;
    }
}
";
            var expectedIL = @"{
  // Code size       47 (0x2f)
  .maxstack  2
  IL_0000:  ldnull
  IL_0001:  ldnull
  IL_0002:  call       ""System.Delegate System.Delegate.Combine(System.Delegate, System.Delegate)""
  IL_0007:  castclass  ""boo""
  IL_000c:  ldnull
  IL_000d:  call       ""System.Delegate System.Delegate.Remove(System.Delegate, System.Delegate)""
  IL_0012:  castclass  ""boo""
  IL_0017:  ldnull
  IL_0018:  call       ""System.Delegate System.Delegate.Combine(System.Delegate, System.Delegate)""
  IL_001d:  castclass  ""boo""
  IL_0022:  ldnull
  IL_0023:  call       ""System.Delegate System.Delegate.Remove(System.Delegate, System.Delegate)""
  IL_0028:  castclass  ""boo""
  IL_002d:  pop
  IL_002e:  ret
}";
            CompileAndVerify(text).VerifyIL("C.Main", expectedIL);
        }

        // The object to removal or concatenation could be an object of delegate
        [Fact]
        public void ObjectOfCompAssignOperatorIsObjectOfDelegate()
        {
            var text =
@"
using System;
delegate void boo();
public class abc
{
    public void bar() { System.Console.WriteLine(""bar""); }
    static public void far() { System.Console.WriteLine(""far""); }
}
class C
{
    static void Main(string[] args)
    {
        abc p = new abc();
        boo goo = null;
        boo goo1 = new boo(abc.far);
        goo += goo1; // Same type
        goo();
        goo -= goo1; // Same type
        boo[] arrfoo = { p.bar, abc.far };
        goo += (boo)Delegate.Combine(arrfoo);	// OK
        goo += (boo)Delegate.Combine(goo, goo1);  	// OK
        goo();
    }
}
";
            var expectedOutput = @"
far
bar
far
bar
far
far";
            CompileAndVerify(text, expectedOutput: expectedOutput);
        }

        [Fact]
        public void AnonymousMethodToRemovalOrConcatenation()
        {
            var text = @"
using System;
delegate void boo(int x);
class C
{
    static void Main()
    {
        boo goo = null;
        goo += delegate (int x)
        {
            System.Console.WriteLine(x);
        };
        goo(10);
        Delegate[] del = goo.GetInvocationList();
        goo -= (boo)del[0];
    }
}
";
            CompileAndVerify(text, expectedOutput: "10").VerifyIL("C.Main", @"
{
  // Code size       77 (0x4d)
  .maxstack  3
  .locals init (System.Delegate[] V_0) //del
  IL_0000:  ldnull
  IL_0001:  ldsfld     ""boo C.<>c.<>9__0_0""
  IL_0006:  dup
  IL_0007:  brtrue.s   IL_0020
  IL_0009:  pop
  IL_000a:  ldsfld     ""C.<>c C.<>c.<>9""
  IL_000f:  ldftn      ""void C.<>c.<Main>b__0_0(int)""
  IL_0015:  newobj     ""boo..ctor(object, System.IntPtr)""
  IL_001a:  dup
  IL_001b:  stsfld     ""boo C.<>c.<>9__0_0""
  IL_0020:  call       ""System.Delegate System.Delegate.Combine(System.Delegate, System.Delegate)""
  IL_0025:  castclass  ""boo""
  IL_002a:  dup
  IL_002b:  ldc.i4.s   10
  IL_002d:  callvirt   ""void boo.Invoke(int)""
  IL_0032:  dup
  IL_0033:  callvirt   ""System.Delegate[] System.Delegate.GetInvocationList()""
  IL_0038:  stloc.0
  IL_0039:  ldloc.0
  IL_003a:  ldc.i4.0
  IL_003b:  ldelem.ref
  IL_003c:  castclass  ""boo""
  IL_0041:  call       ""System.Delegate System.Delegate.Remove(System.Delegate, System.Delegate)""
  IL_0046:  castclass  ""boo""
  IL_004b:  pop
  IL_004c:  ret
}
");
        }

        [Fact]
        public void LambdaMethodToRemovalOrConcatenation()
        {
            var text = @"
using System;
delegate void boo(string x);
class C
{
    static void Main()
    {
        boo goo = null;
        goo += (x) =>
        {
            System.Console.WriteLine(x);
        };
        goo(""Hello"");
        Delegate[] del = goo.GetInvocationList();
        goo -= (boo)del[0];
    }
}
";
            CompileAndVerify(text, expectedOutput: "Hello").VerifyIL("C.Main()", @"
{
  // Code size       80 (0x50)
  .maxstack  3
  .locals init (System.Delegate[] V_0) //del
  IL_0000:  ldnull
  IL_0001:  ldsfld     ""boo C.<>c.<>9__0_0""
  IL_0006:  dup
  IL_0007:  brtrue.s   IL_0020
  IL_0009:  pop
  IL_000a:  ldsfld     ""C.<>c C.<>c.<>9""
  IL_000f:  ldftn      ""void C.<>c.<Main>b__0_0(string)""
  IL_0015:  newobj     ""boo..ctor(object, System.IntPtr)""
  IL_001a:  dup
  IL_001b:  stsfld     ""boo C.<>c.<>9__0_0""
  IL_0020:  call       ""System.Delegate System.Delegate.Combine(System.Delegate, System.Delegate)""
  IL_0025:  castclass  ""boo""
  IL_002a:  dup
  IL_002b:  ldstr      ""Hello""
  IL_0030:  callvirt   ""void boo.Invoke(string)""
  IL_0035:  dup
  IL_0036:  callvirt   ""System.Delegate[] System.Delegate.GetInvocationList()""
  IL_003b:  stloc.0
  IL_003c:  ldloc.0
  IL_003d:  ldc.i4.0
  IL_003e:  ldelem.ref
  IL_003f:  castclass  ""boo""
  IL_0044:  call       ""System.Delegate System.Delegate.Remove(System.Delegate, System.Delegate)""
  IL_0049:  castclass  ""boo""
  IL_004e:  pop
  IL_004f:  ret
}
");
        }

        // Mixed named method and Lambda expression to removal or concatenation
        [Fact]
        public void MixedNamedMethodAndLambdaToRemovalOrConcatenation()
        {
            var text =
@"
using System;
delegate void boo(int x);
class C
{
    static public void far(int x) { Console.WriteLine(""far:{0}"", x); }
    static void Main(string[] args)
    {
        boo goo = far;
        goo += (x) =>
            System.Console.WriteLine(""lambda:{0}"", x);
        goo(10);
        Delegate[] del = goo.GetInvocationList();
        goo -= (boo)del[0];
        goo(20);
    }
}
";
            var expectedOutPut = @"far:10
lambda:10
lambda:20";
            CompileAndVerify(text, expectedOutput: expectedOutPut);
        }

        // Mixed named method and Anonymous  method  to removal or concatenation
        [Fact]
        public void MixedNamedMethodAndAnonymousToRemovalOrConcatenation()
        {
            var text =
@"
using System;
delegate void boo(int x);
class C
{
    static public void far(int x) { Console.WriteLine(""far:{0}"", x); }
    static void Main(string[] args)
    {
        boo goo = far;
        goo += delegate(int x)
        {
            System.Console.WriteLine(""Anonymous:{0}"", x);
        };
        goo(10);
        Delegate[] del = goo.GetInvocationList();
        goo -= (boo)del[0];
        goo(20);
    }
}
";
            var expectedOutPut = @"far:10
Anonymous:10
Anonymous:20";
            CompileAndVerify(text, expectedOutput: expectedOutPut);
        }

        // Mixed Lambda expression and Anonymous  method  to removal or concatenation
        [Fact]
        public void MixedAnonymousAndLambdaToRemovalOrConcatenation()
        {
            var text =
@"
using System;
delegate void boo(int x);
class C
{
    static public void far(int x) { Console.WriteLine(""far:{0}"", x); }
    static void Main(string[] args)
    {
        boo goo = far;
        goo += x =>
        {
            System.Console.WriteLine(""Lambda:{0}"", x);
        };
        goo += delegate(int x)
        {
            System.Console.WriteLine(""Anonymous:{0}"", x);
        };
        goo(10);
        Delegate[] del = goo.GetInvocationList();
        goo -= (boo)del[0];
        goo(20);
    }
}
";
            var expectedOutPut = @"far:10
Lambda:10
Anonymous:10
Lambda:20
Anonymous:20";
            CompileAndVerify(text, expectedOutput: expectedOutPut);
        }

        // To removal or concatenation same method multi- times
        [Fact]
        public void RemoveSameMethodMultiTime()
        {
            var text =
@"
using System;
delegate void D(ref int x);
class C
{
    public static void M1(ref int i)
    {
        Console.WriteLine(""M1: "" + i);
        i = 1;
    }
    public static void M2(ref int i)
    {
        Console.WriteLine(""M2: "" + i);
        i = 2;
    }

    static void Main(string[] args)
    {
        int i = 0;
        D cd1 = new D(M1); // M1
        D cd2 = cd1;
        cd1 += M2; // M1,M2 
        cd1 += M1; // M1,M2,M1 
        cd1(ref i);
        cd1 -= cd2;// remove last M1
        cd1(ref i);
        cd1 -= M1; // remove first M1
        cd1(ref i);
    }
}
";
            var expectedOutPut = @"M1: 0
M2: 1
M1: 2
M1: 1
M2: 1
M2: 2
";
            CompileAndVerify(text, expectedOutput: expectedOutPut);
        }

        // Remove Non existed method
        [Fact]
        public void RemoveNotExistMethod()
        {
            var text =
@"
using System;
delegate void D(ref int x);
class C
{
    public static void M1(ref int i)
    {
        Console.WriteLine(""M1: "" + i);
        i = 1;
    }
    public static void M2(ref int i)
    {
        Console.WriteLine(""M2: "" + i);
        i = 2;
    }

    static void Main(string[] args)
    {
        int i = 0;
        D cd1 = new D(M1); // M1
        cd1 -= M2;	// M1
        cd1 -= M2;	// M1
        cd1(ref i);
    }
}
";
            var expectedOutPut = @"M1: 0";
            CompileAndVerify(text, expectedOutput: expectedOutPut);
        }

        // Removal and concatenation works on both static and instance methods 
        [Fact]
        public void RemoveBothStaticAndInstanceMethod()
        {
            var text =
@"
delegate void boo();
public class abc
{
    public void bar() { System.Console.WriteLine(""bar""); }
    static public void far() { System.Console.WriteLine(""far""); }
}

class C
{
    static void Main(string[] args)
    {
        abc p = new abc();
        boo goo = new boo(p.bar);
        goo();
        goo -= p.bar;
        goo = new boo(abc.far);
        goo();
        goo -= abc.far;
        goo += p.bar;
        goo += abc.far;
        goo();
    }
}
";
            var expectedOutPut = @"bar
far
bar
far";
            CompileAndVerify(text, expectedOutput: expectedOutPut);
        }

        // Removal or concatenation for the delegate that is member of classes
        [Fact]
        public void RemoveDelegateIsMemberOfClass()
        {
            var text =
@"
public delegate void boo();
public class abc
{
    public void bar() { System.Console.WriteLine(""bar""); }
    static public void far() { System.Console.WriteLine(""far""); }
    public boo goo = null;
}

class C
{
    static void Main(string[] args)
    {
        abc p = new abc();
        p.goo = null;
        p.goo += abc.far;
        p.goo += p.bar;
        p.goo();
        p.goo -= abc.far;
        p.goo -= p.bar;
    }
}
";
            var expectedOutPut = @"far
bar
";
            CompileAndVerify(text, expectedOutput: expectedOutPut);
        }

        // Removal or concatenation for the delegate works on ternary operator
        [Fact]
        public void CompAssignWorksOnTernaryOperator()
        {
            var text =
@"
delegate void boo();
public class abc
{
    public void bar() { System.Console.WriteLine(""bar""); }
    static public void far() { System.Console.WriteLine(""far""); }
}

class C
{
    static void Main(string[] args)
    {
        abc p = new abc();
        boo goo = null;
        goo += loo() ? new boo(p.bar) : new boo(abc.far);
        goo();
        goo -= loo() ? new boo(p.bar) : new boo(abc.far);
        boo left = null;
        boo right = null;
        goo = !loo() ? left += new boo(abc.far) : right += new boo(p.bar);
        goo();
        goo = !loo() ? left -= new boo(abc.far) : right -= new boo(p.bar);
    }

    private static bool loo()
    {
        return true;
    }
}
";
            var expectedOutPut = @"bar
bar
";
            CompileAndVerify(text, expectedOutput: expectedOutPut);
        }

        // Removal or concatenation for the delegate that with 9 args
        [Fact]
        public void DelegateWithNineArgs()
        {
            var text =
@"
delegate void boo(out int i, double d, ref float f, string s, char c, decimal dc, C client, byte b, short sh);

class C
{
    public static void Hello(out int i, double d, ref float f, string s, char c, decimal dc, C client, byte b, short sh)
    {
        i = 1;
        System.Console.WriteLine(""Hello"");
    }
    static void Main(string[] args)
    {
        boo goo = null;
        goo += new boo(C.Hello);
        int i = 1;
        float ff = 0;
        goo(out i, 5.5, ref ff, ""a string"", 'C', 0.555m, new C(), 3, 16);
    }
}
";
            var expectedOutPut = @"Hello
";
            CompileAndVerify(text, expectedOutput: expectedOutPut);
        }

        // Removal or concatenation for the delegate that is virtual struct methods
        [WorkItem(539908, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/539908")]
        [Fact]
        public void DelegateWithStructMethods()
        {
            var text =
@"
delegate int boo();

interface I
{
    int bar();
}
public struct abc : I
{
    public int bar() { System.Console.WriteLine(""bar""); return 0x01; }
}
class C
{
    static void Main(string[] args)
    {
        abc p = new abc();
        boo goo = null;
        goo += new boo(p.bar);
        goo();
    }
}
";
            var expectedOutPut = @"bar
";

            var expectedIL = @"
{
  // Code size       44 (0x2c)
  .maxstack  3
  .locals init (abc V_0) //p
  IL_0000:  ldloca.s   V_0
  IL_0002:  initobj    ""abc""
  IL_0008:  ldnull
  IL_0009:  ldloc.0
  IL_000a:  box        ""abc""
  IL_000f:  dup
  IL_0010:  ldvirtftn  ""int abc.bar()""
  IL_0016:  newobj     ""boo..ctor(object, System.IntPtr)""
  IL_001b:  call       ""System.Delegate System.Delegate.Combine(System.Delegate, System.Delegate)""
  IL_0020:  castclass  ""boo""
  IL_0025:  callvirt   ""int boo.Invoke()""
  IL_002a:  pop
  IL_002b:  ret
}
";
            CompileAndVerify(text, expectedOutput: expectedOutPut).VerifyIL("C.Main", expectedIL);
        }

        // Removal or concatenation for the delegate that the method is in base class
        [Fact]
        public void AddMethodThatInBaseClass()
        {
            var text =
@"
delegate double MyDelegate(int integerPortion, float fraction);

public class BaseClass
{
    public delegate void MyDelegate();

    public void DelegatedMethod()
    {
        System.Console.WriteLine(""Base"");
    }
}
public class DerivedClass : BaseClass
{
    new public delegate void MyDelegate();

    public new void DelegatedMethod()
    {
        System.Console.WriteLine(""Derived"");
    }
    static void Main(string[] args)
    {
        DerivedClass derived = new DerivedClass();
        BaseClass baseCls = new BaseClass();
        MyDelegate derivedDel = new MyDelegate(derived.DelegatedMethod);
        derivedDel += baseCls.DelegatedMethod;
        derivedDel();
        BaseClass.MyDelegate BaseDel = new BaseClass.MyDelegate(((BaseClass)derived).DelegatedMethod);
        BaseDel += derived.DelegatedMethod;
        BaseDel();
    }
}
";
            var expectedOutPut = @"Derived
Base
Base
Derived
";

            CompileAndVerify(text, expectedOutput: expectedOutPut);
        }

        // delegate-in-a-generic-class (C<t>.goo(…)) += methodgroup-in-a-generic-class (C<T>.bar(…))
        [Fact]
        public void CompAssignOperatorForGenericClass()
        {
            var text =
@"
delegate void boo(short x);
class C<T>
{
    public void bar(short x) { System.Console.WriteLine(""bar""); }
    public static void far(T x) { System.Console.WriteLine(""far""); }
    public static void par<U>(U x) { System.Console.WriteLine(""par""); }
    public static boo goo = null;
}
class D
{
    static void Main(string[] args)
    {
        C<long> p = new C<long>();
        C<long>.goo += p.bar;
        C<short>.goo += C<short>.far;
        C<long>.goo += C<long>.par<short>;
        C<long>.goo(short.MaxValue);
        C<short>.goo(short.MaxValue);
    }
}
";
            var expectedOutPut = @"bar
par
far
";
            CompileAndVerify(text, expectedOutput: expectedOutPut);
        }

        // Compound assignment for the method with derived return type
        [Fact]
        public void CompAssignOperatorForInherit01()
        {
            var text =
@"
delegate BaseClass MyBaseDelegate(BaseClass x);
delegate DerivedClass MyDerivedDelegate(DerivedClass x);
public class BaseClass
{
    public static BaseClass DelegatedMethod(BaseClass x)
    {
        System.Console.WriteLine(""Base"");
        return x;
    }
}
public class DerivedClass : BaseClass
{
    public static DerivedClass DelegatedMethod(DerivedClass x)
    {
        System.Console.WriteLine(""Derived"");
        return x;
    }
    static void Main(string[] args)
    {
        MyBaseDelegate goo = null;
        goo += BaseClass.DelegatedMethod;
        goo += DerivedClass.DelegatedMethod;
        goo(new BaseClass());
        goo(new DerivedClass());
        MyDerivedDelegate goo1 = null;
        //goo1 += BaseClass.DelegatedMethod;
        goo1 += DerivedClass.DelegatedMethod;
        goo1(new DerivedClass());
    }
}
";
            var expectedOutPut = @"Base
Base
Base
Base
Derived
";
            CompileAndVerify(text, expectedOutput: expectedOutPut);
        }

        // Compound assignment for the method with derived return type
        [Fact]
        public void CompAssignOperatorForInherit02()
        {
            var text =
@"
delegate T MyDelegate<T>(T x);
public class BaseClass
{
    public static BaseClass DelegatedMethod(BaseClass x)
    {
        System.Console.WriteLine(""Base1"");
        return x;
    }
    public static DerivedClass DelegatedMethod(DerivedClass x)
    {
        System.Console.WriteLine(""Base2"");
        return x;
    }
}
public class DerivedClass : BaseClass
{
    public static new DerivedClass DelegatedMethod(DerivedClass x)
    {
        System.Console.WriteLine(""Derived1"");
        return x;
    }
    public static new BaseClass DelegatedMethod(BaseClass x)
    {
        System.Console.WriteLine(""Derived2"");
        return x;
    }
    static void Main(string[] args)
    {
        MyDelegate<BaseClass> goo = null;
        goo += BaseClass.DelegatedMethod;
        goo += DerivedClass.DelegatedMethod;
        goo(new BaseClass());
        goo(new DerivedClass());
        MyDelegate<DerivedClass> goo1 = null;
        goo1 += BaseClass.DelegatedMethod;
        goo1 += DerivedClass.DelegatedMethod;
        //goo1(new BaseClass());
        goo1(new DerivedClass());
    }
}
";
            var expectedOutPut = @"Base1
Derived2
Base1
Derived2
Base2
Derived1
";
            CompileAndVerify(text, expectedOutput: expectedOutPut);
        }

        // Compound assignment for the method with derived return type
        [WorkItem(539927, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/539927")]
        [Fact]
        public void CompAssignOperatorForInherit03()
        {
            var text =
@"
delegate T MyDelegate<T>(T x);
public class BaseClass
{
    public static T DelegatedMethod<T>(T x)
    {
        System.Console.WriteLine(""Base"");
        return x;
    }
    public static double DelegatedMethod(double x)
    {
        System.Console.WriteLine(""double"");
        return x;
    }
}
public class DerivedClass : BaseClass
{
    public static new T DelegatedMethod<T>(T x)
    {
        System.Console.WriteLine(""Derived"");
        return x;
    }

    static void Main(string[] args)
    {
        MyDelegate<BaseClass> goo = null;
        goo += BaseClass.DelegatedMethod;
        goo += DerivedClass.DelegatedMethod;
        goo(new BaseClass());
        goo(new DerivedClass());
        MyDelegate<DerivedClass> goo1 = null;
        goo1 += BaseClass.DelegatedMethod;
        goo1 += DerivedClass.DelegatedMethod;
        //goo1(new BaseClass());
        goo1(new DerivedClass());
        MyDelegate<double> goo2 = null;
        goo2 += BaseClass.DelegatedMethod<double>;
        goo2 += BaseClass.DelegatedMethod;
        goo2 += DerivedClass.DelegatedMethod;
        goo2(2);
    }
}
";
            var expectedOutPut = @"Base
Derived
Base
Derived
Base
Derived
Base
double
Derived
";
            CompileAndVerify(text, expectedOutput: expectedOutPut);
        }

        // Compound assignment for the method with derived return type
        [WorkItem(539927, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/539927")]
        [Fact]
        public void CompAssignOperatorForInherit04()
        {
            var text =
@"

delegate double MyDelegate(double x);
public class BaseClass
{
    public static T DelegatedMethod<T>(T x)
    {
        System.Console.WriteLine(""Base"");
        return x;
    }
    public static double DelegatedMethod(double x)
    {
        System.Console.WriteLine(""double"");
        return x;
    }
}
public class DerivedClass : BaseClass
{
    public static new T DelegatedMethod<T>(T x)
    {
        System.Console.WriteLine(""Derived"");
        return x;
    }

    static void Main(string[] args)
    {
        MyDelegate goo = null;
        goo += BaseClass.DelegatedMethod<double>;
        goo += BaseClass.DelegatedMethod;
        goo += DerivedClass.DelegatedMethod;
        MyDelegate goo1 = null;
        goo1 += goo;
        goo += goo1;
        goo(1);
        goo1(1);
    }
}
";
            var expectedOutPut = @"Base
double
Derived
Base
double
Derived
Base
double
Derived
";
            CompileAndVerify(text, expectedOutput: expectedOutPut);
        }
    }
}
