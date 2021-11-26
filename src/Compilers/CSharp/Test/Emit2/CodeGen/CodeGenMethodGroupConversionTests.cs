// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

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
            var compilation = CompileAndVerify(source, symbolValidator: containerValidator, expectedOutput: PASS, references: s_SystemCoreRef);
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
}";
            Action<ModuleSymbol> containerValidator = module =>
            {
                Assert.Null(module.GlobalNamespace.GetMember<NamedTypeSymbol>("<>x"));
            };
            var compilation = CompileAndVerify(source, symbolValidator: containerValidator, expectedOutput: PASS, references: s_SystemCoreRef);
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
            var compilation = CompileAndVerify(source, symbolValidator: containerValidator, references: s_SystemCoreRef);
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
}";
            Action<ModuleSymbol> containerValidator = module =>
            {
                Assert.Null(module.GlobalNamespace.GetMember<NamedTypeSymbol>("<>x"));
            };
            var compilation = CompileAndVerify(source, symbolValidator: containerValidator, references: s_SystemCoreRef);
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
}";
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
}";
            var compilation = CompileAndVerify(source, expectedOutput: PASS);
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
}";
            var compilation = CompileAndVerify(source, expectedOutput: PASS);
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
}";
            var compilation = CompileAndVerify(source, expectedOutput: PASS);
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
}";
            var compilation = CompileAndVerify(source, expectedOutput: PASS);
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
}";
            var compilation = CompileAndVerify(source, expectedOutput: PASS);
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
}";
            var compilation = CompileAndVerify(source, expectedOutput: PASS);
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
}";
            var compilation = CompileAndVerify(source, expectedOutput: PASS);
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
}";
            var compilation = CompileAndVerify(source, expectedOutput: PASS);
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
}";
            var compilation = CompileAndVerify(source, expectedOutput: PASS);
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
}";
            var compilation = CompileAndVerify(source, expectedOutput: PASS);
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
            var compilation = CompileAndVerify(source, expectedOutput: PASS);
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
            var compilation = CompileAndVerify(source, expectedOutput: PASS);
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
            var compilation = CompileAndVerify(source, expectedOutput: PASS);
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
            var compilation = CompileAndVerify(source, expectedOutput: PASS);
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
            var compilation = CompileAndVerify(source, expectedOutput: PASS);
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
            var compilation = CompileAndVerify(source, expectedOutput: PASS);
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
            var compilation = CompileAndVerify(source, expectedOutput: PASS);
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
            var compilation = CompileAndVerify(source, expectedOutput: PASS);
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
            var compilation = CompileAndVerify(source, expectedOutput: PASS);
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
            var compilation = CompileAndVerify(source, expectedOutput: PASS);
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
            var compilation = CompileAndVerify(source, expectedOutput: PASS);
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
            var compilation = CompileAndVerify(source, expectedOutput: PASS);
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
            var compilation = CompileAndVerify(source, expectedOutput: PASS);
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
            var compilation = CompileAndVerify(source, expectedOutput: PASS);
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
            var compilation = CompileAndVerify(source, expectedOutput: PASS);
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
            var compilation = CompileAndVerify(source, expectedOutput: PASS);
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
            var compilation = CompileAndVerify(source, expectedOutput: PASS);
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
            var compilation = CompileAndVerify(source, expectedOutput: PASS);
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
            var compilation = CompileAndVerify(source, expectedOutput: PASS);
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
            var compilation = CompileAndVerify(source, expectedOutput: PASS);
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
            var compilation = CompileAndVerify(source, expectedOutput: PASS);
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
            var compilation = CompileAndVerify(source, expectedOutput: PASS);
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
            var compilation = CompileAndVerify(source, expectedOutput: PASS);
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
                Assert.Equal(1, m.ConstraintTypes().Length);
                Assert.Equal(testClass.TypeParameters[0], m.ConstraintTypes()[0]);
            };
            var compilation = CompileAndVerify(source, symbolValidator: containerValidator, expectedOutput: PASS);
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
                Assert.Equal(1, m.ConstraintTypes().Length);
                Assert.Equal(mainClass, m.ConstraintTypes()[0]);
            };
            var compilation = CompileAndVerify(source, symbolValidator: containerValidator, expectedOutput: PASS);
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

                var m = typeParameters[0];
                Assert.NotNull(m);
                Assert.True(m.IsValueType);
            };
            var compilation = CompileAndVerify(source, symbolValidator: containerValidator, expectedOutput: PASS);
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
            var compilation = CompileAndVerify(source, expectedOutput: PASS, references: s_SystemCoreRef);
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
            var compilation = CompileAndVerify(source, expectedOutput: PASS, references: s_SystemCoreRef);
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
            var compilation = CompileAndVerify(source, references: s_SystemCoreRef);
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
            var compilation = CompileAndVerify(source, expectedOutput: PASS, references: s_SystemCoreRef);
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
            var compilation = CompileAndVerify(source, expectedOutput: PASS, references: s_SystemCoreRef);
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
            var compilation = CompileAndVerify(source, references: s_SystemCoreRef);
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
            var compilation = CompileAndVerify(source, expectedOutput: PASS, references: s_SystemCoreRef);
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
            var compilation = CompileAndVerify(source, expectedOutput: PASS, references: s_SystemCoreRef);
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
            var compilation = CompileAndVerify(source, references: s_SystemCoreRef);
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
            var compilation = CompileAndVerify(source, expectedOutput: PASS);
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
            var compilation = CompileAndVerify(source, expectedOutput: PASS);
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
            var compilation = CompileAndVerify(source, expectedOutput: PASS);
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
            var compilation = CompileAndVerify(source, expectedOutput: PASS);
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
            var compilation = CompileAndVerify(source, expectedOutput: PASS);
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
            var compilation = CompileAndVerify(source, expectedOutput: PASS);
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
                Assert.Equal("<Target>w", field!.Name);

                var fieldType = field.Type as NamedTypeSymbol;
                Assert.NotNull(fieldType);
                Assert.True(fieldType!.IsDelegateType());
                Assert.Equal("System", fieldType!.ContainingNamespace.Name);
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
                Assert.Equal("<Target>w", field!.Name);

                var fieldType = field.Type as NamedTypeSymbol;
                Assert.NotNull(fieldType);
                Assert.True(fieldType!.IsDelegateType());
                Assert.Equal("System", fieldType!.ContainingNamespace.Name);
                Assert.Equal("Action", fieldType.Name);
                Assert.Equal(1, fieldType.Arity);
                Assert.Equal(module.GlobalNamespace.GetTypeMember("C"), fieldType.TypeArguments()[0]);
            };
            var compilation = CompileAndVerify(source, symbolValidator: containerValidator, references: s_SystemCoreRef);
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
                Assert.Equal("<Target>w", field!.Name);

                var fieldType = field.Type as NamedTypeSymbol;
                Assert.NotNull(fieldType);
                Assert.True(fieldType!.IsDelegateType());
                Assert.Equal("System", fieldType!.ContainingNamespace.Name);
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
                Assert.Equal("<Target>w", field!.Name);

                var fieldType = field.Type as NamedTypeSymbol;
                Assert.NotNull(fieldType);
                Assert.True(fieldType!.IsDelegateType());
                Assert.Equal("System", fieldType!.ContainingNamespace.Name);
                Assert.Equal("Func", fieldType.Name);
                Assert.Equal(1, fieldType.Arity);
                Assert.Equal(module.GlobalNamespace.GetTypeMember("C").TypeParameters[0], fieldType.TypeArguments()[0]);
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
                Assert.Equal("<Target>w", field!.Name);

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
                Assert.Equal("<Target>w", field!.Name);

                var fieldType = field.Type as NamedTypeSymbol;
                Assert.NotNull(fieldType);
                Assert.True(fieldType!.IsDelegateType());
                Assert.Equal("System", fieldType!.ContainingNamespace.Name);
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
                Assert.Equal("<Target>w", field!.Name);

                var fieldType = field.Type as NamedTypeSymbol;
                Assert.NotNull(fieldType);
                Assert.True(fieldType!.IsDelegateType());
                Assert.Equal("System", fieldType!.ContainingNamespace.Name);
                Assert.Equal("Func", fieldType.Name);
                Assert.Equal(2, fieldType.Arity);
                Assert.Equal(testClass.TypeParameters[0], fieldType.TypeArguments()[0]);
                Assert.Equal(container.TypeParameters[0], fieldType.TypeArguments()[1]);
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
                Assert.Equal("<Target>w", field!.Name);

                var fieldType = field.Type as NamedTypeSymbol;
                Assert.True(fieldType!.IsDelegateType());
                Assert.Equal(module.GlobalNamespace.GetMember<NamedTypeSymbol>("C.MyFunc"), fieldType);
            };
            var compilation = CompileAndVerify(source, symbolValidator: containerValidator, references: s_SystemCoreRef);
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
                Assert.Equal("<Target0>w", field0!.Name);

                var field1 = members[1] as FieldSymbol;
                Assert.NotNull(field1);
                Assert.Equal("<Target1>w__1", field1!.Name);

                var field2 = members[2] as FieldSymbol;
                Assert.NotNull(field2);
                Assert.Equal("<Target2>w__2", field2!.Name);

                Assert.True(TypeSymbol.Equals(field0.Type, field1.Type, TypeCompareKind.ConsiderEverything));
                Assert.True(TypeSymbol.Equals(field0.Type, field2.Type, TypeCompareKind.ConsiderEverything));
                Assert.True(TypeSymbol.Equals(field1.Type, field2.Type, TypeCompareKind.ConsiderEverything));

                var fieldType = field0.Type as NamedTypeSymbol;
                Assert.NotNull(fieldType);
                Assert.True(fieldType!.IsDelegateType());
                Assert.Equal("System", fieldType!.ContainingNamespace.Name);
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
                Assert.Equal("<Target0>w", field0!.Name);

                var field1 = members[1] as FieldSymbol;
                Assert.NotNull(field1);
                Assert.Equal("<Target1>w__1", field1!.Name);

                Assert.True(TypeSymbol.Equals(field0.Type, field1.Type, TypeCompareKind.ConsiderEverything));

                var fieldType = field0.Type as NamedTypeSymbol;
                Assert.NotNull(fieldType);
                Assert.True(fieldType!.IsDelegateType());
                Assert.Equal("System", fieldType!.ContainingNamespace.Name);
                Assert.Equal("Action", fieldType.Name);
                Assert.Equal(1, fieldType.Arity);
                Assert.Equal(module.GlobalNamespace.GetTypeMember("C"), fieldType.TypeArguments()[0]);
            };
            var compilation = CompileAndVerify(source, symbolValidator: containerValidator, references: s_SystemCoreRef);
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
                Assert.Equal(2, members0.Length);

                var field00 = members0[0] as FieldSymbol;
                Assert.NotNull(field00);
                Assert.Equal("<Target0>w", field00!.Name);

                var field01 = members0[1] as FieldSymbol;
                Assert.NotNull(field01);
                Assert.Equal("<Target1>w__1", field01!.Name);

                Assert.True(TypeSymbol.Equals(field00.Type, field01.Type, TypeCompareKind.ConsiderEverything));

                var fieldType0 = field00.Type as NamedTypeSymbol;
                Assert.NotNull(fieldType0);
                Assert.True(fieldType0!.IsDelegateType());
                Assert.Equal("System", fieldType0!.ContainingNamespace.Name);
                Assert.Equal("Action", fieldType0.Name);
                Assert.Equal(0, fieldType0.Arity);

                var container1 = module.GlobalNamespace.GetTypeMember("<>x__1");
                Assert.NotNull(container1);
                Assert.Equal(0, container1.Arity);

                var members1 = container1.GetMembers();
                Assert.Equal(2, members1.Length);

                var field10 = members1[0] as FieldSymbol;
                Assert.NotNull(field10);
                Assert.Equal("<Target0>w", field10!.Name);

                var field11 = members1[1] as FieldSymbol;
                Assert.NotNull(field11);
                Assert.Equal("<Target1>w__1", field11!.Name);

                Assert.True(TypeSymbol.Equals(field10.Type, field11.Type, TypeCompareKind.ConsiderEverything));

                var fieldType1 = field10.Type as NamedTypeSymbol;
                Assert.NotNull(fieldType1);
                Assert.True(fieldType1!.IsDelegateType());
                Assert.Equal("System", fieldType1!.ContainingNamespace.Name);
                Assert.Equal("Action", fieldType1.Name);
                Assert.Equal(1, fieldType1.Arity);
                Assert.Equal(module.GlobalNamespace.GetTypeMember("C"), fieldType1.TypeArguments()[0]);
            };
            var compilation = CompileAndVerify(source, symbolValidator: containerValidator, references: s_SystemCoreRef);
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
                Assert.Equal("<Target0>w", field0!.Name);

                var fieldType0 = field0.Type as NamedTypeSymbol;
                Assert.NotNull(fieldType0);
                Assert.True(fieldType0!.IsDelegateType());
                Assert.Equal("System", fieldType0!.ContainingNamespace.Name);
                Assert.Equal("Action", fieldType0.Name);
                Assert.Equal(1, fieldType0.Arity);
                Assert.Equal(T, fieldType0.TypeArguments()[0]);

                var field1 = members[1] as FieldSymbol;
                Assert.NotNull(field1);
                Assert.Equal("<Target1>w__1", field1!.Name);

                var fieldType1 = field1.Type as NamedTypeSymbol;
                Assert.NotNull(fieldType1);
                Assert.True(fieldType1!.IsDelegateType());
                Assert.Equal("System", fieldType1!.ContainingNamespace.Name);
                Assert.Equal("Action", fieldType1.Name);
                Assert.Equal(1, fieldType1.Arity);
                Assert.Equal(T, fieldType1.TypeArguments()[0]);

                var field2 = members[2] as FieldSymbol;
                Assert.NotNull(field2);
                Assert.Equal("<Target2>w__2", field2!.Name);

                var fieldType2 = field2.Type as NamedTypeSymbol;
                Assert.NotNull(fieldType2);
                Assert.True(fieldType2!.IsDelegateType());
                Assert.Equal("System", fieldType2!.ContainingNamespace.Name);
                Assert.Equal("Action", fieldType2.Name);
                Assert.Equal(1, fieldType2.Arity);
                Assert.Equal(T, fieldType2.TypeArguments()[0]);

                var field3 = members[3] as FieldSymbol;
                Assert.NotNull(field3);
                Assert.Equal("<Target3>w__3", field3!.Name);

                var fieldType3 = field3.Type as NamedTypeSymbol;
                Assert.NotNull(fieldType3);
                Assert.True(fieldType3!.IsDelegateType());
                Assert.Equal("System", fieldType3!.ContainingNamespace.Name);
                Assert.Equal("Action", fieldType3.Name);
                Assert.Equal(2, fieldType3.Arity);
                Assert.Equal(T, fieldType3.TypeArguments()[0]);
                Assert.Equal(V, fieldType3.TypeArguments()[1]);

                var field4 = members[4] as FieldSymbol;
                Assert.NotNull(field4);
                Assert.Equal("<Target4>w__4", field4!.Name);

                var fieldType4 = field4.Type as NamedTypeSymbol;
                Assert.NotNull(fieldType4);
                Assert.True(fieldType4!.IsDelegateType());
                Assert.Equal("System", fieldType4!.ContainingNamespace.Name);
                Assert.Equal("Action", fieldType4.Name);
                Assert.Equal(2, fieldType4.Arity);
                Assert.Equal(T, fieldType4.TypeArguments()[0]);
                Assert.Equal(V, fieldType4.TypeArguments()[1]);

                var field5 = members[5] as FieldSymbol;
                Assert.NotNull(field5);
                Assert.Equal("<Target5>w__5", field5!.Name);

                var fieldType5 = field5.Type as NamedTypeSymbol;
                Assert.NotNull(fieldType5);
                Assert.True(fieldType5!.IsDelegateType());
                Assert.Equal("System", fieldType5!.ContainingNamespace.Name);
                Assert.Equal("Action", fieldType5.Name);
                Assert.Equal(1, fieldType5.Arity);
                Assert.Equal(T, fieldType5.TypeArguments()[0]);

                var field6 = members[6] as FieldSymbol;
                Assert.NotNull(field6);
                Assert.Equal("<Target5>w__6", field6!.Name);

                var fieldType6 = field6.Type as NamedTypeSymbol;
                Assert.NotNull(fieldType6);
                Assert.True(fieldType6!.IsDelegateType());
                Assert.Equal("System", fieldType6!.ContainingNamespace.Name);
                Assert.Equal("Action", fieldType6.Name);
                Assert.Equal(1, fieldType6.Arity);
                Assert.Equal(V, fieldType6.TypeArguments()[0]);
            };
            var compilation = CompileAndVerify(source, symbolValidator: containerValidator, references: s_SystemCoreRef);
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
                Assert.Equal("<Target0>w", field0!.Name);

                var fieldType0 = field0.Type as NamedTypeSymbol;
                Assert.NotNull(fieldType0);
                Assert.True(fieldType0!.IsDelegateType());
                Assert.Equal("System", fieldType0!.ContainingNamespace.Name);
                Assert.Equal("Action", fieldType0.Name);
                Assert.Equal(0, fieldType0.Arity);

                var field1 = members[1] as FieldSymbol;
                Assert.NotNull(field1);
                Assert.Equal("<Target1>w__1", field1!.Name);

                var fieldType1 = field1.Type as NamedTypeSymbol;
                Assert.NotNull(fieldType1);
                Assert.True(fieldType1!.IsDelegateType());
                Assert.Equal("System", fieldType1!.ContainingNamespace.Name);
                Assert.Equal("Action", fieldType1.Name);
                Assert.Equal(1, fieldType1.Arity);
                Assert.Equal(T, fieldType1.TypeArguments()[0]);

                var field2 = members[2] as FieldSymbol;
                Assert.NotNull(field2);
                Assert.Equal("<Target2>w__2", field2!.Name);

                var fieldType2 = field2.Type as NamedTypeSymbol;
                Assert.NotNull(fieldType2);
                Assert.True(fieldType2!.IsDelegateType());
                Assert.Equal("System", fieldType2!.ContainingNamespace.Name);
                Assert.Equal("Action", fieldType2.Name);
                Assert.Equal(0, fieldType2.Arity);

                var field3 = members[3] as FieldSymbol;
                Assert.NotNull(field3);
                Assert.Equal("<Target3>w__3", field3!.Name);

                var fieldType3 = field3.Type as NamedTypeSymbol;
                Assert.NotNull(fieldType3);
                Assert.True(fieldType3!.IsDelegateType());
                Assert.Equal("System", fieldType3!.ContainingNamespace.Name);
                Assert.Equal("Action", fieldType3.Name);
                Assert.Equal(1, fieldType3.Arity);
                Assert.Equal(testClass, fieldType3.TypeArguments()[0]);
            };
            var compilation = CompileAndVerify(source, symbolValidator: containerValidator, references: s_SystemCoreRef);
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
                Assert.Equal("<Target0>w", field0!.Name);

                var fieldType0 = field0.Type as NamedTypeSymbol;
                Assert.NotNull(fieldType0);
                Assert.True(fieldType0!.IsDelegateType());
                Assert.Equal("System", fieldType0!.ContainingNamespace.Name);
                Assert.Equal("Action", fieldType0.Name);
                Assert.Equal(0, fieldType0.Arity);

                var field1 = members[1] as FieldSymbol;
                Assert.NotNull(field1);
                Assert.Equal("<Target1>w__1", field1!.Name);

                var fieldType1 = field1.Type as NamedTypeSymbol;
                Assert.NotNull(fieldType1);
                Assert.True(fieldType1!.IsDelegateType());
                Assert.Equal("System", fieldType1!.ContainingNamespace.Name);
                Assert.Equal("Action", fieldType1.Name);
                Assert.Equal(1, fieldType1.Arity);
                Assert.Equal(T, fieldType1.TypeArguments()[0]);

                var field2 = members[2] as FieldSymbol;
                Assert.NotNull(field2);
                Assert.Equal("<Target2>w__2", field2!.Name);

                var fieldType2 = field2.Type as NamedTypeSymbol;
                Assert.NotNull(fieldType2);
                Assert.True(fieldType2!.IsDelegateType());
                Assert.Equal("System", fieldType2!.ContainingNamespace.Name);
                Assert.Equal("Action", fieldType2.Name);
                Assert.Equal(0, fieldType2.Arity);

                var field3 = members[3] as FieldSymbol;
                Assert.NotNull(field3);
                Assert.Equal("<Target3>w__3", field3!.Name);

                var fieldType3 = field3.Type as NamedTypeSymbol;
                Assert.NotNull(fieldType3);
                Assert.True(fieldType3!.IsDelegateType());
                Assert.Equal("System", fieldType3!.ContainingNamespace.Name);
                Assert.Equal("Action", fieldType3.Name);
                Assert.Equal(1, fieldType3.Arity);
                Assert.Equal(C, fieldType3.TypeArguments()[0]);
            };
            var compilation = CompileAndVerify(source, symbolValidator: containerValidator, references: s_SystemCoreRef);
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
            var compilation = CompileAndVerify(source);
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
            var compilation = CompileAndVerify(source);
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
            var compilation = CompileAndVerify(source);
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
            var compilation = CompileAndVerify(source);
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
            var compilation = CompileAndVerify(source, expectedOutput: "{ x = 0 }");
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
            var compilation = CompileAndVerify(source, expectedOutput: "{ x = 0 }");
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
            var compilation = CompileAndVerify(source, expectedOutput: "{ x = 0 }");
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
            var compilation = CompileAndVerify(source, expectedOutput: "{ x = 0 }");
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
            var compilation = CompileAndVerify(source, expectedOutput: "{ x = 0 }");
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
            var compilation = CompileAndVerify(source, expectedOutput: "{ x = 0 }");
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
            var compilation = CompileAndVerify(source, options: TestOptions.UnsafeReleaseDll);
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
            var compilation = CompileAndVerify(source, options: TestOptions.UnsafeReleaseDll);
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
            var compilation = CompileAndVerify(source, options: TestOptions.UnsafeReleaseDll);
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
            var compilation = CompileAndVerify(source, references: new[] { SystemCoreRef, CSharpRef });
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
}";
            var compilation = CompileAndVerify(source, references: new[] { SystemCoreRef, CSharpRef });
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
}";
            var compilation = CompileAndVerify(source, references: new[] { SystemCoreRef, CSharpRef });
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
