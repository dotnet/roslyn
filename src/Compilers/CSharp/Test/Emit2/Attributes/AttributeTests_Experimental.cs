// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using Microsoft.CodeAnalysis.CSharp.Test.Utilities;
using Microsoft.CodeAnalysis.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.CSharp.UnitTests
{
    public class AttributeTests_WindowsExperimental : CSharpTestBase
    {
        private const string DeprecatedAttributeSource =
@"using System;
namespace Windows.Foundation.Metadata
{
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct | AttributeTargets.Enum | AttributeTargets.Constructor | AttributeTargets.Method | AttributeTargets.Property | AttributeTargets.Field | AttributeTargets.Event | AttributeTargets.Interface | AttributeTargets.Delegate, AllowMultiple = true)]
    public sealed class DeprecatedAttribute : Attribute
    {
        public DeprecatedAttribute(System.String message, DeprecationType type, System.UInt32 version)
        {
        }
    }
    public enum DeprecationType
    {
        Deprecate = 0,
        Remove = 1
    }
}";

        private const string ExperimentalAttributeSource =
@"using System;
namespace Windows.Foundation.Metadata
{
    [AttributeUsage(
        AttributeTargets.Class | AttributeTargets.Struct | AttributeTargets.Enum | AttributeTargets.Interface | AttributeTargets.Delegate,
        AllowMultiple = false)]
    public sealed class ExperimentalAttribute : Attribute
    {
    }
}";

        [Fact]
        public void TestExperimentalAttribute()
        {
            var source1 =
@"using Windows.Foundation.Metadata;
namespace N
{
    [Experimental] public struct S { }
    [Experimental] internal delegate void D<T>();
    public class A<T>
    {
        [Experimental] public class B { }
        static void M()
        {
            new B();
            D<int> d = null;
            d();
        }
    }
    [Experimental] public enum E { A }
}";
            var comp1 = CreateCompilation(new[] { Parse(ExperimentalAttributeSource), Parse(source1) });
            comp1.VerifyDiagnostics(
                // (11,17): warning CS8305: 'N.A<T>.B' is for evaluation purposes only and is subject to change or removal in future updates.
                //             new B();
                Diagnostic(ErrorCode.WRN_Experimental, "B").WithArguments("N.A<T>.B").WithLocation(11, 17),
                // (12,13): warning CS8305: 'N.D<int>' is for evaluation purposes only and is subject to change or removal in future updates.
                //             D<int> d = null;
                Diagnostic(ErrorCode.WRN_Experimental, "D<int>").WithArguments("N.D<int>").WithLocation(12, 13));

            var source2 =
@"using N;
using B = N.A<int>.B;
#pragma warning disable 219
class C
{
    static void F()
    {
        object o = new B();
        o = default(S);
        var e = default(E);
        e = E.A;
    }
}";
            var comp2A = CreateCompilation(source2, new[] { comp1.EmitToImageReference() });
            comp2A.VerifyDiagnostics(
                // (8,24): warning CS8305: 'N.A<int>.B' is for evaluation purposes only and is subject to change or removal in future updates.
                //         object o = new B();
                Diagnostic(ErrorCode.WRN_Experimental, "B").WithArguments("N.A<int>.B").WithLocation(8, 24),
                // (9,21): warning CS8305: 'N.S' is for evaluation purposes only and is subject to change or removal in future updates.
                //         o = default(S);
                Diagnostic(ErrorCode.WRN_Experimental, "S").WithArguments("N.S").WithLocation(9, 21),
                // (10,25): warning CS8305: 'N.E' is for evaluation purposes only and is subject to change or removal in future updates.
                //         var e = default(E);
                Diagnostic(ErrorCode.WRN_Experimental, "E").WithArguments("N.E").WithLocation(10, 25),
                // (11,13): warning CS8305: 'N.E' is for evaluation purposes only and is subject to change or removal in future updates.
                //         e = E.A;
                Diagnostic(ErrorCode.WRN_Experimental, "E").WithArguments("N.E").WithLocation(11, 13));

            var comp2B = CreateCompilation(source2, new[] { new CSharpCompilationReference(comp1) });
            comp2B.VerifyDiagnostics(
                // (8,24): warning CS8305: 'N.A<int>.B' is for evaluation purposes only and is subject to change or removal in future updates.
                //         object o = new B();
                Diagnostic(ErrorCode.WRN_Experimental, "B").WithArguments("N.A<int>.B").WithLocation(8, 24),
                // (9,21): warning CS8305: 'N.S' is for evaluation purposes only and is subject to change or removal in future updates.
                //         o = default(S);
                Diagnostic(ErrorCode.WRN_Experimental, "S").WithArguments("N.S").WithLocation(9, 21),
                // (10,25): warning CS8305: 'N.E' is for evaluation purposes only and is subject to change or removal in future updates.
                //         var e = default(E);
                Diagnostic(ErrorCode.WRN_Experimental, "E").WithArguments("N.E").WithLocation(10, 25),
                // (11,13): warning CS8305: 'N.E' is for evaluation purposes only and is subject to change or removal in future updates.
                //         e = E.A;
                Diagnostic(ErrorCode.WRN_Experimental, "E").WithArguments("N.E").WithLocation(11, 13));
        }

        // [Experimental] applied to members even though
        // AttributeUsage is types only.
        [Fact]
        public void TestExperimentalMembers()
        {
            var source0 =
@".class public Windows.Foundation.Metadata.ExperimentalAttribute extends [mscorlib]System.Attribute
{
  .custom instance void [mscorlib]System.AttributeUsageAttribute::.ctor(valuetype [mscorlib]System.AttributeTargets) = ( 01 00 1C 14 00 00 01 00 54 02 0D 41 6C 6C 6F 77   // ........T..Allow
                                                                                                                         4D 75 6C 74 69 70 6C 65 00 )                      // Multiple.
}
.class public E extends [mscorlib]System.Enum
{
  .field public specialname rtspecialname int32 value__
  .field public static literal valuetype E A = int32(0x00000000)
  .custom instance void Windows.Foundation.Metadata.ExperimentalAttribute::.ctor() = ( 01 00 00 00 ) 
  .field public static literal valuetype E B = int32(0x00000001)
}
.class interface public I
{
  .method public abstract virtual instance void F()
  {
    .custom instance void Windows.Foundation.Metadata.ExperimentalAttribute::.ctor() = ( 01 00 00 00 ) 
  }
}
.class public C implements I
{
  .method public virtual final instance void F() { ret }
}";
            var ref0 = CompileIL(source0);
            var source1 =
@"#pragma warning disable 219
class Program
{
    static void Main()
    {
        E e = default(E);
        e = E.A;        // warning CS8305: 'E.A' is for evaluation purposes only
        e = E.B;
        var o = default(C);
        o.F();
        ((I)o).F();     // warning CS8305: 'I.F()' is for evaluation purposes only
    }
}";
            var comp1 = CreateCompilation(source1, new[] { ref0 });
            comp1.VerifyDiagnostics(
                // (7,13): warning CS8305: 'E.A' is for evaluation purposes only and is subject to change or removal in future updates.
                //         e = E.A;        // warning CS8305: 'F.A' is for evaluation purposes only
                Diagnostic(ErrorCode.WRN_Experimental, "E.A").WithArguments("E.A").WithLocation(7, 13),
                // (11,9): warning CS8305: 'I.F()' is for evaluation purposes only and is subject to change or removal in future updates.
                //         ((I)o).F();     // warning CS8305: 'I.F()' is for evaluation purposes only
                Diagnostic(ErrorCode.WRN_Experimental, "((I)o).F()").WithArguments("I.F()").WithLocation(11, 9));
        }

        [Fact]
        public void TestExperimentalTypeWithDeprecatedAndObsoleteMembers()
        {
            var source =
@"using System;
using Windows.Foundation.Metadata;
[Experimental]
class A
{
    internal void F0() { }
    [Deprecated("""", DeprecationType.Deprecate, 0)]
    internal void F1() { }
    [Deprecated("""", DeprecationType.Remove, 0)]
    internal void F2() { }
    [Obsolete("""", false)]
    internal void F3() { }
    [Obsolete("""", true)]
    internal void F4() { }
    [Experimental]
    internal class B { }
}
class C
{
    static void F(A a)
    {
        a.F0();
        a.F1();
        a.F2();
        a.F3();
        a.F4();
        (new A.B()).ToString();
    }
}";
            var comp = CreateCompilationWithMscorlib40AndSystemCore(new[] { Parse(DeprecatedAttributeSource), Parse(ExperimentalAttributeSource), Parse(source) });
            comp.VerifyDiagnostics(
                // (20,19): warning CS8305: 'A' is for evaluation purposes only and is subject to change or removal in future updates.
                //     static void F(A a)
                Diagnostic(ErrorCode.WRN_Experimental, "A").WithArguments("A").WithLocation(20, 19),
                // (23,9): warning CS0618: 'A.F1()' is obsolete: ''
                //         a.F1();
                Diagnostic(ErrorCode.WRN_DeprecatedSymbolStr, "a.F1()").WithArguments("A.F1()", "").WithLocation(23, 9),
                // (24,9): error CS0619: 'A.F2()' is obsolete: ''
                //         a.F2();
                Diagnostic(ErrorCode.ERR_DeprecatedSymbolStr, "a.F2()").WithArguments("A.F2()", "").WithLocation(24, 9),
                // (25,9): warning CS0618: 'A.F3()' is obsolete: ''
                //         a.F3();
                Diagnostic(ErrorCode.WRN_DeprecatedSymbolStr, "a.F3()").WithArguments("A.F3()", "").WithLocation(25, 9),
                // (26,9): error CS0619: 'A.F4()' is obsolete: ''
                //         a.F4();
                Diagnostic(ErrorCode.ERR_DeprecatedSymbolStr, "a.F4()").WithArguments("A.F4()", "").WithLocation(26, 9),
                // (27,14): warning CS8305: 'A' is for evaluation purposes only and is subject to change or removal in future updates.
                //         (new A.B()).ToString();
                Diagnostic(ErrorCode.WRN_Experimental, "A").WithArguments("A").WithLocation(27, 14),
                // (27,14): warning CS8305: 'A.B' is for evaluation purposes only and is subject to change or removal in future updates.
                //         (new A.B()).ToString();
                Diagnostic(ErrorCode.WRN_Experimental, "A.B").WithArguments("A.B").WithLocation(27, 14));
        }

        [Fact]
        public void TestDeprecatedLocalFunctions()
        {
            var source =
@"
using Windows.Foundation.Metadata;
class A
{
    void M()
    {
        local1(); // 1
        local2(); // 2

        [Deprecated("""", DeprecationType.Deprecate, 0)]
        void local1() { }

        [Deprecated("""", DeprecationType.Remove, 0)]
        void local2() { }

#pragma warning disable 8321 // Unreferenced local function
        [Deprecated("""", DeprecationType.Deprecate, 0)]
        void local3()
        {
            // No obsolete warnings expected inside a deprecated local function
            local1();
            local2();
        }
    }
}";
            var comp = CreateCompilationWithMscorlib40AndSystemCore(new[] { DeprecatedAttributeSource, ExperimentalAttributeSource, source }, parseOptions: TestOptions.Regular9);
            comp.VerifyDiagnostics(
                // (7,9): warning CS0618: 'local1()' is obsolete: ''
                //         local1(); // 1
                Diagnostic(ErrorCode.WRN_DeprecatedSymbolStr, "local1()").WithArguments("local1()", "").WithLocation(7, 9),
                // (8,9): error CS0619: 'local2()' is obsolete: ''
                //         local2(); // 2
                Diagnostic(ErrorCode.ERR_DeprecatedSymbolStr, "local2()").WithArguments("local2()", "").WithLocation(8, 9));
        }

        // Diagnostics for [Obsolete] members
        // are not suppressed in [Experimental] types.
        [Fact]
        public void TestObsoleteMembersInExperimentalType()
        {
            var source =
@"using System;
using Windows.Foundation.Metadata;
class A
{
    internal void F0() { }
    [Deprecated("""", DeprecationType.Deprecate, 0)]
    internal void F1() { }
    [Deprecated("""", DeprecationType.Remove, 0)]
    internal void F2() { }
    [Obsolete("""", false)]
    internal void F3() { }
    [Obsolete("""", true)]
    internal void F4() { }
    [Experimental]
    internal class B { }
}
[Experimental]
class C
{
    static void F(A a)
    {
        a.F0();
        a.F1();
        a.F2();
        a.F3();
        a.F4();
        (new A.B()).ToString();
    }
}";
            var comp = CreateCompilationWithMscorlib40AndSystemCore(new[] { Parse(DeprecatedAttributeSource), Parse(ExperimentalAttributeSource), Parse(source) });
            comp.VerifyDiagnostics(
                // (23,9): warning CS0618: 'A.F1()' is obsolete: ''
                //         a.F1();
                Diagnostic(ErrorCode.WRN_DeprecatedSymbolStr, "a.F1()").WithArguments("A.F1()", "").WithLocation(23, 9),
                // (24,9): error CS0619: 'A.F2()' is obsolete: ''
                //         a.F2();
                Diagnostic(ErrorCode.ERR_DeprecatedSymbolStr, "a.F2()").WithArguments("A.F2()", "").WithLocation(24, 9),
                // (25,9): warning CS0618: 'A.F3()' is obsolete: ''
                //         a.F3();
                Diagnostic(ErrorCode.WRN_DeprecatedSymbolStr, "a.F3()").WithArguments("A.F3()", "").WithLocation(25, 9),
                // (26,9): error CS0619: 'A.F4()' is obsolete: ''
                //         a.F4();
                Diagnostic(ErrorCode.ERR_DeprecatedSymbolStr, "a.F4()").WithArguments("A.F4()", "").WithLocation(26, 9),
                // (27,14): warning CS8305: 'A.B' is for evaluation purposes only and is subject to change or removal in future updates.
                //         (new A.B()).ToString();
                Diagnostic(ErrorCode.WRN_Experimental, "A.B").WithArguments("A.B").WithLocation(27, 14));
        }

        [Fact]
        public void TestObsoleteMembersInExperimentalTypeInObsoleteType()
        {
            var source =
@"using System;
using Windows.Foundation.Metadata;
class A
{
    [Obsolete("""", false)]
    internal void F() { }
}
[Obsolete("""", false)]
class B
{
    [Experimental]
    class C
    {
        static void G(A a)
        {
            a.F();
        }
    }
}";
            var comp = CreateCompilationWithMscorlib40AndSystemCore(new[] { Parse(DeprecatedAttributeSource), Parse(ExperimentalAttributeSource), Parse(source) });
            comp.VerifyDiagnostics();
        }

        // Diagnostics for [Experimental] types
        // are not suppressed in [Obsolete] members.
        [Fact]
        public void TestExperimentalTypeInObsoleteMember()
        {
            var source =
@"using System;
using Windows.Foundation.Metadata;
[Experimental] class A { }
[Experimental] class B { }
class C
{
    static object FA() => new A();
    [Obsolete("""", false)]
    static object FB() => new B();
}";
            var comp = CreateCompilationWithMscorlib40AndSystemCore(new[] { Parse(ExperimentalAttributeSource), Parse(source) });
            comp.VerifyDiagnostics(
                // (7,31): warning CS8305: 'A' is for evaluation purposes only and is subject to change or removal in future updates.
                //     static object FA() => new A();
                Diagnostic(ErrorCode.WRN_Experimental, "A").WithArguments("A"),
                // (9,31): warning CS8305: 'B' is for evaluation purposes only and is subject to change or removal in future updates.
                //     static object FB() => new B();
                Diagnostic(ErrorCode.WRN_Experimental, "B").WithArguments("B").WithLocation(9, 31));
        }

        [Fact]
        public void TestExperimentalTypeWithAttributeMarkedObsolete()
        {
            var source =
@"using System;
using Windows.Foundation.Metadata;
[Obsolete]
class MyAttribute : Attribute
{
}
[Experimental]
[MyAttribute]
class A
{
}
class B
{
    A F() => null;
}";
            var comp = CreateCompilationWithMscorlib40AndSystemCore(new[] { Parse(ExperimentalAttributeSource), Parse(source) });
            comp.VerifyDiagnostics(
                // (8,2): warning CS0612: 'MyAttribute' is obsolete
                // [MyAttribute]
                Diagnostic(ErrorCode.WRN_DeprecatedSymbol, "MyAttribute").WithArguments("MyAttribute"),
                // (14,5): warning CS8305: 'A' is for evaluation purposes only and is subject to change or removal in future updates.
                //     A F() => null;
                Diagnostic(ErrorCode.WRN_Experimental, "A").WithArguments("A").WithLocation(14, 5));
        }

        [Fact]
        public void TestObsoleteTypeWithAttributeMarkedExperimental()
        {
            var source =
@"using System;
using Windows.Foundation.Metadata;
[Experimental]
class MyAttribute : Attribute
{
}
[Obsolete]
[MyAttribute]
class A
{
}
class B
{
    A F() => null;
}";
            var comp = CreateCompilationWithMscorlib40AndSystemCore(new[] { Parse(ExperimentalAttributeSource), Parse(source) });
            comp.VerifyDiagnostics(
                // (8,2): warning CS8305: 'MyAttribute' is for evaluation purposes only and is subject to change or removal in future updates.
                // [MyAttribute]
                Diagnostic(ErrorCode.WRN_Experimental, "MyAttribute").WithArguments("MyAttribute").WithLocation(8, 2),
                // (14,5): warning CS0612: 'A' is obsolete
                //     A F() => null;
                Diagnostic(ErrorCode.WRN_DeprecatedSymbol, "A").WithArguments("A").WithLocation(14, 5));
        }

        [Fact]
        public void TestAttributesMarkedExperimentalAndObsolete()
        {
            var source =
@"using System;
using Windows.Foundation.Metadata;
[Experimental]
[B]
class AAttribute : Attribute
{
}
[Obsolete]
[A]
class BAttribute : Attribute
{
}
[A]
[B]
class C
{
}";
            var comp = CreateCompilationWithMscorlib40AndSystemCore(new[] { Parse(ExperimentalAttributeSource), Parse(source) });
            comp.VerifyDiagnostics(
                // (9,2): warning CS8305: 'AAttribute' is for evaluation purposes only and is subject to change or removal in future updates.
                // [A]
                Diagnostic(ErrorCode.WRN_Experimental, "A").WithArguments("AAttribute").WithLocation(9, 2),
                // (4,2): warning CS0612: 'BAttribute' is obsolete
                // [B]
                Diagnostic(ErrorCode.WRN_DeprecatedSymbol, "B").WithArguments("BAttribute").WithLocation(4, 2),
                // (13,2): warning CS8305: 'AAttribute' is for evaluation purposes only and is subject to change or removal in future updates.
                // [A]
                Diagnostic(ErrorCode.WRN_Experimental, "A").WithArguments("AAttribute").WithLocation(13, 2),
                // (14,2): warning CS0612: 'BAttribute' is obsolete
                // [B]
                Diagnostic(ErrorCode.WRN_DeprecatedSymbol, "B").WithArguments("BAttribute").WithLocation(14, 2));
        }

        [Fact]
        public void TestAttributesMarkedExperimentalAndObsolete2()
        {
            var source =
@"using System;
using Windows.Foundation.Metadata;
[Obsolete]
[B]
class AAttribute : Attribute
{
}
[Experimental]
[A]
class BAttribute : Attribute
{
}
[A]
[B]
class C
{
}";
            var comp = CreateCompilationWithMscorlib40AndSystemCore(new[] { Parse(ExperimentalAttributeSource), Parse(source) });
            comp.VerifyDiagnostics(
                // (9,2): warning CS0612: 'AAttribute' is obsolete
                // [A]
                Diagnostic(ErrorCode.WRN_DeprecatedSymbol, "A").WithArguments("AAttribute").WithLocation(9, 2),
                // (4,2): warning CS8305: 'BAttribute' is for evaluation purposes only and is subject to change or removal in future updates.
                // [B]
                Diagnostic(ErrorCode.WRN_Experimental, "B").WithArguments("BAttribute").WithLocation(4, 2),
                // (13,2): warning CS0612: 'AAttribute' is obsolete
                // [A]
                Diagnostic(ErrorCode.WRN_DeprecatedSymbol, "A").WithArguments("AAttribute").WithLocation(13, 2),
                // (14,2): warning CS8305: 'BAttribute' is for evaluation purposes only and is subject to change or removal in future updates.
                // [B]
                Diagnostic(ErrorCode.WRN_Experimental, "B").WithArguments("BAttribute").WithLocation(14, 2));
        }

        // Combinations of attributes.
        [Fact]
        public void TestDeprecatedAndExperimentalAndObsoleteAttributes()
        {
            var source1 =
@"using System;
using Windows.Foundation.Metadata;
[Obsolete(""OA"", false),                          Deprecated(""DA"", DeprecationType.Deprecate, 0)]public struct SA { }
[Obsolete(""OB"", false),                          Deprecated(""DB"", DeprecationType.Remove, 0)]   public struct SB { }
[Obsolete(""OC"", false),                          Experimental]                                  public struct SC { }
[Obsolete(""OD"", true),                           Deprecated(""DC"", DeprecationType.Deprecate, 0)]public struct SD { }
[Obsolete(""OE"", true),                           Deprecated(""DD"", DeprecationType.Remove, 0)]   public struct SE { }
[Obsolete(""OF"", true),                           Experimental]                                  public struct SF { }
[Deprecated(""DG"", DeprecationType.Deprecate, 0), Obsolete(""OG"", false)]                         public interface IG { }
[Deprecated(""DH"", DeprecationType.Deprecate, 0), Obsolete(""OH"", true)]                          public interface IH { }
[Deprecated(""DI"", DeprecationType.Deprecate, 0), Experimental]                                  public interface II { }
[Deprecated(""DJ"", DeprecationType.Remove, 0),    Obsolete(""OJ"", false)]                         public interface IJ { }
[Deprecated(""DK"", DeprecationType.Remove, 0),    Obsolete(""OK"", true)]                          public interface IK { }
[Deprecated(""DL"", DeprecationType.Remove, 0),    Experimental]                                  public interface IL { }
[Experimental,                                   Obsolete(""OM"", false)]                         public enum EM { }
[Experimental,                                   Obsolete(""ON"", true)]                          public enum EN { }
[Experimental,                                   Deprecated(""DO"", DeprecationType.Deprecate, 0)]public enum EO { }
[Experimental,                                   Deprecated(""DP"", DeprecationType.Remove, 0)]   public enum EP { }";
            var comp1 = CreateCompilationWithMscorlib40AndSystemCore(new[] { Parse(DeprecatedAttributeSource), Parse(ExperimentalAttributeSource), Parse(source1) });
            comp1.VerifyDiagnostics();

            var source2 =
@"class C
{
    static void F(object o) { }
    static void Main()
    {
        F(default(SA));
        F(default(SB));
        F(default(SC));
        F(default(SD));
        F(default(SE));
        F(default(SF));
        F(default(IG));
        F(default(IH));
        F(default(II));
        F(default(IJ));
        F(default(IK));
        F(default(EM));
        F(default(EN));
        F(default(EO));
        F(default(EP));
    }
}";
            var comp2 = CreateCompilationWithMscorlib40AndSystemCore(source2, references: new[] { comp1.EmitToImageReference() });
            comp2.VerifyDiagnostics(
                // (6,19): warning CS0618: 'SA' is obsolete: 'DA'
                //         F(default(SA));
                Diagnostic(ErrorCode.WRN_DeprecatedSymbolStr, "SA").WithArguments("SA", "DA").WithLocation(6, 19),
                // (7,19): error CS0619: 'SB' is obsolete: 'DB'
                //         F(default(SB));
                Diagnostic(ErrorCode.ERR_DeprecatedSymbolStr, "SB").WithArguments("SB", "DB").WithLocation(7, 19),
                // (8,19): warning CS0618: 'SC' is obsolete: 'OC'
                //         F(default(SC));
                Diagnostic(ErrorCode.WRN_DeprecatedSymbolStr, "SC").WithArguments("SC", "OC").WithLocation(8, 19),
                // (9,19): warning CS0618: 'SD' is obsolete: 'DC'
                //         F(default(SD));
                Diagnostic(ErrorCode.WRN_DeprecatedSymbolStr, "SD").WithArguments("SD", "DC").WithLocation(9, 19),
                // (10,19): error CS0619: 'SE' is obsolete: 'DD'
                //         F(default(SE));
                Diagnostic(ErrorCode.ERR_DeprecatedSymbolStr, "SE").WithArguments("SE", "DD").WithLocation(10, 19),
                // (11,19): error CS0619: 'SF' is obsolete: 'OF'
                //         F(default(SF));
                Diagnostic(ErrorCode.ERR_DeprecatedSymbolStr, "SF").WithArguments("SF", "OF").WithLocation(11, 19),
                // (12,19): warning CS0618: 'IG' is obsolete: 'DG'
                //         F(default(IG));
                Diagnostic(ErrorCode.WRN_DeprecatedSymbolStr, "IG").WithArguments("IG", "DG").WithLocation(12, 19),
                // (13,19): warning CS0618: 'IH' is obsolete: 'DH'
                //         F(default(IH));
                Diagnostic(ErrorCode.WRN_DeprecatedSymbolStr, "IH").WithArguments("IH", "DH").WithLocation(13, 19),
                // (14,19): warning CS0618: 'II' is obsolete: 'DI'
                //         F(default(II));
                Diagnostic(ErrorCode.WRN_DeprecatedSymbolStr, "II").WithArguments("II", "DI").WithLocation(14, 19),
                // (15,19): error CS0619: 'IJ' is obsolete: 'DJ'
                //         F(default(IJ));
                Diagnostic(ErrorCode.ERR_DeprecatedSymbolStr, "IJ").WithArguments("IJ", "DJ").WithLocation(15, 19),
                // (16,19): error CS0619: 'IK' is obsolete: 'DK'
                //         F(default(IK));
                Diagnostic(ErrorCode.ERR_DeprecatedSymbolStr, "IK").WithArguments("IK", "DK").WithLocation(16, 19),
                // (17,19): warning CS0618: 'EM' is obsolete: 'OM'
                //         F(default(EM));
                Diagnostic(ErrorCode.WRN_DeprecatedSymbolStr, "EM").WithArguments("EM", "OM").WithLocation(17, 19),
                // (18,19): error CS0619: 'EN' is obsolete: 'ON'
                //         F(default(EN));
                Diagnostic(ErrorCode.ERR_DeprecatedSymbolStr, "EN").WithArguments("EN", "ON").WithLocation(18, 19),
                // (19,19): warning CS0618: 'EO' is obsolete: 'DO'
                //         F(default(EO));
                Diagnostic(ErrorCode.WRN_DeprecatedSymbolStr, "EO").WithArguments("EO", "DO").WithLocation(19, 19),
                // (20,19): error CS0619: 'EP' is obsolete: 'DP'
                //         F(default(EP));
                Diagnostic(ErrorCode.ERR_DeprecatedSymbolStr, "EP").WithArguments("EP", "DP").WithLocation(20, 19));
        }

        [Fact]
        public void TestImportStatements()
        {
            var source =
@"#pragma warning disable 219
#pragma warning disable 8019
using System;
using Windows.Foundation.Metadata;
using CA = C<A>;
using CB = C<B>;
using CC = C<C>;
using CD = C<D>;
[Obsolete] class A { }
[Obsolete] class B { }
[Experimental] class C { }
[Experimental] class D { }
class C<T> { }
class P
{
    static void Main()
    {
        object o;
        o = default(CB);
        o = default(CD);
    }
}";
            var comp = CreateCompilationWithMscorlib40AndSystemCore(new[] { Parse(ExperimentalAttributeSource), Parse(source) });
            comp.VerifyDiagnostics(
                // (19,21): warning CS0612: 'B' is obsolete
                //         o = default(CB);
                Diagnostic(ErrorCode.WRN_DeprecatedSymbol, "CB").WithArguments("B").WithLocation(19, 21),
                // (20,21): warning CS8305: 'D' is for evaluation purposes only and is subject to change or removal in future updates.
                //         o = default(CD);
                Diagnostic(ErrorCode.WRN_Experimental, "CD").WithArguments("D").WithLocation(20, 21));
        }
    }
}
