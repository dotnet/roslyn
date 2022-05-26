// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.CSharp.Test.Utilities;
using Microsoft.CodeAnalysis.CSharp.UnitTests.Emit;
using Microsoft.CodeAnalysis.Test.Utilities;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.CSharp.UnitTests.CodeGen
{
    public class CodeGenExplicitImplementationTests : CSharpTestBase
    {
        [Fact]
        public void TestExplicitInterfaceMethodImplementationSource()
        {
            var source = @"
interface I1
{
    void Method();
}

interface I2
{
    void Method();
}

class C : I1, I2
{
    public void Method()
    {
        System.Console.WriteLine(""C.Method"");
    }

    void I1.Method()
    {
        System.Console.WriteLine(""I1.Method"");
    }

    void I2.Method()
    {
        System.Console.WriteLine(""I2.Method"");
    }

    static void Main()
    {
        C c = new C();
        c.Method();
        ((I1)c).Method();
        ((I2)c).Method();
    }
}
";
            CompileAndVerify(source, expectedOutput: @"
C.Method
I1.Method
I2.Method
");
        }

        [Fact]
        public void TestExplicitInterfacePropertyImplementationSource()
        {
            var text1 = @"
public interface I1
{
    string Property { get; }
}

public interface I2
{
    string Property { get; }
}

public class C : I1, I2
{
    public string Property
    {
        get
        {
            return ""C.Property"";
        }
    }

    string I1.Property
    {
        get
        {
            return ""I1.Property"";
        }
    }

    string I2.Property
    {
        get
        {
            return ""I2.Property"";
        }
    }
}
";
            var text2 = @"
class Test
{
    static void Main()
    {
        C c = new C();
        System.Console.WriteLine(c.Property);
        System.Console.WriteLine(((I1)c).Property);
        System.Console.WriteLine(((I2)c).Property);
    }
}
";

            var comp1 = CreateCompilation(text1, assemblyName: "OHI_ExplicitImplProp1");

            var comp = CreateCompilation(
                text2,
                references: new[] { comp1.EmitToImageReference() },
                options: TestOptions.ReleaseExe,
                assemblyName: "OHI_ExplicitImplProp3");

            CompileAndVerify(comp, expectedOutput: @"
C.Property
I1.Property
I2.Property
");
        }

        [WorkItem(540431, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/540431")]
        [Fact]
        public void TestExpImpInterfaceImplementationMetadata()
        {
            #region "Impl"
            var text1 = @"using System;
    public class CSIMeth02Impl : IMeth02
    {
        #region Explicit
        void IMeth01.Sub01(params byte[] ary)
        {
            Console.Write(""CSS1Exp "");
        }
        void IMeth02.Sub01(sbyte p1, params byte[] ary)
        {
            Console.Write(""CSS11Exp "");
        }

        string IMeth01.Func01(params string[] ary)
        {
            Console.Write(""CSF1Exp "");
            return ary[0];
        }
        string IMeth02.Func01(object p1, params string[] ary)
        {
            Console.Write(""CSF11Exp "");
            return p1.ToString();
        }
        #endregion

        #region Implicit
        public void Sub01(params byte[] ary)
        {
            Console.Write(""CSS1Imp "");
        }
        public void Sub01(sbyte p1, params byte[] ary)
        {
            Console.Write(""CSS11Imp "");
        }

        public string Func01(params string[] ary)
        {
            Console.Write(""CSF1Imp "");
            return ary[0];
        }
        public string Func01(object p1, params string[] ary)
        {
            Console.Write(""CSF11Imp "");
            return p1.ToString();
        }
        #endregion
    }
";
            #endregion

            var text2 = @"
class Test
{
    static void Main()
    {
        CSIMeth02Impl obj = new CSIMeth02Impl();

        obj.Sub01(1, 0, 111);
        ((IMeth01)obj).Sub01(1, 0, 123);
        ((IMeth02)obj).Sub01(1, 0, 127);

        obj.Func01(""A"", ""B"");
        ((IMeth01)obj).Func01(""A"", ""B"");
        ((IMeth02)obj).Func01(""A"", ""B"");
    }
}
";
            var comp1 = CreateCompilation(
                text1,
                references: new[] { TestReferences.MetadataTests.InterfaceAndClass.VBInterfaces01 },
                assemblyName: "OHI_ExpImpImpl001",
                options: TestOptions.ReleaseDll);

            var comp = CreateCompilation(
                text2,
                references: new MetadataReference[]
                {
                    TestReferences.MetadataTests.InterfaceAndClass.VBInterfaces01,
                    new CSharpCompilationReference(comp1)
                },
                assemblyName: "OHI_ExpImpImpl002",
                options: TestOptions.ReleaseExe);

            CompileAndVerify(comp, expectedOutput: @"CSS11Imp CSS1Exp CSS11Exp CSF1Imp CSF1Exp CSF11Exp");
        }

        [WorkItem(540431, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/540431")]
        [Fact]
        public void TestVBInterfaceImplementationMetadata()
        {
            var text = @"
class Test
{
    static void Main()
    {
        VBIMeth02Impl obj = new VBIMeth02Impl();

        obj.Sub01(1, 0, 111);
        ((IMeth01)obj).Sub01(1, 0, 123);
        ((IMeth02)obj).Sub01(1, 0, 127);

        obj.Func01(""A"", ""B"");
        ((IMeth01)obj).Func01(""A"", ""B"");
        ((IMeth02)obj).Func01(""A"", ""B"");
    }
}
";
            var asm01 = TestReferences.MetadataTests.InterfaceAndClass.VBInterfaces01;
            var asm02 = TestReferences.MetadataTests.InterfaceAndClass.VBClasses01;

            var comp = CreateCompilation(
                text,
                references: new[] { asm01, asm02 },
                assemblyName: "OHI_ExpImpVBImpl001",
                options: TestOptions.ReleaseExe);

            CompileAndVerify(comp, expectedOutput: @"VBS1_V VBS1_V VBS11_OL VBF1_V VBF1_V VBF11");
        }

        [WorkItem(540431, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/540431")]
        [Fact]
        public void TestExpImpInterfaceImplementationPropMetadata()
        {
            #region "Impl"
            var text1 = @"using System;
public class CSIPropImpl : IProp
{
    private string _str;
    private uint _uint = 22;
    #region implicit
    public string ReadOnlyProp
    {
        get { return _str; }
    }

    public string WriteOnlyProp
    {
        set { _str = value; }
    }

    public virtual string NormalProp
    {
        get { return _str; }
        set { _str = value; }
    }

    public virtual uint get_ReadOnlyPropWithParams(ushort x)
    {
        return _uint;
    }

    public virtual void set_WriteOnlyPropWithParams(uint x, ulong y)
    {
        _uint = x;
    }

    public long get_NormalPropWithParams(long x, short y)
    {
        return (long)_uint;
    }

    public void set_NormalPropWithParams(long x, short y, long z)
    {
        _uint = (uint)x;
    }
    #endregion

    #region explicit
    string IProp.ReadOnlyProp
    {
        get { return _str; }
    }
    string IProp.WriteOnlyProp
    {
        set { _str = value; }
    }
    string IProp.NormalProp
    {
        get { return _str; }
        set { _str = value; }
    }

    uint IProp.get_ReadOnlyPropWithParams(ushort x)
    {
        return _uint;
    }
    void IProp.set_WriteOnlyPropWithParams(uint x, ulong y)
    {
        _uint = x;
    }
    long IProp.get_NormalPropWithParams(long x, short y)
    {
        return (long)_uint;
    }
    void IProp.set_NormalPropWithParams(long x, short y, long z)
    {
        _uint = (uint)x;
    }
    #endregion
}
";
            #endregion

            var text2 = @"using System;
class Test
{
    static void Main()
    {
        CSIPropImpl obj = new CSIPropImpl();
        obj.WriteOnlyProp = ""WriteReadOnly "";
        Console.Write(obj.ReadOnlyProp);

        ((IProp)obj).NormalProp = ""NormProp "";
        Console.Write(((IProp)obj).NormalProp);

        obj.set_NormalPropWithParams(123, 4, 5);
        Console.Write(obj.get_NormalPropWithParams(11, 22));

        ((IProp)obj).set_WriteOnlyPropWithParams(456, 0);
        Console.Write(((IProp)obj).get_ReadOnlyPropWithParams(789));
    }
}
";

            var asm01 = TestReferences.MetadataTests.InterfaceAndClass.VBInterfaces01;

            var comp1 = CreateCompilation(
                text1,
                references: new[] { asm01 },
                assemblyName: "OHI_ExpImpPropImpl001",
                options: TestOptions.ReleaseDll);

            var comp = CreateCompilation(
                text2,
                references: new MetadataReference[] { asm01, new CSharpCompilationReference(comp1) },
                assemblyName: "OHI_ExpImpPropImpl002",
                options: TestOptions.ReleaseExe);

            CompileAndVerify(comp, expectedOutput: @"WriteReadOnly NormProp 123456");
        }

        [ConditionalFact(typeof(DesktopOnly))]
        public void TestExplicitImplSignatureMismatches_ParamsAndOptionals()
        {
            // Tests:
            // Replace params with non-params in signature of implemented member (and vice-versa)
            // Replace optional parameter with non-optional parameter

            var source = @"
using System;
using System.Collections.Generic;
interface I1<T>
{
    void Method(int a, long b = 2, string c = null, params List<T>[] d);
}
class Class1 : I1<string>
{
    // Change default value of optional parameter
    void I1<string>.Method(int a, long b = 3, string c = """", params List<string>[] d)
    { Console.WriteLine(""Base.Method({0}, {1}, {2})"", a, b, c); }
}
class Class2 : I1<string>
{
    // Replace optional with non-optional - OK
    void I1<string>.Method(int a, long b, string c = """", params List<string>[] d)
    { Console.WriteLine(""Class.Method({0}, {1}, {2})"", a, b, c); }
}
class Class3 : I1<string>
{
    // Replace non-optional with optional - OK
    // Omit params and replace with optional - OK
    void I1<string>.Method(int a = 4, long b = 3, string c = """", List<string>[] d = null)
    { Console.WriteLine(""Class2.Method({0}, {1}, {2})"", a, b, c); }
}
class Test
{
    public static void Main()
    {
        string[] s1 = new string[]{""a""};
        List<string>[] l1 = new List<string>[]{};

        I1<string> i1 = new Class1();
        i1.Method(1, 2, ""b"", l1);
        
        i1 = new Class2();
        i1.Method(3, 4, ""c"", l1);

        i1 = new Class3();
        i1.Method(4, 5, ""c"", l1);
    }
}";
            var comp = CompileAndVerify(source,
                expectedOutput: @"
Base.Method(1, 2, b)
Class.Method(3, 4, c)
Class2.Method(4, 5, c)",
                expectedSignatures: new[]
                {
                    Signature("Class1", "I1<System.String>.Method", ".method private hidebysig newslot virtual final instance System.Void I1<System.String>.Method(System.Int32 a, [opt] System.Int64 b = 3, [opt] System.String c = \"\", [System.ParamArrayAttribute()] System.Collections.Generic.List`1[System.String][] d) cil managed"),
                    Signature("Class2", "I1<System.String>.Method", ".method private hidebysig newslot virtual final instance System.Void I1<System.String>.Method(System.Int32 a, System.Int64 b, [opt] System.String c = \"\", [System.ParamArrayAttribute()] System.Collections.Generic.List`1[System.String][] d) cil managed"),
                    Signature("Class3", "I1<System.String>.Method", ".method private hidebysig newslot virtual final instance System.Void I1<System.String>.Method([opt] System.Int32 a = 4, [opt] System.Int64 b = 3, [opt] System.String c = \"\", [opt] System.Collections.Generic.List`1[System.String][] d) cil managed")
                });

            comp.VerifyDiagnostics(
                // (11,40): warning CS1066: The default value specified for parameter 'b' will have no effect because it applies to a member that is used in contexts that do not allow optional arguments
                //     void I1<string>.Method(int a, long b = 3, string c = "", params List<string>[] d)
                Diagnostic(ErrorCode.WRN_DefaultValueForUnconsumedLocation, "b").WithArguments("b"),
                // (11,54): warning CS1066: The default value specified for parameter 'c' will have no effect because it applies to a member that is used in contexts that do not allow optional arguments
                //     void I1<string>.Method(int a, long b = 3, string c = "", params List<string>[] d)
                Diagnostic(ErrorCode.WRN_DefaultValueForUnconsumedLocation, "c").WithArguments("c"),
                // (17,50): warning CS1066: The default value specified for parameter 'c' will have no effect because it applies to a member that is used in contexts that do not allow optional arguments
                //     void I1<string>.Method(int a, long b, string c = "", params List<string>[] d)
                Diagnostic(ErrorCode.WRN_DefaultValueForUnconsumedLocation, "c").WithArguments("c"),
                // (24,32): warning CS1066: The default value specified for parameter 'a' will have no effect because it applies to a member that is used in contexts that do not allow optional arguments
                //     void I1<string>.Method(int a = 4, long b = 3, string c = "", List<string>[] d = null)
                Diagnostic(ErrorCode.WRN_DefaultValueForUnconsumedLocation, "a").WithArguments("a"),
                // (24,44): warning CS1066: The default value specified for parameter 'b' will have no effect because it applies to a member that is used in contexts that do not allow optional arguments
                //     void I1<string>.Method(int a = 4, long b = 3, string c = "", List<string>[] d = null)
                Diagnostic(ErrorCode.WRN_DefaultValueForUnconsumedLocation, "b").WithArguments("b"),
                // (24,58): warning CS1066: The default value specified for parameter 'c' will have no effect because it applies to a member that is used in contexts that do not allow optional arguments
                //     void I1<string>.Method(int a = 4, long b = 3, string c = "", List<string>[] d = null)
                Diagnostic(ErrorCode.WRN_DefaultValueForUnconsumedLocation, "c").WithArguments("c"),
                // (24,81): warning CS1066: The default value specified for parameter 'd' will have no effect because it applies to a member that is used in contexts that do not allow optional arguments
                //     void I1<string>.Method(int a = 4, long b = 3, string c = "", List<string>[] d = null)
                Diagnostic(ErrorCode.WRN_DefaultValueForUnconsumedLocation, "d").WithArguments("d"));
        }

        [WorkItem(540501, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/540501")]
        [Fact]
        public void TestImplementingGenericNestedInterfaces_Explicit()
        {
            // Tests:
            // Sanity check – use open (T) and closed (C<String>) generic types in the signature of implemented methods
            // Implement members of generic interface nested inside other generic classes

            var source = @"
using System;
using System.Collections.Generic;
class Outer<T>
{
    internal class Inner<U>
    {
        protected internal interface Interface<V, W>
        {
            T Property { set; }
            void Method<Z>(T a, U[] b, List<V> c, Dictionary<W, Z> d);
        }
        internal class Derived1 : Inner<int>.Interface<long, string>
        {
            T Outer<T>.Inner<int>.Interface<long, string>.Property
            {
                set { Console.WriteLine(""Derived1.set_Property""); }
            }
            void Inner<int>.Interface<long, string>.Method<K>(T A, int[] B, List<long> c, Dictionary<string, K> D)
            {
                Console.WriteLine(""Derived1.Method"");
            }
            internal class Derived2<X, Y> : Outer<X>.Inner<int>.Interface<long, Y>
            {
                X Outer<X>.Inner<int>.Interface<long, Y>.Property
                {
                    set { Console.WriteLine(""Derived2.set_Property""); }
                }
                void Outer<X>.Inner<int>.Interface<long, Y>.Method<K>(X A, int[] b, List<long> C, Dictionary<Y, K> d)
                {
                    Console.WriteLine(""Derived2.Method"");
                }
            }
        }
        internal class Derived3 : Interface<long, string>
        {
            T Inner<U>.Interface<long, string>.Property
            {
                set { Console.WriteLine(""Derived3.set_Property""); }
            }
            void Outer<T>.Inner<U>.Interface<long, string>.Method<K>(T a, U[] B, List<long> C, Dictionary<string, K> d)
            {
                Console.WriteLine(""Derived3.Method"");
            }
        }
        internal class Derived4 : Outer<U>.Inner<T>.Interface<T, U>
        {
            U Outer<U>.Inner<T>.Interface<T, U>.Property
            {
                set { Console.WriteLine(""Derived4.set_Property""); }
            }
            void Outer<U>.Inner<T>.Interface<T, U>.Method<K>(U a, T[] b, List<T> C, Dictionary<U, K> d)
            {
                Console.WriteLine(""Derived4.Method"");
            }
            internal class Derived5 : Outer<T>.Inner<U>.Interface<U, T>
            {
                T Outer<T>.Inner<U>.Interface<U, T>.Property
                {
                    set { Console.WriteLine(""Derived5.set_Property""); }
                }
                void Inner<U>.Interface<U, T>.Method<K>(T a, U[] b, List<U> c, Dictionary<T, K> D)
                {
                    Console.WriteLine(""Derived5.Method"");
                }
                internal class Derived6 : Outer<List<T>>.Inner<U>.Interface<List<U>, T>
                {
                    List<T> Outer<List<T>>.Inner<U>.Interface<List<U>, T>.Property
                    {
                        set { Console.WriteLine(""Derived6.set_Property""); }
                    }

                    void Outer<List<T>>.Inner<U>.Interface<List<U>, T>.Method<K>(List<T> AA, U[] b, List<List<U>> c, Dictionary<T, K> d)
                    {
                        Console.WriteLine(""Derived6.Method"");
                    }
                }
            }
        }
    }
}
class Test
{
    public static void Main()
    {
        Outer<string>.Inner<int>.Interface<long, string> i = new Outer<string>.Inner<int>.Derived1();
        i.Property = """";
        i.Method<string>("""", new int[]{}, new List<long>(), new Dictionary<string,string>());

        i = new Outer<string>.Inner<int>.Derived1.Derived2<string, string>();
        i.Property = """";
        i.Method<string>("""", new int[] { }, new List<long>(), new Dictionary<string, string>());

        i = new Outer<string>.Inner<int>.Derived3();
        i.Property = """";
        i.Method<string>("""", new int[] { }, new List<long>(), new Dictionary<string, string>());

        Outer<int>.Inner<string>.Interface<string, int> i2 = new Outer<string>.Inner<int>.Derived4();
        i2.Property = 1;
        i2.Method<string>(1, new string[] { }, new List<string>(), new Dictionary<int, string>());

        Outer<string>.Inner<int>.Interface<int, string>  i3 = new Outer<string>.Inner<int>.Derived4.Derived5();
        i3.Property = """";
        i3.Method<string>("""", new int[] { }, new List<int>(), new Dictionary<string, string>());

        Outer<List<int>>.Inner<List<string>>.Interface<List<List<string>>, int> i4 = new Outer<int>.Inner<List<string>>.Derived4.Derived5.Derived6();
        i4.Property = new List<int>();
        i4.Method<List<long>>(new List<int>(), new List<string>[]{}, new List<List<List<string>>>(), new Dictionary<int, List<long>>());
    }
}";
            var comp = CompileAndVerify(source,
                expectedOutput: @"
Derived1.set_Property
Derived1.Method
Derived2.set_Property
Derived2.Method
Derived3.set_Property
Derived3.Method
Derived4.set_Property
Derived4.Method
Derived5.set_Property
Derived5.Method
Derived6.set_Property
Derived6.Method",
                expectedSignatures: new[]
                {
                    Signature("Outer`1+Inner`1+Derived1", "Outer<T>.Inner<System.Int32>.Interface<System.Int64,System.String>.set_Property", ".method private hidebysig newslot specialname virtual final instance System.Void Outer<T>.Inner<System.Int32>.Interface<System.Int64,System.String>.set_Property(T value) cil managed"),
                    Signature("Outer`1+Inner`1+Derived1", "Outer<T>.Inner<System.Int32>.Interface<System.Int64,System.String>.Method", ".method private hidebysig newslot virtual final instance System.Void Outer<T>.Inner<System.Int32>.Interface<System.Int64,System.String>.Method<K>(T A, System.Int32[] B, System.Collections.Generic.List`1[System.Int64] c, System.Collections.Generic.Dictionary`2[System.String,K] D) cil managed"),

                    Signature("Outer`1+Inner`1+Derived1+Derived2`2", "Outer<X>.Inner<System.Int32>.Interface<System.Int64,Y>.set_Property", ".method private hidebysig newslot specialname virtual final instance System.Void Outer<X>.Inner<System.Int32>.Interface<System.Int64,Y>.set_Property(X value) cil managed"),
                    Signature("Outer`1+Inner`1+Derived1+Derived2`2", "Outer<X>.Inner<System.Int32>.Interface<System.Int64,Y>.Method", ".method private hidebysig newslot virtual final instance System.Void Outer<X>.Inner<System.Int32>.Interface<System.Int64,Y>.Method<K>(X A, System.Int32[] b, System.Collections.Generic.List`1[System.Int64] C, System.Collections.Generic.Dictionary`2[Y,K] d) cil managed"),

                    Signature("Outer`1+Inner`1+Derived3", "Outer<T>.Inner<U>.Interface<System.Int64,System.String>.set_Property", ".method private hidebysig newslot specialname virtual final instance System.Void Outer<T>.Inner<U>.Interface<System.Int64,System.String>.set_Property(T value) cil managed"),
                    Signature("Outer`1+Inner`1+Derived3", "Outer<T>.Inner<U>.Interface<System.Int64,System.String>.Method", ".method private hidebysig newslot virtual final instance System.Void Outer<T>.Inner<U>.Interface<System.Int64,System.String>.Method<K>(T a, U[] B, System.Collections.Generic.List`1[System.Int64] C, System.Collections.Generic.Dictionary`2[System.String,K] d) cil managed"),

                    Signature("Outer`1+Inner`1+Derived4", "Outer<U>.Inner<T>.Interface<T,U>.set_Property", ".method private hidebysig newslot specialname virtual final instance System.Void Outer<U>.Inner<T>.Interface<T,U>.set_Property(U value) cil managed"),
                    Signature("Outer`1+Inner`1+Derived4", "Outer<U>.Inner<T>.Interface<T,U>.Method", ".method private hidebysig newslot virtual final instance System.Void Outer<U>.Inner<T>.Interface<T,U>.Method<K>(U a, T[] b, System.Collections.Generic.List`1[T] C, System.Collections.Generic.Dictionary`2[U,K] d) cil managed"),

                    Signature("Outer`1+Inner`1+Derived4+Derived5", "Outer<T>.Inner<U>.Interface<U,T>.set_Property", ".method private hidebysig newslot specialname virtual final instance System.Void Outer<T>.Inner<U>.Interface<U,T>.set_Property(T value) cil managed"),
                    Signature("Outer`1+Inner`1+Derived4+Derived5", "Outer<T>.Inner<U>.Interface<U,T>.Method", ".method private hidebysig newslot virtual final instance System.Void Outer<T>.Inner<U>.Interface<U,T>.Method<K>(T a, U[] b, System.Collections.Generic.List`1[U] c, System.Collections.Generic.Dictionary`2[T,K] D) cil managed"),

                    Signature("Outer`1+Inner`1+Derived4+Derived5+Derived6", "Outer<System.Collections.Generic.List<T>>.Inner<U>.Interface<System.Collections.Generic.List<U>,T>.set_Property", ".method private hidebysig newslot specialname virtual final instance System.Void Outer<System.Collections.Generic.List<T>>.Inner<U>.Interface<System.Collections.Generic.List<U>,T>.set_Property(System.Collections.Generic.List`1[T] value) cil managed"),
                    Signature("Outer`1+Inner`1+Derived4+Derived5+Derived6", "Outer<System.Collections.Generic.List<T>>.Inner<U>.Interface<System.Collections.Generic.List<U>,T>.Method", ".method private hidebysig newslot virtual final instance System.Void Outer<System.Collections.Generic.List<T>>.Inner<U>.Interface<System.Collections.Generic.List<U>,T>.Method<K>(System.Collections.Generic.List`1[T] AA, U[] b, System.Collections.Generic.List`1[System.Collections.Generic.List`1[U]] c, System.Collections.Generic.Dictionary`2[T,K] d) cil managed")
                });

            comp.VerifyDiagnostics(); // No Errors
        }

        [WorkItem(540501, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/540501")]
        [Fact]
        public void TestImplementingGenericNestedInterfaces_Explicit_HideTypeParameter()
        {
            // Tests:
            // Explicitly implement generic methods on generic interfaces – test case where type parameter 
            // on method hides the type parameter on class (both in interface and in implementing type)

            var source = @"
using System;
using System.Collections.Generic;
class Outer<T>
{
    internal class Inner<U>
    {
        protected internal interface Interface<V, W>
        {
            void Method<Z>(T a, U[] b, List<V> c, Dictionary<W, Z> d);
            void Method<V, W>(T a, U[] b, List<V> c, Dictionary<W, W> d);
        }
        internal class Derived1<X, Y> : Outer<long>.Inner<int>.Interface<long, Y>
        {
            void Outer<long>.Inner<int>.Interface<long, Y>.Method<X>(long A, int[] b, List<long> C, Dictionary<Y, X> d)
            {
                Console.WriteLine(""Derived1.Method`1"");
            }
            void Outer<long>.Inner<int>.Interface<long, Y>.Method<X, Y>(long A, int[] b, List<X> C, Dictionary<Y, Y> d)
            {
                Console.WriteLine(""Derived1.Method`2"");
            }
        }
    }
}
class Test
{
    public static void Main()
    {
        Outer<long>.Inner<int>.Interface<long, string> i = new Outer<long>.Inner<int>.Derived1<string, string>();
        i.Method<string>(1, new int[]{}, new List<long>(), new Dictionary<string, string>());
        i.Method<string, int>(1, new int[]{}, new List<string>(), new Dictionary<int, int>());
    }
}";

            var comp = CompileAndVerify(source,
                expectedOutput: @"
Derived1.Method`1
Derived1.Method`2",
                expectedSignatures: new[]
                {
                    Signature("Outer`1+Inner`1+Derived1`2", "Outer<System.Int64>.Inner<System.Int32>.Interface<System.Int64,Y>.Method", ".method private hidebysig newslot virtual final instance System.Void Outer<System.Int64>.Inner<System.Int32>.Interface<System.Int64,Y>.Method<X>(System.Int64 A, System.Int32[] b, System.Collections.Generic.List`1[System.Int64] C, System.Collections.Generic.Dictionary`2[Y,X] d) cil managed"),
                    Signature("Outer`1+Inner`1+Derived1`2", "Outer<System.Int64>.Inner<System.Int32>.Interface<System.Int64,Y>.Method", ".method private hidebysig newslot virtual final instance System.Void Outer<System.Int64>.Inner<System.Int32>.Interface<System.Int64,Y>.Method<X, Y>(System.Int64 A, System.Int32[] b, System.Collections.Generic.List`1[X] C, System.Collections.Generic.Dictionary`2[Y,Y] d) cil managed"),
                });

            comp.VerifyDiagnostics(
                // (11,25): warning CS0693: Type parameter 'V' has the same name as the type parameter from outer type 'Outer<T>.Inner<U>.Interface<V, W>'
                Diagnostic(ErrorCode.WRN_TypeParameterSameAsOuterTypeParameter, "V").WithArguments("V", "Outer<T>.Inner<U>.Interface<V, W>"),
                // (11,28): warning CS0693: Type parameter 'W' has the same name as the type parameter from outer type 'Outer<T>.Inner<U>.Interface<V, W>'
                Diagnostic(ErrorCode.WRN_TypeParameterSameAsOuterTypeParameter, "W").WithArguments("W", "Outer<T>.Inner<U>.Interface<V, W>"),
                // (15,67): warning CS0693: Type parameter 'X' has the same name as the type parameter from outer type 'Outer<T>.Inner<U>.Derived1<X, Y>'
                Diagnostic(ErrorCode.WRN_TypeParameterSameAsOuterTypeParameter, "X").WithArguments("X", "Outer<T>.Inner<U>.Derived1<X, Y>"),
                // (19,67): warning CS0693: Type parameter 'X' has the same name as the type parameter from outer type 'Outer<T>.Inner<U>.Derived1<X, Y>'
                Diagnostic(ErrorCode.WRN_TypeParameterSameAsOuterTypeParameter, "X").WithArguments("X", "Outer<T>.Inner<U>.Derived1<X, Y>"),
                // (19,70): warning CS0693: Type parameter 'Y' has the same name as the type parameter from outer type 'Outer<T>.Inner<U>.Derived1<X, Y>'
                Diagnostic(ErrorCode.WRN_TypeParameterSameAsOuterTypeParameter, "Y").WithArguments("Y", "Outer<T>.Inner<U>.Derived1<X, Y>"));
        }

        [ConditionalFact(typeof(ClrOnly), Reason = "https://github.com/mono/mono/issues/10837")]
        public void TestExplicitImplementationInBaseGenericType()
        {
            // Tests:
            // Implement I<string> explicitly in base class and I<int> explicitly in derived class –
            // assuming I<string> and I<int> have members with same signature (i.e. members 
            // that don't depend on generic-ness of the interface) test which (base / derived class) 
            // members are invoked when calling through each interface

            var source = @"
using System;
interface Interface<T>
{
    void Method(T x);
    void Method();
}
class Base<T> : Interface<T>
{
    void Interface<T>.Method() { Console.WriteLine(""Base.Method()""); }
    void Interface<T>.Method(T x) { Console.WriteLine(""Base.Method(T)""); }
}
class Derived<U, V> : Base<int>, Interface<U>
{
    void Interface<U>.Method() { Console.WriteLine(""Derived`2.Method()""); }
    public void Method(int x) { Console.WriteLine(""Derived`2.Method(int)""); }
    void Interface<U>.Method(U x) { Console.WriteLine(""Derived`2.Method(U)""); }
    public void Method(V x) { Console.WriteLine(""Derived`2.Method(V)""); }
}
class Derived : Derived<int, string>, Interface<string>
{
    void Interface<string>.Method() { Console.WriteLine(""Derived.Method()""); }
    void Interface<string>.Method(string x) { Console.WriteLine(""Derived.Method(string)""); }
}
class Test
{
    public static void Main()
    {
        Interface<string> i = new Derived<string, int>();
        i.Method("""");
        i.Method();

        Interface<int> j = new Derived<string, int>();
        j.Method(1);
        j.Method();

        i = new Derived();
        i.Method("""");
        i.Method();

        j = new Derived();
        j.Method(1);
        j.Method();
    }
}";

            var comp = CompileAndVerify(source, expectedOutput: @"
Derived`2.Method(U)
Derived`2.Method()
Base.Method(T)
Base.Method()
Derived.Method(string)
Derived.Method()
Derived`2.Method(U)
Derived`2.Method()");

            comp.VerifyDiagnostics(); // No errors
        }

        [ConditionalFact(typeof(ClrOnly), Reason = "https://github.com/mono/mono/issues/10837")]
        public void TestExplicitImplementationInBaseGenericType2()
        {
            // Tests:
            // Variation of TestExplicitImplementationInBaseGenericType with re-implementation

            var source = @"
using System;
interface Interface<T>
{
    void Method(T x);
    void Method();
}
class Base<T> : Interface<T>
{
    void Interface<T>.Method() { Console.WriteLine(""Base.Method()""); }
    void Interface<T>.Method(T x) { Console.WriteLine(""Base.Method(T)""); }
}
class Derived<U, V> : Base<int>, Interface<U>
{
    void Interface<U>.Method() { Console.WriteLine(""Derived`2.Method()""); }
    void Interface<U>.Method(U x) { Console.WriteLine(""Derived`2.Method(U)""); }
    public virtual void Method(V x) { Console.WriteLine(""Derived`2.Method(V)""); }
}
class Derived : Derived<int, string>, Interface<string>, Interface<int>
{
    void Interface<string>.Method() { Console.WriteLine(""Derived.Method()""); }
    void Interface<string>.Method(string x) { Console.WriteLine(""Derived.Method(string)""); }
    void Interface<int>.Method(int x) { Console.WriteLine(""Derived.Method(int)""); }
}
class Test
{
    public static void Main()
    {
        Interface<string> i = new Derived<string, int>();
        i.Method("""");
        i.Method();

        Interface<int> j = new Derived<string, int>();
        j.Method(1);
        j.Method();

        i = new Derived();
        i.Method("""");
        i.Method();

        j = new Derived();
        j.Method(1);
        j.Method();
    }
}";

            var comp = CompileAndVerify(source, expectedOutput: @"
Derived`2.Method(U)
Derived`2.Method()
Base.Method(T)
Base.Method()
Derived.Method(string)
Derived.Method()
Derived.Method(int)
Derived`2.Method()");

            comp.VerifyDiagnostics(); // No errors
        }

        [Fact]
        public void TestImplementAmbiguousSignaturesExplicitly()
        {
            // Tests:
            // Explicitly implement multiple base interface members with same signatures
            // but different return types / ref / out / params

            var source = @"
using System;
interface I1
{
    int P { set; }
    void M1(long x);
    void M2(long x);
    void M3(long x);
    void M4(ref long x);
    void M5(out long x);
    void M6(ref long x);
    void M7(out long x);
    void M8(params long[] x);
    void M9(long[] x);
}
interface I2 : I1
{
}
interface I3 : I2
{
    new long P { set; }
    new int M1(long x); // Return type
    void M2(ref long x); // Add ref
    void M3(out long x); // Add out
    void M4(long x); // Omit ref
    void M5(long x); // Omit out
    void M6(out long x); // Toggle ref to out
    void M7(ref long x); // Toggle out to ref
    new void M8(long[] x); // Omit params
    new void M9(params long[] x); // Add params
}
class Test : I3
{
    int I1.P { set { Console.WriteLine(""I1.P""); } }
    long I3.P { set { Console.WriteLine(""I3.P""); } }
    void I1.M1(long x) { Console.WriteLine(""I1.M1""); }
    void I1.M2(long x) { Console.WriteLine(""I1.M2""); }
    void I1.M3(long x) { Console.WriteLine(""I1.M3""); }
    void I1.M4(ref long x) { Console.WriteLine(""I1.M4""); }
    void I1.M5(out long x) { x = 0; Console.WriteLine(""I1.M5""); }
    void I1.M6(ref long x) { Console.WriteLine(""I1.M6""); }
    void I1.M7(out long x) { x = 0; Console.WriteLine(""I1.M7""); }
    void I1.M8(params long[] x) { Console.WriteLine(""I1.M8""); }
    void I1.M9(long[] x) { Console.WriteLine(""I1.M9""); }
    int I3.M1(long x) { Console.WriteLine(""I3.M1""); return 0; }
    void I3.M2(ref long x) { Console.WriteLine(""I3.M2""); }
    void I3.M3(out long x) { x = 0; Console.WriteLine(""I3.M3""); }
    void I3.M4(long x) { Console.WriteLine(""I3.M4""); }
    void I3.M5(long x) { Console.WriteLine(""I3.M5""); }
    void I3.M6(out long x) { x = 0; Console.WriteLine(""I3.M6""); }
    void I3.M7(ref long x) { Console.WriteLine(""I3.M7""); }
    void I3.M8(long[] x) { Console.WriteLine(""I3.M8""); }
    void I3.M9(params long[] x) { Console.WriteLine(""I3.M9""); }
    public static void Main()
    {
        I3 i = new Test();
        long x = 1;
        i.M1(x); i.M2(x); i.M2(ref x); i.M3(x); i.M3(out x);
        i.M4(x); i.M4(ref x); i.M5(x); i.M5(out x);
        i.M6(out x); i.M6(ref x); i.M7(ref x); i.M7(out x);
        i.M8(new long[] { x, x, x }); i.M8(x, x, x);
        i.M9(new long[] { x, x, x }); i.M9(x, x, x);
        i.P = 1;

        I1 j = i;
        j.M1(x); j.M2(x); j.M3(x);
        j.M4(ref x); j.M5(out x);
        j.M6(ref x); j.M7(out x);
        j.M8(new long[] { x, x, x }); j.M8(x, x, x);
        j.M9(new long[] { x, x, x });
        j.P = 1;
    }
}";
            CompileAndVerify(source, expectedOutput: @"
I3.M1
I1.M2
I3.M2
I1.M3
I3.M3
I3.M4
I1.M4
I3.M5
I1.M5
I3.M6
I1.M6
I3.M7
I1.M7
I3.M8
I1.M8
I3.M9
I3.M9
I3.P
I1.M1
I1.M2
I1.M3
I1.M4
I1.M5
I1.M6
I1.M7
I1.M8
I1.M8
I1.M9
I1.P").VerifyDiagnostics(); // No errors
        }

        [WorkItem(543426, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/543426")]
        [Fact]
        public void TestExplicitlyImplementInterfaceNestedInGenericType()
        {
            // Tests:
            // Variation of TestExplicitImplementationInBaseGenericType with re-implementation

            var source = @"
class Outer<T>
{
    interface IInner
    {
        void M(T t);
    }
 
    class Inner : IInner
    {
        void IInner.M(T t) { }
    }
}
";

            var comp = CompileAndVerify(source, expectedSignatures: new[]
            {
                Signature("Outer`1+IInner", "M", ".method public hidebysig newslot abstract virtual instance System.Void M(T t) cil managed"),
                Signature("Outer`1+Inner", "Outer<T>.IInner.M", ".method private hidebysig newslot virtual final instance System.Void Outer<T>.IInner.M(T t) cil managed"),
            });

            comp.VerifyDiagnostics(); // No errors
        }

        [WorkItem(598052, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/598052")]
        [Fact]
        public void TestExternAliasInName()
        {
            var libSource = @"
public interface I
{
    void M();
    int P { get; set; }
    event System.Action E;
}
";

            var source = @"
extern alias Q;

class C : Q::I
{
    void Q::I.M() { }
    int Q::I.P { get { return 0; } set { } }
    event System.Action Q::I.E { add { } remove { } }
}
";

            var libComp = CreateCompilation(libSource);
            libComp.VerifyDiagnostics();

            var comp = CreateCompilation(source, new[] { new CSharpCompilationReference(libComp, aliases: ImmutableArray.Create("Q")) });
            comp.VerifyDiagnostics();

            var classC = comp.GlobalNamespace.GetMember<NamedTypeSymbol>("C");
            var classCMembers = classC.GetMembers();

            // The alias is preserved, in case a similar interface is implemented from another aliased assembly.
            AssertEx.All(classCMembers.Select(m => m.Name), name => name == WellKnownMemberNames.InstanceConstructorName || name.StartsWith("Q::I.", StringComparison.Ordinal));
            AssertEx.All(classCMembers.Select(m => m.MetadataName), metadataName => metadataName == WellKnownMemberNames.InstanceConstructorName || metadataName.StartsWith("Q::I.", StringComparison.Ordinal));
            AssertEx.None(classCMembers.Select(m => m.ToString()), id => id.Contains("Q"));
            AssertEx.None(classCMembers.Select(m => m.GetDocumentationCommentId()), id => id.Contains("Q"));
        }

        [WorkItem(598052, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/598052")]
        [Fact]
        public void TestImplementMultipleExternAliasInterfaces()
        {
            var libSource = @"
public interface I
{
    void M();
    int P { get; set; }
    event System.Action E;
}
";

            var source = @"
extern alias A;
extern alias B;

class C : A::I, B::I
{
    void A::I.M() { }
    int A::I.P { get { return 0; } set { } }
    event System.Action A::I.E { add { } remove { } }

    void B::I.M() { }
    int B::I.P { get { return 0; } set { } }
    event System.Action B::I.E { add { } remove { } }
}
";

            var libComp1 = CreateCompilation(libSource, assemblyName: "lib1");
            libComp1.VerifyDiagnostics();

            var libComp2 = CreateCompilation(libSource, assemblyName: "lib2");
            libComp2.VerifyDiagnostics();

            // Same reference, two aliases.
            var comp1 = CreateCompilation(source, new[] { new CSharpCompilationReference(libComp1, aliases: ImmutableArray.Create("A")), new CSharpCompilationReference(libComp1, aliases: ImmutableArray.Create("B")) });
            comp1.VerifyDiagnostics(
                // (5,17): error CS0528: 'I' is already listed in interface list
                // class C : A::I, B::I
                Diagnostic(ErrorCode.ERR_DuplicateInterfaceInBaseList, "B::I").WithArguments("I"),
                // (5,7): error CS8646: 'I.E' is explicitly implemented more than once.
                // class C : A::I, B::I
                Diagnostic(ErrorCode.ERR_DuplicateExplicitImpl, "C").WithArguments("I.E").WithLocation(5, 7),
                // (5,7): error CS8646: 'I.P' is explicitly implemented more than once.
                // class C : A::I, B::I
                Diagnostic(ErrorCode.ERR_DuplicateExplicitImpl, "C").WithArguments("I.P").WithLocation(5, 7),
                // (5,7): error CS8646: 'I.M()' is explicitly implemented more than once.
                // class C : A::I, B::I
                Diagnostic(ErrorCode.ERR_DuplicateExplicitImpl, "C").WithArguments("I.M()").WithLocation(5, 7));

            // Two assemblies with the same content, two aliases.
            var comp2 = CreateCompilation(source, new[] { new CSharpCompilationReference(libComp1, aliases: ImmutableArray.Create("A")), new CSharpCompilationReference(libComp2, aliases: ImmutableArray.Create("B")) });
            var verifier2 = CompileAndVerify(comp2, expectedSignatures: new[]
            {
                Signature("C", "A::I.M", ".method private hidebysig newslot virtual final instance System.Void A::I.M() cil managed"),
                Signature("C", "A::I.get_P", ".method private hidebysig newslot specialname virtual final instance System.Int32 A::I.get_P() cil managed"),
                Signature("C", "A::I.set_P", ".method private hidebysig newslot specialname virtual final instance System.Void A::I.set_P(System.Int32 value) cil managed"),
                Signature("C", "A::I.add_E", ".method private hidebysig newslot specialname virtual final instance System.Void A::I.add_E(System.Action value) cil managed"),
                Signature("C", "A::I.remove_E", ".method private hidebysig newslot specialname virtual final instance System.Void A::I.remove_E(System.Action value) cil managed"),

                Signature("C", "B::I.M", ".method private hidebysig newslot virtual final instance System.Void B::I.M() cil managed"),
                Signature("C", "B::I.get_P", ".method private hidebysig newslot specialname virtual final instance System.Int32 B::I.get_P() cil managed"),
                Signature("C", "B::I.set_P", ".method private hidebysig newslot specialname virtual final instance System.Void B::I.set_P(System.Int32 value) cil managed"),
                Signature("C", "B::I.add_E", ".method private hidebysig newslot specialname virtual final instance System.Void B::I.add_E(System.Action value) cil managed"),
                Signature("C", "B::I.remove_E", ".method private hidebysig newslot specialname virtual final instance System.Void B::I.remove_E(System.Action value) cil managed"),
            });

            // Simple verification that the test infrastructure supports such methods.
            var testData = verifier2.TestData;
            var pair = testData.Methods.Single(m => m.Key.Name == "A::I.M");
            pair.Value.VerifyIL(@"
{
  // Code size        1 (0x1)
  .maxstack  0
  IL_0000:  ret
}");
        }
    }
}
