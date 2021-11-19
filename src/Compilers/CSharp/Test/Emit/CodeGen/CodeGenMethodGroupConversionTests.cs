// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Linq;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Test.Utilities;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.CSharp.UnitTests.CodeGen
{
    public class CodeGenMethodGroupConversionTests : CSharpTestBase
    {
        const string PASS = "PASS";
        static readonly MetadataReference[] s_SystemCoreRef = new[] { SystemCoreRef };

        #region Not caching

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
            Action<ModuleSymbol> containerValidator = module =>
            {
                Assert.Null(module.GlobalNamespace.GetMember<NamedTypeSymbol>("<>x"));
            };
            var compilation = CompileAndVerify(source, symbolValidator: containerValidator, expectedOutput: PASS);
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
            Action<ModuleSymbol> containerValidator = module =>
            {
                Assert.Null(module.GlobalNamespace.GetMember<NamedTypeSymbol>("<>x"));
            };
            var compilation = CompileAndVerify(source, symbolValidator: containerValidator, expectedOutput: PASS);
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
            Action<ModuleSymbol> containerValidator = module =>
            {
                Assert.Null(module.GlobalNamespace.GetMember<NamedTypeSymbol>("<>x"));
            };
            var compilation = CompileAndVerify(source, symbolValidator: containerValidator, expectedOutput: PASS, additionalRefs: s_SystemCoreRef);
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
            Action<ModuleSymbol> containerValidator = module =>
            {
                Assert.Null(module.GlobalNamespace.GetMember<NamedTypeSymbol>("<>x"));
            };
            var compilation = CompileAndVerify(source, symbolValidator: containerValidator, expectedOutput: PASS, additionalRefs: s_SystemCoreRef);
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
            Action<ModuleSymbol> containerValidator = module =>
            {
                Assert.Null(module.GlobalNamespace.GetMember<NamedTypeSymbol>("<>x"));
            };
            var compilation = CompileAndVerify(source, symbolValidator: containerValidator, additionalRefs: s_SystemCoreRef);
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
            Action<ModuleSymbol> containerValidator = module =>
            {
                Assert.Null(module.GlobalNamespace.GetMember<NamedTypeSymbol>("<>x"));
            };
            var compilation = CompileAndVerify(source, symbolValidator: containerValidator, additionalRefs: s_SystemCoreRef);
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
            Action<ModuleSymbol> containerValidator = module =>
            {
                Assert.Null(module.GlobalNamespace.GetMember<NamedTypeSymbol>("<>x"));
            };
            var compilation = CompileAndVerify(source, symbolValidator: containerValidator);
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
            Action<ModuleSymbol> containerValidator = module =>
            {
                Assert.Null(module.GlobalNamespace.GetMember<NamedTypeSymbol>("<>x"));
            };
            var compilation = CompileAndVerify(source, symbolValidator: containerValidator);
        }

        #endregion

        [Fact]
        public void CacheExplicitConversions_ModuleScoped0()
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
            var compilation = CompileAndVerify(source, expectedOutput: PASS).VerifyIL("C.Main", @"
{
  // Code size       33 (0x21)
  .maxstack  2
  IL_0000:  ldsfld     ""System.Action <>x.<Target>w""
  IL_0005:  dup
  IL_0006:  brtrue.s   IL_001b
  IL_0008:  pop
  IL_0009:  ldnull
  IL_000a:  ldftn      ""void C.Target()""
  IL_0010:  newobj     ""System.Action..ctor(object, System.IntPtr)""
  IL_0015:  dup
  IL_0016:  stsfld     ""System.Action <>x.<Target>w""
  IL_001b:  call       ""void C.Test(System.Action)""
  IL_0020:  ret
}
");
        }

        [Fact]
        public void CacheExplicitConversions_ModuleScoped1()
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
            var compilation = CompileAndVerify(source, expectedOutput: PASS).VerifyIL("D<T>.Test", @"
{
  // Code size       28 (0x1c)
  .maxstack  2
  IL_0000:  ldsfld     ""System.Action <>x.<Target>w""
  IL_0005:  dup
  IL_0006:  brtrue.s   IL_001b
  IL_0008:  pop
  IL_0009:  ldnull
  IL_000a:  ldftn      ""void C.Target()""
  IL_0010:  newobj     ""System.Action..ctor(object, System.IntPtr)""
  IL_0015:  dup
  IL_0016:  stsfld     ""System.Action <>x.<Target>w""
  IL_001b:  ret
}
");
        }

        [Fact]
        public void CacheExplicitConversions_ModuleScoped2()
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
            var compilation = CompileAndVerify(source, expectedOutput: PASS).VerifyIL("D<T>.Test", @"
{
  // Code size       33 (0x21)
  .maxstack  2
  IL_0000:  ldsfld     ""System.Action <>x.<Target>w""
  IL_0005:  dup
  IL_0006:  brtrue.s   IL_001b
  IL_0008:  pop
  IL_0009:  ldnull
  IL_000a:  ldftn      ""void E<int>.Target()""
  IL_0010:  newobj     ""System.Action..ctor(object, System.IntPtr)""
  IL_0015:  dup
  IL_0016:  stsfld     ""System.Action <>x.<Target>w""
  IL_001b:  callvirt   ""void System.Action.Invoke()""
  IL_0020:  ret
}
");
        }

        [Fact]
        public void CacheExplicitConversions_ModuleScoped3()
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
            var compilation = CompileAndVerify(source, expectedOutput: PASS).VerifyIL("D<T>.Test", @"
{
  // Code size       34 (0x22)
  .maxstack  2
  IL_0000:  ldsfld     ""System.Action<int> <>x.<Target>w""
  IL_0005:  dup
  IL_0006:  brtrue.s   IL_001b
  IL_0008:  pop
  IL_0009:  ldnull
  IL_000a:  ldftn      ""void E<int>.Target(int)""
  IL_0010:  newobj     ""System.Action<int>..ctor(object, System.IntPtr)""
  IL_0015:  dup
  IL_0016:  stsfld     ""System.Action<int> <>x.<Target>w""
  IL_001b:  ldc.i4.0
  IL_001c:  callvirt   ""void System.Action<int>.Invoke(int)""
  IL_0021:  ret
}
");
        }

        [Fact]
        public void CacheExplicitConversions_ModuleScoped4()
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
            var compilation = CompileAndVerify(source, expectedOutput: PASS).VerifyIL("D<T>.Test", @"
{
  // Code size       34 (0x22)
  .maxstack  2
  IL_0000:  ldsfld     ""System.Action<int> <>x.<Target>w""
  IL_0005:  dup
  IL_0006:  brtrue.s   IL_001b
  IL_0008:  pop
  IL_0009:  ldnull
  IL_000a:  ldftn      ""void E<int>.Target<double>(int)""
  IL_0010:  newobj     ""System.Action<int>..ctor(object, System.IntPtr)""
  IL_0015:  dup
  IL_0016:  stsfld     ""System.Action<int> <>x.<Target>w""
  IL_001b:  ldc.i4.0
  IL_001c:  callvirt   ""void System.Action<int>.Invoke(int)""
  IL_0021:  ret
}
");
        }

        [Fact]
        public void CacheExplicitConversions_ModuleScoped5()
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
            var compilation = CompileAndVerify(source, expectedOutput: PASS).VerifyIL("D<T>.Test", @"
{
  // Code size       35 (0x23)
  .maxstack  2
  IL_0000:  ldsfld     ""System.Func<int, int> <>x.<Target>w""
  IL_0005:  dup
  IL_0006:  brtrue.s   IL_001b
  IL_0008:  pop
  IL_0009:  ldnull
  IL_000a:  ldftn      ""int E<int>.Target<double>(int)""
  IL_0010:  newobj     ""System.Func<int, int>..ctor(object, System.IntPtr)""
  IL_0015:  dup
  IL_0016:  stsfld     ""System.Func<int, int> <>x.<Target>w""
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
            var compilation = CompileAndVerify(source, expectedOutput: PASS).VerifyIL("D<T>.Test", @"
{
  // Code size       33 (0x21)
  .maxstack  2
  IL_0000:  ldsfld     ""System.Action D<T>.<>x.<Target>w""
  IL_0005:  dup
  IL_0006:  brtrue.s   IL_001b
  IL_0008:  pop
  IL_0009:  ldnull
  IL_000a:  ldftn      ""void D<T>.Target()""
  IL_0010:  newobj     ""System.Action..ctor(object, System.IntPtr)""
  IL_0015:  dup
  IL_0016:  stsfld     ""System.Action D<T>.<>x.<Target>w""
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
            var compilation = CompileAndVerify(source, expectedOutput: PASS).VerifyIL("D<T>.Test", @"
{
  // Code size       33 (0x21)
  .maxstack  2
  IL_0000:  ldsfld     ""System.Action D<T>.<>x.<Target>w""
  IL_0005:  dup
  IL_0006:  brtrue.s   IL_001b
  IL_0008:  pop
  IL_0009:  ldnull
  IL_000a:  ldftn      ""void D<T>.Target<int>()""
  IL_0010:  newobj     ""System.Action..ctor(object, System.IntPtr)""
  IL_0015:  dup
  IL_0016:  stsfld     ""System.Action D<T>.<>x.<Target>w""
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
            var compilation = CompileAndVerify(source, expectedOutput: PASS).VerifyIL("D<T>.Test", @"
{
  // Code size       33 (0x21)
  .maxstack  2
  IL_0000:  ldsfld     ""System.Action D<T>.<>x.<Target>w""
  IL_0005:  dup
  IL_0006:  brtrue.s   IL_001b
  IL_0008:  pop
  IL_0009:  ldnull
  IL_000a:  ldftn      ""void D<T>.Target<T>()""
  IL_0010:  newobj     ""System.Action..ctor(object, System.IntPtr)""
  IL_0015:  dup
  IL_0016:  stsfld     ""System.Action D<T>.<>x.<Target>w""
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
            var compilation = CompileAndVerify(source, expectedOutput: PASS).VerifyIL("D<T>.Test", @"
{
  // Code size       33 (0x21)
  .maxstack  2
  IL_0000:  ldsfld     ""System.Action D<T>.<>x.<Target>w""
  IL_0005:  dup
  IL_0006:  brtrue.s   IL_001b
  IL_0008:  pop
  IL_0009:  ldnull
  IL_000a:  ldftn      ""void E<T>.Target()""
  IL_0010:  newobj     ""System.Action..ctor(object, System.IntPtr)""
  IL_0015:  dup
  IL_0016:  stsfld     ""System.Action D<T>.<>x.<Target>w""
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
            var compilation = CompileAndVerify(source, expectedOutput: PASS).VerifyIL("D<T>.Test", @"
{
  // Code size       33 (0x21)
  .maxstack  2
  IL_0000:  ldsfld     ""System.Action D<T>.<>x.<Target>w""
  IL_0005:  dup
  IL_0006:  brtrue.s   IL_001b
  IL_0008:  pop
  IL_0009:  ldnull
  IL_000a:  ldftn      ""void E<T>.Target<double>()""
  IL_0010:  newobj     ""System.Action..ctor(object, System.IntPtr)""
  IL_0015:  dup
  IL_0016:  stsfld     ""System.Action D<T>.<>x.<Target>w""
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
            var compilation = CompileAndVerify(source, expectedOutput: PASS).VerifyIL("D<T>.Test", @"
{
  // Code size       34 (0x22)
  .maxstack  2
  IL_0000:  ldsfld     ""System.Func<T> D<T>.<>x.<Target>w""
  IL_0005:  dup
  IL_0006:  brtrue.s   IL_001b
  IL_0008:  pop
  IL_0009:  ldnull
  IL_000a:  ldftn      ""T E<int>.Target<T>()""
  IL_0010:  newobj     ""System.Func<T>..ctor(object, System.IntPtr)""
  IL_0015:  dup
  IL_0016:  stsfld     ""System.Func<T> D<T>.<>x.<Target>w""
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
            var compilation = CompileAndVerify(source, expectedOutput: PASS).VerifyIL("D<T>.Test", @"
{
  // Code size       34 (0x22)
  .maxstack  2
  IL_0000:  ldsfld     ""System.Func<int> D<T>.<>x.<Target>w""
  IL_0005:  dup
  IL_0006:  brtrue.s   IL_001b
  IL_0008:  pop
  IL_0009:  ldnull
  IL_000a:  ldftn      ""int E<T>.Target<int>()""
  IL_0010:  newobj     ""System.Func<int>..ctor(object, System.IntPtr)""
  IL_0015:  dup
  IL_0016:  stsfld     ""System.Func<int> D<T>.<>x.<Target>w""
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
            var compilation = CompileAndVerify(source, expectedOutput: PASS).VerifyIL("D<T>.Test", @"
{
  // Code size       43 (0x2b)
  .maxstack  2
  .locals init (T V_0)
  IL_0000:  ldsfld     ""System.Func<T, int> D<T>.<>x.<Target>w""
  IL_0005:  dup
  IL_0006:  brtrue.s   IL_001b
  IL_0008:  pop
  IL_0009:  ldnull
  IL_000a:  ldftn      ""int E<T>.Target<int>(T)""
  IL_0010:  newobj     ""System.Func<T, int>..ctor(object, System.IntPtr)""
  IL_0015:  dup
  IL_0016:  stsfld     ""System.Func<T, int> D<T>.<>x.<Target>w""
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
            var compilation = CompileAndVerify(source, expectedOutput: PASS).VerifyIL("D.Test<T>", @"
{
  // Code size       33 (0x21)
  .maxstack  2
  IL_0000:  ldsfld     ""System.Action D.<Test>x<T>.<Target>w""
  IL_0005:  dup
  IL_0006:  brtrue.s   IL_001b
  IL_0008:  pop
  IL_0009:  ldnull
  IL_000a:  ldftn      ""void C.Target<T>()""
  IL_0010:  newobj     ""System.Action..ctor(object, System.IntPtr)""
  IL_0015:  dup
  IL_0016:  stsfld     ""System.Action D.<Test>x<T>.<Target>w""
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
            var compilation = CompileAndVerify(source, expectedOutput: PASS).VerifyIL("D.Test<T>", @"
{
  // Code size       33 (0x21)
  .maxstack  2
  IL_0000:  ldsfld     ""System.Action D.<Test>x<T>.<Target>w""
  IL_0005:  dup
  IL_0006:  brtrue.s   IL_001b
  IL_0008:  pop
  IL_0009:  ldnull
  IL_000a:  ldftn      ""void E<T>.Target()""
  IL_0010:  newobj     ""System.Action..ctor(object, System.IntPtr)""
  IL_0015:  dup
  IL_0016:  stsfld     ""System.Action D.<Test>x<T>.<Target>w""
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
            var compilation = CompileAndVerify(source, expectedOutput: PASS).VerifyIL("D.Test<T>", @"
{
  // Code size       43 (0x2b)
  .maxstack  2
  .locals init (T V_0)
  IL_0000:  ldsfld     ""System.Func<T, int> D.<Test>x<T>.<Target>w""
  IL_0005:  dup
  IL_0006:  brtrue.s   IL_001b
  IL_0008:  pop
  IL_0009:  ldnull
  IL_000a:  ldftn      ""int E<T>.Target<int>(T)""
  IL_0010:  newobj     ""System.Func<T, int>..ctor(object, System.IntPtr)""
  IL_0015:  dup
  IL_0016:  stsfld     ""System.Func<T, int> D.<Test>x<T>.<Target>w""
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
            var compilation = CompileAndVerify(source, expectedOutput: PASS).VerifyIL("D<M>.Test<T>", @"
{
  // Code size       51 (0x33)
  .maxstack  3
  .locals init (M V_0,
                T V_1)
  IL_0000:  ldsfld     ""System.Action<M, T> D<M>.<Test>x<T>.<Target>w""
  IL_0005:  dup
  IL_0006:  brtrue.s   IL_001b
  IL_0008:  pop
  IL_0009:  ldnull
  IL_000a:  ldftn      ""void E<M>.Target<T>(M, T)""
  IL_0010:  newobj     ""System.Action<M, T>..ctor(object, System.IntPtr)""
  IL_0015:  dup
  IL_0016:  stsfld     ""System.Action<M, T> D<M>.<Test>x<T>.<Target>w""
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
        public void CacheImplicitConversions_ModuleScoped0()
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
            var compilation = CompileAndVerify(source, expectedOutput: PASS).VerifyIL("C.Main", @"
{
  // Code size       33 (0x21)
  .maxstack  2
  IL_0000:  ldsfld     ""MyAction<int> <>x.<Target>w""
  IL_0005:  dup
  IL_0006:  brtrue.s   IL_001b
  IL_0008:  pop
  IL_0009:  ldnull
  IL_000a:  ldftn      ""void C.Target()""
  IL_0010:  newobj     ""MyAction<int>..ctor(object, System.IntPtr)""
  IL_0015:  dup
  IL_0016:  stsfld     ""MyAction<int> <>x.<Target>w""
  IL_001b:  callvirt   ""void MyAction<int>.Invoke()""
  IL_0020:  ret
}
");
        }

        [Fact]
        public void CacheImplicitConversions_ModuleScoped1()
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
            var compilation = CompileAndVerify(source, expectedOutput: PASS).VerifyIL("C.Main", @"
{
  // Code size       33 (0x21)
  .maxstack  2
  IL_0000:  ldsfld     ""C.MyAction<int> <>x.<Target>w""
  IL_0005:  dup
  IL_0006:  brtrue.s   IL_001b
  IL_0008:  pop
  IL_0009:  ldnull
  IL_000a:  ldftn      ""void C.Target()""
  IL_0010:  newobj     ""C.MyAction<int>..ctor(object, System.IntPtr)""
  IL_0015:  dup
  IL_0016:  stsfld     ""C.MyAction<int> <>x.<Target>w""
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
            var compilation = CompileAndVerify(source, expectedOutput: PASS).VerifyIL("D<T>.Test", @"
{
  // Code size       33 (0x21)
  .maxstack  2
  IL_0000:  ldsfld     ""MyAction D<T>.<>x.<Target>w""
  IL_0005:  dup
  IL_0006:  brtrue.s   IL_001b
  IL_0008:  pop
  IL_0009:  ldnull
  IL_000a:  ldftn      ""void D<T>.Target()""
  IL_0010:  newobj     ""MyAction..ctor(object, System.IntPtr)""
  IL_0015:  dup
  IL_0016:  stsfld     ""MyAction D<T>.<>x.<Target>w""
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
            var compilation = CompileAndVerify(source, expectedOutput: PASS).VerifyIL("D<T>.Test", @"
{
  // Code size       28 (0x1c)
  .maxstack  2
  IL_0000:  ldsfld     ""D<T>.MyAction D<T>.<>x.<Target>w""
  IL_0005:  dup
  IL_0006:  brtrue.s   IL_001b
  IL_0008:  pop
  IL_0009:  ldnull
  IL_000a:  ldftn      ""void D<T>.Target<int>()""
  IL_0010:  newobj     ""D<T>.MyAction..ctor(object, System.IntPtr)""
  IL_0015:  dup
  IL_0016:  stsfld     ""D<T>.MyAction D<T>.<>x.<Target>w""
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
            var compilation = CompileAndVerify(source, expectedOutput: PASS).VerifyIL("D<T>.Test", @"
{
  // Code size       33 (0x21)
  .maxstack  2
  IL_0000:  ldsfld     ""D<T>.MyAction<int> D<T>.<>x.<Target>w""
  IL_0005:  dup
  IL_0006:  brtrue.s   IL_001b
  IL_0008:  pop
  IL_0009:  ldnull
  IL_000a:  ldftn      ""void D<T>.Target<T>()""
  IL_0010:  newobj     ""D<T>.MyAction<int>..ctor(object, System.IntPtr)""
  IL_0015:  dup
  IL_0016:  stsfld     ""D<T>.MyAction<int> D<T>.<>x.<Target>w""
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
            var compilation = CompileAndVerify(source, expectedOutput: PASS).VerifyIL("D<T>.Test", @"
{
  // Code size       33 (0x21)
  .maxstack  2
  IL_0000:  ldsfld     ""D<T>.MyAction<T> D<T>.<>x.<Target>w""
  IL_0005:  dup
  IL_0006:  brtrue.s   IL_001b
  IL_0008:  pop
  IL_0009:  ldnull
  IL_000a:  ldftn      ""void E<T>.Target()""
  IL_0010:  newobj     ""D<T>.MyAction<T>..ctor(object, System.IntPtr)""
  IL_0015:  dup
  IL_0016:  stsfld     ""D<T>.MyAction<T> D<T>.<>x.<Target>w""
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
            var compilation = CompileAndVerify(source, expectedOutput: PASS).VerifyIL("D<T>.Test", @"
{
  // Code size       33 (0x21)
  .maxstack  2
  IL_0000:  ldsfld     ""D<T>.MyAction<T> D<T>.<>x.<Target>w""
  IL_0005:  dup
  IL_0006:  brtrue.s   IL_001b
  IL_0008:  pop
  IL_0009:  ldnull
  IL_000a:  ldftn      ""void E<T>.Target<int>()""
  IL_0010:  newobj     ""D<T>.MyAction<T>..ctor(object, System.IntPtr)""
  IL_0015:  dup
  IL_0016:  stsfld     ""D<T>.MyAction<T> D<T>.<>x.<Target>w""
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
            var compilation = CompileAndVerify(source, expectedOutput: PASS).VerifyIL("D<T>.Test", @"
{
  // Code size       34 (0x22)
  .maxstack  2
  IL_0000:  ldsfld     ""D<T>.MyFunc D<T>.<>x.<Target>w""
  IL_0005:  dup
  IL_0006:  brtrue.s   IL_001b
  IL_0008:  pop
  IL_0009:  ldnull
  IL_000a:  ldftn      ""T E<T>.Target<T, int>()""
  IL_0010:  newobj     ""D<T>.MyFunc..ctor(object, System.IntPtr)""
  IL_0015:  dup
  IL_0016:  stsfld     ""D<T>.MyFunc D<T>.<>x.<Target>w""
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
            var compilation = CompileAndVerify(source, expectedOutput: PASS).VerifyIL("D<T>.Test", @"
{
  // Code size       43 (0x2b)
  .maxstack  2
  .locals init (T V_0)
  IL_0000:  ldsfld     ""D<T>.MyFunc D<T>.<>x.<Target>w""
  IL_0005:  dup
  IL_0006:  brtrue.s   IL_001b
  IL_0008:  pop
  IL_0009:  ldnull
  IL_000a:  ldftn      ""int E<T>.Target<int>(T)""
  IL_0010:  newobj     ""D<T>.MyFunc..ctor(object, System.IntPtr)""
  IL_0015:  dup
  IL_0016:  stsfld     ""D<T>.MyFunc D<T>.<>x.<Target>w""
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
            var compilation = CompileAndVerify(source, expectedOutput: PASS).VerifyIL("D<T, M>.Test", @"
{
  // Code size       43 (0x2b)
  .maxstack  2
  .locals init (M V_0)
  IL_0000:  ldsfld     ""D<T, M>.MyFunc D<T, M>.<>x.<Target>w""
  IL_0005:  dup
  IL_0006:  brtrue.s   IL_001b
  IL_0008:  pop
  IL_0009:  ldnull
  IL_000a:  ldftn      ""T E<M>.Target<T>(M)""
  IL_0010:  newobj     ""D<T, M>.MyFunc..ctor(object, System.IntPtr)""
  IL_0015:  dup
  IL_0016:  stsfld     ""D<T, M>.MyFunc D<T, M>.<>x.<Target>w""
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
            var compilation = CompileAndVerify(source, expectedOutput: PASS).VerifyIL("C.Main", @"
{
  // Code size       33 (0x21)
  .maxstack  2
  IL_0000:  ldsfld     ""C.MyAction<int> C.<>x.<Target>w""
  IL_0005:  dup
  IL_0006:  brtrue.s   IL_001b
  IL_0008:  pop
  IL_0009:  ldnull
  IL_000a:  ldftn      ""void C.Target()""
  IL_0010:  newobj     ""C.MyAction<int>..ctor(object, System.IntPtr)""
  IL_0015:  dup
  IL_0016:  stsfld     ""C.MyAction<int> C.<>x.<Target>w""
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
            var compilation = CompileAndVerify(source, expectedOutput: PASS).VerifyIL("D.Test<T>", @"
{
  // Code size       33 (0x21)
  .maxstack  2
  IL_0000:  ldsfld     ""D.MyAction D.<Test>x__1<T>.<Target>w""
  IL_0005:  dup
  IL_0006:  brtrue.s   IL_001b
  IL_0008:  pop
  IL_0009:  ldnull
  IL_000a:  ldftn      ""void C.Target<T>()""
  IL_0010:  newobj     ""D.MyAction..ctor(object, System.IntPtr)""
  IL_0015:  dup
  IL_0016:  stsfld     ""D.MyAction D.<Test>x__1<T>.<Target>w""
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
            var compilation = CompileAndVerify(source, expectedOutput: PASS).VerifyIL("D<V>.Test<T>", @"
{
  // Code size       42 (0x2a)
  .maxstack  2
  .locals init (V V_0)
  IL_0000:  ldsfld     ""D<V>.MyAction<V> D<V>.<Test>x__1<T>.<Target>w""
  IL_0005:  dup
  IL_0006:  brtrue.s   IL_001b
  IL_0008:  pop
  IL_0009:  ldnull
  IL_000a:  ldftn      ""void C.Target<V, T>(V)""
  IL_0010:  newobj     ""D<V>.MyAction<V>..ctor(object, System.IntPtr)""
  IL_0015:  dup
  IL_0016:  stsfld     ""D<V>.MyAction<V> D<V>.<Test>x__1<T>.<Target>w""
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
            var compilation = CompileAndVerify(source, expectedOutput: PASS).VerifyIL("D<V>.Test<T>", @"
{
  // Code size       52 (0x34)
  .maxstack  3
  .locals init (T V_0,
                V V_1)
  IL_0000:  ldsfld     ""D<V>.MyFunc<T> D<V>.<Test>x__1<T>.<Target>w""
  IL_0005:  dup
  IL_0006:  brtrue.s   IL_001b
  IL_0008:  pop
  IL_0009:  ldnull
  IL_000a:  ldftn      ""V C.Target<T, V>(T, V)""
  IL_0010:  newobj     ""D<V>.MyFunc<T>..ctor(object, System.IntPtr)""
  IL_0015:  dup
  IL_0016:  stsfld     ""D<V>.MyFunc<T> D<V>.<Test>x__1<T>.<Target>w""
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
            var compilation = CompileAndVerify(source, expectedOutput: PASS).VerifyIL("D<T>.Test", @"
{
  // Code size       42 (0x2a)
  .maxstack  2
  .locals init (T V_0)
  IL_0000:  ldsfld     ""D<T>.MyAction D<T>.<>x.<Target>w""
  IL_0005:  dup
  IL_0006:  brtrue.s   IL_001b
  IL_0008:  pop
  IL_0009:  ldnull
  IL_000a:  ldftn      ""void E<int>.Target<C>(C)""
  IL_0010:  newobj     ""D<T>.MyAction..ctor(object, System.IntPtr)""
  IL_0015:  dup
  IL_0016:  stsfld     ""D<T>.MyAction D<T>.<>x.<Target>w""
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
            Action<ModuleSymbol> containerValidator = module =>
            {
                var testClass = module.GlobalNamespace.GetTypeMember("D");
                var container = testClass.GetTypeMember("<Test>x__1");
                Assert.NotNull(container);

                var typeParameters = container.TypeParameters;
                Assert.Equal(1, container.TypeParameters.Length);

                var m = typeParameters[0];
                Assert.Equal(1, m.ConstraintTypes.Length);
                Assert.Equal(testClass.TypeParameters[0], m.ConstraintTypes[0]);
            };
            var compilation = CompileAndVerify(source, symbolValidator: containerValidator, expectedOutput: PASS).VerifyIL("D<T>.Test<M>", @"
{
  // Code size       34 (0x22)
  .maxstack  2
  IL_0000:  ldsfld     ""D<T>.MyFunc D<T>.<Test>x__1<M>.<Target>w""
  IL_0005:  dup
  IL_0006:  brtrue.s   IL_001b
  IL_0008:  pop
  IL_0009:  ldnull
  IL_000a:  ldftn      ""M E<int>.Target<M>()""
  IL_0010:  newobj     ""D<T>.MyFunc..ctor(object, System.IntPtr)""
  IL_0015:  dup
  IL_0016:  stsfld     ""D<T>.MyFunc D<T>.<Test>x__1<M>.<Target>w""
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
            Action<ModuleSymbol> containerValidator = module =>
            {
                var globalNs = module.GlobalNamespace;
                var mainClass = globalNs.GetTypeMember("C");
                var container = globalNs.GetMember<NamedTypeSymbol>("D.<Test>x__1");
                Assert.NotNull(container);

                var typeParameters = container.TypeParameters;
                Assert.Equal(1, container.TypeParameters.Length);

                var m = typeParameters[0];
                Assert.Equal(1, m.ConstraintTypes.Length);
                Assert.Equal(mainClass, m.ConstraintTypes[0]);
            };
            var compilation = CompileAndVerify(source, symbolValidator: containerValidator, expectedOutput: PASS).VerifyIL("D.Test<M>", @"
{
  // Code size       34 (0x22)
  .maxstack  2
  IL_0000:  ldsfld     ""D.MyFunc D.<Test>x__1<M>.<Target>w""
  IL_0005:  dup
  IL_0006:  brtrue.s   IL_001b
  IL_0008:  pop
  IL_0009:  ldnull
  IL_000a:  ldftn      ""M E<int>.Target<M>()""
  IL_0010:  newobj     ""D.MyFunc..ctor(object, System.IntPtr)""
  IL_0015:  dup
  IL_0016:  stsfld     ""D.MyFunc D.<Test>x__1<M>.<Target>w""
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
            Action<ModuleSymbol> containerValidator = module =>
            {
                var container = module.GlobalNamespace.GetMember<NamedTypeSymbol>("D.<Test>x");
                Assert.NotNull(container);

                var typeParameters = container.TypeParameters;
                Assert.Equal(1, container.TypeParameters.Length);

                var m = typeParameters[0] as Cci.IGenericParameter;
                Assert.NotNull(m);
                Assert.Equal(true, m.MustBeValueType);
            };
            var compilation = CompileAndVerify(source, symbolValidator: containerValidator, expectedOutput: PASS).VerifyIL("D.Test<M>", @"
{
  // Code size       34 (0x22)
  .maxstack  2
  IL_0000:  ldsfld     ""System.Func<M?> D.<Test>x<M>.<Target>w""
  IL_0005:  dup
  IL_0006:  brtrue.s   IL_001b
  IL_0008:  pop
  IL_0009:  ldnull
  IL_000a:  ldftn      ""M? E.Target<M?>()""
  IL_0010:  newobj     ""System.Func<M?>..ctor(object, System.IntPtr)""
  IL_0015:  dup
  IL_0016:  stsfld     ""System.Func<M?> D.<Test>x<M>.<Target>w""
  IL_001b:  callvirt   ""M? System.Func<M?>.Invoke()""
  IL_0020:  pop
  IL_0021:  ret
}
");
        }

        [Fact]
        public void ExtensionMethod_ModuleScoped0()
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
            var compilation = CompileAndVerify(source, expectedOutput: PASS, additionalRefs: s_SystemCoreRef).VerifyIL("C.Main", @"
{
  // Code size       34 (0x22)
  .maxstack  2
  IL_0000:  ldsfld     ""System.Action<C> <>x.<Target>w""
  IL_0005:  dup
  IL_0006:  brtrue.s   IL_001b
  IL_0008:  pop
  IL_0009:  ldnull
  IL_000a:  ldftn      ""void E.Target(C)""
  IL_0010:  newobj     ""System.Action<C>..ctor(object, System.IntPtr)""
  IL_0015:  dup
  IL_0016:  stsfld     ""System.Action<C> <>x.<Target>w""
  IL_001b:  ldnull
  IL_001c:  callvirt   ""void System.Action<C>.Invoke(C)""
  IL_0021:  ret
}
");
        }

        [Fact]
        public void ExtensionMethod_ModuleScoped1()
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
            var compilation = CompileAndVerify(source, expectedOutput: PASS, additionalRefs: s_SystemCoreRef).VerifyIL("C.Main", @"
{
  // Code size       34 (0x22)
  .maxstack  2
  IL_0000:  ldsfld     ""System.Action<C> <>x.<Target>w""
  IL_0005:  dup
  IL_0006:  brtrue.s   IL_001b
  IL_0008:  pop
  IL_0009:  ldnull
  IL_000a:  ldftn      ""void E.Target<C>(C)""
  IL_0010:  newobj     ""System.Action<C>..ctor(object, System.IntPtr)""
  IL_0015:  dup
  IL_0016:  stsfld     ""System.Action<C> <>x.<Target>w""
  IL_001b:  ldnull
  IL_001c:  callvirt   ""void System.Action<C>.Invoke(C)""
  IL_0021:  ret
}
");
        }

        [Fact]
        public void ExtensionMethod_ModuleScoped2()
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
            var compilation = CompileAndVerify(source, additionalRefs: s_SystemCoreRef).VerifyIL("E.Test", @"
{
  // Code size       25 (0x19)
  .maxstack  2
  IL_0000:  ldsfld     ""System.Action<int> <>x.<Target>w""
  IL_0005:  brtrue.s   IL_0018
  IL_0007:  ldnull
  IL_0008:  ldftn      ""void E.Target<int>(int)""
  IL_000e:  newobj     ""System.Action<int>..ctor(object, System.IntPtr)""
  IL_0013:  stsfld     ""System.Action<int> <>x.<Target>w""
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
            var compilation = CompileAndVerify(source, expectedOutput: PASS, additionalRefs: s_SystemCoreRef).VerifyIL("D<T>.Test", @"
{
  // Code size       42 (0x2a)
  .maxstack  2
  .locals init (T V_0)
  IL_0000:  ldsfld     ""System.Action<T> D<T>.<>x.<Target>w""
  IL_0005:  dup
  IL_0006:  brtrue.s   IL_001b
  IL_0008:  pop
  IL_0009:  ldnull
  IL_000a:  ldftn      ""void E.Target(C)""
  IL_0010:  newobj     ""System.Action<T>..ctor(object, System.IntPtr)""
  IL_0015:  dup
  IL_0016:  stsfld     ""System.Action<T> D<T>.<>x.<Target>w""
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
            var compilation = CompileAndVerify(source, expectedOutput: PASS, additionalRefs: s_SystemCoreRef).VerifyIL("D<K>.Test", @"
{
  // Code size       34 (0x22)
  .maxstack  2
  IL_0000:  ldsfld     ""System.Action<K> D<K>.<>x.<Target>w""
  IL_0005:  dup
  IL_0006:  brtrue.s   IL_001b
  IL_0008:  pop
  IL_0009:  ldnull
  IL_000a:  ldftn      ""void E.Target<K>(K)""
  IL_0010:  newobj     ""System.Action<K>..ctor(object, System.IntPtr)""
  IL_0015:  dup
  IL_0016:  stsfld     ""System.Action<K> D<K>.<>x.<Target>w""
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
            var compilation = CompileAndVerify(source, additionalRefs: s_SystemCoreRef).VerifyIL("E.F<T>.Test", @"
{
  // Code size       25 (0x19)
  .maxstack  2
  IL_0000:  ldsfld     ""System.Action<T> E.F<T>.<>x.<Target>w""
  IL_0005:  brtrue.s   IL_0018
  IL_0007:  ldnull
  IL_0008:  ldftn      ""void E.Target<T>(T)""
  IL_000e:  newobj     ""System.Action<T>..ctor(object, System.IntPtr)""
  IL_0013:  stsfld     ""System.Action<T> E.F<T>.<>x.<Target>w""
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
            var compilation = CompileAndVerify(source, expectedOutput: PASS, additionalRefs: s_SystemCoreRef).VerifyIL("C.Test<T>", @"
{
  // Code size       34 (0x22)
  .maxstack  2
  IL_0000:  ldsfld     ""System.Action<T> C.<Test>x__1<T>.<Target>w""
  IL_0005:  dup
  IL_0006:  brtrue.s   IL_001b
  IL_0008:  pop
  IL_0009:  ldnull
  IL_000a:  ldftn      ""void E.Target<T>(T)""
  IL_0010:  newobj     ""System.Action<T>..ctor(object, System.IntPtr)""
  IL_0015:  dup
  IL_0016:  stsfld     ""System.Action<T> C.<Test>x__1<T>.<Target>w""
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
            var compilation = CompileAndVerify(source, expectedOutput: PASS, additionalRefs: s_SystemCoreRef).VerifyIL("C.Test<T>", @"
{
  // Code size       42 (0x2a)
  .maxstack  2
  .locals init (T V_0)
  IL_0000:  ldsfld     ""System.Action<T> C.<Test>x__1<T>.<Target>w""
  IL_0005:  dup
  IL_0006:  brtrue.s   IL_001b
  IL_0008:  pop
  IL_0009:  ldnull
  IL_000a:  ldftn      ""void E.Target(C)""
  IL_0010:  newobj     ""System.Action<T>..ctor(object, System.IntPtr)""
  IL_0015:  dup
  IL_0016:  stsfld     ""System.Action<T> C.<Test>x__1<T>.<Target>w""
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
            var compilation = CompileAndVerify(source, additionalRefs: s_SystemCoreRef).VerifyIL("E.Test<T>", @"
{
  // Code size       25 (0x19)
  .maxstack  2
  IL_0000:  ldsfld     ""System.Action<T> E.<Test>x<T>.<Target>w""
  IL_0005:  brtrue.s   IL_0018
  IL_0007:  ldnull
  IL_0008:  ldftn      ""void E.Target<T>(T)""
  IL_000e:  newobj     ""System.Action<T>..ctor(object, System.IntPtr)""
  IL_0013:  stsfld     ""System.Action<T> E.<Test>x<T>.<Target>w""
  IL_0018:  ret
}
");
        }

        [Fact]
        public void Lambda_ModuleScoped0()
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
            var compilation = CompileAndVerify(source, expectedOutput: PASS).VerifyIL("C.<>c.<Main>b__0_0", @"
{
  // Code size       33 (0x21)
  .maxstack  2
  IL_0000:  ldsfld     ""System.Action <>x.<Target>w""
  IL_0005:  dup
  IL_0006:  brtrue.s   IL_001b
  IL_0008:  pop
  IL_0009:  ldnull
  IL_000a:  ldftn      ""void C.Target()""
  IL_0010:  newobj     ""System.Action..ctor(object, System.IntPtr)""
  IL_0015:  dup
  IL_0016:  stsfld     ""System.Action <>x.<Target>w""
  IL_001b:  call       ""void C.Invoke(System.Action)""
  IL_0020:  ret
}
");
        }

        [Fact]
        public void Lambda_ModuleScoped1()
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
            var compilation = CompileAndVerify(source, expectedOutput: PASS).VerifyIL("C.<>c.<Main>b__0_0", @"
{
  // Code size       33 (0x21)
  .maxstack  2
  IL_0000:  ldsfld     ""System.Action <>x.<Target>w""
  IL_0005:  dup
  IL_0006:  brtrue.s   IL_001b
  IL_0008:  pop
  IL_0009:  ldnull
  IL_000a:  ldftn      ""void D.Target()""
  IL_0010:  newobj     ""System.Action..ctor(object, System.IntPtr)""
  IL_0015:  dup
  IL_0016:  stsfld     ""System.Action <>x.<Target>w""
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
            var compilation = CompileAndVerify(source, expectedOutput: PASS).VerifyIL("D<T>.<>c.<Test>b__0_0", @"
{
  // Code size       34 (0x22)
  .maxstack  2
  IL_0000:  ldsfld     ""System.Func<T> D<T>.<>x.<Target>w""
  IL_0005:  dup
  IL_0006:  brtrue.s   IL_001b
  IL_0008:  pop
  IL_0009:  ldnull
  IL_000a:  ldftn      ""T D<T>.Target()""
  IL_0010:  newobj     ""System.Func<T>..ctor(object, System.IntPtr)""
  IL_0015:  dup
  IL_0016:  stsfld     ""System.Func<T> D<T>.<>x.<Target>w""
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
            var compilation = CompileAndVerify(source, expectedOutput: PASS).VerifyIL("D<T>.<>c.<Test>b__1_0", @"
{
  // Code size       28 (0x1c)
  .maxstack  2
  IL_0000:  ldsfld     ""D<T>.MyFunc D<T>.<>x.<Target>w""
  IL_0005:  dup
  IL_0006:  brtrue.s   IL_001b
  IL_0008:  pop
  IL_0009:  ldnull
  IL_000a:  ldftn      ""T E.Target<T>()""
  IL_0010:  newobj     ""D<T>.MyFunc..ctor(object, System.IntPtr)""
  IL_0015:  dup
  IL_0016:  stsfld     ""D<T>.MyFunc D<T>.<>x.<Target>w""
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
            var compilation = CompileAndVerify(source, expectedOutput: PASS).VerifyIL("D<T>.<>c__0<G>.<Test>b__0_0", @"
{
  // Code size       34 (0x22)
  .maxstack  2
  IL_0000:  ldsfld     ""System.Func<T> D<T>.<Test>x<G>.<Target>w""
  IL_0005:  dup
  IL_0006:  brtrue.s   IL_001b
  IL_0008:  pop
  IL_0009:  ldnull
  IL_000a:  ldftn      ""T D<T>.Target<G>()""
  IL_0010:  newobj     ""System.Func<T>..ctor(object, System.IntPtr)""
  IL_0015:  dup
  IL_0016:  stsfld     ""System.Func<T> D<T>.<Test>x<G>.<Target>w""
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
            var compilation = CompileAndVerify(source, expectedOutput: PASS).VerifyIL("D.<>c__0<G>.<Test>b__0_0", @"
{
  // Code size       28 (0x1c)
  .maxstack  2
  IL_0000:  ldsfld     ""System.Func<G> D.<Test>x<G>.<Target>w""
  IL_0005:  dup
  IL_0006:  brtrue.s   IL_001b
  IL_0008:  pop
  IL_0009:  ldnull
  IL_000a:  ldftn      ""G E<G>.Target()""
  IL_0010:  newobj     ""System.Func<G>..ctor(object, System.IntPtr)""
  IL_0015:  dup
  IL_0016:  stsfld     ""System.Func<G> D.<Test>x<G>.<Target>w""
  IL_001b:  ret
}
");
        }

        [Fact]
        public void SameTypeAndSymbolResultsSameField_ModuleScoped0()
        {
            var source = @"
using System;
class C
{
    public static void Main(string[] args)
    {
        Action t0 = Target;
        var t1 = (Action)Target;

        if ( t0 == t1 ) t0();
    }

    static void Target() { Console.WriteLine(""PASS""); }
}
";
            Action<ModuleSymbol> containerValidator = module =>
            {
                var container = module.GlobalNamespace.GetTypeMember("<>x");
                Assert.NotNull(container);
                Assert.Equal(0, container.Arity);

                var members = container.GetMembers();
                Assert.Equal(1, members.Length);

                var field = members[0] as FieldSymbol;
                Assert.NotNull(field);
                Assert.Equal("<Target>w", field.Name);

                var fieldType = field.Type as NamedTypeSymbol;
                Assert.NotNull(fieldType);
                Assert.True(fieldType.IsDelegateType());
                Assert.Equal("System", fieldType.ContainingNamespace.Name);
                Assert.Equal("Action", fieldType.Name);
                Assert.Equal(0, fieldType.Arity);
            };
            var compilation = CompileAndVerify(source, symbolValidator: containerValidator, expectedOutput: PASS);
        }

        [Fact]
        public void SameTypeAndSymbolResultsSameField_ModuleScoped1()
        {
            var source = @"
using System;
class C
{
    void M() { Action<C> t0 = E.Target; }
}
class D
{
    void M() { var t1 = (Action<C>)E.Target; }
}
static class E
{
    public static void Target(this C c) { Console.WriteLine(""PASS""); }
}
";
            Action<ModuleSymbol> containerValidator = module =>
            {
                var container = module.GlobalNamespace.GetTypeMember("<>x");
                Assert.NotNull(container);
                Assert.Equal(0, container.Arity);

                var members = container.GetMembers();
                Assert.Equal(1, members.Length);

                var field = members[0] as FieldSymbol;
                Assert.NotNull(field);
                Assert.Equal("<Target>w", field.Name);

                var fieldType = field.Type as NamedTypeSymbol;
                Assert.NotNull(fieldType);
                Assert.True(fieldType.IsDelegateType());
                Assert.Equal("System", fieldType.ContainingNamespace.Name);
                Assert.Equal("Action", fieldType.Name);
                Assert.Equal(1, fieldType.Arity);
                Assert.Equal(module.GlobalNamespace.GetTypeMember("C"), fieldType.TypeArguments[0]);
            };
            var compilation = CompileAndVerify(source, symbolValidator: containerValidator, additionalRefs: s_SystemCoreRef);
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
            Action<ModuleSymbol> containerValidator = module =>
            {
                var container = module.GlobalNamespace.GetMember<NamedTypeSymbol>("C.<>x");
                Assert.NotNull(container);
                Assert.Equal(0, container.Arity);

                var members = container.GetMembers();
                Assert.Equal(1, members.Length);

                var field = members[0] as FieldSymbol;
                Assert.NotNull(field);
                Assert.Equal("<Target>w", field.Name);

                var fieldType = field.Type as NamedTypeSymbol;
                Assert.NotNull(fieldType);
                Assert.True(fieldType.IsDelegateType());
                Assert.Equal("System", fieldType.ContainingNamespace.Name);
                Assert.Equal("Action", fieldType.Name);
                Assert.Equal(0, fieldType.Arity);
            };
            var compilation = CompileAndVerify(source, symbolValidator: containerValidator);
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
            Action<ModuleSymbol> containerValidator = module =>
            {
                var container = module.GlobalNamespace.GetMember<NamedTypeSymbol>("C.<>x");
                Assert.NotNull(container);
                Assert.Equal(0, container.Arity);

                var members = container.GetMembers();
                Assert.Equal(1, members.Length);

                var field = members[0] as FieldSymbol;
                Assert.NotNull(field);
                Assert.Equal("<Target>w", field.Name);

                var fieldType = field.Type as NamedTypeSymbol;
                Assert.NotNull(fieldType);
                Assert.True(fieldType.IsDelegateType());
                Assert.Equal("System", fieldType.ContainingNamespace.Name);
                Assert.Equal("Func", fieldType.Name);
                Assert.Equal(1, fieldType.Arity);
                Assert.Equal(module.GlobalNamespace.GetTypeMember("C").TypeParameters[0], fieldType.TypeArguments[0]);
            };
            var compilation = CompileAndVerify(source, symbolValidator: containerValidator);
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
            Action<ModuleSymbol> containerValidator = module =>
            {
                var container = module.GlobalNamespace.GetMember<NamedTypeSymbol>("C.<>x");
                Assert.NotNull(container);
                Assert.Equal(0, container.Arity);

                var members = container.GetMembers();
                Assert.Equal(1, members.Length);

                var field = members[0] as FieldSymbol;
                Assert.NotNull(field);
                Assert.Equal("<Target>w", field.Name);

                var fieldType = field.Type as NamedTypeSymbol;
                Assert.Equal(module.GlobalNamespace.GetMember<NamedTypeSymbol>("C.MyFunc"), fieldType);
            };
            var compilation = CompileAndVerify(source, symbolValidator: containerValidator);
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
            Action<ModuleSymbol> containerValidator = module =>
            {
                var container = module.GlobalNamespace.GetMember<NamedTypeSymbol>("C.<Test>x");
                Assert.NotNull(container);
                Assert.True(container.IsGenericType);

                var members = container.GetMembers();
                Assert.Equal(1, members.Length);

                var field = members[0] as FieldSymbol;
                Assert.NotNull(field);
                Assert.Equal("<Target>w", field.Name);

                var fieldType = field.Type as NamedTypeSymbol;
                Assert.NotNull(fieldType);
                Assert.True(fieldType.IsDelegateType());
                Assert.Equal("System", fieldType.ContainingNamespace.Name);
                Assert.Equal("Action", fieldType.Name);
                Assert.Equal(0, fieldType.Arity);
            };
            var compilation = CompileAndVerify(source, symbolValidator: containerValidator);
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
            Action<ModuleSymbol> containerValidator = module =>
            {
                var testClass = module.GlobalNamespace.GetTypeMember("C");
                var container = testClass.GetTypeMember("<Test>x");
                Assert.NotNull(container);
                Assert.Equal(1, container.Arity);

                var members = container.GetMembers();
                Assert.Equal(1, members.Length);

                var field = members[0] as FieldSymbol;
                Assert.NotNull(field);
                Assert.Equal("<Target>w", field.Name);

                var fieldType = field.Type as NamedTypeSymbol;
                Assert.NotNull(fieldType);
                Assert.True(fieldType.IsDelegateType());
                Assert.Equal("System", fieldType.ContainingNamespace.Name);
                Assert.Equal("Func", fieldType.Name);
                Assert.Equal(2, fieldType.Arity);
                Assert.Equal(testClass.TypeParameters[0], fieldType.TypeArguments[0]);
                Assert.Equal(container.TypeParameters[0], fieldType.TypeArguments[1]);
            };
            var compilation = CompileAndVerify(source, symbolValidator: containerValidator);
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
            Action<ModuleSymbol> containerValidator = module =>
            {
                var container = module.GlobalNamespace.GetMember<NamedTypeSymbol>("C.<Test>x__2");
                Assert.NotNull(container);
                Assert.Equal(1, container.Arity);

                var members = container.GetMembers();
                Assert.Equal(1, members.Length);

                var field = members[0] as FieldSymbol;
                Assert.NotNull(field);
                Assert.Equal("<Target>w", field.Name);

                var fieldType = field.Type as NamedTypeSymbol;
                Assert.True(fieldType.IsDelegateType());
                Assert.Equal(module.GlobalNamespace.GetMember<NamedTypeSymbol>("C.MyFunc"), fieldType);
            };
            var compilation = CompileAndVerify(source, symbolValidator: containerValidator, additionalRefs: s_SystemCoreRef);
        }

        [Fact]
        public void ContainersCanBeShared_ModuleScoped0()
        {
            var source = @"
using System;
class C
{
    void Test0()
    {
        Action t = Target0;
    }

    public static void Target0() { }
    public static void Target1() { }
}
class D
{
    void Test0()
    {
        var t = (Action)C.Target1;
    }

    void Test1()
    {
        Action t = Target2;
    }

    static void Target2() { }
}
";
            Action<ModuleSymbol> containerValidator = module =>
            {
                var container = module.GlobalNamespace.GetTypeMember("<>x");
                Assert.NotNull(container);
                Assert.Equal(0, container.Arity);

                var members = container.GetMembers();
                Assert.Equal(3, members.Length);

                var field0 = members[0] as FieldSymbol;
                Assert.NotNull(field0);
                Assert.Equal("<Target0>w", field0.Name);

                var field1 = members[1] as FieldSymbol;
                Assert.NotNull(field1);
                Assert.Equal("<Target1>w__1", field1.Name);

                var field2 = members[2] as FieldSymbol;
                Assert.NotNull(field2);
                Assert.Equal("<Target2>w__2", field2.Name);

                Assert.True(field0.Type == field1.Type);
                Assert.True(field0.Type == field2.Type);
                Assert.True(field1.Type == field2.Type);

                var fieldType = field0.Type as NamedTypeSymbol;
                Assert.NotNull(fieldType);
                Assert.True(fieldType.IsDelegateType());
                Assert.Equal("System", fieldType.ContainingNamespace.Name);
                Assert.Equal("Action", fieldType.Name);
                Assert.Equal(0, fieldType.Arity);
            };
            var compilation = CompileAndVerify(source, symbolValidator: containerValidator);
        }

        [Fact]
        public void ContainersCanBeShared_ModuleScoped1()
        {
            var source = @"
using System;
class C
{
    static void Test0()
    {
        Action<C> t = E.Target0;
    }

    void Test1()
    {
        var t = (Action<C>)E.Target1;
    }
}
static class E
{
    public static void Target0(this C c) { }
    public static void Target1(this C c) { }

    static void Test2()
    {
        Action<C> t = Target0;
    }
}
";
            Action<ModuleSymbol> containerValidator = module =>
            {
                var container = module.GlobalNamespace.GetTypeMember("<>x");
                Assert.NotNull(container);
                Assert.Equal(0, container.Arity);

                var members = container.GetMembers();
                Assert.Equal(2, members.Length);

                var field0 = members[0] as FieldSymbol;
                Assert.NotNull(field0);
                Assert.Equal("<Target0>w", field0.Name);

                var field1 = members[1] as FieldSymbol;
                Assert.NotNull(field1);
                Assert.Equal("<Target1>w__1", field1.Name);

                Assert.True(field0.Type == field1.Type);

                var fieldType = field0.Type as NamedTypeSymbol;
                Assert.NotNull(fieldType);
                Assert.True(fieldType.IsDelegateType());
                Assert.Equal("System", fieldType.ContainingNamespace.Name);
                Assert.Equal("Action", fieldType.Name);
                Assert.Equal(1, fieldType.Arity);
                Assert.Equal(module.GlobalNamespace.GetTypeMember("C"), fieldType.TypeArguments[0]);
            };
            var compilation = CompileAndVerify(source, symbolValidator: containerValidator, additionalRefs: s_SystemCoreRef);
        }

        [Fact]
        public void ContainersCanBeShared_ModuleScoped_GroupByDelegateType()
        {
            var source = @"
using System;
class C
{
    public static void Main(string[] args)
    {
        Action a = Target0;
        var b = (Action)Target1;

        Action<C> c = E.Target0;
        var d = (Action<C>)E.Target1;
    }

    static void Target0() { }
    static void Target1() { }
}
static class E
{
    public static void Target0(this C c) { }
    public static void Target1(this C c) { }
}
";
            Action<ModuleSymbol> containerValidator = module =>
            {
                var container0 = module.GlobalNamespace.GetTypeMember("<>x");
                Assert.NotNull(container0);
                Assert.Equal(0, container0.Arity);

                var members0 = container0.GetMembers();
                Assert.Equal(members0.Length, 2);

                var field00 = members0[0] as FieldSymbol;
                Assert.NotNull(field00);
                Assert.Equal("<Target0>w", field00.Name);

                var field01 = members0[1] as FieldSymbol;
                Assert.NotNull(field01);
                Assert.Equal("<Target1>w__1", field01.Name);

                Assert.True(field00.Type == field01.Type);

                var fieldType0 = field00.Type as NamedTypeSymbol;
                Assert.NotNull(fieldType0);
                Assert.True(fieldType0.IsDelegateType());
                Assert.Equal("System", fieldType0.ContainingNamespace.Name);
                Assert.Equal("Action", fieldType0.Name);
                Assert.Equal(0, fieldType0.Arity);

                var container1 = module.GlobalNamespace.GetTypeMember("<>x__1");
                Assert.NotNull(container1);
                Assert.Equal(0, container1.Arity);

                var members1 = container1.GetMembers();
                Assert.Equal(members1.Length, 2);

                var field10 = members1[0] as FieldSymbol;
                Assert.NotNull(field10);
                Assert.Equal("<Target0>w", field10.Name);

                var field11 = members1[1] as FieldSymbol;
                Assert.NotNull(field11);
                Assert.Equal("<Target1>w__1", field11.Name);

                Assert.True(field10.Type == field11.Type);

                var fieldType1 = field10.Type as NamedTypeSymbol;
                Assert.NotNull(fieldType1);
                Assert.True(fieldType1.IsDelegateType());
                Assert.Equal("System", fieldType1.ContainingNamespace.Name);
                Assert.Equal("Action", fieldType1.Name);
                Assert.Equal(1, fieldType1.Arity);
                Assert.Equal(module.GlobalNamespace.GetTypeMember("C"), fieldType1.TypeArguments[0]);
            };
            var compilation = CompileAndVerify(source, symbolValidator: containerValidator, additionalRefs: s_SystemCoreRef).VerifyIL("C.Main", @"
{
  // Code size       97 (0x61)
  .maxstack  2
  IL_0000:  ldsfld     ""System.Action <>x.<Target0>w""
  IL_0005:  brtrue.s   IL_0018
  IL_0007:  ldnull
  IL_0008:  ldftn      ""void C.Target0()""
  IL_000e:  newobj     ""System.Action..ctor(object, System.IntPtr)""
  IL_0013:  stsfld     ""System.Action <>x.<Target0>w""
  IL_0018:  ldsfld     ""System.Action <>x.<Target1>w__1""
  IL_001d:  brtrue.s   IL_0030
  IL_001f:  ldnull
  IL_0020:  ldftn      ""void C.Target1()""
  IL_0026:  newobj     ""System.Action..ctor(object, System.IntPtr)""
  IL_002b:  stsfld     ""System.Action <>x.<Target1>w__1""
  IL_0030:  ldsfld     ""System.Action<C> <>x__1.<Target0>w""
  IL_0035:  brtrue.s   IL_0048
  IL_0037:  ldnull
  IL_0038:  ldftn      ""void E.Target0(C)""
  IL_003e:  newobj     ""System.Action<C>..ctor(object, System.IntPtr)""
  IL_0043:  stsfld     ""System.Action<C> <>x__1.<Target0>w""
  IL_0048:  ldsfld     ""System.Action<C> <>x__1.<Target1>w__1""
  IL_004d:  brtrue.s   IL_0060
  IL_004f:  ldnull
  IL_0050:  ldftn      ""void E.Target1(C)""
  IL_0056:  newobj     ""System.Action<C>..ctor(object, System.IntPtr)""
  IL_005b:  stsfld     ""System.Action<C> <>x__1.<Target1>w__1""
  IL_0060:  ret
}
");
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
            Action<ModuleSymbol> containerValidator = module =>
            {
                var A = module.GlobalNamespace.GetTypeMember("A");
                var B = A.GetTypeMember("B");
                var T = A.TypeParameters[0];
                var V = B.TypeParameters[0];

                var container = B.GetTypeMember("<>x");
                Assert.NotNull(container);
                Assert.Equal(0, container.Arity);

                var members = container.GetMembers();
                Assert.Equal(7, members.Length);

                var field0 = members[0] as FieldSymbol;
                Assert.NotNull(field0);
                Assert.Equal("<Target0>w", field0.Name);

                var fieldType0 = field0.Type as NamedTypeSymbol;
                Assert.NotNull(fieldType0);
                Assert.True(fieldType0.IsDelegateType());
                Assert.Equal("System", fieldType0.ContainingNamespace.Name);
                Assert.Equal("Action", fieldType0.Name);
                Assert.Equal(1, fieldType0.Arity);
                Assert.Equal(T, fieldType0.TypeArguments[0]);

                var field1 = members[1] as FieldSymbol;
                Assert.NotNull(field1);
                Assert.Equal("<Target1>w__1", field1.Name);

                var fieldType1 = field1.Type as NamedTypeSymbol;
                Assert.NotNull(fieldType1);
                Assert.True(fieldType1.IsDelegateType());
                Assert.Equal("System", fieldType1.ContainingNamespace.Name);
                Assert.Equal("Action", fieldType1.Name);
                Assert.Equal(1, fieldType1.Arity);
                Assert.Equal(T, fieldType1.TypeArguments[0]);

                var field2 = members[2] as FieldSymbol;
                Assert.NotNull(field2);
                Assert.Equal("<Target2>w__2", field2.Name);

                var fieldType2 = field2.Type as NamedTypeSymbol;
                Assert.NotNull(fieldType2);
                Assert.True(fieldType2.IsDelegateType());
                Assert.Equal("System", fieldType2.ContainingNamespace.Name);
                Assert.Equal("Action", fieldType2.Name);
                Assert.Equal(1, fieldType2.Arity);
                Assert.Equal(T, fieldType2.TypeArguments[0]);

                var field3 = members[3] as FieldSymbol;
                Assert.NotNull(field3);
                Assert.Equal("<Target3>w__3", field3.Name);

                var fieldType3 = field3.Type as NamedTypeSymbol;
                Assert.NotNull(fieldType3);
                Assert.True(fieldType3.IsDelegateType());
                Assert.Equal("System", fieldType3.ContainingNamespace.Name);
                Assert.Equal("Action", fieldType3.Name);
                Assert.Equal(2, fieldType3.Arity);
                Assert.Equal(T, fieldType3.TypeArguments[0]);
                Assert.Equal(V, fieldType3.TypeArguments[1]);

                var field4 = members[4] as FieldSymbol;
                Assert.NotNull(field4);
                Assert.Equal("<Target4>w__4", field4.Name);

                var fieldType4 = field4.Type as NamedTypeSymbol;
                Assert.NotNull(fieldType4);
                Assert.True(fieldType4.IsDelegateType());
                Assert.Equal("System", fieldType4.ContainingNamespace.Name);
                Assert.Equal("Action", fieldType4.Name);
                Assert.Equal(2, fieldType4.Arity);
                Assert.Equal(T, fieldType4.TypeArguments[0]);
                Assert.Equal(V, fieldType4.TypeArguments[1]);

                var field5 = members[5] as FieldSymbol;
                Assert.NotNull(field5);
                Assert.Equal("<Target5>w__5", field5.Name);

                var fieldType5 = field5.Type as NamedTypeSymbol;
                Assert.NotNull(fieldType5);
                Assert.True(fieldType5.IsDelegateType());
                Assert.Equal("System", fieldType5.ContainingNamespace.Name);
                Assert.Equal("Action", fieldType5.Name);
                Assert.Equal(1, fieldType5.Arity);
                Assert.Equal(T, fieldType5.TypeArguments[0]);

                var field6 = members[6] as FieldSymbol;
                Assert.NotNull(field6);
                Assert.Equal("<Target5>w__6", field6.Name);

                var fieldType6 = field6.Type as NamedTypeSymbol;
                Assert.NotNull(fieldType6);
                Assert.True(fieldType6.IsDelegateType());
                Assert.Equal("System", fieldType6.ContainingNamespace.Name);
                Assert.Equal("Action", fieldType6.Name);
                Assert.Equal(1, fieldType6.Arity);
                Assert.Equal(V, fieldType6.TypeArguments[0]);
            };
            var compilation = CompileAndVerify(source, symbolValidator: containerValidator, additionalRefs: s_SystemCoreRef);
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
            Action<ModuleSymbol> containerValidator = module =>
            {
                var testClass = module.GlobalNamespace.GetTypeMember("C");
                var container = testClass.GetTypeMember("<Test>x");
                Assert.NotNull(container);

                Assert.Equal(1, container.Arity);
                var T = container.TypeParameters[0];

                var members = container.GetMembers();
                Assert.Equal(4, members.Length);


                var field0 = members[0] as FieldSymbol;
                Assert.NotNull(field0);
                Assert.Equal("<Target0>w", field0.Name);

                var fieldType0 = field0.Type as NamedTypeSymbol;
                Assert.NotNull(fieldType0);
                Assert.True(fieldType0.IsDelegateType());
                Assert.Equal("System", fieldType0.ContainingNamespace.Name);
                Assert.Equal("Action", fieldType0.Name);
                Assert.Equal(0, fieldType0.Arity);

                var field1 = members[1] as FieldSymbol;
                Assert.NotNull(field1);
                Assert.Equal("<Target1>w__1", field1.Name);

                var fieldType1 = field1.Type as NamedTypeSymbol;
                Assert.NotNull(fieldType1);
                Assert.True(fieldType1.IsDelegateType());
                Assert.Equal("System", fieldType1.ContainingNamespace.Name);
                Assert.Equal("Action", fieldType1.Name);
                Assert.Equal(1, fieldType1.Arity);
                Assert.Equal(T, fieldType1.TypeArguments[0]);

                var field2 = members[2] as FieldSymbol;
                Assert.NotNull(field2);
                Assert.Equal("<Target2>w__2", field2.Name);

                var fieldType2 = field2.Type as NamedTypeSymbol;
                Assert.NotNull(fieldType2);
                Assert.True(fieldType2.IsDelegateType());
                Assert.Equal("System", fieldType2.ContainingNamespace.Name);
                Assert.Equal("Action", fieldType2.Name);
                Assert.Equal(0, fieldType2.Arity);

                var field3 = members[3] as FieldSymbol;
                Assert.NotNull(field3);
                Assert.Equal("<Target3>w__3", field3.Name);

                var fieldType3 = field3.Type as NamedTypeSymbol;
                Assert.NotNull(fieldType3);
                Assert.True(fieldType3.IsDelegateType());
                Assert.Equal("System", fieldType3.ContainingNamespace.Name);
                Assert.Equal("Action", fieldType3.Name);
                Assert.Equal(1, fieldType3.Arity);
                Assert.Equal(testClass, fieldType3.TypeArguments[0]);
            };
            var compilation = CompileAndVerify(source, symbolValidator: containerValidator, additionalRefs: s_SystemCoreRef);
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
            Action<ModuleSymbol> containerValidator = module =>
            {
                var testClass = module.GlobalNamespace.GetTypeMember("E");
                var container = testClass.GetTypeMember("<Test>x");
                Assert.NotNull(container);

                Assert.Equal(1, container.Arity);
                var T = container.TypeParameters[0];
                var C = module.GlobalNamespace.GetTypeMember("C");

                var members = container.GetMembers();
                Assert.Equal(4, members.Length);


                var field0 = members[0] as FieldSymbol;
                Assert.NotNull(field0);
                Assert.Equal("<Target0>w", field0.Name);

                var fieldType0 = field0.Type as NamedTypeSymbol;
                Assert.NotNull(fieldType0);
                Assert.True(fieldType0.IsDelegateType());
                Assert.Equal("System", fieldType0.ContainingNamespace.Name);
                Assert.Equal("Action", fieldType0.Name);
                Assert.Equal(0, fieldType0.Arity);

                var field1 = members[1] as FieldSymbol;
                Assert.NotNull(field1);
                Assert.Equal("<Target1>w__1", field1.Name);

                var fieldType1 = field1.Type as NamedTypeSymbol;
                Assert.NotNull(fieldType1);
                Assert.True(fieldType1.IsDelegateType());
                Assert.Equal("System", fieldType1.ContainingNamespace.Name);
                Assert.Equal("Action", fieldType1.Name);
                Assert.Equal(1, fieldType1.Arity);
                Assert.Equal(T, fieldType1.TypeArguments[0]);

                var field2 = members[2] as FieldSymbol;
                Assert.NotNull(field2);
                Assert.Equal("<Target2>w__2", field2.Name);

                var fieldType2 = field2.Type as NamedTypeSymbol;
                Assert.NotNull(fieldType2);
                Assert.True(fieldType2.IsDelegateType());
                Assert.Equal("System", fieldType2.ContainingNamespace.Name);
                Assert.Equal("Action", fieldType2.Name);
                Assert.Equal(0, fieldType2.Arity);

                var field3 = members[3] as FieldSymbol;
                Assert.NotNull(field3);
                Assert.Equal("<Target3>w__3", field3.Name);

                var fieldType3 = field3.Type as NamedTypeSymbol;
                Assert.NotNull(fieldType3);
                Assert.True(fieldType3.IsDelegateType());
                Assert.Equal("System", fieldType3.ContainingNamespace.Name);
                Assert.Equal("Action", fieldType3.Name);
                Assert.Equal(1, fieldType3.Arity);
                Assert.Equal(C, fieldType3.TypeArguments[0]);
            };
            var compilation = CompileAndVerify(source, symbolValidator: containerValidator, additionalRefs: s_SystemCoreRef);
        }

        [Fact]
        public void EventHandlers_ModuleScoped0()
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
            var compilation = CompileAndVerify(source).VerifyIL("C.Test", @"
{
  // Code size       38 (0x26)
  .maxstack  3
  IL_0000:  call       ""System.AppDomain System.AppDomain.CurrentDomain.get""
  IL_0005:  ldsfld     ""System.ResolveEventHandler <>x.<Target>w""
  IL_000a:  dup
  IL_000b:  brtrue.s   IL_0020
  IL_000d:  pop
  IL_000e:  ldnull
  IL_000f:  ldftn      ""System.Reflection.Assembly C.Target(object, System.ResolveEventArgs)""
  IL_0015:  newobj     ""System.ResolveEventHandler..ctor(object, System.IntPtr)""
  IL_001a:  dup
  IL_001b:  stsfld     ""System.ResolveEventHandler <>x.<Target>w""
  IL_0020:  callvirt   ""void System.AppDomain.AssemblyResolve.add""
  IL_0025:  ret
}
");
        }

        [Fact]
        public void EventHandlers_ModuleScoped1()
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
            var compilation = CompileAndVerify(source).VerifyIL("C<TEventArgs>.Test", @"
{
  // Code size       38 (0x26)
  .maxstack  3
  IL_0000:  newobj     ""C<MyEventArgs>..ctor()""
  IL_0005:  ldsfld     ""System.EventHandler<MyEventArgs> <>x.<Target>w""
  IL_000a:  dup
  IL_000b:  brtrue.s   IL_0020
  IL_000d:  pop
  IL_000e:  ldnull
  IL_000f:  ldftn      ""void D.Target(object, MyEventArgs)""
  IL_0015:  newobj     ""System.EventHandler<MyEventArgs>..ctor(object, System.IntPtr)""
  IL_001a:  dup
  IL_001b:  stsfld     ""System.EventHandler<MyEventArgs> <>x.<Target>w""
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
            var compilation = CompileAndVerify(source).VerifyIL("C<TEventArgs>.Test", @"
{
  // Code size       34 (0x22)
  .maxstack  3
  IL_0000:  ldarg.0
  IL_0001:  ldsfld     ""System.EventHandler<TEventArgs> C<TEventArgs>.<>x.<Target>w""
  IL_0006:  dup
  IL_0007:  brtrue.s   IL_001c
  IL_0009:  pop
  IL_000a:  ldnull
  IL_000b:  ldftn      ""void C<TEventArgs>.Target(object, TEventArgs)""
  IL_0011:  newobj     ""System.EventHandler<TEventArgs>..ctor(object, System.IntPtr)""
  IL_0016:  dup
  IL_0017:  stsfld     ""System.EventHandler<TEventArgs> C<TEventArgs>.<>x.<Target>w""
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
            var compilation = CompileAndVerify(source).VerifyIL("C.Test<TEventArgs>", @"
{
  // Code size       38 (0x26)
  .maxstack  3
  IL_0000:  newobj     ""D<TEventArgs>..ctor()""
  IL_0005:  ldsfld     ""System.EventHandler<TEventArgs> C.<Test>x<TEventArgs>.<Target>w""
  IL_000a:  dup
  IL_000b:  brtrue.s   IL_0020
  IL_000d:  pop
  IL_000e:  ldnull
  IL_000f:  ldftn      ""void C.Target<TEventArgs>(object, TEventArgs)""
  IL_0015:  newobj     ""System.EventHandler<TEventArgs>..ctor(object, System.IntPtr)""
  IL_001a:  dup
  IL_001b:  stsfld     ""System.EventHandler<TEventArgs> C.<Test>x<TEventArgs>.<Target>w""
  IL_0020:  callvirt   ""void D<TEventArgs>.SomethingHappened.add""
  IL_0025:  ret
}
");
        }

        [Fact]
        public void AnonymousTypes_ModuleScoped0()
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
            var compilation = CompileAndVerify(source, expectedOutput: "{ x = 0 }").VerifyIL("C.Main", @"
{
  // Code size       39 (0x27)
  .maxstack  3
  IL_0000:  ldc.i4.0
  IL_0001:  newobj     ""<>f__AnonymousType0<int>..ctor(int)""
  IL_0006:  ldsfld     ""System.Action<<anonymous type: int x>> <>x.<Target>w""
  IL_000b:  dup
  IL_000c:  brtrue.s   IL_0021
  IL_000e:  pop
  IL_000f:  ldnull
  IL_0010:  ldftn      ""void C.Target<<anonymous type: int x>>(<anonymous type: int x>)""
  IL_0016:  newobj     ""System.Action<<anonymous type: int x>>..ctor(object, System.IntPtr)""
  IL_001b:  dup
  IL_001c:  stsfld     ""System.Action<<anonymous type: int x>> <>x.<Target>w""
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
            var compilation = CompileAndVerify(source, expectedOutput: "{ x = 0 }").VerifyIL("D<G>.Test", @"
{
  // Code size       39 (0x27)
  .maxstack  3
  IL_0000:  ldarg.1
  IL_0001:  newobj     ""<>f__AnonymousType0<G>..ctor(G)""
  IL_0006:  ldsfld     ""System.Action<<anonymous type: G x>> D<G>.<>x.<Target>w""
  IL_000b:  dup
  IL_000c:  brtrue.s   IL_0021
  IL_000e:  pop
  IL_000f:  ldnull
  IL_0010:  ldftn      ""void D<G>.Target<<anonymous type: G x>>(<anonymous type: G x>)""
  IL_0016:  newobj     ""System.Action<<anonymous type: G x>>..ctor(object, System.IntPtr)""
  IL_001b:  dup
  IL_001c:  stsfld     ""System.Action<<anonymous type: G x>> D<G>.<>x.<Target>w""
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
            var compilation = CompileAndVerify(source, expectedOutput: "{ x = 0 }").VerifyIL("D<G>.Test", @"
{
  // Code size       39 (0x27)
  .maxstack  3
  IL_0000:  ldarg.1
  IL_0001:  newobj     ""<>f__AnonymousType0<G>..ctor(G)""
  IL_0006:  ldsfld     ""System.Action<<anonymous type: G x>> D<G>.<>x.<Target>w""
  IL_000b:  dup
  IL_000c:  brtrue.s   IL_0021
  IL_000e:  pop
  IL_000f:  ldnull
  IL_0010:  ldftn      ""void E.Target<<anonymous type: G x>>(<anonymous type: G x>)""
  IL_0016:  newobj     ""System.Action<<anonymous type: G x>>..ctor(object, System.IntPtr)""
  IL_001b:  dup
  IL_001c:  stsfld     ""System.Action<<anonymous type: G x>> D<G>.<>x.<Target>w""
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
            var compilation = CompileAndVerify(source, expectedOutput: "{ x = 0 }").VerifyIL("D<G>.Test", @"
{
  // Code size       39 (0x27)
  .maxstack  3
  IL_0000:  ldc.i4.0
  IL_0001:  newobj     ""<>f__AnonymousType0<int>..ctor(int)""
  IL_0006:  ldsfld     ""D<G>.MyAction<<anonymous type: int x>> D<G>.<>x.<Target>w""
  IL_000b:  dup
  IL_000c:  brtrue.s   IL_0021
  IL_000e:  pop
  IL_000f:  ldnull
  IL_0010:  ldftn      ""void D<G>.Target<<anonymous type: int x>>(<anonymous type: int x>)""
  IL_0016:  newobj     ""D<G>.MyAction<<anonymous type: int x>>..ctor(object, System.IntPtr)""
  IL_001b:  dup
  IL_001c:  stsfld     ""D<G>.MyAction<<anonymous type: int x>> D<G>.<>x.<Target>w""
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
            var compilation = CompileAndVerify(source, expectedOutput: "{ x = 0 }").VerifyIL("D.Test<T>", @"
{
  // Code size       39 (0x27)
  .maxstack  3
  IL_0000:  ldarg.0
  IL_0001:  newobj     ""<>f__AnonymousType0<T>..ctor(T)""
  IL_0006:  ldsfld     ""System.Action<<anonymous type: T x>> D.<Test>x<T>.<Target>w""
  IL_000b:  dup
  IL_000c:  brtrue.s   IL_0021
  IL_000e:  pop
  IL_000f:  ldnull
  IL_0010:  ldftn      ""void D.Target<<anonymous type: T x>>(<anonymous type: T x>)""
  IL_0016:  newobj     ""System.Action<<anonymous type: T x>>..ctor(object, System.IntPtr)""
  IL_001b:  dup
  IL_001c:  stsfld     ""System.Action<<anonymous type: T x>> D.<Test>x<T>.<Target>w""
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
            var compilation = CompileAndVerify(source, expectedOutput: "{ x = 0 }").VerifyIL("D.Test<T>", @"
{
  // Code size       40 (0x28)
  .maxstack  4
  IL_0000:  ldarg.0
  IL_0001:  ldc.i4.0
  IL_0002:  newobj     ""<>f__AnonymousType0<int>..ctor(int)""
  IL_0007:  ldsfld     ""System.Action<T, <anonymous type: int x>> D.<Test>x<T>.<Target>w""
  IL_000c:  dup
  IL_000d:  brtrue.s   IL_0022
  IL_000f:  pop
  IL_0010:  ldnull
  IL_0011:  ldftn      ""void D.Target<T, <anonymous type: int x>>(T, <anonymous type: int x>)""
  IL_0017:  newobj     ""System.Action<T, <anonymous type: int x>>..ctor(object, System.IntPtr)""
  IL_001c:  dup
  IL_001d:  stsfld     ""System.Action<T, <anonymous type: int x>> D.<Test>x<T>.<Target>w""
  IL_0022:  call       ""void D.Invoke<T, <anonymous type: int x>>(T, <anonymous type: int x>, System.Action<T, <anonymous type: int x>>)""
  IL_0027:  ret
}
");
        }

        [Fact]
        public void Pointer_ModuleScoped0()
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
            var compilation = CompileAndVerify(source, options: TestOptions.UnsafeReleaseDll).VerifyIL("C.Test", @"
{
  // Code size       34 (0x22)
  .maxstack  2
  IL_0000:  ldsfld     ""System.Func<int*[]> <>x.<Target>w""
  IL_0005:  dup
  IL_0006:  brtrue.s   IL_001b
  IL_0008:  pop
  IL_0009:  ldnull
  IL_000a:  ldftn      ""int*[] C.Target()""
  IL_0010:  newobj     ""System.Func<int*[]>..ctor(object, System.IntPtr)""
  IL_0015:  dup
  IL_0016:  stsfld     ""System.Func<int*[]> <>x.<Target>w""
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
            var compilation = CompileAndVerify(source, options: TestOptions.UnsafeReleaseDll).VerifyIL("C<T>.Test", @"
{
  // Code size       43 (0x2b)
  .maxstack  2
  .locals init (T V_0)
  IL_0000:  ldsfld     ""System.Func<T, int*[]> C<T>.<>x.<Target>w""
  IL_0005:  dup
  IL_0006:  brtrue.s   IL_001b
  IL_0008:  pop
  IL_0009:  ldnull
  IL_000a:  ldftn      ""int*[] C<T>.Target(T)""
  IL_0010:  newobj     ""System.Func<T, int*[]>..ctor(object, System.IntPtr)""
  IL_0015:  dup
  IL_0016:  stsfld     ""System.Func<T, int*[]> C<T>.<>x.<Target>w""
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
            var compilation = CompileAndVerify(source, options: TestOptions.UnsafeReleaseDll).VerifyIL("C.Test<T>", @"
{
  // Code size       35 (0x23)
  .maxstack  2
  IL_0000:  ldsfld     ""System.Func<T, int*[]> C.<Test>x<T>.<Target>w""
  IL_0005:  dup
  IL_0006:  brtrue.s   IL_001b
  IL_0008:  pop
  IL_0009:  ldnull
  IL_000a:  ldftn      ""int*[] C.Target<T>(T)""
  IL_0010:  newobj     ""System.Func<T, int*[]>..ctor(object, System.IntPtr)""
  IL_0015:  dup
  IL_0016:  stsfld     ""System.Func<T, int*[]> C.<Test>x<T>.<Target>w""
  IL_001b:  ldarg.1
  IL_001c:  callvirt   ""int*[] System.Func<T, int*[]>.Invoke(T)""
  IL_0021:  pop
  IL_0022:  ret
}
");
        }

        [Fact]
        public void Dynamic_ModuleScoped0()
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
            var compilation = CompileAndVerify(source, additionalRefs: new[] { SystemCoreRef, CSharpRef }).VerifyIL("C.Test", @"
{
  // Code size       34 (0x22)
  .maxstack  2
  IL_0000:  ldsfld     ""System.Func<dynamic> <>x.<Target>w""
  IL_0005:  dup
  IL_0006:  brtrue.s   IL_001b
  IL_0008:  pop
  IL_0009:  ldnull
  IL_000a:  ldftn      ""dynamic C.Target()""
  IL_0010:  newobj     ""System.Func<dynamic>..ctor(object, System.IntPtr)""
  IL_0015:  dup
  IL_0016:  stsfld     ""System.Func<dynamic> <>x.<Target>w""
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
            var compilation = CompileAndVerify(source, additionalRefs: new[] { SystemCoreRef, CSharpRef }).VerifyIL("C<T>.Test", @"
{
  // Code size       43 (0x2b)
  .maxstack  2
  .locals init (T V_0)
  IL_0000:  ldsfld     ""System.Func<T, dynamic> C<T>.<>x.<Target>w""
  IL_0005:  dup
  IL_0006:  brtrue.s   IL_001b
  IL_0008:  pop
  IL_0009:  ldnull
  IL_000a:  ldftn      ""dynamic C<T>.Target(T)""
  IL_0010:  newobj     ""System.Func<T, dynamic>..ctor(object, System.IntPtr)""
  IL_0015:  dup
  IL_0016:  stsfld     ""System.Func<T, dynamic> C<T>.<>x.<Target>w""
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
            var compilation = CompileAndVerify(source, additionalRefs: new[] { SystemCoreRef, CSharpRef }).VerifyIL("C.Test<T>", @"
{
  // Code size       35 (0x23)
  .maxstack  2
  IL_0000:  ldsfld     ""System.Func<T, dynamic> C.<Test>x<T>.<Target>w""
  IL_0005:  dup
  IL_0006:  brtrue.s   IL_001b
  IL_0008:  pop
  IL_0009:  ldnull
  IL_000a:  ldftn      ""dynamic C.Target<T>(T)""
  IL_0010:  newobj     ""System.Func<T, dynamic>..ctor(object, System.IntPtr)""
  IL_0015:  dup
  IL_0016:  stsfld     ""System.Func<T, dynamic> C.<Test>x<T>.<Target>w""
  IL_001b:  ldarg.1
  IL_001c:  callvirt   ""dynamic System.Func<T, dynamic>.Invoke(T)""
  IL_0021:  pop
  IL_0022:  ret
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
  IL_000c:  ldsfld     ""System.Func<int> <>x.<M>w""
  IL_0011:  dup
  IL_0012:  brtrue.s   IL_002a
  IL_0014:  pop
  IL_0015:  ldnull
  IL_0016:  ldftn      ""int C.M()""
  IL_001c:  newobj     ""System.Func<int>..ctor(object, System.IntPtr)""
  IL_0021:  dup
  IL_0022:  stsfld     ""System.Func<int> <>x.<M>w""
  IL_0027:  br.s       IL_002a
  IL_0029:  ldloc.0
  IL_002a:  call       ""void System.Console.WriteLine(object)""
  IL_002f:  brtrue.s   IL_0034
  IL_0031:  ldloc.0
  IL_0032:  br.s       IL_004f
  IL_0034:  ldsfld     ""System.Func<int> <>x.<M>w""
  IL_0039:  dup
  IL_003a:  brtrue.s   IL_004f
  IL_003c:  pop
  IL_003d:  ldnull
  IL_003e:  ldftn      ""int C.M()""
  IL_0044:  newobj     ""System.Func<int>..ctor(object, System.IntPtr)""
  IL_0049:  dup
  IL_004a:  stsfld     ""System.Func<int> <>x.<M>w""
  IL_004f:  call       ""void System.Console.WriteLine(object)""
  IL_0054:  ret
}
");
        }

        #region Potential breaking changes

        [Fact]
        public void LockDelegate()
        {
            var text = @"
delegate void D(int p1);
partial class Test
{
    public static void Main()
    {
        D d1;
        lock (d1 = PM)
        {
        }
    }
    static partial void PM(int p2);
    static partial void PM(int p2)
    {
    }
}";
            CompileAndVerify(text);
        }

        [Fact]
        public void LockDelegate1()
        {
            var text = @"
delegate void D(int p1);
partial class Test
{
    public static void Main()
    {
        D d1;
        lock (d1 = (D)PM)
        {
        }
    }
    static partial void PM(int p2);
    static partial void PM(int p2)
    {
    }
}";
            CompileAndVerify(text);
        }

        #endregion

    }
}
