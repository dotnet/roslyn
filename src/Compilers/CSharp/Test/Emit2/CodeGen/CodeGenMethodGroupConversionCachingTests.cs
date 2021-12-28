// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Diagnostics;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.CSharp.UnitTests.CodeGen;

public class CodeGenMethodGroupConversionCachingTests : CSharpTestBase
{
    const string PASS = "PASS";

    [Fact]
    public void Not_DelegateCreations_Static()
    {
        var source = @"
using System;
delegate void D();
class C
{
    public static void Main(string[] args)
    {
        Invoke(new D(Target), new D(Target));
    }

    static void Target() { Console.WriteLine(""FAIL""); }
    static void Invoke(D x, D y) { Console.Write(Object.ReferenceEquals(x, y) ? ""FAIL"" : ""PASS""); }
}";
        static void containerValidator(ModuleSymbol module)
            => Assert.Null(module.GlobalNamespace.GetMember<NamedTypeSymbol>("<>O"));

        CompileAndVerify(source, symbolValidator: containerValidator, expectedOutput: PASS);
    }

    [Fact]
    public void Not_DelegateCreations_Instance()
    {
        var source = @"
using System;
delegate void D();
class C
{
    public static void Main(string[] args)
    {
        var c = new C();
        c.Invoke(new D(c.Target), new D(c.Target));
    }

    void Target() { Console.WriteLine(""FAIL""); }
    void Invoke(D x, D y) { Console.Write(Object.ReferenceEquals(x, y) ? ""FAIL"" : ""PASS""); }
}";
        static void containerValidator(ModuleSymbol module)
        {
            Assert.Null(module.GlobalNamespace.GetMember<NamedTypeSymbol>("<>O"));
        }
        CompileAndVerify(source, symbolValidator: containerValidator, expectedOutput: PASS);
    }

    [Fact]
    public void Not_DelegateCreations_InstanceExtensionMethod()
    {
        var source = @"
using System;
delegate void D();
class C
{
    public static void Main(string[] args)
    {
        var c = new C();
        c.Invoke(new D(c.Target), new D(c.Target));
    }

    void Invoke(D x, D y) { Console.Write(Object.ReferenceEquals(x, y) ? ""FAIL"" : ""PASS""); }
}
static class E
{
    public static void Target(this C that) { Console.WriteLine(""FAIL""); }
}
";
        static void containerValidator(ModuleSymbol module)
        {
            Assert.Null(module.GlobalNamespace.GetMember<NamedTypeSymbol>("<>O"));
        }
        CompileAndVerify(source, symbolValidator: containerValidator, expectedOutput: PASS);
    }

    [Fact]
    public void Not_DelegateCreations_StaticExtensionMethod()
    {
        var source = @"
using System;
delegate void D(C arg);
class C
{
    public static void Main(string[] args)
    {
        var c = new C();
        c.Invoke(new D(E.Target), new D(E.Target));
    }

    void Invoke(D x, D y) { Console.Write(Object.ReferenceEquals(x, y) ? ""FAIL"" : ""PASS""); }
}
static class E
{
    public static void Target(this C that) { Console.WriteLine(""FAIL""); }
}
";
        static void containerValidator(ModuleSymbol module)
        {
            Assert.Null(module.GlobalNamespace.GetMember<NamedTypeSymbol>("<>O"));
        }
        CompileAndVerify(source, symbolValidator: containerValidator, expectedOutput: PASS);
    }

    [Fact]
    public void Not_InExpressionLamba0()
    {
        var source = @"
using System;
using System.Linq.Expressions;
class C
{
    public static void Main(string[] args)
    {
        Expression<Func<int, Func<int, int>>> e = x => Target;
    }

    static int Target(int x) => 0;
}
";
        static void containerValidator(ModuleSymbol module)
        {
            Assert.Null(module.GlobalNamespace.GetMember<NamedTypeSymbol>("<>O"));
        }
        CompileAndVerify(source, symbolValidator: containerValidator);
    }

    [Fact]
    public void Not_InExpressionLamba1()
    {
        var source = @"
using System;
using System.Linq.Expressions;
class C
{
    public static void Main(string[] args)
    {
        Func<int, Expression<Func<int, Func<int, int>>>> f = x => y => Target;
    }

    static int Target(int x) => 0;
}
";
        static void containerValidator(ModuleSymbol module)
        {
            Assert.Null(module.GlobalNamespace.GetMember<NamedTypeSymbol>("<>O"));
        }
        CompileAndVerify(source, symbolValidator: containerValidator);
    }

    [Fact]
    public void Not_InStaticConstructor0()
    {
        var source = @"
using System;
class C
{
    static readonly Action ManualCache = Target;
    static void Target() { }
}
";
        static void containerValidator(ModuleSymbol module)
        {
            Assert.Null(module.GlobalNamespace.GetMember<NamedTypeSymbol>("<>O"));
        }
        CompileAndVerify(source, symbolValidator: containerValidator);
    }

    [Fact]
    public void Not_InStaticConstructor1()
    {
        var source = @"
using System;
struct C
{
    static readonly Action ManualCache;
    static void Target() { }

    static C()
    {
        ManualCache = Target;
    }
}
";
        static void containerValidator(ModuleSymbol module)
        {
            Assert.Null(module.GlobalNamespace.GetMember<NamedTypeSymbol>("<>O"));
        }
        CompileAndVerify(source, symbolValidator: containerValidator);
    }

    [Fact]
    public void CacheExplicitConversions_TypeScoped_CouldBeModuleScoped0()
    {
        var source = @"
using System;
class C
{
    public static void Main(string[] args)
    {
        Test((Action)Target);
    }

    static void Test(Action action)
    {
        action();
    }

    static void Target() { Console.WriteLine(""PASS""); }
}
";
        var verifier = CompileAndVerify(source, expectedOutput: PASS);
        verifier.VerifyIL("C.Main", @"
{
  // Code size       33 (0x21)
  .maxstack  2
  IL_0000:  ldsfld     ""System.Action C.<>O.<0>__Target""
  IL_0005:  dup
  IL_0006:  brtrue.s   IL_001b
  IL_0008:  pop
  IL_0009:  ldnull
  IL_000a:  ldftn      ""void C.Target()""
  IL_0010:  newobj     ""System.Action..ctor(object, System.IntPtr)""
  IL_0015:  dup
  IL_0016:  stsfld     ""System.Action C.<>O.<0>__Target""
  IL_001b:  call       ""void C.Test(System.Action)""
  IL_0020:  ret
}
");
    }

    [Fact]
    public void CacheExplicitConversions_TypeScoped_CouldBeModuleScoped1()
    {
        var source = @"
using System;
class C
{
    public static void Main(string[] args)
    {
        var d = new D<int>();
        d.Test()();
    }

    public static void Target() { Console.WriteLine(""PASS""); }
}
class D<T>
{
    public Action Test()
    {
        return (Action)C.Target;
    }
}
";
        var verifier = CompileAndVerify(source, expectedOutput: PASS);
        verifier.VerifyIL("D<T>.Test", @"
{
  // Code size       28 (0x1c)
  .maxstack  2
  IL_0000:  ldsfld     ""System.Action D<T>.<>O.<0>__Target""
  IL_0005:  dup
  IL_0006:  brtrue.s   IL_001b
  IL_0008:  pop
  IL_0009:  ldnull
  IL_000a:  ldftn      ""void C.Target()""
  IL_0010:  newobj     ""System.Action..ctor(object, System.IntPtr)""
  IL_0015:  dup
  IL_0016:  stsfld     ""System.Action D<T>.<>O.<0>__Target""
  IL_001b:  ret
}
");
    }

    [Fact]
    public void CacheExplicitConversions_TypeScoped_CouldBeModuleScoped2()
    {
        var source = @"
using System;
class C
{
    public static void Main(string[] args)
    {
        (new D<int>()).Test();
    }
}
class D<T>
{
    public void Test()
    {
        var t = (Action)E<int>.Target;
        t();
    }
}
class E<V>
{
    public static void Target() { Console.WriteLine(""PASS""); }
}
";
        var verifier = CompileAndVerify(source, expectedOutput: PASS);
        verifier.VerifyIL("D<T>.Test", @"
{
  // Code size       33 (0x21)
  .maxstack  2
  IL_0000:  ldsfld     ""System.Action D<T>.<>O.<0>__Target""
  IL_0005:  dup
  IL_0006:  brtrue.s   IL_001b
  IL_0008:  pop
  IL_0009:  ldnull
  IL_000a:  ldftn      ""void E<int>.Target()""
  IL_0010:  newobj     ""System.Action..ctor(object, System.IntPtr)""
  IL_0015:  dup
  IL_0016:  stsfld     ""System.Action D<T>.<>O.<0>__Target""
  IL_001b:  callvirt   ""void System.Action.Invoke()""
  IL_0020:  ret
}
");
    }

    [Fact]
    public void CacheExplicitConversions_TypeScoped_CouldBeModuleScoped3()
    {
        var source = @"
using System;
class C
{
    public static void Main(string[] args)
    {
        (new D<int>()).Test();
    }
}
class D<T>
{
    public void Test()
    {
        var t = (Action<int>)E<int>.Target;
        t(0);
    }
}
class E<V>
{
    public static void Target(V v) { Console.WriteLine(""PASS""); }
}
";
        var verifier = CompileAndVerify(source, expectedOutput: PASS);
        verifier.VerifyIL("D<T>.Test", @"
{
  // Code size       34 (0x22)
  .maxstack  2
  IL_0000:  ldsfld     ""System.Action<int> D<T>.<>O.<0>__Target""
  IL_0005:  dup
  IL_0006:  brtrue.s   IL_001b
  IL_0008:  pop
  IL_0009:  ldnull
  IL_000a:  ldftn      ""void E<int>.Target(int)""
  IL_0010:  newobj     ""System.Action<int>..ctor(object, System.IntPtr)""
  IL_0015:  dup
  IL_0016:  stsfld     ""System.Action<int> D<T>.<>O.<0>__Target""
  IL_001b:  ldc.i4.0
  IL_001c:  callvirt   ""void System.Action<int>.Invoke(int)""
  IL_0021:  ret
}
");
    }

    [Fact]
    public void CacheExplicitConversions_TypeScoped_CouldBeModuleScoped4()
    {
        var source = @"
using System;
class C
{
    public static void Main(string[] args)
    {
        (new D<int>()).Test();
    }
}
class D<T>
{
    public void Test()
    {
        var t = (Action<int>)E<int>.Target<double>;
        t(0);
    }
}
class E<V>
{
    public static void Target<K>(V v) { Console.WriteLine(""PASS""); }
}
";
        var verifier = CompileAndVerify(source, expectedOutput: PASS);
        verifier.VerifyIL("D<T>.Test", @"
{
  // Code size       34 (0x22)
  .maxstack  2
  IL_0000:  ldsfld     ""System.Action<int> D<T>.<>O.<0>__Target""
  IL_0005:  dup
  IL_0006:  brtrue.s   IL_001b
  IL_0008:  pop
  IL_0009:  ldnull
  IL_000a:  ldftn      ""void E<int>.Target<double>(int)""
  IL_0010:  newobj     ""System.Action<int>..ctor(object, System.IntPtr)""
  IL_0015:  dup
  IL_0016:  stsfld     ""System.Action<int> D<T>.<>O.<0>__Target""
  IL_001b:  ldc.i4.0
  IL_001c:  callvirt   ""void System.Action<int>.Invoke(int)""
  IL_0021:  ret
}
");
    }

    [Fact]
    public void CacheExplicitConversions_TypeScoped_CouldBeModuleScoped5()
    {
        var source = @"
using System;
class C
{
    public static void Main(string[] args)
    {
        (new D<int>()).Test();
    }
}
class D<T>
{
    public void Test()
    {
        var t = (Func<int, int>)E<int>.Target<double>;
        t(0);
    }
}
class E<V>
{
    public static V Target<K>(V v) { Console.WriteLine(""PASS""); return default(V); }
}
";
        var verifier = CompileAndVerify(source, expectedOutput: PASS);
        verifier.VerifyIL("D<T>.Test", @"
{
  // Code size       35 (0x23)
  .maxstack  2
  IL_0000:  ldsfld     ""System.Func<int, int> D<T>.<>O.<0>__Target""
  IL_0005:  dup
  IL_0006:  brtrue.s   IL_001b
  IL_0008:  pop
  IL_0009:  ldnull
  IL_000a:  ldftn      ""int E<int>.Target<double>(int)""
  IL_0010:  newobj     ""System.Func<int, int>..ctor(object, System.IntPtr)""
  IL_0015:  dup
  IL_0016:  stsfld     ""System.Func<int, int> D<T>.<>O.<0>__Target""
  IL_001b:  ldc.i4.0
  IL_001c:  callvirt   ""int System.Func<int, int>.Invoke(int)""
  IL_0021:  pop
  IL_0022:  ret
}
");
    }

    [Fact]
    public void CacheExplicitConversions_TypeScoped0()
    {
        var source = @"
using System;
class C
{
    public static void Main(string[] args)
    {
        (new D<int>()).Test();
    }
}
class D<T>
{
    public void Test()
    {
        var t = (Action)Target;
        t();
    }

    static void Target() { Console.WriteLine(""PASS""); }
}
";
        var verifier = CompileAndVerify(source, expectedOutput: PASS);
        verifier.VerifyIL("D<T>.Test", @"
{
  // Code size       33 (0x21)
  .maxstack  2
  IL_0000:  ldsfld     ""System.Action D<T>.<>O.<0>__Target""
  IL_0005:  dup
  IL_0006:  brtrue.s   IL_001b
  IL_0008:  pop
  IL_0009:  ldnull
  IL_000a:  ldftn      ""void D<T>.Target()""
  IL_0010:  newobj     ""System.Action..ctor(object, System.IntPtr)""
  IL_0015:  dup
  IL_0016:  stsfld     ""System.Action D<T>.<>O.<0>__Target""
  IL_001b:  callvirt   ""void System.Action.Invoke()""
  IL_0020:  ret
}
");
    }

    [Fact]
    public void CacheExplicitConversions_TypeScoped1()
    {
        var source = @"
using System;
class C
{
    public static void Main(string[] args)
    {
        (new D<int>()).Test();
    }
}
class D<T>
{
    public void Test()
    {
        var t = (Action)Target<int>;
        t();
    }

    static void Target<K>() { Console.WriteLine(""PASS""); }
}
";
        var verifier = CompileAndVerify(source, expectedOutput: PASS);
        verifier.VerifyIL("D<T>.Test", @"
{
  // Code size       33 (0x21)
  .maxstack  2
  IL_0000:  ldsfld     ""System.Action D<T>.<>O.<0>__Target""
  IL_0005:  dup
  IL_0006:  brtrue.s   IL_001b
  IL_0008:  pop
  IL_0009:  ldnull
  IL_000a:  ldftn      ""void D<T>.Target<int>()""
  IL_0010:  newobj     ""System.Action..ctor(object, System.IntPtr)""
  IL_0015:  dup
  IL_0016:  stsfld     ""System.Action D<T>.<>O.<0>__Target""
  IL_001b:  callvirt   ""void System.Action.Invoke()""
  IL_0020:  ret
}
");
    }

    [Fact]
    public void CacheExplicitConversions_TypeScoped2()
    {
        var source = @"
using System;
class C
{
    public static void Main(string[] args)
    {
        (new D<int>()).Test();
    }
}
class D<T>
{
    public void Test()
    {
        var t = (Action)Target<T>;
        t();
    }

    static void Target<K>() { Console.WriteLine(""PASS""); }
}
";
        var verifier = CompileAndVerify(source, expectedOutput: PASS);
        verifier.VerifyIL("D<T>.Test", @"
{
  // Code size       33 (0x21)
  .maxstack  2
  IL_0000:  ldsfld     ""System.Action D<T>.<>O.<0>__Target""
  IL_0005:  dup
  IL_0006:  brtrue.s   IL_001b
  IL_0008:  pop
  IL_0009:  ldnull
  IL_000a:  ldftn      ""void D<T>.Target<T>()""
  IL_0010:  newobj     ""System.Action..ctor(object, System.IntPtr)""
  IL_0015:  dup
  IL_0016:  stsfld     ""System.Action D<T>.<>O.<0>__Target""
  IL_001b:  callvirt   ""void System.Action.Invoke()""
  IL_0020:  ret
}
");
    }

    [Fact]
    public void CacheExplicitConversions_TypeScoped3()
    {
        var source = @"
using System;
class C
{
    public static void Main(string[] args)
    {
        (new D<int>()).Test();
    }
}
class D<T>
{
    public void Test()
    {
        var t = (Action)E<T>.Target;
        t();
    }
}
class E<V>
{
    public static void Target() { Console.WriteLine(""PASS""); }
}
";
        var verifier = CompileAndVerify(source, expectedOutput: PASS);
        verifier.VerifyIL("D<T>.Test", @"
{
  // Code size       33 (0x21)
  .maxstack  2
  IL_0000:  ldsfld     ""System.Action D<T>.<>O.<0>__Target""
  IL_0005:  dup
  IL_0006:  brtrue.s   IL_001b
  IL_0008:  pop
  IL_0009:  ldnull
  IL_000a:  ldftn      ""void E<T>.Target()""
  IL_0010:  newobj     ""System.Action..ctor(object, System.IntPtr)""
  IL_0015:  dup
  IL_0016:  stsfld     ""System.Action D<T>.<>O.<0>__Target""
  IL_001b:  callvirt   ""void System.Action.Invoke()""
  IL_0020:  ret
}
");
    }

    [Fact]
    public void CacheExplicitConversions_TypeScoped4()
    {
        var source = @"
using System;
class C
{
    public static void Main(string[] args)
    {
        (new D<int>()).Test();
    }
}
class D<T>
{
    public void Test()
    {
        var t = (Action)E<T>.Target<double>;
        t();
    }
}
class E<V>
{
    public static void Target<N>() { Console.WriteLine(""PASS""); }
}
";
        var verifier = CompileAndVerify(source, expectedOutput: PASS);
        verifier.VerifyIL("D<T>.Test", @"
{
  // Code size       33 (0x21)
  .maxstack  2
  IL_0000:  ldsfld     ""System.Action D<T>.<>O.<0>__Target""
  IL_0005:  dup
  IL_0006:  brtrue.s   IL_001b
  IL_0008:  pop
  IL_0009:  ldnull
  IL_000a:  ldftn      ""void E<T>.Target<double>()""
  IL_0010:  newobj     ""System.Action..ctor(object, System.IntPtr)""
  IL_0015:  dup
  IL_0016:  stsfld     ""System.Action D<T>.<>O.<0>__Target""
  IL_001b:  callvirt   ""void System.Action.Invoke()""
  IL_0020:  ret
}
");
    }

    [Fact]
    public void CacheExplicitConversions_TypeScoped5()
    {
        var source = @"
using System;
class C
{
    public static void Main(string[] args)
    {
        (new D<int>()).Test();
    }
}
class D<T>
{
    public void Test()
    {
        var t = (Func<T>)E<int>.Target<T>;
        t();
    }
}
class E<V>
{
    public static N Target<N>() { Console.WriteLine(""PASS""); return default(N); }
}
";
        var verifier = CompileAndVerify(source, expectedOutput: PASS);
        verifier.VerifyIL("D<T>.Test", @"
{
  // Code size       34 (0x22)
  .maxstack  2
  IL_0000:  ldsfld     ""System.Func<T> D<T>.<>O.<0>__Target""
  IL_0005:  dup
  IL_0006:  brtrue.s   IL_001b
  IL_0008:  pop
  IL_0009:  ldnull
  IL_000a:  ldftn      ""T E<int>.Target<T>()""
  IL_0010:  newobj     ""System.Func<T>..ctor(object, System.IntPtr)""
  IL_0015:  dup
  IL_0016:  stsfld     ""System.Func<T> D<T>.<>O.<0>__Target""
  IL_001b:  callvirt   ""T System.Func<T>.Invoke()""
  IL_0020:  pop
  IL_0021:  ret
}
");
    }

    [Fact]
    public void CacheExplicitConversions_TypeScoped6()
    {
        var source = @"
using System;
class C
{
    public static void Main(string[] args)
    {
        (new D<int>()).Test();
    }
}
class D<T>
{
    public void Test()
    {
        var t = (Func<int>)E<T>.Target<int>;
        t();
    }
}
class E<K>
{
    public static V Target<V>() { Console.WriteLine(""PASS""); return default(V); }
}
";
        var verifier = CompileAndVerify(source, expectedOutput: PASS);
        verifier.VerifyIL("D<T>.Test", @"
{
  // Code size       34 (0x22)
  .maxstack  2
  IL_0000:  ldsfld     ""System.Func<int> D<T>.<>O.<0>__Target""
  IL_0005:  dup
  IL_0006:  brtrue.s   IL_001b
  IL_0008:  pop
  IL_0009:  ldnull
  IL_000a:  ldftn      ""int E<T>.Target<int>()""
  IL_0010:  newobj     ""System.Func<int>..ctor(object, System.IntPtr)""
  IL_0015:  dup
  IL_0016:  stsfld     ""System.Func<int> D<T>.<>O.<0>__Target""
  IL_001b:  callvirt   ""int System.Func<int>.Invoke()""
  IL_0020:  pop
  IL_0021:  ret
}
");
    }

    [Fact]
    public void CacheExplicitConversions_TypeScoped7()
    {
        var source = @"
using System;
class C
{
    public static void Main(string[] args)
    {
        (new D<int>()).Test();
    }
}
class D<T>
{
    public void Test()
    {
        var t = (Func<T, int>)E<T>.Target<int>;
        t(default(T));
    }
}
class E<K>
{
    public static V Target<V>(K k) { Console.WriteLine(""PASS""); return default(V); }
}
";
        var verifier = CompileAndVerify(source, expectedOutput: PASS);
        verifier.VerifyIL("D<T>.Test", @"
{
  // Code size       43 (0x2b)
  .maxstack  2
  .locals init (T V_0)
  IL_0000:  ldsfld     ""System.Func<T, int> D<T>.<>O.<0>__Target""
  IL_0005:  dup
  IL_0006:  brtrue.s   IL_001b
  IL_0008:  pop
  IL_0009:  ldnull
  IL_000a:  ldftn      ""int E<T>.Target<int>(T)""
  IL_0010:  newobj     ""System.Func<T, int>..ctor(object, System.IntPtr)""
  IL_0015:  dup
  IL_0016:  stsfld     ""System.Func<T, int> D<T>.<>O.<0>__Target""
  IL_001b:  ldloca.s   V_0
  IL_001d:  initobj    ""T""
  IL_0023:  ldloc.0
  IL_0024:  callvirt   ""int System.Func<T, int>.Invoke(T)""
  IL_0029:  pop
  IL_002a:  ret
}
");
    }

    [Fact]
    public void CacheExplicitConversions_MethodScoped0()
    {
        var source = @"
using System;
class C
{
    public static void Main(string[] args)
    {
        D.Test<int>();
    }

    public static void Target<K>() { Console.WriteLine(""PASS""); }
}
class D
{
    public static void Test<T>()
    {
        var t = (Action)C.Target<T>;
        t();
    }
}
";
        var verifier = CompileAndVerify(source, expectedOutput: PASS);
        verifier.VerifyIL("D.Test<T>", @"
{
  // Code size       33 (0x21)
  .maxstack  2
  IL_0000:  ldsfld     ""System.Action D.<Test>O__0_0<T>.<0>__Target""
  IL_0005:  dup
  IL_0006:  brtrue.s   IL_001b
  IL_0008:  pop
  IL_0009:  ldnull
  IL_000a:  ldftn      ""void C.Target<T>()""
  IL_0010:  newobj     ""System.Action..ctor(object, System.IntPtr)""
  IL_0015:  dup
  IL_0016:  stsfld     ""System.Action D.<Test>O__0_0<T>.<0>__Target""
  IL_001b:  callvirt   ""void System.Action.Invoke()""
  IL_0020:  ret
}
");
    }

    [Fact]
    public void CacheExplicitConversions_MethodScoped1()
    {
        var source = @"
using System;
class C
{
    public static void Main(string[] args)
    {
        (new D()).Test<int>();
    }
}
class D
{
    public void Test<T>()
    {
        var t = (Action)E<T>.Target;
        t();
    }
}
class E<K>
{
    public static void Target() { Console.WriteLine(""PASS""); }
}
";
        var verifier = CompileAndVerify(source, expectedOutput: PASS);
        verifier.VerifyIL("D.Test<T>", @"
{
  // Code size       33 (0x21)
  .maxstack  2
  IL_0000:  ldsfld     ""System.Action D.<Test>O__0_0<T>.<0>__Target""
  IL_0005:  dup
  IL_0006:  brtrue.s   IL_001b
  IL_0008:  pop
  IL_0009:  ldnull
  IL_000a:  ldftn      ""void E<T>.Target()""
  IL_0010:  newobj     ""System.Action..ctor(object, System.IntPtr)""
  IL_0015:  dup
  IL_0016:  stsfld     ""System.Action D.<Test>O__0_0<T>.<0>__Target""
  IL_001b:  callvirt   ""void System.Action.Invoke()""
  IL_0020:  ret
}
");
    }

    [Fact]
    public void CacheExplicitConversions_MethodScoped2()
    {
        var source = @"
using System;
class C
{
    public static void Main(string[] args)
    {
        (new D()).Test<int>();
    }
}
class D
{
    public void Test<T>()
    {
        var t = (Func<T, int>)E<T>.Target<int>;
        t(default(T));
    }
}
class E<K>
{
    public static V Target<V>(K k) { Console.WriteLine(""PASS""); return default(V); }
}
";
        var verifier = CompileAndVerify(source, expectedOutput: PASS);
        verifier.VerifyIL("D.Test<T>", @"
{
  // Code size       43 (0x2b)
  .maxstack  2
  .locals init (T V_0)
  IL_0000:  ldsfld     ""System.Func<T, int> D.<Test>O__0_0<T>.<0>__Target""
  IL_0005:  dup
  IL_0006:  brtrue.s   IL_001b
  IL_0008:  pop
  IL_0009:  ldnull
  IL_000a:  ldftn      ""int E<T>.Target<int>(T)""
  IL_0010:  newobj     ""System.Func<T, int>..ctor(object, System.IntPtr)""
  IL_0015:  dup
  IL_0016:  stsfld     ""System.Func<T, int> D.<Test>O__0_0<T>.<0>__Target""
  IL_001b:  ldloca.s   V_0
  IL_001d:  initobj    ""T""
  IL_0023:  ldloc.0
  IL_0024:  callvirt   ""int System.Func<T, int>.Invoke(T)""
  IL_0029:  pop
  IL_002a:  ret
}
");
    }

    [Fact]
    public void CacheExplicitConversions_MethodScoped3()
    {
        var source = @"
using System;
class C
{
    public static void Main(string[] args)
    {
        (new D<long>()).Test<int>();
    }
}
class D<M>
{
    public void Test<T>()
    {
        var t = (Action<M, T>)E<M>.Target<T>;
        t(default(M), default(T));
    }
}
class E<K>
{
    public static void Target<V>(K k, V v) { Console.WriteLine(""PASS""); }
}
";
        var verifier = CompileAndVerify(source, expectedOutput: PASS);
        verifier.VerifyIL("D<M>.Test<T>", @"
{
  // Code size       51 (0x33)
  .maxstack  3
  .locals init (M V_0,
                T V_1)
  IL_0000:  ldsfld     ""System.Action<M, T> D<M>.<Test>O__0_0<T>.<0>__Target""
  IL_0005:  dup
  IL_0006:  brtrue.s   IL_001b
  IL_0008:  pop
  IL_0009:  ldnull
  IL_000a:  ldftn      ""void E<M>.Target<T>(M, T)""
  IL_0010:  newobj     ""System.Action<M, T>..ctor(object, System.IntPtr)""
  IL_0015:  dup
  IL_0016:  stsfld     ""System.Action<M, T> D<M>.<Test>O__0_0<T>.<0>__Target""
  IL_001b:  ldloca.s   V_0
  IL_001d:  initobj    ""M""
  IL_0023:  ldloc.0
  IL_0024:  ldloca.s   V_1
  IL_0026:  initobj    ""T""
  IL_002c:  ldloc.1
  IL_002d:  callvirt   ""void System.Action<M, T>.Invoke(M, T)""
  IL_0032:  ret
}
");
    }

    [Fact]
    public void CacheImplicitConversions_TypeScoped_CouldBeModuleScoped0()
    {
        var source = @"
using System;
delegate void MyAction<T>();
class C
{
    public static void Main(string[] args)
    {
        MyAction<int> t = Target;
        t();
    }

    static void Target() { Console.WriteLine(""PASS""); }
}
";
        var verifier = CompileAndVerify(source, expectedOutput: PASS);
        verifier.VerifyIL("C.Main", @"
{
  // Code size       33 (0x21)
  .maxstack  2
  IL_0000:  ldsfld     ""MyAction<int> C.<>O.<0>__Target""
  IL_0005:  dup
  IL_0006:  brtrue.s   IL_001b
  IL_0008:  pop
  IL_0009:  ldnull
  IL_000a:  ldftn      ""void C.Target()""
  IL_0010:  newobj     ""MyAction<int>..ctor(object, System.IntPtr)""
  IL_0015:  dup
  IL_0016:  stsfld     ""MyAction<int> C.<>O.<0>__Target""
  IL_001b:  callvirt   ""void MyAction<int>.Invoke()""
  IL_0020:  ret
}
");
    }

    [Fact]
    public void CacheImplicitConversions_TypeScoped_CouldBeModuleScoped1()
    {
        var source = @"
using System;
class C
{
    public delegate void MyAction<T>();

    public static void Main(string[] args)
    {
        MyAction<int> t = Target;
        t();
    }

    static void Target() { Console.WriteLine(""PASS""); }
}
";
        var verifier = CompileAndVerify(source, expectedOutput: PASS);
        verifier.VerifyIL("C.Main", @"
{
  // Code size       33 (0x21)
  .maxstack  2
  IL_0000:  ldsfld     ""C.MyAction<int> C.<>O.<0>__Target""
  IL_0005:  dup
  IL_0006:  brtrue.s   IL_001b
  IL_0008:  pop
  IL_0009:  ldnull
  IL_000a:  ldftn      ""void C.Target()""
  IL_0010:  newobj     ""C.MyAction<int>..ctor(object, System.IntPtr)""
  IL_0015:  dup
  IL_0016:  stsfld     ""C.MyAction<int> C.<>O.<0>__Target""
  IL_001b:  callvirt   ""void C.MyAction<int>.Invoke()""
  IL_0020:  ret
}
");
    }

    [Fact]
    public void CacheImplicitConversions_TypeScoped0()
    {
        var source = @"
using System;
delegate void MyAction();
class C
{
    public static void Main(string[] args)
    {
        (new D<int>()).Test();
    }
}
class D<T>
{
    public void Test()
    {
        MyAction t = Target;
        t();
    }

    public static void Target() { Console.WriteLine(""PASS""); }
}
";
        var verifier = CompileAndVerify(source, expectedOutput: PASS);
        verifier.VerifyIL("D<T>.Test", @"
{
  // Code size       33 (0x21)
  .maxstack  2
  IL_0000:  ldsfld     ""MyAction D<T>.<>O.<0>__Target""
  IL_0005:  dup
  IL_0006:  brtrue.s   IL_001b
  IL_0008:  pop
  IL_0009:  ldnull
  IL_000a:  ldftn      ""void D<T>.Target()""
  IL_0010:  newobj     ""MyAction..ctor(object, System.IntPtr)""
  IL_0015:  dup
  IL_0016:  stsfld     ""MyAction D<T>.<>O.<0>__Target""
  IL_001b:  callvirt   ""void MyAction.Invoke()""
  IL_0020:  ret
}
");
    }

    [Fact]
    public void CacheImplicitConversions_TypeScoped1()
    {
        var source = @"
using System;
class C
{
    public static void Main(string[] args)
    {
        (new D<int>()).Test()();
    }
}
class D<T>
{
    public delegate void MyAction();

    public MyAction Test()
    {
        return Target<int>;
    }

    static void Target<K>() { Console.WriteLine(""PASS""); }
}
";
        var verifier = CompileAndVerify(source, expectedOutput: PASS);
        verifier.VerifyIL("D<T>.Test", @"
{
  // Code size       28 (0x1c)
  .maxstack  2
  IL_0000:  ldsfld     ""D<T>.MyAction D<T>.<>O.<0>__Target""
  IL_0005:  dup
  IL_0006:  brtrue.s   IL_001b
  IL_0008:  pop
  IL_0009:  ldnull
  IL_000a:  ldftn      ""void D<T>.Target<int>()""
  IL_0010:  newobj     ""D<T>.MyAction..ctor(object, System.IntPtr)""
  IL_0015:  dup
  IL_0016:  stsfld     ""D<T>.MyAction D<T>.<>O.<0>__Target""
  IL_001b:  ret
}
");
    }

    [Fact]
    public void CacheImplicitConversions_TypeScoped2()
    {
        var source = @"
using System;
class C
{
    public static void Main(string[] args)
    {
        (new D<int>()).Test();
    }
}
class D<T>
{
    delegate void MyAction<M>();

    public void Test()
    {
        MyAction<int> t = Target<T>;
        t();
    }

    static void Target<K>() { Console.WriteLine(""PASS""); }
}
";
        var verifier = CompileAndVerify(source, expectedOutput: PASS);
        verifier.VerifyIL("D<T>.Test", @"
{
  // Code size       33 (0x21)
  .maxstack  2
  IL_0000:  ldsfld     ""D<T>.MyAction<int> D<T>.<>O.<0>__Target""
  IL_0005:  dup
  IL_0006:  brtrue.s   IL_001b
  IL_0008:  pop
  IL_0009:  ldnull
  IL_000a:  ldftn      ""void D<T>.Target<T>()""
  IL_0010:  newobj     ""D<T>.MyAction<int>..ctor(object, System.IntPtr)""
  IL_0015:  dup
  IL_0016:  stsfld     ""D<T>.MyAction<int> D<T>.<>O.<0>__Target""
  IL_001b:  callvirt   ""void D<T>.MyAction<int>.Invoke()""
  IL_0020:  ret
}
");
    }

    [Fact]
    public void CacheImplicitConversions_TypeScoped3()
    {
        var source = @"
using System;
class C
{
    public static void Main(string[] args)
    {
        (new D<int>()).Test();
    }
}
class D<T>
{
    delegate void MyAction<M>();

    public void Test()
    {
        MyAction<T> t = E<T>.Target;
        t();
    }
}
class E<V>
{
    public static void Target() { Console.WriteLine(""PASS""); }
}
";
        var verifier = CompileAndVerify(source, expectedOutput: PASS);
        verifier.VerifyIL("D<T>.Test", @"
{
  // Code size       33 (0x21)
  .maxstack  2
  IL_0000:  ldsfld     ""D<T>.MyAction<T> D<T>.<>O.<0>__Target""
  IL_0005:  dup
  IL_0006:  brtrue.s   IL_001b
  IL_0008:  pop
  IL_0009:  ldnull
  IL_000a:  ldftn      ""void E<T>.Target()""
  IL_0010:  newobj     ""D<T>.MyAction<T>..ctor(object, System.IntPtr)""
  IL_0015:  dup
  IL_0016:  stsfld     ""D<T>.MyAction<T> D<T>.<>O.<0>__Target""
  IL_001b:  callvirt   ""void D<T>.MyAction<T>.Invoke()""
  IL_0020:  ret
}
");
    }

    [Fact]
    public void CacheImplicitConversions_TypeScoped4()
    {
        var source = @"
using System;
class C
{
    public static void Main(string[] args)
    {
        (new D<int>()).Test();
    }
}
class D<T>
{
    delegate void MyAction<M>();

    public void Test()
    {
        MyAction<T> t = E<T>.Target<int>;
        t();
    }
}
class E<V>
{
    public static void Target<N>() { Console.WriteLine(""PASS""); }
}
";
        var verifier = CompileAndVerify(source, expectedOutput: PASS);
        verifier.VerifyIL("D<T>.Test", @"
{
  // Code size       33 (0x21)
  .maxstack  2
  IL_0000:  ldsfld     ""D<T>.MyAction<T> D<T>.<>O.<0>__Target""
  IL_0005:  dup
  IL_0006:  brtrue.s   IL_001b
  IL_0008:  pop
  IL_0009:  ldnull
  IL_000a:  ldftn      ""void E<T>.Target<int>()""
  IL_0010:  newobj     ""D<T>.MyAction<T>..ctor(object, System.IntPtr)""
  IL_0015:  dup
  IL_0016:  stsfld     ""D<T>.MyAction<T> D<T>.<>O.<0>__Target""
  IL_001b:  callvirt   ""void D<T>.MyAction<T>.Invoke()""
  IL_0020:  ret
}
");
    }

    [Fact]
    public void CacheImplicitConversions_TypeScoped5()
    {
        var source = @"
using System;
class C
{
    public static void Main(string[] args)
    {
        (new D<int>()).Test();
    }
}
class D<T>
{
    delegate T MyFunc();

    public void Test()
    {
        MyFunc t = E<T>.Target<T, int>;
        t();
    }
}
class E<K>
{
    public static V Target<V, K>() { Console.WriteLine(""PASS""); return default(V); }
}
";
        var verifier = CompileAndVerify(source, expectedOutput: PASS);
        verifier.VerifyIL("D<T>.Test", @"
{
  // Code size       34 (0x22)
  .maxstack  2
  IL_0000:  ldsfld     ""D<T>.MyFunc D<T>.<>O.<0>__Target""
  IL_0005:  dup
  IL_0006:  brtrue.s   IL_001b
  IL_0008:  pop
  IL_0009:  ldnull
  IL_000a:  ldftn      ""T E<T>.Target<T, int>()""
  IL_0010:  newobj     ""D<T>.MyFunc..ctor(object, System.IntPtr)""
  IL_0015:  dup
  IL_0016:  stsfld     ""D<T>.MyFunc D<T>.<>O.<0>__Target""
  IL_001b:  callvirt   ""T D<T>.MyFunc.Invoke()""
  IL_0020:  pop
  IL_0021:  ret
}
");
    }

    [Fact]
    public void CacheImplicitConversions_TypeScoped6()
    {
        var source = @"
using System;
class C
{
    public static void Main(string[] args)
    {
        (new D<int>()).Test();
    }
}
class D<T>
{
    delegate int MyFunc(T i);

    public void Test()
    {
        MyFunc t = E<T>.Target<int>;
        t(default(T));
    }
}
class E<K>
{
    public static V Target<V>(K k) { Console.WriteLine(""PASS""); return default(V); }
}
";
        var verifier = CompileAndVerify(source, expectedOutput: PASS);
        verifier.VerifyIL("D<T>.Test", @"
{
  // Code size       43 (0x2b)
  .maxstack  2
  .locals init (T V_0)
  IL_0000:  ldsfld     ""D<T>.MyFunc D<T>.<>O.<0>__Target""
  IL_0005:  dup
  IL_0006:  brtrue.s   IL_001b
  IL_0008:  pop
  IL_0009:  ldnull
  IL_000a:  ldftn      ""int E<T>.Target<int>(T)""
  IL_0010:  newobj     ""D<T>.MyFunc..ctor(object, System.IntPtr)""
  IL_0015:  dup
  IL_0016:  stsfld     ""D<T>.MyFunc D<T>.<>O.<0>__Target""
  IL_001b:  ldloca.s   V_0
  IL_001d:  initobj    ""T""
  IL_0023:  ldloc.0
  IL_0024:  callvirt   ""int D<T>.MyFunc.Invoke(T)""
  IL_0029:  pop
  IL_002a:  ret
}
");
    }

    [Fact]
    public void CacheImplicitConversions_TypeScoped7()
    {
        var source = @"
using System;
class C
{
    public static void Main(string[] args)
    {
        (new D<int, double>()).Test();
    }
}
class D<T, M>
{
    delegate T MyFunc(M m);

    public void Test()
    {
        MyFunc t = E<M>.Target<T>;
        t(default(M));
    }
}
class E<K>
{
    public static V Target<V>(K k) { Console.WriteLine(""PASS""); return default(V); }
}
";
        var verifier = CompileAndVerify(source, expectedOutput: PASS);
        verifier.VerifyIL("D<T, M>.Test", @"
{
  // Code size       43 (0x2b)
  .maxstack  2
  .locals init (M V_0)
  IL_0000:  ldsfld     ""D<T, M>.MyFunc D<T, M>.<>O.<0>__Target""
  IL_0005:  dup
  IL_0006:  brtrue.s   IL_001b
  IL_0008:  pop
  IL_0009:  ldnull
  IL_000a:  ldftn      ""T E<M>.Target<T>(M)""
  IL_0010:  newobj     ""D<T, M>.MyFunc..ctor(object, System.IntPtr)""
  IL_0015:  dup
  IL_0016:  stsfld     ""D<T, M>.MyFunc D<T, M>.<>O.<0>__Target""
  IL_001b:  ldloca.s   V_0
  IL_001d:  initobj    ""M""
  IL_0023:  ldloc.0
  IL_0024:  callvirt   ""T D<T, M>.MyFunc.Invoke(M)""
  IL_0029:  pop
  IL_002a:  ret
}
");
    }

    [Fact]
    public void CacheImplicitConversions_TypeScoped8()
    {
        var source = @"
using System;
class C
{
    delegate void MyAction<T>();

    public static void Main(string[] args)
    {
        MyAction<int> t = Target;
        t();
    }

    static void Target() { Console.WriteLine(""PASS""); }
}
";
        var verifier = CompileAndVerify(source, expectedOutput: PASS);
        verifier.VerifyIL("C.Main", @"
{
  // Code size       33 (0x21)
  .maxstack  2
  IL_0000:  ldsfld     ""C.MyAction<int> C.<>O.<0>__Target""
  IL_0005:  dup
  IL_0006:  brtrue.s   IL_001b
  IL_0008:  pop
  IL_0009:  ldnull
  IL_000a:  ldftn      ""void C.Target()""
  IL_0010:  newobj     ""C.MyAction<int>..ctor(object, System.IntPtr)""
  IL_0015:  dup
  IL_0016:  stsfld     ""C.MyAction<int> C.<>O.<0>__Target""
  IL_001b:  callvirt   ""void C.MyAction<int>.Invoke()""
  IL_0020:  ret
}
");
    }

    [Fact]
    public void CacheImplicitConversions_MethodScoped0()
    {
        var source = @"
using System;
class C
{
    public static void Main(string[] args)
    {
        (new D()).Test<int>();
    }

    public static void Target<K>() { Console.WriteLine(""PASS""); }
}
class D
{
    delegate void MyAction();

    public void Test<T>()
    {
        MyAction t = C.Target<T>;
        t();
    }
}
";
        var verifier = CompileAndVerify(source, expectedOutput: PASS);
        verifier.VerifyIL("D.Test<T>", @"
{
  // Code size       33 (0x21)
  .maxstack  2
  IL_0000:  ldsfld     ""D.MyAction D.<Test>O__1_0<T>.<0>__Target""
  IL_0005:  dup
  IL_0006:  brtrue.s   IL_001b
  IL_0008:  pop
  IL_0009:  ldnull
  IL_000a:  ldftn      ""void C.Target<T>()""
  IL_0010:  newobj     ""D.MyAction..ctor(object, System.IntPtr)""
  IL_0015:  dup
  IL_0016:  stsfld     ""D.MyAction D.<Test>O__1_0<T>.<0>__Target""
  IL_001b:  callvirt   ""void D.MyAction.Invoke()""
  IL_0020:  ret
}
");
    }

    [Fact]
    public void CacheImplicitConversions_MethodScoped1()
    {
        var source = @"
using System;
class C
{
    public static void Main(string[] args)
    {
        (new D<int>()).Test<int>();
    }

    public static void Target<K, N>(K k) { Console.WriteLine(""PASS""); }
}
class D<V>
{
    delegate void MyAction<M>(M m);

    public void Test<T>()
    {
        MyAction<V> t = C.Target<V, T>;
        t(default(V));
    }
}
";
        var verifier = CompileAndVerify(source, expectedOutput: PASS);
        verifier.VerifyIL("D<V>.Test<T>", @"
{
  // Code size       42 (0x2a)
  .maxstack  2
  .locals init (V V_0)
  IL_0000:  ldsfld     ""D<V>.MyAction<V> D<V>.<Test>O__1_0<T>.<0>__Target""
  IL_0005:  dup
  IL_0006:  brtrue.s   IL_001b
  IL_0008:  pop
  IL_0009:  ldnull
  IL_000a:  ldftn      ""void C.Target<V, T>(V)""
  IL_0010:  newobj     ""D<V>.MyAction<V>..ctor(object, System.IntPtr)""
  IL_0015:  dup
  IL_0016:  stsfld     ""D<V>.MyAction<V> D<V>.<Test>O__1_0<T>.<0>__Target""
  IL_001b:  ldloca.s   V_0
  IL_001d:  initobj    ""V""
  IL_0023:  ldloc.0
  IL_0024:  callvirt   ""void D<V>.MyAction<V>.Invoke(V)""
  IL_0029:  ret
}
");
    }

    [Fact]
    public void CacheImplicitConversions_MethodScoped2()
    {
        var source = @"
using System;
class C
{
    public static void Main(string[] args)
    {
        (new D<byte>()).Test<int>();
    }

    public static N Target<K, N>(K k, N n) { Console.WriteLine(""PASS""); return default(N); }
}
class D<V>
{
    delegate V MyFunc<M>(M m, V v);

    public void Test<T>()
    {
        MyFunc<T> t = C.Target<T, V>;
        t(default(T), default(V));
    }
}
";
        var verifier = CompileAndVerify(source, expectedOutput: PASS);
        verifier.VerifyIL("D<V>.Test<T>", @"
{
  // Code size       52 (0x34)
  .maxstack  3
  .locals init (T V_0,
                V V_1)
  IL_0000:  ldsfld     ""D<V>.MyFunc<T> D<V>.<Test>O__1_0<T>.<0>__Target""
  IL_0005:  dup
  IL_0006:  brtrue.s   IL_001b
  IL_0008:  pop
  IL_0009:  ldnull
  IL_000a:  ldftn      ""V C.Target<T, V>(T, V)""
  IL_0010:  newobj     ""D<V>.MyFunc<T>..ctor(object, System.IntPtr)""
  IL_0015:  dup
  IL_0016:  stsfld     ""D<V>.MyFunc<T> D<V>.<Test>O__1_0<T>.<0>__Target""
  IL_001b:  ldloca.s   V_0
  IL_001d:  initobj    ""T""
  IL_0023:  ldloc.0
  IL_0024:  ldloca.s   V_1
  IL_0026:  initobj    ""V""
  IL_002c:  ldloc.1
  IL_002d:  callvirt   ""V D<V>.MyFunc<T>.Invoke(T, V)""
  IL_0032:  pop
  IL_0033:  ret
}
");
    }

    [Fact]
    public void Where_TypeScoped0()
    {
        var source = @"
using System;
class C
{
    public static void Main(string[] args)
    {
        (new D<C>()).Test();
    }
}
class D<T> where T : C
{
    delegate void MyAction(T t);

    public void Test()
    {
        MyAction t = E<int>.Target<C>;
        t(null);
    }
}
class E<V>
{
    public static void Target<N>(N n) { Console.WriteLine(""PASS""); }
}
";
        var verifier = CompileAndVerify(source, expectedOutput: PASS);
        verifier.VerifyIL("D<T>.Test", @"
{
  // Code size       42 (0x2a)
  .maxstack  2
  .locals init (T V_0)
  IL_0000:  ldsfld     ""D<T>.MyAction D<T>.<>O.<0>__Target""
  IL_0005:  dup
  IL_0006:  brtrue.s   IL_001b
  IL_0008:  pop
  IL_0009:  ldnull
  IL_000a:  ldftn      ""void E<int>.Target<C>(C)""
  IL_0010:  newobj     ""D<T>.MyAction..ctor(object, System.IntPtr)""
  IL_0015:  dup
  IL_0016:  stsfld     ""D<T>.MyAction D<T>.<>O.<0>__Target""
  IL_001b:  ldloca.s   V_0
  IL_001d:  initobj    ""T""
  IL_0023:  ldloc.0
  IL_0024:  callvirt   ""void D<T>.MyAction.Invoke(T)""
  IL_0029:  ret
}
");
    }

    [Fact]
    public void Where_MethodScoped0()
    {
        var source = @"
using System;
class C
{
    public static void Main(string[] args)
    {
        (new D<C>()).Test(new C());
    }
}
class D<T> where T : C
{
    delegate T MyFunc();

    public void Test<M>(M m) where M : T
    {
        MyFunc t = E<int>.Target<M>;
        t();
    }
}
class E<V>
{
    public static N Target<N>() { Console.WriteLine(""PASS""); return default(N); }
}
";
        static void containerValidator(ModuleSymbol module)
        {
            var testClass = module.GlobalNamespace.GetTypeMember("D");
            var container = testClass.GetTypeMember("<Test>O__1_0");
            Assert.NotNull(container); Debug.Assert(container is { });

            var typeParameters = container.TypeParameters;
            Assert.Equal(1, container.TypeParameters.Length);

            var m = typeParameters[0];
            Assert.Equal(1, m.ConstraintTypes().Length);
            Assert.Equal(testClass.TypeParameters[0], m.ConstraintTypes()[0]);
        }
        CompileAndVerify(source, symbolValidator: containerValidator, expectedOutput: PASS).VerifyIL("D<T>.Test<M>", @"
{
  // Code size       34 (0x22)
  .maxstack  2
  IL_0000:  ldsfld     ""D<T>.MyFunc D<T>.<Test>O__1_0<M>.<0>__Target""
  IL_0005:  dup
  IL_0006:  brtrue.s   IL_001b
  IL_0008:  pop
  IL_0009:  ldnull
  IL_000a:  ldftn      ""M E<int>.Target<M>()""
  IL_0010:  newobj     ""D<T>.MyFunc..ctor(object, System.IntPtr)""
  IL_0015:  dup
  IL_0016:  stsfld     ""D<T>.MyFunc D<T>.<Test>O__1_0<M>.<0>__Target""
  IL_001b:  callvirt   ""T D<T>.MyFunc.Invoke()""
  IL_0020:  pop
  IL_0021:  ret
}
");
    }

    [Fact]
    public void Where_MethodScoped1()
    {
        var source = @"
using System;
class C
{
    public static void Main(string[] args)
    {
        (new D()).Test(new C());
    }
}
class D
{
    delegate C MyFunc();

    public void Test<M>(M m) where M : C
    {
        MyFunc t = E<int>.Target<M>;
        t();
    }
}
class E<V>
{
    public static N Target<N>() { Console.WriteLine(""PASS""); return default(N); }
}
";
        static void containerValidator(ModuleSymbol module)
        {
            var globalNs = module.GlobalNamespace;
            var mainClass = globalNs.GetTypeMember("C");
            var container = globalNs.GetMember<NamedTypeSymbol>("D.<Test>O__1_0");
            Assert.NotNull(container); Debug.Assert(container is { });

            var typeParameters = container.TypeParameters;
            Assert.Equal(1, container.TypeParameters.Length);

            var m = typeParameters[0];
            Assert.Equal(1, m.ConstraintTypes().Length);
            Assert.Equal(mainClass, m.ConstraintTypes()[0]);
        }
        CompileAndVerify(source, symbolValidator: containerValidator, expectedOutput: PASS).VerifyIL("D.Test<M>", @"
{
  // Code size       34 (0x22)
  .maxstack  2
  IL_0000:  ldsfld     ""D.MyFunc D.<Test>O__1_0<M>.<0>__Target""
  IL_0005:  dup
  IL_0006:  brtrue.s   IL_001b
  IL_0008:  pop
  IL_0009:  ldnull
  IL_000a:  ldftn      ""M E<int>.Target<M>()""
  IL_0010:  newobj     ""D.MyFunc..ctor(object, System.IntPtr)""
  IL_0015:  dup
  IL_0016:  stsfld     ""D.MyFunc D.<Test>O__1_0<M>.<0>__Target""
  IL_001b:  callvirt   ""C D.MyFunc.Invoke()""
  IL_0020:  pop
  IL_0021:  ret
}
");
    }

    [Fact]
    public void Where_MethodScoped2()
    {
        var source = @"
using System;
class C
{
    public static void Main(string[] args)
    {
        (new D()).Test(0);
    }
}
class D
{
    public void Test<M>(M m) where M : struct
    {
        Func<M?> t = E.Target<M?>;
        t();
    }
}
class E
{
    public static N Target<N>() { Console.WriteLine(""PASS""); return default(N); }
}
";
        static void containerValidator(ModuleSymbol module)
        {
            var container = module.GlobalNamespace.GetMember<NamedTypeSymbol>("D.<Test>O__0_0");
            Assert.NotNull(container); Debug.Assert(container is { });

            var typeParameters = container.TypeParameters;
            Assert.Equal(1, container.TypeParameters.Length);

            var m = typeParameters[0];
            Assert.NotNull(m); Debug.Assert(m is { });
            Assert.True(m.IsValueType);
        }
        CompileAndVerify(source, symbolValidator: containerValidator, expectedOutput: PASS).VerifyIL("D.Test<M>", @"
{
  // Code size       34 (0x22)
  .maxstack  2
  IL_0000:  ldsfld     ""System.Func<M?> D.<Test>O__0_0<M>.<0>__Target""
  IL_0005:  dup
  IL_0006:  brtrue.s   IL_001b
  IL_0008:  pop
  IL_0009:  ldnull
  IL_000a:  ldftn      ""M? E.Target<M?>()""
  IL_0010:  newobj     ""System.Func<M?>..ctor(object, System.IntPtr)""
  IL_0015:  dup
  IL_0016:  stsfld     ""System.Func<M?> D.<Test>O__0_0<M>.<0>__Target""
  IL_001b:  callvirt   ""M? System.Func<M?>.Invoke()""
  IL_0020:  pop
  IL_0021:  ret
}
");
    }

    [Fact]
    public void ExtensionMethod_TypeScoped_CouldBeModuleScoped0()
    {
        var source = @"
using System;
class C
{
    public static void Main(string[] args)
    {
        var t = (Action<C>)E.Target;
        t(null);
    }
}
static class E
{
    public static void Target(this C c) { Console.WriteLine(""PASS""); }
}
";
        CompileAndVerify(source, expectedOutput: PASS).VerifyIL("C.Main", @"
{
  // Code size       34 (0x22)
  .maxstack  2
  IL_0000:  ldsfld     ""System.Action<C> C.<>O.<0>__Target""
  IL_0005:  dup
  IL_0006:  brtrue.s   IL_001b
  IL_0008:  pop
  IL_0009:  ldnull
  IL_000a:  ldftn      ""void E.Target(C)""
  IL_0010:  newobj     ""System.Action<C>..ctor(object, System.IntPtr)""
  IL_0015:  dup
  IL_0016:  stsfld     ""System.Action<C> C.<>O.<0>__Target""
  IL_001b:  ldnull
  IL_001c:  callvirt   ""void System.Action<C>.Invoke(C)""
  IL_0021:  ret
}
");
    }

    [Fact]
    public void ExtensionMethod_TypeScoped_CouldBeModuleScoped1()
    {
        var source = @"
using System;
class C
{
    public static void Main(string[] args)
    {
        Action<C> t = E.Target;
        t(null);
    }
}
static class E
{
    public static void Target<T>(this T t) { Console.WriteLine(""PASS""); }
}
";
        CompileAndVerify(source, expectedOutput: PASS).VerifyIL("C.Main", @"
{
  // Code size       34 (0x22)
  .maxstack  2
  IL_0000:  ldsfld     ""System.Action<C> C.<>O.<0>__Target""
  IL_0005:  dup
  IL_0006:  brtrue.s   IL_001b
  IL_0008:  pop
  IL_0009:  ldnull
  IL_000a:  ldftn      ""void E.Target<C>(C)""
  IL_0010:  newobj     ""System.Action<C>..ctor(object, System.IntPtr)""
  IL_0015:  dup
  IL_0016:  stsfld     ""System.Action<C> C.<>O.<0>__Target""
  IL_001b:  ldnull
  IL_001c:  callvirt   ""void System.Action<C>.Invoke(C)""
  IL_0021:  ret
}
");
    }

    [Fact]
    public void ExtensionMethod_TypeScoped_CouldBeModuleScoped2()
    {
        var source = @"
using System;
static class E
{
    static void Test()
    {
        Action<int> t = Target<int>;
    }

    public static void Target<T>(this T t) { }
}
";
        CompileAndVerify(source).VerifyIL("E.Test", @"
{
  // Code size       25 (0x19)
  .maxstack  2
  IL_0000:  ldsfld     ""System.Action<int> E.<>O.<0>__Target""
  IL_0005:  brtrue.s   IL_0018
  IL_0007:  ldnull
  IL_0008:  ldftn      ""void E.Target<int>(int)""
  IL_000e:  newobj     ""System.Action<int>..ctor(object, System.IntPtr)""
  IL_0013:  stsfld     ""System.Action<int> E.<>O.<0>__Target""
  IL_0018:  ret
}
");
    }

    [Fact]
    public void ExtensionMethod_TypeScoped0()
    {
        var source = @"
using System;
class C
{
    public static void Main(string[] args)
    {
        D<C>.Test();
    }
}
class D<T> where T : C
{
    public static void Test()
    {
        var t = (Action<T>)E.Target;
        t(null);
    }
}
static class E
{
    public static void Target(this C c) { Console.WriteLine(""PASS""); }
}
";
        CompileAndVerify(source, expectedOutput: PASS).VerifyIL("D<T>.Test", @"
{
  // Code size       42 (0x2a)
  .maxstack  2
  .locals init (T V_0)
  IL_0000:  ldsfld     ""System.Action<T> D<T>.<>O.<0>__Target""
  IL_0005:  dup
  IL_0006:  brtrue.s   IL_001b
  IL_0008:  pop
  IL_0009:  ldnull
  IL_000a:  ldftn      ""void E.Target(C)""
  IL_0010:  newobj     ""System.Action<T>..ctor(object, System.IntPtr)""
  IL_0015:  dup
  IL_0016:  stsfld     ""System.Action<T> D<T>.<>O.<0>__Target""
  IL_001b:  ldloca.s   V_0
  IL_001d:  initobj    ""T""
  IL_0023:  ldloc.0
  IL_0024:  callvirt   ""void System.Action<T>.Invoke(T)""
  IL_0029:  ret
}
");
    }

    [Fact]
    public void ExtensionMethod_TypeScoped1()
    {
        var source = @"
using System;
class C
{
    public static void Main(string[] args)
    {
        D<int>.Test(0);
    }
}
class D<K>
{
    public static void Test(K k)
    {
        Action<K> t = E.Target;
        t(k);
    }
}
static class E
{
    public static void Target<T>(this T t) { Console.WriteLine(""PASS""); }
}
";
        CompileAndVerify(source, expectedOutput: PASS).VerifyIL("D<K>.Test", @"
{
  // Code size       34 (0x22)
  .maxstack  2
  IL_0000:  ldsfld     ""System.Action<K> D<K>.<>O.<0>__Target""
  IL_0005:  dup
  IL_0006:  brtrue.s   IL_001b
  IL_0008:  pop
  IL_0009:  ldnull
  IL_000a:  ldftn      ""void E.Target<K>(K)""
  IL_0010:  newobj     ""System.Action<K>..ctor(object, System.IntPtr)""
  IL_0015:  dup
  IL_0016:  stsfld     ""System.Action<K> D<K>.<>O.<0>__Target""
  IL_001b:  ldarg.0
  IL_001c:  callvirt   ""void System.Action<K>.Invoke(K)""
  IL_0021:  ret
}
");
    }

    [Fact]
    public void ExtensionMethod_TypeScoped2()
    {
        var source = @"
using System;
static class E
{
    class F<T>
    {
        void Test()
        {
            Action<T> t = Target<T>;
        }
    }

    public static void Target<T>(this T t) { }
}
";
        CompileAndVerify(source).VerifyIL("E.F<T>.Test", @"
{
  // Code size       25 (0x19)
  .maxstack  2
  IL_0000:  ldsfld     ""System.Action<T> E.F<T>.<>O.<0>__Target""
  IL_0005:  brtrue.s   IL_0018
  IL_0007:  ldnull
  IL_0008:  ldftn      ""void E.Target<T>(T)""
  IL_000e:  newobj     ""System.Action<T>..ctor(object, System.IntPtr)""
  IL_0013:  stsfld     ""System.Action<T> E.F<T>.<>O.<0>__Target""
  IL_0018:  ret
}
");
    }

    [Fact]
    public void ExtensionMethod_MethodScoped0()
    {
        var source = @"
using System;
class C
{
    public static void Main(string[] args)
    {
        Test(0);
    }

    static void Test<T>(T arg)
    {
        var t = (Action<T>)E.Target;
        t(arg);
    }
}
static class E
{
    public static void Target<M>(this M m) { Console.WriteLine(""PASS""); }
}
";
        CompileAndVerify(source, expectedOutput: PASS).VerifyIL("C.Test<T>", @"
{
  // Code size       34 (0x22)
  .maxstack  2
  IL_0000:  ldsfld     ""System.Action<T> C.<Test>O__1_0<T>.<0>__Target""
  IL_0005:  dup
  IL_0006:  brtrue.s   IL_001b
  IL_0008:  pop
  IL_0009:  ldnull
  IL_000a:  ldftn      ""void E.Target<T>(T)""
  IL_0010:  newobj     ""System.Action<T>..ctor(object, System.IntPtr)""
  IL_0015:  dup
  IL_0016:  stsfld     ""System.Action<T> C.<Test>O__1_0<T>.<0>__Target""
  IL_001b:  ldarg.0
  IL_001c:  callvirt   ""void System.Action<T>.Invoke(T)""
  IL_0021:  ret
}
");
    }

    [Fact]
    public void ExtensionMethod_MethodScoped1()
    {
        var source = @"
using System;
class C
{
    public static void Main(string[] args)
    {
        Test<C>();
    }

    static void Test<T>() where T : C
    {
        var t = (Action<T>)E.Target;
        t(null);
    }
}
static class E
{
    public static void Target(this C c) { Console.WriteLine(""PASS""); }
}
";
        CompileAndVerify(source, expectedOutput: PASS).VerifyIL("C.Test<T>", @"
{
  // Code size       42 (0x2a)
  .maxstack  2
  .locals init (T V_0)
  IL_0000:  ldsfld     ""System.Action<T> C.<Test>O__1_0<T>.<0>__Target""
  IL_0005:  dup
  IL_0006:  brtrue.s   IL_001b
  IL_0008:  pop
  IL_0009:  ldnull
  IL_000a:  ldftn      ""void E.Target(C)""
  IL_0010:  newobj     ""System.Action<T>..ctor(object, System.IntPtr)""
  IL_0015:  dup
  IL_0016:  stsfld     ""System.Action<T> C.<Test>O__1_0<T>.<0>__Target""
  IL_001b:  ldloca.s   V_0
  IL_001d:  initobj    ""T""
  IL_0023:  ldloc.0
  IL_0024:  callvirt   ""void System.Action<T>.Invoke(T)""
  IL_0029:  ret
}
");
    }

    [Fact]
    public void ExtensionMethod_MethodScoped2()
    {
        var source = @"
using System;
static class E
{
    static void Test<T>()
    {
        Action<T> t = Target<T>;
    }

    public static void Target<T>(this T t) { }
}
";
        CompileAndVerify(source).VerifyIL("E.Test<T>", @"
{
  // Code size       25 (0x19)
  .maxstack  2
  IL_0000:  ldsfld     ""System.Action<T> E.<Test>O__0_0<T>.<0>__Target""
  IL_0005:  brtrue.s   IL_0018
  IL_0007:  ldnull
  IL_0008:  ldftn      ""void E.Target<T>(T)""
  IL_000e:  newobj     ""System.Action<T>..ctor(object, System.IntPtr)""
  IL_0013:  stsfld     ""System.Action<T> E.<Test>O__0_0<T>.<0>__Target""
  IL_0018:  ret
}
");
    }

    [Fact]
    public void Lambda_TypeScoped_CouldBeModuleScoped0()
    {
        var source = @"
using System;
class C
{
    public static void Main(string[] args)
    {
        Action test = () => Invoke(Target);
        test();
    }

    static void Invoke(Action a) => a();

    static void Target() => Console.WriteLine(""PASS"");
}
";
        var verifier = CompileAndVerify(source, expectedOutput: PASS);
        verifier.VerifyIL("C.<>c.<Main>b__0_0", @"
{
  // Code size       33 (0x21)
  .maxstack  2
  IL_0000:  ldsfld     ""System.Action C.<>O.<0>__Target""
  IL_0005:  dup
  IL_0006:  brtrue.s   IL_001b
  IL_0008:  pop
  IL_0009:  ldnull
  IL_000a:  ldftn      ""void C.Target()""
  IL_0010:  newobj     ""System.Action..ctor(object, System.IntPtr)""
  IL_0015:  dup
  IL_0016:  stsfld     ""System.Action C.<>O.<0>__Target""
  IL_001b:  call       ""void C.Invoke(System.Action)""
  IL_0020:  ret
}
");
    }

    [Fact]
    public void Lambda_TypeScoped_CouldBeModuleScoped1()
    {
        var source = @"
using System;
class C
{
    public static void Main(string[] args)
    {
        Action test = () => ((Action)D.Target)();
        test();
    }
}
class D
{
    public static void Target() { Console.WriteLine(""PASS""); }
}
";
        var verifier = CompileAndVerify(source, expectedOutput: PASS);
        verifier.VerifyIL("C.<>c.<Main>b__0_0", @"
{
  // Code size       33 (0x21)
  .maxstack  2
  IL_0000:  ldsfld     ""System.Action C.<>O.<0>__Target""
  IL_0005:  dup
  IL_0006:  brtrue.s   IL_001b
  IL_0008:  pop
  IL_0009:  ldnull
  IL_000a:  ldftn      ""void D.Target()""
  IL_0010:  newobj     ""System.Action..ctor(object, System.IntPtr)""
  IL_0015:  dup
  IL_0016:  stsfld     ""System.Action C.<>O.<0>__Target""
  IL_001b:  callvirt   ""void System.Action.Invoke()""
  IL_0020:  ret
}
");
    }

    [Fact]
    public void Lambda_TypeScoped0()
    {
        var source = @"
using System;
class C
{
    static void Main(string[] args)
    {
        D<int>.Test();
    }
}
class D<T>
{
    public static void Test()
    {
        Action test = () =>
        {
            ((Func<T>)Target)();
        };
        test();
    }

    static T Target() { Console.WriteLine(""PASS""); return default(T); }
}
";
        var verifier = CompileAndVerify(source, expectedOutput: PASS);
        verifier.VerifyIL("D<T>.<>c.<Test>b__0_0", @"
{
  // Code size       34 (0x22)
  .maxstack  2
  IL_0000:  ldsfld     ""System.Func<T> D<T>.<>O.<0>__Target""
  IL_0005:  dup
  IL_0006:  brtrue.s   IL_001b
  IL_0008:  pop
  IL_0009:  ldnull
  IL_000a:  ldftn      ""T D<T>.Target()""
  IL_0010:  newobj     ""System.Func<T>..ctor(object, System.IntPtr)""
  IL_0015:  dup
  IL_0016:  stsfld     ""System.Func<T> D<T>.<>O.<0>__Target""
  IL_001b:  callvirt   ""T System.Func<T>.Invoke()""
  IL_0020:  pop
  IL_0021:  ret
}
");
    }

    [Fact]
    public void Lambda_TypeScoped1()
    {
        var source = @"
using System;
class C
{
    static void Main(string[] args)
    {
        D<int>.Test();
    }
}
class D<T>
{
    delegate T MyFunc();    
    public static void Test()
    {
        Func<MyFunc> a = () => E.Target<T>;
        a()();
    }
}
class E
{
    public static V Target<V>() { Console.WriteLine(""PASS""); return default(V); }
}
";
        var verifier = CompileAndVerify(source, expectedOutput: PASS);
        verifier.VerifyIL("D<T>.<>c.<Test>b__1_0", @"
{
  // Code size       28 (0x1c)
  .maxstack  2
  IL_0000:  ldsfld     ""D<T>.MyFunc D<T>.<>O.<0>__Target""
  IL_0005:  dup
  IL_0006:  brtrue.s   IL_001b
  IL_0008:  pop
  IL_0009:  ldnull
  IL_000a:  ldftn      ""T E.Target<T>()""
  IL_0010:  newobj     ""D<T>.MyFunc..ctor(object, System.IntPtr)""
  IL_0015:  dup
  IL_0016:  stsfld     ""D<T>.MyFunc D<T>.<>O.<0>__Target""
  IL_001b:  ret
}
");
    }

    [Fact]
    public void Lambda_MethodScoped0()
    {
        var source = @"
using System;
class C
{
    static void Main(string[] args)
    {
        D<int>.Test(0);
    }
}
class D<T>
{
    public static void Test<G>(G g)
    {
        Action test = () =>
        {
            ((Func<T>)Target<G>)();
        };
        test();
    }

    static T Target<K>() { Console.WriteLine(""PASS""); return default(T); }
}
";
        var verifier = CompileAndVerify(source, expectedOutput: PASS);
        verifier.VerifyIL("D<T>.<>c__0<G>.<Test>b__0_0", @"
{
  // Code size       34 (0x22)
  .maxstack  2
  IL_0000:  ldsfld     ""System.Func<T> D<T>.<Test>O__0_0<G>.<0>__Target""
  IL_0005:  dup
  IL_0006:  brtrue.s   IL_001b
  IL_0008:  pop
  IL_0009:  ldnull
  IL_000a:  ldftn      ""T D<T>.Target<G>()""
  IL_0010:  newobj     ""System.Func<T>..ctor(object, System.IntPtr)""
  IL_0015:  dup
  IL_0016:  stsfld     ""System.Func<T> D<T>.<Test>O__0_0<G>.<0>__Target""
  IL_001b:  callvirt   ""T System.Func<T>.Invoke()""
  IL_0020:  pop
  IL_0021:  ret
}
");
    }

    [Fact]
    public void Lambda_MethodScoped1()
    {
        var source = @"
using System;
class C
{
    static void Main(string[] args)
    {
        D.Test(0);
    }
}
class D
{
    public static void Test<G>(G g)
    {
        Func<Func<G>> a = () => E<G>.Target;
        a()();
    }
}
class E<V>
{
    public static V Target() { Console.WriteLine(""PASS""); return default(V); }
}
";
        var verifier = CompileAndVerify(source, expectedOutput: PASS);
        verifier.VerifyIL("D.<>c__0<G>.<Test>b__0_0", @"
{
  // Code size       28 (0x1c)
  .maxstack  2
  IL_0000:  ldsfld     ""System.Func<G> D.<Test>O__0_0<G>.<0>__Target""
  IL_0005:  dup
  IL_0006:  brtrue.s   IL_001b
  IL_0008:  pop
  IL_0009:  ldnull
  IL_000a:  ldftn      ""G E<G>.Target()""
  IL_0010:  newobj     ""System.Func<G>..ctor(object, System.IntPtr)""
  IL_0015:  dup
  IL_0016:  stsfld     ""System.Func<G> D.<Test>O__0_0<G>.<0>__Target""
  IL_001b:  ret
}
");
    }

    [Fact]
    public void SameTypeAndSymbolResultsSameField_TypeScoped0()
    {
        var source = @"
using System;
class C<T>
{
    void Test0() { var t = (Action)D.Target<T>; }
    void Test1() { Action t = D.Target<T>; }
}
class D
{
    public static void Target<V>() { }
}
";
        static void containerValidator(ModuleSymbol module)
        {
            var container = module.GlobalNamespace.GetMember<NamedTypeSymbol>("C.<>O");
            Assert.NotNull(container); Debug.Assert(container is { });
            Assert.Equal(0, container.Arity);

            var members = container.GetMembers();
            Assert.Equal(1, members.Length);

            var field = members[0] as FieldSymbol;
            Assert.NotNull(field); Debug.Assert(field is { });
            Assert.Equal("<0>__Target", field.Name);

            var fieldType = field.Type as NamedTypeSymbol;
            Assert.NotNull(fieldType); Debug.Assert(fieldType is { });
            Assert.True(fieldType.IsDelegateType());
            Assert.Equal("System", fieldType.ContainingNamespace.Name);
            Assert.Equal("Action", fieldType.Name);
            Assert.Equal(0, fieldType.Arity);
        }
        CompileAndVerify(source, symbolValidator: containerValidator);
    }

    [Fact]
    public void SameTypeAndSymbolResultsSameField_TypeScoped1()
    {
        var source = @"
using System;
class C<T>
{
    void Test0() { var t = (Func<T>)Target<T>; }
    void Test1() { Func<T> t = Target<T>; }
    static V Target<V>() { return default(V); }
}
";
        static void containerValidator(ModuleSymbol module)
        {
            var container = module.GlobalNamespace.GetMember<NamedTypeSymbol>("C.<>O");
            Assert.NotNull(container); Debug.Assert(container is { });
            Assert.Equal(0, container.Arity);

            var members = container.GetMembers();
            Assert.Equal(1, members.Length);

            var field = members[0] as FieldSymbol;
            Assert.NotNull(field); Debug.Assert(field is { });
            Assert.Equal("<0>__Target", field.Name);

            var fieldType = field.Type as NamedTypeSymbol;
            Assert.NotNull(fieldType); Debug.Assert(fieldType is { });
            Assert.True(fieldType.IsDelegateType());
            Assert.Equal("System", fieldType.ContainingNamespace.Name);
            Assert.Equal("Func", fieldType.Name);
            Assert.Equal(1, fieldType.Arity);
            Assert.Equal(module.GlobalNamespace.GetTypeMember("C").TypeParameters[0], fieldType.TypeArguments()[0]);
        }
        CompileAndVerify(source, symbolValidator: containerValidator);
    }

    [Fact]
    public void SameTypeAndSymbolResultsSameField_TypeScoped2()
    {
        var source = @"
using System;
class C<T, V>
{
    delegate T MyFunc();
    void Test0() { var t = (MyFunc)Target; }
    void Test1() { MyFunc t = Target; }
    static T Target() { return default(T); }
}
";
        static void containerValidator(ModuleSymbol module)
        {
            var container = module.GlobalNamespace.GetMember<NamedTypeSymbol>("C.<>O");
            Assert.NotNull(container); Debug.Assert(container is { });
            Assert.Equal(0, container.Arity);

            var members = container.GetMembers();
            Assert.Equal(1, members.Length);

            var field = members[0] as FieldSymbol;
            Assert.NotNull(field); Debug.Assert(field is { });
            Assert.Equal("<0>__Target", field.Name);

            var fieldType = field.Type as NamedTypeSymbol;
            Assert.Equal(module.GlobalNamespace.GetMember<NamedTypeSymbol>("C.MyFunc"), fieldType);
        }
        CompileAndVerify(source, symbolValidator: containerValidator);
    }

    [Fact]
    public void SameTypeAndSymbolResultsSameField_MethodScoped0()
    {
        var source = @"
using System;
class C
{
    void Test<V>()
    {
        var t0 = (Action)D.Target<V>;
        Action t1 = D.Target<V>;
    }
}
class D
{
    public static void Target<B>() { }
}
";
        static void containerValidator(ModuleSymbol module)
        {
            var container = module.GlobalNamespace.GetMember<NamedTypeSymbol>("C.<Test>O__0_0");
            Assert.NotNull(container); Debug.Assert(container is { });
            Assert.True(container.IsGenericType);

            var members = container.GetMembers();
            Assert.Equal(1, members.Length);

            var field = members[0] as FieldSymbol;
            Assert.NotNull(field); Debug.Assert(field is { });
            Assert.Equal("<0>__Target", field.Name);

            var fieldType = field.Type as NamedTypeSymbol;
            Assert.NotNull(fieldType); Debug.Assert(fieldType is { });
            Assert.True(fieldType.IsDelegateType());
            Assert.Equal("System", fieldType.ContainingNamespace.Name);
            Assert.Equal("Action", fieldType.Name);
            Assert.Equal(0, fieldType.Arity);
        }
        CompileAndVerify(source, symbolValidator: containerValidator);
    }

    [Fact]
    public void SameTypeAndSymbolResultsSameField_MethodScoped1()
    {
        var source = @"
using System;
class C<T>
{
    void Test<V>()
    {
        var t0 = (Func<T, V>)D<V>.Target<T>;
        Func<T, V> t1 = D<V>.Target<T>;
    }
}
class D<B>
{
    public static B Target<H>(H h) => default(B);
}
";
        static void containerValidator(ModuleSymbol module)
        {
            var testClass = module.GlobalNamespace.GetTypeMember("C");
            var container = testClass.GetTypeMember("<Test>O__0_0");
            Assert.NotNull(container); Debug.Assert(container is { });
            Assert.Equal(1, container.Arity);

            var members = container.GetMembers();
            Assert.Equal(1, members.Length);

            var field = members[0] as FieldSymbol;
            Assert.NotNull(field); Debug.Assert(field is { });
            Assert.Equal("<0>__Target", field.Name);

            var fieldType = field.Type as NamedTypeSymbol;
            Assert.NotNull(fieldType); Debug.Assert(fieldType is { });
            Assert.True(fieldType.IsDelegateType());
            Assert.Equal("System", fieldType.ContainingNamespace.Name);
            Assert.Equal("Func", fieldType.Name);
            Assert.Equal(2, fieldType.Arity);
            Assert.Equal(testClass.TypeParameters[0], fieldType.TypeArguments()[0]);
            Assert.Equal(container.TypeParameters[0], fieldType.TypeArguments()[1]);
        }
        CompileAndVerify(source, symbolValidator: containerValidator);
    }

    [Fact]
    public void SameTypeAndSymbolResultsSameField_MethodScoped2()
    {
        var source = @"
using System;
class C<A, T>
{
    delegate O MyFunc(int num);
    class O { }
    void Test<V>() where V : O
    {
        var t0 = (MyFunc)D.Target<V>;
        MyFunc t1 = D.Target<V>;
    }
}
static class D
{
    public static B Target<B>(this int num) => default(B);
}
";
        static void containerValidator(ModuleSymbol module)
        {
            var container = module.GlobalNamespace.GetMember<NamedTypeSymbol>("C.<Test>O__2_0");
            Assert.NotNull(container); Debug.Assert(container is { });
            Assert.Equal(1, container.Arity);

            var members = container.GetMembers();
            Assert.Equal(1, members.Length);

            var field = members[0] as FieldSymbol;
            Assert.NotNull(field); Debug.Assert(field is { });
            Assert.Equal("<0>__Target", field.Name);

            var fieldType = field.Type as NamedTypeSymbol;
            Assert.NotNull(fieldType); Debug.Assert(fieldType is { });
            Assert.True(fieldType.IsDelegateType());
            Assert.Equal(module.GlobalNamespace.GetMember<NamedTypeSymbol>("C.MyFunc"), fieldType);
        }
        CompileAndVerify(source, symbolValidator: containerValidator);
    }

    [Fact]
    public void ContainersCanBeShared_TypeScoped0()
    {
        var source = @"
using System;
class A<T>
{
    class B<V>
    {
        void Test0()
        {
            Action<T> t0 = Target0;
            Action<T> t1 = Target1;
        }

        void Test1()
        {
            Action<T> t1 = Target1;
            Action<T> t2 = D<T>.Target2;
        }

        void Test2()
        {
            Action<T> t2 = D<T>.Target2;
            Action<T, V> t3 = D<T>.Target3<V>;
        }

        void Test3()
        {
            Action<T, V> t3 = D<T>.Target3<V>;
            Action<T, V> t4 = D<T>.E<V>.Target4;
        }

        void Test4()
        {
            Action<T, V> t4 = D<T>.E<V>.Target4;
            Action<T> t5 = E.Target5<T>;
        }

        void Test5()
        {
            Action<T> t5t = E.Target5<T>;
            Action<V> t5v = E.Target5<V>;
        }

        static void Target0(T t) { }
    }

    static void Target1(T t) { }
}
class D<K>
{
    public static void Target2(K k) { }

    public static void Target3<M>(K k, M m) { }

    public class E<P>
    {
        public static void Target4(K k, P p) { }
    }
}
static class E
{
    public static void Target5<N>(this N n) { }
}
";
        static void containerValidator(ModuleSymbol module)
        {
            var A = module.GlobalNamespace.GetTypeMember("A");
            var B = A.GetTypeMember("B");
            var T = A.TypeParameters[0];
            var V = B.TypeParameters[0];

            var container = B.GetTypeMember("<>O");
            Assert.NotNull(container); Debug.Assert(container is { });
            Assert.Equal(0, container.Arity);

            var members = container.GetMembers();
            Assert.Equal(7, members.Length);

            var field0 = members[0] as FieldSymbol;
            Assert.NotNull(field0); Debug.Assert(field0 is { });
            Assert.Equal("<0>__Target0", field0.Name);

            var fieldType0 = field0.Type as NamedTypeSymbol;
            Assert.NotNull(fieldType0); Debug.Assert(fieldType0 is { });
            Assert.True(fieldType0.IsDelegateType());
            Assert.Equal("System", fieldType0.ContainingNamespace.Name);
            Assert.Equal("Action", fieldType0.Name);
            Assert.Equal(1, fieldType0.Arity);
            Assert.Equal(T, fieldType0.TypeArguments()[0]);

            var field1 = members[1] as FieldSymbol;
            Assert.NotNull(field1); Debug.Assert(field1 is { });
            Assert.Equal("<1>__Target1", field1.Name);

            var fieldType1 = field1.Type as NamedTypeSymbol;
            Assert.NotNull(fieldType1); Debug.Assert(fieldType1 is { });
            Assert.True(fieldType1.IsDelegateType());
            Assert.Equal("System", fieldType1.ContainingNamespace.Name);
            Assert.Equal("Action", fieldType1.Name);
            Assert.Equal(1, fieldType1.Arity);
            Assert.Equal(T, fieldType1.TypeArguments()[0]);

            var field2 = members[2] as FieldSymbol;
            Assert.NotNull(field2); Debug.Assert(field2 is { });
            Assert.Equal("<2>__Target2", field2.Name);

            var fieldType2 = field2.Type as NamedTypeSymbol;
            Assert.NotNull(fieldType2); Debug.Assert(fieldType2 is { });
            Assert.True(fieldType2.IsDelegateType());
            Assert.Equal("System", fieldType2.ContainingNamespace.Name);
            Assert.Equal("Action", fieldType2.Name);
            Assert.Equal(1, fieldType2.Arity);
            Assert.Equal(T, fieldType2.TypeArguments()[0]);

            var field3 = members[3] as FieldSymbol;
            Assert.NotNull(field3); Debug.Assert(field3 is { });
            Assert.Equal("<3>__Target3", field3.Name);

            var fieldType3 = field3.Type as NamedTypeSymbol;
            Assert.NotNull(fieldType3); Debug.Assert(fieldType3 is { });
            Assert.True(fieldType3.IsDelegateType());
            Assert.Equal("System", fieldType3.ContainingNamespace.Name);
            Assert.Equal("Action", fieldType3.Name);
            Assert.Equal(2, fieldType3.Arity);
            Assert.Equal(T, fieldType3.TypeArguments()[0]);
            Assert.Equal(V, fieldType3.TypeArguments()[1]);

            var field4 = members[4] as FieldSymbol;
            Assert.NotNull(field4); Debug.Assert(field4 is { });
            Assert.Equal("<4>__Target4", field4.Name);

            var fieldType4 = field4.Type as NamedTypeSymbol;
            Assert.NotNull(fieldType4); Debug.Assert(fieldType4 is { });
            Assert.True(fieldType4.IsDelegateType());
            Assert.Equal("System", fieldType4.ContainingNamespace.Name);
            Assert.Equal("Action", fieldType4.Name);
            Assert.Equal(2, fieldType4.Arity);
            Assert.Equal(T, fieldType4.TypeArguments()[0]);
            Assert.Equal(V, fieldType4.TypeArguments()[1]);

            var field5 = members[5] as FieldSymbol;
            Assert.NotNull(field5); Debug.Assert(field5 is { });
            Assert.Equal("<5>__Target5", field5.Name);

            var fieldType5 = field5.Type as NamedTypeSymbol;
            Assert.NotNull(fieldType5); Debug.Assert(fieldType5 is { });
            Assert.True(fieldType5.IsDelegateType());
            Assert.Equal("System", fieldType5.ContainingNamespace.Name);
            Assert.Equal("Action", fieldType5.Name);
            Assert.Equal(1, fieldType5.Arity);
            Assert.Equal(T, fieldType5.TypeArguments()[0]);

            var field6 = members[6] as FieldSymbol;
            Assert.NotNull(field6); Debug.Assert(field6 is { });
            Assert.Equal("<6>__Target5", field6.Name);

            var fieldType6 = field6.Type as NamedTypeSymbol;
            Assert.NotNull(fieldType6); Debug.Assert(fieldType6 is { });
            Assert.True(fieldType6.IsDelegateType());
            Assert.Equal("System", fieldType6.ContainingNamespace.Name);
            Assert.Equal("Action", fieldType6.Name);
            Assert.Equal(1, fieldType6.Arity);
            Assert.Equal(V, fieldType6.TypeArguments()[0]);
        }
        CompileAndVerify(source, symbolValidator: containerValidator);
    }

    [Fact]
    public void ContainersCanBeShared_MethodScoped0()
    {
        var source = @"
using System;
class C
{
    void Test<T>()
    {
        Action    a = Target0<T>;
        Action<T> b = Target1;
        Action    c = D<T>.Target2;
        Action<C> d = E.Target3<T>;
    }

    static void Target0<T>() { }
    static void Target1<T>(T t) { }

    class D<K>
    {
        public static void Target2() { }
    }
}
static class E
{
    public static void Target3<V>(this C c) { }
}
";
        static void containerValidator(ModuleSymbol module)
        {
            var testClass = module.GlobalNamespace.GetTypeMember("C");
            var container = testClass.GetTypeMember("<Test>O__0_0");
            Assert.NotNull(container); Debug.Assert(container is { });

            Assert.Equal(1, container.Arity);
            var T = container.TypeParameters[0];

            var members = container.GetMembers();
            Assert.Equal(4, members.Length);


            var field0 = members[0] as FieldSymbol;
            Assert.NotNull(field0); Debug.Assert(field0 is { });
            Assert.Equal("<0>__Target0", field0.Name);

            var fieldType0 = field0.Type as NamedTypeSymbol;
            Assert.NotNull(fieldType0); Debug.Assert(fieldType0 is { });
            Assert.True(fieldType0.IsDelegateType());
            Assert.Equal("System", fieldType0.ContainingNamespace.Name);
            Assert.Equal("Action", fieldType0.Name);
            Assert.Equal(0, fieldType0.Arity);

            var field1 = members[1] as FieldSymbol;
            Assert.NotNull(field1); Debug.Assert(field1 is { });
            Assert.Equal("<1>__Target1", field1.Name);

            var fieldType1 = field1.Type as NamedTypeSymbol;
            Assert.NotNull(fieldType1); Debug.Assert(fieldType1 is { });
            Assert.True(fieldType1.IsDelegateType());
            Assert.Equal("System", fieldType1.ContainingNamespace.Name);
            Assert.Equal("Action", fieldType1.Name);
            Assert.Equal(1, fieldType1.Arity);
            Assert.Equal(T, fieldType1.TypeArguments()[0]);

            var field2 = members[2] as FieldSymbol;
            Assert.NotNull(field2); Debug.Assert(field2 is { });
            Assert.Equal("<2>__Target2", field2.Name);

            var fieldType2 = field2.Type as NamedTypeSymbol;
            Assert.NotNull(fieldType2); Debug.Assert(fieldType2 is { });
            Assert.True(fieldType2.IsDelegateType());
            Assert.Equal("System", fieldType2.ContainingNamespace.Name);
            Assert.Equal("Action", fieldType2.Name);
            Assert.Equal(0, fieldType2.Arity);

            var field3 = members[3] as FieldSymbol;
            Assert.NotNull(field3); Debug.Assert(field3 is { });
            Assert.Equal("<3>__Target3", field3.Name);

            var fieldType3 = field3.Type as NamedTypeSymbol;
            Assert.NotNull(fieldType3); Debug.Assert(fieldType3 is { });
            Assert.True(fieldType3.IsDelegateType());
            Assert.Equal("System", fieldType3.ContainingNamespace.Name);
            Assert.Equal("Action", fieldType3.Name);
            Assert.Equal(1, fieldType3.Arity);
            Assert.Equal(testClass, fieldType3.TypeArguments()[0]);
        }
        CompileAndVerify(source, symbolValidator: containerValidator);
    }

    [Fact]
    public void ContainersCanBeShared_MethodScoped1()
    {
        var source = @"
using System;
class C
{
    public static void Target0<T>() { }
    public static void Target1<T>(T t) { }

    public class D<T>
    {
        public static void Target2() { }
    }
}
static class E
{
    static void Test<T>()
    {
        Action    a = C.Target0<T>;
        Action<T> b = C.Target1;
        Action    c = C.D<T>.Target2;
        Action<C> d = Target3<T>;
    }

    public static void Target3<T>(this C c) { }
}
";
        static void containerValidator(ModuleSymbol module)
        {
            var testClass = module.GlobalNamespace.GetTypeMember("E");
            var container = testClass.GetTypeMember("<Test>O__0_0");
            Assert.NotNull(container); Debug.Assert(container is { });

            Assert.Equal(1, container.Arity);
            var T = container.TypeParameters[0];
            var C = module.GlobalNamespace.GetTypeMember("C");

            var members = container.GetMembers();
            Assert.Equal(4, members.Length);


            var field0 = members[0] as FieldSymbol;
            Assert.NotNull(field0); Debug.Assert(field0 is { });
            Assert.Equal("<0>__Target0", field0.Name);

            var fieldType0 = field0.Type as NamedTypeSymbol;
            Assert.NotNull(fieldType0); Debug.Assert(fieldType0 is { });
            Assert.True(fieldType0.IsDelegateType());
            Assert.Equal("System", fieldType0.ContainingNamespace.Name);
            Assert.Equal("Action", fieldType0.Name);
            Assert.Equal(0, fieldType0.Arity);

            var field1 = members[1] as FieldSymbol;
            Assert.NotNull(field1); Debug.Assert(field1 is { });
            Assert.Equal("<1>__Target1", field1.Name);

            var fieldType1 = field1.Type as NamedTypeSymbol;
            Assert.NotNull(fieldType1); Debug.Assert(fieldType1 is { });
            Assert.True(fieldType1.IsDelegateType());
            Assert.Equal("System", fieldType1.ContainingNamespace.Name);
            Assert.Equal("Action", fieldType1.Name);
            Assert.Equal(1, fieldType1.Arity);
            Assert.Equal(T, fieldType1.TypeArguments()[0]);

            var field2 = members[2] as FieldSymbol;
            Assert.NotNull(field2); Debug.Assert(field2 is { });
            Assert.Equal("<2>__Target2", field2.Name);

            var fieldType2 = field2.Type as NamedTypeSymbol;
            Assert.NotNull(fieldType2); Debug.Assert(fieldType2 is { });
            Assert.True(fieldType2.IsDelegateType());
            Assert.Equal("System", fieldType2.ContainingNamespace.Name);
            Assert.Equal("Action", fieldType2.Name);
            Assert.Equal(0, fieldType2.Arity);

            var field3 = members[3] as FieldSymbol;
            Assert.NotNull(field3); Debug.Assert(field3 is { });
            Assert.Equal("<3>__Target3", field3.Name);

            var fieldType3 = field3.Type as NamedTypeSymbol;
            Assert.NotNull(fieldType3); Debug.Assert(fieldType3 is { });
            Assert.True(fieldType3.IsDelegateType());
            Assert.Equal("System", fieldType3.ContainingNamespace.Name);
            Assert.Equal("Action", fieldType3.Name);
            Assert.Equal(1, fieldType3.Arity);
            Assert.Equal(C, fieldType3.TypeArguments()[0]);
        }

        CompileAndVerify(source, symbolValidator: containerValidator);
    }

    [Fact]
    public void ContainersCanBeShared_SkippingUnused()
    {
        var source = @"
using System;
class C
{
    public static void Target<T>(T t) { }
}
static class E
{
    static void Test<T>()
    {
        void LF2<G>()
        {
            void LF3()
            {
                Action<T> d = C.Target<T>;
                static void LF4 () { Action<T> d = C.Target<T>; }

                LF4();
            }
                
            void LF5()
            {
                Action<T> d = C.Target<T>;
            }

            LF3(); LF5();
        }
        
        LF2<int>();
    }
}
";
        static void containerValidator(ModuleSymbol module)
        {
            var testClass = module.GlobalNamespace.GetTypeMember("E");
            var container = testClass.GetTypeMember("<Test>O__0_0");
            Assert.NotNull(container); Debug.Assert(container is { });

            Assert.Equal(1, container.Arity);

            var members = container.GetMembers();
            Assert.Equal(1, members.Length);

            var field0 = members[0] as FieldSymbol;
            Assert.NotNull(field0); Debug.Assert(field0 is { });
            Assert.Equal("<0>__Target", field0.Name);

            var fieldType0 = field0.Type as NamedTypeSymbol;
            Assert.NotNull(fieldType0); Debug.Assert(fieldType0 is { });
            Assert.True(fieldType0.IsDelegateType());
            Assert.Equal("System", fieldType0.ContainingNamespace.Name);
            Assert.Equal("Action", fieldType0.Name);
            Assert.Equal(1, fieldType0.Arity);
        }

        CompileAndVerify(source, symbolValidator: containerValidator);
    }

    [Fact]
    public void ContainersCanBeShared_LocalFunctions()
    {
        var source = @"
using System;
static class E
{
    static void Test<T>()
    {
        void Owner<G>()
        {
            void LF1()
            {
                Action d = LF2;
                static void LF2() { }

                LF2();
            }
                
            void LF3()
            {
                Action d = LF2;
                static void LF2() { }

                LF2();
            }

            LF1(); LF3();
        }
        
        Owner<int>();
    }
}
";
        static void containerValidator(ModuleSymbol module)
        {
            var testClass = module.GlobalNamespace.GetTypeMember("E");
            var container = testClass.GetTypeMember("<Owner>O__0_0");
            Assert.NotNull(container); Debug.Assert(container is { });

            Assert.Equal(2, container.Arity);

            var members = container.GetMembers();
            Assert.Equal(2, members.Length);

            var field0 = members[0] as FieldSymbol;
            Assert.NotNull(field0); Debug.Assert(field0 is { });
            Assert.Equal("<0>__LF2", field0.Name);

            var fieldType0 = field0.Type as NamedTypeSymbol;
            Assert.NotNull(fieldType0); Debug.Assert(fieldType0 is { });
            Assert.True(fieldType0.IsDelegateType());
            Assert.Equal("System", fieldType0.ContainingNamespace.Name);
            Assert.Equal("Action", fieldType0.Name);
            Assert.Equal(0, fieldType0.Arity);

            var field1 = members[1] as FieldSymbol;
            Assert.NotNull(field1); Debug.Assert(field1 is { });
            Assert.Equal("<1>__LF2", field1.Name);

            var fieldType1 = field1.Type as NamedTypeSymbol;
            Assert.NotNull(fieldType1); Debug.Assert(fieldType1 is { });
            Assert.True(fieldType1.IsDelegateType());
            Assert.Equal("System", fieldType1.ContainingNamespace.Name);
            Assert.Equal("Action", fieldType1.Name);
            Assert.Equal(0, fieldType1.Arity);
        }

        CompileAndVerify(source, symbolValidator: containerValidator);
    }

    [Fact]
    public void EventHandlers_TypeScoped_CouldBeModuleScoped0()
    {
        var source = @"
using System;
using System.Reflection;
class C
{
    void Test()
    {
        AppDomain.CurrentDomain.AssemblyResolve += Target;
    }

    static Assembly Target(object sender, ResolveEventArgs e) => null;
}
";
        CompileAndVerify(source).VerifyIL("C.Test", @"
{
  // Code size       38 (0x26)
  .maxstack  3
  IL_0000:  call       ""System.AppDomain System.AppDomain.CurrentDomain.get""
  IL_0005:  ldsfld     ""System.ResolveEventHandler C.<>O.<0>__Target""
  IL_000a:  dup
  IL_000b:  brtrue.s   IL_0020
  IL_000d:  pop
  IL_000e:  ldnull
  IL_000f:  ldftn      ""System.Reflection.Assembly C.Target(object, System.ResolveEventArgs)""
  IL_0015:  newobj     ""System.ResolveEventHandler..ctor(object, System.IntPtr)""
  IL_001a:  dup
  IL_001b:  stsfld     ""System.ResolveEventHandler C.<>O.<0>__Target""
  IL_0020:  callvirt   ""void System.AppDomain.AssemblyResolve.add""
  IL_0025:  ret
}
");
    }

    [Fact]
    public void EventHandlers_TypeScoped_CouldBeModuleScoped1()
    {
        var source = @"
using System;
class MyEventArgs : EventArgs { }
class C<TEventArgs> where TEventArgs : EventArgs
{
    event EventHandler<TEventArgs> SomethingHappened;
 
    void Test()
    {
        var c = new C<MyEventArgs>();
        c.SomethingHappened += D.Target;
    }

}
class D
{
    public static void Target(object sender, MyEventArgs e) { }
}
";
        CompileAndVerify(source).VerifyIL("C<TEventArgs>.Test", @"
{
  // Code size       38 (0x26)
  .maxstack  3
  IL_0000:  newobj     ""C<MyEventArgs>..ctor()""
  IL_0005:  ldsfld     ""System.EventHandler<MyEventArgs> C<TEventArgs>.<>O.<0>__Target""
  IL_000a:  dup
  IL_000b:  brtrue.s   IL_0020
  IL_000d:  pop
  IL_000e:  ldnull
  IL_000f:  ldftn      ""void D.Target(object, MyEventArgs)""
  IL_0015:  newobj     ""System.EventHandler<MyEventArgs>..ctor(object, System.IntPtr)""
  IL_001a:  dup
  IL_001b:  stsfld     ""System.EventHandler<MyEventArgs> C<TEventArgs>.<>O.<0>__Target""
  IL_0020:  callvirt   ""void C<MyEventArgs>.SomethingHappened.add""
  IL_0025:  ret
}
");
    }

    [Fact]
    public void EventHandlers_TypeScoped0()
    {
        var source = @"
using System;
class C<TEventArgs> where TEventArgs : EventArgs
{
    event EventHandler<TEventArgs> SomethingHappened;
 
    void Test()
    {
        this.SomethingHappened += Target;
    }

    static void Target(object sender, TEventArgs e) { }
}
";
        CompileAndVerify(source).VerifyIL("C<TEventArgs>.Test", @"
{
  // Code size       34 (0x22)
  .maxstack  3
  IL_0000:  ldarg.0
  IL_0001:  ldsfld     ""System.EventHandler<TEventArgs> C<TEventArgs>.<>O.<0>__Target""
  IL_0006:  dup
  IL_0007:  brtrue.s   IL_001c
  IL_0009:  pop
  IL_000a:  ldnull
  IL_000b:  ldftn      ""void C<TEventArgs>.Target(object, TEventArgs)""
  IL_0011:  newobj     ""System.EventHandler<TEventArgs>..ctor(object, System.IntPtr)""
  IL_0016:  dup
  IL_0017:  stsfld     ""System.EventHandler<TEventArgs> C<TEventArgs>.<>O.<0>__Target""
  IL_001c:  call       ""void C<TEventArgs>.SomethingHappened.add""
  IL_0021:  ret
}
");
    }

    [Fact]
    public void EventHandlers_MethodScoped0()
    {
        var source = @"
using System;
class C
{
    void Test<TEventArgs>() where TEventArgs : EventArgs
    {
        var d = new D<TEventArgs>();
        d.SomethingHappened += Target;
    }

    static void Target<TEventArgs>(object sender, TEventArgs e) where TEventArgs : EventArgs { }
}
class D<TEventArgs> where TEventArgs : EventArgs
{
    public event EventHandler<TEventArgs> SomethingHappened;
}
";
        CompileAndVerify(source).VerifyIL("C.Test<TEventArgs>", @"
{
  // Code size       38 (0x26)
  .maxstack  3
  IL_0000:  newobj     ""D<TEventArgs>..ctor()""
  IL_0005:  ldsfld     ""System.EventHandler<TEventArgs> C.<Test>O__0_0<TEventArgs>.<0>__Target""
  IL_000a:  dup
  IL_000b:  brtrue.s   IL_0020
  IL_000d:  pop
  IL_000e:  ldnull
  IL_000f:  ldftn      ""void C.Target<TEventArgs>(object, TEventArgs)""
  IL_0015:  newobj     ""System.EventHandler<TEventArgs>..ctor(object, System.IntPtr)""
  IL_001a:  dup
  IL_001b:  stsfld     ""System.EventHandler<TEventArgs> C.<Test>O__0_0<TEventArgs>.<0>__Target""
  IL_0020:  callvirt   ""void D<TEventArgs>.SomethingHappened.add""
  IL_0025:  ret
}
");
    }

    [Fact]
    public void AnonymousTypes_TypeScoped_CouldBeModuleScoped0()
    {
        var source = @"
using System;
class C
{
    static void Main(string[] args) => Invoke(new { x = 0 }, Target);

    static void Invoke<T>(T t, Action<T> f) => f(t);

    static void Target<T>(T t) => Console.WriteLine(t);
}
";
        CompileAndVerify(source, expectedOutput: "{ x = 0 }").VerifyIL("C.Main", @"
{
  // Code size       39 (0x27)
  .maxstack  3
  IL_0000:  ldc.i4.0
  IL_0001:  newobj     ""<>f__AnonymousType0<int>..ctor(int)""
  IL_0006:  ldsfld     ""System.Action<<anonymous type: int x>> C.<>O.<0>__Target""
  IL_000b:  dup
  IL_000c:  brtrue.s   IL_0021
  IL_000e:  pop
  IL_000f:  ldnull
  IL_0010:  ldftn      ""void C.Target<<anonymous type: int x>>(<anonymous type: int x>)""
  IL_0016:  newobj     ""System.Action<<anonymous type: int x>>..ctor(object, System.IntPtr)""
  IL_001b:  dup
  IL_001c:  stsfld     ""System.Action<<anonymous type: int x>> C.<>O.<0>__Target""
  IL_0021:  call       ""void C.Invoke<<anonymous type: int x>>(<anonymous type: int x>, System.Action<<anonymous type: int x>>)""
  IL_0026:  ret
}
");
    }

    [Fact]
    public void AnonymousTypes_TypeScoped0()
    {
        var source = @"
using System;
class C
{
    static void Main(string[] args) => (new D<int>()).Test(0);
}
class D<G>
{
    public void Test(G g) => Invoke(new { x = g }, Target);

    static void Invoke<T>(T t, Action<T> f) => f(t);

    static void Target<T>(T t) => Console.WriteLine(t);
}
";
        CompileAndVerify(source, expectedOutput: "{ x = 0 }").VerifyIL("D<G>.Test", @"
{
  // Code size       39 (0x27)
  .maxstack  3
  IL_0000:  ldarg.1
  IL_0001:  newobj     ""<>f__AnonymousType0<G>..ctor(G)""
  IL_0006:  ldsfld     ""System.Action<<anonymous type: G x>> D<G>.<>O.<0>__Target""
  IL_000b:  dup
  IL_000c:  brtrue.s   IL_0021
  IL_000e:  pop
  IL_000f:  ldnull
  IL_0010:  ldftn      ""void D<G>.Target<<anonymous type: G x>>(<anonymous type: G x>)""
  IL_0016:  newobj     ""System.Action<<anonymous type: G x>>..ctor(object, System.IntPtr)""
  IL_001b:  dup
  IL_001c:  stsfld     ""System.Action<<anonymous type: G x>> D<G>.<>O.<0>__Target""
  IL_0021:  call       ""void D<G>.Invoke<<anonymous type: G x>>(<anonymous type: G x>, System.Action<<anonymous type: G x>>)""
  IL_0026:  ret
}
");
    }

    [Fact]
    public void AnonymousTypes_TypeScoped1()
    {
        var source = @"
using System;
class C
{
    static void Main(string[] args) => (new D<int>()).Test(0);
}
class D<G>
{
    public void Test(G g) => Invoke(new { x = g }, E.Target);

    static void Invoke<T>(T t, Action<T> f) => f(t);
}
class E
{
    public static void Target<T>(T t) => Console.WriteLine(t);
}
";
        CompileAndVerify(source, expectedOutput: "{ x = 0 }").VerifyIL("D<G>.Test", @"
{
  // Code size       39 (0x27)
  .maxstack  3
  IL_0000:  ldarg.1
  IL_0001:  newobj     ""<>f__AnonymousType0<G>..ctor(G)""
  IL_0006:  ldsfld     ""System.Action<<anonymous type: G x>> D<G>.<>O.<0>__Target""
  IL_000b:  dup
  IL_000c:  brtrue.s   IL_0021
  IL_000e:  pop
  IL_000f:  ldnull
  IL_0010:  ldftn      ""void E.Target<<anonymous type: G x>>(<anonymous type: G x>)""
  IL_0016:  newobj     ""System.Action<<anonymous type: G x>>..ctor(object, System.IntPtr)""
  IL_001b:  dup
  IL_001c:  stsfld     ""System.Action<<anonymous type: G x>> D<G>.<>O.<0>__Target""
  IL_0021:  call       ""void D<G>.Invoke<<anonymous type: G x>>(<anonymous type: G x>, System.Action<<anonymous type: G x>>)""
  IL_0026:  ret
}
");
    }

    [Fact]
    public void AnonymousTypes_TypeScoped2()
    {
        var source = @"
using System;
class C
{
    static void Main(string[] args) => D<int>.Test();
}
class D<G>
{
    delegate void MyAction<T>(T t);

    public static void Test() => Invoke(new { x = 0 }, Target);

    static void Invoke<T>(T t, MyAction<T> f) => f(t);

    static void Target<T>(T t) => Console.WriteLine(t);
}
";
        CompileAndVerify(source, expectedOutput: "{ x = 0 }").VerifyIL("D<G>.Test", @"
{
  // Code size       39 (0x27)
  .maxstack  3
  IL_0000:  ldc.i4.0
  IL_0001:  newobj     ""<>f__AnonymousType0<int>..ctor(int)""
  IL_0006:  ldsfld     ""D<G>.MyAction<<anonymous type: int x>> D<G>.<>O.<0>__Target""
  IL_000b:  dup
  IL_000c:  brtrue.s   IL_0021
  IL_000e:  pop
  IL_000f:  ldnull
  IL_0010:  ldftn      ""void D<G>.Target<<anonymous type: int x>>(<anonymous type: int x>)""
  IL_0016:  newobj     ""D<G>.MyAction<<anonymous type: int x>>..ctor(object, System.IntPtr)""
  IL_001b:  dup
  IL_001c:  stsfld     ""D<G>.MyAction<<anonymous type: int x>> D<G>.<>O.<0>__Target""
  IL_0021:  call       ""void D<G>.Invoke<<anonymous type: int x>>(<anonymous type: int x>, D<G>.MyAction<<anonymous type: int x>>)""
  IL_0026:  ret
}
");
    }

    [Fact]
    public void AnonymousTypes_MethodScoped0()
    {
        var source = @"
using System;
class C
{
    static void Main(string[] args) => D.Test(0);
}
class D
{
    public static void Test<T>(T t) => Invoke(new { x = t }, Target);

    static void Invoke<T>(T t, Action<T> f) => f(t);

    static void Target<T>(T t) => Console.WriteLine(t);
}
";
        CompileAndVerify(source, expectedOutput: "{ x = 0 }").VerifyIL("D.Test<T>", @"
{
  // Code size       39 (0x27)
  .maxstack  3
  IL_0000:  ldarg.0
  IL_0001:  newobj     ""<>f__AnonymousType0<T>..ctor(T)""
  IL_0006:  ldsfld     ""System.Action<<anonymous type: T x>> D.<Test>O__0_0<T>.<0>__Target""
  IL_000b:  dup
  IL_000c:  brtrue.s   IL_0021
  IL_000e:  pop
  IL_000f:  ldnull
  IL_0010:  ldftn      ""void D.Target<<anonymous type: T x>>(<anonymous type: T x>)""
  IL_0016:  newobj     ""System.Action<<anonymous type: T x>>..ctor(object, System.IntPtr)""
  IL_001b:  dup
  IL_001c:  stsfld     ""System.Action<<anonymous type: T x>> D.<Test>O__0_0<T>.<0>__Target""
  IL_0021:  call       ""void D.Invoke<<anonymous type: T x>>(<anonymous type: T x>, System.Action<<anonymous type: T x>>)""
  IL_0026:  ret
}
");
    }

    [Fact]
    public void AnonymousTypes_MethodScoped1()
    {
        var source = @"
using System;
class C
{
    static void Main(string[] args) => D.Test(0);
}
class D
{
    public static void Test<T>(T t) => Invoke(t, new { x = 0 }, Target);

    static void Invoke<T, V>(T t, V v, Action<T, V> f) => f(t, v);

    static void Target<T, V>(T t, V v) => Console.WriteLine(v);
}
";
        CompileAndVerify(source, expectedOutput: "{ x = 0 }").VerifyIL("D.Test<T>", @"
{
  // Code size       40 (0x28)
  .maxstack  4
  IL_0000:  ldarg.0
  IL_0001:  ldc.i4.0
  IL_0002:  newobj     ""<>f__AnonymousType0<int>..ctor(int)""
  IL_0007:  ldsfld     ""System.Action<T, <anonymous type: int x>> D.<Test>O__0_0<T>.<0>__Target""
  IL_000c:  dup
  IL_000d:  brtrue.s   IL_0022
  IL_000f:  pop
  IL_0010:  ldnull
  IL_0011:  ldftn      ""void D.Target<T, <anonymous type: int x>>(T, <anonymous type: int x>)""
  IL_0017:  newobj     ""System.Action<T, <anonymous type: int x>>..ctor(object, System.IntPtr)""
  IL_001c:  dup
  IL_001d:  stsfld     ""System.Action<T, <anonymous type: int x>> D.<Test>O__0_0<T>.<0>__Target""
  IL_0022:  call       ""void D.Invoke<T, <anonymous type: int x>>(T, <anonymous type: int x>, System.Action<T, <anonymous type: int x>>)""
  IL_0027:  ret
}
");
    }

    [Fact]
    public void AnonymousClass_AnonymousDelegate0()
    {
        var source = @"
using System;
class D
{
    public void Owner<T>()
    {
        static void Test<NotUsed>(T t)
        {
            var f = F<T>;
            var a = new { x = f };

            Invoke(a, Target);
        }
    }

    static void F<T>(ref T t) { }

    static void Invoke<T>(T t, Action<T> f) { }

    static void Target<T>(T t) { }
}
";
        CompileAndVerify(source).VerifyIL("D.<Owner>g__Test|0_0<T, NotUsed>", @"
{
  // Code size       65 (0x41)
  .maxstack  3
  IL_0000:  ldsfld     ""<anonymous delegate> D.<Owner>O__0_0<T>.<0>__F""
  IL_0005:  dup
  IL_0006:  brtrue.s   IL_001b
  IL_0008:  pop
  IL_0009:  ldnull
  IL_000a:  ldftn      ""void D.F<T>(ref T)""
  IL_0010:  newobj     ""<>A{00000001}<T>..ctor(object, System.IntPtr)""
  IL_0015:  dup
  IL_0016:  stsfld     ""<anonymous delegate> D.<Owner>O__0_0<T>.<0>__F""
  IL_001b:  newobj     ""<>f__AnonymousType0<<anonymous delegate>>..ctor(<anonymous delegate>)""
  IL_0020:  ldsfld     ""System.Action<<anonymous type: <anonymous delegate> x>> D.<Owner>O__0_0<T>.<1>__Target""
  IL_0025:  dup
  IL_0026:  brtrue.s   IL_003b
  IL_0028:  pop
  IL_0029:  ldnull
  IL_002a:  ldftn      ""void D.Target<<anonymous type: <anonymous delegate> x>>(<anonymous type: <anonymous delegate> x>)""
  IL_0030:  newobj     ""System.Action<<anonymous type: <anonymous delegate> x>>..ctor(object, System.IntPtr)""
  IL_0035:  dup
  IL_0036:  stsfld     ""System.Action<<anonymous type: <anonymous delegate> x>> D.<Owner>O__0_0<T>.<1>__Target""
  IL_003b:  call       ""void D.Invoke<<anonymous type: <anonymous delegate> x>>(<anonymous type: <anonymous delegate> x>, System.Action<<anonymous type: <anonymous delegate> x>>)""
  IL_0040:  ret
}
");
    }

    [Fact]
    public void AnonymousClass_AnonymousDelegate1()
    {
        var source = @"
using System;
class D<T>
{
    public void Top<N0>()
    {
        static void Test<N1>(T t)
        {
            var f = F;
            var a = new { x = f };

            Invoke(a, Target);
        }
    }

    static void F(ref T t) { }

    static void Invoke<G>(G t, Action<G> f) { }

    static void Target<G>(G t) { }
}
";
        CompileAndVerify(source).VerifyIL("D<T>.<Top>g__Test|0_0<N0, N1>", @"
{
  // Code size       65 (0x41)
  .maxstack  3
  IL_0000:  ldsfld     ""<anonymous delegate> D<T>.<>O.<0>__F""
  IL_0005:  dup
  IL_0006:  brtrue.s   IL_001b
  IL_0008:  pop
  IL_0009:  ldnull
  IL_000a:  ldftn      ""void D<T>.F(ref T)""
  IL_0010:  newobj     ""<>A{00000001}<T>..ctor(object, System.IntPtr)""
  IL_0015:  dup
  IL_0016:  stsfld     ""<anonymous delegate> D<T>.<>O.<0>__F""
  IL_001b:  newobj     ""<>f__AnonymousType0<<anonymous delegate>>..ctor(<anonymous delegate>)""
  IL_0020:  ldsfld     ""System.Action<<anonymous type: <anonymous delegate> x>> D<T>.<>O.<1>__Target""
  IL_0025:  dup
  IL_0026:  brtrue.s   IL_003b
  IL_0028:  pop
  IL_0029:  ldnull
  IL_002a:  ldftn      ""void D<T>.Target<<anonymous type: <anonymous delegate> x>>(<anonymous type: <anonymous delegate> x>)""
  IL_0030:  newobj     ""System.Action<<anonymous type: <anonymous delegate> x>>..ctor(object, System.IntPtr)""
  IL_0035:  dup
  IL_0036:  stsfld     ""System.Action<<anonymous type: <anonymous delegate> x>> D<T>.<>O.<1>__Target""
  IL_003b:  call       ""void D<T>.Invoke<<anonymous type: <anonymous delegate> x>>(<anonymous type: <anonymous delegate> x>, System.Action<<anonymous type: <anonymous delegate> x>>)""
  IL_0040:  ret
}
");
    }

    [Fact]
    public void Pointer_TypeScoped_CouldBeModuleScoped0()
    {
        var source = @"
using System;
class C
{
    unsafe void Test()
    {
        Func<int*[]> t = Target;
        t();
    }

    unsafe static int*[] Target() => default(int*[]);
}
";
        CompileAndVerify(source, options: TestOptions.UnsafeReleaseDll).VerifyIL("C.Test", @"
{
  // Code size       34 (0x22)
  .maxstack  2
  IL_0000:  ldsfld     ""System.Func<int*[]> C.<>O.<0>__Target""
  IL_0005:  dup
  IL_0006:  brtrue.s   IL_001b
  IL_0008:  pop
  IL_0009:  ldnull
  IL_000a:  ldftn      ""int*[] C.Target()""
  IL_0010:  newobj     ""System.Func<int*[]>..ctor(object, System.IntPtr)""
  IL_0015:  dup
  IL_0016:  stsfld     ""System.Func<int*[]> C.<>O.<0>__Target""
  IL_001b:  callvirt   ""int*[] System.Func<int*[]>.Invoke()""
  IL_0020:  pop
  IL_0021:  ret
}
");
    }

    [Fact]
    public void Pointer_TypeScoped0()
    {
        var source = @"
using System;
class C<T>
{
    unsafe void Test()
    {
        Func<T, int*[]> t = Target;
        t(default(T));
    }

    unsafe static int*[] Target(T t) => default(int*[]);
}
";
        CompileAndVerify(source, options: TestOptions.UnsafeReleaseDll).VerifyIL("C<T>.Test", @"
{
  // Code size       43 (0x2b)
  .maxstack  2
  .locals init (T V_0)
  IL_0000:  ldsfld     ""System.Func<T, int*[]> C<T>.<>O.<0>__Target""
  IL_0005:  dup
  IL_0006:  brtrue.s   IL_001b
  IL_0008:  pop
  IL_0009:  ldnull
  IL_000a:  ldftn      ""int*[] C<T>.Target(T)""
  IL_0010:  newobj     ""System.Func<T, int*[]>..ctor(object, System.IntPtr)""
  IL_0015:  dup
  IL_0016:  stsfld     ""System.Func<T, int*[]> C<T>.<>O.<0>__Target""
  IL_001b:  ldloca.s   V_0
  IL_001d:  initobj    ""T""
  IL_0023:  ldloc.0
  IL_0024:  callvirt   ""int*[] System.Func<T, int*[]>.Invoke(T)""
  IL_0029:  pop
  IL_002a:  ret
}
");
    }

    [Fact]
    public void Pointer_MethodScoped0()
    {
        var source = @"
using System;
class C
{
    unsafe void Test<T>(T t)
    {
        Func<T, int*[]> f = Target<T>;
        f(t);
    }

    unsafe static int*[] Target<G>(G g) => default(int*[]);
}
";
        CompileAndVerify(source, options: TestOptions.UnsafeReleaseDll).VerifyIL("C.Test<T>", @"
{
  // Code size       35 (0x23)
  .maxstack  2
  IL_0000:  ldsfld     ""System.Func<T, int*[]> C.<Test>O__0_0<T>.<0>__Target""
  IL_0005:  dup
  IL_0006:  brtrue.s   IL_001b
  IL_0008:  pop
  IL_0009:  ldnull
  IL_000a:  ldftn      ""int*[] C.Target<T>(T)""
  IL_0010:  newobj     ""System.Func<T, int*[]>..ctor(object, System.IntPtr)""
  IL_0015:  dup
  IL_0016:  stsfld     ""System.Func<T, int*[]> C.<Test>O__0_0<T>.<0>__Target""
  IL_001b:  ldarg.1
  IL_001c:  callvirt   ""int*[] System.Func<T, int*[]>.Invoke(T)""
  IL_0021:  pop
  IL_0022:  ret
}
");
    }

    [Fact]
    public void Dynamic_TypeScoped_CouldBeModuleScoped0()
    {
        var source = @"
using System;
class C
{
    void Test()
    {
        Func<dynamic> t = Target;
        t();
    }

    static dynamic Target() => 0;
}
";
        CompileAndVerify(source).VerifyIL("C.Test", @"
{
  // Code size       34 (0x22)
  .maxstack  2
  IL_0000:  ldsfld     ""System.Func<dynamic> C.<>O.<0>__Target""
  IL_0005:  dup
  IL_0006:  brtrue.s   IL_001b
  IL_0008:  pop
  IL_0009:  ldnull
  IL_000a:  ldftn      ""dynamic C.Target()""
  IL_0010:  newobj     ""System.Func<dynamic>..ctor(object, System.IntPtr)""
  IL_0015:  dup
  IL_0016:  stsfld     ""System.Func<dynamic> C.<>O.<0>__Target""
  IL_001b:  callvirt   ""dynamic System.Func<dynamic>.Invoke()""
  IL_0020:  pop
  IL_0021:  ret
}
");
    }

    [Fact]
    public void Dynamic_TypeScoped0()
    {
        var source = @"
using System;
class C<T>
{
    void Test()
    {
        Func<T, dynamic> t = Target;
        t(default(T));
    }

    static dynamic Target(T t) => 0;
}
";
        CompileAndVerify(source).VerifyIL("C<T>.Test", @"
{
  // Code size       43 (0x2b)
  .maxstack  2
  .locals init (T V_0)
  IL_0000:  ldsfld     ""System.Func<T, dynamic> C<T>.<>O.<0>__Target""
  IL_0005:  dup
  IL_0006:  brtrue.s   IL_001b
  IL_0008:  pop
  IL_0009:  ldnull
  IL_000a:  ldftn      ""dynamic C<T>.Target(T)""
  IL_0010:  newobj     ""System.Func<T, dynamic>..ctor(object, System.IntPtr)""
  IL_0015:  dup
  IL_0016:  stsfld     ""System.Func<T, dynamic> C<T>.<>O.<0>__Target""
  IL_001b:  ldloca.s   V_0
  IL_001d:  initobj    ""T""
  IL_0023:  ldloc.0
  IL_0024:  callvirt   ""dynamic System.Func<T, dynamic>.Invoke(T)""
  IL_0029:  pop
  IL_002a:  ret
}
");
    }

    [Fact]
    public void Dynamic_MethodScoped0()
    {
        var source = @"
using System;
class C
{
    void Test<T>(T t)
    {
        Func<T, dynamic> f = Target<T>;
        f(t);
    }

    static dynamic Target<G>(G g) => 0;
}
";
        CompileAndVerify(source).VerifyIL("C.Test<T>", @"
{
  // Code size       35 (0x23)
  .maxstack  2
  IL_0000:  ldsfld     ""System.Func<T, dynamic> C.<Test>O__0_0<T>.<0>__Target""
  IL_0005:  dup
  IL_0006:  brtrue.s   IL_001b
  IL_0008:  pop
  IL_0009:  ldnull
  IL_000a:  ldftn      ""dynamic C.Target<T>(T)""
  IL_0010:  newobj     ""System.Func<T, dynamic>..ctor(object, System.IntPtr)""
  IL_0015:  dup
  IL_0016:  stsfld     ""System.Func<T, dynamic> C.<Test>O__0_0<T>.<0>__Target""
  IL_001b:  ldarg.1
  IL_001c:  callvirt   ""dynamic System.Func<T, dynamic>.Invoke(T)""
  IL_0021:  pop
  IL_0022:  ret
}
");
    }

    [Fact]
    public void SynthesizedAnonymousDelegate_TypeScoped0()
    {
        var source = @"
using System;
class C
{
    void Test<T>(T t)
    {
        G(Target<int>);
    }

    void G(Delegate d) {}

    static dynamic Target<G>(ref G g) => 0;
}
";
        CompileAndVerify(source).VerifyIL("C.Test<T>", @"
{
  // Code size       34 (0x22)
  .maxstack  3
  IL_0000:  ldarg.0
  IL_0001:  ldsfld     ""<anonymous delegate> C.<>O.<0>__Target""
  IL_0006:  dup
  IL_0007:  brtrue.s   IL_001c
  IL_0009:  pop
  IL_000a:  ldnull
  IL_000b:  ldftn      ""dynamic C.Target<int>(ref int)""
  IL_0011:  newobj     ""<>F{00000001}<int, dynamic>..ctor(object, System.IntPtr)""
  IL_0016:  dup
  IL_0017:  stsfld     ""<anonymous delegate> C.<>O.<0>__Target""
  IL_001c:  call       ""void C.G(System.Delegate)""
  IL_0021:  ret
}
");
    }

    [Fact]
    public void SynthesizedAnonymousDelegate_MethodScoped0()
    {
        var source = @"
using System;
class C
{
    void Test<T>(T t)
    {
        G(Target<T>);
    }

    void G(Delegate d) {}

    static dynamic Target<G>(ref G g) => 0;
}
";
        CompileAndVerify(source).VerifyIL("C.Test<T>", @"
{
  // Code size       34 (0x22)
  .maxstack  3
  IL_0000:  ldarg.0
  IL_0001:  ldsfld     ""<anonymous delegate> C.<Test>O__0_0<T>.<0>__Target""
  IL_0006:  dup
  IL_0007:  brtrue.s   IL_001c
  IL_0009:  pop
  IL_000a:  ldnull
  IL_000b:  ldftn      ""dynamic C.Target<T>(ref T)""
  IL_0011:  newobj     ""<>F{00000001}<T, dynamic>..ctor(object, System.IntPtr)""
  IL_0016:  dup
  IL_0017:  stsfld     ""<anonymous delegate> C.<Test>O__0_0<T>.<0>__Target""
  IL_001c:  call       ""void C.G(System.Delegate)""
  IL_0021:  ret
}
");
    }

    [Fact]
    public void TopLevel_LocalFunctions_TypeScoped0()
    {
        var source = @"
using System;
class C
{
    void Test(int t)
    {
        Func<int, dynamic> f = Target<int>;
        f(t);

        static dynamic Target<G>(G g) => 0;
    }
}
";
        CompileAndVerify(source).VerifyIL("C.Test", @"
{
  // Code size       35 (0x23)
  .maxstack  2
  IL_0000:  ldsfld     ""System.Func<int, dynamic> C.<>O.<0>__Target""
  IL_0005:  dup
  IL_0006:  brtrue.s   IL_001b
  IL_0008:  pop
  IL_0009:  ldnull
  IL_000a:  ldftn      ""dynamic C.<Test>g__Target|0_0<int>(int)""
  IL_0010:  newobj     ""System.Func<int, dynamic>..ctor(object, System.IntPtr)""
  IL_0015:  dup
  IL_0016:  stsfld     ""System.Func<int, dynamic> C.<>O.<0>__Target""
  IL_001b:  ldarg.1
  IL_001c:  callvirt   ""dynamic System.Func<int, dynamic>.Invoke(int)""
  IL_0021:  pop
  IL_0022:  ret
}
");
    }

    [Fact]
    public void TopLevel_LocalFunctions_NotStatic()
    {
        var source = @"
using System;
class C
{
    void Test(int t)
    {
        Func<int, dynamic> f = Target<int>;
        f(t);

        dynamic Target<G>(G g) => 0;
    }
}
";
        CompileAndVerify(source).VerifyIL("C.Test", @"
{
  // Code size       20 (0x14)
  .maxstack  2
  IL_0000:  ldnull
  IL_0001:  ldftn      ""dynamic C.<Test>g__Target|0_0<int>(int)""
  IL_0007:  newobj     ""System.Func<int, dynamic>..ctor(object, System.IntPtr)""
  IL_000c:  ldarg.1
  IL_000d:  callvirt   ""dynamic System.Func<int, dynamic>.Invoke(int)""
  IL_0012:  pop
  IL_0013:  ret
}
");
    }

    [Fact]
    public void TopLevel_LocalFunctions_MethodScoped0()
    {
        var source = @"
using System;
class C
{
    void Test<T>(T t)
    {
        Func<T, dynamic> f = Target<T>;
        f(t);

        static dynamic Target<G>(G g) => 0;
    }
}
";
        CompileAndVerify(source).VerifyIL("C.Test<T>", @"
{
  // Code size       35 (0x23)
  .maxstack  2
  IL_0000:  ldsfld     ""System.Func<T, dynamic> C.<Test>O__0_0<T>.<0>__Target""
  IL_0005:  dup
  IL_0006:  brtrue.s   IL_001b
  IL_0008:  pop
  IL_0009:  ldnull
  IL_000a:  ldftn      ""dynamic C.<Test>g__Target|0_0<T, T>(T)""
  IL_0010:  newobj     ""System.Func<T, dynamic>..ctor(object, System.IntPtr)""
  IL_0015:  dup
  IL_0016:  stsfld     ""System.Func<T, dynamic> C.<Test>O__0_0<T>.<0>__Target""
  IL_001b:  ldarg.1
  IL_001c:  callvirt   ""dynamic System.Func<T, dynamic>.Invoke(T)""
  IL_0021:  pop
  IL_0022:  ret
}
");
    }

    [Fact]
    public void Local_LocalFunctions_MethodScoped0()
    {
        var source = @"
using System;
class C
{
    void TopLevel<T>(T t)
    {
        void Test()
        {
            Func<T, dynamic> f = Target<T>;
            f(t);

            static dynamic Target<G>(G g) => 0;
        }
    }
}
";
        CompileAndVerify(source).VerifyIL("C.<TopLevel>g__Test|0_0<T>", @"
{
  // Code size       40 (0x28)
  .maxstack  2
  IL_0000:  ldsfld     ""System.Func<T, dynamic> C.<TopLevel>O__0_0<T>.<0>__Target""
  IL_0005:  dup
  IL_0006:  brtrue.s   IL_001b
  IL_0008:  pop
  IL_0009:  ldnull
  IL_000a:  ldftn      ""dynamic C.<TopLevel>g__Target|0_1<T, T>(T)""
  IL_0010:  newobj     ""System.Func<T, dynamic>..ctor(object, System.IntPtr)""
  IL_0015:  dup
  IL_0016:  stsfld     ""System.Func<T, dynamic> C.<TopLevel>O__0_0<T>.<0>__Target""
  IL_001b:  ldarg.0
  IL_001c:  ldfld      ""T C.<>c__DisplayClass0_0<T>.t""
  IL_0021:  callvirt   ""dynamic System.Func<T, dynamic>.Invoke(T)""
  IL_0026:  pop
  IL_0027:  ret
}
");
    }

    [Fact]
    public void Lambda_Local_LocalFunctions_MethodScoped0()
    {
        var source = @"
using System;
class C
{
    void TopLevel<T>(T t)
    {
        Action x = () =>
        {
            void Test()
            {
                Func<T, dynamic> f = Target<T>;
                f(t);

                static dynamic Target<G>(G g) => 0;
            }

            Test();
        };

        x();
    }
}
";
        CompileAndVerify(source).VerifyIL("C.<>c__DisplayClass0_0<T>.<TopLevel>g__Test|1", @"
{
  // Code size       40 (0x28)
  .maxstack  2
  IL_0000:  ldsfld     ""System.Func<T, dynamic> C.<TopLevel>O__0_0<T>.<0>__Target""
  IL_0005:  dup
  IL_0006:  brtrue.s   IL_001b
  IL_0008:  pop
  IL_0009:  ldnull
  IL_000a:  ldftn      ""dynamic C.<TopLevel>g__Target|0_2<T, T>(T)""
  IL_0010:  newobj     ""System.Func<T, dynamic>..ctor(object, System.IntPtr)""
  IL_0015:  dup
  IL_0016:  stsfld     ""System.Func<T, dynamic> C.<TopLevel>O__0_0<T>.<0>__Target""
  IL_001b:  ldarg.0
  IL_001c:  ldfld      ""T C.<>c__DisplayClass0_0<T>.t""
  IL_0021:  callvirt   ""dynamic System.Func<T, dynamic>.Invoke(T)""
  IL_0026:  pop
  IL_0027:  ret
}
");
    }

    [Fact]
    public void Lambda_Local_LocalFunctions_MethodScoped1()
    {
        var source = @"
using System;
class C
{
    void TopLevel<T>(T t)
    {
        Action y = () =>
        {
            void Test() { /* Test method ordinals in generated names */ }
            Test();
        };
        Action x = () =>
        {
            void Test()
            {
                Func<T, dynamic> f = Target<T>;
                f(t);

                static dynamic Target<G>(G g) => 0;
            }

            Test();
        };

        x();
        y();
    }
}
";
        CompileAndVerify(source).VerifyIL("C.<>c__DisplayClass0_0<T>.<TopLevel>g__Test|3", @"
{
  // Code size       40 (0x28)
  .maxstack  2
  IL_0000:  ldsfld     ""System.Func<T, dynamic> C.<TopLevel>O__0_0<T>.<0>__Target""
  IL_0005:  dup
  IL_0006:  brtrue.s   IL_001b
  IL_0008:  pop
  IL_0009:  ldnull
  IL_000a:  ldftn      ""dynamic C.<TopLevel>g__Target|0_4<T, T>(T)""
  IL_0010:  newobj     ""System.Func<T, dynamic>..ctor(object, System.IntPtr)""
  IL_0015:  dup
  IL_0016:  stsfld     ""System.Func<T, dynamic> C.<TopLevel>O__0_0<T>.<0>__Target""
  IL_001b:  ldarg.0
  IL_001c:  ldfld      ""T C.<>c__DisplayClass0_0<T>.t""
  IL_0021:  callvirt   ""dynamic System.Func<T, dynamic>.Invoke(T)""
  IL_0026:  pop
  IL_0027:  ret
}
");
    }

    [Fact]
    public void TopLevelStatement_Lambda_Local_LocalFunctions_MethodScoped0()
    {
        var source = @"
using System;

Action y = () =>
{
    void Test() { /* Test method ordinals in generated names */ }
    Test();
};
Action x = () =>
{
    void Test<T>(T t)
    {
        Func<T, dynamic> f = Target<T>;
        f(t);

        static dynamic Target<G>(G g) => 0;
    }

    Test(0);
};

x();
y();
";
        CompileAndVerify(source).VerifyIL("Program.<<Main>$>g__Test|0_3<T>", @"
{
  // Code size       35 (0x23)
  .maxstack  2
  IL_0000:  ldsfld     ""System.Func<T, dynamic> Program.<Test>O__0_0<T>.<0>__Target""
  IL_0005:  dup
  IL_0006:  brtrue.s   IL_001b
  IL_0008:  pop
  IL_0009:  ldnull
  IL_000a:  ldftn      ""dynamic Program.<<Main>$>g__Target|0_4<T, T>(T)""
  IL_0010:  newobj     ""System.Func<T, dynamic>..ctor(object, System.IntPtr)""
  IL_0015:  dup
  IL_0016:  stsfld     ""System.Func<T, dynamic> Program.<Test>O__0_0<T>.<0>__Target""
  IL_001b:  ldarg.0
  IL_001c:  callvirt   ""dynamic System.Func<T, dynamic>.Invoke(T)""
  IL_0021:  pop
  IL_0022:  ret
}
");
    }

    [Fact]
    public void TopLevelStatement_Lambda_Local_LocalFunctions_MethodScoped1()
    {
        var source = @"
var y = () =>
{
    void Test() { /* Test method ordinals in generated names */ }
    Test();
};
var x = () =>
{
    void Test<T>(T t)
    {
        var f = Target<int>;
        f(0);

        static dynamic Target<G>(G g)
        {
            T f = default;
            return f;
        }
    }

    Test(false);
};

x();
y();
";
        CompileAndVerify(source).VerifyIL("Program.<<Main>$>g__Test|0_3<T>", @"
{
  // Code size       35 (0x23)
  .maxstack  2
  IL_0000:  ldsfld     ""System.Func<int, dynamic> Program.<Test>O__0_0<T>.<0>__Target""
  IL_0005:  dup
  IL_0006:  brtrue.s   IL_001b
  IL_0008:  pop
  IL_0009:  ldnull
  IL_000a:  ldftn      ""dynamic Program.<<Main>$>g__Target|0_4<T, int>(int)""
  IL_0010:  newobj     ""System.Func<int, dynamic>..ctor(object, System.IntPtr)""
  IL_0015:  dup
  IL_0016:  stsfld     ""System.Func<int, dynamic> Program.<Test>O__0_0<T>.<0>__Target""
  IL_001b:  ldc.i4.0
  IL_001c:  callvirt   ""dynamic System.Func<int, dynamic>.Invoke(int)""
  IL_0021:  pop
  IL_0022:  ret
}
");
    }

    [Fact]
    public void TopLevelStatement_Tuples_LocalFunction_TypeScoped0()
    {
        var source = @"
(System.Action a, int _) = (Target, 0);
static void Target() { }
";
        CompileAndVerify(source).VerifyIL("<top-level-statements-entry-point>", @"
{
  // Code size       29 (0x1d)
  .maxstack  2
  IL_0000:  ldsfld     ""System.Action Program.<>O.<0>__Target""
  IL_0005:  dup
  IL_0006:  brtrue.s   IL_001b
  IL_0008:  pop
  IL_0009:  ldnull
  IL_000a:  ldftn      ""void Program.<<Main>$>g__Target|0_0()""
  IL_0010:  newobj     ""System.Action..ctor(object, System.IntPtr)""
  IL_0015:  dup
  IL_0016:  stsfld     ""System.Action Program.<>O.<0>__Target""
  IL_001b:  pop
  IL_001c:  ret
}
");
    }

    [Fact]
    public void TopLevelStatement_Tuples_LocalFunction_TypeScoped1()
    {
        var source = @"
var t = Target;
static (int a, int b) Target() => (0, 0);
";
        CompileAndVerify(source).VerifyIL("<top-level-statements-entry-point>", @"
{
  // Code size       25 (0x19)
  .maxstack  2
  IL_0000:  ldsfld     ""System.Func<System.ValueTuple<int, int>> Program.<>O.<0>__Target""
  IL_0005:  brtrue.s   IL_0018
  IL_0007:  ldnull
  IL_0008:  ldftn      ""System.ValueTuple<int, int> Program.<<Main>$>g__Target|0_0()""
  IL_000e:  newobj     ""System.Func<System.ValueTuple<int, int>>..ctor(object, System.IntPtr)""
  IL_0013:  stsfld     ""System.Func<System.ValueTuple<int, int>> Program.<>O.<0>__Target""
  IL_0018:  ret
}
");
    }

    [Fact]
    public void TopLevelStatement_Tuples_LocalFunction_MethodScoped0()
    {
        var source = @"
Test(0);
static void Test<T>(T t)
{
    (System.Action a, int _) = (Target, 0);
    static void Target() { }
}
";
        CompileAndVerify(source).VerifyIL("Program.<<Main>$>g__Test|0_0<T>", @"
{
  // Code size       29 (0x1d)
  .maxstack  2
  IL_0000:  ldsfld     ""System.Action Program.<Test>O__0_0<T>.<0>__Target""
  IL_0005:  dup
  IL_0006:  brtrue.s   IL_001b
  IL_0008:  pop
  IL_0009:  ldnull
  IL_000a:  ldftn      ""void Program.<<Main>$>g__Target|0_1<T>()""
  IL_0010:  newobj     ""System.Action..ctor(object, System.IntPtr)""
  IL_0015:  dup
  IL_0016:  stsfld     ""System.Action Program.<Test>O__0_0<T>.<0>__Target""
  IL_001b:  pop
  IL_001c:  ret
}
");
    }

    [Fact]
    public void TopLevelStatement_Tuples_LocalFunction_MethodScoped1()
    {
        var source = @"
Test(0);
static void Test<T>(T t)
{
    (System.Func<(T, int)> a, int _) = (Target, 0);
    static (T, int) Target() => (default(T), 0);
}
";
        CompileAndVerify(source).VerifyIL("Program.<<Main>$>g__Test|0_0<T>", @"
{
  // Code size       29 (0x1d)
  .maxstack  2
  IL_0000:  ldsfld     ""System.Func<System.ValueTuple<T, int>> Program.<Test>O__0_0<T>.<0>__Target""
  IL_0005:  dup
  IL_0006:  brtrue.s   IL_001b
  IL_0008:  pop
  IL_0009:  ldnull
  IL_000a:  ldftn      ""System.ValueTuple<T, int> Program.<<Main>$>g__Target|0_1<T>()""
  IL_0010:  newobj     ""System.Func<System.ValueTuple<T, int>>..ctor(object, System.IntPtr)""
  IL_0015:  dup
  IL_0016:  stsfld     ""System.Func<System.ValueTuple<T, int>> Program.<Test>O__0_0<T>.<0>__Target""
  IL_001b:  pop
  IL_001c:  ret
}
");
    }

    [Fact]
    public void TopLevelStatement_Tuples_LocalFunction_MethodScoped2()
    {
        var source = @"
Test(0);
static void Test<T>(T t)
{
    (System.Func<T, (T, T)> a, int _) = (Target<T>, 0);
    static (T, G) Target<G>(G g) => (default(T), g);
}
";
        CompileAndVerify(source).VerifyIL("Program.<<Main>$>g__Test|0_0<T>", @"
{
  // Code size       29 (0x1d)
  .maxstack  2
  IL_0000:  ldsfld     ""System.Func<T, System.ValueTuple<T, T>> Program.<Test>O__0_0<T>.<0>__Target""
  IL_0005:  dup
  IL_0006:  brtrue.s   IL_001b
  IL_0008:  pop
  IL_0009:  ldnull
  IL_000a:  ldftn      ""System.ValueTuple<T, T> Program.<<Main>$>g__Target|0_1<T, T>(T)""
  IL_0010:  newobj     ""System.Func<T, System.ValueTuple<T, T>>..ctor(object, System.IntPtr)""
  IL_0015:  dup
  IL_0016:  stsfld     ""System.Func<T, System.ValueTuple<T, T>> Program.<Test>O__0_0<T>.<0>__Target""
  IL_001b:  pop
  IL_001c:  ret
}
");
    }

    [Fact]
    public void Tuples_LocalFunction_TypeScoped0()
    {
        var source = @"
class C<T>
{
    void Test()
    {
        var x = Target;
        static (T, T) Target(T t) => (t, t);
    }
}
";
        CompileAndVerify(source).VerifyIL("C<T>.Test", @"
{
  // Code size       25 (0x19)
  .maxstack  2
  IL_0000:  ldsfld     ""System.Func<T, System.ValueTuple<T, T>> C<T>.<>O.<0>__Target""
  IL_0005:  brtrue.s   IL_0018
  IL_0007:  ldnull
  IL_0008:  ldftn      ""System.ValueTuple<T, T> C<T>.<Test>g__Target|0_0(T)""
  IL_000e:  newobj     ""System.Func<T, System.ValueTuple<T, T>>..ctor(object, System.IntPtr)""
  IL_0013:  stsfld     ""System.Func<T, System.ValueTuple<T, T>> C<T>.<>O.<0>__Target""
  IL_0018:  ret
}
");
    }

    [Fact]
    public void Tuples_LocalFunction_MethodScoped0()
    {
        var source = @"
class C<T>
{
    void Test<G>()
    {
        var x = Target;
        static (T, G) Target(T t, G g) => (t, g);
    }
}
";
        CompileAndVerify(source).VerifyIL("C<T>.Test<G>", @"
{
  // Code size       25 (0x19)
  .maxstack  2
  IL_0000:  ldsfld     ""System.Func<T, G, System.ValueTuple<T, G>> C<T>.<Test>O__0_0<G>.<0>__Target""
  IL_0005:  brtrue.s   IL_0018
  IL_0007:  ldnull
  IL_0008:  ldftn      ""System.ValueTuple<T, G> C<T>.<Test>g__Target|0_0<G>(T, G)""
  IL_000e:  newobj     ""System.Func<T, G, System.ValueTuple<T, G>>..ctor(object, System.IntPtr)""
  IL_0013:  stsfld     ""System.Func<T, G, System.ValueTuple<T, G>> C<T>.<Test>O__0_0<G>.<0>__Target""
  IL_0018:  ret
}
");
    }

    [Fact]
    public void Tuples_LocalFunction_MethodScoped1()
    {
        var source = @"
class C<T>
{
    void Test<G>()
    {
        var x = Target<T>;
        static (T, G, V) Target<V>(T t, G g, V v) => (t, g, v);
    }
}
";
        CompileAndVerify(source).VerifyIL("C<T>.Test<G>", @"
{
  // Code size       25 (0x19)
  .maxstack  2
  IL_0000:  ldsfld     ""System.Func<T, G, T, System.ValueTuple<T, G, T>> C<T>.<Test>O__0_0<G>.<0>__Target""
  IL_0005:  brtrue.s   IL_0018
  IL_0007:  ldnull
  IL_0008:  ldftn      ""System.ValueTuple<T, G, T> C<T>.<Test>g__Target|0_0<G, T>(T, G, T)""
  IL_000e:  newobj     ""System.Func<T, G, T, System.ValueTuple<T, G, T>>..ctor(object, System.IntPtr)""
  IL_0013:  stsfld     ""System.Func<T, G, T, System.ValueTuple<T, G, T>> C<T>.<Test>O__0_0<G>.<0>__Target""
  IL_0018:  ret
}
");
    }

    [Fact]
    public void Tuples_LocalFunction_MethodScoped2()
    {
        var source = @"
class M<N>
{
    void F<I>()
    {
        void Test<C>()
        {
            var x = Target<N>;
            static (N, I, C, E) Target<E>(N n, I i, C c, E e) => (n, i, c, e);
        }
    }
}
";
        CompileAndVerify(source).VerifyIL("M<N>.<F>g__Test|0_0<I, C>", @"
{
  // Code size       25 (0x19)
  .maxstack  2
  IL_0000:  ldsfld     ""System.Func<N, I, C, N, System.ValueTuple<N, I, C, N>> M<N>.<Test>O__0_0<I, C>.<0>__Target""
  IL_0005:  brtrue.s   IL_0018
  IL_0007:  ldnull
  IL_0008:  ldftn      ""System.ValueTuple<N, I, C, N> M<N>.<F>g__Target|0_1<I, C, N>(N, I, C, N)""
  IL_000e:  newobj     ""System.Func<N, I, C, N, System.ValueTuple<N, I, C, N>>..ctor(object, System.IntPtr)""
  IL_0013:  stsfld     ""System.Func<N, I, C, N, System.ValueTuple<N, I, C, N>> M<N>.<Test>O__0_0<I, C>.<0>__Target""
  IL_0018:  ret
}
");
    }

    [Fact]
    public void TestConditionalOperatorMethodGroup()
    {
        var source = @"
class C
{
    static void Main()
    {
        bool b = true;
        System.Func<int> f = null;
        System.Console.WriteLine(f);
        System.Func<int> g1 = b ? f : M;
        System.Console.WriteLine(g1);
        System.Func<int> g2 = b ? M : f;
        System.Console.WriteLine(g2);
    }

    static int M()
    {
        return 0;
    }
}";
        var comp = CompileAndVerify(source);
        comp.VerifyDiagnostics();
        comp.VerifyIL("C.Main", @"
{
  // Code size       85 (0x55)
  .maxstack  3
  .locals init (System.Func<int> V_0) //f
  IL_0000:  ldc.i4.1
  IL_0001:  ldnull
  IL_0002:  stloc.0
  IL_0003:  ldloc.0
  IL_0004:  call       ""void System.Console.WriteLine(object)""
  IL_0009:  dup
  IL_000a:  brtrue.s   IL_0029
  IL_000c:  ldsfld     ""System.Func<int> C.<>O.<0>__M""
  IL_0011:  dup
  IL_0012:  brtrue.s   IL_002a
  IL_0014:  pop
  IL_0015:  ldnull
  IL_0016:  ldftn      ""int C.M()""
  IL_001c:  newobj     ""System.Func<int>..ctor(object, System.IntPtr)""
  IL_0021:  dup
  IL_0022:  stsfld     ""System.Func<int> C.<>O.<0>__M""
  IL_0027:  br.s       IL_002a
  IL_0029:  ldloc.0
  IL_002a:  call       ""void System.Console.WriteLine(object)""
  IL_002f:  brtrue.s   IL_0034
  IL_0031:  ldloc.0
  IL_0032:  br.s       IL_004f
  IL_0034:  ldsfld     ""System.Func<int> C.<>O.<0>__M""
  IL_0039:  dup
  IL_003a:  brtrue.s   IL_004f
  IL_003c:  pop
  IL_003d:  ldnull
  IL_003e:  ldftn      ""int C.M()""
  IL_0044:  newobj     ""System.Func<int>..ctor(object, System.IntPtr)""
  IL_0049:  dup
  IL_004a:  stsfld     ""System.Func<int> C.<>O.<0>__M""
  IL_004f:  call       ""void System.Console.WriteLine(object)""
  IL_0054:  ret
}
");
    }

    [Fact]
    public void WinMdEventAssignment()
    {
        var source = @"
class C
{
    public event System.Action Instance;
    public static event System.Action Static;
}

class D
{
    C c;

    void InstanceAdd()
    {
        c.Instance += Action;
    }

    void InstanceRemove()
    {
        c.Instance -= Action;
    }

    static void StaticAdd()
    {
        C.Static += Action;
    }

    static void StaticRemove()
    {
        C.Static -= Action;
    }

    static void Action()
    {
    }
}
";
        var verifier = CompileAndVerifyWithWinRt(source, options: TestOptions.ReleaseWinMD);

        verifier.VerifyIL("D.InstanceAdd", @"
{
  // Code size       64 (0x40)
  .maxstack  4
  .locals init (C V_0)
  IL_0000:  ldarg.0
  IL_0001:  ldfld      ""C D.c""
  IL_0006:  stloc.0
  IL_0007:  ldloc.0
  IL_0008:  ldftn      ""System.Runtime.InteropServices.WindowsRuntime.EventRegistrationToken C.Instance.add""
  IL_000e:  newobj     ""System.Func<System.Action, System.Runtime.InteropServices.WindowsRuntime.EventRegistrationToken>..ctor(object, System.IntPtr)""
  IL_0013:  ldloc.0
  IL_0014:  ldftn      ""void C.Instance.remove""
  IL_001a:  newobj     ""System.Action<System.Runtime.InteropServices.WindowsRuntime.EventRegistrationToken>..ctor(object, System.IntPtr)""
  IL_001f:  ldsfld     ""System.Action D.<>O.<0>__Action""
  IL_0024:  dup
  IL_0025:  brtrue.s   IL_003a
  IL_0027:  pop
  IL_0028:  ldnull
  IL_0029:  ldftn      ""void D.Action()""
  IL_002f:  newobj     ""System.Action..ctor(object, System.IntPtr)""
  IL_0034:  dup
  IL_0035:  stsfld     ""System.Action D.<>O.<0>__Action""
  IL_003a:  call       ""void System.Runtime.InteropServices.WindowsRuntime.WindowsRuntimeMarshal.AddEventHandler<System.Action>(System.Func<System.Action, System.Runtime.InteropServices.WindowsRuntime.EventRegistrationToken>, System.Action<System.Runtime.InteropServices.WindowsRuntime.EventRegistrationToken>, System.Action)""
  IL_003f:  ret
}");

        verifier.VerifyIL("D.InstanceRemove", @"
{
  // Code size       50 (0x32)
  .maxstack  3
  IL_0000:  ldarg.0
  IL_0001:  ldfld      ""C D.c""
  IL_0006:  ldftn      ""void C.Instance.remove""
  IL_000c:  newobj     ""System.Action<System.Runtime.InteropServices.WindowsRuntime.EventRegistrationToken>..ctor(object, System.IntPtr)""
  IL_0011:  ldsfld     ""System.Action D.<>O.<0>__Action""
  IL_0016:  dup
  IL_0017:  brtrue.s   IL_002c
  IL_0019:  pop
  IL_001a:  ldnull
  IL_001b:  ldftn      ""void D.Action()""
  IL_0021:  newobj     ""System.Action..ctor(object, System.IntPtr)""
  IL_0026:  dup
  IL_0027:  stsfld     ""System.Action D.<>O.<0>__Action""
  IL_002c:  call       ""void System.Runtime.InteropServices.WindowsRuntime.WindowsRuntimeMarshal.RemoveEventHandler<System.Action>(System.Action<System.Runtime.InteropServices.WindowsRuntime.EventRegistrationToken>, System.Action)""
  IL_0031:  ret
}");
        verifier.VerifyIL("D.StaticAdd", @"
{
  // Code size       57 (0x39)
  .maxstack  4
  IL_0000:  ldnull
  IL_0001:  ldftn      ""System.Runtime.InteropServices.WindowsRuntime.EventRegistrationToken C.Static.add""
  IL_0007:  newobj     ""System.Func<System.Action, System.Runtime.InteropServices.WindowsRuntime.EventRegistrationToken>..ctor(object, System.IntPtr)""
  IL_000c:  ldnull
  IL_000d:  ldftn      ""void C.Static.remove""
  IL_0013:  newobj     ""System.Action<System.Runtime.InteropServices.WindowsRuntime.EventRegistrationToken>..ctor(object, System.IntPtr)""
  IL_0018:  ldsfld     ""System.Action D.<>O.<0>__Action""
  IL_001d:  dup
  IL_001e:  brtrue.s   IL_0033
  IL_0020:  pop
  IL_0021:  ldnull
  IL_0022:  ldftn      ""void D.Action()""
  IL_0028:  newobj     ""System.Action..ctor(object, System.IntPtr)""
  IL_002d:  dup
  IL_002e:  stsfld     ""System.Action D.<>O.<0>__Action""
  IL_0033:  call       ""void System.Runtime.InteropServices.WindowsRuntime.WindowsRuntimeMarshal.AddEventHandler<System.Action>(System.Func<System.Action, System.Runtime.InteropServices.WindowsRuntime.EventRegistrationToken>, System.Action<System.Runtime.InteropServices.WindowsRuntime.EventRegistrationToken>, System.Action)""
  IL_0038:  ret
}");

        verifier.VerifyIL("D.StaticRemove", @"
{
  // Code size       45 (0x2d)
  .maxstack  3
  IL_0000:  ldnull
  IL_0001:  ldftn      ""void C.Static.remove""
  IL_0007:  newobj     ""System.Action<System.Runtime.InteropServices.WindowsRuntime.EventRegistrationToken>..ctor(object, System.IntPtr)""
  IL_000c:  ldsfld     ""System.Action D.<>O.<0>__Action""
  IL_0011:  dup
  IL_0012:  brtrue.s   IL_0027
  IL_0014:  pop
  IL_0015:  ldnull
  IL_0016:  ldftn      ""void D.Action()""
  IL_001c:  newobj     ""System.Action..ctor(object, System.IntPtr)""
  IL_0021:  dup
  IL_0022:  stsfld     ""System.Action D.<>O.<0>__Action""
  IL_0027:  call       ""void System.Runtime.InteropServices.WindowsRuntime.WindowsRuntimeMarshal.RemoveEventHandler<System.Action>(System.Action<System.Runtime.InteropServices.WindowsRuntime.EventRegistrationToken>, System.Action)""
  IL_002c:  ret
}");
    }

    [Fact]
    public void WinMdEventFieldAssignment()
    {
        var source = @"
class C
{
    public event System.Action Instance;
    public static event System.Action Static;

    void InstanceAssign()
    {
        Instance = Action;
    }

    static void StaticAssign()
    {
        Static = Action;
    }

    static void Action()
    {
    }
}
";
        var verifier = CompileAndVerifyWithWinRt(source, options: TestOptions.ReleaseWinMD);

        verifier.VerifyIL("C.InstanceAssign", @"
{
  // Code size       74 (0x4a)
  .maxstack  4
  IL_0000:  ldarg.0
  IL_0001:  ldftn      ""void C.Instance.remove""
  IL_0007:  newobj     ""System.Action<System.Runtime.InteropServices.WindowsRuntime.EventRegistrationToken>..ctor(object, System.IntPtr)""
  IL_000c:  call       ""void System.Runtime.InteropServices.WindowsRuntime.WindowsRuntimeMarshal.RemoveAllEventHandlers(System.Action<System.Runtime.InteropServices.WindowsRuntime.EventRegistrationToken>)""
  IL_0011:  ldarg.0
  IL_0012:  ldftn      ""System.Runtime.InteropServices.WindowsRuntime.EventRegistrationToken C.Instance.add""
  IL_0018:  newobj     ""System.Func<System.Action, System.Runtime.InteropServices.WindowsRuntime.EventRegistrationToken>..ctor(object, System.IntPtr)""
  IL_001d:  ldarg.0
  IL_001e:  ldftn      ""void C.Instance.remove""
  IL_0024:  newobj     ""System.Action<System.Runtime.InteropServices.WindowsRuntime.EventRegistrationToken>..ctor(object, System.IntPtr)""
  IL_0029:  ldsfld     ""System.Action C.<>O.<0>__Action""
  IL_002e:  dup
  IL_002f:  brtrue.s   IL_0044
  IL_0031:  pop
  IL_0032:  ldnull
  IL_0033:  ldftn      ""void C.Action()""
  IL_0039:  newobj     ""System.Action..ctor(object, System.IntPtr)""
  IL_003e:  dup
  IL_003f:  stsfld     ""System.Action C.<>O.<0>__Action""
  IL_0044:  call       ""void System.Runtime.InteropServices.WindowsRuntime.WindowsRuntimeMarshal.AddEventHandler<System.Action>(System.Func<System.Action, System.Runtime.InteropServices.WindowsRuntime.EventRegistrationToken>, System.Action<System.Runtime.InteropServices.WindowsRuntime.EventRegistrationToken>, System.Action)""
  IL_0049:  ret
}");

        verifier.VerifyIL("C.StaticAssign", @"
{
  // Code size       74 (0x4a)
  .maxstack  4
  IL_0000:  ldnull
  IL_0001:  ldftn      ""void C.Static.remove""
  IL_0007:  newobj     ""System.Action<System.Runtime.InteropServices.WindowsRuntime.EventRegistrationToken>..ctor(object, System.IntPtr)""
  IL_000c:  call       ""void System.Runtime.InteropServices.WindowsRuntime.WindowsRuntimeMarshal.RemoveAllEventHandlers(System.Action<System.Runtime.InteropServices.WindowsRuntime.EventRegistrationToken>)""
  IL_0011:  ldnull
  IL_0012:  ldftn      ""System.Runtime.InteropServices.WindowsRuntime.EventRegistrationToken C.Static.add""
  IL_0018:  newobj     ""System.Func<System.Action, System.Runtime.InteropServices.WindowsRuntime.EventRegistrationToken>..ctor(object, System.IntPtr)""
  IL_001d:  ldnull
  IL_001e:  ldftn      ""void C.Static.remove""
  IL_0024:  newobj     ""System.Action<System.Runtime.InteropServices.WindowsRuntime.EventRegistrationToken>..ctor(object, System.IntPtr)""
  IL_0029:  ldsfld     ""System.Action C.<>O.<0>__Action""
  IL_002e:  dup
  IL_002f:  brtrue.s   IL_0044
  IL_0031:  pop
  IL_0032:  ldnull
  IL_0033:  ldftn      ""void C.Action()""
  IL_0039:  newobj     ""System.Action..ctor(object, System.IntPtr)""
  IL_003e:  dup
  IL_003f:  stsfld     ""System.Action C.<>O.<0>__Action""
  IL_0044:  call       ""void System.Runtime.InteropServices.WindowsRuntime.WindowsRuntimeMarshal.AddEventHandler<System.Action>(System.Func<System.Action, System.Runtime.InteropServices.WindowsRuntime.EventRegistrationToken>, System.Action<System.Runtime.InteropServices.WindowsRuntime.EventRegistrationToken>, System.Action)""
  IL_0049:  ret
}");
    }

    [Fact]
    public void LockDelegate()
    {
        var text =
@"
delegate void D(string p1);
partial class Test
{
    public static void Main()
    {
        D d1;
        lock (d1 = PM)
        {
            d1(""PASS"");
        }
    }
    static partial void PM(string p2);
    static partial void PM(string p2)
    {
        System.Console.WriteLine(p2);
    }
}
";

        CompileAndVerify(text, expectedOutput: PASS).VerifyIL("Test.Main", @"
{
  // Code size       64 (0x40)
  .maxstack  2
  .locals init (D V_0, //d1
                D V_1,
                bool V_2)
  IL_0000:  ldsfld     ""D Test.<>O.<0>__PM""
  IL_0005:  dup
  IL_0006:  brtrue.s   IL_001b
  IL_0008:  pop
  IL_0009:  ldnull
  IL_000a:  ldftn      ""void Test.PM(string)""
  IL_0010:  newobj     ""D..ctor(object, System.IntPtr)""
  IL_0015:  dup
  IL_0016:  stsfld     ""D Test.<>O.<0>__PM""
  IL_001b:  dup
  IL_001c:  stloc.0
  IL_001d:  stloc.1
  IL_001e:  ldc.i4.0
  IL_001f:  stloc.2
  .try
  {
    IL_0020:  ldloc.1
    IL_0021:  ldloca.s   V_2
    IL_0023:  call       ""void System.Threading.Monitor.Enter(object, ref bool)""
    IL_0028:  ldloc.0
    IL_0029:  ldstr      ""PASS""
    IL_002e:  callvirt   ""void D.Invoke(string)""
    IL_0033:  leave.s    IL_003f
  }
  finally
  {
    IL_0035:  ldloc.2
    IL_0036:  brfalse.s  IL_003e
    IL_0038:  ldloc.1
    IL_0039:  call       ""void System.Threading.Monitor.Exit(object)""
    IL_003e:  endfinally
  }
  IL_003f:  ret
}
");
    }

    [Fact]
    public void NullCoalescingAssignmentValidRHS()
    {
        CompileAndVerify(@"
using System;
public class C
{
    public static void Main()
    {
        Action a = null;
        (a ??= TestMethod)();
        (a ??= () => {})();
    }
    static void TestMethod() => Console.WriteLine(""In TestMethod"");
}
", expectedOutput: @"
In TestMethod
In TestMethod
").VerifyIL("C.Main()", @"
{
  // Code size       85 (0x55)
  .maxstack  2
  .locals init (System.Action V_0) //a
  IL_0000:  ldnull
  IL_0001:  stloc.0
  IL_0002:  ldloc.0
  IL_0003:  dup
  IL_0004:  brtrue.s   IL_0024
  IL_0006:  pop
  IL_0007:  ldsfld     ""System.Action C.<>O.<0>__TestMethod""
  IL_000c:  dup
  IL_000d:  brtrue.s   IL_0022
  IL_000f:  pop
  IL_0010:  ldnull
  IL_0011:  ldftn      ""void C.TestMethod()""
  IL_0017:  newobj     ""System.Action..ctor(object, System.IntPtr)""
  IL_001c:  dup
  IL_001d:  stsfld     ""System.Action C.<>O.<0>__TestMethod""
  IL_0022:  dup
  IL_0023:  stloc.0
  IL_0024:  callvirt   ""void System.Action.Invoke()""
  IL_0029:  ldloc.0
  IL_002a:  dup
  IL_002b:  brtrue.s   IL_004f
  IL_002d:  pop
  IL_002e:  ldsfld     ""System.Action C.<>c.<>9__0_0""
  IL_0033:  dup
  IL_0034:  brtrue.s   IL_004d
  IL_0036:  pop
  IL_0037:  ldsfld     ""C.<>c C.<>c.<>9""
  IL_003c:  ldftn      ""void C.<>c.<Main>b__0_0()""
  IL_0042:  newobj     ""System.Action..ctor(object, System.IntPtr)""
  IL_0047:  dup
  IL_0048:  stsfld     ""System.Action C.<>c.<>9__0_0""
  IL_004d:  dup
  IL_004e:  stloc.0
  IL_004f:  callvirt   ""void System.Action.Invoke()""
  IL_0054:  ret
}");
    }

    [Fact]
    public void ImplicitlyTypedVariables_01()
    {
        var source =
@"using System;
class Program
{
    static void Main()
    {
        var d1 = Main;
        Report(d1);
        var d2 = () => { };
        Report(d2);
        var d3 = delegate () { };
        Report(d3);
    }
    static void Report(Delegate d) => Console.WriteLine($""{d.GetType().Namespace}.{d.GetType().Name}"");
}";

        var comp = CreateCompilation(source, parseOptions: TestOptions.Regular10, options: TestOptions.DebugExe);
        comp.VerifyDiagnostics();

        var verifier = CompileAndVerify(comp, expectedOutput:
@"System.Action
System.Action
System.Action");
        verifier.VerifyIL("Program.Main",
@"{
  // Code size      100 (0x64)
  .maxstack  2
  .locals init (System.Action V_0, //d1
                System.Action V_1, //d2
                System.Action V_2) //d3
  IL_0000:  nop
  IL_0001:  ldnull
  IL_0002:  ldftn      ""void Program.Main()""
  IL_0008:  newobj     ""System.Action..ctor(object, System.IntPtr)""
  IL_000d:  stloc.0
  IL_000e:  ldloc.0
  IL_000f:  call       ""void Program.Report(System.Delegate)""
  IL_0014:  nop
  IL_0015:  ldsfld     ""System.Action Program.<>c.<>9__0_0""
  IL_001a:  dup
  IL_001b:  brtrue.s   IL_0034
  IL_001d:  pop
  IL_001e:  ldsfld     ""Program.<>c Program.<>c.<>9""
  IL_0023:  ldftn      ""void Program.<>c.<Main>b__0_0()""
  IL_0029:  newobj     ""System.Action..ctor(object, System.IntPtr)""
  IL_002e:  dup
  IL_002f:  stsfld     ""System.Action Program.<>c.<>9__0_0""
  IL_0034:  stloc.1
  IL_0035:  ldloc.1
  IL_0036:  call       ""void Program.Report(System.Delegate)""
  IL_003b:  nop
  IL_003c:  ldsfld     ""System.Action Program.<>c.<>9__0_1""
  IL_0041:  dup
  IL_0042:  brtrue.s   IL_005b
  IL_0044:  pop
  IL_0045:  ldsfld     ""Program.<>c Program.<>c.<>9""
  IL_004a:  ldftn      ""void Program.<>c.<Main>b__0_1()""
  IL_0050:  newobj     ""System.Action..ctor(object, System.IntPtr)""
  IL_0055:  dup
  IL_0056:  stsfld     ""System.Action Program.<>c.<>9__0_1""
  IL_005b:  stloc.2
  IL_005c:  ldloc.2
  IL_005d:  call       ""void Program.Report(System.Delegate)""
  IL_0062:  nop
  IL_0063:  ret
}");

        comp = CreateCompilation(source, options: TestOptions.DebugExe);
        comp.VerifyDiagnostics();

        verifier = CompileAndVerify(comp, expectedOutput:
@"System.Action
System.Action
System.Action");
        verifier.VerifyIL("Program.Main",
@"{
  // Code size      115 (0x73)
  .maxstack  2
  .locals init (System.Action V_0, //d1
                System.Action V_1, //d2
                System.Action V_2) //d3
  IL_0000:  nop
  IL_0001:  ldsfld     ""System.Action Program.<>O.<0>__Main""
  IL_0006:  dup
  IL_0007:  brtrue.s   IL_001c
  IL_0009:  pop
  IL_000a:  ldnull
  IL_000b:  ldftn      ""void Program.Main()""
  IL_0011:  newobj     ""System.Action..ctor(object, System.IntPtr)""
  IL_0016:  dup
  IL_0017:  stsfld     ""System.Action Program.<>O.<0>__Main""
  IL_001c:  stloc.0
  IL_001d:  ldloc.0
  IL_001e:  call       ""void Program.Report(System.Delegate)""
  IL_0023:  nop
  IL_0024:  ldsfld     ""System.Action Program.<>c.<>9__0_0""
  IL_0029:  dup
  IL_002a:  brtrue.s   IL_0043
  IL_002c:  pop
  IL_002d:  ldsfld     ""Program.<>c Program.<>c.<>9""
  IL_0032:  ldftn      ""void Program.<>c.<Main>b__0_0()""
  IL_0038:  newobj     ""System.Action..ctor(object, System.IntPtr)""
  IL_003d:  dup
  IL_003e:  stsfld     ""System.Action Program.<>c.<>9__0_0""
  IL_0043:  stloc.1
  IL_0044:  ldloc.1
  IL_0045:  call       ""void Program.Report(System.Delegate)""
  IL_004a:  nop
  IL_004b:  ldsfld     ""System.Action Program.<>c.<>9__0_1""
  IL_0050:  dup
  IL_0051:  brtrue.s   IL_006a
  IL_0053:  pop
  IL_0054:  ldsfld     ""Program.<>c Program.<>c.<>9""
  IL_0059:  ldftn      ""void Program.<>c.<Main>b__0_1()""
  IL_005f:  newobj     ""System.Action..ctor(object, System.IntPtr)""
  IL_0064:  dup
  IL_0065:  stsfld     ""System.Action Program.<>c.<>9__0_1""
  IL_006a:  stloc.2
  IL_006b:  ldloc.2
  IL_006c:  call       ""void Program.Report(System.Delegate)""
  IL_0071:  nop
  IL_0072:  ret
}");
    }

}
